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
            int shield = playerHealth.Player1WhiteShield;
            int virtualHp = playerHealth.GetVirtualHealth();
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
            UpdateText(current, max, shield, virtualHp);
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
        int currentShield = playerHealth.Player1WhiteShield; // Giáp Ảo riêng của Player 1 (White Shield)
        int virtualHp = playerHealth.GetVirtualHealth();     // Máu Ảo từ Lõi Nâng Cấp (Augments)

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

        UpdateText(currentHP, maxHP, currentShield, virtualHp);
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

            // Đảm bảo chữ số hiển thị máu luôn ở trên cùng không bị che khuất
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

        // Đảm bảo chữ số hiển thị máu luôn ở trên cùng không bị che khuất
        if (tmpHpText != null) tmpHpText.transform.SetAsLastSibling();
        else if (hpText != null) hpText.transform.SetAsLastSibling();
    }

    private void EnsureTextComponent()
    {
        // Xóa sạch tấm nền xám cũ nếu có để thanh máu liền mạch một màu đỏ thẫm
        Transform existBackdrop = transform.Find("HP_Text_Backdrop");
        if (existBackdrop != null) Destroy(existBackdrop.gameObject);

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
        RectTransform txtRt = tmp.GetComponent<RectTransform>();
        if (txtRt != null)
        {
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-6f, 0f);
        }
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.fontSize = 13f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        // Viền đen đậm nét 0.50f chống chìm màu chữ 100% khi full giáp ảo
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.50f, Color.black);
    }

    private int lastDisplayedVirtualHp = -1;

    private void UpdateText(int current, int max, int shield = 0, int virtualHp = 0)
    {
        EnsureTextComponent();
        if (current == lastDisplayedCurrentHP && max == lastDisplayedMaxHP && shield == lastDisplayedShield && virtualHp == lastDisplayedVirtualHp) return;
        lastDisplayedCurrentHP = current;
        lastDisplayedMaxHP = max;
        lastDisplayedShield = shield;
        lastDisplayedVirtualHp = virtualHp;

        if (tmpHpText != null)
        {
            tmpHpText.transform.SetAsLastSibling();
            if (shield > 0)
            {
                // Khi có Giáp Ảo: Đổi màu chữ sang màu đen để tương phản sắc nét trên thanh giáp ảo màu trắng/lam sáng
                string vHpStr = (virtualHp > 0) ? $" <color=#B45309>[+{virtualHp}]</color>" : "";
                tmpHpText.text = $"<color=#000000><b>{current}</b></color> <color=#B45309><b>(+{shield})</b></color>{vHpStr} <color=#1E293B>/</color> <color=#000000><b>{max}</b></color>";
                // Viền mỏng sáng nhẹ giúp nét chữ đen hiển thị sắc nét tuyệt đối
                SoulslikeHUDStyler.SetSafeOutline(tmpHpText, 0.25f, new Color(1f, 1f, 1f, 0.35f));
            }
            else
            {
                // Khi không có Giáp Ảo: Màu trắng bình thường trên thanh máu đỏ thẫm
                string vHpStr = (virtualHp > 0) ? $" <color=#F59E0B><b>[+{virtualHp}]</b></color>" : "";
                tmpHpText.text = $"<color=#FFFFFF><b>{current}</b></color>{vHpStr} <color=#94A3B8>/</color> <color=#FFFFFF><b>{max}</b></color>";
                // Viền đen đậm Souls-like
                SoulslikeHUDStyler.SetSafeOutline(tmpHpText, 0.50f, Color.black);
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
            string vHpStr = (virtualHp > 0) ? $" [+{virtualHp}]" : "";
            hpText.text = (shield > 0) ? $"{current} (+{shield}){vHpStr} / {max}" : $"{current}{vHpStr} / {max}";
        }
    }
}
