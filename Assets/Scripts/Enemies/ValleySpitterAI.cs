using UnityEngine;
using System.Collections;

/// <summary>
/// Quai vat ban xa Map 2 (Valley Thorn Spitter):
/// Sinh vat thuc vat / gai doc thung lung tan tich.
/// Co vung ngam canh bao mau do truoc khi ban chum gai doc ve phia Player.
/// </summary>
public class ValleySpitterAI : MonoBehaviour, IDamageable
{
    public static event System.Action<ValleySpitterAI> OnEnemyDied;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 95;
    [SerializeField] private int currentHealth;
    [SerializeField] private float attackRange = 8.0f;
    [SerializeField] private float attackCooldown = 3.0f;
    [SerializeField] private int projectileDamage = 11;
    [SerializeField] private float projectileSpeed = 6.5f;

    private Transform player;
    private PlayerHealth playerHealth;
    private SpriteRenderer sr;
    private Collider2D myCol;

    private float lastAttackTime = -10f;
    private bool isDead = false;
    private bool isCharging = false;
    private Vector3 basePosition;

    void Awake()
    {
        gameObject.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        gameObject.layer = (enemyLayer != -1) ? enemyLayer : 6;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5000;

#if UNITY_EDITOR
        if (sr.sprite == null)
        {
            sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_48_48x47.png"
            );
        }
#endif

