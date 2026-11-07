using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class MiniSpiderMinion : MonoBehaviour, IDamageable, IEnemyHealth
{
    public static readonly System.Collections.Generic.HashSet<MiniSpiderMinion> ActiveMinions = new System.Collections.Generic.HashSet<MiniSpiderMinion>();
    private static Sprite[] s_CachedWalkSprites;
    private static Sprite s_CachedDeathSprite;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    private int maxHealth = 65;
    private int currentHealth;
    private float moveSpeed = 4.2f;
    private int biteDamage = 3;
    private float biteCooldown = 1.0f;
    private float biteTimer = 0f;
    private bool isDead = false;

    private Sprite[] walkSprites;
    private Sprite deathSprite;
    private int walkFrame = 0;
    private float animTimer = 0f;

    public static event System.Action<MiniSpiderMinion> OnEnemyDied;

    public static MiniSpiderMinion Spawn(Vector3 pos)
    {
        GameObject go = new GameObject("MiniSpider_Minion");
        go.transform.position = pos;
        return go.AddComponent<MiniSpiderMinion>();
    }

    void OnEnable()
    {
        ActiveMinions.Add(this);
    }

    void OnDisable()
    {
        ActiveMinions.Remove(this);
    }

    void OnDestroy()
    {
        ActiveMinions.Remove(this);
    }

    void Awake()
    {
        maxHealth = MapScalingManager.ScaleHealth(maxHealth);
        biteDamage = MapScalingManager.ScaleDamage(biteDamage);
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = gameObject.AddComponent<SpriteRenderer>();

        // Gỡ bỏ triệt để PolygonCollider2D và DynamicSpriteColliderSync để Box2D không bị nghẽn
        var oldSync = GetComponent<DynamicSpriteColliderSync>();
        if (oldSync != null) DestroyImmediate(oldSync);
        var oldPoly = GetComponent<PolygonCollider2D>();
        if (oldPoly != null) DestroyImmediate(oldPoly);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        gameObject.layer = (enemyLayer != -1) ? enemyLayer : 6;
        gameObject.tag = "Enemy";

        sr.sortingOrder = 1200;
        transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        // Load Sprites tĩnh 1 lần duy nhất (Zero-Lag Memory Caching)
        if (s_CachedWalkSprites == null)
        {
            s_CachedWalkSprites = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                s_CachedWalkSprites[i] = Resources.Load<Sprite>($"Enemies/Map3/SkeletonKing_Walk_{i}");
#if UNITY_EDITOR
                if (s_CachedWalkSprites[i] == null)
                    s_CachedWalkSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Bosses/Skeleton King/SkeletonKing_Walk_{i}.png");
#endif
            }

            s_CachedDeathSprite = Resources.Load<Sprite>("Enemies/Map3/SkeletonKing_Death_1");
#if UNITY_EDITOR
            if (s_CachedDeathSprite == null)
                s_CachedDeathSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Death_1.png");
#endif

            if (s_CachedWalkSprites[0] == null)
            {
                Sprite fallback = GetProceduralSpiderSprite();
                for (int i = 0; i < 4; i++)
                {
                    s_CachedWalkSprites[i] = fallback;
                }
            }
            if (s_CachedDeathSprite == null) s_CachedDeathSprite = s_CachedWalkSprites[0];
        }

        walkSprites = s_CachedWalkSprites;
        deathSprite = s_CachedDeathSprite;

        sr.sprite = walkSprites[0];
        sr.color = Color.white;

        // Zero-Bug Android: Dùng CircleCollider2D nhẹ tênh thay vì PolygonCollider2D để không bao giờ bị nghẽn vật lý Box2D
        var circleCol = GetComponent<CircleCollider2D>();
        if (circleCol == null) circleCol = gameObject.AddComponent<CircleCollider2D>();
        circleCol.radius = 0.35f;
        circleCol.isTrigger = false;
        col = circleCol;
        EnemyNavigationHelper.EnsureFrictionless(col);

        gameObject.AddComponent<AutoDepthSort>();
    }

    private static Sprite proceduralSpiderCache;
    private static Sprite GetProceduralSpiderSprite()
    {
        if (proceduralSpiderCache != null) return proceduralSpiderCache;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color transparent = new Color(0, 0, 0, 0);
        Color spiderBody = new Color(0.18f, 0.15f, 0.22f, 1f);
        Color spiderEyes = new Color(0.95f, 0.2f, 0.2f, 1f);
        Color spiderLegs = new Color(0.35f, 0.3f, 0.4f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 16f;
                float dy = y - 16f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= 8.5f)
                {
                    if (dy > 1.5f && (Mathf.Abs(dx - 3f) < 2f || Mathf.Abs(dx + 3f) < 2f))
                        tex.SetPixel(x, y, spiderEyes);
                    else
                        tex.SetPixel(x, y, spiderBody);
                }
                else if (dist <= 13.5f && (Mathf.Abs(dx) > 5f || Mathf.Abs(dy) > 5f))
                {
                    tex.SetPixel(x, y, spiderLegs);
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        proceduralSpiderCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return proceduralSpiderCache;
    }

    void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (PlayerHealth.IsPlayerDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (biteTimer > 0f) biteTimer -= Time.deltaTime;

        Vector2 bodyPos = DynamicSpriteColliderSync.GetBodyCenter(transform, col);

        // Bò nhanh tới player (tính chuẩn từ tâm cơ thể nhện con thực tế)
        Vector2 dir = ((Vector2)player.position - bodyPos).normalized;
        Vector2 moveDir = EnemyNavigationHelper.GetSteeredDirection(bodyPos, dir, col, 0.25f, 0.8f);
        rb.linearVelocity = moveDir * moveSpeed;

        // Lật mặt theo hướng đi (Zero-Bug: Dùng flipX để Box2D không bị nghẽn)
        if (Mathf.Abs(dir.x) > 0.05f)
        {
            if (sr != null) sr.flipX = (dir.x < 0);
            transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        }

        // Hoat anh bò lom ngom
        animTimer += Time.deltaTime;
        if (animTimer >= 0.12f)
        {
            animTimer = 0f;
            walkFrame = (walkFrame + 1) % 4;
            if (walkSprites != null && walkSprites[walkFrame] != null)
            {
                sr.sprite = walkSprites[walkFrame];
            }
        }

        // Cắn cận chiến
        if (Vector2.Distance(bodyPos, player.position) <= 0.95f && biteTimer <= 0f)
        {
            biteTimer = biteCooldown;
            var target = player.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(biteDamage);
                SoundManager.EnsureInstance()?.PlayHitImpact();
            }
        }
    }

    private Rect homeRoomBounds;

    void LateUpdate()
    {
        if (isDead) return;
        Vector3 pos = transform.position;
        Vector3 clamped = DungeonRoomManager.ClampEnemyToHomeRoom(pos, ref homeRoomBounds, 0.7f);
        if (Mathf.Abs(pos.x - clamped.x) > 0.005f || Mathf.Abs(pos.y - clamped.y) > 0.005f)
        {
            transform.position = new Vector3(clamped.x, clamped.y, 0f);
            if (rb != null)
            {
                rb.position = new Vector2(clamped.x, clamped.y);
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        EnemyFloatingHealthBar.ReportHealth(gameObject, currentHealth, maxHealth);
        StartCoroutine(DamageFlashRoutine());
        Vector2 bodyPos = DynamicSpriteColliderSync.GetBodyCenter(transform, col);
        DamagePopup.Create(bodyPos, damage, false);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        sr.color = Color.white;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;

        if (deathSprite != null) sr.sprite = deathSprite;
        SoundManager.EnsureInstance()?.PlayHitImpact();
        OnEnemyDied?.Invoke(this);

        // Roi Linh Hon & Vat pham ngau nhien
        AethelShard.SpawnShards(transform.position, Random.Range(1, 3));
        if (Random.value < 0.25f)
        {
            WorldItemPickup.RollDrop(transform.position, false, -0.1f);
        }

        Destroy(gameObject, 0.25f);
    }

    // ─── IEnemyHealth Implementation ───────────────────────
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsBoss => false;
    public void MultiplyHealth(float mult)
    {
        maxHealth = Mathf.RoundToInt(maxHealth * mult);
        currentHealth = Mathf.RoundToInt(currentHealth * mult);
    }

}
