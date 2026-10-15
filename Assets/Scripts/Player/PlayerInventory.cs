using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    [Header("Currency")]
    [SerializeField] private long aethelShards = 0;

    [Header("Consumables")]
    [SerializeField] private int healthPotions = 0;

    public event Action<long> OnShardsChanged;
    public event Action<int> OnPotionsChanged;

    private static long currentRunShards = 0;

    public static bool IsBattleScene()
    {
        string s = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(s)) return false;
        return s.Contains("Map") || s.Contains("Valley") || s.Contains("Forgotten") || s == "SampleScene";
    }

    public static void ResetRunShards()
    {
        currentRunShards = 0;
    }

    public static long GetCurrentRunShards() => currentRunShards;

    void Awake()
    {
        AccountDatabase.EnsureActiveSessionLoaded();

        // Trong trận đấu, hiển thị số linh hồn kiếm được trong trận (bắt đầu từ 0)
        if (IsBattleScene())
        {
            aethelShards = currentRunShards;
        }
        else
        {
            currentRunShards = 0;
            aethelShards = AccountDatabase.GetAethelShards();
        }

        int defaultPotions = PlayerPrefs.GetInt("Saved_HealthPotions", healthPotions);
        healthPotions = PlayerPrefs.GetInt("Save_HealthPotions", defaultPotions);
    }

    void Start()
    {
        OnShardsChanged?.Invoke(aethelShards);
        OnPotionsChanged?.Invoke(healthPotions);

        // Tu dong dam bao co nut Btn_Potion tren Canvas
        PotionQuickSlotUI.EnsureUIExists();
    }

    void OnDisable()
    {
        SaveData();
    }

    void OnDestroy()
    {
        SaveData();
    }

    public void AddShards(long amount)
    {
        if (amount <= 0) return;

        if (IsBattleScene())
        {
            currentRunShards += amount;
            aethelShards = currentRunShards;
            AccountDatabase.AddAethelShards(amount);
            AccountDatabase.SaveActiveSessionToDatabase();
        }
        else
        {
            aethelShards += amount;
            AccountDatabase.SetAethelShards(aethelShards);
            AccountDatabase.SaveActiveSessionToDatabase();
        }

        SavePotionsData();
        Debug.Log($"<color=cyan>[INVENTORY]</color> Nhặt được +{amount} Aethel Shards! Trong trận: {currentRunShards} | Tổng tài khoản: {AccountDatabase.GetAethelShards()}");
        OnShardsChanged?.Invoke(aethelShards);
    }

    public bool SpendShards(long amount)
    {
        if (amount <= 0) return true;
        if (aethelShards >= amount)
        {
            aethelShards -= amount;
            if (IsBattleScene())
            {
                currentRunShards = Math.Max(0, currentRunShards - amount);
                AccountDatabase.AddAethelShards(-amount);
                AccountDatabase.SaveActiveSessionToDatabase();
            }
            else
            {
                AccountDatabase.SetAethelShards(aethelShards);
                AccountDatabase.SaveActiveSessionToDatabase();
            }
            SavePotionsData();
            OnShardsChanged?.Invoke(aethelShards);
            return true;
        }
        return false;
    }

    public long GetShards() => aethelShards;

    public void AddPotions(int count)
    {
        healthPotions += count;
        SaveData();
        Debug.Log($"<color=green>[INVENTORY]</color> Đã lưu +{count} Bình Máu vào túi đồ! Tổng sở hữu: {healthPotions}");
        OnPotionsChanged?.Invoke(healthPotions);
        string potionLabel = LocalizationManager.T("Bình Máu", "Potion");
        DamagePopup.CreateText(transform.position, $"+{count} {potionLabel}", new Color(1f, 0.35f, 0.45f));
    }

    public bool UsePotion()
    {
        if (healthPotions <= 0)
        {
            Debug.Log("<color=yellow>[INVENTORY]</color> Bạn đã hết bình máu!");
            return false;
        }

        var health = GetComponent<PlayerHealth>() ?? FindAnyObjectByType<PlayerHealth>();
        if (health != null)
        {
            if (health.IsDead || health.CurrentHealth <= 0)
            {
                Debug.LogWarning("<color=red>[INVENTORY]</color> Nhân vật đã tử trận, không thể sử dụng bình máu!");
                return false;
            }

            if (health.IsFullHealth())
            {
                Debug.Log("<color=yellow>[INVENTORY]</color> Máu đã đầy, không cần uống!");
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

    public void SaveData()
    {
        // Chỉ ghi đè Aethel Shards tổng của tài khoản khi đang ở menu
        if (!IsBattleScene())
        {
            AccountDatabase.SetAethelShards(aethelShards);
        }
        SavePotionsData();
    }

    public void SavePotionsData()
    {
        PlayerPrefs.SetInt("Save_HealthPotions", healthPotions);
        PlayerPrefs.SetInt("Saved_HealthPotions", healthPotions);
        PlayerPrefs.SetInt("Player_Health_Potions", healthPotions);
        PlayerPrefs.Save();

        // Dong bo vao Database tai khoan hien tai
        AccountDatabase.UpdateCurrentAccountShardsAndPotions(AccountDatabase.GetAethelShards(), healthPotions);
    }

    public void ResetInventory(long shards = 0, int potions = 0)
    {
        aethelShards = shards;
        currentRunShards = shards;
        healthPotions = potions;
        SaveData();
        OnShardsChanged?.Invoke(aethelShards);
        OnPotionsChanged?.Invoke(healthPotions);
        Debug.Log($"<color=red>[INVENTORY RESET]</color> Toan bo Shards va Binh Mau da ve {shards} shards, {potions} potions!");
    }

    public int GetPotions() => healthPotions;

    /// <summary>
    /// Co che Dark Souls: Khi chet va chon Choi lai tu dau map, toan bo so Shards dang mang theo se bi roi tai vi tri tu tran!
    /// Nguoi choi phai quay lai nhat lai; neu chet lan 2 truoc khi nhat, so tien cu se bi de boi luot moi (mat vinh vien)!
    /// </summary>
    public void DropAllShardsOnDeath(Vector3 deathPos, string sceneName)
    {
        long dropped = currentRunShards;
        aethelShards = 0;
        currentRunShards = 0;

        if (dropped > 0)
        {
            AccountDatabase.AddAethelShards(-dropped);
            AccountDatabase.SaveActiveSessionToDatabase();
        }
        SavePotionsData();
        OnShardsChanged?.Invoke(0);

        if (dropped > 0)
        {
            PlayerPrefs.SetInt("Has_Lost_Shards", 1);
            PlayerPrefs.SetString("Lost_Shards_Amount_Long", dropped.ToString());
            PlayerPrefs.SetInt("Lost_Shards_Amount", (int)Math.Min((long)int.MaxValue, dropped));
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
            PlayerPrefs.DeleteKey("Lost_Shards_Amount_Long");
            PlayerPrefs.SetInt("Lost_Shards_Amount", 0);
            PlayerPrefs.Save();
            Debug.Log("<color=red>[DARK SOULS LOST]</color> Chien binh da chet truoc khi kip thu hoi vung hon! Toan bo so Shards cu da tan bien vinh vien!");
        }
    }
}
