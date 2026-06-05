using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class LPC_InteractiveGrass : MonoBehaviour
{
    [Header("Sway animation (Optional)")]
    [Tooltip("Tốc độ rung lắc đung đưa")]
    public float swaySpeed = 4f;
    [Tooltip("Biên độ đung đưa nhẹ")]
    public float swayAmplitude = 0.05f;

    private Animator anim;
    private SpriteRenderer sr;
    private Vector3 originalScale;
    private float randomOffset;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        originalScale = transform.localScale;
        
        // Tạo offset ngẫu nhiên để các bụi cỏ đung đưa lệch nhịp nhau (nhìn tự nhiên hơn)
        randomOffset = Random.Range(0f, 100f);

        // Cấu hình Collider tự động làm Trigger để nhân vật đi xuyên qua được
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = new Vector2(0.6f, 0.4f); // Co nhỏ va chạm dưới chân
        }

        // Cấu hình Y-sorting Pivot sát đáy chân
        if (sr != null)
        {
            sr.sortingLayerName = "Entities";
            sr.sortingOrder = 0;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
        }
    }

    void Update()
    {
        // Cỏ đứng yên mặc định, chỉ phản hồi co giật (rustle) khi Player đi xuyên qua!
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Khi người chơi hoặc quái vật đi xuyên qua bụi cỏ
        if (other.CompareTag("Player") || other.CompareTag("Enemy") || other.gameObject.layer == LayerMask.NameToLayer("enemy"))
        {
            PlayRustleEffect();
        }
    }

    private void PlayRustleEffect()
    {
        // 1. Nếu có Animator hoạt ảnh vẽ tay (như cỏ rẽ đôi) -> Kích hoạt Trigger hoạt ảnh
        if (anim != null)
        {
            anim.SetTrigger("rustle");
        }
        else
        {
            // Nếu không có, ta co giật bụi cỏ nhẹ theo phương ngang để giả lập phản hồi vật lý nhanh
            StartCoroutine(RustleShakeCoroutine());
        }
    }

    private System.Collections.IEnumerator RustleShakeCoroutine()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float shakeX = Mathf.Sin(elapsed * 40f) * 0.12f;
            transform.localScale = new Vector3(originalScale.x + shakeX, originalScale.y, originalScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
    }
}
