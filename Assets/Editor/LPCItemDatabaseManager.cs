using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// LPC Item Database Manager — Công cụ quản lý tập trung toàn bộ trang bị dưới dạng bảng lưới Excel-like.
/// Hỗ trợ chỉnh sửa hàng loạt và xem chi tiết chỉ số.
/// </summary>
public class LPCItemDatabaseManager : EditorWindow
{
    private List<LPCItemData> allItems = new List<LPCItemData>();
    private List<LPCItemData> filteredItems = new List<LPCItemData>();
    
    // Selection and State
    private LPCItemData selectedItem;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    
    // Filters
    private string searchName = "";
    private string filterCategory = "All";
    private int filterRarity = -1; // -1 means All
    
    // Layout and Styles
    private GUIStyle headerStyle;
    private GUIStyle categoryLabelStyle;
    private GUIStyle cardStyle;
    private GUIStyle tableHeaderStyle;
    private GUIStyle tableRowStyle;
    private GUIStyle selectedRowStyle;
    
    private int statsTab = 0;
    private readonly string[] statsTabLabels = { "Định Danh", "Tấn Công & Thủ", "Bổ Trợ & Đặc Biệt", "Thuộc Tính & Nguyên Tố" };
    private readonly string[] categories = { "All", "Weapon", "Clothing", "Armor", "Face", "Hair", "Behind", "FX", "Other" };

    [MenuItem("Tools/LPC Item Database Manager", false, 10)]
    public static void Open()
    {
        var w = GetWindow<LPCItemDatabaseManager>("LPC Database");
        w.minSize = new Vector2(950, 600);
        w.LoadDatabase();
    }

