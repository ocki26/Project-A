using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class LPCEquipmentManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    //  Events
    // -------------------------------------------------------------------------
    public event Action<string, LPCItemData> OnItemEquipped;
    public event Action<string> OnItemUnequipped;
    public event Action<CharacterLoadout> OnLoadoutChanged;

    // -------------------------------------------------------------------------
    //  Weight Tiers
    // -------------------------------------------------------------------------
    public enum WeightTier { Normal, Encumbered, HeavyEncumbered }

    // -------------------------------------------------------------------------
    //  Slot Definition
    // -------------------------------------------------------------------------
    [System.Serializable]
    public class SlotDefinition
    {
        public string slotName;
        public string displayName;
        public Sprite slotIcon;
        public string[] allowedCategories;
        public bool isAppearance;
        public bool alwaysOn = false;
    }

    // -------------------------------------------------------------------------
    //  Inspector Fields
    // -------------------------------------------------------------------------
    [Header("Weight")]
    public float maxCarryWeight = 60f;

    [Header("Slot Definitions (leave empty to auto-generate all 26 slots)")]
    public List<SlotDefinition> slotDefinitions = new List<SlotDefinition>();

    [Header("Character Visual Root (the Player GameObject with Animator)")]
    public Transform characterRoot;

    [Header("Outfit Schedule (optional  assign for auto time-based outfit)")]
    public OutfitSchedule outfitSchedule;

    // -------------------------------------------------------------------------
    //  Default Equipment
    //  Assign items here in the Inspector  they will be equipped automatically
    //  when the game starts (Start). Use this for default hair, clothes, etc.
    // -------------------------------------------------------------------------
    [System.Serializable]
    public class DefaultSlotEntry
    {
        [Tooltip("Slot name: Weapon, Torso, HairFront, Legs, etc.")]
        public string slotName;
        public LPCItemData item;
    }

    [Header("Default Equipment (auto-equipped on Start)")]
    public List<DefaultSlotEntry> defaultEquipment = new List<DefaultSlotEntry>();

    // -------------------------------------------------------------------------
    //  Front ? Behind mirror pairs
    //
    //  When a front slot (Weapon, Shield) is equipped, the behind variant child
    //  gets the SAME SpriteLibrary automatically.  LPCSpriteSync then controls
    //  which layer is actually visible based on character direction:
    //    Front  visible when facing Down / Left / Right
    //    Behind visible when facing Up
    // -------------------------------------------------------------------------
    private static readonly Dictionary<string, string> FrontToBehind = new()
    {
        { "Weapon", "WeaponBehind" },
        { "Shield", "ShieldBehind" },
    };

    // -------------------------------------------------------------------------
    //  Internal State
    // -------------------------------------------------------------------------
    private readonly Dictionary<string, LPCItemData> _equipped =
        new Dictionary<string, LPCItemData>();

    private CharacterLoadout _activeLoadout;

    // =========================================================================
    //  Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        Debug.Log($"[LPCEquipment Debug] Awake: slotDefinitions.Count = {slotDefinitions.Count}");
        for (int i = 0; i < slotDefinitions.Count; i++)
        {
            var s = slotDefinitions[i];
            Debug.Log($"  Slot {i}: slotName='{s.slotName}', displayName='{s.displayName}'");
        }
        
        bool hasValidSlots = slotDefinitions.Count > 0 &&
                             slotDefinitions.Any(s => !string.IsNullOrEmpty(s.slotName));
                             
        Debug.Log($"[LPCEquipment Debug] hasValidSlots = {hasValidSlots}");

        if (!hasValidSlots)
        {
            Debug.Log("[LPCEquipment Debug] Calling InitDefaultSlots()");
            InitDefaultSlots();
            Debug.Log($"[LPCEquipment Debug] After InitDefaultSlots: count = {slotDefinitions.Count}");
        }

        foreach (var s in slotDefinitions)
            s.allowedCategories ??= Array.Empty<string>();

        if (characterRoot == null)
            characterRoot = transform;
    }

    private void Start()
    {
        // Auto-attach LPCSpriteSync to pre-configured equipment GameObjects in hierarchy
        var animatorDriven = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Body", "Ears", "Eyes", "Underwear"
        };
        for (int i = 0; i < characterRoot.childCount; i++)
        {
            var child = characterRoot.GetChild(i);
            if (animatorDriven.Contains(child.name)) continue;

            var resolver = child.GetComponent<UnityEngine.U2D.Animation.SpriteResolver>();
            var library = child.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
            if (resolver != null && library != null)
            {
                var sync = child.GetComponent<LPCSpriteSync>();
                if (sync == null)
                {
                    sync = child.gameObject.AddComponent<LPCSpriteSync>();
                    // Determine visibilityMode
                    bool hasBehind = FrontToBehind.ContainsKey(child.name);
                    bool isBehind = FrontToBehind.ContainsValue(child.name);
                    var syncMode = hasBehind ? LPCSpriteSync.VisibilityMode.FrontPaired
                                 : isBehind ? LPCSpriteSync.VisibilityMode.BehindPaired
                                 : LPCSpriteSync.VisibilityMode.AlwaysVisible;
                    sync.Initialize(syncMode);
                }
            }
        }

        if (defaultEquipment == null || defaultEquipment.Count == 0) return;

        foreach (var entry in defaultEquipment)
        {
            if (entry == null || entry.item == null || string.IsNullOrEmpty(entry.slotName)) continue;
            EquipItem(entry.slotName, entry.item);
        }

        // Single Rebind after all default items are equipped
        RebindParentAnimator();
    }

    // =========================================================================
    //  Default 26-Slot Layout
    // =========================================================================

    private void InitDefaultSlots()
    {
        slotDefinitions = new List<SlotDefinition>
        {
            new SlotDefinition { slotName="Shadow",      displayName="Shadow",       allowedCategories=new string[0],              isAppearance=true,  alwaysOn=true  },
            new SlotDefinition { slotName="Body",        displayName="Body",         allowedCategories=new[]{"Body"},              isAppearance=true,  alwaysOn=true  },
            new SlotDefinition { slotName="Ears",        displayName="Ears",         allowedCategories=new[]{"Ears"},              isAppearance=true,  alwaysOn=true  },
            new SlotDefinition { slotName="Eyes",        displayName="Eyes",         allowedCategories=new[]{"Eyes"},              isAppearance=true,  alwaysOn=true  },
            new SlotDefinition { slotName="Underwear",   displayName="Underwear",    allowedCategories=new[]{"Underwear"},         isAppearance=true,  alwaysOn=true  },
            new SlotDefinition { slotName="HairBehind",  displayName="Hair Behind",  allowedCategories=new[]{"Hair"},              isAppearance=true,  alwaysOn=false },
            new SlotDefinition { slotName="HairFront",   displayName="Hair Front",   allowedCategories=new[]{"Hair"},              isAppearance=true,  alwaysOn=false },
            new SlotDefinition { slotName="FacialHair",  displayName="Facial Hair",  allowedCategories=new[]{"FacialHair","Hair"}, isAppearance=true,  alwaysOn=false },
            new SlotDefinition { slotName="Effects",     displayName="Effects",      allowedCategories=new string[0],              isAppearance=true,  alwaysOn=false },
            new SlotDefinition { slotName="Helmet",      displayName="Helmet",       allowedCategories=new[]{"Armor","Clothing"},  isAppearance=false },
            new SlotDefinition { slotName="Mask",        displayName="Mask",         allowedCategories=new[]{"Armor","Clothing"},  isAppearance=false },
            new SlotDefinition { slotName="Shoulders",   displayName="Shoulders",    allowedCategories=new[]{"Armor"},             isAppearance=false },
            new SlotDefinition { slotName="Armor",       displayName="Chest Armor",  allowedCategories=new[]{"Armor"},             isAppearance=false },
            new SlotDefinition { slotName="Torso",       displayName="Torso/Shirt",  allowedCategories=new[]{"Clothing","Armor"},  isAppearance=false },
            new SlotDefinition { slotName="Arms",        displayName="Arms",         allowedCategories=new[]{"Armor","Clothing"},  isAppearance=false },
            new SlotDefinition { slotName="Gloves",      displayName="Gloves",       allowedCategories=new[]{"Armor","Clothing"},  isAppearance=false },
            new SlotDefinition { slotName="Belt",        displayName="Belt",         allowedCategories=new[]{"Armor","Clothing"},  isAppearance=false },
            new SlotDefinition { slotName="Legs",        displayName="Legs/Pants",   allowedCategories=new[]{"Armor","Clothing"},  isAppearance=false },
            new SlotDefinition { slotName="Feet",        displayName="Feet/Boots",   allowedCategories=new[]{"Armor","Clothing"},  isAppearance=false },
            new SlotDefinition { slotName="Neck",        displayName="Necklace",     allowedCategories=new[]{"Accessory"},         isAppearance=false },
            new SlotDefinition { slotName="Weapon",      displayName="Weapon",       allowedCategories=new[]{"Weapon"},            isAppearance=false },
            new SlotDefinition { slotName="Shield",      displayName="Shield",       allowedCategories=new[]{"Weapon","Armor"},    isAppearance=false },
            new SlotDefinition { slotName="WeaponBehind",displayName="Weapon Back",  allowedCategories=new[]{"Weapon"},            isAppearance=false },
            new SlotDefinition { slotName="ShieldBehind",displayName="Shield Back",  allowedCategories=new[]{"Weapon","Armor"},    isAppearance=false },
            new SlotDefinition { slotName="CapeBehind",  displayName="Cape",         allowedCategories=new[]{"Clothing"},          isAppearance=false },
            new SlotDefinition { slotName="Quiver",      displayName="Quiver",       allowedCategories=new[]{"Weapon"},            isAppearance=false },
        };
    }

    // =========================================================================
    //  Slot Queries
    // =========================================================================

    public IEnumerable<string> GetEquipmentSlotNames() =>
        slotDefinitions.Where(s => !s.isAppearance).Select(s => s.slotName);

    public IEnumerable<string> GetAppearanceSlotNames() =>
        slotDefinitions.Where(s => s.isAppearance).Select(s => s.slotName);

    public SlotDefinition GetSlotDefinition(string slotName) =>
        slotDefinitions.FirstOrDefault(s =>
            s.slotName.Equals(slotName, StringComparison.OrdinalIgnoreCase));

    public LPCItemData GetEquipped(string slotName) =>
        _equipped.TryGetValue(slotName, out var item) ? item : null;

    public Dictionary<string, LPCItemData> GetAllEquipped() => _equipped;

    // =========================================================================
    //  Equip / Unequip
    // =========================================================================

    public bool EquipItem(LPCItemData item)
    {
        if (item == null) return false;
        return EquipItem(item.childPath, item);
    }

    public bool EquipItem(string slotName, LPCItemData item)
    {
        if (item == null || string.IsNullOrEmpty(slotName)) return false;

        var def = GetSlotDefinition(slotName);
        if (def == null)
        {
            def = slotDefinitions.FirstOrDefault(s =>
                s.allowedCategories != null &&
                s.allowedCategories.Contains(item.category, StringComparer.OrdinalIgnoreCase));
        }

        if (def == null)
        {
            def = slotDefinitions.FirstOrDefault(s =>
                s.allowedCategories != null &&
                s.allowedCategories.Contains(item.category, StringComparer.OrdinalIgnoreCase));
        }

        if (def == null)
        {
            Debug.LogWarning($"[LPCEquipment] No slot found for '{slotName}' or category '{item.category}'.");
            return false;
        }

        bool categoryAllowed = def.allowedCategories == null
                            || def.allowedCategories.Length == 0
                            || def.allowedCategories.Contains(item.category, StringComparer.OrdinalIgnoreCase);

        if (!categoryAllowed)
        {
            Debug.LogWarning($"[LPCEquipment] Cannot equip '{item.category}' into '{def.slotName}'.");
            return false;
        }

        // Tự động tạo bản sao động (Runtime Instance) trên RAM để bảo toàn ScriptableObject gốc an toàn tuyệt đối
        LPCItemData runtimeItem = Instantiate(item);

        _equipped[def.slotName] = runtimeItem;
        Debug.Log($"[LPCEquipment] Equipped Runtime Instance of '{runtimeItem.itemName}' -> '{def.slotName}'.");

        UpdateCharacterVisuals(def.slotName, runtimeItem);

        if (def.slotName.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
        {
            var player = GetComponent<LPCPlayerController2>();
            if (player != null)
            {
                player.EquipWeapon(runtimeItem.weaponType);
            }
        }

        OnItemEquipped?.Invoke(def.slotName, runtimeItem);
        return true;
    }

    public void UnequipItem(string slotName)
    {
        if (_equipped.Remove(slotName))
        {
            Debug.Log($"[LPCEquipment] Unequipped slot '{slotName}'.");
            UpdateCharacterVisuals(slotName, null);

            if (slotName.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
            {
                var player = GetComponent<LPCPlayerController2>();
                if (player != null)
                {
                    player.EquipWeapon(LPCPlayerController2.WeaponType.Unarmed_Slash);
                }
            }

            OnItemUnequipped?.Invoke(slotName);
        }
    }

    // =========================================================================
    //  Loadout
    // =========================================================================

    /// <summary>
    /// Apply a full loadout. Calls Rebind() ONCE after all slots are updated.
    /// </summary>
    public void ApplyLoadout(CharacterLoadout loadout)
    {
        if (loadout == null) return;
        if (_activeLoadout == loadout) return;
        _activeLoadout = loadout;

        Debug.Log($"[LPCEquipment] Applying loadout '{loadout.name}'.");

        foreach (var entry in loadout.entries)
        {
            if (entry.item == null || string.IsNullOrEmpty(entry.slotName)) continue;
            EquipItem(entry.slotName, entry.item);
        }

        // FIX BUG 2  single Rebind after all slots are updated.
        RebindParentAnimator();
        OnLoadoutChanged?.Invoke(loadout);
    }

    // =========================================================================
    //  Time-Based Outfit
    // =========================================================================

    public void UpdateOutfitByTime(float timeOfDay)
    {
        if (outfitSchedule == null)
        {
            Debug.LogWarning("[LPCEquipment] UpdateOutfitByTime called but outfitSchedule is not assigned.");
            return;
        }

        CharacterLoadout loadout = outfitSchedule.GetLoadoutForHour(timeOfDay);
        if (loadout == null)
        {
            Debug.LogWarning($"[LPCEquipment] No loadout in schedule for hour {timeOfDay:F1}.");
            return;
        }

        ApplyLoadout(loadout);
    }

    // =========================================================================
    //  Character Visual Update
    // =========================================================================

    private void UpdateCharacterVisuals(string slotName, LPCItemData item)
    {
        if (characterRoot == null) return;

        string targetName = (item != null && !string.IsNullOrEmpty(item.childPath))
            ? item.childPath : slotName;

        Transform part = characterRoot.Find(targetName)
                      ?? FindChildRecursive(characterRoot, targetName);

        if (part == null)
        {
            Debug.LogWarning($"[LPCEquipment] Child '{targetName}' not found.");
            return;
        }

        var sr = part.GetComponent<SpriteRenderer>() ?? part.gameObject.AddComponent<SpriteRenderer>();
        var sl = part.GetComponent<SpriteLibrary>() ?? part.gameObject.AddComponent<SpriteLibrary>();
        var resolver = part.GetComponent<SpriteResolver>() ?? part.gameObject.AddComponent<SpriteResolver>();
        var childAnim = part.GetComponent<Animator>();
        var def = GetSlotDefinition(slotName);

        if (item != null)
        {
            sr.enabled = true;

            // FIX BUG 4  Do NOT set sr.sortingOrder here.
            // SetLayerOrder.Awake() is the single source of truth for sort order.

            sl.spriteLibraryAsset = item.spriteLibrary;

            // FIX BUG 3  set category before resolving so the sprite appears immediately.
            if (item.spriteLibrary != null)
            {
                string defaultCat = PickDefaultCategory(item.spriteLibrary);
                if (!string.IsNullOrEmpty(defaultCat))
                    resolver.SetCategoryAndLabel(defaultCat, "0");
            }

            resolver.ResolveSpriteToSpriteRenderer();

            if (item.itemController != null)
            {
                if (childAnim == null) childAnim = part.gameObject.AddComponent<Animator>();
                childAnim.runtimeAnimatorController = item.itemController;
            }
            else
            {
                if (childAnim != null) Destroy(childAnim);
            }

            // Attach LPCSpriteSync for animation frame sync.
            // Determine if this slot has a front/behind pair.
            bool hasBehind = FrontToBehind.ContainsKey(slotName);
            bool isBehind = FrontToBehind.ContainsValue(slotName);

            var syncMode = hasBehind ? LPCSpriteSync.VisibilityMode.FrontPaired
                         : isBehind ? LPCSpriteSync.VisibilityMode.BehindPaired
                         : LPCSpriteSync.VisibilityMode.AlwaysVisible;

            var sync = part.GetComponent<LPCSpriteSync>() ?? part.gameObject.AddComponent<LPCSpriteSync>();
            sync.Initialize(syncMode, item);

            // DIRECTIONAL MIRROR  when equipping Weapon, also set up WeaponBehind
            // with the same SpriteLibrary so the weapon is visible when facing Up.
            if (hasBehind)
                SetupMirrorChild(FrontToBehind[slotName], item, LPCSpriteSync.VisibilityMode.BehindPaired);
        }
        else
        {
            // Unequip
            bool alwaysOn = def != null && def.alwaysOn;
            sr.enabled = alwaysOn;
            sl.spriteLibraryAsset = null;
            if (!alwaysOn) sr.sprite = null;
            if (childAnim != null) Destroy(childAnim);

            // Remove LPCSpriteSync
            var sync = part.GetComponent<LPCSpriteSync>();
            if (sync != null) Destroy(sync);

            // Clear mirror child too
            if (FrontToBehind.TryGetValue(slotName, out string behindSlotName))
                ClearMirrorChild(behindSlotName);
        }

        // FIX BUG 2  Rebind removed from here; called once in ApplyLoadout.
    }

    // =========================================================================
    //  Mirror Child Setup (WeaponBehind / ShieldBehind)
    // =========================================================================

    /// <summary>
    /// Sets up a "behind" child (e.g. WeaponBehind) with the same SpriteLibrary
    /// as the front weapon. LPCSpriteSync will control visibility (only shown when
    /// the character faces Up) and sync the correct animation frame.
    /// </summary>
    private void SetupMirrorChild(string childName, LPCItemData item,
                                  LPCSpriteSync.VisibilityMode syncMode)
    {
        Transform child = characterRoot.Find(childName)
                       ?? FindChildRecursive(characterRoot, childName);

        if (child == null)
        {
            Debug.LogWarning($"[LPCEquipment] Mirror child '{childName}' not found in hierarchy. " +
                             "Make sure the character prefab has this GameObject.");
            return;
        }

        var sr = child.GetComponent<SpriteRenderer>() ?? child.gameObject.AddComponent<SpriteRenderer>();
        var sl = child.GetComponent<SpriteLibrary>() ?? child.gameObject.AddComponent<SpriteLibrary>();
        var resolver = child.GetComponent<SpriteResolver>() ?? child.gameObject.AddComponent<SpriteResolver>();

        sl.spriteLibraryAsset = item.spriteLibrary;

        // Start with an Up-direction category so there's no frame-0 pop when
        // the character first faces Up.
        if (item.spriteLibrary != null)
        {
            string upCat = PickDirectionalCategory(item.spriteLibrary, "Up")
                        ?? PickDefaultCategory(item.spriteLibrary);
            if (!string.IsNullOrEmpty(upCat))
                resolver.SetCategoryAndLabel(upCat, "0");
        }

        resolver.ResolveSpriteToSpriteRenderer();

        // LPCSpriteSync controls sr.enabled for this child.
        var sync = child.GetComponent<LPCSpriteSync>() ?? child.gameObject.AddComponent<LPCSpriteSync>();
        sync.Initialize(syncMode, item); // BehindPaired ? sr.enabled starts false
    }

    private void ClearMirrorChild(string childName)
    {
        Transform child = characterRoot.Find(childName)
                       ?? FindChildRecursive(characterRoot, childName);
        if (child == null) return;

        var sl = child.GetComponent<SpriteLibrary>();
        if (sl != null) sl.spriteLibraryAsset = null;

        var sr = child.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.sprite = null; sr.enabled = false; }

        var sync = child.GetComponent<LPCSpriteSync>();
        if (sync != null) Destroy(sync);
    }

    // =========================================================================
    //  Category Helpers
    // =========================================================================

    private static string PickDefaultCategory(SpriteLibraryAsset library)
    {
        if (library == null) return null;
        try
        {
            var cats = library.GetCategoryNames()?.ToList();
            if (cats == null || cats.Count == 0) return null;
            // Prefer Idle_Down for a clean default pose; fall back to first available.
            if (cats.Contains("Idle_Down")) return "Idle_Down";
            if (cats.Contains("Idle")) return "Idle";
            if (cats.Contains("Walk_Down")) return "Walk_Down";
            return cats[0];
        }
        catch { return null; }
    }

    private static string PickDirectionalCategory(SpriteLibraryAsset library, string direction)
    {
        if (library == null) return null;
        try
        {
            var cats = library.GetCategoryNames()?.ToList();
            if (cats == null || cats.Count == 0) return null;

            // Try Walk_{dir}, Idle_{dir}, any _{dir}
            string[] preferred = { "Walk", "Idle", "Combat" };
            foreach (string p in preferred)
            {
                string cat = $"{p}_{direction}";
                if (cats.Contains(cat)) return cat;
            }

            var anyDir = cats.FirstOrDefault(c => c.EndsWith($"_{direction}"));
            return anyDir;
        }
        catch { return null; }
    }

    // =========================================================================
    //  Utility
    // =========================================================================

    public void RebindParentAnimator()
    {
        var anim = characterRoot.GetComponent<Animator>()
                ?? characterRoot.GetComponentInParent<Animator>();
        if (anim != null && anim.isActiveAndEnabled)
            anim.Rebind();
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // =========================================================================
    //  Weight
    // =========================================================================

    public bool CanEquip(string slotName, LPCItemData item)
    {
        if (item == null || string.IsNullOrEmpty(slotName)) return false;
        var def = GetSlotDefinition(slotName);
        if (def == null) return false;
        if (def.allowedCategories == null || def.allowedCategories.Length == 0) return true;
        return def.allowedCategories.Contains(item.category);
    }

    public float GetTotalEquippedWeight() =>
        _equipped.Values.Where(i => i != null).Sum(i => i.weight);

    public WeightTier GetWeightTier()
    {
        if (maxCarryWeight <= 0f) return WeightTier.Normal;
        float ratio = GetTotalEquippedWeight() / maxCarryWeight;
        return ratio > 0.75f ? WeightTier.HeavyEncumbered
             : ratio > 0.50f ? WeightTier.Encumbered
             : WeightTier.Normal;
    }

    public float GetWeightSpeedMultiplier() => GetWeightTier() switch
    {
        WeightTier.HeavyEncumbered => 0.5f,
        WeightTier.Encumbered => 0.8f,
        _ => 1.0f,
    };
}