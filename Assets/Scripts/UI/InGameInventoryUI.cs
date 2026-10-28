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

    /// <summary>
    /// Cho phép bật/tắt toàn bộ hệ thống Túi Đồ và Trang bị 6 ô trong trận.
    /// Đặt false để ẩn hoàn toàn nút Bag và giao diện Túi Đồ.
    /// </summary>
    public static bool EnableInGameInventory = false;

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
        if (!EnableInGameInventory)
        {
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
        LoadAssets();
    }

    void Start()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        RefreshModal();
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

    private float liveRefreshTimer = 0f;

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

        // Đếm thời gian thực khi mở túi, CHỈ cập nhật text thông số & đếm lùi Buff thời hạn, TUYỆT ĐỐI KHÔNG recreate card đồ
        if (modalPanel != null && modalPanel.activeSelf)
        {
            liveRefreshTimer += Time.unscaledDeltaTime;
            if (liveRefreshTimer >= 0.2f)
            {
                liveRefreshTimer = 0f;
                UpdateLiveStatsText();
            }
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
        // Giữ nguyên thời gian trôi tự nhiên trong trận, không đóng băng (timeScale = 1)
        Time.timeScale = 1f;
        RefreshModal();
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void Close()
    {
        EquipmentItemDetailPopupUI.Hide();
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
        GameAutoBootstrap.SetGameplayHUDVisible(true);
        Time.timeScale = 1f;
        NotifyPlayerCombatStatsChanged();
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    private void UpdateLiveStatsText()
    {
        if (heroStatsText == null || modalPanel == null || !modalPanel.activeSelf) return;

        string curHero = EquipmentSocketManager.GetActiveHeroId();
        string h = (curHero ?? "").ToLower();
        bool isVi = LocalizationManager.IsVietnamese;

        int baseHp = 100;
        int baseAtk = 20;
        float sLife = 0f;

        if (h.Contains("titan") || h.Contains("player4")) { baseHp = 200; baseAtk = 32; }
        else if (h.Contains("azure") || h.Contains("player3")) { baseHp = 110; baseAtk = 24; }
        else if (h.Contains("ronin") || h.Contains("gale") || h.Contains("maid") || h.Contains("elysia")) { baseHp = 95; baseAtk = 28; }
        else if (h.Contains("dune") || h.Contains("player5")) { baseHp = 110; baseAtk = 18; }
        else if (h.Contains("mage") || h.Contains("celestial") || h.Contains("player6")) { baseHp = 90; baseAtk = 30; }
        else { baseHp = 100; baseAtk = 20; }

        EquipmentSocketManager.GetTotalHeroSocketBonus(
            out int sAtk, out int sPhysAtk, out int sMagicAtk, out int sTrueAtk,
            out int sHp, out int sDef,
            out float sLifesteal, out float sCd, out float sShard, out float sRegen,
            out float sMaxEn, out float sEnRegen,
            out float sPhysLife, out float sSpellVamp, out float sCrit,
            curHero);

        int upHp = PlayerUpgradeManager.EnsureInstance()?.GetHealthBonus(curHero) ?? 0;
        int upAtk = PlayerUpgradeManager.EnsureInstance()?.GetAttackBonus(curHero) ?? 0;
        float upDashCd = PlayerUpgradeManager.EnsureInstance()?.GetDashCooldownReduction(curHero) ?? 0f;

        int augHp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusHp() : 0;
        int augAtk = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusAtk() : 0;
        int augDef = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugDef() : 0;
        float augCrit = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCrit() : 0f;
        float augCritDmg = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCritDmg() : 0f;
        float augLife = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugLifesteal() : 0f;
        float augSpellVamp = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugSpellVamp() : 0f;
        float augCDR = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugCDR() : 0f;
        float augSpeed = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusSpeed() : 0f;

        int totalHp = baseHp + upHp + sHp + augHp;
        int totalAtk = baseAtk + upAtk + sAtk + augAtk;
        int totalDef = sDef + augDef;
        float finalCd = sCd + augCDR;

        string hpAdd = (upHp + sHp + augHp > 0) ? $" <color=#22C55E>(+{upHp + sHp + augHp})</color>" : "";
        string defAdd = totalDef > 0 ? $" <color=#38BDF8>(+{totalDef})</color>" : "";
        string regenStr = sRegen > 0f ? $" <color=#10B981>(+{sRegen:0.#}/s)</color>" : "";
        string atkAdd = (upAtk + sAtk + augAtk > 0) ? $" <color=#FF3B30>(+{upAtk + sAtk + augAtk})</color>" : "";
        string manaAdd = sMaxEn > 0 ? $" <color=#00E5FF>(+{(int)sMaxEn})</color>" : "";
        string manaRegenStr = sEnRegen > 0f ? $" <color=#06B6D4>(+{sEnRegen:0.#}/s)</color>" : "";

        bool isMagicHero = (h.Contains("azure") || h.Contains("player3") || h.Contains("mage") || h.Contains("celestial") || h.Contains("player6"));
        int totalPhys = (!isMagicHero ? (baseAtk + upAtk + augAtk) : 0) + sPhysAtk;
        int totalMagic = (isMagicHero ? (baseAtk + upAtk + augAtk) : 0) + sMagicAtk;
        int totalTrue = sTrueAtk;

        List<string> dmgTags = new List<string>();
        if (totalPhys > 0) dmgTags.Add($"<color=#FF3B30>{totalPhys} ATK</color>");
        if (totalMagic > 0) dmgTags.Add($"<color=#B84DFF>{totalMagic} AP</color>");
        if (totalTrue > 0) dmgTags.Add($"<color=#E2E8F0>{totalTrue} TD</color>");
        string dmgBreakdownStr = dmgTags.Count > 0 ? string.Join(" | ", dmgTags) : "<color=#94A3B8>None</color>";

        float totalCritBonus = sCrit + augCrit;
        int totalCritPct = 20 + Mathf.RoundToInt(totalCritBonus * 100);
        string critBonusStr = totalCritBonus > 0.001f ? $" <color=#F59E0B>(+{Mathf.RoundToInt(totalCritBonus * 100)}%)</color>" : "";

        float finalCritMultiplier = 1.75f + augCritDmg;
        string critDmgBonusStr = augCritDmg > 0.001f ? $" <color=#F59E0B>(+{Mathf.RoundToInt(augCritDmg * 100)}%)</color>" : "";
        string critDmgValStr = $"x{finalCritMultiplier:0.##}";

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

        float baseSpeed = 5.0f;
        float totalSpeed = baseSpeed * (1f + augSpeed);
        string speedBonusStr = augSpeed > 0.001f ? $" <color=#38BDF8>(+{Mathf.RoundToInt(augSpeed * 100)}%)</color>" : "";

        List<string> statLines = new List<string>
        {
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "SINH LỰC (HP):" : "HEALTH (HP):")}</b> <color=#22C55E>{totalHp}</color>{hpAdd}{regenStr}",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "GIÁP PHÒNG THỦ:" : "DEFENSE (DEF):")}</b> <color=#38BDF8>{totalDef}</color>{defAdd}",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "TỔNG CÔNG KÍCH:" : "TOTAL ATTACK:")}</b> <color=#FF3B30>{totalAtk}</color>{atkAdd}",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "PHÂN LOẠI SÁT THƯƠNG:" : "DAMAGE TYPE:")}</b> {dmgBreakdownStr}",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "CHÍ MẠNG / BẠO KÍCH:" : "CRITICAL STATS:")}</b> <color=#F59E0B>{totalCritPct}%</color>{critBonusStr} | <color=#F59E0B>{critDmgValStr}</color>{critDmgBonusStr}",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "NĂNG LƯỢNG (MANA):" : "ENERGY (MANA):")}</b> <color=#00E5FF>{100 + (int)sMaxEn}</color>{manaAdd}{manaRegenStr}",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "HÚT MÁU / HỒI PHỤC:" : "VAMP & HEALING:")}</b> <color=#F43F5E>{physLife}% {(isVi ? "VL" : "Phys")}</color> | <color=#E879F9>{spellVamp}% {(isVi ? "Phép" : "Spell")}</color>",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "TỐC ĐỘ DI CHUYỂN:" : "MOVE SPEED:")}</b> <color=#38BDF8>{totalSpeed:0.#}</color>{speedBonusStr}",
            $"<color=#E2E8F0>•</color> <b>{(isVi ? "GIẢM COOLDOWN:" : "CD REDUCTION:")}</b> <color=#C084FC>{cdTag}</color>" + (shardPct > 0 ? $" | <b>SHARD:</b> <color=#FBBF24>+{shardPct}%</color>" : "")
        };

        if (AugmentManager.Instance != null && AugmentManager.Instance.HasActiveTimedAugments())
        {
            var timed = AugmentManager.Instance.GetActiveTimedAugments();
            List<string> buffTags = new List<string>();
            foreach (var kvp in timed)
            {
                string bName = isVi ? kvp.Key.nameVN : kvp.Key.nameEN;
                buffTags.Add($"<b>{bName}</b>: <color=#FBBF24>{kvp.Value:0.#}s</color>");
            }
            if (buffTags.Count > 0)
            {
                statLines.Add($"<color=#E2E8F0>•</color> <b>{(isVi ? "BUFF THỜI HẠN:" : "ACTIVE BUFFS:")}</b> {string.Join(", ", buffTags)}");
            }
        }

        heroStatsText.text = string.Join("\n", statLines);
    }

    public void RefreshModal()
    {
        RefreshModal(false);
    }

    public void RefreshModal(bool force)
    {
        if (modalPanel == null || !modalPanel.activeSelf) return;
        if (!force && UIDragDropItem.IsAnyDragging) return; // Tuyệt đối không recreate UI khi đang kéo thả ngọc/trang bị

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

    public void NotifyPlayerCombatStatsChanged()
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
        if (skillIdx == 0) return WhiteIconLoader.GetSprite("Star") ?? MobileActionButton.GetHeroActionIcon(h, MobileActionButton.ButtonType.Attack);
        if (skillIdx == 1) return MobileActionButton.GetHeroActionIcon(h, MobileActionButton.ButtonType.Attack);
        if (skillIdx == 2) return MobileActionButton.GetHeroActionIcon(h, MobileActionButton.ButtonType.Dash);
        return MobileActionButton.GetHeroActionIcon(h, MobileActionButton.ButtonType.Skill);
    }

    private static Sprite LoadHeroAvatarSprite(string heroId)
    {
        Sprite face = PlayerAvatarUI.GetHeroFaceSprite(heroId);
        if (face != null) return face;

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
            heroAvatarPreviewImage.rectTransform.anchoredPosition = MainMenuManager.GetHeroVisualOffset(h);
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
        float augSpeed = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugBonusSpeed() : 0f;

        int totalHp = baseHp + upHp + sHp + augHp;
        int totalAtk = baseAtk + upAtk + sAtk + augAtk;
        int totalDef = sDef + augDef;
        float finalCd = sCd + augCDR;

        if (heroStatsText != null)
        {
            heroStatsText.fontSize = 13.5f;
            heroStatsText.lineSpacing = -2f;
            RectTransform stRt = heroStatsText.GetComponent<RectTransform>();
            if (stRt != null)
            {
                stRt.anchoredPosition = new Vector2(14f, -70f);
                stRt.sizeDelta = new Vector2(-28f, 315f);
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

            // Tốc độ di chuyển (Move Speed)
            float baseSpeed = 5.0f;
            float totalSpeed = baseSpeed * (1f + augSpeed);
            string speedBonusStr = augSpeed > 0.001f ? $" <color=#38BDF8>(+{Mathf.RoundToInt(augSpeed * 100)}%)</color>" : "";

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
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "TỐC ĐỘ DI CHUYỂN:" : "MOVE SPEED:")}</b> <color=#38BDF8>{totalSpeed:0.#}</color>{speedBonusStr}",
                $"<color=#E2E8F0>•</color> <b>{(isVi ? "GIẢM COOLDOWN:" : "CD REDUCTION:")}</b> <color=#C084FC>{cdTag}</color>" + (shardPct > 0 ? $" | <b>SHARD:</b> <color=#FBBF24>+{shardPct}%</color>" : "")
            };

            // Hiển thị các Lõi Tạm Thời (Timed Buffs) đang có hiệu lực
            if (AugmentManager.Instance != null && AugmentManager.Instance.HasActiveTimedAugments())
            {
                var timed = AugmentManager.Instance.GetActiveTimedAugments();
                List<string> buffTags = new List<string>();
                foreach (var kvp in timed)
                {
                    string bName = isVi ? kvp.Key.nameVN : kvp.Key.nameEN;
                    buffTags.Add($"<b>{bName}</b>: <color=#FBBF24>{kvp.Value:0.#}s</color>");
                }
                if (buffTags.Count > 0)
                {
                    statLines.Add($"<color=#E2E8F0>•</color> <b>{(isVi ? "BUFF THỜI HẠN:" : "ACTIVE BUFFS:")}</b> {string.Join(", ", buffTags)}");
                }
            }

            heroStatsText.text = string.Join("\n", statLines);
        }

        // 2. Render 6 Sockets around the Avatar & Conduit Lines
        for (int s = 0; s < EquipmentSocketManager.HERO_SOCKET_COUNT && s < socketSlotObjs.Count; s++)
        {
            int slotIdx = s;
            var sObj = socketSlotObjs[slotIdx];
            if (sObj == null) continue;

            sObj.transform.SetAsLastSibling();
            foreach (Transform ch in sObj.transform) Destroy(ch.gameObject);

            // Always ensure UIDropSocketTarget is ready and active on each slot
            var dropTarget = sObj.GetComponent<UIDropSocketTarget>();
            if (dropTarget == null) dropTarget = sObj.AddComponent<UIDropSocketTarget>();
            dropTarget.socketSlotIndex = slotIdx;
            dropTarget.onGemDropped = (droppedGemId, targetSlot) =>
            {
                string curHero = EquipmentSocketManager.GetActiveHeroId();
                if (EquipmentSocketManager.SocketGemToHeroSlot(targetSlot, droppedGemId, out _, out _, curHero))
                {
                    SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                    NotifyPlayerCombatStatsChanged();
                    RefreshModal();
                }
            };

            bool hasItem = EquipmentSocketManager.GetHeroSocketGem(slotIdx, out string gemId, out int itemLevel, heroId);
            var sImg = sObj.GetComponent<Image>();
            if (sImg != null) sImg.raycastTarget = true;
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

                int curSlot = slotIdx;
                string curGemId = gemId;
                int curLevel = itemLevel;
                string curHero = heroId;

                Action openPopupAction = () =>
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                    EquipmentItemDetailPopupUI.EnsureUIExists(transform);
                    EquipmentItemDetailPopupUI.Show(curSlot, curGemId, curLevel, curHero, () =>
                    {
                        NotifyPlayerCombatStatsChanged();
                        RefreshModal();
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
                // Xóa bỏ Drag Drop component cũ khi ô này trống
                var sDrag = sObj.GetComponent<UIDragDropItem>();
                if (sDrag != null) Destroy(sDrag);

                // Reset click listener của ô trống để chọn ô khảm mong muốn
                var sBtn = sObj.GetComponent<Button>();
                if (sBtn != null)
                {
                    sBtn.onClick.RemoveAllListeners();
                    sBtn.onClick.AddListener(() =>
                    {
                        selectedSocketSlotIndex = slotIdx;
                        RefreshModal();
                        SoundManager.EnsureInstance()?.PlayMenuHover();
                    });
                }

                if (slotIdx < socketConduitLines.Count && socketConduitLines[slotIdx] != null)
                {
                    socketConduitLines[slotIdx].color = new Color(0.85f, 0.72f, 0.35f, 0.25f);
                }

                bool isSelectedEmpty = (selectedSocketSlotIndex == slotIdx);
                if (sImg != null)
                {
                    sImg.color = isSelectedEmpty ? new Color(0.08f, 0.16f, 0.28f, 0.98f) : new Color(0.04f, 0.06f, 0.10f, 0.90f);
                    sImg.raycastTarget = true;
                }
                if (sOutline != null)
                {
                    sOutline.effectColor = isSelectedEmpty ? new Color(0f, 0.95f, 1f, 1f) : new Color(0.35f, 0.45f, 0.60f, 0.5f);
                    sOutline.effectDistance = isSelectedEmpty ? new Vector2(2f, -2f) : new Vector2(1.2f, -1.2f);
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
                string slotNumCol = isSelectedEmpty ? "#38BDF8" : "#94A3B8";
                string plusCol = isSelectedEmpty ? "#38BDF8" : "#64748B";
                lblTmp.text = $"<size=11><color={slotNumCol}><b>#{slotIdx + 1}</b></color></size>\n<size=15><color={plusCol}>+</color></size>";
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

        string[] skillTags = new string[4];
        string[] skillNames;
        string[] skillDescs;
        string[] skillCosts;

        if (isMage)
        {
            skillTags = new string[] {
                isVi ? "NỘI TẠI" : "PASSIVE",
                "AP",
                isVi ? "TỐC BIẾN" : "BLINK",
                "AP"
            };
            skillNames = new string[] {
                isVi ? "NỘI TẠI: TINH TÚ CỘNG HƯỞNG" : "PASSIVE: CELESTIAL RESONANCE",
                isVi ? "1. CẦU MA PHÁP (GỒNG KHIÊN)" : "1. ARCANE ORBS & BARRIER",
                isVi ? "2. TỐC BIẾN HƯ KHÔNG (BLINK)" : "2. ARCANE BLINK DASH",
                isVi ? "3. THIÊN THẠCH MA TRẬN" : "3. METEOR APOCALYPSE"
            };
            skillCosts = new string[] {
                "<color=#D9A6FF><b>[BỊ ĐỘNG]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#00E5FF><b>[60 Mana]</b></color>"
            };
            int meteorDmg = (22 * 5) + (totalAtk * 3);
            skillDescs = new string[] {
                isVi ? "Cầu ma pháp bắn trúng có 35% tỉ lệ bộc phát sét ma pháp lan sang mục tiêu lân cận & tự động hồi <color=#38BDF8><b>+4 Mana</b></color>. <color=#FFDF66><b>[THẦN NỘ TINH TÚ]:</b></color> Đầy 100% Nội Năng tím bùng nổ trạng thái <color=#B84DFF><b>Overdrive (8s)</b></color>: Hồi Tuyệt Kỹ giảm về <color=#55FF88><b>1.0s</b></color> (xả thiên thạch liên tục), tiêu hao giảm còn 25 Mana & lập tức hồi <color=#38BDF8><b>+25 Mana</b></color>!"
                     : "Arcane spells have 35% chance to chain celestial lightning to adjacent targets & restore <color=#38BDF8><b>+4 Mana</b></color>. <color=#FFDF66><b>[CELESTIAL OVERDRIVE]:</b></color> 100% Inner Energy triggers <color=#B84DFF><b>Overdrive (8s)</b></color>: Ultimate CD drops to <color=#55FF88><b>1.0s</b></color> (meteor spam), cost reduced to 25 Mana & instantly restores <color=#38BDF8><b>+25 Mana</b></color>!",
                isVi ? $"Bắn cầu ma pháp tự dẫn đường thần tốc gây <color=#B84DFF><b>{totalAtk} AP</b></color>. <color=#FFDF66><b>[Nhấn Giữ Gồng Khiên Hộ Thể]:</b></color> Giảm 60% sát thương nhận vào, nổ đẩy lùi phản đòn <color=#B84DFF><b>AP</b></color> & hồi ngay <color=#38BDF8><b>+10 Mana</b></color>!"
                     : $"Fires high-speed homing celestial orb dealing <color=#B84DFF><b>{totalAtk} AP</b></color>. <color=#FFDF66><b>[Hold Astral Barrier]:</b></color> 60% DR, bursts back foes with <color=#B84DFF><b>AP</b></color> & restores <color=#38BDF8><b>+10 Mana</b></color>!",
                isVi ? "Dịch chuyển tức thời 3.2m né đòn, để lại Vết Rách Hư Không xoay tròn gây <color=#B84DFF><b>AP liên tục (1.6s)</b></color>, <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> & hồi ngay <color=#38BDF8><b>+12 Mana</b></color>."
                     : "Instant 3.2m void warp to dodge, leaving a Singularity Rift dealing <color=#B84DFF><b>continuous AP (1.6s)</b></color>, <color=#FFAA33><b>Reduces Ulti CD by 1.5s</b></color> & restores <color=#38BDF8><b>+12 Mana</b></color>.",
                isVi ? $"Mở đại pháp trận tinh tú triệu hồi 5 đợt mưa thiên thạch ma pháp gây <color=#B84DFF><b>{meteorDmg} AP diện rộng</b></color> hủy diệt toàn bản đồ. <color=#FFDF66><b>[Khi Overdrive]:</b></color> Hồi chiêu rút ngắn chỉ còn <color=#55FF88><b>1.0s</b></color> để xả bão thiên thạch liên hoàn!"
                     : $"Opens celestial circles summoning 5 waves of devastating meteor strikes inflicting catastrophic AoE <color=#B84DFF><b>{meteorDmg} AP</b></color>. <color=#FFDF66><b>[During Overdrive]:</b></color> CD drops to <color=#55FF88><b>1.0s</b></color> for continuous meteor storms!"
            };
        }
        else if (isDune)
        {
            skillTags = new string[] {
                isVi ? "NỘI TẠI" : "PASSIVE",
                isVi ? "GỒNG / ATK" : "CHARGE / ATK",
                isVi ? "ẢO ẢNH" : "MIRAGE",
                isVi ? "TUYỆT KỸ" : "ULTIMATE"
            };
            skillNames = new string[] {
                isVi ? "NỘI TẠI: HUYẾT SA TẨY LỄ" : "PASSIVE: BLOOD SAND RITUAL",
                isVi ? "1. SONG ĐAO (GỒNG THỦ)" : "1. DUAL DAGGERS (PARRY)",
                isVi ? "2. ẢO ẢNH SA LƯỚT" : "2. SAND MIRAGE DASH",
                isVi ? "3. CUỒNG BÃO SA ẢNH X-REND" : "3. SANDSTORM X-REND"
            };
            skillCosts = new string[] {
                "<color=#D9A6FF><b>[BỊ ĐỘNG]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#FF4757><b>[4% HP]</b></color>",
                "<color=#FF4757><b>[20% HP]</b></color>"
            };
            int duneUlti = (14 * 6) + (totalAtk * 3);
            skillDescs = new string[] {
                isVi ? "Dùng 2 chiêu bất kỳ hồi ngay <color=#22C55E><b>10% HP đã mất</b></color>. Đánh thường tích Nộ; khi <color=#FF3B30><b>Đầy 100% Nộ</b></color> cường hóa lốc xoáy thành <color=#FF3B30><b>Cơn Lốc Đỏ</b></color> uy lực cực đại."
                     : "Every 2 skill casts restores <color=#22C55E><b>10% Missing HP</b></color>. Attacks build Rage; at <color=#FF3B30><b>100% Rage</b></color> empowers vortex into a devastating <color=#FF3B30><b>Crimson Vortex</b></color>.",
                isVi ? $"Đòn 1-2 chém bạo kích ({totalAtk} ATK). Đòn 3 phóng <color=#FFDF66><b>Lốc Sa Mạc</b></color> cuốn quái (tiêu hao 8% HP, CD nội tại 3.5s). <color=#FFDF66><b>[Gồng Thủ / Đầy Nộ]:</b></color> Giảm 70% ST, phóng <color=#FF3B30><b>Lốc Đỏ</b></color> (12% HP) gây <color=#E2E8F0><b>TD</b></color>, <color=#FFAA33><b>Choáng 0.75s</b></color> & <color=#FFAA33><b>Ngắt chiêu Boss</b></color>!"
                     : $"Combo slashes ({totalAtk} ATK). Hit 3 unleashes <color=#FFDF66><b>Sand Vortex</b></color> (8% HP, 3.5s CD). <color=#FFDF66><b>[Hold Parry / Full Rage]:</b></color> 70% DR, unleashes <color=#FF3B30><b>Crimson Vortex</b></color> (12% HP) dealing <color=#E2E8F0><b>TD</b></color>, <color=#FFAA33><b>0.75s Stun</b></color> & <color=#FFAA33><b>Interrupts Bosses</b></color>!",
                isVi ? "Tích lũy tối đa <color=#55FFFF><b>3 lần lướt liên hoàn</b></color> (tiêu hao 4% HP), tạo tàn ảnh cát & <color=#FFAA33><b>Giảm 1.5s hồi Tuyệt Kỹ</b></color> mỗi lần lướt."
                     : "Stores up to <color=#55FFFF><b>3 Consecutive Dash Charges</b></color> (consumes 4% HP), leaves mirages & <color=#FFAA33><b>Reduces Ulti CD 1.5s</b></color> per dash.",
                isVi ? $"Hút quái vào bão cát & trảm kích chữ X gây <color=#E2E8F0><b>{duneUlti} TD cực lớn (bỏ qua giáp)</b></color>, <color=#55FF88><b>Hút máu 50% sát thương gây ra</b></color> & gây Chảy Máu Sa Mạc liên tục."
                     : $"Pulls enemies & Cross-X Slash dealing <color=#E2E8F0><b>{duneUlti} TD (ignores armor)</b></color>, <color=#55FF88><b>50% Lifesteal from damage dealt</b></color> & continuous Sand Bleed."
            };
        }
        else if (isTitan)
        {
            skillTags = new string[] {
                isVi ? "NỘI TẠI" : "PASSIVE",
                isVi ? "GỒNG / TANK" : "CHARGE / TANK",
                isVi ? "CHIẾN XA" : "CHARGE",
                isVi ? "TUYỆT KỸ" : "ULTIMATE"
            };
            skillNames = new string[] {
                isVi ? "NỘI TẠI: DA DUNG NHAM CHIẾN THẦN" : "PASSIVE: VOLCANIC CARAPACE",
                isVi ? "1. ĐẠI CHÙY (GỒNG ĐỊA CHẤN)" : "1. WAR HAMMER (EARTHSHAKER)",
                isVi ? "2. CHIẾN XA XOAY BÚA 360°" : "2. JUGGERNAUT 360° SPIN",
                isVi ? "3. CUỒNG NỘ CHẤN ĐỊA DUNG NHAM" : "3. VOLCANIC CATACLYSM SLAM"
            };
            skillCosts = new string[] {
                "<color=#D9A6FF><b>[BỊ ĐỘNG]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#00E5FF><b>[45 Mana]</b></color>"
            };
            int titanUlti = 95 + (totalAtk * 2);
            skillDescs = new string[] {
                isVi ? "Thân thể bọc giáp dung nham vĩnh cửu: Giảm cố định <color=#55FF88><b>20% sát thương nhận vào</b></color> & nhận thêm <color=#FFAA33><b>+30% Kháng Khống Chế</b></color>."
                     : "Molten lava armor: Permanently gains <color=#55FF88><b>20% Damage Reduction</b></color> & <color=#FFAA33><b>+30% CC Resistance</b></color>.",
                isVi ? $"Nện đại chùy càn quét gây <color=#FF3B30><b>{totalAtk} ATK</b></color>. <color=#FFDF66><b>[Nhấn Giữ Gồng Địa Chấn]:</b></color> <color=#55FF88><b>Bất tử 100%</b></color>, thả tay nện đất bộc phá gây <color=#E2E8F0><b>{Mathf.RoundToInt(totalAtk * 1.5f)} TD</b></color> + <color=#FFAA33><b>Choáng 1.0s</b></color>! (Hồi chiêu gồng: 5s - 10s)."
                     : $"Massive hammer smashes inflicting <color=#FF3B30><b>{totalAtk} ATK</b></color>. <color=#FFDF66><b>[Hold Earthshaker]:</b></color> <color=#55FF88><b>100% Invulnerable</b></color>, release to smash ground inflicting <color=#E2E8F0><b>{Mathf.RoundToInt(totalAtk * 1.5f)} TD</b></color> + <color=#FFAA33><b>Stun 1.0s</b></color>! (Charge CD: 5s - 10s).",
                isVi ? $"Lướt tốc độ cao xoay búa 360° càn quét gây <color=#FF3B30><b>{totalAtk} ATK</b></color> & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>."
                     : $"Heavy spin dash slashing all enemies along path with <color=#FF3B30><b>{totalAtk} ATK</b></color> & <color=#FFAA33><b>Reduces Ultimate CD by 2.0s</b></color>.",
                isVi ? $"Hút toàn bộ quái vật vào tâm chấn (5.5m), bật nhảy nện búa dung nham bộc phát <color=#FF3B30><b>{titanUlti} ATK cực đại</b></color> hất tung diện rộng & <color=#55FF88><b>Nhận Máu Ảo bằng 50% HP hiện tại trong 15s</b></color>!"
                     : $"Pulls all enemies into epicenter (5.5m), leaps to smash volcanic hammer for <color=#FF3B30><b>{titanUlti} ATK</b></color> with AoE knockdown & <color=#55FF88><b>Gains Barrier equal to 50% Current HP for 15s</b></color>!"
            };
        }
        else if (isAzure)
        {
            skillTags = new string[] {
                isVi ? "NỘI TẠI" : "PASSIVE",
                "AP",
                isVi ? "LƯỚT" : "DASH",
                isVi ? "TUYỆT KỸ" : "ULTIMATE"
            };
            skillNames = new string[] {
                isVi ? "NỘI TẠI: HÀN TÂM & KIẾM KHÍ QUY HÓA" : "PASSIVE: FROST SOUL & FLOWING RESONANCE",
                isVi ? "1. TRỌNG KIẾM & SÓNG BĂNG KIẾM KHÍ" : "1. FROST SWORD WAVES",
                isVi ? "2. HÀN BĂNG TRƯỢT TUYẾT" : "2. FROST GLIDE DASH",
                isVi ? "3. BÃO SÓNG KIẾM NAN QUẠT" : "3. CRESCENT TEMPEST"
            };
            skillCosts = new string[] {
                "<color=#D9A6FF><b>BỊ ĐỘNG</b></color>",
                "<color=#10B981><b>0 Mana</b></color>",
                "<color=#10B981><b>0 Mana</b></color>",
                "<color=#00E5FF><b>50 Mana</b></color>"
            };
            int azureUlti = (48 + totalAtk) * 5;
            skillDescs = new string[] {
                isVi ? "Đòn đánh trúng đích hồi <color=#38BDF8><b>+6 Mana</b></color> & giảm 50% thời gian chờ đòn kế tiếp. <b>Kiếm Khí Quy Hóa:</b> Toàn bộ <b>Tốc Đánh & Tốc Độ cộng thêm</b> tự động chuyển hóa thành <b>Sát Thương Phép (AP)</b> cho Sóng Kiếm!"
                     : "Basic attacks restore <color=#38BDF8><b>+6 Mana</b></color> & cut next attack CD by 50%. <b>Flowing Resonance:</b> All <b>bonus Attack Speed & Speed</b> automatically converts into powerful <b>Magic Damage (AP)</b> for Sword Waves!",
                isVi ? $"Chém phóng <color=#55FFFF><b>Sóng Kiếm Băng</b></color> gây <color=#B84DFF><b>{totalAtk} AP</b></color> xuyên thấu (tầm 6.5m). Đòn đánh găm <color=#FF2961><b>Vết Thương Sâu (Trái Tim Nứt)</b></color> trong 5s: <color=#FF4D79><b>giảm 50% hồi máu & hút máu</b></color> của địch và tăng <color=#55FFFF><b>+20% sát thương băng</b></color> nhận vào! <color=#FFDF66><b>[Nhấn Giữ Tụ Lực (1s)]:</b></color> Tăng sát thương sóng kiếm tới <color=#FFAA33><b>x2.25 lần (225% AP)</b></color>!"
                     : $"Heavy slashes unleashing <color=#55FFFF><b>Frost Sword Waves</b></color> piercing foes with <color=#B84DFF><b>{totalAtk} AP</b></color> (6.5m). Hits inflict <color=#FF2961><b>Grievous Wounds (Cracked Heart)</b></color> for 5s: <color=#FF4D79><b>reduces enemy healing & lifesteal by 50%</b></color> and increases Frost damage taken by <color=#55FFFF><b>+20%</b></color>! <color=#FFDF66><b>[Hold to Charge (1s)]:</b></color> Amplifies wave damage up to <color=#FFAA33><b>x2.25 (225% AP)</b></color>!",
                isVi ? "Lướt trượt nhanh trên dải băng tuyết & <color=#55FFFF><b>Hồi ngay lập tức đòn đánh thường</b></color> kế tiếp."
                     : "Swift glide dash over frost tracks & <color=#55FFFF><b>Instantly resets basic attack cooldown</b></color>.",
                isVi ? $"Tụ nội lực phóng 5 đợt kiếm khí băng lam nan quạt khổng lồ gây <color=#B84DFF><b>{azureUlti} AP diện rộng</b></color> quét sạch toàn bộ chiến trường."
                     : $"Releases 5 massive crescent frost sword waves with devastating AoE <color=#B84DFF><b>{azureUlti} AP</b></color> across the battlefield."
            };
        }
        else if (isRonin)
        {
            skillTags = new string[] {
                isVi ? "NỘI TẠI" : "PASSIVE",
                isVi ? "GỒNG / IAIDO" : "CHARGE / IAIDO",
                isVi ? "TỐC BIẾN" : "WARP",
                isVi ? "TUYỆT KỸ" : "ULTIMATE"
            };
            skillNames = new string[] {
                isVi ? "NỘI TẠI: PHONG LÔI PHONG ẤN" : "PASSIVE: WIND SEAL RESONANCE",
                isVi ? "1. PHONG TRẢM (GỒNG TỤ LỰC)" : "1. GALE SLASH & IAIDO",
                isVi ? "2. TÀN ẢNH TỐC BIẾN (MURAD STYLE)" : "2. SHADOW PHANTOM WARP",
                isVi ? "3. ÁM ẢNH PHÂN THÂN TRẢM" : "3. PHANTOM FLASH CLEAVE"
            };
            skillCosts = new string[] {
                "<color=#D9A6FF><b>[BỊ ĐỘNG]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#00E5FF><b>[50 Mana]</b></color>"
            };
            int roninUlti = (28 * 3) + (totalAtk * 2);
            skillDescs = new string[] {
                isVi ? "Đòn đánh quét kiếm tích lũy Phong Ấn (3 tầng bộc phát nổ lốc xoáy hút quái gây <color=#FFAA33><b>240% Crit DMG</b></color>). Tiêu diệt quái hồi ngay <color=#38BDF8><b>+10 Mana</b></color>."
                     : "Cleaves build Wind Marks (3 stacks bursts vacuum vortex for <color=#FFAA33><b>240% Crit DMG</b></color>). Slaying foes restores <color=#38BDF8><b>+10 Mana</b></color>.",
                isVi ? $"Chém 180° gây <color=#FF3B30><b>{totalAtk} ATK</b></color>. <color=#FFDF66><b>[ĐIỀU KIỆN: ĐẦY NỘI NĂNG / KHIÊN HỘ THÂN]:</b></color> Nhấn giữ gồng Iaido phóng Nguyệt Kiếm Khí xuyên thấu <color=#FFAA33><b>x2.85 Crit DMG</b></color>! Max Charge trúng quái giúp <color=#FFAA33><b>Giảm 35% CD Tuyệt Kỹ</b></color> & hồi ngay <color=#38BDF8><b>+25 Mana</b></color>!"
                     : $"180° slashes dealing <color=#FF3B30><b>{totalAtk} ATK</b></color>. <color=#FFDF66><b>[REQ: FULL ENERGY / SHIELD ACTIVE]:</b></color> Hold to charge Iaido unleashing Crescent Wave for <color=#FFAA33><b>x2.85 Crit DMG</b></color>! Max Charge hit cuts <color=#FFAA33><b>Ulti CD by 35%</b></color> & restores <color=#38BDF8><b>+25 Mana</b></color>!",
                isVi ? "Nhịp 1 tốc biến 4.8m tới trước (<color=#55FF88><b>Bất tử 0.25s</b></color>) cắm bóng 4.5s. Nhịp 2 Teleport quay về bóng (<color=#55FF88><b>Bất tử 0.30s</b></color>) & <color=#FFAA33><b>Giảm 2.0s hồi Tuyệt Kỹ</b></color>!"
                     : "Stage 1 warps 4.8m forward leaving clone for 4.5s (<color=#55FF88><b>Invincible 0.25s</b></color>). Stage 2 Warps back to clone (<color=#55FF88><b>Invincible 0.30s</b></color>) & <color=#FFAA33><b>Cuts Ulti CD by 2.0s</b></color>!",
                isVi ? $"Lướt ảo ảnh chớp nhoáng trảm sát liên hoàn mọi kẻ địch trong không gian gây <color=#E2E8F0><b>{roninUlti} TD (bỏ qua giáp)</b></color>, <color=#55FF88><b>Bất tử hoàn toàn</b></color>. <color=#FFDF66><b>Kết liễu kẻ địch hồi ngay lập tức Tuyệt Kỹ!</b></color>"
                     : $"Executes blinding phantom dash slashes dealing devastating <color=#E2E8F0><b>{roninUlti} TD (ignores defense)</b></color> with <color=#55FF88><b>Complete Invincibility</b></color>. <color=#FFDF66><b>Executing enemies instantly resets Ultimate CD!</b></color>"
            };
        }
        else
        {
            skillTags = new string[] {
                isVi ? "NỘI TẠI" : "PASSIVE",
                isVi ? "GỒNG / PARRY" : "CHARGE / PARRY",
                isVi ? "THÁNH THUẪN" : "SHIELD",
                isVi ? "TUYỆT KỸ" : "ULTIMATE"
            };
            skillNames = new string[] {
                isVi ? "NỘI TẠI: THÁNH QUANG BẢO HỘ" : "PASSIVE: RADIANT AEGIS",
                isVi ? "1. ĐẠI KIẾM (GỒNG THẾ THỦ)" : "1. ANCIENT SWORD (PARRY)",
                isVi ? "2. THÁNH THUẪN PHẢN ĐÒN" : "2. RADIANT RIPOSTE DASH",
                isVi ? "3. THÁNH KIẾM THIÊN NHAM" : "3. HOLY SWORD TEMPEST"
            };
            skillCosts = new string[] {
                "<color=#D9A6FF><b>[BỊ ĐỘNG]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#10B981><b>[0 Mana]</b></color>",
                "<color=#00E5FF><b>[50 Mana]</b></color>"
            };
            int vaelUlti = (20 + (int)(totalAtk * 0.4f)) * 2 + (25 + (int)(totalAtk * 0.5f)) + (50 + totalAtk);
            skillDescs = new string[] {
                isVi ? "Đòn đánh thường trúng đích hồi ngay <color=#38BDF8><b>+8 Mana</b></color>. Khi HP dưới 35%, kích hoạt Thánh Giáp Thiên Thần hồi ngay <color=#55FF88><b>25% HP</b></color>."
                     : "Basic hits restore <color=#38BDF8><b>+8 Mana</b></color>. When HP drops below 35%, activates Divine Aegis healing <color=#55FF88><b>25% HP</b></color>.",
                isVi ? $"Chém kiếm 3 nhịp gây <color=#FF3B30><b>{totalAtk} ATK</b></color> (hồi <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66><b>[Nhấn Giữ Gồng Thế Thủ]:</b></color> Giảm 20% sát thương, thả tay bộc phá Thần Kiếm Phản Kích gây <color=#FF3B30><b>x3.2 Crit ATK</b></color> phóng luồng thánh quang xuyên phá!"
                     : $"3-hit combo dealing <color=#FF3B30><b>{totalAtk} ATK</b></color> (restores <color=#38BDF8><b>+8 Mana</b></color>). <color=#FFDF66><b>[Hold Parry]:</b></color> 20% DR, release to unleash Divine Riposte dealing <color=#FF3B30><b>x3.2 Crit ATK</b></color> firing holy shockwave pillars!",
                isVi ? $"Lướt né đòn (<color=#55FF88><b>Bất tử khi lướt</b></color>). Lướt qua quái/đỡ đòn sẽ <color=#FFAA33><b>Hồi 50% CD Lướt</b></color>, hồi <color=#38BDF8><b>+15 Mana</b></color> & phóng 3 Thánh Kiếm Khí gây <color=#FF3B30><b>{totalAtk} ATK</b></color>!"
                     : $"Holy aegis dash (<color=#55FF88><b>Invulnerable</b></color>). Deflecting attacks <color=#FFAA33><b>Refunds 50% Dash CD</b></color>, restores <color=#38BDF8><b>+15 Mana</b></color> & fires 3 Holy Waves with <color=#FF3B30><b>{totalAtk} ATK</b></color>!",
                isVi ? $"Xoay bão lốc kiếm 360° hút quái vật và triệu hồi Cự Kiếm Thiên Nham giáng thế gây <color=#FF3B30><b>{vaelUlti} ATK cực đại</b></color> diện rộng."
                     : $"360° sword vortex storm pulling enemies and summons a gigantic Holy Sword slamming down with massive AoE <color=#FF3B30><b>{vaelUlti} ATK</b></color>."
            };
        }

        float[] baseCds = GetHeroBaseCooldowns(heroId);

        for (int i = 0; i < 4; i++)
        {
            int skillIdx = i;
            GameObject card = new GameObject($"SkillCard_{skillIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.layer = 5;
            card.transform.SetParent(skillsContainer, false);
            skillCardObjs.Add(card);

            RectTransform cRt = card.GetComponent<RectTransform>();
            float rowTop = 1f - i * 0.25f;
            float rowBot = 1f - (i + 1) * 0.25f;
            cRt.anchorMin = new Vector2(0f, rowBot);
            cRt.anchorMax = new Vector2(1f, rowTop);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.offsetMin = new Vector2(0f, 3.5f);
            cRt.offsetMax = new Vector2(0f, -3.5f);

            Image cImg = card.GetComponent<Image>();
            cImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);

            Color outlineCol = (skillIdx == 0) ? new Color(0.85f, 0.65f, 1f, 0.85f)
                             : (skillIdx == 1) ? new Color(1f, 0.85f, 0.35f, 0.80f)
                             : (skillIdx == 2) ? new Color(0.20f, 0.85f, 0.55f, 0.80f)
                             : new Color(0.95f, 0.35f, 0.35f, 0.85f);
            WhiteIconLoader.ApplyGothicFrame(card, outlineCol, 0.45f);

            // Icon Badge on Left (54x54px) with Gothic frame
            GameObject iconBadge = new GameObject("IconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconBadge.layer = 5;
            iconBadge.transform.SetParent(card.transform, false);
            RectTransform ibRt = iconBadge.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(12f, 0f);
            ibRt.sizeDelta = new Vector2(54f, 54f);
            Image ibImg = iconBadge.GetComponent<Image>();
            ibImg.color = new Color(0.06f, 0.09f, 0.15f, 1f);
            WhiteIconLoader.ApplyGothicFrame(iconBadge, outlineCol, 0.35f);

            Sprite skillSpr = LoadHeroSkillSprite(heroId, skillIdx);
            if (skillSpr != null)
            {
                GameObject iconImg = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconImg.layer = 5;
                iconImg.transform.SetParent(iconBadge.transform, false);
                RectTransform iiRt = iconImg.GetComponent<RectTransform>();
                iiRt.anchorMin = Vector2.zero;
                iiRt.anchorMax = Vector2.one;
                iiRt.sizeDelta = new Vector2(-10f, -10f);
                Image ii = iconImg.GetComponent<Image>();
                ii.sprite = skillSpr;
                ii.color = (skillIdx == 0) ? new Color(0.92f, 0.78f, 1f) : Color.white;
                ii.preserveAspect = true;
                ii.raycastTarget = false;
            }

            Color tagCol = outlineCol;
            string sTagUpper = skillTags[skillIdx].ToUpper();
            if (skillIdx == 0)
                tagCol = new Color(0.85f, 0.65f, 1f); // Tím neon
            else if (sTagUpper.Contains("AP"))
                tagCol = new Color(0.72f, 0.35f, 1f); // Tím
            else if (sTagUpper.Contains("TD"))
                tagCol = new Color(0.92f, 0.95f, 1f); // Trắng Bạc
            else if (sTagUpper.Contains("ATK"))
                tagCol = new Color(1f, 0.23f, 0.23f); // Đỏ
            else if (sTagUpper.Contains("LƯỚT") || sTagUpper.Contains("DASH") || sTagUpper.Contains("ẢO ẢNH") || sTagUpper.Contains("TỐC BIẾN"))
                tagCol = new Color(0.20f, 0.85f, 0.55f);

            // Tính toán Cooldown động chính xác cập nhật theo ngọc khảm, nâng cấp và lõi
            float effCd = 0f;
            if (skillIdx > 0 && (skillIdx - 1) < baseCds.Length)
            {
                float baseCd = baseCds[skillIdx - 1];
                if (baseCd > 0.05f)
                {
                    float clampedCdr = Mathf.Clamp(finalCd, 0f, 0.60f);
                    if (skillIdx == 1) effCd = Mathf.Max(0.5f, baseCd * (1f - clampedCdr));
                    else if (skillIdx == 2) effCd = Mathf.Max(1.0f, (baseCd * (1f - clampedCdr)) - upDashCd);
                    else effCd = Mathf.Max(3.0f, (baseCd * (1f - clampedCdr)) - upUltiCd);
                }
            }

            string cleanTag = skillTags[skillIdx].Trim('[', ']', ' ');
            float textLeft = 78f;

            // Nhóm Header góc phải: Tag + Cooldown hợp nhất thành 1 Text (chống đè chồng, chống tràn viền)
            GameObject topGroup = new GameObject("TopRightInfo", typeof(RectTransform), typeof(CanvasRenderer));
            topGroup.layer = 5;
            topGroup.transform.SetParent(card.transform, false);
            RectTransform trRt = topGroup.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0.55f, 1f);
            trRt.anchorMax = new Vector2(1f, 1f);
            trRt.pivot = new Vector2(1f, 1f);
            trRt.anchoredPosition = new Vector2(-16f, -9f);
            trRt.sizeDelta = new Vector2(0f, 22f);

            var infoTmp = topGroup.AddComponent<TextMeshProUGUI>();
            infoTmp.fontSize = 12.5f;
            infoTmp.fontStyle = FontStyles.Bold;
            infoTmp.alignment = TextAlignmentOptions.MidlineRight;
            infoTmp.enableWordWrapping = false;
            infoTmp.raycastTarget = false;

            string tagHex = ColorUtility.ToHtmlStringRGB(tagCol);
            if (skillIdx == 0)
            {
                infoTmp.text = $"<color=#{tagHex}><b>[{cleanTag}]</b></color>  <color=#E2E8F0><b>VÔ HẠN</b></color>";
            }
            else
            {
                string cdHex = (effCd > 0f) ? "#38BDF8" : "#94A3B8";
                string cdStr = (effCd > 0f) ? $"CD: {effCd:0.#}s" : "CD: 0s";
                infoTmp.text = $"<color=#{tagHex}><b>[{cleanTag}]</b></color>  <color={cdHex}><b>{cdStr}</b></color>";
            }

            // Title (Bắt đầu sau IconBadge, dừng trước 55% bề ngang card, không bao giờ đè lên TopRightInfo)
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            titleObj.layer = 5;
            titleObj.transform.SetParent(card.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(0.55f, 1f);
            tRt.pivot = new Vector2(0f, 1f);
            tRt.anchoredPosition = new Vector2(textLeft, -9f);
            tRt.sizeDelta = new Vector2(-textLeft, 22f);

            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = $"<b>{skillNames[skillIdx]}</b>  <size=85%>{skillCosts[skillIdx]}</size>";
            tTmp.fontSize = 13.5f;
            tTmp.enableAutoSizing = true;
            tTmp.fontSizeMin = 10.5f;
            tTmp.fontSizeMax = 13.5f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.alignment = TextAlignmentOptions.Left;
            tTmp.color = Color.white;
            tTmp.overflowMode = TextOverflowModes.Ellipsis;
            tTmp.enableWordWrapping = false;

            // Description: Đặt bên dưới Title với khoảng cách thông thoáng
            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer));
            descObj.layer = 5;
            descObj.transform.SetParent(card.transform, false);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.offsetMin = new Vector2(textLeft, 6f);
            dRt.offsetMax = new Vector2(-16f, -33f);

            var dTmp = descObj.AddComponent<TextMeshProUGUI>();
            dTmp.text = skillDescs[skillIdx];
            dTmp.fontSize = 11.5f;
            dTmp.lineSpacing = 1.2f;
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
            bagSectionHeaderText.text = "";
        }

        var invItems = AccountDatabase.GetActiveUserInventory();
        if (bagContentContainer != null)
        {
            var grid = bagContentContainer.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = new Vector2(104f, 102f);
                grid.spacing = new Vector2(12f, 10f);
                grid.padding = new RectOffset(14, 14, 8, 8);
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 16;
            }
        }

        List<AccountDatabase.InventoryItemEntry> ownedGems = new List<AccountDatabase.InventoryItemEntry>();
        foreach (var it in invItems)
        {
            if (it.quantity > 0 && EquipmentSocketManager.IsGem(it.itemId))
            {
                ownedGems.Add(it);
            }
        }

        int spawnedCardCount = 0;
        for (int i = 0; i < ownedGems.Count; i++)
        {
            var gemEntry = ownedGems[i];
            var gemDef = InventoryItemDatabase.GetItem(gemEntry.itemId);
            var gemEff = EquipmentSocketManager.GetGemEffect(gemEntry.itemId);
            if (gemDef == null || !gemEff.HasValue) continue;

            spawnedCardCount++;
            GameObject cardObj = new GameObject($"BagGemCard_{gemEntry.itemId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardObj.layer = 5;
            cardObj.transform.SetParent(bagContentContainer, false);
            bagItemCardObjs.Add(cardObj);

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(104f, 102f);

            Image cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.06f, 0.09f, 0.15f, 0.98f);
            cImg.raycastTarget = true;
            Color frameGemCol = Color.Lerp(gemEff.Value.gemColor, Color.white, 0.15f);
            WhiteIconLoader.ApplyGothicFrame(cardObj, frameGemCol, 0.65f);

            string gemIdToSocket = gemEntry.itemId;
            Action openPopupAction = () =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                EquipmentItemDetailPopupUI.EnsureUIExists(transform);
                EquipmentItemDetailPopupUI.Show(-1, gemIdToSocket, 1, curHero, () =>
                {
                    NotifyPlayerCombatStatsChanged();
                    RefreshModal();
                }, transform);
            };

            Button cardBtn = cardObj.GetComponent<Button>();
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.targetGraphic = cImg;
            cardBtn.onClick.AddListener(() => openPopupAction());

            // Drag & Drop Component
            var dragItem = cardObj.AddComponent<UIDragDropItem>();
            dragItem.itemId = gemEntry.itemId;
            dragItem.itemIcon = gemDef.Icon;
            dragItem.itemColor = gemEff.Value.gemColor;
            dragItem.parentScrollRect = bagContentContainer.GetComponentInParent<ScrollRect>();
            dragItem.onClick = openPopupAction;

            // Subtle Gem Background Radial Glow / Tint
            GameObject glowObj = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowObj.layer = 5;
            glowObj.transform.SetParent(cardObj.transform, false);
            RectTransform glowRt = glowObj.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.offsetMin = new Vector2(8f, 8f);
            glowRt.offsetMax = new Vector2(-8f, -8f);
            var glowImg = glowObj.GetComponent<Image>();
            glowImg.color = new Color(gemEff.Value.gemColor.r, gemEff.Value.gemColor.g, gemEff.Value.gemColor.b, 0.14f);
            glowImg.raycastTarget = false;

            // Center Item Icon
            GameObject ibObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ibObj.layer = 5;
            ibObj.transform.SetParent(cardObj.transform, false);
            RectTransform ibRt = ibObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0.5f, 0.5f);
            ibRt.anchorMax = new Vector2(0.5f, 0.5f);
            ibRt.pivot = new Vector2(0.5f, 0.5f);
            ibRt.anchoredPosition = new Vector2(0f, 2f);
            ibRt.sizeDelta = new Vector2(66f, 66f);
            Image ibImg = ibObj.GetComponent<Image>();
            ibImg.sprite = gemDef.Icon;
            ibImg.preserveAspect = true;
            ibImg.raycastTarget = false;

            // Quantity Stack Badge in Bottom-Right Corner: High-contrast Dark Pill with Gold Border & Bright Neon Text
            if (gemEntry.quantity > 0)
            {
                GameObject qObj = new GameObject("QtyBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                qObj.layer = 5;
                qObj.transform.SetParent(cardObj.transform, false);
                RectTransform qRt = qObj.GetComponent<RectTransform>();
                qRt.anchorMin = new Vector2(1f, 0f);
                qRt.anchorMax = new Vector2(1f, 0f);
                qRt.pivot = new Vector2(1f, 0f);
                qRt.anchoredPosition = new Vector2(-4f, 4f);
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
                string qtyStr = gemEntry.quantity > 99 ? "99+" : $"x{gemEntry.quantity}";
                qTmp.text = $"<b>{qtyStr}</b>";
                qTmp.fontSize = 11.5f;
                qTmp.fontStyle = FontStyles.Bold;
                qTmp.color = new Color(0.35f, 1f, 0.60f, 1f); // Neon bright emerald
                qTmp.alignment = TextAlignmentOptions.Center;
                qTmp.enableWordWrapping = false;
                qTmp.raycastTarget = false;
            }
        }

        // Pad with styled empty bag slots so the inventory always looks full & structured (exactly 48 slots = 3 rows x 16 cols)
        int minBagSlots = 48;
        int targetSlotCount = Mathf.Max(minBagSlots, ((spawnedCardCount + 15) / 16) * 16);

        // If slots fit within 3 rows (48 slots), disable vertical scrolling so it never intercepts drags
        var parentScroll = bagContentContainer.GetComponentInParent<ScrollRect>();
        if (parentScroll != null)
        {
            parentScroll.vertical = (targetSlotCount > 48);
        }

        for (int e = spawnedCardCount; e < targetSlotCount; e++)
        {
            GameObject emptySlot = new GameObject($"EmptyBagSlot_{e}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            emptySlot.layer = 5;
            emptySlot.transform.SetParent(bagContentContainer, false);
            bagItemCardObjs.Add(emptySlot);

            RectTransform esRt = emptySlot.GetComponent<RectTransform>();
            esRt.sizeDelta = new Vector2(104f, 102f);

            Image esImg = emptySlot.GetComponent<Image>();
            esImg.color = new Color(0.04f, 0.07f, 0.12f, 0.65f);
            esImg.raycastTarget = false;

            var esOutl = emptySlot.AddComponent<Outline>();
            esOutl.effectColor = new Color(0.28f, 0.38f, 0.52f, 0.25f);
            esOutl.effectDistance = new Vector2(1f, -1f);

            GameObject runeObj = new GameObject("Rune", typeof(RectTransform), typeof(CanvasRenderer));
            runeObj.layer = 5;
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

    public static InGameInventoryUI EnsureUIExists(Canvas targetCanvas = null)
    {
        Canvas rootCanvas = targetCanvas != null ? targetCanvas : GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) return null;

        if (!EnableInGameInventory)
        {
            Transform exBtn = rootCanvas.transform.Find("Btn_InGameBag");
            if (exBtn != null) exBtn.gameObject.SetActive(false);
            Transform exModal = rootCanvas.transform.Find("InGameInventoryModal");
            if (exModal != null) exModal.gameObject.SetActive(false);
            if (Instance != null && Instance.gameObject != null) Instance.gameObject.SetActive(false);
            return null;
        }

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
            btnImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;

            var outline = btnObj.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                var ui = Instance != null ? Instance : EnsureUIExists();
                if (ui != null) ui.Toggle();
            });

            WhiteIconLoader.ReplaceTextWithIcon(btn, "Bag", new Color(1f, 0.88f, 0.55f), new Vector2(28f, 28f));
        }

        // 2. InGameBagPanel - Destroy any stale or cached panel to ensure latest 48 slots layout & listeners
        Transform existingPanel = rootCanvas.transform.Find("InGameInventoryModal");
        if (existingPanel != null)
        {
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
        bgImg.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
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
        dImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        dImg.type = Image.Type.Sliced;
        dImg.color = Color.white;
        WhiteIconLoader.ApplyGothicFrame(dialogBox, Color.white, 0.40f);

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
        uiComp.titleText.color = new Color(0.98f, 0.88f, 0.55f, 1f);

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
        uiComp.subtitleText.color = new Color(0.80f, 0.85f, 0.95f, 0.85f);
        SoulslikeHUDStyler.StyleHeader(uiComp.titleText, uiComp.subtitleText);
        SoulslikeHUDStyler.AddFiligreeDivider(dialogBox.transform, new Vector2(0f, -72f), new Vector2(480f, 12f));

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

        Button cBtn = closeBtnObj.GetComponent<Button>();
        cBtn.onClick.AddListener(uiComp.Close);
        SoulslikeHUDStyler.StyleCloseButton(cBtn);

        // TOP SECTION: HERO STAGE (Avatar + 6 Sockets) + STATS + 3 SKILL CARDS
        GameObject topSection = new GameObject("TopSection_HeroSockets", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topSection.layer = 5;
        topSection.transform.SetParent(dialogBox.transform, false);
        RectTransform tsRt = topSection.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0f, 0.38f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.pivot = new Vector2(0.5f, 0.5f);
        tsRt.offsetMin = new Vector2(16f, 4f);
        tsRt.offsetMax = new Vector2(-16f, -76f);
        Image tsImg = topSection.GetComponent<Image>();
        tsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);
        tsImg.raycastTarget = false;
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
        stgImg.raycastTarget = false;
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
        uiComp.heroAvatarPreviewImage.raycastTarget = false;

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
                string curHero = EquipmentSocketManager.GetActiveHeroId();
                if (EquipmentSocketManager.SocketGemToHeroSlot(targetSlot, droppedGemId, out _, out _, curHero))
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
        skcRt.offsetMin = new Vector2(8f, 12f);
        skcRt.offsetMax = new Vector2(-12f, -12f);
        uiComp.skillsContainer = skillsBox.transform;

        // BOTTOM SECTION: Bag Grid
        GameObject botSection = new GameObject("BottomSection_Bag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        botSection.layer = 5;
        botSection.transform.SetParent(dialogBox.transform, false);
        RectTransform bsRt = botSection.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0f, 0f);
        bsRt.anchorMax = new Vector2(1f, 0.38f);
        bsRt.pivot = new Vector2(0.5f, 0.5f);
        bsRt.offsetMin = new Vector2(16f, 32f);
        bsRt.offsetMax = new Vector2(-16f, -4f);
        Image bsImg = botSection.GetComponent<Image>();
        bsImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);
        bsImg.raycastTarget = false;
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
        bhRt.sizeDelta = Vector2.zero;
        uiComp.bagSectionHeaderText = bhObj.AddComponent<TextMeshProUGUI>();
        uiComp.bagSectionHeaderText.text = "";

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

        uiComp.autoMergeAllBtn = autoMergeObj.GetComponent<Button>();
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
        uiComp.autoMergeAllBtnText.color = new Color(0.96f, 0.88f, 0.65f, 1f);
        uiComp.autoMergeAllBtnText.alignment = TextAlignmentOptions.Center;
        uiComp.autoMergeAllBtnText.enableWordWrapping = false;
        SoulslikeHUDStyler.StyleButton(uiComp.autoMergeAllBtn);

        // Scroll View
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect));
        scrollObj.layer = 5;
        scrollObj.transform.SetParent(botSection.transform, false);
        RectTransform scrRt = scrollObj.GetComponent<RectTransform>();
        scrRt.anchorMin = Vector2.zero;
        scrRt.anchorMax = Vector2.one;
        scrRt.offsetMin = new Vector2(10f, 4f);
        scrRt.offsetMax = new Vector2(-10f, -34f);
        var scroll = scrollObj.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
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
        vpImg.raycastTarget = false;
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
        grid.cellSize = new Vector2(104f, 102f);
        grid.spacing = new Vector2(12f, 10f);
        grid.padding = new RectOffset(14, 14, 8, 8);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 16;

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

    public int GetSelectedEmptySocketIndex()
    {
        string curHero = EquipmentSocketManager.GetActiveHeroId();
        if (selectedSocketSlotIndex >= 0 && selectedSocketSlotIndex < EquipmentSocketManager.HERO_SOCKET_COUNT)
        {
            if (!EquipmentSocketManager.GetHeroSocketGem(selectedSocketSlotIndex, out _, out _, curHero))
            {
                return selectedSocketSlotIndex;
            }
        }
        return -1;
    }

    public UIDropSocketTarget FindClosestSocketTarget(Vector2 screenPos, float maxDistancePx = 85f)
    {
        if (socketSlotObjs == null || socketSlotObjs.Count == 0) return null;
        Canvas c = GetComponentInParent<Canvas>();
        Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;

        UIDropSocketTarget bestTarget = null;
        float bestDist = maxDistancePx;

        foreach (var sObj in socketSlotObjs)
        {
            if (sObj == null || !sObj.activeInHierarchy) continue;
            RectTransform rt = sObj.GetComponent<RectTransform>();
            if (rt == null) continue;

            Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
            float dist = Vector2.Distance(screenPos, slotScreenPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = sObj.GetComponent<UIDropSocketTarget>();
            }
        }
        return bestTarget;
    }
}
