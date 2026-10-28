using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Calamity Realms & Corrupted Cycle Manager
/// Hệ thống 5 Cõi Thảm Họa lũy tiến chuẩn phong cách Shape of Dream & Dash Fantasy:
/// - Chu Kỳ 1: Cõi Tàn Tích (Normal)
/// - Chu Kỳ 2: Ác Mộng Hư Không (Void Nightmare - Quái nổ hư không, Boss tăng tốc đòn đánh)
/// - Chu Kỳ 3: Cõi Nham Thạch (Magma Calamity - Mưa thiên thạch & Vũng dung nham)
/// - Chu Kỳ 4: Mưa Axit Ăn Mòn (Acid Downpour - Axit rút máu, cơ chế Khát Máu chém quái để trung hòa)
/// - Chu Kỳ 5: Bão Tuyết Băng Giá (Frostfall Blizzard - Hạt tuyết làm chậm & 0.5s đóng băng khóa chiêu)
/// </summary>
public enum CalamityRealm
{
    Normal = 1,
    VoidNightmare = 2,
    MagmaCalamity = 3,
    AcidDownpour = 4,
    FrostfallBlizzard = 5
}

public static class CorruptedCycleManager
{
    private const string PREF_CORRUPTED_ACTIVE = "Vaelstrider_CorruptedCycle_Active";
    private const string PREF_CYCLE_NUMBER = "Vaelstrider_CorruptedCycle_Number";

    private static bool isInitialized = false;
    private static bool isCorruptedActive = false;
    private static int currentCycle = 1;

    public static CalamityRealm CurrentRealm
    {
        get
        {
            EnsureInitialized();
            if (!isCorruptedActive) return CalamityRealm.Normal;
            return (CalamityRealm)Mathf.Clamp(currentCycle, 1, 5);
        }
    }

    public static bool IsCorruptedActive
    {
        get
        {
            EnsureInitialized();
            return isCorruptedActive && currentCycle >= 2;
        }
    }

    public static int CurrentCycle
    {
        get
        {
            EnsureInitialized();
            return currentCycle;
        }
    }

    // Các hệ số khuếch đại chỉ số theo từng Cõi Thảm Họa
    public static float HealthMultiplier
    {
        get
        {
            switch (CurrentRealm)
            {
                case CalamityRealm.VoidNightmare: return 2.6f;
                case CalamityRealm.MagmaCalamity: return 3.6f;
                case CalamityRealm.AcidDownpour: return 4.8f;
                case CalamityRealm.FrostfallBlizzard: return 6.2f;
                default: return 1.0f;
            }
        }
    }

    public static float DamageMultiplier
    {
        get
        {
            switch (CurrentRealm)
            {
                case CalamityRealm.VoidNightmare: return 1.8f;
                case CalamityRealm.MagmaCalamity: return 2.4f;
                case CalamityRealm.AcidDownpour: return 3.0f;
                case CalamityRealm.FrostfallBlizzard: return 3.8f;
                default: return 1.0f;
            }
        }
    }

    public static float SpeedMultiplier
    {
        get
        {
            switch (CurrentRealm)
            {
                case CalamityRealm.VoidNightmare: return 1.20f;
                case CalamityRealm.MagmaCalamity: return 1.25f;
                case CalamityRealm.AcidDownpour: return 1.30f;
                case CalamityRealm.FrostfallBlizzard: return 1.35f;
                default: return 1.0f;
            }
        }
    }

    public static float ShardsDropMultiplier
    {
        get
        {
            switch (CurrentRealm)
            {
                case CalamityRealm.VoidNightmare: return 2.2f;
                case CalamityRealm.MagmaCalamity: return 3.2f;
                case CalamityRealm.AcidDownpour: return 4.5f;
                case CalamityRealm.FrostfallBlizzard: return 6.0f;
                default: return 1.0f;
            }
        }
    }

    private static void EnsureInitialized()
    {
        if (isInitialized) return;
        // Luôn mặc định là Chế độ Thường (Normal Mode - Chu kỳ 1).
        // Chỉ khi người chơi chủ động bước qua Cổng Tha Hóa trong trận mới kích hoạt Calamity Realm!
        isCorruptedActive = false;
        currentCycle = 1;
        try
        {
            PlayerPrefs.DeleteKey(PREF_CORRUPTED_ACTIVE);
            PlayerPrefs.DeleteKey(PREF_CYCLE_NUMBER);
        }
        catch { }
        isInitialized = true;
    }

    public static CalamityRealm GetNextRealm()
    {
        int next = (int)CurrentRealm + 1;
        if (next > 5) next = 5;
        return (CalamityRealm)next;
    }

