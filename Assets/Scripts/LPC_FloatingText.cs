using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class LPC_FloatingText : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float fadeDuration = 0.8f;
    public Vector3 randomOffset = new Vector3(0.4f, 0.2f, 0f);

    private TextMeshPro tmp;
    private Color startColor;
    private float elapsed = 0f;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        startColor = tmp.color;

        // Tự động đẩy Sorting Layer và Sorting Order của MeshRenderer lên Characters/9999 để luôn hiển thị trên đầu
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Characters";
            meshRenderer.sortingOrder = 9999;
        }

        // Thêm một chút lệch ngẫu nhiên để các chữ số không đè khít lên nhau
        transform.position += new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y),
            0f
        );
    }

    public void Setup(string text, Color color, float sizeMultiplier = 1.0f)
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = color;
        startColor = color;
        tmp.fontSize = tmp.fontSize * sizeMultiplier;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // Bay từ từ lên trên
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // Hiệu ứng Fade out (Mờ dần)
        float pct = elapsed / fadeDuration;
        tmp.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, pct));

        // Tự hủy khi hết thời gian
        if (elapsed >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}
