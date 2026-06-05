using UnityEngine;


using UnityEditor;


using UnityEngine.U2D.Animation;


using UnityEditor.U2D.Sprites;


using System.Collections.Generic;


using System.IO;


using System.Linq;





/// <summary>


/// LPC Item Creator v9


/// Menu: Tools -> LPC Item Creator


/// </summary>


public class LPCItemCreator : EditorWindow


{


    // =========================================================================


    //  ANIMATION SLOT


    // =========================================================================





    [System.Serializable]


    private class AnimSlot


    {


        public string animName;


        public int realFrames;


        public bool loop;


        public bool directionless;


        public bool skip;


        public Texture2D texture;


        public int detectedCols;


        public int detectedRows;


        public int spriteWidth;


        public int spriteHeight;





        public AnimSlot(string n, int f, bool lp,


                        bool dl = false, int w = 64, int h = 64)


        { animName = n; realFrames = f; loop = lp; directionless = dl; spriteWidth = w; spriteHeight = h; }


    }





    private static AnimSlot[] DefaultSlots() => new[]


    {


        new AnimSlot("Walk",         9,  true,  false, 64,  64),


        new AnimSlot("Run",          8,  true,  false, 64,  64),


        new AnimSlot("Idle",         2,  true,  false, 64,  64),


        new AnimSlot("Slash",        6,  false, false, 64, 64),


        new AnimSlot("Thrust",       8,  false, false, 64, 64),


        new AnimSlot("Shoot",        13, false, false, 64, 64),


        new AnimSlot("Spellcast",    7,  false, false, 64, 64),


        new AnimSlot("1h_Slash",     13, false, false, 64,  64),


        new AnimSlot("1h_Backslash", 13, false, false, 64,  64),


        new AnimSlot("1h_Halfslash", 6,  false, false, 64,  64),


        new AnimSlot("Combat",       2,  true,  false, 64,  64),


        new AnimSlot("Hurt",         6,  false, true,  64,  64),


        new AnimSlot("Die",          6,  false, true,  64,  64),


        new AnimSlot("Jump",         5,  false, false, 64,  64),


        new AnimSlot("Sit",          3,  true,  false, 64,  64),


        new AnimSlot("Climb",        6,  true,  true,  64,  64),


        new AnimSlot("Emote",        3,  false, false, 64,  64),


        new AnimSlot("Watering",     8,  false, false, 64,  64),


    };





    // =========================================================================


    //  FIELDS


    // =========================================================================





    private string itemName = "MyItem";


    private string childPath = "Weapon";


    private string category = "Weapon";


    private LPCPlayerController2.WeaponType weaponType = LPCPlayerController2.WeaponType.Unarmed_Slash;
    private float attackRange = 1.2f;
    private float attackAngle = 90f;
    private bool isRanged = false;

    private DirectionalVisibility visibilityUp = DirectionalVisibility.Default;
    private DirectionalVisibility visibilityLeft = DirectionalVisibility.Default;
    private DirectionalVisibility visibilityDown = DirectionalVisibility.Default;
    private DirectionalVisibility visibilityRight = DirectionalVisibility.Default;


    private string outputRoot = "Assets/Items";


    private string description = "";


    private ItemRarity rarity = ItemRarity.Common;


    private Sprite manualIcon = null;


    private DefaultAsset folderAsset = null;





    private float itemWeight = 1f;





    private float maxDurability = 100f;


    private float currentDurability = 100f;





    private int pixelsPerUnit = 64;


    private int frameRate = 8;


    private FilterMode filterMode = FilterMode.Point;


    private bool autoDetect = false;


    public enum PivotMode
    {
        Center,
        BottomCenter,
        Custom
    }
    private PivotMode pivotMode = PivotMode.BottomCenter;
    private Vector2 customPivot = new Vector2(0.5f, 0.05f);


    private bool compactView = false;





    private CoreStats statCore;


    private OffensiveStats statOffensive;


    private DefensiveStats statDefensive;


    private UtilityStats statUtility;


    private AttributeStats statAttributes;


    private ElementStats statElemental;


    private SpecialStats statSpecial;





    private bool foldIdentity = true;


    private bool foldSprite = true;


    private bool foldSlots = true;


    private bool foldCore = false;


    private bool foldOffensive = false;


    private bool foldDefensive = false;


    private bool foldUtility = false;


    private bool foldAttributes = false;


    private bool foldElemental = false;


    private bool foldSpecial = false;





    private List<AnimSlot> slots = new();


    private Vector2 scroll;





    private static readonly string[] Categories =


        { "Weapon","Clothing","Armor","Face","Hair","Behind","FX","Other" };


    private static readonly string[] Directions = { "Up", "Left", "Down", "Right" };





    private float previewMaxWeight = 60f;





    private struct SlotGroup { public string label; public string[] names; }


    private static readonly SlotGroup[] Groups =


    {


        new SlotGroup{label="Locomotion", names=new[]{"Walk","Run","Idle"}},


        new SlotGroup{label="Combat",     names=new[]{"Slash","Thrust","Shoot","Spellcast"}},


        new SlotGroup{label="One-Handed", names=new[]{"1h_Slash","1h_Backslash","1h_Halfslash"}},


        new SlotGroup{label="Reactions",  names=new[]{"Combat","Hurt","Die"}},


        new SlotGroup{label="Actions",    names=new[]{"Jump","Sit","Climb","Emote","Watering"}},


    };





    private static Color RarityBg(ItemRarity r) => r switch


    {


        ItemRarity.Common => new Color(0.25f, 0.25f, 0.25f),


        ItemRarity.Uncommon => new Color(0.10f, 0.35f, 0.10f),


        ItemRarity.Rare => new Color(0.10f, 0.20f, 0.45f),


        ItemRarity.Epic => new Color(0.30f, 0.08f, 0.45f),


        ItemRarity.Legendary => new Color(0.50f, 0.30f, 0.02f),


        _ => Color.gray,


    };





    private string GetAnimIcon(string animName) => animName switch


    {


        "Walk" => "🚶",


        "Run" => "🏃",


        "Idle" => "💤",


        "Slash" => "⚔️",


        "Thrust" => "🗡️",


        "Shoot" => "🏹",


        "Spellcast" => "✨",


        "1h_Slash" => "⚔️",


        "1h_Backslash" => "🛡️",


        "1h_Halfslash" => "⚔️",


        "Combat" => "🛡️",


        "Hurt" => "💥",


        "Die" => "💀",


        "Jump" => "🦘",


        "Sit" => "🪑",


        "Climb" => "🧗",


        "Emote" => "💬",


        "Watering" => "💧",


        _ => "🎬"


    };





    // =========================================================================


    //  GUI STYLE CACHE


    // =========================================================================





    private static GUIStyle styleTitle;


    private static GUIStyle styleRarityHeader;


    private static GUIStyle styleGroupHeader;


    private static GUIStyle styleSlotName;


    private static GUIStyle styleSlotNameDimmed;


