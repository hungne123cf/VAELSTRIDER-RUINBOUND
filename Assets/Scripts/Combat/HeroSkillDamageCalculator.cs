using UnityEngine;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Hero Skill Dual-Scaling Damage System (Kiểu LMHT / TFT)
/// 
/// Hệ thống tính toán Sát thương Kết Hợp (Dual-Scaling: %ATK + %AP):
/// - Khi người chơi nhặt bất kỳ Lõi nâng cấp nào (+ATK hoặc +AP), cả 2 chỉ số đều được cộng dồn thật
///   và hiển thị trực tiếp trên Bảng Chỉ Số HUD.
/// - Mỗi chiêu thức (Đánh thường, Lướt, Kỹ năng, Tuyệt kỹ) đều có công thức Dual-Scaling chi tiết:
///   Sát thương = BaseDamage + (%ATK * Tổng ATK) + (%AP * Tổng AP)
///   mang lại sự linh hoạt tối đa khi xây dựng trang bị và lõi công nghệ!
/// </summary>
public static class HeroSkillDamageCalculator
{
    public class SkillInfo
    {
        public string skillName;
        public string skillCategory; // ĐÒN ĐÁNH THƯỜNG / KÍCH HOẠT / LƯỚT / PHÒNG THỦ / TUYỆT KỸ
        public string cooldownText;
        public string description;
        public int baseDamage;
        public float atkRatio;
        public float apRatio;
        public int fromAtk;
        public int fromAP;
        public int totalDamage;
        public DamageType damageType;
        public Sprite skillIcon;
    }

    /// <summary>
    /// Lấy thông tin chi tiết và công thức sát thương của Đòn Đánh Thường (Attack).
    /// </summary>
    public static SkillInfo GetAttackSkillInfo(PlayerCombat combat)
    {
        if (combat == null) return null;

        string charId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
        int totalAtk = combat.TotalPhysicalDamage;
        int totalAP = combat.TotalMagicDamage;

        SkillInfo info = new SkillInfo();
        info.cooldownText = LocalizationManager.T("Hồi chiêu: 0s (Liên tục)", "Cooldown: 0s (Continuous)");
        info.skillCategory = LocalizationManager.T("ĐÒN ĐÁNH THƯỜNG", "BASIC ATTACK");

        switch (charId)
        {
            case CharacterManager.CHAR_CELESTIAL_MAGE: // Player 6 (Mage)
                info.skillName = LocalizationManager.T("Tinh Tú Ma Cầu & Sét Lan", "Star Orbs & Chain Lightning");
                info.description = LocalizationManager.T("Phóng 3 luồng cầu ma pháp tự động truy tìm kẻ địch và bộc phá phóng sét lan sang quái vật lân cận.", 
                                                         "Fire 3 homing star orbs that detonate and chain lightning to nearby targets.");
                info.baseDamage = combat.Attack1Damage;
                info.atkRatio = 0.30f;
                info.apRatio = 1.00f;
                info.damageType = DamageType.Magic;
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Player 3 (Azure Blade)
                info.skillName = LocalizationManager.T("Trọng Kiếm Khí & Sóng Xuyên Thấu", "Heavy Swordwave & Piercing Arc");
                info.description = LocalizationManager.T("Mỗi nhát vung trọng kiếm phóng ra luồng kiếm khí lam sắc xuyên thấu hàng loạt quái vật từ xa.", 
                                                         "Heavy slash that unleashes an azure swordwave piercing through multiple enemies.");
                info.baseDamage = combat.Attack1Damage;
                info.atkRatio = 0.60f;
                info.apRatio = 0.80f;
                info.damageType = DamageType.Magic;
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Player 2 (Ronin)
                info.skillName = LocalizationManager.T("Cư Hợp Lôi Kiếm Combo", "Iaido Thunder Combo");
                info.description = LocalizationManager.T("Chuỗi trảm kích tốc độ cực cao, nhát chém thứ 2 tăng mạnh phạm vi và tốc độ xuất chiêu.", 
                                                         "High-speed slashes with increased range and swiftness on the second finisher.");
                info.baseDamage = combat.Attack1Damage;
                info.atkRatio = 1.00f;
                info.apRatio = 0.30f;
                info.damageType = DamageType.Physical;
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Player 4 (Titan)
                info.skillName = LocalizationManager.T("Đại Chùy Nghiền Nát", "Heavy Warhammer Smash");
                info.description = LocalizationManager.T("Vung đại chùy càn quét diện rộng, đập nát giáp trụ và đẩy lùi toàn bộ quái vật trước mặt.", 
                                                         "Sweeping warhammer strikes that shatter armor and push back all foes.");
                info.baseDamage = combat.Attack1Damage;
                info.atkRatio = 1.15f;
                info.apRatio = 0.25f;
                info.damageType = DamageType.Physical;
                break;

            case CharacterManager.CHAR_DUNE_ASSASSIN: // Player 5 (Dune)
                info.skillName = LocalizationManager.T("Song Đoản Đao Tốc Biến", "Twin Dagger Flurry");
                info.description = LocalizationManager.T("Combo song đoản đao lướt chém chớp nhoáng với tỉ lệ chí mạng cao nhất trong toàn bộ anh hùng.", 
                                                         "Lightning-fast dual dagger slices boasting the highest innate critical rate.");
                info.baseDamage = combat.Attack1Damage;
                info.atkRatio = 1.05f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                break;

            default: // Vaelstrider (Player 1)
                info.skillName = LocalizationManager.T("Trảm Kích Kiếm Sĩ Cổ Đại", "Ancient Blade Strike");
                info.description = LocalizationManager.T("Combo 2 đòn chém cơ bản vung kiếm vòng cung, nhát chém thứ hai bồi thêm sát thương dứt điểm.", 
                                                         "2-hit sword slash combo; second blow delivers heavy finishing impact.");
                info.baseDamage = combat.Attack1Damage;
                info.atkRatio = 1.00f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                break;
        }

        info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
        info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
        info.totalDamage = Mathf.Max(1, info.baseDamage + info.fromAtk + info.fromAP);
        return info;
    }

