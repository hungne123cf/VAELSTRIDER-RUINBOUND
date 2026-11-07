using UnityEngine;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Hero Skill Dual-Scaling Damage System (Chuẩn LMHT / Đấu Trường Chân Lý TFT)
/// 
/// Hệ thống tính toán Sát thương & Thông số Kỹ năng Chuẩn xác 100% theo Mã Nguồn Thực Tế:
/// - Mô tả đầy đủ cả cơ chế Đánh Thường lẫn cơ chế ĐÈ GIỮ (GỒNG CHIÊU / TỤ LỰC) của từng vị tướng.
/// - Hiển thị tỉ lệ phần trăm tỉ lệ (% Scaling: 100% ATK, 35% AP...) ở dòng công thức chuẩn LMHT/TFT.
/// - Phân định rõ ràng chiêu nào gây sát thương, chiêu nào thuần di chuyển né đòn (Dash Dune Assassin không gây dame).
/// - Chi phí tiêu hao chính xác tuyệt đối: Cố định (Năng lượng), Theo giây (Gồng), hoặc Trừ Máu (Hiến Tế HP cho Dune Assassin).
/// - Nếu chiêu không tốn gì thì ghi "Tiêu hao: 0".
/// </summary>
public static class HeroSkillDamageCalculator
{
    public class SkillInfo
    {
        public string skillName;
        public string skillCategory; // ĐÒN ĐÁNH THƯỜNG / KÍCH HOẠT / LƯỚT / TUYỆT KỸ
        public string cooldownText;
        public string energyCostText; // Năng lượng tiêu hao hoặc máu
        public string description;
        public int totalDamage;
        public int fromAtk;
        public int fromAP;
        public float atkRatio;
        public float apRatio;
        public DamageType damageType;
        public bool hasDamage = true; // Chiêu có gây sát thương hay không
        public Sprite skillIcon;
    }

