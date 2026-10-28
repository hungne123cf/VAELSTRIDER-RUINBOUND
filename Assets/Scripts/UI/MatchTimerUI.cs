using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Bộ đếm thời gian trong trận (Match Stopwatch HUD).
/// Thiết kế giao diện Dark Fantasy sang trọng, hiển thị thời gian chính xác (00:00).
/// Chạy liên tục từ Map này sang Map khác (Map 1 -> Map 2 -> Map 3) trong suốt cả Run,
/// chỉ reset về 00:00 khi bắt đầu Run mới từ Main Menu hoặc khi bấm Thử Lại (Restart).
/// </summary>
public class MatchTimerUI : MonoBehaviour
{
    public static MatchTimerUI Instance { get; private set; }

    [Header("Timer State")]
    // Lưu thời gian dạng static để không bị mất khi chuyển cảnh giữa các Map
    private static float totalRunElapsedTime = 0f;
    private static bool isTimerRunning = false;
    private static float lastFinalTime = 0f;

    [Header("UI Elements")]
    private TextMeshProUGUI timerText;
    private Image timerBackground;
    private CanvasGroup canvasGroup;

    public float ElapsedTime => totalRunElapsedTime;
    public static float CurrentElapsedTime => totalRunElapsedTime;
    public static float LastFinalClearTime => lastFinalTime > 0f ? lastFinalTime : totalRunElapsedTime;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (timerText == null) timerText = GetComponentInChildren<TextMeshProUGUI>();
        UpdateTimerText();
    }

    void Start()
    {
        string curScene = SceneManager.GetActiveScene().name;
        if (curScene != "LoginScene" && curScene != "MainMenuScene")
        {
            StartTimer();
        }
        UpdateTimerText();
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
        if (scene.name == "LoginScene" || scene.name == "MainMenuScene")
        {
            StopTimer();
            totalRunElapsedTime = 0f;
            if (gameObject != null) gameObject.SetActive(false);
        }
        else
        {
            // Chuyển Map trong trận (Map 1 -> Map 2 -> Map 3):
            // TIẾP TỤC CHẠY THỜI GIAN CỦA RUN, KHÔNG RESET VỀ 00:00!
            StartTimer();
            UpdateTimerText();
        }
    }

    void Update()
    {
        if (!isTimerRunning) return;

        totalRunElapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            string label = LocalizationManager.T("THỜI GIAN", "TIME");
            timerText.text = $"<color=#FFA502><size=11>{label}</size></color>  <color=#FFFFFF><b>{FormatTime(totalRunElapsedTime)}</b></color>";
        }
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    public void ResumeTimer()
    {
        isTimerRunning = true;
        UpdateTimerText();
    }

    /// <summary>
    /// Bắt đầu một Run hoàn toàn mới: Reset thời gian về 00:00 và bắt đầu đếm
    /// </summary>
    public static void ResetRunTimer()
    {
        totalRunElapsedTime = 0f;
        lastFinalTime = 0f;
        isTimerRunning = true;
        if (Instance != null) Instance.UpdateTimerText();
    }

    public void ResetAndStartTimer()
    {
        ResetRunTimer();
    }

    public void StopTimer()
    {
        if (isTimerRunning)
        {
            isTimerRunning = false;
            lastFinalTime = totalRunElapsedTime;
        }
    }

    public static void PauseRunTimer()
    {
        if (isTimerRunning)
        {
            isTimerRunning = false;
            lastFinalTime = totalRunElapsedTime;
        }
    }

    public static void ContinueRunTimer()
    {
        isTimerRunning = true;
        if (Instance != null) Instance.UpdateTimerText();
    }

    public static string GetFormattedCurrentTime()
    {
        return FormatTime(CurrentElapsedTime);
    }

    public static string GetFormattedLastClearTime()
    {
        return FormatTime(lastFinalTime > 0f ? lastFinalTime : CurrentElapsedTime);
    }

    public static string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public static MatchTimerUI EnsureUIExists()
    {
        if (Instance != null)
        {
            Instance.gameObject.SetActive(true);
            Instance.UpdateTimerText();
            return Instance;
        }

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("MatchTimerHUD");
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            var exRt = existing.GetComponent<RectTransform>();
            if (exRt != null)
            {
                exRt.anchoredPosition = new Vector2(0f, -8f);
                exRt.sizeDelta = new Vector2(120f, 26f);
            }
            Instance = existing.GetComponent<MatchTimerUI>();
            if (Instance != null)
            {
                Instance.UpdateTimerText();
                return Instance;
            }
        }

        // Tạo HUD Badge thời gian nằm chính giữa mép trên Canvas
        GameObject rootObj = new GameObject("MatchTimerHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootObj.layer = 5;
        rootObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = rootObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta = new Vector2(120f, 26f);

        Image bgImg = rootObj.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.92f);

        var outline = rootObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.82f, 0.3f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Text Thời Gian
        GameObject textObj = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer));
        textObj.layer = 5;
        textObj.transform.SetParent(rootObj.transform, false);
        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        string label = LocalizationManager.T("THỜI GIAN", "TIME");
        tmp.text = $"<color=#FFA502><size=11>{label}</size></color>  <color=#FFFFFF><b>{FormatTime(totalRunElapsedTime)}</b></color>";
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        var timerComp = rootObj.AddComponent<MatchTimerUI>();
        timerComp.timerText = tmp;
        timerComp.timerBackground = bgImg;

        Instance = timerComp;
        return timerComp;
    }
}
