using UnityEngine;
using Unity.Cinemachine;

public class DungeonCameraController : MonoBehaviour
{
    [Header("Cài đặt Camera")]
    public CinemachineCamera vCam;
    public CinemachineConfiner2D confiner;

    [Header("Tùy chỉnh độ mượt")]
    [Tooltip("Tốc độ chuyển phòng. Số càng nhỏ càng chậm và mượt.")]
    [Range(0.1f, 10f)]
    public float smoothSpeed = 2.0f;

    private Collider2D targetBounds;
    private bool isSwitching = false;

    void Awake()
    {
        // Tự tìm nếu chưa gán
        if (vCam == null) vCam = GetComponent<CinemachineCamera>();
        if (confiner == null) confiner = GetComponent<CinemachineConfiner2D>();
    }

    public void SwitchRoom(Collider2D newBounds, Vector3 roomPosition)
    {
        if (confiner == null) return;

        // Cập nhật vùng giới hạn
        confiner.BoundingShape2D = newBounds;
        targetBounds = newBounds;
        isSwitching = true;
    }

    void LateUpdate()
    {
        // Bạn có thể tùy chỉnh thêm logic ở đây nếu muốn camera 'trôi' 
        // Thay vì nhảy lập tức vào phạm vi confiner.
    }
}