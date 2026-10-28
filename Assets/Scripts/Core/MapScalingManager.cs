using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hệ thống tự động cân bằng chỉ số Quái vật & Trùm theo Sức mạnh Player & Từng Bản đồ:
/// - Player mạnh tới đâu thì Quái thường mạnh gấp ~2 lần (HP và Sát thương scale tương xứng, không bị 1-shot).
/// - Boss mạnh gấp ~5 lần Player cả về Sát thương lẫn Máu (trận đánh trùm kịch tính, căng thẳng chuẩn Soulslike).
/// - Tự động nhân thêm hệ số Calamity / Nightmare Realm khi bước vào Chu kỳ Tha Hóa.
/// </summary>
public static class MapScalingManager
{
    public static float GetCurrentMultiplier()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return GetMultiplierForScene(sceneName);
    }

    public static float GetMultiplierForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return 1.0f;

        // Map 3: Vùng Ký Ức Lãng Quên -> x1.75
        if (sceneName.Contains("Map3") || sceneName.Contains("Forgotten") || sceneName.Contains("Ancient"))
        {
            return 1.75f;
        }

        // Map 2: Thung Lũng Tàn Tích -> x1.35
        if (sceneName.Contains("Map2") || sceneName.Contains("Valley"))
        {
            return 1.35f;
        }

        // Map 1: Khởi Đầu / SampleScene / Mặc định -> x1.0
        return 1.0f;
    }

    // ─── THÔNG SỐ SỨC MẠNH PLAYER (PLAYER POWER STATS) ───────────────────────
    public static int GetPlayerMaxHealth()
    {
        var ph = Object.FindAnyObjectByType<PlayerHealth>();
        if (ph != null && ph.MaxHealth > 0) return ph.MaxHealth;
        return 100;
    }

    public static int GetPlayerAttackDamage()
    {
        var pc = Object.FindAnyObjectByType<PlayerCombat>();
        if (pc != null && pc.TotalAttackDamage > 0) return pc.TotalAttackDamage;
        var dune = Object.FindAnyObjectByType<DuneAssassinSkill>();
        if (dune != null) return dune.GetEffectiveAttackDamage();
        return 20;
    }

    /// <summary>
    /// Hệ số tăng trưởng sức mạnh tổng thể của Player so với khởi đầu Level 1 (Máu 100, Sát thương 20)
    /// </summary>
    public static float GetPlayerPowerGrowth()
    {
        float hpRatio = (float)GetPlayerMaxHealth() / 100f;
        float atkRatio = (float)GetPlayerAttackDamage() / 20f;
        return Mathf.Max(1.0f, hpRatio * 0.65f + atkRatio * 0.35f);
    }

    // ─── SCALE QUÁI THƯỜNG (GẤP ~2 LẦN SỨC MẠNH PLAYER) ──────────────────────
    public static int ScaleHealth(int baseHealth)
    {
        float mapMult = GetCurrentMultiplier() * CorruptedCycleManager.HealthMultiplier;
        float growth = GetPlayerPowerGrowth();

        // Quái thường mạnh gấp ~2 lần tốc độ tăng trưởng của player
        float scaled = baseHealth * mapMult * (1f + (growth - 1f) * 1.8f);

        // Đảm bảo lượng máu tối thiểu: chịu được các đòn combo đao kiếm của player (~35% Player Max HP)
        int pMaxHp = GetPlayerMaxHealth();
        float minHpByPlayer = pMaxHp * 0.35f * mapMult;

        return Mathf.Max(baseHealth, Mathf.RoundToInt(Mathf.Max(scaled, minHpByPlayer)));
    }

    public static int ScaleDamage(int baseDamage)
    {
        float mapMult = GetCurrentMultiplier() * CorruptedCycleManager.DamageMultiplier;
        float growth = GetPlayerPowerGrowth();

        float scaled = baseDamage * mapMult * (1f + (growth - 1f) * 1.4f);

        // Đảm bảo sát thương quái thường gây áp lực thực sự: tối thiểu ~5.5% Max HP của Player
        int pMaxHp = GetPlayerMaxHealth();
        float minDmgByPlayer = pMaxHp * 0.055f * mapMult;

        return Mathf.Max(baseDamage, Mathf.RoundToInt(Mathf.Max(scaled, minDmgByPlayer)));
    }

    public static float ScaleDamage(float baseDamage)
    {
        return (float)ScaleDamage(Mathf.RoundToInt(baseDamage));
    }

    // ─── SCALE TRÙM / BOSS (GẤP ~5 LẦN PLAYER CẢ VỀ SÁT THƯƠNG LẪN MÁU) ──────
    /// <summary>
    /// Cân bằng Máu Boss: Tổng lượng máu Boss qua các Phase gấp ~5 lần Max HP của Player!
    /// </summary>
    public static int ScaleBossHealth(int baseHealth, int phases = 1)
    {
        phases = Mathf.Max(1, phases);
        int pMaxHp = GetPlayerMaxHealth();
        float mapMult = GetCurrentMultiplier() * CorruptedCycleManager.HealthMultiplier;

        // Mục tiêu: Tổng lượng HP của Boss = 5.0x Player Max HP
        // Máu mỗi phase = (PlayerMaxHP * 5.0f) / phases
        float bossTargetHpTotal = pMaxHp * 5.0f * mapMult;
        float perPhaseTarget = bossTargetHpTotal / phases;

        float baseScaled = baseHealth * mapMult * (1f + (GetPlayerPowerGrowth() - 1f) * 2.2f);
        return Mathf.Max(baseHealth, Mathf.RoundToInt(Mathf.Max(baseScaled, perPhaseTarget)));
    }

    /// <summary>
    /// Cân bằng Sát thương Boss: Sát thương thường ~16% Max HP, Đòn đặc biệt/Slam ~30% Max HP của Player!
    /// </summary>
    public static int ScaleBossDamage(int baseDamage, bool isSpecialSkill = false)
    {
        int pMaxHp = GetPlayerMaxHealth();
        float mapMult = GetCurrentMultiplier() * CorruptedCycleManager.DamageMultiplier;

        // Sát thương chiêu đặc biệt (Slam, Địa chấn, Hư không): ~30% Player HP. Đòn thường: ~16% Player HP.
        float targetDmg = isSpecialSkill ? (pMaxHp * 0.30f * mapMult) : (pMaxHp * 0.16f * mapMult);

        float growth = GetPlayerPowerGrowth();
        float baseScaled = baseDamage * mapMult * (1f + (growth - 1f) * 1.8f);

        return Mathf.Max(baseDamage, Mathf.RoundToInt(Mathf.Max(baseScaled, targetDmg)));
    }

    public static float ScaleBossDamage(float baseDamage, bool isSpecialSkill = false)
    {
        return (float)ScaleBossDamage(Mathf.RoundToInt(baseDamage), isSpecialSkill);
    }
}
