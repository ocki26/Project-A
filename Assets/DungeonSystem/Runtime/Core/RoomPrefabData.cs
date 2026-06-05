using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Generation;

namespace DungeonSystem.Core
{
    [DisallowMultipleComponent]
    public class RoomPrefabData : MonoBehaviour
    {
        [Header("Ghi Chú & Mô Tả")]
        [TextArea(3, 8)]
        [Tooltip("Ghi chú cá nhân mô tả về phòng này (ví dụ: Phòng Boss chính thiết kế đối xứng, có cột đá hai bên)")]
        public string developerNotes;

        [Header("Thuộc Tính Phòng")]
        [Tooltip("Loại phòng tương ứng (Normal, Treasure, Boss, Shop, Secret, Corridor)")]
        public RoomType roomType;
        [Tooltip("Kích thước vật lý thực tế của phòng này (Ví dụ: 20x20, 40x40 đơn vị)")]
        public Vector2Int roomSize = new Vector2Int(20, 20);
        [Tooltip("Mặt nạ cửa ra vào của phòng (North, South, East, West)")]
        public DoorMask doors;
        [Range(0.1f, 100f)]
        [Tooltip("Trọng số ngẫu nhiên khi chọn prefab phòng này (Chỉ số càng cao phòng càng dễ xuất hiện)")]
        public float weight = 1.0f;

        [Header("Ghi Đè Preset Spawn (Tùy Chọn)")]
        [Tooltip("Chỉ định một RoomSpawnPreset cụ thể cho phòng này. Nếu để trống, hệ thống sẽ sử dụng preset theo mặc định của loại phòng.")]
        public Spawning.RoomSpawnPreset spawnPreset;
        
        [Tooltip("True nếu điểm tựa (Pivot) của Prefab nằm ở tâm (0.5, 0.5). False nếu nằm ở góc dưới bên trái (0, 0) của phòng.")]
        public bool pivotIsCenter = true;

        [Header("Basic 1x1 Walls & Doors (Or general slots)")]
        public GameObject wallTop;
        public GameObject wallBottom;
        public GameObject wallLeft;
        public GameObject wallRight;
        public GameObject doorTop;
        public GameObject doorBottom;
        public GameObject doorLeft;
        public GameObject doorRight;

        [Header("Extended 2x2 Walls & Doors (8-Door Setup)")]
        public GameObject wallTopLeft;
        public GameObject wallTopRight;
        public GameObject doorTopLeft;
        public GameObject doorTopRight;

        public GameObject wallBottomLeft;
        public GameObject wallBottomRight;
        public GameObject doorBottomLeft;
        public GameObject doorBottomRight;

        public GameObject wallLeftBottom;
        public GameObject wallLeftTop;
        public GameObject doorLeftBottom;
        public GameObject doorLeftTop;

        public GameObject wallRightBottom;
        public GameObject wallRightTop;
        public GameObject doorRightBottom;
        public GameObject doorRightTop;

        [Header("Custom Shape Setup (L, T, U shapes)")]
        public bool isCustomShape = false;
        public List<Vector2Int> customGridOccupancy = new List<Vector2Int>();

        public Vector2Int GetGridSize(int cellSize)
        {
            return new Vector2Int(
                Mathf.Max(1, roomSize.x / cellSize),
                Mathf.Max(1, roomSize.y / cellSize)
            );
        }

