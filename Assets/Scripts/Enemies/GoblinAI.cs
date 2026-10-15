using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class GoblinAI : MonoBehaviour, IDamageable
{
    public static event System.Action<GoblinAI> OnEnemyDied;

    [Header("Goblin Stats")]
    [SerializeField] private int maxHealth = 145;
    [SerializeField] private int currentHealth;
    [SerializeField] private int attackDamage = 18;
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float detectionRange = 100f; // Luon phat hien va truy duoi
    [SerializeField] private float meleeRange = 1.35f;
    [SerializeField] private float attackCooldown = 1.1f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CapsuleCollider2D col;
    private Color originalColor;

    private float attackTimer = 0f;
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isKnockedBack = false;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        maxHealth = MapScalingManager.ScaleHealth(maxHealth);
        attackDamage = MapScalingManager.ScaleDamage(attackDamage);
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Gan layer Enemy (Layer 6) va Tag Enemy
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        gameObject.layer = (enemyLayer != -1) ? enemyLayer : 6;
        gameObject.tag = "Enemy";

        if (col != null)
        {
            col.size = new Vector2(0.8f, 1.3f);
            col.offset = new Vector2(0f, 0.65f);
            col.isTrigger = false;
        }

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (GetComponent<AutoDepthSort>() == null) gameObject.AddComponent<AutoDepthSort>();
    }

    void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) p = pc.gameObject;
        }
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (PlayerHealth.IsPlayerDead)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool(IsMovingHash, false);
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        if (isKnockedBack || isAttacking) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Lật mặt theo hướng Player
        Vector3 diff = player.position - transform.position;
        if (Mathf.Abs(diff.x) > 0.05f)
        {
            transform.localScale = new Vector3(diff.x > 0 ? 1f : -1f, 1f, 1f);
        }

        if (dist <= meleeRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool(IsMovingHash, false);

            if (attackTimer <= 0f)
            {
                StartCoroutine(MeleeLungeAttackRoutine());
            }
        }
        else
        {
            // Luon truy duoi Player
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
            if (animator != null) animator.SetBool(IsMovingHash, true);
        }
    }

    private void FixedUpdate()
    {
        if (isDead || isKnockedBack || isAttacking || player == null || PlayerHealth.IsPlayerDead) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > meleeRange)
        {
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private IEnumerator MeleeLungeAttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger(AttackHash);

        yield return new WaitForSeconds(0.18f);

        if (isDead || PlayerHealth.IsPlayerDead) { isAttacking = false; yield break; }

        // Lao nhanh mot doan ngan dam kiem
        Vector2 lungeDir = (player != null) ? (player.position - transform.position).normalized : Vector2.right;
        rb.linearVelocity = lungeDir * (moveSpeed * 1.5f);

        yield return new WaitForSeconds(0.15f);
        rb.linearVelocity = Vector2.zero;

        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeRange + 0.6f)
        {
            IDamageable target = player.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        StartCoroutine(DamageFlashRoutine());

        if (player != null && !isKnockedBack)
        {
            Vector2 kbDir = (transform.position - player.position).normalized;
            StartCoroutine(KnockbackRoutine(kbDir, 4.5f, 0.12f));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (animator != null) animator.SetTrigger(HurtHash);
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.4f, 0.9f);
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 dir, float force, float duration)
    {
        isKnockedBack = true;
        rb.linearVelocity = dir * force;
        yield return new WaitForSeconds(duration);
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;

        if (animator != null) animator.SetTrigger(DieHash);

        AethelShard.SpawnShards(transform.position, Random.Range(1, 3));
        WorldItemPickup.RollDrop(transform.position, false);

        OnEnemyDied?.Invoke(this);
        Destroy(gameObject, 0.65f);
    }
}
