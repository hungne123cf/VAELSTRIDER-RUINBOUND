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

        // Xóa sạch triệt để toàn bộ key nâng cấp trụ cũ để chuyển đổi 100% sang chế độ Roguelike
        string[] allHeroes = new string[]
        {
            CharacterManager.CHAR_VAELSTRIDER, CharacterManager.CHAR_GALE_RONIN,
            CharacterManager.CHAR_AZURE_BLADE, CharacterManager.CHAR_TITAN_BERSERKER,
            CharacterManager.CHAR_DUNE_ASSASSIN, CharacterManager.CHAR_CELESTIAL_MAGE,
            "vael", "ronin", "azure", "titan", "dune", "mage", "Maid",
            "Vaelstrider", "GaleRonin", "AzureBlade", "TitanBerserker", "DuneAssassin", "CelestialMage",
            "Player1", "Player2", "Player3", "Player4", "Player5", "Player6"
        };
        string[] upTypes = new string[] { "Attack", "Health", "Agility", "Ultimate" };
        bool modified = false;
        foreach (var h in allHeroes)
        {
            foreach (var t in upTypes)
            {
                string k = $"Upgrade_{h}_{t}_Level";
                if (PlayerPrefs.HasKey(k)) { PlayerPrefs.DeleteKey(k); modified = true; }
            }
        }
        foreach (var t in upTypes)
        {
            string k = $"Upgrade_{t}_Level";
            if (PlayerPrefs.HasKey(k)) { PlayerPrefs.DeleteKey(k); modified = true; }
        }
        if (modified) PlayerPrefs.Save();
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

    public static string NormalizeHeroKey(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return CharacterManager.CHAR_VAELSTRIDER;
        string h = heroId.Trim().ToLower();
        if (h.Contains("titan") || h.Contains("player4")) return CharacterManager.CHAR_TITAN_BERSERKER;
        if (h.Contains("azure") || h.Contains("player3")) return CharacterManager.CHAR_AZURE_BLADE;
        if (h.Contains("ronin") || h.Contains("gale") || h.Contains("maid") || h.Contains("elysia") || h.Contains("player2")) return CharacterManager.CHAR_GALE_RONIN;
        if (h.Contains("dune") || h.Contains("player5")) return CharacterManager.CHAR_DUNE_ASSASSIN;
        if (h.Contains("mage") || h.Contains("celestial") || h.Contains("player6")) return CharacterManager.CHAR_CELESTIAL_MAGE;
        return CharacterManager.CHAR_VAELSTRIDER;
    }

    public string GetCurrentHeroKey()
    {
        var cm = CharacterManager.EnsureInstance();
        string heroId = (cm != null) ? cm.GetSelectedCharacterId() : "";
        if (string.IsNullOrEmpty(heroId)) heroId = PlayerPrefs.GetString("SelectedHero", "");
        if (string.IsNullOrEmpty(heroId)) heroId = "Vaelstrider";
        return NormalizeHeroKey(heroId);
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
        // Chế độ Roguelike (1 mạng): Toàn bộ nâng cấp diễn ra trong trận qua Level & Lõi Nâng Cấp (Augments), không còn buff tĩnh vĩnh viễn từ trụ cũ
        return 0;
    }

    public int GetCost(UpgradeType type)
    {
        int level = GetLevel(type);
        return GetCostAtLevel(type, level);
    }

    public static int GetCostAtLevel(UpgradeType type, int level)
    {
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

    public void GetCostAndLevelsForMultiplier(UpgradeType type, int multiplier, out int buyCount, out long totalCost)
    {
        int curLevel = GetLevel(type);
        long curShards = CurrentShards;

        if (multiplier <= 0) // Chế độ MAX: Mua tối đa có thể với số Shards hiện có
        {
            buyCount = 0;
            totalCost = 0;
            while (true)
            {
                long nextCost = GetCostAtLevel(type, curLevel + buyCount);
                if (totalCost + nextCost > curShards) break;
                totalCost += nextCost;
                buyCount++;
                if (buyCount >= 50000) break;
            }
            if (buyCount == 0)
            {
                buyCount = 1;
                totalCost = GetCostAtLevel(type, curLevel);
            }
        }
        else // Chế độ cố định (x1, x10, x50...)
        {
            buyCount = multiplier;
            totalCost = 0;
            for (int i = 0; i < buyCount; i++)
            {
                totalCost += GetCostAtLevel(type, curLevel + i);
            }
        }
    }

    public string GetTitle(UpgradeType type)
    {
        string hero = GetCurrentHeroKey();
        bool isTitan = (hero == CharacterManager.CHAR_TITAN_BERSERKER);
        bool isAzure = (hero == CharacterManager.CHAR_AZURE_BLADE);
        bool isRonin = (hero == CharacterManager.CHAR_GALE_RONIN || hero == "Maid");

        switch (type)
        {
            case UpgradeType.Attack:
                if (isTitan) return LocalizationManager.T("Đại Chùy Titan (Tấn Công)", "Titan Hammer (Attack)");
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
            {
                bool isMage = (hero == CharacterManager.CHAR_CELESTIAL_MAGE || hero.ToLower().Contains("mage") || hero.ToLower().Contains("player6") || hero.ToLower().Contains("celestial"));
                string dmgUnit = (isMage || isAzure) ? "<color=#B84DFF>AP</color>" : "<color=#FF3B30>ATK</color>";
                return $"+{level * 2} {dmgUnit}";
            }

            case UpgradeType.Health:
                return LocalizationManager.T("+" + (level * 15) + " Máu tối đa (+HP)", "+" + (level * 15) + " Max Health (+HP)");

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
            {
                float ultiCd = Mathf.Min(MAX_ULTI_CD_REDUCTION, level * 0.6f);
                int ultiDmg = level * 8;
                bool isUltiCdMax = (level * 0.6f >= MAX_ULTI_CD_REDUCTION);
                bool isMage = (hero == CharacterManager.CHAR_CELESTIAL_MAGE || hero.ToLower().Contains("mage") || hero.ToLower().Contains("player6") || hero.ToLower().Contains("celestial"));
                string ultiDmgUnit = (isMage || isAzure) ? "<color=#B84DFF>AP</color>" : "<color=#FF3B30>ATK</color>";

                if (isUltiCdMax)
                {
                    return LocalizationManager.T($"-{ultiCd:F1}s Hồi Tuyệt Kỹ (MAX) | +{ultiDmg} {ultiDmgUnit}",
                                                 $"-{ultiCd:F1}s Ulti CD (MAX) | +{ultiDmg} {ultiDmgUnit}");
                }
                return LocalizationManager.T($"-{ultiCd:F1}s Hồi Tuyệt Kỹ | +{ultiDmg} {ultiDmgUnit}",
                                             $"-{ultiCd:F1}s Ulti CD | +{ultiDmg} {ultiDmgUnit}");
            }

            default:
                return "";
        }
    }

    public string GetNextBonusDescription(UpgradeType type, int count = 1)
    {
        if (IsMaxLevel(type))
        {
            return "MAX";
        }

        string hero = GetCurrentHeroKey();
        bool isAzure = (hero == CharacterManager.CHAR_AZURE_BLADE || hero.ToLower().Contains("azure") || hero.ToLower().Contains("player3"));
        bool isMage = (hero == CharacterManager.CHAR_CELESTIAL_MAGE || hero.ToLower().Contains("mage") || hero.ToLower().Contains("player6") || hero.ToLower().Contains("celestial"));
        int level = GetLevel(type);

        if (count <= 1)
        {
            switch (type)
            {
                case UpgradeType.Attack:
                    return (isMage || isAzure) ? "<color=#B84DFF>+2 AP</color>" : "<color=#FF3B30>+2 ATK</color>";

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
                {
                    bool isCdMaxed = (level * 0.6f >= MAX_ULTI_CD_REDUCTION);
                    string nextUltiUnit = (isMage || isAzure) ? "<color=#B84DFF>AP</color>" : "<color=#FF3B30>ATK</color>";
                    if (isCdMaxed)
                    {
                        return LocalizationManager.T($"+8 {nextUltiUnit} (Hồi chiêu MAX)", $"+8 {nextUltiUnit} (CD MAX)");
                    }
                    return LocalizationManager.T($"-0.6s Hồi chiêu & +8 {nextUltiUnit}", $"-0.6s Cooldown & +8 {nextUltiUnit}");
                }

                default:
                    return "";
            }
        }
        else
        {
            switch (type)
            {
                case UpgradeType.Attack:
                {
                    int add = count * 2;
                    return (isMage || isAzure) ? $"<color=#B84DFF>+{add} AP</color>" : $"<color=#FF3B30>+{add} ATK</color>";
                }

                case UpgradeType.Health:
                {
                    int add = count * 15;
                    return LocalizationManager.T($"+{add} Máu tối đa & Hồi máu", $"+{add} Max Health & Heal");
                }

                case UpgradeType.Agility:
                {
                    float addSpd = count * 0.08f;
                    float addDash = count * 0.05f;
                    if (isAzure)
                    {
                        float addWave = count * 0.35f;
                        return LocalizationManager.T($"+{addSpd:F2} Tốc | -{addDash:F2}s Lướt | -{addWave:F2}s Sóng", $"+{addSpd:F2} Spd | -{addDash:F2}s Dash | -{addWave:F2}s Wave");
                    }
                    return LocalizationManager.T($"+{addSpd:F2} Tốc chạy | -{addDash:F2}s Hồi lướt", $"+{addSpd:F2} Speed | -{addDash:F2}s Dash CD");
                }

                case UpgradeType.Ultimate:
                {
                    int addDmg = count * 8;
                    float addCd = count * 0.6f;
                    string nextUltiUnit = (isMage || isAzure) ? "<color=#B84DFF>AP</color>" : "<color=#FF3B30>ATK</color>";
                    return LocalizationManager.T($"-{addCd:F1}s Hồi chiêu & +{addDmg} {nextUltiUnit}", $"-{addCd:F1}s Cooldown & +{addDmg} {nextUltiUnit}");
                }

                default:
                    return "";
            }
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
        return TryPurchaseUpgradeMulti(type, 1);
    }

    public bool TryPurchaseUpgradeMulti(UpgradeType type, int count)
    {
        if (count <= 0) return false;
        FindPlayerComponents();
        if (IsMaxLevel(type)) return false;

        string heroKey = GetCurrentHeroKey();
        int curLevel = GetLevel(type, heroKey);
        long curShards = CurrentShards;

        int actualCount = 0;
        long totalCost = 0;
        for (int i = 0; i < count; i++)
        {
            long nextCost = GetCostAtLevel(type, curLevel + i);
            if (totalCost + nextCost > curShards) break;
            totalCost += nextCost;
            actualCount++;
        }

        if (actualCount <= 0 || totalCost <= 0) return false;

        bool spent = false;
        if (inventory != null)
        {
            spent = inventory.SpendShards(totalCost);
        }
        else
        {
            spent = AccountDatabase.SpendAethelShards(totalCost);
        }

        if (spent)
        {
            int nextLevel = curLevel + actualCount;
            PlayerPrefs.SetInt($"Upgrade_{heroKey}_{type}_Level", nextLevel);

            if (heroKey == CharacterManager.CHAR_VAELSTRIDER || heroKey == "Vaelstrider")
            {
                PlayerPrefs.SetInt("Upgrade_" + type.ToString() + "_Level", nextLevel);
            }
            PlayerPrefs.Save();
            AccountDatabase.SaveActiveSessionToDatabase();

            ApplyAllUpgrades();
            OnUpgradesChanged?.Invoke();

            Debug.Log($"<color=yellow>[UPGRADE SUCCESS]</color> Đã nâng cấp {type} (+{actualCount} Cấp) cho {heroKey} lên Cấp {nextLevel}!");
            return true;
        }

        return false;
    }

    public int UpgradeAllMax()
    {
        FindPlayerComponents();
        long curShards = CurrentShards;
        if (curShards <= 0) return 0;

        string heroKey = GetCurrentHeroKey();
        int[] levels = new int[4];
        for (int i = 0; i < 4; i++) levels[i] = GetLevel((UpgradeType)i, heroKey);

        long spentTotal = 0;
        int upgradesBought = 0;

        while (true)
        {
            int bestType = -1;
            long lowestCost = long.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                long cost = GetCostAtLevel((UpgradeType)i, levels[i]);
                if (cost < lowestCost)
                {
                    lowestCost = cost;
                    bestType = i;
                }
            }

            if (bestType == -1 || spentTotal + lowestCost > curShards) break;

            spentTotal += lowestCost;
            levels[bestType]++;
            upgradesBought++;

            if (upgradesBought >= 100000) break;
        }

        if (upgradesBought > 0 && spentTotal > 0)
        {
            bool spent = false;
            if (inventory != null) spent = inventory.SpendShards(spentTotal);
            else spent = AccountDatabase.SpendAethelShards(spentTotal);

            if (spent)
            {
                for (int i = 0; i < 4; i++)
                {
                    var type = (UpgradeType)i;
                    PlayerPrefs.SetInt($"Upgrade_{heroKey}_{type}_Level", levels[i]);
                    if (heroKey == CharacterManager.CHAR_VAELSTRIDER || heroKey == "Vaelstrider")
                    {
                        PlayerPrefs.SetInt("Upgrade_" + type.ToString() + "_Level", levels[i]);
                    }
                }
                PlayerPrefs.Save();
                AccountDatabase.SaveActiveSessionToDatabase();

                ApplyAllUpgrades();
                OnUpgradesChanged?.Invoke();
                return upgradesBought;
            }
        }
        return 0;
    }

    public void ApplyAllUpgrades()
    {
        FindPlayerComponents();
        string heroKey = GetCurrentHeroKey();

        // 1. Attack Bonus (+2 per level)
        int atkLevel = GetLevel(UpgradeType.Attack, heroKey);
        int bonusAtk = atkLevel * 2;
        if (playerCombat != null)
        {
            playerCombat.ApplyUpgradeBonus(bonusAtk, GetUltiCooldownReduction(heroKey), GetUltiDamageBonus(heroKey));
        }

        // 2. Health Bonus (+15 HP per level)
        int hpLevel = GetLevel(UpgradeType.Health, heroKey);
        int bonusHp = hpLevel * 15;
        if (playerHealth != null)
        {
            playerHealth.ApplyUpgradeBonus(bonusHp);
        }

        // 3. Agility Bonus
        int agiLevel = GetLevel(UpgradeType.Agility, heroKey);
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

    public float GetUltiCooldownReduction(string heroId = null) => 0f;
    public int GetUltiDamageBonus(string heroId = null) => 0;
    public int GetAttackBonus(string heroId = null) => 0;
    public int GetHealthBonus(string heroId = null) => 0;
    public float GetDashCooldownReduction(string heroId = null) => 0f;
    public float GetSpeedBonus(string heroId = null) => 0f;
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
        string[] heroes = new string[]
        {
            CharacterManager.CHAR_VAELSTRIDER, CharacterManager.CHAR_GALE_RONIN,
            CharacterManager.CHAR_AZURE_BLADE, CharacterManager.CHAR_TITAN_BERSERKER,
            CharacterManager.CHAR_DUNE_ASSASSIN, CharacterManager.CHAR_CELESTIAL_MAGE,
            "vael", "ronin", "azure", "titan", "dune", "mage", "Maid",
            "Vaelstrider", "GaleRonin", "AzureBlade", "TitanBerserker", "DuneAssassin", "CelestialMage",
            "Player1", "Player2", "Player3", "Player4", "Player5", "Player6"
        };
        string[] types = new string[] { "Attack", "Health", "Agility", "Ultimate" };

        foreach (var h in heroes)
        {
            foreach (var t in types)
            {
                PlayerPrefs.DeleteKey($"Upgrade_{h}_{t}_Level");
            }
            for (int i = 0; i < 6; i++)
            {
                PlayerPrefs.DeleteKey($"Hero_{h}_GearSlot_{i}");
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
