using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LPC_TransitionManager : MonoBehaviour
{
    public static LPC_TransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    public float fadeDuration = 0.4f;

    private CanvasGroup fadeCanvasGroup;
    private bool isTransitioning = false;
    private string targetSpawnTag = "";

    private void Awake()
    {
        // Cơ chế Singleton để chỉ duy trì duy nhất 1 bộ chuyển cảnh toàn game
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        // Tự động tạo Canvas và Image để làm hiệu ứng Fade to Black cực xịn AAA tại Runtime
        GameObject canvasObj = new GameObject("TransitionFadeCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Luôn vẽ trên cùng hết mọi thứ

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image img = imageObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;

        // Giãn căng đều màn hình
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f; // Bắt đầu trong suốt
        fadeCanvasGroup.blocksRaycasts = false;
    }

    public void TransitionToScene(string sceneName, string spawnTag)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCoroutine(sceneName, spawnTag));
    }

    private IEnumerator TransitionCoroutine(string sceneName, string spawnTag)
    {
        isTransitioning = true;
        fadeCanvasGroup.blocksRaycasts = true;

        // 1. Fade out (Màn hình tối dần)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // BẢO TOÀN PLAYER: Giữ lại nhân vật cùng với cấp độ, máu, hòm đồ nguyên vẹn khi qua màn!
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            DontDestroyOnLoad(playerObj);
        }

        targetSpawnTag = spawnTag;

        // 2. Load Scene mới một cách bất đồng bộ
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // TỰ ĐỘNG KHỬ DUPLICATE PLAYER:
        // Khi quay lại TownScene, Unity sẽ sinh ra Player pre-placed mới.
        // Chúng ta quét và hủy ngay lập tức bản sao mới này, giữ lại duy nhất Player gốc mang theo hòm đồ!
        if (playerObj != null)
        {
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in allPlayers)
            {
                if (p != playerObj)
                {
                    Destroy(p); // Tiêu diệt bản sao trùng lặp trong scene mới
                }
            }
        }

        // 3. Dịch chuyển Player đến vị trí Spawn Point có Tag khớp
        PositionPlayerAtSpawnPoint();

        // 4. Fade in (Màn hình sáng dần)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;

        fadeCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private void PositionPlayerAtSpawnPoint()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        // Tìm điểm spawn có tag khớp trong Scene mới
        LPC_SpawnPoint[] spawnPoints = FindObjectsOfType<LPC_SpawnPoint>();
        LPC_SpawnPoint targetPoint = null;

        foreach (var point in spawnPoints)
        {
            if (point.spawnTag == targetSpawnTag)
            {
                targetPoint = point;
                break;
            }
        }

        if (targetPoint != null)
        {
            playerObj.transform.position = targetPoint.transform.position;
            Debug.Log($"[Transition] Player positioned at spawn point '{targetSpawnTag}'!");
        }
        else if (spawnPoints.Length > 0)
        {
            // Fallback sang spawn point đầu tiên tìm thấy
            playerObj.transform.position = spawnPoints[0].transform.position;
            Debug.LogWarning($"[Transition] Spawn point '{targetSpawnTag}' not found. Positioned at first available spawn point.");
        }
        
        // Đưa Camera chính về ngay vị trí Player để tránh bị trượt giật camera khi vừa load map
        if (Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            Camera.main.transform.position = new Vector3(playerObj.transform.position.x, playerObj.transform.position.y, camPos.z);
        }
    }
}
