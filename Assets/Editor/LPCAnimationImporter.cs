using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// LPC Animation Importer — Row-based spritesheet processor.
/// Optimised for LPC Universal Generator.
///
/// Drives only Body/Ears/Eyes/Underwear via m_Sprite.
/// Equipment layers (Weapon, Armor, etc.) use SpriteLibrary + SpriteResolver.
///
/// DIRECTIONAL WEAPON FIX:
///   Each animation clip now writes m_Enabled curves for the four
///   Weapon/WeaponBehind and Shield/ShieldBehind child objects so that:
///     _Up clips   ? WeaponBehind/ShieldBehind ON,  Weapon/Shield OFF
///     _Down/Left/Right ? Weapon/Shield ON, WeaponBehind/ShieldBehind OFF
///   This makes the weapon correctly appear behind the character when
///   facing away from the camera without changing any sorting orders.
/// </summary>
public class LPCAnimationImporter : EditorWindow
{
    #region Animation Configuration
    [System.Serializable]
    private class AnimationSheet
    {
        public string name;
        public Texture2D texture;
        public int frameCount;
        public bool loop;
        public string description;

        public AnimationSheet(string name, int frameCount, bool loop, string description = "")
        {
            this.name = name;
            this.frameCount = frameCount;
            this.loop = loop;
            this.description = description;
        }
    }

    private List<AnimationSheet> animationSheets = new List<AnimationSheet>();
    #endregion

    #region Settings
    private int spriteWidth = 64;
    private int spriteHeight = 64;
    private int pixelsPerUnit = 64;
    private int frameRate = 8;

    private string outputFolder = "Assets/Animations";
    private string characterName = "Character";
    private FilterMode filterMode = FilterMode.Point;
    private bool generateAnimator = true;
    private bool use8Directional = false;
    private bool autoDetectFrames = false;
    
    public enum PivotMode
    {
        Center,
        BottomCenter,
        Custom
    }
    private PivotMode pivotMode = PivotMode.BottomCenter;
    private Vector2 customPivot = new Vector2(0.5f, 0.05f);

    // Cấu hình chuyển cảnh nâng cao
    private float transitionDuration = 0f;
    private TransitionInterruptionSource interruptionSource = TransitionInterruptionSource.Destination;

    private Vector2 scrollPosition;
    private DefaultAsset folderAsset;
    private bool showSettings = true;
    private bool showAnimations = true;
    private bool showHelp = false;

    // Body-layer child paths driven directly by AnimationClip (m_Sprite).
    // Equipment layers must NOT be listed here.
    private static readonly string[] BodyPaths =
    {
        "Body", "Ears", "Eyes", "Underwear"
    };

    // These pairs are toggled by direction:
    //   _Up  ? Behind=ON, Front=OFF
    //   other ? Front=ON, Behind=OFF
    private static readonly (string front, string behind)[] DirectionalPairs =
    {
        ("Weapon", "WeaponBehind"),
        ("Shield", "ShieldBehind"),
    };
    #endregion

    #region Unity Menu
    [MenuItem("Tools/LPC Animation Importer")]
    public static void ShowWindow()
    {
        var window = GetWindow<LPCAnimationImporter>("LPC Importer");
        window.minSize = new Vector2(560, 420);
        window.InitializeAnimationSheets();
    }
    #endregion

