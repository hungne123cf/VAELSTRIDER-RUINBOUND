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

        Destroy(gameObject, lifeTime);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;
    }

    void Update()
    {
        if (rb != null && rb.linearVelocity == Vector2.zero && direction != Vector2.zero)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Ignore enemies and triggers (e.g. shard pickup zones)
        if (other.GetComponent<BotWheelAI>() != null || other.isTrigger)
        {
            return;
        }

        // Hit Player
        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            hasHit = true;
            playerHealth.TakeDamage(damage);
            DamagePopup.Create(other.transform.position, damage, isPlayer: true);

            // Screen shake
            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.12f, 0.25f);

            Destroy(gameObject);
            return;
        }

        // Hit Walls / Obstacles
        if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null ||
            other.GetComponent<CompositeCollider2D>() != null ||
            other.gameObject.name.ToLower().Contains("wall"))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
