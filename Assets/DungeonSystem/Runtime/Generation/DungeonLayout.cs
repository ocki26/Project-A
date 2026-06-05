using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Core;

namespace DungeonSystem.Generation
{
    public class RoomNode
    {
        public Vector2Int gridPos; // Bottom-left coordinate on grid units
        public RoomType type;
        public DoorMask requiredDoors; // Door connections determined by layout graph
        public Vector2Int gridSize = Vector2Int.one; // Grid width/height
        public RoomPrefabData assignedPrefab;
        public List<RoomNode> neighbors = new List<RoomNode>();
    }

    public class DungeonLayout
    {
        public Dictionary<Vector2Int, RoomNode> CellToRoomMap { get; } = new Dictionary<Vector2Int, RoomNode>();
        public List<RoomNode> Rooms { get; } = new List<RoomNode>();

        // 1. Quét va chạm hình chữ nhật mặc định (cho giai đoạn dựng graph ban đầu khi chưa gán prefab)
        public bool IsSpaceOccupied(Vector2Int startPos, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    if (CellToRoomMap.ContainsKey(startPos + new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // 2. Quét va chạm nâng cao theo hình dạng thực tế (custom shape offsets) của prefab hầm ngục
        public bool IsSpaceOccupied(Vector2Int startPos, List<Vector2Int> occupiedOffsets)
        {
            if (occupiedOffsets == null || occupiedOffsets.Count == 0) return false;
            foreach (var offset in occupiedOffsets)
            {
                if (CellToRoomMap.ContainsKey(startPos + offset))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddRoom(RoomNode room, int cellSize)
        {
            Rooms.Add(room);
            RegisterOccupancy(room, cellSize);
        }

        public void RegisterOccupancy(RoomNode room, int cellSize)
        {
            if (room.assignedPrefab != null)
            {
                // Đăng ký chiếm dụng chuẩn xác từng ô logic theo cấu hình đa dạng của Prefab
                List<Vector2Int> offsets = room.assignedPrefab.GetOccupiedOffsets(cellSize);
                foreach (var offset in offsets)
                {
                    CellToRoomMap[room.gridPos + offset] = room;
                }
            }
            else
            {
                // Cơ chế dự phòng (Fallback) khi khởi tạo đồ thị chưa gán prefab
                for (int x = 0; x < room.gridSize.x; x++)
                {
                    for (int y = 0; y < room.gridSize.y; y++)
                    {
                        CellToRoomMap[room.gridPos + new Vector2Int(x, y)] = room;
                    }
                }
            }
        }
    }
}
