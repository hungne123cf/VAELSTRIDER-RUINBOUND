using UnityEngine;
using System.IO;

/// <summary>
/// Quản lý và cung cấp toàn bộ biểu tượng đồ họa (Sprite) riêng biệt cho từng Lõi Nâng Cấp (Augments) & Cổ Vật.
/// - Mỗi Lõi Nâng Cấp sở hữu một biểu tượng độc nhất đặc trưng (Sword, Shield, Divine Shield, Heart, Blood, Wings, Flame, Lightning, Skull, Meteor, Crown, Demon...)
/// - Đồng bộ hoàn hảo giữa màn hình Chọn Lõi (LevelUpChoiceUI), Tiệm Cổ Vật (WanderingShopUI) và Thanh Lõi Trong Trận Đấu (AugmentHUDBar)
/// </summary>
public static class AugmentIconHelper
{
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> _cache
        = new System.Collections.Generic.Dictionary<string, Sprite>();

    public static string GetIconFileName(AugmentDatabase.AugmentType type)
    {
        switch (type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
            case AugmentDatabase.AugmentType.LaceratingEdge:
            case AugmentDatabase.AugmentType.ComboMastery:
            case AugmentDatabase.AugmentType.ShadowAmbush:
            case AugmentDatabase.AugmentType.HeavyImpact:
            case AugmentDatabase.AugmentType.RelentlessAssault:
            case AugmentDatabase.AugmentType.GiantSlayer:
                return "sword_slash.png";

            case AugmentDatabase.AugmentType.BonusHealth:
            case AugmentDatabase.AugmentType.AbundantVitality:
            case AugmentDatabase.AugmentType.SecondWind:
                return "heart_vitality.png";

            case AugmentDatabase.AugmentType.BonusSpeed:
            case AugmentDatabase.AugmentType.FleetWind:
            case AugmentDatabase.AugmentType.SwiftShadow:
                return "wings_fleet.png";

            case AugmentDatabase.AugmentType.BonusCritChance:
            case AugmentDatabase.AugmentType.BonusCritDamage:
                return "eye_focus.png";

            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.IronBastion:
            case AugmentDatabase.AugmentType.AncientShield:
            case AugmentDatabase.AugmentType.ArcaneShield:
            case AugmentDatabase.AugmentType.UnwaveringWill:
            case AugmentDatabase.AugmentType.PrismaticTitan:
                return "shield_aegis.png";

            case AugmentDatabase.AugmentType.BonusCooldownReduction:
                return "hourglass_cdr.png";

            case AugmentDatabase.AugmentType.BonusMaxEnergy:
            case AugmentDatabase.AugmentType.SurgingPulse:
                return "energy_pulse.png";

            case AugmentDatabase.AugmentType.BonusLifesteal:
            case AugmentDatabase.AugmentType.VampireBlood:
            case AugmentDatabase.AugmentType.BloodGodTribute:
            case AugmentDatabase.AugmentType.LifeTransfusion:
                return "blood_drop.png";

            case AugmentDatabase.AugmentType.RagingFire:
            case AugmentDatabase.AugmentType.BattleFrenzy:
                return "flame_fire.png";

            case AugmentDatabase.AugmentType.BerserkerMode:
            case AugmentDatabase.AugmentType.DeathsTouch:
                return "skull_death.png";

            case AugmentDatabase.AugmentType.PoisonTouch:
                return "poison_flask.png";

            case AugmentDatabase.AugmentType.RegenerativeCore:
                return "potion_regen.png";

            case AugmentDatabase.AugmentType.EchoStrike:
            case AugmentDatabase.AugmentType.StaticOverload:
            case AugmentDatabase.AugmentType.ArcaneOverdrive:
                return "lightning_bolt.png";

            case AugmentDatabase.AugmentType.SoulHarvest:
                return "scythe_harvest.png";

            case AugmentDatabase.AugmentType.UltimateCharge:
            case AugmentDatabase.AugmentType.GodforgedArsenal:
            case AugmentDatabase.AugmentType.BountyHunter:
                return "godforged_arsenal.png";

            case AugmentDatabase.AugmentType.DemonicPact:
                return "demon_pact.png";

            case AugmentDatabase.AugmentType.UndyingSovereign:
            case AugmentDatabase.AugmentType.AscendedDivinity:
                return "crown_sovereign.png";

            case AugmentDatabase.AugmentType.CelestialCascade:
            case AugmentDatabase.AugmentType.BonusSpellVamp:
            case AugmentDatabase.AugmentType.ChainReaction:
                return "meteor_cascade.png";

            case AugmentDatabase.AugmentType.StormDancer:
                return "cyclone_storm.png";

            case AugmentDatabase.AugmentType.EchoingVoid:
                return "meteor_cascade.png";

            case AugmentDatabase.AugmentType.DivineAegis:
                return "divine_shield.png";

            // ─── Lõi Nguyên Tố (Elemental Realm) ───
            case AugmentDatabase.AugmentType.FlameAdaptation:
            case AugmentDatabase.AugmentType.MagmaEruption:
                return "flame_fire.png";

            case AugmentDatabase.AugmentType.AcidMembrane:
            case AugmentDatabase.AugmentType.VirulentBloom:
                return "poison_flask.png";

            case AugmentDatabase.AugmentType.GlacialSoul:
            case AugmentDatabase.AugmentType.AbsoluteZeroNova:
                return "divine_shield.png";

            case AugmentDatabase.AugmentType.VoidTranscendence:
                return "demon_pact.png";

            case AugmentDatabase.AugmentType.ShadowMirroring:
                return "scythe_harvest.png";

            default:
                return "sword_slash.png";
        }
    }

