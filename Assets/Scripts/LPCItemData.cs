using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections.Generic;

// =============================================================================
//  ENUMS
// =============================================================================

public enum ItemRarity
{
    Common    = 0,
    Uncommon  = 1,
    Rare      = 2,
    Epic      = 3,
    Legendary = 4,
}

public enum LPCItemCategory
{
    Weapon,     // Vũ khí
    Armor,      // Giáp
    Seed,       // Hạt giống
    Crop,       // Nông sản thu hoạch
    Material,   // Nguyên liệu khai thác (Gỗ, Đá, Quặng)
    Consumable, // Đồ ăn hồi phục
    Special     // Đặc biệt (Chìa khóa rương)
}


public enum DirectionalVisibility
{
    Default = 0,   // Mặc định theo thiết lập tự động của hệ thống
    Front = 1,     // Ép hiển thị ở lớp trước (Front Layer)
    Behind = 2,    // Ép hiển thị ở lớp sau (Behind Layer / Sau lưng)
    Hidden = 3,    // Ẩn hoàn toàn trang bị khi ở hướng này
    Visible = 4,   // Luôn hiển thị ở hướng này
}

// =============================================================================
//  STAT STRUCTS
// =============================================================================

[System.Serializable]
public struct CoreStats
{
    [Tooltip("Bonus max HP")]          public float maxHP;
    [Tooltip("Bonus max MP")]          public float maxMP;
    [Tooltip("Bonus max Stamina")]     public float maxStamina;
    [Tooltip("HP regeneration / sec")] public float hpRegen;
    [Tooltip("MP regeneration / sec")] public float mpRegen;
}

[System.Serializable]
public struct OffensiveStats
{
    [Tooltip("Physical attack")]                          public float atk;
    [Tooltip("Magic attack")]                             public float matk;
    [Range(0, 1), Tooltip("Critical hit chance (0-1)")]  public float critRate;
    [Tooltip("Critical damage multiplier (1.5 = 150%)")] public float critDamage;
    [Tooltip("Normal attack speed")]                      public float attackSpeed;
    [Tooltip("Skill cast speed")]                         public float castSpeed;
    [Tooltip("Physical armor penetration")]               public float armorPenetration;
    [Tooltip("Magic resistance penetration")]             public float magicPenetration;
    [Tooltip("Final damage multiplier (1.1 = +10%)")]    public float finalDamageMultiplier;
    [Tooltip("True damage — ignores armor")]              public float trueDamage;
    [Range(0, 1), Tooltip("Physical lifesteal (0.1 = 10%)")] public float lifesteal;
    [Range(0, 1), Tooltip("Spell vamp (0.1 = 10%)")]          public float spellVamp;
}

[System.Serializable]
public struct DefensiveStats
{
    [Tooltip("Physical armor")]                               public float def;
    [Tooltip("Magic resistance")]                             public float mdef;
    [Range(0, 1), Tooltip("Dodge chance")]                   public float dodge;
    [Range(0, 1), Tooltip("Block chance")]                   public float blockRate;
    [Range(0, 1), Tooltip("Damage reduction when blocking")] public float blockAmount;
    [Range(0, 1), Tooltip("Flat damage reduction")]          public float damageReduction;
    [Tooltip("Shield — absorbs damage before HP")]            public float shield;
    [Range(0, 1), Tooltip("Crowd control resistance")]        public float tenacity;
}

[System.Serializable]
public struct UtilityStats
{
    [Tooltip("Bonus movement speed")]                                   public float moveSpeed;
    [Range(0, 1), Tooltip("Cooldown reduction (0.2 = 20%)")]           public float cooldownReduction;
    [Tooltip("Increases incoming healing by this percentage")]          public float healingBonus;
    [Range(0, 1), Tooltip("Extends duration of effects you apply")]    public float effectDurationBonus;
    [Range(0, 1), Tooltip("Reduces duration of effects applied to you")] public float effectResistance;
}

[System.Serializable]
public struct AttributeStats
{
    [Tooltip("Strength   -> ATK, DEF")]            public int str;
    [Tooltip("Dexterity  -> CritRate, AtkSpeed")]  public int dex;
    [Tooltip("Intelligence -> MATK, MaxMP")]        public int @int;
    [Tooltip("Vitality   -> MaxHP, HPRegen")]       public int vit;
    [Tooltip("Agility    -> MoveSpeed, AtkSpeed")]  public int agi;
    [Tooltip("Luck       -> CritRate, DropRate")]   public int luk;
}

