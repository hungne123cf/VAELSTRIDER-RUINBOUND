using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hệ thống tự động cân bằng chỉ số Quái vật & Trùm theo từng Bản đồ:
/// - Map 1 (Thestart / SampleScene): x2 HP & x2 Damage
/// - Map 2 (ValleyRuin): x4 HP & x4 Damage
/// - Map 3 (ForgottenMemories): x6 HP & x6 Damage
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
        if (string.IsNullOrEmpty(sceneName)) return 2.0f;

        // Map 3: Vùng Ký Ức Lãng Quên -> x6 HP & x6 Damage
        if (sceneName.Contains("Map3") || sceneName.Contains("Forgotten"))
        {
            return 6.0f;
        }

        // Map 2: Thung Lũng Tàn Tích -> x4 HP & x4 Damage
        if (sceneName.Contains("Map2") || sceneName.Contains("Valley"))
        {
            return 4.0f;
        }

        // Map 1: Khởi Đầu / SampleScene / Mặc định -> x2 HP & x2 Damage
        return 2.0f;
    }

    public static int ScaleHealth(int baseHealth)
    {
        return Mathf.RoundToInt(baseHealth * GetCurrentMultiplier());
    }

    public static int ScaleDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * GetCurrentMultiplier());
    }

    public static float ScaleDamage(float baseDamage)
    {
        return baseDamage * GetCurrentMultiplier();
    }
}
