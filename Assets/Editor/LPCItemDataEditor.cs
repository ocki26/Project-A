using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LPCItemData))]
[CanEditMultipleObjects]
public class LPCItemDataEditor : Editor
{
    private SerializedProperty itemCategoryProp;
    private SerializedProperty forceShowWeaponStatsProp;

    private void OnEnable()
    {
        itemCategoryProp = serializedObject.FindProperty("itemCategory");
        forceShowWeaponStatsProp = serializedObject.FindProperty("forceShowWeaponStats");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Kiểm tra xem đây có phải là Vũ khí không
        LPCItemCategory category = (LPCItemCategory)itemCategoryProp.enumValueIndex;
        bool isWeapon = (category == LPCItemCategory.Weapon);
        bool showWeaponFields = isWeapon || forceShowWeaponStatsProp.boolValue;

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        // Theo dõi xem ta có đang nằm trong group cụ thể nào không
        bool inWeaponGroup = false;
        bool inEnchantmentGroup = false;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Bỏ qua thuộc tính script mặc định để giao diện gọn gàng
            if (iterator.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(iterator);
                }
                continue;
            }

            // Ghi đè hiển thị forceShowWeaponStats: Ẩn đi nếu đây đã là vũ khí rồi (đỡ rối)
            if (iterator.name == "forceShowWeaponStats")
            {
                if (!isWeapon)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.PropertyField(iterator);
                    EditorGUILayout.Space(2);
                }
                continue;
            }

            // Phân loại các trường cụ thể cần ẩn/hiện
            bool isWeaponSpecific = iterator.name == "weaponType" ||
                                     iterator.name == "attackRange" ||
                                     iterator.name == "attackAngle" ||
                                     iterator.name == "isRanged" ||
                                     iterator.name == "knockbackForce" ||
                                     iterator.name == "knockbackDuration";

            bool isEnchantmentSpecific = iterator.name == "enchantmentEffect" ||
                                         iterator.name == "enchantmentValue" ||
                                         iterator.name == "enchantmentDuration";

            // Vẽ khối Enchantment
            if (isEnchantmentSpecific)
            {
                if (showWeaponFields)
                {
                    if (!inEnchantmentGroup)
                    {
                        inEnchantmentGroup = true;
                        EditorGUILayout.Space(5);
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField("⚡ HIỆU ỨNG PHÙ PHÉP (ENCHANTMENT)", EditorStyles.boldLabel);
                    }
                    EditorGUILayout.PropertyField(iterator);
                }
            }
            // Vẽ khối Weapon Specific
            else if (isWeaponSpecific)
            {
                // Đóng khối cũ nếu có
                if (inEnchantmentGroup)
                {
                    inEnchantmentGroup = false;
                    EditorGUILayout.EndVertical();
                }

                if (showWeaponFields)
                {
                    if (!inWeaponGroup)
                    {
                        inWeaponGroup = true;
                        EditorGUILayout.Space(5);
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField("⚔️ CHỈ SỐ VŨ KHÍ CẬN CHIẾN / TẦM XA", EditorStyles.boldLabel);
                    }
                    EditorGUILayout.PropertyField(iterator);
                }
            }
            else
            {
                // Đóng bất kỳ group box nào đang mở trước khi vẽ các trường tiếp theo (như weight, durability, v.v.)
                if (inEnchantmentGroup)
                {
                    inEnchantmentGroup = false;
                    EditorGUILayout.EndVertical();
                }
                if (inWeaponGroup)
                {
                    inWeaponGroup = false;
                    EditorGUILayout.EndVertical();
                }

                // Vẽ các trường thông thường
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        // Đảm bảo đóng hết box ở cuối vòng lặp
        if (inEnchantmentGroup || inWeaponGroup)
        {
            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