[System.Serializable]
public struct ElementStats
{
    [Header("Elemental Damage")]
    public float fireDamage;
    public float iceDamage;
    public float lightningDamage;
    public float darkDamage;
    public float lightDamage;

    [Header("Elemental Resistance")]
    public float fireResistance;
    public float iceResistance;
    public float lightningResistance;
    public float darkResistance;
    public float lightResistance;
}

[System.Serializable]
public struct SpecialStats
{
    [Tooltip("Bonus gold gained (%)")]                     public float goldBonus;
    [Tooltip("Increases item drop rate (%)")]              public float dropRate;
    [Tooltip("Bonus experience gained (%)")]               public float expBonus;
    [Tooltip("Execute enemies below X% HP (0 = off)")]    public float executeDamageThreshold;
    [Tooltip("Reflect a percentage of damage received")]   public float reflectDamage;
    [Tooltip("Aggro / threat multiplier (for tanks)")]    public float aggro;
}

// =============================================================================
//  LPCItemData  — Main item ScriptableObject
//
//  IMPORTANT: The menu path "LPC/LPC Item Data" is intentionally different from
//  "LPC/Item Data" used by any other ItemData class. Having two ScriptableObjects
//  with the same CreateAssetMenu path causes silent asset-creation conflicts.
// =============================================================================

