using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// LPC Tile Palette Generator — Công cụ tự động tạo Tile Palette từ thư mục chứa các file Tile (.asset).
/// Giúp người chơi tạo nhanh bảng Palette để vẽ thay vì kéo thả thủ công từng ô gạch.
/// </summary>
public class LPCTilePaletteGenerator : EditorWindow
{
    private string sourceFolder = "Assets/Arsetmap/titleFolder";
    private string paletteName = "titleFolder_Palette";
    private int gridColumns = 16;
    private bool selectCreatedPalette = true;

    [MenuItem("Tools/LPC/Tile Palette Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<LPCTilePaletteGenerator>("Tile Palette Gen");
        window.minSize = new Vector2(420, 240);
        window.maxSize = new Vector2(600, 320);
        
        // Tự động lấy thư mục đang chọn trong Project view làm mặc định
        string selectedPath = GetSelectedFolderPath();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            window.sourceFolder = selectedPath;
            window.paletteName = Path.GetFileName(selectedPath) + "_Palette";
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.15f, 0.6f, 1f) }
        };
        EditorGUILayout.LabelField("🎨 LPC Tile Palette Generator", headerStyle);
        EditorGUILayout.LabelField("Tạo Tile Palette tự động từ thư mục chứa các file .asset (Tile)", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        sourceFolder = EditorGUILayout.TextField("Thư mục chứa Tiles", sourceFolder);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sử dụng thư mục đang chọn trong Project view", EditorStyles.miniButton))
        {
            string selectedPath = GetSelectedFolderPath();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                sourceFolder = selectedPath;
                paletteName = Path.GetFileName(selectedPath) + "_Palette";
            }
            else
            {
                EditorUtility.DisplayDialog("Thông báo", "Vui lòng click chọn một thư mục trong Project view trước!", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        paletteName = EditorGUILayout.TextField("Tên Palette tạo ra", paletteName);
        gridColumns = EditorGUILayout.IntSlider("Số cột hiển thị", gridColumns, 4, 32);
        selectCreatedPalette = EditorGUILayout.Toggle("Chọn Palette sau khi tạo", selectCreatedPalette);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.15f, 0.6f, 1f);
        if (GUILayout.Button("⚡ GENERATE TILE PALETTE", GUILayout.Height(40)))
        {
            GeneratePalette();
        }
        GUI.backgroundColor = Color.white;
    }

    private static string GetSelectedFolderPath()
    {
        if (Selection.activeObject == null) return null;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (AssetDatabase.IsValidFolder(path)) return path;
        
        if (!string.IsNullOrEmpty(path))
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) return dir.Replace('\\', '/');
        }
        return null;
    }

    private void GeneratePalette()
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            EditorUtility.DisplayDialog("Lỗi", $"Thư mục nguồn không hợp lệ:\n{sourceFolder}", "OK");
            return;
        }

        if (string.IsNullOrEmpty(paletteName))
        {
            EditorUtility.DisplayDialog("Lỗi", "Tên Palette không được để trống!", "OK");
            return;
        }

        // Quét tìm toàn bộ file Tile (.asset) trong thư mục nguồn
        string[] guids = AssetDatabase.FindAssets("t:Tile", new[] { sourceFolder });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", $"Không tìm thấy file .asset (Tile) nào trong thư mục:\n{sourceFolder}", "OK");
            return;
        }

        List<TileBase> tiles = new List<TileBase>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null)
            {
                tiles.Add(tile);
            }
        }

        // Sắp xếp các ô gạch theo thứ tự tên tự nhiên (ví dụ: Objects_0, Objects_1, Objects_2, Objects_10...)
        tiles.Sort((a, b) => CompareNatural(a.name, b.name));

        // Khởi tạo cấu trúc GameObject Palette chuẩn của Unity
        GameObject paletteGO = new GameObject(paletteName);
        var grid = paletteGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        GameObject layerGO = new GameObject("Layer1");
        layerGO.transform.SetParent(paletteGO.transform, false);
        var tilemap = layerGO.AddComponent<Tilemap>();
        var tilemapRenderer = layerGO.AddComponent<TilemapRenderer>();
        tilemapRenderer.sortingLayerName = "Default";
        tilemapRenderer.sortingOrder = 0;
        tilemapRenderer.mode = TilemapRenderer.Mode.Chunk;

        // Xếp các gạch vào lưới của Tilemap
        for (int i = 0; i < tiles.Count; i++)
        {
            int x = i % gridColumns;
            int y = -(i / gridColumns); // Vẽ đi xuống dưới
            tilemap.SetTile(new Vector3Int(x, y, 0), tiles[i]);
        }

        // Lưu GameObject thành file Prefab Asset
        string outputPath = $"{sourceFolder}/{paletteName}.prefab";
        
        if (File.Exists(outputPath))
        {
            if (!EditorUtility.DisplayDialog("Cảnh báo", $"File Palette '{paletteName}.prefab' đã tồn tại. Bạn có muốn ghi đè không?", "Có", "Không"))
            {
                DestroyImmediate(paletteGO);
                return;
            }
        }

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(paletteGO, outputPath);
        DestroyImmediate(paletteGO);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LPC Palette Gen] Đã tạo thành công Palette với {tiles.Count} ô gạch tại: {outputPath}");
        EditorUtility.DisplayDialog("Thành công", $"Đã tạo thành công Tile Palette '{paletteName}' chứa {tiles.Count} ô gạch tại:\n{outputPath}\n\nBạn có thể mở cửa sổ Tile Palette trong Unity, chọn bảng này và bắt đầu vẽ ngay!", "Tuyệt vời");

        if (selectCreatedPalette && prefabAsset != null)
        {
            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
        }
    }

    /// <summary>
    /// So sánh chuỗi tự nhiên để Objects_2 đứng trước Objects_10
    /// </summary>
    private static int CompareNatural(string x, string y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        string[] xParts = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
        string[] yParts = Regex.Split(y.Replace(" ", ""), "([0-9]+)");

        for (int i = 0; i < Mathf.Min(xParts.Length, yParts.Length); i++)
        {
            if (xParts[i] != yParts[i])
            {
                int xNum, yNum;
                if (int.TryParse(xParts[i], out xNum) && int.TryParse(yParts[i], out yNum))
                {
                    return xNum.CompareTo(yNum);
                }
                return string.Compare(xParts[i], yParts[i], System.StringComparison.OrdinalIgnoreCase);
            }
        }

        return xParts.Length.CompareTo(yParts.Length);
    }
}
