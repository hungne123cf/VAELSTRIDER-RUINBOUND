using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Ice Stalagmite Crystal (Cột Thạch Anh Băng Hư Không - Map 3)
/// 
/// ĐẶC TÍNH CHIẾN THUẬT:
/// 1. Là vật cản tự nhiên che chắn các tia đạn và đòn đánh của Skeleton King / Nhện hư không.
/// 2. Có 35 HP, có thể bị đánh vỡ bởi đòn chém của Player hoặc các cú dộng đất của Boss.
/// 3. Khi vỡ nát:
///    - Phát nổ hàn băng (Frost Nova) bán kính 3.5m:
///      + Gây 40 sát thương lên tất cả quái vật xung quanh.
///      + Đóng băng (Freeze) toàn bộ quái trong 0.85s!
///      + Bắn tung tóe các mảnh thạch anh băng sắc nhọn.
/// </summary>
public class IceStalagmiteCrystal : MonoBehaviour, IDamageable
{
    [Header("Crystal Settings")]
    [SerializeField] private int maxHealth = 35;
    [SerializeField] private int shatterDamageToEnemies = 40;
    [SerializeField] private float shatterRadius = 3.5f;

    private int currentHealth;
    private bool isShattered = false;

    private SpriteRenderer sr;
    private CapsuleCollider2D col;
    private static Sprite cachedCrystalSprite;

    void Awake()
    {
        currentHealth = maxHealth;
        gameObject.layer = 0;

        sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1750;

        col = gameObject.GetComponent<CapsuleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.9f, 1.4f);
        col.direction = CapsuleDirection2D.Vertical;
        col.isTrigger = false; // Rắn, che chắn đạn

        EnsureCrystalSprite();
        if (cachedCrystalSprite != null) sr.sprite = cachedCrystalSprite;
    }

    public void TakeDamage(int damage)
    {
        if (isShattered) return;

        currentHealth -= damage;
        StartCoroutine(FlashCyanHitRoutine());

        if (currentHealth <= 0)
        {
            Shatter();
        }
    }

    private IEnumerator FlashCyanHitRoutine()
    {
        if (sr != null) sr.color = new Color(0.5f, 0.95f, 1f, 1f);
        yield return new WaitForSeconds(0.08f);
        if (sr != null && !isShattered) sr.color = Color.white;
    }

    private void Shatter()
    {
        if (isShattered) return;
        isShattered = true;

        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;

        Vector3 shatterPos = transform.position;

        SoundManager.EnsureInstance()?.PlayHitImpact();

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.45f);

        // 1. Quét sát thương & Đóng băng quái xung quanh
        Collider2D[] hits = Physics2D.OverlapCircleAll(shatterPos, shatterRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i];
            if (c == null || c.gameObject == gameObject) continue;

            var dmgable = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
            if (dmgable != null && (dmgable as Component) != null)
            {
                var comp = dmgable as Component;
                if (!comp.CompareTag("Player"))
                {
                    dmgable.TakeDamage(shatterDamageToEnemies);
                    DamagePopup.Create(comp.transform.position + Vector3.up * 0.8f, shatterDamageToEnemies, isPlayer: false);

                    // Đóng băng quái 0.85s!
                    EnemyFreezeEffect.ApplyFreeze(comp.gameObject, 0.85f);
                }
            }
        }

        // 2. Hiệu ứng bộc phá mảnh băng Frost Nova
        SpawnFrostNovaFX(shatterPos);

        Destroy(gameObject, 0.5f);
    }

    private void SpawnFrostNovaFX(Vector3 pos)
    {
        GameObject fx = new GameObject("FrostNovaFX");
        fx.transform.position = pos;
        var fSr = fx.AddComponent<SpriteRenderer>();
        fSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        fSr.color = new Color(0.4f, 0.85f, 1f, 0.9f);
        fSr.sortingOrder = 5500;

        StartCoroutine(FrostBurstRoutine(fx, fSr));
    }

    private IEnumerator FrostBurstRoutine(GameObject fx, SpriteRenderer fSr)
    {
        float dur = 0.32f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.4f;
        Vector3 endScale = Vector3.one * (shatterRadius * 2.1f);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (fx != null)
            {
                fx.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                if (fSr != null)
                {
                    fSr.color = new Color(0.4f, 0.85f, 1f, Mathf.Lerp(0.9f, 0f, t));
                }
            }
            yield return null;
        }

        if (fx != null) Destroy(fx);
    }

    private static void EnsureCrystalSprite()
    {
        if (cachedCrystalSprite != null) return;

        int w = 26;
        int h = 38;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color iceCore = new Color(0.72f, 0.95f, 1f, 1f);
        Color iceMid = new Color(0.35f, 0.72f, 0.95f, 1f);
        Color iceDeep = new Color(0.15f, 0.38f, 0.65f, 1f);
        Color iceDark = new Color(0.08f, 0.18f, 0.35f, 1f);
        Color frostGlow = new Color(0.92f, 0.98f, 1f, 1f);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            // Độ vát nhọn của cột thạch anh
            float taper = (y / (float)h);
            int halfW = Mathf.RoundToInt(Mathf.Lerp(11f, 1f, taper));

            for (int x = 0; x < w; x++)
            {
                int dist = Mathf.Abs(x - w / 2);
                if (dist > halfW)
                {
                    tex.SetPixel(x, y, clear);
                }
                else if (dist == halfW || y == 0)
                {
                    tex.SetPixel(x, y, iceDark);
                }
                else
                {
                    // Mặt cắt vát ánh sáng bên trái (chói sáng) và bên phải (bóng đổ)
                    if (x < w / 2 - 1)
                    {
                        tex.SetPixel(x, y, (x == w / 2 - 2) ? frostGlow : iceCore);
                    }
                    else if (x == w / 2 - 1 || x == w / 2)
                    {
                        tex.SetPixel(x, y, iceMid);
                    }
                    else
                    {
                        tex.SetPixel(x, y, iceDeep);
                    }
                }
            }
        }

        tex.Apply();
        cachedCrystalSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.4f), 24f);
    }

    public static IceStalagmiteCrystal Create(Vector3 worldPos, Transform parent = null)
    {
        GameObject go = new GameObject("IceStalagmiteCrystal");
        go.transform.position = worldPos;
        if (parent != null) go.transform.SetParent(parent, true);
        return go.AddComponent<IceStalagmiteCrystal>();
    }
}

