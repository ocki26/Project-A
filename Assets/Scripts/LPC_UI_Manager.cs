using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(UIDocument))]
public class LPC_UI_Manager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    //  Inspector References
    // -------------------------------------------------------------------------
    [Header("References")]
    public LPCEquipmentManager equipmentManager;

    [Header("Inventory")]
    public List<LPCItemData> inventory = new();

    [Header("Player Info")]
    public string playerName  = "Hero";
    public int    playerLevel = 1;
    public int    gold        = 4280;

    [Header("Base Stats")]
    public int baseATK = 100;
    public int baseDEF = 60;
    public int baseSPD = 20;

    [Header("HUD Document")]
    public UIDocument hudDocument;

    // -------------------------------------------------------------------------
    //  Private State
    // -------------------------------------------------------------------------
    private VisualElement _root;
    private VisualElement _backdrop;

    private enum PanelMode { Equipment, Stats }
    private PanelMode _panelMode    = PanelMode.Equipment;
    private string    _currentFilter = "All";
    private Button    _activeFilterBtn;

    // Save Slots Popup
    private bool          _popupIsSaveMode = true;
    private VisualElement _saveSlotPopup;
    private Label         _popupTitle;
    private Label         _slotInfo1;
    private Label         _slotInfo2;
    private Label         _slotInfo3;

    private static readonly string[] SortCycles = { "name", "type", "rarity", "weight" };
    private int  _sortIndex     = 0;
    private bool _sortAscending = true;
    private string CurrentSort => SortCycles[_sortIndex];

    private LPCItemData    _selectedItem;
    private VisualElement  _selectedCell;

    // Drag & Drop
    private bool          _isDragging  = false;
    private LPCItemData   _dragItem    = null;
    private VisualElement _dragGhost   = null;
    private VisualElement _dragSource  = null;

    // Dirty flag — only rebuild the grid when something actually changed
    private bool _inventoryDirty = true;

    // =========================================================================
    //  Unity Lifecycle
    // =========================================================================

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("[LPC_UI] UIDocument component missing!");
            return;
        }

        _root    = doc.rootVisualElement;
        _backdrop = _root.Q<VisualElement>("Backdrop");

        BindAll();
        RefreshAll();
    }

    private void Start() => HideUI();

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame) ToggleUI();

        if (kb.escapeKey.wasPressedThisFrame &&
            _backdrop?.resolvedStyle.display == DisplayStyle.Flex)
            HideUI();

        // Refresh UI in real-time while open
        if (_backdrop != null && _backdrop.resolvedStyle.display == DisplayStyle.Flex)
        {
            RefreshAll();
        }
    }

    // =========================================================================
    //  Binding
    // =========================================================================

    private void BindAll()
    {
        _root.Q<Button>("CloseButton")?.RegisterCallback<ClickEvent>(_ => HideUI());
        _root.Q<Button>("Tab_Equipment") ?.RegisterCallback<ClickEvent>(_ => SetPanelMode(PanelMode.Equipment));
        _root.Q<Button>("Tab_Stats")     ?.RegisterCallback<ClickEvent>(_ => SetPanelMode(PanelMode.Stats));

        // Save / Load buttons
        _root.Q<Button>("SaveBtn")?.RegisterCallback<ClickEvent>(_ => OnSaveClicked());
        _root.Q<Button>("LoadBtn")?.RegisterCallback<ClickEvent>(_ => OnLoadClicked());

        // Popup bindings
        _saveSlotPopup = _root.Q<VisualElement>("SaveSlotPopup");
        _popupTitle = _root.Q<Label>("PopupTitle");
        _slotInfo1 = _root.Q<Label>("SlotInfo_1");
        _slotInfo2 = _root.Q<Label>("SlotInfo_2");
        _slotInfo3 = _root.Q<Label>("SlotInfo_3");

        _root.Q<Button>("BtnSlot_1")?.RegisterCallback<ClickEvent>(_ => OnSlotClicked(1));
        _root.Q<Button>("BtnSlot_2")?.RegisterCallback<ClickEvent>(_ => OnSlotClicked(2));
        _root.Q<Button>("BtnSlot_3")?.RegisterCallback<ClickEvent>(_ => OnSlotClicked(3));
        _root.Q<Button>("BtnSlotCancel")?.RegisterCallback<ClickEvent>(_ => HideSlotPopup());

        // Equipment filter buttons
        BindFilter("Filter_All",      "All");
        BindFilter("Filter_Weapon",   "Weapon");
        BindFilter("Filter_Armor",    "Armor");
        BindFilter("Filter_Clothing", "Clothing");
        BindFilter("Filter_Other",    "Other");

        _activeFilterBtn = _root.Q<Button>("Filter_All");

        // Stat distribution buttons
        BindAddStatButton("BtnAdd_STR", "STR");
        BindAddStatButton("BtnAdd_DEX", "DEX");
        BindAddStatButton("BtnAdd_INT", "INT");
        BindAddStatButton("BtnAdd_VIT", "VIT");
        BindAddStatButton("BtnAdd_AGI", "AGI");
        BindAddStatButton("BtnAdd_LUK", "LUK");

        // Action buttons
        _root.Q<Button>("EquipButton")  ?.RegisterCallback<ClickEvent>(_ => OnEquipClicked());
        _root.Q<Button>("UnequipButton")?.RegisterCallback<ClickEvent>(_ => OnUnequipClicked());
        _root.Q<Button>("DropButton")   ?.RegisterCallback<ClickEvent>(_ => OnDropClicked());
        _root.Q<Button>("SortBtn")      ?.RegisterCallback<ClickEvent>(_ => CycleSort());
        _root.Q<Button>("LoadoutBtn")   ?.RegisterCallback<ClickEvent>(_ => Debug.Log("[LPC_UI] Loadout coming soon."));

        BindAllSlots();

        // Global pointer events for drag ghost movement
        _root.RegisterCallback<PointerMoveEvent>(OnRootPointerMove);
        _root.RegisterCallback<PointerUpEvent>(OnRootPointerUp);
    }

    private void BindAllSlots()
    {
        if (equipmentManager == null) return;

        foreach (var sn in GetUIActiveSlots())
        {
            string captured = sn;
            var slotEl = _root.Q<VisualElement>($"Slot_{sn}");
            if (slotEl == null) continue;

            slotEl.RegisterCallback<ClickEvent>(_ => OnEquipSlotClicked(captured));

            slotEl.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (_isDragging) slotEl.AddToClassList("equip-slot--drag-over");
            });

            slotEl.RegisterCallback<PointerLeaveEvent>(_ =>
                slotEl.RemoveFromClassList("equip-slot--drag-over"));

            slotEl.RegisterCallback<PointerUpEvent>(_ =>
            {
                if (_isDragging && _dragItem != null)
                {
                    slotEl.RemoveFromClassList("equip-slot--drag-over");
                    TryEquipDragItem(captured);
                }
            });
        }
    }

    private void BindFilter(string btnName, string filter)
    {
        var btn = _root.Q<Button>(btnName);
        if (btn == null) return;
        btn.RegisterCallback<ClickEvent>(_ => SetFilter(filter, btn));
    }

    // =========================================================================
    //  Panel Mode (Equipment / Appearance)
    // =========================================================================

    private void SetPanelMode(PanelMode mode)
    {
        _panelMode = mode;

        _root.Q<Button>("Tab_Equipment") ?.RemoveFromClassList("filter-active");
        _root.Q<Button>("Tab_Stats")     ?.RemoveFromClassList("filter-active");

        var paperDoll = _root.Q<VisualElement>("PaperDollScroll");
        var statsScroll = _root.Q<VisualElement>("StatsScroll");

        if (mode == PanelMode.Equipment)
        {
            _root.Q<Button>("Tab_Equipment")?.AddToClassList("filter-active");
            if (paperDoll != null) paperDoll.style.display = DisplayStyle.Flex;
            if (statsScroll != null) statsScroll.style.display = DisplayStyle.None;

            SetSlotsVisible(GetUIActiveSlots(),  true);
            SetSlotsVisible(equipmentManager?.GetAppearanceSlotNames(), false);
            SetSlotsVisible(new[] { "WeaponBehind", "ShieldBehind" }, false);
        }
        else // Stats Mode
        {
            _root.Q<Button>("Tab_Stats")?.AddToClassList("filter-active");
            if (paperDoll != null) paperDoll.style.display = DisplayStyle.None;
            if (statsScroll != null) statsScroll.style.display = DisplayStyle.Flex;
        }

        RefreshEquipSlots();
    }

    private void SetSlotsVisible(IEnumerable<string> slotNames, bool visible)
    {
        if (slotNames == null) return;
        foreach (var sn in slotNames)
        {
            var el = _root.Q<VisualElement>($"Slot_{sn}");
            if (el != null)
                el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // =========================================================================
    //  Drag & Drop
    // =========================================================================

    private void BeginDrag(LPCItemData item, VisualElement source, Vector2 pos)
    {
        _isDragging = true;
        _dragItem   = item;
        _dragSource = source;

        _dragGhost = new VisualElement();
        _dragGhost.style.position = Position.Absolute;
        _dragGhost.style.width    = _dragGhost.style.height = 56;
        _dragGhost.style.backgroundColor = new Color(0.08f, 0.10f, 0.18f, 0.92f);

        var rc = LPCItemData.RarityColor(item.rarity);
        _dragGhost.style.borderTopWidth   = _dragGhost.style.borderBottomWidth =
        _dragGhost.style.borderLeftWidth  = _dragGhost.style.borderRightWidth  = 2;
        _dragGhost.style.borderTopColor   = _dragGhost.style.borderBottomColor =
        _dragGhost.style.borderLeftColor  = _dragGhost.style.borderRightColor  = new StyleColor(rc);

        _dragGhost.style.borderTopLeftRadius     =
        _dragGhost.style.borderTopRightRadius    =
        _dragGhost.style.borderBottomLeftRadius  =
        _dragGhost.style.borderBottomRightRadius = 8;

        _dragGhost.pickingMode = PickingMode.Ignore;
        _dragGhost.style.opacity = 0.88f;

        if (item.icon != null)
        {
            var img = new VisualElement();
            img.style.width                      = img.style.height = 46;
            img.style.alignSelf                  = Align.Center;
            img.style.marginTop                  = 4;
            img.style.backgroundImage            = new StyleBackground(Background.FromSprite(item.icon));
            img.style.unityBackgroundScaleMode   = ScaleMode.ScaleToFit;
            img.pickingMode                      = PickingMode.Ignore;
            _dragGhost.Add(img);
        }
        else
        {
            // Show text category abbreviation when no icon
            var lbl = new Label { text = GetCategoryTag(item.category) };
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbl.style.fontSize       = 9;
            lbl.style.color          = Color.white;
            lbl.style.width          = lbl.style.height = 56;
            lbl.pickingMode          = PickingMode.Ignore;
            _dragGhost.Add(lbl);
        }

        _root.Add(_dragGhost);
        MoveDragGhost(pos);
    }

    private void MoveDragGhost(Vector2 pos)
    {
        if (_dragGhost == null) return;
        _dragGhost.style.left = pos.x - 28;
        _dragGhost.style.top  = pos.y - 28;
    }

    private void EndDrag()
    {
        _isDragging = false;
        _dragItem   = null;
        _dragSource = null;

        if (_dragGhost != null)
        {
            _root.Remove(_dragGhost);
            _dragGhost = null;
        }

        if (equipmentManager == null) return;

        // Clear all drag-over highlights
        foreach (var sn in GetUIActiveSlots())
            _root.Q<VisualElement>($"Slot_{sn}")?.RemoveFromClassList("equip-slot--drag-over");
    }

    private void TryEquipDragItem(string slotName)
    {
        if (_dragItem == null || equipmentManager == null)
        {
            EndDrag();
            return;
        }

        bool ok = equipmentManager.EquipItem(slotName, _dragItem);
        if (ok)
        {
            MarkInventoryDirty();
            RefreshAll();
        }
        else
        {
            Debug.LogWarning($"[LPC_UI] Cannot equip '{_dragItem.itemName}' into slot '{slotName}'.");
        }

        EndDrag();
    }

    private void OnRootPointerMove(PointerMoveEvent e)
    {
        if (_isDragging) MoveDragGhost(e.position);
    }

    private void OnRootPointerUp(PointerUpEvent e)
    {
        if (_isDragging) EndDrag();
    }

    // =========================================================================
    //  Refresh — only rebuild the inventory grid when the dirty flag is set
    // =========================================================================

    private void MarkInventoryDirty() => _inventoryDirty = true;

    public void RefreshAll()
    {
        RefreshHeader();
        RefreshStats();
        RefreshWeightBar();
        RefreshEquipSlots();

        if (_inventoryDirty)
        {
            RefreshInventoryGrid();
            _inventoryDirty = false;
        }
    }

    private void RefreshHeader()
    {
        SetText("CharacterName", playerName ?? "Hero");
        SetText("LevelValue",    playerLevel.ToString());
    }

    private void RefreshStats()
    {
        LPCPlayerController2 player = null;
        if (equipmentManager != null)
            player = equipmentManager.GetComponent<LPCPlayerController2>();

        if (player != null)
        {
            // Level & Experience
            SetText("Stat_Level", player.level.ToString());
            SetText("Stat_Exp", $"{player.currentExp:0} / {player.requiredExp:0}");

            // Stat Points (Always display container)
            var ptsContainer = _root.Q<VisualElement>("StatPointsContainer");
            if (ptsContainer != null)
            {
                ptsContainer.style.display = DisplayStyle.Flex;
            }
            SetText("StatPointsValue", player.statPoints.ToString());

            // Core attributes with plus buttons (Always show buttons, formatted as base (+bonus))
            bool showPlus = true;
            SetStatWithAddBtn("Stat_STR", "BtnAdd_STR", FormatStatValue(player.baseSTR, player.finalSTR), showPlus);
            SetStatWithAddBtn("Stat_DEX", "BtnAdd_DEX", FormatStatValue(player.baseDEX, player.finalDEX), showPlus);
            SetStatWithAddBtn("Stat_INT", "BtnAdd_INT", FormatStatValue(player.baseINT, player.finalINT), showPlus);
            SetStatWithAddBtn("Stat_VIT", "BtnAdd_VIT", FormatStatValue(player.baseVIT, player.finalVIT), showPlus);
            SetStatWithAddBtn("Stat_AGI", "BtnAdd_AGI", FormatStatValue(player.baseAGI, player.finalAGI), showPlus);
            SetStatWithAddBtn("Stat_LUK", "BtnAdd_LUK", FormatStatValue(player.baseLUK, player.finalLUK), showPlus);

            // Combat Ratings (breakdown of base vs equipment)
            float baseATK = player.baseSTR * 2f;
            float baseMATK = player.baseINT * 2f;
            float baseDEF = player.baseVIT * 0.5f;
            float baseMDEF = player.baseINT * 0.25f;
            float baseSPD = player.moveSpeed + (player.baseAGI * 0.05f);

            SetText("StatATK", FormatStatValueFloat(baseATK, player.finalATK));
            SetText("StatMATK", FormatStatValueFloat(baseMATK, player.finalMATK));
            SetText("StatDEF", FormatStatValueFloat(baseDEF, player.finalDEF));
            SetText("StatMDEF", FormatStatValueFloat(baseMDEF, player.finalMDEF));
            SetText("StatSPD", FormatStatValueFloat(baseSPD, player.finalMoveSpeed, "F1"));

            // Advanced Ratings (breakdown of base vs equipment)
            float baseCrit = ((player.baseDEX * 0.001f) + (player.baseLUK * 0.0015f));
            float baseCritDmg = 1.5f + (player.baseLUK * 0.005f);
            float basePhysPen = player.baseDEX * 1.0f;
            float baseMagicPen = player.baseINT * 0.5f;
            float baseSpellVamp = player.baseINT * 0.002f;
            float baseDodge = player.baseAGI * 0.001f;

            SetText("StatCritRate", FormatStatValueFloat(baseCrit * 100f, player.finalCritRate * 100f, "F1") + "%");
            SetText("StatCritDmg", FormatStatValueFloat(baseCritDmg * 100f, player.finalCritDamage * 100f, "F0") + "%");
            SetText("StatPhysPen", FormatStatValueFloat(basePhysPen, player.finalArmorPenetration));
            SetText("StatMagicPen", FormatStatValueFloat(baseMagicPen, player.finalMagicPenetration));
            SetText("StatLifesteal", $"{player.finalLifesteal * 100f:F0}%"); // 0 base lifesteal
            SetText("StatSpellVamp", FormatStatValueFloat(baseSpellVamp * 100f, player.finalSpellVamp * 100f, "F1") + "%");
            SetText("StatDodge", FormatStatValueFloat(baseDodge * 100f, player.finalDodge * 100f, "F1") + "%");
        }
        else
        {
            float atk = baseATK;
            float def = baseDEF;
            float spd = baseSPD;

            if (equipmentManager != null)
            {
                float speedMult = equipmentManager.GetWeightSpeedMultiplier();

                foreach (var item in equipmentManager.GetAllEquipped().Values)
                {
                    if (item == null) continue;
                    atk += item.offensive.atk * item.DamageMultiplier;
                    def += item.defensive.def;
                    spd += item.utility.moveSpeed;
                }

                spd *= speedMult;
            }

            SetText("Stat_Level", "1");
            SetText("Stat_Exp", "0 / 2000");
            var ptsContainer = _root.Q<VisualElement>("StatPointsContainer");
            if (ptsContainer != null) ptsContainer.style.display = DisplayStyle.Flex;

            SetStatWithAddBtn("Stat_STR", "BtnAdd_STR", "10", true);
            SetStatWithAddBtn("Stat_DEX", "BtnAdd_DEX", "10", true);
            SetStatWithAddBtn("Stat_INT", "BtnAdd_INT", "10", true);
            SetStatWithAddBtn("Stat_VIT", "BtnAdd_VIT", "10", true);
            SetStatWithAddBtn("Stat_AGI", "BtnAdd_AGI", "10", true);
            SetStatWithAddBtn("Stat_LUK", "BtnAdd_LUK", "10", true);

            SetText("StatATK", Mathf.RoundToInt(atk).ToString());
            SetText("StatDEF", Mathf.RoundToInt(def).ToString());
            SetText("StatSPD", spd.ToString("F1"));
        }
    }

    private string FormatStatValue(int baseVal, int finalVal)
    {
        int bonus = finalVal - baseVal;
        if (bonus > 0)
            return $"{baseVal} (+{bonus})";
        else if (bonus < 0)
            return $"{baseVal} ({bonus})";
        return baseVal.ToString();
    }

    private string FormatStatValueFloat(float baseVal, float finalVal, string format = "F0")
    {
        float bonus = finalVal - baseVal;
        if (Mathf.Abs(bonus) > 0.05f)
        {
            string bonusSign = bonus > 0 ? "+" : "";
            return $"{baseVal.ToString(format)} ({bonusSign}{bonus.ToString(format)})";
        }
        return baseVal.ToString(format);
    }

    private void SetStatWithAddBtn(string labelName, string btnName, string valueText, bool showBtn)
    {
        SetText(labelName, valueText);
        var btn = _root.Q<Button>(btnName);
        if (btn != null)
        {
            btn.style.display = showBtn ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void BindAddStatButton(string btnName, string statName)
    {
        var btn = _root.Q<Button>(btnName);
        if (btn == null) return;
        btn.RegisterCallback<ClickEvent>(_ => OnAddStatClicked(statName));
    }

    private void OnAddStatClicked(string statName)
    {
        LPCPlayerController2 player = null;
        if (equipmentManager != null)
            player = equipmentManager.GetComponent<LPCPlayerController2>();

        if (player == null || player.statPoints <= 0) return;

        player.statPoints--;
        switch (statName)
        {
            case "STR": player.baseSTR++; break;
            case "DEX": player.baseDEX++; break;
            case "INT": player.baseINT++; break;
            case "VIT": player.baseVIT++; break;
            case "AGI": player.baseAGI++; break;
            case "LUK": player.baseLUK++; break;
        }

        player.CalculateFinalStats();
        RefreshAll();
    }

    private void OnSaveClicked()
    {
        _popupIsSaveMode = true;
        if (_popupTitle != null) _popupTitle.text = "SAVE GAME";
        UpdateSlotInfos();
        if (_saveSlotPopup != null) _saveSlotPopup.style.display = DisplayStyle.Flex;
    }

    private void OnLoadClicked()
    {
        _popupIsSaveMode = false;
        if (_popupTitle != null) _popupTitle.text = "LOAD GAME";
        UpdateSlotInfos();
        if (_saveSlotPopup != null) _saveSlotPopup.style.display = DisplayStyle.Flex;
    }

    private void OnSlotClicked(int slot)
    {
        LPCPlayerController2 player = null;
        if (equipmentManager != null)
            player = equipmentManager.GetComponent<LPCPlayerController2>();

        if (player == null) return;

        if (_popupIsSaveMode)
        {
            LPC_SaveSystem.SaveGame(player, this, slot);
        }
        else
        {
            if (LPC_SaveSystem.LoadGame(player, this, slot))
            {
                MarkInventoryDirty();
                RefreshAll();
            }
        }
        HideSlotPopup();
    }

    private void HideSlotPopup()
    {
        if (_saveSlotPopup != null) _saveSlotPopup.style.display = DisplayStyle.None;
    }

    private void UpdateSlotInfos()
    {
        if (_slotInfo1 != null) _slotInfo1.text = LPC_SaveSystem.GetSlotInfo(1);
        if (_slotInfo2 != null) _slotInfo2.text = LPC_SaveSystem.GetSlotInfo(2);
        if (_slotInfo3 != null) _slotInfo3.text = LPC_SaveSystem.GetSlotInfo(3);
    }

    private void RefreshWeightBar()
    {
        float cur = equipmentManager?.GetTotalEquippedWeight() ?? 0f;
        float max = equipmentManager?.maxCarryWeight ?? 60f;

        SetText("GoldCount",  gold.ToString("N0"));
        SetText("WeightText", $"{cur:0.#}/{max:0}kg");

        var bar = _root.Q<VisualElement>("WeightBar");
        if (bar != null)
        {
            float pct = max > 0 ? Mathf.Clamp01(cur / max) * 100f : 0f;
            bar.style.width = Length.Percent(pct);

            var tier = equipmentManager?.GetWeightTier() ?? LPCEquipmentManager.WeightTier.Normal;
            bar.style.backgroundColor = tier switch
            {
                LPCEquipmentManager.WeightTier.HeavyEncumbered => new Color(0.88f, 0.27f, 0.2f),
                LPCEquipmentManager.WeightTier.Encumbered       => new Color(0.9f,  0.75f, 0.2f),
                _                                               => new Color(0.29f, 0.56f, 0.88f),
            };
        }

        var warn = _root.Q<Label>("WeightWarn");
        if (warn != null)
        {
            var tier = equipmentManager?.GetWeightTier() ?? LPCEquipmentManager.WeightTier.Normal;

            // Use plain ASCII text — emoji/symbols won't render on Unity's default font
            warn.text = tier switch
            {
                LPCEquipmentManager.WeightTier.HeavyEncumbered => "!! OVERLOADED  -50% SPD",
                LPCEquipmentManager.WeightTier.Encumbered       => "! ENCUMBERED  -20% SPD",
                _                                               => "",
            };

            warn.style.display = tier == LPCEquipmentManager.WeightTier.Normal
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }

    // =========================================================================
    //  Equipment Slot UI
    // =========================================================================

    private void RefreshEquipSlots()
    {
        if (equipmentManager == null) return;

        foreach (var sn in GetUIActiveSlots())
            UpdateSlotUI(sn, equipmentManager.GetEquipped(sn));
    }

    private void UpdateSlotUI(string slotName, LPCItemData item)
    {
        var slotEl = _root.Q<VisualElement>($"Slot_{slotName}");
        if (slotEl == null) return;

        var iconLabel = slotEl.Q<Label>($"Slot_{slotName}_Icon");
        var nameLabel = slotEl.Q<Label>($"Slot_{slotName}_Name");
        var rarityBar = slotEl.Q<VisualElement>($"Slot_{slotName}_Rarity");

        if (item != null)
        {
            if (iconLabel != null)
            {
                if (item.icon != null)
                {
                    iconLabel.text = "";
                    iconLabel.style.backgroundImage          = new StyleBackground(item.icon);
                    iconLabel.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    iconLabel.style.position                 = Position.Absolute;
                    iconLabel.style.width                    = 36;
                    iconLabel.style.height                   = 36;
                    iconLabel.style.top                      = 8;
                    iconLabel.style.left                     = 8;
                    iconLabel.style.fontSize                 = 0;
                }
                else
                {
                    // No icon — show a short ASCII category tag
                    iconLabel.style.backgroundImage = StyleKeyword.None;
                    ResetIconLayout(iconLabel);
                    iconLabel.text     = GetCategoryTag(item.category);
                    iconLabel.style.fontSize  = 8;
                    iconLabel.style.color     = new StyleColor(Color.white);
                    iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                }
            }

            if (nameLabel != null)
            {
                nameLabel.text          = item.itemName ?? "";
                nameLabel.style.display = DisplayStyle.Flex;
            }

            slotEl.AddToClassList("equip-slot--filled");
            ApplyRarityClass(rarityBar, GetRarityClass(item));
        }
        else
        {
            // Empty slot
            if (iconLabel != null)
            {
                iconLabel.text = "";
                iconLabel.style.backgroundImage = StyleKeyword.None;
                iconLabel.style.fontSize        = 0;
                ResetIconLayout(iconLabel);
            }

            if (nameLabel != null)
            {
                nameLabel.text          = "";
                nameLabel.style.display = DisplayStyle.None;
            }

            slotEl.RemoveFromClassList("equip-slot--filled");
            ClearRarityClass(rarityBar);
        }
    }

    private static void ResetIconLayout(Label lbl)
    {
        lbl.style.position = Position.Relative;
        lbl.style.width    = StyleKeyword.Auto;
        lbl.style.height   = StyleKeyword.Auto;
        lbl.style.top      = StyleKeyword.Auto;
        lbl.style.left     = StyleKeyword.Auto;
    }

    // =========================================================================
    //  Inventory Grid
    // =========================================================================

    private void RefreshInventoryGrid()
    {
        var grid = _root.Q<ScrollView>("InventoryGrid");
        if (grid == null) return;

        grid.Clear();

        var items = SortInventory(FilteredInventory());

        foreach (var item in items)
        {
            if (item == null) continue;
            grid.Add(CreateItemCell(item));
        }

        // Deselect if the selected item is no longer visible
        if (_selectedItem != null && !items.Contains(_selectedItem))
        {
            _selectedItem = null;
            _selectedCell = null;
        }

        RefreshDetailPanel(_selectedItem);
    }

    private List<LPCItemData> FilteredInventory()
    {
        var valid = (inventory ?? new List<LPCItemData>())
            .Where(i => i != null)
            .Where(i =>
            {
                // Hide appearance-only items from the equipment inventory view
                if (equipmentManager != null)
                {
                    var def = equipmentManager.GetSlotDefinition(i.childPath);
                    if (def != null && def.isAppearance) return false;
                }

                // Always exclude pure cosmetic categories from the main grid
                if (i.category is "Hair" or "Face" or "Behind" or "Eyes" or "Ears")
                    return false;

                return true;
            })
            .ToList();

        if (_currentFilter == "All") return valid;

        return valid
            .Where(i => !string.IsNullOrEmpty(i.category) &&
                        i.category.Equals(_currentFilter, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private VisualElement CreateItemCell(LPCItemData item)
    {
        var cell = new VisualElement();
        cell.AddToClassList("inv-item");

        // Icon or text fallback
        if (item.icon != null)
        {
            var ie = new VisualElement();
            ie.style.width                    = ie.style.height = 40;
            ie.style.alignSelf                = Align.Center;
            ie.style.backgroundImage          = new StyleBackground(Background.FromSprite(item.icon));
            ie.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            ie.pickingMode                    = PickingMode.Ignore;
            cell.Add(ie);
        }
        else
        {
            // ASCII category tag — safe on any font
            var il = new Label { text = GetCategoryTag(item.category) };
            il.AddToClassList("inv-item-icon");
            il.style.fontSize    = 9;
            il.style.color       = new StyleColor(Color.white);
            il.pickingMode       = PickingMode.Ignore;
            cell.Add(il);
        }

        var nm = new Label { text = item.itemName ?? "Unknown" };
        nm.AddToClassList("inv-item-name");
        nm.pickingMode = PickingMode.Ignore;
        cell.Add(nm);

        var wt = new Label { text = $"{item.weight:0.#}kg" };
        wt.AddToClassList("inv-item-weight");
        wt.pickingMode = PickingMode.Ignore;
        cell.Add(wt);

        var dot = new VisualElement();
        dot.AddToClassList("inv-item-rarity-dot");
        dot.AddToClassList(GetRarityClass(item));
        dot.pickingMode = PickingMode.Ignore;
        cell.Add(dot);

        // "E" badge when item is already equipped
        if (IsItemEquipped(item))
        {
            var badge = new VisualElement();
            badge.AddToClassList("inv-item-equipped-badge");
            var bt = new Label { text = "E" };
            bt.AddToClassList("inv-item-equipped-badge-text");
            bt.pickingMode = PickingMode.Ignore;
            badge.Add(bt);
            cell.Add(badge);
        }

        // Rarity border tint
        var rc = LPCItemData.RarityColor(item.rarity);
        cell.style.borderTopColor    =
        cell.style.borderBottomColor =
        cell.style.borderLeftColor   =
        cell.style.borderRightColor  = new StyleColor(rc * 0.7f);

        if (item == _selectedItem)
        {
            cell.AddToClassList("inv-item--selected");
            _selectedCell = cell;
        }

        cell.RegisterCallback<ClickEvent>(_ => SelectItem(item, cell));
        cell.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                SelectItem(item, cell);
                BeginDrag(item, cell, evt.position);
                evt.StopPropagation();
            }
        });

        // --- Vẽ Thanh Độ Bền Mini đồ họa dưới đáy ô trang bị ---
        if (item.maxDurability > 0)
        {
            var miniBarContainer = new VisualElement();
            miniBarContainer.name = "MiniDurabilityBarContainer";
            miniBarContainer.style.height = 3;
            miniBarContainer.style.width = Length.Percent(90);
            miniBarContainer.style.position = Position.Absolute;
            miniBarContainer.style.bottom = 4;
            miniBarContainer.style.left = Length.Percent(5);
            miniBarContainer.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f); // Nền đen
            miniBarContainer.style.borderTopLeftRadius = miniBarContainer.style.borderTopRightRadius =
            miniBarContainer.style.borderBottomLeftRadius = miniBarContainer.style.borderBottomRightRadius = 1;

            var miniBarFill = new VisualElement();
            miniBarFill.name = "MiniDurabilityBarFill";
            miniBarFill.style.height = Length.Percent(100);
            
            float pct = Mathf.Clamp01(item.currentDurability / item.maxDurability);
            miniBarFill.style.width = Length.Percent(pct * 100f);
            
            // Màu sắc thanh mini (Xanh lá -> Vàng -> Đỏ)
            if (pct > 0.5f)
                miniBarFill.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
            else if (pct > 0.2f)
                miniBarFill.style.backgroundColor = new Color(0.9f, 0.7f, 0.1f, 1f);
            else
                miniBarFill.style.backgroundColor = new Color(0.85f, 0.2f, 0.2f, 1f);

            miniBarFill.style.borderTopLeftRadius = miniBarFill.style.borderBottomLeftRadius = 1;
            if (pct >= 0.98f)
                miniBarFill.style.borderTopRightRadius = miniBarFill.style.borderBottomRightRadius = 1;

            miniBarContainer.Add(miniBarFill);
            cell.Add(miniBarContainer);
        }

        return cell;
    }

    // =========================================================================
    //  Filter & Sort
    // =========================================================================

    private void SetFilter(string filter, Button btn)
    {
        _currentFilter = filter;
        _activeFilterBtn?.RemoveFromClassList("filter-active");
        btn.AddToClassList("filter-active");
        _activeFilterBtn = btn;

        MarkInventoryDirty();
        RefreshInventoryGrid();
        _inventoryDirty = false; // Reset after immediate rebuild
    }

    private void CycleSort()
    {
        _sortIndex    = (_sortIndex + 1) % SortCycles.Length;
        _sortAscending = true;

        var btn = _root.Q<Button>("SortBtn");
        if (btn != null) btn.text = $"SORT: {CurrentSort.ToUpper()}";

        MarkInventoryDirty();
        RefreshInventoryGrid();
        _inventoryDirty = false;
    }

    private List<LPCItemData> SortInventory(List<LPCItemData> list)
    {
        if (list == null || list.Count == 0) return new List<LPCItemData>();

        IOrderedEnumerable<LPCItemData> sorted = CurrentSort switch
        {
            "name"   => list.OrderBy(i => i?.itemName ?? ""),
            "type"   => list.OrderBy(i => i?.category ?? "").ThenBy(i => i?.itemName ?? ""),
            "rarity" => list.OrderByDescending(i => (int)(i?.rarity ?? 0)),
            "weight" => list.OrderByDescending(i => i?.weight ?? 0f),
            _        => list.OrderBy(i => i?.itemName ?? ""),
        };

        return (_sortAscending ? sorted : sorted.Reverse()).ToList();
    }

    // =========================================================================
    //  Selection
    // =========================================================================

    private void SelectItem(LPCItemData item, VisualElement cell)
    {
        _selectedCell?.RemoveFromClassList("inv-item--selected");
        _selectedItem = item;
        _selectedCell = cell;
        cell?.AddToClassList("inv-item--selected");
        RefreshDetailPanel(item);
    }

    // =========================================================================
    //  Detail Panel
    // =========================================================================

    private void RefreshDetailPanel(LPCItemData item)
    {
        var detailPanel = _root.Q<VisualElement>("DetailPanel");
        if (item == null)
        {
            if (detailPanel != null)
            {
                detailPanel.style.display = DisplayStyle.None; // Ẩn bảng chi tiết khi không chọn/hover item
            }
            return;
        }

        if (detailPanel != null)
        {
            detailPanel.style.display = DisplayStyle.Flex; // Hiện bảng chi tiết khi chọn/hover item
        }

        SetText("ItemName",     item.itemName ?? "Unknown");
        SetText("ItemCategory", (item.category ?? "-").ToUpper());
        SetText("RarityStars",  BuildRarityStars(item));
        SetText("ItemWeight",   $"{item.weight:0.#} kg");

        var descLabel = _root.Q<Label>("ItemDesc");
        string desc = !string.IsNullOrEmpty(item.description)
            ? item.description
            : $"{item.category} | {item.childPath}";

        if (item.IsBroken)
        {
            if (item.category != null && item.category.Equals("Weapon", System.StringComparison.OrdinalIgnoreCase))
                desc += "\n[BROKEN] Damage reduced by 75%";
            else
                desc += "\n[BROKEN] Defense reduced by 75%";
        }
        SetText("ItemDesc", desc);

        // --- Đồ Họa Thanh Độ Bền Co Giãn Động ---
        var parentElement = descLabel?.parent;
        if (parentElement != null)
        {
            var durBarContainer = parentElement.Q<VisualElement>("DynamicDurabilityBarContainer");
            if (item.maxDurability > 0)
            {
                if (durBarContainer == null)
                {
                    // Tạo container cho thanh độ bền
                    durBarContainer = new VisualElement();
                    durBarContainer.name = "DynamicDurabilityBarContainer";
                    durBarContainer.style.height = 16;
                    durBarContainer.style.marginTop = 6;
                    durBarContainer.style.marginBottom = 6;
                    durBarContainer.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f); // Nền đen/xám tối
                    durBarContainer.style.borderTopWidth = durBarContainer.style.borderBottomWidth =
                    durBarContainer.style.borderLeftWidth = durBarContainer.style.borderRightWidth = 1;
                    durBarContainer.style.borderTopColor = durBarContainer.style.borderBottomColor =
                    durBarContainer.style.borderLeftColor = durBarContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.4f, 1f);
                    durBarContainer.style.borderTopLeftRadius = durBarContainer.style.borderTopRightRadius =
                    durBarContainer.style.borderBottomLeftRadius = durBarContainer.style.borderBottomRightRadius = 4;
                    durBarContainer.style.flexDirection = FlexDirection.Row;
                    durBarContainer.style.alignItems = Align.Center;

                    // Tạo thanh co giãn bên trong
                    var durBarFill = new VisualElement();
                    durBarFill.name = "DynamicDurabilityBarFill";
                    durBarFill.style.height = Length.Percent(100);
                    durBarFill.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Màu xanh lá mặc định
                    durBarFill.style.borderTopLeftRadius = durBarFill.style.borderBottomLeftRadius = 3;
                    durBarContainer.Add(durBarFill);

                    // Tạo chữ hiển thị số độ bền đè lên trên thanh
                    var durBarText = new Label();
                    durBarText.name = "DynamicDurabilityBarText";
                    durBarText.style.position = Position.Absolute;
                    durBarText.style.width = Length.Percent(100);
                    durBarText.style.height = Length.Percent(100);
                    durBarText.style.unityTextAlign = TextAnchor.MiddleCenter;
                    durBarText.style.fontSize = 9;
                    durBarText.style.color = Color.white;
                    durBarContainer.Add(durBarText);

                    // Chèn vào ngay phía trên descLabel
                    int descIdx = parentElement.IndexOf(descLabel);
                    parentElement.Insert(descIdx, durBarContainer);
                }

                // Cập nhật giá trị thanh
                var fill = durBarContainer.Q<VisualElement>("DynamicDurabilityBarFill");
                var text = durBarContainer.Q<Label>("DynamicDurabilityBarText");
                
                float pct = Mathf.Clamp01(item.currentDurability / item.maxDurability);
                if (fill != null)
                {
                    fill.style.width = Length.Percent(pct * 100f);
                    // Đổi màu thanh dựa trên phần trăm độ bền (Xanh lá -> Vàng -> Đỏ)
                    if (pct > 0.5f)
                        fill.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Xanh lá
                    else if (pct > 0.2f)
                        fill.style.backgroundColor = new Color(0.9f, 0.7f, 0.1f, 1f); // Vàng
                    else
                        fill.style.backgroundColor = new Color(0.85f, 0.2f, 0.2f, 1f); // Đỏ
                        
                    // Bo góc phải của thanh fill nếu đầy 100%
                    if (pct >= 0.98f)
                        fill.style.borderTopRightRadius = fill.style.borderBottomRightRadius = 3;
                    else
                        fill.style.borderTopRightRadius = fill.style.borderBottomRightRadius = 0;
                }
                
                if (text != null)
                {
                    text.text = $"Durability: {item.currentDurability:0}/{item.maxDurability:0} ({pct * 100f:0}%)";
                }
                
                durBarContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                // Nếu vật phẩm không có độ bền (ví dụ cosmetic), ẩn thanh đi
                if (durBarContainer != null)
                {
                    durBarContainer.style.display = DisplayStyle.None;
                }
            }
        }

        // Portrait image
        var portrait = _root.Q<VisualElement>("DetailPortrait");
        var dIcon    = _root.Q<Label>("DetailIcon");

        if (item.icon != null && portrait != null)
        {
            portrait.style.backgroundImage          = new StyleBackground(Background.FromSprite(item.icon));
            portrait.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            if (dIcon != null) dIcon.text = "";
        }
        else
        {
            if (portrait != null) portrait.style.backgroundImage = StyleKeyword.None;
            if (dIcon    != null) dIcon.text = GetCategoryTag(item.category);
        }

        // Stat comparison vs what is currently equipped in that slot
        SetDiffLabel("DiffATK", "ATK", item.offensive.atk - GetEquippedStat(item.childPath, true));
        SetDiffLabel("DiffDEF", "DEF", item.defensive.def - GetEquippedStat(item.childPath, false));
    }

    private float GetEquippedStat(string slotName, bool isATK)
    {
        var cur = equipmentManager?.GetEquipped(slotName);
        if (cur == null) return 0f;
        return isATK ? cur.offensive.atk : cur.defensive.def;
    }

    private void SetDiffLabel(string labelName, string statName, float diff)
    {
        var lbl = _root.Q<Label>(labelName);
        if (lbl == null) return;

        if (Mathf.Abs(diff) < 0.01f)
        {
            lbl.text = "";
            return;
        }

        lbl.text = $"{statName} {(diff > 0 ? "+" : "")}{diff:F0}";
        lbl.RemoveFromClassList("diff-positive");
        lbl.RemoveFromClassList("diff-negative");
        lbl.AddToClassList(diff > 0 ? "diff-positive" : "diff-negative");
    }

    // =========================================================================
    //  Actions
    // =========================================================================

    private void OnEquipClicked()
    {
        if (_selectedItem == null || equipmentManager == null) return;

        if (equipmentManager.EquipItem(_selectedItem))
        {
            MarkInventoryDirty(); // Badge state changes require a grid rebuild
            RefreshAll();
        }
    }

    private void OnUnequipClicked()
    {
        if (_selectedItem == null || equipmentManager == null) return;

        foreach (var kvp in equipmentManager.GetAllEquipped())
        {
            if (kvp.Value == _selectedItem)
            {
                equipmentManager.UnequipItem(kvp.Key);
                MarkInventoryDirty();
                RefreshAll();
                break;
            }
        }
    }

    private void OnDropClicked()
    {
        if (_selectedItem == null) return;

        // Unequip first if currently equipped
        if (equipmentManager != null)
        {
            foreach (var kvp in equipmentManager.GetAllEquipped())
            {
                if (kvp.Value == _selectedItem)
                {
                    equipmentManager.UnequipItem(kvp.Key);
                    break;
                }
            }
        }

        inventory.Remove(_selectedItem);
        _selectedItem = null;
        _selectedCell = null;

        MarkInventoryDirty(); // Item removed — grid must rebuild
        RefreshAll();
    }

    private void OnEquipSlotClicked(string slotName)
    {
        var item = equipmentManager?.GetEquipped(slotName);
        if (item != null) SelectItem(item, null);
    }

    // =========================================================================
    //  Show / Hide
    // =========================================================================

    public void ShowUI()
    {
        if (_backdrop != null)
        {
            _backdrop.style.display = DisplayStyle.Flex;
            MarkInventoryDirty(); // Always render fresh state when opening
            RefreshAll();
        }

        if (hudDocument?.rootVisualElement != null)
            hudDocument.rootVisualElement.style.display = DisplayStyle.None;

        SetPanelMode(PanelMode.Equipment);
    }

    public void HideUI()
    {
        if (_backdrop != null)
            _backdrop.style.display = DisplayStyle.None;

        if (hudDocument?.rootVisualElement != null)
            hudDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void ToggleUI()
    {
        if (_backdrop == null) return;

        if (_backdrop.resolvedStyle.display == DisplayStyle.Flex)
            HideUI();
        else
            ShowUI();
    }

    private IEnumerable<string> GetUIActiveSlots()
    {
        if (equipmentManager == null) return Enumerable.Empty<string>();
        return equipmentManager.GetEquipmentSlotNames()
            .Where(sn => !sn.Equals("WeaponBehind", System.StringComparison.OrdinalIgnoreCase) &&
                         !sn.Equals("ShieldBehind", System.StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private void SetText(string name, string text)
    {
        var el = _root.Q<Label>(name);
        if (el != null) el.text = text ?? "";
    }

    private bool IsItemEquipped(LPCItemData item)
    {
        if (item == null || equipmentManager == null) return false;
        return equipmentManager.GetAllEquipped().Values.Contains(item);
    }

    // -------------------------------------------------------------------------
    //  Rarity helpers
    // -------------------------------------------------------------------------

    private static string GetRarityClass(LPCItemData item) => ((int)item.rarity) switch
    {
        2 => "rarity-rare",
        3 => "rarity-epic",
        4 => "rarity-legend",
        _ => "rarity-common",
    };

    /// <summary>
    /// Builds a rarity star string using plain ASCII characters that render
    /// correctly on any Unity font. Filled = [*], empty = [ ].
    /// </summary>
    private static string BuildRarityStars(LPCItemData item)
    {
        int filled = Mathf.Clamp((int)item.rarity + 1, 0, 5);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 5; i++)
            sb.Append(i < filled ? "[*]" : "[ ]");
        return sb.ToString();
    }

    /// <summary>
    /// Returns a short ASCII tag for a category, safe on any Unity font.
    /// Replaces emoji icons that appear as ??? on many fonts.
    /// </summary>
    private static string GetCategoryTag(string cat) => cat switch
    {
        "Weapon"   => "[WPN]",
        "Armor"    => "[ARM]",
        "Clothing" => "[CLO]",
        "Hair"     => "[HAIR]",
        "Face"     => "[FACE]",
        "Behind"   => "[BEH]",
        "FX"       => "[FX]",
        _          => "[?]",
    };

    private static void ApplyRarityClass(VisualElement el, string cls)
    {
        if (el == null) return;
        ClearRarityClass(el);
        el.AddToClassList(cls);
    }

    private static void ClearRarityClass(VisualElement el)
    {
        if (el == null) return;
        el.RemoveFromClassList("rarity-common");
        el.RemoveFromClassList("rarity-rare");
        el.RemoveFromClassList("rarity-epic");
        el.RemoveFromClassList("rarity-legend");
    }
}
