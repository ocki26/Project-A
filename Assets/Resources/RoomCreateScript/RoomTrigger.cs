using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    // Kéo cái Collider bao quanh phòng vào đây trong Inspector
    public Collider2D roomBounds; 

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var camCtrl = FindObjectOfType<DungeonCameraController>();
            if (camCtrl != null && roomBounds != null)
            {
                // Truyền Collider bao quanh phòng vào Camera
                camCtrl.SwitchRoom(roomBounds, transform.parent.position);
            }
        }
    }
}