    /// <summary>
    /// Lấy Sprite đồ họa đặc trưng cho từng loại Lõi Nâng Cấp
    /// - Riêng các Lõi Đặc Quyền Tướng: Lấy trực tiếp Sprite chiêu thức của chính tướng đó
    /// </summary>
    public static Sprite GetAugmentSprite(AugmentDatabase.AugmentType type)
    {
        // 1. Ưu tiên Lõi Đặc Quyền của Tướng: Dùng đúng Icon đồ họa chiêu thức của tướng đó!
        switch (type)
        {
            case AugmentDatabase.AugmentType.TripleMirageTornado:
                return MobileActionButton.GetHeroActionIcon(CharacterManager.CHAR_DUNE_ASSASSIN, MobileActionButton.ButtonType.Attack);
            case AugmentDatabase.AugmentType.MeteorSupercluster:
                return MobileActionButton.GetHeroActionIcon(CharacterManager.CHAR_CELESTIAL_MAGE, MobileActionButton.ButtonType.Skill);
            case AugmentDatabase.AugmentType.TripleRuinGale:
                return MobileActionButton.GetHeroActionIcon(CharacterManager.CHAR_VAELSTRIDER, MobileActionButton.ButtonType.Skill);
            case AugmentDatabase.AugmentType.EightWayTempest:
                return MobileActionButton.GetHeroActionIcon(CharacterManager.CHAR_GALE_RONIN, MobileActionButton.ButtonType.Skill);
            case AugmentDatabase.AugmentType.TwinFrostNova:
                return MobileActionButton.GetHeroActionIcon(CharacterManager.CHAR_AZURE_BLADE, MobileActionButton.ButtonType.Skill);
            case AugmentDatabase.AugmentType.CataclysmicQuake:
                return MobileActionButton.GetHeroActionIcon(CharacterManager.CHAR_TITAN_BERSERKER, MobileActionButton.ButtonType.Skill);
        }

        string filename = GetIconFileName(type);
        if (_cache.TryGetValue(filename, out Sprite cached) && cached != null)
        {
            return cached;
        }

        string assetPath = $"Assets/Sprites/UI/Augments/{filename}";
        Sprite spr = null;

#if UNITY_EDITOR
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif

        if (spr == null)
        {
            string diskPath = Path.Combine(Application.dataPath, "Sprites/UI/Augments", filename);
            if (File.Exists(diskPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(diskPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AugmentIconHelper] Không thể tải icon từ {diskPath}: {ex.Message}");
                }
            }
        }

        // Fallback sang các sprite cơ bản của project nếu cần
        if (spr == null)
        {
            if (filename.Contains("sword")) spr = WhiteIconLoader.GetSprite("Sword") ?? Resources.Load<Sprite>("UI/Attack");
            else if (filename.Contains("wing") || filename.Contains("dash")) spr = Resources.Load<Sprite>("UI/Dash");
            else if (filename.Contains("shield")) spr = WhiteIconLoader.GetSprite("Hero");
            else if (filename.Contains("meteor") || filename.Contains("lightning")) spr = WhiteIconLoader.GetSprite("Star");
            else spr = Resources.Load<Sprite>("UI/Ulti");
        }

        if (spr != null)
        {
            _cache[filename] = spr;
        }

        return spr;
    }

