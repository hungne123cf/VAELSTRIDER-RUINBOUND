using UnityEngine;
using System;

public class PlayerUpgradeManager : MonoBehaviour
{
    public static PlayerUpgradeManager Instance { get; private set; }

    public enum UpgradeType
    {
        Attack,
        Health,
        Agility,
        Ultimate
    }

    public const int MAX_LEVEL = int.MaxValue;

    public event Action OnUpgradesChanged;

    private PlayerInventory inventory;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private PlayerController playerController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

        FindPlayerComponents();
        ApplyAllUpgrades();
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged()
    {
        OnUpgradesChanged?.Invoke();
    }

    private void FindPlayerComponents()
    {
        if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        if (playerCombat == null) playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

    public string GetCurrentHeroKey()
    {
        var cm = CharacterManager.EnsureInstance();
        string heroId = (cm != null) ? cm.GetSelectedCharacterId() : "Vaelstrider";
        if (string.IsNullOrEmpty(heroId)) heroId = "Vaelstrider";
        return heroId;
    }

    public long CurrentShards
    {
        get
        {
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
            return inventory != null ? inventory.GetShards() : AccountDatabase.GetAethelShards();
        }
    }

    public int GetUpgradeLevel(UpgradeType type) => GetLevel(type);
    public int GetUpgradeCost(UpgradeType type) => GetCost(type);
    public string GetUpgradeName(UpgradeType type) => GetTitle(type);
    public string GetCurrentEffectDescription(UpgradeType type) => GetDescription(type, GetLevel(type));

    public int GetLevel(UpgradeType type, string heroId = null)
    {
        string heroKey = !string.IsNullOrEmpty(heroId) ? heroId : GetCurrentHeroKey();
        string key = $"Upgrade_{heroKey}_{type}_Level";

        // Tương thích ngược: Nếu là Vaelstrider mà key mới chưa có, đọc key cũ
        if (!PlayerPrefs.HasKey(key) && (heroKey == CharacterManager.CHAR_VAELSTRIDER || heroKey == "Vaelstrider"))
        {
            return PlayerPrefs.GetInt("Upgrade_" + type.ToString() + "_Level", 0);
        }

        return PlayerPrefs.GetInt(key, 0);
    }

    public int GetCost(UpgradeType type)
    {
        int level = GetLevel(type);
        switch (type)
        {
            case UpgradeType.Attack:
                return 4 + level * 5; // 4, 9, 14, ...
            case UpgradeType.Health:
                return 3 + level * 4; // 3, 7, 11, ...
            case UpgradeType.Agility:
                return 3 + level * 4; // 3, 7, 11, ...
            case UpgradeType.Ultimate:
                return 5 + level * 6; // 5, 11, 17, ...
            default:
                return 5 + level * 5;
        }
    }

    public string GetTitle(UpgradeType type)
    {
        string hero = GetCurrentHeroKey();
        bool isAzure = (hero == CharacterManager.CHAR_AZURE_BLADE);
        bool isRonin = (hero == CharacterManager.CHAR_GALE_RONIN || hero == "Maid");

        switch (type)
        {
            case UpgradeType.Attack:
                if (isAzure) return LocalizationManager.T("Trọng Kiếm Sóng (Tấn Công)", "Wave Greatsword (Attack)");
                if (isRonin) return LocalizationManager.T("Phong Nguyệt Đao (Tấn Công)", "Gale Blade (Attack)");
                return LocalizationManager.T("Kiếm Tàn Tích (Tấn Công)", "Ruin Blade (Attack)");

            case UpgradeType.Health:
                return LocalizationManager.T("Sinh Lực Khổng Lồ (Máu)", "Titan Vitality (Health)");

            case UpgradeType.Agility:
                if (isAzure) return LocalizationManager.T("Trọng Bộ Lướt (Tốc Độ & Giảm CD Sóng)", "Heavy Stride (Speed & Wave CD)");
                if (isRonin) return LocalizationManager.T("Phong Ảnh Thần Tốc (Tốc Độ & Lướt)", "Phantom Gale (Speed & Dash)");
                return LocalizationManager.T("Thoát Thân Bóng Ma (Tốc Độ & Lướt)", "Agile Shadow (Speed & Dash)");

            case UpgradeType.Ultimate:
                if (isAzure) return LocalizationManager.T("Bão Sóng Kiếm (Tuyệt Kỹ)", "Crescent Tempest (Ultimate)");
                if (isRonin) return LocalizationManager.T("Ám Ảnh Trảm (Tuyệt Kỹ)", "Shadow Strike (Ultimate)");
                return LocalizationManager.T("Cơn Lốc Tàn Tích (Tuyệt Kỹ)", "Ruin Whirlwind (Ultimate)");

            default:
                return LocalizationManager.T("Nâng Cấp", "Upgrade");
        }
    }

    public const float MAX_ULTI_CD_REDUCTION = 15.0f; // Giảm tối đa 15.0s (Tuyệt kỹ còn tối thiểu 5.0s)
    public const float MAX_DASH_CD_REDUCTION = 4.0f;  // Giảm tối đa 4.0s (Lướt né còn tối thiểu 1.0s)
    public const float MAX_SPEED_BONUS = 3.2f;        // Tăng tối đa +3.20 tốc độ chạy
    public const float MAX_WAVE_CD_REDUCTION = 0.75f; // Giảm tối đa 0.75s hồi sóng kiếm (còn tối thiểu 0.45s)

    public int GetMaxLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Agility:
                return 80; // Lv.80 đạt trần +3.20 Speed & -4.00s Dash CD
            case UpgradeType.Attack:
                return 200;
            case UpgradeType.Health:
                return 200;
            case UpgradeType.Ultimate:
                return 200;
            default:
                return 200;
        }
    }

    public bool IsMaxLevel(UpgradeType type)
    {
        return GetLevel(type) >= GetMaxLevel(type);
    }

    public string GetDescription(UpgradeType type, int level)
    {
        string hero = GetCurrentHeroKey();
        bool isAzure = (hero == CharacterManager.CHAR_AZURE_BLADE);
        bool isRonin = (hero == CharacterManager.CHAR_GALE_RONIN || hero == "Maid");

        switch (type)
        {
            case UpgradeType.Attack:
                return LocalizationManager.T("+" + (level * 2) + " Sát thương (+ATK)", "+" + (level * 2) + " Attack Damage (+ATK)");

            case UpgradeType.Health:
                return LocalizationManager.T("+" + (level * 15) + " Máu tối đa (+Max HP)", "+" + (level * 15) + " Max Health (+Max HP)");

            case UpgradeType.Agility:
                float spd = Mathf.Min(MAX_SPEED_BONUS, level * 0.08f);
                float dash = Mathf.Min(MAX_DASH_CD_REDUCTION, level * 0.05f);
                bool isSpdMax = (level * 0.08f >= MAX_SPEED_BONUS);
                bool isDashMax = (level * 0.05f >= MAX_DASH_CD_REDUCTION);

                if (isAzure)
                {
                    float wave = Mathf.Min(MAX_WAVE_CD_REDUCTION, level * 0.35f);
                    bool isWaveMax = (level * 0.35f >= MAX_WAVE_CD_REDUCTION);
                    return LocalizationManager.T(
                        $"+{spd:F2} Tốc độ{(isSpdMax ? " (MAX)" : "")} | -{dash:F2}s Lướt{(isDashMax ? " (MAX)" : "")} | -{wave:F2}s Sóng Kiếm{(isWaveMax ? " (MAX)" : "")}",
                        $"+{spd:F2} Speed{(isSpdMax ? " (MAX)" : "")} | -{dash:F2}s Dash{(isDashMax ? " (MAX)" : "")} | -{wave:F2}s Wave CD{(isWaveMax ? " (MAX)" : "")}");
                }
                return LocalizationManager.T(
                    $"+{spd:F2} Tốc độ{(isSpdMax ? " (MAX)" : "")} | -{dash:F2}s Lướt né{(isDashMax ? " (MAX)" : "")}",
                    $"+{spd:F2} Speed{(isSpdMax ? " (MAX)" : "")} | -{dash:F2}s Dash CD{(isDashMax ? " (MAX)" : "")}");

            case UpgradeType.Ultimate:
                float ultiCd = Mathf.Min(MAX_ULTI_CD_REDUCTION, level * 0.6f);
                int ultiDmg = level * 8;
                bool isUltiCdMax = (level * 0.6f >= MAX_ULTI_CD_REDUCTION);

                if (isUltiCdMax)
                {
                    return LocalizationManager.T($"-{ultiCd:F1}s Hồi Tuyệt Kỹ (MAX) | +{ultiDmg} Sát thương",
                                                 $"-{ultiCd:F1}s Ulti CD (MAX) | +{ultiDmg} Damage");
                }
                return LocalizationManager.T($"-{ultiCd:F1}s Hồi Tuyệt Kỹ | +{ultiDmg} Sát thương",
                                             $"-{ultiCd:F1}s Ulti CD | +{ultiDmg} Damage");

            default:
                return "";
        }
    }

    public string GetNextBonusDescription(UpgradeType type)
    {
        if (IsMaxLevel(type))
        {
            return "MAX";
        }

        string hero = GetCurrentHeroKey();
        bool isAzure = (hero == CharacterManager.CHAR_AZURE_BLADE);
        int level = GetLevel(type);

        switch (type)
        {
            case UpgradeType.Attack:
                return LocalizationManager.T("+2 Sát thương", "+2 Strike Damage");

            case UpgradeType.Health:
                return LocalizationManager.T("+15 Máu tối đa & Hồi máu", "+15 Max Health & Heal");

            case UpgradeType.Agility:
                bool isSpdMax = (level * 0.08f >= MAX_SPEED_BONUS);
                bool isDashMax = (level * 0.05f >= MAX_DASH_CD_REDUCTION);

                if (isSpdMax && isDashMax)
                {
                    return "MAX";
                }
                if (isDashMax)
                {
                    return LocalizationManager.T("+0.08 Tốc chạy (Lướt MAX)", "+0.08 Speed (Dash MAX)");
                }
                if (isSpdMax)
                {
                    return LocalizationManager.T("-0.05s Hồi lướt (Tốc độ MAX)", "-0.05s Dash CD (Speed MAX)");
                }
                if (isAzure)
                    return LocalizationManager.T("+0.08 Tốc chạy | -0.05s Lướt | -0.35s Sóng Kiếm", "+0.08 Speed | -0.05s Dash | -0.35s Wave CD");
                return LocalizationManager.T("+0.08 Tốc chạy | -0.05s Hồi lướt", "+0.08 Speed | -0.05s Dash CD");

            case UpgradeType.Ultimate:
                bool isCdMaxed = (level * 0.6f >= MAX_ULTI_CD_REDUCTION);
                if (isCdMaxed)
                {
                    return LocalizationManager.T("+8 Sát thương (Hồi chiêu MAX)", "+8 Damage (CD MAX)");
                }
                return LocalizationManager.T("-0.6s Hồi chiêu & +8 Sát thương", "-0.6s Cooldown & +8 Damage");

            default:
                return "";
        }
    }

    public bool CanUpgrade(UpgradeType type)
    {
        if (IsMaxLevel(type)) return false;
        FindPlayerComponents();
        if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        long currentShards = (inventory != null) ? inventory.GetShards() : AccountDatabase.GetAethelShards();

        int cost = GetCost(type);
        return currentShards >= cost;
    }

    public bool TryPurchaseUpgrade(UpgradeType type)
    {
        FindPlayerComponents();
        if (IsMaxLevel(type) || !CanUpgrade(type)) return false;

        int cost = GetCost(type);
        bool spent = false;
        if (inventory != null)
        {
            spent = inventory.SpendShards(cost);
        }
        else
        {
            spent = AccountDatabase.SpendAethelShards(cost);
        }

        if (spent)
        {
            string heroKey = GetCurrentHeroKey();
            int nextLevel = GetLevel(type, heroKey) + 1;
            PlayerPrefs.SetInt($"Upgrade_{heroKey}_{type}_Level", nextLevel);

            if (heroKey == CharacterManager.CHAR_VAELSTRIDER || heroKey == "Vaelstrider")
            {
                PlayerPrefs.SetInt("Upgrade_" + type.ToString() + "_Level", nextLevel);
            }
            PlayerPrefs.Save();

            AccountDatabase.SaveActiveSessionToDatabase();

            ApplyAllUpgrades();
            OnUpgradesChanged?.Invoke();

            Debug.Log($"<color=yellow>[UPGRADE SUCCESS]</color> Đã nâng cấp {type} cho {heroKey} lên Cấp {nextLevel}!");
            return true;
        }

        return false;
    }

    public void ApplyAllUpgrades()
    {
        FindPlayerComponents();

        // 1. Attack Bonus (+2 per level)
        int atkLevel = GetLevel(UpgradeType.Attack);
        int bonusAtk = atkLevel * 2;
        if (playerCombat != null)
        {
            playerCombat.ApplyUpgradeBonus(bonusAtk, GetUltiCooldownReduction(), GetUltiDamageBonus());
        }

        // 2. Health Bonus (+15 HP per level)
        int hpLevel = GetLevel(UpgradeType.Health);
        int bonusHp = hpLevel * 15;
        if (playerHealth != null)
        {
            playerHealth.ApplyUpgradeBonus(bonusHp);
        }

        // 3. Agility Bonus
        int agiLevel = GetLevel(UpgradeType.Agility);
        float bonusSpeed = Mathf.Min(MAX_SPEED_BONUS, agiLevel * 0.08f);
        float bonusDashCd = Mathf.Min(MAX_DASH_CD_REDUCTION, agiLevel * 0.05f);
        if (playerController != null)
        {
            playerController.ApplyUpgradeBonus(bonusSpeed, bonusDashCd);
        }

        // 4. Update Azure Blade Skill / Gale Ronin Skill
        var azure = FindAnyObjectByType<AzureBladeSkill>();
        if (azure != null)
        {
            azure.RefreshCooldownStats();
        }

        var ronin = FindAnyObjectByType<GaleRoninSkill>();
        if (ronin != null)
        {
            ronin.RefreshCooldownStats();
        }
    }

    public float GetUltiCooldownReduction()
    {
        return Mathf.Min(MAX_ULTI_CD_REDUCTION, GetLevel(UpgradeType.Ultimate) * 0.6f);
    }

    public int GetUltiDamageBonus()
    {
        return GetLevel(UpgradeType.Ultimate) * 8;
    }

    public int GetAttackBonus() => GetLevel(UpgradeType.Attack) * 2;
    public int GetHealthBonus() => GetLevel(UpgradeType.Health) * 15;
    public float GetDashCooldownReduction() => Mathf.Min(MAX_DASH_CD_REDUCTION, GetLevel(UpgradeType.Agility) * 0.05f);
    public float GetSpeedBonus() => Mathf.Min(MAX_SPEED_BONUS, GetLevel(UpgradeType.Agility) * 0.08f);
    public string GetUpgradeDescription(UpgradeType type) => GetDescription(type, GetLevel(type));
    public string GetNextBonusString(UpgradeType type) => GetNextBonusDescription(type);

    public static void ResetEntireGameProgress()
    {
        Time.timeScale = 1.0f;
        AccountDatabase.SetAethelShards(0);

        PlayerPrefs.SetInt("Save_HealthPotions", 0);
        PlayerPrefs.SetInt("Saved_HealthPotions", 0);
        PlayerPrefs.DeleteKey("Saved_CurrentHealth");

        PlayerPrefs.SetInt("Has_Lost_Shards", 0);
        PlayerPrefs.SetInt("Lost_Shards_Amount", 0);
        PlayerPrefs.DeleteKey("Lost_Shards_X");
        PlayerPrefs.DeleteKey("Lost_Shards_Y");
        PlayerPrefs.DeleteKey("Lost_Shards_Scene");

        // Reset nâng cấp cho tất cả các tướng
        string[] heroes = new string[] { CharacterManager.CHAR_VAELSTRIDER, CharacterManager.CHAR_GALE_RONIN, CharacterManager.CHAR_AZURE_BLADE, "Maid" };
        string[] types = new string[] { "Attack", "Health", "Agility", "Ultimate" };

        foreach (var h in heroes)
        {
            foreach (var t in types)
            {
                PlayerPrefs.DeleteKey($"Upgrade_{h}_{t}_Level");
            }
        }
        foreach (var t in types)
        {
            PlayerPrefs.DeleteKey($"Upgrade_{t}_Level");
        }

        PlayerPrefs.SetString("SelectedHero", "vael");
        PlayerPrefs.SetString("Selected_Character_ID", CharacterManager.CHAR_VAELSTRIDER);
        PlayerPrefs.SetInt("Map1_Completed", 0);
        PlayerPrefs.Save();

        PlayerInventory inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.ResetInventory(0, 0);
        }

        if (Instance != null)
        {
            Instance.ApplyAllUpgrades();
        }

        Debug.Log("<color=red>[HARDCORE RESET]</color> Toàn bộ tiến trình game đã được reset về con số 0!");
    }

    public static PlayerUpgradeManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<PlayerUpgradeManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject go = new GameObject("PlayerUpgradeManager");
        Instance = go.AddComponent<PlayerUpgradeManager>();
        return Instance;
    }
}
