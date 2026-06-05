using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Core;

namespace DungeonSystem.Generation
{
    [CreateAssetMenu(fileName = "NewDungeonConfig", menuName = "Dungeon/Dungeon Config")]
    public class DungeonConfig : ScriptableObject
    {
        [Header("Ghi Chú & Mô Tả")]
        [TextArea(3, 8)]
        [Tooltip("Ghi chú cá nhân mô tả cấu hình hầm ngục này (ví dụ: Bản đồ rừng rậm độ khó dễ, nhiều rương kho báu)")]
        public string developerNotes;

        [Header("Thiết Lập Chung")]
        [Tooltip("Seed cố định dùng để sinh hầm ngục (Đảm bảo việc sinh map là đồng nhất nếu trùng seed)")]
        public int seed = 1337;
        [Tooltip("Nếu chọn, game sẽ tự động dùng Seed ngẫu nhiên mỗi lần sinh map mới")]
        public bool useRandomSeed = true;
        [Tooltip("Kích thước của một ô phòng tiêu chuẩn (Ví dụ: 20 tương ứng phòng 20x20 đơn vị)")]
        public int cellSize = 20;
        [Tooltip("Kích thước lưới giới hạn tối đa cho toàn bộ hầm ngục")]
        public Vector2Int mapBounds = new Vector2Int(100, 100);

        [Header("Giới Hạn Sinh Phòng")]
        [Tooltip("Số lượng phòng tối thiểu mong muốn trong hầm ngục")]
        public int minRooms = 8;
        [Tooltip("Số lượng phòng tối đa mong muốn trong hầm ngục")]
        public int maxRooms = 15;

        [Header("Giới Hạn Phòng Đặc Biệt")]
        [Tooltip("Số lượng phòng kho báu tối đa")]
        public int maxTreasureRooms = 1;
        [Tooltip("Số lượng phòng cửa hàng (Shop) tối đa")]
        public int maxShopRooms = 1;
        [Tooltip("Số lượng phòng bí mật (Secret Room) tối đa")]
        public int maxSecretRooms = 1;

        [Header("Danh Sách Prefab Phòng")]
        [Tooltip("Các prefab dùng để sinh phòng bình thường")]
        public List<RoomPrefabData> normalRooms = new List<RoomPrefabData>();
        [Tooltip("Các prefab dùng để sinh phòng kho báu")]
        public List<RoomPrefabData> treasureRooms = new List<RoomPrefabData>();
        [Tooltip("Các prefab dùng để sinh phòng Boss")]
        public List<RoomPrefabData> bossRooms = new List<RoomPrefabData>();
        [Tooltip("Các prefab dùng để sinh phòng Shop")]
        public List<RoomPrefabData> shopRooms = new List<RoomPrefabData>();
        [Tooltip("Các prefab dùng để sinh phòng bí mật")]
        public List<RoomPrefabData> secretRooms = new List<RoomPrefabData>();
        [Tooltip("Các prefab dùng để sinh hành lang nối các phòng")]
        public List<RoomPrefabData> corridors = new List<RoomPrefabData>();
        [Tooltip("Các prefab dùng để sinh phòng bắt đầu (Start Room)")]
        public List<RoomPrefabData> startRooms = new List<RoomPrefabData>();

        public List<RoomPrefabData> GetPrefabsByType(RoomType type)
        {
            switch (type)
            {
                case RoomType.Normal: return normalRooms;
                case RoomType.Treasure: return treasureRooms;
                case RoomType.Boss: return bossRooms;
                case RoomType.Shop: return shopRooms;
                case RoomType.Secret: return secretRooms;
                case RoomType.Corridor: return corridors;
                case RoomType.Start: return startRooms;
                default: return normalRooms;
            }
        }
    }
}
