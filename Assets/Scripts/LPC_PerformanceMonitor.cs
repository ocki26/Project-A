using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// LPC Performance Monitor - Hệ thống đo chỉ số hiệu năng chuyên nghiệp (FPS, FrameTime, RAM, GPU, DrawCalls)
/// Được tối ưu hoàn toàn cho Unity 6 / URP 2D, hiển thị dạng HUD kính mờ (Glassmorphism) cực đẹp.
/// Bấm F3 (hoặc phím bạn chọn) để bật/tắt HUD khi chạy game.
/// </summary>
public class LPC_PerformanceMonitor : MonoBehaviour
{
    [Header("Cấu hình")]
    [Tooltip("Phím tắt để ẩn/hiện bảng đo chỉ số")]
    public KeyCode toggleKey = KeyCode.F3;
    
    [Tooltip("Có hiển thị bảng ngay khi khởi động game không")]
    public bool showOnStart = true;
    
    [Header("Giao diện (Aesthetics)")]
    public Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.9f);
    public Color textColor = new Color(0.9f, 0.92f, 0.95f, 1f);
    public Color accentColor = new Color(0.12f, 0.75f, 0.55f, 1f); // Màu xanh neon sang trọng

    private bool _isVisible;
    
    // FPS & Frame Time
    private float _fpsAccumulator = 0;
    private int _fpsCounter = 0;
    private float _fpsTimeLeft = 0.5f;
    private float _currentFps = 0f;
    private float _frameTimeMs = 0f;
    
    private float _minFps = 999f;
    private float _maxFps = 0f;
    private List<float> _fpsHistory = new List<float>();
    private const int MaxHistory = 70;
    
    // Các đối tượng GUI vẽ đồ họa
    private Texture2D _pixelTex;
    private GUIStyle _panelStyle;
    private GUIStyle _textStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _valueStyle;

    void Start()
    {
        _isVisible = showOnStart;
        
        // Tạo Texture 1x1 pixel màu trắng động để tô màu nền
        _pixelTex = new Texture2D(1, 1);
        _pixelTex.SetPixel(0, 0, Color.white);
        _pixelTex.Apply();
        
        // Giữ bộ đo chạy xuyên suốt mọi Scene (Singleton-like)
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        bool togglePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Key targetNewKey = Key.F3;
            switch (toggleKey)
            {
                case KeyCode.F1: targetNewKey = Key.F1; break;
                case KeyCode.F2: targetNewKey = Key.F2; break;
                case KeyCode.F3: targetNewKey = Key.F3; break;
                case KeyCode.F4: targetNewKey = Key.F4; break;
                case KeyCode.F5: targetNewKey = Key.F5; break;
                case KeyCode.F6: targetNewKey = Key.F6; break;
                case KeyCode.F7: targetNewKey = Key.F7; break;
                case KeyCode.F8: targetNewKey = Key.F8; break;
                case KeyCode.F9: targetNewKey = Key.F9; break;
                case KeyCode.F10: targetNewKey = Key.F10; break;
                case KeyCode.F11: targetNewKey = Key.F11; break;
                case KeyCode.F12: targetNewKey = Key.F12; break;
                case KeyCode.BackQuote: targetNewKey = Key.Backquote; break;
                case KeyCode.Space: targetNewKey = Key.Space; break;
                case KeyCode.Escape: targetNewKey = Key.Escape; break;
            }
            togglePressed = Keyboard.current[targetNewKey].wasPressedThisFrame;
        }
#else
        togglePressed = Input.GetKeyDown(toggleKey);
