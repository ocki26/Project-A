using UnityEngine;

/// <summary>
/// LPC Door — Quản lý cửa tương tác cho các căn nhà trong game.
/// Cho phép đóng/mở cửa, tự động bật/tắt vật cản (Collider) và thay đổi Sprite/Animation tương ứng.
/// </summary>
public class LPC_Door : MonoBehaviour
{
    [Header("Sprites & Visuals")]
    [Tooltip("Sprite hiển thị khi cửa đóng")]
    public Sprite closedSprite;
    [Tooltip("Sprite hiển thị khi cửa mở")]
    public Sprite openSprite;
    
    [Header("Components")]
    [Tooltip("SpriteRenderer hiển thị hình ảnh cửa")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Collider vật lý ngăn người chơi đi qua khi cửa đóng")]
    public BoxCollider2D physicalCollider;
    [Tooltip("Animator (tùy chọn) nếu cửa có hoạt ảnh chuyển động đóng/mở mượt mà")]
    public Animator animator;

    [Header("Settings")]
    [Tooltip("Nếu tích, cửa tự động mở khi nhân vật lại gần và đóng khi đi xa")]
    public bool autoOpen = true;
    [Tooltip("Từ khóa tag của nhân vật để kích hoạt mở cửa")]
    public string playerTag = "Player";
    [Tooltip("Trạng thái hiện tại của cửa")]
    public bool isOpen = false;

    private void Start()
    {
        // Tự động gán nếu chưa kéo trong Inspector
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (physicalCollider == null)
            physicalCollider = GetComponent<BoxCollider2D>();
        if (animator == null)
            animator = GetComponent<Animator>();

        UpdateDoorStateVisuals();
    }

    /// <summary>
    /// Chuyển đổi trạng thái đóng/mở cửa
    /// </summary>
    public void ToggleDoor()
    {
        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    /// <summary>
    /// Mở cửa
    /// </summary>
    public void OpenDoor()
    {
        isOpen = true;
        UpdateDoorStateVisuals();
    }

    /// <summary>
    /// Đóng cửa
    /// </summary>
    public void CloseDoor()
    {
        isOpen = false;
        UpdateDoorStateVisuals();
    }

    private void UpdateDoorStateVisuals()
    {
        // 1. Quản lý Collider vật lý (Mở cửa -> tắt va chạm để người chơi đi qua)
        if (physicalCollider != null)
        {
            physicalCollider.enabled = !isOpen;
        }

        // 2. Quản lý Animator
        if (animator != null)
        {
            animator.SetBool("IsOpen", isOpen);
        }
        else
        {
            // 3. Nếu không dùng Animator, đổi Sprite tĩnh
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = isOpen ? openSprite : closedSprite;
            }
        }
    }

    // Tự động đóng/mở khi có trigger vùng lại gần (nếu autoOpen = true)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (autoOpen && other.CompareTag(playerTag))
        {
            OpenDoor();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (autoOpen && other.CompareTag(playerTag))
        {
            CloseDoor();
        }
    }
}
