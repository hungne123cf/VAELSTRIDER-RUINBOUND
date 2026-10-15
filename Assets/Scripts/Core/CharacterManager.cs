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
    public const string CHAR_TITAN_BERSERKER = "TitanBerserker";
    public const string CHAR_DUNE_ASSASSIN = "DuneAssassin";
    public const string CHAR_CELESTIAL_MAGE = "CelestialMage";

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
            roleVN = "Hiệp Sĩ Tàn Tích / Cân Bằng",
            roleEN = "Ruin Knight / Balanced",
            skillNameVN = "Bão Lốc Tàn Tích",
            skillNameEN = "Ruin Whirlwind",
            skillDescVN = "Combo kiếm thuật hoàng kim. Nhấn Giữ vào Thế Thủ Kiếm Thuẫn: Đỡ đòn hoàn hảo chặn đứng sát thương và tung đòn Phản Kích Hoàng Kim x3.0 sát thương bạo kích! Tuyệt kỹ xoay kiếm 360 độ hút quái và tung đòn kết liễu hất văng.",
            skillDescEN = "Golden sword arts. Hold attack for Divine Parry Stance: blocks incoming damage and retaliates with a x3.0 Crit Divine Riposte! Special skill spins 360° pulling enemies and erupting with a blast.",
            baseHealth = 100,
            baseAttack = 20,
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
            roleVN = "Bạch Dạ Kiếm Khách / Phong Ấn & Ảo Ảnh",
            roleEN = "Phantom Blade / Wind Mark & Phantom Cleave",
            skillNameVN = "Ám Ảnh Trảm",
            skillNameEN = "Shadow Strike",
            skillDescVN = "Đòn chém bán nguyệt quét rộng 180 độ tích tầng Phong Ấn. Khi đạt 100% nộ kích hoạt Khiên Nội Năng, có thể Nhấn Giữ Tụ Lực chém rách không gian & phóng Nguyệt Kiếm Khí x2.85 sát thương bạo kích! Tuyệt kỹ dịch chuyển chớp nhoáng trảm sát kẻ địch!",
            skillDescEN = "Wide 180° slashes applying Wind Marks. When Inner Energy Shield is active, hold attack to charge Iaido Spatial Slash & shoot Gale Crescent Waves (up to x2.85 Crit DMG)! Special skill executes phantom assassination slashes!",
            baseHealth = 95,
            baseAttack = 28,
            baseSpeed = 5.4f,
            unlockPrice = 0, // Mở khóa sẵn sàng chơi
            controllerPath = "Assets/Animation/Player2/Player2.controller",
            spriteSheetPath = "Assets/ART/Player2/_down idle.png"
        };

        // 3. Lam Kiếm Sĩ (Azure Blade - Player 3)
        characters[CHAR_AZURE_BLADE] = new CharacterData
        {
            id = CHAR_AZURE_BLADE,
            nameVN = "Lam Kiếm Sĩ",
            nameEN = "Azure Blade",
            roleVN = "Kiếm Khí Trọng Kiếm / Tầm Xa & Sóng Kiếm",
            roleEN = "Heavy Crescent Blade / Ranged Waves",
            skillNameVN = "Bão Sóng Kiếm",
            skillNameEN = "Crescent Tempest",
            skillDescVN = "Đòn chém chậm nhưng uy lực, mỗi nhát chém phóng ra sóng kiếm xuyên thấu quái vật. Tuyệt kỹ phóng ra 5 đợt kiếm khí hình nan quạt quét sạch chiến trường!",
            skillDescEN = "Heavy slashes that unleash piercing sword waves. Special skill releases 5 massive crescent waves in a fan spread!",
            baseHealth = 110,
            baseAttack = 24,
            baseSpeed = 4.8f,
            unlockPrice = 0, // Mở khóa sẵn sàng chơi
            controllerPath = "Assets/Animation/Player3/Player3.controller",
            spriteSheetPath = "Assets/ART/Player3/Sword_Idle.png"
        };

        // 4. Cuồng Chiến Binh Đại Chùy (Titan Berserker - Player 4)
        characters[CHAR_TITAN_BERSERKER] = new CharacterData
        {
            id = CHAR_TITAN_BERSERKER,
            nameVN = "Cuồng Chiến Binh",
            nameEN = "Titan Berserker",
            roleVN = "Đại Chùy Chiến Thần / Tanker & Chấn Địa",
            roleEN = "Titan Berserker / Tanker & Cataclysm Slam",
            skillNameVN = "Cuồng Nộ Chấn Địa",
            skillNameEN = "Titan Cataclysm",
            skillDescVN = "Đại chùy càn quét. Nhấn Giữ gồng Địa Chấn: Miễn nhiễm toàn bộ sát thương và choáng trong lúc gồng, thả tay nện đất bộc phá làm Choáng kẻ địch 1 giây! Tuyệt kỹ hút toàn bộ quái vật vào tâm chấn rồi nện búa dung nham hất tung.",
            skillDescEN = "Heavy war hammer. Hold attack for Earthshaker Stance: immune to all damage & CC while charging, release to smash the ground and Stun enemies for 1s! Special skill draws all enemies and slams with cataclysmic lava.",
            baseHealth = 200,
            baseAttack = 32,
            baseSpeed = 4.0f,
            unlockPrice = 0, // Mở khóa sẵn sàng chơi
            controllerPath = "Assets/Animation/Player4/Player4.controller",
            spriteSheetPath = "Assets/ART/Player4/Hammer_Idle.png"
        };

        // 5. Sa Mạc Thích Khách (Dune Assassin - Player 5)
        characters[CHAR_DUNE_ASSASSIN] = new CharacterData
        {
            id = CHAR_DUNE_ASSASSIN,
            nameVN = "Sa Mạc Thích Khách",
            nameEN = "Dune Assassin",
            roleVN = "Song Đoản Đao / Tốc Biến & Phản Đòn",
            roleEN = "Dual Scimitars / Speed & Counter",
            skillNameVN = "Cuồng Bão Sa Ảnh",
            skillNameEN = "Sandstorm X-Rend",
            skillDescVN = "Combo 3 đòn song đao chém lướt bạo kích. Nhấn Giữ vào Thế Thủ Ảo Ảnh: Giảm 70% sát thương và tung đòn Phản Kích Choáng 1.2s! Tuyệt kỹ tạo lốc cát vây khốn và trảm kích chữ X xé toạc chiến trường gây Chảy Máu liên tục.",
            skillDescEN = "Swift 3-hit dagger combos with high critical chance. Hold attack for Mirage Defend: reduces damage by 70% and unleashes an Air Slash Counter (1.2s Stun)! Special skill summons a sand vortex and executes a devastating Cross-X Slash with Sand Bleed.",
            baseHealth = 110,
            baseAttack = 18,
            baseSpeed = 4.8f,
            unlockPrice = 0, // Mở khóa sẵn sàng chơi
            controllerPath = "Assets/Animation/Player5/Player5.controller",
            spriteSheetPath = "Assets/ART/Player5/PNG/idle/idle_1.png"
        };

        // 6. Pháp Sư Tinh Tú (Celestial Mage - Player 6)
        characters[CHAR_CELESTIAL_MAGE] = new CharacterData
        {
            id = CHAR_CELESTIAL_MAGE,
            nameVN = "Pháp Sư Tinh Tú",
            nameEN = "Celestial Mage",
            roleVN = "Pháp Sư / Tinh Vân Ma Pháp & Thiên Thạch",
            roleEN = "Mage / Arcane Stars & Meteor Apocalypse",
            skillNameVN = "Thiên Thạch Ma Trận",
            skillNameEN = "Meteor Apocalypse",
            skillDescVN = "Phóng 3 luồng cầu ma pháp tự dẫn đường và bộc phá sét lan. Nhấn Giữ tạo Khiên Ma Trận hấp thụ 80% sát thương và bộc phá đẩy lùi. Tuyệt kỹ triệu hồi bão thiên thạch hủy diệt toàn màn hình!",
            skillDescEN = "Launches 3 homing arcane orbs with chain lightning. Hold attack for Arcane Barrier absorbing 80% damage and bursting outward. Special skill summons a cataclysmic meteor storm!",
            baseHealth = 90,
            baseAttack = 30,
            baseSpeed = 4.5f,
            unlockPrice = 0,
            controllerPath = "",
            spriteSheetPath = "Assets/ART/Player6/sample(idle&walk)/idle/sprite sheets/idle.png"
        };

        // Mặc định mở khóa
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_VAELSTRIDER, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_GALE_RONIN, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_AZURE_BLADE, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_TITAN_BERSERKER, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_DUNE_ASSASSIN, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_CELESTIAL_MAGE, 1);
        PlayerPrefs.Save();
    }

    public string GetSelectedCharacterId()
    {
        string selected = PlayerPrefs.GetString(SELECTED_CHAR_KEY, "");
        if (string.IsNullOrEmpty(selected))
        {
            selected = PlayerPrefs.GetString("SelectedHero", CHAR_TITAN_BERSERKER);
        }

        if (string.Equals(selected, "titan", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(selected, "titanberserker", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(selected, "player4", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selected, CHAR_TITAN_BERSERKER, StringComparison.OrdinalIgnoreCase))
        {
            return CHAR_TITAN_BERSERKER;
        }
        else if (string.Equals(selected, "azure", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(selected, "azureblade", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(selected, "player3", StringComparison.OrdinalIgnoreCase) ||
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

        else if (string.Equals(selected, "dune", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, "duneassassin", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, "player5", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, CHAR_DUNE_ASSASSIN, StringComparison.OrdinalIgnoreCase))
        {
            return CHAR_DUNE_ASSASSIN;
        }
        else if (string.Equals(selected, "mage", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, "celestialmage", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, "player6", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(selected, CHAR_CELESTIAL_MAGE, StringComparison.OrdinalIgnoreCase))
        {
            return CHAR_CELESTIAL_MAGE;
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
        return PlayerPrefs.GetInt(UNLOCKED_PREFIX + id, 0) == 1 || 
               id == CHAR_VAELSTRIDER || 
               id == CHAR_GALE_RONIN || 
               id == CHAR_AZURE_BLADE ||
               id == CHAR_TITAN_BERSERKER ||
               id == CHAR_DUNE_ASSASSIN ||
               id == CHAR_CELESTIAL_MAGE;
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
        if (string.Equals(id, "titan", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(id, "titanberserker", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(id, "player4", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(id, CHAR_TITAN_BERSERKER, StringComparison.OrdinalIgnoreCase))
        {
            normId = CHAR_TITAN_BERSERKER;
        }
        else if (string.Equals(id, "azure", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(id, "azureblade", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(id, "player3", StringComparison.OrdinalIgnoreCase) ||
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
        else if (string.Equals(id, "dune", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, "duneassassin", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, "player5", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, CHAR_DUNE_ASSASSIN, StringComparison.OrdinalIgnoreCase))
        {
            normId = CHAR_DUNE_ASSASSIN;
        }
        else if (string.Equals(id, "mage", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, "celestialmage", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, "player6", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(id, CHAR_CELESTIAL_MAGE, StringComparison.OrdinalIgnoreCase))
        {
            normId = CHAR_CELESTIAL_MAGE;
        }

        PlayerPrefs.SetString(SELECTED_CHAR_KEY, normId);
        PlayerPrefs.SetString("SelectedHero", (normId == CHAR_TITAN_BERSERKER) ? "titan" : ((normId == CHAR_AZURE_BLADE) ? "azure" : (normId == CHAR_GALE_RONIN ? "ronin" : (normId == CHAR_DUNE_ASSASSIN ? "dune" : (normId == CHAR_CELESTIAL_MAGE ? "mage" : "vael")))));
        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[CHARACTER]</color> Đã chọn nhân vật: {normId}");
    }
}
