using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class VictoryUI : MonoBehaviour
{
    public static VictoryUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI enemiesText;
    [SerializeField] private TextMeshProUGUI shardsText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button nextRealmButton;

    private static float runStartTime = 0f;
    private static int enemiesKilled = 0;
    private bool isEnded = false;

    void Awake()
    {
        Instance = this;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartRun);
        if (nextRealmButton != null) nextRealmButton.onClick.AddListener(GoToNextRealm);
    }

    void OnEnable()
    {
        Instance = this;
        BotWheelAI.OnEnemyDied += HandleEnemyDied;
        BloodMonsterAI.OnEnemyDied += HandleBloodMonsterDied;
    }

    void OnDisable()
    {
        BotWheelAI.OnEnemyDied -= HandleEnemyDied;
        BloodMonsterAI.OnEnemyDied -= HandleBloodMonsterDied;
    }

    void Start()
    {
        if (runStartTime <= 0f) runStartTime = Time.time;
    }

    private void HandleEnemyDied(BotWheelAI enemy)
    {
        enemiesKilled++;
    }

    private void HandleBloodMonsterDied(BloodMonsterAI enemy)
    {
        enemiesKilled++;
    }

    public void ShowVictoryScreen()
    {
        if (isEnded) return;
        isEnded = true;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "SampleScene" || currentScene.Contains("Map1"))
        {
            PlayerPrefs.SetInt("Map1_Completed", 1);
            PlayerPrefs.Save();
        }

        float elapsed = Time.time - (runStartTime > 0 ? runStartTime : Time.time - 90f);
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);

        int totalShards = 0;
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null) totalShards = inv.GetShards();
        }

        string rank = "S RANK";
        if (elapsed > 180f) rank = "B RANK";
        else if (elapsed > 120f) rank = "A RANK";

        if (timeText != null) timeText.text = timeStr;
        if (enemiesText != null) enemiesText.text = $"{enemiesKilled} Slain";
        if (shardsText != null) shardsText.text = $"{totalShards} Shards";
        if (rankText != null) rankText.text = rank;

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        yield return new WaitForSeconds(0.3f);

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float t = 0;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.6f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        runStartTime = 0f;
        enemiesKilled = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToNextRealm()
    {
        Time.timeScale = 1f;
        runStartTime = 0f;
        enemiesKilled = 0;
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "SampleScene" || currentScene.Contains("Map1"))
        {
            if (Application.CanStreamedLevelBeLoaded("Map2_ValleyRuin"))
            {
                SceneManager.LoadScene("Map2_ValleyRuin");
                return;
            }
        }
        else if (currentScene.Contains("Map2") || currentScene.Contains("Valley") || currentScene.Contains("Map3") || currentScene.Contains("Ancient"))
        {
            EndingCreditsUI.ShowEnding();
            return;
        }

        Debug.LogWarning("[VICTORY] Không tìm thấy Scene tiếp theo! Tải lại scene hiện tại.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void CreateRuntimeVictoryUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("VictoryPanel");
        if (existing != null)
        {
            var v = existing.GetComponent<VictoryUI>();
            if (v != null) { v.ShowVictoryScreen(); return; }
        }

        GameObject panelObj = new GameObject("VictoryPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.03f, 0.08f, 0.92f);

        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        var victoryComp = panelObj.AddComponent<VictoryUI>();

        // Card Window
        GameObject cardObj = new GameObject("CardWindow");
        cardObj.transform.SetParent(panelObj.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(540, 420);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.11f, 0.09f, 0.16f, 0.98f);

        // Title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 150);
        titleRect.sizeDelta = new Vector2(500, 50);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "REALM PURIFIED!";
        titleTmp.fontSize = 36;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.85f, 0.2f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(cardObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 115);
        subRect.sizeDelta = new Vector2(480, 30);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "The ancient ruins have been cleansed of corruption.";
        subTmp.fontSize = 15;
        subTmp.color = new Color(0.75f, 0.75f, 0.85f);
        subTmp.alignment = TextAlignmentOptions.Center;

        // Stats Container
        GameObject statsContainer = new GameObject("StatsContainer");
        statsContainer.transform.SetParent(cardObj.transform, false);
        RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
        statsRect.anchoredPosition = new Vector2(0, 20);
        statsRect.sizeDelta = new Vector2(440, 140);

        var timeTmp = CreateRow(statsContainer.transform, "TIME ELAPSED", "01:25", 45);
        var enemiesTmp = CreateRow(statsContainer.transform, "ENEMIES SLAIN", $"{enemiesKilled} Slain", 15);
        var shardsTmp = CreateRow(statsContainer.transform, "AETHEL SHARDS", "0 Shards", -15);
        var rankTmp = CreateRow(statsContainer.transform, "CLEAR RANK", "S RANK", -45, new Color(1f, 0.85f, 0.2f));

        // Buttons
        GameObject btnContainer = new GameObject("ButtonsContainer");
        btnContainer.transform.SetParent(cardObj.transform, false);
        RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -145);
        btnRect.sizeDelta = new Vector2(460, 60);

        var playBtn = CreateBtn(btnContainer.transform, "Btn_PlayAgain", "PLAY AGAIN", new Vector2(-115, 0), new Color(0.25f, 0.35f, 0.7f));
        var nextBtn = CreateBtn(btnContainer.transform, "Btn_NextRealm", "NEXT REALM", new Vector2(115, 0), new Color(0.85f, 0.45f, 0.15f));

        playBtn.onClick.AddListener(victoryComp.RestartRun);
        nextBtn.onClick.AddListener(victoryComp.GoToNextRealm);

        victoryComp.timeText = timeTmp;
        victoryComp.enemiesText = enemiesTmp;
        victoryComp.shardsText = shardsTmp;
        victoryComp.rankText = rankTmp;
        victoryComp.canvasGroup = cg;

        victoryComp.ShowVictoryScreen();
    }

    private static TextMeshProUGUI CreateRow(Transform parent, string label, string defaultValue, float posY, Color? valColor = null)
    {
        GameObject row = new GameObject("Row_" + label);
        row.transform.SetParent(parent, false);
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, posY);
        rect.sizeDelta = new Vector2(420, 26);

        GameObject lblObj = new GameObject("Label");
        lblObj.transform.SetParent(row.transform, false);
        RectTransform lblRect = lblObj.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0.5f);
        lblRect.anchorMax = new Vector2(0, 0.5f);
        lblRect.pivot = new Vector2(0, 0.5f);
        lblRect.sizeDelta = new Vector2(200, 26);
        var lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 16;
        lblTmp.color = new Color(0.65f, 0.65f, 0.75f);
        lblTmp.alignment = TextAlignmentOptions.Left;

        GameObject valObj = new GameObject("Value");
        valObj.transform.SetParent(row.transform, false);
        RectTransform valRect = valObj.AddComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1, 0.5f);
        valRect.anchorMax = new Vector2(1, 0.5f);
        valRect.pivot = new Vector2(1, 0.5f);
        valRect.sizeDelta = new Vector2(200, 26);
        var valTmp = valObj.AddComponent<TextMeshProUGUI>();
        valTmp.text = defaultValue;
        valTmp.fontSize = 18;
        valTmp.fontStyle = FontStyles.Bold;
        valTmp.color = valColor.HasValue ? valColor.Value : Color.white;
        valTmp.alignment = TextAlignmentOptions.Right;

        return valTmp;
    }

    private static Button CreateBtn(Transform parent, string name, string text, Vector2 pos, Color bgColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(200, 50);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }
}
