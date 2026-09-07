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

    void Start()
    {
        OnShardsChanged?.Invoke(aethelShards);
        OnPotionsChanged?.Invoke(healthPotions);
    }

    // --- Aethel Shards ---
    public void AddShards(int amount)
    {
        aethelShards += amount;
        Debug.Log($"<color=cyan>[INVENTORY]</color> Nhặt được +{amount} Aethel Shards! Tổng sở hữu: {aethelShards}");
        OnShardsChanged?.Invoke(aethelShards);
    }

    public bool SpendShards(int amount)
    {
        if (aethelShards >= amount)
        {
            aethelShards -= amount;
            OnShardsChanged?.Invoke(aethelShards);
            return true;
        }
        return false;
    }

    public int GetShards() => aethelShards;

    // --- Health Potions ---
    public void AddPotions(int count)
    {
        healthPotions += count;
        Debug.Log($"<color=green>[INVENTORY]</color> Đã lưu +{count} Bình Máu vào túi đồ! Tổng sở hữu: {healthPotions}");
        OnPotionsChanged?.Invoke(healthPotions);
        DamagePopup.CreateText(transform.position, $"+{count} Potion", new Color(1f, 0.35f, 0.45f));
    }

    public bool UsePotion()
    {
        if (healthPotions <= 0)
        {
            Debug.Log("<color=yellow>[INVENTORY]</color> Bạn đã hết bình máu!");
            return false;
        }

        var health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            if (health.IsFullHealth())
            {
                DamagePopup.CreateText(transform.position, "Health Full!", new Color(1f, 0.9f, 0.2f));
                return false;
            }

            healthPotions--;
            health.Heal(35);
            OnPotionsChanged?.Invoke(healthPotions);
            Debug.Log($"<color=green>[INVENTORY]</color> Đã dùng 1 bình máu! Còn lại: {healthPotions}");
            return true;
        }
        return false;
    }

    public int GetPotions() => healthPotions;
}
