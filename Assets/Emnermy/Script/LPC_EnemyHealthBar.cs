using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class LPC_EnemyHealthBar : MonoBehaviour
{
    private EnemyAI enemy;
    private SpriteRenderer enemyRenderer;

    private GameObject healthBarParent;
    private SpriteRenderer bgRenderer;
    private SpriteRenderer catchUpRenderer; // Thanh trượt trễ (Receding/Damage Lag bar) cực xịn
    private SpriteRenderer fgRenderer;

    [Header("Cấu hình Thanh máu")]
    [Tooltip("Khoảng cách lệch vị trí trên đầu quái")]
    public Vector3 offset = new Vector3(0f, 0.9f, 0f); 
    [Tooltip("Kích thước thanh máu (Rộng x Cao)")]
    public Vector2 size = new Vector2(0.8f, 0.10f);    
    [Tooltip("Ẩn thanh máu khi đầy máu")]
    public bool hideWhenFull = false; // Thiết lập false mặc định để quái đầy máu vẫn hiện rõ ràng cho người chơi quan sát!

    private float catchUpHpRatio = 1f; // Tỉ lệ máu của thanh trượt trễ
    private static Sprite _whitePixelSprite;

    void Start()
    {
        enemy = GetComponent<EnemyAI>();
        enemyRenderer = GetComponent<SpriteRenderer>();
        CreateHealthBar();
    }

    private Sprite GetWhiteSprite()
    {
        if (_whitePixelSprite != null) return _whitePixelSprite;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        // Giữ bộ lọc Point cho pixel perfect nghệ thuật
        tex.filterMode = FilterMode.Point;
        _whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _whitePixelSprite;
    }

    void CreateHealthBar()
    {
        // 1. Dọn dẹp triệt để bất kỳ Canvas UGUI cũ hoặc thanh máu cũ nào bị trùng lặp/sót lại (đặc biệt từ Prefab cũ hoặc khi nhân bản Slime con)
        Transform oldCanvas = transform.Find("EnemyHealthCanvas");
        if (oldCanvas != null)
        {
            Destroy(oldCanvas.gameObject);
        }

        Transform existingChild = transform.Find("Runtime_HealthBar");
        if (existingChild != null)
        {
            Destroy(existingChild.gameObject);
        }

        // Tự động dọn dẹp các thanh máu độc lập cũ trong Scene của quái này (nếu có) để tránh rác Scene
        GameObject oldIndependentBar = GameObject.Find("IndependentHealthBar_" + gameObject.GetInstanceID());
        if (oldIndependentBar != null)
        {
            Destroy(oldIndependentBar);
        }

        // 2. TẠO THANH MÁU CON (Parented to this Enemy)
        // Bằng cách làm con trực tiếp, thanh máu di chuyển 100% đồng bộ với quái vật, triệt tiêu hoàn toàn trễ hình (lag/jitter).
        // Chúng ta sẽ triệt tiêu méo mó tỷ lệ (scale) và hướng xoay (flip) ở LateUpdate bằng cách bù trừ scale nghịch đảo.
        healthBarParent = new GameObject("Runtime_HealthBar");
        healthBarParent.transform.SetParent(transform);
        healthBarParent.transform.localPosition = offset;
        healthBarParent.transform.localRotation = Quaternion.identity;
        healthBarParent.transform.localScale = Vector3.one;

        // Lấy sorting layer của quái để thanh máu vẽ chuẩn phối cảnh 2D
        string sortingLayer = enemyRenderer != null ? enemyRenderer.sortingLayerName : "Entities";
        int sortingOrder = enemyRenderer != null ? enemyRenderer.sortingOrder : 10;

        // 3. Tạo thanh Background (Màu nền đen viền mờ)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(healthBarParent.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localRotation = Quaternion.identity;
        bgObj.transform.localScale = new Vector3(size.x, size.y, 1f);

        bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = GetWhiteSprite();
        bgRenderer.color = new Color(0.08f, 0.08f, 0.08f, 0.85f); // Đen mờ obsidian cực xịn
        bgRenderer.sortingLayerName = sortingLayer;
        bgRenderer.sortingOrder = sortingOrder + 10; // Đặt vẽ lên trên quái vật

        // 4. Tạo thanh Catch-up (Thanh vàng trượt trễ khi mất máu)
        GameObject catchUpObj = new GameObject("CatchUp");
        catchUpObj.transform.SetParent(healthBarParent.transform);
        catchUpObj.transform.localPosition = Vector3.zero;
        catchUpObj.transform.localRotation = Quaternion.identity;
        catchUpObj.transform.localScale = new Vector3(size.x - 0.04f, size.y - 0.03f, 1f);

        catchUpRenderer = catchUpObj.AddComponent<SpriteRenderer>();
        catchUpRenderer.sprite = GetWhiteSprite();
        catchUpRenderer.color = new Color(0.95f, 0.80f, 0.20f, 1.0f); // Màu vàng cam sang trọng của RPG cổ điển
        catchUpRenderer.sortingLayerName = sortingLayer;
        catchUpRenderer.sortingOrder = sortingOrder + 11; // Nằm giữa BG và FG

        // 5. Tạo thanh Foreground (Máu đỏ tươi)
        GameObject fgObj = new GameObject("FG");
        fgObj.transform.SetParent(healthBarParent.transform);
        // Ban đầu vẽ đè hoàn toàn
        fgObj.transform.localPosition = Vector3.zero;
        fgObj.transform.localRotation = Quaternion.identity;
        fgObj.transform.localScale = new Vector3(size.x - 0.04f, size.y - 0.03f, 1f);

        fgRenderer = fgObj.AddComponent<SpriteRenderer>();
        fgRenderer.sprite = GetWhiteSprite();
        fgRenderer.color = new Color(0.85f, 0.15f, 0.15f, 1.0f); // Đỏ tươi rực cháy combat
        fgRenderer.sortingLayerName = sortingLayer;
        fgRenderer.sortingOrder = sortingOrder + 12; // Nằm trên cùng
    }

    void Update()
    {
        if (enemy == null || healthBarParent == null || fgRenderer == null || bgRenderer == null || catchUpRenderer == null) return;

        // Ẩn thanh máu hoàn toàn khi quái chết
        if (enemy.currentState == EnemyAI.State.Dead)
        {
            healthBarParent.SetActive(false);
            return;
        }

        // Tính toán tỷ lệ máu hiện tại
        float hpRatio = (float)enemy.CurrentHealth / enemy.maxHealth;
        hpRatio = Mathf.Clamp01(hpRatio);

        // Tự động ẩn thanh máu khi đầy HP (nếu bật hideWhenFull)
        if (hideWhenFull && hpRatio >= 0.99f)
        {
            healthBarParent.SetActive(false);
        }
        else
        {
            healthBarParent.SetActive(true);
        }

        // Cập nhật vị trí & tỷ lệ co giãn của thanh máu đỏ co từ phải sang trái (Left aligned)
        float maxFgWidth = size.x - 0.04f;
        float fgHeight = size.y - 0.03f;
        float currentFgWidth = maxFgWidth * hpRatio;

        fgRenderer.transform.localScale = new Vector3(currentFgWidth, fgHeight, 1f);
        // Math shift: Dịch tâm sang trái tỉ lệ thuận để giữ mép trái Bar cố định
        fgRenderer.transform.localPosition = new Vector3(-maxFgWidth * 0.5f + currentFgWidth * 0.5f, 0f, 0f);

        // Lerp mượt mà thanh màu vàng (Catch-up) đuổi theo thanh màu đỏ khi có biến động
        if (catchUpHpRatio > hpRatio)
        {
            catchUpHpRatio = Mathf.Lerp(catchUpHpRatio, hpRatio, Time.deltaTime * 3.5f); // Tốc độ trượt đuổi theo cực mượt
            if (catchUpHpRatio - hpRatio < 0.005f)
            {
                catchUpHpRatio = hpRatio;
            }
        }
        else
        {
            catchUpHpRatio = hpRatio; // Nếu hồi máu thì thanh vàng bắt kịp ngay
        }

        float currentCatchUpWidth = maxFgWidth * catchUpHpRatio;
        catchUpRenderer.transform.localScale = new Vector3(currentCatchUpWidth, fgHeight, 1f);
        catchUpRenderer.transform.localPosition = new Vector3(-maxFgWidth * 0.5f + currentCatchUpWidth * 0.5f, 0f, 0f);
    }

    // LATEUPDATE: Cập nhật tỷ lệ và hướng xoay của thanh máu để triệt tiêu hoàn toàn ảnh hưởng từ cha.
    // Vì thanh máu là con trực tiếp nên vị trí tự động cập nhật đồng bộ hoàn hảo bởi engine vật lý,
    // loại bỏ hoàn toàn 100% hiện tượng rung lắc/trễ hình (Jitter/Lag) khi di chuyển!
    void LateUpdate()
    {
        if (healthBarParent != null && enemy != null)
        {
            // Triệt tiêu hoàn toàn góc xoay của cha (luôn giữ thẳng đứng)
            healthBarParent.transform.rotation = Quaternion.identity;

            // Khôi phục kích thước thế giới chuẩn (không bị co giãn hay lật theo cha)
            Vector3 parentScale = transform.localScale;
            float scaleX = parentScale.x != 0 ? 1f / parentScale.x : 1f;
            float scaleY = parentScale.y != 0 ? 1f / parentScale.y : 1f;
            
            // Đặt localScale bù trừ nghịch đảo tỷ lệ của cha
            healthBarParent.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }

    // Hủy bỏ thanh máu độc lập trong Scene khi quái vật bị tiêu diệt
    void OnDestroy()
    {
        if (healthBarParent != null)
        {
            Destroy(healthBarParent);
        }
    }

    // Ẩn thanh máu khi quái vật bị ẩn (deactivate)
    void OnDisable()
    {
        if (healthBarParent != null)
        {
            healthBarParent.SetActive(false);
        }
    }

    // Hiện lại thanh máu khi quái vật được kích hoạt lại
    void OnEnable()
    {
        if (healthBarParent != null)
        {
            healthBarParent.SetActive(true);
        }
    }
}
