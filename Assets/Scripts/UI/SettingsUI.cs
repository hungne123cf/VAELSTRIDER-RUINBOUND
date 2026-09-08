using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject guidePanel;

    [Header("Controls")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button langViBtn;
    [SerializeField] private Button langEnBtn;

    [Header("Text Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bgmLabel;
    [SerializeField] private TextMeshProUGUI sfxLabel;
    [SerializeField] private TextMeshProUGUI langLabel;
    [SerializeField] private TextMeshProUGUI guideBtnText;
    [SerializeField] private TextMeshProUGUI restartBtnText;
    [SerializeField] private TextMeshProUGUI resumeBtnText;
    [SerializeField] private TextMeshProUGUI exitBtnText;

    private bool isOpen = false;

    void Awake()
    {
        Instance = this;
    }

    public static SettingsUI EnsureInstance() => GetOrCreate();

    public static SettingsUI GetOrCreate()
    {
        if (Instance != null) return Instance;

        var all = Resources.FindObjectsOfTypeAll<SettingsUI>();
        foreach (var s in all)
        {
            if (s != null && s.gameObject.scene.isLoaded)
            {
                Instance = s;
                return Instance;
            }
        }

        EnsureSettingsButtonExists();

        if (Instance != null) return Instance;

        all = Resources.FindObjectsOfTypeAll<SettingsUI>();
        foreach (var s in all)
        {
            if (s != null && s.gameObject.scene.isLoaded)
            {
                Instance = s;
                return Instance;
            }
        }
        return Instance;
    }

    void Start()
    {
        if (bgmSlider != null)
        {
            bgmSlider.value = SoundManager.Instance != null ? SoundManager.Instance.MusicVolume : 0.8f;
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = SoundManager.Instance != null ? SoundManager.Instance.SFXVolume : 0.85f;
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        LocalizationManager.OnLanguageChanged += RefreshLabels;
        RefreshLabels();
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLabels;
    }

    public void ShowSettings()
    {
        if (!isOpen) ToggleSettings();
    }

    public void ToggleSettings()
    {
        isOpen = !isOpen;
        if (settingsPanel == null) settingsPanel = gameObject;

        if (settingsPanel != null)
        {
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(isOpen);
        }
        if (guidePanel != null && !isOpen) guidePanel.SetActive(false);

        string curScene = SceneManager.GetActiveScene().name; bool isLogin = curScene == "LoginScene" || curScene == "MainMenuScene";
        Time.timeScale = (isOpen && !isLogin) ? 0f : 1f;
        if (isOpen) RefreshLabels();
    }

    public void ShowGuide()
    {
        if (guidePanel != null) guidePanel.SetActive(true);
    }

    public void CloseGuide()
    {
        if (guidePanel != null) guidePanel.SetActive(false);
    }

    private void OnBgmChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.MusicVolume = val;
    }

    private void OnSfxChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SFXVolume = val;
    }

    public void SetLanguage(int langIndex)
    {
        LocalizationManager.CurrentLanguage = (LocalizationManager.Language)langIndex;
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToLogin()
    {
        Time.timeScale = 1f;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySwordSlash();
        SceneManager.LoadScene("LoginScene");
    }

    private void RefreshLabels()
    {
        if (titleText != null) titleText.text = LocalizationManager.Get("SETTINGS_TITLE");
        if (bgmLabel != null) bgmLabel.text = LocalizationManager.Get("AUDIO_BGM");
        if (sfxLabel != null) sfxLabel.text = LocalizationManager.Get("AUDIO_SFX");
        if (langLabel != null) langLabel.text = LocalizationManager.Get("LANGUAGE");
        if (guideBtnText != null) guideBtnText.text = LocalizationManager.Get("BTN_GUIDE");
        if (restartBtnText != null) restartBtnText.text = LocalizationManager.Get("BTN_RESTART");
        if (resumeBtnText != null) resumeBtnText.text = LocalizationManager.Get("BTN_RESUME");
        if (exitBtnText != null) exitBtnText.text = LocalizationManager.Get("BTN_EXIT_LOGIN");
    }

    public static void EnsureSettingsButtonExists()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existingBtn = canvas.transform.Find("Btn_SettingsGear");
        if (existingBtn != null) return;

        // 1. Create Gear Button on Top Right (next to pause/stats)
        GameObject btnObj = new GameObject("Btn_SettingsGear");
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 1f);
        btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.pivot = new Vector2(1f, 1f);
        btnRt.anchoredPosition = new Vector2(-25f, -25f);
        btnRt.sizeDelta = new Vector2(58f, 58f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.14f, 0.2f, 0.92f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject iconObj = new GameObject("GearIcon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = Vector2.zero;
        var iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
        iconTmp.text = "[*]";
        iconTmp.fontSize = 28;
        iconTmp.fontStyle = FontStyles.Bold;
        iconTmp.color = new Color(1f, 0.88f, 0.4f);
        iconTmp.alignment = TextAlignmentOptions.Center;
        iconTmp.raycastTarget = false;

        // 2. Create Settings Window Panel
        GameObject panelObj = new GameObject("SettingsPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f); // Dim overlay

        // Settings Card
        GameObject cardObj = new GameObject("Card");
        cardObj.transform.SetParent(panelObj.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(640f, 530f);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.11f, 0.12f, 0.18f, 0.98f);
        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        var settingsUI = panelObj.AddComponent<SettingsUI>();
        settingsUI.settingsPanel = panelObj;
        Instance = settingsUI;
        btn.onClick.AddListener(settingsUI.ToggleSettings);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 215f);
        titleRt.sizeDelta = new Vector2(500f, 40f);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "CAI DAT HE THONG";
        titleTmp.fontSize = 28;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.85f, 0.35f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        settingsUI.titleText = titleTmp;

        // Sliders & Controls
        settingsUI.bgmSlider = CreateSliderRow(cardObj.transform, "BGM", "Nhac Nen (BGM)", 140f, out settingsUI.bgmLabel);
        settingsUI.sfxSlider = CreateSliderRow(cardObj.transform, "SFX", "Hieu Ung (SFX)", 80f, out settingsUI.sfxLabel);

        // Language Row
        CreateLanguageRow(cardObj.transform, 20f, settingsUI, out settingsUI.langLabel);

        // Row 1 Action Buttons: Resume & Guide
        CreateActionButton(cardObj.transform, "Btn_Resume", "TIEP TUC", new Vector2(-135f, -65f), new Vector2(240f, 46f), new Color(0.2f, 0.55f, 0.35f), () => settingsUI.ToggleSettings(), out settingsUI.resumeBtnText);
        CreateActionButton(cardObj.transform, "Btn_Guide", "HUONG DAN CHOI", new Vector2(135f, -65f), new Vector2(240f, 46f), new Color(0.25f, 0.4f, 0.75f), () => settingsUI.ShowGuide(), out settingsUI.guideBtnText);

        // Row 2 Action Buttons: Restart & Exit to Login!
        CreateActionButton(cardObj.transform, "Btn_Restart", "CHOI LAI TU DAU", new Vector2(-135f, -135f), new Vector2(240f, 46f), new Color(0.7f, 0.4f, 0.2f), () => settingsUI.RestartRun(), out settingsUI.restartBtnText);
        CreateActionButton(cardObj.transform, "Btn_ExitToLogin", "VE TRANG DANG NHAP", new Vector2(135f, -135f), new Vector2(240f, 46f), new Color(0.75f, 0.22f, 0.22f), () => settingsUI.ExitToLogin(), out settingsUI.exitBtnText);

        // Row 3 Action Button: Reset All Progress (Hardcore Mode)
        CreateActionButton(cardObj.transform, "Btn_ResetGameProgress", "💀 RESET GAME (XÓA TIẾN TRÌNH)", new Vector2(0f, -195f), new Vector2(510f, 44f), new Color(0.68f, 0.12f, 0.16f), () => {
            PlayerUpgradeManager.ResetEntireGameProgress();
            Time.timeScale = 1f;
            SceneManager.LoadScene("LoginScene");
        }, out _);

        // Guide Sub-Panel
        settingsUI.guidePanel = CreateGuidePanel(canvas.transform, settingsUI);
        settingsUI.guidePanel.SetActive(false);

        panelObj.SetActive(false);
    }

    private static Slider CreateSliderRow(Transform parent, string name, string label, float posY, out TextMeshProUGUI labelTmp)
    {
        GameObject row = new GameObject("Row_" + name);
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, posY);
        rt.sizeDelta = new Vector2(520f, 40f);

        GameObject lblObj = new GameObject("Label");
        lblObj.transform.SetParent(row.transform, false);
        RectTransform lblRt = lblObj.AddComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0.5f);
        lblRt.anchorMax = new Vector2(0f, 0.5f);
        lblRt.pivot = new Vector2(0f, 0.5f);
        lblRt.sizeDelta = new Vector2(220f, 35f);
        labelTmp = lblObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 18;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.Left;

        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sRt = sliderObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(1f, 0.5f);
        sRt.anchorMax = new Vector2(1f, 0.5f);
        sRt.pivot = new Vector2(1f, 0.5f);
        sRt.sizeDelta = new Vector2(250f, 24f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.22f, 0.28f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fillObj.AddComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.sizeDelta = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.75f, 0.95f);
        slider.fillRect = fRt;

        return slider;
    }

    private static void CreateLanguageRow(Transform parent, float posY, SettingsUI ui, out TextMeshProUGUI langLabelTmp)
    {
        GameObject row = new GameObject("Row_Language");
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, posY);
        rt.sizeDelta = new Vector2(520f, 40f);

        GameObject lblObj = new GameObject("Label");
        lblObj.transform.SetParent(row.transform, false);
        RectTransform lblRt = lblObj.AddComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0.5f);
        lblRt.anchorMax = new Vector2(0f, 0.5f);
        lblRt.pivot = new Vector2(0f, 0.5f);
        lblRt.sizeDelta = new Vector2(220f, 35f);
        langLabelTmp = lblObj.AddComponent<TextMeshProUGUI>();
        langLabelTmp.text = "Ngon Ngu";
        langLabelTmp.fontSize = 18;
        langLabelTmp.color = Color.white;
        langLabelTmp.alignment = TextAlignmentOptions.Left;

        // VI Button
        CreateSmallButton(row.transform, "Btn_VI", "Tieng Viet", new Vector2(90f, 0f), () => ui.SetLanguage(0));
        // EN Button
        CreateSmallButton(row.transform, "Btn_EN", "English", new Vector2(215f, 0f), () => ui.SetLanguage(1));
    }

    private static void CreateActionButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, System.Action onClick, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        btnTxt = txtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = text;
        btnTxt.fontSize = 16;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.raycastTarget = false;
    }

    private static void CreateSmallButton(Transform parent, string name, string text, Vector2 pos, System.Action onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(110f, 36f);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.3f, 0.4f);
        Button btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private static GameObject CreateGuidePanel(Transform canvas, SettingsUI ui)
    {
        GameObject panelObj = new GameObject("GuidePanel");
        panelObj.transform.SetParent(canvas, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.03f, 0.05f, 0.95f);

        GameObject cardObj = new GameObject("GuideCard");
        cardObj.transform.SetParent(panelObj.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(760f, 540f);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.12f, 0.14f, 0.2f);

        // Guide Title
        GameObject titleObj = new GameObject("GuideTitle");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 220f);
        titleRt.sizeDelta = new Vector2(700f, 45f);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = LocalizationManager.Get("GUIDE_TITLE");
        titleTmp.fontSize = 26;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.85f, 0.3f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Guide Items
        string[] guideKeys = new string[]
        {
            "GUIDE_MOVE", "GUIDE_ATTACK", "GUIDE_DASH", "GUIDE_SKILL", "GUIDE_POTION", "GUIDE_SHRINE"
        };

        float startY = 145f;
        for (int i = 0; i < guideKeys.Length; i++)
        {
            GameObject gObj = new GameObject("GuideItem_" + i);
            gObj.transform.SetParent(cardObj.transform, false);
            RectTransform gRt = gObj.AddComponent<RectTransform>();
            gRt.anchoredPosition = new Vector2(0f, startY - (i * 48f));
            gRt.sizeDelta = new Vector2(680f, 40f);
            var gTmp = gObj.AddComponent<TextMeshProUGUI>();
            gTmp.text = LocalizationManager.Get(guideKeys[i]);
            gTmp.fontSize = 17;
            gTmp.color = new Color(0.9f, 0.92f, 0.96f);
            gTmp.alignment = TextAlignmentOptions.Left;
        }

        // Close Guide Button
        CreateActionButton(cardObj.transform, "Btn_CloseGuide", "DONG", new Vector2(0f, -205f), new Vector2(200f, 45f), new Color(0.7f, 0.25f, 0.25f), () => ui.CloseGuide(), out _);

        return panelObj;
    }
}
