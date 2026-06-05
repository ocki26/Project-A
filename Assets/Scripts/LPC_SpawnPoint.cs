using UnityEngine;

public class LPC_SpawnPoint : MonoBehaviour
{
    [Tooltip("Tag định danh điểm spawn (Ví dụ: Town_Entrance, Dungeon_Start)")]
    public string spawnTag = "";

    private void OnDrawGizmos()
    {
        // Vẽ vòng tròn ngọc xanh lục trực quan trên Scene View để nhà thiết kế dễ kéo thả vị trí spawn
        Gizmos.color = new Color(0f, 0.85f, 0.45f, 0.75f);
        Gizmos.DrawSphere(transform.position, 0.4f);
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