    /// <summary>
    /// Lấy mã chữ viết tắt Latinh chuẩn
    /// </summary>
    public static string GetAugmentShortCode(AugmentDatabase.AugmentType type)
    {
        switch (type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
            case AugmentDatabase.AugmentType.LaceratingEdge:
            case AugmentDatabase.AugmentType.RagingFire:
            case AugmentDatabase.AugmentType.BerserkerMode:
            case AugmentDatabase.AugmentType.BonusCritDamage:
            case AugmentDatabase.AugmentType.HeavyImpact:
            case AugmentDatabase.AugmentType.RelentlessAssault:
            case AugmentDatabase.AugmentType.GiantSlayer:
            case AugmentDatabase.AugmentType.BattleFrenzy:
                return "ATK";

            case AugmentDatabase.AugmentType.BonusSpellVamp:
            case AugmentDatabase.AugmentType.EchoStrike:
            case AugmentDatabase.AugmentType.ArcaneOverdrive:
            case AugmentDatabase.AugmentType.CelestialCascade:
            case AugmentDatabase.AugmentType.StaticOverload:
            case AugmentDatabase.AugmentType.EchoingVoid:
                return "AP";

            case AugmentDatabase.AugmentType.BonusCritChance:
            case AugmentDatabase.AugmentType.ShadowAmbush:
            case AugmentDatabase.AugmentType.ChainReaction:
                return "CRIT";

            case AugmentDatabase.AugmentType.BonusLifesteal:
            case AugmentDatabase.AugmentType.VampireBlood:
            case AugmentDatabase.AugmentType.BloodGodTribute:
            case AugmentDatabase.AugmentType.DeathsTouch:
            case AugmentDatabase.AugmentType.LifeTransfusion:
                return "VAMP";

            case AugmentDatabase.AugmentType.PoisonTouch:
                return "TOXIN";

            case AugmentDatabase.AugmentType.BonusHealth:
            case AugmentDatabase.AugmentType.RegenerativeCore:
            case AugmentDatabase.AugmentType.SoulHarvest:
            case AugmentDatabase.AugmentType.AbundantVitality:
            case AugmentDatabase.AugmentType.SecondWind:
                return "HP";

            case AugmentDatabase.AugmentType.BonusSpeed:
            case AugmentDatabase.AugmentType.SwiftShadow:
            case AugmentDatabase.AugmentType.FleetWind:
            case AugmentDatabase.AugmentType.StormDancer:
                return "SPD";

            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.AncientShield:
            case AugmentDatabase.AugmentType.IronBastion:
            case AugmentDatabase.AugmentType.DivineAegis:
            case AugmentDatabase.AugmentType.ArcaneShield:
            case AugmentDatabase.AugmentType.UnwaveringWill:
            case AugmentDatabase.AugmentType.PrismaticTitan:
                return "DEF";

            case AugmentDatabase.AugmentType.BountyHunter:
                return "SHARD";

            case AugmentDatabase.AugmentType.BonusCooldownReduction:
            case AugmentDatabase.AugmentType.UltimateCharge:
            case AugmentDatabase.AugmentType.ComboMastery:
            case AugmentDatabase.AugmentType.SurgingPulse:
            case AugmentDatabase.AugmentType.BonusMaxEnergy:
                return "CDR";

            case AugmentDatabase.AugmentType.DemonicPact:
            case AugmentDatabase.AugmentType.UndyingSovereign:
            case AugmentDatabase.AugmentType.GodforgedArsenal:
            case AugmentDatabase.AugmentType.AscendedDivinity:
                return "MYTH";

            // ─── Lõi Nguyên Tố & Tướng ───
            case AugmentDatabase.AugmentType.FlameAdaptation:
            case AugmentDatabase.AugmentType.MagmaEruption:
                return "FIRE";
            case AugmentDatabase.AugmentType.AcidMembrane:
            case AugmentDatabase.AugmentType.VirulentBloom:
                return "ACID";
            case AugmentDatabase.AugmentType.GlacialSoul:
            case AugmentDatabase.AugmentType.AbsoluteZeroNova:
                return "FROST";
            case AugmentDatabase.AugmentType.VoidTranscendence:
            case AugmentDatabase.AugmentType.ShadowMirroring:
                return "VOID";
            case AugmentDatabase.AugmentType.TripleMirageTornado:
            case AugmentDatabase.AugmentType.MeteorSupercluster:
            case AugmentDatabase.AugmentType.TripleRuinGale:
            case AugmentDatabase.AugmentType.EightWayTempest:
            case AugmentDatabase.AugmentType.TwinFrostNova:
            case AugmentDatabase.AugmentType.CataclysmicQuake:
                return "HERO";

            default:
                return "MYTH";
        }
    }
}
