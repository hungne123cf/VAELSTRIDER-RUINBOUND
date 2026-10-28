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

        // Áp dụng Dress Code theo Nguyên Tố của Map hiện tại
        ApplyElementalDressCode();

        // Immediate point-blank check on spawn
        CheckProximityHit();

        Destroy(gameObject, lifeTime);
    }

    private float trailParticleTimer = 0f;

    private void ApplyElementalDressCode()
    {
        var sr = GetComponent<SpriteRenderer>();
        var realm = CorruptedCycleManager.CurrentRealm;

        Color mainColor;
        Color trailStartColor;
        Color trailEndColor;

        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                // Map Ác Mộng: Viên đạn màu đen sâu thẳm, tỏa bóng đêm
                mainColor = new Color(0.12f, 0.05f, 0.22f, 1f);
                trailStartColor = new Color(0.45f, 0.12f, 0.70f, 0.85f);
                trailEndColor = new Color(0.02f, 0.01f, 0.05f, 0.0f);
                break;

            case CalamityRealm.MagmaCalamity:
                // Map Dung Nham: Viên đạn màu cam đỏ, bắn ra có tia lửa phía sau
                mainColor = new Color(1.0f, 0.35f, 0.05f, 1f);
                trailStartColor = new Color(1.0f, 0.80f, 0.15f, 0.95f);
                trailEndColor = new Color(0.9f, 0.15f, 0.0f, 0.0f);
                break;

            case CalamityRealm.AcidDownpour:
                // Map Mưa Axit: Viên đạn màu xanh lá có tia nước xanh lá phía sau
                mainColor = new Color(0.25f, 1.0f, 0.35f, 1f);
                trailStartColor = new Color(0.35f, 1.0f, 0.45f, 0.90f);
                trailEndColor = new Color(0.1f, 0.6f, 0.2f, 0.0f);
                break;

            case CalamityRealm.FrostfallBlizzard:
                // Map Tuyết: Viên đạn tuyết / hoa tuyết màu xanh băng trắng
                mainColor = new Color(0.85f, 0.95f, 1.0f, 1f);
                trailStartColor = new Color(0.6f, 0.9f, 1.0f, 0.85f);
                trailEndColor = new Color(0.3f, 0.7f, 1.0f, 0.0f);
                break;

            default:
                mainColor = new Color(0.75f, 0.35f, 1.0f, 1f);
                trailStartColor = new Color(0.8f, 0.4f, 1.0f, 0.7f);
                trailEndColor = new Color(0.4f, 0.1f, 0.8f, 0.0f);
                break;
        }

        if (sr != null)
        {
            sr.color = mainColor;
        }

        // Tạo TrailRenderer vệt bay cho đạn
        var trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = trailStartColor;
        trail.endColor = trailEndColor;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.02f;
        trail.time = 0.24f;
        trail.sortingOrder = (sr != null) ? (sr.sortingOrder - 1) : 5400;
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
        if (Time.timeScale <= 0f) return;
        if (hasHit) return;

        // Sinh hạt hiệu ứng nguyên tố phía sau đạn (Khói bóng đêm, tia lửa, tia nước acid, bông tuyết)
        trailParticleTimer += Time.deltaTime;
        if (trailParticleTimer >= 0.065f)
        {
            trailParticleTimer = 0f;
            SpawnElementalTrailFX();
        }

        if (rb != null && rb.linearVelocity == Vector2.zero && direction != Vector2.zero)
        {
            rb.linearVelocity = direction * speed;
        }

        // Continuous proximity overlap check to prevent skipping fast projectiles or close targets
        CheckProximityHit();
    }

    private void SpawnElementalTrailFX()
    {
        var realm = CorruptedCycleManager.CurrentRealm;
        GameObject p = new GameObject("Proj_Trail_Particle");
        p.transform.position = transform.position + (Vector3)(-direction * 0.15f);

        var pSr = p.AddComponent<SpriteRenderer>();
        pSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        pSr.sortingOrder = 5350;

        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                // Bóng đêm đen tím tỏa ra mờ ảo
                pSr.color = new Color(0.12f, 0.04f, 0.20f, 0.65f);
                p.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);
                break;

            case CalamityRealm.MagmaCalamity:
                // Tia lửa than hồng bắn tóe
                pSr.color = new Color(1.0f, Random.Range(0.6f, 0.9f), 0.1f, 0.85f);
                p.transform.localScale = Vector3.one * Random.Range(0.15f, 0.30f);
                break;

            case CalamityRealm.AcidDownpour:
                // Giọt nước / bọt axit xanh lá bắn tóe
                pSr.color = new Color(0.3f, 1.0f, 0.4f, 0.75f);
                p.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
                break;

            case CalamityRealm.FrostfallBlizzard:
                // Hạt sương tuyết trắng xanh
                pSr.color = new Color(0.85f, 0.95f, 1.0f, 0.80f);
                p.transform.localScale = Vector3.one * Random.Range(0.20f, 0.35f);
                break;

            default:
                pSr.color = new Color(0.8f, 0.4f, 1.0f, 0.65f);
                p.transform.localScale = Vector3.one * Random.Range(0.20f, 0.30f);
                break;
        }

        Destroy(p, 0.20f);
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
