using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// LPC Control Hub — Bảng điều khiển trung tâm quản lý luồng làm việc 5 bước của LPC Editor.
/// </summary>
public class LPCEditorHub : EditorWindow
{
    private GUIStyle headerStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle stepTitleStyle;
    private GUIStyle stepDescStyle;
    private GUIStyle cardStyle;
    private GUIStyle buttonStyle;
    private Vector2 scrollPos;

    [MenuItem("Tools/LPC Control Hub", false, 0)]
    public static void ShowWindow()
    {
        var window = GetWindow<LPCEditorHub>("LPC Control Hub");
        window.minSize = new Vector2(500, 620);
        window.titleContent = new GUIContent("LPC Control Hub", EditorGUIUtility.IconContent("d_UnityEditor.SceneView").image);
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.15f, 0.6f, 1f) }
        };

        subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
        };

        stepTitleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        stepDescStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            wordWrap = true,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        cardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(15, 15, 12, 12),
            margin = new RectOffset(10, 10, 5, 5)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30
        };
    }

    private void OnGUI()
    {
        InitStyles();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🎮 LPC Character Toolset Control Hub", headerStyle);
        EditorGUILayout.LabelField("Bảng điều khiển trung tâm quy trình làm việc chuẩn LPC", subtitleStyle);
        EditorGUILayout.Space(10);

        // Divider
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.Space(10);

        // Steps
        DrawStep(
            1,
            "Nhập Hoạt Ảnh & Cắt Spritesheet",
            "Công cụ xử lý ảnh thô của nhân vật (Body/Ears/Eyes/Underwear), cắt theo dòng và tạo các tệp hoạt ảnh .anim tương ứng.",
            "LPCAnimationImporter",
            () => LPCAnimationImporter.ShowWindow(),
            new Color(0.2f, 0.4f, 0.2f, 0.15f)
        );

        DrawStep(
            2,
            "Cài Đặt & Chẩn Đoán Nhân Vật",
            "Quét lỗi phân cấp đối tượng nhân vật, tự động sửa sorting layer/order và tạo cấu trúc phân cấp (Hierarchy) chuẩn LPC.",
            "LPCSetupFixer",
            () => LPCSetupFixer.ShowWindow(),
            new Color(0.4f, 0.2f, 0.2f, 0.15f)
        );

        DrawStep(
            3,
            "Tạo File Vật Phẩm Độc Lập",
            "Tạo các trang bị/vật phẩm ScriptableObject (LPCItemData), trích xuất và sinh SpriteLibraryAsset từ spritesheet trang bị của bạn.",
            "LPCItemCreator",
            () => LPCItemCreator.Open(),
            new Color(0.2f, 0.2f, 0.5f, 0.15f)
        );

        DrawStep(
            4,
            "Quản Lý Database Trang Bị (Mới)",
            "Quản lý toàn bộ vật phẩm trong dự án trên một giao diện lưới Excel-like. Chỉnh sửa nhanh chỉ số tấn công, phòng thủ, thuộc tính...",
            "LPCItemDatabaseManager",
            () => LPCItemDatabaseManager.Open(),
            new Color(0.4f, 0.3f, 0.1f, 0.15f)
        );

        DrawStep(
            5,
            "Ghép Trang Bị & Xem Trước (Composer)",
            "Ghép các trang bị lên người nhân vật, bake trực tiếp hoặc setup child animators. Đi kèm bộ xem trước hoạt ảnh chuyển động thời gian thực.",
            "LPCCharacterComposer",
            () => LPCCharacterComposer.ShowWindow(),
            new Color(0.3f, 0.1f, 0.4f, 0.15f)
        );

        EditorGUILayout.EndScrollView();
    }

    private void DrawStep(int stepNumber, string title, string description, string windowClass, System.Action openAction, Color bgColor)
    {
        GUI.backgroundColor = bgColor;
        EditorGUILayout.BeginVertical(cardStyle);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginHorizontal();
        
        // Step number indicator
        GUI.color = new Color(0.15f, 0.6f, 1f);
        EditorGUILayout.LabelField($"BƯỚC {stepNumber}", EditorStyles.boldLabel, GUILayout.Width(65));
        GUI.color = Color.white;

        // Title
        EditorGUILayout.LabelField(title, stepTitleStyle);
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        
        // Description
        EditorGUILayout.LabelField(description, stepDescStyle);

        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // Check if window is open
        bool isOpen = HasOpenWindow(windowClass);
        if (isOpen)
        {
            GUI.color = new Color(0.3f, 0.9f, 0.3f);
            EditorGUILayout.LabelField("● Đang mở", EditorStyles.miniBoldLabel, GUILayout.Width(75));
            GUI.color = Color.white;
        }

        if (GUILayout.Button($"Mở Trình {title.Split(' ')[0]}", buttonStyle, GUILayout.Width(180)))
        {
            openAction?.Invoke();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private bool HasOpenWindow(string className)
    {
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        return windows.Any(w => w.GetType().Name == className);
    }
}
