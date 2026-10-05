using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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
    [SerializeField] private Button rosterActionBtn;
    [SerializeField] private TextMeshProUGUI rosterActionBtnText;
    [SerializeField] private Button rosterBackBtn;
    [SerializeField] private TextMeshProUGUI rosterBackBtnText;

    [Header("Roster Avatar Cards")]
    [SerializeField] private Button cardVaelBtn;
    [SerializeField] private Button cardMaidBtn;
    [SerializeField] private Button cardRoninBtn;
    [SerializeField] private Button cardAzureBtn;
    [SerializeField] private Image cardVaelAvatarImg;
    [SerializeField] private Image cardMaidAvatarImg;
    [SerializeField] private Image cardRoninAvatarImg;
    [SerializeField] private Image cardAzureAvatarImg;
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
    [SerializeField] private TextMeshProUGUI cardPlaceholderText;

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
        EnsureDynamicBackground();
        EnsureBottomNavigationBar();
        EnsureHeroSprites();
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

        // 2. Ensure 6 Navigation Buttons:
        // [ XUẤT TRẬN / BATTLE ]  [ NHÂN VẬT / HEROES ]  [ KỸ NĂNG / SKILLS ]  [ TÚI ĐỒ / INVENTORY ]  [ CỬA HÀNG / SHOP ]  [ CÀI ĐẶT / SETTINGS ]
        float btnWidth = 165f;
        float btnHeight = 52f;
        float posY = 50f;

        playButton = EnsureNavButton(navParent, playButton, "Btn_Play", LocalizationManager.Get("BTN_PLAY"), new Vector2(-425f, posY), new Vector2(btnWidth, btnHeight), new Color(0.85f, 0.55f, 0.15f, 1f), out playBtnText);
        rosterButton = EnsureNavButton(navParent, rosterButton, "Btn_Hero", LocalizationManager.Get("BTN_ROSTER"), new Vector2(-255f, posY), new Vector2(btnWidth, btnHeight), new Color(0.22f, 0.48f, 0.82f, 1f), out rosterBtnText);
        skillButton = EnsureNavButton(navParent, skillButton, "Btn_Skills", LocalizationManager.Get("BTN_SKILLS"), new Vector2(-85f, posY), new Vector2(btnWidth, btnHeight), new Color(0.12f, 0.55f, 0.85f, 1f), out skillBtnText);
        inventoryButton = EnsureNavButton(navParent, inventoryButton, "Btn_Inventory", LocalizationManager.Get("BTN_INVENTORY"), new Vector2(85f, posY), new Vector2(btnWidth, btnHeight), new Color(0.55f, 0.28f, 0.82f, 1f), out inventoryBtnText);
        shopButton = EnsureNavButton(navParent, shopButton, "Btn_Shop", LocalizationManager.Get("BTN_SHOP"), new Vector2(255f, posY), new Vector2(btnWidth, btnHeight), new Color(0.22f, 0.65f, 0.42f, 1f), out shopBtnText);
        settingsButton = EnsureNavButton(navParent, settingsButton, "Btn_Settings", LocalizationManager.Get("BTN_SETTINGS"), new Vector2(425f, posY), new Vector2(btnWidth, btnHeight), new Color(0.42f, 0.46f, 0.56f, 1f), out settingsBtnText);

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
        titleTxt.fontSize = 18;
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
        descTxt.fontSize = 14;
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
        bRt.sizeDelta = new Vector2(200f, 44f);

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
        btTmp.fontSize = 16;
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

            if (boxRtExisting == null || boxRtExisting.anchorMax != Vector2.one || rosterHeroNameText == null || hasOldSkillFrame || hasOldBtnFrame || hasOldMaid)
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

        // 1. Root Modal Overlay
        rosterPanel = new GameObject("RosterModalPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        rosterPanel.layer = 5;
        rosterPanel.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = rosterPanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        Image overlayImg = rosterPanel.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        overlayImg.raycastTarget = true;

        Button overlayBtn = rosterPanel.GetComponent<Button>();
        overlayBtn.targetGraphic = overlayImg;
        overlayBtn.onClick.RemoveAllListeners();
        overlayBtn.onClick.AddListener(CloseRosterPanel);

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

        var outline = dialogBox.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.95f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

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

        var closeOutline = closeBtnObj.AddComponent<Outline>();
        closeOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.9f);
        closeOutline.effectDistance = new Vector2(2f, -2f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(CloseRosterPanel);

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        closeTxtObj.layer = 5;
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.sizeDelta = Vector2.zero;
        var closeTmp = closeTxtObj.AddComponent<TextMeshProUGUI>();
        closeTmp.text = "X";
        closeTmp.fontSize = 24;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = Color.white;
        closeTmp.raycastTarget = false;

        rosterBackBtn = closeBtn;
        rosterBackBtnText = null;

        // 4. LEFT COLUMN: Hero Selection List (Full-Height Split Left Column)
        GameObject leftCol = new GameObject("HeroSelectionColumn", typeof(RectTransform));
        leftCol.transform.SetParent(dialogBox.transform, false);
        RectTransform lRt = leftCol.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(0.48f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.offsetMin = new Vector2(40f, 30f);
        lRt.offsetMax = new Vector2(-15f, -110f);

        GameObject lHeaderObj = new GameObject("ColHeader", typeof(RectTransform), typeof(CanvasRenderer));
        lHeaderObj.transform.SetParent(leftCol.transform, false);
        RectTransform lhRt = lHeaderObj.GetComponent<RectTransform>();
        lhRt.anchorMin = new Vector2(0f, 1f);
        lhRt.anchorMax = new Vector2(1f, 1f);
        lhRt.pivot = new Vector2(0f, 1f);
        lhRt.anchoredPosition = new Vector2(0f, 0f);
        lhRt.sizeDelta = new Vector2(0f, 30f);
        leftColHeaderText = lHeaderObj.AddComponent<TextMeshProUGUI>();
        leftColHeaderText.fontSize = 18;
        leftColHeaderText.fontStyle = FontStyles.Bold;
        leftColHeaderText.color = new Color(0.85f, 0.90f, 0.95f);
        leftColHeaderText.text = LocalizationManager.T("CHỌN ANH HÙNG", "HERO SELECTION");

        EnsureHeroSprites();
        Sprite vaelAvatar = (vaelIdleFrames != null && vaelIdleFrames.Length > 0) ? vaelIdleFrames[0] : null;
        cardVaelBtn = CreateAvatarCard(leftCol.transform, "vael", new Vector2(0f, -40f), new Vector2(0f, 140f),
            vaelAvatar, false,
            out cardVaelAvatarImg, out cardVaelTitleText, out cardVaelSubText, out cardVaelStatusText);
        cardVaelBtn.onClick.AddListener(() => InspectHero("vael"));

        Sprite roninAvatar = (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames[0] : null;
        cardRoninBtn = CreateAvatarCard(leftCol.transform, "ronin", new Vector2(0f, -190f), new Vector2(0f, 140f),
            roninAvatar, false,
            out cardRoninAvatarImg, out cardRoninTitleText, out cardRoninSubText, out cardRoninStatusText);
        cardRoninBtn.onClick.AddListener(() => InspectHero("ronin"));
        cardMaidBtn = cardRoninBtn;
        cardMaidAvatarImg = cardRoninAvatarImg;
        cardMaidTitleText = cardRoninTitleText;
        cardMaidSubText = cardRoninSubText;
        cardMaidStatusText = cardRoninStatusText;

        Sprite azureAvatar = (azureIdleFrames != null && azureIdleFrames.Length > 0) ? azureIdleFrames[0] : null;
        cardAzureBtn = CreateAvatarCard(leftCol.transform, "azure", new Vector2(0f, -340f), new Vector2(0f, 140f),
            azureAvatar, false,
            out cardAzureAvatarImg, out cardAzureTitleText, out cardAzureSubText, out cardAzureStatusText);
        cardAzureBtn.onClick.AddListener(() => InspectHero("azure"));

        // 5. RIGHT COLUMN: Showcase Details Panel (Full-Height Split Right Column)
        GameObject rightCol = new GameObject("ShowcaseDetailsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightCol.transform.SetParent(dialogBox.transform, false);
        RectTransform rRt = rightCol.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.48f, 0f);
        rRt.anchorMax = new Vector2(1f, 1f);
        rRt.pivot = new Vector2(1f, 1f);
        rRt.offsetMin = new Vector2(15f, 30f);
        rRt.offsetMax = new Vector2(-40f, -110f);

        Image rcImg = rightCol.GetComponent<Image>();
        rcImg.color = new Color(0.09f, 0.12f, 0.18f, 0.96f);

        var rcOutline = rightCol.AddComponent<Outline>();
        rcOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.6f);
        rcOutline.effectDistance = new Vector2(2f, -2f);

        // Animation Showcase Box (with RectMask2D)
        GameObject animBox = new GameObject("ShowcaseAnimBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        animBox.transform.SetParent(rightCol.transform, false);
        RectTransform aRt = animBox.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f);
        aRt.anchorMax = new Vector2(0f, 1f);
        aRt.pivot = new Vector2(0f, 1f);
        aRt.anchoredPosition = new Vector2(30f, -30f);
        aRt.sizeDelta = new Vector2(160f, 160f);

        Image abImg = animBox.GetComponent<Image>();
        abImg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
        var abOutline = animBox.AddComponent<Outline>();
        abOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.95f);
        abOutline.effectDistance = new Vector2(2f, -2f);

        GameObject animImgObj = new GameObject("HeroLiveSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        animImgObj.transform.SetParent(animBox.transform, false);
        RectTransform aiRt = animImgObj.GetComponent<RectTransform>();
        aiRt.anchorMin = Vector2.zero;
        aiRt.anchorMax = Vector2.one;
        aiRt.sizeDelta = Vector2.zero;
        rosterHeroPreviewImage = animImgObj.GetComponent<Image>();
        rosterHeroPreviewImage.preserveAspect = true;
        rosterHeroPreviewImage.transform.localScale = GetHeroVisualScale("vael");

        // Info Row beside AnimBox
        GameObject nameObj = new GameObject("RosterHeroName", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.transform.SetParent(rightCol.transform, false);
        RectTransform nRt = nameObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(215f, -30f);
        nRt.sizeDelta = new Vector2(-235f, 36f);
        rosterHeroNameText = nameObj.AddComponent<TextMeshProUGUI>();
        rosterHeroNameText.fontSize = 26;
        rosterHeroNameText.fontStyle = FontStyles.Bold;
        rosterHeroNameText.color = new Color(1f, 0.88f, 0.45f);

        GameObject roleObj = new GameObject("RosterHeroRole", typeof(RectTransform), typeof(CanvasRenderer));
        roleObj.transform.SetParent(rightCol.transform, false);
        RectTransform rlRt = roleObj.GetComponent<RectTransform>();
        rlRt.anchorMin = new Vector2(0f, 1f);
        rlRt.anchorMax = new Vector2(1f, 1f);
        rlRt.pivot = new Vector2(0f, 1f);
        rlRt.anchoredPosition = new Vector2(215f, -75f);
        rlRt.sizeDelta = new Vector2(-235f, 26f);
        rosterHeroRoleText = roleObj.AddComponent<TextMeshProUGUI>();
        rosterHeroRoleText.fontSize = 16f;
        rosterHeroRoleText.color = new Color(0.6f, 0.85f, 1f);

        GameObject statsObj = new GameObject("RosterHeroStats", typeof(RectTransform), typeof(CanvasRenderer));
        statsObj.transform.SetParent(rightCol.transform, false);
        RectTransform stRt = statsObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 1f);
        stRt.anchorMax = new Vector2(1f, 1f);
        stRt.pivot = new Vector2(0f, 1f);
        stRt.anchoredPosition = new Vector2(215f, -115f);
        stRt.sizeDelta = new Vector2(-235f, 30f);
        rosterHeroStatsText = statsObj.AddComponent<TextMeshProUGUI>();
        rosterHeroStatsText.fontSize = 16;
        rosterHeroStatsText.fontStyle = FontStyles.Bold;
        rosterHeroStatsText.color = new Color(0.95f, 0.95f, 0.95f);

        // Special Skill Section - Spacious Stretched Box
        GameObject skillBox = new GameObject("SkillBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        skillBox.transform.SetParent(rightCol.transform, false);
        RectTransform skRt = skillBox.GetComponent<RectTransform>();
        skRt.anchorMin = new Vector2(0f, 0f);
        skRt.anchorMax = new Vector2(1f, 1f);
        skRt.pivot = new Vector2(0.5f, 1f);
        skRt.offsetMin = new Vector2(30f, 105f);
        skRt.offsetMax = new Vector2(-30f, -210f);
        Image skImg = skillBox.GetComponent<Image>();
        skImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        var skOutline = skillBox.AddComponent<Outline>();
        skOutline.effectColor = new Color(0.40f, 0.45f, 0.55f, 0.5f);

        GameObject skTitleObj = new GameObject("SkillTitle", typeof(RectTransform), typeof(CanvasRenderer));
        skTitleObj.transform.SetParent(skillBox.transform, false);
        RectTransform sktRt = skTitleObj.GetComponent<RectTransform>();
        sktRt.anchorMin = new Vector2(0f, 1f);
        sktRt.anchorMax = new Vector2(1f, 1f);
        sktRt.pivot = new Vector2(0.5f, 1f);
        sktRt.anchoredPosition = new Vector2(20f, -16f);
        sktRt.sizeDelta = new Vector2(-40f, 30f);
        rosterHeroSkillTitleText = skTitleObj.AddComponent<TextMeshProUGUI>();
        rosterHeroSkillTitleText.fontSize = 18;
        rosterHeroSkillTitleText.fontStyle = FontStyles.Bold;
        rosterHeroSkillTitleText.color = new Color(1f, 0.85f, 0.35f);

        GameObject skDescObj = new GameObject("SkillDesc", typeof(RectTransform), typeof(CanvasRenderer));
        skDescObj.transform.SetParent(skillBox.transform, false);
        RectTransform skdRt = skDescObj.GetComponent<RectTransform>();
        skdRt.anchorMin = Vector2.zero;
        skdRt.anchorMax = Vector2.one;
        skdRt.anchoredPosition = new Vector2(20f, -54f);
        skdRt.sizeDelta = new Vector2(-40f, -70f);
        rosterHeroSkillDescText = skDescObj.AddComponent<TextMeshProUGUI>();
        rosterHeroSkillDescText.fontSize = 15f;
        rosterHeroSkillDescText.lineSpacing = 12;
        rosterHeroSkillDescText.color = new Color(0.85f, 0.88f, 0.92f);

        // Action Button (Bottom of Right Column)
        GameObject actBtnObj = new GameObject("Btn_RosterAction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actBtnObj.transform.SetParent(rightCol.transform, false);
        RectTransform actRt = actBtnObj.GetComponent<RectTransform>();
        actRt.anchorMin = new Vector2(0f, 0f);
        actRt.anchorMax = new Vector2(1f, 0f);
        actRt.pivot = new Vector2(0.5f, 0f);
        actRt.anchoredPosition = new Vector2(0f, 25f);
        actRt.sizeDelta = new Vector2(-60f, 60f);

        Image actImg = actBtnObj.GetComponent<Image>();
        actImg.color = new Color(0.16f, 0.58f, 0.28f, 1f);
        var actOutline = actBtnObj.AddComponent<Outline>();
        actOutline.effectColor = new Color(0.95f, 0.82f, 0.35f, 0.9f);
        actOutline.effectDistance = new Vector2(2f, -2f);

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
        rosterActionBtnText.fontSize = 20f;
        rosterActionBtnText.fontStyle = FontStyles.Bold;
        rosterActionBtnText.alignment = TextAlignmentOptions.Center;
        rosterActionBtnText.color = Color.white;
        rosterActionBtnText.raycastTarget = false;

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
        if (string.IsNullOrEmpty(heroId)) return Vector3.one;
        string id = heroId.ToLower();
        if (id == "vael" || id == "vaelstrider")
        {
            return new Vector3(2.4f, 2.4f, 1f);
        }
        else if (id == "ronin" || id == "galeronin" || id == "player2")
        {
            return new Vector3(2.4f, 2.4f, 1f);
        }
        else if (id == "maid" || id == "elysia")
        {
            return new Vector3(2.4f, 2.4f, 1f);
        }
        else if (id == "azure" || id == "azureblade" || id == "player3")
        {
            return new Vector3(2.4f, 2.4f, 1f);
        }
        return Vector3.one;
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
        img.color = isLocked ? new Color(0.10f, 0.11f, 0.16f, 0.90f) : new Color(0.12f, 0.16f, 0.24f, 0.95f);

        var outline = card.AddComponent<Outline>();
        outline.effectColor = isLocked ? new Color(0.40f, 0.40f, 0.45f, 0.5f) : new Color(0.85f, 0.72f, 0.35f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

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
            liTmp.fontSize = 12;
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
        tRt.anchoredPosition = new Vector2(120f, -16f);
        tRt.sizeDelta = new Vector2(-130f, 26f);
        titleTxtRef = tObj.AddComponent<TextMeshProUGUI>();
        titleTxtRef.fontSize = 17f;
        titleTxtRef.fontStyle = FontStyles.Bold;
        titleTxtRef.color = isLocked ? new Color(0.75f, 0.75f, 0.8f) : new Color(1f, 0.88f, 0.45f);
        titleTxtRef.raycastTarget = false;

        GameObject sObj = new GameObject("Sub", typeof(RectTransform), typeof(CanvasRenderer));
        sObj.transform.SetParent(card.transform, false);
        RectTransform sRt = sObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0f, 1f);
        sRt.anchoredPosition = new Vector2(120f, -46f);
        sRt.sizeDelta = new Vector2(-130f, 22f);
        subTxtRef = sObj.AddComponent<TextMeshProUGUI>();
        subTxtRef.fontSize = 13.5f;
        subTxtRef.color = new Color(0.6f, 0.85f, 1f);
        subTxtRef.raycastTarget = false;

        GameObject stObj = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer));
        stObj.transform.SetParent(card.transform, false);
        RectTransform stRt = stObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(1f, 0f);
        stRt.pivot = new Vector2(0f, 0f);
        stRt.anchoredPosition = new Vector2(120f, 16f);
        stRt.sizeDelta = new Vector2(-130f, 22f);
        statusTxtRef = stObj.AddComponent<TextMeshProUGUI>();
        statusTxtRef.fontSize = 13f;
        statusTxtRef.fontStyle = FontStyles.Bold;
        statusTxtRef.raycastTarget = false;

        return btn;
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
        txtRef.fontSize = 14;
        txtRef.alignment = TextAlignmentOptions.Center;
        txtRef.color = new Color(0.5f, 0.55f, 0.65f, 0.7f);
        txtRef.text = LocalizationManager.T("KHÓA - ANH HÙNG TIẾP THEO", "LOCKED - COMING SOON");
    }

    public void EnsureHeroSprites()
    {
        if (heroPreviewImage != null)
        {
            heroPreviewImage.preserveAspect = true;
            heroPreviewImage.color = Color.white;
        }

#if UNITY_EDITOR
        if (vaelIdleFrames == null || vaelIdleFrames.Length == 0)
        {
            var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player/IDLE/idle_down.png");
            var list = new List<Sprite>();
            if (objects != null)
            {
                foreach (var o in objects) if (o is Sprite s) list.Add(s);
            }
            if (list.Count > 0) vaelIdleFrames = list.ToArray();
        }

        if (roninIdleFrames == null || roninIdleFrames.Length == 0)
        {
            var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player2/_down idle.png");
            var list = new List<Sprite>();
            if (objects != null)
            {
                foreach (var o in objects) if (o is Sprite s) list.Add(s);
            }
            if (list.Count > 0) roninIdleFrames = list.ToArray();
        }

        if (maidIdleFrames == null || maidIdleFrames.Length == 0)
        {
            maidIdleFrames = roninIdleFrames;
        }

        if (azureIdleFrames == null || azureIdleFrames.Length == 0)
        {
            var objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player3/Sword_Idle.png");
            var list = new List<Sprite>();
            if (objects != null)
            {
                foreach (var o in objects) if (o is Sprite s) list.Add(s);
            }
            if (list.Count > 0) azureIdleFrames = list.ToArray();
        }
#endif
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

        if (playBtnText != null) playBtnText.text = LocalizationManager.Get("BTN_PLAY");
        if (mapSelectBtnText != null) mapSelectBtnText.text = LocalizationManager.Get("BTN_MAP_SELECT");
        if (rosterBtnText != null) rosterBtnText.text = LocalizationManager.Get("BTN_ROSTER");
        if (skillBtnText != null) skillBtnText.text = LocalizationManager.Get("BTN_SKILLS");
        if (inventoryBtnText != null) inventoryBtnText.text = LocalizationManager.Get("BTN_INVENTORY");
        if (shopBtnText != null) shopBtnText.text = LocalizationManager.Get("BTN_SHOP");
        if (settingsBtnText != null) settingsBtnText.text = LocalizationManager.Get("BTN_SETTINGS");
        if (exitBtnText != null) exitBtnText.text = LocalizationManager.Get("BTN_EXIT");

        // Map Select panel
        if (mapPanelTitleText != null) mapPanelTitleText.text = LocalizationManager.Get("BTN_MAP_SELECT");
        if (map1TitleText != null) map1TitleText.text = LocalizationManager.Get("MAP1_NAME");
        if (map1DescText != null) map1DescText.text = LocalizationManager.Get("MAP1_DESC");
        if (map2TitleText != null) map2TitleText.text = LocalizationManager.Get("MAP2_NAME");
        if (map2DescText != null) map2DescText.text = LocalizationManager.Get("MAP2_DESC");
        if (map3TitleText != null) map3TitleText.text = LocalizationManager.Get("MAP3_NAME");
        if (map3DescText != null) map3DescText.text = LocalizationManager.Get("MAP3_DESC");
        if (mapBackBtnText != null) mapBackBtnText.text = LocalizationManager.Get("BTN_BACK");

        // Roster panel titles & Card texts
        if (rosterPanelTitleText != null) rosterPanelTitleText.text = LocalizationManager.Get("BTN_ROSTER");
        if (leftColHeaderText != null) leftColHeaderText.text = LocalizationManager.T("CHỌN ANH HÙNG", "HERO SELECTION");
        if (rosterBackBtnText != null) rosterBackBtnText.text = LocalizationManager.Get("BTN_BACK");

        string currentSelected = CharacterManager.EnsureInstance().GetSelectedCharacterId();

        if (cardVaelTitleText != null) cardVaelTitleText.text = LocalizationManager.Get("HERO_VAEL_NAME");
        if (cardVaelSubText != null) cardVaelSubText.text = LocalizationManager.Get("HERO_VAEL_ROLE");
        if (cardVaelStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_VAELSTRIDER);
            cardVaelStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }

        if (cardRoninTitleText != null) cardRoninTitleText.text = LocalizationManager.Get("HERO_RONIN_NAME");
        if (cardRoninSubText != null) cardRoninSubText.text = LocalizationManager.Get("HERO_RONIN_ROLE");
        if (cardRoninStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_GALE_RONIN);
            cardRoninStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }
        else if (cardMaidTitleText != null)
        {
            cardMaidTitleText.text = LocalizationManager.Get("HERO_RONIN_NAME");
            if (cardMaidSubText != null) cardMaidSubText.text = LocalizationManager.Get("HERO_RONIN_ROLE");
            if (cardMaidStatusText != null)
            {
                bool isSel = (currentSelected == CharacterManager.CHAR_GALE_RONIN);
                cardMaidStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
            }
        }

        if (cardAzureTitleText != null) cardAzureTitleText.text = LocalizationManager.T("Lam Kiếm Sĩ (Azure)", "Azure Blade");
        if (cardAzureSubText != null) cardAzureSubText.text = LocalizationManager.T("Trọng Kiếm / Sóng Kiếm", "Heavy / Sword Waves");
        if (cardAzureStatusText != null)
        {
            bool isSel = (currentSelected == CharacterManager.CHAR_AZURE_BLADE);
            cardAzureStatusText.text = isSel ? LocalizationManager.T("<color=#55FF55>ĐANG CHỌN</color>", "<color=#55FF55>SELECTED</color>") : LocalizationManager.T("<color=#88CCFF>SẴN SÀNG</color>", "<color=#88CCFF>READY</color>");
        }

        UpdateCurrencyDisplay();
        UpdateHeroDetails();
        InspectHero(inspectedHeroId);
        UpdateMapCardsLockState();
    }

    private void UpdateHeroDetails()
    {
        string curChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        if (curChar == CharacterManager.CHAR_AZURE_BLADE)
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.T("Lam Kiếm Sĩ", "Azure Blade");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("Trọng Kiếm Khí (Sóng Kiếm Tầm Xa)", "Heavy Crescent Blade");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("Bão Sóng Kiếm", "Crescent Tempest");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T("Đòn chém chậm nhưng uy lực, mỗi nhát chém phóng ra sóng kiếm xuyên thấu quái vật. Tuyệt kỹ phóng ra 5 đợt kiếm khí hình nan quạt quét sạch chiến trường!", "Heavy slashes that unleash piercing sword waves. Special skill releases 5 massive crescent waves in a fan spread!");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 110   CÔNG: 24   TỐC: 4.8", "HP: 110   ATK: 24   SPD: 4.8");
            }
        }
        else if (curChar == CharacterManager.CHAR_GALE_RONIN)
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.T("Phong Lôi Lãng Khách", "Gale Ronin");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.T("Bạch Dạ Kiếm Khách (Phong Ấn & Ảo Ảnh)", "Phantom Blade (Wind Marks & Cleave)");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.T("Bão Phong Trảm", "Phantom Cleave");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.T("Đòn chém quét 180° tích Phong Ấn bộc phá bạo kích & lốc xoáy. Tuyệt kỹ lướt ảo ảnh chớp nhoáng trảm sát toàn bộ kẻ địch!", "180° cleave stacking Wind Marks for critical bursts & vortex. Special skill unleashes phantom dash slashes across foes!");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 95   CÔNG: 24   TỐC: 5.4", "HP: 95   ATK: 24   SPD: 5.4");
            }
        }
        else
        {
            if (heroNameText != null) heroNameText.text = LocalizationManager.Get("HERO_VAEL_NAME");
            if (heroRoleText != null) heroRoleText.text = LocalizationManager.Get("HERO_VAEL_ROLE");
            if (heroSkillText != null) heroSkillText.text = LocalizationManager.Get("HERO_VAEL_SKILL");
            if (heroSkillDescText != null) heroSkillDescText.text = LocalizationManager.Get("HERO_VAEL_SKILL_DESC");
            if (heroStatsSummaryText != null)
            {
                heroStatsSummaryText.text = LocalizationManager.T("MÁU: 100   CÔNG: 20   TỐC: 5.0", "HP: 100   ATK: 20   SPD: 5.0");
            }
        }
    }

    public void InspectHero(string heroId)
    {
        inspectedHeroId = heroId.ToLower();
        string currentSelected = CharacterManager.EnsureInstance().GetSelectedCharacterId();

        if (inspectedHeroId == "azure" || inspectedHeroId == "azureblade")
        {
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Lam Kiếm Sĩ (Azure Blade)", "Azure Blade");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("Trọng Kiếm Khí (Sóng Kiếm Tầm Xa)", "Heavy Crescent Blade (Ranged Waves)");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 110   CÔNG: 24   TỐC: 4.8", "HP: 110   ATK: 24   SPD: 4.8");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("Tuyệt Kỹ: Bão Sóng Kiếm", "Special: Crescent Tempest");
            if (rosterHeroSkillDescText != null) rosterHeroSkillDescText.text = LocalizationManager.T("Đòn chém chậm nhưng uy lực, mỗi nhát chém phóng ra sóng kiếm xuyên thấu quái vật. Tuyệt kỹ phóng ra 5 đợt kiếm khí hình nan quạt quét sạch chiến trường!", "Heavy slashes that unleash piercing sword waves. Special skill releases 5 massive crescent waves in a fan spread!");

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
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.T("Phong Lôi Lãng Khách (Gale Ronin)", "Gale Ronin (Phantom Blade)");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.T("Bạch Dạ Kiếm Khách (Phong Ấn & Ảo Ảnh)", "Phantom Blade (Wind Marks & Cleave)");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 95   CÔNG: 24   TỐC: 5.4", "HP: 95   ATK: 24   SPD: 5.4");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.T("Tuyệt Kỹ: Bão Phong Trảm", "Special: Phantom Cleave");
            if (rosterHeroSkillDescText != null) rosterHeroSkillDescText.text = LocalizationManager.T("Đòn chém quét 180° tích Phong Ấn bộc phá bạo kích & lốc xoáy. Tuyệt kỹ lướt ảo ảnh chớp nhoáng trảm sát toàn bộ kẻ địch!", "180° cleave stacking Wind Marks for critical bursts & vortex. Special skill unleashes phantom dash slashes across foes!");

            bool isSelected = (currentSelected == CharacterManager.CHAR_GALE_RONIN);
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
            if (rosterHeroNameText != null) rosterHeroNameText.text = LocalizationManager.Get("HERO_VAEL_NAME");
            if (rosterHeroRoleText != null) rosterHeroRoleText.text = LocalizationManager.Get("HERO_VAEL_ROLE");
            if (rosterHeroStatsText != null) rosterHeroStatsText.text = LocalizationManager.T("MÁU: 100   CÔNG: 20   TỐC: 5.0", "HP: 100   ATK: 20   SPD: 5.0");
            if (rosterHeroSkillTitleText != null) rosterHeroSkillTitleText.text = LocalizationManager.Get("HERO_VAEL_SKILL");
            if (rosterHeroSkillDescText != null) rosterHeroSkillDescText.text = LocalizationManager.Get("HERO_VAEL_SKILL_DESC");

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
            UpdateRosterEquipmentMaterial();
        }

        StartRosterHeroAnimation();
    }

    private Material rosterHeroMat;

    public void UpdateRosterEquipmentMaterial()
    {
        if (rosterHeroPreviewImage == null) return;
        if (inspectedHeroId == "maid" || inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "azure" || inspectedHeroId == "azureblade")
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

        if (inspectedHeroId == "azure" || inspectedHeroId == "azureblade")
        {
            targetChar = CharacterManager.CHAR_AZURE_BLADE;
            heroPref = "azure";
        }
        else if (inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid")
        {
            targetChar = CharacterManager.CHAR_GALE_RONIN;
            heroPref = "ronin";
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

        if (heroId.ToLower() == "azure" || heroId.ToLower() == "azureblade")
        {
            target = CharacterManager.CHAR_AZURE_BLADE;
            heroPref = "azure";
        }
        else if (heroId.ToLower() == "ronin" || heroId.ToLower() == "galeronin" || heroId.ToLower() == "player2" || heroId.ToLower() == "maid")
        {
            target = CharacterManager.CHAR_GALE_RONIN;
            heroPref = "ronin";
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
        if (curChar == CharacterManager.CHAR_AZURE_BLADE) frames = azureIdleFrames;
        else if (curChar == CharacterManager.CHAR_GALE_RONIN) frames = (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames;

        if (heroPreviewImage == null) yield break;

        heroPreviewImage.preserveAspect = true;
        heroPreviewImage.color = Color.white;
        heroPreviewImage.transform.localScale = GetHeroVisualScale(curChar == CharacterManager.CHAR_AZURE_BLADE ? "azure" : (curChar == CharacterManager.CHAR_GALE_RONIN ? "ronin" : "vael"));

        if (frames == null || frames.Length == 0)
        {
            EnsureHeroSprites();
            frames = (curChar == CharacterManager.CHAR_AZURE_BLADE) 
                ? azureIdleFrames 
                : (curChar == CharacterManager.CHAR_GALE_RONIN ? ((roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames) : vaelIdleFrames);
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
        if (inspectedHeroId == "azure" || inspectedHeroId == "azureblade") 
            frames = azureIdleFrames;
        else if (inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid") 
            frames = (roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames;

        if (rosterHeroPreviewImage == null) yield break;

        rosterHeroPreviewImage.preserveAspect = true;
        rosterHeroPreviewImage.color = Color.white;

        if (frames == null || frames.Length == 0)
        {
            EnsureHeroSprites();
            frames = (inspectedHeroId == "azure" || inspectedHeroId == "azureblade") 
                ? azureIdleFrames 
                : ((inspectedHeroId == "ronin" || inspectedHeroId == "galeronin" || inspectedHeroId == "player2" || inspectedHeroId == "maid") ? ((roninIdleFrames != null && roninIdleFrames.Length > 0) ? roninIdleFrames : maidIdleFrames) : vaelIdleFrames);
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

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneName);
    }
}
