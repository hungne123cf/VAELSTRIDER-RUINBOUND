using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Vùng Hủy Chiêu Chuẩn MOBA / Liên Quân Mobile (Skill Cancel Zone).
/// Đặt ở GÓC TRÊN BÊN PHẢI (Top-Right) màn hình, hoàn toàn tách biệt khỏi cụm phím kỹ năng góc dưới.
/// Khi người chơi nhấn giữ / kéo tia nhắm:
/// - Vùng chữ X trồi lên ở góc trên bên phải.
/// - Kéo ngón tay lên góc trên chữ X: Phóng to + cảnh báo đỏ.
/// - Thả tay trong vùng X: Hủy chiêu hoàn toàn (không mất mana, không tính hồi chiêu).
/// </summary>
public class SkillCancelZoneUI : MonoBehaviour
{
    private static SkillCancelZoneUI instance;
    public static SkillCancelZoneUI Instance => instance;

    [Header("UI Components")]
    [SerializeField] private RectTransform zoneRect;
    [SerializeField] private Image bgImage;
    [SerializeField] private Image ringImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private CanvasGroup canvasGroup;

    private bool isHovered = false;
    private bool isVisible = false;
    private float targetScale = 1f;
    private float currentScale = 1f;

    public bool IsHovered => isHovered && isVisible;

    public static SkillCancelZoneUI EnsureExists(Canvas canvas = null)
    {
        if (instance != null) return instance;

        if (canvas == null)
        {
            canvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null) return null;

        GameObject go = new GameObject("SkillCancelZoneUI");
        go.transform.SetParent(canvas.transform, false);
        instance = go.AddComponent<SkillCancelZoneUI>();
        instance.SetupUI();
        return instance;
    }

    void Awake()
    {
        instance = this;
        if (zoneRect == null) SetupUI();
    }

    private void SetupUI()
    {
        zoneRect = GetComponent<RectTransform>();
        if (zoneRect == null) zoneRect = gameObject.AddComponent<RectTransform>();

        // Đặt ở vị trí vừa tầm ngón tay cái (Upper-Left của cụm phím kỹ năng)
        zoneRect.anchorMin = new Vector2(1f, 0f);
        zoneRect.anchorMax = new Vector2(1f, 0f);
        zoneRect.pivot = new Vector2(0.5f, 0.5f);
        zoneRect.anchoredPosition = new Vector2(-230f, 420f);
        zoneRect.sizeDelta = new Vector2(92f, 92f);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 1. Nền tròn tối bán trong suốt
        GameObject bgGo = new GameObject("Cancel_BG");
        bgGo.transform.SetParent(transform, false);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgImage = bgGo.AddComponent<Image>();
        bgImage.sprite = GenerateCircleSprite(128);
        bgImage.color = new Color(0.15f, 0.04f, 0.06f, 0.75f);
        bgImage.raycastTarget = false;

        // 2. Vành tròn phát sáng (Glowing Ring)
        GameObject ringGo = new GameObject("Cancel_Ring");
        ringGo.transform.SetParent(transform, false);
        RectTransform ringRect = ringGo.AddComponent<RectTransform>();
        ringRect.anchorMin = Vector2.zero;
        ringRect.anchorMax = Vector2.one;
        ringRect.sizeDelta = Vector2.zero;
        ringImage = ringGo.AddComponent<Image>();
        ringImage.sprite = GenerateRingSprite(128, 5);
        ringImage.color = new Color(1f, 0.25f, 0.25f, 0.70f);
        ringImage.raycastTarget = false;

        // 3. Biểu tượng chữ X sắc nét vẽ theo Texture (không phụ thuộc Font)
        GameObject iconGo = new GameObject("Cancel_Icon");
        iconGo.transform.SetParent(transform, false);
        RectTransform iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.28f, 0.38f);
        iconRect.anchorMax = new Vector2(0.72f, 0.88f);
        iconRect.sizeDelta = Vector2.zero;
        iconImage = iconGo.AddComponent<Image>();
        iconImage.sprite = GenerateXSprite(64);
        iconImage.color = new Color(1f, 0.40f, 0.40f, 0.95f);
        iconImage.raycastTarget = false;

