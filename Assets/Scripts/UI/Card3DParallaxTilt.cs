using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Hiệu ứng Parallax 2.5D kết hợp Gyroscopic Tilt (Con quay hồi chuyển trên Mobile Android)
/// và Holographic Foil Shimmer (Ánh kim cầu vồng phản quang lấp lánh) chuẩn thẻ bài Dark Fantasy.
/// Tối ưu 60 FPS di động, Zero-GC Allocation trong Update, hỗ trợ song song cả Android Sensor và PC Mouse.
/// </summary>
[DisallowMultipleComponent]
public class Card3DParallaxTilt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tilt Configuration")]
    [Tooltip("Góc nghiêng xoay 3D tối đa của thẻ bài (độ)")]
    public float maxTiltAngleX = 10f;
    public float maxTiltAngleY = 12f;
    public float maxRollAngleZ = 2.5f;

    [Header("Parallax Configuration")]
    [Tooltip("Biên độ dịch chuyển thị sai 2.5D của bức tranh ngược chiều với góc nghiêng")]
    public float maxParallaxOffset = 14f;

    [Header("Smoothing")]
    public float tiltSmoothTime = 0.08f;

    [Header("References")]
    public RectTransform cardRect;
    public RectTransform illustrationRect;
    public Image holographicFoilImg;

    private static bool _gyroInitialized = false;
    private static bool _hasGyro = false;

    private Vector2 _currentTilt = Vector2.zero;
    private Vector2 _tiltVelocity = Vector2.zero;
    private Vector2 _targetTilt = Vector2.zero;

    private bool _isHovered = false;
    private Vector3 _originalIllustrationScale = Vector3.one;
    private Vector2 _originalIllustrationPos = Vector2.zero;
    private Quaternion _baseRotation = Quaternion.identity;

    private static Sprite _cachedHoloFoilSprite;

    private void Awake()
    {
        if (cardRect == null) cardRect = GetComponent<RectTransform>();
        _baseRotation = cardRect != null ? cardRect.localRotation : Quaternion.identity;

        InitSensorsOnce();

        if (illustrationRect != null)
        {
            _originalIllustrationPos = illustrationRect.anchoredPosition;
            // Phóng to nhẹ 8% để khi dịch chuyển Parallax không bao giờ bị hở mép viền
            illustrationRect.localScale = new Vector3(1.08f, 1.08f, 1f);
        }
    }

    private static void InitSensorsOnce()
    {
        if (_gyroInitialized) return;
        _gyroInitialized = true;

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            _hasGyro = true;
        }
        else
        {
            _hasGyro = false;
        }
    }

    public void Setup(RectTransform card, RectTransform illustration, Image holoFoil = null)
    {
        cardRect = card;
        illustrationRect = illustration;
        holographicFoilImg = holoFoil;

        if (cardRect != null) _baseRotation = cardRect.localRotation;
        if (illustrationRect != null)
        {
            _originalIllustrationPos = illustrationRect.anchoredPosition;
            illustrationRect.localScale = new Vector3(1.08f, 1.08f, 1f);
        }
    }

    private void OnDisable()
    {
        ResetToCenter();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }

    private void Update()
    {
        CalculateTargetTilt();

        // Làm mượt góc nghiêng bằng SmoothDamp (Zero allocation, phản hồi tự nhiên không giật)
        _currentTilt.x = Mathf.SmoothDamp(_currentTilt.x, _targetTilt.x, ref _tiltVelocity.x, tiltSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        _currentTilt.y = Mathf.SmoothDamp(_currentTilt.y, _targetTilt.y, ref _tiltVelocity.y, tiltSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        // 1. Áp dụng Xoay 3D (3D Gyroscopic Tilt) lên Thẻ Bài
        if (cardRect != null)
        {
            float rotX = -_currentTilt.y * maxTiltAngleX;
            float rotY = _currentTilt.x * maxTiltAngleY;
            float rotZ = -_currentTilt.x * maxRollAngleZ;

            cardRect.localRotation = _baseRotation * Quaternion.Euler(rotX, rotY, rotZ);
        }

        // 2. Áp dụng Parallax 2.5D (Dịch chuyển tranh ngược chiều góc nghiêng tạo chiều sâu vô cực)
        if (illustrationRect != null)
        {
            Vector2 parallaxDelta = new Vector2(-_currentTilt.x * maxParallaxOffset, -_currentTilt.y * maxParallaxOffset);
            illustrationRect.anchoredPosition = _originalIllustrationPos + parallaxDelta;
        }

        // 3. Holographic 2.5D Foil Shimmer (Lớp ánh kim quang học phản chiếu theo góc tilt)
        if (holographicFoilImg != null)
        {
            // Cường độ và vị trí ánh kim thay đổi theo góc nghiêng
            float tiltMag = _currentTilt.magnitude;
            float foilAlpha = Mathf.Clamp(0.08f + tiltMag * 0.28f, 0.05f, 0.40f);
            Color c = holographicFoilImg.color;
            c.a = foilAlpha;
            holographicFoilImg.color = c;

            // Dịch chuyển nhẹ lớp vệt sáng Holographic
            var holoRT = holographicFoilImg.rectTransform;
            holoRT.anchoredPosition = new Vector2(_currentTilt.x * 24f, _currentTilt.y * 24f);
        }
    }

    private void CalculateTargetTilt()
    {
        Vector2 sensorInput = Vector2.zero;
        bool hasSensorInput = false;

        // 1. Ưu tiên đọc Gyroscope / Accelerometer trên Android thiết bị thực
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_hasGyro && Input.gyro != null)
        {
            // Gyro gravity vector: nghiêng trái/phải -> x, nghiêng trên/dưới -> y
            Vector3 grav = Input.gyro.gravity;
            sensorInput = new Vector2(
                Mathf.Clamp(grav.x * 2.2f, -1f, 1f),
                Mathf.Clamp((grav.y + 0.6f) * 2.2f, -1f, 1f) // Cân bằng với tư thế cầm máy nghiêng 45-60 độ tự nhiên
            );
            hasSensorInput = true;
        }
        else
        {
            // Fallback sang Accelerometer
            Vector3 accel = Input.acceleration;
            sensorInput = new Vector2(
                Mathf.Clamp(accel.x * 2.0f, -1f, 1f),
                Mathf.Clamp((accel.y + 0.6f) * 2.0f, -1f, 1f)
            );
            hasSensorInput = true;
        }
#endif

        if (hasSensorInput)
        {
            _targetTilt = sensorInput;
            return;
        }

        // 2. Fallback trên PC / Unity Editor (Chuột / Mouse Hover hoặc Cursor Position)
        if (_isHovered && cardRect != null)
        {
            Vector2 mousePos = Input.mousePosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(cardRect, mousePos, null, out Vector2 localPoint))
            {
                Rect rect = cardRect.rect;
                float halfW = rect.width * 0.5f;
                float halfH = rect.height * 0.5f;

                if (halfW > 1f && halfH > 1f)
                {
                    float normX = Mathf.Clamp(localPoint.x / halfW, -1f, 1f);
                    float normY = Mathf.Clamp(localPoint.y / halfH, -1f, 1f);
                    _targetTilt = new Vector2(normX, normY);
                    return;
                }
            }
        }

        // Tự động hồi về cân bằng khi không tương tác
        _targetTilt = Vector2.zero;
    }

    private void ResetToCenter()
    {
        _currentTilt = Vector2.zero;
        _tiltVelocity = Vector2.zero;
        _targetTilt = Vector2.zero;
        if (cardRect != null) cardRect.localRotation = _baseRotation;
        if (illustrationRect != null) illustrationRect.anchoredPosition = _originalIllustrationPos;
    }

    /// <summary>
    /// Tạo Procedural Sprite lớp Holographic 2.5D Cầu Vồng Kim Tuyến lấp lánh
    /// </summary>
    public static Sprite GetHolographicFoilSprite()
    {
        if (_cachedHoloFoilSprite != null) return _cachedHoloFoilSprite;

        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] colors = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;

                // Góc chéo tia quang học Hologram (Diagonal Rainbow Caustic)
                float diag = (u * 1.5f + v * 0.8f);
                float hue = Mathf.Repeat(diag, 1f);

                // Màu cầu vồng quang học tinh tế
                Color rainbow = Color.HSVToRGB(hue, 0.75f, 1f);

                // Điểm sáng kim tuyến lấp lánh (Prismatic sparkles)
                float sparkle = Mathf.Pow(Mathf.Sin((u + v) * 28f) * 0.5f + 0.5f, 4f) * 0.35f;

                // Gradient mờ dần từ tâm ra 4 cạnh để ôm viền mềm mại
                float edgeMaskX = Mathf.SmoothStep(0f, 0.15f, u) * Mathf.SmoothStep(1f, 0.85f, u);
                float edgeMaskY = Mathf.SmoothStep(0f, 0.15f, v) * Mathf.SmoothStep(1f, 0.85f, v);
                float alpha = (0.25f + sparkle) * (edgeMaskX * edgeMaskY);

                colors[y * size + x] = new Color(rainbow.r, rainbow.g, rainbow.b, alpha);
            }
        }

        tex.SetPixels(colors);
        tex.Apply(false, true);

        _cachedHoloFoilSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _cachedHoloFoilSprite;
    }
}
