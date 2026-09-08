using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Linq;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("UI Roots")]
    private Canvas mainCanvas;
    private TextMeshProUGUI shardsText;
    private TextMeshProUGUI potionsText;
    private TextMeshProUGUI heroNameText;
    private TextMeshProUGUI heroRoleText;
    private TextMeshProUGUI heroStatsText;
    private TextMeshProUGUI heroSkillText;
    private Image heroPreviewImage;

    // Modals
    private GameObject mapSelectModal;
    private GameObject heroRosterModal;
    private GameObject shopModal;
    private GameObject confirmResetModal;

    // Sprite references
    private Sprite[] maidIdleSprites;
    private Sprite[] vaelIdleSprites;
    private Coroutine heroAnimRoutine;

    public static MainMenuManager EnsureInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("MainMenuManager");
            Instance = go.AddComponent<MainMenuManager>();
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        CharacterManager.EnsureInstance();
        SoundManager.EnsureInstance()?.PlaySceneAmbience("MainMenuScene");

        LoadSprites();
        BuildMainMenuUI();
        RefreshHeroDisplay();
        RefreshCurrencies();

        LocalizationManager.OnLanguageChanged -= OnLangChanged;
        LocalizationManager.OnLanguageChanged += OnLangChanged;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= OnLangChanged;
    }

    private void OnLangChanged()
    {
        RefreshHeroDisplay();
        RefreshCurrencies();
    }

    private void LoadSprites()
    {
#if UNITY_EDITOR
        maidIdleSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player1/Maid Idle.png")
            ?.OfType<Sprite>().OrderBy(s => GetSpriteIndex(s.name)).ToArray();
        vaelIdleSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player/IDLE/idle_down.png")
            ?.OfType<Sprite>().OrderBy(s => GetSpriteIndex(s.name)).ToArray();
#endif
    }

    private int GetSpriteIndex(string name)
    {
        int idx = name.LastIndexOf('_');
        if (idx >= 0 && int.TryParse(name.Substring(idx + 1), out int val)) return val;
        return 0;
    }
    private void BuildMainMenuUI()
    {
        // 1. Canvas
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Background
        GameObject bg = CreatePanel(canvasObj.transform, "Background", new Color(0.06f, 0.07f, 0.10f, 1f));
        SetFullScreen(bg.GetComponent<RectTransform>());

        // 3. Top Bar
        BuildTopBar(canvasObj.transform);

        // 4. Hero Stage
        BuildHeroStage(canvasObj.transform);

        // 5. Bottom Navigation Bar
        BuildBottomNavBar(canvasObj.transform);

        // 6. Modals
        BuildMapSelectModal(canvasObj.transform);
        BuildHeroRosterModal(canvasObj.transform);
        BuildShopModal(canvasObj.transform);
        BuildConfirmResetModal(canvasObj.transform);
    }

        private void BuildTopBar(Transform parent)
    {
        GameObject topBar = CreatePanel(parent, "TopBar", new Color(0.09f, 0.10f, 0.15f, 0.85f));
        RectTransform rt = topBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 90f);

        // Lay ten da nhap tu Login (dong bo Player_Name va Player_Username)
        string uname = PlayerPrefs.GetString("Player_Name", PlayerPrefs.GetString("Player_Username", "Vaelstrider"));
        if (string.IsNullOrWhiteSpace(uname)) uname = "Vaelstrider";

        var pNameTmp = CreateTMP(topBar.transform, "PlayerName", uname.ToUpper(), 24, FontStyles.Bold, new Color(0.95f, 0.85f, 0.45f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(35f, 14f), new Vector2(350f, 38f), TextAlignmentOptions.Left);
        pNameTmp.rectTransform.pivot = new Vector2(0f, 0.5f);

        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;
        var pTitleTmp = CreateTMP(topBar.transform, "PlayerTitle", isVi ? "CHIEN BINH TAN TICH" : "RUIN WANDERER", 14, FontStyles.Normal, new Color(0.65f, 0.7f, 0.8f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(35f, -16f), new Vector2(350f, 28f), TextAlignmentOptions.Left);
        pTitleTmp.rectTransform.pivot = new Vector2(0f, 0.5f);

        shardsText = CreateTMP(topBar.transform, "ShardsText", "SHARDS: 0", 22, FontStyles.Bold, new Color(0.4f, 0.85f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 40f), TextAlignmentOptions.Center);

        potionsText = CreateTMP(topBar.transform, "PotionsText", "POTIONS: 3", 22, FontStyles.Bold, new Color(0.3f, 1f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 0f), new Vector2(200f, 40f), TextAlignmentOptions.Center);

        Button btnLogout = CreateButton(topBar.transform, "BtnLogout", isVi ? "DANG XUAT" : "LOGOUT", new Color(0.55f, 0.15f, 0.2f, 0.9f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(140f, 50f));
        btnLogout.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("LoginScene");
        });
    }

    private void BuildHeroStage(Transform parent)
    {
        GameObject stage = CreatePanel(parent, "HeroStage", Color.clear);
        RectTransform rt = stage.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 40f);
        rt.sizeDelta = new Vector2(1000f, 650f);

        GameObject glow = CreatePanel(stage.transform, "PedestalGlow", new Color(0.3f, 0.5f, 0.85f, 0.18f));
        RectTransform glowRt = glow.GetComponent<RectTransform>();
        glowRt.anchoredPosition = new Vector2(0f, -40f);
        glowRt.sizeDelta = new Vector2(360f, 80f);

        GameObject imgObj = new GameObject("HeroPreviewImage");
        imgObj.transform.SetParent(stage.transform, false);
        heroPreviewImage = imgObj.AddComponent<Image>();
        heroPreviewImage.preserveAspect = true;
        RectTransform imgRt = imgObj.GetComponent<RectTransform>();
        imgRt.anchoredPosition = new Vector2(0f, 60f);
        imgRt.sizeDelta = new Vector2(400f, 400f);

        heroNameText = CreateTMP(stage.transform, "HeroName", "VAELSTRIDER", 38, FontStyles.Bold, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(600f, 50f), TextAlignmentOptions.Center);

        heroRoleText = CreateTMP(stage.transform, "HeroRole", "Hiệp Sĩ Cân Bằng", 20, FontStyles.Italic, new Color(0.4f, 0.85f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(600f, 35f), TextAlignmentOptions.Center);

        heroStatsText = CreateTMP(stage.transform, "HeroStats", "HP: 100    ATK: 20    SPD: 5.0", 22, FontStyles.Bold, new Color(1f, 0.9f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -185f), new Vector2(700f, 40f), TextAlignmentOptions.Center);

        GameObject skillBox = CreatePanel(stage.transform, "SkillBox", new Color(0.12f, 0.14f, 0.22f, 0.85f));
        RectTransform sRt = skillBox.GetComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(0f, -250f);
        sRt.sizeDelta = new Vector2(750f, 80f);

        heroSkillText = CreateTMP(skillBox.transform, "HeroSkillText", "TUYỆT KỸ: ...", 17, FontStyles.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(710f, 70f), TextAlignmentOptions.Center);
    }

    private void BuildBottomNavBar(Transform parent)
    {
        GameObject navBar = CreatePanel(parent, "BottomNavBar", new Color(0.08f, 0.09f, 0.14f, 0.95f));
        RectTransform rt = navBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 25f);
        rt.sizeDelta = new Vector2(1000f, 95f);

        float w = 220f;
        float spacing = 20f;
        float startX = -((4 * w + 3 * spacing) * 0.5f) + w * 0.5f;

        Button btnPlay = CreateButton(navBar.transform, "BtnPlay", "⚔️ XUẤT TRẬN", new Color(0.85f, 0.65f, 0.15f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(startX, 0f), new Vector2(w, 65f));
        btnPlay.onClick.AddListener(() => OpenMapSelectModal());

        Button btnHero = CreateButton(navBar.transform, "BtnHero", "👥 NHÂN VẬT", new Color(0.2f, 0.45f, 0.75f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(startX + (w + spacing), 0f), new Vector2(w, 65f));
        btnHero.onClick.AddListener(() => OpenHeroRosterModal());

        Button btnShop = CreateButton(navBar.transform, "BtnShop", "🛒 CỬA HÀNG", new Color(0.25f, 0.65f, 0.45f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(startX + 2 * (w + spacing), 0f), new Vector2(w, 65f));
        btnShop.onClick.AddListener(() => OpenShopModal());

        Button btnSettings = CreateButton(navBar.transform, "BtnSettings", "⚙️ CÀI ĐẶT", new Color(0.4f, 0.42f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(startX + 3 * (w + spacing), 0f), new Vector2(w, 65f));
        btnSettings.onClick.AddListener(() =>
        {
            var settings = SettingsUI.GetOrCreate();
            if (settings != null) settings.ToggleSettings();
        });
    }
    private void BuildMapSelectModal(Transform parent)
    {
        mapSelectModal = CreatePanel(parent, "MapSelectModal", new Color(0.04f, 0.05f, 0.08f, 0.95f));
        SetFullScreen(mapSelectModal.GetComponent<RectTransform>());
        mapSelectModal.SetActive(false);

        CreateTMP(mapSelectModal.transform, "Title", "CHỌN KHU VỰC XUẤT TRẬN", 34, FontStyles.Bold, new Color(0.95f, 0.85f, 0.4f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(800f, 50f), TextAlignmentOptions.Center);

        // Map 1 Card
        GameObject card1 = CreatePanel(mapSelectModal.transform, "CardMap1", new Color(0.12f, 0.14f, 0.2f, 0.9f));
        RectTransform c1Rt = card1.GetComponent<RectTransform>();
        c1Rt.anchoredPosition = new Vector2(-280f, 0f);
        c1Rt.sizeDelta = new Vector2(480f, 480f);

        CreateTMP(card1.transform, "Map1Title", "HẦM NGỤC CỔ", 28, FontStyles.Bold, new Color(0.4f, 0.85f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 40f), TextAlignmentOptions.Center);
        CreateTMP(card1.transform, "Map1Subtitle", "Khu Phế Tích Tầng 1 • Độ Khó: DỄ", 16, FontStyles.Normal, new Color(0.7f, 0.75f, 0.85f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(400f, 30f), TextAlignmentOptions.Center);
        CreateTMP(card1.transform, "Map1Desc", "Phế tích hầm ngục u tối với 4 căn phòng cổ, cạm bẫy lửa, bão quái máy và Rương Kho Báu cổ đại.", 16, FontStyles.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 160f), TextAlignmentOptions.Center);

        Button btnEnter1 = CreateButton(card1.transform, "BtnEnter1", "TIẾN VÀO (MAP 1)", new Color(0.85f, 0.65f, 0.15f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(280f, 60f));
        btnEnter1.onClick.AddListener(() => { Time.timeScale = 1.0f; SceneManager.LoadScene("SampleScene"); });

        // Map 2 Card
        GameObject card2 = CreatePanel(mapSelectModal.transform, "CardMap2", new Color(0.12f, 0.16f, 0.18f, 0.9f));
        RectTransform c2Rt = card2.GetComponent<RectTransform>();
        c2Rt.anchoredPosition = new Vector2(280f, 0f);
        c2Rt.sizeDelta = new Vector2(480f, 480f);

        CreateTMP(card2.transform, "Map2Title", "THUNG LŨNG CỔ THỤ", 28, FontStyles.Bold, new Color(0.35f, 0.95f, 0.55f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 40f), TextAlignmentOptions.Center);
        CreateTMP(card2.transform, "Map2Subtitle", "Khu Thung Lũng Tầng 2 • Độ Khó: TRUNG BÌNH", 16, FontStyles.Normal, new Color(0.7f, 0.75f, 0.85f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(400f, 30f), TextAlignmentOptions.Center);
        CreateTMP(card2.transform, "Map2Desc", "Thiên nhiên rộng lớn bao quanh bởi những tán cây khổng lồ nghìn năm, Trụ Đá Cổ Phát Sáng và quái vật hung bạo.", 16, FontStyles.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 160f), TextAlignmentOptions.Center);

        Button btnEnter2 = CreateButton(card2.transform, "BtnEnter2", "TIẾN VÀO (MAP 2)", new Color(0.2f, 0.7f, 0.4f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(280f, 60f));
        btnEnter2.onClick.AddListener(() => { Time.timeScale = 1.0f; SceneManager.LoadScene("Map2_ValleyRuin"); });

        Button btnClose = CreateButton(mapSelectModal.transform, "BtnClose", "ĐÓNG", new Color(0.5f, 0.15f, 0.2f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(180f, 50f));
        btnClose.onClick.AddListener(() => mapSelectModal.SetActive(false));
    }

    private void BuildHeroRosterModal(Transform parent)
    {
        heroRosterModal = CreatePanel(parent, "HeroRosterModal", new Color(0.04f, 0.05f, 0.08f, 0.95f));
        SetFullScreen(heroRosterModal.GetComponent<RectTransform>());
        heroRosterModal.SetActive(false);

        CreateTMP(heroRosterModal.transform, "Title", "CHỌN ANH HÙNG XUẤT TRẬN", 34, FontStyles.Bold, new Color(0.95f, 0.85f, 0.4f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(800f, 50f), TextAlignmentOptions.Center);

        // Vaelstrider Card
        GameObject card1 = CreatePanel(heroRosterModal.transform, "CardVael", new Color(0.12f, 0.14f, 0.22f, 0.9f));
        RectTransform c1Rt = card1.GetComponent<RectTransform>();
        c1Rt.anchoredPosition = new Vector2(-280f, 0f);
        c1Rt.sizeDelta = new Vector2(460f, 520f);

        CreateTMP(card1.transform, "VaelTitle", "VAELSTRIDER", 28, FontStyles.Bold, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 40f), TextAlignmentOptions.Center);
        CreateTMP(card1.transform, "VaelRole", "Hiệp Sĩ Cân Bằng (Đã Mở Khóa)", 16, FontStyles.Normal, new Color(0.4f, 0.85f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(400f, 30f), TextAlignmentOptions.Center);
        CreateTMP(card1.transform, "VaelStats", "❤️ Máu: 100\n⚔️ Tấn Công: 20\n⚡ Tốc Độ: 5.0\n\n🌟 Tuyệt Kỹ: Bão Lốc Tàn Tích\n(Xoay kiếm hút quái & nổ hất văng)", 18, FontStyles.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(400f, 200f), TextAlignmentOptions.Center);

        Button btnSelectVael = CreateButton(card1.transform, "BtnSelectVael", "CHỌN", new Color(0.2f, 0.6f, 0.9f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(260f, 55f));
        btnSelectVael.onClick.AddListener(() =>
        {
            CharacterManager.Instance.SelectCharacter(CharacterManager.CHAR_VAELSTRIDER);
            RefreshHeroDisplay();
            heroRosterModal.SetActive(false);
        });

        // Maid Card
        GameObject card2 = CreatePanel(heroRosterModal.transform, "CardMaid", new Color(0.22f, 0.12f, 0.18f, 0.9f));
        RectTransform c2Rt = card2.GetComponent<RectTransform>();
        c2Rt.anchoredPosition = new Vector2(280f, 0f);
        c2Rt.sizeDelta = new Vector2(460f, 520f);

        CreateTMP(card2.transform, "MaidTitle", "MAID CHIẾN BINH", 28, FontStyles.Bold, new Color(1f, 0.45f, 0.65f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 40f), TextAlignmentOptions.Center);
        CreateTMP(card2.transform, "MaidRole", "Sát Thủ Hầu Gái (Tốc Độ & Bạo Kích)", 16, FontStyles.Normal, new Color(1f, 0.8f, 0.9f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(400f, 30f), TextAlignmentOptions.Center);
        CreateTMP(card2.transform, "MaidStats", "❤️ Máu: 85\n⚔️ Tấn Công: 25\n⚡ Tốc Độ: 5.8\n\n🌟 Tuyệt Kỹ: Quét Tan Kẻ Yếu\n(Chém nón diệt TỨC THÌ quái thường, choáng Boss)", 18, FontStyles.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(400f, 200f), TextAlignmentOptions.Center);

        Button btnSelectMaid = CreateButton(card2.transform, "BtnSelectMaid", "CHỌN", new Color(0.85f, 0.25f, 0.55f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(260f, 55f));
        btnSelectMaid.onClick.AddListener(() =>
        {
            if (CharacterManager.Instance.IsCharacterUnlocked(CharacterManager.CHAR_MAID))
            {
                CharacterManager.Instance.SelectCharacter(CharacterManager.CHAR_MAID);
                RefreshHeroDisplay();
                heroRosterModal.SetActive(false);
            }
            else
            {
                heroRosterModal.SetActive(false);
                OpenShopModal();
            }
        });

        Button btnClose = CreateButton(heroRosterModal.transform, "BtnClose", "ĐÓNG", new Color(0.5f, 0.15f, 0.2f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(180f, 50f));
        btnClose.onClick.AddListener(() => heroRosterModal.SetActive(false));
    }
    private void BuildShopModal(Transform parent)
    {
        shopModal = CreatePanel(parent, "ShopModal", new Color(0.04f, 0.05f, 0.08f, 0.95f));
        SetFullScreen(shopModal.GetComponent<RectTransform>());
        shopModal.SetActive(false);

        CreateTMP(shopModal.transform, "Title", "CỬA HÀNG CỔ ĐẠI (AETHEL SHOP)", 34, FontStyles.Bold, new Color(0.95f, 0.85f, 0.4f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(800f, 50f), TextAlignmentOptions.Center);

        // Item 1: Maid
        GameObject item1 = CreatePanel(shopModal.transform, "ShopItemMaid", new Color(0.15f, 0.13f, 0.22f, 0.9f));
        RectTransform i1Rt = item1.GetComponent<RectTransform>();
        i1Rt.anchoredPosition = new Vector2(0f, 120f);
        i1Rt.sizeDelta = new Vector2(750f, 100f);

        CreateTMP(item1.transform, "MaidShopName", "MỞ KHÓA: MAID CHIẾN BINH", 22, FontStyles.Bold, new Color(1f, 0.4f, 0.7f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 15f), new Vector2(400f, 35f), TextAlignmentOptions.Left);
        CreateTMP(item1.transform, "MaidShopDesc", "Tuyệt kỹ: Quét Tan Kẻ Yếu (Tiêu diệt tức thì quái thường)", 15, FontStyles.Normal, Color.white,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, -15f), new Vector2(400f, 30f), TextAlignmentOptions.Left);

        Button btnBuyMaid = CreateButton(item1.transform, "BtnBuyMaid", "💎 999 SHARDS", new Color(0.85f, 0.25f, 0.55f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 55f));
        btnBuyMaid.onClick.AddListener(() =>
        {
            if (CharacterManager.Instance.IsCharacterUnlocked(CharacterManager.CHAR_MAID)) return;
            if (CharacterManager.Instance.UnlockCharacter(CharacterManager.CHAR_MAID))
            {
                RefreshCurrencies();
                RefreshHeroDisplay();
                btnBuyMaid.GetComponentInChildren<TextMeshProUGUI>().text = "ĐÃ SỞ HỮU";
                btnBuyMaid.interactable = false;
            }
        });

        // Item 2: Potion
        GameObject item2 = CreatePanel(shopModal.transform, "ShopItemPotion", new Color(0.12f, 0.18f, 0.15f, 0.9f));
        RectTransform i2Rt = item2.GetComponent<RectTransform>();
        i2Rt.anchoredPosition = new Vector2(0f, 0f);
        i2Rt.sizeDelta = new Vector2(750f, 100f);

        CreateTMP(item2.transform, "PotionShopName", "MUA BÌNH MÁU HỒI PHỤC (+50 HP)", 22, FontStyles.Bold, new Color(0.3f, 1f, 0.6f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 15f), new Vector2(400f, 35f), TextAlignmentOptions.Left);
        CreateTMP(item2.transform, "PotionShopDesc", "Hồi 50 HP tức thì trong giao tranh (Tối đa 5 bình)", 15, FontStyles.Normal, Color.white,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, -15f), new Vector2(400f, 30f), TextAlignmentOptions.Left);

        Button btnBuyPotion = CreateButton(item2.transform, "BtnBuyPotion", "💎 25 SHARDS", new Color(0.2f, 0.7f, 0.4f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 55f));
        btnBuyPotion.onClick.AddListener(() =>
        {
            int shards = PlayerPrefs.GetInt("Save_AethelShards", 0);
            int potions = PlayerPrefs.GetInt("Save_HealthPotions", 3);
            if (shards >= 25 && potions < 5)
            {
                shards -= 25;
                potions += 1;
                PlayerPrefs.SetInt("Save_AethelShards", shards);
                PlayerPrefs.SetInt("Save_HealthPotions", potions);
                PlayerPrefs.Save();
                RefreshCurrencies();
                SoundManager.EnsureInstance()?.PlayPotionDrink();
            }
        });

        // Item 3: Reset Game (Hardcore / Thu thach nguyen ban)
        GameObject item3 = CreatePanel(shopModal.transform, "ShopItemReset", new Color(0.22f, 0.11f, 0.13f, 0.95f));
        RectTransform i3Rt = item3.GetComponent<RectTransform>();
        i3Rt.anchoredPosition = new Vector2(0f, -120f);
        i3Rt.sizeDelta = new Vector2(750f, 100f);

        CreateTMP(item3.transform, "ResetName", "💀 RESET GAME (NEW GAME+)", 22, FontStyles.Bold, new Color(1f, 0.35f, 0.38f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 15f), new Vector2(440f, 35f), TextAlignmentOptions.Left);
        CreateTMP(item3.transform, "ResetDesc", "Game quá dễ? Xóa sạch Tinh Thể, cấp Đền Thờ & Khóa Tướng để thử thách lại!", 14, FontStyles.Normal, new Color(0.9f, 0.85f, 0.88f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, -15f), new Vector2(440f, 30f), TextAlignmentOptions.Left);

        Button btnReset = CreateButton(item3.transform, "BtnResetGame", "💀 RESET GAME", new Color(0.75f, 0.16f, 0.20f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 55f));
        btnReset.onClick.AddListener(() =>
        {
            if (confirmResetModal != null) confirmResetModal.SetActive(true);
        });

        Button btnClose = CreateButton(shopModal.transform, "BtnClose", "ĐÓNG", new Color(0.5f, 0.15f, 0.2f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(180f, 50f));
        btnClose.onClick.AddListener(() => shopModal.SetActive(false));
    }


    private void BuildConfirmResetModal(Transform parent)
    {
        confirmResetModal = CreatePanel(parent, "ConfirmResetModal", new Color(0.02f, 0.02f, 0.04f, 0.96f));
        SetFullScreen(confirmResetModal.GetComponent<RectTransform>());

        GameObject box = CreatePanel(confirmResetModal.transform, "ConfirmBox", new Color(0.14f, 0.08f, 0.10f, 0.98f));
        RectTransform bRt = box.GetComponent<RectTransform>();
        bRt.anchoredPosition = Vector2.zero;
        bRt.sizeDelta = new Vector2(620f, 360f);

        CreateTMP(box.transform, "ConfirmTitle", "⚠️ XÁC NHẬN RESET TOÀN BỘ GAME", 24, FontStyles.Bold, new Color(1f, 0.3f, 0.35f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(560f, 40f), TextAlignmentOptions.Center);

        string warningMsg = "Bạn có chắc chắn muốn <b>TÁI SINH TỪ SỐ 0</b>?\n\n" +
            "• Tinh thể Aethel Shards sẽ về 0\n" +
            "• Tất cả 4 nhánh nâng cấp Đền Thờ bị xóa sạch\n" +
            "• Tướng Maid Chiến Binh sẽ bị khóa lại\n" +
            "• Số bình máu trở về mặc định (3 bình)\n\n" +
            "<color=#F87171>Hành động này không thể hoàn tác!</color>";

        CreateTMP(box.transform, "ConfirmDesc", warningMsg, 16, FontStyles.Normal, new Color(0.92f, 0.94f, 0.98f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(540f, 180f), TextAlignmentOptions.Center);

        Button btnAccept = CreateButton(box.transform, "BtnAcceptReset", "ĐỒNG Ý RESET", new Color(0.85f, 0.18f, 0.22f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, 45f), new Vector2(200f, 50f));
        btnAccept.onClick.AddListener(() =>
        {
            PlayerUpgradeManager.ResetEntireGameProgress();
            RefreshCurrencies();
            RefreshHeroDisplay();
            OpenShopModal();
            confirmResetModal.SetActive(false);
            SoundManager.EnsureInstance()?.PlayHitImpact();
        });

        Button btnCancel = CreateButton(box.transform, "BtnCancelReset", "HỦY BỎ", new Color(0.25f, 0.30f, 0.38f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(130f, 45f), new Vector2(200f, 50f));
        btnCancel.onClick.AddListener(() =>
        {
            confirmResetModal.SetActive(false);
        });

        confirmResetModal.SetActive(false);
    }

    public void OpenMapSelectModal()
    {
        heroRosterModal?.SetActive(false);
        shopModal?.SetActive(false);
        mapSelectModal?.SetActive(true);
    }

    public void OpenHeroRosterModal()
    {
        mapSelectModal?.SetActive(false);
        shopModal?.SetActive(false);
        heroRosterModal?.SetActive(true);

        var btnMaid = heroRosterModal.transform.Find("CardMaid/BtnSelectMaid");
        if (btnMaid != null)
        {
            var txt = btnMaid.GetComponentInChildren<TextMeshProUGUI>();
            bool isUnlocked = CharacterManager.Instance.IsCharacterUnlocked(CharacterManager.CHAR_MAID);
            bool isSelected = CharacterManager.Instance.GetSelectedCharacterId() == CharacterManager.CHAR_MAID;
            if (txt != null)
            {
                txt.text = isSelected ? "ĐANG CHỌN" : (isUnlocked ? "CHỌN TƯỚNG" : "MUA NGAY (999 💎)");
            }
        }
        var btnVael = heroRosterModal.transform.Find("CardVael/BtnSelectVael");
        if (btnVael != null)
        {
            var txt = btnVael.GetComponentInChildren<TextMeshProUGUI>();
            bool isSelected = CharacterManager.Instance.GetSelectedCharacterId() == CharacterManager.CHAR_VAELSTRIDER;
            if (txt != null)
            {
                txt.text = isSelected ? "ĐANG CHỌN" : "CHỌN TƯỚNG";
            }
        }
    }

    public void OpenShopModal()
    {
        mapSelectModal?.SetActive(false);
        heroRosterModal?.SetActive(false);
        shopModal?.SetActive(true);

        var btnBuyMaid = shopModal.transform.Find("ShopItemMaid/BtnBuyMaid");
        if (btnBuyMaid != null)
        {
            var txt = btnBuyMaid.GetComponentInChildren<TextMeshProUGUI>();
            var btn = btnBuyMaid.GetComponent<Button>();
            if (CharacterManager.Instance.IsCharacterUnlocked(CharacterManager.CHAR_MAID))
            {
                if (txt != null) txt.text = "ĐÃ SỞ HỮU";
                if (btn != null) btn.interactable = false;
            }
            else
            {
                if (txt != null) txt.text = "💎 999 SHARDS";
                if (btn != null) btn.interactable = true;
            }
        }
    }

    public void RefreshCurrencies()
    {
        int shards = PlayerPrefs.GetInt("Save_AethelShards", 0);
        int potions = PlayerPrefs.GetInt("Save_HealthPotions", 3);

        if (shardsText != null) shardsText.text = $"💎 {shards}";
        if (potionsText != null) potionsText.text = $"🧪 {potions}";
    }

        public void RefreshHeroDisplay()
    {
        var hero = CharacterManager.Instance.GetSelectedCharacter();
        if (hero == null) return;

        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (heroNameText != null) heroNameText.text = isVi ? hero.nameVN.ToUpper() : hero.nameEN.ToUpper();
        if (heroRoleText != null) heroRoleText.text = isVi ? hero.roleVN : hero.roleEN;
        if (heroStatsText != null)
        {
            heroStatsText.text = isVi
                ? $"MAU: {hero.baseHealth}    TAN CONG: {hero.baseAttack}    TOC DO: {hero.baseSpeed:F1}"
                : $"HP: {hero.baseHealth}    ATK: {hero.baseAttack}    SPD: {hero.baseSpeed:F1}";
        }
        if (heroSkillText != null)
        {
            string sName = isVi ? hero.skillNameVN : hero.skillNameEN;
            string sDesc = isVi ? hero.skillDescVN : hero.skillDescEN;
            heroSkillText.text = $"<b>{sName.ToUpper()}</b>: {sDesc}";
        }

        if (heroAnimRoutine != null)
        {
            StopCoroutine(heroAnimRoutine);
            heroAnimRoutine = null;
        }

        if (heroPreviewImage != null)
        {
            RectTransform imgRt = heroPreviewImage.rectTransform;

            if (hero.id == CharacterManager.CHAR_MAID)
            {
                // Kich thuoc Maid 32x32 can doi voi Player dau tien tren be dai
                imgRt.sizeDelta = new Vector2(115f, 185f);
                imgRt.anchoredPosition = new Vector2(0f, 45f);

                if (maidIdleSprites != null && maidIdleSprites.Length > 0)
                {
                    Sprite[] frames = maidIdleSprites.Skip(5).Take(5).ToArray();
                    if (frames.Length == 0) frames = maidIdleSprites;
                    heroPreviewImage.sprite = frames[0];
                    heroAnimRoutine = StartCoroutine(AnimateSpriteLoop(frames, 8f));
                }
            }
            else
            {
                // Vaelstrider: sprite 96x80 voi nhan vat o giua can doi chieu cao 185f
                imgRt.sizeDelta = new Vector2(220f, 185f);
                imgRt.anchoredPosition = new Vector2(0f, 45f);

                if (vaelIdleSprites != null && vaelIdleSprites.Length > 0)
                {
                    heroPreviewImage.sprite = vaelIdleSprites[0];
                    heroAnimRoutine = StartCoroutine(AnimateSpriteLoop(vaelIdleSprites, 8f));
                }
            }
        }
    }

    private IEnumerator AnimateSpriteLoop(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0 || heroPreviewImage == null) yield break;

        int idx = 0;
        float delay = 1f / fps;
        while (true)
        {
            if (heroPreviewImage != null)
            {
                heroPreviewImage.sprite = frames[idx];
            }
            idx = (idx + 1) % frames.Length;
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    private void SetFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        if (anchorMin.x == 0f && anchorMax.x == 0f) rt.pivot = new Vector2(0f, 0.5f);
        else if (anchorMin.x == 1f && anchorMax.x == 1f) rt.pivot = new Vector2(1f, 0.5f);
        else rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateButton(Transform parent, string name, string text, Color bgColor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

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
