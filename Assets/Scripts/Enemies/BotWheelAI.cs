using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BotWheelAI : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private int maxHealth = 60;
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("AI Ranges")]
    [SerializeField] private float detectionRange = 6.5f;
    [SerializeField] private float attackRange = 1.4f;

    private int currentHealth;
    private float attackCooldownTimer = 0f;
    private bool isDead = false;
    private bool isKnockedBack = false;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animator;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // Freeze Z rotation de quai khong bi xoay tron
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
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
            return;
        }

        if (isKnockedBack) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 1. Ngoai tam nhin -> Dung yen
        if (distanceToPlayer > detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 2. Trong tam danh -> Tan cong
        if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (attackCooldownTimer <= 0)
            {
                AttackPlayer();
            }
            return;
        }

        // 3. Trong tam phat hien -> Duoi theo nguoi choi
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        // Tu dong lat mat theo huong chay (trai / phai)
        if (spriteRenderer != null)
        {
            if (dir.x > 0.1f) spriteRenderer.flipX = false;
            else if (dir.x < -0.1f) spriteRenderer.flipX = true;
        }
    }

    private void AttackPlayer()
    {
        attackCooldownTimer = attackCooldown;
        Debug.Log($"<color=red>[BOT WHEEL ATTACK]</color> Quai vat tan cong nguoi choi!");

        if (player != null)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                DamagePopup.Create(player.position, attackDamage, isPlayer: true);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"<color=yellow>[BOT WHEEL HIT]</color> Quai nhan {damage} DMG! Mau con: {currentHealth}/{maxHealth}");

        // Rung man hinh camera
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.2f);

        // Bi giat lui (Knockback) ve phia sau
        if (player != null)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir));
        }

        // Chop chop do
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
        rb.linearVelocity = dir * 7f;
        yield return new WaitForSeconds(0.15f);
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

        // Tat va cham de khong con can tro Player
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        // Vong vang: Tam phat hien
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vong do: Tam tan cong
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
