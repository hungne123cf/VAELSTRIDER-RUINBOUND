using UnityEngine;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Hero Skill Dual-Scaling Damage System (Chuẩn LMHT / TFT)
/// 
/// Hệ thống tính toán Sát thương Kết Hợp (Dual-Scaling: %ATK + %AP):
/// - Sát thương đòn đánh và kỹ năng được đồng bộ 100% chính xác với Bảng Chỉ Số HUD và số sát thương nhảy thực tế trên đầu quái.
/// - Không còn hiện tượng cộng đúp Base Damage khiến sát thương bị vống lên vô lý.
/// </summary>
public static class HeroSkillDamageCalculator
{
    public class SkillInfo
    {
        public string skillName;
        public string skillCategory; // ĐÒN ĐÁNH THƯỜNG / KÍCH HOẠT / LƯỚT / TUYỆT KỸ
        public string cooldownText;
        public string description;
        public int totalDamage;
        public int fromAtk;
        public int fromAP;
        public float atkRatio;
        public float apRatio;
        public DamageType damageType;
        public Sprite skillIcon;
    }

    /// <summary>
    /// Lấy thông tin chi tiết của Đòn Đánh Thường (Basic Attack).
    /// </summary>
    public static SkillInfo GetAttackSkillInfo(PlayerCombat combat)
    {
        if (combat == null) return null;

        string charId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
        int totalAtk = combat.TotalPhysicalDamage;
        int totalAP = combat.TotalMagicDamage;

        SkillInfo info = new SkillInfo();
        info.cooldownText = LocalizationManager.T("Hồi chiêu: 0s", "Cooldown: 0s");
        info.skillCategory = LocalizationManager.T("ĐÒN ĐÁNH THƯỜNG", "BASIC ATTACK");

        switch (charId)
        {
            case CharacterManager.CHAR_CELESTIAL_MAGE: // Pháp Sư Tinh Tú
                info.skillName = LocalizationManager.T("Tinh Tú Ma Cầu & Sét Lan", "Star Orbs & Chain Lightning");
                info.atkRatio = 0.30f;
                info.apRatio = 1.00f;
                info.damageType = DamageType.Magic;
                info.fromAP = totalAP;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = Mathf.Max(1, info.fromAP + info.fromAtk);
                info.description = LocalizationManager.T(
                    $"Phóng 3 luồng cầu ma pháp tự động truy tìm kẻ địch và bộc phá phóng sét lan, gây tổng cộng <b><color=#4DEEEA>{info.totalDamage}</color></b> sát thương phép.",
                    $"Fires 3 homing star orbs that detonate and chain lightning, dealing a total of <b><color=#4DEEEA>{info.totalDamage}</color></b> magic damage.");
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Lam Kiếm Sĩ
                info.skillName = LocalizationManager.T("Trọng Kiếm Khí & Sóng Xuyên Thấu", "Heavy Swordwave & Piercing Arc");
                info.atkRatio = 0.50f;
                info.apRatio = 1.00f;
                info.damageType = DamageType.Magic;
                info.fromAP = totalAP;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = Mathf.Max(1, info.fromAP + info.fromAtk);
                info.description = LocalizationManager.T(
                    $"Vung kiếm phóng luồng kiếm khí lam sắc xuyên thấu hàng loạt quái vật, gây <b><color=#4DEEEA>{info.totalDamage}</color></b> sát thương phép.",
                    $"Slashes forward releasing an azure swordwave that pierces through multiple enemies, dealing <b><color=#4DEEEA>{info.totalDamage}</color></b> magic damage.");
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Lôi Kiếm Lãng Tử
                info.skillName = LocalizationManager.T("Cư Hợp Lôi Kiếm Combo", "Iaido Thunder Combo");
                info.atkRatio = 1.00f;
                info.apRatio = 0.30f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Chuỗi trảm kích lôi ảnh chém chớp nhoáng diện rộng, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Rapid thunder slash combo sweeping a wide arc, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Cuồng Nộ Giáp Sắt
                info.skillName = LocalizationManager.T("Đại Chùy Nghiền Nát", "Heavy Warhammer Smash");
                info.atkRatio = 1.00f;
                info.apRatio = 0.25f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Vung đại chùy càn quét diện rộng đẩy lùi quái vật, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Heavy warhammer sweeps repelling foes, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;

            case CharacterManager.CHAR_DUNE_ASSASSIN: // Sa Mạc Thích Khách
                info.skillName = LocalizationManager.T("Song Đoản Đao Tốc Biến", "Twin Dagger Flurry");
                info.atkRatio = 1.00f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Combo song đoản đao lướt chém chớp nhoáng với tỉ lệ chí mạng cao, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Lightning-fast dual dagger slices boasting high critical strike rate, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;

            default: // Vaelstrider
                info.skillName = LocalizationManager.T("Trảm Kích Kiếm Sĩ Cổ Đại", "Ancient Blade Strike");
                info.atkRatio = 1.00f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Vung kiếm cổ đại theo hình vòng cung dứt điểm kẻ địch, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Sweeping ancient sword slashes slicing foes, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;
        }

        info.skillIcon = MobileActionButton.GetHeroActionIcon(charId, MobileActionButton.ButtonType.Attack);
        return info;
    }

