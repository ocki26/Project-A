using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(LPC_RoofFader))]
public class LPC_RoofFaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        
        var fader = (LPC_RoofFader)target;

        GUI.backgroundColor = new Color(0.15f, 0.6f, 1f);
        if (GUILayout.Button("🔍 Auto-Detect & Assign Roofs", GUILayout.Height(30)))
        {
            AutoDetectRoofs(fader);
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.HelpBox("Clicking this button will scan all child objects of the parent Grid (like Roof, RoofB, RoofDetail, Roof_Details) and automatically populate the arrays above.", MessageType.Info);
    }

    private void AutoDetectRoofs(LPC_RoofFader fader)
    {
        var parent = fader.transform.parent;
        if (parent == null)
        {
            EditorUtility.DisplayDialog("LPC Roof Fader", "Trigger object has no parent! Cannot search for roof objects.", "OK");
            return;
        }

        var tms = new List<Tilemap>();
        var srs = new List<SpriteRenderer>();

        foreach (Transform child in parent)
        {
            string lowerName = child.name.ToLower();
            if (lowerName.Contains("roof") || lowerName.Contains("overhead") || lowerName.Contains("chimney") || lowerName.Contains("ceiling"))
            {
                var tm = child.GetComponent<Tilemap>();
                if (tm != null) tms.Add(tm);

                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) srs.Add(sr);
            }
        }

        Undo.RecordObject(fader, "Auto Detect and Assign Roofs");
        
        fader.roofTilemaps = tms;
        fader.roofSpriteRenderers = srs;

        // Clear single ones if we are assigning arrays
        if (tms.Count > 0) fader.roofTilemap = null;
        if (srs.Count > 0) fader.roofSpriteRenderer = null;

        EditorUtility.SetDirty(fader);

        EditorUtility.DisplayDialog("LPC Roof Fader", 
            $"Successfully assigned:\n- {tms.Count} Tilemap(s)\n- {srs.Count} SpriteRenderer(s)\nfrom the parent Grid '{parent.name}'.", "Awesome");
    }
}
#endif
