using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Spawning
{
    [CreateAssetMenu(fileName = "NewRoomSpawnPreset", menuName = "Dungeon/Spawning/Room Spawn Preset")]
    public class RoomSpawnPreset : ScriptableObject
    {
        [Header("Ghi Chú & Mô Tả")]
        [TextArea(3, 8)]
        [Tooltip("Ghi chú cá nhân mô tả mục đích của Preset này (ví dụ: Preset phòng kho báu nhiều rương và cỏ dại)")]
        public string developerNotes;

        [Header("Cấu Hình Cơ Bản")]
        [Tooltip("Tên nhận diện trực quan cho Preset này")]
        public string presetName;

        [Tooltip("Cho phép kích hoạt preset này hoạt động hay không")]
        public bool isActive = true;

        [Header("Giới Hạn & Mật Độ Toàn Cục")]
        [Tooltip("Giới hạn tổng số lượng thực thể tối đa có thể được sinh ra trong một căn phòng áp dụng preset này (để tránh bị quá dày)")]
        public int maxTotalSpawns = 150;

        [Range(0f, 2f)]
        [Tooltip("Hệ số nhân tỷ lệ spawn toàn cục (ví dụ: 0.5 là giảm một nửa lượng spawn, 2 là nhân đôi lượng spawn)")]
        public float globalSpawnChanceMultiplier = 1.0f;

        [Header("Danh Sách Vùng Spawn")]
        [Tooltip("Các vùng Spawn Zone cấu hình con được quản lý bởi Preset này")]
        public List<SpawnZoneData> zones = new List<SpawnZoneData>();
    }
}
