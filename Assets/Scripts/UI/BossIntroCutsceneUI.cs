using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Soul Knight Style Boss Introduction Cutscene
/// Màn Giới Thiệu Boss Điện Ảnh Phong Cách Soul Knight:
/// 1. Vừa bước vào phòng/triệu hồi: Cửa sập khóa chặt, khóa nhẹ nút di chuyển của người chơi.
/// 2. Hai dải đen điện ảnh (Letterbox) trượt từ mép trên & dưới vào.
/// 3. Camera lia mượt sang Boss và Zoom cận cảnh (Cinematic Close-Up).
/// 4. Boss gầm rú nện đất tạo sóng xung kích chấn động (Shockwave + Rung nhẹ).
/// 5. Thẻ Tên Boss phong cách Soul Knight dập sầm vào giữa màn hình (Avatar Chân Dung + Tên Boss + Danh hiệu + Nguy hiểm).
/// 6. Trả lại góc nhìn người chơi, Thanh Máu Boss trượt xuống và nạp từ 0% lên 100% rực lửa.
/// 7. Chữ "CHIẾN ĐẤU! (FIGHT!)" lóe sáng và bắt đầu trận đấu!
/// 8. Hỗ trợ chạm màn hình hoặc bấm Space để Bỏ Qua (Skip) tức thì.
/// </summary>
public class BossIntroCutsceneUI : MonoBehaviour
{
    public static BossIntroCutsceneUI Instance { get; private set; }

    public static bool IsCutsceneActive { get; private set; } = false;

    [System.Serializable]
    public class BossIntroData
    {
        public string bossName;
        public string bossTitle;
        public string threatLevel;
        public Sprite bossAvatar;
        public Color themeColor = new Color(1f, 0.25f, 0.25f);
        public Color titleColor = new Color(1f, 0.88f, 0.35f);
        public System.Action onFinished;
    }

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform topLetterbox;
    private RectTransform bottomLetterbox;
    private RectTransform cardContainer;
    private CanvasGroup cardCanvasGroup;

    // Boss Avatar Components
    private RectTransform avatarRoot;
    private Image avatarImg;
    private Image avatarGlow;
    private RectTransform infoContainer;

    // Texts
    private TextMeshProUGUI threatText;
    private TextMeshProUGUI bossNameText;
    private TextMeshProUGUI bossTitleText;
    private TextMeshProUGUI fightText;
    private TextMeshProUGUI skipPromptText;