        // 4. Chữ "HỦY CHIÊU" / "CANCEL"
        GameObject labelGo = new GameObject("Cancel_Label");
        labelGo.transform.SetParent(transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.08f);
        labelRect.anchorMax = new Vector2(1f, 0.36f);
        labelRect.sizeDelta = Vector2.zero;
        labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = LocalizationManager.T("HỦY CHIÊU", "CANCEL");
        labelText.fontSize = 11f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(1f, 0.85f, 0.85f, 0.9f);
        labelText.raycastTarget = false;

        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isVisible) return;

        // Hiệu ứng phóng to mượt khi kéo vào vùng
        currentScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * 14f);
        transform.localScale = Vector3.one * currentScale;

        // Hiệu ứng nhấp nháy phát sáng nhẹ khi hover
        if (isHovered && ringImage != null)
        {
            float pulse = 0.75f + Mathf.PingPong(Time.unscaledTime * 4f, 0.25f);
            ringImage.color = new Color(1f, 0.2f, 0.2f, pulse);
        }
    }

    public void Show()
    {
        isVisible = true;
        isHovered = false;
        targetScale = 1.0f;
        currentScale = 0.5f;
        transform.localScale = Vector3.one * currentScale;
        if (labelText != null) labelText.text = LocalizationManager.T("HỦY CHIÊU", "CANCEL");
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        UpdateVisualState(false);
    }

    public void Hide()
    {
        isVisible = false;
        isHovered = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Kiểm tra vị trí ngón tay có nằm trong phạm vi vùng hủy chiêu không (hỗ trợ mọi Canvas Render Mode)
    /// </summary>
    public bool CheckIfInside(Vector2 screenPos)
    {
        if (zoneRect == null) return false;

        Canvas c = GetComponentInParent<Canvas>();
        Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;

        if (RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPos, cam))
        {
            return true;
        }

        Vector2 localPt;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneRect, screenPos, cam, out localPt))
        {
            float radius = Mathf.Max(zoneRect.rect.width, zoneRect.rect.height) * 0.95f;
            return localPt.magnitude <= radius;
        }

        return false;
    }

    /// <summary>
    /// Cập nhật vị trí ngón tay kéo màn hình và kích hoạt hiệu ứng hover
    /// </summary>
    public bool UpdatePointerPosition(Vector2 screenPos)
    {
        if (!isVisible || zoneRect == null) return false;

        bool newHovered = CheckIfInside(screenPos);

        if (newHovered != isHovered)
        {
            isHovered = newHovered;
            UpdateVisualState(isHovered);
        }

        return isHovered;
    }

    private void UpdateVisualState(bool hovered)
    {
        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (hovered)
        {
            targetScale = 1.25f; // Phóng to báo hiệu đã vào vùng hủy

            if (bgImage != null) bgImage.color = new Color(0.85f, 0.08f, 0.12f, 0.90f);
            if (ringImage != null) ringImage.color = new Color(1f, 0.95f, 0.95f, 1f);
            if (iconImage != null) iconImage.color = Color.white;
            if (labelText != null)
            {
                labelText.text = isVi ? "THẢ ĐỂ HỦY" : "RELEASE TO CANCEL";
                labelText.color = new Color(1f, 1f, 0.8f, 1f);
            }

            if (SkillAimIndicator.Instance != null)
            {
                SkillAimIndicator.Instance.SetCancelHighlight(true);
            }
        }
        else
        {
            targetScale = 1.0f;

            if (bgImage != null) bgImage.color = new Color(0.15f, 0.04f, 0.06f, 0.75f);
            if (ringImage != null) ringImage.color = new Color(1f, 0.25f, 0.25f, 0.70f);
            if (iconImage != null) iconImage.color = new Color(1f, 0.40f, 0.40f, 0.95f);
            if (labelText != null)
            {
                labelText.text = isVi ? "HỦY CHIÊU" : "CANCEL";
                labelText.color = new Color(1f, 0.85f, 0.85f, 0.85f);
            }

            if (SkillAimIndicator.Instance != null)
            {
                SkillAimIndicator.Instance.SetCancelHighlight(false);
            }
        }
    }

    private Sprite GenerateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color white = Color.white;
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (d <= radius)
                {
                    float alpha = Mathf.Clamp01((radius - d) * 1.5f);
                    tex.SetPixel(x, y, new Color(white.r, white.g, white.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GenerateRingSprite(int size, int strokeWidth)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color white = Color.white;
        float center = size / 2f;
        float outerR = size / 2f - 1f;
        float innerR = outerR - strokeWidth;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (d <= outerR && d >= innerR)
                {
                    float edgeAlpha = Mathf.Min(Mathf.Clamp01((outerR - d) * 1.5f), Mathf.Clamp01((d - innerR) * 1.5f));
                    tex.SetPixel(x, y, new Color(white.r, white.g, white.b, edgeAlpha));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GenerateXSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color white = Color.white;
        float stroke = size * 0.18f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Khoảng cách tới 2 đường chéo (d1: x=y, d2: x+y=size)
                float d1 = Mathf.Abs(x - y) / 1.414f;
                float d2 = Mathf.Abs(x + y - size) / 1.414f;
                float dist = Mathf.Min(d1, d2);

                if (dist <= stroke)
                {
                    float alpha = Mathf.Clamp01((stroke - dist) * 2f);
                    tex.SetPixel(x, y, new Color(white.r, white.g, white.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
