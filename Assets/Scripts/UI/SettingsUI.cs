using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    public enum SettingsTab
    {
        AudioLang = 0,
        Controls = 1,
        GameData = 2
    }

    private SettingsTab currentTab = SettingsTab.AudioLang;

    [Header("UI Structure")]
    private CanvasGroup canvasGroup;
    private GameObject dialogBox;
    private GameObject resetConfirmModal;

    [Header("Headers & Titles")]
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;

    [Header("Category Navigation Sidebar")]
    private GameObject categorySidebar;
    private TextMeshProUGUI sidebarTitleText;
    private Button tabAudioBtn;
    private TextMeshProUGUI tabAudioText;
    private Button tabControlsBtn;
    private TextMeshProUGUI tabControlsText;
    private Button tabDataBtn;
    private TextMeshProUGUI tabDataText;

    [Header("Content Panels (Pages)")]
    private GameObject contentPanel;
    private GameObject pageAudioLang;
    private GameObject pageControls;
    private GameObject pageGameData;

    [Header("Page Titles & Descriptions")]
    private TextMeshProUGUI pageAudioHeader;
    private TextMeshProUGUI pageAudioSub;
    private TextMeshProUGUI pageControlsHeader;
    private TextMeshProUGUI pageControlsSub;
    private TextMeshProUGUI pageDataHeader;
    private TextMeshProUGUI pageDataSub;

    [Header("Volume Controls")]
    private TextMeshProUGUI bgmLabel;
    private Slider bgmSlider;
    private TextMeshProUGUI bgmValueText;
    private TextMeshProUGUI sfxLabel;
    private Slider sfxSlider;
    private TextMeshProUGUI sfxValueText;

    [Header("Language Controls")]
    private TextMeshProUGUI languageLabel;
    private Button languageToggleButton;
    private TextMeshProUGUI languageButtonText;

    [Header("Control Settings")]
    private GameObject controlsCard;
    private TextMeshProUGUI controlsCardTitle;
    private TextMeshProUGUI controlsCardDesc;
    private TextMeshProUGUI controlsCardTip;
    private Button customizeLayoutBtn;
    private TextMeshProUGUI customizeLayoutBtnText;
    private Button resetControlsDefaultBtn;
    private TextMeshProUGUI resetControlsDefaultBtnText;

    [Header("Game Data Controls")]
    private TextMeshProUGUI resetDataDesc;
    private Button resetDataButton;
    private TextMeshProUGUI resetDataBtnText;
    private TextMeshProUGUI mainMenuDesc;
    private Button mainMenuButton;
    private TextMeshProUGUI mainMenuBtnText;
    private TextMeshProUGUI offlineNoteText;

    [Header("Footer Controls")]
    private Button closeButton;
    private TextMeshProUGUI closeBtnText;

    [Header("Reset Confirmation Modal")]
    private TextMeshProUGUI resetPromptText;
    private Button resetConfirmYesBtn;
    private Button resetConfirmNoBtn;
    private TextMeshProUGUI resetYesBtnText;
    private TextMeshProUGUI resetNoBtnText;

    private bool isGameplayScene => SceneManager.GetActiveScene().name.Contains("Map") || SceneManager.GetActiveScene().name == "SampleScene";

    public static SettingsUI EnsureUIExists(Canvas targetCanvas = null)
    {
        if (Instance != null && Instance.gameObject != null)
        {
            return Instance;
        }

        Instance = Object.FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (Instance != null)
        {
            return Instance;
        }

        Canvas rootCanvas = targetCanvas != null ? targetCanvas : GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null)
        {
            rootCanvas = Object.FindAnyObjectByType<Canvas>();
        }

        if (rootCanvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasObj.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        Transform existing = rootCanvas.transform.Find("SettingsModal");
        if (existing == null) existing = rootCanvas.transform.Find("SettingsUI");

        if (existing != null)
        {
            Instance = existing.GetComponent<SettingsUI>();
            if (Instance == null) Instance = existing.gameObject.AddComponent<SettingsUI>();
            if (!Application.isPlaying)
            {
                Instance.EnsureFullscreenOverlay();
                Instance.BuildUI();
            }
            existing.gameObject.SetActive(false);
            return Instance;
        }

        GameObject settingsObj = new GameObject("SettingsModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        settingsObj.layer = 5;
        settingsObj.transform.SetParent(rootCanvas.transform, false);

        RectTransform sRt = settingsObj.GetComponent<RectTransform>();
        sRt.anchorMin = Vector2.zero;
        sRt.anchorMax = Vector2.one;
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.offsetMin = Vector2.zero;
        sRt.offsetMax = Vector2.zero;

        Image bgImg = settingsObj.GetComponent<Image>();
        bgImg.color = new Color(0.03f, 0.04f, 0.07f, 0.94f);
        bgImg.raycastTarget = true;

        Instance = settingsObj.AddComponent<SettingsUI>();
        if (!Application.isPlaying)
        {
            Instance.BuildUI();
        }
        settingsObj.SetActive(false);
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
            if (existingBtn == null) existingBtn = rootCanvas.transform.Find("Btn_InGameSettings");

            if (existingBtn != null)
            {
                Button btn = existingBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (Instance != null) Instance.OpenSettings();
                        else EnsureUIExists()?.OpenSettings();
                    });
                }
            }
            else
            {
                GameObject btnObj = new GameObject("Btn_SettingsGear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnObj.layer = 5;
                btnObj.transform.SetParent(rootCanvas.transform, false);

                RectTransform btnRt = btnObj.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1f, 1f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.pivot = new Vector2(1f, 1f);
                btnRt.anchoredPosition = new Vector2(-20f, -20f);
                btnRt.sizeDelta = new Vector2(46f, 46f);

                Image btnImg = btnObj.GetComponent<Image>();
                btnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.94f);

                var outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.8f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                Button btn = btnObj.GetComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() =>
                {
                    if (Instance != null) Instance.OpenSettings();
                    else EnsureUIExists()?.OpenSettings();
                });

                WhiteIconLoader.ReplaceTextWithIcon(btn, "Gear", new Color(1f, 0.88f, 0.45f), new Vector2(28f, 28f));
            }
        }

        return ui;
    }

    private void Awake()
    {
        Instance = this;
        gameObject.layer = 5;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureFullscreenOverlay();
        BuildUI();
        gameObject.SetActive(false);
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

    private void EnsureFullscreenOverlay()
    {
        RectTransform myRt = GetComponent<RectTransform>();
        if (myRt != null)
        {
            myRt.anchorMin = Vector2.zero;
            myRt.anchorMax = Vector2.one;
            myRt.pivot = new Vector2(0.5f, 0.5f);
            myRt.offsetMin = Vector2.zero;
            myRt.offsetMax = Vector2.zero;
        }

        Image img = GetComponent<Image>();
        if (img == null) img = gameObject.AddComponent<Image>();
        img.color = new Color(0.03f, 0.04f, 0.07f, 0.94f);
        img.raycastTarget = true;
    }

    public void BuildUI()
    {
        // Clean up old children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        // 1. Center Dialog Box
        dialogBox = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(transform, false);

        RectTransform boxRt = dialogBox.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(1080f, 610f);

        Image boxImg = dialogBox.GetComponent<Image>();
        boxImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        boxImg.type = Image.Type.Sliced;
        boxImg.color = Color.white;
        WhiteIconLoader.ApplyGothicFrame(dialogBox, Color.white, 0.40f);

        // 2. Header Title & Subtitle
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(0f, 36f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.98f, 0.88f, 0.55f);
        titleText.text = "<b>CÀI ĐẶT HỆ THỐNG</b>";

        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.transform.SetParent(dialogBox.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -56f);
        subRt.sizeDelta = new Vector2(0f, 22f);

        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 15f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.80f, 0.85f, 0.95f, 0.85f);
        subtitleText.text = LocalizationManager.IsVietnamese ? "Âm thanh, Điều khiển & Dữ liệu trò chơi" : "Audio, Controls & Game Data";
        SoulslikeHUDStyler.StyleHeader(titleText, subtitleText);
        SoulslikeHUDStyler.AddFiligreeDivider(dialogBox.transform, new Vector2(0f, -78f), new Vector2(720f, 12f));

        // 3. LEFT CATEGORY SIDEBAR
        categorySidebar = new GameObject("CategorySidebar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        categorySidebar.transform.SetParent(dialogBox.transform, false);
        RectTransform sbRt = categorySidebar.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0f);
        sbRt.anchorMax = new Vector2(0f, 1f);
        sbRt.pivot = new Vector2(0f, 0.5f);
        sbRt.offsetMin = new Vector2(30f, 85f);
        sbRt.offsetMax = new Vector2(285f, -95f);

        Image sbImg = categorySidebar.GetComponent<Image>();
        sbImg.color = new Color(0.05f, 0.07f, 0.11f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(categorySidebar, new Color(0.35f, 0.50f, 0.70f, 0.65f), 0.50f);

        sidebarTitleText = CreateLabel(categorySidebar.transform, "SidebarTitle", "<b>DANH MỤC</b>", new Vector2(20f, -15f), new Vector2(215f, 26f));
        sidebarTitleText.fontSize = 15;
        sidebarTitleText.color = new Color(0.98f, 0.85f, 0.40f);
        sidebarTitleText.fontStyle = FontStyles.Bold;

        SoulslikeHUDStyler.AddFiligreeDivider(categorySidebar.transform, new Vector2(0f, -42f), new Vector2(220f, 8f));

        // Category Tab Buttons
        tabAudioBtn = CreateStyledButton(categorySidebar.transform, "Btn_Tab_Audio", new Vector2(15f, -55f), new Vector2(225f, 60f), new Color(0.08f, 0.10f, 0.15f), out tabAudioText);
        tabControlsBtn = CreateStyledButton(categorySidebar.transform, "Btn_Tab_Controls", new Vector2(15f, -128f), new Vector2(225f, 60f), new Color(0.08f, 0.10f, 0.15f), out tabControlsText);
        tabDataBtn = CreateStyledButton(categorySidebar.transform, "Btn_Tab_Data", new Vector2(15f, -201f), new Vector2(225f, 60f), new Color(0.08f, 0.10f, 0.15f), out tabDataText);

        // 4. RIGHT CONTENT PANEL
        contentPanel = new GameObject("ContentPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentPanel.transform.SetParent(dialogBox.transform, false);
        RectTransform cpRt = contentPanel.GetComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0f, 0f);
        cpRt.anchorMax = new Vector2(1f, 1f);
        cpRt.pivot = new Vector2(0.5f, 0.5f);
        cpRt.offsetMin = new Vector2(300f, 85f);
        cpRt.offsetMax = new Vector2(-30f, -95f);

        Image cpImg = contentPanel.GetComponent<Image>();
        cpImg.color = new Color(0.06f, 0.08f, 0.13f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(contentPanel, new Color(0.65f, 0.55f, 0.35f, 0.65f), 0.55f);

        // --- PAGE 1: AUDIO & LANGUAGE ---
        pageAudioLang = CreatePage(contentPanel.transform, "Page_AudioLang");
        pageAudioHeader = CreateLabel(pageAudioLang.transform, "Header", "<b><color=#55EFC4>1. ÂM THANH & NGÔN NGỮ</color></b>", new Vector2(25f, -18f), new Vector2(650f, 28f));
        pageAudioHeader.fontSize = 18;
        pageAudioSub = CreateLabel(pageAudioLang.transform, "Sub", "Tùy chỉnh âm lượng nhạc nền, hiệu ứng âm thanh và ngôn ngữ hiển thị", new Vector2(25f, -46f), new Vector2(650f, 22f));
        pageAudioSub.fontSize = 13.5f;
        pageAudioSub.color = new Color(0.75f, 0.82f, 0.92f, 0.8f);
        SoulslikeHUDStyler.AddFiligreeDivider(pageAudioLang.transform, new Vector2(0f, -70f), new Vector2(680f, 8f));

        bgmLabel = CreateLabel(pageAudioLang.transform, "BGMLabel", "Âm nhạc nền (BGM):", new Vector2(25f, -85f), new Vector2(500f, 24f));
        bgmLabel.fontSize = 16;
        bgmSlider = CreateSlider(pageAudioLang.transform, "BGMSlider", new Vector2(25f, -114f), new Vector2(570f, 28f));
        bgmValueText = CreateLabel(pageAudioLang.transform, "BGMVal", "80%", new Vector2(610f, -114f), new Vector2(80f, 28f));
        bgmValueText.fontSize = 17;
        bgmValueText.fontStyle = FontStyles.Bold;
        bgmValueText.color = new Color(1f, 0.85f, 0.35f);

        sfxLabel = CreateLabel(pageAudioLang.transform, "SFXLabel", "Hiệu ứng âm thanh (SFX):", new Vector2(25f, -160f), new Vector2(500f, 24f));
        sfxLabel.fontSize = 16;
        sfxSlider = CreateSlider(pageAudioLang.transform, "SFXSlider", new Vector2(25f, -189f), new Vector2(570f, 28f));
        sfxValueText = CreateLabel(pageAudioLang.transform, "SFXVal", "80%", new Vector2(610f, -189f), new Vector2(80f, 28f));
        sfxValueText.fontSize = 17;
        sfxValueText.fontStyle = FontStyles.Bold;
        sfxValueText.color = new Color(1f, 0.85f, 0.35f);

        languageLabel = CreateLabel(pageAudioLang.transform, "LangLabel", "Ngôn ngữ hiển thị (Display Language):", new Vector2(25f, -238f), new Vector2(500f, 24f));
        languageLabel.fontSize = 16;
        languageToggleButton = CreateStyledButton(pageAudioLang.transform, "Btn_LanguageToggle", new Vector2(25f, -268f), new Vector2(570f, 50f), new Color(0.16f, 0.24f, 0.38f), out languageButtonText);
        languageButtonText.fontSize = 17;

        // --- PAGE 2: TOUCH CONTROLS ---
        pageControls = CreatePage(contentPanel.transform, "Page_Controls");
        pageControlsHeader = CreateLabel(pageControls.transform, "Header", "<b><color=#55EFC4>2. PHÍM ĐIỀU KHIỂN CẢM ỨNG</color></b>", new Vector2(25f, -16f), new Vector2(650f, 26f));
        pageControlsHeader.fontSize = 18;
        pageControlsSub = CreateLabel(pageControls.transform, "Sub", "Kéo thả trực tiếp vị trí các phím trên màn hình trận đấu", new Vector2(25f, -42f), new Vector2(650f, 20f));
        pageControlsSub.fontSize = 13.5f;
        pageControlsSub.color = new Color(0.75f, 0.82f, 0.92f, 0.8f);
        SoulslikeHUDStyler.AddFiligreeDivider(pageControls.transform, new Vector2(0f, -64f), new Vector2(680f, 8f));

        // Showcase Container Card
        controlsCard = new GameObject("ControlsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        controlsCard.transform.SetParent(pageControls.transform, false);
        RectTransform ccRt = controlsCard.GetComponent<RectTransform>();
        ccRt.anchorMin = new Vector2(0f, 1f);
        ccRt.anchorMax = new Vector2(0f, 1f);
        ccRt.pivot = new Vector2(0f, 1f);
        ccRt.anchoredPosition = new Vector2(25f, -76f);
        ccRt.sizeDelta = new Vector2(670f, 235f);

        Image ccImg = controlsCard.GetComponent<Image>();
        ccImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        ccImg.type = Image.Type.Sliced;
        ccImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
        WhiteIconLoader.ApplyGothicFrame(controlsCard, new Color(0.35f, 0.55f, 0.75f, 0.55f), 0.45f);

        // Title inside Card
        controlsCardTitle = CreateLabel(controlsCard.transform, "CardTitle", "<b>KÉO THẢ PHÍM TRỰC TIẾP TRONG TRẬN ĐẤU</b>", new Vector2(22f, -18f), new Vector2(626f, 26f));
        controlsCardTitle.fontSize = 16.5f;
        controlsCardTitle.color = new Color(1f, 0.88f, 0.45f);

        // Description inside Card
        controlsCardDesc = CreateLabel(controlsCard.transform, "CardDesc", "Chạm và kéo tự do Cần di chuyển (Joystick), Nút Đánh, Lướt, Tuyệt kỹ & Bình máu trực tiếp trên màn hình trận đấu để phù hợp nhất với cỡ tay của bạn.\n\nVị trí mới sẽ được lưu tự động và áp dụng vĩnh viễn cho tất cả các trận đấu!", new Vector2(22f, -50f), new Vector2(626f, 85f));
        controlsCardDesc.fontSize = 14f;
        controlsCardDesc.color = new Color(0.82f, 0.88f, 0.95f, 0.9f);

        // 2 Big Action Buttons inside Card
        customizeLayoutBtn = CreateStyledButton(controlsCard.transform, "Btn_CustomizeLayout", new Vector2(22f, -150f), new Vector2(370f, 54f), new Color(0.10f, 0.38f, 0.42f), out customizeLayoutBtnText);
        customizeLayoutBtnText.fontSize = 15.5f;

        resetControlsDefaultBtn = CreateStyledButton(controlsCard.transform, "Btn_ResetControlsDefault", new Vector2(405f, -150f), new Vector2(243f, 54f), new Color(0.26f, 0.14f, 0.18f), out resetControlsDefaultBtnText);
        resetControlsDefaultBtnText.fontSize = 15f;

        // Bottom Tip below Card
        controlsCardTip = CreateLabel(pageControls.transform, "CardTip", "<i><color=#88A0C0>* Trận đấu sẽ tự động tạm dừng an toàn trong lúc bạn căn chỉnh vị trí các nút.</color></i>", new Vector2(25f, -320f), new Vector2(670f, 24f));
        controlsCardTip.fontSize = 13.5f;

        // --- PAGE 3: GAME DATA & SYSTEM ---
        pageGameData = CreatePage(contentPanel.transform, "Page_GameData");
        pageDataHeader = CreateLabel(pageGameData.transform, "Header", "<b><color=#FFA502>3. DỮ LIỆU TRÒ CHƠI & HỆ THỐNG</color></b>", new Vector2(25f, -18f), new Vector2(650f, 28f));
        pageDataHeader.fontSize = 18;
        pageDataSub = CreateLabel(pageGameData.transform, "Sub", "Quản lý tiến trình trò chơi ngoại tuyến và thao tác thoát trận", new Vector2(25f, -46f), new Vector2(650f, 22f));
        pageDataSub.fontSize = 13.5f;
        pageDataSub.color = new Color(0.75f, 0.82f, 0.92f, 0.8f);
        SoulslikeHUDStyler.AddFiligreeDivider(pageGameData.transform, new Vector2(0f, -70f), new Vector2(680f, 8f));

        resetDataDesc = CreateLabel(pageGameData.transform, "ResetDesc", "Xóa toàn bộ dữ liệu lưu trữ để bắt đầu một hành trình mới:", new Vector2(25f, -85f), new Vector2(650f, 24f));
        resetDataDesc.fontSize = 15;
        resetDataButton = CreateStyledButton(pageGameData.transform, "Btn_ResetData", new Vector2(25f, -114f), new Vector2(570f, 50f), new Color(0.38f, 0.14f, 0.16f), out resetDataBtnText);
        resetDataBtnText.fontSize = 16.5f;

        mainMenuDesc = CreateLabel(pageGameData.transform, "MainMenuDesc", "Thoát khỏi màn chơi hiện tại và quay về sảnh chính:", new Vector2(25f, -180f), new Vector2(650f, 24f));
        mainMenuDesc.fontSize = 15;
        mainMenuButton = CreateStyledButton(pageGameData.transform, "Btn_MainMenu", new Vector2(25f, -209f), new Vector2(570f, 50f), new Color(0.2f, 0.25f, 0.35f), out mainMenuBtnText);
        mainMenuBtnText.fontSize = 16.5f;

        offlineNoteText = CreateLabel(pageGameData.transform, "OfflineNote", "", new Vector2(25f, -280f), new Vector2(660f, 40f));
        offlineNoteText.fontSize = 14f;

        // 5. BOTTOM ROW: CLOSE BUTTON
        closeButton = CreateStyledButton(dialogBox.transform, "Btn_Close", Vector2.zero, new Vector2(360f, 48f), new Color(0.12f, 0.48f, 0.24f), out closeBtnText);
        RectTransform cbRt = closeButton.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.5f, 0f);
        cbRt.anchorMax = new Vector2(0.5f, 0f);
        cbRt.pivot = new Vector2(0.5f, 0.5f);
        cbRt.anchoredPosition = new Vector2(0f, 42f);
        closeBtnText.fontSize = 19;

        // 6. Reset Confirmation Modal
        BuildResetConfirmModal(dialogBox.transform);

        InitializeSliders();
        HookListeners();
        SwitchTab(currentTab);
        UpdateLocalizedTexts();
    }

    private GameObject CreatePage(Transform parent, string name)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.layer = 5;
        page.transform.SetParent(parent, false);

        RectTransform rt = page.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return page;
    }

    private void SwitchTab(SettingsTab tab)
    {
        currentTab = tab;
        if (pageAudioLang != null) pageAudioLang.SetActive(currentTab == SettingsTab.AudioLang);
        if (pageControls != null) pageControls.SetActive(currentTab == SettingsTab.Controls);
        if (pageGameData != null) pageGameData.SetActive(currentTab == SettingsTab.GameData);
        UpdateTabVisuals();
    }

    private void UpdateTabVisuals()
    {
        bool isVi = LocalizationManager.IsVietnamese;

        Color activeBg = new Color(0.20f, 0.28f, 0.44f, 0.98f);
        Color inactiveBg = new Color(0.06f, 0.08f, 0.12f, 0.85f);

        Color activeBorder = new Color(0.98f, 0.85f, 0.35f, 0.95f);
        Color inactiveBorder = new Color(0.25f, 0.32f, 0.45f, 0.45f);

        Color activeTxt = new Color(1f, 0.95f, 0.75f);
        Color inactiveTxt = new Color(0.70f, 0.75f, 0.85f);

        Color activeIcon = new Color(1f, 0.88f, 0.40f);
        Color inactiveIcon = new Color(0.65f, 0.72f, 0.82f);

        bool t1 = (currentTab == SettingsTab.AudioLang);
        SetTabStyle(tabAudioBtn, tabAudioText, isVi ? "ÂM THANH" : "AUDIO & LANG", "Sound-Three", t1 ? activeBg : inactiveBg, t1 ? activeBorder : inactiveBorder, t1 ? activeTxt : inactiveTxt, t1 ? activeIcon : inactiveIcon, t1);

        bool t2 = (currentTab == SettingsTab.Controls);
        SetTabStyle(tabControlsBtn, tabControlsText, isVi ? "ĐIỀU KHIỂN" : "CONTROLS", "Star", t2 ? activeBg : inactiveBg, t2 ? activeBorder : inactiveBorder, t2 ? activeTxt : inactiveTxt, t2 ? activeIcon : inactiveIcon, t2);

        bool t3 = (currentTab == SettingsTab.GameData);
        SetTabStyle(tabDataBtn, tabDataText, isVi ? "DỮ LIỆU" : "GAME DATA", "Exclamation", t3 ? activeBg : inactiveBg, t3 ? activeBorder : inactiveBorder, t3 ? activeTxt : inactiveTxt, t3 ? activeIcon : inactiveIcon, t3);
    }

    private void SetTabStyle(Button btn, TextMeshProUGUI textRef, string label, string icon, Color bg, Color border, Color txtColor, Color iconColor, bool isActive)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = bg;

        Outline outline = btn.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = border;
            outline.effectDistance = isActive ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
        }

        if (textRef != null) textRef.text = label;
        WhiteIconLoader.SetButtonIconAndText(btn, icon, label, iconColor, txtColor, true);
    }

    private void BuildResetConfirmModal(Transform parent)
    {
        resetConfirmModal = new GameObject("ResetConfirmModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        resetConfirmModal.layer = 5;
        resetConfirmModal.transform.SetParent(parent, false);

        RectTransform rmRt = resetConfirmModal.GetComponent<RectTransform>();
        rmRt.anchorMin = Vector2.zero;
        rmRt.anchorMax = Vector2.one;
        rmRt.offsetMin = Vector2.zero;
        rmRt.offsetMax = Vector2.zero;

        Image rmBg = resetConfirmModal.GetComponent<Image>();
        rmBg.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);
        rmBg.raycastTarget = true;

        GameObject box = new GameObject("ConfirmBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.layer = 5;
        box.transform.SetParent(resetConfirmModal.transform, false);
        RectTransform bxRt = box.GetComponent<RectTransform>();
        bxRt.anchorMin = new Vector2(0.5f, 0.5f);
        bxRt.anchorMax = new Vector2(0.5f, 0.5f);
        bxRt.pivot = new Vector2(0.5f, 0.5f);
        bxRt.sizeDelta = new Vector2(600f, 280f);

        Image bxImg = box.GetComponent<Image>();
        bxImg.color = new Color(0.12f, 0.06f, 0.08f, 0.98f);
        var bxOutline = box.AddComponent<Outline>();
        bxOutline.effectColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        bxOutline.effectDistance = new Vector2(2f, -2f);

        GameObject pObj = new GameObject("Prompt", typeof(RectTransform), typeof(CanvasRenderer));
        pObj.transform.SetParent(box.transform, false);
        RectTransform pRt = pObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0f, 1f);
        pRt.anchorMax = new Vector2(1f, 1f);
        pRt.pivot = new Vector2(0.5f, 1f);
        pRt.anchoredPosition = new Vector2(0f, -30f);
        pRt.sizeDelta = new Vector2(-40f, 100f);

        resetPromptText = pObj.AddComponent<TextMeshProUGUI>();
        resetPromptText.fontSize = 17;
        resetPromptText.alignment = TextAlignmentOptions.Center;
        resetPromptText.color = new Color(1f, 0.85f, 0.85f);
        resetPromptText.text = "Bạn có chắc chắn muốn xóa toàn bộ dữ liệu và bắt đầu lại?";

        resetConfirmYesBtn = CreateStyledButton(box.transform, "Btn_Yes", new Vector2(40f, 30f), new Vector2(230f, 50f), new Color(0.72f, 0.16f, 0.20f), out resetYesBtnText);
        RectTransform yRt = resetConfirmYesBtn.GetComponent<RectTransform>();
        yRt.anchorMin = new Vector2(0f, 0f);
        yRt.anchorMax = new Vector2(0f, 0f);
        yRt.pivot = new Vector2(0f, 0f);
        resetYesBtnText.fontSize = 16;
        resetYesBtnText.text = "XÁC NHẬN XÓA";
        WhiteIconLoader.SetButtonIconAndText(resetConfirmYesBtn, "Tick", resetYesBtnText.text, new Color(1f, 0.4f, 0.4f), Color.white, true);

        resetConfirmNoBtn = CreateStyledButton(box.transform, "Btn_No", new Vector2(-40f, 30f), new Vector2(230f, 50f), new Color(0.2f, 0.25f, 0.35f), out resetNoBtnText);
        RectTransform nRt = resetConfirmNoBtn.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(1f, 0f);
        nRt.anchorMax = new Vector2(1f, 0f);
        nRt.pivot = new Vector2(1f, 0f);
        resetNoBtnText.fontSize = 16;
        resetNoBtnText.text = "HỦY BỎ";
        WhiteIconLoader.SetButtonIconAndText(resetConfirmNoBtn, "Cross", resetNoBtnText.text, Color.white, Color.white, true);

        resetConfirmModal.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateStyledButton(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor, out TextMeshProUGUI txtRef)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Button btn = obj.GetComponent<Button>();

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        txtRef = txtObj.AddComponent<TextMeshProUGUI>();
        txtRef.fontSize = 16;
        txtRef.fontStyle = FontStyles.Bold;
        txtRef.alignment = TextAlignmentOptions.Center;
        txtRef.color = new Color(0.96f, 0.88f, 0.65f);
        txtRef.raycastTarget = false;

        SoulslikeHUDStyler.StyleButton(btn);

        return btn;
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.layer = 5;
        sliderObj.transform.SetParent(parent, false);

        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        // Background
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.layer = 5;
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = SoulslikeHUDStyler.GetObsidianTroughSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.layer = 5;
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(5f, 0f);
        faRt.offsetMax = new Vector2(-15f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.layer = 5;
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fill.GetComponent<RectTransform>();
        fRt.sizeDelta = Vector2.zero;
        Image fImg = fill.GetComponent<Image>();
        fImg.sprite = SoulslikeHUDStyler.GetAmberFillSprite();
        fImg.type = Image.Type.Sliced;
        fImg.color = Color.white;

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.layer = 5;
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10f, 0f);
        haRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.layer = 5;
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(24f, 0f);
        Image hImg = handle.GetComponent<Image>();
        hImg.color = new Color(1f, 0.85f, 0.35f);

        var hOutline = handle.AddComponent<Outline>();
        hOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        hOutline.effectDistance = new Vector2(1f, -1f);

        slider.fillRect = fRt;
        slider.handleRect = hRt;
        slider.targetGraphic = hImg;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private void InitializeSliders()
    {
        float bgmVal = 0.8f;
        float sfxVal = 0.8f;
        float btnSizeVal = PlayerPrefs.GetFloat("Game_Controls_ButtonScale", 1.0f);
        float btnOpacityVal = PlayerPrefs.GetFloat("Game_Controls_Opacity", 1.0f);
        float joyX = PlayerPrefs.GetFloat("Game_Controls_Joy_PosX", 250f);
        float joyY = PlayerPrefs.GetFloat("Game_Controls_Joy_PosY", 250f);

        if (SoundManager.Instance != null)
        {
            bgmVal = SoundManager.Instance.GetBGMVolume();
            sfxVal = SoundManager.Instance.GetSFXVolume();
        }
        else
        {
            bgmVal = PlayerPrefs.GetFloat("Game_BGM_Volume", 0.8f);
            sfxVal = PlayerPrefs.GetFloat("Game_SFX_Volume", 0.8f);
        }

        if (bgmSlider != null) bgmSlider.value = bgmVal;
        if (sfxSlider != null) sfxSlider.value = sfxVal;
        if (bgmValueText != null) bgmValueText.text = $"{Mathf.RoundToInt(bgmVal * 100)}%";
        if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(sfxVal * 100)}%";
    }

    private void HookListeners()
    {
        if (tabAudioBtn != null)
        {
            tabAudioBtn.onClick.RemoveAllListeners();
            tabAudioBtn.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                SwitchTab(SettingsTab.AudioLang);
            });
        }
        if (tabControlsBtn != null)
        {
            tabControlsBtn.onClick.RemoveAllListeners();
            tabControlsBtn.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                SwitchTab(SettingsTab.Controls);
            });
        }
        if (tabDataBtn != null)
        {
            tabDataBtn.onClick.RemoveAllListeners();
            tabDataBtn.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                SwitchTab(SettingsTab.GameData);
            });
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener((val) => {
                if (bgmValueText != null) bgmValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
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
                if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(val);
                else
                {
                    PlayerPrefs.SetFloat("Game_SFX_Volume", val);
                    PlayerPrefs.Save();
                }
            });
        }



        if (customizeLayoutBtn != null)
        {
            customizeLayoutBtn.onClick.RemoveAllListeners();
            customizeLayoutBtn.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                gameObject.SetActive(false);
                MobileControlsLayoutEditor.OpenEditor(() => {
                    if (Instance != null)
                    {
                        Instance.OpenSettings();
                        Instance.SwitchTab(SettingsTab.Controls);
                    }
                });
            });
        }

        if (resetControlsDefaultBtn != null)
        {
            resetControlsDefaultBtn.onClick.RemoveAllListeners();
            resetControlsDefaultBtn.onClick.AddListener(OnResetControlsDefault);
        }

        if (languageToggleButton != null)
        {
            languageToggleButton.onClick.RemoveAllListeners();
            languageToggleButton.onClick.AddListener(OnLanguageToggleClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
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

    private void OnResetControlsDefault()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        PlayerPrefs.SetFloat("Game_Controls_ButtonScale", 1.0f);
        PlayerPrefs.SetFloat("Game_Controls_Opacity", 1.0f);

        PlayerPrefs.SetFloat("Game_Controls_Joy_PosX", 250f);
        PlayerPrefs.SetFloat("Game_Controls_Joy_PosY", 250f);
        PlayerPrefs.SetFloat("Game_Controls_Atk_PosX", -220f);
        PlayerPrefs.SetFloat("Game_Controls_Atk_PosY", 220f);
        PlayerPrefs.SetFloat("Game_Controls_Dash_PosX", -380f);
        PlayerPrefs.SetFloat("Game_Controls_Dash_PosY", 160f);
        PlayerPrefs.SetFloat("Game_Controls_Skill_PosX", -350f);
        PlayerPrefs.SetFloat("Game_Controls_Skill_PosY", 290f);
        PlayerPrefs.SetFloat("Game_Controls_Potion_PosX", -220f);
        PlayerPrefs.SetFloat("Game_Controls_Potion_PosY", 360f);

        PlayerPrefs.Save();

        ApplyControlSettingsLive();
    }

    public static void ApplyControlSettingsLive()
    {
        MobileActionButton.ApplyToAllButtons();
        VirtualJoystick.ApplyToAllJoysticks();

        float scaleMod = PlayerPrefs.GetFloat("Game_Controls_ButtonScale", 1.0f);
        float opacity = PlayerPrefs.GetFloat("Game_Controls_Opacity", 1.0f);
        var potionSlot = GameObject.Find("Btn_Potion");
        if (potionSlot != null)
        {
            potionSlot.transform.localScale = Vector3.one * scaleMod;
            var cg = potionSlot.GetComponent<CanvasGroup>();
            if (cg == null) cg = potionSlot.AddComponent<CanvasGroup>();
            cg.alpha = opacity;
        }
    }

    public void UpdateLocalizedTexts()
    {
        bool isVi = LocalizationManager.IsVietnamese;
        if (titleText != null) titleText.text = isVi ? "<b>CÀI ĐẶT HỆ THỐNG</b>" : "<b>SYSTEM SETTINGS</b>";
        if (subtitleText != null) subtitleText.text = isVi ? "Âm thanh, Điều khiển & Dữ liệu trò chơi" : "Audio, Controls & Game Data";

        if (sidebarTitleText != null) sidebarTitleText.text = isVi ? "<b>DANH MỤC</b>" : "<b>CATEGORIES</b>";

        UpdateTabVisuals();

        // Page 1
        if (pageAudioHeader != null) pageAudioHeader.text = isVi ? "<b><color=#55EFC4>1. ÂM THANH & NGÔN NGỮ</color></b>" : "<b><color=#55EFC4>1. AUDIO & LANGUAGE</color></b>";
        if (pageAudioSub != null) pageAudioSub.text = isVi ? "Tùy chỉnh âm lượng nhạc nền, hiệu ứng âm thanh và ngôn ngữ hiển thị" : "Adjust background music, sound effects volume and display language";
        if (bgmLabel != null) bgmLabel.text = isVi ? "Âm nhạc nền (BGM):" : "Music Volume (BGM):";
        if (sfxLabel != null) sfxLabel.text = isVi ? "Hiệu ứng âm thanh (SFX):" : "Sound Effects (SFX):";
        if (languageLabel != null) languageLabel.text = isVi ? "Ngôn ngữ hiển thị (Display Language):" : "Display Language:";
        if (languageButtonText != null)
        {
            languageButtonText.text = isVi ? "TIẾNG VIỆT (VIETNAMESE)" : "ENGLISH";
            if (languageToggleButton != null)
            {
                WhiteIconLoader.SetButtonIconAndText(languageToggleButton, "Language_Globe", languageButtonText.text, new Color(0.4f, 0.85f, 1f), Color.white, true);
            }
        }

        // Page 2
        if (pageControlsHeader != null) pageControlsHeader.text = isVi ? "<b><color=#55EFC4>2. PHÍM ĐIỀU KHIỂN CẢM ỨNG</color></b>" : "<b><color=#55EFC4>2. TOUCH CONTROLS SETTINGS</color></b>";
        if (pageControlsSub != null) pageControlsSub.text = isVi ? "Kéo thả trực tiếp vị trí các phím trên màn hình trận đấu" : "Drag and drop button positions directly on the battle screen";

        if (controlsCardTitle != null) controlsCardTitle.text = isVi ? "<b>KÉO THẢ PHÍM TRỰC TIẾP TRONG TRẬN ĐẤU</b>" : "<b>DIRECT IN-MATCH CONTROLS CUSTOMIZATION</b>";
        if (controlsCardDesc != null)
        {
            controlsCardDesc.text = isVi
                ? "Chạm và kéo tự do Cần di chuyển (Joystick), Nút Đánh, Lướt, Tuyệt kỹ & Bình máu trực tiếp trên màn hình trận đấu để phù hợp nhất với cỡ tay của bạn.\n\nVị trí mới sẽ được lưu tự động và áp dụng vĩnh viễn cho tất cả các trận đấu!"
                : "Touch and drag the Joystick, Attack, Dash, Skill & Potion buttons directly on screen to fit your hand grip.\n\nCustom positions are saved automatically and applied for all future battles!";
        }

        if (customizeLayoutBtnText != null)
        {
            customizeLayoutBtnText.text = isVi ? "KÉO THẢ TRỰC TIẾP TRONG TRẬN" : "DRAG CONTROLS IN MATCH";
            if (customizeLayoutBtn != null) WhiteIconLoader.SetButtonIconAndText(customizeLayoutBtn, "Levels", customizeLayoutBtnText.text, new Color(0.3f, 0.9f, 1f), Color.white, true);
        }

        if (resetControlsDefaultBtnText != null)
        {
            resetControlsDefaultBtnText.text = isVi ? "KHÔI PHỤC MẶC ĐỊNH" : "RESET TO DEFAULT";
            if (resetControlsDefaultBtn != null)
            {
                WhiteIconLoader.SetButtonIconAndText(resetControlsDefaultBtn, "Repeat-Right", resetControlsDefaultBtnText.text, new Color(1f, 0.85f, 0.35f), Color.white, true);
            }
        }

        if (controlsCardTip != null)
        {
            controlsCardTip.text = isVi
                ? "<i><color=#88A0C0>* Trận đấu sẽ tự động tạm dừng an toàn trong lúc bạn căn chỉnh vị trí các nút.</color></i>"
                : "<i><color=#88A0C0>* The match pauses safely while you arrange button positions on screen.</color></i>";
        }

        // Page 3
        if (pageDataHeader != null) pageDataHeader.text = isVi ? "<b><color=#FFA502>3. DỮ LIỆU TRÒ CHƠI & HỆ THỐNG</color></b>" : "<b><color=#FFA502>3. GAME DATA & SYSTEM</color></b>";
        if (pageDataSub != null) pageDataSub.text = isVi ? "Quản lý tiến trình trò chơi ngoại tuyến và thao tác thoát trận" : "Manage local offline save progress and match exit options";
        if (resetDataDesc != null) resetDataDesc.text = isVi ? "Xóa toàn bộ dữ liệu lưu trữ để bắt đầu một hành trình mới:" : "Reset all saved progress to start a fresh adventure:";
        if (resetDataBtnText != null)
        {
            resetDataBtnText.text = isVi ? "XÓA TOÀN BỘ DỮ LIỆU" : "RESET ALL DATA";
            if (resetDataButton != null) WhiteIconLoader.SetButtonIconAndText(resetDataButton, "Exclamation", resetDataBtnText.text, new Color(1f, 0.4f, 0.4f), Color.white, true);
        }

        bool gameplay = isGameplayScene;
        if (mainMenuDesc != null)
        {
            mainMenuDesc.gameObject.SetActive(gameplay);
            mainMenuDesc.text = isVi ? "Thoát khỏi màn chơi hiện tại và quay về sảnh chính:" : "Exit current battle and return to main menu:";
        }
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(gameplay);
        if (mainMenuBtnText != null)
        {
            mainMenuBtnText.text = isVi ? "VỀ SẢNH CHÍNH" : "MAIN MENU";
            if (mainMenuButton != null) WhiteIconLoader.SetButtonIconAndText(mainMenuButton, "Home", mainMenuBtnText.text, new Color(1f, 0.85f, 0.35f), Color.white, true);
        }

        if (offlineNoteText != null)
        {
            offlineNoteText.text = isVi 
                ? "<color=#88A0C0><i>* Trò chơi thuần Offline. Mọi dữ liệu cấp độ, mảnh hồn và trang bị được lưu an toàn trên thiết bị của bạn.</i></color>"
                : "<color=#88A0C0><i>* Pure Offline Game. All levels, soul shards and equipment are securely stored on your device.</i></color>";
        }

        if (closeBtnText != null)
        {
            closeBtnText.text = gameplay ? (isVi ? "TIẾP TỤC" : "RESUME") : (isVi ? "QUAY LẠI" : "BACK");
            if (closeButton != null)
            {
                string cIcon = gameplay ? "Play" : "SolidArrow-Left";
                WhiteIconLoader.SetButtonIconAndText(closeButton, cIcon, closeBtnText.text, Color.white, Color.white, true);
            }
        }

        if (resetPromptText != null) resetPromptText.text = isVi ? "Bạn có chắc chắn muốn xóa toàn bộ dữ liệu và bắt đầu lại?" : "Are you sure you want to reset all game data?";
        if (resetYesBtnText != null)
        {
            resetYesBtnText.text = isVi ? "XÁC NHẬN XÓA" : "CONFIRM";
            if (resetConfirmYesBtn != null) WhiteIconLoader.SetButtonIconAndText(resetConfirmYesBtn, "Tick", resetYesBtnText.text, new Color(1f, 0.4f, 0.4f), Color.white, true);
        }
        if (resetNoBtnText != null)
        {
            resetNoBtnText.text = isVi ? "HỦY BỎ" : "CANCEL";
            if (resetConfirmNoBtn != null) WhiteIconLoader.SetButtonIconAndText(resetConfirmNoBtn, "Cross", resetNoBtnText.text, Color.white, Color.white, true);
        }
    }

    public void OpenSettings()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = Object.FindAnyObjectByType<Canvas>();
        if (rootCanvas != null && transform.parent != rootCanvas.transform)
        {
            transform.SetParent(rootCanvas.transform, false);
        }

        EnsureFullscreenOverlay();

        if (dialogBox == null || categorySidebar == null || contentPanel == null)
        {
            BuildUI();
        }

        if (isGameplayScene)
        {
            GameAutoBootstrap.SetGameplayHUDVisible(false);
        }

        transform.SetAsLastSibling();
        gameObject.SetActive(true);

        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);

        Time.timeScale = 0f;
        InitializeSliders();
        HookListeners();
        SwitchTab(currentTab);
        UpdateLocalizedTexts();
    }

    public bool IsOpen()
    {
        return gameObject.activeSelf;
    }

    public void CloseSettings()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuClose();
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
        ApplyControlSettingsLive();
        gameObject.SetActive(false);
        Time.timeScale = 1.0f;

        if (isGameplayScene)
        {
            GameAutoBootstrap.SetGameplayHUDVisible(true);
            ApplyControlSettingsLive();
        }
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

    private void OnLogoutClicked()
    {
        OnMainMenuClicked();
    }

    private void OnMainMenuClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        CloseSettings();
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnResetDataClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (resetConfirmModal != null) resetConfirmModal.SetActive(true);
    }

    private void OnConfirmResetYes()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        AccountDatabase.ResetActiveUserProgress();
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
        CloseSettings();
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnConfirmResetNo()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
    }
}
