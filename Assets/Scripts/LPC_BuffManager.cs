using UnityEngine;
using System.Collections.Generic;

public class LPC_BuffManager : MonoBehaviour
{
    public enum BuffType
    {
        None,
        Burn,
        Freeze,
        Bleed,
        Regeneration,
        Shield,
        WindWalk // Bộ pháp gió (AGI)
    }

    [System.Serializable]
    public class ActiveBuff
    {
        public BuffType type;
        public float duration;
        public float maxDuration;
        public float value; // Sát thương hoặc giá trị buff
        public float tickTimer;

        public ActiveBuff(BuffType t, float dur, float val = 0f)
        {
            type = t;
            duration = maxDuration = dur;
            value = val;
            tickTimer = 0f;
        }
    }

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private LPCPlayerController2 player;
    private EnemyAI enemy;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer; // Cache SpriteRenderer đầu ra tối ưu hóa 144 FPS

    private void Awake()
    {
        player = GetComponent<LPCPlayerController2>();
        enemy = GetComponent<EnemyAI>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>(); // Cache tại Awake
    }

    public void AddBuff(BuffType type, float duration, float value = 0f)
    {
        var existing = activeBuffs.Find(b => b.type == type);
        if (existing != null)
        {
            existing.duration = duration;
            existing.maxDuration = Mathf.Max(existing.maxDuration, duration);
            if (type == BuffType.Shield)
            {
                existing.value += value;
                if (player != null) player.currentShield = existing.value;
            }
            else
            {
                existing.value = value;
            }
        }
        else
        {
            var newBuff = new ActiveBuff(type, duration, value);
            activeBuffs.Add(newBuff);
            if (type == BuffType.Shield && player != null)
            {
                player.currentShield = value;
            }
            if (player != null) player.CalculateFinalStats();
        }
    }

    public bool HasBuff(BuffType type) => activeBuffs.Exists(b => b.type == type);

    public ActiveBuff GetBuff(BuffType type) => activeBuffs.Find(b => b.type == type);

    public List<ActiveBuff> GetAllBuffs() => activeBuffs;

    private void Update()
    {
        if (player != null && player.isDeadState) return;
        if (enemy != null && enemy.currentState == EnemyAI.State.Dead) return;

        // --- HIỆU ỨNG HÌNH ẢNH MÀU SẮC TRÊN SPRITE QUÁI VẬT / PLAYER ---
        if (_spriteRenderer != null)
        {
            if (HasBuff(BuffType.Burn))
            {
                // Màu cam lửa rực cháy nhấp nháy nhẹ sống động
                float pulse = 0.8f + Mathf.PingPong(Time.time * 5f, 0.2f);
                _spriteRenderer.color = new Color(1f * pulse, 0.45f * pulse, 0.2f, 1f);
            }
            else if (HasBuff(BuffType.Freeze))
            {
                // Màu xanh băng tuyết lạnh giá
                _spriteRenderer.color = new Color(0.4f, 0.7f, 1f, 1f);
            }
            else if (HasBuff(BuffType.Bleed))
            {
                // Màu đỏ thẫm xuất huyết nhấp nháy chậm biểu thị rỉ máu
                float pulse = 0.8f + Mathf.PingPong(Time.time * 2.5f, 0.15f);
                _spriteRenderer.color = new Color(0.9f * pulse, 0.15f * pulse, 0.15f * pulse, 1f);
            }
            else
            {
                _spriteRenderer.color = Color.white; // Trở lại bình thường
            }
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            buff.duration -= Time.deltaTime;

            // Burn: Trừ HP mỗi 0.5s bỏ qua giáp
            if (buff.type == BuffType.Burn)
            {
                buff.tickTimer += Time.deltaTime;
                if (buff.tickTimer >= 0.5f)
                {
                    buff.tickTimer = 0f;
                    int dmg = Mathf.RoundToInt(buff.value * 0.5f);
                    if (player != null)
                    {
                        player.TakeDamage(dmg, true);
                        SpawnFloatingDamageText(player.transform.position + Vector3.up * 1.2f, dmg, new Color(1f, 0.45f, 0.1f)); // Số màu cam cháy
                    }
                    else if (enemy != null)
                    {
                        enemy.TakeDamage(dmg, false, 9999f, false); // ignore defense/armor, do NOT trigger hurt/flinch animation
                        SpawnFloatingDamageText(enemy.transform.position + Vector3.up * 0.8f, dmg, new Color(1f, 0.45f, 0.1f)); // Số màu cam cháy
                    }
                }
            }
            // Bleed: Trừ HP mỗi 0.5s khi di chuyển
            else if (buff.type == BuffType.Bleed)
            {
                buff.tickTimer += Time.deltaTime;
                if (buff.tickTimer >= 0.5f)
                {
                    buff.tickTimer = 0f;
                    int dmg = Mathf.RoundToInt(buff.value * 0.5f);
                    if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.1f)
                    {
                        if (player != null)
                        {
                            player.TakeDamage(dmg, false);
                            SpawnFloatingDamageText(player.transform.position + Vector3.up * 1.2f, dmg, new Color(0.75f, 0.05f, 0.05f)); // Số màu đỏ thẫm
                        }
                        else if (enemy != null)
                        {
                            enemy.TakeDamage(dmg, false, 0f, false); // do NOT trigger hurt/flinch animation
                            SpawnFloatingDamageText(enemy.transform.position + Vector3.up * 0.8f, dmg, new Color(0.75f, 0.05f, 0.05f)); // Số màu đỏ thẫm
                        }
                    }
                }
            }

            if (buff.duration <= 0f)
            {
                activeBuffs.RemoveAt(i);
                if (buff.type == BuffType.Shield && player != null)
                {
                    player.currentShield = 0f;
                }
                if (player != null) player.CalculateFinalStats();
            }
        }
    }

    private void SpawnFloatingDamageText(Vector3 pos, int damage, Color color)
    {
        GameObject textObj = new GameObject("Runtime_BuffDamageText");
        textObj.transform.position = pos;
        TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 3.2f; // Nhỏ hơn sát thương gốc một chút để phân biệt

        LPC_FloatingText ft = textObj.AddComponent<LPC_FloatingText>();
        ft.Setup(damage.ToString(), color, 0.85f);
    }
}
