using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("UI Structure")]
    private CanvasGroup canvasGroup;
    private GameObject dialogBox;
    private GameObject resetConfirmModal;

    [Header("Headers & Titles")]
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;

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

    [Header("Account & Data Controls")]
    private TextMeshProUGUI accountInfoText;
    private Button logoutButton;
    private TextMeshProUGUI logoutBtnText;
    private Button resetDataButton;
    private TextMeshProUGUI resetDataBtnText;

    [Header("In-Game Actions")]
    private Button restartButton;
    private TextMeshProUGUI restartBtnText;
    private Button mainMenuButton;
    private TextMeshProUGUI mainMenuBtnText;

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
                // In-Game top right Gear icon button
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

                GameObject txtObj = new GameObject("GearIcon", typeof(RectTransform), typeof(CanvasRenderer));
                txtObj.layer = 5;
                txtObj.transform.SetParent(btnObj.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "<size=24>⚙</size>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 0.85f, 0.4f);
                tmp.raycastTarget = false;

                Button btn = btnObj.GetComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() =>
                {
                    if (Instance != null) Instance.OpenSettings();
                    else EnsureUIExists()?.OpenSettings();
                });
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
        gameObject.SetActive(false); // An modal mac dinh khi bat dau
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
        boxRt.sizeDelta = new Vector2(1060f, 600f);

        Image boxImg = dialogBox.GetComponent<Image>();
        boxImg.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);

        var outline = dialogBox.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.95f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // 2. Header Title & Subtitle
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -22f);
        titleRt.sizeDelta = new Vector2(0f, 38f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.85f, 0.35f);
        titleText.text = "<b>CÀI ĐẶT HỆ THỐNG</b>";

        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.transform.SetParent(dialogBox.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -60f);
        subRt.sizeDelta = new Vector2(0f, 24f);

        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 14f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.6f, 0.85f, 1f);
        subtitleText.text = "Âm thanh, Ngôn ngữ, Quản lý tài khoản & Dữ liệu trò chơi";

        // 3. LEFT COLUMN: Audio & Language
        GameObject leftCol = new GameObject("LeftCol_AudioLang", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        leftCol.transform.SetParent(dialogBox.transform, false);
        RectTransform lcRt = leftCol.GetComponent<RectTransform>();
        lcRt.anchorMin = new Vector2(0f, 0f);
        lcRt.anchorMax = new Vector2(0.5f, 1f);
        lcRt.pivot = new Vector2(0.5f, 0.5f);
        lcRt.offsetMin = new Vector2(30f, 85f);
        lcRt.offsetMax = new Vector2(-15f, -95f);

        Image lcImg = leftCol.GetComponent<Image>();
        lcImg.color = new Color(0.09f, 0.12f, 0.18f, 0.95f);
        var lcOutline = leftCol.AddComponent<Outline>();
        lcOutline.effectColor = new Color(0.2f, 0.5f, 0.8f, 0.6f);
        lcOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var lcHeader = CreateLabel(leftCol.transform, "Header", "<b><color=#55EFC4>1. ÂM THANH & NGÔN NGỮ</color></b>", new Vector2(20f, -15f), new Vector2(450f, 28f));
        lcHeader.fontSize = 17;

        // BGM Slider Row
        bgmLabel = CreateLabel(leftCol.transform, "BGMLabel", "Âm nhạc nền (BGM):", new Vector2(20f, -55f), new Vector2(400f, 26f));
        bgmLabel.fontSize = 15;
        bgmSlider = CreateSlider(leftCol.transform, "BGMSlider", new Vector2(20f, -88f), new Vector2(360f, 28f));
        bgmValueText = CreateLabel(leftCol.transform, "BGMVal", "80%", new Vector2(395f, -88f), new Vector2(75f, 28f));
        bgmValueText.fontSize = 16;
        bgmValueText.fontStyle = FontStyles.Bold;
        bgmValueText.color = new Color(1f, 0.85f, 0.35f);

        // SFX Slider Row
        sfxLabel = CreateLabel(leftCol.transform, "SFXLabel", "Hiệu ứng âm thanh (SFX):", new Vector2(20f, -145f), new Vector2(400f, 26f));
        sfxLabel.fontSize = 15;
        sfxSlider = CreateSlider(leftCol.transform, "SFXSlider", new Vector2(20f, -178f), new Vector2(360f, 28f));
        sfxValueText = CreateLabel(leftCol.transform, "SFXVal", "80%", new Vector2(395f, -178f), new Vector2(75f, 28f));
        sfxValueText.fontSize = 16;
        sfxValueText.fontStyle = FontStyles.Bold;
        sfxValueText.color = new Color(1f, 0.85f, 0.35f);

        // Language Row
        languageLabel = CreateLabel(leftCol.transform, "LangLabel", "Ngôn ngữ hiển thị (Language):", new Vector2(20f, -240f), new Vector2(450f, 26f));
        languageLabel.fontSize = 15;
        languageToggleButton = CreateStyledButton(leftCol.transform, "Btn_LanguageToggle", new Vector2(20f, -275f), new Vector2(450f, 52f), new Color(0.16f, 0.24f, 0.38f), out languageButtonText);
        languageButtonText.fontSize = 16;
        languageButtonText.text = LocalizationManager.IsVietnamese ? "TIẾNG VIỆT (VIETNAMESE)" : "ENGLISH";

        // 4. RIGHT COLUMN: Account & Data
        GameObject rightCol = new GameObject("RightCol_AccountData", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightCol.transform.SetParent(dialogBox.transform, false);
        RectTransform rcRt = rightCol.GetComponent<RectTransform>();
        rcRt.anchorMin = new Vector2(0.5f, 0f);
        rcRt.anchorMax = new Vector2(1f, 1f);
        rcRt.pivot = new Vector2(0.5f, 0.5f);
        rcRt.offsetMin = new Vector2(15f, 85f);
        rcRt.offsetMax = new Vector2(-30f, -95f);

        Image rcImg = rightCol.GetComponent<Image>();
        rcImg.color = new Color(0.09f, 0.12f, 0.18f, 0.95f);
        var rcOutline = rightCol.AddComponent<Outline>();
        rcOutline.effectColor = new Color(0.8f, 0.4f, 0.3f, 0.6f);
        rcOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var rcHeader = CreateLabel(rightCol.transform, "Header", "<b><color=#FFA502>2. TÀI KHOẢN & DỮ LIỆU</color></b>", new Vector2(20f, -15f), new Vector2(450f, 28f));
        rcHeader.fontSize = 17;

        // Account Info Box
        GameObject accBox = new GameObject("AccBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accBox.transform.SetParent(rightCol.transform, false);
        RectTransform abRt = accBox.GetComponent<RectTransform>();
        abRt.anchorMin = new Vector2(0f, 1f);
        abRt.anchorMax = new Vector2(1f, 1f);
        abRt.pivot = new Vector2(0.5f, 1f);
        abRt.anchoredPosition = new Vector2(0f, -50f);
        abRt.sizeDelta = new Vector2(-40f, 75f);
        Image abImg = accBox.GetComponent<Image>();
        abImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
        var abOutline = accBox.AddComponent<Outline>();
        abOutline.effectColor = new Color(0.3f, 0.4f, 0.55f, 0.6f);

        string initUser = PlayerPrefs.GetString(AccountDatabase.PREF_CURRENT_USER,
            PlayerPrefs.GetString("Player_Name", "WANDERER")).ToUpper();
        accountInfoText = CreateLabel(accBox.transform, "AccText",
            $"Tài khoản: <color=#FFA502><b>{initUser}</b></color>\nTrạng thái: <color=#2ED573>Đã đăng nhập</color>",
            new Vector2(15f, -12f), new Vector2(420f, 50f));
        accountInfoText.fontSize = 15;
        accountInfoText.lineSpacing = 10;

        // 1. LOGOUT BUTTON
        logoutButton = CreateStyledButton(rightCol.transform, "Btn_Logout", new Vector2(20f, -140f), new Vector2(450f, 50f), new Color(0.72f, 0.16f, 0.20f), out logoutBtnText);
        logoutBtnText.fontSize = 16;
        logoutBtnText.text = "<b>ĐĂNG XUẤT TÀI KHOẢN (LOGOUT)</b>";

        // 2. RESET DATA BUTTON
        resetDataButton = CreateStyledButton(rightCol.transform, "Btn_ResetData", new Vector2(20f, -200f), new Vector2(450f, 46f), new Color(0.38f, 0.14f, 0.16f), out resetDataBtnText);
        resetDataBtnText.fontSize = 15;
        resetDataBtnText.text = LocalizationManager.Get("BTN_RESET_DATA");

        // 3. Gameplay In-Game Action Buttons
        restartButton = CreateStyledButton(rightCol.transform, "Btn_Restart", new Vector2(20f, -260f), new Vector2(215f, 48f), new Color(0.2f, 0.25f, 0.35f), out restartBtnText);
        restartBtnText.fontSize = 15;
        restartBtnText.text = LocalizationManager.Get("BTN_RESTART");

        mainMenuButton = CreateStyledButton(rightCol.transform, "Btn_MainMenu", new Vector2(255f, -260f), new Vector2(215f, 48f), new Color(0.2f, 0.25f, 0.35f), out mainMenuBtnText);
        mainMenuBtnText.fontSize = 15;
        mainMenuBtnText.text = LocalizationManager.Get("BTN_MAIN_MENU");

        // 5. BOTTOM ROW: CLOSE BUTTON
        closeButton = CreateStyledButton(dialogBox.transform, "Btn_Close", Vector2.zero, new Vector2(360f, 48f), new Color(0.12f, 0.48f, 0.24f), out closeBtnText);
        RectTransform cbRt = closeButton.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.5f, 0f);
        cbRt.anchorMax = new Vector2(0.5f, 0f);
        cbRt.pivot = new Vector2(0.5f, 0.5f);
        cbRt.anchoredPosition = new Vector2(0f, 42f);
        closeBtnText.fontSize = 18;
        closeBtnText.text = LocalizationManager.Get("BTN_BACK");

        // 6. Reset Confirmation Modal
        BuildResetConfirmModal(dialogBox.transform);

        InitializeSliders();
        HookListeners();
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

        resetConfirmNoBtn = CreateStyledButton(box.transform, "Btn_No", new Vector2(-40f, 30f), new Vector2(230f, 50f), new Color(0.2f, 0.25f, 0.35f), out resetNoBtnText);
        RectTransform nRt = resetConfirmNoBtn.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(1f, 0f);
        nRt.anchorMax = new Vector2(1f, 0f);
        nRt.pivot = new Vector2(1f, 0f);
        resetNoBtnText.fontSize = 16;
        resetNoBtnText.text = "HỦY BỎ";

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

        Image img = obj.GetComponent<Image>();
        img.color = bgColor;

        var outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

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
        txtRef.color = Color.white;
        txtRef.raycastTarget = false;

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
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

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
        fImg.color = new Color(0.25f, 0.75f, 0.45f, 1f);

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

        if (languageToggleButton != null)
        {
            languageToggleButton.onClick.RemoveAllListeners();
            languageToggleButton.onClick.AddListener(OnLanguageToggleClicked);
        }
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
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
        bool isVi = LocalizationManager.IsVietnamese;
        if (titleText != null) titleText.text = isVi ? "<b>CÀI ĐẶT HỆ THỐNG</b>" : "<b>SYSTEM SETTINGS</b>";
        if (subtitleText != null) subtitleText.text = isVi ? "Âm thanh, Ngôn ngữ, Quản lý tài khoản & Dữ liệu trò chơi" : "Audio, Language, Account Management & Game Data";
        if (bgmLabel != null) bgmLabel.text = isVi ? "Âm nhạc nền (BGM):" : "Music Volume (BGM):";
        if (sfxLabel != null) sfxLabel.text = isVi ? "Hiệu ứng âm thanh (SFX):" : "Sound Effects (SFX):";
        if (languageLabel != null) languageLabel.text = isVi ? "Ngôn ngữ hiển thị (Language):" : "Display Language:";

        if (languageButtonText != null)
        {
            languageButtonText.text = isVi ? "TIẾNG VIỆT (VIETNAMESE)" : "ENGLISH";
        }

        string activeUser = PlayerPrefs.GetString(AccountDatabase.PREF_CURRENT_USER, PlayerPrefs.GetString("Player_Name", "WANDERER"));
        if (accountInfoText != null)
        {
            accountInfoText.text = isVi
                ? $"Tài khoản: <color=#FFA502><b>{activeUser.ToUpper()}</b></color>\nTrạng thái: <color=#2ED573>Đã đăng nhập</color>"
                : $"Account: <color=#FFA502><b>{activeUser.ToUpper()}</b></color>\nStatus: <color=#2ED573>Authenticated</color>";
        }

        if (logoutBtnText != null) logoutBtnText.text = isVi ? "<b>ĐĂNG XUẤT TÀI KHOẢN (LOGOUT)</b>" : "<b>LOGOUT ACCOUNT</b>";

        bool gameplay = isGameplayScene;
        if (restartButton != null) restartButton.gameObject.SetActive(gameplay);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(gameplay);

        if (restartBtnText != null) restartBtnText.text = isVi ? "CHƠI LẠI" : "RESTART";
        if (mainMenuBtnText != null) mainMenuBtnText.text = isVi ? "VỀ SẢNH CHÍNH" : "MAIN MENU";
        if (resetDataBtnText != null) resetDataBtnText.text = isVi ? "XÓA TOÀN BỘ DỮ LIỆU (RESET)" : "RESET ALL DATA";
        if (closeBtnText != null) closeBtnText.text = gameplay ? (isVi ? "TIẾP TỤC" : "RESUME") : (isVi ? "QUAY LẠI" : "BACK");

        if (resetPromptText != null) resetPromptText.text = isVi ? "Bạn có chắc chắn muốn xóa toàn bộ dữ liệu và bắt đầu lại?" : "Are you sure you want to reset all game data?";
        if (resetYesBtnText != null) resetYesBtnText.text = isVi ? "XÁC NHẬN XÓA" : "CONFIRM";
        if (resetNoBtnText != null) resetNoBtnText.text = isVi ? "HỦY BỎ" : "CANCEL";
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

        if (dialogBox == null)
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
        gameObject.SetActive(false);
        Time.timeScale = 1.0f;

        if (isGameplayScene)
        {
            GameAutoBootstrap.SetGameplayHUDVisible(true);
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
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        CloseSettings();
        AccountDatabase.SaveActiveSessionToDatabase();
        SceneManager.LoadScene("LoginScene");
    }

    private void OnRestartClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        CloseSettings();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("LoginScene");
    }

    private void OnConfirmResetNo()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (resetConfirmModal != null) resetConfirmModal.SetActive(false);
    }
}
