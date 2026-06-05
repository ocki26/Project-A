using UnityEngine;
using System.Collections.Generic;

public class LPC_SkillManager : MonoBehaviour
{
    [System.Serializable]
    public class Skill
    {
        public SkillData data; // Asset cấu hình kỹ năng
        
        [HideInInspector]
        public float cooldownTimer;

        // Ánh xạ thuộc tính tự động giúp LPC_HUD_Manager không bị lỗi biên dịch
        public string name => data != null ? data.skillName : "Unnamed";
        public string desc => data != null ? data.description : "";
        public float cooldown => data != null ? data.cooldown : 0f;
        public float manaCost => data != null ? data.manaCost : 0f;
        public float staminaCost => data != null ? data.staminaCost : 0f;
        public LPCPlayerController2.WeaponType animationType => data != null ? data.animationType : LPCPlayerController2.WeaponType.Spell;
    }

    public List<Skill> skills = new List<Skill>();
    private LPCPlayerController2 player;
    private LPC_BuffManager buffManager;

    private void Awake()
    {
        player = GetComponent<LPCPlayerController2>();
        buffManager = GetComponent<LPC_BuffManager>();
    }

    private void Update()
    {
        if (player == null || player.isDeadState) return;

        foreach (var skill in skills)
        {
            if (skill.cooldownTimer > 0f)
                skill.cooldownTimer = Mathf.Max(0f, skill.cooldownTimer - Time.deltaTime);
        }

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.qKey.wasPressedThisFrame) TriggerSkill(0);
            if (kb.fKey.wasPressedThisFrame) TriggerSkill(1);
            if (kb.eKey.wasPressedThisFrame) TriggerSkill(2);
            if (kb.rKey.wasPressedThisFrame) TriggerSkill(3);
        }
    }

    public void TriggerSkill(int index)
    {
        if (index < 0 || index >= skills.Count) return;
        var skill = skills[index];

        // Ràng buộc: phải trang bị kỹ năng mới được cast
        if (skill.data == null) return;
        if (skill.cooldownTimer > 0f) return;

        // ─── THIÊN PHÚ HỆ TRÍ TUỆ (INT) - Ma pháp quá tải (Tăng mana cost) ───
        float finalManaCost = skill.manaCost;
        if (player != null && player.finalINT >= 15)
        {
            finalManaCost *= 1.10f; // Tốn thêm 10% MP
        }

        if (player.currentMP < finalManaCost) return;
        if (player.currentStamina < skill.staminaCost) return;

        // Kiểm tra loại vũ khí yêu cầu của kỹ năng
        if (!player.IsWeaponTypeEquipped(skill.animationType))
        {
            Debug.LogWarning($"[LPC Skills] Không thể cast {skill.name}! Yêu cầu trang bị đúng vũ khí thuộc loại {skill.animationType}.");
            return;
        }

        // Khấu trừ tài nguyên
        player.currentMP -= finalManaCost;
        player.ConsumeStamina(skill.staminaCost);

        // Giảm 5 điểm độ bền của vũ khí khi sử dụng kỹ năng
        if (player.equipmentManager != null)
        {
            var weaponItem = player.equipmentManager.GetEquipped("Weapon");
            if (weaponItem != null)
            {
                bool wasBrokenBefore = weaponItem.IsBroken;
                weaponItem.TakeDurabilityDamage(5.0f * weaponItem.durabilityLossMultiplier);
                Debug.Log($"[Durability] Used skill {skill.name}! Weapon {weaponItem.itemName} durability decreased by {5.0f * weaponItem.durabilityLossMultiplier:F1}! Current: {weaponItem.currentDurability:0}/{weaponItem.maxDurability:0}");
                player.CalculateFinalStats(); // Đồng bộ lại sát thương (có thể hỏng)
                player.CheckWeaponShatter(weaponItem, wasBrokenBefore);
            }
        }

        skill.cooldownTimer = skill.cooldown;
        
        // ─── PHÁT ÂM THANH KỸ NĂNG (SFX) ───
        if (skill.data.sfxClip != null)
        {
            AudioSource.PlayClipAtPoint(skill.data.sfxClip, transform.position);
        }

        // Kích hoạt hoạt ảnh nhân vật chém/phép tương ứng
        player.StartAttack(skill.animationType);

        // Khóa di chuyển/quay đầu của nhân vật nếu được cấu hình khóa hành động riêng
        if (skill.data.castLockDuration > 0f)
        {
            player.SetCustomLockDuration(skill.data.castLockDuration);
        }

        // Xử lý kích hoạt kỹ năng đặc trưng kèm độ trễ hoạt ảnh
        StartCoroutine(ExecuteSkillEffectsCoroutine(skill.data));
    }

    private System.Collections.IEnumerator ExecuteSkillEffectsCoroutine(SkillData data)
    {
        if (data.vfxList != null && data.vfxList.Count > 0)
        {
            // Chạy song song nhiều hiệu ứng VFX với thời lượng trễ (delay) riêng biệt
            foreach (var config in data.vfxList)
            {
                if (config.vfxPrefab != null)
                {
                    StartCoroutine(SpawnSingleVfxConfigCoroutine(data, config));
                }
            }
            yield break;
        }

        // --- FALLBACK: Sinh 1 VFX đơn lẻ như cũ nếu danh sách trống ---
        // Chờ thời gian trễ hoạt ảnh vung kiếm/niệm chú trước khi xuất hiện hiệu ứng
        if (data.vfxDelay > 0f)
        {
            yield return new WaitForSeconds(data.vfxDelay);
        }

        if (data.vfxPrefab == null)
        {
            Debug.LogWarning($"[LPC Skills] Không thể sinh VFX vì Vfx Prefab đang để trống trên chiêu thức {data.skillName}!");
            yield break;
        }

        // Tính hướng và góc xoay đạn bay thẳng tuyệt đối (kèm bù trừ vfxRotationOffset + góc xoay mặc định của Prefab)
        Vector2 faceDir = player.lastDir.normalized;
        if (faceDir == Vector2.zero) faceDir = Vector2.down; // Fallback
        
        float prefabZRotation = data.vfxPrefab != null ? data.vfxPrefab.transform.rotation.eulerAngles.z : 0f;
        float angle = Mathf.Atan2(faceDir.y, faceDir.x) * Mathf.Rad2Deg + data.vfxRotationOffset + prefabZRotation;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // Tự động tính toán Spawn Offset cục bộ xoay theo hướng nhìn của nhân vật
        Vector2 rightDir = new Vector2(faceDir.y, -faceDir.x); // Hướng ngang (bên phải nhân vật)
        Vector3 spawnPos = transform.position + (Vector3)(faceDir * data.spawnOffset.y + rightDir * data.spawnOffset.x);
        spawnPos.z = transform.position.z;

        Debug.Log($"[LPC Skills Debug] Đang sinh VFX đơn lẻ: {data.vfxPrefab.name} tại vị trí: {spawnPos}, Góc xoay: {angle}");

        GameObject vfxObj = Instantiate(data.vfxPrefab, spawnPos, rotation);
        vfxObj.SetActive(true);

        vfxObj.transform.localScale = data.vfxPrefab.transform.localScale * data.vfxScale;

        if (data.projectileSpeed > 0f)
        {
            LPC_Projectile proj = vfxObj.GetComponent<LPC_Projectile>();
            if (proj == null) proj = vfxObj.AddComponent<LPC_Projectile>();
            proj.Initialize(player, faceDir, data);
        }
        else
        {
            LPC_VFXEffect effect = vfxObj.GetComponent<LPC_VFXEffect>();
            if (effect == null) effect = vfxObj.AddComponent<LPC_VFXEffect>();
            effect.Initialize(player, data);
        }
    }

    private System.Collections.IEnumerator SpawnSingleVfxConfigCoroutine(SkillData data, VfxConfig config)
    {
        if (config.vfxDelay > 0f)
        {
            yield return new WaitForSeconds(config.vfxDelay);
        }

        if (config.vfxPrefab == null) yield break;

        // Tính hướng và góc xoay đạn bay thẳng tuyệt đối (kèm bù trừ vfxRotationOffset + góc xoay mặc định của Prefab)
        Vector2 faceDir = player.lastDir.normalized;
        if (faceDir == Vector2.zero) faceDir = Vector2.down; // Fallback
        
        float prefabZRotation = config.vfxPrefab.transform.rotation.eulerAngles.z;
        float angle = Mathf.Atan2(faceDir.y, faceDir.x) * Mathf.Rad2Deg + config.vfxRotationOffset + prefabZRotation;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // Tự động tính toán Spawn Offset cục bộ xoay theo hướng nhìn của nhân vật
        Vector2 rightDir = new Vector2(faceDir.y, -faceDir.x);
        Vector3 spawnPos = transform.position + (Vector3)(faceDir * config.spawnOffset.y + rightDir * config.spawnOffset.x);
        spawnPos.z = transform.position.z;

        Debug.Log($"[LPC Skills] Sinh VFX nằm trong chuỗi: {config.effectName} ({config.vfxPrefab.name}) tại: {spawnPos}, Trễ: {config.vfxDelay}s");

        GameObject vfxObj = Instantiate(config.vfxPrefab, spawnPos, rotation);
        vfxObj.SetActive(true);

        // Nhân kích thước cấu hình (vfxScale) với kích thước gốc của Prefab
        vfxObj.transform.localScale = config.vfxPrefab.transform.localScale * config.vfxScale;

        if (config.projectileSpeed > 0f)
        {
            LPC_Projectile proj = vfxObj.GetComponent<LPC_Projectile>();
            if (proj == null) proj = vfxObj.AddComponent<LPC_Projectile>();
            proj.Initialize(player, faceDir, data, config.vfxScale, config.projectileSpeed, config.lifespan, config.vfxRotationOffset, config.vfxPrefab);
        }
        else
        {
            LPC_VFXEffect effect = vfxObj.GetComponent<LPC_VFXEffect>();
            if (effect == null) effect = vfxObj.AddComponent<LPC_VFXEffect>();
            effect.Initialize(player, data, config.vfxScale, data.attackRange, config.lifespan, config.vfxRotationOffset, config.vfxPrefab, config.spawnOffset);
        }
    }
}
