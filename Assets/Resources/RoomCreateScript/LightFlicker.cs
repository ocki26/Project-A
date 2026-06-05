using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    [Header("Kéo TẤT CẢ các đèn cần nháy vào đây")]
    public Light2D[] targetLights; // Dấu [] tạo ra danh sách nhiều ô

    [Header("Cài đặt mức độ nhấp nháy")]
    [Tooltip("0.8 nghĩa là lúc tối nhất sẽ bằng 80% độ sáng gốc")]
    public float minFlickerPercent = 0.8f; 
    
    [Tooltip("1.2 nghĩa là lúc sáng nhất sẽ bằng 120% độ sáng gốc")]
    public float maxFlickerPercent = 1.2f; 
    
    public float flickerSpeed = 5.0f; // Tốc độ nhấp nháy

    private float[] baseIntensities; // Mảng lưu lại độ sáng ban đầu của từng đèn
    private float randomOffset;

    void Start()
    {
        randomOffset = Random.Range(0f, 100f);

        // Lưu lại độ sáng mặc định bạn đã setup trong Unity cho từng đèn
        baseIntensities = new float[targetLights.Length];
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
            {
                baseIntensities[i] = targetLights[i].intensity;
            }
        }
    }

    void Update()
    {
        // Tạo ra dải số bập bùng mượt mà từ 0 đến 1
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + randomOffset, 0f);

        // Tính ra tỷ lệ nhân (từ minFlickerPercent đến maxFlickerPercent)
        float currentMultiplier = Mathf.Lerp(minFlickerPercent, maxFlickerPercent, noise);

        // Áp dụng độ bập bùng cho TẤT CẢ các đèn trong danh sách
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
            {
                // Lấy độ sáng gốc NHÂN VỚI tỷ lệ bập bùng
                targetLights[i].intensity = baseIntensities[i] * currentMultiplier;
            }
        }
    }
}