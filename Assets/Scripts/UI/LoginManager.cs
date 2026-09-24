using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button guestLoginBtn;
    [SerializeField] private Button googleLoginBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private TextMeshProUGUI guestBtnText;
    [SerializeField] private TextMeshProUGUI googleBtnText;
    [SerializeField] private TextMeshProUGUI versionText;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private SettingsUI settingsUI;

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
    }

    private void Start()
    {
        HookButtons();
        UpdateAllTexts();
        LocalizationManager.OnLanguageChanged += UpdateAllTexts;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= UpdateAllTexts;
    }

    private void EnsureInputSystem()
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            var esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null)
        {
            Destroy(standalone);
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

    private void HookButtons()
    {
        if (guestLoginBtn != null) guestLoginBtn.onClick.AddListener(OnGuestLoginClicked);
        if (googleLoginBtn != null) googleLoginBtn.onClick.AddListener(OnGoogleLoginClicked);
        if (settingsBtn != null) settingsBtn.onClick.AddListener(OnSettingsClicked);
    }

    public void UpdateAllTexts()
    {
        if (titleText != null) titleText.text = LocalizationManager.Get("APP_TITLE");
        if (subtitleText != null) subtitleText.text = LocalizationManager.Get("LOGIN_SUBTITLE");
        if (guestBtnText != null) guestBtnText.text = LocalizationManager.Get("BTN_GUEST_LOGIN");
        if (googleBtnText != null) googleBtnText.text = LocalizationManager.Get("BTN_GOOGLE_LOGIN");
        if (versionText != null) versionText.text = LocalizationManager.Get("LOGIN_VERSION");
    }

    public void OnGuestLoginClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        PlayerPrefs.SetString("Player_Account_Type", "Guest");
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnGoogleLoginClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        PlayerPrefs.SetString("Player_Account_Type", "Google");
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnSettingsClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();
        var s = SettingsUI.Instance != null ? SettingsUI.Instance : SettingsUI.EnsureUIExists();
        if (s != null)
        {
            s.OpenSettings();
        }
    }
}
