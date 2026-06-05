using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Core;

namespace DungeonSystem.Generation
{
    public class RoomGenerator
    {
        private readonly DungeonConfig config;
        private readonly System.Random rng;

        public RoomGenerator(DungeonConfig config, System.Random rng)
        {
            this.config = config;
            this.rng = rng;
        }

        public DungeonLayout GenerateLayout()
        {
            DungeonLayout layout = new DungeonLayout();
            int targetRoomCount = rng.Next(config.minRooms, config.maxRooms + 1);
            Queue<RoomNode> openSet = new Queue<RoomNode>();

            RoomNode startRoom = new RoomNode { gridPos = Vector2Int.zero, type = RoomType.Start };
            layout.AddRoom(startRoom, config.cellSize);
            openSet.Enqueue(startRoom);

            int iterations = 0;
            while (openSet.Count > 0 && layout.Rooms.Count < targetRoomCount && iterations < 1000)
            {
                iterations++;
                RoomNode current = openSet.Dequeue();
                DoorMask[] directions = { DoorMask.North, DoorMask.South, DoorMask.East, DoorMask.West };
                ShuffleArray(directions);

                foreach (var dir in directions)
                {
                    if (layout.Rooms.Count >= targetRoomCount) break;

                    Vector2Int offset = DoorMaskUtility.DirectionToVector(dir);
                    Vector2Int targetPos = current.gridPos + offset;

                    if (Mathf.Abs(targetPos.x) > config.mapBounds.x / 2 || Mathf.Abs(targetPos.y) > config.mapBounds.y / 2)
                        continue;

                    if (!layout.IsSpaceOccupied(targetPos, Vector2Int.one))
                    {
                        RoomNode neighbor = new RoomNode { gridPos = targetPos, type = RoomType.Normal };
                        
                        current.requiredDoors |= dir;
                        neighbor.requiredDoors |= DoorMaskUtility.GetOpposite(dir);

                        current.neighbors.Add(neighbor);
                        neighbor.neighbors.Add(current);

                        layout.AddRoom(neighbor, config.cellSize);
                        openSet.Enqueue(neighbor);
                    }
                }
            }

            AssignSpecialRooms(layout);
            return layout;
        }

        private void AssignSpecialRooms(DungeonLayout layout)
        {
            List<RoomNode> deadEnds = new List<RoomNode>();
            foreach (var room in layout.Rooms)
            {
                if (room.gridPos != Vector2Int.zero && room.neighbors.Count == 1)
                {
                    deadEnds.Add(room);
                }
            }

            deadEnds.Sort((a, b) => Vector2Int.Distance(b.gridPos, Vector2Int.zero).CompareTo(Vector2Int.Distance(a.gridPos, Vector2Int.zero)));

            int assignedBoss = 0;
            int assignedTreasure = 0;
            int assignedShop = 0;

            for (int i = 0; i < deadEnds.Count; i++)
            {
                if (assignedBoss < 1 && config.bossRooms != null && config.bossRooms.Count > 0)
                {
                    deadEnds[i].type = RoomType.Boss;
                    assignedBoss++;
                }
                else if (assignedTreasure < config.maxTreasureRooms && config.treasureRooms != null && config.treasureRooms.Count > 0)
                {
                    deadEnds[i].type = RoomType.Treasure;
                    assignedTreasure++;
                }
                else if (assignedShop < config.maxShopRooms && config.shopRooms != null && config.shopRooms.Count > 0)
                {
                    deadEnds[i].type = RoomType.Shop;
                    assignedShop++;
                }
            }
        }

        private void ShuffleArray<T>(T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }
}
