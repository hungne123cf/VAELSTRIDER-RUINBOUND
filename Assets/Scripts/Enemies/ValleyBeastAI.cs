using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quai vat can chien Map 2 (Valley Beast):
/// Hoang thu nhanh nhen dac trung cua Thung Lung Tan Tich.
/// Su dung sprite sheet luck creature voi hoat anh Chay (Run) va Dung yen (Idle) 4 huong.
/// </summary>
public class ValleyBeastAI : MonoBehaviour, IDamageable, IEnemyHealth
{
    public static event System.Action<ValleyBeastAI> OnEnemyDied;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 120;
    [SerializeField] private int currentHealth;
    [SerializeField] private float moveSpeed = 3.6f;
    [SerializeField] private int attackDamage = 12;
    [SerializeField] private float attackRange = 1.35f;
    [SerializeField] private float attackCooldown = 1.6f;
    [SerializeField] private float detectionRadius = 10f;

    private Transform player;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D myCol;

    private float lastAttackTime = -10f;
    private bool isDead = false;
    private bool isFlashing = false;

    // Sprite Animation
    private static List<Sprite> idleSprites;
    private static List<Sprite> runSprites;
    private float animTimer = 0f;
    private int currentFrame = 0;
    private bool isMoving = false;

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

        maxHealth = MapScalingManager.ScaleHealth(maxHealth);
        attackDamage = MapScalingManager.ScaleDamage(attackDamage);
        currentHealth = maxHealth;
        LoadSprites();

        if (idleSprites != null && idleSprites.Count > 0)
        {
            sr.sprite = idleSprites[0];
        }
        else if (sr.sprite == null)
        {
            // Procedural fallback to ensure zero-invisible bug on Android
            Texture2D tex = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    tex.SetPixel(x, y, new Color(0.85f, 0.45f, 0.15f, 1f));
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
        }

        myCol = DynamicSpriteColliderSync.EnsureAttached(gameObject);
        EnemyNavigationHelper.EnsureFrictionless(myCol);
        EnemyCCCleanseSystem.EnsureAttached(gameObject);
    }

    void Start()
    {
        FindPlayer();
        IgnorePlayerCollision();
    }

    void Update()
    {
        if (isDead) return;

        if (PlayerHealth.IsPlayerDead)
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
            UpdateAnimation();
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= detectionRadius)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

            // Flip sprite theo huong chay
            if (dir.x > 0.05f) sr.flipX = false;
            else if (dir.x < -0.05f) sr.flipX = true;

            if (dist > attackRange)
            {
                // Duoi theo nguoi choi (tu dong luon lach qua vat can)
                Vector2 moveDir = EnemyNavigationHelper.GetSteeredDirection(transform.position, dir, myCol, 0.35f, 1.0f);
                rb.linearVelocity = moveDir * moveSpeed;
                isMoving = true;
            }
            else
            {
                // Trong tam tan cong
                rb.linearVelocity = Vector2.zero;
                isMoving = false;

                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    lastAttackTime = Time.time;
                    StartCoroutine(AttackRoutine());
                }
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
        }

        UpdateAnimation();
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

    private void UpdateAnimation()
    {
        animTimer += Time.deltaTime;
        float frameRate = isMoving ? 0.10f : 0.18f;

        if (animTimer >= frameRate)
        {
            animTimer = 0f;
            currentFrame++;

            if (isMoving)
            {
                if (runSprites != null && runSprites.Count > 0)
                {
                    sr.sprite = runSprites[currentFrame % runSprites.Count];
                }
            }
            else
            {
                if (idleSprites != null && idleSprites.Count > 0)
                {
                    sr.sprite = idleSprites[currentFrame % idleSprites.Count];
                }
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        // Lao len vo can nguoi choi
        Vector3 startPos = transform.position;
        if (player != null)
        {
            Vector3 targetPos = player.position;
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, t / 0.2f);
                yield return null;
            }

            if (playerHealth != null && Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f)
            {
                playerHealth.TakeDamage(attackDamage);
                SoundManager.EnsureInstance()?.PlayPlayerHurt();
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        EnemyFloatingHealthBar.ReportHealth(gameObject, currentHealth, maxHealth);
        StartCoroutine(HitFlashRoutine());

        // Knockback nhe khi trung don
        if (player != null && rb != null)
        {
            Vector2 pushDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
            rb.linearVelocity = pushDir * 3.5f;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (isFlashing) yield break;
        isFlashing = true;

        Color original = sr.color;
        sr.color = new Color(1f, 0.35f, 0.35f, 1f);
        yield return new WaitForSeconds(0.12f);
        sr.color = original;
        isFlashing = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (myCol != null) myCol.enabled = false;
        rb.linearVelocity = Vector2.zero;

        // Roi tinh the Aethel Shards & Khoang thach
        AethelShard.SpawnShards(transform.position, Random.Range(3, 6));
        WorldItemPickup.RollDrop(transform.position, false);

        OnEnemyDied?.Invoke(this);

        // Hieu ung bien mat
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float t = 0;
        Color c = sr.color;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / 0.3f);
            sr.color = c;
            yield return null;
        }

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

    private void LoadSprites()
    {
        if (idleSprites != null && runSprites != null && idleSprites.Count > 0 && runSprites.Count > 0) return;

        idleSprites = new List<Sprite>();
        runSprites = new List<Sprite>();

        // 1. Safe Resource Loading for Android runtime
        var idles = Resources.LoadAll<Sprite>("Enemies/Map2/luck creature-idle");
        if (idles != null && idles.Length > 0)
        {
            foreach (var s in idles) if (s != null) idleSprites.Add(s);
        }

        var runs = Resources.LoadAll<Sprite>("Enemies/Map2/luck creature-run");
        if (runs != null && runs.Length > 0)
        {
            foreach (var s in runs) if (s != null) runSprites.Add(s);
        }

#if UNITY_EDITOR
        // 2. Editor AssetDatabase fallback
        if (idleSprites.Count == 0)
        {
            string idlePath = "Assets/ART/EPIC RPG World Pack - [FREE Demo]Ancient Ruins - Copia/Characters/luck creature-idle.png";
            var editorIdles = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(idlePath);
            if (editorIdles != null)
            {
                foreach (var obj in editorIdles) if (obj is Sprite s) idleSprites.Add(s);
            }
        }

        if (runSprites.Count == 0)
        {
            string runPath = "Assets/ART/EPIC RPG World Pack - [FREE Demo]Ancient Ruins - Copia/Characters/luck creature-run.png";
            var editorRuns = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(runPath);
            if (editorRuns != null)
            {
                foreach (var obj in editorRuns) if (obj is Sprite s) runSprites.Add(s);
            }
        }
#endif
    }

    public static ValleyBeastAI Spawn(Vector3 pos, int index = 1)
    {
        GameObject go = new GameObject($"ValleyBeast_{index}");
        go.transform.position = pos;
        var ai = go.AddComponent<ValleyBeastAI>();
        return ai;
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
