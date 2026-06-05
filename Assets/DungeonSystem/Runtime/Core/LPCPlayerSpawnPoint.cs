using UnityEngine;

namespace DungeonSystem.Core
{
    [DisallowMultipleComponent]
    public class LPCPlayerSpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            // Vẽ một hình tròn màu xanh ngọc biểu thị điểm xuất hiện của người chơi trên Editor
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);
            Gizmos.DrawSphere(transform.position, 0.4f);
            
            // Vẽ mũi tên chỉ hướng đi xuống dưới biểu thị hướng đứng ban đầu
            Gizmos.color = Color.cyan;
            Vector3 direction = Vector3.down * 0.6f;
            Gizmos.DrawLine(transform.position, transform.position + direction);
            
            // Vẽ đầu mũi tên nhỏ
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
            Gizmos.DrawRay(transform.position + direction, right * 0.2f);
            Gizmos.DrawRay(transform.position + direction, left * 0.2f);
        }
    }
}
