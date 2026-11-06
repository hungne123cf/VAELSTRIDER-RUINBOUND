using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Explosive Barrel (Thùng Thuốc Nổ / Thùng Dầu Hỏa Cổ Đại)
/// 
/// ĐẶC TÍNH CHIẾN THUẬT:
/// 1. Là vật thể rắn (Solid Obstacle) che chắn góc hẹp và chặn đường quái.
/// 2. Có 20 HP, nhận sát thương từ Player, Quái vật, Đạn bay hoặc Bẫy gai.
/// 3. Khi bị đánh vỡ:
///    - Kích hoạt ngòi nổ đỏ rực nhấp nháy 0.28s.
///    - Phát nổ diện rộng bán kính 3.5m:
///      + Quái vật trong vùng nổ: Chịu 65 sát thương + bị đẩy lùi mạnh + Choáng 0.6s!
///      + Player nếu đứng quá gần (trong 2.2m): Nhận 15 sát thương.
///      + Gây nổ dây chuyền (Chain Reaction) nếu có các thùng nổ khác ở gần!
/// </summary>
public class ExplosiveBarrel : MonoBehaviour, IDamageable
{
    [Header("Barrel Settings")]
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private int explosionDamageToEnemies = 65;
    [SerializeField] private int explosionDamageToPlayer = 15;
    [SerializeField] private float explosionRadius = 3.5f;

    private int currentHealth;
    private bool isExploded = false;
    private bool isFusing = false;

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private static Sprite cachedBarrelSprite;

    void Awake()
    {
        currentHealth = maxHealth;
        gameObject.tag = "Breakable";
        gameObject.layer = 0; // Default

        sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1700; // Ngang hàng quái & vật thể

        col = gameObject.GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 1.1f);
        col.offset = new Vector2(0f, 0.05f);
        col.isTrigger = false; // Rắn, cản chuyển động

