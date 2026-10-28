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
        }

        if (spr != null) return spr;

        if (item.iconName == "Dash")
        {
            if (_cachedDashIcon == null)
                _cachedDashIcon = LoadSpriteFromDiskOrEditor("Assets/Sprites/UI/Dash.png");
            return _cachedDashIcon;
        }

        if (item.iconName == "Ulti")
        {
            if (_cachedUltiIcon == null)
                _cachedUltiIcon = LoadSpriteFromDiskOrEditor("Assets/Sprites/UI/Ulti.png");
            return _cachedUltiIcon;
        }

        if (item.iconName == "Star")
        {
            if (_cachedStarIcon == null)
                _cachedStarIcon = WhiteIconLoader.GetSprite("Star");
            return _cachedStarIcon;
        }

        if (item.iconName == "Hero")
        {
            if (_cachedHeroIcon == null)
                _cachedHeroIcon = WhiteIconLoader.GetSprite("Hero");
            return _cachedHeroIcon;
        }

        if (_cachedAttackIcon == null)
            _cachedAttackIcon = LoadSpriteFromDiskOrEditor("Assets/Sprites/UI/Attack.png");
        return _cachedAttackIcon;
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
        // 1. Cửa sổ chính (Main Modal Window) - Kích thước rộng rãi 1320 x 750 chuẩn 1080p
        GameObject modalObj = new GameObject("ShopModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalObj.transform.SetParent(root, false);

        RectTransform modalRt = modalObj.GetComponent<RectTransform>();
        modalRt.anchorMin = new Vector2(0.5f, 0.5f);
        modalRt.anchorMax = new Vector2(0.5f, 0.5f);
        modalRt.sizeDelta = new Vector2(1320f, 750f);
        modalRt.anchoredPosition = Vector2.zero;

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
        titleRt.anchorMin = new Vector2(0.12f, 1f);
        titleRt.anchorMax = new Vector2(0.88f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 36f);
        titleRt.anchoredPosition = new Vector2(0f, -18f);

        headerTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        headerTitleText.fontSize = 24f;
        headerTitleText.fontStyle = FontStyles.Bold;
        headerTitleText.alignment = TextAlignmentOptions.Center;
        headerTitleText.color = new Color(1f, 0.88f, 0.45f);
        SoulslikeHUDStyler.SetSafeOutline(headerTitleText, 0.45f, new Color(0.02f, 0.02f, 0.02f, 0.98f));

        // Cánh hoa văn trang trí 2 bên Tiêu đề
        CreateDividerWing(modalObj.transform, "TitleLeftWing",
            new Vector2(0.18f, 0.965f), new Vector2(0.32f, 0.985f),
            new Color(1f, 0.85f, 0.40f, 0.85f), false);

        CreateDividerWing(modalObj.transform, "TitleRightWing",
            new Vector2(0.68f, 0.965f), new Vector2(0.82f, 0.985f),
            new Color(1f, 0.85f, 0.40f, 0.85f), true);

        // Phụ đề (Subtitle)
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(modalObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.10f, 1f);
        subRt.anchorMax = new Vector2(0.90f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.sizeDelta = new Vector2(0f, 22f);
        subRt.anchoredPosition = new Vector2(0f, -54f);

        headerSubtitleText = subObj.GetComponent<TextMeshProUGUI>();
        headerSubtitleText.fontSize = 13.5f;
        headerSubtitleText.alignment = TextAlignmentOptions.Center;
        headerSubtitleText.color = new Color(0.78f, 0.86f, 0.95f);
        SoulslikeHUDStyler.SetSafeOutline(headerSubtitleText, 0.30f, Color.black);

        // Đường hoa văn phân cách đối xứng
        CreateOrnamentDivider(modalObj.transform, "HeaderDivider",
            new Vector2(0.18f, 0.882f), new Vector2(0.82f, 0.898f),
            new Color(0.95f, 0.82f, 0.40f, 0.85f));

        // Khay hiển thị số dư Mảnh Cổ Tự (Shards Counter)
        GameObject shardsObj = new GameObject("ShardsCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
        shardsObj.transform.SetParent(modalObj.transform, false);
        RectTransform shardsRt = shardsObj.GetComponent<RectTransform>();
        shardsRt.anchorMin = new Vector2(0.5f, 1f);
        shardsRt.anchorMax = new Vector2(0.5f, 1f);
        shardsRt.pivot = new Vector2(0.5f, 1f);
        shardsRt.sizeDelta = new Vector2(480f, 26f);
        shardsRt.anchoredPosition = new Vector2(0f, -94f);

        shardsCounterText = shardsObj.GetComponent<TextMeshProUGUI>();
        shardsCounterText.fontSize = 15.5f;
        shardsCounterText.fontStyle = FontStyles.Bold;
        shardsCounterText.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(shardsCounterText, 0.35f, Color.black);

        // Nút Đóng [X] góc phải trên
        GameObject closeBtnObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        closeBtnObj.transform.SetParent(modalObj.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(36f, 36f);
        closeRt.anchoredPosition = new Vector2(-18f, -18f);

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
        closeTmp.fontSize = 18f;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = new Color(1f, 0.85f, 0.4f);
        SoulslikeHUDStyler.SetSafeOutline(closeTmp, 0.35f, Color.black);

        // 3. Khung chứa 3 Thẻ Lõi Cổ Vật (Rộng rãi, tương xứng tuyệt đối với LevelUpChoiceUI)
        GameObject cardsObj = new GameObject("CardContainer", typeof(RectTransform));
        cardsObj.transform.SetParent(modalObj.transform, false);
        RectTransform cardsRt = cardsObj.GetComponent<RectTransform>();
        cardsRt.anchorMin = new Vector2(0.03f, 0.10f);
        cardsRt.anchorMax = new Vector2(0.97f, 0.84f);
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
        toastRt.anchoredPosition = new Vector2(0f, 74f);

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
        rerollRt.anchorMin = new Vector2(0.5f, 0f);
        rerollRt.anchorMax = new Vector2(0.5f, 0f);
        rerollRt.sizeDelta = new Vector2(280f, 44f);
        rerollRt.anchoredPosition = new Vector2(-160f, 22f);

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
        rerollTmp.fontSize = 15f;
        rerollTmp.fontStyle = FontStyles.Bold;
        rerollTmp.alignment = TextAlignmentOptions.Center;
        rerollTmp.color = new Color(1f, 0.88f, 0.45f);
        SoulslikeHUDStyler.SetSafeOutline(rerollTmp, 0.35f, Color.black);

        // 6. Nút "Tiếp tục qua cổng" ở chân Modal
        GameObject continueBtnObj = new GameObject("ContinueBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        continueBtnObj.transform.SetParent(modalObj.transform, false);
        RectTransform contRt = continueBtnObj.GetComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0.5f, 0f);
        contRt.anchorMax = new Vector2(0.5f, 0f);
        contRt.sizeDelta = new Vector2(280f, 44f);
        contRt.anchoredPosition = new Vector2(160f, 22f);

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
        contTmp.fontSize = 15f;
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
            categoryVN = "DUNG NHAM PHUN TRÀO", categoryEN = "MAGMA ERUPTION",
            badgeColor = new Color(1f, 0.35f, 0.10f),
            rarity = AugmentDatabase.AugmentData.Rarity.Epic,
            realmRequirement = CalamityRealm.MagmaCalamity,
            augmentType = AugmentDatabase.AugmentType.MagmaEruption,
            maxPurchases = 1,
            cost = 320,
            titleVN = "Dung Nham Bộc Phá", titleEN = "Magma Eruption",
            descVN = "Đòn đánh thứ 3 tạo vùng Dung Nham sôi sục nổ gây 40 Sát Thương mỗi 0.5s và làm chậm kẻ địch.",
            descEN = "3rd hit creates a boiling Magma Pool dealing 40 DMG per 0.5s and slowing foes.",
            flavorVN = "«Đất nứt toác dưới chân kẻ thù, magma trào dâng nuốt chửng.»",
            flavorEN = "«The earth fractures beneath foes, swallowing them in molten rock.»",
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

    // ─── Build 3 Relic Cards (Đồng bộ mỹ thuật với LevelUpChoiceUI) ────────────
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

        float cardWidth = 0.30f;
        float startX    = 0.035f;
        float gap       = (1f - startX * 2f - cardWidth * 3f) / 2f;

        for (int i = 0; i < activeChoices.Count; i++)
        {
            int idx = i;
            var item = activeChoices[i];

            float xMin = startX + i * (cardWidth + gap);
            float xMax = xMin + cardWidth;

            // Màu viền và banner phẩm chất chuẩn TFT
            string tierTitle;
            Color tierFrameColor;
            Color tierBannerBg;

            switch (item.rarity)
            {
                case AugmentDatabase.AugmentData.Rarity.Epic:
                    tierTitle = isVi ? "CỔ VẬT KIM CƯƠNG" : "PRISMATIC RELIC";
                    tierFrameColor = new Color(0.92f, 0.50f, 0.98f, 1f); // Tím Lăng Kính Ánh Tinh Vân
                    tierBannerBg   = new Color(0.32f, 0.08f, 0.42f, 0.95f);
                    break;

                case AugmentDatabase.AugmentData.Rarity.Rare:
                    tierTitle = isVi ? "CỔ VẬT VÀNG" : "GOLD RELIC";
                    tierFrameColor = new Color(1.00f, 0.82f, 0.28f, 1f); // Hoàng Kim Vương Giả
                    tierBannerBg   = new Color(0.38f, 0.22f, 0.05f, 0.95f);
                    break;

                default:
                    tierTitle = isVi ? "CỔ VẬT BẠC" : "SILVER RELIC";
                    tierFrameColor = new Color(0.80f, 0.86f, 0.95f, 0.95f); // Thép Bạc Cổ Điển
                    tierBannerBg   = new Color(0.12f, 0.16f, 0.24f, 0.95f);
                    break;
            }

            // 1. Thẻ Nâng Cấp Khung Đá Obsidian (Framed Panel)
            var card = CreateFramedPanel(cardContainer, $"Card_{item.id}",
                new Color(0.065f, 0.080f, 0.115f, 0.98f), tierFrameColor,
                new Vector2(xMin, 0.02f), new Vector2(xMax, 0.98f),
                0.38f, out Image cardFrameImg);

            var cardRT = card.GetComponent<RectTransform>();

            // 2. Banner Bậc Phẩm Chất Trên Cùng (Tier Banner) - Không ký tự sao gây lỗi ô vuông
            var tierBannerObj = new GameObject("TierBanner");
            tierBannerObj.transform.SetParent(card.transform, false);
            var tbRT = tierBannerObj.AddComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0.04f, 0.925f);
            tbRT.anchorMax = new Vector2(0.96f, 0.985f);
            tbRT.offsetMin = tbRT.offsetMax = Vector2.zero;

            var tbImg = tierBannerObj.AddComponent<Image>();
            tbImg.sprite = GetPanelBgSprite();
            tbImg.type = Image.Type.Sliced;
            tbImg.color = tierBannerBg;

            var tbOutline = tierBannerObj.AddComponent<Outline>();
            tbOutline.effectColor = tierFrameColor;
            tbOutline.effectDistance = new Vector2(1.2f, -1.2f);

            var tierTxtObj = new GameObject("TierText");
            tierTxtObj.transform.SetParent(tierBannerObj.transform, false);
            var ttRT = tierTxtObj.AddComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero;
            ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;

            var tierTxt = tierTxtObj.AddComponent<TextMeshProUGUI>();
            tierTxt.text = tierTitle;
            tierTxt.color = tierFrameColor;
            tierTxt.fontSize = 13.5f;
            tierTxt.alignment = TextAlignmentOptions.Center;
            tierTxt.fontStyle = FontStyles.Bold;
            SoulslikeHUDStyler.SetSafeOutline(tierTxt, 0.35f, new Color(0.02f, 0.02f, 0.02f, 0.95f));

            // 3. Stat Tag Pill - Bỏ hoàn toàn dấu ngoặc vuông "[ ]"
            var statTagObj = new GameObject("StatTag");
            statTagObj.transform.SetParent(card.transform, false);
            var stRT = statTagObj.AddComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.16f, 0.855f);
            stRT.anchorMax = new Vector2(0.84f, 0.912f);
            stRT.offsetMin = stRT.offsetMax = Vector2.zero;

            var stImg = statTagObj.AddComponent<Image>();
            stImg.sprite = GetStoneBtnSprite();
            stImg.type = Image.Type.Sliced;
            stImg.color = new Color(0.04f, 0.05f, 0.07f, 0.95f);

            var stOutline = statTagObj.AddComponent<Outline>();
            stOutline.effectColor = item.badgeColor;
            stOutline.effectDistance = new Vector2(1.2f, -1.2f);

            var stTxtObj = new GameObject("StatTagText");
            stTxtObj.transform.SetParent(statTagObj.transform, false);
            var sttRT = stTxtObj.AddComponent<RectTransform>();
            sttRT.anchorMin = Vector2.zero;
            sttRT.anchorMax = Vector2.one;
            sttRT.offsetMin = sttRT.offsetMax = Vector2.zero;

            var stTxt = stTxtObj.AddComponent<TextMeshProUGUI>();
            stTxt.text = isVi ? item.categoryVN : item.categoryEN;
            stTxt.color = item.badgeColor;
            stTxt.fontSize = 12.5f;
            stTxt.alignment = TextAlignmentOptions.Center;
            stTxt.fontStyle = FontStyles.Bold;
            SoulslikeHUDStyler.SetSafeOutline(stTxt, 0.30f, Color.black);

            // 4. Central Runic Medallion (Huy hiệu đá quý cổ điển)
            var medallionObj = new GameObject("Medallion");
            medallionObj.transform.SetParent(card.transform, false);
            var medRT = medallionObj.AddComponent<RectTransform>();
            medRT.anchorMin = new Vector2(0.5f, 0.725f);
            medRT.anchorMax = new Vector2(0.5f, 0.725f);
            medRT.sizeDelta = new Vector2(84f, 84f);
            medRT.pivot = new Vector2(0.5f, 0.5f);

            // Obsidian Core (Nền đá tròn)
            var coreImg = medallionObj.AddComponent<Image>();
            coreImg.sprite = GetObsidianCoreSprite();
            coreImg.color = new Color(0.08f, 0.10f, 0.14f, 1f);

            // Runic Ring (Vành kim loại chạm khắc cổ)
            var ringObj = new GameObject("RunicRing");
            ringObj.transform.SetParent(medallionObj.transform, false);
            var ringRT = ringObj.AddComponent<RectTransform>();
            ringRT.anchorMin = Vector2.zero;
            ringRT.anchorMax = Vector2.one;
            ringRT.offsetMin = ringRT.offsetMax = Vector2.zero;
            var ringImg = ringObj.AddComponent<Image>();
            ringImg.sprite = GetRunicRingSprite();
            ringImg.color = Color.Lerp(tierFrameColor, item.badgeColor, 0.40f);

            // Biểu tượng icon ở giữa: Dùng Sprite Image đồ họa thật không bao giờ bị ô vuông
            Sprite iconSprite = GetRelicSprite(item);
            if (iconSprite != null)
            {
                var iconImgObj = new GameObject("IconImg");
                iconImgObj.transform.SetParent(medallionObj.transform, false);
                var iconRT = iconImgObj.AddComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.24f, 0.24f);
                iconRT.anchorMax = new Vector2(0.76f, 0.76f);
                iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

                var iconImg = iconImgObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                bool isHeroCustomSkill = item.heroRequirement != null || (item.id != null && item.id.Contains("hero_"));
                iconImg.color = isHeroCustomSkill ? Color.white : item.badgeColor;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }
            else
            {
                var iconTxtObj = new GameObject("IconText");
                iconTxtObj.transform.SetParent(medallionObj.transform, false);
                var iconRT = iconTxtObj.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

                var iconTMP = iconTxtObj.AddComponent<TextMeshProUGUI>();
                iconTMP.text = GetStatShortCode(item);
                iconTMP.fontSize = 20f;
                iconTMP.fontStyle = FontStyles.Bold;
                iconTMP.alignment = TextAlignmentOptions.Center;
                iconTMP.color = item.badgeColor;
                SoulslikeHUDStyler.SetSafeOutline(iconTMP, 0.35f, Color.black);
            }

            // 5. Tên Cổ Vật (Item Title)
            var nameObj = new GameObject("ItemTitle");
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.05f, 0.540f);
            nameRT.anchorMax = new Vector2(0.95f, 0.635f);
            nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = isVi ? item.titleVN : item.titleEN;
            nameTMP.fontSize = 19.5f;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = new Color(1f, 0.96f, 0.88f);
            nameTMP.enableWordWrapping = true;
            SoulslikeHUDStyler.SetSafeOutline(nameTMP, 0.40f, new Color(0.05f, 0.05f, 0.05f, 0.95f));

            // 6. Ornamental Divider 1
            CreateOrnamentDivider(card.transform, "Divider1",
                new Vector2(0.08f, 0.520f), new Vector2(0.92f, 0.535f),
                tierFrameColor * 0.85f);

            // 7. Mô tả Chi Tiết + Trích dẫn phong vị tàn tích (Desc + Flavor)
            string descText = isVi ? item.descVN : item.descEN;
            string flavorText = isVi ? item.flavorVN : item.flavorEN;
            string fullDesc = descText;
            if (!string.IsNullOrEmpty(flavorText))
            {
                fullDesc += $"\n\n<size=82%><color=#94A3B8><i>{flavorText}</i></color></size>";
            }

            var descObj = new GameObject("ItemDesc");
            descObj.transform.SetParent(card.transform, false);
            var descRT = descObj.AddComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.06f, 0.270f);
            descRT.anchorMax = new Vector2(0.94f, 0.510f);
            descRT.offsetMin = descRT.offsetMax = Vector2.zero;

            var descTMP = descObj.AddComponent<TextMeshProUGUI>();
            descTMP.text = fullDesc;
            descTMP.fontSize = 14.5f;
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.color = new Color(0.92f, 0.95f, 1f);
            descTMP.enableWordWrapping = true;
            SoulslikeHUDStyler.SetSafeOutline(descTMP, 0.25f, new Color(0f, 0f, 0f, 0.85f));

            // 8. Ornamental Divider 2
            CreateOrnamentDivider(card.transform, "Divider2",
                new Vector2(0.08f, 0.250f), new Vector2(0.92f, 0.265f),
                tierFrameColor * 0.85f);

            // 9. Tiến độ Cấp Độ Cổ Vật (Upgrade Tier Progress Indicator) - Bỏ dấu ngoặc vuông "[ ]"
            var progObj = new GameObject("UpgradeTier");
            progObj.transform.SetParent(card.transform, false);
            var progRT = progObj.AddComponent<RectTransform>();
            progRT.anchorMin = new Vector2(0.06f, 0.200f);
            progRT.anchorMax = new Vector2(0.94f, 0.245f);
            progRT.offsetMin = progRT.offsetMax = Vector2.zero;

            var progTMP = progObj.AddComponent<TextMeshProUGUI>();
            progTMP.fontSize = 13.5f;
            progTMP.fontStyle = FontStyles.Bold;
            progTMP.alignment = TextAlignmentOptions.Center;
            SoulslikeHUDStyler.SetSafeOutline(progTMP, 0.30f, Color.black);
            progressTexts.Add(progTMP);

            // 10. Nút Mua (BUY BUTTON) - Khác với bên ngoài là ô MUA thay vì ô CHỌN
            var btnObj = new GameObject("BuyBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            btnObj.transform.SetParent(card.transform, false);
            var btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.08f, 0.045f);
            btnRT.anchorMax = new Vector2(0.92f, 0.190f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

            var btnImg = btnObj.GetComponent<Image>();
            btnImg.sprite = GetStoneBtnSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.16f, 0.52f, 0.35f, 0.98f);

            var bOut = btnObj.GetComponent<Outline>();
            bOut.effectColor = tierFrameColor;
            bOut.effectDistance = new Vector2(1.5f, -1.5f);

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => OnBuyClicked(idx));

            var btnTxtObj = new GameObject("BtnText");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btnTxtRT = btnTxtObj.AddComponent<RectTransform>();
            btnTxtRT.anchorMin = Vector2.zero;
            btnTxtRT.anchorMax = Vector2.one;
            btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

            var btnTMP = btnTxtObj.AddComponent<TextMeshProUGUI>();
            btnTMP.fontSize = 16f;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.color = Color.white;
            SoulslikeHUDStyler.SetSafeOutline(btnTMP, 0.40f, new Color(0f, 0f, 0f, 0.95f));

            cardObjects.Add(card);
            buyButtons.Add(btn);
            buyButtonTexts.Add(btnTMP);

            // Hiệu ứng Hover tương tác
            SetupCardHover(card, cardRT, cardFrameImg, tierFrameColor);
        }

        RefreshUI();
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
            var badge = card.transform.Find("StatTag/StatTagText")?.GetComponent<TextMeshProUGUI>();
            if (badge != null)
            {
                badge.text = isVi ? item.categoryVN : item.categoryEN;
            }

            var title = card.transform.Find("ItemTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.text = isVi ? item.titleVN : item.titleEN;

            var desc = card.transform.Find("ItemDesc")?.GetComponent<TextMeshProUGUI>();
            if (desc != null)
            {
                string descText = isVi ? item.descVN : item.descEN;
                string flavorText = isVi ? item.flavorVN : item.flavorEN;
                string fullDesc = descText;
                if (!string.IsNullOrEmpty(flavorText))
                {
                    fullDesc += $"\n\n<size=82%><color=#94A3B8><i>{flavorText}</i></color></size>";
                }
                desc.text = fullDesc;
            }

            // Tiến độ Cấp (Không dấu "[ ]")
            if (i < progressTexts.Count && progressTexts[i] != null)
            {
                if (item.isInfinite)
                {
                    progressTexts[i].text = isVi
                        ? $"<color=#55EFC4>TIÊN ĐAN VÔ HẠN (ĐÃ MUA {item.purchaseCount} LẦN)</color>"
                        : $"<color=#55EFC4>ENDLESS ELIXIR (PURCHASED x{item.purchaseCount})</color>";
                }
                else
                {
                    bool isMax = (item.purchaseCount >= item.maxPurchases);
                    if (isMax)
                    {
                        progressTexts[i].text = isVi
                            ? $"<color=#FBBF24>CẤP TỐI ĐA ({item.maxPurchases}/{item.maxPurchases})</color>"
                            : $"<color=#FBBF24>MAX LEVEL ({item.maxPurchases}/{item.maxPurchases})</color>";
                    }
                    else
                    {
                        progressTexts[i].text = isVi
                            ? $"<color=#CBD5E1>TIẾN ĐỘ: {item.purchaseCount}/{item.maxPurchases}</color>"
                            : $"<color=#CBD5E1>PROGRESS: {item.purchaseCount}/{item.maxPurchases}</color>";
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
