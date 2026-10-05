using UnityEngine;

/// <summary>
/// Hệ thống Tia nhắm Laze (Aim Trajectory Indicator) cho nhân vật tầm xa & Lam Kiếm Sĩ (Azure Blade).
/// Vẽ tia laser định hướng, quầng ngắm tại đích, và cập nhật hiệu ứng phát sáng khi tụ lực.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RangedAimIndicator : MonoBehaviour
{
    [Header("Aim Line Settings")]
    [SerializeField] private float maxLineLength = 8.5f;
    [SerializeField] private float baseLineWidth = 0.12f;
    [SerializeField] private float chargedLineWidth = 0.26f;
    [SerializeField] private Color baseColor = new Color(0.1f, 0.75f, 1f, 0.75f);
    [SerializeField] private Color fullChargeColor = new Color(0.8f, 1f, 1f, 1f);

    private LineRenderer lineRenderer;
    private GameObject targetReticle;
    private SpriteRenderer reticleSr;
    private bool isAiming = false;
    private float currentChargeRatio = 0f;
    private Vector2 currentAimDirection = Vector2.down;

    public bool IsAiming => isAiming;
    public float CurrentChargeRatio => currentChargeRatio;
    public Vector2 CurrentAimDirection => currentAimDirection;

    public static RangedAimIndicator EnsureExists(GameObject playerObj)
    {
        if (playerObj == null) return null;
        var ind = playerObj.GetComponent<RangedAimIndicator>();
        if (ind == null) ind = playerObj.AddComponent<RangedAimIndicator>();
        return ind;
    }

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        SetupLineRenderer();
        SetupTargetReticle();
        Hide();
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

    private void SetupTargetReticle()
    {
        Transform existing = transform.Find("AimTargetReticle");
        if (existing != null)
        {
            targetReticle = existing.gameObject;
            reticleSr = targetReticle.GetComponent<SpriteRenderer>();
        }
        else
        {
            targetReticle = new GameObject("AimTargetReticle");
            targetReticle.transform.SetParent(transform, false);

            reticleSr = targetReticle.AddComponent<SpriteRenderer>();
            reticleSr.sortingLayerName = "Default";
            reticleSr.sortingOrder = 5350;

            // Tạo Sprite vòng ngắm tròn sắc nét
            reticleSr.sprite = GenerateReticleSprite();
            targetReticle.transform.localScale = Vector3.one * 0.75f;
        }
        targetReticle.SetActive(false);
    }

    private Sprite GenerateReticleSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color cyan = new Color(0.3f, 0.9f, 1f, 0.95f);
        Color center = new Color(1f, 1f, 1f, 1f);

        float centerPos = (size - 1) / 2f;
        float outerR = size * 0.42f;
        float innerR = size * 0.32f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(centerPos, centerPos));
                if (d >= innerR && d <= outerR)
                {
                    tex.SetPixel(x, y, cyan);
                }
                else if (d <= 2f)
                {
                    tex.SetPixel(x, y, center);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    public void Show(Vector2 origin, Vector2 direction, float chargeRatio)
    {
        isAiming = true;
        currentChargeRatio = Mathf.Clamp01(chargeRatio);
        currentAimDirection = direction.normalized;

        if (currentAimDirection.sqrMagnitude < 0.001f) currentAimDirection = Vector2.down;

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null) lineRenderer.enabled = true;
        if (targetReticle != null) targetReticle.SetActive(true);

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
            lineRenderer.endWidth = width * 0.6f;

            Color activeColor = Color.Lerp(baseColor, fullChargeColor, currentChargeRatio);
            lineRenderer.startColor = activeColor;
            lineRenderer.endColor = new Color(activeColor.r, activeColor.g, activeColor.b, 0.1f);
        }

        if (targetReticle != null)
        {
            targetReticle.transform.position = endPoint;
            targetReticle.transform.Rotate(0f, 0f, 180f * Time.deltaTime);
            float scale = Mathf.Lerp(0.6f, 1.1f, currentChargeRatio);
            targetReticle.transform.localScale = Vector3.one * scale;
            if (reticleSr != null) reticleSr.color = Color.Lerp(baseColor, fullChargeColor, currentChargeRatio);
        }
    }

    public void Hide()
    {
        isAiming = false;
        currentChargeRatio = 0f;
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (targetReticle != null) targetReticle.SetActive(false);
    }
}
