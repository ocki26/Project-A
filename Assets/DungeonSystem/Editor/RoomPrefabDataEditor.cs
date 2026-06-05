#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DungeonSystem.Core;
using DungeonSystem.Spawning;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace DungeonSystem.Editor
{
    [CustomEditor(typeof(RoomPrefabData))]
    public class RoomPrefabDataEditor : UnityEditor.Editor
    {
        private RoomPrefabData prefabData;
        private RoomSpawnZoneAuthoring localAuthoring;
        private UnityEditor.Editor authoringEditor;

        // Foldouts
        private static bool showGridSlotsFoldout = false;
        private static bool showCustomShapeFoldout = false;
        private static bool showSpawnConfigFoldout = true;

        // Styles
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle badgeStyle;

        private void OnEnable()
        {
            prefabData = (RoomPrefabData)target;
            UpdateAuthoringReference();
        }

        private void OnDisable()
        {
            CleanUpSubEditor();
        }

        private void CleanUpSubEditor()
        {
            if (authoringEditor != null)
            {
                DestroyImmediate(authoringEditor);
                authoringEditor = null;
            }
        }

        private void UpdateAuthoringReference()
        {
            if (prefabData != null)
            {
                localAuthoring = prefabData.GetComponent<RoomSpawnZoneAuthoring>();
                if (localAuthoring != null)
                {
                    if (authoringEditor == null || authoringEditor.target != localAuthoring)
                    {
                        CleanUpSubEditor();
                        authoringEditor = UnityEditor.Editor.CreateEditor(localAuthoring);
                    }
                }
                else
                {
                    CleanUpSubEditor();
                }
            }
        }

        private void InitializeStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.box);
            headerStyle.padding = new RectOffset(10, 10, 10, 10);
            headerStyle.margin = new RectOffset(0, 0, 5, 5);

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(8, 8, 8, 8);
            boxStyle.margin = new RectOffset(0, 0, 4, 4);

            titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 13;
            titleStyle.normal.textColor = Color.white;

            badgeStyle = new GUIStyle(EditorStyles.miniLabel);
            badgeStyle.normal.textColor = Color.yellow;
            badgeStyle.fontStyle = FontStyle.Bold;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            prefabData = (RoomPrefabData)target;
            UpdateAuthoringReference();
            InitializeStyles();

            // 1. Banner Header
            GUI.backgroundColor = new Color(0.1f, 0.6f, 0.45f);
            GUILayout.BeginVertical(headerStyle);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("🏠 ROOM PREFAB DIAGNOSTICS 🏠", titleStyle);
            EditorGUILayout.LabelField("Cấu hình Thuộc tính Phòng & Quản lý Spawn Zones hợp nhất", EditorStyles.miniLabel);
            GUILayout.Space(5);
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // 2. Personal Notes
            GUI.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
            GUILayout.BeginVertical(boxStyle);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("📝 Ghi chú thiết kế phòng:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string newNotes = EditorGUILayout.TextArea(prefabData.developerNotes, GUILayout.Height(40));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(prefabData, "Change Room Prefab Notes");
                prefabData.developerNotes = newNotes;
                EditorUtility.SetDirty(prefabData);
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 3. Main Attributes
            GUI.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
            GUILayout.BeginVertical(boxStyle);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("🏷️ THÔNG SỐ CƠ BẢN PHÒNG", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            RoomType type = (RoomType)EditorGUILayout.EnumPopup(new GUIContent("Loại Phòng (Type)", "Loại phòng logic trong dungeon"), prefabData.roomType);
            
            GUILayout.BeginHorizontal();
            Vector2Int size = EditorGUILayout.Vector2IntField(new GUIContent("Kích Thước (Size)", "Kích thước thực tế của phòng (ví dụ: 20x20)"), prefabData.roomSize);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("🔍 Auto Fit", GUILayout.Width(75), GUILayout.Height(18)))
            {
                AutoFitRoomSize(prefabData);
                size = prefabData.roomSize; // Update local variable to apply changes
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            DoorMask doors = (DoorMask)EditorGUILayout.EnumFlagsField(new GUIContent("Cửa Cho Phép (Doors)", "Các hướng cửa mà phòng này hỗ trợ"), prefabData.doors);
            float weight = EditorGUILayout.Slider(new GUIContent("Trọng Số Random (Weight)", "Tỉ lệ xuất hiện ngẫu nhiên"), prefabData.weight, 0.1f, 20f);
            bool pivotIsCenter = EditorGUILayout.Toggle(new GUIContent("Pivot Ở Tâm (Center)", "True nếu tâm prefab ở (0.5, 0.5), False nếu ở góc dưới bên trái (0, 0)"), prefabData.pivotIsCenter);
            RoomSpawnPreset spawnPreset = (RoomSpawnPreset)EditorGUILayout.ObjectField(new GUIContent("Global Spawn Preset", "Preset sinh vật thể mặc định cho loại phòng này"), prefabData.spawnPreset, typeof(RoomSpawnPreset), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(prefabData, "Change Room Prefab Core Parameters");
                prefabData.roomType = type;
                prefabData.roomSize = size;
                prefabData.doors = doors;
                prefabData.weight = weight;
                prefabData.pivotIsCenter = pivotIsCenter;
                prefabData.spawnPreset = spawnPreset;
                EditorUtility.SetDirty(prefabData);
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 4. Doors & Walls Slots Foldout
            showGridSlotsFoldout = EditorGUILayout.Foldout(showGridSlotsFoldout, "🚪 DANH SÁCH WALLS & DOORS SLOTS (1x1 & 2x2)", true);
            if (showGridSlotsFoldout)
            {
                GUI.backgroundColor = new Color(0.98f, 0.98f, 0.98f);
                GUILayout.BeginVertical("HelpBox");
                GUI.backgroundColor = Color.white;

                SerializedProperty wTop = serializedObject.FindProperty("wallTop");
                SerializedProperty wBot = serializedObject.FindProperty("wallBottom");
                SerializedProperty wLeft = serializedObject.FindProperty("wallLeft");
                SerializedProperty wRight = serializedObject.FindProperty("wallRight");
                SerializedProperty dTop = serializedObject.FindProperty("doorTop");
                SerializedProperty dBot = serializedObject.FindProperty("doorBottom");
                SerializedProperty dLeft = serializedObject.FindProperty("doorLeft");
                SerializedProperty dRight = serializedObject.FindProperty("doorRight");

                EditorGUILayout.LabelField("⚡ Phòng Tiêu Chuẩn 1x1 Cell:", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(wTop);
                EditorGUILayout.PropertyField(wBot);
                EditorGUILayout.PropertyField(wLeft);
                EditorGUILayout.PropertyField(wRight);
                EditorGUILayout.PropertyField(dTop);
                EditorGUILayout.PropertyField(dBot);
                EditorGUILayout.PropertyField(dLeft);
                EditorGUILayout.PropertyField(dRight);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("⚡ Phòng Lớn 2x2 Cell (8 Cửa):", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallTopLeft"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallTopRight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorTopLeft"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorTopRight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallBottomLeft"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallBottomRight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorBottomLeft"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorBottomRight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallLeftBottom"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallLeftTop"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorLeftBottom"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorLeftTop"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallRightBottom"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallRightTop"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorRightBottom"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorRightTop"));

                GUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // 5. Custom Grid Occupancy
            showCustomShapeFoldout = EditorGUILayout.Foldout(showCustomShapeFoldout, "📐 CẤU HÌNH DẠNG PHÒNG ĐẶC BIỆT (L, T, U SHAPED)", true);
            if (showCustomShapeFoldout)
            {
                GUI.backgroundColor = new Color(0.98f, 0.98f, 0.98f);
                GUILayout.BeginVertical("HelpBox");
                GUI.backgroundColor = Color.white;

                SerializedProperty isCustom = serializedObject.FindProperty("isCustomShape");
                SerializedProperty customGrid = serializedObject.FindProperty("customGridOccupancy");

                EditorGUILayout.PropertyField(isCustom, new GUIContent("Là Dạng Đặc Biệt", "Tick chọn nếu phòng có hình dạng phức tạp không phải hình chữ nhật đứng"));
                if (isCustom.boolValue)
                {
                    EditorGUILayout.PropertyField(customGrid, new GUIContent("Ô Lưới Chiếm Chỗ", "Danh sách độ lệch (offsets) của các ô lưới mà phòng chiếm giữ"), true);
                }

                GUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // 6. SPAWN ZONE INTEGRATION
            showSpawnConfigFoldout = EditorGUILayout.Foldout(showSpawnConfigFoldout, "🛰️ QUẢN LÝ VÙNG SPAWN ZONE (INTEGRATED)", true);
            if (showSpawnConfigFoldout)
            {
                GUI.backgroundColor = new Color(0.92f, 0.96f, 1f);
                GUILayout.BeginVertical("HelpBox");
                GUI.backgroundColor = Color.white;

                if (localAuthoring == null)
                {
                    if (prefabData.spawnPreset == null)
                    {
                        // TH 1: Không có cấu hình nào
                        EditorGUILayout.HelpBox("Phòng này chưa có cấu hình sinh vật thể (Spawn Zone). Bạn có thể chọn cách setup dưới đây:", MessageType.Info);
                        
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("⚡ Vẽ Vùng Cục Bộ (Local)", GUILayout.Height(30)))
                        {
                            Undo.AddComponent<RoomSpawnZoneAuthoring>(prefabData.gameObject);
                            UpdateAuthoringReference();
                        }
                        if (GUILayout.Button("📥 Chọn Preset Toàn Cục", GUILayout.Height(30)))
                        {
                            // Highlight preset field to grab attention
                            EditorGUIUtility.PingObject(prefabData);
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        // TH 2: Sử dụng Preset toàn cục, chưa có Local Override
                        EditorGUILayout.HelpBox($"Phòng đang sử dụng cấu hình Preset toàn cục: '{prefabData.spawnPreset.presetName}'\nPreset chứa {prefabData.spawnPreset.zones.Count} vùng spawn toàn cục.", MessageType.Info);

                        // Draw simple summary of preset zones
                        EditorGUI.indentLevel++;
                        for (int k = 0; k < prefabData.spawnPreset.zones.Count; k++)
                        {
                            var z = prefabData.spawnPreset.zones[k];
                            if (z != null)
                            {
                                EditorGUILayout.LabelField($"• {z.zoneName} ({z.shapeType}) - Priority: {z.priority}, Allowed: {z.allowedObjects.Count}");
                            }
                        }
                        EditorGUI.indentLevel--;

                        EditorGUILayout.Space(5);

                        GUI.backgroundColor = new Color(1f, 0.75f, 0.2f);
                        if (GUILayout.Button("💥 Tách Thành Vùng Cục Bộ Để Vẽ Tay (Override Preset)", GUILayout.Height(35)))
                        {
                            if (EditorUtility.DisplayDialog("Xác nhận tách vùng", 
                                "Bạn có muốn copy toàn bộ danh sách vùng từ Preset toàn cục vào phòng này để vẽ tay cục bộ không? Việc này giúp bạn tinh chỉnh cọ vẽ chính xác cho riêng phòng này.", "Có", "Không"))
                            {
                                RoomSpawnZoneAuthoring newAuth = Undo.AddComponent<RoomSpawnZoneAuthoring>(prefabData.gameObject);
                                newAuth.isActive = true;
                                newAuth.maxTotalSpawns = prefabData.spawnPreset.maxTotalSpawns;
                                newAuth.globalSpawnChanceMultiplier = prefabData.spawnPreset.globalSpawnChanceMultiplier;
                                newAuth.developerNotes = $"Được tách từ preset '{prefabData.spawnPreset.presetName}'\n" + prefabData.spawnPreset.developerNotes;

                                foreach (var pZone in prefabData.spawnPreset.zones)
                                {
                                    if (pZone == null) continue;
                                    RoomSpawnZoneAuthoring.LocalSpawnZone local = new RoomSpawnZoneAuthoring.LocalSpawnZone
                                    {
                                        zoneName = pZone.zoneName,
                                        shapeType = pZone.shapeType,
                                        debugColor = pZone.debugColor,
                                        priority = pZone.priority,
                                        spawnWeight = pZone.spawnWeight,
                                        density = pZone.density,
                                        allowedObjects = new List<SpawnRule>(pZone.allowedObjects),
                                        rectCenter = pZone.rectCenter,
                                        rectSize = pZone.rectSize,
                                        circleCenter = pZone.circleCenter,
                                        circleRadius = pZone.circleRadius,
                                        polygonVertices = new List<Vector2>(pZone.polygonVertices),
                                        noiseScale = pZone.noiseScale,
                                        noiseThreshold = pZone.noiseThreshold,
                                        brushCells = new List<Vector2Int>(pZone.brushCells),
                                        useNoiseModifier = pZone.useNoiseModifier,
                                        modifierNoiseScale = pZone.modifierNoiseScale,
                                        modifierNoiseThreshold = pZone.modifierNoiseThreshold,
                                        biomeTags = new List<string>(pZone.biomeTags)
                                    };
                                    newAuth.localZones.Add(local);
                                }

                                // Clear global preset to avoid double processing or confusion
                                prefabData.spawnPreset = null;
                                EditorUtility.SetDirty(prefabData);
                                EditorUtility.SetDirty(newAuth);
                                UpdateAuthoringReference();
                            }
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
                else
                {
                    // TH 3: Có component local authoring
                    if (!localAuthoring.isActive)
                    {
                        EditorGUILayout.HelpBox("⚠️ Bộ vẽ vùng cục bộ (Local Zones) đang bị TẮT. Hệ thống sẽ tự động fallback về dùng Preset toàn cục nếu có.", MessageType.Warning);
                        if (GUILayout.Button("⚡ Bật Bộ Vẽ Vùng Cục Bộ", GUILayout.Height(28)))
                        {
                            Undo.RecordObject(localAuthoring, "Activate Local Spawner");
                            localAuthoring.isActive = true;
                            EditorUtility.SetDirty(localAuthoring);
                        }
                    }
                    else
                    {
                        // Embed the RoomSpawnZoneAuthoringEditor GUI directly!
                        if (authoringEditor != null)
                        {
                            EditorGUILayout.Space(5);
                            EditorGUILayout.LabelField("🛠️ BẢNG VẼ VÀ ĐIỀU CHỈNH CỤC BỘ", EditorStyles.boldLabel);
                            
                            GUILayout.BeginVertical("box");
                            authoringEditor.OnInspectorGUI();
                            GUILayout.EndVertical();
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Không thể tạo Sub-Editor cho Local Authoring.", MessageType.Error);
                        }

                        EditorGUILayout.Space(10);
                        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                        if (GUILayout.Button("🗑️ Xóa Bộ Vẽ Cục Bộ (Xóa Hoàn Toàn Vùng Đang Vẽ)", GUILayout.Height(25)))
                        {
                            if (EditorUtility.DisplayDialog("Xóa vùng vẽ", 
                                "Bạn có chắc chắn muốn xóa bỏ hoàn toàn bộ vẽ vùng cục bộ trên prefab phòng này? Tất cả các ô vẽ và vùng chỉnh tay sẽ bị mất vĩnh viễn.", "Xóa", "Hủy"))
                            {
                                Undo.DestroyObjectImmediate(localAuthoring);
                                UpdateAuthoringReference();
                            }
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }

                GUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AutoFitRoomSize(RoomPrefabData data)
        {
            var tilemaps = data.GetComponentsInChildren<Tilemap>();
            if (tilemaps == null || tilemaps.Length == 0)
            {
                EditorUtility.DisplayDialog("Auto Fit Room Size", "Không tìm thấy Tilemap nào dưới GameObject này!", "OK");
                return;
            }

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            bool hasTiles = false;

            foreach (var tilemap in tilemaps)
            {
                BoundsInt bounds = tilemap.cellBounds;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (tilemap.HasTile(pos))
                    {
                        hasTiles = true;
                        minX = Mathf.Min(minX, pos.x);
                        maxX = Mathf.Max(maxX, pos.x);
                        minY = Mathf.Min(minY, pos.y);
                        maxY = Mathf.Max(maxY, pos.y);
                    }
                }
            }

            if (!hasTiles)
            {
                EditorUtility.DisplayDialog("Auto Fit Room Size", "Các Tilemap được tìm thấy đều trống rỗng (không có gạch)!", "OK");
                return;
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            Undo.RecordObject(data, "Auto Fit Room Size");
            data.roomSize = new Vector2Int(width, height);
            
            // Auto check pivot alignment
            bool isSymmetricX = Mathf.Abs(minX + maxX) <= 1;
            bool isSymmetricY = Mathf.Abs(minY + maxY) <= 1;
            data.pivotIsCenter = isSymmetricX && isSymmetricY;

            EditorUtility.SetDirty(data);
            SceneView.RepaintAll();
            
            EditorUtility.DisplayDialog("Auto Fit Room Size Thành công", 
                $"Đã tự động căn chỉnh kích thước phòng theo Tilemap:\n\n" +
                $"• Kích thước mới: {width} x {height}\n" +
                $"• Biên Tilemap: X ({minX} đến {maxX}), Y ({minY} đến {maxY})\n" +
                $"• Tự động đặt Pivot Ở Tâm = {data.pivotIsCenter} (Dựa trên tính chất đối xứng)", "OK");
        }
    }
}
#endif
