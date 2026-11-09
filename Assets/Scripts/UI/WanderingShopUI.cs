using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Giao diện Tiệm Cổ Vật Thương Nhân Lãng Khách (Wandering Relic Merchant)
/// - Thiết kế đồng bộ hoàn hảo với hệ thống Lõi Nâng Cấp (LevelUpChoiceUI) chuẩn Dark Fantasy & TFT:
///   + Khung viền đá Obsidian và hoa văn Gothic hoàng kim rực rỡ
///   + Thẻ phẩm chất (Prismatic / Gold / Silver Relic) phân cấp rõ ràng
///   + Thẻ hệ sát thương (Physical, Magic, Crit, Lifesteal, Agility...) không có dấu ngoặc vuông "[ ]"
///   + Huy hiệu đá quý trung tâm (Runic Medallion) chứa Sprite đồ họa thật (Sword, Dash, Star, Hero, Ulti)
///   + Nút Mua (BUY BUTTON) hiển thị giá Mảnh Cổ Tự (Shards) kèm tiến độ nâng cấp (Tier 1/3)
///   + Hiệu ứng Hover tương tác phóng to mượt mà 1.035x và viền phát sáng
///   + Popup Toast thông báo nổi trực tiếp khi không đủ Mảnh Cổ Tự hoặc đã đạt cấp tối đa
/// </summary>
public class WanderingShopUI : MonoBehaviour
{
    private static WanderingShopUI instance;
    public static WanderingShopUI Instance => instance;

    public class ShopRelicItem
    {
        public string id;
        public string categoryVN;
        public string categoryEN;
        public Color badgeColor;
        public string titleVN;
        public string titleEN;
        public string descVN;
        public string descEN;
        public string flavorVN;
        public string flavorEN;
        public int cost;
        public System.Action onPurchase;
        public int purchaseCount = 0;
        public int maxPurchases = 3;
        public AugmentDatabase.AugmentData.Rarity rarity = AugmentDatabase.AugmentData.Rarity.Rare;
        public string iconName; // "Attack", "Dash", "Ulti", "Star", "Hero"
        public bool isInfinite = false;
        public string heroRequirement = null;
        public CalamityRealm? realmRequirement = null;
        public AugmentDatabase.AugmentType? augmentType = null;
    }

    // Catalog tất cả các vật phẩm cổ vật
    private static readonly List<ShopRelicItem> shopCatalog = new List<ShopRelicItem>();
    // 3 vật phẩm ngẫu nhiên cho lần ghé thăm hiện tại
    private static readonly List<ShopRelicItem> activeChoices = new List<ShopRelicItem>();

    [Header("UI References")]
    private TextMeshProUGUI headerTitleText;
    private TextMeshProUGUI headerSubtitleText;
    private TextMeshProUGUI shardsCounterText;
    private Transform cardContainer;
    private CanvasGroup canvasGroup;
    private bool isOpen = false;

    // Toast Notification Popup inside Shop Modal
    private GameObject toastObj;
    private TextMeshProUGUI toastText;
    private Outline toastOutline;
    private CanvasGroup toastCg;
    private Coroutine toastCoroutine;

    private readonly List<GameObject> cardObjects = new List<GameObject>();
    private readonly List<Button> buyButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> buyButtonTexts = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> progressTexts = new List<TextMeshProUGUI>();

    // Cache sprites
    private static Sprite _frameSprite;
    private static Sprite _dividerSprite;
    private static Sprite _stoneBtnSprite;
    private static Sprite _panelBgSprite;
    private static Sprite _runicRingSprite;
    private static Sprite _obsidianCoreSprite;
    private static Sprite _cachedAttackIcon;
    private static Sprite _cachedDashIcon;
    private static Sprite _cachedUltiIcon;
    private static Sprite _cachedStarIcon;
    private static Sprite _cachedHeroIcon;

    void Awake()
    {
        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        LocalizationManager.OnLanguageChanged += RefreshUI;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshUI;
    }

