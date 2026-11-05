using UnityEngine;
using System.IO;

/// <summary>
/// Quản lý và cung cấp toàn bộ biểu tượng đồ họa (Sprite) riêng biệt cho từng Lõi Nâng Cấp (Augments) & Cổ Vật.
/// - Mỗi Lõi Nâng Cấp sở hữu một biểu tượng độc nhất đặc trưng chuẩn Dark Fantasy 128x128
/// - Nạp đa tầng siêu bền bỉ: Resources -> Texture2D -> Direct Disk Load -> AssetDatabase -> Procedural Colored Emblem
/// - Tuyệt đối không bao giờ bị biến thành icon hình ngôi sao (Star)!
/// </summary>
public static class AugmentIconHelper
{
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> _cache
        = new System.Collections.Generic.Dictionary<string, Sprite>();

    public static string GetIconFileName(AugmentDatabase.AugmentType type)
    {
        switch (type)
        {
            // ─── Tấn công & Sát thương ───
            case AugmentDatabase.AugmentType.BonusAttack:
                return "sword_slash.png";
            case AugmentDatabase.AugmentType.LaceratingEdge:
                return "lacerating_edge.png";
            case AugmentDatabase.AugmentType.HeavyImpact:
                return "heavy_impact.png";
            case AugmentDatabase.AugmentType.RelentlessAssault:
                return "relentless_assault.png";
            case AugmentDatabase.AugmentType.GiantSlayer:
                return "giant_slayer.png";
            case AugmentDatabase.AugmentType.ComboMastery:
                return "combo_mastery.png";
            case AugmentDatabase.AugmentType.ShadowAmbush:
                return "shadow_ambush.png";
            case AugmentDatabase.AugmentType.BattleFrenzy:
                return "battle_frenzy.png";
            case AugmentDatabase.AugmentType.RagingFire:
                return "flame_fire.png";
            case AugmentDatabase.AugmentType.BerserkerMode:
                return "skull_death.png";

            // ─── Sinh mệnh & Hồi phục ───
            case AugmentDatabase.AugmentType.BonusHealth:
                return "heart_vitality.png";
            case AugmentDatabase.AugmentType.AbundantVitality:
                return "abundant_vitality.png";
            case AugmentDatabase.AugmentType.SecondWind:
                return "second_wind.png";
            case AugmentDatabase.AugmentType.RegenerativeCore:
                return "potion_regen.png";
            case AugmentDatabase.AugmentType.SoulHarvest:
                return "scythe_harvest.png";

            // ─── Tốc độ & Thao tác ───
            case AugmentDatabase.AugmentType.BonusSpeed:
                return "wings_fleet.png";
            case AugmentDatabase.AugmentType.FleetWind:
                return "fleet_wind.png";
            case AugmentDatabase.AugmentType.SwiftShadow:
                return "swift_shadow.png";
            case AugmentDatabase.AugmentType.StormDancer:
                return "cyclone_storm.png";

            // ─── Chí mạng & Nhắm bắn ───
            case AugmentDatabase.AugmentType.BonusCritChance:
                return "eye_focus.png";
            case AugmentDatabase.AugmentType.BonusCritDamage:
                return "crit_damage.png";
            case AugmentDatabase.AugmentType.ChainReaction:
                return "chain_reaction.png";

            // ─── Phòng ngự & Giáp hộ thân ───
            case AugmentDatabase.AugmentType.BonusDefense:
                return "shield_aegis.png";
            case AugmentDatabase.AugmentType.IronBastion:
                return "iron_bastion.png";
            case AugmentDatabase.AugmentType.AncientShield:
                return "ancient_shield.png";
            case AugmentDatabase.AugmentType.ArcaneShield:
                return "arcane_shield.png";
            case AugmentDatabase.AugmentType.UnwaveringWill:
                return "unwavering_will.png";
            case AugmentDatabase.AugmentType.PrismaticTitan:
                return "prismatic_titan.png";
            case AugmentDatabase.AugmentType.DivineAegis:
                return "divine_shield.png";

            // ─── Hút máu & Ma thuật huyết sắc ───
            case AugmentDatabase.AugmentType.BonusLifesteal:
                return "blood_drop.png";
            case AugmentDatabase.AugmentType.VampireBlood:
                return "vampire_blood.png";
            case AugmentDatabase.AugmentType.BloodGodTribute:
                return "blood_god_tribute.png";
            case AugmentDatabase.AugmentType.LifeTransfusion:
                return "life_transfusion.png";
            case AugmentDatabase.AugmentType.DeathsTouch:
                return "deaths_touch.png";
            case AugmentDatabase.AugmentType.PoisonTouch:
                return "poison_flask.png";

            // ─── Năng lượng & Tuyệt kỹ & Hồi chiêu ───
            case AugmentDatabase.AugmentType.BonusCooldownReduction:
                return "hourglass_cdr.png";
            case AugmentDatabase.AugmentType.BonusMaxEnergy:
                return "energy_pulse.png";
            case AugmentDatabase.AugmentType.SurgingPulse:
                return "surging_pulse.png";
            case AugmentDatabase.AugmentType.UltimateCharge:
                return "ultimate_charge.png";
            case AugmentDatabase.AugmentType.BountyHunter:
                return "bounty_hunter.png";

            // ─── Sấm sét & Ma thuật huyền bí ───
            case AugmentDatabase.AugmentType.BonusSpellVamp:
                return "spell_vamp.png";
            case AugmentDatabase.AugmentType.EchoStrike:
                return "lightning_bolt.png";
            case AugmentDatabase.AugmentType.StaticOverload:
                return "static_overload.png";
            case AugmentDatabase.AugmentType.ArcaneOverdrive:
                return "arcane_overdrive.png";
            case AugmentDatabase.AugmentType.CelestialCascade:
                return "meteor_cascade.png";
            case AugmentDatabase.AugmentType.EchoingVoid:
                return "echoing_void.png";

            // ─── Thần khí tối thượng & Hoàng quyền ───
            case AugmentDatabase.AugmentType.DemonicPact:
                return "demon_pact.png";
            case AugmentDatabase.AugmentType.UndyingSovereign:
                return "crown_sovereign.png";
            case AugmentDatabase.AugmentType.GodforgedArsenal:
                return "godforged_arsenal.png";
            case AugmentDatabase.AugmentType.AscendedDivinity:
                return "ascended_divinity.png";

            // ─── Lõi Nguyên Tố (Elemental Realm) ───
            case AugmentDatabase.AugmentType.FlameAdaptation:
                return "flame_adaptation.png";
            case AugmentDatabase.AugmentType.MagmaEruption:
                return "magma_eruption.png";
            case AugmentDatabase.AugmentType.AcidMembrane:
                return "acid_membrane.png";
            case AugmentDatabase.AugmentType.VirulentBloom:
                return "virulent_bloom.png";
            case AugmentDatabase.AugmentType.GlacialSoul:
                return "glacial_soul.png";
            case AugmentDatabase.AugmentType.AbsoluteZeroNova:
                return "absolute_zero_nova.png";
            case AugmentDatabase.AugmentType.VoidTranscendence:
                return "void_transcendence.png";
            case AugmentDatabase.AugmentType.ShadowMirroring:
                return "shadow_mirroring.png";

            // ─── 8 Lõi Nâng Cấp Mới & Cursed Double-Edged ───
            case AugmentDatabase.AugmentType.PercentCurrentHpExecute:
                return "guillotine_execute.png";
            case AugmentDatabase.AugmentType.GoldenSanctuary:
                return "golden_sanctuary.png";
            case AugmentDatabase.AugmentType.CursedMight:
                return "cursed_might.png";
            case AugmentDatabase.AugmentType.IroncladPact:
                return "hex_ironclad.png";
            case AugmentDatabase.AugmentType.ChronoStasis:
                return "chrono_mire.png";
            case AugmentDatabase.AugmentType.BloodTitheBlade:
                return "blood_tithe.png";
            case AugmentDatabase.AugmentType.GlassCannonTitan:
                return "glass_cannon.png";
            case AugmentDatabase.AugmentType.PhantomRush:
                return "phantom_rush.png";

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

        string nameNoExt = Path.GetFileNameWithoutExtension(filename);
        Sprite spr = null;

        // 1. Resources.Load<Sprite> chuẩn Unity (Cho bản build APK Android và PC Standalone)
        spr = Resources.Load<Sprite>("UI/Augments/" + nameNoExt);

        // 2. Resources.Load<Texture2D> fallback nếu Sprite importer chưa được nạp
        if (spr == null)
        {
            Texture2D rTex = Resources.Load<Texture2D>("UI/Augments/" + nameNoExt);
            if (rTex != null)
            {
                spr = Sprite.Create(rTex, new Rect(0, 0, rTex.width, rTex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        // 3. Resources.LoadAll<Sprite>
        if (spr == null)
        {
            var all = Resources.LoadAll<Sprite>("UI/Augments/" + nameNoExt);
            if (all != null && all.Length > 0) spr = all[0];
        }

        // 4. Đọc file nhị phân trực tiếp từ đĩa (Cứu cánh 100% trong Editor hoặc file vừa sinh ra chưa reimport)
        if (spr == null)
        {
            string[] searchPaths = new string[]
            {
                Path.Combine(Application.dataPath, "Resources/UI/Augments", filename),
                Path.Combine(Application.dataPath, "Sprites/UI/Augments", filename)
            };

            for (int i = 0; i < searchPaths.Length; i++)
            {
                if (File.Exists(searchPaths[i]))
                {
                    try
                    {
                        byte[] fileBytes = File.ReadAllBytes(searchPaths[i]);
                        Texture2D diskTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                        if (diskTex.LoadImage(fileBytes))
                        {
                            diskTex.filterMode = FilterMode.Bilinear;
                            spr = Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f), 100f);
                            break;
                        }
                    }
                    catch { }
                }
            }
        }

#if UNITY_EDITOR
        // 5. Unity Editor AssetDatabase Fallback
        if (spr == null)
        {
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Augments/{filename}");
            if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Augments/{filename}");
        }
#endif

        // 6. Fallback đồ họa procedurally sinh huy hiệu ma pháp theo màu sát thương (Tuyệt đối không hiển thị hình ngôi sao!)
        if (spr == null)
        {
            spr = CreateProceduralEmblem(type);
        }

        if (spr != null)
        {
            _cache[filename] = spr;
        }

        return spr;
    }

    /// <summary>
    /// Tự động vẽ một phù hiệu Dark Fantasy 64x64 sắc nét với viền cổ ngữ mang màu sát thương chuẩn của Lõi
    /// </summary>
    private static Sprite CreateProceduralEmblem(AugmentDatabase.AugmentType type)
    {
        Color typeCol = LevelUpChoiceUI.GetDamageTypeColor(type);
        int sz = 64;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        Color[] px = new Color[sz * sz];

        float center = sz * 0.5f;
        float rOuter = sz * 0.46f;
        float rInner = sz * 0.38f;

        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > rOuter)
                {
                    px[y * sz + x] = Color.clear;
                }
                else if (d >= rInner)
                {
                    px[y * sz + x] = typeCol;
                }
                else
                {
                    // Nền đá hắc diện thạch
                    px[y * sz + x] = new Color(0.10f, 0.12f, 0.16f, 0.95f);
                }
            }
        }

        // Vẽ chữ thập cổ ngữ hoặc khối thoi ở trung tâm
        for (int i = -8; i <= 8; i++)
        {
            int cx = Mathf.RoundToInt(center);
            int cy = Mathf.RoundToInt(center);
            if (cx + i >= 0 && cx + i < sz && cy >= 0 && cy < sz)
                px[cy * sz + (cx + i)] = Color.white;
            if (cx >= 0 && cx < sz && cy + i >= 0 && cy + i < sz)
                px[(cy + i) * sz + cx] = Color.white;
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
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

            // ─── Lõi Nâng Cấp Mới & Cursed Double-Edged ───
            case AugmentDatabase.AugmentType.PercentCurrentHpExecute:
                return "EXEC";
            case AugmentDatabase.AugmentType.GoldenSanctuary:
                return "IMMUNE";
            case AugmentDatabase.AugmentType.CursedMight:
                return "CURSE";
            case AugmentDatabase.AugmentType.IroncladPact:
                return "HEX";
            case AugmentDatabase.AugmentType.ChronoStasis:
                return "CHRONO";
            case AugmentDatabase.AugmentType.BloodTitheBlade:
                return "BLOOD";
            case AugmentDatabase.AugmentType.GlassCannonTitan:
                return "GLASS";
            case AugmentDatabase.AugmentType.PhantomRush:
                return "RUSH";

            default:
                return "MYTH";
        }
    }
}
