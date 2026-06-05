using System;

namespace DungeonSystem.Core
{
    public enum RoomType
    {
        Normal,
        Treasure,
        Boss,
        Shop,
        Secret,
        Corridor,
        Start
    }

    [Flags]
    public enum DoorMask
    {
        None  = 0,
        North = 1,
        South = 2,
        East  = 4,
        West  = 8
    }

    public static class DoorMaskUtility
    {
        public static DoorMask GetOpposite(DoorMask door)
        {
            switch (door)
            {
                case DoorMask.North: return DoorMask.South;
                case DoorMask.South: return DoorMask.North;
                case DoorMask.East:  return DoorMask.West;
                case DoorMask.West:  return DoorMask.East;
                default: return DoorMask.None;
            }
        }

        public static UnityEngine.Vector2Int DirectionToVector(DoorMask door)
        {
            switch (door)
            {
                case DoorMask.North: return new UnityEngine.Vector2Int(0, 1);
                case DoorMask.South: return new UnityEngine.Vector2Int(0, -1);
                case DoorMask.East:  return new UnityEngine.Vector2Int(1, 0);
                case DoorMask.West:  return new UnityEngine.Vector2Int(-1, 0);
                default: return UnityEngine.Vector2Int.zero;
            }
        }
    }
}
