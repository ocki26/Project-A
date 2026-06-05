using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LPCEquipSync — Runtime Component
///
/// Gắn vào child GameObject của item (WeaponRight, HairFront, ...).
/// Tự động sync Animator state với parent Animator của player.
///
/// Cách dùng:
///   1. Player (Animator [Body Controller])
///      └── WeaponRight (Animator [Item Controller] + SpriteRenderer + LPCEquipSync)
///
///   2. Trong code equip:
///      var sync = weaponChild.GetComponent<LPCEquipSync>();
///      sync.Equip(itemData);   // khi trang bị
///      sync.Unequip();          // khi tháo
///
/// LPCEquipSync tự đọc state từ parent Animator mỗi frame và mirror sang child Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class LPCEquipSync : MonoBehaviour
{
    // ─── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Animator của player (parent). Auto-detected nếu để trống.")]
    public Animator playerAnimator;

    [Header("Item Data (runtime)")]
    [Tooltip("Item hiện tại đang được trang bị.")]
    public LPCItemData currentItem;

    [Header("Sync Settings")]
    [Tooltip("Sync float parameters (DirectionX, DirectionY, Speed)")]
    public bool syncFloats   = true;
    [Tooltip("Sync bool parameters (IsAttacking, IsCasting, IsDead)")]
    public bool syncBools    = true;
    [Tooltip("Sync int parameters (AttackType)")]
    public bool syncInts     = true;
    [Tooltip("Sync trigger parameters (IsHurt)")]
    public bool syncTriggers = true;

    [Header("Visibility")]
    [Tooltip("Ẩn SpriteRenderer khi chưa có item được trang bị")]
    public bool hideWhenEmpty = true;

    // ─── Private ───────────────────────────────────────────────────────────────

    private Animator          childAnimator;
    private SpriteRenderer    childRenderer;
    private bool              isEquipped = false;

    // Parameter cache
    private static readonly string[] FloatParams   = { "DirectionX", "DirectionY", "Speed" };
    private static readonly string[] BoolParams    = { "IsAttacking", "IsCasting", "IsDead" };
    private static readonly string[] IntParams     = { "AttackType" };
    private static readonly string[] TriggerParams = { "IsHurt" };

    // Track trigger states to avoid missing them
    private Dictionary<string, bool> prevTriggers = new Dictionary<string, bool>();

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        childAnimator = GetComponent<Animator>();
        childRenderer = GetComponent<SpriteRenderer>();

        // Auto-find player animator in parent hierarchy
        if (playerAnimator == null)
            playerAnimator = GetComponentInParent<Animator>(includeInactive: true);

        // Don't pick our own
        if (playerAnimator == this.childAnimator)
        {
            Transform p = transform.parent;
            while (p != null)
            {
                var a = p.GetComponent<Animator>();
                if (a != null && a != childAnimator)
                {
                    playerAnimator = a;
                    break;
                }
                p = p.parent;
            }
        }

        if (playerAnimator == null)
            Debug.LogWarning($"[LPCEquipSync] {name}: Could not find parent Animator.");

        foreach (string t in TriggerParams)
            prevTriggers[t] = false;

        if (hideWhenEmpty && !isEquipped)
            SetRendererVisible(false);
    }

    private void Update()
    {
        if (!isEquipped || playerAnimator == null || childAnimator == null) return;

        SyncParameters();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Trang bị item. Kích hoạt SpriteRenderer và bắt đầu sync.</summary>
    public void Equip(LPCItemData item)
    {
        currentItem = item;

        if (item != null && item.itemController != null)
            childAnimator.runtimeAnimatorController = item.itemController;

        isEquipped = true;
        SetRendererVisible(true);

        Debug.Log($"[LPCEquipSync] {name}: Equipped '{(item != null ? item.itemName : "null")}'");
    }

    /// <summary>Tháo item. Ẩn SpriteRenderer.</summary>
    public void Unequip()
    {
        isEquipped  = false;
        currentItem = null;

        if (hideWhenEmpty) SetRendererVisible(false);

        Debug.Log($"[LPCEquipSync] {name}: Unequipped.");
    }

    public bool IsEquipped => isEquipped;

    // ─── Sync ─────────────────────────────────────────────────────────────────

    private void SyncParameters()
    {
        if (syncFloats)
        {
            foreach (string p in FloatParams)
            {
                if (HasParam(playerAnimator, p, AnimatorControllerParameterType.Float) &&
                    HasParam(childAnimator,  p, AnimatorControllerParameterType.Float))
                    childAnimator.SetFloat(p, playerAnimator.GetFloat(p));
            }
        }

        if (syncBools)
        {
            foreach (string p in BoolParams)
            {
                if (HasParam(playerAnimator, p, AnimatorControllerParameterType.Bool) &&
                    HasParam(childAnimator,  p, AnimatorControllerParameterType.Bool))
                    childAnimator.SetBool(p, playerAnimator.GetBool(p));
            }
        }

        if (syncInts)
        {
            foreach (string p in IntParams)
            {
                if (HasParam(playerAnimator, p, AnimatorControllerParameterType.Int) &&
                    HasParam(childAnimator,  p, AnimatorControllerParameterType.Int))
                    childAnimator.SetInteger(p, playerAnimator.GetInteger(p));
            }
        }

        if (syncTriggers)
        {
            // Triggers: detect rising edge by polling AnimatorStateInfo
            // (Triggers reset after being consumed, so we use a state-match approach)
            int parentHash  = playerAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            int parentTrans = playerAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;

            foreach (string p in TriggerParams)
            {
                // Mirror trigger by comparing normalized time — if parent just transitioned, mirror trigger
                if (HasParam(playerAnimator, p, AnimatorControllerParameterType.Trigger) &&
                    HasParam(childAnimator,  p, AnimatorControllerParameterType.Trigger))
                {
                    // Simple approach: if parent is in a state named same as trigger's target,
                    // set the trigger on child too. 
                    // More robust: use AnimatorStateInfo to detect state change.
                    if (playerAnimator.IsInTransition(0))
                    {
                        var info = playerAnimator.GetNextAnimatorStateInfo(0);
                        // If entering a trigger-driven state, fire the same trigger
                        if (p == "IsHurt" && info.IsName("Hurt"))
                            childAnimator.SetTrigger(p);
                    }
                }
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetRendererVisible(bool visible)
    {
        if (childRenderer != null)
            childRenderer.enabled = visible;
    }

    private static bool HasParam(Animator animator, string name,
        AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    // ─── Context Menu (Editor helpers) ────────────────────────────────────────

    [ContextMenu("Force Sync Now")]
    private void ForceSyncNow()
    {
        if (Application.isPlaying) SyncParameters();
    }

    [ContextMenu("Test Equip (current item)")]
    private void TestEquip()
    {
        if (Application.isPlaying) Equip(currentItem);
    }

    [ContextMenu("Test Unequip")]
    private void TestUnequip()
    {
        if (Application.isPlaying) Unequip();
    }
}
