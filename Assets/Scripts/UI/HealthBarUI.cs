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

    [Header("White Shield Bar (Giáp Ảo Màu Trắng Phủ Trực Tiếp Trên Thanh Máu)")]
    [SerializeField] private Color whiteShieldColor = new Color(1f, 1f, 1f, 0.95f); // Trắng sáng tinh khiết chuẩn MOBA
    [SerializeField] private Color whiteShieldOverflowColor = new Color(1f, 1f, 1f, 0.72f); // Trắng mờ phát sáng khi giáp tràn qua 100%

    private PlayerHealth playerHealth;
    private GameObject whiteShieldUnderflowObj;
    private RectTransform whiteShieldUnderflowRt;
    private Image whiteShieldUnderflowImg;
    private GameObject whiteShieldOverflowObj;
    private RectTransform whiteShieldOverflowRt;
    private Image whiteShieldOverflowImg;

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
            if (whiteShieldUnderflowObj != null && whiteShieldUnderflowObj.activeSelf)
                whiteShieldUnderflowObj.SetActive(false);
            if (whiteShieldOverflowObj != null && whiteShieldOverflowObj.activeSelf)
                whiteShieldOverflowObj.SetActive(false);
            return;
        }

        float hpRatio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
        float shieldRatio = maxHP > 0 ? (float)shield / maxHP : 0f;

        // 1. Dải Giáp Ảo nối tiếp ngay sau thanh HP hiện tại (lấp đầy thanh máu về phía 100%)
        float underflowMin = hpRatio;
        float underflowMax = Mathf.Clamp01(hpRatio + shieldRatio);

        if (underflowMax > underflowMin && whiteShieldUnderflowObj != null)
        {
            if (!whiteShieldUnderflowObj.activeSelf) whiteShieldUnderflowObj.SetActive(true);
            whiteShieldUnderflowRt.anchorMin = new Vector2(underflowMin, 0f);
            whiteShieldUnderflowRt.anchorMax = new Vector2(underflowMax, 1f);
            whiteShieldUnderflowRt.offsetMin = Vector2.zero;
            whiteShieldUnderflowRt.offsetMax = Vector2.zero;
        }
        else if (whiteShieldUnderflowObj != null && whiteShieldUnderflowObj.activeSelf)
        {
            whiteShieldUnderflowObj.SetActive(false);
        }

        // 2. Dải Giáp Ảo tràn (Over-shield khi Máu + Giáp Ảo > 100% Max HP): Phủ lớp màng trắng phát sáng từ 0% lên
        float overflowRatio = (hpRatio + shieldRatio) - 1.0f;
        if (overflowRatio > 0.002f && whiteShieldOverflowObj != null)
        {
            if (!whiteShieldOverflowObj.activeSelf) whiteShieldOverflowObj.SetActive(true);
            whiteShieldOverflowRt.anchorMin = new Vector2(0f, 0f);
            whiteShieldOverflowRt.anchorMax = new Vector2(Mathf.Clamp01(overflowRatio), 1f);
            whiteShieldOverflowRt.offsetMin = Vector2.zero;
            whiteShieldOverflowRt.offsetMax = Vector2.zero;
        }
        else if (whiteShieldOverflowObj != null && whiteShieldOverflowObj.activeSelf)
        {
            whiteShieldOverflowObj.SetActive(false);
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

        if (whiteShieldUnderflowObj != null && whiteShieldOverflowObj != null) return;

        Transform container = (hpSlider != null && hpSlider.fillRect != null) ? hpSlider.fillRect.parent : transform.Find("Fill Area");
        if (container == null) container = transform;

        // 1. Tạo WhiteShield_Underflow
        Transform existUnder = container.Find("WhiteShield_Underflow");
        if (existUnder != null)
        {
            whiteShieldUnderflowObj = existUnder.gameObject;
            whiteShieldUnderflowRt = whiteShieldUnderflowObj.GetComponent<RectTransform>();
            whiteShieldUnderflowImg = whiteShieldUnderflowObj.GetComponent<Image>();
        }
        else
        {
            whiteShieldUnderflowObj = new GameObject("WhiteShield_Underflow");
            whiteShieldUnderflowObj.transform.SetParent(container, false);
            whiteShieldUnderflowRt = whiteShieldUnderflowObj.AddComponent<RectTransform>();
            whiteShieldUnderflowImg = whiteShieldUnderflowObj.AddComponent<Image>();
            whiteShieldUnderflowImg.color = whiteShieldColor;
            whiteShieldUnderflowImg.raycastTarget = false;
        }

        // 2. Tạo WhiteShield_Overflow
        Transform existOver = container.Find("WhiteShield_Overflow");
        if (existOver != null)
        {
            whiteShieldOverflowObj = existOver.gameObject;
            whiteShieldOverflowRt = whiteShieldOverflowObj.GetComponent<RectTransform>();
            whiteShieldOverflowImg = whiteShieldOverflowObj.GetComponent<Image>();
        }
        else
        {
            whiteShieldOverflowObj = new GameObject("WhiteShield_Overflow");
            whiteShieldOverflowObj.transform.SetParent(container, false);
            whiteShieldOverflowRt = whiteShieldOverflowObj.AddComponent<RectTransform>();
            whiteShieldOverflowImg = whiteShieldOverflowObj.AddComponent<Image>();
            whiteShieldOverflowImg.color = whiteShieldOverflowColor;
            whiteShieldOverflowImg.raycastTarget = false;
        }

        // Đảm bảo chữ số hiển thị máu luôn ở trên cùng không bị che khuất
        if (tmpHpText != null) tmpHpText.transform.SetAsLastSibling();
        else if (hpText != null) hpText.transform.SetAsLastSibling();
    }

    private void EnsureTextComponent()
    {
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
        tmpHpText.fontSize = 13;
        tmpHpText.fontStyle = FontStyles.Bold;
        tmpHpText.color = Color.white;
        tmpHpText.alignment = TextAlignmentOptions.Center;
        tmpHpText.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(tmpHpText, 0.35f, Color.black);
    }

    private void ApplyBoldOutline(TextMeshProUGUI tmp)
    {
        ApplySoulslikeTypography(tmp);
    }

    private void ApplySoulslikeTypography(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        RectTransform txtRt = tmp.GetComponent<RectTransform>();
        if (txtRt != null)
        {
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-8f, 0f);
        }
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.fontSize = 11f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.96f, 0.92f, 0.82f, 1f); // Trắng ngà hoàng gia
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.35f, new Color(0.05f, 0.05f, 0.05f, 0.95f));
    }

    private void UpdateText(int current, int max, int shield = 0)
    {
        EnsureTextComponent();
        if (current == lastDisplayedCurrentHP && max == lastDisplayedMaxHP && shield == lastDisplayedShield) return;
        lastDisplayedCurrentHP = current;
        lastDisplayedMaxHP = max;
        lastDisplayedShield = shield;

        if (tmpHpText != null)
        {
            if (shield > 0)
            {
                tmpHpText.text = $"<color=#f0d890>{current}</color> <color=#ffffff><b>(+{shield})</b></color> <size=9><color=#8c806a>/</color></size> <color=#c4b89f>{max}</color>";
            }
            else
            {
                tmpHpText.text = $"<color=#f0d890>{current}</color> <size=9><color=#8c806a>/</color></size> <color=#c4b89f>{max}</color>";
            }

            if (!hpTypographyApplied)
            {
                ApplySoulslikeTypography(tmpHpText);
                hpTypographyApplied = true;
            }
        }
        else if (hpText != null)
        {
            hpText.text = (shield > 0) ? $"{current} (+{shield}) / {max}" : $"{current} / {max}";
        }
    }
}
