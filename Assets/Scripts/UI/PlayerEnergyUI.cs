using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giao dien HUD hien thi 2 thanh ben duoi thanh mau chinh:
/// 1. Thanh Nang Luong (Energy Slider) mau Xanh Cyan (hien thi % voi chu trang vien den ro net).
/// 2. Thanh Noi Nang / Giap Ho Than (Inner Energy Slider) mau Vang Cam (visual tien do thuan tuy).
/// </summary>
public class PlayerEnergyUI : MonoBehaviour
{
    public static PlayerEnergyUI Instance { get; private set; }

    [Header("Energy Bar (Nang Luong)")]
    private Slider energySlider;
    private Image energyFillImage;
    private TextMeshProUGUI energyText;

    [Header("Inner Energy Bar (Noi Nang & Giap Ho Than)")]
    private Slider innerEnergySlider;
    private Image innerEnergyFillImage;
    private TextMeshProUGUI innerEnergyText;
    private Outline innerEnergyOutline;

    private PlayerEnergySystem energySystem;
    private RectTransform rootRt;

    void Awake()
    {
        Instance = this;
        rootRt = GetComponent<RectTransform>();
    }

    void Start()
    {
        FindEnergySystem();
        RepositionShardsCounter();
    }

    void Update()
    {
        if (energySystem == null)
        {
            FindEnergySystem();
            if (energySystem == null) return;
        }

        UpdateEnergyBar();
        UpdateInnerEnergyBar();
        RepositionShardsCounter();
    }

