using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HỆ THỐNG TÀI KHOẢN, ĐẶT TÊN HIỂN THỊ (NICKNAME), HỒ SƠ PROFILE & BẢNG XẾP HẠNG RANK CHUẨN LIÊN QUÂN MOBILE
/// Kết nối trực tiếp tới cụm máy chủ Microsoft PlayFab (Title ID: 1BE56D).
/// Được tách bạch rõ ràng thành 3 Giao Diện Độc Lập (Không bao giờ gộp chung gây rối mắt):
/// 1. LOGIN WIZARD (Ngoài màn hình Mở Game - Xuất hiện TRƯỚC màn hình "Bắt Đầu Trò Chơi"):
///    - Khung nhỏ gọn giữa màn hình: Đăng Nhập, Đăng Ký, hoặc 1 chạm Facebook / Google / Zalo / Chơi Ngay (Guest).
///    - Đăng nhập xong chuyển qua bước ĐẶT TÊN HIỂN THỊ TRONG GAME (Ingame Nickname) -> Xong mới hiện màn hình "BẮT ĐẦU TRÒ CHƠI".
/// 2. PROFILE MODAL (Mục Quản Lý Profile ở Góc Trên Trái Sảnh Chính):
///    - Xem hồ sơ Chiến Binh, Đổi Tên Hiển Thị Trong Game, Liên kết tài khoản Guest, Đăng Xuất / Đổi Tài Khoản.
/// 3. RANK LEADERBOARD MODAL (Mục Rank / Bảng Xếp Hạng ở Sảnh Chính & Sảnh PvP):
///    - Xem bậc Rank hiện tại và Bảng Xếp Hạng Cao Thủ Toàn Server trên PlayFab 1BE56D.
/// </summary>
public class PlayFabAuthLoginUI : MonoBehaviour
{
    public static PlayFabAuthLoginUI Instance { get; private set; }
    public static bool IsModalOpen => Instance != null && Instance._modalRoot != null && Instance._modalRoot.activeInHierarchy;

    private enum ModalMode
    {
        LoginWizard = 0,
        PlayerProfile = 1,
        RankLeaderboard = 2
    }

    public enum LoginStep
    {
        Step1_ChooseMethodHub = 0,
        Step2_AccountLoginForm = 1,
        Step2_AccountRegisterForm = 2,
        Step2_GoogleEmailPrompt = 3,
        Step3_SetIngameNickname = 4
    }

    private GameObject _modalRoot;
    private Image _backdropImg;
    private Action _onLoginWizardFinished;

    // 3 Container độc lập cho 3 chế độ
    private GameObject _loginWizardWindow;
    private GameObject _profileWindow;
    private GameObject _rankLeaderboardWindow;

    // --- 1. LOGIN WIZARD ELEMENTS (Khung gọn đẹp chuẩn Liên Quân trước khi Bắt Đầu Trò Chơi) ---
    private GameObject _wizStep1Hub;
    private GameObject _wizStep2LoginForm;
    private GameObject _wizStep2RegisterForm;
    private GameObject _wizStep2GooglePrompt;
    private GameObject _wizStep3NicknameForm;
    private TextMeshProUGUI _wizStatusText;

    private TMP_InputField _googleEmailInput;

    private TMP_InputField _loginUserInput;
    private TMP_InputField _loginPassInput;
    private bool _loginPassVisible = false;
    private TextMeshProUGUI _loginEyeText;
    private bool _loginRememberMe = true;
    private TextMeshProUGUI _rememberBtnText;
    private Image _rememberBtnBg;
    private Image _rememberCheckIndicator;
    private TextMeshProUGUI _rememberCheckMark;

    private TMP_InputField _regUserInput;
    private TMP_InputField _regPassInput;
    private TMP_InputField _regConfirmPassInput;
    private TMP_InputField _regEmailInput;
    private bool _regPassVisible = false;
    private bool _regConfirmVisible = false;
    private TextMeshProUGUI _regEye1Text;
    private TextMeshProUGUI _regEye2Text;

    private TextMeshProUGUI _nicknameLoggedAccountLabel;
    private TMP_InputField _nicknameInput;

    // --- 2. PROFILE MODAL ELEMENTS (Quản Lý Profile trong Sảnh Chính) ---
    private Image _profAvatarImg;
    private Image _profHeroFullBodyImg;
    private TextMeshProUGUI _profDisplayNameText;
    private TextMeshProUGUI _profLevelText;
    private TextMeshProUGUI _profExpText;
    private RectTransform _profExpFillRt;
    private TextMeshProUGUI _profIdText;
    private TextMeshProUGUI _profHeroDescText;
    private TextMeshProUGUI _profHeroBadgeText;
    private TextMeshProUGUI _profShardsText;
    private TextMeshProUGUI _profRankTierText;
    private TextMeshProUGUI _profEloText;
    private TextMeshProUGUI _profTotalMatchesText;
    private TextMeshProUGUI _profWinsText;
    private TextMeshProUGUI _profWinRateText;
    private TextMeshProUGUI _profHeroesCountText;
    private TextMeshProUGUI _profAccountInfoText;
    private TextMeshProUGUI _profStatsText;
    private TMP_InputField _profRenameInput;
    private TextMeshProUGUI _profStatusText;
    private GameObject _profGuestLinkBox;
    private TMP_InputField _profLinkUserInput;
    private TMP_InputField _profLinkPassInput;
    private GameObject _profOverviewSubPanel;
    private GameObject _profAccountSubPanel;
    private GameObject _profRoadmapSubPanel;
    private Image _profSideOverviewBtnBg;
    private Image _profSideAccountBtnBg;
    private Image _profSideRoadmapBtnBg;

    // Localized Text Labels
    private TextMeshProUGUI _profHeaderTitleText;
    private TextMeshProUGUI _profSideOverviewBtnText;
    private TextMeshProUGUI _profSideArenaBtnText;
    private TextMeshProUGUI _profSideRoadmapBtnText;
    private TextMeshProUGUI _profSideAccountBtnText;
    private TextMeshProUGUI _profChangeHeroBtnText;
    private TextMeshProUGUI _profStatsHeaderTitle;
    private TextMeshProUGUI _profTitlesHeaderTitle;
    private TextMeshProUGUI _profTitlesContentText;
    private TextMeshProUGUI _profConnHeaderTitle;
    private TextMeshProUGUI _profOpenRankBtnText;
    private TextMeshProUGUI _profLogoutBtnText;
    private TextMeshProUGUI _profRenameTitleText;
    private TextMeshProUGUI _profSaveNameBtnText;
    private TextMeshProUGUI _profLinkTitleText;
    private TextMeshProUGUI _profLinkPermBtnText;
    private TextMeshProUGUI _profLinkGgBtnText;
    private TextMeshProUGUI _profAccountLogoutBtnText;
    private TextMeshProUGUI _profRoadmapHeaderTitle;
    private TextMeshProUGUI _profRoadmapContentText;

    private TextMeshProUGUI _profLblRank;
    private TextMeshProUGUI _profLblElo;
    private TextMeshProUGUI _profLblTotal;
    private TextMeshProUGUI _profLblWins;
    private TextMeshProUGUI _profLblRate;
    private TextMeshProUGUI _profLblHeroes;

    // --- 3. RANK LEADERBOARD ELEMENTS (Mục Rank / BXH trong Sảnh Chính) ---
    private TextMeshProUGUI _rankMySummaryText;
    private TextMeshProUGUI _rankStatusText;
    private Transform _rankRowsContainer;
    private readonly List<GameObject> _spawnedRankRows = new List<GameObject>();

    public static PlayFabAuthLoginUI EnsureInstance(Transform canvasTransform = null)
    {
        if (Instance != null && Instance._modalRoot != null) return Instance;

        if (canvasTransform == null)
        {
            var canvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas != null) canvasTransform = canvas.transform;
            if (canvasTransform == null)
            {
                var anyCanvas = FindAnyObjectByType<Canvas>();
                if (anyCanvas != null) canvasTransform = anyCanvas.transform;
            }
        }
        if (canvasTransform == null) return null;

