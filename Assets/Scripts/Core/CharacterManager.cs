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

    private Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();

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

    public static CharacterManager EnsureInstance()
    {
        if (Instance == null)
        {
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

        // Mặc định Vaelstrider luôn mở khóa
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_VAELSTRIDER, 1);
        PlayerPrefs.Save();
    }

    public string GetSelectedCharacterId()
    {
        string selected = PlayerPrefs.GetString(SELECTED_CHAR_KEY, CHAR_VAELSTRIDER);
        if (selected == CHAR_MAID)
        {
            // Tam khoa nhan vat so 2, tu dong chuyen ve Vaelstrider
            selected = CHAR_VAELSTRIDER;
            PlayerPrefs.SetString(SELECTED_CHAR_KEY, CHAR_VAELSTRIDER);
            PlayerPrefs.Save();
        }
        return selected;
    }

    public CharacterData GetSelectedCharacter()
    {
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
            // Tam khoa nhan vat so 2 (Maid)
            return false;
        }
        return true;
    }

    public bool UnlockCharacter(string id)
    {
        if (id == CHAR_MAID)
        {
            Debug.LogWarning("<color=yellow>[CHARACTER]</color> Nhân vật Maid hiện đang tạm khóa!");
            return false;
        }
        return true;
    }

    public void SelectCharacter(string id)
    {
        if (id == CHAR_MAID)
        {
            Debug.LogWarning("<color=yellow>[CHARACTER]</color> Nhân vật Maid hiện đang tạm khóa, giữ nguyên Vaelstrider!");
            PlayerPrefs.SetString(SELECTED_CHAR_KEY, CHAR_VAELSTRIDER);
            PlayerPrefs.Save();
            return;
        }

        if (IsCharacterUnlocked(id))
        {
            PlayerPrefs.SetString(SELECTED_CHAR_KEY, id);
            PlayerPrefs.Save();
            Debug.Log($"<color=cyan>[CHARACTER]</color> Đã chọn nhân vật: {id}");
        }
    }
}
