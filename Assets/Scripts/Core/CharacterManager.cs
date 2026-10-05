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
    public const string CHAR_MAID = "Maid";
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

        // 2. Maid Chiến Binh
        characters[CHAR_MAID] = new CharacterData
        {
            id = CHAR_MAID,
            nameVN = "Maid Chiến Binh",
            nameEN = "Battle Maid",
            roleVN = "Sát Thủ Hầu Gái (Tốc Độ & Bạo Kích)",
            roleEN = "Maid Assassin (Speed & Crit)",
            skillNameVN = "Quét Tan Kẻ Yếu",
            skillNameEN = "Sweep the Weak",
            skillDescVN = "Vung nhát chém hình nón hủy diệt: Tiêu diệt TỨC THÌ mọi quái thường, gây choáng và sát thương cực lớn lên Boss!",
            skillDescEN = "Unleashes a devastating cone cleave: INSTANTLY EXECUTES normal monsters, deals massive damage & stuns Bosses!",
            baseHealth = 85,
            baseAttack = 5,
            baseSpeed = 5.8f,
            unlockPrice = 999,
            controllerPath = "Assets/Animation/Maid/Maid.controller",
            spriteSheetPath = "Assets/ART/Player1/Maid Idle.png"
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
        else if (string.Equals(selected, "maid", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, CHAR_MAID, StringComparison.OrdinalIgnoreCase))
        {
            return CHAR_AZURE_BLADE; // Since maid is locked
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
        if (id == CHAR_MAID)
        {
            return false;
        }
        return PlayerPrefs.GetInt(UNLOCKED_PREFIX + id, 0) == 1 || id == CHAR_VAELSTRIDER || id == CHAR_AZURE_BLADE;
    }

    public bool UnlockCharacter(string id)
    {
        if (id == CHAR_MAID)
        {
            Debug.LogWarning("<color=yellow>[CHARACTER]</color> Nhân vật Maid hiện đang tạm khóa!");
            return false;
        }
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
        else if (string.Equals(id, "maid", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, CHAR_MAID, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("<color=yellow>[CHARACTER]</color> Nhân vật Maid hiện đang tạm khóa!");
            return;
        }

        PlayerPrefs.SetString(SELECTED_CHAR_KEY, normId);
        PlayerPrefs.SetString("SelectedHero", (normId == CHAR_AZURE_BLADE) ? "azure" : "vael");
        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[CHARACTER]</color> Đã chọn nhân vật: {normId}");
    }
}
