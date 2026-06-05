using UnityEngine;

public class LPC_ItemPickup : MonoBehaviour
{
    [Header("Item Config")]
    public LPCItemData itemData;
    public int count = 1;

    [Header("Juicy Animations")]
    public float bobSpeed = 3f;
    public float bobAmplitude = 0.1f;
    public float rotateSpeed = 45f;

    [Header("Magnetism (Stardew Style)")]
    public float magnetRange = 2.2f;
    public float magnetSpeed = 6.0f;

    private Vector3 startPos;
    private Transform playerTransform;
    private bool isBeingAttracted = false;

    void Start()
    {
        startPos = transform.position;
        
        // Cố gắng lấy hình ảnh icon làm hình hiển thị vật phẩm rơi trên đất
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        
        if (itemData != null && itemData.icon != null)
        {
            sr.sprite = itemData.icon;
        }
        else
        {
            // Fallback: Tạo hình vuông màu sắc pixel đại diện
            Texture2D tex = new Texture2D(8, 8);
            Color[] cols = new Color[64];
            for (int i = 0; i < 64; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 16f);
            
            // Lấy màu sắc đặc trưng của quặng/thảo mộc làm màu pixel
            if (itemData != null)
            {
                sr.color = itemData.itemName.Contains("Copper") ? new Color(0.85f, 0.45f, 0.25f) :
                           itemData.itemName.Contains("Iron") ? new Color(0.7f, 0.7f, 0.75f) :
                           itemData.itemName.Contains("Gold") ? new Color(1.0f, 0.85f, 0.1f) :
                           itemData.itemName.Contains("Mushroom") ? new Color(0.9f, 0.2f, 0.2f) :
                           itemData.itemName.Contains("Herb") ? new Color(0.2f, 0.8f, 0.3f) :
                           new Color(0.95f, 0.6f, 0.1f);
            }
        }
        
        // Thiết lập Sorting Layer "Loot_Items" để nằm trên nền đất nhưng dưới chân nhân vật
        sr.sortingLayerName = "Loot_Items";
        sr.sortingOrder = 5;
        sr.drawMode = SpriteDrawMode.Simple;
        
        // Thu nhỏ kích thước hiển thị cho xinh xắn (0.4 - 0.5 là vừa vặn)
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        // Đảm bảo có Collider 2D dạng Trigger để nhặt đồ
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var cc = gameObject.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.4f;
        }

        // Tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (itemData == null) return;

        // 1. Hiệu ứng nhún nhảy (Bobbing) & xoay tròn nhẹ nhàng cực kỳ sinh động trên mặt đất
        if (!isBeingAttracted)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
        }
        else if (playerTransform != null)
        {
            // 2. Bay hút nam châm (Magnetism) về phía Player
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);
            transform.Rotate(Vector3.forward, rotateSpeed * 3f * Time.deltaTime); // Xoay tít mù khi bay về phía người chơi
        }

        // 3. Quét tầm hút nam châm
        if (!isBeingAttracted && playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= magnetRange)
            {
                isBeingAttracted = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PickUp(other.gameObject);
        }
    }

    private void PickUp(GameObject player)
    {
        var uiManager = FindObjectOfType<LPC_UI_Manager>();
        if (uiManager != null)
        {
            // Bảo toàn an toàn ScriptableObject bằng cách nhân bản động
            for (int i = 0; i < count; i++)
            {
                LPCItemData itemInstance = Instantiate(itemData);
                uiManager.inventory.Add(itemInstance);
            }

            // Đánh dấu bẩn để UI Grid tự động vẽ lại giao diện hòm đồ tức thời
            uiManager.GetType().GetMethod("MarkInventoryDirty", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(uiManager, null);
            
            // Hiện Floating Text màu xanh lá thông báo nhặt thành công
            var playerCtrl = player.GetComponent<LPCPlayerController2>();
            if (playerCtrl != null)
            {
                playerCtrl.SpawnDamageText(transform.position, 0, false); // Gọi nảy chữ số để đồng bộ class
                
                // Tự sinh ra một dòng chữ trôi nổi thông báo nhặt đồ "+X Tên Vật Phẩm"
                SpawnPickupFloatingText(transform.position, $"+{count} {itemData.itemName}", Color.green);
            }

            Destroy(gameObject);
        }
    }

    private void SpawnPickupFloatingText(Vector3 pos, string text, Color color)
    {
        GameObject textObj = new GameObject("Runtime_PickupText");
        textObj.transform.position = pos + Vector3.up * 0.5f;
        TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 4.2f;
        
        var ft = textObj.AddComponent<LPC_FloatingText>();
        ft.Setup(text, color, 1.2f);
    }
}
