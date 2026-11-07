using UnityEngine;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Hero Skill Dual-Scaling Damage System (Chuẩn LMHT / Đấu Trường Chân Lý TFT)
/// 
/// Hệ thống tính toán Sát thương Kết Hợp (Dual-Scaling: %ATK + %AP):
/// - Sát thương đòn đánh và kỹ năng được đồng bộ 100% chính xác với Bảng Chỉ Số HUD và số sát thương nhảy thực tế trên đầu quái.
/// - Gộp sát thương lên TRÊN chung hàng với phần mô tả, không tách riêng 2 hàng ở dưới đáy.
/// - Không còn phần chia cấp độ [ 30 / 45 / 70 ] vì game không có level chiêu.
/// - Ghi rõ năng lượng tiêu hao cho từng chiêu: cố định (50 Năng lượng), trên giây (15/s), hoặc dùng HP (20% Máu tối đa).
/// </summary>
public static class HeroSkillDamageCalculator
{
    public class SkillInfo
    {
        public string skillName;
        public string skillCategory; // ĐÒN ĐÁNH THƯỜNG / KÍCH HOẠT / LƯỚT / TUYỆT KỸ
        public string cooldownText;
        public string energyCostText; // Năng lượng tiêu hao cố định, trên giây, hoặc máu
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
        info.energyCostText = LocalizationManager.T("Tiêu hao: Miễn phí", "Cost: Free");

        switch (charId)
        {
            case CharacterManager.CHAR_CELESTIAL_MAGE: // Pháp Sư Tinh Tú (Player 6)
                info.skillName = LocalizationManager.T("Tinh Tú Ma Cầu & Sét Lan", "Star Orbs & Chain Lightning");
                info.atkRatio = 0.30f;
                info.apRatio = 1.00f;
                info.damageType = DamageType.Magic;
                info.fromAP = totalAP;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = Mathf.Max(1, info.fromAP + info.fromAtk);
                info.description = LocalizationManager.T(
                    $"Phóng 3 luồng cầu ma pháp tự động truy tìm kẻ địch và bộc phá phóng sét lan sang quái vật lân cận. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép lên các mục tiêu trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)",
                    $"Fires 3 homing star orbs that detonate and chain lightning to adjacent enemies. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage to hit targets.\n\n<color=#D0D5DD>Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)");
                break;

            case CharacterManager.CHAR_AZURE_BLADE: // Lam Kiếm Sĩ (Player 3)
                info.skillName = LocalizationManager.T("Trọng Kiếm Khí & Sóng Xuyên Thấu", "Heavy Swordwave & Piercing Arc");
                info.atkRatio = 0.50f;
                info.apRatio = 1.00f;
                info.damageType = DamageType.Magic;
                info.fromAP = totalAP;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = Mathf.Max(1, info.fromAP + info.fromAtk);
                info.description = LocalizationManager.T(
                    $"Vung kiếm phóng luồng kiếm khí lam sắc xuyên thấu hàng loạt quái vật từ xa. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép lên các mục tiêu trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)",
                    $"Slashes forward releasing an azure swordwave that pierces through multiple enemies from afar. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage.\n\n<color=#D0D5DD>Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)");
                break;

            case CharacterManager.CHAR_GALE_RONIN: // Lôi Kiếm Lãng Tử (Player 2)
                info.skillName = LocalizationManager.T("Cư Hợp Lôi Kiếm Combo", "Iaido Thunder Combo");
                info.atkRatio = 1.00f;
                info.apRatio = 0.30f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Chuỗi trảm kích lôi ảnh chém chớp nhoáng diện rộng với đường kiếm sắc bén. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các mục tiêu trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Rapid thunder slash combo sweeping a wide arc with lethal razor slashes. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Cuồng Nộ Giáp Sắt (Player 4)
                info.skillName = LocalizationManager.T("Đại Chùy Nghiền Nát", "Heavy Warhammer Smash");
                info.atkRatio = 1.00f;
                info.apRatio = 0.25f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Vung đại chùy càn quét diện rộng đẩy lùi và nghiền nát lũ quái vật. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các mục tiêu trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Heavy warhammer sweeps repelling and crushing enemy swarms. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_DUNE_ASSASSIN: // Sa Mạc Thích Khách (Player 5)
                info.skillName = LocalizationManager.T("Song Đoản Đao Tốc Biến", "Twin Dagger Flurry");
                info.atkRatio = 1.00f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Combo song đoản đao lướt chém chớp nhoáng với tỉ lệ chí mạng cao. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các mục tiêu trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Lightning-fast dual dagger slices boasting high critical strike rate. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
                break;

            default: // Vaelstrider (Player 1)
                info.skillName = LocalizationManager.T("Trảm Kích Kiếm Sĩ Cổ Đại", "Ancient Blade Strike");
                info.atkRatio = 1.00f;
                info.apRatio = 0.35f;
                info.damageType = DamageType.Physical;
                info.fromAtk = totalAtk;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = Mathf.Max(1, info.fromAtk + info.fromAP);
                info.description = LocalizationManager.T(
                    $"Vung kiếm cổ đại theo hình vòng cung dứt điểm kẻ địch trước mặt. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các mục tiêu trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Sweeping ancient sword slashes slicing foes ahead. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
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
            case CharacterManager.CHAR_CELESTIAL_MAGE: // Player 6 (Pháp Sư Tinh Tú)
                info.skillName = LocalizationManager.T("Thiên Thạch Ma Trận", "Celestial Meteor Storm");
                info.skillCategory = LocalizationManager.T("KÍCH HOẠT DIỆN RỘNG", "AREA ULTIMATE");
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 35 Năng lượng (25 khi Quá Tải, Khiên: 20/s)", "<sprite name=\"mana\"> Cost: 35 Energy (25 in Overdrive, Shield: 20/s)");
                info.atkRatio = 0.40f;
                info.apRatio = 1.30f;
                info.damageType = DamageType.Magic;
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.totalDamage = 40 + info.fromAP + info.fromAtk;
                info.description = LocalizationManager.T(
                    $"Triệu hồi cơn mưa thiên thạch từ không gian rơi xuống hủy diệt toàn bộ kẻ địch trên bản đồ. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép mỗi viên.\n\n<color=#D0D5DD>Sát Thương: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)",
                    $"Summon a catastrophic meteor storm obliterating all battlefield enemies. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage per meteor.\n\n<color=#D0D5DD>Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)");
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
                    $"Tích tụ cực hạn và phóng ra 5 luồng kiếm khí khổng lồ quét sạch toàn bộ góc chiến trường. Nó gây <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) sát thương phép mỗi luồng.\n\n<color=#D0D5DD>Sát Thương: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)",
                    $"Releases 5 giant fan-shaped sword arcs sweeping clean the battlefield. Deals <color=#4DEEEA><b>{info.totalDamage}</b></color> (<sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color> + <sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color>) magic damage per arc.\n\n<color=#D0D5DD>Damage: </color><color=#4DEEEA><b>{info.totalDamage}</b></color> = <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">) + <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">)");
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
                    $"Biến mất vào hư không và xuất hiện chém xuyên qua hàng loạt quái vật trong chớp mắt. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên kẻ địch trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Vanishes into shadows and slashes through a line of enemies at blinding speed. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
                break;

