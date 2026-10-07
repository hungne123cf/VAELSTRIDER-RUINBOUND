// ========================================================
// VAELSTRIDER: RUINBOUND - Comprehensive Inventory & Gacha UI
// Full Item Bag for Battle Drops, Equipment & Gacha Vault System
// ========================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    private static InventoryUI instance;
    public static InventoryUI Instance => instance;

    private CanvasGroup canvasGroup;
    private GameObject dialogBox;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;

    // Tabs (5 Tabs: All, Equip, Material, Artifacts, Gacha)
    private int currentCategoryFilter = -1; // -1: All, 0: Equip, 1: Material, 2: Artifacts, 99: Gacha
    private Button[] tabButtons;
    private Image[] tabButtonImgs;
    private TextMeshProUGUI[] tabButtonTexts;

    // Grid Scroll Area
    private GameObject gridAreaObj;
    private Transform gridContent;
    private ScrollRect scrollRect;
    private List<GameObject> slotGameObjects = new List<GameObject>();

    // Right Inspector Panel
    private GameObject rightInspectorObj;
    private Image detailIconBoxImg;
    private Image detailIconImg;
    private TextMeshProUGUI detailNameText;
    private TextMeshProUGUI detailTierBadgeText;
    private TextMeshProUGUI detailSourceText;
    private TextMeshProUGUI detailStatsText;
    private TextMeshProUGUI detailDescText;
    private TextMeshProUGUI detailQuantityText;
    private Button actionButton;
    private TextMeshProUGUI actionBtnText;
    private Image actionBtnImg;
    private Button openAllButton;
    private TextMeshProUGUI openAllBtnText;
    private Image openAllBtnImg;
    private Button socketButton;
    private TextMeshProUGUI socketBtnText;
    private Image socketBtnImg;
    private Button dismantleButton;
    private TextMeshProUGUI dismantleBtnText;
    private Image dismantleBtnImg;
    private Button closeButton;

    // Gem Socket Modal
    private GameObject gemSocketModalObj;
    private TextMeshProUGUI socketModalWeaponTitle;
    private Image socketModalWeaponIcon;
    private TextMeshProUGUI socketModalStatsBonusText;
    private Transform socketSlotsContainer;
    private Transform availableGemsContainer;
    private List<GameObject> gemCardObjs = new List<GameObject>();
    private List<GameObject> socketSlotObjs = new List<GameObject>();
    private int selectedSocketSlotIndex = 0;
    private string targetEquipIdForSocketing = "";
    private Button autoMergeAllBtn;
    private TextMeshProUGUI autoMergeAllBtnText;

    // Gacha Altar Section
    private GameObject gachaAltarObj;
    private TextMeshProUGUI gachaShardsBalanceText;
    private TextMeshProUGUI gachaScrollsBalanceText;
    private Button gachaSingleRollBtn;
    private Button gachaMultiRollBtn;

    // Gacha Result Modal
    private GameObject gachaResultModalObj;
    private Transform gachaResultCardContainer;
    private Button gachaResultConfirmBtn;
    private List<GameObject> resultCardObjs = new List<GameObject>();

    // Chest Reward Modal
    private GameObject chestRewardModalObj;
    private Image chestRewardIconImg;
    private TextMeshProUGUI chestRewardTitleText;
    private TextMeshProUGUI chestRewardSubText;
    private Transform chestRewardCardsContainer;
    private Button chestRewardConfirmBtn;
    private List<GameObject> chestRewardCardObjs = new List<GameObject>();
    private static Sprite sprChestClosed = null;
    private static Sprite sprChestOpen = null;
    private static Sprite sprAethelShard = null;

    // Gacha Hybrid Confirmation Modal
    private GameObject gachaHybridConfirmModalObj;
    private TextMeshProUGUI gachaHybridPromptText;
    private Button gachaHybridYesBtn;
    private Button gachaHybridNoBtn;
    private System.Action onGachaHybridConfirmAction;

    private string selectedItemId = "";
    private bool isOpen = false;
    private bool isStandaloneSkillMode = false;

    // Item Frame Sprites (Only applied to Item Icon Frames & Cards)
    private static Sprite sprItemSlotFrame = null;
    private static Sprite sprSkillAttack = null;
    private static Sprite sprSkillDash = null;
    private static Sprite sprSkillUlti = null;

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

    private static Sprite LoadHeroSkillSprite(string heroId, int skillIdx)
    {
        string h = string.IsNullOrEmpty(heroId) ? "vael" : heroId.ToLower();
        bool isTitan = (h.Contains("titan") || h.Contains("player4"));
        bool isAzure = (h.Contains("azure") || h.Contains("player3"));
        bool isRonin = (h.Contains("ronin") || h.Contains("gale") || h.Contains("maid"));

        string fileName = (skillIdx == 0) ? "icon_attack.png" : (skillIdx == 1 ? "icon_dash.png" : "icon_ulti.png");
        string resourceName = (skillIdx == 0) ? "icon_attack" : (skillIdx == 1 ? "icon_dash" : "icon_ulti");

        Sprite spr = null;
        if (isTitan)
        {
#if UNITY_EDITOR
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Player4/UI/{fileName}");
#endif
            if (spr == null) spr = Resources.Load<Sprite>($"Player4/UI/{resourceName}");
        }
        else if (isRonin)
        {
#if UNITY_EDITOR
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Player2/UI/{fileName}");
#endif
            if (spr == null) spr = Resources.Load<Sprite>($"Player2/UI/{resourceName}");
        }
        else if (isAzure)
        {
#if UNITY_EDITOR
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Player3/UI/{fileName}");
#endif
            if (spr == null) spr = Resources.Load<Sprite>($"Player3/UI/{resourceName}");
        }
        else
        {
            string oldPath = (skillIdx == 0) ? "Assets/Sprites/UI/Attack.png" : (skillIdx == 1 ? "Assets/Sprites/UI/Dash.png" : "Assets/Sprites/UI/Ulti.png");
            string oldRes = (skillIdx == 0) ? "UI/Attack" : (skillIdx == 1 ? "UI/Dash" : "UI/Ulti");
#if UNITY_EDITOR
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(oldPath);
#endif
            if (spr == null) spr = Resources.Load<Sprite>(oldRes);
        }
        return spr;
    }

    private static void EnsureItemSprites()
    {
        if (sprItemSlotFrame == null) sprItemSlotFrame = LoadFramedSprite("Border/panel-border-014.png", new Vector4(8, 8, 8, 8));
        if (sprItemSlotFrame == null) sprItemSlotFrame = LoadFramedSprite("Border/panel-border-000.png", new Vector4(8, 8, 8, 8));

        if (sprSkillAttack == null)
        {
#if UNITY_EDITOR
            sprSkillAttack = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Attack.png");
#endif
            if (sprSkillAttack == null) sprSkillAttack = Resources.Load<Sprite>("UI/Attack");
        }
        if (sprSkillDash == null)
        {
#if UNITY_EDITOR
            sprSkillDash = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Dash.png");
#endif
            if (sprSkillDash == null) sprSkillDash = Resources.Load<Sprite>("UI/Dash");
        }
        if (sprSkillUlti == null)
        {
#if UNITY_EDITOR
            sprSkillUlti = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Ulti.png");
#endif
            if (sprSkillUlti == null) sprSkillUlti = Resources.Load<Sprite>("UI/Ulti");
        }

        if (sprChestClosed == null)
        {
            sprChestClosed = Resources.Load<Sprite>("Items/chest_closed");
#if UNITY_EDITOR
            if (sprChestClosed == null)
                sprChestClosed = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_closed.png");
#endif
        }
        if (sprChestOpen == null)
        {
            sprChestOpen = Resources.Load<Sprite>("Items/chest_open");
#if UNITY_EDITOR
            if (sprChestOpen == null)
                sprChestOpen = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_open.png");
#endif
        }
        if (sprAethelShard == null)
        {
            sprAethelShard = Resources.Load<Sprite>("Items/aethel_shard");
#if UNITY_EDITOR
            if (sprAethelShard == null)
                sprAethelShard = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/aethel_shard.png");
#endif
        }
    }

    void Awake()
    {
        if (instance == null) instance = this;
        InventoryItemDatabase.Initialize();
        EnsureItemSprites();
        if (dialogBox == null || tabButtons == null || tabButtons.Length == 0)
        {
            BuildUI();
        }
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLanguageTexts;
        LocalizationManager.OnLanguageChanged += RefreshLanguageTexts;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLanguageTexts;
    }

    private void RefreshLanguageTexts()
    {
        if (titleText != null)
            titleText.text = LocalizationManager.T("TÚI ĐỒ & ĐÀI QUAY VẬN MỆNH", "INVENTORY & GACHA VAULT");
        if (subtitleText != null)
            subtitleText.text = LocalizationManager.T("Lưu trữ trang bị, khoáng thạch & Dùng Linh Hồn quay Thần Binh cổ đại", "Store battle gear & spend Aethel Shards to summon mythic artifacts");

        if (tabButtonTexts != null)
        {
            string[] tabLabelsVi = new string[] { "TẤT CẢ", "TRANG BỊ", "KHOÁNG THẠCH", "LÕI THẦN BÍ", "QUAY GACHA" };
            string[] tabLabelsEn = new string[] { "ALL", "EQUIPMENT", "MATERIALS", "ARTIFACTS", "GACHA VAULT" };
            for (int i = 0; i < tabButtonTexts.Length && i < tabLabelsVi.Length; i++)
            {
                if (tabButtonTexts[i] != null)
                    tabButtonTexts[i].text = LocalizationManager.T(tabLabelsVi[i], tabLabelsEn[i]);
            }
        }

        if (isOpen)
        {
            if (gemSocketModalObj != null && gemSocketModalObj.activeSelf)
            {
                RefreshGemSocketModal();
            }
            else if (currentCategoryFilter == 99)
            {
                UpdateGachaBalanceDisplay();
            }
            else
            {
                RefreshGrid();
                if (!string.IsNullOrEmpty(selectedItemId))
                {
                    InspectItem(selectedItemId);
                }
            }
        }
    }

    public static InventoryUI EnsureUIExists()
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("InventoryModalPanel");
        if (existing != null)
        {
            instance = existing.GetComponent<InventoryUI>();
            if (instance != null)
            {
                if (instance.dialogBox == null || instance.tabButtons == null || instance.tabButtons.Length == 0)
                {
                    instance.BuildUI();
                }
                return instance;
            }
        }

        GameObject panelObj = new GameObject("InventoryModalPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.layer = 5;
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        panelImg.raycastTarget = true;

        instance = panelObj.AddComponent<InventoryUI>();
        instance.canvasGroup = panelObj.GetComponent<CanvasGroup>();
        instance.canvasGroup.alpha = 0f;
        instance.canvasGroup.blocksRaycasts = false;
        instance.canvasGroup.interactable = false;

        instance.BuildUI();
        panelObj.SetActive(false);
        return instance;
    }

    public void BuildUI()
    {
        InventoryItemDatabase.Initialize();
        EnsureItemSprites();

        // Clear previous children if any to prevent duplicate or orphaned UI
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 1. Center Modal Box - 100% Edge-to-Edge Full Screen
        dialogBox = new GameObject("InventoryBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(transform, false);

        RectTransform boxRt = dialogBox.GetComponent<RectTransform>();
        boxRt.anchorMin = Vector2.zero;
        boxRt.anchorMax = Vector2.one;
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.offsetMin = Vector2.zero;
        boxRt.offsetMax = Vector2.zero;

        Image boxImg = dialogBox.GetComponent<Image>();
        boxImg.color = new Color(0.04f, 0.05f, 0.09f, 1f);

        var outline = dialogBox.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.95f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // 2. Header Title
        GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer));
        headerObj.layer = 5;
        headerObj.transform.SetParent(dialogBox.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -25f);
        headerRt.sizeDelta = new Vector2(0f, 40f);

        titleText = headerObj.AddComponent<TextMeshProUGUI>();
        titleText.text = LocalizationManager.T("TÚI ĐỒ & ĐÀI QUAY VẬN MỆNH", "INVENTORY & GACHA VAULT");
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.35f);
        titleText.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(dialogBox.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -68f);
        subRt.sizeDelta = new Vector2(0f, 26f);

        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = LocalizationManager.T("Lưu trữ trang bị, khoáng thạch & Dùng Linh Hồn quay Thần Binh cổ đại", "Store battle gear & spend Aethel Shards to summon mythic artifacts");
        subtitleText.fontSize = 15;
        subtitleText.color = new Color(0.7f, 0.75f, 0.85f);
        subtitleText.alignment = TextAlignmentOptions.Center;

        // 3. Category Filter Tabs
        BuildCategoryTabs(dialogBox.transform);

        // 4. Left Grid Area
        BuildGridScrollView(dialogBox.transform);

        // 5. Right Inspector Panel
        BuildRightInspectorPanel(dialogBox.transform);

        // 6. Gacha Altar Section
        BuildGachaAltarSection(dialogBox.transform);

        // 7. Gacha Result Reveal Popup (Directly under Panel root)
        BuildGachaResultModal(transform);

        // 8. Gem Socket Forge Modal (Directly under Panel root to avoid overlapping InventoryBox)
        BuildGemSocketModal(transform);

        // 9. Chest Reward Modal (Directly under Panel root)
        BuildChestRewardModal(transform);

        // 10. Gacha Hybrid Confirmation Modal (Directly under Panel root)
        BuildGachaHybridConfirmModal(transform);

        // 11. Close Button
        BuildCloseButton(dialogBox.transform);
    }

    private void BuildCategoryTabs(Transform parent)
    {
        string[] tabLabelsVi = new string[] { "TẤT CẢ", "TRANG BỊ", "KHOÁNG THẠCH", "LÕI THẦN BÍ", "QUAY GACHA" };
        string[] tabLabelsEn = new string[] { "ALL", "EQUIPMENT", "MATERIALS", "ARTIFACTS", "GACHA VAULT" };

        int count = tabLabelsVi.Length;
        tabButtons = new Button[count];
        tabButtonImgs = new Image[count];
        tabButtonTexts = new TextMeshProUGUI[count];

        float spacing = 230f;
        float startX = -((count - 1) * spacing) / 2f;

        for (int i = 0; i < count; i++)
        {
            int index = i;
            int catFilter;
            if (i == 0) catFilter = -1; // All
            else if (i == count - 1) catFilter = 99; // Gacha
            else catFilter = i - 1;

            GameObject tabObj = new GameObject($"Tab_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tabObj.layer = 5;
            tabObj.transform.SetParent(parent, false);

            RectTransform tabRt = tabObj.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0.5f, 1f);
            tabRt.anchorMax = new Vector2(0.5f, 1f);
            tabRt.pivot = new Vector2(0.5f, 1f);
            tabRt.anchoredPosition = new Vector2(startX + (i * spacing), -108f);
            tabRt.sizeDelta = new Vector2(215f, 44f);

            Image img = tabObj.GetComponent<Image>();
            img.color = (i == count - 1) ? new Color(0.55f, 0.18f, 0.72f, 1f) : new Color(0.12f, 0.15f, 0.22f, 0.95f);

            var tabOutline = tabObj.AddComponent<Outline>();
            tabOutline.effectColor = (i == count - 1) ? new Color(1f, 0.85f, 0.35f, 0.9f) : new Color(0.85f, 0.75f, 0.4f, 0.6f);
            tabOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = tabObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnTabClicked(catFilter, index));

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            txtObj.layer = 5;
            txtObj.transform.SetParent(tabObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = LocalizationManager.T(tabLabelsVi[i], tabLabelsEn[i]);
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            tabButtons[i] = btn;
            tabButtonImgs[i] = img;
            tabButtonTexts[i] = tmp;
        }
    }

    private void BuildGridScrollView(Transform parent)
    {
        gridAreaObj = new GameObject("InventoryScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        gridAreaObj.layer = 5;
        gridAreaObj.transform.SetParent(parent, false);

        RectTransform scrollRt = gridAreaObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(0.62f, 1f);
        scrollRt.pivot = new Vector2(0.5f, 0.5f);
        scrollRt.offsetMin = new Vector2(40f, 30f);
        scrollRt.offsetMax = new Vector2(-15f, -170f);

        Image scrollBg = gridAreaObj.GetComponent<Image>();
        scrollBg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        var gridOutline = gridAreaObj.AddComponent<Outline>();
        gridOutline.effectColor = new Color(0.25f, 0.32f, 0.45f, 0.6f);
        gridOutline.effectDistance = new Vector2(1.5f, -1.5f);

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
        grid.cellSize = new Vector2(125f, 135f);
        grid.spacing = new Vector2(15f, 15f);
        grid.padding = new RectOffset(15, 15, 15, 15);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        gridContent = contentObj.transform;
        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;
    }

    private void BuildRightInspectorPanel(Transform parent)
    {
        rightInspectorObj = new GameObject("InspectorPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightInspectorObj.layer = 5;
        rightInspectorObj.transform.SetParent(parent, false);

        RectTransform rt = rightInspectorObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.62f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(15f, 30f);
        rt.offsetMax = new Vector2(-40f, -170f);

        Image img = rightInspectorObj.GetComponent<Image>();
        img.color = new Color(0.05f, 0.06f, 0.10f, 0.98f);

        var rightOutline = rightInspectorObj.AddComponent<Outline>();
        rightOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.75f);
        rightOutline.effectDistance = new Vector2(2f, -2f);

        // 1. Icon Box with Framed Sprite
        GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconBox.layer = 5;
        iconBox.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform iconBoxRt = iconBox.GetComponent<RectTransform>();
        iconBoxRt.anchoredPosition = new Vector2(0f, 172f);
        iconBoxRt.sizeDelta = new Vector2(76f, 76f);
        
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
        iconRt.sizeDelta = new Vector2(-16f, -16f);
        detailIconImg = iconObj.GetComponent<Image>();
        detailIconImg.preserveAspect = true;

        // 2. Name
        GameObject nameObj = new GameObject("ItemName", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.layer = 5;
        nameObj.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchoredPosition = new Vector2(0f, 120f);
        nameRt.sizeDelta = new Vector2(350f, 26f);
        detailNameText = nameObj.AddComponent<TextMeshProUGUI>();
        detailNameText.fontSize = 16;
        detailNameText.fontStyle = FontStyles.Bold;
        detailNameText.alignment = TextAlignmentOptions.Center;
        detailNameText.color = new Color(1f, 0.9f, 0.45f);
        detailNameText.overflowMode = TextOverflowModes.Ellipsis;
        detailNameText.enableWordWrapping = false;

        // 3. Tier & Category Badge
        GameObject tierObj = new GameObject("TierBadge", typeof(RectTransform), typeof(CanvasRenderer));
        tierObj.layer = 5;
        tierObj.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform tierRt = tierObj.GetComponent<RectTransform>();
        tierRt.anchoredPosition = new Vector2(0f, 96f);
        tierRt.sizeDelta = new Vector2(350f, 20f);
        detailTierBadgeText = tierObj.AddComponent<TextMeshProUGUI>();
        detailTierBadgeText.fontSize = 11.5f;
        detailTierBadgeText.fontStyle = FontStyles.Bold;
        detailTierBadgeText.alignment = TextAlignmentOptions.Center;
        detailTierBadgeText.color = new Color(0.3f, 0.8f, 1f);

        // 4. Source Text
        GameObject srcObj = new GameObject("SourceText", typeof(RectTransform), typeof(CanvasRenderer));
        srcObj.layer = 5;
        srcObj.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform srcRt = srcObj.GetComponent<RectTransform>();
        srcRt.anchoredPosition = new Vector2(0f, 75f);
        srcRt.sizeDelta = new Vector2(350f, 18f);
        detailSourceText = srcObj.AddComponent<TextMeshProUGUI>();
        detailSourceText.fontSize = 10.5f;
        detailSourceText.alignment = TextAlignmentOptions.Center;
        detailSourceText.color = new Color(0.6f, 0.7f, 0.85f);
        detailSourceText.enableWordWrapping = false;

        // 5. Middle Info Card (Dark Slate Container with border)
        GameObject cardObj = new GameObject("InfoCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.layer = 5;
        cardObj.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchoredPosition = new Vector2(0f, -8f);
        cardRt.sizeDelta = new Vector2(350f, 138f);
        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.03f, 0.04f, 0.07f, 0.92f);

        var cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.25f, 0.32f, 0.45f, 0.6f);
        cardOutline.effectDistance = new Vector2(1f, -1f);

        // Stats Summary inside InfoCard
        GameObject statsObj = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer));
        statsObj.layer = 5;
        statsObj.transform.SetParent(cardObj.transform, false);
        RectTransform statsRt = statsObj.GetComponent<RectTransform>();
        statsRt.anchoredPosition = new Vector2(0f, 30f);
        statsRt.sizeDelta = new Vector2(330f, 54f);
        detailStatsText = statsObj.AddComponent<TextMeshProUGUI>();
        detailStatsText.fontSize = 11f;
        detailStatsText.fontStyle = FontStyles.Bold;
        detailStatsText.alignment = TextAlignmentOptions.Center;
        detailStatsText.color = new Color(0.25f, 1f, 0.65f);
        detailStatsText.lineSpacing = -3f;
        detailStatsText.enableWordWrapping = true;
        detailStatsText.overflowMode = TextOverflowModes.Truncate;

        // Description Lore inside InfoCard
        GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(CanvasRenderer));
        descObj.layer = 5;
        descObj.transform.SetParent(cardObj.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchoredPosition = new Vector2(0f, -30f);
        descRt.sizeDelta = new Vector2(330f, 52f);
        detailDescText = descObj.AddComponent<TextMeshProUGUI>();
        detailDescText.fontSize = 10f;
        detailDescText.alignment = TextAlignmentOptions.Center;
        detailDescText.color = new Color(0.80f, 0.85f, 0.92f);
        detailDescText.lineSpacing = -2f;
        detailDescText.enableWordWrapping = true;
        detailDescText.overflowMode = TextOverflowModes.Truncate;

        // 6. Quantity counter
        GameObject qtyObj = new GameObject("QuantityText", typeof(RectTransform), typeof(CanvasRenderer));
        qtyObj.layer = 5;
        qtyObj.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform qtyRt = qtyObj.GetComponent<RectTransform>();
        qtyRt.anchoredPosition = new Vector2(0f, -96f);
        qtyRt.sizeDelta = new Vector2(340f, 22f);
        detailQuantityText = qtyObj.AddComponent<TextMeshProUGUI>();
        detailQuantityText.fontSize = 13f;
        detailQuantityText.fontStyle = FontStyles.Bold;
        detailQuantityText.alignment = TextAlignmentOptions.Center;
        detailQuantityText.color = new Color(1f, 0.85f, 0.3f);
        detailQuantityText.enableAutoSizing = true;
        detailQuantityText.fontSizeMin = 9.5f;
        detailQuantityText.fontSizeMax = 13f;

        // 7. Action Button
        GameObject actionObj = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actionObj.layer = 5;
        actionObj.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform actionRt = actionObj.GetComponent<RectTransform>();
        actionRt.anchoredPosition = new Vector2(0f, -145f);
        actionRt.sizeDelta = new Vector2(300f, 44f);

        actionBtnImg = actionObj.GetComponent<Image>();
        actionBtnImg.color = new Color(0.18f, 0.65f, 0.38f, 1f);

        var actionOutline = actionObj.AddComponent<Outline>();
        actionOutline.effectColor = new Color(0.9f, 0.8f, 0.4f, 0.8f);
        actionOutline.effectDistance = new Vector2(1.5f, -1.5f);

        actionButton = actionObj.GetComponent<Button>();
        actionButton.targetGraphic = actionBtnImg;
        actionButton.onClick.AddListener(OnActionButtonClicked);

        GameObject actTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        actTxtObj.layer = 5;
        actTxtObj.transform.SetParent(actionObj.transform, false);
        RectTransform actTxtRt = actTxtObj.GetComponent<RectTransform>();
        actTxtRt.anchorMin = Vector2.zero;
        actTxtRt.anchorMax = Vector2.one;
        actTxtRt.sizeDelta = Vector2.zero;
        actionBtnText = actTxtObj.AddComponent<TextMeshProUGUI>();
        actionBtnText.text = LocalizationManager.T("SỬ DỤNG", "USE");
        actionBtnText.fontSize = 13.5f;
        actionBtnText.fontStyle = FontStyles.Bold;
        actionBtnText.color = Color.white;
        actionBtnText.alignment = TextAlignmentOptions.Center;
        actionBtnText.enableAutoSizing = true;
        actionBtnText.fontSizeMin = 8.5f;
        actionBtnText.fontSizeMax = 13.5f;
        actionBtnText.enableWordWrapping = false;
        actionBtnText.overflowMode = TextOverflowModes.Ellipsis;
        actionBtnText.margin = new Vector4(4f, 2f, 4f, 2f);

        // 8. Open All Button (Appears next to actionButton when >= 2 chests)
        GameObject openAllObj = new GameObject("OpenAllButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        openAllObj.layer = 5;
        openAllObj.transform.SetParent(rightInspectorObj.transform, false);
        RectTransform openAllRt = openAllObj.GetComponent<RectTransform>();
        openAllRt.anchoredPosition = new Vector2(92f, -145f);
        openAllRt.sizeDelta = new Vector2(176f, 44f);

        openAllBtnImg = openAllObj.GetComponent<Image>();
        openAllBtnImg.color = new Color(0.9f, 0.55f, 0.12f, 1f);

        var openAllOutline = openAllObj.AddComponent<Outline>();
        openAllOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.9f);
        openAllOutline.effectDistance = new Vector2(1.5f, -1.5f);

        openAllButton = openAllObj.GetComponent<Button>();
        openAllButton.targetGraphic = openAllBtnImg;
        openAllButton.onClick.AddListener(OnOpenAllButtonClicked);

        GameObject oTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        oTxtObj.layer = 5;
        oTxtObj.transform.SetParent(openAllObj.transform, false);
        RectTransform oTxtRt = oTxtObj.GetComponent<RectTransform>();
        oTxtRt.anchorMin = Vector2.zero;
        oTxtRt.anchorMax = Vector2.one;
        oTxtRt.sizeDelta = Vector2.zero;
        openAllBtnText = oTxtObj.AddComponent<TextMeshProUGUI>();
        openAllBtnText.text = LocalizationManager.T("MỞ TẤT CẢ", "OPEN ALL");
        openAllBtnText.fontSize = 13f;
        openAllBtnText.fontStyle = FontStyles.Bold;
        openAllBtnText.color = Color.white;
        openAllBtnText.alignment = TextAlignmentOptions.Center;
        openAllBtnText.enableAutoSizing = true;
        openAllBtnText.fontSizeMin = 8.5f;
        openAllBtnText.fontSizeMax = 13f;
        openAllBtnText.enableWordWrapping = false;
        openAllBtnText.overflowMode = TextOverflowModes.Ellipsis;
        openAllBtnText.margin = new Vector4(4f, 2f, 4f, 2f);
        openAllObj.SetActive(false);
    }

    private void BuildGachaAltarSection(Transform parent)
    {
        gachaAltarObj = new GameObject("GachaAltarSection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gachaAltarObj.layer = 5;
        gachaAltarObj.transform.SetParent(parent, false);

        RectTransform rt = gachaAltarObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(40f, 30f);
        rt.offsetMax = new Vector2(-40f, -170f);

        Image img = gachaAltarObj.GetComponent<Image>();
        img.color = new Color(0.06f, 0.07f, 0.12f, 0.98f);

        var altarOutline = gachaAltarObj.AddComponent<Outline>();
        altarOutline.effectColor = new Color(0.7f, 0.35f, 0.9f, 0.85f);
        altarOutline.effectDistance = new Vector2(2f, -2f);

        // Banner Title
        GameObject titleObj = new GameObject("AltarTitle", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.layer = 5;
        titleObj.transform.SetParent(gachaAltarObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 190f);
        titleRt.sizeDelta = new Vector2(900f, 36f);
        var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = LocalizationManager.T("ĐÀI TRIỆU HỒI VẬN MỆNH CỔ ĐẠI", "ANCIENT GACHA SUMMON ALTAR");
        tTmp.fontSize = 22;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = new Color(1f, 0.85f, 0.35f);
        tTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("AltarSub", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(gachaAltarObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 155f);
        subRt.sizeDelta = new Vector2(900f, 26f);
        var sTmp = subObj.AddComponent<TextMeshProUGUI>();
        sTmp.text = LocalizationManager.T("Bỏ Linh Hồn hoặc Bùa Triệu Hồi để quay ngẫu nhiên Thần Khí, Vũ Khí và Đá Quý", "Spend Aethel Shards or Summon Scrolls to roll for Mythic Blades & Relics");
        sTmp.fontSize = 13;
        sTmp.color = new Color(0.75f, 0.8f, 0.9f);
        sTmp.alignment = TextAlignmentOptions.Center;

        // Balance Bar
        GameObject balanceObj = new GameObject("BalanceBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        balanceObj.layer = 5;
        balanceObj.transform.SetParent(gachaAltarObj.transform, false);
        RectTransform balRt = balanceObj.GetComponent<RectTransform>();
        balRt.anchoredPosition = new Vector2(0f, 115f);
        balRt.sizeDelta = new Vector2(700f, 38f);
        Image balImg = balanceObj.GetComponent<Image>();
        balImg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        var balOutline = balanceObj.AddComponent<Outline>();
        balOutline.effectColor = new Color(0.4f, 0.5f, 0.7f, 0.6f);
        balOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject shardsTxtObj = new GameObject("ShardsText", typeof(RectTransform), typeof(CanvasRenderer));
        shardsTxtObj.layer = 5;
        shardsTxtObj.transform.SetParent(balanceObj.transform, false);
        RectTransform sRt = shardsTxtObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(0.5f, 1f);
        sRt.sizeDelta = Vector2.zero;
        gachaShardsBalanceText = shardsTxtObj.AddComponent<TextMeshProUGUI>();
        gachaShardsBalanceText.fontSize = 15;
        gachaShardsBalanceText.fontStyle = FontStyles.Bold;
        gachaShardsBalanceText.color = new Color(0.3f, 0.85f, 1f);
        gachaShardsBalanceText.alignment = TextAlignmentOptions.Center;

        GameObject scrollTxtObj = new GameObject("ScrollsText", typeof(RectTransform), typeof(CanvasRenderer));
        scrollTxtObj.layer = 5;
        scrollTxtObj.transform.SetParent(balanceObj.transform, false);
        RectTransform scRt = scrollTxtObj.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.5f, 0f);
        scRt.anchorMax = new Vector2(1f, 1f);
        scRt.sizeDelta = Vector2.zero;
        gachaScrollsBalanceText = scrollTxtObj.AddComponent<TextMeshProUGUI>();
        gachaScrollsBalanceText.fontSize = 15;
        gachaScrollsBalanceText.fontStyle = FontStyles.Bold;
        gachaScrollsBalanceText.color = new Color(1f, 0.85f, 0.3f);
        gachaScrollsBalanceText.alignment = TextAlignmentOptions.Center;

        // Drop Rate Guide Box
        GameObject rateBox = new GameObject("RateBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rateBox.layer = 5;
        rateBox.transform.SetParent(gachaAltarObj.transform, false);
        RectTransform rateRt = rateBox.GetComponent<RectTransform>();
        rateRt.anchoredPosition = new Vector2(0f, 25f);
        rateRt.sizeDelta = new Vector2(900f, 120f);
        Image rateImg = rateBox.GetComponent<Image>();
        rateImg.color = new Color(0.08f, 0.1f, 0.16f, 0.85f);

        var rateOutline = rateBox.AddComponent<Outline>();
        rateOutline.effectColor = new Color(0.3f, 0.35f, 0.5f, 0.6f);
        rateOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject rateTxtObj = new GameObject("RateText", typeof(RectTransform), typeof(CanvasRenderer));
        rateTxtObj.layer = 5;
        rateTxtObj.transform.SetParent(rateBox.transform, false);
        RectTransform rRt = rateTxtObj.GetComponent<RectTransform>();
        rRt.anchorMin = Vector2.zero;
        rRt.anchorMax = Vector2.one;
        rRt.offsetMin = new Vector2(15f, 10f);
        rRt.offsetMax = new Vector2(-15f, -10f);
        var rTmp = rateTxtObj.AddComponent<TextMeshProUGUI>();
        rTmp.text = LocalizationManager.T(
            "<b>TỶ LỆ TRIỆU HỒI VẬN MỆNH:</b>\n" +
            "- <color=#FF4757>[THẦN THOẠI 3%]</color>: Thánh Kiếm Excalibur (+45 ATK), Khiên Long Thần, Lõi Linh Hồn (+1000 Shards)\n" +
            "- <color=#FFA502>[HUYỀN THOẠI 12%]</color>: Hỏa Diệm Kiếm (+32 ATK), Chìa Khóa Tàn Tích, Khiên Long Thần\n" +
            "- <color=#9B59B6>[SỬ THI 25%]</color>: Hắc Dạ Katana (+24 ATK), Rương Tàn Tích Cổ, Huyết Thạch Ma Thuật\n" +
            "- <color=#3742FA>[HIẾM 35%]</color>: Kiếm Hiệp Sĩ, Khiên Hoàng Gia, Thạch Anh Tím, Ngọc Bích, Quặng Vàng, Quặng Thiếc\n" +
            "- <color=#CED6E0>[PHỔ THÔNG 25%]</color>: Đá Luyện Kim, Đá Trấn Cung, Quặng Đồng, Quặng Sắt Hắc Ám",
            "<b>SUMMON DROP RATES:</b>\n" +
            "- <color=#FF4757>[MYTHIC 3%]</color>: Excalibur Blade (+45 ATK), Dragon Shield, Abyssal Core (+1000 Shards)\n" +
            "- <color=#FFA502>[LEGENDARY 12%]</color>: Flamebrand Sword, Ancient Key, Dragon Shield\n" +
            "- <color=#9B59B6>[EPIC 25%]</color>: Shadow Katana, Relic Chest, Bloodstone Crystal\n" +
            "- <color=#3742FA>[RARE 35%]</color>: Paladin Blade, Royal Shield, Amethyst, Jade, Gold Node, Tin Ore\n" +
            "- <color=#CED6E0>[COMMON 25%]</color>: Forge Stone, Keepstone, Copper Ore, Dark Iron Ore");
        rTmp.fontSize = 12;
        rTmp.lineSpacing = 1.2f;
        rTmp.alignment = TextAlignmentOptions.TopLeft;

        // Roll 1 Button
        gachaSingleRollBtn = CreateGachaButton(gachaAltarObj.transform, "Btn_GachaSingle",
            LocalizationManager.T("QUAY 1 LẦN\n<size=13><color=#FFD700>100 Linh Hồn / 1 Bùa</color></size>", "ROLL 1x\n<size=13><color=#FFD700>100 Shards / 1 Scroll</color></size>"),
            new Vector2(-220f, -145f), new Vector2(340f, 65f), new Color(0.2f, 0.5f, 0.85f, 1f));
        gachaSingleRollBtn.onClick.AddListener(() => PerformGachaRoll(1));

        // Roll 10 Button
        gachaMultiRollBtn = CreateGachaButton(gachaAltarObj.transform, "Btn_GachaMulti",
            LocalizationManager.T("QUAY 10 LẦN (TIẾT KIỆM 10%)\n<size=13><color=#FFD700>900 Linh Hồn / 10 Bùa</color></size>", "ROLL 10x (SAVE 10%)\n<size=13><color=#FFD700>900 Shards / 10 Scrolls</color></size>"),
            new Vector2(220f, -145f), new Vector2(380f, 65f), new Color(0.85f, 0.45f, 0.15f, 1f));
        gachaMultiRollBtn.onClick.AddListener(() => PerformGachaRoll(10));

        gachaAltarObj.SetActive(false);
    }

    private Button CreateGachaButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.color = bgColor;

        var outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private void BuildGachaResultModal(Transform parent)
    {
        gachaResultModalObj = new GameObject("GachaResultModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gachaResultModalObj.layer = 5;
        gachaResultModalObj.transform.SetParent(parent, false);

        RectTransform rt = gachaResultModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = gachaResultModalObj.GetComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.04f, 0.96f);

        GameObject boxObj = new GameObject("ResultBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(gachaResultModalObj.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(1050f, 600f);
        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.08f, 0.09f, 0.15f, 1f);

        var boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.9f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        // Result Title
        GameObject tObj = new GameObject("ResultTitle", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.layer = 5;
        tObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 245f);
        tRt.sizeDelta = new Vector2(800f, 45f);
        var tTmp = tObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = LocalizationManager.T("KẾT QUẢ TRIỆU HỒI VẬN MỆNH", "GACHA SUMMON RESULTS");
        tTmp.fontSize = 24;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = new Color(1f, 0.85f, 0.35f);
        tTmp.alignment = TextAlignmentOptions.Center;

        // Cards Container
        GameObject containerObj = new GameObject("CardsContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        containerObj.layer = 5;
        containerObj.transform.SetParent(boxObj.transform, false);
        RectTransform cRt = containerObj.GetComponent<RectTransform>();
        cRt.anchoredPosition = new Vector2(0f, 25f);
        cRt.sizeDelta = new Vector2(980f, 360f);

        var grid = containerObj.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(165f, 160f);
        grid.spacing = new Vector2(25f, 20f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        gachaResultCardContainer = containerObj.transform;

        // Confirm Button
        gachaResultConfirmBtn = CreateGachaButton(boxObj.transform, "Btn_ConfirmResult",
            LocalizationManager.T("NHẬN CHIẾN LỢI PHẨM", "CLAIM REWARDS"),
            new Vector2(0f, -240f), new Vector2(360f, 50f), new Color(0.18f, 0.65f, 0.38f, 1f));
        gachaResultConfirmBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            gachaResultModalObj.SetActive(false);
            UpdateGachaBalanceDisplay();
            if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();
        });

        gachaResultModalObj.SetActive(false);
    }

    private void BuildChestRewardModal(Transform parent)
    {
        chestRewardModalObj = new GameObject("ChestRewardModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        chestRewardModalObj.layer = 5;
        chestRewardModalObj.transform.SetParent(parent, false);

        RectTransform rt = chestRewardModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = chestRewardModalObj.GetComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.04f, 0.96f);

        GameObject boxObj = new GameObject("ChestBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(chestRewardModalObj.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(760f, 460f);
        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.07f, 0.08f, 0.14f, 1f);

        var boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
        boxOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Header Title
        GameObject tObj = new GameObject("ChestTitle", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.layer = 5;
        tObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 185f);
        tRt.sizeDelta = new Vector2(700f, 40f);
        chestRewardTitleText = tObj.AddComponent<TextMeshProUGUI>();
        chestRewardTitleText.text = LocalizationManager.T("MỞ RƯƠNG TÀN TÍCH THÀNH CÔNG!", "ANCIENT CHEST OPENED!");
        chestRewardTitleText.fontSize = 20;
        chestRewardTitleText.fontStyle = FontStyles.Bold;
        chestRewardTitleText.color = new Color(1f, 0.88f, 0.35f);
        chestRewardTitleText.alignment = TextAlignmentOptions.Center;

        // Big Chest Icon (Animated)
        GameObject ciObj = new GameObject("ChestIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ciObj.layer = 5;
        ciObj.transform.SetParent(boxObj.transform, false);
        RectTransform ciRt = ciObj.GetComponent<RectTransform>();
        ciRt.anchoredPosition = new Vector2(0f, 115f);
        ciRt.sizeDelta = new Vector2(86f, 86f);
        chestRewardIconImg = ciObj.GetComponent<Image>();
        chestRewardIconImg.preserveAspect = true;

        // Subtitle
        GameObject subObj = new GameObject("ChestSub", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(0f, 52f);
        sRt.sizeDelta = new Vector2(700f, 24f);
        chestRewardSubText = subObj.AddComponent<TextMeshProUGUI>();
        chestRewardSubText.text = LocalizationManager.T("Bạn đã nhận được các vật phẩm sau:", "You have acquired the following rewards:");
        chestRewardSubText.fontSize = 13;
        chestRewardSubText.color = new Color(0.75f, 0.8f, 0.9f);
        chestRewardSubText.alignment = TextAlignmentOptions.Center;

        // Reward Cards ScrollView (Anti-Overflow with Touch Swipe for Mobile)
        GameObject scrollObj = new GameObject("CardsScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.layer = 5;
        scrollObj.transform.SetParent(boxObj.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchoredPosition = new Vector2(0f, -32f);
        scrollRt.sizeDelta = new Vector2(720f, 115f);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 25f;

        // Content
        GameObject contentObj = new GameObject("CardsContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.layer = 5;
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 115f);

        var hlg = contentObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(12, 12, 5, 5);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRt;
        scrollRect.viewport = scrollRt;

        chestRewardCardsContainer = contentObj.transform;

        // Confirm Button
        chestRewardConfirmBtn = CreateGachaButton(boxObj.transform, "Btn_ConfirmChest",
            LocalizationManager.T("NHẬN CHIẾN LỢI PHẨM", "CLAIM REWARDS"),
            new Vector2(0f, -175f), new Vector2(280f, 46f), new Color(0.18f, 0.65f, 0.38f, 1f));
        chestRewardConfirmBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            chestRewardModalObj.SetActive(false);
            RefreshGrid();
        });

        chestRewardModalObj.SetActive(false);
    }

    private void BuildGachaHybridConfirmModal(Transform parent)
    {
        gachaHybridConfirmModalObj = new GameObject("GachaHybridConfirmModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gachaHybridConfirmModalObj.layer = 5;
        gachaHybridConfirmModalObj.transform.SetParent(parent, false);

        RectTransform rt = gachaHybridConfirmModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = gachaHybridConfirmModalObj.GetComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.04f, 0.92f);

        GameObject boxObj = new GameObject("ConfirmBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(gachaHybridConfirmModalObj.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(660f, 380f);
        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.08f, 0.10f, 0.16f, 1f);

        var boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
        boxOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Header Title
        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.layer = 5;
        tObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 145f);
        tRt.sizeDelta = new Vector2(600f, 40f);
        var tTmp = tObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = LocalizationManager.T("XÁC NHẬN BÙ SHARDS QUAY 10 LẦN", "10x SUMMON SHARD TOP-UP");
        tTmp.fontSize = 20;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = new Color(1f, 0.85f, 0.35f);
        tTmp.alignment = TextAlignmentOptions.Center;

        // Content Area / Prompt Box
        GameObject contentBox = new GameObject("ContentBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentBox.layer = 5;
        contentBox.transform.SetParent(boxObj.transform, false);
        RectTransform cRt = contentBox.GetComponent<RectTransform>();
        cRt.anchoredPosition = new Vector2(0f, 25f);
        cRt.sizeDelta = new Vector2(580f, 170f);
        Image cImg = contentBox.GetComponent<Image>();
        cImg.color = new Color(0.04f, 0.05f, 0.09f, 0.9f);

        var cOutline = contentBox.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.3f, 0.4f, 0.6f, 0.5f);
        cOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject pObj = new GameObject("PromptText", typeof(RectTransform), typeof(CanvasRenderer));
        pObj.layer = 5;
        pObj.transform.SetParent(contentBox.transform, false);
        RectTransform pRt = pObj.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = new Vector2(20f, 15f);
        pRt.offsetMax = new Vector2(-20f, -15f);
        gachaHybridPromptText = pObj.AddComponent<TextMeshProUGUI>();
        gachaHybridPromptText.fontSize = 15;
        gachaHybridPromptText.lineSpacing = 1.25f;
        gachaHybridPromptText.alignment = TextAlignmentOptions.Center;
        gachaHybridPromptText.color = Color.white;

        // Yes Button
        gachaHybridYesBtn = CreateGachaButton(boxObj.transform, "Btn_ConfirmYes",
            LocalizationManager.T("XÁC NHẬN QUAY", "CONFIRM ROLL"),
            new Vector2(-150f, -135f), new Vector2(240f, 50f), new Color(0.18f, 0.65f, 0.38f, 1f));
        gachaHybridYesBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            gachaHybridConfirmModalObj.SetActive(false);
            onGachaHybridConfirmAction?.Invoke();
        });

        // No Button
        gachaHybridNoBtn = CreateGachaButton(boxObj.transform, "Btn_ConfirmNo",
            LocalizationManager.T("HỦY BỎ", "CANCEL"),
            new Vector2(150f, -135f), new Vector2(240f, 50f), new Color(0.45f, 0.2f, 0.2f, 1f));
        gachaHybridNoBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            gachaHybridConfirmModalObj.SetActive(false);
        });

        gachaHybridConfirmModalObj.SetActive(false);
    }

    private void ShowGachaHybridConfirmModal(int currentScrolls, int missingPulls, int topUpShardCost, System.Action onConfirm)
    {
        if (gachaHybridConfirmModalObj == null) return;

        onGachaHybridConfirmAction = onConfirm;

        if (gachaHybridPromptText != null)
        {
            gachaHybridPromptText.text = LocalizationManager.T(
                $"Bạn đang có: <color=#FFA502><b>{currentScrolls} Bùa Triệu Hồi</b></color>\n" +
                $"Số vé còn thiếu: <color=#FF4757><b>{missingPulls} Vé</b></color>\n" +
                $"Chi phí bù thêm: <color=#00E5FF><b>{topUpShardCost} Linh Hồn</b></color> <size=13><color=#2ED573>(Đã giảm 10% gói 10x)</color></size>\n\n" +
                $"<i>Bạn có muốn dùng {currentScrolls} Bùa + {topUpShardCost} Linh Hồn để hoàn tất Quay 10 Lần không?</i>",
                $"You currently have: <color=#FFA502><b>{currentScrolls} Summon Scrolls</b></color>\n" +
                $"Missing tickets: <color=#FF4757><b>{missingPulls} Scrolls</b></color>\n" +
                $"Top-up cost: <color=#00E5FF><b>{topUpShardCost} Aethel Shards</b></color> <size=13><color=#2ED573>(10x Discount Applied)</color></size>\n\n" +
                $"<i>Would you like to spend {currentScrolls} Scrolls + {topUpShardCost} Shards to perform 10x Roll?</i>"
            );
        }

        gachaHybridConfirmModalObj.SetActive(true);
        gachaHybridConfirmModalObj.transform.SetAsLastSibling();
    }

    private void BuildGemSocketModal(Transform parent)
    {
        gemSocketModalObj = new GameObject("GemSocketModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gemSocketModalObj.layer = 5;
        gemSocketModalObj.transform.SetParent(parent, false);

        RectTransform rt = gemSocketModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = gemSocketModalObj.GetComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.05f, 0.96f);

        // Center Box 100% Full Screen Expansive
        GameObject boxObj = new GameObject("SocketBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(gemSocketModalObj.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.anchorMin = Vector2.zero;
        boxRt.anchorMax = Vector2.one;
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.offsetMin = Vector2.zero;
        boxRt.offsetMax = Vector2.zero;
        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.04f, 0.05f, 0.09f, 1f);

        var boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
        boxOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Header Title
        GameObject tObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.layer = 5;
        tObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -25f);
        tRt.sizeDelta = new Vector2(0f, 38f);
        var tTmp = tObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = LocalizationManager.T("LÒ KHẢM KHOÁNG THẠCH & ĐÁ QUÝ", "RUIN GEM & MINERAL FORGE");
        tTmp.fontSize = 26;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = new Color(1f, 0.85f, 0.35f);
        tTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject sObj = new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasRenderer));
        sObj.layer = 5;
        sObj.transform.SetParent(boxObj.transform, false);
        RectTransform sRt = sObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -62f);
        sRt.sizeDelta = new Vector2(0f, 22f);
        var sTmp = sObj.AddComponent<TextMeshProUGUI>();
        sTmp.text = LocalizationManager.T("Cầm và kéo thả trực tiếp khoáng thạch vào lỗ khảm hoặc bấm nút Khảm bên dưới.",
            "Touch & drag gemstones directly into socket slots, or click the Socket button below.");
        sTmp.fontSize = 14f;
        sTmp.color = new Color(0.4f, 0.85f, 1f);
        sTmp.alignment = TextAlignmentOptions.Center;

        // Close Button
        GameObject cBtnObj = new GameObject("Btn_CloseSocketModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cBtnObj.layer = 5;
        cBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform cbRt = cBtnObj.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1f, 1f);
        cbRt.anchorMax = new Vector2(1f, 1f);
        cbRt.pivot = new Vector2(1f, 1f);
        cbRt.anchoredPosition = new Vector2(-30f, -30f);
        cbRt.sizeDelta = new Vector2(52f, 52f);
        Image cbImg = cBtnObj.GetComponent<Image>();
        cbImg.color = new Color(0.75f, 0.22f, 0.25f, 1f);
        var cbOutline = cBtnObj.AddComponent<Outline>();
        cbOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.7f);
        Button cbBtn = cBtnObj.GetComponent<Button>();
        cbBtn.targetGraphic = cbImg;
        cbBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            if (gemSocketModalObj != null) gemSocketModalObj.SetActive(false);
            if (isStandaloneSkillMode)
            {
                Close();
            }
            else
            {
                if (dialogBox != null) dialogBox.SetActive(true);
                if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
                RefreshGrid();
            }
        });
        GameObject cTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        cTxtObj.layer = 5;
        cTxtObj.transform.SetParent(cBtnObj.transform, false);
        RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.sizeDelta = Vector2.zero;
        var ctTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        ctTmp.text = "X";
        ctTmp.fontSize = 24;
        ctTmp.fontStyle = FontStyles.Bold;
        ctTmp.color = Color.white;
        ctTmp.alignment = TextAlignmentOptions.Center;

        // TOP SECTION: 3-Skill Cards Container
        GameObject topSection = new GameObject("TopSection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topSection.layer = 5;
        topSection.transform.SetParent(boxObj.transform, false);
        RectTransform tsRt = topSection.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 0.52f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.pivot = new Vector2(0.5f, 0.5f);
        tsRt.offsetMin = new Vector2(40f, 10f);
        tsRt.offsetMax = new Vector2(-40f, -100f);
        Image tsImg = topSection.GetComponent<Image>();
        tsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);
        var tsOutline = topSection.AddComponent<Outline>();
        tsOutline.effectColor = new Color(0.25f, 0.35f, 0.5f, 0.6f);

        // Total Socket Buff Summary Text Banner
        GameObject wBuffObj = new GameObject("TotalBuffBanner", typeof(RectTransform), typeof(CanvasRenderer));
        wBuffObj.layer = 5;
        wBuffObj.transform.SetParent(topSection.transform, false);
        RectTransform wbRt = wBuffObj.GetComponent<RectTransform>();
        wbRt.anchorMin = new Vector2(0f, 1f);
        wbRt.anchorMax = new Vector2(1f, 1f);
        wbRt.pivot = new Vector2(0f, 1f);
        wbRt.anchoredPosition = new Vector2(15f, -8f);
        wbRt.sizeDelta = new Vector2(-280f, 28f);
        socketModalStatsBonusText = wBuffObj.AddComponent<TextMeshProUGUI>();
        socketModalStatsBonusText.fontSize = 13.5f;
        socketModalStatsBonusText.fontStyle = FontStyles.Bold;
        socketModalStatsBonusText.color = new Color(0.2f, 1f, 0.45f);
        socketModalStatsBonusText.alignment = TextAlignmentOptions.Left;
        socketModalStatsBonusText.enableWordWrapping = false;
        socketModalStatsBonusText.overflowMode = TextOverflowModes.Ellipsis;

        // Auto Merge All Button (Top-Right)
        GameObject autoMergeObj = new GameObject("Btn_AutoMergeAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        autoMergeObj.layer = 5;
        autoMergeObj.transform.SetParent(topSection.transform, false);
        RectTransform amRt = autoMergeObj.GetComponent<RectTransform>();
        amRt.anchorMin = new Vector2(1f, 1f);
        amRt.anchorMax = new Vector2(1f, 1f);
        amRt.pivot = new Vector2(1f, 1f);
        amRt.anchoredPosition = new Vector2(-15f, -6f);
        amRt.sizeDelta = new Vector2(250f, 32f);

        Image amImg = autoMergeObj.GetComponent<Image>();
        amImg.color = new Color(0.92f, 0.55f, 0.08f, 1f);
        var amOutline = autoMergeObj.AddComponent<Outline>();
        amOutline.effectColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        amOutline.effectDistance = new Vector2(1.5f, -1.5f);

        autoMergeAllBtn = autoMergeObj.GetComponent<Button>();
        autoMergeAllBtn.targetGraphic = amImg;
        autoMergeAllBtn.onClick.AddListener(OnAutoMergeAllButtonClicked);

        GameObject amTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        amTxtObj.layer = 5;
        amTxtObj.transform.SetParent(autoMergeObj.transform, false);
        RectTransform amtRt = amTxtObj.GetComponent<RectTransform>();
        amtRt.anchorMin = Vector2.zero;
        amtRt.anchorMax = Vector2.one;
        amtRt.sizeDelta = Vector2.zero;

        autoMergeAllBtnText = amTxtObj.AddComponent<TextMeshProUGUI>();
        autoMergeAllBtnText.fontSize = 13f;
        autoMergeAllBtnText.fontStyle = FontStyles.Bold;
        autoMergeAllBtnText.color = Color.white;
        autoMergeAllBtnText.alignment = TextAlignmentOptions.Center;
        autoMergeAllBtnText.enableWordWrapping = false;

        // Sockets Container (Horizontal row of 3 Skill Cards spanning across the top section)
        GameObject socketsContObj = new GameObject("SocketsRow", typeof(RectTransform));
        socketsContObj.layer = 5;
        socketsContObj.transform.SetParent(topSection.transform, false);
        RectTransform scRt = socketsContObj.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0f, 0f);
        scRt.anchorMax = new Vector2(1f, 1f);
        scRt.pivot = new Vector2(0.5f, 0.5f);
        scRt.offsetMin = new Vector2(10f, 8f);
        scRt.offsetMax = new Vector2(-10f, -40f);
        socketSlotsContainer = socketsContObj.transform;

        // BOTTOM SECTION: Gem Inventory Scroll Area
        GameObject botSection = new GameObject("BotSection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        botSection.layer = 5;
        botSection.transform.SetParent(boxObj.transform, false);
        RectTransform bsRt = botSection.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0f, 0f);
        bsRt.anchorMax = new Vector2(1f, 0.52f);
        bsRt.pivot = new Vector2(0.5f, 0.5f);
        bsRt.offsetMin = new Vector2(40f, 30f);
        bsRt.offsetMax = new Vector2(-40f, -10f);
        Image bsImg = botSection.GetComponent<Image>();
        bsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);
        var bsOutline = botSection.AddComponent<Outline>();
        bsOutline.effectColor = new Color(0.25f, 0.35f, 0.5f, 0.6f);

        // Available Gems Header
        GameObject avHeaderObj = new GameObject("GemsHeader", typeof(RectTransform), typeof(CanvasRenderer));
        avHeaderObj.layer = 5;
        avHeaderObj.transform.SetParent(botSection.transform, false);
        RectTransform avhRt = avHeaderObj.GetComponent<RectTransform>();
        avhRt.anchorMin = new Vector2(0f, 1f);
        avhRt.anchorMax = new Vector2(1f, 1f);
        avhRt.pivot = new Vector2(0.5f, 1f);
        avhRt.anchoredPosition = new Vector2(0f, -8f);
        avhRt.sizeDelta = new Vector2(-20f, 26f);
        var avhTmp = avHeaderObj.AddComponent<TextMeshProUGUI>();
        avhTmp.text = LocalizationManager.T(
            "KHOÁNG THẠCH & ĐÁ QUÝ (CHẠM/KÉO VÀO ĐỂ KHẢM • KÉO RA NGOÀI ĐỂ THÁO)",
            "MINERALS & GEMS (TAP/DRAG INTO SLOT TO SOCKET • DRAG OUT TO UNSOCKET)");
        avhTmp.fontSize = 14f;
        avhTmp.fontStyle = FontStyles.Bold;
        avhTmp.color = new Color(1f, 0.85f, 0.35f);
        avhTmp.alignment = TextAlignmentOptions.Center;

        // Scroll View
        GameObject gemScrollObj = new GameObject("GemsScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect));
        gemScrollObj.layer = 5;
        gemScrollObj.transform.SetParent(botSection.transform, false);
        RectTransform gscRt = gemScrollObj.GetComponent<RectTransform>();
        gscRt.anchorMin = new Vector2(0f, 0f);
        gscRt.anchorMax = new Vector2(1f, 1f);
        gscRt.pivot = new Vector2(0.5f, 0.5f);
        gscRt.offsetMin = new Vector2(15f, 15f);
        gscRt.offsetMax = new Vector2(-15f, -40f);
        var gemScroll = gemScrollObj.GetComponent<ScrollRect>();
        gemScroll.horizontal = false;
        gemScroll.vertical = true;
        gemScroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject gVpObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        gVpObj.layer = 5;
        gVpObj.transform.SetParent(gemScrollObj.transform, false);
        RectTransform gvpRt = gVpObj.GetComponent<RectTransform>();
        gvpRt.anchorMin = Vector2.zero;
        gvpRt.anchorMax = Vector2.one;
        gvpRt.sizeDelta = Vector2.zero;
        Image gvpImg = gVpObj.GetComponent<Image>();
        gvpImg.color = Color.white;
        var gmask = gVpObj.GetComponent<Mask>();
        gmask.showMaskGraphic = false;

        GameObject gContentObj = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        gContentObj.layer = 5;
        gContentObj.transform.SetParent(gVpObj.transform, false);
        RectTransform gcRt = gContentObj.GetComponent<RectTransform>();
        gcRt.anchorMin = new Vector2(0f, 1f);
        gcRt.anchorMax = new Vector2(1f, 1f);
        gcRt.pivot = new Vector2(0.5f, 1f);
        gcRt.anchoredPosition = Vector2.zero;
        gcRt.sizeDelta = Vector2.zero;

        var gGrid = gContentObj.GetComponent<GridLayoutGroup>();
        gGrid.cellSize = new Vector2(275f, 105f);
        gGrid.spacing = new Vector2(14f, 12f);
        gGrid.padding = new RectOffset(12, 12, 12, 12);
        gGrid.childAlignment = TextAnchor.UpperLeft;
        gGrid.constraint = GridLayoutGroup.Constraint.Flexible;

        var gFitter = gContentObj.GetComponent<ContentSizeFitter>();
        gFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        gemScroll.content = gcRt;
        gemScroll.viewport = gvpRt;
        availableGemsContainer = gContentObj.transform;

        gemSocketModalObj.SetActive(false);
    }

    public void OpenGemSocketModal(string equipId = "")
    {
        EnsureItemSprites();
        if (dialogBox == null || gemSocketModalObj == null)
        {
            BuildUI();
        }

        // Hide main inventory dialogBox so no unwanted background UI is visible
        if (dialogBox != null) dialogBox.SetActive(false);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        isOpen = true;
        isStandaloneSkillMode = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();

        if (string.IsNullOrEmpty(equipId))
        {
            equipId = PlayerPrefs.GetString("Equipped_Weapon_Id", "");
        }
        if (string.IsNullOrEmpty(equipId))
        {
            var items = AccountDatabase.GetActiveUserInventory();
            foreach (var it in items)
            {
                var d = InventoryItemDatabase.GetItem(it.itemId);
                if (d != null && d.category == InventoryItemDatabase.ItemCategory.Equipment)
                {
                    equipId = d.id;
                    break;
                }
            }
        }
        if (string.IsNullOrEmpty(equipId))
        {
            equipId = "wpn_legendary_sword";
        }

        targetEquipIdForSocketing = equipId;
        selectedSocketSlotIndex = 0;
        if (gemSocketModalObj != null)
        {
            gemSocketModalObj.SetActive(true);
            gemSocketModalObj.transform.SetAsLastSibling();
            RefreshGemSocketModal();
        }
    }

    private void RefreshGemSocketModal()
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // Set Header Title
        if (socketModalWeaponTitle != null)
        {
            socketModalWeaponTitle.text = isVi
                ? "LÒ KHẢM 3 CHIÊU THỨC (KIẾM, KHIÊN & ĐÁ QUÝ)"
                : "3-SKILL SOCKET FORGE (SWORDS, SHIELDS & GEMS)";
        }

        // Calculate and Show Total Active 3-Skill Socket Bonuses
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out int sAtk, out int sHp, out int sDef, out float sLifesteal, out float sCd, out float sShard, out float sRegen);
        List<string> bonuses = new List<string>();
        if (sAtk > 0) bonuses.Add($"<color=#FFCC00>+{sAtk} ATK</color>");
        if (sHp > 0) bonuses.Add($"<color=#22C55E>+{sHp} HP</color>");
        if (sDef > 0) bonuses.Add($"<color=#38BDF8>+{sDef} DEF</color>");
        if (sLifesteal > 0) bonuses.Add($"<color=#F43F5E>+{Mathf.RoundToInt(sLifesteal * 100)}% Hút Máu</color>");
        if (sCd > 0) bonuses.Add($"<color=#C084FC>-{Mathf.RoundToInt(sCd * 100)}% CD</color>");
        if (sShard > 0) bonuses.Add($"<color=#FBBF24>+{Mathf.RoundToInt(sShard * 100)}% Shard</color>");
        if (sRegen > 0) bonuses.Add($"<color=#10B981>+{sRegen} HP/s</color>");

        if (socketModalStatsBonusText != null)
        {
            socketModalStatsBonusText.text = (bonuses.Count > 0)
                ? $"{LocalizationManager.T("Tổng Buff 3 Chiêu:", "Total Active Buffs:")} {string.Join(" | ", bonuses)}"
                : LocalizationManager.T("<color=#888888>Chưa khảm vật phẩm nào vào 3 chiêu thức. Hãy chọn ô và nhấn khảm kiếm, khiên hoặc ngọc bên dưới!</color>",
                    "<color=#888888>No items socketed into skills. Select a slot and choose a weapon, shield or gem below!</color>");
        }

        // Update Auto Merge All Button State
        int mergeableCount = EquipmentSocketManager.GetTotalAvailableMergeGemsCount();
        if (autoMergeAllBtnText != null)
        {
            if (mergeableCount > 0)
            {
                string compactMerge = FormatCompactNumber(mergeableCount);
                autoMergeAllBtnText.text = LocalizationManager.T($"HẤP THU TOÀN BỘ (+{compactMerge})", $"AUTO MERGE ALL (+{compactMerge})");
                if (autoMergeAllBtn != null)
                {
                    autoMergeAllBtn.interactable = true;
                    var img = autoMergeAllBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.92f, 0.55f, 0.08f, 1f);
                }
            }
            else
            {
                autoMergeAllBtnText.text = LocalizationManager.T("HẤP THU TOÀN BỘ", "AUTO MERGE ALL");
                if (autoMergeAllBtn != null)
                {
                    autoMergeAllBtn.interactable = false;
                    var img = autoMergeAllBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
                }
            }
        }

        // Render 3 Skill Cards with 3 Sub-Slots Each
        foreach (var obj in socketSlotObjs)
        {
            if (obj != null) Destroy(obj);
        }
        socketSlotObjs.Clear();

        int skillCount = EquipmentSocketManager.SKILL_COUNT;
        int subCount = EquipmentSocketManager.SUB_SOCKETS_PER_SKILL;

        float cardW = 480f;
        float cardH = 245f;
        float spacing = 18f;
        float startX = -((skillCount - 1) * (cardW + spacing)) / 2f;

        var charMgr = CharacterManager.EnsureInstance();
        string selectedCharId = charMgr != null ? charMgr.GetSelectedCharacterId().ToLower() : "vaelstrider";
        bool isAzure = (selectedCharId.Contains("azure") || selectedCharId.Contains("player3"));
        bool isRonin = (selectedCharId.Contains("ronin") || selectedCharId.Contains("gale") || selectedCharId.Contains("maid"));

        string[] skillNamesVi;
        string[] skillNamesEn;

        if (isRonin)
        {
            skillNamesVi = new string[] { "[1] PHONG NGUYỆT TRẢM", "[2] PHONG THIỂM TỐC LƯỚT", "[3] ÁM ẢNH PHÂN THÂN TRẢM" };
            skillNamesEn = new string[] { "[1] GALE CRESCENT SWEEP", "[2] GALE FLASH DASH", "[3] SHADOW GALE STRIKE" };
        }
        else if (isAzure)
        {
            skillNamesVi = new string[] { "[1] SONG BĂNG ĐOẢN KIẾM", "[2] HÀN BĂNG TRƯỢT TUYẾT", "[3] 5 TIA BĂNG NGUYỆT SÓNG" };
            skillNamesEn = new string[] { "[1] TWIN FROST BLADES", "[2] FROST GLIDE DASH", "[3] 5-WAY AZURE TEMPEST" };
        }
        else
        {
            skillNamesVi = new string[] { "[1] ĐẠI KIẾM HOÀNG KIM", "[2] BỤI CỔ TỐC BIẾN", "[3] BÃO XOÁY CỔ KIẾM" };
            skillNamesEn = new string[] { "[1] ANCIENT BROADSWORD", "[2] RUIN DUST DASH", "[3] RUIN WHIRLWIND" };
        }

        for (int i = 0; i < skillCount; i++)
        {
            int skillIdx = i;
            bool isSelected = (skillIdx == selectedSocketSlotIndex);

            GameObject cardObj = new GameObject($"SkillCard_{skillIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardObj.layer = 5;
            cardObj.transform.SetParent(socketSlotsContainer, false);
            socketSlotObjs.Add(cardObj);

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(startX + i * (cardW + spacing), 0f);
            cRt.sizeDelta = new Vector2(cardW, cardH);

            Image cImg = cardObj.GetComponent<Image>();
            cImg.color = isSelected ? new Color(0.10f, 0.18f, 0.32f, 0.98f) : new Color(0.05f, 0.07f, 0.12f, 0.94f);

            var cOutline = cardObj.AddComponent<Outline>();
            cOutline.effectColor = isSelected ? new Color(0f, 0.9f, 1f, 1f) : new Color(0.3f, 0.4f, 0.55f, 0.6f);
            cOutline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1.2f, -1.2f);

            Button cBtn = cardObj.GetComponent<Button>();
            cBtn.targetGraphic = cImg;
            cBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                selectedSocketSlotIndex = skillIdx;
                RefreshGemSocketModal();
            });

            // Card Drop Target
            var cardDrop = cardObj.AddComponent<UIDropSocketTarget>();
            cardDrop.socketSlotIndex = skillIdx;
            cardDrop.onGemDropped = (droppedGemId, targetSlot) =>
            {
                if (EquipmentSocketManager.SocketGemToSkill(targetSlot, -1, droppedGemId, out _, out _))
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
                    RefreshGemSocketModal();
                    if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
                    RefreshGrid();
                }
            };

            // 1. Skill Header Container (Shape of Dreams Style: Skill Icon Badge + Title + Subtitle)
            GameObject headerObj = new GameObject("SkillHeader", typeof(RectTransform), typeof(CanvasRenderer));
            headerObj.layer = 5;
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -8f);
            hRt.sizeDelta = new Vector2(-16f, 38f);

            // Skill Accent Colors & Sprites for each hero
            Sprite skillSpr = LoadHeroSkillSprite(selectedCharId, skillIdx);
            Color accentCol;
            string accentHex;

            if (isRonin)
            {
                accentCol = (skillIdx == 0) ? new Color(0.28f, 1f, 0.75f, 1f)
                          : (skillIdx == 1) ? new Color(0.18f, 0.85f, 0.45f, 1f)
                          : new Color(0.44f, 0.63f, 1f, 1f);
                accentHex = (skillIdx == 0) ? "47FFBF" : (skillIdx == 1) ? "2ED573" : "70A1FF";
            }
            else if (isAzure)
            {
                accentCol = (skillIdx == 0) ? new Color(0.35f, 0.88f, 1f, 1f)
                          : (skillIdx == 1) ? new Color(0.44f, 0.63f, 1f, 1f)
                          : new Color(0.22f, 0.74f, 0.97f, 1f);
                accentHex = (skillIdx == 0) ? "59E0FF" : (skillIdx == 1) ? "70A1FF" : "38BDF8";
            }
            else
            {
                accentCol = (skillIdx == 0) ? new Color(1f, 0.85f, 0.35f, 1f)
                          : (skillIdx == 1) ? new Color(0.2f, 0.95f, 0.65f, 1f)
                          : new Color(0.75f, 0.5f, 1f, 1f);
                accentHex = (skillIdx == 0) ? "FFDF66" : (skillIdx == 1) ? "2ED573" : "A29BFE";
            }

            // Framed Skill Icon Badge
            GameObject iconBadgeObj = new GameObject("SkillIconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconBadgeObj.layer = 5;
            iconBadgeObj.transform.SetParent(headerObj.transform, false);
            RectTransform ibRt = iconBadgeObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(6f, 0f);
            ibRt.sizeDelta = new Vector2(34f, 34f);

            Image ibImg = iconBadgeObj.GetComponent<Image>();
            ibImg.color = new Color(0.04f, 0.06f, 0.11f, 0.95f);
            var ibOutline = iconBadgeObj.AddComponent<Outline>();
            ibOutline.effectColor = isSelected ? new Color(0f, 0.9f, 1f, 1f) : accentCol;
            ibOutline.effectDistance = new Vector2(1.5f, -1.5f);

            if (skillSpr != null)
            {
                GameObject iconInner = new GameObject("SkillIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconInner.layer = 5;
                iconInner.transform.SetParent(iconBadgeObj.transform, false);
                RectTransform iiRt = iconInner.GetComponent<RectTransform>();
                iiRt.anchorMin = new Vector2(0.5f, 0.5f);
                iiRt.anchorMax = new Vector2(0.5f, 0.5f);
                iiRt.pivot = new Vector2(0.5f, 0.5f);
                iiRt.anchoredPosition = Vector2.zero;
                iiRt.sizeDelta = new Vector2(26f, 26f);

                Image iiImg = iconInner.GetComponent<Image>();
                iiImg.sprite = skillSpr;
                iiImg.color = Color.white;
                iiImg.preserveAspect = true;
                iiImg.raycastTarget = false;
            }

            // Title & Subtitle Group
            GameObject textGroupObj = new GameObject("SkillTextGroup", typeof(RectTransform), typeof(CanvasRenderer));
            textGroupObj.layer = 5;
            textGroupObj.transform.SetParent(headerObj.transform, false);
            RectTransform tgRt = textGroupObj.GetComponent<RectTransform>();
            tgRt.anchorMin = new Vector2(0f, 0f);
            tgRt.anchorMax = new Vector2(1f, 1f);
            tgRt.offsetMin = new Vector2(46f, 0f);
            tgRt.offsetMax = new Vector2(-4f, 0f);

            // Title Text
            GameObject titleObj = new GameObject("SkillTitle", typeof(RectTransform), typeof(CanvasRenderer));
            titleObj.layer = 5;
            titleObj.transform.SetParent(textGroupObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.45f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            string sName = isVi ? skillNamesVi[skillIdx] : skillNamesEn[skillIdx];
            titleTmp.text = $"<b><color=#{accentHex}>{sName}</color></b>";
            titleTmp.fontSize = 14f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Left;

            // Subtitle Text
            GameObject subObj = new GameObject("SkillSub", typeof(RectTransform), typeof(CanvasRenderer));
            subObj.layer = 5;
            subObj.transform.SetParent(textGroupObj.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.45f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;

            var subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = isVi ? "3 Ô Khảm • Ghép 2 món cùng loại để tăng cấp" : "3 Sockets • Merge duplicates to level up";
            subTmp.fontSize = 10.5f;
            subTmp.color = new Color(0.65f, 0.82f, 0.95f, 0.85f);
            subTmp.alignment = TextAlignmentOptions.Left;

            // 3. Middle: 3 Sub-Sockets Row
            GameObject subRowObj = new GameObject("SubSocketsRow", typeof(RectTransform), typeof(CanvasRenderer));
            subRowObj.layer = 5;
            subRowObj.transform.SetParent(cardObj.transform, false);
            RectTransform srRt = subRowObj.GetComponent<RectTransform>();
            srRt.anchorMin = new Vector2(0.5f, 0.5f);
            srRt.anchorMax = new Vector2(0.5f, 0.5f);
            srRt.pivot = new Vector2(0.5f, 0.5f);
            srRt.anchoredPosition = new Vector2(0f, 4f);
            srRt.sizeDelta = new Vector2(cardW - 20f, 115f);

            float subBoxW = 142f;
            float subBoxH = 110f;
            float subSpacing = 12f;
            float subStartX = -((subCount - 1) * (subBoxW + subSpacing)) / 2f;

            for (int s = 0; s < subCount; s++)
            {
                int subIdx = s;
                bool hasItem = EquipmentSocketManager.GetSkillSubSocketGem(skillIdx, subIdx, out string itemId, out int itemLevel);

                GameObject subBoxObj = new GameObject($"SubSocket_{subIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                subBoxObj.layer = 5;
                subBoxObj.transform.SetParent(subRowObj.transform, false);

                RectTransform sbRt = subBoxObj.GetComponent<RectTransform>();
                sbRt.anchoredPosition = new Vector2(subStartX + s * (subBoxW + subSpacing), 0f);
                sbRt.sizeDelta = new Vector2(subBoxW, subBoxH);

                Image sbImg = subBoxObj.GetComponent<Image>();
                if (sprItemSlotFrame != null) { sbImg.sprite = sprItemSlotFrame; sbImg.type = Image.Type.Sliced; }

                var sbOutline = subBoxObj.AddComponent<Outline>();
                Button sbBtn = subBoxObj.GetComponent<Button>();
                sbBtn.targetGraphic = sbImg;

                // Unconditional drop target for this specific sub-slot
                var subDrop = subBoxObj.AddComponent<UIDropSocketTarget>();
                subDrop.socketSlotIndex = skillIdx;
                subDrop.subSlotIndex = subIdx;
                subDrop.onGemDroppedDetailed = (droppedGemId, targetSlot, targetSub) =>
                {
                    if (EquipmentSocketManager.SocketGemToSkill(targetSlot, targetSub, droppedGemId, out _, out _))
                    {
                        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
                        RefreshGemSocketModal();
                        if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
                        RefreshGrid();
                    }
                };

                if (!hasItem)
                {
                    sbImg.color = new Color(0.18f, 0.22f, 0.32f, 0.65f);
                    sbOutline.effectColor = new Color(0.35f, 0.45f, 0.6f, 0.5f);
                    sbOutline.effectDistance = new Vector2(1f, -1f);

                    GameObject plusObj = new GameObject("PlusText", typeof(RectTransform), typeof(CanvasRenderer));
                    plusObj.layer = 5;
                    plusObj.transform.SetParent(subBoxObj.transform, false);
                    RectTransform pRt = plusObj.GetComponent<RectTransform>();
                    pRt.anchorMin = Vector2.zero;
                    pRt.anchorMax = Vector2.one;
                    pRt.sizeDelta = Vector2.zero;
                    var pTmp = plusObj.AddComponent<TextMeshProUGUI>();
                    pTmp.text = $"<color=#557799><size=20>+</size>\n<size=11>Ô {subIdx + 1}</size></color>";
                    pTmp.alignment = TextAlignmentOptions.Center;
                    pTmp.raycastTarget = false;
                }
                else
                {
                    var itemDef = InventoryItemDatabase.GetItem(itemId);
                    var itemEff = EquipmentSocketManager.GetGemEffect(itemId);
                    Color itemCol = itemEff.HasValue ? itemEff.Value.gemColor : Color.white;

                    sbImg.color = new Color(itemCol.r * 0.3f, itemCol.g * 0.3f, itemCol.b * 0.3f, 0.95f);
                    sbOutline.effectColor = itemCol;
                    sbOutline.effectDistance = new Vector2(1.5f, -1.5f);

                    // Item Icon
                    GameObject gIcon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    gIcon.layer = 5;
                    gIcon.transform.SetParent(subBoxObj.transform, false);
                    RectTransform gicRt = gIcon.GetComponent<RectTransform>();
                    gicRt.anchorMin = new Vector2(0.5f, 0.5f);
                    gicRt.anchorMax = new Vector2(0.5f, 0.5f);
                    gicRt.pivot = new Vector2(0.5f, 0.5f);
                    gicRt.anchoredPosition = new Vector2(0f, 4f);
                    gicRt.sizeDelta = new Vector2(52f, 52f);
                    Image gicImg = gIcon.GetComponent<Image>();
                    gicImg.sprite = itemDef != null ? itemDef.Icon : null;
                    gicImg.preserveAspect = true;
                    gicImg.raycastTarget = false;

                    // Mini Level Tag at bottom
                    GameObject lvObj = new GameObject("LvTag", typeof(RectTransform), typeof(CanvasRenderer));
                    lvObj.layer = 5;
                    lvObj.transform.SetParent(subBoxObj.transform, false);
                    RectTransform lvRt = lvObj.GetComponent<RectTransform>();
                    lvRt.anchorMin = new Vector2(0f, 0f);
                    lvRt.anchorMax = new Vector2(1f, 0f);
                    lvRt.pivot = new Vector2(0.5f, 0f);
                    lvRt.anchoredPosition = new Vector2(0f, 2f);
                    lvRt.sizeDelta = new Vector2(-6f, 18f);
                    var lvTmp = lvObj.AddComponent<TextMeshProUGUI>();
                    string lvColor = (itemLevel == 1) ? "#FFFFFF"
                                   : (itemLevel == 2) ? "#00FFFF"
                                   : (itemLevel == 3) ? "#FFCC00"
                                   : (itemLevel == 4) ? "#FF00FF"
                                   : "#FF3366";
                    lvTmp.text = $"<size=11><color={lvColor}><b>Lv.{itemLevel}</b></color></size>";
                    lvTmp.alignment = TextAlignmentOptions.Center;
                    lvTmp.raycastTarget = false;

                    // Unsocket [x] button
                    GameObject unBtnObj = new GameObject("Btn_X", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    unBtnObj.layer = 5;
                    unBtnObj.transform.SetParent(subBoxObj.transform, false);
                    RectTransform unRt = unBtnObj.GetComponent<RectTransform>();
                    unRt.anchorMin = new Vector2(1f, 1f);
                    unRt.anchorMax = new Vector2(1f, 1f);
                    unRt.pivot = new Vector2(1f, 1f);
                    unRt.anchoredPosition = new Vector2(2f, 2f);
                    unRt.sizeDelta = new Vector2(22f, 22f);
                    Image unImg = unBtnObj.GetComponent<Image>();
                    unImg.color = new Color(0.8f, 0.2f, 0.25f, 0.95f);
                    Button unBtn = unBtnObj.GetComponent<Button>();
                    unBtn.targetGraphic = unImg;
                    unBtn.onClick.AddListener(() =>
                    {
                        EquipmentSocketManager.UnsocketSubGem(skillIdx, subIdx);
                        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
                        RefreshGemSocketModal();
                        if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
                        RefreshGrid();
                    });

                    GameObject unTxt = new GameObject("X", typeof(RectTransform), typeof(CanvasRenderer));
                    unTxt.layer = 5;
                    unTxt.transform.SetParent(unBtnObj.transform, false);
                    RectTransform utRt = unTxt.GetComponent<RectTransform>();
                    utRt.anchorMin = Vector2.zero;
                    utRt.anchorMax = Vector2.one;
                    utRt.sizeDelta = Vector2.zero;
                    var utTmp = unTxt.AddComponent<TextMeshProUGUI>();
                    utTmp.text = "x";
                    utTmp.fontSize = 14f;
                    utTmp.fontStyle = FontStyles.Bold;
                    utTmp.alignment = TextAlignmentOptions.Center;
                    utTmp.color = Color.white;
                    utTmp.raycastTarget = false;

                    // Drag Support
                    var sDrag = subBoxObj.AddComponent<UIDragDropItem>();
                    sDrag.itemId = itemId;
                    sDrag.itemIcon = itemDef != null ? itemDef.Icon : null;
                    sDrag.itemColor = itemCol;
                    sDrag.isSocketedGem = true;
                    sDrag.sourceSocketIndex = skillIdx;
                    sDrag.sourceSubSocketIndex = subIdx;
                    sDrag.onDragOutOfSocket = (unslotIdx) =>
                    {
                        EquipmentSocketManager.UnsocketSubGem(unslotIdx, subIdx);
                        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
                        RefreshGemSocketModal();
                        if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
                        RefreshGrid();
                    };
                    sDrag.onMoveSubSocket = (fromSkill, fromSub, toSkill, toSub) =>
                    {
                        EquipmentSocketManager.MoveOrSwapSubGem(fromSkill, fromSub, toSkill, toSub);
                        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
                        RefreshGemSocketModal();
                        if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
                        RefreshGrid();
                    };
                }
            }

            // 4. Bottom Skill Buff Summary
            EquipmentSocketManager.GetSkillSocketBonus(skillIdx, out int bAtk, out int bHp, out int bDef, out float bLife, out float bCd, out float bShard, out float bRegen);
            List<string> skillBuffs = new List<string>();
            if (bAtk > 0) skillBuffs.Add($"<color=#FFCC00>+{bAtk} ATK</color>");
            if (bHp > 0) skillBuffs.Add($"<color=#22C55E>+{bHp} HP</color>");
            if (bDef > 0) skillBuffs.Add($"<color=#38BDF8>+{bDef} DEF</color>");
            if (bLife > 0) skillBuffs.Add($"<color=#F43F5E>+{Mathf.RoundToInt(bLife * 100)}% Hút Máu</color>");
            if (bCd > 0) skillBuffs.Add($"<color=#C084FC>-{Mathf.RoundToInt(bCd * 100)}% CD</color>");
            if (bShard > 0) skillBuffs.Add($"<color=#FBBF24>+{Mathf.RoundToInt(bShard * 100)}% Shard</color>");
            if (bRegen > 0) skillBuffs.Add($"<color=#10B981>+{bRegen} HP/s</color>");

            GameObject buffSummaryObj = new GameObject("SkillBuffSummary", typeof(RectTransform), typeof(CanvasRenderer));
            buffSummaryObj.layer = 5;
            buffSummaryObj.transform.SetParent(cardObj.transform, false);
            RectTransform bsRt = buffSummaryObj.GetComponent<RectTransform>();
            bsRt.anchorMin = new Vector2(0f, 0f);
            bsRt.anchorMax = new Vector2(1f, 0f);
            bsRt.pivot = new Vector2(0.5f, 0f);
            bsRt.anchoredPosition = new Vector2(0f, 6f);
            bsRt.sizeDelta = new Vector2(-16f, 24f);

            var bsTmp = buffSummaryObj.AddComponent<TextMeshProUGUI>();
            bsTmp.text = (skillBuffs.Count > 0)
                ? $"<b>Buff:</b> {string.Join("  ", skillBuffs)}"
                : (isVi ? "<color=#777777>[Trống]</color>" : "<color=#777777>[Empty]</color>");
            bsTmp.fontSize = 11f;
            bsTmp.alignment = TextAlignmentOptions.Center;
            bsTmp.enableWordWrapping = true;
        }

        // Render Available Items (Weapons, Shields, Ores, Gems) in Player Bag
        foreach (var obj in gemCardObjs)
        {
            if (obj != null) Destroy(obj);
        }
        gemCardObjs.Clear();

        var invItems = AccountDatabase.GetActiveUserInventory();
        List<AccountDatabase.InventoryItemEntry> ownedSocketable = new List<AccountDatabase.InventoryItemEntry>();
        foreach (var it in invItems)
        {
            if (it.quantity > 0 && EquipmentSocketManager.IsGem(it.itemId))
            {
                ownedSocketable.Add(it);
            }
        }

        if (ownedSocketable.Count == 0)
        {
            GameObject noGemsObj = new GameObject("NoGemsNotice", typeof(RectTransform), typeof(CanvasRenderer));
            noGemsObj.layer = 5;
            noGemsObj.transform.SetParent(availableGemsContainer, false);
            gemCardObjs.Add(noGemsObj);
            RectTransform ngRt = noGemsObj.GetComponent<RectTransform>();
            ngRt.sizeDelta = new Vector2(900f, 60f);
            var ngTmp = noGemsObj.AddComponent<TextMeshProUGUI>();
            ngTmp.text = LocalizationManager.T(
                "Trong túi đồ hiện không có kiếm, khiên, khoáng thạch hoặc đá quý!\nHãy đi phó bản diệt quái hoặc mở Gacha để nhận thêm trang bị.",
                "No swords, shields, minerals or gems in inventory!\nDefeat dungeon monsters or pull Gacha to obtain items.");
            ngTmp.fontSize = 13f;
            ngTmp.color = new Color(0.7f, 0.75f, 0.85f);
            ngTmp.alignment = TextAlignmentOptions.Center;
            return;
        }

        for (int i = 0; i < ownedSocketable.Count; i++)
        {
            var itemEntry = ownedSocketable[i];
            var itemDef = InventoryItemDatabase.GetItem(itemEntry.itemId);
            var itemEff = EquipmentSocketManager.GetGemEffect(itemEntry.itemId);
            if (itemDef == null || !itemEff.HasValue) continue;

            GameObject cardObj = new GameObject($"GemCard_{itemEntry.itemId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.layer = 5;
            cardObj.transform.SetParent(availableGemsContainer, false);
            gemCardObjs.Add(cardObj);

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(225f, 105f);

            Image cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);

            var cOutline = cardObj.AddComponent<Outline>();
            cOutline.effectColor = itemEff.Value.gemColor * 0.8f;
            // Mobile 2s Long-Press to Reveal Salvage [X] Badge
            var lp = cardObj.AddComponent<UILongPressTrigger>();
            lp.targetItemId = itemEntry.itemId;
            lp.holdDuration = 2.0f;

            // Drag & Drop Support
            var dragItem = cardObj.AddComponent<UIDragDropItem>();
            dragItem.itemId = itemEntry.itemId;
            dragItem.itemIcon = itemDef.Icon;
            dragItem.itemColor = itemEff.Value.gemColor;
            dragItem.parentScrollRect = availableGemsContainer.GetComponentInParent<ScrollRect>();
            string itemIdToSocket = itemEntry.itemId;
            dragItem.onClick = () =>
            {
                if (EquipmentSocketManager.SocketGemToSkill(selectedSocketSlotIndex, -1, itemIdToSocket, out _, out _))
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
                    RefreshGemSocketModal();
                    if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
                    RefreshGrid();
                }
            };

            // Icon Box (Left)
            GameObject ibObj = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ibObj.layer = 5;
            ibObj.transform.SetParent(cardObj.transform, false);
            RectTransform ibRt = ibObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(8f, 0f);
            ibRt.sizeDelta = new Vector2(48f, 48f);
            Image ibImg = ibObj.GetComponent<Image>();
            if (sprItemSlotFrame != null) { ibImg.sprite = sprItemSlotFrame; ibImg.type = Image.Type.Sliced; }
            ibImg.color = itemEff.Value.gemColor;

            GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icObj.layer = 5;
            icObj.transform.SetParent(ibObj.transform, false);
            RectTransform icRt = icObj.GetComponent<RectTransform>();
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.sizeDelta = new Vector2(-8f, -8f);
            Image icImg = icObj.GetComponent<Image>();
            icImg.sprite = itemDef.Icon;
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;

            // Details (Right)
            GameObject infoObj = new GameObject("Info", typeof(RectTransform), typeof(CanvasRenderer));
            infoObj.layer = 5;
            infoObj.transform.SetParent(cardObj.transform, false);
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0f);
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.offsetMin = new Vector2(62f, 4f);
            infoRt.offsetMax = new Vector2(-6f, -4f);

            var vLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleLeft;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;
            vLayout.spacing = 2f;

            // Title + Quantity
            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            tObj.layer = 5;
            tObj.transform.SetParent(infoObj.transform, false);
            var tTmp = tObj.AddComponent<TextMeshProUGUI>();
            string gName = LocalizationManager.IsVietnamese ? itemEff.Value.nameVi : itemEff.Value.nameEn;
            string qtyStr = itemEntry.quantity > 99 ? "99+" : $"x{itemEntry.quantity}";
            tTmp.text = $"<b>{gName}</b> <size=11><color=#00FF88>{qtyStr}</color></size>";
            tTmp.fontSize = 11.5f;
            tTmp.color = Color.white;
            tTmp.raycastTarget = false;

            // Buff Summary
            GameObject bObj = new GameObject("Buff", typeof(RectTransform), typeof(CanvasRenderer));
            bObj.layer = 5;
            bObj.transform.SetParent(infoObj.transform, false);
            var bTmp = bObj.AddComponent<TextMeshProUGUI>();
            bTmp.text = LocalizationManager.IsVietnamese ? itemEff.Value.effectSummaryVi : itemEff.Value.effectSummaryEn;
            bTmp.fontSize = 9.5f;
            bTmp.fontStyle = FontStyles.Bold;
            bTmp.color = itemEff.Value.gemColor;
        }
    }

    private void OnAutoMergeAllButtonClicked()
    {
        int available = EquipmentSocketManager.GetTotalAvailableMergeGemsCount();
        if (available <= 0)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeFail();
            DamagePopup.CreateText(autoMergeAllBtn != null ? autoMergeAllBtn.transform.position : transform.position, LocalizationManager.T("Không có ngọc trùng trong túi!", "No matching gems in bag!"), Color.yellow);
            Debug.LogWarning("[AUTO-MERGE] Không có khoáng thạch/đá quý trùng loại trong túi đồ để nâng cấp!");
            return;
        }

        int consumed = EquipmentSocketManager.AutoMergeAllMatchingGems(out int upgradedSlots, out int totalLevelsGained);
        if (consumed > 0)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();

            // Play floating reward badge effect
            PlaySoulShardFlyEffect(autoMergeAllBtn != null ? autoMergeAllBtn.transform.position : transform.position, consumed);

            RefreshGemSocketModal();
            if (!string.IsNullOrEmpty(selectedItemId)) InspectItem(selectedItemId);
            RefreshGrid();

            string notify = LocalizationManager.T(
                $"Đã nâng cấp {upgradedSlots} ô ngọc (+{totalLevelsGained} Cấp)!",
                $"Upgraded {upgradedSlots} sockets (+{totalLevelsGained} Lvs)!"
            );
            DamagePopup.CreateText(autoMergeAllBtn != null ? autoMergeAllBtn.transform.position : transform.position, notify, new Color(0.2f, 1f, 0.45f));
            Debug.Log($"<color=green>[AUTO-MERGE SUCCESS]</color> Đã tự động nâng cấp thành công {upgradedSlots} ô ngọc, tiêu hao {consumed} viên đá (+{totalLevelsGained} Cấp)!");
        }
    }

    private void BuildCloseButton(Transform parent)
    {
        GameObject closeObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.layer = 5;
        closeObj.transform.SetParent(parent, false);

        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-30f, -30f);
        closeRt.sizeDelta = new Vector2(52f, 52f);

        Image img = closeObj.GetComponent<Image>();
        img.color = new Color(0.75f, 0.22f, 0.25f, 1f);

        var closeOutline = closeObj.AddComponent<Outline>();
        closeOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.7f);
        closeOutline.effectDistance = new Vector2(1.5f, -1.5f);

        closeButton = closeObj.GetComponent<Button>();
        closeButton.targetGraphic = img;
        closeButton.onClick.AddListener(Close);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(closeObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "X";
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    public void Open()
    {
        EnsureItemSprites();
        if (dialogBox == null || tabButtonImgs == null || tabButtonImgs.Length == 0)
        {
            BuildUI();
        }

        isStandaloneSkillMode = false;
        if (dialogBox != null) dialogBox.SetActive(true);
        if (gemSocketModalObj != null) gemSocketModalObj.SetActive(false);
        if (gachaResultModalObj != null) gachaResultModalObj.SetActive(false);
        if (chestRewardModalObj != null) chestRewardModalObj.SetActive(false);
        if (gachaHybridConfirmModalObj != null) gachaHybridConfirmModalObj.SetActive(false);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        isOpen = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();

        EnsureSampleStartingItemsIfEmpty();
        OnTabClicked(-1, 0);
    }

    public void OpenToGachaTab()
    {
        Open();
        OnTabClicked(99, tabButtons != null ? tabButtons.Length - 1 : 5);
    }

    public void Close()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        isOpen = false;
        isStandaloneSkillMode = false;
        if (dialogBox != null) dialogBox.SetActive(false);
        if (gemSocketModalObj != null) gemSocketModalObj.SetActive(false);
        if (gachaResultModalObj != null) gachaResultModalObj.SetActive(false);
        if (chestRewardModalObj != null) chestRewardModalObj.SetActive(false);
        if (gachaHybridConfirmModalObj != null) gachaHybridConfirmModalObj.SetActive(false);
        for (int i = activeFlyShardBadges.Count - 1; i >= 0; i--)
        {
            if (activeFlyShardBadges[i] != null) Destroy(activeFlyShardBadges[i]);
        }
        activeFlyShardBadges.Clear();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(false);
    }

    private void EnsureSampleStartingItemsIfEmpty()
    {
        var inv = AccountDatabase.GetActiveUserInventory();
        if (inv == null || inv.Count == 0)
        {
            AccountDatabase.AddItemToActiveUser("scroll_summon_mythic", 3, "GachaArtifact", "Tặng Tân Thủ");
            AccountDatabase.AddItemToActiveUser("chest_ancient_relic", 2, "GachaArtifact", "Rương Chiến Lợi Phẩm");
            AccountDatabase.AddItemToActiveUser("key_ancient_dungeon", 2, "GachaArtifact", "Chìa Khóa Mở Rương");
            AccountDatabase.AddItemToActiveUser("gem_amethyst", 5, "Material", "Rơi từ Hầm Ngục Map 1");
            AccountDatabase.AddItemToActiveUser("ore_forge_stone", 10, "Material", "Rơi từ Quái vật");
            AccountDatabase.AddItemToActiveUser("wpn_paladin_sword", 1, "Equipment", "Thần Binh Hoàng Gia");
            AccountDatabase.AddItemToActiveUser("shield_aegis_01", 1, "Equipment", "Khiên Hoàng Gia");
        }
    }

    private void OnTabClicked(int categoryFilter, int tabIndex)
    {
        currentCategoryFilter = categoryFilter;

        if (tabButtonImgs == null || tabButtons == null)
        {
            BuildUI();
        }

        if (tabButtonImgs != null)
        {
            for (int i = 0; i < tabButtonImgs.Length; i++)
            {
                if (tabButtonImgs[i] != null)
                {
                    if (tabButtons != null && i == tabButtons.Length - 1) // Gacha Tab
                    {
                        tabButtonImgs[i].color = (i == tabIndex) ? new Color(0.85f, 0.25f, 0.95f, 1f) : new Color(0.45f, 0.15f, 0.6f, 0.95f);
                    }
                    else
                    {
                        tabButtonImgs[i].color = (i == tabIndex) ? new Color(0.85f, 0.42f, 0.15f, 1f) : new Color(0.12f, 0.15f, 0.22f, 0.95f);
                    }
                }
            }
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        if (categoryFilter == 99)
        {
            // Show Gacha Altar View
            if (gridAreaObj != null) gridAreaObj.SetActive(false);
            if (rightInspectorObj != null) rightInspectorObj.SetActive(false);
            if (gachaAltarObj != null)
            {
                gachaAltarObj.SetActive(true);
                UpdateGachaBalanceDisplay();
            }
        }
        else
        {
            // Show Normal Inventory View
            if (gridAreaObj != null) gridAreaObj.SetActive(true);
            if (rightInspectorObj != null) rightInspectorObj.SetActive(true);
            if (gachaAltarObj != null) gachaAltarObj.SetActive(false);
            RefreshGrid();
        }
    }

    private void UpdateGachaBalanceDisplay()
    {
        long currentShards = AccountDatabase.GetAethelShards();
        int currentScrolls = AccountDatabase.GetActiveUserItemCount("scroll_summon_mythic");

        if (gachaShardsBalanceText != null)
        {
            string compactShards = FormatCompactNumber(currentShards);
            gachaShardsBalanceText.text = $"{LocalizationManager.T("Linh Hồn Hiện Có:", "Aethel Shards:")} <color=#FFD700>{compactShards}</color>";
        }
        if (gachaScrollsBalanceText != null)
        {
            gachaScrollsBalanceText.text = $"{LocalizationManager.T("Bùa Triệu Hồi:", "Summon Scrolls:")} <color=#FFA502>{currentScrolls}</color>";
        }
    }

    public void PerformGachaRoll(int pullCount)
    {
        long currentShards = AccountDatabase.GetAethelShards();
        int currentScrolls = AccountDatabase.GetActiveUserItemCount("scroll_summon_mythic");

        if (pullCount == 1)
        {
            if (currentScrolls >= 1)
            {
                ExecuteGachaRoll(1, 1, 0);
            }
            else if (currentShards >= 100)
            {
                ExecuteGachaRoll(1, 0, 100);
            }
            else
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeFail();
                Debug.LogWarning("[GACHA] Không đủ tài nguyên! Cần 100 Linh Hồn hoặc 1 Bùa Triệu Hồi.");
            }
        }
        else if (pullCount == 10)
        {
            // Trường hợp 1: Có đủ >= 10 vé
            if (currentScrolls >= 10)
            {
                ExecuteGachaRoll(10, 10, 0);
            }
            // Trường hợp 2: Có 1 đến 9 vé (Hiện Popup xác nhận bù Shards với giá ưu đãi 90 Shards/lượt)
            else if (currentScrolls > 0)
            {
                int missingPulls = 10 - currentScrolls; // Ví dụ có 7 vé -> thiếu 3 vé
                int topUpShardCost = missingPulls * 90; // Áp dụng đúng giá ưu đãi gói 10x: 3 x 90 = 270 Shards

                if (currentShards >= topUpShardCost)
                {
                    // Hiển thị Popup xác nhận rõ ràng chi phí bù Shards
                    ShowGachaHybridConfirmModal(currentScrolls, missingPulls, topUpShardCost, () =>
                    {
                        ExecuteGachaRoll(10, currentScrolls, topUpShardCost);
                    });
                }
                else if (currentShards >= 900)
                {
                    // Nếu không đủ Shards bù vé nhưng có đủ 900 Shards thuần
                    ExecuteGachaRoll(10, 0, 900);
                }
                else
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeFail();
                    Debug.LogWarning($"[GACHA] Không đủ tài nguyên! Bạn có {currentScrolls} Bùa, cần thêm {topUpShardCost} Linh Hồn ({missingPulls} lượt x 90 Shards) để Quay 10 Lần.");
                }
            }
            // Trường hợp 3: Có 0 vé -> Dùng 900 Shards
            else
            {
                if (currentShards >= 900)
                {
                    ExecuteGachaRoll(10, 0, 900);
                }
                else
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeFail();
                    Debug.LogWarning("[GACHA] Không đủ tài nguyên! Cần 900 Linh Hồn hoặc 10 Bùa Triệu Hồi.");
                }
            }
        }
    }

    private void ExecuteGachaRoll(int pullCount, int scrollsToUse, int shardsToUse)
    {
        // Deduct cost
        if (scrollsToUse > 0)
        {
            AccountDatabase.RemoveItemFromActiveUser("scroll_summon_mythic", scrollsToUse);
            Debug.Log($"<color=cyan>[GACHA]</color> Đã trừ {scrollsToUse} Bùa Triệu Hồi!");
        }

        if (shardsToUse > 0)
        {
            AccountDatabase.SpendAethelShards(shardsToUse);
            if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();
            Debug.Log($"<color=cyan>[GACHA]</color> Đã trừ {shardsToUse} Linh Hồn!");
        }

        UpdateGachaBalanceDisplay();

        // Roll Items from Pool with 10-Roll Pity System
        List<InventoryItemDatabase.ItemDefinition> wonItems = new List<InventoryItemDatabase.ItemDefinition>();
        bool hasHighTier = false;

        for (int i = 0; i < pullCount; i++)
        {
            var wonDef = RollSingleItemFromPool();
            if (wonDef != null && (wonDef.rarity == InventoryItemDatabase.ItemRarity.Epic || wonDef.rarity == InventoryItemDatabase.ItemRarity.Legendary || wonDef.rarity == InventoryItemDatabase.ItemRarity.Mythic))
            {
                hasHighTier = true;
            }
            wonItems.Add(wonDef);
        }

        // Pity Guarantee: If 10-roll and no Epic+ was rolled, upgrade the 10th slot to a guaranteed Epic!
        if (pullCount >= 10 && !hasHighTier && wonItems.Count > 0)
        {
            string[] epicGuaranteedPool = new string[] { "gem_bloodstone", "chest_ancient_relic", "wpn_katana", "gem_amethyst" };
            string epicId = epicGuaranteedPool[Random.Range(0, epicGuaranteedPool.Length)];
            wonItems[wonItems.Count - 1] = InventoryItemDatabase.GetItem(epicId);
        }

        // Add awarded items to account
        foreach (var def in wonItems)
        {
            if (def != null)
            {
                AccountDatabase.AddItemToActiveUser(def.id, 1, def.category.ToString(), "Gacha Vận Mệnh");
            }
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();

        // Show Reveal Modal
        ShowGachaResultReveal(wonItems);
    }

    private InventoryItemDatabase.ItemDefinition RollSingleItemFromPool()
    {
        float roll = Random.Range(0f, 100f);

        string[] mythicPool = new string[] { "wpn_legendary_sword", "shield_dragon_06", "core_abyssal_soul" };
        string[] legendaryPool = new string[] { "wpn_fire_sword", "key_ancient_dungeon", "shield_dragon_06" };
        string[] epicPool = new string[] { "wpn_katana", "chest_ancient_relic", "gem_bloodstone", "gem_amethyst" };
        string[] rarePool = new string[] { "wpn_paladin_sword", "shield_aegis_01", "gem_jade", "ore_gold_node", "ore_tin", "ore_boulder_stone" };
        string[] commonPool = new string[] { "ore_forge_stone", "ore_keep_stone", "ore_copper", "ore_iron", "ore_coal", "ore_stone" };

        string pickedId;
        // Balanced Rates: Mythic 1.5%, Legendary 6.5%, Epic 18%, Rare 34%, Common 40%
        if (roll < 1.5f) pickedId = mythicPool[Random.Range(0, mythicPool.Length)];
        else if (roll < 8.0f) pickedId = legendaryPool[Random.Range(0, legendaryPool.Length)];
        else if (roll < 26.0f) pickedId = epicPool[Random.Range(0, epicPool.Length)];
        else if (roll < 60.0f) pickedId = rarePool[Random.Range(0, rarePool.Length)];
        else pickedId = commonPool[Random.Range(0, commonPool.Length)];

        return InventoryItemDatabase.GetItem(pickedId);
    }

    private void ShowGachaResultReveal(List<InventoryItemDatabase.ItemDefinition> items)
    {
        foreach (var c in resultCardObjs)
        {
            if (c != null) Destroy(c);
        }
        resultCardObjs.Clear();

        foreach (var def in items)
        {
            if (def == null) continue;
            var cardObj = CreateResultCard(def);
            resultCardObjs.Add(cardObj);
        }

        if (gachaResultModalObj != null)
        {
            gachaResultModalObj.SetActive(true);
            gachaResultModalObj.transform.SetAsLastSibling();
        }
    }

    private GameObject CreateResultCard(InventoryItemDatabase.ItemDefinition def)
    {
        GameObject cardObj = new GameObject($"ResultCard_{def.id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.layer = 5;
        cardObj.transform.SetParent(gachaResultCardContainer, false);

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(165f, 160f);

        Image img = cardObj.GetComponent<Image>();
        img.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

        Color rCol = InventoryItemDatabase.GetRarityColor(def.rarity);
        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = rCol;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Icon Box with Framed Sprite
        GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconBox.layer = 5;
        iconBox.transform.SetParent(cardObj.transform, false);
        RectTransform iconBoxRt = iconBox.GetComponent<RectTransform>();
        iconBoxRt.anchoredPosition = new Vector2(0f, 22f);
        iconBoxRt.sizeDelta = new Vector2(74f, 74f);

        Image boxImg = iconBox.GetComponent<Image>();
        if (sprItemSlotFrame != null)
        {
            boxImg.sprite = sprItemSlotFrame;
            boxImg.type = Image.Type.Sliced;
        }
        boxImg.color = rCol;

        // Icon inside
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.layer = 5;
        iconObj.transform.SetParent(iconBox.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = new Vector2(-16f, -16f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = def.Icon;
        iconImg.preserveAspect = true;

        // Tier Tag
        GameObject tierObj = new GameObject("TierTag", typeof(RectTransform), typeof(CanvasRenderer));
        tierObj.layer = 5;
        tierObj.transform.SetParent(cardObj.transform, false);
        RectTransform tierRt = tierObj.GetComponent<RectTransform>();
        tierRt.anchoredPosition = new Vector2(0f, -32f);
        tierRt.sizeDelta = new Vector2(150f, 18f);
        var tierTmp = tierObj.AddComponent<TextMeshProUGUI>();
        tierTmp.text = $"[{InventoryItemDatabase.GetRarityName(def.rarity)}]";
        tierTmp.fontSize = 11;
        tierTmp.fontStyle = FontStyles.Bold;
        tierTmp.color = rCol;
        tierTmp.alignment = TextAlignmentOptions.Center;

        // Name
        GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.layer = 5;
        nameObj.transform.SetParent(cardObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchoredPosition = new Vector2(0f, -54f);
        nameRt.sizeDelta = new Vector2(155f, 24f);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = LocalizationManager.IsVietnamese ? def.nameVi : def.nameEn;
        nameTmp.fontSize = 12;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;

        return cardObj;
    }

    public void RefreshGrid()
    {
        foreach (var go in slotGameObjects)
        {
            if (go != null) Destroy(go);
        }
        slotGameObjects.Clear();

        var userItems = AccountDatabase.GetActiveUserInventory();
        List<InventoryItemDatabase.ItemDefinition> displayItems = new List<InventoryItemDatabase.ItemDefinition>();

        foreach (var entry in userItems)
        {
            if (entry == null || entry.quantity <= 0) continue;
            var def = InventoryItemDatabase.GetItem(entry.itemId);
            if (def == null) continue;

            if (currentCategoryFilter == -1 || (int)def.category == currentCategoryFilter)
            {
                displayItems.Add(def);
            }
        }

        if (displayItems.Count > 0)
        {
            if (string.IsNullOrEmpty(selectedItemId) || !displayItems.Exists(i => i.id == selectedItemId))
            {
                selectedItemId = displayItems[0].id;
            }
        }
        else
        {
            selectedItemId = "";
        }

        foreach (var def in displayItems)
        {
            int qty = AccountDatabase.GetActiveUserItemCount(def.id);
            var slotObj = CreateItemSlot(def, qty);
            slotGameObjects.Add(slotObj);
        }

        InspectItem(selectedItemId);
    }

    private GameObject CreateItemSlot(InventoryItemDatabase.ItemDefinition def, int quantity)
    {
        GameObject slotObj = new GameObject($"Slot_{def.id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        slotObj.layer = 5;
        slotObj.transform.SetParent(gridContent, false);

        RectTransform slotRt = slotObj.GetComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(125f, 135f);

        Image slotImg = slotObj.GetComponent<Image>();
        bool isSelected = (def.id == selectedItemId);
        slotImg.color = isSelected ? new Color(0.18f, 0.24f, 0.35f, 1f) : new Color(0.09f, 0.11f, 0.16f, 0.95f);

        Color rarityCol = InventoryItemDatabase.GetRarityColor(def.rarity);
        var slotOutline = slotObj.AddComponent<Outline>();
        slotOutline.effectColor = isSelected ? new Color(1f, 0.9f, 0.35f, 1f) : new Color(0.3f, 0.38f, 0.5f, 0.5f);
        slotOutline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

        // Mobile 2s Long-Press to Reveal Salvage [X] Badge
        var lp = slotObj.AddComponent<UILongPressTrigger>();
        lp.targetItemId = def.id;
        lp.holdDuration = 2.0f;

        Button btn = slotObj.GetComponent<Button>();
        btn.targetGraphic = slotImg;
        btn.onClick.AddListener(() =>
        {
            selectedItemId = def.id;
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            RefreshGrid();
        });

        // Framed Icon Box
        GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconBox.layer = 5;
        iconBox.transform.SetParent(slotObj.transform, false);
        RectTransform iconBoxRt = iconBox.GetComponent<RectTransform>();
        iconBoxRt.anchoredPosition = new Vector2(0f, 16f);
        iconBoxRt.sizeDelta = new Vector2(68f, 68f);

        Image boxImg = iconBox.GetComponent<Image>();
        if (sprItemSlotFrame != null)
        {
            boxImg.sprite = sprItemSlotFrame;
            boxImg.type = Image.Type.Sliced;
        }
        boxImg.color = isSelected ? new Color(1f, 0.9f, 0.35f, 1f) : rarityCol;

        // Icon inside box
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.layer = 5;
        iconObj.transform.SetParent(iconBox.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = new Vector2(-16f, -16f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = def.Icon;
        iconImg.preserveAspect = true;

        // Name (Clean, placed below the framed icon box)
        GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.layer = 5;
        nameObj.transform.SetParent(slotObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0f);
        nameRt.anchorMax = new Vector2(1f, 0f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.anchoredPosition = new Vector2(0f, 6f);
        nameRt.sizeDelta = new Vector2(-8f, 26f);

        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = LocalizationManager.IsVietnamese ? def.nameVi : def.nameEn;
        nameTmp.fontSize = 11;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = rarityCol;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.enableWordWrapping = false;

        // Quantity Badge (if > 1)
        if (quantity > 1)
        {
            GameObject qtyObj = new GameObject("QtyBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            qtyObj.layer = 5;
            qtyObj.transform.SetParent(slotObj.transform, false);
            RectTransform qtyRt = qtyObj.GetComponent<RectTransform>();
            qtyRt.anchorMin = new Vector2(1f, 1f);
            qtyRt.anchorMax = new Vector2(1f, 1f);
            qtyRt.pivot = new Vector2(1f, 1f);
            qtyRt.anchoredPosition = new Vector2(-4f, -4f);
            qtyRt.sizeDelta = new Vector2(36f, 18f);

            Image qtyBg = qtyObj.GetComponent<Image>();
            qtyBg.color = new Color(0f, 0f, 0f, 0.85f);

            GameObject qtyTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            qtyTxtObj.layer = 5;
            qtyTxtObj.transform.SetParent(qtyObj.transform, false);
            RectTransform qTxtRt = qtyTxtObj.GetComponent<RectTransform>();
            qTxtRt.anchorMin = Vector2.zero;
            qTxtRt.anchorMax = Vector2.one;
            qTxtRt.sizeDelta = Vector2.zero;

            var qTmp = qtyTxtObj.AddComponent<TextMeshProUGUI>();
            qTmp.text = (quantity > 99) ? "99+" : $"x{quantity}";
            qTmp.fontSize = 10;
            qTmp.fontStyle = FontStyles.Bold;
            qTmp.color = Color.white;
            qTmp.alignment = TextAlignmentOptions.Center;
            qTmp.enableWordWrapping = false;
            qTmp.overflowMode = TextOverflowModes.Overflow;
        }

        return slotObj;
    }

    private void InspectItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            detailNameText.text = LocalizationManager.T("CHƯA CHỌN VẬT PHẨM", "NO ITEM SELECTED");
            detailTierBadgeText.text = "";
            detailSourceText.text = "";
            detailStatsText.text = "";
            detailDescText.text = LocalizationManager.T("Chọn một vật phẩm bên danh sách để xem chi tiết.", "Select an item from the list to inspect details.");
            detailQuantityText.text = "";
            if (actionButton != null) actionButton.gameObject.SetActive(false);
            if (dismantleButton != null) dismantleButton.gameObject.SetActive(false);
            if (detailIconImg != null) detailIconImg.gameObject.SetActive(false);
            if (detailIconBoxImg != null) detailIconBoxImg.gameObject.SetActive(false);
            return;
        }

        var def = InventoryItemDatabase.GetItem(itemId);
        if (def == null) return;

        int count = AccountDatabase.GetActiveUserItemCount(itemId);
        Color rCol = InventoryItemDatabase.GetRarityColor(def.rarity);

        if (detailIconBoxImg != null)
        {
            detailIconBoxImg.gameObject.SetActive(true);
            detailIconBoxImg.color = rCol;
        }

        if (detailIconImg != null)
        {
            detailIconImg.gameObject.SetActive(true);
            detailIconImg.sprite = def.Icon;
        }

        detailNameText.text = LocalizationManager.IsVietnamese ? def.nameVi : def.nameEn;
        detailNameText.color = rCol;

        detailTierBadgeText.text = $"[ {InventoryItemDatabase.GetRarityName(def.rarity)} - {InventoryItemDatabase.GetCategoryName(def.category)} ]";
        detailTierBadgeText.color = rCol;

        string src = LocalizationManager.IsVietnamese ? def.sourceVi : def.sourceEn;
        detailSourceText.text = $"{LocalizationManager.T("Nguồn gốc:", "Source:")} {src}";

        List<string> statLines = new List<string>();
        if (def.atkBonus > 0) statLines.Add($"+{def.atkBonus} ATK");
        if (def.hpBonus > 0) statLines.Add($"+{def.hpBonus} HP");
        if (def.defBonus > 0) statLines.Add($"+{def.defBonus}% DEF");
        if (def.speedBonus > 0) statLines.Add($"+{def.speedBonus:F1} SPD");

        int chestCount = AccountDatabase.GetActiveUserItemCount("chest_ancient_relic");
        int keyCount = AccountDatabase.GetActiveUserItemCount("key_ancient_dungeon");

        string countFormatted = (count >= 10_000) ? $"{FormatCompactNumber(count)} ({count:N0})" : count.ToString("N0");

        if (def.id.Contains("chest"))
        {
            detailStatsText.text = $"<color=#FFD700>[ {LocalizationManager.T("RƯƠNG TÀN TÍCH CỔ", "ANCIENT RELIC CHEST")} ]</color>\n<color={(keyCount > 0 ? "#00FF88" : "#FF5555")}>{LocalizationManager.T("Yêu cầu: 1 Rương + 1 Chìa Khóa Tàn Tích", "Requires: 1 Chest + 1 Ancient Key")}</color>\n<color=#00E5FF>{LocalizationManager.T("Mở nhận 200~500 Linh Hồn & Khoáng Thạch Quý", "Open to receive 200~500 Shards & Rare Gems")}</color>";
            detailQuantityText.text = $"{LocalizationManager.T("Sở hữu:", "Owned:")} <color=#FFD700>{chestCount} {LocalizationManager.T("Rương", "Chest(s)")}</color>  •  <color={(keyCount > 0 ? "#00FF88" : "#FF5555")}>{keyCount} {LocalizationManager.T("Chìa", "Key(s)")}</color>";
        }
        else if (def.id.Contains("key"))
        {
            detailStatsText.text = $"<color=#FFD700>[ {LocalizationManager.T("CHÌA KHÓA TÀN TÍCH", "ANCIENT DUNGEON KEY")} ]</color>\n<color={(chestCount > 0 ? "#00FF88" : "#FF5555")}>{LocalizationManager.T("Dùng kết hợp với Rương Tàn Tích (Cần 1 Rương + 1 Chìa)", "Use with Ancient Chest (Requires 1 Chest + 1 Key)")}</color>\n<color=#00E5FF>{LocalizationManager.T("Mở khóa kho báu cổ ngàn năm", "Unlocks ancient empire treasures")}</color>";
            detailQuantityText.text = $"{LocalizationManager.T("Sở hữu:", "Owned:")} <color=#FFD700>{keyCount} {LocalizationManager.T("Chìa", "Key(s)")}</color>  •  <color={(chestCount > 0 ? "#00FF88" : "#FF5555")}>{chestCount} {LocalizationManager.T("Rương", "Chest(s)")}</color>";
        }
        else if (def.id == "scroll_summon_mythic")
        {
            detailStatsText.text = $"<color=#FFD700>[ {LocalizationManager.T("BÙA TRIỆU HỒI", "SUMMON SCROLL")} ]</color>\n<color=#00FF88>{LocalizationManager.T("Dùng tại Đài Vận Mệnh để triệu hồi Thần Binh", "Use at Gacha Vault to summon Mythic Blades")}</color>";
            detailQuantityText.text = $"{LocalizationManager.T("Số lượng sở hữu:", "Owned:")} <color=#FFD700>{countFormatted}</color>";
        }
        else if (def.id == "core_abyssal_soul")
        {
            detailStatsText.text = $"<color=#FF4757>[ {LocalizationManager.T("LÕI LINH HỒN THẦN THOẠI", "MYTHIC SOUL CORE")} ]</color>\n<color=#00FF88>{LocalizationManager.T("Hấp thụ nhận ngay +1000 Linh Hồn (Aethel Shards)", "Absorb to immediately obtain +1000 Aethel Shards")}</color>";
            detailQuantityText.text = $"{LocalizationManager.T("Số lượng sở hữu:", "Owned:")} <color=#FFD700>{countFormatted}</color>";
        }
        else if (def.category == InventoryItemDatabase.ItemCategory.Equipment)
        {
            var gemEff = EquipmentSocketManager.GetGemEffect(def.id);
            string effText = gemEff.HasValue ? (LocalizationManager.IsVietnamese ? gemEff.Value.effectSummaryVi : gemEff.Value.effectSummaryEn) : "";
            string baseStatStr = (statLines.Count > 0) ? string.Join("  |  ", statLines) : "";
            string buffPart = !string.IsNullOrEmpty(effText) ? $"<color=#00FF88>[Khảm Chiêu]: {effText}</color>" : "";
            detailStatsText.text = !string.IsNullOrEmpty(buffPart) ? $"{baseStatStr}\n{buffPart}" : baseStatStr;
            detailQuantityText.text = $"{LocalizationManager.T("Số lượng sở hữu:", "Owned:")} <color=#FFD700>{countFormatted}</color>";
        }
        else if (EquipmentSocketManager.IsGem(def.id) || def.category == InventoryItemDatabase.ItemCategory.Material)
        {
            var gemEff = EquipmentSocketManager.GetGemEffect(def.id);
            string effText = gemEff.HasValue ? (LocalizationManager.IsVietnamese ? gemEff.Value.effectSummaryVi : gemEff.Value.effectSummaryEn) : "";
            detailStatsText.text = $"<color=#00FF88>[Thuộc tính Khảm]: {effText}</color>\n<color=#00D2FF>{LocalizationManager.T("Khảm tại mục 'Kỹ Năng' | Ghép trùng để Lên Cấp", "Socket at 'Skills' | Merge duplicates to Level Up")}</color>";
            detailQuantityText.text = $"{LocalizationManager.T("Số lượng sở hữu:", "Owned:")} <color=#FFD700>{countFormatted}</color>";
        }
        else if (statLines.Count > 0)
        {
            detailStatsText.text = string.Join("  |  ", statLines);
            detailQuantityText.text = $"{LocalizationManager.T("Số lượng sở hữu:", "Owned:")} <color=#FFD700>{countFormatted}</color>";
        }
        else
        {
            detailStatsText.text = def.isUsable ? (LocalizationManager.IsVietnamese ? def.useEffectTextVi : def.useEffectTextEn) : "";
            detailQuantityText.text = $"{LocalizationManager.T("Số lượng sở hữu:", "Owned:")} <color=#FFD700>{countFormatted}</color>";
        }

        detailDescText.text = LocalizationManager.IsVietnamese ? def.descVi : def.descEn;

        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(true);
            RectTransform actionRt = actionButton.GetComponent<RectTransform>();

            if (openAllButton != null) openAllButton.gameObject.SetActive(false);

            if (def.id == "scroll_summon_mythic")
            {
                if (actionRt != null) { actionRt.anchoredPosition = new Vector2(0f, -145f); actionRt.sizeDelta = new Vector2(360f, 44f); }
                actionBtnImg.color = new Color(0.7f, 0.25f, 0.85f, 1f);
                actionBtnText.text = LocalizationManager.T("ĐẾN ĐÀI GACHA", "GO TO GACHA");
                actionButton.interactable = (count > 0);
            }
            else if (def.id.Contains("chest"))
            {
                int maxOpen = Mathf.Min(chestCount, keyCount);

                if (maxOpen >= 2)
                {
                    // 2-Button Split: [ MỞ ] (Left) and [ MỞ TẤT CẢ (N) ] (Right)
                    if (actionRt != null) { actionRt.anchoredPosition = new Vector2(-92f, -145f); actionRt.sizeDelta = new Vector2(176f, 44f); }
                    actionBtnText.text = LocalizationManager.T("MỞ", "OPEN");
                    actionButton.interactable = true;
                    actionBtnImg.color = new Color(0.18f, 0.65f, 0.38f, 1f);

                    if (openAllButton != null)
                    {
                        openAllButton.gameObject.SetActive(true);
                        RectTransform openAllRt = openAllButton.GetComponent<RectTransform>();
                        if (openAllRt != null) { openAllRt.anchoredPosition = new Vector2(92f, -145f); openAllRt.sizeDelta = new Vector2(176f, 44f); }

                        openAllBtnImg.color = new Color(0.9f, 0.55f, 0.12f, 1f);
                        openAllBtnText.text = LocalizationManager.T($"MỞ HẾT ({FormatCompactNumber(maxOpen)})", $"OPEN ALL ({FormatCompactNumber(maxOpen)})");
                        openAllButton.interactable = true;
                    }
                }
                else
                {
                    // Single Centered Button: only 1 chest or only 1 key available
                    if (openAllButton != null) openAllButton.gameObject.SetActive(false);

                    if (actionRt != null) { actionRt.anchoredPosition = new Vector2(0f, -145f); actionRt.sizeDelta = new Vector2(360f, 44f); }
                    if (keyCount > 0 && chestCount > 0)
                    {
                        actionBtnImg.color = new Color(0.18f, 0.65f, 0.38f, 1f);
                        actionBtnText.text = LocalizationManager.T("MỞ", "OPEN");
                        actionButton.interactable = true;
                    }
                    else
                    {
                        actionBtnImg.color = new Color(0.55f, 0.20f, 0.22f, 1f);
                        actionBtnText.text = LocalizationManager.T("THIẾU CHÌA KHÓA", "MISSING KEY");
                        actionButton.interactable = false;
                    }
                }
            }
            else if (def.id.Contains("key"))
            {
                if (actionRt != null) { actionRt.anchoredPosition = new Vector2(0f, -145f); actionRt.sizeDelta = new Vector2(360f, 44f); }
                if (chestCount > 0)
                {
                    actionBtnImg.color = new Color(0.20f, 0.50f, 0.85f, 1f);
                    actionBtnText.text = LocalizationManager.T("ĐẾN RƯƠNG TÀN TÍCH", "GO TO ANCIENT CHEST");
                    actionButton.interactable = true;
                }
                else
                {
                    actionBtnImg.color = new Color(0.40f, 0.40f, 0.45f, 1f);
                    actionBtnText.text = LocalizationManager.T("CHƯA CÓ RƯƠNG", "NO CHEST IN BAG");
                    actionButton.interactable = false;
                }
            }
            else if (def.id.Contains("core"))
            {
                if (count >= 2)
                {
                    if (actionRt != null) { actionRt.anchoredPosition = new Vector2(-92f, -145f); actionRt.sizeDelta = new Vector2(176f, 44f); }
                    actionBtnImg.color = new Color(0.15f, 0.55f, 0.85f, 1f);
                    actionBtnText.text = LocalizationManager.T("HẤP THỤ 1", "ABSORB 1");
                    actionButton.interactable = true;

                    if (openAllButton != null)
                    {
                        openAllButton.gameObject.SetActive(true);
                        RectTransform openAllRt = openAllButton.GetComponent<RectTransform>();
                        if (openAllRt != null) { openAllRt.anchoredPosition = new Vector2(92f, -145f); openAllRt.sizeDelta = new Vector2(176f, 44f); }
                        openAllBtnImg.color = new Color(0.85f, 0.25f, 0.85f, 1f);
                        openAllBtnText.text = LocalizationManager.T($"HẤP THỤ HẾT ({FormatCompactNumber(count)})", $"ABSORB ALL ({FormatCompactNumber(count)})");
                        openAllButton.interactable = true;
                    }
                }
                else
                {
                    if (openAllButton != null) openAllButton.gameObject.SetActive(false);
                    if (actionRt != null) { actionRt.anchoredPosition = new Vector2(0f, -145f); actionRt.sizeDelta = new Vector2(360f, 44f); }
                    actionBtnImg.color = new Color(0.15f, 0.55f, 0.85f, 1f);
                    actionBtnText.text = LocalizationManager.T("HẤP THỤ (+1000 LINH HỒN)", "ABSORB (+1000 SHARDS)");
                    actionButton.interactable = (count > 0);
                }
            }
            else if (def.category == InventoryItemDatabase.ItemCategory.Equipment || EquipmentSocketManager.IsGem(def.id) || def.category == InventoryItemDatabase.ItemCategory.Material)
            {
                int shardVal = GetDismantleShardValue(def);
                if (count >= 2)
                {
                    long totalShardVal = (long)shardVal * count;
                    if (actionRt != null) { actionRt.anchoredPosition = new Vector2(-92f, -145f); actionRt.sizeDelta = new Vector2(176f, 44f); }
                    actionBtnImg.color = new Color(0.85f, 0.40f, 0.12f, 1f);
                    actionBtnText.text = LocalizationManager.T($"RÃ 1 (+{FormatCompactNumber(shardVal)})", $"SALVAGE 1 (+{FormatCompactNumber(shardVal)})");
                    actionButton.interactable = true;

                    if (openAllButton != null)
                    {
                        openAllButton.gameObject.SetActive(true);
                        RectTransform openAllRt = openAllButton.GetComponent<RectTransform>();
                        if (openAllRt != null) { openAllRt.anchoredPosition = new Vector2(92f, -145f); openAllRt.sizeDelta = new Vector2(176f, 44f); }
                        openAllBtnImg.color = new Color(0.92f, 0.25f, 0.15f, 1f);
                        openAllBtnText.text = LocalizationManager.T($"RÃ HẾT (+{FormatCompactNumber(totalShardVal)})", $"SALVAGE ALL (+{FormatCompactNumber(totalShardVal)})");
                        openAllButton.interactable = true;
                    }
                }
                else
                {
                    if (openAllButton != null) openAllButton.gameObject.SetActive(false);
                    if (actionRt != null) { actionRt.anchoredPosition = new Vector2(0f, -145f); actionRt.sizeDelta = new Vector2(360f, 44f); }
                    actionBtnImg.color = new Color(0.85f, 0.40f, 0.12f, 1f);
                    actionBtnText.text = LocalizationManager.T($"RÃ LẤY LINH HỒN (+{FormatCompactNumber(shardVal)})", $"SALVAGE (+{FormatCompactNumber(shardVal)} SHARDS)");
                    actionButton.interactable = (count > 0);
                }
            }
            else
            {
                if (openAllButton != null) openAllButton.gameObject.SetActive(false);
                if (actionRt != null) { actionRt.anchoredPosition = new Vector2(0f, -145f); actionRt.sizeDelta = new Vector2(360f, 44f); }
                actionBtnImg.color = new Color(0.35f, 0.35f, 0.4f, 1f);
                actionBtnText.text = LocalizationManager.T("SỬ DỤNG", "USE");
                actionButton.interactable = (count > 0);
            }
        }
    }

    public static string FormatCompactNumber(long val)
    {
        if (val >= 1_000_000_000) return (val / 1_000_000_000.0).ToString("0.##") + "B";
        if (val >= 1_000_000) return (val / 1_000_000.0).ToString("0.##") + "M";
        if (val >= 10_000) return (val / 1_000.0).ToString("0.#") + "K";
        return val.ToString("N0");
    }

    public static int GetDismantleShardValue(InventoryItemDatabase.ItemDefinition def)
    {
        if (def == null) return 10;
        if (def.sellPrice > 0) return def.sellPrice;
        return GetDismantleValue(def.rarity);
    }

    public static int GetDismantleValue(InventoryItemDatabase.ItemRarity rarity)
    {
        switch (rarity)
        {
            case InventoryItemDatabase.ItemRarity.Mythic: return 2000;
            case InventoryItemDatabase.ItemRarity.Legendary: return 800;
            case InventoryItemDatabase.ItemRarity.Epic: return 300;
            case InventoryItemDatabase.ItemRarity.Rare: return 120;
            case InventoryItemDatabase.ItemRarity.Common: return 20;
            default: return 25;
        }
    }

    private void OnActionButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        var def = InventoryItemDatabase.GetItem(selectedItemId);
        if (def == null) return;

        if (def.id == "scroll_summon_mythic")
        {
            // Switch to Gacha Tab
            OnTabClicked(99, tabButtons != null ? tabButtons.Length - 1 : 4);
            return;
        }

        if (def.id.Contains("key"))
        {
            // Direct Link / Jump to Ancient Relic Chest
            selectedItemId = "chest_ancient_relic";
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            RefreshGrid();
            return;
        }

        if (def.id.Contains("chest"))
        {
            int chestCount = AccountDatabase.GetActiveUserItemCount("chest_ancient_relic");
            int keyCount = AccountDatabase.GetActiveUserItemCount("key_ancient_dungeon");

            if (chestCount < 1 || keyCount < 1)
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeFail();
                Debug.LogWarning("[INVENTORY] Cần có cả 1 Rương Tàn Tích và 1 Chìa Khóa để mở khóa!");
                return;
            }

            var chestDef = InventoryItemDatabase.GetItem("chest_ancient_relic");
            if (chestDef != null)
            {
                OpenChestWithRewardAnimation(chestDef);
            }
            return;
        }

        if (def.id.Contains("core"))
        {
            if (AccountDatabase.RemoveItemFromActiveUser(selectedItemId, 1))
            {
                AccountDatabase.AddAethelShards(1000);
                if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

                PlaySoulShardFlyEffect(actionButton != null ? actionButton.transform.position : transform.position, 1000);
                Debug.Log("<color=green>[INVENTORY]</color> Đã hấp thu Lõi Linh Hồn Vực Thẳm, nhận +1000 Aethel Shards!");
                InspectItem(selectedItemId);
                RefreshGrid();
            }
            return;
        }

        // Salvage / Dismantle Gear or Materials
        if (def.category == InventoryItemDatabase.ItemCategory.Equipment || EquipmentSocketManager.IsGem(def.id) || def.category == InventoryItemDatabase.ItemCategory.Material)
        {
            int shardVal = GetDismantleShardValue(def);
            if (AccountDatabase.RemoveItemFromActiveUser(selectedItemId, 1))
            {
                AccountDatabase.AddAethelShards(shardVal);
                if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

                PlaySoulShardFlyEffect(actionButton != null ? actionButton.transform.position : transform.position, shardVal);
                Debug.Log($"<color=green>[INVENTORY]</color> Đã rã 1x {def.nameVi} nhận được +{shardVal} Linh Hồn!");
                InspectItem(selectedItemId);
                RefreshGrid();
            }
        }
    }

    private List<GameObject> activeFlyShardBadges = new List<GameObject>();

    public void PlaySoulShardFlyEffect(Vector3 startWorldPos, long shardAmount)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();

        // Clean up any existing floating badges so they never stack or linger
        for (int i = activeFlyShardBadges.Count - 1; i >= 0; i--)
        {
            if (activeFlyShardBadges[i] != null) Destroy(activeFlyShardBadges[i]);
        }
        activeFlyShardBadges.Clear();

        // Spawn lightweight floating soul shard text
        GameObject flyObj = new GameObject("FlySoulShardBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
        flyObj.layer = 5;
        flyObj.transform.SetParent(transform, false);
        flyObj.transform.position = startWorldPos;

        activeFlyShardBadges.Add(flyObj);

        RectTransform rt = flyObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240f, 32f);

        CanvasGroup cg = flyObj.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Glowing crisp floating text with drop shadow (no opaque dark box covering the UI)
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(flyObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        string formattedAmount = FormatCompactNumber(shardAmount);
        tmp.text = $"<color=#00FFFF><b>+{formattedAmount}</b></color> <size=12><color=#FFD700>★ {LocalizationManager.T("LINH HỒN", "SHARDS")}</color></size>";
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 15f;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        var shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        StartCoroutine(AnimateFlyShards(flyObj, cg, rt));
    }

    private IEnumerator AnimateFlyShards(GameObject flyObj, CanvasGroup cg, RectTransform rt)
    {
        Vector2 startPos = rt.anchoredPosition;
        // Float upwards slightly (+30px) without ever reaching InfoCard or description text
        Vector2 endPos = startPos + new Vector2(0f, 30f);

        float duration = 0.55f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (flyObj == null || cg == null || rt == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
            float scale = (t < 0.2f) ? Mathf.Lerp(0.7f, 1.15f, t / 0.2f) : Mathf.Lerp(1.15f, 1.0f, (t - 0.2f) / 0.8f);
            rt.localScale = Vector3.one * scale;

            if (t > 0.35f)
            {
                cg.alpha = Mathf.Lerp(1f, 0f, (t - 0.35f) / 0.65f);
            }

            yield return null;
        }

        if (flyObj != null)
        {
            activeFlyShardBadges.Remove(flyObj);
            Destroy(flyObj);
        }
    }

    private void OpenChestWithRewardAnimation(InventoryItemDatabase.ItemDefinition chestDef)
    {
        // Require 1 Chest + 1 Key (Hextech Mechanism)
        if (!AccountDatabase.RemoveItemFromActiveUser("chest_ancient_relic", 1)) return;
        if (!AccountDatabase.RemoveItemFromActiveUser("key_ancient_dungeon", 1))
        {
            // Refund chest if key failed
            AccountDatabase.AddItemToActiveUser("chest_ancient_relic", 1, "GachaArtifact", "Hoàn trả rương");
            return;
        }

        // Roll Rewards: 250 ~ 500 Aethel Shards + 1 Bonus Precious Mineral
        int shardReward = Random.Range(250, 501);
        AccountDatabase.AddAethelShards(shardReward);

        string[] bonusPool = new string[] { "ore_gold_node", "gem_amethyst", "gem_bloodstone", "gem_jade", "ore_tin", "ore_forge_stone" };
        string bonusItemId = bonusPool[Random.Range(0, bonusPool.Length)];
        var bonusItemDef = InventoryItemDatabase.GetItem(bonusItemId);
        if (bonusItemDef != null)
        {
            AccountDatabase.AddItemToActiveUser(bonusItemDef.id, 1, bonusItemDef.category.ToString(), "Rương Tàn Tích");
        }

        if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();
        InspectItem(selectedItemId);
        RefreshGrid();

        StartCoroutine(AnimateChestOpeningSequence(chestDef, shardReward, bonusItemDef));
    }

    private void OnOpenAllButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        var def = InventoryItemDatabase.GetItem(selectedItemId);
        if (def == null) return;

        if (def.id.Contains("chest"))
        {
            int chestCount = AccountDatabase.GetActiveUserItemCount("chest_ancient_relic");
            int keyCount = AccountDatabase.GetActiveUserItemCount("key_ancient_dungeon");
            int openCount = Mathf.Min(chestCount, keyCount);
            if (openCount < 2)
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeFail();
                Debug.LogWarning("[INVENTORY] Không đủ số lượng chìa khóa và rương để mở tất cả!");
                return;
            }

            OpenMultipleChestsWithRewardAnimation(openCount);
            return;
        }

        if (def.id.Contains("core"))
        {
            int coreCount = AccountDatabase.GetActiveUserItemCount(selectedItemId);
            if (coreCount < 2) return;

            if (AccountDatabase.RemoveItemFromActiveUser(selectedItemId, coreCount))
            {
                long gain = (long)1000 * coreCount;
                AccountDatabase.AddAethelShards(gain);
                if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

                PlaySoulShardFlyEffect(openAllButton != null ? openAllButton.transform.position : transform.position, gain);
                Debug.Log($"<color=green>[INVENTORY]</color> Đã hấp thu toàn bộ {coreCount}x Lõi Linh Hồn, nhận +{gain} Aethel Shards!");
                InspectItem(selectedItemId);
                RefreshGrid();
            }
            return;
        }

        // Salvage / Dismantle All items of this type
        if (def.category == InventoryItemDatabase.ItemCategory.Equipment || EquipmentSocketManager.IsGem(def.id) || def.category == InventoryItemDatabase.ItemCategory.Material)
        {
            int totalCount = AccountDatabase.GetActiveUserItemCount(selectedItemId);
            if (totalCount < 2) return;

            int singleShardVal = GetDismantleShardValue(def);
            long totalShardVal = (long)singleShardVal * totalCount;

            if (AccountDatabase.RemoveItemFromActiveUser(selectedItemId, totalCount))
            {
                AccountDatabase.AddAethelShards(totalShardVal);
                if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

                PlaySoulShardFlyEffect(openAllButton != null ? openAllButton.transform.position : transform.position, totalShardVal);
                Debug.Log($"<color=green>[INVENTORY]</color> Đã rã toàn bộ {totalCount}x {def.nameVi} nhận được +{totalShardVal} Linh Hồn!");
                InspectItem(selectedItemId);
                RefreshGrid();
            }
        }
    }

    private void OpenMultipleChestsWithRewardAnimation(int openCount)
    {
        // Deduct openCount chests and openCount keys
        if (!AccountDatabase.RemoveItemFromActiveUser("chest_ancient_relic", openCount)) return;
        if (!AccountDatabase.RemoveItemFromActiveUser("key_ancient_dungeon", openCount))
        {
            AccountDatabase.AddItemToActiveUser("chest_ancient_relic", openCount, "GachaArtifact", "Hoàn trả rương");
            return;
        }

        // Calculate total shard reward: openCount times Random.Range(250, 501)
        long totalShards = 0;
        for (int i = 0; i < openCount; i++)
        {
            totalShards += Random.Range(250, 501);
        }

        AccountDatabase.AddAethelShards(totalShards);

        // Roll openCount bonus minerals/gems & aggregate
        string[] bonusPool = new string[] { "ore_gold_node", "gem_amethyst", "gem_bloodstone", "gem_jade", "ore_tin", "ore_forge_stone" };
        Dictionary<string, int> bonusDrops = new Dictionary<string, int>();
        for (int i = 0; i < openCount; i++)
        {
            string bonusItemId = bonusPool[Random.Range(0, bonusPool.Length)];
            if (bonusDrops.ContainsKey(bonusItemId))
                bonusDrops[bonusItemId]++;
            else
                bonusDrops[bonusItemId] = 1;
        }

        foreach (var kvp in bonusDrops)
        {
            var bDef = InventoryItemDatabase.GetItem(kvp.Key);
            if (bDef != null)
            {
                AccountDatabase.AddItemToActiveUser(bDef.id, kvp.Value, bDef.category.ToString(), "Rương Tàn Tích");
            }
        }

        if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();
        InspectItem(selectedItemId);
        RefreshGrid();

        StartCoroutine(AnimateMultipleChestsOpeningSequence(openCount, totalShards, bonusDrops));
    }

    private IEnumerator AnimateMultipleChestsOpeningSequence(int openCount, long totalShards, Dictionary<string, int> bonusDrops)
    {
        if (chestRewardModalObj == null) yield break;

        foreach (var c in chestRewardCardObjs)
        {
            if (c != null) Destroy(c);
        }
        chestRewardCardObjs.Clear();

        chestRewardModalObj.SetActive(true);
        chestRewardModalObj.transform.SetAsLastSibling();

        if (chestRewardConfirmBtn != null) chestRewardConfirmBtn.gameObject.SetActive(false);

        var chestDef = InventoryItemDatabase.GetItem("chest_ancient_relic");

        if (chestRewardTitleText != null)
        {
            chestRewardTitleText.text = LocalizationManager.T($"ĐANG MỞ KHÓA {openCount} RƯƠNG TÀN TÍCH...", $"UNLOCKING {openCount} ANCIENT CHESTS...");
            chestRewardTitleText.color = new Color(1f, 0.75f, 0.25f);
        }
        if (chestRewardSubText != null)
        {
            chestRewardSubText.text = LocalizationManager.T($"Tiêu hao: {openCount} Rương Tàn Tích + {openCount} Chìa Khóa Tàn Tích", $"Consumed: {openCount} Ancient Chests + {openCount} Ancient Keys");
        }

        if (chestRewardIconImg != null)
        {
            chestRewardIconImg.sprite = (sprChestClosed != null) ? sprChestClosed : (chestDef != null ? chestDef.Icon : null);
            chestRewardIconImg.transform.localScale = Vector3.one;
        }

        // Shake chest animation for 0.55s
        Vector3 origPos = (chestRewardIconImg != null) ? chestRewardIconImg.rectTransform.anchoredPosition : Vector2.zero;
        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (chestRewardIconImg != null)
            {
                float shakeOffset = Mathf.Sin(elapsed * 50f) * (7f * (elapsed / 0.55f));
                chestRewardIconImg.rectTransform.anchoredPosition = origPos + new Vector3(shakeOffset, 0f, 0f);
            }
            yield return null;
        }

        if (chestRewardIconImg != null) chestRewardIconImg.rectTransform.anchoredPosition = origPos;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();

        if (chestRewardIconImg != null)
        {
            chestRewardIconImg.sprite = (sprChestOpen != null) ? sprChestOpen : (chestDef != null ? chestDef.Icon : null);
        }

        // Scale burst
        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, 1.35f, elapsed / 0.25f);
            if (chestRewardIconImg != null) chestRewardIconImg.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.35f, 1.0f, elapsed / 0.2f);
            if (chestRewardIconImg != null) chestRewardIconImg.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        if (chestRewardTitleText != null)
        {
            chestRewardTitleText.text = LocalizationManager.T($"ĐÃ MỞ THÀNH CÔNG {openCount} RƯƠNG!", $"{openCount} CHESTS OPENED!");
            chestRewardTitleText.color = new Color(1f, 0.88f, 0.35f);
        }
        if (chestRewardSubText != null)
        {
            chestRewardSubText.text = LocalizationManager.T($"Tổng chiến lợi phẩm nhận được từ {openCount} rương:", $"Total rewards received from {openCount} chests:");
        }

        // 1. Shards Card
        string compactShards = FormatCompactNumber(totalShards);
        var shardsCard = CreateChestRewardCard(sprAethelShard, $"+{compactShards} {LocalizationManager.T("Linh Hồn", "Shards")}", $"[ {LocalizationManager.T("TỔNG LINH HỒN", "TOTAL SHARDS")} ]", new Color(0f, 0.85f, 1f));
        chestRewardCardObjs.Add(shardsCard);

        // 2. Bonus Dropped Cards
        foreach (var kvp in bonusDrops)
        {
            var bDef = InventoryItemDatabase.GetItem(kvp.Key);
            if (bDef != null)
            {
                Color rCol = InventoryItemDatabase.GetRarityColor(bDef.rarity);
                string itName = LocalizationManager.IsVietnamese ? bDef.nameVi : bDef.nameEn;
                var bonusCard = CreateChestRewardCard(bDef.Icon, $"{itName} x{kvp.Value}", $"[ {InventoryItemDatabase.GetRarityName(bDef.rarity)} ]", rCol);
                chestRewardCardObjs.Add(bonusCard);
            }
        }

        Canvas.ForceUpdateCanvases();
        if (chestRewardCardsContainer != null)
        {
            var sr = chestRewardCardsContainer.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.horizontalNormalizedPosition = 0f;
        }

        if (chestRewardConfirmBtn != null)
        {
            chestRewardConfirmBtn.gameObject.SetActive(true);
        }
    }

    private IEnumerator AnimateChestOpeningSequence(InventoryItemDatabase.ItemDefinition chestDef, int shardReward, InventoryItemDatabase.ItemDefinition bonusItemDef)
    {
        if (chestRewardModalObj == null) yield break;

        // Clear previous cards
        foreach (var c in chestRewardCardObjs)
        {
            if (c != null) Destroy(c);
        }
        chestRewardCardObjs.Clear();

        chestRewardModalObj.SetActive(true);
        chestRewardModalObj.transform.SetAsLastSibling();

        if (chestRewardConfirmBtn != null) chestRewardConfirmBtn.gameObject.SetActive(false);

        if (chestRewardTitleText != null)
        {
            chestRewardTitleText.text = LocalizationManager.T("ĐANG TRA CHÌA & MỞ KHÓA RƯƠNG...", "UNLOCKING ANCIENT CHEST...");
            chestRewardTitleText.color = new Color(1f, 0.75f, 0.25f);
        }
        if (chestRewardSubText != null)
        {
            chestRewardSubText.text = LocalizationManager.T("Tiêu hao: 1 Rương Tàn Tích + 1 Chìa Khóa Tàn Tích", "Consumed: 1 Ancient Chest + 1 Ancient Key");
        }

        if (chestRewardIconImg != null)
        {
            chestRewardIconImg.sprite = (sprChestClosed != null) ? sprChestClosed : chestDef.Icon;
            chestRewardIconImg.transform.localScale = Vector3.one;
        }

        // Shake chest animation for 0.55 seconds
        Vector3 origPos = (chestRewardIconImg != null) ? chestRewardIconImg.rectTransform.anchoredPosition : Vector2.zero;
        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (chestRewardIconImg != null)
            {
                float shakeOffset = Mathf.Sin(elapsed * 50f) * (7f * (elapsed / 0.55f));
                chestRewardIconImg.rectTransform.anchoredPosition = origPos + new Vector3(shakeOffset, 0f, 0f);
            }
            yield return null;
        }

        if (chestRewardIconImg != null) chestRewardIconImg.rectTransform.anchoredPosition = origPos;

        // Sound and Burst Open
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();

        if (chestRewardIconImg != null)
        {
            chestRewardIconImg.sprite = (sprChestOpen != null) ? sprChestOpen : chestDef.Icon;
        }

        // Scale burst animation
        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, 1.35f, elapsed / 0.25f);
            if (chestRewardIconImg != null) chestRewardIconImg.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.35f, 1.0f, elapsed / 0.2f);
            if (chestRewardIconImg != null) chestRewardIconImg.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        if (chestRewardTitleText != null)
        {
            chestRewardTitleText.text = LocalizationManager.T("MỞ RƯƠNG TÀN TÍCH THÀNH CÔNG!", "ANCIENT CHEST OPENED!");
            chestRewardTitleText.color = new Color(1f, 0.88f, 0.35f);
        }
        if (chestRewardSubText != null)
        {
            chestRewardSubText.text = LocalizationManager.T("Bạn đã nhận được các vật phẩm sau:", "You have acquired the following rewards:");
        }

        // 1. Shards Card
        var shardsCard = CreateChestRewardCard(sprAethelShard, $"+{shardReward} {LocalizationManager.T("Linh Hồn", "Shards")}", $"[ {LocalizationManager.T("LINH HỒN", "SHARDS")} ]", new Color(0f, 0.85f, 1f));
        chestRewardCardObjs.Add(shardsCard);

        // 2. Bonus Mineral Card
        if (bonusItemDef != null)
        {
            Color rCol = InventoryItemDatabase.GetRarityColor(bonusItemDef.rarity);
            string itName = LocalizationManager.IsVietnamese ? bonusItemDef.nameVi : bonusItemDef.nameEn;
            var bonusCard = CreateChestRewardCard(bonusItemDef.Icon, $"{itName} x1", $"[ {InventoryItemDatabase.GetRarityName(bonusItemDef.rarity)} ]", rCol);
            chestRewardCardObjs.Add(bonusCard);
        }

        Canvas.ForceUpdateCanvases();
        if (chestRewardCardsContainer != null)
        {
            var sr = chestRewardCardsContainer.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.horizontalNormalizedPosition = 0f;
        }

        if (chestRewardConfirmBtn != null)
        {
            chestRewardConfirmBtn.gameObject.SetActive(true);
        }
    }

    private GameObject CreateChestRewardCard(Sprite icon, string title, string tierBadge, Color outlineCol)
    {
        GameObject cardObj = new GameObject("RewardCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.layer = 5;
        cardObj.transform.SetParent(chestRewardCardsContainer, false);

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(175f, 95f);

        var le = cardObj.AddComponent<LayoutElement>();
        le.preferredWidth = 175f;
        le.preferredHeight = 95f;
        le.minWidth = 175f;
        le.minHeight = 95f;

        Image img = cardObj.GetComponent<Image>();
        img.color = new Color(0.10f, 0.12f, 0.18f, 0.95f);

        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = outlineCol;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Icon Box
        GameObject ibObj = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ibObj.layer = 5;
        ibObj.transform.SetParent(cardObj.transform, false);
        RectTransform ibRt = ibObj.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(8f, 0f);
        ibRt.sizeDelta = new Vector2(50f, 50f);

        Image ibImg = ibObj.GetComponent<Image>();
        if (sprItemSlotFrame != null) { ibImg.sprite = sprItemSlotFrame; ibImg.type = Image.Type.Sliced; }
        ibImg.color = outlineCol;

        GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icObj.layer = 5;
        icObj.transform.SetParent(ibObj.transform, false);
        RectTransform icRt = icObj.GetComponent<RectTransform>();
        icRt.anchorMin = Vector2.zero;
        icRt.anchorMax = Vector2.one;
        icRt.sizeDelta = new Vector2(-10f, -10f);
        Image icImg = icObj.GetComponent<Image>();
        icImg.sprite = icon;
        icImg.preserveAspect = true;

        // Details (Right)
        GameObject infoObj = new GameObject("Info", typeof(RectTransform), typeof(CanvasRenderer));
        infoObj.layer = 5;
        infoObj.transform.SetParent(cardObj.transform, false);
        RectTransform infoRt = infoObj.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0f, 0f);
        infoRt.anchorMax = new Vector2(1f, 1f);
        infoRt.offsetMin = new Vector2(64f, 6f);
        infoRt.offsetMax = new Vector2(-6f, -6f);

        var vlg = infoObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.spacing = 2f;

        // Tier
        GameObject tierObj = new GameObject("Tier", typeof(RectTransform), typeof(CanvasRenderer));
        tierObj.layer = 5;
        tierObj.transform.SetParent(infoObj.transform, false);
        var tTmp = tierObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = tierBadge;
        tTmp.fontSize = 10;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = outlineCol;

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.layer = 5;
        titleObj.transform.SetParent(infoObj.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = 11.5f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.overflowMode = TextOverflowModes.Ellipsis;

        return cardObj;
    }
}
