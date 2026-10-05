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
        EnsureInputSystem();
        EnsureCloudService();
    }

    private void Start()
    {
        AutoBindUIElements();
        LoadRememberedAccount();
        HookButtons();
        SetMode(false); // Mặc định mở Tab Đăng Nhập
        UpdateEyeIconDisplay();
        UpdateAllTexts();
        LocalizationManager.OnLanguageChanged += UpdateAllTexts;

        ShowLoginPanel();
    }

    private void Update()
    {
        // Hiệu ứng nhịp thở cho chữ TAP TO START
        if (tapToStartPanel != null && tapToStartPanel.activeSelf && tapToStartText != null && !isStartingGame)
        {
            pulseTimer += Time.deltaTime * 3.5f;
            float alpha = Mathf.PingPong(pulseTimer, 0.7f) + 0.3f;
            Color c = tapToStartText.color;
            c.a = alpha;
            tapToStartText.color = c;
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
        if (tabLoginBg != null) tabLoginBg.color = !isRegisterMode ? new Color(0.24f, 0.50f, 0.90f) : new Color(0.14f, 0.17f, 0.25f);
        if (tabRegisterBg != null) tabRegisterBg.color = isRegisterMode ? new Color(0.85f, 0.50f, 0.15f) : new Color(0.14f, 0.17f, 0.25f);

        if (tabLoginTxt != null) tabLoginTxt.color = !isRegisterMode ? Color.white : new Color(0.65f, 0.70f, 0.80f);
        if (tabRegisterTxt != null) tabRegisterTxt.color = isRegisterMode ? Color.white : new Color(0.65f, 0.70f, 0.80f);

        if (actionSubmitButtonText != null)
        {
            actionSubmitButtonText.text = isRegisterMode ? "TẠO TÀI KHOẢN MỚI" : "ĐĂNG NHẬP VÀO GAME";
        }
        if (actionSubmitButton != null)
        {
            var img = actionSubmitButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = isRegisterMode ? new Color(0.85f, 0.50f, 0.15f) : new Color(0.24f, 0.50f, 0.90f);
            }
        }
    }

    public void UpdateAllTexts()
    {
        if (titleText != null) titleText.text = LocalizationManager.Get("APP_TITLE");
        if (subtitleText != null) subtitleText.text = LocalizationManager.Get("LOGIN_SUBTITLE");
        if (tapToStartText != null && !isStartingGame)
        {
            tapToStartText.text = "— CHẠM VÀO MÀN HÌNH ĐỂ BẮT ĐẦU —";
        }
        if (versionText != null) versionText.text = "Phiên bản 1.0.0 (Cloud Sync Enabled)";
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
        if (loginCardPanel != null) loginCardPanel.SetActive(true);
        if (tapToStartPanel != null) tapToStartPanel.SetActive(false);
        if (statusFeedbackText != null) statusFeedbackText.text = "";
    }

    public void OnTapToStartClicked()
    {
        if (isStartingGame) return;
        isStartingGame = true;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        StartCoroutine(StartGameTransitionRoutine());
    }

    private IEnumerator StartGameTransitionRoutine()
    {
        if (tapToStartText != null)
        {
            tapToStartText.color = new Color(0.4f, 1f, 0.6f, 1f);
            tapToStartText.text = "ĐANG VÀO THẾ GIỚI CỔ TÍCH...";
        }

        Debug.Log($"<color=green>[START GAME]</color> Chuyển đến MainMenuScene...");
        yield return new WaitForSeconds(0.45f);

        SceneManager.LoadScene("MainMenuScene");
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
