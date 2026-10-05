using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Bộ đếm thời gian trong trận (Match Stopwatch HUD).
/// Thiết kế giao diện Dark Fantasy sang trọng, hiển thị thời gian chính xác (00:00).
/// Tự động đồng bộ với VictoryUI và GameOverUI.
/// </summary>
public class MatchTimerUI : MonoBehaviour
{
    public static MatchTimerUI Instance { get; private set; }

    [Header("Timer State")]
    private float elapsedTime = 0f;
    private bool isRunning = false;
    private static float lastFinalTime = 0f;

    [Header("UI Elements")]
    private TextMeshProUGUI timerText;
    private Image timerBackground;
    private CanvasGroup canvasGroup;

    public static float CurrentElapsedTime => Instance != null ? Instance.elapsedTime : 0f;
    public static float LastFinalClearTime => lastFinalTime;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        StartTimer();
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
        if (scene.name != "LoginScene" && scene.name != "MainMenuScene")
        {
            ResetAndStartTimer();
        }
        else
        {
            StopTimer();
            if (gameObject != null) gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"<color=#FFA502><size=11>TIME</size></color>  <color=#FFFFFF><b>{FormatTime(elapsedTime)}</b></color>";
        }
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void ResetAndStartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        if (timerText != null) timerText.text = "<color=#FFA502><size=11>TIME</size></color>  <color=#FFFFFF><b>00:00</b></color>";
    }

    public void StopTimer()
    {
        if (isRunning)
        {
            isRunning = false;
            lastFinalTime = elapsedTime;
        }
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
            return Instance;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("MatchTimerHUD");
        if (existing != null)
        {
            Instance = existing.GetComponent<MatchTimerUI>();
            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
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
        rt.anchoredPosition = new Vector2(0f, -14f);
        rt.sizeDelta = new Vector2(130f, 32f);

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
        tmp.text = "<color=#FFA502><size=11>TIME</size></color>  <color=#FFFFFF><b>00:00</b></color>";
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
