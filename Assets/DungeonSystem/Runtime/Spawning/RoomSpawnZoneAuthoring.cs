using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Spawning
{
    [DisallowMultipleComponent]
    public class RoomSpawnZoneAuthoring : MonoBehaviour
    {
        [System.Serializable]
        public class LocalSpawnZone
        {
            [Header("Thông Tin Vùng")]
            [Tooltip("Mã ID duy nhất định danh vùng này (Dùng để tối ưu cache nội bộ)")]
            public string zoneId;
            [Tooltip("Tên hiển thị trực quan của vùng")]
            public string zoneName = "Spawn Zone";
            [Tooltip("Hình dạng địa lý của vùng spawn này")]
            public ZoneShapeType shapeType = ZoneShapeType.Rectangle;
            [Tooltip("Màu sắc hiển thị Gizmos vùng này trong Scene View khi vẽ/debug")]
            public Color debugColor = new Color(0f, 1f, 0f, 0.3f);
            
            [Range(0, 100)]
            [Tooltip("Độ ưu tiên của vùng (Vùng có priority cao hơn sẽ tự động chiếm quyền và đè lên vùng thấp hơn khi chồng lấn)")]
            public int priority = 0;
            
            [Range(0f, 1f)]
            [Tooltip("Trọng số khi giải quyết trùng lặp nếu hai vùng chồng lấn có cùng Priority")]
            public float spawnWeight = 1.0f;
            
            [Tooltip("Mật độ vật thể phân bố (Tỉ lệ mong muốn trên mỗi ô đơn vị vuông diện tích)")]
            public float density = 0.5f;

            [Range(0f, 1f)]
            [Tooltip("Tỉ lệ xuất hiện của vùng này khi tạo phòng (0 = 0%, 1 = 100%). Dùng để tăng tính ngẫu nhiên giữa các phòng.")]
            public float zoneSpawnChance = 1.0f;

            [Header("Vật Thể Được Cho Phép")]
            [Tooltip("Danh sách các quy tắc (Spawn Rule) của vật thể được phép mọc bên trong vùng này")]
            public List<SpawnRule> allowedObjects = new List<SpawnRule>();

            [Header("Cấu Hình - Rectangle (Hình Chữ Nhật)")]
            [Tooltip("Tọa độ tâm hình chữ nhật (Tọa độ cục bộ so với góc trái dưới căn phòng)")]
            public Vector2 rectCenter;
            [Tooltip("Kích thước chiều ngang và chiều dọc của hình chữ nhật")]
            public Vector2 rectSize = new Vector2(5f, 5f);

            [Header("Cấu Hình - Circle (Hình Tròn)")]
            [Tooltip("Tọa độ tâm hình tròn (Tọa độ cục bộ so với góc trái dưới căn phòng)")]
            public Vector2 circleCenter;
            [Tooltip("Bán kính mọc vật thể của hình tròn")]
            public float circleRadius = 3f;

            [Header("Cấu Hình - Polygon (Hình Đa Giác)")]
            [Tooltip("Danh sách các đỉnh tạo nên đa giác khép kín (Tọa độ cục bộ phòng)")]
            public List<Vector2> polygonVertices = new List<Vector2>();

            [Header("Cấu Hình - Noise Mask (Mặt Nạ Nhiễu Hạt)")]
            [Tooltip("Tỷ lệ thu phóng của bản đồ nhiễu Perlin Noise")]
            public float noiseScale = 0.1f;
            [Range(0f, 1f)]
            [Tooltip("Ngưỡng nhiễu hạt (Nếu giá trị Perlin tại điểm lớn hơn ngưỡng này thì được phép spawn)")]
            public float noiseThreshold = 0.5f;

            [Header("Cấu Hình - Brush Paint (Dùng Cọ Vẽ)")]
            [Tooltip("Danh sách các tọa độ ô lưới (1x1) đã được tô bằng cọ vẽ trên Scene View")]
            public List<Vector2Int> brushCells = new List<Vector2Int>();

            [System.NonSerialized]
            private HashSet<Vector2Int> cachedBrushSet;

            public HashSet<Vector2Int> GetBrushCellsHashSet()
            {
                if (cachedBrushSet == null)
                {
                    cachedBrushSet = brushCells != null ? new HashSet<Vector2Int>(brushCells) : new HashSet<Vector2Int>();
                }
                return cachedBrushSet;
            }

            public void InvalidateBrushCache()
            {
                cachedBrushSet = null;
            }

            [Header("Bộ Lọc Nhiễu Thưa (Làm Mỏng Vật Thể)")]
            [Tooltip("Bật tính năng dùng tiếng ồn Perlin Noise để tự động loại bỏ bớt một số vật thể ngẫu nhiên")]
            public bool useNoiseModifier = false;
            [Tooltip("Tỉ lệ thu phóng nhiễu của bộ lọc thưa")]
            public float modifierNoiseScale = 0.2f;
            [Range(0f, 1f)]
            [Tooltip("Ngưỡng lọc thưa")]
            public float modifierNoiseThreshold = 0.4f;

            [Header("Lọc Theo Biome")]
            [Tooltip("Chỉ cho phép vùng này hoạt động ở các Biome khớp với Tag này (Để trống = Mọi Biome)")]
            public List<string> biomeTags = new List<string>();

            public LocalSpawnZone()
            {
                zoneId = Guid.NewGuid().ToString();
            }
        }

        [Header("Ghi Chú & Mô Tả")]
        [TextArea(3, 8)]
        [Tooltip("Ghi chú cá nhân mô tả ý đồ thiết kế spawn cho phòng này")]
        public string developerNotes;

        [Header("Cấu Hình Quản Lý")]
        [Tooltip("Kích hoạt tính năng spawn cục bộ cho phòng này")]
        public bool isActive = true;
        [Tooltip("Giới hạn tổng số vật thể tối đa sinh ra trong phòng này")]
        public int maxTotalSpawns = 150;
        [Range(0f, 2f)]
        [Tooltip("Hệ số nhân tỉ lệ spawn toàn cục cho phòng này")]
        public float globalSpawnChanceMultiplier = 1.0f;

        [Header("Danh Sách Các Vùng Spawn")]
        [Tooltip("Danh sách các vùng spawn cục bộ vẽ tay hoặc cấu hình trên căn phòng này")]
        public List<LocalSpawnZone> localZones = new List<LocalSpawnZone>();
    }
}