        EnsureBarrelSprite();
        if (cachedBarrelSprite != null) sr.sprite = cachedBarrelSprite;
    }

    public void TakeDamage(int damage)
    {
        if (isExploded || isFusing) return;

        currentHealth -= damage;
        StartCoroutine(FlashHitRoutine());

        if (currentHealth <= 0)
        {
            StartCoroutine(FuseAndExplodeRoutine());
        }
    }

    private IEnumerator FlashHitRoutine()
    {
        if (sr != null) sr.color = new Color(1f, 0.4f, 0.4f);
        yield return new WaitForSeconds(0.08f);
        if (sr != null && !isFusing) sr.color = Color.white;
    }

    private IEnumerator FuseAndExplodeRoutine()
    {
        isFusing = true;
        SoundManager.EnsureInstance()?.PlayHitImpact();

        // Rung lắc & nhấp nháy đỏ rực trong 0.28s
        float fuseTime = 0.28f;
        float elapsed = 0f;
        Vector3 origPos = transform.position;

        while (elapsed < fuseTime)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 18f, 1f);
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.2f, 0.1f), pulse);

            float jitter = Mathf.Sin(elapsed * 60f) * 0.06f;
            transform.position = origPos + new Vector3(jitter, 0f, 0f);
            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;

        Vector3 blastPos = transform.position;

        // Rung màn hình uy lực
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.65f);

        SoundManager.EnsureInstance()?.PlayHitImpact();

        // 1. Quét sát thương toàn diện xung quanh
        Collider2D[] colliders = Physics2D.OverlapCircleAll(blastPos, explosionRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D c = colliders[i];
            if (c == null || c.gameObject == gameObject) continue;

            // Player trúng nổ nếu đứng gần
            if (c.CompareTag("Player") || c.GetComponent<PlayerHealth>() != null)
            {
                var ph = c.GetComponent<PlayerHealth>() ?? c.GetComponentInParent<PlayerHealth>();
                if (ph != null && Vector2.Distance(blastPos, c.bounds.center) <= 2.2f)
                {
                    ph.TakeDamage(explosionDamageToPlayer);
                    DamagePopup.Create(c.bounds.center + Vector3.up * 0.8f, explosionDamageToPlayer, isPlayer: true);
                }
                continue;
            }

            // Quái vật trúng nổ
            var dmgable = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
            if (dmgable != null && (dmgable as Component) != null)
            {
                var comp = dmgable as Component;
                if (!comp.CompareTag("Player"))
                {
                    dmgable.TakeDamage(explosionDamageToEnemies);
                    DamagePopup.Create(comp.transform.position + Vector3.up * 0.8f, explosionDamageToEnemies, isPlayer: false);

                    // Đẩy lùi mạnh quái
                    var rbEnemy = comp.GetComponent<Rigidbody2D>();
                    if (rbEnemy != null)
                    {
                        Vector2 pushDir = ((Vector2)comp.transform.position - (Vector2)blastPos).normalized;
                        rbEnemy.linearVelocity = pushDir * 6.5f;
                    }

                    // Choáng 0.6s
                    EnemyStunEffect.ApplyStun(comp.gameObject, 0.6f);
                }
            }
        }

        // 2. Hiệu ứng bộc phá lửa & khói nổ (Explosion Shockwave)
        SpawnExplosionVFX(blastPos);

        // 3. Để lại vết cháy đen trên mặt sàn
        SpawnScorchMark(blastPos);

        Destroy(gameObject, 0.4f);
    }

    private void SpawnExplosionVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("BarrelExplosionFX");
        fx.transform.position = pos;
        var fSr = fx.AddComponent<SpriteRenderer>();
        fSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        fSr.color = new Color(1.0f, 0.45f, 0.1f, 0.95f);
        fSr.sortingOrder = 5500;

        StartCoroutine(ExplosionGrowFadeRoutine(fx, fSr));
    }

    private IEnumerator ExplosionGrowFadeRoutine(GameObject fx, SpriteRenderer fSr)
    {
        float dur = 0.35f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * (explosionRadius * 2.2f);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (fx != null)
            {
                fx.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                if (fSr != null)
                {
                    Color c = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.9f, 0.15f, 0.05f), t);
                    c.a = Mathf.Lerp(0.95f, 0f, t);
                    fSr.color = c;
                }
            }
            yield return null;
        }

        if (fx != null) Destroy(fx);
    }

    private void SpawnScorchMark(Vector3 pos)
    {
        GameObject scorch = new GameObject("BarrelScorchMark");
        scorch.transform.position = pos;
        var sSr = scorch.AddComponent<SpriteRenderer>();
        sSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sSr.color = new Color(0.08f, 0.05f, 0.05f, 0.65f); // Vết cháy đen
        sSr.sortingOrder = 1420; // Nằm trên sàn
        scorch.transform.localScale = new Vector3(2.4f, 1.4f, 1f);

        Destroy(scorch, 12f); // Mờ dần và biến mất sau 12s
    }

    private static void EnsureBarrelSprite()
    {
        if (cachedBarrelSprite != null) return;

        int w = 24;
        int h = 30;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color woodBase = new Color(0.42f, 0.26f, 0.16f, 1f);
        Color woodDark = new Color(0.28f, 0.16f, 0.09f, 1f);
        Color redBand = new Color(0.85f, 0.18f, 0.12f, 1f); // Vòng kim loại sơn đỏ cảnh báo
        Color redBandShadow = new Color(0.55f, 0.10f, 0.08f, 1f);
        Color skullWhite = new Color(0.95f, 0.95f, 0.95f, 1f);
        Color outline = new Color(0.12f, 0.08f, 0.05f, 1f);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            // Độ phình của thùng gỗ theo chiều dọc
            float curve = Mathf.Sin((y / (float)h) * Mathf.PI);
            int halfWidth = Mathf.RoundToInt(6 + curve * 4.5f);

            for (int x = 0; x < w; x++)
            {
                int distFromCenter = Mathf.Abs(x - w / 2);
                if (distFromCenter > halfWidth)
                {
                    tex.SetPixel(x, y, clear);
                }
                else if (distFromCenter == halfWidth || y == 0 || y == h - 1)
                {
                    tex.SetPixel(x, y, outline);
                }
                else
                {
                    // Các đai sắt sơn đỏ ở tầng y = 5, y = 14, y = 23
                    bool isBand = (y >= 4 && y <= 6) || (y >= 13 && y <= 15) || (y >= 22 && y <= 24);
                    if (isBand)
                    {
                        tex.SetPixel(x, y, (x < w / 2) ? redBand : redBandShadow);
                    }
                    else
                    {
                        // Thớ gỗ
                        bool isPlankDivider = (x == w / 2 - 4 || x == w / 2 + 4);
                        tex.SetPixel(x, y, isPlankDivider ? woodDark : woodBase);
                    }
                }
            }
        }

        // Vẽ ký hiệu đầu lâu cảnh báo nổ ở giữa thân thùng (x=10..13, y=17..20)
        tex.SetPixel(11, 19, skullWhite);
        tex.SetPixel(12, 19, skullWhite);
        tex.SetPixel(10, 18, skullWhite);
        tex.SetPixel(13, 18, skullWhite);
        tex.SetPixel(11, 18, outline); // mắt trái
        tex.SetPixel(12, 18, outline); // mắt phải
        tex.SetPixel(11, 17, skullWhite);
        tex.SetPixel(12, 17, skullWhite);

        tex.Apply();
        cachedBarrelSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 24f);
    }

    public static ExplosiveBarrel Create(Vector3 worldPos, Transform parent = null)
    {
        GameObject go = new GameObject("ExplosiveBarrel");
        go.transform.position = worldPos;
        if (parent != null) go.transform.SetParent(parent, true);
        return go.AddComponent<ExplosiveBarrel>();
    }
}
