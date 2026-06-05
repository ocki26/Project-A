using UnityEngine;
using System.Collections.Generic;

public class LPC_DungeonFarmingShowcase : MonoBehaviour
{
    private Transform playerTransform;

    void Start()
    {
        // Tự động tìm Player trong Scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log("[Showcase] LPC_DungeonFarmingShowcase initialized on Player! Press [F5] to get Weapons, [F6] to spawn Ores/Plants!");
        }
        else
        {
            Debug.LogWarning("[Showcase] Player not found! Make sure Player has 'Player' Tag.");
        }
    }

    void Update()
    {
        // Hỗ trợ cả Input System mới và cũ để tương thích 100% trong mọi cấu hình dự án
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.f5Key.wasPressedThisFrame)
            {
                GiveShowcaseWeapons();
            }
            if (kb.f6Key.wasPressedThisFrame)
            {
                SpawnResourceNodes();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.F5))
        {
            GiveShowcaseWeapons();
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            SpawnResourceNodes();
        }
#endif
    }

    public void GiveShowcaseWeapons()
    {
        var uiManager = FindObjectOfType<LPC_UI_Manager>();
        if (uiManager == null)
        {
            Debug.LogError("[Showcase] LPC_UI_Manager not found in scene!");
            return;
        }

        // Tạo vũ khí 1: Kiếm Lửa (Burn Effect)
        LPCItemData fireSword = ScriptableObject.CreateInstance<LPCItemData>();
        fireSword.itemName = "Flame Brand";
        fireSword.category = "Weapon";
        fireSword.itemCategory = LPCItemCategory.Weapon;
        fireSword.rarity = ItemRarity.Rare;
        fireSword.description = "A magnificent heavy sword forged in volcanic lava. Screshes enemies and infuses attacks with fire. Applies Burn on hit (+12 dmg over 4 seconds).";
        fireSword.weaponType = LPCPlayerController2.WeaponType.OneHand_Slash;
        fireSword.attackRange = 1.3f;
        fireSword.knockbackForce = 5.0f;
        fireSword.enchantmentEffect = LPC_BuffManager.BuffType.Burn;
        fireSword.enchantmentValue = 12f; // Deal 12 dmg (6 dmg per tick)
        fireSword.enchantmentDuration = 4f;

        // Tạo vũ khí 2: Cung Băng (Freeze / Slow Effect)
        LPCItemData iceBow = ScriptableObject.CreateInstance<LPCItemData>();
        iceBow.itemName = "Frost Breath";
        iceBow.category = "Weapon";
        iceBow.itemCategory = LPCItemCategory.Weapon;
        iceBow.rarity = ItemRarity.Rare;
        iceBow.description = "An ancient bow carved from everlasting glacier ice. Shoots cold winds. Applies Freeze on hit, slowing enemies by 50% for 3 seconds.";
        iceBow.weaponType = LPCPlayerController2.WeaponType.Bow_Shoot;
        iceBow.isRanged = true;
        iceBow.attackRange = 5.5f;
        iceBow.knockbackForce = 2.5f;
        iceBow.enchantmentEffect = LPC_BuffManager.BuffType.Freeze;
        iceBow.enchantmentValue = 0.5f; // Slow factor 50%
        iceBow.enchantmentDuration = 3f;

        // Tạo vũ khí 3: Thương Độc (Bleed Effect)
        LPCItemData venomSpear = ScriptableObject.CreateInstance<LPCItemData>();
        venomSpear.itemName = "Viper Spear";
        venomSpear.category = "Weapon";
        venomSpear.itemCategory = LPCItemCategory.Weapon;
        venomSpear.rarity = ItemRarity.Epic;
        venomSpear.description = "A long bone spear coated in necro-venom. Pierces through defense. Applies Bleed on hit (+16 dmg over 5 seconds when moving).";
        venomSpear.weaponType = LPCPlayerController2.WeaponType.Thrust;
        venomSpear.attackRange = 1.8f;
        venomSpear.knockbackForce = 4.0f;
        venomSpear.enchantmentEffect = LPC_BuffManager.BuffType.Bleed;
        venomSpear.enchantmentValue = 16f; // Bleed damage ticks
        venomSpear.enchantmentDuration = 5f;

        // Thêm vào hòm đồ
        uiManager.inventory.Add(fireSword);
        uiManager.inventory.Add(iceBow);
        uiManager.inventory.Add(venomSpear);

        // Đánh dấu bẩn để Grid vẽ lại giao diện lập tức
        uiManager.GetType().GetMethod("MarkInventoryDirty", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(uiManager, null);
        uiManager.RefreshAll();

        // Nảy chữ thông báo hoành tráng trên đầu Player
        if (playerTransform != null)
        {
            SpawnFloatingShowcaseText(playerTransform.position + Vector3.up * 0.8f, "ENCHANTED WEAPONS ADDED (F5)!", new Color(0.7f, 0.2f, 1f));
        }

        Debug.Log("[Showcase] Showcase weapons (Flame Brand, Frost Breath, Viper Spear) successfully added to player's inventory!");
    }

    public void SpawnResourceNodes()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        Vector3 playerPos = playerTransform.position;

        // 1. Spawning Copper Ore (Left)
        CreateNode("Copper Ore Node", playerPos + Vector3.left * 1.5f, LPC_Harvestable.ResourceType.CopperOre, "Copper Ore", LPCItemCategory.Material, 3, new Color(0.85f, 0.45f, 0.25f));

        // 2. Spawning Gold Ore (Right - Hard node, requires 5 hits)
        CreateNode("Gold Ore Node", playerPos + Vector3.right * 1.5f, LPC_Harvestable.ResourceType.GoldOre, "Gold Ore", LPCItemCategory.Material, 5, new Color(1.0f, 0.85f, 0.1f));

        // 3. Spawning Medicinal Herb (Up)
        CreateNode("Medicinal Herb Node", playerPos + Vector3.up * 1.3f, LPC_Harvestable.ResourceType.MedicinalHerb, "Medicinal Herb", LPCItemCategory.Consumable, 2, new Color(0.2f, 0.8f, 0.3f));

        // 4. Spawning Wild Mushroom (Down)
        CreateNode("Wild Mushroom Node", playerPos + Vector3.down * 1.3f, LPC_Harvestable.ResourceType.WildMushroom, "Wild Mushroom", LPCItemCategory.Consumable, 2, new Color(0.9f, 0.2f, 0.2f));

        // Nảy chữ thông báo trên đầu Player
        SpawnFloatingShowcaseText(playerPos + Vector3.up * 0.8f, "ORE & FORAGE SPAWNED (F6)!", new Color(0.1f, 0.8f, 0.9f));
        Debug.Log("[Showcase] Spawned Copper Ore, Gold Ore, Medicinal Herb, and Wild Mushroom around the Player!");
    }

    private void CreateNode(string nodeName, Vector3 pos, LPC_Harvestable.ResourceType resType, string lootName, LPCItemCategory lootCat, int maxHits, Color color)
    {
        GameObject nodeObj = new GameObject(nodeName);
        nodeObj.transform.position = pos;

        // Thêm SpriteRenderer và BoxCollider2D
        var sr = nodeObj.AddComponent<SpriteRenderer>();
        var bc = nodeObj.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.8f, 0.8f);

        // Tạo LPC_Harvestable component
        var harv = nodeObj.AddComponent<LPC_Harvestable>();
        harv.resourceType = resType;
        harv.maxHits = maxHits;
        harv.hitParticleColor = color;

        // Cấu hình Item loot
        LPCItemData lootData = ScriptableObject.CreateInstance<LPCItemData>();
        lootData.itemName = lootName;
        lootData.category = lootCat.ToString();
        lootData.itemCategory = lootCat;
        lootData.rarity = ItemRarity.Common;
        lootData.description = $"A resource node harvest product: {lootName}. Used in crafting and farm production.";

        harv.lootItem = lootData;
        harv.minLoot = 1;
        harv.maxLoot = 3;

        // Gán Physics Layer là "enemy" để đòn chém quét được
        int enemyLayer = LayerMask.NameToLayer("enemy");
        if (enemyLayer < 0) enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) nodeObj.layer = enemyLayer;
    }

    private void SpawnFloatingShowcaseText(Vector3 pos, string text, Color color)
    {
        GameObject textObj = new GameObject("Runtime_ShowcaseText");
        textObj.transform.position = pos;
        TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 4.5f;

        var ft = textObj.AddComponent<LPC_FloatingText>();
        ft.Setup(text, color, 1.3f);
    }
}
