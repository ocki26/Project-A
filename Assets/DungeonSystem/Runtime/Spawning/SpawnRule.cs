using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem.Spawning
{
    [CreateAssetMenu(fileName = "NewSpawnRule", menuName = "Dungeon/Spawning/Spawn Rule")]
    public class SpawnRule : ScriptableObject
    {
        [System.Serializable]
        public struct PrefabVariant
        {
            [Tooltip("Prefab biến thể của vật thể")]
            public GameObject prefab;
            [Range(0.01f, 100f)]
            [Tooltip("Trọng số xuất hiện (Trọng số càng cao so với tổng biến thể thì tỉ lệ ra càng lớn)")]
            public float weight;
        }

        [Header("Ghi Chú & Mô Tả")]
        [TextArea(2, 5)]
        [Tooltip("Ghi chú cá nhân để bạn dễ nhớ mục đích của luật spawn này (ví dụ: Cây thông lớn góc phòng)")]
        public string developerNotes;

        [Header("Liên Kết Prefab")]
        [Tooltip("Prefab chính của vật thể muốn tạo ra (Fallback nếu danh sách biến thể trống)")]
        public GameObject prefab;

        [Tooltip("Danh sách các biến thể prefab khác nhau để spawn ngẫu nhiên thay thế (ví dụ: các loại nấm khác nhau)")]
        public List<PrefabVariant> prefabVariants = new List<PrefabVariant>();
        
        [Tooltip("Trọng số xuất hiện khi chọn ngẫu nhiên giữa các vật thể trong cùng một vùng (Chỉ số càng cao càng dễ ra)")]
        public float spawnWeight = 1.0f;
        
        [Tooltip("Số lượng vật thể tối thiểu muốn thử tạo ra trong vùng này")]
        public int minSpawnCount = 1;
        
        [Tooltip("Số lượng vật thể tối đa muốn thử tạo ra trong vùng này")]
        public int maxSpawnCount = 5;
        
        [Tooltip("Khoảng cách tối thiểu an toàn giữa các vật thể cùng loại này để tránh đè chồng")]
        public float minDistanceBetween = 1.0f;
        
        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ xuất hiện tại điểm hợp lệ (0 = không bao giờ, 1 = 100%)")]
        public float spawnChance = 1.0f;

        [Header("Căn Chỉnh Transform")]
        [Tooltip("Có tự động xoay ngẫu nhiên quanh trục Z (XY top-down) không?")]
        public bool randomRotation = true;
        [Tooltip("Khoảng phóng to/thu nhỏ ngẫu nhiên (X: tỉ lệ nhỏ nhất, Y: tỉ lệ lớn nhất)")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        [Tooltip("Độ lệch độ cao (Bù trừ theo trục Z/Y tùy thuộc hệ tọa độ)")]
        public float heightOffset = 0f;

        /// <summary>
        /// Checks if there is any valid prefab configured in this rule.
        /// </summary>
        public bool HasValidPrefab()
        {
            if (prefab != null) return true;
            if (prefabVariants != null && prefabVariants.Count > 0)
            {
                int count = prefabVariants.Count;
                for (int i = 0; i < count; i++)
                {
                    if (prefabVariants[i].prefab != null) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Rolls and returns a prefab to spawn based on configured variants and their weights.
        /// Falls back to the main prefab if no variants are active.
        /// </summary>
        public GameObject GetPrefabToSpawn(System.Random rng)
        {
            if (prefabVariants != null && prefabVariants.Count > 0)
            {
                float totalWeight = 0f;
                int count = prefabVariants.Count;
                for (int i = 0; i < count; i++)
                {
                    if (prefabVariants[i].prefab != null)
                    {
                        totalWeight += prefabVariants[i].weight;
                    }
                }

                if (totalWeight > 0f)
                {
                    double roll = rng.NextDouble() * totalWeight;
                    float currentWeight = 0f;
                    for (int i = 0; i < count; i++)
                    {
                        if (prefabVariants[i].prefab != null)
                        {
                            currentWeight += prefabVariants[i].weight;
                            if (roll <= currentWeight)
                            {
                                return prefabVariants[i].prefab;
                            }
                        }
                    }
                }
            }
            return prefab;
        }

        [Header("Ràng Buộc Vị Trí & Va Chạm")]
        [Tooltip("Cho phép spawn đè lên các vật thể khác đã spawn trước đó không?")]
        public bool allowOverlap = false;
        [Tooltip("Cho phép spawn sát mép ngoài cùng của căn phòng không?")]
        public bool allowEdgeSpawn = false;
        [Tooltip("Cho phép mọc sát tường phòng không?")]
        public bool allowWallNearSpawn = false;
        
        [Tooltip("Khoảng cách an toàn tối thiểu cách xa tường phòng (Chỉ áp dụng nếu bỏ chọn AllowWallNearSpawn)")]
        public float wallSafetyMargin = 1.0f;
        
        [Tooltip("Khoảng cách an toàn tối thiểu cách xa các cửa phòng để tránh cản đường di chuyển")]
        public float doorSafetyMargin = 2.0f;
    }
}
