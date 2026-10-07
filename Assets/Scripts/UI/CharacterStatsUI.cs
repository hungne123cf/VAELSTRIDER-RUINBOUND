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
        bool isTitan = (selectedCharId == CharacterManager.CHAR_TITAN_BERSERKER || selectedCharId.ToLower().Contains("titan") || selectedCharId.ToLower().Contains("player4"));
        bool isAzure = (selectedCharId == CharacterManager.CHAR_AZURE_BLADE || selectedCharId.ToLower().Contains("azure") || selectedCharId.ToLower().Contains("player3"));
        bool isRonin = (selectedCharId == CharacterManager.CHAR_GALE_RONIN || selectedCharId.ToLower().Contains("ronin") || selectedCharId.ToLower().Contains("gale") || selectedCharId == "Maid");
        bool isVi = LocalizationManager.IsVietnamese;

        int atkBonus = upgradeMgr != null ? upgradeMgr.GetAttackBonus() : 0;
        int hpBonus = upgradeMgr != null ? upgradeMgr.GetHealthBonus() : 0;
        float dashCdRed = upgradeMgr != null ? upgradeMgr.GetDashCooldownReduction() : 0f;
        float ultiCdRed = upgradeMgr != null ? upgradeMgr.GetUltiCooldownReduction() : 0f;
        int ultiDmgBonus = upgradeMgr != null ? upgradeMgr.GetUltiDamageBonus() : 0;

        // Gem Socket Bonuses
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out int sAtk, out int sHp, out int sDef, out float sLifesteal, out float sCd, out float sShard, out float sRegen);

        int baseAtk = isTitan ? 12 : (isAzure ? 6 : (isRonin ? 6 : 5));
        int baseHp = isTitan ? 200 : (isAzure ? 110 : (isRonin ? 95 : 100));
        int totalAtk = baseAtk + atkBonus + sAtk;
        int totalHp = baseHp + hpBonus + sHp;
        float finalDashCd = Mathf.Max(1.0f, 5.0f - dashCdRed);
        float baseUltiCd = isTitan ? 12.0f : (isAzure ? 18.0f : (isRonin ? 15.0f : 24.0f));
        float gemCdSec = baseUltiCd * sCd;
        float finalUltiCd = Mathf.Max(5.0f, baseUltiCd - ultiCdRed - gemCdSec);

        // Đảm bảo đồng bộ 100% với giá trị Runtime Skill Component thực tế
        var pcObj = FindAnyObjectByType<PlayerCombat>();
        if (pcObj != null)
        {
            pcObj.RecalculateCombatStats();
            var roninSkill = pcObj.GetComponent<GaleRoninSkill>();
            var azureSkill = pcObj.GetComponent<AzureBladeSkill>();
            var titanSkill = pcObj.GetComponent<TitanBerserkerSkill>();
            if (isTitan && titanSkill != null)
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
        Color heroAccent = isTitan ? new Color(1f, 0.65f, 0.2f) : (isAzure ? new Color(0.35f, 0.88f, 1f) : (isRonin ? new Color(0.28f, 1f, 0.75f) : new Color(1f, 0.85f, 0.35f)));

        Sprite sprAttack = null;
        Sprite sprDash = null;
        Sprite sprUlti = null;

        if (isTitan)
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
            sprAttack = LoadSprite("Assets/Sprites/UI/Attack.png", "UI/Attack");
            sprDash = LoadSprite("Assets/Sprites/UI/Dash.png", "UI/Dash");
            sprUlti = LoadSprite("Assets/Sprites/UI/Ulti.png", "UI/Ulti");
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

        // Header Title
        if (characterTitleText != null)
        {
            if (isTitan)
                characterTitleText.text = isVi ? "CUỒNG CHIẾN BINH (TITAN BERSERKER)" : "TITAN BERSERKER (HEAVY TANKER)";
            else if (isAzure)
                characterTitleText.text = isVi ? "LAM KIẾM SĨ (AZURE BLADE)" : "AZURE BLADE (SWORDSWOMAN)";
            else if (isRonin)
                characterTitleText.text = isVi ? "PHONG LÔI LÃNG KHÁCH (GALE RONIN)" : "GALE RONIN (PHANTOM BLADE)";
            else
                characterTitleText.text = isVi ? "HIỆP SĨ VAELSTRIDER" : "KNIGHT VAELSTRIDER";
            characterTitleText.color = heroAccent;
        }
        if (characterSubtitleText != null)
        {
            if (isTitan)
                characterSubtitleText.text = isVi ? "Phong cách: Đại Chùy Tanker | Nện búa chấn động, Giảm 20% sát thương & Cuồng nộ chấn địa" : "Style: Heavy Titan Tanker | Conic Hammer Smashes, Innate 20% DR & Cataclysm Slam";
            else if (isAzure)
                characterSubtitleText.text = isVi ? "Phong cách: Trọng Kiếm Khí | Chém chậm xuất Sóng Kiếm xuyên quái & Bão kiếm nan quạt" : "Style: Heavy Crescent | Slow Slashes with Piercing Waves & Fan Spread Tempest";
            else if (isRonin)
                characterSubtitleText.text = isVi ? "Phong cách: Bán Nguyệt Phong Ấn | Chém quét 180° tích tầng Phong Ấn bộc phá & Lốc xoáy mini" : "Style: Crescent Wind | 180° Cleave, Wind Marks Stack Burst & Mini Vortex";
            else
                characterSubtitleText.text = isVi ? "Phong cách: Kiếm Sĩ Cổ Tộc | Đột kích liên hoàn & Bão kiếm quét rộng" : "Style: Ancient Blade | Multi-slash Combo & Area Storm";
        }

        // 4 Quick Stats Badges
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
            if (isTitan)
                attackCardTitle.text = isVi ? "1. ĐẠI CHÙY CHẤN ĐỊA (140°)" : "1. TITAN HAMMER SMASH (140°)";
            else if (isAzure)
                attackCardTitle.text = isVi ? "1. SONG BĂNG ĐOẢN KIẾM & SÓNG KIẾM" : "1. TWIN FROST BLADES & SWORD WAVE";
            else if (isRonin)
                attackCardTitle.text = isVi ? "1. PHONG NGUYỆT TRẢM (180°)" : "1. GALE CRESCENT SWEEP";
            else
                attackCardTitle.text = isVi ? "1. ĐẠI KIẾM HOÀNG KIM" : "1. ANCIENT BROADSWORD";
        }
        if (attackCardDesc != null)
        {
            if (isTitan)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> | Nện đất hình nón hất lùi quái\n" +
                      $"• <b>Nội tại:</b> Tích lũy <b>Cuồng Nộ</b> khi đánh trúng, kích hoạt <b>Khiên Đất Đá</b>"
                    : $"• <b>Damage:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> | Conic Shockwave Knockback\n" +
                      $"• <b>Passive:</b> Builds <b>Berserk Rage</b> on hit, activates <b>Titan Bulwark</b>";
            }
            else if (isAzure)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> | Phóng <color=#55FFFF><b>Sóng Kiếm Băng</b></color> xuyên quái (Tầm 6.5m)\n" +
                      $"• <b>Hiệu ứng:</b> Đánh trúng hồi <color=#38BDF8><b>+6 Năng Lượng</b></color> & Giảm 50% hồi đòn đánh"
                    : $"• <b>Damage:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> | Fires <color=#55FFFF><b>Frost Sword Waves</b></color> (6.5m)\n" +
                      $"• <b>Effect:</b> Restores <color=#38BDF8><b>+6 Energy</b></color> & Cuts attack cooldown by 50% on hit";
            }
            else if (isRonin)
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> | Quét bán nguyệt 180° cực nhanh\n" +
                      $"• <b>Nội tại:</b> Tích <b>Phong Ấn</b> (3 tầng nổ <color=#FFAA33><b>240% Crit</b></color> + tạo <b>Lốc Xoáy hút quái</b>)"
                    : $"• <b>Damage:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> | Fast 180° Crescent Cleave\n" +
                      $"• <b>Passive:</b> Stacks <b>Wind Marks</b> (3 stacks detonates for <color=#FFAA33><b>240% Crit</b></color> + <b>Vacuum Vortex</b>)";
            }
            else
            {
                attackCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> (Tầm đánh: 1.35m)\n" +
                      $"• <b>Hiệu ứng:</b> Đánh trúng hồi <color=#38BDF8><b>+8 Năng Lượng</b></color>"
                    : $"• <b>Damage:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> (Range: 1.35m)\n" +
                      $"• <b>Effect:</b> Restores <color=#38BDF8><b>+8 Energy</b></color> on hit";
            }
        }

        // Card 2: Dash
        if (dashCardTitle != null)
        {
            if (isTitan)
                dashCardTitle.text = isVi ? "2. CHIẾN XA BẠO ĐỘT" : "2. TITAN JUGGERNAUT DASH";
            else if (isAzure)
                dashCardTitle.text = isVi ? "2. HÀN BĂNG TRƯỢT TUYẾT" : "2. FROST GLIDE DASH";
            else if (isRonin)
                dashCardTitle.text = isVi ? "2. PHONG THIỂM TỐC LƯỚT" : "2. GALE FLASH DASH";
            else
                dashCardTitle.text = isVi ? "2. BỤI CỔ TỐC BIẾN" : "2. RUIN DUST DASH";
        }
        if (dashCardDesc != null)
        {
            if (isTitan)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt càn quét (bất tử 0.2s) & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Heavy dash with 0.2s Invincibility & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>";
            }
            else if (isRonin)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt né đòn (bất tử 0.2s) & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Dash with 0.2s Invincibility & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>";
            }
            else if (isAzure)
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt né đòn & <color=#55FFFF><b>Hồi ngay đòn đánh thường</b></color>"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Dash forward & <color=#55FFFF><b>Instantly resets basic attack</b></color>";
            }
            else
            {
                dashCardDesc.text = isVi
                    ? $"• <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Đặc tính:</b> Lướt nhanh và <color=#55FF88><b>bất tử 0.2s</b></color> né mọi sát thương"
                    : $"• <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color>\n" +
                      $"• <b>Trait:</b> Dash forward with <color=#55FF88><b>0.2s Invincibility</b></color>";
            }
        }

        // Card 3: Ulti
        if (ultiCardTitle != null)
        {
            if (isTitan)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: CUỒNG NỘ CHẤN ĐỊA" : "3. ULTIMATE: TITAN CATACLYSM SLAM";
            else if (isAzure)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: 5 TIA BĂNG NGUYỆT SÓNG" : "3. ULTIMATE: 5-WAY AZURE TEMPEST";
            else if (isRonin)
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: ÁM ẢNH PHÂN THÂN TRẢM" : "3. ULTIMATE: SHADOW GALE STRIKE";
            else
                ultiCardTitle.text = isVi ? "3. TUYỆT KỸ: BÃO XOÁY CỔ KIẾM" : "3. ULTIMATE: RUIN WHIRLWIND";
        }
        if (ultiCardDesc != null)
        {
            if (isTitan)
            {
                int titanUltiDmg = 95 + (totalAtk * 2) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF5566><b>{titanUltiDmg} Sát thương Địa Chấn Dung Nham</b></color>\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>45 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Hút quái vật (5.5m) -> Nện đại chùy hất tung và gây choáng diện rộng\n" +
                      $"• <b>Nội tại:</b> <color=#55FF88><b>Kích hoạt ngay Khiên Hộ Thân Đất Đá!</b></color>"
                    : $"• <b>Damage:</b> <color=#FF6677><b>{titanUltiDmg} Cataclysm Seismic Damage</b></color>\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>45 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Draws enemies (5.5m) -> Cataclysmic slam launches and stuns all foes\n" +
                      $"• <b>Special:</b> <color=#55FF88><b>Instantly triggers Titan Bulwark Shield!</b></color>";
            }
            else if (isAzure)
            {
                int azureWaveDmg = 48 + totalAtk + ultiDmgBonus;
                int azureTotalDmg = azureWaveDmg * 5;

                ultiCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF5566><b>{azureWaveDmg} ATK</b></color> x 5 Tia (Tổng: <color=#FFAA33><b>{azureTotalDmg} Sát thương</b></color>)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Phóng 5 luồng đại kiếm khí nan quạt quét sạch chiến trường"
                    : $"• <b>Damage:</b> <color=#FF5566><b>{azureWaveDmg} ATK</b></color> x 5 Waves (Total: <color=#FFAA33><b>{azureTotalDmg}</b></color>)\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Fires 5 massive fan-spread crescent waves piercing enemies";
            }
            else if (isRonin)
            {
                int roninUltiDmg = (28 * 3) + (totalAtk * 2) + ultiDmgBonus;

                ultiCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF5566><b>{roninUltiDmg} Sát thương Bạo Kích Nhất Trảm</b></color>\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>40 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Khóa mục tiêu (10.5m) -> Dịch chuyển ra sau lưng trảm cực mạnh rồi biến về vị trí cũ\n" +
                      $"• <b>Nội tại:</b> <color=#55FF88><b>Kết liễu mục tiêu hồi chiêu Tuyệt Kỹ ngay lập tức!</b></color>"
                    : $"• <b>Damage:</b> <color=#FF5566><b>{roninUltiDmg} Execution Critical Strike</b></color>\n" +
                      $"• <b>Cost:</b> <color=#38BDF8><b>40 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Effect:</b> Target lock-on (10.5m) -> Teleports behind enemy for a devastating strike then returns\n" +
                      $"• <b>Special:</b> <color=#55FF88><b>Executing target instantly resets Ultimate Cooldown!</b></color>";
            }
            else
            {
                int vaelFinalHit = 50 + totalAtk + ultiDmgBonus;
                int vaelTotal = (20 + (int)(totalAtk * 0.4f)) * 2 + (25 + (int)(totalAtk * 0.5f)) + vaelFinalHit;

                ultiCardDesc.text = isVi
                    ? $"• <b>Sát thương:</b> <color=#FF5566><b>{vaelTotal}</b></color> (4 Đợt Bão Kiếm, Đòn cuối: <color=#FFAA33><b>{vaelFinalHit}</b></color>)\n" +
                      $"• <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color>\n" +
                      $"• <b>Hiệu ứng:</b> Triệu hồi bão kiếm quét 4 đợt diện rộng, hút và dồn sát thương"
                    : $"• <b>Damage:</b> <color=#FF5566><b>{vaelTotal}</b></color> (4-Hit Whirlwind, Final: <color=#FFAA33><b>{vaelFinalHit}</b></color>)\n" +
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
        tmp.fontSize = 13.5f;
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
        bg.color = new Color(0.10f, 0.13f, 0.20f, 0.96f);

        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(accentCol.r, accentCol.g, accentCol.b, 0.65f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        cardOutlineOut = outline;

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