    // ─── Sprite Helpers (Đồng bộ với LevelUpChoiceUI & SoulslikeHUDStyler) ──────
    private static Sprite LoadSpriteFromDiskOrEditor(string relativePath)
    {
#if UNITY_EDITOR
        Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        if (s != null) return s;
#endif
        string diskPath = System.IO.Path.Combine(Application.dataPath, relativePath.Replace("Assets/", ""));
        if (System.IO.File.Exists(diskPath))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(diskPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
            {
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }
        return null;
    }

    private static Sprite GetFrameSprite()
    {
        if (_frameSprite != null) return _frameSprite;
        _frameSprite = Resources.Load<Sprite>("UI/frame_gothic_border");
        if (_frameSprite != null) return _frameSprite;

#if UNITY_EDITOR
        _frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/frame_gothic_border.png");
        if (_frameSprite != null) return _frameSprite;
#endif

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
        return _dividerSprite;
    }

    private static Sprite GetStoneBtnSprite()
    {
        if (_stoneBtnSprite != null) return _stoneBtnSprite;
        _stoneBtnSprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        return _stoneBtnSprite;
    }

    private static Sprite GetPanelBgSprite()
    {
        if (_panelBgSprite != null) return _panelBgSprite;
        _panelBgSprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        return _panelBgSprite;
    }

    private static Sprite GetRunicRingSprite()
    {
        if (_runicRingSprite != null) return _runicRingSprite;
        _runicRingSprite = SoulslikeHUDStyler.GetActionButtonRingSprite();
        return _runicRingSprite;
    }

    private static Sprite GetObsidianCoreSprite()
    {
        if (_obsidianCoreSprite != null) return _obsidianCoreSprite;
        _obsidianCoreSprite = SoulslikeHUDStyler.GetActionButtonCoreSprite();
        return _obsidianCoreSprite;
    }

    private static Sprite GetRelicSprite(ShopRelicItem item)
    {
        if (item == null) return null;

        if (item.augmentType.HasValue)
        {
            Sprite s = AugmentIconHelper.GetAugmentSprite(item.augmentType.Value);
            if (s != null) return s;
        }

        // Ưu tiên biểu tượng đồ họa riêng biệt từ AugmentIconHelper theo id của Cổ Vật
        Sprite spr = null;
        switch (item.id)
        {
            case "relic_physical":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.BonusAttack);
                break;
            case "relic_thunder":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.EchoStrike);
                break;
            case "relic_truedamage":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.LaceratingEdge);
                break;
            case "relic_crit":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.BonusCritChance);
                break;
            case "relic_lifesteal":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.BonusLifesteal);
                break;
            case "relic_elixir":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.RegenerativeCore);
                break;
            case "relic_swiftness":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.BonusCooldownReduction);
                break;
            case "relic_shield":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.BonusDefense);
                break;
            case "relic_abyssal":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.DemonicPact);
                break;
            case "relic_infinite_vitality":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.AbundantVitality);
                break;
            case "relic_infinite_power":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.BattleFrenzy);
                break;
            case "relic_infinite_shield":
                spr = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.DivineAegis);
                break;
        }

        if (spr != null) return spr;

        return AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.BonusAttack);
    }

    private static string GetStatShortCode(ShopRelicItem item)
    {
        if (item.augmentType.HasValue)
        {
            return AugmentIconHelper.GetAugmentShortCode(item.augmentType.Value);
        }

        string cat = (item.categoryEN ?? "").ToLower();
        if (cat.Contains("phys")) return "ATK";
        if (cat.Contains("magic") || cat.Contains("thunder")) return "AP";
        if (cat.Contains("crit")) return "CRIT";
        if (cat.Contains("true")) return "TRUE";
        if (cat.Contains("life") || cat.Contains("vamp")) return "VAMP";
        if (cat.Contains("vitality") || cat.Contains("health")) return "HP";
        if (cat.Contains("speed") || cat.Contains("agility")) return "SPD";
        if (cat.Contains("shield") || cat.Contains("defense")) return "DEF";
        return "RELIC";
    }

    // ─── Entry Point & Singleton Factory ───────────────────────────────────────
    public static WanderingShopUI EnsureUIExists()
    {
        if (instance != null) return instance;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("WanderingShopPanel");
        if (existing != null)
        {
            instance = existing.GetComponent<WanderingShopUI>();
            if (instance != null) return instance;
        }

        GameObject panelObj = new GameObject("WanderingShopPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.layer = 5;
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.015f, 0.020f, 0.030f, 0.88f); // Deep dark gothic vignette
        panelBg.raycastTarget = true;

        Canvas pCanvas = panelObj.AddComponent<Canvas>();
        pCanvas.overrideSorting = true;
        pCanvas.sortingOrder = 28500;
        panelObj.AddComponent<GraphicRaycaster>();

        instance = panelObj.AddComponent<WanderingShopUI>();
        instance.BuildUIHierarchy(panelObj.transform);
        instance.gameObject.SetActive(false);

        return instance;
    }

    // ─── Modal Construction (Dark Fantasy / TFT proportions) ───────────────────
    private void BuildUIHierarchy(Transform root)
    {
        // 1. Cửa sổ chính (Main Modal Window) - Phủ toàn màn hình để kích thước thẻ bài tương xứng tuyệt đối với LevelUpChoiceUI
        GameObject modalObj = new GameObject("ShopModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalObj.transform.SetParent(root, false);

        RectTransform modalRt = modalObj.GetComponent<RectTransform>();
        modalRt.anchorMin = new Vector2(0.015f, 0.015f);
        modalRt.anchorMax = new Vector2(0.985f, 0.985f);
        modalRt.offsetMin = modalRt.offsetMax = Vector2.zero;
        modalRt.pivot = new Vector2(0.5f, 0.5f);

        Image modalBg = modalObj.GetComponent<Image>();
        modalBg.sprite = GetPanelBgSprite();
        modalBg.type = Image.Type.Sliced;
        modalBg.color = new Color(0.050f, 0.065f, 0.095f, 0.98f);

        // Khung viền chạm khắc Gothic mạ hoàng kim cổ điển
        var frameObj = new GameObject("FrameBorder", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(modalObj.transform, false);
        var frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = frameRt.offsetMax = Vector2.zero;

        var frameImg = frameObj.GetComponent<Image>();
        frameImg.sprite = GetFrameSprite();
        frameImg.type = Image.Type.Sliced;
        frameImg.pixelsPerUnitMultiplier = 0.35f;
        frameImg.color = new Color(0.95f, 0.82f, 0.40f, 0.90f);
        frameImg.raycastTarget = false;

        // 2. Tiêu đề Tiệm Cổ Vật (Header Title)
        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(modalObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.20f, 0.932f);
        titleRt.anchorMax = new Vector2(0.80f, 0.985f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
        titleRt.pivot = new Vector2(0.5f, 0.5f);

        headerTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        headerTitleText.fontSize = 26f;
        headerTitleText.fontStyle = FontStyles.Bold;
        headerTitleText.alignment = TextAlignmentOptions.Center;
        headerTitleText.color = new Color(1f, 0.88f, 0.45f);
        SoulslikeHUDStyler.SetSafeOutline(headerTitleText, 0.45f, new Color(0.02f, 0.02f, 0.02f, 0.98f));

        // Cánh hoa văn trang trí 2 bên Tiêu đề
        CreateDividerWing(modalObj.transform, "TitleLeftWing",
            new Vector2(0.20f, 0.945f), new Vector2(0.32f, 0.970f),
            new Color(1f, 0.85f, 0.40f, 0.85f), false);

        CreateDividerWing(modalObj.transform, "TitleRightWing",
            new Vector2(0.68f, 0.945f), new Vector2(0.80f, 0.970f),
            new Color(1f, 0.85f, 0.40f, 0.85f), true);

        // Phụ đề (Subtitle)
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(modalObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.15f, 0.885f);
        subRt.anchorMax = new Vector2(0.85f, 0.930f);
        subRt.offsetMin = subRt.offsetMax = Vector2.zero;
        subRt.pivot = new Vector2(0.5f, 0.5f);

        headerSubtitleText = subObj.GetComponent<TextMeshProUGUI>();
        headerSubtitleText.fontSize = 14f;
        headerSubtitleText.alignment = TextAlignmentOptions.Center;
        headerSubtitleText.color = new Color(0.78f, 0.86f, 0.95f);
        SoulslikeHUDStyler.SetSafeOutline(headerSubtitleText, 0.30f, Color.black);

        // Đường hoa văn phân cách đối xứng
        CreateOrnamentDivider(modalObj.transform, "HeaderDivider",
            new Vector2(0.20f, 0.865f), new Vector2(0.80f, 0.880f),
            new Color(0.95f, 0.82f, 0.40f, 0.85f));

        // Khay hiển thị số dư Mảnh Cổ Tự (Shards Counter) - Đặt gọn ở góc trên bên trái
        GameObject shardsObj = new GameObject("ShardsCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
        shardsObj.transform.SetParent(modalObj.transform, false);
        RectTransform shardsRt = shardsObj.GetComponent<RectTransform>();
        shardsRt.anchorMin = new Vector2(0.035f, 0.920f);
        shardsRt.anchorMax = new Vector2(0.240f, 0.980f);
        shardsRt.offsetMin = shardsRt.offsetMax = Vector2.zero;
        shardsRt.pivot = new Vector2(0f, 0.5f);

        shardsCounterText = shardsObj.GetComponent<TextMeshProUGUI>();
        shardsCounterText.fontSize = 16f;
        shardsCounterText.fontStyle = FontStyles.Bold;
        shardsCounterText.alignment = TextAlignmentOptions.MidlineLeft;
        SoulslikeHUDStyler.SetSafeOutline(shardsCounterText, 0.35f, Color.black);

        // Nút Đóng [X] góc phải trên
        GameObject closeBtnObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        closeBtnObj.transform.SetParent(modalObj.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.945f, 0.920f);
        closeRt.anchorMax = new Vector2(0.985f, 0.980f);
        closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;
        closeRt.pivot = new Vector2(1f, 0.5f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.sprite = GetStoneBtnSprite();
        closeImg.type = Image.Type.Sliced;
        closeImg.color = new Color(0.35f, 0.12f, 0.15f, 0.95f);

        var closeOutline = closeBtnObj.GetComponent<Outline>();
        closeOutline.effectColor = new Color(0.95f, 0.35f, 0.35f, 0.85f);
        closeOutline.effectDistance = new Vector2(1.2f, -1.2f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(Close);

        GameObject closeTxtObj = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        var ctRt = closeTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;

        var closeTmp = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTmp.text = "X";
        closeTmp.fontSize = 20f;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = new Color(1f, 0.85f, 0.4f);
        SoulslikeHUDStyler.SetSafeOutline(closeTmp, 0.35f, Color.black);

        // 3. Khung chứa 3 Thẻ Lõi Cổ Vật (Rộng rãi, tương xứng tuyệt đối với LevelUpChoiceUI)
        GameObject cardsObj = new GameObject("CardContainer", typeof(RectTransform));
        cardsObj.transform.SetParent(modalObj.transform, false);
        RectTransform cardsRt = cardsObj.GetComponent<RectTransform>();
        cardsRt.anchorMin = new Vector2(0.05f, 0.115f);
        cardsRt.anchorMax = new Vector2(0.95f, 0.855f);
        cardsRt.offsetMin = cardsRt.offsetMax = Vector2.zero;
        cardContainer = cardsObj.transform;

        // 4. Popup Toast Thông Báo Trong Shop
        toastObj = new GameObject("ShopToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        toastObj.transform.SetParent(modalObj.transform, false);
        RectTransform toastRt = toastObj.GetComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0.5f, 0f);
        toastRt.anchorMax = new Vector2(0.5f, 0f);
        toastRt.pivot = new Vector2(0.5f, 0f);
        toastRt.sizeDelta = new Vector2(680f, 38f);
        toastRt.anchoredPosition = new Vector2(0f, 80f);

        var toastBg = toastObj.GetComponent<Image>();
        toastBg.sprite = GetStoneBtnSprite();
        toastBg.type = Image.Type.Sliced;
        toastBg.color = new Color(0.08f, 0.05f, 0.06f, 0.96f);

        toastOutline = toastObj.GetComponent<Outline>();
        toastOutline.effectColor = new Color(1f, 0.35f, 0.35f, 0.9f);
        toastOutline.effectDistance = new Vector2(1.5f, -1.5f);

        toastCg = toastObj.GetComponent<CanvasGroup>();
        toastCg.alpha = 0f;
        toastCg.blocksRaycasts = false;

        GameObject toastTxtObj = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        toastTxtObj.transform.SetParent(toastObj.transform, false);
        RectTransform ttRt = toastTxtObj.GetComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero;
        ttRt.anchorMax = Vector2.one;
        ttRt.offsetMin = new Vector2(14f, 2f);
        ttRt.offsetMax = new Vector2(-14f, -2f);

        toastText = toastTxtObj.GetComponent<TextMeshProUGUI>();
        toastText.fontSize = 14f;
        toastText.fontStyle = FontStyles.Bold;
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.color = Color.white;
        SoulslikeHUDStyler.SetSafeOutline(toastText, 0.35f, Color.black);

        toastObj.SetActive(false);

        // 5. Nút Làm Mới Lõi Cổ Vật (Reroll Button - 50 Mảnh Cổ Tự)
        GameObject rerollBtnObj = new GameObject("RerollBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        rerollBtnObj.transform.SetParent(modalObj.transform, false);
        RectTransform rerollRt = rerollBtnObj.GetComponent<RectTransform>();
        rerollRt.anchorMin = new Vector2(0.20f, 0.018f);
        rerollRt.anchorMax = new Vector2(0.48f, 0.095f);
        rerollRt.offsetMin = rerollRt.offsetMax = Vector2.zero;

        Image rerollImg = rerollBtnObj.GetComponent<Image>();
        rerollImg.sprite = GetStoneBtnSprite();
        rerollImg.type = Image.Type.Sliced;
        rerollImg.color = new Color(0.26f, 0.16f, 0.40f, 0.98f); // Runic Mystic Violet

        var rerollOutline = rerollBtnObj.GetComponent<Outline>();
        rerollOutline.effectColor = new Color(0.85f, 0.45f, 1f, 0.90f);
        rerollOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button rerollBtn = rerollBtnObj.GetComponent<Button>();
        rerollBtn.onClick.AddListener(OnRerollShopClicked);

        GameObject rerollTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        rerollTxtObj.transform.SetParent(rerollBtnObj.transform, false);
        var rttRt = rerollTxtObj.GetComponent<RectTransform>();
        rttRt.anchorMin = Vector2.zero;
        rttRt.anchorMax = Vector2.one;
        rttRt.offsetMin = rttRt.offsetMax = Vector2.zero;

        var rerollTmp = rerollTxtObj.GetComponent<TextMeshProUGUI>();
        rerollTmp.text = LocalizationManager.T("LÀM MỚI (50 MẢNH)", "REROLL (50 SHARDS)");
        rerollTmp.fontSize = 16f;
        rerollTmp.fontStyle = FontStyles.Bold;
        rerollTmp.alignment = TextAlignmentOptions.Center;
        rerollTmp.color = new Color(1f, 0.88f, 0.45f);
        SoulslikeHUDStyler.SetSafeOutline(rerollTmp, 0.35f, Color.black);

        // 6. Nút "Tiếp tục qua cổng" ở chân Modal
        GameObject continueBtnObj = new GameObject("ContinueBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        continueBtnObj.transform.SetParent(modalObj.transform, false);
        RectTransform contRt = continueBtnObj.GetComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0.52f, 0.018f);
        contRt.anchorMax = new Vector2(0.80f, 0.095f);
        contRt.offsetMin = contRt.offsetMax = Vector2.zero;

        Image contImg = continueBtnObj.GetComponent<Image>();
        contImg.sprite = GetStoneBtnSprite();
        contImg.type = Image.Type.Sliced;
        contImg.color = new Color(0.12f, 0.22f, 0.36f, 0.98f);

        var contOutline = continueBtnObj.GetComponent<Outline>();
        contOutline.effectColor = new Color(0.35f, 0.75f, 1f, 0.85f);
        contOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button contBtn = continueBtnObj.GetComponent<Button>();
        contBtn.onClick.AddListener(Close);

        GameObject contTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        contTxtObj.transform.SetParent(continueBtnObj.transform, false);
        var ctRt2 = contTxtObj.GetComponent<RectTransform>();
        ctRt2.anchorMin = Vector2.zero;
        ctRt2.anchorMax = Vector2.one;
        ctRt2.offsetMin = ctRt2.offsetMax = Vector2.zero;

        var contTmp = contTxtObj.GetComponent<TextMeshProUGUI>();
        contTmp.text = LocalizationManager.T("TIẾP TỤC HÀNH TRÌNH", "CONTINUE JOURNEY");
        contTmp.fontSize = 16f;
        contTmp.fontStyle = FontStyles.Bold;
        contTmp.alignment = TextAlignmentOptions.Center;
        contTmp.color = Color.white;
        SoulslikeHUDStyler.SetSafeOutline(contTmp, 0.35f, Color.black);

        InitRelicCatalog();
        RollRandomChoices(3);
        BuildCardItems();
    }

    // ─── Danh Mục Cổ Vật (9 Thần Binh & Cổ Vật Quý Hiếm Đậm Chất Dark Fantasy) ──
    public static void ResetShopForNewRun()
    {
        shopCatalog.Clear();
        activeChoices.Clear();
        lastRolledSceneName = "";
    }

    // ─── Danh Mục Cổ Vật (9 Cổ Vật Cơ Bản + 8 Cổ Vật Map Nguyên Tố + 6 Cổ Vật Tướng Độc Quyền + 3 Tiên Đan Vô Hạn) ──
    private void InitRelicCatalog()
    {
        if (shopCatalog.Count > 0) return;

        // =========================================================================
        // ─── PHẦN 1: 9 CỔ VẬT CƠ BẢN (CORE RELICS) ──────────────────────────────
        // =========================================================================

        // 1. Sát Thương Vật Lý (Physical Damage)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_physical",
            categoryVN = "SÁT THƯƠNG VẬT LÝ", categoryEN = "PHYSICAL DAMAGE",
            badgeColor = new Color(0.96f, 0.26f, 0.21f), // Đỏ Chiến Binh
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Attack",
            augmentType = AugmentDatabase.AugmentType.BonusAttack,
            titleVN = "Lưỡi Kiếm Hắc Thiết", titleEN = "Dark Iron Blade",
            descVN = "Tăng vĩnh viễn <color=#FFEAA7><b>+22 Sát Thương Vật Lý</b></color> trên mọi chiêu thức và đòn đánh căn bản.",
            descEN = "Permanently adds <color=#FFEAA7><b>+22 Base Physical Damage</b></color> across all abilities and attacks.",
            flavorVN = "«Thanh gươm rèn từ tàn tích của một vương triều đã sụp đổ.»",
            flavorEN = "«A heavy blade forged from the obsidian embers of a fallen realm.»",
            cost = 260,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusAttack,
                    value = 22f,
                    nameVN = "Lưỡi Kiếm Hắc Thiết", nameEN = "Dark Iron Blade",
                    descVN = "Tăng <color=#FFEAA7><b>+{v} Sát Thương Vật Lý</b></color>.",
                    descEN = "Gain <color=#FFEAA7><b>+{v} Physical Attack</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
            }
        });

        // 2. Sát Thương Phép (Magic Damage)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_thunder",
            categoryVN = "SÁT THƯƠNG PHÉP", categoryEN = "MAGIC DAMAGE",
            badgeColor = new Color(0.72f, 0.33f, 1f), // Tím Ma Pháp
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Ulti",
            augmentType = AugmentDatabase.AugmentType.EchoStrike,
            titleVN = "Lôi Đình Trảm", titleEN = "Chain Lightning",
            descVN = "Đòn đánh phóng tia sét lan truyền gây <color=#B854FF><b>55 Sát Thương Phép</b></color> lên tối đa 3 kẻ địch lân cận.",
            descEN = "Attacks unleash chain lightning dealing <color=#B854FF><b>55 Magic Damage</b></color> arcing up to 3 nearby foes.",
            flavorVN = "«Sấm sét cổ xưa cuộn trào trên lưỡi kiếm rít gào.»",
            flavorEN = "«Ancient thunderbolts surging through whistling blade edges.»",
            cost = 320,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.EchoStrike,
                    value = 45f,
                    nameVN = "Lôi Đình Trảm", nameEN = "Chain Lightning",
                    descVN = "Đòn đánh phóng tia sét lan truyền gây <color=#B854FF><b>{v}% Sát Thương Phép</b></color>.",
                    descEN = "Attacks unleash chain lightning dealing <color=#B854FF><b>{v}% Magic Damage</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Epic
                });
            }
        });

        // 3. Sát Thương Chuẩn (True Damage)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_truedamage",
            categoryVN = "SÁT THƯƠNG CHUẨN", categoryEN = "TRUE DAMAGE",
            badgeColor = new Color(0.92f, 0.96f, 1f), // Bạc Kim Cương
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Attack",
            augmentType = AugmentDatabase.AugmentType.LaceratingEdge,
            titleVN = "Lưỡi Kiếm Hư Không", titleEN = "Void Piercer",
            descVN = "Đòn đánh xuyên phá tuyệt đối <color=#55EFC4><b>35% Giáp</b></color>, chuyển hóa thành Sát Thương Chuẩn bỏ qua mọi phòng ngự.",
            descEN = "Attacks pierce <color=#55EFC4><b>35% Armor</b></color>, converting into irresistible True Damage.",
            flavorVN = "«Không tấm khiên nào có thể đứng vững trước vết rách hư không.»",
            flavorEN = "«No shield withstands the severing tears of the void.»",
            cost = 290,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusAttack,
                    value = 18f,
                    nameVN = "Lưỡi Kiếm Hư Không", nameEN = "Void Piercer",
                    descVN = "Đòn đánh xuyên phá tuyệt đối, tăng <color=#FFEAA7><b>+{v} Sát Thương</b></color>.",
                    descEN = "Gain <color=#FFEAA7><b>+{v} Attack</b></color> piercing defenses.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Epic
                });
            }
        });

        // 4. Bạo Kích Chí Mạng (Critical Strike)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_crit",
            categoryVN = "BẠO KÍCH CHÍ MẠNG", categoryEN = "CRITICAL STRIKE",
            badgeColor = new Color(1f, 0.82f, 0.12f), // Vàng Kim Hoàng Triều
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Star",
            augmentType = AugmentDatabase.AugmentType.BonusCritChance,
            titleVN = "Mắt Ưng Cuồng Bạo", titleEN = "Hawkeye Fury",
            descVN = "Tăng <color=#FBBF24><b>+12% Tỉ Lệ Bạo Kích</b></color> và nhận thêm <color=#F59E0B><b>+35% Sát Thương Bạo Kích</b></color>.",
            descEN = "Gain <color=#FBBF24><b>+12% Critical Chance</b></color> and <color=#F59E0B><b>+35% Critical Damage</b></color>.",
            flavorVN = "«Ánh nhìn săn mồi nhìn thấu mọi khe hở tử huyệt.»",
            flavorEN = "«Predatory focus perceiving every fatal fracture.»",
            cost = 280,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusCritChance,
                    value = 12f,
                    nameVN = "Mắt Ưng Cuồng Bạo", nameEN = "Hawkeye Fury",
                    descVN = "Tăng <color=#FBBF24><b>+{v}% Tỉ Lệ Bạo Kích</b></color>.",
                    descEN = "Gain <color=#FBBF24><b>+{v}% Critical Chance</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusCritDamage,
                    value = 35f,
                    nameVN = "Cuồng Bạo Chí Mạng", nameEN = "Critical Fury",
                    descVN = "Tăng <color=#F59E0B><b>+{v}% Sát Thương Bạo Kích</b></color>.",
                    descEN = "Gain <color=#F59E0B><b>+{v}% Critical Damage</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
            }
        });

        // 5. Huyết Thống Hút Máu (Lifesteal)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_lifesteal",
            categoryVN = "HUYẾT THỐNG HÚT MÁU", categoryEN = "LIFESTEAL",
            badgeColor = new Color(0.95f, 0.15f, 0.25f), // Đỏ Huyết Vampiric
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Attack",
            augmentType = AugmentDatabase.AugmentType.BonusLifesteal,
            titleVN = "Huyết Nguyệt Trảo", titleEN = "Bloodmoon Claws",
            descVN = "Nhận <color=#F42640><b>+6% Hút Máu Vật Lý</b></color> trên mọi đòn đánh. Hồi phục sinh lực liên tục trong giao tranh.",
            descEN = "Gain <color=#F42640><b>+6% Physical Lifesteal</b></color> on all hits. Continuously restores health in combat.",
            flavorVN = "«Từng vết chém uống cạn sinh khí của con mồi tàn tích.»",
            flavorEN = "«Every slash siphons the vitality of falling foes.»",
            cost = 250,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusLifesteal,
                    value = 6f,
                    nameVN = "Huyết Nguyệt Trảo", nameEN = "Bloodmoon Claws",
                    descVN = "Nhận <color=#F42640><b>+{v}% Hút Máu Vật Lý</b></color> trên mọi đòn đánh.",
                    descEN = "Gain <color=#F42640><b>+{v}% Physical Lifesteal</b></color> on all hits.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
            }
        });

        // 6. Sinh Lực Thần Dược (Vitality Elixir)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_elixir",
            categoryVN = "SINH LỰC THẦN DƯỢC", categoryEN = "VITALITY ELIXIR",
            badgeColor = new Color(0.18f, 0.88f, 0.45f), // Xanh Ngọc Sinh Lực
            rarity = AugmentDatabase.AugmentData.Rarity.Common,
            iconName = "Hero",
            augmentType = AugmentDatabase.AugmentType.RegenerativeCore,
            titleVN = "Thần Dược Trường Sinh", titleEN = "Vitality Elixir",
            descVN = "Hồi ngay <color=#55EFC4><b>150 HP</b></color> và gia tăng vĩnh viễn <color=#55EFC4><b>+75 Máu Tối Đa</b></color> cho toàn bộ màn chơi.",
            descEN = "Instantly heals <color=#55EFC4><b>150 HP</b></color> and permanently grants <color=#55EFC4><b>+75 Max HP</b></color> for the run.",
            flavorVN = "«Chưng cất từ giọt sương thiêng của đại thụ tàn tích cổ đại.»",
            flavorEN = "«Distilled from sacred drops of the ancient ruin arbor.»",
            cost = 180,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusHealth,
                    value = 75f,
                    nameVN = "Thần Dược Trường Sinh", nameEN = "Vitality Elixir",
                    descVN = "Gia tăng vĩnh viễn <color=#55EFC4><b>+{v} Máu Tối Đa</b></color>.",
                    descEN = "Gain permanent <color=#55EFC4><b>+{v} Max Health</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Common
                });
                var ph = Object.FindAnyObjectByType<PlayerHealth>();
                if (ph != null) ph.Heal(150);
            }
        });

        // 7. Tốc Đánh Nhanh Nhẹn (Agility & CDR)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_swiftness",
            categoryVN = "TỐC ĐÁNH NHANH NHẸN", categoryEN = "AGILITY SPEED",
            badgeColor = new Color(0.20f, 0.80f, 1f), // Xanh Băng Lam
            rarity = AugmentDatabase.AugmentData.Rarity.Common,
            iconName = "Dash",
            augmentType = AugmentDatabase.AugmentType.BonusCooldownReduction,
            titleVN = "Bão Táp Tốc Đánh", titleEN = "Tempest Swiftness",
            descVN = "Tăng <color=#38BDF8><b>+20% Tốc Độ Đánh</b></color> & giảm <color=#38BDF8><b>-20% Thời Gian Hồi Chiêu</b></color> căn bản.",
            descEN = "Gain <color=#38BDF8><b>+20% Attack Speed</b></color> and reduce <color=#38BDF8><b>-20% Attack Cooldown</b></color>.",
            flavorVN = "«Vút nhanh như cơn cuồng phong rít qua đỉnh tháp đổ nát.»",
            flavorEN = "«Swift as storm winds whistling across broken spires.»",
            cost = 210,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusCooldownReduction,
                    value = 20f,
                    nameVN = "Bão Táp Tốc Đánh", nameEN = "Tempest Swiftness",
                    descVN = "Giảm <color=#38BDF8><b>+{v}% Thời Gian Hồi Chiêu</b></color>.",
                    descEN = "Reduce <color=#38BDF8><b>+{v}% Cooldowns</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Common
                });
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusSpeed,
                    value = 15f,
                    nameVN = "Bước Chân Cuồng Phong", nameEN = "Gale Stride",
                    descVN = "Tăng <color=#38BDF8><b>+{v}% Tốc Độ Di Chuyển</b></color>.",
                    descEN = "Gain <color=#38BDF8><b>+{v}% Movement Speed</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Common
                });
            }
        });

        // 8. Khiên Hộ Thần Cổ (Defense Barrier)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_shield",
            categoryVN = "PHÒNG THỦ HỘ MỆNH", categoryEN = "DEFENSE BARRIER",
            badgeColor = new Color(0.35f, 0.65f, 1f), // Lam Khiên Phòng Thủ
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Hero",
            augmentType = AugmentDatabase.AugmentType.BonusDefense,
            titleVN = "Khiên Hộ Thần Cổ", titleEN = "Aegis of the Fallen",
            descVN = "Nhận <color=#60A5FA><b>+18 Giáp Cố Định</b></color> và tạo lớp Giáp Ảo <color=#38BDF8><b>120 Shield</b></color> bảo vệ cơ thể.",
            descEN = "Gain <color=#60A5FA><b>+18 Armor</b></color> and gain a <color=#38BDF8><b>120 Virtual Shield barrier</b></color>.",
            flavorVN = "«Tấm khiên rèn từ hợp kim thép đen kiên cố ngàn năm.»",
            flavorEN = "«Tempered from millennial black iron, unyielding to blows.»",
            cost = 240,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusDefense,
                    value = 18f,
                    nameVN = "Khiên Hộ Thần Cổ", nameEN = "Aegis of the Fallen",
                    descVN = "Nhận <color=#60A5FA><b>+{v} Giáp Cố Định</b></color>.",
                    descEN = "Gain <color=#60A5FA><b>+{v} Permanent Armor</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
                var ph = Object.FindAnyObjectByType<PlayerHealth>();
                if (ph != null) ph.AddVirtualHealth(120, 30f);
            }
        });

        // 9. Khế Ước Vực Thẳm (Mythic Covenant)
        // ─── CỔ VẬT MỚI & LÕI HAI MẶT ĐÁNH ĐỔI (CURSED & MECHANICS) ───
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_guillotine",
            categoryVN = "TRẢM SÁT KIM CƯƠNG", categoryEN = "PRISMATIC EXECUTE",
            badgeColor = new Color(0.96f, 0.26f, 0.21f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Attack",
            augmentType = AugmentDatabase.AugmentType.PercentCurrentHpExecute,
            titleVN = "Trảm Sát Trảm Kích", titleEN = "Guillotine Execute",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Đòn đánh gây thêm sát thương bằng <color=#FFEAA7><b>50% Máu Hiện Tại</b></color> của kẻ địch.\n<color=#FDCB6E><b>[HẠN CHẾ]</b></color> Sát thương lên Boss tối đa <color=#FDCB6E><b>15% Max HP</b></color> (trần 1200 dmg).",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Attacks deal bonus damage equal to <color=#FFEAA7><b>50% Target Current HP</b></color>.\n<color=#FDCB6E><b>[LIMIT]</b></color> Capped at <color=#FDCB6E><b>15% Max HP</b></color> on Bosses (max 1200 dmg).",
            flavorVN = "«Một trảm đoạn tuyệt sinh cơ, vạn vật quy về cát bụi.»",
            flavorEN = "«A single cleave that severs life itself, regardless of armor.»",
            cost = 380,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.PercentCurrentHpExecute);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_golden_sanctuary",
            categoryVN = "HỘ THỂ HOÀNG KIM", categoryEN = "GOLDEN SANCTUARY",
            badgeColor = new Color(1.00f, 0.82f, 0.20f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Hero",
            augmentType = AugmentDatabase.AugmentType.GoldenSanctuary,
            titleVN = "Kim Thân Bất Hoại", titleEN = "Golden Sanctuary",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Mỗi <color=#FFEAA7><b>10 Giây</b></color> ban Hào Quang Vàng bọc thân, <color=#55EFC4><b>Miễn Nhiễm 100% Khống Chế</b></color> (Choáng, Băng, Nhện).\n<color=#FDCB6E><b>[CƠ CHẾ]</b></color> Tự động kích hoạt khi trúng đòn khống chế, sau đó đếm ngược 10s hồi lại.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Every <color=#FFEAA7><b>10s</b></color> grants Golden Aura, <color=#55EFC4><b>100% Immunity to CC</b></color> (Stun, Freeze, Web).\n<color=#FDCB6E><b>[MECHANIC]</b></color> Consumed on trigger, recharges after 10s cooldown.",
            flavorVN = "«Thân như kim cương bất hoại, vạn tà bất xâm.»",
            flavorEN = "«Body forged in diamond brilliance, impervious to all afflictions.»",
            cost = 350,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.GoldenSanctuary);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_cursed_might",
            categoryVN = "KHẾ ƯỚC NGỦA RỦA", categoryEN = "CURSED COVENANT",
            badgeColor = new Color(0.85f, 0.15f, 0.25f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Attack",
            augmentType = AugmentDatabase.AugmentType.CursedMight,
            titleVN = "Khế Ước Cuồng Nộ", titleEN = "Cursed Might",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Nhận vĩnh viễn <color=#FFEAA7><b>+100 Sát Thương Đòn Đánh (ATK)</b></color>.\n<color=#FF7675><b>[BẤT LỢI]</b></color> Toàn bộ Quái vật & Boss tăng thêm <color=#FF7675><b>+50% Máu Tối Đa & Hiện Tại</b></color>.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Permanently gain <color=#FFEAA7><b>+100 Attack Damage (ATK)</b></color>.\n<color=#FF7675><b>[CURSE]</b></color> All Enemies & Bosses permanently gain <color=#FF7675><b>+50% Max HP</b></color>.",
            flavorVN = "«Sức mạnh hủy diệt đổi lấy sự thức tỉnh của bầy quái hung bạo.»",
            flavorEN = "«Catastrophic power granted at the cost of awakening monstrosities.»",
            cost = 320,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.CursedMight);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_ironclad_pact",
            categoryVN = "MINH ƯỚC HẮC THIẾT", categoryEN = "IRONCLAD PACT",
            badgeColor = new Color(0.35f, 0.65f, 0.95f),
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Hero",
            augmentType = AugmentDatabase.AugmentType.IroncladPact,
            titleVN = "Minh Ước Hắc Thiết", titleEN = "Ironclad Pact",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Nhận thêm <color=#74B9FF><b>+50 Giáp Phòng Thủ</b></color>.\n<color=#FF7675><b>[BẤT LỢI]</b></color> Quái & Lính nhận thêm <color=#FF7675><b>5% Miễn Thương</b></color> vĩnh viễn.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Gain <color=#74B9FF><b>+50 Armor Defense</b></color>.\n<color=#FF7675><b>[CURSE]</b></color> All Minions & Monsters gain <color=#FF7675><b>+5% Damage Reduction</b></color>.",
            flavorVN = "«Lớp giáp dày như thành lũy rèn từ quặng hắc thiết bị nguyền rủa.»",
            flavorEN = "«Impenetrable fortress armor forged with cursed black iron.»",
            cost = 240,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.IroncladPact);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_chrono_stasis",
            categoryVN = "ĐẦM LẦY THỜI KHÔNG", categoryEN = "CHRONO STASIS",
            badgeColor = new Color(0.00f, 0.85f, 0.85f),
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Star",
            augmentType = AugmentDatabase.AugmentType.ChronoStasis,
            titleVN = "Đầm Lầy Thời Không", titleEN = "Chrono Stasis",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Trong <color=#FFEAA7><b>60 Giây</b></color>, toàn bộ quái vật bị <color=#00CEC9><b>Làm Chậm 35%</b></color> tốc đánh, đạn bay và di chuyển.\n<color=#FF7675><b>[BẤT LỢI]</b></color> Bản thân bị <color=#FF7675><b>Giảm 50% Tốc Chạy</b></color> trong thời gian hiệu lực.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> For <color=#FFEAA7><b>60s</b></color>, all enemies suffer <color=#00CEC9><b>-35% Attack & Projectile Speed & Move Speed</b></color>.\n<color=#FF7675><b>[CURSE]</b></color> Hero's Move Speed is <color=#FF7675><b>Reduced by 50%</b></color> for the duration.",
            flavorVN = "«Dòng chảy thời gian bị bóp méo, từng bước chân như chìm trong đầm lầy hắc ám.»",
            flavorEN = "«Time itself turns viscous, drowning footsteps in the eternal mire.»",
            cost = 250,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.ChronoStasis);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_blood_tithe",
            categoryVN = "MA KIẾM HUYẾT TẾ", categoryEN = "BLOOD TITHE BLADE",
            badgeColor = new Color(0.95f, 0.15f, 0.20f),
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Attack",
            augmentType = AugmentDatabase.AugmentType.BloodTitheBlade,
            titleVN = "Ma Kiếm Huyết Tế", titleEN = "Blood Tithe Blade",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Nhận <color=#FFEAA7><b>+40 ATK</b></color> và <color=#55EFC4><b>+40% Hút Máu Toàn Phần</b></color>.\n<color=#FF7675><b>[BẤT LỢI]</b></color> Ma kiếm ăn mòn sinh lực, <color=#FF7675><b>Mất 1% Máu mỗi giây</b></color> (tối thiểu 1 HP).",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Gain <color=#FFEAA7><b>+40 ATK</b></color> and <color=#55EFC4><b>+40% Omnivamp</b></color>.\n<color=#FF7675><b>[CURSE]</b></color> Drains <color=#FF7675><b>1% Max HP per second</b></color> (caps at 1 HP).",
            flavorVN = "«Lưỡi kiếm uống máu của kẻ thù lẫn sinh lực của chủ nhân.»",
            flavorEN = "«A thirsting blade that feeds equally upon its foes and bearer.»",
            cost = 260,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.BloodTitheBlade);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_glass_cannon",
            categoryVN = "BẢN THỂ THỦY TINH", categoryEN = "GLASS CANNON",
            badgeColor = new Color(1.00f, 0.80f, 0.10f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Star",
            augmentType = AugmentDatabase.AugmentType.GlassCannonTitan,
            titleVN = "Bản Thể Thủy Tinh", titleEN = "Glass Cannon Titan",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Nhận <color=#FFEAA7><b>+75% Tỉ Lệ Crit</b></color> và <color=#FFEAA7><b>+80% Sát Thương Crit</b></color>.\n<color=#FF7675><b>[BẤT LỢI]</b></color> Thân thể pha lê: <color=#FF7675><b>Nhận Thêm +30% Sát Thương</b></color> từ kẻ địch.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Gain <color=#FFEAA7><b>+75% Crit</b></color> and <color=#FFEAA7><b>+80% Crit DMG</b></color>.\n<color=#FF7675><b>[CURSE]</b></color> Fragile body takes <color=#FF7675><b>+30% More Damage</b></color> from enemies.",
            flavorVN = "«Sắc bén khôn cùng nhưng mỏng manh tựa pha lê trước đòn thù.»",
            flavorEN = "«Unmatched lethality encased in fragile crystalline glass.»",
            cost = 330,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.GlassCannonTitan);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_phantom_rush",
            categoryVN = "BÓNG MA CUỒNG NỘ", categoryEN = "PHANTOM RUSH",
            badgeColor = new Color(0.65f, 0.35f, 0.95f),
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Star",
            augmentType = AugmentDatabase.AugmentType.PhantomRush,
            titleVN = "Bóng Ma Cuồng Nộ", titleEN = "Phantom Rush",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Nhận thêm <color=#74B9FF><b>+50% Hồi Chiêu (CDR)</b></color> mọi kỹ năng.\n<color=#FF7675><b>[BẤT LỢI]</b></color> Hy sinh vĩnh viễn <color=#FF7675><b>25% Máu Tối Đa</b></color>.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Gain <color=#74B9FF><b>+50% Cooldown Reduction</b></color> across all abilities.\n<color=#FF7675><b>[CURSE]</b></color> Permanently sacrifice <color=#FF7675><b>25% Max HP</b></color>.",
            flavorVN = "«Tốc biến xuất quỷ nhập thần đổi lấy sinh mệnh hao gầy.»",
            flavorEN = "«Ghostly haste achieved by shaving away mortal vitality.»",
            cost = 270,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.PhantomRush);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_soul_abundance",
            categoryVN = "LINH HỒN BỘI THU", categoryEN = "SOUL ABUNDANCE",
            badgeColor = new Color(0.15f, 0.85f, 0.95f),
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Hero",
            augmentType = AugmentDatabase.AugmentType.SoulHarvesterMultiplier,
            titleVN = "Linh Hồn Bội Thu", titleEN = "Soul Abundance",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Nhận thêm <color=#00FFFF><b>+50% Linh Hồn (x1.5 Lần)</b></color> từ mọi nguồn thu thập trong trận đấu.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Gain <color=#00FFFF><b>+50% More Shards (x1.5 Multiplier)</b></color> from all soul drops in battle.",
            flavorVN = "«Linh khí đất trời hội tụ, mỗi tàn hồn tiêu tán đều kết tinh thành bảo vật.»",
            flavorEN = "«Spiritual aether converges, condensing every fallen essence into crystal bounty.»",
            cost = 280,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.SoulHarvesterMultiplier);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_skill_soul_burst",
            categoryVN = "KHAI HOA LINH HỒN", categoryEN = "SKILL SOUL SURGE",
            badgeColor = new Color(0.75f, 0.40f, 1.00f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Ulti",
            augmentType = AugmentDatabase.AugmentType.SkillSoulSurge,
            titleVN = "Khai Hoa Linh Hồn", titleEN = "Skill Soul Surge",
            descVN = "<color=#55EFC4><b>[LỢI ÍCH]</b></color> Mỗi khi thi triển <color=#FFEAA7><b>Tuyệt Kỹ / Kỹ Năng</b></color>, có <color=#A29BFE><b>50% Tỉ Lệ</b></color> khai mở bộc phát rơi ra <color=#00FFFF><b>3 đến 6 Mảnh Linh Hồn</b></color>.",
            descEN = "<color=#55EFC4><b>[BENEFIT]</b></color> Whenever casting a <color=#FFEAA7><b>Special Skill</b></color>, have a <color=#A29BFE><b>50% Chance</b></color> to surge and burst forth <color=#00FFFF><b>3-6 Aethel Shards</b></color>.",
            flavorVN = "«Tuyệt kỹ xé rách ranh giới hư vô, giải phóng nguồn linh hồn ẩn sâu trong vạn vật.»",
            flavorEN = "«Supreme prowess sunders the veil, releasing boundless dormant souls into the realm.»",
            cost = 360,
            onPurchase = () =>
            {
                var augData = AugmentDatabase.GetData(AugmentDatabase.AugmentType.SkillSoulSurge);
                if (augData != null) AugmentManager.EnsureInstance()?.ApplyAugment(augData);
            }
        });

        shopCatalog.Add(new ShopRelicItem

        {

            id = "relic_abyssal",
            categoryVN = "TOÀN NĂNG THẦN THOẠI", categoryEN = "MYTHIC COVENANT",
            badgeColor = new Color(0.95f, 0.45f, 0.90f), // Hồng Tinh Vân
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Ulti",
            augmentType = AugmentDatabase.AugmentType.DemonicPact,
            titleVN = "Khế Ước Vực Thẳm", titleEN = "Abyssal Covenant",
            descVN = "Tăng <color=#FFEAA7><b>+30 Sát Thương</b></color>, <color=#38BDF8><b>+15% Tốc Chạy</b></color> và <color=#F42640><b>+8% Hút Máu</b></color> toàn năng.",
            descEN = "Grants <color=#FFEAA7><b>+30 Attack</b></color>, <color=#38BDF8><b>+15% Move Speed</b></color> and <color=#F42640><b>+8% Omnivamp</b></color>.",
            flavorVN = "«Dấu ấn huyết thệ cổ xưa khai mở sức mạnh tột đỉnh tàn tích.»",
            flavorEN = "«Bloodbound pact unraveling unbound ancient destruction.»",
            cost = 360,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusAttack,
                    value = 30f,
                    nameVN = "Khế Ước Vực Thẳm", nameEN = "Abyssal Covenant",
                    descVN = "Tăng <color=#FFEAA7><b>+{v} Sát Thương</b></color> toàn năng.",
                    descEN = "Gain <color=#FFEAA7><b>+{v} Damage</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Epic
                });
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusSpeed,
                    value = 15f,
                    nameVN = "Hư Không Tật Tốc", nameEN = "Void Rush",
                    descVN = "Tăng <color=#38BDF8><b>+{v}% Tốc Chạy</b></color>.",
                    descEN = "Gain <color=#38BDF8><b>+{v}% Move Speed</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Epic
                });
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusLifesteal,
                    value = 8f,
                    nameVN = "Huyết Hư Không", nameEN = "Void Leech",
                    descVN = "Nhận <color=#F42640><b>+{v}% Hút Máu</b></color>.",
                    descEN = "Gain <color=#F42640><b>+{v}% Omnivamp</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Epic
                });
            }
        });

        // =========================================================================
        // ─── PHẦN 2: CỔ VẬT NGUYÊN TỐ THEO MAP (ELEMENTAL REALM RELICS) ──────────
        // =========================================================================

        // Magma Realm
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_flame_adaptation",
            categoryVN = "HỎA DIỆM NGUYÊN TỐ", categoryEN = "FLAME REALM",
            badgeColor = new Color(1f, 0.45f, 0.15f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.MagmaCalamity,
            augmentType = AugmentDatabase.AugmentType.FlameAdaptation,
            maxPurchases = 1,
            cost = 340,
            titleVN = "Thích Ứng Hỏa Ma", titleEN = "Flame Adaptation",
            descVN = "Kháng hoàn toàn trạng thái Thiêu Đốt. Khi trúng lửa, lập tức phóng Cầu Lửa Dung Nham phản kích gây 140 Sát Thương!",
            descEN = "Immune to Burn. When struck by fire, counter-fires a 140 DMG Magma Fireball!",
            flavorVN = "«Cơ thể hòa quyện cùng dung nham, biến lửa dữ thành sức mạnh.»",
            flavorEN = "«Merging with the magma, transmuting scorching flames into fury.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.FlameAdaptation);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_magma_eruption",
            categoryVN = "HỎA DIỆM THIÊU ĐỐT", categoryEN = "BLAZING INFERNO",
            badgeColor = new Color(1f, 0.35f, 0.10f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.MagmaCalamity,
            augmentType = AugmentDatabase.AugmentType.MagmaEruption,
            maxPurchases = 1,
            cost = 320,
            titleVN = "Hỏa Diệm Thiêu Đốt", titleEN = "Blazing Inferno",
            descVN = "Đòn đánh và Lướt phóng Ngọn Lửa Thiêu Đốt bám chặt lên cơ thể quái và Boss, gây cháy 35 Sát Thương mỗi 0.5s suốt 4.5s.",
            descEN = "Attacks and Dash ignite enemies and Bosses with clinging flames, dealing 35 DMG per 0.5s over 4.5s.",
            flavorVN = "«Ngọn lửa bất diệt bám chặt lấy con mồi, thiêu rụi đến tàn tro cuối cùng.»",
            flavorEN = "«Undying flames cling to their prey, burning until nothing but ash remains.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.MagmaEruption);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // Acid Realm
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_acid_membrane",
            categoryVN = "AXIT KHÁNG ĐỘC", categoryEN = "ACID MEMBRANE",
            badgeColor = new Color(0.35f, 0.95f, 0.25f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.AcidDownpour,
            augmentType = AugmentDatabase.AugmentType.AcidMembrane,
            maxPurchases = 1,
            cost = 340,
            titleVN = "Màng Sinh Học Axit", titleEN = "Acid Membrane",
            descVN = "Giảm 60% sát thương Axit/Độc. Khi dính axit, hồi 6% Máu tối đa và phát nổ Bào Tử Độc gây choáng kẻ địch xung quanh!",
            descEN = "Reduces Acid/Poison DMG by 60%. When struck, heals 6% Max HP and detonates Toxic Spores!",
            flavorVN = "«Lớp vỏ chitin bài tiết độc tố, biến độc dược thành thần dược hồi sinh.»",
            flavorEN = "«Chitinous membrane siphoning toxicity into pure regenerative vitality.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.AcidMembrane);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_virulent_bloom",
            categoryVN = "BÀO TỬ ĐỘC TỐ", categoryEN = "VIRULENT BLOOM",
            badgeColor = new Color(0.40f, 0.98f, 0.35f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.AcidDownpour,
            augmentType = AugmentDatabase.AugmentType.VirulentBloom,
            maxPurchases = 1,
            cost = 320,
            titleVN = "Kịch Độc Nở Rộ", titleEN = "Virulent Bloom",
            descVN = "Đòn đánh găm Độc Axit Ăn Mòn; khi địch tử trận sẽ phát nổ lây lan sang các mục tiêu khác.",
            descEN = "Attacks afflict Corrosive Acid; dying targets detonate and spread poison spores.",
            flavorVN = "«Mầm bệnh tàn tích ký sinh trên sinh mệnh mục nát.»",
            flavorEN = "«Ruin spores metastasizing upon decaying vitality.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.VirulentBloom);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // Frostfall Realm
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_glacial_soul",
            categoryVN = "BĂNG GIÁ HỘ MỆNH", categoryEN = "FROST REALM",
            badgeColor = new Color(0.45f, 0.85f, 1f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.FrostfallBlizzard,
            augmentType = AugmentDatabase.AugmentType.GlacialSoul,
            maxPurchases = 1,
            cost = 340,
            titleVN = "Linh Hồn Băng Tuyết", titleEN = "Glacial Soul",
            descVN = "Miễn nhiễm làm chậm và đóng băng từ bão tuyết. Nhận Khiên Băng +220 Shield; khi vỡ làm đóng băng kẻ địch xung quanh!",
            descEN = "Immune to Blizzard slow/freeze. Grants +220 Frost Shield that freezes nearby foes upon shattering!",
            flavorVN = "«Trái tim băng giá không bao giờ khuất phục trước cái lạnh vĩnh cửu.»",
            flavorEN = "«A frozen soul that never yields to the eternal blizzard.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.GlacialSoul);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_absolute_zero_nova",
            categoryVN = "BĂNG PHONG TUYỆT ĐỐI", categoryEN = "ABSOLUTE ZERO",
            badgeColor = new Color(0.60f, 0.90f, 1f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.FrostfallBlizzard,
            augmentType = AugmentDatabase.AugmentType.AbsoluteZeroNova,
            maxPurchases = 1,
            cost = 330,
            titleVN = "Sương Hàn Tuyệt Đối", titleEN = "Absolute Zero Nova",
            descVN = "Đòn đánh có 30% tỉ lệ phát nổ Tinh Thể Băng gây 95 Sát Thương và đóng băng kẻ địch trong 1.2s.",
            descEN = "Attacks have 30% chance to detonate Ice Crystals dealing 95 DMG and freezing foes for 1.2s.",
            flavorVN = "«Một nhát kiếm đưa kẻ thù vào giấc ngủ ngàn năm trong băng giá.»",
            flavorEN = "«A single strike entombing foes into millennial slumber.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.AbsoluteZeroNova);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // Void / Nightmare Realm
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_void_transcendence",
            categoryVN = "HƯ KHÔNG SIÊU VIỆT", categoryEN = "VOID TRANSCENDENCE",
            badgeColor = new Color(0.85f, 0.35f, 1f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.VoidNightmare,
            augmentType = AugmentDatabase.AugmentType.VoidTranscendence,
            maxPurchases = 1,
            cost = 360,
            titleVN = "Hư Không Siêu Việt", titleEN = "Void Transcendence",
            descVN = "Tăng +40 Sát Thương, +25% Hút Máu. Chuyển hóa 25% sát thương nhận vào từ Hư Không thành lượng Hồi Máu!",
            descEN = "Grants +40 ATK, +25% Lifesteal. Transmutes 25% Void DMG taken into healing!",
            flavorVN = "«Nuốt chửng bóng đêm tăm tối nhất để trở thành chúa tể hư vô.»",
            flavorEN = "«Devouring the deepest darkness to reign as master of the void.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.VoidTranscendence);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_shadow_mirroring",
            categoryVN = "PHÂN THÂN ẢNH KIẾM", categoryEN = "SHADOW MIRROR",
            badgeColor = new Color(0.70f, 0.20f, 0.95f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.VoidNightmare,
            augmentType = AugmentDatabase.AugmentType.ShadowMirroring,
            maxPurchases = 1,
            cost = 350,
            titleVN = "Bản Sao Bóng Đêm", titleEN = "Shadow Mirroring",
            descVN = "Mỗi khi Lướt (Dash), triệu hồi một Phân Thân Hư Không tung nhát chém quét gây 120 Sát Thương diện rộng!",
            descEN = "Dashing summons a Void Clone executing a sweep slash dealing 120 AoE Damage!",
            flavorVN = "«Ảo ảnh sinh đôi chém nát kẻ bám đuổi trong chớp mắt.»",
            flavorEN = "«Twin mirages severing pursuers in the blink of an eye.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.ShadowMirroring);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // =========================================================================
        // ─── PHẦN 3: CỔ VẬT ĐẶC QUYỀN RIÊNG CỦA 6 TƯỚNG (HERO SIGNATURE RELICS) ──
        // =========================================================================

        // 1. Dune Assassin (Sát Thủ Bão Sa - Chiêu Gồng Phóng 3 Lốc)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_dune_tornado",
            categoryVN = "ĐẶC QUYỀN TƯỚNG: BÃO SA", categoryEN = "HERO: DUNE ASSASSIN",
            badgeColor = new Color(1f, 0.78f, 0.25f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            heroRequirement = "DuneAssassin",
            augmentType = AugmentDatabase.AugmentType.TripleMirageTornado,
            maxPurchases = 1,
            cost = 380,
            titleVN = "Tam Sa Cuồng Phong", titleEN = "Triple Mirage Tornado",
            descVN = "Chiêu gồng tích tụ bão cát khi phóng ra sẽ giải phóng đồng thời <color=#FBBF24><b>3 Cơn Lốc Xoáy Bão Sa</b></color> (tiêu tốn máu x3) bủa vây quét sạch toàn bộ chiến trường!",
            descEN = "Charged sandstorm defense unleashes <color=#FBBF24><b>3 Mirage Tornadoes</b></color> (costs 3x HP) sweeping across the entire battlefield!",
            flavorVN = "«Cơn cuồng phong sa mạc cổ đại xé toạc mọi kẻ thù dám cản bước sát thủ.»",
            flavorEN = "«Ancient desert tempest tearing asunder all who dare impede the assassin.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.TripleMirageTornado);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // 2. Celestial Mage (Pháp Sư Tinh Tú - Siêu Cụm Mưa Thiên Thạch)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_mage_meteor",
            categoryVN = "ĐẶC QUYỀN TƯỚNG: TINH TÚ", categoryEN = "HERO: CELESTIAL MAGE",
            badgeColor = new Color(0.75f, 0.40f, 1f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            heroRequirement = "CelestialMage",
            augmentType = AugmentDatabase.AugmentType.MeteorSupercluster,
            maxPurchases = 1,
            cost = 380,
            titleVN = "Siêu Cụm Thiên Thạch", titleEN = "Meteor Supercluster",
            descVN = "Chiêu Thiên Thạch triệu hồi thêm 4 mảnh thiên thạch phụ oanh tạc liên hoàn, tăng 100% diện tích nổ và gây choáng 1.5s!",
            descEN = "Meteor skill bombards with 4 additional fragment strikes, +100% explosion area and 1.5s stun!",
            flavorVN = "«Ngàn sao trên bầu trời rơi xuống hủy diệt tàn tích.»",
            flavorEN = "«A multitude of stars falling from the firmament to obliterate ruins.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.MeteorSupercluster);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // 3. Vaelstrider (Chiến Binh Kiếm Khí)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_vael_gale",
            categoryVN = "ĐẶC QUYỀN TƯỚNG: KIẾM KHÍ", categoryEN = "HERO: VAELSTRIDER",
            badgeColor = new Color(0.95f, 0.25f, 0.25f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            heroRequirement = "Vaelstrider",
            augmentType = AugmentDatabase.AugmentType.TripleRuinGale,
            maxPurchases = 1,
            cost = 380,
            titleVN = "Tam Trảm Tàn Tích", titleEN = "Triple Ruin Gale",
            descVN = "Đòn đánh và chiêu thức phóng ra chùm 3 luồng Kiếm Khí Tàn Tích rẽ quạt gây 75 Sát Thương mỗi luồng!",
            descEN = "Attacks and skills loose a 3-way fan of Ruin Sword Waves dealing 75 DMG each!",
            flavorVN = "«Đường kiếm tung hoành mở lối giữa biển quái vật.»",
            flavorEN = "«Unchecked blade path parting seas of ravenous abominations.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.TripleRuinGale);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // 4. Gale Ronin (Kiếm Khách Bão Tố - Lướt Tung 8 Hướng Kiếm Khí)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_ronin_tempest",
            categoryVN = "ĐẶC QUYỀN TƯỚNG: LÃNG KHÁCH", categoryEN = "HERO: GALE RONIN",
            badgeColor = new Color(0.20f, 0.85f, 0.95f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            heroRequirement = "GaleRonin",
            augmentType = AugmentDatabase.AugmentType.EightWayTempest,
            maxPurchases = 1,
            cost = 380,
            titleVN = "Bát Hướng Cuồng Phong", titleEN = "Eight-Way Tempest",
            descVN = "Mỗi khi Lướt (Dash), lập tức phát động 8 luồng Kiếm Khí Tật Phong quét 360 độ xé nát mọi kẻ thù vây quanh!",
            descEN = "Dashing immediately fires 8 Gale Crescent Blades in a 360-degree radius!",
            flavorVN = "«Rút kiếm trong chớp mắt, phong trảm tám hướng tuyệt diệt kẻ thù.»",
            flavorEN = "«Iaido strike unleashed, eight winds severing all adversaries.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.EightWayTempest);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // 5. Azure Blade (Lam Phong Băng Kiếm - Song Trụ Băng Phong)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_azure_frost",
            categoryVN = "ĐẶC QUYỀN TƯỚNG: BĂNG LAM", categoryEN = "HERO: AZURE BLADE",
            badgeColor = new Color(0.35f, 0.75f, 1f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            heroRequirement = "AzureBlade",
            augmentType = AugmentDatabase.AugmentType.TwinFrostNova,
            maxPurchases = 1,
            cost = 380,
            titleVN = "Song Băng Trụ Trảm", titleEN = "Twin Frost Nova",
            descVN = "Chiêu kiếm triệu hồi thêm 2 Cột Băng Cực Đại gây 110 Sát Thương Băng và đóng băng tuyệt đối 2.5s!",
            descEN = "Blade skills summon 2 additional Giant Frost Pillars dealing 110 DMG and freezing for 2.5s!",
            flavorVN = "«Hàn khí ngưng kết hóa thành song trụ băng vĩnh cửu.»",
            flavorEN = "«Frost essence crystallizing into twin eternal ice pillars.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.TwinFrostNova);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // 6. Titan Berserker (Cuồng Chiến Binh - Địa Chấn Hủy Diệt)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_titan_quake",
            categoryVN = "ĐẶC QUYỀN TƯỚNG: CỰ THẦN", categoryEN = "HERO: TITAN BERSERKER",
            badgeColor = new Color(0.95f, 0.40f, 0.15f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            heroRequirement = "TitanBerserker",
            augmentType = AugmentDatabase.AugmentType.CataclysmicQuake,
            maxPurchases = 1,
            cost = 380,
            titleVN = "Địa Chấn Cuồng Loạn", titleEN = "Cataclysmic Quake",
            descVN = "Mọi đòn nện và dậm đất tạo Sóng Xung Kích Địa Chấn gây 90 Sát Thương và làm choáng 1.5s toàn bộ diện rộng!",
            descEN = "Slams and ground stomps create Cataclysmic Shockwaves dealing 90 DMG and 1.5s AoE stun!",
            flavorVN = "«Một cú dậm chân làm rung chuyển cả đại lục tàn tích.»",
            flavorEN = "«A single earth stomp shaking the foundations of the ancient continent.»",
            onPurchase = () =>
            {
                var aug = AugmentDatabase.GetData(AugmentDatabase.AugmentType.CataclysmicQuake);
                if (aug != null) AugmentManager.EnsureInstance()?.ApplyAugment(aug);
            }
        });

        // =========================================================================
        // ─── PHẦN 4: TIÊN ĐAN VÔ HẠN (ENDLESS DRAUGHTS - MUA KHÔNG GIỚI HẠN) ─────
        // =========================================================================

        // 1. Tiên Đan Sinh Mệnh (Hồi đầy máu + tăng 80 Max HP vĩnh viễn)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_infinite_vitality",
            categoryVN = "TIÊN ĐAN VÔ HẠN", categoryEN = "ENDLESS ELIXIR",
            badgeColor = new Color(0.18f, 0.90f, 0.45f),
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Hero",
            isInfinite = true,
            maxPurchases = 999999,
            titleVN = "Tiên Đan Trường Sinh", titleEN = "Endless Vitality Draught",
            descVN = "Hồi phục <color=#55EFC4><b>100% Máu</b></color> ngay lập tức và tăng thêm vĩnh viễn <color=#55EFC4><b>+80 Máu Tối Đa</b></color>. Có thể mua vô hạn lần!",
            descEN = "Instantly heals <color=#55EFC4><b>100% Health</b></color> and permanently grants <color=#55EFC4><b>+80 Max HP</b></color>. Can be purchased infinitely!",
            flavorVN = "«Tinh hoa linh dược cô đọng từ ngàn hoa tàn tích.»",
            flavorEN = "«Condensed essence distilled from a myriad of ruin blooms.»",
            cost = 180,
            onPurchase = () =>
            {
                var ph = Object.FindAnyObjectByType<PlayerHealth>();
                if (ph != null) ph.Heal(99999);
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusHealth,
                    value = 80f,
                    nameVN = "Tiên Đan Trường Sinh", nameEN = "Endless Vitality",
                    descVN = "Tăng <color=#55EFC4><b>+80 Máu Tối Đa</b></color>.",
                    descEN = "Gain <color=#55EFC4><b>+80 Max Health</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
            }
        });

        // 2. Tiên Đan Bá Lực (Tăng vĩnh viễn +14 Sát Thương và +5% Tốc Đánh)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_infinite_power",
            categoryVN = "TIÊN ĐAN VÔ HẠN", categoryEN = "ENDLESS ELIXIR",
            badgeColor = new Color(1f, 0.35f, 0.25f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            iconName = "Attack",
            isInfinite = true,
            maxPurchases = 999999,
            titleVN = "Tiên Đan Bá Lực", titleEN = "Endless Might Draught",
            descVN = "Tăng vĩnh viễn <color=#FFEAA7><b>+14 Sát Thương</b></color> và <color=#38BDF8><b>+5% Tốc Đánh</b></color>. Có thể mua vô hạn lần!",
            descEN = "Permanently grants <color=#FFEAA7><b>+14 Attack</b></color> and <color=#38BDF8><b>+5% Attack Speed</b></color>. Can be purchased infinitely!",
            flavorVN = "«Ngọn lửa chiến cuồng bất tận rực cháy trong huyết quản.»",
            flavorEN = "«Unending flame of berserk might surging through veins.»",
            cost = 220,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusAttack,
                    value = 14f,
                    nameVN = "Tiên Đan Bá Lực", nameEN = "Endless Might",
                    descVN = "Tăng <color=#FFEAA7><b>+14 Sát Thương</b></color>.",
                    descEN = "Gain <color=#FFEAA7><b>+14 Attack</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Epic
                });
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusCooldownReduction,
                    value = 5f,
                    nameVN = "Cuồng Nộ Tốc Đánh", nameEN = "Might Swiftness",
                    descVN = "Giảm <color=#38BDF8><b>+5% Hồi Chiêu</b></color>.",
                    descEN = "Reduce <color=#38BDF8><b>+5% Cooldowns</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
            }
        });

        // 3. Tiên Đan Kim Thân (Tăng vĩnh viễn +10 Giáp và tạo 200 Khiên Ảo)
        shopCatalog.Add(new ShopRelicItem
        {
            id = "relic_infinite_shield",
            categoryVN = "TIÊN ĐAN VÔ HẠN", categoryEN = "ENDLESS ELIXIR",
            badgeColor = new Color(0.35f, 0.75f, 1f),
            rarity = AugmentDatabase.AugmentData.Rarity.Rare,
            iconName = "Hero",
            isInfinite = true,
            maxPurchases = 999999,
            titleVN = "Tiên Đan Kim Thân", titleEN = "Endless Aegis Draught",
            descVN = "Tăng vĩnh viễn <color=#60A5FA><b>+10 Giáp</b></color> và tạo Khiên Ảo <color=#38BDF8><b>200 Shield</b></color>. Có thể mua vô hạn lần!",
            descEN = "Permanently grants <color=#60A5FA><b>+10 Armor</b></color> and creates a <color=#38BDF8><b>200 Virtual Shield</b></color>. Can be purchased infinitely!",
            flavorVN = "«Khí giáp hộ thể kim cang bất hoại che chở sinh mệnh.»",
            flavorEN = "«Indestructible golden barrier safeguarding mortal soul.»",
            cost = 240,
            onPurchase = () =>
            {
                AugmentManager.EnsureInstance()?.ApplyAugment(new AugmentDatabase.AugmentData
                {
                    type = AugmentDatabase.AugmentType.BonusDefense,
                    value = 10f,
                    nameVN = "Tiên Đan Kim Thân", nameEN = "Endless Aegis",
                    descVN = "Tăng <color=#60A5FA><b>+10 Giáp</b></color>.",
                    descEN = "Gain <color=#60A5FA><b>+10 Armor</b></color>.",
                    rarity = AugmentDatabase.AugmentData.Rarity.Rare
                });
                var ph = Object.FindAnyObjectByType<PlayerHealth>();
                if (ph != null) ph.AddVirtualHealth(200, 45f);
            }
        });
    }

    private static string lastRolledSceneName = "";

    /// <summary>
    /// Tìm một cổ vật hợp lệ chưa mua tối đa để thay thế ô đã full (3/3)
    /// </summary>
    private static ShopRelicItem GetNextAvailableRelic(List<ShopRelicItem> excludeList = null)
    {
        string heroKey = PlayerUpgradeManager.Instance != null ? PlayerUpgradeManager.Instance.GetCurrentHeroKey() : "Vaelstrider";
        CalamityRealm curRealm = CorruptedCycleManager.CurrentRealm;

        // 1. Ưu tiên các Cổ vật hữu hạn chưa mua đủ (purchaseCount < maxPurchases && !isInfinite)
        var finitePool = shopCatalog.FindAll(x =>
            x != null &&
            !x.isInfinite &&
            x.purchaseCount < x.maxPurchases &&
            (x.heroRequirement == null || x.heroRequirement.Equals(heroKey, System.StringComparison.OrdinalIgnoreCase)) &&
            (x.realmRequirement == null || x.realmRequirement == curRealm) &&
            (excludeList == null || !excludeList.Contains(x))
        );

        if (finitePool.Count > 0)
        {
            return finitePool[Random.Range(0, finitePool.Count)];
        }

        // 2. Nếu đã mua hết tất cả lõi hữu hạn, lấy Tiên Đan Vô Hạn (Endless Consumables)
        var infinitePool = shopCatalog.FindAll(x => x != null && x.isInfinite && (excludeList == null || !excludeList.Contains(x)));
        if (infinitePool.Count > 0)
        {
            return infinitePool[Random.Range(0, infinitePool.Count)];
        }

        // Fallback bất kỳ tiên đan nào
        var anyInfinite = shopCatalog.FindAll(x => x != null && x.isInfinite);
        if (anyInfinite.Count > 0)
        {
            return anyInfinite[Random.Range(0, anyInfinite.Count)];
        }

        return null;
    }

    /// <summary>
    /// Random đúng 3 Lõi Cổ Vật khác nhau từ catalog (Ưu tiên Lõi chưa đạt cấp tối đa và phù hợp tướng + map)
    /// </summary>
    public static void RollRandomChoices(int count = 3, bool forceReroll = false)
    {
        if (shopCatalog == null || shopCatalog.Count == 0) return;

        string heroKey = PlayerUpgradeManager.Instance != null ? PlayerUpgradeManager.Instance.GetCurrentHeroKey() : "Vaelstrider";
        CalamityRealm curRealm = CorruptedCycleManager.CurrentRealm;

        // Lọc các item phù hợp với tướng hiện tại & map nguyên tố hiện tại
        List<ShopRelicItem> finiteCandidates = shopCatalog.FindAll(item =>
            item != null &&
            !item.isInfinite &&
            item.purchaseCount < item.maxPurchases &&
            (item.heroRequirement == null || item.heroRequirement.Equals(heroKey, System.StringComparison.OrdinalIgnoreCase)) &&
            (item.realmRequirement == null || item.realmRequirement == curRealm)
        );

        activeChoices.Clear();

        // Xáo trộn candidate
        for (int i = 0; i < finiteCandidates.Count; i++)
        {
            int r = Random.Range(i, finiteCandidates.Count);
            var temp = finiteCandidates[i];
            finiteCandidates[i] = finiteCandidates[r];
            finiteCandidates[r] = temp;
        }

        // Lấy tối đa từ finiteCandidates
        for (int i = 0; i < count && i < finiteCandidates.Count; i++)
        {
            activeChoices.Add(finiteCandidates[i]);
        }

        // Nếu thiếu để đủ 3 slot, điền bằng Tiên Đan Vô Hạn
        while (activeChoices.Count < count)
        {
            var next = GetNextAvailableRelic(activeChoices);
            if (next != null)
            {
                activeChoices.Add(next);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Xử lý bấm nút Làm Mới Tiệm Cổ Vật (50 Mảnh Cổ Tự)
    /// </summary>
    private void OnRerollShopClicked()
    {
        bool isVi = LocalizationManager.IsVietnamese;
        const int rerollCost = 50;
        long shards = GetPlayerShards();

        if (shards < rerollCost)
        {
            string msg = isVi
                ? $"Không đủ Mảnh Cổ Tự để làm mới! (Cần {rerollCost} Mảnh)"
                : $"Not enough Aethel Shards to reroll! (Need {rerollCost} Shards)";
            ShowShopToast(msg, new Color(1f, 0.35f, 0.35f));
            SoundManager.EnsureInstance()?.PlayUpgradeFail();
            return;
        }

        if (SpendShards(rerollCost))
        {
            RollRandomChoices(3, forceReroll: true);
            BuildCardItems();
            SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

            string succMsg = isVi
                ? "Đã làm mới danh sách Cổ Vật (-50 Mảnh)!"
                : "Shop choices refreshed (-50 Shards)!";
            ShowShopToast(succMsg, new Color(0.85f, 0.45f, 1f));
        }
    }

    // ─── Build 3 Relic Cards (Đồng bộ 100% thiết kế Dark Fantasy Yu-Gi-Oh + 3D Parallax Tilt với LevelUpChoiceUI) ──
    private static Sprite GetShopRelicIllustrationSprite(ShopRelicItem item, HashSet<Sprite> usedSprites)
    {
        if (item == null) return null;

        if (item.augmentType.HasValue)
        {
            var dummyAug = new AugmentDatabase.AugmentData
            {
                type = item.augmentType.Value,
                rarity = item.rarity
            };
            return LevelUpChoiceUI.GetAugmentCardIllustrationSprite(dummyAug, usedSprites);
        }

        var dummyType = AugmentDatabase.AugmentType.BonusHealth;
        string t = (item.titleVN ?? "").ToLower();
        if (t.Contains("kiếm") || t.Contains("tấn công") || t.Contains("sát thương") || t.Contains("chém"))
            dummyType = AugmentDatabase.AugmentType.BonusAttack;
        else if (t.Contains("giáp") || t.Contains("khiên") || t.Contains("thủ") || t.Contains("hộ"))
            dummyType = AugmentDatabase.AugmentType.BonusDefense;
        else if (t.Contains("tốc") || t.Contains("gió") || t.Contains("lướt"))
            dummyType = AugmentDatabase.AugmentType.BonusSpeed;
        else if (t.Contains("máu") || t.Contains("sinh") || t.Contains("huyết") || t.Contains("hút"))
            dummyType = AugmentDatabase.AugmentType.BonusLifesteal;
        else if (t.Contains("phép") || t.Contains("hồi") || t.Contains("năng") || t.Contains("chú"))
            dummyType = AugmentDatabase.AugmentType.BonusAbilityPower;
        else if (t.Contains("tinh thể") || t.Contains("vàng") || t.Contains("tiền"))
            dummyType = AugmentDatabase.AugmentType.BountyHunter;

        var mappedAug = new AugmentDatabase.AugmentData
        {
            type = dummyType,
            rarity = item.rarity
        };
        return LevelUpChoiceUI.GetAugmentCardIllustrationSprite(mappedAug, usedSprites);
    }

    private void BuildCardItems()
    {
        foreach (var c in cardObjects) if (c != null) Destroy(c);
        cardObjects.Clear();
        buyButtons.Clear();
        buyButtonTexts.Clear();
        progressTexts.Clear();

        if (activeChoices.Count == 0)
        {
            RollRandomChoices(3);
        }

        bool isVi = LocalizationManager.IsVietnamese;

        float cardWidth = 0.296f;
        float startX    = 0.024f;
        float gap       = (1f - startX * 2f - cardWidth * 3f) / 2f;

        // Tập hợp theo dõi sprite đã xuất hiện trong shop đợt này (Triệt tiêu 100% trùng hình)
        var usedSpritesInShop = new HashSet<Sprite>();

        for (int i = 0; i < activeChoices.Count; i++)
        {
            int idx = i;
            var item = activeChoices[i];

            float xMin = startX + i * (cardWidth + gap);
            float xMax = xMin + cardWidth;

            // Màu viền và banner phẩm chất đồng bộ 100% với LevelUpChoiceUI
            string tierTitle;
            Color tierFrameColor;
            Color tierBodyTint;
            Color tierBannerBg;
            int tierGemCount;
            bool isPrismatic = (item.rarity == AugmentDatabase.AugmentData.Rarity.Epic);

            switch (item.rarity)
            {
                case AugmentDatabase.AugmentData.Rarity.Epic:
                    tierTitle        = isVi ? "BẬC KIM CƯƠNG" : "PRISMATIC TIER";
                    tierFrameColor   = new Color(0.95f, 0.52f, 1.00f, 1.00f); // Lăng kính Tinh Vân
                    tierBodyTint     = new Color(0.10f, 0.07f, 0.16f, 0.98f); // Tím Hắc Ám sâu
                    tierBannerBg     = new Color(0.26f, 0.09f, 0.38f, 0.98f);
                    tierGemCount     = 3;
                    break;

                case AugmentDatabase.AugmentData.Rarity.Rare:
                    tierTitle        = isVi ? "BẬC VÀNG" : "GOLD TIER";
                    tierFrameColor   = new Color(1.00f, 0.82f, 0.26f, 1.00f); // Hoàng Kim Vương Giả
                    tierBodyTint     = new Color(0.11f, 0.085f, 0.055f, 0.98f); // Đen Hổ Phách
                    tierBannerBg     = new Color(0.32f, 0.20f, 0.05f, 0.98f);
                    tierGemCount     = 2;
                    break;

                default:
                    tierTitle        = isVi ? "BẬC BẠC" : "SILVER TIER";
                    tierFrameColor   = new Color(0.82f, 0.90f, 1.00f, 0.98f); // Bạch Kim Băng Giá
                    tierBodyTint     = new Color(0.065f, 0.085f, 0.125f, 0.98f); // Xanh Đá Phiến
                    tierBannerBg     = new Color(0.12f, 0.18f, 0.28f, 0.98f);
                    tierGemCount     = 1;
                    break;
            }

            // 1. Thẻ Bài Root Khung Đá Obsidian & Viền Chạm Khắc Kim Loại Kép
            var card = new GameObject($"Card_{item.id}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            card.transform.SetParent(cardContainer, false);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(xMin, 0.015f);
            cardRT.anchorMax = new Vector2(xMax, 0.985f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;
            cardRT.pivot = new Vector2(0.5f, 0.5f);

            // Hào quang viền ngoài thẻ (Outer Ambient Glow)
            var outerGlowObj = new GameObject("OuterGlow", typeof(RectTransform), typeof(Image));
            outerGlowObj.transform.SetParent(card.transform, false);
            var ogRT = outerGlowObj.GetComponent<RectTransform>();
            ogRT.anchorMin = new Vector2(-0.04f, -0.03f);
            ogRT.anchorMax = new Vector2(1.04f, 1.03f);
            ogRT.offsetMin = ogRT.offsetMax = Vector2.zero;
            var outerGlowImg = outerGlowObj.GetComponent<Image>();
            outerGlowImg.sprite = LevelUpChoiceUI.GetRadialGlowSprite();
            outerGlowImg.color = new Color(tierFrameColor.r, tierFrameColor.g, tierFrameColor.b, isPrismatic ? 0.16f : 0.08f);
            outerGlowImg.raycastTarget = false;

            // Thân thẻ Tarot (Card Body)
            var cardBodyImg = card.GetComponent<Image>();
            cardBodyImg.sprite = LevelUpChoiceUI.GetCardBodySprite();
            cardBodyImg.type = Image.Type.Sliced;
            cardBodyImg.color = tierBodyTint;
            cardBodyImg.raycastTarget = true;

            // Khung kim loại chạm khắc kép (Ornate Metallic Double Frame)
            var frameObj = new GameObject("FrameBorder", typeof(RectTransform), typeof(Image));
            frameObj.transform.SetParent(card.transform, false);
            var frameRT = frameObj.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = frameRT.offsetMax = Vector2.zero;
            var cardFrameImg = frameObj.GetComponent<Image>();
            cardFrameImg.sprite = LevelUpChoiceUI.GetOrnateCardFrameSprite();
            cardFrameImg.type = Image.Type.Sliced;
            cardFrameImg.color = tierFrameColor;
            cardFrameImg.raycastTarget = false;

            // 2. Top Header Bar (Tên Vật Phẩm + Icon Thuộc Tính ở góc trên bên phải)
            var headerBarObj = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            headerBarObj.transform.SetParent(card.transform, false);
            var hbRT = headerBarObj.GetComponent<RectTransform>();
            hbRT.anchorMin = new Vector2(0.045f, 0.910f);
            hbRT.anchorMax = new Vector2(0.955f, 0.985f);
            hbRT.offsetMin = hbRT.offsetMax = Vector2.zero;

            var hbImg = headerBarObj.GetComponent<Image>();
            hbImg.sprite = LevelUpChoiceUI.GetPillBadgeSprite();
            hbImg.type = Image.Type.Sliced;
            hbImg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            hbImg.raycastTarget = false;

            var hbOutline = headerBarObj.AddComponent<Outline>();
            hbOutline.effectColor = tierFrameColor * 0.85f;
            hbOutline.effectDistance = new Vector2(1f, -1f);

            var nameObj = new GameObject("ItemTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(headerBarObj.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.04f, 0f);
            nameRT.anchorMax = new Vector2(0.82f, 1f);
            nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

            var nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
            nameTMP.text = isVi ? item.titleVN : item.titleEN;
            nameTMP.enableAutoSizing = true;
            nameTMP.fontSizeMin = 24f;
            nameTMP.fontSizeMax = 36f;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            nameTMP.color = new Color(1f, 0.95f, 0.82f);
            nameTMP.raycastTarget = false;
            GameFontManager.ApplyFontToText(nameTMP, true);
            SoulslikeHUDStyler.SetSafeOutline(nameTMP, 0.0f, Color.black);

            // Attribute Element Badge ở góc phải Header
            var attrBadgeObj = new GameObject("AttributeElementBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
            attrBadgeObj.transform.SetParent(headerBarObj.transform, false);
            var attrRT = attrBadgeObj.GetComponent<RectTransform>();
            attrRT.anchorMin = new Vector2(0.84f, 0.08f);
            attrRT.anchorMax = new Vector2(0.98f, 0.92f);
            attrRT.offsetMin = attrRT.offsetMax = Vector2.zero;
            var attrImg = attrBadgeObj.GetComponent<Image>();
            attrImg.sprite = GetObsidianCoreSprite();
            attrImg.color = new Color(0.05f, 0.07f, 0.12f, 1f);
            attrImg.raycastTarget = false;

            var attrOutline = attrBadgeObj.GetComponent<Outline>();
            attrOutline.effectColor = item.badgeColor;
            attrOutline.effectDistance = new Vector2(1.2f, -1.2f);

            Sprite iconSprite = GetRelicSprite(item);
            if (iconSprite != null)
            {
                var attrIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                attrIconObj.transform.SetParent(attrBadgeObj.transform, false);
                var aicRT = attrIconObj.GetComponent<RectTransform>();
                aicRT.anchorMin = new Vector2(0.12f, 0.12f);
                aicRT.anchorMax = new Vector2(0.88f, 0.88f);
                aicRT.offsetMin = aicRT.offsetMax = Vector2.zero;
                var aicImg = attrIconObj.GetComponent<Image>();
                aicImg.sprite = iconSprite;
                aicImg.color = Color.white;
                aicImg.preserveAspect = true;
                aicImg.raycastTarget = false;
            }

            // 3. StatusRow (Bậc Phẩm Chất + Huy Hiệu Tiến Độ Cấp)
            var statusRowObj = new GameObject("StatusRow", typeof(RectTransform));
            statusRowObj.transform.SetParent(card.transform, false);
            var srRT = statusRowObj.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0.05f, 0.860f);
            srRT.anchorMax = new Vector2(0.95f, 0.900f);
            srRT.offsetMin = srRT.offsetMax = Vector2.zero;

            var tierBannerObj = new GameObject("TierBanner", typeof(RectTransform), typeof(Image));
            tierBannerObj.transform.SetParent(statusRowObj.transform, false);
            var tbRT = tierBannerObj.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0f, 0f);
            tbRT.anchorMax = new Vector2(0.48f, 1f);
            tbRT.offsetMin = tbRT.offsetMax = Vector2.zero;
            var tbImg = tierBannerObj.GetComponent<Image>();
            tbImg.sprite = LevelUpChoiceUI.GetPillBadgeSprite();
            tbImg.type = Image.Type.Sliced;
            tbImg.color = tierBannerBg;
            tbImg.raycastTarget = false;

            var tierTxtObj = new GameObject("TierText", typeof(RectTransform), typeof(TextMeshProUGUI));
            tierTxtObj.transform.SetParent(tierBannerObj.transform, false);
            var ttRT = tierTxtObj.GetComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero;
            ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;
            var tierTxt = tierTxtObj.GetComponent<TextMeshProUGUI>();
            tierTxt.text = tierTitle;
            tierTxt.color = tierFrameColor;
            tierTxt.fontSize = 24f;
            tierTxt.alignment = TextAlignmentOptions.Center;
            tierTxt.fontStyle = FontStyles.Bold;
            tierTxt.raycastTarget = false;
            GameFontManager.ApplyFontToText(tierTxt);
            SoulslikeHUDStyler.SetSafeOutline(tierTxt, 0.0f, Color.black);

            var rankBadgeObj = new GameObject("UpgradeTier", typeof(RectTransform), typeof(Image));
            rankBadgeObj.transform.SetParent(statusRowObj.transform, false);
            var rbRT = rankBadgeObj.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(0.52f, 0f);
            rbRT.anchorMax = new Vector2(1f, 1f);
            rbRT.offsetMin = rbRT.offsetMax = Vector2.zero;
            var rbImg = rankBadgeObj.GetComponent<Image>();
            rbImg.sprite = LevelUpChoiceUI.GetPillBadgeSprite();
            rbImg.type = Image.Type.Sliced;
            rbImg.color = new Color(0.04f, 0.20f, 0.12f, 0.96f);
            rbImg.raycastTarget = false;

            var progObj = new GameObject("RankStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            progObj.transform.SetParent(rankBadgeObj.transform, false);
            var progRT = progObj.GetComponent<RectTransform>();
            progRT.anchorMin = Vector2.zero;
            progRT.anchorMax = Vector2.one;
            progRT.offsetMin = progRT.offsetMax = Vector2.zero;

            var progTMP = progObj.GetComponent<TextMeshProUGUI>();
            progTMP.fontSize = 24f;
            progTMP.fontStyle = FontStyles.Bold;
            progTMP.alignment = TextAlignmentOptions.Center;
            progTMP.raycastTarget = false;
            GameFontManager.ApplyFontToText(progTMP);
            SoulslikeHUDStyler.SetSafeOutline(progTMP, 0.0f, Color.black);
            progressTexts.Add(progTMP);

            // 4. PictureArea: Vùng Tranh Lớn Ôm Khít Tỷ Lệ Chuẩn 3:4 (Không Bè, Không Lệch)
            var picAreaObj = new GameObject("PictureArea", typeof(RectTransform));
            picAreaObj.transform.SetParent(card.transform, false);
            var paRT = picAreaObj.GetComponent<RectTransform>();
            paRT.anchorMin = new Vector2(0.05f, 0.340f);
            paRT.anchorMax = new Vector2(0.95f, 0.850f);
            paRT.offsetMin = paRT.offsetMax = Vector2.zero;

            Sprite illustrationSprite = GetShopRelicIllustrationSprite(item, usedSpritesInShop);
            float artRatio = 0.747f;
            if (illustrationSprite != null && illustrationSprite.rect.height > 0)
            {
                artRatio = (float)illustrationSprite.rect.width / illustrationSprite.rect.height;
            }

            var picFrameObj = new GameObject("PictureFrame", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(AspectRatioFitter));
            picFrameObj.transform.SetParent(picAreaObj.transform, false);
            var pfRT = picFrameObj.GetComponent<RectTransform>();
            pfRT.anchorMin = Vector2.zero;
            pfRT.anchorMax = Vector2.one;
            pfRT.offsetMin = pfRT.offsetMax = Vector2.zero;
            pfRT.pivot = new Vector2(0.5f, 0.5f);

            var arf = picFrameObj.GetComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = artRatio;

            var pfImg = picFrameObj.GetComponent<Image>();
            pfImg.sprite = LevelUpChoiceUI.GetCardBodySprite();
            pfImg.type = Image.Type.Sliced;
            pfImg.color = new Color(0.03f, 0.04f, 0.06f, 1f);
            pfImg.raycastTarget = false;

            RectTransform phantomRT = null;
            if (illustrationSprite != null)
            {
                var artObj = new GameObject("CardIllustration", typeof(RectTransform), typeof(Image));
                artObj.transform.SetParent(picFrameObj.transform, false);
                phantomRT = artObj.GetComponent<RectTransform>();
                phantomRT.anchorMin = Vector2.zero;
                phantomRT.anchorMax = Vector2.one;
                phantomRT.offsetMin = phantomRT.offsetMax = Vector2.zero;
                phantomRT.pivot = new Vector2(0.5f, 0.5f);

                var artImg = artObj.GetComponent<Image>();
                artImg.sprite = illustrationSprite;
                artImg.color = Color.white;
                artImg.preserveAspect = false;
                artImg.raycastTarget = false;
            }

            // Lớp Phản Quang Kim Tuyến Cầu Vồng 2.5D (Holographic Foil Shimmer)
            var holoFoilObj = new GameObject("HolographicFoil", typeof(RectTransform), typeof(Image));
            holoFoilObj.transform.SetParent(picFrameObj.transform, false);
            var holoRT = holoFoilObj.GetComponent<RectTransform>();
            holoRT.anchorMin = Vector2.zero;
            holoRT.anchorMax = Vector2.one;
            holoRT.offsetMin = holoRT.offsetMax = Vector2.zero;
            holoRT.pivot = new Vector2(0.5f, 0.5f);

            var holoImg = holoFoilObj.GetComponent<Image>();
            holoImg.sprite = Card3DParallaxTilt.GetHolographicFoilSprite();
            holoImg.color = new Color(1f, 1f, 1f, 0.12f);
            holoImg.raycastTarget = false;

            // Khung viền kim loại kép mạ vàng bao quanh bức tranh
            var picBorderObj = new GameObject("PictureBorder", typeof(RectTransform), typeof(Image));
            picBorderObj.transform.SetParent(picFrameObj.transform, false);
            var pbRT = picBorderObj.GetComponent<RectTransform>();
            pbRT.anchorMin = Vector2.zero;
            pbRT.anchorMax = Vector2.one;
            pbRT.offsetMin = pbRT.offsetMax = Vector2.zero;
            var pbImg = picBorderObj.GetComponent<Image>();
            pbImg.sprite = LevelUpChoiceUI.GetOrnateCardFrameSprite();
            pbImg.type = Image.Type.Sliced;
            pbImg.color = tierFrameColor;
            pbImg.raycastTarget = false;
            picBorderObj.transform.SetAsLastSibling();

            // 5. DescriptionBox: Hộp Mô Tả Văn Bản Riêng Biệt + Nút Mua Khắc Vát
            var descBoxObj = new GameObject("DescriptionBox", typeof(RectTransform), typeof(Image), typeof(Outline));
            descBoxObj.transform.SetParent(card.transform, false);
            var dbRT = descBoxObj.GetComponent<RectTransform>();
            dbRT.anchorMin = new Vector2(0.045f, 0.016f);
            dbRT.anchorMax = new Vector2(0.955f, 0.328f);
            dbRT.offsetMin = dbRT.offsetMax = Vector2.zero;

            var dbImg = descBoxObj.GetComponent<Image>();
            dbImg.sprite = LevelUpChoiceUI.GetCardBodySprite();
            dbImg.type = Image.Type.Sliced;
            dbImg.color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            dbImg.raycastTarget = false;

            var dbOut = descBoxObj.GetComponent<Outline>();
            dbOut.effectColor = tierFrameColor * 0.75f;
            dbOut.effectDistance = new Vector2(1.2f, -1.2f);

            // Dòng phân loại Tag hệ kỹ năng
            var statTagObj = new GameObject("StatTag", typeof(RectTransform));
            statTagObj.transform.SetParent(descBoxObj.transform, false);
            var stRT = statTagObj.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.04f, 0.83f);
            stRT.anchorMax = new Vector2(0.96f, 0.97f);
            stRT.offsetMin = stRT.offsetMax = Vector2.zero;

            var stTxtObj = new GameObject("StatTagText", typeof(RectTransform), typeof(TextMeshProUGUI));
            stTxtObj.transform.SetParent(statTagObj.transform, false);
            var sttRT = stTxtObj.GetComponent<RectTransform>();
            sttRT.anchorMin = Vector2.zero;
            sttRT.anchorMax = Vector2.one;
            sttRT.offsetMin = sttRT.offsetMax = Vector2.zero;

            var stTxt = stTxtObj.GetComponent<TextMeshProUGUI>();
            stTxt.text = isVi ? $"[{item.categoryVN.ToUpper()}]" : $"[{item.categoryEN.ToUpper()}]";
            stTxt.color = item.badgeColor;
            stTxt.fontSize = 20f;
            stTxt.alignment = TextAlignmentOptions.MidlineLeft;
            stTxt.fontStyle = FontStyles.Bold;
            stTxt.raycastTarget = false;
            GameFontManager.ApplyFontToText(stTxt);
            SoulslikeHUDStyler.SetSafeOutline(stTxt, 0.0f, Color.black);

            CreateOrnamentDivider(descBoxObj.transform, "DescInnerDivider",
                new Vector2(0.04f, 0.805f), new Vector2(0.96f, 0.825f),
                tierFrameColor * 0.65f);

            // Nội dung mô tả
            string descText = isVi ? item.descVN : item.descEN;
            string flavorText = isVi ? item.flavorVN : item.flavorEN;
            string fullDesc = descText;
            if (!string.IsNullOrEmpty(flavorText))
            {
                fullDesc += $"\n<size=82%><color=#94A3B8><i>«{flavorText}»</i></color></size>";
            }

            var descObj = new GameObject("ItemDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(descBoxObj.transform, false);
            var descRT = descObj.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.04f, 0.26f);
            descRT.anchorMax = new Vector2(0.96f, 0.79f);
            descRT.offsetMin = descRT.offsetMax = Vector2.zero;

            var descTMP = descObj.GetComponent<TextMeshProUGUI>();
            descTMP.text = fullDesc;
            descTMP.enableAutoSizing = true;
            descTMP.fontSizeMin = 22f;
            descTMP.fontSizeMax = 30f;
            descTMP.alignment = TextAlignmentOptions.TopLeft;
            descTMP.color = new Color(0.92f, 0.95f, 1f);
            descTMP.enableWordWrapping = true;
            GameFontManager.ApplyFontToText(descTMP);
            SoulslikeHUDStyler.SetSafeOutline(descTMP, 0.0f, Color.black);

            // Nút Mua (BUY BUTTON) đặt gọn gàng ở đáy Hộp Mô Tả, hoàn toàn tách biệt không bao giờ che chữ
            var btnObj = new GameObject("BuyBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            btnObj.transform.SetParent(descBoxObj.transform, false);
            var btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.04f, 0.035f);
            btnRT.anchorMax = new Vector2(0.96f, 0.225f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

            var btnImg = btnObj.GetComponent<Image>();
            btnImg.sprite = LevelUpChoiceUI.GetPillBadgeSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.16f, 0.52f, 0.35f, 0.98f);

            var bOut = btnObj.GetComponent<Outline>();
            bOut.effectColor = tierFrameColor;
            bOut.effectDistance = new Vector2(1.2f, -1.2f);

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => OnBuyClicked(idx));

            var btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btnTxtRT = btnTxtObj.GetComponent<RectTransform>();
            btnTxtRT.anchorMin = Vector2.zero;
            btnTxtRT.anchorMax = Vector2.one;
            btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

            var btnTMP = btnTxtObj.GetComponent<TextMeshProUGUI>();
            btnTMP.fontSize = 26f;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.color = Color.white;
            GameFontManager.ApplyFontToText(btnTMP);
            SoulslikeHUDStyler.SetSafeOutline(btnTMP, 0.0f, Color.black);

            cardObjects.Add(card);
            buyButtons.Add(btn);
            buyButtonTexts.Add(btnTMP);
            GameFontManager.ApplyFontsGlobally(card);

            // Gắn hệ thống Hiệu ứng Con quay hồi chuyển (Gyroscopic Tilt) & Parallax 2.5D cho Thẻ Bài Shop
            var tiltComp = card.AddComponent<Card3DParallaxTilt>();
            tiltComp.Setup(cardRT, phantomRT, holoImg);

            // Hiệu ứng Hover tương tác
            SetupCardHover(card, cardRT, cardFrameImg, tierFrameColor);
        }

        Canvas.ForceUpdateCanvases();
        RefreshUI();
    }

    private void CreateCardGem(Transform parent, string name, Vector2 anchorPos, float size, Color color)
    {
        var gemObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        gemObj.transform.SetParent(parent, false);
        var rt = gemObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        var img = gemObj.GetComponent<Image>();
        img.sprite = LevelUpChoiceUI.GetDiamondGemSprite();
        img.color = color;
        img.raycastTarget = false;
    }

    private void SetupCardHover(GameObject cardObj, RectTransform cardRT, Image cardFrame, Color normalBorderColor)
    {
        var trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => {
            if (cardRT != null)
            {
                StartCoroutine(AnimateHover(cardRT, cardFrame, 1.035f, Color.white, 0.12f));
            }
        });

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => {
            if (cardRT != null)
            {
                StartCoroutine(AnimateHover(cardRT, cardFrame, 1.000f, normalBorderColor, 0.12f));
            }
        });

        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private IEnumerator AnimateHover(RectTransform rt, Image cardFrame,
        float targetScale, Color targetFrameColor, float duration)
    {
        if (rt == null) yield break;

        Vector3 startScale = rt.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        Color startCardColor = cardFrame != null ? cardFrame.color : targetFrameColor;

        float t = 0f;
        while (t < duration)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / duration);

            rt.localScale = Vector3.Lerp(startScale, endScale, factor);
            if (cardFrame != null) cardFrame.color = Color.Lerp(startCardColor, targetFrameColor, factor);

            yield return null;
        }

        if (rt != null) rt.localScale = endScale;
        if (cardFrame != null) cardFrame.color = targetFrameColor;
    }

    // ─── Xử lý Mua Cổ Vật (Buy Logic) ──────────────────────────────────────────
    private void OnBuyClicked(int index)
    {
        if (index < 0 || index >= activeChoices.Count) return;
        var item = activeChoices[index];
        bool isVi = LocalizationManager.IsVietnamese;

        // 1. Kiểm tra giới hạn mua tối đa (chỉ áp dụng cho vật phẩm không phải vô hạn)
        if (!item.isInfinite && item.purchaseCount >= item.maxPurchases)
        {
            string maxMsg = isVi
                ? $"Đã đạt cấp nâng cấp tối đa ({item.maxPurchases}/{item.maxPurchases})!"
                : $"Maximum upgrade limit reached ({item.maxPurchases}/{item.maxPurchases})!";

            ShowShopToast(maxMsg, new Color(1f, 0.85f, 0.25f));
            SoundManager.EnsureInstance()?.PlayUpgradeFail();

            // Tự động thay thế ngay ô đã full bằng một cổ vật khác hoặc Tiên Đan Vô Hạn
            var rep = GetNextAvailableRelic(activeChoices);
            if (rep != null)
            {
                activeChoices[index] = rep;
                BuildCardItems();
            }
            return;
        }

        // 2. Kiểm tra số dư Mảnh Cổ Tự
        long playerShards = GetPlayerShards();
        if (playerShards < item.cost)
        {
            string costStr = MainMenuManager.FormatCompactNumber(item.cost);
            string needMsg = isVi
                ? $"Không đủ Mảnh Cổ Tự! (Cần {costStr} Mảnh)"
                : $"Not enough Aethel Shards! (Need {costStr} Shards)";

            ShowShopToast(needMsg, new Color(1f, 0.35f, 0.35f));
            SoundManager.EnsureInstance()?.PlayUpgradeFail();
            return;
        }

        // 3. Tiến hành mua thành công
        if (SpendShards(item.cost))
        {
            item.purchaseCount++;
            item.onPurchase?.Invoke();

            SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

            string name = isVi ? item.titleVN : item.titleEN;
            string succMsg = item.isInfinite
                ? (isVi ? $"+ Mua thành công: {name} (Lần #{item.purchaseCount})!" : $"+ Purchased: {name} (Time #{item.purchaseCount})!")
                : (isVi ? $"+ Mua thành công: {name} (Cấp {item.purchaseCount}/{item.maxPurchases})!" : $"+ Purchased: {name} (Tier {item.purchaseCount}/{item.maxPurchases})!");

            ShowShopToast(succMsg, item.badgeColor);

            // Khi một lõi hữu hạn đạt cấp tối đa (3/3), tự động thay thế ngay bằng một lõi mới hoặc Tiên Đan Vô Hạn!
            if (!item.isInfinite && item.purchaseCount >= item.maxPurchases)
            {
                var replacement = GetNextAvailableRelic(activeChoices);
                if (replacement != null)
                {
                    activeChoices[index] = replacement;
                    BuildCardItems();
                }
            }

            RefreshUI();
        }
    }

    public void ShowShopToast(string message, Color accentColor)
    {
        if (toastObj == null || toastText == null || toastCg == null) return;

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }

        toastCoroutine = StartCoroutine(ToastRoutine(message, accentColor));
    }

    private IEnumerator ToastRoutine(string message, Color accentColor)
    {
        toastObj.SetActive(true);
        toastText.text = message;
        toastText.color = Color.Lerp(accentColor, Color.white, 0.25f);
        if (toastOutline != null)
        {
            toastOutline.effectColor = accentColor;
        }

        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / 0.15f);
            toastCg.alpha = factor;
            toastObj.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, factor);
            yield return null;
        }

        toastCg.alpha = 1f;
        toastObj.transform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(2.2f);

        t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / 0.25f);
            toastCg.alpha = 1f - factor;
            yield return null;
        }

        toastCg.alpha = 0f;
        toastObj.SetActive(false);
    }

    public void Open()
    {
        isOpen = true;
        GameAutoBootstrap.SetGameplayHUDVisible(false);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        string curScene = SceneManager.GetActiveScene().name;
        if (activeChoices.Count == 0 || cardObjects.Count == 0 || lastRolledSceneName != curScene)
        {
            lastRolledSceneName = curScene;
            RollRandomChoices(3);
            BuildCardItems();
        }
        else
        {
            // Kiểm tra xem có thẻ nào đã full (purchaseCount >= maxPurchases && !isInfinite) không, nếu có thì tự động thay thế bằng thẻ mới!
            bool needRebuild = false;
            for (int i = 0; i < activeChoices.Count; i++)
            {
                var it = activeChoices[i];
                if (it != null && !it.isInfinite && it.purchaseCount >= it.maxPurchases)
                {
                    var rep = GetNextAvailableRelic(activeChoices);
                    if (rep != null)
                    {
                        activeChoices[i] = rep;
                        needRebuild = true;
                    }
                }
            }
            if (needRebuild)
            {
                BuildCardItems();
            }
        }

        RefreshUI();
        SoundManager.EnsureInstance()?.PlayMenuHover();
        GameFontManager.ApplyFontsGlobally(gameObject);
    }

    public void Close()
    {
        isOpen = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        GameAutoBootstrap.SetGameplayHUDVisible(true);
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public bool IsOpen() => isOpen;

    public void RefreshUI()
    {
        bool isVi = LocalizationManager.IsVietnamese;

        if (headerTitleText != null)
        {
            headerTitleText.text = isVi ? "TIỆM CỔ VẬT THƯƠNG NHÂN LÃNG KHÁCH" : "WANDERING RELIC MERCHANT";
        }
        if (headerSubtitleText != null)
        {
            headerSubtitleText.text = isVi
                ? "Trao đổi Mảnh Cổ Tự lấy các Thần Dược & Cổ Vật Quý Hiếm trước khi sang ải mới."
                : "Exchange Aethel Shards for Rare Relics and Combat Boons before entering the portal.";
        }

        long shards = GetPlayerShards();
        if (shardsCounterText != null)
        {
            string sStr = MainMenuManager.FormatCompactNumber(shards);
            shardsCounterText.text = isVi
                ? $"<color=#94A3B8>MẢNH CỔ TỰ:</color> <color=#FBBF24><b>{sStr}</b></color> <color=#38BDF8>Mảnh</color>"
                : $"<color=#94A3B8>AETHEL SHARDS:</color> <color=#FBBF24><b>{sStr}</b></color> <color=#38BDF8>Shards</color>";
        }

        for (int i = 0; i < activeChoices.Count && i < cardObjects.Count; i++)
        {
            var item = activeChoices[i];
            var card = cardObjects[i];
            if (card == null) continue;

            // Stat tag text (Không dấu "[ ]")
            var badge = card.transform.Find("DescriptionBox/StatTag/StatTagText")?.GetComponent<TextMeshProUGUI>()
                        ?? card.transform.Find("StatTag/StatTagText")?.GetComponent<TextMeshProUGUI>();
            if (badge != null)
            {
                badge.text = isVi ? $"[{item.categoryVN.ToUpper()}]" : $"[{item.categoryEN.ToUpper()}]";
            }

            var title = card.transform.Find("HeaderBar/ItemTitle")?.GetComponent<TextMeshProUGUI>()
                        ?? card.transform.Find("ItemTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.text = isVi ? item.titleVN : item.titleEN;

            var desc = card.transform.Find("DescriptionBox/ItemDesc")?.GetComponent<TextMeshProUGUI>()
                       ?? card.transform.Find("ItemDesc")?.GetComponent<TextMeshProUGUI>();
            if (desc != null)
            {
                string descText = isVi ? item.descVN : item.descEN;
                string flavorText = isVi ? item.flavorVN : item.flavorEN;
                string fullDesc = descText;
                if (!string.IsNullOrEmpty(flavorText))
                {
                    fullDesc += $"\n<size=82%><color=#94A3B8><i>«{flavorText}»</i></color></size>";
                }
                desc.text = fullDesc;
            }

            // Tiến độ Cấp & Trạng thái Lõi đồng bộ 100% với LevelUpChoiceUI ("LÕI MỚI" / "THĂNG CẤP: CẤP X -> CẤP Y")
            if (i < progressTexts.Count && progressTexts[i] != null)
            {
                var badgeTransform = progressTexts[i].transform.parent;
                var badgeBg = badgeTransform != null ? badgeTransform.GetComponent<Image>() : null;
                var badgeOut = badgeTransform != null ? badgeTransform.GetComponent<Outline>() : null;

                if (item.isInfinite)
                {
                    progressTexts[i].text = isVi
                        ? (item.purchaseCount == 0 ? "<color=#55EFC4>TIÊN ĐAN VÔ HẠN</color>" : $"<color=#55EFC4>TIÊN ĐAN VÔ HẠN (ĐÃ MUA x{item.purchaseCount})</color>")
                        : (item.purchaseCount == 0 ? "<color=#55EFC4>ENDLESS ELIXIR</color>" : $"<color=#55EFC4>ENDLESS ELIXIR (BOUGHT x{item.purchaseCount})</color>");
                    if (badgeBg != null) badgeBg.color = new Color(0.05f, 0.22f, 0.15f, 0.96f);
                    if (badgeOut != null) badgeOut.effectColor = new Color(0.22f, 0.95f, 0.62f, 0.95f);
                }
                else
                {
                    int currentRank = item.purchaseCount;
                    if (item.augmentType.HasValue && AugmentManager.Instance != null)
                    {
                        currentRank = Mathf.Max(currentRank, AugmentManager.Instance.GetOwnedAugmentCount(item.augmentType.Value));
                    }

                    bool isMax = (item.purchaseCount >= item.maxPurchases);
                    if (isMax)
                    {
                        progressTexts[i].text = isVi
                            ? $"<color=#FBBF24>CẤP TỐI ĐA ({item.maxPurchases}/{item.maxPurchases})</color>"
                            : $"<color=#FBBF24>MAX LEVEL ({item.maxPurchases}/{item.maxPurchases})</color>";
                        if (badgeBg != null) badgeBg.color = new Color(0.28f, 0.18f, 0.04f, 0.96f);
                        if (badgeOut != null) badgeOut.effectColor = new Color(1.00f, 0.84f, 0.28f, 1.0f);
                    }
                    else if (currentRank == 0)
                    {
                        progressTexts[i].text = isVi
                            ? "<color=#8CFBE6>LÕI MỚI</color>"
                            : "<color=#8CFBE6>NEW AUGMENT</color>";
                        if (badgeBg != null) badgeBg.color = new Color(0.05f, 0.22f, 0.15f, 0.96f);
                        if (badgeOut != null) badgeOut.effectColor = new Color(0.22f, 0.95f, 0.62f, 0.95f);
                    }
                    else
                    {
                        progressTexts[i].text = isVi
                            ? $"<color=#FFEFA0>THĂNG CẤP:  CẤP {currentRank}  ->  CẤP {currentRank + 1}</color>"
                            : $"<color=#FFEFA0>UPGRADE:  LV.{currentRank}  ->  LV.{currentRank + 1}</color>";
                        if (badgeBg != null) badgeBg.color = new Color(0.26f, 0.18f, 0.04f, 0.96f);
                        if (badgeOut != null) badgeOut.effectColor = new Color(1.00f, 0.86f, 0.28f, 1.0f);
                    }
                }
            }

            // Nút Mua (BUY BUTTON)
            if (i < buyButtons.Count && i < buyButtonTexts.Count)
            {
                var btn = buyButtons[i];
                var btnText = buyButtonTexts[i];
                if (btn != null && btnText != null)
                {
                    btn.interactable = true;
                    bool isMax = (!item.isInfinite && item.purchaseCount >= item.maxPurchases);
                    bool canAfford = (shards >= item.cost);
                    var btnImg = btn.GetComponent<Image>();

                    if (isMax)
                    {
                        btnText.text = isVi ? "ĐÃ ĐẠT TỐI ĐA (3/3)" : "MAX PURCHASED (3/3)";
                        if (btnImg != null) btnImg.color = new Color(0.18f, 0.20f, 0.24f, 0.88f);
                    }
                    else
                    {
                        string costStr = MainMenuManager.FormatCompactNumber(item.cost);
                        string countSuffix = item.isInfinite
                            ? (item.purchaseCount > 0 ? $" (x{item.purchaseCount})" : "")
                            : (item.purchaseCount > 0 ? $" ({item.purchaseCount}/{item.maxPurchases})" : "");

                        btnText.text = isVi
                            ? $"MUA{countSuffix} ({costStr} MẢNH)"
                            : $"BUY{countSuffix} ({costStr} SHARDS)";

                        if (btnImg != null)
                        {
                            btnImg.color = canAfford
                                ? new Color(0.16f, 0.55f, 0.35f, 0.98f) // Xanh lục ngọc khi đủ tiền
                                : new Color(0.42f, 0.16f, 0.20f, 0.92f); // Đỏ sẫm khi thiếu tiền
                        }
                    }
                }
            }
        }
    }

    private static long GetPlayerShards()
    {
        var inv = Object.FindAnyObjectByType<PlayerInventory>();
        return inv != null ? inv.GetShards() : AccountDatabase.GetAethelShards();
    }

    private static bool SpendShards(long cost)
    {
        var inv = Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            return inv.SpendShards(cost);
        }
        long cur = AccountDatabase.GetAethelShards();
        if (cur >= cost)
        {
            AccountDatabase.AddAethelShards(-cost);
            return true;
        }
        return false;
    }

    // ─── Visual Elements Builders ──────────────────────────────────────────────
    private GameObject CreateFramedPanel(Transform parent, string name, Color bgColor, Color frameColor,
        Vector2 anchorMin, Vector2 anchorMax, float framePixelsMultiplier, out Image frameImg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var bgImg = go.AddComponent<Image>();
        bgImg.sprite = GetPanelBgSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = bgColor;
        bgImg.raycastTarget = true;

        var frameObj = new GameObject("FrameBorder");
        frameObj.transform.SetParent(go.transform, false);
        var frameRT = frameObj.AddComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = frameRT.offsetMax = Vector2.zero;

        frameImg = frameObj.AddComponent<Image>();
        frameImg.sprite = GetFrameSprite();
        frameImg.type = Image.Type.Sliced;
        frameImg.pixelsPerUnitMultiplier = framePixelsMultiplier;
        frameImg.color = frameColor;
        frameImg.raycastTarget = false;

        return go;
    }

    private GameObject CreateDividerWing(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color, bool flipHorizontal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        if (flipHorizontal)
        {
            rt.localScale = new Vector3(-1f, 1f, 1f);
        }

        var img = go.AddComponent<Image>();
        img.sprite = GetDividerSprite();
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = false;

        return go;
    }

    private GameObject CreateOrnamentDivider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var left = new GameObject("Left");
        left.transform.SetParent(go.transform, false);
        var leftRT = left.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.5f, 1f);
        leftRT.offsetMin = leftRT.offsetMax = Vector2.zero;
        var leftImg = left.AddComponent<Image>();
        leftImg.sprite = GetDividerSprite();
        leftImg.color = color;
        leftImg.raycastTarget = false;
        leftImg.preserveAspect = false;

        var right = new GameObject("Right");
        right.transform.SetParent(go.transform, false);
        var rightRT = right.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.5f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = rightRT.offsetMax = Vector2.zero;
        rightRT.localScale = new Vector3(-1f, 1f, 1f);
        var rightImg = right.AddComponent<Image>();
        rightImg.sprite = GetDividerSprite();
        rightImg.color = color;
        rightImg.raycastTarget = false;
        rightImg.preserveAspect = false;

        return go;
    }
}
