// ========================================================
// VAELSTRIDER: RUINBOUND - In-Game Battle Skill Gem Socketing UI
// Cho phép mở túi đồ và khảm / tháo ngọc trực tiếp vào 3 Chiêu Thức trong trận đấu!
// (1. Đòn Đánh Thường, 2. Lướt Tốc Biến, 3. Tuyệt Kỹ)
// ========================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameInventoryUI : MonoBehaviour
{
    public static InGameInventoryUI Instance { get; private set; }

    [Header("UI Roots")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Skill Sockets View")]
    [SerializeField] private TextMeshProUGUI totalBuffsText;
    [SerializeField] private Transform socketsContainer;

    [Header("Bag Grid")]
    [SerializeField] private Transform bagContentContainer;
    [SerializeField] private TextMeshProUGUI bagSectionHeaderText;
    [SerializeField] private TextMeshProUGUI lootSummaryText;
    [SerializeField] private TextMeshProUGUI topShardsBadgeText;
    [SerializeField] private Button autoMergeAllBtn;
    [SerializeField] private TextMeshProUGUI autoMergeAllBtnText;

    private List<GameObject> socketSlotObjs = new List<GameObject>();
    private List<GameObject> bagItemCardObjs = new List<GameObject>();
    private int selectedSocketSlotIndex = 0;
    private Sprite sprSlotFrame = null;

    void Awake()
    {
        Instance = this;
        LoadAssets();
    }

    void Start()
    {
        LocalizationManager.OnLanguageChanged += RefreshModal;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshModal;
    }

    private void LoadAssets()
    {
        if (sprSlotFrame == null)
        {
            sprSlotFrame = Resources.Load<Sprite>("UI/item_slot_frame");
#if UNITY_EDITOR
            if (sprSlotFrame == null)
                sprSlotFrame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/item_slot_frame.png");
#endif
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            Toggle();
        }
        else if (modalPanel != null && modalPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public bool IsOpen()
    {
        return modalPanel != null && modalPanel.activeSelf;
    }

    public void Toggle()
    {
        if (modalPanel != null && modalPanel.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        GameAutoBootstrap.SetGameplayHUDVisible(false);
        if (modalPanel != null)
        {
            Transform oldResume = modalPanel.transform.Find("DialogBox/FooterBar/Btn_Resume");
            if (oldResume != null) Destroy(oldResume.gameObject);
            modalPanel.SetActive(true);
            modalPanel.transform.SetAsLastSibling();
        }
        Time.timeScale = 0f;
        RefreshModal();
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void Close()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
        GameAutoBootstrap.SetGameplayHUDVisible(true);
        Time.timeScale = 1f;
        NotifyPlayerCombatStatsChanged();
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void RefreshModal()
    {
        if (modalPanel == null || !modalPanel.activeSelf) return;

        bool isVi = LocalizationManager.IsVietnamese;

        // Header Title
        if (titleText != null)
        {
            titleText.text = isVi ? "KHẢM NGỌC 3 CHIÊU THỨC TRẬN ĐẤU" : "BATTLE SKILL GEM INSCRIPTION";
        }
        if (subtitleText != null)
        {
            subtitleText.text = isVi
                ? "Kéo thả ngọc vào 3 Chiêu Thức bên dưới để kích hoạt hiệu ứng chiến đấu tức thì!"
                : "Drag & drop gems into the 3 Skill Slots below for instant battle buffs!";
        }

        // Live Total Skill Socket Bonuses
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out int sAtk, out int sHp, out int sDef, out float sLifesteal, out float sCd, out float sShard, out float sRegen);
        List<string> buffTags = new List<string>();
        if (sAtk > 0) buffTags.Add($"<color=#FFCC00>+{sAtk} ATK</color>");
        if (sHp > 0) buffTags.Add($"<color=#22C55E>+{sHp} HP</color>");
        if (sDef > 0) buffTags.Add($"<color=#38BDF8>+{sDef} DEF</color>");
        if (sLifesteal > 0) buffTags.Add($"<color=#F43F5E>+{Mathf.RoundToInt(sLifesteal * 100)}% Hút Máu</color>");
        if (sCd > 0) buffTags.Add($"<color=#C084FC>-{Mathf.RoundToInt(sCd * 100)}% CD</color>");
        if (sShard > 0) buffTags.Add($"<color=#FBBF24>+{Mathf.RoundToInt(sShard * 100)}% Shard</color>");
        if (sRegen > 0) buffTags.Add($"<color=#10B981>+{sRegen} HP/s</color>");

        if (totalBuffsText != null)
        {
            totalBuffsText.text = (buffTags.Count > 0)
                ? $"{LocalizationManager.T("Tổng Buff 3 Chiêu:", "Total Active Buffs:")} {string.Join(" | ", buffTags)}"
                : LocalizationManager.T("<color=#888888>Chưa khảm ngọc vào chiêu thức. Hãy kéo ngọc bên dưới vào ô để tăng lực đánh / hồi chiêu!</color>",
                                        "<color=#888888>No skill gems socketed. Drag gems below into skills to boost stats & cooldowns!</color>");
        }

        // Update Auto Merge All Button
        int mergeableCount = EquipmentSocketManager.GetTotalAvailableMergeGemsCount();
        if (autoMergeAllBtnText != null)
        {
            if (mergeableCount > 0)
            {
                string compactMerge = MainMenuManager.FormatCompactNumber(mergeableCount);
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

        // Render 3 Skill Sockets
        RenderSkillSockets();

        // Render Bag Gems & Items
        RenderBagGems();

        // Ensure Top Shards Badge Exists
        EnsureTopShardsBadge();

        // Bottom Summary & Top Shards Badge
        long shards = AccountDatabase.GetAethelShards();
        string shardStr = MainMenuManager.FormatCompactNumber(shards);
        int gold = PlayerPrefs.GetInt("PlayerGold", 0);

        if (topShardsBadgeText != null)
        {
            topShardsBadgeText.text = $"<color=#00FFFF>SHARDS: {shardStr}</color>";
        }

        if (lootSummaryText != null)
        {
            lootSummaryText.text = $"<color=#00FFFF>Aethel Shards: {shardStr}</color>";
        }
    }

    private void EnsureTopShardsBadge()
    {
        if (topShardsBadgeText != null) return;
        if (modalPanel == null) return;

        Transform dBox = modalPanel.transform.Find("DialogBox");
        if (dBox == null) dBox = modalPanel.transform;

        Transform existingBadge = dBox.Find("TopShardsBadge");
        if (existingBadge != null)
        {
            topShardsBadgeText = existingBadge.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        GameObject sBadgeObj = new GameObject("TopShardsBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sBadgeObj.layer = 5;
        sBadgeObj.transform.SetParent(dBox, false);
        RectTransform sbrt = sBadgeObj.GetComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(0f, 1f);
        sbrt.anchorMax = new Vector2(0f, 1f);
        sbrt.pivot = new Vector2(0f, 1f);
        sbrt.anchoredPosition = new Vector2(35f, -20f);
        sbrt.sizeDelta = new Vector2(210f, 38f);
        Image sbImg = sBadgeObj.GetComponent<Image>();
        sbImg.color = new Color(0.06f, 0.12f, 0.22f, 0.95f);
        var sbOutline = sBadgeObj.AddComponent<Outline>();
        sbOutline.effectColor = new Color(0f, 0.85f, 1f, 0.7f);
        sbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject sbtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        sbtObj.layer = 5;
        sbtObj.transform.SetParent(sBadgeObj.transform, false);
        RectTransform sbttRt = sbtObj.GetComponent<RectTransform>();
        sbttRt.anchorMin = Vector2.zero;
        sbttRt.anchorMax = Vector2.one;
        sbttRt.sizeDelta = Vector2.zero;
        topShardsBadgeText = sbtObj.AddComponent<TextMeshProUGUI>();
        topShardsBadgeText.fontSize = 15;
        topShardsBadgeText.fontStyle = FontStyles.Bold;
        topShardsBadgeText.alignment = TextAlignmentOptions.Center;
        topShardsBadgeText.color = new Color(0.2f, 0.95f, 1f);
    }

    private void NotifyPlayerCombatStatsChanged()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
        if (player != null) player.RefreshEquipmentBonuses();
        var health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
        if (health != null) health.RefreshSocketHealthBonus();
        if (CharacterStatsUI.Instance != null && CharacterStatsUI.Instance.gameObject.activeSelf)
        {
            CharacterStatsUI.Instance.RefreshData();
        }
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

    private void RenderSkillSockets()
    {
        if (socketsContainer == null) return;

        foreach (var o in socketSlotObjs) if (o != null) Destroy(o);
        socketSlotObjs.Clear();

        bool isVi = LocalizationManager.IsVietnamese;
        int skillCount = EquipmentSocketManager.SKILL_COUNT;
        int subCount = EquipmentSocketManager.SUB_SOCKETS_PER_SKILL;

        float cardW = 460f;
        float cardH = 240f;
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
            cardObj.transform.SetParent(socketsContainer, false);
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
                selectedSocketSlotIndex = skillIdx;
                RefreshModal();
                SoundManager.EnsureInstance()?.PlayMenuHover();
            });

            // Card Drop Target (drop on entire skill card)
            var cardDrop = cardObj.AddComponent<UIDropSocketTarget>();
            cardDrop.socketSlotIndex = skillIdx;
            cardDrop.onGemDropped = (droppedGemId, targetSlot) =>
            {
                if (EquipmentSocketManager.SocketGemToSkill(targetSlot, -1, droppedGemId, out bool leveledUp, out int resLv))
                {
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    NotifyPlayerCombatStatsChanged();
                    RefreshModal();
                }
            };

            // 1. Skill Header Container
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
            subTmp.text = isVi ? "3 Ô Khảm • Ghép trùng ngọc để Lên Cấp" : "3 Sockets • Merge duplicates to level up";
            subTmp.fontSize = 10.5f;
            subTmp.color = new Color(0.65f, 0.82f, 0.95f, 0.85f);
            subTmp.alignment = TextAlignmentOptions.Left;

            // 3. Middle: 3 Sub-Socket Boxes Row
            GameObject subRowObj = new GameObject("SubSocketsRow", typeof(RectTransform), typeof(CanvasRenderer));
            subRowObj.layer = 5;
            subRowObj.transform.SetParent(cardObj.transform, false);
            RectTransform srRt = subRowObj.GetComponent<RectTransform>();
            srRt.anchorMin = new Vector2(0.5f, 0.5f);
            srRt.anchorMax = new Vector2(0.5f, 0.5f);
            srRt.pivot = new Vector2(0.5f, 0.5f);
            srRt.anchoredPosition = new Vector2(0f, 4f);
            srRt.sizeDelta = new Vector2(cardW - 16f, 82f);

            float subBoxW = 108f;
            float subBoxH = 96f;
            float subSpacing = 12f;
            float subStartX = -((subCount - 1) * (subBoxW + subSpacing)) / 2f;

            for (int s = 0; s < subCount; s++)
            {
                int subIdx = s;
                bool hasGem = EquipmentSocketManager.GetSkillSubSocketGem(skillIdx, subIdx, out string gemId, out int gemLevel);

                GameObject subBoxObj = new GameObject($"SubSocket_{subIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                subBoxObj.layer = 5;
                subBoxObj.transform.SetParent(subRowObj.transform, false);

                RectTransform sbRt = subBoxObj.GetComponent<RectTransform>();
                sbRt.anchoredPosition = new Vector2(subStartX + s * (subBoxW + subSpacing), 0f);
                sbRt.sizeDelta = new Vector2(subBoxW, subBoxH);

                Image sbImg = subBoxObj.GetComponent<Image>();
                if (sprSlotFrame != null) { sbImg.sprite = sprSlotFrame; sbImg.type = Image.Type.Sliced; }

                var sbOutline = subBoxObj.AddComponent<Outline>();
                Button sbBtn = subBoxObj.GetComponent<Button>();
                sbBtn.targetGraphic = sbImg;

                // Unconditional drop target for this specific sub-slot
                var subDrop = subBoxObj.AddComponent<UIDropSocketTarget>();
                subDrop.socketSlotIndex = skillIdx;
                subDrop.subSlotIndex = subIdx;
                subDrop.onGemDroppedDetailed = (droppedGemId, targetSlot, targetSub) =>
                {
                    if (EquipmentSocketManager.SocketGemToSkill(targetSlot, targetSub, droppedGemId, out bool leveledUp, out int resLv))
                    {
                        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                        NotifyPlayerCombatStatsChanged();
                        RefreshModal();
                    }
                };

                if (!hasGem)
                {
                    // Empty Sub-Slot
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
                    pTmp.text = $"<color=#557799><size=15>+</size>\n<size=8>Ô {subIdx + 1}</size></color>";
                    pTmp.alignment = TextAlignmentOptions.Center;
                    pTmp.raycastTarget = false;
                }
                else
                {
                    // Filled Sub-Slot with Gem & Level Badge
                    var gemDef = InventoryItemDatabase.GetItem(gemId);
                    var gemEff = EquipmentSocketManager.GetGemEffect(gemId);
                    Color gemCol = gemEff.HasValue ? gemEff.Value.gemColor : Color.white;

                    sbImg.color = new Color(gemCol.r * 0.3f, gemCol.g * 0.3f, gemCol.b * 0.3f, 0.95f);
                    sbOutline.effectColor = gemCol;
                    sbOutline.effectDistance = new Vector2(1.5f, -1.5f);

                    // Gem Icon
                    GameObject gIcon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    gIcon.layer = 5;
                    gIcon.transform.SetParent(subBoxObj.transform, false);
                    RectTransform gicRt = gIcon.GetComponent<RectTransform>();
                    gicRt.anchorMin = new Vector2(0.5f, 0.5f);
                    gicRt.anchorMax = new Vector2(0.5f, 0.5f);
                    gicRt.pivot = new Vector2(0.5f, 0.5f);
                    gicRt.anchoredPosition = new Vector2(0f, 4f);
                    gicRt.sizeDelta = new Vector2(44f, 44f);
                    Image gicImg = gIcon.GetComponent<Image>();
                    gicImg.sprite = gemDef != null ? gemDef.Icon : null;
                    gicImg.preserveAspect = true;
                    gicImg.raycastTarget = false;

                    // Mini Level Tag on Bottom
                    GameObject lvObj = new GameObject("LvTag", typeof(RectTransform), typeof(CanvasRenderer));
                    lvObj.layer = 5;
                    lvObj.transform.SetParent(subBoxObj.transform, false);
                    RectTransform lvRt = lvObj.GetComponent<RectTransform>();
                    lvRt.anchorMin = new Vector2(0f, 0f);
                    lvRt.anchorMax = new Vector2(1f, 0f);
                    lvRt.pivot = new Vector2(0.5f, 0f);
                    lvRt.anchoredPosition = new Vector2(0f, 2f);
                    lvRt.sizeDelta = new Vector2(-4f, 18f);
                    var lvTmp = lvObj.AddComponent<TextMeshProUGUI>();
                    string lvColor = (gemLevel == 1) ? "#FFFFFF"
                                   : (gemLevel == 2) ? "#00FFFF"
                                   : (gemLevel == 3) ? "#FFCC00"
                                   : (gemLevel == 4) ? "#FF00FF"
                                   : "#FF3366";
                    lvTmp.text = $"<size=11><color={lvColor}><b>Lv.{gemLevel}</b></color></size>";
                    lvTmp.alignment = TextAlignmentOptions.Center;
                    lvTmp.raycastTarget = false;

                    // Mini Unsocket Button [x] in Top Right Corner
                    GameObject unBtnObj = new GameObject("Btn_X", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    unBtnObj.layer = 5;
                    unBtnObj.transform.SetParent(subBoxObj.transform, false);
                    RectTransform unRt = unBtnObj.GetComponent<RectTransform>();
                    unRt.anchorMin = new Vector2(1f, 1f);
                    unRt.anchorMax = new Vector2(1f, 1f);
                    unRt.pivot = new Vector2(1f, 1f);
                    unRt.anchoredPosition = new Vector2(2f, 2f);
                    unRt.sizeDelta = new Vector2(16f, 16f);
                    Image unImg = unBtnObj.GetComponent<Image>();
                    unImg.color = new Color(0.8f, 0.2f, 0.25f, 0.95f);
                    Button unBtn = unBtnObj.GetComponent<Button>();
                    unBtn.targetGraphic = unImg;
                    unBtn.onClick.AddListener(() =>
                    {
                        EquipmentSocketManager.UnsocketSubGem(skillIdx, subIdx);
                        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                        NotifyPlayerCombatStatsChanged();
                        RefreshModal();
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
                    utTmp.fontSize = 11f;
                    utTmp.fontStyle = FontStyles.Bold;
                    utTmp.alignment = TextAlignmentOptions.Center;
                    utTmp.color = Color.white;
                    utTmp.raycastTarget = false;

                    // Touch Drag-Out to Unsocket / Drag between skills
                    var sDrag = subBoxObj.AddComponent<UIDragDropItem>();
                    sDrag.itemId = gemId;
                    sDrag.itemIcon = gemDef != null ? gemDef.Icon : null;
                    sDrag.itemColor = gemCol;
                    sDrag.isSocketedGem = true;
                    sDrag.sourceSocketIndex = skillIdx;
                    sDrag.sourceSubSocketIndex = subIdx;
                    sDrag.onDragOutOfSocket = (unslotIdx) =>
                    {
                        EquipmentSocketManager.UnsocketSubGem(unslotIdx, subIdx);
                        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                        NotifyPlayerCombatStatsChanged();
                        RefreshModal();
                    };
                    sDrag.onMoveSubSocket = (fromSkill, fromSub, toSkill, toSub) =>
                    {
                        EquipmentSocketManager.MoveOrSwapSubGem(fromSkill, fromSub, toSkill, toSub);
                        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                        NotifyPlayerCombatStatsChanged();
                        RefreshModal();
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
            bsRt.sizeDelta = new Vector2(-12f, 22f);

            var bsTmp = buffSummaryObj.AddComponent<TextMeshProUGUI>();
            bsTmp.text = (skillBuffs.Count > 0)
                ? $"<b>Buff:</b> {string.Join("  ", skillBuffs)}"
                : (isVi ? "<color=#777777>[Trống]</color>" : "<color=#777777>[Empty]</color>");
            bsTmp.fontSize = 9.5f;
            bsTmp.alignment = TextAlignmentOptions.Center;
            bsTmp.enableWordWrapping = true;
        }
    }

    private void RenderBagGems()
    {
        if (bagContentContainer == null) return;

        foreach (var o in bagItemCardObjs) if (o != null) Destroy(o);
        bagItemCardObjs.Clear();

        if (bagSectionHeaderText != null)
        {
            bagSectionHeaderText.text = LocalizationManager.T(
                "VẬT PHẨM & KHOÁNG THẠCH TRONG TÚI (KÉO VÀO 3 CHIÊU TRÊN ĐỂ KHẢM)",
                "ITEMS, WEAPONS & GEMS IN BAG (DRAG INTO ANY OF THE 3 SKILLS TO SOCKET)");
        }

        var invItems = AccountDatabase.GetActiveUserInventory();
        List<AccountDatabase.InventoryItemEntry> ownedGems = new List<AccountDatabase.InventoryItemEntry>();
        foreach (var it in invItems)
        {
            if (it.quantity > 0 && EquipmentSocketManager.IsGem(it.itemId))
            {
                ownedGems.Add(it);
            }
        }

        if (ownedGems.Count == 0)
        {
            GameObject emptyMsgObj = new GameObject("EmptyBagMsg", typeof(RectTransform), typeof(CanvasRenderer));
            emptyMsgObj.layer = 5;
            emptyMsgObj.transform.SetParent(bagContentContainer, false);
            bagItemCardObjs.Add(emptyMsgObj);
            RectTransform emRt = emptyMsgObj.GetComponent<RectTransform>();
            emRt.sizeDelta = new Vector2(850f, 50f);
            var emTmp = emptyMsgObj.AddComponent<TextMeshProUGUI>();
            emTmp.text = LocalizationManager.T(
                "Chưa có kiếm, khiên hoặc khoáng thạch trong trận! Hãy diệt quái hoặc đập vỡ Rương Kho Báu để thu thập.",
                "No weapons, shields or minerals in bag yet! Defeat dungeon monsters or open Treasure Chests to gather items.");
            emTmp.fontSize = 12.5f;
            emTmp.color = new Color(0.7f, 0.75f, 0.85f);
            emTmp.alignment = TextAlignmentOptions.Center;
            return;
        }

        for (int i = 0; i < ownedGems.Count; i++)
        {
            var gemEntry = ownedGems[i];
            var gemDef = InventoryItemDatabase.GetItem(gemEntry.itemId);
            var gemEff = EquipmentSocketManager.GetGemEffect(gemEntry.itemId);
            if (gemDef == null || !gemEff.HasValue) continue;

            GameObject cardObj = new GameObject($"BagGemCard_{gemEntry.itemId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.layer = 5;
            cardObj.transform.SetParent(bagContentContainer, false);
            bagItemCardObjs.Add(cardObj);

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(210f, 95f);

            Image cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);

            var cOutline = cardObj.AddComponent<Outline>();
            cOutline.effectColor = gemEff.Value.gemColor * 0.8f;
            cOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Drag & Drop Component
            var dragItem = cardObj.AddComponent<UIDragDropItem>();
            dragItem.itemId = gemEntry.itemId;
            dragItem.itemIcon = gemDef.Icon;
            dragItem.itemColor = gemEff.Value.gemColor;
            dragItem.parentScrollRect = bagContentContainer.GetComponentInParent<ScrollRect>();
            string gemIdToSocket = gemEntry.itemId;
            dragItem.onClick = () =>
            {
                if (EquipmentSocketManager.SocketGemToSkill(selectedSocketSlotIndex, -1, gemIdToSocket, out _, out _))
                {
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    NotifyPlayerCombatStatsChanged();
                    RefreshModal();
                }
            };

            // Icon Box
            GameObject ibObj = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ibObj.layer = 5;
            ibObj.transform.SetParent(cardObj.transform, false);
            RectTransform ibRt = ibObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(8f, 0f);
            ibRt.sizeDelta = new Vector2(46f, 46f);
            Image ibImg = ibObj.GetComponent<Image>();
            if (sprSlotFrame != null) { ibImg.sprite = sprSlotFrame; ibImg.type = Image.Type.Sliced; }
            ibImg.color = gemEff.Value.gemColor;

            GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icObj.layer = 5;
            icObj.transform.SetParent(ibObj.transform, false);
            RectTransform icRt = icObj.GetComponent<RectTransform>();
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.sizeDelta = new Vector2(-8f, -8f);
            Image icImg = icObj.GetComponent<Image>();
            icImg.sprite = gemDef.Icon;
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;

            // Details (Right)
            GameObject infoObj = new GameObject("Info", typeof(RectTransform), typeof(CanvasRenderer));
            infoObj.layer = 5;
            infoObj.transform.SetParent(cardObj.transform, false);
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0f);
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.offsetMin = new Vector2(60f, 4f);
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
            string gName = LocalizationManager.IsVietnamese ? gemEff.Value.nameVi : gemEff.Value.nameEn;
            string qtyStr = gemEntry.quantity > 99 ? "99+" : $"x{gemEntry.quantity}";
            tTmp.text = $"<b>{gName}</b> <size=11><color=#00FF88>{qtyStr}</color></size>";
            tTmp.fontSize = 11.5f;
            tTmp.color = Color.white;
            tTmp.raycastTarget = false;

            // Buff Summary
            GameObject bObj = new GameObject("Buff", typeof(RectTransform), typeof(CanvasRenderer));
            bObj.layer = 5;
            bObj.transform.SetParent(infoObj.transform, false);
            var bTmp = bObj.AddComponent<TextMeshProUGUI>();
            bTmp.text = LocalizationManager.IsVietnamese ? gemEff.Value.effectSummaryVi : gemEff.Value.effectSummaryEn;
            bTmp.fontSize = 9.5f;
            bTmp.fontStyle = FontStyles.Bold;
            bTmp.color = gemEff.Value.gemColor;
            bTmp.enableWordWrapping = true;
            bTmp.raycastTarget = false;
        }
    }

    public static InGameInventoryUI EnsureUIExists(Canvas targetCanvas = null)
    {
        Canvas rootCanvas = targetCanvas != null ? targetCanvas : GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) return null;

        // 1. HUD Button: Btn_InGameBag
        Transform existingBtn = rootCanvas.transform.Find("Btn_InGameBag");
        if (existingBtn != null)
        {
            Button btn = existingBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var ui = Instance != null ? Instance : EnsureUIExists();
                    if (ui != null) ui.Toggle();
                });
            }
        }
        else
        {
            GameObject btnObj = new GameObject("Btn_InGameBag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.layer = 5;
            btnObj.transform.SetParent(rootCanvas.transform, false);

            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-136f, -20f);
            btnRt.sizeDelta = new Vector2(50f, 50f);

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.94f);

            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.3f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                var ui = Instance != null ? Instance : EnsureUIExists();
                if (ui != null) ui.Toggle();
            });

            GameObject txtObj = new GameObject("IconText", typeof(RectTransform), typeof(CanvasRenderer));
            txtObj.layer = 5;
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "BAG";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.85f, 0.35f);
            tmp.raycastTarget = false;
        }

        // 2. InGameBagPanel
        Transform existingPanel = rootCanvas.transform.Find("InGameInventoryModal");
        if (existingPanel != null)
        {
            var comp = existingPanel.GetComponent<InGameInventoryUI>();
            if (comp != null) return comp;
        }

        // Build Dialog Modal (100% Full-Screen Expansive Layout)
        GameObject modalObj = new GameObject("InGameInventoryModal", typeof(RectTransform), typeof(CanvasRenderer));
        modalObj.layer = 5;
        modalObj.transform.SetParent(rootCanvas.transform, false);

        RectTransform mRt = modalObj.GetComponent<RectTransform>();
        mRt.anchorMin = Vector2.zero;
        mRt.anchorMax = Vector2.one;
        mRt.pivot = new Vector2(0.5f, 0.5f);
        mRt.offsetMin = Vector2.zero;
        mRt.offsetMax = Vector2.zero;

        // Dedicated Canvas with High Sorting Order & Raycast Blocker
        Canvas modalCanvas = modalObj.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 99990;
        modalObj.AddComponent<GraphicRaycaster>();

        CanvasGroup modalCg = modalObj.AddComponent<CanvasGroup>();
        modalCg.blocksRaycasts = true;
        modalCg.interactable = true;

        // Dark Backdrop (100% Raycast Target)
        GameObject bgObj = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.layer = 5;
        bgObj.transform.SetParent(modalObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.96f);
        bgImg.raycastTarget = true;

        // Main Dialog Box - Edge to Edge
        GameObject dialogBox = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(modalObj.transform, false);
        RectTransform dRt = dialogBox.GetComponent<RectTransform>();
        dRt.anchorMin = Vector2.zero;
        dRt.anchorMax = Vector2.one;
        dRt.pivot = new Vector2(0.5f, 0.5f);
        dRt.offsetMin = Vector2.zero;
        dRt.offsetMax = Vector2.zero;

        Image dImg = dialogBox.GetComponent<Image>();
        dImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);

        var dOutline = dialogBox.AddComponent<Outline>();
        dOutline.effectColor = new Color(0.25f, 0.65f, 0.95f, 0.85f);
        dOutline.effectDistance = new Vector2(2f, -2f);

        var uiComp = modalObj.AddComponent<InGameInventoryUI>();
        uiComp.modalPanel = modalObj;

        // Header Title
        GameObject hTitleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer));
        hTitleObj.layer = 5;
        hTitleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform htRt = hTitleObj.GetComponent<RectTransform>();
        htRt.anchorMin = new Vector2(0f, 1f);
        htRt.anchorMax = new Vector2(1f, 1f);
        htRt.pivot = new Vector2(0.5f, 1f);
        htRt.anchoredPosition = new Vector2(0f, -22f);
        htRt.sizeDelta = new Vector2(0f, 36f);

        uiComp.titleText = hTitleObj.AddComponent<TextMeshProUGUI>();
        uiComp.titleText.text = LocalizationManager.T("LÒ KHẢM KHOÁNG THẠCH & ĐÁ QUÝ", "RUIN GEM & MINERAL FORGE");
        uiComp.titleText.fontSize = 24;
        uiComp.titleText.fontStyle = FontStyles.Bold;
        uiComp.titleText.alignment = TextAlignmentOptions.Center;
        uiComp.titleText.color = new Color(1f, 0.85f, 0.35f);

        // Top-Left Prominent Shards Badge
        GameObject sBadgeObj = new GameObject("TopShardsBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sBadgeObj.layer = 5;
        sBadgeObj.transform.SetParent(dialogBox.transform, false);
        RectTransform sbrt = sBadgeObj.GetComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(0f, 1f);
        sbrt.anchorMax = new Vector2(0f, 1f);
        sbrt.pivot = new Vector2(0f, 1f);
        sbrt.anchoredPosition = new Vector2(35f, -20f);
        sbrt.sizeDelta = new Vector2(210f, 38f);
        Image sbImg = sBadgeObj.GetComponent<Image>();
        sbImg.color = new Color(0.06f, 0.12f, 0.22f, 0.95f);
        var sbOutline = sBadgeObj.AddComponent<Outline>();
        sbOutline.effectColor = new Color(0f, 0.85f, 1f, 0.7f);
        sbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject sbtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        sbtObj.layer = 5;
        sbtObj.transform.SetParent(sBadgeObj.transform, false);
        RectTransform sbttRt = sbtObj.GetComponent<RectTransform>();
        sbttRt.anchorMin = Vector2.zero;
        sbttRt.anchorMax = Vector2.one;
        sbttRt.sizeDelta = Vector2.zero;
        uiComp.topShardsBadgeText = sbtObj.AddComponent<TextMeshProUGUI>();
        uiComp.topShardsBadgeText.fontSize = 15;
        uiComp.topShardsBadgeText.fontStyle = FontStyles.Bold;
        uiComp.topShardsBadgeText.alignment = TextAlignmentOptions.Center;
        uiComp.topShardsBadgeText.color = new Color(0.2f, 0.95f, 1f);

        // Subtitle
        GameObject subObj = new GameObject("HeaderSubtitle", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(dialogBox.transform, false);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -56f);
        sRt.sizeDelta = new Vector2(0f, 22f);

        uiComp.subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        uiComp.subtitleText.fontSize = 13f;
        uiComp.subtitleText.alignment = TextAlignmentOptions.Center;
        uiComp.subtitleText.color = new Color(0.4f, 0.85f, 1f);

        // Close Button (Top Right [X])
        GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(dialogBox.transform, false);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-25f, -22f);
        cRt.sizeDelta = new Vector2(46f, 46f);

        Image cImg = closeBtnObj.GetComponent<Image>();
        cImg.color = new Color(0.75f, 0.22f, 0.25f, 1f);
        var cOutline = closeBtnObj.AddComponent<Outline>();
        cOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.7f);

        Button cBtn = closeBtnObj.GetComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.onClick.AddListener(uiComp.Close);

        GameObject cTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        cTxtObj.layer = 5;
        cTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI cTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        cTmp.text = "X";
        cTmp.fontSize = 22;
        cTmp.fontStyle = FontStyles.Bold;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;

        // TOP SECTION: 3 Skill Sockets Container
        GameObject topSection = new GameObject("TopSection_SkillSockets", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topSection.layer = 5;
        topSection.transform.SetParent(dialogBox.transform, false);
        RectTransform tsRt = topSection.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 0.53f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.pivot = new Vector2(0.5f, 0.5f);
        tsRt.offsetMin = new Vector2(35f, 8f);
        tsRt.offsetMax = new Vector2(-35f, -86f);
        Image tsImg = topSection.GetComponent<Image>();
        tsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

        var tsOutline = topSection.AddComponent<Outline>();
        tsOutline.effectColor = new Color(0.25f, 0.35f, 0.5f, 0.6f);

        // Total Buffs Banner (Top-Left of TopSection)
        GameObject buffTextObj = new GameObject("TotalBuffsBanner", typeof(RectTransform), typeof(CanvasRenderer));
        buffTextObj.layer = 5;
        buffTextObj.transform.SetParent(topSection.transform, false);
        RectTransform btRt = buffTextObj.GetComponent<RectTransform>();
        btRt.anchorMin = new Vector2(0f, 1f);
        btRt.anchorMax = new Vector2(1f, 1f);
        btRt.pivot = new Vector2(0f, 1f);
        btRt.anchoredPosition = new Vector2(15f, -8f);
        btRt.sizeDelta = new Vector2(-280f, 28f);
        uiComp.totalBuffsText = buffTextObj.AddComponent<TextMeshProUGUI>();
        uiComp.totalBuffsText.fontSize = 13f;
        uiComp.totalBuffsText.fontStyle = FontStyles.Bold;
        uiComp.totalBuffsText.alignment = TextAlignmentOptions.Left;
        uiComp.totalBuffsText.color = new Color(0.2f, 1f, 0.45f);
        uiComp.totalBuffsText.enableWordWrapping = false;
        uiComp.totalBuffsText.overflowMode = TextOverflowModes.Ellipsis;

        // Auto Merge All Button (Top-Right of TopSection)
        GameObject autoMergeObj = new GameObject("Btn_AutoMergeAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        autoMergeObj.layer = 5;
        autoMergeObj.transform.SetParent(topSection.transform, false);
        RectTransform amRt = autoMergeObj.GetComponent<RectTransform>();
        amRt.anchorMin = new Vector2(1f, 1f);
        amRt.anchorMax = new Vector2(1f, 1f);
        amRt.pivot = new Vector2(1f, 1f);
        amRt.anchoredPosition = new Vector2(-15f, -6f);
        amRt.sizeDelta = new Vector2(240f, 32f);

        Image amImg = autoMergeObj.GetComponent<Image>();
        amImg.color = new Color(0.92f, 0.55f, 0.08f, 1f);
        var amOutline = autoMergeObj.AddComponent<Outline>();
        amOutline.effectColor = new Color(1f, 0.85f, 0.4f, 0.8f);

        uiComp.autoMergeAllBtn = autoMergeObj.GetComponent<Button>();
        uiComp.autoMergeAllBtn.targetGraphic = amImg;
        uiComp.autoMergeAllBtn.onClick.AddListener(uiComp.OnAutoMergeAllButtonClicked);

        GameObject amTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        amTxtObj.layer = 5;
        amTxtObj.transform.SetParent(autoMergeObj.transform, false);
        RectTransform amtRt = amTxtObj.GetComponent<RectTransform>();
        amtRt.anchorMin = Vector2.zero;
        amtRt.anchorMax = Vector2.one;
        amtRt.sizeDelta = Vector2.zero;

        uiComp.autoMergeAllBtnText = amTxtObj.AddComponent<TextMeshProUGUI>();
        uiComp.autoMergeAllBtnText.fontSize = 12.5f;
        uiComp.autoMergeAllBtnText.fontStyle = FontStyles.Bold;
        uiComp.autoMergeAllBtnText.color = Color.white;
        uiComp.autoMergeAllBtnText.alignment = TextAlignmentOptions.Center;
        uiComp.autoMergeAllBtnText.enableWordWrapping = false;

        // 3 Skill Sockets Container (Center of TopSection)
        GameObject sContainer = new GameObject("SocketsContainer", typeof(RectTransform), typeof(CanvasRenderer));
        sContainer.layer = 5;
        sContainer.transform.SetParent(topSection.transform, false);
        RectTransform scRt = sContainer.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.5f, 0.5f);
        scRt.anchorMax = new Vector2(0.5f, 0.5f);
        scRt.pivot = new Vector2(0.5f, 0.5f);
        scRt.anchoredPosition = new Vector2(0f, -12f);
        scRt.sizeDelta = new Vector2(1200f, 195f);
        uiComp.socketsContainer = sContainer.transform;

        // BOTTOM SECTION: Bag Grid
        GameObject botSection = new GameObject("BottomSection_Bag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        botSection.layer = 5;
        botSection.transform.SetParent(dialogBox.transform, false);
        RectTransform bsRt = botSection.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0f, 0f);
        bsRt.anchorMax = new Vector2(1f, 0.53f);
        bsRt.pivot = new Vector2(0.5f, 0.5f);
        bsRt.offsetMin = new Vector2(35f, 42f);
        bsRt.offsetMax = new Vector2(-35f, -8f);
        Image bsImg = botSection.GetComponent<Image>();
        bsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

        var bsOutline = botSection.AddComponent<Outline>();
        bsOutline.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.5f);

        // Header
        GameObject bhObj = new GameObject("BagHeader", typeof(RectTransform), typeof(CanvasRenderer));
        bhObj.layer = 5;
        bhObj.transform.SetParent(botSection.transform, false);
        RectTransform bhRt = bhObj.GetComponent<RectTransform>();
        bhRt.anchorMin = new Vector2(0f, 1f);
        bhRt.anchorMax = new Vector2(1f, 1f);
        bhRt.pivot = new Vector2(0.5f, 1f);
        bhRt.anchoredPosition = new Vector2(0f, -6f);
        bhRt.sizeDelta = new Vector2(-20f, 22f);
        uiComp.bagSectionHeaderText = bhObj.AddComponent<TextMeshProUGUI>();
        uiComp.bagSectionHeaderText.fontSize = 12.5f;
        uiComp.bagSectionHeaderText.fontStyle = FontStyles.Bold;
        uiComp.bagSectionHeaderText.alignment = TextAlignmentOptions.Center;
        uiComp.bagSectionHeaderText.color = new Color(1f, 0.85f, 0.35f);

        // Scroll View
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect));
        scrollObj.layer = 5;
        scrollObj.transform.SetParent(botSection.transform, false);
        RectTransform scrRt = scrollObj.GetComponent<RectTransform>();
        scrRt.anchorMin = Vector2.zero;
        scrRt.anchorMax = Vector2.one;
        scrRt.offsetMin = new Vector2(10f, 8f);
        scrRt.offsetMax = new Vector2(-10f, -32f);
        var scroll = scrollObj.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        vpObj.layer = 5;
        vpObj.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRt = vpObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero;
        Image vpImg = vpObj.GetComponent<Image>();
        vpImg.color = Color.white;
        var mask = vpObj.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObj.layer = 5;
        contentObj.transform.SetParent(vpObj.transform, false);
        RectTransform cntRt = contentObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0f, 1f);
        cntRt.anchorMax = new Vector2(1f, 1f);
        cntRt.pivot = new Vector2(0.5f, 1f);
        cntRt.anchoredPosition = Vector2.zero;
        cntRt.sizeDelta = Vector2.zero;

        var grid = contentObj.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(230f, 95f);
        grid.spacing = new Vector2(10f, 10f);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = cntRt;
        scroll.viewport = vpRt;
        uiComp.bagContentContainer = contentObj.transform;

        // Bottom Footer Bar (Loot Summary & Resume Button)
        GameObject footObj = new GameObject("FooterBar", typeof(RectTransform), typeof(CanvasRenderer));
        footObj.layer = 5;
        footObj.transform.SetParent(dialogBox.transform, false);
        RectTransform fRt = footObj.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0f);
        fRt.anchorMax = new Vector2(1f, 0f);
        fRt.pivot = new Vector2(0.5f, 0f);
        fRt.anchoredPosition = new Vector2(0f, 6f);
        fRt.sizeDelta = new Vector2(-35f, 32f);

        GameObject lootObj = new GameObject("LootSummary", typeof(RectTransform), typeof(CanvasRenderer));
        lootObj.layer = 5;
        lootObj.transform.SetParent(footObj.transform, false);
        RectTransform lootRt = lootObj.GetComponent<RectTransform>();
        lootRt.anchorMin = new Vector2(0f, 0.5f);
        lootRt.anchorMax = new Vector2(0f, 0.5f);
        lootRt.pivot = new Vector2(0f, 0.5f);
        lootRt.anchoredPosition = new Vector2(10f, 0f);
        lootRt.sizeDelta = new Vector2(900f, 28f);
        uiComp.lootSummaryText = lootObj.AddComponent<TextMeshProUGUI>();
        uiComp.lootSummaryText.fontSize = 14f;
        uiComp.lootSummaryText.fontStyle = FontStyles.Bold;
        uiComp.lootSummaryText.alignment = TextAlignmentOptions.MidlineLeft;

        modalObj.SetActive(false);
        return uiComp;
    }

    private void OnAutoMergeAllButtonClicked()
    {
        if (SoundManager.EnsureInstance() != null) SoundManager.EnsureInstance().PlayButtonClick();

        int mergeable = EquipmentSocketManager.GetTotalAvailableMergeGemsCount();
        if (mergeable <= 0)
        {
            DamagePopup.CreateText(autoMergeAllBtn != null ? autoMergeAllBtn.transform.position : transform.position, LocalizationManager.T("Không có ngọc trùng trong túi!", "No matching gems in bag!"), Color.yellow);
            return;
        }

        int consumed = EquipmentSocketManager.AutoMergeAllMatchingGems(out int upgradedSlots, out int totalLevelsGained);
        if (consumed > 0)
        {
            SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
            NotifyPlayerCombatStatsChanged();
            RefreshModal();

            string notify = LocalizationManager.T(
                $"Đã nâng cấp {upgradedSlots} ô ngọc (+{totalLevelsGained} Cấp)!",
                $"Upgraded {upgradedSlots} sockets (+{totalLevelsGained} Lvs)!"
            );
            DamagePopup.CreateText(autoMergeAllBtn != null ? autoMergeAllBtn.transform.position : transform.position, notify, new Color(0.2f, 1f, 0.45f));
        }
    }
}