    private static GUIStyle styleMiniLabel;


    private static GUIStyle styleMiniLabelBoldOk;


    private static GUIStyle styleMiniLabelBoldErr;


    private static GUIStyle styleMiniLabelItalic;


    private static GUIStyle styleCreateLabel;


    private static GUIStyle styleFoldoutHeader;


    private static GUIStyle styleFoldoutSub;





    private void InitStyles()


    {


        if (styleTitle != null) return;





        styleTitle = new GUIStyle(EditorStyles.boldLabel)


        {


            fontSize = 15,


            alignment = TextAnchor.MiddleCenter


        };





        styleRarityHeader = new GUIStyle(EditorStyles.helpBox)


        {


            fontSize = 12,


            fontStyle = FontStyle.Bold,


            alignment = TextAnchor.MiddleCenter


        };





        styleGroupHeader = new GUIStyle(EditorStyles.boldLabel)


        {


            fontSize = 11,


            normal = { textColor = Color.white }


        };





        styleSlotName = new GUIStyle(EditorStyles.boldLabel)


        {


            fontSize = 12,


            normal = { textColor = Color.white }


        };





        styleSlotNameDimmed = new GUIStyle(EditorStyles.boldLabel)


        {


            fontSize = 12,


            normal = { textColor = Color.gray }


        };





        styleMiniLabel = new GUIStyle(EditorStyles.miniLabel)


        {


            normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }


        };





        styleMiniLabelBoldOk = new GUIStyle(EditorStyles.miniLabel)


        {


            fontStyle = FontStyle.Bold,


            normal = { textColor = new Color(0.4f, 1f, 0.4f) }


        };





        styleMiniLabelBoldErr = new GUIStyle(EditorStyles.miniLabel)


        {


            fontStyle = FontStyle.Bold,


            normal = { textColor = Color.red }


        };





        styleMiniLabelItalic = new GUIStyle(EditorStyles.miniLabel)


