using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DungeonSystem.Spawning;
using DungeonSystem.Core;
using System;

namespace DungeonSystem.Editor.Spawning
{
    [CustomEditor(typeof(RoomSpawnZoneAuthoring))]
    public class RoomSpawnZoneAuthoringEditor : UnityEditor.Editor
    {
        private int selectedZoneIndex = -1;
        private bool isPaintingMode = false;
        private RoomSpawnPreset presetToImport = null;
        private Dictionary<string, bool> zoneFoldouts = new Dictionary<string, bool>();
        private int brushSize = 1;

        // Styles
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle selectedBoxStyle;
        private GUIStyle titleStyle;

        private void OnEnable()
        {
            // Reset state
            isPaintingMode = false;
        }

        public override void OnInspectorGUI()
        {
            RoomSpawnZoneAuthoring authoring = (RoomSpawnZoneAuthoring)target;
            RoomPrefabData prefabData = authoring.GetComponent<RoomPrefabData>();

            InitializeStyles();

            // 1. Beautiful Header Banner
            GUI.backgroundColor = new Color(0.12f, 0.45f, 0.8f);
            GUILayout.BeginVertical(headerStyle);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("⚡ HYBRID DUNGEON SPAWN ZONE ⚡", titleStyle);
            EditorGUILayout.LabelField("Thiết kế & Vẽ vùng Spawn trực tiếp trên Room Prefab Stage", EditorStyles.miniLabel);
            GUILayout.Space(5);
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // 2. Room Data Diagnostics
            if (prefabData == null)
            {
                EditorGUILayout.HelpBox("⚠️ Không tìm thấy component RoomPrefabData trên GameObject này!\nHãy gắn RoomPrefabData để xác định kích thước phòng và Pivot.", MessageType.Error);
                return;
            }

            // General Config Group
            GUI.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
            GUILayout.BeginVertical("HelpBox");
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("⚙️ CẤU HÌNH PHÒNG CHUNG", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool isActive = EditorGUILayout.Toggle(new GUIContent("Kích Hoạt Vẽ Tay", "Nếu tắt, hệ thống tự động fallback dùng Preset ScriptableObject mặc định"), authoring.isActive);
            int maxTotalSpawns = EditorGUILayout.IntField(new GUIContent("Tổng Spawn Tối Đa", "Số lượng vật thể tối đa được phép sinh ra trong phòng này"), authoring.maxTotalSpawns);
            float globalMult = EditorGUILayout.Slider(new GUIContent("Hệ Số Tỉ Lệ Spawn", "Hệ số nhân tỉ lệ spawn toàn cục cho phòng này"), authoring.globalSpawnChanceMultiplier, 0f, 2f);
            
            EditorGUILayout.LabelField("Mô Tả Ý Đồ Thiết Kế:");
            string notes = EditorGUILayout.TextArea(authoring.developerNotes, GUILayout.Height(50));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(authoring, "Change Room Spawn Authoring Settings");
                authoring.isActive = isActive;
                authoring.maxTotalSpawns = maxTotalSpawns;
                authoring.globalSpawnChanceMultiplier = globalMult;
                authoring.developerNotes = notes;
                EditorUtility.SetDirty(authoring);
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 3. Import preset helper
            GUI.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
            GUILayout.BeginVertical("HelpBox");
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("📥 IMPORT TỪ PRESET TOÀN CỤC", EditorStyles.boldLabel);
            presetToImport = (RoomSpawnPreset)EditorGUILayout.ObjectField(new GUIContent("Preset Mẫu", "Chọn một preset ScriptableObject có sẵn để import các vùng cấu hình"), presetToImport, typeof(RoomSpawnPreset), false);

            if (presetToImport != null)
            {
                if (GUILayout.Button("Import Tất Cả Vùng Từ Preset Này", GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("Import Zones", $"Bạn có chắc chắn muốn thêm {presetToImport.zones.Count} vùng từ preset '{presetToImport.name}' vào phòng cục bộ này?", "Có", "Không"))
                    {
                        Undo.RecordObject(authoring, "Import Zones From Preset");
                        foreach (var pZone in presetToImport.zones)
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
                                zoneSpawnChance = pZone.zoneSpawnChance,
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
                            authoring.localZones.Add(local);
                        }
                        EditorUtility.SetDirty(authoring);
                        presetToImport = null; // Reset
                    }
                }
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 4. Local Spawn Zones List Header
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📍 DANH SÁCH VÙNG SPAWN ({authoring.localZones.Count})", EditorStyles.boldLabel);
            
            if (GUILayout.Button("＋ Thêm Vùng Mới", GUILayout.Width(130)))
            {
                Undo.RecordObject(authoring, "Add Local Spawn Zone");
                RoomSpawnZoneAuthoring.LocalSpawnZone newZone = new RoomSpawnZoneAuthoring.LocalSpawnZone
                {
                    zoneName = "Vùng Mới " + (authoring.localZones.Count + 1),
                    debugColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 0.4f)
                };
                authoring.localZones.Add(newZone);
                selectedZoneIndex = authoring.localZones.Count - 1;
                EditorUtility.SetDirty(authoring);
            }
            GUILayout.EndHorizontal();

            if (authoring.localZones.Count == 0)
            {
                EditorGUILayout.HelpBox("Phòng chưa có vùng spawn cục bộ nào. Bấm 'Thêm Vùng Mới' hoặc Import từ preset mẫu ở trên.", MessageType.Info);
                return;
            }

            // 5. Draw Zones List
            for (int i = 0; i < authoring.localZones.Count; i++)
            {
                RoomSpawnZoneAuthoring.LocalSpawnZone zone = authoring.localZones[i];
                if (zone == null) continue;

                string foldKey = zone.zoneId;
                if (!zoneFoldouts.ContainsKey(foldKey))
                {
                    zoneFoldouts[foldKey] = (i == selectedZoneIndex);
                }

                bool isSelected = (i == selectedZoneIndex);
                GUI.backgroundColor = isSelected ? new Color(0.85f, 0.92f, 1f) : Color.white;
                GUILayout.BeginVertical(isSelected ? selectedBoxStyle : boxStyle);
                GUI.backgroundColor = Color.white;

                // Foldout header bar
                GUILayout.BeginHorizontal();
                
                // Color bar
                Rect colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
                EditorGUI.DrawRect(colorRect, zone.debugColor);
                GUILayout.Space(5);

                zoneFoldouts[foldKey] = EditorGUILayout.Foldout(zoneFoldouts[foldKey], $"{zone.zoneName} ({zone.shapeType})", true, EditorStyles.foldoutHeader);

                GUILayout.FlexibleSpace();

                // Select to edit button
                if (isSelected)
                {
                    GUI.contentColor = Color.green;
                    GUILayout.Label("正在编辑 (Đang chọn)", EditorStyles.miniBoldLabel);
                    GUI.contentColor = Color.white;
                }
                else
                {
                    if (GUILayout.Button("Chọn Vẽ", GUILayout.Width(70)))
                    {
                        selectedZoneIndex = i;
                        zoneFoldouts[foldKey] = true;
                        isPaintingMode = (zone.shapeType == ZoneShapeType.CustomBrush);
                        SceneView.RepaintAll();
                    }
                }

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("Xóa", GUILayout.Width(45)))
                {
                    if (EditorUtility.DisplayDialog("Xóa Vùng", $"Bạn có chắc chắn muốn xóa vùng '{zone.zoneName}'?", "Có", "Không"))
                    {
                        Undo.RecordObject(authoring, "Delete Local Spawn Zone");
                        authoring.localZones.RemoveAt(i);
                        if (selectedZoneIndex == i) selectedZoneIndex = -1;
                        else if (selectedZoneIndex > i) selectedZoneIndex--;
                        EditorUtility.SetDirty(authoring);
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        break;
                    }
                }
                GUI.backgroundColor = Color.white;

                GUILayout.EndHorizontal();

                // Expanded settings
                if (zoneFoldouts[foldKey])
                {
                    GUILayout.Space(5);
                    EditorGUI.indentLevel++;

                    EditorGUI.BeginChangeCheck();

                    string zoneName = EditorGUILayout.TextField(new GUIContent("Tên Vùng", "Tên hiển thị trực quan của vùng spawn này"), zone.zoneName);
                    ZoneShapeType shapeType = (ZoneShapeType)EditorGUILayout.EnumPopup(new GUIContent("Hình Dạng Vùng", "Hình dạng địa lý của vùng (Rectangle, Circle, Polygon, NoiseMask, CustomBrush)"), zone.shapeType);
                    Color debugColor = EditorGUILayout.ColorField(new GUIContent("Màu Sắc Hiển Thị", "Màu sắc hiển thị Gizmos vùng này trong Scene View để phân biệt khi thiết kế"), zone.debugColor);
                    int priority = EditorGUILayout.IntSlider(new GUIContent("Độ Ưu Tiên (Priority)", "Vùng có Priority cao hơn sẽ đè lên và chiếm quyền của vùng thấp hơn khi có diện tích chồng lấn chồng chéo"), zone.priority, 0, 100);
                    float weightVal = EditorGUILayout.Slider(new GUIContent("Trọng Số Giao Thoa", "Nếu hai vùng đè lên nhau có cùng Độ Ưu Tiên, trọng số này sẽ quyết định tỉ lệ ngẫu nhiên vùng nào chiếm chỗ"), zone.spawnWeight, 0f, 1f);
                    float density = EditorGUILayout.FloatField(new GUIContent("Mật Độ (density/ô vuông)", "Mật độ phân bổ vật thể mong muốn trên mỗi ô đơn vị vuông diện tích (vd: 0.1 nghĩa là trung bình 10 ô vuông có 1 vật thể)"), zone.density);
                    float zoneSpawnChance = EditorGUILayout.Slider(new GUIContent("Tỉ Lệ Xuất Hiện Vùng (%)", "Tỉ lệ phần trăm vùng spawn này có cơ hội được sinh ra khi tạo phòng. Đặt dưới 100% để tăng tính ngẫu nhiên độc lạ giữa các phòng."), zone.zoneSpawnChance * 100f, 0f, 100f) / 100f;

                    // Dynamic Fields based on Shape Type
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Cấu hình hình học:", EditorStyles.boldLabel);

                    if (shapeType == ZoneShapeType.Rectangle)
                    {
                        zone.rectCenter = EditorGUILayout.Vector2Field("Tâm (Local)", zone.rectCenter);
                        zone.rectSize = EditorGUILayout.Vector2Field("Kích Thước", zone.rectSize);
                    }
                    else if (shapeType == ZoneShapeType.Circle)
                    {
                        zone.circleCenter = EditorGUILayout.Vector2Field("Tâm (Local)", zone.circleCenter);
                        zone.circleRadius = EditorGUILayout.FloatField("Bán Kính", zone.circleRadius);
                    }
                    else if (shapeType == ZoneShapeType.Polygon)
                    {
                        // Standard list element or show manual vertices
                        EditorGUILayout.LabelField("Vẽ đa giác bằng cách chỉnh các đỉnh cục bộ hoặc dùng Gizmos.");
                        // Custom Vertex editor
                        if (zone.polygonVertices == null) zone.polygonVertices = new List<Vector2>();
                        int vertCount = EditorGUILayout.IntField("Số Lượng Đỉnh", zone.polygonVertices.Count);
                        while (zone.polygonVertices.Count < vertCount) zone.polygonVertices.Add(Vector2.zero);
                        while (zone.polygonVertices.Count > vertCount) zone.polygonVertices.RemoveAt(zone.polygonVertices.Count - 1);
                        for (int v = 0; v < zone.polygonVertices.Count; v++)
                        {
                            zone.polygonVertices[v] = EditorGUILayout.Vector2Field($"Đỉnh {v + 1}", zone.polygonVertices[v]);
                        }
                    }
                    else if (shapeType == ZoneShapeType.NoiseMask)
                    {
                        zone.noiseScale = EditorGUILayout.FloatField("Noise Scale", zone.noiseScale);
                        zone.noiseThreshold = EditorGUILayout.Slider("Ngưỡng Noise", zone.noiseThreshold, 0f, 1f);
                    }
                    else if (shapeType == ZoneShapeType.CustomBrush)
                    {
                        int paintedCount = zone.brushCells != null ? zone.brushCells.Count : 0;
                        EditorGUILayout.LabelField($"Số Ô Đã Vẽ: {paintedCount}", EditorStyles.boldLabel);

                        GUILayout.BeginHorizontal();
                        if (isSelected)
                        {
                            if (isPaintingMode)
                            {
                                if (GUILayout.Button("Thoát Chế Độ Vẽ Cọ (Scene View)", GUILayout.Height(25)))
                                {
                                    isPaintingMode = false;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Vào Chế Độ Vẽ Cọ (Scene View)", GUILayout.Height(25)))
                                {
                                    isPaintingMode = true;
                                }
                            }
                        }
                        else
                        {
                            GUI.enabled = false;
                            GUILayout.Button("Hãy chọn vùng này để Vẽ", GUILayout.Height(25));
                            GUI.enabled = true;
                        }

                        if (GUILayout.Button("Xóa Tất Cả Ô Vẽ", GUILayout.Height(25)))
                        {
                            if (EditorUtility.DisplayDialog("Xóa Ô", "Bạn muốn xóa tất cả ô đã vẽ?", "Có", "Không"))
                            {
                                Undo.RecordObject(authoring, "Clear Brush Cells");
                                zone.brushCells.Clear();
                                EditorUtility.SetDirty(authoring);
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (isSelected && isPaintingMode)
                        {
                            EditorGUILayout.HelpBox("🖌️ CHẾ ĐỘ VẼ ĐANG KÍCH HOẠT:\n- Chuột Trái & Kéo: TÔ màu ô lưới\n- Ctrl + Chuột Trái & Kéo: XÓA ô lưới\nHãy thao tác trên cửa sổ Scene View.", MessageType.Warning);
                        }
                    }

                    // Allowed rules list
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Vật thể được mọc trong vùng này:", EditorStyles.boldLabel);
                    
                    // Render List<SpawnRule> allowedObjects
                    if (zone.allowedObjects == null) zone.allowedObjects = new List<SpawnRule>();
                    int ruleCount = EditorGUILayout.IntField("Số Lượng Loại Vật Thể", zone.allowedObjects.Count);
                    while (zone.allowedObjects.Count < ruleCount) zone.allowedObjects.Add(null);
                    while (zone.allowedObjects.Count > ruleCount) zone.allowedObjects.RemoveAt(zone.allowedObjects.Count - 1);
                    
                    for (int r = 0; r < zone.allowedObjects.Count; r++)
                    {
                        zone.allowedObjects[r] = (SpawnRule)EditorGUILayout.ObjectField($"Loại Vật Thể {r + 1}", zone.allowedObjects[r], typeof(SpawnRule), false);
                    }

                    // Noise modifier filter
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Làm thưa vật thể (Noise Modifier):", EditorStyles.boldLabel);
                    bool useNoise = EditorGUILayout.Toggle(new GUIContent("Sử Dụng Lọc Nhiễu Thưa", "Sử dụng Perlin Noise để tự động loại bỏ bớt vật thể một cách ngẫu nhiên mượt mà, tạo các khoảng thưa tự nhiên"), zone.useNoiseModifier);
                    float modScale = EditorGUILayout.FloatField(new GUIContent("Noise Scale", "Tỷ lệ thu phóng bản đồ nhiễu (Scale càng nhỏ thì các khoảng trống thưa thớt càng lớn và mượt)"), zone.modifierNoiseScale);
                    float modThreshold = EditorGUILayout.Slider(new GUIContent("Ngưỡng Lọc", "Giá trị nhiễu lớn hơn ngưỡng này mới được phép spawn vật thể. Ngưỡng càng cao vật thể càng thưa."), zone.modifierNoiseThreshold, 0f, 1f);

                    // Biome tags
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Lọc Biome:", EditorStyles.boldLabel);
                    if (zone.biomeTags == null) zone.biomeTags = new List<string>();
                    int tagCount = EditorGUILayout.IntField("Số Lượng Biome Tag", zone.biomeTags.Count);
                    while (zone.biomeTags.Count < tagCount) zone.biomeTags.Add("");
                    while (zone.biomeTags.Count > tagCount) zone.biomeTags.RemoveAt(zone.biomeTags.Count - 1);
                    for (int t = 0; t < zone.biomeTags.Count; t++)
                    {
                        zone.biomeTags[t] = EditorGUILayout.TextField($"Tag {t + 1}", zone.biomeTags[t]);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(authoring, "Modify Local Zone Property");
                        zone.zoneName = zoneName;
                        zone.shapeType = shapeType;
                        zone.debugColor = debugColor;
                        zone.priority = priority;
                        zone.spawnWeight = weightVal;
                        zone.density = density;
                        zone.zoneSpawnChance = zoneSpawnChance;
                        zone.useNoiseModifier = useNoise;
                        zone.modifierNoiseScale = modScale;
                        zone.modifierNoiseThreshold = modThreshold;
                        
                        // Sync painting state if shape type changes
                        if (isSelected && shapeType != ZoneShapeType.CustomBrush)
                        {
                            isPaintingMode = false;
                        }

                        EditorUtility.SetDirty(authoring);
                    }

                    EditorGUI.indentLevel--;
                }

                GUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        private List<Vector2Int> GetBrushCells(Vector2Int center, int size)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            if (size == 1)
            {
                cells.Add(center);
            }
            else if (size == 2)
            {
                for (int dx = 0; dx <= 1; dx++)
                {
                    for (int dy = 0; dy <= 1; dy++)
                    {
                        cells.Add(center + new Vector2Int(dx, dy));
                    }
                }
            }
            else if (size == 3)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        cells.Add(center + new Vector2Int(dx, dy));
                    }
                }
            }
            return cells;
        }

