using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BloodMonsterAI : MonoBehaviour, IDamageable
{
    public static event System.Action<BloodMonsterAI> OnEnemyDied;

    [Header("Boss / Monster Mode")]
    [SerializeField] public bool isBoss = false;
    public bool IsBoss => isBoss;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 180;
    [SerializeField] private float moveSpeed = 3.4f;
    [SerializeField] private int attackDamage = 18;
    [SerializeField] private float attackCooldown = 1.6f;

    [Header("Boss Phases (if Boss)")]
    public int totalBossPhases = 3;
    private int currentBossPhase = 3;
    public int CurrentBossPhase => currentBossPhase;
    private int phaseMaxHealth = 480;
    private bool isTransitioningPhase = false;

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 9.5f;
    [SerializeField] private float meleeRange = 1.8f;
    [SerializeField] private float rangedAttackRange = 6.8f;

    [Header("Ranged Attack Settings")]
    [SerializeField] private float projectileSpeed = 8.0f;
    [SerializeField] private Sprite runtimeBloodOrbSprite;

    private int currentHealth;
    private float attackTimer = 0f;
    private bool isAttacking = false;
    private bool isDead = false;
    public bool IsDead => isDead;
    private bool isKnockedBack = false;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animator;
    private Collider2D enemyCol;

    // Animator Hashes
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SpecialAttackHash = Animator.StringToHash("SpecialAttack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        enemyCol = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) gameObject.layer = enemyLayer;
        gameObject.tag = "Enemy";

        if (enemyCol == null)
        {
            var cap = gameObject.AddComponent<CapsuleCollider2D>();
            cap.size = isBoss ? new Vector2(1.5f, 1.4f) : new Vector2(1.0f, 1.0f);
            cap.direction = CapsuleDirection2D.Vertical;
            enemyCol = cap;
        }

        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CreateBloodOrbSprite();
    }

    private void CreateBloodOrbSprite()
    {
        if (runtimeBloodOrbSprite != null) return;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = 7.5f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < 2.5f)
                    tex.SetPixel(x, y, new Color(1f, 0.9f, 0.8f, 1f)); // Loi trang hong sang
                else if (dist < 5.0f)
                    tex.SetPixel(x, y, new Color(0.95f, 0.15f, 0.2f, 1f)); // Mau do tuoi ruc ro
                else if (dist < 7.0f)
                    tex.SetPixel(x, y, new Color(0.55f, 0.05f, 0.1f, 0.85f)); // Vien mau tham
                else if (dist < 8.0f)
                    tex.SetPixel(x, y, new Color(0.35f, 0.0f, 0.05f, 0.35f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        runtimeBloodOrbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    void Start()
    {
        FindPlayerTarget();
        if (isBoss)
        {
            BossHealthBarUI.EnsureInstance()?.ShowBoss(null, "GOR'MOK • HUYẾT THÚ CỔ TỰ");
            BossHealthBarUI.EnsureInstance()?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
        }
    }

    private void FindPlayerTarget()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if (player == null)
        {
            FindPlayerTarget();
            SetMoving(false);
            return;
        }

        if (isKnockedBack || isTransitioningPhase || isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Ngoai tam phat hien
        if (distance > detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);
            return;
        }

        // Quay mat ve huong nguoi choi
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (spriteRenderer != null)
        {
            if (dir.x > 0.05f) spriteRenderer.flipX = false;
            else if (dir.x < -0.05f) spriteRenderer.flipX = true;
        }

        // 1. Can chien (Melee Claws)
        if (distance <= meleeRange)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);
            if (attackTimer <= 0f)
            {
                StartCoroutine(PerformMeleeAttackRoutine());
            }
            return;
        }

        // 2. Danh tam xa (Blood Orb Spit)
        if (distance <= rangedAttackRange)
        {
            if (attackTimer <= 0f)
            {
                StartCoroutine(PerformRangedAttackRoutine());
                return;
            }
        }

        // 3. Di chuyen tiep can
        rb.linearVelocity = dir * moveSpeed;
        SetMoving(true);
    }

    private void SetMoving(bool moving)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(IsMovingHash, moving);
        }
    }

    private IEnumerator PerformMeleeAttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(AttackHash);
        }

        yield return new WaitForSeconds(0.35f);
        if (isDead) { isAttacking = false; yield break; }

        // Gay sat thuong can chien neu nguoi choi con o gan
        if (player != null && Vector2.Distance(transform.position, player.position) <= (meleeRange + 0.6f))
        {
            var dmg = player.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(attackDamage);
                SoundManager.EnsureInstance()?.PlayHitImpact();
                DamagePopup.Create(player.position + Vector3.up * 0.8f, attackDamage, isPlayer: true);
            }
        }

        yield return new WaitForSeconds(0.35f);
        isAttacking = false;
    }

    private IEnumerator PerformRangedAttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(SpecialAttackHash);
        }

        yield return new WaitForSeconds(0.45f);
        if (isDead) { isAttacking = false; yield break; }

        if (player != null)
        {
            Vector2 aimDir = ((Vector2)player.position + Vector2.up * 0.4f - (Vector2)transform.position).normalized;
            Vector3 spawnPos = transform.position + (Vector3)(aimDir * 0.6f) + Vector3.up * 0.3f;

            if (isBoss && currentBossPhase <= 2)
            {
                // Boss ban chum 3 vien cau gai mau
                float[] angles = new float[] { -18f, 0f, 18f };
                foreach (float angle in angles)
                {
                    Vector2 spreadDir = Quaternion.Euler(0, 0, angle) * aimDir;
                    SpawnBloodProjectile(spawnPos, spreadDir);
                }
            }
            else
            {
                SpawnBloodProjectile(spawnPos, aimDir);
            }

            SoundManager.EnsureInstance()?.PlayEnemyShoot();
        }

        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    private void SpawnBloodProjectile(Vector3 pos, Vector2 dir)
    {
        GameObject proj = new GameObject("BloodMonster_Projectile");
        proj.transform.position = pos;
        proj.transform.localScale = isBoss ? Vector3.one * 0.52f : Vector3.one * 0.40f;

        var sr = proj.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeBloodOrbSprite;
        sr.sortingOrder = 5500;

        var col = proj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.14f;

        var rbProj = proj.AddComponent<Rigidbody2D>();
        rbProj.gravityScale = 0f;
        rbProj.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var enemyProj = proj.AddComponent<EnemyProjectile>();
        enemyProj.Initialize(dir, attackDamage - 3, projectileSpeed);
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
            var bossUI = BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance();
            bossUI?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
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
                    var bossUI = BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance();
                    bossUI?.HideBoss();
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
        SetMoving(false);

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.5f, 0.6f);

        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        string title = currentBossPhase == 2 
            ? "PHASE II: HUYẾT THẦN THỨC TỈNH!" 
            : "FINAL PHASE: TẬN THẾ HUYẾT NGUYỆT!";
        DamagePopup.CreateText(transform.position + Vector3.up * 2.0f, title, new Color(1f, 0.2f, 0.2f));

        moveSpeed *= 1.25f;
        attackDamage += 6;
        attackCooldown *= 0.75f;

        if (spriteRenderer != null)
        {
            originalColor = currentBossPhase == 2 ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.15f, 0.15f);
            spriteRenderer.color = originalColor;
        }

        var bossUI = BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance();
        bossUI?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);

        yield return new WaitForSeconds(1.2f);
        isTransitioningPhase = false;
    }

    private IEnumerator KnockbackRoutine(Vector2 dir)
    {
        isKnockedBack = true;
        rb.linearVelocity = dir * 5.5f;
        yield return new WaitForSeconds(0.14f);
        isKnockedBack = false;
    }

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        Debug.Log($"<color=red>[BLOOD MONSTER SLAIN]</color> {(isBoss ? "Đại Trùm Gor'Mok" : "Huyết Thú")} đã gục ngã!");

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(DieHash);
        }

        if (isBoss) SoundManager.EnsureInstance()?.PlayBossDeath();
        else SoundManager.EnsureInstance()?.PlayHitImpact();

        OnEnemyDied?.Invoke(this);

        if (isBoss)
        {
            AethelShard.SpawnShards(transform.position, Random.Range(24, 35));
        }
        else
        {
            AethelShard.SpawnShards(transform.position, Random.Range(4, 7));
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 0.85f);
    }

    public void SetupBoss(float scaleMultiplier = 1.85f, int damageMultiplier = 2)
    {
        isBoss = true;
        transform.localScale = Vector3.one * scaleMultiplier;

        totalBossPhases = 3;
        currentBossPhase = 3;
        phaseMaxHealth = 500;
        currentHealth = phaseMaxHealth;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) gameObject.layer = enemyLayer;
        gameObject.tag = "Enemy";

        var cap = GetComponent<CapsuleCollider2D>();
        if (cap == null) cap = gameObject.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.5f, 1.4f);
        cap.direction = CapsuleDirection2D.Vertical;
        cap.isTrigger = false;
        enemyCol = cap;

        attackDamage *= damageMultiplier;
        moveSpeed *= 1.15f;
        projectileSpeed *= 1.3f;
        meleeRange = 2.5f;
        rangedAttackRange = 8.5f;
        detectionRange = 14.0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.35f, 0.35f);
            originalColor = spriteRenderer.color;
        }

        BossHealthBarUI.EnsureInstance()?.ShowBoss("GOR'MOK • HUYẾT THÚ CỔ TỰ", totalBossPhases);
    }
}
