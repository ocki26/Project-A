using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Spawning.Spatial
{
    public class SpatialHashGrid
    {
        private float cellSize;
        private readonly Dictionary<Vector2Int, List<Vector3>> grid = new Dictionary<Vector2Int, List<Vector3>>();
        private readonly List<List<Vector3>> listPool = new List<List<Vector3>>();

        public SpatialHashGrid(float cellSize)
        {
            // Avoid zero division
            this.cellSize = Mathf.Max(0.1f, cellSize);
        }

        public void Reset(float newCellSize)
        {
            this.cellSize = Mathf.Max(0.1f, newCellSize);
            Clear();
        }

        private Vector2Int GetKey(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize)
            );
        }

        /// <summary>
        /// Inserts a point into the grid, utilizing pooled lists where possible.
        /// </summary>
        public void Insert(Vector3 position)
        {
            Vector2Int key = GetKey(position);
            if (!grid.TryGetValue(key, out var list))
            {
                if (listPool.Count > 0)
                {
                    int lastIdx = listPool.Count - 1;
                    list = listPool[lastIdx];
                    listPool.RemoveAt(lastIdx);
                    list.Clear();
                }
                else
                {
                    list = new List<Vector3>();
                }
                grid[key] = list;
            }
            list.Add(position);
        }

        /// <summary>
        /// Checks if there is any point within the given radius around the candidate position.
        /// Returns true if a conflict (another point too close) is found.
        /// </summary>
        public bool CheckProximity(Vector3 position, float radius)
        {
            Vector2Int centerKey = GetKey(position);
            int range = Mathf.CeilToInt(radius / cellSize);
            float radiusSqr = radius * radius;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    Vector2Int key = centerKey + new Vector2Int(x, y);
                    if (grid.TryGetValue(key, out var points))
                    {
                        // Using square distance is faster than Vector3.Distance
                        int count = points.Count;
                        for (int i = 0; i < count; i++)
                        {
                            if ((points[i] - position).sqrMagnitude < radiusSqr)
                            {
                                return true; // Collision/Proximity conflict
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Clears the grid data, returning all cell lists to the pool for reuse.
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in grid)
            {
                if (kvp.Value != null)
                {
                    listPool.Add(kvp.Value);
                }
            }
            grid.Clear();
        }
    }
}
