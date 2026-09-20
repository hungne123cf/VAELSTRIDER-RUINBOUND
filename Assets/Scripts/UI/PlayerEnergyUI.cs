using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giao dien HUD hien thi 2 thanh:
/// 1. Thanh Nang Luong (Energy Slider) mau Xanh Cyan phat quang.
/// 2. Thanh Noi Nang (Inner Energy Slider) mau Vang Ho Phach (kich hoat Giap Ao khi day).
/// Can chinh toa do chuan xac ben duoi thanh mau PlayerHealthBar va day ShardsCounter xuong duoi de khong bi chong de.
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

    void Awake()
    {
        Instance = this;
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
    }

    private void RepositionShardsCounter()
    {
        // Dam bao ShardsCounter khong bao gio bi de len thanh nang luong
        var shardsUI = FindAnyObjectByType<ShardsUI>();
        if (shardsUI != null)
        {
            RectTransform scRt = shardsUI.GetComponent<RectTransform>();
            if (scRt != null && scRt.anchoredPosition.y > -105f)
            {
                scRt.anchoredPosition = new Vector2(scRt.anchoredPosition.x, -112f);
            }
        }
    }

    private void UpdateEnergyBar()
    {
        if (energySlider == null || energySystem == null) return;

        float current = energySystem.GetEnergy();
        float max = energySystem.GetMaxEnergy();

        energySlider.maxValue = max;
        energySlider.value = current;

        if (energyText != null)
        {
            energyText.text = $"NL: {(int)current} / {(int)max}";
        }
    }

    private void UpdateInnerEnergyBar()
    {
        if (innerEnergySlider == null || energySystem == null) return;

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

            if (innerEnergyText != null)
            {
                innerEnergyText.text = $"GIAP HO THAN: {shieldHp}/{maxShield}";
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

            if (innerEnergyText != null)
            {
                innerEnergyText.text = $"NOI NANG: {(int)inner} / {(int)maxInner}";
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

        // Tao Container gan goc tren ben trai
        GameObject rootObj = new GameObject("Player_Energy_Bars_HUD");
        rootObj.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = rootObj.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);

        // Can chinh toa do chuan theo PlayerHealthBar
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

        // 1. Tao Thanh Nang Luong (Energy Bar) - Mau Cyan
        Instance.energySlider = CreateSubBar(rootObj.transform, "EnergyBar", 0f, barWidth, 13f, new Color(0.15f, 0.75f, 1f), out Instance.energyFillImage, out Instance.energyText, out _);

        // 2. Tao Thanh Noi Nang (Inner Energy Bar) - Mau Vang Ho Phach
        Instance.innerEnergySlider = CreateSubBar(rootObj.transform, "InnerEnergyBar", -17f, barWidth, 13f, new Color(0.95f, 0.72f, 0.15f), out Instance.innerEnergyFillImage, out Instance.innerEnergyText, out Instance.innerEnergyOutline);

        // Day ShardsCounter xuong duoi de khong chong len
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

        // Nen Bar toi mau sang trong
        Image bgImg = barObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        outlineComponent = barObj.AddComponent<Outline>();
        outlineComponent.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.6f);
        outlineComponent.effectDistance = new Vector2(1f, -1f);

        Slider slider = barObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        // Fill Area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(barObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        slider.fillRect = fillRt;

        // Text Label
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(barObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        labelText = textObj.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = 10;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
        labelText.outlineWidth = 0.2f;
        labelText.outlineColor = Color.black;

        return slider;
    }
}
