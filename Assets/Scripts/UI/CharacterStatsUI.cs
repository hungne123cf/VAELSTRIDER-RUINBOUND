using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bảng chỉ số chi tiết nhân vật & kỹ năng (Character Stats & Skill Details).
/// Hỗ trợ chuyển đổi ngôn ngữ linh hoạt 100% Tiếng Việt / English.
/// </summary>
public class CharacterStatsUI : MonoBehaviour
{
    public static CharacterStatsUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI characterTitleText;
    [SerializeField] private TextMeshProUGUI characterSubtitleText;

    // 4 Mini Stat Badges
    [SerializeField] private TextMeshProUGUI hpStatText;
    [SerializeField] private TextMeshProUGUI atkStatText;
    [SerializeField] private TextMeshProUGUI dashCdStatText;
    [SerializeField] private TextMeshProUGUI ultiCdStatText;

    // 3 Skill & Trait Detail Cards
    [SerializeField] private TextMeshProUGUI attackCardTitle;
    [SerializeField] private TextMeshProUGUI attackCardDesc;
    [SerializeField] private TextMeshProUGUI dashCardTitle;
    [SerializeField] private TextMeshProUGUI dashCardDesc;
    [SerializeField] private TextMeshProUGUI ultiCardTitle;
    [SerializeField] private TextMeshProUGUI ultiCardDesc;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LocalizationManager.OnLanguageChanged += RefreshData;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshData;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Toggle();
        }
        else if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public bool IsOpen() => gameObject.activeSelf;

    public void Toggle()
    {
        if (gameObject.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        Transform dBox = transform.Find("DialogBox");
        if (dBox != null)
        {
            RectTransform dRt = dBox.GetComponent<RectTransform>();
            Transform closeBtn = dBox.Find("Btn_Close");
            if ((dRt != null && dRt.sizeDelta != Vector2.zero) || closeBtn == null)
            {
                // Old layout without full-screen or missing close button: rebuild
                Destroy(gameObject);
                var freshUi = EnsureUIExists();
                if (freshUi != null) freshUi.Open();
                return;
            }
        }

        GameAutoBootstrap.SetGameplayHUDVisible(false);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        RefreshData();
        Time.timeScale = 0f;
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        GameAutoBootstrap.SetGameplayHUDVisible(true);
        Time.timeScale = 1f;
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void RefreshData()
    {
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        var charMgr = CharacterManager.EnsureInstance();

        string selectedCharId = charMgr != null ? charMgr.GetSelectedCharacterId() : "Vaelstrider";
        bool isAzure = (selectedCharId == CharacterManager.CHAR_AZURE_BLADE);
        bool isMaid = (selectedCharId == CharacterManager.CHAR_MAID);
        bool isVi = LocalizationManager.IsVietnamese;

        int atkBonus = upgradeMgr != null ? upgradeMgr.GetAttackBonus() : 0;
        int hpBonus = upgradeMgr != null ? upgradeMgr.GetHealthBonus() : 0;
        float dashCdRed = upgradeMgr != null ? upgradeMgr.GetDashCooldownReduction() : 0f;
        float ultiCdRed = upgradeMgr != null ? upgradeMgr.GetUltiCooldownReduction() : 0f;
        int ultiDmgBonus = upgradeMgr != null ? upgradeMgr.GetUltiDamageBonus() : 0;

        // Gem Socket Bonuses
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out int sAtk, out int sHp, out int sDef, out float sLifesteal, out float sCd, out float sShard, out float sRegen);

        int totalAtk = (isAzure ? 6 : 5) + atkBonus + sAtk;
        int totalHp = (isAzure ? 110 : 100) + hpBonus + sHp;
        float finalDashCd = Mathf.Max(1.0f, 5.0f - dashCdRed);
        float finalUltiCd = Mathf.Max(5.0f, (isAzure ? 18.0f : 30.0f) - ultiCdRed - (30.0f * sCd));

        // Header Title
        if (characterTitleText != null)
        {
            if (isAzure)
                characterTitleText.text = isVi ? "LAM KIẾM SĨ (AZURE BLADE)" : "AZURE BLADE (SWORDSWOMAN)";
            else if (isMaid)
                characterTitleText.text = isVi ? "HẦU GÁI CHIẾN BINH (ELYSIA)" : "WARRIOR MAID (ELYSIA)";
            else
                characterTitleText.text = isVi ? "HIỆP SĨ VAELSTRIDER" : "KNIGHT VAELSTRIDER";
        }
        if (characterSubtitleText != null)
        {
            if (isAzure)
                characterSubtitleText.text = isVi ? "Phong cách: Trọng Kiếm Khí | Chém chậm xuất Sóng Kiếm xuyên quái & Bão kiếm nan quạt" : "Style: Heavy Crescent | Slow Slashes with Piercing Waves & Fan Spread Tempest";
            else if (isMaid)
                characterSubtitleText.text = isVi ? "Phong cách: Tuyệt kỹ Trăng Khuyết | Trảm sát tức thì & Khống chế Boss" : "Style: Crescent Slash | Instant Execute & Boss Stun";
            else
                characterSubtitleText.text = isVi ? "Phong cách: Kiếm Sĩ Cổ Tộc | Đột kích liên hoàn & Bão kiếm quét rộng" : "Style: Ancient Blade | Multi-slash Combo & Area Storm";
        }

        // 4 Quick Stats Badges (To, ro rang, khong thua chu)
        if (hpStatText != null)
        {
            string label = isVi ? "MÁU (HP)" : "HEALTH (HP)";
            hpStatText.text = $"{label}\n<color=#55FF88><size=18><b>{totalHp}</b></size></color>";
        }
        if (atkStatText != null)
        {
            string label = isVi ? "TẤN CÔNG (ATK)" : "ATTACK (ATK)";
            atkStatText.text = $"{label}\n<color=#FF5566><size=18><b>{totalAtk}</b></size></color>";
        }
        if (dashCdStatText != null)
        {
            string label = isVi ? "HỒI LƯỚT" : "DASH CD";
            dashCdStatText.text = $"{label}\n<color=#44DDFF><size=18><b>{finalDashCd:F1}s</b></size></color>";
        }
        if (ultiCdStatText != null)
        {
            string label = isVi ? "HỒI TUYỆT KỸ" : "ULTI CD";
            ultiCdStatText.text = $"{label}\n<color=#FFAA33><size=18><b>{finalUltiCd:F1}s</b></size></color>";
        }

        // Card 1: Attack
        if (attackCardTitle != null)
        {
            attackCardTitle.text = isVi ? "1. ĐÒN ĐÁNH CƠ BẢN" : "1. BASIC ATTACK";
        }
        if (attackCardDesc != null)
        {
            attackCardDesc.text = isVi
                ? $"• <b>Sát thương:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> (Tầm đánh: 1.35m)\n" +
                  $"• <b>Hiệu ứng:</b> Đánh trúng hồi <color=#38BDF8><b>+8 Năng Lượng</b></color>"
                : $"• <b>Damage:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> (Range: 1.35m)\n" +
                  $"• <b>Effect:</b> Restores <color=#38BDF8><b>+8 Energy</b></color> on hit";
        }

        // Card 2: Dash
        if (dashCardTitle != null)
        {
            dashCardTitle.text = isVi ? "2. LƯỚT NÉ ĐÒN" : "2. DASH & DODGE";
        }
        if (dashCardDesc != null)
        {
            dashCardDesc.text = isVi
                ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                  $"• <b>Đặc tính:</b> Lướt nhanh và <color=#55FF88><b>bất tử 0.2s</b></color> né mọi sát thương"
                : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                  $"• <b>Trait:</b> Dash forward with <color=#55FF88><b>0.2s Invincibility</b></color>";
        }

        // Card 3: Ulti
        if (ultiCardTitle != null)
        {
            ultiCardTitle.text = isMaid 
                ? (isVi ? "3. TUYỆT KỸ: QUÉT TẬN KẺ YẾU" : "3. ULTIMATE: SWEEP THE WEAK") 
                : (isVi ? "3. TUYỆT KỸ: CƠN LỐC TÀN TÍCH" : "3. ULTIMATE: RUIN WHIRLWIND");
        }
        if (ultiCardDesc != null)
        {
            if (isMaid)
            {
                ultiCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> <color=#FF4455><b>Kết liễu quái thường</b></color> | Gây choáng Boss 2.5s"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> <color=#FF4455><b>Insta-kill normal mobs</b></color> | Stun Boss 2.5s";
            }
            else
            {
                ultiCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Triệu hồi bão kiếm quét 4 đợt diện rộng, hút và dồn sát thương"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Summons 4-hit area whirlwind pulling and shredding enemies";
            }
        }

    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = 5;
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    public static CharacterStatsUI EnsureUIExists()
    {
        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) return null;

        Transform existingBtn = rootCanvas.transform.Find("Btn_CharacterStats");
        if (existingBtn != null)
        {
            Button btn = existingBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var ui = EnsureUIExists();
                    if (ui != null) ui.Open();
                });
            }
        }
        else
        {
            GameObject btnObj = CreateUIObject("Btn_CharacterStats", rootCanvas.transform);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-78f, -20f);
            btnRt.sizeDelta = new Vector2(50f, 50f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.94f);

            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.65f, 0.95f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                var ui = EnsureUIExists();
                if (ui != null) ui.Open();
            });

            GameObject txtObj = CreateUIObject("IconText", btnObj.transform);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "INFO";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.8f, 1f);
            tmp.raycastTarget = false;
        }

        Transform existingPanel = rootCanvas.transform.Find("CharacterStatsPanel");
        if (existingPanel != null)
        {
            DestroyImmediate(existingPanel.gameObject);
        }

        // Tạo Panel Character Stats (Full-Screen Edge-to-Edge Mobile Layout)
        GameObject panelObj = CreateUIObject("CharacterStatsPanel", rootCanvas.transform);
        RectTransform pRt = panelObj.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.pivot = new Vector2(0.5f, 0.5f);
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.04f, 0.08f, 0.96f);
        panelBg.raycastTarget = true;

        Canvas csCanvas = panelObj.AddComponent<Canvas>();
        csCanvas.overrideSorting = true;
        csCanvas.sortingOrder = 99995;
        panelObj.AddComponent<GraphicRaycaster>();

        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // Dialog Box inside - Edge to Edge Full Screen
        GameObject dialogBox = CreateUIObject("DialogBox", panelObj.transform);
        RectTransform dRt = dialogBox.GetComponent<RectTransform>();
        dRt.anchorMin = Vector2.zero;
        dRt.anchorMax = Vector2.one;
        dRt.pivot = new Vector2(0.5f, 0.5f);
        dRt.offsetMin = Vector2.zero;
        dRt.offsetMax = Vector2.zero;

        Image dImg = dialogBox.AddComponent<Image>();
        dImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);
        var dOutline = dialogBox.AddComponent<Outline>();
        dOutline.effectColor = new Color(0.25f, 0.65f, 0.95f, 0.85f);
        dOutline.effectDistance = new Vector2(2f, -2f);

        var uiComp = panelObj.AddComponent<CharacterStatsUI>();

        // Top-Right Close Button [X] (Touch-Friendly Mobile Button)
        GameObject closeBtnObj = CreateUIObject("Btn_Close", dialogBox.transform);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-25f, -22f);
        cRt.sizeDelta = new Vector2(52f, 52f);

        Image cImg = closeBtnObj.AddComponent<Image>();
        cImg.color = new Color(0.75f, 0.22f, 0.25f, 1f);
        var cOutline = closeBtnObj.AddComponent<Outline>();
        cOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.7f);

        Button cBtn = closeBtnObj.AddComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.onClick.AddListener(uiComp.Close);

        GameObject cTxtObj = CreateUIObject("Text", closeBtnObj.transform);
        RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI cTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        cTmp.text = "<b>X</b>";
        cTmp.fontSize = 26;
        cTmp.fontStyle = FontStyles.Bold;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;

        // Header Title
        GameObject headerObj = CreateUIObject("HeaderTitle", dialogBox.transform);
        RectTransform hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -20f);
        hRt.sizeDelta = new Vector2(-160f, 38f);

        uiComp.characterTitleText = headerObj.AddComponent<TextMeshProUGUI>();
        uiComp.characterTitleText.fontSize = 28;
        uiComp.characterTitleText.fontStyle = FontStyles.Bold;
        uiComp.characterTitleText.alignment = TextAlignmentOptions.Center;
        uiComp.characterTitleText.color = new Color(1f, 0.85f, 0.35f);

        // Subtitle
        GameObject subObj = CreateUIObject("HeaderSubtitle", dialogBox.transform);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -60f);
        sRt.sizeDelta = new Vector2(-160f, 26f);

        uiComp.characterSubtitleText = subObj.AddComponent<TextMeshProUGUI>();
        uiComp.characterSubtitleText.fontSize = 15;
        uiComp.characterSubtitleText.alignment = TextAlignmentOptions.Center;
        uiComp.characterSubtitleText.color = new Color(0.6f, 0.85f, 1f);

        // 4 Badges Container (Stretches across top under subtitle)
        GameObject badgesRow = CreateUIObject("BadgesRow", dialogBox.transform);
        RectTransform bRowRt = badgesRow.GetComponent<RectTransform>();
        bRowRt.anchorMin = new Vector2(0f, 1f);
        bRowRt.anchorMax = new Vector2(1f, 1f);
        bRowRt.pivot = new Vector2(0.5f, 1f);
        bRowRt.anchoredPosition = new Vector2(0f, -95f);
        bRowRt.sizeDelta = new Vector2(-60f, 66f);

        var hlg = badgesRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        uiComp.hpStatText = CreateMiniBadge(badgesRow.transform, "HP", new Color(0.12f, 0.28f, 0.18f, 0.95f));
        uiComp.atkStatText = CreateMiniBadge(badgesRow.transform, "ATK", new Color(0.32f, 0.14f, 0.16f, 0.95f));
        uiComp.dashCdStatText = CreateMiniBadge(badgesRow.transform, "DashCD", new Color(0.12f, 0.22f, 0.32f, 0.95f));
        uiComp.ultiCdStatText = CreateMiniBadge(badgesRow.transform, "UltiCD", new Color(0.32f, 0.24f, 0.12f, 0.95f));

        // 3 Cards Container (Fills the remaining screen area)
        GameObject cardsSection = CreateUIObject("CardsSection", dialogBox.transform);
        RectTransform csRt = cardsSection.GetComponent<RectTransform>();
        csRt.anchorMin = new Vector2(0f, 0f);
        csRt.anchorMax = new Vector2(1f, 1f);
        csRt.pivot = new Vector2(0.5f, 0.5f);
        csRt.offsetMin = new Vector2(30f, 22f);
        csRt.offsetMax = new Vector2(-30f, -172f);

        var vlg = cardsSection.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        Sprite sprAttack = LoadSprite("Assets/Sprites/UI/Attack.png", "UI/Attack");
        Sprite sprDash = LoadSprite("Assets/Sprites/UI/Dash.png", "UI/Dash");
        Sprite sprUlti = LoadSprite("Assets/Sprites/UI/Ulti.png", "UI/Ulti");

        CreateSkillCard(cardsSection.transform, "Card_Attack", "1. ĐÒN ĐÁNH CƠ BẢN", sprAttack, new Color(1f, 0.45f, 0.25f), out uiComp.attackCardDesc, out uiComp.attackCardTitle);
        CreateSkillCard(cardsSection.transform, "Card_Dash", "2. LƯỚT NÉ ĐÒN", sprDash, new Color(0.2f, 0.95f, 0.65f), out uiComp.dashCardDesc, out uiComp.dashCardTitle);
        CreateSkillCard(cardsSection.transform, "Card_Ulti", "3. TUYỆT KỸ", sprUlti, new Color(0.75f, 0.5f, 1f), out uiComp.ultiCardDesc, out uiComp.ultiCardTitle);

        panelObj.SetActive(false);
        return uiComp;
    }

    private static Sprite LoadSprite(string assetPath, string resourcePath)
    {
        Sprite spr = null;
#if UNITY_EDITOR
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
        if (spr == null) spr = Resources.Load<Sprite>(resourcePath);
        return spr;
    }

    private static TextMeshProUGUI CreateMiniBadge(Transform parent, string id, Color bgColor)
    {
        GameObject badgeObj = CreateUIObject($"Badge_{id}", parent);
        Image bg = badgeObj.AddComponent<Image>();
        bg.color = bgColor;

        var outline = badgeObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.7f, 1f, 0.45f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject txtObj = CreateUIObject("Text", badgeObj.transform);
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 13.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.lineSpacing = 10;
        return tmp;
    }

    private static void CreateSkillCard(Transform parent, string name, string title, Sprite iconSpr, Color accentCol, out TextMeshProUGUI descTmp, out TextMeshProUGUI titleTmp)
    {
        GameObject cardObj = CreateUIObject(name, parent);
        Image bg = cardObj.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.13f, 0.20f, 0.96f);

        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(accentCol.r, accentCol.g, accentCol.b, 0.65f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        float leftPad = 25f;
        if (iconSpr != null)
        {
            GameObject iconBadgeObj = CreateUIObject("IconBadge", cardObj.transform);
            RectTransform ibRt = iconBadgeObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(25f, 0f);
            ibRt.sizeDelta = new Vector2(76f, 76f);

            Image ibBg = iconBadgeObj.AddComponent<Image>();
            ibBg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
            var ibOutline = iconBadgeObj.AddComponent<Outline>();
            ibOutline.effectColor = accentCol;
            ibOutline.effectDistance = new Vector2(2f, -2f);

            GameObject iconInner = CreateUIObject("Icon", iconBadgeObj.transform);
            RectTransform iiRt = iconInner.GetComponent<RectTransform>();
            iiRt.anchorMin = new Vector2(0.5f, 0.5f);
            iiRt.anchorMax = new Vector2(0.5f, 0.5f);
            iiRt.pivot = new Vector2(0.5f, 0.5f);
            iiRt.anchoredPosition = Vector2.zero;
            iiRt.sizeDelta = new Vector2(62f, 62f);

            Image iiImg = iconInner.AddComponent<Image>();
            iiImg.sprite = iconSpr;
            iiImg.preserveAspect = true;
            iiImg.raycastTarget = false;

            leftPad = 125f;
        }

        GameObject tObj = CreateUIObject("Title", cardObj.transform);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(leftPad, -16f);
        tRt.sizeDelta = new Vector2(-(leftPad + 25f), 32f);

        titleTmp = tObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = 22f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.88f, 0.38f);

        GameObject dObj = CreateUIObject("Desc", cardObj.transform);
        RectTransform dRt = dObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.anchoredPosition = new Vector2(leftPad, -52f);
        dRt.sizeDelta = new Vector2(-(leftPad + 25f), -60f);

        descTmp = dObj.AddComponent<TextMeshProUGUI>();
        descTmp.fontSize = 17f;
        descTmp.lineSpacing = 16;
        descTmp.color = new Color(0.90f, 0.94f, 1f);
        descTmp.enableWordWrapping = true;
    }
}
