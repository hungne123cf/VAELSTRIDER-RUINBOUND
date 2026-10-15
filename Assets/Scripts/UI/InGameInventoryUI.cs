// ========================================================
// VAELSTRIDER: RUINBOUND - In-Game Battle Hero Gear & Gem Socketing UI
// Cho phép mở túi đồ và khảm / tháo ngọc & trang bị trực tiếp vào 6 Ô xung quanh Anh Hùng trong trận đấu!
// (Slot 0: Vũ Khí, Slot 1: Áo Giáp, Slot 2: Giày, Slot 3: Mũ Nón, Slot 4: Nhẫn, Slot 5: Hộ Phù)
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

    [Header("Hero Stage & Sockets View")]
    [SerializeField] private Image heroAvatarPreviewImage;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI heroRoleText;
    [SerializeField] private TextMeshProUGUI heroStatsText;
    [SerializeField] private TextMeshProUGUI totalBuffsText;
    [SerializeField] private Transform socketsContainer;
    [SerializeField] private Transform skillsContainer;

    [Header("Bag Grid")]
    [SerializeField] private Transform bagContentContainer;
    [SerializeField] private TextMeshProUGUI bagSectionHeaderText;
    [SerializeField] private TextMeshProUGUI lootSummaryText;
    [SerializeField] private Button autoMergeAllBtn;
    [SerializeField] private TextMeshProUGUI autoMergeAllBtnText;

    private List<GameObject> socketSlotObjs = new List<GameObject>();
    private List<Image> socketConduitLines = new List<Image>();
    private List<GameObject> skillCardObjs = new List<GameObject>();
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
        string curHero = EquipmentSocketManager.GetActiveHeroId();

        // Header Title
        if (titleText != null)
        {
            titleText.text = isVi ? "TRANG BỊ & NGỌC ANH HÙNG" : "HERO GEAR & RUNIC GEMS";
        }
        if (subtitleText != null)
        {
            subtitleText.text = isVi
                ? "Lắp trang bị và ngọc vào 6 ô xung quanh Anh Hùng để nhận sức mạnh trong trận đấu!"
                : "Equip gear and runic gems into 6 slots around the Hero for battle buffs!";
        }

        // Live Total Socket Bonuses
        EquipmentSocketManager.GetTotalHeroSocketBonus(
            out int sAtk, out int sPhysAtk, out int sMagicAtk, out int sTrueAtk,
            out int sHp, out int sDef,
            out float sLifesteal, out float sCd, out float sShard, out float sRegen,
            out float sMaxEnergy, out float sEnergyRegen,
            out float sPhysLife, out float sSpellVamp, out float sCrit,
            curHero);

        // Thêm chỉ số từ Lõi Nâng Cấp (Augments) cộng thẳng vào cùng chỉ số tương ứng
        int aAtk = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusAtk() : 0;
        int aHp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusHp() : 0;
        int aDef = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugDef() : 0;
        float aCrit = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCrit() : 0f;
        float aCritDmg = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCritDmg() : 0f;
        float aLife = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugLifesteal() : 0f;
        float aSVamp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugSpellVamp() : 0f;
        float aCDR = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCDR() : 0f;

        string heroKey = curHero.ToLower();
        bool isMagic = (heroKey.Contains("azure") || heroKey.Contains("player3") || heroKey.Contains("mage") || heroKey.Contains("celestial") || heroKey.Contains("player6"));

        int totalBonusPhysAtk = sPhysAtk + (!isMagic ? aAtk : 0);
        int totalBonusMagicAtk = sMagicAtk + (isMagic ? aAtk : 0);
        int totalBonusTrueAtk = sTrueAtk;
        int totalBonusGeneralAtk = sAtk + aAtk;

        int totalBonusHp = sHp + aHp;
        int totalBonusDef = sDef + aDef;
        float totalBonusCrit = sCrit + aCrit;
        float totalBonusCritDmg = aCritDmg;
        float totalBonusPhysLife = (sPhysLife > 0f ? sPhysLife : sLifesteal) + aLife;
        float totalBonusSpellVamp = sSpellVamp + aSVamp;
        float totalBonusCd = sCd + aCDR;

        List<string> buffTags = new List<string>();
        if (totalBonusPhysAtk > 0) buffTags.Add($"<color=#FF3B30>+{totalBonusPhysAtk} ATK</color>");
        if (totalBonusMagicAtk > 0) buffTags.Add($"<color=#B84DFF>+{totalBonusMagicAtk} AP</color>");
        if (totalBonusTrueAtk > 0) buffTags.Add($"<color=#E2E8F0>+{totalBonusTrueAtk} TD</color>");
        if (totalBonusPhysAtk == 0 && totalBonusMagicAtk == 0 && totalBonusTrueAtk == 0 && totalBonusGeneralAtk > 0)
            buffTags.Add($"<color=#FF3B30>+{totalBonusGeneralAtk} ATK</color>");

        if (totalBonusHp > 0) buffTags.Add($"<color=#22C55E>+{totalBonusHp} HP</color>");
        if (totalBonusDef > 0) buffTags.Add($"<color=#38BDF8>+{totalBonusDef} DEF</color>");
        if (totalBonusCrit > 0.0001f) buffTags.Add($"<color=#F59E0B>+{Mathf.RoundToInt(totalBonusCrit * 100)}% CRIT</color>");
        if (totalBonusCritDmg > 0.0001f) buffTags.Add($"<color=#F59E0B>+{Mathf.RoundToInt(totalBonusCritDmg * 100)}% {LocalizationManager.T("ST Bạo", "Crit Dmg")}</color>");
        if (totalBonusPhysLife > 0.0001f) buffTags.Add($"<color=#F43F5E>+{Mathf.RoundToInt(totalBonusPhysLife * 100)}% {LocalizationManager.T("Hút Máu VL", "Phys Lifesteal")}</color>");
        if (totalBonusSpellVamp > 0.0001f) buffTags.Add($"<color=#E879F9>+{Mathf.RoundToInt(totalBonusSpellVamp * 100)}% {LocalizationManager.T("Hút Máu Phép", "Spell Vamp")}</color>");
        if (totalBonusCd > 0.0001f) buffTags.Add($"<color=#C084FC>+{Mathf.RoundToInt(totalBonusCd * 100)}% {LocalizationManager.T("Giảm CD", "CDR")}</color>");
        if (sMaxEnergy > 0) buffTags.Add($"<color=#00E5FF>+{Mathf.RoundToInt(sMaxEnergy)} Mana</color>");
        if (sEnergyRegen > 0) buffTags.Add($"<color=#06B6D4>+{sEnergyRegen:0.#} Mana/s</color>");
        if (sRegen > 0) buffTags.Add($"<color=#10B981>+{sRegen} HP/s</color>");
        if (sShard > 0) buffTags.Add($"<color=#FBBF24>+{Mathf.RoundToInt(sShard * 100)}% Shard</color>");

        if (totalBuffsText != null)
        {
            totalBuffsText.text = (buffTags.Count > 0)
                ? $"{LocalizationManager.T("Tổng Buff Đang Kích Hoạt:", "Total Active Buffs:")} {string.Join(" | ", buffTags)}"
                : LocalizationManager.T("<color=#888888>Chưa có buff nào được kích hoạt. Hãy lắp trang bị vào 6 ô hoặc chọn Lõi Nâng Cấp!</color>",
                                        "<color=#888888>No active buffs. Equip gear into 6 slots or choose Augments to boost stats!</color>");
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

        // Render Hero Stage, 6 Sockets & 3 Skills
        RenderHeroStageAndSockets(curHero, sAtk, sHp, sDef, sMaxEnergy, sEnergyRegen, sLifesteal, sCd, sPhysAtk, sMagicAtk, sTrueAtk, sPhysLife, sSpellVamp, sCrit, sRegen, sShard);

        // Render Bag Gems & Items
        RenderBagGems();

        // Bottom Summary (Synchronized with PlayerLevelSystem)
        long shards = AccountDatabase.GetAethelShards();
        string shardStr = MainMenuManager.FormatCompactNumber(shards);
        int curLv = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1;
        int curExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentExp : 0;
        int reqExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.ExpRequired : 4;

        if (lootSummaryText != null)
        {
            lootSummaryText.text = $"<color=#FFD700>LV {curLv} (EXP {curExp}/{reqExp})</color>  |  <color=#00FFFF>Aethel Shards: {shardStr}</color>";
        }
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
        var btnType = (skillIdx == 0) ? MobileActionButton.ButtonType.Attack 
                    : (skillIdx == 1 ? MobileActionButton.ButtonType.Dash : MobileActionButton.ButtonType.Skill);
        return MobileActionButton.GetHeroActionIcon(h, btnType);
    }

    private static Sprite LoadHeroAvatarSprite(string heroId)
    {
        string h = string.IsNullOrEmpty(heroId) ? "vael" : heroId.ToLower();
        
        // 1. Try to get sprite from active player in the scene
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
        if (player != null)
        {
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;
        }

        // 2. Load from character sprite paths
        if (h.Contains("azure") || h.Contains("player3"))
        {
            var sprites = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player3/Sword_Idle.png", 64, 64);
            if (sprites != null && sprites.Length > 0) return sprites[0];
            return Resources.Load<Sprite>("UI/Avatars/avatar_azure");
        }
        else if (h.Contains("titan") || h.Contains("player4"))
        {
            var sprites = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player4/Hammer_Idle.png", 128, 128);
            if (sprites != null && sprites.Length > 0) return sprites[0];
            return Resources.Load<Sprite>("UI/Avatars/avatar_titan");
        }
        else if (h.Contains("ronin") || h.Contains("gale") || h.Contains("maid") || h.Contains("elysia"))
        {
            var sprites = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player2/_down idle.png", 64, 64);
            if (sprites != null && sprites.Length > 0) return sprites[0];
            return Resources.Load<Sprite>("UI/Avatars/avatar_ronin");
        }
        else if (h.Contains("dune") || h.Contains("player5"))
        {
            var sprites = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player5/PNG/idle/idle_1.png", 64, 64);
            if (sprites != null && sprites.Length > 0) return sprites[0];
            return Resources.Load<Sprite>("UI/Avatars/avatar_dune");
        }
        else if (h.Contains("mage") || h.Contains("celestial") || h.Contains("player6"))
        {
            var sprites = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player6/sample(idle&walk)/idle/sprite sheets/idle.png", 46, 55);
            if (sprites != null && sprites.Length > 0) return sprites[0];
            return Resources.Load<Sprite>("UI/Avatars/avatar_mage");
        }
        else
        {
            var sprites = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player/IDLE/idle_down.png", 64, 64);
            if (sprites != null && sprites.Length > 0) return sprites[0];
            return Resources.Load<Sprite>("UI/Avatars/avatar_vael");
        }
    }

    private void RenderHeroStageAndSockets(
        string heroId, int sAtk, int sHp, int sDef, float sMaxEn, float sEnRegen,
        float sLife, float sCd, int sPhysAtk, int sMagicAtk, int sTrueAtk,
        float sPhysLife, float sSpellVamp, float sCrit, float sRegen, float sShard)
    {
        bool isVi = LocalizationManager.IsVietnamese;
        string h = heroId.ToLower();

        // 1. Hero Preview Sprite & Name/Role
        if (heroAvatarPreviewImage != null)
        {
            Sprite avatarSpr = LoadHeroAvatarSprite(h);
            if (avatarSpr != null) heroAvatarPreviewImage.sprite = avatarSpr;
            heroAvatarPreviewImage.transform.localScale = MainMenuManager.GetHeroVisualScale(h);
        }

        int baseHp = 100;
        int baseAtk = 20;
        string heroNameStr = "Vaelstrider";
        string heroRoleStr = "Hiệp Sĩ Tiên Phong";

        if (h.Contains("titan") || h.Contains("player4"))
        {
            baseHp = 200; baseAtk = 32;
            heroNameStr = isVi ? "Cuồng Chiến Binh Titan" : "Titan Berserker";
            heroRoleStr = isVi ? "Đấu Sĩ Cận Chiến / Chống Chịu & Siêu Trọng Trảm" : "Vanguard / Heavy Greatsword Brawler";
        }
        else if (h.Contains("azure") || h.Contains("player3"))
        {
            baseHp = 110; baseAtk = 24;
            heroNameStr = isVi ? "Kiếm Sĩ Băng Azure Blade" : "Frost Swordmaster Azure";
            heroRoleStr = isVi ? "Sát Thủ Băng Hàn / Song Đoản Kiếm & Kiếm Khí" : "Frost Assassin / Twin Blades & Tempests";
        }
        else if (h.Contains("ronin") || h.Contains("gale") || h.Contains("maid") || h.Contains("elysia"))
        {
            baseHp = 95; baseAtk = 28;
            heroNameStr = isVi ? "Nữ Hiệp Gale Ronin" : "Gale Ronin Elysia";
            heroRoleStr = isVi ? "Nữ Kiếm Sĩ Phong Ma / Tốc Trảm & Phân Thân" : "Gale Striker / Lightning Clones & Sweeps";
        }
        else if (h.Contains("dune") || h.Contains("player5"))
        {
            baseHp = 110; baseAtk = 18;
            heroNameStr = isVi ? "Sát Thủ Sa Mạc Dune" : "Dune Assassin";
            heroRoleStr = isVi ? "Sát Thủ Ám Ảnh / Song Đao Độc & Phục Kích" : "Shadow Assassin / Dual Daggers & Ambush";
        }
        else if (h.Contains("mage") || h.Contains("celestial") || h.Contains("player6"))
        {
            baseHp = 90; baseAtk = 30;
            heroNameStr = isVi ? "Pháp Sư Tinh Tú Aurelia" : "Celestial Mage Aurelia";
            heroRoleStr = isVi ? "Pháp Sư Tinh Tú / Đạn Tinh Tú & Thiên Thạch Hủy Diệt" : "Celestial Sorceress / Homing Orbs & Meteor Apocalypse";
        }
        else
        {
            heroNameStr = isVi ? "Hiệp Sĩ Vaelstrider" : "Knight Vaelstrider";
            heroRoleStr = isVi ? "Hiệp Sĩ Tiên Phong / Thánh Kiếm & Phản Đòn" : "Warrior / Holy Blade Master";
        }

        int heroLv = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1;
        int heroExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentExp : 0;
        int heroReqExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.ExpRequired : 4;

        if (heroNameText != null)
        {
            heroNameText.text = heroNameStr;
            RectTransform hnRt = heroNameText.rectTransform;
            if (hnRt != null)
            {
                hnRt.anchorMin = new Vector2(0f, 1f);
                hnRt.anchorMax = new Vector2(1f, 1f);
                hnRt.pivot = new Vector2(0f, 1f);
                hnRt.offsetMin = new Vector2(14f, -42f);
                hnRt.offsetMax = new Vector2(-165f, -10f);
            }

            // Cụm Level Badge: [divider-fade-000] Lv.X [divider-fade-000 đối xứng]
            Transform parentT = heroNameText.transform.parent;
            if (parentT != null)
            {
                Transform existingBadge = parentT.Find("HeroLevelBadge");
                GameObject badgeObj;
                if (existingBadge != null)
                {
                    badgeObj = existingBadge.gameObject;
                }
                else
                {
                    badgeObj = new GameObject("HeroLevelBadge", typeof(RectTransform));
                    badgeObj.layer = 5;
                    badgeObj.transform.SetParent(parentT, false);
                }

                RectTransform bRt = badgeObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 1f);
                bRt.anchorMax = new Vector2(1f, 1f);
                bRt.pivot = new Vector2(1f, 0.5f);
                bRt.anchoredPosition = new Vector2(-14f, -26f);
                bRt.sizeDelta = new Vector2(150f, 24f);

                Sprite divSpr = WhiteIconLoader.GetDividerFade000Sprite();
                Color divCol = new Color(1f, 0.85f, 0.35f, 0.95f);

                // 1. divider-fade-000.png (đuôi fade sang trái, hoa văn hướng vào giữa ôm chữ Lv.X)
                Transform ldTrans = badgeObj.transform.Find("LeftDivider");
                GameObject leftDiv;
                if (ldTrans == null)
                {
                    leftDiv = new GameObject("LeftDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    leftDiv.layer = 5;
                    leftDiv.transform.SetParent(badgeObj.transform, false);
                }
                else
                {
                    leftDiv = ldTrans.gameObject;
                }
                RectTransform ldRt = leftDiv.GetComponent<RectTransform>();
                ldRt.anchorMin = new Vector2(1f, 0.5f);
                ldRt.anchorMax = new Vector2(1f, 0.5f);
                ldRt.pivot = new Vector2(1f, 0.5f);
                ldRt.anchoredPosition = new Vector2(-100f, 0f);
                ldRt.sizeDelta = new Vector2(50f, 13f);
                ldRt.localScale = Vector3.one;
                Image ldImg = leftDiv.GetComponent<Image>();
                ldImg.sprite = divSpr;
                ldImg.color = divCol;
                ldImg.preserveAspect = false;
                ldImg.raycastTarget = false;

                // 2. Chữ Lv.X chính giữa
                Transform lvtTrans = badgeObj.transform.Find("LevelText");
                GameObject lvTxtObj;
                TextMeshProUGUI lvtTmp;
                if (lvtTrans == null)
                {
                    lvTxtObj = new GameObject("LevelText", typeof(RectTransform), typeof(CanvasRenderer));
                    lvTxtObj.layer = 5;
                    lvTxtObj.transform.SetParent(badgeObj.transform, false);
                    lvtTmp = lvTxtObj.AddComponent<TextMeshProUGUI>();
                }
                else
                {
                    lvTxtObj = lvtTrans.gameObject;
                    lvtTmp = lvTxtObj.GetComponent<TextMeshProUGUI>();
                }
                RectTransform lvtRt = lvTxtObj.GetComponent<RectTransform>();
                lvtRt.anchorMin = new Vector2(1f, 0.5f);
                lvtRt.anchorMax = new Vector2(1f, 0.5f);
                lvtRt.pivot = new Vector2(0.5f, 0.5f);
                lvtRt.anchoredPosition = new Vector2(-75f, 0f);
                lvtRt.sizeDelta = new Vector2(46f, 22f);
                lvtTmp.text = $"Lv.{heroLv}";
                lvtTmp.fontSize = 17f;
                lvtTmp.fontStyle = FontStyles.Bold;
                lvtTmp.color = new Color(1f, 0.88f, 0.35f);
                lvtTmp.alignment = TextAlignmentOptions.Center;
                lvtTmp.enableWordWrapping = false;
                lvtTmp.raycastTarget = false;

                // 3. divider-fade-000.png bản đối xứng (lật ngang: hoa văn hướng vào giữa ôm chữ Lv.X, đuôi fade sang phải)
                Transform rdTrans = badgeObj.transform.Find("RightDivider");
                GameObject rightDiv;
                if (rdTrans == null)
                {
                    rightDiv = new GameObject("RightDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    rightDiv.layer = 5;
                    rightDiv.transform.SetParent(badgeObj.transform, false);
                }
                else
                {
                    rightDiv = rdTrans.gameObject;
                }
                RectTransform rdRt = rightDiv.GetComponent<RectTransform>();
                rdRt.anchorMin = new Vector2(1f, 0.5f);
                rdRt.anchorMax = new Vector2(1f, 0.5f);
                rdRt.pivot = new Vector2(0.5f, 0.5f);
                rdRt.anchoredPosition = new Vector2(-25f, 0f);
                rdRt.sizeDelta = new Vector2(50f, 13f);
                rdRt.localScale = new Vector3(-1f, 1f, 1f);
                Image rdImg = rightDiv.GetComponent<Image>();
                rdImg.sprite = divSpr;
                rdImg.color = divCol;
                rdImg.preserveAspect = false;
                rdImg.raycastTarget = false;
            }
        }
        if (heroRoleText != null) heroRoleText.text = $"<color=#38BDF8>[{(isVi ? $"CẤP {heroLv} - EXP {heroExp}/{heroReqExp}" : $"LV {heroLv} - EXP {heroExp}/{heroReqExp}")}]</color>  {heroRoleStr}";

        int upHp = PlayerUpgradeManager.EnsureInstance()?.GetHealthBonus(heroId) ?? 0;
        int upAtk = PlayerUpgradeManager.EnsureInstance()?.GetAttackBonus(heroId) ?? 0;
        float upDashCd = PlayerUpgradeManager.EnsureInstance()?.GetDashCooldownReduction(heroId) ?? 0f;
        float upUltiCd = PlayerUpgradeManager.EnsureInstance()?.GetUltiCooldownReduction(heroId) ?? 0f;

        // Augment Bonuses (từ hệ thống Lõi Nâng Cấp khi lên cấp trong trận)
        int augHp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusHp() : 0;
        int augAtk = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusAtk() : 0;
        int augDef = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugDef() : 0;
        float augCrit = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCrit() : 0f;
        float augCritDmg = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCritDmg() : 0f;
        float augLife = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugLifesteal() : 0f;
        float augSpellVamp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugSpellVamp() : 0f;
        float augCDR = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCDR() : 0f;

        int totalHp = baseHp + upHp + sHp + augHp;
        int totalAtk = baseAtk + upAtk + sAtk + augAtk;
        int totalDef = sDef + augDef;
        float finalCd = sCd + augCDR;

        if (heroStatsText != null)
        {
            heroStatsText.fontSize = 15f;
            heroStatsText.lineSpacing = 1.25f;
            RectTransform stRt = heroStatsText.GetComponent<RectTransform>();
            if (stRt != null)
            {
                stRt.anchoredPosition = new Vector2(14f, -76f);
                stRt.sizeDelta = new Vector2(-28f, 290f);
            }

            string hpAdd = (upHp + sHp + augHp > 0) ? $" <color=#22C55E>(+{upHp + sHp + augHp})</color>" : "";
            string defAdd = totalDef > 0 ? $" <color=#38BDF8>(+{totalDef})</color>" : "";
            string regenStr = sRegen > 0f ? $" <color=#10B981>(+{sRegen:0.#}/s)</color>" : "";

            string atkAdd = (upAtk + sAtk + augAtk > 0) ? $" <color=#FF3B30>(+{upAtk + sAtk + augAtk})</color>" : "";

            string manaAdd = sMaxEn > 0 ? $" <color=#00E5FF>(+{(int)sMaxEn})</color>" : "";
            string manaRegenStr = sEnRegen > 0f ? $" <color=#06B6D4>(+{sEnRegen:0.#}/s)</color>" : "";

            // Phân loại chi tiết từng hệ sát thương
            bool isMagicHero = (h.Contains("azure") || h.Contains("player3") || h.Contains("mage") || h.Contains("celestial") || h.Contains("player6"));
            int totalPhys = (!isMagicHero ? (baseAtk + upAtk + augAtk) : 0) + sPhysAtk;
            int totalMagic = (isMagicHero ? (baseAtk + upAtk + augAtk) : 0) + sMagicAtk;
            int totalTrue = sTrueAtk;

            List<string> dmgTags = new List<string>();
            if (totalPhys > 0) dmgTags.Add($"<color=#FF3B30>{totalPhys} ATK</color>");
            if (totalMagic > 0) dmgTags.Add($"<color=#B84DFF>{totalMagic} AP</color>");
            if (totalTrue > 0) dmgTags.Add($"<color=#E2E8F0>{totalTrue} TD</color>");
            string dmgBreakdownStr = dmgTags.Count > 0 ? string.Join(" | ", dmgTags) : "<color=#94A3B8>None</color>";

            // Chí mạng (cơ bản 20%), Sát thương bạo (cơ bản x1.75 + Lõi), Năng lượng
            float totalCritBonus = sCrit + augCrit;
            int totalCritPct = 20 + Mathf.RoundToInt(totalCritBonus * 100);
            string critBonusStr = totalCritBonus > 0.001f ? $" <color=#F59E0B>(+{Mathf.RoundToInt(totalCritBonus * 100)}%)</color>" : "";

            float finalCritMultiplier = 1.75f + augCritDmg;
            string critDmgBonusStr = augCritDmg > 0.001f ? $" <color=#F59E0B>(+{Mathf.RoundToInt(augCritDmg * 100)}%)</color>" : "";
            string critDmgValStr = $"x{finalCritMultiplier:0.##}";

            // Hút máu vật lý, Hút máu phép, Giảm CD, Thưởng Shard
            float finalPhysLife = (sPhysLife > 0f ? sPhysLife : sLife) + augLife;
            float finalSpellVamp = sSpellVamp + augSpellVamp;

            int physLife = Mathf.RoundToInt(finalPhysLife * 100);
            int spellVamp = Mathf.RoundToInt(finalSpellVamp * 100);
            int cdPct = Mathf.RoundToInt(finalCd * 100);
            int shardPct = Mathf.RoundToInt(sShard * 100);

            string cdTag = $"{cdPct}%";
            if (upDashCd > 0f)
            {
                string dashStr = isVi ? $"-{upDashCd:0.#}s Lướt" : $"-{upDashCd:0.#}s Dash";
                cdTag = $"{cdTag} | {dashStr}";
            }

            // Từng dòng thông số xếp thành 1 hàng dọc xuống chuyên nghiệp, dễ nhìn
            // Toàn bộ Lõi Nâng Cấp (Augments) đã được cộng gộp trực tiếp vào từng chỉ số tương ứng ở trên
            List<string> statLines = new List<string>
            {
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "SINH LỰC (HP):" : "HEALTH (HP):")}</b> <color=#22C55E>{totalHp}</color>{hpAdd}{regenStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "GIÁP PHÒNG THỦ:" : "DEFENSE (DEF):")}</b> <color=#38BDF8>{totalDef}</color>{defAdd}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "TỔNG CÔNG KÍCH:" : "TOTAL ATTACK:")}</b> <color=#FF3B30>{totalAtk}</color>{atkAdd}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "PHÂN LOẠI SÁT THƯƠNG:" : "DAMAGE TYPE:")}</b> {dmgBreakdownStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "CHÍ MẠNG / BẠO KÍCH:" : "CRITICAL STATS:")}</b> <color=#F59E0B>{totalCritPct}%</color>{critBonusStr} | <color=#F59E0B>{critDmgValStr}</color>{critDmgBonusStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "NĂNG LƯỢNG (MANA):" : "ENERGY (MANA):")}</b> <color=#00E5FF>{100 + (int)sMaxEn}</color>{manaAdd}{manaRegenStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "HÚT MÁU / HỒI PHỤC:" : "VAMP & HEALING:")}</b> <color=#F43F5E>{physLife}% {(isVi ? "VL" : "Phys")}</color> | <color=#E879F9>{spellVamp}% {(isVi ? "Phép" : "Spell")}</color>",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "GIẢM COOLDOWN:" : "CD REDUCTION:")}</b> <color=#C084FC>{cdTag}</color>" + (shardPct > 0 ? $" | <b>SHARD:</b> <color=#FBBF24>+{shardPct}%</color>" : "")
            };

            heroStatsText.text = string.Join("\n", statLines);
        }

        // 2. Render 6 Sockets around the Avatar & Conduit Lines
        for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT && s < socketSlotObjs.Count; s++)
        {
            int slotIdx = s;
            var sObj = socketSlotObjs[slotIdx];
            if (sObj == null) continue;

            foreach (Transform ch in sObj.transform) Destroy(ch.gameObject);

            bool hasItem = EquipmentSocketManager.GetHeroSocketGem(slotIdx, out string gemId, out int itemLevel, heroId);
            var sImg = sObj.GetComponent<Image>();
            var sOutline = sObj.GetComponent<Outline>();
            var gemEff = hasItem ? EquipmentSocketManager.GetGemEffect(gemId) : null;

            if (hasItem)
            {
                var itemDef = InventoryItemDatabase.GetItem(gemId);
                Color rarityCol = gemEff.HasValue ? gemEff.Value.gemColor : Color.cyan;

                if (slotIdx < socketConduitLines.Count && socketConduitLines[slotIdx] != null)
                {
                    socketConduitLines[slotIdx].color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.90f);
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
                icRt.sizeDelta = new Vector2(-8f, -8f);
                var icImg = icObj.GetComponent<Image>();
                if (itemDef != null && itemDef.Icon != null) icImg.sprite = itemDef.Icon;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;

                // Level Badge (Góc dưới phải gọn gàng, nền mờ không che item)
                GameObject lvObj = new GameObject("LevelBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lvObj.transform.SetParent(sObj.transform, false);
                RectTransform lvRt = lvObj.GetComponent<RectTransform>();
                lvRt.anchorMin = new Vector2(1f, 0f);
                lvRt.anchorMax = new Vector2(1f, 0f);
                lvRt.pivot = new Vector2(1f, 0f);
                lvRt.anchoredPosition = new Vector2(-2f, 2f);
                lvRt.sizeDelta = new Vector2(34f, 15f);
                Image lvImg = lvObj.GetComponent<Image>();
                lvImg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);
                lvImg.raycastTarget = false;

                GameObject lvTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
                lvTxtObj.transform.SetParent(lvObj.transform, false);
                RectTransform lvtRt = lvTxtObj.GetComponent<RectTransform>();
                lvtRt.anchorMin = Vector2.zero;
                lvtRt.anchorMax = Vector2.one;
                lvtRt.sizeDelta = Vector2.zero;
                var lvTmp = lvTxtObj.AddComponent<TextMeshProUGUI>();
                lvTmp.text = $"Lv.{itemLevel}";
                lvTmp.fontSize = 11f;
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
                    EquipmentSocketManager.UnsocketHeroSlot(unslotIdx, heroId);
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    NotifyPlayerCombatStatsChanged();
                    RefreshModal();
                };
                sDrag.onMoveSocket = (fromS, toS) =>
                {
                    EquipmentSocketManager.MoveOrSwapHeroSlot(fromS, toS, heroId);
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    NotifyPlayerCombatStatsChanged();
                    RefreshModal();
                };
            }
            else
            {
                if (slotIdx < socketConduitLines.Count && socketConduitLines[slotIdx] != null)
                {
                    socketConduitLines[slotIdx].color = new Color(0.85f, 0.72f, 0.35f, 0.25f);
                }

                if (sImg != null) sImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
                if (sOutline != null)
                {
                    sOutline.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.5f);
                    sOutline.effectDistance = new Vector2(1.2f, -1.2f);
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
                lblTmp.text = $"<size=11><color=#94A3B8><b>{sName}</b></color></size>\n<size=15><color=#64748B>+</color></size>";
                lblTmp.fontSize = 11f;
                lblTmp.fontStyle = FontStyles.Bold;
                lblTmp.alignment = TextAlignmentOptions.Center;
                lblTmp.lineSpacing = -8f;
                lblTmp.enableWordWrapping = false;
                lblTmp.overflowMode = TextOverflowModes.Truncate;
                lblTmp.raycastTarget = false;
            }

            // Xóa khung dày Gothic đè che mất icon item
            Transform gfb = sObj.transform.Find("GothicFrameBorder");
            if (gfb != null) Destroy(gfb.gameObject);
        }

        // 3. Render 3 Skill Cards with Damage Scaling, Damage Type Breakdown & Dynamic Cooldown
        RenderSkillBreakdownCards(h, baseAtk + sAtk, sPhysAtk, sMagicAtk, sTrueAtk, finalCd, upDashCd, upUltiCd);
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

    private void RenderSkillBreakdownCards(string heroId, int totalAtk, int sPhysAtk, int sMagicAtk, int sTrueAtk, float finalCd = 0f, float upDashCd = 0f, float upUltiCd = 0f)
    {
        if (skillsContainer == null) return;
        foreach (var obj in skillCardObjs) if (obj != null) Destroy(obj);
        skillCardObjs.Clear();

        bool isVi = LocalizationManager.IsVietnamese;
        bool isRonin = (heroId.Contains("ronin") || heroId.Contains("gale") || heroId.Contains("maid") || heroId.Contains("elysia"));
        bool isAzure = (heroId.Contains("azure") || heroId.Contains("player3"));
        bool isTitan = (heroId.Contains("titan") || heroId.Contains("player4"));
        bool isDune = (heroId.Contains("dune") || heroId.Contains("player5"));
        bool isMage = (heroId.Contains("mage") || heroId.Contains("celestial") || heroId.Contains("player6"));

        string[] skillTags = new string[3];
        string[] skillNames;
        string[] skillDescs;

        if (isMage)
        {
            skillTags = new string[] {
                "AP",
                isVi ? "TỐC BIẾN" : "BLINK",
                "AP"
            };
            skillNames = new string[] {
                isVi ? "1. CẦU MA PHÁP & HỘ THỂ MA TRẬN" : "1. ARCANE ORBS & BARRIER",
                isVi ? "2. TỐC BIẾN HƯ KHÔNG (BLINK)" : "2. ARCANE BLINK DASH",
                isVi ? "3. THIÊN THẠCH MA TRẬN" : "3. METEOR APOCALYPSE"
            };
            int meteorDmg = (22 * 5) + (totalAtk * 3);
            skillDescs = new string[] {
                isVi ? $"Bắn 3 cầu ma pháp tự dẫn đường gây <color=#B84DFF><b>{totalAtk} AP</b></color> nổ sét lan. <color=#FFDF66>Nhấn Giữ:</color> Tạo khiên hấp thụ 80% sát thương, bộc phá đẩy lùi phản đòn <color=#B84DFF><b>AP</b></color> & hồi <color=#38BDF8><b>+10 Mana</b></color>!"
                     : $"Fires 3 homing arcane orbs inflicting <color=#B84DFF><b>{totalAtk} AP</b></color> with chain lightning. <color=#FFDF66>Hold attack:</color> Barrier absorbs 80% damage, bursts back enemies with <color=#B84DFF><b>AP</b></color> & restores <color=#38BDF8><b>+10 Mana</b></color>!",
                isVi ? "Dịch chuyển tức thời (<color=#55FF88><b>Bất tử</b></color>), để lại tàn ảnh ma pháp nổ đẩy lùi & hồi <color=#38BDF8><b>+12 Mana</b></color>."
                     : "Instant warp (<color=#55FF88><b>Invulnerable</b></color>), leaves repulsive arcane clone & restores <color=#38BDF8><b>+12 Mana</b></color>.",
                isVi ? $"Mở đại pháp trận tinh tú triệu hồi 5 đợt mưa thiên thạch ma pháp gây <color=#B84DFF><b>{meteorDmg} AP diện rộng</b></color> toàn bộ kẻ địch trên bản đồ."
                     : $"Opens celestial circles summoning 5 waves of devastating meteor strikes inflicting catastrophic AoE <color=#B84DFF><b>{meteorDmg} AP</b></color> across the battlefield."
            };
        }
        else if (isDune)
        {
            skillTags = new string[] {
                "ATK / TD",
                isVi ? "ẢO ẢNH" : "MIRAGE",
                "TD"
            };
            skillNames = new string[] {
                isVi ? "1. SONG ĐAO & THẾ THỦ PHẢN KÍCH" : "1. DUAL DAGGERS & MIRAGE PARRY",
                isVi ? "2. ẢO ẢNH SA LƯỚT (3 LẦN LIÊN HOÀN)" : "2. TRIPLE SAND MIRAGE DASH",
                isVi ? "3. CUỒNG BÃO SA ẢNH X-REND" : "3. SANDSTORM X-REND"
            };
            int duneUlti = (14 * 6) + (totalAtk * 3);
            skillDescs = new string[] {
                isVi ? $"Combo 3 đòn song đao gây <color=#FF3B30><b>{totalAtk} ATK</b></color> bạo kích cao. <color=#FFDF66>Nhấn Giữ Thế Thủ:</color> Giảm 70% sát thương và tung đòn Phản Kích Kiếm Khí gây <color=#E2E8F0><b>{totalAtk} TD</b></color> + <color=#FFAA33><b>Choáng 1.2s</b></color>!"
                     : $"Fast 3-hit combos dealing <color=#FF3B30><b>{totalAtk} ATK</b></color>. <color=#FFDF66>Hold attack for Mirage Defend:</color> 70% DR & unleashes Air Slash Counter inflicting <color=#E2E8F0><b>{totalAtk} TD</b></color> + <color=#FFAA33><b>1.2s Stun</b></color>!",
                isVi ? "Tích lũy tối đa <color=#55FFFF><b>3 lần lướt liên hoàn</b></color> cực cơ động (tiêu hao 4% HP), tạo tàn ảnh cát & <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> mỗi lần lướt."
                     : "Stores up to <color=#55FFFF><b>3 Consecutive Dash Charges</b></color> (costs 4% HP), leaves mirages & <color=#FFAA33><b>Reduces Ulti CD 1.5s</b></color> per dash.",
                isVi ? $"Tạo lốc xoáy cát hút quái (tiêu hao 20% HP) và trảm kích chữ X xé toạc chiến trường gây <color=#E2E8F0><b>{duneUlti} TD cực lớn</b></color> cùng hiệu ứng <color=#FF6B6B><b>Chảy Máu Sa Mạc</b></color>."
                     : $"Summons sand vortex (costs 20% HP) and executes a devastating Cross-X Slash inflicting massive <color=#E2E8F0><b>{duneUlti} TD</b></color> with continuous <color=#FF6B6B><b>Sand Bleed</b></color>."
            };
        }
        else if (isTitan)
        {
            skillTags = new string[] {
                "ATK",
                isVi ? "CHIẾN XA" : "CHARGE",
                "ATK"
            };
            skillNames = new string[] {
                isVi ? "1. ĐẠI CHÙY & THẾ THỦ NỆN ĐẤT" : "1. WAR HAMMER & EARTHSHAKER",
                isVi ? "2. CHIẾN XA XOAY BÚA 360°" : "2. JUGGERNAUT 360° SPIN",
                isVi ? "3. CUỒNG NỘ CHẤN ĐỊA DUNG NHAM" : "3. VOLCANIC CATACLYSM SLAM"
            };
            int titanUlti = 95 + (totalAtk * 2);
            skillDescs = new string[] {
                isVi ? $"Nện đại chùy càn quét gây <color=#FF3B30><b>{totalAtk} ATK</b></color> (Nội tại: Giảm 20% sát thương). <color=#FFDF66>Nhấn Giữ gồng Thế Thủ:</color> <color=#55FF88><b>Bất tử 100%</b></color>, thả tay nện đất bộc phá gây <color=#E2E8F0><b>{Mathf.RoundToInt(totalAtk * 1.5f)} TD</b></color> + <color=#FFAA33><b>Choáng 1.0s</b></color>!"
                     : $"Massive hammer smashes inflicting <color=#FF3B30><b>{totalAtk} ATK</b></color> (Innate 20% DR). <color=#FFDF66>Hold attack for Earthshaker:</color> <color=#55FF88><b>100% Invulnerable</b></color>, release to smash ground inflicting <color=#E2E8F0><b>{Mathf.RoundToInt(totalAtk * 1.5f)} TD</b></color> + <color=#FFAA33><b>Stun 1.0s</b></color>!",
                isVi ? $"Lướt tốc độ cao xoay búa 360° càn quét gây <color=#FF3B30><b>{totalAtk} ATK</b></color> & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>."
                     : $"Heavy spin dash slashing all enemies along path with <color=#FF3B30><b>{totalAtk} ATK</b></color> & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>.",
                isVi ? $"Hút toàn bộ quái vật vào tâm chấn, bật nhảy nện búa dung nham bộc phát chấn động gây <color=#FF3B30><b>{titanUlti} ATK cực đại</b></color> & hất tung kẻ địch."
                     : $"Pulls enemies into epicenter and leaps to smash down volcanic cataclysmic shockwaves with massive <color=#FF3B30><b>{titanUlti} ATK</b></color>."
            };
        }
        else if (isAzure)
        {
            skillTags = new string[] {
                "AP",
                isVi ? "LƯỚT" : "DASH",
                "AP"
            };
            skillNames = new string[] {
                isVi ? "1. TRỌNG KIẾM & SÓNG BĂNG KIẾM KHÍ" : "1. FROST SWORD WAVES",
                isVi ? "2. HÀN BĂNG TRƯỢT TUYẾT" : "2. FROST GLIDE DASH",
                isVi ? "3. BÃO SÓNG KIẾM NAN QUẠT" : "3. CRESCENT TEMPEST"
            };
            int azureUlti = (48 + totalAtk) * 5;
            skillDescs = new string[] {
                isVi ? $"Chém phóng <color=#55FFFF><b>Sóng Kiếm Băng</b></color> gây <color=#B84DFF><b>{totalAtk} AP</b></color> xuyên thấu kẻ địch (tầm 6.5m), trúng đích hồi <color=#38BDF8><b>+6 Mana</b></color> & giảm 50% thời gian chờ đòn đánh."
                     : $"Heavy slashes unleashing <color=#55FFFF><b>Frost Sword Waves</b></color> piercing enemies with <color=#B84DFF><b>{totalAtk} AP</b></color> (6.5m), restores <color=#38BDF8><b>+6 Mana</b></color> & cuts attack cooldown 50%.",
                isVi ? "Lướt né đòn trên dải băng tuyết & <color=#55FFFF><b>Hồi ngay lập tức đòn đánh thường</b></color> kế tiếp."
                     : "Swift glide dash dodging attacks & <color=#55FFFF><b>Instantly resets basic attack cooldown</b></color>.",
                isVi ? $"Tụ nội lực phóng 5 đợt kiếm khí băng lam nan quạt khổng lồ gây <color=#B84DFF><b>{azureUlti} AP diện rộng</b></color> quét sạch toàn bộ chiến trường."
                     : $"Releases 5 massive crescent frost sword waves in a fan spread across the battlefield inflicting devastating AoE <color=#B84DFF><b>{azureUlti} AP</b></color>."
            };
        }
        else if (isRonin)
        {
            skillTags = new string[] {
                "ATK / AP",
                isVi ? "TỐC BIẾN" : "WARP",
                "TD"
            };
            skillNames = new string[] {
                isVi ? "1. PHONG NGUYỆT TRẢM & TỤ LỰC IAIDO" : "1. GALE CRESCENT SWEEP & IAIDO",
                isVi ? "2. TÀN ẢNH TỐC BIẾN (MURAD STYLE)" : "2. SHADOW PHANTOM WARP",
                isVi ? "3. ÁM ẢNH PHÂN THÂN TRẢM" : "3. PHANTOM FLASH CLEAVE"
            };
            int roninUlti = (28 * 3) + (totalAtk * 2);
            skillDescs = new string[] {
                isVi ? $"Chém 180° gây <color=#FF3B30><b>{totalAtk} ATK</b></color> (hồi <color=#38BDF8><b>+10 Mana</b></color>) tích <color=#55FFFF><b>Phong Ấn</b></color> (3 tầng nổ <color=#B84DFF><b>{totalAtk * 2} AP 240% Crit</b></color> + Lốc Xoáy). Khi có Khiên: Gồng chém rách không gian phóng Nguyệt Kiếm Khí <color=#FF3B30><b>x2.85 Crit ATK</b></color>!"
                     : $"180° slashes dealing <color=#FF3B30><b>{totalAtk} ATK</b></color> (+10 Mana) & stacking <color=#55FFFF><b>Wind Marks</b></color> (3 stacks bursts for <color=#B84DFF><b>{totalAtk * 2} AP 240% Crit</b></color> + Vortex). When Shielded: Charge for <color=#FF3B30><b>x2.85 Crit ATK</b></color>!",
                isVi ? "Nhịp 1 tốc biến 4.8m tới trước (<color=#55FF88><b>Bất tử</b></color>) cắm bóng. Nhịp 2 Teleport quay về bóng & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>!"
                : "Stage 1 warps 4.8m forward leaving clone (<color=#55FF88><b>Invincible</b></color>). Stage 2 Warps back to clone & <color=#FFAA33><b>Cuts Ulti CD by 2.0s</b></color>!",
                isVi ? $"Lướt ảo ảnh chớp nhoáng trảm sát liên hoàn mọi kẻ địch trong không gian gây <color=#E2E8F0><b>{roninUlti} TD (bỏ qua giáp)</b></color>, <color=#55FF88><b>Bất tử hoàn toàn</b></color> trong lúc xuất chiêu (Kết liễu hồi ngay chiêu)!"
                     : $"Executes blinding phantom dash slashes dealing devastating <color=#E2E8F0><b>{roninUlti} TD (ignores defense)</b></color> with <color=#55FF88><b>Complete Invincibility</b></color> (Instant Ulti reset on kill)!"
            };
        }
        else
        {
            skillTags = new string[] {
                "ATK",
                isVi ? "THÁNH THUẪN" : "SHIELD",
                "ATK"
            };
            skillNames = new string[] {
                isVi ? "1. ĐẠI KIẾM & THẾ THỦ PHẢN KÍCH" : "1. ANCIENT SWORD & DIVINE PARRY",
                isVi ? "2. THÁNH THUẪN PHẢN ĐÒN" : "2. RADIANT RIPOSTE DASH",
                isVi ? "3. THÁNH KIẾM THIÊN NHAM" : "3. HOLY SWORD TEMPEST"
            };
            int vaelUlti = (20 + (int)(totalAtk * 0.4f)) * 2 + (25 + (int)(totalAtk * 0.5f)) + (50 + totalAtk);
            skillDescs = new string[] {
                isVi ? $"Chém kiếm 3 nhịp gây <color=#FF3B30><b>{totalAtk} ATK</b></color> (hồi <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66>Nhấn Giữ Thế Thủ:</color> Giảm 20% sát thương, thả tay bộc phá Thần Kiếm Phản Kích gây <color=#FF3B30><b>x3.2 Crit ATK</b></color>!"
                     : $"3-hit combo dealing <color=#FF3B30><b>{totalAtk} ATK</b></color> (restores <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66>Hold attack for Parry:</color> 20% DR, release to unleash Divine Riposte dealing <color=#FF3B30><b>x3.2 Crit ATK</b></color>!",
                isVi ? $"Lướt né đòn (<color=#55FF88><b>Bất tử khi lướt</b></color>). Lướt qua quái/đỡ đòn sẽ <color=#FFAA33><b>Hồi 50% CD Lướt</b></color>, hồi <color=#38BDF8><b>+15 Mana</b></color> & phóng 3 Thánh Kiếm Khí gây <color=#FF3B30><b>{totalAtk} ATK</b></color>!"
                     : $"Holy aegis dash (<color=#55FF88><b>Invulnerable</b></color>). Deflecting attacks <color=#FFAA33><b>Refunds 50% Dash CD</b></color>, restores <color=#38BDF8><b>+15 Mana</b></color> & fires 3 Holy Waves with <color=#FF3B30><b>{totalAtk} ATK</b></color>!",
                isVi ? $"Xoay bão lốc kiếm 360° hút quái vật và triệu hồi Cự Kiếm Thiên Nham giáng thế gây <color=#FF3B30><b>{vaelUlti} ATK cực đại</b></color> diện rộng."
                     : $"360° sword vortex storm pulling enemies and summons a gigantic Holy Sword slamming down with massive AoE <color=#FF3B30><b>{vaelUlti} ATK</b></color>."
            };
        }

        float[] baseCds = GetHeroBaseCooldowns(heroId);

        for (int i = 0; i < 3; i++)
        {
            int skillIdx = i;
            GameObject card = new GameObject($"SkillCard_{skillIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.layer = 5;
            card.transform.SetParent(skillsContainer, false);
            skillCardObjs.Add(card);

            RectTransform cRt = card.GetComponent<RectTransform>();
            float rowTop = 1f - i * (1f / 3f);
            float rowBot = 1f - (i + 1) * (1f / 3f);
            cRt.anchorMin = new Vector2(0f, rowBot);
            cRt.anchorMax = new Vector2(1f, rowTop);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.offsetMin = new Vector2(0f, 4f);
            cRt.offsetMax = new Vector2(0f, -4f);

            Image cImg = card.GetComponent<Image>();
            cImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);

            Color outlineCol = (skillIdx == 0) ? new Color(1f, 0.85f, 0.35f, 0.7f)
                             : (skillIdx == 1 ? new Color(0.35f, 0.90f, 0.65f, 0.7f) : new Color(0.95f, 0.45f, 0.45f, 0.7f));
            WhiteIconLoader.ApplyGothicFrame(card, outlineCol, 0.55f);

            // Large Icon Badge on Left (85x85px) with Gothic frame
            GameObject iconBadge = new GameObject("IconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconBadge.layer = 5;
            iconBadge.transform.SetParent(card.transform, false);
            RectTransform ibRt = iconBadge.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(14f, 0f);
            ibRt.sizeDelta = new Vector2(85f, 85f);
            Image ibImg = iconBadge.GetComponent<Image>();
            ibImg.color = new Color(0.08f, 0.12f, 0.20f, 1f);
            WhiteIconLoader.ApplyGothicFrame(iconBadge, outlineCol, 0.40f);

            Sprite skillSpr = LoadHeroSkillSprite(heroId, skillIdx);
            if (skillSpr != null)
            {
                GameObject iconImg = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconImg.layer = 5;
                iconImg.transform.SetParent(iconBadge.transform, false);
                RectTransform iiRt = iconImg.GetComponent<RectTransform>();
                iiRt.anchorMin = Vector2.zero;
                iiRt.anchorMax = Vector2.one;
                iiRt.sizeDelta = new Vector2(-6f, -6f);
                Image ii = iconImg.GetComponent<Image>();
                ii.sprite = skillSpr;
                ii.preserveAspect = true;
                ii.raycastTarget = false;
            }

            Color tagCol = outlineCol;
            string sTagUpper = skillTags[skillIdx].ToUpper();
            if (sTagUpper.Contains("AP"))
                tagCol = new Color(0.72f, 0.35f, 1f); // Tím
            else if (sTagUpper.Contains("TD"))
                tagCol = new Color(0.92f, 0.95f, 1f); // Trắng Bạc
            else if (sTagUpper.Contains("ATK"))
                tagCol = new Color(1f, 0.23f, 0.23f); // Đỏ

            // Tính toán Cooldown động chính xác cập nhật theo ngọc khảm, nâng cấp và lõi
            float baseCd = (skillIdx < baseCds.Length) ? baseCds[skillIdx] : 0f;
            float effCd = 0f;
            if (baseCd > 0.05f)
            {
                float clampedCdr = Mathf.Clamp(finalCd, 0f, 0.60f);
                if (skillIdx == 0) effCd = Mathf.Max(0.5f, baseCd * (1f - clampedCdr));
                else if (skillIdx == 1) effCd = Mathf.Max(1.0f, (baseCd * (1f - clampedCdr)) - upDashCd);
                else effCd = Mathf.Max(3.0f, (baseCd * (1f - clampedCdr)) - upUltiCd);
            }

            string cleanTag = skillTags[skillIdx].Trim('[', ']', ' ');
            float badgeW = Mathf.Max(60f, 28f + cleanTag.Length * 10f);
            float topGroupW = badgeW + 85f;

            // Nhóm Header góc phải của chiêu: Tag Badge + Cooldown Badge (luôn cập nhật theo chỉ số cộng thêm)
            GameObject topGroup = new GameObject("TopRightGroup", typeof(RectTransform));
            topGroup.layer = 5;
            topGroup.transform.SetParent(card.transform, false);
            RectTransform trRt = topGroup.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(1f, 1f);
            trRt.anchorMax = new Vector2(1f, 1f);
            trRt.pivot = new Vector2(1f, 1f);
            trRt.anchoredPosition = new Vector2(-14f, -7f);
            trRt.sizeDelta = new Vector2(topGroupW, 24f);

            // 1. Tag Badge với 2 hoa văn Gothic đối xứng 2 bên, chữ ở giữa KHÔNG có vạch đè lên chữ
            GameObject tagBadge = new GameObject("TagBadge", typeof(RectTransform));
            tagBadge.layer = 5;
            tagBadge.transform.SetParent(topGroup.transform, false);
            RectTransform tagRt = tagBadge.GetComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0f, 0.5f);
            tagRt.anchorMax = new Vector2(0f, 0.5f);
            tagRt.pivot = new Vector2(0f, 0.5f);
            tagRt.anchoredPosition = Vector2.zero;
            tagRt.sizeDelta = new Vector2(badgeW, 22f);

            Sprite bracketSpr = WhiteIconLoader.GetGothicBracketSprite();

            // Hoa văn ngoặc trái [ (lật ngang đối xứng)
            GameObject leftB = new GameObject("LeftBracket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            leftB.layer = 5;
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
            lbImg.color = tagCol;
            lbImg.preserveAspect = true;
            lbImg.raycastTarget = false;

            // Chữ Tag ở giữa
            GameObject tagObj = new GameObject("TagText", typeof(RectTransform), typeof(CanvasRenderer));
            tagObj.layer = 5;
            tagObj.transform.SetParent(tagBadge.transform, false);
            RectTransform ttRt = tagObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.offsetMin = new Vector2(14f, 0f);
            ttRt.offsetMax = new Vector2(-14f, 0f);
            var tagTmp = tagObj.AddComponent<TextMeshProUGUI>();
            tagTmp.text = $"<b>{cleanTag}</b>";
            tagTmp.fontSize = 13.5f;
            tagTmp.fontStyle = FontStyles.Bold;
            tagTmp.alignment = TextAlignmentOptions.Center;
            tagTmp.color = tagCol;
            tagTmp.enableWordWrapping = false;
            tagTmp.raycastTarget = false;

            // Hoa văn ngoặc phải ]
            GameObject rightB = new GameObject("RightBracket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rightB.layer = 5;
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
            rbImg.color = tagCol;
            rbImg.preserveAspect = true;
            rbImg.raycastTarget = false;

            // 2. Cooldown Badge ở góc phải
            GameObject cdBadge = new GameObject("CooldownBadge", typeof(RectTransform));
            cdBadge.layer = 5;
            cdBadge.transform.SetParent(topGroup.transform, false);
            RectTransform cdRt = cdBadge.GetComponent<RectTransform>();
            cdRt.anchorMin = new Vector2(1f, 0.5f);
            cdRt.anchorMax = new Vector2(1f, 0.5f);
            cdRt.pivot = new Vector2(1f, 0.5f);
            cdRt.anchoredPosition = Vector2.zero;
            cdRt.sizeDelta = new Vector2(80f, 22f);

            GameObject cdObj = new GameObject("CDText", typeof(RectTransform), typeof(CanvasRenderer));
            cdObj.layer = 5;
            cdObj.transform.SetParent(cdBadge.transform, false);
            RectTransform cdtRt = cdObj.GetComponent<RectTransform>();
            cdtRt.anchorMin = Vector2.zero;
            cdtRt.anchorMax = Vector2.one;
            cdtRt.sizeDelta = Vector2.zero;
            var cdTmp = cdObj.AddComponent<TextMeshProUGUI>();
            cdTmp.fontSize = 13.5f;
            cdTmp.fontStyle = FontStyles.Bold;
            cdTmp.alignment = TextAlignmentOptions.MidlineRight;
            cdTmp.enableWordWrapping = false;
            cdTmp.raycastTarget = false;
            cdTmp.text = (effCd > 0f) ? $"<color=#38BDF8><b>CD: {effCd:0.#}s</b></color>" : "<color=#94A3B8><b>CD: 0s</b></color>";

            // Title (Bắt đầu sau IconBadge bên trái, dừng trước TopRightGroup, tuyệt đối không đè nhau)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            titleObj.layer = 5;
            titleObj.transform.SetParent(card.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0f, 1f);
            tRt.anchoredPosition = new Vector2(112f, -7f);
            tRt.sizeDelta = new Vector2(-(112f + topGroupW + 16f), 26f);
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = $"<b>{skillNames[skillIdx]}</b>";
            tTmp.fontSize = 17f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = Color.white;
            tTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Description
            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer));
            descObj.layer = 5;
            descObj.transform.SetParent(card.transform, false);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.offsetMin = new Vector2(112f, 6f);
            dRt.offsetMax = new Vector2(-12f, -34f);
            var dTmp = descObj.AddComponent<TextMeshProUGUI>();
            dTmp.text = skillDescs[skillIdx];
            dTmp.fontSize = 13.5f;
            dTmp.lineSpacing = 1.5f;
            dTmp.color = new Color(0.88f, 0.94f, 1f);
            dTmp.enableWordWrapping = true;
            dTmp.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private void RenderBagGems()
    {
        if (bagContentContainer == null) return;

        foreach (var o in bagItemCardObjs) if (o != null) Destroy(o);
        bagItemCardObjs.Clear();

        string curHero = EquipmentSocketManager.GetActiveHeroId();

        if (bagSectionHeaderText != null)
        {
            bagSectionHeaderText.text = LocalizationManager.T(
                "VẬT PHẨM & TRANG BỊ TRONG TÚI (KÉO VÀO 6 Ô TRÊN ĐỂ TRANG BỊ)",
                "ITEMS, WEAPONS & GEMS IN BAG (DRAG INTO ANY OF THE 6 HERO SLOTS TO EQUIP)");
            bagSectionHeaderText.fontSize = 18.5f;
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
            // Parent lên Viewport / ScrollView để không bị dính ô GridLayoutGroup (340x108) ép lệch sang góc trái
            Transform parentContainer = (bagContentContainer.parent != null) ? bagContentContainer.parent : bagContentContainer;
            emptyMsgObj.transform.SetParent(parentContainer, false);
            bagItemCardObjs.Add(emptyMsgObj);

            RectTransform emRt = emptyMsgObj.GetComponent<RectTransform>();
            emRt.anchorMin = Vector2.zero;
            emRt.anchorMax = Vector2.one;
            emRt.pivot = new Vector2(0.5f, 0.5f);
            emRt.offsetMin = new Vector2(40f, 20f);
            emRt.offsetMax = new Vector2(-40f, -20f);

            var emTmp = emptyMsgObj.AddComponent<TextMeshProUGUI>();
            emTmp.text = LocalizationManager.T(
                "Chưa có kiếm, khiên hoặc khoáng thạch trong trận!\nHãy diệt quái vật hoặc đập vỡ Rương Kho Báu để thu thập trang bị.",
                "No weapons, shields or minerals in bag yet!\nDefeat dungeon monsters or open Treasure Chests to gather items.");
            emTmp.fontSize = 20.5f;
            emTmp.fontStyle = FontStyles.Bold;
            emTmp.color = new Color(0.78f, 0.84f, 0.95f);
            emTmp.alignment = TextAlignmentOptions.Center;
            emTmp.enableWordWrapping = true;
            emTmp.lineSpacing = 4f;
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
            cRt.sizeDelta = new Vector2(340f, 108f);

            Image cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.06f, 0.08f, 0.13f, 0.95f);
            WhiteIconLoader.ApplyGothicFrame(cardObj, gemEff.Value.gemColor * 0.85f, 0.65f);

            // Drag & Drop Component
            var dragItem = cardObj.AddComponent<UIDragDropItem>();
            dragItem.itemId = gemEntry.itemId;
            dragItem.itemIcon = gemDef.Icon;
            dragItem.itemColor = gemEff.Value.gemColor;
            dragItem.parentScrollRect = bagContentContainer.GetComponentInParent<ScrollRect>();
            string gemIdToSocket = gemEntry.itemId;
            dragItem.onClick = () =>
            {
                if (EquipmentSocketManager.SocketGemToHeroSlot(selectedSocketSlotIndex, gemIdToSocket, out _, out _, curHero))
                {
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    NotifyPlayerCombatStatsChanged();
                    RefreshModal();
                }
            };

            // Large Icon Box
            GameObject ibObj = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ibObj.layer = 5;
            ibObj.transform.SetParent(cardObj.transform, false);
            RectTransform ibRt = ibObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(10f, 0f);
            ibRt.sizeDelta = new Vector2(68f, 68f);
            Image ibImg = ibObj.GetComponent<Image>();
            if (sprSlotFrame != null) { ibImg.sprite = sprSlotFrame; ibImg.type = Image.Type.Sliced; }
            ibImg.color = gemEff.Value.gemColor;

            GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icObj.layer = 5;
            icObj.transform.SetParent(ibObj.transform, false);
            RectTransform icRt = icObj.GetComponent<RectTransform>();
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.sizeDelta = new Vector2(-6f, -6f);
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
            infoRt.offsetMin = new Vector2(86f, 4f);
            infoRt.offsetMax = new Vector2(-8f, -4f);

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
            tTmp.text = $"<b>{gName}</b> <size=15><color=#00FF88>{qtyStr}</color></size>";
            tTmp.fontSize = 16.5f;
            tTmp.color = Color.white;
            tTmp.raycastTarget = false;

            // Buff Summary
            GameObject bObj = new GameObject("Buff", typeof(RectTransform), typeof(CanvasRenderer));
            bObj.layer = 5;
            bObj.transform.SetParent(infoObj.transform, false);
            var bTmp = bObj.AddComponent<TextMeshProUGUI>();
            string effSum = LocalizationManager.IsVietnamese ? gemEff.Value.effectSummaryVi : gemEff.Value.effectSummaryEn;
            bTmp.text = EquipmentSocketManager.FormatColoredStatSummary(effSum);
            bTmp.fontSize = 14f;
            bTmp.lineSpacing = 1.2f;
            bTmp.fontStyle = FontStyles.Bold;
            bTmp.color = Color.white;
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
            btnRt.anchoredPosition = new Vector2(-78f, -20f);
            btnRt.sizeDelta = new Vector2(46f, 46f);

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

            WhiteIconLoader.ReplaceTextWithIcon(btn, "Bag", new Color(1f, 0.85f, 0.35f), new Vector2(28f, 28f));
        }

        // 2. InGameBagPanel
        Transform existingPanel = rootCanvas.transform.Find("InGameInventoryModal");
        if (existingPanel != null)
        {
            var comp = existingPanel.GetComponent<InGameInventoryUI>();
            bool hasOldAutoMerge = existingPanel.Find("DialogBox/TopSection_Overview/Btn_AutoMergeAll") != null;
            if (comp != null && !hasOldAutoMerge) return comp;

            if (Application.isPlaying) Destroy(existingPanel.gameObject);
            else DestroyImmediate(existingPanel.gameObject);
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
        modalCanvas.sortingOrder = 28000;
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
        dImg.color = new Color(0.03f, 0.04f, 0.07f, 1f);
        WhiteIconLoader.ApplyGothicFrame(dialogBox, new Color(0.85f, 0.72f, 0.38f, 0.90f), 0.35f);

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
        htRt.anchoredPosition = new Vector2(0f, -16f);
        htRt.sizeDelta = new Vector2(0f, 36f);

        uiComp.titleText = hTitleObj.AddComponent<TextMeshProUGUI>();
        uiComp.titleText.text = LocalizationManager.T("TRANG BỊ & NGỌC ANH HÙNG", "HERO GEAR & RUNIC GEMS");
        uiComp.titleText.fontSize = 29;
        uiComp.titleText.fontStyle = FontStyles.Bold;
        uiComp.titleText.alignment = TextAlignmentOptions.Center;
        uiComp.titleText.color = new Color(1f, 0.85f, 0.35f);

        // Subtitle
        GameObject subObj = new GameObject("HeaderSubtitle", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(dialogBox.transform, false);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -52f);
        sRt.sizeDelta = new Vector2(0f, 24f);

        uiComp.subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        uiComp.subtitleText.fontSize = 17f;
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
        cRt.anchoredPosition = new Vector2(-25f, -14f);
        cRt.sizeDelta = new Vector2(44f, 44f);

        Image cImg = closeBtnObj.GetComponent<Image>();
        cImg.color = new Color(0.75f, 0.22f, 0.25f, 1f);
        WhiteIconLoader.ApplyGothicFrame(closeBtnObj, new Color(1f, 0.85f, 0.40f, 0.85f), 0.35f);

        Button cBtn = closeBtnObj.GetComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.onClick.AddListener(uiComp.Close);
        WhiteIconLoader.ReplaceTextWithIcon(cBtn, "Cross", Color.white, new Vector2(24f, 24f));

        // TOP SECTION: HERO STAGE (Avatar + 6 Sockets) + STATS + 3 SKILL CARDS
        GameObject topSection = new GameObject("TopSection_HeroSockets", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topSection.layer = 5;
        topSection.transform.SetParent(dialogBox.transform, false);
        RectTransform tsRt = topSection.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 0.40f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.pivot = new Vector2(0.5f, 0.5f);
        tsRt.offsetMin = new Vector2(16f, 6f);
        tsRt.offsetMax = new Vector2(-16f, -76f);
        Image tsImg = topSection.GetComponent<Image>();
        tsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);
        WhiteIconLoader.ApplyGothicFrame(topSection, new Color(0.35f, 0.45f, 0.60f, 0.60f), 0.50f);


        // 2. Hero Stage Box (Avatar + 6 Sockets) on the Left (Spacious 26% width framing 6 sockets at 6 corners)
        GameObject stageBox = new GameObject("HeroStageBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        stageBox.layer = 5;
        stageBox.transform.SetParent(topSection.transform, false);
        RectTransform sbRt = stageBox.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0f);
        sbRt.anchorMax = new Vector2(0.26f, 1f);
        sbRt.pivot = new Vector2(0f, 0.5f);
        sbRt.offsetMin = new Vector2(10f, 8f);
        sbRt.offsetMax = new Vector2(-4f, -8f);

        Image stgImg = stageBox.GetComponent<Image>();
        stgImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(stageBox, new Color(0.85f, 0.72f, 0.38f, 0.70f), 0.50f);

        // 6 Socket Slot Positions spread out with clean vertical spacing (no overlap)
        Vector2[] socketPositions = new Vector2[]
        {
            new Vector2(-148f, 85f),   // Slot 0: Vũ Khí (Top-Left)
            new Vector2(-156f, 0f),    // Slot 1: Áo Giáp (Mid-Left)
            new Vector2(-148f, -85f),  // Slot 2: Giày (Bot-Left)
            new Vector2(148f, 85f),    // Slot 3: Mũ Nón (Top-Right)
            new Vector2(156f, 0f),     // Slot 4: Nhẫn (Mid-Right)
            new Vector2(148f, -85f)    // Slot 5: Hộ Phù (Bot-Right)
        };

        // 6 Glowing Conduit Lines connecting 6 sockets to center Avatar (rendered underneath)
        uiComp.socketConduitLines.Clear();
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
            uiComp.socketConduitLines.Add(lineImg);
        }

        // Center Avatar Box (Clean seamless container, no extra solid box or outline behind hero)
        GameObject animBox = new GameObject("ShowcaseAnimBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
        animBox.layer = 5;
        animBox.transform.SetParent(stageBox.transform, false);
        RectTransform aRt = animBox.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.5f, 0.5f);
        aRt.anchorMax = new Vector2(0.5f, 0.5f);
        aRt.pivot = new Vector2(0.5f, 0.5f);
        aRt.anchoredPosition = Vector2.zero;
        aRt.sizeDelta = new Vector2(120f, 180f);

        GameObject animImgObj = new GameObject("HeroLiveSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        animImgObj.layer = 5;
        animImgObj.transform.SetParent(animBox.transform, false);
        RectTransform aiRt = animImgObj.GetComponent<RectTransform>();
        aiRt.anchorMin = Vector2.zero;
        aiRt.anchorMax = Vector2.one;
        aiRt.sizeDelta = Vector2.zero;
        uiComp.heroAvatarPreviewImage = animImgObj.GetComponent<Image>();
        uiComp.heroAvatarPreviewImage.preserveAspect = true;

        uiComp.socketSlotObjs.Clear();
        for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT; s++)
        {
            int slotIdx = s;
            GameObject sObj = new GameObject($"HeroSlot_{slotIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            sObj.layer = 5;
            sObj.transform.SetParent(stageBox.transform, false);
            uiComp.socketSlotObjs.Add(sObj);

            RectTransform sSlotRt = sObj.GetComponent<RectTransform>();
            sSlotRt.anchorMin = new Vector2(0.5f, 0.5f);
            sSlotRt.anchorMax = new Vector2(0.5f, 0.5f);
            sSlotRt.pivot = new Vector2(0.5f, 0.5f);
            sSlotRt.anchoredPosition = socketPositions[slotIdx];
            sSlotRt.sizeDelta = new Vector2(68f, 68f);

            Image sImg = sObj.GetComponent<Image>();
            sImg.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            var sOutline = sObj.AddComponent<Outline>();
            sOutline.effectColor = new Color(0.45f, 0.55f, 0.70f, 0.6f);
            sOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var dropTarget = sObj.AddComponent<UIDropSocketTarget>();
            dropTarget.socketSlotIndex = slotIdx;
            dropTarget.onGemDropped = (droppedGemId, targetSlot) =>
            {
                if (EquipmentSocketManager.SocketGemToHeroSlot(targetSlot, droppedGemId, out _, out _))
                {
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    uiComp.NotifyPlayerCombatStatsChanged();
                    uiComp.RefreshModal();
                }
            };

            Button slotBtn = sObj.GetComponent<Button>();
            slotBtn.onClick.AddListener(() =>
            {
                uiComp.selectedSocketSlotIndex = slotIdx;
                uiComp.RefreshModal();
                SoundManager.EnsureInstance()?.PlayMenuHover();
            });
        }

        // 4. Hero Name, Role & Combat Stats beside StageBox (Expansive Center-Left Column: 26% to 54%)
        GameObject infoGroup = new GameObject("HeroInfoGroup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        infoGroup.layer = 5;
        infoGroup.transform.SetParent(topSection.transform, false);
        RectTransform igRt = infoGroup.GetComponent<RectTransform>();
        igRt.anchorMin = new Vector2(0.26f, 0f);
        igRt.anchorMax = new Vector2(0.54f, 1f);
        igRt.pivot = new Vector2(0.5f, 0.5f);
        igRt.offsetMin = new Vector2(4f, 8f);
        igRt.offsetMax = new Vector2(-4f, -8f);

        Image igImg = infoGroup.GetComponent<Image>();
        igImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        WhiteIconLoader.ApplyGothicFrame(infoGroup, new Color(0.35f, 0.45f, 0.60f, 0.60f), 0.55f);

        GameObject nameObj = new GameObject("HeroName", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.layer = 5;
        nameObj.transform.SetParent(infoGroup.transform, false);
        RectTransform nRt = nameObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(14f, -10f);
        nRt.sizeDelta = new Vector2(-28f, 32f);
        uiComp.heroNameText = nameObj.AddComponent<TextMeshProUGUI>();
        uiComp.heroNameText.fontSize = 25f;
        uiComp.heroNameText.fontStyle = FontStyles.Bold;
        uiComp.heroNameText.color = new Color(1f, 0.88f, 0.45f);
        uiComp.heroNameText.enableWordWrapping = false;
        uiComp.heroNameText.overflowMode = TextOverflowModes.Ellipsis;

        GameObject roleObj = new GameObject("HeroRole", typeof(RectTransform), typeof(CanvasRenderer));
        roleObj.layer = 5;
        roleObj.transform.SetParent(infoGroup.transform, false);
        RectTransform rlRt = roleObj.GetComponent<RectTransform>();
        rlRt.anchorMin = new Vector2(0f, 1f);
        rlRt.anchorMax = new Vector2(1f, 1f);
        rlRt.pivot = new Vector2(0f, 1f);
        rlRt.anchoredPosition = new Vector2(14f, -48f);
        rlRt.sizeDelta = new Vector2(-28f, 24f);
        uiComp.heroRoleText = roleObj.AddComponent<TextMeshProUGUI>();
        uiComp.heroRoleText.fontSize = 16.5f;
        uiComp.heroRoleText.color = new Color(0.45f, 0.85f, 1f);
        uiComp.heroRoleText.enableWordWrapping = false;
        uiComp.heroRoleText.overflowMode = TextOverflowModes.Ellipsis;

        GameObject statsObj = new GameObject("HeroStats", typeof(RectTransform), typeof(CanvasRenderer));
        statsObj.layer = 5;
        statsObj.transform.SetParent(infoGroup.transform, false);
        RectTransform stRt = statsObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 1f);
        stRt.anchorMax = new Vector2(1f, 1f);
        stRt.pivot = new Vector2(0f, 1f);
        stRt.anchoredPosition = new Vector2(14f, -76f);
        stRt.sizeDelta = new Vector2(-28f, 255f);
        uiComp.heroStatsText = statsObj.AddComponent<TextMeshProUGUI>();
        uiComp.heroStatsText.fontSize = 14.5f;
        uiComp.heroStatsText.fontStyle = FontStyles.Bold;
        uiComp.heroStatsText.lineSpacing = 1.15f;
        uiComp.heroStatsText.color = new Color(0.95f, 0.95f, 0.95f);

        // Hint/Tip in HeroInfoGroup
        GameObject hintObj = new GameObject("HeroTip", typeof(RectTransform), typeof(CanvasRenderer));
        hintObj.layer = 5;
        hintObj.transform.SetParent(infoGroup.transform, false);
        RectTransform htRt2 = hintObj.GetComponent<RectTransform>();
        htRt2.anchorMin = new Vector2(0f, 0f);
        htRt2.anchorMax = new Vector2(1f, 0f);
        htRt2.pivot = new Vector2(0f, 0f);
        htRt2.anchoredPosition = new Vector2(14f, 6f);
        htRt2.sizeDelta = new Vector2(-28f, 36f);
        var htTmp = hintObj.AddComponent<TextMeshProUGUI>();
        htTmp.text = LocalizationManager.T(
            "<color=#FBBF24>[Mẹo]:</color> <color=#94A3B8>Kéo vũ khí, giáp & ngọc vào 6 ô xung quanh để kích hoạt thêm chỉ số chiến đấu!</color>",
            "<color=#FBBF24>[Tip]:</color> <color=#94A3B8>Drag weapons, armor & gems into the 6 slots to activate battle stat boosts!</color>"
        );
        htTmp.fontSize = 12.5f;
        htTmp.enableWordWrapping = true;
        htTmp.lineSpacing = -2f;

        // 5. 3 Skill Breakdown Cards Container (Right column of TopSection: 54% to 100%)
        GameObject skillsBox = new GameObject("SkillsContainer", typeof(RectTransform));
        skillsBox.layer = 5;
        skillsBox.transform.SetParent(topSection.transform, false);
        RectTransform skcRt = skillsBox.GetComponent<RectTransform>();
        skcRt.anchorMin = new Vector2(0.54f, 0f);
        skcRt.anchorMax = new Vector2(1f, 1f);
        skcRt.pivot = new Vector2(0.5f, 0.5f);
        skcRt.offsetMin = new Vector2(6f, 8f);
        skcRt.offsetMax = new Vector2(-12f, -8f);
        uiComp.skillsContainer = skillsBox.transform;

        // BOTTOM SECTION: Bag Grid
        GameObject botSection = new GameObject("BottomSection_Bag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        botSection.layer = 5;
        botSection.transform.SetParent(dialogBox.transform, false);
        RectTransform bsRt = botSection.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0f, 0f);
        bsRt.anchorMax = new Vector2(1f, 0.40f);
        bsRt.pivot = new Vector2(0.5f, 0.5f);
        bsRt.offsetMin = new Vector2(16f, 14f);
        bsRt.offsetMax = new Vector2(-16f, -4f);
        Image bsImg = botSection.GetComponent<Image>();
        bsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);
        WhiteIconLoader.ApplyGothicFrame(botSection, new Color(0.35f, 0.45f, 0.60f, 0.60f), 0.50f);

        // Header
        GameObject bhObj = new GameObject("BagHeader", typeof(RectTransform), typeof(CanvasRenderer));
        bhObj.layer = 5;
        bhObj.transform.SetParent(botSection.transform, false);
        RectTransform bhRt = bhObj.GetComponent<RectTransform>();
        bhRt.anchorMin = new Vector2(0f, 1f);
        bhRt.anchorMax = new Vector2(1f, 1f);
        bhRt.pivot = new Vector2(0.5f, 1f);
        bhRt.anchoredPosition = new Vector2(0f, -6f);
        bhRt.sizeDelta = new Vector2(-250f, 24f);
        uiComp.bagSectionHeaderText = bhObj.AddComponent<TextMeshProUGUI>();
        uiComp.bagSectionHeaderText.fontSize = 18.5f;
        uiComp.bagSectionHeaderText.fontStyle = FontStyles.Bold;
        uiComp.bagSectionHeaderText.alignment = TextAlignmentOptions.Center;
        uiComp.bagSectionHeaderText.color = new Color(1f, 0.85f, 0.35f);

        // Auto Merge All Button (Top-Right of Bag Section)
        GameObject autoMergeObj = new GameObject("Btn_AutoMergeAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        autoMergeObj.layer = 5;
        autoMergeObj.transform.SetParent(botSection.transform, false);
        RectTransform amRt = autoMergeObj.GetComponent<RectTransform>();
        amRt.anchorMin = new Vector2(1f, 1f);
        amRt.anchorMax = new Vector2(1f, 1f);
        amRt.pivot = new Vector2(1f, 1f);
        amRt.anchoredPosition = new Vector2(-15f, -5f);
        amRt.sizeDelta = new Vector2(210f, 26f);

        Image amImg = autoMergeObj.GetComponent<Image>();
        amImg.color = new Color(0.92f, 0.55f, 0.08f, 1f);
        WhiteIconLoader.ApplyGothicFrame(autoMergeObj, new Color(1f, 0.85f, 0.40f, 0.85f), 0.35f);

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
        uiComp.autoMergeAllBtnText.fontSize = 15f;
        uiComp.autoMergeAllBtnText.fontStyle = FontStyles.Bold;
        uiComp.autoMergeAllBtnText.color = Color.white;
        uiComp.autoMergeAllBtnText.alignment = TextAlignmentOptions.Center;
        uiComp.autoMergeAllBtnText.enableWordWrapping = false;

        // Scroll View
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect));
        scrollObj.layer = 5;
        scrollObj.transform.SetParent(botSection.transform, false);
        RectTransform scrRt = scrollObj.GetComponent<RectTransform>();
        scrRt.anchorMin = Vector2.zero;
        scrRt.anchorMax = Vector2.one;
        scrRt.offsetMin = new Vector2(10f, 6f);
        scrRt.offsetMax = new Vector2(-10f, -32f);
        var scroll = scrollObj.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 35f;

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
        grid.cellSize = new Vector2(340f, 108f);
        grid.spacing = new Vector2(12f, 10f);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = cntRt;
        scroll.viewport = vpRt;
        uiComp.bagContentContainer = contentObj.transform;

        // Bottom Footer Bar (Loot Summary)
        GameObject footObj = new GameObject("FooterBar", typeof(RectTransform), typeof(CanvasRenderer));
        footObj.layer = 5;
        footObj.transform.SetParent(dialogBox.transform, false);
        RectTransform fRt = footObj.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0f);
        fRt.anchorMax = new Vector2(1f, 0f);
        fRt.pivot = new Vector2(0.5f, 0f);
        fRt.anchoredPosition = new Vector2(0f, 4f);
        fRt.sizeDelta = new Vector2(-30f, 28f);

        GameObject lootObj = new GameObject("LootSummary", typeof(RectTransform), typeof(CanvasRenderer));
        lootObj.layer = 5;
        lootObj.transform.SetParent(footObj.transform, false);
        RectTransform lootRt = lootObj.GetComponent<RectTransform>();
        lootRt.anchorMin = new Vector2(0f, 0.5f);
        lootRt.anchorMax = new Vector2(0f, 0.5f);
        lootRt.pivot = new Vector2(0f, 0.5f);
        lootRt.anchoredPosition = new Vector2(10f, 0f);
        lootRt.sizeDelta = new Vector2(900f, 26f);
        uiComp.lootSummaryText = lootObj.AddComponent<TextMeshProUGUI>();
        uiComp.lootSummaryText.fontSize = 17f;
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
                $"Đã nâng cấp {upgradedSlots} ô trang bị (+{totalLevelsGained} Cấp)!",
                $"Upgraded {upgradedSlots} gear sockets (+{totalLevelsGained} Lvs)!"
            );
            DamagePopup.CreateText(autoMergeAllBtn != null ? autoMergeAllBtn.transform.position : transform.position, notify, new Color(0.2f, 1f, 0.45f));
        }
    }
}
