using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BloodMonsterAI : MonoBehaviour, IDamageable
{
    public static event System.Action<BloodMonsterAI> OnEnemyDied;

    [Header("Monster Identity")]
    [SerializeField] public bool isBoss = false;
    public bool IsBoss => isBoss;

    [Header("Base Stats")]
    [SerializeField] public int maxHealth = 220;
    [SerializeField] public int currentHealth;
    [SerializeField] public int attackDamage = 22;
    [SerializeField] public float moveSpeed = 3.6f;
    [SerializeField] public float detectionRange = 10f;
    [SerializeField] public float meleeRange = 1.4f;
    [SerializeField] public float rangedAttackRange = 7.5f;
    [SerializeField] public float attackCooldown = 1.5f;
    [SerializeField] public float projectileSpeed = 8.5f;

    [Header("Boss 3-Phase Settings")]
    [SerializeField] public int totalBossPhases = 3;
    [SerializeField] public int currentBossPhase = 3; // 3: Normal -> 2: Enraged -> 1: Final Nightmare
    [SerializeField] public int phaseMaxHealth = 1100;

    // Cooldown timers
    private float attackTimer = 0f;
    private float pounceTimer = 0f;
    private float spikesTimer = 0f;
    private float crescentTimer = 0f;
    private float siphonTimer = 0f;
    private float blinkTimer = 0f;

    private int normalAttackCount = 0;
    private const int ATTACKS_BEFORE_SKILL = 2; // Tung 2 don thuong roi tung skill

    // State flags
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isKnockedBack = false;
    private bool isTransitioningPhase = false;
    private bool isStunned = false;

    // Component references
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D enemyCol;
    private Color originalColor;

    // Telegraph Warning Cone for Crescent Spray
    private GameObject warningZoneObj;
    private LineRenderer warningLine;

    // Blood Orb runtime sprite (red blood pixel art, no magenta)
    private static Sprite runtimeBloodOrbSprite;

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
        CreateWarningCone();
    }

    private void CreateWarningCone()
    {
        warningZoneObj = new GameObject("Boss_TelegraphCone");
        warningZoneObj.transform.SetParent(transform);
        warningZoneObj.transform.localPosition = Vector3.zero;

        warningLine = warningZoneObj.AddComponent<LineRenderer>();
        Shader unlitS = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        if (unlitS != null) warningLine.material = new Material(unlitS);

        warningLine.startColor = new Color(1f, 0.08f, 0.08f, 0.95f);
        warningLine.endColor   = new Color(1f, 0.35f, 0.05f, 0.95f);
        warningLine.startWidth = 0.14f;
        warningLine.endWidth   = 0.14f;
        warningLine.loop = true;
        warningLine.sortingOrder = 4900;
        warningZoneObj.SetActive(false);
    }

    private void DrawWarningCone(Vector3 origin, Vector2 aimDir, float length = 7.5f, float halfAngle = 26f)
    {
        if (warningZoneObj == null || warningLine == null) return;
        warningZoneObj.transform.SetParent(null);
        warningZoneObj.transform.position = Vector3.zero;
        warningZoneObj.SetActive(true);

        int arcSegments = 16;
        warningLine.positionCount = arcSegments + 2;

        float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - halfAngle;
        float endAngle   = baseAngle + halfAngle;

        warningLine.SetPosition(0, origin);

        for (int i = 0; i <= arcSegments; i++)
        {
            float curRad = Mathf.Lerp(startAngle, endAngle, i / (float)arcSegments) * Mathf.Deg2Rad;
            Vector3 pt = origin + new Vector3(Mathf.Cos(curRad), Mathf.Sin(curRad), 0) * length;
            warningLine.SetPosition(i + 1, pt);
        }

        StartCoroutine(FlashWarningConeRoutine());
    }

    private IEnumerator FlashWarningConeRoutine()
    {
        float elapsed = 0f;
        float duration = 0.75f;
        while (elapsed < duration && warningZoneObj != null && warningZoneObj.activeSelf)
        {
            float alpha = Mathf.Lerp(0.35f, 1.0f, Mathf.Sin(elapsed * 18f) * 0.5f + 0.5f);
            if (warningLine != null)
            {
                warningLine.startColor = new Color(1f, 0.05f, 0.05f, alpha);
                warningLine.endColor   = new Color(1f, 0.45f, 0.0f,  alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void HideWarningCone()
    {
        if (warningZoneObj != null)
        {
            warningZoneObj.SetActive(false);
            warningZoneObj.transform.SetParent(transform);
        }
    }

    private void CreateBloodOrbSprite()
    {
        if (runtimeBloodOrbSprite != null) return;
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = 7.5f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float d = Vector2.Distance(new Vector2(x,y), new Vector2(c,c));
                if (d < 2.5f) tex.SetPixel(x, y, new Color(1f, 0.92f, 0.82f, 1f));
                else if (d < 5.0f) tex.SetPixel(x, y, new Color(0.95f, 0.08f, 0.12f, 1f));
                else if (d < 7.0f) tex.SetPixel(x, y, new Color(0.55f, 0.02f, 0.06f, 0.85f));
                else if (d < 8.0f) tex.SetPixel(x, y, new Color(0.30f, 0f, 0.03f, 0.35f));
                else tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        runtimeBloodOrbSprite = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 32);
    }

    void Start()
    {
        FindPlayerTarget();
        if (isBoss)
        {
            BossHealthBarUI.EnsureInstance()?.ShowBoss("GOR'MOK - HUYET THU CO TU", totalBossPhases);
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
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (pounceTimer > 0f) pounceTimer -= Time.deltaTime;
        if (spikesTimer > 0f) spikesTimer -= Time.deltaTime;
        if (crescentTimer > 0f) crescentTimer -= Time.deltaTime;
        if (siphonTimer > 0f) siphonTimer -= Time.deltaTime;
        if (blinkTimer > 0f) blinkTimer -= Time.deltaTime;

        if (player == null) { FindPlayerTarget(); SetMoving(false); return; }
        if (isKnockedBack || isTransitioningPhase || isAttacking || isStunned) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) { rb.linearVelocity = Vector2.zero; SetMoving(false); return; }

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (spriteRenderer != null)
        {
            if (dir.x > 0.05f) spriteRenderer.flipX = false;
            else if (dir.x < -0.05f) spriteRenderer.flipX = true;
        }

        // 1. BOSS DICH CHUYEN SAU LUNG KHI PLAYER CHAY RA QUA XA (dist > 5.5m)
        if (isBoss && dist > 5.5f && blinkTimer <= 0f && attackTimer <= 0f)
        {
            StartCoroutine(PerformShadowBlinkBehindPlayerRoutine());
            return;
        }

        // 2. BOSS PHASE CUOI (FINAL PHASE): Hut mau lien tuc cooldown 0.1s
        if (isBoss && currentBossPhase == 1 && siphonTimer <= 0f && dist <= 7.0f && attackTimer <= 0f)
        {
            StartCoroutine(PerformSanguineSiphonRoutine());
            return;
        }

        // 3. BOSS CHON SKILL THEO COMBO
        if (isBoss && normalAttackCount >= ATTACKS_BEFORE_SKILL && TryBossPhaseSkill(dist, dir)) return;

        // 4. CAN CHIEN MELEE
        if (dist <= meleeRange)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);
            if (attackTimer <= 0f)
            {
                normalAttackCount++;
                if (isBoss) StartCoroutine(PerformBossSlamRoutine());
                else StartCoroutine(PerformMeleeAttackRoutine());
            }
            return;
        }

        // 5. BAN TAM TRUNG / MINION
        if (!isBoss && dist <= rangedAttackRange && attackTimer <= 0f)
        {
            StartCoroutine(PerformRangedAttackRoutine());
            return;
        }

        if (isBoss && dist > meleeRange * 1.5f && dist <= rangedAttackRange * 0.65f && attackTimer <= 0f)
        {
            normalAttackCount++;
            StartCoroutine(PerformRangedAttackRoutine());
            return;
        }

        // 6. DUOI THEO PLAYER
        rb.linearVelocity = dir * (isBoss ? moveSpeed * 1.15f : moveSpeed);
        SetMoving(true);
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
            float padding = isBoss ? 1.0f : 0.6f;
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

    private bool TryBossPhaseSkill(float distance, Vector2 dir)
    {
        if (attackTimer > 0f) return false;
        bool used = false;

        // Phase 1 (Final): Uu tien Hut Mau va Nhay Vo
        if (currentBossPhase == 1)
        {
            if (siphonTimer <= 0f && distance <= 7.0f)
            {
                StartCoroutine(PerformSanguineSiphonRoutine());
                used = true;
            }
            else if (crescentTimer <= 0f && distance <= 8.5f)
            {
                StartCoroutine(PerformCrescentWavesRoutine());
                used = true;
            }
            else if (pounceTimer <= 0f && distance >= 2.0f)
            {
                StartCoroutine(PerformCrimsonPounceRoutine(dir));
                used = true;
            }
        }
        // Phase 2 (Enraged): Nguyet Tram Hinh Non & Huyết Đột Kích
        else if (currentBossPhase == 2)
        {
            if (crescentTimer <= 0f && distance <= 8.0f)
            {
                StartCoroutine(PerformCrescentWavesRoutine());
                used = true;
            }
            else if (pounceTimer <= 0f && distance >= 2.2f)
            {
                StartCoroutine(PerformCrimsonPounceRoutine(dir));
                used = true;
            }
            else if (siphonTimer <= 0f && distance <= 5.5f)
            {
                StartCoroutine(PerformSanguineSiphonRoutine());
                used = true;
            }
        }
        // Phase 3 (Normal): Nguyet Tram & Pounce
        else
        {
            if (crescentTimer <= 0f && distance <= 7.5f)
            {
                StartCoroutine(PerformCrescentWavesRoutine());
                used = true;
            }
            else if (pounceTimer <= 0f && distance >= 2.5f)
            {
                StartCoroutine(PerformCrimsonPounceRoutine(dir));
                used = true;
            }
        }

        if (used)
        {
            normalAttackCount = 0;
            return true;
        }
        return false;
    }

    // ===== CHIÊU 1: DỊCH CHUYỂN AN TOÀN (SHADOW BLINK) =====
    private IEnumerator PerformShadowBlinkBehindPlayerRoutine()
    {
        isAttacking = true;
        blinkTimer = (currentBossPhase == 1) ? 2.5f : 4.5f;
        attackTimer = 0.5f;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        // Hieu ung bien mat tai vi tri cu
        SpawnBlinkSmoke(transform.position);
        SoundManager.EnsureInstance()?.PlayBossWarning();
        DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, LocalizationManager.T("HUYẾT ẢNH BIẾN THỂ!", "BLOOD PHANTOM!"), new Color(1f, 0.15f, 0.15f));

        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0f, 0f, 0.2f);
        yield return new WaitForSeconds(0.2f);

        if (isDead) { isAttacking = false; yield break; }

        if (player != null)
        {
            Vector2 playerFacing = Vector2.down;
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) playerFacing = pc.GetFacingDirection();
            if (playerFacing == Vector2.zero) playerFacing = Vector2.down;

            // Tìm vị trí dịch chuyển an toàn tuyệt đối, không đâm xuyên tường hay rơi ra ngoài Map
            Vector3 safeBlinkPos = FindSafeBlinkPosition(player.position, playerFacing);
            transform.position = safeBlinkPos;

            // Hieu ung xuat hien tai vi tri moi
            SpawnBlinkSmoke(transform.position);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;

            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.35f, 0.25f);

            DamagePopup.CreateText(transform.position + Vector3.up * 1.6f, LocalizationManager.T("DỊCH CHUYỂN SAU LƯNG!", "SHADOW STEP!"), new Color(1f, 0.2f, 0.2f));
            SoundManager.EnsureInstance()?.PlayHitImpact();

            yield return new WaitForSeconds(0.15f);

            // Lap tuc tung don tan cong hoac hut mau neu phase cuoi
            if (currentBossPhase == 1)
            {
                isAttacking = false;
                StartCoroutine(PerformSanguineSiphonRoutine());
                yield break;
            }
            else
            {
                StartCoroutine(PerformBossSlamRoutine());
                yield break;
            }
        }

        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        isAttacking = false;
    }

    private Vector3 FindSafeBlinkPosition(Vector3 playerPos, Vector2 playerFacing)
    {
        Vector2[] testOffsets = new Vector2[]
        {
            -playerFacing.normalized * 1.25f,
            Vector2.Perpendicular(playerFacing.normalized) * 1.25f,
            -Vector2.Perpendicular(playerFacing.normalized) * 1.25f,
            playerFacing.normalized * 1.25f
        };

        var roomMgr = DungeonRoomManager.Instance;
        Rect roomRect = (roomMgr != null && roomMgr.currentRoom != null && roomMgr.currentRoom.roomBounds.width > 0)
            ? roomMgr.currentRoom.roomBounds
            : new Rect(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

        float padding = 1.0f;

        foreach (var offset in testOffsets)
        {
            Vector3 candidate = playerPos + (Vector3)offset;

            // Kiểm tra va chạm với tường và nằm trọn vẹn trong phòng
            Collider2D hitWall = Physics2D.OverlapCircle(candidate, 0.6f, LayerMask.GetMask("Default", "Wall", "Obstacle", "Environment"));
            bool isInsideRoom = (candidate.x >= roomRect.xMin + padding && candidate.x <= roomRect.xMax - padding &&
                                 candidate.y >= roomRect.yMin + padding && candidate.y <= roomRect.yMax - padding);

            if (hitWall == null && isInsideRoom)
            {
                return candidate;
            }
        }

        // Nếu sát tường, dịch chuyển theo hướng về tâm phòng
        if (roomMgr != null && roomMgr.currentRoom != null && roomMgr.currentRoom.roomBounds.width > 0)
        {
            Vector3 toCenter = (roomMgr.currentRoom.roomCenter - playerPos).normalized;
            Vector3 safePos = playerPos + toCenter * 0.95f;
            float x = Mathf.Clamp(safePos.x, roomRect.xMin + padding, roomRect.xMax - padding);
            float y = Mathf.Clamp(safePos.y, roomRect.yMin + padding, roomRect.yMax - padding);
            return new Vector3(x, y, playerPos.z);
        }

        return playerPos + Vector3.up * 0.5f;
    }

    private void SpawnBlinkSmoke(Vector3 pos)
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject smoke = new GameObject("BlinkSmokeFX");
            smoke.transform.position = pos + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.3f, 0.3f), 0);
            smoke.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);
            var sr = smoke.AddComponent<SpriteRenderer>();
            sr.sprite = runtimeBloodOrbSprite;
            sr.color = new Color(Random.Range(0.85f, 1f), 0.05f, 0.05f, 0.85f);
            sr.sortingOrder = 5200;
            Destroy(smoke, 0.25f);
        }
    }

    // ===== CHIÊU 2: PHUN ĐẠN HÌNH NÓN KÈM VÙNG CẢNH BÁO HÌNH NÓN (CRESCENT BARRAGE) =====
    private IEnumerator PerformCrescentWavesRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown + 1.2f;
        crescentTimer = (currentBossPhase == 1) ? 3.0f : 5.5f;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        if (player == null) { isAttacking = false; yield break; }

        Vector2 aimDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (aimDir == Vector2.zero) aimDir = Vector2.right;

        // Ve VUNG CANH BAO HINH NON chuan theo huong ban ve phia player
        DrawWarningCone(transform.position + Vector3.up * 0.3f, aimDir, 7.5f, 26f);
        SoundManager.EnsureInstance()?.PlayBossWarning();
        DamagePopup.CreateText(transform.position + Vector3.up * 1.6f, LocalizationManager.T("HUYẾT NGUYỆT TRẢM!", "BLOOD MOON SLASH!"), new Color(1f, 0.1f, 0.2f));

        // Canh bao nhap nhay 0.75s
        yield return new WaitForSeconds(0.75f);
        HideWarningCone();

        if (isDead) { isAttacking = false; yield break; }

        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(SpecialAttackHash);
        yield return new WaitForSeconds(0.15f);

        // Ban 3-4 dot dan hinh non toa deu theo goc
        float[] spreadAngles = { -22f, -11f, 0f, 11f, 22f };
        int waves = (currentBossPhase == 1) ? 4 : 3;

        for (int w = 0; w < waves; w++)
        {
            if (isDead) break;
            if (player != null)
            {
                Vector2 playerCenter = (Vector2)player.position + Vector2.up * 0.4f;
                Vector2 monsterCenter = (Vector2)transform.position + Vector2.up * 0.3f;
                Vector2 curAim = (playerCenter - monsterCenter).normalized;
                float distToPlayer = Vector2.Distance(monsterCenter, playerCenter);
                float spawnDist = Mathf.Min(0.35f, distToPlayer * 0.35f);
                Vector3 sp = (Vector3)monsterCenter + (Vector3)(curAim * spawnDist);

                foreach (float a in spreadAngles)
                {
                    SpawnBloodProjectile(sp, Quaternion.Euler(0, 0, a) * curAim);
                }
                SoundManager.EnsureInstance()?.PlayEnemyShoot();
            }
            yield return new WaitForSeconds(0.24f);
        }

        var cr = FindAnyObjectByType<CameraFollow>();
        if (cr != null) cr.Shake(0.35f, 0.3f);
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    // ===== CHIÊU 3: HUYẾT TRẬN HÚT MÁU + GÂY CHOÁNG (SANGUINE SIPHON) =====
    private IEnumerator PerformSanguineSiphonRoutine()
    {
        isAttacking = true;
        // Phase cuoi cooldown 0.1s theo yeu cau!
        siphonTimer = (currentBossPhase == 1) ? 0.1f : 6.0f;
        attackTimer = (currentBossPhase == 1) ? 0.8f : 1.6f;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(SpecialAttackHash);
        DamagePopup.CreateText(transform.position + Vector3.up * 1.8f, LocalizationManager.T("HUYẾT TRẬN TRUY HỒN!", "SOUL HUNT ARRAY!"), new Color(1f, 0.05f, 0.05f));
        SoundManager.EnsureInstance()?.PlayBossWarning();

        if (player != null)
        {
            // GAY CHOANG PLAYER TRONG SUOT THOI GIAN HUT MAU
            float siphonDuration = (currentBossPhase == 1) ? 0.9f : 1.1f;
            StartCoroutine(StunPlayerRoutine(player, siphonDuration));
            DamagePopup.CreateText(player.position + Vector3.up * 1.4f, LocalizationManager.T("KHÓA HỒN!", "SOUL LOCK!"), new Color(1f, 0.85f, 0f));

            int ticks = (currentBossPhase == 1) ? 4 : 3;
            float tickInterval = siphonDuration / (float)ticks;

            for (int t = 0; t < ticks; t++)
            {
                if (isDead || player == null) break;

                var dmg = player.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    int sd = attackDamage + (currentBossPhase == 1 ? 6 : 4);
                    dmg.TakeDamage(sd);
                    DamagePopup.Create(player.position + Vector3.up * 0.8f, sd, isPlayer: true);

                    // Boss hoi phuc mau
                    int healAmt = Mathf.RoundToInt(phaseMaxHealth * 0.045f);
                    currentHealth = Mathf.Min(currentHealth + healAmt, phaseMaxHealth);
                    DamagePopup.Create(transform.position + Vector3.up * 1.0f, healAmt, isPlayer: false);
                    (BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance())?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);

                    SoundManager.EnsureInstance()?.PlayHitImpact();
                    var sc = FindAnyObjectByType<CameraFollow>();
                    if (sc != null) sc.Shake(0.25f, 0.18f);

                    // Sinh tia mau chay tu nguoi choi ve Boss
                    SpawnSiphonParticles(player.position, transform.position);
                }

                yield return new WaitForSeconds(tickInterval);
            }
        }

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    private void SpawnSiphonParticles(Vector3 from, Vector3 to)
    {
        for (int i = 0; i < 7; i++)
        {
            Vector3 at = from + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.3f, 0.3f), 0);
            Vector2 dir = (to - at).normalized;
            GameObject p = new GameObject("SiphonFX");
            p.transform.position = at;
            p.transform.localScale = Vector3.one * Random.Range(0.20f, 0.35f);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = runtimeBloodOrbSprite;
            sr.color = new Color(Random.Range(0.9f, 1f), 0.02f, 0.05f, 0.95f);
            sr.sortingOrder = 5100;
            var rbfx = p.AddComponent<Rigidbody2D>();
            rbfx.gravityScale = 0f;
            rbfx.linearVelocity = dir * Random.Range(8.5f, 13f);
            Destroy(p, 0.35f);
        }
    }

    // ===== CHIÊU 4: HUYẾT ĐỘT KÍCH (CRIMSON POUNCE) =====
    private IEnumerator PerformCrimsonPounceRoutine(Vector2 pounceDir)
    {
        isAttacking = true;
        attackTimer = attackCooldown + 0.6f;
        pounceTimer = (currentBossPhase == 1) ? 2.5f : 4.5f;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        DamagePopup.CreateText(transform.position + Vector3.up * 1.6f, LocalizationManager.T("HUYẾT ĐỘT KÍCH!", "BLOOD POUNCE!"), new Color(1f, 0.1f, 0.1f));
        SoundManager.EnsureInstance()?.PlayBossWarning();

        yield return new WaitForSeconds(0.35f);
        if (isDead) { isAttacking = false; yield break; }

        if (player != null)
        {
            Vector2 finalDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(AttackHash);

            StartCoroutine(SpawnDashTrail());

            rb.linearVelocity = finalDir * moveSpeed * 6.0f;
            yield return new WaitForSeconds(0.22f);
            rb.linearVelocity = Vector2.zero;

            if (Vector2.Distance(transform.position, player.position) <= meleeRange + 1.6f)
            {
                var dmg = player.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    int pounceDmg = attackDamage + 10;
                    dmg.TakeDamage(pounceDmg);
                    DamagePopup.Create(player.position + Vector3.up * 0.9f, pounceDmg, isPlayer: true);
                    SoundManager.EnsureInstance()?.PlayHitImpact();

                    var cam = FindAnyObjectByType<CameraFollow>();
                    if (cam != null) cam.Shake(0.45f, 0.35f);

                    // Hat tung + choang nhe
                    var playerRb = player.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockDir = (finalDir + Vector2.up * 0.35f).normalized;
                        playerRb.linearVelocity = Vector2.zero;
                        playerRb.AddForce(knockDir * 18f, ForceMode2D.Impulse);
                    }
                    StartCoroutine(StunPlayerRoutine(player, 0.65f));
                    DamagePopup.CreateText(player.position + Vector3.up * 1.3f, LocalizationManager.T("CHOÁNG!", "STUNNED!"), new Color(1f, 0.85f, 0f));
                }
            }
        }
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    private IEnumerator SpawnDashTrail()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject trail = new GameObject("DashTrailFX");
            trail.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.1f, 0.1f), 0);
            trail.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
            var sr = trail.AddComponent<SpriteRenderer>();
            sr.sprite = runtimeBloodOrbSprite;
            sr.color = new Color(Random.Range(0.85f, 1f), 0.04f, 0.04f, 0.82f);
            sr.sortingOrder = 4800;
            Destroy(trail, 0.16f);
            yield return new WaitForSeconds(0.03f);
        }
    }

    // Choáng Player: Khóa di chuyển và hiển thị sao choáng
    private IEnumerator StunPlayerRoutine(Transform playerTransform, float duration)
    {
        if (playerTransform == null) yield break;
        var pCtrl = playerTransform.GetComponent<PlayerController>();
        if (pCtrl != null)
        {
            pCtrl.SetCanMove(false);
            pCtrl.enabled = false;
        }

        var pRb = playerTransform.GetComponent<Rigidbody2D>();
        if (pRb != null) pRb.linearVelocity = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < duration && playerTransform != null)
        {
            if (elapsed % 0.18f < Time.deltaTime)
            {
                float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 starPos = playerTransform.position + new Vector3(Mathf.Cos(ang) * 0.45f, Mathf.Sin(ang) * 0.45f + 0.85f, 0);
                DamagePopup.CreateText(starPos, "*", new Color(1f, 0.9f, 0.15f));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (pCtrl != null)
        {
            pCtrl.enabled = true;
            pCtrl.SetCanMove(true);
        }
    }

    // ===== ĐÒN ĐÁNH THƯỜNG / MELEE =====
    private void SetMoving(bool moving)
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetBool(IsMovingHash, moving);
    }

    private IEnumerator PerformMeleeAttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(AttackHash);
        yield return new WaitForSeconds(0.25f);
        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeRange + 0.5f)
        {
            var dmg = player.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(attackDamage);
                DamagePopup.Create(player.position + Vector3.up * 0.8f, attackDamage, isPlayer: true);
                SoundManager.EnsureInstance()?.PlayHitImpact();
            }
        }
        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
    }

    private IEnumerator PerformBossSlamRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown * 0.8f;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(AttackHash);
        yield return new WaitForSeconds(0.22f);
        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeRange + 0.8f)
        {
            var dmg = player.GetComponent<IDamageable>();
            if (dmg != null)
            {
                int slamDamage = attackDamage + 4;
                dmg.TakeDamage(slamDamage);
                DamagePopup.Create(player.position + Vector3.up * 0.8f, slamDamage, isPlayer: true);
                SoundManager.EnsureInstance()?.PlayHitImpact();
                var sc = FindAnyObjectByType<CameraFollow>();
                if (sc != null) sc.Shake(0.35f, 0.25f);
            }
        }
        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
    }

    private IEnumerator PerformRangedAttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(AttackHash);
        yield return new WaitForSeconds(0.2f);
        if (player != null)
        {
            Vector2 playerCenter = (Vector2)player.position + Vector2.up * 1.0f;
            Vector2 monsterCenter = (Vector2)transform.position + Vector2.up * 0.5f;
            Vector2 aimDir = (playerCenter - monsterCenter).normalized;
            float distToPlayer = Vector2.Distance(monsterCenter, playerCenter);
            float spawnDist = Mathf.Min(0.35f, distToPlayer * 0.35f);
            Vector3 sp = (Vector3)monsterCenter + (Vector3)(aimDir * spawnDist);

            if (isBoss)
            {
                float[] angs = { -16f, 0f, 16f };
                foreach (float a in angs) SpawnBloodProjectile(sp, Quaternion.Euler(0,0,a) * aimDir);
            }
            else SpawnBloodProjectile(sp, aimDir);
            SoundManager.EnsureInstance()?.PlayEnemyShoot();
        }
        yield return new WaitForSeconds(0.35f);
        isAttacking = false;
    }

    private void SpawnBloodProjectile(Vector3 pos, Vector2 dir)
    {
        GameObject proj = new GameObject("BloodMonster_Projectile");
        proj.transform.position = pos;
        proj.transform.localScale = isBoss ? Vector3.one * 0.58f : Vector3.one * 0.45f;

        var sr = proj.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeBloodOrbSprite;
        sr.sortingOrder = 5500;

        var col = proj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        var rbP = proj.AddComponent<Rigidbody2D>();
        rbP.gravityScale = 0f;
        rbP.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var ep = proj.AddComponent<EnemyProjectile>();
        ep.Initialize(dir, isBoss ? attackDamage : attackDamage - 2, projectileSpeed);
    }

    // ===== DAMAGE / PHASE / DEATH =====
    public void TakeDamage(int damage)
    {
        if (isDead || isTransitioningPhase) return;
        currentHealth -= damage;
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.1f, 0.18f);

        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(HurtHash);
        if (player != null && !isBoss) StartCoroutine(KnockbackRoutine(((Vector2)transform.position - (Vector2)player.position).normalized));
        if (spriteRenderer != null) StartCoroutine(FlashRedRoutine());

        if (isBoss) (BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance())?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);

        if (currentHealth <= 0)
        {
            if (isBoss && currentBossPhase > 1) StartCoroutine(BossPhaseTransitionRoutine());
            else
            {
                if (isBoss) (BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance())?.HideBoss();
                Die();
            }
        }
    }

    private IEnumerator BossPhaseTransitionRoutine()
    {
        isTransitioningPhase = true;
        currentBossPhase--;
        currentHealth = phaseMaxHealth;
        normalAttackCount = 0;
        pounceTimer = 0f;
        spikesTimer = 0f;
        crescentTimer = 0f;
        siphonTimer = 0f;
        blinkTimer = 0f;
        attackTimer = 0f;

        rb.linearVelocity = Vector2.zero;
        SetMoving(false);
        HideWarningCone();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.55f, 0.65f);
        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        string title = currentBossPhase == 2 
            ? LocalizationManager.T("GIAI ĐOẠN II: HUYẾT THẦN THỨC TỈNH!", "PHASE II: BLOOD GOD AWAKENING!") 
            : LocalizationManager.T("GIAI ĐOẠN CUỐI: TẬN THẾ HUYẾT NGUYỆT!", "FINAL PHASE: BLOOD MOON ECLIPSE!");
        DamagePopup.CreateText(transform.position + Vector3.up * 2.0f, title, new Color(1f, 0.15f, 0.15f));

        moveSpeed *= 1.22f;
        attackDamage += 5;
        attackCooldown = Mathf.Max(0.7f, attackCooldown * 0.8f);

        if (spriteRenderer != null)
        {
            originalColor = currentBossPhase == 2 ? new Color(1f, 0.42f, 0.42f) : new Color(1f, 0.15f, 0.15f);
            spriteRenderer.color = originalColor;
        }

        (BossHealthBarUI.Instance ?? BossHealthBarUI.EnsureInstance())?.UpdateHealth(currentHealth, phaseMaxHealth, currentBossPhase);
        yield return new WaitForSeconds(1.1f);
        isTransitioningPhase = false;
    }

    private IEnumerator KnockbackRoutine(Vector2 dir)
    {
        isKnockedBack = true;
        rb.linearVelocity = dir * 5.5f;
        yield return new WaitForSeconds(0.13f);
        isKnockedBack = false;
    }

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        SetMoving(false);
        HideWarningCone();

        Debug.Log($"<color=red>[BLOOD MONSTER SLAIN]</color> {(isBoss ? "Dai Trum Gor'Mok" : "Huyet Thu")} da guc nga!");
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(DieHash);
        if (isBoss) SoundManager.EnsureInstance()?.PlayBossDeath();
        else SoundManager.EnsureInstance()?.PlayHitImpact();

        OnEnemyDied?.Invoke(this);

        if (isBoss)
        {
            AethelShard.SpawnShards(transform.position, Random.Range(25, 35));
            HealthPotion.Spawn(transform.position + Vector3.left * 0.8f, 1);
            HealthPotion.Spawn(transform.position + Vector3.right * 0.8f, 1);
            WorldItemPickup.RollDrop(transform.position, true);
        }
        else
        {
            AethelShard.SpawnShards(transform.position, Random.Range(4, 8));
            WorldItemPickup.RollDrop(transform.position, false, 0.1f);
        }

        var co = GetComponent<Collider2D>();
        if (co != null) co.enabled = false;
        Destroy(gameObject, 0.85f);
    }

    void OnDestroy()
    {
        if (warningZoneObj != null) Destroy(warningZoneObj);
    }

    // ===== SETUP / FACTORY =====
    public void SetupBoss(float scaleMultiplier = 1.85f, int damageMultiplier = 2)
    {
        isBoss = true;
        transform.localScale = Vector3.one * scaleMultiplier;
        totalBossPhases = 3;
        currentBossPhase = 3;
        phaseMaxHealth = 1100;
        currentHealth = phaseMaxHealth;

        int el = LayerMask.NameToLayer("Enemy");
        if (el != -1) gameObject.layer = el;
        gameObject.tag = "Enemy";

        var cap = GetComponent<CapsuleCollider2D>();
        if (cap == null) cap = gameObject.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.5f, 1.4f);
        cap.direction = CapsuleDirection2D.Vertical;
        cap.isTrigger = false;
        enemyCol = cap;

        attackDamage *= damageMultiplier;
        moveSpeed *= 1.15f;
        projectileSpeed *= 1.25f;
        meleeRange = 2.4f;
        rangedAttackRange = 8.5f;
        detectionRange = 14.0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.32f, 0.32f);
            originalColor = spriteRenderer.color;
        }

        BossHealthBarUI.EnsureInstance()?.ShowBoss("GOR'MOK - HUYET THU CO TU", totalBossPhases);
    }

    public static BloodMonsterAI Spawn(Vector3 pos, bool isBoss = false, int index = 1)
    {
        GameObject go = new GameObject(isBoss ? "Boss_GorMok_BloodMonster" : $"BloodMonster_{index}");
        go.transform.position = pos;
        go.transform.localScale = isBoss ? new Vector3(1.85f, 1.85f, 1f) : new Vector3(1.2f, 1.2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadBloodMonsterSprite();
        sr.sortingOrder = 10;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = LoadBloodMonsterAnimator();

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = isBoss ? new Vector2(1.5f, 1.4f) : new Vector2(1.0f, 1.0f);
        col.direction = CapsuleDirection2D.Vertical;

        var po = Object.FindAnyObjectByType<PlayerController>();
        if (po != null)
        {
            foreach (var pc in po.GetComponentsInChildren<Collider2D>())
            {
                if (pc != null && col != null) Physics2D.IgnoreCollision(pc, col, true);
            }
        }

        var ai = go.AddComponent<BloodMonsterAI>();
        if (isBoss) ai.SetupBoss(1.85f, 2);
        else
        {
            ai.isBoss = false;
            ai.maxHealth = 220;
            ai.currentHealth = 220;
            ai.attackDamage = 22;
            ai.moveSpeed = 3.6f;
        }

        return ai;
    }

    public static Sprite LoadBloodMonsterSprite()
    {
        Sprite s = null;
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Blood Monster_A with shadows/Blood Monster_A.png");
        if (all != null)
        {
            foreach (var a in all)
            {
                if (a is Sprite sp) { s = sp; break; }
            }
        }
#endif
        return s;
    }

    public static RuntimeAnimatorController LoadBloodMonsterAnimator()
    {
        RuntimeAnimatorController a = null;
#if UNITY_EDITOR
        a = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/BloodMonster/BloodMonster.controller");
#endif
        return a;
    }
}
