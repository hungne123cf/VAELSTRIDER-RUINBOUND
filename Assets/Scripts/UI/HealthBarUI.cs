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

    [Header("Virtual Health Bar (Nằm Sát Mép Phải Thanh Máu Thật - Màu Cam Đỏ Cục Gọn)")]
    [SerializeField] private Color barrierColor = new Color(1f, 0.28f, 0.05f, 1f); // Màu cam đỏ rực rỡ
    [SerializeField] private Color barrierFlashColor = new Color(1f, 0.85f, 0.15f, 0.25f);

    private PlayerHealth playerHealth;
    private GameObject virtualHpBarObj;
    private Slider virtualHpSlider;
    private Image virtualHpFillImage;
    private TextMeshProUGUI virtualHpText;
    private Outline virtualHpOutline;

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
            UpdateText(current, max);
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

        UpdateText(currentHP, maxHP);
        UpdateVirtualHealthBar();
    }

    private void UpdateVirtualHealthBar()
    {
        bool isVirtualActive = playerHealth != null && playerHealth.IsVirtualHealthActive();

        if (isVirtualActive)
        {
            EnsureVirtualHealthBar();
            if (virtualHpBarObj == null) return;

            if (!virtualHpBarObj.activeSelf) virtualHpBarObj.SetActive(true);

            int currentVHp = playerHealth.GetVirtualHealth();
            int maxVHp = playerHealth.GetMaxVirtualHealth();

            if (virtualHpSlider != null)
            {
                virtualHpSlider.maxValue = Mathf.Max(1, maxVHp);
                virtualHpSlider.value = currentVHp;
            }

            if (virtualHpText != null)
            {
                virtualHpText.text = $"+{currentVHp}";
            }

            bool isExpiringSoon = playerHealth.IsVirtualHealthExpiringSoon();
            if (isExpiringSoon)
            {
                // Nhấp nháy nhanh khi còn <= 3.5 giây
                float t = Mathf.PingPong(Time.time * 9f, 1f);
                if (virtualHpFillImage != null)
                {
                    virtualHpFillImage.color = Color.Lerp(barrierColor, barrierFlashColor, t);
                }
                if (virtualHpOutline != null)
                {
                    virtualHpOutline.effectColor = Color.Lerp(new Color(1f, 0.4f, 0.1f, 1f), new Color(1f, 0.9f, 0.2f, 0.2f), t);
                }
                if (virtualHpText != null)
                {
                    virtualHpText.color = t > 0.5f ? Color.white : new Color(1f, 0.85f, 0.2f, 1f);
                }
            }
            else
            {
                if (virtualHpFillImage != null)
                {
                    virtualHpFillImage.color = barrierColor;
                }
                if (virtualHpOutline != null)
                {
                    virtualHpOutline.effectColor = new Color(0.9f, 0.2f, 0.05f, 0.85f);
                }
                if (virtualHpText != null)
                {
                    virtualHpText.color = Color.white;
                }
            }
        }
        else
        {
            if (virtualHpBarObj != null && virtualHpBarObj.activeSelf)
            {
                virtualHpBarObj.SetActive(false);
            }
        }
    }

    private void EnsureVirtualHealthBar()
    {
        if (virtualHpBarObj != null) return;

        // Dọn dẹp object cũ nếu có
        Transform oldInParent = transform.parent != null ? transform.parent.Find("VirtualHealthBar_RightSide") : null;
        if (oldInParent != null && oldInParent.gameObject != gameObject)
        {
            Destroy(oldInParent.gameObject);
        }

        Transform existingChild = transform.Find("VirtualHealthBar_RightSide");
        if (existingChild != null)
        {
            virtualHpBarObj = existingChild.gameObject;
            virtualHpSlider = virtualHpBarObj.GetComponent<Slider>();
            virtualHpFillImage = virtualHpBarObj.transform.Find("FillArea/Fill")?.GetComponent<Image>();
            virtualHpText = virtualHpBarObj.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            virtualHpOutline = virtualHpBarObj.GetComponent<Outline>();
            return;
        }

        virtualHpBarObj = new GameObject("VirtualHealthBar_RightSide");
        virtualHpBarObj.transform.SetParent(transform, false);

        // Neo trực tiếp vào mép phải thanh máu thật: y kéo dài 100% khớp tuyệt đối, x cách đúng 2px sát rạt
        RectTransform vRt = virtualHpBarObj.AddComponent<RectTransform>();
        vRt.anchorMin = new Vector2(1f, 0f);
        vRt.anchorMax = new Vector2(1f, 1f);
        vRt.pivot = new Vector2(0f, 0.5f);
        vRt.anchoredPosition = new Vector2(2f, 0f); // Sát mép phải 2 pixel
        vRt.sizeDelta = new Vector2(75f, 0f);       // Rộng 75px (dạng khối cục gọn), cao khớp 100%

        // Background
        Image bgImg = virtualHpBarObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.04f, 0.04f, 0.95f);

        virtualHpOutline = virtualHpBarObj.AddComponent<Outline>();
        virtualHpOutline.effectColor = new Color(0.9f, 0.2f, 0.05f, 0.85f);
        virtualHpOutline.effectDistance = new Vector2(1f, -1f);

        virtualHpSlider = virtualHpBarObj.AddComponent<Slider>();
        virtualHpSlider.interactable = false;
        virtualHpSlider.transition = Selectable.Transition.None;

        // Fill Area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(virtualHpBarObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        // Fill Image
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        virtualHpFillImage = fill.AddComponent<Image>();
        virtualHpFillImage.color = barrierColor;
        virtualHpSlider.fillRect = fillRt;

        // Number Text Label - Chữ trắng sáng viền đen dày nổi bật trên mọi nền
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(virtualHpBarObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        virtualHpText = textObj.AddComponent<TextMeshProUGUI>();
        virtualHpText.fontSize = 12;
        virtualHpText.fontStyle = FontStyles.Bold;
        virtualHpText.color = Color.white;
        virtualHpText.alignment = TextAlignmentOptions.Center;
        virtualHpText.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(virtualHpText, 0.35f, Color.black);
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

    private void UpdateText(int current, int max)
    {
        EnsureTextComponent();
        if (tmpHpText != null)
        {
            tmpHpText.text = $"<color=#f0d890>{current}</color> <size=9><color=#8c806a>/</color></size> <color=#c4b89f>{max}</color>";
            ApplySoulslikeTypography(tmpHpText);
        }
        else if (hpText != null)
        {
            hpText.text = $"{current} / {max}";
        }
    }
}
