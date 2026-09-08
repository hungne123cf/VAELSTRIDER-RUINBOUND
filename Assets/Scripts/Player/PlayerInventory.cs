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

    /// <summary>
    /// Co che Dark Souls: Khi chet va chon Choi lai tu dau map, toan bo so Shards dang mang theo se bi roi tai vi tri tu tran!
    /// Nguoi choi phai quay lai nhat lai; neu chet lan 2 truoc khi nhat, so tien cu se bi de boi luot moi (mat vinh vien)!
    /// </summary>
    public void DropAllShardsOnDeath(Vector3 deathPos, string sceneName)
    {
        int dropped = aethelShards;
        aethelShards = 0;
        SaveData();
        OnShardsChanged?.Invoke(0);

        if (dropped > 0)
        {
            PlayerPrefs.SetInt("Has_Lost_Shards", 1);
            PlayerPrefs.SetInt("Lost_Shards_Amount", dropped);
            PlayerPrefs.SetFloat("Lost_Shards_X", deathPos.x);
            PlayerPrefs.SetFloat("Lost_Shards_Y", deathPos.y);
            PlayerPrefs.SetString("Lost_Shards_Scene", sceneName);
            PlayerPrefs.Save();
            Debug.Log($"<color=orange>[DARK SOULS DROP]</color> Chien binh da danh roi toan bo {dropped} Aethel Shards tai ({deathPos.x:F1}, {deathPos.y:F1})!");
        }
        else
        {
            // Neu chet ma tren nguoi co 0 Shards (tuc la chet tren duong di nhat lai ma chua nhat duoc),
            // thi mo hon cu bi de boi 0 shards (mat vinh vien)!
            PlayerPrefs.SetInt("Has_Lost_Shards", 0);
            PlayerPrefs.SetInt("Lost_Shards_Amount", 0);
            PlayerPrefs.Save();
            Debug.Log("<color=red>[DARK SOULS LOST]</color> Chien binh da chet truoc khi kip thu hoi vung hon! Toan bo so Shards cu da tan bien vinh vien!");
        }
    }
}