        private void DrawSceneHUD(RoomSpawnZoneAuthoring authoring, RoomSpawnZoneAuthoring.LocalSpawnZone selectedZone, Vector2Int roomSize)
        {
            Handles.BeginGUI();
            Rect hudRect = new Rect(10, 10, 240, 210);
            GUI.backgroundColor = new Color(0.12f, 0.15f, 0.2f, 0.9f);
            
            GUILayout.BeginArea(hudRect, "", "Position");
            GUILayout.BeginVertical(boxStyle);
            
            GUI.contentColor = new Color(0.4f, 0.8f, 1f);
            GUILayout.Label("🛠️ BẢNG ĐIỀU KHIỂN CỌ VẼ", titleStyle);
            GUI.contentColor = Color.white;
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            
            GUILayout.Label($"Vùng: <b>{selectedZone.zoneName}</b>", EditorStyles.miniLabel);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cỡ Cọ:", GUILayout.Width(50));
            brushSize = GUILayout.Toolbar(brushSize - 1, new string[] { "1x1", "2x2", "3x3" }) + 1;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            GUILayout.Label("Thao Tác Nhanh:", EditorStyles.miniBoldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tô Đầy (Fill)", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Tô Đầy Vùng", "Bạn có chắc chắn muốn tô đầy toàn bộ căn phòng?", "Tô Đầy", "Hủy"))
                {
                    Undo.RecordObject(authoring, "Fill Brush Cells");
                    if (selectedZone.brushCells == null) selectedZone.brushCells = new List<Vector2Int>();
                    selectedZone.brushCells.Clear();
                    for (int x = 0; x < roomSize.x; x++)
                    {
                        for (int y = 0; y < roomSize.y; y++)
                        {
                            selectedZone.brushCells.Add(new Vector2Int(x, y));
                        }
                    }
                    EditorUtility.SetDirty(authoring);
                }
            }
            if (GUILayout.Button("Đảo (Invert)", GUILayout.Height(22)))
            {
                Undo.RecordObject(authoring, "Invert Brush Cells");
                if (selectedZone.brushCells == null) selectedZone.brushCells = new List<Vector2Int>();
                HashSet<Vector2Int> current = new HashSet<Vector2Int>(selectedZone.brushCells);
                selectedZone.brushCells.Clear();
                for (int x = 0; x < roomSize.x; x++)
                {
                    for (int y = 0; y < roomSize.y; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (!current.Contains(cell))
                        {
                            selectedZone.brushCells.Add(cell);
                        }
                    }
                }
                EditorUtility.SetDirty(authoring);
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Xóa Sạch (Clear)", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Xóa Sạch Vùng", "Bạn muốn xóa toàn bộ ô vẽ?", "Xóa", "Hủy"))
                {
                    Undo.RecordObject(authoring, "Clear Brush Cells");
                    if (selectedZone.brushCells != null) selectedZone.brushCells.Clear();
                    EditorUtility.SetDirty(authoring);
                }
            }
            
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            
            GUILayout.Label("⌨️ Phím Tắt:", EditorStyles.miniBoldLabel);
            GUILayout.Label("- Chuột Trái + Kéo: Vẽ ô", EditorStyles.miniLabel);
            GUILayout.Label("- Ctrl + Chuột Trái + Kéo: Xóa ô", EditorStyles.miniLabel);
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            Handles.EndGUI();
            GUI.backgroundColor = Color.white;
        }

        private void OnSceneGUI()
        {
            RoomSpawnZoneAuthoring authoring = (RoomSpawnZoneAuthoring)target;
            RoomPrefabData prefabData = authoring.GetComponent<RoomPrefabData>();
            if (prefabData == null) return;

            Vector2Int roomSize = prefabData.roomSize;
            
            // Calculate Bottom-Left corner in World Space based on pivotIsCenter
            Vector3 roomBottomLeftInWorld = authoring.transform.position;
            if (prefabData.pivotIsCenter)
            {
                roomBottomLeftInWorld -= new Vector3(roomSize.x * 0.5f, roomSize.y * 0.5f, 0f);
            }

            // 1. Draw Bounding Box & Grid of the Room always
            Handles.color = new Color(1f, 1f, 1f, 0.4f);
            Vector3[] roomCorners = new Vector3[]
            {
                roomBottomLeftInWorld,
                roomBottomLeftInWorld + new Vector3(roomSize.x, 0f, 0f),
                roomBottomLeftInWorld + new Vector3(roomSize.x, roomSize.y, 0f),
                roomBottomLeftInWorld + new Vector3(0f, roomSize.y, 0f)
            };
            Handles.DrawSolidRectangleWithOutline(roomCorners, new Color(1f, 1f, 1f, 0.01f), Color.white);

            // Draw sub-grid lines inside room
            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            for (int x = 1; x < roomSize.x; x++)
            {
                Handles.DrawLine(roomBottomLeftInWorld + new Vector3(x, 0f, 0f), roomBottomLeftInWorld + new Vector3(x, roomSize.y, 0f));
            }
            for (int y = 1; y < roomSize.y; y++)
            {
                Handles.DrawLine(roomBottomLeftInWorld + new Vector3(0f, y, 0f), roomBottomLeftInWorld + new Vector3(roomSize.x, y, 0f));
            }

            // Draw coordinate axis labels (ruler ticks) along bottom and left borders
            GUIStyle coordStyle = new GUIStyle();
            coordStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
            coordStyle.fontSize = 9;
            coordStyle.alignment = TextAnchor.MiddleCenter;

            // X-Axis labels at the bottom edge (y = -0.5)
            int xStep = Mathf.Max(1, roomSize.x / 10);
            for (int x = 0; x < roomSize.x; x += xStep)
            {
                Vector3 pos = roomBottomLeftInWorld + new Vector3(x + 0.5f, -0.5f, 0f);
                Handles.Label(pos, x.ToString(), coordStyle);
            }
            if (roomSize.x > 1 && (roomSize.x - 1) % xStep != 0)
            {
                Vector3 pos = roomBottomLeftInWorld + new Vector3(roomSize.x - 0.5f, -0.5f, 0f);
                Handles.Label(pos, (roomSize.x - 1).ToString(), coordStyle);
            }

            // Y-Axis labels at the left edge (x = -0.6)
            int yStep = Mathf.Max(1, roomSize.y / 10);
            for (int y = 0; y < roomSize.y; y += yStep)
            {
                Vector3 pos = roomBottomLeftInWorld + new Vector3(-0.6f, y + 0.5f, 0f);
                Handles.Label(pos, y.ToString(), coordStyle);
            }
            if (roomSize.y > 1 && (roomSize.y - 1) % yStep != 0)
            {
                Vector3 pos = roomBottomLeftInWorld + new Vector3(-0.6f, roomSize.y - 0.5f, 0f);
                Handles.Label(pos, (roomSize.y - 1).ToString(), coordStyle);
            }


            // 2. Draw all non-selected zones with thinner transparency
            for (int i = 0; i < authoring.localZones.Count; i++)
            {
                if (i == selectedZoneIndex) continue;
                DrawZoneGizmoInScene(roomBottomLeftInWorld, roomSize, authoring.localZones[i], false);
            }

            // 3. Draw and Handle the selected zone with interactive handles
            if (selectedZoneIndex >= 0 && selectedZoneIndex < authoring.localZones.Count)
            {
                RoomSpawnZoneAuthoring.LocalSpawnZone selectedZone = authoring.localZones[selectedZoneIndex];
                DrawZoneGizmoInScene(roomBottomLeftInWorld, roomSize, selectedZone, true);

                // Add interactive placement handles
                if (selectedZone.shapeType == ZoneShapeType.Rectangle)
                {
                    Vector3 worldCenter = roomBottomLeftInWorld + new Vector3(selectedZone.rectCenter.x, selectedZone.rectCenter.y, 0f);
                    
                    // Position handle
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorldCenter = Handles.PositionHandle(worldCenter, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(authoring, "Move Rectangle Zone");
                        selectedZone.rectCenter = (Vector2)(newWorldCenter - roomBottomLeftInWorld);
                        EditorUtility.SetDirty(authoring);
                    }

                    // Dimension slider handle
                    EditorGUI.BeginChangeCheck();
                    Vector3 rightHandle = worldCenter + new Vector3(selectedZone.rectSize.x * 0.5f, 0f, 0f);
                    Vector3 newRight = Handles.Slider(rightHandle, Vector3.right, HandleUtility.GetHandleSize(rightHandle) * 0.12f, Handles.DotHandleCap, 0.1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(authoring, "Resize Rectangle Width");
                        selectedZone.rectSize.x = Mathf.Max(0.5f, (newRight.x - worldCenter.x) * 2f);
                        EditorUtility.SetDirty(authoring);
                    }

                    EditorGUI.BeginChangeCheck();
                    Vector3 topHandle = worldCenter + new Vector3(0f, selectedZone.rectSize.y * 0.5f, 0f);
                    Vector3 newTop = Handles.Slider(topHandle, Vector3.up, HandleUtility.GetHandleSize(topHandle) * 0.12f, Handles.DotHandleCap, 0.1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(authoring, "Resize Rectangle Height");
                        selectedZone.rectSize.y = Mathf.Max(0.5f, (newTop.y - worldCenter.y) * 2f);
                        EditorUtility.SetDirty(authoring);
                    }
                }
                else if (selectedZone.shapeType == ZoneShapeType.Circle)
                {
                    Vector3 worldCenter = roomBottomLeftInWorld + new Vector3(selectedZone.circleCenter.x, selectedZone.circleCenter.y, 0f);

                    // Position handle
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorldCenter = Handles.PositionHandle(worldCenter, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(authoring, "Move Circle Zone");
                        selectedZone.circleCenter = (Vector2)(newWorldCenter - roomBottomLeftInWorld);
                        EditorUtility.SetDirty(authoring);
                    }

                    // Radius slider handle
                    EditorGUI.BeginChangeCheck();
                    Vector3 radiusHandle = worldCenter + new Vector3(selectedZone.circleRadius, 0f, 0f);
                    Vector3 newRadiusHandle = Handles.Slider(radiusHandle, Vector3.right, HandleUtility.GetHandleSize(radiusHandle) * 0.12f, Handles.DotHandleCap, 0.1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(authoring, "Resize Circle Radius");
                        selectedZone.circleRadius = Mathf.Max(0.2f, newRadiusHandle.x - worldCenter.x);
                        EditorUtility.SetDirty(authoring);
                    }
                }
                else if (selectedZone.shapeType == ZoneShapeType.Polygon)
                {
                    if (selectedZone.polygonVertices == null) selectedZone.polygonVertices = new List<Vector2>();

                    // Vertices editing
                    int vertCount = selectedZone.polygonVertices.Count;
                    for (int j = 0; j < vertCount; j++)
                    {
                        Vector3 worldVert = roomBottomLeftInWorld + new Vector3(selectedZone.polygonVertices[j].x, selectedZone.polygonVertices[j].y, 0f);
                        
                        EditorGUI.BeginChangeCheck();
                        Vector3 newWorldVert = Handles.FreeMoveHandle(
                            worldVert, 
                            HandleUtility.GetHandleSize(worldVert) * 0.08f, 
                            Vector3.zero, 
                            Handles.CircleHandleCap
                        );
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(authoring, "Move Polygon Vertex");
                            selectedZone.polygonVertices[j] = (Vector2)(newWorldVert - roomBottomLeftInWorld);
                            EditorUtility.SetDirty(authoring);
                        }
                    }
                }
                else if (selectedZone.shapeType == ZoneShapeType.CustomBrush && isPaintingMode)
                {
                    // Block click-to-deselect control
                    int controlID = GUIUtility.GetControlID(FocusType.Passive);
                    HandleUtility.AddDefaultControl(controlID);

                    // Draw HUD
                    DrawSceneHUD(authoring, selectedZone, roomSize);

                    Event e = Event.current;

                    // Repaint immediately on mouse move to show cursor preview
                    if (e.type == EventType.MouseMove)
                    {
                        SceneView.RepaintAll();
                    }

                    // Project ray to get cell coordinates
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    Plane xyPlane = new Plane(Vector3.forward, roomBottomLeftInWorld);

                    if (xyPlane.Raycast(ray, out float enter))
                    {
                        Vector3 hitPoint = ray.GetPoint(enter);
                        Vector2 localPt = (Vector2)(hitPoint - roomBottomLeftInWorld);
                        Vector2Int cellCoord = new Vector2Int(
                            Mathf.FloorToInt(localPt.x),
                            Mathf.FloorToInt(localPt.y)
                        );

                        // Draw brush preview outline
                        List<Vector2Int> previewCells = GetBrushCells(cellCoord, brushSize);
                        Handles.color = e.control ? new Color(1f, 0.2f, 0.2f, 0.6f) : new Color(0.2f, 1f, 0.2f, 0.6f);
                        foreach (var cell in previewCells)
                        {
                            if (cell.x >= 0 && cell.x < roomSize.x && cell.y >= 0 && cell.y < roomSize.y)
                            {
                                Vector3 cellCenter = roomBottomLeftInWorld + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                                Handles.DrawWireCube(cellCenter, new Vector3(0.95f, 0.95f, 0.05f));
                            }
                        }

                        // Draw coordinate tooltip above the brush cursor
                        if (cellCoord.x >= 0 && cellCoord.x < roomSize.x && cellCoord.y >= 0 && cellCoord.y < roomSize.y)
                        {
                            Vector3 tooltipPos = roomBottomLeftInWorld + new Vector3(cellCoord.x + 0.5f, cellCoord.y + 1.3f, 0f);
                            GUIStyle tooltipStyle = new GUIStyle();
                            tooltipStyle.normal.textColor = Color.yellow;
                            tooltipStyle.fontSize = 10;
                            tooltipStyle.fontStyle = FontStyle.Bold;
                            tooltipStyle.alignment = TextAnchor.MiddleCenter;
                            Handles.Label(tooltipPos, $"({cellCoord.x}, {cellCoord.y})", tooltipStyle);
                        }


                        // Painting input
                        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                        {
                            if (e.button == 0) // Left click
                            {
                                bool isErasing = e.control;
                                bool changed = false;

                                if (selectedZone.brushCells == null) selectedZone.brushCells = new List<Vector2Int>();

                                foreach (var cell in previewCells)
                                {
                                    if (cell.x >= 0 && cell.x < roomSize.x && cell.y >= 0 && cell.y < roomSize.y)
                                    {
                                        if (isErasing)
                                        {
                                            if (selectedZone.brushCells.Contains(cell))
                                            {
                                                if (!changed)
                                                {
                                                    Undo.RecordObject(authoring, "Erase Cells");
                                                    changed = true;
                                                }
                                                selectedZone.brushCells.Remove(cell);
                                            }
                                        }
                                        else
                                        {
                                            if (!selectedZone.brushCells.Contains(cell))
                                            {
                                                if (!changed)
                                                {
                                                    Undo.RecordObject(authoring, "Paint Cells");
                                                    changed = true;
                                                }
                                                selectedZone.brushCells.Add(cell);
                                            }
                                        }
                                    }
                                }

                                if (changed)
                                {
                                    EditorUtility.SetDirty(authoring);
                                    e.Use();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawZoneGizmoInScene(Vector3 roomBottomLeft, Vector2Int roomSize, RoomSpawnZoneAuthoring.LocalSpawnZone zone, bool isFocused)
        {
            float fillAlpha = isFocused ? zone.debugColor.a : zone.debugColor.a * 0.3f;
            float outlineAlpha = isFocused ? 1.0f : 0.4f;

            Color fillColor = new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, fillAlpha * 0.25f);
            Color outlineColor = new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, outlineAlpha);

            switch (zone.shapeType)
            {
                case ZoneShapeType.Rectangle:
                    Vector3 rectCenter = roomBottomLeft + new Vector3(zone.rectCenter.x, zone.rectCenter.y, 0f);
                    
                    Handles.color = fillColor;
                    Handles.DrawSolidRectangleWithOutline(
                        new Vector3[]
                        {
                            rectCenter + new Vector3(-zone.rectSize.x * 0.5f, -zone.rectSize.y * 0.5f, 0f),
                            rectCenter + new Vector3(zone.rectSize.x * 0.5f, -zone.rectSize.y * 0.5f, 0f),
                            rectCenter + new Vector3(zone.rectSize.x * 0.5f, zone.rectSize.y * 0.5f, 0f),
                            rectCenter + new Vector3(-zone.rectSize.x * 0.5f, zone.rectSize.y * 0.5f, 0f)
                        },
                        fillColor,
                        outlineColor
                    );
                    break;

                case ZoneShapeType.Circle:
                    Vector3 circleCenter = roomBottomLeft + new Vector3(zone.circleCenter.x, zone.circleCenter.y, 0f);
                    
                    Handles.color = fillColor;
                    Handles.DrawSolidDisc(circleCenter, Vector3.forward, zone.circleRadius);
                    Handles.color = outlineColor;
                    Handles.DrawWireDisc(circleCenter, Vector3.forward, zone.circleRadius);
                    break;

                case ZoneShapeType.Polygon:
                    if (zone.polygonVertices != null && zone.polygonVertices.Count > 1)
                    {
                        int count = zone.polygonVertices.Count;
                        Vector3[] pts = new Vector3[count];
                        for (int j = 0; j < count; j++)
                        {
                            pts[j] = roomBottomLeft + new Vector3(zone.polygonVertices[j].x, zone.polygonVertices[j].y, 0f);
                        }

                        Handles.color = fillColor;
                        Handles.DrawSolidRectangleWithOutline(pts, fillColor, outlineColor); // Approximate polygon fill in Handles
                    }
                    break;

                case ZoneShapeType.NoiseMask:
                    // Outline room boundaries for Perlin Noise Masks
                    Vector3 boundsCenter = roomBottomLeft + new Vector3(roomSize.x * 0.5f, roomSize.y * 0.5f, 0f);
                    Handles.color = outlineColor;
                    Handles.DrawWireCube(boundsCenter, new Vector3(roomSize.x, roomSize.y, 0f));
                    break;

                case ZoneShapeType.CustomBrush:
                    if (zone.brushCells != null)
                    {
                        int cellCount = zone.brushCells.Count;
                        for (int j = 0; j < cellCount; j++)
                        {
                            Vector2Int cell = zone.brushCells[j];
                            Vector3 cellCenter = roomBottomLeft + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

                            Handles.color = fillColor;
                            Handles.DrawSolidRectangleWithOutline(
                                new Vector3[]
                                {
                                    cellCenter + new Vector3(-0.475f, -0.475f, 0f),
                                    cellCenter + new Vector3(0.475f, -0.475f, 0f),
                                    cellCenter + new Vector3(0.475f, 0.475f, 0f),
                                    cellCenter + new Vector3(-0.475f, 0.475f, 0f)
                                },
                                new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, fillAlpha * 0.4f),
                                outlineColor
                            );
                        }
                    }
                    break;
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
            boxStyle.margin = new RectOffset(2, 2, 4, 4);

            selectedBoxStyle = new GUIStyle(GUI.skin.box);
            selectedBoxStyle.padding = new RectOffset(8, 8, 8, 8);
            selectedBoxStyle.margin = new RectOffset(2, 2, 4, 4);

            titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 13;
            titleStyle.normal.textColor = Color.white;
        }
    }
}
