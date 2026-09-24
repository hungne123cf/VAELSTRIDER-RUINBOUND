using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject resetConfirmModal;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Headers & Titles")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Volume Controls")]
    [SerializeField] private TextMeshProUGUI bgmLabel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI sfxLabel;
    [SerializeField] private Slider sfxSlider;

    [Header("Language Controls")]
    [SerializeField] private TextMeshProUGUI languageLabel;
    [SerializeField] private Button languageToggleButton;
    [SerializeField] private TextMeshProUGUI languageButtonText;

    [Header("Action Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private TextMeshProUGUI resumeBtnText;
    [SerializeField] private TextMeshProUGUI restartBtnText;
    [SerializeField] private TextMeshProUGUI mainMenuBtnText;
    [SerializeField] private TextMeshProUGUI resetDataBtnText;
    [SerializeField] private TextMeshProUGUI closeBtnText;

    [Header("Reset Confirmation Modal")]
    [SerializeField] private TextMeshProUGUI resetPromptText;
    [SerializeField] private Button resetConfirmYesBtn;
    [SerializeField] private Button resetConfirmNoBtn;
    [SerializeField] private TextMeshProUGUI resetYesBtnText;
    [SerializeField] private TextMeshProUGUI resetNoBtnText;

    public static SettingsUI EnsureUIExists(Canvas targetCanvas = null)
    {
        if (Instance == null)
        {
            Instance = Object.FindAnyObjectByType<SettingsUI>();
        }

        if (Instance == null)
        {
            Canvas canvas = targetCanvas != null ? targetCanvas : GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas == null)
            {
                canvas = Object.FindAnyObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            GameObject settingsObj = new GameObject("SettingsUI", typeof(RectTransform), typeof(CanvasRenderer));
            settingsObj.transform.SetParent(canvas.transform, false);
            Instance = settingsObj.AddComponent<SettingsUI>();
            Instance.BuildSettingsPanelDirectly();
        }
        else if (Instance.settingsPanel == null)
        {
            Instance.BuildSettingsPanelDirectly();
        }

        return Instance;
    }

    public static SettingsUI EnsureSettingsButtonExists()
    {
        var ui = EnsureUIExists();

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = Object.FindAnyObjectByType<Canvas>();

        if (rootCanvas != null)
        {
            Transform existingBtn = rootCanvas.transform.Find("Btn_SettingsGear");
            if (existingBtn == null) existingBtn = rootCanvas.transform.Find("Btn_Settings");

            if (existingBtn == null)
            {
                GameObject btnObj = new GameObject("Btn_SettingsGear", typeof(RectTransform), typeof(CanvasRenderer));
                btnObj.layer = 5;
                btnObj.transform.SetParent(rootCanvas.transform, false);

                RectTransform btnRt = btnObj.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1f, 1f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.pivot = new Vector2(1f, 1f);
                btnRt.anchoredPosition = new Vector2(-20f, -20f);
                btnRt.sizeDelta = new Vector2(50f, 50f);

                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.94f);

                var outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.75f, 0.35f, 0.85f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() =>
                {
                    if (Instance != null) Instance.OpenSettings();
                    else EnsureUIExists()?.OpenSettings();
                });

                GameObject txtObj = new GameObject("GearIconText", typeof(RectTransform), typeof(CanvasRenderer));
                txtObj.layer = 5;
                txtObj.transform.SetParent(btnObj.transform, false);

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "SET";
                tmp.fontSize = 15;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 0.9f, 0.5f);
                tmp.raycastTarget = false;
            }
        }

        return ui;
    }

    private void Awake()
    {
        Instance = this;
        if (settingsPanel == null)
        {
            BuildSettingsPanelDirectly();
        }
        InitializeSliders();
        HookListeners();
    }

    private void Start()
    {
        UpdateLocalizedTexts();
        LocalizationManager.OnLanguageChanged += UpdateLocalizedTexts;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= UpdateLocalizedTexts;
    }

    public void BuildSettingsPanelDirectly()
    {
        if (settingsPanel != null) return;

        // 1. Root Modal Panel (Overlay)
        settingsPanel = new GameObject("SettingsModalPanel", typeof(RectTransform), typeof(CanvasRenderer));
        settingsPanel.layer = 5;
        settingsPanel.transform.SetParent(transform, false);

        RectTransform overlayRt = settingsPanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        Image overlayImg = settingsPanel.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = true;

        panelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();

        // 2. Dialog Box
        GameObject dialogBox = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(settingsPanel.transform, false);

        RectTransform boxRt = dialogBox.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(500f, 540f);

        Image boxImg = dialogBox.AddComponent<Image>();
        boxImg.color = new Color(0.08f, 0.10f, 0.15f, 0.96f);

        var outline = dialogBox.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        // 3. Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(0f, 40f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.85f, 0.4f);
        titleText.text = "CÀI ĐẶT / SETTINGS";

        // 4. Content Area
        float startY = -80f;

        // BGM Slider Row
        bgmLabel = CreateLabel(dialogBox.transform, "BGMLabel", "Âm nhạc (BGM):", new Vector2(40f, startY), new Vector2(150f, 30f));
        bgmSlider = CreateSlider(dialogBox.transform, "BGMSlider", new Vector2(200f, startY), new Vector2(240f, 24f));

        startY -= 55f;
        // SFX Slider Row
        sfxLabel = CreateLabel(dialogBox.transform, "SFXLabel", "Hiệu ứng (SFX):", new Vector2(40f, startY), new Vector2(150f, 30f));
        sfxSlider = CreateSlider(dialogBox.transform, "SFXSlider", new Vector2(200f, startY), new Vector2(240f, 24f));

        startY -= 65f;
        // Language Toggle Row
        languageLabel = CreateLabel(dialogBox.transform, "LangLabel", "Ngôn ngữ:", new Vector2(40f, startY), new Vector2(150f, 30f));
        languageToggleButton = CreateStyledButton(dialogBox.transform, "Btn_LanguageToggle", new Vector2(200f, startY), new Vector2(240f, 40f), new Color(0.18f, 0.22f, 0.32f), out languageButtonText);
        languageButtonText.text = LocalizationManager.IsVietnamese ? "[VI] TIẾNG VIỆT" : "[EN] ENGLISH";

        startY -= 75f;
        // Reset Data Button
        resetDataButton = CreateStyledButton(dialogBox.transform, "Btn_ResetData", new Vector2(50f, startY), new Vector2(400f, 38f), new Color(0.35f, 0.12f, 0.12f), out resetDataBtnText);
        resetDataBtnText.text = LocalizationManager.Get("BTN_RESET_DATA");

        startY -= 50f;
        // Restart Button (Optional for gameplay)
        restartButton = CreateStyledButton(dialogBox.transform, "Btn_Restart", new Vector2(50f, startY), new Vector2(190f, 42f), new Color(0.2f, 0.25f, 0.35f), out restartBtnText);
        restartBtnText.text = LocalizationManager.Get("BTN_RESTART");

        // Main Menu Button
        mainMenuButton = CreateStyledButton(dialogBox.transform, "Btn_MainMenu", new Vector2(260f, startY), new Vector2(190f, 42f), new Color(0.2f, 0.25f, 0.35f), out mainMenuBtnText);
        mainMenuBtnText.text = LocalizationManager.Get("BTN_MAIN_MENU");

        startY -= 55f;
        // Close / Resume Button
        closeButton = CreateStyledButton(dialogBox.transform, "Btn_Close", new Vector2(50f, startY), new Vector2(400f, 44f), new Color(0.15f, 0.45f, 0.25f), out closeBtnText);
        closeBtnText.text = LocalizationManager.Get("BTN_BACK");
        resumeButton = closeButton;
        resumeBtnText = closeBtnText;

        // 5. Build Reset Confirmation Modal
        BuildResetConfirmModal(dialogBox.transform);

        settingsPanel.SetActive(false);
    }

    private void BuildResetConfirmModal(Transform parent)
    {
        resetConfirmModal = new GameObject("ResetConfirmModal", typeof(RectTransform), typeof(CanvasRenderer));
        resetConfirmModal.layer = 5;
        resetConfirmModal.transform.SetParent(parent, false);

        RectTransform rt = resetConfirmModal.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = resetConfirmModal.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);

        resetPromptText = CreateLabel(resetConfirmModal.transform, "PromptText", LocalizationManager.Get("MSG_RESET_CONFIRM"), new Vector2(30f, -120f), new Vector2(440f, 80f));
        resetPromptText.fontSize = 20;
        resetPromptText.alignment = TextAlignmentOptions.Center;

        resetConfirmYesBtn = CreateStyledButton(resetConfirmModal.transform, "Btn_ConfirmYes", new Vector2(50f, -240f), new Vector2(180f, 45f), new Color(0.6f, 0.15f, 0.15f), out resetYesBtnText);
        resetYesBtnText.text = LocalizationManager.Get("BTN_CONFIRM");

        resetConfirmNoBtn = CreateStyledButton(resetConfirmModal.transform, "Btn_ConfirmNo", new Vector2(270f, -240f), new Vector2(180f, 45f), new Color(0.2f, 0.25f, 0.35f), out resetNoBtnText);
        resetNoBtnText.text = LocalizationManager.Get("BTN_CANCEL");

        resetConfirmModal.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = new Color(0.9f, 0.92f, 0.95f);
        tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform));
        sliderObj.transform.SetParent(parent, false);
        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fill.GetComponent<RectTransform>();
        fRt.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.3f, 1f);

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(24f, 24f);
        handle.GetComponent<Image>().color = new Color(1f, 0.95f, 0.8f, 1f);

        slider.fillRect = fRt;
        slider.handleRect = hRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;

        return slider;
    }

    private Button CreateStyledButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color bgColor, out TextMeshProUGUI btnText)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        btnText = txtObj.AddComponent<TextMeshProUGUI>();
        btnText.fontSize = 18;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.raycastTarget = false;

        return btn;
    }

    private void InitializeSliders()
    {
        if (SoundManager.Instance != null)
        {
            if (bgmSlider != null) bgmSlider.value = SoundManager.Instance.GetBGMVolume();
            if (sfxSlider != null) sfxSlider.value = SoundManager.Instance.GetSFXVolume();
        }
        else
        {
            if (bgmSlider != null) bgmSlider.value = PlayerPrefs.GetFloat("Game_BGM_Volume", 0.7f);
            if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("Game_SFX_Volume", 0.8f);
        }
    }

    private void HookListeners()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener((val) => {
                if (SoundManager.Instance != null) SoundManager.Instance.SetBGMVolume(val);
                else
                {
                    PlayerPrefs.SetFloat("Game_BGM_Volume", val);
                    PlayerPrefs.Save();
                }
            });
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener((val) => {
                if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(val);
                else
                {
                    PlayerPrefs.SetFloat("Game_SFX_Volume", val);
                    PlayerPrefs.Save();
                }
            });
        }

        if (languageToggleButton != null)
        {
            languageToggleButton.onClick.RemoveAllListeners();
            languageToggleButton.onClick.AddListener(OnLanguageToggleClicked);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(CloseSettings);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        if (resetDataButton != null)
        {
            resetDataButton.onClick.RemoveAllListeners();
            resetDataButton.onClick.AddListener(OnResetDataClicked);
        }
        if (closeButton != null && closeButton != resumeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
        }

        if (resetConfirmYesBtn != null)
        {
            resetConfirmYesBtn.onClick.RemoveAllListeners();
            resetConfirmYesBtn.onClick.AddListener(OnConfirmResetYes);
        }
        if (resetConfirmNoBtn != null)
        {
            resetConfirmNoBtn.onClick.RemoveAllListeners();
            resetConfirmNoBtn.onClick.AddListener(OnConfirmResetNo);
        }
    }

    public void UpdateLocalizedTexts()
    {
        if (titleText != null) titleText.text = LocalizationManager.Get("SETTINGS_TITLE");
        if (bgmLabel != null) bgmLabel.text = LocalizationManager.Get("LBL_BGM");
        if (sfxLabel != null) sfxLabel.text = LocalizationManager.Get("LBL_SFX");
        if (languageLabel != null) languageLabel.text = LocalizationManager.Get("LBL_LANGUAGE");

        if (languageButtonText != null)
        {
            languageButtonText.text = LocalizationManager.IsVietnamese ? "[VI] TIẾNG VIỆT" : "[EN] ENGLISH";
        }

        string curScene = SceneManager.GetActiveScene().name;
        bool isGameplay = curScene.Contains("Map") || curScene == "SampleScene";

        if (restartButton != null) restartButton.gameObject.SetActive(isGameplay);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(isGameplay);

        if (resumeBtnText != null) resumeBtnText.text = isGameplay ? LocalizationManager.Get("BTN_RESUME") : LocalizationManager.Get("BTN_BACK");
        if (restartBtnText != null) restartBtnText.text = LocalizationManager.Get("BTN_RESTART");
        if (mainMenuBtnText != null) mainMenuBtnText.text = LocalizationManager.Get("BTN_MAIN_MENU");
        if (resetDataBtnText != null) resetDataBtnText.text = LocalizationManager.Get("BTN_RESET_DATA");
        if (closeBtnText != null) closeBtnText.text = LocalizationManager.Get("BTN_BACK");

        if (resetPromptText != null) resetPromptText.text = LocalizationManager.Get("MSG_RESET_CONFIRM");
        if (resetYesBtnText != null) resetYesBtnText.text = LocalizationManager.Get("BTN_CONFIRM");
        if (resetNoBtnText != null) resetNoBtnText.text = LocalizationManager.Get("BTN_CANCEL");
    }

    public void OpenSettings()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();
        if (settingsPanel == null) BuildSettingsPanelDirectly();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
        InitializeSliders();
        HookListeners();
        UpdateLocalizedTexts();
    }

    public void CloseSettings()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuClose();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
    }

    private void OnLanguageToggleClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        LocalizationManager.ToggleLanguage();
        UpdateLocalizedTexts();
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.UpdateAllTexts();
        }
    }

    private void OnRestartClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void OnMainMenuClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnResetDataClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (resetConfirmModal != null)
        {
            resetConfirmModal.SetActive(true);
            resetConfirmModal.transform.SetAsLastSibling();
        }
    }

    private void OnConfirmResetYes()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
        UpdateLocalizedTexts();
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.UpdateCurrencyDisplay();
            MainMenuManager.Instance.UpdateAllTexts();
        }
    }

    private void OnConfirmResetNo()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
    }
}
