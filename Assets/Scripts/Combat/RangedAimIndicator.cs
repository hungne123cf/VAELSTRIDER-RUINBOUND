using UnityEngine;

/// <summary>
/// Hệ thống Tia nhắm Laze (Aim Trajectory Indicator) cho nhân vật tầm xa & Lam Kiếm Sĩ (Azure Blade).
/// Vẽ tia laser định hướng sắc nét thanh thoát xuyên suốt tầm bắn, phát sáng rực rỡ khi tụ lực.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RangedAimIndicator : MonoBehaviour
{
    [Header("Aim Line Settings")]
    [SerializeField] private float maxLineLength = 8.5f;
    [SerializeField] private float baseLineWidth = 0.12f;
    [SerializeField] private float chargedLineWidth = 0.24f;
    [SerializeField] private Color baseColor = new Color(0.15f, 0.85f, 1f, 0.85f);
    [SerializeField] private Color fullChargeColor = new Color(0.9f, 1f, 1f, 1f);

    private LineRenderer lineRenderer;
    private bool isAiming = false;
    private float currentChargeRatio = 0f;
    private Vector2 currentAimDirection = Vector2.down;

    public bool IsAiming => isAiming;
    public float CurrentChargeRatio => currentChargeRatio;
    public Vector2 CurrentAimDirection => currentAimDirection;

    private static RangedAimIndicator instance;
    public static RangedAimIndicator Instance
    {
        get => instance != null ? instance : null;
        private set => instance = value;
    }

    public static RangedAimIndicator EnsureExists(GameObject playerObj)
    {
        if (playerObj == null) return null;
        var ind = playerObj.GetComponent<RangedAimIndicator>();
        if (ind == null) ind = playerObj.AddComponent<RangedAimIndicator>();
        Instance = ind;
        return ind;
    }

    void Awake()
    {
        Instance = this;
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        SetupLineRenderer();

        // Xóa sạch mọi targetReticle cũ nếu còn tồn tại
        Transform oldReticle = transform.Find("AimTargetReticle");
        if (oldReticle != null) Destroy(oldReticle.gameObject);

        Hide();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void SetupLineRenderer()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = 5300;

        // Tạo material phát sáng mượt mà
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        lineRenderer.material = mat;

        // Texture mode / Cap vertices
        lineRenderer.numCapVertices = 5;
        lineRenderer.numCornerVertices = 5;
    }

    public void Show(Vector2 origin, Vector2 direction, float chargeRatio)
    {
        isAiming = true;
        currentChargeRatio = Mathf.Clamp01(chargeRatio);
        currentAimDirection = direction.normalized;

        if (currentAimDirection.sqrMagnitude < 0.001f) currentAimDirection = Vector2.down;

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null) lineRenderer.enabled = true;

        // Raycast cản nếu gặp tường/chướng ngại vật
        float length = maxLineLength;
        RaycastHit2D hit = Physics2D.Raycast(origin, currentAimDirection, maxLineLength, LayerMask.GetMask("Obstacle", "Wall", "Default"));
        if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform.root))
        {
            length = Mathf.Max(0.5f, hit.distance);
        }

        Vector3 startPoint = (Vector3)origin + Vector3.up * 0.1f;
        Vector3 endPoint = startPoint + (Vector3)(currentAimDirection * length);

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

            // Hiệu ứng màu sắc và độ dày tia nhắm theo mức tụ lực
            float width = Mathf.Lerp(baseLineWidth, chargedLineWidth, currentChargeRatio);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width * 0.75f;

            Color activeColor = (currentChargeRatio >= 0.95f) 
                ? new Color(0.95f, 1f, 1f, 1f) 
                : Color.Lerp(baseColor, fullChargeColor, currentChargeRatio);

            lineRenderer.startColor = activeColor;
            lineRenderer.endColor = new Color(activeColor.r, activeColor.g, activeColor.b, 0.20f);
        }
    }

    public void Hide()
    {
        if (this == null) return;
        isAiming = false;
        currentChargeRatio = 0f;
        if (lineRenderer != null) lineRenderer.enabled = false;
    }
}
