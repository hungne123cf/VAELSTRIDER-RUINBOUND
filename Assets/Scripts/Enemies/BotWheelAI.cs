using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BotWheelAI : MonoBehaviour, IDamageable
{
    public static event System.Action<BotWheelAI> OnEnemyDied;

    [SerializeField] private bool isBoss = false;

    [Header("Enemy Stats")]
    [SerializeField] private int maxHealth = 60;
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.8f;

    [Header("AI Ranges")]
    [SerializeField] private float detectionRange = 7.5f;
    [SerializeField] private float attackRange = 5.2f; // Tam ban dan tam xa

    [Header("Ranged Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 7.5f;
    [SerializeField] private Sprite runtimeBulletSprite;

    private int currentHealth;
    private float attackCooldownTimer = 0f;
    private bool isDead = false;
    private bool isKnockedBack = false;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animator;

    // Animator Parameter Hashes
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        rb.freezeRotation = true;
        rb.gravityScale = 0f;

        // Auto-load projectile sprite if not assigned
        if (runtimeBulletSprite == null)
        {
            runtimeBulletSprite = Resources.Load<Sprite>("Combat/enemy_projectile");
#if UNITY_EDITOR
            if (runtimeBulletSprite == null)
            {
                runtimeBulletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/enemy_projectile.png");
            }
#endif
        }
    }

    void Start()
    {
        FindPlayerTarget();
    }

    private void FindPlayerTarget()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (attackCooldownTimer > 0) attackCooldownTimer -= Time.deltaTime;

        if (player == null)
        {
            FindPlayerTarget();
            SetMoving(false);
            return;
        }

        if (isKnockedBack) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 1. Ngoai tam phat hien -> Dung yen
        if (distanceToPlayer > detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);
            return;
        }

        // 2. Trong tam ban -> Dung lai ngam ban dan!
        if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);

            // Luon quay nong sung ve phia Player
            Vector2 aimDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            if (spriteRenderer != null)
            {
                if (aimDir.x > 0.05f) spriteRenderer.flipX = false;
                else if (aimDir.x < -0.05f) spriteRenderer.flipX = true;
            }

            if (attackCooldownTimer <= 0)
            {
                AttackPlayer();
            }
            return;
        }

        // 3. Trong tam phat hien nhung chua toi tam ban -> Tiep can nguoi choi
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
        SetMoving(true);

        if (spriteRenderer != null)
        {
            if (dir.x > 0.05f) spriteRenderer.flipX = false;
            else if (dir.x < -0.05f) spriteRenderer.flipX = true;
        }
    }

    private void SetMoving(bool moving)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(IsMovingHash, moving);
        }
    }

    private void AttackPlayer()
    {
        attackCooldownTimer = attackCooldown;
        Debug.Log($"<color=red>[BOT WHEEL SHOOT]</color> Quai vat ban dan vao nguoi choi!");

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(AttackHash);
        }

        if (player != null)
        {
            Vector2 shootDir = ((Vector2)player.position + Vector2.up * 0.4f - (Vector2)transform.position).normalized;
            StartCoroutine(SpawnBulletRoutine(shootDir));
        }
    }

    private IEnumerator SpawnBulletRoutine(Vector2 dir)
    {
        // Cho dung thoi diem phat hoa cua nong sung trong animation
        yield return new WaitForSeconds(0.12f);

        if (isDead) yield break;

        // Tinh vi tri nong sung dua vao huong quay
        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        Vector3 muzzleOffset = new Vector3(facingLeft ? -0.45f : 0.45f, 0.35f, 0f);
        Vector3 spawnPos = transform.position + muzzleOffset;

        if (projectilePrefab != null)
        {
            GameObject bulletObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = bulletObj.GetComponent<EnemyProjectile>();
            if (proj != null) proj.Initialize(dir, attackDamage, projectileSpeed);
        }
        else
        {
            // Tu tao vien dan bay ngay lap tuc
            CreateRuntimeBullet(spawnPos, dir);
        }
    }

    private void CreateRuntimeBullet(Vector3 spawnPos, Vector2 dir)
    {
        GameObject bullet = new GameObject("Enemy_EnergyBullet");
        bullet.transform.position = spawnPos;
        if (isBoss) bullet.transform.localScale = Vector3.one * 1.5f;

        var sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeBulletSprite;
        sr.sortingOrder = 5500; // tren nen map

        var col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.22f;

        var rbBullet = bullet.AddComponent<Rigidbody2D>();
        rbBullet.gravityScale = 0f;
        rbBullet.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var proj = bullet.AddComponent<EnemyProjectile>();
        proj.Initialize(dir, attackDamage, projectileSpeed);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"<color=yellow>[BOT WHEEL HIT]</color> Quai nhan {damage} DMG! Mau con: {currentHealth}/{maxHealth}");

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.2f);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(HurtHash);
        }

        if (player != null)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir));
        }

        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRedRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 dir)
    {
        isKnockedBack = true;
        rb.linearVelocity = dir * 6f;
        yield return new WaitForSeconds(0.12f);
        isKnockedBack = false;
    }

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        Debug.Log($"<color=green>[BOT WHEEL DESTROYED]</color> Quai vat da bi tieu diet!");

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(DieHash);
        }

        OnEnemyDied?.Invoke(this);

        if (isBoss)
        {
            AethelShard.SpawnShards(transform.position, Random.Range(12, 18));
        }
        else
        {
            AethelShard.SpawnShards(transform.position, Random.Range(3, 6));
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 0.65f);
    }

    public void SetupBoss(float scaleMultiplier = 1.75f, int hpMultiplier = 3, int damageMultiplier = 2)
    {
        isBoss = true;
        transform.localScale = transform.localScale * scaleMultiplier;
        maxHealth *= hpMultiplier;
        currentHealth = maxHealth;
        attackDamage *= damageMultiplier;
        moveSpeed *= 0.85f;
        projectileSpeed *= 1.25f; // Dan Boss bay nhanh hon!
        attackRange = 6.5f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.4f);
            originalColor = spriteRenderer.color;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
