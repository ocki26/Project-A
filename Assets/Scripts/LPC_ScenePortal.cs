using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LPC_ScenePortal : MonoBehaviour
{
    [Header("Target Destination")]
    [Tooltip("Tên chính xác của Scene mục tiêu muốn load (Ví dụ: SampleScene, TownScene)")]
    public string targetSceneName = "";
    
    [Tooltip("Tag của điểm Spawn mục tiêu ở Scene mới muốn đặt Player vào")]
    public string targetSpawnTag = "";

    private void Start()
    {
        // Đảm bảo Collider là Trigger để đi xuyên qua và kích hoạt chuyển Scene
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem có phải là người chơi chạm vào cổng không
        if (other.CompareTag("Player") && !string.IsNullOrEmpty(targetSceneName))
        {
            if (LPC_TransitionManager.Instance != null)
            {
                LPC_TransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnTag);
                Debug.Log($"[Portal] Loading scene '{targetSceneName}' targeting spawn tag '{targetSpawnTag}'...");
            }
            else
            {
                // Fallback tự động tạo nếu quên thiết lập TransitionManager
                Debug.LogWarning("[Portal] LPC_TransitionManager not found in scene! Creating a temporary persistent instance...");
                GameObject managerObj = new GameObject("TransitionManager_RuntimeTemp");
                var mgr = managerObj.AddComponent<LPC_TransitionManager>();
                mgr.TransitionToScene(targetSceneName, targetSpawnTag);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Vẽ hình hộp màu cam vàng trong suốt trên Scene View đại diện cho cánh cổng chuyển cảnh
        Gizmos.color = new Color(1.0f, 0.65f, 0f, 0.45f);
        
        Collider2D col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(transform.position, box.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, new Vector3(1.2f, 1.2f, 1f));
        }
        
        Gizmos.color = Color.white;
    }
}
