using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// LPC Batch Sprite Slicer - Công cụ cắt ảnh hàng loạt tại chỗ (In-place Grid Slicer)
/// Đường dẫn Menu: Tools -> LPC Batch Sprite Slicer
/// </summary>
public class LPCBatchSpriteSlicer : EditorWindow
{
    private DefaultAsset targetFolder;
    private int spriteWidth = 64;
    private int spriteHeight = 64;
    private int pixelsPerUnit = 64;
    private FilterMode filterMode = FilterMode.Point;
    
    public enum PivotPreset
    {
        BottomCenter, // Pivot dưới chân (Chuẩn game 2.5D Top-down)
        Center,        // Pivot ở giữa
        Custom        // Điểm Pivot tự chọn
    }
    private PivotPreset pivotPreset = PivotPreset.BottomCenter;
    private Vector2 customPivot = new Vector2(0.5f, 0.05f);

    [MenuItem("Tools/LPC Batch Sprite Slicer")]
    public static void ShowWindow()
    {
        var window = GetWindow<LPCBatchSpriteSlicer>("Batch Slicer");
        window.titleContent = new GUIContent("LPC Batch Slicer", EditorGUIUtility.IconContent("d_Sprite Asset Icon").image);
        window.minSize = new Vector2(420, 360);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.12f, 0.75f, 0.55f) }
        };
        EditorGUILayout.LabelField("✂️ LPC Batch Sprite Slicer", headerStyle);
        EditorGUILayout.LabelField("Cắt Grid Hàng Loạt Tại Chỗ — Không Tạo Thư Mục Mới", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);

        // 1. CHỌN THƯ MỤC
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        var subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.15f, 0.65f, 1f) } };
        EditorGUILayout.LabelField("📁 1. Chọn thư mục chứa ảnh cần cắt", subHeaderStyle);
        EditorGUILayout.Space(3);
        
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Thư Mục Target", targetFolder, typeof(DefaultAsset), false);
        
        if (targetFolder != null)
        {
            string path = AssetDatabase.GetAssetPath(targetFolder);
            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorGUILayout.HelpBox("Đối tượng được chọn không phải là một Thư mục hợp lệ trong Assets!", MessageType.Error);
            }
            else
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                EditorGUILayout.LabelField($"🔎 Phát hiện: {guids.Length} file ảnh (Texture) trong thư mục này.", EditorStyles.miniBoldLabel);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 2. CẤU HÌNH CẮT
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚙️ 2. Cấu hình thông số cắt ảnh (Grid)", subHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        spriteWidth = EditorGUILayout.IntField("Rộng ô (Width)", spriteWidth);
        spriteHeight = EditorGUILayout.IntField("Cao ô (Height)", spriteHeight);
        EditorGUILayout.EndHorizontal();

        pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit (PPU)", pixelsPerUnit);
        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Chế độ lọc (Filter Mode)", filterMode);
        pivotPreset = (PivotPreset)EditorGUILayout.EnumPopup("Vị trí Pivot mặc định", pivotPreset);
        if (pivotPreset == PivotPreset.Custom)
        {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot (X, Y)", customPivot);
        }
        
        EditorGUILayout.HelpBox(
            "Mẹo: Game Top-down 2D hãy chọn 'BottomCenter' để nhân vật & vật thể đè lớp (Y-Sorting) chuẩn xác nhất!", 
            MessageType.Info);
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        // 3. NÚT CHẠY
        string folderPath = targetFolder != null ? AssetDatabase.GetAssetPath(targetFolder) : "";
        bool isValid = targetFolder != null && AssetDatabase.IsValidFolder(folderPath);

        GUI.enabled = isValid;
        GUI.backgroundColor = new Color(0.12f, 0.75f, 0.35f);
        if (GUILayout.Button("✂️ Bắt Đầu Cắt Hàng Loạt (Batch Slice)", GUILayout.Height(50)))
        {
            BatchSliceSprites();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void BatchSliceSprites()
    {
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file ảnh (Texture2D) nào trong thư mục đã chọn!", "OK");
            return;
        }

        // Hiện hộp thoại xác nhận trước khi đè cấu hình cũ
        if (!EditorUtility.DisplayDialog("Xác nhận cắt hàng loạt", 
            $"Bạn có chắc chắn muốn cắt tự động {guids.Length} file ảnh trong thư mục này không?\n\nTất cả file ảnh sẽ được cắt lưới ({spriteWidth}x{spriteHeight}) và gán Pivot '{pivotPreset}' ngay tại chỗ.", 
            "Đồng Ý", "Hủy Bỏ"))
        {
            return;
        }

        int successCount = 0;
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Batch Sprite Slicer", 
                    $"Đang cắt: {Path.GetFileName(assetPath)} ({i + 1}/{guids.Length})...", 
                    (float)i / guids.Length);

                if (SliceTextureInPlace(assetPath))
                {
                    successCount++;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BatchSlicer] Lỗi hệ thống: {e}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Hoàn thành!", 
                $"Đã cắt lưới thành công {successCount}/{guids.Length} file ảnh tại chỗ!\n\nKhông có thư mục mới nào được tạo.", 
                "Tuyệt Vời!");
        }
    }

    private bool SliceTextureInPlace(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        // Đọc kích thước thật của Texture nguồn
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) return false;

        // Cấu hình import bắt buộc
        importer.isReadable = true;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        int tw = tex.width;
        int th = tex.height;

        int cols = Mathf.Max(1, tw / spriteWidth);
        int rows = Mathf.Max(1, th / spriteHeight);

        var rects = new List<SpriteMetaData>();
        string filename = Path.GetFileNameWithoutExtension(assetPath);

        // Thiết lập Pivot theo Preset
        float pX = 0.5f;
        float pY = 0.5f;
        int align = (int)SpriteAlignment.Center;

        if (pivotPreset == PivotPreset.BottomCenter)
        {
            pX = 0.5f;
            pY = 0f;
            align = (int)SpriteAlignment.BottomCenter;
        }
        else if (pivotPreset == PivotPreset.Custom)
        {
            pX = customPivot.x;
            pY = customPivot.y;
            align = (int)SpriteAlignment.Custom;
        }

        // Cắt ảnh theo lưới từ góc trên bên trái xuống dưới
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                rects.Add(new SpriteMetaData
                {
                    pivot = new Vector2(pX, pY),
                    alignment = align,
                    name = $"{filename}_r{r}_c{c}",
                    rect = new Rect(
                        c * spriteWidth,
                        (rows - r - 1) * spriteHeight,
                        spriteWidth,
                        spriteHeight)
                });
            }
        }

        importer.spritesheet = rects.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        
        return true;
    }
}
