using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Core;
using DungeonSystem.DebugTools;
using DungeonSystem.Spawners;
using DungeonSystem.Optimization;
using DungeonSystem.Spawning;

namespace DungeonSystem.Generation
{
    [RequireComponent(typeof(DebugVisualizer))]
    [RequireComponent(typeof(SpawnZoneManager))]
    [RequireComponent(typeof(VegetationPooler))]
    public class DungeonGenerator : MonoBehaviour
    {
        public Dictionary<Vector2Int, GameObject> SpawnedRoomsMap { get; } = new Dictionary<Vector2Int, GameObject>();

        [Header("Configuration")]
        public DungeonConfig config;
        
        [Header("Runtime References")]
        public Transform dungeonParent;
        public Transform playerTransform;

        private DungeonLayout layout;
        private SeedManager seedManager;
        private DebugVisualizer visualizer;
        private SpawnZoneManager spawnZoneManager;

        private void Awake()
        {
            visualizer = GetComponent<DebugVisualizer>();
            seedManager = new SeedManager();
            spawnZoneManager = GetComponent<SpawnZoneManager>();
        }

        private void Start()
        {
            if (config != null)
            {
                GenerateDungeon();
            }
            else
            {
                Debug.LogError("Chưa gán DungeonConfig vào DungeonGenerator!");
            }
        }

        public void GenerateDungeon()
        {
            ResetGenerator();

            // 1. Quản lý Seed độc lập
            seedManager.Initialize(config.seed, config.useRandomSeed);
            System.Random rng = seedManager.RNG;

            // 2. Sinh sơ đồ logic đồ thị
            RoomGenerator roomGen = new RoomGenerator(config, rng);
            layout = roomGen.GenerateLayout();

            // 3. Xử lý hành lang
            CorridorGenerator corridorGen = new CorridorGenerator();
            corridorGen.ConnectCorridors(layout);

            // 4. Khởi tạo prefab và tự động căn chỉnh điểm tựa (Pivot)
            SpawnRoomPrefabs(rng);

            // Dịch chuyển Player về phòng xuất phát (tọa độ 0,0) trước khi spawn vật thể và kích hoạt culling
            TeleportPlayerToStartRoom();

            // 4b. Sinh đối tượng theo Spawn Zones (Dựa trên Seed, vị trí phòng và preset)
            if (spawnZoneManager != null)
            {
                spawnZoneManager.GenerateObjects(layout, rng, SpawnedRoomsMap, config.cellSize);
            }

            // 5. Triển khai Spawners ngoài
            EnemySpawner enemySpawner = GetComponent<EnemySpawner>();
            if (enemySpawner != null) enemySpawner.SpawnEnemies(layout, rng);

            LootSpawner lootSpawner = GetComponent<LootSpawner>();
            if (lootSpawner != null) lootSpawner.SpawnLoot(layout, rng);

            // 6. Kích hoạt Culling tối ưu hóa hiệu năng render
            DungeonCullingManager cullingManager = GetComponent<DungeonCullingManager>();
            if (cullingManager != null)
            {
                cullingManager.InitializeCulling();
            }
        }

        private void ResetGenerator()
        {
            // Thu hồi toàn bộ đối tượng đang hoạt động về Pool trước khi hủy phòng
            if (spawnZoneManager != null)
            {
                spawnZoneManager.ClearObjects();
            }

            if (dungeonParent != null)
            {
                for (int i = dungeonParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(dungeonParent.GetChild(i).gameObject);
                }
            }
            
            if (visualizer != null) visualizer.Clear();
            SpawnedRoomsMap.Clear(); 
        }

