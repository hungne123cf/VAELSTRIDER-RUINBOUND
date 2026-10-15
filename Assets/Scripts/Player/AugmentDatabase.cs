using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database tất cả các Augment (lựa chọn khi lên cấp).
/// Mỗi augment mô tả rõ loại, giá trị, icon emoji, tên VN/EN.
/// Phân theo nhân vật: General (tất cả) + per-hero specific.
/// </summary>
public static class AugmentDatabase
{
    public enum AugmentType
    {
        // Chỉ số cơ bản
        BonusAttack,
        BonusHealth,
        BonusSpeed,
        BonusCritChance,
        BonusCritDamage,
        BonusLifesteal,
        BonusSpellVamp,
        BonusCooldownReduction,
        BonusDefense,
        BonusMaxEnergy,

        // Phúc lợi đặc biệt (thời hạn / cơ chế riêng)
        VampireBlood,       // +5% lifesteal trong 45s
        RagingFire,         // +25% ATK trong 30s
        AncientShield,      // +200 HP shield trong 20s
        SwiftShadow,        // +40% tốc độ trong 20s
        ComboMastery,       // Combo reset nhanh hơn 50%
        BerserkerMode,      // Mỗi kill +2 ATK stack (max 20, mất khi chết)
        RegenerativeCore,   // Hồi 2 HP/giây trong 60s
        EchoStrike,         // 30% cơ hội đánh đúp không tốn cooldown
        SoulHarvest,        // Mỗi kill hồi 8 HP
        UltimateCharge,     // Giảm 30% cooldown Ulti ngay lập tức
    }

    public class AugmentData
    {
        public AugmentType type;
        public string nameVN;
        public string nameEN;
        public string descVN;
        public string descEN;
        public string emoji;          // Icon đại diện
        public float value;           // Giá trị chỉ số
        public float duration;        // -1 = vĩnh viễn
        public string[] heroFilter;   // null = tất cả nhân vật
        public Color cardColor;       // Màu viền card
        public Rarity rarity;

        public enum Rarity { Common, Rare, Epic }

        public string GetFormattedDesc(bool isVi, float remainingSec = -1f)
        {
            string raw = isVi ? descVN : descEN;
            if (string.IsNullOrEmpty(raw)) return "";
            string valStr = (value % 1 == 0) ? ((int)value).ToString() : value.ToString("0.#");
            raw = raw.Replace("{v}", valStr);
            if (duration > 0)
            {
                int sec = remainingSec >= 0f ? Mathf.Max(1, Mathf.CeilToInt(remainingSec)) : (int)duration;
                raw = raw.Replace("{d}", sec.ToString());
            }
            return raw;
        }
    }

