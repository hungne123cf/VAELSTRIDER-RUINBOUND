using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar Sliders")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider easeHpSlider;
    [SerializeField] private Text hpText;
    [SerializeField] private TextMeshProUGUI tmpHpText;

    [Header("Easing Speed")]
    [SerializeField] private float easeSpeed = 0.05f;

    [Header("White Shield Bar (Giáp Ảo Chuẩn Shape of Dreams - Đi từ Phải sang Trái)")]
    [SerializeField] private Color whiteShieldColor = new Color(0.68f, 0.84f, 0.98f, 0.55f); // Băng tinh lam mờ dịu mắt, không bị chói lóa làm mờ chữ số

    private PlayerHealth playerHealth;
    private GameObject whiteShieldBarObj;
    private RectTransform whiteShieldBarRt;
    private Image whiteShieldBarImg;
    private Outline whiteShieldOutline;
    private GameObject textBackdropObj;

    private int lastDisplayedCurrentHP = -1;
    private int lastDisplayedMaxHP = -1;
    private int lastDisplayedShield = -1;
    private bool hpTypographyApplied = false;

    void Awake()
    {
        EnsureTextComponent();
    }

    void Start()
    {
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (hpSlider == null) hpSlider = GetComponent<Slider>();
        EnsureTextComponent();
        SoulslikeHUDStyler.StyleHealthBar(GetComponentInParent<Canvas>());

        if (playerHealth != null)
        {
            int max = playerHealth.GetMaxHealth();
            int current = playerHealth.GetCurrentHealth();
            int shield = playerHealth.GetVirtualHealth();
            if (hpSlider != null)
            {
                hpSlider.maxValue = max;
                hpSlider.value = current;
            }
            if (easeHpSlider != null)
            {
                easeHpSlider.maxValue = max;
                easeHpSlider.value = current;
            }
            UpdateText(current, max, shield);
        }
    }

    void Update()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            return;
        }

        int currentHP = playerHealth.GetCurrentHealth();
        int maxHP = playerHealth.GetMaxHealth();
        int currentShield = playerHealth.GetVirtualHealth();

        if (hpSlider != null)
        {
            if (hpSlider.maxValue != maxHP)
            {
                hpSlider.maxValue = maxHP;
            }
            hpSlider.value = currentHP;
        }

        if (easeHpSlider != null)
        {
            if (easeHpSlider.maxValue != maxHP)
            {
                easeHpSlider.maxValue = maxHP;
            }
            if (easeHpSlider.value != hpSlider.value)
            {
                easeHpSlider.value = Mathf.Lerp(easeHpSlider.value, currentHP, easeSpeed);
            }
        }

        UpdateText(currentHP, maxHP, currentShield);
        UpdateWhiteShieldBar(currentHP, maxHP, currentShield);
    }

    private void UpdateWhiteShieldBar(int currentHP, int maxHP, int shield)
    {
        EnsureWhiteShieldOverlay();

        if (shield <= 0)
        {
            if (whiteShieldBarObj != null && whiteShieldBarObj.activeSelf)
                whiteShieldBarObj.SetActive(false);
            return;
        }

        if (whiteShieldBarObj != null)
        {
            if (!whiteShieldBarObj.activeSelf) whiteShieldBarObj.SetActive(true);

            // Chuẩn Shape of Dreams: Giáp ảo neo cố định ở mép phải (1.0f) và mở rộng dần về bên trái (1.0f -> 0.0f)
            float shieldRatio = maxHP > 0 ? (float)shield / maxHP : 0f;
            float minX = Mathf.Clamp01(1.0f - shieldRatio);

            whiteShieldBarRt.anchorMin = new Vector2(minX, 0f);
            whiteShieldBarRt.anchorMax = new Vector2(1.0f, 1f);
            whiteShieldBarRt.offsetMin = Vector2.zero;
            whiteShieldBarRt.offsetMax = Vector2.zero;

            // Đảm bảo chữ số hiển thị máu và tấm nền chữ luôn ở trên cùng không bị che khuất
            if (textBackdropObj != null) textBackdropObj.transform.SetAsLastSibling();
            if (tmpHpText != null) tmpHpText.transform.SetAsLastSibling();
            else if (hpText != null) hpText.transform.SetAsLastSibling();
        }
    }

    private void EnsureWhiteShieldOverlay()
    {
        // Dọn dẹp khối cục cũ nếu có (VirtualHealthBar_RightSide)
        Transform oldSide = transform.Find("VirtualHealthBar_RightSide");
        if (oldSide != null) Destroy(oldSide.gameObject);
        if (transform.parent != null)
        {
            Transform oldInParent = transform.parent.Find("VirtualHealthBar_RightSide");
            if (oldInParent != null && oldInParent.gameObject != gameObject)
            {
                Destroy(oldInParent.gameObject);
            }
        }

        Transform container = (hpSlider != null && hpSlider.fillRect != null) ? hpSlider.fillRect.parent : transform.Find("Fill Area");
        if (container == null) container = transform;

        // Dọn dẹp các GameObject thử nghiệm cũ
        Transform oldUnder = container.Find("WhiteShield_Underflow");
        if (oldUnder != null) Destroy(oldUnder.gameObject);
        Transform oldOver = container.Find("WhiteShield_Overflow");
        if (oldOver != null) Destroy(oldOver.gameObject);

        if (whiteShieldBarObj != null) return;

        Transform exist = container.Find("WhiteShield_Bar");
        if (exist != null)
        {
            whiteShieldBarObj = exist.gameObject;
            whiteShieldBarRt = whiteShieldBarObj.GetComponent<RectTransform>();
            whiteShieldBarImg = whiteShieldBarObj.GetComponent<Image>();
            whiteShieldOutline = whiteShieldBarObj.GetComponent<Outline>();
            return;
        }

        whiteShieldBarObj = new GameObject("WhiteShield_Bar");
        whiteShieldBarObj.transform.SetParent(container, false);
        whiteShieldBarRt = whiteShieldBarObj.AddComponent<RectTransform>();
        whiteShieldBarImg = whiteShieldBarObj.AddComponent<Image>();
        whiteShieldBarImg.color = whiteShieldColor;
        whiteShieldBarImg.raycastTarget = false;

        // Viền tinh thể phát sáng sang trọng cho Giáp Ảo chuẩn Shape of Dreams
        whiteShieldOutline = whiteShieldBarObj.AddComponent<Outline>();
        whiteShieldOutline.effectColor = new Color(0.40f, 0.85f, 1f, 0.80f);
        whiteShieldOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Đảm bảo chữ số hiển thị máu và tấm nền chữ luôn ở trên cùng không bị che khuất
        if (textBackdropObj != null) textBackdropObj.transform.SetAsLastSibling();
        if (tmpHpText != null) tmpHpText.transform.SetAsLastSibling();
        else if (hpText != null) hpText.transform.SetAsLastSibling();
    }

    private void EnsureTextBackdrop()
    {
        if (textBackdropObj != null) return;
        Transform exist = transform.Find("HP_Text_Backdrop");
        if (exist != null)
        {
            textBackdropObj = exist.gameObject;
            return;
        }

        textBackdropObj = new GameObject("HP_Text_Backdrop");
        textBackdropObj.transform.SetParent(transform, false);
        RectTransform rt = textBackdropObj.AddComponent<RectTransform>();
        // Neo ở góc phải nơi hiển thị chữ số HP để bảo vệ tương phản chữ
        rt.anchorMin = new Vector2(0.42f, 0.10f);
        rt.anchorMax = new Vector2(0.99f, 0.90f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = textBackdropObj.AddComponent<Image>();
        img.color = new Color(0.02f, 0.03f, 0.06f, 0.62f); // Nền tối Obsidian chống lóa chìm màu chữ 100%
        img.raycastTarget = false;
    }

    private void EnsureTextComponent()
    {
        EnsureTextBackdrop();
        if (tmpHpText != null || hpText != null) return;

        tmpHpText = GetComponentInChildren<TextMeshProUGUI>();
        if (tmpHpText != null)
        {
            ApplyBoldOutline(tmpHpText);
            return;
        }

        hpText = GetComponentInChildren<Text>();
        if (hpText != null) return;

        // Auto create centered text on health bar
        GameObject textObj = new GameObject("HP_Value_Text");
        textObj.transform.SetParent(transform, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        tmpHpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpHpText.fontSize = 12.5f;
        tmpHpText.fontStyle = FontStyles.Bold;
        tmpHpText.color = Color.white;
        tmpHpText.alignment = TextAlignmentOptions.Center;
        tmpHpText.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(tmpHpText, 0.55f, Color.black);
    }

    private void ApplyBoldOutline(TextMeshProUGUI tmp)
    {
        ApplySoulslikeTypography(tmp);
    }

    private void ApplySoulslikeTypography(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        EnsureTextBackdrop();
        RectTransform txtRt = tmp.GetComponent<RectTransform>();
        if (txtRt != null)
        {
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-6f, 0f);
        }
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.fontSize = 12.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        // Viền đen đậm nét 0.50f chống chìm màu chữ 100% khi full giáp ảo
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.50f, Color.black);
    }

    private void UpdateText(int current, int max, int shield = 0)
    {
        EnsureTextComponent();
        if (current == lastDisplayedCurrentHP && max == lastDisplayedMaxHP && shield == lastDisplayedShield) return;
        lastDisplayedCurrentHP = current;
        lastDisplayedMaxHP = max;
        lastDisplayedShield = shield;

        if (textBackdropObj != null) textBackdropObj.transform.SetAsLastSibling();

        if (tmpHpText != null)
        {
            tmpHpText.transform.SetAsLastSibling();
            if (shield > 0)
            {
                // Máu trắng sáng, Giáp Ảo màu vàng kim hổ phách rực rỡ nổi bật tuyệt đối trên nền tối và viền đen
                tmpHpText.text = $"<color=#FFFFFF><b>{current}</b></color> <color=#FBBF24><b>(+{shield})</b></color> <color=#94A3B8>/</color> <color=#FFFFFF><b>{max}</b></color>";
            }
            else
            {
                tmpHpText.text = $"<color=#FFFFFF><b>{current}</b></color> <color=#94A3B8>/</color> <color=#FFFFFF><b>{max}</b></color>";
            }

            if (!hpTypographyApplied)
            {
                ApplySoulslikeTypography(tmpHpText);
                hpTypographyApplied = true;
            }
        }
        else if (hpText != null)
        {
            hpText.transform.SetAsLastSibling();
            hpText.text = (shield > 0) ? $"{current} (+{shield}) / {max}" : $"{current} / {max}";
        }
    }
}
