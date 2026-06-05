using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "LPC/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    [TextArea(2, 5)]
    public string description;
    public Sprite icon;
    
    [Header("Resource Costs")]
    public float cooldown = 4f;
    public float manaCost = 0f;
    public float staminaCost = 10f;
    
    [Header("Animation & Weapon Requirements")]
    [Tooltip("Kiểu hoạt ảnh nhân vật sẽ thực hiện. Nếu chọn các hoạt ảnh vũ khí (như OneHand_Slash), người chơi bắt buộc phải trang bị vũ khí tương ứng mới có thể cast.")]
    public LPCPlayerController2.WeaponType animationType = LPCPlayerController2.WeaponType.Spell;
    
    [Header("Combat Stats")]
    public float damageMultiplier = 1.5f; 
    public bool isMagicDamage = true;     // TRUE: Tính theo MATK phép | FALSE: Tính theo ATK vật lý (Kiếm Khí...)
    public float criticalChanceModifier = 0f; // Cộng thêm tỉ lệ chí mạng riêng cho kỹ năng
    [Tooltip("Tầm đánh hoặc bán kính quét sát thương (m) của kỹ năng.")]
    public float attackRange = 1.5f;

    
    [Header("VFX Settings")]
    public GameObject vfxPrefab;          // Prefab hiệu ứng hoặc đạn bay
    public AudioClip sfxClip;             // Âm thanh phát ra khi cast chiêu

    public float vfxScale = 1.0f;         // Tùy chỉnh kích thước hiệu ứng (Scale)
    [Tooltip("Thời gian trễ (giây) trước khi sinh hiệu ứng VFX để đồng bộ với thời điểm vung kiếm/niệm chú trong hoạt ảnh.")]
    public float vfxDelay = 0.1f;
    [Tooltip("Góc xoay bù trừ thêm cho hiệu ứng VFX (tính bằng độ) để điều chỉnh hướng nhát chém/đạn khớp với hình vẽ gốc.")]
    public float vfxRotationOffset = 0f;
    public float projectileSpeed = 8f;     // Tốc độ bay thẳng tuyệt đối (đặt = 0 nếu là hiệu ứng tại chỗ)
    public float lifespan = 3f;           // Thời gian tự hủy tối đa
    public Vector3 spawnOffset = new Vector3(0f, 0.2f, 0f); // Tọa độ lệch tâm spawn từ player
    
    [Header("Status Effects (Buff/Debuff)")]
    public bool applyBuffDebuff = false;
    public LPC_BuffManager.BuffType buffType;
    public float buffDuration = 4f;
    public float buffValue = 15f;

    [Header("Multiple VFX Settings (Optional)")]
    [Tooltip("Nếu danh sách này có phần tử, hệ thống sẽ sử dụng danh sách này thay vì các trường VFX đơn lẻ ở trên! Cho phép kết hợp nhiều hiệu ứng với thời gian trễ và tồn tại khác nhau.")]
    public System.Collections.Generic.List<VfxConfig> vfxList = new System.Collections.Generic.List<VfxConfig>();

    [Tooltip("Thời gian khóa hành động/di chuyển của nhân vật khi cast skill này (giây). Đặt = 0 nếu muốn tự động mở khóa theo hoạt ảnh nhân vật.")]
    public float castLockDuration = 0f;
}

[System.Serializable]
public class VfxConfig
{
    [Tooltip("Tên gợi nhớ cho hiệu ứng này (ví dụ: Chém Lửa 1, Tiếng Vang).")]
    public string effectName = "VFX Effect";
    public GameObject vfxPrefab;
    public float vfxScale = 1.0f;
    [Tooltip("Thời gian trễ (giây) kể từ lúc bấm phím kích hoạt skill để sinh hiệu ứng này.")]
    public float vfxDelay = 0.1f;
    [Tooltip("Góc xoay bù trừ thêm cho hiệu ứng VFX (tính bằng độ).")]
    public float vfxRotationOffset = 0f;
    [Tooltip("Tốc độ bay thẳng tuyệt đối (đặt = 0 nếu là hiệu ứng chém tại chỗ hoặc buff).")]
    public float projectileSpeed = 8f;
    [Tooltip("Thời gian tồn tại tự hủy của hiệu ứng này.")]
    public float lifespan = 3f;
    [Tooltip("Tọa độ lệch tâm spawn từ player.")]
    public Vector3 spawnOffset = new Vector3(0f, 0.2f, 0f);
}