    // ─── Pool tổng ─────────────────────────────────────────────────────────────
    private static readonly List<AugmentData> _all = new List<AugmentData>
    {
        // ── COMMON: Chỉ số cơ bản ──────────────────────────────────────────────
        new AugmentData {
            type = AugmentType.BonusAttack,
            nameVN = "Sức Mạnh Thô",        nameEN = "Raw Strength",
            descVN = "+{v} Sát Thương vật lý", descEN = "+{v} Physical Damage",
            emoji = "⚔️", value = 8f, duration = -1f,
            cardColor = new Color(0.9f, 0.3f, 0.2f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusAttack,
            nameVN = "Lưỡi Sắc Bén",        nameEN = "Razor Edge",
            descVN = "+{v} Sát Thương vật lý", descEN = "+{v} Physical Damage",
            emoji = "🗡️", value = 12f, duration = -1f,
            cardColor = new Color(0.9f, 0.3f, 0.2f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusHealth,
            nameVN = "Sinh Lực Dồi Dào",    nameEN = "Vitality Surge",
            descVN = "+{v} HP tối đa",        descEN = "+{v} Max HP",
            emoji = "❤️", value = 50f, duration = -1f,
            cardColor = new Color(0.2f, 0.8f, 0.3f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusHealth,
            nameVN = "Thân Thép",            nameEN = "Iron Body",
            descVN = "+{v} HP tối đa",        descEN = "+{v} Max HP",
            emoji = "🛡️", value = 80f, duration = -1f,
            cardColor = new Color(0.2f, 0.8f, 0.3f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusSpeed,
            nameVN = "Bước Chân Nhanh",      nameEN = "Fleet Foot",
            descVN = "+{v}% tốc độ di chuyển", descEN = "+{v}% Move Speed",
            emoji = "💨", value = 10f, duration = -1f,
            cardColor = new Color(0.3f, 0.7f, 1f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusCritChance,
            nameVN = "Mắt Cú Vọ",           nameEN = "Eagle Eye",
            descVN = "+{v}% tỉ lệ chí mạng", descEN = "+{v}% Crit Chance",
            emoji = "👁️", value = 8f, duration = -1f,
            cardColor = new Color(1f, 0.9f, 0.2f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusCritDamage,
            nameVN = "Đòn Tử Thần",         nameEN = "Death Strike",
            descVN = "+{v}% sát thương chí mạng", descEN = "+{v}% Crit Damage",
            emoji = "💥", value = 20f, duration = -1f,
            cardColor = new Color(1f, 0.5f, 0.1f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusLifesteal,
            nameVN = "Bấc Sinh Khí",        nameEN = "Life Drain",
            descVN = "+{v}% hút máu vật lý", descEN = "+{v}% Physical Lifesteal",
            emoji = "🩸", value = 5f, duration = -1f,
            cardColor = new Color(0.8f, 0.1f, 0.4f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusSpellVamp,
            nameVN = "Hồn Pháp Thuật",      nameEN = "Spell Vamp",
            descVN = "+{v}% hút máu phép thuật", descEN = "+{v}% Spell Vamp",
            emoji = "🌀", value = 5f, duration = -1f,
            cardColor = new Color(0.6f, 0.2f, 0.9f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusCooldownReduction,
            nameVN = "Tập Trung Năng Lượng", nameEN = "Focus Energy",
            descVN = "+{v}% giảm hồi chiêu", descEN = "+{v}% Cooldown Reduction",
            emoji = "⚡", value = 10f, duration = -1f,
            cardColor = new Color(0.2f, 0.9f, 0.9f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusDefense,
            nameVN = "Giáp Kiên Cố",        nameEN = "Fortified Armor",
            descVN = "+{v} giảm sát thương nhận vào", descEN = "+{v} Damage Reduction",
            emoji = "🧱", value = 10f, duration = -1f,
            cardColor = new Color(0.5f, 0.5f, 0.7f), rarity = AugmentData.Rarity.Common
        },

        // ── RARE: Phúc lợi đặc biệt ───────────────────────────────────────────
        new AugmentData {
            type = AugmentType.SoulHarvest,
            nameVN = "Thu Hoạch Linh Hồn",  nameEN = "Soul Harvest",
            descVN = "Mỗi khi hạ quái, hồi {v} HP", descEN = "Each kill restores {v} HP",
            emoji = "☠️", value = 8f, duration = -1f,
            cardColor = new Color(0.5f, 0f, 0.5f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.BerserkerMode,
            nameVN = "Điên Cuồng Chiến Đấu", nameEN = "Berserk Stack",
            descVN = "Mỗi kill +{v} ATK (tối đa 20 stack)", descEN = "Each kill +{v} ATK (max 20 stacks)",
            emoji = "😈", value = 2f, duration = -1f,
            cardColor = new Color(1f, 0.2f, 0f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.EchoStrike,
            nameVN = "Tiếng Vang Của Lưỡi Kiếm", nameEN = "Echo Strike",
            descVN = "{v}% cơ hội kích hoạt đòn đánh kép", descEN = "{v}% chance to double-hit",
            emoji = "🔁", value = 30f, duration = -1f,
            cardColor = new Color(0.8f, 0.8f, 0.1f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.RegenerativeCore,
            nameVN = "Lõi Tái Sinh",         nameEN = "Regenerative Core",
            descVN = "Hồi {v} HP/giây trong {d}s", descEN = "Regen {v} HP/sec for {d}s",
            emoji = "💚", value = 2f, duration = 60f,
            cardColor = new Color(0.1f, 0.8f, 0.4f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.UltimateCharge,
            nameVN = "Nạp Tuyệt Kỹ Ngay",   nameEN = "Ultimate Charge",
            descVN = "Giảm {v}% cooldown Ulti ngay lập tức", descEN = "Reduce Ulti CD by {v}% instantly",
            emoji = "🌟", value = 30f, duration = -1f,
            cardColor = new Color(1f, 0.8f, 0f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.ComboMastery,
            nameVN = "Làm Chủ Combo",        nameEN = "Combo Mastery",
            descVN = "Thời gian reset combo giảm {v}%", descEN = "Combo reset time reduced {v}%",
            emoji = "⚡", value = 50f, duration = -1f,
            cardColor = new Color(0.9f, 0.6f, 0.1f), rarity = AugmentData.Rarity.Rare
        },

        // ── EPIC: Phúc lợi tạm thời mạnh ──────────────────────────────────────
        new AugmentData {
            type = AugmentType.VampireBlood,
            nameVN = "Huyết Quỷ",           nameEN = "Vampire Blood",
            descVN = "+{v}% hút máu trong {d}s", descEN = "+{v}% Lifesteal for {d}s",
            emoji = "🧛", value = 15f, duration = 45f,
            cardColor = new Color(0.7f, 0f, 0.3f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.RagingFire,
            nameVN = "Lửa Cuồng Nộ",        nameEN = "Raging Fire",
            descVN = "+{v}% ATK trong {d}s",  descEN = "+{v}% ATK for {d}s",
            emoji = "🔥", value = 35f, duration = 30f,
            cardColor = new Color(1f, 0.4f, 0f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.AncientShield,
            nameVN = "Khiên Cổ Đại",        nameEN = "Ancient Shield",
            descVN = "Nhận {v} HP máu ảo trong {d}s", descEN = "{v} Shield HP for {d}s",
            emoji = "🛡️", value = 200f, duration = 25f,
            cardColor = new Color(0.3f, 0.5f, 1f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.SwiftShadow,
            nameVN = "Bóng Tối Vun Vút",    nameEN = "Swift Shadow",
            descVN = "+{v}% tốc độ trong {d}s", descEN = "+{v}% Speed for {d}s",
            emoji = "🌪️", value = 50f, duration = 20f,
            cardColor = new Color(0.2f, 0.9f, 0.9f), rarity = AugmentData.Rarity.Epic
        },
    };

    // ─── Lấy 3 augment không trùng nhau ──────────────────────────────────────
    public static List<AugmentData> GetRandomChoices(string heroId, int count = 3)
    {
        // Lọc pool phù hợp nhân vật
        var pool = new List<AugmentData>();
        foreach (var a in _all)
        {
            if (a.heroFilter == null || System.Array.Exists(a.heroFilter, h => h == heroId))
                pool.Add(a);
        }

        // Shuffle Fisher-Yates
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        // Đảm bảo không trùng TYPE - lấy tối đa `count` augment khác type
        var chosen = new List<AugmentData>();
        var usedTypes = new HashSet<AugmentType>();
        foreach (var a in pool)
        {
            if (!usedTypes.Contains(a.type))
            {
                chosen.Add(a);
                usedTypes.Add(a.type);
            }
            if (chosen.Count >= count) break;
        }
        return chosen;
    }
}
