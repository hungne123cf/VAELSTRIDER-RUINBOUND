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
    [SerializeField] private int maxHealth = 35;
    [SerializeField] private int currentHealth;
    [SerializeField] private float attackRange = 8.5f;
    [SerializeField] private float attackCooldown = 2.8f;
    [SerializeField] private int projectileDamage = 10;
    [SerializeField] private float projectileSpeed = 7.0f;

    private Transform player;
    private PlayerHealth playerHealth;
    private SpriteRenderer sr;
    private Collider2D myCol;

    private float lastAttackTime = -10f;
    private bool isDead = false;
    private bool isCharging = false;
    private LineRenderer telegraphLine;

    void Awake()
    {
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
            box.size = new Vector2(1.0f, 1.0f);
            myCol = box;
        }

        currentHealth = maxHealth;
        SetupTelegraphLine();
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

        // Nhip tho nhap nho
        float breathe = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
        transform.localScale = new Vector3(breathe, 2f - breathe, 1f);

        if (dist <= attackRange && !isCharging)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(ChargeAndShootRoutine());
            }
        }
    }

    private void SetupTelegraphLine()
    {
        telegraphLine = gameObject.AddComponent<LineRenderer>();
        Shader unlitS = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"); if (unlitS != null) telegraphLine.material = new Material(unlitS);
        telegraphLine.startColor = new Color(1f, 0.2f, 0.2f, 0.7f);
        telegraphLine.endColor = new Color(1f, 0.5f, 0.1f, 0.2f);
        telegraphLine.startWidth = 0.08f;
        telegraphLine.endWidth = 0.04f;
        telegraphLine.positionCount = 2;
        telegraphLine.sortingOrder = 5100;
        telegraphLine.enabled = false;
    }

    private IEnumerator ChargeAndShootRoutine()
    {
        isCharging = true;

        // Giai doan 1: Ngam ban va bat duong canh bao do trong 1.0s
        float elapsed = 0f;
        float chargeDuration = 1.0f;
        if (telegraphLine != null) telegraphLine.enabled = true;

        Vector2 aimDir = Vector2.down;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            if (player != null)
            {
                aimDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                if (telegraphLine != null)
                {
                    telegraphLine.SetPosition(0, transform.position);
                    telegraphLine.SetPosition(1, (Vector2)transform.position + aimDir * attackRange);
                }
            }

            // Nhap nhay mau canh bao
            float pulse = Mathf.PingPong(elapsed * 8f, 1f);
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.2f), pulse);

            yield return null;
        }

        if (telegraphLine != null) telegraphLine.enabled = false;
        sr.color = Color.white;

        // Giai doan 2: Ban dan gai doc ve huong da ngam
        if (!isDead)
        {
            SoundManager.EnsureInstance()?.PlayEnemyShoot();
            SpawnThornProjectile(transform.position, aimDir);
        }

        isCharging = false;
    }

    private void SpawnThornProjectile(Vector3 origin, Vector2 dir)
    {
        GameObject proj = new GameObject("Valley_Thorn_Projectile");
        proj.transform.position = origin + (Vector3)(dir * 0.5f);

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
        col.radius = 0.25f;
        col.isTrigger = true;

        var script = proj.AddComponent<ValleyThornProjectile>();
        script.Init(dir * projectileSpeed, projectileDamage);
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

        if (telegraphLine != null) telegraphLine.enabled = false;
        if (myCol != null) myCol.enabled = false;

        // Roi tinh the xanh
        AethelShard.SpawnShards(transform.position, Random.Range(3, 5));
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
}

public class ValleyThornProjectile : MonoBehaviour
{
    private Vector2 velocity;
    private int damage;
    private float maxLifetime = 3.5f;

    public void Init(Vector2 vel, int dmg)
    {
        velocity = vel;
        damage = dmg;
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
        {
            var ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                SoundManager.EnsureInstance()?.PlayPlayerHurt();
            }
            Destroy(gameObject);
        }
        else if (other.gameObject.name.ToLower().Contains("wall") || other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null)
        {
            Destroy(gameObject);
        }
    }
}
