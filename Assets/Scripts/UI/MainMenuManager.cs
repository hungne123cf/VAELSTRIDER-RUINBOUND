using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject mapSelectPanel;
    [SerializeField] private GameObject rosterPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmationModal;

    [Header("Main Panel Controls")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button mapSelectButton;
    [SerializeField] private Button rosterButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button inventoryButton;

    [SerializeField] private TextMeshProUGUI playBtnText;
    [SerializeField] private TextMeshProUGUI mapSelectBtnText;
    [SerializeField] private TextMeshProUGUI rosterBtnText;
    [SerializeField] private TextMeshProUGUI skillBtnText;
    [SerializeField] private TextMeshProUGUI settingsBtnText;
    [SerializeField] private TextMeshProUGUI exitBtnText;
    [SerializeField] private TextMeshProUGUI logoutBtnText;
    [SerializeField] private TextMeshProUGUI shopBtnText;
    [SerializeField] private TextMeshProUGUI inventoryBtnText;

    [Header("Top Bar Display")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerTitleText;
    [SerializeField] private TextMeshProUGUI shardsTopText;
    [SerializeField] private TextMeshProUGUI potionsTopText;

    [Header("Main Screen Hero Preview")]
    [SerializeField] private Image heroPreviewImage;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI heroRoleText;
    [SerializeField] private TextMeshProUGUI heroStatsSummaryText;
    [SerializeField] private TextMeshProUGUI heroSkillText;
    [SerializeField] private TextMeshProUGUI heroSkillDescText;
    [SerializeField] private Sprite[] vaelIdleFrames;
    [SerializeField] private Sprite[] maidIdleFrames;
    [SerializeField] private Sprite[] roninIdleFrames;
    [SerializeField] private Sprite[] azureIdleFrames;
    [SerializeField] private Sprite[] titanIdleFrames;
    [SerializeField] private Sprite[] duneIdleFrames;
    [SerializeField] private Sprite[] mageIdleFrames;

    [Header("Map Select Panel")]
    [SerializeField] private TextMeshProUGUI mapPanelTitleText;
    [SerializeField] private Button map1Button;
    [SerializeField] private Button map2Button;
    [SerializeField] private Button map3Button;
    [SerializeField] private Button mapBackBtn;
    [SerializeField] private TextMeshProUGUI map1TitleText;
    [SerializeField] private TextMeshProUGUI map1DescText;
    [SerializeField] private TextMeshProUGUI map2TitleText;
    [SerializeField] private TextMeshProUGUI map2DescText;
    [SerializeField] private TextMeshProUGUI map3TitleText;
    [SerializeField] private TextMeshProUGUI map3DescText;
    [SerializeField] private TextMeshProUGUI mapBackBtnText;

    [Header("Roster Modal Elements")]
    [SerializeField] private TextMeshProUGUI rosterPanelTitleText;
    [SerializeField] private TextMeshProUGUI leftColHeaderText;
    [SerializeField] private Image rosterHeroPreviewImage;
    [SerializeField] private TextMeshProUGUI rosterHeroNameText;
    [SerializeField] private TextMeshProUGUI rosterHeroRoleText;
    [SerializeField] private TextMeshProUGUI rosterHeroStatsText;
    [SerializeField] private TextMeshProUGUI rosterHeroSkillTitleText;
    [SerializeField] private TextMeshProUGUI rosterHeroSkillDescText;

    [Header("Roster 3-Skill Breakdown Cards")]
    [SerializeField] private Image rosterSkill1IconImg;
    [SerializeField] private TextMeshProUGUI rosterSkill1TagText;
    [SerializeField] private TextMeshProUGUI rosterSkill1CdText;
    [SerializeField] private TextMeshProUGUI rosterSkill1TitleText;
    [SerializeField] private TextMeshProUGUI rosterSkill1DescText;

    [SerializeField] private Image rosterSkill2IconImg;
    [SerializeField] private TextMeshProUGUI rosterSkill2TagText;
    [SerializeField] private TextMeshProUGUI rosterSkill2CdText;
    [SerializeField] private TextMeshProUGUI rosterSkill2TitleText;
    [SerializeField] private TextMeshProUGUI rosterSkill2DescText;

    [SerializeField] private Image rosterSkill3IconImg;
    [SerializeField] private TextMeshProUGUI rosterSkill3TagText;
    [SerializeField] private TextMeshProUGUI rosterSkill3CdText;
    [SerializeField] private TextMeshProUGUI rosterSkill3TitleText;
    [SerializeField] private TextMeshProUGUI rosterSkill3DescText;

    [SerializeField] private Button rosterActionBtn;
    [SerializeField] private TextMeshProUGUI rosterActionBtnText;
    [SerializeField] private Button rosterBackBtn;
    [SerializeField] private TextMeshProUGUI rosterBackBtnText;

    [Header("Roster 6-Socket & Bag System")]
    private List<GameObject> rosterHeroSocketSlotObjs = new List<GameObject>();
    private List<Image> rosterSocketConduitLines = new List<Image>();
    private List<GameObject> rosterBagItemCardObjs = new List<GameObject>();
    [SerializeField] private TextMeshProUGUI rosterBagSectionHeaderText;
    [SerializeField] private Button rosterAutoMergeBtn;
    [SerializeField] private TextMeshProUGUI rosterAutoMergeBtnText;
    [SerializeField] private Transform rosterBagContainer;

    [Header("Roster Avatar Cards")]
    [SerializeField] private Button cardVaelBtn;
    [SerializeField] private Button cardMaidBtn;
    [SerializeField] private Button cardRoninBtn;
    [SerializeField] private Button cardAzureBtn;
    [SerializeField] private Button cardTitanBtn;
    [SerializeField] private Button cardDuneBtn;
    [SerializeField] private Button cardMageBtn;
    [SerializeField] private Image cardVaelAvatarImg;
    [SerializeField] private Image cardMaidAvatarImg;
    [SerializeField] private Image cardRoninAvatarImg;
    [SerializeField] private Image cardAzureAvatarImg;
    [SerializeField] private Image cardTitanAvatarImg;
    [SerializeField] private Image cardDuneAvatarImg;
    [SerializeField] private Image cardMageAvatarImg;
    [SerializeField] private TextMeshProUGUI cardVaelTitleText;
    [SerializeField] private TextMeshProUGUI cardVaelSubText;
    [SerializeField] private TextMeshProUGUI cardVaelStatusText;
    [SerializeField] private TextMeshProUGUI cardMaidTitleText;
    [SerializeField] private TextMeshProUGUI cardMaidSubText;
    [SerializeField] private TextMeshProUGUI cardMaidStatusText;
    [SerializeField] private TextMeshProUGUI cardRoninTitleText;
    [SerializeField] private TextMeshProUGUI cardRoninSubText;
    [SerializeField] private TextMeshProUGUI cardRoninStatusText;
    [SerializeField] private TextMeshProUGUI cardAzureTitleText;
    [SerializeField] private TextMeshProUGUI cardAzureSubText;
    [SerializeField] private TextMeshProUGUI cardAzureStatusText;
    [SerializeField] private TextMeshProUGUI cardTitanTitleText;
    [SerializeField] private TextMeshProUGUI cardTitanSubText;
    [SerializeField] private TextMeshProUGUI cardTitanStatusText;
    [SerializeField] private TextMeshProUGUI cardDuneTitleText;
    [SerializeField] private TextMeshProUGUI cardDuneSubText;
    [SerializeField] private TextMeshProUGUI cardDuneStatusText;
    [SerializeField] private TextMeshProUGUI cardMageTitleText;
    [SerializeField] private TextMeshProUGUI cardMageSubText;
    [SerializeField] private TextMeshProUGUI cardMageStatusText;
    [SerializeField] private TextMeshProUGUI cardPlaceholderText;

    // Hero Role Category Filter System
    public enum HeroRoleType { All = 0, Tank = 1, Warrior = 2, Marksman = 3, Assassin = 4, Mage = 5 }
    private HeroRoleType currentHeroRoleFilter = HeroRoleType.All;
    private List<Button> heroRoleTabButtons = new List<Button>();
    private List<Image> heroRoleTabBgList = new List<Image>();
    private List<TextMeshProUGUI> heroRoleTabTextList = new List<TextMeshProUGUI>();

    [Header("Currency / Info Display")]
    [SerializeField] private TextMeshProUGUI aethelShardsDisplay;

    [Header("Settings UI Reference")]
    [SerializeField] private SettingsUI settingsUI;

    private Coroutine mainHeroAnimCoroutine;
    private Coroutine rosterHeroAnimCoroutine;
    private int mainAnimFrame = 0;
    private int rosterAnimFrame = 0;
    private string inspectedHeroId = "azure";

    public static MainMenuManager EnsureInstance()
    {
        if (Instance == null)
        {
            Instance = Object.FindAnyObjectByType<MainMenuManager>();
        }
        return Instance;
    }

    private void Awake()
    {
        Instance = this;
        EnsureHeroSprites();
        EnsureInputSystemUIModule();
        EnsureDynamicBackground();
        EnsureBottomNavigationBar();
        AutoBindUIElements();
        HideCenterHeroDisplay();
        EnsureRosterPanel();
        InventoryUI.EnsureUIExists();
        ArmorShopUI.EnsureUIExists();
        SettingsUI.EnsureUIExists();
    }

    private void Start()
    {
        AccountDatabase.EnsureActiveSessionLoaded();
        EnsureHeroSprites();
        EnsureDynamicBackground();
        EnsureBottomNavigationBar();
        HideCenterHeroDisplay();
        EnsureRosterPanel();
        InventoryUI.EnsureUIExists();
        ArmorShopUI.EnsureUIExists();
        SettingsUI.EnsureUIExists();
        HookButtons();
        UpdateAllTexts();
        UpdateCurrencyDisplay();

        LocalizationManager.OnLanguageChanged += UpdateAllTexts;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= UpdateAllTexts;
    }

    public void RefreshHeroDisplay() => UpdateHeroDetails();
    public void RefreshCurrencies() => UpdateCurrencyDisplay();
    public void UpdateTopHUD() => UpdateCurrencyDisplay();
    public void RefreshAllTexts() => UpdateAllTexts();

    public void HideCenterHeroDisplay()
    {
        if (heroPreviewImage != null)
        {
            heroPreviewImage.gameObject.SetActive(false);
            if (heroPreviewImage.transform.parent != null && heroPreviewImage.transform.parent.name.ToLower().Contains("preview"))
            {
                heroPreviewImage.transform.parent.gameObject.SetActive(false);
            }
        }

        if (heroNameText != null) heroNameText.gameObject.SetActive(false);
        if (heroRoleText != null) heroRoleText.gameObject.SetActive(false);
        if (heroSkillText != null) heroSkillText.gameObject.SetActive(false);
        if (heroSkillDescText != null) heroSkillDescText.gameObject.SetActive(false);
        if (heroStatsSummaryText != null) heroStatsSummaryText.gameObject.SetActive(false);

        // Find HeroStage or center hero containers under Canvas or scene
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                string cName = child.name.ToLower();
                if (cName == "herostage" || cName == "heropreview" || cName == "characterpreview" || cName == "previewframe")
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
                else if (cName.Contains("topbar") || cName.Contains("bottomnavbar"))
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        if (mainPanel != null)
        {
            var pImg = mainPanel.GetComponent<Image>();
            if (pImg != null)
            {
                pImg.color = new Color(0f, 0f, 0f, 0f);
            }

            for (int i = 0; i < mainPanel.transform.childCount; i++)
            {
                var child = mainPanel.transform.GetChild(i);
                string cName = child.name.ToLower();

                // Preserve Top Bar and Bottom navigation buttons (hide potion display)
                if (cName.Contains("potion") || cName.Contains("binhmau"))
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                if (cName.Contains("logout"))
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                if (cName.Contains("top") || cName.Contains("header") || cName.Contains("bar") || cName.Contains("player") || cName.Contains("currency") || cName.Contains("shard"))
                {
                    child.gameObject.SetActive(true);
                    continue;
                }
                if (child.GetComponent<Button>() != null || cName.Contains("btn") || cName.Contains("button") || cName.Contains("nav"))
                {
                    child.gameObject.SetActive(true);
                    continue;
                }

                // Hide center hero visuals, boxes, and texts
                if (cName.Contains("hero") || cName.Contains("preview") || cName.Contains("skill") || cName.Contains("ultimate") || cName.Contains("stat") || cName.Contains("box") || cName.Contains("character") || cName.Contains("image"))
                {
                    var rt = child.GetComponent<RectTransform>();
                    if (rt != null && Mathf.Abs(rt.anchoredPosition.y) < 300f)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void EnsureDynamicBackground()
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            // Remove any old conflicting solid black backgrounds
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                if ((child.name == "Background" || child.name == "BG" || child.name == "SolidBg") && child.name != "DynamicBackgroundRoot")
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            MainMenuDynamicBackground.EnsureBackground(canvas);
        }
    }

    public void EnsureInputSystemUIModule()
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

    public void BuildMainMenuUI()
    {
        EnsureDynamicBackground();
        EnsureBottomNavigationBar();
        AutoBindUIElements();
        HideCenterHeroDisplay();
        EnsureRosterPanel();
        EnsureHeroSprites();
        InventoryUI.EnsureUIExists();
        ArmorShopUI.EnsureUIExists();
        SettingsUI.EnsureUIExists();
    }

    public void EnsureBottomNavigationBar()
    {
        // 1. Find existing buttons in the scene
        AutoBindUIElements();

        Transform navParent = null;
        if (playButton != null) navParent = playButton.transform.parent;
        else if (rosterButton != null) navParent = rosterButton.transform.parent;
        else if (shopButton != null) navParent = shopButton.transform.parent;
        else if (settingsButton != null) navParent = settingsButton.transform.parent;

        if (navParent == null)
        {
            if (mainPanel != null) navParent = mainPanel.transform;
            else
            {
                var p = GameObject.Find("MainPanel");
                if (p == null) p = GameObject.Find("Panel_Main");
                if (p != null) { mainPanel = p; navParent = p.transform; }
            }
        }

        if (navParent == null)
        {
            Canvas c = GameAutoBootstrap.GetMainGameplayCanvas();
            if (c == null) c = FindAnyObjectByType<Canvas>();
            if (c != null) navParent = c.transform;
        }

        if (navParent == null) return;

        // 2. Ensure 5 Navigation Buttons (Skills merged into Heroes):
        // [ XUẤT TRẬN / BATTLE ]  [ ANH HÙNG / HEROES ]  [ TÚI ĐỒ / INVENTORY ]  [ CỬA HÀNG / SHOP ]  [ CÀI ĐẶT / SETTINGS ]
        float btnWidth = 188f;
        float btnHeight = 54f;
        float posY = 50f;

        playButton = EnsureNavButton(navParent, playButton, "Btn_Play", LocalizationManager.Get("BTN_PLAY"), new Vector2(-400f, posY), new Vector2(btnWidth, btnHeight), new Color(0.85f, 0.55f, 0.15f, 1f), out playBtnText);
        rosterButton = EnsureNavButton(navParent, rosterButton, "Btn_Hero", LocalizationManager.Get("BTN_ROSTER"), new Vector2(-200f, posY), new Vector2(btnWidth, btnHeight), new Color(0.22f, 0.48f, 0.82f, 1f), out rosterBtnText);
        inventoryButton = EnsureNavButton(navParent, inventoryButton, "Btn_Inventory", LocalizationManager.Get("BTN_INVENTORY"), new Vector2(0f, posY), new Vector2(btnWidth, btnHeight), new Color(0.55f, 0.28f, 0.82f, 1f), out inventoryBtnText);
        shopButton = EnsureNavButton(navParent, shopButton, "Btn_Shop", LocalizationManager.Get("BTN_SHOP"), new Vector2(200f, posY), new Vector2(btnWidth, btnHeight), new Color(0.22f, 0.65f, 0.42f, 1f), out shopBtnText);
        settingsButton = EnsureNavButton(navParent, settingsButton, "Btn_Settings", LocalizationManager.Get("BTN_SETTINGS"), new Vector2(400f, posY), new Vector2(btnWidth, btnHeight), new Color(0.42f, 0.46f, 0.56f, 1f), out settingsBtnText);

        // Hide old standalone skill button if present in scene
        if (skillButton != null) skillButton.gameObject.SetActive(false);
        var oldSkillsBtn = navParent.Find("Btn_Skills");
        if (oldSkillsBtn != null) oldSkillsBtn.gameObject.SetActive(false);

        HookButtons();
        UpdateAllTexts();
    }

    private Button EnsureNavButton(Transform parent, Button existingBtn, string name, string defaultText, Vector2 pos, Vector2 size, Color bgColor, out TextMeshProUGUI btnTxt)
    {
        GameObject btnObj = null;
        if (existingBtn != null)
        {
            btnObj = existingBtn.gameObject;
        }
        else
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                btnObj = existing.gameObject;
            }
            else
            {
                btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnObj.layer = 5;
                btnObj.transform.SetParent(parent, false);
            }
        }

        btnObj.name = name;
        btnObj.SetActive(true);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (rt == null) rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        if (img == null) img = btnObj.AddComponent<Image>();
#if UNITY_EDITOR
        if (img.sprite == null)
        {
            var navSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/UI/Default/Panel/panel-001.png");
            if (navSpr != null)
            {
                img.sprite = navSpr;
                img.type = Image.Type.Sliced;
            }
        }
#endif
        img.color = bgColor;

        var outline = btnObj.GetComponent<Outline>();
        if (outline == null) outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.8f, 0.4f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null) btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        Transform txtT = btnObj.transform.Find("Text");
        GameObject txtObj;
        if (txtT != null)
        {
            txtObj = txtT.gameObject;
        }
        else
        {
            var tmpInChild = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpInChild != null)
            {
                txtObj = tmpInChild.gameObject;
            }
            else
            {
                txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
                txtObj.layer = 5;
                txtObj.transform.SetParent(btnObj.transform, false);
            }
        }

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        btnTxt = txtObj.GetComponent<TextMeshProUGUI>();
        if (btnTxt == null) btnTxt = txtObj.AddComponent<TextMeshProUGUI>();
        if (string.IsNullOrEmpty(btnTxt.text)) btnTxt.text = defaultText;
        btnTxt.fontSize = 18;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.white;
        btnTxt.raycastTarget = false;

        return btn;
    }

    private void AutoBindUIElements()
    {
        if (mainPanel == null)
        {
            var p = GameObject.Find("MainPanel");
            if (p == null) p = GameObject.Find("Panel_Main");
            if (p != null) mainPanel = p;
        }

        var allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            string bName = btn.name.ToLower();
            if (playButton == null && (bName.Contains("play") || bName.Contains("battle") || bName.Contains("xuat tran") || bName.Contains("btn_play") || bName.Contains("btn_battle")))
            {
                playButton = btn;
                playBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if (rosterButton == null && (bName == "btnhero" || bName == "btn_hero" || bName == "btn_roster" || bName.Contains("roster")))
            {
                rosterButton = btn;
                rosterBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if (skillButton == null && (bName == "btn_skills" || bName.Contains("skill") || bName.Contains("ky nang") || bName.Contains("kham")))
            {
                skillButton = btn;
                skillBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if (inventoryButton == null && (bName == "btn_inventory" || bName.Contains("inventory") || bName.Contains("tui do") || bName.Contains("bag") || bName.Contains("kho")))
            {
                inventoryButton = btn;
                inventoryBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if (shopButton == null && (bName == "btnshop" || bName == "btn_shop" || bName == "btn_store"))
            {
                shopButton = btn;
                shopBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if (settingsButton == null && (bName.Contains("setting") || bName.Contains("cai dat") || bName.Contains("btn_settings") || bName.Contains("gear")))
            {
                settingsButton = btn;
                settingsBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if (logoutButton == null && (bName.Contains("logout") || bName.Contains("dang xuat") || bName.Contains("btn_logout")))
            {
                logoutButton = btn;
                logoutBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                // Hide floating logout button from main screen (Moved cleanly inside Settings)
                btn.gameObject.SetActive(false);
            }
        }

        // Second pass: fallback với pattern rộng cho button nào vẫn chưa tìm được
        // Chỉ xét button đang active và không phải button close/back/reset
        if (rosterButton == null || shopButton == null)
        {
            foreach (var btn in allButtons)
            {
                if (btn == null || !btn.gameObject.activeInHierarchy) continue; // Chỉ xét button đang active
                string bName = !string.IsNullOrEmpty(btn.name) ? btn.name.ToLower() : "";
                // Bỏ qua button close, back, reset, x
                if (bName.Contains("close") || bName.Contains("back") || bName.Contains("reset") || bName == "x") continue;

                if (rosterButton == null && (bName.Contains("hero") || bName.Contains("nhan vat")))
                {
                    rosterButton = btn;
                    rosterBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                }
                else if (shopButton == null && (bName.Contains("shop") || bName.Contains("cua hang") || bName.Contains("store")))
                {
                    shopButton = btn;
                    shopBtnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
        }

        var allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in allTexts)
        {
            if (tmp == null) continue;
            string tName = !string.IsNullOrEmpty(tmp.name) ? tmp.name.ToLower() : "";
            string tText = !string.IsNullOrEmpty(tmp.text) ? tmp.text.ToLower() : "";
            if (playerTitleText == null && (tName.Contains("title") || tText.Contains("wanderer") || tText.Contains("lữ khách")))
            {
                playerTitleText = tmp;
            }
            if (shardsTopText == null && (tName.Contains("shard") || tText.Contains("linh hồn") || tName.Contains("coin")))
            {
                shardsTopText = tmp;
            }
            if (potionsTopText == null && (tName.Contains("potion") || tText.Contains("bình máu") || tText.Contains("binh mau")))
            {
                potionsTopText = tmp;
                potionsTopText.gameObject.SetActive(false);
            }
            if (heroSkillText == null && (tName.Contains("skill") || tText.Contains("tuyệt kỹ") || tText.Contains("ultimate")))
            {
                heroSkillText = tmp;
            }
            if (heroStatsSummaryText == null && (tText.Contains("hp:") || tText.Contains("atk:")))
            {
                heroStatsSummaryText = tmp;
            }
            if (heroNameText == null && (tText.Contains("hiệp sĩ") || tText.Contains("hầu gái") || tText.Contains("vael") || tText.Contains("elysia")))
            {
                heroNameText = tmp;
            }
        }

        var allImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var img in allImages)
        {
            if (img == null) continue;
            string imgName = !string.IsNullOrEmpty(img.name) ? img.name.ToLower() : "";
            if (heroPreviewImage == null && (imgName.Contains("preview") || imgName.Contains("heropreview") || imgName.Contains("character")))
            {
                heroPreviewImage = img;
            }
        }
    }

    public void EnsureMapSelectPanel()
    {
        if (mapSelectPanel != null) return;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var existing = canvas.transform.Find("MapSelectModalPanel");
        if (existing != null)
        {
            mapSelectPanel = existing.gameObject;
            return;
        }

        mapSelectPanel = new GameObject("MapSelectModalPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mapSelectPanel.layer = 5;
        mapSelectPanel.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = mapSelectPanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        Image overlayImg = mapSelectPanel.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        overlayImg.raycastTarget = true;

        GameObject dialogBox = new GameObject("MapSelectBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(mapSelectPanel.transform, false);

        RectTransform boxRt = dialogBox.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(980f, 540f);

        Image boxImg = dialogBox.GetComponent<Image>();
        boxImg.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);

        var outline = dialogBox.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.9f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        GameObject titleObj = new GameObject("MapPanelTitle", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(0f, 40f);

        mapPanelTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        mapPanelTitleText.fontSize = 28;
        mapPanelTitleText.fontStyle = FontStyles.Bold;
        mapPanelTitleText.alignment = TextAlignmentOptions.Center;
        mapPanelTitleText.color = new Color(1f, 0.85f, 0.4f);
        mapPanelTitleText.text = LocalizationManager.Get("BTN_MAP_SELECT");

        float cardWidth = 280f;
        float cardHeight = 360f;
        float spacing = 30f;
        float totalWidth = (3 * cardWidth) + (2 * spacing);
        float startX = -totalWidth / 2f + cardWidth / 2f;

        CreateMapCard(dialogBox.transform, new Vector2(startX, -30f), new Vector2(cardWidth, cardHeight),
            "MAP1_NAME", "MAP1_DESC", "Map1_Thestart", out map1TitleText, out map1DescText, out map1Button);

        CreateMapCard(dialogBox.transform, new Vector2(startX + cardWidth + spacing, -30f), new Vector2(cardWidth, cardHeight),
            "MAP2_NAME", "MAP2_DESC", "Map2_ValleyRuin", out map2TitleText, out map2DescText, out map2Button);

        CreateMapCard(dialogBox.transform, new Vector2(startX + 2 * (cardWidth + spacing), -30f), new Vector2(cardWidth, cardHeight),
            "MAP3_NAME", "MAP3_DESC", "Map3_ForgottenMemories", out map3TitleText, out map3DescText, out map3Button);

        GameObject backBtnObj = new GameObject("Btn_MapBack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(dialogBox.transform, false);
        RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0f);
        backRt.anchorMax = new Vector2(0.5f, 0f);
        backRt.pivot = new Vector2(0.5f, 0f);
        backRt.anchoredPosition = new Vector2(0f, 20f);
        backRt.sizeDelta = new Vector2(220f, 45f);

        Image backImg = backBtnObj.GetComponent<Image>();
        backImg.color = new Color(0.2f, 0.25f, 0.35f, 1f);

        var backOutline = backBtnObj.AddComponent<Outline>();
        backOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.8f);
        backOutline.effectDistance = new Vector2(1.5f, -1.5f);

        mapBackBtn = backBtnObj.GetComponent<Button>();
        mapBackBtn.targetGraphic = backImg;
        mapBackBtn.onClick.RemoveAllListeners();
        mapBackBtn.onClick.AddListener(ShowMainPanel);
        AttachPointerClick(backBtnObj, ShowMainPanel);

        GameObject backTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        backTxtObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform backTxtRt = backTxtObj.GetComponent<RectTransform>();
        backTxtRt.anchorMin = Vector2.zero;
        backTxtRt.anchorMax = Vector2.one;
        backTxtRt.sizeDelta = Vector2.zero;

        mapBackBtnText = backTxtObj.AddComponent<TextMeshProUGUI>();
        mapBackBtnText.fontSize = 20;
        mapBackBtnText.fontStyle = FontStyles.Bold;
        mapBackBtnText.alignment = TextAlignmentOptions.Center;
        mapBackBtnText.color = Color.white;
        mapBackBtnText.text = LocalizationManager.Get("BTN_BACK");
        mapBackBtnText.raycastTarget = false;

        mapSelectPanel.SetActive(false);
    }

    private void CreateMapCard(Transform parent, Vector2 pos, Vector2 size, string titleKey, string descKey, string sceneName, out TextMeshProUGUI titleTxt, out TextMeshProUGUI descTxt, out Button deployBtn)
    {
        GameObject card = new GameObject("Card_" + sceneName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = card.GetComponent<Image>();
        img.color = new Color(0.13f, 0.16f, 0.22f, 0.95f);

        var outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.7f, 0.6f, 0.3f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.transform.SetParent(card.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -15f);
        tRt.sizeDelta = new Vector2(-20f, 40f);

        titleTxt = tObj.AddComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = new Color(1f, 0.85f, 0.45f);
        titleTxt.text = LocalizationManager.Get(titleKey);
        titleTxt.raycastTarget = false;

        GameObject dObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer));
        dObj.transform.SetParent(card.transform, false);
        RectTransform dRt = dObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0.5f, 0.5f);
        dRt.anchoredPosition = new Vector2(0f, -10f);
        dRt.sizeDelta = new Vector2(-30f, -140f);

        descTxt = dObj.AddComponent<TextMeshProUGUI>();
        descTxt.fontSize = 15.5f;
        descTxt.lineSpacing = 2f;
        descTxt.alignment = TextAlignmentOptions.Center;
        descTxt.color = new Color(0.8f, 0.85f, 0.9f);
        descTxt.text = LocalizationManager.Get(descKey);
        descTxt.raycastTarget = false;

        GameObject bObj = new GameObject("Btn_Deploy", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        bObj.transform.SetParent(card.transform, false);
        RectTransform bRt = bObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 0f);
        bRt.anchorMax = new Vector2(0.5f, 0f);
        bRt.pivot = new Vector2(0.5f, 0f);
        bRt.anchoredPosition = new Vector2(0f, 20f);
        bRt.sizeDelta = new Vector2(210f, 46f);

        Image bImg = bObj.GetComponent<Image>();
        bImg.color = new Color(0.72f, 0.22f, 0.18f, 1f);

        var bOutline = bObj.AddComponent<Outline>();
        bOutline.effectColor = new Color(1f, 0.85f, 0.3f, 0.8f);
        bOutline.effectDistance = new Vector2(1.5f, -1.5f);

        deployBtn = bObj.GetComponent<Button>();
        deployBtn.targetGraphic = bImg;
        deployBtn.onClick.RemoveAllListeners();
        deployBtn.onClick.AddListener(() => LoadMap(sceneName));
        AttachPointerClick(bObj, () => LoadMap(sceneName));

        GameObject btObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        btObj.transform.SetParent(bObj.transform, false);
        RectTransform btRt = btObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.sizeDelta = Vector2.zero;

        var btTmp = btObj.AddComponent<TextMeshProUGUI>();
        btTmp.fontSize = 17.5f;
        btTmp.fontStyle = FontStyles.Bold;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = Color.white;
        btTmp.text = LocalizationManager.Get("BTN_PLAY");
        btTmp.raycastTarget = false;
    }

    private static Sprite sprRosterFrameBorder;

    private static Sprite LoadFramedSprite(string relativePath, Vector4 border)
    {
        Sprite spr = null;
#if UNITY_EDITOR
        string fullAssetPath = "Assets/ART/UI/Default/" + relativePath;
        var allObjs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fullAssetPath);
        if (allObjs != null)
        {
            foreach (var obj in allObjs)
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

    private static void EnsureRosterSprites()
    {
        if (sprRosterFrameBorder == null) sprRosterFrameBorder = LoadFramedSprite("Border/panel-border-014.png", new Vector4(8, 8, 8, 8));
        if (sprRosterFrameBorder == null) sprRosterFrameBorder = LoadFramedSprite("Border/panel-border-000.png", new Vector4(8, 8, 8, 8));
    }

    public void EnsureRosterPanel()
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        EnsureHeroSprites();
        EnsureRosterSprites();

        var existing = canvas.transform.Find("RosterModalPanel");
        if (existing != null)
        {
            Transform boxT = existing.Find("RosterBox");
            RectTransform boxRtExisting = boxT != null ? boxT.GetComponent<RectTransform>() : null;
            Transform sb = existing.Find("RosterBox/ShowcaseDetailsPanel/SkillBox");
            bool hasOldSkillFrame = sb != null && sb.GetComponent<Image>() != null && sb.GetComponent<Image>().sprite != null;
            Transform act = existing.Find("RosterBox/ShowcaseDetailsPanel/Btn_RosterAction");
            bool hasOldBtnFrame = act != null && act.GetComponent<Image>() != null && act.GetComponent<Image>().sprite != null;
            Transform oldMaidCard = existing.Find("RosterBox/HeroSelectionColumn/Card_Maid");
            bool hasOldMaid = oldMaidCard != null;

            Transform titanCardImg = existing.Find("RosterBox/HeroSelectionColumn/Card_titan/AvatarFrame/AvatarImg");
            bool hasMissingTitanSprite = titanCardImg == null || titanCardImg.GetComponent<Image>() == null || titanCardImg.GetComponent<Image>().sprite == null;

            Transform duneCard = existing.Find("RosterBox/HeroSelectionColumn/Card_dune");
            bool hasMissingDuneCard = duneCard == null;

            Transform skill1Card = existing.Find("RosterBox/ShowcaseDetailsPanel/SkillBox/SkillCard_0");
            bool hasMissingSkill1Card = skill1Card == null;
            bool hasAccidentalOverlayBtn = existing.GetComponent<Button>() != null;
            Transform tsTrans = existing.Find("RosterBox/ShowcaseDetailsPanel/TopHeroSection");
            bool hasOldTopHeight = tsTrans != null && tsTrans.GetComponent<RectTransform>() != null && tsTrans.GetComponent<RectTransform>().sizeDelta.y < 235f;

            if (boxRtExisting == null || boxRtExisting.anchorMax != Vector2.one || rosterHeroNameText == null || rosterSkill1TitleText == null || rosterSkill1CdText == null || hasOldSkillFrame || hasOldBtnFrame || hasOldMaid || hasMissingTitanSprite || hasMissingDuneCard || hasMissingSkill1Card || hasAccidentalOverlayBtn || hasOldTopHeight)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
                existing = null;
            }
            else
            {
                rosterPanel = existing.gameObject;
                return;
            }
        }

        // 1. Root Modal Overlay (Solid Raycast Blocker - No accidental background click closing)
        rosterPanel = new GameObject("RosterModalPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rosterPanel.layer = 5;
        rosterPanel.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = rosterPanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        Image overlayImg = rosterPanel.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        overlayImg.raycastTarget = true; // Blocks clicks from hitting main menu underneath

        // 2. Main Dialog Box - 100% Edge-to-Edge Full Screen
        GameObject dialogBox = new GameObject("RosterBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(rosterPanel.transform, false);

        RectTransform boxRt = dialogBox.GetComponent<RectTransform>();
        boxRt.anchorMin = Vector2.zero;
        boxRt.anchorMax = Vector2.one;
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.offsetMin = Vector2.zero;
        boxRt.offsetMax = Vector2.zero;

        Image boxImg = dialogBox.GetComponent<Image>();
        boxImg.color = new Color(0.04f, 0.05f, 0.09f, 1f);
        boxImg.raycastTarget = true;
        WhiteIconLoader.ApplyGothicFrame(dialogBox, new Color(0.85f, 0.72f, 0.38f, 0.90f), 0.35f);

        // 3. Header Title & Subtitle
        GameObject titleObj = new GameObject("RosterPanelTitle", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -25f);
        titleRt.sizeDelta = new Vector2(0f, 40f);

        rosterPanelTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        rosterPanelTitleText.fontSize = 28;
        rosterPanelTitleText.fontStyle = FontStyles.Bold;
        rosterPanelTitleText.alignment = TextAlignmentOptions.Center;
        rosterPanelTitleText.color = new Color(1f, 0.85f, 0.35f);
        rosterPanelTitleText.text = LocalizationManager.Get("BTN_ROSTER");
        rosterPanelTitleText.raycastTarget = false;

        GameObject subObj = new GameObject("RosterSubtitle", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.transform.SetParent(dialogBox.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -68f);
        subRt.sizeDelta = new Vector2(0f, 26f);

        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.fontSize = 15;
        subTmp.color = new Color(0.7f, 0.75f, 0.85f);
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.text = LocalizationManager.T("Chọn chiến binh xuất trận & Xem thông số kỹ năng anh hùng", "Select vanguard hero & inspect combat skill specifications");
        subTmp.raycastTarget = false;

        // Top-Right Close Button [X] (52 x 52)
        GameObject closeBtnObj = new GameObject("Btn_CloseTopRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(dialogBox.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-30f, -30f);
        closeRt.sizeDelta = new Vector2(52f, 52f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.75f, 0.22f, 0.25f, 1f);
        closeImg.raycastTarget = true;

        WhiteIconLoader.ApplyGothicFrame(closeBtnObj, new Color(1f, 0.85f, 0.4f, 0.95f), 0.5f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(CloseRosterPanel);
        WhiteIconLoader.ReplaceTextWithIcon(closeBtn, "Cross", Color.white, new Vector2(24f, 24f));

        rosterBackBtn = closeBtn;
        rosterBackBtnText = null;

        // 4. LEFT COLUMN: Hero Selection List (Full-Height Split Left Column)
        GameObject leftCol = new GameObject("HeroSelectionColumn", typeof(RectTransform));
        leftCol.transform.SetParent(dialogBox.transform, false);
        RectTransform lRt = leftCol.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(0.38f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.offsetMin = new Vector2(40f, 30f);
        lRt.offsetMax = new Vector2(-10f, -110f);

        GameObject lHeaderObj = new GameObject("ColHeader", typeof(RectTransform), typeof(CanvasRenderer));
        lHeaderObj.transform.SetParent(leftCol.transform, false);
        RectTransform lhRt = lHeaderObj.GetComponent<RectTransform>();
        lhRt.anchorMin = new Vector2(0f, 1f);
        lhRt.anchorMax = new Vector2(1f, 1f);
        lhRt.pivot = new Vector2(0f, 1f);
        lhRt.anchoredPosition = new Vector2(0f, 0f);
        lhRt.sizeDelta = new Vector2(0f, 26f);
        leftColHeaderText = lHeaderObj.AddComponent<TextMeshProUGUI>();
        leftColHeaderText.fontSize = 20;
        leftColHeaderText.fontStyle = FontStyles.Bold;
        leftColHeaderText.color = new Color(0.85f, 0.90f, 0.95f);
        leftColHeaderText.text = LocalizationManager.T("CHỌN ANH HÙNG", "HERO SELECTION");

        // Role Category Filter Tabs Bar (Horizontal Row)
        BuildHeroRoleTabs(leftCol.transform);

        EnsureHeroSprites();
        Sprite vaelAvatar = (vaelIdleFrames != null && vaelIdleFrames.Length > 0) ? vaelIdleFrames[0] : null;
        cardVaelBtn = CreateAvatarCard(leftCol.transform, "vael", new Vector2(0f, -78f), new Vector2(0f, 102f),
            vaelAvatar, false,
            out cardVaelAvatarImg, out cardVaelTitleText, out cardVaelSubText, out cardVaelStatusText);
        cardVaelBtn.onClick.AddListener(() => InspectHero("vael"));

        Sprite roninAvatar = (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames[0] : null;
        cardRoninBtn = CreateAvatarCard(leftCol.transform, "ronin", new Vector2(0f, -186f), new Vector2(0f, 102f),
            roninAvatar, false,
            out cardRoninAvatarImg, out cardRoninTitleText, out cardRoninSubText, out cardRoninStatusText);
        cardRoninBtn.onClick.AddListener(() => InspectHero("ronin"));
        cardMaidBtn = cardRoninBtn;
        cardMaidAvatarImg = cardRoninAvatarImg;
        cardMaidTitleText = cardRoninTitleText;
        cardMaidSubText = cardRoninSubText;
        cardMaidStatusText = cardRoninStatusText;

        Sprite azureAvatar = (azureIdleFrames != null && azureIdleFrames.Length > 0) ? azureIdleFrames[0] : null;
        cardAzureBtn = CreateAvatarCard(leftCol.transform, "azure", new Vector2(0f, -294f), new Vector2(0f, 102f),
            azureAvatar, false,
            out cardAzureAvatarImg, out cardAzureTitleText, out cardAzureSubText, out cardAzureStatusText);
        cardAzureBtn.onClick.AddListener(() => InspectHero("azure"));

        Sprite titanAvatar = (titanIdleFrames != null && titanIdleFrames.Length > 0) ? titanIdleFrames[0] : null;
        cardTitanBtn = CreateAvatarCard(leftCol.transform, "titan", new Vector2(0f, -402f), new Vector2(0f, 102f),
            titanAvatar, false,
            out cardTitanAvatarImg, out cardTitanTitleText, out cardTitanSubText, out cardTitanStatusText);
        cardTitanBtn.onClick.AddListener(() => InspectHero("titan"));

        Sprite duneAvatar = (duneIdleFrames != null && duneIdleFrames.Length > 0) ? duneIdleFrames[0] : null;
        cardDuneBtn = CreateAvatarCard(leftCol.transform, "dune", new Vector2(0f, -510f), new Vector2(0f, 102f),
            duneAvatar, false,
            out cardDuneAvatarImg, out cardDuneTitleText, out cardDuneSubText, out cardDuneStatusText);
        cardDuneBtn.onClick.AddListener(() => InspectHero("dune"));

        Sprite mageAvatar = (mageIdleFrames != null && mageIdleFrames.Length > 0) ? mageIdleFrames[0] : null;
        cardMageBtn = CreateAvatarCard(leftCol.transform, "mage", new Vector2(0f, -618f), new Vector2(0f, 102f),
            mageAvatar, false,
            out cardMageAvatarImg, out cardMageTitleText, out cardMageSubText, out cardMageStatusText);
        cardMageBtn.onClick.AddListener(() => InspectHero("mage"));

        ApplyHeroRoleFilter(currentHeroRoleFilter);

        // 5. RIGHT COLUMN: Showcase Details Panel (Full-Height Split Right Column)
        GameObject rightCol = new GameObject("ShowcaseDetailsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightCol.transform.SetParent(dialogBox.transform, false);
        RectTransform rRt = rightCol.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.38f, 0f);
        rRt.anchorMax = new Vector2(1f, 1f);
        rRt.pivot = new Vector2(1f, 1f);
        rRt.offsetMin = new Vector2(15f, 25f);
        rRt.offsetMax = new Vector2(-30f, -100f);

        Image rcImg = rightCol.GetComponent<Image>();
        rcImg.color = new Color(0.06f, 0.08f, 0.13f, 0.98f);
        WhiteIconLoader.ApplyGothicFrame(rightCol, new Color(0.35f, 0.45f, 0.60f, 0.65f), 0.50f);

        // TOP SECTION: HERO STAGE (Avatar + 6 Sockets) & STATS (Expanded to 240px for vertical stats)
        GameObject topSection = new GameObject("TopHeroSection", typeof(RectTransform));
        topSection.transform.SetParent(rightCol.transform, false);
        RectTransform tsRt = topSection.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 1f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.pivot = new Vector2(0.5f, 1f);
        tsRt.anchoredPosition = new Vector2(0f, -10f);
        tsRt.sizeDelta = new Vector2(-24f, 240f);

        // Hero Center Stage (Avatar + 6 Sockets)
        GameObject stageBox = new GameObject("HeroStageBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        stageBox.transform.SetParent(topSection.transform, false);
        RectTransform sbRt = stageBox.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0.5f);
        sbRt.anchorMax = new Vector2(0f, 0.5f);
        sbRt.pivot = new Vector2(0f, 0.5f);
        sbRt.anchoredPosition = new Vector2(6f, 0f);
        sbRt.sizeDelta = new Vector2(350f, 225f);

        Image sbImg = stageBox.GetComponent<Image>();
        sbImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(stageBox, new Color(0.85f, 0.72f, 0.38f, 0.70f), 0.45f);

        // 6 Socket Slot Positions spread out with clean vertical spacing (no overlap)
        Vector2[] socketPositions = new Vector2[]
        {
            new Vector2(-130f, 75f),   // Slot 0: Vũ Khí (Top-Left)
            new Vector2(-136f, 0f),    // Slot 1: Áo Giáp (Mid-Left)
            new Vector2(-130f, -75f),  // Slot 2: Giày (Bot-Left)
            new Vector2(130f, 75f),    // Slot 3: Mũ Nón (Top-Right)
            new Vector2(136f, 0f),     // Slot 4: Nhẫn (Mid-Right)
            new Vector2(130f, -75f)    // Slot 5: Hộ Phù (Bot-Right)
        };

        // 6 Glowing Conduit Lines connecting 6 sockets to center Avatar (rendered underneath)
        rosterSocketConduitLines.Clear();
        for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT; s++)
        {
            Vector2 sPos = socketPositions[s];
            Vector2 mid = sPos * 0.5f;
            float dist = sPos.magnitude;
            float angle = Mathf.Atan2(sPos.y, sPos.x) * Mathf.Rad2Deg;

            GameObject lineObj = new GameObject($"ConduitLine_{s}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObj.layer = 5;
            lineObj.transform.SetParent(stageBox.transform, false);
            RectTransform lineRt = lineObj.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);
            lineRt.anchoredPosition = mid;
            lineRt.sizeDelta = new Vector2(dist, 2.5f);
            lineRt.localRotation = Quaternion.Euler(0f, 0f, angle);

            Image lineImg = lineObj.GetComponent<Image>();
            lineImg.color = new Color(0.85f, 0.72f, 0.35f, 0.30f);
            lineImg.raycastTarget = false;
            rosterSocketConduitLines.Add(lineImg);
        }

        // Center Avatar Box (Clean seamless container, no extra solid box or outline behind hero)
        GameObject animBox = new GameObject("ShowcaseAnimBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
        animBox.transform.SetParent(stageBox.transform, false);
        RectTransform aRt = animBox.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.5f, 0.5f);
        aRt.anchorMax = new Vector2(0.5f, 0.5f);
        aRt.pivot = new Vector2(0.5f, 0.5f);
        aRt.anchoredPosition = Vector2.zero;
        aRt.sizeDelta = new Vector2(120f, 180f);

        GameObject animImgObj = new GameObject("HeroLiveSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        animImgObj.transform.SetParent(animBox.transform, false);
        RectTransform aiRt = animImgObj.GetComponent<RectTransform>();
        aiRt.anchorMin = Vector2.zero;
        aiRt.anchorMax = Vector2.one;
        aiRt.sizeDelta = Vector2.zero;
        rosterHeroPreviewImage = animImgObj.GetComponent<Image>();
        rosterHeroPreviewImage.preserveAspect = true;
        rosterHeroPreviewImage.transform.localScale = GetHeroVisualScale("vael");

        rosterHeroSocketSlotObjs.Clear();
        for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT; s++)
        {
            int slotIdx = s;
            GameObject sObj = new GameObject($"RosterSlot_{slotIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            sObj.layer = 5;
            sObj.transform.SetParent(stageBox.transform, false);
            rosterHeroSocketSlotObjs.Add(sObj);

            RectTransform sRt = sObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0.5f);
            sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = socketPositions[slotIdx];
            sRt.sizeDelta = new Vector2(58f, 58f);

            Image sImg = sObj.GetComponent<Image>();
            sImg.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            var sOutline = sObj.AddComponent<Outline>();
            sOutline.effectColor = new Color(0.40f, 0.50f, 0.65f, 0.70f);
            sOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var dropTarget = sObj.AddComponent<UIDropSocketTarget>();
            dropTarget.socketSlotIndex = slotIdx;
            dropTarget.onGemDropped = (droppedGemId, targetSlot) =>
            {
                if (EquipmentSocketManager.SocketGemToHeroSlot(targetSlot, droppedGemId, out _, out _, inspectedHeroId))
                {
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    InspectHero(inspectedHeroId);
                }
            };
        }

        // Info & Stats Panel beside StageBox
        GameObject infoGroup = new GameObject("InfoGroup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        infoGroup.transform.SetParent(topSection.transform, false);
        RectTransform igRt = infoGroup.GetComponent<RectTransform>();
        igRt.anchorMin = Vector2.zero;
        igRt.anchorMax = Vector2.one;
        igRt.offsetMin = new Vector2(362f, 0f);
        igRt.offsetMax = new Vector2(-6f, 0f);

        Image igImg = infoGroup.GetComponent<Image>();
        igImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(infoGroup, new Color(0.35f, 0.45f, 0.60f, 0.60f), 0.45f);

        GameObject nameObj = new GameObject("RosterHeroName", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.transform.SetParent(infoGroup.transform, false);
        RectTransform nRt = nameObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(12f, -6f);
        nRt.sizeDelta = new Vector2(-24f, 26f);
        rosterHeroNameText = nameObj.AddComponent<TextMeshProUGUI>();
        rosterHeroNameText.fontSize = 24f;
        rosterHeroNameText.fontStyle = FontStyles.Bold;
        rosterHeroNameText.color = new Color(1f, 0.88f, 0.45f);

        GameObject roleObj = new GameObject("RosterHeroRole", typeof(RectTransform), typeof(CanvasRenderer));
        roleObj.transform.SetParent(infoGroup.transform, false);
        RectTransform rlRt = roleObj.GetComponent<RectTransform>();
        rlRt.anchorMin = new Vector2(0f, 1f);
        rlRt.anchorMax = new Vector2(1f, 1f);
        rlRt.pivot = new Vector2(0f, 1f);
        rlRt.anchoredPosition = new Vector2(12f, -32f);
        rlRt.sizeDelta = new Vector2(-24f, 20f);
        rosterHeroRoleText = roleObj.AddComponent<TextMeshProUGUI>();
        rosterHeroRoleText.fontSize = 15.5f;
        rosterHeroRoleText.color = new Color(0.6f, 0.85f, 1f);

        GameObject statsObj = new GameObject("RosterHeroStats", typeof(RectTransform), typeof(CanvasRenderer));
        statsObj.transform.SetParent(infoGroup.transform, false);
        RectTransform stRt = statsObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 1f);
        stRt.anchorMax = new Vector2(1f, 1f);
        stRt.pivot = new Vector2(0f, 1f);
        stRt.anchoredPosition = new Vector2(12f, -54f);
        stRt.sizeDelta = new Vector2(-24f, 138f);
        rosterHeroStatsText = statsObj.AddComponent<TextMeshProUGUI>();
        rosterHeroStatsText.fontSize = 15f;
        rosterHeroStatsText.lineSpacing = 1.15f;
        rosterHeroStatsText.fontStyle = FontStyles.Bold;
        rosterHeroStatsText.color = new Color(0.95f, 0.95f, 0.95f);

        // Action Buttons Row inside Top Section
        GameObject btnRow = new GameObject("ActionBtnRow", typeof(RectTransform));
        btnRow.transform.SetParent(infoGroup.transform, false);
        RectTransform brRt = btnRow.GetComponent<RectTransform>();
        brRt.anchorMin = new Vector2(0f, 0f);
        brRt.anchorMax = new Vector2(1f, 0f);
        brRt.pivot = new Vector2(0.5f, 0f);
        brRt.anchoredPosition = new Vector2(0f, 6f);
        brRt.sizeDelta = new Vector2(-20f, 38f);

        // 1. Costume Wardrobe Button (Left)
        GameObject cosBtnObj = new GameObject("Btn_RosterCostume", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cosBtnObj.transform.SetParent(btnRow.transform, false);
        RectTransform cosRt = cosBtnObj.GetComponent<RectTransform>();
        cosRt.anchorMin = new Vector2(0f, 0f);
        cosRt.anchorMax = new Vector2(0.35f, 1f);
        cosRt.offsetMin = Vector2.zero;
        cosRt.offsetMax = new Vector2(-4f, 0f);

        Image cosImg = cosBtnObj.GetComponent<Image>();
        cosImg.color = new Color(0.65f, 0.40f, 0.12f, 1f);
        WhiteIconLoader.ApplyGothicFrame(cosBtnObj, new Color(1f, 0.85f, 0.35f, 0.85f), 0.35f);

        var cosBtn = cosBtnObj.GetComponent<Button>();
        cosBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            CostumeWardrobeUI.EnsureUIExists()?.Open(inspectedHeroId);
        });

        GameObject cosTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        cosTxtObj.transform.SetParent(cosBtnObj.transform, false);
        RectTransform cosTxtRt = cosTxtObj.GetComponent<RectTransform>();
        cosTxtRt.anchorMin = Vector2.zero;
        cosTxtRt.anchorMax = Vector2.one;
        cosTxtRt.sizeDelta = Vector2.zero;
        var cosTxt = cosTxtObj.AddComponent<TextMeshProUGUI>();
        cosTxt.text = LocalizationManager.T("TRANG PHỤC", "WARDROBE");
        cosTxt.fontSize = 16.5f;
        cosTxt.fontStyle = FontStyles.Bold;
        cosTxt.alignment = TextAlignmentOptions.Center;
        cosTxt.color = Color.white;
        cosTxt.raycastTarget = false;

        // 2. Main Select Battle Hero Button (Right)
        GameObject actBtnObj = new GameObject("Btn_RosterAction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actBtnObj.transform.SetParent(btnRow.transform, false);
        RectTransform actRt = actBtnObj.GetComponent<RectTransform>();
        actRt.anchorMin = new Vector2(0.35f, 0f);
        actRt.anchorMax = new Vector2(1f, 1f);
        actRt.offsetMin = new Vector2(4f, 0f);
        actRt.offsetMax = Vector2.zero;

        Image actImg = actBtnObj.GetComponent<Image>();
        actImg.color = new Color(0.16f, 0.58f, 0.28f, 1f);
        WhiteIconLoader.ApplyGothicFrame(actBtnObj, new Color(0.95f, 0.82f, 0.35f, 0.85f), 0.40f);

        rosterActionBtn = actBtnObj.GetComponent<Button>();
        rosterActionBtn.targetGraphic = actImg;
        rosterActionBtn.onClick.RemoveAllListeners();
        rosterActionBtn.onClick.AddListener(OnRosterActionClicked);

        GameObject actTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        actTxtObj.transform.SetParent(actBtnObj.transform, false);
        RectTransform actTxtRt = actTxtObj.GetComponent<RectTransform>();
        actTxtRt.anchorMin = Vector2.zero;
        actTxtRt.anchorMax = Vector2.one;
        actTxtRt.sizeDelta = Vector2.zero;
        rosterActionBtnText = actTxtObj.AddComponent<TextMeshProUGUI>();
        rosterActionBtnText.fontSize = 16.5f;
        rosterActionBtnText.fontStyle = FontStyles.Bold;
        rosterActionBtnText.alignment = TextAlignmentOptions.Center;
        rosterActionBtnText.color = Color.white;
        rosterActionBtnText.raycastTarget = false;

        // MIDDLE SECTION: 3 COMBAT SKILL CARDS (Adjusted cleanly to not overlap topSection or bagBox)
        GameObject skillBox = new GameObject("SkillBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        skillBox.transform.SetParent(rightCol.transform, false);
        RectTransform skRt = skillBox.GetComponent<RectTransform>();
        skRt.anchorMin = new Vector2(0f, 0f);
        skRt.anchorMax = new Vector2(1f, 1f);
        skRt.pivot = new Vector2(0.5f, 1f);
        skRt.offsetMin = new Vector2(15f, 166f);
        skRt.offsetMax = new Vector2(-15f, -255f);
        Image skImg = skillBox.GetComponent<Image>();
        skImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
        WhiteIconLoader.ApplyGothicFrame(skillBox, new Color(0.35f, 0.45f, 0.60f, 0.60f), 0.50f);

        GameObject skTitleObj = new GameObject("SkillTitle", typeof(RectTransform), typeof(CanvasRenderer));
        skTitleObj.transform.SetParent(skillBox.transform, false);
        RectTransform sktRt = skTitleObj.GetComponent<RectTransform>();
        sktRt.anchorMin = new Vector2(0f, 1f);
        sktRt.anchorMax = new Vector2(1f, 1f);
        sktRt.pivot = new Vector2(0.5f, 1f);
        sktRt.anchoredPosition = new Vector2(12f, -5f);
        sktRt.sizeDelta = new Vector2(-24f, 22f);
        rosterHeroSkillTitleText = skTitleObj.AddComponent<TextMeshProUGUI>();
        rosterHeroSkillTitleText.fontSize = 16.5f;
        rosterHeroSkillTitleText.fontStyle = FontStyles.Bold;
        rosterHeroSkillTitleText.color = new Color(1f, 0.85f, 0.35f);

        // 3 Skill Breakdown Cards (Attack, Dash, Ultimate)
        CreateSkillRowCard(skillBox.transform, 0, -28f, 66f,
            out rosterSkill1IconImg, out rosterSkill1TagText, out rosterSkill1CdText, out rosterSkill1TitleText, out rosterSkill1DescText);
        CreateSkillRowCard(skillBox.transform, 1, -96f, 66f,
            out rosterSkill2IconImg, out rosterSkill2TagText, out rosterSkill2CdText, out rosterSkill2TitleText, out rosterSkill2DescText);
        CreateSkillRowCard(skillBox.transform, 2, -164f, 70f,
            out rosterSkill3IconImg, out rosterSkill3TagText, out rosterSkill3CdText, out rosterSkill3TitleText, out rosterSkill3DescText);
        rosterHeroSkillDescText = rosterSkill1DescText;

        // BOTTOM SECTION: BAG ITEMS & MINERALS/GEMS (Direct Drag & Drop to 6 Slots) - ENLARGED FOR MOBILE
        GameObject bagBox = new GameObject("RosterBagBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bagBox.transform.SetParent(rightCol.transform, false);
        RectTransform bbRt = bagBox.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0f, 0f);
        bbRt.anchorMax = new Vector2(1f, 0f);
        bbRt.pivot = new Vector2(0.5f, 0f);
        bbRt.anchoredPosition = new Vector2(0f, 10f);
        bbRt.sizeDelta = new Vector2(-30f, 150f);

        Image bbImg = bagBox.GetComponent<Image>();
        bbImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(bagBox, new Color(0.35f, 0.45f, 0.60f, 0.60f), 0.45f);

        // Bag Header Bar (Aligned with padding and right-margin for AutoMerge button)
        GameObject bhObj = new GameObject("BagHeader", typeof(RectTransform), typeof(CanvasRenderer));
        bhObj.transform.SetParent(bagBox.transform, false);
        RectTransform bhRt = bhObj.GetComponent<RectTransform>();
        bhRt.anchorMin = new Vector2(0f, 1f);
        bhRt.anchorMax = new Vector2(1f, 1f);
        bhRt.pivot = new Vector2(0f, 1f);
        bhRt.anchoredPosition = new Vector2(12f, -8f);
        bhRt.sizeDelta = new Vector2(-235f, 26f);

        rosterBagSectionHeaderText = bhObj.AddComponent<TextMeshProUGUI>();
        rosterBagSectionHeaderText.fontSize = 15f;
        rosterBagSectionHeaderText.fontStyle = FontStyles.Bold;
        rosterBagSectionHeaderText.color = new Color(0.85f, 0.90f, 1f);
        rosterBagSectionHeaderText.enableWordWrapping = false;
        rosterBagSectionHeaderText.overflowMode = TextOverflowModes.Ellipsis;
        rosterBagSectionHeaderText.text = LocalizationManager.T("VẬT PHẨM & TRANG BỊ TRONG TÚI (KÉO VÀO 6 Ô TRÊN ĐỂ TRANG BỊ)", "ITEMS & GEAR IN BAG (DRAG INTO THE 6 SLOTS TO EQUIP)");

        // Auto Merge Button (Cleanly nested inside BagBox with proper padding from border and header text)
        GameObject ambObj = new GameObject("Btn_AutoMerge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        ambObj.transform.SetParent(bagBox.transform, false);
        RectTransform ambRt = ambObj.GetComponent<RectTransform>();
        ambRt.anchorMin = new Vector2(1f, 1f);
        ambRt.anchorMax = new Vector2(1f, 1f);
        ambRt.pivot = new Vector2(1f, 1f);
        ambRt.anchoredPosition = new Vector2(-12f, -6f);
        ambRt.sizeDelta = new Vector2(205f, 28f);

        Image ambImg = ambObj.GetComponent<Image>();
        ambImg.color = new Color(0.72f, 0.45f, 0.12f, 1f);
        WhiteIconLoader.ApplyGothicFrame(ambObj, new Color(1f, 0.85f, 0.40f, 0.85f), 0.35f);

        rosterAutoMergeBtn = ambObj.GetComponent<Button>();
        rosterAutoMergeBtn.onClick.AddListener(() =>
        {
            int gained = EquipmentSocketManager.AutoMergeAllMatchingGems(out int upgradedSlots, out int levelsGained, inspectedHeroId);
            if (gained > 0)
            {
                SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                InspectHero(inspectedHeroId);
            }
        });

        GameObject amTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        amTxtObj.transform.SetParent(ambObj.transform, false);
        RectTransform amtRt = amTxtObj.GetComponent<RectTransform>();
        amtRt.anchorMin = Vector2.zero;
        amtRt.anchorMax = Vector2.one;
        amtRt.sizeDelta = Vector2.zero;
        rosterAutoMergeBtnText = amTxtObj.AddComponent<TextMeshProUGUI>();
        rosterAutoMergeBtnText.fontSize = 15.5f;
        rosterAutoMergeBtnText.fontStyle = FontStyles.Bold;
        rosterAutoMergeBtnText.alignment = TextAlignmentOptions.Center;
        rosterAutoMergeBtnText.color = Color.white;
        rosterAutoMergeBtnText.text = LocalizationManager.T("HẤP THU TOÀN BỘ", "AUTO MERGE ALL");

        // Bag Scroll Container (Mobile-Optimized Smooth Scrolling)
        GameObject scrollObj = new GameObject("BagScrollRect", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(bagBox.transform, false);
        RectTransform scrRt = scrollObj.GetComponent<RectTransform>();
        scrRt.anchorMin = new Vector2(0f, 0f);
        scrRt.anchorMax = new Vector2(1f, 1f);
        scrRt.offsetMin = new Vector2(38f, 8f);
        scrRt.offsetMax = new Vector2(-38f, -32f);

        ScrollRect sRect = scrollObj.GetComponent<ScrollRect>();
        sRect.horizontal = true;
        sRect.vertical = false;
        sRect.movementType = ScrollRect.MovementType.Elastic;
        sRect.elasticity = 0.1f;
        sRect.inertia = true;
        sRect.decelerationRate = 0.135f;
        sRect.scrollSensitivity = 35f;

        // Left Navigation Arrow Button for Mobile
        GameObject leftBtnObj = new GameObject("Btn_ScrollLeft", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        leftBtnObj.transform.SetParent(bagBox.transform, false);
        RectTransform lbRt = leftBtnObj.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(0f, 0.5f);
        lbRt.anchorMax = new Vector2(0f, 0.5f);
        lbRt.pivot = new Vector2(0f, 0.5f);
        lbRt.anchoredPosition = new Vector2(4f, -14f);
        lbRt.sizeDelta = new Vector2(30f, 100f);
        Image lbImg = leftBtnObj.GetComponent<Image>();
        lbImg.color = new Color(0.10f, 0.14f, 0.22f, 0.92f);
        WhiteIconLoader.ApplyGothicFrame(leftBtnObj, new Color(0.45f, 0.55f, 0.70f, 0.70f), 0.35f);
        var lbBtn = leftBtnObj.GetComponent<Button>();
        lbBtn.onClick.AddListener(() =>
        {
            sRect.horizontalNormalizedPosition = Mathf.Clamp01(sRect.horizontalNormalizedPosition - 0.25f);
            SoundManager.EnsureInstance()?.PlayMenuHover();
        });
        GameObject ltObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        ltObj.transform.SetParent(leftBtnObj.transform, false);
        RectTransform ltRt = ltObj.GetComponent<RectTransform>();
        ltRt.anchorMin = Vector2.zero; ltRt.anchorMax = Vector2.one; ltRt.sizeDelta = Vector2.zero;
        var ltTmp = ltObj.AddComponent<TextMeshProUGUI>();
        ltTmp.text = "<";
        ltTmp.fontSize = 20f;
        ltTmp.fontStyle = FontStyles.Bold;
        ltTmp.alignment = TextAlignmentOptions.Center;
        ltTmp.color = new Color(0.85f, 0.90f, 1f);

        // Right Navigation Arrow Button for Mobile
        GameObject rightBtnObj = new GameObject("Btn_ScrollRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        rightBtnObj.transform.SetParent(bagBox.transform, false);
        RectTransform rbRt = rightBtnObj.GetComponent<RectTransform>();
        rbRt.anchorMin = new Vector2(1f, 0.5f);
        rbRt.anchorMax = new Vector2(1f, 0.5f);
        rbRt.pivot = new Vector2(1f, 0.5f);
        rbRt.anchoredPosition = new Vector2(-4f, -14f);
        rbRt.sizeDelta = new Vector2(30f, 100f);
        Image rbImg = rightBtnObj.GetComponent<Image>();
        rbImg.color = new Color(0.10f, 0.14f, 0.22f, 0.92f);
        WhiteIconLoader.ApplyGothicFrame(rightBtnObj, new Color(0.45f, 0.55f, 0.70f, 0.70f), 0.35f);
        var rbBtn = rightBtnObj.GetComponent<Button>();
        rbBtn.onClick.AddListener(() =>
        {
            sRect.horizontalNormalizedPosition = Mathf.Clamp01(sRect.horizontalNormalizedPosition + 0.25f);
            SoundManager.EnsureInstance()?.PlayMenuHover();
        });
        GameObject rtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        rtObj.transform.SetParent(rightBtnObj.transform, false);
        RectTransform rtRt = rtObj.GetComponent<RectTransform>();
        rtRt.anchorMin = Vector2.zero; rtRt.anchorMax = Vector2.one; rtRt.sizeDelta = Vector2.zero;
        var rtTmp = rtObj.AddComponent<TextMeshProUGUI>();
        rtTmp.text = ">";
        rtTmp.fontSize = 20f;
        rtTmp.fontStyle = FontStyles.Bold;
        rtTmp.alignment = TextAlignmentOptions.Center;
        rtTmp.color = new Color(0.85f, 0.90f, 1f);

        GameObject contentObj = new GameObject("BagContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contRt = contentObj.GetComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0f, 0.5f);
        contRt.anchorMax = new Vector2(0f, 0.5f);
        contRt.pivot = new Vector2(0f, 0.5f);
        contRt.anchoredPosition = Vector2.zero;
        contRt.sizeDelta = new Vector2(0f, 105f);

        var hlg = contentObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 10f;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        sRect.content = contRt;
        rosterBagContainer = contRt;

        rosterPanel.SetActive(false);
    }

    public void CloseRosterPanel()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (rosterPanel != null) rosterPanel.SetActive(false);

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            var r = canvas.transform.Find("RosterModalPanel");
            if (r != null) r.gameObject.SetActive(false);
            var hr = canvas.transform.Find("HeroRosterModal");
            if (hr != null) hr.gameObject.SetActive(false);
        }

        if (mainPanel != null) mainPanel.SetActive(true);
        HideCenterHeroDisplay();
        UpdateCurrencyDisplay();
    }

    public static Vector3 GetHeroVisualScale(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return new Vector3(2.35f, 2.35f, 1f);
        string id = heroId.ToLower();
        if (id == "vael" || id == "vaelstrider")
        {
            return new Vector3(2.35f, 2.35f, 1f);
        }
        else if (id == "ronin" || id == "galeronin" || id == "player2" || id == "maid" || id == "elysia")
        {
            return new Vector3(2.45f, 2.45f, 1f);
        }
        else if (id == "azure" || id == "azureblade" || id == "player3")
        {
            return new Vector3(2.75f, 2.75f, 1f);
        }
        else if (id == "titan" || id == "titanberserker" || id == "player4")
        {
            return new Vector3(2.35f, 2.35f, 1f);
        }
        else if (id == "dune" || id == "duneassassin" || id == "player5")
        {
            return new Vector3(1.0f, 1.0f, 1f);
        }
        else if (id == "mage" || id == "celestialmage" || id == "player6")
        {
            return new Vector3(1.05f, 1.05f, 1f);
        }
        return new Vector3(2.0f, 2.0f, 1f);
    }

    private static float[] GetHeroBaseCooldowns(string heroId)
    {
        string h = (heroId ?? "").ToLower();
        if (h.Contains("mage") || h.Contains("celestial") || h.Contains("player6"))
            return new float[] { 0.75f, 4.0f, 15.0f };
        if (h.Contains("dune") || h.Contains("player5"))
            return new float[] { 0f, 3.5f, 13.0f };
        if (h.Contains("titan") || h.Contains("player4"))
            return new float[] { 5.0f, 4.5f, 24.0f };
        if (h.Contains("azure") || h.Contains("player3"))
            return new float[] { 5.0f, 4.5f, 18.0f };
        if (h.Contains("ronin") || h.Contains("gale") || h.Contains("maid") || h.Contains("elysia"))
            return new float[] { 0f, 4.0f, 15.0f };
        // Default (Knight Vaelstrider / Warrior)
        return new float[] { 5.0f, 5.0f, 24.0f };
    }

    private GameObject CreateSkillRowCard(Transform parent, int skillIndex, float yOffset, float height,
        out Image iconImg, out TextMeshProUGUI tagTxt, out TextMeshProUGUI cdTxt, out TextMeshProUGUI titleTxt, out TextMeshProUGUI descTxt)
    {
        GameObject card = new GameObject($"SkillCard_{skillIndex}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform cRt = card.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = new Vector2(0f, yOffset);
        cRt.sizeDelta = new Vector2(-20f, height);

        Image cImg = card.GetComponent<Image>();
        cImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
        Color outlineCol = (skillIndex == 0) ? new Color(1f, 0.85f, 0.35f, 0.65f)
                         : (skillIndex == 1 ? new Color(0.35f, 0.90f, 0.65f, 0.65f) : new Color(0.95f, 0.45f, 0.45f, 0.65f));
        WhiteIconLoader.ApplyGothicFrame(card, outlineCol, 0.50f);

        // Icon Badge Container (48x48) with Gothic frame
        GameObject iconBadge = new GameObject("IconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconBadge.transform.SetParent(card.transform, false);
        RectTransform ibRt = iconBadge.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(10f, 0f);
        ibRt.sizeDelta = new Vector2(48f, 48f);

        Image ibImg = iconBadge.GetComponent<Image>();
        ibImg.color = new Color(0.07f, 0.10f, 0.16f, 1f);
        WhiteIconLoader.ApplyGothicFrame(iconBadge, outlineCol, 0.35f);

        // Icon Image inside badge
        GameObject iconInner = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconInner.transform.SetParent(iconBadge.transform, false);
        RectTransform iiRt = iconInner.GetComponent<RectTransform>();
        iiRt.anchorMin = Vector2.zero;
        iiRt.anchorMax = Vector2.one;
        iiRt.sizeDelta = new Vector2(-6f, -6f);
        iconImg = iconInner.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Nhóm Header góc phải của chiêu: Tag Badge + Cooldown Badge
        float badgeW = 75f;
        float cdW = 80f;
        float topGroupW = badgeW + cdW + 4f;

        GameObject topGroup = new GameObject("TopRightGroup", typeof(RectTransform));
        topGroup.transform.SetParent(card.transform, false);
        RectTransform trRt = topGroup.GetComponent<RectTransform>();
        trRt.anchorMin = new Vector2(1f, 1f);
        trRt.anchorMax = new Vector2(1f, 1f);
        trRt.pivot = new Vector2(1f, 1f);
        trRt.anchoredPosition = new Vector2(-10f, -4f);
        trRt.sizeDelta = new Vector2(topGroupW, 22f);

        // 1. Tag badge
        GameObject tagBadge = new GameObject("TagBadge", typeof(RectTransform));
        tagBadge.transform.SetParent(topGroup.transform, false);
        RectTransform tRt = tagBadge.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.5f);
        tRt.anchorMax = new Vector2(0f, 0.5f);
        tRt.pivot = new Vector2(0f, 0.5f);
        tRt.anchoredPosition = Vector2.zero;
        tRt.sizeDelta = new Vector2(badgeW, 22f);

        Sprite bracketSpr = WhiteIconLoader.GetGothicBracketSprite();

        // Left Gothic bracket [
        GameObject leftB = new GameObject("LeftBracket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        leftB.transform.SetParent(tagBadge.transform, false);
        RectTransform lbRt = leftB.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(0f, 0.5f);
        lbRt.anchorMax = new Vector2(0f, 0.5f);
        lbRt.pivot = new Vector2(0.5f, 0.5f);
        lbRt.anchoredPosition = new Vector2(7f, 0f);
        lbRt.sizeDelta = new Vector2(14f, 16f);
        lbRt.localScale = new Vector3(-1f, 1f, 1f);
        Image lbImg = leftB.GetComponent<Image>();
        lbImg.sprite = bracketSpr;
        lbImg.color = outlineCol;
        lbImg.preserveAspect = true;
        lbImg.raycastTarget = false;

        // Tag text in center (unobstructed)
        GameObject tagObj = new GameObject("TagText", typeof(RectTransform), typeof(CanvasRenderer));
        tagObj.transform.SetParent(tagBadge.transform, false);
        RectTransform ttRt2 = tagObj.GetComponent<RectTransform>();
        ttRt2.anchorMin = Vector2.zero;
        ttRt2.anchorMax = Vector2.one;
        ttRt2.offsetMin = new Vector2(12f, 0f);
        ttRt2.offsetMax = new Vector2(-12f, 0f);
        tagTxt = tagObj.AddComponent<TextMeshProUGUI>();
        tagTxt.fontSize = 13f;
        tagTxt.fontStyle = FontStyles.Bold;
        tagTxt.alignment = TextAlignmentOptions.Center;
        tagTxt.color = outlineCol;
        tagTxt.enableWordWrapping = false;
        tagTxt.raycastTarget = false;

        // Right Gothic bracket ]
        GameObject rightB = new GameObject("RightBracket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightB.transform.SetParent(tagBadge.transform, false);
        RectTransform rbRt = rightB.GetComponent<RectTransform>();
        rbRt.anchorMin = new Vector2(1f, 0.5f);
        rbRt.anchorMax = new Vector2(1f, 0.5f);
        rbRt.pivot = new Vector2(0.5f, 0.5f);
        rbRt.anchoredPosition = new Vector2(-7f, 0f);
        rbRt.sizeDelta = new Vector2(14f, 16f);
        rbRt.localScale = new Vector3(1f, 1f, 1f);
        Image rbImg = rightB.GetComponent<Image>();
        rbImg.sprite = bracketSpr;
        rbImg.color = outlineCol;
        rbImg.preserveAspect = true;
        rbImg.raycastTarget = false;

        // 2. Cooldown Badge ở góc phải
        GameObject cdBadge = new GameObject("CooldownBadge", typeof(RectTransform));
        cdBadge.transform.SetParent(topGroup.transform, false);
        RectTransform cdRt = cdBadge.GetComponent<RectTransform>();
        cdRt.anchorMin = new Vector2(1f, 0.5f);
        cdRt.anchorMax = new Vector2(1f, 0.5f);
        cdRt.pivot = new Vector2(1f, 0.5f);
        cdRt.anchoredPosition = Vector2.zero;
        cdRt.sizeDelta = new Vector2(cdW, 22f);

        GameObject cdObj = new GameObject("CDText", typeof(RectTransform), typeof(CanvasRenderer));
        cdObj.transform.SetParent(cdBadge.transform, false);
        RectTransform cdtRt = cdObj.GetComponent<RectTransform>();
        cdtRt.anchorMin = Vector2.zero;
        cdtRt.anchorMax = Vector2.one;
        cdtRt.sizeDelta = Vector2.zero;
        cdTxt = cdObj.AddComponent<TextMeshProUGUI>();
        cdTxt.fontSize = 13.5f;
        cdTxt.fontStyle = FontStyles.Bold;
        cdTxt.alignment = TextAlignmentOptions.MidlineRight;
        cdTxt.enableWordWrapping = false;
        cdTxt.raycastTarget = false;
        cdTxt.text = "<color=#94A3B8><b>CD: 0s</b></color>";

        // Text Group (Right side, dừng trước topGroup)
        GameObject textGroup = new GameObject("TextGroup", typeof(RectTransform));
        textGroup.transform.SetParent(card.transform, false);
        RectTransform tgRt = textGroup.GetComponent<RectTransform>();
        tgRt.anchorMin = Vector2.zero;
        tgRt.anchorMax = Vector2.one;
        tgRt.offsetMin = new Vector2(68f, 3f);
        tgRt.offsetMax = new Vector2(-(topGroupW + 16f), -3f);

        // Title text
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(textGroup.transform, false);
        RectTransform ttRt = titleObj.GetComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0f, 1f);
        ttRt.anchorMax = new Vector2(1f, 1f);
        ttRt.pivot = new Vector2(0f, 1f);
        ttRt.anchoredPosition = new Vector2(0f, -2f);
        ttRt.sizeDelta = new Vector2(0f, 24f);
        titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 17.5f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Left;
        titleTxt.enableWordWrapping = false;
        titleTxt.overflowMode = TextOverflowModes.Ellipsis;
        titleTxt.raycastTarget = false;

        // Desc text
        GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer));
        descObj.transform.SetParent(textGroup.transform, false);
        RectTransform dtRt = descObj.GetComponent<RectTransform>();
        dtRt.anchorMin = Vector2.zero;
        dtRt.anchorMax = Vector2.one;
        dtRt.offsetMin = new Vector2(0f, 2f);
        dtRt.offsetMax = new Vector2(0f, -24f);
        descTxt = descObj.AddComponent<TextMeshProUGUI>();
        descTxt.fontSize = 15f;
        descTxt.lineSpacing = 2.5f;
        descTxt.enableWordWrapping = true;
        descTxt.overflowMode = TextOverflowModes.Ellipsis;
        descTxt.color = new Color(0.85f, 0.88f, 0.92f);
        descTxt.raycastTarget = false;

        return card;
    }

    private void SetHeroSkillCard(int cardIdx, Sprite icon, string tag, string title, string desc, float baseCd = 0f, float finalCd = 0f, float upDashCd = 0f, float upUltiCd = 0f)
    {
        Color tagCol = Color.white;
        string upperTag = string.IsNullOrEmpty(tag) ? "" : tag.ToUpper();
        if (upperTag.Contains("AP") || upperTag.Contains("PHÉP") || upperTag.Contains("MAGIC")) tagCol = new Color(0.72f, 0.35f, 1f); // Tím AP
        else if (upperTag.Contains("TD") || upperTag.Contains("CHUẨN") || upperTag.Contains("TRUE")) tagCol = new Color(0.92f, 0.95f, 1f); // Trắng Bạc TD
        else if (upperTag.Contains("ATK") || upperTag.Contains("VẬT LÝ") || upperTag.Contains("PHYS") || upperTag.Contains("V.LÝ")) tagCol = new Color(1f, 0.23f, 0.23f); // Đỏ ATK
        else if (upperTag.Contains("LƯỚT") || upperTag.Contains("DASH") || upperTag.Contains("TỐC BIẾN") || upperTag.Contains("BLINK") || upperTag.Contains("WARP") || upperTag.Contains("ẢO ẢNH")) tagCol = new Color(0.35f, 0.90f, 0.65f); // Xanh Ngọc Cơ Động
        else tagCol = new Color(1f, 0.85f, 0.35f);

        string cleanTag = string.IsNullOrEmpty(tag) ? "" : tag.Trim('[', ']', ' ');

        // Tính toán Cooldown động cập nhật theo ngọc khảm và nâng cấp
        float effCd = 0f;
        if (baseCd > 0.05f)
        {
            float clampedCdr = Mathf.Clamp(finalCd, 0f, 0.60f);
            if (cardIdx == 0) effCd = Mathf.Max(0.5f, baseCd * (1f - clampedCdr));
            else if (cardIdx == 1) effCd = Mathf.Max(1.0f, (baseCd * (1f - clampedCdr)) - upDashCd);
            else effCd = Mathf.Max(3.0f, (baseCd * (1f - clampedCdr)) - upUltiCd);
        }
        string cdStr = (effCd > 0f) ? $"<color=#38BDF8><b>CD: {effCd:0.#}s</b></color>" : "<color=#94A3B8><b>CD: 0s</b></color>";

        if (cardIdx == 0)
        {
            if (rosterSkill1IconImg != null)
            {
                rosterSkill1IconImg.sprite = icon;
                rosterSkill1IconImg.color = (icon != null) ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
            if (rosterSkill1TagText != null) 
            { 
                rosterSkill1TagText.text = cleanTag; 
                rosterSkill1TagText.color = tagCol;
                var imgs = rosterSkill1TagText.transform.parent.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs) img.color = tagCol;
            }
            if (rosterSkill1CdText != null) rosterSkill1CdText.text = cdStr;
            if (rosterSkill1TitleText != null) rosterSkill1TitleText.text = title;
            if (rosterSkill1DescText != null) rosterSkill1DescText.text = desc;
        }
        else if (cardIdx == 1)
        {
            if (rosterSkill2IconImg != null)
            {
                rosterSkill2IconImg.sprite = icon;
                rosterSkill2IconImg.color = (icon != null) ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
            if (rosterSkill2TagText != null) 
            { 
                rosterSkill2TagText.text = cleanTag; 
                rosterSkill2TagText.color = tagCol;
                var imgs = rosterSkill2TagText.transform.parent.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs) img.color = tagCol;
            }
            if (rosterSkill2CdText != null) rosterSkill2CdText.text = cdStr;
            if (rosterSkill2TitleText != null) rosterSkill2TitleText.text = title;
            if (rosterSkill2DescText != null) rosterSkill2DescText.text = desc;
        }
        else if (cardIdx == 2)
        {
            if (rosterSkill3IconImg != null)
            {
                rosterSkill3IconImg.sprite = icon;
                rosterSkill3IconImg.color = (icon != null) ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
            if (rosterSkill3TagText != null) 
            { 
                rosterSkill3TagText.text = cleanTag; 
                rosterSkill3TagText.color = tagCol;
                var imgs = rosterSkill3TagText.transform.parent.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs) img.color = tagCol;
            }
            if (rosterSkill3CdText != null) rosterSkill3CdText.text = cdStr;
            if (rosterSkill3TitleText != null) rosterSkill3TitleText.text = title;
            if (rosterSkill3DescText != null) rosterSkill3DescText.text = desc;
        }
    }

    private Button CreateAvatarCard(Transform parent, string heroId, Vector2 pos, Vector2 size, Sprite avatarSprite, bool isLocked, out Image avatarImgRef, out TextMeshProUGUI titleTxtRef, out TextMeshProUGUI subTxtRef, out TextMeshProUGUI statusTxtRef)
    {
        GameObject card = new GameObject("Card_" + heroId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = card.GetComponent<Image>();
        img.color = isLocked ? new Color(0.06f, 0.07f, 0.11f, 0.90f) : new Color(0.06f, 0.08f, 0.13f, 0.95f);

        Color borderCol = isLocked ? new Color(0.40f, 0.40f, 0.45f, 0.5f) : new Color(0.85f, 0.72f, 0.35f, 0.85f);
        WhiteIconLoader.ApplyGothicFrame(card, borderCol, 0.65f);

        Button btn = card.GetComponent<Button>();
        btn.targetGraphic = img;

        // Avatar frame with clean vector outline & RectMask2D to neatly clip scaled sprites
        GameObject avBox = new GameObject("AvatarFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        avBox.transform.SetParent(card.transform, false);
        RectTransform avRt = avBox.GetComponent<RectTransform>();
        avRt.anchorMin = new Vector2(0f, 0.5f);
        avRt.anchorMax = new Vector2(0f, 0.5f);
        avRt.pivot = new Vector2(0f, 0.5f);
        avRt.anchoredPosition = new Vector2(15f, 0f);
        avRt.sizeDelta = new Vector2(90f, 90f);

        Image avbImg = avBox.GetComponent<Image>();
        avbImg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
        var avOutline = avBox.AddComponent<Outline>();
        avOutline.effectColor = isLocked ? new Color(0.40f, 0.40f, 0.45f, 0.6f) : new Color(0.85f, 0.72f, 0.35f, 0.85f);
        avOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject avImgObj = new GameObject("AvatarImg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        avImgObj.transform.SetParent(avBox.transform, false);
        RectTransform aiRt = avImgObj.GetComponent<RectTransform>();
        aiRt.anchorMin = Vector2.zero;
        aiRt.anchorMax = Vector2.one;
        aiRt.sizeDelta = Vector2.zero;

        avatarImgRef = avImgObj.GetComponent<Image>();
        avatarImgRef.sprite = avatarSprite;
        avatarImgRef.preserveAspect = true;
        avatarImgRef.transform.localScale = GetHeroVisualScale(heroId);
        string hid = heroId.ToLower();
        if (hid == "titan" || hid == "titanberserker" || hid == "player4")
        {
            aiRt.anchoredPosition = new Vector2(0f, 8f);
        }
        else if (hid == "ronin" || hid == "galeronin" || hid == "player2" || hid == "maid" || hid == "elysia")
        {
            aiRt.anchoredPosition = new Vector2(0f, 4f);
        }
        else if (hid == "vael" || hid == "vaelstrider")
        {
            aiRt.anchoredPosition = new Vector2(0f, 8f);
        }
        else if (hid == "azure" || hid == "azureblade" || hid == "player3")
        {
            aiRt.anchoredPosition = new Vector2(0f, 4f);
        }
        else if (hid == "dune" || hid == "duneassassin" || hid == "player5")
        {
            aiRt.anchoredPosition = new Vector2(0f, 0f);
        }
        else if (hid == "mage" || hid == "celestialmage" || hid == "player6")
        {
            aiRt.anchoredPosition = new Vector2(0f, 0f);
        }
        if (isLocked) avatarImgRef.color = new Color(0.5f, 0.5f, 0.55f, 0.6f);

        if (isLocked)
        {
            GameObject lockOverlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lockOverlay.transform.SetParent(avBox.transform, false);
            RectTransform lkRt = lockOverlay.GetComponent<RectTransform>();
            lkRt.anchorMin = Vector2.zero;
            lkRt.anchorMax = Vector2.one;
            lkRt.sizeDelta = Vector2.zero;
            lockOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            GameObject lockIcon = new GameObject("LockLabelText", typeof(RectTransform), typeof(CanvasRenderer));
            lockIcon.transform.SetParent(lockOverlay.transform, false);
            RectTransform liRt = lockIcon.GetComponent<RectTransform>();
            liRt.anchorMin = Vector2.zero;
            liRt.anchorMax = Vector2.one;
            liRt.sizeDelta = Vector2.zero;
            var liTmp = lockIcon.AddComponent<TextMeshProUGUI>();
            liTmp.text = LocalizationManager.T("KHÓA", "LOCKED");
            liTmp.fontSize = 15f;
            liTmp.fontStyle = FontStyles.Bold;
            liTmp.alignment = TextAlignmentOptions.Center;
            liTmp.color = new Color(1f, 0.85f, 0.4f);
            liTmp.raycastTarget = false;
        }

        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.transform.SetParent(card.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(120f, -14f);
        tRt.sizeDelta = new Vector2(-130f, 26f);
        titleTxtRef = tObj.AddComponent<TextMeshProUGUI>();
        titleTxtRef.fontSize = 18.5f;
        titleTxtRef.fontStyle = FontStyles.Bold;
        titleTxtRef.color = isLocked ? new Color(0.75f, 0.75f, 0.8f) : new Color(1f, 0.88f, 0.45f);
        titleTxtRef.raycastTarget = false;

        GameObject sObj = new GameObject("Sub", typeof(RectTransform), typeof(CanvasRenderer));
        sObj.transform.SetParent(card.transform, false);
        RectTransform sRt = sObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0f, 1f);
        sRt.anchoredPosition = new Vector2(120f, -44f);
        sRt.sizeDelta = new Vector2(-130f, 24f);
        subTxtRef = sObj.AddComponent<TextMeshProUGUI>();
        subTxtRef.fontSize = 15.5f;
        subTxtRef.color = new Color(0.6f, 0.85f, 1f);
        subTxtRef.raycastTarget = false;

        GameObject stObj = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer));
        stObj.transform.SetParent(card.transform, false);
        RectTransform stRt = stObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(1f, 0f);
        stRt.pivot = new Vector2(0f, 0f);
        stRt.anchoredPosition = new Vector2(120f, 14f);
        stRt.sizeDelta = new Vector2(-130f, 24f);
        statusTxtRef = stObj.AddComponent<TextMeshProUGUI>();
        statusTxtRef.fontSize = 15.5f;
        statusTxtRef.fontStyle = FontStyles.Bold;
        statusTxtRef.raycastTarget = false;

        // Role Badge Tag (Top-Right of card)
        GameObject badgeObj = new GameObject("RoleBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObj.transform.SetParent(card.transform, false);
        RectTransform bRt = badgeObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 1f);
        bRt.anchorMax = new Vector2(1f, 1f);
        bRt.pivot = new Vector2(1f, 1f);
        bRt.anchoredPosition = new Vector2(-12f, -10f);
        bRt.sizeDelta = new Vector2(96f, 24f);

        Image bImg = badgeObj.GetComponent<Image>();
        string bTextVi, bTextEn;
        Color bBgCol, bTextCol;
        GetHeroRoleBadgeInfo(heroId, out bTextVi, out bTextEn, out bBgCol, out bTextCol);
        bImg.color = bBgCol;

        var bOutline = badgeObj.AddComponent<Outline>();
        bOutline.effectColor = bTextCol;
        bOutline.effectDistance = new Vector2(1f, -1f);

        GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        bTxtObj.transform.SetParent(badgeObj.transform, false);
        RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.sizeDelta = Vector2.zero;

        var bTmp = bTxtObj.AddComponent<TextMeshProUGUI>();
        bTmp.text = LocalizationManager.T(bTextVi, bTextEn);
        bTmp.fontSize = 12.5f;
        bTmp.fontStyle = FontStyles.Bold;
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.color = bTextCol;
        bTmp.raycastTarget = false;

        return btn;
    }

    private void BuildHeroRoleTabs(Transform parent)
    {
        heroRoleTabButtons.Clear();
        heroRoleTabBgList.Clear();
        heroRoleTabTextList.Clear();

        GameObject barObj = new GameObject("HeroRoleTabBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        barObj.transform.SetParent(parent, false);
        RectTransform barRt = barObj.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -34f);
        barRt.sizeDelta = new Vector2(0f, 36f);

        Image barBg = barObj.GetComponent<Image>();
        barBg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);

        var barOutline = barObj.AddComponent<Outline>();
        barOutline.effectColor = new Color(0.25f, 0.35f, 0.5f, 0.5f);
        barOutline.effectDistance = new Vector2(1f, -1f);

        var hlg = barObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(4, 4, 3, 3);

        string[] tabNamesVi = new string[] { "TẤT CẢ", "ĐỠ ĐÒN", "ĐẤU SĨ", "XẠ THỦ", "SÁT THỦ", "PHÁP SƯ" };
        string[] tabNamesEn = new string[] { "ALL", "TANK", "WARRIOR", "MARKSMAN", "ASSASSIN", "MAGE" };
        HeroRoleType[] tabRoles = new HeroRoleType[] { HeroRoleType.All, HeroRoleType.Tank, HeroRoleType.Warrior, HeroRoleType.Marksman, HeroRoleType.Assassin, HeroRoleType.Mage };

        for (int i = 0; i < tabRoles.Length; i++)
        {
            int idx = i;
            HeroRoleType rType = tabRoles[i];

            GameObject tObj = new GameObject($"RoleTab_{rType}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tObj.transform.SetParent(barObj.transform, false);

            Image tImg = tObj.GetComponent<Image>();
            tImg.color = (rType == currentHeroRoleFilter) ? new Color(0.18f, 0.45f, 0.75f, 1f) : new Color(0.08f, 0.11f, 0.17f, 0.95f);

            var tOutline = tObj.AddComponent<Outline>();
            tOutline.effectColor = (rType == currentHeroRoleFilter) ? new Color(1f, 0.85f, 0.35f, 0.9f) : new Color(0.3f, 0.4f, 0.55f, 0.4f);
            tOutline.effectDistance = new Vector2(1f, -1f);

            Button tBtn = tObj.GetComponent<Button>();
            tBtn.targetGraphic = tImg;
            tBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                ApplyHeroRoleFilter(rType);
            });

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            txtObj.transform.SetParent(tObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = LocalizationManager.T(tabNamesVi[i], tabNamesEn[i]);
            tmp.fontSize = 13f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = (rType == currentHeroRoleFilter) ? Color.white : new Color(0.7f, 0.75f, 0.85f);
            tmp.raycastTarget = false;

            heroRoleTabButtons.Add(tBtn);
            heroRoleTabBgList.Add(tImg);
            heroRoleTabTextList.Add(tmp);
        }
    }

    private void UpdateHeroRoleTabVisuals()
    {
        HeroRoleType[] tabRoles = new HeroRoleType[] { HeroRoleType.All, HeroRoleType.Tank, HeroRoleType.Warrior, HeroRoleType.Marksman, HeroRoleType.Assassin, HeroRoleType.Mage };
        string[] tabNamesVi = new string[] { "TẤT CẢ", "ĐỠ ĐÒN", "ĐẤU SĨ", "XẠ THỦ", "SÁT THỦ", "PHÁP SƯ" };
        string[] tabNamesEn = new string[] { "ALL", "TANK", "WARRIOR", "MARKSMAN", "ASSASSIN", "MAGE" };

        for (int i = 0; i < heroRoleTabButtons.Count && i < tabRoles.Length; i++)
        {
            bool isSelected = (tabRoles[i] == currentHeroRoleFilter);
            if (i < heroRoleTabBgList.Count && heroRoleTabBgList[i] != null)
            {
                heroRoleTabBgList[i].color = isSelected ? new Color(0.20f, 0.48f, 0.82f, 1f) : new Color(0.08f, 0.11f, 0.17f, 0.95f);
                var outl = heroRoleTabBgList[i].GetComponent<Outline>();
                if (outl != null)
                {
                    outl.effectColor = isSelected ? new Color(1f, 0.85f, 0.35f, 0.95f) : new Color(0.3f, 0.4f, 0.55f, 0.4f);
                }
            }
            if (i < heroRoleTabTextList.Count && heroRoleTabTextList[i] != null)
            {
                heroRoleTabTextList[i].text = LocalizationManager.T(tabNamesVi[i], tabNamesEn[i]);
                heroRoleTabTextList[i].color = isSelected ? new Color(1f, 0.95f, 0.6f) : new Color(0.7f, 0.75f, 0.85f);
            }
        }
    }

    public void ApplyHeroRoleFilter(HeroRoleType role)
    {
        currentHeroRoleFilter = role;
        UpdateHeroRoleTabVisuals();

        float startY = -78f;
        float cardHeight = 102f;
        float spacing = 6f;
        int visibleIndex = 0;

        // Vael (Warrior)
        bool showVael = (role == HeroRoleType.All || role == HeroRoleType.Warrior);
        if (cardVaelBtn != null)
        {
            cardVaelBtn.gameObject.SetActive(showVael);
            if (showVael)
            {
                RectTransform rt = cardVaelBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, startY - (visibleIndex * (cardHeight + spacing)));
                visibleIndex++;
            }
        }

        // Ronin (Assassin)
        bool showRonin = (role == HeroRoleType.All || role == HeroRoleType.Assassin);
        if (cardRoninBtn != null)
        {
            cardRoninBtn.gameObject.SetActive(showRonin);
            if (showRonin)
            {
                RectTransform rt = cardRoninBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, startY - (visibleIndex * (cardHeight + spacing)));
                visibleIndex++;
            }
        }

        // Azure (Marksman)
        bool showAzure = (role == HeroRoleType.All || role == HeroRoleType.Marksman);
        if (cardAzureBtn != null)
        {
            cardAzureBtn.gameObject.SetActive(showAzure);
            if (showAzure)
            {
                RectTransform rt = cardAzureBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, startY - (visibleIndex * (cardHeight + spacing)));
                visibleIndex++;
            }
        }

        // Titan (Tank)
        bool showTitan = (role == HeroRoleType.All || role == HeroRoleType.Tank);
        if (cardTitanBtn != null)
        {
            cardTitanBtn.gameObject.SetActive(showTitan);
            if (showTitan)
            {
                RectTransform rt = cardTitanBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, startY - (visibleIndex * (cardHeight + spacing)));
                visibleIndex++;
            }
        }

        // Dune (Assassin)
        bool showDune = (role == HeroRoleType.All || role == HeroRoleType.Assassin);
        if (cardDuneBtn != null)
        {
            cardDuneBtn.gameObject.SetActive(showDune);
            if (showDune)
            {
                RectTransform rt = cardDuneBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, startY - (visibleIndex * (cardHeight + spacing)));
                visibleIndex++;
            }
        }

        // Mage (Mage)
        bool showMage = (role == HeroRoleType.All || role == HeroRoleType.Mage);
        if (cardMageBtn != null)
        {
            cardMageBtn.gameObject.SetActive(showMage);
            if (showMage)
            {
                RectTransform rt = cardMageBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, startY - (visibleIndex * (cardHeight + spacing)));
                visibleIndex++;
            }
        }

        // Auto inspect visible hero if current inspected hero is hidden
        if (role == HeroRoleType.Tank && inspectedHeroId != "titan") InspectHero("titan");
        else if (role == HeroRoleType.Warrior && inspectedHeroId != "vael") InspectHero("vael");
        else if (role == HeroRoleType.Marksman && inspectedHeroId != "azure") InspectHero("azure");
        else if (role == HeroRoleType.Assassin && inspectedHeroId != "ronin" && inspectedHeroId != "dune") InspectHero("ronin");
        else if (role == HeroRoleType.Mage && inspectedHeroId != "mage") InspectHero("mage");
    }

    private static void GetHeroRoleBadgeInfo(string heroId, out string textVi, out string textEn, out Color bgCol, out Color textCol)
    {
        string hid = string.IsNullOrEmpty(heroId) ? "" : heroId.ToLower();
        if (hid == "titan" || hid == "titanberserker" || hid == "player4")
        {
            textVi = "ĐỠ ĐÒN"; textEn = "TANKER";
            bgCol = new Color(0.48f, 0.24f, 0.05f, 0.95f);
            textCol = new Color(1f, 0.78f, 0.25f, 1f);
        }
        else if (hid == "azure" || hid == "azureblade" || hid == "player3")
        {
            textVi = "XẠ THỦ"; textEn = "MARKSMAN";
            bgCol = new Color(0.04f, 0.28f, 0.42f, 0.95f);
            textCol = new Color(0.25f, 0.90f, 1f, 1f);
        }
        else if (hid == "ronin" || hid == "galeronin" || hid == "player2" || hid == "maid" || hid == "dune" || hid == "duneassassin" || hid == "player5")
        {
            textVi = "SÁT THỦ"; textEn = "ASSASSIN";
            bgCol = new Color(0.38f, 0.12f, 0.52f, 0.95f);
            textCol = new Color(0.88f, 0.55f, 1f, 1f);
        }
        else if (hid == "mage" || hid == "celestialmage" || hid == "player6")
        {
            textVi = "PHÁP SƯ"; textEn = "MAGE";
            bgCol = new Color(0.38f, 0.12f, 0.58f, 0.95f);
            textCol = new Color(0.92f, 0.55f, 1f, 1f);
        }
        else // vael
        {
            textVi = "ĐẤU SĨ"; textEn = "WARRIOR";
            bgCol = new Color(0.52f, 0.12f, 0.16f, 0.95f);
            textCol = new Color(1f, 0.45f, 0.50f, 1f);
        }
    }

    private void CreateLockedPlaceholderCard(Transform parent, Vector2 pos, Vector2 size, out TextMeshProUGUI txtRef)
    {
        GameObject card = new GameObject("Card_Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = card.GetComponent<Image>();
        img.color = new Color(0.08f, 0.09f, 0.12f, 0.7f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(card.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        txtRef = txtObj.AddComponent<TextMeshProUGUI>();
        txtRef.fontSize = 15f;
        txtRef.alignment = TextAlignmentOptions.Center;
        txtRef.color = new Color(0.5f, 0.55f, 0.65f, 0.7f);
        txtRef.text = LocalizationManager.T("KHÓA - ANH HÙNG TIẾP THEO", "LOCKED - COMING SOON");
    }

    public static Sprite[] LoadSpritesFromPath(string assetPath, int frameW = 64, int frameH = 64)
    {
        List<Sprite> list = new List<Sprite>();
#if UNITY_EDITOR
        var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (objects != null)
        {
            foreach (var o in objects)
            {
                if (o is Sprite s) list.Add(s);
            }
            if (list.Count > 0)
            {
                return list.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
            }
        }
#endif
        if (list.Count == 0)
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
            if (System.IO.File.Exists(fullPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    int cols = Mathf.Max(1, tex.width / frameW);
                    int rows = Mathf.Max(1, tex.height / frameH);
                    for (int r = 0; r < rows; r++)
                    {
                        int unityY = (rows - 1 - r) * frameH;
                        for (int c = 0; c < cols; c++)
                        {
                            Sprite s = Sprite.Create(tex, new Rect(c * frameW, unityY, frameW, frameH), new Vector2(0.5f, 0.5f), 64f);
                            list.Add(s);
                        }
                    }
                }
            }
        }
        return list.ToArray();
    }

    public void EnsureHeroSprites()
    {
        if (heroPreviewImage != null)
        {
            heroPreviewImage.preserveAspect = true;
            heroPreviewImage.color = Color.white;
        }

        if (vaelIdleFrames == null || vaelIdleFrames.Length == 0)
            vaelIdleFrames = LoadSpritesFromPath("Assets/ART/Player/IDLE/idle_down.png", 64, 64);

        if (roninIdleFrames == null || roninIdleFrames.Length == 0)
            roninIdleFrames = LoadSpritesFromPath("Assets/ART/Player2/_down idle.png", 64, 64);

        if (maidIdleFrames == null || maidIdleFrames.Length == 0)
            maidIdleFrames = roninIdleFrames;

        if (azureIdleFrames == null || azureIdleFrames.Length == 0)
            azureIdleFrames = LoadSpritesFromPath("Assets/ART/Player3/Sword_Idle.png", 64, 64);

        if (titanIdleFrames == null || titanIdleFrames.Length == 0)
        {
            var allFrames = LoadSpritesFromPath("Assets/ART/Player4/Hammer_Idle.png", 128, 128);
            if (allFrames != null && allFrames.Length > 0)
            {
                titanIdleFrames = allFrames.Take(Mathf.Min(5, allFrames.Length)).ToArray();
            }
        }

        if (duneIdleFrames == null || duneIdleFrames.Length == 0)
        {
            List<Sprite> list = new List<Sprite>();
            for (int i = 1; i <= 8; i++)
            {
                string p = $"Assets/ART/Player5/PNG/idle/idle_{i}.png";
#if UNITY_EDITOR
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) { list.Add(s); continue; }
#endif
                var fallbacks = LoadSpritesFromPath(p, 64, 64);
                if (fallbacks != null && fallbacks.Length > 0) list.Add(fallbacks[0]);
            }
            if (list.Count > 0) duneIdleFrames = list.ToArray();
        }

        if (mageIdleFrames == null || mageIdleFrames.Length == 0)
        {
            mageIdleFrames = LoadSpritesFromPath("Assets/ART/Player6/sample(idle&walk)/idle/sprite sheets/idle.png", 64, 64);
        }
    }

    private void HookButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
            AttachPointerClick(playButton.gameObject, OnPlayClicked);
        }
        if (mapSelectButton != null)
        {
            mapSelectButton.onClick.RemoveAllListeners();
            mapSelectButton.onClick.AddListener(OnMapSelectClicked);
            AttachPointerClick(mapSelectButton.gameObject, OnMapSelectClicked);
        }
        if (rosterButton != null)
        {
            rosterButton.onClick.RemoveAllListeners();
            rosterButton.onClick.AddListener(OnRosterClicked);
            AttachPointerClick(rosterButton.gameObject, OnRosterClicked);
        }
        if (skillButton != null)
        {
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(OnSkillsClicked);
            AttachPointerClick(skillButton.gameObject, OnSkillsClicked);
        }
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(OnInventoryClicked);
            AttachPointerClick(inventoryButton.gameObject, OnInventoryClicked);
        }
        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
            shopButton.onClick.AddListener(OnShopClicked);
            AttachPointerClick(shopButton.gameObject, OnShopClicked);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsClicked);
            AttachPointerClick(settingsButton.gameObject, OnSettingsClicked);
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitClicked);
            AttachPointerClick(exitButton.gameObject, OnExitClicked);
        }
        if (logoutButton != null)
        {
            logoutButton.gameObject.SetActive(false);
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogoutClicked);
            AttachPointerClick(logoutButton.gameObject, OnLogoutClicked);
        }

        if (map1Button != null)
        {
            map1Button.onClick.RemoveAllListeners();
            map1Button.onClick.AddListener(() => LoadMap("Map1_Thestart"));
            AttachPointerClick(map1Button.gameObject, () => LoadMap("Map1_Thestart"));
        }
        if (map2Button != null)
        {
            map2Button.onClick.RemoveAllListeners();
            map2Button.onClick.AddListener(() => LoadMap("Map2_ValleyRuin"));
            AttachPointerClick(map2Button.gameObject, () => LoadMap("Map2_ValleyRuin"));
        }
        if (map3Button != null)
        {
            map3Button.onClick.RemoveAllListeners();
            map3Button.onClick.AddListener(() => LoadMap("Map3_ForgottenMemories"));
            AttachPointerClick(map3Button.gameObject, () => LoadMap("Map3_ForgottenMemories"));
        }
        if (mapBackBtn != null)
        {
            mapBackBtn.onClick.RemoveAllListeners();
            mapBackBtn.onClick.AddListener(ShowMainPanel);
            AttachPointerClick(mapBackBtn.gameObject, ShowMainPanel);
        }

        if (rosterBackBtn != null)
        {
            rosterBackBtn.onClick.RemoveAllListeners();
            rosterBackBtn.onClick.AddListener(ShowMainPanel);
            AttachPointerClick(rosterBackBtn.gameObject, ShowMainPanel);
        }
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

    public void UpdateAllTexts()
    {
        string activeUser = PlayerPrefs.GetString(AccountDatabase.PREF_CURRENT_USER, PlayerPrefs.GetString("Player_Name", "WANDERER"));
        if (playerNameText != null)
        {
            playerNameText.text = activeUser.ToUpper();
        }
        else
        {
            var tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in tmps)
            {
                if (t.name.ToLower().Contains("playername") || t.text == "HUNG" || t.text == "WANDERER")
                {
                    playerNameText = t;
                    t.text = activeUser.ToUpper();
                    break;
                }
            }
        }

        if (titleText != null) titleText.text = LocalizationManager.Get("APP_TITLE");
        if (playerTitleText != null) playerTitleText.text = LocalizationManager.Get("LBL_PLAYER_TITLE");
        if (logoutBtnText != null) logoutBtnText.text = LocalizationManager.Get("BTN_LOGOUT");

        if (playBtnText != null)
        {
            playBtnText.text = LocalizationManager.Get("BTN_PLAY");
            if (playButton != null) WhiteIconLoader.SetButtonIconAndText(playButton, "Play", playBtnText.text, Color.white, Color.white, true);
        }
        if (mapSelectBtnText != null) mapSelectBtnText.text = LocalizationManager.Get("BTN_MAP_SELECT");
        if (rosterBtnText != null)
        {
            rosterBtnText.text = LocalizationManager.Get("BTN_ROSTER");
            if (rosterButton != null) WhiteIconLoader.SetButtonIconAndText(rosterButton, "Hero", rosterBtnText.text, Color.white, Color.white, true);
        }
        if (skillBtnText != null) skillBtnText.text = LocalizationManager.Get("BTN_SKILLS");
        if (inventoryBtnText != null)
        {
            inventoryBtnText.text = LocalizationManager.Get("BTN_INVENTORY");
            if (inventoryButton != null) WhiteIconLoader.SetButtonIconAndText(inventoryButton, "Bag", inventoryBtnText.text, Color.white, Color.white, true);
        }
        if (shopBtnText != null)
        {
            shopBtnText.text = LocalizationManager.Get("BTN_SHOP");
            if (shopButton != null) WhiteIconLoader.SetButtonIconAndText(shopButton, "Cart", shopBtnText.text, Color.white, Color.white, true);
        }
        if (settingsBtnText != null)
        {
            settingsBtnText.text = LocalizationManager.Get("BTN_SETTINGS");
            if (settingsButton != null) WhiteIconLoader.SetButtonIconAndText(settingsButton, "Gear", settingsBtnText.text, Color.white, Color.white, true);
        }
        if (exitBtnText != null) exitBtnText.text = LocalizationManager.Get("BTN_EXIT");

        // Map Select panel
        if (mapPanelTitleText != null) mapPanelTitleText.text = LocalizationManager.Get("BTN_MAP_SELECT");
        if (map1TitleText != null) map1TitleText.text = LocalizationManager.Get("MAP1_NAME");
        if (map1DescText != null) map1DescText.text = LocalizationManager.Get("MAP1_DESC");
        if (map2TitleText != null) map2TitleText.text = LocalizationManager.Get("MAP2_NAME");
        if (map2DescText != null) map2DescText.text = LocalizationManager.Get("MAP2_DESC");
        if (map3TitleText != null) map3TitleText.text = LocalizationManager.Get("MAP3_NAME");
        if (map3DescText != null) map3DescText.text = LocalizationManager.Get("MAP3_DESC");
        if (mapBackBtnText != null)
        {
            mapBackBtnText.text = LocalizationManager.Get("BTN_BACK");
            if (mapBackBtn != null) WhiteIconLoader.SetButtonIconAndText(mapBackBtn, "SolidArrow-Left", mapBackBtnText.text, Color.white, Color.white, true);
        }

        // Roster panel titles & Card texts
        if (rosterPanelTitleText != null) rosterPanelTitleText.text = LocalizationManager.Get("BTN_ROSTER");
        if (leftColHeaderText != null) leftColHeaderText.text = LocalizationManager.T("CHỌN ANH HÙNG", "HERO SELECTION");
        if (rosterBackBtn != null) WhiteIconLoader.ReplaceTextWithIcon(rosterBackBtn, "Cross", Color.white, new Vector2(24f, 24f));

        string currentSelected = CharacterManager.EnsureInstance().GetSelectedCharacterId();

        if (cardVaelTitleText != null) cardVaelTitleText.text = LocalizationManager.Get("HERO_VAEL_NAME");
        if (cardVaelSubText != null) cardVaelSubText.text = LocalizationManager.T("Đấu Sĩ / Thánh Kiếm", "Warrior / Holy Blade");
        if (cardVaelStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_VAELSTRIDER);
            cardVaelStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }

        if (cardRoninTitleText != null) cardRoninTitleText.text = LocalizationManager.Get("HERO_RONIN_NAME");
        if (cardRoninSubText != null) cardRoninSubText.text = LocalizationManager.T("Sát Thủ / Phong Ấn & Ảo Ảnh", "Assassin / Phantom Blade");
        if (cardRoninStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_GALE_RONIN);
            cardRoninStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }
        else if (cardMaidTitleText != null)
        {
            cardMaidTitleText.text = LocalizationManager.Get("HERO_RONIN_NAME");
            if (cardMaidSubText != null) cardMaidSubText.text = LocalizationManager.T("Sát Thủ / Phong Ấn & Ảo Ảnh", "Assassin / Phantom Blade");
            if (cardMaidStatusText != null)
            {
                bool isSel = (currentSelected == CharacterManager.CHAR_GALE_RONIN);
                cardMaidStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
            }
        }

        if (cardAzureTitleText != null) cardAzureTitleText.text = LocalizationManager.T("Lam Kiếm Sĩ", "Azure Blade");
        if (cardAzureSubText != null) cardAzureSubText.text = LocalizationManager.T("Xạ Thủ / Sóng Kiếm Băng", "Marksman / Frost Waves");
        if (cardAzureStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_AZURE_BLADE);
            cardAzureStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }

        if (cardTitanTitleText != null) cardTitanTitleText.text = LocalizationManager.T("Cuồng Chiến Binh", "Titan Berserker");
        if (cardTitanSubText != null) cardTitanSubText.text = LocalizationManager.T("Đỡ Đòn / Đại Chùy", "Tanker / Heavy Hammer");
        if (cardTitanStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_TITAN_BERSERKER);
            cardTitanStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }

        if (cardDuneTitleText != null) cardDuneTitleText.text = LocalizationManager.T("Sa Mạc Thích Khách", "Dune Assassin");
        if (cardDuneSubText != null) cardDuneSubText.text = LocalizationManager.T("Sát Thủ / Song Đoản Đao", "Assassin / Dual Scimitars");
        if (cardDuneStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_DUNE_ASSASSIN);
            cardDuneStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }

        if (cardMageTitleText != null) cardMageTitleText.text = LocalizationManager.Get("HERO_MAGE_NAME");
        if (cardMageSubText != null) cardMageSubText.text = LocalizationManager.T("Pháp Sư / Tinh Vân & Thiên Thạch", "Mage / Arcane & Meteor");
        if (cardMageStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_CELESTIAL_MAGE);
            cardMageStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }

        // Đảm bảo toàn bộ Avatar trên thẻ bài không bị trắng
        EnsureHeroSprites();
        if (cardVaelAvatarImg != null && cardVaelAvatarImg.sprite == null && vaelIdleFrames != null && vaelIdleFrames.Length > 0)
            cardVaelAvatarImg.sprite = vaelIdleFrames[0];
        if (cardRoninAvatarImg != null && cardRoninAvatarImg.sprite == null && roninIdleFrames != null && roninIdleFrames.Length > 0)
            cardRoninAvatarImg.sprite = roninIdleFrames[0];
        if (cardAzureAvatarImg != null && cardAzureAvatarImg.sprite == null && azureIdleFrames != null && azureIdleFrames.Length > 0)
            cardAzureAvatarImg.sprite = azureIdleFrames[0];
        if (cardTitanAvatarImg != null && cardTitanAvatarImg.sprite == null && titanIdleFrames != null && titanIdleFrames.Length > 0)
            cardTitanAvatarImg.sprite = titanIdleFrames[0];
        if (cardDuneAvatarImg != null && cardDuneAvatarImg.sprite == null && duneIdleFrames != null && duneIdleFrames.Length > 0)
            cardDuneAvatarImg.sprite = duneIdleFrames[0];
        if (cardMageAvatarImg != null && cardMageAvatarImg.sprite == null && mageIdleFrames != null && mageIdleFrames.Length > 0)
            cardMageAvatarImg.sprite = mageIdleFrames[0];

        UpdateHeroRoleTabVisuals();
        UpdateCurrencyDisplay();
        UpdateHeroDetails();
        InspectHero(inspectedHeroId);
        UpdateMapCardsLockState();
    }

    private void UpdateHeroDetails()
    {
        string curChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        if (curChar == CharacterManager.CHAR_TITAN_BERSERKER)
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.T("Cuồng Chiến Binh", "Titan Berserker");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("<color=#FFA502>[ĐỠ ĐÒN]</color> Đại Chùy Chiến Thần / Tanker & Chấn Địa", "<color=#FFA502>[TANKER]</color> Heavy War Hammer / Tanker & CC");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("BỘ KỸ NĂNG: CUỒNG CHIẾN ĐẠI CHÙY", "SKILLSET: TITAN WAR HAMMER");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T(
                "• <b>Đòn Đánh & Gồng Địa Chấn:</b> Nện đại chùy càn quét diện rộng (Nội tại: Giảm 20% sát thương). Nhấn Giữ gồng Thế Thủ: <color=#55FF88><b>Bất tử 100%</b></color>, thả tay nện đất bộc phá gây <color=#FFAA33><b>Choáng 1.0s</b></color>!\n" +
                "• <b>Lướt (Chiến Xa Xoay Búa 360°):</b> Lướt tốc độ cao xoay búa 360° chém quét mọi kẻ địch trên đường lướt & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>.\n" +
                "• <b>Tuyệt Kỹ (Cuồng Nộ Chấn Địa):</b> Hút toàn bộ quái vật vào tâm chấn, bật nhảy nện búa dung nham bộc phát chấn động hủy diệt hất tung toàn bộ kẻ địch.",
                "• <b>Attack & Earthshaker:</b> Massive hammer smashes (Innate 20% DR). Hold attack for Earthshaker Stance: <color=#55FF88><b>100% Invulnerable</b></color>, release to smash the ground and <color=#FFAA33><b>Stun for 1.0s</b></color>!\n" +
                "• <b>Dash (Juggernaut 360° Spin):</b> Heavy spin dash slashing all enemies along the path & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>.\n" +
                "• <b>Ultimate (Cataclysm Slam):</b> Pulls enemies into the vortex epicenter and leaps to smash down volcanic cataclysmic shockwaves.");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 200   CÔNG: 32   TỐC: 4.0", "HP: 200   ATK: 32   SPD: 4.0");
            }
        }
        else if (curChar == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.T("Sa Mạc Thích Khách", "Dune Assassin");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("<color=#A855F7>[SÁT THỦ]</color> Song Đoản Đao / Tốc Biến & Phản Đòn", "<color=#A855F7>[ASSASSIN]</color> Dual Scimitars / Speed & Counter");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("BỘ KỸ NĂNG: SA MẠC THÍCH KHÁCH", "SKILLSET: DUNE SHADOW ASSASSIN");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T(
                "• <b>Đòn Đánh & Thủ Phản Kích:</b> Combo 3 đòn song đao chém lướt bạo kích cao. Nhấn Giữ vào Thế Thủ Sa Mạc: Giảm 70% sát thương và tung đòn <color=#FFAA33><b>Phản Kích Kiếm Khí Choáng 1.2s</b></color>!\n" +
                "• <b>Lướt (Ảo Ảnh Sa Lướt):</b> Tích lũy tối đa <color=#55FFFF><b>3 lần lướt liên hoàn</b></color> cực cơ động, tạo tàn ảnh cát & <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> mỗi lần lướt.\n" +
                "• <b>Tuyệt Kỹ (Cuồng Bão Sa Ảnh X-Rend):</b> Tạo lốc xoáy bão cát vây hãm và trảm kích chữ X xé toạc chiến trường gây sát thương cực lớn và hiệu ứng <b>Chảy Máu Sa Mạc</b>.",
                "• <b>Attack & Mirage Counter:</b> Fast 3-hit dagger combos with high crit chance. Hold attack for Mirage Defend: reduces damage by 70% and unleashes an Air Slash Counter (<color=#FFAA33><b>1.2s Stun</b></color>)!\n" +
                "• <b>Dash (Triple Sand Mirage):</b> Stores up to <color=#55FFFF><b>3 Consecutive Dash Charges</b></color>, leaves sand mirages & <color=#FFAA33><b>Reduces Ultimate CD by 1.5s</b></color> per dash.\n" +
                "• <b>Ultimate (Sandstorm X-Rend):</b> Summons a sand vortex and executes a devastating Cross-X Slash with Sand Bleed.");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 110   CÔNG: 18   TỐC: 4.8", "HP: 110   ATK: 18   SPD: 4.8");
            }
        }
        else if (curChar == CharacterManager.CHAR_AZURE_BLADE)
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.T("Lam Kiếm Sĩ", "Azure Blade");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("<color=#00D2FF>[XẠ THỦ]</color> Trọng Kiếm Khí / Sóng Kiếm Băng Tầm Xa", "<color=#00D2FF>[MARKSMAN]</color> Heavy Crescent Blade / Ranged Waves");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("BỘ KỸ NĂNG: TRỌNG KIẾM BĂNG KHÍ", "SKILLSET: AZURE CRESCENT BLADE");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T(
                "• <b>Đòn Đánh & Sóng Kiếm:</b> Đòn chém uy lực phóng <color=#55FFFF><b>Sóng Kiếm Băng</b></color> xuyên thấu quái vật (tầm 6.5m), trúng đích hồi <color=#38BDF8><b>+6 Mana</b></color> & giảm 50% thời gian chờ đòn đánh.\n" +
                "• <b>Lướt (Hàn Băng Trượt Tuyết):</b> Lướt né đòn và <color=#55FFFF><b>Hồi ngay lập tức đòn đánh thường</b></color> kế tiếp.\n" +
                "• <b>Tuyệt Kỹ (Bão Sóng Kiếm Nan Quạt):</b> Tụ khí phóng 5 đợt kiếm khí hình nan quạt khổng lồ quét sạch toàn bộ chiến trường.",
                "• <b>Attack & Frost Wave:</b> Heavy slashes unleashing <color=#55FFFF><b>Frost Sword Waves</b></color> piercing enemies (6.5m), restores <color=#38BDF8><b>+6 Mana</b></color> & cuts attack cooldown by 50% on hit.\n" +
                "• <b>Dash (Frost Glide):</b> Swift dash dodging attacks & <color=#55FFFF><b>Instantly resets basic attack</b></color>.\n" +
                "• <b>Ultimate (Crescent Tempest):</b> Releases 5 massive crescent sword waves in a fan spread across the battlefield.");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 110   CÔNG: 24   TỐC: 4.8", "HP: 110   ATK: 24   SPD: 4.8");
            }
        }
        else if (curChar == CharacterManager.CHAR_GALE_RONIN)
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.T("Phong Lôi Lãng Khách", "Gale Ronin");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("<color=#A855F7>[SÁT THỦ]</color> Bạch Dạ Kiếm Khách / Nhất Kích Tất Sát", "<color=#A855F7>[ASSASSIN]</color> Phantom Blade / Iaido Precision");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("BỘ KỸ NĂNG: PHONG LÔI KIẾM THUẬT", "SKILLSET: GALE PHANTOM BLADE");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T(
                "• <b>Đòn Đánh & Phong Trảm:</b> Chém quét bán nguyệt 180° siêu nhanh tích <color=#55FFFF><b>Phong Ấn</b></color> (3 tầng nổ <color=#FFAA33><b>240% Crit</b></color> + Lốc Xoáy hút quái). Khi có Khiên Nội Năng: Nhấn Giữ Tụ Lực chém rách không gian & phóng Nguyệt Kiếm Khí <color=#FFAA33><b>x2.85 Crit DMG</b></color>!\n" +
                "• <b>Lướt (Tàn Ảnh Tốc Biến - Murad Style):</b> Nhịp 1 tốc biến 4.8m tới trước & cắm lại <b>Tàn Ảnh Bóng Ma</b> (4.5s). Nhịp 2 (Bấm lại) lập tức Teleport quay về vị trí bóng & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>!\n" +
                "• <b>Tuyệt Kỹ (Ảo Ảnh Trảm Sát):</b> Lướt ảo ảnh chớp nhoáng trảm sát liên hoàn toàn bộ kẻ địch trong không gian, <color=#55FF88><b>Bất tử hoàn toàn</b></color> trong lúc xuất chiêu.",
                "• <b>Attack & Wind Marks:</b> Fast 180° crescent cleave stacking <color=#55FFFF><b>Wind Marks</b></color> (3 stacks bursts for <color=#FFAA33><b>240% Crit</b></color> + Vacuum Vortex). When Inner Energy Shield is active: Hold attack to charge Iaido Spatial Slash & shoot Gale Crescent Wave (<color=#FFAA33><b>x2.85 Crit DMG</b></color>)!\n" +
                "• <b>Dash (Shadow Phantom Warp):</b> Stage 1 warps 4.8m forward leaving a <b>Shadow Clone</b> (4.5s). Stage 2 (Cast again) instantly Warps back to clone & <color=#FFAA33><b>Cuts Ultimate CD by 2.0s</b></color>!\n" +
                "• <b>Ultimate (Phantom Flash Cleave):</b> Executes blinding phantom dash slashes cutting all enemies in space with <color=#55FF88><b>Complete Invincibility</b></color>.");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 95   CÔNG: 28   TỐC: 5.4", "HP: 95   ATK: 28   SPD: 5.4");
            }
        }
        else if (curChar == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.Get("HERO_MAGE_NAME");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("<color=#E056FD>[PHÁP SƯ]</color> Tinh Vân Ma Pháp / Triệu Hồi Thiên Thạch & Hộ Thể", "<color=#E056FD>[MAGE]</color> Arcane Sorcery / Meteor Tempest & Barrier");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("BỘ KỸ NĂNG: TINH VÂN MA PHÁP", "SKILLSET: CELESTIAL ARCANE");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T(
                "• <b>Đòn Đánh & Cầu Ma Pháp:</b> Phóng 3 cầu ma pháp tinh tú tự dẫn đường và bộc phát sét lan. <color=#FFDF66>Nhấn Giữ Hộ Thể Ma Trận:</color> Hấp thụ 80% sát thương, bộc phá sóng năng lượng đẩy lùi & hồi <color=#38BDF8><b>+10 Mana</b></color>!\n" +
                "• <b>Lướt (Tốc Biến Hư Không):</b> Dịch chuyển tức thời trong làn khói ma pháp (<color=#55FF88><b>Bất tử</b></color>) & hồi phục <color=#38BDF8><b>+12 Mana</b></color>.\n" +
                "• <b>Tuyệt Kỹ (Thiên Thạch Ma Trận):</b> Mở đại pháp trận tinh tú giáng mưa thiên thạch ma pháp hủy diệt toàn màn hình.",
                "• <b>Attack & Arcane Orbs:</b> Fires 3 homing arcane star orbs chaining lightning. <color=#FFDF66>Hold attack for Barrier:</color> absorbs 80% damage, bursts back enemies & grants <color=#38BDF8><b>+10 Mana</b></color>!\n" +
                "• <b>Dash (Arcane Blink):</b> Instantly warps in mystical mist with <color=#55FF88><b>Invulnerability</b></color> & restores <color=#38BDF8><b>+12 Mana</b></color>.\n" +
                "• <b>Ultimate (Meteor Apocalypse):</b> Opens celestial magic circles summoning a devastating meteor storm across the battlefield.");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 90   CÔNG: 30   TỐC: 4.5", "HP: 90   ATK: 30   SPD: 4.5");
            }
        }
        else
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.Get("HERO_VAEL_NAME");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("<color=#FF4757>[ĐẤU SĨ]</color> Hiệp Sĩ Tiên Phong / Thánh Kiếm Cổ Tộc", "<color=#FF4757>[WARRIOR]</color> Holy Vanguard / Ancient Blade Master");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("BỘ KỸ NĂNG: THÁNH QUANG HIỆP SĨ", "SKILLSET: HOLY VANGUARD");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T(
                "• <b>Đòn Đánh & Thế Thủ Phản Kích:</b> Chém kiếm 3 nhịp liên hoàn (hồi <color=#38BDF8><b>+8 Mana</b></color>). Nhấn Giữ gồng Thế Thủ: Giảm 20% sát thương, thả tay bộc phá <color=#FFAA33><b>Thần Kiếm Phản Kích (x3.2 Crit DMG)</b></color> phóng cột sóng thánh quang xuyên phá!\n" +
                "• <b>Lướt (Thánh Thuẫn Phản Đòn):</b> Lướt nhanh với vòng Thánh Quang (<color=#55FF88><b>Bất tử khi lướt</b></color>). Lướt qua quái hoặc đỡ đòn thành công sẽ <color=#FFAA33><b>Hồi 50% CD Lướt</b></color>, hồi <color=#38BDF8><b>+15 Mana</b></color> & phóng <b>3 luồng Thánh Kiếm Khí</b> phản công!\n" +
                "• <b>Tuyệt Kỹ (Thánh Kiếm Thiên Nham):</b> Xoay bão lốc kiếm 360° hút quái vật vào tâm bão và triệu hồi Thánh Kiếm Thiên Nham giáng thế từ không trung hủy diệt diện rộng.",
                "• <b>Attack & Divine Parry:</b> 3-hit sword combo (restores <color=#38BDF8><b>+8 Mana</b></color>). Hold attack for Parry Stance: 20% DR, release to unleash <color=#FFAA33><b>Divine Riposte (x3.2 Crit DMG)</b></color> firing holy shockwave pillars!\n" +
                "• <b>Dash (Radiant Riposte Dash):</b> Swift dash with holy aegis (<color=#55FF88><b>Invulnerable during dash</b></color>). Dashing through enemies or deflecting attacks <color=#FFAA33><b>Refunds 50% Dash CD</b></color>, restores <color=#38BDF8><b>+15 Mana</b></color> & fires <b>3 Holy Sword Waves</b>!\n" +
                "• <b>Ultimate (Holy Sword Tempest):</b> 360° sword vortex storm pulling enemies and summons a gigantic Holy Sword of the Heavens slamming down with massive AoE damage.");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 100   CÔNG: 20   TỐC: 5.0", "HP: 100   ATK: 20   SPD: 5.0");
            }
        }
    }

    public void InspectHero(string heroId)
    {
        EnsureHeroSprites();
        inspectedHeroId = heroId.ToLower();
        string currentSelected = CharacterManager.EnsureInstance().GetSelectedCharacterId();

        EquipmentSocketManager.GetTotalHeroSocketBonus(
            out _, out _, out _, out _,
            out _, out _,
            out _, out float sCd, out _, out _,
            out _, out _,
            out _, out _, out _,
            inspectedHeroId);

        float upDashCd = PlayerUpgradeManager.EnsureInstance()?.GetDashCooldownReduction(inspectedHeroId) ?? 0f;
        float upUltiCd = PlayerUpgradeManager.EnsureInstance()?.GetUltiCooldownReduction(inspectedHeroId) ?? 0f;
        float[] baseCds = GetHeroBaseCooldowns(inspectedHeroId);

        if (inspectedHeroId == "titan" || inspectedHeroId == "titanberserker" || inspectedHeroId == "player4")
        {
            if (rosterHeroPreviewImage != null && titanIdleFrames != null && titanIdleFrames.Length > 0)
                rosterHeroPreviewImage.sprite = titanIdleFrames[0];
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Cuồng Chiến Binh (Titan Berserker)", "Titan Berserker (Heavy Tanker)");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("<color=#FFA502>[ĐỠ ĐÒN]</color> Đại Chùy Chiến Thần / Tanker & Chấn Địa", "<color=#FFA502>[TANKER]</color> Heavy War Hammer / Tanker & CC");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 200   CÔNG: 32   TỐC: 4.0", "HP: 200   ATK: 32   SPD: 4.0");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("BỘ KỸ NĂNG: CUỒNG CHIẾN ĐẠI CHÙY", "SKILLSET: TITAN WAR HAMMER");

            SetHeroSkillCard(0, MobileActionButton.GetHeroActionIcon("titan", MobileActionButton.ButtonType.Attack),
                "ATK",
                LocalizationManager.T("1. ĐẠI CHÙY & THẾ THỦ NỆN ĐẤT", "1. WAR HAMMER & EARTHSHAKER"),
                LocalizationManager.T("Nện đại chùy càn quét gây <color=#FF3B30><b>ATK</b></color> (Nội tại: Giảm 20% sát thương). <color=#FFDF66>Nhấn Giữ gồng Thế Thủ:</color> <color=#55FF88><b>Bất tử 100%</b></color>, thả tay nện đất bộc phá gây <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Choáng 1.0s</b></color>!",
                                     "Massive hammer smashes inflicting <color=#FF3B30><b>ATK</b></color> (Innate 20% DR). <color=#FFDF66>Hold attack for Earthshaker:</color> <color=#55FF88><b>100% Invulnerable</b></color>, release to smash ground inflicting <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Stun 1.0s</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("titan", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("LƯỚT", "DASH"),
                LocalizationManager.T("2. CHIẾN XA XOAY BÚA 360°", "2. JUGGERNAUT 360° SPIN"),
                LocalizationManager.T("Lướt tốc độ cao xoay búa 360° càn quét gây <color=#FF3B30><b>ATK</b></color> & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>.",
                                     "Heavy spin dash slashing all enemies along path with <color=#FF3B30><b>ATK</b></color> & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>."),
                baseCds[1], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("titan", MobileActionButton.ButtonType.Skill),
                "ATK",
                LocalizationManager.T("3. CUỒNG NỘ CHẤN ĐỊA DUNG NHAM", "3. VOLCANIC CATACLYSM SLAM"),
                LocalizationManager.T("Hút toàn bộ quái vật vào tâm chấn, bật nhảy nện búa dung nham bộc phát chấn động gây <color=#FF3B30><b>ATK cực đại</b></color> & hất tung kẻ địch.",
                                     "Pulls enemies into epicenter and leaps to smash down volcanic cataclysmic shockwaves with massive <color=#FF3B30><b>ATK</b></color>."),
                baseCds[2], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(0, MobileActionButton.GetHeroActionIcon("azure", MobileActionButton.ButtonType.Attack),
                "AP",
                LocalizationManager.T("1. TRỌNG KIẾM & SÓNG BĂNG KIẾM KHÍ", "1. FROST SWORD WAVES"),
                LocalizationManager.T("Chém phóng <color=#55FFFF><b>Sóng Kiếm Băng</b></color> gây <color=#B84DFF><b>AP</b></color> xuyên thấu kẻ địch (tầm 6.5m), trúng đích hồi <color=#38BDF8><b>+6 Mana</b></color> & giảm 50% thời gian chờ đòn đánh.",
                                     "Heavy slashes unleashing <color=#55FFFF><b>Frost Sword Waves</b></color> piercing enemies with <color=#B84DFF><b>AP</b></color> (6.5m), restores <color=#38BDF8><b>+6 Mana</b></color> & cuts attack cooldown 50%."),
                baseCds[0], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("azure", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("LƯỚT", "DASH"),
                LocalizationManager.T("2. HÀN BĂNG TRƯỢT TUYẾT", "2. FROST GLIDE DASH"),
                LocalizationManager.T("Lướt né đòn trên dải băng tuyết & <color=#55FFFF><b>Hồi ngay lập tức đòn đánh thường</b></color> kế tiếp.",
                                     "Swift glide dash dodging attacks & <color=#55FFFF><b>Instantly resets basic attack cooldown</b></color>."),
                baseCds[1], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("azure", MobileActionButton.ButtonType.Skill),
                "AP",
                LocalizationManager.T("3. BÃO SÓNG KIẾM NAN QUẠT", "3. CRESCENT TEMPEST"),
                LocalizationManager.T("Tụ nội lực phóng 5 đợt kiếm khí băng lam nan quạt khổng lồ gây <color=#B84DFF><b>AP diện rộng</b></color> quét sạch toàn bộ chiến trường.",
                                     "Releases 5 massive crescent frost sword waves in a fan spread across the battlefield inflicting devastating AoE <color=#B84DFF><b>AP</b></color>."),
                baseCds[2], sCd, upDashCd, upUltiCd);

            bool isSelected = (currentSelected == CharacterManager.CHAR_AZURE_BLADE);
            if (rosterActionBtnText != null) rosterActionBtnText.text = isSelected ? LocalizationManager.T("ĐANG CHỌN XUẤT TRẬN", "SELECTED FOR BATTLE") : LocalizationManager.T("CHỌN XUẤT TRẬN", "SELECT FOR BATTLE");
            if (rosterActionBtn != null)
            {
                rosterActionBtn.interactable = true;
                var img = rosterActionBtn.GetComponent<Image>();
                if (img != null) img.color = isSelected ? new Color(0.18f, 0.55f, 0.28f, 1f) : new Color(0.18f, 0.45f, 0.75f, 1f);
            }
        }
        else if (inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid")
        {
            if (rosterHeroPreviewImage != null && roninIdleFrames != null && roninIdleFrames.Length > 0)
                rosterHeroPreviewImage.sprite = roninIdleFrames[0];
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Phong Lôi Lãng Khách (Gale Ronin)", "Gale Ronin (Phantom Blade)");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("<color=#A855F7>[SÁT THỦ]</color> Bạch Dạ Kiếm Khách / Nhất Kích Tất Sát", "<color=#A855F7>[ASSASSIN]</color> Phantom Blade / Iaido Precision");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 95   CÔNG: 28   TỐC: 5.4", "HP: 95   ATK: 28   SPD: 5.4");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("BỘ KỸ NĂNG: PHONG LÔI KIẾM THUẬT", "SKILLSET: GALE PHANTOM BLADE");

            SetHeroSkillCard(0, MobileActionButton.GetHeroActionIcon("ronin", MobileActionButton.ButtonType.Attack),
                "ATK / AP",
                LocalizationManager.T("1. PHONG NGUYỆT TRẢM & TỤ LỰC IAIDO", "1. GALE CRESCENT SWEEP & IAIDO"),
                LocalizationManager.T("Chém 180° gây <color=#FF3B30><b>ATK</b></color> tích <color=#55FFFF><b>Phong Ấn</b></color> (3 tầng nổ <color=#B84DFF><b>AP 240% Crit</b></color> + Lốc Xoáy). Khi có Khiên: Gồng chém rách không gian phóng Nguyệt Kiếm Khí <color=#FF3B30><b>x2.85 Crit ATK</b></color>!",
                                     "180° slashes dealing <color=#FF3B30><b>ATK</b></color> & stacking <color=#55FFFF><b>Wind Marks</b></color> (3 stacks bursts for <color=#B84DFF><b>AP 240% Crit</b></color> + Vortex). When Shielded: Charge for <color=#FF3B30><b>x2.85 Crit ATK</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("ronin", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("TỐC BIẾN", "WARP"),
                LocalizationManager.T("2. TÀN ẢNH TỐC BIẾN (MURAD STYLE)", "2. SHADOW PHANTOM WARP"),
                LocalizationManager.T("Nhịp 1 tốc biến 4.8m tới trước (<color=#55FF88><b>Bất tử</b></color>) cắm bóng. Nhịp 2 Teleport quay về bóng & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>!",
                                     "Stage 1 warps 4.8m forward leaving clone (<color=#55FF88><b>Invincible</b></color>). Stage 2 Warps back to clone & <color=#FFAA33><b>Cuts Ulti CD by 2.0s</b></color>!"),
                baseCds[1], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("ronin", MobileActionButton.ButtonType.Skill),
                "TD",
                LocalizationManager.T("3. ÁM ẢNH PHÂN THÂN TRẢM", "3. PHANTOM FLASH CLEAVE"),
                LocalizationManager.T("Lướt ảo ảnh chớp nhoáng trảm sát liên hoàn mọi kẻ địch trong không gian gây <color=#E2E8F0><b>TD (bỏ qua giáp)</b></color>, <color=#55FF88><b>Bất tử hoàn toàn</b></color> trong lúc xuất chiêu.",
                                     "Executes blinding phantom dash slashes dealing devastating <color=#E2E8F0><b>TD (ignores defense)</b></color> with <color=#55FF88><b>Complete Invincibility</b></color>."),
                baseCds[2], sCd, upDashCd, upUltiCd);

            bool isSelected = (currentSelected == CharacterManager.CHAR_GALE_RONIN);
            if (rosterActionBtnText != null) rosterActionBtnText.text = isSelected ? LocalizationManager.T("ĐANG CHỌN XUẤT TRẬN", "SELECTED FOR BATTLE") : LocalizationManager.T("CHỌN XUẤT TRẬN", "SELECT FOR BATTLE");
            if (rosterActionBtn != null)
            {
                rosterActionBtn.interactable = true;
                var img = rosterActionBtn.GetComponent<Image>();
                if (img != null) img.color = isSelected ? new Color(0.18f, 0.55f, 0.28f, 1f) : new Color(0.18f, 0.45f, 0.75f, 1f);
            }
        }
        else if (inspectedHeroId == "dune" || inspectedHeroId == "duneassassin" || inspectedHeroId == "player5")
        {
            if (rosterHeroPreviewImage != null && duneIdleFrames != null && duneIdleFrames.Length > 0)
                rosterHeroPreviewImage.sprite = duneIdleFrames[0];
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Sa Mạc Thích Khách (Dune Assassin)", "Dune Assassin (Dual Scimitars)");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("<color=#A855F7>[SÁT THỦ]</color> Song Đoản Đao / Tốc Biến & Phản Đòn", "<color=#A855F7>[ASSASSIN]</color> Dual Scimitars / Speed & Counter");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 110   CÔNG: 18   TỐC: 4.8", "HP: 110   ATK: 18   SPD: 4.8");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("BỘ KỸ NĂNG: SA MẠC THÍCH KHÁCH", "SKILLSET: DUNE SHADOW ASSASSIN");

            SetHeroSkillCard(0, MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Attack),
                "ATK / TD",
                LocalizationManager.T("1. SONG ĐAO & THẾ THỦ PHẢN KÍCH", "1. DUAL DAGGERS & MIRAGE PARRY"),
                LocalizationManager.T("Combo 3 đòn song đao gây <color=#FF3B30><b>ATK</b></color> bạo kích cao. <color=#FFDF66>Nhấn Giữ Thế Thủ:</color> Giảm 70% sát thương và tung đòn Phản Kích Kiếm Khí gây <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Choáng 1.2s</b></color>!",
                                     "Fast 3-hit combos dealing <color=#FF3B30><b>ATK</b></color>. <color=#FFDF66>Hold attack for Mirage Defend:</color> 70% DR & unleashes Air Slash Counter inflicting <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>1.2s Stun</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("ẢO ẢNH", "MIRAGE"),
                LocalizationManager.T("2. ẢO ẢNH SA LƯỚT (3 LẦN LIÊN HOÀN)", "2. TRIPLE SAND MIRAGE DASH"),
                LocalizationManager.T("Tích lũy tối đa <color=#55FFFF><b>3 lần lướt liên hoàn</b></color> cực cơ động, tạo tàn ảnh cát & <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> mỗi lần lướt.",
                                     "Stores up to <color=#55FFFF><b>3 Consecutive Dash Charges</b></color>, leaves mirages & <color=#FFAA33><b>Reduces Ulti CD 1.5s</b></color> per dash."),
                baseCds[1], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Skill),
                "TD",
                LocalizationManager.T("3. CUỒNG BÃO SA ẢNH X-REND", "3. SANDSTORM X-REND"),
                LocalizationManager.T("Tạo lốc xoáy cát hút quái và trảm kích chữ X xé toạc chiến trường gây <color=#E2E8F0><b>TD cực lớn</b></color> cùng hiệu ứng <color=#FF6B6B><b>Chảy Máu Sa Mạc</b></color>.",
                                     "Summons sand vortex and executes a devastating Cross-X Slash inflicting massive <color=#E2E8F0><b>TD</b></color> with continuous <color=#FF6B6B><b>Sand Bleed</b></color>."),
                baseCds[2], sCd, upDashCd, upUltiCd);

            bool isSelected = (currentSelected == CharacterManager.CHAR_DUNE_ASSASSIN);
            if (rosterActionBtnText != null) rosterActionBtnText.text = isSelected ? LocalizationManager.T("ĐANG CHỌN XUẤT TRẬN", "SELECTED FOR BATTLE") : LocalizationManager.T("CHỌN XUẤT TRẬN", "SELECT FOR BATTLE");
            if (rosterActionBtn != null)
            {
                rosterActionBtn.interactable = true;
                var img = rosterActionBtn.GetComponent<Image>();
                if (img != null) img.color = isSelected ? new Color(0.18f, 0.55f, 0.28f, 1f) : new Color(0.18f, 0.45f, 0.75f, 1f);
            }
        }
        else if (inspectedHeroId == "mage" || inspectedHeroId == "celestialmage" || inspectedHeroId == "player6")
        {
            if (rosterHeroPreviewImage != null && mageIdleFrames != null && mageIdleFrames.Length > 0)
                rosterHeroPreviewImage.sprite = mageIdleFrames[0];
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Pháp Sư Tinh Tú (Celestial Mage)", "Celestial Mage (Arcane Sorcerer)");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("<color=#E056FD>[PHÁP SƯ]</color> Tinh Vân Ma Pháp / Triệu Hồi Thiên Thạch & Hộ Thể", "<color=#E056FD>[MAGE]</color> Arcane Sorcery / Meteor Tempest & Barrier");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 90   CÔNG: 30   TỐC: 4.5", "HP: 90   ATK: 30   SPD: 4.5");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("BỘ KỸ NĂNG: TINH VÂN MA PHÁP", "SKILLSET: CELESTIAL ARCANE");

            SetHeroSkillCard(0, MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Attack),
                "AP",
                LocalizationManager.T("1. CẦU MA PHÁP & HỘ THỂ MA TRẬN", "1. ARCANE ORBS & BARRIER"),
                LocalizationManager.T("Bắn 3 cầu ma pháp tự dẫn đường gây <color=#B84DFF><b>AP</b></color> nổ sét lan. <color=#FFDF66>Nhấn Giữ:</color> Tạo khiên hấp thụ 80% sát thương, bộc phá đẩy lùi phản đòn <color=#B84DFF><b>AP</b></color> & hồi <color=#38BDF8><b>+10 Mana</b></color>!",
                                     "Fires 3 homing arcane orbs inflicting <color=#B84DFF><b>AP</b></color> with chain lightning. <color=#FFDF66>Hold attack:</color> Barrier absorbs 80% damage, bursts back enemies with <color=#B84DFF><b>AP</b></color> & restores <color=#38BDF8><b>+10 Mana</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("TỐC BIẾN", "BLINK"),
                LocalizationManager.T("2. TỐC BIẾN HƯ KHÔNG (BLINK)", "2. ARCANE BLINK DASH"),
                LocalizationManager.T("Dịch chuyển tức thời (<color=#55FF88><b>Bất tử</b></color>), để lại tàn ảnh ma pháp & hồi <color=#38BDF8><b>+12 Mana</b></color>.",
                                     "Instant warp (<color=#55FF88><b>Invulnerable</b></color>), leaves arcane clone & restores <color=#38BDF8><b>+12 Mana</b></color>."),
                baseCds[1], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Skill),
                "AP",
                LocalizationManager.T("3. THIÊN THẠCH MA TRẬN", "3. METEOR APOCALYPSE"),
                LocalizationManager.T("Mở đại pháp trận tinh tú triệu hồi 5 đợt mưa thiên thạch ma pháp gây <color=#B84DFF><b>AP diện rộng</b></color> toàn bộ kẻ địch trên bản đồ.",
                                     "Opens celestial circles summoning 5 waves of devastating meteor strikes inflicting catastrophic AoE <color=#B84DFF><b>AP</b></color> across the battlefield."),
                baseCds[2], sCd, upDashCd, upUltiCd);

            bool isSelected = (currentSelected == CharacterManager.CHAR_CELESTIAL_MAGE);
            if (rosterActionBtnText != null) rosterActionBtnText.text = isSelected ? LocalizationManager.T("ĐANG CHỌN XUẤT TRẬN", "SELECTED FOR BATTLE") : LocalizationManager.T("CHỌN XUẤT TRẬN", "SELECT FOR BATTLE");
            if (rosterActionBtn != null)
            {
                rosterActionBtn.interactable = true;
                var img = rosterActionBtn.GetComponent<Image>();
                if (img != null) img.color = isSelected ? new Color(0.18f, 0.55f, 0.28f, 1f) : new Color(0.18f, 0.45f, 0.75f, 1f);
            }
        }
        else
        {
            if (rosterHeroPreviewImage != null && vaelIdleFrames != null && vaelIdleFrames.Length > 0)
                rosterHeroPreviewImage.sprite = vaelIdleFrames[0];
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Hiệp Sĩ Vaelstrider (Balanced Knight)", "Knight Vaelstrider (Holy Vanguard)");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("<color=#FF4757>[ĐẤU SĨ]</color> Hiệp Sĩ Tiên Phong / Thánh Kiếm & Phản Đòn", "<color=#FF4757>[WARRIOR]</color> Holy Vanguard / Ancient Blade Master");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 100   CÔNG: 20   TỐC: 5.0", "HP: 100   ATK: 20   SPD: 5.0");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("BỘ KỸ NĂNG: THÁNH QUANG HIỆP SĨ", "SKILLSET: HOLY VANGUARD");

            SetHeroSkillCard(0, MobileActionButton.GetHeroActionIcon("vael", MobileActionButton.ButtonType.Attack),
                "ATK",
                LocalizationManager.T("1. ĐẠI KIẾM & THẾ THỦ PHẢN KÍCH", "1. ANCIENT SWORD & DIVINE PARRY"),
                LocalizationManager.T("Chém kiếm 3 nhịp gây <color=#FF3B30><b>ATK</b></color> (hồi <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66>Nhấn Giữ Thế Thủ:</color> Giảm 20% sát thương, thả tay bộc phá Thần Kiếm Phản Kích gây <color=#FF3B30><b>x3.2 Crit ATK</b></color>!",
                                     "3-hit combo dealing <color=#FF3B30><b>ATK</b></color> (restores <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66>Hold attack for Parry:</color> 20% DR, release to unleash Divine Riposte dealing <color=#FF3B30><b>x3.2 Crit ATK</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("vael", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("THÁNH THUẪN", "SHIELD"),
                LocalizationManager.T("2. THÁNH THUẪN PHẢN ĐÒN", "2. RADIANT RIPOSTE DASH"),
                LocalizationManager.T("Lướt né đòn (<color=#55FF88><b>Bất tử khi lướt</b></color>). Lướt qua quái/đỡ đòn sẽ <color=#FFAA33><b>Hồi 50% CD Lướt</b></color>, hồi <color=#38BDF8><b>+15 Mana</b></color> & phóng 3 Thánh Kiếm Khí gây <color=#FF3B30><b>ATK</b></color>!",
                                     "Holy aegis dash (<color=#55FF88><b>Invulnerable</b></color>). Deflecting attacks <color=#FFAA33><b>Refunds 50% Dash CD</b></color>, restores <color=#38BDF8><b>+15 Mana</b></color> & fires 3 Holy Waves with <color=#FF3B30><b>ATK</b></color>!"),
                baseCds[1], sCd, upDashCd, upUltiCd);

            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("vael", MobileActionButton.ButtonType.Skill),
                "ATK",
                LocalizationManager.T("3. THÁNH KIẾM THIÊN NHAM", "3. HOLY SWORD TEMPEST"),
                LocalizationManager.T("Xoay bão lốc kiếm 360° hút quái vật và triệu hồi Cự Kiếm Thiên Nham giáng thế gây <color=#FF3B30><b>ATK cực đại</b></color> diện rộng.",
                                     "360° sword vortex storm pulling enemies and summons a gigantic Holy Sword slamming down with massive AoE <color=#FF3B30><b>ATK</b></color>."),
                baseCds[2], sCd, upDashCd, upUltiCd);

            bool isSelected = (currentSelected == CharacterManager.CHAR_VAELSTRIDER);
            if (rosterActionBtnText != null) rosterActionBtnText.text = isSelected ? LocalizationManager.T("ĐANG CHỌN XUẤT TRẬN", "SELECTED FOR BATTLE") : LocalizationManager.T("CHỌN XUẤT TRẬN", "SELECT FOR BATTLE");
            if (rosterActionBtn != null)
            {
                rosterActionBtn.interactable = true;
                var img = rosterActionBtn.GetComponent<Image>();
                if (img != null) img.color = isSelected ? new Color(0.18f, 0.55f, 0.28f, 1f) : new Color(0.18f, 0.45f, 0.75f, 1f);
            }
        }

        if (rosterHeroPreviewImage != null)
        {
            rosterHeroPreviewImage.transform.localScale = GetHeroVisualScale(inspectedHeroId);
            RectTransform rt = rosterHeroPreviewImage.rectTransform;
            if (rt != null)
            {
                if (inspectedHeroId == "titan" || inspectedHeroId == "titanberserker" || inspectedHeroId == "player4")
                {
                    rt.anchoredPosition = new Vector2(0f, 14f);
                }
                else if (inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid" || inspectedHeroId == "elysia")
                {
                    rt.anchoredPosition = new Vector2(0f, 4f);
                }
                else if (inspectedHeroId == "vael" || inspectedHeroId == "vaelstrider")
                {
                    rt.anchoredPosition = new Vector2(0f, 14f);
                }
                else if (inspectedHeroId == "azure" || inspectedHeroId == "azureblade" || inspectedHeroId == "player3")
                {
                    rt.anchoredPosition = new Vector2(0f, 6f);
                }
                else if (inspectedHeroId == "dune" || inspectedHeroId == "duneassassin" || inspectedHeroId == "player5")
                {
                    rt.anchoredPosition = new Vector2(0f, 0f);
                }
                else if (inspectedHeroId == "mage" || inspectedHeroId == "celestialmage" || inspectedHeroId == "player6")
                {
                    rt.anchoredPosition = new Vector2(0f, 0f);
                }
                else
                {
                    rt.anchoredPosition = Vector2.zero;
                }
            }
            UpdateRosterEquipmentMaterial();
        }

        RefreshRosterSocketsAndBag();
        StartRosterHeroAnimation();
    }

    public void RefreshRosterSocketsAndBag()
    {
        RenderRosterHeroSockets();
        RenderRosterBagItems();
    }

    private void RenderRosterHeroSockets()
    {
        if (rosterHeroSocketSlotObjs == null || rosterHeroSocketSlotObjs.Count == 0) return;

        bool isVi = LocalizationManager.IsVietnamese;
        string hero = inspectedHeroId;

        // Calculate live stats with 6 sockets
        EquipmentSocketManager.GetTotalHeroSocketBonus(
            out int sAtk, out int sPhysAtk, out int sMagicAtk, out int sTrueAtk,
            out int sHp, out int sDef,
            out float sLife, out float sCd, out float sShard, out float sRegen,
            out float sMaxEn, out float sEnRegen,
            out float sPhysLife, out float sSpellVamp, out float sCrit,
            hero);

        int baseHp = 100;
        int baseAtk = 20;

        if (hero == "titan" || hero == "titanberserker" || hero == "player4")
        {
            baseHp = 200; baseAtk = 32;
        }
        else if (hero == "azure" || hero == "azureblade" || hero == "player3")
        {
            baseHp = 110; baseAtk = 24;
        }
        else if (hero == "ronin" || hero == "galeronin" || hero == "player2" || hero == "maid")
        {
            baseHp = 95; baseAtk = 28;
        }
        else if (hero == "dune" || hero == "duneassassin" || hero == "player5")
        {
            baseHp = 110; baseAtk = 18;
        }
        else if (hero == "mage" || hero == "celestialmage" || hero == "player6")
        {
            baseHp = 90; baseAtk = 30;
        }

        int upHp = PlayerUpgradeManager.EnsureInstance()?.GetHealthBonus(hero) ?? 0;
        int upAtk = PlayerUpgradeManager.EnsureInstance()?.GetAttackBonus(hero) ?? 0;
        float upDashCd = PlayerUpgradeManager.EnsureInstance()?.GetDashCooldownReduction(hero) ?? 0f;
        float upUltiCd = PlayerUpgradeManager.EnsureInstance()?.GetUltiCooldownReduction(hero) ?? 0f;

        int totalHp = baseHp + upHp + sHp;
        int totalAtk = baseAtk + upAtk + sAtk;

        if (rosterHeroStatsText != null)
        {
            string hpAdd = (upHp + sHp > 0) ? $" <color=#22C55E>(+{upHp + sHp})</color>" : "";
            string defAdd = sDef > 0 ? $" <color=#38BDF8>(+{sDef})</color>" : "";
            string regenStr = sRegen > 0f ? $"   <b>{(isVi ? "HỒI:" : "REGEN:")}</b> <color=#10B981>+{sRegen:0.#} HP/s</color>" : "";

            string atkAdd = (upAtk + sAtk > 0) ? $" <color=#FF3B30>(+{upAtk + sAtk})</color>" : "";

            string manaAdd = sMaxEn > 0 ? $" <color=#00E5FF>(+{(int)sMaxEn})</color>" : "";
            string manaRegenStr = sEnRegen > 0f ? $" <color=#06B6D4>(+{sEnRegen:0.#}/s)</color>" : "";

            // Phân loại sát thương gốc, trụ nâng cấp và ngọc khảm cho từng tướng
            bool isMagicHero = (hero == "azure" || hero == "azureblade" || hero == "player3" || hero == "mage" || hero == "celestialmage" || hero == "player6");
            int totalPhys = (!isMagicHero ? (baseAtk + upAtk) : 0) + sPhysAtk;
            int totalMagic = (isMagicHero ? (baseAtk + upAtk) : 0) + sMagicAtk;
            int totalTrue = sTrueAtk;

            List<string> dmgTags = new List<string>();
            if (totalPhys > 0) dmgTags.Add($"<color=#FF3B30>{totalPhys} ATK</color>");
            if (totalMagic > 0) dmgTags.Add($"<color=#B84DFF>{totalMagic} AP</color>");
            if (totalTrue > 0) dmgTags.Add($"<color=#E2E8F0>{totalTrue} TD</color>");
            string dmgBreakdownStr = string.Join(" | ", dmgTags);

            // Tính toán Chí mạng, Hút máu, Giảm hồi chiêu và Thưởng Shard
            int totalCritPct = 20 + Mathf.RoundToInt(sCrit * 100);
            string critBonusStr = sCrit > 0f ? $" <color=#F59E0B>(+{Mathf.RoundToInt(sCrit * 100)}%)</color>" : "";

            int physLife = Mathf.RoundToInt((sPhysLife > 0f ? sPhysLife : sLife) * 100);
            int spellVamp = Mathf.RoundToInt(sSpellVamp * 100);
            int cdPct = Mathf.RoundToInt(sCd * 100);
            int shardPct = Mathf.RoundToInt(sShard * 100);

            string cdTag = "";
            if (cdPct > 0) cdTag += $"-{cdPct}%";
            if (upDashCd > 0f)
            {
                string dashStr = isVi ? $"-{upDashCd:0.#}s Lướt" : $"-{upDashCd:0.#}s Dash";
                cdTag = string.IsNullOrEmpty(cdTag) ? dashStr : $"{cdTag} | {dashStr}";
            }
            if (string.IsNullOrEmpty(cdTag)) cdTag = "-0%";

            // Từng dòng thông số xếp thành hàng dọc với gạch đầu dòng rõ ràng, font lớn dễ đọc trên mobile
            List<string> statLines = new List<string>
            {
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "SINH LỰC (HP):" : "HEALTH (HP):")}</b> <color=#22C55E>{totalHp}</color>{hpAdd}{regenStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "GIÁP PHÒNG THỦ:" : "DEFENSE (DEF):")}</b> <color=#38BDF8>{sDef}</color>{defAdd}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "TỔNG CÔNG KÍCH:" : "TOTAL ATTACK:")}</b> <color=#FF3B30>{totalAtk}</color>{atkAdd}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "PHÂN LOẠI SÁT THƯƠNG:" : "DAMAGE TYPE:")}</b> {dmgBreakdownStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "CHÍ MẠNG / BẠO KÍCH:" : "CRITICAL STATS:")}</b> <color=#F59E0B>{totalCritPct}%</color>{critBonusStr} | <color=#F59E0B>x1.75</color>",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "NĂNG LƯỢNG (MANA):" : "ENERGY (MANA):")}</b> <color=#00E5FF>{100 + (int)sMaxEn}</color>{manaAdd}{manaRegenStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "HÚT MÁU / HỒI PHỤC:" : "VAMP & HEALING:")}</b> <color=#F43F5E>{physLife}% {(isVi ? "VL" : "Phys")}</color> | <color=#E879F9>{spellVamp}% {(isVi ? "Phép" : "Spell")}</color>",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "GIẢM COOLDOWN:" : "CD REDUCTION:")}</b> <color=#C084FC>{cdTag}</color>" + (shardPct > 0 ? $" | <b>SHARD:</b> <color=#FBBF24>+{shardPct}%</color>" : "")
            };

            rosterHeroStatsText.text = string.Join("\n", statLines);
        }

        for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT && s < rosterHeroSocketSlotObjs.Count; s++)
        {
            int slotIdx = s;
            var sObj = rosterHeroSocketSlotObjs[slotIdx];
            if (sObj == null) continue;

            // Clear dynamic children
            foreach (Transform ch in sObj.transform) Destroy(ch.gameObject);

            bool hasItem = EquipmentSocketManager.GetHeroSocketGem(slotIdx, out string gemId, out int itemLevel, hero);
            var sImg = sObj.GetComponent<Image>();
            var sOutline = sObj.GetComponent<Outline>();

            if (hasItem)
            {
                var itemDef = InventoryItemDatabase.GetItem(gemId);
                var gemEff = EquipmentSocketManager.GetGemEffect(gemId);
                Color rarityCol = gemEff.HasValue ? gemEff.Value.gemColor : Color.cyan;

                if (slotIdx < rosterSocketConduitLines.Count && rosterSocketConduitLines[slotIdx] != null)
                {
                    rosterSocketConduitLines[slotIdx].color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.90f);
                }

                if (sImg != null) sImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);
                if (sOutline != null)
                {
                    sOutline.effectColor = rarityCol;
                    sOutline.effectDistance = new Vector2(1.5f, -1.5f);
                }

                // Item Icon
                GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icObj.transform.SetParent(sObj.transform, false);
                RectTransform icRt = icObj.GetComponent<RectTransform>();
                icRt.anchorMin = Vector2.zero;
                icRt.anchorMax = Vector2.one;
                icRt.sizeDelta = new Vector2(-6f, -6f);
                var icImg = icObj.GetComponent<Image>();
                if (itemDef != null && itemDef.Icon != null) icImg.sprite = itemDef.Icon;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;

                // Level Badge (Góc dưới phải gọn gàng, nền tối mờ không che item)
                GameObject lvObj = new GameObject("LevelBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lvObj.transform.SetParent(sObj.transform, false);
                RectTransform lvRt = lvObj.GetComponent<RectTransform>();
                lvRt.anchorMin = new Vector2(1f, 0f);
                lvRt.anchorMax = new Vector2(1f, 0f);
                lvRt.pivot = new Vector2(1f, 0f);
                lvRt.anchoredPosition = new Vector2(-1f, 1f);
                lvRt.sizeDelta = new Vector2(30f, 14f);
                Image lvImg = lvObj.GetComponent<Image>();
                lvImg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);
                lvImg.raycastTarget = false;

                GameObject lvTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
                lvTxtObj.transform.SetParent(lvObj.transform, false);
                RectTransform lvtRt = lvTxtObj.GetComponent<RectTransform>();
                lvtRt.anchorMin = Vector2.zero; lvtRt.anchorMax = Vector2.one; lvtRt.sizeDelta = Vector2.zero;
                var lvTmp = lvTxtObj.AddComponent<TextMeshProUGUI>();
                lvTmp.text = $"Lv.{itemLevel}";
                lvTmp.fontSize = 10f;
                lvTmp.fontStyle = FontStyles.Bold;
                lvTmp.alignment = TextAlignmentOptions.Center;
                lvTmp.color = new Color(0.3f, 1f, 0.5f);
                lvTmp.raycastTarget = false;

                // Drag Drop Handler for Equipped Item
                var sDrag = sObj.GetComponent<UIDragDropItem>();
                if (sDrag == null) sDrag = sObj.AddComponent<UIDragDropItem>();
                sDrag.itemId = gemId;
                sDrag.itemIcon = itemDef?.Icon;
                sDrag.itemColor = rarityCol;
                sDrag.isSocketedGem = true;
                sDrag.sourceSocketIndex = slotIdx;
                sDrag.onDragOutOfSocket = (unslotIdx) =>
                {
                    EquipmentSocketManager.UnsocketHeroSlot(unslotIdx, inspectedHeroId);
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    InspectHero(inspectedHeroId);
                };
                sDrag.onMoveSocket = (fromS, toS) =>
                {
                    EquipmentSocketManager.MoveOrSwapHeroSlot(fromS, toS, inspectedHeroId);
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    InspectHero(inspectedHeroId);
                };
            }
            else
            {
                if (slotIdx < rosterSocketConduitLines.Count && rosterSocketConduitLines[slotIdx] != null)
                {
                    rosterSocketConduitLines[slotIdx].color = new Color(0.85f, 0.72f, 0.35f, 0.25f);
                }

                if (sImg != null) sImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
                if (sOutline != null)
                {
                    sOutline.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.5f);
                    sOutline.effectDistance = new Vector2(1f, -1f);
                }

                // Slot Label
                GameObject lblObj = new GameObject("SlotLabel", typeof(RectTransform), typeof(CanvasRenderer));
                lblObj.transform.SetParent(sObj.transform, false);
                RectTransform lblRt = lblObj.GetComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = new Vector2(2f, 2f);
                lblRt.offsetMax = new Vector2(-2f, -2f);
                var lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
                string sName = isVi ? EquipmentSocketManager.SlotNamesVi[slotIdx] : EquipmentSocketManager.SlotNamesEn[slotIdx];
                lblTmp.text = $"<size=10><color=#94A3B8><b>{sName}</b></color></size>\n<size=14><color=#64748B>+</color></size>";
                lblTmp.fontSize = 10f;
                lblTmp.fontStyle = FontStyles.Bold;
                lblTmp.alignment = TextAlignmentOptions.Center;
                lblTmp.lineSpacing = -8f;
                lblTmp.enableWordWrapping = false;
                lblTmp.overflowMode = TextOverflowModes.Truncate;
                lblTmp.raycastTarget = false;

                var sDrag = sObj.GetComponent<UIDragDropItem>();
                if (sDrag != null) Destroy(sDrag);
            }

            // Xóa khung dày Gothic đè che mất icon item
            Transform gfb = sObj.transform.Find("GothicFrameBorder");
            if (gfb != null) Destroy(gfb.gameObject);
        }
    }

    private void RenderRosterBagItems()
    {
        if (rosterBagContainer == null) return;

        foreach (var o in rosterBagItemCardObjs) if (o != null) Destroy(o);
        rosterBagItemCardObjs.Clear();

        int mergeCount = EquipmentSocketManager.GetTotalAvailableMergeGemsCount(inspectedHeroId);
        if (rosterAutoMergeBtnText != null)
        {
            string compactMerge = FormatCompactNumber(mergeCount);
            rosterAutoMergeBtnText.text = LocalizationManager.T(
                mergeCount > 0 ? $"HẤP THU TOÀN BỘ (+{compactMerge})" : "HẤP THU TOÀN BỘ",
                mergeCount > 0 ? $"AUTO MERGE ALL (+{compactMerge})" : "AUTO MERGE ALL");
            if (rosterAutoMergeBtn != null)
            {
                rosterAutoMergeBtn.interactable = (mergeCount > 0);
            }
        }

        var invItems = AccountDatabase.GetActiveUserInventory();
        List<AccountDatabase.InventoryItemEntry> validItems = new List<AccountDatabase.InventoryItemEntry>();
        foreach (var it in invItems)
        {
            if (it.quantity > 0 && EquipmentSocketManager.IsGem(it.itemId))
            {
                validItems.Add(it);
            }
        }

        if (validItems.Count == 0)
        {
            GameObject emptyMsgObj = new GameObject("EmptyBagMsg", typeof(RectTransform), typeof(CanvasRenderer));
            Transform parentContainer = (rosterBagContainer.parent != null) ? rosterBagContainer.parent : rosterBagContainer;
            emptyMsgObj.transform.SetParent(parentContainer, false);
            rosterBagItemCardObjs.Add(emptyMsgObj);
            RectTransform emRt = emptyMsgObj.GetComponent<RectTransform>();
            emRt.anchorMin = Vector2.zero;
            emRt.anchorMax = Vector2.one;
            emRt.pivot = new Vector2(0.5f, 0.5f);
            emRt.offsetMin = new Vector2(40f, 10f);
            emRt.offsetMax = new Vector2(-40f, -10f);
            var emTmp = emptyMsgObj.AddComponent<TextMeshProUGUI>();
            emTmp.text = LocalizationManager.T("Chưa có khoáng thạch / đá quý trong túi đồ.", "No minerals or gems in bag yet.");
            emTmp.fontSize = 19f;
            emTmp.fontStyle = FontStyles.Bold;
            emTmp.color = new Color(0.75f, 0.80f, 0.90f);
            emTmp.alignment = TextAlignmentOptions.Center;
            return;
        }

        for (int i = 0; i < validItems.Count; i++)
        {
            var itemEntry = validItems[i];
            var def = InventoryItemDatabase.GetItem(itemEntry.itemId);
            var gemEff = EquipmentSocketManager.GetGemEffect(itemEntry.itemId);
            if (def == null || !gemEff.HasValue) continue;

            GameObject cardObj = new GameObject($"BagCard_{itemEntry.itemId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(rosterBagContainer, false);
            rosterBagItemCardObjs.Add(cardObj);

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(250f, 106f);

            Image cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
            WhiteIconLoader.ApplyGothicFrame(cardObj, gemEff.Value.gemColor * 0.85f, 0.45f);

            var drag = cardObj.AddComponent<UIDragDropItem>();
            drag.itemId = itemEntry.itemId;
            drag.itemIcon = def.Icon;
            drag.itemColor = gemEff.Value.gemColor;
            drag.parentScrollRect = rosterBagContainer.GetComponentInParent<ScrollRect>();
            string targetGemId = itemEntry.itemId;
            drag.onClick = () =>
            {
                if (EquipmentSocketManager.SocketGemToHeroSlot(-1, targetGemId, out _, out _, inspectedHeroId))
                {
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    InspectHero(inspectedHeroId);
                }
            };

            // Icon box (Enlarged for Mobile)
            GameObject ibObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ibObj.transform.SetParent(cardObj.transform, false);
            RectTransform ibRt = ibObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(8f, 0f);
            ibRt.sizeDelta = new Vector2(58f, 58f);
            var ibImg = ibObj.GetComponent<Image>();
            ibImg.sprite = def.Icon;
            ibImg.preserveAspect = true;
            ibImg.raycastTarget = false;

            // Text info (Enlarged for Mobile)
            GameObject infoObj = new GameObject("Info", typeof(RectTransform), typeof(CanvasRenderer));
            infoObj.transform.SetParent(cardObj.transform, false);
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchorMin = Vector2.zero;
            infoRt.anchorMax = Vector2.one;
            infoRt.offsetMin = new Vector2(72f, 4f);
            infoRt.offsetMax = new Vector2(-6f, -4f);

            var vlg = infoObj.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.spacing = 2f;

            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            tObj.transform.SetParent(infoObj.transform, false);
            var tTmp = tObj.AddComponent<TextMeshProUGUI>();
            string gName = LocalizationManager.IsVietnamese ? gemEff.Value.nameVi : gemEff.Value.nameEn;
            tTmp.text = $"<b>{gName}</b> <color=#00FF88>x{itemEntry.quantity}</color>";
            tTmp.fontSize = 15.5f;
            tTmp.color = Color.white;
            tTmp.raycastTarget = false;

            GameObject bObj = new GameObject("Buff", typeof(RectTransform), typeof(CanvasRenderer));
            bObj.transform.SetParent(infoObj.transform, false);
            var bTmp = bObj.AddComponent<TextMeshProUGUI>();
            string effSum = LocalizationManager.IsVietnamese ? gemEff.Value.effectSummaryVi : gemEff.Value.effectSummaryEn;
            bTmp.text = EquipmentSocketManager.FormatColoredStatSummary(effSum);
            bTmp.fontSize = 13.5f;
            bTmp.lineSpacing = 1.2f;
            bTmp.color = Color.white;
            bTmp.enableWordWrapping = true;
            bTmp.raycastTarget = false;
        }
    }

    private Material rosterHeroMat;

    public void UpdateRosterEquipmentMaterial()
    {
        if (rosterHeroPreviewImage == null) return;

        HeroCostumeManager.ApplyCostumeToImage(rosterHeroPreviewImage, inspectedHeroId);

        if (inspectedHeroId == "maid" || inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "azure" || inspectedHeroId == "azureblade" || inspectedHeroId == "titan" || inspectedHeroId == "titanberserker" || inspectedHeroId == "player4" || inspectedHeroId == "dune" || inspectedHeroId == "duneassassin" || inspectedHeroId == "player5" || inspectedHeroId == "mage" || inspectedHeroId == "celestialmage" || inspectedHeroId == "player6")
        {
            rosterHeroPreviewImage.material = null;
            return;
        }

        if (rosterHeroMat == null)
        {
            Shader s = Shader.Find("Custom/CharacterEquipmentShader");
            if (s != null) rosterHeroMat = new Material(s);
        }

        if (rosterHeroMat != null)
        {
            rosterHeroPreviewImage.material = rosterHeroMat;

            int helmetId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Helmet);
            int armorId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Armor);
            int pantsId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Pants);
            int glovesId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Gloves);
            int bootsId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Boots);
            int cloakId = ArmorShopUI.GetEquippedIdForSlot(ArmorShopUI.EquipmentSlot.Cloak);

            Color armorPri = PlayerEquipmentVisual.GetItemPrimaryColor(armorId > 0 ? armorId : 1, new Color(0.77f, 0.14f, 0.19f, 1f));
            Color armorSec = PlayerEquipmentVisual.GetItemSecondaryColor(armorId > 0 ? armorId : 1, new Color(0.54f, 0.12f, 0.17f, 1f));
            rosterHeroMat.SetColor("_ArmorColor", armorPri);
            rosterHeroMat.SetColor("_ArmorSecColor", armorSec);

            rosterHeroMat.SetColor("_CloakColor", cloakId > 0 ? PlayerEquipmentVisual.GetItemPrimaryColor(cloakId, new Color(0.85f, 0.2f, 0.2f, 1f)) : new Color(0.85f, 0.2f, 0.2f, 1f));
            rosterHeroMat.SetColor("_PantsColor", pantsId > 0 ? PlayerEquipmentVisual.GetItemPrimaryColor(pantsId, new Color(0.24f, 0.24f, 0.24f, 1f)) : new Color(0.24f, 0.24f, 0.24f, 1f));
            rosterHeroMat.SetColor("_GlovesColor", glovesId > 0 ? PlayerEquipmentVisual.GetItemPrimaryColor(glovesId, new Color(0.85f, 0.85f, 0.85f, 1f)) : new Color(0.85f, 0.85f, 0.85f, 1f));
            rosterHeroMat.SetColor("_BootsColor", bootsId > 0 ? PlayerEquipmentVisual.GetItemPrimaryColor(bootsId, new Color(0.36f, 0.17f, 0.15f, 1f)) : new Color(0.36f, 0.17f, 0.15f, 1f));
            rosterHeroMat.SetColor("_HelmetColor", helmetId > 0 ? PlayerEquipmentVisual.GetItemPrimaryColor(helmetId, new Color(0.54f, 0.28f, 0.21f, 1f)) : new Color(0.54f, 0.28f, 0.21f, 1f));
        }
    }

    private void OnRosterActionClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        string targetChar = CharacterManager.CHAR_VAELSTRIDER;
        string heroPref = "vael";

        if (inspectedHeroId == "titan" || inspectedHeroId == "titanberserker" || inspectedHeroId == "player4")
        {
            targetChar = CharacterManager.CHAR_TITAN_BERSERKER;
            heroPref = "titan";
        }
        else if (inspectedHeroId == "azure" || inspectedHeroId == "azureblade")
        {
            targetChar = CharacterManager.CHAR_AZURE_BLADE;
            heroPref = "azure";
        }
        else if (inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid")
        {
            targetChar = CharacterManager.CHAR_GALE_RONIN;
            heroPref = "ronin";
        }
        else if (inspectedHeroId == "dune" || inspectedHeroId == "duneassassin" || inspectedHeroId == "player5")
        {
            targetChar = CharacterManager.CHAR_DUNE_ASSASSIN;
            heroPref = "dune";
        }
        else if (inspectedHeroId == "mage" || inspectedHeroId == "celestialmage" || inspectedHeroId == "player6")
        {
            targetChar = CharacterManager.CHAR_CELESTIAL_MAGE;
            heroPref = "mage";
        }

        CharacterManager.EnsureInstance().SelectCharacter(targetChar);
        PlayerPrefs.SetString("SelectedHero", heroPref);
        PlayerPrefs.Save();

        AccountDatabase.SaveActiveSessionToDatabase();

        InspectHero(inspectedHeroId);
        UpdateAllTexts();
    }

    public static string FormatCompactNumber(long val)
    {
        if (val >= 1_000_000_000) return (val / 1_000_000_000.0).ToString("0.##") + "B";
        if (val >= 1_000_000) return (val / 1_000_000.0).ToString("0.##") + "M";
        if (val >= 10_000) return (val / 1_000.0).ToString("0.#") + "K";
        return val.ToString("N0");
    }

    public void UpdateCurrencyDisplay()
    {
        long shards = AccountDatabase.GetAethelShards();
        string formattedShards = FormatCompactNumber(shards);

        if (shardsTopText != null)
        {
            shardsTopText.text = $"{LocalizationManager.Get("LBL_SHARDS")}: {formattedShards}";
            shardsTopText.enableWordWrapping = false;
            shardsTopText.overflowMode = TextOverflowModes.Overflow;
            shardsTopText.gameObject.SetActive(true);
        }
        if (potionsTopText != null)
        {
            potionsTopText.gameObject.SetActive(false);
        }
        if (aethelShardsDisplay != null)
        {
            aethelShardsDisplay.text = $"{LocalizationManager.Get("LBL_SHARDS")}: {formattedShards}";
            aethelShardsDisplay.enableWordWrapping = false;
            aethelShardsDisplay.overflowMode = TextOverflowModes.Overflow;
        }
    }

    public void SelectCharacter(string heroId)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        string target = CharacterManager.CHAR_VAELSTRIDER;
        string heroPref = "vael";

        if (heroId.ToLower() == "titan" || heroId.ToLower() == "titanberserker" || heroId.ToLower() == "player4")
        {
            target = CharacterManager.CHAR_TITAN_BERSERKER;
            heroPref = "titan";
        }
        else if (heroId.ToLower() == "azure" || heroId.ToLower() == "azureblade")
        {
            target = CharacterManager.CHAR_AZURE_BLADE;
            heroPref = "azure";
        }
        else if (heroId.ToLower() == "ronin" || heroId.ToLower() == "galeronin" || heroId.ToLower() == "player2" || heroId.ToLower() == "maid")
        {
            target = CharacterManager.CHAR_GALE_RONIN;
            heroPref = "ronin";
        }
        else if (heroId.ToLower() == "dune" || heroId.ToLower() == "duneassassin" || heroId.ToLower() == "player5")
        {
            target = CharacterManager.CHAR_DUNE_ASSASSIN;
            heroPref = "dune";
        }
        else if (heroId.ToLower() == "mage" || heroId.ToLower() == "celestialmage" || heroId.ToLower() == "player6")
        {
            target = CharacterManager.CHAR_CELESTIAL_MAGE;
            heroPref = "mage";
        }

        CharacterManager.EnsureInstance().SelectCharacter(target);
        PlayerPrefs.SetString("SelectedHero", heroPref);
        PlayerPrefs.Save();
        AccountDatabase.SaveActiveSessionToDatabase();
        UpdateHeroDetails();
        StartMainHeroAnimation();
    }

    private void StartMainHeroAnimation()
    {
        if (mainHeroAnimCoroutine != null) StopCoroutine(mainHeroAnimCoroutine);
        mainHeroAnimCoroutine = StartCoroutine(AnimateMainHeroRoutine());
    }

    private IEnumerator AnimateMainHeroRoutine()
    {
        string curChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        Sprite[] frames = vaelIdleFrames;
        if (curChar == CharacterManager.CHAR_TITAN_BERSERKER) frames = titanIdleFrames;
        else if (curChar == CharacterManager.CHAR_AZURE_BLADE) frames = azureIdleFrames;
        else if (curChar == CharacterManager.CHAR_GALE_RONIN) frames = (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames;
        else if (curChar == CharacterManager.CHAR_DUNE_ASSASSIN) frames = duneIdleFrames;
        else if (curChar == CharacterManager.CHAR_CELESTIAL_MAGE) frames = mageIdleFrames;

        if (heroPreviewImage == null) yield break;

        heroPreviewImage.preserveAspect = true;
        heroPreviewImage.color = Color.white;
        heroPreviewImage.transform.localScale = GetHeroVisualScale(curChar == CharacterManager.CHAR_TITAN_BERSERKER ? "titan" : (curChar == CharacterManager.CHAR_AZURE_BLADE ? "azure" : (curChar == CharacterManager.CHAR_GALE_RONIN ? "ronin" : (curChar == CharacterManager.CHAR_DUNE_ASSASSIN ? "dune" : (curChar == CharacterManager.CHAR_CELESTIAL_MAGE ? "mage" : "vael")))));
        RectTransform mainRt = heroPreviewImage.rectTransform;
        if (mainRt != null)
        {
            if (curChar == CharacterManager.CHAR_TITAN_BERSERKER)
            {
                mainRt.anchoredPosition = new Vector2(0f, 14f);
            }
            else if (curChar == CharacterManager.CHAR_GALE_RONIN || curChar == CharacterManager.CHAR_MAID)
            {
                mainRt.anchoredPosition = new Vector2(0f, 4f);
            }
            else if (curChar == CharacterManager.CHAR_VAELSTRIDER)
            {
                mainRt.anchoredPosition = new Vector2(0f, 14f);
            }
            else if (curChar == CharacterManager.CHAR_AZURE_BLADE)
            {
                mainRt.anchoredPosition = new Vector2(0f, 6f);
            }
            else if (curChar == CharacterManager.CHAR_DUNE_ASSASSIN)
            {
                mainRt.anchoredPosition = new Vector2(0f, 0f);
            }
            else if (curChar == CharacterManager.CHAR_CELESTIAL_MAGE)
            {
                mainRt.anchoredPosition = new Vector2(0f, 0f);
            }
            else
            {
                mainRt.anchoredPosition = Vector2.zero;
            }
        }

        if (frames == null || frames.Length == 0)
        {
            EnsureHeroSprites();
            frames = (curChar == CharacterManager.CHAR_TITAN_BERSERKER)
                ? titanIdleFrames
                : ((curChar == CharacterManager.CHAR_AZURE_BLADE) 
                    ? azureIdleFrames 
                    : (curChar == CharacterManager.CHAR_GALE_RONIN ? ((roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames) : (curChar == CharacterManager.CHAR_DUNE_ASSASSIN ? duneIdleFrames : (curChar == CharacterManager.CHAR_CELESTIAL_MAGE ? mageIdleFrames : vaelIdleFrames))));
            if (frames == null || frames.Length == 0) yield break;
        }

        while (true)
        {
            mainAnimFrame = (mainAnimFrame + 1) % frames.Length;
            if (frames[mainAnimFrame] != null)
            {
                heroPreviewImage.sprite = frames[mainAnimFrame];
            }
            yield return new WaitForSeconds(0.15f);
        }
    }

    private void StartRosterHeroAnimation()
    {
        if (rosterHeroAnimCoroutine != null) StopCoroutine(rosterHeroAnimCoroutine);
        rosterHeroAnimCoroutine = StartCoroutine(AnimateRosterHeroRoutine());
    }

    private IEnumerator AnimateRosterHeroRoutine()
    {
        Sprite[] frames = vaelIdleFrames;
        if (inspectedHeroId == "titan" || inspectedHeroId == "titanberserker" || inspectedHeroId == "player4")
            frames = titanIdleFrames;
        else if (inspectedHeroId == "azure" || inspectedHeroId == "azureblade") 
            frames = azureIdleFrames;
        else if (inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid") 
            frames = (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames;
        else if (inspectedHeroId == "dune" || inspectedHeroId == "duneassassin" || inspectedHeroId == "player5")
            frames = duneIdleFrames;
        else if (inspectedHeroId == "mage" || inspectedHeroId == "celestialmage" || inspectedHeroId == "player6")
            frames = mageIdleFrames;

        if (rosterHeroPreviewImage == null) yield break;

        rosterHeroPreviewImage.preserveAspect = true;
        rosterHeroPreviewImage.color = Color.white;

        if (frames == null || frames.Length == 0)
        {
            EnsureHeroSprites();
            frames = (inspectedHeroId == "titan" || inspectedHeroId == "titanberserker" || inspectedHeroId == "player4")
                ? titanIdleFrames
                : ((inspectedHeroId == "azure" || inspectedHeroId == "azureblade") 
                    ? azureIdleFrames 
                    : ((inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid") ? ((roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames) : (inspectedHeroId == "dune" || inspectedHeroId == "duneassassin" || inspectedHeroId == "player5" ? duneIdleFrames : (inspectedHeroId == "mage" || inspectedHeroId == "celestialmage" || inspectedHeroId == "player6" ? mageIdleFrames : vaelIdleFrames))));
            if (frames == null || frames.Length == 0) yield break;
        }

        while (true)
        {
            rosterAnimFrame = (rosterAnimFrame + 1) % frames.Length;
            if (frames[rosterAnimFrame] != null)
            {
                rosterHeroPreviewImage.sprite = frames[rosterAnimFrame];
            }
            yield return new WaitForSeconds(0.15f);
        }
    }

    public void OnPlayClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        AccountDatabase.SaveActiveSessionToDatabase();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("Map1_Thestart");
    }

    public void OnMapSelectClicked()
    {
        OnPlayClicked();
    }

    public void OnRosterClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        EnsureRosterPanel();
        ShowRosterPanel();
    }

    public void OnSkillsClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        var invUI = InventoryUI.EnsureUIExists();
        if (invUI != null)
        {
            invUI.OpenGemSocketModal("skills");
        }
    }

    public void OnInventoryClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        var invUI = InventoryUI.EnsureUIExists();
        if (invUI != null)
        {
            invUI.Open();
        }
    }

    public void OnShopClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        var armorShop = ArmorShopUI.EnsureUIExists();
        if (armorShop != null)
        {
            armorShop.Open();
        }
    }

    public void OnSettingsClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();
        var s = SettingsUI.Instance != null ? SettingsUI.Instance : SettingsUI.EnsureUIExists();
        if (s != null)
        {
            s.OpenSettings();
        }
    }

    public void OnLogoutClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        AccountDatabase.SaveActiveSessionToDatabase();
        SceneManager.LoadScene("LoginScene");
    }

    public void OnExitClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowMainPanel()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (mainPanel != null) mainPanel.SetActive(true);
        HideCenterHeroDisplay();
        if (mapSelectPanel != null) mapSelectPanel.SetActive(false);
        if (rosterPanel != null) rosterPanel.SetActive(false);

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            var r = canvas.transform.Find("RosterModalPanel");
            if (r != null) r.gameObject.SetActive(false);
            var hr = canvas.transform.Find("HeroRosterModal");
            if (hr != null) hr.gameObject.SetActive(false);
            var ms = canvas.transform.Find("MapSelectModalPanel");
            if (ms != null) ms.gameObject.SetActive(false);
            var inv = canvas.transform.Find("InventoryModalPanel");
            if (inv != null) inv.gameObject.SetActive(false);
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (SettingsUI.Instance != null) SettingsUI.Instance.CloseSettings();
        if (InventoryUI.Instance != null) InventoryUI.Instance.Close();
        if (ArmorShopUI.Instance != null) ArmorShopUI.Instance.Close();
        UpdateCurrencyDisplay();
    }

    public void ShowMapSelectPanel()
    {
        EnsureMapSelectPanel();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (mapSelectPanel != null)
        {
            mapSelectPanel.SetActive(true);
            mapSelectPanel.transform.SetAsLastSibling();
        }
        if (rosterPanel != null) rosterPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        UpdateMapCardsLockState();
        UpdateAllTexts();
    }

    public void UpdateMapCardsLockState()
    {
        bool map1Completed = PlayerPrefs.GetInt("Map1_Completed", 0) == 1;
        bool map2Completed = PlayerPrefs.GetInt("Map2_Completed", 0) == 1;

        // Map 1: Luôn mở
        SetMapCardState(map1Button, true, LocalizationManager.Get("BTN_PLAY"));

        // Map 2: Cần vượt Map 1
        if (map1Completed)
        {
            SetMapCardState(map2Button, true, LocalizationManager.Get("BTN_PLAY"));
        }
        else
        {
            SetMapCardState(map2Button, false, LocalizationManager.T("KHÓA (VƯỢT MAP 1)", "LOCKED (CLEAR MAP 1)"));
        }

        // Map 3: Cần vượt Map 2
        if (map2Completed)
        {
            SetMapCardState(map3Button, true, LocalizationManager.Get("BTN_PLAY"));
        }
        else
        {
            SetMapCardState(map3Button, false, LocalizationManager.T("KHÓA (VƯỢT MAP 2)", "LOCKED (CLEAR MAP 2)"));
        }
    }

    private void SetMapCardState(Button btn, bool unlocked, string text)
    {
        if (btn == null) return;
        btn.interactable = unlocked;

        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = unlocked ? new Color(0.72f, 0.22f, 0.18f, 1f) : new Color(0.2f, 0.22f, 0.26f, 0.85f);
        }

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = unlocked ? Color.white : new Color(0.6f, 0.65f, 0.7f, 0.8f);
        }

        var outline = btn.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = unlocked ? new Color(1f, 0.85f, 0.3f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.3f);
        }
    }

    public void ShowRosterPanel()
    {
        EnsureRosterPanel();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (mapSelectPanel != null) mapSelectPanel.SetActive(false);
        if (rosterPanel != null)
        {
            rosterPanel.SetActive(true);
            rosterPanel.transform.SetAsLastSibling();
        }
        if (settingsPanel != null) settingsPanel.SetActive(false);
        InspectHero("vael");
        UpdateAllTexts();
    }

    public void LoadMap(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName == "SampleScene") sceneName = "Map1_Thestart";

        PlayerInventory.ResetRunShards();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneName);
    }
}
