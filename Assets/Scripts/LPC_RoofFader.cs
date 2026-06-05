using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LPC Roof Fader — Tự động làm mờ mái nhà (nhiều lớp Sprite hoặc nhiều lớp Tilemap, GameObject con) khi nhân vật đi vào bên trong.
/// </summary>
public class LPC_RoofFader : MonoBehaviour
{
    [Header("Single References (Optional)")]
    [Tooltip("SpriteRenderer của mái nhà (nếu là ảnh đơn)")]
    public SpriteRenderer roofSpriteRenderer;
    [Tooltip("Tilemap của mái nhà (nếu vẽ bằng nhiều ô tile nhỏ)")]
    public Tilemap roofTilemap;

    [Header("Multiple References")]
    [Tooltip("Danh sách các Tilemap mái nhà sẽ cùng làm mờ")]
    public List<Tilemap> roofTilemaps = new List<Tilemap>();
    
    [Tooltip("Danh sách các SpriteRenderer mái nhà sẽ cùng làm mờ")]
    public List<SpriteRenderer> roofSpriteRenderers = new List<SpriteRenderer>();

    [Tooltip("Các GameObject khác muốn làm mờ (sẽ tự động tìm tất cả Tilemap/SpriteRenderer bên trong chúng)")]
    public List<GameObject> additionalObjectsToFade = new List<GameObject>();
    
    [Header("Settings")]
    [Tooltip("Độ trong suốt khi nhân vật đi vào trong nhà")]
    [Range(0f, 1f)]
    public float targetAlpha = 0.25f;
    [Tooltip("Thời gian chuyển đổi (giây)")]
    public float fadeDuration = 0.25f;

    private Coroutine fadeCoroutine;
    private List<SpriteRenderer> activeSprites = new List<SpriteRenderer>();
    private List<Tilemap> activeTilemaps = new List<Tilemap>();
    
    private List<float> originalSpriteAlphas = new List<float>();
    private List<float> originalTilemapAlphas = new List<float>();

    private void Start()
    {
        // 1. Thu thập các đối tượng đơn lẻ
        if (roofSpriteRenderer != null) activeSprites.Add(roofSpriteRenderer);
        if (roofTilemap != null) activeTilemaps.Add(roofTilemap);

        // 2. Thu thập từ danh sách Tilemaps
        if (roofTilemaps != null)
        {
            foreach (var tm in roofTilemaps)
            {
                if (tm != null && !activeTilemaps.Contains(tm)) activeTilemaps.Add(tm);
            }
        }

        // 3. Thu thập từ danh sách SpriteRenderers
        if (roofSpriteRenderers != null)
        {
            foreach (var sr in roofSpriteRenderers)
            {
                if (sr != null && !activeSprites.Contains(sr)) activeSprites.Add(sr);
            }
        }

        // 4. Thu thập từ các GameObject bổ sung (và tất cả con của chúng)
        if (additionalObjectsToFade != null)
        {
            foreach (var go in additionalObjectsToFade)
            {
                if (go == null) continue;
                
                var childTMs = go.GetComponentsInChildren<Tilemap>(true);
                foreach (var tm in childTMs)
                {
                    if (tm != null && !activeTilemaps.Contains(tm)) activeTilemaps.Add(tm);
                }

                var childSRs = go.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in childSRs)
                {
                    if (sr != null && !activeSprites.Contains(sr)) activeSprites.Add(sr);
                }
            }
        }

        // 5. Nếu chưa gán gì, tự động tìm kiếm thông minh từ cha của Trigger (Grid Template)
        if (activeSprites.Count == 0 && activeTilemaps.Count == 0)
        {
            var parent = transform.parent;
            if (parent != null)
            {
                foreach (Transform child in parent)
                {
                    string lowerName = child.name.ToLower();
                    if (lowerName.Contains("roof") || lowerName.Contains("overhead") || lowerName.Contains("chimney") || lowerName.Contains("ceiling"))
                    {
                        var tm = child.GetComponent<Tilemap>();
                        if (tm != null && !activeTilemaps.Contains(tm)) activeTilemaps.Add(tm);

                        var sr = child.GetComponent<SpriteRenderer>();
                        if (sr != null && !activeSprites.Contains(sr)) activeSprites.Add(sr);
                    }
                }
            }
        }

        // 6. Lưu lại độ trong suốt nguyên bản
        foreach (var sr in activeSprites)
        {
            originalSpriteAlphas.Add(sr.color.a);
        }
        foreach (var tm in activeTilemaps)
        {
            originalTilemapAlphas.Add(tm.color.a);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartFade(targetAlpha);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartFade(1f);
        }
    }

    private void StartFade(float targetFactor)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeRoutine(targetFactor));
    }

    private IEnumerator FadeRoutine(float targetFactor)
    {
        float elapsed = 0f;
        
        List<float> startSpriteAlphas = new List<float>();
        List<float> startTilemapAlphas = new List<float>();

        foreach (var sr in activeSprites) startSpriteAlphas.Add(sr.color.a);
        foreach (var tm in activeTilemaps) startTilemapAlphas.Add(tm.color.a);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // Xử lý các SpriteRenderer
            for (int i = 0; i < activeSprites.Count; i++)
            {
                if (activeSprites[i] == null) continue;
                Color color = activeSprites[i].color;
                float targetAlphaValue = originalSpriteAlphas[i] * targetFactor;
                color.a = Mathf.Lerp(startSpriteAlphas[i], targetAlphaValue, t);
                activeSprites[i].color = color;
            }

            // Xử lý các Tilemap
            for (int i = 0; i < activeTilemaps.Count; i++)
            {
                if (activeTilemaps[i] == null) continue;
                Color color = activeTilemaps[i].color;
                float targetAlphaValue = originalTilemapAlphas[i] * targetFactor;
                color.a = Mathf.Lerp(startTilemapAlphas[i], targetAlphaValue, t);
                activeTilemaps[i].color = color;
            }

            yield return null;
        }

        // Đảm bảo thiết lập chính xác giá trị đích ở frame cuối
        for (int i = 0; i < activeSprites.Count; i++)
        {
            if (activeSprites[i] == null) continue;
            Color color = activeSprites[i].color;
            color.a = originalSpriteAlphas[i] * targetFactor;
            activeSprites[i].color = color;
        }
        for (int i = 0; i < activeTilemaps.Count; i++)
        {
            if (activeTilemaps[i] == null) continue;
            Color color = activeTilemaps[i].color;
            color.a = originalTilemapAlphas[i] * targetFactor;
            activeTilemaps[i].color = color;
        }
    }
}
