// ========================================================
// VAELSTRIDER: RUINBOUND - Sanctuary Supply & Summon Shop
// Sells Gacha Scrolls, Materials, Chests & Supplies with direct
// link to the Destiny Gacha Altar in Inventory.
// ========================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ArmorShopUI : MonoBehaviour
{
    private static ArmorShopUI instance;
    public static ArmorShopUI Instance => instance;

    public enum EquipmentSlot
    {
        Helmet,
        Armor,
        Pants,
        Gloves,
        Boots,
        Cloak,
        Bag
    }

    public enum ArmorTier
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public static void InitDatabase() { }

    public static int GetEquippedIdForSlot(EquipmentSlot slot)
    {
        string key = "Equipped_Armor_Slot_" + (int)slot;
        return PlayerPrefs.GetInt(key, 0);
    }

    public static void GetEquippedArmorBonus(out int hpBonus, out float defPercent, out float speedBonus)
    {
        GetEquippedArmorBonus(out hpBonus, out defPercent, out speedBonus, out _, out _);
    }

    public static void GetEquippedArmorBonus(out int hpBonus, out float defPercent, out float speedBonus, out float manaBonus, out float manaRegenBonus)
    {
        hpBonus = 0;
        defPercent = 0f;
        speedBonus = 0f;
        manaBonus = 0f;
        manaRegenBonus = 0f;
        for (int i = 0; i <= 6; i++)
        {
            int eqId = PlayerPrefs.GetInt("Equipped_Armor_Slot_" + i, 0);
            if (eqId > 0)
            {
                if (eqId >= 51) { hpBonus += 30; defPercent += 0.08f; speedBonus += 0.2f; manaBonus += 15f; manaRegenBonus += 1.0f; }
                else if (eqId >= 41) { hpBonus += 20; defPercent += 0.05f; speedBonus += 0.1f; manaBonus += 10f; manaRegenBonus += 0.7f; }
                else if (eqId >= 27) { hpBonus += 15; defPercent += 0.03f; manaBonus += 8f; manaRegenBonus += 0.5f; }
                else if (eqId >= 13) { hpBonus += 10; defPercent += 0.02f; manaBonus += 5f; manaRegenBonus += 0.3f; }
                else { hpBonus += 5; defPercent += 0.01f; manaBonus += 3f; manaRegenBonus += 0.15f; }
            }
        }
    }

    public enum ShopCategory
    {
        Minerals = 0,    // Khoáng Thạch & Đá Quý (14 loại đá độc nhất)
        Scrolls = 1,     // Bùa Chú & Cổ Vật (Sắp ra mắt)
        Potions = 2,     // Dược Phẩm & Thánh Thủy (Sắp ra mắt)
        Equipment = 3    // Trang Bị & Thần Binh (Sắp ra mắt)
    }

    [System.Serializable]
    public class ShopSupplyItem
    {
        public string itemId;
        public string nameVi;
        public string nameEn;
        public string descVi;
        public string descEn;
        public int quantity;
        public int priceShards;
        public string iconPath;
        public ShopCategory category = ShopCategory.Minerals;
        public string badgeTextVi;
        public string badgeTextEn;
        public Color badgeColor;

        private Sprite _cachedSprite = null;
        public Sprite Icon
        {
            get
            {
                if (_cachedSprite != null) return _cachedSprite;
                
                // 1. Try lookup from InventoryItemDatabase first
                var itemDef = InventoryItemDatabase.GetItem(itemId);
                if (itemDef != null && itemDef.Icon != null)
                {
                    _cachedSprite = itemDef.Icon;
                    return _cachedSprite;
                }

                // 2. Try loading from iconPath
                if (!string.IsNullOrEmpty(iconPath))
                {
                    _cachedSprite = Resources.Load<Sprite>(iconPath);
#if UNITY_EDITOR
                    if (_cachedSprite == null)
                    {
                        _cachedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                    }
#endif
                    if (_cachedSprite == null)
                    {
                        string fullDiskPath = iconPath.StartsWith("Assets") ? System.IO.Path.Combine(Application.dataPath, iconPath.Substring(6).TrimStart('/', '\\')) : iconPath;
                        if (System.IO.File.Exists(fullDiskPath))
                        {
                            byte[] bytes = System.IO.File.ReadAllBytes(fullDiskPath);
                            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (tex.LoadImage(bytes))
                            {
                                tex.filterMode = FilterMode.Point;
                                _cachedSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                            }
                        }
                    }
                }
                return _cachedSprite;
            }
        }
    }

    private static List<ShopSupplyItem> shopItems = new List<ShopSupplyItem>();
    private static bool isDatabaseInitialized = false;

    [Header("UI References")]
    private CanvasGroup canvasGroup;
    private GameObject dialogBox;
    private TextMeshProUGUI shopTitleText;
    private TextMeshProUGUI shopSubtitleText;
    private Transform gridContent;
    private ScrollRect scrollRect;
    private GameObject emptyCategoryNoticeObj;

    // Tabs
    private ShopCategory currentCategory = ShopCategory.Minerals;
    private List<Button> tabButtons = new List<Button>();
    private List<Image> tabBtnImgs = new List<Image>();
    private List<TextMeshProUGUI> tabBtnTexts = new List<TextMeshProUGUI>();

    // Balance
    private TextMeshProUGUI balanceText;

    // Right Inspector Panel
    private Image detailIconBoxImg;
    private Image detailIconImg;
    private TextMeshProUGUI detailNameText;
    private TextMeshProUGUI detailBadgeText;
    private TextMeshProUGUI detailDescText;
    private TextMeshProUGUI detailPriceText;
    private Button buyActionButton;
    private TextMeshProUGUI buyActionBtnText;
    private Image buyActionBtnImg;
    private Button goToGachaBtn;

    // Multipliers
    private int selectedMultiplierMode = 1; // 1, 5, 10, 20, or -1 (MAX)
    private readonly int[] multiplierValues = new int[] { 1, 5, 10, 20, -1 };
    private List<Button> multiplierBtns = new List<Button>();
    private List<Image> multiplierBtnImgs = new List<Image>();
    private List<Outline> multiplierBtnOutlines = new List<Outline>();
    private List<TextMeshProUGUI> multiplierBtnTexts = new List<TextMeshProUGUI>();

    private int selectedShopIndex = 0;
    private List<GameObject> cardGameObjects = new List<GameObject>();
    private List<ShopSupplyItem> currentFilteredItems = new List<ShopSupplyItem>();
    private bool isOpen = false;

    // Frame Sprites (Only applied to Item Icon Frames)
    private static Sprite sprItemSlotFrame = null;

    private static Sprite LoadFramedSprite(string relativePath, Vector4 border)
    {
        Sprite spr = null;
#if UNITY_EDITOR
        string fullAssetPath = "Assets/ART/UI/Default/" + relativePath;
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fullAssetPath);
        if (assets != null)
        {
            foreach (var obj in assets)
            {
                if (obj is Sprite s && (s.name.EndsWith("_1") || s.name == System.IO.Path.GetFileNameWithoutExtension(relativePath)))
                {
                    spr = s;
                    break;
                }
            }
            if (spr == null)
            {
                spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fullAssetPath);
            }
        }