    public static string GetRealmName(CalamityRealm realm)
    {
        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                return LocalizationManager.T("ÁC MỘNG HƯ KHÔNG", "VOID NIGHTMARE");
            case CalamityRealm.MagmaCalamity:
                return LocalizationManager.T("CÕI NHAM THẠCH", "MAGMA CALAMITY");
            case CalamityRealm.AcidDownpour:
                return LocalizationManager.T("MƯA AXIT ĂN MÒN", "ACID DOWNPOUR");
            case CalamityRealm.FrostfallBlizzard:
                return LocalizationManager.T("BÃO TUYẾT BĂNG GIÁ", "FROSTFALL BLIZZARD");
            default:
                return LocalizationManager.T("CÕI TÀN TÍCH", "RUINS OF OLD");
        }
    }

    public static Color GetRealmColor(CalamityRealm realm)
    {
        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                return new Color(0.85f, 0.35f, 1.0f); // Amethyst Purple
            case CalamityRealm.MagmaCalamity:
                return new Color(1.0f, 0.42f, 0.12f); // Molten Magma Orange
            case CalamityRealm.AcidDownpour:
                return new Color(0.25f, 0.95f, 0.35f); // Toxic Acid Emerald
            case CalamityRealm.FrostfallBlizzard:
                return new Color(0.35f, 0.85f, 1.0f); // Glacial Frost Cyan
            default:
                return new Color(1.0f, 0.85f, 0.35f); // Celestial Gold
        }
    }

    public static string GetRealmDescription(CalamityRealm realm)
    {
        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                return LocalizationManager.T(
                    "• Quái có hào quang Hư Không & phát nổ khi bị tiêu diệt\n• Boss hồi chiêu nhanh hơn và ra đòn dồn dập tàn khốc\n• Toàn bộ Cấp Độ, Trang Bị và Mảnh Linh Hồn được BẢO LƯU!",
                    "• Enemies wield Void Auras & detonate upon defeat\n• Bosses recover faster and unleash unrelenting combos\n• ALL Level, Equipment and Shards are KEPT!"
                );
            case CalamityRealm.MagmaCalamity:
                return LocalizationManager.T(
                    "• Thiên thạch nham thạch rơi ngẫu nhiên xuống đấu trường\n• Vũng dung nham thiêu đốt buộc bạn phải vừa đánh vừa né\n• Toàn bộ Cấp Độ, Trang Bị và Mảnh Linh Hồn được BẢO LƯU!",
                    "• Molten meteors rain down randomly upon the arena\n• Scorching magma pools force constant movement and dashing\n• ALL Level, Equipment and Shards are KEPT!"
                );
            case CalamityRealm.AcidDownpour:
                return LocalizationManager.T(
                    "• Mưa axit ăn mòn bào mòn sinh lực liên tục theo thời gian\n• Cơ chế Khát Máu: Đánh quái liên tục để kích hoạt Khiên Trung Hòa\n• Toàn bộ Cấp Độ, Trang Bị và Mảnh Linh Hồn được BẢO LƯU!",
                    "• Toxic acid rain steadily erodes your health over time\n• Bloodrush Rule: Attack enemies constantly to gain Neutralization Shields\n• ALL Level, Equipment and Shards are KEPT!"
                );
            case CalamityRealm.FrostfallBlizzard:
                return LocalizationManager.T(
                    "• Bông tuyết băng giá làm chậm tốc chạy và tốc đánh\n• Ngẫu nhiên đóng băng và khóa chiêu trong 0.5 giây\n• Toàn bộ Cấp Độ, Trang Bị và Mảnh Linh Hồn được BẢO LƯU!",
                    "• Drifting frostflakes slow down movement and attack speed\n• Chance to freeze and silence skills for 0.5s upon contact\n• ALL Level, Equipment and Shards are KEPT!"
                );
            default:
                return LocalizationManager.T(
                    "Hành trình nguyên bản của dũng sĩ Vaelstrider.",
                    "The original journey of the Vaelstrider warrior."
                );
        }
    }

    /// <summary>
    /// Kích hoạt tiến nhập vào Cõi Thảm Họa kế tiếp và bắt đầu từ Map 1
    /// </summary>
    public static void StartNextCalamityCycle()
    {
        EnsureInitialized();
        isCorruptedActive = true;
        currentCycle = Mathf.Clamp(currentCycle + 1, 2, 5);

        Debug.Log($"<color=#F59E0B>[CALAMITY REALM]</color> Tiến nhập Cõi Thảm Họa Chu Kỳ {currentCycle}: {GetRealmName(CurrentRealm)}!");

        MatchTimerUI.ContinueRunTimer();
        Time.timeScale = 1.0f;

        if (Application.CanStreamedLevelBeLoaded("Map1_Thestart"))
        {
            SceneManager.LoadScene("Map1_Thestart");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public static void StartCorruptedCycle() => StartNextCalamityCycle();

    public static void ResetToNormalCycle()
    {
        isInitialized = true;
        isCorruptedActive = false;
        currentCycle = 1;
        try
        {
            PlayerPrefs.DeleteKey(PREF_CORRUPTED_ACTIVE);
            PlayerPrefs.DeleteKey(PREF_CYCLE_NUMBER);
            PlayerPrefs.Save();
        }
        catch { }
        Debug.Log("<color=green>[CALAMITY]</color> Đã thiết lập lại về Chế độ Thường (Normal Mode - Chu kỳ 1).");
    }
}
