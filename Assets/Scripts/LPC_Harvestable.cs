using UnityEngine;

public class LPC_Harvestable : MonoBehaviour
{
    public enum ResourceType
    {
        CopperOre,
        IronOre,
        GoldOre,
        StoneDeposit,
        WildMushroom,
        MedicinalHerb,
        WildCrop
    }

    [Header("Resource Config")]
    public ResourceType resourceType = ResourceType.CopperOre;
    public LPCItemData lootItem;
    public int minLoot = 1;
    public int maxLoot = 3;

    [Header("Durability / Hits")]
    [Tooltip("Số lần đập/thu hoạch để vỡ/hái")]
    public int maxHits = 3;
    private int currentHits = 0;

    [Header("Visual Effects")]
    [Tooltip("Màu sắc tia pixel tóe ra khi bị đập trúng")]
    public Color hitParticleColor = new Color(0.7f, 0.4f, 0.2f);

    [Header("Components override")]
    [Tooltip("Kéo thả SpriteRenderer quặng tùy chỉnh (nếu trống sẽ tự tìm trên chính object này)")]
    public SpriteRenderer customSpriteRenderer;

    private SpriteRenderer sr;
    private Vector3 originalScale;

    void Start()
    {
        // Sử dụng SpriteRenderer tự gán trong Inspector, nếu trống thì tìm tự động trên GameObject
        sr = customSpriteRenderer != null ? customSpriteRenderer : GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        // Đảm bảo có Collider2D để vũ khí chém trúng quét được
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(0.8f, 0.8f);
        }

        // Tự động cấu hình Y-sorting Pivot chuẩn 2D
        if (sr != null)
        {
            // Chỉ bắt buộc set layer Entities và Order 0 khi đây là quặng tự sinh động (fallback)
            if (customSpriteRenderer == null && sr.sprite == null)
            {
                sr.sortingLayerName = "Entities";
                sr.sortingOrder = 0;
            }
            
            // Đảm bảo Sprite Sort Point luôn là Pivot để hệ thống Y-sorting 2.5D của Unity hoạt động
            sr.spriteSortPoint = SpriteSortPoint.Pivot;

            // CHỈ sinh hình vuông màu sắc pixel đại diện nếu thực sự chưa gán Sprite nào trong Inspector!
            if (sr.sprite == null)
            {
                Texture2D tex = new Texture2D(12, 12);
                Color[] cols = new Color[144];
                for (int i = 0; i < 144; i++) cols[i] = Color.white;
                tex.SetPixels(cols);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, 12f, 12f), new Vector2(0.5f, 0f), 16f); // Pivot at bottom!
                
                // Chỉ gán màu sắc phẳng làm đại diện cho khối vuông fallback
                sr.color = hitParticleColor;
            }
        }

        // Tự động gán màu sắc hạt đập quặng chuẩn theo phân loại nếu chưa chỉ định
        if (hitParticleColor == new Color(0.7f, 0.4f, 0.2f))
        {
            hitParticleColor = resourceType switch
            {
                ResourceType.CopperOre => new Color(0.85f, 0.45f, 0.25f), // Cam đồng
                ResourceType.IronOre => new Color(0.7f, 0.7f, 0.75f),     // Xám bạc sắt
                ResourceType.GoldOre => new Color(1.0f, 0.85f, 0.1f),     // Vàng kim
                ResourceType.StoneDeposit => new Color(0.5f, 0.5f, 0.5f), // Xám đá
                ResourceType.WildMushroom => new Color(0.9f, 0.2f, 0.2f), // Đỏ nấm rừng
                ResourceType.MedicinalHerb => new Color(0.2f, 0.8f, 0.3f),// Xanh thảo mộc
                ResourceType.WildCrop => new Color(0.95f, 0.6f, 0.1f),    // Cam ngô vàng
                _ => Color.white
            };
        }
    }

    // Hàm nhận sát thương trùng khớp với chữ ký của EnemyAI để tương thích chém thường của Player!
    public void TakeDamage(float amount, bool isMagicDamage = false, float penetration = 0f)
    {
        if (currentHits >= maxHits) return;

        currentHits++;

        // 1. Hiệu ứng rung bần bật và giật mình khi bị gõ trúng (Juicy feedback)
        StartCoroutine(HitShakeCoroutine());

        // 2. Bắn hạt pixel bắn tóe quặng theo màu đặc trưng
        SpawnHitParticles(transform.position, hitParticleColor, 12);

        // 3. Tạo chữ trôi nổi thể hiện số lần đập còn lại
        SpawnFloatingFeedbackText(transform.position + Vector3.up * 0.4f, $"{currentHits}/{maxHits}", hitParticleColor);

        // 4. Nếu đủ số lần đập, tiến hành vỡ và rớt đồ!
        if (currentHits >= maxHits)
        {
            Harvest();
        }
    }

    // Giữ overload tương thích sát thương int
    public void TakeDamage(int damageAmount)
    {
        TakeDamage((float)damageAmount, false, 0f);
    }

    private System.Collections.IEnumerator HitShakeCoroutine()
    {
        float duration = 0.15f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-0.06f, 0.06f);
            float offsetY = Random.Range(-0.06f, 0.06f);
            transform.localScale = originalScale + new Vector3(offsetX, offsetY, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void Harvest()
    {
        // 1. Sinh hạt vỡ tóe siêu lớn khi vỡ quặng hoàn toàn
        SpawnHitParticles(transform.position, hitParticleColor, 25);

        // 2. Spawn Loot rơi trên đất dạng hút nam châm
        if (lootItem != null)
        {
            int lootCount = Random.Range(minLoot, maxLoot + 1);
            for (int i = 0; i < lootCount; i++)
            {
                // Bắn hạt loot ra xung quanh gốc quặng nhẹ nhàng
                Vector3 spawnOffset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f), 0f);
                GameObject lootObj = new GameObject($"Loot_{lootItem.itemName}");
                lootObj.transform.position = transform.position + spawnOffset;
                
                LPC_ItemPickup pickup = lootObj.AddComponent<LPC_ItemPickup>();
                pickup.itemData = lootItem;
                pickup.count = 1;
            }
            
            Debug.Log($"[Harvest] {gameObject.name} broken! Spawned {lootCount}x {lootItem.itemName} drops!");
        }

        // Tự hủy quặng/cây
        Destroy(gameObject);
    }

    private void SpawnHitParticles(Vector3 pos, Color color, int count)
    {
        GameObject pObj = new GameObject("Runtime_HarvestParticles");
        pObj.transform.position = pos;
        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startColor = color;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.duration = 0.35f;
        main.loop = false;
        main.gravityModifier = 1.6f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.burstCount = 1;

        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
        bursts[0] = new ParticleSystem.Burst(0.0f, (short)count);
        emission.SetBursts(bursts);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        ps.Play();
    }

    private void SpawnFloatingFeedbackText(Vector3 pos, string text, Color color)
    {
        GameObject textObj = new GameObject("Runtime_HarvestFeedbackText");
        textObj.transform.position = pos;
        TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 3.8f;
        
        var ft = textObj.AddComponent<LPC_FloatingText>();
        ft.Setup(text, color, 0.8f);
    }
}
