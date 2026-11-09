using UnityEngine;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Adaptive Camera Aspect Ratio Manager
/// Tự động điều chỉnh tầm nhìn của Camera cho mọi tỷ lệ màn hình (16:9, 19.5:9, 20:9, 21:9, Tablet 4:3):
/// - Trên máy tính bảng / màn hình gập (4:3, 3:2, 16:10): Tự động nâng nhẹ orthographicSize để không bị hụt tầm nhìn ngang.
/// - Trên điện thoại siêu dài (20:9, 21:9): Giữ nguyên chiều cao chuẩn, mở rộng góc nhìn 2 bên tạo cảm giác điện ảnh.
/// </summary>
[RequireComponent(typeof(Camera))]
public class AdaptiveCameraAspect : MonoBehaviour
{
    [Header("Aspect Settings")]
    [SerializeField] private float targetAspect = 16f / 9f; // Chuẩn vàng 16:9 (1.777f)
    [SerializeField] private float baseOrthographicSize = 6.5f;

    private Camera cam;
    private float lastScreenWidth = -1f;
    private float lastScreenHeight = -1f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic)
        {
            baseOrthographicSize = cam.orthographicSize;
        }
        UpdateCameraSize();
    }

    void Start()
    {
        UpdateCameraSize();
    }

    void LateUpdate()
    {
        if (Mathf.Abs(Screen.width - lastScreenWidth) > 1f || Mathf.Abs(Screen.height - lastScreenHeight) > 1f)
        {
            UpdateCameraSize();
        }
    }

    public void UpdateCameraSize()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (Screen.height <= 0) return;

        float currentAspect = (float)Screen.width / Screen.height;

        // Nếu màn hình vuông hơn chuẩn 16:9 (iPad 4:3, Fold 3:2, Laptop 16:10)
        // Bù kích thước dọc để bảo toàn 100% tầm nhìn ngang cho game thủ
        if (currentAspect < targetAspect)
        {
            float scaleFactor = targetAspect / currentAspect;
            cam.orthographicSize = baseOrthographicSize * scaleFactor;
        }
        else
        {
            // Màn hình dài hơn hoặc bằng 16:9 (18:9, 19.5:9, 20:9): giữ kích thước chuẩn
            cam.orthographicSize = baseOrthographicSize;
        }
    }

    /// <summary>
    /// Gắn AdaptiveCameraAspect vào Main Camera nếu chưa có.
    /// </summary>
    public static AdaptiveCameraAspect EnsureCameraAdaptive()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindAnyObjectByType<Camera>();
        if (mainCam == null) return null;

        var comp = mainCam.GetComponent<AdaptiveCameraAspect>();
        if (comp == null) comp = mainCam.gameObject.AddComponent<AdaptiveCameraAspect>();
        comp.UpdateCameraSize();
        return comp;
    }
}
