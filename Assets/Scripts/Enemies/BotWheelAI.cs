using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class BotWheelAI : MonoBehaviour, IDamageable, IEnemyHealth
{
    public static event System.Action<BotWheelAI> OnEnemyDied;

    [SerializeField] public bool isBoss = false;
    public bool IsBoss => isBoss;

    [Header("Enemy Stats")]
    [SerializeField] private int maxHealth = 110; // Quai thuong can bang (110 HP)
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private int attackDamage = 11;
    [SerializeField] public float attackCooldown = 2.0f;

    [Header("Boss 3 Health Bars")]
    public int totalBossPhases = 3;
    private int currentBossPhase = 3;
    public int CurrentBossPhase => currentBossPhase;
    private int phaseMaxHealth = 420;
    private bool isTransitioningPhase = false;
    public bool IsTransitioningPhase => isTransitioningPhase;

    [Header("Boss Clone & Teleport Mechanics")]
    [SerializeField] public bool isClone = false;
    [SerializeField] private BotWheelAI mainBoss;
    private Vector3 initialSpawnPosition;
    private bool isPurpleRingActive = false;
    public bool IsPurpleRingActive => isPurpleRingActive;
    private float purpleRingTimer = 14f;
    private List<BotWheelAI> activeClones = new List<BotWheelAI>();

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
    public bool IsDead => isDead;
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
        try { gameObject.tag = "Enemy"; } catch { }
        maxHealth = MapScalingManager.ScaleHealth(maxHealth);
        attackDamage = MapScalingManager.ScaleDamage(attackDamage);
        currentHealth = maxHealth;
        initialSpawnPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 4.0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        enemyCol = DynamicSpriteColliderSync.EnsureAttached(gameObject);
        EnemyNavigationHelper.EnsureFrictionless(enemyCol);
        EnemyCCCleanseSystem.EnsureAttached(gameObject);

        // Dam bao tam ban va phat hien luon du tam tam xa
        if (attackRange < 4.5f) attackRange = 5.2f;
        if (detectionRange < 7.5f) detectionRange = 8.5f;

        if (runtimeBulletSprite == null)
        {
            runtimeBulletSprite = Resources.Load<Sprite>("Combat/enemy_projectile");
#if UNITY_EDITOR
            if (runtimeBulletSprite == null) runtimeBulletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/enemy_projectile.png");
#endif
            if (runtimeBulletSprite == null)
            {
                // Tao vien dan nang luong mau tim toa sang ruc ro
                int texSize = 16;
                Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                float center = 7.5f;
                for (int x = 0; x < texSize; x++)
                {
                    for (int y = 0; y < texSize; y++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        if (dist < 2.5f)
                            tex.SetPixel(x, y, new Color(0.96f, 0.88f, 1f, 1f)); // Loi trang tim sang
                        else if (dist < 4.8f)
                            tex.SetPixel(x, y, new Color(0.72f, 0.22f, 0.98f, 1f)); // Vang tim neon ruc ro
                        else if (dist < 6.8f)
                            tex.SetPixel(x, y, new Color(0.49f, 0.08f, 0.82f, 0.8f)); // Hao quang tim dam
                        else if (dist < 7.8f)
                            tex.SetPixel(x, y, new Color(0.35f, 0.04f, 0.65f, 0.35f)); // Vien ngoai mo
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
                }
                tex.Apply();
                runtimeBulletSprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 32);
            }
        }
    }

    private bool hasPlayedIntro = false;

    public void TriggerBossIntro()
    {
        if (hasPlayedIntro || !isBoss || isClone) return;
        hasPlayedIntro = true;
        BossIntroCutsceneUI.PlayGoliathGikk(transform);
    }

    private string GetBossDisplayName()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene.Contains("Map2") || scene.Contains("Valley"))
        {
            return LocalizationManager.T("CỰ THẠCH HỘ VỆ THẦN THỤ", "ANCIENT VALLEY GOLEM");
        }
        return LocalizationManager.T("PHÁO ĐÀI BÁNH XE CỔ ĐẠI", "THE ANCIENT TITAN WHEEL");
    }

    void Start()
    {
        FindPlayerTarget();
        if (isBoss && !isClone)
        {
            BossHealthBarUI.EnsureInstance()?.ShowBoss(this, GetBossDisplayName());
            TriggerBossIntro();
        }
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

        // Đóng băng hoàn toàn AI khi đang hiển thị màn hình chọn Lõi Nâng Cấp
        if (LevelUpChoiceUI.IsShowing || BossIntroCutsceneUI.IsCutsceneActive)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);
            // Reset cooldown để kẻ địch KHÔNG bắn ngay khi màn hình đóng lại
            attackCooldownTimer = attackCooldown * 0.5f;
            return;
        }

        if (PlayerHealth.IsPlayerDead)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);
            return;
        }

        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // Đòn ma trận đạn tím dịch chuyển trung tâm định kỳ cho Boss chính ở Phase 2 & 1
        if (isBoss && !isClone && currentBossPhase <= 2 && !isTransitioningPhase && !isPurpleRingActive)
        {
            purpleRingTimer -= Time.deltaTime;
            if (purpleRingTimer <= 0)
            {
                purpleRingTimer = Random.Range(14f, 18f);
                StartCoroutine(PurpleBulletRingTeleportRoutine());
            }
        }

        if (player == null)
        {
            FindPlayerTarget();
            SetMoving(false);
            return;
        }

        if (isKnockedBack || isTransitioningPhase || isPurpleRingActive) return;

        Vector3 bodyPos = GetBodyFootPosition();
        float distanceToPlayer = Vector2.Distance(bodyPos, player.position);

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

            // Luon quay nong sung ve phia Player (dung deadzone 0.15m de khong giat mat)
            float dx = player.position.x - bodyPos.x;
            if (Mathf.Abs(dx) > 0.15f)
            {
                SetFacing(dx < 0f);
            }

            if (attackCooldownTimer <= 0)
            {
                AttackPlayer();
            }
            return;
        }

        // 3. Trong tam phat hien nhung chua toi tam ban -> Tiep can nguoi choi (luon lach qua vat can)
        Vector2 dir = ((Vector2)player.position - (Vector2)bodyPos).normalized;
        Vector2 moveDir = EnemyNavigationHelper.GetSteeredDirection(bodyPos, dir, enemyCol, 0.35f, 1.0f);
        rb.linearVelocity = moveDir * moveSpeed;
        SetMoving(true);

        if (spriteRenderer != null)
        {
            float moveDx = player.position.x - bodyPos.x;
            if (Mathf.Abs(moveDx) > 0.15f)
            {
                SetFacing(moveDx < 0f);
            }
        }
    }

    private Rect homeRoomBounds;

    void FixedUpdate()
    {
        if (isDead || LevelUpChoiceUI.IsShowing || BossIntroCutsceneUI.IsCutsceneActive) return;
        if (isBoss) ClampInsideRoomBounds();
    }

    void LateUpdate()
    {
        if (LevelUpChoiceUI.IsShowing || BossIntroCutsceneUI.IsCutsceneActive) return;
        ClampInsideRoomBounds();
    }

    private void ClampInsideRoomBounds()
    {
        if (isDead) return;
        float padding = isBoss ? 1.35f : 0.7f;
        Vector3 bodyPos = GetBodyFootPosition();
        Vector3 clamped = DungeonRoomManager.ClampEnemyToHomeRoom(bodyPos, ref homeRoomBounds, padding);
        if (Mathf.Abs(bodyPos.x - clamped.x) > 0.005f || Mathf.Abs(bodyPos.y - clamped.y) > 0.005f)
        {
            Vector3 delta = new Vector3(clamped.x - bodyPos.x, clamped.y - bodyPos.y, 0f);
            transform.position += delta;
            if (rb != null)
            {
                rb.position = (Vector2)transform.position;
                rb.linearVelocity = Vector2.zero;
            }
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
            StartCoroutine(SpawnBulletRoutine());
            SoundManager.EnsureInstance()?.PlayEnemyShoot();
        }
    }

    private IEnumerator SpawnBulletRoutine()
    {
        yield return new WaitForSeconds(0.12f);
        if (isDead || player == null) yield break;

        // Vị trí trọng tâm người chơi (ngực / BoxCollider2D của Player)
        Vector3 playerCenter = player.position + Vector3.up * 1.0f;

        Vector3 bodyPos = GetBodyFootPosition();
        // Luôn quay nòng súng đối diện người chơi
        bool facingLeft = playerCenter.x < bodyPos.x;
        SetFacing(facingLeft);
        bodyPos = GetBodyFootPosition();

        // Vị trí nòng súng phía trước mặt thân thật của quái
        float absScaleX = Mathf.Abs(transform.localScale.x);
        float absScaleY = Mathf.Abs(transform.localScale.y);
        float muzzleX = (facingLeft ? -0.35f : 0.35f) * absScaleX;
        float muzzleY = 0.42f * absScaleY;
        Vector3 muzzlePos = bodyPos + new Vector3(muzzleX, muzzleY, 0f);

        // Hướng bắn trực diện từ nòng súng vào thẳng ngực người chơi (dù đứng sát cạnh hay góc nào đều bắn trúng 100%)
        Vector2 flyDir = ((Vector2)playerCenter - (Vector2)muzzlePos).normalized;
        Vector3 spawnBase = muzzlePos + (Vector3)(flyDir * 0.1f);

        if (isBoss)
        {
            // Boss bắn tia 3 viên đạn năng lượng tím bay tỏa hình quạt về phía người chơi
            float[] angles = new float[] { -15f, 0f, 15f };
            foreach (float angle in angles)
            {
                Vector2 spreadDir = Quaternion.Euler(0, 0, angle) * flyDir;
                CreateRuntimeBullet(spawnBase, spreadDir);
            }
        }
        else
        {
            CreateRuntimeBullet(spawnBase, flyDir);
        }
    }

    private void CreateRuntimeBullet(Vector3 spawnPos, Vector2 dir)
    {
        GameObject bullet = new GameObject("Enemy_EnergyBullet");
        bullet.transform.position = spawnPos;
        // Kích thước rõ ràng, bay đẹp mắt
        bullet.transform.localScale = isBoss ? Vector3.one * 0.55f : Vector3.one * 0.45f;

        var sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeBulletSprite;
        sr.sortingOrder = 5500;

        var col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        var rbBullet = bullet.AddComponent<Rigidbody2D>();
        rbBullet.gravityScale = 0f;
        rbBullet.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var proj = bullet.AddComponent<EnemyProjectile>();
        proj.Initialize(dir, attackDamage, projectileSpeed);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isTransitioningPhase) return;

        // KHI LÀ PHÂN THÂN: Sài chung 1 thanh máu duy nhất với Trùm chính!
        if (isClone && mainBoss != null)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(HurtHash);
            }
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashRedRoutine());
            }
            var c = FindAnyObjectByType<CameraFollow>();
            if (c != null) c.Shake(0.12f, 0.2f);

            // Chuyển toàn bộ sát thương sang thanh máu của Trùm chính
            mainBoss.TakeDamage(damage);
            return;
        }

        currentHealth -= damage;
        if (!isBoss)
        {
            EnemyFloatingHealthBar.ReportHealth(gameObject, currentHealth, maxHealth);
        }

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.2f);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(HurtHash);
        }

        if (player != null && !isBoss)
        {
            Vector2 knockbackDir = ((Vector2)GetBodyFootPosition() - (Vector2)player.position).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir));
        }

        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRedRoutine());
        }

        if (isBoss && !isClone)
        {
            var bossUI = BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance();
            if (bossUI != null)
            {
                if (!bossUI.IsVisible)
                {
                    bossUI.ShowBoss(this, GetBossDisplayName());
                }
                bossUI.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
            }
        }

        if (currentHealth <= 0)
        {
            if (isBoss && !isClone && currentBossPhase > 1)
            {
                StartCoroutine(BossPhaseTransitionRoutine());
            }
            else
            {
                if (isBoss && !isClone)
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

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.55f);

        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        string phaseTitle = currentBossPhase == 2 
            ? LocalizationManager.T("HƯ KHÔNG THỨC TỈNH!", "VOID AWAKENING!") 
            : LocalizationManager.T("CUỒNG NỘ TAM THỂ!", "TRIPLE FRENZY!");
        Color titleColor = currentBossPhase == 2 
            ? new Color(0.85f, 0.45f, 1f) 
            : new Color(1f, 0.2f, 0.2f);

        Vector3 bossCenter = GetBossVisualCenter();
        DamagePopup.CreateText(bossCenter + Vector3.up * 1.5f, phaseTitle, titleColor);

        // Đổi màu hào quang Boss (Phase 2: Tím hư không, Phase 1: Đỏ huyết nguyệt)
        if (spriteRenderer != null)
        {
            originalColor = currentBossPhase == 2 
                ? new Color(0.9f, 0.4f, 1f) 
                : new Color(1f, 0.15f, 0.15f);
            spriteRenderer.color = originalColor;
        }

        // Tăng tốc và sức mạnh
        moveSpeed *= 1.25f;
        attackDamage += 6;
        attackCooldown *= 0.8f;

        // Đẩy lùi người chơi ra xa nhẹ
        if (player != null)
        {
            Vector2 pushDir = ((Vector2)player.position - (Vector2)bossCenter).normalized;
            var playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null) playerRb.AddForce(pushDir * 8f, ForceMode2D.Impulse);
        }

        var bossUI = BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance();
        bossUI?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);

        yield return new WaitForSeconds(1.0f);

        if (currentBossPhase == 2)
        {
            // Mất thanh máu đầu tiên: Ngay lập tức tạo vòng tròn đạn tím 3 giây
            yield return StartCoroutine(PurpleBulletRingTeleportRoutine());
        }
        else if (currentBossPhase == 1)
        {
            // Mất thanh máu thứ 2 (bước vào thanh máu cuối cùng):
            // Boss điên loạn cực độ (tăng tốc độ chạy, bắn nhanh gấp bội) và phân thân thành 3 con 3 màu khác nhau
            moveSpeed *= 1.35f;
            attackDamage += 8;
            attackCooldown *= 0.55f;
            projectileSpeed *= 1.25f;

            SpawnBossClones();
        }

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
        Debug.Log($"<color=green>[BOT WHEEL DESTROYED]</color> {(isClone ? "Phân thân" : "Quái vật")} đã bị tiêu diệt!");

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(DieHash);
        }
        if (isBoss && !isClone) SoundManager.EnsureInstance()?.PlayBossDeath();
        else SoundManager.EnsureInstance()?.PlayHitImpact();

        // Xóa sạch toàn bộ vòng đỏ cảnh báo và hiệu ứng chiêu còn sót lại
        if (isBoss)
        {
            BossGigapackSkills.ClearAllWarningZonesAndEffects();
        }

        if (!isClone)
        {
            // 1. Tiêu diệt toàn bộ phân thân còn lại NGAY LẬP TỨC trước khi thông báo kết thúc phòng
            foreach (var clone in activeClones)
            {
                if (clone != null && !clone.isDead)
                {
                    clone.isDead = true;
                    clone.Die();
                }
            }
            activeClones.Clear();

            // 2. Thông báo đợt quái / Spawner
            OnEnemyDied?.Invoke(this);

            // 3. Thông báo Room Manager kết thúc đại điện Boss
            if (isBoss)
            {
                var roomMgr = DungeonRoomManager.Instance ?? FindAnyObjectByType<DungeonRoomManager>();
                if (roomMgr != null)
                {
                    var bossRoom = roomMgr.rooms.Find(r => r != null && r.roomType == DungeonRoom.RoomType.TitanSanctum) ?? roomMgr.currentRoom;
                    if (bossRoom != null)
                    {
                        roomMgr.OnBossDefeated(bossRoom);
                    }
                }
            }
        }

        Vector3 dropPos = GetBodyFootPosition();
        if (isBoss && !isClone)
        {
            AethelShard.SpawnShards(dropPos, Random.Range(18, 26));
            WorldItemPickup.RollDrop(dropPos, true);
        }
        else if (!isClone)
        {
            AethelShard.SpawnShards(dropPos, Random.Range(3, 6));
            WorldItemPickup.RollDrop(dropPos, false);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 0.65f);
    }

    public void SetupBoss(float scaleMultiplier = 1.75f, int hpMultiplier = 3, int damageMultiplier = 2)
    {
        isBoss = true;
        Vector3 bodyBefore = GetBodyFootPosition();
        initialSpawnPosition = bodyBefore;
        transform.localScale = transform.localScale * scaleMultiplier;
        Vector3 bodyAfter = GetBodyFootPosition();
        transform.position += (bodyBefore - bodyAfter);

        // Hệ thống 3 thanh máu: Mỗi thanh 420 HP (scale theo Map)
        totalBossPhases = 3;
        currentBossPhase = 3;
        phaseMaxHealth = MapScalingManager.ScaleHealth(420);
        currentHealth = phaseMaxHealth;

        attackDamage = MapScalingManager.ScaleDamage(16);
        moveSpeed *= 0.95f;
        projectileSpeed *= 1.15f;
        attackRange = 7.0f;
        detectionRange = 12.0f;

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
        BossHealthBarUI.EnsureInstance()?.ShowBoss(this, GetBossDisplayName());
    }

    /// <summary>
    /// Vị trí chân thực tế của thân робоt BotWheel trên màn hình.
    /// Do bộ sprite gốc (117x26 px) có phần thân nằm lệch bên trái (x ≈ 21.5 px, cách tâm 58.5 px một đoạn 1.136 unit),
    /// hàm này luôn trả về tọa độ thế giới chính xác của thân quái vật.
    /// </summary>
    public Vector3 GetBodyFootPosition()
    {
        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        float absScaleX = Mathf.Abs(transform.localScale.x);
        float offsetX = (facingLeft ? 1.136f : -1.136f) * absScaleX;
        return transform.position + new Vector3(offsetX, 0f, 0f);
    }

    // =========================================================================
    // CHIÊU THỨC MA TRẬN ĐẠN TÍM (PHASE 2 & 1)
    // 1. Boss đứng im 1 giây
    // 2. Các viên đạn tím kết tinh xung quanh boss (boss là TÂM bánh xe, đạn là VÀNH)
    // 3. Sau 3 giây kết tinh, các viên đạn bắn bung tỏa 360 độ ra ngoài
    // =========================================================================
    public Vector3 GetBossVisualCenter()
    {
        Vector3 foot = GetBodyFootPosition();
        float absScaleY = Mathf.Abs(transform.localScale.y);
        return foot + new Vector3(0f, 0.35f * absScaleY, 0f);
    }

    private IEnumerator PurpleBulletRingTeleportRoutine()
    {
        isPurpleRingActive = true;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        // Hiển thị thông báo chiêu thức ngay trên đầu tâm thật của Boss
        Vector3 initialCenter = GetBossVisualCenter();
        Vector3 popupPos = initialCenter + Vector3.up * (1.1f * Mathf.Abs(transform.localScale.y));
        DamagePopup.CreateText(popupPos, LocalizationManager.T("MA TRẬN HƯ KHÔNG", "VOID MATRIX"), new Color(0.85f, 0.35f, 1f));
        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        // 1. BOSS ĐỨNG IM 1 GIÂY
        float preWait = 0f;
        while (preWait < 1.0f)
        {
            if (isDead)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                isPurpleRingActive = false;
                yield break;
            }
            preWait += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            yield return null;
        }

        // 2. CÁC VIÊN ĐẠN TÍM BẮT ĐẦU KẾT TINH XUNG QUANH TÂM NGƯỜI BOSS
        // Boss nằm chính xác ở tâm vòng tròn như tâm bánh xe đạp!
        int bulletCount = 18;
        float ringRadius = 1.85f;
        List<GameObject> ringBullets = new List<GameObject>();

        Vector3 bossCenter = GetBossVisualCenter();

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = (i * (360f / bulletCount)) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
            GameObject b = CreateOrbitBullet(bossCenter + offset);
            ringBullets.Add(b);
        }

        float duration = 3.0f;
        float elapsed = 0f;

        // 3. VÒNG ĐẠN XOAY QUANH BOSS TRONG 3 GIÂY
        // Boss tuyệt đối đứng im ở tâm, không được di chuyển cho đến khi đạn được bắn đi!
        while (elapsed < duration)
        {
            if (isDead)
            {
                foreach (var b in ringBullets) if (b != null) Destroy(b);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                isPurpleRingActive = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;

            bossCenter = GetBossVisualCenter();

            float baseAngle = elapsed * 190f;
            for (int i = 0; i < ringBullets.Count; i++)
            {
                if (ringBullets[i] != null)
                {
                    float angle = (i * (360f / bulletCount) + baseAngle) * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
                    ringBullets[i].transform.position = bossCenter + offset;
                }
            }

            yield return null;
        }

        if (isDead)
        {
            foreach (var b in ringBullets) if (b != null) Destroy(b);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            isPurpleRingActive = false;
            yield break;
        }

        // 4. HẾT 3 GIÂY: BẮN CÁC VIÊN ĐẠN ĐÓ RA NGOÀI 360 ĐỘ TỪ TÂM BOSS!
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.45f);

        SoundManager.EnsureInstance()?.PlayEnemyShoot();

        bossCenter = GetBossVisualCenter();
        for (int i = 0; i < ringBullets.Count; i++)
        {
            if (ringBullets[i] != null)
            {
                Vector2 outwardDir = ((Vector2)ringBullets[i].transform.position - (Vector2)bossCenter).normalized;
                if (outwardDir == Vector2.zero)
                {
                    float angle = (i * (360f / bulletCount)) * Mathf.Deg2Rad;
                    outwardDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }

                var proj = ringBullets[i].AddComponent<EnemyProjectile>();
                proj.Initialize(outwardDir, attackDamage + 3, projectileSpeed * 1.35f);
            }
        }

        yield return new WaitForSeconds(0.2f);
        // Khôi phục constraints và cho phép boss di chuyển lại sau khi bắn
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        isPurpleRingActive = false;
    }

    private GameObject CreateOrbitBullet(Vector3 spawnPos)
    {
        GameObject bullet = new GameObject("Boss_EnergyBullet_Orbit");
        bullet.transform.position = spawnPos;
        bullet.transform.localScale = Vector3.one * 0.48f;

        var sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeBulletSprite;
        sr.sortingOrder = 5500;

        var col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;

        var rbBullet = bullet.AddComponent<Rigidbody2D>();
        rbBullet.gravityScale = 0f;
        rbBullet.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        return bullet;
    }

    // =========================================================================
    // HỆ THỐNG PHÂN THÂN 3 BOSS (3 MÀU KHÁC NHAU) Ở THANH MÁU CUỐI CÙNG
    // =========================================================================
    private void SpawnBossClones()
    {
        foreach (var old in activeClones)
        {
            if (old != null && !old.isDead) Destroy(old.gameObject);
        }
        activeClones.Clear();

        DamagePopup.CreateText(GetBossVisualCenter() + Vector3.up * 1.6f, LocalizationManager.T("TAM THỂ PHÂN THÂN", "TRIPLE CLONES"), new Color(1f, 0.3f, 0.3f));
        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        // 1. Trùm chính: Màu Đỏ Huyết Nguyệt (Blood Crimson)
        if (spriteRenderer != null)
        {
            originalColor = new Color(1f, 0.15f, 0.15f);
            spriteRenderer.color = originalColor;
        }

        // 2. Phân thân 1: Màu Tím Hư Không (Void Violet)
        CreateBossClone(new Vector3(-2.8f, 0.5f, 0f), new Color(0.72f, 0.22f, 1f), LocalizationManager.T("PHÂN THÂN HƯ KHÔNG", "VOID CLONE"));

        // 3. Phân thân 2: Màu Lam Lôi Đình (Storm Cyan)
        CreateBossClone(new Vector3(2.8f, -0.5f, 0f), new Color(0.12f, 0.85f, 1f), LocalizationManager.T("PHÂN THÂN LÔI ĐÌNH", "STORM CLONE"));
    }

    private void CreateBossClone(Vector3 offset, Color cloneColor, string cloneTitle)
    {
        GameObject cloneObj = Instantiate(gameObject, transform.position + offset, Quaternion.identity);
        cloneObj.name = $"BossClone_{activeClones.Count + 1}";

        var cloneAI = cloneObj.GetComponent<BotWheelAI>();
        if (cloneAI != null)
        {
            cloneAI.isBoss = true;
            cloneAI.isClone = true;
            cloneAI.mainBoss = this; // Sài chung 1 thanh máu với Trùm chính!
            cloneAI.totalBossPhases = 1;
            cloneAI.currentBossPhase = 1;
            cloneAI.maxHealth = phaseMaxHealth;
            cloneAI.phaseMaxHealth = phaseMaxHealth;
            cloneAI.currentHealth = currentHealth;
            cloneAI.moveSpeed = moveSpeed * 1.05f;
            cloneAI.attackCooldown = 1.35f;
            cloneAI.attackDamage = attackDamage;
            cloneAI.originalColor = cloneColor;
            if (cloneAI.spriteRenderer != null) cloneAI.spriteRenderer.color = cloneColor;
            cloneAI.isTransitioningPhase = false;
            cloneAI.isPurpleRingActive = false;
            cloneAI.activeClones = new List<BotWheelAI>();

            activeClones.Add(cloneAI);
            DamagePopup.CreateText(cloneAI.GetBossVisualCenter() + Vector3.up * 1.4f, cloneTitle, cloneColor);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? GetBodyFootPosition() : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
    }

    private void SetFacing(bool faceLeft)
    {
        if (spriteRenderer != null && spriteRenderer.flipX != faceLeft)
        {
            // Giữ nguyên tuyệt đối vị trí hiển thị của thân робоt trên màn hình khi lật mặt (bù trừ độ lệch tâm 1.136 unit của khung hình 117x26)
            Vector3 bodyFootBefore = GetBodyFootPosition();
            spriteRenderer.flipX = faceLeft;
            Vector3 bodyFootAfter = GetBodyFootPosition();
            Vector3 delta = bodyFootBefore - bodyFootAfter;
            transform.position += delta;
            if (rb != null)
            {
                rb.position += (Vector2)delta;
            }

            var colSync = GetComponent<DynamicSpriteColliderSync>();
            if (colSync != null)
            {
                colSync.SyncColliderWithSprite(force: true);
            }
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

    // ─── IEnemyHealth Implementation ───────────────────────
    public int CurrentHealth => currentHealth;
    public int MaxHealth => isBoss ? phaseMaxHealth : maxHealth;
    // IsBoss already defined at line 12
    public void MultiplyHealth(float mult)
    {
        maxHealth = Mathf.RoundToInt(maxHealth * mult);
        phaseMaxHealth = Mathf.RoundToInt(phaseMaxHealth * mult);
        currentHealth = Mathf.RoundToInt(currentHealth * mult);
    }

}
