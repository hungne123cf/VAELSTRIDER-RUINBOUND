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
/// 5. THẺ TÊN BOSS PHONG CÁCH SOUL KNIGHT (Tilted Vibrant Banner + Huge Character Splash + 3D Block Text).
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
        public string bossName; // Tên hiển thị lớn 2 dòng trên banner (vd: "GOLIATH\nGIKK")
        public string displayName; // Tên 1 dòng ở dải dưới (vd: "GOLIATH GIKK")
        public string bossTitle; // Danh hiệu (vd: "[ PHÁO ĐÀI BÁNH XE CỔ ĐẠI // THE ANCIENT TITAN WHEEL ]")
        public string threatLevel; // Mức độ nguy hiểm (vd: "// CẤP ĐỘ NGUY HIỂM: THẢM HỌA TITAN //")
        public Sprite bossSplashSprite; // Splash art khổ lớn phong cách Soul Knight (có viền sticker trắng)
        public Color bannerColor = new Color(0.92f, 0.38f, 0.04f); // Màu nền chủ đạo của Boss
        public Color titleColor = new Color(1f, 0.88f, 0.35f);
        public System.Action onFinished;
    }

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform topLetterbox;
    private RectTransform bottomLetterbox;

    // Slanted Soul Knight Banner
    private RectTransform slantedBanner;
    private Image bannerBg;
    private CanvasGroup bannerCanvasGroup;

    // Boss Character Splash Art (Right Side)
    private RectTransform bossSplashRt;
    private Image bossSplashImg;

    // Giant 3D Block Boss Name (Left Side)
    private TextMeshProUGUI bigNameText;
    private TextMeshProUGUI bigNameShadowText;

    // Bottom Letterbox Information (Bottom-Left)
    private TextMeshProUGUI bottomNameText;
    private TextMeshProUGUI bottomTitleText;
    private TextMeshProUGUI bottomThreatText;

    // Flash & Prompts
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
            if (existing.topLetterbox == null || existing.fightText == null)
            {
                existing.BuildUIStructure(existing.gameObject, existing.GetComponentInParent<Canvas>());
            }
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
        topLetterbox.sizeDelta = new Vector2(0f, 125f);
        topLetterbox.anchoredPosition = new Vector2(0f, 135f); // Ẩn phía trên
        var topImg = topBarObj.AddComponent<Image>();
        topImg.color = new Color(0.04f, 0.04f, 0.05f, 0.98f);

        // 2. BOTTOM LETTERBOX BAR
        GameObject bottomBarObj = CreateUIGameObject("BottomLetterbox", root.transform);
        bottomLetterbox = bottomBarObj.GetComponent<RectTransform>();
        bottomLetterbox.anchorMin = new Vector2(0f, 0f);
        bottomLetterbox.anchorMax = new Vector2(1f, 0f);
        bottomLetterbox.pivot = new Vector2(0.5f, 0f);
        bottomLetterbox.sizeDelta = new Vector2(0f, 135f);
        bottomLetterbox.anchoredPosition = new Vector2(0f, -145f); // Ẩn phía dưới
        var btmImg = bottomBarObj.AddComponent<Image>();
        btmImg.color = new Color(0.04f, 0.04f, 0.05f, 0.98f);

        // 2.A. Thông tin hiển thị góc dưới trái của Bottom Letterbox (như Snowman King trong hình mẫu)
        GameObject bottomInfoObj = CreateUIGameObject("BottomInfoContainer", bottomBarObj.transform);
        var biRt = bottomInfoObj.GetComponent<RectTransform>();
        biRt.anchorMin = new Vector2(0f, 0.5f);
        biRt.anchorMax = new Vector2(0f, 0.5f);
        biRt.pivot = new Vector2(0f, 0.5f);
        biRt.sizeDelta = new Vector2(900f, 115f);
        biRt.anchoredPosition = new Vector2(65f, 0f);

        // Tên Boss 1 dòng ở dải dưới
        GameObject bNameObj = CreateUIGameObject("BottomNameText", bottomInfoObj.transform);
        var bnRt = bNameObj.GetComponent<RectTransform>();
        bnRt.anchorMin = new Vector2(0f, 1f);
        bnRt.anchorMax = new Vector2(1f, 1f);
        bnRt.pivot = new Vector2(0f, 1f);
        bnRt.sizeDelta = new Vector2(0f, 38f);
        bnRt.anchoredPosition = new Vector2(0f, -5f);

        bottomNameText = bNameObj.AddComponent<TextMeshProUGUI>();
        if (font != null) bottomNameText.font = font;
        bottomNameText.fontSize = 32;
        bottomNameText.fontStyle = FontStyles.Bold;
        bottomNameText.alignment = TextAlignmentOptions.Left;
        bottomNameText.color = Color.white;
        bottomNameText.text = "";

        // Danh hiệu Boss ở dải dưới
        GameObject bTitleObj = CreateUIGameObject("BottomTitleText", bottomInfoObj.transform);
        var btRt = bTitleObj.GetComponent<RectTransform>();
        btRt.anchorMin = new Vector2(0f, 0.5f);
        btRt.anchorMax = new Vector2(1f, 0.5f);
        btRt.pivot = new Vector2(0f, 0.5f);
        btRt.sizeDelta = new Vector2(0f, 28f);
        btRt.anchoredPosition = new Vector2(0f, -4f);

        bottomTitleText = bTitleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) bottomTitleText.font = font;
        bottomTitleText.fontSize = 19;
        bottomTitleText.fontStyle = FontStyles.Italic | FontStyles.Bold;
        bottomTitleText.alignment = TextAlignmentOptions.Left;
        bottomTitleText.color = new Color(1f, 0.85f, 0.35f);
        bottomTitleText.text = "";

        // Cấp độ nguy hiểm ở dải dưới
        GameObject bThreatObj = CreateUIGameObject("BottomThreatText", bottomInfoObj.transform);
        var bthRt = bThreatObj.GetComponent<RectTransform>();
        bthRt.anchorMin = new Vector2(0f, 0f);
        bthRt.anchorMax = new Vector2(1f, 0f);
        bthRt.pivot = new Vector2(0f, 0f);
        bthRt.sizeDelta = new Vector2(0f, 26f);
        bthRt.anchoredPosition = new Vector2(0f, 6f);

        bottomThreatText = bThreatObj.AddComponent<TextMeshProUGUI>();
        if (font != null) bottomThreatText.font = font;
        bottomThreatText.fontSize = 15;
        bottomThreatText.fontStyle = FontStyles.Bold;
        bottomThreatText.alignment = TextAlignmentOptions.Left;
        bottomThreatText.color = new Color(1f, 0.45f, 0.15f);
        bottomThreatText.text = "";

        // 3. SLANTED SOUL KNIGHT BANNER CONTAINER (GIỐNG HÌNH MẪU 100%)
        GameObject bannerObj = CreateUIGameObject("SlantedSoulKnightBanner", root.transform);
        slantedBanner = bannerObj.GetComponent<RectTransform>();
        slantedBanner.anchorMin = new Vector2(0f, 0.5f);
        slantedBanner.anchorMax = new Vector2(1f, 0.5f);
        slantedBanner.pivot = new Vector2(0.5f, 0.5f);
        slantedBanner.sizeDelta = new Vector2(300f, 330f); // Rộng hơn mép màn hình để xoay không bị lộ góc
        slantedBanner.anchoredPosition = new Vector2(0f, 25f);
        slantedBanner.localRotation = Quaternion.Euler(0f, 0f, -3.2f); // Góc nghiêng năng động Soul Knight

        bannerCanvasGroup = bannerObj.AddComponent<CanvasGroup>();
        bannerCanvasGroup.alpha = 0f;

        // Màu nền rực rỡ theo chủ đề từng Boss
        bannerBg = bannerObj.AddComponent<Image>();
        bannerBg.color = new Color(0.92f, 0.38f, 0.04f, 1f); // Mặc định Cam núi lửa của Goliath Gikk

        // Viền đen trên (Thick comic border)
        GameObject topBorder = CreateUIGameObject("BannerTopBorder", bannerObj.transform);
        var tbRt = topBorder.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 1f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.sizeDelta = new Vector2(0f, 5f);
        tbRt.anchoredPosition = Vector2.zero;
        topBorder.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.07f, 0.95f);

        // Viền đen dưới
        GameObject btmBorder = CreateUIGameObject("BannerBottomBorder", bannerObj.transform);
        var bbRt = btmBorder.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0f, 0f);
        bbRt.anchorMax = new Vector2(1f, 0f);
        bbRt.pivot = new Vector2(0.5f, 0f);
        bbRt.sizeDelta = new Vector2(0f, 5f);
        bbRt.anchoredPosition = Vector2.zero;
        btmBorder.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.07f, 0.95f);

        // Content Wrapper bên trong banner nghiêng (không bị méo layout)
        GameObject contentWrapper = CreateUIGameObject("BannerContentWrapper", bannerObj.transform);
        var cwRt = contentWrapper.GetComponent<RectTransform>();
        cwRt.anchorMin = new Vector2(0.5f, 0.5f);
        cwRt.anchorMax = new Vector2(0.5f, 0.5f);
        cwRt.pivot = new Vector2(0.5f, 0.5f);
        cwRt.sizeDelta = new Vector2(1650f, 330f);
        cwRt.anchoredPosition = Vector2.zero;

        // 3.A. TÊN BOSS KHỔNG LỒ 3D BLOCK (BÊN TRÁI BANNER - GIỐNG CHỮ "SNOWMAN KING")
        GameObject nameHolderObj = CreateUIGameObject("BigNameHolder", contentWrapper.transform);
        var nhRt = nameHolderObj.GetComponent<RectTransform>();
        nhRt.anchorMin = new Vector2(0.5f, 0.5f);
        nhRt.anchorMax = new Vector2(0.5f, 0.5f);
        nhRt.pivot = new Vector2(0.5f, 0.5f);
        nhRt.sizeDelta = new Vector2(850f, 260f);
        nhRt.anchoredPosition = new Vector2(-280f, 0f);

        // Lớp bóng đổ 3D đen dập xuống phía dưới-phải (3D Drop Shadow)
        GameObject shadowTextObj = CreateUIGameObject("BigNameShadow", nameHolderObj.transform);
        var stRt = shadowTextObj.GetComponent<RectTransform>();
        stRt.anchorMin = Vector2.zero;
        stRt.anchorMax = Vector2.one;
        stRt.offsetMin = Vector2.zero;
        stRt.offsetMax = Vector2.zero;
        stRt.anchoredPosition = new Vector2(7f, -7f); // Độ dời bóng 3D

        bigNameShadowText = shadowTextObj.AddComponent<TextMeshProUGUI>();
        if (font != null) bigNameShadowText.font = font;
        bigNameShadowText.fontSize = 80;
        bigNameShadowText.fontStyle = FontStyles.Bold;
        bigNameShadowText.alignment = TextAlignmentOptions.Left;
        bigNameShadowText.lineSpacing = -18f;
        bigNameShadowText.color = new Color(0.05f, 0.05f, 0.07f, 0.98f);
        bigNameShadowText.text = "GOLIATH\nGIKK";

        // Lớp chữ chính màu trắng sáng phía trên
        GameObject mainTextObj = CreateUIGameObject("BigNameMain", nameHolderObj.transform);
        var mtRt = mainTextObj.GetComponent<RectTransform>();
        mtRt.anchorMin = Vector2.zero;
        mtRt.anchorMax = Vector2.one;
        mtRt.offsetMin = Vector2.zero;
        mtRt.offsetMax = Vector2.zero;
        mtRt.anchoredPosition = Vector2.zero;

        bigNameText = mainTextObj.AddComponent<TextMeshProUGUI>();
        if (font != null) bigNameText.font = font;
        bigNameText.fontSize = 80;
        bigNameText.fontStyle = FontStyles.Bold;
        bigNameText.alignment = TextAlignmentOptions.Left;
        bigNameText.lineSpacing = -18f;
        bigNameText.color = Color.white;
        bigNameText.text = "GOLIATH\nGIKK";

        // 3.B. HÌNH ẢNH CHÂN DUNG BOSS KHỔNG LỒ (BÊN PHẢI BANNER - GIỐNG SNOWMAN KING TRONG ẢNH MẪU)
        GameObject splashObj = CreateUIGameObject("BossSplashArt", contentWrapper.transform);
        bossSplashRt = splashObj.GetComponent<RectTransform>();
        bossSplashRt.anchorMin = new Vector2(0.5f, 0.5f);
        bossSplashRt.anchorMax = new Vector2(0.5f, 0.5f);
        bossSplashRt.pivot = new Vector2(0.5f, 0.5f);
        bossSplashRt.sizeDelta = new Vector2(430f, 400f); // Kích thước khổng lồ tràn nhẹ khỏi viền banner
        bossSplashRt.anchoredPosition = new Vector2(430f, 15f);

        bossSplashImg = splashObj.AddComponent<Image>();
        bossSplashImg.preserveAspect = true;

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
        skipPromptText.text = LocalizationManager.T("Chạm để bỏ qua", "Tap anywhere to skip");
    }

    public static void PlayGoliathGikk(Transform bossTransform, System.Action onFinished = null)
    {
        var splash = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_GoliathGikk") 
                  ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_GoliathGikk");

        var data = new BossIntroData
        {
            bossName = LocalizationManager.T("PHÁO ĐÀI\nBÁNH XE", "GOLIATH\nGIKK"),
            displayName = LocalizationManager.T("PHÁO ĐÀI BÁNH XE CỔ ĐẠI", "GOLIATH GIKK"),
            bossTitle = LocalizationManager.T("CỖ MÁY HUYỀN THOẠI TÀN TÍCH", "THE ANCIENT TITAN WHEEL"),
            threatLevel = LocalizationManager.T("CẤP ĐỘ NGUY HIỂM: THẢM HỌA TITAN", "THREAT LEVEL: CALAMITY TITAN"),
            bossSplashSprite = splash,
            bannerColor = new Color(0.92f, 0.38f, 0.04f), // Cam lửa rực cháy
            titleColor = new Color(1f, 0.88f, 0.35f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlayGorMok(Transform bossTransform, System.Action onFinished = null)
    {
        var splash = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_GorMok") 
                  ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_GorMok");

        var data = new BossIntroData
        {
            bossName = LocalizationManager.T("HUYẾT THÚ\nGOR'MOK", "GOR'MOK\nPRIMAL"),
            displayName = LocalizationManager.T("HUYẾT THÚ GOR'MOK", "GOR'MOK"),
            bossTitle = LocalizationManager.T("BÁ CHỦ MÁU CỔ THỤ NGUYÊN SINH", "PRIMAL BLOOD BEAST"),
            threatLevel = LocalizationManager.T("CẤP ĐỘ NGUY HIỂM: HUYẾT THẦN TẾ LỄ", "THREAT LEVEL: PRIMAL BLOOD TITAN"),
            bossSplashSprite = splash,
            bannerColor = new Color(0.76f, 0.08f, 0.14f), // Đỏ máu nguyên sinh
            titleColor = new Color(1f, 0.45f, 0.45f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlaySkeletonKing(Transform bossTransform, System.Action onFinished = null)
    {
        var splash = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_SkeletonKing") 
                  ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_SkeletonKing");

        var data = new BossIntroData
        {
            bossName = LocalizationManager.T("CHÚA TỂ\nNHỆN CỔ", "DEATHLORD\nVALTHOR"),
            displayName = LocalizationManager.T("CHÚA TỂ NHỆN CỔ ĐẠI", "DEATHLORD VALTHOR"),
            bossTitle = LocalizationManager.T("VUA XƯƠNG CÕI LÃNG QUÊN", "SKELETON KING OF THE FORGOTTEN"),
            threatLevel = LocalizationManager.T("CẤP ĐỘ NGUY HIỂM: BẠO CHÚA BĂNG GIÁ", "THREAT LEVEL: FROSTFALL TYRANT"),
            bossSplashSprite = splash,
            bannerColor = new Color(0.01f, 0.52f, 0.78f), // Xanh băng tuyết Frostfall
            titleColor = new Color(0.85f, 0.95f, 1f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlayValleyGolem(Transform bossTransform, System.Action onFinished = null)
    {
        var splash = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_Valenthus") 
                  ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_Valenthus");

        var data = new BossIntroData
        {
            bossName = LocalizationManager.T("CỰ THẠCH\nVALENTHUS", "VALENTHUS\nGOLEM"),
            displayName = LocalizationManager.T("CỰ THẠCH HỘ VỆ THẦN THỤ", "VALENTHUS"),
            bossTitle = LocalizationManager.T("HỘ VỆ THẦN THỤ NGUYÊN BẢN", "ANCIENT VALLEY GOLEM"),
            threatLevel = LocalizationManager.T("CẤP ĐỘ NGUY HIỂM: THẠCH HỘ VỆ", "THREAT LEVEL: STONE GUARDIAN"),
            bossSplashSprite = splash,
            bannerColor = new Color(0.08f, 0.52f, 0.25f), // Xanh ngọc lục bảo tàn tích
            titleColor = new Color(0.75f, 1f, 0.75f)
        };
        PlayIntro(data, bossTransform, onFinished);
    }

    public static void PlayShadowDoppelganger(Transform bossTransform, System.Action onFinished = null)
    {
        var splash = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_ShadowDoppelganger");

        var data = new BossIntroData
        {
            bossName = LocalizationManager.T("BÓNG MA\nBẢN NGÃ", "SHADOW\nDOPPELGANGER"),
            displayName = LocalizationManager.T("BÓNG MA BẢN NGÃ", "SHADOW DOPPELGANGER"),
            bossTitle = LocalizationManager.T("BẢN NGÃ HƯ KHÔNG PHẢN CHIẾU", "VOID REFLECTION"),
            threatLevel = LocalizationManager.T("MỐI ĐE DỌA: ÁC MỘNG HƯ KHÔNG", "SUPREME THREAT: VOID NIGHTMARE"),
            bossSplashSprite = splash,
            bannerColor = new Color(0.48f, 0.12f, 0.78f), // Tím hư không huyền bí
            titleColor = new Color(0.9f, 0.6f, 1f)
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

    private IEnumerator SafetyWatchdogRoutine(float timeout)
    {
        yield return new WaitForSecondsRealtime(timeout);
        if (IsCutsceneActive)
        {
            Debug.LogWarning("[BossIntroCutsceneUI] Safety watchdog timeout fired! Force restoring player control and HUD.");
            ForceEndCutscene();
        }
    }

    public static void ForceEndCutscene()
    {
        IsCutsceneActive = false;
        var pCtrl = Object.FindAnyObjectByType<PlayerController>();
        if (pCtrl != null) pCtrl.SetCanMove(true);

        var cam = CameraFollow.Instance ?? Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.RestoreFocus();

        var ui = Instance;
        if (ui != null)
        {
            ui.SetGameplayHUDVisible(true);
            if (ui.canvasGroup != null) ui.canvasGroup.alpha = 0f;
            if (ui.bannerCanvasGroup != null) ui.bannerCanvasGroup.alpha = 0f;
            if (ui.fightText != null) ui.fightText.gameObject.SetActive(false);
            if (ui.skipPromptText != null) ui.skipPromptText.gameObject.SetActive(false);
            if (ui.activeRoutine != null)
            {
                ui.StopCoroutine(ui.activeRoutine);
                ui.activeRoutine = null;
            }
        }
    }

    private IEnumerator PlayIntroRoutine(BossIntroData data, Transform bossTransform, System.Action onFinished)
    {
        IsCutsceneActive = true;
        skipRequested = false;

        var playerCtrl = Object.FindAnyObjectByType<PlayerController>();
        Coroutine watchdog = StartCoroutine(SafetyWatchdogRoutine(3.5f));

        try
        {
            if (playerCtrl != null) playerCtrl.SetCanMove(false);
            SetGameplayHUDVisible(false);

            if (topLetterbox == null || bottomLetterbox == null || slantedBanner == null || fightText == null)
            {
                BuildUIStructure(gameObject, canvas ?? GetComponentInParent<Canvas>());
            }

            // Nạp Splash Art khổ lớn cho Boss
            Sprite splashSprite = data.bossSplashSprite;
            if (splashSprite == null)
            {
                string nameUpper = (data.bossName ?? "").ToUpper();
                if (nameUpper.Contains("GIKK") || nameUpper.Contains("WHEEL") || nameUpper.Contains("BOT"))
                {
                    splashSprite = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_GoliathGikk") 
                                ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_GoliathGikk");
                }
                else if (nameUpper.Contains("GOR'MOK") || nameUpper.Contains("GORMOK") || nameUpper.Contains("BLOOD"))
                {
                    splashSprite = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_GorMok") 
                                ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_GorMok");
                }
                else if (nameUpper.Contains("VALENTHUS") || nameUpper.Contains("GOLEM"))
                {
                    splashSprite = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_Valenthus") 
                                ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_Valenthus");
                }
                else if (nameUpper.Contains("VALTHOR") || nameUpper.Contains("SKELETON") || nameUpper.Contains("DEATHLORD") || nameUpper.Contains("NHỆN"))
                {
                    splashSprite = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_SkeletonKing") 
                                ?? Resources.Load<Sprite>("Enemies/BossAvatars/Avatar_SkeletonKing");
                }
                else if (nameUpper.Contains("SHADOW") || nameUpper.Contains("DOPPELGANGER"))
                {
                    splashSprite = Resources.Load<Sprite>("Enemies/BossPortraits/Splash_ShadowDoppelganger");
                }
            }

            // Cập nhật Banner Background Color
            if (bannerBg != null)
            {
                bannerBg.color = data.bannerColor;
            }

            // Gán hình Boss Splash Art
            if (bossSplashImg != null)
            {
                if (splashSprite != null)
                {
                    bossSplashImg.sprite = splashSprite;
                    bossSplashImg.color = Color.white;
                    bossSplashImg.gameObject.SetActive(true);
                }
                else
                {
                    bossSplashImg.gameObject.SetActive(false);
                }
            }

            // Chuẩn bị Text tên lớn 3D trên banner
            string bigName = data.bossName;
            if (bigNameText != null) bigNameText.text = bigName;
            if (bigNameShadowText != null) bigNameShadowText.text = bigName;

            // Chuẩn bị Text dải dưới (Bottom Letterbox)
            string displayName = !string.IsNullOrEmpty(data.displayName) ? data.displayName : data.bossName.Replace("\n", " ");
            if (bottomNameText != null) bottomNameText.text = displayName;
            if (bottomTitleText != null) bottomTitleText.text = data.bossTitle;
            if (bottomThreatText != null)
            {
                bottomThreatText.text = string.IsNullOrEmpty(data.threatLevel) 
                    ? (LocalizationManager.IsVietnamese ? "[!] CẤP ĐỘ NGUY HIỂM: THẢM HỌA TITAN [!]" : "[!] THREAT LEVEL: CALAMITY TITAN [!]")
                    : data.threatLevel;
                bottomThreatText.color = data.bannerColor;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (fightText != null) fightText.gameObject.SetActive(false);
            if (skipPromptText != null) skipPromptText.gameObject.SetActive(true);

            // 2. Slide In Letterbox Bars (0.35s)
            float elapsed = 0f;
            float letterboxDuration = 0.35f;
            while (elapsed < letterboxDuration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / letterboxDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (topLetterbox != null) topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(135f, 0f, smoothT));
                if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(-145f, 0f, smoothT));
                yield return null;
            }
            if (topLetterbox != null) topLetterbox.anchoredPosition = Vector2.zero;
            if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = Vector2.zero;

            // 3. Camera lia sang Boss và Zoom cận cảnh
            var cam = CameraFollow.Instance ?? Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null && bossTransform != null)
            {
                cam.FocusOn(bossTransform, 3.8f);
            }

            // 4. Âm thanh cảnh báo Boss & Sóng xung kích chấn động đất
            try { SoundManager.EnsureInstance()?.PlayBossWarning(); } catch { }
            if (bossTransform != null)
            {
                try { SpawnGroundShockwave(bossTransform.position, data.bannerColor); } catch { }
            }

            // Chờ 0.20s để camera bắt trọn khoảnh khắc Boss
            elapsed = 0f;
            while (elapsed < 0.20f && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 5. THẺ TÊN SOUL KNIGHT DẬP SẦM VÀO GIỮA MÀN HÌNH (SLAM IMPACT)
            try { SoundManager.EnsureInstance()?.PlayTitanSlam(); } catch { }
            if (cam != null) cam.Shake(0.38f, 0.45f);

            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f;
            if (slantedBanner != null) slantedBanner.localScale = Vector3.one * 1.35f;
            if (bossSplashRt != null) bossSplashRt.localScale = Vector3.one * 0.75f;

            // Hiệu ứng dập nảy (Elastic slam down từ 1.35x xuống 1.0x)
            elapsed = 0f;
            float slamDuration = 0.22f;
            while (elapsed < slamDuration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slamDuration);
                float scale = Mathf.Lerp(1.35f, 1.0f, t);
                if (slantedBanner != null) slantedBanner.localScale = Vector3.one * scale;

                if (bossSplashRt != null)
                {
                    float splashScale = Mathf.Lerp(0.75f, 1.0f, Mathf.Sqrt(t));
                    bossSplashRt.localScale = Vector3.one * splashScale;
                }
                yield return null;
            }
            if (slantedBanner != null) slantedBanner.localScale = Vector3.one;
            if (bossSplashRt != null) bossSplashRt.localScale = Vector3.one;

            // 6. Giữ lại banner để người chơi ngắm trọn vẹn Boss Splash Art (~1.5s)
            elapsed = 0f;
            while (elapsed < 1.5f && !skipRequested)
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
                if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f - t;
                if (topLetterbox != null) topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 135f, t));
                if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -145f, t));
                yield return null;
            }
            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 0f;
            if (skipPromptText != null) skipPromptText.gameObject.SetActive(false);

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
            if (fightText != null)
            {
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
            }
        }
        finally
        {
            if (watchdog != null) StopCoroutine(watchdog);
            // 10. Luôn luôn khôi phục HUD & Trả quyền điều khiển cho người chơi an toàn 100%
            SetGameplayHUDVisible(true);
            if (playerCtrl != null) playerCtrl.SetCanMove(true);

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            IsCutsceneActive = false;
            activeRoutine = null;

            onFinished?.Invoke();
        }
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