    private void FindEnergySystem()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            energySystem = player.GetComponent<PlayerEnergySystem>();
            if (energySystem == null)
            {
                energySystem = player.gameObject.AddComponent<PlayerEnergySystem>();
            }
        }

        float barWidth = (rootRt != null && rootRt.sizeDelta.x > 50f) ? rootRt.sizeDelta.x : 220f;

        if (energySlider == null)
        {
            Transform eb = transform.Find("EnergyBar");
            if (eb != null)
            {
                energySlider = eb.GetComponent<Slider>();
                energyFillImage = eb.Find("FillArea/Fill")?.GetComponent<Image>();
                energyText = eb.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                energySlider = CreateSubBar(transform, "EnergyBar", 0f, barWidth, 13f, new Color(0.15f, 0.75f, 1f), out energyFillImage, out energyText, out _);
            }
        }

        if (innerEnergySlider == null)
        {
            Transform ieb = transform.Find("InnerEnergyBar");
            if (ieb != null)
            {
                innerEnergySlider = ieb.GetComponent<Slider>();
                innerEnergyFillImage = ieb.Find("FillArea/Fill")?.GetComponent<Image>();
                innerEnergyText = ieb.Find("Text")?.GetComponent<TextMeshProUGUI>();
                innerEnergyOutline = ieb.GetComponent<Outline>();
            }
            else
            {
                innerEnergySlider = CreateSubBar(transform, "InnerEnergyBar", -17f, barWidth, 13f, new Color(0.95f, 0.72f, 0.15f), out innerEnergyFillImage, out innerEnergyText, out innerEnergyOutline);
            }
        }

        ApplyHighContrastStyle(energyText);
        if (innerEnergyText != null) innerEnergyText.gameObject.SetActive(false);
    }

    private void ApplyHighContrastStyle(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; // Chữ trắng sáng
        tmp.outlineWidth = 0.35f;
        tmp.outlineColor = Color.black; // Viền đen dày đậm 360 độ cực kỳ dễ nhìn trên nền Cyan
    }

    private void RepositionShardsCounter()
    {
        var shardsUI = FindAnyObjectByType<ShardsUI>();
        if (shardsUI != null)
        {
            RectTransform scRt = shardsUI.GetComponent<RectTransform>();
            if (scRt != null && scRt.anchoredPosition.y > -130f)
            {
                scRt.anchoredPosition = new Vector2(scRt.anchoredPosition.x, -135f);
            }
        }
    }

    private DuneAssassinSkill duneSkill;

    private void UpdateEnergyBar()
    {
        if (energySlider == null) return;

        if (duneSkill == null && energySystem != null)
        {
            duneSkill = energySystem.GetComponent<DuneAssassinSkill>();
        }

        if (duneSkill != null && duneSkill.enabled)
        {
            float curRage = duneSkill.CurrentRage;
            float maxRage = duneSkill.MaxRage;

            energySlider.maxValue = maxRage;
            energySlider.value = curRage;

            bool isFull = duneSkill.IsRageFull;
            if (energyFillImage != null)
            {
                energyFillImage.color = isFull 
                    ? new Color(1f, 0.15f, 0.15f, 1f) 
                    : new Color(0.95f, 0.35f, 0.15f, 1f);
            }

            if (energyText != null)
            {
                int pct = maxRage > 0 ? Mathf.Clamp(Mathf.RoundToInt((curRage / maxRage) * 100f), 0, 100) : 0;
                energyText.text = $"{pct}%";
                ApplyHighContrastStyle(energyText);
            }
            return;
        }

        if (energySystem == null) return;

        float current = energySystem.GetEnergy();
        float max = energySystem.GetMaxEnergy();

        energySlider.maxValue = max;
        energySlider.value = current;

        if (energyFillImage != null)
        {
            energyFillImage.color = new Color(0.15f, 0.75f, 1f); // Cyan
        }

        if (energyText != null)
        {
            int pct = max > 0 ? Mathf.Clamp(Mathf.RoundToInt((current / max) * 100f), 0, 100) : 0;
            energyText.text = $"{pct}%";
            ApplyHighContrastStyle(energyText);
        }
    }

    private void UpdateInnerEnergyBar()
    {
        if (innerEnergySlider == null) return;

        if (!innerEnergySlider.gameObject.activeSelf)
        {
            innerEnergySlider.gameObject.SetActive(true);
        }

        if (energySystem == null) return;

        // Bỏ chữ số cho thanh nội năng
        if (innerEnergyText != null && innerEnergyText.gameObject.activeSelf)
        {
            innerEnergyText.gameObject.SetActive(false);
        }

        // Kiểm tra riêng cho Player 6 (Celestial Mage): Không có lớp giáp vàng, màu Tím Tinh Tú huyền ảo
        var mage = (energySystem != null) 
            ? (energySystem.GetComponent<CelestialMageSkill>() ?? energySystem.GetComponentInChildren<CelestialMageSkill>() ?? energySystem.GetComponentInParent<CelestialMageSkill>() ?? FindAnyObjectByType<CelestialMageSkill>()) 
            : null;
        if (mage != null && mage.enabled)
        {
            float inner = energySystem.GetInnerEnergy();
            float maxInner = energySystem.GetMaxInnerEnergy();

            innerEnergySlider.maxValue = maxInner;
            innerEnergySlider.value = inner;

            if (innerEnergyFillImage != null)
            {
                if (mage.IsOverdriveActive)
                {
                    // Chớp sáng tím dạ quang báo hiệu đang xả chiêu cuối 1s liên tục!
                    float pulse = 0.85f + Mathf.PingPong(Time.time * 5f, 0.15f);
                    innerEnergyFillImage.color = new Color(0.95f, 0.45f, 1f, pulse);
                }
                else
                {
                    // Tím Tinh Vân
                    innerEnergyFillImage.color = new Color(0.78f, 0.38f, 0.98f, 0.95f);
                }
            }

            if (innerEnergyOutline != null)
            {
                if (mage.IsOverdriveActive)
                {
                    float pulse = 0.5f + Mathf.Sin(Time.time * 8f) * 0.5f;
                    innerEnergyOutline.effectColor = new Color(1f, 0.4f, 1f, pulse);
                }
                else
                {
                    innerEnergyOutline.effectColor = new Color(0.35f, 0.15f, 0.45f, 0.5f);
                }
            }
            return;
        }

        bool hasShield = energySystem.IsShieldActive();

        if (hasShield)
        {
            int shieldHp = energySystem.GetShieldHealth();
            int maxShield = energySystem.GetMaxShieldHealth();

            innerEnergySlider.maxValue = maxShield;
            innerEnergySlider.value = shieldHp;

            if (innerEnergyFillImage != null)
            {
                innerEnergyFillImage.color = new Color(1f, 0.92f, 0.35f, 1f); // Vang ruc ro
            }

            if (innerEnergyOutline != null)
            {
                float pulse = 0.5f + Mathf.Sin(Time.time * 6f) * 0.5f;
                innerEnergyOutline.effectColor = new Color(1f, 0.85f, 0.2f, pulse);
            }
        }
        else
        {
            float inner = energySystem.GetInnerEnergy();
            float maxInner = energySystem.GetMaxInnerEnergy();

            innerEnergySlider.maxValue = maxInner;
            innerEnergySlider.value = inner;

            if (innerEnergyFillImage != null)
            {
                innerEnergyFillImage.color = new Color(0.95f, 0.72f, 0.15f, 0.95f); // Ho phach
            }

            if (innerEnergyOutline != null)
            {
                innerEnergyOutline.effectColor = new Color(0.35f, 0.25f, 0.1f, 0.5f);
            }
        }
    }

    public static PlayerEnergyUI EnsureUIExists()
    {
        if (Instance != null) return Instance;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("Player_Energy_Bars_HUD");
        if (existing != null)
        {
            Instance = existing.GetComponent<PlayerEnergyUI>();
            if (Instance != null) return Instance;
        }

        GameObject rootObj = new GameObject("Player_Energy_Bars_HUD");
        rootObj.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = rootObj.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);

        Transform healthBar = canvas.transform.Find("PlayerHealthBar");
        if (healthBar == null) healthBar = canvas.transform.Find("HealthBar");

        float startX = 20f;
        float startY = -43f;
        float barWidth = 220f;

        if (healthBar != null)
        {
            RectTransform hbRt = healthBar.GetComponent<RectTransform>();
            if (hbRt != null)
            {
                startX = hbRt.anchoredPosition.x - (hbRt.sizeDelta.x * hbRt.pivot.x);
                startY = hbRt.anchoredPosition.y - (hbRt.sizeDelta.y * (1f - hbRt.pivot.y)) - 4f;
                barWidth = hbRt.sizeDelta.x;
            }
        }

        rootRt.anchoredPosition = new Vector2(startX, startY);
        rootRt.sizeDelta = new Vector2(barWidth, 36f);

        Instance = rootObj.AddComponent<PlayerEnergyUI>();

        // 1. Thanh Nang Luong (Cyan)
        Instance.energySlider = CreateSubBar(rootObj.transform, "EnergyBar", 0f, barWidth, 13f, new Color(0.15f, 0.75f, 1f), out Instance.energyFillImage, out Instance.energyText, out _);

        // 2. Thanh Noi Nang (Ho Phach / Vang)
        Instance.innerEnergySlider = CreateSubBar(rootObj.transform, "InnerEnergyBar", -17f, barWidth, 13f, new Color(0.95f, 0.72f, 0.15f), out Instance.innerEnergyFillImage, out Instance.innerEnergyText, out Instance.innerEnergyOutline);

        Instance.RepositionShardsCounter();

        return Instance;
    }

    private static Slider CreateSubBar(Transform parent, string name, float offsetY, float width, float height, Color fillColor, out Image fillImg, out TextMeshProUGUI labelText, out Outline outlineComponent)
    {
        GameObject barObj = new GameObject(name);
        barObj.transform.SetParent(parent, false);

        RectTransform rt = barObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, offsetY);
        rt.sizeDelta = new Vector2(width, height);

        Image bgImg = barObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        outlineComponent = barObj.AddComponent<Outline>();
        outlineComponent.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.6f);
        outlineComponent.effectDistance = new Vector2(1f, -1f);

        Slider slider = barObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(barObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        slider.fillRect = fillRt;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(barObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        labelText = textObj.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = 13.5f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
        labelText.outlineWidth = 0.35f;
        labelText.outlineColor = Color.black;

        return slider;
    }
}
