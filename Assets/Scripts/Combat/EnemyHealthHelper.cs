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

    /// <summary>
    /// Kích hoạt khi quái mới xuất hiện trong trận đấu:
    /// - Tự động nhân cấp số máu (HP) và độ khó theo số lượng người chơi Co-op (tối đa gấp 4 lần cho 4 người)
    /// - Áp dụng bùa hại CursedMight nếu người chơi đã chọn
    /// - Áp dụng hiệu ứng làm chậm Thời Không nếu đang kích hoạt
    /// </summary>
    public static void OnEnemySpawned(GameObject enemy)
    {
        if (enemy == null) return;
        DynamicSpriteColliderSync.EnsureAttached(enemy);

        // ĐỘ KHÓ CO-OP: Tăng lượng máu theo cấp số nhân theo số lượng người chơi trong trận (1 người = 1x, 2 người = 2x, 3 người = 3x, 4 người = 4x)
        int coopMult = PvPMatchManager.GetCurrentCoopPlayerMultiplier();
        if (coopMult > 1)
        {
            var eh = enemy.GetComponent<IEnemyHealth>() ?? enemy.GetComponentInParent<IEnemyHealth>();
            if (eh != null)
            {
                eh.MultiplyHealth((float)coopMult);
            }
        }

        if (AugmentManager.Instance != null && AugmentManager.Instance.HasAugment(AugmentDatabase.AugmentType.CursedMight))
        {
            var eh = enemy.GetComponent<IEnemyHealth>() ?? enemy.GetComponentInParent<IEnemyHealth>();
            if (eh != null)
            {
                eh.MultiplyHealth(1.50f);
            }
        }

        // Lõi Đầm Lầy Thời Không: Làm chậm quái và giãn cooldown ngay khi sinh ra
        if (EnemyChronoSlowEffect.IsChronoSlowActive)
        {
            EnemyChronoSlowEffect.Apply(enemy);
        }
    }
}

