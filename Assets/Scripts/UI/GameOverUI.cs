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
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset luot hoi sinh khi load map moi hoac choi lai
        RespawnsUsedInCurrentMap = 0;
    }

    public static void Show()
    {
        if (Instance != null)
        {
            Instance.TriggerShow();
        }
        else
        {
            CreateRuntimeGameOverUI();
        }
    }

    public void TriggerShow()
    {
        if (isShown) return;
        isShown = true;

        UpdateButtonsAndTexts();
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
                ? (isVi ? "- Ban con 1 lan hoi sinh trong ban do nay -" : "- You have 1 respawn left for this realm -")
                : (isVi ? "- Da het luot hoi sinh cho map nay (Toi da: 1/1) -" : "- Out of respawns for this map (Max: 1/1) -");
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

        // 3. Nut Ve trang dang nhap
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
        yield return new WaitForSeconds(0.4f);

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.6f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine(System.Action onComplete)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
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
            var playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Revive();
            }
        }));
    }

    /// <summary>
    /// Choi lai tu dau map: Reset luot hoi sinh va load lai scene
    /// </summary>
    public void OnRestartMapClicked()
    {
        RespawnsUsedInCurrentMap = 0;
        Time.timeScale = 1f;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySwordSlash();

        // CO CHE DARK SOULS: Nguoi choi chet khong hoi sinh ma chon Choi lai tu dau,
        // toan bo so Shards dang mang theo se bi danh roi tai vi tri tu tran!
        var inv = Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.DropAllShardsOnDeath(PlayerHealth.LastDeathPosition, SceneManager.GetActiveScene().name);
        }

        Debug.Log("<color=cyan>[RESTART MAP]</color> Choi lai tu dau map! Toan bo tien da roi tai vi tri guc nga.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Thoat ve trang dang nhap
    /// </summary>
    public void OnExitToLoginClicked()
    {
        RespawnsUsedInCurrentMap = 0;
        Time.timeScale = 1f;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySwordSlash();

        // CO CHE DARK SOULS: Danh roi tien tai vi tri chet truoc khi thoat
        var inv = Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.DropAllShardsOnDeath(PlayerHealth.LastDeathPosition, SceneManager.GetActiveScene().name);
        }

        Debug.Log("<color=yellow>[EXIT]</color> Tro ve trang LoginScene...");
        SceneManager.LoadScene("LoginScene");
    }

    public static void CreateRuntimeGameOverUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("GameOverPanel");
        if (existing != null)
        {
            var go = existing.GetComponent<GameOverUI>();
            if (go != null) { go.TriggerShow(); return; }
        }

        GameObject panelObj = new GameObject("GameOverPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.02f, 0.02f, 0.94f); // Dark red death vignette

        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        var gameOverComp = panelObj.AddComponent<GameOverUI>();
        gameOverComp.canvasGroup = cg;

        // Center Card Box
        GameObject boxObj = new GameObject("CenterBox");
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRect = boxObj.AddComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(600, 480);
        Image boxBg = boxObj.AddComponent<Image>();
        boxBg.color = new Color(0.11f, 0.12f, 0.18f, 0.98f);
        var outline = boxObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.15f, 0.15f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Title: YOU DIED
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 185);
        titleRect.sizeDelta = new Vector2(560, 60);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "YOU DIED";
        titleTmp.fontSize = 48;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.95f, 0.2f, 0.2f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        gameOverComp.titleText = titleTmp;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 130);
        subRect.sizeDelta = new Vector2(540, 35);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Chuyen hanh trinh cua ban da tam dung...";
        subTmp.fontSize = 17;
        subTmp.color = new Color(0.85f, 0.75f, 0.75f);
        subTmp.alignment = TextAlignmentOptions.Center;
        gameOverComp.subtitleText = subTmp;

        // Respawn Status Indicator
        GameObject statusObj = new GameObject("RespawnStatus");
        statusObj.transform.SetParent(boxObj.transform, false);
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 85);
        statusRect.sizeDelta = new Vector2(540, 30);
        var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.fontSize = 15;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.alignment = TextAlignmentOptions.Center;
        gameOverComp.respawnStatusText = statusTmp;

        // 1. Button: RESPAWN (HOI SINH TAI CHO)
        gameOverComp.respawnButton = CreateButton(boxObj.transform, "Btn_Respawn", "HOI SINH TAI CHO (1/1)", new Vector2(0, 20), new Vector2(480, 52), new Color(0.18f, 0.65f, 0.45f), 19, out gameOverComp.respawnBtnText);

        // 2. Button: RESTART MAP (CHOI LAI TU DAU MAP)
        gameOverComp.restartButton = CreateButton(boxObj.transform, "Btn_RestartMap", "CHOI LAI TU DAU", new Vector2(0, -45), new Vector2(480, 52), new Color(0.85f, 0.45f, 0.15f), 19, out gameOverComp.restartBtnText);

        // 3. Button: EXIT TO LOGIN (VE TRANG DANG NHAP)
        gameOverComp.exitButton = CreateButton(boxObj.transform, "Btn_ExitLogin", "VE TRANG DANG NHAP", new Vector2(0, -110), new Vector2(480, 48), new Color(0.65f, 0.22f, 0.22f), 17, out gameOverComp.exitBtnText);

        gameOverComp.TriggerShow();
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, float fontSize, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = bgColor;
        var btn = obj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
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
