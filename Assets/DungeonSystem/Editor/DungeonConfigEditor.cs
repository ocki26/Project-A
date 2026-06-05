#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DungeonSystem.Generation;
using DungeonSystem.Core;

namespace DungeonSystem.Editor
{
    [CustomEditor(typeof(DungeonConfig))]
    public class DungeonConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DungeonConfig config = (DungeonConfig)target;

            DrawDefaultInspector();

            GUILayout.Space(20);
            GUILayout.Label("Công cụ Tiện ích (Validator)", EditorStyles.boldLabel);

            if (GUILayout.Button("Kiểm tra Trùng lặp & Hợp lệ Prefab", GUILayout.Height(30)))
            {
                ValidatePrefabs(config);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidatePrefabs(DungeonConfig config)
        {
            var allPrefabs = new System.Collections.Generic.HashSet<GameObject>();
            bool hasError = false;

            RoomType[] types = (RoomType[])System.Enum.GetValues(typeof(RoomType));
            foreach (var type in types)
            {
                var list = config.GetPrefabsByType(type);
                foreach (var prefab in list)
                {
                    if (prefab == null)
                    {
                        Debug.LogError($"[DungeonConfig] Có ô trống (Null element) trong danh sách loại {type}!");
                        hasError = true;
                        continue;
                    }

                    if (allPrefabs.Contains(prefab.gameObject))
                    {
                        Debug.LogWarning($"[DungeonConfig] Prefab '{prefab.gameObject.name}' bị khai báo trùng lặp tại nhiều danh sách!");
                    }
                    else
                    {
                        allPrefabs.Add(prefab.gameObject);
                    }

                    if (prefab.roomSize.x % config.cellSize != 0 || prefab.roomSize.y % config.cellSize != 0)
                    {
                        Debug.LogError($"[DungeonConfig] Prefab '{prefab.gameObject.name}' có kích thước {prefab.roomSize} không khớp với kích thước ô lưới {config.cellSize}!");
                        hasError = true;
                    }
                }
            }

            if (!hasError)
            {
                EditorUtility.DisplayDialog("Dungeon Validator", "Tất cả các prefab đều hợp lệ và cấu hình hoàn toàn tương thích!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Dungeon Validator", "Phát hiện lỗi cấu hình. Vui lòng xem thông tin chi tiết trong bảng Console.", "OK");
            }
        }
    }
}
#endif