    #region Initialization
    private void InitializeAnimationSheets()
    {
        if (animationSheets.Count > 0) return;

        animationSheets.Add(new AnimationSheet("Walk", 9, true, "9 khung hình  — Chu kỳ đi bộ"));
        animationSheets.Add(new AnimationSheet("Run", 8, true, "8 khung hình  — Chu kỳ chạy"));
        animationSheets.Add(new AnimationSheet("Idle", 2, true, "2 khung hình  — Nhịp thở đứng yên"));
        animationSheets.Add(new AnimationSheet("Slash", 6, false, "6 khung hình  — Tấn công cận chiến (Loại 0)"));
        animationSheets.Add(new AnimationSheet("Thrust", 8, false, "8 khung hình  — Đâm vũ khí        (Loại 1)"));
        animationSheets.Add(new AnimationSheet("Shoot", 13, false, "13 khung hình — Bắn cung          (Loại 2)"));
        animationSheets.Add(new AnimationSheet("Spellcast", 7, false, "7 khung hình  — Niệm phép thuật   (Loại 3)"));
        animationSheets.Add(new AnimationSheet("1h_Slash", 13, false, "13 khung hình — Chém 1 tay        (Loại 10)"));
        animationSheets.Add(new AnimationSheet("1h_Backslash", 13, false, "13 khung hình — Chém ngược 1 tay   (Loại 11)"));
        animationSheets.Add(new AnimationSheet("1h_Halfslash", 6, false, "6 khung hình  — Vung kiếm ngắn     (Loại 12)"));
        animationSheets.Add(new AnimationSheet("Combat", 2, true, "2 khung hình  — Tư thế chiến đấu"));
        animationSheets.Add(new AnimationSheet("Hurt", 6, false, "6 khung hình  — Nhận sát thương"));
        animationSheets.Add(new AnimationSheet("Die", 6, false, "6 khung hình  — Chết gục"));
        animationSheets.Add(new AnimationSheet("Jump", 5, false, "5 khung hình  — Nhảy cao"));
        animationSheets.Add(new AnimationSheet("Sit", 3, true, "3 khung hình  — Ngồi nghỉ"));
        animationSheets.Add(new AnimationSheet("Climb", 6, true, "6 khung hình  — Leo trèo"));
        animationSheets.Add(new AnimationSheet("Emote", 3, false, "3 khung hình  — Biểu cảm nhanh"));
        animationSheets.Add(new AnimationSheet("Watering", 8, false, "8 khung hình  — Tưới cây"));
    }
    #endregion

