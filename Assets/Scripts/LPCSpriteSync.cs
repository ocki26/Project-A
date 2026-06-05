using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// LPCSpriteSync — syncs a SpriteResolver's category+label with the parent Animator.
///
/// OPTIMISED FOR BODY SPRITE SYNC:
///   This script directly reads the active sprite name of the "Body" layer (drived by Animator)
///   and instantly maps it to the corresponding Category and Label of the equipment's SpriteLibrary.
///   This ensures 100% real-time pixel sync, zero flicker, and zero frame mismatch.
/// </summary>
[RequireComponent(typeof(SpriteResolver))]
[RequireComponent(typeof(SpriteLibrary))]
public class LPCSpriteSync : MonoBehaviour
{
    // =========================================================================
    //  Configuration (set by LPCEquipmentManager)
    // =========================================================================

    public enum VisibilityMode
    {
        /// <summary>Always visible. Component only syncs category/label.</summary>
        AlwaysVisible,

        /// <summary>Front weapon/shield. Hidden when facing Up (WeaponBehind takes over).</summary>
        FrontPaired,

        /// <summary>Behind weapon/shield. Visible ONLY when facing Up.</summary>
        BehindPaired,
    }

    [Tooltip("Controls when this SpriteRenderer is visible relative to character direction.")]
    public VisibilityMode visibilityMode = VisibilityMode.AlwaysVisible;

    // =========================================================================
    //  Internal state
    // =========================================================================

    private SpriteResolver _resolver;
    private SpriteLibraryAsset _library;
    private SpriteRenderer _sr;
    private SpriteRenderer _bodySR;
    private LPCItemData _activeItem;

    private string _lastCategory;
    private string _lastLabel;
    private bool _isWeapon;
    private bool _isShield;

    private readonly List<string> _availableCategories = new List<string>();
    private readonly Dictionary<string, List<string>> _categoryLabelsCache = new Dictionary<string, List<string>>();

    // =========================================================================
    //  Lifecycle
    // =========================================================================

    private void Awake()
    {
        _resolver = GetComponent<SpriteResolver>();
        _sr = GetComponent<SpriteRenderer>();
        RefreshLibrary();
    }

    private void Start()
    {
        _isWeapon = name.StartsWith("Weapon", System.StringComparison.OrdinalIgnoreCase);
        _isShield = name.StartsWith("Shield", System.StringComparison.OrdinalIgnoreCase);

        // Find the Body layer (sibling of this equipment slot)
        if (transform.parent != null)
        {
            var bodyTrans = transform.parent.Find("Body");
            if (bodyTrans != null)
            {
                _bodySR = bodyTrans.GetComponent<SpriteRenderer>();
            }
        }
    }

