using UnityEngine;

public class LPC_GrassMassSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("Prefab bụi cỏ động muốn sinh")]
    public GameObject grassPrefab;
    
    [Tooltip("Số lượng bụi cỏ muốn sinh")]
    public int grassCount = 100;
    
    [Tooltip("Kích thước vùng sinh cỏ (Rộng x Cao)")]
    public Vector2 spawnArea = new Vector2(15f, 15f);

    [Header("Obstacle Prevention (Optional)")]
    [Tooltip("Tránh sinh cỏ đè lên các chướng ngại vật cứng (đá, tường, hàng rào)")]
    public bool avoidObstacles = true;
    [Tooltip("Chọn layer cản đường (ví dụ: Default hoặc Obstacles)")]
    public LayerMask obstacleLayer;

    // Nút bấm tiện ích chuột phải hiển thị trong Inspector
    [ContextMenu("🌱 Sinh Nhanh Cánh Đồng Cỏ (Mass Spawn Grass)")]
    public void SpawnGrassField()
    {
        if (grassPrefab == null)
        {
            Debug.LogError("[Grass Spawner] Vui lòng gán Prefab ngọn cỏ động vào ô 'Grass Prefab' trước!");
            return;
        }

        int spawnedCount = 0;
        int maxAttempts = grassCount * 5; // Tránh treo máy nếu không tìm được vị trí trống
        int attempts = 0;

        while (spawnedCount < grassCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Lấy vị trí ngẫu nhiên trong vùng hình chữ nhật
            float rx = Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f);
            float ry = Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f);
            Vector3 spawnPos = transform.position + new Vector3(rx, ry, 0f);

            // Kiểm tra va chạm để tránh sinh đè lên đá cản, ao nước hay tường
            if (avoidObstacles)
            {
                Collider2D hit = Physics2D.OverlapCircle(spawnPos, 0.25f, obstacleLayer);
                if (hit != null) continue;
            }

            // Sinh ngọn cỏ động giữ nguyên liên kết Prefab gốc
#if UNITY_EDITOR
            GameObject grassInstance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(grassPrefab);
            grassInstance.transform.position = spawnPos;
            grassInstance.transform.SetParent(transform);
#else
            GameObject grassInstance = Instantiate(grassPrefab, spawnPos, Quaternion.identity, transform);
#endif
            spawnedCount++;
        }

        Debug.Log($"[Grass Spawner] Đã sinh thành công {spawnedCount} bụi cỏ động!");
    }

    [ContextMenu("🧹 Dọn Sạch Cỏ (Clear All Grass)")]
    public void ClearGrassField()
    {
        // Xóa sạch các con trực thuộc lập tức trong Editor
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        Debug.Log("[Grass Spawner] Đã dọn dẹp sạch sẽ cỏ!");
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ khung hộp màu xanh lục trực quan biểu thị vùng sinh cỏ trên Scene View
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.25f);
        Gizmos.DrawCube(transform.position, new Vector3(spawnArea.x, spawnArea.y, 1f));
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, spawnArea.y, 1f));
    }
}
