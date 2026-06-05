using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DungeonSystem.Spawning;
using DungeonSystem.Core;

namespace DungeonSystem.Editor.Spawning
{
    [CustomEditor(typeof(SpawnZoneData))]
    public class SpawnPresetEditor : UnityEditor.Editor
    {
        private bool isPaintingMode = false;
        private RoomPrefabData previewPrefab = null;

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            SpawnZoneData zone = (SpawnZoneData)target;

            if (zone.shapeType == ZoneShapeType.CustomBrush)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Custom Brush Painting Tool", EditorStyles.boldLabel);

                previewPrefab = (RoomPrefabData)EditorGUILayout.ObjectField(
                    "Preview Room Prefab", 
                    previewPrefab, 
                    typeof(RoomPrefabData), 
                    true
                );

                if (previewPrefab == null)
                {
                    EditorGUILayout.HelpBox("Assign a Room Prefab to define grid size for painting.", MessageType.Info);
                    isPaintingMode = false;
                    return;
                }

                GUILayout.BeginHorizontal();
                if (isPaintingMode)
                {
                    if (GUILayout.Button("Exit Painting Mode", GUILayout.Height(30)))
                    {
                        isPaintingMode = false;
                    }
                }
                else
                {
                    if (GUILayout.Button("Enter Painting Mode", GUILayout.Height(30)))
                    {
                        isPaintingMode = true;
                    }
                }

                if (GUILayout.Button("Clear All Cells", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Clear Cells", "Are you sure you want to clear all painted cells?", "Yes", "No"))
                    {
                        Undo.RecordObject(zone, "Clear Brush Cells");
                        zone.brushCells.Clear();
                        EditorUtility.SetDirty(zone);
                    }
                }
                GUILayout.EndHorizontal();

                if (isPaintingMode)
                {
                    EditorGUILayout.HelpBox(
                        "Painting Mode Active:\n" +
                        "- Left-Click & Drag in Scene View to PAINT cells.\n" +
                        "- Ctrl + Left-Click & Drag to ERASE cells.\n" +
                        "Switch to the Scene tab to begin painting.", 
                        MessageType.Warning
                    );
                }
            }
            else
            {
                isPaintingMode = false;
            }
        }

        private void OnSceneGUI()
        {
            SpawnZoneData zone = (SpawnZoneData)target;
            if (zone.shapeType != ZoneShapeType.CustomBrush || previewPrefab == null) return;

            // Draw room grid border
            Vector2Int roomSize = previewPrefab.roomSize;
            Vector3 origin = Vector3.zero;

            // Draw bounding border
            Handles.color = Color.white;
            Vector3[] corners = new Vector3[]
            {
                origin,
                origin + new Vector3(roomSize.x, 0, 0),
                origin + new Vector3(roomSize.x, roomSize.y, 0),
                origin + new Vector3(0, roomSize.y, 0)
            };
            Handles.DrawSolidRectangleWithOutline(corners, new Color(1, 1, 1, 0.02f), Color.white);

            // Draw sub-grid lines (each 1x1 cell)
            Handles.color = new Color(1, 1, 1, 0.15f);
            for (int x = 1; x < roomSize.x; x++)
            {
                Handles.DrawLine(origin + new Vector3(x, 0, 0), origin + new Vector3(x, roomSize.y, 0));
            }
            for (int y = 1; y < roomSize.y; y++)
            {
                Handles.DrawLine(origin + new Vector3(0, y, 0), origin + new Vector3(roomSize.x, y, 0));
            }

            // Draw currently painted cells
            Handles.color = zone.debugColor;
            if (zone.brushCells != null)
            {
                int cellCount = zone.brushCells.Count;
                for (int i = 0; i < cellCount; i++)
                {
                    Vector2Int cell = zone.brushCells[i];
                    Vector3 cellCenter = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                    Handles.DrawSolidRectangleWithOutline(
                        new Vector3[]
                        {
                            cellCenter + new Vector3(-0.5f, -0.5f, 0),
                            cellCenter + new Vector3(0.5f, -0.5f, 0),
                            cellCenter + new Vector3(0.5f, 0.5f, 0),
                            cellCenter + new Vector3(-0.5f, 0.5f, 0)
                        },
                        new Color(zone.debugColor.r, zone.debugColor.g, zone.debugColor.b, 0.35f),
                        zone.debugColor
                    );
                }
            }

            // Intercept mouse clicks if painting
            if (isPaintingMode)
            {
                // Disable selecting other objects in Scene View
                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlID);

                Event e = Event.current;
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0) // Left click
                    {
                        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                        Plane xyPlane = new Plane(Vector3.forward, Vector3.zero);

                        if (xyPlane.Raycast(ray, out float enter))
                        {
                            Vector3 hitPoint = ray.GetPoint(enter);
                            Vector2Int cellCoord = new Vector2Int(
                                Mathf.FloorToInt(hitPoint.x),
                                Mathf.FloorToInt(hitPoint.y)
                            );

                            // Check room bounds
                            if (cellCoord.x >= 0 && cellCoord.x < roomSize.x &&
                                cellCoord.y >= 0 && cellCoord.y < roomSize.y)
                            {
                                bool isErasing = e.control; // Ctrl to erase
                                
                                Undo.RecordObject(zone, isErasing ? "Erase Cell" : "Paint Cell");
                                if (zone.brushCells == null) zone.brushCells = new List<Vector2Int>();

                                if (isErasing)
                                {
                                    if (zone.brushCells.Contains(cellCoord))
                                    {
                                        zone.brushCells.Remove(cellCoord);
                                        EditorUtility.SetDirty(zone);
                                    }
                                }
                                else
                                {
                                    if (!zone.brushCells.Contains(cellCoord))
                                    {
                                        zone.brushCells.Add(cellCoord);
                                        EditorUtility.SetDirty(zone);
                                    }
                                }

                                e.Use();
                            }
                        }
                    }
                }
            }
        }
    }
}