    #region GUI
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawHeader();
        DrawSettings();
        DrawFolderAssignPanel();
        DrawAnimationSheets();
        DrawImportButton();
        EditorGUILayout.EndScrollView();
    }

    private void DrawFolderAssignPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.12f, 0.65f, 1f) }
        };
        EditorGUILayout.LabelField("📁 Tự Động Gán Nhanh Từ Thư Mục (Drag Folder Here)", headerStyle);
        EditorGUILayout.Space(2);
        
        EditorGUILayout.LabelField("Kéo thả thư mục chứa các file ảnh hoạt ảnh (Walk, Run, Idle...) vào ô dưới đây để gán nhanh:", EditorStyles.miniLabel);
        
        var newFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Thư Mục Ảnh Sprites", folderAsset, typeof(DefaultAsset), false);
            
        if (newFolder != folderAsset)
        {
            folderAsset = newFolder;
            if (folderAsset != null)
            {
                AutoAssignFromFolder(folderAsset);
            }
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void AutoAssignFromFolder(DefaultAsset folder)
    {
        string path = AssetDatabase.GetAssetPath(folder);
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning("[LPCImporter] Vui lòng chọn một thư mục hợp lệ!");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
        int matchCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) continue;

            string filename = Path.GetFileNameWithoutExtension(assetPath).ToLower();

            foreach (var sheet in animationSheets)
            {
                string sheetNameLower = sheet.name.ToLower();
                if (filename == sheetNameLower || 
                    filename.EndsWith("_" + sheetNameLower) || 
                    filename.EndsWith("-" + sheetNameLower) ||
                    filename == sheetNameLower.Replace("1h_", "1h") ||
                    filename.EndsWith("_" + sheetNameLower.Replace("1h_", "1h")) ||
                    filename.EndsWith("-" + sheetNameLower.Replace("1h_", "1h")))
                {
                    sheet.texture = tex;
                    matchCount++;
                    if (autoDetectFrames) AutoDetectFrameCount(sheet);
                    break;
                }
            }
        }

        if (matchCount > 0)
        {
            Debug.Log($"[LPCImporter] Đã tự động gán {matchCount} file ảnh vào các ô hoạt ảnh tương ứng!");
        }
        else
        {
            Debug.LogWarning("[LPCImporter] Không tìm thấy file ảnh nào khớp với tên hoạt ảnh trong thư mục!");
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.15f, 0.6f, 1f) } };
        EditorGUILayout.LabelField("🎞️ LPC Animation Importer", headerStyle);
        EditorGUILayout.Space(5);

        showHelp = EditorGUILayout.Foldout(showHelp, "📖 Hướng dẫn nhanh", true);
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "QUY TRÌNH THỰC HIỆN\n\n" +
                "1. Từ Universal LPC Generator → Bật 'Slice Spritesheet' → Tải file ZIP.\n" +
                "2. Giải nén và import các file ảnh PNG vào Unity (Assets/Sprites/...)\n" +
                "3. Kéo thả từng file PNG vào đúng các ô hoạt ảnh bên dưới.\n" +
                "4. Click 'Nhập Hoạt Ảnh & Tạo Controller' để hoàn thành.\n\n" +
                "KIẾN TRÚC HOẠT ẢNH\n" +
                "  AnimationClips sẽ điều khiển trực tiếp: Body, Ears, Eyes, Underwear (m_Sprite).\n" +
                "  Các lớp trang bị khác sẽ sử dụng cơ chế SpriteLibrary + SpriteResolver để đồng bộ.\n" +
                "  Đừng bao giờ thêm các lớp trang bị vào danh sách BodyPaths để tránh xung đột.\n\n" +
                "ĐỒNG BỘ HIỂN THỊ VŨ KHÍ THEO HƯỚNG\n" +
                "  Hệ thống tự động bật/tắt lớp Weapon/WeaponBehind và Shield/ShieldBehind theo hướng mặt:\n" +
                "  - Khi di chuyển LÊN (_Up) → Vũ khí sau lưng (Behind) sẽ tự động BẬT, mặt trước TẮT.\n" +
                "  - Khi di chuyển hướng khác → Vũ khí mặt trước (Front) sẽ tự động BẬT, sau lưng TẮT.",
                MessageType.Info);
        }
        EditorGUILayout.Space(10);
    }

    private void DrawSettings()
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "⚙️ Thiết lập cấu hình", true);
        if (!showSettings) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        characterName = EditorGUILayout.TextField("Tên Nhân Vật", characterName);
        outputFolder = EditorGUILayout.TextField("Thư Mục Đầu Ra", outputFolder);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        spriteWidth = EditorGUILayout.IntField("Chiều rộng Sprite (W)", spriteWidth);
        spriteHeight = EditorGUILayout.IntField("Chiều cao Sprite (H)", spriteHeight);
        EditorGUILayout.EndHorizontal();

        pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit (PPU)", pixelsPerUnit);
        frameRate = EditorGUILayout.IntSlider("Tốc độ khung hình (Frame Rate)", frameRate, 1, 60);
        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Chế độ lọc (Filter)", filterMode);
        EditorGUILayout.Space(5);
        autoDetectFrames = EditorGUILayout.Toggle("Tự động đếm khung hình", autoDetectFrames);
        generateAnimator = EditorGUILayout.Toggle("Tạo Animator Controller", generateAnimator);
        use8Directional = EditorGUILayout.Toggle("Sử dụng Blend 8 hướng", use8Directional);
        pivotMode = (PivotMode)EditorGUILayout.EnumPopup("Chế độ Pivot (Pivot Mode)", pivotMode);
        if (pivotMode == PivotMode.Custom)
        {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot (X, Y)", customPivot);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("⚡ Cấu hình chuyển tiếp (Transitions)", EditorStyles.boldLabel);
        transitionDuration = EditorGUILayout.Slider("Thời gian chuyển tiếp (Duration)", transitionDuration, 0f, 1f);
        interruptionSource = (TransitionInterruptionSource)EditorGUILayout.EnumPopup("Nguồn ngắt (Interruption)", interruptionSource);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private int activeCategoryTab = 0;
    private static readonly string[] CategoryTabNames = { "Di Chuyển", "Chiến Đấu", "Một Tay (1H)", "Phản Ứng", "Hành Động Khác" };

    private void DrawAnimationSheets()
    {
        showAnimations = EditorGUILayout.Foldout(showAnimations, "🎞️ Danh Sách Hoạt Ảnh (Animation Sheets)", true);
        if (!showAnimations) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("➕ Thêm Tự Chọn", GUILayout.Height(24))) animationSheets.Add(new AnimationSheet("Custom", 1, true, "Hoạt ảnh tự chọn"));
        if (GUILayout.Button("🔍 Tự Phát Hiện Khung Hình", GUILayout.Height(24))) DetectAllFrameCounts();
        if (GUILayout.Button("🔄 Reset Mặc Định", GUILayout.Height(24))) ResetToDefaults();
        if (GUILayout.Button("🧼 Xóa Hết Texture", GUILayout.Height(24))) ClearAllTextures();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        activeCategoryTab = GUILayout.Toolbar(activeCategoryTab, CategoryTabNames, GUILayout.Height(25));
        EditorGUILayout.Space(5);

        switch (activeCategoryTab)
        {
            case 0:
                DrawCategory("Locomotion (Di Chuyển)", 0, 3);
                break;
            case 1:
                DrawCategory("Standard Combat (Chiến Đấu Chuẩn)", 3, 4);
                break;
            case 2:
                DrawCategory("One-Handed (Vũ Khí 1 Tay)", 7, 3);
                break;
            case 3:
                DrawCategory("Reactions (Phản Ứng Va Chạm)", 10, 3);
                break;
            case 4:
                DrawCategory("Other Actions (Hành Động Khác)", 13, 100);
                break;
        }
    }

    private void DrawCategory(string label, int startIndex, int count)
    {
        if (startIndex >= animationSheets.Count) return;
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        for (int i = startIndex; i < startIndex + count && i < animationSheets.Count; i++)
            DrawSlot(i);
        EditorGUI.indentLevel--;
    }

    private void DrawSlot(int index)
    {
        var sheet = animationSheets[index];

        // Background color based on drag state
        if (sheet.texture != null)
            GUI.backgroundColor = new Color(0.12f, 0.32f, 0.15f); // Pastel deep green for assigned spritesheet
        else
            GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = Color.white; // Reset

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(sheet.name, EditorStyles.boldLabel, GUILayout.Width(120));

        var newTex = (Texture2D)EditorGUILayout.ObjectField(
            sheet.texture, typeof(Texture2D), false, GUILayout.Height(50), GUILayout.Width(50));
        if (newTex != sheet.texture)
        {
            sheet.texture = newTex;
            if (autoDetectFrames && newTex != null) AutoDetectFrameCount(sheet);
        }

        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Khung hình:", GUILayout.Width(70));
        sheet.frameCount = EditorGUILayout.IntSlider(sheet.frameCount, 1, 24, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lặp lại:", GUILayout.Width(70));
        sheet.loop = EditorGUILayout.Toggle(sheet.loop, GUILayout.Width(20));
        EditorGUILayout.LabelField(sheet.description, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (sheet.name == "Custom")
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(50)))
            {
                animationSheets.RemoveAt(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();

        if (sheet.texture != null)
        {
            int cols = sheet.texture.width / spriteWidth;
            int rows = sheet.texture.height / spriteHeight;
            GUI.color = (Mathf.Min(sheet.frameCount, cols) == sheet.frameCount) ? Color.green : Color.yellow;
            EditorGUILayout.LabelField(
                $"  Kích thước: {sheet.texture.width}x{sheet.texture.height}  |  Lưới: {cols} cột x {rows} dòng",
                EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void AutoDetectFrameCount(AnimationSheet sheet)
    {
        if (sheet.texture == null) return;
        int cols = sheet.texture.width / spriteWidth;
        if (sheet.frameCount >= cols) sheet.frameCount = cols;
    }

    private void DetectAllFrameCounts()
    {
        foreach (var s in animationSheets)
            if (s.texture != null) s.frameCount = s.texture.width / spriteWidth;
    }

    private void ResetToDefaults()
    {
        animationSheets.Clear();
        InitializeAnimationSheets();
    }

    private void ClearAllTextures()
    {
        foreach (var s in animationSheets) s.texture = null;
    }

    private void DrawImportButton()
    {
        EditorGUILayout.Space(10);
        int assigned = animationSheets.Count(s => s.texture != null);
        EditorGUILayout.HelpBox(
            $"{assigned} sheet(s) assigned.  " +
            "Clips drive: Body / Ears / Eyes / Underwear (m_Sprite) + Weapon/Shield visibility (m_Enabled).",
            assigned > 0 ? MessageType.Info : MessageType.Warning);

        GUI.enabled = assigned > 0;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("Import & Generate", GUILayout.Height(50))) ImportAnimations();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }
    #endregion

    #region Import Logic
    private static void EnsureFolderExists(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;
        folderPath = folderPath.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] folders = folderPath.Split('/');
        if (folders.Length == 0 || folders[0] != "Assets") return;

        string currentPath = "Assets";
        for (int i = 1; i < folders.Length; i++)
        {
            string nextPath = currentPath + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath = nextPath;
        }
    }

    private void ImportAnimations()
    {
        try
        {
            EditorUtility.DisplayProgressBar("LPC Importer", "Initializing...", 0f);

            string animFolder = $"{outputFolder}/{characterName}";
            EnsureFolderExists(animFolder);

            var allAnimations = new Dictionary<string, AnimationClip>();
            int processed = 0;

            foreach (var sheet in animationSheets)
            {
                if (sheet.texture == null) continue;

                EditorUtility.DisplayProgressBar("LPC Importer",
                    $"Processing {sheet.name} ({processed + 1}/{animationSheets.Count})...",
                    (float)processed / animationSheets.Count);

                SliceTexture(sheet);

                string[] directions = { "Up", "Left", "Down", "Right" };
                for (int row = 0; row < 4; row++)
                {
                    string animName = $"{sheet.name}_{directions[row]}";
                    AnimationClip clip = CreateAnimationClip(sheet, row, animName, animFolder);
                    if (clip != null) allAnimations[animName] = clip;
                }
                processed++;
            }

            if (generateAnimator && allAnimations.Count > 0)
                CreateAnimatorController(allAnimations, animFolder);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done",
                $"Imported {allAnimations.Count} animation clips to:\n{animFolder}", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[LPCImporter] {e}");
            EditorUtility.DisplayDialog("Error", e.Message, "OK");
        }
    }

    private void SliceTexture(AnimationSheet sheet)
    {
        string path = AssetDatabase.GetAssetPath(sheet.texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.isReadable = true;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        int cols = sheet.texture.width / spriteWidth;
        int rows = sheet.texture.height / spriteHeight;
        var rects = new List<SpriteMetaData>();

        float pX = 0.5f;
        float pY = 0.5f;
        int align = (int)SpriteAlignment.Center;

        if (pivotMode == PivotMode.BottomCenter)
        {
            pX = 0.5f;
            pY = 0f;
            align = (int)SpriteAlignment.BottomCenter;
        }
        else if (pivotMode == PivotMode.Custom)
        {
            pX = customPivot.x;
            pY = customPivot.y;
            align = (int)SpriteAlignment.Custom;
        }

        for (int row = 0; row < rows; row++)
        {
            int framesToSlice = Mathf.Min(sheet.frameCount, cols);
            for (int col = 0; col < framesToSlice; col++)
            {
                rects.Add(new SpriteMetaData
                {
                    pivot = new Vector2(pX, pY),
                    alignment = align,
                    name = $"{sheet.name}_r{row}_c{col}",
                    rect = new Rect(
                        col * spriteWidth,
                        (rows - row - 1) * spriteHeight,
                        spriteWidth,
                        spriteHeight)
                });
            }
        }

        importer.spritesheet = rects.ToArray();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// Creates an AnimationClip that:
    ///   1. Drives m_Sprite on Body/Ears/Eyes/Underwear (existing behaviour).
    ///   2. Writes m_Enabled on Weapon+Shield front/behind pairs based on direction.
    ///      This is the directional weapon visibility fix — no sorting-order changes needed.
    /// </summary>
    private AnimationClip CreateAnimationClip(AnimationSheet sheet, int row,
                                              string animName, string folder)
    {
        string texPath = AssetDatabase.GetAssetPath(sheet.texture);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath)
            .OfType<Sprite>()
            .Where(s => s.name.Contains($"_r{row}_"))
            .OrderBy(s =>
            {
                int ci = s.name.LastIndexOf("_c");
                return ci >= 0 && int.TryParse(s.name[(ci + 2)..], out int n) ? n : 0;
            })
            .Take(sheet.frameCount)
            .ToList();

        if (sprites.Count == 0) return null;

        AnimationClip clip = new AnimationClip { frameRate = frameRate };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = sheet.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ?? 1. Body layer sprites (unchanged) ????????????????????????????????
        foreach (string bodyPath in BodyPaths)
        {
            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = bodyPath,
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / (float)frameRate,
                    value = sprites[i]
                };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        }

        // ⚔️ 2. Directional weapon / shield visibility ⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️⚔️
        // - Weapon: behind when Up or Left
        // - Shield: behind when Up or Right
        // This makes the weapon/shield correctly appear behind the character
        // when facing away or when the hand holding them is on the far side.
        float clipDuration = (sprites.Count - 1) / (float)frameRate;

        bool isUp = animName.EndsWith("_Up");
        bool isLeft = animName.EndsWith("_Left");
        bool isRight = animName.EndsWith("_Right");

        bool weaponBehind = isUp || isLeft || isRight;
        bool shieldBehind = isUp || isRight;

        SetEnabledCurve(clip, "Weapon", !weaponBehind, clipDuration);
        SetEnabledCurve(clip, "WeaponBehind", weaponBehind, clipDuration);

        SetEnabledCurve(clip, "Shield", !shieldBehind, clipDuration);
        SetEnabledCurve(clip, "ShieldBehind", shieldBehind, clipDuration);

        AssetDatabase.CreateAsset(clip, $"{folder}/{animName}.anim");
        return clip;
    }

    /// <summary>
    /// Writes a constant m_Enabled float curve on the named child's SpriteRenderer.
    /// Unity stores enabled as a float curve (1 = enabled, 0 = disabled).
    /// A constant curve keeps the value locked for the full clip duration,
    /// so blending between states never leaves both layers visible simultaneously.
    /// </summary>
    private static void SetEnabledCurve(AnimationClip clip, string childPath,
                                        bool enabled, float duration)
    {
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = childPath,
            propertyName = "m_Enabled"
        };

        float val = enabled ? 1f : 0f;
        // Use a two-keyframe constant curve so Unity doesn't interpolate the value.
        var curve = new AnimationCurve(
            new Keyframe(0f, val, Mathf.Infinity, Mathf.Infinity),
            new Keyframe(duration, val, Mathf.Infinity, Mathf.Infinity));

        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }
    #endregion

    #region Animator Controller
    private void CreateAnimatorController(Dictionary<string, AnimationClip> animations, string folder)
    {
        string controllerPath = $"{folder}/{characterName}_Controller.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("DirectionX", AnimatorControllerParameterType.Float);
        controller.AddParameter("DirectionY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsCasting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsHurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        // Các tham số trạng thái mở rộng mới
        controller.AddParameter("IsCombat", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsSitting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Emote", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Watering", AnimatorControllerParameterType.Trigger);

        var rootSM = controller.layers[0].stateMachine;
        var statesByName = new Dictionary<string, AnimatorState>();

        var grouped = animations
            .GroupBy(kvp => kvp.Key[..kvp.Key.LastIndexOf('_')])
            .ToDictionary(g => g.Key, g => g.ToDictionary(k => k.Key, v => v.Value));

        foreach (var group in grouped)
            if (group.Value.Count >= 4)
                statesByName[group.Key] = CreateBlendTree(controller, rootSM, group.Key, group.Value);

        SetupTransitions(rootSM, statesByName);
        AssetDatabase.SaveAssets();
    }

    private void SetupTransitions(AnimatorStateMachine sm, Dictionary<string, AnimatorState> states)
    {
        states.TryGetValue("Idle", out AnimatorState idle);
        states.TryGetValue("Walk", out AnimatorState walk);
        states.TryGetValue("Run", out AnimatorState run);

        sm.defaultState = idle ?? walk;

        // Đi bộ <-> Đứng im <-> Chạy
        if (idle != null && walk != null)
        {
            MakeTransition(idle, walk, "Speed", 0.1f, true);
            MakeTransition(walk, idle, "Speed", 0.1f, false);
        }
        if (walk != null && run != null)
        {
            MakeTransition(walk, run, "Speed", 1.2f, true);
            MakeTransition(run, walk, "Speed", 1.2f, false);
        }
        if (idle != null && run != null)
        {
            MakeTransition(idle, run, "Speed", 1.2f, true);
            MakeTransition(run, idle, "Speed", 0.1f, false);
        }

        var returnTargets = new[] { idle, walk, run }.Where(s => s != null).ToList();

        foreach (var kvp in states)
        {
            string name = kvp.Key;
            AnimatorState state = kvp.Value;
            if (name is "Idle" or "Walk" or "Run") continue;

            int attackID = GetAttackTypeIndex(name);

            if (attackID >= 0)
            {
                var entry = sm.AddAnyStateTransition(state);
                entry.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                entry.AddCondition(AnimatorConditionMode.Equals, attackID, "AttackType");
                entry.duration = transitionDuration;
                entry.hasExitTime = false;
                entry.canTransitionToSelf = false;

                foreach (var target in returnTargets)
                {
                    var exit = state.AddTransition(target);
                    exit.hasExitTime = true;
                    exit.exitTime = 1.0f;
                    exit.duration = transitionDuration;
                    exit.interruptionSource = interruptionSource;
                }
            }
            else if (name == "Spellcast")
            {
                var entry = sm.AddAnyStateTransition(state);
                entry.AddCondition(AnimatorConditionMode.If, 0, "IsCasting");
                entry.duration = transitionDuration;
                entry.hasExitTime = false;
                entry.canTransitionToSelf = false;

                foreach (var target in returnTargets)
                {
                    var exit = state.AddTransition(target);
                    exit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCasting");
                    exit.hasExitTime = true;
                    exit.exitTime = 0.8f;
                    exit.duration = transitionDuration;
                    exit.interruptionSource = interruptionSource;
                }
            }
            else if (name == "Hurt")
            {
                var entry = sm.AddAnyStateTransition(state);
                entry.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                entry.duration = transitionDuration;
                entry.hasExitTime = false;
                entry.canTransitionToSelf = false;

                foreach (var target in returnTargets)
                {
                    var exit = state.AddTransition(target);
                    exit.hasExitTime = true;
                    exit.exitTime = 1.0f;
                    exit.duration = transitionDuration;
                    exit.interruptionSource = interruptionSource;
                }
            }
            else if (name == "Die")
            {
                var entry = sm.AddAnyStateTransition(state);
                entry.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                entry.duration = transitionDuration;
                entry.hasExitTime = false;
                entry.canTransitionToSelf = false;
            }
            else if (name == "Combat")
            {
                // Tư thế chiến đấu đứng im
                if (idle != null)
                {
                    var toCombat = idle.AddTransition(state);
                    toCombat.AddCondition(AnimatorConditionMode.If, 0, "IsCombat");
                    toCombat.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
                    toCombat.duration = transitionDuration;
                    toCombat.hasExitTime = false;

                    var fromCombat = state.AddTransition(idle);
                    fromCombat.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCombat");
                    fromCombat.duration = transitionDuration;
                    fromCombat.hasExitTime = false;
                }

                // Nếu di chuyển khi đang ở tư thế chiến đấu -> chuyển nhanh sang đi bộ/chạy
                if (walk != null)
                {
                    var toWalk = state.AddTransition(walk);
                    toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                    toWalk.duration = transitionDuration;
                    toWalk.hasExitTime = false;
                }
                if (run != null)
                {
                    var toRun = state.AddTransition(run);
                    toRun.AddCondition(AnimatorConditionMode.Greater, 1.2f, "Speed");
                    toRun.duration = transitionDuration;
                    toRun.hasExitTime = false;
                }
            }
            else if (name == "Climb")
            {
                var entry = sm.AddAnyStateTransition(state);
                entry.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing");
                entry.duration = transitionDuration;
                entry.hasExitTime = false;
                entry.canTransitionToSelf = false;

                foreach (var target in returnTargets)
                {
                    var exit = state.AddTransition(target);
                    exit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing");
                    exit.hasExitTime = false;
                    exit.duration = transitionDuration;
                    exit.interruptionSource = interruptionSource;
                }
            }
            else if (name == "Sit")
            {
                var entry = sm.AddAnyStateTransition(state);
                entry.AddCondition(AnimatorConditionMode.If, 0, "IsSitting");
                entry.duration = transitionDuration;
                entry.hasExitTime = false;
                entry.canTransitionToSelf = false;

                foreach (var target in returnTargets)
                {
                    var exit = state.AddTransition(target);
                    exit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSitting");
                    exit.hasExitTime = false;
                    exit.duration = transitionDuration;
                    exit.interruptionSource = interruptionSource;
                }
            }
            else if (name is "Jump" or "Emote" or "Watering")
            {
                var entry = sm.AddAnyStateTransition(state);
                entry.AddCondition(AnimatorConditionMode.If, 0, name); // Tên trigger trùng tên hoạt ảnh
                entry.duration = transitionDuration;
                entry.hasExitTime = false;
                entry.canTransitionToSelf = false;

                foreach (var target in returnTargets)
                {
                    var exit = state.AddTransition(target);
                    exit.hasExitTime = true;
                    exit.exitTime = 1.0f;
                    exit.duration = transitionDuration;
                    exit.interruptionSource = interruptionSource;
                }
            }
        }
    }

    private void MakeTransition(AnimatorState from, AnimatorState to,
                                string param, float threshold, bool greater)
    {
        var t = from.AddTransition(to);
        t.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less,
                       threshold, param);
        t.hasExitTime = false;
        t.duration = transitionDuration;
        t.interruptionSource = interruptionSource;
    }

    private int GetAttackTypeIndex(string name)
    {
        if (name.Contains("1h_Backslash")) return 11;
        if (name.Contains("1h_Halfslash")) return 12;
        if (name.Contains("1h_Slash")) return 10;
        if (name.Contains("Slash")) return 0;
        if (name.Contains("Thrust")) return 1;
        if (name.Contains("Shoot")) return 2;
        return -1;
    }

    private AnimatorState CreateBlendTree(AnimatorController controller,
                                          AnimatorStateMachine sm,
                                          string name,
                                          Dictionary<string, AnimationClip> clips)
    {
        var state = sm.AddState(name);
        var tree = new BlendTree
        {
            name = name,
            blendType = use8Directional
                                ? BlendTreeType.FreeformDirectional2D
                                : BlendTreeType.SimpleDirectional2D,
            blendParameter = "DirectionX",
            blendParameterY = "DirectionY"
        };

        foreach (var kvp in clips)
        {
            Vector2 pos = Vector2.zero;
            if (kvp.Key.EndsWith("Up")) pos = new Vector2(0, 1);
            else if (kvp.Key.EndsWith("Down")) pos = new Vector2(0, -1);
            else if (kvp.Key.EndsWith("Left")) pos = new Vector2(-1, 0);
            else if (kvp.Key.EndsWith("Right")) pos = new Vector2(1, 0);
            tree.AddChild(kvp.Value, pos);
        }

        state.motion = tree;
        AssetDatabase.AddObjectToAsset(tree, controller);
        return state;
    }
    #endregion
}