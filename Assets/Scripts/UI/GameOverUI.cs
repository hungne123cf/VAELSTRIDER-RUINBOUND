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
    [SerializeField] private Button mainMenuButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI respawnBtnText;
    [SerializeField] private TextMeshProUGUI restartBtnText;
    [SerializeField] private TextMeshProUGUI mainMenuBtnText;
    [SerializeField] private TextMeshProUGUI respawnStatusText;

    private bool isShown = false;
    public bool IsShown() => isShown || (canvasGroup != null && canvasGroup.alpha > 0.05f);

    // Cache sprites
    private static Sprite _frameSprite;
    private static Sprite _dividerSprite;

    // ─── Neutral Dark Color Palette ────────────────────────────────────────────
    private static readonly Color ColorOverlayBg      = new Color(0.015f, 0.018f, 0.024f, 0.94f);
    private static readonly Color ColorBoxBg          = new Color(0.065f, 0.070f, 0.080f, 0.98f);
    private static readonly Color ColorBoxFrame       = new Color(0.650f, 0.700f, 0.800f, 0.90f);
    private static readonly Color ColorDividerLine    = new Color(0.500f, 0.550f, 0.650f, 0.55f);

    private static readonly Color ColorBtnRestartBg   = new Color(0.140f, 0.150f, 0.180f, 1.00f);
    private static readonly Color ColorBtnMainMenuBg  = new Color(0.120f, 0.130f, 0.160f, 1.00f);
    private static readonly Color ColorBtnFrame       = new Color(0.600f, 0.650f, 0.750f, 0.90f);

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
        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (titleText != null) 
        {
            titleText.text = isVi ? "CHUYẾN PHIÊU LƯU KẾT THÚC" : "EXPEDITION FAILED";
            titleText.enableWordWrapping = false;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 20f;
            titleText.fontSizeMax = 28f;
            var rt = titleText.rectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(620f, 40f);
                rt.anchoredPosition = new Vector2(0f, 172f);
            }

            Transform titleBox = titleText.transform.parent;
            if (titleBox != null)
            {
                var leftWing = titleBox.Find("TitleLeftWing");
                if (leftWing != null) leftWing.gameObject.SetActive(false);
                var rightWing = titleBox.Find("TitleRightWing");
                if (rightWing != null) rightWing.gameObject.SetActive(false);
                var titleDiv = titleBox.Find("TitleDivider");
                if (titleDiv != null)
                {
                    var divRt = titleDiv.GetComponent<RectTransform>();
                    if (divRt != null) divRt.anchoredPosition = new Vector2(0f, 142f);
                }
            }
        }
        if (subtitleText != null) 
        {
            subtitleText.text = isVi ? "Sinh mệnh đã cạn. Mọi kinh nghiệm và lõi ấn chỉ lưu giữ trong chuyến đi này." : "Your journey ends here. Battle levels and augments only exist within this run.";
            var sRt = subtitleText.rectTransform;
            if (sRt != null)
            {
                sRt.sizeDelta = new Vector2(620f, 32f);
                sRt.anchoredPosition = new Vector2(0f, 110f);
            }
        }

        // Game 1 mạng: Ẩn nút Respawn hồi sinh tại chỗ
        if (respawnButton != null)
        {
            respawnButton.gameObject.SetActive(false);
        }

        // Run Summary: Chỉ hiển thị linh hồn thu thập được trong trận đấu hiện tại
        if (respawnStatusText != null)
        {
            long shards = PlayerInventory.GetCurrentRunShards();

            respawnStatusText.text = isVi 
                ? $"<color=#00FFFF><b>LINH HỒN: {shards:N0}</b></color>"
                : $"<color=#00FFFF><b>AETHEL SHARDS: {shards:N0}</b></color>";
            respawnStatusText.color = Color.white;
        }

        // 1. Nút Chơi lại run mới từ đầu
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartMapClicked);
        }
        if (restartBtnText != null)
        {
            restartBtnText.text = isVi ? "HỒI SINH & CHƠI LẠI RUN MỚI" : "REVIVE & START NEW RUN";
            if (restartButton != null)
            {
                WhiteIconLoader.SetButtonIconAndText(restartButton, "Repeat-Right", restartBtnText.text, new Color(1f, 0.85f, 0.35f), Color.white, true);
                restartButton.transform.Find("FrameBorder")?.SetAsLastSibling();
            }
        }

        // 2. Nút Về Main Menu (Sảnh Sanctuary)
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
        }
        if (mainMenuBtnText != null)
        {
            mainMenuBtnText.text = isVi ? "VỀ SẢNH CHÍNH" : "RETURN TO SANCTUARY";
            if (mainMenuButton != null)
            {
                WhiteIconLoader.SetButtonIconAndText(mainMenuButton, "Home", mainMenuBtnText.text, new Color(0.4f, 0.85f, 1f), Color.white, true);
                mainMenuButton.transform.Find("FrameBorder")?.SetAsLastSibling();
            }
        }

        // Đảm bảo khung ngoài CenterBox luôn ở trên cùng
        Transform box = transform.Find("CenterBox");
        if (box != null)
        {
            box.Find("FrameBorder")?.SetAsLastSibling();
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
        gameObject.SetActive(false);

        // Khoi phuc lai toan bo HUD dieu khien, nut bam va UI/UX
        GameAutoBootstrap.SetGameplayHUDVisible(true);
        if (MatchTimerUI.Instance != null) MatchTimerUI.Instance.ResumeTimer();

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
            GameAutoBootstrap.SetGameplayHUDVisible(true);
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
        AccountDatabase.SaveActiveSessionToDatabase();

        CorruptedCycleManager.ResetToNormalCycle();
        PlayerInventory.ResetRunShards();
        PlayerLevelSystem.Instance?.ResetForNewBattle();
        AugmentManager.Instance?.ResetForNewBattle();
        LevelUpChoiceUI.ResetUI();
        LevelUpChoiceUI.ResetMatchRerolls();
        MatchTimerUI.ResetRunTimer();
        PlayerHealth.ResetRunPreservedHealth();

        Debug.Log("<color=cyan>[RESTART MAP]</color> Bat dau run moi tu dau (Map 1 - Level 1)!");
        if (Application.CanStreamedLevelBeLoaded("Map1_Thestart"))
        {
            SceneManager.LoadScene("Map1_Thestart");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    /// <summary>
    /// Ve Menu Chinh (Sanctuary / Main Menu Scene)
    /// </summary>
    public void OnReturnToMainMenuClicked()
    {
        RespawnsUsedInCurrentMap = 0;
        Time.timeScale = 1f;

        var inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.SaveData();
        }
        AccountDatabase.SaveActiveSessionToDatabase();

        CorruptedCycleManager.ResetToNormalCycle();
        PlayerInventory.ResetRunShards();
        PlayerLevelSystem.Instance?.ResetForNewBattle();
        AugmentManager.Instance?.ResetForNewBattle();
        LevelUpChoiceUI.ResetUI();
        LevelUpChoiceUI.ResetMatchRerolls();
        MatchTimerUI.ResetRunTimer();
        PlayerHealth.ResetRunPreservedHealth();

        Debug.Log("<color=cyan>[MAIN MENU]</color> Tro ve MainMenuScene voi toan bo Linh Hon thu thap duoc...");
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// Thoát về sảnh chính (Game Offline thuần túy)
    /// </summary>
    public void OnExitToLoginClicked()
    {
        OnReturnToMainMenuClicked();
    }

    // ─── Asset Loaders ─────────────────────────────────────────────────────────
    private static Sprite GetFrameSprite()
    {
        if (_frameSprite != null) return _frameSprite;
        _frameSprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        if (_frameSprite != null) return _frameSprite;

#if UNITY_EDITOR
        _frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/frame_gothic_border.png");
        if (_frameSprite != null) return _frameSprite;
#endif

        _frameSprite = Resources.Load<Sprite>("UI/frame_gothic_border");
        if (_frameSprite != null) return _frameSprite;

        string[] candidates = new string[]
        {
            System.IO.Path.Combine(Application.dataPath, "Sprites/UI/frame_gothic_border.png"),
            System.IO.Path.Combine(Application.dataPath, "Resources/UI/frame_gothic_border.png"),
            System.IO.Path.Combine(Application.dataPath, "ART/UI/Default/Border/panel-border-004.png")
        };

        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _frameSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
                    break;
                }
            }
        }

        return _frameSprite;
    }

    private static Sprite GetDividerSprite()
    {
        if (_dividerSprite != null) return _dividerSprite;
        _dividerSprite = SoulslikeHUDStyler.GetGothicDividerSprite();
        if (_dividerSprite != null) return _dividerSprite;

#if UNITY_EDITOR
        _dividerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/divider_fade_004.png");
        if (_dividerSprite != null) return _dividerSprite;
#endif

        _dividerSprite = Resources.Load<Sprite>("UI/divider_fade_004");
        if (_dividerSprite != null) return _dividerSprite;

        string[] candidates = new string[]
        {
            System.IO.Path.Combine(Application.dataPath, "Sprites/UI/divider_fade_004.png"),
            System.IO.Path.Combine(Application.dataPath, "Resources/UI/divider_fade_004.png"),
            System.IO.Path.Combine(Application.dataPath, "ART/UI/Default/Divider Fade/divider-fade-004.png")
        };

        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _dividerSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                    break;
                }
            }
        }

        return _dividerSprite;
    }

    public static GameOverUI EnsureUIExists()
    {
        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) return null;

        Transform existing = rootCanvas.transform.Find("GameOverPanel");
        if (existing != null)
        {
            GameOverUI ui = existing.GetComponent<GameOverUI>();
            Transform box = existing.Find("CenterBox");
            if (ui != null && ui.mainMenuButton != null && box != null && box.Find("FrameBorder") != null)
            {
                Instance = ui;
                return Instance;
            }

            // Phiên bản cũ chưa có FrameBorder -> huỷ để tạo mới chuẩn Gothic
            Destroy(existing.gameObject);
            Instance = null;
        }

        // Tạo Container UI Panel phủ toàn màn hình
        GameObject panelObj = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.layer = 5;
        panelObj.transform.SetParent(rootCanvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = ColorOverlayBg;

        // Overriding canvas để đảm bảo luôn ở trên cùng
        Canvas panelCanvas = panelObj.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 31000;
        panelObj.AddComponent<GraphicRaycaster>();

        CanvasGroup cg = panelObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Instance = panelObj.AddComponent<GameOverUI>();
        Instance.canvasGroup = cg;

        // ─── Center Box (Khung Gothic chạm khắc) ───────────────────────────────
        GameObject boxObj = new GameObject("CenterBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(680, 480);
        boxRect.anchoredPosition = Vector2.zero;

        Image boxBg = boxObj.GetComponent<Image>();
        boxBg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        boxBg.type = Image.Type.Sliced;
        boxBg.color = Color.white;

        // ─── Title: YOU DIED / NGƯƠI ĐÃ TỬ TRẬN ──────────────────────────────────────────
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.layer = 5;
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 172);
        titleRect.sizeDelta = new Vector2(620, 44);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = LocalizationManager.T("NGƯƠI ĐÃ TỬ TRẬN", "YOU DIED");
        titleTmp.fontSize = 32;
        titleTmp.enableAutoSizing = true;
        titleTmp.fontSizeMin = 24;
        titleTmp.fontSizeMax = 34;
        titleTmp.enableWordWrapping = false;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.78f, 0.12f, 0.12f); // Deep blood ruby Soulslike
        titleTmp.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(titleTmp, 0.35f, Color.black);
        Instance.titleText = titleTmp;

        // Title Bottom Symmetric Divider (No overlapping flanking wings)
        CreateOrnamentDivider(boxObj.transform, "TitleDivider", new Vector2(0, 142), new Vector2(540, 12), new Color(1f, 0.9f, 0.65f, 0.85f));

        // ─── Subtitle ──────────────────────────────────────────────────────────
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform));
        subObj.layer = 5;
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 110);
        subRect.sizeDelta = new Vector2(620, 32);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Sinh mệnh đã cạn. Mọi kinh nghiệm và lõi ấn chỉ lưu giữ trong chuyến đi này.";
        subTmp.fontSize = 14.5f;
        subTmp.color = new Color(0.75f, 0.78f, 0.86f);
        subTmp.alignment = TextAlignmentOptions.Center;
        Instance.subtitleText = subTmp;

        // ─── Run Summary Indicator ─────────────────────────────────────────────
        GameObject statusObj = new GameObject("RespawnStatus", typeof(RectTransform));
        statusObj.layer = 5;
        statusObj.transform.SetParent(boxObj.transform, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 58);
        statusRect.sizeDelta = new Vector2(620, 34);
        var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.fontSize = 18f;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.alignment = TextAlignmentOptions.Center;
        Instance.respawnStatusText = statusTmp;

        // Section Divider before buttons
        CreateOrnamentDivider(boxObj.transform, "SectionDivider", new Vector2(0, 22), new Vector2(540, 10), ColorDividerLine);

        // ─── 1. Button: START NEW RUN (LEVEL 1) ────────────────────────────────
        Instance.restartButton = CreateFramedButton(boxObj.transform, "Btn_RestartMap", "HỒI SINH & CHƠI LẠI RUN MỚI",
            new Vector2(0, -34), new Vector2(520, 54), ColorBtnRestartBg, ColorBtnFrame, 18, out Instance.restartBtnText);

        // ─── 2. Button: RETURN TO SANCTUARY ────────────────────────────────────
        Instance.mainMenuButton = CreateFramedButton(boxObj.transform, "Btn_MainMenu", "VỀ SẢNH CHÍNH",
            new Vector2(0, -104), new Vector2(520, 54), ColorBtnMainMenuBg, ColorBtnFrame, 18, out Instance.mainMenuBtnText);

        // Đảm bảo khung ngoài của CenterBox nếu có luôn hiển thị trên cùng
        Transform frame = boxObj.transform.Find("FrameBorder");
        if (frame != null) frame.SetAsLastSibling();

        return Instance;
    }

    private static Button CreateFramedButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, Color frameColor, float fontSize, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = obj.GetComponent<Image>();
        img.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.layer = 5;
        txtObj.transform.SetParent(obj.transform, false);

        var tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;

        btnTxt = txtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = text;
        btnTxt.fontSize = fontSize;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = new Color(0.96f, 0.88f, 0.65f, 1f); // Vàng kim cổ điển
        btnTxt.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(btnTxt, 0.35f, Color.black);
        btnTxt.raycastTarget = false;

        return btn;
    }

    private static GameObject CreateDividerWing(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool flipHorizontal)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        if (flipHorizontal)
        {
            rt.localScale = new Vector3(-1f, 1f, 1f);
        }

        var img = go.GetComponent<Image>();
        img.sprite = GetDividerSprite();
        img.color = color;
        img.raycastTarget = false;

        return go;
    }

    private static GameObject CreateOrnamentDivider(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Left half
        var left = new GameObject("Left", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        left.layer = 5;
        left.transform.SetParent(go.transform, false);
        var leftRT = left.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.5f, 1f);
        leftRT.offsetMin = leftRT.offsetMax = Vector2.zero;
        var leftImg = left.GetComponent<Image>();
        leftImg.sprite = GetDividerSprite();
        leftImg.color = color;
        leftImg.raycastTarget = false;

        // Right half (flipped)
        var right = new GameObject("Right", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        right.layer = 5;
        right.transform.SetParent(go.transform, false);
        var rightRT = right.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.5f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = rightRT.offsetMax = Vector2.zero;
        rightRT.localScale = new Vector3(-1f, 1f, 1f);
        var rightImg = right.GetComponent<Image>();
        rightImg.sprite = GetDividerSprite();
        rightImg.color = color;
        rightImg.raycastTarget = false;

        return go;
    }
}
