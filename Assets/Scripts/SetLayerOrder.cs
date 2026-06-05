using UnityEngine;

/// <summary>
/// SetLayerOrder — sets sortingLayerName + sortingOrder for every LPC character child.
///
/// FIX BUG 4:
///   Previously missing sortingLayerName caused children to render on the "Default"
///   sorting layer, mixing with tiles/environment and producing wrong draw order.
///   Now every child is explicitly placed on the "Characters" sorting layer.
///
/// RULE: This component is the SINGLE source of truth for sort order.
///   Do NOT set item.sortingOrder != 0 in LPCItemData — doing so lets
///   LPCEquipmentManager override this at equip time and breaks the order.
/// </summary>
[ExecuteAlways]
public class SetLayerOrder : MonoBehaviour
{
    [Tooltip("Must match an entry in Edit > Project Settings > Tags and Layers > Sorting Layers.")]
    public string sortingLayerName = "Characters";

    void Awake()
    {
        SortLayers();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SortLayers();
    }
#endif

    public void SortLayers()
    {
        // Order matches the LPC Universal Generator render stack - bottom (0) to top (25).
        // WeaponBehind/ShieldBehind are BELOW Body so they render behind the character
        // when facing Up. The AnimationClip (LPCAnimationImporter) toggles their
        // SpriteRenderer.enabled per direction - do not change their order here.
        string[] order =
        {
            "Shadow",       //  0 - always on, below everything
            "WeaponBehind", //  1 - behind body: visible facing Up
            "ShieldBehind", //  2 - behind body: visible facing Up
            "CapeBehind",   //  3
            "Quiver",       //  4
            "HairBehind",   //  5
            "Body",         //  6 - AnimationClip driven (m_Sprite)
            "Ears",         //  7 - AnimationClip driven
            "Eyes",         //  8 - AnimationClip driven
            "Underwear",    //  9 - AnimationClip driven
            "Legs",         // 10
            "Feet",         // 11
            "Torso",        // 12
            "Armor",        // 13
            "Arms",         // 14
            "Gloves",       // 15
            "Belt",         // 16
            "Neck",         // 17
            "Shoulders",    // 18
            "FacialHair",   // 19
            "HairFront",    // 20
            "Mask",         // 21
            "Helmet",       // 22
            "Shield",       // 23 - front shield: visible facing Down/Left/Right
            "Weapon",       // 24 - front weapon: visible facing Down/Left/Right
            "Effects",      // 25 - VFX always on top
        };

        for (int i = 0; i < order.Length; i++)
        {
            Transform child = transform.Find(order[i]);
            if (child == null) continue;

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            // FIX BUG 4a - set the correct sorting layer so children don't bleed
            // onto the Default layer (where tiles and environment objects live).
            sr.sortingLayerName = sortingLayerName;

            // FIX BUG 4b - set order once here; LPCEquipmentManager no longer
            // overrides this value at equip time (item.sortingOrder must be 0).
            sr.sortingOrder = i;
        }
    }
}