        myCol = GetComponent<Collider2D>();
        if (myCol == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.offset = Vector2.zero;
            box.size = new Vector2(2.6f, 2.5f);
            box.isTrigger = false;
            myCol = box;
        }
        else if (myCol is BoxCollider2D bCol && bCol.offset.magnitude > 0.5f)
        {
            bCol.offset = Vector2.zero;
            bCol.size = new Vector2(2.6f, 2.5f);
        }
        EnemyNavigationHelper.EnsureFrictionless(myCol);

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        maxHealth = MapScalingManager.ScaleHealth(maxHealth);
        projectileDamage = MapScalingManager.ScaleDamage(projectileDamage);
        currentHealth = maxHealth;
        basePosition = transform.position;
    }

    void Start()
    {
        basePosition = transform.position;
        FindPlayer();
        IgnorePlayerCollision();
    }

    void Update()
    {
        if (isDead) return;

        // Đóng băng hoàn toàn AI khi đang hiển thị màn hình chọn Lõi Nâng Cấp
        if (LevelUpChoiceUI.IsShowing)
        {
            // Nguồn diện từ - không di chuyển, không bao giờ bắn đạn ngay khi resume
            lastAttackTime = Time.time; // Chặn timer - rồi mới cho phép tấn công sau full cooldown
            return;
        }

        if (PlayerHealth.IsPlayerDead) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // Nhip tho nhap nho khi khong dang gao the tich tu nang luong
        if (!isCharging)
        {
            float breathe = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
            transform.localScale = new Vector3(breathe, 2f - breathe, 1f);
        }

        if (dist <= attackRange && !isCharging)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(ChargeAndShootRoutine());
            }
        }
    }

    private IEnumerator ChargeAndShootRoutine()
    {
        isCharging = true;
        Vector3 anchorPos = transform.position;

        // Giai doan 1: Tich tu doc to va rung chuyen mat dat trong 1.0s (Khong con tia laze do gay roi mat)
        float elapsed = 0f;
        float chargeDuration = 1.0f;
        Vector2 aimDir = Vector2.down;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            if (player != null)
            {
                Vector2 playerTarget = (Vector2)player.position + Vector2.up * 0.5f;
                Vector2 myCenter = (Vector2)anchorPos;
                aimDir = (playerTarget - myCenter).normalized;
            }

            // Hieu ung rung lac cuc da (vibration) khi tich tu nang luong
            float shakeAmp = Mathf.Lerp(0.02f, 0.07f, elapsed / chargeDuration);
            transform.position = anchorPos + (Vector3)(Random.insideUnitCircle * shakeAmp);

            // Nhap nhay sac mau cam vang canh bao tren be mat da
            float pulse = Mathf.PingPong(elapsed * 9f, 1f);
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.65f, 0.2f), pulse);

            yield return null;
        }

        transform.position = anchorPos;
        sr.color = Color.white;

        // Giai doan 2: Ban dan gai doc ve huong da ngam tu loi cuc da
        if (!isDead)
        {
            SoundManager.EnsureInstance()?.PlayEnemyShoot();
            Vector3 shootOrigin = anchorPos;
            SpawnThornProjectile(shootOrigin, aimDir);
        }

        isCharging = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null && box.offset.magnitude > 0.5f)
        {
            box.offset = Vector2.zero;
            box.size = new Vector2(2.6f, 2.5f);
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void SpawnThornProjectile(Vector3 origin, Vector2 dir)
    {
        GameObject proj = new GameObject("Valley_Thorn_Projectile");
        float distToPlayer = (player != null) ? Vector2.Distance(transform.position + new Vector3(1.5f, 1.45f, 0f), player.position) : 5f;
        float spawnDist = Mathf.Min(1.35f, distToPlayer * 0.4f);
        proj.transform.position = origin + (Vector3)(dir * spawnDist);

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
        srProj.color = new Color(0.2f, 0.95f, 0.35f, 1f); // Xanh gai doc thung lung
        srProj.sortingOrder = 5200;

        var col = proj.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        col.isTrigger = true;

        var script = proj.AddComponent<ValleyThornProjectile>();
        script.Init(dir * projectileSpeed, projectileDamage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        // Hien thanh mau noi ngay tren dau cuc da voi toa do chuan
        EnemyFloatingHealthBar.ReportHealth(gameObject, currentHealth, maxHealth, 0.35f);
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

        if (myCol != null) myCol.enabled = false;

        // Roi tinh the xanh & da khoang
        AethelShard.SpawnShards(transform.position, Random.Range(3, 5));
        WorldItemPickup.RollDrop(transform.position, false);
        OnEnemyDied?.Invoke(this);

        Destroy(gameObject, 0.1f);
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

    public static ValleySpitterAI Spawn(Vector3 pos, int index = 1)
    {
        GameObject go = new GameObject($"ValleySpitter_{index}");
        go.transform.position = pos;
        var ai = go.AddComponent<ValleySpitterAI>();
        return ai;
    }
}

public class ValleyThornProjectile : MonoBehaviour
{
    private Vector2 velocity;
    private int damage;
    private float maxLifetime = 3.5f;
    private bool hasHit = false;

    private float trailTimer = 0f;

    public void Init(Vector2 vel, int dmg)
    {
        velocity = vel;
        damage = dmg;

        ApplyDressCode();
        CheckProximity();
        Destroy(gameObject, maxLifetime);
    }

    private void ApplyDressCode()
    {
        var sr = GetComponent<SpriteRenderer>();
        var realm = CorruptedCycleManager.CurrentRealm;

        Color mainCol;
        Color trailStart;
        Color trailEnd;

        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                mainCol = new Color(0.12f, 0.05f, 0.22f, 1f);
                trailStart = new Color(0.45f, 0.12f, 0.70f, 0.85f);
                trailEnd = new Color(0.02f, 0.01f, 0.05f, 0.0f);
                break;
            case CalamityRealm.MagmaCalamity:
                mainCol = new Color(1.0f, 0.35f, 0.05f, 1f);
                trailStart = new Color(1.0f, 0.80f, 0.15f, 0.95f);
                trailEnd = new Color(0.9f, 0.15f, 0.0f, 0.0f);
                break;
            case CalamityRealm.AcidDownpour:
                mainCol = new Color(0.25f, 1.0f, 0.35f, 1f);
                trailStart = new Color(0.35f, 1.0f, 0.45f, 0.90f);
                trailEnd = new Color(0.1f, 0.6f, 0.2f, 0.0f);
                break;
            case CalamityRealm.FrostfallBlizzard:
                mainCol = new Color(0.85f, 0.95f, 1.0f, 1f);
                trailStart = new Color(0.6f, 0.9f, 1.0f, 0.85f);
                trailEnd = new Color(0.3f, 0.7f, 1.0f, 0.0f);
                break;
            default:
                mainCol = new Color(0.2f, 0.95f, 0.35f, 1f);
                trailStart = new Color(0.3f, 1.0f, 0.4f, 0.75f);
                trailEnd = new Color(0.1f, 0.5f, 0.1f, 0.0f);
                break;
        }

        if (sr != null) sr.color = mainCol;

        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = trailStart;
        trail.endColor = trailEnd;
        trail.startWidth = 0.28f;
        trail.endWidth = 0.02f;
        trail.time = 0.22f;
        trail.sortingOrder = 5190;
    }

    void Update()
    {
        if (hasHit) return;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        trailTimer += Time.deltaTime;
        if (trailTimer >= 0.07f)
        {
            trailTimer = 0f;
            SpawnTrailParticle();
        }

        CheckProximity();
    }

    private void SpawnTrailParticle()
    {
        var realm = CorruptedCycleManager.CurrentRealm;
        GameObject p = new GameObject("Thorn_Trail_FX");
        p.transform.position = transform.position;
        var pSr = p.AddComponent<SpriteRenderer>();
        pSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        pSr.sortingOrder = 5180;

        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                pSr.color = new Color(0.15f, 0.04f, 0.22f, 0.7f);
                p.transform.localScale = Vector3.one * Random.Range(0.2f, 0.35f);
                break;
            case CalamityRealm.MagmaCalamity:
                pSr.color = new Color(1.0f, 0.7f, 0.1f, 0.85f);
                p.transform.localScale = Vector3.one * Random.Range(0.14f, 0.25f);
                break;
            case CalamityRealm.AcidDownpour:
                pSr.color = new Color(0.3f, 1.0f, 0.4f, 0.75f);
                p.transform.localScale = Vector3.one * Random.Range(0.16f, 0.28f);
                break;
            case CalamityRealm.FrostfallBlizzard:
                pSr.color = new Color(0.85f, 0.95f, 1.0f, 0.8f);
                p.transform.localScale = Vector3.one * Random.Range(0.18f, 0.3f);
                break;
            default:
                pSr.color = new Color(0.3f, 0.9f, 0.4f, 0.65f);
                p.transform.localScale = Vector3.one * Random.Range(0.15f, 0.25f);
                break;
        }

        Destroy(p, 0.18f);
    }

    private void CheckProximity()
    {
        if (hasHit) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.35f);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            var ph = h.GetComponent<PlayerHealth>() ?? h.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                hasHit = true;
                ph.TakeDamage(damage);
                SoundManager.EnsureInstance()?.PlayPlayerHurt();
                DamagePopup.Create(h.transform.position, damage, isPlayer: true);
                Destroy(gameObject);
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other == null) return;

        var ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            hasHit = true;
            ph.TakeDamage(damage);
            SoundManager.EnsureInstance()?.PlayPlayerHurt();
            DamagePopup.Create(other.transform.position, damage, isPlayer: true);
            Destroy(gameObject);
            return;
        }
        else if (other.gameObject.name.ToLower().Contains("wall") || other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