    /// <summary>
    /// Lấy thông tin chi tiết của Tuyệt Kỹ (Ultimate Skill).
    /// </summary>
    public static SkillInfo GetSpecialSkillInfo(PlayerCombat combat)
    {
        if (combat == null) return null;

        string charId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
        int totalAtk = combat.TotalPhysicalDamage;
        int totalAP = combat.TotalMagicDamage;
        float cd = combat.SkillCooldown;

        SkillInfo info = new SkillInfo();
        info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: {cd:0.0}s";
        info.skillCategory = LocalizationManager.T("TUYỆT KỸ TỐI THƯỢNG", "ULTIMATE SKILL");

        switch (charId)
        {
            case CharacterManager.CHAR_CELESTIAL_MAGE: // Player 6
                info.skillName = LocalizationManager.T("Thiên Thạch Ma Trận", "Celestial Meteor Storm");
                info.atkRatio = 0.40f;
                info.apRatio = 1.30f;
                info.damageType = DamageType.Magic;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = 40 + info.fromAP + info.fromAtk;
                info.description = LocalizationManager.T(
                    $"Triệu hồi mưa thiên thạch từ không gian rơi xuống hủy diệt toàn bộ kẻ địch trên bản đồ, gây <b><color=#4DEEEA>{info.totalDamage}</color></b> sát thương phép mỗi viên.",
                    $"Summon a catastrophic meteor storm obliterating all battlefield enemies, dealing <b><color=#4DEEEA>{info.totalDamage}</color></b> magic damage per meteor.");
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Player 3
                info.skillName = LocalizationManager.T("Bão Sóng Kiếm Nan Quạt", "Fan-Blade Wave Barrage");
                info.atkRatio = 0.50f;
                info.apRatio = 1.10f;
                info.damageType = DamageType.Magic;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = 32 + info.fromAP + info.fromAtk;
                info.description = LocalizationManager.T(
                    $"Phóng ra 5 luồng kiếm khí khổng lồ quét sạch toàn bộ góc chiến trường, gây <b><color=#4DEEEA>{info.totalDamage}</color></b> sát thương phép mỗi luồng.",
                    $"Releases 5 giant fan-shaped sword arcs sweeping clean the battlefield, dealing <b><color=#4DEEEA>{info.totalDamage}</color></b> magic damage per arc.");
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Player 2
                info.skillName = LocalizationManager.T("Nhất Kiếm Đoạt Mệnh", "One-Sword Decapitation");
                info.atkRatio = 1.30f;
                info.apRatio = 0.70f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 40 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Biến mất vào hư không và xuất hiện chém xuyên qua hàng loạt quái vật, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Vanishes into shadows and slashes through a line of enemies at blinding speed, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Player 4
                info.skillName = LocalizationManager.T("Địa Chấn Nện Đất", "Cataclysmic Ground Slam");
                info.atkRatio = 1.40f;
                info.apRatio = 0.50f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 50 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Nện búa sấm sét tạo sóng xung kích 360 độ làm choáng kẻ địch, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Slams the warhammer down creating a 360-degree shockwave that stuns foes, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;

            case CharacterManager.CHAR_DUNE_ASSASSIN: // Player 5
                info.skillName = LocalizationManager.T("Cuồng Bão Sa Ảnh (Chém X)", "Sandstorm Phantom (Cross Slash)");
                info.atkRatio = 1.20f;
                info.apRatio = 0.60f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 45 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Triệu hồi lốc cát vây khốn quái vật và tung nhát chém chữ X xé rách chiến trường, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Conjures sand tornadoes and executes a lethal X-Slash, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;

            default: // Vaelstrider
                info.skillName = LocalizationManager.T("Bão Kiếm Cổ Đại", "Ruin Whirlwind");
                info.atkRatio = 1.10f;
                info.apRatio = 0.60f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 35 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Xoay tròn đại kiếm cổ đại hất tung quái vật xung quanh, gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
                    $"Spins the ancient greatsword knocking surrounding enemies airborne, dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");
                break;
        }

        info.skillIcon = MobileActionButton.GetHeroActionIcon(charId, MobileActionButton.ButtonType.Skill);
        return info;
    }

    /// <summary>
    /// Lấy thông tin chi tiết của Kỹ Năng Lướt Nhanh (Dash).
    /// </summary>
    public static SkillInfo GetDashSkillInfo(PlayerController controller, PlayerCombat combat)
    {
        if (controller == null) return null;

        string charId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
        int totalAtk = combat != null ? combat.TotalPhysicalDamage : 18;
        int totalAP = combat != null ? combat.TotalMagicDamage : 0;

        SkillInfo info = new SkillInfo();
        info.skillName = LocalizationManager.T("Tốc Biến Lướt Nhanh", "Shadow Dash");
        info.skillCategory = LocalizationManager.T("KỸ NĂNG DI CHUYỂN", "MOBILITY SKILL");
        info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 2.5s (3 {LocalizationManager.T("lần", "charges")})";
        info.atkRatio = 0.40f;
        info.apRatio = 0.25f;
        info.damageType = DamageType.Physical;
        info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
        info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
        info.totalDamage = 12 + info.fromAtk + info.fromAP;

        info.description = LocalizationManager.T(
            $"Lướt nhanh né đòn và trở nên bất khả xâm phạm trong thoáng chốc, hất văng quái vật cản đường và gây <b><color=#FFA500>{info.totalDamage}</color></b> sát thương vật lý.",
            $"Swift dash granting invulnerability, pushing back path enemies and dealing <b><color=#FFA500>{info.totalDamage}</color></b> physical damage.");

        info.skillIcon = MobileActionButton.GetHeroActionIcon(charId, MobileActionButton.ButtonType.Dash);
        return info;
    }
}