            case CharacterManager.CHAR_TITAN_BERSERKER: // Player 4 (Cuồng Nộ Giáp Sắt)
                info.skillName = LocalizationManager.T("Địa Chấn Nện Đất", "Cataclysmic Ground Slam");
                info.skillCategory = LocalizationManager.T("VẬN SỨC & NỆN ĐẤT", "CHANNEL & SLAM");
                info.energyCostText = LocalizationManager.T("<sprite name=\"mana\"> Tiêu hao: 45 Năng lượng (Gồng: 15/s)", "<sprite name=\"mana\"> Cost: 45 Energy (Channel: 15/s)");
                info.atkRatio = 1.40f;
                info.apRatio = 0.50f;
                info.damageType = DamageType.Physical;
                info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
                info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
                info.totalDamage = 50 + info.fromAtk + info.fromAP;
                info.description = LocalizationManager.T(
                    $"Nện đại chùy tạo sóng xung kích 360 độ làm choáng kẻ địch xung quanh trong 1.5s. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các kẻ địch trúng chấn động.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Slams the warhammer down creating a 360-degree shockwave that stuns foes for 1.5s. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
                break;

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
                    $"Hiến tế máu triệu hồi lốc cát vây khốn quái vật và tung nhát chém chữ X xé rách chiến trường gây Chảy Máu liên tục. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý diện rộng.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Sacrifices health to conjure sand vortices and execute an X-slash inflicting Bleed. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
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
                    $"Xoay tròn đại kiếm cổ đại liên tục hất tung toàn bộ quái vật xung quanh. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên các kẻ địch trúng đòn.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
                    $"Spins the ancient greatsword knocking surrounding enemies airborne. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");
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
        info.energyCostText = LocalizationManager.T("Tiêu hao: Miễn phí", "Cost: Free");
        info.cooldownText = $"{LocalizationManager.T("Hồi chiêu", "Cooldown")}: 2.5s (3 {LocalizationManager.T("lần", "charges")})";
        info.atkRatio = 0.40f;
        info.apRatio = 0.25f;
        info.damageType = DamageType.Physical;
        info.fromAtk = Mathf.RoundToInt(totalAtk * info.atkRatio);
        info.fromAP = Mathf.RoundToInt(totalAP * info.apRatio);
        info.totalDamage = 12 + info.fromAtk + info.fromAP;

        info.description = LocalizationManager.T(
            $"Lướt nhanh né đòn và trở nên bất khả xâm phạm trong thoáng chốc, hất văng quái vật cản đường. Nó gây <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) sát thương vật lý lên kẻ địch va phải.\n\n<color=#D0D5DD>Sát Thương: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)",
            $"Swift dash granting invulnerability and knocking back pathway enemies. Deals <color=#FFA500><b>{info.totalDamage}</b></color> (<sprite name=\"atk\"> <color=#FFA500>{info.fromAtk}</color> + <sprite name=\"ap\"> <color=#4DEEEA>{info.fromAP}</color>) physical damage.\n\n<color=#D0D5DD>Damage: </color><color=#FFA500><b>{info.totalDamage}</b></color> = <color=#FFA500>{info.fromAtk}</color> (<sprite name=\"atk\">) + <color=#4DEEEA>{info.fromAP}</color> (<sprite name=\"ap\">)");

        info.skillIcon = MobileActionButton.GetHeroActionIcon(charId, MobileActionButton.ButtonType.Dash);
        return info;
    }
}
