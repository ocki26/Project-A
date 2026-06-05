using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// LPC Setup Fixer & Dashboard — Bộ công cụ chẩn đoán, thiết lập nhân vật trung tâm chuẩn LPC.
/// </summary>
public class LPCSetupFixer : EditorWindow
{
    private struct LPCLayer
    {
        public string name;
        public int defaultOrder;
        public string displayName;
        public bool isAlwaysOn;

        public LPCLayer(string name, int order, string displayName, bool isAlwaysOn = false)
        {
            this.name = name;
            this.defaultOrder = order;
            this.displayName = displayName;
            this.isAlwaysOn = isAlwaysOn;
        }
    }

    private static readonly LPCLayer[] StandardLayers = new LPCLayer[]
    {
        new LPCLayer("Shadow",       -10, "Bóng nhân vật", true),
        new LPCLayer("WeaponBehind",  -5, "Vũ khí (sau lưng)"),
        new LPCLayer("ShieldBehind",  -4, "Khiên (sau lưng)"),
        new LPCLayer("CapeBehind",    -3, "Áo choàng (sau lưng)"),
        new LPCLayer("Quiver",        -2, "Ống đựng tên"),
        new LPCLayer("HairBehind",    -1, "Tóc dài (sau lưng)", true),
        new LPCLayer("Body",           0, "Thân hình gốc (Base)", true),
        new LPCLayer("Ears",           1, "Tai (Elf, thú)", true),
        new LPCLayer("Eyes",           2, "Mắt", true),
        new LPCLayer("Underwear",     10, "Đồ lót", true),
        new LPCLayer("Legs",          11, "Quần"),
        new LPCLayer("Feet",          12, "Giày / Bốt"),
        new LPCLayer("Torso",         20, "Áo trong"),
        new LPCLayer("Armor",         21, "Áo giáp / Áo khoác ngoài"),
        new LPCLayer("Arms",          22, "Tay áo"),
        new LPCLayer("Gloves",        23, "Găng tay"),
        new LPCLayer("Belt",          30, "Thắt lưng"),
        new LPCLayer("Neck",          31, "Khăn quàng cổ"),
        new LPCLayer("Shoulders",     32, "Giáp vai"),
        new LPCLayer("FacialHair",    40, "Râu"),
        new LPCLayer("HairFront",     41, "Tóc mái", true),
        new LPCLayer("Mask",          42, "Mặt nạ / Kính"),
        new LPCLayer("Helmet",        43, "Mũ / Mũ giáp"),
        new LPCLayer("Shield",        50, "Khiên (mặt trước)"),
        new LPCLayer("Weapon",        51, "Vũ khí (mặt trước)"),
        new LPCLayer("Effects",       60, "Hiệu ứng đòn đánh")
    };

    private const string SortingLayerName = "Characters";

    // Diagnosis item structure
    private enum IssueSeverity { Error, Warning, OK }
    private class DiagnosticIssue
    {
        public string description;
        public IssueSeverity severity;
        public System.Action fixAction; // Callback to fix this specific issue

        public DiagnosticIssue(string desc, IssueSeverity sev, System.Action fix = null)
        {
            description = desc;
            severity = sev;
            fixAction = fix;
        }
    }

    private GameObject targetPlayer;
    private string animFolder = "Assets/Animations/Character";
    private string spritesRoot = "Assets/Sprites";
    private bool autoFixOnDiagnose = false;
    private int activeTab = 0;
    private Vector2 scrollPos;

    // Diagnose results lists
    private List<DiagnosticIssue> diagnosticIssues = new List<DiagnosticIssue>();
    private bool diagnosed = false;

    // GUI styles cached
    private GUIStyle headerStyle;
    private GUIStyle tabButtonStyle;
    private GUIStyle cardStyle;
    private GUIStyle errorStyle;
    private GUIStyle warningStyle;
    private GUIStyle okStyle;

    [MenuItem("Tools/LPC Character Toolkit Dashboard")]
    public static void ShowWindow()
    {
        var w = GetWindow<LPCSetupFixer>("LPC Toolkit");
        w.minSize = new Vector2(600, 550);
    }