        {


            fontStyle = FontStyle.Italic,


            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }


        };





        styleCreateLabel = new GUIStyle(EditorStyles.helpBox)


        {


            alignment = TextAnchor.MiddleCenter


        };





        styleFoldoutHeader = new GUIStyle(EditorStyles.foldoutHeader)


        {


            fontStyle = FontStyle.Bold,


            fontSize = 12


        };





        styleFoldoutSub = new GUIStyle(EditorStyles.foldout)


        {


            fontStyle = FontStyle.Bold


        };


    }





    // =========================================================================


    //  OPEN / LIFECYCLE


    // =========================================================================





    [MenuItem("Tools/LPC Item Creator")]


    public static void Open()


    {


        var w = GetWindow<LPCItemCreator>("LPC Item Creator");


        w.minSize = new Vector2(660, 580);


        if (w.slots.Count == 0) w.slots = DefaultSlots().ToList();


    }





    private void OnEnable()


    {


        if (slots.Count == 0) slots = DefaultSlots().ToList();


    }





    // =========================================================================


    //  OnGUI


    // =========================================================================





    private void OnGUI()


    {


        InitStyles();


        scroll = EditorGUILayout.BeginScrollView(scroll);


        DrawHeader();


        DrawIdentityPanel();


        DrawWeightPanel();


        DrawDurabilityPanel();


        DrawSpritePanel();


        DrawStatsPanel();


        DrawSlotsPanel();


        DrawCreateButton();


        EditorGUILayout.EndScrollView();


    }





    // =========================================================================


    //  DRAW - HEADER


    // =========================================================================





    private void DrawHeader()


    {


        EditorGUILayout.Space(8);


        EditorGUILayout.LabelField("LPC Item Creator v9 (Unity 6)", styleTitle);


        EditorGUILayout.Space(3);





        GUI.backgroundColor = RarityBg(rarity);


        EditorGUILayout.LabelField(


            $"  {LPCItemData.RarityLabel(rarity)}  --  {itemName}",


            styleRarityHeader,


            GUILayout.Height(26));


        GUI.backgroundColor = Color.white;


        EditorGUILayout.Space(4);


    }





    // =========================================================================


    //  DRAW - IDENTITY


    // =========================================================================





    private void DrawIdentityPanel()


    {


        foldIdentity = DrawFoldout(foldIdentity, "Identity & Info");


        if (!foldIdentity) return;





        EditorGUILayout.BeginVertical(EditorStyles.helpBox);





        itemName = EditorGUILayout.TextField("Item Name", itemName).Trim();


        childPath = EditorGUILayout.TextField("Child Path", childPath).Trim();





        // sortingOrder removed — SetLayerOrder.cs controls order


        EditorGUILayout.HelpBox(


            "Sorting Order is managed by SetLayerOrder.cs — do not set it here.",


            MessageType.None);





        int ci = Mathf.Max(System.Array.IndexOf(Categories, category), 0);


        ci = EditorGUILayout.Popup("Category", ci, Categories);


        if (ci >= 0) category = Categories[ci];


        if (category == "Weapon")
        {
            var oldType = weaponType;
            weaponType = (LPCPlayerController2.WeaponType)EditorGUILayout.EnumPopup("Weapon Type", weaponType);
            if (weaponType != oldType)
            {
                ApplyWeaponPresets(weaponType);
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Weapon Combat Config", EditorStyles.boldLabel);
            attackRange = EditorGUILayout.FloatField("Attack Range (blocks/meters)", attackRange);
            attackAngle = EditorGUILayout.Slider("Attack Angle (Melee)", attackAngle, 10f, 360f);
            isRanged = EditorGUILayout.Toggle("Is Ranged (spawns projectile)", isRanged);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Directional Visibility Rules", EditorStyles.boldLabel);
        visibilityUp = (DirectionalVisibility)EditorGUILayout.EnumPopup("Visibility Up (North)", visibilityUp);
        visibilityLeft = (DirectionalVisibility)EditorGUILayout.EnumPopup("Visibility Left (West)", visibilityLeft);
        visibilityDown = (DirectionalVisibility)EditorGUILayout.EnumPopup("Visibility Down (South)", visibilityDown);
        visibilityRight = (DirectionalVisibility)EditorGUILayout.EnumPopup("Visibility Right (East)", visibilityRight);
        EditorGUILayout.EndVertical();


        outputRoot = EditorGUILayout.TextField("Output Folder", outputRoot).Trim();





        EditorGUILayout.Space(4);





        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.LabelField("Rarity", GUILayout.Width(EditorGUIUtility.labelWidth));


        GUI.backgroundColor = RarityBg(rarity) * 2f;


        rarity = (ItemRarity)EditorGUILayout.EnumPopup(rarity);


        GUI.backgroundColor = Color.white;


        EditorGUILayout.EndHorizontal();





        EditorGUILayout.Space(4);


        EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);


        description = EditorGUILayout.TextArea(description,


            GUILayout.MinHeight(52), GUILayout.MaxHeight(80));





        EditorGUILayout.Space(4);





        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.LabelField("Icon", GUILayout.Width(EditorGUIUtility.labelWidth));


        manualIcon = (Sprite)EditorGUILayout.ObjectField(


            manualIcon, typeof(Sprite), false,


            GUILayout.Width(52), GUILayout.Height(52));


        EditorGUILayout.BeginVertical();


        if (manualIcon != null)


        {


            GUI.color = Color.green;


            EditorGUILayout.LabelField("Manual icon (priority)", EditorStyles.miniLabel);


            GUI.color = Color.white;


            EditorGUILayout.LabelField(manualIcon.name, EditorStyles.miniLabel);


            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(60)))


                manualIcon = null;


        }


        else


        {


            GUI.color = new Color(0.7f, 0.7f, 0.7f);


            EditorGUILayout.LabelField("Drag Sprite here", EditorStyles.miniLabel);


            EditorGUILayout.LabelField("(empty = auto from Idle_Down[0])", EditorStyles.miniLabel);


            GUI.color = Color.white;


        }


        EditorGUILayout.EndVertical();


        EditorGUILayout.EndHorizontal();





        EditorGUILayout.EndVertical();


        EditorGUILayout.Space(4);


    }





    // =========================================================================


    //  DRAW - WEIGHT


    // =========================================================================





    private void DrawWeightPanel()


    {


        EditorGUILayout.BeginVertical(EditorStyles.helpBox);





        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.LabelField("Weight", EditorStyles.boldLabel);





        float ratio = previewMaxWeight > 0 ? itemWeight / previewMaxWeight : 0f;


        string tierLabel = ratio > 0.75f ? "HEAVY  -50% SPD"


                         : ratio > 0.50f ? "ENCUMBERED  -20% SPD"


                         : "Light -- OK";


        Color tierColor = ratio > 0.75f ? Color.red


                         : ratio > 0.50f ? Color.yellow


                         : Color.green;


        GUI.color = tierColor;


        EditorGUILayout.LabelField($"  [{tierLabel}]", EditorStyles.miniLabel);


        GUI.color = Color.white;


        EditorGUILayout.EndHorizontal();





        itemWeight = EditorGUILayout.FloatField("Weight (kg)", itemWeight);


        if (itemWeight < 0f) itemWeight = 0f;





        previewMaxWeight = EditorGUILayout.FloatField(


            new GUIContent("Preview maxCarryWeight",


                "Editor preview only. Actual value comes from LPCEquipmentManager."),


            previewMaxWeight);





        Rect barRect = EditorGUILayout.GetControlRect(false, 8f);


        float pct = previewMaxWeight > 0 ? Mathf.Clamp01(itemWeight / previewMaxWeight) : 0f;


        EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));


        EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * pct, barRect.height), tierColor);





        EditorGUILayout.HelpBox(


            "Speed penalty (applied to total equipped weight):\n" +


            "  > 50% carry weight  ->  -20% move speed\n" +


            "  > 75% carry weight  ->  -50% move speed",


            MessageType.Info);





        EditorGUILayout.EndVertical();


        EditorGUILayout.Space(4);


    }





    // =========================================================================


    //  DRAW - DURABILITY


    // =========================================================================





    private void DrawDurabilityPanel()


    {


        EditorGUILayout.BeginVertical(EditorStyles.helpBox);





        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.LabelField("Durability", EditorStyles.boldLabel);


        float ratio = maxDurability > 0 ? currentDurability / maxDurability : 0f;


        GUI.color = Color.Lerp(Color.red, Color.green, ratio);


        EditorGUILayout.LabelField(


            currentDurability <= 0 ? "  BROKEN (-75% dmg)" : $"  {ratio * 100:F0}%",


            EditorStyles.miniLabel);


        GUI.color = Color.white;


        EditorGUILayout.EndHorizontal();





        maxDurability = EditorGUILayout.FloatField("Max Durability", maxDurability);


        currentDurability = EditorGUILayout.Slider("Current Durability",


            currentDurability, 0f, Mathf.Max(maxDurability, 1f));





        if (currentDurability <= 0f)


        {


            GUI.color = new Color(1f, 0.4f, 0.4f);


            EditorGUILayout.LabelField("Broken! Damage dealt will be reduced by 75%.",


                EditorStyles.miniLabel);


            GUI.color = Color.white;


        }





        EditorGUILayout.EndVertical();


        EditorGUILayout.Space(4);


    }





    // =========================================================================


    //  DRAW - SPRITE SETTINGS


    // =========================================================================





    private void DrawSpritePanel()


    {


        foldSprite = DrawFoldout(foldSprite, "Sprite Settings");


        if (!foldSprite) return;





        EditorGUILayout.BeginVertical(EditorStyles.helpBox);


        pixelsPerUnit = EditorGUILayout.IntField("PPU", pixelsPerUnit);


        frameRate = EditorGUILayout.IntSlider("Frame Rate", frameRate, 1, 60);


        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);


        autoDetect = EditorGUILayout.Toggle("Auto-Detect Frames", autoDetect);


        compactView = EditorGUILayout.Toggle("Compact View", compactView);


        pivotMode = (PivotMode)EditorGUILayout.EnumPopup("Chế độ Pivot (Pivot Mode)", pivotMode);
        if (pivotMode == PivotMode.Custom)
        {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot (X, Y)", customPivot);
        }


        EditorGUILayout.HelpBox(


            "Icon is auto-extracted from the first frame of Idle_Down (LPC row 2).",


            MessageType.Info);


        EditorGUILayout.EndVertical();


        EditorGUILayout.Space(4);


    }





    // =========================================================================


    //  DRAW - STATS


    // =========================================================================





    private int activeStatsTab = 0;


    private static readonly string[] StatsTabNames = { "Cơ Bản (Core)", "Tấn Công", "Phòng Thủ", "Thuộc Tính", "Nguyên Tố", "Đa Dụng", "Đặc Biệt" };





    private void DrawStatsPanel()


    {


        EditorGUILayout.Space(2);


        EditorGUILayout.BeginVertical(EditorStyles.helpBox);


        EditorGUILayout.LabelField("📊 Chỉ Số & Thuộc Tính Trang Bị", EditorStyles.boldLabel);


        EditorGUILayout.Space(4);





        activeStatsTab = GUILayout.Toolbar(activeStatsTab, StatsTabNames, GUILayout.Height(24));


        EditorGUILayout.Space(6);





        EditorGUI.indentLevel++;





        switch (activeStatsTab)


        {


            case 0: // Core


                statCore.maxHP = FloatField("Máu tối đa (+ Max HP)", statCore.maxHP);


                statCore.maxMP = FloatField("Năng lượng (+ Max MP)", statCore.maxMP);


                statCore.maxStamina = FloatField("Thể lực (+ Max Stamina)", statCore.maxStamina);


                statCore.hpRegen = FloatField("Hồi máu / giây (HP Regen/s)", statCore.hpRegen);


                statCore.mpRegen = FloatField("Hồi mana / giây (MP Regen/s)", statCore.mpRegen);


                break;


            case 1: // Offensive


                statOffensive.atk = FloatField("Sát thương vật lý (ATK)", statOffensive.atk);


                statOffensive.matk = FloatField("Sát thương phép (MATK)", statOffensive.matk);


                statOffensive.critRate = SliderField("Tỷ lệ chí mạng (Crit Rate)", statOffensive.critRate, 0, 1);


                statOffensive.critDamage = FloatField("Sát thương chí mạng (Crit Dmg x)", statOffensive.critDamage);


                statOffensive.attackSpeed = FloatField("Tốc độ đánh (Attack Speed)", statOffensive.attackSpeed);


                statOffensive.castSpeed = FloatField("Tốc độ niệm phép (Cast Speed)", statOffensive.castSpeed);


                statOffensive.armorPenetration = FloatField("Xuyên giáp (Armor Pen)", statOffensive.armorPenetration);


                statOffensive.magicPenetration = FloatField("Xuyên kháng phép (Magic Pen)", statOffensive.magicPenetration);


                statOffensive.finalDamageMultiplier = FloatField("Hệ số sát thương cuối (Final Dmg x)", statOffensive.finalDamageMultiplier);


                statOffensive.trueDamage = FloatField("Sát thương chuẩn (True Damage)", statOffensive.trueDamage);


                statOffensive.lifesteal = SliderField("Hút máu (Lifesteal)", statOffensive.lifesteal, 0, 1);


                statOffensive.spellVamp = SliderField("Hút máu phép (Spell Vamp)", statOffensive.spellVamp, 0, 1);


                break;


            case 2: // Defensive


                statDefensive.def = FloatField("Giáp vật lý (DEF)", statDefensive.def);


                statDefensive.mdef = FloatField("Kháng phép (MDEF)", statDefensive.mdef);


                statDefensive.dodge = SliderField("Né tránh (Dodge)", statDefensive.dodge, 0, 1);


                statDefensive.blockRate = SliderField("Tỷ lệ đỡ đòn (Block Rate)", statDefensive.blockRate, 0, 1);


                statDefensive.blockAmount = SliderField("Lượng sát thương đỡ (Block Amount)", statDefensive.blockAmount, 0, 1);


                statDefensive.damageReduction = SliderField("Giảm sát thương (Dmg Reduction)", statDefensive.damageReduction, 0, 1);


                statDefensive.shield = FloatField("Lá chắn hấp thụ (Shield)", statDefensive.shield);


                statDefensive.tenacity = SliderField("Kháng hiệu ứng (Tenacity)", statDefensive.tenacity, 0, 1);


                break;


            case 3: // Attributes


                statAttributes.str = EditorGUILayout.IntField("Sức mạnh (STR)", statAttributes.str);


                statAttributes.dex = EditorGUILayout.IntField("Khéo léo (DEX)", statAttributes.dex);


                statAttributes.@int = EditorGUILayout.IntField("Trí tuệ (INT)", statAttributes.@int);


                statAttributes.vit = EditorGUILayout.IntField("Sức chống chịu (VIT)", statAttributes.vit);


                statAttributes.agi = EditorGUILayout.IntField("Nhanh nhẹn (AGI)", statAttributes.agi);


                statAttributes.luk = EditorGUILayout.IntField("May mắn (LUK)", statAttributes.luk);


                break;


            case 4: // Elemental


                EditorGUILayout.LabelField("Sát Thương Nguyên Tố", EditorStyles.miniBoldLabel);


                statElemental.fireDamage = FloatField("Hỏa (Fire Dmg)", statElemental.fireDamage);


                statElemental.iceDamage = FloatField("Băng (Ice Dmg)", statElemental.iceDamage);


                statElemental.lightningDamage = FloatField("Lôi (Lightning Dmg)", statElemental.lightningDamage);


                statElemental.darkDamage = FloatField("Ám (Dark Dmg)", statElemental.darkDamage);


                statElemental.lightDamage = FloatField("Quang (Light Dmg)", statElemental.lightDamage);


                EditorGUILayout.Space(4);


                EditorGUILayout.LabelField("Kháng Nguyên Tố", EditorStyles.miniBoldLabel);


                statElemental.fireResistance = SliderField("Kháng Hỏa (Fire Res)", statElemental.fireResistance, 0, 1);


                statElemental.iceResistance = SliderField("Kháng Băng (Ice Res)", statElemental.iceResistance, 0, 1);


                statElemental.lightningResistance = SliderField("Kháng Lôi (Lightning Res)", statElemental.lightningResistance, 0, 1);


                statElemental.darkResistance = SliderField("Kháng Ám (Dark Res)", statElemental.darkResistance, 0, 1);


                statElemental.lightResistance = SliderField("Kháng Quang (Light Res)", statElemental.lightResistance, 0, 1);


                break;


            case 5: // Utility


                statUtility.moveSpeed = FloatField("Tốc độ di chuyển (Move Speed)", statUtility.moveSpeed);


                statUtility.cooldownReduction = SliderField("Giảm hồi chiêu (CDR)", statUtility.cooldownReduction, 0, 1);


                statUtility.healingBonus = SliderField("Tăng hiệu quả hồi phục (Healing+)", statUtility.healingBonus, 0, 2);


                statUtility.effectDurationBonus = SliderField("Thời gian hiệu ứng (Duration+)", statUtility.effectDurationBonus, 0, 1);


                statUtility.effectResistance = SliderField("Kháng hiệu ứng bất lợi (Effect Resist)", statUtility.effectResistance, 0, 1);


                break;


            case 6: // Special


                statSpecial.goldBonus = SliderField("Tăng Vàng nhận được (Gold+)", statSpecial.goldBonus, 0, 2);


                statSpecial.dropRate = SliderField("Tỷ lệ rơi đồ (Drop Rate+)", statSpecial.dropRate, 0, 2);


                statSpecial.expBonus = SliderField("Tăng Kinh Nghiệm (EXP+)", statSpecial.expBonus, 0, 2);


                statSpecial.executeDamageThreshold = SliderField("Ngưỡng kết liễu quái (Execute < HP%)", statSpecial.executeDamageThreshold, 0, 1);


                statSpecial.reflectDamage = SliderField("Phản sát thương (Reflect Dmg)", statSpecial.reflectDamage, 0, 1);


                statSpecial.aggro = FloatField("Độ thu hút quái (Aggro)", statSpecial.aggro);


                break;


        }





        EditorGUI.indentLevel--;


        EditorGUILayout.EndVertical();


        EditorGUILayout.Space(4);


    }





    // =========================================================================


    //  DRAW - ANIMATION SLOTS


    // =========================================================================





    private void DrawSlotsPanel()


    {


        foldSlots = DrawFoldout(foldSlots, "🎬 Animation Slots Mapping");


        if (!foldSlots) return;





        EditorGUILayout.BeginVertical(EditorStyles.helpBox);


        // Premium Folder Auto-Populate Drop Zone
        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(0.1f, 0.6f, 1f, 0.12f); // Sleek glassmorphic cyan tint
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("📁 TỰ ĐỘNG GÁN NHANH TỪ THƯ MỤC (Auto-Populate Folder)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "💡 Kéo và thả thư mục chứa toàn bộ ảnh hoạt ảnh (như walk.png, run.png...) vào ô dưới đây để gán nhanh toàn bộ 18 ô trong 1 giây!",
            MessageType.Info);
            
        EditorGUI.BeginChangeCheck();
        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Kéo thư mục vào đây", folderAsset, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck() && folderAsset != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(folderAsset);
            AutoPopulateFromFolder(folderPath);
            folderAsset = null; // Clear immediately for subsequent drops
        }
        EditorGUILayout.Space(2);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);





        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.LabelField("Gán Sprite Sheet cho từng động tác của trang bị:", EditorStyles.miniLabel);


        GUILayout.FlexibleSpace();





        // Đồng bộ hóa/đưa nút toggle Compact View xuống đây để sử dụng thuận tiện nhất


        compactView = GUILayout.Toggle(compactView, "🔍 Chế độ rút gọn (Compact)", "Button", GUILayout.Width(190));





        if (GUILayout.Button("🔄 Reset về mặc định", EditorStyles.miniButton, GUILayout.Width(130)))


        {


            if (EditorUtility.DisplayDialog("Reset Slots", "Bạn có chắc chắn muốn reset toàn bộ slot về mặc định?", "Có", "Không"))


                slots = DefaultSlots().ToList();


        }


        if (GUILayout.Button("🗑️ Clear tất cả", EditorStyles.miniButton, GUILayout.Width(100)))


        {


            foreach (var s in slots) { s.texture = null; s.detectedCols = s.detectedRows = 0; }


        }


        EditorGUILayout.EndHorizontal();





        if (compactView)


        {


            EditorGUILayout.HelpBox("💡 Đang bật 'Compact View'. Các Slot trống sẽ thu gọn thành dòng gọn gàng (Micro Row) bên dưới. Bạn có thể kéo thả trực tiếp sprite sheet vào ô ObjectField ở bên phải dòng đó bất kỳ lúc nào để cấu hình nhanh!", MessageType.Info);


        }





        foreach (var g in Groups)


        {


            var gs = slots.Where(s => g.names.Contains(s.animName)).ToList();


            if (gs.Count == 0) continue;





            EditorGUILayout.Space(6);





            // Vẽ Group Header dạng thanh ngang đẹp mắt


            int filledCount = gs.Count(s => s.texture != null);


            int activeCount = gs.Count(s => !s.skip);


            string groupStatus = filledCount > 0 ? $" ({filledCount}/{activeCount} đã gán)" : "";





            GUI.backgroundColor = new Color(0.18f, 0.18f, 0.18f);


            EditorGUILayout.BeginHorizontal("box");


            GUI.backgroundColor = Color.white; // Restore background color immediately to avoid darkening child elements





            EditorGUILayout.LabelField($"📁 {g.label.ToUpper()}{groupStatus}", styleGroupHeader);





            GUILayout.FlexibleSpace();





            if (GUILayout.Button("Skip Group", EditorStyles.miniButton, GUILayout.Width(80)))


            {


                foreach (var s in gs) s.skip = true;


            }


            if (GUILayout.Button("Enable Group", EditorStyles.miniButton, GUILayout.Width(90)))


            {


                foreach (var s in gs) s.skip = false;


            }


            EditorGUILayout.EndHorizontal();





            // Vẽ các Slot trong group


            foreach (var slot in gs)


            {


                if (compactView && slot.texture == null)


                {


                    // Nếu Compact View bật và slot trống, vẽ Micro Row siêu gọn


                    DrawMicroSlot(slot);


                }


                else


                {


                    // Vẽ Full Card tiêu chuẩn


                    DrawSlot(slot);


                }


            }


        }





        EditorGUILayout.EndVertical();


    }





    private void DrawSlot(AnimSlot slot)


    {


        Color originalBg = GUI.backgroundColor;


        if (slot.skip)


        {


            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.6f); // Xám mờ


        }


        else if (slot.texture != null)


        {


            GUI.backgroundColor = new Color(0.15f, 0.38f, 0.18f, 0.85f); // Xanh lục đậm tinh tế


        }


        else


        {


            GUI.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 0.9f); // Xám sẫm tiêu chuẩn


        }





        EditorGUILayout.BeginVertical("helpBox");


        GUI.backgroundColor = originalBg; // Reset lại màu nền để các control bên trong vẽ đúng màu





        EditorGUILayout.BeginHorizontal();





        // 1. SKIP CHECKBOX & ICON + NAME


        EditorGUILayout.BeginVertical(GUILayout.Width(150));


        EditorGUILayout.BeginHorizontal();


        slot.skip = GUILayout.Toggle(slot.skip, GUIContent.none, GUILayout.Width(16));





        var nameStyle = slot.skip ? styleSlotNameDimmed : styleSlotName;


        if (!slot.skip && slot.animName == "Idle")


        {


            // Màu vàng cam sang trọng cho Idle


            var idleStyle = new GUIStyle(nameStyle) { normal = { textColor = new Color(1f, 0.85f, 0.4f) } };


            EditorGUILayout.LabelField($"{GetAnimIcon(slot.animName)} {slot.animName}", idleStyle, GUILayout.Width(120));


        }


        else


        {


            EditorGUILayout.LabelField($"{GetAnimIcon(slot.animName)} {slot.animName}", nameStyle, GUILayout.Width(120));


        }


        EditorGUILayout.EndHorizontal();





        if (slot.skip)


        {


            EditorGUILayout.LabelField("🚫 Bị bỏ qua khi build", styleMiniLabel);


        }


        else


        {


            EditorGUILayout.LabelField($"{slot.realFrames}f | {(slot.loop ? "Lặp lại" : "Chạy 1 lần")} | {(slot.directionless ? "1 Hướng" : "4 Hướng")}", styleMiniLabel);


        }


        EditorGUILayout.EndVertical();





        EditorGUILayout.Space(8);





        // 2. DRAG ZONE (TEXTURE FIELD)


        var prev = slot.texture;


        slot.texture = (Texture2D)EditorGUILayout.ObjectField(


            slot.texture, typeof(Texture2D), false,


            GUILayout.Height(46), GUILayout.Width(46));


        if (slot.texture != prev && slot.texture != null) AutoDetect(slot);





        EditorGUILayout.Space(8);





        // 3. CONFIG ZONE


        EditorGUILayout.BeginVertical();


        if (slot.skip)


        {


            GUI.enabled = false;


            EditorGUILayout.LabelField("Slot này đã bị tắt. Tích chọn ô vuông bên trái để mở lại.", styleMiniLabelItalic);


            GUI.enabled = true;


        }


        else if (slot.texture != null)


        {


            EditorGUILayout.BeginHorizontal();


            EditorGUILayout.LabelField("Khổ Sprite:", styleMiniLabel, GUILayout.Width(62));





            EditorGUI.BeginChangeCheck();


            EditorGUIUtility.labelWidth = 14;


            slot.spriteWidth = EditorGUILayout.IntField("W", slot.spriteWidth, GUILayout.Width(46));


            slot.spriteHeight = EditorGUILayout.IntField("H", slot.spriteHeight, GUILayout.Width(46));


            EditorGUIUtility.labelWidth = 0;


            if (EditorGUI.EndChangeCheck()) AutoDetect(slot);





            string texPath = AssetDatabase.GetAssetPath(slot.texture);


            bool valid = !string.IsNullOrEmpty(texPath);


            if (valid)


            {


                EditorGUILayout.LabelField($"   ✓ OK ({slot.detectedCols}x{slot.detectedRows} ô)", styleMiniLabelBoldOk);


            }


            else


            {


                EditorGUILayout.LabelField("   ✗ File không nằm trong Assets!", styleMiniLabelBoldErr);


            }





            EditorGUILayout.EndHorizontal();





            EditorGUILayout.BeginHorizontal();


            EditorGUILayout.LabelField("Số Frame:", styleMiniLabel, GUILayout.Width(62));


            slot.realFrames = EditorGUILayout.IntSlider(


                slot.realFrames, 1, Mathf.Max(slot.detectedCols, slot.realFrames, 1), GUILayout.Width(160));





            bool singleDir = slot.directionless || slot.detectedRows == 1;


            var catStyle = new GUIStyle(styleMiniLabel) { normal = { textColor = new Color(0.5f, 0.8f, 1f) } };


            EditorGUILayout.LabelField(


                singleDir ? "   ➡️ Đơn hướng" : "   🧭 4 hướng (LPC)", catStyle);


            EditorGUILayout.EndHorizontal();


        }


        else


        {


            EditorGUILayout.LabelField($"👉 Kéo thả file sprite sheet '{slot.animName.ToLower()}.png' vào ô vuông bên cạnh.", styleMiniLabelItalic);


            EditorGUILayout.LabelField($"Cấu hình mặc định: {slot.spriteWidth}x{slot.spriteHeight} px | {slot.realFrames} frames", styleMiniLabelItalic);


        }


        EditorGUILayout.EndVertical();





        // 4. ACTION BUTTON


        if (!slot.skip && slot.texture != null)


        {


            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f, 0.9f);


            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(46)))


            {


                slot.texture = null;


                slot.detectedCols = slot.detectedRows = 0;


            }


            GUI.backgroundColor = originalBg;


        }





        EditorGUILayout.EndHorizontal();


        EditorGUILayout.EndVertical();


        EditorGUILayout.Space(1);


    }





    private void DrawMicroSlot(AnimSlot slot)


    {


        Color originalBg = GUI.backgroundColor;


        


        // Thiết lập màu nền dựa trên skip


        if (slot.skip)


        {


            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Xám mờ cho skip


        }


        else


        {


            GUI.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 0.9f); // Xám sẫm trang nhã


        }





        // Vẽ dòng Micro Row bằng helpBox mặc định với chiều cao ổn định


        EditorGUILayout.BeginHorizontal("helpBox", GUILayout.Height(28));


        GUI.backgroundColor = originalBg; // Khôi phục ngay lập tức để các control con vẽ đúng màu





        // 1. Skip checkbox (căn giữa theo chiều dọc)


        EditorGUILayout.BeginVertical(GUILayout.Width(16), GUILayout.Height(20));


        GUILayout.FlexibleSpace();


        slot.skip = GUILayout.Toggle(slot.skip, GUIContent.none, GUILayout.Width(16));


        GUILayout.FlexibleSpace();


        EditorGUILayout.EndVertical();





        EditorGUILayout.Space(4);





        // 2. Icon + Tên Slot


        EditorGUILayout.BeginVertical(GUILayout.Width(140), GUILayout.Height(20));


        GUILayout.FlexibleSpace();


        var nameStyle = slot.skip ? styleSlotNameDimmed : styleSlotName;


        var finalNameStyle = new GUIStyle(nameStyle) { fontSize = 11 };


        EditorGUILayout.LabelField($"{GetAnimIcon(slot.animName)} {slot.animName}", finalNameStyle, GUILayout.Width(130));


        GUILayout.FlexibleSpace();


        EditorGUILayout.EndVertical();





        // 3. Info tag siêu gọn


        EditorGUILayout.BeginVertical(GUILayout.Height(20));


        GUILayout.FlexibleSpace();


        if (!slot.skip)


        {


            EditorGUILayout.LabelField($"Kích thước mặc định: {slot.spriteWidth}x{slot.spriteHeight} px | {slot.realFrames} frames", styleMiniLabel);


        }


        else


        {


            EditorGUILayout.LabelField("🚫 Bị bỏ qua khi build", styleMiniLabel);


        }


        GUILayout.FlexibleSpace();


        EditorGUILayout.EndVertical();





        GUILayout.FlexibleSpace();





        // 4. Ô kéo thả siêu mỏng cùng dòng (chỉ hiện khi không skip)


        if (!slot.skip)


        {


            EditorGUILayout.BeginVertical(GUILayout.Width(150), GUILayout.Height(20));


            GUILayout.FlexibleSpace();


            var prev = slot.texture;


            // Chiều cao ObjectField là 18px để vừa vặn, chiều rộng 140px cực kỳ gọn gàng


            slot.texture = (Texture2D)EditorGUILayout.ObjectField(


                slot.texture, typeof(Texture2D), false, GUILayout.Width(140), GUILayout.Height(18));


            if (slot.texture != prev && slot.texture != null) AutoDetect(slot);


            GUILayout.FlexibleSpace();


            EditorGUILayout.EndVertical();


        }





        EditorGUILayout.EndHorizontal();


    }





    // =========================================================================


    //  DRAW - CREATE BUTTON


    // =========================================================================





    private void DrawCreateButton()


    {


        EditorGUILayout.Space(10);


        int filled = slots.Count(s => s.texture != null && !s.skip);


        bool validName = itemName.Length > 0 && !itemName.Contains(" ");





        if (!validName)


            EditorGUILayout.HelpBox("Item Name must not contain spaces!", MessageType.Error);





        GUI.backgroundColor = RarityBg(rarity) * 1.8f;


        EditorGUILayout.LabelField(


            $"  {LPCItemData.RarityLabel(rarity)}  |  {itemWeight:0.#}kg  |  " +


            $"{(filled > 0 ? $"{filled} slots" : "No slots")}  ->  {outputRoot}/{itemName}/",


            styleCreateLabel,


            GUILayout.Height(22));


        GUI.backgroundColor = Color.white;





        GUI.enabled = filled > 0 && validName;


        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);


        if (GUILayout.Button("Create Item (Unity 6)", GUILayout.Height(52)))


            CreateItem();


        GUI.backgroundColor = Color.white;


        GUI.enabled = true;


        EditorGUILayout.Space(8);


    }





    // =========================================================================


    //  AUTO DETECT


    // =========================================================================





    private void AutoPopulateFromFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        int matchedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path).ToLower()
                                  .Replace(" ", "").Replace("-", "").Replace("_", "");

            foreach (var slot in slots)
            {
                string slotNameNormalized = slot.animName.ToLower()
                                                .Replace(" ", "").Replace("-", "").Replace("_", "");

                if (fileName == slotNameNormalized)
                {
                    slot.texture = tex;
                    AutoDetect(slot);
                    matchedCount++;
                    break;
                }
            }
        }

        if (matchedCount > 0)
        {
            Debug.Log($"[LPCItemCreator] Auto-populated {matchedCount} animation slots from folder: {folderPath}");
            EditorUtility.DisplayDialog("Auto Populate Success", $"Đã tự động nhận diện và gán {matchedCount} ảnh hoạt ảnh thành công!", "Tuyệt vời");
        }
        else
        {
            EditorUtility.DisplayDialog("Auto Populate Info", "Không tìm thấy tệp ảnh nào có tên trùng khớp với các ô hoạt ảnh (Walk, Run, Idle...).\\n\\nVui lòng kiểm tra lại cách đặt tên file!", "OK");
        }
    }


    private void AutoDetect(AnimSlot slot)
    {
        if (slot.texture == null) return;

        string path = AssetDatabase.GetAssetPath(slot.texture);
        if (string.IsNullOrEmpty(path)) return;

        int tw, th;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null) imp.GetSourceTextureWidthAndHeight(out tw, out th);
        else { tw = slot.texture.width; th = slot.texture.height; }

        slot.detectedCols = Mathf.Max(1, tw / slot.spriteWidth);
        slot.detectedRows = Mathf.Max(1, th / slot.spriteHeight);
        slot.directionless = slot.detectedRows == 1;

        if (autoDetect)
        {
            slot.realFrames = slot.detectedCols;
        }
        else
        {
            slot.realFrames = Mathf.Clamp(slot.realFrames, 1, slot.detectedCols);
        }
    }





    // =========================================================================


    //  CREATE ITEM


    // =========================================================================





    private void CreateItem()


    {


        itemName = itemName.Trim().Replace(" ", "_");


        childPath = childPath.Trim();


        outputRoot = outputRoot.TrimEnd('/').Trim();





        try


        {


            EditorUtility.DisplayProgressBar("LPC Item Creator", "Initializing...", 0f);





            string itemFolder = $"{outputRoot}/{itemName}";


            EnsureFolder(outputRoot, itemName);





            var animSprites = new Dictionary<string, List<Sprite>>();


            var active = slots.Where(s => s.texture != null && !s.skip).ToList();





            for (int i = 0; i < active.Count; i++)


            {


                var slot = active[i];


                EditorUtility.DisplayProgressBar("LPC Item Creator",


                    $"Slicing {slot.animName} ({i + 1}/{active.Count})...",


                    (float)i / active.Count * 0.65f);





                var sprites = SliceAndLoadU6(slot);


                if (sprites == null || sprites.Count == 0) continue;





                bool singleDir = slot.directionless || slot.detectedRows == 1;


                if (singleDir)


                {


                    var frames = GetRow(sprites, 0, slot);


                    if (frames.Count > 0) animSprites[slot.animName] = frames;


                }


                else


                {


                    for (int row = 0; row < Mathf.Min(4, slot.detectedRows); row++)


                    {


                        string cat = $"{slot.animName}_{Directions[row]}";


                        var frames = GetRow(sprites, row, slot);


                        if (frames.Count > 0) animSprites[cat] = frames;


                    }


                }


            }





            if (animSprites.Count == 0)


            {


                EditorUtility.ClearProgressBar();


                EditorUtility.DisplayDialog("Error", "No sprites were generated.", "OK");


                return;


            }





            EditorUtility.DisplayProgressBar("LPC Item Creator", "Creating icon...", 0.72f);


            Sprite autoIcon = ExtractIdleDownIcon(animSprites);





            EditorUtility.DisplayProgressBar("LPC Item Creator", "Creating SpriteLibraryAsset...", 0.80f);


            var library = BuildSpriteLibrary(itemFolder, animSprites);





            EditorUtility.DisplayProgressBar("LPC Item Creator", "Creating ItemData...", 0.92f);


            CreateItemData(itemFolder, library, autoIcon);





            EditorUtility.ClearProgressBar();


            AssetDatabase.SaveAssets();


            AssetDatabase.Refresh();





            EditorUtility.DisplayDialog("Done!",


                $"Item '{itemName}' created successfully!\n\n" +


                $"Rarity: {rarity}\n" +


                $"Weight: {itemWeight:0.#} kg\n" +


                $"{animSprites.Count} categories\n" +


                $"Output: {itemFolder}/", "OK");





            EditorGUIUtility.PingObject(


                AssetDatabase.LoadAssetAtPath<Object>($"{itemFolder}/{itemName}_ItemData.asset"));


        }


        catch (System.Exception e)


        {


            EditorUtility.ClearProgressBar();


            Debug.LogError($"[ItemCreator] Exception: {e}");


            EditorUtility.DisplayDialog("Error", e.Message, "OK");


        }


    }





    private Sprite ExtractIdleDownIcon(Dictionary<string, List<Sprite>> animSprites)


    {


        if (animSprites.TryGetValue("Idle_Down", out var d) && d.Count > 0) return d[0];


        if (animSprites.TryGetValue("Idle", out var i) && i.Count > 0) return i[0];


        return animSprites.Values.FirstOrDefault(l => l.Count > 0)?[0];


    }





    // =========================================================================


    //  SLICE - UNITY 6


    // =========================================================================





    private Dictionary<string, Sprite> SliceAndLoadU6(AnimSlot slot)


    {


        string texPath = AssetDatabase.GetAssetPath(slot.texture);


        if (string.IsNullOrEmpty(texPath)) return null;





        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;


        if (importer == null) return null;





        importer.isReadable = true;


        importer.textureType = TextureImporterType.Sprite;


        importer.spriteImportMode = SpriteImportMode.Multiple;


        importer.spritePixelsPerUnit = pixelsPerUnit;


        importer.filterMode = filterMode;


        importer.textureCompression = TextureImporterCompression.Uncompressed;


        importer.alphaIsTransparency = true;


        importer.maxTextureSize = 4096;





        int tw, th;


        importer.GetSourceTextureWidthAndHeight(out tw, out th);





        int cols = Mathf.Max(1, tw / slot.spriteWidth);


        int rows = Mathf.Max(1, th / slot.spriteHeight);


        int useCols = Mathf.Clamp(slot.realFrames, 1, cols);





        slot.detectedCols = cols;


        slot.detectedRows = rows;


        if (rows == 1) slot.directionless = true;





        var rects = new List<SpriteMetaData>();


        float pX = 0.5f;
        float pY = 0.5f;
        int align = (int)SpriteAlignment.Center;

        if (pivotMode == PivotMode.BottomCenter)
        {
            pX = 0.5f;
            pY = 0f;
            align = (int)SpriteAlignment.BottomCenter;
        }
        else if (pivotMode == PivotMode.Custom)
        {
            pX = customPivot.x;
            pY = customPivot.y;
            align = (int)SpriteAlignment.Custom;
        }


        for (int r = 0; r < rows; r++)


        {


            for (int c = 0; c < useCols; c++)


            {


                rects.Add(new SpriteMetaData


                {


                    pivot = new Vector2(pX, pY),


                    alignment = align,


                    name = $"{slot.animName}_r{r}_c{c}",


                    rect = new Rect(


                        c * slot.spriteWidth,


                        (rows - r - 1) * slot.spriteHeight,


                        slot.spriteWidth,


                        slot.spriteHeight)


                });


            }


        }





        importer.spritesheet = rects.ToArray();


        EditorUtility.SetDirty(importer);


        importer.SaveAndReimport();


        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);


        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);





        var result = new Dictionary<string, Sprite>();


        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(texPath))


            if (obj is Sprite sp) result[sp.name] = sp;





        return result;


    }





    private List<Sprite> GetRow(Dictionary<string, Sprite> sprites, int row, AnimSlot slot)
    {
        var rowSprites = new List<Sprite>();
        for (int c = 0; c < slot.realFrames; c++)
        {
            string key = $"{slot.animName}_r{row}_c{c}";
            sprites.TryGetValue(key, out Sprite sp);
            rowSprites.Add(sp);
        }
        return rowSprites;
    }

    private SpriteLibraryAsset BuildSpriteLibrary(string folder, Dictionary<string, List<Sprite>> animSprites)


    {


        string path = $"{folder}/{itemName}_Library.asset";


        var existing = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);


        var asset = existing ?? ScriptableObject.CreateInstance<SpriteLibraryAsset>();





        foreach (var kv in animSprites)


            for (int i = 0; i < kv.Value.Count; i++)


            {


                if (kv.Value[i] == null) continue;


                asset.AddCategoryLabel(kv.Value[i], kv.Key, i.ToString());


            }





        if (existing == null) AssetDatabase.CreateAsset(asset, path);


        else EditorUtility.SetDirty(asset);





        AssetDatabase.SaveAssets();


        return AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);


    }





    private void CreateItemData(string folder, SpriteLibraryAsset library, Sprite icon)


    {


        string path = $"{folder}/{itemName}_ItemData.asset";


        var existing = AssetDatabase.LoadAssetAtPath<LPCItemData>(path);


        var data = existing ?? ScriptableObject.CreateInstance<LPCItemData>();





        data.itemName = itemName;


        data.childPath = childPath;


        data.category = category;


        data.weaponType = weaponType;
        data.attackRange = attackRange;
        data.attackAngle = attackAngle;
        data.isRanged = isRanged;
        data.sortingOrder = 0;   // SetLayerOrder controls order
        data.visibilityUp = visibilityUp;
        data.visibilityLeft = visibilityLeft;
        data.visibilityDown = visibilityDown;
        data.visibilityRight = visibilityRight;


        data.description = description;


        data.rarity = rarity;


        data.weight = itemWeight;


        data.maxDurability = maxDurability;


        data.currentDurability = currentDurability;


        data.spriteLibrary = library;


        data.spriteWidth = 64;
        data.spriteHeight = 64;


        data.pixelsPerUnit = pixelsPerUnit;


        data.frameRate = frameRate;


        data.core = statCore;


        data.offensive = statOffensive;


        data.defensive = statDefensive;


        data.utility = statUtility;


        data.attributes = statAttributes;


        data.elemental = statElemental;


        data.special = statSpecial;





        if (manualIcon != null) data.icon = manualIcon;


        else if (icon != null) data.icon = icon;





        if (existing == null) AssetDatabase.CreateAsset(data, path);


        else EditorUtility.SetDirty(data);





        AssetDatabase.SaveAssets();


    }





    // =========================================================================


    //  GUI HELPERS


    // =========================================================================





    private static bool DrawFoldout(bool state, string label)


    {


        GUI.backgroundColor = new Color(0.22f, 0.22f, 0.22f);


        state = EditorGUILayout.BeginFoldoutHeaderGroup(state, label, styleFoldoutHeader);


        EditorGUILayout.EndFoldoutHeaderGroup();


        GUI.backgroundColor = Color.white;


        return state;


    }





    private static bool DrawSubFoldout(bool state, string label)


        => EditorGUILayout.Foldout(state, label, true, styleFoldoutSub);





    private static float FloatField(string label, float value)


        => EditorGUILayout.FloatField(label, value);





    private static float SliderField(string label, float value, float min, float max)


        => EditorGUILayout.Slider(label, value, min, max);





    private static void EnsureFolder(string parent, string child)


    {


        string full = $"{parent}/{child}";


        if (AssetDatabase.IsValidFolder(full)) return;


        string abs = Path.GetFullPath(full).Replace('\\', '/');


        if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);


        AssetDatabase.Refresh();


        if (!AssetDatabase.IsValidFolder(full))


            AssetDatabase.CreateFolder(parent, child);


    }



    private static GUID GetDeterministicGUID(string source)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] input = System.Text.Encoding.UTF8.GetBytes(source);
            byte[] hash = md5.ComputeHash(input);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return new GUID(sb.ToString());
        }
    }

    private void ApplyWeaponPresets(LPCPlayerController2.WeaponType type)
    {
        switch (type)
        {
            case LPCPlayerController2.WeaponType.Unarmed_Slash:
                attackRange = 1.0f;
                attackAngle = 90f;
                isRanged = false;
                break;
            case LPCPlayerController2.WeaponType.OneHand_Slash: // Kiếm/Rìu thường tầm vừa
                attackRange = 1.5f;
                attackAngle = 90f;
                isRanged = false;
                break;
            case LPCPlayerController2.WeaponType.OneHand_Back: // Kiếm chém ngược tầm vừa
                attackRange = 1.5f;
                attackAngle = 90f;
                isRanged = false;
                break;
            case LPCPlayerController2.WeaponType.OneHand_Half: // Dao găm / Đoản kiếm / Chùy tầm ngắn-vừa
                attackRange = 0.8f; // Gần áp sát
                attackAngle = 90f;
                isRanged = false;
                break;
            case LPCPlayerController2.WeaponType.Thrust: // Thương / Giáo tầm dài
                attackRange = 2.5f;
                attackAngle = 30f; // Đâm thẳng nên góc hẹp hơn
                isRanged = false;
                break;
            case LPCPlayerController2.WeaponType.Bow_Shoot: // Cung/Nỏ tầm xa
                attackRange = 10f;
                attackAngle = 360f;
                isRanged = true;
                break;
            case LPCPlayerController2.WeaponType.Spell: // Pháp cụ tầm xa
                attackRange = 8f;
                attackAngle = 360f;
                isRanged = true;
                break;
        }
    }
}