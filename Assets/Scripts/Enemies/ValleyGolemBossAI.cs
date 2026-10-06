using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Dai Boss Map 2 (Ancient Valley Golem):
/// Cu Thach Ho Ve Thung Lung Tan Tich.
/// Co co che vung canh bao mau do (Red Warning Zone) kem am thanh canh bao truoc dung 2 giay (2.0s) cho moi tuyet ky!
/// Co ca don dap dat rung chan man hinh va don ban chum dan bay 360 do.
/// </summary>
public class ValleyGolemBossAI : MonoBehaviour, IDamageable
{
    public static event System.Action<ValleyGolemBossAI> OnEnemyDied;

    [Header("Boss Stats")]
    [SerializeField] private int maxHealth = 1400;
    [SerializeField] private int currentHealth;
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float skillCooldown = 4.5f;

    private Transform player;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D myCol;

    private float lastSkillTime = -10f;
    private bool isDead = false;
    private bool isCasting = false;
    private int attackCycle = 0;

    // Visual warning indicators
    private GameObject redCircleIndicator;
    private LineRenderer circleLine;

    void Awake()
    {
        try { gameObject.tag = "Enemy"; } catch { }
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5000;

#if UNITY_EDITOR
        if (sr.sprite == null)
        {
            sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_63_79x89.png"
            );
        }
#endif

        // Boss khong lo (scale 1.8x)
        transform.localScale = Vector3.one * 1.8f;

        myCol = GetComponent<Collider2D>();
        if (myCol == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.5f, 1.8f);
            box.offset = new Vector2(0, 0);
            myCol = box;
        }

        currentHealth = maxHealth;
        CreateWarningCircle();
    }

    void Start()
    {
        FindPlayer();
        IgnorePlayerCollision();

        // Thong bao Boss tren UI
        var waveUI = FindAnyObjectByType<WaveUI>();
        if (waveUI != null)
        {
            waveUI.ShowBanner("BOSS INCOMING: ANCIENT GOLEM!", LocalizationManager.T("Cự Thạch Hộ Vệ Thung Lũng Tàn Tích", "Valley Ruins Ancient Stone Golem"));
        }
    }

    void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isCasting)
        {
            // Di chuyen cham chap ap sat player
            if (dist > 2.5f)
            {
                Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            // Kich hoat tuyet ky theo chu ky
            if (Time.time - lastSkillTime >= skillCooldown)
            {
                lastSkillTime = Time.time;
                attackCycle++;
                if (attackCycle % 2 == 1)
                {
                    StartCoroutine(EarthShatterRoutine());
                }
                else
                {
                    StartCoroutine(StoneBarrageRoutine());
                }
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
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
            float padding = 1.2f;
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

    private void CreateWarningCircle()
    {
        redCircleIndicator = new GameObject("Boss_RedWarningZone");
        redCircleIndicator.transform.SetParent(transform);
        redCircleIndicator.transform.localPosition = Vector3.zero;

        circleLine = redCircleIndicator.AddComponent<LineRenderer>();
        Shader unlitS = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"); if (unlitS != null) circleLine.material = new Material(unlitS);
        circleLine.startColor = new Color(1f, 0.1f, 0.1f, 0.85f);
        circleLine.endColor = new Color(1f, 0.25f, 0.1f, 0.85f);
        circleLine.startWidth = 0.12f;
        circleLine.endWidth = 0.12f;
        circleLine.loop = true;
        circleLine.sortingOrder = 4800;

        int segments = 40;
        circleLine.positionCount = segments;
        redCircleIndicator.SetActive(false);
    }

    private void DrawWarningCircle(Vector3 center, float radius)
    {
        redCircleIndicator.transform.SetParent(null);
        redCircleIndicator.transform.position = center;
        redCircleIndicator.SetActive(true);

        int segments = 40;
        for (int i = 0; i < segments; i++)
        {
            float rad = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0);
            circleLine.SetPosition(i, pos);
        }
    }

    /// <summary>
    /// Chieu 1: Dap dat rung chan man hinh - Canh bao do truoc dung 2 giay!
    /// </summary>
    private IEnumerator EarthShatterRoutine()
    {
        isCasting = true;

        Vector3 targetPos = (player != null) ? player.position : transform.position;
        float radius = 4.2f;

        // 1. Bat am thanh va hien vung do canh bao 2 giay
        SoundManager.EnsureInstance()?.PlayBossWarning();
        DrawWarningCircle(targetPos, radius);

        float elapsed = 0f;
        float warningDuration = 2.0f; // Dung 2 giay theo yeu cau

        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            // Nhap nhay vung canh bao do
            float alpha = 0.4f + Mathf.PingPong(elapsed * 5f, 0.6f);
            circleLine.startColor = new Color(1f, 0.1f, 0.1f, alpha);
            circleLine.endColor = new Color(1f, 0.3f, 0.1f, alpha);

            // Boss tich tu nang luong bay nhe len
            transform.position += new Vector3(0, Mathf.Sin(elapsed * 6f) * 0.005f, 0);

            yield return null;
        }

        redCircleIndicator.SetActive(false);
        redCircleIndicator.transform.SetParent(transform);

        // 2. Boss dap xuong vi tri muc tieu!
        transform.position = targetPos;

        // Rung camera cuc manh
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.6f, 0.7f);

        SoundManager.EnsureInstance()?.PlayHitImpact();
        BreakableDebrisFX.SpawnPotShatter(targetPos);

        // Gay sat thuong neu nguoi choi con trong vung
        if (player != null && Vector2.Distance(player.position, targetPos) <= radius)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(45);
                SoundManager.EnsureInstance()?.PlayPlayerHurt();
            }
        }

        yield return new WaitForSeconds(0.6f);
        isCasting = false;
    }

    /// <summary>
    /// Chieu 2: Ban bao dan da 360 do - Canh bao do truoc dung 2 giay!
    /// </summary>
    private IEnumerator StoneBarrageRoutine()
    {
        isCasting = true;

        SoundManager.EnsureInstance()?.PlayBossWarning();

        // 1. Hien thi vung canh bao 360 do trong 2 giay
        DrawWarningCircle(transform.position, 2.5f);

        float elapsed = 0f;
        float warningDuration = 2.0f; // Dung 2 giay theo yeu cau

        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 6f, 1f);
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), pulse);
            yield return null;
        }

        redCircleIndicator.SetActive(false);
        redCircleIndicator.transform.SetParent(transform);
        sr.color = Color.white;

        // 2. Ban toa 12 vien dan bay khap 360 do!
        SoundManager.EnsureInstance()?.PlayEnemyShoot();

        int projectileCount = 12;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (i / (float)projectileCount) * 360f;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            SpawnBossProjectile(transform.position, dir);
        }

        yield return new WaitForSeconds(0.8f);
        isCasting = false;
    }

    private void SpawnBossProjectile(Vector3 origin, Vector2 dir)
    {
        GameObject proj = new GameObject("Boss_Stone_Projectile");
        proj.transform.position = origin + (Vector3)(dir * 0.8f);

        float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0, 0, rotZ);

        var srProj = proj.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("Combat/enemy_projectile");