    /// <summary>
    /// Lấy thông tin chi tiết và công thức sát thương của Kỹ Năng / Tuyệt Kỹ (Special / Skill).
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
                info.description = LocalizationManager.T("Triệu hồi cơn mưa thiên thạch từ không gian rơi xuống hủy diệt toàn bộ kẻ địch trên màn hình.", 
                                                         "Summon a catastrophic meteor storm obliterating all enemies across the battlefield.");
                info.baseDamage = 55;
                info.atkRatio = 0.50f;
                info.apRatio = 1.50f;
                info.damageType = DamageType.Magic;
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Player 3
                info.skillName = LocalizationManager.T("Bão Sóng Kiếm Nan Quạt", "Fan-Blade Wave Barrage");
                info.description = LocalizationManager.T("Tích tụ cực hạn và phóng ra 5 luồng kiếm khí khổng lồ quét sạch toàn bộ góc chiến trường.", 
                                                         "Charge and release 5 giant fan-shaped sword arcs sweeping clean the battlefield.");
                info.baseDamage = 45;
                info.atkRatio = 0.70f;
                info.apRatio = 1.20f;
                info.damageType = DamageType.Magic;
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Player 2
                info.skillName = LocalizationManager.T("Nhất Kiếm Đoạt Mệnh", "One-Sword Decapitation");
                info.description = LocalizationManager.T("Biến mất vào hư không và xuất hiện chém xuyên qua toàn bộ quái vật theo đường thẳng.", 
                                                         "Vanish into shadows and slash through a straight line of enemies at blinding speed.");
                info.baseDamage = 50;
                info.atkRatio = 1.50f;
                info.apRatio = 0.80f;
                info.damageType = DamageType.Physical;
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Player 4
                info.skillName = LocalizationManager.T("Địa Chấn Nện Đất", "Cataclysmic Ground Slam");
                info.description = LocalizationManager.T("Nhảy bổ lên không trung và nện búa sấm sét tạo sóng xung kích chấn động 360 độ làm choáng.", 
                                                         "Leap into the air and slam the warhammer down creating a 360-degree shockwave with stun.");
                info.baseDamage = 60;
                info.atkRatio = 1.60f;
                info.apRatio = 0.60f;
                info.damageType = DamageType.Physical;
                break;

