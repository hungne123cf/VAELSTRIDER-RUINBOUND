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

    // 3 Skill & Trait Detail Cards
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
        int totalAtk = baseAtk + atkBonus + sAtk + augAtk;
        int totalPhysAtk = (!isMage && !isAzure) ? (baseAtk + atkBonus + sPhysAtk + augAtk) : sPhysAtk;
        int totalMagicAtk = (isMage || isAzure) ? (baseAtk + atkBonus + sMagicAtk + augAtk) : sMagicAtk;
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
            atkStatText.text = $"{label}\n<color=#FF5566><size=16><b>{totalAtk}</b></size></color>\n{subStats}";
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

        // Card 1: Attack
        if (attackCardTitle != null)
        {
            if (isMage)
                attackCardTitle.text = isVi ? "1. CẦU MA PHÁP & HỘ THỂ MA TRẬN" : "1. ARCANE ORBS & BARRIER";
            else if (isDune)
                attackCardTitle.text = isVi ? "1. SONG ĐAO & THẾ THỦ PHẢN KÍCH" : "1. DUAL DAGGERS & MIRAGE PARRY";
            else if (isTitan)
                attackCardTitle.text = isVi ? "1. ĐẠI CHÙY & THẾ THỦ NỆN ĐẤT" : "1. WAR HAMMER & EARTHSHAKER";
            else if (isAzure)
                attackCardTitle.text = isVi ? "1. TRỌNG KIẾM & SÓNG BĂNG KIẾM KHÍ" : "1. FROST SWORD WAVES";
            else if (isRonin)
                attackCardTitle.text = isVi ? "1. PHONG NGUYỆT TRẢM & TỤ LỰC IAIDO" : "1. GALE CRESCENT SWEEP & IAIDO";
            else
                attackCardTitle.text = isVi ? "1. ĐẠI KIẾM & THẾ THỦ PHẢN KÍCH" : "1. ANCIENT SWORD & DIVINE PARRY";
        }
        if (attackCardDesc != null)
        {
            if (isMage)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Cầu Ma Pháp:</b> Bắn 3 cầu tự dẫn đường nổ sét lan | Gồng tạo <b>Hộ Thể Ma Trận</b> hấp thụ 80% dame, phản đòn AP & hồi <color=#38BDF8><b>+10 Mana</b></color>!"
                    : $"• <b>DMG Type:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Arcane Orbs:</b> 3 homing orbs with chain lightning | Hold for <b>Barrier</b> absorbing 80% dmg, counter AP & +10 Mana!";
            }
            else if (isDune)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> & <color=#E2E8F0><b>TD</b></color>\n" +
                      $"• <b>Đặc tính:</b> Combo 3 đòn song đao ({totalAtk} ATK). Nhấn Giữ Thế Thủ giảm 70% sát thương, tung Phản Kích gây <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Choáng 1.2s!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> & <color=#E2E8F0><b>TD</b></color>\n" +
                      $"• <b>Trait:</b> Fast 3-hit combo ({totalAtk} ATK). Hold attack for Mirage Guard: 70% DR & Air Slash Counter dealing <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>1.2s Stun!</b></color>";
            }
            else if (isTitan)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> (Nội tại: Giảm 20% sát thương)\n" +
                      $"• <b>Đặc tính:</b> Nện đất ({totalAtk} ATK). Nhấn Giữ gồng Thế Thủ: <color=#55FF88><b>Bất tử 100%</b></color>, thả tay nện đất bộc phá gây <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Choáng 1.0s!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> (Innate: 20% DR)\n" +
                      $"• <b>Trait:</b> Smashes inflicting {totalAtk} ATK. Hold attack for Earthshaker: <color=#55FF88><b>100% Invulnerable</b></color>, release to smash ground inflicting <color=#E2E8F0><b>TD</b></color> + <color=#FFAA33><b>Stun 1.0s!</b></color>";
            }
            else if (isAzure)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Sóng Kiếm Tầm Xa:</b> Phóng sóng kiếm băng xuyên quái (6.5m), hồi <color=#38BDF8><b>+6 Mana</b></color> & giảm 50% thời gian chờ đòn đánh."
                    : $"• <b>DMG Type:</b> <color=#B84DFF><b>AP</b></color> ({totalAtk} AP)\n" +
                      $"• <b>Frost Wave:</b> Piercing wave (6.5m), restores <color=#38BDF8><b>+6 Mana</b></color> & cuts attack cooldown 50%.";
            }
            else if (isRonin)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> & <color=#B84DFF><b>AP</b></color> (Phong Ấn)\n" +
                      $"• <b>Đặc tính:</b> Quét kiếm 180° ({totalAtk} ATK, hồi +10 Mana), tích 3 tầng nổ <color=#FFAA33><b>AP 240% Crit</b></color> + Lốc Xoáy. Khi có Khiên: Gồng phóng Nguyệt Kiếm Khí <color=#FF3B30><b>x2.85 Crit ATK!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> & <color=#B84DFF><b>AP</b></color> (Wind Marks)\n" +
                      $"• <b>Trait:</b> 180° Crescent cleave ({totalAtk} ATK, +10 Mana), 3 marks detonate for <color=#FFAA33><b>AP 240% Crit</b></color> + Vortex. When Shielded: Charge for <color=#FF3B30><b>x2.85 Crit ATK!</b></color>";
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
                dashCardTitle.text = isVi ? "2. TỐC BIẾN HƯ KHÔNG (BLINK)" : "2. ARCANE BLINK DASH";
            else if (isDune)
                dashCardTitle.text = isVi ? "2. ẢO ẢNH SA LƯỚT (3 LẦN LIÊN HOÀN)" : "2. TRIPLE SAND MIRAGE DASH";
            else if (isTitan)
                dashCardTitle.text = isVi ? "2. CHIẾN XA XOAY BÚA 360°" : "2. JUGGERNAUT 360° SPIN";
            else if (isAzure)
                dashCardTitle.text = isVi ? "2. HÀN BĂNG TRƯỢT TUYẾT" : "2. FROST GLIDE DASH";
            else if (isRonin)
                dashCardTitle.text = isVi ? "2. TÀN ẢNH TỐC BIẾN (MURAD STYLE)" : "2. SHADOW PHANTOM WARP";
            else
                dashCardTitle.text = isVi ? "2. THÁNH THUẪN PHẢN ĐÒN" : "2. RADIANT RIPOSTE DASH";
        }
        if (dashCardDesc != null)
        {
            if (isMage)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>15 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Bất tử khi tốc biến</b></color>)\n" +
                      $"• <b>Đặc tính:</b> Dịch chuyển tức thời, để lại tàn ảnh ma pháp nổ đẩy lùi & hồi <color=#38BDF8><b>+12 Mana</b></color>"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>15 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Invulnerable</b></color>)\n" +
                      $"• <b>Trait:</b> Instant warp leaving repulsive arcane clone & restores <color=#38BDF8><b>+12 Mana</b></color>";
            }
            else if (isDune)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#FF5566><b>4% Máu</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Tích lũy tối đa 3 lần lướt liên hoàn cực cơ động, tạo tàn ảnh cát & <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> mỗi lần lướt."
                    : $"• <b>Cost:</b> <color=#FF5566><b>4% HP</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Stores up to 3 Consecutive Dash Charges, leaves mirages & <color=#FFAA33><b>Reduces Ulti CD 1.5s</b></color> per dash.";
            }
            else if (isTitan)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt tốc độ cao xoay búa 360° càn quét gây ATK & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>."
                    : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Heavy spin dash slashing all enemies along path with ATK & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>.";
            }
            else if (isRonin)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>15 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Bất tử khi dịch chuyển</b></color>)\n" +
                      $"• <b>Nhịp 1:</b> Tốc biến 4.8m tới trước (<color=#55FF88><b>Bất tử</b></color>) cắm bóng.\n" +
                      $"• <b>Nhịp 2 (Bấm lại):</b> Lập tức Teleport quay về bóng & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ!</b></color>"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>15 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Invulnerable</b></color>)\n" +
                      $"• <b>Stage 1:</b> Warps 4.8m forward leaving clone (<color=#55FF88><b>Invincible</b></color>).\n" +
                      $"• <b>Stage 2 (Cast again):</b> Instantly Warps back to clone & <color=#FFAA33><b>Cuts Ulti CD by 2.0s!</b></color>";
            }
            else if (isAzure)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt né đòn trên dải băng tuyết & <color=#55FFFF><b>Hồi ngay lập tức đòn đánh thường</b></color> kế tiếp."
                    : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Swift glide dash dodging attacks & <color=#55FFFF><b>Instantly resets basic attack cooldown</b></color>.";
            }
            else
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Bất tử khi lướt</b></color>)\n" +
                      $"• <b>Thánh Thuẫn Phản Đòn:</b> Lướt qua quái/đỡ đòn sẽ <color=#FFAA33><b>Hồi 50% CD Lướt</b></color>, hồi <color=#38BDF8><b>+15 Mana</b></color> & phóng 3 Thánh Kiếm Khí gây ATK!"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> (<color=#55FF88><b>Invulnerable</b></color>)\n" +
                      $"• <b>Radiant Riposte:</b> Deflecting attacks <color=#FFAA33><b>Refunds 50% Dash CD</b></color>, restores <color=#38BDF8><b>+15 Mana</b></color> & fires 3 Holy Waves with ATK!";
            }
        }

        // Card 3: Ulti
        if (ultiCardTitle != null)
        {
            if (isMage)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: THIÊN THẠCH MA TRẬN" : "3. ULTIMATE: METEOR APOCALYPSE";
            else if (isDune)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: CUỒNG BÃO SA ẢNH X-REND" : "3. ULTIMATE: SANDSTORM X-REND";
            else if (isTitan)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: CUỒNG NỘ CHẤN ĐỊA DUNG NHAM" : "3. ULTIMATE: VOLCANIC CATACLYSM SLAM";
            else if (isAzure)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: BÃO SÓNG KIẾM NAN QUẠT" : "3. ULTIMATE: CRESCENT TEMPEST";
            else if (isRonin)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: ÁM ẢNH PHÂN THÂN TRẢM" : "3. ULTIMATE: PHANTOM FLASH CLEAVE";
            else
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: THÁNH KIẾM THIÊN NHAM" : "3. ULTIMATE: HOLY SWORD TEMPEST";
        }
        if (ultiCardDesc != null)
        {
            if (isMage)
            {
                int mageMeteorDmg = (22 * 5) + (totalAtk * 3) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#B84DFF><b>AP</b></color> (+<color=#E2E8F0><b>TD</b></color> từ Ngọc)\n" +
                      $"• <b>Sát thương:</b> <color=#B84DFF><b>{mageMeteorDmg} AP</b></color> (5 Đợt Mưa Thiên Thạch)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Triệu hồi 5 đợt thiên thạch ma pháp giáng thế từ không gian hủy diệt toàn bộ chiến trường"
                    : $"• <b>DMG Type:</b> <color=#B84DFF><b>AP</b></color> (+<color=#E2E8F0><b>TD</b></color> from Gems)\n" +
                      $"• <b>Damage:</b> <color=#B84DFF><b>{mageMeteorDmg} AP</b></color> (5 Waves Meteor Strikes)\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Summons 5 waves of devastating meteor strikes annihilating all battlefield foes";
            }
            else if (isDune)
            {
                int duneUltiDmg = (14 * 6) + (totalAtk * 3) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> (6 Đòn) & <color=#E2E8F0><b>TD</b></color> (X-Rend Trảm)\n" +
                      $"• <b>Sát thương:</b> <color=#FF3B30><b>{duneUltiDmg} ATK</b></color> (6 Trảm + X-Slash Bạo Kích)\n" +
                      $"• <b>Tiêu hao:</b> <color=#FF5566><b>20% Máu</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Khóa mục tiêu (8.5m) -> Hóa bóng cát lướt 6 đòn chém liên hoàn cực tốc rồi chém chéo X-Slash dứt điểm, đẩy lùi toàn bộ quái xung quanh"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> (6 Hits) & <color=#E2E8F0><b>TD</b></color> (X-Rend Finish)\n" +
                      $"• <b>Damage:</b> <color=#FF3B30><b>{duneUltiDmg} ATK</b></color> (6 Flurry Hits + Final Critical X-Slash)\n" +
                      $"• <b>Cost:</b> <color=#FF5566><b>20% HP</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Lock-on (8.5m) -> Dissolves into sand mirages delivering 6 supersonic slashes followed by an explosive X-Rend cross slash knockback";
            }
            else if (isTitan)
            {
                int titanUltiDmg = 95 + (totalAtk * 2) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Hệ Sát Thương:</b> <color=#FF3B30><b>ATK</b></color> (+<color=#E2E8F0><b>TD</b></color> từ Ngọc)\n" +
                      $"• <b>Sát thương:</b> <color=#FF3B30><b>{titanUltiDmg} ATK</b></color> (Địa Chấn Dung Nham)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>45 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Hút quái vật (5.5m) -> Nện đại chùy hất tung và gây choáng diện rộng\n" +
                      $"• <b>Nội tại:</b> <color=#55FF88><b>Kích hoạt ngay Khiên Hộ Thân Đất Đá!</b></color>"
                    : $"• <b>DMG Type:</b> <color=#FF3B30><b>ATK</b></color> (+<color=#E2E8F0><b>TD</b></color> from Gems)\n" +
                      $"• <b>Damage:</b> <color=#FF3B30><b>{titanUltiDmg} ATK</b></color> (Cataclysm Slam)\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>45 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Draws enemies (5.5m) -> Cataclysmic slam launches and stuns all foes\n" +
                      $"• <b>Special:</b> <color=#55FF88><b>Instantly triggers Titan Bulwark Shield!</b></color>";
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
        dImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);
        WhiteIconLoader.ApplyGothicFrame(dialogBox, new Color(0.85f, 0.72f, 0.38f, 0.90f), 0.35f);

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
        WhiteIconLoader.ReplaceTextWithIcon(cBtn, "Cross", Color.white, new Vector2(24f, 24f));

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
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        Sprite sprAttack = LoadSprite("Assets/Sprites/UI/Attack.png", "UI/Attack");
        Sprite sprDash = LoadSprite("Assets/Sprites/UI/Dash.png", "UI/Dash");
        Sprite sprUlti = LoadSprite("Assets/Sprites/UI/Ulti.png", "UI/Ulti");

        CreateSkillCard(cardsSection.transform, "Card_Attack", "1. ĐÒN ĐÁNH CƠ BẢN", sprAttack, new Color(1f, 0.45f, 0.25f), out uiComp.attackCardDesc, out uiComp.attackCardTitle, out uiComp.attackCardIcon, out uiComp.attackCardOutline, out uiComp.attackBadgeOutline);
        CreateSkillCard(cardsSection.transform, "Card_Dash", "2. LƯỚT NÉ ĐÒN", sprDash, new Color(0.2f, 0.95f, 0.65f), out uiComp.dashCardDesc, out uiComp.dashCardTitle, out uiComp.dashCardIcon, out uiComp.dashCardOutline, out uiComp.dashBadgeOutline);
        CreateSkillCard(cardsSection.transform, "Card_Ulti", "3. TUYỆT KỸ", sprUlti, new Color(0.75f, 0.5f, 1f), out uiComp.ultiCardDesc, out uiComp.ultiCardTitle, out uiComp.ultiCardIcon, out uiComp.ultiCardOutline, out uiComp.ultiBadgeOutline);

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

        float leftPad = 25f;
        badgeOutlineOut = null;
        iconImgOut = null;

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
        badgeOutlineOut = ibOutline;

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
        iconImgOut = iiImg;

        leftPad = 125f;

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
