using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quai vat can chien Map 2 (Valley Beast):
/// Hoang thu nhanh nhen dac trung cua Thung Lung Tan Tich.
/// Su dung sprite sheet luck creature voi hoat anh Chay (Run) va Dung yen (Idle) 4 huong.
/// </summary>
public class ValleyBeastAI : MonoBehaviour, IDamageable
{
    public static event System.Action<ValleyBeastAI> OnEnemyDied;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 220;
    [SerializeField] private int currentHealth;
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private int attackDamage = 22;
    [SerializeField] private float attackRange = 1.35f;
    [SerializeField] private float attackCooldown = 1.4f;
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

        myCol = GetComponent<Collider2D>();
        if (myCol == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.9f, 0.8f);
            box.offset = new Vector2(0, 0);
            myCol = box;
        }

        currentHealth = maxHealth;
        LoadSprites();

        if (idleSprites != null && idleSprites.Count > 0)
        {
            sr.sprite = idleSprites[0];
        }
    }

    void Start()
    {
        FindPlayer();
        IgnorePlayerCollision();
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
        if (dist <= detectionRadius)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

            // Flip sprite theo huong chay
            if (dir.x > 0.05f) sr.flipX = false;
            else if (dir.x < -0.05f) sr.flipX = true;

            if (dist > attackRange)
            {
                // Duoi theo nguoi choi
                rb.linearVelocity = dir * moveSpeed;
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
        if (idleSprites != null && runSprites != null) return;

        idleSprites = new List<Sprite>();
        runSprites = new List<Sprite>();

#if UNITY_EDITOR
        string idlePath = "Assets/ART/EPIC RPG World Pack - [FREE Demo]Ancient Ruins - Copia/Characters/luck creature-idle.png";
        string runPath = "Assets/ART/EPIC RPG World Pack - [FREE Demo]Ancient Ruins - Copia/Characters/luck creature-run.png";

        var idles = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(idlePath);
        foreach (var obj in idles)
        {
            if (obj is Sprite s) idleSprites.Add(s);
        }

        var runs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(runPath);
        foreach (var obj in runs)
        {
            if (obj is Sprite s) runSprites.Add(s);
        }
#endif
    }
}
