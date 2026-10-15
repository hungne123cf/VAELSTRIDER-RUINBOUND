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
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI enemiesText;
    [SerializeField] private TextMeshProUGUI shardsText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button nextRealmButton;

    private static float runStartTime = 0f;
    private static int enemiesKilled = 0;
    private bool isEnded = false;
    public bool IsShown() => isEnded || (canvasGroup != null && canvasGroup.alpha > 0.05f);

    void Awake()
    {
        Instance = this;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (titleText == null)
        {
            var t = transform.Find("CardWindow/TitleText");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleText == null)
        {
            var s = transform.Find("CardWindow/SubtitleText");
            if (s != null) subtitleText = s.GetComponent<TextMeshProUGUI>();
        }

        // Xoa cac component LocalizedText trung lap bi loi gan APP_TITLE de tranh de len nhau
        if (titleText != null)
        {
            var loc = titleText.GetComponent<LocalizedText>();
            if (loc != null) Destroy(loc);
            titleText.text = LocalizationManager.T("CHIẾN THẮNG!", "REALM PURIFIED!");
        }
        if (subtitleText != null)
        {
            var loc = subtitleText.GetComponent<LocalizedText>();
            if (loc != null) Destroy(loc);
            subtitleText.text = LocalizationManager.T("Tàn tích cổ đại đã được thanh tẩy khỏi sự tha hóa.", "The ancient ruins have been cleansed of corruption.");
        }

        if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartRun);
        if (nextRealmButton != null) nextRealmButton.onClick.AddListener(GoToNextRealm);
    }

    void OnEnable()
    {
        Instance = this;
        BotWheelAI.OnEnemyDied += HandleEnemyDied;
        BloodMonsterAI.OnEnemyDied += HandleBloodMonsterDied;
        GoblinAI.OnEnemyDied += HandleGoblinDied;
        SkeletonAI.OnEnemyDied += HandleSkeletonDied;
        SlimeAI.OnEnemyDied += HandleSlimeDied;
        SkeletonKingBossAI.OnBossDied += HandleSkeletonKingDied;
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        BotWheelAI.OnEnemyDied -= HandleEnemyDied;
        BloodMonsterAI.OnEnemyDied -= HandleBloodMonsterDied;
        GoblinAI.OnEnemyDied -= HandleGoblinDied;
        SkeletonAI.OnEnemyDied -= HandleSkeletonDied;
        SlimeAI.OnEnemyDied -= HandleSlimeDied;
        SkeletonKingBossAI.OnBossDied -= HandleSkeletonKingDied;
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (titleText != null) titleText.text = LocalizationManager.T("CHIẾN THẮNG!", "REALM PURIFIED!");
        if (subtitleText != null) subtitleText.text = LocalizationManager.T("Tàn tích cổ đại đã được thanh tẩy khỏi sự tha hóa.", "The ancient ruins have been cleansed of corruption.");
        if (isEnded)
        {
            if (enemiesText != null) enemiesText.text = $"{enemiesKilled} " + LocalizationManager.T("Kẻ địch", "Slain");
            if (playAgainButton != null)
            {
                var txt = playAgainButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = LocalizationManager.T("CHƠI LẠI", "PLAY AGAIN");
            }
            if (nextRealmButton != null)
            {
                var txt = nextRealmButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = LocalizationManager.T("ẢI TIẾP THEO", "NEXT REALM");
            }
        }
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

    private void HandleGoblinDied(GoblinAI enemy)
    {
        enemiesKilled++;
    }

    private void HandleSkeletonDied(SkeletonAI enemy)
    {
        enemiesKilled++;
    }

    private void HandleSlimeDied(SlimeAI enemy)
    {
        enemiesKilled++;
    }

    private void HandleSkeletonKingDied(SkeletonKingBossAI boss)
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
            AccountDatabase.AddItemToActiveUser("scroll_summon_mythic", 1, "GachaArtifact", "Chiến Thắng Map 1");
            AccountDatabase.AddItemToActiveUser("gem_amethyst", 3, "Material", "Chiến Thắng Map 1");
        }
        else if (currentScene.Contains("Map2") || currentScene.Contains("Valley"))
        {
            PlayerPrefs.SetInt("Map2_Completed", 1);
            AccountDatabase.AddItemToActiveUser("scroll_summon_mythic", 2, "GachaArtifact", "Chiến Thắng Map 2");
            AccountDatabase.AddItemToActiveUser("gem_bloodstone", 2, "Material", "Chiến Thắng Map 2");
            AccountDatabase.AddItemToActiveUser("chest_ancient_relic", 1, "GachaArtifact", "Chiến Thắng Map 2");
        }
        else if (currentScene.Contains("Map3") || currentScene.Contains("Forgotten"))
        {
            PlayerPrefs.SetInt("Map3_Completed", 1);
            AccountDatabase.AddItemToActiveUser("core_abyssal_soul", 1, "GachaArtifact", "Đại Thắng Map 3");
            AccountDatabase.AddItemToActiveUser("scroll_summon_mythic", 3, "GachaArtifact", "Đại Thắng Map 3");
        }
        PlayerPrefs.Save();
        AccountDatabase.SaveActiveSessionToDatabase();

        if (MatchTimerUI.Instance != null) MatchTimerUI.Instance.StopTimer();
        float elapsed = MatchTimerUI.LastFinalClearTime > 0f ? MatchTimerUI.LastFinalClearTime : (Time.time - runStartTime);
        string timeStr = MatchTimerUI.GetFormattedLastClearTime();

        long totalShards = PlayerInventory.GetCurrentRunShards();
        if (totalShards == 0)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null) totalShards = inv.GetShards();
            }
        }

        string rank = "S RANK";
        if (elapsed > 240f) rank = "B RANK";
        else if (elapsed > 150f) rank = "A RANK";

        if (titleText != null) titleText.text = LocalizationManager.T("CHIẾN THẮNG!", "REALM PURIFIED!");
        if (subtitleText != null) subtitleText.text = LocalizationManager.T("Tàn tích cổ đại đã được thanh tẩy khỏi sự tha hóa.", "The ancient ruins have been cleansed of corruption.");

        if (timeText != null) timeText.text = timeStr;
        if (enemiesText != null) enemiesText.text = $"{enemiesKilled} " + LocalizationManager.T("Kẻ địch", "Slain");
        if (shardsText != null) shardsText.text = $"+{MainMenuManager.FormatCompactNumber(totalShards)} " + LocalizationManager.T("Linh hồn", "Shards");
        if (rankText != null) rankText.text = rank;

        GameAutoBootstrap.SetGameplayHUDVisible(false);
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
        PlayerInventory.ResetRunShards();
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
        else if (currentScene.Contains("Map2") || currentScene.Contains("Valley"))
        {
            if (Application.CanStreamedLevelBeLoaded("Map3_ForgottenMemories"))
            {
                SceneManager.LoadScene("Map3_ForgottenMemories");
                return;
            }
        }
        else if (currentScene.Contains("Map3") || currentScene.Contains("Forgotten"))
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
        var locTitle = titleObj.AddComponent<LocalizedText>();
        locTitle.SetTexts("CHIẾN THẮNG!", "REALM PURIFIED!");
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
        var locSub = subObj.AddComponent<LocalizedText>();
        locSub.SetTexts("Tàn tích cổ đại đã được thanh tẩy khỏi sự tha hóa.", "The ancient ruins have been cleansed of corruption.");
        subTmp.fontSize = 15;
        subTmp.color = new Color(0.75f, 0.75f, 0.85f);
        subTmp.alignment = TextAlignmentOptions.Center;

        // Stats Container
        GameObject statsContainer = new GameObject("StatsContainer");
        statsContainer.transform.SetParent(cardObj.transform, false);
        RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
        statsRect.anchoredPosition = new Vector2(0, 20);
        statsRect.sizeDelta = new Vector2(440, 140);

        var timeTmp = CreateRow(statsContainer.transform, "TIME_ROW", LocalizationManager.T("THỜI GIAN", "TIME ELAPSED"), "01:25", 45);
        var enemiesTmp = CreateRow(statsContainer.transform, "ENEMIES_ROW", LocalizationManager.T("KẺ ĐỊCH TIÊU DIỆT", "ENEMIES SLAIN"), $"{enemiesKilled}", 15);
        var shardsTmp = CreateRow(statsContainer.transform, "SHARDS_ROW", LocalizationManager.T("MẢNH LINH HỒN", "AETHEL SHARDS"), "0", -15);
        var rankTmp = CreateRow(statsContainer.transform, "RANK_ROW", LocalizationManager.T("XẾP HẠNG", "CLEAR RANK"), "S RANK", -45, new Color(1f, 0.85f, 0.2f));

        // Buttons
        GameObject btnContainer = new GameObject("ButtonsContainer");
        btnContainer.transform.SetParent(cardObj.transform, false);
        RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -145);
        btnRect.sizeDelta = new Vector2(460, 60);

        var playBtn = CreateBtn(btnContainer.transform, "Btn_PlayAgain", LocalizationManager.T("CHƠI LẠI", "PLAY AGAIN"), new Vector2(-115, 0), new Color(0.25f, 0.35f, 0.7f));
        var nextBtn = CreateBtn(btnContainer.transform, "Btn_NextRealm", LocalizationManager.T("ẢI TIẾP THEO", "NEXT REALM"), new Vector2(115, 0), new Color(0.85f, 0.45f, 0.15f));

        WhiteIconLoader.SetButtonIconAndText(playBtn, "Repeat-Right", LocalizationManager.T("CHƠI LẠI", "PLAY AGAIN"), new Color(0.4f, 0.85f, 1f), Color.white, true);
        WhiteIconLoader.SetButtonIconAndText(nextBtn, "Play", LocalizationManager.T("ẢI TIẾP THEO", "NEXT REALM"), new Color(1f, 0.85f, 0.35f), Color.white, true);

        playBtn.onClick.AddListener(victoryComp.RestartRun);
        nextBtn.onClick.AddListener(victoryComp.GoToNextRealm);

        victoryComp.timeText = timeTmp;
        victoryComp.enemiesText = enemiesTmp;
        victoryComp.shardsText = shardsTmp;
        victoryComp.rankText = rankTmp;
        victoryComp.canvasGroup = cg;

        victoryComp.ShowVictoryScreen();
    }

    private static TextMeshProUGUI CreateRow(Transform parent, string rowName, string label, string defaultValue, float posY, Color? valColor = null)
    {
        GameObject row = new GameObject("Row_" + rowName);
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

