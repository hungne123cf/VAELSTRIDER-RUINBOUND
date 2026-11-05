using UnityEngine;

/// <summary>
/// Hiệu ứng ĐẦM LẦY THỜI GIAN (Chrono Stasis Effect) áp dụng cho Quái vật & Boss
/// khi người chơi sở hữu Lõi Nâng Cấp Hai Mặt "Đầm Lầy Thời Gian":
/// - Có thời hạn hiệu lực là 60 Giây.
/// - Quái vật và Boss bị đánh chậm lại và di chuyển chậm lại 35%.
/// - Toàn bộ đạn quái bắn ra bay chậm lại 35%.
/// - Khi hết 60s sẽ tự động phục hồi toàn bộ vận tốc, cooldown và xóa VFX.
/// </summary>
public class EnemyChronoSlowEffect : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    public const float SLOW_FACTOR = 0.65f; // Chậm 35%
    public const float COOLDOWN_FACTOR = 1.538f; // Hồi chiêu lâu hơn 53.8% (tương đương giảm 35% tần suất bắn/đánh)
    private GameObject chronoVfx;
    private bool hasSlowedAnim = false;
    private bool hasSlowedCooldowns = false;

    public static bool IsChronoSlowActive
    {
        get
        {
            return AugmentManager.Instance != null &&
                   AugmentManager.Instance.HasAugment(AugmentDatabase.AugmentType.ChronoStasis);
        }
    }

    public static void ApplyToAllEnemies()
    {
        if (!IsChronoSlowActive) return;

        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is IEnemyHealth && mb != null && mb.gameObject != null)
            {
                Apply(mb.gameObject);
            }
        }
    }

    public static void RemoveFromAllEnemies()
    {
        var all = Object.FindObjectsByType<EnemyChronoSlowEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var effect in all)
        {
            if (effect != null)
            {
                effect.RestoreAndDestroy();
            }
        }
    }

    public static void Apply(GameObject enemy)
    {
        if (!IsChronoSlowActive) return;
        if (enemy == null) return;
        if (enemy.CompareTag("Player") || enemy.GetComponent<PlayerHealth>() != null) return;
        if (enemy.GetComponent<BreakableProp>() != null || enemy.GetComponent<TreasureChest>() != null) return;

        if (enemy.GetComponent<EnemyChronoSlowEffect>() != null) return;
        enemy.AddComponent<EnemyChronoSlowEffect>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (anim != null && !hasSlowedAnim)
        {
            anim.speed *= SLOW_FACTOR;
            hasSlowedAnim = true;
        }
        ApplyCooldownSlow();
        EnsureVfx();
    }

    private void ApplyCooldownSlow()
    {
        if (hasSlowedCooldowns) return;
        hasSlowedCooldowns = true;

        // Giãn hồi chiêu của các quái bắn xa và quái cận chiến
        var spitter = GetComponent<ValleySpitterAI>();
        if (spitter != null) spitter.attackCooldown *= COOLDOWN_FACTOR;

        var botWheel = GetComponent<BotWheelAI>();
        if (botWheel != null) botWheel.attackCooldown *= COOLDOWN_FACTOR;

        var bloodMonster = GetComponent<BloodMonsterAI>();
        if (bloodMonster != null) bloodMonster.attackCooldown *= COOLDOWN_FACTOR;

        var skeletonKing = GetComponent<SkeletonKingBossAI>();
        if (skeletonKing != null)
        {
            skeletonKing.meleeCooldown *= COOLDOWN_FACTOR;
            skeletonKing.skillCooldown *= COOLDOWN_FACTOR;
            skeletonKing.webSpitCooldown *= COOLDOWN_FACTOR;
        }

        var golem = GetComponent<ValleyGolemBossAI>();
        if (golem != null) golem.skillCooldown *= COOLDOWN_FACTOR;

        var doppel = GetComponent<ShadowDoppelgangerBoss>();
        if (doppel != null)
        {
            doppel.attackCooldown *= COOLDOWN_FACTOR;
            doppel.skillCooldown *= COOLDOWN_FACTOR;
            doppel.dashCooldown *= COOLDOWN_FACTOR;
        }
    }

    public void RestoreAndDestroy()
    {
        // 1. Phục hồi tốc độ hoạt ảnh
        if (anim != null && hasSlowedAnim)
        {
            anim.speed /= SLOW_FACTOR;
            hasSlowedAnim = false;
        }

        // 2. Phục hồi cooldown tấn công
        if (hasSlowedCooldowns)
        {
            hasSlowedCooldowns = false;

            var spitter = GetComponent<ValleySpitterAI>();
            if (spitter != null) spitter.attackCooldown /= COOLDOWN_FACTOR;

            var botWheel = GetComponent<BotWheelAI>();
            if (botWheel != null) botWheel.attackCooldown /= COOLDOWN_FACTOR;

            var bloodMonster = GetComponent<BloodMonsterAI>();
            if (bloodMonster != null) bloodMonster.attackCooldown /= COOLDOWN_FACTOR;

            var skeletonKing = GetComponent<SkeletonKingBossAI>();
            if (skeletonKing != null)
            {
                skeletonKing.meleeCooldown /= COOLDOWN_FACTOR;
                skeletonKing.skillCooldown /= COOLDOWN_FACTOR;
                skeletonKing.webSpitCooldown /= COOLDOWN_FACTOR;
            }

            var golem = GetComponent<ValleyGolemBossAI>();
            if (golem != null) golem.skillCooldown /= COOLDOWN_FACTOR;

            var doppel = GetComponent<ShadowDoppelgangerBoss>();
            if (doppel != null)
            {
                doppel.attackCooldown /= COOLDOWN_FACTOR;
                doppel.skillCooldown /= COOLDOWN_FACTOR;
                doppel.dashCooldown /= COOLDOWN_FACTOR;
            }
        }

        // 3. Hủy hiệu ứng VFX hào quang ngưng đọng thời gian
        if (chronoVfx != null)
        {
            Destroy(chronoVfx);
            chronoVfx = null;
        }

        Destroy(this);
    }

    void FixedUpdate()
    {
        // Nếu Lõi Đầm Lầy Thời Không đã hết hạn 60s: tự động phục hồi và hủy hiệu ứng
        if (!IsChronoSlowActive)
        {
            RestoreAndDestroy();
            return;
        }

        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity *= SLOW_FACTOR;
        }
    }

    private void EnsureVfx()
    {
        if (chronoVfx == null)
        {
            chronoVfx = new GameObject("ChronoMireVFX");
            chronoVfx.transform.SetParent(transform, false);
            chronoVfx.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var sr = chronoVfx.AddComponent<SpriteRenderer>();
            sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
            sr.color = new Color(0.35f, 0.95f, 0.85f, 0.35f); // Hào quang ngưng đọng thời gian ngọc bích
            sr.sortingOrder = 5050;
            chronoVfx.transform.localScale = Vector3.one * 1.35f;
        }
    }
}
