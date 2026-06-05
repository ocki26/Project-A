using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Spawning
{
    [DisallowMultipleComponent]
    public class SpawnDebugger : MonoBehaviour
    {
        public bool showDebug = true;
        public bool showFailedAttempts = true;
        public bool showZoneOutlines = true;

        [System.Serializable]
        public struct DebugSpawnPoint
        {
            public Vector3 position;
            public string ruleName;
        }

        [HideInInspector] public List<DebugSpawnPoint> successPoints = new List<DebugSpawnPoint>();
        [HideInInspector] public List<DebugSpawnPoint> failedPoints = new List<DebugSpawnPoint>();
        
        // Cache active zones mapped to their room position to draw outlines
        private readonly List<(Vector3 roomWorldPos, Vector2 roomSize, SpawnZoneData zone)> activeZones = 
            new List<(Vector3, Vector2, SpawnZoneData)>();

        private readonly List<(Vector3 roomWorldPos, Vector2 roomSize, RoomSpawnZoneAuthoring.LocalSpawnZone zone)> activeLocalZones = 
            new List<(Vector3, Vector2, RoomSpawnZoneAuthoring.LocalSpawnZone)>();

        public void Clear()
        {
            successPoints.Clear();
            failedPoints.Clear();
            activeZones.Clear();
            activeLocalZones.Clear();
        }

        public void AddSuccess(Vector3 pos, string ruleName)
        {
            successPoints.Add(new DebugSpawnPoint { position = pos, ruleName = ruleName });
        }

        public void AddFailure(Vector3 pos, string reason)
        {
            failedPoints.Add(new DebugSpawnPoint { position = pos, ruleName = reason });
        }

        public void RegisterZoneOutline(Vector3 roomWorldPos, Vector2 roomSize, SpawnZoneData zone)
        {
            activeZones.Add((roomWorldPos, roomSize, zone));
        }

        public void RegisterZoneOutline(Vector3 roomWorldPos, Vector2 roomSize, RoomSpawnZoneAuthoring.LocalSpawnZone zone)
        {
            activeLocalZones.Add((roomWorldPos, roomSize, zone));
        }

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            // 1. Draw Zone Outlines
            if (showZoneOutlines)
            {
                foreach (var item in activeZones)
                {
                    Gizmos.color = item.zone.debugColor;
                    DrawZoneGizmo(item.roomWorldPos, item.roomSize, item.zone);
                }
                foreach (var item in activeLocalZones)
                {
                    Gizmos.color = item.zone.debugColor;
                    DrawZoneGizmo(item.roomWorldPos, item.roomSize, item.zone);
                }
            }

            // 2. Draw Success Points (Green spheres)
            Gizmos.color = Color.green;
            int successCount = successPoints.Count;
            for (int i = 0; i < successCount; i++)
            {
                Gizmos.DrawSphere(successPoints[i].position, 0.2f);
            }

            // 3. Draw Failed Points (Red cross/wire spheres)
            if (showFailedAttempts)
            {
                Gizmos.color = Color.red;
                int failedCount = failedPoints.Count;
                for (int i = 0; i < failedCount; i++)
                {
                    Gizmos.DrawWireSphere(failedPoints[i].position, 0.15f);
                }
            }
        }

        private void DrawZoneGizmo(Vector3 roomWorldPos, Vector2 roomSize, SpawnZoneData zone)
        {
            switch (zone.shapeType)
            {
                case ZoneShapeType.Rectangle:
                    Vector3 rectCenter = roomWorldPos + new Vector3(zone.rectCenter.x, zone.rectCenter.y, 0f);
                    Gizmos.DrawWireCube(rectCenter, new Vector3(zone.rectSize.x, zone.rectSize.y, 0.1f));
                    Gizmos.color = new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, zone.debugColor.a * 0.3f);
                    Gizmos.DrawCube(rectCenter, new Vector3(zone.rectSize.x, zone.rectSize.y, 0.1f));
                    break;

                case ZoneShapeType.Circle:
                    Vector3 circleCenter = roomWorldPos + new Vector3(zone.circleCenter.x, zone.circleCenter.y, 0f);
                    // Draw circle outline in 2D (Z-up / XY plane)
                    DrawGizmoCircle(circleCenter, zone.circleRadius);
                    break;

                case ZoneShapeType.Polygon:
                    if (zone.polygonVertices != null && zone.polygonVertices.Count > 2)
                    {
                        int count = zone.polygonVertices.Count;
                        for (int i = 0; i < count; i++)
                        {
                            Vector3 p1 = roomWorldPos + new Vector3(zone.polygonVertices[i].x, zone.polygonVertices[i].y, 0f);
                            Vector3 p2 = roomWorldPos + new Vector3(zone.polygonVertices[(i + 1) % count].x, zone.polygonVertices[(i + 1) % count].y, 0f);
                            Gizmos.DrawLine(p1, p2);
                        }
                    }
                    break;

                case ZoneShapeType.NoiseMask:
                    // Draw room bounds for Noise Mask
                    Vector3 roomCenter = roomWorldPos + new Vector3(roomSize.x * 0.5f, roomSize.y * 0.5f, 0f);
                    Gizmos.DrawWireCube(roomCenter, new Vector3(roomSize.x, roomSize.y, 0.1f));
                    break;

                case ZoneShapeType.CustomBrush:
                    if (zone.brushCells != null)
                    {
                        foreach (var cell in zone.brushCells)
                        {
                            // Each cell is assumed to be 1x1, centered at cell + 0.5
                            Vector3 cellCenter = roomWorldPos + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                            Gizmos.DrawWireCube(cellCenter, new Vector3(1f, 1f, 0.1f));
                            Gizmos.color = new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, zone.debugColor.a * 0.3f);
                            Gizmos.DrawCube(cellCenter, new Vector3(1f, 1f, 0.1f));
                            Gizmos.color = zone.debugColor; // Reset color
                        }
                    }
                    break;
            }
        }

        private void DrawZoneGizmo(Vector3 roomWorldPos, Vector2 roomSize, RoomSpawnZoneAuthoring.LocalSpawnZone zone)
        {
            switch (zone.shapeType)
            {
                case ZoneShapeType.Rectangle:
                    Vector3 rectCenter = roomWorldPos + new Vector3(zone.rectCenter.x, zone.rectCenter.y, 0f);
                    Gizmos.DrawWireCube(rectCenter, new Vector3(zone.rectSize.x, zone.rectSize.y, 0.1f));
                    Gizmos.color = new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, zone.debugColor.a * 0.3f);
                    Gizmos.DrawCube(rectCenter, new Vector3(zone.rectSize.x, zone.rectSize.y, 0.1f));
                    break;

                case ZoneShapeType.Circle:
                    Vector3 circleCenter = roomWorldPos + new Vector3(zone.circleCenter.x, zone.circleCenter.y, 0f);
                    DrawGizmoCircle(circleCenter, zone.circleRadius);
                    break;

                case ZoneShapeType.Polygon:
                    if (zone.polygonVertices != null && zone.polygonVertices.Count > 2)
                    {
                        int count = zone.polygonVertices.Count;
                        for (int i = 0; i < count; i++)
                        {
                            Vector3 p1 = roomWorldPos + new Vector3(zone.polygonVertices[i].x, zone.polygonVertices[i].y, 0f);
                            Vector3 p2 = roomWorldPos + new Vector3(zone.polygonVertices[(i + 1) % count].x, zone.polygonVertices[(i + 1) % count].y, 0f);
                            Gizmos.DrawLine(p1, p2);
                        }
                    }
                    break;

                case ZoneShapeType.NoiseMask:
                    Vector3 roomCenter = roomWorldPos + new Vector3(roomSize.x * 0.5f, roomSize.y * 0.5f, 0f);
                    Gizmos.DrawWireCube(roomCenter, new Vector3(roomSize.x, roomSize.y, 0.1f));
                    break;

                case ZoneShapeType.CustomBrush:
                    if (zone.brushCells != null)
                    {
                        foreach (var cell in zone.brushCells)
                        {
                            Vector3 cellCenter = roomWorldPos + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                            Gizmos.DrawWireCube(cellCenter, new Vector3(1f, 1f, 0.1f));
                            Gizmos.color = new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, zone.debugColor.a * 0.3f);
                            Gizmos.DrawCube(cellCenter, new Vector3(1f, 1f, 0.1f));
                            Gizmos.color = zone.debugColor; // Reset color
                        }
                    }
                    break;
            }
        }

        private void DrawGizmoCircle(Vector3 center, float radius)
        {
            int segments = 24;
            float angleStep = 360f / segments;
            Vector3 prevPt = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPt = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(prevPt, nextPt);
                prevPt = nextPt;
            }
        }
    }
}
