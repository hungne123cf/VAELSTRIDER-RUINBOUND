using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BotWheelAI : MonoBehaviour, IDamageable
{
    public static event System.Action<BotWheelAI> OnEnemyDied;

    [SerializeField] public bool isBoss = false;
    public bool IsBoss => isBoss;

    [Header("Enemy Stats")]
    [SerializeField] private int maxHealth = 130; // Quai thuong mau trau hon (130 HP)
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.8f;

    [Header("Boss 3 Health Bars")]
    public int totalBossPhases = 3;
    private int currentBossPhase = 3;
    public int CurrentBossPhase => currentBossPhase;
    private int phaseMaxHealth = 450;
    private bool isTransitioningPhase = false;

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
    private Collider2D enemyCol;

    // Animator Parameter Hashes
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        enemyCol = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (runtimeBulletSprite == null)
        {
            Texture2D tex = new Texture2D(16, 16);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    tex.SetPixel(x, y, dist < 7f ? new Color(1f, 0.35f, 0.1f, 1f) : Color.clear);
                }
            }
            tex.Apply();
            runtimeBulletSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }
    }

    void Start()
    {
        FindPlayerTarget();
    }

    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (player == null)
        {
            FindPlayerTarget();
            SetMoving(false);
            return;
        }

        if (isKnockedBack || isTransitioningPhase) return;

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
                if (aimDir.x > 0.05f) SetFacing(false);
                else if (aimDir.x < -0.05f) SetFacing(true);
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
            if (dir.x > 0.05f) SetFacing(false);
            else if (dir.x < -0.05f) SetFacing(true);
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

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(AttackHash);
        }

        if (player != null)
        {
            Vector2 shootDir = ((Vector2)player.position + Vector2.up * 0.4f - (Vector2)transform.position).normalized;
            StartCoroutine(SpawnBulletRoutine(shootDir));
            SoundManager.EnsureInstance()?.PlayEnemyShoot();
        }
    }

    private IEnumerator SpawnBulletRoutine(Vector2 dir)
    {
        yield return new WaitForSeconds(0.12f);
        if (isDead) yield break;

        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        float botCenterX = facingLeft ? 1.14f : -1.14f;
        float gunOffset = facingLeft ? -0.45f : 0.45f;
        Vector3 muzzleOffset = new Vector3(botCenterX + gunOffset, 0.35f, 0f);
        Vector3 spawnPos = transform.position + muzzleOffset;

        if (projectilePrefab != null)
        {
            GameObject bulletObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = bulletObj.GetComponent<EnemyProjectile>();
            if (proj != null) proj.Initialize(dir, attackDamage, projectileSpeed);
        }
        else
        {
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
        sr.sortingOrder = 5500;

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
        if (isDead || isTransitioningPhase) return;

        currentHealth -= damage;

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.2f);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(HurtHash);
        }

        if (player != null && !isBoss)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir));
        }

        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRedRoutine());
        }

        if (isBoss)
        {
            BossHealthBarUI.Instance?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
        }

        if (currentHealth <= 0)
        {
            if (isBoss && currentBossPhase > 1)
            {
                StartCoroutine(BossPhaseTransitionRoutine());
            }
            else
            {
                if (isBoss)
                {
                    BossHealthBarUI.Instance?.HideBoss();
                }
                Die();
            }
        }
    }

    private IEnumerator BossPhaseTransitionRoutine()
    {
        isTransitioningPhase = true;
        currentBossPhase--;
        currentHealth = phaseMaxHealth;

        rb.linearVelocity = Vector2.zero;

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.55f);

        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        string phaseTitle = currentBossPhase == 2 
            ? "PHASE II: HƯ KHÔNG THỨC TỈNH!" 
            : "FINAL PHASE: CUỒNG NỘ HUYẾT NGUYỆT!";
        Color titleColor = currentBossPhase == 2 
            ? new Color(0.85f, 0.45f, 1f) 
            : new Color(1f, 0.2f, 0.2f);

        DamagePopup.CreateText(transform.position + Vector3.up * 1.8f, phaseTitle, titleColor);

        // Đổi màu hào quang Boss
        if (spriteRenderer != null)
        {
            originalColor = currentBossPhase == 2 
                ? new Color(0.9f, 0.4f, 1f) 
                : new Color(1f, 0.25f, 0.25f);
            spriteRenderer.color = originalColor;
        }

        // Tăng tốc và sức mạnh
        moveSpeed *= 1.25f;
        attackDamage += 8;
        attackCooldown *= 0.8f;

        // Đẩy lùi người chơi ra xa nhẹ
        if (player != null)
        {
            Vector2 pushDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            var playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null) playerRb.AddForce(pushDir * 8f, ForceMode2D.Impulse);
        }

        BossHealthBarUI.Instance?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);

        yield return new WaitForSeconds(1.2f);
        isTransitioningPhase = false;
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
        if (isBoss) SoundManager.EnsureInstance()?.PlayBossDeath();
        else SoundManager.EnsureInstance()?.PlayHitImpact();

        OnEnemyDied?.Invoke(this);

        if (isBoss)
        {
            AethelShard.SpawnShards(transform.position, Random.Range(18, 26));
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

        // Hệ thống 3 thanh máu: Mỗi thanh 450 HP (Tổng 1350 HP)
        totalBossPhases = 3;
        currentBossPhase = 3;
        phaseMaxHealth = 450;
        currentHealth = phaseMaxHealth;

        attackDamage *= damageMultiplier;
        moveSpeed *= 0.95f;
        projectileSpeed *= 1.25f;
        attackRange = 7.0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.4f);
            originalColor = spriteRenderer.color;
        }

        // Tự động gắn bộ 25 skill Gigapack
        if (GetComponent<BossGigapackSkills>() == null)
        {
            gameObject.AddComponent<BossGigapackSkills>();
        }

        // Tự động kích hoạt thanh máu Boss 3 lớp
        string scene = SceneManager.GetActiveScene().name;
        string bName = scene.Contains("Map2") || scene.Contains("Valley")
            ? "✦ VALENTHUS • HỘ VỆ THẦN THỤ ✦"
            : "✦ TITAN CRUSHER • CỖ MÁY TÀN TÍCH ✦";

        BossHealthBarUI.EnsureInstance()?.ShowBoss(this, bName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void SetFacing(bool faceLeft)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = faceLeft;
        }

        if (enemyCol == null) enemyCol = GetComponent<Collider2D>();
        if (enemyCol is CapsuleCollider2D cap)
        {
            float absX = Mathf.Abs(cap.offset.x);
            if (absX < 0.1f) absX = 1.14f;
            cap.offset = new Vector2(faceLeft ? absX : -absX, cap.offset.y);
        }
        else if (enemyCol is BoxCollider2D box)
        {
            float absX = Mathf.Abs(box.offset.x);
            if (absX < 0.1f) absX = 1.14f;
            box.offset = new Vector2(faceLeft ? absX : -absX, box.offset.y);
        }
    }
}