    private void LateUpdate()
    {
        if (_bodySR == null || _resolver == null || _library == null) return;

        Sprite bodySprite = _bodySR.sprite;
        if (bodySprite == null)
        {
            if (_sr != null) _sr.enabled = false;
            return;
        }

        if (!TryParseSprite(bodySprite, out string animName, out int row, out int col))
        {
            return;
        }

        // Xác định Category và Label trong Sprite Library của trang bị
        string category;
        string[] dirs = { "Up", "Left", "Down", "Right" };

        if (_availableCategories.Count == 0) return;

        string directionalCat = $"{animName}_{dirs[Mathf.Clamp(row, 0, 3)]}";
        if (_availableCategories.Contains(directionalCat))
        {
            category = directionalCat;
        }
        else if (_availableCategories.Contains(animName))
        {
            category = animName;
        }
        else
        {
            // Không tìm thấy hoạt ảnh tương ứng -> Ẩn trang bị (Không fallback sang Walk)
            if (_sr != null) _sr.enabled = false;
            return;
        }

        string label = col.ToString();

        // Kiểm tra xem label này có tồn tại trong category của trang bị không (tránh frame trống ở cuối làm chớp tắt)
        if (!_categoryLabelsCache.TryGetValue(category, out var labels) || !labels.Contains(label))
        {
            // Nếu không có label này (ví dụ frame trống ở cuối), ẩn trang bị để tránh nhấp nháy!
            if (_sr != null) _sr.enabled = false;
            return;
        }

        // Điều khiển hiển thị trang bị (phù hợp với visibilityMode cho front/behind)
        if (_sr != null)
        {
            if (visibilityMode == VisibilityMode.AlwaysVisible)
            {
                // Cho phép ẩn hoàn toàn nếu cấu hình là Hidden ngay cả khi ở chế độ AlwaysVisible
                DirectionalVisibility dirVis = DirectionalVisibility.Default;
                if (_activeItem != null)
                {
                    if (row == 0) dirVis = _activeItem.visibilityUp;
                    else if (row == 1) dirVis = _activeItem.visibilityLeft;
                    else if (row == 2) dirVis = _activeItem.visibilityDown;
                    else if (row == 3) dirVis = _activeItem.visibilityRight;
                }

                if (dirVis == DirectionalVisibility.Hidden)
                {
                    _sr.enabled = false;
                }
                else
                {
                    _sr.enabled = true;
                }
            }
            else
            {
                // - Hướng Up: row == 0
                // - Hướng Left: row == 1
                // - Hướng Down: row == 2
                // - Hướng Right: row == 3
                bool isWeapon = _isWeapon;
                bool isShield = _isShield;

                DirectionalVisibility dirVis = DirectionalVisibility.Default;
                if (_activeItem != null)
                {
                    if (row == 0) dirVis = _activeItem.visibilityUp;
                    else if (row == 1) dirVis = _activeItem.visibilityLeft;
                    else if (row == 2) dirVis = _activeItem.visibilityDown;
                    else if (row == 3) dirVis = _activeItem.visibilityRight;
                }

                if (dirVis == DirectionalVisibility.Hidden)
                {
                    _sr.enabled = false;
                    return;
                }

                bool isBehind = false;
                if (dirVis == DirectionalVisibility.Front)
                {
                    isBehind = false;
                }
                else if (dirVis == DirectionalVisibility.Behind)
                {
                    isBehind = true;
                }
                else if (dirVis == DirectionalVisibility.Visible)
                {
                    isBehind = false; // Luôn hiện ở lớp trước
                }
                else // Default
                {
                    if (isWeapon)
                    {
                        isBehind = (row == 0 || row == 1 || row == 3); // Up, Left, hoặc Right (vũ khí ở xa camera/sau lưng khi đi ngang)
                    }
                    else if (isShield)
                    {
                        isBehind = (row == 0 || row == 3); // Up hoặc Right (khiên tay trái ở xa camera)
                    }
                    else
                    {
                        isBehind = (row == 0); // Mặc định fallback cho các cặp khác
                    }
                }

                if (visibilityMode == VisibilityMode.FrontPaired)
                {
                    _sr.enabled = !isBehind;
                    if (isBehind) return;
                }
                else if (visibilityMode == VisibilityMode.BehindPaired)
                {
                    _sr.enabled = isBehind;
                    if (!isBehind) return;
                }
            }
        }

        if (name.StartsWith("Weapon", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[LPCSpriteSync Debug] {name}: enabled={_sr.enabled}, sortingOrder={_sr.sortingOrder}, sortingLayer={_sr.sortingLayerName}, row={row}, col={col}, sprite={(_sr.sprite != null ? _sr.sprite.name : "null")}");
        }

        if (category == _lastCategory && label == _lastLabel) return;

        _resolver.SetCategoryAndLabel(category, label);
        _resolver.ResolveSpriteToSpriteRenderer(); // Ép cập nhật ngay lập tức để triệt tiêu độ trễ 1-frame
        _lastCategory = category;
        _lastLabel = label;
    }

    // =========================================================================
    //  Public API (called by LPCEquipmentManager)
    // =========================================================================

    public void Initialize(VisibilityMode mode, LPCItemData item = null)
    {
        visibilityMode = mode;
        _activeItem = item;
        RefreshLibrary();
        _lastCategory = null;
        _lastLabel = null;

        // Start in the correct enabled state
        if (_sr != null)
        {
            _sr.enabled = mode switch
            {
                VisibilityMode.BehindPaired => false, // hidden until facing Up
                _ => true,
            };
        }
    }

    public void RefreshLibrary()
    {
        var sl = GetComponent<SpriteLibrary>();
        _library = sl != null ? sl.spriteLibraryAsset : null;
        _lastCategory = null;
        _lastLabel = null;

        _availableCategories.Clear();
        _categoryLabelsCache.Clear();

        if (_library != null)
        {
            var cats = _library.GetCategoryNames();
            if (cats != null)
            {
                foreach (var cat in cats)
                {
                    _availableCategories.Add(cat);
                    var labels = _library.GetCategoryLabelNames(cat);
                    if (labels != null)
                    {
                        _categoryLabelsCache[cat] = new List<string>(labels);
                    }
                }
            }
        }
    }

    private bool TryParseSprite(Sprite sprite, out string animName, out int row, out int col)
    {
        animName = "";
        row = 0;
        col = 0;

        if (sprite == null) return false;
        string spriteName = sprite.name;
        if (string.IsNullOrEmpty(spriteName)) return false;

        // Case 0: Giant spritesheet format "sprite_8_0" or "sprite_row_col"
        if (spriteName.StartsWith("sprite_"))
        {
            string[] parts = spriteName.Split('_');
            if (parts.Length == 3 && int.TryParse(parts[1], out int giantRow) && int.TryParse(parts[2], out col))
            {
                if (TryMapLPCGiantSheet(giantRow, out animName, out row))
                {
                    return true;
                }
            }
        }

        // Case 1: Standard format "Walk_r2_c3"
        int ri = spriteName.LastIndexOf("_r");
        int ci = spriteName.LastIndexOf("_c");
        if (ri >= 0 && ci >= 0 && ci > ri)
        {
            animName = spriteName.Substring(0, ri);
            string rStr = spriteName.Substring(ri + 2, ci - ri - 2);
            string cStr = spriteName.Substring(ci + 2);
            if (int.TryParse(rStr, out row) && int.TryParse(cStr, out col))
            {
                return true;
            }
        }

        // Case 2: Legacy format "walk_18" or "spellcast_24"
        int underscoreIdx = spriteName.LastIndexOf('_');
        if (underscoreIdx >= 0)
        {
            string anim = spriteName.Substring(0, underscoreIdx);
            string idxStr = spriteName.Substring(underscoreIdx + 1);
            if (int.TryParse(idxStr, out int globalIdx))
            {
                animName = FormatAnimName(anim);

                // Use robust texture coordinate calculation if sprite has texture
                if (sprite.texture != null)
                {
                    int texW = sprite.texture.width;
                    int texH = sprite.texture.height;
                    
                    float cellH = 64f;
                    if (texH == 128) cellH = 32f;
                    else if (texH == 256) cellH = 64f;
                    else if (texH == 512) cellH = 128f;
                    else if (texH == 64) cellH = 64f;
                    else if (texH == 32) cellH = 32f;
                    
                    float cellW = cellH;

                    float rectCenterX = sprite.rect.x + sprite.rect.width * 0.5f;
                    float rectCenterY = sprite.rect.y + sprite.rect.height * 0.5f;

                    int expectedRows = 4;
                    string lowerAnimName = animName.ToLower();
                    if (lowerAnimName == "die" || lowerAnimName == "hurt" || lowerAnimName == "sit")
                    {
                        expectedRows = 1;
                    }

                    col = Mathf.FloorToInt(rectCenterX / cellW);
                    row = Mathf.FloorToInt(((float)texH - rectCenterY) / cellH);

                    int frames = GetFramesPerRow(animName);
                    col = Mathf.Clamp(col, 0, frames - 1);
                    row = Mathf.Clamp(row, 0, expectedRows - 1);
                    
                    return true;
                }
                else
                {
                    int framesPerRow = GetFramesPerRow(animName);
                    row = globalIdx / framesPerRow;
                    col = globalIdx % framesPerRow;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryMapLPCGiantSheet(int giantRow, out string animName, out int localRow)
    {
        animName = "";
        localRow = 0;

        if (giantRow >= 0 && giantRow <= 3)
        {
            animName = "Spellcast";
            localRow = giantRow;
            return true;
        }
        else if (giantRow >= 4 && giantRow <= 7)
        {
            animName = "Thrust";
            localRow = giantRow - 4;
            return true;
        }
        else if (giantRow >= 8 && giantRow <= 11)
        {
            animName = "Walk";
            localRow = giantRow - 8;
            return true;
        }
        else if (giantRow >= 12 && giantRow <= 15)
        {
            animName = "Slash";
            localRow = giantRow - 12;
            return true;
        }
        else if (giantRow >= 16 && giantRow <= 19)
        {
            animName = "Shoot";
            localRow = giantRow - 16;
            return true;
        }
        else if (giantRow == 20)
        {
            animName = "Hurt";
            localRow = 0;
            return true;
        }
        return false;
    }

    private string FormatAnimName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "";
        string lower = rawName.ToLower();
        if (lower == "1h_slash") return "1h_Slash";
        if (lower == "1h_backslash") return "1h_Backslash";
        if (lower == "1h_halfslash") return "1h_Halfslash";
        return char.ToUpper(lower[0]) + lower.Substring(1);
    }

    private int GetFramesPerRow(string anim)
    {
        return anim.ToLower() switch
        {
            "walk" => 9,
            "run" => 8,
            "idle" => 2,
            "slash" => 6,
            "thrust" => 8,
            "shoot" => 13,
            "spellcast" => 7,
            "1h_slash" => 13,
            "1h_backslash" => 13,
            "1h_halfslash" => 6,
            "combat" => 2,
            "hurt" => 6,
            "die" => 6,
            "jump" => 5,
            "sit" => 3,
            "climb" => 6,
            "emote" => 3,
            "watering" => 8,
            _ => 9
        };
    }

}