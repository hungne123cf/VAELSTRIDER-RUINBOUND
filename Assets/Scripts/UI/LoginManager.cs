using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject loginCardPanel;
    [SerializeField] private GameObject tapToStartPanel;
    [SerializeField] private GameIntroLoadingController introLoadingController;

    [Header("Tabs")]
    [SerializeField] private Button tabLoginBtn;
    [SerializeField] private Button tabRegisterBtn;
    [SerializeField] private Image tabLoginBg;
    [SerializeField] private Image tabRegisterBg;
    [SerializeField] private TextMeshProUGUI tabLoginTxt;
    [SerializeField] private TextMeshProUGUI tabRegisterTxt;

    [Header("Login & Register Form Elements")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button togglePasswordVisibilityBtn;
    [SerializeField] private TextMeshProUGUI togglePasswordBtnText;
    [SerializeField] private Toggle rememberMeToggle;
    [SerializeField] private Button actionSubmitButton;
    [SerializeField] private TextMeshProUGUI actionSubmitButtonText;
    [SerializeField] private Button quitButton;

    [Header("Tap To Start Elements")]
    [SerializeField] private Button fullScreenTapButton;
    [SerializeField] private TextMeshProUGUI tapToStartText;
    [SerializeField] private TextMeshProUGUI welcomeUserBannerText;
    [SerializeField] private Button switchAccountBtn;

    [Header("Header & General Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI statusFeedbackText;
    [SerializeField] private TextMeshProUGUI versionText;

    private bool isRegisterMode = false;
    private bool isPasswordVisible = false;
    private bool isStartingGame = false;
    private float pulseTimer = 0f;

    public static LoginManager EnsureInstance()
    {
        if (Instance == null)
        {
            Instance = Object.FindAnyObjectByType<LoginManager>();
        }
        return Instance;
    }

    private void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        EnsureInputSystem();
        EnsureCloudService();
    }

    private void Start()
    {
        // Tự động nạp hoặc tạo session offline để người chơi vào thẳng game không cần đăng nhập/đăng ký
        AccountDatabase.EnsureOfflineSession();

        AutoBindUIElements();
        LoadRememberedAccount();
        HookButtons();
        ApplySoulslikeTheme();
        SetMode(false);
        UpdateEyeIconDisplay();
        UpdateAllTexts();
        LocalizationManager.OnLanguageChanged += UpdateAllTexts;

        // Game Offline: Vào thẳng màn hình Bắt đầu trò chơi
        ShowTapToStartDirectly();
    }

    public void ShowTapToStartDirectly()
    {
        if (loginCardPanel != null) loginCardPanel.SetActive(false);
        if (tapToStartPanel != null) tapToStartPanel.SetActive(true);
        if (switchAccountBtn != null) switchAccountBtn.gameObject.SetActive(false); // Ẩn nút đổi tài khoản vì là game offline

        // Đảm bảo Tiêu Đề Game "VAELSTRIDER: RUINBOUND" luôn hiển thị 1 lớp duy nhất ở phía trên màn hình (Không bao giờ đè 2 lớp chữ)
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
            if (titleText.transform.parent != null && titleText.transform.parent.name == "Header")
            {
                titleText.transform.parent.gameObject.SetActive(false);
            }
        }
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);
        var oldHeader = GameObject.Find("Header");
        if (oldHeader != null) oldHeader.SetActive(false);

        if (tapToStartPanel != null)
        {
            Transform existingTitle = tapToStartPanel.transform.Find("TapTitleHeader");
            TextMeshProUGUI headerTmp = null;
            if (existingTitle != null)
            {
                existingTitle.gameObject.SetActive(true);
                headerTmp = existingTitle.GetComponent<TextMeshProUGUI>();
                existingTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 265f);
            }
            else
            {
                GameObject tObj = new GameObject("TapTitleHeader", typeof(RectTransform), typeof(CanvasRenderer));
                tObj.transform.SetParent(tapToStartPanel.transform, false);
                tObj.transform.SetAsFirstSibling();
                RectTransform tRt = tObj.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0.5f, 0.5f);
                tRt.anchorMax = new Vector2(0.5f, 0.5f);
                tRt.pivot = new Vector2(0.5f, 0.5f);
                tRt.anchoredPosition = new Vector2(0f, 265f);
                tRt.sizeDelta = new Vector2(1400f, 220f);

                headerTmp = tObj.AddComponent<TextMeshProUGUI>();
                headerTmp.raycastTarget = false;
                GameFontManager.ApplyFontToText(headerTmp, true);
            }

            if (headerTmp != null)
            {
                headerTmp.text = "<size=84><color=#FFD700><b>VAELSTRIDER</b></color></size>\n<size=40><color=#FFAA33><b>— RUINBOUND —</b></color></size>";
                headerTmp.alignment = TextAlignmentOptions.Center;
                headerTmp.overflowMode = TextOverflowModes.Overflow;
                SoulslikeHUDStyler.SetSafeOutline(headerTmp, 0.35f, Color.black);
            }
        }

        // Đảm bảo LoadingProgressPanel (chứa thanh slider trắng thừa) bị ẩn hoàn toàn khi ở màn hình Tap To Start
        var lpp = GameObject.Find("LoadingProgressPanel");
        if (lpp != null) lpp.SetActive(false);

        if (introLoadingController == null)
        {
            introLoadingController = Object.FindAnyObjectByType<GameIntroLoadingController>();
        }

        // Tự động ẩn toàn bộ các thanh Divider trắng thừa trên Canvas
        var dividers = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var img in dividers)
        {
            if (img != null && img.gameObject.name.ToLower().Contains("divider"))
            {
                img.gameObject.SetActive(false);
            }
        }

        // CHUẨN LIÊN QUÂN MOBILE:
        // Phần Đăng Nhập / Đăng Ký xuất hiện TRƯỚC phần "BẮT ĐẦU TRÒ CHƠI"!
        // Khi nào đăng nhập & đặt tên hiển thị xong hết mới hiện màn hình "BẮT ĐẦU TRÒ CHƠI" kèm Tên hiển thị!
        var pf = PlayFabClientService.EnsureInstance();
        if (!pf.IsLoggedIn)
        {
            SetTapToStartReadyState(false);
            Canvas rootCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
            PlayFabAuthLoginUI.OpenWizardForStartGame(OnAuthAndNicknameCompleted, rootCanvas != null ? rootCanvas.transform : null);
        }
        else
        {
            OnAuthAndNicknameCompleted();
        }
    }

    private TextMeshProUGUI _loggedInPlayerBadgeText;
    private GameObject _loggedInSwitchBtnObj;

    private void SetTapToStartReadyState(bool readyToStart)
    {
        if (tapToStartText != null) tapToStartText.gameObject.SetActive(readyToStart);
        if (fullScreenTapButton != null) fullScreenTapButton.interactable = readyToStart;
        if (_loggedInPlayerBadgeText != null && _loggedInPlayerBadgeText.transform.parent != null)
            _loggedInPlayerBadgeText.transform.parent.gameObject.SetActive(readyToStart);
        if (_loggedInSwitchBtnObj != null)
            _loggedInSwitchBtnObj.SetActive(readyToStart);
    }

    private void OnAuthAndNicknameCompleted()
    {
        EnsureLoggedInStartScreenBanner();
        SetTapToStartReadyState(true);
    }

    private void EnsureLoggedInStartScreenBanner()
    {
        if (tapToStartPanel == null) return;

        Transform existingBanner = tapToStartPanel.transform.Find("LoggedInAccountBanner");
        if (existingBanner == null)
        {
            GameObject bannerObj = new GameObject("LoggedInAccountBanner", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(tapToStartPanel.transform, false);
            RectTransform bRt = bannerObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0f, -85f);
            bRt.sizeDelta = new Vector2(760f, 58f);
            bannerObj.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.16f, 0.92f);

            GameObject txtObj = new GameObject("BannerText", typeof(RectTransform));
            txtObj.transform.SetParent(bannerObj.transform, false);
            RectTransform tRt = txtObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(16f, 4f);
            tRt.offsetMax = new Vector2(-16f, -4f);

            _loggedInPlayerBadgeText = txtObj.AddComponent<TextMeshProUGUI>();
            _loggedInPlayerBadgeText.fontSize = 21f;
            _loggedInPlayerBadgeText.fontStyle = FontStyles.Bold;
            _loggedInPlayerBadgeText.alignment = TextAlignmentOptions.Center;
            _loggedInPlayerBadgeText.raycastTarget = false;
            SoulslikeHUDStyler.SetSafeOutline(_loggedInPlayerBadgeText, 0.28f, Color.black);
            GameFontManager.ApplyFontToText(_loggedInPlayerBadgeText, true);
        }

        Transform existingSwitch = tapToStartPanel.transform.Find("Btn_TopRightSwitchAccount");
        if (existingSwitch == null)
        {
            _loggedInSwitchBtnObj = new GameObject("Btn_TopRightSwitchAccount", typeof(RectTransform), typeof(Image), typeof(Button));
            _loggedInSwitchBtnObj.transform.SetParent(tapToStartPanel.transform, false);
            RectTransform sRt = _loggedInSwitchBtnObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 1f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot = new Vector2(1f, 1f);
            sRt.anchoredPosition = new Vector2(-32f, -28f);
            sRt.sizeDelta = new Vector2(230f, 54f);
            _loggedInSwitchBtnObj.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.36f, 0.95f);

            GameObject stObj = new GameObject("SwitchTxt", typeof(RectTransform));
            stObj.transform.SetParent(_loggedInSwitchBtnObj.transform, false);
            RectTransform stRt = stObj.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;

            var stTmp = stObj.AddComponent<TextMeshProUGUI>();
            stTmp.text = LocalizationManager.T("ĐĂNG NHẬP / ĐỔI TK", "LOGIN / SWITCH");
            stTmp.fontSize = 18f;
            stTmp.fontStyle = FontStyles.Bold;
            stTmp.color = new Color(1f, 0.90f, 0.45f);
            stTmp.alignment = TextAlignmentOptions.Center;
            stTmp.raycastTarget = false;
            SoulslikeHUDStyler.SetSafeOutline(stTmp, 0.25f, Color.black);
            GameFontManager.ApplyFontToText(stTmp, true);

            _loggedInSwitchBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                PlayFabClientService.EnsureInstance().LogoutSession();
                SetTapToStartReadyState(false);
                Canvas rootCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
                PlayFabAuthLoginUI.OpenWizardForStartGame(OnAuthAndNicknameCompleted, rootCanvas != null ? rootCanvas.transform : null);
            });
        }

        Transform existingOfflineBtn = tapToStartPanel.transform.Find("Btn_TopLeftOffline");
        if (existingOfflineBtn == null)
        {
            GameObject offBtnObj = new GameObject("Btn_TopLeftOffline", typeof(RectTransform), typeof(Image), typeof(Button));
            offBtnObj.transform.SetParent(tapToStartPanel.transform, false);
            RectTransform oRt = offBtnObj.GetComponent<RectTransform>();
            oRt.anchorMin = new Vector2(0f, 1f);
            oRt.anchorMax = new Vector2(0f, 1f);
            oRt.pivot = new Vector2(0f, 1f);
            oRt.anchoredPosition = new Vector2(32f, -28f);
            oRt.sizeDelta = new Vector2(230f, 54f);
            offBtnObj.GetComponent<Image>().color = new Color(0.18f, 0.58f, 0.42f, 0.95f);

            GameObject otObj = new GameObject("OfflineTxt", typeof(RectTransform));
            otObj.transform.SetParent(offBtnObj.transform, false);
            RectTransform otRt = otObj.GetComponent<RectTransform>();
            otRt.anchorMin = Vector2.zero;
            otRt.anchorMax = Vector2.one;
            otRt.offsetMin = Vector2.zero;
            otRt.offsetMax = Vector2.zero;

            var otTmp = otObj.AddComponent<TextMeshProUGUI>();
            otTmp.text = LocalizationManager.T("CHƠI OFFLINE", "PLAY OFFLINE");
            otTmp.fontSize = 18f;
            otTmp.fontStyle = FontStyles.Bold;
            otTmp.color = Color.white;
            otTmp.alignment = TextAlignmentOptions.Center;
            otTmp.raycastTarget = false;
            SoulslikeHUDStyler.SetSafeOutline(otTmp, 0.25f, Color.black);
            GameFontManager.ApplyFontToText(otTmp, true);

            offBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                PlayFabAuthLoginUI.EnsureInstance()?.StartOfflineMode();
            });
        }

        if (_loggedInPlayerBadgeText != null)
        {
            var acc = AccountDatabase.GetActiveAccount();
            var pf = PlayFabClientService.EnsureInstance();
            bool isOff = PlayFabClientService.IsOfflineMode;
            string dispName = acc != null && !string.IsNullOrEmpty(acc.displayName) ? acc.displayName : "Chiến Binh";
            if (dispName == "Player" || dispName == "Wanderer")
            {
                string savedName = PlayerPrefs.GetString("Player_Name", "");
                if (!string.IsNullOrEmpty(savedName) && savedName != "Player" && savedName != "Wanderer")
                    dispName = savedName;
                else
                    dispName = isOff ? "Hiệp Sĩ Offline" : "Chiến Binh";
            }
            string accUser = acc != null ? acc.username : (isOff ? "Offline" : pf.PlayFabId);
            string prov = isOff ? "Chế độ Offline" : (pf.IsLoggedIn ? pf.LoggedInProvider : "PlayFab");

            _loggedInPlayerBadgeText.text = LocalizationManager.T(
                $"CHIẾN BINH: <color=#FFE066>{dispName}</color>   |   TÀI KHOẢN: <color=#66DDFF>{accUser}</color> ({prov})",
                $"FIGHTER: <color=#FFE066>{dispName}</color>   |   ACCOUNT: <color=#66DDFF>{accUser}</color> ({prov})");
        }
    }

    private void ApplySoulslikeTheme()
    {
        if (loginCardPanel != null)
        {
            SoulslikeHUDStyler.StyleModalWindow(loginCardPanel.transform);
        }

        // Tắt tiêu đề cũ bị trùng lặp để chỉ giữ 1 tiêu đề TapTitleHeader duy nhất
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
            if (titleText.transform.parent != null && titleText.transform.parent.name == "Header")
                titleText.transform.parent.gameObject.SetActive(false);
        }
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);

        if (actionSubmitButton != null)
        {
            SoulslikeHUDStyler.StyleButton(actionSubmitButton);
            var st = actionSubmitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (st != null) st.fontSize = 32f;
        }

        if (quitButton != null)
        {
            SoulslikeHUDStyler.StyleButton(quitButton);
            var qt = quitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (qt != null) qt.fontSize = 30f;
        }

        if (switchAccountBtn != null)
        {
            switchAccountBtn.gameObject.SetActive(false);
        }

        if (usernameInputField != null)
        {
            SoulslikeHUDStyler.StyleInputField(usernameInputField);
        }

        if (passwordInputField != null)
        {
            SoulslikeHUDStyler.StyleInputField(passwordInputField);
        }

        if (tapToStartText != null)
        {
            tapToStartText.fontSize = 52f;
            tapToStartText.fontStyle = FontStyles.Bold;
            tapToStartText.color = new Color(1f, 0.95f, 0.55f);
            tapToStartText.overflowMode = TextOverflowModes.Overflow;
            SoulslikeHUDStyler.SetSafeOutline(tapToStartText, 0.35f, Color.black);
            RectTransform tRt = tapToStartText.GetComponent<RectTransform>();
            if (tRt != null)
            {
                tRt.sizeDelta = new Vector2(1200f, 180f);
            }
        }

        GameFontManager.ApplyFontsGlobally(gameObject);
    }

    private void Update()
    {
        // Không nhận phím tắt chạm màn hình nếu bảng Đăng Nhập / Đăng Ký 5 bước PlayFab đang mở
        if (PlayFabAuthLoginUI.IsModalOpen) return;

        // Hiệu ứng nhịp thở cho chữ TAP TO START - Giữ độ sáng từ 0.85 đến 1.0 với viền đen sắc nét để luôn nổi bật
        if (tapToStartPanel != null && tapToStartPanel.activeSelf && tapToStartText != null && !isStartingGame)
        {
            pulseTimer += Time.deltaTime * 3.0f;
            float alpha = Mathf.PingPong(pulseTimer, 0.15f) + 0.85f;
            Color c = new Color(1f, 0.95f, 0.55f, alpha);
            tapToStartText.color = c;

            // Hỗ trợ nhấn phím Space hoặc Enter để bắt đầu
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnTapToStartClicked();
            }
        }
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= UpdateAllTexts;
    }

    private void EnsureCloudService()
    {
        if (CloudAccountService.Instance == null)
        {
            var cloudObj = new GameObject("CloudAccountService");
            cloudObj.AddComponent<CloudAccountService>();
        }
    }

    private void EnsureInputSystem()
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        }
        if (eventSystem == null)
        {
            var esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null)
        {
            if (Application.isPlaying) Destroy(standalone);
            else DestroyImmediate(standalone);
        }

        var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        inputModule.AssignDefaultActions();
        inputModule.enabled = true;
