using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Tạo SpriteLibraryAsset từ spritesheet LPC
/// Cấu trúc: Category = tên animation+direction, Label = frame index
/// </summary>
public static class LPCLibraryBuilder
{
    /// <summary>
    /// Gọi hàm này từ LPCItemCreator sau khi slice sprites xong
    /// </summary>
    public static SpriteLibraryAsset BuildLibraryAsset(
        string itemName,
        string outputFolder,
        Dictionary<string, List<Sprite>> animSprites)
    // key: "Walk_Down", "Walk_Up"... value: danh sách sprites theo frame
    {
        var asset = ScriptableObject.CreateInstance<SpriteLibraryAsset>();

        foreach (var anim in animSprites)
        {
            string category = anim.Key; // ví dụ: "Walk_Down"
            var sprites = anim.Value;

            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] == null) continue;
                // Label = "0", "1", "2"...
                asset.AddCategoryLabel(sprites[i], category, i.ToString());
            }
        }

        string path = $"{outputFolder}/{itemName}_Library.spriteLib";
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[LPCLibrary] Created: {path}");
        return AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);
    }

    /// <summary>
    /// Helper: slice texture và nhóm sprite theo animation
    /// </summary>
    public static Dictionary<string, List<Sprite>> SliceIntoAnimGroups(
        Texture2D texture,
        string texturePath,
        int spriteW, int spriteH,
        string animName,
        int frameCount,
        bool directionless)
    {
        // Đảm bảo texture đã được slice
        EnsureTextureSliced(texturePath, spriteW, spriteH);

        var spriteDict = new Dictionary<string, Sprite>();
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(texturePath))
        {
            if (obj is Sprite sp)
            {
                spriteDict[sp.name] = sp;
            }
        }

        int cols = texture.width / spriteW;
        int rows = texture.height / spriteH;

        var result = new Dictionary<string, List<Sprite>>();
        string[] dirs = { "Up", "Left", "Down", "Right" };

        if (directionless || rows == 1)
        {
            var frames = new List<Sprite>();
            for (int f = 0; f < frameCount; f++)
            {
                string spriteName = $"sprite_0_{f}";
                spriteDict.TryGetValue(spriteName, out Sprite sp);
                frames.Add(sp);
            }
            result[animName] = frames;
        }
        else
        {
            for (int row = 0; row < Mathf.Min(4, rows); row++)
            {
                string key = $"{animName}_{dirs[row]}";
                var frames = new List<Sprite>();
                for (int f = 0; f < frameCount; f++)
                {
                    string spriteName = $"sprite_{row}_{f}";
                    spriteDict.TryGetValue(spriteName, out Sprite sp);
                    frames.Add(sp);
                }
                result[key] = frames;
            }
        }

        return result;
    }

    private static void EnsureTextureSliced(string path, int w, int h)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.filterMode = FilterMode.Point;
        imp.spritePixelsPerUnit = 32;

        int tw, th;
        imp.GetSourceTextureWidthAndHeight(out tw, out th);
        int cols = tw / w, rows = th / h;

        var metas = new List<SpriteMetaData>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = $"sprite_{r}_{c}",
                    rect = new Rect(c * w, th - (r + 1) * h, w, h),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = 0
                });
            }

        imp.spritesheet = metas.ToArray();
        EditorUtility.SetDirty(imp);
        imp.SaveAndReimport();
    }
}