#endif
        if (spr == null)
        {
            string filePath = System.IO.Path.Combine(Application.dataPath, "ART/UI/Default/" + relativePath);
            if (System.IO.File.Exists(filePath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    Rect r = (tex.width >= 48) ? new Rect(1, 1, tex.width - 2, tex.height - 2) : new Rect(0, 0, tex.width, tex.height);
                    spr = Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                }
            }
        }
        return spr;
    }

    private static void EnsureSprites()
    {
        if (sprItemSlotFrame == null) sprItemSlotFrame = LoadFramedSprite("Border/panel-border-014.png", new Vector4(8, 8, 8, 8));
        if (sprItemSlotFrame == null) sprItemSlotFrame = LoadFramedSprite("Border/panel-border-000.png", new Vector4(8, 8, 8, 8));
    }

    public static ArmorShopUI EnsureUIExists()
    {
        if (instance != null) return instance;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        var existing = canvas.transform.Find("ArmorShopModal");
        if (existing != null)
        {
            instance = existing.GetComponent<ArmorShopUI>();
            if (instance != null)
            {
                if (instance.dialogBox == null) instance.BuildUI(canvas);
                return instance;
            }
        }

        GameObject shopObj = new GameObject("ArmorShopModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shopObj.layer = 5;
        shopObj.transform.SetParent(canvas.transform, false);

        instance = shopObj.AddComponent<ArmorShopUI>();
        instance.BuildUI(canvas);
        shopObj.SetActive(false);
        return instance;
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        gameObject.layer = 5;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        InitShopDatabase();
        EnsureSprites();
        if (dialogBox == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
            if (parentCanvas != null) BuildUI(parentCanvas);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshUI;
        LocalizationManager.OnLanguageChanged += RefreshUI;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshUI;
    }

    private void InitShopDatabase()
    {
        if (isDatabaseInitialized) return;
        isDatabaseInitialized = true;
        shopItems.Clear();

        // =========================================================================
        // MỤC: KHOÁNG THẠCH & ĐÁ QUÝ (Tất cả 14 loại đá độc nhất từ Assets/ART/Item/Nodes)
        // Tuyệt đối không trùng hình, không chứa icon khiên
        // =========================================================================

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "gem_amethyst",
            nameVi = "Thạch Anh Tím Hư Không (x5)",
            nameEn = "Void Amethyst Crystal (x5)",
            descVi = "Khoáng thạch tím phát sáng mang năng lượng ma thuật cổ xưa, dùng để tinh luyện thần binh và đổi Shards.",
            descEn = "Luminous violet gemstone infused with ancient magic for weapon upgrades or shard exchange.",
            quantity = 5,
            priceShards = 120,
            iconPath = "Assets/ART/Item/Nodes/amethystNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "ĐÁ QUÝ",
            badgeTextEn = "GEM",
            badgeColor = new Color(0.75f, 0.3f, 0.95f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "gem_bloodstone",
            nameVi = "Huyết Thạch Ma Thuật (x5)",
            nameEn = "Bloodstone Crystal (x5)",
            descVi = "Viên đá đỏ rực rỉ ra tử khí tàn tích, vật phẩm cực kỳ quý giá dùng để nâng cấp thuộc tính trang bị.",
            descEn = "A crimson gemstone radiating demonic energy, essential for gear attribute enhancements.",
            quantity = 5,
            priceShards = 150,
            iconPath = "Assets/ART/Item/Nodes/bloodstoneNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "HUYẾT THẠCH",
            badgeTextEn = "MYTHIC",
            badgeColor = new Color(1f, 0.2f, 0.25f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "gem_jade",
            nameVi = "Ngọc Bích Thượng Cổ (x5)",
            nameEn = "Ancient Jade Gem (x5)",
            descVi = "Viên ngọc bích tỏa ra ánh sáng phục hồi dịu nhẹ, chứa đựng năng lượng thiên nhiên nguyên thủy.",
            descEn = "A pristine jade gem emitting calming light, containing pure primordial nature energy.",
            quantity = 5,
            priceShards = 110,
            iconPath = "Assets/ART/Item/Nodes/jadeNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "NGỌC BÍCH",
            badgeTextEn = "JADE",
            badgeColor = new Color(0.2f, 0.9f, 0.5f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_gold_node",
            nameVi = "Quặng Vàng Hoàng Kim (x5)",
            nameEn = "Royal Gold Chunk (x5)",
            descVi = "Khối quặng vàng ròng lấp lánh, có thể bán để quy đổi lấy lượng lớn Aethel Shards.",
            descEn = "A sparkling chunk of pure royal gold, can be converted into abundant Aethel Shards.",
            quantity = 5,
            priceShards = 180,
            iconPath = "Assets/ART/Item/Nodes/goldNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "VÀNG RÒNG",
            badgeTextEn = "GOLD",
            badgeColor = new Color(1f, 0.85f, 0.2f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_tin",
            nameVi = "Quặng Thiếc Lam Hư Không (x10)",
            nameEn = "Cyan Tin Ore (x10)",
            descVi = "Khối quặng thiếc tỏa ánh sáng lam dịu mát, vật liệu dẫn ma lực cao cấp cho các vũ khí phép thuật.",
            descEn = "A light-blue tin mineral radiating mana resonance, vital for magical artifact conductivity.",
            quantity = 10,
            priceShards = 90,
            iconPath = "Assets/ART/Item/Nodes/tinNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "MA LỰC",
            badgeTextEn = "MANA",
            badgeColor = new Color(0.3f, 0.85f, 1f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_forge_stone",
            nameVi = "Đá Luyện Kim Thượng Hạng (x10)",
            nameEn = "Master Forge Stone (x10)",
            descVi = "Vật liệu rèn quý hiếm dùng để tinh luyện trang bị và nâng cấp vũ khí tại lò rèn hoàng gia.",
            descEn = "Rare crafting mineral used for weapon tempering and equipment enhancement.",
            quantity = 10,
            priceShards = 80,
            iconPath = "Assets/ART/Item/Nodes/forgeStoneNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "RÈN ĐÚC",
            badgeTextEn = "FORGE",
            badgeColor = new Color(0.3f, 0.7f, 1f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_copper",
            nameVi = "Quặng Đồng Đỏ Cổ Xưa (x10)",
            nameEn = "Ancient Copper Ore (x10)",
            descVi = "Quặng đồng đỏ đúc tiền tệ và hợp kim bền dẻo thời kỳ đế chế hoàng kim.",
            descEn = "A rich copper deposit used in ancient metallurgy for resilient weapons and alloys.",
            quantity = 10,
            priceShards = 60,
            iconPath = "Assets/ART/Item/Nodes/copperNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "KHOÁNG SẢN",
            badgeTextEn = "MINERAL",
            badgeColor = new Color(0.9f, 0.5f, 0.2f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_iron",
            nameVi = "Quặng Sắt Hắc Ám (x10)",
            nameEn = "Dark Iron Ore (x10)",
            descVi = "Khối sắt đen nguyên khối cứng hơn thép thường gấp 10 lần, cốt lõi để rèn giáp trụ nặng.",
            descEn = "Dense black iron ore ten times harder than common steel, crucial for heavy plate armor.",
            quantity = 10,
            priceShards = 70,
            iconPath = "Assets/ART/Item/Nodes/ironNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "SẮT ĐEN",
            badgeTextEn = "IRON",
            badgeColor = new Color(0.7f, 0.75f, 0.85f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_coal",
            nameVi = "Hắc Than Nhiệt Lượng (x15)",
            nameEn = "Blazing Dark Coal (x15)",
            descVi = "Nhiên liệu than đen cháy ở nhiệt độ cực cao, duy trì ngọn lửa vĩnh cửu trong lò rèn hoàng gia.",
            descEn = "High-potency coal burning at immense heat to power the eternal forge fires.",
            quantity = 15,
            priceShards = 45,
            iconPath = "Assets/ART/Item/Nodes/coalNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "NHIÊN LIỆU",
            badgeTextEn = "FUEL",
            badgeColor = new Color(0.5f, 0.5f, 0.55f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_keep_stone",
            nameVi = "Đá Cổ Trấn Cung (x5)",
            nameEn = "Ancient Keepstone (x5)",
            descVi = "Mảnh vỡ từ tường thành của đế chế cổ, mang cấu trúc vật lý kiên cố gia tăng chỉ số phòng thủ.",
            descEn = "A piece of fallen ruin fortress masonry, extremely durable and reinforces hero defense.",
            quantity = 5,
            priceShards = 55,
            iconPath = "Assets/ART/Item/Nodes/keepStoneNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "PHÒNG HỘ",
            badgeTextEn = "DEFENSE",
            badgeColor = new Color(0.4f, 0.6f, 0.8f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_boulder_stone",
            nameVi = "Cự Thạch Cổ Vạn Năm (x2)",
            nameEn = "Ancient Ruin Megalith (x2)",
            descVi = "Khối cự thạch chứa năng lượng đất mẹ kiên cố, dùng để đúc khiên và thành trì phòng hộ.",
            descEn = "A massive megalith carrying primal earth resilience for heavy shield reinforcement.",
            quantity = 2,
            priceShards = 65,
            iconPath = "Assets/ART/Item/Nodes/boulderNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "CỰ THẠCH",
            badgeTextEn = "BOULDER",
            badgeColor = new Color(0.6f, 0.65f, 0.55f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_medium_rock",
            nameVi = "Đá Granite Tàn Tích (x10)",
            nameEn = "Ruin Granite Ore (x10)",
            descVi = "Mảnh đá hoa cương kết tinh từ nham thạch nguội, độ cứng cao và chống va đập tốt.",
            descEn = "Crystallized volcanic granite with high durability and impact resistance.",
            quantity = 10,
            priceShards = 50,
            iconPath = "Assets/ART/Item/Nodes/mediumRockNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "GRANITE",
            badgeTextEn = "ROCK",
            badgeColor = new Color(0.65f, 0.7f, 0.75f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_small_rocks",
            nameVi = "Sỏi Cổ Tụ Linh (x20)",
            nameEn = "Spirit Rune Pebbles (x20)",
            descVi = "Những viên sỏi tích tụ tinh hoa đất trời trong các khe nứt tàn tích, vật phẩm phụ trợ cường hóa.",
            descEn = "Spiritual pebbles infused with ambient ruin mana for auxiliary alchemy.",
            quantity = 20,
            priceShards = 40,
            iconPath = "Assets/ART/Item/Nodes/smallRocks.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "SỎI LINH",
            badgeTextEn = "PEBBLES",
            badgeColor = new Color(0.7f, 0.8f, 0.6f)
        });

        shopItems.Add(new ShopSupplyItem
        {
            itemId = "ore_stone",
            nameVi = "Đá Cổ Trầm Tích (x20)",
            nameEn = "Sedimentary Ruin Stone (x20)",
            descVi = "Đá trầm tích cơ bản của vùng đất Vaelstrider, vật liệu xây dựng và chế tác cơ bản nhất.",
            descEn = "Standard ruin bedrock material used for basic masonry and beginner weapon polishing.",
            quantity = 20,
            priceShards = 30,
            iconPath = "Assets/ART/Item/Nodes/stoneNode.png",
            category = ShopCategory.Minerals,
            badgeTextVi = "CƠ BẢN",
            badgeTextEn = "BASIC",
            badgeColor = new Color(0.6f, 0.6f, 0.6f)
        });
    }

    public void BuildUI(Canvas canvas)
    {
        InitShopDatabase();
        EnsureSprites();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        RectTransform rootRt = GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Image bgDim = GetComponent<Image>();
        bgDim.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        bgDim.raycastTarget = true;

        // Modal Frame - 100% Edge-to-Edge Full Screen
        dialogBox = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(transform, false);
        RectTransform dbRt = dialogBox.GetComponent<RectTransform>();
        dbRt.anchorMin = Vector2.zero;
        dbRt.anchorMax = Vector2.one;
        dbRt.pivot = new Vector2(0.5f, 0.5f);
        dbRt.offsetMin = Vector2.zero;
        dbRt.offsetMax = Vector2.zero;

        Image dbImg = dialogBox.GetComponent<Image>();
        dbImg.color = new Color(0.04f, 0.05f, 0.09f, 1f);
        WhiteIconLoader.ApplyGothicFrame(dialogBox, new Color(0.85f, 0.72f, 0.38f, 0.90f), 0.35f);

        // Header Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.layer = 5;
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -25f);
        tRt.sizeDelta = new Vector2(0f, 40f);
        shopTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        shopTitleText.fontSize = 28;
        shopTitleText.fontStyle = FontStyles.Bold;
        shopTitleText.alignment = TextAlignmentOptions.Center;
        shopTitleText.color = new Color(1f, 0.88f, 0.45f);
        shopTitleText.text = LocalizationManager.T("TIỆM TIẾP TẾ & KHOÁNG SẢN HOÀNG GIA", "ROYAL SUPPLIES & MINERALS EMPORIUM");

        // Close Button (Top Right)
        GameObject closeBtnObj = new GameObject("Btn_CloseShop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(dialogBox.transform, false);
        RectTransform cbRt = closeBtnObj.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1f, 1f);
        cbRt.anchorMax = new Vector2(1f, 1f);
        cbRt.pivot = new Vector2(1f, 1f);
        cbRt.anchoredPosition = new Vector2(-30f, -30f);
        cbRt.sizeDelta = new Vector2(52f, 52f);

        Image cbImg = closeBtnObj.GetComponent<Image>();
        cbImg.color = new Color(0.75f, 0.22f, 0.25f, 1f);
        var cbOutline = closeBtnObj.AddComponent<Outline>();
        cbOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.7f);
        cbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = cbImg;
        closeBtn.onClick.AddListener(Close);
        WhiteIconLoader.ReplaceTextWithIcon(closeBtn, "Cross", Color.white, new Vector2(24f, 24f));

        // Category Tab Bar (Shifted up to -70f now that subtitle is removed)
        BuildCategoryTabs(dialogBox.transform);

        // Left Grid Area
        BuildShopScrollView(dialogBox.transform);

        // Right Inspector Panel
        BuildShopInspectorPanel(dialogBox.transform);

        RefreshUI();
    }

    private void BuildCategoryTabs(Transform parent)
    {
        tabButtons.Clear();
        tabBtnImgs.Clear();
        tabBtnTexts.Clear();

        GameObject tabArea = new GameObject("CategoryTabBar", typeof(RectTransform));
        tabArea.layer = 5;
        tabArea.transform.SetParent(parent, false);

        RectTransform taRt = tabArea.GetComponent<RectTransform>();
        taRt.anchorMin = new Vector2(0.5f, 1f);
        taRt.anchorMax = new Vector2(0.5f, 1f);
        taRt.pivot = new Vector2(0.5f, 1f);
        taRt.anchoredPosition = new Vector2(0f, -72f);
        taRt.sizeDelta = new Vector2(920f, 44f);

        (ShopCategory cat, string vi, string en)[] tabs = new[]
        {
            (ShopCategory.Minerals, "KHOÁNG THẠCH", "MINERALS & GEMS"),
            (ShopCategory.Scrolls, "BÙA CHÚ", "SCROLLS & RELICS"),
            (ShopCategory.Potions, "DƯỢC LIỆU", "POTIONS"),
            (ShopCategory.Equipment, "TRANG BỊ", "EQUIPMENT")
        };

        float tabWidth = 220f;
        float spacing = 10f;
        float startX = -((tabs.Length - 1) * (tabWidth + spacing)) / 2f;

        for (int i = 0; i < tabs.Length; i++)
        {
            var tabInfo = tabs[i];
            GameObject tBtnObj = new GameObject($"Tab_{tabInfo.cat}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tBtnObj.layer = 5;
            tBtnObj.transform.SetParent(tabArea.transform, false);

            RectTransform tbRt = tBtnObj.GetComponent<RectTransform>();
            tbRt.anchoredPosition = new Vector2(startX + i * (tabWidth + spacing), 0f);
            tbRt.sizeDelta = new Vector2(tabWidth, 44f);

            Image tImg = tBtnObj.GetComponent<Image>();
            tImg.color = (tabInfo.cat == currentCategory) ? new Color(0.2f, 0.45f, 0.75f, 0.95f) : new Color(0.08f, 0.1f, 0.15f, 0.85f);
            var tOutline = tBtnObj.AddComponent<Outline>();
            tOutline.effectColor = (tabInfo.cat == currentCategory) ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.3f, 0.4f, 0.55f, 0.5f);
            tOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button tBtn = tBtnObj.GetComponent<Button>();
            tBtn.targetGraphic = tImg;
            tBtn.onClick.AddListener(() => SwitchCategory(tabInfo.cat));

            GameObject tTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            tTxtObj.layer = 5;
            tTxtObj.transform.SetParent(tBtnObj.transform, false);
            RectTransform ttRt = tTxtObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.sizeDelta = Vector2.zero;

            var tTmp = tTxtObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = LocalizationManager.T(tabInfo.vi, tabInfo.en);
            tTmp.fontSize = 16.5f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = (tabInfo.cat == currentCategory) ? Color.white : new Color(0.7f, 0.75f, 0.85f);

            tabButtons.Add(tBtn);
            tabBtnImgs.Add(tImg);
            tabBtnTexts.Add(tTmp);
        }
    }

    private void SwitchCategory(ShopCategory newCat)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        currentCategory = newCat;
        selectedShopIndex = 0;
        RefreshCategoryTabsVisual();
        RefreshUI();
    }

    private void RefreshCategoryTabsVisual()
    {
        (ShopCategory cat, string vi, string en)[] tabs = new[]
        {
            (ShopCategory.Minerals, "KHOÁNG THẠCH", "MINERALS & GEMS"),
            (ShopCategory.Scrolls, "BÙA CHÚ", "SCROLLS & RELICS"),
            (ShopCategory.Potions, "DƯỢC LIỆU", "POTIONS"),
            (ShopCategory.Equipment, "TRANG BỊ", "EQUIPMENT")
        };

        for (int i = 0; i < tabBtnImgs.Count; i++)
        {
            bool isSel = (i == (int)currentCategory);
            if (tabBtnImgs[i] != null)
            {
                tabBtnImgs[i].color = isSel ? new Color(0.2f, 0.45f, 0.75f, 0.95f) : new Color(0.08f, 0.1f, 0.15f, 0.85f);
                var outline = tabBtnImgs[i].GetComponent<Outline>();
                if (outline != null) outline.effectColor = isSel ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.3f, 0.4f, 0.55f, 0.5f);
            }
            if (tabBtnTexts[i] != null)
            {
                if (i < tabs.Length) tabBtnTexts[i].text = LocalizationManager.T(tabs[i].vi, tabs[i].en);
                tabBtnTexts[i].color = isSel ? Color.white : new Color(0.7f, 0.75f, 0.85f);
            }
        }
    }

    private void BuildShopScrollView(Transform parent)
    {
        GameObject gridAreaObj = new GameObject("ShopScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        gridAreaObj.layer = 5;
        gridAreaObj.transform.SetParent(parent, false);

        RectTransform scrollRt = gridAreaObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(0.63f, 1f);
        scrollRt.pivot = new Vector2(0.5f, 0.5f);
        scrollRt.offsetMin = new Vector2(30f, 25f);
        scrollRt.offsetMax = new Vector2(-10f, -128f);

        Image scrollBg = gridAreaObj.GetComponent<Image>();
        scrollBg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
        WhiteIconLoader.ApplyGothicFrame(gridAreaObj, new Color(0.25f, 0.35f, 0.50f, 0.60f), 0.50f);

        scrollRect = gridAreaObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportObj.layer = 5;
        viewportObj.transform.SetParent(gridAreaObj.transform, false);
        RectTransform vpRt = viewportObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero;

        Image vpImg = viewportObj.GetComponent<Image>();
        vpImg.color = Color.white;
        var mask = viewportObj.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObj.layer = 5;
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        var grid = contentObj.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(252f, 132f);
        grid.spacing = new Vector2(14f, 12f);
        grid.padding = new RectOffset(16, 16, 14, 14);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        gridContent = contentObj.transform;
        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;

        // Empty category notice
        emptyCategoryNoticeObj = new GameObject("EmptyCategoryNotice", typeof(RectTransform), typeof(CanvasRenderer));
        emptyCategoryNoticeObj.layer = 5;
        emptyCategoryNoticeObj.transform.SetParent(gridAreaObj.transform, false);
        RectTransform enRt = emptyCategoryNoticeObj.GetComponent<RectTransform>();
        enRt.anchorMin = Vector2.zero;
        enRt.anchorMax = Vector2.one;
        enRt.sizeDelta = Vector2.zero;
        var enTmp = emptyCategoryNoticeObj.AddComponent<TextMeshProUGUI>();
        enTmp.text = LocalizationManager.T("Danh mục này đang được chuẩn bị,\nsẽ sớm cập nhật trong bản tới!", "This category is coming soon in the next update!");
        enTmp.fontSize = 16.5f;
        enTmp.fontStyle = FontStyles.Bold;
        enTmp.alignment = TextAlignmentOptions.Center;
        enTmp.color = new Color(0.6f, 0.7f, 0.85f, 0.75f);
        emptyCategoryNoticeObj.SetActive(false);
    }

    private void BuildShopInspectorPanel(Transform parent)
    {
        GameObject inspObj = new GameObject("ShopInspector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inspObj.layer = 5;
        inspObj.transform.SetParent(parent, false);

        RectTransform rt = inspObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.64f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(10f, 25f);
        rt.offsetMax = new Vector2(-25f, -128f);

        Image img = inspObj.GetComponent<Image>();
        img.color = new Color(0.05f, 0.06f, 0.10f, 0.98f);
        WhiteIconLoader.ApplyGothicFrame(inspObj, new Color(0.85f, 0.72f, 0.35f, 0.75f), 0.50f);

        // Big Icon Box with Framed Sprite
        GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconBox.layer = 5;
        iconBox.transform.SetParent(inspObj.transform, false);
        RectTransform iconBoxRt = iconBox.GetComponent<RectTransform>();
        iconBoxRt.anchorMin = new Vector2(0.5f, 1f);
        iconBoxRt.anchorMax = new Vector2(0.5f, 1f);
        iconBoxRt.pivot = new Vector2(0.5f, 1f);
        iconBoxRt.anchoredPosition = new Vector2(0f, -20f);
        iconBoxRt.sizeDelta = new Vector2(120f, 120f);
        
        detailIconBoxImg = iconBox.GetComponent<Image>();
        if (sprItemSlotFrame != null)
        {
            detailIconBoxImg.sprite = sprItemSlotFrame;
            detailIconBoxImg.type = Image.Type.Sliced;
        }
        detailIconBoxImg.color = new Color(1f, 0.85f, 0.35f, 1f);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.layer = 5;
        iconObj.transform.SetParent(iconBox.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = new Vector2(-18f, -18f);
        detailIconImg = iconObj.GetComponent<Image>();
        detailIconImg.preserveAspect = true;

        // Name
        GameObject nameObj = new GameObject("ItemName", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.layer = 5;
        nameObj.transform.SetParent(inspObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -152f);
        nameRt.sizeDelta = new Vector2(0f, 38f);
        detailNameText = nameObj.AddComponent<TextMeshProUGUI>();
        detailNameText.fontSize = 28f;
        detailNameText.fontStyle = FontStyles.Bold;
        detailNameText.alignment = TextAlignmentOptions.Center;
        detailNameText.color = new Color(1f, 0.9f, 0.45f);

        // Badge
        GameObject badgeObj = new GameObject("BadgeText", typeof(RectTransform), typeof(CanvasRenderer));
        badgeObj.layer = 5;
        badgeObj.transform.SetParent(inspObj.transform, false);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 1f);
        badgeRt.anchorMax = new Vector2(1f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 1f);
        badgeRt.anchoredPosition = new Vector2(0f, -194f);
        badgeRt.sizeDelta = new Vector2(0f, 28f);
        detailBadgeText = badgeObj.AddComponent<TextMeshProUGUI>();
        detailBadgeText.fontSize = 18f;
        detailBadgeText.fontStyle = FontStyles.Bold;
        detailBadgeText.alignment = TextAlignmentOptions.Center;

        // Description Card (Middle container)
        GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(CanvasRenderer));
        descObj.layer = 5;
        descObj.transform.SetParent(inspObj.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.06f, 0.44f);
        descRt.anchorMax = new Vector2(0.94f, 0.64f);
        descRt.offsetMin = Vector2.zero;
        descRt.offsetMax = Vector2.zero;
        detailDescText = descObj.AddComponent<TextMeshProUGUI>();
        detailDescText.fontSize = 17.5f;
        detailDescText.alignment = TextAlignmentOptions.Center;
        detailDescText.color = new Color(0.85f, 0.9f, 0.98f);
        detailDescText.enableWordWrapping = true;
        detailDescText.overflowMode = TextOverflowModes.Ellipsis;
        detailDescText.lineSpacing = 4f;

        // Price Text
        GameObject prcObj = new GameObject("PriceText", typeof(RectTransform), typeof(CanvasRenderer));
        prcObj.layer = 5;
        prcObj.transform.SetParent(inspObj.transform, false);
        RectTransform prcRt = prcObj.GetComponent<RectTransform>();
        prcRt.anchorMin = new Vector2(0f, 0f);
        prcRt.anchorMax = new Vector2(1f, 0f);
        prcRt.pivot = new Vector2(0.5f, 0f);
        prcRt.anchoredPosition = new Vector2(0f, 235f);
        prcRt.sizeDelta = new Vector2(0f, 34f);
        detailPriceText = prcObj.AddComponent<TextMeshProUGUI>();
        detailPriceText.fontSize = 22f;
        detailPriceText.fontStyle = FontStyles.Bold;
        detailPriceText.alignment = TextAlignmentOptions.Center;
        detailPriceText.color = new Color(1f, 0.85f, 0.3f);
        detailPriceText.enableAutoSizing = true;
        detailPriceText.fontSizeMin = 17f;
        detailPriceText.fontSizeMax = 22f;

        // Balance Display
        GameObject balObj = new GameObject("BalanceText", typeof(RectTransform), typeof(CanvasRenderer));
        balObj.layer = 5;
        balObj.transform.SetParent(inspObj.transform, false);
        RectTransform balRt = balObj.GetComponent<RectTransform>();
        balRt.anchorMin = new Vector2(0f, 0f);
        balRt.anchorMax = new Vector2(1f, 0f);
        balRt.pivot = new Vector2(0.5f, 0f);
        balRt.anchoredPosition = new Vector2(0f, 198f);
        balRt.sizeDelta = new Vector2(0f, 30f);
        balanceText = balObj.AddComponent<TextMeshProUGUI>();
        balanceText.fontSize = 20f;
        balanceText.fontStyle = FontStyles.Bold;
        balanceText.alignment = TextAlignmentOptions.Center;
        balanceText.color = new Color(0.35f, 0.85f, 1f);
        balanceText.enableAutoSizing = true;
        balanceText.fontSizeMin = 16f;
        balanceText.fontSizeMax = 20f;

        // Multiplier Buttons Row (x1, x5, x10, x20, MAX)
        GameObject multRowObj = new GameObject("MultiplierRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(HorizontalLayoutGroup));
        multRowObj.layer = 5;
        multRowObj.transform.SetParent(inspObj.transform, false);
        RectTransform multRt = multRowObj.GetComponent<RectTransform>();
        multRt.anchorMin = new Vector2(0.5f, 0f);
        multRt.anchorMax = new Vector2(0.5f, 0f);
        multRt.pivot = new Vector2(0.5f, 0f);
        multRt.anchoredPosition = new Vector2(0f, 142f);
        multRt.sizeDelta = new Vector2(380f, 44f);

        var multLayout = multRowObj.GetComponent<HorizontalLayoutGroup>();
        multLayout.spacing = 8f;
        multLayout.childAlignment = TextAnchor.MiddleCenter;
        multLayout.childControlWidth = true;
        multLayout.childControlHeight = true;
        multLayout.childForceExpandWidth = true;
        multLayout.childForceExpandHeight = true;

        multiplierBtns.Clear();
        multiplierBtnImgs.Clear();
        multiplierBtnOutlines.Clear();
        multiplierBtnTexts.Clear();

        for (int m = 0; m < multiplierValues.Length; m++)
        {
            int val = multiplierValues[m];
            string btnLabel = (val == -1) ? "MAX" : $"x{val}";

            GameObject mBtnObj = new GameObject($"Btn_Mult_{btnLabel}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            mBtnObj.layer = 5;
            mBtnObj.transform.SetParent(multRowObj.transform, false);

            Image mImg = mBtnObj.GetComponent<Image>();
            mImg.color = new Color(0.08f, 0.10f, 0.16f, 0.85f);
            var mOutline = mBtnObj.AddComponent<Outline>();
            mOutline.effectColor = new Color(0.3f, 0.4f, 0.55f, 0.5f);
            mOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button mBtn = mBtnObj.GetComponent<Button>();
            mBtn.targetGraphic = mImg;
            mBtn.onClick.AddListener(() => OnMultiplierButtonClicked(val));

            GameObject mTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            mTxtObj.layer = 5;
            mTxtObj.transform.SetParent(mBtnObj.transform, false);
            RectTransform mtRt = mTxtObj.GetComponent<RectTransform>();
            mtRt.anchorMin = Vector2.zero;
            mtRt.anchorMax = Vector2.one;
            mtRt.sizeDelta = Vector2.zero;
            var mTmp = mTxtObj.AddComponent<TextMeshProUGUI>();
            mTmp.text = btnLabel;
            mTmp.fontSize = 17f;
            mTmp.fontStyle = FontStyles.Bold;
            mTmp.alignment = TextAlignmentOptions.Center;
            mTmp.color = new Color(0.7f, 0.75f, 0.85f);

            multiplierBtns.Add(mBtn);
            multiplierBtnImgs.Add(mImg);
            multiplierBtnOutlines.Add(mOutline);
            multiplierBtnTexts.Add(mTmp);
        }

        // Buy Action Button
        GameObject buyObj = new GameObject("Btn_BuyItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buyObj.layer = 5;
        buyObj.transform.SetParent(inspObj.transform, false);
        RectTransform buyRt = buyObj.GetComponent<RectTransform>();
        buyRt.anchorMin = new Vector2(0.5f, 0f);
        buyRt.anchorMax = new Vector2(0.5f, 0f);
        buyRt.pivot = new Vector2(0.5f, 0f);
        buyRt.anchoredPosition = new Vector2(0f, 80f);
        buyRt.sizeDelta = new Vector2(380f, 52f);

        buyActionBtnImg = buyObj.GetComponent<Image>();
        buyActionBtnImg.color = new Color(0.18f, 0.65f, 0.38f, 1f);
        var buyOutline = buyObj.AddComponent<Outline>();
        buyOutline.effectColor = new Color(0.9f, 0.8f, 0.4f, 0.8f);

        buyActionButton = buyObj.GetComponent<Button>();
        buyActionButton.targetGraphic = buyActionBtnImg;
        buyActionButton.onClick.AddListener(OnBuyButtonClicked);

        GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        bTxtObj.layer = 5;
        bTxtObj.transform.SetParent(buyObj.transform, false);
        RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.sizeDelta = Vector2.zero;
        buyActionBtnText = bTxtObj.AddComponent<TextMeshProUGUI>();
        buyActionBtnText.text = LocalizationManager.T("MUA VẬT PHẨM", "BUY ITEM");
        buyActionBtnText.fontSize = 21f;
        buyActionBtnText.fontStyle = FontStyles.Bold;
        buyActionBtnText.color = Color.white;
        buyActionBtnText.alignment = TextAlignmentOptions.Center;
        buyActionBtnText.enableAutoSizing = true;
        buyActionBtnText.fontSizeMin = 17f;
        buyActionBtnText.fontSizeMax = 21f;
        buyActionBtnText.margin = new Vector4(4f, 2f, 4f, 2f);

        // Big Glowing Shortcut Button to Gacha Altar
        GameObject gachaObj = new GameObject("Btn_GoToGacha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        gachaObj.layer = 5;
        gachaObj.transform.SetParent(inspObj.transform, false);
        RectTransform gRt = gachaObj.GetComponent<RectTransform>();
        gRt.anchorMin = new Vector2(0.5f, 0f);
        gRt.anchorMax = new Vector2(0.5f, 0f);
        gRt.pivot = new Vector2(0.5f, 0f);
        gRt.anchoredPosition = new Vector2(0f, 20f);
        gRt.sizeDelta = new Vector2(380f, 48f);

        Image gImg = gachaObj.GetComponent<Image>();
        gImg.color = new Color(0.7f, 0.25f, 0.85f, 1f);
        var gOutline = gachaObj.AddComponent<Outline>();
        gOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.9f);

        goToGachaBtn = gachaObj.GetComponent<Button>();
        goToGachaBtn.targetGraphic = gImg;
        goToGachaBtn.onClick.AddListener(OnGoToGachaClicked);

        GameObject gTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        gTxtObj.layer = 5;
        gTxtObj.transform.SetParent(gachaObj.transform, false);
        RectTransform gtRt = gTxtObj.GetComponent<RectTransform>();
        gtRt.anchorMin = Vector2.zero;
        gtRt.anchorMax = Vector2.one;
        gtRt.sizeDelta = Vector2.zero;
        var gTmp = gTxtObj.AddComponent<TextMeshProUGUI>();
        gTmp.text = LocalizationManager.T("ĐẾN ĐÀI QUAY GACHA", "GO TO GACHA ALTAR");
        gTmp.fontSize = 19f;
        gTmp.fontStyle = FontStyles.Bold;
        gtRt.sizeDelta = Vector2.zero;
        gTmp.color = Color.white;
        gTmp.alignment = TextAlignmentOptions.Center;
        gTmp.enableWordWrapping = false;
    }

    private void RefreshUI()
    {
        if (shopTitleText != null)
        {
            shopTitleText.text = LocalizationManager.T("TIỆM TIẾP TẾ & KHOÁNG SẢN HOÀNG GIA", "ROYAL SUPPLIES & MINERALS EMPORIUM");
        }

        RefreshCategoryTabsVisual();

        if (gridContent == null) return;

        // Filter items
        currentFilteredItems.Clear();
        for (int i = 0; i < shopItems.Count; i++)
        {
            if (shopItems[i].category == currentCategory)
            {
                currentFilteredItems.Add(shopItems[i]);
            }
        }

        // Rebuild cards
        for (int i = cardGameObjects.Count - 1; i >= 0; i--)
        {
            if (cardGameObjects[i] != null) Destroy(cardGameObjects[i]);
        }
        cardGameObjects.Clear();

        if (emptyCategoryNoticeObj != null)
        {
            var enTmp = emptyCategoryNoticeObj.GetComponent<TextMeshProUGUI>();
            if (enTmp != null) enTmp.text = LocalizationManager.T("Danh mục này đang được chuẩn bị,\nsẽ sớm cập nhật trong bản tới!", "This category is coming soon in the next update!");
            emptyCategoryNoticeObj.SetActive(currentFilteredItems.Count == 0);
        }

        if (currentFilteredItems.Count == 0)
        {
            ClearInspector();
            return;
        }

        for (int i = 0; i < currentFilteredItems.Count; i++)
        {
            int index = i;
            var item = currentFilteredItems[i];

            GameObject cardObj = new GameObject($"Card_Supply_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardObj.layer = 5;
            cardObj.transform.SetParent(gridContent, false);
            cardGameObjects.Add(cardObj);

            Image cardImg = cardObj.GetComponent<Image>();
            cardImg.color = (i == selectedShopIndex) ? new Color(0.18f, 0.22f, 0.32f, 0.95f) : new Color(0.09f, 0.11f, 0.16f, 0.9f);

            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = (i == selectedShopIndex) ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.4f, 0.5f, 0.7f, 0.5f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = cardObj.GetComponent<Button>();
            btn.targetGraphic = cardImg;
            btn.onClick.AddListener(() => OnSelectItem(index));

            // Framed Icon Box
            GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconBox.layer = 5;
            iconBox.transform.SetParent(cardObj.transform, false);
            RectTransform iBoxRt = iconBox.GetComponent<RectTransform>();
            iBoxRt.anchorMin = new Vector2(0f, 0.5f);
            iBoxRt.anchorMax = new Vector2(0f, 0.5f);
            iBoxRt.pivot = new Vector2(0f, 0.5f);
            iBoxRt.anchoredPosition = new Vector2(10f, 0f);
            iBoxRt.sizeDelta = new Vector2(56f, 56f);

            Image boxImg = iconBox.GetComponent<Image>();
            if (sprItemSlotFrame != null)
            {
                boxImg.sprite = sprItemSlotFrame;
                boxImg.type = Image.Type.Sliced;
            }
            boxImg.color = (i == selectedShopIndex) ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.7f, 0.8f, 0.9f, 1f);

            // Icon inside box
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.layer = 5;
            iconObj.transform.SetParent(iconBox.transform, false);
            RectTransform iRt = iconObj.GetComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero;
            iRt.anchorMax = Vector2.one;
            iRt.sizeDelta = new Vector2(-12f, -12f);
            Image icImg = iconObj.GetComponent<Image>();
            icImg.sprite = item.Icon;
            icImg.preserveAspect = true;
            icImg.enabled = (item.Icon != null);

            // Badge (Category Tag - Top Right)
            if (!string.IsNullOrEmpty(item.badgeTextVi))
            {
                GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer));
                badgeObj.layer = 5;
                badgeObj.transform.SetParent(cardObj.transform, false);
                RectTransform bRt = badgeObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 1f);
                bRt.anchorMax = new Vector2(1f, 1f);
                bRt.pivot = new Vector2(1f, 1f);
                bRt.anchoredPosition = new Vector2(-8f, -6f);
                bRt.sizeDelta = new Vector2(80f, 20f);
                var bTmp = badgeObj.AddComponent<TextMeshProUGUI>();
                bTmp.text = LocalizationManager.T(item.badgeTextVi, item.badgeTextEn);
                bTmp.fontSize = 11.5f;
                bTmp.fontStyle = FontStyles.Bold;
                bTmp.alignment = TextAlignmentOptions.TopRight;
                bTmp.color = item.badgeColor;
            }

            // Name (Top-Left relative to card, right of icon)
            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer));
            nameObj.layer = 5;
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nRt = nameObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.anchoredPosition = new Vector2(74f, -8f);
            nRt.sizeDelta = new Vector2(-160f, 48f);
            var nTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nTmp.text = LocalizationManager.T(item.nameVi, item.nameEn);
            nTmp.fontSize = 13.5f;
            nTmp.lineSpacing = 1.1f;
            nTmp.fontStyle = FontStyles.Bold;
            nTmp.alignment = TextAlignmentOptions.TopLeft;
            nTmp.color = Color.white;
            nTmp.enableWordWrapping = true;
            nTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Price Tag (Bottom-Left relative to card, right of icon)
            GameObject prcObj = new GameObject("Price", typeof(RectTransform), typeof(CanvasRenderer));
            prcObj.layer = 5;
            prcObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRt = prcObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(1f, 0f);
            pRt.pivot = new Vector2(0f, 0f);
            pRt.anchoredPosition = new Vector2(74f, 8f);
            pRt.sizeDelta = new Vector2(-80f, 24f);
            var pTmp = prcObj.AddComponent<TextMeshProUGUI>();
            string prcStr = item.priceShards >= 10000 ? (item.priceShards / 1000.0).ToString("0.#") + "K" : item.priceShards.ToString("N0");
            pTmp.text = $"{prcStr} {LocalizationManager.T("Linh Hồn", "Shards")}";
            pTmp.fontSize = 16.5f;
            pTmp.fontStyle = FontStyles.Bold;
            pTmp.alignment = TextAlignmentOptions.BottomLeft;
            pTmp.color = new Color(1f, 0.85f, 0.3f);
            pTmp.enableAutoSizing = true;
            pTmp.fontSizeMin = 14.5f;
            pTmp.fontSizeMax = 16.5f;
        }

        if (selectedShopIndex >= currentFilteredItems.Count) selectedShopIndex = 0;
        InspectSelectedItem(selectedShopIndex);
    }

    private void ClearInspector()
    {
        if (detailIconImg != null) detailIconImg.enabled = false;
        if (detailNameText != null) detailNameText.text = "";
        if (detailBadgeText != null) detailBadgeText.text = "";
        if (detailDescText != null) detailDescText.text = LocalizationManager.T("Chọn một vật phẩm để xem chi tiết.", "Select an item to view details.");
        if (detailPriceText != null) detailPriceText.text = "";
        if (buyActionButton != null) buyActionButton.interactable = false;
        if (buyActionBtnText != null) buyActionBtnText.text = LocalizationManager.T("MUA VẬT PHẨM", "BUY ITEM");
    }

    private void OnMultiplierButtonClicked(int val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        selectedMultiplierMode = val;
        InspectSelectedItem(selectedShopIndex);
    }

    private void RefreshMultiplierButtonsVisual(int maxAffordable)
    {
        for (int i = 0; i < multiplierValues.Length; i++)
        {
            if (i >= multiplierBtnImgs.Count) break;
            int mode = multiplierValues[i];
            bool isSelected = (mode == selectedMultiplierMode);

            if (multiplierBtnImgs[i] != null)
            {
                multiplierBtnImgs[i].color = isSelected 
                    ? new Color(0.18f, 0.45f, 0.85f, 0.95f) 
                    : new Color(0.08f, 0.10f, 0.16f, 0.85f);
            }
            if (multiplierBtnOutlines[i] != null)
            {
                multiplierBtnOutlines[i].effectColor = isSelected 
                    ? new Color(1f, 0.85f, 0.35f, 1f) 
                    : new Color(0.3f, 0.4f, 0.55f, 0.5f);
            }
            if (multiplierBtnTexts[i] != null)
            {
                multiplierBtnTexts[i].color = isSelected ? Color.white : new Color(0.7f, 0.75f, 0.85f);
            }
        }
    }

    private void OnSelectItem(int index)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        selectedShopIndex = index;

        for (int i = 0; i < cardGameObjects.Count; i++)
        {
            if (cardGameObjects[i] == null) continue;
            var img = cardGameObjects[i].GetComponent<Image>();
            var outline = cardGameObjects[i].GetComponent<Outline>();
            bool isSel = (i == index);
            if (img != null) img.color = isSel ? new Color(0.18f, 0.22f, 0.32f, 0.95f) : new Color(0.09f, 0.11f, 0.16f, 0.9f);
            if (outline != null) outline.effectColor = isSel ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.4f, 0.5f, 0.7f, 0.5f);
        }

        InspectSelectedItem(index);
    }

    public static string FormatCompactNumber(long val)
    {
        if (val >= 1_000_000_000) return (val / 1_000_000_000.0).ToString("0.##") + "B";
        if (val >= 1_000_000) return (val / 1_000_000.0).ToString("0.##") + "M";
        if (val >= 10_000) return (val / 1_000.0).ToString("0.#") + "K";
        return val.ToString("N0");
    }

    private void InspectSelectedItem(int index)
    {
        if (index < 0 || index >= currentFilteredItems.Count) return;
        var item = currentFilteredItems[index];

        if (detailIconImg != null)
        {
            detailIconImg.sprite = item.Icon;
            detailIconImg.enabled = (item.Icon != null);
        }
        if (detailNameText != null) detailNameText.text = LocalizationManager.T(item.nameVi, item.nameEn);
        if (detailBadgeText != null)
        {
            detailBadgeText.text = LocalizationManager.T(item.badgeTextVi, item.badgeTextEn);
            detailBadgeText.color = item.badgeColor;
        }
        if (detailDescText != null) detailDescText.text = LocalizationManager.T(item.descVi, item.descEn);

        long currentShards = AccountDatabase.GetAethelShards();
        long maxAffordable = (item.priceShards > 0) ? (currentShards / item.priceShards) : 1;
        if (maxAffordable < 1) maxAffordable = 1;
        if (maxAffordable > int.MaxValue) maxAffordable = int.MaxValue;

        long actualBuyMultiplier = (selectedMultiplierMode == -1) ? maxAffordable : selectedMultiplierMode;
        if (actualBuyMultiplier < 1) actualBuyMultiplier = 1;

        long totalCost = actualBuyMultiplier * item.priceShards;
        long totalQuantityGained = actualBuyMultiplier * item.quantity;

        string prcDetailStr = FormatCompactNumber(item.priceShards);
        if (detailPriceText != null)
        {
            detailPriceText.text = $"{LocalizationManager.T("Đơn giá:", "Unit Price:")} <color=#FFD700>{prcDetailStr} {LocalizationManager.T("Linh Hồn", "Shards")}</color>";
        }

        if (balanceText != null)
        {
            string compactShards = FormatCompactNumber(currentShards);
            balanceText.text = (currentShards >= 10_000)
                ? $"{LocalizationManager.T("Linh Hồn hiện có:", "Your Shards:")} <color=#00D2FF>{compactShards} ({currentShards:N0})</color>"
                : $"{LocalizationManager.T("Linh Hồn hiện có:", "Your Shards:")} <color=#00D2FF>{currentShards:N0}</color>";
        }

        RefreshMultiplierButtonsVisual((int)System.Math.Min((long)int.MaxValue, maxAffordable));

        if (buyActionButton != null)
        {
            bool canAfford = currentShards >= totalCost && totalCost > 0;
            buyActionButton.interactable = canAfford;
            if (buyActionBtnImg != null) buyActionBtnImg.color = canAfford ? new Color(0.18f, 0.65f, 0.38f, 1f) : new Color(0.3f, 0.3f, 0.3f, 0.8f);

            if (buyActionBtnText != null)
            {
                if (actualBuyMultiplier > 1)
                {
                    buyActionBtnText.text = LocalizationManager.T(
                        $"MUA x{actualBuyMultiplier:N0} ({FormatCompactNumber(totalCost)} Linh Hồn - {FormatCompactNumber(totalQuantityGained)} cái)",
                        $"BUY x{actualBuyMultiplier:N0} ({FormatCompactNumber(totalCost)} Shards - {FormatCompactNumber(totalQuantityGained)} pcs)"
                    );
                }
                else
                {
                    buyActionBtnText.text = LocalizationManager.T(
                        $"MUA VẬT PHẨM ({FormatCompactNumber(totalCost)} Linh Hồn)",
                        $"BUY ITEM ({FormatCompactNumber(totalCost)} Shards)"
                    );
                }
            }
        }
    }

    private void OnBuyButtonClicked()
    {
        if (selectedShopIndex < 0 || selectedShopIndex >= currentFilteredItems.Count) return;
        var item = currentFilteredItems[selectedShopIndex];

        long currentShards = AccountDatabase.GetAethelShards();
        long maxAffordable = (item.priceShards > 0) ? (currentShards / item.priceShards) : 1;
        if (maxAffordable < 1) maxAffordable = 1;
        if (maxAffordable > int.MaxValue) maxAffordable = int.MaxValue;

        long actualBuyMultiplier = (selectedMultiplierMode == -1) ? maxAffordable : selectedMultiplierMode;
        if (actualBuyMultiplier < 1) actualBuyMultiplier = 1;

        long totalCost = actualBuyMultiplier * item.priceShards;
        long totalQuantityGained = actualBuyMultiplier * item.quantity;

        if (currentShards < totalCost || totalCost <= 0)
        {
            Debug.LogWarning("[SHOP] Not enough Aethel Shards!");
            return;
        }

        if (!AccountDatabase.SpendAethelShards(totalCost))
        {
            Debug.LogWarning("[SHOP] Spend shards failed!");
            return;
        }

        string targetItemId = (item.itemId == "scroll_summon_mythic_10") ? "scroll_summon_mythic" : item.itemId;
        int qtyClamped = (int)System.Math.Min((long)int.MaxValue, totalQuantityGained);
        AccountDatabase.AddItemToActiveUser(targetItemId, qtyClamped, "Material", "Mua từ Tiệm Tiếp Tế");

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        InspectSelectedItem(selectedShopIndex);

        var mm = Object.FindAnyObjectByType<MainMenuManager>();
        if (mm != null) mm.UpdateTopHUD();
    }

    private void OnGoToGachaClicked()
    {
        Close();
        var inv = InventoryUI.EnsureUIExists();
        if (inv != null)
        {
            inv.OpenToGachaTab();
        }
    }

    public bool IsOpen() => isOpen;

    public void Open()
    {
        EnsureSprites();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        isOpen = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (dialogBox == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
            if (parentCanvas != null) BuildUI(parentCanvas);
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();
        RefreshCategoryTabsVisual();
        RefreshUI();
    }

    public void Close()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        isOpen = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(false);
    }
}