#endif

        if (togglePressed)
        {
            _isVisible = !_isVisible;
        }

        if (!_isVisible) return;

        // Tính toán thông số khung hình thực tế
        float deltaTime = Time.unscaledDeltaTime;
        _frameTimeMs = deltaTime * 1000f;
        
        _fpsTimeLeft -= deltaTime;
        _fpsAccumulator += 1f / deltaTime;
        _fpsCounter++;

        // Cập nhật chỉ số FPS trung bình sau mỗi 250ms
        if (_fpsTimeLeft <= 0f)
        {
            _currentFps = _fpsAccumulator / _fpsCounter;
            
            // Theo dõi Min/Max FPS sau khi game đã chạy ổn định (bỏ qua 2 giây đầu load game)
            if (Time.timeSinceLevelLoad > 2f)
            {
                if (_currentFps < _minFps) _minFps = _currentFps;
                if (_currentFps > _maxFps) _maxFps = _currentFps;
            }

            // Ghi nhận lịch sử FPS để vẽ biểu đồ
            _fpsHistory.Add(_currentFps);
            if (_fpsHistory.Count > MaxHistory)
            {
                _fpsHistory.RemoveAt(0);
            }

            _fpsTimeLeft = 0.25f;
            _fpsAccumulator = 0f;
            _fpsCounter = 0;
        }
    }

    private void OnGUI()
    {
        if (!_isVisible) return;

        InitStyles();

        // Định hình kích thước panel HUD (nằm góc trên bên phải màn hình)
        float panelWidth = 330f;
        float panelHeight = 445f;
        Rect rect = new Rect(Screen.width - panelWidth - 15f, 15f, panelWidth, panelHeight);
        
        // 1. Vẽ nền kính mờ tối màu (Background Panel)
        GUI.color = panelColor;
        GUI.DrawTexture(rect, _pixelTex);
        GUI.color = Color.white;
        
        // 2. Vẽ viền mỏng màu xanh lá cây Neon thời thượng
        DrawBorder(rect, new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f), 1);

        // 3. Khởi tạo vùng vẽ nội dung (Padding 15px)
        GUILayout.BeginArea(new Rect(rect.x + 15f, rect.y + 12f, rect.width - 30f, rect.height - 24f));
        
        // --- TIÊU ĐỀ ---
        GUILayout.Label("📊 SYSTEM PERFORMANCE", _titleStyle);
        DrawHorizontalLine();
        GUILayout.Space(6);

        // --- FPS & KHUNG HÌNH ---
        Color fpsColor = Color.green;
        if (_currentFps < 30f) fpsColor = Color.red;
        else if (_currentFps < 55f) fpsColor = Color.yellow;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Tốc độ khung hình (FPS):", _textStyle);
        GUILayout.FlexibleSpace();
        GUI.color = fpsColor;
        GUILayout.Label($"{_currentFps:F1} FPS", _valueStyle);
        GUI.color = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Thời gian dựng (FrameTime):", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{_frameTimeMs:F2} ms", _valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("FPS Min / Max:", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{(_minFps == 999f ? 0 : _minFps):F0} / {_maxFps:F0}", _valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        
        // Biểu đồ Sparkline FPS thời gian thực
        GUILayout.Label("Đồ thị FPS thời gian thực:", _textStyle);
        Rect graphRect = GUILayoutUtility.GetRect(300f, 55f);
        DrawGraph(graphRect);
        
        GUILayout.Space(10);

        // --- BỘ NHỚ RAM / DỮ LIỆU ---
        GUILayout.Label("💾 MEMORY HEAP", _titleStyle);
        DrawHorizontalLine();
        GUILayout.Space(6);

        long monoUsed = Profiler.GetMonoUsedSizeLong();
        long totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
        long gcMemory = System.GC.GetTotalMemory(false);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Mono Allocated Heap:", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(FormatBytes(monoUsed), _valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Total Reserved Memory:", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(FormatBytes(totalAlloc), _valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("GC Allocated Size:", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(FormatBytes(gcMemory), _valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // --- HỆ THỐNG DỰNG HÌNH GPU (Draw Calls) ---
        GUILayout.Label("🎮 RENDERING PIPELINE", _titleStyle);
        DrawHorizontalLine();
        GUILayout.Space(6);

#if UNITY_EDITOR
        GUILayout.BeginHorizontal();
        GUILayout.Label("Draw Calls (Batches):", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(UnityEditor.UnityStats.batches.ToString(), _valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("SetPass Calls:", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(UnityEditor.UnityStats.setPassCalls.ToString(), _valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Triangles / Vertices:", _textStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{FormatNumber(UnityEditor.UnityStats.triangles)} / {FormatNumber(UnityEditor.UnityStats.vertices)}", _valueStyle);
        GUILayout.EndHorizontal();
#else
        GUILayout.Label("Chỉ số Draw Calls / Triangles chỉ đọc được trong Editor.", _textStyle);
#endif

        GUILayout.Space(10);
        
        // --- THÔNG TIN THIẾT BỊ ---
        GUILayout.Label("🖥️ HARDWARE SPECIFICATIONS", _titleStyle);
        DrawHorizontalLine();
        GUILayout.Space(6);
        
        GUILayout.Label($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize}MB)", _textStyle);
        GUILayout.Label($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} Cores)", _textStyle);
        
        double refreshRate = 60;
#if UNITY_2022_2_OR_NEWER
        refreshRate = Screen.currentResolution.refreshRateRatio.value;
#else
        refreshRate = Screen.currentResolution.refreshRate;
#endif
        GUILayout.Label($"Screen: {Screen.width}x{Screen.height} @ {refreshRate:F0}Hz", _textStyle);

        GUILayout.EndArea();
    }

    // Vẽ biểu đồ FPS cột thời gian thực
    private void DrawGraph(Rect rect)
    {
        // Vẽ nền xám trong suốt cho biểu đồ
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(rect, _pixelTex);
        GUI.color = Color.white;
        DrawBorder(rect, new Color(1f, 1f, 1f, 0.15f), 1);

        if (_fpsHistory.Count == 0) return;

        float barWidth = rect.width / MaxHistory;
        float maxVal = 120f; // FPS tối đa tương ứng đỉnh biểu đồ

        for (int i = 0; i < _fpsHistory.Count; i++)
        {
            float val = _fpsHistory[i];
            float normalized = Mathf.Clamp01(val / maxVal);
            float barHeight = rect.height * normalized;

            Rect barRect = new Rect(
                rect.x + (i * barWidth),
                rect.y + rect.height - barHeight,
                barWidth - 1f,
                barHeight
            );

            // Đổi màu cột đồ thị dựa trên FPS hiện tại
            Color barColor = Color.green;
            if (val < 30f) barColor = Color.red;
            else if (val < 55f) barColor = Color.yellow;
            barColor.a = 0.75f;

            GUI.color = barColor;
            GUI.DrawTexture(barRect, _pixelTex);
        }
        GUI.color = Color.white;
    }

    private void InitStyles()
    {
        if (_panelStyle == null)
        {
            _panelStyle = new GUIStyle();
            
            _textStyle = new GUIStyle();
            _textStyle.normal.textColor = textColor;
            _textStyle.fontSize = 11;
            
            _valueStyle = new GUIStyle();
            _valueStyle.normal.textColor = textColor;
            _valueStyle.fontSize = 11;
            _valueStyle.fontStyle = FontStyle.Bold;

            _titleStyle = new GUIStyle();
            _titleStyle.normal.textColor = accentColor;
            _titleStyle.fontSize = 11;
            _titleStyle.fontStyle = FontStyle.Bold;
        }
    }

    private void DrawBorder(Rect rect, Color color, int thickness)
    {
        GUI.color = color;
        // Top
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _pixelTex);
        // Bottom
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), _pixelTex);
        // Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _pixelTex);
        // Right
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), _pixelTex);
        GUI.color = Color.white;
    }

    private void DrawHorizontalLine()
    {
        Rect lineRect = GUILayoutUtility.GetRect(300f, 1f);
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(lineRect, _pixelTex);
        GUI.color = Color.white;
    }

    private string FormatBytes(long bytes)
    {
        double mb = bytes / 1024d / 1024d;
        return $"{mb:F2} MB";
    }

    private string FormatNumber(int num)
    {
        if (num >= 1000000)
            return $"{(num / 1000000f):F1}M";
        if (num >= 1000)
            return $"{(num / 1000f):F1}K";
        return num.ToString();
    }
}
