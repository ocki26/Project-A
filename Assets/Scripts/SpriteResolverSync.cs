using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Gắn vào Player root.
/// Mỗi frame đọc clip + frame từ Body Animator
/// rồi set Category + Label cho tất cả SpriteResolver trên các child.
///
/// Category = tên clip body animator (ví dụ: "Idle_Down", "Walk_Right")
/// Label    = frame index ("0", "1", "2"...)
///
/// Fallback: nếu label không tồn tại → dùng label "0"
///           nếu category không tồn tại → bỏ qua (layer này không có anim này)
/// </summary>
[RequireComponent(typeof(Animator))]
public class SpriteResolverSync : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Phải khớp với frameRate khi tạo item trong LPCItemCreator")]
    public int frameRate = 8;

    [Tooltip("Bật để xem log debug (tắt khi release)")]
    public bool debugLog = false;

    [Tooltip("Bật để log ra danh sách categories trong library khi khởi động")]
    public bool logAvailableCategories = true;

    // ─── Runtime ─────────────────────────────────────────────────────
    private Animator _animator;

    private struct ResolverInfo
    {
        public SpriteResolver  resolver;
        public SpriteLibrary   library;
        public string          childName;
    }

    private readonly List<ResolverInfo> _resolvers = new();
    private string _lastCategory = "";
    private int    _lastFrame    = -1;

    // ═══════════════════════════════════════════════════════════════════
    void Awake()
    {
        _animator = GetComponent<Animator>();
        RefreshResolvers();
    }

    /// <summary>Quét lại SpriteResolver — gọi sau mỗi lần equip.</summary>
    public void RefreshResolvers()
    {
        _resolvers.Clear();

        foreach (Transform child in transform)
        {
            var resolver = child.GetComponent<SpriteResolver>();
            var library  = child.GetComponent<SpriteLibrary>();

            if (resolver == null || library == null) continue;

            _resolvers.Add(new ResolverInfo
            {
                resolver  = resolver,
                library   = library,
                childName = child.name
            });

            // Log categories có sẵn để debug
            if (logAvailableCategories && library.spriteLibraryAsset != null)
                LogCategories(child.name, library.spriteLibraryAsset);
        }

        Debug.Log($"[SpriteResolverSync] '{name}' — {_resolvers.Count} resolvers active.");
        _lastCategory = "";
        _lastFrame    = -1;
    }

    // ═══════════════════════════════════════════════════════════════════
    void LateUpdate()
    {
        if (_animator == null || _resolvers.Count == 0) return;

        var clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos.Length == 0) return;

        string clipName   = clipInfos[0].clip.name;
        var    stateInfo  = _animator.GetCurrentAnimatorStateInfo(0);
        float  normalized = stateInfo.normalizedTime % 1f;

        // Tính frame
        int totalFrames = Mathf.Max(1, Mathf.RoundToInt(clipInfos[0].clip.length * frameRate));
        int frame       = Mathf.Clamp(Mathf.FloorToInt(normalized * totalFrames), 0, totalFrames - 1);

        if (clipName == _lastCategory && frame == _lastFrame) return;
        _lastCategory = clipName;
        _lastFrame    = frame;

        string label = frame.ToString();

        if (debugLog)
            Debug.Log($"[SpriteResolverSync] Clip='{clipName}' Frame={frame}");

        foreach (var info in _resolvers)
        {
            if (info.resolver  == null) continue;
            if (info.library   == null) continue;

            // Skip sync if the child has LPCSpriteSync to avoid conflicts and flickering
            if (info.resolver.GetComponent<LPCSpriteSync>() != null) continue;

            var asset = info.library.spriteLibraryAsset;
            if (asset == null) continue;

            // ── Thử tìm sprite đúng category + label ─────────────────
            var sprite = asset.GetSprite(clipName, label);

            if (sprite != null)
            {
                // ✅ Tìm thấy đúng
                info.resolver.SetCategoryAndLabel(clipName, label);
            }
            else
            {
                // ── Fallback 1: cùng category, label "0" ─────────────
                var fallbackSprite = asset.GetSprite(clipName, "0");
                if (fallbackSprite != null)
                {
                    info.resolver.SetCategoryAndLabel(clipName, "0");
                    if (debugLog)
                        Debug.Log($"[SpriteResolverSync] '{info.childName}' " +
                                  $"fallback label '0' for '{clipName}'");
                }
                // ── Fallback 2: không có category này → giữ nguyên ───
                // (layer này không có animation tương ứng → không làm gì)
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// In ra danh sách categories trong SpriteLibraryAsset.
    /// Dùng để debug xem tên category có khớp với clip name không.
    /// </summary>
    private static void LogCategories(string childName, SpriteLibraryAsset asset)
    {
        // Dùng reflection để lấy category list vì API public bị giới hạn
        try
        {
            var categories = new List<string>();

            // Unity 2022+: GetCategoryNames()
            var method = asset.GetType().GetMethod("GetCategoryNames",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (method != null)
            {
                var result = method.Invoke(asset, null) as IEnumerable<string>;
                if (result != null) categories.AddRange(result);
            }

            if (categories.Count > 0)
                Debug.Log($"[SpriteResolverSync] '{childName}' library categories:\n  " +
                          string.Join("\n  ", categories));
            else
                Debug.Log($"[SpriteResolverSync] '{childName}' — không đọc được categories. " +
                          "Mở Sprite Library Editor để kiểm tra.");
        }
        catch
        {
            Debug.Log($"[SpriteResolverSync] '{childName}' — Mở Sprite Library Editor " +
                      "để xem danh sách categories.");
        }
    }

#if UNITY_EDITOR
    /// <summary>Nút debug trong Inspector để xem categories ngay.</summary>
    [ContextMenu("Log All Categories")]
    private void LogAllCategories()
    {
        RefreshResolvers();
    }
#endif
}