        private void SpawnRoomPrefabs(System.Random rng)
        {
            List<Bounds> boundsList = new List<Bounds>();

            foreach (var node in layout.Rooms)
            {
                List<RoomPrefabData> candidates = config.GetPrefabsByType(node.type);
                List<RoomPrefabData> matchingPrefabs = new List<RoomPrefabData>();

                foreach (var prefab in candidates)
                {
                    Vector2Int prefabGridSize = prefab.GetGridSize(config.cellSize);

                    if (prefabGridSize.x > 1 || prefabGridSize.y > 1)
                    {
                        bool overlapConflict = false;
                        for (int x = 0; x < prefabGridSize.x; x++)
                        {
                            for (int y = 0; y < prefabGridSize.y; y++)
                            {
                                if (x == 0 && y == 0) continue;
                                Vector2Int cellToCheck = node.gridPos + new Vector2Int(x, y);
                                if (layout.CellToRoomMap.TryGetValue(cellToCheck, out RoomNode occupyingRoom))
                                {
                                    if (occupyingRoom != node)
                                    {
                                        overlapConflict = true;
                                        break;
                                    }
                                }
                            }
                            if (overlapConflict) break;
                        }
                        if (overlapConflict) continue;
                    }

                    if ((prefab.doors & node.requiredDoors) == node.requiredDoors)
                    {
                        matchingPrefabs.Add(prefab);
                    }
                }

                if (matchingPrefabs.Count == 0)
                {
                    foreach (var prefab in config.normalRooms)
                    {
                        if ((prefab.doors & node.requiredDoors) == node.requiredDoors && prefab.GetGridSize(config.cellSize) == Vector2Int.one)
                        {
                            matchingPrefabs.Add(prefab);
                        }
                    }
                }

                if (matchingPrefabs.Count > 0)
                {
                    RoomPrefabData chosen = WeightedRandomSelector.Select(matchingPrefabs, p => p.weight, rng);
                    node.assignedPrefab = chosen;
                    node.gridSize = chosen.GetGridSize(config.cellSize);

                    layout.RegisterOccupancy(node, config.cellSize);

                    // Điểm góc dưới bên trái của ô lưới
                    Vector3 bottomLeftPos = new Vector3(node.gridPos.x * config.cellSize, node.gridPos.y * config.cellSize, 0f);
                    
                    // Khoảng dịch đến tâm dựa trên kích thước thực của prefab
                    Vector3 roomCenterOffset = new Vector3(chosen.roomSize.x * 0.5f, chosen.roomSize.y * 0.5f, 0f);
                    
                    // Căn chỉnh vị trí Spawn dựa theo thiết lập Pivot
                    Vector3 spawnPos = chosen.pivotIsCenter ? (bottomLeftPos + roomCenterOffset) : bottomLeftPos;
                    
                    GameObject spawnedObj = Instantiate(chosen.gameObject, spawnPos, Quaternion.identity, dungeonParent);
                    spawnedObj.name = $"{node.type}_Room_{node.gridPos.x}_{node.gridPos.y}";

                    for (int x = 0; x < node.gridSize.x; x++)
                    {
                        for (int y = 0; y < node.gridSize.y; y++)
                        {
                            Vector2Int cellCoord = node.gridPos + new Vector2Int(x, y);
                            SpawnedRoomsMap[cellCoord] = spawnedObj;
                        }
                    }

                    RoomPrefabData spawnedRoomData = spawnedObj.GetComponent<RoomPrefabData>();
                    if (spawnedRoomData != null)
                    {
                        // Gọi hàm cấu hình mở rộng mới, tự động giải quyết 4 cửa hoặc 8 cửa
                        spawnedRoomData.ConfigureDoorsAndWallsExtended(node, layout, config.cellSize);
                    }

                    // TỰ ĐỘNG GÁN COLLIDER PHÒNG CHO QUÁI VẬT CON BẰNG RUNTIME REFLECTION (Khử Spawning Lag & Sửa lỗi CS0246 do Assembly)
                    System.Type roomBoundsType = System.Type.GetType("RoomBounds, Assembly-CSharp");
                    if (roomBoundsType != null)
                    {
                        Component roomBounds = spawnedObj.GetComponentInChildren(roomBoundsType);
                        if (roomBounds != null)
                        {
                            Collider2D roomCol = roomBounds.GetComponent<Collider2D>();
                            if (roomCol != null)
                            {
                                System.Type enemyAiType = System.Type.GetType("EnemyAI, Assembly-CSharp");
                                if (enemyAiType != null)
                                {
                                    Component[] enemies = spawnedObj.GetComponentsInChildren(enemyAiType, true);
                                    if (enemies != null && enemies.Length > 0)
                                    {
                                        var setRoomColliderMethod = enemyAiType.GetMethod("SetRoomCollider", new System.Type[] { typeof(Collider2D) });
                                        if (setRoomColliderMethod != null)
                                        {
                                            object[] parameters = new object[] { roomCol };
                                            foreach (Component enemy in enemies)
                                            {
                                                if (enemy != null)
                                                {
                                                    setRoomColliderMethod.Invoke(enemy, parameters);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // TỰ ĐỘNG SINH KHOÁNG SẢN, THẢO MỘC, PHÒNG SHOP VÀ PHÒNG SECRET BẰNG REFLECTION ĐỘNG
                    PopulateRoomInteractiveElements(spawnedObj, node);

                    // Đồng bộ vùng bao Debug phẳng theo tọa độ thực tế
                    Bounds b = new Bounds(
                        bottomLeftPos + roomCenterOffset,
                        new Vector3(chosen.roomSize.x, chosen.roomSize.y, 0.1f)
                    );
                    boundsList.Add(b);
                }
                else
                {
                    Debug.LogWarning($"Không tìm thấy prefab tương thích cho node {node.type} tại {node.gridPos} với door mask: {node.requiredDoors}");
                }
            }

            if (visualizer != null)
            {
                visualizer.SetBounds(boundsList);
            }
        }

        private void TeleportPlayerToStartRoom()
        {
            if (playerTransform == null)
            {
                // Tự động tìm kiếm Player qua tag nếu chưa gán trong Inspector
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[DungeonGenerator] Không tìm thấy playerTransform để thực hiện dịch chuyển xuất phát!");
                return;
            }

            // Dò tìm phòng xuất phát tại tọa độ (0,0) trong bản đồ đã sinh
            if (SpawnedRoomsMap.TryGetValue(Vector2Int.zero, out GameObject startRoomObj))
            {
                if (startRoomObj != null)
                {
                    // 1. Thử tìm kiếm component LPCPlayerSpawnPoint trong các Object con của phòng xuất phát
                    LPCPlayerSpawnPoint spawnPoint = startRoomObj.GetComponentInChildren<LPCPlayerSpawnPoint>();
                    if (spawnPoint != null)
                    {
                        playerTransform.position = spawnPoint.transform.position;
                        Debug.Log($"[Dungeon] Teleport thành công Player đến vị trí Spawn Point: {spawnPoint.transform.position}");
                        return;
                    }

                    // 2. Fallback: Nếu phòng xuất phát không có Spawn Point, dịch chuyển Player về tâm phòng xuất phát
                    RoomPrefabData startRoomData = startRoomObj.GetComponent<RoomPrefabData>();
                    if (startRoomData != null)
                    {
                        Vector3 bottomLeftPos = Vector3.zero;
                        Vector3 roomCenterOffset = new Vector3(startRoomData.roomSize.x * 0.5f, startRoomData.roomSize.y * 0.5f, 0f);
                        Vector3 fallbackPos = bottomLeftPos + roomCenterOffset;

                        playerTransform.position = fallbackPos;
                        Debug.LogWarning($"[Dungeon] Không tìm thấy LPCPlayerSpawnPoint trong phòng xuất phát. Teleport Player về tâm phòng: {fallbackPos}");
                    }
                }
            }
        }

        private void PopulateRoomInteractiveElements(GameObject roomObj, RoomNode node)
        {
            // Lấy kiểu dữ liệu bằng Reflection để tránh CS0246 và đảm bảo biên dịch 100% độc lập
            System.Type harvestableType = System.Type.GetType("LPC_Harvestable, Assembly-CSharp");
            System.Type itemDataType = System.Type.GetType("LPCItemData, Assembly-CSharp");
            if (harvestableType == null || itemDataType == null) return;

            // Dùng vị trí phòng làm Seed để sinh vật ngẫu nhiên đồng bộ
            System.Random rng = new System.Random(node.gridPos.x * 37 + node.gridPos.y * 101);

            // 1. Phân phối tài nguyên cho PHÒNG THƯỜNG (Normal Room)
            if (node.type == RoomType.Normal)
            {
                // Sinh ngẫu nhiên từ 1 đến 3 cụm tài nguyên (quặng/thảo mộc)
                int numSpawns = rng.Next(1, 4);
                for (int i = 0; i < numSpawns; i++)
                {
                    // Chọn vị trí ngẫu nhiên xung quanh tâm phòng
                    float rx = (float)(rng.NextDouble() * 7.0 - 3.5);
                    float ry = (float)(rng.NextDouble() * 7.0 - 3.5);
                    Vector3 localPos = new Vector3(rx, ry, 0f);

                    GameObject resObj = new GameObject($"Harvestable_{node.gridPos.x}_{node.gridPos.y}_{i}");
                    resObj.transform.SetParent(roomObj.transform, false);
                    resObj.transform.localPosition = localPos;

                    // Thêm Component bằng Reflection
                    Component harvestable = resObj.AddComponent(harvestableType);

                    // Chọn loại tài nguyên ngẫu nhiên (từ CopperOre đến WildCrop)
                    int typeVal = rng.Next(0, 7); 
                    harvestableType.GetField("resourceType").SetValue(harvestable, typeVal);

                    // Tạo và gán LPCItemData ScriptableObject bằng Reflection
                    string itemName = typeVal switch
                    {
                        0 => "Copper Ore",
                        1 => "Iron Ore",
                        2 => "Gold Ore",
                        3 => "Stone",
                        4 => "Wild Mushroom",
                        5 => "Medicinal Herb",
                        _ => "Wild Crop"
                    };

                    int itemCatVal = typeVal switch
                    {
                        0 or 1 or 2 or 3 => 4, // LPCItemCategory.Material = 4
                        4 or 5 => 5,           // LPCItemCategory.Consumable = 5
                        _ => 3                 // LPCItemCategory.Crop = 3
                    };

                    // Gọi ScriptableObject.CreateInstance
                    ScriptableObject itemData = ScriptableObject.CreateInstance("LPCItemData");
                    if (itemData != null)
                    {
                        itemDataType.GetField("itemName").SetValue(itemData, itemName);
                        
                        // Gán Category
                        System.Type lpcCatType = System.Type.GetType("LPCItemCategory, Assembly-CSharp");
                        if (lpcCatType != null)
                        {
                            var catEnumVal = System.Enum.ToObject(lpcCatType, itemCatVal);
                            itemDataType.GetField("itemCategory").SetValue(itemData, catEnumVal);
                        }

                        // Gán lootItem cho Harvestable
                        harvestableType.GetField("lootItem").SetValue(harvestable, itemData);
                    }

                    // Tự động gán Physics Layer là "enemy" để người chơi chém trúng quét được
                    int enemyLayer = LayerMask.NameToLayer("enemy");
                    if (enemyLayer < 0) enemyLayer = LayerMask.NameToLayer("Enemy");
                    if (enemyLayer >= 0)
                    {
                        resObj.layer = enemyLayer;
                    }
                }
            }
            // 2. Phân phối tài nguyên và hạt giống cho PHÒNG SHOP (Shop Room)
            else if (node.type == RoomType.Shop)
            {
                // Sinh 3 bục mua bán/trưng bày hạt giống và quặng
                Vector3[] pedestalOffsets = new Vector3[] { new Vector3(-2f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(2f, 0f, 0f) };
                string[] shopItems = new string[] { "Radish Seed", "Corn Seed", "Iron Ore" };
                int[] shopItemCats = new int[] { 2, 2, 4 }; // Seed = 2, Material = 4

                for (int i = 0; i < 3; i++)
                {
                    GameObject pedObj = new GameObject($"Shop_Pedestal_{i}");
                    pedObj.transform.SetParent(roomObj.transform, false);
                    pedObj.transform.localPosition = pedestalOffsets[i];

                    // Tạo Sprite hiển thị vật phẩm nằm trên bục (dạng loot pickup)
                    System.Type pickupType = System.Type.GetType("LPC_ItemPickup, Assembly-CSharp");
                    if (pickupType != null)
                    {
                        Component pickup = pedObj.AddComponent(pickupType);
                        
                        // Tạo LPCItemData ScriptableObject
                        ScriptableObject itemData = ScriptableObject.CreateInstance("LPCItemData");
                        if (itemData != null)
                        {
                            itemDataType.GetField("itemName").SetValue(itemData, shopItems[i]);
                            
                            System.Type lpcCatType = System.Type.GetType("LPCItemCategory, Assembly-CSharp");
                            if (lpcCatType != null)
                            {
                                var catEnumVal = System.Enum.ToObject(lpcCatType, shopItemCats[i]);
                                itemDataType.GetField("itemCategory").SetValue(itemData, catEnumVal);
                            }

                            pickupType.GetField("itemData").SetValue(pickup, itemData);
                            pickupType.GetField("count").SetValue(pickup, rng.Next(1, 4));
                        }
                    }
                }
            }
            // 3. Phân phối khoáng sản cực kỳ quý hiếm cho PHÒNG BÍ MẬT (Secret Room)
            else if (node.type == RoomType.Secret)
            {
                // Sinh 2 cụm quặng Vàng và 1 Rương siêu to khổng lồ
                Vector3[] secretOffsets = new Vector3[] { new Vector3(-1.5f, 0.5f, 0f), new Vector3(1.5f, 0.5f, 0f) };
                for (int i = 0; i < 2; i++)
                {
                    GameObject resObj = new GameObject($"Secret_GoldOre_{i}");
                    resObj.transform.SetParent(roomObj.transform, false);
                    resObj.transform.localPosition = secretOffsets[i];

                    Component harvestable = resObj.AddComponent(harvestableType);

                    // 2 = ResourceType.GoldOre
                    harvestableType.GetField("resourceType").SetValue(harvestable, 2);
                    harvestableType.GetField("maxHits").SetValue(harvestable, 5); // Cần đập 5 phát vì quặng vàng rất cứng!

                    ScriptableObject itemData = ScriptableObject.CreateInstance("LPCItemData");
                    if (itemData != null)
                    {
                        itemDataType.GetField("itemName").SetValue(itemData, "Gold Ore");
                        
                        System.Type lpcCatType = System.Type.GetType("LPCItemCategory, Assembly-CSharp");
                        if (lpcCatType != null)
                        {
                            var catEnumVal = System.Enum.ToObject(lpcCatType, 4); // Material = 4
                            itemDataType.GetField("itemCategory").SetValue(itemData, catEnumVal);
                        }

                        harvestableType.GetField("lootItem").SetValue(harvestable, itemData);
                    }

                    int enemyLayer = LayerMask.NameToLayer("enemy");
                    if (enemyLayer < 0) enemyLayer = LayerMask.NameToLayer("Enemy");
                    if (enemyLayer >= 0) resObj.layer = enemyLayer;
                }
            }
        }
    }
}
