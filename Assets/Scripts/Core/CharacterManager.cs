using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class CharacterData
{
    public string id;
    public string nameVN;
    public string nameEN;
    public string roleVN;
    public string roleEN;
    public string skillNameVN;
    public string skillNameEN;
    public string skillDescVN;
    public string skillDescEN;
    public int baseHealth;
    public int baseAttack;
    public float baseSpeed;
    public int unlockPrice;
    public string controllerPath;
    public string spriteSheetPath;
}

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    private const string SELECTED_CHAR_KEY = "Selected_Character_ID";
    private const string UNLOCKED_PREFIX = "Char_Unlocked_";

    public const string CHAR_VAELSTRIDER = "Vaelstrider";
    public const string CHAR_GALE_RONIN = "GaleRonin";
    public const string CHAR_MAID = "GaleRonin"; // Backward compatible alias
    public const string CHAR_AZURE_BLADE = "AzureBlade";

    private Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();

    private static bool isQuitting = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCharacters();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    public static CharacterManager EnsureInstance()
    {
        if (isQuitting) return Instance;

        if (Instance == null)
        {
            var existing = FindAnyObjectByType<CharacterManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("CharacterManager");
            Instance = go.AddComponent<CharacterManager>();
            DontDestroyOnLoad(go);
            Instance.InitializeCharacters();
        }
        return Instance;
    }

    private void InitializeCharacters()
    {
        characters.Clear();

        // 1. Vaelstrider
        characters[CHAR_VAELSTRIDER] = new CharacterData
        {
            id = CHAR_VAELSTRIDER,
            nameVN = "Vaelstrider",
            nameEN = "Vaelstrider",
            roleVN = "Hiệp Sĩ Tàn Tích (Cân Bằng)",
            roleEN = "Ruin Knight (Balanced)",
            skillNameVN = "Bão Lốc Tàn Tích",
            skillNameEN = "Ruin Whirlwind",
            skillDescVN = "Xoay kiếm 360 độ hút quái và tung đòn kết liễu hất văng.",
            skillDescEN = "Spins 360 degrees pulling enemies, concluding with an explosive blow.",
            baseHealth = 100,
            baseAttack = 5,
            baseSpeed = 5.0f,
            unlockPrice = 0,
            controllerPath = "Assets/Animation/Player/idle_down_0.controller",
            spriteSheetPath = "Assets/ART/Player/IDLE/idle_down.png"
        };

        // 2. Phong Lôi Lãng Khách (Player 2)
        characters[CHAR_GALE_RONIN] = new CharacterData
        {
            id = CHAR_GALE_RONIN,
            nameVN = "Phong Lôi Lãng Khách",
            nameEN = "Gale Ronin",
            roleVN = "Bạch Dạ Kiếm Khách (Phong Ấn & Ảo Ảnh)",
            roleEN = "Phantom Blade (Wind Mark & Phantom Cleave)",
            skillNameVN = "Ám Ảnh Trảm",
            skillNameEN = "Shadow Strike",
            skillDescVN = "Đòn chém bán nguyệt quét rộng 180 độ tích tầng Phong Ấn. Tuyệt kỹ dịch chuyển tức thời ra sau lưng kẻ địch, tung đòn chém chí mạng kinh thiên động địa rồi thoắt ẩn thoắt hiện trở về vị trí cũ!",
            skillDescEN = "Wide 180° sweeps applying Wind Marks. Special skill teleports behind the target's back, delivers a devastating decisive execution slash, and seamlessly returns to original position!",
            baseHealth = 95,
            baseAttack = 6,
            baseSpeed = 5.4f,
            unlockPrice = 0, // Mở khóa sẵn sàng chơi
            controllerPath = "Assets/Animation/Player2/Player2.controller",
            spriteSheetPath = "Assets/ART/Player2/_down idle.png"
        };

        // 3. Lam Kiếm Sĩ (Azure Blade)
        characters[CHAR_AZURE_BLADE] = new CharacterData
        {
            id = CHAR_AZURE_BLADE,
            nameVN = "Lam Kiếm Sĩ",
            nameEN = "Azure Blade",
            roleVN = "Kiếm Khí Trọng Kiếm (Tầm Xa & Sóng Kiếm)",
            roleEN = "Heavy Crescent Blade (Ranged Waves)",
            skillNameVN = "Bão Sóng Kiếm",
            skillNameEN = "Crescent Tempest",
            skillDescVN = "Đòn chém chậm nhưng uy lực, mỗi nhát chém phóng ra sóng kiếm xuyên thấu quái vật. Tuyệt kỹ phóng ra 5 đợt kiếm khí hình nan quạt quét sạch chiến trường!",
            skillDescEN = "Heavy slashes that unleash piercing sword waves. Special skill releases 5 massive crescent waves in a fan spread!",
            baseHealth = 110,
            baseAttack = 6,
            baseSpeed = 4.8f,
            unlockPrice = 0, // Mở khóa sẵn sàng chơi
            controllerPath = "Assets/Animation/Player3/Player3.controller",
            spriteSheetPath = "Assets/ART/Player3/Sword_Idle.png"
        };

        // Mặc định mở khóa
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_VAELSTRIDER, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_GALE_RONIN, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_AZURE_BLADE, 1);
        PlayerPrefs.Save();
    }

    public string GetSelectedCharacterId()
    {
        string selected = PlayerPrefs.GetString(SELECTED_CHAR_KEY, "");
        if (string.IsNullOrEmpty(selected))
        {
            selected = PlayerPrefs.GetString("SelectedHero", CHAR_AZURE_BLADE);
        }

        if (string.Equals(selected, "azure", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(selected, "azureblade", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(selected, CHAR_AZURE_BLADE, StringComparison.OrdinalIgnoreCase))
        {
            return CHAR_AZURE_BLADE;
        }
        else if (string.Equals(selected, "ronin", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, "galeronin", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, "player2", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(selected, "maid", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, CHAR_GALE_RONIN, StringComparison.OrdinalIgnoreCase))
        {
            return CHAR_GALE_RONIN;
        }

        return CHAR_VAELSTRIDER;
    }

    public CharacterData GetSelectedCharacter()
    {
        string id = GetSelectedCharacterId();
        if (characters.TryGetValue(id, out var data)) return data;
        return characters[CHAR_VAELSTRIDER];
    }

    public CharacterData GetCharacter(string id)
    {
        if (characters.TryGetValue(id, out var data)) return data;
        return null;
    }

    public bool IsCharacterUnlocked(string id)
    {
        return PlayerPrefs.GetInt(UNLOCKED_PREFIX + id, 0) == 1 || id == CHAR_VAELSTRIDER || id == CHAR_GALE_RONIN || id == CHAR_AZURE_BLADE;
    }

    public bool UnlockCharacter(string id)
    {
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + id, 1);
        PlayerPrefs.Save();
        return true;
    }

    public void SelectCharacter(string id)
    {
        string normId = CHAR_VAELSTRIDER;
        if (string.Equals(id, "azure", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(id, "azureblade", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(id, CHAR_AZURE_BLADE, StringComparison.OrdinalIgnoreCase))
        {
            normId = CHAR_AZURE_BLADE;
        }
        else if (string.Equals(id, "ronin", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, "galeronin", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, "player2", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(id, "maid", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, CHAR_GALE_RONIN, StringComparison.OrdinalIgnoreCase))
        {
            normId = CHAR_GALE_RONIN;
        }

        PlayerPrefs.SetString(SELECTED_CHAR_KEY, normId);
        PlayerPrefs.SetString("SelectedHero", (normId == CHAR_AZURE_BLADE) ? "azure" : (normId == CHAR_GALE_RONIN ? "ronin" : "vael"));
        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[CHARACTER]</color> Đã chọn nhân vật: {normId}");
    }
}