    private Coroutine activeRoutine;
    private bool skipRequested = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        IsCutsceneActive = false;
    }

    void Update()
    {
        if (!IsCutsceneActive) return;

        // Cho phép người chơi chạm màn hình hoặc bấm Space/Enter/Escape để bỏ qua cutscene ngay lập tức
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
        {
            skipRequested = true;
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            skipRequested = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
    }

    private static GameObject CreateUIGameObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }
        return go;
    }

    public static BossIntroCutsceneUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        var existing = Object.FindAnyObjectByType<BossIntroCutsceneUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        Canvas parentCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (parentCanvas == null || !parentCanvas.gameObject.activeInHierarchy)
        {
            GameObject cObj = GameObject.Find("BossCanvas");
            if (cObj != null) parentCanvas = cObj.GetComponent<Canvas>();
        }

        if (parentCanvas == null)
        {
            GameObject cObj = new GameObject("BossIntroCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            parentCanvas = cObj.GetComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parentCanvas.sortingOrder = 9500;
            var scaler = cObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        GameObject root = CreateUIGameObject("BossIntroCutsceneUI", parentCanvas.transform);

        var ui = root.AddComponent<BossIntroCutsceneUI>();
        ui.BuildUIStructure(root, parentCanvas);
        Instance = ui;
        return ui;
    }

    private void BuildUIStructure(GameObject root, Canvas parentCanvas)
    {
        canvas = parentCanvas;
        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform rootRt = root.GetComponent<RectTransform>();
        if (rootRt != null)
        {
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
        }

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/VaelstriderFont") 
            ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
            ?? TMP_Settings.defaultFontAsset;

        // 1. TOP LETTERBOX BAR
        GameObject topBarObj = CreateUIGameObject("TopLetterbox", root.transform);
        topLetterbox = topBarObj.GetComponent<RectTransform>();
        topLetterbox.anchorMin = new Vector2(0f, 1f);
        topLetterbox.anchorMax = new Vector2(1f, 1f);
        topLetterbox.pivot = new Vector2(0.5f, 1f);
        topLetterbox.sizeDelta = new Vector2(0f, 120f);
        topLetterbox.anchoredPosition = new Vector2(0f, 130f); // Hidden initially above
        var topImg = topBarObj.AddComponent<Image>();
        topImg.color = new Color(0.04f, 0.04f, 0.05f, 0.98f);

        // 2. BOTTOM LETTERBOX BAR
        GameObject bottomBarObj = CreateUIGameObject("BottomLetterbox", root.transform);
        bottomLetterbox = bottomBarObj.GetComponent<RectTransform>();
        bottomLetterbox.anchorMin = new Vector2(0f, 0f);
        bottomLetterbox.anchorMax = new Vector2(1f, 0f);
        bottomLetterbox.pivot = new Vector2(0.5f, 0f);
        bottomLetterbox.sizeDelta = new Vector2(0f, 120f);
        bottomLetterbox.anchoredPosition = new Vector2(0f, -130f); // Hidden initially below
        var btmImg = bottomBarObj.AddComponent<Image>();
        btmImg.color = new Color(0.04f, 0.04f, 0.05f, 0.98f);

        // 3. CENTER BOSS SPLASH CARD CONTAINER (SOUL KNIGHT STYLE)
        GameObject cardObj = CreateUIGameObject("BossSplashCard", root.transform);
        cardContainer = cardObj.GetComponent<RectTransform>();
        cardContainer.anchorMin = new Vector2(0f, 0.5f);
        cardContainer.anchorMax = new Vector2(1f, 0.5f);
        cardContainer.pivot = new Vector2(0.5f, 0.5f);
        cardContainer.sizeDelta = new Vector2(0f, 230f);
        cardContainer.anchoredPosition = Vector2.zero;

        cardCanvasGroup = cardObj.AddComponent<CanvasGroup>();
        cardCanvasGroup.alpha = 0f;

        // Card Dark Obsidian / Bloodstone Grunge Banner
        var cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.05f, 0.02f, 0.03f, 0.95f);

        // Top Gold Border Line
        GameObject topLine = CreateUIGameObject("TopBorder", cardObj.transform);
        var tlRt = topLine.GetComponent<RectTransform>();
        tlRt.anchorMin = new Vector2(0f, 1f);
        tlRt.anchorMax = new Vector2(1f, 1f);
        tlRt.pivot = new Vector2(0.5f, 1f);
        tlRt.sizeDelta = new Vector2(0f, 3f);
        tlRt.anchoredPosition = Vector2.zero;
        topLine.AddComponent<Image>().color = new Color(1f, 0.78f, 0.22f, 0.9f);

        // Bottom Gold Border Line
        GameObject btmLine = CreateUIGameObject("BottomBorder", cardObj.transform);
        var blRt = btmLine.GetComponent<RectTransform>();
        blRt.anchorMin = new Vector2(0f, 0f);
        blRt.anchorMax = new Vector2(1f, 0f);
        blRt.pivot = new Vector2(0.5f, 0f);
        blRt.sizeDelta = new Vector2(0f, 3f);
        blRt.anchoredPosition = Vector2.zero;
        btmLine.AddComponent<Image>().color = new Color(1f, 0.78f, 0.22f, 0.9f);

        // Content Wrapper centered horizontally
        GameObject contentWrapper = CreateUIGameObject("CardContentWrapper", cardObj.transform);
        var cwRt = contentWrapper.GetComponent<RectTransform>();
        cwRt.anchorMin = new Vector2(0.5f, 0.5f);
        cwRt.anchorMax = new Vector2(0.5f, 0.5f);
        cwRt.pivot = new Vector2(0.5f, 0.5f);
        cwRt.sizeDelta = new Vector2(1100f, 220f);
        cwRt.anchoredPosition = Vector2.zero;

        // 3.A. BOSS AVATAR / PORTRAIT FRAME (LEFT SIDE)
        GameObject avatarRootObj = CreateUIGameObject("BossAvatarRoot", contentWrapper.transform);
        avatarRoot = avatarRootObj.GetComponent<RectTransform>();
        avatarRoot.anchorMin = new Vector2(0.5f, 0.5f);
        avatarRoot.anchorMax = new Vector2(0.5f, 0.5f);
        avatarRoot.pivot = new Vector2(0.5f, 0.5f);
        avatarRoot.sizeDelta = new Vector2(176f, 176f);
        avatarRoot.anchoredPosition = new Vector2(-360f, 0f);

        // Aura / Glow behind portrait
        GameObject glowObj = CreateUIGameObject("AvatarGlow", avatarRootObj.transform);
        var glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchorMin = new Vector2(0.5f, 0.5f);
        glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.pivot = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(210f, 210f);
        glowRt.anchoredPosition = Vector2.zero;
        avatarGlow = glowObj.AddComponent<Image>();
        var circleSprite = Resources.Load<Sprite>("Combat/circle_fill");
        if (circleSprite != null) avatarGlow.sprite = circleSprite;
        avatarGlow.color = new Color(1f, 0.35f, 0.15f, 0.35f);

        // Dark Portrait Backing
        GameObject frameBgObj = CreateUIGameObject("AvatarBg", avatarRootObj.transform);
        var fbgRt = frameBgObj.GetComponent<RectTransform>();
        fbgRt.anchorMin = Vector2.zero;
        fbgRt.anchorMax = Vector2.one;
        fbgRt.offsetMin = Vector2.zero;
        fbgRt.offsetMax = Vector2.zero;
        var bgImg = frameBgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.05f, 0.09f, 0.95f);

        // Metallic Ornamental Borders (Top, Bottom, Left, Right)
        Color goldBorderColor = new Color(1f, 0.82f, 0.28f, 0.92f);
        CreateFrameLine(avatarRootObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 3f), Vector2.zero, goldBorderColor);
        CreateFrameLine(avatarRootObj.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), Vector2.zero, goldBorderColor);
        CreateFrameLine(avatarRootObj.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), Vector2.zero, goldBorderColor);
        CreateFrameLine(avatarRootObj.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(3f, 0f), Vector2.zero, goldBorderColor);

        // 4 Corner Gold Studs
        Color cornerColor = new Color(1f, 0.92f, 0.55f, 1f);
        CreateCornerStud(avatarRootObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, -1f), cornerColor);
        CreateCornerStud(avatarRootObj.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), cornerColor);
        CreateCornerStud(avatarRootObj.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), cornerColor);
        CreateCornerStud(avatarRootObj.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-1f, 1f), cornerColor);

        // The Boss Avatar Sprite Image
        GameObject avatarImgObj = CreateUIGameObject("AvatarImage", avatarRootObj.transform);
        var aImgRt = avatarImgObj.GetComponent<RectTransform>();
        aImgRt.anchorMin = new Vector2(0.5f, 0.5f);
        aImgRt.anchorMax = new Vector2(0.5f, 0.5f);
        aImgRt.pivot = new Vector2(0.5f, 0.5f);
        aImgRt.sizeDelta = new Vector2(148f, 148f);
        aImgRt.anchoredPosition = Vector2.zero;
        avatarImg = avatarImgObj.AddComponent<Image>();
        avatarImg.preserveAspect = true;

        // 3.B. BOSS INFO CONTAINER (RIGHT SIDE)
        GameObject infoObj = CreateUIGameObject("BossInfoContainer", contentWrapper.transform);
        infoContainer = infoObj.GetComponent<RectTransform>();
        infoContainer.anchorMin = new Vector2(0.5f, 0.5f);
        infoContainer.anchorMax = new Vector2(0.5f, 0.5f);
        infoContainer.pivot = new Vector2(0f, 0.5f);
        infoContainer.sizeDelta = new Vector2(740f, 200f);
        infoContainer.anchoredPosition = new Vector2(-235f, 0f);

        // 3.B.1. Threat Level Tag Text (Top)
        GameObject threatObj = CreateUIGameObject("ThreatLevelText", infoObj.transform);
        var thRt = threatObj.GetComponent<RectTransform>();
        thRt.anchorMin = new Vector2(0f, 1f);
        thRt.anchorMax = new Vector2(1f, 1f);
        thRt.pivot = new Vector2(0f, 1f);
        thRt.sizeDelta = new Vector2(0f, 32f);
        thRt.anchoredPosition = new Vector2(0f, -14f);

        threatText = threatObj.AddComponent<TextMeshProUGUI>();
        if (font != null) threatText.font = font;
        threatText.fontSize = 18;
        threatText.fontStyle = FontStyles.Bold;
        threatText.alignment = TextAlignmentOptions.Left;
        threatText.color = new Color(1f, 0.45f, 0.15f);
        threatText.text = "[!] CẤP ĐỘ NGUY HIỂM: THẢM HỌA TITAN [!]";

        // 3.B.2. Big Boss Name Text (Center)
        GameObject nameObj = CreateUIGameObject("BossNameText", infoObj.transform);
        var nmRt = nameObj.GetComponent<RectTransform>();
        nmRt.anchorMin = new Vector2(0f, 0.5f);
        nmRt.anchorMax = new Vector2(1f, 0.5f);
        nmRt.pivot = new Vector2(0f, 0.5f);
        nmRt.sizeDelta = new Vector2(0f, 76f);
        nmRt.anchoredPosition = new Vector2(0f, 6f);

        bossNameText = nameObj.AddComponent<TextMeshProUGUI>();
        if (font != null) bossNameText.font = font;
        bossNameText.fontSize = 52;
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.alignment = TextAlignmentOptions.Left;
        bossNameText.color = new Color(1f, 0.88f, 0.35f);
        bossNameText.text = "GOLIATH GIKK";

        // 3.B.3. Subtitle / Title Description (Bottom)
        GameObject titleObj = CreateUIGameObject("BossTitleText", infoObj.transform);
        var ttRt = titleObj.GetComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0f, 0f);
        ttRt.anchorMax = new Vector2(1f, 0f);
        ttRt.pivot = new Vector2(0f, 0f);
        ttRt.sizeDelta = new Vector2(0f, 34f);
        ttRt.anchoredPosition = new Vector2(0f, 16f);

        bossTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) bossTitleText.font = font;
        bossTitleText.fontSize = 20;
        bossTitleText.fontStyle = FontStyles.Italic | FontStyles.Bold;
        bossTitleText.alignment = TextAlignmentOptions.Left;
        bossTitleText.color = new Color(0.85f, 0.90f, 0.95f);
        bossTitleText.text = "[ PHÁO ĐÀI BÁNH XE CỔ ĐẠI // THE ANCIENT TITAN WHEEL ]";

        // 4. "FIGHT! / CHIẾN ĐẤU!" FLASH TEXT
        GameObject fightObj = CreateUIGameObject("FightBannerText", root.transform);
        var fgRt = fightObj.GetComponent<RectTransform>();
        fgRt.anchorMin = new Vector2(0.5f, 0.5f);
        fgRt.anchorMax = new Vector2(0.5f, 0.5f);
        fgRt.pivot = new Vector2(0.5f, 0.5f);
        fgRt.sizeDelta = new Vector2(800f, 120f);
        fgRt.anchoredPosition = Vector2.zero;

        fightText = fightObj.AddComponent<TextMeshProUGUI>();
        if (font != null) fightText.font = font;
        fightText.fontSize = 68;
        fightText.fontStyle = FontStyles.Bold;
        fightText.alignment = TextAlignmentOptions.Center;
        fightText.color = new Color(1f, 0.85f, 0.2f);
        fightText.text = LocalizationManager.T("CHIẾN ĐẤU!", "FIGHT!");
        fightObj.SetActive(false);

        // 5. SKIP PROMPT TEXT
        GameObject skipObj = CreateUIGameObject("SkipPromptText", root.transform);
        var skRt = skipObj.GetComponent<RectTransform>();
        skRt.anchorMin = new Vector2(1f, 0f);
        skRt.anchorMax = new Vector2(1f, 0f);
        skRt.pivot = new Vector2(1f, 0f);
        skRt.sizeDelta = new Vector2(350f, 40f);
        skRt.anchoredPosition = new Vector2(-25f, 20f);

        skipPromptText = skipObj.AddComponent<TextMeshProUGUI>();
        if (font != null) skipPromptText.font = font;
        skipPromptText.fontSize = 15;
        skipPromptText.alignment = TextAlignmentOptions.Right;
        skipPromptText.color = new Color(1f, 1f, 1f, 0.45f);
        skipPromptText.text = LocalizationManager.T("Chạm để bỏ qua [TAP TO SKIP]", "Tap anywhere to skip");
    }

    private static void CreateFrameLine(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Vector2 pos, Color col)
    {
        GameObject line = CreateUIGameObject("FrameLine", parent);
        var rt = line.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        line.AddComponent<Image>().color = col;
    }

    private static void CreateCornerStud(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 offset, Color col)
    {
        GameObject stud = CreateUIGameObject("CornerStud", parent);
        var rt = stud.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(8f, 8f);
        rt.anchoredPosition = offset;
        stud.AddComponent<Image>().color = col;
    }

    public static void PlayGoliathGikk(Transform bossTransform, System.Action onFinished = null)
    {
        Sprite avatar = null;
        if (bossTransform != null)
        {
            var sr = bossTransform.GetComponent<SpriteRenderer>() ?? bossTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) avatar = sr.sprite;
        }
        if (avatar == null) avatar = Resources.Load<Sprite>("Enemies/Map1/BotWheel");

        var data = new BossIntroData
        {
            bossName = "GOLIATH GIKK",
            bossTitle = LocalizationManager.T("[ PHÁO ĐÀI BÁNH XE CỔ ĐẠI // THE ANCIENT TITAN WHEEL ]", "[ THE ANCIENT TITAN WHEEL // RUIN COLOSSUS ]"),
            threatLevel = LocalizationManager.T("[!] CẤP ĐỘ NGUY HIỂM: THẢM HỌA TITAN [!]", "[!] THREAT LEVEL: CALAMITY TITAN [!]"),
            bossAvatar = avatar,
            themeColor = new Color(1f, 0.45f, 0.15f),
            titleColor = new Color(1f, 0.88f, 0.35f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlayGorMok(Transform bossTransform, System.Action onFinished = null)
    {
        Sprite avatar = null;
        if (bossTransform != null)
        {
            var sr = bossTransform.GetComponent<SpriteRenderer>() ?? bossTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) avatar = sr.sprite;
        }
        if (avatar == null) avatar = Resources.Load<Sprite>("Enemies/Map2/BloodMonster/BloodMonster_0");

        var data = new BossIntroData
        {
            bossName = "GOR'MOK",
            bossTitle = LocalizationManager.T("[ HUYẾT THÚ NGUYÊN SINH CỔ THỤ // PRIMAL BLOOD BEAST ]", "[ PRIMAL BLOOD BEAST // ANCIENT TERROR ]"),
            threatLevel = LocalizationManager.T("[!] CẤP ĐỘ NGUY HIỂM: HUYẾT THẦN TẾ LỄ [!]", "[!] THREAT LEVEL: PRIMAL BLOOD TITAN [!]"),
            bossAvatar = avatar,
            themeColor = new Color(1f, 0.18f, 0.22f),
            titleColor = new Color(1f, 0.45f, 0.45f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlaySkeletonKing(Transform bossTransform, System.Action onFinished = null)
    {
        Sprite avatar = null;
        if (bossTransform != null)
        {
            var sr = bossTransform.GetComponent<SpriteRenderer>() ?? bossTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) avatar = sr.sprite;
        }
        if (avatar == null) avatar = Resources.Load<Sprite>("Enemies/Map3/SkeletonKing_Walk_0");

        var data = new BossIntroData
        {
            bossName = "DEATHLORD VALTHOR",
            bossTitle = LocalizationManager.T("[ VUA XƯƠNG CỔ ĐẠI TÀN TÍCH // SKELETON KING ]", "[ SKELETON KING OF THE FORGOTTEN // DEATHLORD ]"),
            threatLevel = LocalizationManager.T("[!] CẤP ĐỘ NGUY HIỂM: BẠO CHÚA BĂNG GIÁ [!]", "[!] THREAT LEVEL: FROSTFALL TYRANT [!]"),
            bossAvatar = avatar,
            themeColor = new Color(0.25f, 0.75f, 1f),
            titleColor = new Color(0.85f, 0.95f, 1f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlayShadowDoppelganger(Transform bossTransform, System.Action onFinished = null)
    {
        Sprite avatar = null;
        if (bossTransform != null)
        {
            var sr = bossTransform.GetComponent<SpriteRenderer>() ?? bossTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) avatar = sr.sprite;
        }
        if (avatar == null)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var psr = player.GetComponentInChildren<SpriteRenderer>();
                if (psr != null) avatar = psr.sprite;
            }
        }

        var data = new BossIntroData
        {
            bossName = "SHADOW DOPPELGANGER",
            bossTitle = LocalizationManager.T("[ BẢN NGÃ HƯ KHÔNG PHẢN CHIẾU // VOID APPARITION ]", "[ VOID REFLECTION // TRUE APPARITION ]"),
            threatLevel = LocalizationManager.T("[!] MỐI ĐE DỌA TỐI THƯỢNG: ÁC MỘNG HƯ KHÔNG [!]", "[!] SUPREME THREAT: VOID NIGHTMARE [!]"),
            bossAvatar = avatar,
            themeColor = new Color(0.75f, 0.25f, 1f),
            titleColor = new Color(0.9f, 0.6f, 1f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlayValleyGolem(Transform bossTransform, System.Action onFinished = null)
    {
        Sprite avatar = null;
        if (bossTransform != null)
        {
            var sr = bossTransform.GetComponent<SpriteRenderer>() ?? bossTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) avatar = sr.sprite;
        }
        if (avatar == null) avatar = Resources.Load<Sprite>("Enemies/Map2/obj_63_79x89");

        var data = new BossIntroData
        {
            bossName = "VALENTHUS",
            bossTitle = LocalizationManager.T("[ CỰ THẠCH HỘ VỆ THẦN THỤ // ANCIENT VALLEY GOLEM ]", "[ ANCIENT VALLEY GOLEM // GUARDIAN COLOSSUS ]"),
            threatLevel = LocalizationManager.T("[!] CẤP ĐỘ NGUY HIỂM: THẠCH HỘ VỆ [!]", "[!] THREAT LEVEL: STONE GUARDIAN [!]"),
            bossAvatar = avatar,
            themeColor = new Color(0.35f, 0.85f, 0.45f),
            titleColor = new Color(0.75f, 1f, 0.75f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    /// <summary>
    /// Kích hoạt màn giới thiệu Boss phong cách Soul Knight đầy đủ.
    /// </summary>
    public static void PlayIntro(BossIntroData data, Transform bossTransform, System.Action onFinished = null)
    {
        var ui = EnsureInstance();
        if (ui == null)
        {
            onFinished?.Invoke();
            return;
        }

        if (ui.activeRoutine != null) ui.StopCoroutine(ui.activeRoutine);
        ui.activeRoutine = ui.StartCoroutine(ui.PlayIntroRoutine(data, bossTransform, onFinished));
    }

    private IEnumerator PlayIntroRoutine(BossIntroData data, Transform bossTransform, System.Action onFinished)
    {
        IsCutsceneActive = true;
        skipRequested = false;

        // 1. Khóa nhẹ nút di chuyển của người chơi và ngưng nhận sát thương
        var playerCtrl = Object.FindAnyObjectByType<PlayerController>();
        if (playerCtrl != null) playerCtrl.SetCanMove(false);

        // Tạm thời ẩn các UI vướng víu (Minimap, Joystick, QuickSlots)
        SetGameplayHUDVisible(false);

        // Nạp Avatar chân dung Boss
        Sprite avatarSprite = data.bossAvatar;
        if (avatarSprite == null && bossTransform != null)
        {
            var sr = bossTransform.GetComponent<SpriteRenderer>() ?? bossTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                avatarSprite = sr.sprite;
            }
        }

        // Fallback nạp theo tên boss nếu chưa có sprite
        if (avatarSprite == null)
        {
            string nameUpper = (data.bossName ?? "").ToUpper();
            if (nameUpper.Contains("GIKK") || nameUpper.Contains("WHEEL") || nameUpper.Contains("BOT"))
            {
                avatarSprite = Resources.Load<Sprite>("Enemies/Map1/BotWheel");
            }
            else if (nameUpper.Contains("GOR'MOK") || nameUpper.Contains("GORMOK") || nameUpper.Contains("BLOOD"))
            {
                avatarSprite = Resources.Load<Sprite>("Enemies/Map2/BloodMonster/BloodMonster_0");
            }
            else if (nameUpper.Contains("VALENTHUS") || nameUpper.Contains("GOLEM"))
            {
                avatarSprite = Resources.Load<Sprite>("Enemies/Map2/obj_63_79x89");
            }
            else if (nameUpper.Contains("VALTHOR") || nameUpper.Contains("SKELETON") || nameUpper.Contains("DEATHLORD"))
            {
                avatarSprite = Resources.Load<Sprite>("Enemies/Map3/SkeletonKing_Walk_0");
            }
            else if (nameUpper.Contains("SHADOW") || nameUpper.Contains("DOPPELGANGER"))
            {
                var player = Object.FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    var psr = player.GetComponentInChildren<SpriteRenderer>();
                    if (psr != null) avatarSprite = psr.sprite;
                }
            }
        }

        // Cập nhật Avatar UI
        if (avatarImg != null)
        {
            if (avatarSprite != null)
            {
                avatarImg.sprite = avatarSprite;
                if ((data.bossName ?? "").ToUpper().Contains("SHADOW"))
                {
                    avatarImg.color = new Color(0.45f, 0.15f, 0.75f, 0.95f);
                }
                else
                {
                    avatarImg.color = Color.white;
                }
                if (avatarRoot != null) avatarRoot.gameObject.SetActive(true);
                if (infoContainer != null) infoContainer.anchoredPosition = new Vector2(-235f, 0f);
            }
            else
            {
                if (avatarRoot != null) avatarRoot.gameObject.SetActive(false);
                if (infoContainer != null) infoContainer.anchoredPosition = new Vector2(-360f, 0f);
            }
        }

        if (avatarGlow != null)
        {
            avatarGlow.color = new Color(data.themeColor.r, data.themeColor.g, data.themeColor.b, 0.38f);
        }

        // Chuẩn bị dữ liệu hiển thị Text
        if (threatText != null)
        {
            threatText.text = string.IsNullOrEmpty(data.threatLevel) 
                ? (LocalizationManager.IsVietnamese ? "[!] CẤP ĐỘ NGUY HIỂM: THẢM HỌA TITAN [!]" : "[!] THREAT LEVEL: CALAMITY TITAN [!]")
                : data.threatLevel;
            threatText.color = data.themeColor;
        }
        if (bossNameText != null)
        {
            bossNameText.text = data.bossName;
            bossNameText.color = data.titleColor;
        }
        if (bossTitleText != null)
        {
            bossTitleText.text = data.bossTitle;
        }

        canvasGroup.alpha = 1f;
        fightText.gameObject.SetActive(false);
        skipPromptText.gameObject.SetActive(true);

        // 2. Slide In Letterbox Bars (0.35s)
        float elapsed = 0f;
        float letterboxDuration = 0.35f;
        while (elapsed < letterboxDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / letterboxDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(130f, 0f, smoothT));
            bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(-130f, 0f, smoothT));
            yield return null;
        }
        topLetterbox.anchoredPosition = Vector2.zero;
        bottomLetterbox.anchoredPosition = Vector2.zero;

        // 3. Camera lia sang Boss và Zoom cận cảnh
        var cam = CameraFollow.Instance ?? Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null && bossTransform != null)
        {
            cam.FocusOn(bossTransform, 3.8f);
        }

        // 4. Âm thanh cảnh báo Boss & Sóng xung kích chấn động đất
        SoundManager.EnsureInstance()?.PlayBossWarning();
        if (bossTransform != null)
        {
            SpawnGroundShockwave(bossTransform.position, data.themeColor);
        }

        // Chờ 0.25s để camera bắt trọn khoảnh khắc Boss
        elapsed = 0f;
        while (elapsed < 0.25f && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 5. Thẻ Tên Boss phong cách Soul Knight dập sầm vào giữa màn hình
        SoundManager.EnsureInstance()?.PlayTitanSlam();
        if (cam != null) cam.Shake(0.35f, 0.45f);

        cardCanvasGroup.alpha = 1f;
        cardContainer.localScale = Vector3.one * 1.35f;

        // Hiệu ứng dập nảy (Elastic slam down từ 1.35x xuống 1.0x)
        elapsed = 0f;
        float slamDuration = 0.22f;
        while (elapsed < slamDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slamDuration);
            // Elastic bounce
            float scale = Mathf.Lerp(1.35f, 1.0f, t);
            cardContainer.localScale = Vector3.one * scale;
            yield return null;
        }
        cardContainer.localScale = Vector3.one;

        // 6. Giữ lại banner để người chơi ngắm Boss & đọc tên Boss (~1.4s)
        elapsed = 0f;
        while (elapsed < 1.4f && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 7. Thu hồi thẻ Boss và trả lại góc nhìn
        elapsed = 0f;
        float fadeOutDuration = 0.25f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            cardCanvasGroup.alpha = 1f - t;
            topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 130f, t));
            bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -130f, t));
            yield return null;
        }
        cardCanvasGroup.alpha = 0f;
        skipPromptText.gameObject.SetActive(false);

        // Trả lại Camera về Player
        if (cam != null)
        {
            cam.RestoreFocus();
        }

        // 8. Tự động nạp đầy Thanh Máu Boss (0% -> 100%)
        var bossUI = BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance();
        if (bossUI != null)
        {
            bossUI.AnimateFillFromZero(0.7f);
        }

        // 9. Lóe sáng chữ "CHIẾN ĐẤU! (FIGHT!)"
        fightText.gameObject.SetActive(true);
        fightText.transform.localScale = Vector3.one * 1.25f;
        elapsed = 0f;
        float fightDuration = 0.45f;
        while (elapsed < fightDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fightDuration);
            fightText.transform.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, t);
            yield return null;
        }
        fightText.gameObject.SetActive(false);

        // 10. Hoàn tất cutscene: Khôi phục HUD & Trả quyền điều khiển cho người chơi
        SetGameplayHUDVisible(true);
        if (playerCtrl != null) playerCtrl.SetCanMove(true);

        canvasGroup.alpha = 0f;
        IsCutsceneActive = false;
        activeRoutine = null;

        onFinished?.Invoke();
    }

    private void SetGameplayHUDVisible(bool visible)
    {
        // Ẩn/Hiện Minimap
        var minimap = MinimapHUD.Instance ?? Object.FindAnyObjectByType<MinimapHUD>();
        if (minimap != null && minimap.gameObject.activeInHierarchy)
        {
            var cg = minimap.GetComponent<CanvasGroup>() ?? minimap.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
        }

        // Ẩn/Hiện Phím Lướt & Phím Đánh ảo trên Mobile
        var mobileBtns = Object.FindObjectsByType<MobileActionButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var mb in mobileBtns)
        {
            if (mb != null)
            {
                var cg = mb.GetComponent<CanvasGroup>() ?? mb.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = visible ? 1f : 0f;
            }
        }
    }

    private void SpawnGroundShockwave(Vector3 center, Color color)
    {
        GameObject waveObj = new GameObject("BossGroundShockwave");
        waveObj.transform.position = center + new Vector3(0, -0.2f, 0);

        var lr = waveObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 36;
        lr.startWidth = 0.25f;
        lr.endWidth = 0.25f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = new Color(color.r, color.g, color.b, 0f);

        for (int i = 0; i < 36; i++)
        {
            float rad = (i / 36f) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * 0.5f, Mathf.Sin(rad) * 0.35f, 0));
        }

        StartCoroutine(ShockwaveExpandRoutine(waveObj, lr));
    }

    private IEnumerator ShockwaveExpandRoutine(GameObject go, LineRenderer lr)
    {
        float elapsed = 0f;
        float duration = 0.65f;
        Vector3 initialScale = Vector3.one * 0.5f;
        Vector3 targetScale = Vector3.one * 5.5f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (go != null) go.transform.localScale = Vector3.Lerp(initialScale, targetScale, Mathf.Sqrt(t));
            if (lr != null)
            {
                Color c = lr.startColor;
                c.a = 1f - t;
                lr.startColor = c;
            }
            yield return null;
        }

        if (go != null) Destroy(go);
    }
}
