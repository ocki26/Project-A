using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Spawning
{
    public static class SpawnShapeEvaluator
    {
        /// <summary>
        /// Checks if a local point (relative to room bottom-left corner) is within the spawn zone.
        /// </summary>
        public static bool ContainsPoint(SpawnZoneData zone, Vector2 localPoint, Vector2 roomSize, Vector2 noiseOffset, HashSet<Vector2Int> cachedBrush = null)
        {
            if (zone == null) return false;

            // 1. General shape check
            bool insideShape = false;
            switch (zone.shapeType)
            {
                case ZoneShapeType.Rectangle:
                    insideShape = ContainsRectangle(zone, localPoint);
                    break;
                case ZoneShapeType.Circle:
                    insideShape = ContainsCircle(zone, localPoint);
                    break;
                case ZoneShapeType.Polygon:
                    insideShape = ContainsPolygon(zone, localPoint);
                    break;
                case ZoneShapeType.NoiseMask:
                    insideShape = ContainsNoiseMask(zone, localPoint, noiseOffset);
                    break;
                case ZoneShapeType.CustomBrush:
                    insideShape = ContainsCustomBrush(zone, localPoint, cachedBrush);
                    break;
            }

            if (!insideShape) return false;

            // 2. Extra Noise Modifier check (if enabled)
            if (zone.useNoiseModifier)
            {
                float nx = (localPoint.x + noiseOffset.x) * zone.modifierNoiseScale;
                float ny = (localPoint.y + noiseOffset.y) * zone.modifierNoiseScale;
                float noiseVal = Mathf.PerlinNoise(nx, ny);
                if (noiseVal < zone.modifierNoiseThreshold)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a local point (relative to room bottom-left corner) is within the local spawn zone.
        /// </summary>
        public static bool ContainsPoint(RoomSpawnZoneAuthoring.LocalSpawnZone zone, Vector2 localPoint, Vector2 roomSize, Vector2 noiseOffset, HashSet<Vector2Int> cachedBrush = null)
        {
            if (zone == null) return false;

            // 1. General shape check
            bool insideShape = false;
            switch (zone.shapeType)
            {
                case ZoneShapeType.Rectangle:
                    insideShape = ContainsRectangle(zone, localPoint);
                    break;
                case ZoneShapeType.Circle:
                    insideShape = ContainsCircle(zone, localPoint);
                    break;
                case ZoneShapeType.Polygon:
                    insideShape = ContainsPolygon(zone, localPoint);
                    break;
                case ZoneShapeType.NoiseMask:
                    insideShape = ContainsNoiseMask(zone, localPoint, noiseOffset);
                    break;
                case ZoneShapeType.CustomBrush:
                    insideShape = ContainsCustomBrush(zone, localPoint, cachedBrush);
                    break;
            }

            if (!insideShape) return false;

            // 2. Extra Noise Modifier check (if enabled)
            if (zone.useNoiseModifier)
            {
                float nx = (localPoint.x + noiseOffset.x) * zone.modifierNoiseScale;
                float ny = (localPoint.y + noiseOffset.y) * zone.modifierNoiseScale;
                float noiseVal = Mathf.PerlinNoise(nx, ny);
                if (noiseVal < zone.modifierNoiseThreshold)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsRectangle(SpawnZoneData zone, Vector2 p)
        {
            float halfX = zone.rectSize.x * 0.5f;
            float halfY = zone.rectSize.y * 0.5f;
            float minX = zone.rectCenter.x - halfX;
            float maxX = zone.rectCenter.x + halfX;
            float minY = zone.rectCenter.y - halfY;
            float maxY = zone.rectCenter.y + halfY;

            return p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY;
        }

        private static bool ContainsRectangle(RoomSpawnZoneAuthoring.LocalSpawnZone zone, Vector2 p)
        {
            float halfX = zone.rectSize.x * 0.5f;
            float halfY = zone.rectSize.y * 0.5f;
            float minX = zone.rectCenter.x - halfX;
            float maxX = zone.rectCenter.x + halfX;
            float minY = zone.rectCenter.y - halfY;
            float maxY = zone.rectCenter.y + halfY;

            return p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY;
        }

        private static bool ContainsCircle(SpawnZoneData zone, Vector2 p)
        {
            float sqrDist = (p - zone.circleCenter).sqrMagnitude;
            return sqrDist <= zone.circleRadius * zone.circleRadius;
        }

        private static bool ContainsCircle(RoomSpawnZoneAuthoring.LocalSpawnZone zone, Vector2 p)
        {
            float sqrDist = (p - zone.circleCenter).sqrMagnitude;
            return sqrDist <= zone.circleRadius * zone.circleRadius;
        }

        private static bool ContainsPolygon(SpawnZoneData zone, Vector2 p)
        {
            var vertices = zone.polygonVertices;
            if (vertices == null || vertices.Count < 3) return false;

            int count = vertices.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                if (((vertices[i].y > p.y) != (vertices[j].y > p.y)) &&
                    (p.x < (vertices[j].x - vertices[i].x) * (p.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static bool ContainsPolygon(RoomSpawnZoneAuthoring.LocalSpawnZone zone, Vector2 p)
        {
            var vertices = zone.polygonVertices;
            if (vertices == null || vertices.Count < 3) return false;

            int count = vertices.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                if (((vertices[i].y > p.y) != (vertices[j].y > p.y)) &&
                    (p.x < (vertices[j].x - vertices[i].x) * (p.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static bool ContainsNoiseMask(SpawnZoneData zone, Vector2 p, Vector2 noiseOffset)
        {
            float nx = (p.x + noiseOffset.x) * zone.noiseScale;
            float ny = (p.y + noiseOffset.y) * zone.noiseScale;
            return Mathf.PerlinNoise(nx, ny) >= zone.noiseThreshold;
        }

        private static bool ContainsNoiseMask(RoomSpawnZoneAuthoring.LocalSpawnZone zone, Vector2 p, Vector2 noiseOffset)
        {
            float nx = (p.x + noiseOffset.x) * zone.noiseScale;
            float ny = (p.y + noiseOffset.y) * zone.noiseScale;
            return Mathf.PerlinNoise(nx, ny) >= zone.noiseThreshold;
        }

        private static bool ContainsCustomBrush(SpawnZoneData zone, Vector2 p, HashSet<Vector2Int> cachedBrush)
        {
            Vector2Int cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            if (cachedBrush != null)
            {
                return cachedBrush.Contains(cell);
            }
            return zone.GetBrushCellsHashSet().Contains(cell);
        }

        private static bool ContainsCustomBrush(RoomSpawnZoneAuthoring.LocalSpawnZone zone, Vector2 p, HashSet<Vector2Int> cachedBrush)
        {
            Vector2Int cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            if (cachedBrush != null)
            {
                return cachedBrush.Contains(cell);
            }
            return zone.GetBrushCellsHashSet().Contains(cell);
        }
    }
}