        GameObject go = new GameObject("PlayFabAuthModalRoot", typeof(RectTransform));
        go.transform.SetParent(canvasTransform, false);
        Instance = go.AddComponent<PlayFabAuthLoginUI>();
        Instance.BuildAllModals(go.transform);
        return Instance;
    }

    /// <summary>
    /// Mở Khung Đăng Nhập Gọn Đẹp Chuẩn Liên Quân ở màn hình Mở Game (TRƯỚC màn hình "Bắt Đầu Trò Chơi")
    /// </summary>
    public static void OpenWizardForStartGame(Action onComplete, Transform canvasTransform = null)
    {
        var inst = EnsureInstance(canvasTransform);
        if (inst == null)
        {
            onComplete?.Invoke();
            return;
        }

        inst._onLoginWizardFinished = onComplete;
        inst.ActivateModalMode(ModalMode.LoginWizard);
        inst.ShowLoginStep(LoginStep.Step1_ChooseMethodHub);
    }

    /// <summary>
    /// Mở Mục Quản Lý Profile (tab = 0/1) hoặc Mục Bảng Xếp Hạng Rank (tab = 2) từ Sảnh Chính
    /// </summary>
    public static void OpenModal(int defaultTab = 0, Action onComplete = null)
    {
        var inst = EnsureInstance();
        if (inst == null) return;

        inst._onLoginWizardFinished = onComplete;
        if (defaultTab == 2)
        {
            inst.ActivateModalMode(ModalMode.RankLeaderboard);
            inst.RefreshRankLeaderboard();
        }
        else
        {
            var pf = PlayFabClientService.EnsureInstance();
            if (!pf.IsLoggedIn && SceneManager.GetActiveScene().name == "LoginScene")
            {
                inst.ActivateModalMode(ModalMode.LoginWizard);
                inst.ShowLoginStep(LoginStep.Step1_ChooseMethodHub);
            }
            else
            {
                inst.ActivateModalMode(ModalMode.PlayerProfile);
                inst.RefreshProfileModal();
            }
        }
    }

    public static void OpenProfileModal()
    {
        OpenModal(0);
    }

    public static void OpenRankModal()
    {
        OpenModal(2);
    }

    private void ActivateModalMode(ModalMode mode)
    {
        _modalRoot.SetActive(true);
        _modalRoot.transform.SetAsLastSibling();

        var pf = PlayFabClientService.EnsureInstance();
        pf.OnAuthStatusChanged -= HandleStatusChanged;
        pf.OnAuthStatusChanged += HandleStatusChanged;
        pf.OnLeaderboardLoaded -= HandleLeaderboardLoaded;
        pf.OnLeaderboardLoaded += HandleLeaderboardLoaded;

        if (_backdropImg != null)
        {
            // Ở màn hình Mở Game (LoginWizard), để nền trong suốt nhẹ để thấy cảnh nền game phía sau giống Liên Quân
            _backdropImg.color = (mode == ModalMode.LoginWizard)
                ? new Color(0.02f, 0.03f, 0.06f, 0.62f)
                : new Color(0.02f, 0.03f, 0.06f, 0.92f);
        }

        if (_loginWizardWindow != null) _loginWizardWindow.SetActive(mode == ModalMode.LoginWizard);
        if (_profileWindow != null) _profileWindow.SetActive(mode == ModalMode.PlayerProfile);
        if (_rankLeaderboardWindow != null) _rankLeaderboardWindow.SetActive(mode == ModalMode.RankLeaderboard);
    }

    public void CloseModal()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (PlayFabClientService.Instance != null)
        {
            PlayFabClientService.Instance.OnAuthStatusChanged -= HandleStatusChanged;
            PlayFabClientService.Instance.OnLeaderboardLoaded -= HandleLeaderboardLoaded;
        }
        if (_modalRoot != null) _modalRoot.SetActive(false);

        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.RefreshCurrencies();
            MainMenuManager.Instance.RefreshAllTexts();
        }
        if (PvPLobbyUI.Instance != null)
        {
            PvPLobbyUI.Instance.ShowStep(PvPLobbyUI.LobbyStep.Step2_ModeSelect);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged -= OnLocaleChanged;
        LocalizationManager.OnLanguageChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged()
    {
        if (_profileWindow != null && _profileWindow.activeSelf)
        {
            RefreshProfileModal();
        }
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= OnLocaleChanged;
        if (PlayFabClientService.Instance != null)
        {
            PlayFabClientService.Instance.OnAuthStatusChanged -= HandleStatusChanged;
            PlayFabClientService.Instance.OnLeaderboardLoaded -= HandleLeaderboardLoaded;
        }
        if (Instance == this) Instance = null;
    }

    private void HandleStatusChanged(string msg)
    {
        string cleanMsg = SanitizeNoBrackets(msg);
        if (_wizStatusText != null) _wizStatusText.text = cleanMsg;
        if (_profStatusText != null) _profStatusText.text = cleanMsg;
        if (_rankStatusText != null) _rankStatusText.text = cleanMsg;
    }

    private static string SanitizeNoBrackets(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("[", "")
                    .Replace("]", "")
                    .Replace("zsẮP", "SẮP")
                    .Replace("zSẮP", "SẮP")
                    .Replace("zgỢI", "GỢI")
                    .Replace("zGỢI", "GỢI");
    }

    public static string GetRankTierName(int elo)
    {
        if (elo >= 1600) return LocalizationManager.T("CAO THỦ", "MASTER");
        if (elo >= 1400) return LocalizationManager.T("KIM CƯƠNG", "DIAMOND");
        if (elo >= 1200) return LocalizationManager.T("VÀNG I", "GOLD I");
        if (elo >= 1050) return LocalizationManager.T("BẠC I", "SILVER I");
        return LocalizationManager.T("ĐỒNG III", "BRONZE III");
    }

    private void BuildAllModals(Transform root)
    {
        _modalRoot = root.gameObject;
        RectTransform rootRt = _modalRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        _backdropImg = _modalRoot.AddComponent<Image>();
        _backdropImg.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);

        BuildLoginWizardWindow(root);
        BuildProfileWindow(root);
        BuildRankLeaderboardWindow(root);

        GameFontManager.ApplyFontsGlobally(_modalRoot);
    }

    // =========================================================================
    // 1. KHUNG ĐĂNG NHẬP & ĐẶT TÊN HIỂN THỊ CHUẨN LIÊN QUÂN (TRƯỚC BẮT ĐẦU GAME)
    // =========================================================================
    private void BuildLoginWizardWindow(Transform root)
    {
        _loginWizardWindow = CreateBox(root, "LoginWizardWindow", new Vector2(0f, -45f), new Vector2(720f, 540f), new Color(0.07f, 0.09f, 0.16f, 0.98f));

        // Thanh trạng thái nhỏ gọn dưới đáy Card
        GameObject statusFooter = CreateBox(_loginWizardWindow.transform, "WizStatusFooter", new Vector2(0f, -238f), new Vector2(680f, 42f), new Color(0.04f, 0.05f, 0.09f, 1f));
        _wizStatusText = CreateText(statusFooter.transform,
            LocalizationManager.T("Chọn phương thức đăng nhập để vào trò chơi", "Select a login method to continue"),
            16f, Vector2.zero, new Vector2(650f, 34f), new Color(0.55f, 0.90f, 1f));

        // --- MÀN HÌNH 1: CHỌN ĐĂNG NHẬP / ĐĂNG KÝ (Gọn sạch giống hệt Liên Quân) ---
        _wizStep1Hub = CreateBox(_loginWizardWindow.transform, "WizStep1_Hub", new Vector2(0f, 20f), new Vector2(680f, 450f), new Color(0f, 0f, 0f, 0f));

        CreateText(_wizStep1Hub.transform,
            LocalizationManager.T("ĐĂNG NHẬP / ĐĂNG KÝ", "LOGIN / REGISTER"),
            28f, new Vector2(0f, 185f), new Vector2(600f, 42f), new Color(1f, 0.88f, 0.35f));

        // Nút lớn 1: ĐĂNG NHẬP
        CreateButton(_wizStep1Hub.transform, "Btn_Wiz_GoLogin",
            LocalizationManager.T("ĐĂNG NHẬP TÀI KHOẢN", "ACCOUNT LOGIN"),
            new Vector2(0f, 110f), new Vector2(540f, 66f),
            new Color(0.18f, 0.50f, 0.92f, 1f), 23f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowLoginStep(LoginStep.Step2_AccountLoginForm);
            });

        // Nút lớn 2: ĐĂNG KÝ
        CreateButton(_wizStep1Hub.transform, "Btn_Wiz_GoRegister",
            LocalizationManager.T("ĐĂNG KÝ TÀI KHOẢN MỚI", "REGISTER NEW ACCOUNT"),
            new Vector2(0f, 28f), new Vector2(540f, 66f),
            new Color(0.32f, 0.36f, 0.56f, 1f), 22f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowLoginStep(LoginStep.Step2_AccountRegisterForm);
            });

        CreateText(_wizStep1Hub.transform,
            LocalizationManager.T("--------  Hoặc liên kết nhanh bằng  --------", "--------  Or quick sign in with  --------"),
            16f, new Vector2(0f, -28f), new Vector2(540f, 26f), new Color(0.72f, 0.78f, 0.88f));

        // Nút ĐĂNG NHẬP BẰNG GOOGLE (Đồng bộ tài khoản Cloud qua nhiều điện thoại & PC)
        CreateButton(_wizStep1Hub.transform, "Btn_Wiz_Google",
            LocalizationManager.T("ĐĂNG NHẬP BẰNG GOOGLE", "SIGN IN WITH GOOGLE"),
            new Vector2(0f, -84f), new Vector2(540f, 62f),
            new Color(0.85f, 0.25f, 0.20f, 1f), 22f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                OnGoogleLoginButtonClicked();
            });

        // Nút dưới cùng: CHƠI OFFLINE (KHÔNG CẦN TÀI KHOẢN)
        CreateButton(_wizStep1Hub.transform, "Btn_Wiz_Offline",
            LocalizationManager.T("CHƠI OFFLINE (KHÔNG CẦN TÀI KHOẢN)", "PLAY OFFLINE (NO ACCOUNT)"),
            new Vector2(0f, -162f), new Vector2(540f, 60f),
            new Color(0.18f, 0.58f, 0.42f, 1f), 20f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                StartOfflineMode();
            });

        // --- MÀN HÌNH 2A: ĐIỀN TÀI KHOẢN ĐĂNG NHẬP ---
        _wizStep2LoginForm = CreateBox(_loginWizardWindow.transform, "WizStep2_LoginForm", new Vector2(0f, 20f), new Vector2(680f, 450f), new Color(0f, 0f, 0f, 0f));

        CreateText(_wizStep2LoginForm.transform,
            LocalizationManager.T("ĐĂNG NHẬP TÀI KHOẢN", "ACCOUNT LOGIN"),
            26f, new Vector2(0f, 185f), new Vector2(600f, 40f), new Color(1f, 0.88f, 0.35f));

        _loginUserInput = CreateSafeInputField(_wizStep2LoginForm.transform, "LoginUserInput",
            new Vector2(0f, 114f), new Vector2(560f, 54f),
            LocalizationManager.T("Tên tài khoản hoặc Email...", "Username or Email..."), false);
        _loginUserInput.onValueChanged.AddListener((val) =>
        {
            if (_loginRememberMe && !string.IsNullOrEmpty(val))
            {
                PlayerPrefs.SetString("Auth_SavedUsername", val.Trim());
                PlayerPrefs.Save();
            }
        });

        _loginPassInput = CreateSafeInputField(_wizStep2LoginForm.transform, "LoginPassInput",
            new Vector2(-50f, 46f), new Vector2(460f, 54f),
            LocalizationManager.T("Mật khẩu...", "Password..."), true);
        _loginPassInput.onValueChanged.AddListener((val) =>
        {
            if (_loginRememberMe)
            {
                PlayerPrefs.SetString("Auth_SavedPassword", val);
                PlayerPrefs.Save();
            }
        });

        GameObject loginEyeBtn = CreateButton(_wizStep2LoginForm.transform, "Btn_LoginEye",
            LocalizationManager.T("HIỆN", "SHOW"),
            new Vector2(235f, 46f), new Vector2(90f, 54f), new Color(0.18f, 0.24f, 0.36f, 1f), 16f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                _loginPassVisible = !_loginPassVisible;
                TogglePasswordInput(_loginPassInput, _loginPassVisible, _loginEyeText);
            });
        _loginEyeText = loginEyeBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Nút Ghi Nhớ Đăng Nhập (Remember Me) - Thiết kế Checkbox riêng biệt, không lỗi font
        GameObject remBtnObj = CreateButton(_wizStep2LoginForm.transform, "Btn_RememberLogin",
            "",
            new Vector2(0f, -14f), new Vector2(560f, 46f),
            new Color(0.10f, 0.24f, 0.16f, 0.95f), 17f, ToggleRememberMe);
        _rememberBtnBg = remBtnObj.GetComponent<Image>();

        GameObject checkIndicatorObj = CreateBox(remBtnObj.transform, "CheckIndicator", new Vector2(-235f, 0f), new Vector2(26f, 26f), new Color(0.04f, 0.06f, 0.10f, 1f));
        _rememberCheckIndicator = checkIndicatorObj.GetComponent<Image>();
        _rememberCheckMark = CreateText(checkIndicatorObj.transform, "V", 16f, Vector2.zero, new Vector2(26f, 26f), new Color(0.20f, 0.95f, 0.45f, 1f));

        _rememberBtnText = remBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (_rememberBtnText != null)
        {
            RectTransform txtRt = _rememberBtnText.GetComponent<RectTransform>();
            if (txtRt != null)
            {
                txtRt.anchoredPosition = new Vector2(20f, 0f);
                txtRt.sizeDelta = new Vector2(460f, 38f);
            }
            _rememberBtnText.alignment = TextAlignmentOptions.Left;
            _rememberBtnText.fontSize = 17f;
        }

        _loginRememberMe = PlayerPrefs.GetInt("Auth_RememberLogin", 1) == 1;
        UpdateRememberMeButtonUI();
        PreFillRememberedCredentials();

        CreateButton(_wizStep2LoginForm.transform, "Btn_SubmitLogin",
            LocalizationManager.T("ĐĂNG NHẬP", "LOGIN"),
            new Vector2(0f, -74f), new Vector2(560f, 60f),
            new Color(0.92f, 0.62f, 0.12f, 1f), 24f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                string u = _loginUserInput != null ? _loginUserInput.text.Trim() : "";
                string p = _loginPassInput != null ? _loginPassInput.text : "";

                if (_loginRememberMe && !string.IsNullOrEmpty(u))
                {
                    PlayerPrefs.SetInt("Auth_RememberLogin", 1);
                    PlayerPrefs.SetString("Auth_SavedUsername", u);
                    PlayerPrefs.SetString("Auth_SavedPassword", p);
                    AccountDatabase.SetRememberMe(u, p, true);
                    PlayerPrefs.Save();
                }
                else if (!_loginRememberMe)
                {
                    PlayerPrefs.SetInt("Auth_RememberLogin", 0);
                    PlayerPrefs.DeleteKey("Auth_SavedUsername");
                    PlayerPrefs.DeleteKey("Auth_SavedPassword");
                    AccountDatabase.SetRememberMe("", "", false);
                    PlayerPrefs.Save();
                }

                PlayFabClientService.EnsureInstance().LoginWithUsernameOrEmail(u, p, (ok, _) =>
                {
                    if (ok)
                    {
                        if (_loginRememberMe && !string.IsNullOrEmpty(u))
                        {
                            PlayerPrefs.SetInt("Auth_RememberLogin", 1);
                            PlayerPrefs.SetString("Auth_SavedUsername", u);
                            PlayerPrefs.SetString("Auth_SavedPassword", p);
                            AccountDatabase.SetRememberMe(u, p, true);
                            PlayerPrefs.Save();
                        }
                        OnAuthenticationSuccessProceedToNickname();
                    }
                });
            });

        CreateButton(_wizStep2LoginForm.transform, "Btn_LoginBack",
            LocalizationManager.T("< Quay Lại", "< Back"),
            new Vector2(-150f, -145f), new Vector2(260f, 50f),
            new Color(0.22f, 0.26f, 0.36f, 1f), 19f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowLoginStep(LoginStep.Step1_ChooseMethodHub);
            });

        CreateButton(_wizStep2LoginForm.transform, "Btn_LoginToRegister",
            LocalizationManager.T("Đăng Ký Tài Khoản Mới", "Register New Account"),
            new Vector2(150f, -145f), new Vector2(260f, 50f),
            new Color(0.28f, 0.34f, 0.52f, 1f), 18f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowLoginStep(LoginStep.Step2_AccountRegisterForm);
            });

        // --- MÀN HÌNH 2B: ĐIỀN THÔNG TIN ĐĂNG KÝ TÀI KHOẢN CHAY (Đủ 4 ô chuẩn sơ đồ) ---
        _wizStep2RegisterForm = CreateBox(_loginWizardWindow.transform, "WizStep2_RegisterForm", new Vector2(0f, 20f), new Vector2(680f, 450f), new Color(0f, 0f, 0f, 0f));

        CreateText(_wizStep2RegisterForm.transform,
            LocalizationManager.T("ĐĂNG KÝ TÀI KHOẢN MỚI", "CREATE NEW ACCOUNT"),
            25f, new Vector2(0f, 192f), new Vector2(600f, 38f), new Color(1f, 0.88f, 0.35f));

        _regUserInput = CreateSafeInputField(_wizStep2RegisterForm.transform, "RegUserInput",
            new Vector2(0f, 134f), new Vector2(560f, 50f),
            LocalizationManager.T("Tên tài khoản (từ 3 ký tự)...", "Username (min 3 chars)..."), false);

        _regPassInput = CreateSafeInputField(_wizStep2RegisterForm.transform, "RegPassInput",
            new Vector2(-50f, 72f), new Vector2(460f, 50f),
            LocalizationManager.T("Mật khẩu (từ 6 ký tự)...", "Password (min 6 chars)..."), true);

        GameObject regEye1 = CreateButton(_wizStep2RegisterForm.transform, "Btn_RegEye1",
            LocalizationManager.T("HIỆN", "SHOW"),
            new Vector2(235f, 72f), new Vector2(90f, 50f), new Color(0.18f, 0.24f, 0.36f, 1f), 16f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                _regPassVisible = !_regPassVisible;
                TogglePasswordInput(_regPassInput, _regPassVisible, _regEye1Text);
            });
        _regEye1Text = regEye1.GetComponentInChildren<TextMeshProUGUI>();

        _regConfirmPassInput = CreateSafeInputField(_wizStep2RegisterForm.transform, "RegConfirmPassInput",
            new Vector2(-50f, 10f), new Vector2(460f, 50f),
            LocalizationManager.T("Nhập lại mật khẩu...", "Confirm password..."), true);

        GameObject regEye2 = CreateButton(_wizStep2RegisterForm.transform, "Btn_RegEye2",
            LocalizationManager.T("HIỆN", "SHOW"),
            new Vector2(235f, 10f), new Vector2(90f, 50f), new Color(0.18f, 0.24f, 0.36f, 1f), 16f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                _regConfirmVisible = !_regConfirmVisible;
                TogglePasswordInput(_regConfirmPassInput, _regConfirmVisible, _regEye2Text);
            });
        _regEye2Text = regEye2.GetComponentInChildren<TextMeshProUGUI>();

        _regEmailInput = CreateSafeInputField(_wizStep2RegisterForm.transform, "RegEmailInput",
            new Vector2(0f, -52f), new Vector2(560f, 50f),
            LocalizationManager.T("Email (tùy chọn)...", "Email (optional)..."), false);

        CreateButton(_wizStep2RegisterForm.transform, "Btn_SubmitRegister",
            LocalizationManager.T("ĐĂNG KÝ", "REGISTER"),
            new Vector2(100f, -126f), new Vector2(360f, 58f),
            new Color(0.92f, 0.62f, 0.12f, 1f), 23f, OnWizardRegisterClicked);

        CreateButton(_wizStep2RegisterForm.transform, "Btn_RegBack",
            LocalizationManager.T("< Quay Lại", "< Back"),
            new Vector2(-190f, -126f), new Vector2(180f, 58f),
            new Color(0.22f, 0.26f, 0.36f, 1f), 19f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowLoginStep(LoginStep.Step1_ChooseMethodHub);
            });

        // --- MÀN HÌNH 2C: ĐỒNG BỘ TÀI KHOẢN GOOGLE CLOUD LINH HOẠT TRÊN MỌI THIẾT BỊ ---
        _wizStep2GooglePrompt = CreateBox(_loginWizardWindow.transform, "WizStep2_GooglePrompt", new Vector2(0f, 20f), new Vector2(680f, 450f), new Color(0f, 0f, 0f, 0f));

        CreateText(_wizStep2GooglePrompt.transform,
            LocalizationManager.T("ĐĂNG NHẬP TÀI KHOẢN GOOGLE", "GOOGLE ACCOUNT LOGIN"),
            26f, new Vector2(0f, 185f), new Vector2(600f, 40f), new Color(1f, 0.88f, 0.35f));

        CreateText(_wizStep2GooglePrompt.transform,
            LocalizationManager.T("Nhập Email Google để đồng bộ toàn bộ tài khoản, linh hồn & rank trên MỌI điện thoại và PC:",
                                  "Enter Google Email to sync your account, souls & rank across all phones & PC:"),
            17f, new Vector2(0f, 120f), new Vector2(580f, 48f), new Color(0.85f, 0.90f, 1f));

        _googleEmailInput = CreateSafeInputField(_wizStep2GooglePrompt.transform, "GoogleEmailInput",
            new Vector2(0f, 40f), new Vector2(560f, 58f),
            LocalizationManager.T("Email Google (VD: yourname@gmail.com)...", "Google Email (e.g. yourname@gmail.com)..."), false);

        CreateButton(_wizStep2GooglePrompt.transform, "Btn_ConfirmGoogleEmail",
            LocalizationManager.T("ĐỒNG BỘ CLOUD & VÀO GAME", "SYNC CLOUD & CONTINUE"),
            new Vector2(0f, -48f), new Vector2(560f, 64f),
            new Color(0.85f, 0.25f, 0.20f, 1f), 22f, OnConfirmGoogleEmailClicked);

        CreateButton(_wizStep2GooglePrompt.transform, "Btn_GoogleBack",
            LocalizationManager.T("< Quay Lại", "< Back"),
            new Vector2(-150f, -145f), new Vector2(240f, 50f),
            new Color(0.22f, 0.26f, 0.36f, 1f), 19f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowLoginStep(LoginStep.Step1_ChooseMethodHub);
            });

        if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
        {
            CreateButton(_wizStep2GooglePrompt.transform, "Btn_GoogleBrowserLive",
                LocalizationManager.T("Mở Trình Duyệt Web >", "Open Web Browser >"),
                new Vector2(150f, -145f), new Vector2(240f, 50f),
                new Color(0.18f, 0.50f, 0.92f, 1f), 17f, () =>
                {
                    SoundManager.EnsureInstance()?.PlayButtonClick();
                    OAuthAuthenticator.EnsureInstance().StartSocialLogin("Google",
                        onSuccess: (profile) =>
                        {
                            string email = !string.IsNullOrEmpty(profile.email) ? profile.email : profile.id;
                            string disp = !string.IsNullOrEmpty(profile.name) ? profile.name : "GoogleGamer";
                            PlayerPrefs.SetString("OAuth_LastGoogleEmail", email);
                            PlayerPrefs.Save();
                            PlayFabClientService.EnsureInstance().LoginWithDirectSocialAccount("Google", email, disp, (ok, _) =>
                            {
                                if (ok) OnAuthenticationSuccessProceedToNickname();
                            });
                        },
                        onFailed: (err) => HandleStatusChanged(err),
                        onStatus: (msg) => HandleStatusChanged(msg));
                });
        }
        else
        {
            CreateButton(_wizStep2GooglePrompt.transform, "Btn_OpenAndroidAccountPicker",
                LocalizationManager.T("Chọn Tài Khoản Trên Máy >", "Choose Device Account >"),
                new Vector2(150f, -145f), new Vector2(240f, 50f),
                new Color(0.18f, 0.50f, 0.92f, 1f), 17f, () =>
                {
                    SoundManager.EnsureInstance()?.PlayButtonClick();
                    LaunchNativeGoogleAccountPicker();
                });
        }

        // --- MÀN HÌNH 3: ĐẶT TÊN HIỂN THỊ TRONG GAME (INGAME NICKNAME - CHUẨN LIÊN QUÂN) ---
        _wizStep3NicknameForm = CreateBox(_loginWizardWindow.transform, "WizStep3_NicknameForm", new Vector2(0f, 20f), new Vector2(680f, 450f), new Color(0f, 0f, 0f, 0f));

        CreateText(_wizStep3NicknameForm.transform,
            LocalizationManager.T("ĐĂNG NHẬP THÀNH CÔNG!", "LOGIN SUCCESSFUL!"),
            28f, new Vector2(0f, 175f), new Vector2(620f, 42f), new Color(0.35f, 1f, 0.55f));

        _nicknameLoggedAccountLabel = CreateText(_wizStep3NicknameForm.transform, "",
            19f, new Vector2(0f, 122f), new Vector2(620f, 34f), new Color(0.85f, 0.92f, 1f));

        CreateText(_wizStep3NicknameForm.transform,
            LocalizationManager.T("ĐẶT TÊN HIỂN THỊ TRONG GAME (TÊN CHIẾN BINH):", "ENTER YOUR IN-GAME DISPLAY NAME:"),
            20f, new Vector2(0f, 58f), new Vector2(560f, 32f), new Color(1f, 0.88f, 0.35f));

        _nicknameInput = CreateSafeInputField(_wizStep3NicknameForm.transform, "IngameNicknameInput",
            new Vector2(0f, 0f), new Vector2(560f, 62f),
            LocalizationManager.T("Nhập tên hiển thị trong game (VD: TruongHung)...", "Enter in-game display name..."), false);

        CreateButton(_wizStep3NicknameForm.transform, "Btn_ConfirmNicknameAndEnter",
            LocalizationManager.T("XÁC NHẬN & TIẾP TỤC", "CONFIRM & CONTINUE"),
            new Vector2(0f, -95f), new Vector2(460f, 68f),
            new Color(0.95f, 0.64f, 0.12f, 1f), 24f, OnConfirmNicknameClicked);

        CreateButton(_wizStep3NicknameForm.transform, "Btn_SwitchAccountFromStep3",
            LocalizationManager.T("Đổi Tài Khoản Khác", "Switch Account"),
            new Vector2(0f, -172f), new Vector2(260f, 44f),
            new Color(0.22f, 0.26f, 0.36f, 1f), 17f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                PlayFabClientService.EnsureInstance().LogoutSession();
                ShowLoginStep(LoginStep.Step1_ChooseMethodHub);
            });
    }

    private void ShowLoginStep(LoginStep step)
    {
        if (_wizStep1Hub != null) _wizStep1Hub.SetActive(step == LoginStep.Step1_ChooseMethodHub);
        if (_wizStep2LoginForm != null)
        {
            _wizStep2LoginForm.SetActive(step == LoginStep.Step2_AccountLoginForm);
            if (step == LoginStep.Step2_AccountLoginForm)
            {
                PreFillRememberedCredentials();
            }
        }
        if (_wizStep2RegisterForm != null) _wizStep2RegisterForm.SetActive(step == LoginStep.Step2_AccountRegisterForm);
        if (_wizStep2GooglePrompt != null)
        {
            _wizStep2GooglePrompt.SetActive(step == LoginStep.Step2_GoogleEmailPrompt);
            if (step == LoginStep.Step2_GoogleEmailPrompt && _googleEmailInput != null && string.IsNullOrEmpty(_googleEmailInput.text))
            {
                string lastEmail = PlayerPrefs.GetString("OAuth_LastGoogleEmail", "");
                if (!string.IsNullOrEmpty(lastEmail))
                {
                    _googleEmailInput.text = lastEmail;
                }
            }
        }
        if (_wizStep3NicknameForm != null) _wizStep3NicknameForm.SetActive(step == LoginStep.Step3_SetIngameNickname);
    }

    private void ToggleRememberMe()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        _loginRememberMe = !_loginRememberMe;
        PlayerPrefs.SetInt("Auth_RememberLogin", _loginRememberMe ? 1 : 0);
        if (_loginRememberMe)
        {
            string curU = _loginUserInput != null ? _loginUserInput.text.Trim() : "";
            string curP = _loginPassInput != null ? _loginPassInput.text : "";
            if (!string.IsNullOrEmpty(curU))
            {
                PlayerPrefs.SetString("Auth_SavedUsername", curU);
                PlayerPrefs.SetString("Auth_SavedPassword", curP);
                AccountDatabase.SetRememberMe(curU, curP, true);
            }
            else
            {
                PreFillRememberedCredentials();
            }
        }
        else
        {
            PlayerPrefs.DeleteKey("Auth_SavedUsername");
            PlayerPrefs.DeleteKey("Auth_SavedPassword");
            AccountDatabase.SetRememberMe("", "", false);
        }
        PlayerPrefs.Save();
        UpdateRememberMeButtonUI();
    }

    private void UpdateRememberMeButtonUI()
    {
        if (_rememberBtnText != null)
        {
            _rememberBtnText.text = LocalizationManager.T("GHI NHỚ ĐĂNG NHẬP", "REMEMBER LOGIN");
            _rememberBtnText.color = _loginRememberMe
                ? new Color(0.40f, 1f, 0.65f, 1f)
                : new Color(0.65f, 0.70f, 0.80f, 0.80f);
        }

        if (_rememberCheckMark != null)
        {
            _rememberCheckMark.text = _loginRememberMe ? "V" : "";
            _rememberCheckMark.gameObject.SetActive(_loginRememberMe);
        }

        if (_rememberCheckIndicator != null)
        {
            _rememberCheckIndicator.color = _loginRememberMe
                ? new Color(0.12f, 0.45f, 0.22f, 1f)
                : new Color(0.06f, 0.08f, 0.12f, 1f);
        }

        if (_rememberBtnBg != null)
        {
            _rememberBtnBg.color = _loginRememberMe
                ? new Color(0.10f, 0.24f, 0.16f, 0.95f)
                : new Color(0.12f, 0.14f, 0.20f, 0.85f);
        }
    }

    private void PreFillRememberedCredentials()
    {
        _loginRememberMe = PlayerPrefs.GetInt("Auth_RememberLogin", 1) == 1;
        UpdateRememberMeButtonUI();

        if (_loginRememberMe)
        {
            string savedUser = PlayerPrefs.GetString("Auth_SavedUsername", "");
            string savedPass = PlayerPrefs.GetString("Auth_SavedPassword", "");

            if (string.IsNullOrEmpty(savedUser) || string.IsNullOrEmpty(savedPass))
            {
                if (AccountDatabase.GetRememberedCredentials(out string dbUser, out string dbPass))
                {
                    if (string.IsNullOrEmpty(savedUser)) savedUser = dbUser;
                    if (string.IsNullOrEmpty(savedPass)) savedPass = dbPass;
                }
            }

            // Fallback lấy tài khoản đăng nhập gần nhất (Google email hoặc Local user)
            if (string.IsNullOrEmpty(savedUser))
            {
                string lastEmail = PlayerPrefs.GetString("OAuth_LastGoogleEmail", "");
                if (!string.IsNullOrEmpty(lastEmail))
                {
                    savedUser = lastEmail;
                }
                else
                {
                    string activeUser = PlayerPrefs.GetString("Player_Active_Username", "");
                    if (!string.IsNullOrEmpty(activeUser) && activeUser != "WANDERER" && activeUser != "Player")
                    {
                        savedUser = activeUser;
                    }
                    else
                    {
                        string pName = PlayerPrefs.GetString("Player_Name", "");
                        if (!string.IsNullOrEmpty(pName) && pName != "WANDERER" && pName != "Player")
                        {
                            savedUser = pName;
                        }
                    }
                }
            }

            if (_loginUserInput != null && !string.IsNullOrEmpty(savedUser))
            {
                _loginUserInput.text = savedUser;
                PlayerPrefs.SetString("Auth_SavedUsername", savedUser);
                PlayerPrefs.Save();
            }
            if (_loginPassInput != null && !string.IsNullOrEmpty(savedPass))
            {
                _loginPassInput.text = savedPass;
            }
        }
    }

    private void OnGoogleLoginButtonClicked()
    {
        if (AndroidGoogleSignInBridge.IsSupported)
        {
            LaunchNativeGoogleAccountPicker();
            return;
        }

        // Trên PC / Editor: Mở thẳng Google OAuth 2.0 trên trình duyệt,
        // bỏ qua hoàn toàn màn hình trung gian đồng bộ Cloud theo đúng yêu cầu
        TriggerOneClickSocialLogin("Google");
    }

    private void LaunchNativeGoogleAccountPicker()
    {
        OAuthAuthenticator.EnsureInstance();
        HandleStatusChanged(LocalizationManager.T(
            "Đang mở popup chọn tài khoản Google của máy...",
            "Opening device Google account selector..."));

        AndroidGoogleSignInBridge.LaunchAccountPicker(
            onSuccess: (profile) => OnNativeGooglePickerProfileSuccess(profile),
            onCanceled: (reason) => OnNativeGooglePickerCanceled(reason),
            onError: (err) => OnNativeGooglePickerFailed(err)
        );
    }

    public void OnNativeGooglePickerProfileSuccess(AndroidGoogleSignInBridge.GoogleAccountProfile profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.email))
        {
            OnNativeGooglePickerFailed(LocalizationManager.T("Không lấy được địa chỉ Email từ tài khoản đã chọn.", "Could not retrieve email from selected account."));
            return;
        }

        string chosenEmail = profile.email.Trim();
        string displayName = profile.displayName != null ? profile.displayName.Trim() : "";
        string photoUrl = profile.photoUrl != null ? profile.photoUrl.Trim() : "";

        string suggested = !string.IsNullOrEmpty(displayName) ? displayName : chosenEmail.Split('@')[0];

        // TỰ ĐỘNG BÙ ĐẮP AVATAR: Nếu thiết bị Android không cấp photoUrl trực tiếp (do cơ chế bảo mật AccountManager),
        // tự động tạo Avatar Hiệp Sĩ màu dạ quang xanh cực đẹp theo tên người chơi (UI-Avatars HD 256x256)
        if (string.IsNullOrEmpty(photoUrl))
        {
            string cleanName = Uri.EscapeDataString(suggested);
            photoUrl = $"https://ui-avatars.com/api/?name={cleanName}&background=0f172a&color=38bdf8&size=256&bold=true&format=png";
        }

        HandleStatusChanged(LocalizationManager.T(
            $"Đã chọn tài khoản: {chosenEmail}. Đang kết nối máy chủ PlayFab...",
            $"Selected account: {chosenEmail}. Connecting to PlayFab server..."));

        PlayerPrefs.SetString("OAuth_LastGoogleEmail", chosenEmail);
        if (!string.IsNullOrEmpty(displayName))
        {
            OAuthAuthenticator.LastGoogleUserName = displayName;
            PlayerPrefs.SetString("OAuth_LastGoogleName", displayName);
        }

        if (!string.IsNullOrEmpty(photoUrl))
        {
            OAuthAuthenticator.SaveAndDownloadAvatar(photoUrl);
        }
        PlayerPrefs.Save();
        PlayFabClientService.EnsureInstance().LoginWithDirectSocialAccount("Google", chosenEmail, suggested, (ok, msg) =>
        {
            if (ok)
            {
                // Đồng bộ avatar: có ảnh -> lưu lên PlayFab; không có (AccountManager) -> lấy ảnh từ PlayFab
                PlayFabClientService.EnsureInstance().SyncGoogleAvatar(photoUrl);
                OnAuthenticationSuccessProceedToNickname();
            }
            else
            {
                string errReason = !string.IsNullOrEmpty(msg)
                    ? msg
                    : LocalizationManager.T("Không thể kết nối tới máy chủ PlayFab. Vui lòng kiểm tra mạng Internet!", "Could not connect to PlayFab server. Please check your Internet connection!");
                HandleStatusChanged($"<color=#FF5555>{errReason}</color>");
                ShowLoginStep(LoginStep.Step2_GoogleEmailPrompt);
            }
        });
    }

    public void OnNativeGooglePickerSuccess(string chosenEmail)
    {
        OnNativeGooglePickerProfileSuccess(new AndroidGoogleSignInBridge.GoogleAccountProfile
        {
            email = chosenEmail,
            displayName = "",
            photoUrl = ""
        });
    }

    public void OnNativeGooglePickerSuccessJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var prof = JsonUtility.FromJson<AndroidGoogleSignInBridge.GoogleAccountProfile>(json);
            if (prof != null && !string.IsNullOrEmpty(prof.email))
            {
                OnNativeGooglePickerProfileSuccess(prof);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayFabAuthLoginUI] Lỗi parse JSON profile: {ex.Message}");
        }
    }

    public void OnNativeGooglePickerCanceled(string reason)
    {
        HandleStatusChanged(LocalizationManager.T(
            "<color=#FFD766>Đã đóng bảng chọn Google. Bạn có thể bấm 'Chọn Tài Khoản Trên Máy' hoặc nhập trực tiếp Email Google bên dưới:</color>",
            "<color=#FFD766>Closed Google selector. You can tap 'Choose Device Account' or enter your Google Email below:</color>"));
        ShowLoginStep(LoginStep.Step2_GoogleEmailPrompt);
    }

    public void OnNativeGooglePickerFailed(string err)
    {
        if (AndroidGoogleSignInBridge.IsSupported)
        {
            string cleanReason = string.IsNullOrEmpty(err) ? "Không phản hồi" : err;
            HandleStatusChanged(LocalizationManager.T(
                $"<color=#FF6666>Không mở được danh sách Google trên máy ({cleanReason}). Vui lòng nhập Email Google bên dưới:</color>",
                $"<color=#FF6666>Could not open device Google picker ({cleanReason}). Please enter your Google Email below:</color>"));
            ShowLoginStep(LoginStep.Step2_GoogleEmailPrompt);
            return;
        }

        HandleStatusChanged(LocalizationManager.T(
            $"Không mở được popup Google: {err}. Đang chuyển sang trình duyệt web...",
            $"Could not open Google popup: {err}. Switching to browser..."));
        TriggerOneClickSocialLogin("Google");
    }

    private void OnConfirmGoogleEmailClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string email = _googleEmailInput != null ? _googleEmailInput.text.Trim() : "";
        if (string.IsNullOrEmpty(email) || !email.Contains("@") || email.Length < 6)
        {
            HandleStatusChanged(LocalizationManager.T("Vui lòng nhập địa chỉ Email Google hợp lệ (VD: yourname@gmail.com)!", "Please enter a valid Google Email address!"));
            return;
        }

        PlayerPrefs.SetString("OAuth_LastGoogleEmail", email);
        PlayerPrefs.Save();

        string suggested = email.Split('@')[0];
        HandleStatusChanged(LocalizationManager.T($"Đang đồng bộ tài khoản Google Cloud: {email}...", $"Syncing Google Cloud: {email}..."));
        PlayFabClientService.EnsureInstance().LoginWithDirectSocialAccount("Google", email, suggested, (ok, msg) =>
        {
            if (ok)
            {
                // Đồng bộ avatar: có ảnh -> lưu lên PlayFab; không có (AccountManager) -> lấy ảnh từ PlayFab
                PlayFabClientService.EnsureInstance().SyncGoogleAvatar("");
                OnAuthenticationSuccessProceedToNickname();
            }
            else
            {
                HandleStatusChanged(msg);
            }
        });
    }

    private void TriggerOneClickSocialLogin(string provider = "Google")
    {
        ActivateModalMode(ModalMode.LoginWizard);
        HandleStatusChanged(LocalizationManager.T("Đang mở Google OAuth 2.0 (accounts.google.com)...", "Opening Google OAuth 2.0..."));

        PlayFabClientService.EnsureInstance().LoginWithOneClickSocialOAuth("Google", (ok, _) =>
        {
            if (ok) OnAuthenticationSuccessProceedToNickname();
        });
    }

    private void OnWizardRegisterClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string u = _regUserInput != null ? _regUserInput.text.Trim() : "";
        string p = _regPassInput != null ? _regPassInput.text : "";
        string cp = _regConfirmPassInput != null ? _regConfirmPassInput.text : "";
        string e = _regEmailInput != null ? _regEmailInput.text.Trim() : "";

        if (u.Length < 3)
        {
            HandleStatusChanged(LocalizationManager.T("Tên tài khoản phải từ 3 ký tự trở lên!", "Username must be at least 3 characters!"));
            return;
        }
        if (p.Length < 6)
        {
            HandleStatusChanged(LocalizationManager.T("Mật khẩu phải từ 6 ký tự trở lên!", "Password must be at least 6 characters!"));
            return;
        }
        if (p != cp)
        {
            HandleStatusChanged(LocalizationManager.T("Mật khẩu nhập lại không khớp!", "Confirm password does not match!"));
            return;
        }

        PlayFabClientService.EnsureInstance().RegisterAccount(u, e, p, (ok, _) =>
        {
            if (ok)
            {
                // Tự động ghi nhớ tài khoản và mật khẩu vừa đăng ký mới
                PlayerPrefs.SetInt("Auth_RememberLogin", 1);
                PlayerPrefs.SetString("Auth_SavedUsername", u);
                PlayerPrefs.SetString("Auth_SavedPassword", p);
                AccountDatabase.SetRememberMe(u, p, true);
                PlayerPrefs.Save();

                OnAuthenticationSuccessProceedToNickname();
            }
        });
    }

    public void StartOfflineMode()
    {
        PlayFabClientService.IsOfflineMode = true;
        PlayerPrefs.SetInt("IsOfflineMode", 1);
        PlayerPrefs.Save();

        var acc = AccountDatabase.GetActiveAccount();
        if (acc == null)
        {
            string savedOfflineName = PlayerPrefs.GetString("Player_Name", "Hiệp Sĩ");
            if (savedOfflineName == "Player" || savedOfflineName == "Wanderer") savedOfflineName = "Hiệp Sĩ";
            AccountDatabase.Register("OfflineGamer", "offline", out _);
            AccountDatabase.Login("OfflineGamer", "offline", out acc, out _);
            if (acc != null)
            {
                acc.displayName = savedOfflineName;
                AccountDatabase.SaveOrUpdateCloudAccount(acc);
            }
        }
        else if (acc.displayName == "Player" || acc.displayName == "Wanderer")
        {
            acc.displayName = "Hiệp Sĩ";
            AccountDatabase.SaveOrUpdateCloudAccount(acc);
        }

        FinishLoginAndEnterGame();
    }

    private void FinishLoginAndEnterGame()
    {
        CloseModal();
        Action cb = _onLoginWizardFinished;
        _onLoginWizardFinished = null;
        if (cb != null)
        {
            cb.Invoke();
        }
        else if (LoginManager.Instance != null)
        {
            LoginManager.Instance.ProceedToEnterMainMenuScene();
        }

        MainMenuManager.EnsureInstance()?.RefreshTopLeftPlayerProfileBadge();
        MainMenuManager.EnsureInstance()?.RefreshPvPModeLockState();
    }

    private void OnAuthenticationSuccessProceedToNickname()
    {
        PlayFabClientService.IsOfflineMode = false;
        PlayerPrefs.SetInt("IsOfflineMode", 0);
        PlayerPrefs.Save();

        var pf = PlayFabClientService.EnsureInstance();
        var acc = AccountDatabase.GetActiveAccount();

        string accountId = acc != null ? acc.username : pf.PlayFabId;
        string provider = pf.LoggedInProvider;

        // Nếu là tài khoản Google (hoặc đăng nhập qua Google OAuth): Tự động lấy tên hiển thị và vào thẳng màn hình Bắt Đầu Trò Chơi!
        if (provider == "Google" || !string.IsNullOrEmpty(OAuthAuthenticator.LastGoogleUserName))
        {
            string gName = !string.IsNullOrEmpty(OAuthAuthenticator.LastGoogleUserName)
                ? OAuthAuthenticator.LastGoogleUserName
                : (!string.IsNullOrEmpty(acc?.displayName) ? acc.displayName : (!string.IsNullOrEmpty(accountId) ? accountId : "Hiệp Sĩ"));
            if (acc != null)
            {
                acc.displayName = gName;
                AccountDatabase.SaveOrUpdateCloudAccount(acc);
            }
            PlayerPrefs.SetString("Player_Name", gName);
            PlayerPrefs.SetInt("HasSetNickname_" + accountId, 1);
            PlayerPrefs.Save();

            FinishLoginAndEnterGame();
            return;
        }

        string localSavedName = PlayerPrefs.GetString("Player_Name", "").Trim();
        bool hasSetNicknameFlag = PlayerPrefs.GetInt("HasSetNickname_" + accountId, 0) == 1 ||
                                  (!string.IsNullOrEmpty(localSavedName) && PlayerPrefs.GetInt("HasSetNickname_" + localSavedName, 0) == 1);

        bool hasValidExistingName = (!string.IsNullOrEmpty(acc?.displayName) &&
                                     acc.displayName != "Player" &&
                                     acc.displayName != "Wanderer" &&
                                     acc.displayName != "Chiến Binh" &&
                                     !acc.displayName.StartsWith("Guest_") &&
                                     !acc.displayName.StartsWith("gg") &&
                                     !acc.displayName.StartsWith("gv"))
                                  ||
                                  (!string.IsNullOrEmpty(localSavedName) &&
                                     localSavedName != "Player" &&
                                     localSavedName != "Wanderer" &&
                                     localSavedName != "Chiến Binh" &&
                                     !localSavedName.StartsWith("Guest_") &&
                                     !localSavedName.StartsWith("gg") &&
                                     !localSavedName.StartsWith("gv"));

        // Nếu tài khoản đã có Nickname hoặc đã đặt tên trước đó -> Bỏ qua Step 3, vào thẳng sảnh chính ngay lập tức!
        if (hasSetNicknameFlag || hasValidExistingName)
        {
            if (acc != null && (string.IsNullOrEmpty(acc.displayName) || acc.displayName == "Player" || acc.displayName == "Chiến Binh"))
            {
                if (!string.IsNullOrEmpty(localSavedName))
                {
                    acc.displayName = localSavedName;
                    AccountDatabase.SaveOrUpdateCloudAccount(acc);
                }
            }
            FinishLoginAndEnterGame();
            return;
        }
        
        string currentDisplay = "";
        if (!string.IsNullOrEmpty(OAuthAuthenticator.LastGoogleUserName))
        {
            currentDisplay = OAuthAuthenticator.LastGoogleUserName;
        }
        else if (acc != null && !string.IsNullOrEmpty(acc.displayName) && acc.displayName != "Player" && acc.displayName != "Wanderer")
        {
            currentDisplay = acc.displayName;
        }
        else if (!string.IsNullOrEmpty(accountId) && !accountId.StartsWith("gg") && !accountId.StartsWith("gv"))
        {
            currentDisplay = accountId;
        }
        else
        {
            currentDisplay = "Chiến Binh";
        }

        ShowLoginStep(LoginStep.Step3_SetIngameNickname);

        if (_nicknameLoggedAccountLabel != null)
        {
            _nicknameLoggedAccountLabel.text = LocalizationManager.T(
                $"Tài khoản đã đăng nhập: <color=#FFE066>{accountId}</color> ({provider})",
                $"Logged in account: <color=#FFE066>{accountId}</color> ({provider})");
        }

        if (_nicknameInput != null)
        {
            _nicknameInput.text = currentDisplay;
        }
    }

    private void OnConfirmNicknameClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string newNick = _nicknameInput != null ? _nicknameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(newNick))
        {
            var acc = AccountDatabase.GetActiveAccount();
            newNick = acc != null && !string.IsNullOrEmpty(acc.displayName) ? acc.displayName : "Chiến Binh";
        }

        PlayFabClientService.EnsureInstance().SetIngameDisplayName(newNick, (ok, errMsg) =>
        {
            if (!ok)
            {
                if (_wizStatusText != null)
                {
                    _wizStatusText.text = $"<color=#FF5555>{errMsg}</color>";
                }
                return;
            }
            FinishLoginAndEnterGame();
        });
    }

    // =========================================================================
    // 2. MỤC QUẢN LÝ PROFILE (HỒ SƠ CHIẾN BINH FULL MÀN HÌNH - MOBILE RPG UI)
    // =========================================================================
    private void BuildProfileWindow(Transform root)
    {
        // 1. FULL MÀN HÌNH CHUẨN RPG MOBILE (Trải rộng 100% Canvas, che kín mọi UI phía sau)
        _profileWindow = CreateBox(root, "ProfileModalWindow", Vector2.zero, Vector2.zero, new Color(0.04f, 0.06f, 0.11f, 1f));
        RectTransform profRt = _profileWindow.GetComponent<RectTransform>();
        profRt.anchorMin = Vector2.zero;
        profRt.anchorMax = Vector2.one;
        profRt.offsetMin = Vector2.zero;
        profRt.offsetMax = Vector2.zero;

        // 2. TOP HEADER BAR (Trải dài sát mép trên cùng)
        GameObject header = CreateBox(_profileWindow.transform, "ProfileHeader", Vector2.zero, Vector2.zero, new Color(0.09f, 0.12f, 0.20f, 1f));
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(0f, 68f);
        headerRt.anchoredPosition = Vector2.zero;

        _profHeaderTitleText = CreateText(header.transform,
            LocalizationManager.T("HỒ SƠ CHIẾN BINH", "WARRIOR PROFILE"),
            24f, new Vector2(35f, 0f), new Vector2(500f, 48f), new Color(1f, 0.88f, 0.35f));
        _profHeaderTitleText.alignment = TextAlignmentOptions.Left;
        RectTransform titleRt = _profHeaderTitleText.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(0f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = new Vector2(35f, 0f);

        // Pill Linh Hồn Tàn Tích (Aethel Shards)
        GameObject shardsPill = CreateBox(header.transform, "ShardsPill", Vector2.zero, new Vector2(260f, 42f), new Color(0.04f, 0.06f, 0.11f, 0.95f));
        RectTransform spRt = shardsPill.GetComponent<RectTransform>();
        spRt.anchorMin = new Vector2(1f, 0.5f);
        spRt.anchorMax = new Vector2(1f, 0.5f);
        spRt.pivot = new Vector2(1f, 0.5f);
        spRt.anchoredPosition = new Vector2(-115f, 0f);
        _profShardsText = CreateText(shardsPill.transform, "", 16f, Vector2.zero, new Vector2(250f, 38f), new Color(0.35f, 1f, 0.65f));

        // Nút Đóng [X] góc phải ngoài cùng
        GameObject closeBtn = CreateButton(header.transform, "Btn_CloseProfile", "X",
            Vector2.zero, new Vector2(60f, 46f), new Color(0.72f, 0.16f, 0.20f, 1f), 22f, CloseModal);
        RectTransform cbRt = closeBtn.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1f, 0.5f);
        cbRt.anchorMax = new Vector2(1f, 0.5f);
        cbRt.pivot = new Vector2(1f, 0.5f);
        cbRt.anchoredPosition = new Vector2(-25f, 0f);

        // 3. VERTICAL SIDEBAR (CỘT TRÁI - 3 TABS DỌC PHONG CÁCH RPG, ĐÃ BỎ MỤC XẾP HẠNG)
        GameObject sidebar = CreateBox(_profileWindow.transform, "ProfileSidebar", Vector2.zero, Vector2.zero, new Color(0.07f, 0.09f, 0.15f, 1f));
        RectTransform sideRt = sidebar.GetComponent<RectTransform>();
        sideRt.anchorMin = new Vector2(0f, 0f);
        sideRt.anchorMax = new Vector2(0f, 1f);
        sideRt.pivot = new Vector2(0f, 0.5f);
        sideRt.offsetMin = new Vector2(0f, 40f);
        sideRt.offsetMax = new Vector2(110f, -68f);

        GameObject sideOverviewBtn = CreateButton(sidebar.transform, "Btn_SideOverview", "",
            new Vector2(0f, 140f), new Vector2(96f, 100f), new Color(0.18f, 0.28f, 0.48f, 1f), 15f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                SwitchProfileSubTab(0);
            });
        _profSideOverviewBtnBg = sideOverviewBtn.GetComponent<Image>();
        _profSideOverviewBtnText = sideOverviewBtn.GetComponentInChildren<TextMeshProUGUI>();

        GameObject sideAccountBtn = CreateButton(sidebar.transform, "Btn_SideAccount", "",
            new Vector2(0f, 0f), new Vector2(96f, 100f), new Color(0.12f, 0.16f, 0.26f, 1f), 15f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                SwitchProfileSubTab(1);
            });
        _profSideAccountBtnBg = sideAccountBtn.GetComponent<Image>();
        _profSideAccountBtnText = sideAccountBtn.GetComponentInChildren<TextMeshProUGUI>();

        GameObject sideRoadmapBtn = CreateButton(sidebar.transform, "Btn_SideRoadmap", "",
            new Vector2(0f, -140f), new Vector2(96f, 100f), new Color(0.12f, 0.16f, 0.26f, 1f), 15f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                SwitchProfileSubTab(2);
            });
        _profSideRoadmapBtnBg = sideRoadmapBtn.GetComponent<Image>();
        _profSideRoadmapBtnText = sideRoadmapBtn.GetComponentInChildren<TextMeshProUGUI>();

        // 4. SHOWCASE HERO PEDESTAL (CỘT GIỮA - RỘNG RÃI, BỆ ĐÁ HOÀNG TRÁNG)
        GameObject centerCol = CreateBox(_profileWindow.transform, "ProfileCenterCol", Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.14f, 0.95f));
        RectTransform centerRt = centerCol.GetComponent<RectTransform>();
        centerRt.anchorMin = new Vector2(0f, 0f);
        centerRt.anchorMax = new Vector2(0.48f, 1f);
        centerRt.pivot = new Vector2(0.5f, 0.5f);
        centerRt.offsetMin = new Vector2(120f, 50f);
        centerRt.offsetMax = new Vector2(-10f, -78f);

        // Header Mini: Avatar + Tên + Cấp + EXP Bar
        GameObject heroCard = CreateBox(centerCol.transform, "HeroMiniCard", Vector2.zero, new Vector2(680f, 105f), new Color(0.04f, 0.06f, 0.11f, 0.9f));
        RectTransform cardRt = heroCard.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 1f);
        cardRt.anchorMax = new Vector2(0.5f, 1f);
        cardRt.pivot = new Vector2(0.5f, 1f);
        cardRt.anchoredPosition = new Vector2(0f, -18f);

        GameObject avBorder = CreateBox(heroCard.transform, "AvatarBorder", new Vector2(-260f, 0f), new Vector2(76f, 76f), new Color(0.95f, 0.75f, 0.25f, 1f));
        GameObject avObj = CreateBox(avBorder.transform, "AvatarImg", Vector2.zero, new Vector2(70f, 70f), Color.white);
        _profAvatarImg = avObj.GetComponent<Image>();
        _profAvatarImg.preserveAspect = true;

        GameObject badgeObj = CreateBox(heroCard.transform, "LvBadge", new Vector2(-260f, -32f), new Vector2(56f, 22f), new Color(0.72f, 0.15f, 0.18f, 1f));
        _profLevelText = CreateText(badgeObj.transform, "Lv. 1", 12f, Vector2.zero, new Vector2(54f, 20f), Color.white);

        _profDisplayNameText = CreateText(heroCard.transform, "", 22f, new Vector2(40f, 18f), new Vector2(460f, 30f), new Color(1f, 0.88f, 0.35f));
        _profDisplayNameText.alignment = TextAlignmentOptions.Left;

        // EXP Bar (Rộng 460px thoáng mắt)
        GameObject expBg = CreateBox(heroCard.transform, "ExpBarBg", new Vector2(40f, -16f), new Vector2(460f, 20f), new Color(0.03f, 0.04f, 0.08f, 1f));
        GameObject expFill = CreateBox(expBg.transform, "ExpBarFill", new Vector2(-230f, 0f), new Vector2(100f, 20f), new Color(0.18f, 0.78f, 0.45f, 1f));
        _profExpFillRt = expFill.GetComponent<RectTransform>();
        _profExpFillRt.pivot = new Vector2(0f, 0.5f);
        _profExpFillRt.anchoredPosition = new Vector2(-230f, 0f);
        _profExpText = CreateText(expBg.transform, "", 12f, Vector2.zero, new Vector2(450f, 20f), Color.white);

        // Bệ Đá Ma Thuật Pedestal & Vòng Ma Pháp & Hero Toàn Thân
        GameObject magicAura = CreateBox(centerCol.transform, "MagicAura", new Vector2(0f, -20f), new Vector2(400f, 400f), new Color(0.35f, 0.25f, 0.75f, 0.22f));
        var auraImg = magicAura.GetComponent<Image>();
        if (auraImg != null) auraImg.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();

        GameObject pedestal = CreateBox(centerCol.transform, "Pedestal", new Vector2(0f, -160f), new Vector2(480f, 90f), new Color(0.20f, 0.65f, 0.95f, 0.45f));
        var pedImg = pedestal.GetComponent<Image>();
        if (pedImg != null) pedImg.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();

        // Mô hình Hero toàn thân to, rõ, ấn tượng
        GameObject heroFullObj = CreateBox(centerCol.transform, "HeroFullBody", new Vector2(0f, 10f), new Vector2(360f, 360f), Color.white);
        _profHeroFullBodyImg = heroFullObj.GetComponent<Image>();
        _profHeroFullBodyImg.preserveAspect = true;

        // Badge Tên Tướng Đang Chọn
        GameObject heroBadgeObj = CreateBox(centerCol.transform, "HeroBadgeBox", new Vector2(0f, -250f), new Vector2(560f, 50f), new Color(0.12f, 0.09f, 0.03f, 0.95f));
        _profHeroBadgeText = CreateText(heroBadgeObj.transform, "", 18f, Vector2.zero, new Vector2(540f, 44f), new Color(1f, 0.85f, 0.25f));
        _profHeroBadgeText.fontStyle = FontStyles.Bold;

        // Nút Đổi Tướng
        GameObject changeHeroBtn = CreateButton(centerCol.transform, "Btn_ChangeHeroSkin", "",
            new Vector2(0f, -320f), new Vector2(560f, 56f), new Color(0.35f, 0.20f, 0.58f, 1f), 18f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                CloseModal();
                if (MainMenuManager.Instance != null)
                {
                    MainMenuManager.Instance.OpenHeroSelectForProfile();
                }
            });
        _profChangeHeroBtnText = changeHeroBtn.GetComponentInChildren<TextMeshProUGUI>();

        // 5. RIGHT PANEL (CỘT PHẢI - THỐNG KÊ CHI TIẾT & BẢO MẬT & LỘ TRÌNH RỘNG RÃI)
        GameObject rightCol = CreateBox(_profileWindow.transform, "ProfileRightCol", Vector2.zero, Vector2.zero, new Color(0.06f, 0.08f, 0.14f, 0.95f));
        RectTransform rightRt = rightCol.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.48f, 0f);
        rightRt.anchorMax = new Vector2(1f, 1f);
        rightRt.pivot = new Vector2(0.5f, 0.5f);
        rightRt.offsetMin = new Vector2(10f, 50f);
        rightRt.offsetMax = new Vector2(-20f, -78f);

        // CONTAINER 0: TỔNG QUAN CHIẾN TÍCH THỰC TẾ (Chiếm trọn cột phải, điều khiển trực tiếp qua Sidebar)
        _profOverviewSubPanel = CreateBox(rightCol.transform, "OverviewSubPanel", Vector2.zero, Vector2.zero, Color.clear);
        RectTransform ospRt = _profOverviewSubPanel.GetComponent<RectTransform>();
        ospRt.anchorMin = Vector2.zero;
        ospRt.anchorMax = Vector2.one;
        ospRt.offsetMin = Vector2.zero;
        ospRt.offsetMax = Vector2.zero;

        // Card 1: THỐNG KÊ CHIẾN TRƯỜNG & TÀI SẢN (4 Ô Chỉ Số Lớn, Thoáng Đãng)
        GameObject statsCard = CreateBox(_profOverviewSubPanel.transform, "StatsCard", new Vector2(0f, 175f), new Vector2(840f, 270f), new Color(0.04f, 0.06f, 0.11f, 0.95f));
        _profStatsHeaderTitle = CreateText(statsCard.transform, "", 18f, new Vector2(-200f, 105f), new Vector2(400f, 32f), new Color(1f, 0.85f, 0.35f));
        _profStatsHeaderTitle.alignment = TextAlignmentOptions.Left;

        // 4 ô lớn (2x2): Tổng trận, Thắng, Tỷ lệ %, Số tướng đã mở
        _profTotalMatchesText = CreateStatCellBig(statsCard.transform, out _profLblTotal, new Vector2(-210f, 32f), Color.white);
        _profWinsText = CreateStatCellBig(statsCard.transform, out _profLblWins, new Vector2(210f, 32f), new Color(0.4f, 1f, 0.6f));
        _profWinRateText = CreateStatCellBig(statsCard.transform, out _profLblRate, new Vector2(-210f, -56f), new Color(1f, 0.9f, 0.4f));
        _profHeroesCountText = CreateStatCellBig(statsCard.transform, out _profLblHeroes, new Vector2(210f, -56f), new Color(0.7f, 0.85f, 1f));

        // Card 2: TÌNH TRẠNG KẾT NỐI MÁY CHỦ
        GameObject connCard = CreateBox(_profOverviewSubPanel.transform, "ConnCard", new Vector2(0f, -80f), new Vector2(840f, 190f), new Color(0.04f, 0.06f, 0.11f, 0.95f));
        _profConnHeaderTitle = CreateText(connCard.transform, "", 18f, new Vector2(-200f, 65f), new Vector2(400f, 30f), new Color(1f, 0.85f, 0.35f));
        _profConnHeaderTitle.alignment = TextAlignmentOptions.Left;
        _profStatsText = CreateText(connCard.transform, "", 16f, new Vector2(0f, -15f), new Vector2(780f, 105f), new Color(0.85f, 0.92f, 1f));
        _profStatsText.alignment = TextAlignmentOptions.Left;

        // Bottom Action Button: Đăng Xuất (To, rõ, đặt chính giữa)
        GameObject logoutBtn = CreateButton(_profOverviewSubPanel.transform, "Btn_ProfLogoutSwitchBig", "",
            new Vector2(0f, -250f), new Vector2(640f, 62f), new Color(0.62f, 0.18f, 0.20f, 1f), 19f, OnLogoutAndSwitchAccountClicked);
        _profLogoutBtnText = logoutBtn.GetComponentInChildren<TextMeshProUGUI>();

        // CONTAINER 1: TÀI KHOẢN & BẢO MẬT & ĐỔI TÊN
        _profAccountSubPanel = CreateBox(rightCol.transform, "AccountSubPanel", Vector2.zero, Vector2.zero, Color.clear);
        RectTransform aspRt = _profAccountSubPanel.GetComponent<RectTransform>();
        aspRt.anchorMin = Vector2.zero;
        aspRt.anchorMax = Vector2.one;
        aspRt.offsetMin = Vector2.zero;
        aspRt.offsetMax = Vector2.zero;
        _profAccountSubPanel.SetActive(false);

        // Card Đổi Tên Hiển Thị Trong Game
        GameObject renameCard = CreateBox(_profAccountSubPanel.transform, "RenameCard", new Vector2(0f, 200f), new Vector2(840f, 155f), new Color(0.04f, 0.06f, 0.11f, 0.95f));
        _profRenameTitleText = CreateText(renameCard.transform, "", 18f, new Vector2(-150f, 48f), new Vector2(500f, 30f), new Color(1f, 0.85f, 0.35f));
        _profRenameTitleText.alignment = TextAlignmentOptions.Left;

        _profRenameInput = CreateSafeInputField(renameCard.transform, "ProfileRenameInput",
            new Vector2(-120f, -14f), new Vector2(480f, 52f), "", false);

        GameObject saveNameBtn = CreateButton(renameCard.transform, "Btn_SaveNewNickname", "",
            new Vector2(250f, -14f), new Vector2(200f, 52f),
            new Color(0.18f, 0.62f, 0.36f, 1f), 18f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                string newName = _profRenameInput != null ? _profRenameInput.text.Trim() : "";
                PlayFabClientService.EnsureInstance().SetIngameDisplayName(newName, (ok, errMsg) =>
                {
                    if (ok)
                    {
                        RefreshProfileModal();
                        MainMenuManager.EnsureInstance()?.RefreshTopLeftPlayerProfileBadge();
                    }
                    else if (_profStatusText != null)
                    {
                        _profStatusText.text = $"<color=#FF5555>{errMsg}</color>";
                    }
                });
            });
        _profSaveNameBtnText = saveNameBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Card Thông Tin Tài Khoản Máy Chủ
        GameObject accInfoCard = CreateBox(_profAccountSubPanel.transform, "AccInfoCard", new Vector2(0f, 60f), new Vector2(840f, 220f), new Color(0.04f, 0.06f, 0.11f, 0.95f));
        _profAccountInfoText = CreateText(accInfoCard.transform, "", 18f, Vector2.zero, new Vector2(780f, 180f), new Color(0.78f, 0.88f, 1f));
        _profAccountInfoText.alignment = TextAlignmentOptions.Left;

        // Bottom Action Button in Tab Account (Đăng Xuất / Đổi Tài Khoản ở vị trí trung tâm nổi bật)
        GameObject accLogoutBtn = CreateButton(_profAccountSubPanel.transform, "Btn_ProfAccountLogout", "",
            new Vector2(0f, -120f), new Vector2(560f, 66f),
            new Color(0.65f, 0.18f, 0.20f, 1f), 20f, OnLogoutAndSwitchAccountClicked);
        _profAccountLogoutBtnText = accLogoutBtn.GetComponentInChildren<TextMeshProUGUI>();

        // CONTAINER 2: LỘ TRÌNH PHÁT TRIỂN & GỢI Ý TÍNH NĂNG
        _profRoadmapSubPanel = CreateBox(rightCol.transform, "RoadmapSubPanel", Vector2.zero, Vector2.zero, Color.clear);
        RectTransform rspRt = _profRoadmapSubPanel.GetComponent<RectTransform>();
        rspRt.anchorMin = Vector2.zero;
        rspRt.anchorMax = Vector2.one;
        rspRt.offsetMin = Vector2.zero;
        rspRt.offsetMax = Vector2.zero;
        _profRoadmapSubPanel.SetActive(false);

        GameObject roadmapCard = CreateBox(_profRoadmapSubPanel.transform, "RoadmapCard", Vector2.zero, new Vector2(840f, 670f), new Color(0.04f, 0.06f, 0.11f, 0.95f));
        _profRoadmapHeaderTitle = CreateText(roadmapCard.transform, "", 20f, new Vector2(0f, 300f), new Vector2(780f, 38f), new Color(1f, 0.85f, 0.35f));
        _profRoadmapContentText = CreateText(roadmapCard.transform, "", 16f, new Vector2(0f, -20f), new Vector2(780f, 570f), Color.white);
        _profRoadmapContentText.alignment = TextAlignmentOptions.TopLeft;

        // 6. FOOTER (Sát mép đáy màn hình)
        GameObject footer = CreateBox(_profileWindow.transform, "ProfileFooter", Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.09f, 0.98f));
        RectTransform footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.sizeDelta = new Vector2(0f, 40f);
        footerRt.anchoredPosition = Vector2.zero;
        _profStatusText = CreateText(footer.transform, "", 14f, Vector2.zero, new Vector2(1400f, 32f), new Color(0.45f, 0.92f, 1f));
    }

    private TextMeshProUGUI CreateStatCellBig(Transform parent, out TextMeshProUGUI labelTxt, Vector2 pos, Color valCol)
    {
        GameObject cell = CreateBox(parent, "StatCellBig", pos, new Vector2(380f, 72f), new Color(0.08f, 0.11f, 0.18f, 0.95f));
        labelTxt = CreateText(cell.transform, "", 13.5f, new Vector2(0f, 15f), new Vector2(360f, 22f), new Color(0.65f, 0.75f, 0.9f));
        TextMeshProUGUI valTxt = CreateText(cell.transform, "", 22f, new Vector2(0f, -14f), new Vector2(360f, 30f), valCol);
        valTxt.fontStyle = FontStyles.Bold;
        return valTxt;
    }

    private void SwitchProfileSubTab(int tabIndex)
    {
        if (_profOverviewSubPanel != null) _profOverviewSubPanel.SetActive(tabIndex == 0);
        if (_profAccountSubPanel != null) _profAccountSubPanel.SetActive(tabIndex == 1);
        if (_profRoadmapSubPanel != null) _profRoadmapSubPanel.SetActive(tabIndex == 2);

        Color activeColor = new Color(0.20f, 0.35f, 0.65f, 1f);
        Color inactiveColor = new Color(0.12f, 0.16f, 0.26f, 1f);

        if (_profSideOverviewBtnBg != null) _profSideOverviewBtnBg.color = (tabIndex == 0) ? activeColor : inactiveColor;
        if (_profSideAccountBtnBg != null) _profSideAccountBtnBg.color = (tabIndex == 1) ? activeColor : inactiveColor;
        if (_profSideRoadmapBtnBg != null) _profSideRoadmapBtnBg.color = (tabIndex == 2) ? activeColor : inactiveColor;
    }

    private Sprite GetHeroFullBodySprite(string heroId)
    {
        string norm = CharacterManager.NormalizeHeroId(heroId);
        try
        {
            if (norm == CharacterManager.CHAR_ABYSSAL_MECH)
            {
                var frames = AbyssalMechSkill.GetIdleFrames();
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            else if (norm == CharacterManager.CHAR_CELESTIAL_MAGE)
            {
                var frames = CelestialMageSkill.GetIdleFrames();
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            else if (norm == CharacterManager.CHAR_DUNE_ASSASSIN)
            {
                var frames = DuneAssassinSkill.GetIdleFrames();
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            else if (norm == CharacterManager.CHAR_TITAN_BERSERKER)
            {
                var frames = Resources.LoadAll<Sprite>("Player4/Sprites/Hammer_Idle");
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            else if (norm == CharacterManager.CHAR_AZURE_BLADE)
            {
                var frames = Resources.LoadAll<Sprite>("Player3/Sprites/Sword_Idle");
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            else if (norm == CharacterManager.CHAR_GALE_RONIN)
            {
                var frames = Resources.LoadAll<Sprite>("Player2/Sprites/_down idle");
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            else
            {
                var frames = Resources.LoadAll<Sprite>("Player1/Sprites/idle_down");
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
        }
        catch { }

        return PvPRemoteOpponent.GetHeroPortraitSprite(norm);
    }

    public void RefreshProfileModal()
    {
        var acc = AccountDatabase.GetActiveAccount();
        var pf = PlayFabClientService.EnsureInstance();
        bool isOffline = PlayFabClientService.IsOfflineMode;

        string displayName = acc != null && !string.IsNullOrEmpty(acc.displayName) ? acc.displayName : "Chiến Binh";
        if (displayName == "Player" || displayName == "Wanderer")
        {
            string savedName = PlayerPrefs.GetString("Player_Name", "");
            if (!string.IsNullOrEmpty(savedName) && savedName != "Player" && savedName != "Wanderer")
            {
                displayName = savedName;
                if (acc != null) acc.displayName = savedName;
            }
            else
            {
                displayName = isOffline ? LocalizationManager.T("Hiệp Sĩ Offline", "Offline Knight") : LocalizationManager.T("Chiến Binh", "Warrior");
            }
        }

        string username = acc != null ? acc.username : (isOffline ? LocalizationManager.T("Chế độ Offline", "Offline Mode") : "Local");
        long shards = AccountDatabase.GetAethelShards();
        int wins = acc != null ? acc.pvpWins : 0;
        int losses = acc != null ? acc.pvpLosses : 0;
        int totalMatches = wins + losses;

        string selectedHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        string normHero = CharacterManager.NormalizeHeroId(selectedHero);
        var charData = CharacterManager.EnsureInstance().GetCharacter(normHero);

        string heroName = charData != null ? LocalizationManager.T(charData.nameVN, charData.nameEN) : normHero;

        int heroLv = AccountDatabase.GetHeroLevel(normHero);
        int currentExp = AccountDatabase.GetHeroExp(normHero);
        int reqExp = AccountDatabase.GetExpRequiredForNextLevel(heroLv);

        // Đếm số tướng thật sự đã mở khóa trong hệ thống
        string[] allHeroIds = new string[] {
            CharacterManager.CHAR_VAELSTRIDER,
            CharacterManager.CHAR_GALE_RONIN,
            CharacterManager.CHAR_AZURE_BLADE,
            CharacterManager.CHAR_TITAN_BERSERKER,
            CharacterManager.CHAR_DUNE_ASSASSIN,
            CharacterManager.CHAR_CELESTIAL_MAGE,
            CharacterManager.CHAR_ABYSSAL_MECH
        };
        int unlockedHeroCount = 0;
        for (int i = 0; i < allHeroIds.Length; i++)
        {
            if (CharacterManager.EnsureInstance().IsCharacterUnlocked(allHeroIds[i]))
            {
                unlockedHeroCount++;
            }
        }

        // Cập nhật các Text Header & Sidebar theo ngôn ngữ hiện tại (3 tabs, không còn nút xếp hạng)
        if (_profHeaderTitleText != null) _profHeaderTitleText.text = LocalizationManager.T("HỒ SƠ CHIẾN BINH", "WARRIOR PROFILE");
        if (_profSideOverviewBtnText != null) _profSideOverviewBtnText.text = LocalizationManager.T("TỔNG\nQUAN", "OVER\nVIEW");
        if (_profSideAccountBtnText != null) _profSideAccountBtnText.text = LocalizationManager.T("TÀI\nKHOẢN", "ACC\nOUNT");
        if (_profSideRoadmapBtnText != null) _profSideRoadmapBtnText.text = LocalizationManager.T("LỘ\nTRÌNH", "ROAD\nMAP");

        // 1. Shards Pill
        if (_profShardsText != null)
        {
            _profShardsText.text = LocalizationManager.T($"Linh Hồn: {shards:N0}", $"Shards: {shards:N0}");
        }

        // 2. Avatar & Level & Name (Cột giữa)
        if (_profAvatarImg != null)
        {
            Sprite customAv = OAuthAuthenticator.GetSavedAvatarSprite();
            if (customAv != null)
            {
                _profAvatarImg.sprite = customAv;
            }
            else
            {
                Sprite sp = PvPRemoteOpponent.GetHeroPortraitSprite(normHero);
                if (sp != null) _profAvatarImg.sprite = sp;
            }
        }

        if (_profLevelText != null)
        {
            _profLevelText.text = $"Lv. {heroLv}";
        }

        if (_profDisplayNameText != null)
        {
            _profDisplayNameText.text = displayName;
        }

        // 3. EXP Bar Tích Lũy Thật 100% Của Tướng Hiện Tại
        float expRatio = Mathf.Clamp01((float)currentExp / (float)reqExp);
        if (_profExpFillRt != null)
        {
            _profExpFillRt.sizeDelta = new Vector2(460f * expRatio, 20f);
        }
        if (_profExpText != null)
        {
            _profExpText.text = $"{currentExp:N0} / {reqExp:N0} EXP";
        }

        // 4. Full Body Hero Sprite (Đứng trên bệ đá thoáng, không bị đè chữ)
        if (_profHeroFullBodyImg != null)
        {
            Sprite bodySp = GetHeroFullBodySprite(normHero);
            if (bodySp != null) _profHeroFullBodyImg.sprite = bodySp;
        }

        // 5. Hero Badge & Change Button
        if (_profHeroBadgeText != null)
        {
            _profHeroBadgeText.text = LocalizationManager.T($"TƯỚNG HIỆN TẠI: {heroName.ToUpper()}", $"CURRENT HERO: {heroName.ToUpper()}");
        }
        if (_profChangeHeroBtnText != null)
        {
            _profChangeHeroBtnText.text = LocalizationManager.T("CHỌN TƯỚNG KHÁC", "SELECT OTHER HERO");
        }

        // 6. Stats Tab (100% Số Liệu Thật, Đã Bỏ Mục Xếp Hạng Theo Yêu Cầu)
        if (_profStatsHeaderTitle != null) _profStatsHeaderTitle.text = LocalizationManager.T("THỐNG KÊ CHIẾN TRƯỜNG & TÀI SẢN", "BATTLE STATS & ASSETS");
        if (_profLblTotal != null) _profLblTotal.text = LocalizationManager.T("TỔNG SỐ TRẬN", "TOTAL MATCHES");
        if (_profLblWins != null) _profLblWins.text = LocalizationManager.T("CHIẾN THẮNG", "VICTORIES");
        if (_profLblRate != null) _profLblRate.text = LocalizationManager.T("TỶ LỆ THẮNG", "WIN RATE");
        if (_profLblHeroes != null) _profLblHeroes.text = LocalizationManager.T("TƯỚNG ĐÃ MỞ", "HEROES UNLOCKED");

        if (_profTotalMatchesText != null) _profTotalMatchesText.text = $"{totalMatches}";
        if (_profWinsText != null) _profWinsText.text = $"{wins}";
        if (_profWinRateText != null)
        {
            float rate = (totalMatches > 0) ? ((float)wins / totalMatches * 100f) : 0f;
            _profWinRateText.text = $"{rate:F1}%";
        }
        if (_profHeroesCountText != null) _profHeroesCountText.text = $"{unlockedHeroCount} / {allHeroIds.Length}";

        // Connection Card
        if (_profConnHeaderTitle != null) _profConnHeaderTitle.text = LocalizationManager.T("TÌNH TRẠNG KẾT NỐI MÁY CHỦ", "SERVER CONNECTION STATUS");
        if (_profStatsText != null)
        {
            string provider = pf.IsLoggedIn ? pf.LoggedInProvider : (isOffline ? LocalizationManager.T("Ngoại tuyến", "Offline") : LocalizationManager.T("Tài khoản Cục bộ", "Local Account"));
            string syncStatus = pf.IsLoggedIn ? LocalizationManager.T("Đã đồng bộ an toàn", "Synchronized Safely") : LocalizationManager.T("Chưa kết nối đám mây", "Cloud Disconnected");
            _profStatsText.text =
                $"• {LocalizationManager.T("Cổng dịch vụ", "Service Gateway")}: <color=#FFE066>{provider}</color>\n" +
                $"• {LocalizationManager.T("Trạng thái", "Status")}: <color=#44FF88>{syncStatus}</color>\n" +
                $"• {LocalizationManager.T("Máy chủ đám mây", "Cloud Server")}: <color=#66DDFF>PlayFab Cloud Server</color>";
        }

        if (_profLogoutBtnText != null) _profLogoutBtnText.text = LocalizationManager.T("ĐĂNG XUẤT / ĐỔI TÀI KHOẢN", "LOGOUT / SWITCH ACCOUNT");

        // 7. Account Tab
        if (_profRenameTitleText != null) _profRenameTitleText.text = LocalizationManager.T("ĐỔI TÊN HIỂN THỊ TRONG GAME:", "CHANGE IN-GAME DISPLAY NAME:");
        if (_profSaveNameBtnText != null) _profSaveNameBtnText.text = LocalizationManager.T("LƯU TÊN", "SAVE NAME");
        if (_profRenameInput != null)
        {
            _profRenameInput.text = displayName;
        }

        if (_profAccountInfoText != null)
        {
            if (isOffline)
            {
                _profAccountInfoText.text =
                    $"• {LocalizationManager.T("Tài khoản", "Account")}: <color=#FFE066>{displayName}</color>\n" +
                    $"• {LocalizationManager.T("Cổng liên kết", "Login Gateway")}: <color=#FFAA55>{LocalizationManager.T("Chế độ Offline", "Offline Mode")}</color>\n" +
                    $"• {LocalizationManager.T("Đấu Trường PvP", "PvP Arena")}: <color=#FF6666>{LocalizationManager.T("Đang Khóa (Cần Kết Nối Mạng)", "Locked (Requires Internet)")}</color>";
            }
            else
            {
                string provider = pf.IsLoggedIn ? pf.LoggedInProvider : LocalizationManager.T("Tài khoản Cục bộ", "Local Account");
                string pfId = pf.IsLoggedIn ? pf.PlayFabId : LocalizationManager.T("Chưa kết nối máy chủ", "Not connected to server");
                _profAccountInfoText.text =
                    $"• {LocalizationManager.T("Tài khoản", "Account")}: <color=#FFE066>{username}</color>\n" +
                    $"• {LocalizationManager.T("Cổng dịch vụ", "Service Gateway")}: <color=#66DDFF>{provider}</color>\n" +
                    $"• {LocalizationManager.T("ID Máy Chủ", "Server PlayFab ID")}: <color=#AADDFF>{pfId}</color>";
            }
        }

        if (_profLinkTitleText != null) _profLinkTitleText.text = LocalizationManager.T("LIÊN KẾT BẢO VỆ TÀI KHOẢN VĨNH VIỄN", "LINK & PROTECT PERMANENT ACCOUNT");
        if (_profLinkPermBtnText != null) _profLinkPermBtnText.text = LocalizationManager.T("LIÊN KẾT TÀI KHOẢN MỚI", "LINK NEW ACCOUNT");
        if (_profLinkGgBtnText != null) _profLinkGgBtnText.text = LocalizationManager.T("LIÊN KẾT GOOGLE", "LINK GOOGLE");
        if (_profAccountLogoutBtnText != null) _profAccountLogoutBtnText.text = LocalizationManager.T("ĐĂNG XUẤT / ĐỔI TÀI KHOẢN", "LOGOUT / SWITCH ACCOUNT");

        // 8. Roadmap Tab (Lộ Trình & Gợi Ý Tính Năng Phát Triển - Tuyệt đối không dùng ngoặc vuông để tránh lỗi chữ z của font)
        if (_profRoadmapHeaderTitle != null) _profRoadmapHeaderTitle.text = LocalizationManager.T("LỘ TRÌNH & GỢI Ý TÍNH NĂNG PHÁT TRIỂN", "ROADMAP & FEATURE SUGGESTIONS");
        if (_profRoadmapContentText != null)
        {
            _profRoadmapContentText.text = LocalizationManager.T(
                "<color=#FFE066>1. SẮP RA MẮT: LỊCH SỬ ĐẤU TRƯỜNG PVP CHI TIẾT</color>\n" +
                "Ghi nhận nhật ký 20 trận đối đầu gần nhất (tên đối thủ, tướng dùng, thời gian và K/D/A).\n\n" +
                "<color=#FFE066>2. GỢI Ý PHÁT TRIỂN: HỆ THỐNG BANG HỘI & QUÂN ĐOÀN</color>\n" +
                "Thành lập Guild, bảng xếp hạng liên minh bang hội và cùng đồng đội khiêu chiến Boss Thế Giới.\n\n" +
                "<color=#FFE066>3. GỢI Ý PHÁT TRIỂN: NHIỆM VỤ THÀNH TỰU & DANH HIỆU</color>\n" +
                "Cây nhiệm vụ thành tích phong phú mở khóa danh hiệu độc quyền hiển thị trên đầu nhân vật.\n\n" +
                "<color=#FFE066>4. GỢI Ý PHÁT TRIỂN: PHÙ HIỆU & BẢNG BỔ TRỢ TƯỚNG</color>\n" +
                "Hệ thống ngọc bổ trợ đa dạng giúp tùy biến phong cách chiến đấu chuyên sâu cho từng tướng.",

                "<color=#FFE066>1. COMING SOON: DETAILED PVP MATCH HISTORY</color>\n" +
                "Logs the last 20 PvP matches (opponent name, hero played, match time, and K/D/A).\n\n" +
                "<color=#FFE066>2. ROADMAP SUGGESTION: GUILD & CLAN SYSTEM</color>\n" +
                "Establish Guilds, compete on Alliance Leaderboards, and raid Ancient World Bosses together.\n\n" +
                "<color=#FFE066>3. ROADMAP SUGGESTION: ACHIEVEMENTS & EXCLUSIVE TITLES</color>\n" +
                "Rich achievement quest trees unlocking prestigious overhead player titles and bonus Shards.\n\n" +
                "<color=#FFE066>4. ROADMAP SUGGESTION: RUNES & HERO EMBLEMS</color>\n" +
                "Deep customizable emblem pages allowing personalized combat builds for every hero."
            );
        }

        // Footer
        if (_profStatusText != null)
        {
            _profStatusText.text = LocalizationManager.T("Hồ sơ được đồng bộ hóa và lưu trữ an toàn trên đám mây PlayFab", "Profile is securely synchronized with PlayFab Cloud");
        }

        SwitchProfileSubTab(0);
        GameFontManager.ApplyFontsGlobally(_profileWindow);

        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.RefreshCurrencies();
        }
    }

    private void OnLogoutAndSwitchAccountClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        PlayFabClientService.EnsureInstance().LogoutSession();
        AccountDatabase.Logout();
        CloseModal();

        string curScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(curScene, "LoginScene", StringComparison.OrdinalIgnoreCase))
        {
            // Thoát khỏi sảnh chính, chuyển cảnh về Màn hình Đăng nhập LoginScene chuẩn chỉ
            RealmTransitionManager.LoadSceneWithTransition("LoginScene", RealmTransitionManager.TransitionTheme.LoginIntro);
        }
        else
        {
            // Nếu đang sẵn ở LoginScene, mở lại bảng Đăng nhập / Chọn phương thức
            ActivateModalMode(ModalMode.LoginWizard);
            ShowLoginStep(LoginStep.Step1_ChooseMethodHub);
            _onLoginWizardFinished = () =>
            {
                if (MainMenuManager.Instance != null)
                {
                    MainMenuManager.Instance.RefreshCurrencies();
                    MainMenuManager.Instance.RefreshAllTexts();
                }
            };
        }
    }

    // =========================================================================
    // 3. MỤC RANK / BẢNG XẾP HẠNG ĐẤU TRƯỜNG (TÁCH RIÊNG TRONG SẢNH CHÍNH)
    // =========================================================================
    private void BuildRankLeaderboardWindow(Transform root)
    {
        _rankLeaderboardWindow = CreateBox(root, "RankLeaderboardWindow", Vector2.zero, new Vector2(1180f, 720f), new Color(0.07f, 0.09f, 0.16f, 0.99f));

        // Header
        GameObject header = CreateBox(_rankLeaderboardWindow.transform, "RankHeader", new Vector2(0f, 312f), new Vector2(1140f, 68f), new Color(0.11f, 0.14f, 0.24f, 1f));
        CreateText(header.transform,
            LocalizationManager.T("BẢNG XẾP HẠNG RANK ĐẤU TRƯỜNG TOÀN SERVER", "GLOBAL ARENA RANK LEADERBOARD"),
            26f, new Vector2(-60f, 0f), new Vector2(920f, 48f), new Color(1f, 0.88f, 0.35f));

        CreateButton(header.transform, "Btn_CloseRank", LocalizationManager.T("ĐÓNG", "CLOSE"),
            new Vector2(480f, 0f), new Vector2(140f, 48f), new Color(0.58f, 0.16f, 0.18f, 1f), 19f, CloseModal);

        // Thanh thông tin Rank của bản thân
        GameObject myRankBar = CreateBox(_rankLeaderboardWindow.transform, "MyRankSummaryBar", new Vector2(0f, 240f), new Vector2(1140f, 58f), new Color(0.04f, 0.06f, 0.11f, 1f));
        _rankMySummaryText = CreateText(myRankBar.transform, "", 20f, new Vector2(-150f, 0f), new Vector2(800f, 44f), new Color(1f, 0.90f, 0.38f));

        CreateButton(myRankBar.transform, "Btn_RefreshRankBoard",
            LocalizationManager.T("LÀM MỚI BXH", "REFRESH"),
            new Vector2(430f, 0f), new Vector2(240f, 44f),
            new Color(0.20f, 0.52f, 0.84f, 1f), 18f, RefreshRankLeaderboard);

        // Bảng danh sách Cao Thủ
        GameObject tableBox = CreateBox(_rankLeaderboardWindow.transform, "RankTableBox", new Vector2(0f, -18f), new Vector2(1140f, 430f), new Color(0.04f, 0.05f, 0.09f, 1f));
        _rankRowsContainer = tableBox.transform;

        GameObject headerRow = CreateBox(tableBox.transform, "RankHeaderRow", new Vector2(0f, 186f), new Vector2(1110f, 44f), new Color(0.14f, 0.18f, 0.30f, 1f));
        CreateText(headerRow.transform, LocalizationManager.T("HẠNG", "RANK"), 19f, new Vector2(-460f, 0f), new Vector2(120f, 36f), new Color(1f, 0.86f, 0.32f));
        CreateText(headerRow.transform, LocalizationManager.T("TÊN HIỂN THỊ TRONG GAME", "IN-GAME DISPLAY NAME"), 19f, new Vector2(-130f, 0f), new Vector2(460f, 36f), new Color(1f, 0.86f, 0.32f));
        CreateText(headerRow.transform, LocalizationManager.T("BẬC RANK", "TIER"), 19f, new Vector2(210f, 0f), new Vector2(200f, 36f), new Color(1f, 0.86f, 0.32f));
        CreateText(headerRow.transform, LocalizationManager.T("ĐIỂM ELO", "ELO RATING"), 19f, new Vector2(430f, 0f), new Vector2(200f, 36f), new Color(1f, 0.86f, 0.32f));

        // Footer
        GameObject footer = CreateBox(_rankLeaderboardWindow.transform, "RankFooter", new Vector2(0f, -312f), new Vector2(1140f, 52f), new Color(0.04f, 0.05f, 0.09f, 1f));
        _rankStatusText = CreateText(footer.transform,
            LocalizationManager.T("Đang cập nhật Bảng Xếp Hạng Đấu Trường...", "Updating Arena Leaderboard..."),
            17f, Vector2.zero, new Vector2(1080f, 40f), new Color(0.45f, 0.92f, 1f));
    }

    public void RefreshRankLeaderboard()
    {
        var acc = AccountDatabase.GetActiveAccount();
        string myName = acc != null && !string.IsNullOrEmpty(acc.displayName) ? acc.displayName : "Chiến Binh";
        int rating = acc != null && acc.pvpRating > 0 ? acc.pvpRating : 1000;
        int wins = acc != null ? acc.pvpWins : 0;
        string tier = GetRankTierName(rating);

        if (_rankMySummaryText != null)
        {
            _rankMySummaryText.text = LocalizationManager.T(
                $"CHIẾN BINH: <color=#FFFFFF>{myName}</color>   |   BẬC RANK: <color=#FFE066>{tier}</color>   |   ĐIỂM: <color=#44FF88>{rating} ELO</color> ({wins} THẮNG)",
                $"FIGHTER: <color=#FFFFFF>{myName}</color>   |   RANK: <color=#FFE066>{tier}</color>   |   RATING: <color=#44FF88>{rating} ELO</color> ({wins} WINS)");
        }

        PlayFabClientService.EnsureInstance().FetchGlobalPvPLeaderboard(10);
    }

    private void HandleLeaderboardLoaded(List<PlayFabClientService.LeaderboardEntry> entries)
    {
        foreach (var row in _spawnedRankRows)
        {
            if (row != null) Destroy(row);
        }
        _spawnedRankRows.Clear();

        if (_rankRowsContainer == null || entries == null) return;

        int count = Mathf.Min(entries.Count, 8);
        for (int i = 0; i < count; i++)
        {
            var e = entries[i];
            float y = 136f - i * 44f;
            Color bgCol = (i % 2 == 0) ? new Color(0.09f, 0.11f, 0.18f, 0.95f) : new Color(0.07f, 0.08f, 0.14f, 0.95f);
            if (e.Position == 1) bgCol = new Color(0.32f, 0.26f, 0.08f, 0.95f);

            GameObject rowObj = CreateBox(_rankRowsContainer, "RankRow_" + i, new Vector2(0f, y), new Vector2(1110f, 38f), bgCol);
            _spawnedRankRows.Add(rowObj);

            Color txtCol = (e.Position <= 3) ? new Color(1f, 0.88f, 0.32f) : Color.white;
            string tier = GetRankTierName(e.StatValue);

            CreateText(rowObj.transform, "#" + e.Position, 19f, new Vector2(-460f, 0f), new Vector2(120f, 34f), txtCol);
            CreateText(rowObj.transform, SanitizeNoBrackets(e.DisplayName), 19f, new Vector2(-130f, 0f), new Vector2(460f, 34f), txtCol);
            CreateText(rowObj.transform, tier, 18f, new Vector2(210f, 0f), new Vector2(200f, 34f), new Color(0.65f, 0.88f, 1f));
            CreateText(rowObj.transform, e.StatValue + " ELO", 19f, new Vector2(430f, 0f), new Vector2(200f, 34f), new Color(0.45f, 1f, 0.68f));
        }

        GameFontManager.ApplyFontsGlobally(_rankRowsContainer.gameObject);
    }

    // =========================================================================
    // CÁC HÀM TIỆN ÍCH UI CHUẨN ZERO-TOFU & RECTMASK2D (KHÔNG DÙNG NGOẶC VUÔNG)
    // =========================================================================
    private void TogglePasswordInput(TMP_InputField field, bool visible, TextMeshProUGUI btnLabel)
    {
        if (field == null) return;
        field.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        field.inputType = visible ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;
        field.ForceLabelUpdate();
        string cur = field.text;
        field.text = "";
        field.text = cur;
        if (btnLabel != null)
        {
            btnLabel.text = visible ? LocalizationManager.T("ẨN", "HIDE") : LocalizationManager.T("HIỆN", "SHOW");
            btnLabel.color = visible ? new Color(1f, 0.90f, 0.35f) : Color.white;
        }
    }

    private GameObject CreateBox(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject box = new GameObject(name, typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        box.GetComponent<Image>().color = bgColor;
        return box;
    }

    private TMP_InputField CreateSafeInputField(Transform parent, string name, Vector2 pos, Vector2 size, string placeholderText, bool isPassword)
    {
        GameObject inputObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(parent, false);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        inputObj.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 1f);

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(14f, 6f);
        taRt.offsetMax = new Vector2(-14f, -6f);

        TextMeshProUGUI placeholder = CreateText(textArea.transform, placeholderText, 19f, Vector2.zero, size - new Vector2(28f, 12f), new Color(0.5f, 0.55f, 0.65f, 0.65f));
        placeholder.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI inputText = CreateText(textArea.transform, "", 21f, Vector2.zero, size - new Vector2(28f, 12f), new Color(1f, 0.94f, 0.55f));
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.enableAutoSizing = true;
        inputText.fontSizeMin = 12f;
        inputText.fontSizeMax = 21f;
        inputText.overflowMode = TextOverflowModes.Truncate;

        var field = inputObj.GetComponent<TMP_InputField>();
        field.textViewport = taRt;
        field.textComponent = inputText;
        field.placeholder = placeholder;
        field.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        return field;
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = SanitizeNoBrackets(content);
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 11f;
        tmp.fontSizeMax = fontSize;
        tmp.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.28f, Color.black);
        return tmp;
    }

    private GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, float fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        CreateText(btnObj.transform, label, fontSize, Vector2.zero, size - new Vector2(16f, 8f), Color.white);
        return btnObj;
    }
}
