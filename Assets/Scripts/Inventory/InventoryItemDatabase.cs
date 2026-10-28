// ========================================================
// VAELSTRIDER: RUINBOUND - Inventory Item Database
// Comprehensive Database of In-Game Drops, Equipment & Gacha Items
// ========================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public static class InventoryItemDatabase
{
    public enum ItemCategory
    {
        All = -1,
        Equipment = 0,    // Vũ khí, Khiên, Giáp (Dùng để trang bị, rã lấy linh hồn hoặc khảm)
        Material = 1,     // Đá quý, Khoáng thạch, Tinh thể (Dùng để khảm chiêu thức hoặc rã linh hồn)
        GachaArtifact = 2 // Bùa triệu hồi, Chìa khóa, Rương thần bí, Lõi linh hồn
    }

    public enum ItemRarity
    {
        Common = 0,    // Xám / Trắng
        Rare = 1,      // Xanh lam
        Epic = 2,      // Tím
        Legendary = 3, // Vàng kim
        Mythic = 4     // Đỏ rực
    }

    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string nameVi;
        public string nameEn;
        public string descVi;
        public string descEn;
        public ItemCategory category;
        public ItemRarity rarity;
        public string iconPath;
        public int maxStack = 999;
        public int sellPrice = 10;
        public int hpBonus = 0;
        public int atkBonus = 0;
        public int physAtkBonus = 0;
        public int magicAtkBonus = 0;
        public int trueAtkBonus = 0;
        public int defBonus = 0;
        public float speedBonus = 0f;
        public float manaBonus = 0f;
        public float manaRegenBonus = 0f;
        public int armorPenBonus = 0;
        public int magicPenBonus = 0;
        public float armorPenPctBonus = 0f;
        public float magicPenPctBonus = 0f;
        public string sourceVi;
        public string sourceEn;
        public bool isUsable = false;
        public string useEffectTextVi;
        public string useEffectTextEn;

        private Sprite _cachedSprite = null;
        public Sprite Icon
        {
            get
            {
                if (_cachedSprite != null) return _cachedSprite;
                if (!string.IsNullOrEmpty(iconPath))
                {
                    _cachedSprite = Resources.Load<Sprite>(iconPath);
#if UNITY_EDITOR
                    if (_cachedSprite == null)
                    {
                        _cachedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                    }
#endif
                    if (_cachedSprite == null)
                    {
                        string fullDiskPath = iconPath.StartsWith("Assets") ? System.IO.Path.Combine(Application.dataPath, iconPath.Substring(6).TrimStart('/', '\\')) : iconPath;
                        if (System.IO.File.Exists(fullDiskPath))
                        {
                            byte[] bytes = System.IO.File.ReadAllBytes(fullDiskPath);
                            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (tex.LoadImage(bytes))
                            {
                                tex.filterMode = FilterMode.Point;
                                _cachedSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                            }
                        }
                    }
                }
                return _cachedSprite;
            }
        }
    }

    private static Dictionary<string, ItemDefinition> itemsDict = null;
    private static List<ItemDefinition> allItemsList = null;

    public static void Initialize()
    {
        if (itemsDict != null) return;
        itemsDict = new Dictionary<string, ItemDefinition>();
        allItemsList = new List<ItemDefinition>();

        // =========================================================
        // 1. GACHA & THẦN BÍ (GACHA / ARTIFACTS)
        // =========================================================
        RegisterItem(new ItemDefinition
        {
            id = "scroll_summon_mythic",
            nameVi = "Bùa Triệu Hồi Thần Bí",
            nameEn = "Mystic Summon Scroll",
            descVi = "Cổ phù khắc ấn chú cổ đại, dùng để triệu hồi chiến binh hoặc vũ khí thần thoại từ Vòng Quay Vận Mệnh.",
            descEn = "Ancient engraved scroll used to summon legendary heroes and mythic weapons from the Gacha Vault.",
            category = ItemCategory.GachaArtifact,
            rarity = ItemRarity.Mythic,
            iconPath = "Assets/Sprites/Items/scroll_summon_mythic.png",
            sourceVi = "Phần thưởng Vượt Ải / Đăng nhập",
            sourceEn = "Stage Clear / Daily Rewards",
            sellPrice = 100,
            isUsable = true,
            useEffectTextVi = "Dùng trong Vòng Quay Gacha",
            useEffectTextEn = "Used in Gacha Vault"
        });

        RegisterItem(new ItemDefinition
        {
            id = "key_ancient_dungeon",
            nameVi = "Chìa Khóa Tàn Tích Cổ",
            nameEn = "Ancient Dungeon Key",
            descVi = "Chìa khóa đúc bằng đồng thiêng cổ xưa. Dùng kết hợp với 1 Rương Tàn Tích Cổ để mở khóa chiến lợi phẩm quý hiếm.",
            descEn = "A sacred ancient key. Used together with 1 Ancient Relic Chest to unlock rare treasures and Aethel Shards.",
            category = ItemCategory.GachaArtifact,
            rarity = ItemRarity.Legendary,
            iconPath = "Assets/Sprites/Items/key_ancient_dungeon.png",
            sourceVi = "Rơi từ Boss Vực Thẳm / Gacha",
            sourceEn = "Abyssal Boss Drop / Gacha",
            sellPrice = 80,
            isUsable = true,
            useEffectTextVi = "Mở Rương Tàn Tích (Cần 1 Rương + 1 Chìa)",
            useEffectTextEn = "Opens Ancient Chest (Requires 1 Chest + 1 Key)"
        });

        RegisterItem(new ItemDefinition
        {
            id = "core_abyssal_soul",
            nameVi = "Lõi Linh Hồn Vực Thẳm",
            nameEn = "Abyssal Soul Core",
            descVi = "Lõi nguyên thạch chứa đựng linh hồn cổ xưa tinh khiết, dùng để đột phá sức mạnh giới hạn của hiệp sĩ.",
            descEn = "A pulsating soul core containing concentrated ancient essence, used for extreme hero stat awakening.",
            category = ItemCategory.GachaArtifact,
            rarity = ItemRarity.Mythic,
            iconPath = "Assets/Sprites/Items/core_abyssal_soul.png",
            sourceVi = "Gacha Thần Bí / Boss Map 3",
            sourceEn = "Gacha Vault / Map 3 Boss",
            sellPrice = 250,
            isUsable = true,
            useEffectTextVi = "+1000 Linh Hồn khi kích hoạt",
            useEffectTextEn = "+1000 Shards on consumption"
        });

        RegisterItem(new ItemDefinition
        {
            id = "chest_ancient_relic",
            nameVi = "Rương Tàn Tích Cổ",
            nameEn = "Ancient Relic Chest",
            descVi = "Rương báu bị niêm phong hàng nghìn năm. Cần 1 Chìa Khóa Tàn Tích Cổ để mở, nhận ngẫu nhiên Shards và khoáng thạch quý hiếm.",
            descEn = "A sealed chest from the ancient empire. Requires 1 Ancient Dungeon Key to open, granting random Shards and precious gems.",
            category = ItemCategory.GachaArtifact,
            rarity = ItemRarity.Epic,
            iconPath = "Assets/Sprites/Items/chest_closed.png",
            sourceVi = "Nhặt trong trận / Chiến thắng Ải",
            sourceEn = "Battle Drop / Victory Reward",
            sellPrice = 50,
            isUsable = true,
            useEffectTextVi = "Cần 1 Chìa Khóa để Mở (Nhận 200~500 Linh Hồn)",
            useEffectTextEn = "Requires 1 Key to Open (Grants 200~500 Shards)"
        });

        // =========================================================
        // 2. NGUYÊN LIỆU & KHOÁNG THẠCH TRONG TRẬN (MATERIALS)
        // =========================================================
        RegisterItem(new ItemDefinition
        {
            id = "gem_amethyst",
            nameVi = "Thạch Anh Tím Tàn Tích",
            nameEn = "Ruin Amethyst Gem",
            descVi = "Khối thạch anh mang năng lượng bóng đêm, rơi ra khi tiêu diệt quái vật bộ xương trong hầm ngục.",
            descEn = "A glowing purple gem infused with shadowy energy, dropped by dungeon skeletons.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Rare,
            iconPath = "Assets/ART/Item/Nodes/amethystNode.png",
            sourceVi = "Rơi từ Quái Map 1 (Hầm Ngục)",
            sourceEn = "Dropped by Map 1 Monsters",
            sellPrice = 20
        });

        RegisterItem(new ItemDefinition
        {
            id = "gem_bloodstone",
            nameVi = "Huyết Thạch Ma Thuật",
            nameEn = "Bloodstone Crystal",
            descVi = "Viên đá đỏ rực rỉ ra tử khí tàn tích, vật phẩm cực kỳ quý giá dùng để nâng cấp thuộc tính trang bị.",
            descEn = "A crimson gemstone radiating demonic energy, essential for gear attribute enhancements.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Epic,
            iconPath = "Assets/ART/Item/Nodes/bloodstoneNode.png",
            sourceVi = "Rơi từ Quái Map 2 (Thung Lũng)",
            sourceEn = "Dropped by Map 2 Valley Monsters",
            sellPrice = 45
        });

        RegisterItem(new ItemDefinition
        {
            id = "gem_jade",
            nameVi = "Ngọc Bích Thượng Cổ",
            nameEn = "Ancient Jade Gem",
            descVi = "Viên ngọc bích tỏa ra ánh sáng phục hồi dịu nhẹ, chứa đựng năng lượng thiên nhiên nguyên thủy.",
            descEn = "A pristine jade gem emitting calming light, containing pure primordial nature energy.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Rare,
            iconPath = "Assets/ART/Item/Nodes/jadeNode.png",
            sourceVi = "Rơi từ Rương Báu Map 2",
            sourceEn = "Found in Map 2 Chests",
            sellPrice = 30
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_forge_stone",
            nameVi = "Đá Luyện Kim Bí Truyền",
            nameEn = "Forge Stone Ore",
            descVi = "Loại đá cứng cáp dùng cho lò rèn thượng cổ để rèn đúc giáp trụ và vũ khí hoàng gia.",
            descEn = "A dense mineral essential for ancient forgecrafting to temper armors and royal blades.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/forgeStoneNode.png",
            sourceVi = "Rơi từ Quái vật & Phá Hũ",
            sourceEn = "Dropped by Pots & Enemies",
            sellPrice = 15
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_gold_node",
            nameVi = "Quặng Vàng Nguyên Chất",
            nameEn = "Pure Gold Node",
            descVi = "Khối quặng vàng ròng lấp lánh, có thể bán để đổi lấy lượng lớn Aethel Shards.",
            descEn = "A sparkling chunk of pure gold, can be converted into abundant Aethel Shards.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Rare,
            iconPath = "Assets/ART/Item/Nodes/goldNode.png",
            sourceVi = "Phần thưởng Đấu Trường / Boss",
            sourceEn = "Arena & Boss Drop",
            sellPrice = 60
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_keep_stone",
            nameVi = "Đá Cổ Trấn Cung",
            nameEn = "Ancient Keepstone",
            descVi = "Mảnh vỡ từ tường thành của đế chế cổ, mang cấu trúc vật lý kiên cố gia tăng chỉ số phòng thủ.",
            descEn = "A piece of fallen ruin fortress masonry, extremely durable and reinforces hero defense.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/keepStoneNode.png",
            sourceVi = "Rơi từ Quái Đập Phá Map 1 & 2",
            sourceEn = "Map 1 & 2 Labyrinth Drops",
            sellPrice = 12
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_tin",
            nameVi = "Quặng Thiếc Lam Hư Không",
            nameEn = "Cyan Tin Ore",
            descVi = "Khối quặng thiếc tỏa ánh sáng lam dịu mát, vật liệu dẫn ma lực cao cấp cho các vũ khí phép thuật.",
            descEn = "A light-blue tin mineral radiating mana resonance, vital for magical artifact conductivity.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Rare,
            iconPath = "Assets/ART/Item/Nodes/tinNode.png",
            sourceVi = "Đào khoáng / Tiệm Tiếp Tế",
            sourceEn = "Mining / Sanctuary Shop",
            sellPrice = 28
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_copper",
            nameVi = "Quặng Đồng Đỏ Cổ Xưa",
            nameEn = "Ancient Copper Ore",
            descVi = "Quặng đồng đỏ đúc tiền tệ và hợp kim bền dẻo thời kỳ đế chế hoàng kim.",
            descEn = "A rich copper deposit used in ancient metallurgy for resilient weapons and alloys.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/copperNode.png",
            sourceVi = "Nhặt Trong Trận / Cửa Hàng",
            sourceEn = "Combat Drops / Shop",
            sellPrice = 16
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_iron",
            nameVi = "Quặng Sắt Hắc Ám",
            nameEn = "Dark Iron Ore",
            descVi = "Khối sắt đen nguyên khối cứng hơn thép thường gấp 10 lần, cốt lõi để rèn giáp trụ nặng.",
            descEn = "Dense black iron ore ten times harder than common steel, crucial for heavy plate armor.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/ironNode.png",
            sourceVi = "Rơi từ Quái Skeleton Đao Phủ",
            sourceEn = "Dropped by Skeleton Executioners",
            sellPrice = 18
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_coal",
            nameVi = "Hắc Than Nhiệt Lượng",
            nameEn = "Blazing Dark Coal",
            descVi = "Nhiên liệu than đen cháy ở nhiệt độ cực cao, duy trì ngọn lửa vĩnh cửu trong lò rèn hoàng gia.",
            descEn = "High-potency coal burning at immense heat to power the eternal forge fires.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/coalNode.png",
            sourceVi = "Hầm ngục Map 1 / Tiệm Tiếp Tế",
            sourceEn = "Map 1 Dungeons / Shop",
            sellPrice = 10
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_boulder_stone",
            nameVi = "Đá Tảng Cổ Vạn Năm",
            nameEn = "Ancient Ruin Megalith",
            descVi = "Khối cự thạch chứa năng lượng đất mẹ kiên cố, dùng để đúc khiên và thành trì phòng hộ.",
            descEn = "A massive megalith carrying primal earth resilience for heavy shield reinforcement.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Rare,
            iconPath = "Assets/ART/Item/Nodes/boulderNode.png",
            sourceVi = "Boss Golem Tàn Tích",
            sourceEn = "Ruin Golem Boss Drop",
            sellPrice = 35
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_medium_rock",
            nameVi = "Đá Granite Tàn Tích",
            nameEn = "Ruin Granite Ore",
            descVi = "Mảnh đá hoa cương kết tinh từ nham thạch nguội, độ cứng cao và chống va đập tốt.",
            descEn = "Crystallized volcanic granite with high durability and impact resistance.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/mediumRockNode.png",
            sourceVi = "Phá vỡ chướng ngại vật trong trận",
            sourceEn = "Destroying Dungeon Obstacles",
            sellPrice = 14
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_small_rocks",
            nameVi = "Sỏi Cổ Tụ Linh",
            nameEn = "Spirit Rune Pebbles",
            descVi = "Những viên sỏi tích tụ tinh hoa đất trời trong các khe nứt tàn tích, vật phẩm phụ trợ cường hóa.",
            descEn = "Spiritual pebbles infused with ambient ruin mana for auxiliary alchemy.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/smallRocks.png",
            sourceVi = "Thu thập khắp các bản đồ",
            sourceEn = "Collected across all maps",
            sellPrice = 8
        });

        RegisterItem(new ItemDefinition
        {
            id = "ore_stone",
            nameVi = "Đá Cổ Trầm Tích",
            nameEn = "Sedimentary Ruin Stone",
            descVi = "Đá trầm tích cơ bản của vùng đất Vaelstrider, vật liệu xây dựng và chế tác cơ bản nhất.",
            descEn = "Standard ruin bedrock material used for basic masonry and beginner weapon polishing.",
            category = ItemCategory.Material,
            rarity = ItemRarity.Common,
            iconPath = "Assets/ART/Item/Nodes/stoneNode.png",
            sourceVi = "Rơi phổ biến trong mọi bản đồ",
            sourceEn = "Common drop in all stages",
            sellPrice = 6
        });

        // =========================================================
        // 3. TRANG BỊ & VŨ KHÍ GACHA / DROP (EQUIPMENT)
        // =========================================================
        RegisterItem(new ItemDefinition
        {
            id = "wpn_legendary_sword",
            nameVi = "Thánh Kiếm Huyền Thoại",
            nameEn = "Mythic Excalibur Blade",
            descVi = "Thanh kiếm được rèn từ kim loại sao băng, tỏa hào quang rực rỡ và chém ngọt mọi lớp giáp.",
            descEn = "A divine blade forged from meteorite metal, glowing with heavenly power to cleave all armor.",
            category = ItemCategory.Equipment,
            rarity = ItemRarity.Mythic,
            iconPath = "Assets/ART/Item/(high-res) 10 swords/high-res legendary sword.png",
            sourceVi = "Vòng Quay Gacha Thần Binh",
            sourceEn = "Mythic Weapon Gacha",
            atkBonus = 45,
            physAtkBonus = 30,
            trueAtkBonus = 15,
            armorPenBonus = 25,
            armorPenPctBonus = 0.15f,
            hpBonus = 30,
            sellPrice = 300
        });

        RegisterItem(new ItemDefinition
        {
            id = "wpn_fire_sword",
            nameVi = "Hỏa Diệm Kiếm",
            nameEn = "Flamebrand Broadsword",
            descVi = "Lưỡi kiếm rực cháy ngọn lửa dung nham không bao giờ tắt, thiêu đốt kẻ địch trong từng nhát chém.",
            descEn = "Engulfed in eternal magma flames, setting corrupted foes ablaze on each slash.",
            category = ItemCategory.Equipment,
            rarity = ItemRarity.Legendary,
            iconPath = "Assets/ART/Item/(high-res) 10 swords/high-res fire sword.png",
            sourceVi = "Boss Skeleton King Drop / Gacha",
            sourceEn = "Skeleton King Drop / Gacha",
            atkBonus = 32,
            physAtkBonus = 16,
            magicAtkBonus = 16,
            magicPenBonus = 20,
            magicPenPctBonus = 0.12f,
            sellPrice = 180
        });

        RegisterItem(new ItemDefinition
        {
            id = "wpn_katana",
            nameVi = "Hắc Dạ Katana",
            nameEn = "Shadow Katana",
            descVi = "Thanh kiếm sắc bén như gió thoảng, tăng tốc độ xuất chiêu và khả năng né tránh đòn đánh.",
            descEn = "Razor-sharp blade swift as the wind, enhances attack cadence and evasion agility.",
            category = ItemCategory.Equipment,
            rarity = ItemRarity.Epic,
            iconPath = "Assets/ART/Item/(high-res) 10 swords/high-res katana.png",
            sourceVi = "Rơi từ Boss BotWheel (Map 2)",
            sourceEn = "BotWheel Boss Drop (Map 2)",
            atkBonus = 24,
            physAtkBonus = 24,
            armorPenBonus = 18,
            armorPenPctBonus = 0.10f,
            speedBonus = 0.5f,
            sellPrice = 120
        });

        RegisterItem(new ItemDefinition
        {
            id = "wpn_paladin_sword",
            nameVi = "Thánh Kiếm Hiệp Sĩ",
            nameEn = "Paladin Holy Sword",
            descVi = "Bảo kiếm biểu tượng danh dự của các hiệp sĩ Hoàng Gia Vaelstrider.",
            descEn = "An honorable blade handed down through generations of Vaelstrider Royal Paladins.",
            category = ItemCategory.Equipment,
            rarity = ItemRarity.Rare,
            iconPath = "Assets/ART/Item/(high-res) 10 swords/high-res paladin sword.png",
            sourceVi = "Phần Thưởng Tân Thủ / Shop",
            sourceEn = "Starter Gift / Armor Shop",
            atkBonus = 18,
            physAtkBonus = 12,
            magicAtkBonus = 6,
            armorPenBonus = 10,
            magicPenBonus = 10,
            hpBonus = 20,
            sellPrice = 75
        });

        RegisterItem(new ItemDefinition
        {
            id = "shield_aegis_01",
            nameVi = "Khiên Vệ Binh Hoàng Gia",
            nameEn = "Royal Aegis Shield",
            descVi = "Khiên phòng ngự kiên cố đúc từ thép cứng, giảm đáng kể sát thương từ các đòn đánh trực diện.",
            descEn = "Sturdy defensive shield forged from tempered steel, reduces direct physical impact.",
            category = ItemCategory.Equipment,
            rarity = ItemRarity.Rare,
            iconPath = "Assets/ART/Item/Shield All/01.png",
            sourceVi = "Rơi từ Rương Báu Map 1",
            sourceEn = "Map 1 Chest Drop",
            defBonus = 15,
            hpBonus = 40,
            sellPrice = 80
        });

        RegisterItem(new ItemDefinition
        {
            id = "shield_dragon_06",
            nameVi = "Khiên Long Thần Cổ Đại",
            nameEn = "Ancient Dragoncrest Shield",
            descVi = "Tấm khiên làm từ vảy rồng ngàn năm, mang khả năng kháng sát thương ma thuật tuyệt đối.",
            descEn = "Crafted from primordial dragon scales, grants tremendous defense and magical resistance.",
            category = ItemCategory.Equipment,
            rarity = ItemRarity.Legendary,
            iconPath = "Assets/ART/Item/Shield All/06.png",
            sourceVi = "Gacha Thần Trang / Boss Map 3",
            sourceEn = "Gear Gacha / Map 3 Boss",
            defBonus = 28,
            hpBonus = 90,
            sellPrice = 220
        });

    }

    private static void RegisterItem(ItemDefinition item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return;
        itemsDict[item.id] = item;
        allItemsList.Add(item);
    }

    public static ItemDefinition GetItem(string itemId)
    {
        Initialize();
        if (string.IsNullOrEmpty(itemId)) return null;
        itemsDict.TryGetValue(itemId, out var def);
        return def;
    }

    public static List<ItemDefinition> GetAllItems()
    {
        Initialize();
        return allItemsList;
    }

    public static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return new Color(0.75f, 0.75f, 0.8f);
            case ItemRarity.Rare: return new Color(0.2f, 0.65f, 1f);
            case ItemRarity.Epic: return new Color(0.75f, 0.35f, 1f);
            case ItemRarity.Legendary: return new Color(1f, 0.82f, 0.15f);
            case ItemRarity.Mythic: return new Color(1f, 0.28f, 0.35f);
            default: return Color.white;
        }
    }

    public static string GetRarityName(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return LocalizationManager.T("PHỔ THÔNG", "COMMON");
            case ItemRarity.Rare: return LocalizationManager.T("HIẾM", "RARE");
            case ItemRarity.Epic: return LocalizationManager.T("SỬ THI", "EPIC");
            case ItemRarity.Legendary: return LocalizationManager.T("HUYỀN THOẠI", "LEGENDARY");
            case ItemRarity.Mythic: return LocalizationManager.T("THẦN THOẠI", "MYTHIC");
            default: return "COMMON";
        }
    }

    public static string GetCategoryName(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.All: return LocalizationManager.T("TẤT CẢ", "ALL");
            case ItemCategory.Equipment: return LocalizationManager.T("TRANG BỊ", "EQUIPMENT");
            case ItemCategory.Material: return LocalizationManager.T("KHOÁNG THẠCH", "MATERIALS");
            case ItemCategory.GachaArtifact: return LocalizationManager.T("LÕI THẦN BÍ", "ARTIFACTS");
            default: return "ALL";
        }
    }
}
