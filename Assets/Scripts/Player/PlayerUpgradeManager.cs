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

    public const int MAX_LEVEL = 30;

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

        public int CurrentShards
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
        if (level >= MAX_LEVEL) return -1; // Max level 30

        switch (type)
        {
            case UpgradeType.Attack:
                return 4 + level * 5; // 4, 9, 14, ..., 149 shards
            case UpgradeType.Health:
                return 3 + level * 4; // 3, 7, 11, ..., 119 shards
            case UpgradeType.Agility:
                return 3 + level * 4; // 3, 7, 11, ..., 119 shards
            case UpgradeType.Ultimate:
                return 5 + level * 6; // 5, 11, 17, ..., 179 shards
            default:
                return 5 + level * 5;
        }
    }

    public string GetTitle(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Attack: return "Kiếm Tàn Tích (Ruin Blade)";
            case UpgradeType.Health: return "Sinh Lực Khổng Lồ (Titan Vitality)";
            case UpgradeType.Agility: return "Thoát Thân Bóng Ma (Agility)";
            case UpgradeType.Ultimate: return "Cơn Bão Cổ Đại (Storm Mastery)";
            default: return "Nâng Cấp";
        }
    }

    public string GetDescription(UpgradeType type, int level)
    {
        switch (type)
        {
            case UpgradeType.Attack:
                return "+" + (level * 2) + " Sát thương (+ATK)";
            case UpgradeType.Health:
                return "+" + (level * 15) + " Máu tối đa (+Max HP)";
            case UpgradeType.Agility:
                return "+" + (level * 0.08f).ToString("F2") + " Tốc độ | -" + (level * 0.05f).ToString("F2") + "s Dash";
            case UpgradeType.Ultimate:
                return "-" + (level * 0.6f).ToString("F1") + "s Hồi Ulti | +" + (level * 8) + " Dmg";
            default:
                return "";
        }
    }

    public string GetNextBonusDescription(UpgradeType type)
    {
        int level = GetLevel(type);
        if (level >= MAX_LEVEL) return "ĐÃ ĐẠT CẤP TỐI ĐA (30)";

        switch (type)
        {
            case UpgradeType.Attack: return "+2 Sát thương chém";
            case UpgradeType.Health: return "+15 Máu tối đa & Hồi máu";
            case UpgradeType.Agility: return "+0.08 Tốc chạy & -0.05s Dash";
            case UpgradeType.Ultimate: return "-0.6s Hồi chiêu & +8 Dmg";
            default: return "";
        }
    }

    public bool CanUpgrade(UpgradeType type)
    {
        FindPlayerComponents();
        if (inventory == null) return false;

        int level = GetLevel(type);
        if (level >= MAX_LEVEL) return false;

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

        // 1. Attack Bonus (+2 per level up to +60 at LV 30)
        int atkLevel = GetLevel(UpgradeType.Attack);
        int bonusAtk = atkLevel * 2;
        if (playerCombat != null)
        {
            playerCombat.ApplyUpgradeBonus(bonusAtk, GetUltiCooldownReduction(), GetUltiDamageBonus());
        }

        // 2. Health Bonus (+15 HP per level up to +450 HP at LV 30)
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

    public static void ResetEntireGameProgress()
    {
        // 1. Reset Aethel Shards
        PlayerPrefs.SetInt("Save_AethelShards", 0);

        // 2. Reset Health Potions ve 3 binh mac dinh
        PlayerPrefs.SetInt("Save_HealthPotions", 3);

        // 3. Xoa mo linh hon roi Dark Souls
        PlayerPrefs.DeleteKey("Save_LostShards");
        PlayerPrefs.DeleteKey("Save_DeathPosX");
        PlayerPrefs.DeleteKey("Save_DeathPosY");
        PlayerPrefs.DeleteKey("Save_DeathPosZ");
        PlayerPrefs.DeleteKey("Save_DeathScene");

        // 4. Reset toan bo 4 nhanh nang cap Den Tho
        PlayerPrefs.SetInt("Upgrade_Attack_Level", 0);
        PlayerPrefs.SetInt("Upgrade_Health_Level", 0);
        PlayerPrefs.SetInt("Upgrade_Agility_Level", 0);
        PlayerPrefs.SetInt("Upgrade_Ultimate_Level", 0);

        // 5. Khoa lai Maid va chon Vaelstrider mac dinh
        PlayerPrefs.SetInt("Char_Unlocked_Maid", 0);
        PlayerPrefs.SetString("Selected_Character_ID", "Vaelstrider");

        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.ApplyAllUpgrades();
        }
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
