using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public static class AccountDatabase
{
    private static readonly string DB_PATH = Path.Combine(Application.persistentDataPath, "vaelstrider_accounts.json");
    private const string PREF_REMEMBER_FLAG = "Save_RememberMe";
    private const string PREF_REMEMBER_USER = "Save_RememberedUser";
    private const string PREF_REMEMBER_PASS = "Save_RememberedPass";
    public const string PREF_CURRENT_USER = "Player_Active_Username";

    [Serializable]
    public class AccountData
    {
        public string username;
        public string passwordHash;
        public string displayName;
        public long aethelShards = 0;
        public int healthPotions = 0;
        public int selectedHeroIndex = 0;
        public string selectedHero = "vael";
        public List<string> unlockedHeroes = new List<string> { "vael" };
        public int equippedArmorId = 0;
        public int equippedHelmetId = 0;
        public int equippedPantsId = 0;
        public int equippedGlovesId = 0;
        public int equippedBootsId = 0;
        public int equippedCloakId = 0;
        public int equippedBagId = 0;
        public string equippedArmor = "Iron Armor";
        public string equippedWeapon = "Steel Blade";
        public int upgradeAttack = 0;
        public int upgradeHealth = 0;
        public int upgradeAgility = 0;
        public int upgradeUltimate = 0;
        public bool map1Completed = false;
        public bool map2Completed = false;
        public bool map3Completed = false;
        public bool unlockedMaid = false;
        public List<int> ownedArmorIds = new List<int>();
        public List<InventoryItemEntry> inventoryItems = new List<InventoryItemEntry>();
        public string createdDate;
        public string lastLoginDate;
    }

    [Serializable]
    public class InventoryItemEntry
    {
        public string itemId;
        public string itemType = "Material"; // "Equipment", "Material", "Consumable", "GachaArtifact"
        public int quantity = 1;
        public string acquiredFrom = "Gameplay"; // "Map1", "Map2", "Map3", "Gacha", "Shop", "Chest"
        public string acquiredDate;
    }

    [Serializable]
    private class DatabaseContainer
    {
        public List<AccountData> accounts = new List<AccountData>();
    }

    private static DatabaseContainer dbCache = null;

    public static string GetDatabaseFilePath() => DB_PATH;

    public static void ClearAllAccounts()
    {
        dbCache = new DatabaseContainer();
        SaveDatabase();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=red>[AccountDB]</color> Toàn bộ tài khoản và PlayerPrefs đã được xóa sạch!");
    }

    private static void EnsureDatabaseLoaded()
    {
        if (dbCache != null) return;

        try
        {
            if (File.Exists(DB_PATH))
            {
                string json = File.ReadAllText(DB_PATH);
                dbCache = JsonUtility.FromJson<DatabaseContainer>(json);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AccountDB] Lỗi đọc database: {ex.Message}");
        }

        if (dbCache == null)
        {
            dbCache = new DatabaseContainer();
        }

        // Tu dong khoi phuc 8 ty Shards cho nguoi choi neu truoc do bi kep ve int.MaxValue (~2.15B)
        if (PlayerPrefs.GetInt("Fix_Restored_8B_Shards_V3", 0) == 0)
        {
            PlayerPrefs.SetInt("Fix_Restored_8B_Shards_V3", 1);
            bool updated = false;
            foreach (var acc in dbCache.accounts)
            {
                if (acc.aethelShards >= 2140000000L && acc.aethelShards <= 2147483647L)
                {
                    acc.aethelShards = 8000000000L;
                    updated = true;
                }
                else if (string.Equals(acc.username, "WANDERER", StringComparison.OrdinalIgnoreCase) && acc.aethelShards < 8000000000L)
                {
                    acc.aethelShards = 8000000000L;
                    updated = true;
                }
            }
            if (updated)
            {
                SaveDatabase();
                SetAethelShards(8000000000L);
                Debug.Log("<color=green>[AccountDB]</color> Đã tự động khôi phục 8,000,000,000 (8B) Aethel Shards!");
            }
        }
    }

    public static void SaveDatabase()
    {
        try
        {
            if (dbCache == null) dbCache = new DatabaseContainer();
            string json = JsonUtility.ToJson(dbCache, true);
            File.WriteAllText(DB_PATH, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AccountDB] Lỗi lưu database: {ex.Message}");
        }
    }

    public static string HashPassword(string password)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "_vaelstrider_salt"));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Đăng ký tài khoản mới vào Database (Đồng bộ cả Local và Cloud)
    /// </summary>
    public static bool Register(string username, string password, out string errorMessage)
    {
        errorMessage = "";
        EnsureDatabaseLoaded();

        if (string.IsNullOrWhiteSpace(username) || username.Length < 2)
        {
            errorMessage = "Tên đăng nhập phải có ít nhất 2 ký tự!";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 2)
        {
            errorMessage = "Mật khẩu phải có ít nhất 2 ký tự!";
            return false;
        }

        string cleanUser = username.Trim();

        foreach (var acc in dbCache.accounts)
        {
            if (string.Equals(acc.username, cleanUser, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Tài khoản '{cleanUser}' đã tồn tại. Vui lòng đăng nhập hoặc chọn tên khác!";
                return false;
            }
        }

        var newAcc = new AccountData
        {
            username = cleanUser,
            passwordHash = HashPassword(password),
            displayName = cleanUser,
            aethelShards = 0, // Bắt đầu từ con số 0
            healthPotions = 0, // Không có bình máu ban đầu
            selectedHeroIndex = 0,
            selectedHero = "vael",
            unlockedHeroes = new List<string> { "vael" },
            equippedArmor = "Iron Armor",
            equippedWeapon = "Steel Blade",
            equippedArmorId = 0,
            equippedHelmetId = 0,
            equippedPantsId = 0,
            equippedGlovesId = 0,
            equippedBootsId = 0,
            equippedCloakId = 0,
            equippedBagId = 0,
            upgradeAttack = 0,
            upgradeHealth = 0,
            upgradeAgility = 0,
            upgradeUltimate = 0,
            map1Completed = false,
            map2Completed = false,
            map3Completed = false,
            unlockedMaid = false,
            ownedArmorIds = new List<int>(),
            createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        dbCache.accounts.Add(newAcc);
        SaveDatabase();

        // Đồng bộ lên Cloud Server
        if (CloudAccountService.Instance != null)
        {
            CloudAccountService.Instance.RegisterAccountCloud(newAcc, (success, msg) => { });
        }

        Debug.Log($"<color=green>[AccountDB]</color> Đã tạo mới tài khoản '{newAcc.username}' (Đồng bộ Local & Cloud)");
        return true;
    }

    /// <summary>
    /// Đảm bảo dữ liệu của tài khoản hiện tại được nạp đầy đủ từ Database khi chuyển scene
    /// </summary>
    public static void EnsureActiveSessionLoaded()
    {
        EnsureDatabaseLoaded();
        string activeUser = PlayerPrefs.GetString(PREF_CURRENT_USER, "");
        if (string.IsNullOrEmpty(activeUser)) return;

        foreach (var acc in dbCache.accounts)
        {
            if (string.Equals(acc.username, activeUser, StringComparison.OrdinalIgnoreCase))
            {
                ApplyAccountToGameSession(acc, false);
                return;
            }
        }
    }

    /// <summary>
    /// Đồng bộ và nạp chính xác toàn bộ dữ liệu của tài khoản vào Game Session (PlayerPrefs)
    /// Xóa hoàn toàn rác và tiến trình của tài khoản trước đó để phân tách 100%!
    /// </summary>
    public static void ApplyAccountToGameSession(AccountData acc, bool savePrevious = true)
    {
        if (acc == null) return;

        // 1. Lưu lại tài khoản trước đó nếu là đổi tài khoản khác
        if (savePrevious)
        {
            string previousUser = PlayerPrefs.GetString(PREF_CURRENT_USER, "");
            if (!string.IsNullOrEmpty(previousUser) && !string.Equals(previousUser, acc.username, StringComparison.OrdinalIgnoreCase))
            {
                SaveActiveSessionToDatabase();
            }
        }

        // 2. Xóa sạch các key gameplay cũ trong PlayerPrefs để tránh nhiễm dữ liệu từ tài khoản trước
        ClearTransientGameplayPlayerPrefs();

        // 3. Nạp dữ liệu mới của tài khoản này
        PlayerPrefs.SetString(PREF_CURRENT_USER, acc.username);
        PlayerPrefs.SetString("Player_Name", acc.displayName);

        // Tiền & Bình máu
        SetAethelShards(acc.aethelShards);

        PlayerPrefs.SetInt("Save_HealthPotions", acc.healthPotions);
        PlayerPrefs.SetInt("Saved_HealthPotions", acc.healthPotions);
        PlayerPrefs.SetInt("Player_Health_Potions", acc.healthPotions);

        // Cấp nâng cấp Đền Thờ
        PlayerPrefs.SetInt("Upgrade_Attack_Level", acc.upgradeAttack);
        PlayerPrefs.SetInt("Upgrade_Health_Level", acc.upgradeHealth);
        PlayerPrefs.SetInt("Upgrade_Agility_Level", acc.upgradeAgility);
        PlayerPrefs.SetInt("Upgrade_Ultimate_Level", acc.upgradeUltimate);

        // Bản đồ đã hoàn thành
        PlayerPrefs.SetInt("Map1_Completed", acc.map1Completed ? 1 : 0);
        PlayerPrefs.SetInt("Map2_Completed", acc.map2Completed ? 1 : 0);
        PlayerPrefs.SetInt("Map3_Completed", acc.map3Completed ? 1 : 0);

        // Nhân vật đã chọn
        PlayerPrefs.SetInt("Char_Unlocked_Maid", acc.unlockedMaid ? 1 : 0);
        string hero = acc.selectedHero;
        string charId = CharacterManager.CHAR_VAELSTRIDER;
        if (!string.IsNullOrEmpty(hero))
        {
            if (hero.Equals("titan", StringComparison.OrdinalIgnoreCase) ||
                hero.Equals("titanberserker", StringComparison.OrdinalIgnoreCase) ||
                hero.Equals(CharacterManager.CHAR_TITAN_BERSERKER, StringComparison.OrdinalIgnoreCase))
            {
                charId = CharacterManager.CHAR_TITAN_BERSERKER;
            }
            else if (hero.Equals("azure", StringComparison.OrdinalIgnoreCase) || 
                hero.Equals("azureblade", StringComparison.OrdinalIgnoreCase) || 
                hero.Equals(CharacterManager.CHAR_AZURE_BLADE, StringComparison.OrdinalIgnoreCase))
            {
                charId = CharacterManager.CHAR_AZURE_BLADE;
            }
            else if (hero.Equals("ronin", StringComparison.OrdinalIgnoreCase) ||
                     hero.Equals("galeronin", StringComparison.OrdinalIgnoreCase) ||
                     hero.Equals("maid", StringComparison.OrdinalIgnoreCase) || 
                     hero.Equals(CharacterManager.CHAR_MAID, StringComparison.OrdinalIgnoreCase))
            {
                charId = CharacterManager.CHAR_GALE_RONIN;
            }
        }
        PlayerPrefs.SetString("SelectedHero", (charId == CharacterManager.CHAR_TITAN_BERSERKER) ? "titan" : ((charId == CharacterManager.CHAR_AZURE_BLADE) ? "azure" : (charId == CharacterManager.CHAR_GALE_RONIN || charId == CharacterManager.CHAR_MAID ? "ronin" : "vael")));
        PlayerPrefs.SetString("Selected_Character_ID", charId);
        PlayerPrefs.SetInt("Save_SelectedHeroIndex", acc.selectedHeroIndex);

        // Trang bị giáp & phụ kiện
        PlayerPrefs.SetInt("EquippedArmorId", acc.equippedArmorId);
        PlayerPrefs.SetInt("Equipped_Slot_Helmet", acc.equippedHelmetId);
        PlayerPrefs.SetInt("Equipped_Slot_Armor", acc.equippedArmorId);
        PlayerPrefs.SetInt("Equipped_Slot_Pants", acc.equippedPantsId);
        PlayerPrefs.SetInt("Equipped_Slot_Gloves", acc.equippedGlovesId);
        PlayerPrefs.SetInt("Equipped_Slot_Boots", acc.equippedBootsId);
        PlayerPrefs.SetInt("Equipped_Slot_Cloak", acc.equippedCloakId);
        PlayerPrefs.SetInt("Equipped_Slot_Bag", acc.equippedBagId);

        PlayerPrefs.SetString("Save_EquippedArmor", acc.equippedArmor);
        PlayerPrefs.SetString("Save_EquippedWeapon", acc.equippedWeapon);
        PlayerPrefs.SetString("Player_Account_Type", "Cloud_DB");

        if (acc.ownedArmorIds != null)
        {
            foreach (var armorId in acc.ownedArmorIds)
            {
                PlayerPrefs.SetInt("Owned_Armor_" + armorId, 1);
            }
        }

        PlayerPrefs.Save();
        Debug.Log($"<color=green>[AccountDB]</color> Đã nạp thành công dữ liệu tài khoản '{acc.username}' (Shards={acc.aethelShards}, Potions={acc.healthPotions}, AtkLv={acc.upgradeAttack}, Map1={acc.map1Completed})");
    }

    /// <summary>
    /// Xóa toàn bộ PlayerPrefs về trạng thái trống (trừ âm thanh và thông tin ghi nhớ)
    /// </summary>
    private static void ClearTransientGameplayPlayerPrefs()
    {
        // Shards & Potions
        PlayerPrefs.DeleteKey("Save_AethelShards_Long");
        PlayerPrefs.DeleteKey("Save_AethelShards");
        PlayerPrefs.DeleteKey("Saved_AethelShards");
        PlayerPrefs.DeleteKey("AethelShards");
        PlayerPrefs.DeleteKey("Save_HealthPotions");
        PlayerPrefs.DeleteKey("Saved_HealthPotions");
        PlayerPrefs.DeleteKey("Player_Health_Potions");
        PlayerPrefs.DeleteKey("Saved_CurrentHealth");

        // Upgrades
        PlayerPrefs.DeleteKey("Upgrade_Attack_Level");
        PlayerPrefs.DeleteKey("Upgrade_Health_Level");
        PlayerPrefs.DeleteKey("Upgrade_Agility_Level");
        PlayerPrefs.DeleteKey("Upgrade_Ultimate_Level");

        // Maps
        PlayerPrefs.DeleteKey("Map1_Completed");
        PlayerPrefs.DeleteKey("Map2_Completed");
        PlayerPrefs.DeleteKey("Map3_Completed");

        // Heroes
        PlayerPrefs.DeleteKey("Char_Unlocked_Maid");
        PlayerPrefs.DeleteKey("SelectedHero");
        PlayerPrefs.DeleteKey("Selected_Character_ID");
        PlayerPrefs.DeleteKey("Save_SelectedHeroIndex");

        // Equipment
        PlayerPrefs.DeleteKey("EquippedArmorId");
        PlayerPrefs.DeleteKey("Equipped_Slot_Helmet");
        PlayerPrefs.DeleteKey("Equipped_Slot_Armor");
        PlayerPrefs.DeleteKey("Equipped_Slot_Pants");
        PlayerPrefs.DeleteKey("Equipped_Slot_Gloves");
        PlayerPrefs.DeleteKey("Equipped_Slot_Boots");
        PlayerPrefs.DeleteKey("Equipped_Slot_Cloak");
        PlayerPrefs.DeleteKey("Equipped_Slot_Bag");
        PlayerPrefs.DeleteKey("Save_EquippedArmor");
        PlayerPrefs.DeleteKey("Save_EquippedWeapon");

        // Dark Souls Lost Shards
        PlayerPrefs.DeleteKey("Has_Lost_Shards");
        PlayerPrefs.DeleteKey("Lost_Shards_Amount");
        PlayerPrefs.DeleteKey("Lost_Shards_X");
        PlayerPrefs.DeleteKey("Lost_Shards_Y");
        PlayerPrefs.DeleteKey("Lost_Shards_Scene");
        PlayerPrefs.DeleteKey("Save_LostShards");
        PlayerPrefs.DeleteKey("Save_DeathPosX");
        PlayerPrefs.DeleteKey("Save_DeathPosY");
        PlayerPrefs.DeleteKey("Save_DeathPosZ");
        PlayerPrefs.DeleteKey("Save_DeathScene");
    }

    public static long GetAethelShards()
    {
        string longStr = PlayerPrefs.GetString("Save_AethelShards_Long", "");
        if (!string.IsNullOrEmpty(longStr) && long.TryParse(longStr, out long val))
        {
            return val;
        }

        // Kiem tra truc tiep tu active account trong dbCache neu chua co key PlayerPrefs
        EnsureDatabaseLoaded();
        if (dbCache != null && dbCache.accounts != null)
        {
            string activeUser = PlayerPrefs.GetString(PREF_CURRENT_USER, "");
            foreach (var acc in dbCache.accounts)
            {
                if (string.Equals(acc.username, activeUser, StringComparison.OrdinalIgnoreCase))
                {
                    if (acc.aethelShards > 0)
                    {
                        PlayerPrefs.SetString("Save_AethelShards_Long", acc.aethelShards.ToString());
                        PlayerPrefs.Save();
                        return acc.aethelShards;
                    }
                }
            }
        }

        long fallback = (long)PlayerPrefs.GetInt("Save_AethelShards", PlayerPrefs.GetInt("Saved_AethelShards", PlayerPrefs.GetInt("AethelShards", 0)));
        return fallback;
    }

    public static void SetAethelShards(long amount)
    {
        if (amount < 0) amount = 0;
        PlayerPrefs.SetString("Save_AethelShards_Long", amount.ToString());
        int clamped = (amount > int.MaxValue) ? int.MaxValue : (int)amount;
        PlayerPrefs.SetInt("Save_AethelShards", clamped);
        PlayerPrefs.SetInt("Saved_AethelShards", clamped);
        PlayerPrefs.SetInt("AethelShards", clamped);
        PlayerPrefs.Save();
    }

    public static void AddAethelShards(long delta)
    {
        if (delta <= 0) return;
        long current = GetAethelShards();
        SetAethelShards(current + delta);
    }

    public static bool SpendAethelShards(long cost)
    {
        if (cost < 0) cost = 0;
        long current = GetAethelShards();
        if (current >= cost)
        {
            SetAethelShards(current - cost);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Lưu tiến trình hiện tại từ game session (PlayerPrefs) vào Database file & Cloud
    /// </summary>
    public static void SaveActiveSessionToDatabase()
    {
        EnsureDatabaseLoaded();
        string activeUser = PlayerPrefs.GetString(PREF_CURRENT_USER, "");
        if (string.IsNullOrEmpty(activeUser)) return;

        foreach (var acc in dbCache.accounts)
        {
            if (string.Equals(acc.username, activeUser, StringComparison.OrdinalIgnoreCase))
            {
                acc.aethelShards = GetAethelShards();
                acc.healthPotions = PlayerPrefs.GetInt("Save_HealthPotions", PlayerPrefs.GetInt("Saved_HealthPotions", acc.healthPotions));
                acc.upgradeAttack = PlayerPrefs.GetInt("Upgrade_Attack_Level", 0);
                acc.upgradeHealth = PlayerPrefs.GetInt("Upgrade_Health_Level", 0);
                acc.upgradeAgility = PlayerPrefs.GetInt("Upgrade_Agility_Level", 0);
                acc.upgradeUltimate = PlayerPrefs.GetInt("Upgrade_Ultimate_Level", 0);
                acc.map1Completed = PlayerPrefs.GetInt("Map1_Completed", 0) == 1;
                acc.map2Completed = PlayerPrefs.GetInt("Map2_Completed", 0) == 1;
                acc.map3Completed = PlayerPrefs.GetInt("Map3_Completed", 0) == 1;
                acc.unlockedMaid = PlayerPrefs.GetInt("Char_Unlocked_Maid", 0) == 1;
                string activeChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
                acc.selectedHero = (activeChar == CharacterManager.CHAR_TITAN_BERSERKER) ? "titan" : ((activeChar == CharacterManager.CHAR_AZURE_BLADE) ? "azure" : (activeChar == CharacterManager.CHAR_GALE_RONIN || activeChar == CharacterManager.CHAR_MAID ? "ronin" : "vael"));
                acc.selectedHeroIndex = (activeChar == CharacterManager.CHAR_TITAN_BERSERKER) ? 3 : ((activeChar == CharacterManager.CHAR_AZURE_BLADE) ? 2 : (activeChar == CharacterManager.CHAR_GALE_RONIN || activeChar == CharacterManager.CHAR_MAID ? 1 : 0));
                acc.equippedArmorId = PlayerPrefs.GetInt("EquippedArmorId", 0);
                acc.equippedHelmetId = PlayerPrefs.GetInt("Equipped_Slot_Helmet", 0);
                acc.equippedPantsId = PlayerPrefs.GetInt("Equipped_Slot_Pants", 0);
                acc.equippedGlovesId = PlayerPrefs.GetInt("Equipped_Slot_Gloves", 0);
                acc.equippedBootsId = PlayerPrefs.GetInt("Equipped_Slot_Boots", 0);
                acc.equippedCloakId = PlayerPrefs.GetInt("Equipped_Slot_Cloak", 0);
                acc.equippedBagId = PlayerPrefs.GetInt("Equipped_Slot_Bag", 0);
                acc.lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                SaveDatabase();

                // Đồng bộ lên Cloud Server
                if (CloudAccountService.Instance != null)
                {
                    CloudAccountService.Instance.SyncProgressToCloud(acc.username, acc.aethelShards, acc.healthPotions, acc.equippedArmor, acc.equippedWeapon);
                }
                return;
            }
        }
    }

    /// <summary>
    /// Lưu hoặc cập nhật tài khoản tải về từ Cloud vào Local Database
    /// </summary>
    public static void SaveOrUpdateCloudAccount(AccountData cloudAcc)
    {
        EnsureDatabaseLoaded();
        bool found = false;
        for (int i = 0; i < dbCache.accounts.Count; i++)
        {
            if (string.Equals(dbCache.accounts[i].username, cloudAcc.username, StringComparison.OrdinalIgnoreCase))
            {
                dbCache.accounts[i] = cloudAcc;
                found = true;
                break;
            }
        }
        if (!found)
        {
            dbCache.accounts.Add(cloudAcc);
        }
        SaveDatabase();

        ApplyAccountToGameSession(cloudAcc);
    }

    /// <summary>
    /// Đăng nhập tài khoản từ Local Database
    /// </summary>
    public static bool Login(string username, string password, out AccountData account, out string errorMessage)
    {
        account = null;
        errorMessage = "";
        EnsureDatabaseLoaded();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!";
            return false;
        }

        string cleanUser = username.Trim();
        string inputHash = HashPassword(password);

        foreach (var acc in dbCache.accounts)
        {
            if (string.Equals(acc.username, cleanUser, StringComparison.OrdinalIgnoreCase))
            {
                if (acc.passwordHash == inputHash)
                {
                    acc.lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    SaveDatabase();

                    ApplyAccountToGameSession(acc);

                    account = acc;
                    Debug.Log($"<color=green>[AccountDB]</color> Đăng nhập '{acc.username}' thành công! Linh hồn = {acc.aethelShards}, Bình máu = {acc.healthPotions}");
                    return true;
                }
                else
                {
                    errorMessage = "Mật khẩu không chính xác. Vui lòng thử lại!";
                    return false;
                }
            }
        }

        errorMessage = $"Tài khoản '{cleanUser}' chưa tồn tại trên máy!";
        return false;
    }

    /// <summary>
    /// Cập nhật dữ liệu Linh hồn & Bình máu của tài khoản hiện tại vào Database và Cloud
    /// </summary>
    public static void UpdateCurrentAccountShardsAndPotions(long shards, int potions)
    {
        SetAethelShards(shards);
        SaveActiveSessionToDatabase();
    }

    public static void UpdateCurrentAccountShardsAndPotions(int shards, int potions)
    {
        UpdateCurrentAccountShardsAndPotions((long)shards, potions);
    }

    public static void SetRememberMe(string username, string password, bool remember)
    {
        PlayerPrefs.SetInt(PREF_REMEMBER_FLAG, remember ? 1 : 0);
        if (remember)
        {
            PlayerPrefs.SetString(PREF_REMEMBER_USER, username);
            PlayerPrefs.SetString(PREF_REMEMBER_PASS, password);
        }
        else
        {
            PlayerPrefs.DeleteKey(PREF_REMEMBER_USER);
            PlayerPrefs.DeleteKey(PREF_REMEMBER_PASS);
        }
        PlayerPrefs.Save();
    }

    public static bool GetRememberedCredentials(out string username, out string password)
    {
        username = "";
        password = "";
        if (PlayerPrefs.GetInt(PREF_REMEMBER_FLAG, 0) == 1)
        {
            username = PlayerPrefs.GetString(PREF_REMEMBER_USER, "");
            password = PlayerPrefs.GetString(PREF_REMEMBER_PASS, "");
            return !string.IsNullOrEmpty(username);
        }
        return false;
    }

    // ==========================================
    // INVENTORY MANAGEMENT API
    // ==========================================

    public static AccountData GetOrCreateActiveAccount()
    {
        EnsureDatabaseLoaded();
        string activeUser = PlayerPrefs.GetString(PREF_CURRENT_USER, "");
        if (string.IsNullOrEmpty(activeUser))
        {
            activeUser = PlayerPrefs.GetString("Player_Name", "WANDERER");
            PlayerPrefs.SetString(PREF_CURRENT_USER, activeUser);
            PlayerPrefs.Save();
        }

        foreach (var acc in dbCache.accounts)
        {
            if (string.Equals(acc.username, activeUser, StringComparison.OrdinalIgnoreCase))
            {
                if (acc.inventoryItems == null) acc.inventoryItems = new List<InventoryItemEntry>();
                return acc;
            }
        }

        // Tạo tài khoản mặc định nếu chưa có trong DB để đảm bảo vật phẩm luôn được lưu 100%
        AccountData newAcc = new AccountData
        {
            username = activeUser,
            displayName = activeUser,
            passwordHash = "",
            aethelShards = GetAethelShards(),
            healthPotions = PlayerPrefs.GetInt("Save_HealthPotions", 0),
            createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            inventoryItems = new List<InventoryItemEntry>()
        };
        dbCache.accounts.Add(newAcc);
        SaveDatabase();
        return newAcc;
    }

    public static List<InventoryItemEntry> GetActiveUserInventory()
    {
        var acc = GetOrCreateActiveAccount();
        if (acc != null)
        {
            if (acc.inventoryItems == null) acc.inventoryItems = new List<InventoryItemEntry>();
            
            // Clean up any legacy consumable potions so bag only contains dismantlable / socketable items
            int removed = acc.inventoryItems.RemoveAll(i => i.itemId != null && i.itemId.StartsWith("potion_"));
            if (removed > 0)
            {
                SaveDatabase();
            }

            return acc.inventoryItems;
        }
        return new List<InventoryItemEntry>();
    }

    public static void AddItemToActiveUser(string itemId, int quantity = 1, string itemType = "Material", string acquiredFrom = "Gameplay")
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;
        var acc = GetOrCreateActiveAccount();
        if (acc == null) return;

        if (acc.inventoryItems == null) acc.inventoryItems = new List<InventoryItemEntry>();

        var existing = acc.inventoryItems.Find(i => i.itemId == itemId);
        if (existing != null)
        {
            existing.quantity += quantity;
        }
        else
        {
            acc.inventoryItems.Add(new InventoryItemEntry
            {
                itemId = itemId,
                itemType = itemType,
                quantity = quantity,
                acquiredFrom = acquiredFrom,
                acquiredDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        SaveDatabase();
        Debug.Log($"<color=green>[InventoryDB]</color> Đã thêm +{quantity} '{itemId}' ({acquiredFrom}) vào túi đồ tài khoản '{acc.username}'!");
    }

    public static bool RemoveItemFromActiveUser(string itemId, int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
        var acc = GetOrCreateActiveAccount();
        if (acc == null || acc.inventoryItems == null) return false;

        var existing = acc.inventoryItems.Find(i => i.itemId == itemId);
        if (existing != null && existing.quantity >= quantity)
        {
            existing.quantity -= quantity;
            if (existing.quantity <= 0)
            {
                acc.inventoryItems.Remove(existing);
            }
            SaveDatabase();
            Debug.Log($"<color=yellow>[InventoryDB]</color> Đã sử dụng/trừ -{quantity} '{itemId}' khỏi túi đồ tài khoản '{acc.username}'!");
            return true;
        }
        return false;
    }

    public static int GetActiveUserItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        var acc = GetOrCreateActiveAccount();
        if (acc == null || acc.inventoryItems == null) return 0;

        var existing = acc.inventoryItems.Find(i => i.itemId == itemId);
        return existing != null ? existing.quantity : 0;
    }
}
