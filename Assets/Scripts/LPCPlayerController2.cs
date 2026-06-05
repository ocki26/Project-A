using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class LPCPlayerController2 : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed     = 0.8f;
    public float runMultiplier = 1.5f;

    [Header("Combat")]
    public WeaponType currentWeapon   = WeaponType.Unarmed_Slash;
    public bool canMoveWhileAttacking = false;
    [Tooltip("Tốc độ đánh của nhân vật (1.0 = bình thường, càng cao đánh càng nhanh)")]
    public float attackSpeed = 1.0f;
    [Tooltip("Layer chứa kẻ địch để nhận sát thương")]
    public LayerMask enemyLayer;
    [Tooltip("Prefab của chữ hiển thị sát thương (Floating Text)")]
    public GameObject damageTextPrefab;
    [Header("Shatter Visual Effects")]
    [Tooltip("Prefab hiệu ứng hình ảnh khi vỡ vũ khí. Nếu để trống, game tự tạo hiệu ứng hạt sắt vỡ bay tóe bằng code!")]
    public GameObject weaponShatterVfxPrefab;
    [Tooltip("Prefab hiệu ứng hình ảnh khi vỡ giáp. Nếu để trống, game tự tạo hiệu ứng mảnh giáp vỡ bay tóe bằng code!")]
    public GameObject armorShatterVfxPrefab;
    [Tooltip("Hiển thị vùng đánh debug trên Scene View")]
    public bool showAttackGizmo = true;
    [Header("Spell Weapon Effects")]
    [Tooltip("Prefab quả cầu phép bay ra khi đánh thường bằng Trượng phép")]
    public GameObject spellProjectilePrefab;


    [Header("Sorting Layer")]
    [Tooltip("Tên của Sorting Layer chung cho thực thể (ví dụ: Entities)")]
    public string sortingLayerName = "Entities";

    [Header("Keybinds")]
    public Key attackKey = Key.Z;
    public Key castKey   = Key.X;
    public Key runKey    = Key.LeftShift;

    // ==========================================
    // [THÊM MỚI] Hệ thống tầm nhìn (Line of Sight)
    // ==========================================
    [Header("References")]
    public LPCEquipmentManager equipmentManager;
    [Header("Vision & Lighting")]
    [Tooltip("Kéo object chứa Light 2D của tầm nhìn vào đây")]
    public Transform visionLightPivot;
    [Tooltip("Tốc độ xoay mượt của đèn/tầm nhìn")]
    public float visionRotationSpeed = 6f;

    [Header("RPG Core Attributes")]
    public int baseSTR = 10;
    public int baseDEX = 10;
    public int baseINT = 10;
    public int baseVIT = 10;
    public int baseAGI = 10;
    public int baseLUK = 10;

    [Header("Level & Experience")]
    public int level = 1;
    public float currentExp = 0f;
    public float requiredExp = 2000f;
    public int statPoints = 5;

    [Header("RPG Resources")]
    public float currentHP;
    public float maxHP = 100f;
    public float currentMP;
    public float maxMP = 50f;
    public float currentStamina;
    public float maxStamina = 50f;
    public float currentStaminaLau; // Stamina Lâu (Ngưỡng cạn kiệt tối đa)
    [Header("Thể Lực Chiến Đấu (Combat Stamina)")]
    [Tooltip("Thời gian duy trì (giây) debuff giảm 50% tốc độ hồi thể lực sau khi chém quái hoặc dính đòn.")]
    public float staminaDebuffDuration = 5.0f;
    private float lastCombatTime = -999f;

    public float currentShield = 0f;
    public float currentHunger;
    public float maxHunger = 100f;
    public float currentThirst;
    public float maxThirst = 100f;

    [Header("Calculated RPG Stats (Debug)")]
    public int finalSTR;
    public int finalDEX;
    public int finalINT;
    public int finalVIT;
    public int finalAGI;
    public int finalLUK;
    public float finalATK;
    public float finalMATK;
    public float finalDEF;
    public float finalMDEF;
    public float finalMoveSpeed;
    public float finalAttackSpeed;
    public float finalCritRate;
    public float finalCritDamage;
    public float finalArmorPenetration;
    public float finalMagicPenetration;
    public float finalSpellVamp;
    public float finalLifesteal;
    public float finalDodge;
    public float finalHPRegen;
    public float finalMPRegen;

    [Header("Debug")]
    public bool showDebugGUI = false;

    public enum WeaponType
    {
        Unarmed_Slash = 0, Thrust = 1, Bow_Shoot = 2, Spell = 3,
        OneHand_Slash = 10, OneHand_Back = 11, OneHand_Half = 12,
    }

    private Animator       anim;
    private SpriteRenderer sr;
    private Rigidbody2D    rb;

    private Vector2 moveInput;
    public Vector2 lastDir     = Vector2.down;
    private bool    isAttacking = false;
    private bool    isDead      = false;
    private bool    isExhausted = false;
    private float   customLockTimer = 0f;
    private bool    _isStaminaLauRecovering = false;
    private int     _lastStaminaConsumedFrame = -1;

    private float originalAnimSpeedAtAttackStart = 1f;
    private bool isHitStopping = false;
    private int attackCounter = 0; // Đếm số đòn đánh thường để kích hoạt Nhất Kích Tất Sát (DEX)



    public bool isDeadState => isDead;
    public bool isExhaustedState => isExhausted;

    private static readonly int H_DirX        = Animator.StringToHash("DirectionX");
    private static readonly int H_DirY        = Animator.StringToHash("DirectionY");
    private static readonly int H_Speed       = Animator.StringToHash("Speed");
    private static readonly int H_IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int H_AttackType  = Animator.StringToHash("AttackType");
    private static readonly int H_IsCasting   = Animator.StringToHash("IsCasting");
    private static readonly int H_IsHurt      = Animator.StringToHash("IsHurt");
    private static readonly int H_IsDead      = Animator.StringToHash("IsDead");

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();
        rb   = GetComponent<Rigidbody2D>();
        if (equipmentManager == null) equipmentManager = GetComponent<LPCEquipmentManager>();

        // Tự động gắn công cụ hỗ trợ Test/Showcase phím tắt F5 & F6 để trải nghiệm nhanh
        if (gameObject.GetComponent<LPC_DungeonFarmingShowcase>() == null)
        {
            gameObject.AddComponent<LPC_DungeonFarmingShowcase>();
        }

        ApplySortingLayer();
        ValidateAnimator();

        CalculateFinalStats();
        currentHP = maxHP;
        currentMP = maxMP;
        currentStaminaLau = maxStamina;
        currentStamina = currentStaminaLau;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
    }

    private void OnEnable()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnItemEquipped += UpdateStatsOnEquip;
            equipmentManager.OnItemUnequipped += UpdateStatsOnUnequip;
        }
    }

    private void OnDisable()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnItemEquipped -= UpdateStatsOnEquip;
            equipmentManager.OnItemUnequipped -= UpdateStatsOnUnequip;
        }
    }

    private void UpdateStatsOnEquip(string slot, LPCItemData item) => CalculateFinalStats();
    private void UpdateStatsOnUnequip(string slot) => CalculateFinalStats();

    public void CalculateFinalStats()
    {
        finalSTR = baseSTR;
        finalDEX = baseDEX;
        finalINT = baseINT;
        finalVIT = baseVIT;
        finalAGI = baseAGI;
        finalLUK = baseLUK;

        // Cộng thuộc tính từ trang bị
        if (equipmentManager != null)
        {
            foreach (var item in equipmentManager.GetAllEquipped().Values)
            {
                if (item == null) continue;
                finalSTR += item.attributes.str;
                finalDEX += item.attributes.dex;
                finalINT += item.attributes.@int;
                finalVIT += item.attributes.vit;
                finalAGI += item.attributes.agi;
                finalLUK += item.attributes.luk;
            }
        }

        // Tính chỉ số tối đa dựa trên thuộc tính gốc
        maxHP = 100f + finalVIT * 15f + finalSTR * 5f;
        maxMP = 50f + finalINT * 8f;
        maxStamina = 50f + finalAGI * 5f;

        finalATK = (finalSTR * 2f);
        finalMATK = (finalINT * 2f);
        finalDEF = (finalVIT * 0.5f);
        finalMDEF = (finalINT * 0.25f);
        finalMoveSpeed = moveSpeed + (finalAGI * 0.05f);
        finalAttackSpeed = attackSpeed + (finalDEX * 0.005f);
        finalCritRate = (finalDEX * 0.001f) + (finalLUK * 0.0015f);
        finalCritDamage = 1.5f + (finalLUK * 0.005f);
        finalArmorPenetration = (finalDEX * 1.0f);
        finalMagicPenetration = (finalINT * 0.5f);
        finalSpellVamp = (finalINT * 0.002f);
        finalLifesteal = 0f;
        finalDodge = (finalAGI * 0.001f);
        finalHPRegen = (finalVIT * 0.2f);
        finalMPRegen = (finalINT * 0.01f); // Hồi cực kỳ chậm như yêu cầu (0.01f thay vì 0.05f)

        // Cộng chỉ số bổ sung từ trang bị
        if (equipmentManager != null)
        {
            foreach (var item in equipmentManager.GetAllEquipped().Values)
            {
                if (item == null) continue;
                maxHP += item.core.maxHP;
                maxMP += item.core.maxMP;
                maxStamina += item.core.maxStamina;
                finalHPRegen += item.core.hpRegen;
                finalMPRegen += item.core.mpRegen;

                finalATK += item.offensive.atk * item.DamageMultiplier;
                finalMATK += item.offensive.matk * item.DamageMultiplier;
                finalCritRate += item.offensive.critRate;
                finalCritDamage += item.offensive.critDamage - 1.5f;
                finalAttackSpeed += item.offensive.attackSpeed - 1.0f;
                finalArmorPenetration += item.offensive.armorPenetration;
                finalMagicPenetration += item.offensive.magicPenetration;
                finalLifesteal += item.offensive.lifesteal;
                finalSpellVamp += item.offensive.spellVamp;

                finalDEF += item.defensive.def * item.DamageMultiplier;
                finalMDEF += item.defensive.mdef * item.DamageMultiplier;
                finalDodge += item.defensive.dodge;

                finalMoveSpeed += item.utility.moveSpeed;
            }

            // Áp dụng phạt nặng của túi đồ (Weight Encumbrance)
            finalMoveSpeed *= equipmentManager.GetWeightSpeedMultiplier();
        }

        // Áp dụng debuff làm chậm (Freeze) nếu có
        var buffMgr = GetComponent<LPC_BuffManager>();
        if (buffMgr != null)
        {
            if (buffMgr.HasBuff(LPC_BuffManager.BuffType.Freeze))
            {
                finalMoveSpeed *= 0.5f;
                finalAttackSpeed *= 0.7f;
            }
            if (buffMgr.HasBuff(LPC_BuffManager.BuffType.Regeneration))
            {
                finalHPRegen += 20f; // Hồi 20 HP/s khi hưng phấn
            }
            if (buffMgr.HasBuff(LPC_BuffManager.BuffType.WindWalk))
            {
                finalMoveSpeed *= 1.30f; // Bộ pháp gió: Tăng 30% tốc chạy
            }
        }

        // Áp dụng trạng thái cạn kiệt (Exhausted) nếu có
        if (isExhausted)
        {
            finalMoveSpeed *= 0.5f; // Chạy chậm 50%
        }

        // ─── THIÊN PHÚ HỆ THỂ CHẤT (VIT) - Kháng cự ───
        if (finalVIT >= 15)
        {
            float targetShield = (maxHP - currentHP) * 0.25f;
            if (currentShield < targetShield)
            {
                currentShield = targetShield;
            }
        }

        // ─── THIÊN PHÚ HỆ SỨC MẠNH (STR) - Cuồng nộ ───
        if (finalSTR >= 15 && currentHP <= maxHP * 0.3f)
        {
            finalATK *= 1.4f;      // Tăng 40% ATK
            finalLifesteal += 0.08f; // Nhận 8% hút máu
        }

        // Đồng bộ hóa trần stamina
        currentStaminaLau = Mathf.Clamp(currentStaminaLau, 0f, maxStamina);
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        currentMP = Mathf.Clamp(currentMP, 0f, maxMP);
        currentStamina = Mathf.Clamp(currentStamina, 0f, currentStaminaLau);
    }

    public void AddExperience(float amount)
    {
        currentExp += amount;
        while (currentExp >= requiredExp)
        {
            currentExp -= requiredExp;
            level++;
            statPoints += 2;
            requiredExp = Mathf.Round(requiredExp * 1.2f);
        }
        CalculateFinalStats();
    }

    public void TakeDamage(float amount, bool ignoreArmor = false)
    {
        if (isDead) return;
        RegisterCombatAction();

        // ─── THIÊN PHÚ HỆ NHANH NHẸN (AGI) - Bộ pháp gió (Né tránh) ───
        if (Random.value <= finalDodge)
        {
            // Né tránh thành công!
            SpawnDamageText(transform.position + Vector3.up * 1.2f, 0, false);
            
            if (finalAGI >= 15)
            {
                var buffMgr = GetComponent<LPC_BuffManager>();
                if (buffMgr != null)
                {
                    buffMgr.AddBuff(LPC_BuffManager.BuffType.WindWalk, 3f, 0f); // Tăng 30% tốc chạy trong 3 giây
                }
            }
            return;
        }

        float damage = amount;
        if (!ignoreArmor)
        {
            damage = Mathf.Max(1f, amount - finalDEF);
        }

        // Hấp thụ sát thương bằng Shield trước
        if (currentShield > 0f)
        {
            if (currentShield >= damage)
            {
                currentShield -= damage;
                damage = 0f;
            }
            else
            {
                damage -= currentShield;
                currentShield = 0f;
            }

            var shieldBuff = GetComponent<LPC_BuffManager>()?.GetBuff(LPC_BuffManager.BuffType.Shield);
            if (shieldBuff != null) shieldBuff.value = currentShield;
        }

        if (damage > 0f)
        {
            // ─── [HỆ THỐNG XỊN V2.1] KIỂM TRA PERFECT PARRY (ĐỠ ĐÒN VŨ KHÍ) ───
            if (equipmentManager != null)
            {
                LPCItemData weaponItem = equipmentManager.GetEquipped("Weapon");
                if (weaponItem != null && weaponItem.DurabilityRatio >= 0.8f && Random.value <= 0.10f)
                {
                    // Đỡ đòn hoàn mỹ bằng vũ khí thành công!
                    damage = 0f; // Triệt tiêu toàn bộ sát thương nhận vào

                    // Tạo FloatingText hiển thị chữ "PARRY!" màu vàng óng cực đẹp
                    GameObject parryText = new GameObject("Runtime_ParryText");
                    parryText.transform.position = transform.position + Vector3.up * 1.2f;
                    TMPro.TextMeshPro tmp = parryText.AddComponent<TMPro.TextMeshPro>();
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                    tmp.fontSize = 4.5f;
                    LPC_FloatingText ft = parryText.AddComponent<LPC_FloatingText>();
                    ft.Setup("PARRY!", new Color(1f, 0.75f, 0f), 1.3f);

                    // Phản công: Tìm kẻ địch gần nhất trong bán kính 2m và choáng/đẩy lùi nó nhẹ
                    Collider2D[] localEnemies = Physics2D.OverlapCircleAll(transform.position, 2f, enemyLayer);
                    EnemyAI closestEnemy = null;
                    float closestDist = float.MaxValue;
                    foreach (var col in localEnemies)
                    {
                        if (col.gameObject == gameObject) continue;
                        EnemyAI enemy = col.GetComponent<EnemyAI>();
                        if (enemy != null)
                        {
                            float dist = Vector2.Distance(transform.position, enemy.transform.position);
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                closestEnemy = enemy;
                            }
                        }
                    }
                    if (closestEnemy != null)
                    {
                        Vector2 kbDir = ((Vector2)closestEnemy.transform.position - (Vector2)transform.position).normalized;
                        if (kbDir == Vector2.zero) kbDir = lastDir;
                        closestEnemy.ApplyKnockback(kbDir, 4.0f, 0.2f);
                        Debug.Log($"[Parry] Đẩy lùi quái vật {closestEnemy.name} khi PARRY thành công!");
                    }

                    Debug.LogWarning($"[Durability] Perfect Parry with {weaponItem.itemName}! Miễn nhiễm sát thương và phản công!");
                    
                    CalculateFinalStats();
                    TakeHit();
                    return;
                }
            }

            // ─── [HỆ THỐNG XỊN V2] Khấu Trừ Độ Bền Giáp Theo Vùng Trúng Đòn Trọng Số ───
            if (equipmentManager != null)
            {
                List<string> equippedArmorSlots = new List<string>();
                List<int> weights = new List<int>();
                int totalWeight = 0;

                string[] targetSlots = { "Armor", "Helmet", "Legs", "Feet" };
                int[] slotWeights = { 50, 20, 20, 10 }; // Armor = 50%, Helmet = 20%, Legs = 20%, Feet = 10%

                for (int i = 0; i < targetSlots.Length; i++)
                {
                    string slot = targetSlots[i];
                    if (equipmentManager.GetEquipped(slot) != null)
                    {
                        equippedArmorSlots.Add(slot);
                        weights.Add(slotWeights[i]);
                        totalWeight += slotWeights[i];
                    }
                }

                if (equippedArmorSlots.Count > 0)
                {
                    // Chọn slot ngẫu nhiên theo trọng số
                    int randVal = Random.Range(0, totalWeight);
                    int sum = 0;
                    string chosenSlot = equippedArmorSlots[0];
                    for (int i = 0; i < equippedArmorSlots.Count; i++)
                    {
                        sum += weights[i];
                        if (randVal < sum)
                        {
                            chosenSlot = equippedArmorSlots[i];
                            break;
                        }
                    }

                    LPCItemData armorItem = equipmentManager.GetEquipped(chosenSlot);
                    if (armorItem != null)
                    {
                        // 1. Kiểm tra Perfect Deflect / Parry (Đỡ đòn hoàn mỹ)
                        // Chỉ kích hoạt khi độ bền giáp >= 80% (tức ratio >= 0.8) và tỉ lệ 15%
                        if (armorItem.DurabilityRatio >= 0.8f && Random.value <= 0.15f)
                        {
                            // Đỡ đòn hoàn mỹ thành công!
                            damage *= 0.5f; // Giảm 50% sát thương nhận vào
                            
                            // Tạo FloatingText hiển thị chữ "DEFLECT!" màu xanh ngọc (Cyan) cực đẹp
                            GameObject deflectText = new GameObject("Runtime_DeflectText");
                            deflectText.transform.position = transform.position + Vector3.up * 1.2f;
                            TMPro.TextMeshPro tmp = deflectText.AddComponent<TMPro.TextMeshPro>();
                            tmp.alignment = TMPro.TextAlignmentOptions.Center;
                            tmp.fontSize = 4.5f;
                            LPC_FloatingText ft = deflectText.AddComponent<LPC_FloatingText>();
                            ft.Setup("DEFLECT!", new Color(0f, 0.95f, 0.95f), 1.2f);

                            Debug.Log($"[Durability] Perfect Deflect on {chosenSlot}! Sát thương giảm 50%! Không hao mòn độ bền.");
                        }
                        else
                        {
                            // 2. Logic khấu trừ độ bền thông thường nhân với durabilityLossMultiplier
                            bool wasBrokenBefore = armorItem.IsBroken;
                            float baseLoss = Mathf.Clamp(damage * 0.1f, 1f, 5f);
                            float durLoss = baseLoss * armorItem.durabilityLossMultiplier;
                            
                            armorItem.TakeDurabilityDamage(durLoss);
                            Debug.Log($"[Durability] Hit on {chosenSlot}! {armorItem.itemName} durability decreased by {durLoss:F1}! Current: {armorItem.currentDurability:0}/{armorItem.maxDurability:0}");

                            // 3. Cơ chế Vỡ Giáp Cứu Nguy (Armor Shatter)
                            if (armorItem.IsBroken && !wasBrokenBefore)
                            {
                                // Sinh hiệu ứng hình ảnh (VFX) vỡ giáp
                                if (armorShatterVfxPrefab != null)
                                {
                                    Instantiate(armorShatterVfxPrefab, transform.position, Quaternion.identity);
                                }
                                else
                                {
                                    // Tự sinh 30 mảnh giáp vỡ (màu nâu đồng cổ) bay tóe ra cực đẹp bằng code!
                                    SpawnRuntimeShatterParticles(transform.position, new Color(0.6f, 0.45f, 0.35f, 0.9f), 30);
                                }

                                // Chữ nổi SHATTERED! màu đỏ
                                GameObject shatterText = new GameObject("Runtime_ShatterText");
                                shatterText.transform.position = transform.position + Vector3.up * 1.5f;
                                TMPro.TextMeshPro sTmp = shatterText.AddComponent<TMPro.TextMeshPro>();
                                sTmp.alignment = TMPro.TextAlignmentOptions.Center;
                                sTmp.fontSize = 5f;
                                LPC_FloatingText sFt = shatterText.AddComponent<LPC_FloatingText>();
                                sFt.Setup($"SHATTERED: {armorItem.itemName}!", new Color(1f, 0.1f, 0.1f), 1.4f);

                                // Sóng xung kích đẩy lùi (Knockback) quái vật xung quanh trong bán kính 3m
                                Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 3f, enemyLayer);
                                int knockedCount = 0;
                                foreach (var col in hitEnemies)
                                {
                                    if (col.gameObject == gameObject) continue;
                                    EnemyAI enemy = col.GetComponent<EnemyAI>();
                                    if (enemy != null)
                                    {
                                        Vector2 kbDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                                        if (kbDir == Vector2.zero) kbDir = lastDir;
                                        enemy.ApplyKnockback(kbDir, 6.0f, 0.3f);
                                        knockedCount++;
                                    }
                                }
                                Debug.LogWarning($"[Durability] {armorItem.itemName} SHATTERED! Phát sóng xung kích đẩy lùi {knockedCount} kẻ địch xung quanh!");

                                // Kích hoạt buff Lightweight Adrenaline (tăng 30% tốc chạy trong 3 giây thông qua buff WindWalk có sẵn)
                                var buffMgr = GetComponent<LPC_BuffManager>();
                                if (buffMgr != null)
                                {
                                    buffMgr.AddBuff(LPC_BuffManager.BuffType.WindWalk, 3.0f, 0f);
                                    Debug.Log("[Durability] Rũ bỏ giáp nặng! Kích hoạt buff Lightweight Adrenaline (+30% tốc chạy trong 3s).");
                                }
                            }
                        }
                    }
                }
            }

            // Trừ HP thực tế
            currentHP = Mathf.Clamp(currentHP - damage, 0f, maxHP);

            CalculateFinalStats(); // Cập nhật ngay lập tức chỉ số phòng thủ mới nếu giáp bị hỏng
            TakeHit();
        }

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public void ConsumeStamina(float amount)
    {
        if (amount <= 0f) return;
        currentStamina = Mathf.Clamp(currentStamina - amount, 0f, currentStaminaLau);
        _lastStaminaConsumedFrame = Time.frameCount;
    }

    private void RegenerateStats()
    {
        if (isDead) return;

        if (currentHP < maxHP)
            currentHP = Mathf.Clamp(currentHP + finalHPRegen * Time.deltaTime, 0f, maxHP);

        if (currentMP < maxMP)
            currentMP = Mathf.Clamp(currentMP + finalMPRegen * Time.deltaTime, 0f, maxMP);

        // Kiểm tra trạng thái cạn kiệt (Exhausted) khi Stamina nhanh cạn kiệt về 0
        if (currentStamina <= 0.01f)
        {
            if (!isExhausted)
            {
                isExhausted = true;
                CalculateFinalStats(); // Kích hoạt giảm tốc chạy 50%
            }
        }
        else if (isExhausted && currentStamina >= (currentStaminaLau * 0.3f))
        {
            isExhausted = false;
            CalculateFinalStats(); // Hủy giảm tốc chạy khi hồi lại >= 30% trần Stamina lâu
        }

        // Hồi Stamina Nhanh tiêu hao Stamina Lâu (Tỷ lệ 1 lâu = 20 nhanh)
        // Chỉ chuyển đổi khi KHÔNG tiêu hao stamina (không chạy nhanh, không chém/cast skill ở frame hiện tại)
        // VÀ KHÔNG đang trong trạng thái chiến đấu (chờ hồi phục thể lực sau combat)
        if (currentStamina < currentStaminaLau)
        {
            bool isConsumingThisFrame = (Time.frameCount == _lastStaminaConsumedFrame);
            bool isRunningNow = moveInput.sqrMagnitude > 0.01f 
                && Keyboard.current != null 
                && Keyboard.current[runKey].isPressed 
                && !isExhausted;

            if (!isConsumingThisFrame && !isRunningNow && !isAttacking)
            {
                float baseStaminaRegen = 10f; // Tốc độ hồi mặc định bình thường (10 Stamina/s)

                // Debuff giảm 50% tốc độ hồi khi chém quái hoặc bị quái đánh (trong vòng staminaDebuffDuration giây)
                if (Time.time - lastCombatTime < staminaDebuffDuration)
                {
                    baseStaminaRegen *= 0.5f;
                }

                // Giảm 50% tốc độ hồi phục Stamina nhanh khi bị cạn kiệt (Exhausted)
                if (isExhausted)
                {
                    baseStaminaRegen *= 0.5f;
                }

                float staminaToRegen = baseStaminaRegen * Time.deltaTime;
                
                // Đảm bảo không vượt quá trần Stamina Lâu
                float actualRegen = Mathf.Min(staminaToRegen, currentStaminaLau - currentStamina);
                currentStamina += actualRegen;

                // Mỗi 20 Stamina Nhanh hồi sẽ tiêu hao 1 Stamina Lâu (tỷ lệ 1:20)
                // Nhân với 0.05f thay vì chia cho 20f để tối ưu hiệu năng tính toán số thực
                currentStaminaLau = Mathf.Clamp(currentStaminaLau - (actualRegen * 0.05f), 0f, maxStamina);
            }
        }

        // Cập nhật trần Stamina Nhanh theo Stamina Lâu
        currentStamina = Mathf.Clamp(currentStamina, 0f, currentStaminaLau);

        // ─── HỆ THỐNG SINH TỒN (ĐÓI & NƯỚC) ───
        // Đói và Nước tự tiêu hao cực kỳ chậm theo thời gian
        currentHunger = Mathf.Max(0f, currentHunger - 0.1f * Time.deltaTime);
        currentThirst = Mathf.Max(0f, currentThirst - 0.15f * Time.deltaTime);

        // Hồi phục Stamina Lâu (Ngưỡng thể lực tối đa) bằng cách tiêu thụ Đói & Nước (Tỷ lệ 2 đói, 2 nước đổi 1 lâu)
        // Chỉ hồi khi Stamina Lâu dưới 50% maxStamina, và chỉ hồi phục tối đa lên đến 70% maxStamina
        if (currentStaminaLau < maxStamina * 0.5f)
        {
            _isStaminaLauRecovering = true;
        }
        else if (currentStaminaLau >= maxStamina * 0.7f)
        {
            _isStaminaLauRecovering = false;
        }

        if (_isStaminaLauRecovering)
        {
            float targetStaminaLau = maxStamina * 0.7f;
            if (currentStaminaLau < targetStaminaLau && currentHunger > 0f && currentThirst > 0f)
            {
                float desiredRecovery = 0.5f * Time.deltaTime; // Tốc độ hồi phục Stamina Lâu: 0.5/s
                desiredRecovery = Mathf.Min(desiredRecovery, targetStaminaLau - currentStaminaLau);

                float maxByHunger = currentHunger / 2f;
                float maxByThirst = currentThirst / 2f;
                float actualRecovery = Mathf.Min(desiredRecovery, Mathf.Min(maxByHunger, maxByThirst));

                if (actualRecovery > 0f)
                {
                    currentStaminaLau += actualRecovery;
                    currentHunger -= actualRecovery * 2f;
                    currentThirst -= actualRecovery * 2f;
                }
            }
            else
            {
                _isStaminaLauRecovering = false;
            }
        }
    }

    private void CheckBloodSacrifice()
    {
        if (isDead) return;

        // Khi Mana bằng 0 hoặc nhỏ hơn, tự động rút máu hồi lại 20% MP với tỷ lệ 1:2 (2 HP đổi lấy 1 MP)
        if (currentMP <= 0f)
        {
            float mpToRecover = maxMP * 0.20f;
            float hpCost = mpToRecover * 2f; // Tỷ lệ 1:2 (2 HP mất đi đổi lấy 1 MP hồi lại)

            currentHP = Mathf.Clamp(currentHP - hpCost, 0f, maxHP);
            currentMP = mpToRecover;

            Debug.LogWarning($"[Blood Magic] MP reached 0! Sacrificed {hpCost} HP to instantly recover {mpToRecover} MP.");

            // Kích hoạt hiệu ứng sát thương vật lý và áp dụng Bleed debuff tạm thời để biểu thị cống hiến máu
            TakeHit();
            var buffMgr = GetComponent<LPC_BuffManager>();
            if (buffMgr != null)
            {
                buffMgr.AddBuff(LPC_BuffManager.BuffType.Bleed, 3f, 6f); // Xuất huyết trong 3s
            }

            if (currentHP <= 0f)
            {
                Die();
            }
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (customLockTimer > 0f)
        {
            customLockTimer = Mathf.Max(0f, customLockTimer - Time.deltaTime);
        }

        ReadInput();
        UpdateAnimatorParams();
        UpdateVision();
        RegenerateStats();
        CheckBloodSacrifice();

        // Debug Key for gaining experience
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            AddExperience(500f);
            Debug.Log($"[LPC Debug] Gained 500 EXP. Current: {currentExp}/{requiredExp}, Level: {level}, Stat Points: {statPoints}");
        }

        // [TEST] Nhấn T để test thử chữ số sát thương bay ngay trên đầu nhân vật
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            int dmg = Random.Range(10, 100);
            bool testCrit = Random.value > 0.5f;
            Vector3 spawnPos = transform.position + Vector3.up * 1.2f + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.1f, 0.1f), 0);
            SpawnDamageText(spawnPos, dmg, testCrit);
            Debug.Log($"[LPC Debug] Test Damage Text spawned: {dmg} (Crit: {testCrit})");
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        ApplyMovement();
    }

    // ==========================================
    // [THÊM MỚI] Hàm xoay tầm nhìn mượt mà
    // ==========================================
        private void UpdateVision()
    {
        if (visionLightPivot != null && lastDir != Vector2.zero)
        {
            // Tính toán góc quay và TRỪ ĐI 90 ĐỘ (- 90f) để bù trừ độ lệch của Light 2D
            float targetAngle = Mathf.Atan2(lastDir.y, lastDir.x) * Mathf.Rad2Deg - 90f;

            // Xoay mượt mà (Slerp) góc hiện tại đến góc mục tiêu
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            visionLightPivot.rotation = Quaternion.Slerp(visionLightPivot.rotation, targetRotation, Time.deltaTime * visionRotationSpeed);
        }
    }
    // ==========================================

    // SORTING
    [ContextMenu("Setup Player Sorting")]
    public void SetupPlayerSorting()
    {
        // 1. Tự động thêm component Sorting Group vào cha nếu chưa có
        var sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sortingGroup == null)
        {
            sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            Debug.Log("[LPC Sorting] Đã tự động thêm component Sorting Group vào Player!");
        }

        // Cập nhật sorting layer cho Sorting Group
        sortingGroup.sortingLayerName = sortingLayerName;

        // 2. Định nghĩa thứ tự Order in Layer chuẩn cho các mảnh con
        var sortOrders = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Shadow", -1 },
            { "WeaponBehind", 0 },
            { "ShieldBehind", 1 },
            { "CapeBehind", 2 },
            { "Quiver", 3 },
            { "HairBehind", 4 },
            { "Body", 5 },
            { "Ears", 6 },
            { "Eyes", 7 },
            { "Underwear", 8 },
            { "Legs", 9 },
            { "Feet", 10 },
            { "Torso", 11 },
            { "Armor", 12 },
            { "Arms", 13 },
            { "Gloves", 14 },
            { "Belt", 15 }
        };

        // 3. Duyệt qua tất cả các Sprite Renderer con để gán layer và order chuẩn
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        int processedCount = 0;

        foreach (var r in renderers)
        {
            // Tránh tác động lên chính cha nếu cha cũng có SpriteRenderer
            if (r.gameObject == gameObject) continue;

            // Gán Sorting Layer đồng bộ
            r.sortingLayerName = sortingLayerName;
            
            // Thiết lập điểm tính toán Sort theo Pivot thay vì Center để Y-sorting hoạt động chính xác
            r.spriteSortPoint = SpriteSortPoint.Pivot;

            string childName = r.gameObject.name;
            bool orderAssigned = false;

            // Kiểm tra khớp chính xác tên bộ phận con
            if (sortOrders.TryGetValue(childName, out int order))
            {
                r.sortingOrder = order;
                orderAssigned = true;
            }
            else
            {
                // Kiểm tra theo từ khóa đặc biệt
                if (childName.Contains("Hair") || childName.Contains("Helmet") || childName.Contains("Hat"))
                {
                    r.sortingOrder = 16;
                    orderAssigned = true;
                }
                else if (childName.Contains("Weapon") || childName.Contains("Shield") || childName.Contains("Tool"))
                {
                    r.sortingOrder = 17;
                    orderAssigned = true;
                }
            }

            // Fallback nếu có bộ phận mới chưa định nghĩa
            if (!orderAssigned)
            {
                r.sortingOrder = 6;
            }

            processedCount++;
        }

        Debug.Log($"[LPC Sorting] Đã tự động cấu hình thành công Sorting cho Player và {processedCount} bộ phận con!");
    }

    private void ApplySortingLayer()
    {
        SetupPlayerSorting();
    }

    // INPUT
    private void ReadInput()
    {
        if (Keyboard.current == null) return;

        float h = 0f, v = 0f;
        if (Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed)  h = -1f;
        if (Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) h =  1f;
        if (Keyboard.current[Key.S].isPressed || Keyboard.current[Key.DownArrow].isPressed)  v = -1f;
        if (Keyboard.current[Key.W].isPressed || Keyboard.current[Key.UpArrow].isPressed)    v =  1f;

        moveInput = new Vector2(h, v).normalized;
        // Chỉ cập nhật hướng quay khi đang KHÔNG tấn công/khóa để khóa hướng khi đánh
        if (!isAttacking && customLockTimer <= 0f && moveInput.sqrMagnitude > 0.01f) lastDir = moveInput;

        // Chỉ cho phép chạy nhanh khi có đủ Stamina Nhanh (> 0) và không bị cạn kiệt (Exhausted)
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        bool running = Keyboard.current[runKey].isPressed && currentStamina > 0f && !isExhausted;
        
        // Tiêu hao Stamina Nhanh khi chạy (15 Stamina/s)
        if (isMoving && running)
        {
            ConsumeStamina(15f * Time.deltaTime);
        }

        float speed  = isMoving ? (running ? 1.5f : 0.5f) : 0f;
        anim.SetFloat(H_Speed, speed);

        // Tấn công thường tiêu hao 8 Stamina Nhanh
        if (Keyboard.current[attackKey].wasPressedThisFrame && !isAttacking)
        {
            if (!IsWeaponTypeEquipped(currentWeapon))
            {
                Debug.LogWarning($"[LPC Combat] Cannot attack! Require correct weapon type equipped for: {currentWeapon}");
                return;
            }

            float attackCost = 8f;
            if (currentStamina >= attackCost)
            {
                ConsumeStamina(attackCost);
                StartAttack(currentWeapon);
            }
        }

        anim.SetBool(H_IsCasting, Keyboard.current[castKey].isPressed);
    }

    // MOVEMENT
    private void ApplyMovement()
    {
        if ((isAttacking || customLockTimer > 0f) && !canMoveWhileAttacking)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }
        bool running = Keyboard.current != null && Keyboard.current[runKey].isPressed && currentStamina > 0f && !isExhausted;
        float speed  = moveInput.sqrMagnitude > 0.01f
            ? finalMoveSpeed * (running ? runMultiplier : 1f) : 0f;
        if (rb != null) rb.linearVelocity = moveInput * speed;
        else transform.Translate(moveInput * speed * Time.fixedDeltaTime);
    }

    // ANIMATOR
    private void UpdateAnimatorParams()
    {
        anim.SetFloat(H_DirX, lastDir.x);
        anim.SetFloat(H_DirY, lastDir.y);
    }

    // COMBAT
    private string GetStateNameForWeapon(WeaponType w) => w switch
    {
        WeaponType.Unarmed_Slash => "Slash",
        WeaponType.Thrust        => "Thrust",
        WeaponType.Bow_Shoot     => "Shoot",
        WeaponType.Spell         => "Spellcast",
        WeaponType.OneHand_Slash => "1h_Slash",
        WeaponType.OneHand_Back  => "1h_Backslash",
        WeaponType.OneHand_Half  => "1h_Halfslash",
        _                        => "Slash",
    };

    public void SetCustomLockDuration(float duration)
    {
        customLockTimer = Mathf.Max(customLockTimer, duration);
    }

    public void RegisterCombatAction()
    {
        lastCombatTime = Time.time;
    }

    public void StartAttack(WeaponType weapon)
    {
        if (isAttacking || isDead) return;
        RegisterCombatAction();
        StartCoroutine(AttackSequence(weapon));
    }

    private IEnumerator AttackSequence(WeaponType weapon)
    {
        isAttacking   = true;
        bool hitChecked = false;
        
        // Lưu tốc độ animator gốc và set theo tốc độ đánh mới
        originalAnimSpeedAtAttackStart = anim.speed;
        anim.speed = originalAnimSpeedAtAttackStart * attackSpeed;
        
        anim.SetBool(H_IsAttacking, true);
        anim.SetInteger(H_AttackType, (int)weapon);

        // Chờ 1 frame để Animator chuyển tham số
        yield return null;

        string targetState = GetStateNameForWeapon(weapon);
        float timeout = 2.0f / Mathf.Max(0.1f, attackSpeed); // Tỷ lệ thuận với tốc độ đánh, tránh chia cho 0
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            var nextStateInfo = anim.GetNextAnimatorStateInfo(0);

            bool isCurrent = stateInfo.IsName(targetState);
            bool isNext = nextStateInfo.IsName(targetState);

            if (isCurrent || isNext)
            {
                var activeState = isCurrent ? stateInfo : nextStateInfo;
                
                // Đăng ký đòn đánh ở đỉnh hoạt ảnh (từ frame 40% đến 80%)
                if (!hitChecked && activeState.normalizedTime >= 0.4f && activeState.normalizedTime < 0.9f)
                {
                    hitChecked = true;
                    PerformHitCheck();
                }

                // Khi hoạt ảnh chạy được 95% (hoặc kết thúc), tự động mở khóa
                if (activeState.normalizedTime >= 0.95f)
                {
                    break;
                }
            }
            else
            {
                // Nếu sau 0.5s vẫn chưa chuyển sang state tấn công, thoát để tránh kẹt
                if (elapsed > 0.5f / Mathf.Max(0.1f, attackSpeed))
                {
                    break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Hỗ trợ phòng ngừa: nếu hoạt ảnh quá ngắn hoặc không quét kịp, tự động kích hoạt đánh khi kết thúc
        if (!hitChecked)
        {
            PerformHitCheck();
        }

        isAttacking = false;
        anim.SetBool(H_IsAttacking, false);
        
        // Khôi phục lại tốc độ animator gốc nếu không ở trạng thái hit stop
        if (!isHitStopping)
        {
            anim.speed = originalAnimSpeedAtAttackStart;
        }
    }

    private void PerformHitCheck()
    {
        // 1. Lấy thông tin vũ khí hiện tại
        float range = 0.8f;
        float angle = 90f;
        bool ranged = false;
        float baseDmg = 5f;

        LPCItemData weaponItem = null;
        if (equipmentManager != null)
        {
            weaponItem = equipmentManager.GetEquipped("Weapon");
            if (weaponItem != null)
            {
                range = weaponItem.attackRange;
                angle = weaponItem.attackAngle;
                ranged = weaponItem.isRanged;
            }
        }

        // 2. Tính toán sát thương cơ bản
        float calculatedDmg = finalATK;
        if (calculatedDmg <= 0) calculatedDmg = baseDmg;

        // ─── THIÊN PHÚ HỆ KHÉO LÉO (DEX) - Nhất kích tất sát ───
        attackCounter++;
        bool isCrit = Random.value <= finalCritRate;
        if (finalDEX >= 15 && attackCounter % 3 == 0)
        {
            isCrit = true;
            Debug.Log("[Talent DEX] Nhất Kích Tất Sát! Đòn thứ 3 chắc chắn chí mạng!");
        }

        // 3. Quét mục tiêu
        Vector2 origin = (Vector2)transform.position;
        Vector2 faceDir = lastDir.normalized;
        if (faceDir == Vector2.zero) faceDir = Vector2.down; // Fallback
        
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(origin, range, enemyLayer);
        
        // Lọc danh sách kẻ địch và tài nguyên hái lượm/quặng hợp lệ trong vùng quét
        var validEnemies = new List<EnemyAI>();
        var validHarvestables = new List<LPC_Harvestable>();
        foreach (var col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;
            
            // 1. Quét kẻ địch
            EnemyAI enemy = col.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                if (!ranged)
                {
                    Vector2 dirToEnemy = ((Vector2)col.transform.position - origin);
                    if (dirToEnemy.magnitude > 0.05f)
                    {
                        float angleToEnemy = Vector2.Angle(faceDir, dirToEnemy);
                        if (angleToEnemy > angle * 0.5f) continue;
                    }
                }
                validEnemies.Add(enemy);
                continue;
            }

            // 2. Quét khoáng sản / thực vật hái lượm đập vỡ được
            LPC_Harvestable harvestable = col.GetComponent<LPC_Harvestable>();
            if (harvestable != null)
            {
                if (!ranged)
                {
                    Vector2 dirToRes = ((Vector2)col.transform.position - origin);
                    if (dirToRes.magnitude > 0.05f)
                    {
                        float angleToRes = Vector2.Angle(faceDir, dirToRes);
                        if (angleToRes > angle * 0.5f) continue;
                    }
                }
                validHarvestables.Add(harvestable);
            }
        }

        // Phân loại mục tiêu dựa trên loại vũ khí chém lan hay đơn mục tiêu
        bool isCleave = (currentWeapon == WeaponType.OneHand_Slash || currentWeapon == WeaponType.OneHand_Back || currentWeapon == WeaponType.OneHand_Half || currentWeapon == WeaponType.Bow_Shoot);
        
        var targetsToHit = new List<EnemyAI>();
        if (validEnemies.Count > 0)
        {
            if (isCleave)
            {
                targetsToHit = validEnemies;
            }
            else
            {
                // Cận chiến thường (Unarmed, Giáo...): Chỉ đánh trúng 1 mục tiêu gần nhất
                EnemyAI closest = validEnemies[0];
                float minDist = Vector2.Distance(origin, closest.transform.position);
                for (int j = 1; j < validEnemies.Count; j++)
                 {
                    float dist = Vector2.Distance(origin, validEnemies[j].transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = validEnemies[j];
                    }
                }
                targetsToHit.Add(closest);
            }
        }

        bool hasHitAny = false;
        bool isPrimary = true;

        foreach (var enemy in targetsToHit)
        {
            float targetDmg = calculatedDmg;
            
            // Chí mạng
            if (isCrit)
            {
                targetDmg *= finalCritDamage;
            }

            // Mục tiêu phụ khi chém lan chỉ chịu 70% sát thương
            if (!isPrimary && isCleave)
            {
                targetDmg *= 0.7f;
            }
            isPrimary = false;

            float pen = finalArmorPenetration;
            bool isMagic = (currentWeapon == WeaponType.Spell);

            // ─── WEAPON EFFECT: Giáo/Thương (Thrust) ───
            if (currentWeapon == WeaponType.Thrust)
            {
                pen += 10f; // Cộng thêm 10 xuyên giáp vật lý
                if (Random.value <= 0.30f) // 30% gây xuất huyết (Bleed)
                {
                    var enemyBuffs = enemy.GetComponent<LPC_BuffManager>();
                    if (enemyBuffs == null) enemyBuffs = enemy.gameObject.AddComponent<LPC_BuffManager>();
                    enemyBuffs.AddBuff(LPC_BuffManager.BuffType.Bleed, 4f, finalATK * 0.2f); // Xuất huyết trong 4 giây, deal 20% ATK/tick
                    Debug.Log("[Weapon Thrust] Kích hoạt Xuất Huyết (Bleed) lên quái!");
                }
            }

            // ─── WEAPON EFFECT: Cung tên (Bow_Shoot) ───
            if (currentWeapon == WeaponType.Bow_Shoot)
            {
                float dist = Vector2.Distance(origin, enemy.transform.position);
                float distMultiplier = 1f + Mathf.Min(0.5f, dist * 0.05f); // +5% mỗi mét, tối đa +50%
                targetDmg *= distMultiplier;
                pen = 9999f; // Mũi tên bắn xuyên giáp hoàn toàn
            }

            // ─── WEAPON EFFECT: Trượng phép (Spell) ───
            if (currentWeapon == WeaponType.Spell)
            {
                currentMP = Mathf.Min(maxMP, currentMP + 3f); // Hồi 3 Mana mỗi đòn đánh thường
                Debug.Log("[Weapon Spell] Hồi 3 MP khi đánh thường!");
                
                // 20% cơ hội kích hoạt Quả cầu phép bay ra
                if (spellProjectilePrefab != null && Random.value <= 0.20f)
                {
                    Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(faceDir.y, faceDir.x) * Mathf.Rad2Deg);
                    Vector3 spawnPos = transform.position + (Vector3)(faceDir * 0.5f);
                    GameObject projObj = Instantiate(spellProjectilePrefab, spawnPos, rotation);
                    projObj.SetActive(true);
                    
                    LPC_Projectile proj = projObj.GetComponent<LPC_Projectile>();
                    if (proj == null) proj = projObj.AddComponent<LPC_Projectile>();
                    
                    SkillData dummyData = ScriptableObject.CreateInstance<SkillData>();
                    dummyData.isMagicDamage = true;
                    dummyData.damageMultiplier = 0.8f; // Gây 80% MATK
                    dummyData.buffType = LPC_BuffManager.BuffType.Burn;
                    dummyData.applyBuffDebuff = false;
                    
                    proj.Initialize(this, faceDir, dummyData, 1.0f, 8f, 3f, 0f, spellProjectilePrefab);
                    Debug.Log("[Weapon Spell] Phóng Quả Cầu Phép phụ!");
                }
            }

            int damageAmount = Mathf.RoundToInt(targetDmg);
            
            // Gây sát thương (truyền xuyên giáp & loại sát thương phép/vật lý)
            enemy.TakeDamage(damageAmount, isMagic, pen);
            enemy.TriggerHitStop(0.08f); // Kích hoạt khựng hình quái vật cùng lúc với nhân vật!
            hasHitAny = true;

            // Áp dụng Đẩy lùi (Knockback) lên quái
            float kbForce = (weaponItem != null) ? weaponItem.knockbackForce : 3.0f;
            float kbDuration = (weaponItem != null) ? weaponItem.knockbackDuration : 0.2f;
            Vector2 kbDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            if (kbDir == Vector2.zero) kbDir = lastDir;
            enemy.ApplyKnockback(kbDir, kbForce, kbDuration);

            // Bắn ra các tia máu pixel bay theo hướng chém/đẩy lui cực kỳ sống động!
            SpawnRuntimeBloodSplatter(enemy.transform.position + Vector3.up * 0.4f, kbDir, 18);

            // Trừ độ bền của vũ khí khi chém trúng quái (giảm 1 điểm mỗi đòn chém trúng)
            if (weaponItem != null)
            {
                bool wasBrokenBefore = weaponItem.IsBroken;
                weaponItem.TakeDurabilityDamage(1.0f * weaponItem.durabilityLossMultiplier);
                Debug.Log($"[Durability] {weaponItem.itemName} hit enemy! Durability: {weaponItem.currentDurability:0}/{weaponItem.maxDurability:0}");
                // Cập nhật lại chỉ số (ví dụ: nếu IsBroken thì sát thương giảm nặng còn 25%)
                CalculateFinalStats();
                CheckWeaponShatter(weaponItem, wasBrokenBefore);
            }

            // --- HỆ THỐNG ENCHANTMENT & HIỆU ỨNG TRẠNG THÁI VŨ KHÍ ---
            if (weaponItem != null && weaponItem.enchantmentEffect != LPC_BuffManager.BuffType.None && weaponItem.enchantmentDuration > 0f)
            {
                var enemyBuffs = enemy.GetComponent<LPC_BuffManager>();
                if (enemyBuffs == null) enemyBuffs = enemy.gameObject.AddComponent<LPC_BuffManager>();
                
                enemyBuffs.AddBuff(weaponItem.enchantmentEffect, weaponItem.enchantmentDuration, weaponItem.enchantmentValue);
                Debug.Log($"[Enchantment] Áp dụng hiệu ứng {weaponItem.enchantmentEffect} lên {enemy.name} trong {weaponItem.enchantmentDuration}s (Giá trị: {weaponItem.enchantmentValue})!");
            }

            Debug.Log($"[Combat] Hit {enemy.name} for {damageAmount} damage with knockback! (Crit: {isCrit})");

            // Tạo chữ số sát thương bay lên
            SpawnDamageText(enemy.transform.position + Vector3.up * 0.5f, damageAmount, isCrit);

            // Tự động hồi máu theo Lifesteal / SpellVamp
            if (isMagic && finalSpellVamp > 0f)
            {
                float heal = damageAmount * finalSpellVamp;
                currentHP = Mathf.Clamp(currentHP + heal, 0f, maxHP);
            }
            else if (!isMagic && finalLifesteal > 0f)
            {
                float heal = damageAmount * finalLifesteal;
                currentHP = Mathf.Clamp(currentHP + heal, 0f, maxHP);
            }
        }

        // 3.5. Gây sát thương thu hoạch lên quặng/thảo mộc
        foreach (var harvestable in validHarvestables)
        {
            if (harvestable != null)
            {
                harvestable.TakeDamage(1); // Gõ 1 hit
                hasHitAny = true;
            }
        }

        // 4. Hiệu ứng Khựng hình (Hit Stop) khi đánh trúng ít nhất 1 kẻ địch
        if (hasHitAny)
        {
            StartCoroutine(HitStopCoroutine(0.08f));
        }
    }

    public void SpawnDamageText(Vector3 position, int damage, bool isCrit)
    {
        GameObject textObj = null;

        if (damageTextPrefab != null)
        {
            textObj = Instantiate(damageTextPrefab, position, Quaternion.identity);
        }
        else
        {
            // TỰ KHỞI TẠO ĐỘNG BẰNG CODE (FALLBACK CỰC MẠNH)
            Debug.LogWarning("[Combat] damageTextPrefab is null! Tự động tạo chữ số sát thương bằng code.");
            textObj = new GameObject("Runtime_DamageText");
            textObj.transform.position = position;

            // Thêm TextMeshPro (Dùng đường dẫn đầy đủ của class để tránh lỗi thiếu import)
            TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontSize = 4f; // Kích thước chữ cho thế giới 2D

            // Thêm script điều khiển bay và mờ dần
            textObj.AddComponent<LPC_FloatingText>();
        }

        LPC_FloatingText floatingText = textObj.GetComponent<LPC_FloatingText>();
        if (floatingText != null)
        {
            Color txtColor = isCrit ? new Color(1f, 0.3f, 0f) : new Color(0.95f, 0.95f, 0.95f);
            float sizeMul = isCrit ? 1.5f : 1.0f;
            string textToShow = damage.ToString();
            
            if (damage == 0)
            {
                textToShow = "Dodge!";
                txtColor = new Color(0.7f, 0.7f, 0.7f); // Màu xám cho Né tránh
                sizeMul = 1.0f;
            }
            else if (isCrit)
            {
                textToShow += "!";
            }
            
            floatingText.Setup(textToShow, txtColor, sizeMul);
        }
    }

    private void SpawnRuntimeShatterParticles(Vector3 pos, Color color, int count)
    {
        GameObject pObj = new GameObject("Runtime_ShatterVFX");
        pObj.transform.position = pos;
        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();

        // Unity tự động chạy hệ thống hạt khi AddComponent, ta cần dừng lại trước khi chỉnh cấu hình để tránh warning
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Thiết lập các thuộc tính Particle System bằng code (Fallback cực mạnh)
        var main = ps.main;
        main.startColor = color;
        main.startSize = 0.15f;
        main.startSpeed = 4.5f;
        main.duration = 0.5f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy; // Tự động hủy GameObject sau khi phát xong hạt

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.burstCount = 1;
        
        // Tạo Burst hạt
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
        bursts[0] = new ParticleSystem.Burst(0.0f, (short)count);
        emission.SetBursts(bursts);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        ps.Play();
    }

    private void SpawnRuntimeBloodSplatter(Vector3 pos, Vector2 direction, int count)
    {
        GameObject pObj = new GameObject("Runtime_BloodSplatterVFX");
        pObj.transform.position = pos;
        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();

        // Tạm dừng để cấu hình
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Màu đỏ thẫm pixel máu lửa
        Color bloodColor = new Color(0.65f, 0.02f, 0.02f, 0.95f);

        var main = ps.main;
        main.startColor = bloodColor;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f); // Hạt nhỏ phong cách pixel
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);  // Bắn ra với tốc độ đa dạng
        main.duration = 0.4f;
        main.loop = false;
        main.gravityModifier = 1.8f; // Trọng lực kéo hạt máu rơi xòe xuống đất
        main.stopAction = ParticleSystemStopAction.Destroy; // Tự hủy khi phát xong

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.burstCount = 1;
        
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
        bursts[0] = new ParticleSystem.Burst(0.0f, (short)count);
        emission.SetBursts(bursts);

        // Tạo luồng nón (Cone) bắn tia máu theo hướng đẩy lui
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.05f;
        shape.angle = 25f; // Luồng phun máu gom cụm góc 25 độ cực đẹp

        // Xoay hướng bắn trùng khớp với hướng đẩy lui (Knockback)
        float angleZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        pObj.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        ps.Play();
    }

    public void CheckWeaponShatter(LPCItemData weaponItem, bool wasBrokenBefore)
    {
        if (weaponItem == null) return;
        
        if (weaponItem.IsBroken && !wasBrokenBefore)
        {
            // Sinh hiệu ứng hình ảnh (VFX) vỡ vũ khí
            if (weaponShatterVfxPrefab != null)
            {
                Instantiate(weaponShatterVfxPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                // Tự sinh 30 mảnh sắt xám bạc bay tóe ra 360 độ cực đẹp bằng code!
                SpawnRuntimeShatterParticles(transform.position, new Color(0.75f, 0.75f, 0.8f, 0.9f), 30);
            }

            // Hiển thị Floating Text WEAPON SHATTERED!
            GameObject shatterText = new GameObject("Runtime_WeaponShatterText");
            shatterText.transform.position = transform.position + Vector3.up * 1.5f;
            TMPro.TextMeshPro sTmp = shatterText.AddComponent<TMPro.TextMeshPro>();
            sTmp.alignment = TMPro.TextAlignmentOptions.Center;
            sTmp.fontSize = 5f;
            LPC_FloatingText sFt = shatterText.AddComponent<LPC_FloatingText>();
            sFt.Setup("WEAPON SHATTERED!", new Color(1f, 0.2f, 0.2f), 1.4f);

            // Gây sát thương nổ mảnh vụn (Shrapnel Blast) diện rộng 3m xung quanh người chơi
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 3f, enemyLayer);
            int damagedCount = 0;
            foreach (var col in hitEnemies)
            {
                if (col.gameObject == gameObject) continue;
                EnemyAI enemy = col.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    // Gây sát thương nổ bằng 150% ATK, xuyên giáp
                    float shrapnelDmg = finalATK * 1.5f;
                    enemy.TakeDamage(shrapnelDmg, false, finalArmorPenetration);

                    // Thêm debuff xuất huyết (Bleed) trong 3 giây
                    var enemyBuffs = enemy.GetComponent<LPC_BuffManager>();
                    if (enemyBuffs == null) enemyBuffs = enemy.gameObject.AddComponent<LPC_BuffManager>();
                    enemyBuffs.AddBuff(LPC_BuffManager.BuffType.Bleed, 3f, finalATK * 0.15f);

                    // Đẩy lùi nhẹ quái ra
                    Vector2 kbDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                    if (kbDir == Vector2.zero) kbDir = lastDir;
                    enemy.ApplyKnockback(kbDir, 4.0f, 0.2f);
                    
                    // Tạo chữ số sát thương nổ mảnh vụn
                    SpawnDamageText(enemy.transform.position + Vector3.up * 0.4f, Mathf.RoundToInt(shrapnelDmg), true);
                    damagedCount++;
                }
            }
            Debug.LogWarning($"[Durability] {weaponItem.itemName} SHATTERED! Kích hoạt Nổ Mảnh Vụn gây sát thương & xuất huyết lên {damagedCount} kẻ địch xung quanh!");

            // Buff Lightweight Adrenaline (+30% tốc chạy trong 3 giây thông qua buff WindWalk có sẵn)
            var buffMgr = GetComponent<LPC_BuffManager>();
            if (buffMgr != null)
            {
                buffMgr.AddBuff(LPC_BuffManager.BuffType.WindWalk, 3.0f, 0f);
            }
            
            CalculateFinalStats(); // Đồng bộ lại chỉ số ngay lập tức
        }
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        if (isHitStopping) yield break;
        isHitStopping = true;
        
        anim.speed = 0.02f; // Khựng hình gần như hoàn toàn
        yield return new WaitForSeconds(duration);
        
        isHitStopping = false;
        if (isAttacking)
        {
            anim.speed = originalAnimSpeedAtAttackStart * attackSpeed;
        }
        else
        {
            anim.speed = originalAnimSpeedAtAttackStart;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showAttackGizmo) return;

        // Lấy thông tin vũ khí
        float range = 0.8f;
        float angle = 90f;
        bool ranged = false;
        string weaponName = "Unarmed";

        if (equipmentManager != null)
        {
            var weaponItem = equipmentManager.GetEquipped("Weapon");
            if (weaponItem != null)
            {
                range = weaponItem.attackRange;
                angle = weaponItem.attackAngle;
                ranged = weaponItem.isRanged;
                weaponName = weaponItem.itemName;
            }
        }

        Vector3 pos = transform.position;
        Vector3 direction = (Vector3)lastDir.normalized;

        Color coneColor = isAttacking ? new Color(1f, 0f, 0f, 0.35f) : new Color(1f, 0.92f, 0.016f, 0.15f);
        Color outlineColor = isAttacking ? Color.red : new Color(1f, 0.92f, 0.016f, 0.7f);

        UnityEditor.Handles.color = coneColor;
        Vector3 startDirection = Quaternion.Euler(0, 0, -angle * 0.5f) * direction;

        if (ranged)
        {
            UnityEditor.Handles.DrawSolidArc(pos, Vector3.forward, Vector3.right, 360f, range);
            UnityEditor.Handles.color = outlineColor;
            UnityEditor.Handles.DrawWireArc(pos, Vector3.forward, Vector3.right, 360f, range);
            Gizmos.color = outlineColor;
            Gizmos.DrawLine(pos, pos + direction * range);
        }
        else
        {
            UnityEditor.Handles.DrawSolidArc(pos, Vector3.forward, startDirection, angle, range);
            UnityEditor.Handles.color = outlineColor;
            UnityEditor.Handles.DrawWireArc(pos, Vector3.forward, startDirection, angle, range);
            
            Vector3 endDirection = Quaternion.Euler(0, 0, angle * 0.5f) * direction;
            Gizmos.color = outlineColor;
            Gizmos.DrawLine(pos, pos + startDirection * range);
            Gizmos.DrawLine(pos, pos + endDirection * range);
        }

        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = isAttacking ? Color.red : Color.yellow;
        labelStyle.fontSize = 12;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        string debugText = $"{weaponName}\nRange: {range}m | Angle: {angle}°\nState: {(isAttacking ? "ATTACKING" : "READY")}";
        UnityEditor.Handles.Label(pos + Vector3.up * 1.2f, debugText, labelStyle);
    }
