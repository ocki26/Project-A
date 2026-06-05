using System;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a full character equipment preset.
/// Create via: Assets > Create > LPC > Character Loadout
///
/// Each entry maps a slot name to an item.
/// ApplyLoadout() in LPCEquipmentManager iterates these entries and
/// calls EquipItem(slotName, item) for each one.
/// </summary>
[CreateAssetMenu(menuName = "LPC/Character Loadout", fileName = "NewLoadout")]
public class CharacterLoadout : ScriptableObject
{
    [Serializable]
    public class LoadoutEntry
    {
        [Tooltip("Must match a SlotDefinition.slotName in LPCEquipmentManager " +
                 "(e.g. 'Weapon', 'Helmet', 'Torso').")]
        public string slotName;

        [Tooltip("The item to equip in this slot. Leave null to leave the slot unchanged.")]
        public LPCItemData item;
    }

    [Tooltip("One entry per slot you want to set. Slots not listed here are left as-is.")]
    public LoadoutEntry[] entries;
}