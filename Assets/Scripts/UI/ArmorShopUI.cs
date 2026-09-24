// ========================================================
// VAELSTRIDER: RUINBOUND - Multi-Slot Ancient Armor & Gear Vault
// Fully organized according to user subfolders:
// hat (8), shirt (27), pants (3), glove (4), shoe (10), scarf (3), bag (1)
// Includes Live Animated Player Preview Model in Shop Fitting Room
// and full sync with in-game Player and Main Menu Character Roster.
// ========================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ArmorShopUI : MonoBehaviour
{
    private static ArmorShopUI instance;
    public static ArmorShopUI Instance => instance;

    public enum EquipmentSlot
    {
        Helmet,     // hat (8 items)
        Armor,      // shirt (27 items)
        Pants,      // pants (3 items)
        Gloves,     // glove (4 items)
        Boots,      // shoe (10 items)
        Cloak,      // scarf (3 items)
        Bag         // bag (1 item)
    }

    public enum ArmorTier
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    [System.Serializable]
    public class ArmorItem
    {
        public int id;
        public string nameVi;
        public string nameEn;
        public string descVi;
        public string descEn;
        public EquipmentSlot slot;
        public string folderName;
        public ArmorTier tier;
        public int hpBonus;
        public float defensePercent;
        public float speedBonus;
        public float staminaRegenPercent;
        public Sprite sprite;
    }

    private static List<ArmorItem> allArmorItems = new List<ArmorItem>();
    private static bool isDatabaseInitialized = false;

    [Header("UI References")]
    private CanvasGroup canvasGroup;
    private GameObject dialogBox;
    private TextMeshProUGUI shopTitleText;
    private TextMeshProUGUI shopSubtitleText;
    private Transform gridContent;
    private ScrollRect scrollRect;

    // Category Tabs (8 Tabs: ALL, HAT, SHIRT, PANTS, GLOVE, SHOE, SCARF, BAG)
    private int currentCategoryFilter = -1; // -1 = ALL, 0..6 for slots
    private Button[] tabButtons;
    private Image[] tabButtonImgs;
    private TextMeshProUGUI[] tabButtonTexts;

    // Right Inspector Panel & Live Player Fitting Room
    private Image fittingModelImg;
    private RectTransform fittingModelRt;
    private TextMeshProUGUI fittingFocusTagText;
    private Material fittingModelMat;
    private Sprite[] playerIdleFrames;
    private Coroutine fittingAnimCoroutine;
    private Coroutine cameraFocusCoroutine;
    private int fittingFrameIdx = 0;

    private Image detailIconImg;
    private TextMeshProUGUI detailNameText;
    private TextMeshProUGUI detailSlotText;
    private TextMeshProUGUI detailTierText;
    private TextMeshProUGUI detailStatsText;
    private TextMeshProUGUI detailTotalLoadoutStatsText;
    private TextMeshProUGUI detailDescText;
    private TextMeshProUGUI detailStatusText;
    private Button equipActionButton;
    private TextMeshProUGUI equipActionBtnText;
    private Image equipActionBtnImg;

    private int selectedArmorId = 1;
    private List<GameObject> cardGameObjects = new List<GameObject>();
    private bool isOpen = false;

    public static ArmorShopUI EnsureUIExists()
    {
        if (instance != null) return instance;
        instance = Object.FindAnyObjectByType<ArmorShopUI>(FindObjectsInactive.Include);

        // Found an existing ArmorShopUI - but is it properly built?
        // If dialogBox is null, it's a stale scene-serialized object with no runtime UI wired.
        // Destroy it and create a fresh one.
        if (instance != null)
        {
            if (instance.dialogBox != null) return instance; // Properly built, just return it
            // Stale - destroy and fall through to rebuild
            if (Application.isPlaying) Object.Destroy(instance.gameObject);
            else Object.DestroyImmediate(instance.gameObject);
            instance = null;
        }

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        // Also destroy any stale named ArmorShopModal in scene hierarchy
        var existing = canvas.transform.Find("ArmorShopModal");
        if (existing != null)
        {
            if (Application.isPlaying) Object.Destroy(existing.gameObject);
            else Object.DestroyImmediate(existing.gameObject);
        }

        GameObject shopObj = new GameObject("ArmorShopModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shopObj.layer = 5;
        shopObj.transform.SetParent(canvas.transform, false);

        instance = shopObj.AddComponent<ArmorShopUI>(); // Triggers Awake() synchronously
        // Awake may have already called BuildUI if it had a parent canvas.
        // Only call BuildUI here if Awake didn't already build it.
        if (instance.dialogBox == null)
        {
            instance.BuildUI(canvas);
        }
        shopObj.SetActive(false);
        return instance;
    }

    private void Awake()
    {
        instance = this;
        gameObject.layer = 5; // UI layer
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        InitDatabase();
        LoadPlayerIdleFrames();
        EnsureEventSystemActive();

        // If dialogBox is null the UI hasn't been built yet (e.g. this component was added to a
        // pre-existing scene object without calling BuildUI). Build now.
        if (dialogBox == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = Object.FindAnyObjectByType<Canvas>();
            if (parentCanvas != null) BuildUI(parentCanvas);
        }
    }

    private void OnEnable()
    {
        isOpen = true;
        EnsureEventSystemActive();

        // Ensure CanvasGroup is always interactive when opened
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // If dialogBox is missing (can happen if scene was loaded with old data), rebuild
        if (dialogBox == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = Object.FindAnyObjectByType<Canvas>();
            if (parentCanvas != null) BuildUI(parentCanvas);
        }

        LoadPlayerIdleFrames();
        RefreshUI();

        if (fittingAnimCoroutine != null)
        {
            StopCoroutine(fittingAnimCoroutine);
            fittingAnimCoroutine = null;
        }

        if (gameObject.activeInHierarchy && Application.isPlaying)
        {
            fittingAnimCoroutine = StartCoroutine(AnimateFittingModel());
        }
    }

    public void BindUIReferencesAndEvents()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (dialogBox == null)
        {
            var dbTransform = transform.Find("DialogBox");
            if (dbTransform != null) dialogBox = dbTransform.gameObject;
        }

        if (dialogBox == null) return;

        // 1. Close Button
        var closeTrans = dialogBox.transform.Find("Btn_CloseShop");
        if (closeTrans != null)
        {
            var cb = closeTrans.GetComponent<Button>();
            if (cb != null)
            {
                cb.onClick.RemoveAllListeners();
                cb.onClick.AddListener(Close);
            }
            AttachPointerClick(closeTrans.gameObject, Close);
        }

        // 2. Title & Subtitle Texts
        var tTrans = dialogBox.transform.Find("TitleText");
        if (tTrans != null) shopTitleText = tTrans.GetComponent<TextMeshProUGUI>();
        var subTrans = dialogBox.transform.Find("SubtitleText");
        if (subTrans != null) shopSubtitleText = subTrans.GetComponent<TextMeshProUGUI>();

        // 3. Category Tabs
        var tabsTrans = dialogBox.transform.Find("CategoryTabsContainer");
        if (tabsTrans != null)
        {
            tabButtons = new Button[8];
            tabButtonImgs = new Image[8];
            tabButtonTexts = new TextMeshProUGUI[8];

            for (int t = 0; t < 8; t++)
            {
                var tabBtnTrans = tabsTrans.Find($"TabBtn_{t}");
                if (tabBtnTrans != null)
                {
                    int filterIdx = t - 1;
                    var tBtn = tabBtnTrans.GetComponent<Button>();
                    if (tBtn != null)
                    {
                        tBtn.onClick.RemoveAllListeners();
                        tBtn.onClick.AddListener(() => OnCategoryTabClicked(filterIdx));
                        tabButtons[t] = tBtn;
                    }
                    AttachPointerClick(tabBtnTrans.gameObject, () => OnCategoryTabClicked(filterIdx));
                    tabButtonImgs[t] = tabBtnTrans.GetComponent<Image>();
                    var txtTrans = tabBtnTrans.Find("Text");
                    if (txtTrans != null) tabButtonTexts[t] = txtTrans.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // 4. ScrollView & GridContent
        var scrollTrans = dialogBox.transform.Find("ItemsScrollView");
        if (scrollTrans != null)
        {
            scrollRect = scrollTrans.GetComponent<ScrollRect>();
            gridContent = scrollTrans.Find("GridContent");
        }
        if (gridContent == null)
        {
            var trs = dialogBox.GetComponentsInChildren<Transform>(true);
            foreach (var tr in trs)
            {
                if (tr.name == "GridContent") { gridContent = tr; break; }
            }
        }

        // 5. Inspector Panel Elements
        var inspTrans = dialogBox.transform.Find("ItemDetailInspector");
        if (inspTrans != null)
        {
            var fittingBox = inspTrans.Find("LiveHeroFittingPedestal");
            if (fittingBox != null)
            {
                var modelTrans = fittingBox.Find("LiveHeroModel");
                if (modelTrans != null)
                {
                    fittingModelRt = modelTrans.GetComponent<RectTransform>();
                    fittingModelImg = modelTrans.GetComponent<Image>();
                }
                var tagTrans = fittingBox.Find("FocusTag");
                if (tagTrans != null) fittingFocusTagText = tagTrans.GetComponent<TextMeshProUGUI>();
            }

            var iconFrame = inspTrans.Find("IconFrame");
            if (iconFrame != null)
            {
                var previewIcon = iconFrame.Find("PreviewIcon");
                if (previewIcon != null) detailIconImg = previewIcon.GetComponent<Image>();
            }

            var nameTrans = inspTrans.Find("DetailName");
            if (nameTrans != null) detailNameText = nameTrans.GetComponent<TextMeshProUGUI>();

            var tierTrans = inspTrans.Find("DetailSlotAndTier");
            if (tierTrans != null) detailTierText = tierTrans.GetComponent<TextMeshProUGUI>();

            var statsBox = inspTrans.Find("StatsBox");
            if (statsBox != null)
            {
                var stxt = statsBox.Find("StatsText");
                if (stxt != null) detailStatsText = stxt.GetComponent<TextMeshProUGUI>();
            }

            var totBox = inspTrans.Find("TotalLoadoutBox");
            if (totBox != null)
            {
                var totTxt = totBox.Find("TotalStatsText");
                if (totTxt != null) detailTotalLoadoutStatsText = totTxt.GetComponent<TextMeshProUGUI>();
            }

            var priceTag = inspTrans.Find("PriceTag");
            if (priceTag != null) detailStatusText = priceTag.GetComponent<TextMeshProUGUI>();

            var unequipAllTrans = inspTrans.Find("Btn_UnequipAll");
            if (unequipAllTrans != null)
            {
                var unBtn = unequipAllTrans.GetComponent<Button>();
                if (unBtn != null)
                {
                    unBtn.onClick.RemoveAllListeners();
                    unBtn.onClick.AddListener(OnUnequipAllClicked);
                }
                AttachPointerClick(unequipAllTrans.gameObject, OnUnequipAllClicked);
            }

            var equipBtnTrans = inspTrans.Find("Btn_EquipArmor");
            if (equipBtnTrans != null)
            {
                equipActionButton = equipBtnTrans.GetComponent<Button>();
                equipActionBtnImg = equipBtnTrans.GetComponent<Image>();
                if (equipActionButton != null)
                {
                    equipActionButton.onClick.RemoveAllListeners();
                    equipActionButton.onClick.AddListener(OnEquipButtonClicked);
                }
                AttachPointerClick(equipBtnTrans.gameObject, OnEquipButtonClicked);
                var abTxt = equipBtnTrans.Find("Text");
                if (abTxt != null) equipActionBtnText = abTxt.GetComponent<TextMeshProUGUI>();
            }
        }

        BuildItemCards();
    }

    private void AttachPointerClick(GameObject obj, UnityEngine.Events.UnityAction action)
    {
        if (obj == null || action == null) return;
        var trigger = obj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        trigger.triggers.Clear();
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => action.Invoke());
        trigger.triggers.Add(entry);
    }

    public void EnsureEventSystemActive()
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        }
        if (eventSystem == null)
        {
            var esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null)
        {
            if (Application.isPlaying) Destroy(standalone);
            else DestroyImmediate(standalone);
        }

        var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        inputModule.AssignDefaultActions();
        inputModule.enabled = true;
#else
        var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone == null)
        {
            standalone = eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        standalone.enabled = true;
#endif
    }

    private void Start()
    {
        LocalizationManager.OnLanguageChanged += RefreshUI;
        BindUIReferencesAndEvents();
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshUI;
        if (fittingModelMat != null) Destroy(fittingModelMat);
    }

    public static void InitDatabase()
    {
        if (isDatabaseInitialized && allArmorItems.Count >= 56) return;

        allArmorItems.Clear();

        // Exact subfolder mapping created by user:
        // hat (8 items): 6, 11, 13, 22, 27, 30, 42, 49
        // shirt (27 items): 1, 7, 10, 14, 15, 16, 17, 20, 21, 23, 24, 25, 26, 29, 32, 34, 36, 37, 41, 43, 47, 48, 50, 51, 53, 54, 55
        // pants (3 items): 44, 45, 56
        // glove (4 items): 12, 33, 35, 52
        // shoe (10 items): 2, 4, 5, 8, 18, 28, 31, 38, 40, 46
        // scarf (3 items): 9, 19, 39
        // bag (1 item): 3

        var map = new Dictionary<int, (EquipmentSlot slot, string folder)>()
        {
            // hat
            { 6, (EquipmentSlot.Helmet, "hat") }, { 11, (EquipmentSlot.Helmet, "hat") },
            { 13, (EquipmentSlot.Helmet, "hat") }, { 22, (EquipmentSlot.Helmet, "hat") },
            { 27, (EquipmentSlot.Helmet, "hat") }, { 30, (EquipmentSlot.Helmet, "hat") },
            { 42, (EquipmentSlot.Helmet, "hat") }, { 49, (EquipmentSlot.Helmet, "hat") },

            // shirt
            { 1, (EquipmentSlot.Armor, "shirt") }, { 7, (EquipmentSlot.Armor, "shirt") },
            { 10, (EquipmentSlot.Armor, "shirt") }, { 14, (EquipmentSlot.Armor, "shirt") },
            { 15, (EquipmentSlot.Armor, "shirt") }, { 16, (EquipmentSlot.Armor, "shirt") },
            { 17, (EquipmentSlot.Armor, "shirt") }, { 20, (EquipmentSlot.Armor, "shirt") },
            { 21, (EquipmentSlot.Armor, "shirt") }, { 23, (EquipmentSlot.Armor, "shirt") },
            { 24, (EquipmentSlot.Armor, "shirt") }, { 25, (EquipmentSlot.Armor, "shirt") },
            { 26, (EquipmentSlot.Armor, "shirt") }, { 29, (EquipmentSlot.Armor, "shirt") },
            { 32, (EquipmentSlot.Armor, "shirt") }, { 34, (EquipmentSlot.Armor, "shirt") },
            { 36, (EquipmentSlot.Armor, "shirt") }, { 37, (EquipmentSlot.Armor, "shirt") },
            { 41, (EquipmentSlot.Armor, "shirt") }, { 43, (EquipmentSlot.Armor, "shirt") },
            { 47, (EquipmentSlot.Armor, "shirt") }, { 48, (EquipmentSlot.Armor, "shirt") },
            { 50, (EquipmentSlot.Armor, "shirt") }, { 51, (EquipmentSlot.Armor, "shirt") },
            { 53, (EquipmentSlot.Armor, "shirt") }, { 54, (EquipmentSlot.Armor, "shirt") },
            { 55, (EquipmentSlot.Armor, "shirt") },

            // pants
            { 44, (EquipmentSlot.Pants, "pants") }, { 45, (EquipmentSlot.Pants, "pants") },
            { 56, (EquipmentSlot.Pants, "pants") },

            // glove
            { 12, (EquipmentSlot.Gloves, "glove") }, { 33, (EquipmentSlot.Gloves, "glove") },
            { 35, (EquipmentSlot.Gloves, "glove") }, { 52, (EquipmentSlot.Gloves, "glove") },

            // shoe
            { 2, (EquipmentSlot.Boots, "shoe") }, { 4, (EquipmentSlot.Boots, "shoe") },
            { 5, (EquipmentSlot.Boots, "shoe") }, { 8, (EquipmentSlot.Boots, "shoe") },
            { 18, (EquipmentSlot.Boots, "shoe") }, { 28, (EquipmentSlot.Boots, "shoe") },
            { 31, (EquipmentSlot.Boots, "shoe") }, { 38, (EquipmentSlot.Boots, "shoe") },
            { 40, (EquipmentSlot.Boots, "shoe") }, { 46, (EquipmentSlot.Boots, "shoe") },

            // scarf
            { 9, (EquipmentSlot.Cloak, "scarf") }, { 19, (EquipmentSlot.Cloak, "scarf") },
            { 39, (EquipmentSlot.Cloak, "scarf") },

            // bag
            { 3, (EquipmentSlot.Bag, "bag") }
        };

        for (int i = 1; i <= 56; i++)
        {
            EquipmentSlot slot = EquipmentSlot.Armor;
            string folder = "shirt";
            if (map.ContainsKey(i))
            {
                slot = map[i].slot;
                folder = map[i].folder;
            }

            ArmorTier tier = ArmorTier.Common;
            if (i >= 51) tier = ArmorTier.Mythic;
            else if (i >= 41) tier = ArmorTier.Legendary;
            else if (i >= 27) tier = ArmorTier.Epic;
            else if (i >= 13) tier = ArmorTier.Rare;

            int hp = 8 + i * 3;
            float def = 1.5f + (i * 0.35f);
            float spd = 0.05f + (i * 0.015f);
            float stam = 4f + (i * 0.45f);

            if (slot == EquipmentSlot.Helmet) { hp += 15; def += 1.5f; }
            else if (slot == EquipmentSlot.Armor) { hp += 30; def += 4.0f; }
            else if (slot == EquipmentSlot.Pants) { hp += 20; def += 2.0f; }
            else if (slot == EquipmentSlot.Gloves) { def += 2.5f; spd += 0.1f; }
            else if (slot == EquipmentSlot.Boots) { spd += 0.35f; stam += 10f; }
            else if (slot == EquipmentSlot.Cloak) { stam += 12f; hp += 20; }
            else if (slot == EquipmentSlot.Bag) { stam += 25f; hp += 35; }

            Sprite sprite = LoadArmorSprite(i, folder);

            ArmorItem item = new ArmorItem
            {
                id = i,
                nameVi = GetItemNameVi(i, slot),
                nameEn = GetItemNameEn(i, slot),
                descVi = $"Trang phuc {GetSlotNameVi(slot)} thuoc bo suu tap {folder.ToUpper()}. Giu am va gia tang chi so chien dau.",
                descEn = $"Cozy gear {GetSlotNameEn(slot)} from {folder.ToUpper()} collection. Keeps you warm and empowers battle attributes.",
                slot = slot,
                folderName = folder,
                tier = tier,
                hpBonus = hp,
                defensePercent = def,
                speedBonus = spd,
                staminaRegenPercent = stam,
                sprite = sprite
            };

            allArmorItems.Add(item);
        }

        isDatabaseInitialized = true;
    }

    private static string GetItemNameVi(int id, EquipmentSlot slot)
    {
        switch (id)
        {
            case 1: return "Ao Len Bac Au";
            case 2: return "Giay Len Do Mua Dong";
            case 3: return "Tui Deo Cheo Du Hanh";
            case 4: return "Giay Len Vang Be";
            case 5: return "Giay Len Do Noel";
            case 6: return "Bit Tai Mua Dong";
            case 7: return "Ao Len Kim Cuong Xanh";
            case 8: return "Giay Len Cam Soc";
            case 9: return "Khan Choang Len Caro";
            case 10: return "Ao Len Do Do Am Ap";
            case 11: return "Mu Len Phi Cong Tuyet";
            case 12: return "Gang Tay Len Do Co Day";
            case 13: return "Mu Len Phi Cong Do";
            case 14: return "Ao Len Kem Tranh Theu";
            case 15: return "Ao Len Soc Be Nau";
            case 16: return "Ao Len Nau Ke O";
            case 17: return "Ao Len Nau Dam";
            case 18: return "Giay Len Nau Nhat";
            case 19: return "Khan Len Det Cam";
            case 20: return "Ao Len Nau Thu";
            case 21: return "Ao Len Do Hoa Tuyet";
            case 22: return "Mu Len Long Cuu Vang";
            case 23: return "Ao Len Xanh Reu";
            case 24: return "Ao Len Tuan Loc Cam";
            case 25: return "Ao Len Xanh Bien";
            case 26: return "Ao Len Vang Hoang Kim";
            case 27: return "Mu Len Xanh Khaki";
            case 28: return "Giay Len Nau Co Lo";
            case 29: return "Ao Cardigan Do";
            case 30: return "Mu Len Do Ruou";
            case 31: return "Giay Len Kem Trum";
            case 32: return "Ao Len Xanh Den";
            case 33: return "Gang Tay Len Xam";
            case 34: return "Ao Len Xam Than";
            case 35: return "Bao Tay Ruby";
            case 36: return "Ao Len Cam Hoang Hon";
            case 37: return "Ao Len Xanh Xam";
            case 38: return "Giay Len Xam Da";
            case 39: return "Khan Choang Tim";
            case 40: return "Giay Len Mocha";
            case 41: return "Ao Cardigan Vang";
            case 42: return "Mu Len Do Dat";
            case 43: return "Ao Len Xanh Hoang Gia";
            case 44: return "Quan Thun Xanh Duong";
            case 45: return "Quan Ni Xam The Thao";
            case 46: return "Giay Len Long Trang";
            case 47: return "Ao Len Espresso Dam";
            case 48: return "Ao Len Xanh Denim";
            case 49: return "Mu Len Vang Nhat";
            case 50: return "Ao Len Vang Ho Phach";
            case 51: return "Ao Len Tim Thach Anh";
            case 52: return "Bao Tay Ho Phach";
            case 53: return "Ao Len Xam Titan";
            case 54: return "Ao Len Xanh Rung";
            case 55: return "Ao Len Cat Sa Mac";
            case 56: return "Quan Nhung Tam Nau";
            default: return $"Trang Phuc So {id}";
        }
    }

    private static string GetItemNameEn(int id, EquipmentSlot slot)
    {
        switch (id)
        {
            case 1: return "Nordic Pattern Sweater";
            case 2: return "Winter Red Knit Shoes";
            case 3: return "Traveler Crossbody Bag";
            case 4: return "Soft Beige Knit Shoes";
            case 5: return "Christmas Red Shoes";
            case 6: return "Winter Cozy Earmuffs";
            case 7: return "Diamond Blue Sweater";
            case 8: return "Striped Orange Shoes";
            case 9: return "Plaid Wool Scarf";
            case 10: return "Warm Maroon Sweater";
            case 11: return "Snowflake Aviator Beanie";
            case 12: return "Red Connected Mittens";
            case 13: return "Crimson Aviator Hat";
            case 14: return "Cream Embroidered Jumper";
            case 15: return "Tan Striped Sweater";
            case 16: return "Brown Checked Sweater";
            case 17: return "Dark Espresso Jumper";
            case 18: return "Light Hazel Knit Shoes";
            case 19: return "Orange Knitted Scarf";
            case 20: return "Autumn Brown Sweater";
            case 21: return "Crimson Snowflake Sweater";
            case 22: return "Golden Fur Beanie";
            case 23: return "Moss Green Sweater";
            case 24: return "Orange Reindeer Sweater";
            case 25: return "Teal Winter Sweater";
            case 26: return "Golden Honey Sweater";
            case 27: return "Khaki Green Beanie";
            case 28: return "Brown Warm Knit Shoes";
            case 29: return "Crimson Knit Cardigan";
            case 30: return "Burgundy Red Beanie";
            case 31: return "Cream Knit Shoes";
            case 32: return "Navy Dark Sweater";
            case 33: return "Charcoal Knitted Mittens";
            case 34: return "Charcoal Grey Sweater";
            case 35: return "Ruby Winter Mittens";
            case 36: return "Sunset Orange Sweater";
            case 37: return "Sage Green Sweater";
            case 38: return "Slate Grey Knit Shoes";
            case 39: return "Violet Cashmere Scarf";
            case 40: return "Warm Mocha Shoes";
            case 41: return "Golden Knit Cardigan";
            case 42: return "Terracotta Red Beanie";
            case 43: return "Royal Blue Sweater";
            case 44: return "Blue Drawstring Sweatpants";
            case 45: return "Heather Grey Joggers";
            case 46: return "White Wool Shoes";
            case 47: return "Dark Espresso Sweater";
            case 48: return "Denim Blue Sweater";
            case 49: return "Pale Yellow Beanie";
            case 50: return "Amber Gold Sweater";
            case 51: return "Amethyst Violet Sweater";
            case 52: return "Amber Cozy Mittens";
            case 53: return "Titanium Grey Sweater";
            case 54: return "Forest Moss Sweater";
            case 55: return "Desert Sand Sweater";
            case 56: return "Vintage Brown Corduroy Pants";
            default: return $"Item #{id}";
        }
    }

    public static Sprite LoadArmorSprite(int id, string folder = "")
    {
        Sprite s = null;
        string[] subfolders = string.IsNullOrEmpty(folder) 
            ? new string[] { "hat", "shirt", "pants", "glove", "shoe", "scarf", "bag" } 
            : new string[] { folder, "hat", "shirt", "pants", "glove", "shoe", "scarf", "bag" };

#if UNITY_EDITOR
        foreach (var sub in subfolders)
        {
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Item/Clothes/{sub}/{id}.png");
            if (s != null) return s;
        }
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Item/Clothes/{id}.png");
        if (s != null) return s;
#endif

        foreach (var sub in subfolders)
        {
            string path = Path.Combine(Application.dataPath, $"ART/Item/Clothes/{sub}/{id}.png");
            if (File.Exists(path))
            {
                byte[] data = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        return s;
    }

    public static Sprite GetItemSpriteById(int id)
    {
        InitDatabase();
        var item = allArmorItems.Find(x => x.id == id);
        if (item != null && item.sprite != null) return item.sprite;
        return LoadArmorSprite(id);
    }

    public static string GetSlotNameVi(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Helmet: return "NON";
            case EquipmentSlot.Armor: return "AO";
            case EquipmentSlot.Pants: return "QUAN";
            case EquipmentSlot.Gloves: return "BAO TAY";
            case EquipmentSlot.Boots: return "GIAY";
            case EquipmentSlot.Cloak: return "KHAN";
            case EquipmentSlot.Bag: return "TUI";
            default: return "TRANG PHUC";
        }
    }

    public static string GetSlotNameEn(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Helmet: return "HAT";
            case EquipmentSlot.Armor: return "SHIRT";
            case EquipmentSlot.Pants: return "PANTS";
            case EquipmentSlot.Gloves: return "GLOVE";
            case EquipmentSlot.Boots: return "SHOE";
            case EquipmentSlot.Cloak: return "SCARF";
            case EquipmentSlot.Bag: return "BAG";
            default: return "CLOTHING";
        }
    }

    public static int GetEquippedIdForSlot(EquipmentSlot slot)
    {
        InitDatabase();
        string key = "Equipped_Slot_" + slot.ToString();
        if (!PlayerPrefs.HasKey(key))
        {
            return (slot == EquipmentSlot.Armor) ? 1 : 0;
        }
        return PlayerPrefs.GetInt(key, 0);
    }

    public static void EquipItemToSlot(int itemId)
    {
        InitDatabase();
        var item = allArmorItems.Find(x => x.id == itemId);
        if (item != null)
        {
            string key = "Equipped_Slot_" + item.slot.ToString();
            PlayerPrefs.SetInt(key, itemId);
            if (item.slot == EquipmentSlot.Armor) PlayerPrefs.SetInt("EquippedArmorId", itemId);
            PlayerPrefs.Save();

            // Notify in-game visual
            var pVisual = Object.FindAnyObjectByType<PlayerEquipmentVisual>();
            if (pVisual != null) pVisual.RefreshAllEquipmentVisuals();
        }
    }

    public static void UnequipSlot(EquipmentSlot slot)
    {
        InitDatabase();
        string key = "Equipped_Slot_" + slot.ToString();
        PlayerPrefs.SetInt(key, 0);
        if (slot == EquipmentSlot.Armor) PlayerPrefs.SetInt("EquippedArmorId", 0);
        PlayerPrefs.Save();

        // Notify in-game visual
        var pVisual = Object.FindAnyObjectByType<PlayerEquipmentVisual>();
        if (pVisual != null) pVisual.RefreshAllEquipmentVisuals();
    }

    public static void UnequipAllSlots()
    {
        InitDatabase();
        EquipmentSlot[] allSlots = new EquipmentSlot[]
        {
            EquipmentSlot.Helmet, EquipmentSlot.Armor, EquipmentSlot.Pants,
            EquipmentSlot.Gloves, EquipmentSlot.Boots, EquipmentSlot.Cloak, EquipmentSlot.Bag
        };

        foreach (var slot in allSlots)
        {
            string key = "Equipped_Slot_" + slot.ToString();
            PlayerPrefs.SetInt(key, 0);
        }
        PlayerPrefs.SetInt("EquippedArmorId", 0);
        PlayerPrefs.Save();

        // Notify in-game visual
        var pVisual = Object.FindAnyObjectByType<PlayerEquipmentVisual>();
        if (pVisual != null) pVisual.RefreshAllEquipmentVisuals();
    }

    public static bool IsItemEquipped(int itemId)
    {
        InitDatabase();
        var item = allArmorItems.Find(x => x.id == itemId);
        if (item == null) return false;
        return GetEquippedIdForSlot(item.slot) == itemId;
    }

    public static void GetTotalEquippedGearBonus(out int totalHp, out float totalDef, out float totalSpeed, out float totalStamina)
    {
        InitDatabase();
        totalHp = 0;
        totalDef = 0f;
        totalSpeed = 0f;
        totalStamina = 0f;

        EquipmentSlot[] allSlots = new EquipmentSlot[]
        {
            EquipmentSlot.Helmet, EquipmentSlot.Armor, EquipmentSlot.Pants,
            EquipmentSlot.Gloves, EquipmentSlot.Boots, EquipmentSlot.Cloak, EquipmentSlot.Bag
        };

        foreach (var slot in allSlots)
        {
            int eqId = GetEquippedIdForSlot(slot);
            if (eqId > 0)
            {
                var itm = allArmorItems.Find(x => x.id == eqId);
                if (itm != null)
                {
                    totalHp += itm.hpBonus;
                    totalDef += itm.defensePercent;
                    totalSpeed += itm.speedBonus;
                    totalStamina += itm.staminaRegenPercent;
                }
            }
        }
    }

    public static void GetEquippedArmorBonus(out int bonusHp, out float damageReductionPercent, out float speedBonus)
    {
        GetTotalEquippedGearBonus(out bonusHp, out damageReductionPercent, out speedBonus, out _);
    }

    private void LoadPlayerIdleFrames()
    {
#if UNITY_EDITOR
        if (playerIdleFrames == null || playerIdleFrames.Length == 0)
        {
            var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player/IDLE/idle_down.png");
            var list = new List<Sprite>();
            if (objects != null)
            {
                foreach (var o in objects) if (o is Sprite s) list.Add(s);
            }
            if (list.Count > 0) playerIdleFrames = list.ToArray();
        }
#endif

        if (playerIdleFrames == null || playerIdleFrames.Length == 0)
        {
            string path = Path.Combine(Application.dataPath, "ART/Player/IDLE/idle_down.png");
            if (File.Exists(path))
            {
                byte[] data = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    tex.filterMode = FilterMode.Point;
                    int frameWidth = 96;
                    int frameHeight = 80;
                    int frameCount = tex.width / frameWidth;
                    var list = new List<Sprite>();
                    for (int i = 0; i < frameCount; i++)
                    {
                        var s = Sprite.Create(tex, new Rect(i * frameWidth, 0, frameWidth, frameHeight), new Vector2(0.5f, 0.5f), 100f);
                        list.Add(s);
                    }
                    if (list.Count > 0) playerIdleFrames = list.ToArray();
                }
            }
        }
    }

    public void BuildUI(Canvas canvas)
    {
        LoadPlayerIdleFrames();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        RectTransform rootRt = GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Canvas rootCanvas = GetComponentInParent<Canvas>() ?? canvas;
        if (rootCanvas != null)
        {
            var raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        Image bgDim = GetComponent<Image>();
        bgDim.color = new Color(0f, 0f, 0f, 0.88f);
        bgDim.raycastTarget = true;

        // Modal Frame (1180 x 640)
        dialogBox = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.transform.SetParent(transform, false);
        RectTransform dbRt = dialogBox.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0.5f, 0.5f);
        dbRt.anchorMax = new Vector2(0.5f, 0.5f);
        dbRt.sizeDelta = new Vector2(1180f, 640f);

        Image dbImg = dialogBox.GetComponent<Image>();
        dbImg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);
        dbImg.raycastTarget = false;
        var dbOutline = dialogBox.AddComponent<Outline>();
        dbOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 1f);
        dbOutline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -12f);
        tRt.sizeDelta = new Vector2(-40f, 32f);
        shopTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        shopTitleText.fontSize = 20;
        shopTitleText.fontStyle = FontStyles.Bold;
        shopTitleText.alignment = TextAlignmentOptions.Center;
        shopTitleText.color = new Color(1f, 0.88f, 0.45f);
        shopTitleText.raycastTarget = false;

        // Close Button (Top Right)
        GameObject closeBtnObj = new GameObject("Btn_CloseShop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(dialogBox.transform, false);
        RectTransform cbRt = closeBtnObj.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1f, 1f);
        cbRt.anchorMax = new Vector2(1f, 1f);
        cbRt.pivot = new Vector2(1f, 1f);
        cbRt.anchoredPosition = new Vector2(-12f, -12f);
        cbRt.sizeDelta = new Vector2(36f, 36f);

        Image cbImg = closeBtnObj.GetComponent<Image>();
        cbImg.color = new Color(0.6f, 0.15f, 0.15f, 1f);
        cbImg.raycastTarget = true;
        var cbOutline = closeBtnObj.AddComponent<Outline>();
        cbOutline.effectColor = new Color(0.9f, 0.7f, 0.3f, 0.8f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = cbImg;
        closeBtn.onClick.AddListener(Close);

        GameObject cbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform cbtRt = cbTxtObj.GetComponent<RectTransform>();
        cbtRt.anchorMin = Vector2.zero;
        cbtRt.anchorMax = Vector2.one;
        cbtRt.sizeDelta = Vector2.zero;
        var cbTmp = cbTxtObj.AddComponent<TextMeshProUGUI>();
        cbTmp.fontSize = 18;
        cbTmp.fontStyle = FontStyles.Bold;
        cbTmp.alignment = TextAlignmentOptions.Center;
        cbTmp.text = "X";
        cbTmp.color = Color.white;
        cbTmp.raycastTarget = false;

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.transform.SetParent(dialogBox.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -42f);
        subRt.sizeDelta = new Vector2(-40f, 18f);
        shopSubtitleText = subObj.AddComponent<TextMeshProUGUI>();
        shopSubtitleText.fontSize = 11.5f;
        shopSubtitleText.alignment = TextAlignmentOptions.Center;
        shopSubtitleText.color = new Color(0.4f, 0.85f, 0.65f);
        shopSubtitleText.raycastTarget = false;

        // 8 Category Tabs (ALL, HAT, SHIRT, PANTS, GLOVE, SHOE, SCARF, BAG)
        GameObject tabsContainer = new GameObject("CategoryTabsContainer", typeof(RectTransform));
        tabsContainer.transform.SetParent(dialogBox.transform, false);
        RectTransform tcRt = tabsContainer.GetComponent<RectTransform>();
        tcRt.anchorMin = new Vector2(0f, 1f);
        tcRt.anchorMax = new Vector2(1f, 1f);
        tcRt.pivot = new Vector2(0.5f, 1f);
        tcRt.anchoredPosition = new Vector2(0f, -65f);
        tcRt.sizeDelta = new Vector2(-40f, 32f);

        string[] tabLabelsVi = new string[] { "TAT CA", "NON", "AO", "QUAN", "BAO TAY", "GIAY", "KHAN", "TUI" };
        string[] tabLabelsEn = new string[] { "ALL", "HAT", "SHIRT", "PANTS", "GLOVE", "SHOE", "SCARF", "BAG" };

        tabButtons = new Button[8];
        tabButtonImgs = new Image[8];
        tabButtonTexts = new TextMeshProUGUI[8];

        float tabWidth = 136f;
        float spacing = 5f;
        float totalTabsWidth = 8 * tabWidth + 7 * spacing;
        float startX = -totalTabsWidth * 0.5f + tabWidth * 0.5f;

        for (int t = 0; t < 8; t++)
        {
            int filterIndex = t - 1; // -1 for ALL, 0..6 for slots
            GameObject tabBtnObj = new GameObject($"TabBtn_{t}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tabBtnObj.transform.SetParent(tabsContainer.transform, false);
            RectTransform tbRt = tabBtnObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 0.5f);
            tbRt.anchorMax = new Vector2(0.5f, 0.5f);
            tbRt.pivot = new Vector2(0.5f, 0.5f);
            tbRt.anchoredPosition = new Vector2(startX + t * (tabWidth + spacing), 0f);
            tbRt.sizeDelta = new Vector2(tabWidth, 30f);

            Image tbImg = tabBtnObj.GetComponent<Image>();
            tbImg.color = (t == 0) ? new Color(0.85f, 0.65f, 0.15f, 1f) : new Color(0.12f, 0.15f, 0.22f, 0.95f);
            tbImg.raycastTarget = true;
            tabButtonImgs[t] = tbImg;

            var tbOutline = tabBtnObj.AddComponent<Outline>();
            tbOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.6f);

            Button tBtn = tabBtnObj.GetComponent<Button>();
            tBtn.targetGraphic = tbImg;
            tBtn.onClick.AddListener(() => OnCategoryTabClicked(filterIndex));
            tabButtons[t] = tBtn;

            GameObject tTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            tTxtObj.transform.SetParent(tabBtnObj.transform, false);
            RectTransform tTxtRt = tTxtObj.GetComponent<RectTransform>();
            tTxtRt.anchorMin = Vector2.zero;
            tTxtRt.anchorMax = Vector2.one;
            tTxtRt.sizeDelta = Vector2.zero;
            var tTmp = tTxtObj.AddComponent<TextMeshProUGUI>();
            tTmp.fontSize = 10.5f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.text = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese ? tabLabelsVi[t] : tabLabelsEn[t];
            tTmp.color = Color.white;
            tTmp.raycastTarget = false;
            tabButtonTexts[t] = tTmp;
        }

        // LEFT SCROLLABLE GRID (Width 710)
        GameObject scrollObj = new GameObject("ItemsScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(dialogBox.transform, false);
        RectTransform sRt = scrollObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(0f, 1f);
        sRt.pivot = new Vector2(0f, 0.5f);
        sRt.anchoredPosition = new Vector2(20f, -50f);
        sRt.sizeDelta = new Vector2(720f, -125f);

        Image sImg = scrollObj.GetComponent<Image>();
        sImg.color = new Color(0.04f, 0.05f, 0.08f, 0.8f);
        sImg.raycastTarget = true;

        scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        GameObject contentObj = new GameObject("GridContent", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform cRt = contentObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0f, 1000f);
        gridContent = contentObj.transform;

        var grid = contentObj.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(134f, 132f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = cRt;

        // RIGHT INSPECTOR & LIVE HERO FITTING ROOM (Width 400)
        GameObject inspObj = new GameObject("ItemDetailInspector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inspObj.transform.SetParent(dialogBox.transform, false);
        RectTransform inspRt = inspObj.GetComponent<RectTransform>();
        inspRt.anchorMin = new Vector2(1f, 0f);
        inspRt.anchorMax = new Vector2(1f, 1f);
        inspRt.pivot = new Vector2(1f, 0.5f);
        inspRt.anchoredPosition = new Vector2(-20f, -50f);
        inspRt.sizeDelta = new Vector2(390f, -125f);

        Image inspImg = inspObj.GetComponent<Image>();
        inspImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);
        inspImg.raycastTarget = false;
        var inspOutline = inspObj.AddComponent<Outline>();
        inspOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.6f);

        // LIVE PLAYER FITTING PEDESTAL (Top Left of Inspector)
        GameObject fittingBox = new GameObject("LiveHeroFittingPedestal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        fittingBox.transform.SetParent(inspObj.transform, false);
        RectTransform fbRt = fittingBox.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0f, 1f);
        fbRt.anchorMax = new Vector2(0f, 1f);
        fbRt.pivot = new Vector2(0f, 1f);
        fbRt.anchoredPosition = new Vector2(12f, -12f);
        fbRt.sizeDelta = new Vector2(140f, 140f);

        Image fbImg = fittingBox.GetComponent<Image>();
        fbImg.color = new Color(0.03f, 0.04f, 0.07f, 1f);
        fbImg.raycastTarget = false;
        var fbOutline = fittingBox.AddComponent<Outline>();
        fbOutline.effectColor = new Color(0.35f, 0.75f, 0.95f, 0.8f);

        GameObject modelObj = new GameObject("LiveHeroModel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modelObj.transform.SetParent(fittingBox.transform, false);
        fittingModelRt = modelObj.GetComponent<RectTransform>();
        fittingModelRt.anchorMin = new Vector2(0.5f, 0.5f);
        fittingModelRt.anchorMax = new Vector2(0.5f, 0.5f);
        fittingModelRt.pivot = new Vector2(0.5f, 0.5f);
        fittingModelRt.anchoredPosition = Vector2.zero;
        fittingModelRt.sizeDelta = new Vector2(96f, 80f);
        fittingModelRt.localScale = new Vector2(1.8f, 1.8f);

        fittingModelImg = modelObj.GetComponent<Image>();
        fittingModelImg.preserveAspect = true;
        fittingModelImg.raycastTarget = false;
        if (playerIdleFrames != null && playerIdleFrames.Length > 0)
        {
            fittingModelImg.sprite = playerIdleFrames[0];
        }

        // Camera Focus Mode Tag overlay (Bottom of fitting box)
        GameObject tagObjBox = new GameObject("FocusTag", typeof(RectTransform), typeof(CanvasRenderer));
        tagObjBox.transform.SetParent(fittingBox.transform, false);
        RectTransform tagRt = tagObjBox.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0f, 0f);
        tagRt.anchorMax = new Vector2(1f, 0f);
        tagRt.pivot = new Vector2(0.5f, 0f);
        tagRt.anchoredPosition = new Vector2(0f, 2f);
        tagRt.sizeDelta = new Vector2(0f, 18f);
        fittingFocusTagText = tagObjBox.AddComponent<TextMeshProUGUI>();
        fittingFocusTagText.fontSize = 8.5f;
        fittingFocusTagText.fontStyle = FontStyles.Bold;
        fittingFocusTagText.alignment = TextAlignmentOptions.Center;
        fittingFocusTagText.color = new Color(0.35f, 0.85f, 1f, 0.95f);
        fittingFocusTagText.raycastTarget = false;

        Shader shader = Shader.Find("Custom/CharacterEquipmentShader");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            fittingModelMat = new Material(shader);
            fittingModelImg.material = fittingModelMat;
        }

        // Selected Item Icon Frame (Beside Live Hero)
        GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconFrame.transform.SetParent(inspObj.transform, false);
        RectTransform ifRt = iconFrame.GetComponent<RectTransform>();
        ifRt.anchorMin = new Vector2(1f, 1f);
        ifRt.anchorMax = new Vector2(1f, 1f);
        ifRt.pivot = new Vector2(1f, 1f);
        ifRt.anchoredPosition = new Vector2(-12f, -12f);
        ifRt.sizeDelta = new Vector2(85f, 85f);
        Image ifImg = iconFrame.GetComponent<Image>();
        ifImg.color = new Color(0.04f, 0.05f, 0.08f, 1f);
        ifImg.raycastTarget = false;
        var ifOutline = iconFrame.AddComponent<Outline>();
        ifOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.9f);

        GameObject iconObj = new GameObject("PreviewIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(iconFrame.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = new Vector2(-6f, -6f);
        detailIconImg = iconObj.GetComponent<Image>();
        detailIconImg.preserveAspect = true;
        detailIconImg.raycastTarget = false;

        // Detail Name & Slot Info
        GameObject nameObj = new GameObject("DetailName", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.transform.SetParent(inspObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -148f);
        nameRt.sizeDelta = new Vector2(-20f, 22f);
        detailNameText = nameObj.AddComponent<TextMeshProUGUI>();
        detailNameText.fontSize = 14f;
        detailNameText.fontStyle = FontStyles.Bold;
        detailNameText.alignment = TextAlignmentOptions.Center;
        detailNameText.color = new Color(1f, 0.88f, 0.45f);
        detailNameText.raycastTarget = false;

        GameObject tierObj = new GameObject("DetailSlotAndTier", typeof(RectTransform), typeof(CanvasRenderer));
        tierObj.transform.SetParent(inspObj.transform, false);
        RectTransform tierRt = tierObj.GetComponent<RectTransform>();
        tierRt.anchorMin = new Vector2(0f, 1f);
        tierRt.anchorMax = new Vector2(1f, 1f);
        tierRt.pivot = new Vector2(0.5f, 1f);
        tierRt.anchoredPosition = new Vector2(0f, -170f);
        tierRt.sizeDelta = new Vector2(-20f, 18f);
        detailTierText = tierObj.AddComponent<TextMeshProUGUI>();
        detailTierText.fontSize = 11f;
        detailTierText.alignment = TextAlignmentOptions.Center;
        detailTierText.raycastTarget = false;

        // Stats Box
        GameObject statsBox = new GameObject("StatsBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statsBox.transform.SetParent(inspObj.transform, false);
        RectTransform sbRt = statsBox.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.5f, 1f);
        sbRt.anchorMax = new Vector2(0.5f, 1f);
        sbRt.pivot = new Vector2(0.5f, 1f);
        sbRt.anchoredPosition = new Vector2(0f, -192f);
        sbRt.sizeDelta = new Vector2(360f, 70f);
        Image sbImg = statsBox.GetComponent<Image>();
        sbImg.color = new Color(0.04f, 0.05f, 0.08f, 0.9f);
        sbImg.raycastTarget = false;
        var sbOutline = statsBox.AddComponent<Outline>();
        sbOutline.effectColor = new Color(0.3f, 0.4f, 0.5f, 0.4f);

        GameObject stxtObj = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer));
        stxtObj.transform.SetParent(statsBox.transform, false);
        RectTransform stxtRt = stxtObj.GetComponent<RectTransform>();
        stxtRt.anchorMin = Vector2.zero;
        stxtRt.anchorMax = Vector2.one;
        stxtRt.sizeDelta = new Vector2(-10f, -6f);
        detailStatsText = stxtObj.AddComponent<TextMeshProUGUI>();
        detailStatsText.fontSize = 11f;
        detailStatsText.lineSpacing = 10f;
        detailStatsText.alignment = TextAlignmentOptions.Center;
        detailStatsText.raycastTarget = false;

        // Total Loadout Box
        GameObject totalBox = new GameObject("TotalLoadoutBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        totalBox.transform.SetParent(inspObj.transform, false);
        RectTransform totRt = totalBox.GetComponent<RectTransform>();
        totRt.anchorMin = new Vector2(0.5f, 1f);
        totRt.anchorMax = new Vector2(0.5f, 1f);
        totRt.pivot = new Vector2(0.5f, 1f);
        totRt.anchoredPosition = new Vector2(0f, -266f);
        totRt.sizeDelta = new Vector2(360f, 68f);
        Image totImg = totalBox.GetComponent<Image>();
        totImg.color = new Color(0.07f, 0.12f, 0.18f, 0.85f);
        totImg.raycastTarget = false;
        var totOutline = totalBox.AddComponent<Outline>();
        totOutline.effectColor = new Color(0.35f, 0.75f, 0.95f, 0.5f);

        GameObject totTxtObj = new GameObject("TotalStatsText", typeof(RectTransform), typeof(CanvasRenderer));
        totTxtObj.transform.SetParent(totalBox.transform, false);
        RectTransform totTxtRt = totTxtObj.GetComponent<RectTransform>();
        totTxtRt.anchorMin = Vector2.zero;
        totTxtRt.anchorMax = Vector2.one;
        totTxtRt.sizeDelta = new Vector2(-10f, -6f);
        detailTotalLoadoutStatsText = totTxtObj.AddComponent<TextMeshProUGUI>();
        detailTotalLoadoutStatsText.fontSize = 10.5f;
        detailTotalLoadoutStatsText.lineSpacing = 8f;
        detailTotalLoadoutStatsText.alignment = TextAlignmentOptions.Center;
        detailTotalLoadoutStatsText.raycastTarget = false;

        // Price Status
        GameObject priceObj = new GameObject("PriceTag", typeof(RectTransform), typeof(CanvasRenderer));
        priceObj.transform.SetParent(inspObj.transform, false);
        RectTransform prRt = priceObj.GetComponent<RectTransform>();
        prRt.anchorMin = new Vector2(0.5f, 0f);
        prRt.anchorMax = new Vector2(0.5f, 0f);
        prRt.pivot = new Vector2(0.5f, 0f);
        prRt.anchoredPosition = new Vector2(0f, 78f);
        prRt.sizeDelta = new Vector2(360f, 18f);
        detailStatusText = priceObj.AddComponent<TextMeshProUGUI>();
        detailStatusText.fontSize = 11f;
        detailStatusText.fontStyle = FontStyles.Bold;
        detailStatusText.alignment = TextAlignmentOptions.Center;
        detailStatusText.color = new Color(0.35f, 0.9f, 0.55f);
        detailStatusText.raycastTarget = false;

        // Unequip All Button
        GameObject unequipAllObj = new GameObject("Btn_UnequipAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        unequipAllObj.transform.SetParent(inspObj.transform, false);
        RectTransform unRt = unequipAllObj.GetComponent<RectTransform>();
        unRt.anchorMin = new Vector2(0.5f, 0f);
        unRt.anchorMax = new Vector2(0.5f, 0f);
        unRt.pivot = new Vector2(0.5f, 0f);
        unRt.anchoredPosition = new Vector2(0f, 48f);
        unRt.sizeDelta = new Vector2(360f, 24f);

        Image unImg = unequipAllObj.GetComponent<Image>();
        unImg.color = new Color(0.22f, 0.12f, 0.14f, 0.95f);
        unImg.raycastTarget = true;
        var unOutline = unequipAllObj.AddComponent<Outline>();
        unOutline.effectColor = new Color(0.85f, 0.35f, 0.35f, 0.6f);

        Button unBtn = unequipAllObj.GetComponent<Button>();
        unBtn.targetGraphic = unImg;
        unBtn.onClick.AddListener(OnUnequipAllClicked);

        GameObject unTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        unTxtObj.transform.SetParent(unequipAllObj.transform, false);
        RectTransform unTxtRt = unTxtObj.GetComponent<RectTransform>();
        unTxtRt.anchorMin = Vector2.zero;
        unTxtRt.anchorMax = Vector2.one;
        unTxtRt.sizeDelta = Vector2.zero;
        var unTmp = unTxtObj.AddComponent<TextMeshProUGUI>();
        unTmp.fontSize = 9.5f;
        unTmp.fontStyle = FontStyles.Bold;
        unTmp.alignment = TextAlignmentOptions.Center;
        unTmp.color = new Color(1f, 0.7f, 0.7f, 0.95f);
        unTmp.raycastTarget = false;
        unTmp.text = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese 
            ? "THAO TAT CA TRANG PHUC" 
            : "UNEQUIP ALL ITEMS";

        // Equip Action Button
        GameObject actBtnObj = new GameObject("Btn_EquipArmor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actBtnObj.transform.SetParent(inspObj.transform, false);
        RectTransform abRt = actBtnObj.GetComponent<RectTransform>();
        abRt.anchorMin = new Vector2(0.5f, 0f);
        abRt.anchorMax = new Vector2(0.5f, 0f);
        abRt.pivot = new Vector2(0.5f, 0f);
        abRt.anchoredPosition = new Vector2(0f, 10f);
        abRt.sizeDelta = new Vector2(360f, 34f);

        equipActionBtnImg = actBtnObj.GetComponent<Image>();
        equipActionBtnImg.color = new Color(0.18f, 0.55f, 0.28f, 1f);
        equipActionBtnImg.raycastTarget = true;
        var actOutline = actBtnObj.AddComponent<Outline>();
        actOutline.effectColor = new Color(0.9f, 0.8f, 0.3f, 0.8f);

        equipActionButton = actBtnObj.GetComponent<Button>();
        equipActionButton.targetGraphic = equipActionBtnImg;
        equipActionButton.onClick.AddListener(OnEquipButtonClicked);

        GameObject abTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        abTxtObj.transform.SetParent(actBtnObj.transform, false);
        RectTransform abtRt = abTxtObj.GetComponent<RectTransform>();
        abtRt.anchorMin = Vector2.zero;
        abtRt.anchorMax = Vector2.one;
        abtRt.sizeDelta = Vector2.zero;
        equipActionBtnText = abTxtObj.AddComponent<TextMeshProUGUI>();
        equipActionBtnText.fontSize = 13f;
        equipActionBtnText.fontStyle = FontStyles.Bold;
        equipActionBtnText.alignment = TextAlignmentOptions.Center;
        equipActionBtnText.color = Color.white;
        equipActionBtnText.raycastTarget = false;

        BuildItemCards();
    }

    private void OnCategoryTabClicked(int filterIndex)
    {
        currentCategoryFilter = filterIndex;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        for (int i = 0; i < tabButtonImgs.Length; i++)
        {
            if (tabButtonImgs[i] == null) continue;
            bool isActive = (i == (currentCategoryFilter + 1));
            tabButtonImgs[i].color = isActive ? new Color(0.95f, 0.75f, 0.15f, 1f) : new Color(0.12f, 0.15f, 0.22f, 0.95f);
        }

        BuildItemCards();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildItemCards()
    {
        InitDatabase();

        // 1. Thoroughly purge ALL existing children in gridContent (including any baked scene cards)
        if (gridContent != null)
        {
            for (int i = gridContent.childCount - 1; i >= 0; i--)
            {
                Transform child = gridContent.GetChild(i);
                if (child != null)
                {
                    child.SetParent(null); // Detach immediately so it leaves layout and raycasting pools instantly
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }
        cardGameObjects.Clear();

        List<ArmorItem> filteredList = new List<ArmorItem>();
        if (currentCategoryFilter == -1)
        {
            filteredList.AddRange(allArmorItems);
        }
        else
        {
            EquipmentSlot targetSlot = (EquipmentSlot)currentCategoryFilter;
            filteredList = allArmorItems.FindAll(x => x.slot == targetSlot);
        }

        // If selected item is not present in the filtered tab, select the first available item in tab
        if (filteredList.Count > 0 && !filteredList.Exists(x => x.id == selectedArmorId))
        {
            selectedArmorId = filteredList[0].id;
        }

        for (int i = 0; i < filteredList.Count; i++)
        {
            var item = filteredList[i];
            int itemId = item.id;

            GameObject cardObj = new GameObject($"Card_Armor_{itemId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardObj.transform.SetParent(gridContent, false);
            cardGameObjects.Add(cardObj);

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(134f, 132f);

            Image cardImg = cardObj.GetComponent<Image>();
            cardImg.color = GetTierCardBg(item.tier);
            cardImg.raycastTarget = true;

            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = GetTierColor(item.tier);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = cardObj.GetComponent<Button>();
            btn.targetGraphic = cardImg;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnItemCardClicked(itemId));
            AttachPointerClick(cardObj, () => OnItemCardClicked(itemId));

            // Slot Tag Top-Left
            GameObject slotTagObj = new GameObject("SlotTag", typeof(RectTransform), typeof(CanvasRenderer));
            slotTagObj.transform.SetParent(cardObj.transform, false);
            RectTransform stRt = slotTagObj.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 1f);
            stRt.anchorMax = new Vector2(0f, 1f);
            stRt.pivot = new Vector2(0f, 1f);
            stRt.anchoredPosition = new Vector2(4f, -4f);
            stRt.sizeDelta = new Vector2(70f, 14f);
            var stTmp = slotTagObj.AddComponent<TextMeshProUGUI>();
            stTmp.fontSize = 8f;
            stTmp.fontStyle = FontStyles.Bold;
            stTmp.alignment = TextAlignmentOptions.Left;
            stTmp.color = new Color(0.6f, 0.8f, 1f, 0.85f);
            stTmp.raycastTarget = false;
            stTmp.text = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese ? GetSlotNameVi(item.slot) : GetSlotNameEn(item.slot);

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -14f);
            iconRt.sizeDelta = new Vector2(65f, 65f);
            Image icImg = iconObj.GetComponent<Image>();
            icImg.sprite = item.sprite;
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;

            // Name
            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer));
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nRt = nameObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0f);
            nRt.anchorMax = new Vector2(1f, 0f);
            nRt.pivot = new Vector2(0.5f, 0f);
            nRt.anchoredPosition = new Vector2(0f, 20f);
            nRt.sizeDelta = new Vector2(-6f, 26f);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 9.5f;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.text = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese ? item.nameVi : item.nameEn;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;

            // Equipped Tag Badge
            GameObject tagObj = new GameObject("EquipTag", typeof(RectTransform), typeof(CanvasRenderer));
            tagObj.transform.SetParent(cardObj.transform, false);
            RectTransform tRt = tagObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 3f);
            tRt.sizeDelta = new Vector2(0f, 15f);
            var tagTmp = tagObj.AddComponent<TextMeshProUGUI>();
            tagTmp.fontSize = 9f;
            tagTmp.fontStyle = FontStyles.Bold;
            tagTmp.alignment = TextAlignmentOptions.Center;
            tagTmp.raycastTarget = false;
        }

        RefreshAllCardBadges();
        HighlightSelectedCard(selectedArmorId);
        InspectArmor(selectedArmorId);
    }

    private Color GetTierColor(ArmorTier tier)
    {
        switch (tier)
        {
            case ArmorTier.Common: return new Color(0.6f, 0.65f, 0.7f, 0.8f);
            case ArmorTier.Rare: return new Color(0.2f, 0.7f, 1f, 0.9f);
            case ArmorTier.Epic: return new Color(0.75f, 0.35f, 1f, 0.95f);
            case ArmorTier.Legendary: return new Color(1f, 0.85f, 0.25f, 1f);
            case ArmorTier.Mythic: return new Color(1f, 0.25f, 0.35f, 1f);
            default: return Color.white;
        }
    }

    private Color GetTierCardBg(ArmorTier tier)
    {
        switch (tier)
        {
            case ArmorTier.Common: return new Color(0.09f, 0.11f, 0.15f, 0.95f);
            case ArmorTier.Rare: return new Color(0.08f, 0.14f, 0.22f, 0.95f);
            case ArmorTier.Epic: return new Color(0.14f, 0.10f, 0.22f, 0.95f);
            case ArmorTier.Legendary: return new Color(0.18f, 0.14f, 0.08f, 0.95f);
            case ArmorTier.Mythic: return new Color(0.22f, 0.08f, 0.10f, 0.95f);
            default: return new Color(0.1f, 0.1f, 0.15f, 0.95f);
        }
    }

    private void OnItemCardClicked(int id)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        selectedArmorId = id;
        HighlightSelectedCard(id);
        InspectArmor(id);
    }

    private void HighlightSelectedCard(int selectedId)
    {
        for (int i = 0; i < cardGameObjects.Count; i++)
        {
            var card = cardGameObjects[i];
            if (card == null) continue;
            var outline = card.GetComponent<Outline>();
            if (outline == null) continue;

            string objName = card.name;
            if (int.TryParse(objName.Replace("Card_Armor_", ""), out int itemId))
            {
                if (itemId == selectedId)
                {
                    outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f); // Bright Gold
                    outline.effectDistance = new Vector2(3f, -3f);
                }
                else
                {
                    var it = allArmorItems.Find(x => x.id == itemId);
                    outline.effectColor = it != null ? GetTierColor(it.tier) : Color.white;
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }
    }

    private void InspectArmor(int id)
    {
        selectedArmorId = id;
        InitDatabase();
        var item = allArmorItems.Find(x => x.id == id) ?? allArmorItems[0];

        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (detailIconImg != null) detailIconImg.sprite = item.sprite;
        if (detailNameText != null)
        {
            detailNameText.text = isVi ? item.nameVi : item.nameEn;
            detailNameText.color = GetTierColor(item.tier);
        }
        if (detailTierText != null)
        {
            string slotStr = isVi ? GetSlotNameVi(item.slot) : GetSlotNameEn(item.slot);
            string tierName = item.tier.ToString().ToUpper();
            detailTierText.text = $"{slotStr} - {tierName}";
            detailTierText.color = GetTierColor(item.tier);
        }
        if (detailStatsText != null)
        {
            string hpStr = isVi ? $"MAU (HP): <color=#4EFFA0>+{item.hpBonus}</color>" : $"MAX HP: <color=#4EFFA0>+{item.hpBonus}</color>";
            string defStr = isVi ? $"GIAM ST: <color=#66D6FF>+{item.defensePercent:F1}%</color>" : $"MITIGATION: <color=#66D6FF>+{item.defensePercent:F1}%</color>";
            string spdStr = isVi ? $"TOC DO: <color=#FFE666>+{item.speedBonus:F2}</color>" : $"SPEED: <color=#FFE666>+{item.speedBonus:F2}</color>";
            string stamStr = isVi ? $"HOI THE LUC: <color=#FFAA44>+{item.staminaRegenPercent:F0}%</color>" : $"STAMINA: <color=#FFAA44>+{item.staminaRegenPercent:F0}%</color>";

            detailStatsText.text = $"{hpStr}    {defStr}\n{spdStr}    {stamStr}";
        }

        if (detailTotalLoadoutStatsText != null)
        {
            detailTotalLoadoutStatsText.fontSize = 12f;
            detailTotalLoadoutStatsText.lineSpacing = 6f;
            GetTotalEquippedGearBonus(out int tHp, out float tDef, out float tSpd, out float tStam);
            string header = isVi ? "<color=#FFE266>TONG CHI SO TRANG BI</color>" : "<color=#FFE266>TOTAL EQUIPPED STATS</color>";
            string hpLine = isVi ? $"Tong Mau: +{tHp}   Giam ST: +{tDef:F1}%" : $"Total HP: +{tHp}   Mitigation: +{tDef:F1}%";
            string spdLine = isVi ? $"Toc Do: +{tSpd:F2}   Hoi Luc: +{tStam:F0}%" : $"Speed: +{tSpd:F2}   Stamina: +{tStam:F0}%";
            detailTotalLoadoutStatsText.text = $"{header}\n{hpLine}\n{spdLine}";
        }

        if (detailStatusText != null)
        {
            detailStatusText.text = isVi ? "TRANG THAI: MIEN PHI - DA MO KHOA" : "STATUS: FREE - ALL UNLOCKED";
        }

        bool isEquipped = IsItemEquipped(id);

        if (equipActionBtnText != null)
        {
            equipActionBtnText.text = isEquipped 
                ? (isVi ? "DANG MAC - BAM DE THAO" : "EQUIPPED - CLICK TO REMOVE")
                : (isVi ? "TRANG BI VAO NGUOI" : "EQUIP TO HERO");
        }

        if (equipActionBtnImg != null)
        {
            equipActionBtnImg.color = isEquipped 
                ? new Color(0.72f, 0.20f, 0.20f, 1f) 
                : new Color(0.18f, 0.58f, 0.28f, 1f);
        }

        UpdateFittingModelVisual(item);
        FocusFittingCameraOnSlot(item.slot);
        HighlightSelectedCard(selectedArmorId);
    }

    private void FocusFittingCameraOnSlot(EquipmentSlot slot)
    {
        if (fittingModelRt == null) return;

        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;
        float targetScale = 2.0f;
        Vector2 targetPos = Vector2.zero;
        string tagText = isVi ? "XEM TOAN THAN" : "FULL BODY VIEW";

        switch (slot)
        {
            case EquipmentSlot.Boots: // SHOE / SOCKS (Feet at bottom)
                targetScale = 2.7f;
                targetPos = new Vector2(0f, 28f);
                tagText = isVi ? "CAN CANH: GIAY & VO" : "CLOSE-UP: SHOES & SOCKS";
                break;

            case EquipmentSlot.Gloves: // GLOVE / HANDWRAPS
                targetScale = 2.5f;
                targetPos = new Vector2(0f, 2f);
                tagText = isVi ? "CAN CANH: BAO TAY" : "CLOSE-UP: GLOVES";
                break;

            case EquipmentSlot.Helmet: // HAT / CAP / EARMUFFS (Head at top)
                targetScale = 2.6f;
                targetPos = new Vector2(0f, -22f);
                tagText = isVi ? "CAN CANH: NON & MU" : "CLOSE-UP: HATS & CAPS";
                break;

            case EquipmentSlot.Pants: // PANTS / TROUSERS
                targetScale = 2.5f;
                targetPos = new Vector2(0f, 18f);
                tagText = isVi ? "CAN CANH: QUAN" : "CLOSE-UP: PANTS";
                break;

            case EquipmentSlot.Cloak: // SCARF
                targetScale = 2.5f;
                targetPos = new Vector2(0f, -10f);
                tagText = isVi ? "CAN CANH: KHAN CHOANG" : "CLOSE-UP: SCARF";
                break;

            case EquipmentSlot.Armor: // SHIRT / COAT
                targetScale = 2.3f;
                targetPos = new Vector2(0f, -2f);
                tagText = isVi ? "CAN CANH: AO & AO KHOAC" : "CLOSE-UP: SHIRT & COATS";
                break;

            case EquipmentSlot.Bag: // BAG / ACCESSORY
                targetScale = 2.2f;
                targetPos = Vector2.zero;
                tagText = isVi ? "CAN CANH: PHU KIEN & TUI" : "CLOSE-UP: ACCESSORY & BAG";
                break;
        }

        if (fittingFocusTagText != null)
        {
            fittingFocusTagText.text = tagText;
        }

        if (cameraFocusCoroutine != null)
        {
            StopCoroutine(cameraFocusCoroutine);
            cameraFocusCoroutine = null;
        }

        if (gameObject.activeInHierarchy && Application.isPlaying)
        {
            cameraFocusCoroutine = StartCoroutine(TransitionCameraFocus(targetPos, targetScale));
        }
        else if (fittingModelRt != null)
        {
            fittingModelRt.anchoredPosition = targetPos;
            fittingModelRt.localScale = new Vector3(targetScale, targetScale, 1f);
        }
    }

    private IEnumerator TransitionCameraFocus(Vector2 targetPos, float targetScale)
    {
        if (fittingModelRt == null) yield break;

        Vector2 startPos = fittingModelRt.anchoredPosition;
        float startScale = fittingModelRt.localScale.x;
        float elapsed = 0f;
        float duration = 0.22f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (fittingModelRt != null)
            {
                fittingModelRt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                float curScale = Mathf.Lerp(startScale, targetScale, t);
                fittingModelRt.localScale = new Vector3(curScale, curScale, 1f);
            }
            yield return null;
        }

        if (fittingModelRt != null)
        {
            fittingModelRt.anchoredPosition = targetPos;
            fittingModelRt.localScale = new Vector3(targetScale, targetScale, 1f);
        }
        cameraFocusCoroutine = null;
    }

    private void UpdateFittingModelVisual(ArmorItem previewItem)
    {
        if (fittingModelMat == null && fittingModelImg != null)
        {
            Shader shader = Shader.Find("Custom/CharacterEquipmentShader");
            if (shader != null) fittingModelMat = new Material(shader);
            fittingModelImg.material = fittingModelMat;
        }

        // Collect current loadout and override with previewItem
        int helmetId = (previewItem.slot == EquipmentSlot.Helmet) ? previewItem.id : GetEquippedIdForSlot(EquipmentSlot.Helmet);
        int armorId = (previewItem.slot == EquipmentSlot.Armor) ? previewItem.id : GetEquippedIdForSlot(EquipmentSlot.Armor);
        int pantsId = (previewItem.slot == EquipmentSlot.Pants) ? previewItem.id : GetEquippedIdForSlot(EquipmentSlot.Pants);
        int glovesId = (previewItem.slot == EquipmentSlot.Gloves) ? previewItem.id : GetEquippedIdForSlot(EquipmentSlot.Gloves);
        int bootsId = (previewItem.slot == EquipmentSlot.Boots) ? previewItem.id : GetEquippedIdForSlot(EquipmentSlot.Boots);
        int cloakId = (previewItem.slot == EquipmentSlot.Cloak) ? previewItem.id : GetEquippedIdForSlot(EquipmentSlot.Cloak);

        if (fittingModelMat != null)
        {
            Color defaultArmorPri = new Color(0.77f, 0.14f, 0.19f, 1f);
            Color defaultArmorSec = new Color(0.54f, 0.12f, 0.17f, 1f);
            Color defaultCloak = new Color(0.85f, 0.20f, 0.20f, 1f);
            Color defaultPants = new Color(0.24f, 0.24f, 0.24f, 1f);
            Color defaultGloves = new Color(0.85f, 0.85f, 0.85f, 1f);
            Color defaultBoots = new Color(0.36f, 0.17f, 0.15f, 1f);
            Color defaultHelmet = new Color(0.54f, 0.28f, 0.21f, 1f);

            Color armorPri = (armorId > 0) ? PlayerEquipmentVisual.GetItemPrimaryColor(armorId, defaultArmorPri) : defaultArmorPri;
            Color armorSec = (armorId > 0) ? PlayerEquipmentVisual.GetItemSecondaryColor(armorId, defaultArmorSec) : defaultArmorSec;
            Color cloakCol = (cloakId > 0) ? PlayerEquipmentVisual.GetItemPrimaryColor(cloakId, defaultCloak) : defaultCloak;
            Color pantsCol = (pantsId > 0) ? PlayerEquipmentVisual.GetItemPrimaryColor(pantsId, defaultPants) : defaultPants;
            Color glovesCol = (glovesId > 0) ? PlayerEquipmentVisual.GetItemPrimaryColor(glovesId, defaultGloves) : defaultGloves;
            Color bootsCol = (bootsId > 0) ? PlayerEquipmentVisual.GetItemPrimaryColor(bootsId, defaultBoots) : defaultBoots;
            Color helmCol = (helmetId > 0) ? PlayerEquipmentVisual.GetItemPrimaryColor(helmetId, defaultHelmet) : defaultHelmet;

            fittingModelMat.SetColor("_ArmorColor", armorPri);
            fittingModelMat.SetColor("_ArmorSecColor", armorSec);
            fittingModelMat.SetColor("_CloakColor", cloakCol);
            fittingModelMat.SetColor("_PantsColor", pantsCol);
            fittingModelMat.SetColor("_GlovesColor", glovesCol);
            fittingModelMat.SetColor("_BootsColor", bootsCol);
            fittingModelMat.SetColor("_HelmetColor", helmCol);

            // Tier Glow for Preview item
            ArmorTier previewTier = previewItem.tier;
            switch (previewTier)
            {
                case ArmorTier.Mythic:
                    fittingModelMat.SetColor("_TierGlowColor", new Color(1f, 0.2f, 0.35f, 1f));
                    fittingModelMat.SetFloat("_TierGlowIntensity", 0.45f);
                    break;
                case ArmorTier.Legendary:
                    fittingModelMat.SetColor("_TierGlowColor", new Color(1f, 0.85f, 0.25f, 1f));
                    fittingModelMat.SetFloat("_TierGlowIntensity", 0.35f);
                    break;
                case ArmorTier.Epic:
                    fittingModelMat.SetColor("_TierGlowColor", new Color(0.75f, 0.35f, 1f, 1f));
                    fittingModelMat.SetFloat("_TierGlowIntensity", 0.25f);
                    break;
                case ArmorTier.Rare:
                    fittingModelMat.SetColor("_TierGlowColor", new Color(0.2f, 0.7f, 1f, 1f));
                    fittingModelMat.SetFloat("_TierGlowIntensity", 0.15f);
                    break;
                default:
                    fittingModelMat.SetColor("_TierGlowColor", Color.black);
                    fittingModelMat.SetFloat("_TierGlowIntensity", 0f);
                    break;
            }
        }
    }

    private void OnUnequipAllClicked()
    {
        UnequipAllSlots();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        RefreshAllCardBadges();
        InspectArmor(selectedArmorId);

        var pHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (pHealth != null)
        {
            GetEquippedArmorBonus(out int hp, out float def, out float spd);
            pHealth.ApplyUpgradeBonus(hp);
        }

        var mainMgr = MainMenuManager.Instance;
        if (mainMgr != null)
        {
            mainMgr.RefreshCurrencies();
            mainMgr.RefreshHeroDisplay();
        }
    }

    private void OnEquipButtonClicked()
    {
        var item = allArmorItems.Find(x => x.id == selectedArmorId);
        if (item == null) return;

        if (IsItemEquipped(selectedArmorId))
        {
            // Unequip currently equipped item
            UnequipSlot(item.slot);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        }
        else
        {
            // Equip new item
            EquipItemToSlot(selectedArmorId);
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySwordSlash();
        }

        RefreshAllCardBadges();
        InspectArmor(selectedArmorId);

        var pHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (pHealth != null)
        {
            GetEquippedArmorBonus(out int hp, out float def, out float spd);
            pHealth.ApplyUpgradeBonus(hp);
        }

        // Notify Main Menu Hero previews
        var mainMgr = MainMenuManager.Instance;
        if (mainMgr != null)
        {
            mainMgr.RefreshCurrencies();
            mainMgr.RefreshHeroDisplay();
        }
    }

    private void RefreshAllCardBadges()
    {
        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        for (int i = 0; i < cardGameObjects.Count; i++)
        {
            var card = cardGameObjects[i];
            if (card == null) continue;

            string objName = card.name;
            if (int.TryParse(objName.Replace("Card_Armor_", ""), out int itemId))
            {
                bool isEq = IsItemEquipped(itemId);

                var tagTransform = card.transform.Find("EquipTag");
                if (tagTransform != null)
                {
                    var tmp = tagTransform.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = isEq 
                            ? (isVi ? "<color=#4EFFA0>DANG MAC</color>" : "<color=#4EFFA0>EQUIPPED</color>")
                            : (isVi ? "<color=#88AACC>SO HUU</color>" : "<color=#88AACC>OWNED</color>");
                    }
                }
            }
        }
    }

    public void Open()
    {
        isOpen = true;
        EnsureEventSystemActive();

        // Final safety: if UI was never built (somehow), build it now before making visible
        if (dialogBox == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = Object.FindAnyObjectByType<Canvas>();
            if (parentCanvas != null) BuildUI(parentCanvas);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        LoadPlayerIdleFrames();
        RefreshUI();

        if (fittingAnimCoroutine != null)
        {
            StopCoroutine(fittingAnimCoroutine);
            fittingAnimCoroutine = null;
        }

        if (gameObject.activeInHierarchy && Application.isPlaying)
        {
            fittingAnimCoroutine = StartCoroutine(AnimateFittingModel());
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();
    }

    public void Close()
    {
        isOpen = false;
        if (fittingAnimCoroutine != null)
        {
            StopCoroutine(fittingAnimCoroutine);
            fittingAnimCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
    }

    private IEnumerator AnimateFittingModel()
    {
        while (isOpen)
        {
            if (playerIdleFrames != null && playerIdleFrames.Length > 0 && fittingModelImg != null)
            {
                fittingFrameIdx = (fittingFrameIdx + 1) % playerIdleFrames.Length;
                fittingModelImg.sprite = playerIdleFrames[fittingFrameIdx];
                if (fittingModelMat != null && fittingModelImg.material != fittingModelMat)
                {
                    fittingModelImg.material = fittingModelMat;
                }
            }
            yield return new WaitForSecondsRealtime(0.09f);
        }
    }

    public bool IsOpen() => isOpen;

    public void RefreshUI()
    {
        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (shopTitleText != null)
        {
            shopTitleText.text = isVi ? "PHONG THU DO & CUA HANG TRANG PHUC" : "FITTING ROOM & WARDROBE VAULT";
        }
        if (shopSubtitleText != null)
        {
            shopSubtitleText.text = isVi 
                ? "7 DANH MUC TRANG BI THOI TRANG MUA DONG - XEM TRUC TIEP TREN NGUOI NHAN VAT"
                : "7 WINTER FASHION CATEGORIES - LIVE HERO FITTING PREVIEW";
        }

        string[] tabLabelsVi = new string[] { "TAT CA", "NON (HAT)", "AO (SHIRT)", "QUAN (PANTS)", "BAO TAY (GLOVE)", "GIAY (SHOE)", "KHAN (SCARF)", "TUI (BAG)" };
        string[] tabLabelsEn = new string[] { "ALL", "HAT", "SHIRT", "PANTS", "GLOVE", "SHOE", "SCARF", "BAG" };
        if (tabButtonTexts != null)
        {
            for (int t = 0; t < tabButtonTexts.Length; t++)
            {
                if (tabButtonTexts[t] != null)
                {
                    tabButtonTexts[t].text = isVi ? tabLabelsVi[t] : tabLabelsEn[t];
                }
            }
        }

        RefreshAllCardBadges();
        InspectArmor(selectedArmorId);
    }
}