#if UNITY_EDITOR
        if (sprite == null)
        {
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Combat/enemy_projectile_runtime.png");
        }
#endif
        srProj.sprite = sprite;
        srProj.color = new Color(1f, 0.65f, 0.2f, 1f); // Dan da thung lung phat sang
        srProj.sortingOrder = 5200;

        var col = proj.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        var script = proj.AddComponent<ValleyThornProjectile>();
        script.Init(dir * 6.5f, 12);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        StartCoroutine(HitFlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        Color original = sr.color;
        sr.color = new Color(1f, 0.35f, 0.35f, 1f);
        yield return new WaitForSeconds(0.12f);
        sr.color = original;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (redCircleIndicator != null) Destroy(redCircleIndicator);
        if (myCol != null) myCol.enabled = false;
        rb.linearVelocity = Vector2.zero;

        SoundManager.EnsureInstance()?.PlayBossDeath();

        // Roi mua Tinh The Xanh, Binh Mau va Da Quy Cuc Pham
        AethelShard.SpawnShards(transform.position, 20);
        HealthPotion.Spawn(transform.position + new Vector3(-0.6f, 0, 0), 1);
        HealthPotion.Spawn(transform.position + new Vector3(0.6f, 0, 0), 1);
        WorldItemPickup.RollDrop(transform.position, true);

        OnEnemyDied?.Invoke(this);

        StartCoroutine(BossDeathRoutine());
    }

    private IEnumerator BossDeathRoutine()
    {
        Time.timeScale = 0.25f;
        yield return new WaitForSecondsRealtime(0.8f);
        Time.timeScale = 1.0f;

        // Hieu ung tan vo
        BreakableDebrisFX.SpawnPotShatter(transform.position);

        Destroy(gameObject);
    }

    private void FindPlayer()
    {
        var p = Object.FindAnyObjectByType<PlayerController>();
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    private void IgnorePlayerCollision()
    {
        if (player != null && myCol != null)
        {
            var pCols = player.GetComponentsInChildren<Collider2D>();
            foreach (var pCol in pCols)
            {
                if (pCol != null) Physics2D.IgnoreCollision(myCol, pCol, true);
            }
        }
    }
}
