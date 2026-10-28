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

    // 6 Mini Stat Badges
    [SerializeField] private TextMeshProUGUI hpStatText;
    [SerializeField] private TextMeshProUGUI atkStatText;
    [SerializeField] private TextMeshProUGUI critStatText;
    [SerializeField] private TextMeshProUGUI vampStatText;
    [SerializeField] private TextMeshProUGUI dashCdStatText;
    [SerializeField] private TextMeshProUGUI ultiCdStatText;

    // 4 Skill & Trait Detail Cards (Card 0: NỘI TẠI, Card 1: CHIÊU 1, Card 2: CHIÊU 2, Card 3: CHIÊU 3)
    [SerializeField] private TextMeshProUGUI passiveCardTitle;
    [SerializeField] private TextMeshProUGUI passiveCardDesc;
    [SerializeField] private Image passiveCardIcon;
    [SerializeField] private Outline passiveCardOutline;
    [SerializeField] private Outline passiveBadgeOutline;

    [SerializeField] private TextMeshProUGUI attackCardTitle;
    [SerializeField] private TextMeshProUGUI attackCardDesc;
    [SerializeField] private Image attackCardIcon;
    [SerializeField] private Outline attackCardOutline;
    [SerializeField] private Outline attackBadgeOutline;

    [SerializeField] private TextMeshProUGUI dashCardTitle;
    [SerializeField] private TextMeshProUGUI dashCardDesc;
    [SerializeField] private Image dashCardIcon;
    [SerializeField] private Outline dashCardOutline;
    [SerializeField] private Outline dashBadgeOutline;

    [SerializeField] private TextMeshProUGUI ultiCardTitle;
    [SerializeField] private TextMeshProUGUI ultiCardDesc;
    [SerializeField] private Image ultiCardIcon;
    [SerializeField] private Outline ultiCardOutline;
    [SerializeField] private Outline ultiBadgeOutline;

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

    private void EnsureCardReferences()
    {
        Transform dialogBox = transform.Find("DialogBox");
        if (dialogBox == null) return;
        Transform cardsSection = dialogBox.Find("CardsSection");
        if (cardsSection == null) return;

        if (attackCardIcon == null)
        {
            var t = cardsSection.Find("Card_Attack/IconBadge/Icon");
            if (t != null) attackCardIcon = t.GetComponent<Image>();
        }
        if (attackCardOutline == null)
        {
            var t = cardsSection.Find("Card_Attack");
            if (t != null) attackCardOutline = t.GetComponent<Outline>();
        }
        if (attackBadgeOutline == null)
        {
            var t = cardsSection.Find("Card_Attack/IconBadge");
            if (t != null) attackBadgeOutline = t.GetComponent<Outline>();
        }

        if (dashCardIcon == null)
        {
            var t = cardsSection.Find("Card_Dash/IconBadge/Icon");
            if (t != null) dashCardIcon = t.GetComponent<Image>();
        }
        if (dashCardOutline == null)
        {
            var t = cardsSection.Find("Card_Dash");
            if (t != null) dashCardOutline = t.GetComponent<Outline>();
        }
        if (dashBadgeOutline == null)
        {
            var t = cardsSection.Find("Card_Dash/IconBadge");
            if (t != null) dashBadgeOutline = t.GetComponent<Outline>();
        }

        if (ultiCardIcon == null)
        {
            var t = cardsSection.Find("Card_Ulti/IconBadge/Icon");
            if (t != null) ultiCardIcon = t.GetComponent<Image>();
        }
        if (ultiCardOutline == null)
        {
            var t = cardsSection.Find("Card_Ulti");
            if (t != null) ultiCardOutline = t.GetComponent<Outline>();
        }
        if (ultiBadgeOutline == null)
        {
            var t = cardsSection.Find("Card_Ulti/IconBadge");
            if (t != null) ultiBadgeOutline = t.GetComponent<Outline>();
        }
    }

    public void RefreshData()
    {
        EnsureCardReferences();

        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        var charMgr = CharacterManager.EnsureInstance();

        string selectedCharId = charMgr != null ? charMgr.GetSelectedCharacterId() : "Vaelstrider";
        bool isMage = (selectedCharId == CharacterManager.CHAR_CELESTIAL_MAGE || selectedCharId.ToLower().Contains("mage") || selectedCharId.ToLower().Contains("player6") || selectedCharId.ToLower().Contains("celestial"));
        bool isDune = (selectedCharId == CharacterManager.CHAR_DUNE_ASSASSIN || selectedCharId.ToLower().Contains("dune") || selectedCharId.ToLower().Contains("player5"));
        bool isTitan = (selectedCharId == CharacterManager.CHAR_TITAN_BERSERKER || selectedCharId.ToLower().Contains("titan") || selectedCharId.ToLower().Contains("player4"));
        bool isAzure = (selectedCharId == CharacterManager.CHAR_AZURE_BLADE || selectedCharId.ToLower().Contains("azure") || selectedCharId.ToLower().Contains("player3"));
        bool isRonin = (selectedCharId == CharacterManager.CHAR_GALE_RONIN || selectedCharId.ToLower().Contains("ronin") || selectedCharId.ToLower().Contains("gale") || selectedCharId == "Maid");
        bool isVi = LocalizationManager.IsVietnamese;

        int atkBonus = upgradeMgr != null ? upgradeMgr.GetAttackBonus(selectedCharId) : 0;
        int hpBonus = upgradeMgr != null ? upgradeMgr.GetHealthBonus(selectedCharId) : 0;
        float dashCdRed = upgradeMgr != null ? upgradeMgr.GetDashCooldownReduction(selectedCharId) : 0f;
        float ultiCdRed = upgradeMgr != null ? upgradeMgr.GetUltiCooldownReduction(selectedCharId) : 0f;
        int ultiDmgBonus = upgradeMgr != null ? upgradeMgr.GetUltiDamageBonus(selectedCharId) : 0;

        // Gem Socket Bonuses (Physical, Magic, True Damage, Lifesteal, Spell Vamp, Crit, etc.)
        EquipmentSocketManager.GetActiveEquippedSocketBonus(
            out int sAtk, out int sPhysAtk, out int sMagicAtk, out int sTrueAtk,
            out int sHp, out int sDef,
            out float sLifesteal, out float sCd, out float sShard, out float sRegen,
            out float sMaxEnergy, out float sEnergyRegen,
            out float sPhysLife, out float sSpellVamp, out float sCrit);

        // Augment Bonuses (từ hệ thống Lõi Nâng Cấp khi lên cấp trong trận)
        int augHp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusHp() : 0;
        int augAtk = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusAtk() : 0;
        int augDef = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugDef() : 0;
        float augCrit = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCrit() : 0f;
        float augCritDmg = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCritDmg() : 0f;
        float augLife = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugLifesteal() : 0f;
        float augSpellVamp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugSpellVamp() : 0f;
        float augCDR = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCDR() : 0f;

        int baseAtk = isMage ? 30 : (isDune ? 18 : (isTitan ? 32 : (isAzure ? 24 : (isRonin ? 28 : 20))));
        int baseHp = isMage ? 90 : (isDune ? 110 : (isTitan ? 200 : (isAzure ? 110 : (isRonin ? 95 : 100))));

        int azureSpeedAP = 0;
        var existingAzure = FindAnyObjectByType<AzureBladeSkill>();
        if (existingAzure != null)
        {
            existingAzure.RefreshCooldownStats();
            azureSpeedAP = existingAzure.ConvertedMagicAttack;
        }

        int totalAtk = baseAtk + atkBonus + sAtk + augAtk + (isAzure ? azureSpeedAP : 0);
        int totalPhysAtk = (!isMage && !isAzure) ? (baseAtk + atkBonus + sPhysAtk + augAtk) : sPhysAtk;
        int totalMagicAtk = (isMage || isAzure) ? (baseAtk + atkBonus + sMagicAtk + augAtk + (isAzure ? azureSpeedAP : 0)) : sMagicAtk;
        int totalTrueAtk = sTrueAtk;

        int totalHp = baseHp + hpBonus + sHp + augHp;
        int totalDef = sDef + augDef;
        float finalDashCd = Mathf.Max(1.0f, 5.0f - dashCdRed);
        float baseUltiCd = isMage ? 16.0f : (isDune ? 13.0f : (isTitan ? 12.0f : (isAzure ? 18.0f : (isRonin ? 15.0f : 24.0f))));
        float gemCdSec = baseUltiCd * (sCd + augCDR);
        float finalUltiCd = Mathf.Max(3.0f, baseUltiCd - ultiCdRed - gemCdSec);

        // Đảm bảo đồng bộ 100% với giá trị Runtime Skill Component thực tế
        var pcObj = FindAnyObjectByType<PlayerCombat>();
        if (pcObj != null)
        {
            pcObj.RecalculateCombatStats();
            var duneSkill = pcObj.GetComponent<DuneAssassinSkill>();
            var roninSkill = pcObj.GetComponent<GaleRoninSkill>();
            var azureSkill = pcObj.GetComponent<AzureBladeSkill>();
            var titanSkill = pcObj.GetComponent<TitanBerserkerSkill>();
            if (isDune && duneSkill != null)
            {
                duneSkill.RefreshCooldownStats();
                finalUltiCd = duneSkill.SpecialCooldown;
            }
            else if (isTitan && titanSkill != null)
            {
                finalUltiCd = titanSkill.specialCooldown;
            }
            else if (isRonin && roninSkill != null)
            {
                roninSkill.RefreshCooldownStats();
                finalUltiCd = roninSkill.SpecialCooldown;
            }
            else if (isAzure && azureSkill != null)
            {
                azureSkill.RefreshCooldownStats();
                finalUltiCd = azureSkill.SpecialCooldown;
            }
            else
            {
                finalUltiCd = pcObj.SkillCooldown;
            }
        }

        // Màu viền và icon theo từng tướng
        Color heroAccent = isMage ? new Color(0.88f, 0.34f, 0.99f) : (isDune ? new Color(0.98f, 0.72f, 0.22f) : (isTitan ? new Color(1f, 0.65f, 0.2f) : (isAzure ? new Color(0.35f, 0.88f, 1f) : (isRonin ? new Color(0.28f, 1f, 0.75f) : new Color(1f, 0.85f, 0.35f)))));

        Sprite sprAttack = null;
        Sprite sprDash = null;
        Sprite sprUlti = null;

        if (isMage)
        {
            sprAttack = MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Attack);
            sprDash = MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Dash);
            sprUlti = MobileActionButton.GetHeroActionIcon("mage", MobileActionButton.ButtonType.Skill);
        }
        else if (isDune)
        {
            sprAttack = MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Attack);
            sprDash = MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Dash);
            sprUlti = MobileActionButton.GetHeroActionIcon("dune", MobileActionButton.ButtonType.Skill);
        }
        else if (isTitan)
        {
            sprAttack = LoadSprite("Assets/ART/Player4/UI/icon_attack.png", "Player4/UI/icon_attack");
            sprDash = LoadSprite("Assets/ART/Player4/UI/icon_dash.png", "Player4/UI/icon_dash");
            sprUlti = LoadSprite("Assets/ART/Player4/UI/icon_ulti.png", "Player4/UI/icon_ulti");
        }
        else if (isRonin)
        {
            sprAttack = LoadSprite("Assets/ART/Player2/UI/icon_attack.png", "Player2/UI/icon_attack");
            sprDash = LoadSprite("Assets/ART/Player2/UI/icon_dash.png", "Player2/UI/icon_dash");
            sprUlti = LoadSprite("Assets/ART/Player2/UI/icon_ulti.png", "Player2/UI/icon_ulti");
        }
        else if (isAzure)
        {
            sprAttack = LoadSprite("Assets/ART/Player3/UI/icon_attack.png", "Player3/UI/icon_attack");
            sprDash = LoadSprite("Assets/ART/Player3/UI/icon_dash.png", "Player3/UI/icon_dash");
            sprUlti = LoadSprite("Assets/ART/Player3/UI/icon_ulti.png", "Player3/UI/icon_ulti");
        }
        else
        {
            sprAttack = LoadSprite("Assets/ART/Player/UI/icon_attack.png", "Player/UI/icon_attack") 
                     ?? LoadSprite("Assets/ART/Player1/UI/icon_attack.png", "Player1/UI/icon_attack")
                     ?? LoadSprite("Assets/Sprites/UI/Attack.png", "UI/Attack");
            sprDash = LoadSprite("Assets/ART/Player/UI/icon_dash.png", "Player/UI/icon_dash")
                   ?? LoadSprite("Assets/ART/Player1/UI/icon_dash.png", "Player1/UI/icon_dash")
                   ?? LoadSprite("Assets/Sprites/UI/Dash.png", "UI/Dash");
            sprUlti = LoadSprite("Assets/ART/Player/UI/icon_ulti.png", "Player/UI/icon_ulti")
                   ?? LoadSprite("Assets/ART/Player1/UI/icon_ulti.png", "Player1/UI/icon_ulti")
                   ?? LoadSprite("Assets/Sprites/UI/Ulti.png", "UI/Ulti");
        }

        // Cập nhật Sprite và Viền
        if (attackCardIcon != null && sprAttack != null) { attackCardIcon.sprite = sprAttack; attackCardIcon.color = Color.white; }
        if (dashCardIcon != null && sprDash != null) { dashCardIcon.sprite = sprDash; dashCardIcon.color = Color.white; }
        if (ultiCardIcon != null && sprUlti != null) { ultiCardIcon.sprite = sprUlti; ultiCardIcon.color = Color.white; }

        if (attackCardOutline != null) attackCardOutline.effectColor = new Color(heroAccent.r, heroAccent.g, heroAccent.b, 0.65f);
        if (attackBadgeOutline != null) attackBadgeOutline.effectColor = heroAccent;
        if (dashCardOutline != null) dashCardOutline.effectColor = new Color(heroAccent.r, heroAccent.g, heroAccent.b, 0.65f);
        if (dashBadgeOutline != null) dashBadgeOutline.effectColor = heroAccent;
        if (ultiCardOutline != null) ultiCardOutline.effectColor = new Color(heroAccent.r, heroAccent.g, heroAccent.b, 0.65f);
        if (ultiBadgeOutline != null) ultiBadgeOutline.effectColor = heroAccent;

        // Header Title & Level Sync
        int curLv = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1;
        int curExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentExp : 0;
        int reqExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.ExpRequired : 4;

        if (characterTitleText != null)
        {
            if (isMage)
                characterTitleText.text = isVi ? "[PHÁP SƯ] PHÁP SƯ TINH TÚ (CELESTIAL MAGE)" : "[MAGE] CELESTIAL MAGE (ARCANE SORCERER)";
            else if (isDune)
                characterTitleText.text = isVi ? "SA MẠC THÍCH KHÁCH (DUNE ASSASSIN)" : "DUNE ASSASSIN (SAND SHADOW)";
            else if (isTitan)
                characterTitleText.text = isVi ? "CUỒNG CHIẾN BINH (TITAN BERSERKER)" : "TITAN BERSERKER (HEAVY TANKER)";
            else if (isAzure)
                characterTitleText.text = isVi ? "[XẠ THỦ] LAM KIẾM SĨ (AZURE BLADE)" : "[MARKSMAN] AZURE BLADE (WAVE RANGER)";
            else if (isRonin)
                characterTitleText.text = isVi ? "[SÁT THỦ] PHONG LÔI LÃNG KHÁCH (GALE RONIN)" : "[ASSASSIN] GALE RONIN (PHANTOM BLADE)";
            else
                characterTitleText.text = isVi ? "[ĐẤU SĨ] HIỆP SĨ VAELSTRIDER" : "[WARRIOR] KNIGHT VAELSTRIDER";

            characterTitleText.text += $"  <color=#FFD700>[LV {curLv}]</color>";
            characterTitleText.color = heroAccent;
        }
        if (characterSubtitleText != null)
        {
            string lvPrefix = isVi ? $"<color=#38BDF8>CẤP {curLv} (EXP: {curExp}/{reqExp})</color> | " : $"<color=#38BDF8>LV {curLv} (EXP: {curExp}/{reqExp})</color> | ";
            if (isMage)
                characterSubtitleText.text = lvPrefix + (isVi ? "Vai trò: Pháp Sư Tinh Vân | Cầu Ma Pháp, Tốc Biến Hư Không & Thiên Thạch Ma Trận" : "Role: Arcane Mage | Homing Orbs, Arcane Blink & Meteor Apocalypse");
            else if (isDune)
                characterSubtitleText.text = lvPrefix + (isVi ? "Vai trò: Sát Thủ Cơ Động | Song Đoản Đao, 3 lần Lướt liên hoàn & Cuồng bão Sa Ảnh" : "Role: Mobile Assassin | Twin Scimitars, Triple Dash & Sandstorm X-Rend");
            else if (isTitan)
                characterSubtitleText.text = lvPrefix + (isVi ? "Vai trò: Đỡ Đòn / Tanker | Đại Chùy giáng thế, Giảm 20% sát thương & Cuồng nộ chấn địa" : "Role: Tanker / Juggernaut | Conic Hammer Smashes, Innate 20% DR & Cataclysm Slam");
            else if (isAzure)
                characterSubtitleText.text = lvPrefix + (isVi ? "Vai trò: Xạ Thủ Sóng Kiếm | Băng Nguyệt Kiếm Khí tầm xa, Hồi Mana & Bão sóng nan quạt" : "Role: Marksman / Ranged | Long-range Frost Waves, Mana Regen & 5-Way Tempest");
            else if (isRonin)
                characterSubtitleText.text = lvPrefix + (isVi ? "Vai trò: Sát Thủ Trảm Kích | Bán Nguyệt Phong Ấn, Tàn Ảnh Tốc Biến & Ảo Ảnh Trảm Sát" : "Role: Burst Assassin | Crescent Wind Marks, Phantom Warp & Flash Cleaves");
            else
                characterSubtitleText.text = lvPrefix + (isVi ? "Vai trò: Đấu Sĩ Cổ Tộc | Thánh Kiếm Tiên Phong, Khiên Phản Đòn & Lốc Kiếm Thiên Nham" : "Role: Holy Warrior | Ancient Vanguard, Parry Counter & Holy Sword Tempest");
        }

        // 6 Quick Stats Badges
        if (hpStatText != null)
        {
            string label = isVi ? "MÁU / GIÁP" : "HP / DEF";
            string defStr = totalDef > 0 ? $" <size=10><color=#38BDF8>+{totalDef}</color></size>" : "";
            hpStatText.text = $"{label}\n<color=#55FF88><size=17><b>{totalHp}</b></size></color>{defStr}";
        }
        if (atkStatText != null)
        {
            string label = isVi ? "TẤN CÔNG" : "ATTACK";
            string subStats = $"<size=9><color=#FF3B30>{totalPhysAtk}</color>|<color=#B84DFF>{totalMagicAtk}</color>" + (totalTrueAtk > 0 ? $"|<color=#E2E8F0>{totalTrueAtk}</color>" : "") + "</size>";

            int physPen = pcObj != null ? pcObj.TotalPhysicalArmorPenFlat : 0;
            int physPenPct = pcObj != null ? Mathf.RoundToInt(pcObj.TotalPhysicalArmorPenPercent * 100) : 0;
            int magPen = pcObj != null ? pcObj.TotalMagicPenFlat : 0;
            int magPenPct = pcObj != null ? Mathf.RoundToInt(pcObj.TotalMagicPenPercent * 100) : 0;

            string penSub = "";
            if (physPen > 0 || physPenPct > 0 || magPen > 0 || magPenPct > 0)
            {
                string pPenStr = physPenPct > 0 ? $"{physPen}+{physPenPct}%" : $"{physPen}";
                string mPenStr = magPenPct > 0 ? $"{magPen}+{magPenPct}%" : $"{magPen}";
                penSub = $"\n<size=8><color=#F97316>XG:{pPenStr}</color> <color=#A855F7>XP:{mPenStr}</color></size>";
            }

            atkStatText.text = $"{label}\n<color=#FF5566><size=16><b>{totalAtk}</b></size></color>\n{subStats}{penSub}";
        }
        if (critStatText != null)
        {
            string label = isVi ? "CHÍ MẠNG" : "CRITICAL";
            int critPct = 20 + Mathf.RoundToInt((sCrit + augCrit) * 100);
            float finalCritMultiplier = 1.75f + augCritDmg;
            string critDmgSub = isVi ? $"x{finalCritMultiplier:0.##} ST Bạo" : $"x{finalCritMultiplier:0.##} Crit Dmg";
            critStatText.text = $"{label}\n<color=#F59E0B><size=16><b>{critPct}%</b></size></color>\n<size=9><color=#FBBF24>{critDmgSub}</color></size>";
        }
        if (vampStatText != null)
        {
            string label = isVi ? "HÚT MÁU" : "LIFESTEAL";
            int pLife = Mathf.RoundToInt(((sPhysLife > 0f ? sPhysLife : sLifesteal) + augLife) * 100);
            int sVamp = Mathf.RoundToInt((sSpellVamp + augSpellVamp) * 100);
            vampStatText.text = $"{label}\n<size=11><color=#F43F5E>VL:{pLife}%</color></size>\n<size=11><color=#E879F9>Phép:{sVamp}%</color></size>";
        }
        if (dashCdStatText != null)
        {
            string label = isVi ? "HỒI LƯỚT" : "DASH CD";
            dashCdStatText.text = $"{label}\n<color=#44DDFF><size=16><b>{finalDashCd:F1}s</b></size></color>";
        }
        if (ultiCdStatText != null)
        {
            string label = isVi ? "HỒI TUYỆT KỸ" : "ULTI CD";
            ultiCdStatText.text = $"{label}\n<color=#FFAA33><size=16><b>{finalUltiCd:F1}s</b></size></color>";
        }

        // Card 0: Passive (Nội Tại)
        if (passiveCardTitle != null)
        {
            string pTitle = isMage ? (isVi ? "NỘI TẠI: TINH TÚ CỘNG HƯỞNG" : "PASSIVE: CELESTIAL RESONANCE")
                          : isDune ? (isVi ? "NỘI TẠI: HUYẾT SA TẨY LỄ" : "PASSIVE: BLOOD SAND RITUAL")
                          : isTitan ? (isVi ? "NỘI TẠI: DA DUNG NHAM CHIẾN THẦN" : "PASSIVE: VOLCANIC CARAPACE")
                          : isAzure ? (isVi ? "NỘI TẠI: HÀN TÂM & KIẾM KHÍ QUY HÓA" : "PASSIVE: FROST SOUL & FLOWING RESONANCE")
                          : isRonin ? (isVi ? "NỘI TẠI: PHONG LÔI PHONG ẤN" : "PASSIVE: WIND SEAL RESONANCE")
                          : (isVi ? "NỘI TẠI: THÁNH QUANG BẢO HỘ" : "PASSIVE: RADIANT AEGIS");
            passiveCardTitle.text = $"{pTitle}  <color=#D9A6FF><size=85%>BỊ ĐỘNG</size></color>";
        }
        if (passiveCardDesc != null)
        {
            if (isMage)
            {
                passiveCardDesc.text = isVi
                    ? "• <b>Cộng Hưởng Tinh Tú:</b> Cầu ma pháp bắn trúng bộc phát sét lan & hồi phục <color=#38BDF8><b>+4 Mana</b></color> mỗi đòn trúng.\n• <b>Hồi chiêu:</b> <color=#E2E8F0><b>VÔ HẠN</b></color> (Tác dụng vĩnh cửu)"
                    : "• <b>Celestial Resonance:</b> Hits bounce chain lightning & restore <color=#38BDF8><b>+4 Mana</b></color> per hit.\n• <b>Cooldown:</b> <color=#E2E8F0><b>PERMANENT</b></color>";
            }
            else if (isDune)
            {
                passiveCardDesc.text = isVi
                    ? "• <b>Huyết Sa Tẩy Lễ:</b> Mỗi 2 chiêu thức tung ra hồi ngay <color=#22C55E><b>10% HP đã mất</b></color> & nhận <color=#FFAA33><b>+15% Tốc Đánh</b></color> trong 3s.\n• <b>Hồi chiêu:</b> <color=#E2E8F0><b>VÔ HẠN</b></color> (Tác dụng vĩnh cửu)"
                    : "• <b>Blood Sand Ritual:</b> Every 2 skill casts restores <color=#22C55E><b>10% Missing HP</b></color> & +15% ATK Speed for 3s.\n• <b>Cooldown:</b> <color=#E2E8F0><b>PERMANENT</b></color>";
            }
            else if (isTitan)
            {
                passiveCardDesc.text = isVi
                    ? "• <b>Da Dung Nham:</b> Giảm cố định <color=#55FF88><b>20% sát thương nhận vào</b></color> & nhận thêm <color=#FFAA33><b>+30% Kháng Khống Chế</b></color>.\n• <b>Hồi chiêu:</b> <color=#E2E8F0><b>VÔ HẠN</b></color> (Tác dụng vĩnh cửu)"
                    : "• <b>Volcanic Carapace:</b> Permanently gains <color=#55FF88><b>20% Damage Reduction</b></color> & <color=#FFAA33><b>+30% CC Resistance</b></color>.\n• <b>Cooldown:</b> <color=#E2E8F0><b>PERMANENT</b></color>";
            }
            else if (isAzure)
            {
                passiveCardDesc.text = isVi
                    ? $"• <b>Hàn Tâm Băng Phong:</b> Đòn đánh trúng đích hồi <color=#38BDF8><b>+6 Mana</b></color> & giảm 50% hồi đòn kế tiếp.\n• <b>Kiếm Khí Quy Hóa:</b> Tự động chuyển hóa toàn bộ <b>Tốc Đánh & Tốc Độ</b> cộng thêm thành <color=#D946EF><b>+{azureSpeedAP} Sát Thương Phép (AP)</b></color> cho Sóng Kiếm!\n• <b>Hồi chiêu:</b> <color=#E2E8F0><b>VÔ HẠN</b></color> (Tác dụng vĩnh cửu)"
                    : $"• <b>Frost Blade Soul:</b> Hits restore <color=#38BDF8><b>+6 Mana</b></color> & cut next attack cooldown by 50%.\n• <b>Flowing Resonance:</b> Converts all <b>bonus Attack Speed & Speed</b> directly into <color=#D946EF><b>+{azureSpeedAP} Magic Damage (AP)</b></color> for Sword Waves!\n• <b>Cooldown:</b> <color=#E2E8F0><b>PERMANENT</b></color>";
            }
            else if (isRonin)
            {
                passiveCardDesc.text = isVi
                    ? "• <b>Phong Lôi Phong Ấn:</b> Đòn đánh quét kiếm tích Phong Ấn (3 tầng nổ lốc <color=#FFAA33><b>240% Crit DMG</b></color>). Diệt địch hồi +10 Mana.\n• <b>Hồi chiêu:</b> <color=#E2E8F0><b>VÔ HẠN</b></color> (Tác dụng vĩnh cửu)"
                    : "• <b>Wind Seal:</b> Cleaves stack marks (3 marks burst for <color=#FFAA33><b>240% Crit DMG</b></color>). Slaying foes grants +10 Mana.\n• <b>Cooldown:</b> <color=#E2E8F0><b>PERMANENT</b></color>";
            }
            else
            {
                passiveCardDesc.text = isVi
                    ? "• <b>Thánh Quang Bảo Hộ:</b> Đòn đánh hồi <color=#38BDF8><b>+8 Mana</b></color>. Khi HP dưới 35%, kích hoạt Thánh Giáp Thiên Thần hồi ngay <color=#55FF88><b>25% HP</b></color>.\n• <b>Hồi chiêu:</b> <color=#E2E8F0><b>VÔ HẠN</b></color> (Tác dụng vĩnh cửu)"
                    : "• <b>Radiant Aegis:</b> Hits restore <color=#38BDF8><b>+8 Mana</b></color>. When HP drops below 35%, activates Divine Aegis healing <color=#55FF88><b>25% HP</b></color>.\n• <b>Cooldown:</b> <color=#E2E8F0><b>PERMANENT</b></color>";
            }
        }

        // Card 1: Attack
        if (attackCardTitle != null)
        {
            string baseTitle = isMage ? (isVi ? "1. CẦU MA PHÁP (GỒNG KHIÊN)" : "1. ARCANE ORBS & BARRIER")
                             : isDune ? (isVi ? "1. SONG ĐAO (GỒNG THỦ)" : "1. DUAL DAGGERS (PARRY)")
                             : isTitan ? (isVi ? "1. ĐẠI CHÙY (GỒNG ĐỊA CHẤN)" : "1. WAR HAMMER (EARTHSHAKER)")
                             : isAzure ? (isVi ? "1. TRỌNG KIẾM & SÓNG BĂNG KIẾM KHÍ" : "1. FROST SWORD WAVES")
                             : isRonin ? (isVi ? "1. PHONG TRẢM (GỒNG TỤ LỰC)" : "1. GALE SLASH & IAIDO")
                             : (isVi ? "1. ĐẠI KIẾM (GỒNG THẾ THỦ)" : "1. ANCIENT SWORD (PARRY)");
            attackCardTitle.text = $"{baseTitle}  <color=#10B981><size=85%>[0 Mana]</size></color>";
        }
        if (attackCardDesc != null)
        {
            if (isMage)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Cầu Ma Pháp:</b> Bắn cầu ma pháp tự dẫn đường | Gồng <b>Khiên Hộ Thể</b> giảm 60% sát thương, phản đòn AP & hồi <color=#38BDF8><b>+10 Mana</b></color>!\n" +
                      $"• <b>[Thần Nộ Tinh Tú]:</b> Đầy 100% Nội Năng tím kích hoạt <color=#B84DFF><b>Overdrive (8s)</b></color>: Hồi Tuyệt Kỹ còn <color=#55FF88><b>1.0s</b></color>!"
                    : $"• <b>DMG Type:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Arcane Orbs:</b> Homing orb | Hold for <b>Astral Barrier</b> absorbing 60% dmg, counter AP & +10 Mana!\n" +
                      $"• <b>[Celestial Overdrive]:</b> 100% Inner Energy triggers <color=#B84DFF><b>Overdrive (8s)</b></color>: Ulti CD drops to <color=#55FF88><b>1.0s</b></color>!";
            }
            else if (isDune)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> & <color=#E2E8F0><b>TD</b></color>\n" +
                      $"• <b>Đặc tính:</b> Đòn 3 phóng Lốc Sa Mạc (3.5s CD). [Gồng Thủ / Đầy Nộ]: Giảm 70% ST, phóng Lốc Đỏ gây <color=#E2E8F0><b>TD</b></color>, <color=#FFAA33><b>Choáng 0.75s</b></color> & <color=#FFAA33><b>Ngắt chiêu Boss!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> & <color=#E2E8F0><b>TD</b></color>\n" +
                      $"• <b>Trait:</b> Hit 3 fires Sand Vortex (3.5s CD). [Hold Parry / Full Rage]: 70% DR, fires Crimson Vortex dealing <color=#E2E8F0><b>TD</b></color>, <color=#FFAA33><b>0.75s Stun</b></color> & <color=#FFAA33><b>Interrupts Boss!</b></color>";
            }
            else if (isTitan)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color>\n" +
                      $"• <b>Đặc tính:</b> Nện đất ({totalAtk} ATK). Nhấn Giữ gồng Thế Thủ: <color=#55FF88><b>Bất tử 100%</b></color>, thả tay nện đất bộc phá gây <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Choáng 1.0s!</b></color> (CD gồng: 5s-10s)"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color>\n" +
                      $"• <b>Trait:</b> Smashes inflicting {totalAtk} ATK. Hold attack for Earthshaker: <color=#55FF88><b>100% Invulnerable</b></color>, release to smash ground inflicting <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Stun 1.0s!</b></color> (Charge CD: 5s-10s)";
            }
            else if (isAzure)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Sóng Kiếm Băng:</b> Phóng kiếm khí xuyên quái (6.5m), hồi <color=#38BDF8><b>+6 Mana</b></color>. Găm <color=#FF2961><b>Vết Thương Sâu (Trái Tim Nứt)</b></color> 5s: <color=#FF4D79><b>giảm 50% hồi máu & hút máu</b></color> của địch & nhận <color=#55FFFF><b>+20% ST băng</b></color>! Nhấn Giữ Tụ Lực (1s) tăng ST lên <color=#FFAA33><b>x2.25 lần!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Frost Wave:</b> Piercing wave (6.5m), restores <color=#38BDF8><b>+6 Mana</b></color>. Inflicts <color=#FF2961><b>Grievous Wounds (Cracked Heart)</b></color> for 5s: <color=#FF4D79><b>reduces enemy healing & lifesteal by 50%</b></color> & takes <color=#55FFFF><b>+20% Frost DMG</b></color>! Hold Charge (1s) boosts wave damage to <color=#FFAA33><b>x2.25!</b></color>";
            }
            else if (isRonin)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> & <color=#B84DFF><b>AP</b></color> (Phong Ấn)\n" +
                      $"• <b>Đặc tính:</b> Quét kiếm 180° ({totalAtk} ATK, hồi +10 Mana), tích 3 tầng nổ lốc xoáy <color=#FFAA33><b>AP 240% Crit</b></color>. <color=#FFDF66><b>[Khi Đầy Nội Năng / Khiên Hộ Thân]:</b></color> Gồng phóng Nguyệt Kiếm Khí <color=#FF3B30><b>x2.85 Crit DMG</b></color> & Giảm 35% CD Tuyệt Kỹ!"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> & <color=#B84DFF><b>AP</b></color> (Wind Marks)\n" +
                      $"• <b>Trait:</b> 180° Crescent cleave ({totalAtk} ATK, +10 Mana), 3 marks detonate for <color=#FFAA33><b>AP 240% Crit</b></color> vortex. <color=#FFDF66><b>[When Full Energy / Shield Active]:</b></color> Charge for Crescent Wave <color=#FF3B30><b>x2.85 Crit DMG</b></color> & -35% Ulti CD!";
            }
            else
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> ({totalAtk} ATK)\n" +
                      $"• <b>Đặc tính:</b> Chém 3 nhịp gây ATK (hồi <color=#38BDF8><b>+8 Mana</b></color>). Nhấn Giữ Thế Thủ giảm 20% sát thương, thả tay bộc phá Thần Kiếm Phản Kích gây <color=#FF3B30><b>x3.2 Crit ATK!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> ({totalAtk} ATK)\n" +
                      $"• <b>Trait:</b> 3-Hit combo (restores <color=#38BDF8><b>+8 Mana</b></color>). Hold attack for Parry: 20% DR, release to unleash Divine Riposte dealing <color=#FF3B30><b>x3.2 Crit ATK!</b></color>";
            }
        }

        // Card 2: Dash
        if (dashCardTitle != null)
        {
            if (isMage)
                dashCardTitle.text = (isVi ? "2. TỐC BIẾN HƯ KHÔNG (BLINK)" : "2. ARCANE BLINK DASH") + "  <color=#10B981><size=85%>[0 Mana]</size></color>";
            else if (isDune)
                dashCardTitle.text = (isVi ? "2. ẢO ẢNH SA LƯỚT (3 LẦN LIÊN HOÀN)" : "2. TRIPLE SAND MIRAGE DASH") + "  <color=#FF4757><size=85%>[4% HP]</size></color>";
            else if (isTitan)
                dashCardTitle.text = (isVi ? "2. CHIẾN XA XOAY BÚA 360°" : "2. JUGGERNAUT 360° SPIN") + "  <color=#10B981><size=85%>[0 Mana]</size></color>";
            else if (isAzure)
                dashCardTitle.text = (isVi ? "2. HÀN BĂNG TRƯỢT TUYẾT" : "2. FROST GLIDE DASH") + "  <color=#10B981><size=85%>[0 Mana]</size></color>";
            else if (isRonin)
                dashCardTitle.text = (isVi ? "2. TÀN ẢNH TỐC BIẾN (MURAD STYLE)" : "2. SHADOW PHANTOM WARP") + "  <color=#10B981><size=85%>[0 Mana]</size></color>";
            else
                dashCardTitle.text = (isVi ? "2. THÁNH THUẪN PHẢN ĐÒN" : "2. RADIANT RIPOSTE DASH") + "  <color=#10B981><size=85%>[0 Mana]</size></color>";
        }
        if (dashCardDesc != null)
        {
            if (isMage)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#10B981><b>0 Mana</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Dịch chuyển tức thời 3.2m né đòn, để lại Vết Rách Hư Không xoay tròn gây <color=#B84DFF><b>AP liên tục</b></color>, <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> & hồi <color=#38BDF8><b>+12 Mana</b></color>."
                    : $"• <b>Cost:</b> <color=#10B981><b>0 Mana</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Instant 3.2m void warp to dodge, leaves Singularity Rift dealing continuous AP, <color=#FFAA33><b>Cuts Ulti CD 1.5s</b></color> & restores <color=#38BDF8><b>+12 Mana</b></color>.";
            }
            else if (isDune)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#FF5566><b>4% HP</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Tích lũy tối đa 3 lần lướt liên hoàn cực cơ động (tiêu hao 4% HP), tạo tàn ảnh cát & <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> mỗi lần lướt."
                    : $"• <b>Cost:</b> <color=#FF5566><b>4% HP</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Stores up to 3 Consecutive Dash Charges (consumes 4% HP), leaves mirages & <color=#FFAA33><b>Reduces Ulti CD 1.5s</b></color> per dash.";
            }
            else if (isTitan)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#10B981><b>0 Mana</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt tốc độ cao xoay búa 360° càn quét gây ATK & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>."
                    : $"• <b>Cost:</b> <color=#10B981><b>0 Mana</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Heavy spin dash slashing all enemies along path with ATK & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>.";
            }
            else if (isRonin)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#10B981><b>0 Mana</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Nhịp 1:</b> Tốc biến 4.8m tới trước (<color=#55FF88><b>Bất tử 0.25s</b></color>) cắm bóng 4.5s.\n" +
                      $"• <b>Nhịp 2 (Bấm lại):</b> Lập tức Teleport quay về bóng (<color=#55FF88><b>Bất tử 0.30s</b></color>) & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ!</b></color>"
                    : $"• <b>Cost:</b> <color=#10B981><b>0 Mana</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Stage 1:</b> Warps 4.8m forward leaving clone for 4.5s (<color=#55FF88><b>Invincible 0.25s</b></color>).\n" +
                      $"• <b>Stage 2 (Cast again):</b> Instantly Warps back to clone (<color=#55FF88><b>Invincible 0.30s</b></color>) & <color=#FFAA33><b>Cuts Ulti CD by 2.0s!</b></color>";
            }
            else if (isAzure)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#10B981><b>0 Mana</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt né đòn trên dải băng tuyết & <color=#55FFFF><b>Hồi ngay lập tức đòn đánh thường</b></color> kế tiếp."
                    : $"• <b>Cost:</b> <color=#10B981><b>0 Mana</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Swift glide dash dodging attacks & <color=#55FFFF><b>Instantly resets basic attack cooldown</b></color>.";
            }
            else
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#10B981><b>0 Mana</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Bất tử khi lướt</b></color>)\n" +
                      $"• <b>Thánh Thuẫn Phản Đòn:</b> Lướt qua quái/đỡ đòn sẽ <color=#FFAA33><b>Hồi 50% CD Lướt</b></color>, hồi <color=#38BDF8><b>+15 Mana</b></color> & phóng 3 Thánh Kiếm Khí gây ATK!"
                    : $"• <b>Cost:</b> <color=#10B981><b>0 Mana</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Invulnerable</b></color>)\n" +
                      $"• <b>Radiant Riposte:</b> Deflecting attacks <color=#FFAA33><b>Refunds 50% Dash CD</b></color>, restores <color=#38BDF8><b>+15 Mana</b></color> & fires 3 Holy Waves with ATK!";
            }
        }

        // Card 3: Ulti
        if (ultiCardTitle != null)
        {
            if (isMage)
                ultiCardTitle.text = (isVi ? "3. TUYỆT KỸ: THIÊN THẠCH MA TRẬN" : "3. ULTIMATE: METEOR APOCALYPSE") + "  <color=#00E5FF><size=85%>[60 Mana]</size></color>";
            else if (isDune)
                ultiCardTitle.text = (isVi ? "3. TUYỆT KỸ: CUỒNG BÃO SA ẢNH X-REND" : "3. ULTIMATE: SANDSTORM X-REND") + "  <color=#FF4757><size=85%>[20% HP]</size></color>";
            else if (isTitan)
                ultiCardTitle.text = (isVi ? "3. TUYỆT KỸ: CUỒNG NỘ CHẤN ĐỊA DUNG NHAM" : "3. ULTIMATE: VOLCANIC CATACLYSM SLAM") + "  <color=#00E5FF><size=85%>[45 Mana]</size></color>";
            else if (isAzure)
                ultiCardTitle.text = (isVi ? "3. TUYỆT KỸ: BÃO SÓNG KIẾM NAN QUẠT" : "3. ULTIMATE: CRESCENT TEMPEST") + "  <color=#00E5FF><size=85%>[50 Mana]</size></color>";
            else if (isRonin)
                ultiCardTitle.text = (isVi ? "3. TUYỆT KỸ: ÁM ẢNH PHÂN THÂN TRẢM" : "3. ULTIMATE: PHANTOM FLASH CLEAVE") + "  <color=#00E5FF><size=85%>[50 Mana]</size></color>";
            else
                ultiCardTitle.text = (isVi ? "3. TUYỆT KỸ: THÁNH KIẾM THIÊN NHAM" : "3. ULTIMATE: HOLY SWORD TEMPEST") + "  <color=#00E5FF><size=85%>[50 Mana]</size></color>";
        }
        if (ultiCardDesc != null)
        {
            if (isMage)
            {
                int mageMeteorDmg = (22 * 5) + (totalAtk * 3) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#B84DFF><b>AP</b></color> (+<color=#E2E8F0><b>TD</b></color> từ Ngọc)\n" +
                      $"• <b>Sát thương:</b> <color=#B84DFF><b>{mageMeteorDmg} AP</b></color> (5 Đợt Mưa Thiên Thạch)\n" +
                      $"• <b>Tiêu hao:</b> <color=#00E5FF><b>60 Mana</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Triệu hồi 5 đợt thiên thạch ma pháp giáng thế từ không gian hủy diệt diện rộng.\n" +
                      $"• <b>[Overdrive 8s]:</b> Đầy Nội Năng tím hồi chiêu giảm về <color=#55FF88><b>1.0s</b></color> & tiêu hao còn 25 Mana!"
                    : $"• <b>DMG Type:</b> <color=#B84DFF><b>AP</b></color> (+<color=#E2E8F0><b>TD</b></color> from Gems)\n" +
                      $"• <b>Damage:</b> <color=#B84DFF><b>{mageMeteorDmg} AP</b></color> (5 Waves Meteor Strikes)\n" +
                      $"• <b>Cost:</b> <color=#00E5FF><b>60 Mana</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Summons 5 waves of devastating meteor strikes annihilating all battlefield foes.\n" +
                      $"• <b>[Overdrive 8s]:</b> 100% Inner Energy cuts CD to <color=#55FF88><b>1.0s</b></color> & cost to 25 Mana!";
            }
            else if (isDune)
            {
                int duneUltiDmg = (14 * 6) + (totalAtk * 3) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> & <color=#E2E8F0><b>TD</b></color> (X-Rend)\n" +
                      $"• <b>Sát thương:</b> <color=#FF3B30><b>{duneUltiDmg} ATK/TD</b></color> | <b>Tiêu hao:</b> <color=#FF5566><b>20% HP</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Hút quái -> chém chữ X dứt điểm gây TD cực lớn, <color=#55FF88><b>Hút máu 50% sát thương gây ra</b></color> & Chảy Máu Sa Mạc liên tục."
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> & <color=#E2E8F0><b>TD</b></color> (X-Rend)\n" +
                      $"• <b>Damage:</b> <color=#FF3B30><b>{duneUltiDmg} ATK/TD</b></color> | <b>Cost:</b> <color=#FF5566><b>20% HP</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> 6 flurry hits + X-Slash dealing TD, <color=#55FF88><b>50% Lifesteal from damage dealt</b></color> & continuous Sand Bleed.";
            }
            else if (isTitan)
            {
                int titanUltiDmg = 95 + (totalAtk * 2) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> (+<color=#E2E8F0><b>TD</b></color> từ Ngọc)\n" +
                      $"• <b>Sát thương:</b> <color=#FF3B30><b>{titanUltiDmg} ATK</b></color> (Địa Chấn Dung Nham)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>45 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Hút toàn bộ quái vật (5.5m), đập nện đại chùy hất tung diện rộng & <color=#55FF88><b>Nhận Máu Ảo bằng 50% HP hiện tại trong 15s!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> (+<color=#E2E8F0><b>TD</b></color> from Gems)\n" +
                      $"• <b>Damage:</b> <color=#FF3B30><b>{titanUltiDmg} ATK</b></color> (Cataclysm Slam)\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>45 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Draws enemies (5.5m), cataclysmic slam launches foes & <color=#55FF88><b>Gains Barrier equal to 50% Current HP for 15s!</b></color>";
            }
            else if (isAzure)
            {
                int azureWaveDmg = 48 + totalAtk + ultiDmgBonus;
                int azureTotalDmg = azureWaveDmg * 5;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#B84DFF><b>AP</b></color>\n" +
                      $"• <b>Sát thương:</b> <color=#B84DFF><b>{azureWaveDmg} AP</b></color> x 5 Tia (Tổng: <color=#B84DFF><b>{azureTotalDmg} AP</b></color>)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Phóng 5 luồng đại kiếm khí nan quạt quét sạch chiến trường"
                    : $"• <b>DMG Type:</b> <color=#B84DFF><b>AP</b></color>\n" +
                      $"• <b>Damage:</b> <color=#B84DFF><b>{azureWaveDmg} AP</b></color> x 5 Waves (Total: <color=#B84DFF><b>{azureTotalDmg} AP</b></color>)\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Fires 5 massive fan-spread crescent waves piercing enemies";
            }
            else if (isRonin)
            {
                int roninUltiDmg = (28 * 3) + (totalAtk * 2) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#E2E8F0><b>TD</b></color>\n" +
                      $"• <b>Sát thương:</b> <color=#E2E8F0><b>{roninUltiDmg} TD</b></color> (Bạo Kích Nhất Trảm)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>40 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Khóa mục tiêu (10.5m) -> Dịch chuyển ra sau lưng trảm cực mạnh rồi biến về vị trí cũ\n" +
                      $"• <b>Nội tại:</b> <color=#55FF88><b>Kết liễu mục tiêu hồi chiêu Tuyệt Kỹ ngay lập tức!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#E2E8F0><b>TD</b></color>\n" +
                      $"• <b>Damage:</b> <color=#E2E8F0><b>{roninUltiDmg} TD</b></color> (Execution Strike)\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>40 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Target lock-on (10.5m) -> Teleports behind enemy for a devastating strike then returns\n" +
                      $"• <b>Special:</b> <color=#55FF88><b>Executing target instantly resets Ultimate Cooldown!</b></color>";
            }
            else
            {
                int vaelFinalHit = 50 + totalAtk + ultiDmgBonus;
                int vaelTotal = (20 + (int)(totalAtk * 0.4f)) * 2 + (25 + (int)(totalAtk * 0.5f)) + vaelFinalHit;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color>\n" +
                      $"• <b>Sát thương:</b> <color=#FF3B30><b>{vaelTotal} ATK</b></color> (4 Đợt Bão Kiếm, Đòn cuối: <color=#FFAA33><b>{vaelFinalHit} ATK</b></color>)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Triệu hồi bão kiếm quét 4 đợt diện rộng, hút và dồn sát thương"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color>\n" +
                      $"• <b>Damage:</b> <color=#FF3B30><b>{vaelTotal} ATK</b></color> (4-Hit Whirlwind, Final: <color=#FFAA33><b>{vaelFinalHit} ATK</b></color>)\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
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
            if (Application.isPlaying) Destroy(existingBtn.gameObject);
            else DestroyImmediate(existingBtn.gameObject);
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
        csCanvas.sortingOrder = 28000;
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
        dImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        dImg.type = Image.Type.Sliced;
        dImg.color = Color.white;
        WhiteIconLoader.ApplyGothicFrame(dialogBox, Color.white, 0.40f);

        var uiComp = panelObj.AddComponent<CharacterStatsUI>();

        // Top-Right Close Button [X] (Touch-Friendly Mobile Button)
        GameObject closeBtnObj = CreateUIObject("Btn_Close", dialogBox.transform);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-25f, -22f);
        cRt.sizeDelta = new Vector2(52f, 52f);

        Button cBtn = closeBtnObj.AddComponent<Button>();
        cBtn.onClick.AddListener(uiComp.Close);
        SoulslikeHUDStyler.StyleCloseButton(cBtn);

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
        uiComp.characterTitleText.color = new Color(0.98f, 0.88f, 0.55f);

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
        uiComp.characterSubtitleText.color = new Color(0.80f, 0.85f, 0.95f, 0.85f);
        SoulslikeHUDStyler.StyleHeader(uiComp.characterTitleText, uiComp.characterSubtitleText);
        SoulslikeHUDStyler.AddFiligreeDivider(dialogBox.transform, new Vector2(0f, -82f), new Vector2(520f, 12f));

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
        uiComp.critStatText = CreateMiniBadge(badgesRow.transform, "CRIT", new Color(0.32f, 0.22f, 0.08f, 0.95f));
        uiComp.vampStatText = CreateMiniBadge(badgesRow.transform, "VAMP", new Color(0.28f, 0.10f, 0.24f, 0.95f));
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
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        Sprite sprAttack = LoadSprite("Assets/Sprites/UI/Attack.png", "UI/Attack");
        Sprite sprDash = LoadSprite("Assets/Sprites/UI/Dash.png", "UI/Dash");
        Sprite sprUlti = LoadSprite("Assets/Sprites/UI/Ulti.png", "UI/Ulti");
        Sprite sprPassive = LoadSprite("Assets/Sprites/UI/Star.png", "UI/Star");
        if (sprPassive == null) sprPassive = sprAttack;

        CreateSkillCard(cardsSection.transform, "Card_Passive", "0. NỘI TẠI", sprPassive, new Color(0.85f, 0.65f, 1f), out uiComp.passiveCardDesc, out uiComp.passiveCardTitle, out uiComp.passiveCardIcon, out uiComp.passiveCardOutline, out uiComp.passiveBadgeOutline);
        CreateSkillCard(cardsSection.transform, "Card_Attack", "1. ĐÒN ĐÁNH CƠ BẢN", sprAttack, new Color(1f, 0.85f, 0.35f), out uiComp.attackCardDesc, out uiComp.attackCardTitle, out uiComp.attackCardIcon, out uiComp.attackCardOutline, out uiComp.attackBadgeOutline);
        CreateSkillCard(cardsSection.transform, "Card_Dash", "2. LƯỚT NÉ ĐÒN", sprDash, new Color(0.2f, 0.95f, 0.65f), out uiComp.dashCardDesc, out uiComp.dashCardTitle, out uiComp.dashCardIcon, out uiComp.dashCardOutline, out uiComp.dashBadgeOutline);
        CreateSkillCard(cardsSection.transform, "Card_Ulti", "3. TUYỆT KỸ", sprUlti, new Color(0.95f, 0.35f, 0.35f), out uiComp.ultiCardDesc, out uiComp.ultiCardTitle, out uiComp.ultiCardIcon, out uiComp.ultiCardOutline, out uiComp.ultiBadgeOutline);

        panelObj.SetActive(false);
        return uiComp;
    }

    private static Sprite LoadSprite(string assetPath, string resourcePath)
    {
        Sprite spr = null;
#if UNITY_EDITOR
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (spr == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var a in all)
            {
                if (a is Sprite s) { spr = s; break; }
            }
        }
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
        tmp.fontSize = 15.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.lineSpacing = 10;
        return tmp;
    }

    private static void CreateSkillCard(Transform parent, string name, string title, Sprite iconSpr, Color accentCol, out TextMeshProUGUI descTmp, out TextMeshProUGUI titleTmp, out Image iconImgOut, out Outline cardOutlineOut, out Outline badgeOutlineOut)
    {
        GameObject cardObj = CreateUIObject(name, parent);
        Image bg = cardObj.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.13f, 0.96f);
        WhiteIconLoader.ApplyGothicFrame(cardObj, new Color(accentCol.r, accentCol.g, accentCol.b, 0.70f), 0.55f);
        cardOutlineOut = null;

        float leftPad = 16f;
        badgeOutlineOut = null;
        iconImgOut = null;

        GameObject iconBadgeObj = CreateUIObject("IconBadge", cardObj.transform);
        RectTransform ibRt = iconBadgeObj.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(16f, 0f);
        ibRt.sizeDelta = new Vector2(62f, 62f);

        Image ibBg = iconBadgeObj.AddComponent<Image>();
        ibBg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        var ibOutline = iconBadgeObj.AddComponent<Outline>();
        ibOutline.effectColor = accentCol;
        ibOutline.effectDistance = new Vector2(2f, -2f);
        badgeOutlineOut = ibOutline;

        GameObject iconInner = CreateUIObject("Icon", iconBadgeObj.transform);
        RectTransform iiRt = iconInner.GetComponent<RectTransform>();
        iiRt.anchorMin = new Vector2(0.5f, 0.5f);
        iiRt.anchorMax = new Vector2(0.5f, 0.5f);
        iiRt.pivot = new Vector2(0.5f, 0.5f);
        iiRt.anchoredPosition = Vector2.zero;
        iiRt.sizeDelta = new Vector2(50f, 50f);

        Image iiImg = iconInner.AddComponent<Image>();
        iiImg.sprite = iconSpr;
        iiImg.preserveAspect = true;
        iiImg.raycastTarget = false;
        iconImgOut = iiImg;

        leftPad = 96f;

        GameObject tObj = CreateUIObject("Title", cardObj.transform);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(leftPad, -10f);
        tRt.sizeDelta = new Vector2(-(leftPad + 16f), 26f);

        titleTmp = tObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = 17.5f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.88f, 0.38f);

        GameObject dObj = CreateUIObject("Desc", cardObj.transform);
        RectTransform dRt = dObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.anchoredPosition = new Vector2(leftPad, -38f);
        dRt.sizeDelta = new Vector2(-(leftPad + 16f), -44f);

        descTmp = dObj.AddComponent<TextMeshProUGUI>();
        descTmp.fontSize = 13.5f;
        descTmp.lineSpacing = 6;
        descTmp.color = new Color(0.90f, 0.94f, 1f);
        descTmp.enableWordWrapping = true;
    }
}
