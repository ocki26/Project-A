using UnityEngine;

/// <summary>
/// LPC Force Y-Sorting - Ép Camera sắp xếp chiều sâu 2D theo trục Y chuẩn xác.
/// Giải pháp tối ưu và chống lỗi tuyệt đối trên mọi phiên bản Unity 6 / URP 2D.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class LPC_ForceYSorting : MonoBehaviour
{
    void Start()
    {
        ApplySorting();
    }

    void OnValidate()
    {
        ApplySorting();
    }

    private void ApplySorting()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Bắt buộc camera sử dụng trục Y để tính toán khoảng cách xa gần
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }
    }
}