    /// <summary>
    /// Lấy thông tin chi tiết của Đòn Đánh Thường & Cơ chế Đè Giữ (Gồng Chiêu / Tụ Lực).
    /// </summary>
    public static SkillInfo GetAttackSkillInfo(PlayerCombat combat)
    {
        if (combat == null) return null;

        string charId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
        int totalAtk = combat.TotalPhysicalDamage;
        int totalAP = combat.TotalMagicDamage;

        SkillInfo info = new SkillInfo();
        info.cooldownText = LocalizationManager.T("Hồi chiêu: 0s", "Cooldown: 0s");
        info.skillCategory = LocalizationManager.T("ĐÒN ĐÁNH & GỒNG CHIÊU", "ATTACK & CHARGE");
        info.hasDamage = true;

        switch (charId)
        {
            case CharacterManager.CHAR_DUNE_ASSASSIN: // Sa Mạc Thích Khách (Player 5)
                info.skillName = LocalizationManager.T("Song Đoản Đao & Tam Sa Lốc Xoáy", "Twin Daggers & Sand Vortex");
                info.energyCostText = LocalizationManager.T("Tiêu hao: 0 (Đánh thường) • <sprite name=\"hp\"> 8% Máu (Gồng lốc)", "Cost: 0 (Attack) • <sprite name=\"hp\"> 8% HP (Charge)");
                info.atkRatio = 1.00f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"• <b>Nhấp đánh:</b> Combo song đoản đao lướt chém chớp nhoáng với tỉ lệ chí mạng cao, tích nộ và nội năng. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý.\n" +
                    $"• <b>Đè giữ (Gồng):</b> Vào Thế Thủ Ảo Ảnh (giảm 70% sát thương), nhả nút để phóng <b>Tam Sa Lốc Xoáy</b> cuốn phăng và gây Chảy Máu quái vật liên tục (tiêu hao 8% Máu tối đa, 12% khi đầy nộ).\n\n" +
                    $"<color=#D0D5DD>Sát Thương Đánh Thường: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (35% <sprite name=\"ap\">)",
                    $"• <b>Tap:</b> Lightning-fast dual dagger flurry with high critical strike rate. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n" +
                    $"• <b>Hold (Charge):</b> Enters Mirage Guard (70% damage reduction), release to fire <b>Sand Vortex</b> sweeping and bleeding enemies (costs 8% Max HP, 12% in Rage).\n\n" +
                    $"<color=#D0D5DD>Attack Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (35% <sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_CELESTIAL_MAGE: // Pháp Sư Tinh Tú (Player 6)
                info.skillName = LocalizationManager.T("Tinh Tú Ma Cầu & Khiên Ma Trận", "Star Orbs & Barrier Shield");
                info.energyCostText = LocalizationManager.T("Tiêu hao: 0 (Đánh thường) • <sprite name=\"mana\"> 20/s (Gồng khiên)", "Cost: 0 (Attack) • <sprite name=\"mana\"> 20/s (Barrier)");
                info.atkRatio = 0.30f;
                info.apRatio = 1.00f;
                info.damageType = DamageType.Magic;
                info.fromAP = totalAP;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = Mathf.Max(1, info.fromAP + info.fromAtk);
                info.description = LocalizationManager.T(
                    $"• <b>Nhấp đánh:</b> Phóng 3 luồng cầu ma pháp tự động truy tìm kẻ địch và phóng sét lan sang quái vật lân cận. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép.\n" +
                    $"• <b>Đè giữ (Gồng):</b> Kích hoạt <b>Khiên Ma Trận</b> hấp thụ 80% sát thương nhận vào và đẩy lùi kẻ địch áp sát (tiêu hao 20 Năng lượng/giây), nhả nút bộc phát sóng đẩy quái vật.\n\n" +
                    $"<color=#D0D5DD>Sát Thương Đánh Thường: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (100% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (30% <sprite name=\"atk\">)",
                    $"• <b>Tap:</b> Fires 3 homing star orbs that detonate and chain lightning. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage.\n" +
                    $"• <b>Hold (Charge):</b> Activates <b>Barrier Matrix</b> absorbing 80% damage and pushing enemies back (costs 20 Energy/sec).\n\n" +
                    $"<color=#D0D5DD>Attack Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (100% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (30% <sprite name=\"atk\">)");
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Lam Kiếm Sĩ (Player 3)
                info.skillName = LocalizationManager.T("Trọng Kiếm Khí & Tụ Lực Băng Kiếm", "Heavy Swordwave & Cryo Charge");
                info.energyCostText = LocalizationManager.T("Tiêu hao: 0 (Cả Đánh & Tụ lực)", "Cost: 0 (Attack & Charge)");
                info.atkRatio = 0.50f;
                info.apRatio = 1.00f;
                info.damageType = DamageType.Magic;
                info.fromAP = totalAP;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = Mathf.Max(1, info.fromAP + info.fromAtk);
                info.description = LocalizationManager.T(
                    $"• <b>Nhấp đánh:</b> Vung kiếm phóng kiếm khí lam sắc xuyên thấu quái vật từ xa. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép.\n" +
                    $"• <b>Đè giữ (Gồng):</b> Tụ lực tạo hào quang Băng Hàn cực hạn, nhả nút phóng <b>Đại Kiếm Khí Khổng Lồ</b> tầm xa hơn với sát thương khuếch đại lên đến <b>x2.2 lần</b> và xuyên qua vô số quái vật.\n\n" +
                    $"<color=#D0D5DD>Sát Thương Đánh Thường: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (100% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (50% <sprite name=\"atk\">)",
                    $"• <b>Tap:</b> Slashes releasing piercing swordwaves. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage.\n" +
                    $"• <b>Hold (Charge):</b> Focuses cryo aura to unleash a <b>Giant Swordwave</b> dealing up to <b>2.2x</b> damage piercing all foes.\n\n" +
                    $"<color=#D0D5DD>Attack Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (100% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (50% <sprite name=\"atk\">)");
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Cuồng Nộ Giáp Sắt (Player 4)
                info.skillName = LocalizationManager.T("Đại Chùy & Vận Sức Chấn Địa", "Warhammer & Earthshaker Charge");
                info.energyCostText = LocalizationManager.T("Tiêu hao: 0 (Đánh thường) • <sprite name=\"mana\"> 15/s (Gồng chấn địa)", "Cost: 0 (Attack) • <sprite name=\"mana\"> 15/s (Charge)");
                info.atkRatio = 1.00f;
                info.apRatio = 0.25f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"• <b>Nhấp đánh:</b> Vung đại chùy càn quét diện rộng đẩy lùi quái vật. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý.\n" +
                    $"• <b>Đè giữ (Gồng):</b> Giơ búa thủ thế giảm 50% sát thương (tiêu hao 15 Năng lượng/giây), nhả nút nện búa gây chấn động làm choáng quái vật trong 1.2s và gây thêm 150% sát thương.\n\n" +
                    $"<color=#D0D5DD>Sát Thương Đánh Thường: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (25% <sprite name=\"ap\">)",
                    $"• <b>Tap:</b> Heavy warhammer sweeps repelling foes. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n" +
                    $"• <b>Hold (Charge):</b> Guards taking 50% less damage (costs 15 Energy/sec), release to slam stunning foes for 1.2s and dealing 150% extra damage.\n\n" +
                    $"<color=#D0D5DD>Attack Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (25% <sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Lôi Kiếm Lãng Tử (Player 2)
                info.skillName = LocalizationManager.T("Cư Hợp Lôi Kiếm & Tụ Lực Trảm", "Iaido Combo & Thunder Slash");
                info.energyCostText = LocalizationManager.T("Tiêu hao: 0 (Đánh thường) • <sprite name=\"mana\"> 15 (Gồng Cư Hợp)", "Cost: 0 (Attack) • <sprite name=\"mana\"> 15 (Charge)");
                info.atkRatio = 1.00f;
                info.apRatio = 0.30f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"• <b>Nhấp đánh:</b> Chuỗi trảm kích lôi ảnh chém chớp nhoáng diện rộng. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý.\n" +
                    $"• <b>Đè giữ (Gồng):</b> Vào thế tụ lực Cư Hợp kiếm đạo (tiêu hao 15 Năng lượng), nhả nút lướt chém xuyên qua hàng loạt mục tiêu theo đường thẳng gây 180% sát thương cực lớn.\n\n" +
                    $"<color=#D0D5DD>Sát Thương Đánh Thường: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (30% <sprite name=\"ap\">)",
                    $"• <b>Tap:</b> Rapid thunder slash combo sweeping wide arc. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n" +
                    $"• <b>Hold (Charge):</b> Focuses Iaido stance (costs 15 Energy), release to dash and pierce all straight-line targets dealing 180% damage.\n\n" +
                    $"<color=#D0D5DD>Attack Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (30% <sprite name=\"ap\">)");
                break;

            default: // Vaelstrider (Player 1)
                info.skillName = LocalizationManager.T("Trảm Kích & Thế Thủ Phản Đòn", "Blade Strike & Parry Stance");
                info.energyCostText = LocalizationManager.T("Tiêu hao: 0 (Đánh thường) • <sprite name=\"mana\"> 15/s (Gồng thủ)", "Cost: 0 (Attack) • <sprite name=\"mana\"> 15/s (Parry)");
                info.atkRatio = 1.00f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"• <b>Nhấp đánh:</b> Vung kiếm cổ đại theo hình vòng cung dứt điểm kẻ địch. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý.\n" +
                    $"• <b>Đè giữ (Gồng):</b> Vào Thế Thủ Kiếm Thuẫn giảm 65% sát thương nhận vào (tiêu hao 15 Năng lượng/giây), nhả nút phóng <b>Đại Sóng Kiếm Hoàng Kim</b> càn quét diện rộng.\n\n" +
                    $"<color=#D0D5DD>Sát Thương Đánh Thường: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (35% <sprite name=\"ap\">)",
                    $"• <b>Tap:</b> Sweeping ancient sword slash. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n" +
                    $"• <b>Hold (Charge):</b> Enters Parry Stance taking 65% less damage (costs 15 Energy/sec), release to unleash <b>Radiant Swordwave</b> sweeping enemies.\n\n" +
                    $"<color=#D0D5DD>Attack Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (100% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (35% <sprite name=\"ap\">)");
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
        info.hasDamage = true;

