// ========================================================
// VAELSTRIDER: RUINBOUND - Hero Costume & Wardrobe System
// Manages thematic outfits, armored skins, stat buffs,
// visual shaders & real-time particle auras across all heroes.
// ========================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HeroCostumeManager : MonoBehaviour
{
    public static HeroCostumeManager Instance { get; private set; }

    public enum CostumeRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public enum AuraEffectType
    {
        None,
        MagmaFlames,
        GoldenLightning,
        VoidCosmic,
        FrostBlizzard,
        CrimsonBlood
    }

    [System.Serializable]
    public class HeroCostumeData
    {
        public string id;
        public string heroId;
        public string nameVN;
        public string nameEN;
        public string descVN;
        public string descEN;
        public CostumeRarity rarity;
        public int priceShards;
        public int bonusHp;
        public int bonusAtk;
        public float bonusDefPercent;
        public float bonusSpeed;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.white;
        public Color trimColor = Color.yellow;
        public Color glowColor = Color.cyan;
        public AuraEffectType auraType = AuraEffectType.None;

        public string GetLocalizedName()
        {
            return LocalizationManager.T(nameVN, nameEN);
        }

        public string GetLocalizedDesc()
        {
            return LocalizationManager.T(descVN, descEN);
        }

        public string GetRarityName()
        {
            switch (rarity)
            {
                case CostumeRarity.Mythic: return LocalizationManager.T("THẦN THOẠI", "MYTHIC");
                case CostumeRarity.Legendary: return LocalizationManager.T("HUYỀN THOẠI", "LEGENDARY");
                case CostumeRarity.Epic: return LocalizationManager.T("SỬ THI", "EPIC");
                case CostumeRarity.Rare: return LocalizationManager.T("HIẾM", "RARE");
                default: return LocalizationManager.T("PHỔ THÔNG", "COMMON");
            }
        }

        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case CostumeRarity.Mythic: return new Color(1f, 0.35f, 0.15f, 1f); // Blazing Orange / Red
                case CostumeRarity.Legendary: return new Color(1f, 0.82f, 0.15f, 1f); // Radiant Gold
                case CostumeRarity.Epic: return new Color(0.85f, 0.35f, 1f, 1f); // Majestic Purple
                case CostumeRarity.Rare: return new Color(0.25f, 0.75f, 1f, 1f); // Azure Blue
                default: return new Color(0.75f, 0.75f, 0.75f, 1f); // Silver Grey
            }
        }
    }

    private static Dictionary<string, HeroCostumeData> database = new Dictionary<string, HeroCostumeData>();
    private static bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static HeroCostumeManager EnsureInstance()
    {
        if (Instance == null)
        {
            var existing = FindAnyObjectByType<HeroCostumeManager>();
            if (existing != null)
            {
                Instance = existing;
                Instance.InitializeDatabase();
                return Instance;
            }

            var go = new GameObject("HeroCostumeManager");
            Instance = go.AddComponent<HeroCostumeManager>();
            DontDestroyOnLoad(go);
            Instance.InitializeDatabase();
        }
        return Instance;
    }

    public void InitializeDatabase()
    {
        if (isInitialized && database.Count > 0) return;
        database.Clear();

        // -------------------------------------------------------------
        // 1. PLAYER 4: TITAN BERSERKER (CUỒNG CHIẾN BINH) COSTUMES
        // -------------------------------------------------------------
        AddCostume(new HeroCostumeData
        {
            id = "titan_default",
            heroId = CharacterManager.CHAR_TITAN_BERSERKER,
            nameVN = "Thiết Giáp Titan Thép Đen",
            nameEN = "Titan Heavy Steel Armor",
            descVN = "Bộ chiến giáp thép đen titan tiêu chuẩn của Cuồng Chiến Binh, đúc từ quặng sắt cổ kết hợp viền hoàng kim và đại chùy hủy diệt.",
            descEN = "The standard heavy titanium steel battle plate forged with golden trim, paired with a massive destructive war hammer.",
            rarity = CostumeRarity.Common,
            priceShards = 0,
            bonusHp = 0,
            bonusAtk = 0,
            bonusDefPercent = 0f,
            bonusSpeed = 0f,
            primaryColor = Color.white,
            secondaryColor = new Color(0.30f, 0.35f, 0.45f, 1f),
            trimColor = new Color(1.0f, 0.85f, 0.30f, 1f),
            glowColor = new Color(0.9f, 0.7f, 0.2f, 1f),
            auraType = AuraEffectType.None
        });

        AddCostume(new HeroCostumeData
        {
            id = "titan_magma",
            heroId = CharacterManager.CHAR_TITAN_BERSERKER,
            nameVN = "Hỏa Long Dung Nham Giáp",
            nameEN = "Magma Dragon Dreadplate",
            descVN = "Giáp tôi luyện từ lõi dung nham hắc ám. Thân giáp rực lửa đỏ thẫm bốc khói, đại chùy nung chảy kim loại thiêu đốt mọi kẻ thù trong tầm vung!",
            descEN = "Tempered in subterranean volcanic cores. Emits blazing embers and scorches nearby foes with sheer draconic fury!",
            rarity = CostumeRarity.Legendary,
            priceShards = 1500,
            bonusHp = 45,
            bonusAtk = 12,
            bonusDefPercent = 8f,
            bonusSpeed = 0.2f,
            primaryColor = new Color(1.0f, 0.45f, 0.35f, 1f),
            secondaryColor = new Color(0.85f, 0.15f, 0.10f, 1f),
            trimColor = new Color(1.0f, 0.60f, 0.10f, 1f),
            glowColor = new Color(1.0f, 0.25f, 0.05f, 1f),
            auraType = AuraEffectType.MagmaFlames
        });

        AddCostume(new HeroCostumeData
        {
            id = "titan_paladin",
            heroId = CharacterManager.CHAR_TITAN_BERSERKER,
            nameVN = "Thánh Giáp Hoàng Kim Sấm Sét",
            nameEN = "Golden Thunder Paladin",
            descVN = "Thánh giáp chiến thần hoàng kim chói lọi được ban phước bởi Thần Sấm Cổ Đại. Đại chùy phóng sấm sét áp chế quái vật toàn chiến trường.",
            descEN = "Blessed by the Ancient Thunder God. Radiates brilliant divine gold and unleashes thunderous shockwaves on every swing.",
            rarity = CostumeRarity.Mythic,
            priceShards = 3000,
            bonusHp = 70,
            bonusAtk = 18,
            bonusDefPercent = 12f,
            bonusSpeed = 0.4f,
            primaryColor = new Color(1.0f, 0.92f, 0.55f, 1f),
            secondaryColor = new Color(0.95f, 0.75f, 0.20f, 1f),
            trimColor = new Color(0.40f, 0.90f, 1.0f, 1f),
            glowColor = new Color(1.0f, 0.95f, 0.30f, 1f),
            auraType = AuraEffectType.GoldenLightning
        });

        AddCostume(new HeroCostumeData
        {
            id = "titan_void",
            heroId = CharacterManager.CHAR_TITAN_BERSERKER,
            nameVN = "U Hồn Hư Không Giáp",
            nameEN = "Void Nether Berserker",
            descVN = "Giáp hắc diện thạch toát ra u linh sắc tím ma mị từ cõi Hư Không Vô Tận. Tăng mạnh lực công kích và hút sinh lực từ kẻ địch.",
            descEN = "Forged from abyssal obsidian infused with void cosmic energy. Syphons vitality and deals lethal destructive crushing hits.",
            rarity = CostumeRarity.Epic,
            priceShards = 800,
            bonusHp = 30,
            bonusAtk = 10,
            bonusDefPercent = 5f,
            bonusSpeed = 0.2f,
            primaryColor = new Color(0.65f, 0.40f, 0.95f, 1f),
            secondaryColor = new Color(0.35f, 0.15f, 0.65f, 1f),
            trimColor = new Color(0.85f, 0.50f, 1.0f, 1f),
            glowColor = new Color(0.70f, 0.30f, 1.0f, 1f),
            auraType = AuraEffectType.VoidCosmic
        });

        // -------------------------------------------------------------
        // 2. PLAYER 3: AZURE BLADE (LAM KIẾM SĨ) COSTUMES
        // -------------------------------------------------------------
        AddCostume(new HeroCostumeData
        {
            id = "azure_default",
            heroId = CharacterManager.CHAR_AZURE_BLADE,
            nameVN = "Lam Nguyệt Kiếm Y",
            nameEN = "Azure Crescent Robes",
            descVN = "Trang phục truyền thống của Lam Kiếm Sĩ mang sắc lam trăng khuyết và thanh trọng kiếm phát ra kiếm khí.",
            descEN = "Traditional crescent robes of the Azure Blade, channeling graceful long-range sword waves.",
            rarity = CostumeRarity.Common,
            priceShards = 0,
            bonusHp = 0,
            bonusAtk = 0,
            bonusDefPercent = 0f,
            bonusSpeed = 0f,
            primaryColor = Color.white,
            secondaryColor = new Color(0.20f, 0.45f, 0.85f, 1f),
            trimColor = new Color(0.40f, 0.80f, 1.0f, 1f),
            glowColor = new Color(0.20f, 0.65f, 1.0f, 1f),
            auraType = AuraEffectType.None
        });

        AddCostume(new HeroCostumeData
        {
            id = "azure_crimson",
            heroId = CharacterManager.CHAR_AZURE_BLADE,
            nameVN = "Huyết Nguyệt Ma Kiếm Y",
            nameEN = "Crimson Bloodmoon Robe",
            descVN = "Y phục đỏ rực nhuốm màu Huyết Nguyệt. Kiếm khí chuyển thành lưỡi liềm máu đỏ thẫm chém nát hàng ngũ quái vật.",
            descEN = "Infused with crimson bloodmoon essence. Slashes unleashed as destructive crimson crescent waves.",
            rarity = CostumeRarity.Legendary,
            priceShards = 1500,
            bonusHp = 35,
            bonusAtk = 15,
            bonusDefPercent = 5f,
            bonusSpeed = 0.5f,
            primaryColor = new Color(1.0f, 0.40f, 0.40f, 1f),
            secondaryColor = new Color(0.80f, 0.10f, 0.15f, 1f),
            trimColor = new Color(1.0f, 0.80f, 0.20f, 1f),
            glowColor = new Color(1.0f, 0.20f, 0.20f, 1f),
            auraType = AuraEffectType.CrimsonBlood
        });

        AddCostume(new HeroCostumeData
        {
            id = "azure_radiant",
            heroId = CharacterManager.CHAR_AZURE_BLADE,
            nameVN = "Bạch Kim Thiên Kiếm Y",
            nameEN = "Radiant Celestial Garb",
            descVN = "Trang phục bạch kim thần thánh tỏa ánh hào quang thiên giới, gia tăng tối đa tốc độ di chuyển và sát thương kiếm khí.",
            descEN = "Divine white gold celestial garb. Grants unmatched movement swiftness and celestial wave force.",
            rarity = CostumeRarity.Mythic,
            priceShards = 3000,
            bonusHp = 50,
            bonusAtk = 20,
            bonusDefPercent = 8f,
            bonusSpeed = 0.6f,
            primaryColor = new Color(0.95f, 0.95f, 1.0f, 1f),
            secondaryColor = new Color(0.70f, 0.85f, 1.0f, 1f),
            trimColor = new Color(1.0f, 0.85f, 0.30f, 1f),
            glowColor = new Color(0.60f, 0.90f, 1.0f, 1f),
            auraType = AuraEffectType.GoldenLightning
        });

        // -------------------------------------------------------------
        // 3. PLAYER 2: GALE RONIN (PHONG LÔI LÃNG KHÁCH) COSTUMES
        // -------------------------------------------------------------
        AddCostume(new HeroCostumeData
        {
            id = "ronin_default",
            heroId = CharacterManager.CHAR_GALE_RONIN,
            nameVN = "Bạch Dạ Kiếm Y",
            nameEN = "White Night Haori",
            descVN = "Bộ áo haori trắng thuần phong cách lãng khách cô độc, nhẹ nhàng thanh thoát chuẩn kiếm đạo Iaido.",
            descEN = "A lightweight wanderer's haori suited for swift Iaido precision strikes.",
            rarity = CostumeRarity.Common,
            priceShards = 0,
            bonusHp = 0,
            bonusAtk = 0,
            bonusDefPercent = 0f,
            bonusSpeed = 0f,
            primaryColor = Color.white,
            secondaryColor = new Color(0.85f, 0.85f, 0.95f, 1f),
            trimColor = new Color(0.30f, 0.60f, 0.90f, 1f),
            glowColor = new Color(0.50f, 0.80f, 1.0f, 1f),
            auraType = AuraEffectType.None
        });

        AddCostume(new HeroCostumeData
        {
            id = "ronin_shadow",
            heroId = CharacterManager.CHAR_GALE_RONIN,
            nameVN = "Ám Dạ Thích Khách Y",
            nameEN = "Shadow Night Shinobi",
            descVN = "Trang phục dạ hành đen tuyền bí ẩn, giúp lướt ảo ảnh thoắt ẩn thoắt hiện và gây sát thương chí mạng cực cao.",
            descEN = "Stealth midnight shinobi garb designed for lightning fast phantom strikes.",
            rarity = CostumeRarity.Epic,
            priceShards = 800,
            bonusHp = 20,
            bonusAtk = 14,
            bonusDefPercent = 4f,
            bonusSpeed = 0.5f,
            primaryColor = new Color(0.40f, 0.40f, 0.50f, 1f),
            secondaryColor = new Color(0.15f, 0.15f, 0.20f, 1f),
            trimColor = new Color(0.80f, 0.20f, 0.40f, 1f),
            glowColor = new Color(0.60f, 0.20f, 0.80f, 1f),
            auraType = AuraEffectType.VoidCosmic
        });

        AddCostume(new HeroCostumeData
        {
            id = "ronin_thunder",
            heroId = CharacterManager.CHAR_GALE_RONIN,
            nameVN = "Lôi Thần Trảm Vũ Y",
            nameEN = "Thunder God Storm Robes",
            descVN = "Kim giáp sấm sét hoàng kim rực sáng, triệu hồi sấm sét bao phủ mỗi nhát chém chớp nhoáng.",
            descEN = "Infused with tempestuous lightning that overcharges every iaido execution slash.",
            rarity = CostumeRarity.Legendary,
            priceShards = 1500,
            bonusHp = 35,
            bonusAtk = 18,
            bonusDefPercent = 6f,
            bonusSpeed = 0.7f,
            primaryColor = new Color(1.0f, 0.90f, 0.40f, 1f),
            secondaryColor = new Color(0.90f, 0.70f, 0.10f, 1f),
            trimColor = new Color(0.20f, 0.80f, 1.0f, 1f),
            glowColor = new Color(1.0f, 0.90f, 0.20f, 1f),
            auraType = AuraEffectType.GoldenLightning
        });

        // -------------------------------------------------------------
        // 4. PLAYER 1: VAELSTRIDER (HIỆP SĨ TÀN TÍCH) COSTUMES
        // -------------------------------------------------------------
        AddCostume(new HeroCostumeData
        {
            id = "vael_default",
            heroId = CharacterManager.CHAR_VAELSTRIDER,
            nameVN = "Hiệp Sĩ Khởi Đầu",
            nameEN = "Ruin Knight Attire",
            descVN = "Áo choàng đỏ và chiến giáp tàn tích tiêu chuẩn của hiệp sĩ Vaelstrider.",
            descEN = "The classic scarlet cloak and ruin armor of the legendary Vaelstrider.",
            rarity = CostumeRarity.Common,
            priceShards = 0,
            bonusHp = 0,
            bonusAtk = 0,
            bonusDefPercent = 0f,
            bonusSpeed = 0f,
            primaryColor = Color.white,
            secondaryColor = new Color(0.77f, 0.14f, 0.19f, 1f),
            trimColor = new Color(0.90f, 0.75f, 0.25f, 1f),
            glowColor = new Color(0.85f, 0.20f, 0.20f, 1f),
            auraType = AuraEffectType.None
        });

        AddCostume(new HeroCostumeData
        {
            id = "vael_sunburst",
            heroId = CharacterManager.CHAR_VAELSTRIDER,
            nameVN = "Thái Dương Thánh Giáp",
            nameEN = "Solar Radiance Plate",
            descVN = "Bộ thánh giáp rực sáng ánh dương quang thần thánh ban cho Vaelstrider sức mạnh công thủ toàn diện.",
            descEN = "Blessed by the ancient sun shrine, offering superior offensive and defensive power.",
            rarity = CostumeRarity.Legendary,
            priceShards = 1500,
            bonusHp = 40,
            bonusAtk = 12,
            bonusDefPercent = 8f,
            bonusSpeed = 0.4f,
            primaryColor = new Color(1.0f, 0.95f, 0.60f, 1f),
            secondaryColor = new Color(0.95f, 0.65f, 0.15f, 1f),
            trimColor = new Color(1.0f, 0.85f, 0.20f, 1f),
            glowColor = new Color(1.0f, 0.75f, 0.10f, 1f),
            auraType = AuraEffectType.GoldenLightning
        });

        isInitialized = true;
    }

    private static void AddCostume(HeroCostumeData data)
    {
        database[data.id] = data;
    }

    public static List<HeroCostumeData> GetCostumesForHero(string heroId)
    {
        EnsureInstance();
        string normalized = NormalizeHeroId(heroId);

        List<HeroCostumeData> list = new List<HeroCostumeData>();
        foreach (var kvp in database)
        {
            if (NormalizeHeroId(kvp.Value.heroId) == normalized)
            {
                list.Add(kvp.Value);
            }
        }
        return list;
    }

    public static HeroCostumeData GetCostume(string costumeId)
    {
        EnsureInstance();
        if (database.TryGetValue(costumeId, out var data))
        {
            return data;
        }
        return null;
    }

    public static string NormalizeHeroId(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return CharacterManager.CHAR_VAELSTRIDER;
        string lower = heroId.ToLower();
        if (lower.Contains("titan") || lower.Contains("player4")) return CharacterManager.CHAR_TITAN_BERSERKER;
        if (lower.Contains("azure") || lower.Contains("player3")) return CharacterManager.CHAR_AZURE_BLADE;
        if (lower.Contains("ronin") || lower.Contains("gale") || lower.Contains("maid") || lower.Contains("player2")) return CharacterManager.CHAR_GALE_RONIN;
        return CharacterManager.CHAR_VAELSTRIDER;
    }

    public static string GetEquippedCostumeId(string heroId)
    {
        string norm = NormalizeHeroId(heroId);
        string defaultId = GetDefaultCostumeId(norm);
        string key = "Equipped_Costume_" + norm;
        return PlayerPrefs.GetString(key, defaultId);
    }

    public static string GetDefaultCostumeId(string heroId)
    {
        string norm = NormalizeHeroId(heroId);
        if (norm == CharacterManager.CHAR_TITAN_BERSERKER) return "titan_default";
        if (norm == CharacterManager.CHAR_AZURE_BLADE) return "azure_default";
        if (norm == CharacterManager.CHAR_GALE_RONIN) return "ronin_default";
        return "vael_default";
    }

    public static bool IsCostumeUnlocked(string costumeId)
    {
        var data = GetCostume(costumeId);
        if (data == null) return false;
        if (data.priceShards <= 0 || data.rarity == CostumeRarity.Common) return true;

        string key = "Costume_Unlocked_" + costumeId;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static bool UnlockCostume(string costumeId)
    {
        var data = GetCostume(costumeId);
        if (data == null) return false;
        if (IsCostumeUnlocked(costumeId)) return true;

        long currentShards = AccountDatabase.GetAethelShards();
        if (currentShards < data.priceShards) return false;

        AccountDatabase.AddAethelShards(-data.priceShards);
        PlayerPrefs.SetInt("Costume_Unlocked_" + costumeId, 1);
        PlayerPrefs.Save();
        AccountDatabase.SaveActiveSessionToDatabase();
        return true;
    }

    public static void EquipCostume(string heroId, string costumeId)
    {
        string norm = NormalizeHeroId(heroId);
        var data = GetCostume(costumeId);
        if (data == null) return;

        string key = "Equipped_Costume_" + norm;
        PlayerPrefs.SetString(key, costumeId);
        PlayerPrefs.Save();
        AccountDatabase.SaveActiveSessionToDatabase();
    }

    public static void GetEquippedCostumeBonus(string heroId, out int hp, out int atk, out float defPercent, out float speed)
    {
        hp = 0;
        atk = 0;
        defPercent = 0f;
        speed = 0f;

        string costumeId = GetEquippedCostumeId(heroId);
        var data = GetCostume(costumeId);
        if (data != null)
        {
            hp = data.bonusHp;
            atk = data.bonusAtk;
            defPercent = data.bonusDefPercent;
            speed = data.bonusSpeed;
        }
    }

    public static void ApplyCostumeVisual(GameObject characterObj, string heroId, string costumeId = null)
    {
        if (characterObj == null) return;
        string norm = NormalizeHeroId(heroId);
        if (string.IsNullOrEmpty(costumeId))
        {
            costumeId = GetEquippedCostumeId(norm);
        }

        var data = GetCostume(costumeId);
        if (data == null) return;

        var sr = characterObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = data.primaryColor;
        }

        // Manage In-game Aura GameObject
        Transform existingAura = characterObj.transform.Find("Costume_Aura_Effect");
        if (data.auraType == AuraEffectType.None)
        {
            if (existingAura != null) Destroy(existingAura.gameObject);
        }
        else
        {
            GameObject auraObj = existingAura != null ? existingAura.gameObject : new GameObject("Costume_Aura_Effect");
            auraObj.transform.SetParent(characterObj.transform, false);
            auraObj.transform.localPosition = new Vector3(0, -0.1f, 0);

            var light = auraObj.GetComponent<Light>();
            if (light == null)
            {
                // Create soft glowing light or colored particle emitter
                var auraSr = auraObj.GetComponent<SpriteRenderer>();
                if (auraSr == null) auraSr = auraObj.AddComponent<SpriteRenderer>();
                auraSr.color = new Color(data.glowColor.r, data.glowColor.g, data.glowColor.b, 0.45f);
                auraSr.sortingOrder = (sr != null ? sr.sortingOrder - 1 : 0);
            }
        }
    }

    public static void ApplyCostumeToImage(Image img, string heroId, string costumeId = null)
    {
        if (img == null) return;
        string norm = NormalizeHeroId(heroId);
        if (string.IsNullOrEmpty(costumeId))
        {
            costumeId = GetEquippedCostumeId(norm);
        }

        var data = GetCostume(costumeId);
        if (data != null)
        {
            img.color = data.primaryColor;
        }
        else
        {
            img.color = Color.white;
        }
    }
}
