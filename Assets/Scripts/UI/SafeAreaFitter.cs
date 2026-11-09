using UnityEngine;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Mobile Safe Area Responsive Fitter
/// Tự động thích ứng giao diện với mọi loại màn hình di động:
/// - Chống tràn mép vào Tai thỏ (Notch), Nốt ruồi camera (Hole-punch), Viền bo cong (Curved corners)
/// - Tự động cập nhật khi xoay màn hình (LandscapeLeft <-> LandscapeRight) hoặc gập/mở màn hình gập (Foldables)
/// - Áp dụng cho các cụm nút điều khiển, thanh máu, và Minimap
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform panelRt;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        panelRt = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void OnEnable()
    {
        ApplySafeArea();
    }

    void Update()
    {
        // Kiểm tra biến đổi Safe Area hoặc Kích thước màn hình
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    public void ApplySafeArea()
    {
        if (panelRt == null) panelRt = GetComponent<RectTransform>();
        if (panelRt == null) return;

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Nếu máy không có notch (safeArea phủ 100% màn hình), giữ nguyên anchor chuẩn (0,0) đến (1,1)
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Giữ an toàn: không để anchor vượt ngoài [0..1]
        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        panelRt.anchorMin = anchorMin;
        panelRt.anchorMax = anchorMax;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Gắn SafeAreaFitter vào một GameObject cha bọc HUD trên Canvas để bảo vệ toàn bộ nút.
    /// </summary>
    public static SafeAreaFitter EnsureSafeAreaOn(GameObject target)
    {
        if (target == null) return null;
        var fitter = target.GetComponent<SafeAreaFitter>();
        if (fitter == null) fitter = target.AddComponent<SafeAreaFitter>();
        fitter.ApplySafeArea();
        return fitter;
    }
}

