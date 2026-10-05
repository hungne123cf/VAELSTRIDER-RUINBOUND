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
        FindPlayerComponents();
        ApplyAllUpgrades();
    }

    private void FindPlayerComponents()
    {
        if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        if (playerCombat == null) playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

        public long CurrentShards
    {
        get
        {
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
            return inventory != null ? inventory.GetShards() : 0;
        }
    }

    public int GetUpgradeLevel(UpgradeType type) => GetLevel(type);
    public int GetUpgradeCost(UpgradeType type) => GetCost(type);
    public string GetUpgradeName(UpgradeType type) => GetTitle(type);
    public string GetCurrentEffectDescription(UpgradeType type) => GetDescription(type, GetLevel(type));

    public int GetLevel(UpgradeType type)
    {
        return PlayerPrefs.GetInt("Upgrade_" + type.ToString() + "_Level", 0);
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
        switch (type)
        {
            case UpgradeType.Attack: return LocalizationManager.T("Kiếm Tàn Tích (Tấn Công)", "Ruin Blade (Attack)");
            case UpgradeType.Health: return LocalizationManager.T("Sinh Lực Khổng Lồ (Máu)", "Titan Vitality (Health)");
            case UpgradeType.Agility: return LocalizationManager.T("Thoát Thân Bóng Ma (Tốc Độ)", "Agile Shadow (Speed)");
            case UpgradeType.Ultimate: return LocalizationManager.T("Cơn Bão Cổ Đại (Tuyệt Kỹ)", "Storm Mastery (Ultimate)");
            default: return LocalizationManager.T("Nâng Cấp", "Upgrade");
        }
    }

    public string GetDescription(UpgradeType type, int level)
    {
        switch (type)
        {
            case UpgradeType.Attack:
                return LocalizationManager.T("+" + (level * 2) + " Sát thương (+ATK)", "+" + (level * 2) + " Attack Damage (+ATK)");
            case UpgradeType.Health:
                return LocalizationManager.T("+" + (level * 15) + " Máu tối đa (+Max HP)", "+" + (level * 15) + " Max Health (+Max HP)");
            case UpgradeType.Agility:
                return LocalizationManager.T("+" + (level * 0.08f).ToString("F2") + " Tốc độ | -" + (level * 0.05f).ToString("F2") + "s Lướt | -" + (level * 0.35f).ToString("F2") + "s Hồi Kiếm Khí", "+" + (level * 0.08f).ToString("F2") + " Speed | -" + (level * 0.05f).ToString("F2") + "s Dash | -" + (level * 0.35f).ToString("F2") + "s Wave CD");
            case UpgradeType.Ultimate:
                return LocalizationManager.T("-" + (level * 0.6f).ToString("F1") + "s Hồi Ulti | +" + (level * 8) + " Sát thương", "-" + (level * 0.6f).ToString("F1") + "s Ulti CD | +" + (level * 8) + " Damage");
            default:
                return "";
        }
    }

    public string GetNextBonusDescription(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Attack: return LocalizationManager.T("+2 Sát thương chém", "+2 Strike Damage");
            case UpgradeType.Health: return LocalizationManager.T("+15 Máu tối đa & Hồi máu", "+15 Max Health & Heal");
            case UpgradeType.Agility: return LocalizationManager.T("+0.08 Tốc chạy | -0.05s Lướt | -0.35s Hồi Kiếm Khí", "+0.08 Speed | -0.05s Dash | -0.35s Wave CD");
            case UpgradeType.Ultimate: return LocalizationManager.T("-0.6s Hồi chiêu & +8 Sát thương", "-0.6s Cooldown & +8 Damage");
            default: return "";
        }
    }

    public bool CanUpgrade(UpgradeType type)
    {
        FindPlayerComponents();
        if (inventory == null) return false;

        int cost = GetCost(type);
        return inventory.GetShards() >= cost;
    }

    public bool TryPurchaseUpgrade(UpgradeType type)
    {
        FindPlayerComponents();
        if (!CanUpgrade(type)) return false;

        int cost = GetCost(type);
        if (inventory.SpendShards(cost))
        {
            int nextLevel = GetLevel(type) + 1;
            PlayerPrefs.SetInt("Upgrade_" + type.ToString() + "_Level", nextLevel);
            PlayerPrefs.Save();

            AccountDatabase.SaveActiveSessionToDatabase();

            ApplyAllUpgrades();
            OnUpgradesChanged?.Invoke();

            Debug.Log("<color=yellow>[UPGRADE SUCCESS]</color> Đã nâng cấp " + type + " lên Cấp " + nextLevel + "!");
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
        float bonusSpeed = agiLevel * 0.08f;
        float bonusDashCd = agiLevel * 0.05f;
        if (playerController != null)
        {
            playerController.ApplyUpgradeBonus(bonusSpeed, bonusDashCd);
        }
    }

    public float GetUltiCooldownReduction()
    {
        return GetLevel(UpgradeType.Ultimate) * 0.6f;
    }

    public int GetUltiDamageBonus()
    {
        return GetLevel(UpgradeType.Ultimate) * 8;
    }

    public int GetAttackBonus() => GetLevel(UpgradeType.Attack) * 2;
    public int GetHealthBonus() => GetLevel(UpgradeType.Health) * 15;
    public float GetDashCooldownReduction() => GetLevel(UpgradeType.Agility) * 0.05f;
    public bool IsMaxLevel(UpgradeType type) => false;
    public string GetUpgradeDescription(UpgradeType type) => GetDescription(type, GetLevel(type));
    public string GetNextBonusString(UpgradeType type) => GetNextBonusDescription(type);

    public static void ResetEntireGameProgress()
    {
        Time.timeScale = 1.0f;
        // 1. Reset Aethel Shards (dong bo ca Save_ va Saved_)
        AccountDatabase.SetAethelShards(0);

        // 2. Reset Health Potions ve 0
        PlayerPrefs.SetInt("Save_HealthPotions", 0);
        PlayerPrefs.SetInt("Saved_HealthPotions", 0);

        // 3. Reset mau da luu
        PlayerPrefs.DeleteKey("Saved_CurrentHealth");

        // 4. Xoa mo linh hon roi Dark Souls
        PlayerPrefs.SetInt("Has_Lost_Shards", 0);
        PlayerPrefs.SetInt("Lost_Shards_Amount", 0);
        PlayerPrefs.DeleteKey("Lost_Shards_X");
        PlayerPrefs.DeleteKey("Lost_Shards_Y");
        PlayerPrefs.DeleteKey("Lost_Shards_Scene");
        PlayerPrefs.DeleteKey("Save_LostShards");
        PlayerPrefs.DeleteKey("Save_DeathPosX");
        PlayerPrefs.DeleteKey("Save_DeathPosY");
        PlayerPrefs.DeleteKey("Save_DeathPosZ");
        PlayerPrefs.DeleteKey("Save_DeathScene");

        // 5. Reset toan bo 4 nhanh nang cap Den Tho (30 cap)
        PlayerPrefs.SetInt("Upgrade_Attack_Level", 0);
        PlayerPrefs.SetInt("Upgrade_Health_Level", 0);
        PlayerPrefs.SetInt("Upgrade_Agility_Level", 0);
        PlayerPrefs.SetInt("Upgrade_Ultimate_Level", 0);

        // 6. Khoa lai Maid va chon Vaelstrider mac dinh
        PlayerPrefs.SetInt("Char_Unlocked_Maid", 0);
        PlayerPrefs.SetString("Selected_Character_ID", "Vaelstrider");
        PlayerPrefs.SetInt("Map1_Completed", 0);

        PlayerPrefs.Save();

        // 7. Cap nhat tuc thi neu dang trong gameplay run
        PlayerInventory inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.ResetInventory(0, 0);
        }

        if (Instance != null)
        {
            Instance.ApplyAllUpgrades();
        }

        Debug.Log("<color=red>[HARDCORE RESET]</color> Toan bo tien trinh game (Shards, Potions, Upgrades, Maid) da duoc reset ve con so 0!");
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
