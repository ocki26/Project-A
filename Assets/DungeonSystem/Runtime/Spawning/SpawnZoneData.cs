using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Spawning
{
    public enum ZoneShapeType
    {
        Rectangle,
        Circle,
        Polygon,
        NoiseMask,
        CustomBrush
    }

    [CreateAssetMenu(fileName = "NewSpawnZoneData", menuName = "Dungeon/Spawning/Spawn Zone Data")]
    public class SpawnZoneData : ScriptableObject
    {
        [Header("Ghi Chú & Mô Tả")]
        [TextArea(2, 5)]
        [Tooltip("Ghi chú cá nhân mô tả về vùng spawn này (ví dụ: Khu mọc cây bụi rậm quanh phòng)")]
        public string developerNotes;

        [Header("Thông Tin Cơ Bản Vùng")]
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
        [Tooltip("Trọng số khi giải quyết trùng lặp nếu hai vùng chồng lấn có cùng Priority (Chỉ số càng cao vùng đó càng chiếm thế thượng phong)")]
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
        [Tooltip("Tỷ lệ thu phóng của bản đồ nhiễu Perlin Noise (Scale càng nhỏ độ thưa thớt càng lớn và mượt hơn)")]
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
        [Tooltip("Bật tính năng dùng tiếng ồn Perlin Noise để tự động loại bỏ bớt một số vật thể ngẫu nhiên tạo độ thưa thớt tự nhiên")]
        public bool useNoiseModifier = false;
        [Tooltip("Tỉ lệ thu phóng nhiễu của bộ lọc thưa")]
        public float modifierNoiseScale = 0.2f;
        [Range(0f, 1f)]
        [Tooltip("Ngưỡng lọc thưa (Nếu giá trị nhiễu thấp hơn ngưỡng này thì vật thể tại điểm đó sẽ bị bỏ qua)")]
        public float modifierNoiseThreshold = 0.4f;

        [Header("Lọc Theo Biome")]
        [Tooltip("Chỉ cho phép vùng này hoạt động ở các Biome khớp với Tag này (Để trống = Hoạt động ở mọi Biome)")]
        public List<string> biomeTags = new List<string>();
    }
}
