using UnityEngine;

public class LPC_VFXEffect : MonoBehaviour
{
    private LPCPlayerController2 caster;
    private SkillData skillData;
    private bool followCaster = false;
    private bool isInitialized = false;
    private Quaternion targetRotation;

    private float finalScale = 1.0f;
    private float finalRange = 1.5f;
    private Vector3 finalSpawnOffset;

    public void Initialize(LPCPlayerController2 caster, SkillData data)
    {
        Initialize(caster, data, data.vfxScale, data.attackRange, data.lifespan, data.vfxRotationOffset, data.vfxPrefab, data.spawnOffset);
    }

    public void Initialize(LPCPlayerController2 caster, SkillData data, float customScale, float customRange, float lifespanVal, float rotationOffset, GameObject prefabSource, Vector3 customSpawnOffset)
    {
        this.caster = caster;
        this.skillData = data;
        this.finalScale = customScale;
        this.finalRange = customRange;
        this.finalSpawnOffset = customSpawnOffset;
        
        // Phép buff hào quang bám theo caster (như Adrenaline Rush hoặc hồi phục), còn kiếm khí/chém lửa đứng yên tại chỗ chém
        this.followCaster = (data.projectileSpeed == 0f && (data.skillName.Contains("Adrenaline") || data.buffType == LPC_BuffManager.BuffType.Regeneration || data.buffType == LPC_BuffManager.BuffType.Shield));
        
        // Tính toán góc xoay mục tiêu kèm bù trừ offset từ đầu (gồm vfxRotationOffset + góc xoay mặc định của Prefab)
        Vector2 faceDir = caster.lastDir.normalized;
        if (faceDir == Vector2.zero) faceDir = Vector2.down;
        float prefabZRotation = prefabSource != null ? prefabSource.transform.rotation.eulerAngles.z : 0f;
        float angle = Mathf.Atan2(faceDir.y, faceDir.x) * Mathf.Rad2Deg + rotationOffset + prefabZRotation;
        this.targetRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = targetRotation;

        this.isInitialized = true;

        Debug.Log($"[LPC VFX Debug] Hiệu ứng {gameObject.name} đã được khởi tạo thành công tại: {transform.position}, Bám theo nhân vật: {followCaster}");

        // Tự động hủy sau thời lượng định sẵn
        Destroy(gameObject, lifespanVal);

        // Áp dụng hiệu ứng Buff lên bản thân caster
        if (data.applyBuffDebuff && caster != null)
        {
            LPC_BuffManager casterBuffs = caster.GetComponent<LPC_BuffManager>();
            if (casterBuffs != null)
            {
                casterBuffs.AddBuff(data.buffType, data.buffDuration, data.buffValue);
            }
        }
        
        // Quét sát thương cận chiến tức thời (cho chém lửa, kiếm khí chém lan tại chỗ)
        if (data.projectileSpeed == 0f && !followCaster)
        {
            PerformMeleeAreaDamage();
        }
    }

    private void PerformMeleeAreaDamage()
    {
        if (caster == null) return;
        
        // Quét sát thương cận chiến hình tròn tại tâm VFX dựa trên tầm đánh đã được cấu hình nhân với tỉ lệ scale của VFX
        float range = finalRange * finalScale;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, range, caster.enemyLayer);
        
        Debug.Log($"[LPC VFX Area] Đang quét sát thương chém cận chiến tại: {transform.position}, Tầm quét: {range}m. Tìm thấy {hitColliders.Length} đối tượng va chạm.");

        foreach (var col in hitColliders)
        {
            if (col.gameObject == caster.gameObject) continue;
            EnemyAI enemy = col.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                float baseCasterDamage = skillData.isMagicDamage ? caster.finalMATK : caster.finalATK;
                float finalDamage = baseCasterDamage * skillData.damageMultiplier;
                
                // ─── THIÊN PHÚ HỆ TRÍ TUỆ (INT) - Ma pháp quá tải (Tăng sát thương kỹ năng) ───
                if (caster.finalINT >= 15)
                {
                    finalDamage *= 1.20f; // Tăng 20% sát thương kỹ năng
                }

                bool isCrit = Random.value <= caster.finalCritRate + skillData.criticalChanceModifier;
                if (isCrit) finalDamage *= caster.finalCritDamage;

                int damageAmount = Mathf.RoundToInt(finalDamage);
                
                float pen = skillData.isMagicDamage ? caster.finalMagicPenetration : caster.finalArmorPenetration;
                enemy.TakeDamage(damageAmount, skillData.isMagicDamage, pen);

                caster.SpawnDamageText(col.transform.position + Vector3.up * 0.5f, damageAmount, isCrit);

                if (skillData.applyBuffDebuff)
                {
                    LPC_BuffManager enemyBuffs = enemy.GetComponent<LPC_BuffManager>();
                    if (enemyBuffs == null) enemyBuffs = enemy.gameObject.AddComponent<LPC_BuffManager>();
                    enemyBuffs.AddBuff(skillData.buffType, skillData.buffDuration, skillData.buffValue);
                }
            }
        }
    }

    private void Update()
    {
        if (followCaster && caster != null)
        {
            Vector3 targetPos = caster.transform.position + finalSpawnOffset;
            targetPos.z = caster.transform.position.z; // Khóa chặt Z
            transform.position = targetPos;
        }
    }

    private void LateUpdate()
    {
        if (!isInitialized) return;

        // ÉP góc xoay ở LateUpdate để ghi đè lên sự khóa cứng của Animator/Animation Clip
        transform.rotation = targetRotation;
    }
}
