using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 7.5f;
    [SerializeField] private int damage = 15;
    [SerializeField] private float lifeTime = 3.5f;

    private Vector2 direction;
    private Rigidbody2D rb;
    private bool hasHit = false;

    public void Initialize(Vector2 dir, int dmg, float spd = 7.5f)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * speed;
        }

        // Rotate sprite towards flight direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Immediate point-blank check on spawn
        CheckProximityHit();

        Destroy(gameObject, lifeTime);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = 0.35f; // Generous reliable hitbox
        }
    }

    void Update()
    {
        if (hasHit) return;

        if (rb != null && rb.linearVelocity == Vector2.zero && direction != Vector2.zero)
        {
            rb.linearVelocity = direction * speed;
        }

        // Continuous proximity overlap check to prevent skipping fast projectiles or close targets
        CheckProximityHit();
    }

    private void CheckProximityHit()
    {
        if (hasHit) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.45f);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null) continue;

            var ph = hit.GetComponentInParent<PlayerHealth>() ?? hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInChildren<PlayerHealth>() ?? hit.GetComponentInParent<PlayerController>()?.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                OnHitPlayer(ph, hit.transform.position);
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other == null) return;

        // 1. Hit Player FIRST (regardless of isTrigger)
        var playerHealth = other.GetComponentInParent<PlayerHealth>() ?? other.GetComponent<PlayerHealth>() ?? other.GetComponentInChildren<PlayerHealth>() ?? other.GetComponentInParent<PlayerController>()?.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            OnHitPlayer(playerHealth, other.transform.position);
            return;
        }

        // 2. Ignore all enemies, bosses, or non-obstacle triggers
        if (other.CompareTag("Enemy") ||
            other.GetComponentInParent<BotWheelAI>() != null ||
            other.GetComponentInParent<BloodMonsterAI>() != null ||
            other.GetComponentInParent<ValleySpitterAI>() != null ||
            other.isTrigger)
        {
            return;
        }

        // 3. Hit Walls / Obstacles
        if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null ||
            other.GetComponent<CompositeCollider2D>() != null ||
            other.gameObject.name.ToLower().Contains("wall"))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }

    private void OnHitPlayer(PlayerHealth playerHealth, Vector3 hitPos)
    {
        if (hasHit) return;
        hasHit = true;

        playerHealth.TakeDamage(damage);
        DamagePopup.Create(hitPos, damage, isPlayer: true);

        // Screen shake
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.25f);

        Destroy(gameObject);
    }
}
