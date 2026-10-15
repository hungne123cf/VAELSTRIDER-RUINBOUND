// ========================================================
// VAELSTRIDER: RUINBOUND - Costume Wardrobe UI
// Interactive costume showroom allowing players to preview,
// unlock, and equip high-tier armor outfits & skins for all heroes.
// ========================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CostumeWardrobeUI : MonoBehaviour
{
    public static CostumeWardrobeUI Instance { get; private set; }

    private string currentHeroId = CharacterManager.CHAR_TITAN_BERSERKER;
    private string previewingCostumeId = "";

    // UI Elements
    private GameObject dialogBox;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI currencyText;
    private Image heroPreviewImg;
    private TextMeshProUGUI heroNameText;
    private TextMeshProUGUI heroCostumeNameText;
    private TextMeshProUGUI heroCostumeBuffsText;
    private Transform costumeListContent;
    private Button closeBtn;

    private static Sprite uisprite = null;
    private static TMP_FontAsset mainFont = null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LocalizationManager.OnLanguageChanged += RefreshUI;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshUI;
    }

    private void Update()
    {
        if (gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B)))
        {
            Close();
        }
    }

    public static CostumeWardrobeUI EnsureUIExists()
    {
        if (Instance != null) return Instance;

        var existing = FindAnyObjectByType<CostumeWardrobeUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        GameObject root = new GameObject("CostumeWardrobeUI", typeof(RectTransform), typeof(CanvasGroup), typeof(CostumeWardrobeUI));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Instance = root.GetComponent<CostumeWardrobeUI>();
        Instance.BuildUIHierarchy();
        root.SetActive(false);

        return Instance;
    }

    private void BuildUIHierarchy()
    {
        if (dialogBox != null) return;

        // Overlay Backdrop
        var bgImg = gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);

        // Dialog Box (Full-featured dark slate modal)
        dialogBox = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        dialogBox.transform.SetParent(transform, false);
        RectTransform dRt = dialogBox.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0.5f, 0.5f);
        dRt.anchorMax = new Vector2(0.5f, 0.5f);
        dRt.sizeDelta = new Vector2(920, 560);

        var dImg = dialogBox.GetComponent<Image>();
        dImg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);
        var dOutline = dialogBox.AddComponent<Outline>();
        dOutline.effectColor = new Color(0.85f, 0.65f, 0.20f, 0.60f);
        dOutline.effectDistance = new Vector2(2, -2);

        // 1. Header Bar
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(dialogBox.transform, false);
        RectTransform hRt = header.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0, 1);
        hRt.anchorMax = new Vector2(1, 1);
        hRt.pivot = new Vector2(0.5f, 1);
        hRt.sizeDelta = new Vector2(0, 56);
        header.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.20f, 1f);

        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(header.transform, false);
        RectTransform tRt = titleGo.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0, 0);
        tRt.anchorMax = new Vector2(0.7f, 1);
        tRt.offsetMin = new Vector2(20, 0);
        titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(1f, 0.85f, 0.35f, 1f);

        GameObject curGo = new GameObject("CurrencyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        curGo.transform.SetParent(header.transform, false);
        RectTransform cRt = curGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.6f, 0);
        cRt.anchorMax = new Vector2(0.92f, 1);
        currencyText = curGo.GetComponent<TextMeshProUGUI>();
        currencyText.fontSize = 17;
        currencyText.fontStyle = FontStyles.Bold;
        currencyText.alignment = TextAlignmentOptions.MidlineRight;
        currencyText.color = new Color(0.4f, 0.9f, 1f, 1f);

        // Close Button
        GameObject closeGo = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(header.transform, false);
        RectTransform closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 0.5f);
        closeRt.anchorMax = new Vector2(1, 0.5f);
        closeRt.sizeDelta = new Vector2(40, 40);
        closeRt.anchoredPosition = new Vector2(-28, 0);
        closeGo.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f, 1f);
        closeBtn = closeGo.GetComponent<Button>();
        closeBtn.onClick.AddListener(Close);

        GameObject closeTxt = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxt.transform.SetParent(closeGo.transform, false);
        RectTransform xRt = closeTxt.GetComponent<RectTransform>();
        xRt.anchorMin = Vector2.zero; xRt.anchorMax = Vector2.one;
        var xTmp = closeTxt.GetComponent<TextMeshProUGUI>();
        xTmp.text = "X";
        xTmp.fontSize = 20;
        xTmp.fontStyle = FontStyles.Bold;
        xTmp.alignment = TextAlignmentOptions.Center;
        xTmp.color = Color.white;

        // 2. Left Preview Panel
        GameObject leftPanel = new GameObject("LeftPreviewPanel", typeof(RectTransform), typeof(Image));
        leftPanel.transform.SetParent(dialogBox.transform, false);
        RectTransform lpRt = leftPanel.GetComponent<RectTransform>();
        lpRt.anchorMin = new Vector2(0, 0);
        lpRt.anchorMax = new Vector2(0.40f, 1);
        lpRt.offsetMin = new Vector2(16, 16);
        lpRt.offsetMax = new Vector2(-8, -66);
        leftPanel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 0.95f);
        var lpOutline = leftPanel.AddComponent<Outline>();
        lpOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);

        // Hero Preview Image Box
        GameObject heroBox = new GameObject("HeroPreview", typeof(RectTransform), typeof(Image));
        heroBox.transform.SetParent(leftPanel.transform, false);
        RectTransform hbRt = heroBox.GetComponent<RectTransform>();
        hbRt.anchorMin = new Vector2(0.5f, 0.62f);
        hbRt.anchorMax = new Vector2(0.5f, 0.62f);
        hbRt.sizeDelta = new Vector2(170, 170);
        heroPreviewImg = heroBox.GetComponent<Image>();
        heroPreviewImg.preserveAspect = true;

        // Hero Info Labels
        GameObject hnGo = new GameObject("HeroName", typeof(RectTransform), typeof(TextMeshProUGUI));
        hnGo.transform.SetParent(leftPanel.transform, false);
        RectTransform hnRt = hnGo.GetComponent<RectTransform>();
        hnRt.anchorMin = new Vector2(0, 0.33f);
        hnRt.anchorMax = new Vector2(1, 0.42f);
        hnRt.offsetMin = new Vector2(10, 0); hnRt.offsetMax = new Vector2(-10, 0);
        heroNameText = hnGo.GetComponent<TextMeshProUGUI>();
        heroNameText.fontSize = 20;
        heroNameText.fontStyle = FontStyles.Bold;
        heroNameText.alignment = TextAlignmentOptions.Center;
        heroNameText.color = Color.white;

        GameObject hcnGo = new GameObject("HeroCostumeName", typeof(RectTransform), typeof(TextMeshProUGUI));
        hcnGo.transform.SetParent(leftPanel.transform, false);
        RectTransform hcnRt = hcnGo.GetComponent<RectTransform>();
        hcnRt.anchorMin = new Vector2(0, 0.25f);
        hcnRt.anchorMax = new Vector2(1, 0.33f);
        hcnRt.offsetMin = new Vector2(10, 0); hcnRt.offsetMax = new Vector2(-10, 0);
        heroCostumeNameText = hcnGo.GetComponent<TextMeshProUGUI>();
        heroCostumeNameText.fontSize = 16.5f;
        heroCostumeNameText.fontStyle = FontStyles.Bold;
        heroCostumeNameText.alignment = TextAlignmentOptions.Center;
        heroCostumeNameText.color = new Color(1f, 0.85f, 0.3f, 1f);

        GameObject hcbGo = new GameObject("HeroCostumeBuffs", typeof(RectTransform), typeof(TextMeshProUGUI));
        hcbGo.transform.SetParent(leftPanel.transform, false);
        RectTransform hcbRt = hcbGo.GetComponent<RectTransform>();
        hcbRt.anchorMin = new Vector2(0, 0.04f);
        hcbRt.anchorMax = new Vector2(1, 0.24f);
        hcbRt.offsetMin = new Vector2(12, 0); hcbRt.offsetMax = new Vector2(-12, 0);
        heroCostumeBuffsText = hcbGo.GetComponent<TextMeshProUGUI>();
        heroCostumeBuffsText.fontSize = 15f;
        heroCostumeBuffsText.alignment = TextAlignmentOptions.TopLeft;
        heroCostumeBuffsText.color = new Color(0.6f, 1f, 0.6f, 1f);

        // 3. Right Scrollable Costume List
        GameObject scrollGo = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(dialogBox.transform, false);
        RectTransform sRt = scrollGo.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.40f, 0);
        sRt.anchorMax = new Vector2(1, 1);
        sRt.offsetMin = new Vector2(8, 16);
        sRt.offsetMax = new Vector2(-16, -66);

        var sImg = scrollGo.GetComponent<Image>();
        sImg.color = new Color(0.04f, 0.05f, 0.07f, 0.60f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vRt = viewport.GetComponent<RectTransform>();
        vRt.anchorMin = Vector2.zero; vRt.anchorMax = Vector2.one;
        vRt.offsetMin = Vector2.zero; vRt.offsetMax = Vector2.zero;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRt2 = content.GetComponent<RectTransform>();
        cRt2.anchorMin = new Vector2(0, 1);
        cRt2.anchorMax = new Vector2(1, 1);
        cRt2.pivot = new Vector2(0.5f, 1);
        cRt2.sizeDelta = new Vector2(0, 0);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        costumeListContent = content.transform;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.content = cRt2;
        sr.viewport = vRt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 25f;
    }

    public void Open(string heroId)
    {
        EnsureUIExists();
        currentHeroId = HeroCostumeManager.NormalizeHeroId(heroId);
        previewingCostumeId = HeroCostumeManager.GetEquippedCostumeId(currentHeroId);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        RefreshUI();
    }

    public void Close()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        gameObject.SetActive(false);

        // Update MainMenu / Gameplay visuals when closing
        var mm = FindAnyObjectByType<MainMenuManager>();
        if (mm != null)
        {
            mm.UpdateRosterEquipmentMaterial();
        }
    }

    public void RefreshUI()
    {
        if (titleText != null)
        {
            titleText.text = LocalizationManager.T("🏛️ PHÒNG THAY ĐỒ & TRANG PHỤC CHIẾN THẦN", "🏛️ WARRIOR WARDROBE & COSTUMES");
        }

        long shards = AccountDatabase.GetAethelShards();
        if (currencyText != null)
        {
            currencyText.text = $"💎 {shards:N0}";
        }

        // Left Panel info
        UpdateHeroPreview();

        // Right Panel cards
        PopulateCostumeList();
    }

    private void UpdateHeroPreview()
    {
        var activeCostume = HeroCostumeManager.GetCostume(previewingCostumeId);
        if (activeCostume == null) activeCostume = HeroCostumeManager.GetCostume(HeroCostumeManager.GetDefaultCostumeId(currentHeroId));

        if (heroNameText != null)
        {
            if (currentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
                heroNameText.text = LocalizationManager.T("Cuồng Chiến Binh (Titan)", "Titan Berserker");
            else if (currentHeroId == CharacterManager.CHAR_AZURE_BLADE)
                heroNameText.text = LocalizationManager.T("Lam Kiếm Sĩ (Azure)", "Azure Blade");
            else if (currentHeroId == CharacterManager.CHAR_GALE_RONIN)
                heroNameText.text = LocalizationManager.T("Phong Lôi Lãng Khách", "Gale Ronin");
            else
                heroNameText.text = "Vaelstrider";
        }

        if (heroCostumeNameText != null && activeCostume != null)
        {
            heroCostumeNameText.text = $"[{activeCostume.GetRarityName()}] {activeCostume.GetLocalizedName()}";
            heroCostumeNameText.color = activeCostume.GetRarityColor();
        }

        if (heroCostumeBuffsText != null && activeCostume != null)
        {
            string buffs = $"<b>{LocalizationManager.T("HIỆU QUẢ TRANG PHỤC:", "COSTUME BUFFS:")}</b>\n";
            if (activeCostume.bonusHp > 0) buffs += $"• {LocalizationManager.T("Máu tối đa", "Max HP")}: +{activeCostume.bonusHp}\n";
            if (activeCostume.bonusAtk > 0) buffs += $"• {LocalizationManager.T("Lực công kích", "Attack Damage")}: +{activeCostume.bonusAtk}\n";
            if (activeCostume.bonusDefPercent > 0) buffs += $"• {LocalizationManager.T("Giảm sát thương", "Damage Reduction")}: +{activeCostume.bonusDefPercent:0}%\n";
            if (activeCostume.bonusSpeed > 0) buffs += $"• {LocalizationManager.T("Tốc chạy", "Move Speed")}: +{activeCostume.bonusSpeed:0.1}\n";

            if (activeCostume.bonusHp == 0 && activeCostume.bonusAtk == 0 && activeCostume.bonusDefPercent == 0)
            {
                buffs += LocalizationManager.T("• Trang phục mặc định căn bản.", "• Standard default outfit.");
            }

            heroCostumeBuffsText.text = buffs;
        }

        // Update Sprite / Image tint
        if (heroPreviewImg != null)
        {
            // Load hero portrait / sprite
            Sprite spr = null;
            if (currentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
            {
                spr = Resources.Load<Sprite>("Assets/ART/Player4/UI/Hero_Player4_Portrait");
#if UNITY_EDITOR
                if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Player4/UI/Hero_Player4_Portrait.png");
#endif
            }
            else if (currentHeroId == CharacterManager.CHAR_AZURE_BLADE)
            {
#if UNITY_EDITOR
                spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Player3/Sword_Idle/Sword_Idle3_body.png");
#endif
            }

            if (spr != null) heroPreviewImg.sprite = spr;
            HeroCostumeManager.ApplyCostumeToImage(heroPreviewImg, currentHeroId, previewingCostumeId);
        }
    }

    private void PopulateCostumeList()
    {
        if (costumeListContent == null) return;

        foreach (Transform child in costumeListContent)
        {
            Destroy(child.gameObject);
        }

        var costumes = HeroCostumeManager.GetCostumesForHero(currentHeroId);
        string equippedId = HeroCostumeManager.GetEquippedCostumeId(currentHeroId);

        foreach (var costume in costumes)
        {
            CreateCostumeCard(costume, equippedId);
        }
    }

    private void CreateCostumeCard(HeroCostumeManager.HeroCostumeData data, string equippedId)
    {
        bool isEquipped = (data.id == equippedId);
        bool isUnlocked = HeroCostumeManager.IsCostumeUnlocked(data.id);
        bool isPreviewing = (data.id == previewingCostumeId);

        GameObject card = new GameObject("CostumeCard_" + data.id, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(costumeListContent, false);
        RectTransform cRt = card.GetComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(0, 95);

        var cardImg = card.GetComponent<Image>();
        cardImg.color = isPreviewing ? new Color(0.14f, 0.20f, 0.28f, 1f) : new Color(0.09f, 0.11f, 0.15f, 1f);

        var outline = card.AddComponent<Outline>();
        outline.effectColor = isEquipped ? new Color(0.2f, 0.85f, 0.4f, 0.8f) :
                              isPreviewing ? new Color(0.4f, 0.75f, 1f, 0.8f) :
                              new Color(0.25f, 0.30f, 0.40f, 0.5f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Click on card to preview
        var cardBtn = card.AddComponent<Button>();
        cardBtn.onClick.AddListener(() =>
        {
            previewingCostumeId = data.id;
            UpdateHeroPreview();
            PopulateCostumeList();
        });

        // 1. Rarity & Name Label
        GameObject nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(card.transform, false);
        RectTransform nRt = nameGo.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0, 0.60f);
        nRt.anchorMax = new Vector2(0.70f, 1f);
        nRt.offsetMin = new Vector2(14, 0); nRt.offsetMax = new Vector2(0, -6);
        var nTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nTmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(data.GetRarityColor())}>[{data.GetRarityName()}]</color> {data.GetLocalizedName()}";
        nTmp.fontSize = 16;
        nTmp.fontStyle = FontStyles.Bold;
        nTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // 2. Desc & Buffs Label
        GameObject descGo = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGo.transform.SetParent(card.transform, false);
        RectTransform dRt = descGo.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0, 0);
        dRt.anchorMax = new Vector2(0.70f, 0.60f);
        dRt.offsetMin = new Vector2(14, 6); dRt.offsetMax = new Vector2(0, 0);
        var dTmp = descGo.GetComponent<TextMeshProUGUI>();

        string buffLine = "";
        if (data.bonusHp > 0) buffLine += $" +{data.bonusHp} HP";
        if (data.bonusAtk > 0) buffLine += $" +{data.bonusAtk} ATK";
        if (data.bonusDefPercent > 0) buffLine += $" +{data.bonusDefPercent:0}% DEF";

        dTmp.text = $"{data.GetLocalizedDesc()}\n<color=#66FF88><b>{buffLine}</b></color>";
        dTmp.fontSize = 15f;
        dTmp.alignment = TextAlignmentOptions.TopLeft;
        dTmp.color = new Color(0.80f, 0.85f, 0.90f, 1f);

        // 3. Action Button (MẶC / ĐANG MẶC / MỞ KHÓA)
        GameObject btnGo = new GameObject("Btn_Action", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(card.transform, false);
        RectTransform bRt = btnGo.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.72f, 0.5f);
        bRt.anchorMax = new Vector2(0.98f, 0.5f);
        bRt.sizeDelta = new Vector2(0, 42);
        bRt.anchoredPosition = new Vector2(0, 0);

        var bImg = btnGo.GetComponent<Image>();
        var bBtn = btnGo.GetComponent<Button>();

        GameObject btnTxtGo = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        RectTransform btRt = btnTxtGo.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one;
        var btTmp = btnTxtGo.GetComponent<TextMeshProUGUI>();
        btTmp.fontSize = 16.5f;
        btTmp.fontStyle = FontStyles.Bold;
        btTmp.alignment = TextAlignmentOptions.Center;

        if (isEquipped)
        {
            bImg.color = new Color(0.18f, 0.55f, 0.28f, 1f);
            btTmp.text = LocalizationManager.T("ĐANG MẶC", "EQUIPPED");
            btTmp.color = Color.white;
            bBtn.interactable = false;
        }
        else if (isUnlocked)
        {
            bImg.color = new Color(0.18f, 0.45f, 0.75f, 1f);
            btTmp.text = LocalizationManager.T("MẶC NGAY", "EQUIP");
            btTmp.color = Color.white;
            bBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HeroCostumeManager.EquipCostume(currentHeroId, data.id);
                previewingCostumeId = data.id;
                RefreshUI();
            });
        }
        else
        {
            long shards = AccountDatabase.GetAethelShards();
            bool canAfford = (shards >= data.priceShards);
            bImg.color = canAfford ? new Color(0.85f, 0.55f, 0.15f, 1f) : new Color(0.40f, 0.40f, 0.45f, 1f);
            btTmp.text = $"{data.priceShards:N0} 💎";
            btTmp.color = Color.white;
            bBtn.onClick.AddListener(() =>
            {
                if (HeroCostumeManager.UnlockCostume(data.id))
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayCoin();
                    HeroCostumeManager.EquipCostume(currentHeroId, data.id);
                    previewingCostumeId = data.id;
                    RefreshUI();
                }
                else
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonBack();
                }
            });
        }
    }
}
