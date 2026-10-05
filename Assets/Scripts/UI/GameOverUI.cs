using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    /// <summary>
    /// Gioi han moi map chi duoc hoi sinh 1 lan duy nhat trong suot luot choi!
    /// </summary>
    public static int RespawnsUsedInCurrentMap = 0;
    public const int MAX_RESPAWNS_PER_MAP = 1;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI respawnBtnText;
    [SerializeField] private TextMeshProUGUI restartBtnText;
    [SerializeField] private TextMeshProUGUI exitBtnText;
    [SerializeField] private TextMeshProUGUI respawnStatusText;

    private bool isShown = false;
    public bool IsShown() => isShown || (canvasGroup != null && canvasGroup.alpha > 0.05f);

    void Awake()
    {
        Instance = this;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LocalizationManager.OnLanguageChanged += UpdateButtonsAndTexts;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        LocalizationManager.OnLanguageChanged -= UpdateButtonsAndTexts;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RespawnsUsedInCurrentMap = 0;
    }

    public static void Show()
    {
        Time.timeScale = 1.0f; // Dam bao khong bi ket thoi gian
        EnsureUIExists();

        if (Instance != null)
        {
            Instance.TriggerShow();
        }
        else
        {
            Debug.LogError("<color=red>[GAMEOVER UI]</color> Khong the khoi tao GameOverUI!");
        }
    }

    public void TriggerShow()
    {
        if (MatchTimerUI.Instance != null) MatchTimerUI.Instance.StopTimer();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        UpdateButtonsAndTexts();

        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    public void UpdateButtonsAndTexts()
    {
        bool canRespawn = RespawnsUsedInCurrentMap < MAX_RESPAWNS_PER_MAP;

        if (titleText != null) titleText.text = LocalizationManager.Get("GAMEOVER_TITLE");
        if (subtitleText != null) subtitleText.text = LocalizationManager.Get("GAMEOVER_SUBTITLE");

        // 1. Nut Respawn (Gioi han 1 lan/map)
        if (respawnButton != null)
        {
            respawnButton.interactable = canRespawn;
            var img = respawnButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = canRespawn ? new Color(0.18f, 0.65f, 0.45f) : new Color(0.25f, 0.26f, 0.32f);
            }

            respawnButton.onClick.RemoveAllListeners();
            if (canRespawn)
            {
                respawnButton.onClick.AddListener(OnRespawnClicked);
            }
        }

        if (respawnBtnText != null)
        {
            respawnBtnText.text = canRespawn 
                ? LocalizationManager.Get("GAMEOVER_RESPAWN_AVAILABLE") 
                : LocalizationManager.Get("GAMEOVER_RESPAWN_USED");
            respawnBtnText.color = canRespawn ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }

        if (respawnStatusText != null)
        {
            bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;
            respawnStatusText.text = canRespawn 
                ? (isVi ? "- Bạn còn 1 lần hồi sinh trong bản đồ này -" : "- You have 1 respawn left for this realm -")
                : (isVi ? "- Đã hết lượt hồi sinh cho map này (Tối đa: 1/1) -" : "- Out of respawns for this map (Max: 1/1) -");
            respawnStatusText.color = canRespawn ? new Color(0.3f, 0.95f, 0.65f) : new Color(0.95f, 0.45f, 0.45f);
        }

        // 2. Nut Choi lai tu dau map
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartMapClicked);
        }
        if (restartBtnText != null)
        {
            restartBtnText.text = LocalizationManager.Get("GAMEOVER_RESTART_MAP");
        }

        // 3. Nut Thoat ve trang dang nhap
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitToLoginClicked);
        }
        if (exitBtnText != null)
        {
            exitBtnText.text = LocalizationManager.Get("GAMEOVER_EXIT_LOGIN");
        }
    }

    private IEnumerator FadeInRoutine()
    {
        isShown = true;
        GameAutoBootstrap.SetGameplayHUDVisible(false);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.35f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine(System.Action onComplete)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.25f);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        isShown = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Hoi sinh nhan vat tai cho (chi duoc dung 1 lan moi map)
    /// </summary>
    public void OnRespawnClicked()
    {
        if (RespawnsUsedInCurrentMap >= MAX_RESPAWNS_PER_MAP)
        {
            Debug.LogWarning("<color=red>[RESPAWN]</color> Da het luot hoi sinh trong map nay!");
            return;
        }

        RespawnsUsedInCurrentMap++;
        Debug.Log($"<color=green>[RESPAWN]</color> Da su dung luot hoi sinh duy nhat ({RespawnsUsedInCurrentMap}/{MAX_RESPAWNS_PER_MAP})!");

        StartCoroutine(FadeOutRoutine(() =>
        {
            Time.timeScale = 1f;
            var playerHealth = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Revive();
            }
        }));
    }

    /// <summary>
    /// Choi lai tu dau map
    /// </summary>
    public void OnRestartMapClicked()
    {
        RespawnsUsedInCurrentMap = 0;
        Time.timeScale = 1f;

        var inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.SaveData();
        }

        Debug.Log("<color=cyan>[RESTART MAP]</color> Choi lai tu dau map (Giu nguyen linh hon/Shards)!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Thoat ve trang dang nhap
    /// </summary>
    public void OnExitToLoginClicked()
    {
        RespawnsUsedInCurrentMap = 0;
        Time.timeScale = 1f;

        var inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.SaveData();
        }

        Debug.Log("<color=yellow>[EXIT]</color> Tro ve trang LoginScene (Giu nguyen linh hon/Shards)...");
        SceneManager.LoadScene("LoginScene");
    }

    public static GameOverUI EnsureUIExists()
    {
        if (Instance != null) return Instance;

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) return null;

        Transform existing = rootCanvas.transform.Find("GameOverPanel");
        if (existing != null)
        {
            Instance = existing.GetComponent<GameOverUI>();
            if (Instance != null) return Instance;
        }

        // Tao Container UI Panel
        GameObject panelObj = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.layer = 5;
        panelObj.transform.SetParent(rootCanvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.06f, 0.02f, 0.02f, 0.94f); // Dark death vignette

        // Overriding canvas de dam bao luon o tren cung
        Canvas panelCanvas = panelObj.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 9999;
        panelObj.AddComponent<GraphicRaycaster>();

        CanvasGroup cg = panelObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Instance = panelObj.AddComponent<GameOverUI>();
        Instance.canvasGroup = cg;

        // Center Card Box
        GameObject boxObj = new GameObject("CenterBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(620, 500);
        boxRect.anchoredPosition = Vector2.zero;

        Image boxBg = boxObj.GetComponent<Image>();
        boxBg.color = new Color(0.10f, 0.11f, 0.16f, 0.98f);
        var outline = boxObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.7f, 0.18f, 0.2f, 0.9f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // Title: YOU DIED
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.layer = 5;
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 190);
        titleRect.sizeDelta = new Vector2(580, 60);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "YOU DIED";
        titleTmp.fontSize = 46;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.95f, 0.22f, 0.25f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        Instance.titleText = titleTmp;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform));
        subObj.layer = 5;
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 135);
        subRect.sizeDelta = new Vector2(560, 35);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Chuyen hanh trinh cua ban da tam dung...";
        subTmp.fontSize = 17;
        subTmp.color = new Color(0.85f, 0.75f, 0.78f);
        subTmp.alignment = TextAlignmentOptions.Center;
        Instance.subtitleText = subTmp;

        // Respawn Status Indicator
        GameObject statusObj = new GameObject("RespawnStatus", typeof(RectTransform));
        statusObj.layer = 5;
        statusObj.transform.SetParent(boxObj.transform, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 85);
        statusRect.sizeDelta = new Vector2(560, 32);
        var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.fontSize = 15;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.alignment = TextAlignmentOptions.Center;
        Instance.respawnStatusText = statusTmp;

        // 1. Button: RESPAWN
        Instance.respawnButton = CreateButton(boxObj.transform, "Btn_Respawn", "HỒI SINH TẠI CHỖ (1/1)", new Vector2(0, 18), new Vector2(500, 54), new Color(0.18f, 0.65f, 0.45f), 20, out Instance.respawnBtnText);

        // 2. Button: RESTART MAP
        Instance.restartButton = CreateButton(boxObj.transform, "Btn_RestartMap", "CHƠI LẠI TỪ ĐẦU", new Vector2(0, -50), new Vector2(500, 54), new Color(0.85f, 0.45f, 0.15f), 20, out Instance.restartBtnText);

        // 3. Button: EXIT TO LOGIN
        Instance.exitButton = CreateButton(boxObj.transform, "Btn_ExitLogin", "VỀ TRANG ĐĂNG NHẬP", new Vector2(0, -118), new Vector2(500, 50), new Color(0.65f, 0.22f, 0.25f), 18, out Instance.exitBtnText);

        return Instance;
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, float fontSize, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = obj.GetComponent<Image>();
        img.color = bgColor;

        var btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.layer = 5;
        txtObj.transform.SetParent(obj.transform, false);

        var tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        btnTxt = txtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = text;
        btnTxt.fontSize = fontSize;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.raycastTarget = false;
        return btn;
    }
}
