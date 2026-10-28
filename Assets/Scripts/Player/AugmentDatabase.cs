using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database tất cả các Lõi Nâng Cấp (Augments) phong cách Đấu Trường Chân Lý (TFT) & Dark Fantasy.
/// Chia làm 3 Bậc (Tiers):
/// - BẬC BẠC (Common / Silver Tier - ✦): Chỉ số cơ bản & hiệu ứng nền tảng
/// - BẬC VÀNG (Rare / Gold Tier - ✦✦): Sức mạnh đột phá & hiệu ứng chiến thuật / cộng dồn
/// - BẬC KIM CƯƠNG (Epic / Prismatic Tier - ✦✦✦): Quyền năng thay đổi cục diện trận đấu, cơ chế độc đáo
/// </summary>
public static class AugmentDatabase
{
    public enum AugmentType
    {
        // ─── Chỉ số cơ bản ───
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

        // ─── Cơ chế độc đáo & Tạm thời ───
        VampireBlood,       // +Hút máu trong thời gian
        RagingFire,         // +Cuồng nộ ATK
        AncientShield,      // Khiên máu ảo
        SwiftShadow,        // Tốc độ vũ bão
        ComboMastery,       // Làm chủ chuỗi đòn
        BerserkerMode,      // Cuồng Nộ Bất Tận (stack ATK theo mạng hạ gục)
        RegenerativeCore,   // Hồi phục sinh lực
        EchoStrike,         // Lôi Đình Song Trảm (đánh đúp giật sét)
        SoulHarvest,        // Thu hoạch linh hồn (hồi máu & tăng vĩnh viễn HP khi hạ địch)
        UltimateCharge,     // Nạp tuyệt kỹ ngay lập tức

        // ─── Lõi mới chuẩn Đấu Trường Chân Lý (TFT-style) ───
        DemonicPact,        // Khế Ước Quỷ Dữ (Prismatic): Giảm 15% Max HP đổi lấy ATK, Tốc Chạy, Hút Máu cực lớn
        UndyingSovereign,   // Bất Diệt Cuồng Vương (Prismatic): Càng ít máu càng mạnh, bất tử 3.5s khi gặp đòn chí tử
        CelestialCascade,   // Vọng Âm Tinh Tú (Prismatic): Đòn thứ 3 gọi mưa thiên thạch nổ diện rộng & hồi mana
        BloodGodTribute,    // Huyết Thần Tế Lễ (Prismatic): Hút máu cực cao, hồi máu dư thừa chuyển thành Lá Chắn Máu Ảo
        GodforgedArsenal,   // Thần Khí Tối Thượng (Prismatic): Tăng toàn bộ chỉ số chính & giảm mạnh hồi Tuyệt Kỹ
        StormDancer,        // Vũ Điệu Bão Tố (Prismatic): Lướt (Dash) phóng 4 luồng kiếm khí chém nát kẻ địch xung quanh
        ShadowAmbush,       // Nhát Chém Bóng Đêm (Gold): Sau khi Lướt, đòn đánh tiếp theo chắc chắn Chí Mạng 220%
        ArcaneOverdrive,    // Giao Kèo Ma Thuật (Gold): Tăng mạnh Sát Thương Phép & Hút Máu Phép, giảm hồi chiêu
        DivineAegis,        // Thánh Khiên Hộ Mệnh (Gold): Khi máu dưới 35%, nhận 250 Khiên Ảo và hồi 25% HP
        DeathsTouch,        // Vết Thương Tử Thần (Gold): Đòn đánh gây Vết Thương Sâu & Thiêu Đốt phần trăm máu
        IronBastion,        // Khiên Thép Kiên Cố (Silver): Tăng giáp cố định & giảm 10% sát thương nhận vào
        SurgingPulse,       // Năng Lượng Tuôn Trào (Silver): Giảm 15% hồi chiêu, đòn đánh hồi thêm Mana/Nội Năng
        AbundantVitality,   // Sinh Mệnh Dồi Dào (Silver): Tăng lớn Máu tối đa & lập tức hồi phục 100 HP
        LaceratingEdge,     // Lưỡi Dao Xé Rách (Silver): Tăng sát thương vật lý & tốc độ đánh
        FleetWind,          // Bước Chân Tật Phong (Silver): Tăng tốc chạy, di chuyển liên tục tăng giáp

        // ─── Bổ sung kho Lõi Nâng Cấp phong phú chuẩn TFT & Dark Fantasy ───
        HeavyImpact,        // Chấn Động Nặng Nề (Silver)
        ArcaneShield,       // Hộ Thuẫn Bí Thuật (Silver)
        RelentlessAssault,  // Tấn Công Dồn Dập (Silver)
        BountyHunter,       // Thợ Săn Tiền Thưởng (Silver)
        SecondWind,         // Hơi Thở Thứ Hai (Silver)
        StaticOverload,     // Quá Tải Lôi Quang (Gold)
        PoisonTouch,        // Độc Dược Ăn Mòn (Gold)
        GiantSlayer,        // Diệt Khổng Lồ (Gold)
        LifeTransfusion,    // Truyền Dẫn Sinh Mệnh (Gold)
        UnwaveringWill,     // Ý Chí Kiên Định (Gold)
        BattleFrenzy,       // Cuồng Loạn Chiến Trường (Gold)
        ChainReaction,      // Phản Ứng Dây Chuyền (Prismatic)
        AscendedDivinity,   // Thần Cách Thăng Hoa (Prismatic)
        PrismaticTitan,     // Bản Thể Cự Thần (Prismatic)
        EchoingVoid,        // Hư Không Vọng Âm (Prismatic)

        // ─── Lõi Nguyên Tố Đặc Trưng Theo Map (Elemental Realm Augments) ───
        FlameAdaptation,    // Thích Nghi Hỏa Ngục (Magma Calamity - Prismatic)
        MagmaEruption,      // Hỏa Nham Bộc Phát (Magma Calamity - Gold)
        AcidMembrane,       // Màng Lọc Axit (Acid Downpour - Prismatic)
        VirulentBloom,      // Hoa Độc Bùng Nổ (Acid Downpour - Gold)
        GlacialSoul,        // Linh Hồn Băng Giá (Frostfall Blizzard - Prismatic)
        AbsoluteZeroNova,   // Hàn Băng Cực Độ (Frostfall Blizzard - Gold)
        VoidTranscendence,  // Siêu Việt Hư Không (Void Nightmare - Prismatic)
        ShadowMirroring,    // Ảnh Phân Thân Hư Không (Void Nightmare - Gold)

        // ─── Lõi Riêng Biệt Cho Từng Tướng (Hero Signature Augments) ───
        TripleMirageTornado, // Tam Sa Lốc Xoáy (Dune Assassin - Chiêu Gồng tung 3 Cơn Lốc Cát)
        MeteorSupercluster,  // Siêu Tinh Vụ Trụy Lạc (Celestial Mage - Chùm 6 Thiên Thạch & Hố Đen)
        TripleRuinGale,      // Tam Trảm Tàn Phong (Vaelstrider - 3 Đợt Sóng Kiếm Ruin Gale)
        EightWayTempest,     // Bát Hướng Cuồng Phong (Gale Ronin - Lướt/Chém tung kiếm khí 8 hướng)
        TwinFrostNova,       // Song Băng Trụ Thần Cực (Azure Blade - Tụ lực gọi 2 Cột Băng Đóng Băng diện rộng)
        CataclysmicQuake     // Địa Chấn Tam Liên Hoàn (Titan Berserker - Nện đất tạo 3 đợt sóng xung kích)
    }

    public class AugmentData
    {
        public AugmentType type;
        public string nameVN;
        public string nameEN;
        public string descVN;
        public string descEN;
        public string flavorVN;       // Trích dẫn phong vị tàn tích Dark Fantasy
        public string flavorEN;
        public string emoji;          // Icon đại diện
        public float value;           // Giá trị chỉ số
        public float duration;        // -1 = vĩnh viễn
        public string[] heroFilter;   // null = tất cả nhân vật
        public CalamityRealm[] realmFilter; // null = mọi cõi, hoặc chỉ cõi nguyên tố chỉ định
        public Color cardColor;       // Màu viền fallback
        public Rarity rarity;         // Common = Bạc, Rare = Vàng, Epic = Kim Cương

        public enum Rarity
        {
            Common, // BẬC BẠC (Silver)
            Rare,   // BẬC VÀNG (Gold)
            Epic    // BẬC KIM CƯƠNG / LĂNG KÍNH (Prismatic)
        }

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

