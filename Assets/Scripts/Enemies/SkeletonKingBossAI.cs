using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class SkeletonKingBossAI : MonoBehaviour, IDamageable
{
    public static event System.Action<SkeletonKingBossAI> OnBossDied;

    [Header("Boss Identity")]
    public string bossName = "CHUA TE NHEN - SKELETON KING";
    public int totalBossPhases = 3;
    public int currentBossPhase = 1;
    public int phaseMaxHealth = 1250;
    public int currentHealth;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private int meleeDamage = 38;
    [SerializeField] private int slamDamage = 55;
    [SerializeField] private float meleeRange = 2.4f;
    [SerializeField] private float meleeCooldown = 1.5f;
    [SerializeField] private float skillCooldown = 6.0f;
    [SerializeField] private float webSpitCooldown = 5.0f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CapsuleCollider2D col;
    private Color originalColor;

    private float meleeTimer = 0f;
    private float skillTimer = 3.0f;
    private float webSpitTimer = 2.5f;
    private bool isBusy = false;
    private bool isPhaseTransitioning = false;
    private bool isDead = false;

    // Animator Hashes
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SkillHash = Animator.StringToHash("Skill");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = phaseMaxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        gameObject.layer = (enemyLayer != -1) ? enemyLayer : 6;
        gameObject.tag = "Enemy";

        if (col != null)
        {
            col.size = new Vector2(2.5f, 4.0f);
            col.offset = new Vector2(0f, 2.0f);
            col.direction = CapsuleDirection2D.Vertical;
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
        StartCoroutine(InitBossHealthBarRoutine());
    }

    private IEnumerator InitBossHealthBarRoutine()
    {
        yield return new WaitForSeconds(0.4f);
        var bossUI = BossHealthBarUI.EnsureInstance();
        if (bossUI != null)
        {
            bossUI.ShowBoss(bossName, totalBossPhases);
            bossUI.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
        }
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
        if (isDead || isPhaseTransitioning) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (meleeTimer > 0f) meleeTimer -= Time.deltaTime;
        if (skillTimer > 0f) skillTimer -= Time.deltaTime;
        if (webSpitTimer > 0f) webSpitTimer -= Time.deltaTime;

        if (isBusy) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Lat mat theo huong nguoi choi
        Vector3 diff = player.position - transform.position;
        if (Mathf.Abs(diff.x) > 0.05f)
        {
            transform.localScale = new Vector3(diff.x > 0 ? 1.2f : -1.2f, 1.2f, 1f);
        }

        // Chieu 1: Phun Ken To Nhen lam choang 2s (cooldown 5s)
        if (webSpitTimer <= 0f && dist >= 3.0f && dist <= 14f)
        {
            StartCoroutine(WebSpitSkillRoutine());
            return;
        }

        // Chieu 2: Ky nang dac biet theo Phase (Dam dat & Tha cot trung nhen / Bao dia chan)
        if (skillTimer <= 0f)
        {
            if (currentBossPhase == 3)
            {
                StartCoroutine(TripleEarthquakeSkillRoutine());
            }
            else
            {
                StartCoroutine(GroundSlamSkillRoutine());
            }
            return;
        }

        // Chieu 3: Luot toi chem neu Player o xa (Phase 2 & 3)
        if (currentBossPhase >= 2 && dist > 5.5f && dist < 12f && meleeTimer <= 0.2f)
        {
            StartCoroutine(DashSlashRoutine());
            return;
        }

        // Tan cong can chien (Melee Cleave)
        if (dist <= meleeRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool(IsMovingHash, false);

            if (meleeTimer <= 0f)
            {
                StartCoroutine(MeleeCleaveAttackRoutine());
            }
        }
        else
        {
            // Di chuyen san lung player
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
            if (animator != null) animator.SetBool(IsMovingHash, true);
        }
    }

    void LateUpdate()
    {
        ClampInsideRoomBounds();
    }

    private void ClampInsideRoomBounds()
    {
        if (isDead) return;
        var roomMgr = DungeonRoomManager.Instance;
        if (roomMgr != null && roomMgr.currentRoom != null && roomMgr.currentRoom.roomBounds.width > 0)
        {
            Rect b = roomMgr.currentRoom.roomBounds;
            float padding = 1.0f;
            Vector3 pos = transform.position;
            float clampedX = Mathf.Clamp(pos.x, b.xMin + padding, b.xMax - padding);
            float clampedY = Mathf.Clamp(pos.y, b.yMin + padding, b.yMax - padding);

            if (Mathf.Abs(pos.x - clampedX) > 0.01f || Mathf.Abs(pos.y - clampedY) > 0.01f)
            {
                transform.position = new Vector3(clampedX, clampedY, pos.z);
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }
    }

    // =========================================================================
    // KY NANG PHUN KEN TO NHEN (STUN 2s, COOLDOWN 5s)
    // =========================================================================
    private IEnumerator WebSpitSkillRoutine()
    {
        isBusy = true;
        webSpitTimer = webSpitCooldown;
        rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool(IsMovingHash, false);

        // Huong ve phia player
        Vector2 targetPos = player.position;
        Vector2 spawnOrigin = (Vector2)transform.position + Vector2.up * 1.5f;
        Vector2 fireDir = (targetPos - spawnOrigin).normalized;

        // Vung canh bao tia ban
        GameObject warningLine = CreateWarningAimLine(spawnOrigin, fireDir, 9.0f, 0.35f);

        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        yield return new WaitForSeconds(0.35f);

        if (isDead) yield break;

        // Ban Ken To Nhen
        SpiderWebProjectile.Launch(spawnOrigin, fireDir);
        SoundManager.EnsureInstance()?.PlayHitImpact();

        yield return new WaitForSeconds(0.25f);
        isBusy = false;
    }

    private GameObject CreateWarningAimLine(Vector2 origin, Vector2 dir, float length, float duration)
    {
        GameObject lineObj = new GameObject("WebAimLine");
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.12f;
        lr.endWidth = 0.12f;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 1f, 1f, 0.85f);
        lr.endColor = new Color(0.85f, 0.9f, 1f, 0.15f);
        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin + dir * length);

        Destroy(lineObj, duration);
        return lineObj;
    }

    // =========================================================================
    // KY NANG MELEE CLEAVE (Vung Chan Nhen Can Chien)
    // =========================================================================
    private IEnumerator MeleeCleaveAttackRoutine()
    {
        isBusy = true;
        meleeTimer = meleeCooldown;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        yield return new WaitForSeconds(0.25f);

        if (isDead) yield break;

        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeRange + 0.8f)
        {
            IDamageable target = player.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(meleeDamage);
            }

            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.18f, 0.2f);
        }

        yield return new WaitForSeconds(0.35f);
        isBusy = false;
    }

    // =========================================================================
    // KY NANG GROUND SLAM & BONE EGGS (Dam Dat & Tha Trung Nhen No Mini)
    // =========================================================================
    private IEnumerator GroundSlamSkillRoutine()
    {
        isBusy = true;
        skillTimer = skillCooldown;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetBool(IsMovingHash, false);

        // Vong canh bao do
        GameObject warningCircle = CreateWarningRing(transform.position, 3.5f, 0.5f);

        yield return new WaitForSeconds(0.45f);

        if (isDead) yield break;

        if (animator != null) animator.SetTrigger(SkillHash);

        yield return new WaitForSeconds(0.25f);

        // Sat thuong lan xung quanh
        if (player != null && Vector2.Distance(transform.position, player.position) <= 3.6f)
        {
            IDamageable target = player.GetComponent<IDamageable>();
            if (target != null) target.TakeDamage(slamDamage);
        }

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.5f);

        // Sinh 2 Qua Trung Nhen xung quanh (se no va de ra Nhen Con Mini)
        Vector3 leftEgg = transform.position + new Vector3(-2.8f, Random.Range(-0.8f, 0.8f), 0f);
        Vector3 rightEgg = transform.position + new Vector3(2.8f, Random.Range(-0.8f, 0.8f), 0f);
        BoneEggObstacle.Spawn(leftEgg);
        BoneEggObstacle.Spawn(rightEgg);

        yield return new WaitForSeconds(0.4f);
        isBusy = false;
    }

    // =========================================================================
    // KY NANG DASH SLASH (Luot chem bao kich)
    // =========================================================================
    private IEnumerator DashSlashRoutine()
    {
        isBusy = true;
        meleeTimer = meleeCooldown + 0.8f;
        rb.linearVelocity = Vector2.zero;

        Vector2 dashDir = (player.position - transform.position).normalized;
        GameObject warningLine = CreateWarningAimLine(transform.position, dashDir, 6.0f, 0.35f);

        yield return new WaitForSeconds(0.35f);

        if (isDead) yield break;

        if (animator != null) animator.SetTrigger(AttackHash);

        // Luot cuc nhanh ve phia truoc
        float dashTime = 0.25f;
        float elapsed = 0f;
        while (elapsed < dashTime && !isDead)
        {
            rb.linearVelocity = dashDir * (moveSpeed * 3.6f);
            elapsed += Time.deltaTime;

            if (player != null && Vector2.Distance(transform.position, player.position) <= 2.2f)
            {
                var target = player.GetComponent<IDamageable>();
                if (target != null) target.TakeDamage(meleeDamage + 8);
                break;
            }
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);
        isBusy = false;
    }

    // =========================================================================
    // KY NANG TRIPLE EARTHQUAKE (Phase 3: Bao Dia Chan)
    // =========================================================================
    private IEnumerator TripleEarthquakeSkillRoutine()
    {
        isBusy = true;
        skillTimer = skillCooldown;
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            if (isDead) yield break;

            Vector3 targetPos = (player != null) ? player.position : transform.position;
            GameObject ring = CreateWarningRing(targetPos, 2.5f, 0.4f);

            yield return new WaitForSeconds(0.4f);

            if (isDead) yield break;

            if (animator != null) animator.SetTrigger(SkillHash);

            if (player != null && Vector2.Distance(targetPos, player.position) <= 2.5f)
            {
                var target = player.GetComponent<IDamageable>();
                if (target != null) target.TakeDamage(20);
            }

            // Sinh Trung Nhen no ra Nhen Con Mini
            BoneEggObstacle.Spawn(targetPos);

            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.25f, 0.3f);

            yield return new WaitForSeconds(0.3f);
        }

        isBusy = false;
    }

    private GameObject CreateWarningRing(Vector3 pos, float radius, float duration)
    {
        GameObject ring = new GameObject("BossWarningRing");
        ring.transform.position = pos;
        var lr = ring.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.positionCount = 36;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.2f, 0.2f, 0.7f);
        lr.endColor = new Color(1f, 0.2f, 0.2f, 0.7f);

        float delta = (Mathf.PI * 2f) / 36;
        for (int i = 0; i < 36; i++)
        {
            float theta = i * delta;
            lr.SetPosition(i, new Vector3(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius * 0.7f, 0f));
        }

        Destroy(ring, duration);
        return ring;
    }

    // =========================================================================
    // SAT THUONG & CHUYEN PHASE
    // =========================================================================
    public void TakeDamage(int damage)
    {
        if (isDead || isPhaseTransitioning) return;

        currentHealth -= damage;
        StartCoroutine(DamageFlashRoutine());
        if (animator != null) animator.SetTrigger(HitHash);

        var bossUI = BossHealthBarUI.Instance ?? Object.FindAnyObjectByType<BossHealthBarUI>();
        if (bossUI != null)
        {
            bossUI.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
        }

        if (currentHealth <= 0)
        {
            if (currentBossPhase < totalBossPhases)
            {
                StartCoroutine(NextPhaseRoutine());
            }
            else
            {
                Die();
            }
        }
    }

    private IEnumerator NextPhaseRoutine()
    {
        isPhaseTransitioning = true;
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

        currentBossPhase++;
        currentHealth = phaseMaxHealth;

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.5f, 0.7f);

        DamagePopup.CreateText(transform.position + Vector3.up * 2f, LocalizationManager.T($"GIAI ĐOẠN {currentBossPhase} - CUỒNG NỘ!", $"PHASE {currentBossPhase} - ENRAGED!"), Color.red);

        moveSpeed += 0.5f;
        meleeCooldown = Mathf.Max(1.0f, meleeCooldown - 0.2f);
        skillCooldown = Mathf.Max(3.5f, skillCooldown - 0.8f);
        webSpitCooldown = Mathf.Max(3.5f, webSpitCooldown - 0.5f);

        // CHI TRIEU HOI NHEN CON MINI (Tuyet doi khong co quai ngoai lai nao khac)
        SpawnPhaseMinions();

        yield return new WaitForSeconds(0.8f);

        var bossUI = BossHealthBarUI.Instance ?? Object.FindAnyObjectByType<BossHealthBarUI>();
        if (bossUI != null)
        {
            bossUI.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
        }

        isPhaseTransitioning = false;
        isBusy = false;
    }

    private void SpawnPhaseMinions()
    {
        if (currentBossPhase == 2)
        {
            MiniSpiderMinion.Spawn(transform.position + Vector3.left * 2.5f);
            MiniSpiderMinion.Spawn(transform.position + Vector3.right * 2.5f);
        }
        else if (currentBossPhase == 3)
        {
            MiniSpiderMinion.Spawn(transform.position + Vector3.left * 2.5f);
            MiniSpiderMinion.Spawn(transform.position + Vector3.right * 2.5f);
            MiniSpiderMinion.Spawn(transform.position + Vector3.up * 2.0f);
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

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger(DieHash);
        }

        // 1. AN THANH MAU BOSS NGAY LAP TUC!
        var bossUI = BossHealthBarUI.Instance ?? Object.FindAnyObjectByType<BossHealthBarUI>();
        if (bossUI != null)
        {
            bossUI.HideBoss();
        }

        // 2. TIEU DIET TOAN BO QUAI / MINION CON LAI TREN SAN DAU KHI BOSS CHET!
        ClearAllRemainingEnemies();

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.6f, 1.0f);

        AethelShard.SpawnShards(transform.position, 35);
        HealthPotion.Spawn(transform.position + Vector3.left * 0.8f, 1);
        HealthPotion.Spawn(transform.position + Vector3.right * 0.8f, 1);
        WorldItemPickup.Spawn(transform.position, "core_abyssal_soul", 1);
        WorldItemPickup.RollDrop(transform.position + Vector3.up * 0.5f, true);
        TreasureChest.SpawnChest(transform.position + Vector3.down * 1.5f);

        // 3. Trieu hoi duy nhat 1 Cong Thien Gioi
        StartCoroutine(SpawnHeavenlyPortalRoutine());

        OnBossDied?.Invoke(this);
        Destroy(gameObject, 2.5f);
    }

    private void ClearAllRemainingEnemies()
    {
        var minispiders = Object.FindObjectsByType<MiniSpiderMinion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var ms in minispiders)
        {
            if (ms != null) Destroy(ms.gameObject);
        }

        var obelisks = Object.FindObjectsByType<BoneEggObstacle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var ob in obelisks)
        {
            if (ob != null) Destroy(ob.gameObject);
        }

        var goblins = Object.FindObjectsByType<GoblinAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var g in goblins)
        {
            if (g != null) Destroy(g.gameObject);
        }

        var skeletons = Object.FindObjectsByType<SkeletonAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in skeletons)
        {
            if (s != null) Destroy(s.gameObject);
        }

        var slimes = Object.FindObjectsByType<SlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var sl in slimes)
        {
            if (sl != null) Destroy(sl.gameObject);
        }
    }

    private IEnumerator SpawnHeavenlyPortalRoutine()
    {
        yield return new WaitForSeconds(1.8f);
        RuinPortal.SpawnPortal(transform.position);
    }
}