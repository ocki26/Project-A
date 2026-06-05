using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class LPC_HUD_Manager : MonoBehaviour
{
    [Header("References")]
    public LPC_UI_Manager equipmentUIManager;
    public LPCPlayerController2 player;

    [Header("Settings")]
    public bool showGold = true;

    [Header("Heart Sprites")]
    public Sprite heartFullSprite;
    public Sprite heartBrokenSprite;
    public Sprite heartEmptySprite;
    public Sprite heartShieldSprite;

    [Header("Mana & Stamina Sprites")]
    public Sprite manaIconSprite;
    public Sprite staminaIconSprite;

    [Header("Survival Sprites")]
    public Sprite hungerIconSprite;
    public Sprite thirstIconSprite;

    [Header("Health Icon Sprite")]
    public Sprite healthIconSprite;

    private VisualElement _root;

    // UI Elements
    private VisualElement _hpFill;
    private VisualElement _shieldFill;
    private Label _hpIcon;
    private Label _hpText;
    private Label _mpIcon;
    private Label _staIcon;
    
    private VisualElement _mpFill;
    private Label _mpText;

    private VisualElement _staFill;
    private VisualElement _staLauFill;
    private Label _staText;

    private VisualElement _hungerFill;
    private Label _hungerText;
    private Label _hungerIcon;

    private VisualElement _thirstFill;
    private Label _thirstText;
    private Label _thirstIcon;

    private VisualElement _buffContainer;

    // Cooldown UI Elements
    private VisualElement _cdOverlayQ;
    private Label _cdTextQ;
    private VisualElement _cdOverlayF;
    private Label _cdTextF;
    private VisualElement _cdOverlayE;
    private Label _cdTextE;
    private VisualElement _cdOverlayR;
    private Label _cdTextR;

    // Skill Icon Labels
    private Label _skillIconQ;
    private Label _skillIconF;
    private Label _skillIconE;
    private Label _skillIconR;

    private LPC_BuffManager _buffManager;
    private LPC_SkillManager _skillManager;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;

        _root = doc.rootVisualElement;

        // BIND NÚT BALO
        _root.Q<Button>("HUD_InventoryBtn")
             ?.RegisterCallback<ClickEvent>(_ => equipmentUIManager?.ToggleUI());

        _hpFill = _root.Q<VisualElement>("HUD_HPFill");
        _shieldFill = _root.Q<VisualElement>("HUD_ShieldFill");
        _hpIcon = _root.Q<Label>("HUD_HPIcon");
        _hpText = _root.Q<Label>("HUD_HPText");
        
        _mpFill = _root.Q<VisualElement>("HUD_MPFill");
        _mpText = _root.Q<Label>("HUD_MPText");
        _mpIcon = _root.Q<Label>("HUD_MPIcon");
        
        _staFill = _root.Q<VisualElement>("HUD_STAFill");
        _staLauFill = _root.Q<VisualElement>("HUD_STALauFill");
        _staText = _root.Q<Label>("HUD_STAText");
        _staIcon = _root.Q<Label>("HUD_STAIcon");

        _hungerFill = _root.Q<VisualElement>("HUD_HungerFill");
        _hungerText = _root.Q<Label>("HUD_HungerText");
        _hungerIcon = _root.Q<Label>("HUD_HungerIcon");

        _thirstFill = _root.Q<VisualElement>("HUD_ThirstFill");
        _thirstText = _root.Q<Label>("HUD_ThirstText");
        _thirstIcon = _root.Q<Label>("HUD_ThirstIcon");

        _buffContainer = _root.Q<VisualElement>("HUD_BuffContainer");

        // Skill slots
        _cdOverlayQ = _root.Q<VisualElement>("SkillCooldown_Q");
        _cdTextQ = _root.Q<Label>("SkillCDText_Q");
        _skillIconQ = _root.Q<Label>("SkillIcon_Q");

        _cdOverlayF = _root.Q<VisualElement>("SkillCooldown_F");
        _cdTextF = _root.Q<Label>("SkillCDText_F");
        _skillIconF = _root.Q<Label>("SkillIcon_F");

        _cdOverlayE = _root.Q<VisualElement>("SkillCooldown_E");
        _cdTextE = _root.Q<Label>("SkillCDText_E");
        _skillIconE = _root.Q<Label>("SkillIcon_E");

        _cdOverlayR = _root.Q<VisualElement>("SkillCooldown_R");
        _cdTextR = _root.Q<Label>("SkillCDText_R");
        _skillIconR = _root.Q<Label>("SkillIcon_R");
    }

    private void Start()
    {
        if (player == null)
            player = FindObjectOfType<LPCPlayerController2>();

        if (player != null)
        {
            _buffManager = player.GetComponent<LPC_BuffManager>();
            if (_buffManager == null) _buffManager = player.gameObject.AddComponent<LPC_BuffManager>();

            _skillManager = player.GetComponent<LPC_SkillManager>();
            if (_skillManager == null) _skillManager = player.gameObject.AddComponent<LPC_SkillManager>();
        }

        // Áp dụng Sprite cho Mana & Stamina Icons nếu được gán
        if (_mpIcon != null && manaIconSprite != null)
        {
            _mpIcon.text = "";
            _mpIcon.style.backgroundImage = new StyleBackground(manaIconSprite);
            _mpIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
        if (_staIcon != null && staminaIconSprite != null)
        {
            _staIcon.text = "";
            _staIcon.style.backgroundImage = new StyleBackground(staminaIconSprite);
            _staIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        // Áp dụng Sprite cho Hunger & Thirst Icons nếu được gán
        if (_hungerIcon != null && hungerIconSprite != null)
        {
            _hungerIcon.text = "";
            _hungerIcon.style.backgroundImage = new StyleBackground(hungerIconSprite);
            _hungerIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
        if (_thirstIcon != null && thirstIconSprite != null)
        {
            _thirstIcon.text = "";
            _thirstIcon.style.backgroundImage = new StyleBackground(thirstIconSprite);
            _thirstIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        // Áp dụng Sprite cho Health Icon nếu được gán (hoặc dùng heartFullSprite làm fallback)
        Sprite hpSprite = healthIconSprite != null ? healthIconSprite : heartFullSprite;
        if (_hpIcon != null && hpSprite != null)
        {
            _hpIcon.text = "";
            _hpIcon.style.backgroundImage = new StyleBackground(hpSprite);
            _hpIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }
    }

    private void Update()
    {
        if (player == null) return;

        // 1. Cập nhật HP, MP, Stamina, Shield, Hunger, Thirst
        UpdateStats(player.currentHP, player.maxHP, player.currentMP, player.maxMP, player.currentStamina, player.maxStamina, player.currentShield, player.currentStaminaLau, player.currentHunger, player.maxHunger, player.currentThirst, player.maxThirst);

        // 2. Cập nhật tiền vàng
        if (equipmentUIManager != null)
        {
            RefreshHUD(equipmentUIManager.gold);
        }

        // 3. Cập nhật Cooldown kỹ năng
        UpdateCooldowns();

        // 4. Cập nhật Buff/Debuff Icons
        UpdateBuffIcons();
    }

    public void RefreshHUD(int gold)
    {
        if (!showGold) return;
        var goldLabel = _root?.Q<Label>("HUD_GoldLabel");
        if (goldLabel != null) goldLabel.text = gold.ToString("N0");
    }

    public void UpdateStats(float currentHp, float maxHp, float currentMp, float maxMp, float currentSta, float maxSta, float shield, float staminaLau, float currentHunger, float maxHunger, float currentThirst, float maxThirst)
    {
        if (_hpFill != null)
        {
            float hpPercent = maxHp > 0 ? Mathf.Clamp01(currentHp / maxHp) : 0;
            _hpFill.style.width = Length.Percent(hpPercent * 100f);
        }

        if (_shieldFill != null)
        {
            float shieldPercent = maxHp > 0 ? Mathf.Clamp01(shield / maxHp) : 0;
            _shieldFill.style.width = Length.Percent(shieldPercent * 100f);
        }

        if (_hpText != null)
        {
            if (shield > 0)
            {
                _hpText.text = $"{Mathf.CeilToInt(currentHp)} (+{Mathf.CeilToInt(shield)}🛡) / {Mathf.CeilToInt(maxHp)}";
            }
            else
            {
                _hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
            }
        }

        if (_mpFill != null && _mpText != null)
        {
            float mpPercent = maxMp > 0 ? Mathf.Clamp01(currentMp / maxMp) : 0;
            _mpFill.style.width = Length.Percent(mpPercent * 100f);
            _mpText.text = $"{Mathf.CeilToInt(currentMp)} / {Mathf.CeilToInt(maxMp)}";
        }

        if (_staFill != null && _staText != null)
        {
            float staPercent = maxSta > 0 ? Mathf.Clamp01(currentSta / maxSta) : 0;
            _staFill.style.width = Length.Percent(staPercent * 100f);

            if (_staLauFill != null)
            {
                float staLauPercent = maxSta > 0 ? Mathf.Clamp01(staminaLau / maxSta) : 0;
                _staLauFill.style.width = Length.Percent(staLauPercent * 100f);
            }

            _staText.text = $"{Mathf.CeilToInt(currentSta)} [{Mathf.CeilToInt(staminaLau)}] / {Mathf.CeilToInt(maxSta)}";
        }

        if (_hungerFill != null && _hungerText != null)
        {
            float hungerPercent = maxHunger > 0 ? Mathf.Clamp01(currentHunger / maxHunger) : 0;
            _hungerFill.style.width = Length.Percent(hungerPercent * 100f);
            _hungerText.text = $"{Mathf.CeilToInt(currentHunger)} / {Mathf.CeilToInt(maxHunger)}";
        }

        if (_thirstFill != null && _thirstText != null)
        {
            float thirstPercent = maxThirst > 0 ? Mathf.Clamp01(currentThirst / maxThirst) : 0;
            _thirstFill.style.width = Length.Percent(thirstPercent * 100f);
            _thirstText.text = $"{Mathf.CeilToInt(currentThirst)} / {Mathf.CeilToInt(maxThirst)}";
        }
    }

    private void UpdateCooldowns()
    {
        if (_skillManager == null || _skillManager.skills.Count < 4) return;

        UpdateSkillCDUI(_skillManager.skills[0], _cdOverlayQ, _cdTextQ, _skillIconQ, "⚔");
        UpdateSkillCDUI(_skillManager.skills[1], _cdOverlayF, _cdTextF, _skillIconF, "🗡");
        UpdateSkillCDUI(_skillManager.skills[2], _cdOverlayE, _cdTextE, _skillIconE, "🔥");
        UpdateSkillCDUI(_skillManager.skills[3], _cdOverlayR, _cdTextR, _skillIconR, "🛡");
    }

    private void UpdateSkillCDUI(LPC_SkillManager.Skill skill, VisualElement overlay, Label textLbl, Label iconLbl, string defaultText)
    {
        if (overlay == null || textLbl == null || iconLbl == null) return;

        // Cập nhật hiển thị Icon kỹ năng
        if (skill.data != null && skill.data.icon != null)
        {
            iconLbl.text = ""; // Xóa emoji chữ mặc định
            iconLbl.style.backgroundImage = new StyleBackground(skill.data.icon);
        }
        else
        {
            iconLbl.text = defaultText; // Trả về emoji chữ nếu ô trống
            iconLbl.style.backgroundImage = null;
        }

        // Cập nhật hiển thị bóng mờ Cooldown
        if (skill.data != null && skill.cooldownTimer > 0f)
        {
            float pct = Mathf.Clamp01(skill.cooldownTimer / skill.cooldown) * 100f;
            overlay.style.height = Length.Percent(pct);
            textLbl.text = $"{skill.cooldownTimer:F1}s";
        }
        else
        {
            overlay.style.height = Length.Percent(0f);
            textLbl.text = "";
        }
    }

    private void UpdateBuffIcons()
    {
        if (_buffContainer == null || _buffManager == null) return;

        _buffContainer.Clear();

        // [MỚI] Hiển thị debuff cạn kiệt (Exhausted) trực quan
        if (player != null && player.isExhaustedState)
        {
            var iconBg = new VisualElement();
            iconBg.AddToClassList("hud-buff-icon-bg");
            iconBg.AddToClassList("buff-border-burn"); // Viền đỏ biểu thị cạn kiệt

            var emojiLbl = new Label { text = "🥵" };
            emojiLbl.AddToClassList("hud-buff-icon");
            iconBg.Add(emojiLbl);

            var timerLbl = new Label { text = "KIỆT" }; // Nhãn hiển thị trạng thái kiệt sức
            timerLbl.AddToClassList("hud-buff-timer");
            iconBg.Add(timerLbl);

            _buffContainer.Add(iconBg);
        }
        var buffs = _buffManager.GetAllBuffs();

        foreach (var buff in buffs)
        {
            var iconBg = new VisualElement();
            iconBg.AddToClassList("hud-buff-icon-bg");

            string borderClass = buff.type switch
            {
                LPC_BuffManager.BuffType.Burn => "buff-border-burn",
                LPC_BuffManager.BuffType.Freeze => "buff-border-freeze",
                LPC_BuffManager.BuffType.Bleed => "buff-border-bleed",
                LPC_BuffManager.BuffType.Regeneration => "buff-border-regen",
                LPC_BuffManager.BuffType.Shield => "buff-border-shield",
                _ => "buff-border-shield"
            };
            iconBg.AddToClassList(borderClass);

            string buffEmoji = buff.type switch
            {
                LPC_BuffManager.BuffType.Burn => "🔥",
                LPC_BuffManager.BuffType.Freeze => "❄",
                LPC_BuffManager.BuffType.Bleed => "🩸",
                LPC_BuffManager.BuffType.Regeneration => "💚",
                LPC_BuffManager.BuffType.Shield => "🛡",
                _ => "⭐"
            };

            var emojiLbl = new Label { text = buffEmoji };
            emojiLbl.AddToClassList("hud-buff-icon");
            iconBg.Add(emojiLbl);

            var timerLbl = new Label { text = $"{Mathf.CeilToInt(buff.duration)}s" };
            timerLbl.AddToClassList("hud-buff-timer");
            iconBg.Add(timerLbl);

            _buffContainer.Add(iconBg);
        }
    }
}