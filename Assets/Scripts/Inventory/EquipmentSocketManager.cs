// ========================================================
// VAELSTRIDER: RUINBOUND - Equipment Socket & Gem Infusion System
// Enables socketing mineral ores and gemstones into weapons & gear
// ========================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public static class EquipmentSocketManager
{
    public struct GemEffect
    {
        public string gemId;
        public string nameVi;
        public string nameEn;
        public int bonusAtk;
        public int bonusHp;
        public int bonusDef;
        public float lifestealPct;    // 0.05 = 5%
        public float cdReductionPct;  // 0.10 = 10%
        public float shardBonusPct;   // 0.35 = +35%
        public float hpRegenPerSec;   // 5 = +5 HP/s
        public string effectSummaryVi;
        public string effectSummaryEn;
        public Color gemColor;
    }

    private static Dictionary<string, GemEffect> gemEffects = null;

    public static string FormatCompactStats(int atk, int hp, int def, float lifestealPct = 0f, float cdRedPct = 0f, float shardPct = 0f, float regen = 0f)
    {
        List<string> parts = new List<string>();
        if (atk > 0) parts.Add($"+{atk} ATK");
        if (hp > 0) parts.Add($"+{hp} HP");
        if (def > 0) parts.Add($"+{def} DEF");
        if (lifestealPct > 0f) parts.Add($"+{Mathf.RoundToInt(lifestealPct * 100)}% Hút Máu");
        if (cdRedPct > 0f) parts.Add($"-{Mathf.RoundToInt(cdRedPct * 100)}% CD");
        if (shardPct > 0f) parts.Add($"+{Mathf.RoundToInt(shardPct * 100)}% Shard");
        if (regen > 0f) parts.Add($"+{regen} HP/s");
        return parts.Count > 0 ? string.Join(", ", parts) : "";
    }

    private static void EnsureGemEffects()
    {
        if (gemEffects != null) return;
        gemEffects = new Dictionary<string, GemEffect>
        {
            {
                "gem_bloodstone", new GemEffect
                {
                    gemId = "gem_bloodstone",
                    nameVi = "Huyết Thạch Ma Thuật",
                    nameEn = "Bloodstone Crystal",
                    bonusAtk = 18,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0.06f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+18 ATK, +6% Hút Máu",
                    effectSummaryEn = "+18 ATK, +6% Lifesteal",
                    gemColor = new Color(1f, 0.25f, 0.3f, 1f)
                }
            },
            {
                "gem_amethyst", new GemEffect
                {
                    gemId = "gem_amethyst",
                    nameVi = "Thạch Anh Tím Tàn Tích",
                    nameEn = "Ruin Amethyst Gem",
                    bonusAtk = 14,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0.08f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+14 ATK, -8% CD",
                    effectSummaryEn = "+14 ATK, -8% CD",
                    gemColor = new Color(0.75f, 0.35f, 1f, 1f)
                }
            },
            {
                "gem_jade", new GemEffect
                {
                    gemId = "gem_jade",
                    nameVi = "Ngọc Bích Thượng Cổ",
                    nameEn = "Ancient Jade Gem",
                    bonusAtk = 0,
                    bonusHp = 80,
                    bonusDef = 10,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 5f,
                    effectSummaryVi = "+80 HP, +10 DEF, +5 HP/s",
                    effectSummaryEn = "+80 HP, +10 DEF, +5 HP/s",
                    gemColor = new Color(0.25f, 1f, 0.45f, 1f)
                }
            },
            {
                "ore_gold_node", new GemEffect
                {
                    gemId = "ore_gold_node",
                    nameVi = "Quặng Vàng Nguyên Chất",
                    nameEn = "Pure Gold Node",
                    bonusAtk = 10,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0.35f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+10 ATK, +35% Shard",
                    effectSummaryEn = "+10 ATK, +35% Shards",
                    gemColor = new Color(1f, 0.85f, 0.2f, 1f)
                }
            },
            {
                "ore_blazing_coal", new GemEffect
                {
                    gemId = "ore_blazing_coal",
                    nameVi = "Than Đen Rực Lửa",
                    nameEn = "Blazing Dark Coal",
                    bonusAtk = 16,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+16 ATK",
                    effectSummaryEn = "+16 ATK",
                    gemColor = new Color(1f, 0.45f, 0.15f, 1f)
                }
            },
            {
                "ore_cyan_tin", new GemEffect
                {
                    gemId = "ore_cyan_tin",
                    nameVi = "Thiếc Xanh Mana",
                    nameEn = "Cyan Tin Ore",
                    bonusAtk = 8,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0.10f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+8 ATK, -10% CD",
                    effectSummaryEn = "+8 ATK, -10% CD",
                    gemColor = new Color(0.2f, 0.8f, 1f, 1f)
                }
            },
            {
                "ore_dark_iron", new GemEffect
                {
                    gemId = "ore_dark_iron",
                    nameVi = "Quặng Sắt Đen Cổ",
                    nameEn = "Dark Iron Ore",
                    bonusAtk = 6,
                    bonusHp = 40,
                    bonusDef = 18,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+6 ATK, +40 HP, +18 DEF",
                    effectSummaryEn = "+6 ATK, +40 HP, +18 DEF",
                    gemColor = new Color(0.55f, 0.65f, 0.85f, 1f)
                }
            },
            {
                "ore_forge_stone", new GemEffect
                {
                    gemId = "ore_forge_stone",
                    nameVi = "Đá Luyện Kim Bí Truyền",
                    nameEn = "Forge Stone Ore",
                    bonusAtk = 10,
                    bonusHp = 25,
                    bonusDef = 10,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+10 ATK, +25 HP, +10 DEF",
                    effectSummaryEn = "+10 ATK, +25 HP, +10 DEF",
                    gemColor = new Color(0.4f, 0.55f, 0.75f, 1f)
                }
            },
            {
                "ore_ancient_copper", new GemEffect
                {
                    gemId = "ore_ancient_copper",
                    nameVi = "Đồng Cổ Tàn Tích",
                    nameEn = "Ancient Copper Ore",
                    bonusAtk = 8,
                    bonusHp = 30,
                    bonusDef = 6,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+8 ATK, +30 HP, +6 DEF",
                    effectSummaryEn = "+8 ATK, +30 HP, +6 DEF",
                    gemColor = new Color(0.85f, 0.5f, 0.3f, 1f)
                }
            },
            {
                "ore_small_rocks", new GemEffect
                {
                    gemId = "ore_small_rocks",
                    nameVi = "Sỏi Cổ Tụ Linh",
                    nameEn = "Spirit Rune Pebbles",
                    bonusAtk = 5,
                    bonusHp = 20,
                    bonusDef = 4,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+5 ATK, +20 HP, +4 DEF",
                    effectSummaryEn = "+5 ATK, +20 HP, +4 DEF",
                    gemColor = new Color(0.7f, 0.7f, 0.75f, 1f)
                }
            },
            {
                "ore_stone", new GemEffect
                {
                    gemId = "ore_stone",
                    nameVi = "Đá Cổ Trầm Tích",
                    nameEn = "Sedimentary Ruin Stone",
                    bonusAtk = 4,
                    bonusHp = 15,
                    bonusDef = 8,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+4 ATK, +15 HP, +8 DEF",
                    effectSummaryEn = "+4 ATK, +15 HP, +8 DEF",
                    gemColor = new Color(0.6f, 0.6f, 0.65f, 1f)
                }
            },
            // =========================================================
            // VŨ KHÍ & KHIÊN (KHẢM TRỰC TIẾP VÀO 3 CHIÊU THỨC)
            // =========================================================
            {
                "wpn_legendary_sword", new GemEffect
                {
                    gemId = "wpn_legendary_sword",
                    nameVi = "Thánh Kiếm Huyền Thoại",
                    nameEn = "Mythic Excalibur Blade",
                    bonusAtk = 45,
                    bonusHp = 30,
                    bonusDef = 0,
                    lifestealPct = 0.04f,
                    cdReductionPct = 0.06f,
                    shardBonusPct = 0.20f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+45 ATK, +30 HP, +4% Hút Máu, -6% CD, +20% Shard",
                    effectSummaryEn = "+45 ATK, +30 HP, +4% Lifesteal, -6% CD, +20% Shard",
                    gemColor = new Color(1f, 0.28f, 0.35f, 1f)
                }
            },
            {
                "wpn_fire_sword", new GemEffect
                {
                    gemId = "wpn_fire_sword",
                    nameVi = "Hỏa Diệm Kiếm",
                    nameEn = "Flamebrand Broadsword",
                    bonusAtk = 32,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0.06f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+32 ATK, +6% Hút Máu",
                    effectSummaryEn = "+32 ATK, +6% Lifesteal",
                    gemColor = new Color(1f, 0.55f, 0.15f, 1f)
                }
            },
            {
                "wpn_katana", new GemEffect
                {
                    gemId = "wpn_katana",
                    nameVi = "Hắc Dạ Katana",
                    nameEn = "Shadow Katana",
                    bonusAtk = 24,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0.12f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+24 ATK, -12% CD",
                    effectSummaryEn = "+24 ATK, -12% CD",
                    gemColor = new Color(0.75f, 0.35f, 1f, 1f)
                }
            },
            {
                "wpn_paladin_sword", new GemEffect
                {
                    gemId = "wpn_paladin_sword",
                    nameVi = "Thánh Kiếm Hiệp Sĩ",
                    nameEn = "Paladin Holy Sword",
                    bonusAtk = 18,
                    bonusHp = 20,
                    bonusDef = 5,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+18 ATK, +20 HP, +5 DEF",
                    effectSummaryEn = "+18 ATK, +20 HP, +5 DEF",
                    gemColor = new Color(0.2f, 0.65f, 1f, 1f)
                }
            },
            {
                "shield_dragon_06", new GemEffect
                {
                    gemId = "shield_dragon_06",
                    nameVi = "Khiên Long Thần Cổ Đại",
                    nameEn = "Ancient Dragoncrest Shield",
                    bonusAtk = 0,
                    bonusHp = 90,
                    bonusDef = 28,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 6f,
                    effectSummaryVi = "+90 HP, +28 DEF, +6 HP/s",
                    effectSummaryEn = "+90 HP, +28 DEF, +6 HP/s",
                    gemColor = new Color(1f, 0.82f, 0.15f, 1f)
                }
            },
            {
                "shield_aegis_01", new GemEffect
                {
                    gemId = "shield_aegis_01",
                    nameVi = "Khiên Vệ Binh Hoàng Gia",
                    nameEn = "Royal Aegis Shield",
                    bonusAtk = 0,
                    bonusHp = 40,
                    bonusDef = 15,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 2f,
                    effectSummaryVi = "+40 HP, +15 DEF, +2 HP/s",
                    effectSummaryEn = "+40 HP, +15 DEF, +2 HP/s",
                    gemColor = new Color(0.2f, 0.65f, 1f, 1f)
                }
            },
            {
                "ore_keep_stone", new GemEffect
                {
                    gemId = "ore_keep_stone",
                    nameVi = "Đá Cổ Trấn Cung",
                    nameEn = "Ancient Keepstone",
                    bonusAtk = 5,
                    bonusHp = 25,
                    bonusDef = 12,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+5 ATK, +25 HP, +12 DEF",
                    effectSummaryEn = "+5 ATK, +25 HP, +12 DEF",
                    gemColor = new Color(0.6f, 0.65f, 0.75f, 1f)
                }
            },
            {
                "ore_tin", new GemEffect
                {
                    gemId = "ore_tin",
                    nameVi = "Quặng Thiếc Lam Hư Không",
                    nameEn = "Cyan Tin Ore",
                    bonusAtk = 8,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0.10f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+8 ATK, -10% CD",
                    effectSummaryEn = "+8 ATK, -10% CD",
                    gemColor = new Color(0.2f, 0.8f, 1f, 1f)
                }
            },
            {
                "ore_copper", new GemEffect
                {
                    gemId = "ore_copper",
                    nameVi = "Quặng Đồng Đỏ Cổ Xưa",
                    nameEn = "Ancient Copper Ore",
                    bonusAtk = 8,
                    bonusHp = 30,
                    bonusDef = 6,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+8 ATK, +30 HP, +6 DEF",
                    effectSummaryEn = "+8 ATK, +30 HP, +6 DEF",
                    gemColor = new Color(0.85f, 0.5f, 0.3f, 1f)
                }
            },
            {
                "ore_iron", new GemEffect
                {
                    gemId = "ore_iron",
                    nameVi = "Quặng Sắt Hắc Ám",
                    nameEn = "Dark Iron Ore",
                    bonusAtk = 6,
                    bonusHp = 40,
                    bonusDef = 18,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+6 ATK, +40 HP, +18 DEF",
                    effectSummaryEn = "+6 ATK, +40 HP, +18 DEF",
                    gemColor = new Color(0.55f, 0.65f, 0.85f, 1f)
                }
            },
            {
                "ore_coal", new GemEffect
                {
                    gemId = "ore_coal",
                    nameVi = "Hắc Than Nhiệt Lượng",
                    nameEn = "Blazing Dark Coal",
                    bonusAtk = 16,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+16 ATK",
                    effectSummaryEn = "+16 ATK",
                    gemColor = new Color(1f, 0.45f, 0.15f, 1f)
                }
            },
            {
                "ore_boulder_stone", new GemEffect
                {
                    gemId = "ore_boulder_stone",
                    nameVi = "Đá Tảng Cổ Vạn Năm",
                    nameEn = "Ancient Ruin Megalith",
                    bonusAtk = 8,
                    bonusHp = 50,
                    bonusDef = 15,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+8 ATK, +50 HP, +15 DEF",
                    effectSummaryEn = "+8 ATK, +50 HP, +15 DEF",
                    gemColor = new Color(0.5f, 0.6f, 0.7f, 1f)
                }
            },
            {
                "ore_medium_rock", new GemEffect
                {
                    gemId = "ore_medium_rock",
                    nameVi = "Đá Granite Tàn Tích",
                    nameEn = "Ruin Granite Ore",
                    bonusAtk = 6,
                    bonusHp = 25,
                    bonusDef = 8,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+6 ATK, +25 HP, +8 DEF",
                    effectSummaryEn = "+6 ATK, +25 HP, +8 DEF",
                    gemColor = new Color(0.65f, 0.65f, 0.7f, 1f)
                }
            },
            {
                "core_abyssal_soul", new GemEffect
                {
                    gemId = "core_abyssal_soul",
                    nameVi = "Lõi Linh Hồn Vực Thẳm",
                    nameEn = "Abyssal Soul Core",
                    bonusAtk = 50,
                    bonusHp = 250,
                    bonusDef = 35,
                    lifestealPct = 0.10f,
                    cdReductionPct = 0.10f,
                    shardBonusPct = 0.50f,
                    hpRegenPerSec = 15f,
                    effectSummaryVi = "+50 ATK, +250 HP, +35 DEF, +10% Hút Máu, -10% CD, +50% Shard, +15 HP/s",
                    effectSummaryEn = "+50 ATK, +250 HP, +35 DEF, +10% Lifesteal, -10% CD, +50% Shard, +15 HP/s",
                    gemColor = new Color(1f, 0.25f, 0.35f, 1f)
                }
            }
        };
    }

    public enum SkillSocketType
    {
        Attack = 0,   // Đòn Đánh Thường (Basic Attack)
        Dash = 1,     // Lướt Tốc Biến (Dash & Dodge)
        Ultimate = 2  // Tuyệt Kỹ (Ultimate)
    }

    public const int SKILL_COUNT = 3;
    public const int SUB_SOCKETS_PER_SKILL = 3;
    public const int MAX_GEM_LEVEL = int.MaxValue;

    public struct SocketedGemInfo
    {
        public string gemId;
        public int level; // 1, 2, 3, 4, 5... (No level cap)
        public GemEffect baseEffect;

        public int FinalAtk => Mathf.RoundToInt(baseEffect.bonusAtk * GetLevelMultiplier(level));
        public int FinalHp => Mathf.RoundToInt(baseEffect.bonusHp * GetLevelMultiplier(level));
        public int FinalDef => Mathf.RoundToInt(baseEffect.bonusDef * GetLevelMultiplier(level));
        public float FinalLifesteal => Mathf.Min(0.08f, baseEffect.lifestealPct * GetLevelMultiplier(level));
        public float FinalCdReduction => Mathf.Min(0.20f, baseEffect.cdReductionPct * GetLevelMultiplier(level));
        public float FinalShardBonus => Mathf.Min(0.40f, baseEffect.shardBonusPct * GetLevelMultiplier(level));
        public float FinalHpRegen => Mathf.Min(5.0f, baseEffect.hpRegenPerSec * GetLevelMultiplier(level));

        public string GetStarString()
        {
            return $"Lv.{level}";
        }
    }

    public static float GetLevelMultiplier(int level)
    {
        if (level <= 1) return 1.0f;
        // Diminishing returns curve (Sub-linear scaling):
        // Lv.1: 1.00x | Lv.2: 1.25x | Lv.3: 1.40x | Lv.5: 1.62x | Lv.10: 2.05x | Lv.40: 3.70x
        return 1.0f + Mathf.Pow(level - 1, 0.65f) * 0.25f;
    }

    public static int GetSkillSocketCount() => SKILL_COUNT;
    public static int GetSubSocketsPerSkill() => SUB_SOCKETS_PER_SKILL;

    public static string GetSkillNameVi(int skillIndex)
    {
        var cm = CharacterManager.EnsureInstance();
        string heroId = (cm != null) ? cm.GetSelectedCharacterId().ToLower() : "vaelstrider";
        bool isAzure = (heroId.Contains("azure") || heroId.Contains("player3"));
        bool isRonin = (heroId.Contains("ronin") || heroId.Contains("gale") || heroId.Contains("maid"));

        if (isRonin)
        {
            switch (skillIndex)
            {
                case 0: return "PHONG NGUYỆT TRẢM";
                case 1: return "PHONG THIỂM TỐC LƯỚT";
                case 2: return "ÁM ẢNH PHÂN THÂN TRẢM";
                default: return $"KỸ NĂNG #{skillIndex + 1}";
            }
        }
        else if (isAzure)
        {
            switch (skillIndex)
            {
                case 0: return "SONG BĂNG ĐOẢN KIẾM";
                case 1: return "HÀN BĂNG TRƯỢT TUYẾT";
                case 2: return "5 TIA BĂNG NGUYỆT SÓNG";
                default: return $"KỸ NĂNG #{skillIndex + 1}";
            }
        }
        else
        {
            switch (skillIndex)
            {
                case 0: return "ĐẠI KIẾM HOÀNG KIM";
                case 1: return "BỤI CỔ TỐC BIẾN";
                case 2: return "BÃO XOÁY CỔ KIẾM";
                default: return $"KỸ NĂNG #{skillIndex + 1}";
            }
        }
    }

    public static string GetSkillNameEn(int skillIndex)
    {
        var cm = CharacterManager.EnsureInstance();
        string heroId = (cm != null) ? cm.GetSelectedCharacterId().ToLower() : "vaelstrider";
        bool isAzure = (heroId.Contains("azure") || heroId.Contains("player3"));
        bool isRonin = (heroId.Contains("ronin") || heroId.Contains("gale") || heroId.Contains("maid"));

        if (isRonin)
        {
            switch (skillIndex)
            {
                case 0: return "GALE CRESCENT SWEEP";
                case 1: return "GALE FLASH DASH";
                case 2: return "SHADOW GALE STRIKE";
                default: return $"SKILL #{skillIndex + 1}";
            }
        }
        else if (isAzure)
        {
            switch (skillIndex)
            {
                case 0: return "TWIN FROST BLADES";
                case 1: return "FROST GLIDE DASH";
                case 2: return "5-WAY AZURE TEMPEST";
                default: return $"SKILL #{skillIndex + 1}";
            }
        }
        else
        {
            switch (skillIndex)
            {
                case 0: return "ANCIENT BROADSWORD";
                case 1: return "RUIN DUST DASH";
                case 2: return "RUIN WHIRLWIND";
                default: return $"SKILL #{skillIndex + 1}";
            }
        }
    }

    public static string GetSkillDescVi(int skillIndex)
    {
        var cm = CharacterManager.EnsureInstance();
        string heroId = (cm != null) ? cm.GetSelectedCharacterId().ToLower() : "vaelstrider";
        bool isAzure = (heroId.Contains("azure") || heroId.Contains("player3"));
        bool isRonin = (heroId.Contains("ronin") || heroId.Contains("gale") || heroId.Contains("maid"));

        if (isRonin)
        {
            switch (skillIndex)
            {
                case 0: return "Chém xoay 180° nạp Phong Ấn | Nhận hiệu ứng từ 3 ngọc khảm";
                case 1: return "Tốc lướt né đòn bất tử | Nhận hiệu ứng từ 3 ngọc khảm";
                case 2: return "Dịch chuyển phân thân trảm sát | Nhận hiệu ứng từ 3 ngọc khảm";
                default: return "";
            }
        }
        else if (isAzure)
        {
            switch (skillIndex)
            {
                case 0: return "Song kiếm băng bắn sóng kiếm xuyên thấu | Nhận hiệu ứng từ 3 ngọc khảm";
                case 1: return "Trượt băng tốc độ cao | Nhận hiệu ứng từ 3 ngọc khảm";
                case 2: return "Phóng 5 tia kiếm khí quét toàn map | Nhận hiệu ứng từ 3 ngọc khảm";
                default: return "";
            }
        }
        else
        {
            switch (skillIndex)
            {
                case 0: return "Combo 2 nhịp chém đại kiếm | Nhận hiệu ứng từ 3 ngọc khảm";
                case 1: return "Lướt né bất tử | Nhận hiệu ứng từ 3 ngọc khảm";
                case 2: return "Lốc xoáy bão kiếm hoàng kim | Nhận hiệu ứng từ 3 ngọc khảm";
                default: return "";
            }
        }
    }

    public static string GetSkillDescEn(int skillIndex)
    {
        var cm = CharacterManager.EnsureInstance();
        string heroId = (cm != null) ? cm.GetSelectedCharacterId().ToLower() : "vaelstrider";
        bool isAzure = (heroId.Contains("azure") || heroId.Contains("player3"));
        bool isRonin = (heroId.Contains("ronin") || heroId.Contains("gale") || heroId.Contains("maid"));

        if (isRonin)
        {
            switch (skillIndex)
            {
                case 0: return "180° Cleave stacking Wind Marks | Receives traits from 3 gems";
                case 1: return "Invincible Gale Flash Dash | Receives traits from 3 gems";
                case 2: return "Shadow Clone Execution Strike | Receives traits from 3 gems";
                default: return "";
            }
        }
        else if (isAzure)
        {
            switch (skillIndex)
            {
                case 0: return "Twin frost blades with piercing wave | Receives traits from 3 gems";
                case 1: return "High-speed frost glide | Receives traits from 3 gems";
                case 2: return "5-Way fan-spread sword tempests | Receives traits from 3 gems";
                default: return "";
            }
        }
        else
        {
            switch (skillIndex)
            {
                case 0: return "2-Hit broadsword combo | Receives traits from 3 gems";
                case 1: return "Invincible Dodge | Receives traits from 3 gems";
                case 2: return "Golden Ruin Whirlwind | Receives traits from 3 gems";
                default: return "";
            }
        }
    }

    public static string GetActiveHeroId()
    {
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId();
        if (string.IsNullOrEmpty(curHero))
        {
            curHero = PlayerPrefs.GetString("Selected_Character_ID", PlayerPrefs.GetString("SelectedHero", CharacterManager.CHAR_VAELSTRIDER));
        }
        if (curHero.Equals("titan", StringComparison.OrdinalIgnoreCase) || curHero.Equals("titanberserker", StringComparison.OrdinalIgnoreCase)) return CharacterManager.CHAR_TITAN_BERSERKER;
        if (curHero.Equals("azure", StringComparison.OrdinalIgnoreCase) || curHero.Equals("azureblade", StringComparison.OrdinalIgnoreCase)) return CharacterManager.CHAR_AZURE_BLADE;
        if (curHero.Equals("ronin", StringComparison.OrdinalIgnoreCase) || curHero.Equals("galeronin", StringComparison.OrdinalIgnoreCase) || curHero.Equals("maid", StringComparison.OrdinalIgnoreCase)) return CharacterManager.CHAR_GALE_RONIN;
        return CharacterManager.CHAR_VAELSTRIDER;
    }

    private static string GetSubSocketKey(int skillIndex, int subSlotIndex, string heroId = null)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            heroId = GetActiveHeroId();
        }
        return $"Hero_{heroId}_SkillSocket_Skill_{skillIndex}_Sub_{subSlotIndex}";
    }

    public static bool GetSkillSubSocketGem(int skillIndex, int subSlotIndex, out string gemId, out int level, string heroId = null)
    {
        gemId = "";
        level = 1;
        if (skillIndex < 0 || skillIndex >= SKILL_COUNT || subSlotIndex < 0 || subSlotIndex >= SUB_SOCKETS_PER_SKILL)
            return false;

        if (string.IsNullOrEmpty(heroId))
        {
            heroId = GetActiveHeroId();
        }

        string key = GetSubSocketKey(skillIndex, subSlotIndex, heroId);
        string raw = PlayerPrefs.GetString(key, "");

        // Backward compatibility: Migrate from legacy global key if empty for default hero
        if (string.IsNullOrEmpty(raw))
        {
            string legacyGlobalKey = $"SkillSocket_Skill_{skillIndex}_Sub_{subSlotIndex}";
            string legacyRaw = PlayerPrefs.GetString(legacyGlobalKey, "");
            if (!string.IsNullOrEmpty(legacyRaw) && heroId == CharacterManager.CHAR_VAELSTRIDER)
            {
                raw = legacyRaw;
                PlayerPrefs.SetString(key, raw);
                PlayerPrefs.DeleteKey(legacyGlobalKey);
                PlayerPrefs.Save();
            }
            else if (string.IsNullOrEmpty(legacyRaw) && subSlotIndex == 0)
            {
                string legacySingleKey = $"SkillSocket_Slot_{skillIndex}";
                string legacyGem = PlayerPrefs.GetString(legacySingleKey, "");
                if (!string.IsNullOrEmpty(legacyGem) && heroId == CharacterManager.CHAR_VAELSTRIDER)
                {
                    raw = legacyGem + ":1";
                    PlayerPrefs.SetString(key, raw);
                    PlayerPrefs.DeleteKey(legacySingleKey);
                    PlayerPrefs.Save();
                }
            }
        }

        if (string.IsNullOrEmpty(raw)) return false;

        if (raw.Contains(":"))
        {
            string[] parts = raw.Split(':');
            gemId = parts[0];
            if (parts.Length > 1 && int.TryParse(parts[1], out int parsedLv))
            {
                level = Mathf.Max(1, parsedLv);
            }
        }
        else
        {
            gemId = raw;
            level = 1;
        }

        return !string.IsNullOrEmpty(gemId);
    }

    public static string GetSkillSocketGem(int skillIndex)
    {
        // For simple lookup or compatibility, returns first socket's gem
        if (GetSkillSubSocketGem(skillIndex, 0, out string gemId, out int _))
        {
            return gemId;
        }
        return "";
    }

    public static List<SocketedGemInfo> GetSkillSocketedGems(int skillIndex)
    {
        EnsureGemEffects();
        List<SocketedGemInfo> list = new List<SocketedGemInfo>();
        for (int s = 0; s < SUB_SOCKETS_PER_SKILL; s++)
        {
            if (GetSkillSubSocketGem(skillIndex, s, out string gemId, out int level))
            {
                if (gemEffects.TryGetValue(gemId, out var eff))
                {
                    list.Add(new SocketedGemInfo
                    {
                        gemId = gemId,
                        level = level,
                        baseEffect = eff
                    });
                }
            }
        }
        return list;
    }

    public static bool IsGem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        EnsureGemEffects();
        if (gemEffects.ContainsKey(itemId)) return true;
        var def = InventoryItemDatabase.GetItem(itemId);
        return def != null && (def.category == InventoryItemDatabase.ItemCategory.Equipment || def.category == InventoryItemDatabase.ItemCategory.Material);
    }

    public static GemEffect? GetGemEffect(string gemId)
    {
        if (string.IsNullOrEmpty(gemId)) return null;
        EnsureGemEffects();
        if (gemEffects.TryGetValue(gemId, out var effect)) return effect;

        var def = InventoryItemDatabase.GetItem(gemId);
        if (def != null)
        {
            GemEffect dynEffect = new GemEffect
            {
                gemId = def.id,
                nameVi = def.nameVi,
                nameEn = def.nameEn,
                bonusAtk = def.atkBonus,
                bonusHp = def.hpBonus,
                bonusDef = def.defBonus,
                lifestealPct = 0f,
                cdReductionPct = 0f,
                shardBonusPct = 0f,
                hpRegenPerSec = 0f,
                gemColor = InventoryItemDatabase.GetRarityColor(def.rarity)
            };
            string compact = FormatCompactStats(dynEffect.bonusAtk, dynEffect.bonusHp, dynEffect.bonusDef);
            dynEffect.effectSummaryVi = !string.IsNullOrEmpty(compact) ? compact : (LocalizationManager.IsVietnamese ? def.descVi : def.descEn);
            dynEffect.effectSummaryEn = !string.IsNullOrEmpty(compact) ? compact : def.descEn;
            gemEffects[gemId] = dynEffect;
            return dynEffect;
        }

        return null;
    }

    public static List<GemEffect> GetAllGemDefinitions()
    {
        EnsureGemEffects();
        return new List<GemEffect>(gemEffects.Values);
    }

    /// <summary>
    /// Sockets a gem into a skill.
    /// Shape of Dream Mechanics:
    /// - If the skill already has this exact gemId: Combines and LEVELS UP the existing gem infinitely (Lv.1 -> Lv.2 -> Lv.3 -> Lv.4+), freeing the slot!
    /// - If targeting an empty sub-slot: Sockets as Lv.1.
    /// - If targeting a sub-slot with a different gem: Replaces it and returns old gem.
    /// </summary>
    public static bool SocketGemToSkill(int skillIndex, int preferredSubSlot, string gemId, out bool leveledUp, out int resultingLevel)
    {
        leveledUp = false;
        resultingLevel = 1;
        if (string.IsNullOrEmpty(gemId)) return false;
        EnsureGemEffects();
        if (!gemEffects.ContainsKey(gemId)) return false;

        // CASE A: User explicitly dropped onto a SPECIFIC sub-slot (preferredSubSlot >= 0)
        if (preferredSubSlot >= 0 && preferredSubSlot < SUB_SOCKETS_PER_SKILL)
        {
            // 1. If the item in this exact sub-slot is the SAME gem -> Merge & Level Up
            if (GetSkillSubSocketGem(skillIndex, preferredSubSlot, out string existingGem, out int existingLevel) && existingGem == gemId)
            {
                if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
                {
                    int newLevel = existingLevel + 1;
                    PlayerPrefs.SetString(GetSubSocketKey(skillIndex, preferredSubSlot), $"{gemId}:{newLevel}");
                    PlayerPrefs.Save();
                    leveledUp = true;
                    resultingLevel = newLevel;
                    Debug.Log($"<color=cyan>[GEM-MERGE]</color> Nâng cấp '{gemId}' trên Kỹ năng #{skillIndex} Ô #{preferredSubSlot + 1} lên Lv.{newLevel}!");
                    return true;
                }
                return false;
            }

            // 2. Otherwise: Replace this exact sub-slot, return previous item to bag
            if (GetSkillSubSocketGem(skillIndex, preferredSubSlot, out string oldGem, out int oldLevel))
            {
                AccountDatabase.AddItemToActiveUser(oldGem, oldLevel, "Material", "Tháo Ngọc Cũ");
            }

            if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
            {
                PlayerPrefs.SetString(GetSubSocketKey(skillIndex, preferredSubSlot), $"{gemId}:1");
                PlayerPrefs.Save();
                resultingLevel = 1;
                Debug.Log($"<color=green>[GEM-SOCKET]</color> Đã khảm '{gemId}' Lv.1 vào Kỹ năng #{skillIndex} Ô #{preferredSubSlot + 1}!");
                return true;
            }
            return false;
        }

        // CASE B: User clicked from list or dropped on whole card (preferredSubSlot == -1)
        // 1. Check if ANY sub-slot already contains this exact gem to merge
        for (int s = 0; s < SUB_SOCKETS_PER_SKILL; s++)
        {
            if (GetSkillSubSocketGem(skillIndex, s, out string exGem, out int exLv) && exGem == gemId)
            {
                if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
                {
                    int newLevel = exLv + 1;
                    PlayerPrefs.SetString(GetSubSocketKey(skillIndex, s), $"{gemId}:{newLevel}");
                    PlayerPrefs.Save();
                    leveledUp = true;
                    resultingLevel = newLevel;
                    return true;
                }
                return false;
            }
        }

        // 2. Find first empty slot, or fallback to slot 0
        int autoSlot = -1;
        for (int s = 0; s < SUB_SOCKETS_PER_SKILL; s++)
        {
            if (!GetSkillSubSocketGem(skillIndex, s, out _, out _))
            {
                autoSlot = s;
                break;
            }
        }
        if (autoSlot == -1) autoSlot = 0;

        if (GetSkillSubSocketGem(skillIndex, autoSlot, out string prevGem, out int prevLv))
        {
            AccountDatabase.AddItemToActiveUser(prevGem, prevLv, "Material", "Tháo Ngọc Cũ");
        }

        if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
        {
            PlayerPrefs.SetString(GetSubSocketKey(skillIndex, autoSlot), $"{gemId}:1");
            PlayerPrefs.Save();
            resultingLevel = 1;
            return true;
        }

        return false;
    }

    public static bool SocketGem(string targetId, int slotIndex, string gemId)
    {
        // Map slotIndex to skillIndex and subSlot 0 for backward compatibility
        int skillIdx = Mathf.Clamp(slotIndex, 0, SKILL_COUNT - 1);
        return SocketGemToSkill(skillIdx, -1, gemId, out _, out _);
    }

    public static bool UnsocketSubGem(int skillIndex, int subSlotIndex)
    {
        if (GetSkillSubSocketGem(skillIndex, subSlotIndex, out string gemId, out int level))
        {
            PlayerPrefs.DeleteKey(GetSubSocketKey(skillIndex, subSlotIndex));
            PlayerPrefs.Save();

            // Return full count based on level
            AccountDatabase.AddItemToActiveUser(gemId, level, "Material", "Tháo Ngọc");
            Debug.Log($"<color=yellow>[GEM-UNSOCKET]</color> Đã tháo ngọc '{gemId}' Lv.{level} (trả lại {level} viên) khỏi Kỹ năng #{skillIndex} Ô #{subSlotIndex}!");
            return true;
        }
        return false;
    }

    public static bool UnsocketGem(string targetId, int slotIndex)
    {
        int skillIdx = Mathf.Clamp(slotIndex, 0, SKILL_COUNT - 1);
        return UnsocketSubGem(skillIdx, 0);
    }

    public static bool MoveOrSwapSubGem(int fromSkill, int fromSub, int toSkill, int toSub)
    {
        bool hasA = GetSkillSubSocketGem(fromSkill, fromSub, out string gemA, out int lvA);
        bool hasB = GetSkillSubSocketGem(toSkill, toSub, out string gemB, out int lvB);
        if (!hasA) return false;

        // If moving to same gem type in target skill, merge infinitely!
        if (hasB && gemA == gemB)
        {
            int combinedLv = lvA + lvB;
            PlayerPrefs.SetString(GetSubSocketKey(toSkill, toSub), $"{gemA}:{combinedLv}");
            PlayerPrefs.DeleteKey(GetSubSocketKey(fromSkill, fromSub));
            PlayerPrefs.Save();
            return true;
        }

        PlayerPrefs.SetString(GetSubSocketKey(toSkill, toSub), $"{gemA}:{lvA}");
        if (hasB)
        {
            PlayerPrefs.SetString(GetSubSocketKey(fromSkill, fromSub), $"{gemB}:{lvB}");
        }
        else
        {
            PlayerPrefs.DeleteKey(GetSubSocketKey(fromSkill, fromSub));
        }
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>
    /// Đếm tổng số lượng đá quý trong túi có thể hấp thu vào các ô đang gắn.
    /// </summary>
    public static int GetTotalAvailableMergeGemsCount()
    {
        int total = 0;
        HashSet<string> countedGems = new HashSet<string>();
        for (int k = 0; k < SKILL_COUNT; k++)
        {
            for (int s = 0; s < SUB_SOCKETS_PER_SKILL; s++)
            {
                if (GetSkillSubSocketGem(k, s, out string gemId, out _))
                {
                    if (!countedGems.Contains(gemId))
                    {
                        countedGems.Add(gemId);
                        total += AccountDatabase.GetActiveUserItemCount(gemId);
                    }
                }
            }
        }
        return total;
    }

    /// <summary>
    /// Hấp thu toàn bộ khoáng thạch & đá quý trong túi đồ vào 9 ô ngọc đang gắn trên 3 kỹ năng.
    /// </summary>
    public static int AutoMergeAllMatchingGems(out int upgradedSlotsCount, out int totalLevelsGained)
    {
        upgradedSlotsCount = 0;
        totalLevelsGained = 0;
        int totalItemsConsumed = 0;

        for (int k = 0; k < SKILL_COUNT; k++)
        {
            for (int s = 0; s < SUB_SOCKETS_PER_SKILL; s++)
            {
                if (GetSkillSubSocketGem(k, s, out string gemId, out int currentLevel))
                {
                    int countInBag = AccountDatabase.GetActiveUserItemCount(gemId);
                    if (countInBag > 0)
                    {
                        if (AccountDatabase.RemoveItemFromActiveUser(gemId, countInBag))
                        {
                            int newLevel = currentLevel + countInBag;
                            PlayerPrefs.SetString(GetSubSocketKey(k, s), $"{gemId}:{newLevel}");
                            upgradedSlotsCount++;
                            totalLevelsGained += countInBag;
                            totalItemsConsumed += countInBag;
                            Debug.Log($"<color=cyan>[AUTO-MERGE]</color> Kỹ năng #{k + 1} Ô #{s + 1} ({gemId}) đã nuốt +{countInBag} viên -> Đạt Lv.{newLevel}!");
                        }
                    }
                }
            }
        }

        if (totalItemsConsumed > 0)
        {
            PlayerPrefs.Save();
            AccountDatabase.SaveActiveSessionToDatabase();
        }

        return totalItemsConsumed;
    }

    public static void GetSkillSocketBonus(int skillIndex, out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec)
    {
        bonusAtk = 0;
        bonusHp = 0;
        bonusDef = 0;
        lifestealPct = 0f;
        cdReductionPct = 0f;
        shardBonusPct = 0f;
        hpRegenPerSec = 0f;

        EnsureGemEffects();
        for (int s = 0; s < SUB_SOCKETS_PER_SKILL; s++)
        {
            if (GetSkillSubSocketGem(skillIndex, s, out string gemId, out int level))
            {
                if (gemEffects.TryGetValue(gemId, out var effect))
                {
                    float mult = GetLevelMultiplier(level);
                    bonusAtk += Mathf.RoundToInt(effect.bonusAtk * mult);
                    bonusHp += Mathf.RoundToInt(effect.bonusHp * mult);
                    bonusDef += Mathf.RoundToInt(effect.bonusDef * mult);
                    lifestealPct += effect.lifestealPct * mult;
                    cdReductionPct += effect.cdReductionPct * mult;
                    shardBonusPct += effect.shardBonusPct * mult;
                    hpRegenPerSec += effect.hpRegenPerSec * mult;
                }
            }
        }
        // Per-skill Hard Caps
        lifestealPct = Mathf.Min(0.12f, lifestealPct);
        cdReductionPct = Mathf.Min(0.25f, cdReductionPct);
        shardBonusPct = Mathf.Min(0.50f, shardBonusPct);
        hpRegenPerSec = Mathf.Min(8.0f, hpRegenPerSec);
    }

    public static void GetActiveEquippedSocketBonus(out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec)
    {
        bonusAtk = 0;
        bonusHp = 0;
        bonusDef = 0;
        lifestealPct = 0f;
        cdReductionPct = 0f;
        shardBonusPct = 0f;
        hpRegenPerSec = 0f;

        for (int k = 0; k < SKILL_COUNT; k++)
        {
            GetSkillSocketBonus(k, out int kAtk, out int kHp, out int kDef, out float kLife, out float kCd, out float kShard, out float kRegen);
            bonusAtk += kAtk;
            bonusHp += kHp;
            bonusDef += kDef;
            lifestealPct += kLife;
            cdReductionPct += kCd;
            shardBonusPct += kShard;
            hpRegenPerSec += kRegen;
        }

        // Global Hard Caps (Trần chỉ số toàn diện ngăn lạm phát)
        lifestealPct = Mathf.Min(0.18f, lifestealPct);       // Tối đa 18% Hút Máu
        cdReductionPct = Mathf.Min(0.40f, cdReductionPct);   // Tối đa 40% Giảm CD
        shardBonusPct = Mathf.Min(1.00f, shardBonusPct);     // Tối đa +100% Shards
        hpRegenPerSec = Mathf.Min(15.0f, hpRegenPerSec);     // Tối đa 15 HP/s Hồi máu
    }

    public static string GetSocketedGem(string targetId, int slotIndex)
    {
        int skillIdx = Mathf.Clamp(slotIndex, 0, SKILL_COUNT - 1);
        return GetSkillSocketGem(skillIdx);
    }

    public static bool MoveOrSwapGem(string targetId, int fromSlot, int toSlot)
    {
        int fromSkill = Mathf.Clamp(fromSlot, 0, SKILL_COUNT - 1);
        int toSkill = Mathf.Clamp(toSlot, 0, SKILL_COUNT - 1);
        return MoveOrSwapSubGem(fromSkill, 0, toSkill, 0);
    }

    public static void GetEquipmentTotalSocketBonus(string equipId, InventoryItemDatabase.ItemRarity rarity, out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec)
    {
        GetActiveEquippedSocketBonus(out bonusAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec);
    }

    public static int GetSocketCount(InventoryItemDatabase.ItemRarity rarity)
    {
        return 3;
    }
}
