using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.DebugTools
{
    [ExecuteInEditMode]
    public class DebugVisualizer : MonoBehaviour
    {
        public bool showDebug = true;
        private List<Bounds> roomBounds = new List<Bounds>();

        public void SetBounds(List<Bounds> bounds)
        {
            roomBounds = new List<Bounds>(bounds);
        }

        public void Clear()
        {
            roomBounds.Clear();
        }

        private void OnDrawGizmos()
        {
            if (!showDebug || roomBounds == null) return;

            for (int i = 0; i < roomBounds.Count; i++)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
                Gizmos.DrawCube(roomBounds[i].center, roomBounds[i].size);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(roomBounds[i].center, roomBounds[i].size);
            }
        }
    }
}
