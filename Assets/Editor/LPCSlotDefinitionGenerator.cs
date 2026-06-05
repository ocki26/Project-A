using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Tạo tất cả 26 EquipmentSlotDefinition assets trong 1 cú click.
/// Menu: Tools → LPC → Create All Slot Definitions
/// </summary>
public static class LPCSlotDefinitionGenerator
{
    private struct SlotData
    {
        public string slotName;
        public string displayName;
        public string[] categories;
        public bool    alwaysOn;
        public int     sortingOrder;

        public SlotData(string n, string d, string[] c, bool ao, int o)
        { slotName = n; displayName = d; categories = c; alwaysOn = ao; sortingOrder = o; }
    }

    private static readonly SlotData[] AllSlots =
    {
        // ── Dưới cùng ──────────────────────────────────────────────
        new SlotData("Shadow",       "Bóng",         new[]{"FX"},                       true,  -10),
        new SlotData("WeaponBehind", "Vũ khí (sau)", new[]{"Weapon"},                   false,  -5),
        new SlotData("ShieldBehind", "Khiên (sau)",  new[]{"Weapon"},                   false,  -4),
        new SlotData("CapeBehind",   "Áo choàng",    new[]{"Behind"},                   false,  -3),
        new SlotData("Quiver",       "Ống tên",       new[]{"Behind"},                  false,  -2),
        new SlotData("HairBehind",   "Tóc sau",      new[]{"Hair"},                     true,   -1),

        // ── Cơ thể gốc ─────────────────────────────────────────────
        new SlotData("Body",         "Cơ thể",       new[]{"Clothing"},                 true,    0),
        new SlotData("Ears",         "Tai",           new[]{"Face"},                    true,    1),
        new SlotData("Eyes",         "Mắt",          new[]{"Face"},                     true,    2),

        // ── Thân dưới ──────────────────────────────────────────────
        new SlotData("Underwear",    "Đồ lót",       new[]{"Clothing"},                 true,   10),
        new SlotData("Legs",         "Quần",         new[]{"Clothing","Armor"},          false,  11),
        new SlotData("Feet",         "Giày",         new[]{"Armor"},                    false,  12),

        // ── Thân trên ──────────────────────────────────────────────
        new SlotData("Torso",        "Áo trong",     new[]{"Clothing"},                 false,  20),
        new SlotData("Armor",        "Giáp",         new[]{"Armor"},                    false,  21),
        new SlotData("Arms",         "Tay áo",       new[]{"Clothing","Armor"},          false,  22),
        new SlotData("Gloves",       "Găng tay",     new[]{"Armor"},                    false,  23),

        // ── Phụ kiện ───────────────────────────────────────────────
        new SlotData("Belt",         "Thắt lưng",    new[]{"Clothing","Armor"},          false,  30),
        new SlotData("Neck",         "Khăn cổ",      new[]{"Clothing"},                 false,  31),
        new SlotData("Shoulders",    "Giáp vai",     new[]{"Armor"},                    false,  32),

        // ── Đầu & khuôn mặt ────────────────────────────────────────
        new SlotData("FacialHair",   "Râu",          new[]{"Face"},                     true,   40),
        new SlotData("HairFront",    "Tóc mái",      new[]{"Hair"},                     true,   41),
        new SlotData("Mask",         "Mặt nạ",       new[]{"Face"},                     false,  42),
        new SlotData("Helmet",       "Mũ giáp",      new[]{"Armor","Helmet"},            false,  43),

        // ── Vũ khí & hiệu ứng ──────────────────────────────────────
        new SlotData("Shield",       "Khiên",        new[]{"Weapon"},                   false,  50),
        new SlotData("Weapon",       "Vũ khí",       new[]{"Weapon"},                   false,  51),
        new SlotData("Effects",      "Hiệu ứng",     new[]{"FX"},                       false,  60),
    };

    [MenuItem("Tools/LPC/Create All Slot Definitions")]
    public static void CreateAll()
    {
        string folder = EditorUtility.SaveFolderPanel(
            "Chọn thư mục lưu Slot Definitions",
            "Assets", "SlotDefinitions");

        if (string.IsNullOrEmpty(folder)) return;

        // Chuyển absolute path → relative Assets/ path
        if (!folder.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Lỗi",
                "Phải chọn thư mục bên trong Assets/", "OK");
            return;
        }

        string assetFolder = "Assets" + folder.Substring(Application.dataPath.Length);

        // Tạo folder nếu chưa có
        if (!AssetDatabase.IsValidFolder(assetFolder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        int created = 0, skipped = 0;

        foreach (var data in AllSlots)
        {
            string path = $"{assetFolder}/Slot_{data.slotName}.asset";

            // Bỏ qua nếu đã tồn tại
            if (AssetDatabase.LoadAssetAtPath<EquipmentSlotDefinition>(path) != null)
            {
                skipped++;
                continue;
            }

            var asset = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            asset.slotName            = data.slotName;
            asset.displayName         = data.displayName;
            asset.acceptedCategories  = data.categories;
            asset.alwaysOn            = data.alwaysOn;
            asset.defaultSortingOrder = data.sortingOrder;
            asset.sortingLayerName    = "Characters";

            AssetDatabase.CreateAsset(asset, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Ping folder để dễ thấy
        EditorGUIUtility.PingObject(
            AssetDatabase.LoadAssetAtPath<Object>(assetFolder));

        EditorUtility.DisplayDialog("✅ Hoàn thành!",
            $"Đã tạo {created} Slot Definition assets.\n" +
            (skipped > 0 ? $"Bỏ qua {skipped} (đã tồn tại).\n" : "") +
            $"\nThư mục: {assetFolder}\n\n" +
            "Bước tiếp theo:\n" +
            "1. Kéo các .asset này vào LPCEquipmentManager → Slot Definitions\n" +
            "2. Kéo các child GO tương ứng vào Slot Transforms",
            "OK");
    }

    /// <summary>
    /// Cập nhật tất cả slot đã tồn tại trong folder được chọn.
    /// Dùng khi muốn reset về giá trị mặc định.
    /// </summary>
    [MenuItem("Tools/LPC/Reset All Slot Definitions")]
    public static void ResetAll()
    {
        bool confirm = EditorUtility.DisplayDialog("Reset Slot Definitions",
            "Tìm và reset tất cả EquipmentSlotDefinition về giá trị mặc định?\n\nThao tác này KHÔNG thể hoàn tác.",
            "Reset", "Hủy");
        if (!confirm) return;

        string folder = EditorUtility.SaveFolderPanel(
            "Chọn thư mục chứa Slot Definitions", "Assets", "");
        if (string.IsNullOrEmpty(folder)) return;

        string assetFolder = "Assets" + folder.Substring(Application.dataPath.Length);
        int updated = 0;

        foreach (var data in AllSlots)
        {
            string path  = $"{assetFolder}/Slot_{data.slotName}.asset";
            var    asset = AssetDatabase.LoadAssetAtPath<EquipmentSlotDefinition>(path);
            if (asset == null) continue;

            asset.slotName            = data.slotName;
            asset.displayName         = data.displayName;
            asset.acceptedCategories  = data.categories;
            asset.alwaysOn            = data.alwaysOn;
            asset.defaultSortingOrder = data.sortingOrder;
            asset.sortingLayerName    = "Characters";

            EditorUtility.SetDirty(asset);
            updated++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("✅ Reset xong", $"Đã reset {updated} slot definitions.", "OK");
    }
}