[CreateAssetMenu(menuName = "LPC/LPC Item Data", fileName = "NewItem_LPCItemData")]
public class LPCItemData : ScriptableObject
{
    // -------------------------------------------------------------------------
    //  Identity
    // -------------------------------------------------------------------------
    [Header("--- Identity ---")]
    public string     itemName;
    [Tooltip("Must match the child GameObject name in the character hierarchy.")]
    public string     childPath;
    public string     category;
    public int        sortingOrder;
    [TextArea(3, 6)]
    public string     description;
    public Sprite     icon;
    public Sprite     thumbnail => icon;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("--- Stardew Valley & Enchantment ---")]
    [Tooltip("Phân loại vật phẩm Stardew Valley")]
    public LPCItemCategory itemCategory = LPCItemCategory.Weapon;

    [Tooltip("Tích chọn để bắt buộc hiển thị chỉ số vũ khí (kể cả khi không phải là thể loại Weapon)")]
    public bool forceShowWeaponStats = false;
    
    [Tooltip("Hiệu ứng xấu/tốt của vũ khí khi đánh trúng quái (Hỗ trợ hệ thống Enchant)")]
    public LPC_BuffManager.BuffType enchantmentEffect = LPC_BuffManager.BuffType.None;
    
    [Tooltip("Cường độ hiệu ứng (Ví dụ: 10 sát thương đốt, hoặc 5 giây làm chậm)")]
    public float enchantmentValue = 0f;
    
    [Tooltip("Thời gian tác dụng của hiệu ứng (giây)")]
    public float enchantmentDuration = 0f;

    [Header("--- Weapon Specific ---")]
    [Tooltip("Attack animation type to use when this weapon is equipped.")]
    public LPCPlayerController2.WeaponType weaponType = LPCPlayerController2.WeaponType.Unarmed_Slash;
    [Tooltip("Tầm đánh của vũ khí này")]
    public float attackRange = 1.2f;
    [Tooltip("Góc quét sát thương (đối với cận chiến)")]
    public float attackAngle = 90f;
    [Tooltip("Vũ khí tầm xa bắn đạn/mũi tên")]
    public bool isRanged = false;
    [Tooltip("Lực đẩy lùi của vũ khí này khi đánh trúng quái")]
    public float knockbackForce = 4.0f;
    [Tooltip("Thời gian đẩy lùi (giây) của vũ khí này")]
    public float knockbackDuration = 0.2f;

    // -------------------------------------------------------------------------
    //  Weight
    // -------------------------------------------------------------------------
    [Header("--- Weight ---")]
    [Tooltip("Item weight in kg. Affects movement speed when total equipped weight exceeds 50% / 75% of max carry weight.")]
    [Min(0f)]
    public float weight = 1f;

    // -------------------------------------------------------------------------
    //  Durability
    // -------------------------------------------------------------------------
    [Header("--- Durability ---")]
    public float maxDurability     = 100f;
    [Tooltip("When currentDurability reaches 0, the item is Broken and deals only 25% damage.")]
    public float currentDurability = 100f;
    [Tooltip("Hệ số tiêu hao độ bền (1.0 = mặc định. Ví dụ: 0.5 là bền gấp đôi, 2.0 là dễ hỏng gấp đôi, 0 là không bao giờ hỏng)")]
    public float durabilityLossMultiplier = 1.0f;
    [Tooltip("Tự động khôi phục độ bền đầy 100% khi bấm Play game (Hữu ích khi test trong Editor).")]
    public bool autoResetDurabilityOnPlay = true;

    private void OnEnable()
    {
        if (autoResetDurabilityOnPlay)
        {
            FullRepair();
        }
    }

    // -------------------------------------------------------------------------
    //  Sprite / Animation
    // -------------------------------------------------------------------------
    [Header("--- Sprite / Animation ---")]
    public SpriteLibraryAsset          spriteLibrary;
    public int                         spriteWidth    = 64;
    public int                         spriteHeight   = 64;
    public int                         pixelsPerUnit  = 64;
    public int                         frameRate      = 8;
    public List<AnimationClip>         clips          = new();
    [Tooltip("Optional per-item Animator Controller (e.g. for FX items).")]
    public RuntimeAnimatorController   itemController;

    [Header("--- Custom Directional Visibility (Optional) ---")]
    [Tooltip("Cấu hình hiển thị hướng đi lên (Up).")]
    public DirectionalVisibility visibilityUp = DirectionalVisibility.Default;
    [Tooltip("Cấu hình hiển thị hướng bên trái (Left).")]
    public DirectionalVisibility visibilityLeft = DirectionalVisibility.Default;
    [Tooltip("Cấu hình hiển thị hướng đi xuống (Down).")]
    public DirectionalVisibility visibilityDown = DirectionalVisibility.Default;
    [Tooltip("Cấu hình hiển thị hướng bên phải (Right).")]
    public DirectionalVisibility visibilityRight = DirectionalVisibility.Default;

    // -------------------------------------------------------------------------
    //  Stats
    // -------------------------------------------------------------------------
    [Header("--- Core Stats (HP / MP / Stamina) ---")]
    public CoreStats      core;
    [Header("--- Offensive Stats ---")]
    public OffensiveStats offensive;
    [Header("--- Defensive Stats ---")]
    public DefensiveStats defensive;
    [Header("--- Utility Stats ---")]
    public UtilityStats   utility;
    [Header("--- Attributes ---")]
    public AttributeStats attributes;
    [Header("--- Elemental ---")]
    public ElementStats   elemental;
    [Header("--- Special Stats ---")]
    public SpecialStats   special;

    // -------------------------------------------------------------------------
    //  Computed Properties
    // -------------------------------------------------------------------------

    public bool  IsBroken         => currentDurability <= 0f;
    public float DamageMultiplier => IsBroken ? 0.25f : 1f;
    public float DurabilityRatio  => maxDurability > 0 ? currentDurability / maxDurability : 0f;

    public void TakeDurabilityDamage(float amount) =>
        currentDurability = Mathf.Clamp(currentDurability - amount, 0f, maxDurability);

    public void Repair(float amount) =>
        currentDurability = Mathf.Clamp(currentDurability + amount, 0f, maxDurability);

    public void FullRepair() => currentDurability = maxDurability;

    // -------------------------------------------------------------------------
    //  Rarity Helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns the UI tint color for a given rarity level.</summary>
    public static Color RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common    => new Color(0.80f, 0.80f, 0.80f),
        ItemRarity.Uncommon  => new Color(0.30f, 0.85f, 0.30f),
        ItemRarity.Rare      => new Color(0.25f, 0.60f, 1.00f),
        ItemRarity.Epic      => new Color(0.75f, 0.25f, 1.00f),
        ItemRarity.Legendary => new Color(1.00f, 0.65f, 0.10f),
        _                    => Color.white,
    };

    public Color RarityColor() => RarityColor(rarity);

    /// <summary>Returns a plain-text rarity label (no emoji) safe for any Unity font.</summary>
    public static string RarityLabel(ItemRarity r) => r switch
    {
        ItemRarity.Common    => "Common",
        ItemRarity.Uncommon  => "Uncommon",
        ItemRarity.Rare      => "Rare",
        ItemRarity.Epic      => "Epic",
        ItemRarity.Legendary => "Legendary",
        _                    => "Unknown",
    };

    // -------------------------------------------------------------------------
    //  Validation
    // -------------------------------------------------------------------------

    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(childPath))
            childPath = childPath.Trim();
    }
}
