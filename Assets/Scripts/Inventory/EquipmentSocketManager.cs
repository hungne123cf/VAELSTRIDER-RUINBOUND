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
        public int bonusAtk;          // Tổng công kích (fallback / legacy)
        public int bonusPhysAtk;      // Sát thương Vật Lý (+Physical ATK)
        public int bonusMagicAtk;     // Sát thương Phép (+Magic ATK)
        public int bonusTrueAtk;      // Sát thương Chuẩn (+True Damage)
        public int bonusHp;
        public int bonusDef;
        public float lifestealPct;    // 0.05 = 5% (tương thích ngược)
        public float physLifestealPct; // Hút máu vật lý (+Phys Lifesteal)
        public float spellVampPct;     // Hút máu phép (+Spell Vamp)
        public float critChancePct;    // Tỉ lệ chí mạng (+CRIT)
        public float cdReductionPct;  // 0.10 = 10%
        public float shardBonusPct;   // 0.35 = +35%
        public float hpRegenPerSec;   // 5 = +5 HP/s
        public float bonusMaxEnergy;  // +25 Max Mana
        public float energyRegenPerSec; // +1.5 Mana/s
        public string effectSummaryVi;
        public string effectSummaryEn;
        public Color gemColor;
    }

    private static Dictionary<string, GemEffect> gemEffects = null;

    public static string FormatColoredStatSummary(string rawSummary)
    {
        if (string.IsNullOrEmpty(rawSummary)) return "";

        string[] parts = rawSummary.Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);
        List<string> formattedParts = new List<string>();

        foreach (string part in parts)
        {
            string p = part.Trim();
            if (p.Contains("HP/s"))
            {
                formattedParts.Add($"<color=#10B981>{p}</color>");
            }
            else if (p.Contains("Mana/s"))
            {
                formattedParts.Add($"<color=#06B6D4>{p}</color>");
            }
            else if (p.Contains("Mana"))
            {
                formattedParts.Add($"<color=#00E5FF>{p}</color>");
            }
            else if (p.Contains("ATK"))
            {
                formattedParts.Add($"<color=#FF3B30>{p}</color>");
            }
            else if (p.Contains("AP"))
            {
                formattedParts.Add($"<color=#B84DFF>{p}</color>");
            }
            else if (p.Contains("TD"))
            {
                formattedParts.Add($"<color=#E2E8F0>{p}</color>");
            }
            else if (p.Contains("HP"))
            {
                formattedParts.Add($"<color=#22C55E>{p}</color>");
            }
            else if (p.Contains("DEF"))
            {
                formattedParts.Add($"<color=#38BDF8>{p}</color>");
            }
            else if (p.Contains("CD"))
            {
                formattedParts.Add($"<color=#C084FC>{p}</color>");
            }
            else if (p.Contains("Spell Vamp") || p.Contains("Hút Máu Phép"))
            {
                formattedParts.Add($"<color=#E879F9>{p}</color>");
            }
            else if (p.Contains("Phys Lifesteal") || p.Contains("Hút Máu VL") || p.Contains("Lifesteal") || p.Contains("Hút Máu"))
            {
                formattedParts.Add($"<color=#F43F5E>{p}</color>");
            }
            else if (p.Contains("Shard"))
            {
                formattedParts.Add($"<color=#FBBF24>{p}</color>");
            }
            else if (p.Contains("SPD"))
            {
                formattedParts.Add($"<color=#38BDF8>{p}</color>");
            }
            else if (p.Contains("CRIT"))
            {
                formattedParts.Add($"<color=#F59E0B>{p}</color>");
            }
            else
            {
                formattedParts.Add(p);
            }
        }

        return string.Join(", ", formattedParts);
    }

    public static string FormatCompactStats(int atk, int hp, int def, float lifestealPct = 0f, float cdRedPct = 0f, float shardPct = 0f, float regen = 0f, float bonusEnergy = 0f, float energyRegen = 0f, int physAtk = 0, int magicAtk = 0, int trueAtk = 0)
    {
        List<string> parts = new List<string>();
        if (physAtk > 0) parts.Add($"<color=#FF3B30>+{physAtk} ATK</color>");
        if (magicAtk > 0) parts.Add($"<color=#B84DFF>+{magicAtk} AP</color>");
        if (trueAtk > 0) parts.Add($"<color=#E2E8F0>+{trueAtk} TD</color>");
        if (physAtk == 0 && magicAtk == 0 && trueAtk == 0 && atk > 0) parts.Add($"<color=#FF3B30>+{atk} ATK</color>");
        if (hp > 0) parts.Add($"<color=#22C55E>+{hp} HP</color>");
        if (def > 0) parts.Add($"<color=#38BDF8>+{def} DEF</color>");
        if (lifestealPct > 0f) parts.Add($"<color=#F43F5E>+{Mathf.RoundToInt(lifestealPct * 100)}% {(LocalizationManager.IsVietnamese ? "Hút Máu" : "Lifesteal")}</color>");
        if (cdRedPct > 0f) parts.Add($"<color=#C084FC>-{Mathf.RoundToInt(cdRedPct * 100)}% CD</color>");
        if (shardPct > 0f) parts.Add($"<color=#FBBF24>+{Mathf.RoundToInt(shardPct * 100)}% Shard</color>");
        if (regen > 0f) parts.Add($"<color=#10B981>+{regen} HP/s</color>");
        if (bonusEnergy > 0f) parts.Add($"<color=#00E5FF>+{Mathf.RoundToInt(bonusEnergy)} Mana</color>");
        if (energyRegen > 0f) parts.Add($"<color=#06B6D4>+{energyRegen:F1} Mana/s</color>");
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
                    bonusTrueAtk = 18,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0.06f,
                    physLifestealPct = 0.06f,
                    spellVampPct = 0f,
                    critChancePct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+18 TD, +6% Hút Máu VL",
                    effectSummaryEn = "+18 TD, +6% Phys Lifesteal",
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
                    bonusMagicAtk = 14,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    physLifestealPct = 0f,
                    spellVampPct = 0.06f,
                    critChancePct = 0f,
                    cdReductionPct = 0.08f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 20f,
                    energyRegenPerSec = 1.0f,
                    effectSummaryVi = "+14 AP, +6% Hút Máu Phép, -8% CD, +20 Mana, +1.0 Mana/s",
                    effectSummaryEn = "+14 AP, +6% Spell Vamp, -8% CD, +20 Mana, +1.0 Mana/s",
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
                    bonusMaxEnergy = 15f,
                    energyRegenPerSec = 0.6f,
                    effectSummaryVi = "+80 HP, +10 DEF, +5 HP/s, +15 Mana, +0.6 Mana/s",
                    effectSummaryEn = "+80 HP, +10 DEF, +5 HP/s, +15 Mana, +0.6 Mana/s",
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
                    bonusTrueAtk = 10,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0.35f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 0f,
                    energyRegenPerSec = 0f,
                    effectSummaryVi = "+10 TD, +35% Shards",
                    effectSummaryEn = "+10 TD, +35% Shards",
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
                    bonusPhysAtk = 16,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 0f,
                    energyRegenPerSec = 0f,
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
                    bonusMagicAtk = 8,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0.10f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 30f,
                    energyRegenPerSec = 1.8f,
                    effectSummaryVi = "+8 AP, -10% CD, +30 Mana, +1.8 Mana/s",
                    effectSummaryEn = "+8 AP, -10% CD, +30 Mana, +1.8 Mana/s",
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
                    bonusPhysAtk = 6,
                    bonusHp = 40,
                    bonusDef = 18,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 0f,
                    energyRegenPerSec = 0f,
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
                    bonusPhysAtk = 10,
                    bonusHp = 25,
                    bonusDef = 10,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 0f,
                    energyRegenPerSec = 0f,
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
                    bonusPhysAtk = 8,
                    bonusHp = 30,
                    bonusDef = 6,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 0f,
                    energyRegenPerSec = 0f,
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
                    bonusMagicAtk = 5,
                    bonusHp = 20,
                    bonusDef = 4,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    bonusMaxEnergy = 15f,
                    energyRegenPerSec = 0.8f,
                    effectSummaryVi = "+5 AP, +20 HP, +4 DEF, +15 Mana, +0.8 Mana/s",
                    effectSummaryEn = "+5 AP, +20 HP, +4 DEF, +15 Mana, +0.8 Mana/s",
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
                    bonusPhysAtk = 4,
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
                    bonusPhysAtk = 30,
                    bonusTrueAtk = 15,
                    bonusHp = 30,
                    bonusDef = 0,
                    lifestealPct = 0.04f,
                    physLifestealPct = 0.04f,
                    spellVampPct = 0f,
                    critChancePct = 0.12f,
                    cdReductionPct = 0.06f,
                    shardBonusPct = 0.20f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+30 ATK, +15 TD, +30 HP, +12% CRIT, +4% Hút Máu VL, -6% CD, +20% Shard",
                    effectSummaryEn = "+30 ATK, +15 TD, +30 HP, +12% CRIT, +4% Phys Lifesteal, -6% CD, +20% Shard",
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
                    bonusPhysAtk = 16,
                    bonusMagicAtk = 16,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0.06f,
                    physLifestealPct = 0.06f,
                    spellVampPct = 0.06f,
                    critChancePct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+16 ATK, +16 AP, +6% Hút Máu VL, +6% Hút Máu Phép",
                    effectSummaryEn = "+16 ATK, +16 AP, +6% Phys Lifesteal, +6% Spell Vamp",
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
                    bonusPhysAtk = 24,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    physLifestealPct = 0f,
                    spellVampPct = 0f,
                    critChancePct = 0.15f,
                    cdReductionPct = 0.12f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+24 ATK, -12% CD, +15% CRIT",
                    effectSummaryEn = "+24 ATK, -12% CD, +15% CRIT",
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
                    bonusPhysAtk = 12,
                    bonusMagicAtk = 6,
                    bonusHp = 20,
                    bonusDef = 5,
                    lifestealPct = 0f,
                    cdReductionPct = 0f,
                    shardBonusPct = 0f,
                    hpRegenPerSec = 0f,
                    effectSummaryVi = "+12 ATK, +6 AP, +20 HP, +5 DEF",
                    effectSummaryEn = "+12 ATK, +6 AP, +20 HP, +5 DEF",
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
                    bonusPhysAtk = 5,
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
                    bonusMagicAtk = 8,
                    bonusHp = 0,
                    bonusDef = 0,
                    lifestealPct = 0f,
                    cdReductionPct = 0.10f,
                    bonusMaxEnergy = 30f,
                    energyRegenPerSec = 1.8f,
                    effectSummaryVi = "+8 AP, -10% CD, +30 Mana, +1.8 Mana/s",
                    effectSummaryEn = "+8 AP, -10% CD, +30 Mana, +1.8 Mana/s",
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
                    bonusPhysAtk = 8,
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
                    bonusPhysAtk = 6,
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
                    bonusPhysAtk = 16,
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
                    bonusPhysAtk = 8,
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
                    bonusPhysAtk = 6,
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
                    bonusPhysAtk = 20,
                    bonusMagicAtk = 20,
                    bonusTrueAtk = 10,
                    bonusHp = 250,
                    bonusDef = 35,
                    lifestealPct = 0.10f,
                    cdReductionPct = 0.10f,
                    shardBonusPct = 0.50f,
                    hpRegenPerSec = 15f,
                    effectSummaryVi = "+20 ATK, +20 AP, +10 TD, +250 HP, +35 DEF, +10% Hút Máu, -10% CD",
                    effectSummaryEn = "+20 ATK, +20 AP, +10 TD, +250 HP, +35 DEF, +10% Lifesteal, -10% CD",
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
        public int FinalPhysAtk => Mathf.RoundToInt(baseEffect.bonusPhysAtk * GetLevelMultiplier(level));
        public int FinalMagicAtk => Mathf.RoundToInt(baseEffect.bonusMagicAtk * GetLevelMultiplier(level));
        public int FinalTrueAtk => Mathf.RoundToInt(baseEffect.bonusTrueAtk * GetLevelMultiplier(level));
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
                case 0: return "Chém xoay 180° nạp Phong Ấn";
                case 1: return "Tốc lướt né đòn bất tử";
                case 2: return "Dịch chuyển phân thân trảm sát";
                default: return "";
            }
        }
        else if (isAzure)
        {
            switch (skillIndex)
            {
                case 0: return "Song kiếm băng bắn sóng kiếm xuyên thấu";
                case 1: return "Trượt băng tốc độ cao";
                case 2: return "Phóng 5 tia kiếm khí quét toàn map";
                default: return "";
            }
        }
        else
        {
            switch (skillIndex)
            {
                case 0: return "Combo 2 nhịp chém đại kiếm";
                case 1: return "Lướt né bất tử";
                case 2: return "Lốc xoáy bão kiếm hoàng kim";
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
                case 0: return "180° Cleave stacking Wind Marks";
                case 1: return "Invincible Gale Flash Dash";
                case 2: return "Shadow Clone Execution Strike";
                default: return "";
            }
        }
        else if (isAzure)
        {
            switch (skillIndex)
            {
                case 0: return "Twin frost blades with piercing wave";
                case 1: return "High-speed frost glide";
                case 2: return "5-Way fan-spread sword tempests";
                default: return "";
            }
        }
        else
        {
            switch (skillIndex)
            {
                case 0: return "2-Hit broadsword combo";
                case 1: return "Invincible Dodge";
                case 2: return "Golden Ruin Whirlwind";
                default: return "";
            }
        }
    }

    public const int HERO_SOCKET_COUNT = 6;

    public static readonly string[] SlotNamesVi = new string[]
    {
        "VŨ KHÍ", "ÁO GIÁP", "GIÀY", "MŨ NÓN", "NHẪN", "HỘ PHÙ"
    };

    public static readonly string[] SlotNamesEn = new string[]
    {
        "WEAPON", "ARMOR", "BOOTS", "HELMET", "RING", "AMULET"
    };

    public static string NormalizeHeroId(string heroId)
    {
        string raw = heroId;
        if (string.IsNullOrEmpty(raw))
        {
            raw = CharacterManager.EnsureInstance()?.GetSelectedCharacterId();
        }
        if (string.IsNullOrEmpty(raw))
        {
            raw = PlayerPrefs.GetString("Selected_Character_ID", PlayerPrefs.GetString("SelectedHero", "vael"));
        }
        if (string.IsNullOrEmpty(raw)) raw = "vael";

        string h = raw.Trim().ToLowerInvariant();
        if (h.Contains("titan") || h.Contains("player4") || h.Contains("berserker")) return "titan";
        if (h.Contains("azure") || h.Contains("player3") || h.Contains("frost")) return "azure";
        if (h.Contains("ronin") || h.Contains("gale") || h.Contains("player2") || h.Contains("maid") || h.Contains("elysia")) return "ronin";
        if (h.Contains("dune") || h.Contains("player5") || h.Contains("assassin")) return "dune";
        if (h.Contains("mage") || h.Contains("player6") || h.Contains("celestial")) return "mage";
        return "vael";
    }

    private static string GetLegacyCharConstant(string normHero)
    {
        switch (normHero)
        {
            case "titan": return "TitanBerserker";
            case "azure": return "AzureBlade";
            case "ronin": return "GaleRonin";
            case "dune": return "DuneAssassin";
            case "mage": return "CelestialMage";
            default: return "Vaelstrider";
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
        if (curHero.Equals("dune", StringComparison.OrdinalIgnoreCase) || curHero.Equals("duneassassin", StringComparison.OrdinalIgnoreCase)) return CharacterManager.CHAR_DUNE_ASSASSIN;
        if (curHero.Equals("mage", StringComparison.OrdinalIgnoreCase) || curHero.Equals("celestialmage", StringComparison.OrdinalIgnoreCase) || curHero.Equals("player6", StringComparison.OrdinalIgnoreCase)) return CharacterManager.CHAR_CELESTIAL_MAGE;
        return CharacterManager.CHAR_VAELSTRIDER;
    }

    public static string GetHeroSocketKey(int slotIndex, string heroId = null)
    {
        string normHero = NormalizeHeroId(heroId);
        return $"Hero_{normHero}_GearSlot_{slotIndex}";
    }

    private static string GetSubSocketKey(int skillIndex, int subSlotIndex, string heroId = null)
    {
        string normHero = NormalizeHeroId(heroId);
        return $"Hero_{normHero}_SkillSocket_Skill_{skillIndex}_Sub_{subSlotIndex}";
    }

    public static bool GetHeroSocketGem(int slotIndex, out string gemId, out int level, string heroId = null)
    {
        gemId = "";
        level = 1;
        if (slotIndex < 0 || slotIndex >= HERO_SOCKET_COUNT) return false;

        string normHero = NormalizeHeroId(heroId);
        string key = $"Hero_{normHero}_GearSlot_{slotIndex}";
        string raw = PlayerPrefs.GetString(key, "");

        // Auto migration from all possible key variations if normalized key is empty
        if (string.IsNullOrEmpty(raw))
        {
            string legacyConst = GetLegacyCharConstant(normHero);
            string[] possibleVariants = new string[]
            {
                !string.IsNullOrEmpty(heroId) ? $"Hero_{heroId}_GearSlot_{slotIndex}" : null,
                $"Hero_{legacyConst}_GearSlot_{slotIndex}",
                $"Hero_{normHero.ToUpper()}_GearSlot_{slotIndex}",
                $"Hero_{char.ToUpper(normHero[0]) + normHero.Substring(1)}_GearSlot_{slotIndex}",
                $"Hero_{legacyConst.ToLower()}_GearSlot_{slotIndex}"
            };

            foreach (string varKey in possibleVariants)
            {
                if (!string.IsNullOrEmpty(varKey) && varKey != key)
                {
                    string varVal = PlayerPrefs.GetString(varKey, "");
                    if (!string.IsNullOrEmpty(varVal))
                    {
                        raw = varVal;
                        PlayerPrefs.SetString(key, raw);
                        PlayerPrefs.DeleteKey(varKey);
                        PlayerPrefs.Save();
                        break;
                    }
                }
            }
        }

        // Auto migration from legacy sub-socket keys if still empty
        if (string.IsNullOrEmpty(raw))
        {
            int oldSkill = slotIndex / SUB_SOCKETS_PER_SKILL;
            int oldSub = slotIndex % SUB_SOCKETS_PER_SKILL;
            string oldKey = $"Hero_{normHero}_SkillSocket_Skill_{oldSkill}_Sub_{oldSub}";
            string oldRaw = PlayerPrefs.GetString(oldKey, "");
            if (!string.IsNullOrEmpty(oldRaw))
            {
                raw = oldRaw;
                PlayerPrefs.SetString(key, raw);
                PlayerPrefs.DeleteKey(oldKey);
                PlayerPrefs.Save();
            }
            else if (slotIndex < SKILL_COUNT)
            {
                string legacyKey = $"Hero_{normHero}_SkillSocket_Skill_{slotIndex}_Sub_0";
                string legacyRaw = PlayerPrefs.GetString(legacyKey, "");
                if (!string.IsNullOrEmpty(legacyRaw))
                {
                    raw = legacyRaw;
                    PlayerPrefs.SetString(key, raw);
                    PlayerPrefs.DeleteKey(legacyKey);
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

    public static bool GetSkillSubSocketGem(int skillIndex, int subSlotIndex, out string gemId, out int level, string heroId = null)
    {
        gemId = "";
        level = 1;
        if (skillIndex < 0 || skillIndex >= SKILL_COUNT || subSlotIndex < 0 || subSlotIndex >= SUB_SOCKETS_PER_SKILL)
            return false;

        // Map skillIndex & subSlotIndex to 6 hero slots for unified state
        int mappedHeroSlot = (skillIndex * 2) + (subSlotIndex % 2);
        if (mappedHeroSlot < HERO_SOCKET_COUNT)
        {
            return GetHeroSocketGem(mappedHeroSlot, out gemId, out level, heroId);
        }

        if (string.IsNullOrEmpty(heroId))
        {
            heroId = GetActiveHeroId();
        }

        string key = GetSubSocketKey(skillIndex, subSlotIndex, heroId);
        string raw = PlayerPrefs.GetString(key, "");

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
        if (GetHeroSocketGem(skillIndex, out string gemId, out int _))
        {
            return gemId;
        }
        return "";
    }

    public static List<SocketedGemInfo> GetHeroSocketedGems(string heroId = null)
    {
        EnsureGemEffects();
        List<SocketedGemInfo> list = new List<SocketedGemInfo>();
        for (int s = 0; s < HERO_SOCKET_COUNT; s++)
        {
            if (GetHeroSocketGem(s, out string gemId, out int level, heroId))
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

    public static bool SocketGemToHeroSlot(int slotIndex, string gemId, out bool leveledUp, out int resultingLevel, string heroId = null)
    {
        leveledUp = false;
        resultingLevel = 1;
        if (string.IsNullOrEmpty(gemId)) return false;
        EnsureGemEffects();
        if (!gemEffects.ContainsKey(gemId)) return false;
        if (string.IsNullOrEmpty(heroId)) heroId = GetActiveHeroId();

        // 1. If targeting specific slot (0..5)
        if (slotIndex >= 0 && slotIndex < HERO_SOCKET_COUNT)
        {
            if (GetHeroSocketGem(slotIndex, out string existingGem, out int existingLevel, heroId) && existingGem == gemId)
            {
                if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
                {
                    int newLevel = existingLevel + 1;
                    PlayerPrefs.SetString(GetHeroSocketKey(slotIndex, heroId), $"{gemId}:{newLevel}");
                    PlayerPrefs.Save();
                    leveledUp = true;
                    resultingLevel = newLevel;
                    Debug.Log($"<color=cyan>[HERO-SOCKET-MERGE]</color> Nâng cấp '{gemId}' Ô #{slotIndex + 1} ({SlotNamesVi[slotIndex]}) lên Lv.{newLevel}!");
                    return true;
                }
                return false;
            }

            if (GetHeroSocketGem(slotIndex, out string oldGem, out int oldLevel, heroId))
            {
                AccountDatabase.AddItemToActiveUser(oldGem, oldLevel, "Material", "Tháo Trang Bị Cũ");
            }

            if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
            {
                PlayerPrefs.SetString(GetHeroSocketKey(slotIndex, heroId), $"{gemId}:1");
                PlayerPrefs.Save();
                resultingLevel = 1;
                Debug.Log($"<color=green>[HERO-SOCKET]</color> Đã khảm '{gemId}' Lv.1 vào Ô #{slotIndex + 1} ({SlotNamesVi[slotIndex]})!");
                return true;
            }
            return false;
        }

        // 2. Auto Slot (First matching to merge, or first empty)
        for (int s = 0; s < HERO_SOCKET_COUNT; s++)
        {
            if (GetHeroSocketGem(s, out string exGem, out int exLv, heroId) && exGem == gemId)
            {
                if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
                {
                    int newLevel = exLv + 1;
                    PlayerPrefs.SetString(GetHeroSocketKey(s, heroId), $"{gemId}:{newLevel}");
                    PlayerPrefs.Save();
                    leveledUp = true;
                    resultingLevel = newLevel;
                    return true;
                }
                return false;
            }
        }

        int autoSlot = -1;
        for (int s = 0; s < HERO_SOCKET_COUNT; s++)
        {
            if (!GetHeroSocketGem(s, out _, out _, heroId))
            {
                autoSlot = s;
                break;
            }
        }
        if (autoSlot == -1) autoSlot = 0;

        if (GetHeroSocketGem(autoSlot, out string prevGem, out int prevLv, heroId))
        {
            AccountDatabase.AddItemToActiveUser(prevGem, prevLv, "Material", "Tháo Trang Bị Cũ");
        }

        if (AccountDatabase.RemoveItemFromActiveUser(gemId, 1))
        {
            PlayerPrefs.SetString(GetHeroSocketKey(autoSlot, heroId), $"{gemId}:1");
            PlayerPrefs.Save();
            resultingLevel = 1;
            return true;
        }

        return false;
    }

    public static bool UnsocketHeroSlot(int slotIndex, string heroId = null)
    {
        if (string.IsNullOrEmpty(heroId)) heroId = GetActiveHeroId();
        if (GetHeroSocketGem(slotIndex, out string gemId, out int level, heroId))
        {
            PlayerPrefs.DeleteKey(GetHeroSocketKey(slotIndex, heroId));
            PlayerPrefs.Save();
            AccountDatabase.AddItemToActiveUser(gemId, level, "Material", "Tháo Trang Bị");
            Debug.Log($"<color=yellow>[HERO-UNSOCKET]</color> Đã tháo '{gemId}' Lv.{level} (trả lại {level} viên) khỏi Ô #{slotIndex + 1} ({SlotNamesVi[slotIndex]})!");
            return true;
        }
        return false;
    }

    public static bool MoveOrSwapHeroSlot(int fromSlot, int toSlot, string heroId = null)
    {
        if (string.IsNullOrEmpty(heroId)) heroId = GetActiveHeroId();
        if (fromSlot < 0 || fromSlot >= HERO_SOCKET_COUNT || toSlot < 0 || toSlot >= HERO_SOCKET_COUNT) return false;
        if (fromSlot == toSlot) return false;

        bool hasA = GetHeroSocketGem(fromSlot, out string gemA, out int lvA, heroId);
        bool hasB = GetHeroSocketGem(toSlot, out string gemB, out int lvB, heroId);
        if (!hasA) return false;

        if (hasB && gemA == gemB)
        {
            int combinedLv = lvA + lvB;
            PlayerPrefs.SetString(GetHeroSocketKey(toSlot, heroId), $"{gemA}:{combinedLv}");
            PlayerPrefs.DeleteKey(GetHeroSocketKey(fromSlot, heroId));
            PlayerPrefs.Save();
            return true;
        }

        PlayerPrefs.SetString(GetHeroSocketKey(toSlot, heroId), $"{gemA}:{lvA}");
        if (hasB)
        {
            PlayerPrefs.SetString(GetHeroSocketKey(fromSlot, heroId), $"{gemB}:{lvB}");
        }
        else
        {
            PlayerPrefs.DeleteKey(GetHeroSocketKey(fromSlot, heroId));
        }
        PlayerPrefs.Save();
        return true;
    }

    public static void GetHeroSocketBonus(int slotIndex, out int bonusAtk, out int bonusPhysAtk, out int bonusMagicAtk, out int bonusTrueAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec, out float physLifePct, out float spellVampPct, out float critChancePct, string heroId = null)
    {
        bonusAtk = 0;
        bonusPhysAtk = 0;
        bonusMagicAtk = 0;
        bonusTrueAtk = 0;
        bonusHp = 0;
        bonusDef = 0;
        lifestealPct = 0f;
        cdReductionPct = 0f;
        shardBonusPct = 0f;
        hpRegenPerSec = 0f;
        bonusMaxEnergy = 0f;
        energyRegenPerSec = 0f;
        physLifePct = 0f;
        spellVampPct = 0f;
        critChancePct = 0f;

        if (slotIndex < 0 || slotIndex >= HERO_SOCKET_COUNT) return;
        EnsureGemEffects();

        if (GetHeroSocketGem(slotIndex, out string gemId, out int level, heroId))
        {
            if (gemEffects.TryGetValue(gemId, out var effect))
            {
                float mult = GetLevelMultiplier(level);
                bonusAtk = Mathf.RoundToInt(effect.bonusAtk * mult);
                bonusPhysAtk = Mathf.RoundToInt(effect.bonusPhysAtk * mult);
                bonusMagicAtk = Mathf.RoundToInt(effect.bonusMagicAtk * mult);
                bonusTrueAtk = Mathf.RoundToInt(effect.bonusTrueAtk * mult);
                bonusHp = Mathf.RoundToInt(effect.bonusHp * mult);
                bonusDef = Mathf.RoundToInt(effect.bonusDef * mult);
                lifestealPct = effect.lifestealPct * mult;
                cdReductionPct = effect.cdReductionPct * mult;
                shardBonusPct = effect.shardBonusPct * mult;
                hpRegenPerSec = effect.hpRegenPerSec * mult;
                bonusMaxEnergy = effect.bonusMaxEnergy * mult;
                energyRegenPerSec = effect.energyRegenPerSec * mult;
                physLifePct = effect.physLifestealPct * mult;
                spellVampPct = effect.spellVampPct * mult;
                critChancePct = effect.critChancePct * mult;
            }
        }
    }

    public static void GetHeroSocketBonus(int slotIndex, out int bonusAtk, out int bonusPhysAtk, out int bonusMagicAtk, out int bonusTrueAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec, string heroId = null)
    {
        GetHeroSocketBonus(slotIndex, out bonusAtk, out bonusPhysAtk, out bonusMagicAtk, out bonusTrueAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out bonusMaxEnergy, out energyRegenPerSec, out _, out _, out _, heroId);
    }

    public static void GetHeroSocketBonus(int slotIndex, out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec, string heroId = null)
    {
        GetHeroSocketBonus(slotIndex, out bonusAtk, out _, out _, out _, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out bonusMaxEnergy, out energyRegenPerSec, heroId);
    }

    public static void GetTotalHeroSocketBonus(out int bonusAtk, out int bonusPhysAtk, out int bonusMagicAtk, out int bonusTrueAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec, out float physLifePct, out float spellVampPct, out float critChancePct, string heroId = null)
    {
        bonusAtk = 0;
        bonusPhysAtk = 0;
        bonusMagicAtk = 0;
        bonusTrueAtk = 0;
        bonusHp = 0;
        bonusDef = 0;
        lifestealPct = 0f;
        cdReductionPct = 0f;
        shardBonusPct = 0f;
        hpRegenPerSec = 0f;
        bonusMaxEnergy = 0f;
        energyRegenPerSec = 0f;
        physLifePct = 0f;
        spellVampPct = 0f;
        critChancePct = 0f;

        for (int s = 0; s < HERO_SOCKET_COUNT; s++)
        {
            GetHeroSocketBonus(s, out int sAtk, out int sPhys, out int sMagic, out int sTrue, out int sHp, out int sDef, out float sLife, out float sCd, out float sShard, out float sRegen, out float sMaxEn, out float sEnRegen, out float sPhysLife, out float sSpellVamp, out float sCrit, heroId);
            bonusAtk += sAtk;
            bonusPhysAtk += sPhys;
            bonusMagicAtk += sMagic;
            bonusTrueAtk += sTrue;
            bonusHp += sHp;
            bonusDef += sDef;
            lifestealPct += sLife;
            cdReductionPct += sCd;
            shardBonusPct += sShard;
            hpRegenPerSec += sRegen;
            bonusMaxEnergy += sMaxEn;
            energyRegenPerSec += sEnRegen;
            physLifePct += sPhysLife;
            spellVampPct += sSpellVamp;
            critChancePct += sCrit;
        }

        // Global Hard Caps
        lifestealPct = Mathf.Min(0.25f, lifestealPct);
        physLifePct = Mathf.Min(0.25f, physLifePct);
        spellVampPct = Mathf.Min(0.25f, spellVampPct);
        critChancePct = Mathf.Min(0.50f, critChancePct);
        cdReductionPct = Mathf.Min(0.40f, cdReductionPct);
        shardBonusPct = Mathf.Min(1.00f, shardBonusPct);
        hpRegenPerSec = Mathf.Min(15.0f, hpRegenPerSec);
        bonusMaxEnergy = Mathf.Min(100.0f, bonusMaxEnergy);
        energyRegenPerSec = Mathf.Min(6.0f, energyRegenPerSec);
    }

    public static void GetTotalHeroSocketBonus(out int bonusAtk, out int bonusPhysAtk, out int bonusMagicAtk, out int bonusTrueAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec, string heroId = null)
    {
        GetTotalHeroSocketBonus(out bonusAtk, out bonusPhysAtk, out bonusMagicAtk, out bonusTrueAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out bonusMaxEnergy, out energyRegenPerSec, out _, out _, out _, heroId);
    }

    public static void GetTotalHeroSocketBonus(out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec, string heroId = null)
    {
        GetTotalHeroSocketBonus(out bonusAtk, out _, out _, out _, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out bonusMaxEnergy, out energyRegenPerSec, heroId);
    }

    /// <summary>
    /// Sockets a gem into a skill or hero slot.
    /// </summary>
    public static bool SocketGemToSkill(int skillIndex, int preferredSubSlot, string gemId, out bool leveledUp, out int resultingLevel)
    {
        int targetSlot = (preferredSubSlot >= 0) ? (skillIndex * 2 + preferredSubSlot % 2) : skillIndex;
        if (targetSlot >= HERO_SOCKET_COUNT) targetSlot = HERO_SOCKET_COUNT - 1;
        return SocketGemToHeroSlot(targetSlot, gemId, out leveledUp, out resultingLevel);
    }

    public static bool SocketGem(string targetId, int slotIndex, string gemId)
    {
        int slotIdx = Mathf.Clamp(slotIndex, 0, HERO_SOCKET_COUNT - 1);
        return SocketGemToHeroSlot(slotIdx, gemId, out _, out _);
    }

    public static bool UnsocketSubGem(int skillIndex, int subSlotIndex)
    {
        int targetSlot = (skillIndex * 2) + (subSlotIndex % 2);
        if (targetSlot >= HERO_SOCKET_COUNT) targetSlot = HERO_SOCKET_COUNT - 1;
        return UnsocketHeroSlot(targetSlot);
    }

    public static bool UnsocketGem(string targetId, int slotIndex)
    {
        int slotIdx = Mathf.Clamp(slotIndex, 0, HERO_SOCKET_COUNT - 1);
        return UnsocketHeroSlot(slotIdx);
    }

    public static bool MoveOrSwapSubGem(int fromSkill, int fromSub, int toSkill, int toSub)
    {
        int fromSlot = (fromSkill * 2) + (fromSub % 2);
        int toSlot = (toSkill * 2) + (toSub % 2);
        return MoveOrSwapHeroSlot(fromSlot, toSlot);
    }

    /// <summary>
    /// Đếm tổng số lượng đá quý trong túi có thể hấp thu vào 6 ô trang bị đang gắn.
    /// </summary>
    public static int GetTotalAvailableMergeGemsCount(string heroId = null)
    {
        int total = 0;
        HashSet<string> countedGems = new HashSet<string>();
        for (int s = 0; s < HERO_SOCKET_COUNT; s++)
        {
            if (GetHeroSocketGem(s, out string gemId, out _, heroId))
            {
                if (!countedGems.Contains(gemId))
                {
                    countedGems.Add(gemId);
                    total += AccountDatabase.GetActiveUserItemCount(gemId);
                }
            }
        }
        return total;
    }

    /// <summary>
    /// Hấp thu toàn bộ khoáng thạch & đá quý trong túi đồ vào 6 ô trang bị đang gắn của tướng.
    /// </summary>
    public static int AutoMergeAllMatchingGems(out int upgradedSlotsCount, out int totalLevelsGained)
    {
        return AutoMergeAllMatchingGems(out upgradedSlotsCount, out totalLevelsGained, null);
    }

    public static int AutoMergeAllMatchingGems(out int upgradedSlotsCount, out int totalLevelsGained, string heroId)
    {
        upgradedSlotsCount = 0;
        totalLevelsGained = 0;
        int totalItemsConsumed = 0;
        if (string.IsNullOrEmpty(heroId)) heroId = GetActiveHeroId();

        for (int s = 0; s < HERO_SOCKET_COUNT; s++)
        {
            if (GetHeroSocketGem(s, out string gemId, out int currentLevel, heroId))
            {
                int countInBag = AccountDatabase.GetActiveUserItemCount(gemId);
                if (countInBag > 0)
                {
                    if (AccountDatabase.RemoveItemFromActiveUser(gemId, countInBag))
                    {
                        int newLevel = currentLevel + countInBag;
                        PlayerPrefs.SetString(GetHeroSocketKey(s, heroId), $"{gemId}:{newLevel}");
                        upgradedSlotsCount++;
                        totalLevelsGained += countInBag;
                        totalItemsConsumed += countInBag;
                        Debug.Log($"<color=cyan>[HERO-AUTO-MERGE]</color> Ô #{s + 1} ({gemId}) đã nuốt +{countInBag} viên -> Đạt Lv.{newLevel}!");
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
        GetSkillSocketBonus(skillIndex, out bonusAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out _, out _);
    }

    public static void GetSkillSocketBonus(int skillIndex, out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec)
    {
        int slotA = skillIndex * 2;
        int slotB = skillIndex * 2 + 1;
        GetHeroSocketBonus(slotA, out int aAtk, out int aHp, out int aDef, out float aLife, out float aCd, out float aShard, out float aRegen, out float aMaxEn, out float aEnRegen);
        GetHeroSocketBonus(slotB, out int bAtk, out int bHp, out int bDef, out float bLife, out float bCd, out float bShard, out float bRegen, out float bMaxEn, out float bEnRegen);

        bonusAtk = aAtk + bAtk;
        bonusHp = aHp + bHp;
        bonusDef = aDef + bDef;
        lifestealPct = Mathf.Min(0.12f, aLife + bLife);
        cdReductionPct = Mathf.Min(0.25f, aCd + bCd);
        shardBonusPct = Mathf.Min(0.50f, aShard + bShard);
        hpRegenPerSec = Mathf.Min(8.0f, aRegen + bRegen);
        bonusMaxEnergy = Mathf.Min(50.0f, aMaxEn + bMaxEn);
        energyRegenPerSec = Mathf.Min(3.0f, aEnRegen + bEnRegen);
    }

    public static void GetActiveEquippedSocketBonus(out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec)
    {
        GetActiveEquippedSocketBonus(out bonusAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out _, out _);
    }

    public static void GetActiveEquippedSocketBonus(out int bonusAtk, out int bonusPhysAtk, out int bonusMagicAtk, out int bonusTrueAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec, out float physLifePct, out float spellVampPct, out float critChancePct)
    {
        GetTotalHeroSocketBonus(out bonusAtk, out bonusPhysAtk, out bonusMagicAtk, out bonusTrueAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out bonusMaxEnergy, out energyRegenPerSec, out physLifePct, out spellVampPct, out critChancePct);
    }

    public static void GetActiveEquippedSocketBonus(out int bonusAtk, out int bonusPhysAtk, out int bonusMagicAtk, out int bonusTrueAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec)
    {
        GetTotalHeroSocketBonus(out bonusAtk, out bonusPhysAtk, out bonusMagicAtk, out bonusTrueAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out bonusMaxEnergy, out energyRegenPerSec);
    }

    public static void GetActiveEquippedSocketBonus(out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec, out float bonusMaxEnergy, out float energyRegenPerSec)
    {
        GetTotalHeroSocketBonus(out bonusAtk, out _, out _, out _, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec, out bonusMaxEnergy, out energyRegenPerSec);
    }

    public static string GetSocketedGem(string targetId, int slotIndex)
    {
        int slotIdx = Mathf.Clamp(slotIndex, 0, HERO_SOCKET_COUNT - 1);
        if (GetHeroSocketGem(slotIdx, out string gemId, out _)) return gemId;
        return "";
    }

    public static bool MoveOrSwapGem(string targetId, int fromSlot, int toSlot)
    {
        int from = Mathf.Clamp(fromSlot, 0, HERO_SOCKET_COUNT - 1);
        int to = Mathf.Clamp(toSlot, 0, HERO_SOCKET_COUNT - 1);
        return MoveOrSwapHeroSlot(from, to);
    }

    public static void GetEquipmentTotalSocketBonus(string equipId, InventoryItemDatabase.ItemRarity rarity, out int bonusAtk, out int bonusHp, out int bonusDef, out float lifestealPct, out float cdReductionPct, out float shardBonusPct, out float hpRegenPerSec)
    {
        GetActiveEquippedSocketBonus(out bonusAtk, out bonusHp, out bonusDef, out lifestealPct, out cdReductionPct, out shardBonusPct, out hpRegenPerSec);
    }

    public static int GetSocketCount(InventoryItemDatabase.ItemRarity rarity)
    {
        return HERO_SOCKET_COUNT;
    }
}