            case CharacterManager.CHAR_DUNE_ASSASSIN: // Player 5
                info.skillName = LocalizationManager.T("Cuồng Bão Sa Ảnh (Chém X)", "Sandstorm Phantom (Cross Slash)");
                info.description = LocalizationManager.T("Triệu hồi lốc cát vây khốn quái vật và tung nhát chém chữ X xé rách chiến trường gây Chảy Máu liên tục.", 
                                                         "Conjure sand tornadoes to trap foes, followed by a lethal X-Slash inflicting severe bleed.");
                info.baseDamage = 48;
                info.atkRatio = 1.40f;
                info.apRatio = 0.75f;
                info.damageType = DamageType.Physical;
                break;

            default: // Vaelstrider (Player 1)
                info.skillName = LocalizationManager.T("Bão Kiếm Cổ Đại", "Ruin Whirlwind");
                info.description = LocalizationManager.T("Xoay vòng kiếm cổ đại quét sạch mọi quái vật xung quanh, đẩy lùi và hất tung mục tiêu.", 
                                                         "Spin the ancient greatsword in a devastating vortex that repels and knocks enemies airborne.");
                info.baseDamage = 42;
                info.atkRatio = 1.30f;
                info.apRatio = 0.70f;
                info.damageType = DamageType.Physical;
                break;
        }

        info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
        info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
        info.totalDamage = Mathf.Max(1, info.baseDamage + info.fromAtk + info.fromAP);
        return info;
    }

    /// <summary>
    /// Lấy thông tin chi tiết của Kỹ Năng Lướt Nhanh (Dash).
    /// </summary>
    public static SkillInfo GetDashSkillInfo(PlayerController controller, PlayerCombat combat)
    {
        if (controller == null) return null;

        int totalAtk = combat != null ? combat.TotalPhysicalDamage : 20;
        int totalAP = combat != null ? combat.TotalMagicDamage : 0;

        SkillInfo info = new SkillInfo();
        info.skillName = LocalizationManager.T("Tốc Biến Lướt Nhanh", "Shadow Dash");
        info.skillCategory = LocalizationManager.T("KỸ NĂNG DI CHUYỂN", "MOBILITY SKILL");
        info.cooldownText = $"{LocalizationManager.T("Hồi tích trữ", "Charge CD")}: 2.5s (3 {LocalizationManager.T("lần", "charges")})";
        info.description = LocalizationManager.T("Lướt nhanh né đòn theo hướng di chuyển. Trở nên bất khả xâm phạm và xô dạt quái vật cản đường.", 
                                                 "Swift dash granting brief invulnerability while displacing enemies in the path.");
        info.baseDamage = 12;
        info.atkRatio = 0.40f;
        info.apRatio = 0.25f;
        info.damageType = DamageType.Physical;

        info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
        info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
        info.totalDamage = Mathf.Max(1, info.baseDamage + info.fromAtk + info.fromAP);
        return info;
    }

    /// <summary>
    /// Lấy tổng sát thương đòn đánh sau khi áp dụng Dual-Scaling (%ATK + %AP).
    /// </summary>
    public static int CalculateDualScalingDamage(PlayerCombat combat, int baseHitDamage, float ratioAtk, float ratioAP)
    {
        if (combat == null) return baseHitDamage;
        int atk = combat.TotalPhysicalDamage;
        int ap = combat.TotalMagicDamage;
        int bonusFromAtk = Mathf.RoundToInt(atk * ratioAtk);
        int bonusFromAP = Mathf.RoundToInt(ap * ratioAP);
        return Mathf.Max(1, baseHitDamage + bonusFromAtk + bonusFromAP);
    }
}
