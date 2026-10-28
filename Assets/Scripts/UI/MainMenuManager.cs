using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

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

    [Header("Roster 4-Skill Breakdown Cards (Passive + 3 Skills)")]
    [SerializeField] private Image rosterSkill0IconImg;
    [SerializeField] private TextMeshProUGUI rosterSkill0TagText;
    [SerializeField] private TextMeshProUGUI rosterSkill0CdText;
    [SerializeField] private TextMeshProUGUI rosterSkill0TitleText;
    [SerializeField] private TextMeshProUGUI rosterSkill0DescText;

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

    public enum RosterDetailsTab { Skills, Gear }

    [Header("Roster 2-Tab Navigation")]
    private RosterDetailsTab currentRosterDetailsTab = RosterDetailsTab.Skills;
    private GameObject rosterSkillsTabContent;
    private GameObject rosterGearTabContent;
    private Button btnRosterTabSkills;
    private Button btnRosterTabGear;
    private Image imgRosterTabSkillsBg;
    private Image imgRosterTabGearBg;
    private TextMeshProUGUI txtRosterTabSkills;
    private TextMeshProUGUI txtRosterTabGear;
    [SerializeField] private TextMeshProUGUI rosterHeroQuickStatsText;

    [Header("Roster 6-Socket & Bag System")]
    private List<GameObject> rosterHeroSocketSlotObjs = new List<GameObject>();
    private List<Image> rosterSocketConduitLines = new List<Image>();
    private List<GameObject> rosterBagItemCardObjs = new List<GameObject>();
    [SerializeField] private TextMeshProUGUI rosterBagSectionHeaderText;
    [SerializeField] private Button rosterAutoMergeBtn;
    [SerializeField] private TextMeshProUGUI rosterAutoMergeBtnText;
    [SerializeField] private Transform rosterBagContainer;
    [SerializeField] private TextMeshProUGUI rosterGemEffectsText;

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

    [Header("Hero Select Carousel Modal")]
    private GameObject heroSelectCarouselModal;
    private Image carouselAvatarImg;
    private TextMeshProUGUI carouselHeroNameText;
    private TextMeshProUGUI carouselHeroRoleText;
    private TextMeshProUGUI carouselHeroStatsText;
    private TextMeshProUGUI carouselHeroSkillText;
    private TextMeshProUGUI carouselCounterText;
    private Button carouselPrevBtn;
    private Button carouselNextBtn;
    private Button carouselConfirmBtn;
    private TextMeshProUGUI carouselConfirmBtnText;
    private Button carouselCloseBtn;
    private int currentCarouselIdx = 0;

    // Next-Gen Showcase Arena fields
    private Image carouselHeroStageImage;
    private Image carouselStageHaloGlow;
    private Coroutine carouselHeroAnimCoroutine;
    private int carouselAnimFrame = 0;
    private TextMeshProUGUI carouselTopCurrencyText;
    private Image carouselHpBarFill;
    private TextMeshProUGUI carouselHpValText;
    private Image carouselAtkBarFill;
    private TextMeshProUGUI carouselAtkValText;
    private TextMeshProUGUI carouselAtkLabelText;
    private Image carouselDefBarFill;
    private TextMeshProUGUI carouselDefValText;
    private TextMeshProUGUI carouselDefLabelText;
    private Image carouselSpdBarFill;
    private TextMeshProUGUI carouselSpdValText;
    private TextMeshProUGUI carouselSkillNormalTitle;
    private TextMeshProUGUI carouselSkillNormalDesc;
    private TextMeshProUGUI carouselSkillGuardTitle;
    private TextMeshProUGUI carouselSkillGuardDesc;
    private TextMeshProUGUI carouselSkillUltiTitle;
    private TextMeshProUGUI carouselSkillUltiDesc;
    private readonly List<GameObject> carouselRosterCardObjs = new List<GameObject>();
    private readonly List<Outline> carouselRosterCardOutlines = new List<Outline>();
    private readonly List<Image> carouselRosterCardBgs = new List<Image>();

    private readonly string[] carouselHeroIds = new string[]
    {
        CharacterManager.CHAR_VAELSTRIDER,
        CharacterManager.CHAR_GALE_RONIN,
        CharacterManager.CHAR_AZURE_BLADE,
        CharacterManager.CHAR_TITAN_BERSERKER,
        CharacterManager.CHAR_DUNE_ASSASSIN,
        CharacterManager.CHAR_CELESTIAL_MAGE
    };

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
        CorruptedCycleManager.ResetToNormalCycle();
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

    private void Update()
    {
        if (heroSelectCarouselModal != null && heroSelectCarouselModal.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                CarouselPrevHero();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                CarouselNextHero();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                OnCarouselConfirmClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseHeroSelectModal();
            }
        }
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

        // 2. Play Button (Xuất Trận) - Centered at bottom
        float playWidth = 240f;
        float playHeight = 60f;
        float playPosY = 56f;

        playButton = EnsureNavButton(navParent, playButton, "Btn_Play", LocalizationManager.Get("BTN_PLAY"), new Vector2(0f, playPosY), new Vector2(playWidth, playHeight), new Color(0.85f, 0.55f, 0.15f, 1f), out playBtnText);
        if (playButton != null)
        {
            playButton.gameObject.SetActive(true);
            RectTransform pRt = playButton.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0f);
            pRt.anchorMax = new Vector2(0.5f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, playPosY);
            pRt.sizeDelta = new Vector2(playWidth, playHeight);
        }

        // Hide Hero, Inventory, Shop, Skills buttons as requested
        if (rosterButton != null) rosterButton.gameObject.SetActive(false);
        if (inventoryButton != null) inventoryButton.gameObject.SetActive(false);
        if (shopButton != null) shopButton.gameObject.SetActive(false);
        if (skillButton != null) skillButton.gameObject.SetActive(false);

        string[] hideNames = new string[] { "Btn_Hero", "Btn_Roster", "Btn_Inventory", "Btn_Shop", "Btn_Skills", "Btn_Skill" };
        foreach (var hName in hideNames)
        {
            var t = navParent.Find(hName);
            if (t != null) t.gameObject.SetActive(false);
            if (mainPanel != null)
            {
                var mt = mainPanel.transform.Find(hName);
                if (mt != null) mt.gameObject.SetActive(false);
            }
        }

        // Move Settings Button to Top-Right Corner
        Transform topParent = mainPanel != null ? mainPanel.transform : navParent;
        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas != null) topParent = rootCanvas.transform;

        settingsButton = EnsureNavButton(topParent, settingsButton, "Btn_Settings", "", Vector2.zero, new Vector2(50f, 50f), new Color(0.12f, 0.16f, 0.24f, 0.94f), out settingsBtnText);
        if (settingsButton != null)
        {
            settingsButton.transform.SetParent(topParent, false);
            RectTransform sRt = settingsButton.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 1f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot = new Vector2(1f, 1f);
            sRt.anchoredPosition = new Vector2(-28f, -28f);
            sRt.sizeDelta = new Vector2(50f, 50f);
            settingsButton.gameObject.SetActive(true);
            WhiteIconLoader.ReplaceTextWithIcon(settingsButton, "Gear", new Color(1f, 0.88f, 0.45f), new Vector2(30f, 30f));
        }

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
        if (name == "Btn_Settings")
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-28f, -28f);
            rt.sizeDelta = size;
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        Image img = btnObj.GetComponent<Image>();
        if (img == null) img = btnObj.AddComponent<Image>();
        img.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var outline = btnObj.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

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
        btnTxt.color = new Color(0.96f, 0.88f, 0.65f, 1f); // Vàng kim cổ Soulslike
        SoulslikeHUDStyler.SetSafeOutline(btnTxt, 0.35f, Color.black);
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

            Transform skill1Card = existing.Find("RosterBox/ShowcaseDetailsPanel/Tab_SkillsContent/SkillBox/SkillCard_0_v9_TwoTabs");
            Transform twoTabs = existing.Find("RosterBox/ShowcaseDetailsPanel/HeaderTabsRow/TabBtn_Skills");
            bool hasMissingSkill1Card = skill1Card == null || twoTabs == null;
            bool hasAccidentalOverlayBtn = existing.GetComponent<Button>() != null;

            if (boxRtExisting == null || boxRtExisting.anchorMax != Vector2.one || rosterHeroNameText == null || rosterSkill1TitleText == null || rosterSkill1CdText == null || hasOldSkillFrame || hasOldBtnFrame || hasOldMaid || hasMissingTitanSprite || hasMissingDuneCard || hasMissingSkill1Card || hasAccidentalOverlayBtn)
            {
                existing.name = "__OldRosterModal_PendingDestroy__";
                existing.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
                existing = null;
                rosterPanel = null;
            }
            else
            {
                rosterPanel = existing.gameObject;
                Transform cbT = existing.Find("RosterBox/Btn_CloseTopRight");
                if (cbT != null)
                {
                    Button cb = cbT.GetComponent<Button>();
                    if (cb != null)
                    {
                        cb.interactable = true;
                        cb.onClick.RemoveAllListeners();
                        cb.onClick.AddListener(CloseRosterPanel);
                        rosterBackBtn = cb;
                    }
                    AttachPointerClick(cbT.gameObject, CloseRosterPanel);
                    foreach (var img in cbT.GetComponentsInChildren<Image>(true))
                    {
                        if (img.gameObject != cbT.gameObject) img.raycastTarget = false;
                    }
                    cbT.SetAsLastSibling();
                }
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
        closeRt.anchoredPosition = new Vector2(-28f, -28f);
        closeRt.sizeDelta = new Vector2(52f, 52f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.80f, 0.22f, 0.25f, 1f);
        closeImg.raycastTarget = true;

        WhiteIconLoader.ApplyGothicFrame(closeBtnObj, new Color(1f, 0.85f, 0.4f, 0.95f), 0.5f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.interactable = true;
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(CloseRosterPanel);
        AttachPointerClick(closeBtnObj, CloseRosterPanel);
        WhiteIconLoader.ReplaceTextWithIcon(closeBtn, "Cross", Color.white, new Vector2(26f, 26f));

        foreach (var img in closeBtnObj.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject != closeBtnObj) img.raycastTarget = false;
        }

        rosterBackBtn = closeBtn;
        rosterBackBtnText = null;

        // 4. LEFT COLUMN: Hero Selection List (Full-Height Split Left Column)
        GameObject leftCol = new GameObject("HeroSelectionColumn", typeof(RectTransform));
        leftCol.transform.SetParent(dialogBox.transform, false);
        RectTransform lRt = leftCol.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(0.33f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.offsetMin = new Vector2(20f, 20f);
        lRt.offsetMax = new Vector2(-10f, -95f);

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

        // 5. RIGHT COLUMN: Showcase Details Panel (Spacious ~67% Screen Width)
        GameObject rightCol = new GameObject("ShowcaseDetailsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightCol.transform.SetParent(dialogBox.transform, false);
        RectTransform rRt = rightCol.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.33f, 0f);
        rRt.anchorMax = new Vector2(1f, 1f);
        rRt.pivot = new Vector2(1f, 1f);
        rRt.offsetMin = new Vector2(10f, 20f);
        rRt.offsetMax = new Vector2(-20f, -95f);

        Image rcImg = rightCol.GetComponent<Image>();
        rcImg.color = new Color(0.06f, 0.08f, 0.13f, 0.98f);
        WhiteIconLoader.ApplyGothicFrame(rightCol, new Color(0.35f, 0.45f, 0.60f, 0.65f), 0.50f);

        // -------------------------------------------------------------
        // TOP HEADER ROW: 2 SUB-TABS (Left) + ACTION BUTTONS (Right)
        // -------------------------------------------------------------
        GameObject headerRow = new GameObject("HeaderTabsRow", typeof(RectTransform));
        headerRow.transform.SetParent(rightCol.transform, false);
        RectTransform hrRt = headerRow.GetComponent<RectTransform>();
        hrRt.anchorMin = new Vector2(0f, 1f);
        hrRt.anchorMax = new Vector2(1f, 1f);
        hrRt.pivot = new Vector2(0.5f, 1f);
        hrRt.anchoredPosition = new Vector2(0f, -8f);
        hrRt.sizeDelta = new Vector2(-24f, 40f);

        // A. Left Tabs Container
        GameObject tabsGroup = new GameObject("TabsGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabsGroup.transform.SetParent(headerRow.transform, false);
        RectTransform tgRt = tabsGroup.GetComponent<RectTransform>();
        tgRt.anchorMin = new Vector2(0f, 0.5f);
        tgRt.anchorMax = new Vector2(0f, 0.5f);
        tgRt.pivot = new Vector2(0f, 0.5f);
        tgRt.anchoredPosition = Vector2.zero;
        tgRt.sizeDelta = new Vector2(430f, 38f);

        var tgHlg = tabsGroup.GetComponent<HorizontalLayoutGroup>();
        tgHlg.childControlWidth = false;
        tgHlg.childControlHeight = true;
        tgHlg.childForceExpandWidth = false;
        tgHlg.childForceExpandHeight = true;
        tgHlg.spacing = 10f;

        // Tab 1: Kỹ Năng (Combat Skills)
        GameObject tabSkillsObj = new GameObject("TabBtn_Skills", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        tabSkillsObj.transform.SetParent(tabsGroup.transform, false);
        RectTransform tskRt = tabSkillsObj.GetComponent<RectTransform>();
        tskRt.sizeDelta = new Vector2(200f, 38f);
        imgRosterTabSkillsBg = tabSkillsObj.GetComponent<Image>();
        imgRosterTabSkillsBg.color = new Color(0.18f, 0.45f, 0.75f, 1f);
        var tskOutl = tabSkillsObj.AddComponent<Outline>();
        tskOutl.effectColor = new Color(1f, 0.85f, 0.35f, 1f);
        tskOutl.effectDistance = new Vector2(1.5f, -1.5f);
        btnRosterTabSkills = tabSkillsObj.GetComponent<Button>();
        btnRosterTabSkills.targetGraphic = imgRosterTabSkillsBg;
        btnRosterTabSkills.onClick.AddListener(() => SwitchRosterDetailsTab(RosterDetailsTab.Skills));

        GameObject tskTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        tskTxtObj.transform.SetParent(tabSkillsObj.transform, false);
        RectTransform tskTxtRt = tskTxtObj.GetComponent<RectTransform>();
        tskTxtRt.anchorMin = Vector2.zero; tskTxtRt.anchorMax = Vector2.one; tskTxtRt.sizeDelta = Vector2.zero;
        txtRosterTabSkills = tskTxtObj.AddComponent<TextMeshProUGUI>();
        txtRosterTabSkills.text = LocalizationManager.T("BỘ KỸ NĂNG", "COMBAT SKILLS");
        txtRosterTabSkills.fontSize = 15.5f;
        txtRosterTabSkills.fontStyle = FontStyles.Bold;
        txtRosterTabSkills.alignment = TextAlignmentOptions.Center;
        txtRosterTabSkills.color = Color.white;
        txtRosterTabSkills.raycastTarget = false;

        // Tab 2: Trang Bị & Ngọc (Gear & Sockets)
        GameObject tabGearObj = new GameObject("TabBtn_Gear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        tabGearObj.transform.SetParent(tabsGroup.transform, false);
        RectTransform tgBtnRt = tabGearObj.GetComponent<RectTransform>();
        tgBtnRt.sizeDelta = new Vector2(220f, 38f);
        imgRosterTabGearBg = tabGearObj.GetComponent<Image>();
        imgRosterTabGearBg.color = new Color(0.06f, 0.08f, 0.13f, 0.90f);
        var tgOutl = tabGearObj.AddComponent<Outline>();
        tgOutl.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);
        tgOutl.effectDistance = new Vector2(1.5f, -1.5f);
        btnRosterTabGear = tabGearObj.GetComponent<Button>();
        btnRosterTabGear.targetGraphic = imgRosterTabGearBg;
        btnRosterTabGear.onClick.AddListener(() => SwitchRosterDetailsTab(RosterDetailsTab.Gear));

        GameObject tgTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        tgTxtObj.transform.SetParent(tabGearObj.transform, false);
        RectTransform tgTxtRt = tgTxtObj.GetComponent<RectTransform>();
        tgTxtRt.anchorMin = Vector2.zero; tgTxtRt.anchorMax = Vector2.one; tgTxtRt.sizeDelta = Vector2.zero;
        txtRosterTabGear = tgTxtObj.AddComponent<TextMeshProUGUI>();
        txtRosterTabGear.text = LocalizationManager.T("TRANG BỊ & NGỌC", "GEAR & SOCKETS");
        txtRosterTabGear.fontSize = 15.5f;
        txtRosterTabGear.fontStyle = FontStyles.Bold;
        txtRosterTabGear.alignment = TextAlignmentOptions.Center;
        txtRosterTabGear.color = new Color(0.65f, 0.75f, 0.85f);
        txtRosterTabGear.raycastTarget = false;

        // B. Right Action Buttons Container
        GameObject actionsGroup = new GameObject("ActionsGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        actionsGroup.transform.SetParent(headerRow.transform, false);
        RectTransform agRt = actionsGroup.GetComponent<RectTransform>();
        agRt.anchorMin = new Vector2(1f, 0.5f);
        agRt.anchorMax = new Vector2(1f, 0.5f);
        agRt.pivot = new Vector2(1f, 0.5f);
        agRt.anchoredPosition = Vector2.zero;
        agRt.sizeDelta = new Vector2(340f, 38f);

        var agHlg = actionsGroup.GetComponent<HorizontalLayoutGroup>();
        agHlg.childControlWidth = false;
        agHlg.childControlHeight = true;
        agHlg.childForceExpandWidth = false;
        agHlg.childForceExpandHeight = true;
        agHlg.spacing = 10f;
        agHlg.childAlignment = TextAnchor.MiddleRight;

        // Select Battle Hero Action Button
        GameObject actBtnObj = new GameObject("Btn_RosterAction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actBtnObj.transform.SetParent(actionsGroup.transform, false);
        RectTransform actRt = actBtnObj.GetComponent<RectTransform>();
        actRt.sizeDelta = new Vector2(195f, 38f);
        Image actImg = actBtnObj.GetComponent<Image>();
        actImg.color = new Color(0.16f, 0.58f, 0.28f, 1f);
        var actOutl = actBtnObj.AddComponent<Outline>();
        actOutl.effectColor = new Color(0.95f, 0.82f, 0.35f, 0.90f);
        actOutl.effectDistance = new Vector2(1.5f, -1.5f);
        rosterActionBtn = actBtnObj.GetComponent<Button>();
        rosterActionBtn.targetGraphic = actImg;
        rosterActionBtn.onClick.RemoveAllListeners();
        rosterActionBtn.onClick.AddListener(OnRosterActionClicked);

        GameObject actTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        actTxtObj.transform.SetParent(actBtnObj.transform, false);
        RectTransform actTxtRt = actTxtObj.GetComponent<RectTransform>();
        actTxtRt.anchorMin = Vector2.zero; actTxtRt.anchorMax = Vector2.one; actTxtRt.sizeDelta = Vector2.zero;
        rosterActionBtnText = actTxtObj.AddComponent<TextMeshProUGUI>();
        rosterActionBtnText.fontSize = 15.5f;
        rosterActionBtnText.fontStyle = FontStyles.Bold;
        rosterActionBtnText.alignment = TextAlignmentOptions.Center;
        rosterActionBtnText.color = Color.white;
        rosterActionBtnText.raycastTarget = false;

        // =============================================================
        // TAB 1: BỘ KỸ NĂNG & THÔNG SỐ (SKILLS & STATS TAB)
        // =============================================================
        rosterSkillsTabContent = new GameObject("Tab_SkillsContent", typeof(RectTransform));
        rosterSkillsTabContent.transform.SetParent(rightCol.transform, false);
        RectTransform skTabRt = rosterSkillsTabContent.GetComponent<RectTransform>();
        skTabRt.anchorMin = Vector2.zero;
        skTabRt.anchorMax = Vector2.one;
        skTabRt.offsetMin = new Vector2(12f, 12f);
        skTabRt.offsetMax = new Vector2(-12f, -54f);

        // 1. Quick Stats Header Bar inside Skills Tab
        GameObject quickBarObj = new GameObject("HeroQuickStatsBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        quickBarObj.transform.SetParent(rosterSkillsTabContent.transform, false);
        RectTransform qbRt = quickBarObj.GetComponent<RectTransform>();
        qbRt.anchorMin = new Vector2(0f, 1f);
        qbRt.anchorMax = new Vector2(1f, 1f);
        qbRt.pivot = new Vector2(0.5f, 1f);
        qbRt.anchoredPosition = Vector2.zero;
        qbRt.sizeDelta = new Vector2(0f, 68f);

        Image qbImg = quickBarObj.GetComponent<Image>();
        qbImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        var qbOutl = quickBarObj.AddComponent<Outline>();
        qbOutl.effectColor = new Color(0.85f, 0.72f, 0.38f, 0.65f);
        qbOutl.effectDistance = new Vector2(1.5f, -1.5f);

        // Hero Name Text (Row 1 Left)
        GameObject nameObj = new GameObject("RosterHeroName", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.transform.SetParent(quickBarObj.transform, false);
        RectTransform nRt = nameObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(0.55f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(14f, -4f);
        nRt.sizeDelta = new Vector2(0f, 28f);
        rosterHeroNameText = nameObj.AddComponent<TextMeshProUGUI>();
        rosterHeroNameText.fontSize = 22f;
        rosterHeroNameText.fontStyle = FontStyles.Bold;
        rosterHeroNameText.color = new Color(1f, 0.88f, 0.45f);

        // Hero Role Text (Row 1 Right)
        GameObject roleObj = new GameObject("RosterHeroRole", typeof(RectTransform), typeof(CanvasRenderer));
        roleObj.transform.SetParent(quickBarObj.transform, false);
        RectTransform rlRt = roleObj.GetComponent<RectTransform>();
        rlRt.anchorMin = new Vector2(0.55f, 1f);
        rlRt.anchorMax = new Vector2(1f, 1f);
        rlRt.pivot = new Vector2(1f, 1f);
        rlRt.anchoredPosition = new Vector2(-14f, -6f);
        rlRt.sizeDelta = new Vector2(0f, 26f);
        rosterHeroRoleText = roleObj.AddComponent<TextMeshProUGUI>();
        rosterHeroRoleText.fontSize = 15f;
        rosterHeroRoleText.fontStyle = FontStyles.Bold;
        rosterHeroRoleText.alignment = TextAlignmentOptions.Right;
        rosterHeroRoleText.color = new Color(0.6f, 0.85f, 1f);

        // Quick Stats Line (Row 2 Horizontal Summary)
        GameObject qsObj = new GameObject("RosterHeroQuickStats", typeof(RectTransform), typeof(CanvasRenderer));
        qsObj.transform.SetParent(quickBarObj.transform, false);
        RectTransform qsRt = qsObj.GetComponent<RectTransform>();
        qsRt.anchorMin = new Vector2(0f, 0f);
        qsRt.anchorMax = new Vector2(1f, 0f);
        qsRt.pivot = new Vector2(0.5f, 0f);
        qsRt.anchoredPosition = new Vector2(0f, 6f);
        qsRt.sizeDelta = new Vector2(-28f, 24f);
        rosterHeroQuickStatsText = qsObj.AddComponent<TextMeshProUGUI>();
        rosterHeroQuickStatsText.fontSize = 14.5f;
        rosterHeroQuickStatsText.fontStyle = FontStyles.Bold;
        rosterHeroQuickStatsText.color = new Color(0.92f, 0.95f, 0.98f);
        rosterHeroQuickStatsText.enableWordWrapping = false;
        rosterHeroQuickStatsText.overflowMode = TextOverflowModes.Ellipsis;

        // 2. Large Spacious SkillBox Container
        GameObject skillBox = new GameObject("SkillBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        skillBox.transform.SetParent(rosterSkillsTabContent.transform, false);
        RectTransform skRt = skillBox.GetComponent<RectTransform>();
        skRt.anchorMin = Vector2.zero;
        skRt.anchorMax = Vector2.one;
        skRt.pivot = new Vector2(0.5f, 1f);
        skRt.offsetMin = Vector2.zero;
        skRt.offsetMax = new Vector2(0f, -74f);

        Image skImg = skillBox.GetComponent<Image>();
        skImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
        var skOutl = skillBox.AddComponent<Outline>();
        skOutl.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.60f);
        skOutl.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject skTitleObj = new GameObject("SkillTitle", typeof(RectTransform), typeof(CanvasRenderer));
        skTitleObj.transform.SetParent(skillBox.transform, false);
        RectTransform sktRt = skTitleObj.GetComponent<RectTransform>();
        sktRt.anchorMin = new Vector2(0f, 1f);
        sktRt.anchorMax = new Vector2(1f, 1f);
        sktRt.pivot = new Vector2(0.5f, 1f);
        sktRt.anchoredPosition = new Vector2(14f, -6f);
        sktRt.sizeDelta = new Vector2(-28f, 22f);
        rosterHeroSkillTitleText = skTitleObj.AddComponent<TextMeshProUGUI>();
        rosterHeroSkillTitleText.fontSize = 16f;
        rosterHeroSkillTitleText.fontStyle = FontStyles.Bold;
        rosterHeroSkillTitleText.color = new Color(1f, 0.85f, 0.35f);

        // 4 Spacious Skill Cards: 0 (Nội Tại), 1 (Chiêu 1), 2 (Lướt), 3 (Tuyệt Kỹ)
        CreateSkillRowCard(skillBox.transform, 0, -32f, 98f,
            out rosterSkill0IconImg, out rosterSkill0TagText, out rosterSkill0CdText, out rosterSkill0TitleText, out rosterSkill0DescText);
        CreateSkillRowCard(skillBox.transform, 1, -138f, 98f,
            out rosterSkill1IconImg, out rosterSkill1TagText, out rosterSkill1CdText, out rosterSkill1TitleText, out rosterSkill1DescText);
        CreateSkillRowCard(skillBox.transform, 2, -244f, 98f,
            out rosterSkill2IconImg, out rosterSkill2TagText, out rosterSkill2CdText, out rosterSkill2TitleText, out rosterSkill2DescText);
        CreateSkillRowCard(skillBox.transform, 3, -350f, 98f,
            out rosterSkill3IconImg, out rosterSkill3TagText, out rosterSkill3CdText, out rosterSkill3TitleText, out rosterSkill3DescText);
        rosterHeroSkillDescText = rosterSkill0DescText;

        // =============================================================
        // TAB 2: TRANG BỊ & NGỌC KHẢM (GEAR & SOCKETS TAB)
        // =============================================================
        rosterGearTabContent = new GameObject("Tab_GearContent", typeof(RectTransform));
        rosterGearTabContent.transform.SetParent(rightCol.transform, false);
        RectTransform gearTabRt = rosterGearTabContent.GetComponent<RectTransform>();
        gearTabRt.anchorMin = Vector2.zero;
        gearTabRt.anchorMax = Vector2.one;
        gearTabRt.offsetMin = new Vector2(12f, 12f);
        gearTabRt.offsetMax = new Vector2(-12f, -54f);

        // =============================================================
        // TAB 2: TRANG BỊ & NGỌC KHẢM (GEAR & SOCKETS TAB)
        // Clean 2-Column AAA RPG Layout:
        // Left Column (50%): Hero Stage (Top 430px) + Warrior Attributes (Bottom)
        // Right Column (50%): Full-Height Item Bag (Grid 5 cols x 7 rows, Auto-Merge button)
        // =============================================================
        // -------------------------------------------------------------
        // LEFT COLUMN: HERO STAGE & WARRIOR ATTRIBUTES
        // -------------------------------------------------------------
        GameObject gearLeftCol = new GameObject("GearLeftCol", typeof(RectTransform));
        gearLeftCol.transform.SetParent(rosterGearTabContent.transform, false);
        RectTransform glcRt = gearLeftCol.GetComponent<RectTransform>();
        glcRt.anchorMin = new Vector2(0f, 0f);
        glcRt.anchorMax = new Vector2(0.50f, 1f);
        glcRt.offsetMin = Vector2.zero;
        glcRt.offsetMax = new Vector2(-6f, 0f);

        // A. Hero Center Stage (Avatar + 6 Sockets)
        GameObject stageBox = new GameObject("HeroStageBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        stageBox.transform.SetParent(gearLeftCol.transform, false);
        RectTransform sbRt = stageBox.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 1f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot = new Vector2(0.5f, 1f);
        sbRt.anchoredPosition = Vector2.zero;
        sbRt.sizeDelta = new Vector2(0f, 430f);

        Image sbImg = stageBox.GetComponent<Image>();
        sbImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        var sbOutl = stageBox.AddComponent<Outline>();
        sbOutl.effectColor = new Color(0.85f, 0.72f, 0.38f, 0.70f);
        sbOutl.effectDistance = new Vector2(1.5f, -1.5f);

        // 6 Socket Slot Positions in an arc around the center avatar
        Vector2[] socketPositions = new Vector2[]
        {
            new Vector2(-150f, 110f),  // Ô 1 (Top-Left)
            new Vector2(-175f, 0f),    // Ô 2 (Mid-Left)
            new Vector2(-150f, -110f), // Ô 3 (Bot-Left)
            new Vector2(150f, 110f),   // Ô 4 (Top-Right)
            new Vector2(175f, 0f),     // Ô 5 (Mid-Right)
            new Vector2(150f, -110f)   // Ô 6 (Bot-Right)
        };

        // 6 Glowing Conduit Lines connecting 6 sockets to center Avatar
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
            lineRt.sizeDelta = new Vector2(dist, 3f);
            lineRt.localRotation = Quaternion.Euler(0f, 0f, angle);

            Image lineImg = lineObj.GetComponent<Image>();
            lineImg.color = new Color(0.85f, 0.72f, 0.35f, 0.30f);
            lineImg.raycastTarget = false;
            rosterSocketConduitLines.Add(lineImg);
        }

        // Center Avatar Box
        GameObject animBox = new GameObject("ShowcaseAnimBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
        animBox.transform.SetParent(stageBox.transform, false);
        RectTransform aRt = animBox.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.5f, 0.5f);
        aRt.anchorMax = new Vector2(0.5f, 0.5f);
        aRt.pivot = new Vector2(0.5f, 0.5f);
        aRt.anchoredPosition = Vector2.zero;
        aRt.sizeDelta = new Vector2(165f, 260f);

        GameObject animImgObj = new GameObject("HeroLiveSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        animImgObj.transform.SetParent(animBox.transform, false);
        RectTransform aiRt = animImgObj.GetComponent<RectTransform>();
        aiRt.anchorMin = Vector2.zero;
        aiRt.anchorMax = Vector2.one;
        aiRt.sizeDelta = Vector2.zero;
        rosterHeroPreviewImage = animImgObj.GetComponent<Image>();
        rosterHeroPreviewImage.preserveAspect = true;
        rosterHeroPreviewImage.transform.localScale = GetHeroVisualScale("vael");
        rosterHeroPreviewImage.rectTransform.anchoredPosition = GetHeroVisualOffset("vael");

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
            sRt.sizeDelta = new Vector2(68f, 68f);

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

        // B. Warrior Stats Box below StageBox
        GameObject statsBox = new GameObject("WarriorStatsBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statsBox.transform.SetParent(gearLeftCol.transform, false);
        RectTransform stBoxRt = statsBox.GetComponent<RectTransform>();
        stBoxRt.anchorMin = Vector2.zero;
        stBoxRt.anchorMax = Vector2.one;
        stBoxRt.pivot = new Vector2(0.5f, 0f);
        stBoxRt.offsetMin = Vector2.zero;
        stBoxRt.offsetMax = new Vector2(0f, -438f);

        Image stImg = statsBox.GetComponent<Image>();
        stImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        var stOutl = statsBox.AddComponent<Outline>();
        stOutl.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.60f);
        stOutl.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject igHeaderObj = new GameObject("StatsHeader", typeof(RectTransform), typeof(CanvasRenderer));
        igHeaderObj.transform.SetParent(statsBox.transform, false);
        RectTransform ighRt = igHeaderObj.GetComponent<RectTransform>();
        ighRt.anchorMin = new Vector2(0f, 1f);
        ighRt.anchorMax = new Vector2(1f, 1f);
        ighRt.pivot = new Vector2(0f, 1f);
        ighRt.anchoredPosition = new Vector2(14f, -8f);
        ighRt.sizeDelta = new Vector2(-28f, 26f);
        var ighTmp = igHeaderObj.AddComponent<TextMeshProUGUI>();
        ighTmp.text = LocalizationManager.T("THÔNG SỐ CHIẾN BINH", "WARRIOR ATTRIBUTES");
        ighTmp.fontSize = 15.5f;
        ighTmp.fontStyle = FontStyles.Bold;
        ighTmp.color = new Color(1f, 0.85f, 0.35f);

        GameObject statsObj = new GameObject("RosterHeroStats", typeof(RectTransform), typeof(CanvasRenderer));
        statsObj.transform.SetParent(statsBox.transform, false);
        RectTransform stRt = statsObj.GetComponent<RectTransform>();
        stRt.anchorMin = Vector2.zero;
        stRt.anchorMax = Vector2.one;
        stRt.offsetMin = new Vector2(14f, 8f);
        stRt.offsetMax = new Vector2(-14f, -34f);
        rosterHeroStatsText = statsObj.AddComponent<TextMeshProUGUI>();
        rosterHeroStatsText.fontSize = 13.5f;
        rosterHeroStatsText.lineSpacing = 1.15f;
        rosterHeroStatsText.fontStyle = FontStyles.Bold;
        rosterHeroStatsText.color = new Color(0.95f, 0.95f, 0.95f);

        rosterGemEffectsText = null;

        // -------------------------------------------------------------
        // RIGHT COLUMN: ITEM BAG (REPLACES REDUNDANT GEM LIST & TAKES FULL HEIGHT)
        // -------------------------------------------------------------
        GameObject bagBox = new GameObject("RosterBagBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bagBox.transform.SetParent(rosterGearTabContent.transform, false);
        RectTransform bbRt = bagBox.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0.50f, 0f);
        bbRt.anchorMax = new Vector2(1f, 1f);
        bbRt.pivot = new Vector2(1f, 0.5f);
        bbRt.offsetMin = new Vector2(6f, 0f);
        bbRt.offsetMax = Vector2.zero;

        Image bbImg = bagBox.GetComponent<Image>();
        bbImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(bagBox, new Color(0.35f, 0.45f, 0.60f, 0.65f), 0.50f);

        // Bag Header Strip (Dedicated bar, perfectly centered, no overlapping)
        GameObject bhBarObj = new GameObject("BagHeaderBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bhBarObj.transform.SetParent(bagBox.transform, false);
        RectTransform bhbRt = bhBarObj.GetComponent<RectTransform>();
        bhbRt.anchorMin = new Vector2(0f, 1f);
        bhbRt.anchorMax = new Vector2(1f, 1f);
        bhbRt.pivot = new Vector2(0.5f, 1f);
        bhbRt.anchoredPosition = Vector2.zero;
        bhbRt.sizeDelta = new Vector2(0f, 40f);
        Image bhbImg = bhBarObj.GetComponent<Image>();
        bhbImg.color = new Color(0.06f, 0.08f, 0.14f, 0.90f);

        // Header Title Text
        GameObject bhObj = new GameObject("BagHeaderTitle", typeof(RectTransform), typeof(CanvasRenderer));
        bhObj.transform.SetParent(bhBarObj.transform, false);
        RectTransform bhRt = bhObj.GetComponent<RectTransform>();
        bhRt.anchorMin = new Vector2(0f, 0f);
        bhRt.anchorMax = new Vector2(1f, 1f);
        bhRt.pivot = new Vector2(0f, 0.5f);
        bhRt.anchoredPosition = Vector2.zero;
        bhRt.offsetMin = new Vector2(14f, 0f);
        bhRt.offsetMax = new Vector2(-185f, 0f);

        rosterBagSectionHeaderText = bhObj.AddComponent<TextMeshProUGUI>();
        rosterBagSectionHeaderText.fontSize = 14f;
        rosterBagSectionHeaderText.fontStyle = FontStyles.Bold;
        rosterBagSectionHeaderText.color = new Color(0.85f, 0.90f, 1f);
        rosterBagSectionHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
        rosterBagSectionHeaderText.enableWordWrapping = false;
        rosterBagSectionHeaderText.overflowMode = TextOverflowModes.Ellipsis;
        rosterBagSectionHeaderText.text = LocalizationManager.T("TÚI VẬT PHẨM (KÉO SANG 6 Ô)", "ITEM BAG (DRAG TO 6 SLOTS)");

        // Auto Merge Button (Positioned neatly on top-right of Bag Header Bar)
        GameObject ambObj = new GameObject("Btn_AutoMerge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        ambObj.transform.SetParent(bhBarObj.transform, false);
        RectTransform ambRt = ambObj.GetComponent<RectTransform>();
        ambRt.anchorMin = new Vector2(1f, 0.5f);
        ambRt.anchorMax = new Vector2(1f, 0.5f);
        ambRt.pivot = new Vector2(1f, 0.5f);
        ambRt.anchoredPosition = new Vector2(-10f, 0f);
        ambRt.sizeDelta = new Vector2(165f, 28f);

        Image ambImg = ambObj.GetComponent<Image>();
        ambImg.color = new Color(0.72f, 0.45f, 0.12f, 1f);
        var ambOutl = ambObj.AddComponent<Outline>();
        ambOutl.effectColor = new Color(1f, 0.85f, 0.40f, 0.85f);
        ambOutl.effectDistance = new Vector2(1.5f, -1.5f);

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
        amtRt.anchorMin = Vector2.zero; amtRt.anchorMax = Vector2.one; amtRt.sizeDelta = Vector2.zero;
        rosterAutoMergeBtnText = amTxtObj.AddComponent<TextMeshProUGUI>();
        rosterAutoMergeBtnText.fontSize = 13.5f;
        rosterAutoMergeBtnText.fontStyle = FontStyles.Bold;
        rosterAutoMergeBtnText.alignment = TextAlignmentOptions.Center;
        rosterAutoMergeBtnText.color = Color.white;
        rosterAutoMergeBtnText.text = LocalizationManager.T("HẤP THU TOÀN BỘ", "AUTO MERGE ALL");

        // Subtle Header Divider Line directly under HeaderBar
        GameObject bagDivObj = new GameObject("BagHeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bagDivObj.transform.SetParent(bagBox.transform, false);
        RectTransform bdRt = bagDivObj.GetComponent<RectTransform>();
        bdRt.anchorMin = new Vector2(0f, 1f);
        bdRt.anchorMax = new Vector2(1f, 1f);
        bdRt.pivot = new Vector2(0.5f, 1f);
        bdRt.anchoredPosition = new Vector2(0f, -40f);
        bdRt.sizeDelta = new Vector2(-16f, 1.5f);
        Image bdImg = bagDivObj.GetComponent<Image>();
        bdImg.sprite = WhiteIconLoader.GetDividerFadeSprite();
        bdImg.color = new Color(0.85f, 0.72f, 0.38f, 0.40f);
        bdImg.raycastTarget = false;

        // Bag Scroll Container (Full Height Vertical Scroll)
        GameObject scrollObj = new GameObject("BagScrollRect", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(bagBox.transform, false);
        RectTransform scrRt = scrollObj.GetComponent<RectTransform>();
        scrRt.anchorMin = Vector2.zero;
        scrRt.anchorMax = Vector2.one;
        scrRt.offsetMin = new Vector2(10f, 10f);
        scrRt.offsetMax = new Vector2(-10f, -44f);

        ScrollRect sRect = scrollObj.GetComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.movementType = ScrollRect.MovementType.Elastic;
        sRect.elasticity = 0.1f;
        sRect.inertia = true;
        sRect.decelerationRate = 0.135f;
        sRect.scrollSensitivity = 35f;

        // Bag Content using 5-Column GridLayoutGroup
        GameObject contentObj = new GameObject("BagContent", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contRt = contentObj.GetComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0f, 1f);
        contRt.anchorMax = new Vector2(1f, 1f);
        contRt.pivot = new Vector2(0.5f, 1f);
        contRt.anchoredPosition = Vector2.zero;
        contRt.sizeDelta = Vector2.zero;

        var glg = contentObj.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(104f, 104f);
        glg.spacing = new Vector2(12f, 12f);
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;
        glg.padding = new RectOffset(12, 12, 10, 10);

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sRect.content = contRt;
        rosterBagContainer = contRt;

        // Initialize to Skills Tab by default
        SwitchRosterDetailsTab(RosterDetailsTab.Skills);

        // CRITICAL: Ensure Close Button [X] is ALWAYS the top-most sibling in RosterBox so clicks are never intercepted
        if (closeBtnObj != null)
        {
            closeBtnObj.transform.SetAsLastSibling();
        }

        rosterPanel.SetActive(false);
    }

    public void CloseRosterPanel()
    {
        Debug.Log("<color=green>[MainMenuManager]</color> CloseRosterPanel executed successfully!");
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        EquipmentItemDetailPopupUI.Hide();
        if (rosterPanel != null) rosterPanel.SetActive(false);

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var c = canvas.transform.GetChild(i);
                if (c.name.Contains("RosterModalPanel") || c.name.Contains("HeroRosterModal") || c.name.Contains("OldRosterModal"))
                {
                    c.gameObject.SetActive(false);
                }
            }
        }

        if (mainPanel != null) mainPanel.SetActive(true);
        EnsureBottomNavigationBar();
        UpdateCurrencyDisplay();
        UpdateAllTexts();
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
        else if (id == "dune" || id == "duneassassin" || id == "player5" || id.Contains("dune"))
        {
            return new Vector3(0.85f, 0.85f, 1f);
        }
        else if (id == "mage" || id == "celestialmage" || id == "player6" || id.Contains("mage"))
        {
            return new Vector3(1.05f, 1.05f, 1f);
        }
        return new Vector3(2.0f, 2.0f, 1f);
    }

    public static Vector2 GetHeroVisualOffset(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return new Vector2(0f, 14f);
        string id = heroId.ToLower();
        if (id == "titan" || id == "titanberserker" || id == "player4" || id.Contains("titan"))
        {
            // Titan 128x128 sprite has empty air in top half; +58f aligns Titan's core/chest exactly to (0,0)
            return new Vector2(0f, 58f);
        }
        else if (id == "ronin" || id == "galeronin" || id == "player2" || id == "maid" || id == "elysia" || id.Contains("ronin"))
        {
            return new Vector2(0f, 8f);
        }
        else if (id == "vael" || id == "vaelstrider" || id.Contains("vael"))
        {
            return new Vector2(0f, 14f);
        }
        else if (id == "azure" || id == "azureblade" || id == "player3" || id.Contains("azure"))
        {
            return new Vector2(0f, 10f);
        }
        else if (id == "dune" || id == "duneassassin" || id == "player5" || id.Contains("dune"))
        {
            return new Vector2(0f, 0f);
        }
        else if (id == "mage" || id == "celestialmage" || id == "player6" || id.Contains("mage"))
        {
            return new Vector2(0f, 6f);
        }
        return Vector2.zero;
    }

    /// <summary>
    /// Exact visual offset for hero showcase stage in HeroSelectCarouselModal so that all heroes
    /// stand directly in the center of the golden circular runic altar (AltarBase) and floor shadow.
    /// </summary>
    public static Vector2 GetHeroCarouselVisualOffset(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return new Vector2(12f, -26f);
        string id = heroId.ToLower();
        if (id == "vael" || id == "vaelstrider" || id.Contains("vael"))
        {
            return new Vector2(12f, -26f);
        }
        else if (id == "dune" || id == "duneassassin" || id == "player5" || id.Contains("dune"))
        {
            return new Vector2(0f, -37.5f);
        }
        else if (id == "titan" || id == "titanberserker" || id == "player4" || id.Contains("titan"))
        {
            return new Vector2(0f, 69f);
        }
        else if (id == "ronin" || id == "galeronin" || id == "player2" || id == "maid" || id == "elysia" || id.Contains("ronin"))
        {
            return new Vector2(0f, 51f);
        }
        else if (id == "azure" || id == "azureblade" || id == "player3" || id.Contains("azure"))
        {
            return new Vector2(48f, 23f);
        }
        else if (id == "mage" || id == "celestialmage" || id == "player6" || id.Contains("mage"))
        {
            return new Vector2(0f, 2f);
        }
        return new Vector2(0f, -26f);
    }

    public static Vector2 GetAvatarCardVisualOffset(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return new Vector2(0f, 8f);
        string id = heroId.ToLower();
        if (id == "titan" || id == "titanberserker" || id == "player4" || id.Contains("titan"))
        {
            // Centers Titan vertically inside the 90x90 card avatar box
            return new Vector2(0f, 32f);
        }
        else if (id == "ronin" || id == "galeronin" || id == "player2" || id == "maid" || id == "elysia" || id.Contains("ronin"))
        {
            return new Vector2(0f, 6f);
        }
        else if (id == "vael" || id == "vaelstrider" || id.Contains("vael"))
        {
            return new Vector2(0f, 8f);
        }
        else if (id == "azure" || id == "azureblade" || id == "player3" || id.Contains("azure"))
        {
            return new Vector2(0f, 6f);
        }
        else if (id == "dune" || id == "duneassassin" || id == "player5" || id.Contains("dune"))
        {
            // Dune is tightly sliced (36x39); -3f gives clean top margin so head is never cut off
            return new Vector2(0f, -3f);
        }
        else if (id == "mage" || id == "celestialmage" || id == "player6" || id.Contains("mage"))
        {
            return new Vector2(0f, 4f);
        }
        return Vector2.zero;
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
        GameObject card = new GameObject($"SkillCard_{skillIndex}_v10_PassiveAndSkills", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform cRt = card.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = new Vector2(0f, yOffset);
        cRt.sizeDelta = new Vector2(-24f, height);

        Image cImg = card.GetComponent<Image>();
        cImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
        
        // Màu viền thẻ:
        // skillIndex == 0: NỘI TẠI (Tím Lam Hoàng Kim / Arcane Gold)
        // skillIndex == 1: CHIÊU 1 (Vàng Cam)
        // skillIndex == 2: CHIÊU 2 / LƯỚT (Xanh Ngọc)
        // skillIndex == 3: CHIÊU 3 / ULTI (Đỏ Rực)
        Color outlineCol = (skillIndex == 0) ? new Color(0.85f, 0.65f, 1f, 0.90f)
                         : (skillIndex == 1 ? new Color(1f, 0.85f, 0.35f, 0.85f)
                         : (skillIndex == 2 ? new Color(0.35f, 0.90f, 0.65f, 0.85f) : new Color(0.95f, 0.40f, 0.40f, 0.90f)));
        var cOutl = card.AddComponent<Outline>();
        cOutl.effectColor = outlineCol;
        cOutl.effectDistance = new Vector2(1.5f, -1.5f);

        // Icon Badge Container (52x52) with clean Outline
        GameObject iconBadge = new GameObject("IconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconBadge.transform.SetParent(card.transform, false);
        RectTransform ibRt = iconBadge.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(12f, 0f);
        ibRt.sizeDelta = new Vector2(52f, 52f);

        Image ibImg = iconBadge.GetComponent<Image>();
        ibImg.color = new Color(0.07f, 0.10f, 0.16f, 1f);
        var ibOutl = iconBadge.AddComponent<Outline>();
        ibOutl.effectColor = outlineCol;
        ibOutl.effectDistance = new Vector2(1.5f, -1.5f);

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

        // TOP-RIGHT GROUP: Tag Badge + Cooldown Badge (HorizontalLayoutGroup, Right-Aligned)
        GameObject topGroup = new GameObject("TopRightGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        topGroup.transform.SetParent(card.transform, false);
        RectTransform trRt = topGroup.GetComponent<RectTransform>();
        trRt.anchorMin = new Vector2(1f, 1f);
        trRt.anchorMax = new Vector2(1f, 1f);
        trRt.pivot = new Vector2(1f, 1f);
        trRt.anchoredPosition = new Vector2(-12f, -6f);

        var csf = topGroup.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var hlg = topGroup.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleRight;

        // 1. Tag badge
        GameObject tagBadge = new GameObject("TagBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tagBadge.transform.SetParent(topGroup.transform, false);
        RectTransform tRt = tagBadge.GetComponent<RectTransform>();
        tRt.sizeDelta = new Vector2(110f, 22f);

        Image tagBgImg = tagBadge.GetComponent<Image>();
        tagBgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);
        var tagOutl = tagBadge.AddComponent<Outline>();
        tagOutl.effectColor = outlineCol;
        tagOutl.effectDistance = new Vector2(1f, -1f);

        GameObject tagObj = new GameObject("TagText", typeof(RectTransform), typeof(CanvasRenderer));
        tagObj.transform.SetParent(tagBadge.transform, false);
        RectTransform ttRt2 = tagObj.GetComponent<RectTransform>();
        ttRt2.anchorMin = Vector2.zero;
        ttRt2.anchorMax = Vector2.one;
        ttRt2.offsetMin = new Vector2(6f, 0f);
        ttRt2.offsetMax = new Vector2(-6f, 0f);
        tagTxt = tagObj.AddComponent<TextMeshProUGUI>();
        tagTxt.fontSize = 11.5f;
        tagTxt.fontStyle = FontStyles.Bold;
        tagTxt.alignment = TextAlignmentOptions.Center;
        tagTxt.color = outlineCol;
        tagTxt.enableWordWrapping = false;
        tagTxt.raycastTarget = false;

        // 2. Cooldown Badge
        GameObject cdBadge = new GameObject("CooldownBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cdBadge.transform.SetParent(topGroup.transform, false);
        RectTransform cdRt = cdBadge.GetComponent<RectTransform>();
        cdRt.sizeDelta = new Vector2(76f, 22f);

        Image cdBgImg = cdBadge.GetComponent<Image>();
        cdBgImg.color = new Color(0.05f, 0.10f, 0.18f, 0.90f);
        var cdOutl = cdBadge.AddComponent<Outline>();
        cdOutl.effectColor = new Color(0.22f, 0.74f, 0.97f, 0.85f);
        cdOutl.effectDistance = new Vector2(1f, -1f);

        GameObject cdObj = new GameObject("CDText", typeof(RectTransform), typeof(CanvasRenderer));
        cdObj.transform.SetParent(cdBadge.transform, false);
        RectTransform cdtRt = cdObj.GetComponent<RectTransform>();
        cdtRt.anchorMin = Vector2.zero;
        cdtRt.anchorMax = Vector2.one;
        cdtRt.sizeDelta = Vector2.zero;
        cdTxt = cdObj.AddComponent<TextMeshProUGUI>();
        cdTxt.fontSize = 12f;
        cdTxt.fontStyle = FontStyles.Bold;
        cdTxt.alignment = TextAlignmentOptions.Center;
        cdTxt.enableWordWrapping = false;
        cdTxt.raycastTarget = false;
        cdTxt.text = "<color=#38BDF8><b>CD: 0s</b></color>";

        // TITLE: Neo từ sau Icon (x=76f) đến trước TopRightGroup
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform ttRt = titleObj.GetComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0f, 1f);
        ttRt.anchorMax = new Vector2(1f, 1f);
        ttRt.pivot = new Vector2(0f, 1f);
        ttRt.anchoredPosition = new Vector2(76f, -6f);
        ttRt.sizeDelta = new Vector2(-(76f + 210f), 22f);

        titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 15f;
        titleTxt.enableAutoSizing = true;
        titleTxt.fontSizeMin = 11.5f;
        titleTxt.fontSizeMax = 15.5f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Left;
        titleTxt.enableWordWrapping = false;
        titleTxt.overflowMode = TextOverflowModes.Ellipsis;
        titleTxt.raycastTarget = false;

        // DESC: Chiều cao vừa khít 58px chứa thoải mái 2-3 dòng chữ
        GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer));
        descObj.transform.SetParent(card.transform, false);
        RectTransform dtRt = descObj.GetComponent<RectTransform>();
        dtRt.anchorMin = Vector2.zero;
        dtRt.anchorMax = Vector2.one;
        dtRt.offsetMin = new Vector2(76f, 6f);
        dtRt.offsetMax = new Vector2(-12f, -30f);

        descTxt = descObj.AddComponent<TextMeshProUGUI>();
        descTxt.fontSize = 12.5f;
        descTxt.enableAutoSizing = true;
        descTxt.fontSizeMin = 10.5f;
        descTxt.fontSizeMax = 13f;
        descTxt.lineSpacing = 1.05f;
        descTxt.enableWordWrapping = true;
        descTxt.overflowMode = TextOverflowModes.Ellipsis;
        descTxt.color = new Color(0.88f, 0.91f, 0.96f);
        descTxt.raycastTarget = false;

        return card;
    }

    public void SwitchRosterDetailsTab(RosterDetailsTab tab)
    {
        currentRosterDetailsTab = tab;
        bool isSkills = (tab == RosterDetailsTab.Skills);
        if (rosterSkillsTabContent != null) rosterSkillsTabContent.SetActive(isSkills);
        if (rosterGearTabContent != null) rosterGearTabContent.SetActive(!isSkills);

        if (imgRosterTabSkillsBg != null)
        {
            imgRosterTabSkillsBg.color = isSkills ? new Color(0.18f, 0.45f, 0.75f, 1f) : new Color(0.06f, 0.08f, 0.13f, 0.90f);
            var outl = imgRosterTabSkillsBg.GetComponent<Outline>();
            if (outl != null) outl.effectColor = isSkills ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.3f, 0.35f, 0.45f, 0.5f);
        }
        if (txtRosterTabSkills != null) txtRosterTabSkills.color = isSkills ? Color.white : new Color(0.65f, 0.75f, 0.85f);

        if (imgRosterTabGearBg != null)
        {
            imgRosterTabGearBg.color = !isSkills ? new Color(0.18f, 0.45f, 0.75f, 1f) : new Color(0.06f, 0.08f, 0.13f, 0.90f);
            var outl = imgRosterTabGearBg.GetComponent<Outline>();
            if (outl != null) outl.effectColor = !isSkills ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.3f, 0.35f, 0.45f, 0.5f);
        }
        if (txtRosterTabGear != null) txtRosterTabGear.color = !isSkills ? Color.white : new Color(0.65f, 0.75f, 0.85f);

        if (!isSkills)
        {
            RefreshRosterSocketsAndBag();
        }
    }

    private void SetHeroSkillCard(int cardIdx, Sprite icon, string tag, string title, string desc, float baseCd = 0f, float finalCd = 0f, float upDashCd = 0f, float upUltiCd = 0f, string costTag = "")
    {
        Color tagCol = Color.white;
        string upperTag = string.IsNullOrEmpty(tag) ? "" : tag.ToUpper();
        if (upperTag.Contains("NỘI TẠI") || upperTag.Contains("PASSIVE")) tagCol = new Color(0.85f, 0.65f, 1f); // Tím Hoàng Kim Nội Tại
        else if (upperTag.Contains("AP") || upperTag.Contains("PHÉP") || upperTag.Contains("MAGIC")) tagCol = new Color(0.72f, 0.35f, 1f); // Tím AP
        else if (upperTag.Contains("TD") || upperTag.Contains("CHUẨN") || upperTag.Contains("TRUE")) tagCol = new Color(0.92f, 0.95f, 1f); // Trắng Bạc TD
        else if (upperTag.Contains("ATK") || upperTag.Contains("VẬT LÝ") || upperTag.Contains("PHYS") || upperTag.Contains("V.LÝ") || upperTag.Contains("ULTI") || upperTag.Contains("TUYỆT KỸ")) tagCol = new Color(1f, 0.35f, 0.35f); // Đỏ ATK / Ulti
        else if (upperTag.Contains("LƯỚT") || upperTag.Contains("DASH") || upperTag.Contains("TỐC BIẾN") || upperTag.Contains("BLINK") || upperTag.Contains("WARP") || upperTag.Contains("ẢO ẢNH")) tagCol = new Color(0.35f, 0.90f, 0.65f); // Xanh Ngọc Cơ Động
        else tagCol = new Color(1f, 0.85f, 0.35f);

        string cleanTag = string.IsNullOrEmpty(tag) ? "" : tag.Trim('[', ']', ' ');

        // Tính toán Cooldown động cập nhật theo ngọc khảm và nâng cấp
        float effCd = 0f;
        if (baseCd > 0.05f)
        {
            float clampedCdr = Mathf.Clamp(finalCd, 0f, 0.60f);
            if (cardIdx == 0) effCd = 0f; // Nội tại
            else if (cardIdx == 1) effCd = Mathf.Max(0.5f, baseCd * (1f - clampedCdr));
            else if (cardIdx == 2) effCd = Mathf.Max(1.0f, (baseCd * (1f - clampedCdr)) - upDashCd);
            else effCd = Mathf.Max(3.0f, (baseCd * (1f - clampedCdr)) - upUltiCd);
        }

        string cdStr;
        if (cardIdx == 0)
        {
            cdStr = "<color=#E2E8F0><b>VÔ HẠN</b></color>";
        }
        else
        {
            cdStr = (effCd > 0f) ? $"<color=#38BDF8><b>CD: {effCd:0.#}s</b></color>" : "<color=#94A3B8><b>CD: 0s</b></color>";
        }

        string displayTitle = string.IsNullOrEmpty(costTag) ? title : $"{title}  <size=85%>{costTag}</size>";

        if (cardIdx == 0)
        {
            if (rosterSkill0IconImg != null)
            {
                rosterSkill0IconImg.sprite = icon;
                rosterSkill0IconImg.color = (icon != null) ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
            if (rosterSkill0TagText != null) 
            { 
                rosterSkill0TagText.text = cleanTag; 
                rosterSkill0TagText.color = tagCol;
                var outl = rosterSkill0TagText.transform.parent.GetComponent<Outline>();
                if (outl != null) outl.effectColor = tagCol;
            }
            if (rosterSkill0CdText != null) rosterSkill0CdText.text = cdStr;
            if (rosterSkill0TitleText != null)
            {
                rosterSkill0TitleText.text = displayTitle;
                rosterSkill0TitleText.enableAutoSizing = true;
                rosterSkill0TitleText.fontSizeMin = 11.5f;
                rosterSkill0TitleText.fontSizeMax = 15f;
            }
            if (rosterSkill0DescText != null) rosterSkill0DescText.text = desc;
        }
        else if (cardIdx == 1)
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
                var outl = rosterSkill1TagText.transform.parent.GetComponent<Outline>();
                if (outl != null) outl.effectColor = tagCol;
            }
            if (rosterSkill1CdText != null) rosterSkill1CdText.text = cdStr;
            if (rosterSkill1TitleText != null)
            {
                rosterSkill1TitleText.text = displayTitle;
                rosterSkill1TitleText.enableAutoSizing = true;
                rosterSkill1TitleText.fontSizeMin = 11.5f;
                rosterSkill1TitleText.fontSizeMax = 15f;
            }
            if (rosterSkill1DescText != null) rosterSkill1DescText.text = desc;
        }
        else if (cardIdx == 2)
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
                var outl = rosterSkill2TagText.transform.parent.GetComponent<Outline>();
                if (outl != null) outl.effectColor = tagCol;
            }
            if (rosterSkill2CdText != null) rosterSkill2CdText.text = cdStr;
            if (rosterSkill2TitleText != null)
            {
                rosterSkill2TitleText.text = displayTitle;
                rosterSkill2TitleText.enableAutoSizing = true;
                rosterSkill2TitleText.fontSizeMin = 11.5f;
                rosterSkill2TitleText.fontSizeMax = 15f;
            }
            if (rosterSkill2DescText != null) rosterSkill2DescText.text = desc;
        }
        else if (cardIdx == 3)
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
                var outl = rosterSkill3TagText.transform.parent.GetComponent<Outline>();
                if (outl != null) outl.effectColor = tagCol;
            }
            if (rosterSkill3CdText != null) rosterSkill3CdText.text = cdStr;
            if (rosterSkill3TitleText != null)
            {
                rosterSkill3TitleText.text = displayTitle;
                rosterSkill3TitleText.enableAutoSizing = true;
                rosterSkill3TitleText.fontSizeMin = 11.5f;
                rosterSkill3TitleText.fontSizeMax = 15f;
            }
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
        aiRt.anchoredPosition = GetAvatarCardVisualOffset(heroId);
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
            AttachPointerClick(tObj, () =>
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
            tmp.fontSize = 12f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8.5f;
            tmp.fontSizeMax = 12.5f;
            tmp.enableWordWrapping = false;
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

    public Sprite GetHeroAvatarSprite(string heroId)
    {
        string norm = HeroCostumeManager.NormalizeHeroId(heroId);

        // 1. Ưu tiên lấy chân dung khuôn mặt cận cảnh chuẩn Soulslike
        Sprite av = Resources.Load<Sprite>($"UI/Avatars/Avatar_{norm}");
        if (av == null)
        {
            string alias = "vael";
            if (norm == CharacterManager.CHAR_TITAN_BERSERKER) alias = "titan";
            else if (norm == CharacterManager.CHAR_AZURE_BLADE) alias = "azure";
            else if (norm == CharacterManager.CHAR_GALE_RONIN) alias = "ronin";
            else if (norm == CharacterManager.CHAR_DUNE_ASSASSIN) alias = "dune";
            else if (norm == CharacterManager.CHAR_CELESTIAL_MAGE) alias = "mage";
            av = Resources.Load<Sprite>($"UI/Avatars/avatar_{alias}");
        }
        if (av != null) return av;

        EnsureHeroSprites();
        if (norm == CharacterManager.CHAR_TITAN_BERSERKER) return (titanIdleFrames != null && titanIdleFrames.Length > 0) ? titanIdleFrames[0] : null;
        if (norm == CharacterManager.CHAR_AZURE_BLADE) return (azureIdleFrames != null && azureIdleFrames.Length > 0) ? azureIdleFrames[0] : null;
        if (norm == CharacterManager.CHAR_GALE_RONIN) return (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames[0] : null;
        if (norm == CharacterManager.CHAR_DUNE_ASSASSIN) return (duneIdleFrames != null && duneIdleFrames.Length > 0) ? duneIdleFrames[0] : null;
        if (norm == CharacterManager.CHAR_CELESTIAL_MAGE) return (mageIdleFrames != null && mageIdleFrames.Length > 0) ? mageIdleFrames[0] : null;
        return (vaelIdleFrames != null && vaelIdleFrames.Length > 0) ? vaelIdleFrames[0] : null;
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
            rosterBackBtn.interactable = true;
            rosterBackBtn.onClick.RemoveAllListeners();
            rosterBackBtn.onClick.AddListener(CloseRosterPanel);
            AttachPointerClick(rosterBackBtn.gameObject, CloseRosterPanel);
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
        // 1. Thuan Game Offline: An hoan toan truong hien thi tai khoan / "HUNG"
        if (playerNameText != null)
        {
            playerNameText.gameObject.SetActive(false);
        }
        else
        {
            var tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in tmps)
            {
                if (t != null && (t.name.ToLower().Contains("playername") || t.text == "HUNG" || t.text == "HUNG1"))
                {
                    playerNameText = t;
                    t.gameObject.SetActive(false);
                    break;
                }
            }
        }

        if (titleText != null) titleText.text = LocalizationManager.Get("APP_TITLE");
        if (playerTitleText != null)
        {
            playerTitleText.text = LocalizationManager.Get("LBL_PLAYER_TITLE");
            var rt = playerTitleText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(35f, 0f);
            }
        }
        if (logoutButton != null) logoutButton.gameObject.SetActive(false);

        if (playBtnText != null)
        {
            playBtnText.text = LocalizationManager.Get("BTN_PLAY");
            if (playButton != null) WhiteIconLoader.SetButtonIconAndText(playButton, "Play", playBtnText.text, Color.white, Color.white, true);
        }
        if (mapSelectBtnText != null) mapSelectBtnText.text = LocalizationManager.Get("BTN_MAP_SELECT");
        if (rosterButton != null) rosterButton.gameObject.SetActive(false);
        if (inventoryButton != null) inventoryButton.gameObject.SetActive(false);
        if (shopButton != null) shopButton.gameObject.SetActive(false);
        if (skillButton != null) skillButton.gameObject.SetActive(false);

        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(true);
            WhiteIconLoader.ReplaceTextWithIcon(settingsButton, "Gear", new Color(1f, 0.88f, 0.45f), new Vector2(30f, 30f));
        }

        if (heroSelectCarouselModal != null && heroSelectCarouselModal.activeSelf)
        {
            UpdateCarouselDisplay();
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
        if (rosterBackBtn != null)
        {
            WhiteIconLoader.ReplaceTextWithIcon(rosterBackBtn, "Cross", Color.white, new Vector2(26f, 26f));
            foreach (var img in rosterBackBtn.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject != rosterBackBtn.gameObject) img.raycastTarget = false;
            }
        }

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
                "• <b>Đòn Đánh & Cầu Ma Pháp:</b> Bắn cầu ma pháp tự dẫn đường và bộc phát sét lan. <color=#FFDF66>Nhấn Giữ Hộ Thể Ma Trận:</color> Giảm 60% sát thương, bộc phá sóng năng lượng đẩy lùi & hồi <color=#38BDF8><b>+10 Mana</b></color>!\n" +
                "• <b>Lướt (Tốc Biến Hư Không):</b> Dịch chuyển tức thời 3.2m né đòn, để lại Vết Rách Hư Không gây AP liên tục & hồi <color=#38BDF8><b>+12 Mana</b></color>.\n" +
                "• <b>Tuyệt Kỹ & Thần Nộ:</b> Triệu hồi mưa thiên thạch ma pháp toàn màn hình. <color=#FFDF66>Đầy 100% Nội Năng tím kích hoạt Overdrive (8s):</color> Hồi chiêu cuối giảm về <color=#55FF88><b>1.0s</b></color> để xả bão thiên thạch liên tục!",
                "• <b>Attack & Arcane Orbs:</b> Fires homing arcane star orbs chaining lightning. <color=#FFDF66>Hold attack for Barrier:</color> absorbs 60% damage, bursts back enemies & grants <color=#38BDF8><b>+10 Mana</b></color>!\n" +
                "• <b>Dash (Arcane Blink):</b> Instantly warps 3.2m dodging hits, leaves Singularity Rift dealing continuous AP & restores <color=#38BDF8><b>+12 Mana</b></color>.\n" +
                "• <b>Ultimate & Overdrive:</b> Summons meteor tempest across battlefield. <color=#FFDF66>100% Inner Energy triggers Overdrive (8s):</color> Ultimate CD drops to <color=#55FF88><b>1.0s</b></color> for continuous meteor spam!");
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

            // 0. NỘI TẠI
            SetHeroSkillCard(0, WhiteIconLoader.GetSprite("Star") ?? MobileActionButton.GetHeroActionIcon("titan", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("NỘI TẠI", "PASSIVE"),
                LocalizationManager.T("NỘI TẠI: DA DUNG NHAM CHIẾN THẦN", "PASSIVE: VOLCANIC CARAPACE"),
                LocalizationManager.T("Thân thể bọc giáp dung nham vĩnh cửu: <color=#55FF88><b>Giảm 20% sát thương nhận vào</b></color> & nhận thêm <color=#FFAA33><b>+30% Kháng Khống Chế</b></color> xuyên suốt trận chiến.",
                                     "Molten lava armor: Permanently gains <color=#55FF88><b>20% Damage Reduction</b></color> & <color=#FFAA33><b>+30% CC Resistance</b></color> throughout the entire battle."),
                0f, sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 1. CHIÊU 1 (TẤN CÔNG / GỒNG)
            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("titan", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("GỒNG / TANK", "HOLD / TANK"),
                LocalizationManager.T("1. ĐẠI CHÙY (GỒNG ĐỊA CHẤN)", "1. WAR HAMMER (EARTHSHAKER)"),
                LocalizationManager.T("Nện đại chùy càn quét gây <color=#FF3B30><b>ATK</b></color>. <color=#FFDF66><b>[Gồng Thế Thủ]:</b></color> <color=#55FF88><b>Bất tử 100%</b></color>, thả tay nện đất bộc phá gây <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Choáng 1.0s</b></color>! (Hồi chiêu gồng: 5s - 10s).",
                                     "Massive hammer smashes inflicting <color=#FF3B30><b>ATK</b></color>. <color=#FFDF66><b>[Hold Earthshaker]:</b></color> <color=#55FF88><b>100% Invulnerable</b></color>, release to smash ground inflicting <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Stun 1.0s</b></color>! (Charge CD: 5s - 10s)."),
                baseCds[0], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 2. CHIÊU 2 (LƯỚT)
            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("titan", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("LƯỚT", "DASH"),
                LocalizationManager.T("2. CHIẾN XA XOAY BÚA 360°", "2. JUGGERNAUT 360° SPIN"),
                LocalizationManager.T("Lướt tốc độ cao xoay búa 360° càn quét gây <color=#FF3B30><b>ATK</b></color> & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>.",
                                     "Heavy spin dash slashing all enemies along path with <color=#FF3B30><b>ATK</b></color> & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>."),
                baseCds[1], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 3. CHIÊU 3 (TUYỆT KỸ / ULTI)
            SetHeroSkillCard(3, MobileActionButton.GetHeroActionIcon("titan", MobileActionButton.ButtonType.Skill),
                LocalizationManager.T("TUYỆT KỸ", "ULTIMATE"),
                LocalizationManager.T("3. CUỒNG NỘ CHẤN ĐỊA DUNG NHAM", "3. VOLCANIC CATACLYSM SLAM"),
                LocalizationManager.T("Tạo tâm chấn hút quái vật (5.5m), nhảy cao nện búa dung nham bộc phát diện rộng & <color=#55FF88><b>Nhận Máu Ảo bằng 50% HP hiện tại trong 15s</b></color>!",
                                     "Creates a catastrophic vortex epicenter pulling all enemies (5.5m), leaps to smash down volcanic hammer eruptions with massive AoE knockdown & <color=#55FF88><b>Gains Barrier equal to 50% Current HP for 15s</b></color>!"),
                baseCds[2], sCd, upDashCd, upUltiCd, "<color=#00E5FF><b>[45 Mana]</b></color>");

            bool isSelected = (currentSelected == CharacterManager.CHAR_TITAN_BERSERKER);
            if (rosterActionBtnText != null) rosterActionBtnText.text = isSelected ? LocalizationManager.T("ĐANG CHỌN XUẤT TRẬN", "SELECTED FOR BATTLE") : LocalizationManager.T("CHỌN XUẤT TRẬN", "SELECT FOR BATTLE");
            if (rosterActionBtn != null)
            {
                rosterActionBtn.interactable = true;
                var img = rosterActionBtn.GetComponent<Image>();
                if (img != null) img.color = isSelected ? new Color(0.18f, 0.55f, 0.28f, 1f) : new Color(0.18f, 0.45f, 0.75f, 1f);
            }
        }
        else if (inspectedHeroId == "azure" || inspectedHeroId == "azureblade" || inspectedHeroId == "player3")
        {
            if (rosterHeroPreviewImage != null && azureIdleFrames != null && azureIdleFrames.Length > 0)
                rosterHeroPreviewImage.sprite = azureIdleFrames[0];
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Lam Kiếm Sĩ (Azure Blade)", "Azure Blade (Frost Sword)");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("<color=#38BDF8>[XẠ THỦ]</color> Sóng Kiếm Băng / Xạ Thủ & Băng Phong", "<color=#38BDF8>[MARKSMAN]</color> Frost Sword / Marksman & Ice");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 110   CÔNG: 24   TỐC: 5.2", "HP: 110   ATK: 24   SPD: 5.2");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("BỘ KỸ NĂNG: LAM KIẾM BĂNG SƯ", "SKILLSET: AZURE FROST BLADE");

            // 0. NỘI TẠI
            SetHeroSkillCard(0, WhiteIconLoader.GetSprite("Star") ?? MobileActionButton.GetHeroActionIcon("azure", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("NỘI TẠI", "PASSIVE"),
                LocalizationManager.T("NỘI TẠI: HÀN TÂM & KIẾM KHÍ QUY HÓA", "PASSIVE: FROST SOUL & FLOWING RESONANCE"),
                LocalizationManager.T("Đòn đánh trúng đích hồi ngay <color=#38BDF8><b>+6 Mana</b></color> & giảm 50% thời gian chờ đòn đánh kế tiếp. Đặc biệt: Toàn bộ <b>Tốc Đánh & Tốc Độ cộng thêm</b> tự động chuyển hóa thành <b>Sát Thương Phép (AP)</b> cho Sóng Kiếm!",
                                     "Basic attacks restore <color=#38BDF8><b>+6 Mana</b></color> & cut next basic attack cooldown by 50%. Special: All <b>bonus Attack Speed & Speed</b> automatically convert into powerful <b>Magic Damage (AP)</b> for Sword Waves!"),
                0f, sCd, upDashCd, upUltiCd, "<color=#10B981><b>0 Mana</b></color>");

            // 1. CHIÊU 1 (TẤN CÔNG)
            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("azure", MobileActionButton.ButtonType.Attack),
                "AP",
                LocalizationManager.T("1. TRỌNG KIẾM & SÓNG BĂNG KIẾM KHÍ", "1. FROST SWORD WAVES"),
                LocalizationManager.T("Chém phóng <color=#55FFFF><b>Sóng Kiếm Băng</b></color> gây <color=#B84DFF><b>AP</b></color> xuyên thấu (6.5m). Đòn đánh găm <color=#FF2961><b>Vết Thương Sâu (Trái Tim Nứt)</b></color> 5s: <color=#FF4D79><b>giảm 50% hồi máu & hút máu</b></color> của địch và tăng <color=#55FFFF><b>+20% sát thương băng</b></color> nhận vào! <color=#FFDF66><b>[Nhấn Giữ Tụ Lực (1s)]:</b></color> Sát thương tới <color=#FFAA33><b>x2.25 (225% AP)</b></color>!",
                                     "Heavy sword slashes unleashing <color=#55FFFF><b>Frost Sword Waves</b></color> with <color=#B84DFF><b>AP</b></color> (6.5m). Hits inflict <color=#FF2961><b>Grievous Wounds (Cracked Heart)</b></color> for 5s: <color=#FF4D79><b>reduces enemy healing & lifesteal by 50%</b></color> and increases Frost damage taken by <color=#55FFFF><b>+20%</b></color>! <color=#FFDF66><b>[Hold to Charge (1s)]:</b></color> Amplifies damage up to <color=#FFAA33><b>x2.25 (225% AP)</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 2. CHIÊU 2 (LƯỚT)
            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("azure", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("LƯỚT", "DASH"),
                LocalizationManager.T("2. HÀN BĂNG TRƯỢT TUYẾT", "2. FROST GLIDE DASH"),
                LocalizationManager.T("Lướt trượt nhanh trên dải băng tuyết & <color=#55FFFF><b>Hồi ngay lập tức đòn đánh thường</b></color> kế tiếp.",
                                     "Swift glide dash over frost tracks & <color=#55FFFF><b>Instantly resets basic attack cooldown</b></color>."),
                baseCds[1], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 3. CHIÊU 3 (TUYỆT KỸ / ULTI)
            SetHeroSkillCard(3, MobileActionButton.GetHeroActionIcon("azure", MobileActionButton.ButtonType.Skill),
                "AP",
                LocalizationManager.T("3. BÃO SÓNG KIẾM NAN QUẠT", "3. CRESCENT TEMPEST"),
                LocalizationManager.T("Tụ nội lực phóng 5 đợt kiếm khí băng lam nan quạt khổng lồ gây <color=#B84DFF><b>AP diện rộng</b></color> quét sạch toàn bộ chiến trường.",
                                     "Releases 5 massive crescent frost sword waves in a wide fan spread inflicting devastating AoE <color=#B84DFF><b>AP</b></color> across the battlefield."),
                baseCds[2], sCd, upDashCd, upUltiCd, "<color=#00E5FF><b>[50 Mana]</b></color>");

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

            // 0. NỘI TẠI
            SetHeroSkillCard(0, WhiteIconLoader.GetSprite("Star") ?? MobileActionButton.GetHeroActionIcon("ronin", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("NỘI TẠI", "PASSIVE"),
                LocalizationManager.T("NỘI TẠI: PHONG LÔI PHONG ẤN", "PASSIVE: WIND SEAL BURST"),
                LocalizationManager.T("Tích ấn kiếm khí khi chém: Đủ <color=#FFDF66><b>3 tầng ấn</b></color> bộc phát nổ lốc xoáy gây <color=#FFAA33><b>240% Bạo Kích Crit</b></color> & hút quái. Diệt địch hồi ngay <color=#38BDF8><b>+10 Mana</b></color>.",
                                     "Accumulates wind marks on attack: Reaching <color=#FFDF66><b>3 stacks</b></color> triggers a spatial gale vortex bursting for <color=#FFAA33><b>240% Crit Damage</b></color> & pulls foes. Slaying foes grants <color=#38BDF8><b>+10 Mana</b></color>."),
                0f, sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 1. CHIÊU 1 (TẤN CÔNG / GỒNG)
            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("ronin", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("GỒNG / IAIDO", "HOLD / IAIDO"),
                LocalizationManager.T("1. PHONG TRẢM (GỒNG TỤ LỰC)", "1. GALE SLASH (CHARGE)"),
                LocalizationManager.T("Chém quét 180° gây <color=#FF3B30><b>ATK</b></color>. <color=#FFDF66><b>[ĐIỀU KIỆN: ĐẦY NỘI NĂNG / KHIÊN HỘ THÂN]:</b></color> Nhấn giữ gồng Iaido phóng Nguyệt Kiếm Khí xuyên thấu <color=#FFAA33><b>x2.85 Crit DMG</b></color>! Max Charge trúng quái giúp <color=#FFAA33><b>Giảm 35% CD Tuyệt Kỹ</b></color> & hồi ngay <color=#38BDF8><b>+25 Mana</b></color>!",
                                     "180° slashes dealing <color=#FF3B30><b>ATK</b></color>. <color=#FFDF66><b>[REQ: FULL ENERGY / SHIELD ACTIVE]:</b></color> Hold to charge Iaido unleashing Crescent Wave for <color=#FFAA33><b>x2.85 Crit DMG</b></color>! Max Charge hit cuts <color=#FFAA33><b>Ulti CD by 35%</b></color> & restores <color=#38BDF8><b>+25 Mana</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 2. CHIÊU 2 (LƯỚT / TỐC BIẾN)
            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("ronin", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("TỐC BIẾN", "WARP"),
                LocalizationManager.T("2. TÀN ẢNH TỐC BIẾN (MURAD STYLE)", "2. SHADOW PHANTOM WARP"),
                LocalizationManager.T("Nhịp 1 tốc biến 4.8m tới trước (<color=#55FF88><b>Bất tử 0.25s</b></color>) cắm bóng 4.5s. Nhịp 2 Teleport quay về bóng (<color=#55FF88><b>Bất tử 0.30s</b></color>) & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>!",
                                     "Stage 1 warps 4.8m forward leaving clone for 4.5s (<color=#55FF88><b>Invincible 0.25s</b></color>). Stage 2 Warps back to clone (<color=#55FF88><b>Invincible 0.30s</b></color>) & <color=#FFAA33><b>Cuts Ulti CD by 2.0s</b></color>!"),
                baseCds[1], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 3. CHIÊU 3 (TUYỆT KỸ / ULTI)
            SetHeroSkillCard(3, MobileActionButton.GetHeroActionIcon("ronin", MobileActionButton.ButtonType.Skill),
                "TD",
                LocalizationManager.T("3. ÁM ẢNH PHÂN THÂN TRẢM", "3. PHANTOM FLASH CLEAVE"),
                LocalizationManager.T("Lướt ảo ảnh chớp nhoáng trảm sát liên hoàn mọi kẻ địch trong không gian gây <color=#E2E8F0><b>TD (bỏ qua giáp)</b></color>, <color=#55FF88><b>Bất tử hoàn toàn</b></color>. <color=#FFDF66><b>Kết liễu kẻ địch hồi ngay lập tức Tuyệt Kỹ!</b></color>",
                                     "Executes blinding phantom dash slashes cutting all enemies in space dealing <color=#E2E8F0><b>TD (ignores defense)</b></color> with <color=#55FF88><b>Complete Invincibility</b></color>. <color=#FFDF66><b>Executing enemies instantly resets Ultimate CD!</b></color>"),
                baseCds[2], sCd, upDashCd, upUltiCd, "<color=#00E5FF><b>[50 Mana]</b></color>");

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

            // 0. NỘI TẠI
            SetHeroSkillCard(0, WhiteIconLoader.GetSprite("Star") ?? MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("NỘI TẠI", "PASSIVE"),
                LocalizationManager.T("NỘI TẠI: HUYẾT SA TẨY LỄ", "PASSIVE: BLOOD SAND REBIRTH"),
                LocalizationManager.T("Dùng 2 chiêu bất kỳ hồi ngay <color=#22C55E><b>10% HP đã mất</b></color>. Đánh thường tích Nộ; khi <color=#FF3B30><b>Đầy 100% Nộ</b></color> cường hóa lốc xoáy thành <color=#FF3B30><b>Cơn Lốc Đỏ</b></color> uy lực cực đại.",
                                     "Every 2 skill casts restores <color=#22C55E><b>10% Missing HP</b></color>. Attacks build Rage; at <color=#FF3B30><b>100% Rage</b></color> empowers vortex into a devastating <color=#FF3B30><b>Crimson Vortex</b></color>."),
                0f, sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 1. CHIÊU 1 (TẤN CÔNG / GỒNG THỦ)
            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("GỒNG / ATK", "HOLD / ATK"),
                LocalizationManager.T("1. SONG ĐAO (GỒNG THỦ)", "1. DUAL DAGGERS (PARRY)"),
                LocalizationManager.T("Đòn 1-2 chém bạo kích cao. Đòn 3 phóng <color=#FFDF66><b>Lốc Sa Mạc</b></color> cuốn quái (tiêu hao 8% HP, CD nội tại 3.5s). <color=#FFDF66><b>[Gồng Thủ / Đầy Nộ]:</b></color> Giảm 70% ST, phóng <color=#FF3B30><b>Lốc Đỏ</b></color> (12% HP) gây <color=#E2E8F0><b>TD</b></color>, <color=#FFAA33><b>Choáng 0.75s</b></color> & <color=#FFAA33><b>Ngắt chiêu Boss</b></color>!",
                                     "Combo slashes. Hit 3 unleashes <color=#FFDF66><b>Sand Vortex</b></color> (8% HP, 3.5s CD). <color=#FFDF66><b>[Hold Parry / Full Rage]:</b></color> 70% DR, unleashes <color=#FF3B30><b>Crimson Vortex</b></color> (12% HP) dealing <color=#E2E8F0><b>TD</b></color>, <color=#FFAA33><b>0.75s Stun</b></color> & <color=#FFAA33><b>Interrupts Bosses</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 2. CHIÊU 2 (LƯỚT)
            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("ẢO ẢNH", "MIRAGE"),
                LocalizationManager.T("2. ẢO ẢNH SA LƯỚT", "2. TRIPLE MIRAGE DASH"),
                LocalizationManager.T("Tích tối đa <color=#55FFFF><b>3 lần lướt liên hoàn</b></color> (tiêu hao 4% HP), tạo tàn ảnh cát & <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> mỗi lần lướt.",
                                     "Stores up to <color=#55FFFF><b>3 Dash Charges</b></color> (consumes 4% HP), leaves mirages & <color=#FFAA33><b>Reduces Ulti CD 1.5s</b></color> per dash."),
                baseCds[1], sCd, upDashCd, upUltiCd, "<color=#FF4757><b>[4% HP]</b></color>");

            // 3. CHIÊU 3 (TUYỆT KỸ / ULTI)
            SetHeroSkillCard(3, MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Skill),
                LocalizationManager.T("TUYỆT KỸ", "ULTIMATE"),
                LocalizationManager.T("3. CUỒNG BÃO SA ẢNH X-REND", "3. SANDSTORM X-REND"),
                LocalizationManager.T("Hút quái vật vào tâm bão cát và chém chữ X thần tốc gây <color=#E2E8F0><b>TD cực lớn (bỏ qua giáp)</b></color>, <color=#55FF88><b>Hút máu 50% sát thương gây ra</b></color> & gây Chảy Máu Sa Mạc liên tục.",
                                     "Draws enemies into a blinding sand vortex and executes rapid Cross-X Slash with massive <color=#E2E8F0><b>TD</b></color>, <color=#55FF88><b>50% Lifesteal from damage dealt</b></color> & continuous Sand Bleed."),
                baseCds[2], sCd, upDashCd, upUltiCd, "<color=#FF4757><b>[20% HP]</b></color>");

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

            // 0. NỘI TẠI
            SetHeroSkillCard(0, WhiteIconLoader.GetSprite("Star") ?? MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("NỘI TẠI", "PASSIVE"),
                LocalizationManager.T("NỘI TẠI: TINH TÚ CỘNG HƯỞNG", "PASSIVE: ASTRAL RESONANCE"),
                LocalizationManager.T("Đòn đánh có 35% bộc phát sét lan & hồi <color=#38BDF8><b>+4 Mana</b></color>. <color=#FFDF66><b>[THẦN NỘ TINH TÚ]:</b></color> Đầy 100% Nội Năng tím kích hoạt <color=#B84DFF><b>Overdrive (8s)</b></color>: Hồi Tuyệt Kỹ giảm về <color=#55FF88><b>1.0s</b></color> (xả thiên thạch liên tục), tiêu hao giảm còn 25 Mana & hồi ngay <color=#38BDF8><b>+25 Mana</b></color>!",
                                     "Attacks have 35% chance to chain lightning & restore <color=#38BDF8><b>+4 Mana</b></color>. <color=#FFDF66><b>[CELESTIAL OVERDRIVE]:</b></color> 100% Inner Energy triggers <color=#B84DFF><b>Overdrive (8s)</b></color>: Ultimate CD drops to <color=#55FF88><b>1.0s</b></color> (meteor spam), cost reduced to 25 Mana & restores <color=#38BDF8><b>+25 Mana</b></color>!"),
                0f, sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 1. CHIÊU 1 (TẤN CÔNG / GỒNG KHIÊN)
            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("GỒNG / HỘ THỂ", "HOLD / BARRIER"),
                LocalizationManager.T("1. CẦU MA PHÁP (GỒNG KHIÊN)", "1. ARCANE ORBS (BARRIER)"),
                LocalizationManager.T("Bắn cầu ma pháp tự dẫn đường thần tốc gây <color=#B84DFF><b>AP</b></color>. <color=#FFDF66><b>[Nhấn Giữ Gồng Khiên Hộ Thể]:</b></color> Giảm 60% sát thương nhận vào, nổ đẩy lùi phản đòn <color=#B84DFF><b>AP</b></color> & hồi ngay <color=#38BDF8><b>+10 Mana</b></color>!",
                                     "Fires high-speed homing celestial orb dealing <color=#B84DFF><b>AP</b></color>. <color=#FFDF66><b>[Hold Astral Barrier]:</b></color> 60% DR, bursts back foes with <color=#B84DFF><b>AP</b></color> & restores <color=#38BDF8><b>+10 Mana</b></color>!"),
                baseCds[0], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 2. CHIÊU 2 (LƯỚT / TỐC BIẾN)
            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("TỐC BIẾN", "BLINK"),
                LocalizationManager.T("2. TỐC BIẾN HƯ KHÔNG (BLINK)", "2. ARCANE BLINK DASH"),
                LocalizationManager.T("Dịch chuyển tức thời 3.2m né đòn, để lại Vết Rách Hư Không gây <color=#B84DFF><b>AP liên tục</b></color>, <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> & hồi ngay <color=#38BDF8><b>+12 Mana</b></color>.",
                                     "Instant 3.2m void warp to dodge, leaving a Singularity Rift dealing <color=#B84DFF><b>continuous AP</b></color>, <color=#FFAA33><b>Reduces Ulti CD by 1.5s</b></color> & restores <color=#38BDF8><b>+12 Mana</b></color>."),
                baseCds[1], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 3. CHIÊU 3 (TUYỆT KỸ / ULTI)
            SetHeroSkillCard(3, MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Skill),
                "AP",
                LocalizationManager.T("3. THIÊN THẠCH MA TRẬN", "3. METEOR APOCALYPSE"),
                LocalizationManager.T("Mở đại pháp trận tinh tú triệu hồi 5 đợt mưa thiên thạch ma pháp gây <color=#B84DFF><b>AP diện rộng</b></color> toàn bản đồ. <color=#FFDF66><b>[Khi Overdrive]:</b></color> Hồi chiêu rút ngắn chỉ còn <color=#55FF88><b>1.0s</b></color> để xả bão thiên thạch liên hoàn!",
                                     "Opens celestial circles summoning 5 waves of devastating meteor strikes inflicting catastrophic AoE <color=#B84DFF><b>AP</b></color>. <color=#FFDF66><b>[During Overdrive]:</b></color> CD drops to <color=#55FF88><b>1.0s</b></color> for continuous meteor storms!"),
                baseCds[2], sCd, upDashCd, upUltiCd, "<color=#00E5FF><b>[60 Mana]</b></color>");

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

            // 0. NỘI TẠI
            SetHeroSkillCard(0, WhiteIconLoader.GetSprite("Star") ?? MobileActionButton.GetHeroActionIcon("vael", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("NỘI TẠI", "PASSIVE"),
                LocalizationManager.T("NỘI TẠI: THÁNH QUANG BẢO HỘ", "PASSIVE: RADIANT AEGIS"),
                LocalizationManager.T("Đòn đánh thường trúng đích hồi ngay <color=#38BDF8><b>+8 Mana</b></color>. Khi máu xuống dưới 35%, tự động kích hoạt Thánh Giáp Thiên Thần hồi phục ngay <color=#55FF88><b>25% HP</b></color>.",
                                     "Basic attacks restore <color=#38BDF8><b>+8 Mana</b></color> on hit. When HP drops below 35%, activates Divine Aegis healing <color=#55FF88><b>25% HP</b></color>."),
                0f, sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 1. CHIÊU 1 (TẤN CÔNG / GỒNG PHẢN ĐÒN)
            SetHeroSkillCard(1, MobileActionButton.GetHeroActionIcon("vael", MobileActionButton.ButtonType.Attack),
                LocalizationManager.T("GỒNG / PHẢN", "HOLD / PARRY"),
                LocalizationManager.T("1. ĐẠI KIẾM (GỒNG THẾ THỦ)", "1. ANCIENT SWORD (PARRY)"),
                LocalizationManager.T("Chém kiếm 3 nhịp gây <color=#FF3B30><b>ATK</b></color> (hồi <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66><b>[Nhấn Giữ Gồng Thế Thủ]:</b></color> Giảm 20% sát thương, thả tay bộc phá Thần Kiếm Phản Kích gây <color=#FF3B30><b>x3.2 Crit ATK</b></color> phóng luồng thánh quang xuyên phá!",
                                     "3-hit sword combo dealing <color=#FF3B30><b>ATK</b></color> (restores <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66><b>[Hold Parry]:</b></color> 20% DR, release to unleash Divine Riposte dealing <color=#FF3B30><b>x3.2 Crit ATK</b></color> firing holy shockwave pillars!"),
                baseCds[0], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 2. CHIÊU 2 (LƯỚT / THÁNH THUẪN)
            SetHeroSkillCard(2, MobileActionButton.GetHeroActionIcon("vael", MobileActionButton.ButtonType.Dash),
                LocalizationManager.T("THÁNH THUẪN", "SHIELD"),
                LocalizationManager.T("2. THÁNH THUẪN PHẢN ĐÒN", "2. RADIANT RIPOSTE DASH"),
                LocalizationManager.T("Lướt né đòn (<color=#55FF88><b>Bất tử khi lướt</b></color>). Lướt qua quái hoặc đỡ đòn thành công sẽ <color=#FFAA33><b>Hồi 50% CD Lướt</b></color>, hồi <color=#38BDF8><b>+15 Mana</b></color> & phóng 3 luồng Thánh Kiếm Khí gây <color=#FF3B30><b>ATK</b></color>!",
                                     "Holy aegis dash (<color=#55FF88><b>Invulnerable</b></color>). Deflecting attacks <color=#FFAA33><b>Refunds 50% Dash CD</b></color>, restores <color=#38BDF8><b>+15 Mana</b></color> & fires 3 Holy Waves with <color=#FF3B30><b>ATK</b></color>!"),
                baseCds[1], sCd, upDashCd, upUltiCd, "<color=#10B981><b>[0 Mana]</b></color>");

            // 3. CHIÊU 3 (TUYỆT KỸ / ULTI)
            SetHeroSkillCard(3, MobileActionButton.GetHeroActionIcon("vael", MobileActionButton.ButtonType.Skill),
                LocalizationManager.T("TUYỆT KỸ", "ULTIMATE"),
                LocalizationManager.T("3. THÁNH KIẾM THIÊN NHAM", "3. HOLY SWORD TEMPEST"),
                LocalizationManager.T("Xoay bão kiếm 360° hút gom toàn bộ kẻ địch vào tâm bão và triệu hồi Thánh Kiếm Thiên Nham khổng lồ giáng thế từ không trung gây sát thương cực đại diện rộng.",
                                     "360° sword vortex storm pulling enemies and summons a gigantic Holy Sword of the Heavens slamming down with devastating AoE impact."),
                baseCds[2], sCd, upDashCd, upUltiCd, "<color=#00E5FF><b>[50 Mana]</b></color>");

            bool isSelected = (currentSelected == CharacterManager.CHAR_VAELSTRIDER);
            if (rosterActionBtnText != null) rosterActionBtnText.text = isSelected ? LocalizationManager.T("ĐANG CHỌN XUẤT TRẬN", "SELECTED FOR BATTLE") : LocalizationManager.T("CHỌN XUẤT TRẬN", "SELECT FOR BATTLE");
            if (rosterActionBtn != null)
            {
                rosterActionBtn.interactable = true;
                var img = rosterActionBtn.GetComponent<Image>();
            }
        }

        if (rosterHeroPreviewImage != null)
        {
            rosterHeroPreviewImage.transform.localScale = GetHeroVisualScale(inspectedHeroId);
            RectTransform rt = rosterHeroPreviewImage.rectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = GetHeroVisualOffset(inspectedHeroId);
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
        int totalCritPct = 20 + Mathf.RoundToInt(sCrit * 100);
        int physLife = Mathf.RoundToInt((sPhysLife > 0f ? sPhysLife : sLife) * 100);
        int spellVamp = Mathf.RoundToInt(sSpellVamp * 100);
        int cdPct = Mathf.RoundToInt(sCd * 100);
        int shardPct = Mathf.RoundToInt(sShard * 100);

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

            string critBonusStr = sCrit > 0f ? $" <color=#F59E0B>(+{Mathf.RoundToInt(sCrit * 100)}%)</color>" : "";

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

            int equippedCount = 0;
            for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT; s++)
            {
                if (EquipmentSocketManager.GetHeroSocketGem(s, out _, out _, hero)) equippedCount++;
            }

            if (equippedCount == 6)
            {
                statLines.Add($"<color=#FBBF24><b>★ {(isVi ? "CỘNG HƯỞNG LỤC BẢO:" : "HEXA RESONANCE:")}</b></color> <color=#34D399>+10% {(isVi ? "Toàn bộ Sát Thương & Kháng Hiệu Ứng" : "All Damage & Tenacity")}</color>");
            }
            else
            {
                statLines.Add($"<color=#94A3B8><b>{(isVi ? "CỘNG HƯỞNG LỤC BẢO:" : "HEXA RESONANCE:")}</b> ({equippedCount}/6 - {(isVi ? "Khảm đủ 6 ô để +10% Sát thương" : "Equip 6 slots for +10% Dmg")})</color>");
            }

            rosterHeroStatsText.text = string.Join("\n", statLines);
        }

        if (rosterHeroQuickStatsText != null)
        {
            rosterHeroQuickStatsText.text = isVi
                ? $"<b>HP:</b> <color=#22C55E>{totalHp}</color>   <b>GIÁP:</b> <color=#38BDF8>{sDef}</color>   <b>CÔNG:</b> <color=#FF3B30>{totalAtk}</color>   <b>CHÍ MẠNG:</b> <color=#F59E0B>{totalCritPct}%</color>   <b>MANA:</b> <color=#00E5FF>{100 + (int)sMaxEn}</color>   <b>HÚT MÁU:</b> <color=#F43F5E>{physLife}%</color>"
                : $"<b>HP:</b> <color=#22C55E>{totalHp}</color>   <b>DEF:</b> <color=#38BDF8>{sDef}</color>   <b>ATK:</b> <color=#FF3B30>{totalAtk}</color>   <b>CRIT:</b> <color=#F59E0B>{totalCritPct}%</color>   <b>MANA:</b> <color=#00E5FF>{100 + (int)sMaxEn}</color>   <b>VAMP:</b> <color=#F43F5E>{physLife}%</color>";
        }

        string[] slotNamesVi = { "Ô 1", "Ô 2", "Ô 3", "Ô 4", "Ô 5", "Ô 6" };
        string[] slotNamesEn = { "Slot 1", "Slot 2", "Slot 3", "Slot 4", "Slot 5", "Slot 6" };

        if (rosterGemEffectsText != null)
        {
            List<string> gemLines = new List<string>();
            int equippedCount = 0;
            for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT; s++)
            {
                string sName = isVi ? slotNamesVi[s] : slotNamesEn[s];
                if (EquipmentSocketManager.GetHeroSocketGem(s, out string gemId, out int lv, hero))
                {
                    equippedCount++;
                    var eff = EquipmentSocketManager.GetGemEffect(gemId);
                    string gName = eff.HasValue ? (isVi ? eff.Value.nameVi : eff.Value.nameEn) : gemId;
                    Color gCol = eff.HasValue ? eff.Value.gemColor : Color.white;
                    string gHex = ColorUtility.ToHtmlStringRGB(gCol);
                    string rawSummary = eff.HasValue ? (isVi ? eff.Value.effectSummaryVi : eff.Value.effectSummaryEn) : "";
                    string coloredSummary = EquipmentSocketManager.FormatColoredStatSummary(rawSummary);
                    gemLines.Add($"<color=#E2E8F0>•</color> <b>{sName}:</b> <color=#{gHex}>{gName} Lv.{lv}</color>\n    {coloredSummary}");
                }
                else
                {
                    gemLines.Add($"<color=#64748B>•</color> <color=#94A3B8><b>{sName}:</b></color> <color=#64748B><i>{(isVi ? "Trống (Kéo vật phẩm vào ô)" : "Empty (Drag item here)")}</i></color>");
                }
            }

            if (equippedCount == 6)
            {
                gemLines.Add($"\n<color=#FBBF24><b>★ {(isVi ? "CỘNG HƯỞNG LỤC BẢO KÍCH HOẠT!" : "HEXA RESONANCE ACTIVE!")}</b></color>\n<color=#34D399>+10% {(isVi ? "Toàn bộ Sát Thương & Kháng Hiệu Ứng" : "All Damage & Tenacity")}</color>");
            }
            else
            {
                gemLines.Add($"\n<color=#94A3B8><b>{(isVi ? "CỘNG HƯỞNG LỤC BẢO:" : "HEXA RESONANCE:")}</b> ({equippedCount}/6)</color>\n<color=#64748B><i>{(isVi ? "Khảm đủ 6 ô để kích hoạt +10% Sát thương." : "Equip all 6 slots for +10% Damage.")}</i></color>");
            }

            rosterGemEffectsText.text = string.Join("\n", gemLines);
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

                int curSlot = slotIdx;
                string curGemId = gemId;
                int curLevel = itemLevel;
                string curHero = inspectedHeroId;

                System.Action openPopupAction = () =>
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                    EquipmentItemDetailPopupUI.EnsureUIExists(rosterPanel != null ? rosterPanel.transform : null);
                    EquipmentItemDetailPopupUI.Show(curSlot, curGemId, curLevel, curHero, () =>
                    {
                        InspectHero(curHero);
                    });
                };

                sDrag.onClick = openPopupAction;

                var sBtn = sObj.GetComponent<Button>();
                if (sBtn != null)
                {
                    sBtn.onClick.RemoveAllListeners();
                    sBtn.onClick.AddListener(() => openPopupAction());
                }

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
                string sName = isVi ? slotNamesVi[slotIdx] : slotNamesEn[slotIdx];
                lblTmp.text = $"<size=9><color=#94A3B8><b>{sName}</b></color></size>\n<size=15><color=#64748B>+</color></size>";
                lblTmp.fontSize = 10f;
                lblTmp.fontStyle = FontStyles.Bold;
                lblTmp.alignment = TextAlignmentOptions.Center;
                lblTmp.lineSpacing = -8f;
                lblTmp.enableWordWrapping = false;
                lblTmp.overflowMode = TextOverflowModes.Truncate;
                lblTmp.raycastTarget = false;

                var sDrag = sObj.GetComponent<UIDragDropItem>();
                if (sDrag != null) Destroy(sDrag);

                var sBtn = sObj.GetComponent<Button>();
                if (sBtn != null)
                {
                    sBtn.onClick.RemoveAllListeners();
                    sBtn.onClick.AddListener(() =>
                    {
                        SoundManager.EnsureInstance()?.PlayMenuHover();
                    });
                }
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
            for (int e = 0; e < 20; e++)
            {
                GameObject emptySlot = new GameObject($"EmptyBagSlot_{e}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                emptySlot.transform.SetParent(rosterBagContainer, false);
                rosterBagItemCardObjs.Add(emptySlot);

                RectTransform esRt = emptySlot.GetComponent<RectTransform>();
                esRt.sizeDelta = new Vector2(94f, 94f);

                Image esImg = emptySlot.GetComponent<Image>();
                esImg.color = new Color(0.04f, 0.07f, 0.12f, 0.65f);
                esImg.raycastTarget = false;

                var esOutl = emptySlot.AddComponent<Outline>();
                esOutl.effectColor = new Color(0.28f, 0.38f, 0.52f, 0.25f);
                esOutl.effectDistance = new Vector2(1f, -1f);

                GameObject runeObj = new GameObject("Rune", typeof(RectTransform), typeof(CanvasRenderer));
                runeObj.transform.SetParent(emptySlot.transform, false);
                RectTransform runeRt = runeObj.GetComponent<RectTransform>();
                runeRt.anchorMin = Vector2.zero; runeRt.anchorMax = Vector2.one; runeRt.sizeDelta = Vector2.zero;
                var runeTmp = runeObj.AddComponent<TextMeshProUGUI>();
                runeTmp.text = "<color=#334155><size=18>❖</size></color>";
                runeTmp.alignment = TextAlignmentOptions.Center;
                runeTmp.raycastTarget = false;
            }

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
            emTmp.text = LocalizationManager.T("Chưa có khoáng thạch / đá quý trong túi đồ.\n<size=14><color=#64748B>Chiến đấu hoặc nhận thưởng để tìm kiếm đá quý khảm trang bị.</color></size>", "No minerals or gems in bag yet.\n<size=14><color=#64748B>Battle or claim rewards to find gems to equip.</color></size>");
            emTmp.fontSize = 18f;
            emTmp.fontStyle = FontStyles.Bold;
            emTmp.color = new Color(0.75f, 0.80f, 0.90f);
            emTmp.alignment = TextAlignmentOptions.Center;
            return;
        }

        int spawnedCardCount = 0;
        for (int i = 0; i < validItems.Count; i++)
        {
            var itemEntry = validItems[i];
            var def = InventoryItemDatabase.GetItem(itemEntry.itemId);
            var gemEff = EquipmentSocketManager.GetGemEffect(itemEntry.itemId);
            if (def == null || !gemEff.HasValue) continue;

            spawnedCardCount++;
            GameObject cardObj = new GameObject($"BagCard_{itemEntry.itemId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(rosterBagContainer, false);
            rosterBagItemCardObjs.Add(cardObj);

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(104f, 104f);

            Image cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.06f, 0.09f, 0.15f, 0.98f);
            
            // Viền Gothic tinh tế với tỉ lệ vừa khít (0.65f), hòa quyện màu ngọc
            Color frameGemCol = Color.Lerp(gemEff.Value.gemColor, Color.white, 0.15f);
            WhiteIconLoader.ApplyGothicFrame(cardObj, frameGemCol, 0.65f);

            var drag = cardObj.AddComponent<UIDragDropItem>();
            drag.itemId = itemEntry.itemId;
            drag.itemIcon = def.Icon;
            drag.itemColor = gemEff.Value.gemColor;
            drag.parentScrollRect = rosterBagContainer.GetComponentInParent<ScrollRect>();
            string targetGemId = itemEntry.itemId;
            drag.onClick = () =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                EquipmentItemDetailPopupUI.EnsureUIExists(rosterPanel != null ? rosterPanel.transform : null);
                EquipmentItemDetailPopupUI.Show(-1, targetGemId, 1, inspectedHeroId, () =>
                {
                    InspectHero(inspectedHeroId);
                });
            };

            // Subtle Gem Background Radial Glow / Tint
            GameObject glowObj = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowObj.transform.SetParent(cardObj.transform, false);
            RectTransform glowRt = glowObj.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.offsetMin = new Vector2(8f, 8f);
            glowRt.offsetMax = new Vector2(-8f, -8f);
            var glowImg = glowObj.GetComponent<Image>();
            glowImg.color = new Color(gemEff.Value.gemColor.r, gemEff.Value.gemColor.g, gemEff.Value.gemColor.b, 0.14f);
            glowImg.raycastTarget = false;

            // Center Item Icon (rõ nét, lấp đầy khung hài hòa)
            GameObject ibObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ibObj.transform.SetParent(cardObj.transform, false);
            RectTransform ibRt = ibObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0.5f, 0.5f);
            ibRt.anchorMax = new Vector2(0.5f, 0.5f);
            ibRt.pivot = new Vector2(0.5f, 0.5f);
            ibRt.anchoredPosition = new Vector2(0f, 2f);
            ibRt.sizeDelta = new Vector2(66f, 66f);
            var ibImg = ibObj.GetComponent<Image>();
            ibImg.sprite = def.Icon;
            ibImg.preserveAspect = true;
            ibImg.raycastTarget = false;

            // Quantity Stack Badge in Bottom-Right Corner: High-contrast Dark Pill with Gold Border & Bright Neon Text
            if (itemEntry.quantity > 0)
            {
                GameObject qObj = new GameObject("QtyBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                qObj.transform.SetParent(cardObj.transform, false);
                RectTransform qRt = qObj.GetComponent<RectTransform>();
                qRt.anchorMin = new Vector2(1f, 0f);
                qRt.anchorMax = new Vector2(1f, 0f);
                qRt.pivot = new Vector2(1f, 0f);
                qRt.anchoredPosition = new Vector2(-5f, 5f);
                qRt.sizeDelta = new Vector2(34f, 18f);

                Image qBg = qObj.GetComponent<Image>();
                qBg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);
                qBg.raycastTarget = false;

                var qOutline = qObj.AddComponent<Outline>();
                qOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.90f);
                qOutline.effectDistance = new Vector2(1f, -1f);

                GameObject qTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
                qTextObj.layer = 5;
                qTextObj.transform.SetParent(qObj.transform, false);
                RectTransform qtRt = qTextObj.GetComponent<RectTransform>();
                qtRt.anchorMin = Vector2.zero;
                qtRt.anchorMax = Vector2.one;
                qtRt.sizeDelta = Vector2.zero;

                var qTmp = qTextObj.AddComponent<TextMeshProUGUI>();
                string qtyStr = itemEntry.quantity > 99 ? "99+" : $"x{itemEntry.quantity}";
                qTmp.text = $"<b>{qtyStr}</b>";
                qTmp.fontSize = 11.5f;
                qTmp.fontStyle = FontStyles.Bold;
                qTmp.color = new Color(0.35f, 1f, 0.60f, 1f); // Neon bright emerald
                qTmp.alignment = TextAlignmentOptions.Center;
                qTmp.enableWordWrapping = false;
                qTmp.raycastTarget = false;
            }
        }

        // Pad with styled empty bag slots so the inventory always looks full & structured (at least 40 slots = 8 rows x 5 cols)
        int minBagSlots = 40;
        int targetSlotCount = Mathf.Max(minBagSlots, ((spawnedCardCount + 4) / 5) * 5);
        for (int e = spawnedCardCount; e < targetSlotCount; e++)
        {
            GameObject emptySlot = new GameObject($"EmptyBagSlot_{e}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            emptySlot.transform.SetParent(rosterBagContainer, false);
            rosterBagItemCardObjs.Add(emptySlot);

            RectTransform esRt = emptySlot.GetComponent<RectTransform>();
            esRt.sizeDelta = new Vector2(104f, 104f);

            Image esImg = emptySlot.GetComponent<Image>();
            esImg.color = new Color(0.04f, 0.07f, 0.12f, 0.65f);
            esImg.raycastTarget = false;

            var esOutl = emptySlot.AddComponent<Outline>();
            esOutl.effectColor = new Color(0.28f, 0.38f, 0.52f, 0.25f);
            esOutl.effectDistance = new Vector2(1f, -1f);

            GameObject runeObj = new GameObject("Rune", typeof(RectTransform), typeof(CanvasRenderer));
            runeObj.transform.SetParent(emptySlot.transform, false);
            RectTransform runeRt = runeObj.GetComponent<RectTransform>();
            runeRt.anchorMin = Vector2.zero;
            runeRt.anchorMax = Vector2.one;
            runeRt.sizeDelta = Vector2.zero;
            var runeTmp = runeObj.AddComponent<TextMeshProUGUI>();
            runeTmp.text = "<color=#334155><size=18>+</size></color>";
            runeTmp.alignment = TextAlignmentOptions.Center;
            runeTmp.raycastTarget = false;
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
        string activeHeroId = (curChar == CharacterManager.CHAR_TITAN_BERSERKER) ? "titan" : ((curChar == CharacterManager.CHAR_AZURE_BLADE) ? "azure" : ((curChar == CharacterManager.CHAR_GALE_RONIN || curChar == CharacterManager.CHAR_MAID) ? "ronin" : ((curChar == CharacterManager.CHAR_DUNE_ASSASSIN) ? "dune" : ((curChar == CharacterManager.CHAR_CELESTIAL_MAGE) ? "mage" : "vael"))));
        heroPreviewImage.transform.localScale = GetHeroVisualScale(activeHeroId);
        RectTransform mainRt = heroPreviewImage.rectTransform;
        if (mainRt != null)
        {
            mainRt.anchoredPosition = GetHeroVisualOffset(activeHeroId);
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
        OpenHeroSelectModal();
    }

    public void OpenHeroSelectModal()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenuOpen();
        EnsureHeroSelectCarouselModal();

        string currentSelected = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        int found = Array.IndexOf(carouselHeroIds, currentSelected);
        if (found >= 0) currentCarouselIdx = found;
        else currentCarouselIdx = 0;

        UpdateCarouselDisplay();
        StartCarouselHeroAnimation();

        if (heroSelectCarouselModal != null)
        {
            heroSelectCarouselModal.SetActive(true);
            heroSelectCarouselModal.transform.SetAsLastSibling();
        }
    }

    public void CloseHeroSelectModal()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (carouselHeroAnimCoroutine != null)
        {
            StopCoroutine(carouselHeroAnimCoroutine);
            carouselHeroAnimCoroutine = null;
        }
        if (heroSelectCarouselModal != null) heroSelectCarouselModal.SetActive(false);
    }

    public void SelectHeroByIndex(int idx)
    {
        if (idx < 0 || idx >= carouselHeroIds.Length) return;
        if (idx == currentCarouselIdx) return;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        currentCarouselIdx = idx;
        UpdateCarouselDisplay();
    }

    public void CarouselNextHero()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        currentCarouselIdx = (currentCarouselIdx + 1) % carouselHeroIds.Length;
        UpdateCarouselDisplay();
    }

    public void CarouselPrevHero()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        currentCarouselIdx = (currentCarouselIdx - 1 + carouselHeroIds.Length) % carouselHeroIds.Length;
        UpdateCarouselDisplay();
    }

    public void OnCarouselConfirmClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (carouselHeroAnimCoroutine != null)
        {
            StopCoroutine(carouselHeroAnimCoroutine);
            carouselHeroAnimCoroutine = null;
        }

        if (currentCarouselIdx < 0 || currentCarouselIdx >= carouselHeroIds.Length) currentCarouselIdx = 0;
        string heroId = carouselHeroIds[currentCarouselIdx];

        CharacterManager.EnsureInstance().SelectCharacter(heroId);

        string alias = GetHeroAlias(heroId);
        PlayerPrefs.SetString("SelectedHero", alias);
        PlayerPrefs.Save();
        AccountDatabase.SaveActiveSessionToDatabase();

        if (heroSelectCarouselModal != null) heroSelectCarouselModal.SetActive(false);
        CorruptedCycleManager.ResetToNormalCycle();
        PlayerHealth.ResetRunPreservedHealth();
        MatchTimerUI.ResetRunTimer();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("Map1_Thestart");
    }

    private string GetHeroAlias(string normHeroId)
    {
        if (normHeroId == CharacterManager.CHAR_TITAN_BERSERKER) return "titan";
        if (normHeroId == CharacterManager.CHAR_AZURE_BLADE) return "azure";
        if (normHeroId == CharacterManager.CHAR_GALE_RONIN) return "ronin";
        if (normHeroId == CharacterManager.CHAR_DUNE_ASSASSIN) return "dune";
        if (normHeroId == CharacterManager.CHAR_CELESTIAL_MAGE) return "mage";
        return "vael";
    }

    private void StartCarouselHeroAnimation()
    {
        if (carouselHeroAnimCoroutine != null) StopCoroutine(carouselHeroAnimCoroutine);
        carouselHeroAnimCoroutine = StartCoroutine(AnimateCarouselHeroRoutine());
    }

    private IEnumerator AnimateCarouselHeroRoutine()
    {
        while (true)
        {
            if (currentCarouselIdx >= 0 && currentCarouselIdx < carouselHeroIds.Length)
            {
                string heroId = carouselHeroIds[currentCarouselIdx];
                Sprite[] frames = GetHeroIdleFrames(heroId);

                if (frames == null || frames.Length == 0)
                {
                    EnsureHeroSprites();
                }
                else
                {
                    carouselAnimFrame = (carouselAnimFrame + 1) % frames.Length;
                    if (carouselHeroStageImage != null && frames[carouselAnimFrame] != null)
                    {
                        carouselHeroStageImage.sprite = frames[carouselAnimFrame];
                    }
                }
            }
            yield return new WaitForSeconds(0.14f);
        }
    }

    private Sprite[] GetHeroIdleFrames(string heroId)
    {
        if (heroId == CharacterManager.CHAR_TITAN_BERSERKER) return titanIdleFrames;
        if (heroId == CharacterManager.CHAR_AZURE_BLADE) return azureIdleFrames;
        if (heroId == CharacterManager.CHAR_GALE_RONIN) return (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames;
        if (heroId == CharacterManager.CHAR_DUNE_ASSASSIN) return duneIdleFrames;
        if (heroId == CharacterManager.CHAR_CELESTIAL_MAGE) return mageIdleFrames;
        return vaelIdleFrames;
    }

    private struct HeroCombatInfo
    {
        public string attackTitle;
        public string attackDesc;
        public string guardTitle;
        public string guardDesc;
        public string ultiTitle;
        public string ultiDesc;
        public Color elementColor;
    }

    private HeroCombatInfo GetHeroCombatInfo(string heroId, bool isVN)
    {
        HeroCombatInfo info = new HeroCombatInfo();

        if (heroId == CharacterManager.CHAR_TITAN_BERSERKER)
        {
            info.elementColor = new Color(1f, 0.38f, 0.16f, 0.40f);
            info.attackTitle = isVN ? "Trảm Kích Đại Chùy & Gồng Địa Chấn" : "Warhammer Sweep & Seismic Charge";
            info.attackDesc = isVN 
                ? "Nện đại chùy càn quét diện rộng. Nhấn Giữ gồng Địa Chấn: Miễn nhiễm toàn bộ sát thương và khống chế!"
                : "Heavy crushing blows dealing sweeping AOE damage. Hold attack for Earthshaker: immune to all damage & CC!";
            info.guardTitle = isVN ? "Kim Cương Bất Hoại & Nện Đất (Choáng 1.0s)" : "Unbreakable & Ground Slam (1.0s Stun)";
            info.guardDesc = isVN
                ? "Thả tay sau khi gồng để nện đất bộc phá làm Choáng kẻ địch 1 giây và hất tung quái vật xung quanh."
                : "Release after charging to smash the ground, stunning nearby enemies for 1s and tossing them aloft.";
            info.ultiTitle = isVN ? "Cuồng Nộ Chấn Địa (Dung Nham)" : "Titan Cataclysm (Molten Slam)";
            info.ultiDesc = isVN
                ? "Hút toàn bộ quái vật vào tâm chấn rồi nện búa dung nham hủy diệt hất tung và gây sát thương khổng lồ."
                : "Pulls all enemies into epicenter then detonates with cataclysmic molten lava hammer smash.";
        }
        else if (heroId == CharacterManager.CHAR_AZURE_BLADE)
        {
            info.elementColor = new Color(0.28f, 0.65f, 1f, 0.40f);
            info.attackTitle = isVN ? "Trọng Kiếm Khí & Sóng Xuyên Thấu" : "Heavy Blade & Piercing Waves";
            info.attackDesc = isVN
                ? "Mỗi nhát vung trọng kiếm phóng ra luồng kiếm khí lam sắc xuyên thấu hàng loạt quái vật từ xa."
                : "Heavy greatsword swings unleash piercing azure waves cutting down ranks of monsters from afar.";
            info.guardTitle = isVN ? "Kiên Định Thể Phách (-50% Sát Thương)" : "Iron Bulwark (-50% DMG)";
            info.guardDesc = isVN
                ? "Trụ vững giảm 50% sát thương nhận vào, không bị quái vật đẩy lùi hay ngắt nhịp tung chiêu."
                : "Steadfast stance reduces damage taken by 50% and grants knockback & stagger immunity.";
            info.ultiTitle = isVN ? "Bão Sóng Kiếm (Nan Quạt 5 Luồng)" : "Crescent Tempest (5-Wave Fan)";
            info.ultiDesc = isVN
                ? "Tích tụ cực hạn và phóng ra 5 đợt kiếm khí khổng lồ quét sạch toàn bộ góc chiến trường."
                : "Gathers maximum aura to fire 5 massive crescent waves in an expansive fan spread.";
        }
        else if (heroId == CharacterManager.CHAR_GALE_RONIN)
        {
            info.elementColor = new Color(0.32f, 0.95f, 0.72f, 0.40f);
            info.attackTitle = isVN ? "Nguyệt Kiếm & Tụ Lực Cư Hợp Trảm" : "Crescent Blades & Iaido Charge";
            info.attackDesc = isVN
                ? "Đòn chém bán nguyệt quét rộng 180 độ tích tầng Phong Ấn. Khi có khiên nộ, tụ lực Cư Hợp chém rách không gian x2.85 sát thương."
                : "Wide 180° slashes applying Wind Marks. With shield active, charge Iaido to rip space for x2.85 DMG.";
            info.guardTitle = isVN ? "Khiên Nội Năng & Ảo Ảnh Phản Thân" : "Inner Shield & Phantom Evasion";
            info.guardDesc = isVN
                ? "Kích hoạt khiên nội năng hấp thụ sát thương. Né tránh tạo tàn ảnh gây phân tâm và đánh lừa kẻ địch."
                : "Activates inner energy barrier. Evasive maneuvers leave phantom clones confusing enemies.";
            info.ultiTitle = isVN ? "Ám Ảnh Trảm" : "Shadow Strike";
            info.ultiDesc = isVN
                ? "Biến mất vào hư không, dịch chuyển chớp nhoáng trảm sát đa mục tiêu với tốc độ âm thanh."
                : "Vanishes into mist, executing blinding multi-target assassination slashes at supersonic speed.";
        }
        else if (heroId == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            info.elementColor = new Color(0.96f, 0.76f, 0.22f, 0.40f);
            info.attackTitle = isVN ? "Song Đoản Đao Tốc Biến & Bạo Kích" : "Dual Scimitars & Swift Flurry";
            info.attackDesc = isVN
                ? "Combo 3 đòn song đao lướt chém chớp nhoáng với tỷ lệ chí mạng cao nhất trong toàn bộ anh hùng."
                : "Lightning-fast 3-hit dual blade combo boasting the highest critical strike rate.";
            info.guardTitle = isVN ? "Thế Thủ Ảo Ảnh & Không Trảm (Choáng 1.2s)" : "Mirage Defend & Air Slash (1.2s Stun)";
            info.guardDesc = isVN
                ? "Giảm 70% sát thương, phản kích bằng đòn Không Trảm làm Choáng kẻ địch 1.2s và hồi sinh lực."
                : "Reduces damage by 70%, retaliating with Air Slash Counter that stuns for 1.2s and restores HP.";
            info.ultiTitle = isVN ? "Cuồng Bão Sa Ảnh (Chém X Chảy Máu)" : "Sandstorm X-Rend (Cross-X Bleed)";
            info.ultiDesc = isVN
                ? "Triệu hồi lốc cát vây khốn quái vật và tung nhát trảm chữ X xé toạc chiến trường gây Chảy Máu liên tục."
                : "Summons a sand vortex and executes a devastating Cross-X Slash inflicting severe bleeding.";
        }
        else if (heroId == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            info.elementColor = new Color(0.78f, 0.42f, 1f, 0.40f);
            info.attackTitle = isVN ? "Tinh Tú Ma Cầu & Sét Lan" : "Arcane Star Orbs & Chain Sparks";
            info.attackDesc = isVN
                ? "Phóng 3 luồng cầu ma pháp tự động truy tìm kẻ địch và bộc phá phóng sét lan sang quái lân cận."
                : "Fires 3 homing arcane orbs that hunt enemies and detonate into chaining arcane lightning.";
            info.guardTitle = isVN ? "Khiên Ma Trận Hấp Thụ (-80% Sát Thương)" : "Arcane Barrier (-80% DMG)";
            info.guardDesc = isVN
                ? "Nhấn giữ tạo khiên năng lượng hấp thụ 80% sát thương và tạo sóng đẩy lùi mọi kẻ địch áp sát."
                : "Hold to erect a protective barrier absorbing 80% damage and blasting away melee attackers.";
            info.ultiTitle = isVN ? "Thiên Thạch Ma Trận" : "Meteor Apocalypse";
            info.ultiDesc = isVN
                ? "Triệu hồi cơn mưa thiên thạch từ không gian rơi xuống hủy diệt toàn bộ kẻ địch trên màn hình."
                : "Calls down a catastrophic storm of cosmic meteors obliterating the entire battlefield.";
        }
        else // Vaelstrider
        {
            info.elementColor = new Color(1f, 0.82f, 0.35f, 0.40f);
            info.attackTitle = isVN ? "Trảm Kích Hoàng Kim & Tụ Lực" : "Golden Slashes & Charge";
            info.attackDesc = isVN
                ? "Combo 3 đòn kiếm thuật cận chiến sát thương liên hoàn. Nhấn giữ để vào Thế Thủ Kiếm Thuẫn."
                : "3-hit melee sword combo with fluid cadence. Hold attack to enter Divine Parry Stance.";
            info.guardTitle = isVN ? "Đỡ Đòn & Phản Kích Hoàng Kim (x3.0 Bạo Kích)" : "Divine Guard & Riposte (x3.0 Crit)";
            info.guardDesc = isVN
                ? "Đỡ đòn hoàn hảo triệt tiêu 100% sát thương, lập tức vung kiếm chém phản kích x3.0 sát thương bạo kích cực lớn."
                : "Perfect parry negates 100% damage, unleashing an immediate x3.0 Crit retaliatory slash.";
            info.ultiTitle = isVN ? "Bão Lốc Tàn Tích" : "Ruin Whirlwind";
            info.ultiDesc = isVN
                ? "Xoay kiếm 360 độ hút quái vật vào tâm bão và tung vụ nổ hoàng kim hất văng mọi kẻ địch."
                : "Spins 360° pulling enemies into the vortex before detonating with an expansive blast.";
        }

        return info;
    }

    public void EnsureHeroSelectCarouselModal()
    {
        if (heroSelectCarouselModal != null) return;

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas == null) return;

        Transform existing = rootCanvas.transform.Find("HeroSelectCarouselModal");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        // 1. Root Fullscreen Container
        heroSelectCarouselModal = new GameObject("HeroSelectCarouselModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        heroSelectCarouselModal.layer = 5;
        heroSelectCarouselModal.transform.SetParent(rootCanvas.transform, false);
        heroSelectCarouselModal.transform.SetAsLastSibling();

        RectTransform rootRt = heroSelectCarouselModal.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        Image overlayImg = heroSelectCarouselModal.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);

        var cg = heroSelectCarouselModal.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;

        // 2. Top Header Bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topBar.transform.SetParent(heroSelectCarouselModal.transform, false);
        RectTransform tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 1f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.anchoredPosition = Vector2.zero;
        tbRt.sizeDelta = new Vector2(0f, 85f);
        topBar.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.94f);

        // Gold line under top bar
        GameObject topDiv = new GameObject("TopBorderLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topDiv.transform.SetParent(topBar.transform, false);
        RectTransform tdRt = topDiv.GetComponent<RectTransform>();
        tdRt.anchorMin = new Vector2(0f, 0f);
        tdRt.anchorMax = new Vector2(1f, 0f);
        tdRt.pivot = new Vector2(0.5f, 0f);
        tdRt.anchoredPosition = Vector2.zero;
        tdRt.sizeDelta = new Vector2(0f, 2f);
        topDiv.GetComponent<Image>().color = new Color(0.85f, 0.72f, 0.35f, 0.45f);

        // Back Button (Top Left)
        GameObject backBtnObj = new GameObject("Btn_Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(topBar.transform, false);
        RectTransform bRt = backBtnObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0.5f);
        bRt.anchorMax = new Vector2(0f, 0.5f);
        bRt.pivot = new Vector2(0f, 0.5f);
        bRt.anchoredPosition = new Vector2(35f, 0f);
        bRt.sizeDelta = new Vector2(145f, 44f);

        Image bImg = backBtnObj.GetComponent<Image>();
        bImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        bImg.type = Image.Type.Sliced;
        var bOutline = backBtnObj.AddComponent<Outline>();
        bOutline.effectColor = new Color(0.75f, 0.65f, 0.35f, 0.7f);
        bOutline.effectDistance = new Vector2(1.5f, -1.5f);

        carouselCloseBtn = backBtnObj.GetComponent<Button>();
        carouselCloseBtn.onClick.AddListener(CloseHeroSelectModal);

        GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bTxtObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one; btRt.sizeDelta = Vector2.zero;
        var bTxt = bTxtObj.GetComponent<TextMeshProUGUI>();
        bTxt.text = LocalizationManager.T("← TRỞ VỀ", "← BACK");
        bTxt.fontSize = 15f;
        bTxt.fontStyle = FontStyles.Bold;
        bTxt.alignment = TextAlignmentOptions.Center;
        bTxt.color = new Color(0.96f, 0.88f, 0.65f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(bTxt, 0.3f, Color.black);
        bTxt.raycastTarget = false;

        // Title (Top Center)
        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(topBar.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 0.5f);
        tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = new Vector2(0f, 12f);
        tRt.sizeDelta = new Vector2(620f, 32f);

        var headerTxt = titleObj.GetComponent<TextMeshProUGUI>();
        headerTxt.text = LocalizationManager.T("CHỌN ANH HÙNG XUẤT TRẬN", "SELECT WARRIOR");
        headerTxt.fontSize = 22f;
        headerTxt.fontStyle = FontStyles.Bold;
        headerTxt.alignment = TextAlignmentOptions.Center;
        headerTxt.color = new Color(1f, 0.86f, 0.42f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(headerTxt, 0.35f, Color.black);

        // Filigree Divider
        GameObject fDiv = new GameObject("FiligreeDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fDiv.transform.SetParent(topBar.transform, false);
        RectTransform fdRt = fDiv.GetComponent<RectTransform>();
        fdRt.anchorMin = new Vector2(0.5f, 0.5f);
        fdRt.anchorMax = new Vector2(0.5f, 0.5f);
        fdRt.pivot = new Vector2(0.5f, 0.5f);
        fdRt.anchoredPosition = new Vector2(0f, -8f);
        fdRt.sizeDelta = new Vector2(440f, 6f);
        Image fdImg = fDiv.GetComponent<Image>();
        Sprite divSp = Resources.Load<Sprite>("UI/Soulslike/Gothic_Filigree_Divider");
        if (divSp != null) fdImg.sprite = divSp;
        fdImg.color = new Color(0.85f, 0.72f, 0.35f, 0.8f);
        fdImg.raycastTarget = false;

        // Counter Subtitle
        GameObject cntObj = new GameObject("CounterSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        cntObj.transform.SetParent(topBar.transform, false);
        RectTransform cntRt = cntObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0.5f, 0.5f);
        cntRt.anchorMax = new Vector2(0.5f, 0.5f);
        cntRt.pivot = new Vector2(0.5f, 0.5f);
        cntRt.anchoredPosition = new Vector2(0f, -24f);
        cntRt.sizeDelta = new Vector2(240f, 18f);
        carouselCounterText = cntObj.GetComponent<TextMeshProUGUI>();
        carouselCounterText.fontSize = 12f;
        carouselCounterText.fontStyle = FontStyles.Bold;
        carouselCounterText.alignment = TextAlignmentOptions.Center;
        carouselCounterText.color = new Color(0.72f, 0.78f, 0.88f, 0.85f);

        // Currency Box (Top Right)
        GameObject curObj = new GameObject("CurrencyBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        curObj.transform.SetParent(topBar.transform, false);
        RectTransform curRt = curObj.GetComponent<RectTransform>();
        curRt.anchorMin = new Vector2(1f, 0.5f);
        curRt.anchorMax = new Vector2(1f, 0.5f);
        curRt.pivot = new Vector2(1f, 0.5f);
        curRt.anchoredPosition = new Vector2(-35f, 0f);
        curRt.sizeDelta = new Vector2(200f, 40f);
        Image curImg = curObj.GetComponent<Image>();
        Sprite plaqueBg = Resources.Load<Sprite>("UI/Soulslike/Runic_Plaque_Bg");
        if (plaqueBg != null) { curImg.sprite = plaqueBg; curImg.type = Image.Type.Sliced; }
        else curImg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);

        GameObject curTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        curTxtObj.transform.SetParent(curObj.transform, false);
        RectTransform ctxtRt = curTxtObj.GetComponent<RectTransform>();
        ctxtRt.anchorMin = Vector2.zero; ctxtRt.anchorMax = Vector2.one; ctxtRt.sizeDelta = Vector2.zero;
        carouselTopCurrencyText = curTxtObj.GetComponent<TextMeshProUGUI>();
        carouselTopCurrencyText.fontSize = 13.5f;
        carouselTopCurrencyText.fontStyle = FontStyles.Bold;
        carouselTopCurrencyText.alignment = TextAlignmentOptions.Center;
        carouselTopCurrencyText.color = new Color(0.35f, 0.88f, 1f, 1f);

        // 3. Left Stage: Hero Showcase Area
        GameObject stageObj = new GameObject("HeroStage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        stageObj.transform.SetParent(heroSelectCarouselModal.transform, false);
        RectTransform stRt = stageObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(0.5f, 1f);
        stRt.offsetMin = new Vector2(30f, 155f);
        stRt.offsetMax = new Vector2(-15f, -95f);
        stageObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // transparent raycast receiver

        // Swipe handler
        var swipeHandler = stageObj.AddComponent<HeroCarouselSwipeHandler>();
        swipeHandler.onSwipeLeft = CarouselNextHero;
        swipeHandler.onSwipeRight = CarouselPrevHero;

        // Stage Halo Glow
        GameObject haloObj = new GameObject("StageHalo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        haloObj.transform.SetParent(stageObj.transform, false);
        RectTransform hRt = haloObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0.5f, 0.5f);
        hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.anchoredPosition = new Vector2(0f, -10f);
        hRt.sizeDelta = new Vector2(460f, 460f);
        carouselStageHaloGlow = haloObj.GetComponent<Image>();
        Sprite haloSp = Resources.Load<Sprite>("UI/Soulslike/Action_Button_Obsidian_Core");
        if (haloSp != null) carouselStageHaloGlow.sprite = haloSp;
        carouselStageHaloGlow.color = new Color(1f, 0.82f, 0.35f, 0.28f);
        carouselStageHaloGlow.raycastTarget = false;

        // Ancient Altar Pedestal (Golden Circular Runic Ring on floor)
        GameObject altarObj = new GameObject("AltarBase", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        altarObj.transform.SetParent(stageObj.transform, false);
        RectTransform alRt = altarObj.GetComponent<RectTransform>();
        alRt.anchorMin = new Vector2(0.5f, 0.5f);
        alRt.anchorMax = new Vector2(0.5f, 0.5f);
        alRt.pivot = new Vector2(0.5f, 0.5f);
        alRt.anchoredPosition = new Vector2(0f, -165f);
        alRt.sizeDelta = new Vector2(440f, 130f);
        Image alImg = altarObj.GetComponent<Image>();
        Sprite ringSp = Resources.Load<Sprite>("UI/Soulslike/Action_Button_Runic_Ring");
        if (ringSp != null) alImg.sprite = ringSp;
        alImg.color = new Color(0.85f, 0.72f, 0.35f, 0.65f);
        alImg.raycastTarget = false;

        // Shadow under hero (Concentric with golden circle center)
        GameObject shadowObj = new GameObject("HeroShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shadowObj.transform.SetParent(stageObj.transform, false);
        RectTransform shRt = shadowObj.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0.5f, 0.5f);
        shRt.anchorMax = new Vector2(0.5f, 0.5f);
        shRt.pivot = new Vector2(0.5f, 0.5f);
        shRt.anchoredPosition = new Vector2(0f, -163f);
        shRt.sizeDelta = new Vector2(260f, 48f);
        Image shImg = shadowObj.GetComponent<Image>();
        if (haloSp != null) shImg.sprite = haloSp;
        shImg.color = new Color(0f, 0f, 0f, 0.75f);
        shImg.raycastTarget = false;

        // Animated Character Image (Stands directly in the center of the golden pedestal)
        GameObject heroImgObj = new GameObject("HeroPreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        heroImgObj.transform.SetParent(stageObj.transform, false);
        RectTransform hiRt = heroImgObj.GetComponent<RectTransform>();
        hiRt.anchorMin = new Vector2(0.5f, 0.5f);
        hiRt.anchorMax = new Vector2(0.5f, 0.5f);
        hiRt.pivot = new Vector2(0.5f, 0.5f);
        hiRt.anchoredPosition = new Vector2(12f, -26f);
        hiRt.sizeDelta = new Vector2(320f, 320f);
        carouselHeroStageImage = heroImgObj.GetComponent<Image>();
        carouselHeroStageImage.preserveAspect = true;
        carouselHeroStageImage.raycastTarget = false;
        carouselAvatarImg = carouselHeroStageImage;

        // Left Arrow Button (<)
        GameObject prevObj = new GameObject("Btn_CarouselPrev", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        prevObj.transform.SetParent(stageObj.transform, false);
        RectTransform prevRt = prevObj.GetComponent<RectTransform>();
        prevRt.anchorMin = new Vector2(0f, 0.5f);
        prevRt.anchorMax = new Vector2(0f, 0.5f);
        prevRt.pivot = new Vector2(0f, 0.5f);
        prevRt.anchoredPosition = new Vector2(10f, 0f);
        prevRt.sizeDelta = new Vector2(54f, 78f);
        Image prevImg = prevObj.GetComponent<Image>();
        prevImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        prevImg.type = Image.Type.Sliced;
        var prevOutline = prevObj.AddComponent<Outline>();
        prevOutline.effectColor = new Color(0.75f, 0.65f, 0.35f, 0.7f);
        prevOutline.effectDistance = new Vector2(1.5f, -1.5f);
        carouselPrevBtn = prevObj.GetComponent<Button>();
        carouselPrevBtn.onClick.AddListener(CarouselPrevHero);

        GameObject prevTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        prevTxtObj.transform.SetParent(prevObj.transform, false);
        RectTransform ptRt = prevTxtObj.GetComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one; ptRt.sizeDelta = Vector2.zero;
        var prevTxt = prevTxtObj.GetComponent<TextMeshProUGUI>();
        prevTxt.text = "<";
        prevTxt.fontSize = 34f;
        prevTxt.fontStyle = FontStyles.Bold;
        prevTxt.alignment = TextAlignmentOptions.Center;
        prevTxt.color = new Color(1f, 0.88f, 0.45f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(prevTxt, 0.35f, Color.black);
        prevTxt.raycastTarget = false;

        // Right Arrow Button (>)
        GameObject nextObj = new GameObject("Btn_CarouselNext", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        nextObj.transform.SetParent(stageObj.transform, false);
        RectTransform nextRt = nextObj.GetComponent<RectTransform>();
        nextRt.anchorMin = new Vector2(1f, 0.5f);
        nextRt.anchorMax = new Vector2(1f, 0.5f);
        nextRt.pivot = new Vector2(1f, 0.5f);
        nextRt.anchoredPosition = new Vector2(-10f, 0f);
        nextRt.sizeDelta = new Vector2(54f, 78f);
        Image nextImg = nextObj.GetComponent<Image>();
        nextImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        nextImg.type = Image.Type.Sliced;
        var nextOutline = nextObj.AddComponent<Outline>();
        nextOutline.effectColor = new Color(0.75f, 0.65f, 0.35f, 0.7f);
        nextOutline.effectDistance = new Vector2(1.5f, -1.5f);
        carouselNextBtn = nextObj.GetComponent<Button>();
        carouselNextBtn.onClick.AddListener(CarouselNextHero);

        GameObject nextTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        nextTxtObj.transform.SetParent(nextObj.transform, false);
        RectTransform ntRt = nextTxtObj.GetComponent<RectTransform>();
        ntRt.anchorMin = Vector2.zero; ntRt.anchorMax = Vector2.one; ntRt.sizeDelta = Vector2.zero;
        var nextTxt = nextTxtObj.GetComponent<TextMeshProUGUI>();
        nextTxt.text = ">";
        nextTxt.fontSize = 34f;
        nextTxt.fontStyle = FontStyles.Bold;
        nextTxt.alignment = TextAlignmentOptions.Center;
        nextTxt.color = new Color(1f, 0.88f, 0.45f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(nextTxt, 0.35f, Color.black);
        nextTxt.raycastTarget = false;

        // 4. Right: Combat Dossier Panel
        GameObject dosObj = new GameObject("CombatDossier", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dosObj.transform.SetParent(heroSelectCarouselModal.transform, false);
        RectTransform dosRt = dosObj.GetComponent<RectTransform>();
        dosRt.anchorMin = new Vector2(0.5f, 0f);
        dosRt.anchorMax = new Vector2(1f, 1f);
        dosRt.offsetMin = new Vector2(15f, 155f);
        dosRt.offsetMax = new Vector2(-30f, -95f);
        Image dosImg = dosObj.GetComponent<Image>();
        Sprite panelBg = Resources.Load<Sprite>("UI/Soulslike/Dark_Fantasy_Panel_Bg");
        if (panelBg != null) { dosImg.sprite = panelBg; dosImg.type = Image.Type.Sliced; }
        else dosImg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        var dosOutline = dosObj.AddComponent<Outline>();
        dosOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.55f);
        dosOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header: Hero Name & Role
        GameObject nameObj = new GameObject("HeroName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(dosObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 1f);
        nameRt.anchorMax = new Vector2(0.5f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -16f);
        nameRt.sizeDelta = new Vector2(680f, 38f);
        carouselHeroNameText = nameObj.GetComponent<TextMeshProUGUI>();
        carouselHeroNameText.fontSize = 28f;
        carouselHeroNameText.fontStyle = FontStyles.Bold;
        carouselHeroNameText.alignment = TextAlignmentOptions.Center;
        carouselHeroNameText.color = new Color(1f, 0.88f, 0.45f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(carouselHeroNameText, 0.35f, Color.black);

        GameObject roleObj = new GameObject("HeroRole", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        roleObj.transform.SetParent(dosObj.transform, false);
        RectTransform roleRt = roleObj.GetComponent<RectTransform>();
        roleRt.anchorMin = new Vector2(0.5f, 1f);
        roleRt.anchorMax = new Vector2(0.5f, 1f);
        roleRt.pivot = new Vector2(0.5f, 1f);
        roleRt.anchoredPosition = new Vector2(0f, -54f);
        roleRt.sizeDelta = new Vector2(680f, 24f);
        carouselHeroRoleText = roleObj.GetComponent<TextMeshProUGUI>();
        carouselHeroRoleText.fontSize = 14.5f;
        carouselHeroRoleText.fontStyle = FontStyles.Italic;
        carouselHeroRoleText.alignment = TextAlignmentOptions.Center;
        carouselHeroRoleText.color = new Color(0.68f, 0.84f, 0.98f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(carouselHeroRoleText, 0.25f, Color.black);

        // Subtle Header Divider
        GameObject dLine = new GameObject("HeaderDiv", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dLine.transform.SetParent(dosObj.transform, false);
        RectTransform dlRt = dLine.GetComponent<RectTransform>();
        dlRt.anchorMin = new Vector2(0.5f, 1f);
        dlRt.anchorMax = new Vector2(0.5f, 1f);
        dlRt.pivot = new Vector2(0.5f, 1f);
        dlRt.anchoredPosition = new Vector2(0f, -80f);
        dlRt.sizeDelta = new Vector2(640f, 2f);
        dLine.GetComponent<Image>().color = new Color(0.85f, 0.72f, 0.35f, 0.35f);

        // Stats Plaque Box with 4 Gauges (HP, ATK/AP, GIÁP/DEF, SPD)
        GameObject statsBox = new GameObject("StatsBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statsBox.transform.SetParent(dosObj.transform, false);
        RectTransform sbRt = statsBox.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.5f, 1f);
        sbRt.anchorMax = new Vector2(0.5f, 1f);
        sbRt.pivot = new Vector2(0.5f, 1f);
        sbRt.anchoredPosition = new Vector2(0f, -88f);
        sbRt.sizeDelta = new Vector2(660f, 134f);
        Image sbImg = statsBox.GetComponent<Image>();
        if (plaqueBg != null) { sbImg.sprite = plaqueBg; sbImg.type = Image.Type.Sliced; }
        else sbImg.color = new Color(0.04f, 0.06f, 0.09f, 0.85f);

        Sprite troughSp = Resources.Load<Sprite>("UI/Soulslike/Bar_Obsidian_Trough");
        Sprite rubySp = Resources.Load<Sprite>("UI/Soulslike/Bar_Ruby_Fill");
        Sprite amberSp = Resources.Load<Sprite>("UI/Soulslike/Bar_Amber_Fill");
        Sprite sapphireSp = Resources.Load<Sprite>("UI/Soulslike/Bar_Sapphire_Fill");

        CreateStatRow(statsBox.transform, "Row_HP", new Vector2(0f, 44f), "SINH LỰC (HP)", new Color(1f, 0.4f, 0.4f, 1f), troughSp, rubySp, out carouselHpBarFill, out carouselHpValText, out _);
        CreateStatRow(statsBox.transform, "Row_ATK", new Vector2(0f, 15f), "TẤN CÔNG (ATK)", new Color(1f, 0.65f, 0.25f, 1f), troughSp, amberSp, out carouselAtkBarFill, out carouselAtkValText, out carouselAtkLabelText);
        CreateStatRow(statsBox.transform, "Row_DEF", new Vector2(0f, -15f), "GIÁP (ARMOR)", new Color(0.38f, 0.85f, 1f, 1f), troughSp, sapphireSp, out carouselDefBarFill, out carouselDefValText, out carouselDefLabelText);
        CreateStatRow(statsBox.transform, "Row_SPD", new Vector2(0f, -44f), "TỐC ĐỘ (SPD)", new Color(0.35f, 0.92f, 0.65f, 1f), troughSp, sapphireSp, out carouselSpdBarFill, out carouselSpdValText, out _);

        // 3 Combat Arsenal Breakdown Cards
        CreateSkillCard(dosObj.transform, "SkillCard_Normal", new Vector2(0f, 86f), new Vector2(660f, 82f), out carouselSkillNormalTitle, out carouselSkillNormalDesc);
        CreateSkillCard(dosObj.transform, "SkillCard_Guard", new Vector2(0f, -6f), new Vector2(660f, 82f), out carouselSkillGuardTitle, out carouselSkillGuardDesc);
        CreateSkillCard(dosObj.transform, "SkillCard_Ulti", new Vector2(0f, -98f), new Vector2(660f, 82f), out carouselSkillUltiTitle, out carouselSkillUltiDesc);

        // 5. Bottom Strip: Horizontal Deck & Enter Battle Button
        GameObject btmBar = new GameObject("BottomBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        btmBar.transform.SetParent(heroSelectCarouselModal.transform, false);
        RectTransform bbRt = btmBar.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0f, 0f);
        bbRt.anchorMax = new Vector2(1f, 0f);
        bbRt.pivot = new Vector2(0.5f, 0f);
        bbRt.anchoredPosition = Vector2.zero;
        bbRt.sizeDelta = new Vector2(0f, 145f);
        btmBar.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.96f);

        // Top line for bottom bar
        GameObject btmDiv = new GameObject("BtmBorderLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        btmDiv.transform.SetParent(btmBar.transform, false);
        RectTransform btdRt = btmDiv.GetComponent<RectTransform>();
        btdRt.anchorMin = new Vector2(0f, 1f);
        btdRt.anchorMax = new Vector2(1f, 1f);
        btdRt.pivot = new Vector2(0.5f, 1f);
        btdRt.anchoredPosition = Vector2.zero;
        btdRt.sizeDelta = new Vector2(0f, 2f);
        btmDiv.GetComponent<Image>().color = new Color(0.85f, 0.72f, 0.35f, 0.45f);

        // Build Horizontal Roster Deck (6 Hero Cards)
        BuildBottomRosterDeck(btmBar.transform);

        // Enter Battle Button (XUẤT TRẬN)
        GameObject confirmBtnObj = new GameObject("Btn_ConfirmPlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        confirmBtnObj.transform.SetParent(btmBar.transform, false);
        RectTransform cfbRt = confirmBtnObj.GetComponent<RectTransform>();
        cfbRt.anchorMin = new Vector2(1f, 0.5f);
        cfbRt.anchorMax = new Vector2(1f, 0.5f);
        cfbRt.pivot = new Vector2(1f, 0.5f);
        cfbRt.anchoredPosition = new Vector2(-45f, 0f);
        cfbRt.sizeDelta = new Vector2(275f, 68f);

        Image cfbImg = confirmBtnObj.GetComponent<Image>();
        cfbImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        cfbImg.type = Image.Type.Sliced;
        var cfbOutline = confirmBtnObj.AddComponent<Outline>();
        cfbOutline.effectColor = new Color(1f, 0.85f, 0.3f, 0.95f);
        cfbOutline.effectDistance = new Vector2(2.5f, -2.5f);

        carouselConfirmBtn = confirmBtnObj.GetComponent<Button>();
        carouselConfirmBtn.onClick.AddListener(OnCarouselConfirmClicked);

        GameObject cfbTxtObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        cfbTxtObj.transform.SetParent(confirmBtnObj.transform, false);
        RectTransform cfbtRt = cfbTxtObj.GetComponent<RectTransform>();
        cfbtRt.anchorMin = new Vector2(0f, 0.5f);
        cfbtRt.anchorMax = new Vector2(1f, 0.5f);
        cfbtRt.pivot = new Vector2(0.5f, 0.5f);
        cfbtRt.anchoredPosition = new Vector2(0f, 8f);
        cfbtRt.sizeDelta = new Vector2(0f, 30f);

        carouselConfirmBtnText = cfbTxtObj.GetComponent<TextMeshProUGUI>();
        carouselConfirmBtnText.text = LocalizationManager.T("XUẤT TRẬN", "ENTER BATTLE");
        carouselConfirmBtnText.fontSize = 20f;
        carouselConfirmBtnText.fontStyle = FontStyles.Bold;
        carouselConfirmBtnText.alignment = TextAlignmentOptions.Center;
        carouselConfirmBtnText.color = new Color(1f, 0.92f, 0.65f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(carouselConfirmBtnText, 0.35f, Color.black);
        carouselConfirmBtnText.raycastTarget = false;

        GameObject cfbSubObj = new GameObject("SubText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        cfbSubObj.transform.SetParent(confirmBtnObj.transform, false);
        RectTransform cfbSubRt = cfbSubObj.GetComponent<RectTransform>();
        cfbSubRt.anchorMin = new Vector2(0f, 0.5f);
        cfbSubRt.anchorMax = new Vector2(1f, 0.5f);
        cfbSubRt.pivot = new Vector2(0.5f, 0.5f);
        cfbSubRt.anchoredPosition = new Vector2(0f, -14f);
        cfbSubRt.sizeDelta = new Vector2(0f, 18f);

        var subTxt = cfbSubObj.GetComponent<TextMeshProUGUI>();
        subTxt.text = LocalizationManager.T("TIẾN VÀO HẦM NGỤC", "ENTER RUINS");
        subTxt.fontSize = 11.5f;
        subTxt.alignment = TextAlignmentOptions.Center;
        subTxt.color = new Color(0.82f, 0.85f, 0.90f, 0.85f);
        subTxt.raycastTarget = false;

        heroSelectCarouselModal.SetActive(false);
    }

    private void CreateStatRow(Transform parent, string name, Vector2 pos, string labelText, Color labelColor, Sprite troughSp, Sprite fillSp, out Image fillImg, out TextMeshProUGUI valTxt, out TextMeshProUGUI lblTxt)
    {
        GameObject rowObj = new GameObject(name, typeof(RectTransform));
        rowObj.transform.SetParent(parent, false);
        RectTransform rt = rowObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(620f, 22f);

        // Label
        GameObject lObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lObj.transform.SetParent(rowObj.transform, false);
        RectTransform lrt = lObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0.5f);
        lrt.anchorMax = new Vector2(0f, 0.5f);
        lrt.pivot = new Vector2(0f, 0.5f);
        lrt.anchoredPosition = new Vector2(6f, 0f);
        lrt.sizeDelta = new Vector2(175f, 22f);
        var lTxt = lObj.GetComponent<TextMeshProUGUI>();
        lTxt.text = labelText;
        lTxt.fontSize = 12f;
        lTxt.fontStyle = FontStyles.Bold;
        lTxt.color = labelColor;
        SoulslikeHUDStyler.SetSafeOutline(lTxt, 0.25f, Color.black);
        lblTxt = lTxt;

        // Bar Trough
        GameObject trObj = new GameObject("Trough", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trObj.transform.SetParent(rowObj.transform, false);
        RectTransform trt = trObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.5f);
        trt.anchorMax = new Vector2(0f, 0.5f);
        trt.pivot = new Vector2(0f, 0.5f);
        trt.anchoredPosition = new Vector2(185f, 0f);
        trt.sizeDelta = new Vector2(310f, 15f);
        Image trImg = trObj.GetComponent<Image>();
        if (troughSp != null) { trImg.sprite = troughSp; trImg.type = Image.Type.Sliced; }
        else trImg.color = new Color(0.04f, 0.05f, 0.08f, 0.9f);

        // Bar Fill
        GameObject fObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fObj.transform.SetParent(trObj.transform, false);
        RectTransform frt = fObj.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.sizeDelta = Vector2.zero;
        fillImg = fObj.GetComponent<Image>();
        if (fillSp != null) fillImg.sprite = fillSp;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 0.5f;

        // Value Text
        GameObject vObj = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        vObj.transform.SetParent(rowObj.transform, false);
        RectTransform vrt = vObj.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0.5f);
        vrt.anchorMax = new Vector2(0f, 0.5f);
        vrt.pivot = new Vector2(0f, 0.5f);
        vrt.anchoredPosition = new Vector2(510f, 0f);
        vrt.sizeDelta = new Vector2(105f, 22f);
        valTxt = vObj.GetComponent<TextMeshProUGUI>();
        valTxt.fontSize = 12.5f;
        valTxt.fontStyle = FontStyles.Bold;
        valTxt.color = Color.white;
        valTxt.alignment = TextAlignmentOptions.MidlineLeft;
        SoulslikeHUDStyler.SetSafeOutline(valTxt, 0.25f, Color.black);
    }

    private void CreateStatRow(Transform parent, string name, Vector2 pos, string labelText, Color labelColor, Sprite troughSp, Sprite fillSp, out Image fillImg, out TextMeshProUGUI valTxt)
    {
        CreateStatRow(parent, name, pos, labelText, labelColor, troughSp, fillSp, out fillImg, out valTxt, out _);
    }

    private void CreateSkillCard(Transform parent, string name, Vector2 pos, Vector2 size, out TextMeshProUGUI titleTxt, out TextMeshProUGUI descTxt)
    {
        GameObject cardObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.transform.SetParent(parent, false);
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image bg = cardObj.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.65f, 0.55f, 0.32f, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);

        // Title Text
        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        tObj.transform.SetParent(cardObj.transform, false);
        RectTransform trt = tObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.anchoredPosition = new Vector2(14f, -7f);
        trt.sizeDelta = new Vector2(-28f, 22f);

        titleTxt = tObj.GetComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 13.5f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = new Color(1f, 0.88f, 0.45f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(titleTxt, 0.25f, Color.black);

        // Description Text
        GameObject dObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        dObj.transform.SetParent(cardObj.transform, false);
        RectTransform drt = dObj.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0f, 0f);
        drt.anchorMax = new Vector2(1f, 1f);
        drt.pivot = new Vector2(0.5f, 0.5f);
        drt.offsetMin = new Vector2(14f, 6f);
        drt.offsetMax = new Vector2(-14f, -28f);

        descTxt = dObj.GetComponent<TextMeshProUGUI>();
        descTxt.fontSize = 11.5f;
        descTxt.color = new Color(0.85f, 0.88f, 0.92f, 0.95f);
        descTxt.enableWordWrapping = true;
        descTxt.lineSpacing = -4f;
    }

    private void BuildBottomRosterDeck(Transform parent)
    {
        carouselRosterCardObjs.Clear();
        carouselRosterCardOutlines.Clear();
        carouselRosterCardBgs.Clear();

        GameObject deckObj = new GameObject("HeroRosterDeck", typeof(RectTransform));
        deckObj.transform.SetParent(parent, false);
        RectTransform dRt = deckObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0.5f, 0.5f);
        dRt.anchorMax = new Vector2(0.5f, 0.5f);
        dRt.pivot = new Vector2(0.5f, 0.5f);
        dRt.anchoredPosition = new Vector2(-160f, 0f);
        dRt.sizeDelta = new Vector2(720f, 120f);

        string[] shortNames = new string[] { "Vael", "Ronin", "Azure", "Titan", "Dune", "Mage" };
        Sprite slotSp = Resources.Load<Sprite>("UI/Soulslike/Potion_Slot_Runic_Frame");

        for (int i = 0; i < carouselHeroIds.Length; i++)
        {
            int idx = i;
            string heroId = carouselHeroIds[i];

            GameObject card = new GameObject($"Card_{heroId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            card.transform.SetParent(deckObj.transform, false);
            RectTransform crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(-325f + i * 130f, 0f);
            crt.sizeDelta = new Vector2(114f, 110f);

            Image cImg = card.GetComponent<Image>();
            if (slotSp != null) { cImg.sprite = slotSp; cImg.type = Image.Type.Sliced; }
            else cImg.color = new Color(0.08f, 0.10f, 0.14f, 0.9f);

            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.30f, 0.40f, 0.7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectHeroByIndex(idx));

            // Face Avatar
            GameObject face = new GameObject("Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            face.transform.SetParent(card.transform, false);
            RectTransform frt = face.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.5f, 0.5f);
            frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.anchoredPosition = new Vector2(0f, 14f);
            frt.sizeDelta = new Vector2(54f, 54f);

            Image fImg = face.GetComponent<Image>();
            fImg.sprite = GetHeroAvatarSprite(heroId);
            fImg.preserveAspect = true;
            fImg.raycastTarget = false;

            // Short Name
            GameObject nObj = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nObj.transform.SetParent(card.transform, false);
            RectTransform nrt = nObj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0.5f);
            nrt.anchorMax = new Vector2(0.5f, 0.5f);
            nrt.pivot = new Vector2(0.5f, 0.5f);
            nrt.anchoredPosition = new Vector2(0f, -28f);
            nrt.sizeDelta = new Vector2(100f, 22f);

            var nTxt = nObj.GetComponent<TextMeshProUGUI>();
            nTxt.text = i < shortNames.Length ? shortNames[i] : heroId;
            nTxt.fontSize = 12.5f;
            nTxt.fontStyle = FontStyles.Bold;
            nTxt.alignment = TextAlignmentOptions.Center;
            nTxt.color = new Color(0.9f, 0.9f, 0.92f, 1f);
            SoulslikeHUDStyler.SetSafeOutline(nTxt, 0.25f, Color.black);
            nTxt.raycastTarget = false;

            carouselRosterCardObjs.Add(card);
            carouselRosterCardOutlines.Add(outline);
            carouselRosterCardBgs.Add(cImg);
        }
    }

    private void UpdateCarouselDisplay()
    {
        if (currentCarouselIdx < 0 || currentCarouselIdx >= carouselHeroIds.Length) currentCarouselIdx = 0;
        string heroId = carouselHeroIds[currentCarouselIdx];

        var charMgr = CharacterManager.EnsureInstance();
        CharacterData cData = charMgr != null ? charMgr.GetCharacter(heroId) : null;
        bool isVN = LocalizationManager.IsVietnamese;

        // 1. Counter Subtitle
        if (carouselCounterText != null)
        {
            carouselCounterText.text = $"<  {currentCarouselIdx + 1} / {carouselHeroIds.Length}  >";
        }

        // 2. Top Currency Shards
        if (carouselTopCurrencyText != null)
        {
            string shLabel = LocalizationManager.Get("LBL_SHARDS");
            carouselTopCurrencyText.text = $"{shLabel}: {FormatCompactNumber(AccountDatabase.GetAethelShards())}";
        }

        // 3. Name & Role
        string heroName = cData != null ? (isVN ? cData.nameVN : cData.nameEN) : heroId;
        string heroRole = cData != null ? (isVN ? cData.roleVN : cData.roleEN) : "";

        if (carouselHeroNameText != null) carouselHeroNameText.text = heroName;
        if (carouselHeroRoleText != null) carouselHeroRoleText.text = heroRole;

        // 4. Stat Gauges
        if (cData != null)
        {
            bool isMage = (heroId == CharacterManager.CHAR_CELESTIAL_MAGE || heroId.ToLower().Contains("mage") || heroId.ToLower().Contains("player6"));

            // 4a. Sinh Lực (HP)
            if (carouselHpValText != null) carouselHpValText.text = $"{cData.baseHealth} / 250";
            if (carouselHpBarFill != null) carouselHpBarFill.fillAmount = Mathf.Clamp01(cData.baseHealth / 250f);

            // 4b. Tấn Công (ATK) vs Sức Mạnh Phép (AP) - Pháp sư dùng Phép AP thay vì ATK
            if (carouselAtkLabelText != null)
            {
                if (isMage)
                {
                    carouselAtkLabelText.text = isVN ? "SỨC MẠNH PHÉP (AP)" : "MAGIC POWER (AP)";
                    carouselAtkLabelText.color = new Color(0.88f, 0.52f, 1f, 1f); // Tím Arcane
                }
                else
                {
                    carouselAtkLabelText.text = isVN ? "TẤN CÔNG (ATK)" : "ATTACK (ATK)";
                    carouselAtkLabelText.color = new Color(1f, 0.65f, 0.25f, 1f); // Cam Hổ Phách
                }
            }

            if (carouselAtkBarFill != null)
            {
                if (isMage)
                {
                    Sprite amethystSp = Resources.Load<Sprite>("UI/Soulslike/Bar_Amethyst_Fill");
                    if (amethystSp != null) carouselAtkBarFill.sprite = amethystSp;
                    carouselAtkBarFill.color = Color.white;
                }
                else
                {
                    Sprite amberSp = Resources.Load<Sprite>("UI/Soulslike/Bar_Amber_Fill");
                    if (amberSp != null) carouselAtkBarFill.sprite = amberSp;
                    carouselAtkBarFill.color = Color.white;
                }
                carouselAtkBarFill.fillAmount = Mathf.Clamp01(cData.baseAttack / 40f);
            }

            if (carouselAtkValText != null) carouselAtkValText.text = $"{cData.baseAttack} / 40";

            // 4c. Giáp Trụ (ARMOR / DEF)
            EquipmentSocketManager.GetTotalHeroSocketBonus(out _, out _, out _, out _, out _, out int sDef, out _, out _, out _, out _, out _, out _, out _, out _, out _, heroId);
            HeroCostumeManager.GetEquippedCostumeBonus(heroId, out _, out _, out float costumeDef, out _);
            ArmorShopUI.GetEquippedArmorBonus(out _, out float armorDef, out _);
            if (armorDef > 0f && armorDef <= 1f) armorDef *= 100f;
            int totalArmor = Mathf.RoundToInt(cData.baseDefense + sDef + costumeDef + armorDef);
            totalArmor = Mathf.Min(65, totalArmor);

            if (carouselDefLabelText != null)
            {
                carouselDefLabelText.text = isVN ? "GIÁP TRỤ (DEF)" : "ARMOR (DEF)";
                carouselDefLabelText.color = new Color(0.38f, 0.85f, 1f, 1f);
            }
            if (carouselDefValText != null) carouselDefValText.text = $"{totalArmor}% / 65%";
            if (carouselDefBarFill != null)
            {
                carouselDefBarFill.color = new Color(0.40f, 0.88f, 1f, 1f); // Xanh Lam Hộ Thuẫn
                carouselDefBarFill.fillAmount = Mathf.Clamp01(totalArmor / 65f);
            }

            // 4d. Tốc Độ (SPD)
            if (carouselSpdValText != null) carouselSpdValText.text = $"{cData.baseSpeed:F1} / 6.0";
            if (carouselSpdBarFill != null)
            {
                carouselSpdBarFill.color = new Color(0.35f, 0.92f, 0.65f, 1f); // Xanh Ngọc Phong Lôi
                carouselSpdBarFill.fillAmount = Mathf.Clamp01(cData.baseSpeed / 6.0f);
            }

            // Backward compatibility
            if (carouselHeroStatsText != null)
            {
                string hpLabel = isVN ? "MÁU" : "HP";
                string atkLabel = isMage ? (isVN ? "PHÉP" : "AP") : (isVN ? "CÔNG" : "ATK");
                string defLabel = isVN ? "GIÁP" : "DEF";
                string spdLabel = isVN ? "TỐC ĐỘ" : "SPD";
                carouselHeroStatsText.text = $"<color=#FF5555><b>[{hpLabel}]</b></color> {cData.baseHealth}    <color={(isMage ? "#C084FC" : "#FFB040")}><b>[{atkLabel}]</b></color> {cData.baseAttack}    <color=#38BDF8><b>[{defLabel}]</b></color> {totalArmor}%    <color=#50E0FF><b>[{spdLabel}]</b></color> {cData.baseSpeed:F1}";
            }
        }

        // 5. Combat Breakdown & Element Halo
        HeroCombatInfo combatInfo = GetHeroCombatInfo(heroId, isVN);

        if (carouselStageHaloGlow != null)
        {
            carouselStageHaloGlow.color = combatInfo.elementColor;
        }

        if (carouselSkillNormalTitle != null) carouselSkillNormalTitle.text = combatInfo.attackTitle;
        if (carouselSkillNormalDesc != null) carouselSkillNormalDesc.text = combatInfo.attackDesc;

        if (carouselSkillGuardTitle != null) carouselSkillGuardTitle.text = combatInfo.guardTitle;
        if (carouselSkillGuardDesc != null) carouselSkillGuardDesc.text = combatInfo.guardDesc;

        if (carouselSkillUltiTitle != null) carouselSkillUltiTitle.text = combatInfo.ultiTitle;
        if (carouselSkillUltiDesc != null) carouselSkillUltiDesc.text = combatInfo.ultiDesc;

        // Backward compatibility
        if (carouselHeroSkillText != null && cData != null)
        {
            string skillName = isVN ? cData.skillNameVN : cData.skillNameEN;
            string skillDesc = isVN ? cData.skillDescVN : cData.skillDescEN;
            carouselHeroSkillText.text = $"<color=#FFD700><b>{skillName}</b></color>\n<size=12><color=#CCCCCC>{skillDesc}</color></size>";
        }

        // 6. Action button text
        if (carouselConfirmBtnText != null)
        {
            carouselConfirmBtnText.text = LocalizationManager.T("XUẤT TRẬN", "ENTER BATTLE");
        }

        // 7. Highlight Selected Roster Card in Bottom Deck
        for (int i = 0; i < carouselRosterCardObjs.Count; i++)
        {
            if (carouselRosterCardObjs[i] == null) continue;
            bool isSelected = (i == currentCarouselIdx);

            if (i < carouselRosterCardOutlines.Count && carouselRosterCardOutlines[i] != null)
            {
                carouselRosterCardOutlines[i].effectColor = isSelected
                    ? new Color(1f, 0.85f, 0.3f, 1f)
                    : new Color(0.25f, 0.30f, 0.40f, 0.6f);
                carouselRosterCardOutlines[i].effectDistance = isSelected
                    ? new Vector2(2.5f, -2.5f)
                    : new Vector2(1.2f, -1.2f);
            }

            if (i < carouselRosterCardBgs.Count && carouselRosterCardBgs[i] != null)
            {
                carouselRosterCardBgs[i].color = isSelected
                    ? new Color(0.18f, 0.16f, 0.12f, 1f)
                    : new Color(0.08f, 0.10f, 0.14f, 0.9f);
            }

            carouselRosterCardObjs[i].transform.localScale = isSelected
                ? new Vector3(1.06f, 1.06f, 1f)
                : Vector3.one;
        }

        // 8. Visual Scaling and Offset on Stage
        if (carouselHeroStageImage != null)
        {
            carouselHeroStageImage.transform.localScale = GetHeroVisualScale(heroId);
            carouselHeroStageImage.rectTransform.anchoredPosition = GetHeroCarouselVisualOffset(heroId);

            Sprite[] frames = GetHeroIdleFrames(heroId);
            if (frames != null && frames.Length > 0 && frames[0] != null)
            {
                carouselAnimFrame = 0;
                carouselHeroStageImage.sprite = frames[0];
            }
        }
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

            // GUARANTEE Close Button [X] is hooked, interactive, and brought to top
            Transform cbT = rosterPanel.transform.Find("RosterBox/Btn_CloseTopRight");
            if (cbT != null)
            {
                Button cb = cbT.GetComponent<Button>();
                if (cb != null)
                {
                    cb.interactable = true;
                    cb.onClick.RemoveAllListeners();
                    cb.onClick.AddListener(CloseRosterPanel);
                    rosterBackBtn = cb;
                }
                AttachPointerClick(cbT.gameObject, CloseRosterPanel);
                foreach (var img in cbT.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject != cbT.gameObject) img.raycastTarget = false;
                }
                cbT.SetAsLastSibling();
            }
        }
        if (settingsPanel != null) settingsPanel.SetActive(false);
        InspectHero("vael");
        UpdateAllTexts();
    }

    public void LoadMap(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName == "SampleScene") sceneName = "Map1_Thestart";

        PlayerInventory.ResetRunShards();
        LevelUpChoiceUI.ResetMatchRerolls();
        MatchTimerUI.ResetRunTimer();
        PlayerHealth.ResetRunPreservedHealth();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneName);
    }
}
