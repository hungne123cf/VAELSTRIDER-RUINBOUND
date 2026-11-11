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
    public int baseDefense;
    public float baseSpeed;
    public float baseLifesteal;
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
    public const string CHAR_ABYSSAL_MECH = "AbyssalMech";

    private Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();

    private static bool isQuitting = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeCharacters();
        }
        else if (Instance != this)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
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
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
            Instance.InitializeCharacters();
        }
        return Instance;
    }

    private void InitializeCharacters()
    {
        characters.Clear();

        // 1. Vaelstrider (Protagonist / Balanced / Starter - Mặc định mở khóa)
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
            baseAttack = 25,
            baseDefense = 20,
            baseSpeed = 5.0f,
            baseLifesteal = 0.05f, // 5% Hút máu cơ bản của Hiệp Sĩ Vaelstrider
            unlockPrice = 0, // Miễn phí ban đầu
            controllerPath = "Controllers/Player1",
            spriteSheetPath = "Assets/ART/Player/IDLE/idle_down.png"
        };

        // 2. Phong Lôi Lãng Khách (Gale Ronin / Player 2)
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
            baseDefense = 6,
            baseSpeed = 5.4f,
            unlockPrice = 25000, // Tier A+: Sát thủ cơ động, tốc độ cao nhất (5.4 SPD), bạo kích khủng x2.85
            controllerPath = "Controllers/Player2",
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
            baseDefense = 14,
            baseSpeed = 4.8f,
            unlockPrice = 12000, // Tier B+: Sóng kiếm tầm xa an toàn, phòng ngự vững chắc
            controllerPath = "Controllers/Player3",
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
            baseDefense = 25,
            baseSpeed = 4.0f,
            unlockPrice = 45000, // Tier S: Siêu Tanker bất tử 200 HP, ATK 32 cao nhất cận chiến, nện choáng
            controllerPath = "Controllers/Player4",
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
            baseDefense = 8,
            baseSpeed = 4.8f,
            unlockPrice = 5000, // Tier B: Nhập môn, giá rẻ dễ tiếp cận, dồn sát thương xuất huyết & choáng
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
            baseDefense = 5,
            baseSpeed = 4.5f,
            unlockPrice = 65000, // Tier SS: Pháp sư tối thượng, đạn tự dẫn đường sét lan, bão thiên thạch quét sạch map
            controllerPath = "",
            spriteSheetPath = "Assets/ART/Player6/sample(idle&walk)/idle/sprite sheets/idle.png"
        };

        // 7. Hải Thần Cơ Giáp (Abyssal Leviathan / Goldfish Mech - Player 7)
        characters[CHAR_ABYSSAL_MECH] = new CharacterData
        {
            id = CHAR_ABYSSAL_MECH,
            nameVN = "Hải Thần Cơ Giáp",
            nameEN = "Abyssal Leviathan",
            roleVN = "Đấu Sĩ Đại Dương / Mỏ Neo & Sóng Thần Cá Mập",
            roleEN = "Tidal Juggernaut / Anchor & Shark Tsunami",
            skillNameVN = "Đại Hồng Thủy Cá Mập",
            skillNameEN = "Shark Tsunami Apocalypse",
            skillDescVN = "Combo 3 thức Mỏ Neo Thủy Triều bộc phá sóng xung kích. Kỹ năng Xoáy Nước Biển Sâu kéo hút kẻ địch và tạo Khiên Bong Bóng. Tuyệt kỹ triệu hồi Sóng Thần Đại Dương kèm Đàn Cá Mập Khổng Lồ cưỡi trên đỉnh sóng cuốn phăng và cắn xé toàn bộ chiến trường!",
            skillDescEN = "3-hit Tidal Anchor combo with water shockwaves. Abyssal Whirlpool pulls enemies and grants a Bubble Shield. Ultimate unleashes a massive Pixel Tsunami Wave with Giant Sharks surfing on the crest, sweeping and devouring all foes!",
            baseHealth = 165,
            baseAttack = 26,
            baseDefense = 18,
            baseSpeed = 4.6f,
            baseLifesteal = 0.04f,
            unlockPrice = 0, // Mở khóa sẵn để trải nghiệm ngay nhân vật mới!
            controllerPath = "",
            spriteSheetPath = "Assets/Resources/Sprites/AbyssalMech/goldfish_mech.png"
        };

        // Mặc định ban đầu: Mở khóa Vaelstrider và Hải Thần Cơ Giáp (nhân vật mới)
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_VAELSTRIDER, 1);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + CHAR_ABYSSAL_MECH, 1);
        PlayerPrefs.Save();
    }

    public static string NormalizeHeroId(string id)
    {
        if (string.IsNullOrEmpty(id)) return CHAR_VAELSTRIDER;
        string lower = id.Trim().ToLower();
        if (lower == "titan" || lower == "titanberserker" || lower == "player4" || lower == CHAR_TITAN_BERSERKER.ToLower())
            return CHAR_TITAN_BERSERKER;
        if (lower == "azure" || lower == "azureblade" || lower == "player3" || lower == CHAR_AZURE_BLADE.ToLower())
            return CHAR_AZURE_BLADE;
        if (lower == "ronin" || lower == "galeronin" || lower == "player2" || lower == "maid" || lower == CHAR_GALE_RONIN.ToLower())
            return CHAR_GALE_RONIN;
        if (lower == "dune" || lower == "duneassassin" || lower == "player5" || lower == CHAR_DUNE_ASSASSIN.ToLower())
            return CHAR_DUNE_ASSASSIN;
        if (lower == "mage" || lower == "celestialmage" || lower == "player6" || lower == CHAR_CELESTIAL_MAGE.ToLower())
            return CHAR_CELESTIAL_MAGE;
        if (lower == "mech" || lower == "abyss" || lower == "abyssalmech" || lower == "goldfish" || lower == "player7" || lower == "player8" || lower == CHAR_ABYSSAL_MECH.ToLower())
            return CHAR_ABYSSAL_MECH;
        return CHAR_VAELSTRIDER;
    }

    public static string GetHeroAliasStatic(string normId)
    {
        if (normId == CHAR_TITAN_BERSERKER) return "titan";
        if (normId == CHAR_AZURE_BLADE) return "azure";
        if (normId == CHAR_GALE_RONIN) return "ronin";
        if (normId == CHAR_DUNE_ASSASSIN) return "dune";
        if (normId == CHAR_CELESTIAL_MAGE) return "mage";
        if (normId == CHAR_ABYSSAL_MECH) return "mech";
        return "vael";
    }

    /// <summary>
    /// Kiểm tra xem nhân vật có phải là Tướng hệ Sức Mạnh Phép (AP) hay không.
    /// Hiện tại gồm có: Pháp Sư Tinh Tú (Celestial Mage) và Lam Kiếm Sĩ (Azure Blade).
    /// </summary>
    public static bool IsAbilityPowerHero(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return false;
        string lower = heroId.ToLower();
        return lower == "azure" || lower == "azureblade" || lower == "player3" || lower == CHAR_AZURE_BLADE.ToLower() ||
               lower == "mage" || lower == "celestialmage" || lower == "player6" || lower == CHAR_CELESTIAL_MAGE.ToLower();
    }

    public string GetSelectedCharacterId()
    {
        string selected = PlayerPrefs.GetString(SELECTED_CHAR_KEY, "");
        if (string.IsNullOrEmpty(selected))
        {
            selected = PlayerPrefs.GetString("SelectedHero", CHAR_VAELSTRIDER);
        }

        string norm = NormalizeHeroId(selected);
        return norm;
    }

    public CharacterData GetSelectedCharacter()
    {
        string id = GetSelectedCharacterId();
        if (characters.TryGetValue(id, out var data)) return data;
        return characters[CHAR_VAELSTRIDER];
    }

    public CharacterData GetCharacter(string id)
    {
        string normId = NormalizeHeroId(id);
        if (characters.TryGetValue(normId, out var data)) return data;
        return null;
    }

    public bool IsCharacterUnlocked(string id)
    {
        string normId = NormalizeHeroId(id);
        // Tướng mặc định Vaelstrider và tướng mới Hải Thần Cơ Giáp luôn mở khóa trên mọi nền tảng
        if (normId == CHAR_VAELSTRIDER || normId == CHAR_ABYSSAL_MECH) return true;

        // TRÊN PC & UNITY EDITOR: Mở khóa toàn bộ 100% tướng để trải nghiệm và thử nghiệm tự do
        if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
        {
            return true;
        }

        // ĐỐI VỚI BẢN APK TẢI VỀ (Android Device): Khóa toàn bộ tướng, người chơi phải dùng Linh Hồn để mở khóa
        return IsCharacterPurchased(normId);
    }

    public bool IsCharacterPurchased(string id)
    {
        string normId = NormalizeHeroId(id);
        if (normId == CHAR_VAELSTRIDER || normId == CHAR_ABYSSAL_MECH) return true;
        return PlayerPrefs.GetInt(UNLOCKED_PREFIX + normId, 0) == 1;
    }

    public bool UnlockCharacter(string id)
    {
        string normId = NormalizeHeroId(id);
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + normId, 1);
        PlayerPrefs.Save();
        AccountDatabase.SyncHeroUnlock(normId);
        Debug.Log($"<color=green>[CHARACTER]</color> Đã mở khóa thành công tướng: {normId}");
        return true;
    }

    public void SelectCharacter(string id)
    {
        string normId = NormalizeHeroId(id);
        // Chỉ cho phép chọn vào trận nếu tướng đã được mở khóa hợp lệ
        if (!IsCharacterUnlocked(normId))
        {
            Debug.LogWarning($"[CHARACTER] Tướng {normId} chưa được mở khóa, không thể chọn!");
            return;
        }

        PlayerPrefs.SetString(SELECTED_CHAR_KEY, normId);
        PlayerPrefs.SetString("SelectedHero", GetHeroAliasStatic(normId));
        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[CHARACTER]</color> Đã chọn nhân vật: {normId}");
    }
}