    // ─── Pool Tổng Hợp Tất Cả Lõi Nâng Cấp ──────────────────────────────────────
    private static readonly List<AugmentData> _all = new List<AugmentData>
    {
        // =========================================================================
        // ─── BẬC BẠC (COMMON / SILVER TIER - ✦) ──────────────────────────────────
        // =========================================================================
        new AugmentData {
            type = AugmentType.BonusAttack,
            nameVN = "Lưỡi Dao Xé Rách",      nameEN = "Lacerating Edge",
            descVN = "Tăng <color=#FFEAA7><b>+{v} Sát Thương Vật Lý</b></color> và nhận thêm <color=#55EFC4><b>+8% Tốc Độ Đánh</b></color>.",
            descEN = "Gain <color=#FFEAA7><b>+{v} Physical Attack</b></color> and <color=#55EFC4><b>+8% Attack Speed</b></color>.",
            flavorVN = "«Răng cưa gỉ sét rạch qua từng thớ thịt.»",
            flavorEN = "«Jagged rusted steel tearing through sinew.»",
            emoji = "🗡️", value = 14f, duration = -1f,
            cardColor = new Color(0.9f, 0.35f, 0.25f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.AbundantVitality,
            nameVN = "Sinh Mệnh Dồi Dào",    nameEN = "Abundant Vitality",
            descVN = "Tăng <color=#55EFC4><b>+{v} Máu Tối Đa</b></color> và hồi phục ngay lập tức <color=#55EFC4><b>100 HP</b></color>.",
            descEN = "Grant <color=#55EFC4><b>+{v} Max Health</b></color> and instantly restore <color=#55EFC4><b>100 HP</b></color>.",
            flavorVN = "«Dòng suối sinh mệnh cổ xưa trào dâng trong huyết quản.»",
            flavorEN = "«Ancient fonts of vitality coursing through veins.»",
            emoji = "❤️", value = 120f, duration = -1f,
            cardColor = new Color(0.15f, 0.85f, 0.40f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.FleetWind,
            nameVN = "Bước Chân Tật Phong",    nameEN = "Fleet Wind",
            descVN = "Tăng <color=#38BDF8><b>+{v}% Tốc Độ Di Chuyển</b></color>. Sau khi chạy liên tục 2s, nhận thêm <color=#60A5FA><b>+15 Giáp</b></color>.",
            descEN = "Gain <color=#38BDF8><b>+{v}% Movement Speed</b></color>. Moving for 2s adds <color=#60A5FA><b>+15 Armor</b></color>.",
            flavorVN = "«Như ngọn cuồng phong lướt qua tàn tích hoang tàn.»",
            flavorEN = "«Like wandering gales dancing across ruin stones.»",
            emoji = "💨", value = 12f, duration = -1f,
            cardColor = new Color(0.25f, 0.75f, 1f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusCritChance,
            nameVN = "Tập Trung Thần Nhãn",  nameEN = "Eagle Focus",
            descVN = "Tăng <color=#FBBF24><b>+{v}% Tỉ Lệ Chí Mạng</b></color> và <color=#F59E0B><b>+15% Sát Thương Chí Mạng</b></color>.",
            descEN = "Gain <color=#FBBF24><b>+{v}% Critical Chance</b></color> and <color=#F59E0B><b>+15% Critical Damage</b></color>.",
            flavorVN = "«Đôi mắt ưng săn mồi nhìn thấu mọi điểm sơ hở.»",
            flavorEN = "«Predatory sight perceiving every mortal fracture.»",
            emoji = "👁️", value = 12f, duration = -1f,
            cardColor = new Color(1f, 0.85f, 0.2f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.IronBastion,
            nameVN = "Khiên Thép Kiên Cố",  nameEN = "Iron Bastion",
            descVN = "Nhận <color=#60A5FA><b>+{v} Giáp Cố Định</b></color> và giảm <color=#38BDF8><b>8% mọi sát thương nhận vào</b></color>.",
            descEN = "Gain <color=#60A5FA><b>+{v} Armor</b></color> and reduce <color=#38BDF8><b>8% incoming damage</b></color>.",
            flavorVN = "«Tấm khiên rèn từ quặng thép đen vĩnh cửu.»",
            flavorEN = "«Forged from black iron unyielding against fate.»",
            emoji = "🛡️", value = 16f, duration = -1f,
            cardColor = new Color(0.4f, 0.65f, 1f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.SurgingPulse,
            nameVN = "Năng Lượng Tuôn Trào", nameEN = "Surging Pulse",
            descVN = "Giảm <color=#F59E0B><b>+{v}% Thời Gian Hồi Chiêu</b></color>. Đòn đánh thường hồi thêm <color=#38BDF8><b>+6 Mana/Nội Năng</b></color>.",
            descEN = "Grant <color=#F59E0B><b>+{v}% Cooldown Reduction</b></color>. Basic attacks restore <color=#38BDF8><b>+6 Mana/Energy</b></color>.",
            flavorVN = "«Dòng mạch ma thuật cuồn cuộn không bao giờ cạn kiệt.»",
            flavorEN = "«An endless subterranean river of magical currents.»",
            emoji = "⚡", value = 15f, duration = -1f,
            cardColor = new Color(0.2f, 0.85f, 0.9f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BonusLifesteal,
            nameVN = "Huyết Nguyệt Tinh Hoa", nameEN = "Blood Essence",
            descVN = "Tăng <color=#EF4444><b>+{v}% Hút Máu Toàn Phần</b></color> (cả Vật Lý & Phép) và <color=#60A5FA><b>+10 Giáp</b></color>.",
            descEN = "Gain <color=#EF4444><b>+{v}% Omnivamp</b></color> (Physical & Spell) and <color=#60A5FA><b>+10 Armor</b></color>.",
            flavorVN = "«Nuốt chửng giọt máu tươi để duy trì mạng sống.»",
            flavorEN = "«Consuming fresh blood to sustain mortal breath.»",
            emoji = "🩸", value = 7f, duration = -1f,
            cardColor = new Color(0.85f, 0.2f, 0.35f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.RegenerativeCore,
            nameVN = "Lõi Tái Sinh",        nameEN = "Regenerative Core",
            descVN = "Hồi phục <color=#55EFC4><b>+{v} HP mỗi giây</b></color> trong <color=#FFEAA7><b>{d}s</b></color> và tăng <color=#55EFC4><b>+50 Máu tối đa</b></color>.",
            descEN = "Regenerate <color=#55EFC4><b>+{v} HP/sec</b></color> for <color=#FFEAA7><b>{d}s</b></color> and add <color=#55EFC4><b>+50 Max HP</b></color>.",
            flavorVN = "«Trái tim cơ giới bơm nhựa sống vào lục phủ ngũ tạng.»",
            flavorEN = "«A clockwork core pumping life juice through organs.»",
            emoji = "💚", value = 4f, duration = 60f,
            cardColor = new Color(0.2f, 0.8f, 0.5f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.SwiftShadow,
            nameVN = "U Hồn Lướt Gió",        nameEN = "Phantom Drift",
            descVN = "Trong <color=#FFEAA7><b>{d}s</b></color>, tăng vọt <color=#38BDF8><b>+{v}% Tốc Độ Di Chuyển</b></color> lướt đi thoăn thoắt.",
            descEN = "For <color=#FFEAA7><b>{d}s</b></color>, surge forward with <color=#38BDF8><b>+{v}% Movement Speed</b></color>.",
            flavorVN = "«Thân pháp ma mị như ảo ảnh giữa sương mù hoang tàn.»",
            flavorEN = "«A phantom silhouette slipping through the broken mist.»",
            emoji = "💨", value = 30f, duration = 30f,
            cardColor = new Color(0.25f, 0.75f, 1f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.AncientShield,
            nameVN = "Khiên Thần Cổ Xưa",      nameEN = "Ancient Aegis Ward",
            descVN = "Nhận ngay lớp Giáp Ảo <color=#38BDF8><b>+{v} Khiên Bảo Vệ</b></color> tồn tại trong <color=#FFEAA7><b>{d}s</b></color>.",
            descEN = "Instantly gain <color=#38BDF8><b>+{v} Virtual Shield</b></color> protecting for <color=#FFEAA7><b>{d}s</b></color>.",
            flavorVN = "«Màng bảo vệ cổ đại ngăn chặn mọi đòn hiểm độc.»",
            flavorEN = "«Ancient energy barrier warding off fatal strikes.»",
            emoji = "🛡️", value = 200f, duration = 40f,
            cardColor = new Color(0.35f, 0.65f, 1f), rarity = AugmentData.Rarity.Common
        },

        // =========================================================================
        // ─── BẬC VÀNG (RARE / GOLD TIER - ✦✦) ────────────────────────────────────
        // =========================================================================
        new AugmentData {
            type = AugmentType.BerserkerMode,
            nameVN = "Cuồng Bạo Bất Tận",   nameEN = "Endless Berserk",
            descVN = "Mỗi mạng hạ gục nhận vĩnh viễn <color=#FFEAA7><b>+{v} Sát Thương</b></color> (tối đa 25 tầng). Ở tối đa tầng, nhận thêm <color=#55EFC4><b>+25% Tốc Đánh</b></color>.",
            descEN = "Each kill permanently grants <color=#FFEAA7><b>+{v} Attack</b></color> (max 25 stacks). At max stacks, gain <color=#55EFC4><b>+25% Attack Speed</b></color>.",
            flavorVN = "«Cơn thịnh nộ thiêu rụi lý trí, chỉ còn bản năng tàn sát.»",
            flavorEN = "«Fury incinerating reason, leaving only predatory wrath.»",
            emoji = "😈", value = 3f, duration = -1f,
            cardColor = new Color(1f, 0.35f, 0.1f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.SoulHarvest,
            nameVN = "Hấp Thụ Linh Hồn",   nameEN = "Soul Siphon",
            descVN = "Hạ gục quái vật hồi phục <color=#55EFC4><b>{v} HP</b></color> và tăng vĩnh viễn <color=#FFEAA7><b>+5 Máu Tối Đa</b></color>.",
            descEN = "Slaying enemies restores <color=#55EFC4><b>{v} HP</b></color> and permanently increases <color=#FFEAA7><b>+5 Max Health</b></color>.",
            flavorVN = "«Hút cạn linh hồn kẻ bại trận để bồi đắp thân xác phàm.»",
            flavorEN = "«Devouring vanquished spirits to fortify mortal clay.»",
            emoji = "☠️", value = 25f, duration = -1f,
            cardColor = new Color(0.75f, 0.2f, 0.9f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.EchoStrike,
            nameVN = "Lôi Đình Song Trảm",   nameEN = "Echoing Thunder",
            descVN = "Đòn đánh có <color=#FBBF24><b>{v}% cơ hội</b></color> giáng Lôi Đình trúng kẻ địch, gây <color=#C056FF><b>180% Sát Thương Phép</b></color> giật lan.",
            descEN = "Attacks have <color=#FBBF24><b>{v}% chance</b></color> to summon Thunderbolts dealing <color=#C056FF><b>180% Magic Damage</b></color> chain hit.",
            flavorVN = "«Sấm sét của cổ thần hòa quyện vào đường gươm xé gió.»",
            flavorEN = "«Ancient thunderbolts surging through whistling blade edges.»",
            emoji = "⚡", value = 35f, duration = -1f,
            cardColor = new Color(0.95f, 0.85f, 0.15f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.ShadowAmbush,
            nameVN = "Nhát Chém Bóng Đêm",  nameEN = "Shadow Ambush",
            descVN = "Sau khi Lướt (Dash), đòn đánh kế tiếp trong 3s <color=#FBBF24><b>chắc chắn Chí Mạng</b></color> gây thêm <color=#FFEAA7><b>+{v}% Sát Thương</b></color>.",
            descEN = "After Dashing, next attack within 3s is <color=#FBBF24><b>Guaranteed Critical</b></color> dealing <color=#FFEAA7><b>+{v}% Damage</b></color>.",
            flavorVN = "«Xuất hiện từ cõi hư vô rồi đoạt mạng trong chớp mắt.»",
            flavorEN = "«Emerging from the void to claim life in a heartbeat.»",
            emoji = "🗡️", value = 220f, duration = -1f,
            cardColor = new Color(0.95f, 0.25f, 0.15f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.ArcaneOverdrive,
            nameVN = "Giao Kèo Ma Thuật",   nameEN = "Arcane Overdrive",
            descVN = "Tăng <color=#C056FF><b>+{v} Sát Thương Phép</b></color>, <color=#C056FF><b>+12% Hút Máu Phép</b></color> và giảm <color=#F59E0B><b>25% Hồi Chiêu</b></color>.",
            descEN = "Gain <color=#C056FF><b>+{v} Spell Damage</b></color>, <color=#C056FF><b>+12% Spell Vamp</b></color> and cut <color=#F59E0B><b>25% Cooldowns</b></color>.",
            flavorVN = "«Tri thức cấm kỵ cổ đại giải phóng ma lực vô tận.»",
            flavorEN = "«Forbidden lore unlocking boundless arcane resonances.»",
            emoji = "🔮", value = 32f, duration = -1f,
            cardColor = new Color(0.8f, 0.35f, 1f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.DivineAegis,
            nameVN = "Thánh Khiên Hộ Mệnh",  nameEN = "Divine Aegis",
            descVN = "Khi Máu tụt dưới 35%, lập tức nhận <color=#38BDF8><b>+{v} Khiên Ảo</b></color> và hồi <color=#55EFC4><b>25% HP</b></color> trong 5s (Hồi chiêu 45s).",
            descEN = "When falling below 35% HP, immediately gain <color=#38BDF8><b>+{v} Shield</b></color> and heal <color=#55EFC4><b>25% HP</b></color> (CD: 45s).",
            flavorVN = "«Ánh sáng thần thánh bảo hộ linh hồn trước vực thẳm diệt vong.»",
            flavorEN = "«Divine radiance shielding souls from the brink of oblivion.»",
            emoji = "🛡️", value = 250f, duration = -1f,
            cardColor = new Color(0.35f, 0.65f, 1f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.DeathsTouch,
            nameVN = "Vết Thương Tử Thần",   nameEN = "Death's Touch",
            descVN = "Đòn đánh gây <color=#EF4444><b>Vết Thương Sâu</b></color> (giảm hồi máu 40%) và <color=#FF7675><b>Thiêu Đốt {v}% Máu Tối Đa</b></color> của địch trong 3s.",
            descEN = "Attacks apply <color=#EF4444><b>Grievous Wounds</b></color> and <color=#FF7675><b>Burn {v}% Max HP</b></color> of targets over 3s.",
            flavorVN = "«Lời nguyền ăn mòn da thịt mục rữa từ bên trong.»",
            flavorEN = "«A festering curse rotting enemy flesh from within.»",
            emoji = "💀", value = 5f, duration = -1f,
            cardColor = new Color(0.9f, 0.2f, 0.2f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.ComboMastery,
            nameVN = "Vũ Điệu Lưỡi Gươm",    nameEN = "Blade Dance",
            descVN = "Tăng <color=#55EFC4><b>+25% Tốc Độ Đánh</b></color>, thời gian reset chuỗi combo giảm <color=#FFEAA7><b>+{v}%</b></color>.",
            descEN = "Gain <color=#55EFC4><b>+25% Attack Speed</b></color>, combo chain reset window cut by <color=#FFEAA7><b>+{v}%</b></color>.",
            flavorVN = "«Vũ đạo đoạt mạng liên hoàn không thể cản phá.»",
            flavorEN = "«An unstoppable unbroken ballet of mortal slashes.»",
            emoji = "⚔️", value = 50f, duration = -1f,
            cardColor = new Color(0.95f, 0.65f, 0.15f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.VampireBlood,
            nameVN = "Huyết Triều Cuồng Nộ",   nameEN = "Blood Surge",
            descVN = "Trong <color=#FFEAA7><b>{d}s</b></color>, nhận thêm <color=#EF4444><b>+{v}% Hút Máu Toàn Phần</b></color> cực mạnh.",
            descEN = "For <color=#FFEAA7><b>{d}s</b></color>, gain massive <color=#EF4444><b>+{v}% Omnivamp</b></color>.",
            flavorVN = "«Cơn khát máu cuộn trào thiêu đốt từng tĩnh mạch.»",
            flavorEN = "«A voracious bloodlust surging through every pulse.»",
            emoji = "🩸", value = 15f, duration = 45f,
            cardColor = new Color(0.9f, 0.2f, 0.35f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.RagingFire,
            nameVN = "Cuồng Nộ Hỏa Diệm",      nameEN = "Blazing Wrath",
            descVN = "Trong <color=#FFEAA7><b>{d}s</b></color>, bùng nổ sức mạnh tăng thêm <color=#FFEAA7><b>+{v}% Sát Thương</b></color>.",
            descEN = "For <color=#FFEAA7><b>{d}s</b></color>, ignite your blade granting <color=#FFEAA7><b>+{v}% Physical Attack</b></color>.",
            flavorVN = "«Lửa giận ngút trời thiêu rụi quân thù thành tro bụi.»",
            flavorEN = "«Blazing fury reducing foes to charred cinders.»",
            emoji = "🔥", value = 40f, duration = 35f,
            cardColor = new Color(1f, 0.4f, 0.15f), rarity = AugmentData.Rarity.Rare
        },

        // =========================================================================
        // ─── BẬC KIM CƯƠNG / LĂNG KÍNH (PRISMATIC TIER - ✦✦✦) ────────────────────
        // =========================================================================
        new AugmentData {
            type = AugmentType.DemonicPact,
            nameVN = "Khế Ước Quỷ Dữ",       nameEN = "Demonic Pact",
            descVN = "Hy sinh <color=#FF7675><b>15% Máu Tối Đa</b></color> để đổi lấy <color=#FFEAA7><b>+{v} Sát Thương</b></color>, <color=#38BDF8><b>+20% Tốc Chạy</b></color> và <color=#EF4444><b>+15% Hút Máu Vĩnh Cửu</b></color>!",
            descEN = "Sacrifice <color=#FF7675><b>15% Max HP</b></color> for <color=#FFEAA7><b>+{v} Attack</b></color>, <color=#38BDF8><b>+20% Speed</b></color> and <color=#EF4444><b>+15% Omnivamp</b></color>!",
            flavorVN = "«Ký bằng huyết lệ trên tấm bia da quỷ... Đổi lấy sức mạnh hủy diệt.»",
            flavorEN = "«Signed in blood upon demon parchment... Boundless destruction gained.»",
            emoji = "🔥", value = 45f, duration = -1f,
            cardColor = new Color(1f, 0.15f, 0.35f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.UndyingSovereign,
            nameVN = "Bất Diệt Cuồng Vương", nameEN = "Undying Sovereign",
            descVN = "Khi nhận sát thương chí tử, <color=#FBBF24><b>miễn nhiễm cái chết & Bất Tử trong {v}s</b></color> (Hồi chiêu 90s). Càng ít Máu, Sát Thương tăng tới <color=#FFEAA7><b>+80%</b></color>!",
            descEN = "Upon fatal damage, <color=#FBBF24><b>cheat death & become Invulnerable for {v}s</b></color> (CD: 90s). Low HP grants up to <color=#FFEAA7><b>+80% Damage</b></color>!",
            flavorVN = "«Vua tàn tích không bao giờ quỳ gối trước lưỡi hái tử thần.»",
            flavorEN = "«The Ruin King bows to no scythe of mortality.»",
            emoji = "👑", value = 3.5f, duration = -1f,
            cardColor = new Color(1f, 0.85f, 0.15f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.CelestialCascade,
            nameVN = "Vọng Âm Tinh Tú",      nameEN = "Celestial Cascade",
            descVN = "Mỗi đòn đánh thứ 3 giáng <color=#C056FF><b>Mưa Thiên Thạch</b></color> nổ tung xung quanh gây <color=#C056FF><b>{v} Sát Thương Phép diện rộng</b></color> và hồi <color=#38BDF8><b>15 Mana</b></color>!",
            descEN = "Every 3rd attack calls a <color=#C056FF><b>Celestial Meteor Burst</b></color> dealing <color=#C056FF><b>{v} AoE Magic Damage</b></color> & restores <color=#38BDF8><b>15 Mana</b></color>!",
            flavorVN = "«Các vì sao tinh tú cổ đại gầm vang trên bầu trời u tối.»",
            flavorEN = "«Ancient cosmic constellations rumbling through dying skies.»",
            emoji = "☄️", value = 250f, duration = -1f,
            cardColor = new Color(0.7f, 0.35f, 1f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.BloodGodTribute,
            nameVN = "Huyết Thần Tế Lễ",     nameEN = "Blood God's Tribute",
            descVN = "Nhận <color=#EF4444><b>+{v}% Hút Máu Toàn Phần</b></color>. Khi máu đã đầy, toàn bộ lượng máu hút dư thừa sẽ <color=#FBBF24><b>chuyển thành Lá Chắn Máu Ảo vĩnh cửu</b></color> (lên tới 50% HP)!",
            descEN = "Gain <color=#EF4444><b>+{v}% Omnivamp</b></color>. When at full health, overhealing <color=#FBBF24><b>converts into permanent Blood Shield</b></color> (up to 50% HP)!",
            flavorVN = "«Huyết tế ngập tràn đền thờ thần chết, dệt nên áo giáp bất hoại.»",
            flavorEN = "«Rivers of sacrificial blood woven into an impervious shroud.»",
            emoji = "🩸", value = 18f, duration = -1f,
            cardColor = new Color(0.95f, 0.1f, 0.25f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.GodforgedArsenal,
            nameVN = "Thần Khí Tối Thượng",  nameEN = "Godforged Arsenal",
            descVN = "Nhận ngay <color=#FFEAA7><b>+{v} Sát Thương</b></color>, <color=#60A5FA><b>+20 Giáp</b></color>, <color=#FBBF24><b>+20% Chí Mạng</b></color> và giảm <color=#F59E0B><b>35% Hồi Tuyệt Kỹ</b></color>!",
            descEN = "Instantly gain <color=#FFEAA7><b>+{v} Attack</b></color>, <color=#60A5FA><b>+20 Armor</b></color>, <color=#FBBF24><b>+20% Crit</b></color> and cut <color=#F59E0B><b>35% Ulti Cooldown</b></color>!",
            flavorVN = "«Vũ khí rèn từ lửa thần thánh và xương rồng ngàn năm.»",
            flavorEN = "«Forged in holy fire and ancient thousand-year dragon bones.»",
            emoji = "🌟", value = 50f, duration = -1f,
            cardColor = new Color(1f, 0.88f, 0.4f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.StormDancer,
            nameVN = "Vũ Điệu Bão Tố",       nameEN = "Storm Dancer",
            descVN = "Khi Lướt (Dash), phóng ra <color=#38BDF8><b>4 Luồng Kiếm Khí Bão Tố</b></color> chém nát xung quanh gây <color=#FFEAA7><b>{v} Sát Thương</b></color> và tăng <color=#55EFC4><b>+20% Tốc Chạy (3s)</b></color>!",
            descEN = "Dashing unleashes <color=#38BDF8><b>4 Tempest Blade Waves</b></color> around you dealing <color=#FFEAA7><b>{v} Damage</b></color> and adds <color=#55EFC4><b>+20% Speed (3s)</b></color>!",
            flavorVN = "«Bước chân khuấy đảo phong ba, lưỡi kiếm rạch nát hư không.»",
            flavorEN = "«Steps whipping up tempests, edge cleaving through void space.»",
            emoji = "🌪️", value = 200f, duration = -1f,
            cardColor = new Color(0.2f, 0.9f, 0.95f), rarity = AugmentData.Rarity.Epic
        },

        // =========================================================================
        // ─── BỔ SUNG LÕI BẬC BẠC (+5 LÕI MỚI) ────────────────────────────────────
        // =========================================================================
        new AugmentData {
            type = AugmentType.HeavyImpact,
            nameVN = "Chấn Động Nặng Nề",    nameEN = "Heavy Impact",
            descVN = "Tăng <color=#FFEAA7><b>+{v} Sát Thương Vật Lý</b></color>. Đòn đánh nặng và đòn kết chuỗi có thêm lực va đập cực mạnh.",
            descEN = "Gain <color=#FFEAA7><b>+{v} Physical Attack</b></color>. Heavy strikes unleash punishing impact shockwaves.",
            flavorVN = "«Trọng lượng của tảng đá cổ nghiền nát giáp sắt.»",
            flavorEN = "«Ancient boulders crushing iron plate to dust.»",
            emoji = "🔨", value = 18f, duration = -1f,
            cardColor = new Color(0.95f, 0.45f, 0.2f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.ArcaneShield,
            nameVN = "Hộ Thuẫn Bí Thuật",    nameEN = "Arcane Shield",
            descVN = "Nhận ngay lớp Giáp Ảo <color=#38BDF8><b>+{v} Khiên Bảo Vệ</b></color> và tăng thêm <color=#60A5FA><b>+10 Giáp</b></color>.",
            descEN = "Gain <color=#38BDF8><b>+{v} Virtual Shield</b></color> and permanently increase <color=#60A5FA><b>+10 Armor</b></color>.",
            flavorVN = "«Màng chắn kết tinh từ linh hồn các cổ thuật sư.»",
            flavorEN = "«Crystallized soul-shards of ancient sorcerers.»",
            emoji = "🛡️", value = 150f, duration = -1f,
            cardColor = new Color(0.35f, 0.65f, 1f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.RelentlessAssault,
            nameVN = "Tấn Công Dồn Dập",     nameEN = "Relentless Assault",
            descVN = "Tăng <color=#55EFC4><b>+{v}% Tốc Độ Đánh</b></color> và nhận thêm <color=#FFEAA7><b>+10 Sát Thương Vật Lý</b></color>.",
            descEN = "Grant <color=#55EFC4><b>+{v}% Attack Speed</b></color> and <color=#FFEAA7><b>+10 Physical Attack</b></color>.",
            flavorVN = "«Mũi kiếm vung lên không ngừng nghỉ như bão táp.»",
            flavorEN = "«A relentless tempest of whistling sword strokes.»",
            emoji = "⚔️", value = 15f, duration = -1f,
            cardColor = new Color(0.9f, 0.4f, 0.25f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.BountyHunter,
            nameVN = "Thợ Săn Tiền Thưởng",  nameEN = "Bounty Hunter",
            descVN = "Lập tức nhận <color=#FBBF24><b>+{v} Vụn Tinh Thể (Shards)</b></color> và tăng <color=#38BDF8><b>+15% Tốc Độ Di Chuyển</b></color>.",
            descEN = "Instantly receive <color=#FBBF24><b>+{v} Crystal Shards</b></color> and gain <color=#38BDF8><b>+15% Movement Speed</b></color>.",
            flavorVN = "«Tiếng leng keng của tinh thể dẫn lối kẻ truy lùng tàn tích.»",
            flavorEN = "«The clink of crystals guides ruined realm hunters.»",
            emoji = "💰", value = 200f, duration = -1f,
            cardColor = new Color(1f, 0.82f, 0.2f), rarity = AugmentData.Rarity.Common
        },
        new AugmentData {
            type = AugmentType.SecondWind,
            nameVN = "Hơi Thở Thứ Hai",      nameEN = "Second Wind",
            descVN = "Tăng <color=#55EFC4><b>+{v} Máu Tối Đa</b></color> và hồi phục ngay lập tức <color=#55EFC4><b>80 HP</b></color>.",
            descEN = "Grant <color=#55EFC4><b>+{v} Max Health</b></color> and immediately restore <color=#55EFC4><b>80 HP</b></color>.",
            flavorVN = "«Một hơi thở sâu giữa chiến địa đầy máu lửa.»",
            flavorEN = "«A deep breath drawn amidst blazing battlefields.»",
            emoji = "💚", value = 100f, duration = -1f,
            cardColor = new Color(0.2f, 0.85f, 0.45f), rarity = AugmentData.Rarity.Common
        },

        // =========================================================================
        // ─── BỔ SUNG LÕI BẬC VÀNG (+6 LÕI MỚI) ───────────────────────────────────
        // =========================================================================
        new AugmentData {
            type = AugmentType.StaticOverload,
            nameVN = "Quá Tải Lôi Quang",    nameEN = "Static Overload",
            descVN = "Đòn đánh gây thêm <color=#C056FF><b>+{v} Sát Thương Phép</b></color> và tăng <color=#FBBF24><b>+15% Tốc Độ Đánh</b></color>.",
            descEN = "Attacks deal <color=#C056FF><b>+{v} Bonus Magic Damage</b></color> and add <color=#FBBF24><b>+15% Attack Speed</b></color>.",
            flavorVN = "«Dòng điện tích tụ xé toạc màn đêm u tối.»",
            flavorEN = "«Crackling arcs rupturing darkest horizons.»",
            emoji = "⚡", value = 25f, duration = -1f,
            cardColor = new Color(0.75f, 0.35f, 1f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.PoisonTouch,
            nameVN = "Độc Dược Ăn Mòn",      nameEN = "Corrosive Venom",
            descVN = "Đòn đánh găm nọc độc ăn mòn khiến mục tiêu nhận thêm <color=#EF4444><b>+{v}% Sát Thương</b></color> trong 4 giây.",
            descEN = "Strikes coat targets in venom making them take <color=#EF4444><b>+{v}% More Damage</b></color> for 4s.",
            flavorVN = "«Dịch độc từ nanh mãng xà cổ đại ngấm sâu vào tủy sống.»",
            flavorEN = "«Ancient viper venom dissolving marrow and bone.»",
            emoji = "☠️", value = 20f, duration = -1f,
            cardColor = new Color(0.2f, 0.85f, 0.4f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.GiantSlayer,
            nameVN = "Diệt Khổng Lồ",        nameEN = "Giant Slayer",
            descVN = "Tăng mạnh <color=#FFEAA7><b>+{v} Sát Thương Vật Lý</b></color> và nhận thêm <color=#FBBF24><b>+15% Tỉ Lệ Chí Mạng</b></color>.",
            descEN = "Gain punishing <color=#FFEAA7><b>+{v} Physical Attack</b></color> and <color=#FBBF24><b>+15% Critical Chance</b></color>.",
            flavorVN = "«Lưỡi đại kiếm rèn riêng để hạ gục những gã khổng lồ đá.»",
            flavorEN = "«Greatswords tempered to cleave stone behemoths.»",
            emoji = "🗡️", value = 35f, duration = -1f,
            cardColor = new Color(0.95f, 0.3f, 0.2f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.LifeTransfusion,
            nameVN = "Truyền Dẫn Sinh Mệnh", nameEN = "Life Transfusion",
            descVN = "Nhận thêm <color=#EF4444><b>+{v}% Hút Máu Toàn Phần</b></color> và mỗi mạng hạ gục lập tức hồi phục <color=#55EFC4><b>35 HP</b></color>.",
            descEN = "Grant <color=#EF4444><b>+{v}% Omnivamp</b></color> and each kill instantly restores <color=#55EFC4><b>35 HP</b></color>.",
            flavorVN = "«Rút cạn sinh lực kẻ thù để nuôi dưỡng thể xác.»",
            flavorEN = "«Siphoning enemy vigor to rejuvenate mortal veins.»",
            emoji = "🩸", value = 10f, duration = -1f,
            cardColor = new Color(0.9f, 0.15f, 0.3f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.UnwaveringWill,
            nameVN = "Ý Chí Kiên Định",      nameEN = "Unwavering Will",
            descVN = "Tăng <color=#60A5FA><b>+{v} Giáp Cố Định</b></color> và nhận thêm <color=#55EFC4><b>+150 Máu Tối Đa</b></color>.",
            descEN = "Gain <color=#60A5FA><b>+{v} Permanent Armor</b></color> and <color=#55EFC4><b>+150 Max Health</b></color>.",
            flavorVN = "«Ý chí gang thép không thể suy suyển trước nghịch cảnh.»",
            flavorEN = "«Adamantine will unbending before ruinous storm.»",
            emoji = "🛡️", value = 30f, duration = -1f,
            cardColor = new Color(0.35f, 0.65f, 1f), rarity = AugmentData.Rarity.Rare
        },
        new AugmentData {
            type = AugmentType.BattleFrenzy,
            nameVN = "Cuồng Loạn Chiến Trường", nameEN = "Battle Frenzy",
            descVN = "Tăng <color=#FFEAA7><b>+{v} Sát Thương</b></color>, nhận thêm <color=#EF4444><b>+12% Hút Máu</b></color> và <color=#38BDF8><b>+10% Tốc Chạy</b></color>.",
            descEN = "Gain <color=#FFEAA7><b>+{v} Attack</b></color>, <color=#EF4444><b>+12% Omnivamp</b></color> and <color=#38BDF8><b>+10% Move Speed</b></color>.",
            flavorVN = "«Càng chém giết hăng say, sức mạnh càng cuộn trào.»",
            flavorEN = "«The fiercer the fray, the higher the crimson tide.»",
            emoji = "🔥", value = 25f, duration = -1f,
            cardColor = new Color(1f, 0.45f, 0.15f), rarity = AugmentData.Rarity.Rare
        },

        // =========================================================================
        // ─── BỔ SUNG LÕI BẬC KIM CƯƠNG / LĂNG KÍNH (+4 LÕI MỚI) ───────────────────
        // =========================================================================
        new AugmentData {
            type = AugmentType.ChainReaction,
            nameVN = "Phản Ứng Dây Chuyền",  nameEN = "Chain Reaction",
            descVN = "Tăng khủng khiếp <color=#FFEAA7><b>+{v} Sát Thương</b></color> và <color=#FBBF24><b>+20% Tỉ Lệ Chí Mạng</b></color> xuyên phá mọi phòng thủ!",
            descEN = "Gain devastating <color=#FFEAA7><b>+{v} Physical Attack</b></color> and <color=#FBBF24><b>+20% Critical Chance</b></color>!",
            flavorVN = "«Vụ nổ dây chuyền biến cả chiến trường thành bình địa.»",
            flavorEN = "«Cataclysmic chain bursts reducing bastions to ash.»",
            emoji = "☄️", value = 50f, duration = -1f,
            cardColor = new Color(1f, 0.25f, 0.35f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.AscendedDivinity,
            nameVN = "Thần Cách Thăng Hoa",  nameEN = "Ascended Divinity",
            descVN = "Nhận vĩnh viễn <color=#55EFC4><b>+{v} Máu Tối Đa</b></color>, <color=#FFEAA7><b>+40 Sát Thương</b></color> và <color=#60A5FA><b>+20 Giáp</b></color>!",
            descEN = "Permanently gain <color=#55EFC4><b>+{v} Max Health</b></color>, <color=#FFEAA7><b>+40 Attack</b></color> and <color=#60A5FA><b>+20 Armor</b></color>!",
            flavorVN = "«Rũ bỏ phàm xác, bước lên cảnh giới của các vị thần cổ.»",
            flavorEN = "«Shedding mortality to ascend elder divine planes.»",
            emoji = "👑", value = 350f, duration = -1f,
            cardColor = new Color(1f, 0.85f, 0.2f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.PrismaticTitan,
            nameVN = "Bản Thể Cự Thần",      nameEN = "Prismatic Titan",
            descVN = "Nhận <color=#55EFC4><b>+{v} Máu Tối Đa</b></color>, <color=#60A5FA><b>+40 Giáp</b></color> và lập tức nhận thêm 250 Khiên Ảo!",
            descEN = "Gain <color=#55EFC4><b>+{v} Max Health</b></color>, <color=#60A5FA><b>+40 Armor</b></color> and 250 Virtual Shield!",
            flavorVN = "«Sừng sững như đỉnh núi cổ thụ ngạo nghễ trước sấm sét.»",
            flavorEN = "«Towering like monoliths standing unyielding in thunder.»",
            emoji = "🛡️", value = 500f, duration = -1f,
            cardColor = new Color(0.35f, 0.75f, 1f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.EchoingVoid,
            nameVN = "Hư Không Vọng Âm",      nameEN = "Echoing Void",
            descVN = "Giảm <color=#F59E0B><b>+{v}% Thời Gian Hồi Chiêu</b></color>, tăng <color=#C056FF><b>+45 Sát Thương Phép</b></color> và <color=#38BDF8><b>+25 Nội Năng</b></color>!",
            descEN = "Cut <color=#F59E0B><b>+{v}% Cooldowns</b></color>, add <color=#C056FF><b>+45 Spell Damage</b></color> and <color=#38BDF8><b>+25 Max Energy</b></color>!",
            flavorVN = "«Âm vang từ hư không vô tận bóp méo dòng chảy thời gian.»",
            flavorEN = "«Echoes from abyssal void bending the flow of time.»",
            emoji = "🔮", value = 35f, duration = -1f,
            cardColor = new Color(0.75f, 0.35f, 1f), rarity = AugmentData.Rarity.Epic
        },

        // =========================================================================
        // ─── LÕI NGUYÊN TỐ ĐẶC TRƯNG THEO MAP (ELEMENTAL REALM AUGMENTS) ─────────
        // =========================================================================
        new AugmentData {
            type = AugmentType.FlameAdaptation,
            nameVN = "Thích Nghi Hỏa Ngục",   nameEN = "Flame Adaptation",
            descVN = "Giảm <color=#F59E0B><b>60%</b></color> sát thương Lửa & Thiêu Đốt. Bị đánh trúng lập tức phóng trả <color=#EF4444><b>Quả Cầu Dung Nham</b></color> gây 140 sát thương và làm choáng 1s!",
            descEN = "Reduce Fire & Burn damage by <color=#F59E0B><b>60%</b></color>. Taking damage fires back a <color=#EF4444><b>Magma Fireball</b></color> dealing 140 damage and 1s stun!",
            flavorVN = "«Cơ thể hòa quyện với nham thạch, biến ngọn lửa hủy diệt thành vũ khí phản đòn.»",
            flavorEN = "«Flesh merging with molten slag, weaponizing destructive flames.»",
            emoji = "🔥", value = 60f, duration = -1f,
            realmFilter = new[] { CalamityRealm.MagmaCalamity },
            cardColor = new Color(1f, 0.35f, 0.1f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.MagmaEruption,
            nameVN = "Hỏa Nham Bộc Phát",     nameEN = "Magma Eruption",
            descVN = "Đòn đánh và kỹ năng tạo vệt <color=#EF4444><b>Dung Nham Thiêu Đốt</b></color> trong 3.5s, gây 40 sát thương/s và làm chậm <color=#F59E0B><b>35% Tốc Chạy</b></color> của kẻ địch!",
            descEN = "Attacks and skills scorch the earth with <color=#EF4444><b>Molten Magma</b></color> for 3.5s, dealing 40 dmg/s and slowing by <color=#F59E0B><b>35%</b></color>!",
            flavorVN = "«Bước chân rực lửa nung chảy cả mặt đất hoang tàn.»",
            flavorEN = "«Blazing footsteps liquifying scorched earth beneath.»",
            emoji = "🌋", value = 40f, duration = -1f,
            realmFilter = new[] { CalamityRealm.MagmaCalamity },
            cardColor = new Color(0.95f, 0.45f, 0.15f), rarity = AugmentData.Rarity.Rare
        },

        new AugmentData {
            type = AugmentType.AcidMembrane,
            nameVN = "Màng Lọc Kịch Độc",     nameEN = "Acid Membrane",
            descVN = "Giảm <color=#10B981><b>70%</b></color> sát thương Mưa Axit & Vũng Độc. Mỗi 3 lần nhiễm độc kích nổ <color=#22C55E><b>Bào Tử Axit</b></color> làm choáng quái 1.5s và hồi 15% Máu đã mất!",
            descEN = "Reduce Acid & Poison damage by <color=#10B981><b>70%</b></color>. Every 3 hits detonates <color=#22C55E><b>Toxic Spores</b></color> stunning enemies 1.5s and healing 15% missing HP!",
            flavorVN = "«Lớp màng sinh học cổ xưa hấp thụ độc tố biến thành sinh lực thuần khiết.»",
            flavorEN = "«Ancient bio-membrane distilling lethal venom into regenerative surge.»",
            emoji = "🧪", value = 70f, duration = -1f,
            realmFilter = new[] { CalamityRealm.AcidDownpour },
            cardColor = new Color(0.15f, 0.85f, 0.35f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.VirulentBloom,
            nameVN = "Hoa Độc Bùng Nổ",       nameEN = "Virulent Bloom",
            descVN = "Đòn đánh gây <color=#10B981><b>Độc Ăn Mòn</b></color>. Khi kẻ địch nhiễm độc bị tiêu diệt, thi thể phát nổ gây 150 sát thương và lây độc ra toàn bộ kẻ địch lân cận!",
            descEN = "Attacks inflict <color=#10B981><b>Corrosive Venom</b></color>. Slain poisoned foes detonate dealing 150 damage and spreading toxins to nearby foes!",
            flavorVN = "«Bông hoa tàn độc nở rộ trên xác của những kẻ ngã xuống.»",
            flavorEN = "«Venomous blossoms erupting from corpses of fallen beasts.»",
            emoji = "☣️", value = 150f, duration = -1f,
            realmFilter = new[] { CalamityRealm.AcidDownpour },
            cardColor = new Color(0.20f, 0.80f, 0.40f), rarity = AugmentData.Rarity.Rare
        },

        new AugmentData {
            type = AugmentType.GlacialSoul,
            nameVN = "Linh Hồn Băng Giá",     nameEN = "Glacial Soul",
            descVN = "Miễn nhiễm làm chậm trong bão tuyết. Nhận <color=#38BDF8><b>+220 Giáp Băng Ảo</b></color>; khi khiên vỡ lập tức <color=#60A5FA><b>Đóng Băng Tuyệt Đối</b></color> toàn bộ kẻ địch trong 2.5s!",
            descEN = "Immune to blizzard slows. Gain <color=#38BDF8><b>+220 Ice Shield</b></color>; on shield break immediately <color=#60A5FA><b>Freeze all enemies</b></color> for 2.5s!",
            flavorVN = "«Trái tim băng giá ngưng đọng thời gian và nuốt chửng cái lạnh vĩnh cửu.»",
            flavorEN = "«A frozen heart halting time itself, devouring everlasting frost.»",
            emoji = "❄️", value = 220f, duration = -1f,
            realmFilter = new[] { CalamityRealm.FrostfallBlizzard },
            cardColor = new Color(0.25f, 0.75f, 1f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.AbsoluteZeroNova,
            nameVN = "Hàn Băng Cực Độ",       nameEN = "Absolute Zero Nova",
            descVN = "Đòn đánh có <color=#38BDF8><b>30%</b></color> cơ hội kích hoạt Hàn Băng Cực Độ, gây <color=#60A5FA><b>+95 Sát Thương Phép</b></color> và đóng băng kẻ địch trong 1.2s!",
            descEN = "Attacks have a <color=#38BDF8><b>30%</b></color> chance to trigger Absolute Zero, dealing <color=#60A5FA><b>+95 Magic Damage</b></color> and freezing foes for 1.2s!",
            flavorVN = "«Điểm không tuyệt đối làm đóng băng cả huyết quản đối thủ.»",
            flavorEN = "«Subzero frost crystallizing blood in victims veins.»",
            emoji = "🧊", value = 95f, duration = -1f,
            realmFilter = new[] { CalamityRealm.FrostfallBlizzard },
            cardColor = new Color(0.30f, 0.80f, 1f), rarity = AugmentData.Rarity.Rare
        },

        new AugmentData {
            type = AugmentType.VoidTranscendence,
            nameVN = "Siêu Việt Hư Không",    nameEN = "Void Transcendence",
            descVN = "Miễn nhiễm sát thương vùng hư không tha hóa. Chuyển <color=#C084FC><b>25% Sát Thương Nhận Vào</b></color> thành Hút Máu Toàn Năng và tăng <color=#A855F7><b>+40 Sát Thương Chuẩn</b></color>!",
            descEN = "Immune to corrupted void zones. Converts <color=#C084FC><b>25% of Damage Taken</b></color> into Omnivamp and gains <color=#A855F7><b>+40 True Damage</b></color>!",
            flavorVN = "«Vượt qua ranh giới trần tục, hư không trở thành nguồn sống vô tận.»",
            flavorEN = "«Transcending mortality, the void itself becomes boundless nourishment.»",
            emoji = "🌌", value = 40f, duration = -1f,
            realmFilter = new[] { CalamityRealm.VoidNightmare },
            cardColor = new Color(0.70f, 0.30f, 1f), rarity = AugmentData.Rarity.Epic
        },
        new AugmentData {
            type = AugmentType.ShadowMirroring,
            nameVN = "Ảnh Phân Thân Hư Không",nameEN = "Shadow Mirroring",
            descVN = "Sau khi Lướt hoặc Né đòn thành công, tạo <color=#C084FC><b>Phân Thân Bóng Tối</b></color> tự động trảm kích phụ gây 120 Sát Thương Phép lên kẻ địch xung quanh!",
            descEN = "After Dashing or Dodging, summon a <color=#C084FC><b>Shadow Clone</b></color> unleashing a sweep slash dealing 120 Magic Damage to nearby foes!",
            flavorVN = "«Bóng ma phản chiếu từ vết rách không gian giáng nhát chém vô ảnh.»",
            flavorEN = "«Phantom mirrored from void rifts striking down unseen victims.»",
            emoji = "👤", value = 120f, duration = -1f,
            realmFilter = new[] { CalamityRealm.VoidNightmare },
            cardColor = new Color(0.60f, 0.25f, 0.95f), rarity = AugmentData.Rarity.Rare
        },

        // =========================================================================
        // ─── LÕI ĐẶC QUYỀN RIÊNG CHO 6 TƯỚNG (HERO SIGNATURE AUGMENTS) ───────────
        // =========================================================================
        // 1. Dune Assassin (Player 5): Chiêu gồng thế thủ tung cùng lúc 3 Lốc Cát
        new AugmentData {
            type = AugmentType.TripleMirageTornado,
            nameVN = "Tam Sa Lốc Xoáy",       nameEN = "Triple Mirage Tornado",
            descVN = "Kỹ năng Gồng Thủ & Phóng Lốc của Sát Thủ Sa Mạc giải phóng cùng lúc <color=#F59E0B><b>3 CƠN LỐC CÁT</b></color> (tiêu tốn máu gấp 3 lần) tỏa hình nan quạt, càn quét và hút nghiền nát mọi kẻ địch!",
            descEN = "Dune Assassin's Charged Vortex launches <color=#F59E0B><b>3 SAND VORTICES</b></color> (costs 3x health) fanning out to crush and pull all foes!",
            flavorVN = "«Bão cát sa mạc cuồng nộ tam phân thiên địa, không một sinh linh nào có thể thoát thân.»",
            flavorEN = "«Desert tempest splitting into triple vortexes, leaving nothing standing in its wake.»",
            emoji = "🌪️", value = 3f, duration = -1f,
            heroFilter = new[] { CharacterManager.CHAR_DUNE_ASSASSIN },
            cardColor = new Color(0.95f, 0.70f, 0.20f), rarity = AugmentData.Rarity.Epic
        },

        // 2. Celestial Mage (Player 6): Siêu cụm 6 thiên thạch liên tiếp & Hố Đen
        new AugmentData {
            type = AugmentType.MeteorSupercluster,
            nameVN = "Siêu Tinh Vụ Trụy Lạc",  nameEN = "Meteor Supercluster",
            descVN = "Chiêu Tuyệt Kỹ Thiên Thạch triệu hồi liên hoàn <color=#EC4899><b>6 THIÊN THẠCH</b></color> dồn dập và tạo ra <color=#A855F7><b>Hố Đen Hư Không</b></color> hút chặt mọi kẻ địch vào tâm vụ nổ!",
            descEN = "Meteor Apocalypse summons a rapid barrage of <color=#EC4899><b>6 METEORS</b></color> collapsing into an <color=#A855F7><b>Astral Black Hole</b></color> dragging all foes into the blast!",
            flavorVN = "«Các tinh cầu từ vũ trụ xa xôi lao xuống theo tiếng gọi của pháp sư tinh tú.»",
            flavorEN = "«Celestial orbs raining down in catastrophic unison under the star mage's command.»",
            emoji = "☄️", value = 6f, duration = -1f,
            heroFilter = new[] { CharacterManager.CHAR_CELESTIAL_MAGE },
            cardColor = new Color(0.92f, 0.35f, 0.85f), rarity = AugmentData.Rarity.Epic
        },

        // 3. Vaelstrider (Player 1): Tam Trảm Tàn Phong
        new AugmentData {
            type = AugmentType.TripleRuinGale,
            nameVN = "Tam Trảm Tàn Phong",    nameEN = "Triple Ruin Gale",
            descVN = "Đòn Parry phản đòn hoặc chém tích lực của Vaelstrider phóng ra <color=#FBBF24><b>3 ĐỢT SÓNG KIẾM RUIN GALE</b></color> quét hình nón, xuyên thấu và đẩy lùi toàn bộ quái vật!",
            descEN = "Vaelstrider's Parry or Heavy Charge unleashes <color=#FBBF24><b>3 RUIN GALE SWORD WAVES</b></color> fanning out, piercing and repelling all targets!",
            flavorVN = "«Lưỡi kiếm cổ xưa rít gào, mở đường máu giữa bầy tàn ma.»",
            flavorEN = "«The ancient blade howls, carving a path of ruin through fiendish hordes.»",
            emoji = "⚔️", value = 3f, duration = -1f,
            heroFilter = new[] { CharacterManager.CHAR_VAELSTRIDER },
            cardColor = new Color(1f, 0.85f, 0.25f), rarity = AugmentData.Rarity.Epic
        },

        // 4. Gale Ronin (Player 2): Bát Hướng Cuồng Phong
        new AugmentData {
            type = AugmentType.EightWayTempest,
            nameVN = "Bát Hướng Cuồng Phong", nameEN = "Eight-Way Tempest",
            descVN = "Khi Lướt (Dash) hoặc tung kiếm chiêu đặc biệt, Lãng Khách phóng ra <color=#34D399><b>8 LUỒNG KIẾM KHÍ PHONG LÔI</b></color> tỏa 8 hướng 360 độ chém nát chiến trường!",
            descEN = "When Dashing or casting special skills, unleash <color=#34D399><b>8 WIND BLADES</b></color> in all 360-degree directions slicing down all surrounding foes!",
            flavorVN = "«Kiếm nhanh như gió thoảng, phong trảm tám phương không tì vết.»",
            flavorEN = "«Swifter than whispers of wind, omnidirectional slashes cleaving through the gale.»",
            emoji = "🌀", value = 8f, duration = -1f,
            heroFilter = new[] { CharacterManager.CHAR_GALE_RONIN },
            cardColor = new Color(0.20f, 0.90f, 0.65f), rarity = AugmentData.Rarity.Epic
        },

        // 5. Azure Blade (Player 3): Song Băng Trụ Thần Cực
        new AugmentData {
            type = AugmentType.TwinFrostNova,
            nameVN = "Song Băng Trụ Thần Cực",nameEN = "Twin Frost Nova",
            descVN = "Chiêu Tích Lực tụ kiếm triệu hồi <color=#38BDF8><b>2 CỘT BĂNG ĐỊA CHẤN</b></color> dâng trào từ lòng đất, lập tức <color=#60A5FA><b>Đóng Băng Tuyệt Đối 2.5s</b></color> và nổ tung diện rộng!",
            descEN = "Heavy Charge summons <color=#38BDF8><b>2 GIANT ICE PILLARS</b></color> erupting from the earth, freezing all foes for 2.5s and shattering in a huge blast!",
            flavorVN = "«Băng tinh vạn năm phong ấn sinh linh trong hàn khí bất diệt.»",
            flavorEN = "«Millennial frost seals binding all souls in eternal absolute stillness.»",
            emoji = "🗡️", value = 2f, duration = -1f,
            heroFilter = new[] { CharacterManager.CHAR_AZURE_BLADE },
            cardColor = new Color(0.25f, 0.85f, 1f), rarity = AugmentData.Rarity.Epic
        },

        // 6. Titan Berserker (Player 4): Địa Chấn Tam Liên Hoàn
        new AugmentData {
            type = AugmentType.CataclysmicQuake,
            nameVN = "Địa Chấn Tam Liên Hoàn",nameEN = "Cataclysmic Quake",
            descVN = "Chiêu Nện Đất (Earthshaker) tạo ra <color=#F59E0B><b>3 ĐỢT SÓNG XUNG KÍCH ĐỊA CHẤN</b></color> rạn nứt mặt đất lan tỏa toàn màn hình, làm choáng 2s và gây sát thương x3!",
            descEN = "Earthshaker unleashes <color=#F59E0B><b>3 CONSECUTIVE SEISMIC SHOCKWAVES</b></color> rippling across the arena, stunning for 2s and dealing 3x damage!",
            flavorVN = "«Cú nện nghìn cân làm sụp đổ cả nền móng pháo đài cổ đại.»",
            flavorEN = "«A colossal slam shattering the bedrock of ancient fortresses.»",
            emoji = "🔨", value = 3f, duration = -1f,
            heroFilter = new[] { CharacterManager.CHAR_TITAN_BERSERKER },
            cardColor = new Color(0.95f, 0.55f, 0.20f), rarity = AugmentData.Rarity.Epic
        }
    };

    /// <summary>
    /// Lấy dữ liệu Lõi chuẩn từ Database theo AugmentType
    /// </summary>
    public static AugmentData GetByType(AugmentType type)
    {
        return _all.Find(x => x.type == type);
    }

    /// <summary>
    /// Các Lõi cơ chế Độc Nhất (chỉ xuất hiện và kích hoạt 1 lần duy nhất trong cả trận theo chuẩn TFT)
    /// </summary>
    public static bool IsUniqueAugment(AugmentType type)
    {
        switch (type)
        {
            case AugmentType.DemonicPact:
            case AugmentType.UndyingSovereign:
            case AugmentType.CelestialCascade:
            case AugmentType.BloodGodTribute:
            case AugmentType.GodforgedArsenal:
            case AugmentType.StormDancer:
            case AugmentType.ChainReaction:
            case AugmentType.AscendedDivinity:
            case AugmentType.PrismaticTitan:
            case AugmentType.EchoingVoid:
            case AugmentType.DivineAegis:
            case AugmentType.DeathsTouch:
            case AugmentType.BerserkerMode:
            case AugmentType.SoulHarvest:
            case AugmentType.ComboMastery:
            case AugmentType.ShadowAmbush:
            case AugmentType.EchoStrike:
            case AugmentType.GiantSlayer:
            case AugmentType.UnwaveringWill:
            case AugmentType.BattleFrenzy:
            // Lõi Nguyên Tố & Lõi Tướng Độc Nhất
            case AugmentType.FlameAdaptation:
            case AugmentType.MagmaEruption:
            case AugmentType.AcidMembrane:
            case AugmentType.VirulentBloom:
            case AugmentType.GlacialSoul:
            case AugmentType.AbsoluteZeroNova:
            case AugmentType.VoidTranscendence:
            case AugmentType.ShadowMirroring:
            case AugmentType.TripleMirageTornado:
            case AugmentType.MeteorSupercluster:
            case AugmentType.TripleRuinGale:
            case AugmentType.EightWayTempest:
            case AugmentType.TwinFrostNova:
            case AugmentType.CataclysmicQuake:
                return true;
            default:
                return false;
        }
    }

    // ─── Lấy ngẫu nhiên các lựa chọn Lõi Nâng Cấp (chuẩn TFT) ────────────────────
    public static List<AugmentData> GetRandomChoices(string heroId, int count = 3, int playerLevel = 1, IEnumerable<AugmentType> excludeTypes = null)
    {
        var excludeSet = excludeTypes != null ? new HashSet<AugmentType>(excludeTypes) : null;
        var currentRealm = CorruptedCycleManager.CurrentRealm;

        var pool = new List<AugmentData>();
        foreach (var a in _all)
        {
            if (a.heroFilter != null && !System.Array.Exists(a.heroFilter, h => h == heroId))
                continue;

            // Lọc theo cõi nguyên tố nếu lõi có yêu cầu realmFilter
            if (a.realmFilter != null && !System.Array.Exists(a.realmFilter, r => r == currentRealm))
                continue;

            // Loại trừ Lõi Độc Nhất (Unique) nếu người chơi đã sở hữu
            if (IsUniqueAugment(a.type) && AugmentManager.Instance != null && AugmentManager.Instance.HasAugment(a.type))
                continue;

            // Loại trừ các Lõi đã từng xuất hiện trong phiên Roll hiện tại (chuẩn TFT - Anti-Repeat)
            if (excludeSet != null && excludeSet.Contains(a.type))
                continue;

            pool.Add(a);
        }

        // Fallback an toàn nếu pool bị lọc hết (người chơi roll nhiều lần cạn cả kho)
        if (pool.Count < count)
        {
            foreach (var a in _all)
            {
                if (a.heroFilter != null && !System.Array.Exists(a.heroFilter, h => h == heroId))
                    continue;
                if (a.realmFilter != null && !System.Array.Exists(a.realmFilter, r => r == currentRealm))
                    continue;
                if (IsUniqueAugment(a.type) && AugmentManager.Instance != null && AugmentManager.Instance.HasAugment(a.type))
                    continue;

                if (!pool.Contains(a)) pool.Add(a);
                if (pool.Count >= count) break;
            }
        }

        // Tỷ lệ xuất hiện phẩm chất theo cấp độ người chơi (chuẩn TFT)
        // Cấp thấp: Bạc chiếm đa số, có cơ hội xuất hiện Vàng & Kim Cương
        // Cấp cao: Tăng mạnh tỷ lệ Lõi Vàng & Kim Cương
        float silverWeight = Mathf.Max(25f, 65f - playerLevel * 5f);
        float goldWeight   = Mathf.Clamp(25f + playerLevel * 3.5f, 25f, 55f);
        float prismWeight  = Mathf.Clamp(10f + playerLevel * 2.5f, 10f, 35f);

        // Đôi khi có thể xuất hiện vòng đặc biệt (Full Vàng hoặc Full Kim Cương như TFT)
        float specialRoll = Random.value;
        AugmentData.Rarity? forcedRarity = null;
        if (specialRoll < 0.08f)
        {
            forcedRarity = AugmentData.Rarity.Epic; // 8% cơ hội vòng Kim Cương lấp lánh!
        }
        else if (specialRoll < 0.22f)
        {
            forcedRarity = AugmentData.Rarity.Rare; // 14% cơ hội vòng Vàng rực rỡ!
        }

        var chosen = new List<AugmentData>();
        var usedTypes = new HashSet<AugmentType>();

        // Roll từng thẻ một
        for (int i = 0; i < count; i++)
        {
            AugmentData.Rarity targetRarity;
            if (forcedRarity.HasValue)
            {
                targetRarity = forcedRarity.Value;
            }
            else
            {
                float totalWeight = silverWeight + goldWeight + prismWeight;
                float r = Random.Range(0f, totalWeight);
                if (r < silverWeight) targetRarity = AugmentData.Rarity.Common;
                else if (r < silverWeight + goldWeight) targetRarity = AugmentData.Rarity.Rare;
                else targetRarity = AugmentData.Rarity.Epic;
            }

            // Lọc danh sách khả dụng theo targetRarity
            var candidates = pool.FindAll(a => a.rarity == targetRarity && !usedTypes.Contains(a.type));
            if (candidates.Count == 0)
            {
                // Fallback nếu hết thẻ phẩm chất đó
                candidates = pool.FindAll(a => !usedTypes.Contains(a.type));
            }

            if (candidates.Count > 0)
            {
                var picked = candidates[Random.Range(0, candidates.Count)];
                chosen.Add(picked);
                usedTypes.Add(picked.type);
            }
        }

        // Đảm bảo luôn đủ số lượng `count`
        if (chosen.Count < count)
        {
            foreach (var a in pool)
            {
                if (!usedTypes.Contains(a.type))
                {
                    chosen.Add(a);
                    usedTypes.Add(a.type);
                }
                if (chosen.Count >= count) break;
            }
        }

        return chosen;
    }

    public static AugmentData GetData(AugmentType type)
    {
        return _all.Find(a => a.type == type);
    }
}