        switch (charId)
        {
            case CharacterManager.CHAR_DUNE_ASSASSIN: // Player 5 (Sa Mạc Thích Khách)
                info.skillName = LocalizationManager.T("Cuồng Bão Sa Ảnh (Chém X)", "Sandstorm Phantom (Cross Slash)");
                info.skillCategory = LocalizationManager.T("HIẾN TẾ MÁU", "BLOOD SACRIFICE");
                info.energyCostText = LocalizationManager.T("<sprite name=\"hp\"> Tiêu hao: 20% Máu tối đa", "<sprite name=\"hp\"> Cost: 20% Max HP");
                info.atkRatio = 1.20f;
                info.apRatio = 0.60f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 45 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Hiến tế 20% máu tối đa, triệu hồi lốc cát vây hãm quái vật và tung nhát chém chữ X xé rách chiến trường gây Chảy Máu liên tục. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý diện rộng.\n\n<color=#D0D5DD>Sát Thương Tuyệt Kỹ: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 45 + <color=#FFA500>{info.fromAtk}</color> (120% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (60% <sprite name=\"ap\">)",
                    $"Sacrifices 20% max HP to conjure sand vortices and execute an X-slash inflicting Bleed. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Ultimate Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 45 + <color=#FFA500>{info.fromAtk}</color> (120% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (60% <sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_CELESTIAL_MAGE: // Player 6 (Pháp Sư Tinh Tú)
                info.skillName = LocalizationManager.T("Thiên Thạch Ma Trận", "Celestial Meteor Storm");
                info.skillCategory = LocalizationManager.T("KÍCH HOẠT DIỆN RỘNG", "AREA ULTIMATE");
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 35 Năng lượng (25 khi Quá Tải)", "<sprite name=\"mana\"> Cost: 35 Energy (25 in Overdrive)");
                info.atkRatio = 0.40f;
                info.apRatio = 1.30f;
                info.damageType = DamageType.Magic;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = 40 + info.fromAP + info.fromAtk;
                info.description = LocalizationManager.T(
                    $"Triệu hồi cơn mưa thiên thạch từ không gian rơi xuống hủy diệt toàn bộ kẻ địch trên bản đồ. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép mỗi viên.\n\n<color=#D0D5DD>Sát Thương Tuyệt Kỹ: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = 40 + <color=#4DEEEA>{info.fromAP}</color> (130% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (40% <sprite name=\"atk\">)",
                    $"Summon a catastrophic meteor storm obliterating all battlefield enemies. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage per meteor.\n\n<color=#D0D5DD>Ultimate Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = 40 + <color=#4DEEEA>{info.fromAP}</color> (130% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (40% <sprite name=\"atk\">)");
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Player 3 (Lam Kiếm Sĩ)
                info.skillName = LocalizationManager.T("Bão Sóng Kiếm Nan Quạt", "Fan-Blade Wave Barrage");
                info.skillCategory = LocalizationManager.T("KÍCH HOẠT", "ACTIVATION");
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 50 Năng lượng", "<sprite name=\"mana\"> Cost: 50 Energy");
                info.atkRatio = 0.50f;
                info.apRatio = 1.10f;
                info.damageType = DamageType.Magic;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = 32 + info.fromAP + info.fromAtk;
                info.description = LocalizationManager.T(
                    $"Tích tụ cực hạn và phóng ra 5 luồng kiếm khí khổng lồ quét sạch toàn bộ góc chiến trường. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép mỗi luồng.\n\n<color=#D0D5DD>Sát Thương Tuyệt Kỹ: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = 32 + <color=#4DEEEA>{info.fromAP}</color> (110% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (50% <sprite name=\"atk\">)",
                    $"Releases 5 giant fan-shaped sword arcs sweeping clean the battlefield. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage per arc.\n\n<color=#D0D5DD>Ultimate Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = 32 + <color=#4DEEEA>{info.fromAP}</color> (110% <sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (50% <sprite name=\"atk\">)");
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Player 4 (Cuồng Nộ Giáp Sắt)
                info.skillName = LocalizationManager.T("Địa Chấn Nện Đất", "Cataclysmic Ground Slam");
                info.skillCategory = LocalizationManager.T("KÍCH HOẠT", "ACTIVATION");
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 45 Năng lượng", "<sprite name=\"mana\"> Cost: 45 Energy");
                info.atkRatio = 1.40f;
                info.apRatio = 0.50f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 50 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Nện đại chùy tạo sóng xung kích 360 độ làm choáng kẻ địch xung quanh trong 1.5s. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các kẻ địch trúng chấn động.\n\n<color=#D0D5DD>Sát Thương Tuyệt Kỹ: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 50 + <color=#FFA500>{info.fromAtk}</color> (140% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (50% <sprite name=\"ap\">)",
                    $"Slams the warhammer down creating a 360-degree shockwave that stuns foes for 1.5s. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Ultimate Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 50 + <color=#FFA500>{info.fromAtk}</color> (140% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (50% <sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Player 2 (Lôi Kiếm Lãng Tử)
                info.skillName = LocalizationManager.T("Nhất Kiếm Đoạt Mệnh", "One-Sword Decapitation");
                info.skillCategory = LocalizationManager.T("KÍCH HOẠT", "ACTIVATION");
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 40 Năng lượng", "<sprite name=\"mana\"> Cost: 40 Energy");
                info.atkRatio = 1.30f;
                info.apRatio = 0.70f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 40 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Biến mất vào hư không và xuất hiện chém xuyên qua hàng loạt quái vật trong chớp mắt. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên kẻ địch trúng đòn.\n\n<color=#D0D5DD>Sát Thương Tuyệt Kỹ: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 40 + <color=#FFA500>{info.fromAtk}</color> (130% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (70% <sprite name=\"ap\">)",
                    $"Vanishes into shadows and slashes through a line of enemies at blinding speed. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Ultimate Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 40 + <color=#FFA500>{info.fromAtk}</color> (130% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (70% <sprite name=\"ap\">)");
                break;

            default: // Vaelstrider (Player 1)
                info.skillName = LocalizationManager.T("Bão Kiếm Cổ Đại", "Ruin Whirlwind");
                info.skillCategory = LocalizationManager.T("KÍCH HOẠT", "ACTIVATION");
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 50 Năng lượng", "<sprite name=\"mana\"> Cost: 50 Energy");
                info.atkRatio = 1.10f;
                info.apRatio = 0.60f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 35 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Xoay tròn đại kiếm cổ đại liên tục hất tung toàn bộ quái vật xung quanh. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các kẻ địch trúng đòn.\n\n<color=#D0D5DD>Sát Thương Tuyệt Kỹ: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 35 + <color=#FFA500>{info.fromAtk}</color> (110% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (60% <sprite name=\"ap\">)",
                    $"Spins the ancient greatsword knocking surrounding enemies airborne. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Ultimate Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 35 + <color=#FFA500>{info.fromAtk}</color> (110% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (60% <sprite name=\"ap\">)");
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

        switch (charId)
        {
            case CharacterManager.CHAR_DUNE_ASSASSIN: // Sa Mạc Thích Khách (Player 5)
                info.skillName = LocalizationManager.T("Sa Ảnh Tốc Biến", "Mirage Dash");
                info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 3.5s (3 {LocalizationManager.T("lần tích trữ", "charges")})";
                info.energyCostText = LocalizationManager.T("<sprite name=\"hp\"> Tiêu hao: 4% Máu tối đa", "<sprite name=\"hp\"> Cost: 4% Max HP");
                info.hasDamage = false; // KHÔNG GÂY DAMAGE
                info.totalDamage = 0;
                info.description = LocalizationManager.T(
                    $"Lướt nhanh tốc độ cực hạn né đòn và miễn nhiễm sát thương trong thoáng chốc. <b>Không gây sát thương</b>.\n" +
                    $"• <b>Tích trữ:</b> Lưu trữ tối đa 3 lần lướt liên hoàn (giãn cách 0.22s giữa các lần lướt).\n" +
                    $"• <b>Nội tại kích hoạt:</b> Mỗi lần lướt giảm <b>1.5s</b> hồi chiêu Tuyệt Kỹ và tính 1 lượt dùng chiêu để hồi lại 10% HP đã mất.",
                    $"Ultra-fast evasive dash granting invulnerability. <b>Deals no damage</b>.\n" +
                    $"• <b>Charges:</b> Holds up to 3 consecutive dash charges (0.22s interval).\n" +
                    $"• <b>Passive Trigger:</b> Each dash reduces Ultimate cooldown by <b>1.5s</b> and counts toward restoring 10% lost HP.");
                break;

            case CharacterManager.CHAR_CELESTIAL_MAGE: // Pháp Sư Tinh Tú (Player 6)
                info.skillName = LocalizationManager.T("Tốc Biến Hư Không", "Void Blink");
                info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 5.0s";
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 15 Năng lượng", "<sprite name=\"mana\"> Cost: 15 Energy");
                info.hasDamage = false; // Thuần dịch chuyển
                info.totalDamage = 0;
                info.description = LocalizationManager.T(
                    $"Dịch chuyển tức thời 4.5 mét xuyên qua vật cản và quái vật theo hướng chỉ định. <b>Không gây sát thương</b>, giúp tức thời tái định vị né tránh vòng vây tử thần.",
                    $"Instant teleport blink across obstacles and enemy swarms. <b>Deals no damage</b>, allowing instant repositioning.");
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Lam Kiếm Sĩ (Player 3)
                info.skillName = LocalizationManager.T("Lướt Tái Lập Kiếm Thế", "Blade Stride");
                info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 5.0s";
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 20 Năng lượng", "<sprite name=\"mana\"> Cost: 20 Energy");
                info.hasDamage = false;
                info.totalDamage = 0;
                info.description = LocalizationManager.T(
                    $"Lướt nhanh né đòn bất tử và <b>lập tức hồi lại đòn đánh thường</b> để tung kiếm khí liên hoàn không độ trễ. <b>Không gây sát thương trực tiếp</b>.",
                    $"Quick evasive dash that <b>instantly resets basic attack cooldown</b> for rapid swordwave barrages. <b>Deals no direct damage</b>.");
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Cuồng Nộ Giáp Sắt (Player 4)
                info.skillName = LocalizationManager.T("Xung Kích Chém Xoay 360°", "Whirlwind Dash Spin");
                info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 5.0s";
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 20 Năng lượng", "<sprite name=\"mana\"> Cost: 20 Energy");
                info.hasDamage = true;
                info.atkRatio = 0.60f;
                info.apRatio = 0.30f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 18 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Lao vọt tới trước đồng thời xoay tròn đại chùy 360 độ càn quét mọi quái vật cản đường. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý và hất văng quái vật.\n\n<color=#D0D5DD>Sát Thương Xung Kích: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 18 + <color=#FFA500>{info.fromAtk}</color> (60% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (30% <sprite name=\"ap\">)",
                    $"Charges forward spinning the warhammer 360 degrees. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage knocking back enemies.\n\n<color=#D0D5DD>Charge Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 18 + <color=#FFA500>{info.fromAtk}</color> (60% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (30% <sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Lôi Kiếm Lãng Tử (Player 2)
                info.skillName = LocalizationManager.T("Tàn Ảnh Tốc Biến & Thu Hồi Bóng", "Shadow Dash & Recall");
                info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 4.5s";
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 15 Năng lượng (Nhịp 1) • 0 (Thu hồi)", "<sprite name=\"mana\"> Cost: 15 Energy (Dash) • 0 (Recall)");
                info.hasDamage = false;
                info.totalDamage = 0;
                info.description = LocalizationManager.T(
                    $"• <b>Nhịp 1:</b> Tốc biến 4.8m tới trước và để lại chiếc bóng tàn ảnh tại điểm xuất phát (tiêu hao 15 Năng lượng), làm choáng 0.5s kẻ địch lân cận.\n" +
                    $"• <b>Nhịp 2 (trong 4.5s):</b> Bấm lại để lập tức dịch chuyển ngược về chiếc bóng ban đầu (tiêu hao: 0).",
                    $"• <b>Stage 1:</b> Teleports 4.8m leaving a shadow clone behind (costs 15 Energy), stunning nearby foes for 0.5s.\n" +
                    $"• <b>Stage 2 (within 4.5s):</b> Press again to instantly recall back to the shadow clone (Cost: 0).");
                break;

            default: // Vaelstrider (Player 1)
                info.skillName = LocalizationManager.T("Thánh Thuẫn Phản Đòn", "Radiant Riposte Dash");
                info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 5.0s";
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 20 Năng lượng", "<sprite name=\"mana\"> Cost: 20 Energy");
                info.hasDamage = true;
                info.atkRatio = 0.40f;
                info.apRatio = 0.25f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 12 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Lướt nhanh né đòn bất tử trong 0.2s và hất văng quái cản đường. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên kẻ địch va phải.\n\n<color=#D0D5DD>Sát Thương Phản Đòn: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 12 + <color=#FFA500>{info.fromAtk}</color> (40% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (25% <sprite name=\"ap\">)",
                    $"Swift dash granting invulnerability and knocking back pathway enemies. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Riposte Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = 12 + <color=#FFA500>{info.fromAtk}</color> (40% <sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (25% <sprite name=\"ap\">)");
                break;
        }

        info.skillIcon = MobileActionButton.GetHeroActionIcon(charId, MobileActionButton.ButtonType.Dash);
        return info;
    }
}
