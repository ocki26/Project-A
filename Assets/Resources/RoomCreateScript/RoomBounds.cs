using UnityEngine;

public class RoomBounds : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var camCtrl = FindObjectOfType<DungeonCameraController>();
            if (camCtrl != null)
            {
                // Gửi Collider này cho Camera
                camCtrl.SwitchRoom(GetComponent<Collider2D>(), transform.parent.position);
            }
        }
    }
}