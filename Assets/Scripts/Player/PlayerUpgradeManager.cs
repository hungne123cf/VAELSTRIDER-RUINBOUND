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

    public const int MAX_LEVEL = 10;

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

    public int GetLevel(UpgradeType type)
    {
        return PlayerPrefs.GetInt("Upgrade_" + type.ToString() + "_Level", 0);
    }

    public int GetCost(UpgradeType type)
    {
        int level = GetLevel(type);
        if (level >= MAX_LEVEL) return -1; // Max level reached

        switch (type)
        {
            case UpgradeType.Attack:
                return (level + 1) * 6; // 6, 12, 18, 24, 30, 36...
            case UpgradeType.Health:
                return (level + 1) * 5; // 5, 10, 15, 20, 25, 30...
            case UpgradeType.Agility:
                return (level + 1) * 5; // 5, 10, 15, 20, 25, 30...
            case UpgradeType.Ultimate:
                return (level + 1) * 8; // 8, 16, 24, 32, 40, 48...
            default:
                return 10;
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
                return "+" + (level * 6) + " Sát thương (+ATK)";
            case UpgradeType.Health:
                return "+" + (level * 30) + " Máu tối đa (+Max HP)";
            case UpgradeType.Agility:
                return "+" + (level * 0.35f).ToString("F1") + " Tốc độ | -" + (level * 0.06f).ToString("F2") + "s Dash";
            case UpgradeType.Ultimate:
                return "-" + (level * 0.5f).ToString("F1") + "s Hồi Ulti | +" + (level * 15) + " Dmg";
            default:
                return "";
        }
    }

    public string GetNextBonusDescription(UpgradeType type)
    {
        int level = GetLevel(type);
        if (level >= MAX_LEVEL) return "ĐÃ ĐẠT CẤP TỐI ĐA";

        switch (type)
        {
            case UpgradeType.Attack: return "+6 Sát thương chém";
            case UpgradeType.Health: return "+30 Máu tối đa & Hồi máu";
            case UpgradeType.Agility: return "+0.35 Tốc chạy & -0.06s Dash";
            case UpgradeType.Ultimate: return "-0.5s Hồi chiêu & +15 Dmg";
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

        // 1. Attack Bonus
        int atkLevel = GetLevel(UpgradeType.Attack);
        int bonusAtk = atkLevel * 6;
        if (playerCombat != null)
        {
            playerCombat.ApplyUpgradeBonus(bonusAtk, GetUltiCooldownReduction(), GetUltiDamageBonus());
        }

        // 2. Health Bonus (+30 HP per level)
        int hpLevel = GetLevel(UpgradeType.Health);
        int bonusHp = hpLevel * 30;
        if (playerHealth != null)
        {
            playerHealth.ApplyUpgradeBonus(bonusHp);
        }

        // 3. Agility Bonus
        int agiLevel = GetLevel(UpgradeType.Agility);
        float bonusSpeed = agiLevel * 0.35f;
        float bonusDashCd = agiLevel * 0.06f;
        if (playerController != null)
        {
            playerController.ApplyUpgradeBonus(bonusSpeed, bonusDashCd);
        }
    }

    public float GetUltiCooldownReduction()
    {
        return GetLevel(UpgradeType.Ultimate) * 0.5f;
    }

    public int GetUltiDamageBonus()
    {
        return GetLevel(UpgradeType.Ultimate) * 15;
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