        public List<Vector2Int> GetOccupiedOffsets(int cellSize)
        {
            if (isCustomShape && customGridOccupancy != null && customGridOccupancy.Count > 0)
            {
                return customGridOccupancy;
            }

            List<Vector2Int> offsets = new List<Vector2Int>();
            Vector2Int gridSize = GetGridSize(cellSize);
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    offsets.Add(new Vector2Int(x, y));
                }
            }
            return offsets;
        }

        /// <summary>
        /// Tự động bật/tắt tường và cửa động cho MỌI kích thước phòng (1x1, 2x1, 1x2, 2x2, 3x1, v.v.)
        /// bằng cách quét vị trí các phòng hàng xóm thực tế và ánh xạ thông minh.
        /// </summary>
        public void ConfigureDoorsAndWallsExtended(RoomNode node, DungeonLayout layout, int cellSize)
        {
            Vector2Int size = GetGridSize(cellSize);

            // Bước 1: Mặc định đóng toàn bộ cửa cơ bản và cửa mở rộng (Tường hoạt động, Cửa bị tắt)
            SetDoorAndWallState(wallTop, doorTop, false);
            SetDoorAndWallState(wallBottom, doorBottom, false);
            SetDoorAndWallState(wallLeft, doorLeft, false);
            SetDoorAndWallState(wallRight, doorRight, false);

            SetDoorAndWallState(wallTopLeft, doorTopLeft, false);
            SetDoorAndWallState(wallTopRight, doorTopRight, false);
            SetDoorAndWallState(wallBottomLeft, doorBottomLeft, false);
            SetDoorAndWallState(wallBottomRight, doorBottomRight, false);
            SetDoorAndWallState(wallLeftBottom, doorLeftBottom, false);
            SetDoorAndWallState(wallLeftTop, doorLeftTop, false);
            SetDoorAndWallState(wallRightBottom, doorRightBottom, false);
            SetDoorAndWallState(wallRightTop, doorRightTop, false);

            // Bước 2: Quét danh sách các phòng lân cận logic được tạo bởi đồ thị để mở cửa
            foreach (var neighbor in node.neighbors)
            {
                Vector2Int relativePos = neighbor.gridPos - node.gridPos;

                // 1. Phía Bắc (North - Trên) -> relativePos.y == size.y
                if (relativePos.y == size.y && relativePos.x >= 0 && relativePos.x < size.x)
                {
                    if (size.x == 1)
                    {
                        SetDoorAndWallState(wallTop, doorTop, true);
                    }
                    else if (size.x == 2)
                    {
                        if (relativePos.x == 0)
                            SetDoorAndWallState(wallTopLeft != null ? wallTopLeft : wallTop, doorTopLeft != null ? doorTopLeft : doorTop, true);
                        else if (relativePos.x == 1)
                            SetDoorAndWallState(wallTopRight != null ? wallTopRight : wallTop, doorTopRight != null ? doorTopRight : doorTop, true);
                    }
                    else // Tổng quát cho phòng rộng hơn 2 ô
                    {
                        if (relativePos.x == 0)
                            SetDoorAndWallState(wallTopLeft != null ? wallTopLeft : wallTop, doorTopLeft != null ? doorTopLeft : doorTop, true);
                        else
                            SetDoorAndWallState(wallTopRight != null ? wallTopRight : wallTop, doorTopRight != null ? doorTopRight : doorTop, true);
                    }
                }

                // 2. Phía Nam (South - Dưới) -> relativePos.y == -1
                if (relativePos.y == -1 && relativePos.x >= 0 && relativePos.x < size.x)
                {
                    if (size.x == 1)
                    {
                        SetDoorAndWallState(wallBottom, doorBottom, true);
                    }
                    else if (size.x == 2)
                    {
                        if (relativePos.x == 0)
                            SetDoorAndWallState(wallBottomLeft != null ? wallBottomLeft : wallBottom, doorBottomLeft != null ? doorBottomLeft : doorBottom, true);
                        else if (relativePos.x == 1)
                            SetDoorAndWallState(wallBottomRight != null ? wallBottomRight : wallBottom, doorBottomRight != null ? doorBottomRight : doorBottom, true);
                    }
                    else
                    {
                        if (relativePos.x == 0)
                            SetDoorAndWallState(wallBottomLeft != null ? wallBottomLeft : wallBottom, doorBottomLeft != null ? doorBottomLeft : doorBottom, true);
                        else
                            SetDoorAndWallState(wallBottomRight != null ? wallBottomRight : wallBottom, doorBottomRight != null ? doorBottomRight : doorBottom, true);
                    }
                }

                // 3. Phía Tây (West - Trái) -> relativePos.x == -1
                if (relativePos.x == -1 && relativePos.y >= 0 && relativePos.y < size.y)
                {
                    if (size.y == 1)
                    {
                        SetDoorAndWallState(wallLeft, doorLeft, true);
                    }
                    else if (size.y == 2)
                    {
                        if (relativePos.y == 0)
                            SetDoorAndWallState(wallLeftBottom != null ? wallLeftBottom : wallLeft, doorLeftBottom != null ? doorLeftBottom : doorLeft, true);
                        else if (relativePos.y == 1)
                            SetDoorAndWallState(wallLeftTop != null ? wallLeftTop : wallLeft, doorLeftTop != null ? doorLeftTop : doorLeft, true);
                    }
                    else
                    {
                        if (relativePos.y == 0)
                            SetDoorAndWallState(wallLeftBottom != null ? wallLeftBottom : wallLeft, doorLeftBottom != null ? doorLeftBottom : doorLeft, true);
                        else
                            SetDoorAndWallState(wallLeftTop != null ? wallLeftTop : wallLeft, doorLeftTop != null ? doorLeftTop : doorLeft, true);
                    }
                }

                // 4. Phía Đông (East - Phải) -> relativePos.x == size.x
                if (relativePos.x == size.x && relativePos.y >= 0 && relativePos.y < size.y)
                {
                    if (size.y == 1)
                    {
                        SetDoorAndWallState(wallRight, doorRight, true);
                    }
                    else if (size.y == 2)
                    {
                        if (relativePos.y == 0)
                            SetDoorAndWallState(wallRightBottom != null ? wallRightBottom : wallRight, doorRightBottom != null ? doorRightBottom : doorRight, true);
                        else if (relativePos.y == 1)
                            SetDoorAndWallState(wallRightTop != null ? wallRightTop : wallRight, doorRightTop != null ? doorRightTop : doorRight, true);
                    }
                    else
                    {
                        if (relativePos.y == 0)
                            SetDoorAndWallState(wallRightBottom != null ? wallRightBottom : wallRight, doorRightBottom != null ? doorRightBottom : doorRight, true);
                        else
                            SetDoorAndWallState(wallRightTop != null ? wallRightTop : wallRight, doorRightTop != null ? doorRightTop : doorRight, true);
                    }
                }
            }
        }

        private void SetDoorAndWallState(GameObject wall, GameObject door, bool openDoor)
        {
            if (wall != null) wall.SetActive(!openDoor); // Mở cửa thì tắt tường
            if (door != null) door.SetActive(openDoor);   // Mở cửa thì hiện cửa
        }
    }
}
