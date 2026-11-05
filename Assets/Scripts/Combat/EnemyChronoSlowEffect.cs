using UnityEngine;

/// <summary>
/// Hiệu ứng ĐẦM LẦY THỜI GIAN (Chrono Stasis Effect) áp dụng cho Quái vật & Boss
/// khi người chơi sở hữu Lõi Nâng Cấp Hai Mặt "Đầm Lầy Thời Gian":
/// - Quái vật và Boss bị đánh chậm lại và di chuyển chậm lại 35%.
/// - Có hiệu ứng tinh vân thời gian ngưng đọng (Chrono Glow) bao quanh quái.
/// </summary>
public class EnemyChronoSlowEffect : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private const float SLOW_FACTOR = 0.65f; // Chậm 35%
    private GameObject chronoVfx;
    private bool hasSlowedAnim = false;

    public static void ApplyToAllEnemies()
    {
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is IEnemyHealth && mb != null && mb.gameObject != null)
            {
                Apply(mb.gameObject);
            }
        }
    }

    public static void Apply(GameObject enemy)
    {
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
        EnsureVfx();
    }

    void FixedUpdate()
    {
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