#endif

    private float GetDuration(WeaponType w) => w switch
    {
        WeaponType.Unarmed_Slash => 6f  / 8f,
        WeaponType.Thrust        => 8f  / 8f,
        WeaponType.Bow_Shoot     => 13f / 8f,
        WeaponType.Spell         => 7f  / 8f,
        WeaponType.OneHand_Slash => 13f / 8f,
        WeaponType.OneHand_Back  => 13f / 8f,
        WeaponType.OneHand_Half  => 6f  / 8f,
        _                        => 8f  / 8f,
    };

    // PUBLIC API
    public void TakeHit()  { if (!isDead) anim.SetTrigger(H_IsHurt); }
    public void Die()      { isDead = true; isAttacking = false; if (rb) rb.linearVelocity = Vector2.zero; anim.SetBool(H_IsDead, true); }
    public void Revive()   { isDead = false; anim.SetBool(H_IsDead, false); }
    public void EquipWeapon(WeaponType w) => currentWeapon = w;

    public bool IsWeaponTypeEquipped(WeaponType type)
    {
        // Spellcast (Spell) does not require a weapon
        if (type == WeaponType.Spell)
            return true;

        if (equipmentManager == null) return false;
        var weaponItem = equipmentManager.GetEquipped("Weapon");

        // If checking Unarmed_Slash, it's allowed if no weapon is equipped OR if the equipped weapon is Unarmed_Slash type
        if (type == WeaponType.Unarmed_Slash)
            return weaponItem == null || weaponItem.weaponType == WeaponType.Unarmed_Slash;

        // For other weapon types, a weapon must be equipped
        if (weaponItem == null) return false;

        // If checking 1h Slash variations, allow if equipped weapon is any 1h slash variation
        bool isTarget1h = (type == WeaponType.OneHand_Slash || type == WeaponType.OneHand_Back || type == WeaponType.OneHand_Half);
        bool isEquipped1h = (weaponItem.weaponType == WeaponType.OneHand_Slash || weaponItem.weaponType == WeaponType.OneHand_Back || weaponItem.weaponType == WeaponType.OneHand_Half);
        if (isTarget1h && isEquipped1h)
            return true;

        return weaponItem.weaponType == type;
    }

    private void ValidateAnimator()
    {
        if (anim.runtimeAnimatorController == null)
            Debug.LogError("[LPC] Animator has no Controller assigned!");
        string[] req = { "DirectionX","DirectionY","Speed","IsAttacking","AttackType","IsCasting","IsHurt","IsDead" };
        var have = new System.Collections.Generic.HashSet<string>();
        foreach (var p in anim.parameters) have.Add(p.name);
        foreach (string r in req) if (!have.Contains(r)) Debug.LogWarning($"[LPC] Missing param: '{r}'");
    }

    private void OnGUI()
    {
        if (!showDebugGUI) return;
        GUI.Box(new Rect(10,10,260,120),"LPC Debug");
        GUI.Label(new Rect(20,35,240,20),$"Dir: ({lastDir.x:F1}, {lastDir.y:F1})");
        GUI.Label(new Rect(20,55,240,20),$"Speed: {anim.GetFloat(H_Speed):F2}");
        GUI.Label(new Rect(20,75,240,20),$"Attack: {isAttacking}  Weapon: {currentWeapon}");
        GUI.Label(new Rect(20,95,240,20),$"Dead: {isDead}  Layer: {sortingLayerName}");
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(LPCPlayerController2))]
public class LPCPlayerController2Editor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ giao diện Inspector mặc định
        DrawDefaultInspector();

        LPCPlayerController2 controller = (LPCPlayerController2)target;

        GUILayout.Space(15);
        
        // Màu xanh lá cây rực rỡ và chuyên nghiệp
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        
        if (GUILayout.Button("⚡ TỰ ĐỘNG CẤU HÌNH SORTING PLAYER (CLICK ĐÂY!)", GUILayout.Height(40)))
        {
            controller.SetupPlayerSorting();
            
            // Đánh dấu bẩn để Unity lưu lại thay đổi của Scene/Prefab
            UnityEditor.EditorUtility.SetDirty(controller.gameObject);
            
            // Nếu đang mở trong Prefab Mode, đánh dấu bẩn cho asset prefab
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }
        
        GUI.backgroundColor = Color.white;
    }
}
#endif