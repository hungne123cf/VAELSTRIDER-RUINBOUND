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
    private RectTransform cachedHealthBarRt;
    private RectTransform cachedEnergySliderRt;
    private RectTransform cachedInnerEnergySliderRt;
    private RectTransform cachedShardsRt;
    private CelestialMageSkill cachedMageSkill;
    private bool mageSkillChecked = false;
    private int lastDisplayedPct = -1;

    void Awake()
    {
        Instance = this;
        rootRt = GetComponent<RectTransform>();
    }

    void Start()
    {
        FindEnergySystem();
        AlignWithHealthBar();
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
        AlignWithHealthBar();
    }

    public void AlignWithHealthBar()
    {
        if (cachedHealthBarRt == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Transform healthBar = canvas.transform.Find("PlayerHealthBar");
            if (healthBar == null) healthBar = canvas.transform.Find("HealthBar");
            if (healthBar != null) cachedHealthBarRt = healthBar.GetComponent<RectTransform>();
        }

        RectTransform hbRt = cachedHealthBarRt;
        if (hbRt != null && rootRt != null)
        {
            float startX = hbRt.anchoredPosition.x - (hbRt.sizeDelta.x * hbRt.pivot.x);
            float startY = hbRt.anchoredPosition.y - (hbRt.sizeDelta.y * (1f - hbRt.pivot.y)) - 3f;
            float barWidth = Mathf.Max(340f, hbRt.sizeDelta.x - 10f);
            if (Mathf.Abs(rootRt.anchoredPosition.x - startX) > 0.5f || 
                Mathf.Abs(rootRt.anchoredPosition.y - startY) > 0.5f ||
                Mathf.Abs(rootRt.sizeDelta.x - barWidth) > 0.5f ||
                Mathf.Abs(rootRt.sizeDelta.y - 50f) > 0.5f)
            {
                rootRt.anchoredPosition = new Vector2(startX, startY);
                rootRt.sizeDelta = new Vector2(barWidth, 50f);

                if (energySlider != null)
                {
                    if (cachedEnergySliderRt == null) cachedEnergySliderRt = energySlider.GetComponent<RectTransform>();
                    if (cachedEnergySliderRt != null)
                    {
                        cachedEnergySliderRt.anchoredPosition = new Vector2(0f, 0f);
                        cachedEnergySliderRt.sizeDelta = new Vector2(barWidth, 30f);
                    }
                }
                if (innerEnergySlider != null)
                {
                    if (cachedInnerEnergySliderRt == null) cachedInnerEnergySliderRt = innerEnergySlider.GetComponent<RectTransform>();
                    if (cachedInnerEnergySliderRt != null)
                    {
                        cachedInnerEnergySliderRt.anchoredPosition = new Vector2(0f, -27f);
                        cachedInnerEnergySliderRt.sizeDelta = new Vector2(barWidth - 10f, 17f);
                    }
                }
            }
        }
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

        float barWidth = (rootRt != null && rootRt.sizeDelta.x > 50f) ? rootRt.sizeDelta.x : 420f;

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
                energySlider = CreateSubBar(transform, "EnergyBar", 0f, barWidth, 24f, Color.white, out energyFillImage, out energyText, out _);
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
                innerEnergySlider = CreateSubBar(transform, "InnerEnergyBar", -27f, barWidth - 10f, 17f, Color.white, out innerEnergyFillImage, out innerEnergyText, out innerEnergyOutline);
            }
        }

        ApplyHighContrastStyle(energyText);
        if (innerEnergyText != null) innerEnergyText.gameObject.SetActive(false);
    }

    private void ApplyHighContrastStyle(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        RectTransform tRt = tmp.GetComponent<RectTransform>();
        if (tRt != null)
        {
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(10f, 0f);
            tRt.offsetMax = new Vector2(-10f, 0f);
        }
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.85f, 0.96f, 1f, 1f); // Lam ngọc ánh sáng
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.40f, Color.black);
    }

    private void RepositionShardsCounter()
    {
        if (cachedShardsRt == null)
        {
            var shardsUI = FindAnyObjectByType<ShardsUI>();
            if (shardsUI != null) cachedShardsRt = shardsUI.GetComponent<RectTransform>();
        }
        if (cachedShardsRt != null)
        {
            cachedShardsRt.anchorMin = new Vector2(0f, 1f);
            cachedShardsRt.anchorMax = new Vector2(0f, 1f);
            cachedShardsRt.pivot = new Vector2(0f, 0.5f);
            cachedShardsRt.anchoredPosition = new Vector2(210f, -145f);
            cachedShardsRt.sizeDelta = new Vector2(185f, 44f);
        }
    }

    private DuneAssassinSkill duneSkill;
    private bool duneSkillChecked = false;

    private void UpdateEnergyBar()
    {
        if (energySlider == null) return;

        if (!duneSkillChecked && energySystem != null)
        {
            duneSkill = energySystem.GetComponent<DuneAssassinSkill>();
            duneSkillChecked = true;
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
                if (pct != lastDisplayedPct)
                {
                    lastDisplayedPct = pct;
                    energyText.text = $"{pct}%";
                }
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
            if (pct != lastDisplayedPct)
            {
                lastDisplayedPct = pct;
                energyText.text = $"{pct}%";
            }
        }
    }

    private void UpdateInnerEnergyBar()
    {
        if (innerEnergySlider == null) return;

        // Riêng Player 1 (Vaelstrider): Ẩn hoàn toàn thanh giáp nội năng màu vàng theo yêu cầu
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        bool hasOtherHeroSkill = energySystem != null && (energySystem.GetComponent<GaleRoninSkill>() != null || energySystem.GetComponent<AzureBladeSkill>() != null ||
                                  energySystem.GetComponent<TitanBerserkerSkill>() != null || energySystem.GetComponent<DuneAssassinSkill>() != null ||
                                  energySystem.GetComponent<CelestialMageSkill>() != null);
        bool isVaelstrider = (curHero == CharacterManager.CHAR_VAELSTRIDER || string.IsNullOrEmpty(curHero)) && !hasOtherHeroSkill;

        if (isVaelstrider)
        {
            if (innerEnergySlider.gameObject.activeSelf)
            {
                innerEnergySlider.gameObject.SetActive(false);
            }
            if (innerEnergyText != null && innerEnergyText.gameObject.activeSelf)
            {
                innerEnergyText.gameObject.SetActive(false);
            }
            return;
        }

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

        // Kiểm tra riêng cho Player 6 (Celestial Mage): Không có lớp giáp vàng, màu Tím Tinh Tú huyền ảo (cache 1 lần)
        if (!mageSkillChecked && energySystem != null)
        {
            cachedMageSkill = energySystem.GetComponent<CelestialMageSkill>() 
                ?? energySystem.GetComponentInChildren<CelestialMageSkill>() 
                ?? energySystem.GetComponentInParent<CelestialMageSkill>();
            mageSkillChecked = true;
        }
        var mage = cachedMageSkill;
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
        rootRt.sizeDelta = new Vector2(barWidth, 22f);

        Instance = rootObj.AddComponent<PlayerEnergyUI>();

        // 1. Thanh Nang Luong / Mana (Sapphire)
        Instance.energySlider = CreateSubBar(rootObj.transform, "EnergyBar", 0f, barWidth, 9f, Color.white, out Instance.energyFillImage, out Instance.energyText, out _);

        // 2. Thanh Noi Nang / Stamina (Amber Brass)
        Instance.innerEnergySlider = CreateSubBar(rootObj.transform, "InnerEnergyBar", -11f, barWidth, 7f, Color.white, out Instance.innerEnergyFillImage, out Instance.innerEnergyText, out Instance.innerEnergyOutline);

        Instance.RepositionShardsCounter();
        if (PlayerAvatarUI.Instance != null)
        {
            PlayerAvatarUI.Instance.transform.SetAsLastSibling();
        }

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
        bgImg.sprite = SoulslikeHUDStyler.GetObsidianTroughSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        outlineComponent = barObj.AddComponent<Outline>();
        outlineComponent.effectColor = new Color(0.12f, 0.08f, 0.04f, 0.8f);
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
        fillImg.sprite = (name == "EnergyBar") ? SoulslikeHUDStyler.GetSapphireFillSprite() : SoulslikeHUDStyler.GetAmberFillSprite();
        fillImg.type = Image.Type.Sliced;
        fillImg.color = Color.white;
        slider.fillRect = fillRt;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(barObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 0f);
        textRt.offsetMax = new Vector2(-8f, 0f);

        labelText = textObj.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = 26f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color(0.80f, 0.93f, 1f, 1f);
        labelText.alignment = TextAlignmentOptions.MidlineRight;
        labelText.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(labelText, 0.3f, Color.black);

        return slider;
    }
}
