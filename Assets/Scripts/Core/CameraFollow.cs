using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Mục tiêu bám theo")]
    [SerializeField] private Transform target;

    [Header("Độ mượt mà của Camera (Càng nhỏ càng bám nhanh)")]
    [Range(0.01f, 1f)]
    [SerializeField] private float smoothSpeed = 0.2f;

    [Header("Khoảng cách nhìn")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Giới Hạn Biên Bản Đồ (Map Boundaries Clamp)")]
    [Tooltip("Khóa khung nhìn Camera không bao giờ lọt ra ngoài mép bản đồ tạo khoảng đen")]
    [SerializeField] private bool enableBoundsClamp = true;

    [Tooltip("Tự động dò tìm biên giới hạn của bản đồ hiện tại")]
    [SerializeField] private bool autoDetectBounds = true;

    [SerializeField] private Vector2 minBounds = new Vector2(-16f, -11f);
    [SerializeField] private Vector2 maxBounds = new Vector2(16f, 11f);

    public static CameraFollow Instance { get; private set; }

    private Camera cam;
    private float defaultOrthoSize = 5f;
    private float targetOrthoSize = 5f;
    private float zoomVelocity = 0f;
    private float currentZoomSmoothTime = 0.35f;

    private Vector3 defaultOffset;
    private Vector3 targetOffset;
    private Vector3 offsetVelocity = Vector3.zero;
    private Vector3 velocity = Vector3.zero;

    public bool EnableBoundsClamp
    {
        get => enableBoundsClamp;
        set => enableBoundsClamp = value;
    }

    public Vector2 MinBounds => minBounds;
    public Vector2 MaxBounds => maxBounds;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultOrthoSize = cam.orthographicSize;
            // Đổi background color sang tone màu thiên nhiên/rừng tối thay vì đen ngòm
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.11f, 1.0f);
        }
        targetOrthoSize = defaultOrthoSize;

        defaultOffset = offset;
        targetOffset = offset;

        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
            else
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) target = pc.transform;
            }
        }

        if (autoDetectBounds)
        {
            AutoDetectMapBounds();
        }
    }

    void Start()
    {
        if (autoDetectBounds)
        {
            AutoDetectMapBounds();
        }
    }

    void LateUpdate()
    {
        // Smooth Zoom hỗ trợ cả Unscaled Time (khi ngưng đọng thời gian Ulti hoặc chuyển tầng)
        if (cam != null && Mathf.Abs(cam.orthographicSize - targetOrthoSize) > 0.005f)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetOrthoSize, ref zoomVelocity, currentZoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        // Smooth Offset khi lên đài cao
        offset = Vector3.SmoothDamp(offset, targetOffset, ref offsetVelocity, currentZoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // 1. Kẹp điểm đích theo biên bản đồ để camera giảm tốc và dừng lại êm ái ở mép map
        if (enableBoundsClamp)
        {
            desiredPosition = ClampPositionToBounds(desiredPosition);
        }

        // 2. Nội suy vị trí máy quay
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed, Mathf.Infinity, Time.unscaledDeltaTime);

        // 3. Khóa cứng một lần nữa vị trí thực tế để triệt tiêu mọi sai số / overshoot
        if (enableBoundsClamp)
        {
            transform.position = ClampPositionToBounds(transform.position);
        }
    }

    /// <summary>
    /// Giới hạn tọa độ camera sao cho toàn bộ khung nhìn (Viewport) nằm trọn vẹn trong [minBounds, maxBounds].
    /// Không để lộ bất kỳ khoảng đen hư vô nào bên ngoài bản đồ.
    /// </summary>
    public Vector3 ClampPositionToBounds(Vector3 pos)
    {
        if (!enableBoundsClamp || cam == null) return pos;

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        float minX = minBounds.x + horzExtent;
        float maxX = maxBounds.x - horzExtent;
        float minY = minBounds.y + vertExtent;
        float maxY = maxBounds.y - vertExtent;

        // Nếu bản đồ hẹp hơn cả chiều ngang màn hình thì cố định ở giữa
        float clampedX = (minX > maxX) ? (minBounds.x + maxBounds.x) * 0.5f : Mathf.Clamp(pos.x, minX, maxX);
        // Nếu bản đồ thấp hơn cả chiều dọc màn hình thì cố định ở giữa
        float clampedY = (minY > maxY) ? (minBounds.y + maxBounds.y) * 0.5f : Mathf.Clamp(pos.y, minY, maxY);

        return new Vector3(clampedX, clampedY, pos.z);
    }

    public void SetMapBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        enableBoundsClamp = true;
    }

    /// <summary>
    /// Tự động phát hiện biên bản đồ cho Map 1, Map 2, Map 3 hoặc bất kỳ Map nào.
    /// </summary>
    [ContextMenu("Auto Detect Map Bounds")]
    public void AutoDetectMapBounds()
    {
        // 1. Nếu có component MapBounds trong scene
        var mb = FindAnyObjectByType<MapBounds>();
        if (mb != null && mb.HasValidBounds)
        {
            SetMapBounds(mb.MinBounds, mb.MaxBounds);
            return;
        }

        // 2. Kiểm tra theo tên Scene
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Map3") || sceneName.Contains("ForgottenMemories"))
        {
            // Map 3: Sàn đấu 32m x 22m (từ -16 đến +16, -11 đến +11)
            SetMapBounds(new Vector2(-16f, -11f), new Vector2(16f, 11f));
            return;
        }
        else if (sceneName.Contains("Map2") || sceneName.Contains("ValleyRuin"))
        {
            // Map 2: Thung lũng từ -20 đến 20, -14 đến 14
            SetMapBounds(new Vector2(-20f, -14f), new Vector2(20f, 14f));
            return;
        }
        else if (sceneName.Contains("SampleScene"))
        {
            // Map 1: Hầm ngục nhiều phòng từ -17 đến 72, -9 đến 50
            SetMapBounds(new Vector2(-17f, -9f), new Vector2(72f, 50f));
            return;
        }

        // 3. Dò tìm SpriteRenderer sàn đấu
        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        Bounds totalBounds = new Bounds();
        bool found = false;

        foreach (var sr in renderers)
        {
            string n = sr.gameObject.name.ToLower();
            if (n.Contains("arenafloor") || n.Contains("floor_moss") || n.Contains("ground_floor"))
            {
                if (!found)
                {
                    totalBounds = sr.bounds;
                    found = true;
                }
                else
                {
                    totalBounds.Encapsulate(sr.bounds);
                }
            }
        }

        if (found)
        {
            SetMapBounds(totalBounds.min, totalBounds.max);
            return;
        }

        // 4. Dò tìm Tilemap
        var tilemaps = FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None);
        foreach (var tm in tilemaps)
        {
            string n = tm.gameObject.name.ToLower();
            if (n.Contains("ground") || n.Contains("floor"))
            {
                tm.CompressBounds();
                if (tm.localBounds.size.x > 3f && tm.localBounds.size.y > 3f)
                {
                    if (!found)
                    {
                        totalBounds = tm.localBounds;
                        found = true;
                    }
                    else
                    {
                        totalBounds.Encapsulate(tm.localBounds);
                    }
                }
            }
        }

        if (found)
        {
            SetMapBounds(totalBounds.min, totalBounds.max);
        }
    }

    public void SetCinematicZoom(float orthoSize)
    {
        currentZoomSmoothTime = 0.12f;
        targetOrthoSize = orthoSize;
    }

    public void ResetZoom()
    {
        currentZoomSmoothTime = 0.25f;
        targetOrthoSize = defaultOrthoSize;
    }

    public void SetElevationView(float orthoSize, Vector2 extraOffset, float smoothTime = 0.5f)
    {
        currentZoomSmoothTime = smoothTime;
        targetOrthoSize = orthoSize;
        targetOffset = defaultOffset + new Vector3(extraOffset.x, extraOffset.y, 0f);
    }

    public void ResetElevationView(float smoothTime = 0.5f)
    {
        currentZoomSmoothTime = smoothTime;
        targetOrthoSize = defaultOrthoSize;
        targetOffset = defaultOffset;
    }

    public void Shake(float duration = 0.15f, float magnitude = 0.2f)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private System.Collections.IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Vector3 nextPos = new Vector3(transform.position.x + x, transform.position.y + y, originalPos.z);
            if (enableBoundsClamp)
            {
                nextPos = ClampPositionToBounds(nextPos);
            }
            transform.position = nextPos;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