    private void OnEnable()
    {
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        allItems.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LPCItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<LPCItemData>(path);
            if (item != null) allItems.Add(item);
        }
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        filteredItems = allItems.Where(item => {
            // Name search
            bool matchName = string.IsNullOrEmpty(searchName) || 
                             item.itemName.ToLower().Contains(searchName.ToLower()) ||
                             item.name.ToLower().Contains(searchName.ToLower());
            
            // Category filter
            bool matchCategory = filterCategory == "All" || item.category == filterCategory;
            
            // Rarity filter
            bool matchRarity = filterRarity == -1 || (int)item.rarity == filterRarity;
            
            return matchName && matchCategory && matchRarity;
        }).ToList();
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.15f, 0.6f, 1f) }
        };

        categoryLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            fontSize = 9,
            normal = { textColor = new Color(0.6f, 0.8f, 1f) }
        };

        cardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        tableHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        tableRowStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(0, 0, 1, 1)
        };

        selectedRowStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(0, 0, 1, 1),
            normal = { background = MakeColorTexture(new Color(0.12f, 0.28f, 0.45f, 0.6f)) }
        };
    }

    private Texture2D MakeColorTexture(Color col)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    private void OnGUI()
    {
        InitStyles();
        
        // ─── TOP HEADER & CONTROLS ──────────────────────────────────────────
        DrawHeaderAndFilters();

        // ─── MAIN COLUMN SPLIT ──────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        
        // Left Column: Item Grid Table (65% width)
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.62f));
        DrawItemsTable();
        EditorGUILayout.EndVertical();

        // Divider
        GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));
        EditorGUILayout.Space(5);

        // Right Column: Detail Inspector Panel (35% width)
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.38f - 15));
        DrawItemDetailInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // ─── BOTTOM BULK OPERATIONS ────────────────────────────────────────
        DrawBottomBar();
    }

    private void DrawHeaderAndFilters()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("📁 LPC Item Database Manager", headerStyle, GUILayout.Height(25));
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("🔄 Tải Lại Database", GUILayout.Width(130), GUILayout.Height(25)))
        {
            LoadDatabase();
        }
        if (GUILayout.Button("➕ Tạo Vật Phẩm Mới", GUILayout.Width(150), GUILayout.Height(25)))
        {
            CreateNewItem();
        }
        EditorGUILayout.EndHorizontal();

        // Filter Controls
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("Tìm kiếm:", GUILayout.Width(60));
        string newSearch = EditorGUILayout.TextField(searchName, GUILayout.Width(180));
        if (newSearch != searchName)
        {
            searchName = newSearch;
            ApplyFilters();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Phân loại:", GUILayout.Width(60));
        int catIdx = System.Array.IndexOf(categories, filterCategory);
        int newCatIdx = EditorGUILayout.Popup(catIdx >= 0 ? catIdx : 0, categories, GUILayout.Width(120));
        if (newCatIdx != catIdx)
        {
            filterCategory = categories[newCatIdx];
            ApplyFilters();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Độ hiếm:", GUILayout.Width(60));
        
        string[] rarities = { "All", "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        int newRarity = EditorGUILayout.Popup(filterRarity + 1, rarities, GUILayout.Width(120)) - 1;
        if (newRarity != filterRarity)
        {
            filterRarity = newRarity;
            ApplyFilters();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawItemsTable()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Table Header
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUILayout.LabelField("Tên Vật Phẩm (Editable)", tableHeaderStyle, GUILayout.Width(160));
        EditorGUILayout.LabelField("Phân Loại", tableHeaderStyle, GUILayout.Width(90));
        EditorGUILayout.LabelField("Độ Hiếm", tableHeaderStyle, GUILayout.Width(95));
        EditorGUILayout.LabelField("Sorting Order", tableHeaderStyle, GUILayout.Width(80));
        EditorGUILayout.LabelField("Độ Bền (C/M)", tableHeaderStyle, GUILayout.Width(100));
        EditorGUILayout.LabelField("Trọng Lượng", tableHeaderStyle, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));

        if (filteredItems.Count == 0)
        {
            EditorGUILayout.HelpBox("Không tìm thấy vật phẩm nào khớp với bộ lọc.", MessageType.Info);
        }
        else
        {
            foreach (var item in filteredItems)
            {
                if (item == null) continue;

                bool isSelected = (selectedItem == item);
                var rowStyle = isSelected ? selectedRowStyle : tableRowStyle;

                EditorGUILayout.BeginHorizontal(rowStyle);

                // Quick Select / Name Edit
                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField(item.itemName, GUILayout.Width(155));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(item, "Modify Item Name");
                    item.itemName = newName;
                    EditorUtility.SetDirty(item);
                }

                // Click detector for row selection
                var clickRect = GUILayoutUtility.GetLastRect();
                clickRect.width = position.width * 0.6f; // Span across the columns
                if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
                {
                    selectedItem = item;
                    GUI.FocusControl(null); // Clear keyboard focus to apply edits
                    Event.current.Use();
                }

                // Category
                EditorGUI.BeginChangeCheck();
                string newCat = EditorGUILayout.TextField(item.category, GUILayout.Width(85));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(item, "Modify Item Category");
                    item.category = newCat;
                    EditorUtility.SetDirty(item);
                }

                // Rarity
                EditorGUI.BeginChangeCheck();
                var newRarity = (ItemRarity)EditorGUILayout.EnumPopup(item.rarity, GUILayout.Width(90));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(item, "Modify Item Rarity");
                    item.rarity = newRarity;
                    EditorUtility.SetDirty(item);
                }

                // Sorting Order
                EditorGUI.BeginChangeCheck();
                int newOrder = EditorGUILayout.IntField(item.sortingOrder, GUILayout.Width(75));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(item, "Modify Sorting Order");
                    item.sortingOrder = newOrder;
                    EditorUtility.SetDirty(item);
                }

                // Durability (Current / Max)
                EditorGUILayout.BeginHorizontal(GUILayout.Width(95));
                EditorGUI.BeginChangeCheck();
                float newCurDur = EditorGUILayout.FloatField(item.currentDurability, GUILayout.Width(42));
                EditorGUILayout.LabelField("/", GUILayout.Width(10));
                float newMaxDur = EditorGUILayout.FloatField(item.maxDurability, GUILayout.Width(42));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(item, "Modify Durability");
                    item.currentDurability = Mathf.Clamp(newCurDur, 0, newMaxDur);
                    item.maxDurability = newMaxDur;
                    EditorUtility.SetDirty(item);
                }
                EditorGUILayout.EndHorizontal();

                // Weight
                EditorGUI.BeginChangeCheck();
                float newWeight = EditorGUILayout.FloatField(item.weight, GUILayout.Width(75));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(item, "Modify Item Weight");
                    item.weight = Mathf.Max(0, newWeight);
                    EditorUtility.SetDirty(item);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawItemDetailInspector()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        if (selectedItem == null)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Hãy chọn một vật phẩm ở bảng bên trái\nđể chỉnh sửa thông tin chi tiết.", 
                new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic });
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            return;
        }

        // Header info
        EditorGUILayout.BeginHorizontal();
        var rarityColor = selectedItem.RarityColor();
        GUI.color = rarityColor;
        EditorGUILayout.LabelField("⬤", GUILayout.Width(16));
        GUI.color = Color.white;
        
        EditorGUILayout.LabelField(selectedItem.itemName, EditorStyles.boldLabel);
        
        GUI.backgroundColor = new Color(0.85f, 0.3f, 0.3f);
        if (GUILayout.Button("Xóa", GUILayout.Width(50)))
        {
            DeleteSelectedAsset();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Asset Path label
        GUI.color = Color.gray;
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedItem), EditorStyles.miniLabel);
        GUI.color = Color.white;

        EditorGUILayout.Space(5);

        // Stats detail tabs
        statsTab = GUILayout.Toolbar(statsTab, statsTabLabels);
        EditorGUILayout.Space(8);

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

        Undo.RecordObject(selectedItem, "Edit Item Details");
        EditorGUI.BeginChangeCheck();

        switch (statsTab)
        {
            case 0:
                DrawIdentityAndSpriteTab();
                break;
            case 1:
                DrawCombatStatsTab();
                break;
            case 2:
                DrawUtilityStatsTab();
                break;
            case 3:
                DrawAttributesAndElementalTab();
                break;
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedItem);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawIdentityAndSpriteTab()
    {
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        selectedItem.itemName = EditorGUILayout.TextField("Tên Vật Phẩm", selectedItem.itemName);
        selectedItem.childPath = EditorGUILayout.TextField("Child Node Path", selectedItem.childPath);
        selectedItem.category = EditorGUILayout.TextField("Phân Loại (Category)", selectedItem.category);
        selectedItem.rarity = (ItemRarity)EditorGUILayout.EnumPopup("Độ Hiếm (Rarity)", selectedItem.rarity);
        selectedItem.sortingOrder = EditorGUILayout.IntField("Thứ Tự Vẽ (Order)", selectedItem.sortingOrder);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Mô tả vật phẩm:");
        selectedItem.description = EditorGUILayout.TextArea(selectedItem.description, GUILayout.Height(60));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sprite & Icons", EditorStyles.boldLabel);
        selectedItem.icon = (Sprite)EditorGUILayout.ObjectField("Icon / Thumbnail", selectedItem.icon, typeof(Sprite), false, GUILayout.Height(64));
        selectedItem.spriteLibrary = (UnityEngine.U2D.Animation.SpriteLibraryAsset)EditorGUILayout.ObjectField("Sprite Library Asset", selectedItem.spriteLibrary, typeof(UnityEngine.U2D.Animation.SpriteLibraryAsset), false);

        EditorGUILayout.Space(5);
        selectedItem.spriteWidth = EditorGUILayout.IntField("Sprite Width", selectedItem.spriteWidth);
        selectedItem.spriteHeight = EditorGUILayout.IntField("Sprite Height", selectedItem.spriteHeight);
        selectedItem.pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", selectedItem.pixelsPerUnit);
        selectedItem.frameRate = EditorGUILayout.IntField("Frame Rate", selectedItem.frameRate);
        selectedItem.itemController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Item Controller (FX)", selectedItem.itemController, typeof(RuntimeAnimatorController), false);
    }

    private void DrawCombatStatsTab()
    {
        EditorGUILayout.LabelField("Core Stats", EditorStyles.boldLabel);
        var core = selectedItem.core;
        core.maxHP = EditorGUILayout.FloatField("Max HP", core.maxHP);
        core.maxMP = EditorGUILayout.FloatField("Max MP", core.maxMP);
        core.maxStamina = EditorGUILayout.FloatField("Max Stamina", core.maxStamina);
        core.hpRegen = EditorGUILayout.FloatField("HP Regen/s", core.hpRegen);
        core.mpRegen = EditorGUILayout.FloatField("MP Regen/s", core.mpRegen);
        selectedItem.core = core;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Offensive Stats", EditorStyles.boldLabel);
        var off = selectedItem.offensive;
        off.atk = EditorGUILayout.FloatField("Phys Attack (ATK)", off.atk);
        off.matk = EditorGUILayout.FloatField("Magic Attack (MATK)", off.matk);
        off.critRate = EditorGUILayout.Slider("Crit Rate (0-1)", off.critRate, 0f, 1f);
        off.critDamage = EditorGUILayout.FloatField("Crit Multiplier", off.critDamage);
        off.attackSpeed = EditorGUILayout.FloatField("Attack Speed", off.attackSpeed);
        off.castSpeed = EditorGUILayout.FloatField("Cast Speed", off.castSpeed);
        off.armorPenetration = EditorGUILayout.FloatField("Armor Pen", off.armorPenetration);
        off.magicPenetration = EditorGUILayout.FloatField("Magic Pen", off.magicPenetration);
        off.finalDamageMultiplier = EditorGUILayout.FloatField("Final Dmg Mult", off.finalDamageMultiplier);
        off.trueDamage = EditorGUILayout.FloatField("True Damage", off.trueDamage);
        off.lifesteal = EditorGUILayout.Slider("Lifesteal (0-1)", off.lifesteal, 0f, 1f);
        off.spellVamp = EditorGUILayout.Slider("Spell Vamp (0-1)", off.spellVamp, 0f, 1f);
        selectedItem.offensive = off;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Defensive Stats", EditorStyles.boldLabel);
        var def = selectedItem.defensive;
        def.def = EditorGUILayout.FloatField("Physical Def", def.def);
        def.mdef = EditorGUILayout.FloatField("Magic Def", def.mdef);
        def.dodge = EditorGUILayout.Slider("Dodge Rate (0-1)", def.dodge, 0f, 1f);
        def.blockRate = EditorGUILayout.Slider("Block Rate (0-1)", def.blockRate, 0f, 1f);
        def.blockAmount = EditorGUILayout.Slider("Block Dmg Reduc (0-1)", def.blockAmount, 0f, 1f);
        def.damageReduction = EditorGUILayout.Slider("Flat Dmg Reduc (0-1)", def.damageReduction, 0f, 1f);
        def.shield = EditorGUILayout.FloatField("Shield Health", def.shield);
        def.tenacity = EditorGUILayout.Slider("CC Resistance (0-1)", def.tenacity, 0f, 1f);
        selectedItem.defensive = def;
    }

    private void DrawUtilityStatsTab()
    {
        EditorGUILayout.LabelField("Utility Stats", EditorStyles.boldLabel);
        var util = selectedItem.utility;
        util.moveSpeed = EditorGUILayout.FloatField("Move Speed Bonus", util.moveSpeed);
        util.cooldownReduction = EditorGUILayout.Slider("CDR (0-1)", util.cooldownReduction, 0f, 1f);
        util.healingBonus = EditorGUILayout.FloatField("Healing Bonus (%)", util.healingBonus);
        util.effectDurationBonus = EditorGUILayout.Slider("Effect Duration (+) (0-1)", util.effectDurationBonus, 0f, 1f);
        util.effectResistance = EditorGUILayout.Slider("Effect Resistance (-) (0-1)", util.effectResistance, 0f, 1f);
        selectedItem.utility = util;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Special Stats", EditorStyles.boldLabel);
        var spec = selectedItem.special;
        spec.goldBonus = EditorGUILayout.FloatField("Gold Bonus (%)", spec.goldBonus);
        spec.dropRate = EditorGUILayout.FloatField("Item Drop Rate (%)", spec.dropRate);
        spec.expBonus = EditorGUILayout.FloatField("Exp Bonus (%)", spec.expBonus);
        spec.executeDamageThreshold = EditorGUILayout.Slider("Execute Threshold Below X%", spec.executeDamageThreshold, 0f, 100f);
        spec.reflectDamage = EditorGUILayout.FloatField("Reflect Damage (%)", spec.reflectDamage);
        spec.aggro = EditorGUILayout.FloatField("Aggro / Threat Mult", spec.aggro);
        selectedItem.special = spec;
    }

    private void DrawAttributesAndElementalTab()
    {
        EditorGUILayout.LabelField("Attribute Stats", EditorStyles.boldLabel);
        var attr = selectedItem.attributes;
        attr.str = EditorGUILayout.IntField("Sức Mạnh (STR)", attr.str);
        attr.dex = EditorGUILayout.IntField("Khéo Léo (DEX)", attr.dex);
        attr.@int = EditorGUILayout.IntField("Trí Tuệ (INT)", attr.@int);
        attr.vit = EditorGUILayout.IntField("Thể Lực (VIT)", attr.vit);
        attr.agi = EditorGUILayout.IntField("Nhanh Nhẹn (AGI)", attr.agi);
        attr.luk = EditorGUILayout.IntField("May Mắn (LUK)", attr.luk);
        selectedItem.attributes = attr;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Elemental Damage & Resistances", EditorStyles.boldLabel);
        var elem = selectedItem.elemental;
        
        EditorGUILayout.LabelField("Sát thương Nguyên Tố", EditorStyles.miniBoldLabel);
        elem.fireDamage = EditorGUILayout.FloatField("Lửa (Fire Dmg)", elem.fireDamage);
        elem.iceDamage = EditorGUILayout.FloatField("Băng (Ice Dmg)", elem.iceDamage);
        elem.lightningDamage = EditorGUILayout.FloatField("Lôi (Lightning Dmg)", elem.lightningDamage);
        elem.darkDamage = EditorGUILayout.FloatField("Bóng Tối (Dark Dmg)", elem.darkDamage);
        elem.lightDamage = EditorGUILayout.FloatField("Ánh Sáng (Light Dmg)", elem.lightDamage);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Kháng Nguyên Tố", EditorStyles.miniBoldLabel);
        elem.fireResistance = EditorGUILayout.FloatField("Kháng Lửa", elem.fireResistance);
        elem.iceResistance = EditorGUILayout.FloatField("Kháng Băng", elem.iceResistance);
        elem.lightningResistance = EditorGUILayout.FloatField("Kháng Lôi", elem.lightningResistance);
        elem.darkResistance = EditorGUILayout.FloatField("Kháng Bóng Tối", elem.darkResistance);
        elem.lightResistance = EditorGUILayout.FloatField("Kháng Ánh Sáng", elem.lightResistance);
        
        selectedItem.elemental = elem;
    }

    private void DrawBottomBar()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"Tổng cộng: {allItems.Count} items  |  Hiển thị: {filteredItems.Count} items", EditorStyles.miniBoldLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("🔧 Đồng bộ Sorting Order theo Category", GUILayout.Width(250)))
        {
            SyncSortingOrders();
        }

        if (GUILayout.Button("🛠️ Hồi phục Độ Bền của Tất Cả", GUILayout.Width(200)))
        {
            RepairAll();
        }

        if (GUILayout.Button("💾 Lưu Thay Đổi", GUILayout.Width(120)))
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Lưu", "Đã lưu tất cả thay đổi cơ sở dữ liệu!", "OK");
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void CreateNewItem()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Tạo LPC Item Data Mới",
            "NewItem_LPCItemData",
            "asset",
            "Chọn nơi lưu trữ vật phẩm mới trong thư mục Assets"
        );
        if (string.IsNullOrEmpty(path)) return;

        var newItem = ScriptableObject.CreateInstance<LPCItemData>();
        newItem.itemName = Path.GetFileNameWithoutExtension(path);
        newItem.category = "Weapon";
        newItem.rarity = ItemRarity.Common;
        
        AssetDatabase.CreateAsset(newItem, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        selectedItem = newItem;
        LoadDatabase();
        Debug.Log($"[Database Manager] Đã tạo vật phẩm mới tại: {path}");
    }

    private void DeleteSelectedAsset()
    {
        if (selectedItem == null) return;

        string name = selectedItem.itemName;
        string path = AssetDatabase.GetAssetPath(selectedItem);

        if (!EditorUtility.DisplayDialog("Xác nhận xóa", $"Bạn có chắc chắn muốn xóa vĩnh viễn tệp vật phẩm '{name}'?\nHành động này không thể hoàn tác.", "Xóa", "Hủy"))
            return;

        selectedItem = null;
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        LoadDatabase();
        Debug.Log($"[Database Manager] Đã xóa thành công vật phẩm: {name}");
    }

    private void RepairAll()
    {
        if (!EditorUtility.DisplayDialog("Xác nhận", "Bạn có chắc chắn muốn sửa chữa đầy đủ độ bền cho TẤT CẢ vật phẩm trong cơ sở dữ liệu?", "Đồng ý", "Hủy"))
            return;

        int count = 0;
        foreach (var item in allItems)
        {
            item.FullRepair();
            EditorUtility.SetDirty(item);
            count++;
        }
        AssetDatabase.SaveAssets();
        LoadDatabase();
        Debug.Log($"[Database Manager] Đã sửa chữa đầy đủ độ bền cho {count} vật phẩm.");
        EditorUtility.DisplayDialog("Thành công", $"Đã hồi phục độ bền cho {count} vật phẩm!", "OK");
    }

    private void SyncSortingOrders()
    {
        if (!EditorUtility.DisplayDialog("Xác nhận", "Bạn có muốn tự động đồng bộ hóa Sorting Order của tất cả vật phẩm theo phân loại tiêu chuẩn của LPC?", "Đồng ý", "Hủy"))
            return;

        int count = 0;
        foreach (var item in allItems)
        {
            int order = item.category switch
            {
                "Shadow" => -10,
                "Behind" => -3,
                "CapeBehind" => -3,
                "HairBehind" => -1,
                "Body" => 0,
                "Ears" => 1,
                "Eyes" => 2,
                "Underwear" => 10,
                "Clothing" => 20,
                "Armor" => 21,
                "Gloves" => 23,
                "Belt" => 30,
                "Hair" => 41,
                "Helmet" => 43,
                "Shield" => 50,
                "Weapon" => 51,
                "FX" => 60,
                _ => item.sortingOrder
            };

            if (item.sortingOrder != order)
            {
                item.sortingOrder = order;
                EditorUtility.SetDirty(item);
                count++;
            }
        }

        if (count > 0)
        {
            AssetDatabase.SaveAssets();
            LoadDatabase();
            Debug.Log($"[Database Manager] Đã đồng bộ Sorting Order cho {count} vật phẩm.");
            EditorUtility.DisplayDialog("Thành công", $"Đã đồng bộ Sorting Order cho {count} vật phẩm theo tiêu chuẩn!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Thông tin", "Tất cả vật phẩm đều đã khớp với Sorting Order tiêu chuẩn.", "OK");
        }
    }
}
