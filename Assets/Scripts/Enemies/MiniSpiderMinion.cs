using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class MiniSpiderMinion : MonoBehaviour, IDamageable
{
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private CircleCollider2D col;

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

    void Awake()
    {
        maxHealth = MapScalingManager.ScaleHealth(maxHealth);
        biteDamage = MapScalingManager.ScaleDamage(biteDamage);
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        sr = gameObject.AddComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        col.radius = 0.95f;
        col.offset = Vector2.zero;
        col.isTrigger = false;
        EnemyNavigationHelper.EnsureFrictionless(col);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        gameObject.layer = (enemyLayer != -1) ? enemyLayer : 6;
        gameObject.tag = "Enemy";

        sr.sortingOrder = 1200;
        transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        // Load Sprites
        walkSprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
#if UNITY_EDITOR
            walkSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Bosses/Skeleton King/SkeletonKing_Walk_{i}.png");
#endif
        }
#if UNITY_EDITOR
        deathSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Death_1.png");
#endif
        if (walkSprites[0] != null) sr.sprite = walkSprites[0];

        gameObject.AddComponent<AutoDepthSort>();
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

        // Bò nhanh tới player (luon lach qua vat can)
        Vector2 dir = (player.position - transform.position).normalized;
        Vector2 moveDir = EnemyNavigationHelper.GetSteeredDirection(transform.position, dir, col, 0.25f, 0.8f);
        rb.linearVelocity = moveDir * moveSpeed;

        // Lật mặt theo hướng đi
        if (Mathf.Abs(dir.x) > 0.05f)
        {
            transform.localScale = new Vector3(dir.x > 0 ? 0.35f : -0.35f, 0.35f, 1f);
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
        if (Vector2.Distance(transform.position, player.position) <= 0.8f && biteTimer <= 0f)
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

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        EnemyFloatingHealthBar.ReportHealth(gameObject, currentHealth, maxHealth);
        StartCoroutine(DamageFlashRoutine());
        DamagePopup.Create(transform.position, damage, false);

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
}