    private void OnGUI()
    {
        InitStyles();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawDashboardHeader();
        DrawTabButtons();

        EditorGUILayout.Space(10);

        switch (activeTab)
        {
            case 0:
                DrawDiagnosticsTab();
                break;
            case 1:
                DrawPlayerBuilderTab();
                break;
            case 2:
                DrawAdvancedToolsTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.15f, 0.6f, 1f) }
        };

        tabButtonStyle = new GUIStyle(EditorStyles.miniButtonMid)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 28
        };

        cardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };

        errorStyle = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = new Color(1f, 0.4f, 0.4f) }
        };

        warningStyle = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = new Color(1f, 0.8f, 0.2f) }
        };

        okStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.4f, 1f, 0.4f) }
        };
    }

    private void DrawDashboardHeader()
    {
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("🛠️ LPC Character Toolkit Dashboard", headerStyle);
        EditorGUILayout.LabelField("Bộ công cụ chuyên nghiệp quản lý, thiết lập & sửa lỗi nhân vật chuẩn LPC", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(8);
    }

    private void DrawTabButtons()
    {
        EditorGUILayout.BeginHorizontal();

        string[] tabNames = { "🔍 Chẩn Đoán & Sửa Lỗi", "🏗️ Khởi Tạo Nhân Vật (Builder)", "⚙️ Công Cụ Nâng Cao" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            GUI.backgroundColor = activeTab == i ? new Color(0.15f, 0.6f, 1f) : Color.white;
            if (GUILayout.Button(tabNames[i], tabButtonStyle))
            {
                activeTab = i;
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    // =========================================================================
    //  TAB 1: DIAGNOSTICS & FIXER
    // =========================================================================

    private void DrawDiagnosticsTab()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.LabelField("🎯 Đối tượng Cần Chẩn Đoán", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        targetPlayer = (GameObject)EditorGUILayout.ObjectField("Player GameObject", targetPlayer, typeof(GameObject), true);
        animFolder = EditorGUILayout.TextField("Thư mục Anim", animFolder);
        spritesRoot = EditorGUILayout.TextField("Thư mục Sprites", spritesRoot);
        autoFixOnDiagnose = EditorGUILayout.Toggle("Tự động sửa khi quét", autoFixOnDiagnose);

        if (targetPlayer == null)
        {
            EditorGUILayout.HelpBox("Kéo GameObject Nhân vật (Player) từ Hierarchy vào đây, hoặc chọn nó rồi nhấn nút bên dưới.", MessageType.Info);
            if (GUILayout.Button("Sử dụng GameObject đang chọn"))
            {
                if (Selection.activeGameObject != null)
                {
                    targetPlayer = Selection.activeGameObject;
                }
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        GUI.enabled = targetPlayer != null;
        GUI.backgroundColor = new Color(0.9f, 0.7f, 0.1f);
        if (GUILayout.Button("🔍 Chạy Chẩn Đoán Hệ Thống", GUILayout.Height(36)))
        {
            DiagnosePlayer();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (diagnosed && targetPlayer != null)
        {
            DrawDiagnosticResults();
        }
    }

    private void DrawDiagnosticResults()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("📋 Kết Quả Quét Hệ Thống", EditorStyles.boldLabel);

        int errorCount = diagnosticIssues.Count(i => i.severity == IssueSeverity.Error);
        int warningCount = diagnosticIssues.Count(i => i.severity == IssueSeverity.Warning);
        int okCount = diagnosticIssues.Count(i => i.severity == IssueSeverity.OK);

        // Summary Badges
        EditorGUILayout.BeginHorizontal();
        DrawBadge($"{errorCount} Lỗi Nghiêm Trọng", new Color(1f, 0.3f, 0.3f), errorCount > 0);
        DrawBadge($"{warningCount} Cảnh Báo", new Color(1f, 0.75f, 0f), warningCount > 0);
        DrawBadge($"{okCount} Đã Đạt Chuẩn", new Color(0.3f, 0.9f, 0.3f), okCount > 0);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Batch fix button if issues exist
        var fixable = diagnosticIssues.Where(i => i.fixAction != null).ToList();
        if (fixable.Count > 0)
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button($"🔧 SỬA TẤT CẢ LỖI TỰ ĐỘNG ({fixable.Count} mục)", GUILayout.Height(34)))
            {
                FixAllIssuesBatch();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(8);
        }

        // Show issues list
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (diagnosticIssues.Count == 0)
        {
            EditorGUILayout.LabelField("Không có kết quả nào. Hãy nhấn Chạy Chẩn Đoán.", EditorStyles.miniLabel);
        }
        else
        {
            foreach (var issue in diagnosticIssues)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Icon and Description
                string iconPrefix = issue.severity switch
                {
                    IssueSeverity.Error => "🔴 [LỖI]: ",
                    IssueSeverity.Warning => "🟡 [CẢNH BÁO]: ",
                    _ => "🟢 [ĐẠT]: "
                };

                GUIStyle textStyle = issue.severity switch
                {
                    IssueSeverity.Error => errorStyle,
                    IssueSeverity.Warning => warningStyle,
                    _ => okStyle
                };

                EditorGUILayout.LabelField(iconPrefix + issue.description, textStyle, GUILayout.Width(position.width - 160));

                // Individual fix button
                if (issue.fixAction != null)
                {
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
                    if (GUILayout.Button("Sửa Nhanh", GUILayout.Width(80), GUILayout.Height(18)))
                    {
                        issue.fixAction.Invoke();
                        DiagnosePlayer(); // Rescan
                        GUIUtility.ExitGUI();
                    }
                    GUI.backgroundColor = Color.white;
                }
                else if (issue.severity != IssueSeverity.OK)
                {
                    GUI.enabled = false;
                    GUILayout.Button("Sửa Thủ Công", GUILayout.Width(80), GUILayout.Height(18));
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawBadge(string text, Color col, bool active)
    {
        GUI.backgroundColor = active ? col : new Color(0.35f, 0.35f, 0.35f);
        GUILayout.Box(text, GUILayout.Height(24), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = Color.white;
    }

    private void DiagnosePlayer()
    {
        if (targetPlayer == null) return;

        diagnosticIssues.Clear();
        diagnosed = true;

        // 1. Check Animator on Root
        var rootAnim = targetPlayer.GetComponent<Animator>();
        if (rootAnim == null)
        {
            diagnosticIssues.Add(new DiagnosticIssue(
                "Root GameObject không có thành phần Animator.",
                IssueSeverity.Error,
                () => { Undo.AddComponent<Animator>(targetPlayer); Debug.Log("[Fixer] Đã thêm Animator vào root."); }
            ));
        }
        else if (rootAnim.runtimeAnimatorController == null)
        {
            diagnosticIssues.Add(new DiagnosticIssue(
                "Root Animator chưa được gán Animator Controller.",
                IssueSeverity.Warning
            ));
        }
        else
        {
            diagnosticIssues.Add(new DiagnosticIssue(
                $"Root Animator hoạt động bình thường ({rootAnim.runtimeAnimatorController.name}).",
                IssueSeverity.OK
            ));
        }

        // 2. Check SpriteRenderer on Root
        var rootSR = targetPlayer.GetComponent<SpriteRenderer>();
        if (rootSR == null)
        {
            diagnosticIssues.Add(new DiagnosticIssue(
                "Root GameObject không có SpriteRenderer làm tham chiếu cơ sở.",
                IssueSeverity.Warning,
                () => {
                    var sr = Undo.AddComponent<SpriteRenderer>(targetPlayer);
                    sr.sortingLayerName = SortingLayerName;
                    Debug.Log("[Fixer] Đã thêm SpriteRenderer vào root.");
                }
            ));
        }
        else
        {
            diagnosticIssues.Add(new DiagnosticIssue(
                $"Root SpriteRenderer hợp lệ (Layer: {rootSR.sortingLayerName}, Order: {rootSR.sortingOrder}).",
                IssueSeverity.OK
            ));
        }

        // 3. Verify Child Nodes
        string rootLayer = rootSR != null ? rootSR.sortingLayerName : SortingLayerName;
        var children = GetAllChildren(targetPlayer);

        foreach (var layerDef in StandardLayers)
        {
            var t = targetPlayer.transform.Find(layerDef.name);
            if (t == null)
            {
                diagnosticIssues.Add(new DiagnosticIssue(
                    $"Thiếu GameObject con biểu diễn lớp trang bị: '{layerDef.name}' ({layerDef.displayName}).",
                    IssueSeverity.Warning,
                    () => {
                        var child = new GameObject(layerDef.name);
                        Undo.RegisterCreatedObjectUndo(child, "Create Child Layer");
                        child.transform.SetParent(targetPlayer.transform, false);
                        child.transform.localPosition = Vector3.zero;
                        var sr = child.AddComponent<SpriteRenderer>();
                        sr.sortingLayerName = rootLayer;
                        sr.sortingOrder = layerDef.defaultOrder;
                        sr.enabled = layerDef.isAlwaysOn;
                        Debug.Log($"[Fixer] Đã tạo node con '{layerDef.name}'");
                    }
                ));
            }
            else
            {
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    diagnosticIssues.Add(new DiagnosticIssue(
                        $"Node con '{layerDef.name}' thiếu thành phần SpriteRenderer.",
                        IssueSeverity.Warning,
                        () => {
                            var nSR = Undo.AddComponent<SpriteRenderer>(t.gameObject);
                            nSR.sortingLayerName = rootLayer;
                            nSR.sortingOrder = layerDef.defaultOrder;
                            Debug.Log($"[Fixer] Đã thêm SpriteRenderer vào node '{layerDef.name}'");
                        }
                    ));
                }
                else
                {
                    bool wrongLayer = sr.sortingLayerName != rootLayer;
                    bool wrongOrder = sr.sortingOrder != layerDef.defaultOrder;
                    bool isAlwaysOn = layerDef.isAlwaysOn;
                    bool shouldBeEnabledButIsnt = isAlwaysOn && !sr.enabled;

                    if (wrongLayer || wrongOrder || shouldBeEnabledButIsnt)
                    {
                        string desc = $"Node '{layerDef.name}' có thiết lập sai:";
                        if (wrongLayer) desc += $" Layer '{sr.sortingLayerName}' (cần '{rootLayer}').";
                        if (wrongOrder) desc += $" SortingOrder={sr.sortingOrder} (cần {layerDef.defaultOrder}).";
                        if (shouldBeEnabledButIsnt) desc += " SpriteRenderer bị tắt mặc dù là lớp hiển thị thường trực.";

                        diagnosticIssues.Add(new DiagnosticIssue(
                            desc,
                            IssueSeverity.Warning,
                            () => {
                                Undo.RecordObject(sr, "Correct Layer/Order");
                                sr.sortingLayerName = rootLayer;
                                sr.sortingOrder = layerDef.defaultOrder;
                                if (isAlwaysOn) sr.enabled = true;
                                EditorUtility.SetDirty(sr);
                                Debug.Log($"[Fixer] Đã hiệu chỉnh lại Layer/Order cho '{layerDef.name}'");
                            }
                        ));
                    }
                    else
                    {
                        diagnosticIssues.Add(new DiagnosticIssue(
                            $"Lớp '{layerDef.name}' được cấu hình chính xác (Order: {sr.sortingOrder}).",
                            IssueSeverity.OK
                        ));
                    }
                }
            }
        }

        // 4. Verify Animation Clips for Missing Sprite references
        if (AssetDatabase.IsValidFolder(animFolder))
        {
            var clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animFolder });
            int totalClips = 0;
            int missingCurves = 0;

            foreach (string guid in clipGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                totalClips++;
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                foreach (var binding in bindings)
                {
                    if (binding.propertyName != "m_Sprite") continue;

                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    if (keys != null && keys.Any(k => k.value == null))
                    {
                        missingCurves++;
                        string clipName = clip.name;
                        string bindPath = binding.path;
                        diagnosticIssues.Add(new DiagnosticIssue(
                            $"Clip '{clipName}' bị mất Sprite (Missing) tại node '{bindPath}'.",
                            IssueSeverity.Error,
                            () => {
                                var spriteMap = BuildSpriteMap();
                                if (spriteMap.Count == 0)
                                {
                                    EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy sprite nào trong thư mục Sprites để thay thế.", "OK");
                                    return;
                                }
                                var rebuiltKeys = TryRebuildCurve(clip, binding, keys, spriteMap);
                                if (rebuiltKeys != null)
                                {
                                    Undo.RecordObject(clip, "Auto Fix Missing Sprite");
                                    AnimationUtility.SetObjectReferenceCurve(clip, binding, rebuiltKeys);
                                    EditorUtility.SetDirty(clip);
                                    Debug.Log($"[Fixer] Đã tự động khôi phục chuyển động cho clip '{clipName}' đường dẫn '{bindPath}'");
                                }
                                else
                                {
                                    Debug.LogError($"[Fixer] Không tìm thấy sprite thay thế phù hợp cho clip '{clipName}'");
                                }
                            }
                        ));
                    }
                }
            }

            if (missingCurves == 0 && totalClips > 0)
            {
                diagnosticIssues.Add(new DiagnosticIssue(
                    $"Quét thành công {totalClips} animation clips: Không phát hiện lỗi mất Sprite nào.",
                    IssueSeverity.OK
                ));
            }
        }
        else
        {
            diagnosticIssues.Add(new DiagnosticIssue(
                $"Thư mục chứa animations không hợp lệ: '{animFolder}'",
                IssueSeverity.Warning
            ));
        }

        // Auto-fix if checked
        if (autoFixOnDiagnose)
        {
            FixAllIssuesBatch();
        }
    }

    private void FixAllIssuesBatch()
    {
        var fixable = diagnosticIssues.Where(i => i.fixAction != null).ToList();
        if (fixable.Count == 0) return;

        EditorUtility.DisplayProgressBar("LPC Tool Fixer", "Đang sửa tự động các mục lỗi...", 0.1f);
        for (int i = 0; i < fixable.Count; i++)
        {
            EditorUtility.DisplayProgressBar("LPC Tool Fixer", $"Đang xử lý mục {i + 1}/{fixable.Count}...", (float)i / fixable.Count);
            fixable[i].fixAction.Invoke();
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Rescan to check results
        DiagnosePlayer();
        EditorUtility.DisplayDialog("LPC Toolkit", "Đã hoàn thành sửa chữa tự động hàng loạt!", "Tuyệt vời");
    }

    // =========================================================================
    //  TAB 2: PLAYER BUILDER
    // =========================================================================

    private void DrawPlayerBuilderTab()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.LabelField("🏗️ LPC Player Hierarchy Builder", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Thiết lập cấu trúc nhân vật chuẩn 26 lớp hiển thị để đảm bảo hoạt động tương thích với bộ Animator Controller sinh ra từ Importer.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);

        // Display layers list preview
        EditorGUILayout.LabelField("Xem Trước Phân Cấp Lớp (Dưới cùng -> Trên cùng):", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
        foreach (var layer in StandardLayers)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  [{layer.defaultOrder}]", GUILayout.Width(50));
            EditorGUILayout.LabelField(layer.name, EditorStyles.boldLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField($"— {layer.displayName}", EditorStyles.miniLabel);
            if (layer.isAlwaysOn)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("(Thường Trực)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("🏗️ Khởi Tạo Mới Nhân Vật Gốc (Player)", GUILayout.Height(36)))
        {
            CreateNewPlayerHierarchy();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("🔧 Sửa Phân Cấp Nhân Vật Đang Chọn", GUILayout.Height(36)))
        {
            FixSelectedPlayerHierarchy();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void CreateNewPlayerHierarchy()
    {
        // Add character sorting layer if it doesn't exist
        EnsureSortingLayerExists(SortingLayerName);

        var root = new GameObject("New_LPC_Player");
        Undo.RegisterCreatedObjectUndo(root, "Create New LPC Player");

        var rs = root.AddComponent<SpriteRenderer>();
        rs.sortingLayerName = SortingLayerName;
        rs.sortingOrder = 0;

        var an = root.AddComponent<Animator>();
        an.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var cc = root.AddComponent<CapsuleCollider2D>();
        cc.size = new Vector2(0.5f, 0.8f);
        cc.offset = new Vector2(0, 0);

        foreach (var def in StandardLayers)
        {
            var child = new GameObject(def.name);
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.zero;

            var sr = child.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = SortingLayerName;
            sr.sortingOrder = def.defaultOrder;
            sr.enabled = def.isAlwaysOn;
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog("Thành Công", "Đã tạo cấu trúc nhân vật chuẩn LPC thành công!\nBạn có thể gắn Animator Controller vào root và thực hiện Compose.", "OK");
    }

    private void FixSelectedPlayerHierarchy()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn GameObject nhân vật trong Hierarchy trước!", "OK");
            return;
        }

        EnsureSortingLayerExists(SortingLayerName);

        var rs = go.GetComponent<SpriteRenderer>();
        if (rs == null) rs = Undo.AddComponent<SpriteRenderer>(go);

        Undo.RecordObject(rs, "Fix Player Root SpriteRenderer");
        rs.sortingLayerName = SortingLayerName;
        rs.sortingOrder = 0;
        EditorUtility.SetDirty(rs);

        var an = go.GetComponent<Animator>();
        if (an != null)
        {
            Undo.RecordObject(an, "Fix Player Root Animator");
            an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(an);
        }

        int added = 0;
        int fixedCount = 0;

        foreach (var def in StandardLayers)
        {
            var t = go.transform.Find(def.name);
            GameObject child;

            if (t == null)
            {
                child = new GameObject(def.name);
                Undo.RegisterCreatedObjectUndo(child, "Add Missing Layer Object");
                child.transform.SetParent(go.transform, false);
                child.transform.localPosition = Vector3.zero;
                added++;
            }
            else
            {
                child = t.gameObject;
            }

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(child);

            Undo.RecordObject(sr, "Fix Child SpriteRenderer Settings");
            sr.sortingLayerName = SortingLayerName;
            sr.sortingOrder = def.defaultOrder;
            if (def.isAlwaysOn && !sr.enabled) sr.enabled = true;
            EditorUtility.SetDirty(sr);

            fixedCount++;
        }

        EditorUtility.DisplayDialog("Hoàn Thành", $"Đã sửa nhân vật '{go.name}' thành công!\nĐã thêm {added} node con bị thiếu, chuẩn hóa {fixedCount} thành phần SpriteRenderer.", "OK");
    }

    // =========================================================================
    //  TAB 3: ADVANCED UTILITIES
    // =========================================================================

    private void DrawAdvancedToolsTab()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.LabelField("⚙️ Công Cụ Bổ Trợ Nâng Cao", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        if (GUILayout.Button("🧼 Làm Sạch Clip: Xóa Bindings Trùng Lặp", GUILayout.Height(32)))
        {
            CleanDuplicateBindings();
        }
        EditorGUILayout.LabelField("Xóa các liên kết Sprite thừa bị nhân bản trong file animation .anim do quá trình ghép (Bake) nhiều lần gây ra.", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("🔓 Bật Hiển Thị Tất Cả Renderers", GUILayout.Height(32)))
        {
            EnableAllChildRenderers();
        }
        EditorGUILayout.LabelField("Bật cưỡng ép hiển thị SpriteRenderer trên toàn bộ các node con (shadow, body, weapon...). Hữu ích khi debug.", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("📋 In Báo Cáo Phân Cấp Nhân Vật Ra Console", GUILayout.Height(32)))
        {
            PrintPlayerHierarchyInfoToConsole();
        }
        EditorGUILayout.LabelField("Xuất sơ đồ phân cấp chi tiết của nhân vật kèm Sorting Order hiện tại ra cửa sổ Console để kiểm tra.", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("📊 Khởi Tạo Bộ Đo Hiệu Năng (Performance Monitor)", GUILayout.Height(32)))
        {
            SetupPerformanceMonitor();
        }
        EditorGUILayout.LabelField("Tạo tự động GameObject bộ đo hiệu năng chuyên nghiệp (FPS, RAM, GPU) chạy xuyên suốt toàn game.", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("🏠 Tạo Template Nhà Thông Minh (House Prefab)", GUILayout.Height(32)))
        {
            CreateHousePrefabTemplate();
        }
        EditorGUILayout.LabelField("Tạo cấu trúc nhà mẫu phân cấp gồm Tường, Mái đè (Overhead), Trình Fader làm mờ mái khi vào nhà.", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("🎯 Cân Chỉnh Triggers Trong Scene", GUILayout.Height(32)))
        {
            string res = AutoFixInteriorTrigger();
            EditorUtility.DisplayDialog("Cân Chỉnh Triggers", res, "OK");
        }
        EditorGUILayout.LabelField("Tự động di chuyển và thay đổi kích thước của InteriorTrigger của LPC_Tiled_House_Template để khớp khít với sàn nhà Floor thực tế.", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void SetupPerformanceMonitor()
    {
        var existing = FindFirstObjectByType<LPC_PerformanceMonitor>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("LPC Toolkit", "Bộ đo hiệu năng đã tồn tại trong Scene hiện tại!", "OK");
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            return;
        }

        var go = new GameObject("LPC_PerformanceMonitor");
        Undo.RegisterCreatedObjectUndo(go, "Create Performance Monitor");
        go.AddComponent<LPC_PerformanceMonitor>();

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        EditorUtility.DisplayDialog("Thành Công", "Đã tạo bộ đo hiệu năng thành công! Nhấn F3 trong game để ẩn/hiện HUD.", "OK");
    }

    private void CleanDuplicateBindings()
    {
        if (!AssetDatabase.IsValidFolder(animFolder))
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy thư mục: {animFolder}", "OK");
            return;
        }

        int removed = 0;
        var clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animFolder });
        foreach (string guid in clipGuids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) continue;

            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            var seen = new HashSet<string>();

            foreach (var b in bindings)
            {
                string key = $"{b.path}|{b.type}|{b.propertyName}";
                if (!seen.Add(key))
                {
                    AnimationUtility.SetObjectReferenceCurve(clip, b, null);
                    removed++;
                }
            }

            if (removed > 0) EditorUtility.SetDirty(clip);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Hoàn Thành", $"Đã xóa {removed} liên kết bị trùng lặp trong các file Animation.", "OK");
    }

    private void EnableAllChildRenderers()
    {
        if (targetPlayer == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Player GameObject trước!", "OK");
            return;
        }

        int count = 0;
        foreach (var sr in targetPlayer.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!sr.enabled)
            {
                Undo.RecordObject(sr, "Force Enable Child Renderer");
                sr.enabled = true;
                count++;
            }
        }
        EditorUtility.DisplayDialog("Hoàn Thành", $"Đã kích hoạt hiển thị cho {count} SpriteRenderer.", "OK");
    }

    private void PrintPlayerHierarchyInfoToConsole()
    {
        if (targetPlayer == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Player GameObject trước!", "OK");
            return;
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine($"=== BÁO CÁO CẤU TRÚC PHÂN CẤP LPC: {targetPlayer.name} ===");

        var anim = targetPlayer.GetComponent<Animator>();
        report.AppendLine($"Root Animator: {(anim != null ? (anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "Đã Gán Component Nhưng Thiếu Controller") : "Mất/Thiếu Component")}");

        var rootSR = targetPlayer.GetComponent<SpriteRenderer>();
        report.AppendLine($"Root SpriteRenderer: {(rootSR != null ? $"Order={rootSR.sortingOrder} Layer={rootSR.sortingLayerName}" : "Mất/Thiếu Component")}");

        report.AppendLine("\nChi tiết các lớp con (Nodes):");
        foreach (var child in GetAllChildren(targetPlayer))
        {
            var sr = child.GetComponent<SpriteRenderer>();
            var an = child.GetComponent<Animator>();
            string srInfo = sr != null
                ? $"SpriteRenderer [Order: {sr.sortingOrder}, Layer: {sr.sortingLayerName}, Enabled: {sr.enabled}]"
                : "[THIẾU SPRITE RENDERER]";
            string anInfo = an != null ? $" + Animator [Controller: {(an.runtimeAnimatorController != null ? an.runtimeAnimatorController.name : "Chưa gán")}]" : "";
            report.AppendLine($"  - {child.name}: {srInfo}{anInfo}");
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Hoàn Thành", "Đã in báo cáo cấu trúc ra Console. Vui lòng mở tab Console để kiểm tra.", "Đóng");
    }

    // =========================================================================
    //  CORE HELPERS
    // =========================================================================

    private static List<GameObject> GetAllChildren(GameObject parent)
    {
        var list = new List<GameObject>();
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            list.Add(parent.transform.GetChild(i).gameObject);
        }
        return list;
    }

    private Dictionary<string, Sprite> BuildSpriteMap()
    {
        var map = new Dictionary<string, Sprite>();
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesRoot });

        foreach (string g in guids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(g);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texPath);
            foreach (var asset in assets)
            {
                if (asset is Sprite sp)
                {
                    string key = sp.name.ToLower();
                    if (!map.ContainsKey(key))
                    {
                        map[key] = sp;
                    }
                }
            }
        }
        return map;
    }

    private static ObjectReferenceKeyframe[] TryRebuildCurve(
        AnimationClip clip, EditorCurveBinding binding,
        ObjectReferenceKeyframe[] existing, Dictionary<string, Sprite> spriteMap)
    {
        Sprite referenceSprite = existing.FirstOrDefault(k => k.value != null).value as Sprite;
        if (referenceSprite != null)
        {
            string refName = referenceSprite.name;
            int rIdx = refName.LastIndexOf("_r");
            if (rIdx > 0)
            {
                string prefix = refName.Substring(0, rIdx);
                return RebuildFromPrefix(prefix, existing, spriteMap);
            }
        }

        string clipName = clip.name;
        string animBase = GetAnimBase(clipName);
        int rowHint = GetRowFromClipName(clipName);

        var candidates = spriteMap.Keys
            .Where(k => k.Contains($"_r{rowHint}_c0"))
            .Select(k => k.Substring(0, k.LastIndexOf($"_r{rowHint}_c0")))
            .Distinct()
            .ToList();

        foreach (string prefix in candidates)
        {
            var rebuilt = RebuildFromPrefix(prefix, existing, spriteMap);
            if (rebuilt != null) return rebuilt;
        }

        return null;
    }

    private static ObjectReferenceKeyframe[] RebuildFromPrefix(
        string prefix, ObjectReferenceKeyframe[] existing, Dictionary<string, Sprite> spriteMap)
    {
        Sprite refSprite = existing.FirstOrDefault(k => k.value != null).value as Sprite;
        int row = 0;
        if (refSprite != null)
        {
            string refName = refSprite.name;
            int rIdx = refName.LastIndexOf("_r");
            int cIdx = refName.LastIndexOf("_c");
            if (rIdx >= 0 && cIdx > rIdx)
            {
                int.TryParse(refName.Substring(rIdx + 2, cIdx - rIdx - 2), out row);
            }
        }

        var newKeys = new ObjectReferenceKeyframe[existing.Length];
        bool anyFixed = false;

        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i].value != null)
            {
                newKeys[i] = existing[i];
                continue;
            }

            string targetName = $"{prefix}_r{row}_c{i}".ToLower();
            if (spriteMap.TryGetValue(targetName, out Sprite found))
            {
                newKeys[i] = new ObjectReferenceKeyframe { time = existing[i].time, value = found };
                anyFixed = true;
            }
            else
            {
                newKeys[i] = existing[i];
            }
        }

        return anyFixed ? newKeys : null;
    }

    private static string GetAnimBase(string clipName)
    {
        int idx = clipName.LastIndexOf('_');
        return idx > 0 ? clipName.Substring(0, idx) : clipName;
    }

    private static int GetRowFromClipName(string clipName)
    {
        if (clipName.EndsWith("_Up")) return 0;
        if (clipName.EndsWith("_Left")) return 1;
        if (clipName.EndsWith("_Down")) return 2;
        if (clipName.EndsWith("_Right")) return 3;
        return 2; // Default to down
    }

    private static void EnsureSortingLayerExists(string name)
    {
        var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManager == null || tagManager.Length == 0) return;

        var tm = new SerializedObject(tagManager[0]);
        var prop = tm.FindProperty("m_SortingLayers");

        for (int i = 0; i < prop.arraySize; i++)
        {
            if (prop.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
            {
                return;
            }
        }

        prop.InsertArrayElementAtIndex(prop.arraySize);
        var e = prop.GetArrayElementAtIndex(prop.arraySize - 1);
        e.FindPropertyRelative("name").stringValue = name;
        e.FindPropertyRelative("uniqueID").intValue = name.GetHashCode();

        tm.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log($"[LPC] Đã khởi tạo sorting layer: {name}");
    }

    public static string AddColliderToObstacles()
    {
        var grid = GameObject.Find("valley Map");
        if (grid == null) return "Grid 'valley Map' not found in active scene.";
        
        var obstacles = grid.transform.Find("Obstacles");
        if (obstacles == null) return "GameObject 'Obstacles' not found under 'valley Map'.";
        
        var go = obstacles.gameObject;
        
        // Add TilemapCollider2D if missing
        var tilemapCollider = go.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = go.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        }
        
        // Add CompositeCollider2D if missing
        var compositeCollider = go.GetComponent<CompositeCollider2D>();
        if (compositeCollider == null)
        {
            compositeCollider = go.AddComponent<CompositeCollider2D>();
        }
        
        // Connect them
        tilemapCollider.usedByComposite = true;
        
        // Configure Rigidbody2D to Static
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        // Save changes
        EditorUtility.SetDirty(go);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        }
        
        return "Success: Added and configured TilemapCollider2D, CompositeCollider2D, and Static Rigidbody2D on 'Obstacles'.";
    }

    public static string QueryTileset(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var sb = new System.Text.StringBuilder();
        sb.Append($"Found {assets.Length} assets:\n");
        foreach (var a in assets)
        {
            if (a != null)
            {
                sb.Append($"- Name: {a.name}, Type: {a.GetType().FullName}\n");
            }
        }
        return sb.ToString();
    }

    public static string DrawTestMap()
    {
        var grid = GameObject.Find("valley Map");
        if (grid == null) return "Grid 'valley Map' not found in active scene.";
        
        var groundT = grid.transform.Find("Ground");
        var obstaclesT = grid.transform.Find("Obstacles");
        var overheadT = grid.transform.Find("Overhead");
        
        if (groundT == null || obstaclesT == null) 
            return "Ground or Obstacles tilemap not found.";
            
        var groundMap = groundT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        var obstaclesMap = obstaclesT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        var overheadMap = overheadT != null ? overheadT.GetComponent<UnityEngine.Tilemaps.Tilemap>() : null;
        
        // Clear previous tiles
        groundMap.ClearAllTiles();
        obstaclesMap.ClearAllTiles();
        if (overheadMap != null) overheadMap.ClearAllTiles();
        
        // Load all tiles from the tileset
        string tilesetPath = "Assets/Arsetmap/pixel_16_woods v2 free/Tilesets/pixel_16_woods_v2_free_TileSet.tileset";
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(tilesetPath);
        var tileList = new List<UnityEngine.Tilemaps.TileBase>();
        foreach (var a in subAssets)
        {
            if (a is UnityEngine.Tilemaps.TileBase tile)
            {
                tileList.Add(tile);
            }
        }
        
        if (tileList.Count == 0) return "No TileBase assets found in the specified TileSet.";
        
        // Find specific tiles by name
        UnityEngine.Tilemaps.TileBase grassTile = null;
        UnityEngine.Tilemaps.TileBase dirtTile = null;
        var detailTiles = new List<UnityEngine.Tilemaps.TileBase>();
        
        foreach (var t in tileList)
        {
            string name = t.name.ToLower();
            
            // Grass base tile (Tile 33)
            if (name == "free_pixel_16_woods_33" || name.EndsWith("_33"))
            {
                grassTile = t;
            }
            // Dirt path tile (Tile 37)
            else if (name == "free_pixel_16_woods_37" || name.EndsWith("_37"))
            {
                dirtTile = t;
            }
            // Detail tiles (flowers, small rocks/pebbles) - avoiding mushrooms and bottom-pivot grass tufts
            else if (name == "free_pixel_16_woods_71" || name.EndsWith("_71") || // Yellow flower
                     name == "free_pixel_16_woods_72" || name.EndsWith("_72") || // Pink flower
                     name == "free_pixel_16_woods_45" || name.EndsWith("_45") || // Purple flower
                     name == "free_pixel_16_woods_24" || name.EndsWith("_24") || // Medium rock group
                     name == "free_pixel_16_woods_25" || name.EndsWith("_25"))   // Small pebble
            {
                detailTiles.Add(t);
            }
        }
        
        // Fallbacks if search by name patterns didn't match
        if (grassTile == null) grassTile = tileList[0];
        if (dirtTile == null) dirtTile = tileList.Count > 1 ? tileList[1] : tileList[0];
        
        // Paint a 40x30 ground area with grass
        int width = 40;
        int height = 30;
        int startX = -20;
        int startY = -15;
        
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                groundMap.SetTile(new Vector3Int(x, y, 0), grassTile);
            }
        }
        
        // Paint a horizontal dirt path in the middle
        int pathY = 0;
        for (int x = startX; x < startX + width; x++)
        {
            groundMap.SetTile(new Vector3Int(x, pathY, 0), dirtTile);
            groundMap.SetTile(new Vector3Int(x, pathY + 1, 0), dirtTile);
        }
        
        // Scatter some details (flowers/grass tufts) randomly on the grass (not on the path)
        int detailCount = 0;
        if (detailTiles.Count > 0)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    // Don't paint on the path
                    if (y == pathY || y == pathY + 1) continue;
                    
                    // 6% chance to place a detail
                    if (UnityEngine.Random.value < 0.06f)
                    {
                        var randDetail = detailTiles[UnityEngine.Random.Range(0, detailTiles.Count)];
                        obstaclesMap.SetTile(new Vector3Int(x, y, 0), randDetail);
                        detailCount++;
                    }
                }
            }
        }
        
        // Save scene
        EditorUtility.SetDirty(groundMap);
        EditorUtility.SetDirty(obstaclesMap);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(groundMap.gameObject.scene);
        }
        
        return $"Success: Generated 40x30 Map. Painted {width*height} grass tiles, a dirt path, and scattered {detailCount} flowers/details.";
    }

    public static string PrintPaintedTiles()
    {
        var grid = GameObject.Find("valley Map");
        if (grid == null) return "Grid 'valley Map' not found.";
        
        var groundT = grid.transform.Find("Ground");
        if (groundT == null) return "Ground not found.";
        
        var groundMap = groundT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (groundMap == null) return "Ground Tilemap component not found.";
        
        var tileSet = new HashSet<string>();
        var bounds = groundMap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var tile = groundMap.GetTile(new Vector3Int(x, y, 0));
                if (tile != null)
                {
                    tileSet.Add(tile.name);
                }
            }
        }
        
        var obstaclesT = grid.transform.Find("Obstacles");
        var obstacleSet = new HashSet<string>();
        if (obstaclesT != null)
        {
            var obsMap = obstaclesT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (obsMap != null)
            {
                var obsBounds = obsMap.cellBounds;
                for (int x = obsBounds.xMin; x < obsBounds.xMax; x++)
                {
                    for (int y = obsBounds.yMin; y < obsBounds.yMax; y++)
                    {
                        var tile = obsMap.GetTile(new Vector3Int(x, y, 0));
                        if (tile != null)
                        {
                            obstacleSet.Add(tile.name);
                        }
                    }
                }
            }
        }
        
        return $"Ground unique tiles: {string.Join(", ", tileSet)}\nObstacles unique tiles: {string.Join(", ", obstacleSet)}";
    }

    public static string PrintAllHouseTiles()
    {
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null) return "House template not found.";
        
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < house.transform.childCount; i++)
        {
            var child = house.transform.GetChild(i);
            var tm = child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tm == null) continue;
            
            sb.AppendLine($"=== Tilemap: {child.name} ===");
            var bounds = tm.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var tile = tm.GetTile(new Vector3Int(x, y, 0));
                    if (tile != null)
                    {
                        sb.AppendLine($"  ({x}, {y}): {tile.name}");
                    }
                }
            }
        }
        return sb.ToString();
    }

    public static string GetTileSpriteNames()
    {
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null) return "House template not found.";
        var wallsT = house.transform.Find("Walls");
        if (wallsT == null) return "Walls child not found.";
        var wallsMap = wallsT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        
        var tile12 = wallsMap.GetTile(new UnityEngine.Vector3Int(-12, 4, 0)) as UnityEngine.Tilemaps.Tile;
        var tile11 = wallsMap.GetTile(new UnityEngine.Vector3Int(-11, 4, 0)) as UnityEngine.Tilemaps.Tile;
        
        string s12 = tile12 != null ? (tile12.sprite != null ? tile12.sprite.name : "null sprite") : "null tile";
        string s11 = tile11 != null ? (tile11.sprite != null ? tile11.sprite.name : "null sprite") : "null tile";
        
        return $"Tile (-12, 4): {s12}\nTile (-11, 4): {s11}";
    }

    public static string InspectCoords()
    {
        var sb = new System.Text.StringBuilder();
        
        // 1. Check LPC_Tiled_House_Template/Walls
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house != null)
        {
            var wallsT = house.transform.Find("Walls");
            if (wallsT != null)
            {
                var tm = wallsT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                sb.AppendLine("House/Walls:");
                for (int y = 2; y <= 6; y++)
                {
                    var t = tm.GetTile(new UnityEngine.Vector3Int(-12, y, 0));
                    sb.AppendLine($"  (-12, {y}): {(t != null ? t.name : "empty")}");
                }
            }
            var roofT = house.transform.Find("Roof");
            if (roofT != null)
            {
                var tm = roofT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                sb.AppendLine("House/Roof:");
                for (int y = 2; y <= 6; y++)
                {
                    var t = tm.GetTile(new UnityEngine.Vector3Int(-12, y, 0));
                    sb.AppendLine($"  (-12, {y}): {(t != null ? t.name : "empty")}");
                }
            }
        }
        
        // 2. Check valley Map
        var grid = GameObject.Find("valley Map");
        if (grid != null)
        {
            foreach (string name in new[] { "Ground", "Obstacles", "Overhead" })
            {
                var child = grid.transform.Find(name);
                if (child == null) continue;
                var tm = child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                if (tm == null) continue;
                
                sb.AppendLine($"valley Map/{name}:");
                for (int y = 2; y <= 6; y++)
                {
                    var t = tm.GetTile(new UnityEngine.Vector3Int(-12, y, 0));
                    sb.AppendLine($"  (-12, {y}): {(t != null ? t.name : "empty")}");
                }
            }
        }
        
        return sb.ToString();
    }

    public static string GetTileSpriteCoords()
    {
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null) return "House template not found.";
        var wallsT = house.transform.Find("Walls");
        if (wallsT == null) return "Walls child not found.";
        var wallsMap = wallsT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        
        var tile12_3 = wallsMap.GetTile(new UnityEngine.Vector3Int(-12, 3, 0)) as UnityEngine.Tilemaps.Tile;
        var tile11_3 = wallsMap.GetTile(new UnityEngine.Vector3Int(-11, 3, 0)) as UnityEngine.Tilemaps.Tile;
        
        string s12_3 = tile12_3 != null ? (tile12_3.sprite != null ? tile12_3.sprite.name : "null sprite") : "null tile";
        string s11_3 = tile11_3 != null ? (tile11_3.sprite != null ? tile11_3.sprite.name : "null sprite") : "null tile";
        
        return $"Tile (-12, 3): {s12_3}\nTile (-11, 3): {s11_3}";
    }

    public static string PrintValleyMapHouseTiles()
    {
        var grid = GameObject.Find("valley Map");
        if (grid == null) return "valley Map not found.";
        
        var sb = new System.Text.StringBuilder();
        foreach (string name in new[] { "Ground", "Obstacles", "Overhead" })
        {
            var child = grid.transform.Find(name);
            if (child == null) continue;
            var tm = child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tm == null) continue;
            
            sb.AppendLine($"=== valley Map/{name} ===");
            var bounds = tm.cellBounds;
            int count = 0;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var tile = tm.GetTile(new UnityEngine.Vector3Int(x, y, 0));
                    if (tile != null && tile.name.Contains("walls_roofs"))
                    {
                        sb.AppendLine($"  ({x}, {y}): {tile.name}");
                        count++;
                    }
                }
            }
            sb.AppendLine($"  Total house tiles on {name}: {count}");
        }
        return sb.ToString();
    }

    public static string PrintOverheadTiles()
    {
        var grid = GameObject.Find("valley Map");
        if (grid == null) return "valley Map not found.";
        var overhead = grid.transform.Find("Overhead");
        if (overhead == null) return "Overhead not found.";
        var tm = overhead.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (tm == null) return "Tilemap not found.";
        
        var sb = new System.Text.StringBuilder();
        var bounds = tm.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var tile = tm.GetTile(new UnityEngine.Vector3Int(x, y, 0));
                if (tile != null)
                {
                    sb.AppendLine($"({x}, {y}): {tile.name}");
                }
            }
        }
        return sb.ToString();
    }

    public static string GetTilemapSettings()
    {
        var grid = GameObject.Find("valley Map");
        if (grid == null) return "Grid 'valley Map' not found.";
        
        var sb = new System.Text.StringBuilder();
        foreach (string name in new[] { "Ground", "Obstacles", "Overhead" })
        {
            var child = grid.transform.Find(name);
            if (child == null)
            {
                sb.AppendLine($"{name}: Not found.");
                continue;
            }
            
            var tm = child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            var tmr = child.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            
            sb.AppendLine($"=== Tilemap: {name} ===");
            if (tm == null) sb.AppendLine("  Tilemap component: Missing!");
            if (tmr == null) 
            {
                sb.AppendLine("  TilemapRenderer component: Missing!");
            }
            else
            {
                sb.AppendLine($"  Sorting Layer: {tmr.sortingLayerName}");
                sb.AppendLine($"  Order in Layer: {tmr.sortingOrder}");
                sb.AppendLine($"  Mode: {tmr.mode}");
                sb.AppendLine($"  Detect Chunk Culling Bounds: {tmr.detectChunkCullingBounds}");
                sb.AppendLine($"  Sort Order: {tmr.sortOrder}");
            }
        }
        return sb.ToString();
    }

    public static string GetSelectionInfo()
    {
        var go = Selection.activeGameObject;
        if (go == null) return "No active selection.";
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Selected: {go.name} (InstanceID: {go.GetInstanceID()})");
        
        // Find full path
        string path = go.name;
        var p = go.transform.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        sb.AppendLine($"Path: {path}");
        sb.AppendLine($"Position: {go.transform.position}");
        sb.AppendLine($"ActiveSelf: {go.activeSelf}");
        
        sb.AppendLine("Components:");
        foreach (var c in go.GetComponents<Component>())
        {
            if (c == null) continue;
            sb.AppendLine($"  - {c.GetType().FullName}");
            if (c is SpriteRenderer sr)
            {
                sb.AppendLine($"    Sprite: {(sr.sprite != null ? sr.sprite.name : "null")}");
                sb.AppendLine($"    SortingLayer: {sr.sortingLayerName}");
                sb.AppendLine($"    SortingOrder: {sr.sortingOrder}");
            }
            else if (c is UnityEngine.Tilemaps.TilemapRenderer tmr)
            {
                sb.AppendLine($"    SortingLayer: {tmr.sortingLayerName}");
                sb.AppendLine($"    SortingOrder: {tmr.sortingOrder}");
                sb.AppendLine($"    Mode: {tmr.mode}");
            }
        }
        return sb.ToString();
    }

    public static string FixTilemapSettings()
    {
        var grid = GameObject.Find("valley Map");
        if (grid == null) return "Grid 'valley Map' not found.";
        
        var sb = new System.Text.StringBuilder();
        
        // 1. Ground
        var groundT = grid.transform.Find("Ground");
        if (groundT != null)
        {
            var tmr = groundT.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            if (tmr != null)
            {
                Undo.RecordObject(tmr, "Fix Ground TilemapRenderer Settings");
                tmr.sortingLayerName = "Map_Floor";
                tmr.sortingOrder = 0;
                tmr.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk; // Best performance
                EditorUtility.SetDirty(tmr);
                sb.AppendLine("Ground: Set to Map_Floor, Order 0, Chunk Mode.");
            }
        }
        
        // 2. Obstacles
        var obstaclesT = grid.transform.Find("Obstacles");
        if (obstaclesT != null)
        {
            var tmr = obstaclesT.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            if (tmr != null)
            {
                Undo.RecordObject(tmr, "Fix Obstacles TilemapRenderer Settings");
                tmr.sortingLayerName = "Entities"; // Y-sort with characters
                tmr.sortingOrder = 0;
                tmr.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Individual; // Required for individual Y-sorting
                EditorUtility.SetDirty(tmr);
                sb.AppendLine("Obstacles: Set to Entities, Order 0, Individual Mode (Y-Sort).");
            }
        }
        
        // 3. Overhead
        var overheadT = grid.transform.Find("Overhead");
        if (overheadT != null)
        {
            var tmr = overheadT.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            if (tmr != null)
            {
                Undo.RecordObject(tmr, "Fix Overhead TilemapRenderer Settings");
                tmr.sortingLayerName = "Map_Foreground"; // Always in front of characters
                tmr.sortingOrder = 0;
                tmr.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk; // Overhead static chunk
                EditorUtility.SetDirty(tmr);
                sb.AppendLine("Overhead: Set to Map_Foreground, Order 0, Chunk Mode.");
            }
        }
        
        if (!Application.isPlaying && grid.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(grid.scene);
        }
        
        return "Fix complete:\n" + sb.ToString();
    }

    public static string CreateHousePrefabTemplate()
    {
        var root = new GameObject("LPC_Tiled_House_Template");
        Undo.RegisterCreatedObjectUndo(root, "Create LPC Tiled House Template");
        root.transform.position = Vector3.zero;

        // Add Grid component to the root of the house
        var grid = root.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // 0a. Floor Tilemap (For indoor floor base)
        var floor = new GameObject("Floor");
        floor.transform.SetParent(root.transform, false);
        var floorTM = floor.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var floorTMR = floor.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        floorTMR.sortingLayerName = "Map_Floor";
        floorTMR.sortingOrder = 1; // Drawn on top of main ground (0)
        floorTMR.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;

        // 0b. Floor_Details Tilemap (For carpets, rugs, indoor path details)
        var floorDetails = new GameObject("Floor_Details");
        floorDetails.transform.SetParent(root.transform, false);
        var floorDetailsTM = floorDetails.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var floorDetailsTMR = floorDetails.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        floorDetailsTMR.sortingLayerName = "Map_Floor";
        floorDetailsTMR.sortingOrder = 2; // Drawn on top of Floor (1)
        floorDetailsTMR.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;

        // 1. Walls Tilemap
        var walls = new GameObject("Walls");
        walls.transform.SetParent(root.transform, false);
        var wallsTM = walls.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var wallsTMR = walls.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        wallsTMR.sortingLayerName = "Entities"; // Y-sort with player
        wallsTMR.sortingOrder = 0;
        wallsTMR.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Individual; // Required for dynamic sorting per tile

        // 1b. Walls_Details Tilemap (For doors, windows, decorations on top of walls)
        var wallsDetails = new GameObject("Walls_Details");
        wallsDetails.transform.SetParent(root.transform, false);
        var wallsDetailsTM = wallsDetails.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var wallsDetailsTMR = wallsDetails.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        wallsDetailsTMR.sortingLayerName = "Entities";
        wallsDetailsTMR.sortingOrder = 1; // Always drawn on top of base Walls
        wallsDetailsTMR.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Individual;

        // 2. Roof Tilemap
        var roof = new GameObject("Roof");
        roof.transform.SetParent(root.transform, false);
        var roofTM = roof.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var roofTMR = roof.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        roofTMR.sortingLayerName = "Map_Foreground"; // Always in front of player
        roofTMR.sortingOrder = 0;
        roofTMR.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk; // Overhead chunk

        // 2b. Roof_Details Tilemap (For chimneys, trims, decorations on top of roof)
        var roofDetails = new GameObject("Roof_Details");
        roofDetails.transform.SetParent(root.transform, false);
        var roofDetailsTM = roofDetails.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var roofDetailsTMR = roofDetails.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        roofDetailsTMR.sortingLayerName = "Map_Foreground";
        roofDetailsTMR.sortingOrder = 1; // Always drawn on top of base Roof
        roofDetailsTMR.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;

        // 3. Colliders Group (For manual colliders)
        var colliders = new GameObject("Colliders");
        colliders.transform.SetParent(root.transform, false);
        var col = colliders.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 1f);
        col.offset = new Vector2(0f, -0.5f);

        // 4. Interior Trigger (For fading the roof tilemap)
        var triggerObj = new GameObject("InteriorTrigger");
        triggerObj.transform.SetParent(root.transform, false);
        var triggerCol = triggerObj.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector2(2.5f, 1.8f);
        
        var fader = triggerObj.AddComponent<LPC_RoofFader>();
        fader.roofTilemaps = new List<UnityEngine.Tilemaps.Tilemap> { roofTM, roofDetailsTM };

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        
        return "Success: Created LPC Tiled House Template in Scene with Floor, Floor_Details, Walls, Walls_Details, Roof, and Roof_Details layers.";
    }

    public static string AddDetailsLayerToActiveHouse()
    {
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null) return "House template 'LPC_Tiled_House_Template' not found in active scene.";
        
        var detailsT = house.transform.Find("Walls_Details");
        if (detailsT != null) return "Walls_Details already exists on the house.";
        
        var wallsDetails = new GameObject("Walls_Details");
        Undo.RegisterCreatedObjectUndo(wallsDetails, "Add Walls_Details Layer");
        wallsDetails.transform.SetParent(house.transform, false);
        wallsDetails.transform.localPosition = Vector3.zero;
        
        var tm = wallsDetails.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var tmr = wallsDetails.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        tmr.sortingLayerName = "Entities";
        tmr.sortingOrder = 1; // On top of Walls (Order 0)
        tmr.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Individual;
        
        // Save changes
        EditorUtility.SetDirty(wallsDetails);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(house.scene);
        }
        
        Selection.activeGameObject = wallsDetails;
        EditorGUIUtility.PingObject(wallsDetails);
        
        return "Success: Added 'Walls_Details' Tilemap layer to your active house template.";
    }

    public static string AddRoofDetailsLayerToActiveHouse()
    {
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null) return "House template 'LPC_Tiled_House_Template' not found in active scene.";
        
        var detailsT = house.transform.Find("Roof_Details");
        if (detailsT != null) return "Roof_Details already exists on the house.";
        
        var roofDetails = new GameObject("Roof_Details");
        Undo.RegisterCreatedObjectUndo(roofDetails, "Add Roof_Details Layer");
        roofDetails.transform.SetParent(house.transform, false);
        roofDetails.transform.localPosition = Vector3.zero;
        
        var tm = roofDetails.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var tmr = roofDetails.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        tmr.sortingLayerName = "Map_Foreground";
        tmr.sortingOrder = 1; // On top of Roof (Order 0)
        tmr.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;
        
        // Update LPC_RoofFader references on the house if present
        var triggerT = house.transform.Find("InteriorTrigger");
        if (triggerT != null)
        {
            var fader = triggerT.GetComponent<LPC_RoofFader>();
            if (fader != null)
            {
                var tms = new List<UnityEngine.Tilemaps.Tilemap>();
                var srs = new List<SpriteRenderer>();
                foreach (Transform child in house.transform)
                {
                    string lowerName = child.name.ToLower();
                    if (lowerName.Contains("roof") || lowerName.Contains("overhead") || lowerName.Contains("chimney") || lowerName.Contains("ceiling"))
                    {
                        var childTM = child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                        if (childTM != null) tms.Add(childTM);
                        
                        var childSR = child.GetComponent<SpriteRenderer>();
                        if (childSR != null) srs.Add(childSR);
                    }
                }
                Undo.RecordObject(fader, "Update Roof Fader References");
                fader.roofTilemaps = tms;
                fader.roofSpriteRenderers = srs;
                fader.roofTilemap = null;
                fader.roofSpriteRenderer = null;
                EditorUtility.SetDirty(fader);
            }
        }

        // Save changes
        EditorUtility.SetDirty(roofDetails);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(house.scene);
        }
        
        Selection.activeGameObject = roofDetails;
        EditorGUIUtility.PingObject(roofDetails);
        
        return "Success: Added 'Roof_Details' Tilemap layer and updated Roof Fader components on your active house template.";
    }

    public static string AddFloorLayersToActiveHouse()
    {
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null) return "House template 'LPC_Tiled_House_Template' not found in active scene.";
        
        var sb = new System.Text.StringBuilder();
        
        // 1. Floor
        var floorT = house.transform.Find("Floor");
        GameObject floorObj = null;
        if (floorT == null)
        {
            floorObj = new GameObject("Floor");
            Undo.RegisterCreatedObjectUndo(floorObj, "Add Floor Layer");
            floorObj.transform.SetParent(house.transform, false);
            floorObj.transform.localPosition = Vector3.zero;
            
            var tm = floorObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var tmr = floorObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            tmr.sortingLayerName = "Map_Floor";
            tmr.sortingOrder = 1; // Above main ground (0)
            tmr.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;
            sb.AppendLine("Added 'Floor' layer (Order 1).");
        }
        
        // 2. Floor_Details
        var detailsT = house.transform.Find("Floor_Details");
        GameObject detailsObj = null;
        if (detailsT == null)
        {
            detailsObj = new GameObject("Floor_Details");
            Undo.RegisterCreatedObjectUndo(detailsObj, "Add Floor_Details Layer");
            detailsObj.transform.SetParent(house.transform, false);
            detailsObj.transform.localPosition = Vector3.zero;
            
            var tm = detailsObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var tmr = detailsObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            tmr.sortingLayerName = "Map_Floor";
            tmr.sortingOrder = 2; // Above Floor (1)
            tmr.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;
            sb.AppendLine("Added 'Floor_Details' layer (Order 2).");
        }
        
        // Save changes
        if (sb.Length > 0)
        {
            var dirtyObj = floorObj != null ? floorObj : (detailsObj != null ? detailsObj : house);
            EditorUtility.SetDirty(dirtyObj);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(house.scene);
            }
        }
        
        var targetSelect = detailsObj != null ? detailsObj : (floorObj != null ? floorObj : house);
        Selection.activeGameObject = targetSelect;
        EditorGUIUtility.PingObject(targetSelect);
        
        return sb.Length > 0 ? "Success:\n" + sb.ToString() : "Floor and Floor_Details layers already exist on the house.";
    }

    public static string GetPositions()
    {
        var sb = new System.Text.StringBuilder();
        
        var player = GameObject.Find("Player2");
        if (player != null)
        {
            sb.AppendLine($"Player2 Pos: {player.transform.position}");
            var col = player.GetComponent<Collider2D>();
            if (col != null)
            {
                sb.AppendLine($"  Player2 Collider: {col.GetType().Name}, bounds: {col.bounds}, isTrigger: {col.isTrigger}");
            }
        }
        else
        {
            sb.AppendLine("Player2 not found");
        }
        
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house != null)
        {
            sb.AppendLine($"House Pos: {house.transform.position}");
            var trigger = house.transform.Find("InteriorTrigger");
            if (trigger != null)
            {
                sb.AppendLine($"InteriorTrigger LocalPos: {trigger.transform.localPosition}, WorldPos: {trigger.transform.position}");
                var col = trigger.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    sb.AppendLine($"  BoxCollider2D size: {col.size}, offset: {col.offset}, isTrigger: {col.isTrigger}, bounds: {col.bounds}");
                }
                var fader = trigger.GetComponent<LPC_RoofFader>();
                if (fader != null)
                {
                    sb.AppendLine($"  LPC_RoofFader component present on InteriorTrigger");
                }
            }
            else
            {
                sb.AppendLine("InteriorTrigger child not found under house template");
            }
        }
        else
        {
            sb.AppendLine("House template not found");
        }
        
        var grass = GameObject.Find("grass");
        if (grass != null)
        {
            sb.AppendLine($"grass Pos: {grass.transform.position}");
            for (int i = 0; i < grass.transform.childCount; i++)
            {
                var child = grass.transform.GetChild(i);
                sb.AppendLine($"  Child: {child.name} Pos: {child.position}");
                var col = child.GetComponent<Collider2D>();
                if (col != null)
                {
                    sb.AppendLine($"    Collider: {col.GetType().Name}, bounds: {col.bounds}, isTrigger: {col.isTrigger}");
                }
                var fader = child.GetComponent<LPC_RoofFader>();
                if (fader != null)
                {
                    sb.AppendLine($"    WARNING: LPC_RoofFader component is attached to grass child!");
                }
            }
        }
        else
        {
            sb.AppendLine("grass not found");
        }

        // Also search for any other LPC_RoofFader components in the scene!
        var allFaders = GameObject.FindObjectsByType<LPC_RoofFader>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"Found {allFaders.Length} LPC_RoofFader components in the scene:");
        foreach (var fader in allFaders)
        {
            sb.AppendLine($"  On GameObject: {fader.gameObject.name}, path: {GetGameObjectPath(fader.gameObject)}, Pos: {fader.transform.position}");
            var col = fader.GetComponent<Collider2D>();
            if (col != null)
            {
                sb.AppendLine($"    Collider bounds: {col.bounds}, isTrigger: {col.isTrigger}");
            }
        }
        
        return sb.ToString();
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

    public static string DiagnoseSceneColliders()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DIAGNOSE SCENE COLLIDERS ===");
        
        var allColliders = GameObject.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"Total Collider2D found in scene: {allColliders.Length}");
        
        foreach (var col in allColliders)
        {
            var go = col.gameObject;
            sb.AppendLine($"- GO: '{go.name}' | Path: {GetGameObjectPath(go)}");
            sb.AppendLine($"  Pos: {go.transform.position} | Active: {go.activeInHierarchy}");
            sb.AppendLine($"  Type: {col.GetType().Name} | isTrigger: {col.isTrigger} | bounds: {col.bounds}");
            
            // Check components
            var comps = go.GetComponents<Component>();
            List<string> compNames = new List<string>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                compNames.Add(c.GetType().Name);
            }
            sb.AppendLine($"  Components: {string.Join(", ", compNames)}");
        }
        
        return sb.ToString();
    }

    public static string DiagnoseHouseAndGrass()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DIAGNOSE HOUSE AND GRASS ===");
        
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null)
        {
            return "House template not found.";
        }
        
        sb.AppendLine($"House Pos: {house.transform.position}");
        var grid = house.GetComponent<Grid>();
        if (grid == null)
        {
            sb.AppendLine("House has no Grid component.");
        }
        
        var tilemaps = house.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true);
        foreach (var tm in tilemaps)
        {
            tm.CompressBounds();
            var bounds = tm.cellBounds;
            var minWorld = tm.CellToWorld(bounds.min);
            var maxWorld = tm.CellToWorld(bounds.max);
            sb.AppendLine($"- Tilemap: '{tm.name}' | Cell Bounds: {bounds} | World Bounds: min={minWorld}, max={maxWorld}");
        }
        
        var trigger = house.transform.Find("InteriorTrigger");
        if (trigger != null)
        {
            var col = trigger.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                sb.AppendLine($"- InteriorTrigger Bounds: {col.bounds}");
            }
        }
        
        var grass = GameObject.Find("grass");
        if (grass != null)
        {
            sb.AppendLine($"- Grass Pos: {grass.transform.position}");
            for (int i = 0; i < grass.transform.childCount; i++)
            {
                var child = grass.transform.GetChild(i);
                sb.AppendLine($"  Child '{child.name}' Pos: {child.position}");
            }
        }
        
        var player = GameObject.Find("Player2");
        if (player != null)
        {
            sb.AppendLine($"- Player2 Pos: {player.transform.position}");
        }
        
        return sb.ToString();
    }

    public static string AutoFixInteriorTrigger()
    {
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house == null) return "Error: LPC_Tiled_House_Template not found in scene.";
        
        var triggerT = house.transform.Find("InteriorTrigger");
        if (triggerT == null) return "Error: InteriorTrigger child not found under house.";
        
        var floorT = house.transform.Find("Floor");
        if (floorT == null) return "Error: Floor child not found under house.";
        
        var floorTM = floorT.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (floorTM == null) return "Error: Floor has no Tilemap component.";
        
        floorTM.CompressBounds();
        var bounds = floorTM.cellBounds;
        if (bounds.size.x == 0 || bounds.size.y == 0)
        {
            return "Error: Floor tilemap is empty.";
        }
        
        // Calculate center and size in local space
        Vector3 localMin = floorTM.CellToLocal(bounds.min);
        Vector3 localMax = floorTM.CellToLocal(bounds.max);
        
        Vector3 localCenter = (localMin + localMax) / 2f;
        Vector3 localSize = localMax - localMin;
        
        // We set the InteriorTrigger localPosition to localCenter
        // And BoxCollider2D size to localSize
        var col = triggerT.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = triggerT.gameObject.AddComponent<BoxCollider2D>();
        }
        
        Undo.RecordObject(triggerT, "Fix InteriorTrigger Position");
        triggerT.localPosition = localCenter;
        
        Undo.RecordObject(col, "Fix InteriorTrigger Collider");
        col.size = new Vector2(localSize.x, localSize.y);
        col.offset = Vector2.zero;
        col.isTrigger = true;
        
        EditorUtility.SetDirty(triggerT);
        EditorUtility.SetDirty(col);
        
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(house.scene);
        }
        
        string faderRes = FixFaderReferences();
        return $"Success: Aligned InteriorTrigger to Floor bounds. New LocalPosition: {triggerT.localPosition}, Collider Size: {col.size}\nFader setup: {faderRes}";
    }

    public static string FixFaderReferences()
    {
        var fader = GameObject.FindAnyObjectByType<LPC_RoofFader>();
        if (fader == null) return "Error: LPC_RoofFader not found in scene.";
        
        var parent = fader.transform.parent;
        if (parent == null) return "Error: Fader object has no parent.";
        
        var tms = new List<UnityEngine.Tilemaps.Tilemap>();
        var srs = new List<SpriteRenderer>();

        foreach (Transform child in parent)
        {
            string lowerName = child.name.ToLower();
            if (lowerName.Contains("roof") || lowerName.Contains("overhead") || lowerName.Contains("chimney") || lowerName.Contains("ceiling"))
            {
                var tm = child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                if (tm != null) tms.Add(tm);

                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) srs.Add(sr);
            }
        }

        Undo.RecordObject(fader, "Fix Fader References");
        fader.roofTilemap = null;
        fader.roofSpriteRenderer = null;
        fader.roofTilemaps = tms;
        fader.roofSpriteRenderers = srs;
        
        EditorUtility.SetDirty(fader);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fader.gameObject.scene);
        }
        
        return $"Cleared single references and assigned array of {tms.Count} Tilemaps and {srs.Count} SpriteRenderers.";
    }

    public static string CheckPlayerPhysics()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DETAILED PHYSICS DIAGNOSIS ===");
        
        // Check general physics settings
        sb.AppendLine($"EditorApplication.isPlaying: {UnityEditor.EditorApplication.isPlaying}");
        sb.AppendLine($"Physics2D.simulationMode (Legacy/Current): {Physics2D.simulationMode}");
        
        var playerControllers = GameObject.FindObjectsByType<LPCPlayerController2>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"\n[PLAYER CONTROLLERS IN SCENE] Count: {playerControllers.Length}");
        foreach (var pc in playerControllers)
        {
            var go = pc.gameObject;
            sb.AppendLine($"  - Path: {GetGameObjectPath(go)} | Active: {go.activeInHierarchy} | Pos: {go.transform.position}");
        }

        var player = GameObject.Find("Player2");
        Animator playerAnim = null;
        Collider2D playerCol = null;
        Rigidbody2D playerRb = null;
        
        if (player != null)
        {
            sb.AppendLine($"\n[PLAYER] Name: {player.name}, Layer: {player.layer} ({LayerMask.LayerToName(player.layer)}), Active: {player.activeInHierarchy}");
            sb.AppendLine($"Player Position: {player.transform.position}");
            
            sb.AppendLine("Components on Player:");
            foreach (var comp in player.GetComponents<Component>())
            {
                if (comp == null) continue;
                sb.AppendLine($"  - {comp.GetType().Name}");
            }
            
            playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                sb.AppendLine($"Player Rigidbody2D: bodyType={playerRb.bodyType}, simulated={playerRb.simulated}, collisionDetectionMode={playerRb.collisionDetectionMode}, constraints={playerRb.constraints}, mass={playerRb.mass}, isSleeping={playerRb.IsSleeping()}");
            }
            else
            {
                sb.AppendLine("Player has no Rigidbody2D component!");
            }
            
            playerAnim = player.GetComponent<Animator>();
            if (playerAnim != null)
            {
                sb.AppendLine($"Player Animator: applyRootMotion={playerAnim.applyRootMotion}, cullingMode={playerAnim.cullingMode}, hasTransformHierarchy={playerAnim.hasTransformHierarchy}");
            }
            
            var colliders = player.GetComponentsInChildren<Collider2D>(true);
            sb.AppendLine($"Colliders on Player & Children ({colliders.Length} found):");
            foreach (var col in colliders)
            {
                if (col.gameObject == player) playerCol = col;
                string details = "";
                if (col is CapsuleCollider2D cap)
                {
                    details = $", size={cap.size}, offset={cap.offset}, direction={cap.direction}";
                }
                else if (col is BoxCollider2D box)
                {
                    details = $", size={box.size}, offset={box.offset}";
                }
                else if (col is CircleCollider2D circ)
                {
                    details = $", radius={circ.radius}, offset={circ.offset}";
                }
                sb.AppendLine($"  - Path: {GetGameObjectPath(col.gameObject)} | Type: {col.GetType().Name} | isTrigger: {col.isTrigger} | enabled: {col.enabled} | bounds: {col.bounds}{details}");
            }
        }
        else
        {
            sb.AppendLine("\n[PLAYER] Player2 not found in the scene!");
        }
        
        // Scan all colliders in the scene
        var allColliders = GameObject.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"\n[ALL COLLIDERS IN SCENE] Count: {allColliders.Length}");
        foreach (var col in allColliders)
        {
            var go = col.gameObject;
            float dist = player != null ? Vector3.Distance(player.transform.position, go.transform.position) : -1f;
            sb.AppendLine($"  - Path: {GetGameObjectPath(go)}");
            sb.AppendLine($"    Type: {col.GetType().Name} | Layer: {go.layer} ({LayerMask.LayerToName(go.layer)}) | Enabled: {col.enabled} | isTrigger: {col.isTrigger} | usedByComposite: {col.usedByComposite}");
            sb.AppendLine($"    Bounds: {col.bounds} | Dist to Player: {(dist >= 0 ? dist.ToString("F2") : "N/A")}");
            
            if (col.attachedRigidbody != null)
            {
                sb.AppendLine($"    Attached Rigidbody: {GetGameObjectPath(col.attachedRigidbody.gameObject)} (bodyType={col.attachedRigidbody.bodyType}, simulated={col.attachedRigidbody.simulated})");
            }
            else
            {
                sb.AppendLine("    Attached Rigidbody: None (Static)");
            }
            
            if (playerCol != null)
            {
                bool ignoreCol = Physics2D.GetIgnoreCollision(playerCol, col);
                sb.AppendLine($"    Physics2D.GetIgnoreCollision(PlayerCollider, ThisCollider) = {ignoreCol}");
            }
            
            sb.AppendLine("    Components on this object:");
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                sb.AppendLine($"      - {comp.GetType().Name}");
            }
            
            if (player != null)
            {
                bool ignore = Physics2D.GetIgnoreLayerCollision(player.layer, go.layer);
                sb.AppendLine($"    Ignore Collision with Player Layer: {ignore}");
            }
        }
        
        return sb.ToString();
    }

    public static string InspectHouseHierarchy()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== HOUSE HIERARCHY INSPECTOR ===");
        
        var house = GameObject.Find("LPC_Tiled_House_Template");
        if (house != null)
        {
            sb.AppendLine($"House Root: {house.name}");
            sb.AppendLine($"  Active: {house.activeInHierarchy}");
            sb.AppendLine($"  World Position: {house.transform.position}");
            sb.AppendLine($"  Local Position: {house.transform.localPosition}");
            sb.AppendLine($"  Local Scale: {house.transform.localScale}");
            
            sb.AppendLine("\nChildren:");
            for (int i = 0; i < house.transform.childCount; i++)
            {
                var child = house.transform.GetChild(i);
                sb.AppendLine($"  - Child [{i}]: {child.name}");
                sb.AppendLine($"    Active: {child.gameObject.activeSelf}");
                sb.AppendLine($"    Local Position: {child.localPosition}");
                sb.AppendLine($"    World Position: {child.position}");
                sb.AppendLine($"    Local Scale: {child.localScale}");
                
                // Check components
                foreach (var comp in child.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    if (comp is UnityEngine.Tilemaps.Tilemap tm)
                    {
                        tm.CompressBounds();
                        sb.AppendLine($"      - Tilemap: cellBounds={tm.cellBounds}, localBounds={tm.localBounds}");
                    }
                    else if (comp is UnityEngine.Tilemaps.TilemapRenderer tmr)
                    {
                        sb.AppendLine($"      - TilemapRenderer: sortingLayer={tmr.sortingLayerName}, sortingOrder={tmr.sortingOrder}");
                    }
                    else if (comp is SpriteRenderer sr)
                    {
                        sb.AppendLine($"      - SpriteRenderer: sprite={sr.sprite?.name}, sortingLayer={sr.sortingLayerName}, sortingOrder={sr.sortingOrder}");
                    }
                    else if (comp is Collider2D col)
                    {
                        sb.AppendLine($"      - {col.GetType().Name}: enabled={col.enabled}, isTrigger={col.isTrigger}, bounds={col.bounds}");
                    }
                    else
                    {
                        sb.AppendLine($"      - Component: {comp.GetType().Name}");
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("LPC_Tiled_House_Template not found!");
        }
        
        return sb.ToString();
    }

    public static string InspectPlayerControllerFields()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PLAYER CONTROLLER FIELDS ===");
        
        var player = GameObject.Find("Player2");
        if (player != null)
        {
            sb.AppendLine($"player.tag: {player.tag}");
            var pc = player.GetComponent<LPCPlayerController2>();
            if (pc != null)
            {
                sb.AppendLine($"moveSpeed: {pc.moveSpeed}");
                sb.AppendLine($"finalMoveSpeed: {pc.finalMoveSpeed}");
                sb.AppendLine($"runMultiplier: {pc.runMultiplier}");
                sb.AppendLine($"currentHP: {pc.currentHP} / {pc.maxHP}");
                sb.AppendLine($"currentStamina: {pc.currentStamina} / {pc.maxStamina}");
                sb.AppendLine($"player.transform.localScale: {player.transform.localScale}");
                sb.AppendLine($"player.transform.position: {player.transform.position}");
                sb.AppendLine($"player.activeSelf: {player.activeSelf}");
                sb.AppendLine($"isDead: {pc.isDeadState}");
                sb.AppendLine($"isExhausted: {pc.isExhaustedState}");
                
                // Get private fields via reflection
                var type = pc.GetType();
                var fields = new string[] { 
                    "moveInput", "lastDir", "isAttacking", "customLockTimer", "canMoveWhileAttacking", 
                    "originalAnimSpeedAtAttackStart", "attackSpeed", "baseSTR", 
                    "baseDEX", "baseINT", "baseVIT", "baseAGI", "baseLUK",
                    "finalSTR", "finalDEX", "finalINT", "finalVIT", "finalAGI", "finalLUK",
                    "finalATK", "finalMoveSpeed"
                };
                
                sb.AppendLine("\nReflection Fields:");
                foreach (var fieldName in fields)
                {
                    var f = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null)
                    {
                        sb.AppendLine($"  - {fieldName}: {f.GetValue(pc)}");
                    }
                    else
                    {
                        var prop = type.GetProperty(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (prop != null)
                        {
                            sb.AppendLine($"  - {fieldName}: {prop.GetValue(pc)}");
                        }
                        else
                        {
                            sb.AppendLine($"  - {fieldName}: Not Found");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("LPCPlayerController2 component not found on Player2!");
            }
        }
        else
        {
            sb.AppendLine("Player2 not found!");
        }
        
        return sb.ToString();
    }

    public static string StartPlayMode()
    {
        UnityEditor.PlayerSettings.runInBackground = true;
        UnityEditor.EditorApplication.isPlaying = true;
        return "Play mode start triggered (runInBackground set to true).";
    }

    public static string StopPlayMode()
    {
        UnityEditor.EditorApplication.isPlaying = false;
        return "Play mode stop triggered.";
    }

    public static string TestPhysicsOverlap()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PHYSICS OVERLAP TEST ===");
        
        var player = GameObject.Find("Player2");
        var houseColObj = GameObject.Find("/LPC_Tiled_House_Template/Colliders");
        
        if (player == null || houseColObj == null)
        {
            return "Error: Player2 or House Colliders not found in scene.";
        }
        
        var playerCol = player.GetComponent<Collider2D>();
        var houseCol = houseColObj.GetComponent<Collider2D>();
        
        if (playerCol == null || houseCol == null)
        {
            return "Error: Player or House is missing a Collider2D.";
        }
        
        // Move player to the center of the house collider
        var targetPos = houseCol.bounds.center;
        sb.AppendLine($"Teleporting player to house collider center: {targetPos}");
        
        // Record position to restore later
        var originalPos = player.transform.position;
        player.transform.position = targetPos;
        
        // Sync transforms to physics engine
        Physics2D.SyncTransforms();
        
        // Check for overlap using Distance
        var distInfo = playerCol.Distance(houseCol);
        bool isOverlapping = distInfo.isValid && distInfo.distance <= 0f;
        sb.AppendLine($"playerCol.Distance(houseCol).isOverlapping: {isOverlapping} | distance: {distInfo.distance}");
        
        // Get contacts
        var contacts = new List<Collider2D>();
        var filter = new ContactFilter2D();
        filter.useTriggers = true; // Include trigger colliders if any
        int contactCount = playerCol.Overlap(filter, contacts);
        sb.AppendLine($"Player collider contact count: {contactCount}");
        foreach (var contact in contacts)
        {
            sb.AppendLine($"  - Overlapping with: {GetGameObjectPath(contact.gameObject)}");
        }
        
        // Restore player position
        player.transform.position = originalPos;
        Physics2D.SyncTransforms();
        
        return sb.ToString();
    }

    public static string TestMovementPhysics()
    {
        var player = GameObject.Find("Player2");
        if (player == null) return "Error: Player2 not found.";
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return "Error: Player2 has no Rigidbody2D.";
        var pc = player.GetComponent<LPCPlayerController2>();
        if (pc != null)
        {
            pc.enabled = false; // Disable controller to prevent overriding velocity
        }
        
        // Teleport the player to X = -10.0, Y = 1.0f (below the house collider)
        player.transform.position = new Vector3(-10.0f, 1.0f, 0f);
        rb.linearVelocity = new Vector2(0f, 3f); // Move up at 3 units/sec
        Physics2D.SyncTransforms();
        
        return "Disabled controller, teleported player to (-10, 1) and set velocity to (0, 3).";
    }
    
    public static string TestSimulationStep()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PHYSICS SIMULATION STEP TEST ===");
        
        var player = GameObject.Find("Player2");
        var houseColObj = GameObject.Find("/LPC_Tiled_House_Template/Colliders");
        
        if (player == null || houseColObj == null)
        {
            return "Error: Player2 or House Colliders not found in scene.";
        }
        
        var rb = player.GetComponent<Rigidbody2D>();
        var playerCol = player.GetComponent<Collider2D>();
        var houseCol = houseColObj.GetComponent<Collider2D>();
        
        if (rb == null || playerCol == null || houseCol == null)
        {
            return "Error: Missing components on Player or House.";
        }
        
        // Save state
        var originalPos = player.transform.position;
        var originalVel = rb.linearVelocity;
        var originalBodyType = rb.bodyType;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        player.transform.position = new Vector3(-10.01f, 1.0f, 0f); // below the house collider center (-10.01, 5.26)
        rb.linearVelocity = new Vector2(0f, 3f); // Move up at 3 units/sec
        Physics2D.SyncTransforms();
        
        sb.AppendLine($"Start: Pos={player.transform.position}, Vel={rb.linearVelocity}, HouseCenter={houseCol.bounds.center}, HouseExtents={houseCol.bounds.extents}");
        
        // Simulate 50 steps of 0.02s (1.0 second total)
        for (int i = 1; i <= 50; i++)
        {
            Physics2D.Simulate(0.02f);
            sb.AppendLine($"Step {i} (T={i*0.02f:F2}s): Pos={player.transform.position}, Vel={rb.linearVelocity}, Distance={playerCol.Distance(houseCol).distance:F4}");
        }
        
        // Restore state
        player.transform.position = originalPos;
        rb.linearVelocity = originalVel;
        rb.bodyType = originalBodyType;
        Physics2D.SyncTransforms();
        
        return sb.ToString();
    }

    public static string AddCollisionLogger()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== ADDING COLLISION LOGGERS ===");
        
        var player = GameObject.Find("Player2");
        if (player != null)
        {
            if (player.GetComponent<LPC_CollisionLogger>() == null)
            {
                player.AddComponent<LPC_CollisionLogger>();
                sb.AppendLine("Added LPC_CollisionLogger to Player2");
            }
            else
            {
                sb.AppendLine("LPC_CollisionLogger already exists on Player2");
            }
        }
        else
        {
            sb.AppendLine("Player2 not found");
        }
        
        var houseColObj = GameObject.Find("/LPC_Tiled_House_Template/Colliders");
        if (houseColObj != null)
        {
            if (houseColObj.GetComponent<LPC_CollisionLogger>() == null)
            {
                houseColObj.AddComponent<LPC_CollisionLogger>();
                sb.AppendLine("Added LPC_CollisionLogger to House Colliders");
            }
            else
            {
                sb.AppendLine("LPC_CollisionLogger already exists on House Colliders");
            }
        }
        else
        {
            sb.AppendLine("House Colliders not found");
        }

        var houseTriggerObj = GameObject.Find("/LPC_Tiled_House_Template/InteriorTrigger");
        if (houseTriggerObj != null)
        {
            if (houseTriggerObj.GetComponent<LPC_CollisionLogger>() == null)
            {
                houseTriggerObj.AddComponent<LPC_CollisionLogger>();
                sb.AppendLine("Added LPC_CollisionLogger to House InteriorTrigger");
            }
            else
            {
                sb.AppendLine("LPC_CollisionLogger already exists on House InteriorTrigger");
            }
        }
        else
        {
            sb.AppendLine("House InteriorTrigger not found");
        }
        
        return sb.ToString();
    }

    public static string InspectDoors()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DOORS IN SCENE ===");
        var doors = GameObject.FindObjectsByType<LPC_Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"Count: {doors.Length}");
        foreach (var door in doors)
        {
            var go = door.gameObject;
            sb.AppendLine($"Door GameObject: {GetGameObjectPath(go)} | Active: {go.activeInHierarchy}");
            sb.AppendLine($"  isOpen: {door.isOpen} | autoOpen: {door.autoOpen} | playerTag: {door.playerTag}");
            if (door.physicalCollider != null)
            {
                var col = door.physicalCollider;
                sb.AppendLine($"  physicalCollider: {GetGameObjectPath(col.gameObject)} | Type: {col.GetType().Name} | Enabled: {col.enabled} | isTrigger: {col.isTrigger}");
            }
            else
            {
                sb.AppendLine("  physicalCollider: None (Null)");
            }
        }
        return sb.ToString();
    }

    public static string InspectPlayerChildOffsets()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PLAYER CHILD OFFSETS ===");
        var player = GameObject.Find("Player2");
        if (player != null)
        {
            sb.AppendLine($"Parent: {player.name} | position: {player.transform.position} | localPosition: {player.transform.localPosition} | localScale: {player.transform.localScale}");
            
            // Print child offsets
            for (int i = 0; i < player.transform.childCount; i++)
            {
                var child = player.transform.GetChild(i);
                sb.AppendLine($"  - Child [{i}]: {child.name} | localPos: {child.localPosition} | worldPos: {child.position} | localScale: {child.localScale}");
                // If it has children, print them too
                for (int j = 0; j < child.childCount; j++)
                {
                    var grandchild = child.GetChild(j);
                    sb.AppendLine($"    - Grandchild [{j}]: {grandchild.name} | localPos: {grandchild.localPosition} | worldPos: {grandchild.position} | localScale: {grandchild.localScale}");
                }
            }
        }
        else
        {
            sb.AppendLine("Player2 not found!");
        }
        return sb.ToString();
    }

    public static string GetPlayerPositionAndVelocity()
    {
        var player = GameObject.Find("Player2");
        if (player == null) return "Error: Player2 not found.";
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return "Error: No Rigidbody2D on Player2.";
        return $"Pos: {player.transform.position}, Velocity: {rb.linearVelocity}, bodyType: {rb.bodyType}, simulated: {rb.simulated}, isSleeping: {rb.IsSleeping()}, timeScale: {Time.timeScale}, isPaused: {UnityEditor.EditorApplication.isPaused}, isStatic: {player.isStatic}, time: {Time.time}, frameCount: {Time.frameCount}, scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
    }
}


