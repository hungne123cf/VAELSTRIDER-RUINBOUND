using UnityEngine;

/// <summary>
/// Định nghĩa phạm vi biên giới hạn (Bounding Box) của Bản đồ.
/// CameraFollow sẽ sử dụng vùng này để kẹp (clamp) khung nhìn (viewport),
/// ngăn chặn camera nhìn ra ngoài khoảng không hư vô đen khi người chơi đi đến rìa bản đồ.
/// </summary>
public class MapBounds : MonoBehaviour
{
    [Header("Cấu hình Biên Bản Đồ")]
    [Tooltip("Tự động tính toán biên từ sàn đấu / Tilemap / tường có trong Scene")]
    [SerializeField] private bool autoCalculate = true;

    [Tooltip("Góc dưới-trái của bản đồ (World Coordinates)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-16f, -11f);

    [Tooltip("Góc trên-phải của bản đồ (World Coordinates)")]
    [SerializeField] private Vector2 maxBounds = new Vector2(16f, 11f);

    [Header("Hiển thị Debug trong Scene View")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.4f, 0.4f);

    public Vector2 MinBounds => minBounds;
    public Vector2 MaxBounds => maxBounds;
    public bool HasValidBounds => (maxBounds.x > minBounds.x + 1f) && (maxBounds.y > minBounds.y + 1f);

    public static MapBounds Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (autoCalculate)
        {
            CalculateBounds();
        }
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }

    [ContextMenu("Recalculate Bounds")]
    public void CalculateBounds()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 1. Quy ước kích thước chuẩn cho các Map chính
        if (sceneName.Contains("Map3") || sceneName.Contains("ForgottenMemories"))
        {
            minBounds = new Vector2(-16f, -11f);
            maxBounds = new Vector2(16f, 11f);
            return;
        }
        else if (sceneName.Contains("Map2") || sceneName.Contains("ValleyRuin"))
        {
            minBounds = new Vector2(-20f, -14f);
            maxBounds = new Vector2(20f, 14f);
            return;
        }
        else if (sceneName.Contains("Map1") || sceneName.Contains("Thestart") || sceneName.Contains("SampleScene"))
        {
            // Map 1 Dungeon có nhiều phòng trải từ -17 đến 72, -9 đến 50
            minBounds = new Vector2(-17f, -9f);
            maxBounds = new Vector2(72f, 50f);
            return;
        }

        // 2. Tự động quét từ SpriteRenderer của Sàn đấu
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
            minBounds = totalBounds.min;
            maxBounds = totalBounds.max;
            return;
        }

        // 3. Tự động quét từ Tilemap
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
            minBounds = totalBounds.min;
            maxBounds = totalBounds.max;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Vector3 center = (minBounds + maxBounds) * 0.5f;
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
}
