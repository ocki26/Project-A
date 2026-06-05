using UnityEngine;

public class LPC_Projectile : MonoBehaviour
{
    private LPCPlayerController2 caster;
    private Vector2 direction;
    private SkillData skillData;
    private float speed;
    private float lifetime;
    private bool isInitialized = false;
    private Quaternion targetRotation;

    public void Initialize(LPCPlayerController2 caster, Vector2 dir, SkillData data)
    {
        Initialize(caster, dir, data, data.vfxScale, data.projectileSpeed, data.lifespan, data.vfxRotationOffset, data.vfxPrefab);
    }

    public void Initialize(LPCPlayerController2 caster, Vector2 dir, SkillData data, float customScale, float customSpeed, float customLifespan, float rotationOffset, GameObject prefabSource)
    {
        this.caster = caster;
        this.direction = dir.normalized;
        this.skillData = data;
        this.speed = customSpeed;
        this.lifetime = customLifespan;

        // Tính toán góc xoay mục tiêu kèm bù trừ offset từ đầu (gồm vfxRotationOffset + góc xoay mặc định của Prefab)
        float prefabZRotation = prefabSource != null ? prefabSource.transform.rotation.eulerAngles.z : 0f;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset + prefabZRotation;
        this.targetRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = targetRotation;

        this.isInitialized = true;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isInitialized) return;
        
        // Di chuyển đạn theo đường thẳng tuyệt đối
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void LateUpdate()
    {
        if (!isInitialized) return;

        // ÉP góc xoay ở LateUpdate để ghi đè lên sự khóa cứng của Animator/Animation Clip
        transform.rotation = targetRotation;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isInitialized || caster == null) return;

        // Tránh tự bắn trúng bản thân
        if (collision.gameObject == caster.gameObject) return;

        // Chỉ va chạm với Enemy Layer
        if (((1 << collision.gameObject.layer) & caster.enemyLayer.value) != 0)
        {
            EnemyAI enemy = collision.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Tính sát thương dựa trên thuộc tính phép/vật lý
                float baseCasterDamage = skillData.isMagicDamage ? caster.finalMATK : caster.finalATK;
                float finalDamage = baseCasterDamage * skillData.damageMultiplier;

                // ─── THIÊN PHÚ HỆ TRÍ TUỆ (INT) - Ma pháp quá tải (Tăng sát thương kỹ năng) ───
                if (caster.finalINT >= 15)
                {
                    finalDamage *= 1.20f; // Tăng 20% sát thương kỹ năng
                }

                // Tính chí mạng
                bool isCrit = Random.value <= caster.finalCritRate + skillData.criticalChanceModifier;
                if (isCrit) finalDamage *= caster.finalCritDamage;

                int damageAmount = Mathf.RoundToInt(finalDamage);

                // Gây sát thương cho Enemy (truyền xuyên giáp/kháng tương ứng)
                float pen = skillData.isMagicDamage ? caster.finalMagicPenetration : caster.finalArmorPenetration;
                enemy.TakeDamage(damageAmount, skillData.isMagicDamage, pen);

                // Spawn text sát thương (thông qua hàm public của Caster)
                caster.SpawnDamageText(collision.transform.position + Vector3.up * 0.5f, damageAmount, isCrit);

                // Áp dụng trạng thái bất lợi (Buff/Debuff) lên quái
                if (skillData.applyBuffDebuff)
                {
                    LPC_BuffManager enemyBuffs = enemy.GetComponent<LPC_BuffManager>();
                    if (enemyBuffs == null) enemyBuffs = enemy.gameObject.AddComponent<LPC_BuffManager>();
                    
                    enemyBuffs.AddBuff(skillData.buffType, skillData.buffDuration, skillData.buffValue);
                }

                // Hủy đạn khi va chạm
                Destroy(gameObject);
            }
        }
    }
}
