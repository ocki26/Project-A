using System.IO;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LPC_SaveSystem
{
    private static string GetSavePath(int slot) => Path.Combine(Application.persistentDataPath, $"savegame_slot{slot}.json");

    [System.Serializable]
    public class SaveData
    {
        public string playerName;
        public int level;
        public float currentExp;
        public float requiredExp;
        public int statPoints;
        public int gold;

        public int baseSTR;
        public int baseDEX;
        public int baseINT;
        public int baseVIT;
        public int baseAGI;
        public int baseLUK;

        public float currentHP;
        public float currentMP;
        public float currentStamina;
        public float currentStaminaLau;
        public float currentShield;
        public float currentHunger;
        public float currentThirst;

        public List<EquippedItemSave> equippedItems = new List<EquippedItemSave>();
        public List<InventoryItemSave> inventoryItems = new List<InventoryItemSave>();
    }

    [System.Serializable]
    public class EquippedItemSave
    {
        public string slotName;
        public string assetPath;
        public string itemName;
        public float currentDurability;
    }

    [System.Serializable]
    public class InventoryItemSave
    {
        public string assetPath;
        public string itemName;
        public float currentDurability;
    }

    public static string GetSlotInfo(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return "Empty Slot";

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return $"{data.playerName} - Lv.{data.level} ({data.gold:N0} G)";
        }
        catch
        {
            return "Corrupted Save";
        }
    }

    public static void SaveGame(LPCPlayerController2 player, LPC_UI_Manager uiManager, int slot)
    {
        if (player == null || uiManager == null)
        {
            Debug.LogError("[LPC_SaveSystem] Player or UI Manager is null. Cannot save.");
            return;
        }

        SaveData data = new SaveData();
        data.playerName = uiManager.playerName;
        data.level = player.level;
        data.currentExp = player.currentExp;
        data.requiredExp = player.requiredExp;
        data.statPoints = player.statPoints;
        data.gold = uiManager.gold;

        data.baseSTR = player.baseSTR;
        data.baseDEX = player.baseDEX;
        data.baseINT = player.baseINT;
        data.baseVIT = player.baseVIT;
        data.baseAGI = player.baseAGI;
        data.baseLUK = player.baseLUK;

        data.currentHP = player.currentHP;
        data.currentMP = player.currentMP;
        data.currentStamina = player.currentStamina;
        data.currentStaminaLau = player.currentStaminaLau;
        data.currentShield = player.currentShield;
        data.currentHunger = player.currentHunger;
        data.currentThirst = player.currentThirst;

        // Save equipped items
        if (player.equipmentManager != null)
        {
            foreach (var kvp in player.equipmentManager.GetAllEquipped())
            {
                if (kvp.Value == null) continue;

                EquippedItemSave itemSave = new EquippedItemSave();
                itemSave.slotName = kvp.Key;
                itemSave.itemName = kvp.Value.itemName;
                itemSave.currentDurability = kvp.Value.currentDurability;

#if UNITY_EDITOR
                itemSave.assetPath = AssetDatabase.GetAssetPath(kvp.Value);
#else
                itemSave.assetPath = "";
#endif
                data.equippedItems.Add(itemSave);
            }
        }

        // Save inventory
        if (uiManager.inventory != null)
        {
            foreach (var item in uiManager.inventory)
            {
                if (item == null) continue;

                InventoryItemSave itemSave = new InventoryItemSave();
                itemSave.itemName = item.itemName;
                itemSave.currentDurability = item.currentDurability;

#if UNITY_EDITOR
                itemSave.assetPath = AssetDatabase.GetAssetPath(item);
#else
                itemSave.assetPath = "";
#endif
                data.inventoryItems.Add(itemSave);
            }
        }

        string path = GetSavePath(slot);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log($"[LPC_SaveSystem] Game saved successfully to slot {slot}: {path}");
    }

    public static bool LoadGame(LPCPlayerController2 player, LPC_UI_Manager uiManager, int slot)
    {
        if (player == null || uiManager == null)
        {
            Debug.LogError("[LPC_SaveSystem] Player or UI Manager is null. Cannot load.");
            return false;
        }

        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[LPC_SaveSystem] Save file not found for slot {slot} at: {path}");
            return false;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        uiManager.playerName = data.playerName;
        player.level = data.level;
        player.currentExp = data.currentExp;
        player.requiredExp = data.requiredExp;
        player.statPoints = data.statPoints;
        uiManager.gold = data.gold;

        player.baseSTR = data.baseSTR;
        player.baseDEX = data.baseDEX;
        player.baseINT = data.baseINT;
        player.baseVIT = data.baseVIT;
        player.baseAGI = data.baseAGI;
        player.baseLUK = data.baseLUK;

        player.currentHP = data.currentHP;
        player.currentMP = data.currentMP;
        player.currentStamina = data.currentStamina;
        player.currentStaminaLau = data.currentStaminaLau;
        player.currentShield = data.currentShield;
        player.currentHunger = data.currentHunger;
        player.currentThirst = data.currentThirst;

        // Clear existing equipped and inventory
        if (player.equipmentManager != null)
        {
            var equippedSlots = new List<string>(player.equipmentManager.GetAllEquipped().Keys);
            foreach (var s in equippedSlots)
            {
                player.equipmentManager.UnequipItem(s);
            }
        }

        uiManager.inventory.Clear();

        // Prepare list of templates for runtime/build fallback matching
        List<LPCItemData> templates = new List<LPCItemData>();
        templates.AddRange(Resources.LoadAll<LPCItemData>(""));
        
        // Load inventory items
        foreach (var itemSave in data.inventoryItems)
        {
            LPCItemData template = FindItemTemplate(itemSave.assetPath, itemSave.itemName, templates);
            if (template != null)
            {
                LPCItemData runtimeItem = Object.Instantiate(template);
                runtimeItem.currentDurability = itemSave.currentDurability;
                uiManager.inventory.Add(runtimeItem);
            }
        }

        // Load and equip items
        if (player.equipmentManager != null)
        {
            foreach (var itemSave in data.equippedItems)
            {
                LPCItemData template = FindItemTemplate(itemSave.assetPath, itemSave.itemName, templates);
                if (template != null)
                {
                    LPCItemData runtimeItem = Object.Instantiate(template);
                    runtimeItem.currentDurability = itemSave.currentDurability;
                    player.equipmentManager.EquipItem(itemSave.slotName, runtimeItem);
                }
            }
        }

        player.CalculateFinalStats();
        uiManager.RefreshAll();
        Debug.Log($"[LPC_SaveSystem] Game loaded successfully from slot {slot}.");
        return true;
    }

    private static LPCItemData FindItemTemplate(string assetPath, string itemName, List<LPCItemData> templates)
    {
        LPCItemData item = null;

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(assetPath))
        {
            item = AssetDatabase.LoadAssetAtPath<LPCItemData>(assetPath);
            if (item != null) return item;
        }
#endif

        // Fallback 1: search Resources.Load
        item = Resources.Load<LPCItemData>(itemName);
        if (item != null) return item;
        
        item = Resources.Load<LPCItemData>("Items/" + itemName);
        if (item != null) return item;

        // Fallback 2: search preloaded templates
        foreach (var t in templates)
        {
            if (t != null && t.itemName == itemName)
                return t;
        }

        return null;
    }
}
