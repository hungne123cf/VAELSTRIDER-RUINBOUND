using UnityEngine;

/// <summary>
/// Helper tĩnh tối ưu hóa truy xuất và tương tác với Máu Quái vật & Boss.
/// Hỗ trợ 100% các Lõi Nâng Cấp tương tác theo Máu của Địch:
/// - Lõi Diệt Tuyệt Huyết Khí (gây sát thương theo 50% HP mục tiêu)
/// - Lõi Huyết Thệ Cuồng Lực (quái & boss tăng 50% HP)
/// </summary>
public static class EnemyHealthHelper
{
    public static int GetCurrentHealth(GameObject enemy)
    {
        if (enemy == null) return 0;
        var eh = enemy.GetComponent<IEnemyHealth>() ?? enemy.GetComponentInParent<IEnemyHealth>();
        if (eh != null) return eh.CurrentHealth;

        // Fallback kiểm tra cụ thể
        var blood = enemy.GetComponent<BloodMonsterAI>() ?? enemy.GetComponentInParent<BloodMonsterAI>();
        if (blood != null) return blood.currentHealth;

        var skKing = enemy.GetComponent<SkeletonKingBossAI>() ?? enemy.GetComponentInParent<SkeletonKingBossAI>();
        if (skKing != null) return skKing.currentHealth;

        return 100; // Mặc định an toàn
    }

    public static int GetMaxHealth(GameObject enemy)
    {
        if (enemy == null) return 100;
        var eh = enemy.GetComponent<IEnemyHealth>() ?? enemy.GetComponentInParent<IEnemyHealth>();
        if (eh != null) return eh.MaxHealth;

        var blood = enemy.GetComponent<BloodMonsterAI>() ?? enemy.GetComponentInParent<BloodMonsterAI>();
        if (blood != null) return blood.maxHealth;

        var skKing = enemy.GetComponent<SkeletonKingBossAI>() ?? enemy.GetComponentInParent<SkeletonKingBossAI>();
        if (skKing != null) return skKing.phaseMaxHealth;

        return 100;
    }

    public static bool IsBossOrElite(GameObject enemy)
    {
        if (enemy == null) return false;
        var eh = enemy.GetComponent<IEnemyHealth>() ?? enemy.GetComponentInParent<IEnemyHealth>();
        if (eh != null) return eh.IsBoss;

        var blood = enemy.GetComponent<BloodMonsterAI>() ?? enemy.GetComponentInParent<BloodMonsterAI>();
        if (blood != null && blood.isBoss) return true;

        if (enemy.GetComponent<SkeletonKingBossAI>() != null || enemy.GetComponentInParent<SkeletonKingBossAI>() != null) return true;
        if (enemy.GetComponent<ValleyGolemBossAI>() != null || enemy.GetComponentInParent<ValleyGolemBossAI>() != null) return true;
        if (enemy.GetComponent<ShadowDoppelgangerBoss>() != null || enemy.GetComponentInParent<ShadowDoppelgangerBoss>() != null) return true;

        return false;
    }

    public static void MultiplyHealthOfAllActiveEnemies(float mult)
    {
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is IEnemyHealth eh && mb != null)
            {
                eh.MultiplyHealth(mult);
            }
        }
    }
}

