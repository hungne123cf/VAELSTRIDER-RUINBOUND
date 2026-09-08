using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    [Header("Currency")]
    [SerializeField] private int aethelShards = 0;

    [Header("Consumables")]
    [SerializeField] private int healthPotions = 0;

    public event Action<int> OnShardsChanged;
    public event Action<int> OnPotionsChanged;

    void Awake()
    {
        // Load saved shards and potions from PlayerPrefs
        aethelShards = PlayerPrefs.GetInt("Saved_AethelShards", aethelShards);
        healthPotions = PlayerPrefs.GetInt("Saved_HealthPotions", healthPotions);
    }

    void Start()
    {
        OnShardsChanged?.Invoke(aethelShards);
        OnPotionsChanged?.Invoke(healthPotions);

        // Tu dong dam bao co nut Btn_Potion tren Canvas
        PotionQuickSlotUI.EnsureUIExists();
    }

    public void AddShards(int amount)
    {
        aethelShards += amount;
        SaveData();
        Debug.Log($"<color=cyan>[INVENTORY]</color> Nhặt được +{amount} Aethel Shards! Tổng sở hữu: {aethelShards}");
        OnShardsChanged?.Invoke(aethelShards);
    }

    public bool SpendShards(int amount)
    {
        if (aethelShards >= amount)
        {
            aethelShards -= amount;
            SaveData();
            OnShardsChanged?.Invoke(aethelShards);
            return true;
        }
        return false;
    }

    public int GetShards() => aethelShards;

    public void AddPotions(int count)
    {
        healthPotions += count;
        SaveData();
        Debug.Log($"<color=green>[INVENTORY]</color> Đã lưu +{count} Bình Máu vào túi đồ! Tổng sở hữu: {healthPotions}");
        OnPotionsChanged?.Invoke(healthPotions);
        DamagePopup.CreateText(transform.position, $"+{count} Potion", new Color(1f, 0.35f, 0.45f));
    }

    public bool UsePotion()
    {
        if (healthPotions <= 0)
        {
            Debug.Log("<color=yellow>[INVENTORY]</color> Bạn đã hết bình máu!");
            DamagePopup.CreateText(transform.position, "No Potions!", Color.yellow);
            return false;
        }

        var health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            if (health.IsFullHealth())
            {
                Debug.Log("<color=yellow>[INVENTORY]</color> Máu đã đầy, không cần uống!");
                DamagePopup.CreateText(transform.position, "Full HP!", Color.yellow);
                return false;
            }

            healthPotions--;
            SaveData();
            health.Heal(35);
            SoundManager.EnsureInstance()?.PlayPotionDrink();
            OnPotionsChanged?.Invoke(healthPotions);
            Debug.Log($"<color=green>[INVENTORY]</color> Đã dùng 1 bình máu! Còn lại: {healthPotions}");
            return true;
        }
        return false;
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt("Saved_AethelShards", aethelShards);
        PlayerPrefs.SetInt("Saved_HealthPotions", healthPotions);
        PlayerPrefs.Save();
    }

    public int GetPotions() => healthPotions;
}
