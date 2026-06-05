using UnityEngine;

/// <summary>
/// ScriptableObject definition for a single equipment slot.
/// Create via: Assets > Create > LPC > Equipment Slot Definition
/// 
/// The slotName must match:
///   1. The UXML element name:  Slot_[slotName]
///   2. The item's childPath field in LPCItemData
///   3. The child GameObject name in the character hierarchy
/// </summary>
[CreateAssetMenu(menuName = "LPC/Equipment Slot Definition", fileName = "SlotDef_New")]
public class EquipmentSlotDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Slot name — must match item.childPath and the UXML element Slot_XXX.")]
    public string slotName = "Weapon";

    [Tooltip("Human-readable name shown in the UI.")]
    public string displayName = "Weapon";

    public Sprite slotIcon;

    [Header("Slot Type")]
    [Tooltip("TRUE  = Appearance slot (Hair, Face, Clothing, Behind, etc.)\n" +
             "        Shown in the LOOK / Appearance panel.\n" +
             "FALSE = Equipment slot (Weapon, Armor, Helmet, Pants, Boots, etc.)\n" +
             "        Shown in the EQUIPMENT panel.")]
    public bool isAppearance = false;

    [Tooltip("Keep the SpriteRenderer enabled even when no item is equipped.\n" +
             "Use for base body parts (Body, Eyes, Ears) that must always be visible.")]
    public bool alwaysOn = false;

    [Header("Accepted Categories")]
    [Tooltip("Item categories that can be dragged into this slot.\n" +
             "Leave empty to accept any category.")]
    public string[] acceptedCategories = new string[0];

    [Header("Sorting")]
    public int    defaultSortingOrder = 0;
    public string sortingLayerName    = "Characters";

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true if the given item's category is accepted by this slot.
    /// Always returns true when acceptedCategories is empty.
    /// </summary>
    public bool AcceptsItem(LPCItemData item)
    {
        if (item == null) return false;
        if (acceptedCategories == null || acceptedCategories.Length == 0) return true;

        foreach (var c in acceptedCategories)
            if (c == item.category) return true;

        return false;
    }
}