#else
        var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone == null)
        {
            standalone = eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        standalone.enabled = true;
#endif
    }

    private void AutoBindUIElements()
    {
        if (usernameInputField == null || passwordInputField == null)
        {
            var inputs = Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var inp in inputs)
            {
                string iName = inp.name.ToLower();
                if (iName.Contains("user") || iName.Contains("name")) usernameInputField = inp;
                else if (iName.Contains("pass")) passwordInputField = inp;
            }
        }

        if (togglePasswordVisibilityBtn == null)
        {
            var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in allButtons)
            {
                if (b.name.Contains("TogglePassword") || b.name.Contains("Eye"))
                {
                    togglePasswordVisibilityBtn = b;
                    break;
                }
            }
        }
    }

    private void LoadRememberedAccount()
    {
        if (AccountDatabase.GetRememberedCredentials(out string u, out string p))
        {
            if (usernameInputField != null) usernameInputField.text = u;
            if (passwordInputField != null) passwordInputField.text = p;
            if (rememberMeToggle != null) rememberMeToggle.isOn = true;
        }
        else
        {
            if (rememberMeToggle != null) rememberMeToggle.isOn = true;
        }
    }

    private void HookButtons()
    {
        if (tabLoginBtn != null)
        {
            tabLoginBtn.onClick.RemoveAllListeners();
            tabLoginBtn.onClick.AddListener(() => SetMode(false));
            AttachPointerClick(tabLoginBtn.gameObject, () => SetMode(false));
        }

        if (tabRegisterBtn != null)
        {
            tabRegisterBtn.onClick.RemoveAllListeners();
            tabRegisterBtn.onClick.AddListener(() => SetMode(true));
            AttachPointerClick(tabRegisterBtn.gameObject, () => SetMode(true));
        }

        if (actionSubmitButton != null)
        {
            actionSubmitButton.onClick.RemoveAllListeners();
            actionSubmitButton.onClick.AddListener(OnSubmitClicked);
            AttachPointerClick(actionSubmitButton.gameObject, OnSubmitClicked);
        }

        if (togglePasswordVisibilityBtn != null)
        {
            togglePasswordVisibilityBtn.onClick.RemoveAllListeners();
            togglePasswordVisibilityBtn.onClick.AddListener(TogglePasswordVisibility);

            // Xóa bỏ EventTrigger nếu có để tránh lỗi click bị gọi 2 lần (Double-Toggle) trong 1 frame
            var trigger = togglePasswordVisibilityBtn.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger != null)
            {
                if (Application.isPlaying) Destroy(trigger);
                else DestroyImmediate(trigger);
            }
        }

        if (fullScreenTapButton != null)
        {
            fullScreenTapButton.onClick.RemoveAllListeners();
            fullScreenTapButton.onClick.AddListener(OnTapToStartClicked);
            AttachPointerClick(fullScreenTapButton.gameObject, OnTapToStartClicked);
        }

        if (switchAccountBtn != null)
        {
            switchAccountBtn.onClick.RemoveAllListeners();
            switchAccountBtn.onClick.AddListener(ShowLoginPanel);
            AttachPointerClick(switchAccountBtn.gameObject, ShowLoginPanel);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
            AttachPointerClick(quitButton.gameObject, OnQuitClicked);
        }
    }

    private void AttachPointerClick(GameObject obj, UnityEngine.Events.UnityAction action)
    {
        if (obj == null || action == null) return;
        var trigger = obj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        trigger.triggers.Clear();
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => action.Invoke());
        trigger.triggers.Add(entry);
    }

    public void SetMode(bool register)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        isRegisterMode = register;
        if (statusFeedbackText != null) statusFeedbackText.text = "";

        // Cập nhật giao diện Tab
        if (tabLoginBtn != null)
        {
            SoulslikeHUDStyler.StyleTabButton(tabLoginBtn, !isRegisterMode, LocalizationManager.T("ĐĂNG NHẬP", "LOGIN"));
        }
        if (tabRegisterBtn != null)
        {
            SoulslikeHUDStyler.StyleTabButton(tabRegisterBtn, isRegisterMode, LocalizationManager.T("ĐĂNG KÝ", "REGISTER"));
        }

        if (actionSubmitButtonText != null)
        {
            actionSubmitButtonText.text = isRegisterMode 
                ? LocalizationManager.T("TẠO TÀI KHOẢN MỚI", "CREATE NEW ACCOUNT") 
                : LocalizationManager.T("ĐĂNG NHẬP VÀO GAME", "LOGIN TO GAME");
        }
        if (actionSubmitButton != null)
        {
            SoulslikeHUDStyler.StyleButton(actionSubmitButton);
        }
    }

    public void UpdateAllTexts()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
            if (titleText.transform.parent != null && titleText.transform.parent.name == "Header")
                titleText.transform.parent.gameObject.SetActive(false);
        }
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);
        if (tapToStartText != null)
        {
            tapToStartText.fontSize = 52f;
            tapToStartText.fontStyle = FontStyles.Bold;
            tapToStartText.overflowMode = TextOverflowModes.Overflow;
            GameFontManager.ApplyFontToText(tapToStartText);
            SoulslikeHUDStyler.SetSafeOutline(tapToStartText, 0.35f, Color.black);
            RectTransform tRt = tapToStartText.GetComponent<RectTransform>();
            if (tRt != null)
            {
                tRt.anchoredPosition = new Vector2(0f, -220f);
                tRt.sizeDelta = new Vector2(1200f, 150f);
            }
        }

        if (tabLoginTxt != null) tabLoginTxt.text = LocalizationManager.T("ĐĂNG NHẬP", "LOGIN");
        if (tabRegisterTxt != null) tabRegisterTxt.text = LocalizationManager.T("ĐĂNG KÝ", "REGISTER");

        if (actionSubmitButtonText != null)
        {
            actionSubmitButtonText.text = isRegisterMode 
                ? LocalizationManager.T("TẠO TÀI KHOẢN MỚI", "CREATE NEW ACCOUNT") 
                : LocalizationManager.T("ĐĂNG NHẬP VÀO GAME", "LOGIN TO GAME");
        }

        if (switchAccountBtn != null)
        {
            var swTmp = switchAccountBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (swTmp != null) swTmp.text = LocalizationManager.T("ĐỔI TÀI KHOẢN", "SWITCH ACCOUNT");
        }

        if (tapToStartText != null && !isStartingGame)
        {
            tapToStartText.text = LocalizationManager.T(
                "<b><color=#FFE57F>— BẮT ĐẦU TRÒ CHƠI —</color></b>\n<size=30><color=#F1F5F9>Chạm vào màn hình để bắt đầu</color></size>", 
                "<b><color=#FFE57F>— START GAME —</color></b>\n<size=30><color=#F1F5F9>Tap anywhere to start</color></size>"
            );
        }
        if (versionText != null)
        {
            versionText.text = LocalizationManager.T("Phiên bản Offline v1.0.0", "Offline Edition v1.0.0");
        }
    }

    [Header("Password Eye Sprites")]
    [SerializeField] private Image passwordEyeIconImg;
    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;

    public void TogglePasswordVisibility()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        isPasswordVisible = !isPasswordVisible;

        if (passwordInputField != null)
        {
            if (isPasswordVisible)
            {
                passwordInputField.contentType = TMP_InputField.ContentType.Standard;
                passwordInputField.inputType = TMP_InputField.InputType.Standard;
            }
            else
            {
                passwordInputField.contentType = TMP_InputField.ContentType.Password;
                passwordInputField.inputType = TMP_InputField.InputType.Password;
            }

            passwordInputField.ForceLabelUpdate();

            string currentText = passwordInputField.text;
            passwordInputField.text = "";
            passwordInputField.text = currentText;
        }

        UpdateEyeIconDisplay();
        Debug.Log($"<color=yellow>[PASSWORD VISIBILITY]</color> isPasswordVisible = {isPasswordVisible}");
    }

    public void UpdateEyeIconDisplay()
    {
        EnsureEyeSprites();
        if (passwordEyeIconImg == null && togglePasswordVisibilityBtn != null)
        {
            var childImg = togglePasswordVisibilityBtn.transform.Find("Icon")?.GetComponent<Image>();
            passwordEyeIconImg = childImg != null ? childImg : togglePasswordVisibilityBtn.GetComponent<Image>();
        }

        if (passwordEyeIconImg != null)
        {
            Sprite targetSprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
            if (targetSprite != null)
            {
                passwordEyeIconImg.sprite = targetSprite;
            }
            passwordEyeIconImg.color = isPasswordVisible ? new Color(1f, 0.9f, 0.45f, 1f) : new Color(0.75f, 0.85f, 0.95f, 0.95f);
        }

        if (togglePasswordVisibilityBtn != null)
        {
            var btnBg = togglePasswordVisibilityBtn.GetComponent<Image>();
            if (btnBg != null)
            {
                btnBg.color = isPasswordVisible ? new Color(0.24f, 0.38f, 0.65f, 1f) : new Color(0.14f, 0.18f, 0.28f, 1f);
            }
        }

        if (togglePasswordBtnText != null)
        {
            togglePasswordBtnText.text = ""; // Ẩn text để icon con mắt hiển thị đẹp nhất
        }
    }

    private void EnsureEyeSprites()
    {
        if (eyeOpenSprite == null)
        {
#if UNITY_EDITOR
            eyeOpenSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/UI/eye_open.png");
#endif
            if (eyeOpenSprite == null) eyeOpenSprite = LoadSpriteFromFile("Assets/ART/UI/eye_open.png");
        }

        if (eyeClosedSprite == null)
        {
#if UNITY_EDITOR
            eyeClosedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/UI/eye_closed.png");
#endif
            if (eyeClosedSprite == null) eyeClosedSprite = LoadSpriteFromFile("Assets/ART/UI/eye_closed.png");
        }
    }

    private Sprite LoadSpriteFromFile(string path)
    {
        string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
        if (File.Exists(fullPath))
        {
            byte[] data = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }
        return null;
    }

    public void OnSubmitClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        string user = usernameInputField != null ? usernameInputField.text.Trim() : "";
        string pass = passwordInputField != null ? passwordInputField.text : "";
        bool remember = rememberMeToggle != null && rememberMeToggle.isOn;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            ShowStatusError("Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!");
            return;
        }

        if (isRegisterMode)
        {
            // Đăng ký vào Database & Cloud
            if (AccountDatabase.Register(user, pass, out string regErr))
            {
                ShowStatusSuccess("Đăng ký thành công! Đang tự động nạp tiến trình...");
                AccountDatabase.SetRememberMe(user, pass, remember);

                if (AccountDatabase.Login(user, pass, out AccountDatabase.AccountData acc, out string _))
                {
                    OnLoginSuccess(acc);
                }
            }
            else
            {
                ShowStatusError(regErr);
            }
        }
        else
        {
            // Đăng nhập từ Local Database trước
            if (AccountDatabase.Login(user, pass, out AccountDatabase.AccountData acc, out _))
            {
                AccountDatabase.SetRememberMe(user, pass, remember);
                OnLoginSuccess(acc);
            }
            else
            {
                // Nếu máy mới chưa có trong Local -> Tải tiến trình từ Máy chủ Đám mây (Cloud) về
                ShowStatusLoading("Đang tìm và tải tiến trình từ Máy chủ Đám mây...");
                EnsureCloudService();
                string passHash = AccountDatabase.HashPassword(pass);
                CloudAccountService.Instance.LoginAndFetchCloud(user, passHash, (success, cloudAcc, cloudMsg) =>
                {
                    if (success && cloudAcc != null)
                    {
                        AccountDatabase.SaveOrUpdateCloudAccount(cloudAcc);
                        AccountDatabase.SetRememberMe(user, pass, remember);
                        OnLoginSuccess(cloudAcc);
                    }
                    else
                    {
                        ShowStatusError("Tài khoản hoặc mật khẩu không đúng!");
                    }
                });
            }
        }
    }

    private void OnLoginSuccess(AccountDatabase.AccountData account)
    {
        ShowStatusSuccess($"Đăng nhập thành công! Chào mừng {account.displayName}.");
        StartCoroutine(SwitchToTapToStartRoutine(account.displayName));
    }

    private IEnumerator SwitchToTapToStartRoutine(string userName)
    {
        yield return new WaitForSeconds(0.35f);

        if (loginCardPanel != null) loginCardPanel.SetActive(false);
        if (tapToStartPanel != null) tapToStartPanel.SetActive(true);

        if (welcomeUserBannerText != null)
        {
            welcomeUserBannerText.text = $"Chiến Binh: <b>{userName}</b> (Đã đồng bộ Đám Mây)";
        }
    }

    public void ShowLoginPanel()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Canvas rootCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
        PlayFabAuthLoginUI.OpenWizardForStartGame(ProceedToEnterMainMenuScene, rootCanvas != null ? rootCanvas.transform : null);
    }

    public void OnTapToStartClicked()
    {
        if (isStartingGame || PlayFabAuthLoginUI.IsModalOpen) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        // QUY TRÌNH 5 BƯỚC CHUẨN GAME ONLINE THƯƠNG MẠI:
        // Bước 1 (Mở Game -> Bấm Bắt Đầu) -> Nếu chưa xác thực PlayFab Cloud trong phiên này, mở Bước 2 (Chọn Đăng Nhập / Đăng Ký / Guest / Google / FB / Zalo)
        var pf = PlayFabClientService.EnsureInstance();
        if (!pf.IsLoggedIn && !PlayFabClientService.IsOfflineMode)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
            PlayFabAuthLoginUI.OpenWizardForStartGame(ProceedToEnterMainMenuScene, rootCanvas != null ? rootCanvas.transform : null);
            return;
        }

        ProceedToEnterMainMenuScene();
    }

    public void ProceedToEnterMainMenuScene()
    {
        if (isStartingGame) return;
        isStartingGame = true;
        StartCoroutine(StartGameTransitionRoutine());
    }

    private IEnumerator StartGameTransitionRoutine()
    {
        if (tapToStartText != null)
        {
            tapToStartText.color = new Color(0.4f, 1f, 0.6f, 1f);
            tapToStartText.text = LocalizationManager.T("ĐANG KHỞI HÀNH VÀO VƯƠNG QUỐC...", "ENTERING RUINBOUND REALM...");
        }

        Debug.Log($"<color=green>[START GAME]</color> Người chơi bấm Bắt Đầu -> Kích hoạt RealmTransitionManager...");
        yield return new WaitForSeconds(0.12f);

        // Sử dụng RealmTransitionManager (DontDestroyOnLoad, Canvas sortingOrder 9999) để nạp ngầm bất đồng bộ
        // Giữ màn hình 100% che phủ cho đến khi MainMenuScene render xong frame đầu tiên -> Triệt tiêu 100% lỗi lộ lại màn hình cũ!
        RealmTransitionManager.LoadSceneWithTransition("MainMenuScene", RealmTransitionManager.TransitionTheme.LoginIntro);
    }

    private void ShowStatusLoading(string msg)
    {
        if (statusFeedbackText != null)
        {
            statusFeedbackText.gameObject.SetActive(true);
            statusFeedbackText.color = new Color(0.4f, 0.85f, 1f);
            statusFeedbackText.text = msg;
        }
    }

    private void ShowStatusSuccess(string msg)
    {
        if (statusFeedbackText != null)
        {
            statusFeedbackText.gameObject.SetActive(true);
            statusFeedbackText.color = new Color(0.4f, 1f, 0.5f);
            statusFeedbackText.text = msg;
        }
    }

    private void ShowStatusError(string msg)
    {
        if (statusFeedbackText != null)
        {
            statusFeedbackText.gameObject.SetActive(true);
            statusFeedbackText.color = new Color(1f, 0.4f, 0.4f);
            statusFeedbackText.text = msg;
        }
    }

    public void OnQuitClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
