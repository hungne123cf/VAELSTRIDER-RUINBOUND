using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Nguyệt Kiếm Khí Phong Lôi (Gale Crescent Wave):
/// Luồng kiếm khí hình bán nguyệt sắc bén xanh ngọc bão lốc phóng ra khi Gale Ronin Tụ Lực Iaido (Charged Slash).
/// Bay xuyên thấu qua toàn bộ kẻ thù, nổ dấu ấn Phong Lôi và tạo tia chớp xé toạc không gian.
/// </summary>
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class GaleCrescentWave : MonoBehaviour
{
    private Vector2 moveDirection = Vector2.down;
    private int waveDamage = 35;
    private float moveSpeed = 14.5f;
    private float lifetime = 0.75f;
    private float maxDistance = 7.5f;
    private Vector3 spawnPosition;
    private GaleRoninSkill ownerSkill;
    private HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

    private SpriteRenderer sr;
    private Light2D waveLight;
    private static Sprite cachedForwardCrescentSprite;
    private static Sprite cachedSparkSprite;

    public static GaleCrescentWave Spawn(Vector3 origin, Vector2 direction, int damage, float speed = 14.5f, float scale = 0.85f, GaleRoninSkill owner = null)
    {
        GameObject go = new GameObject("GaleCrescentWave");
        go.transform.position = origin;
        var wave = go.AddComponent<GaleCrescentWave>();
        wave.Setup(direction, damage, speed, scale, owner);
        return wave;
    }

    public void Setup(Vector2 direction, int damage, float speed, float scale, GaleRoninSkill owner)
    {
        moveDirection = (direction.sqrMagnitude > 0.001f) ? direction.normalized : Vector2.down;
        waveDamage = damage;
        moveSpeed = speed;
        ownerSkill = owner;
        spawnPosition = transform.position;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetForwardCrescentSprite();
        sr.sortingOrder = 3500;
        sr.color = new Color(0.2f, 1f, 0.88f, 1f);

        // Góc xoay chuẩn: Sprite vẽ mũi kiếm nhọn cong hướng sang Phải (+X), nên góc xoay Atan2(y, x) sẽ luôn chỉ thẳng hướng bay
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one * scale;

        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.85f, 0.55f);

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Dynamic Light2D phát sáng xanh lục lam vừa vặn, không bị chói quá mức
        GameObject lightObj = new GameObject("WaveLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = Vector3.zero;
        waveLight = lightObj.AddComponent<Light2D>();
        waveLight.lightType = Light2D.LightType.Point;
        waveLight.color = new Color(0.1f, 1f, 0.85f, 1f);
        waveLight.intensity = 1.6f;
        waveLight.pointLightInnerRadius = 0.15f;
        waveLight.pointLightOuterRadius = 1.2f;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += (Vector3)(moveDirection * (moveSpeed * Time.deltaTime));

        // Tự hủy nếu vượt quá khoảng cách tối đa
        if (Vector3.Distance(spawnPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        // Tạo vệt tàn khí phong lôi lấp lánh phía sau
        if (Random.value > 0.40f)
        {
            SpawnTrailSpark(transform.position);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.gameObject.CompareTag("Player")) return;

        var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if (damageable != null && !(damageable is PlayerHealth) && !hitDamageables.Contains(damageable))
        {
            hitDamageables.Add(damageable);
            GameObject attacker = ownerSkill != null ? ownerSkill.gameObject : gameObject;
            int actualDamage = AugmentManager.ProcessOutgoingDamage(attacker, other.gameObject, waveDamage);
            damageable.TakeDamage(actualDamage);
            DamagePopup.Create(other.transform.position, actualDamage, isCritical: true);

            SoundManager.EnsureInstance()?.PlayHitImpact();

            // Hiệu ứng tia sét nổ bạo kích trên thân quái
            SpawnHitLightningSparks(other.transform.position);

            AugmentManager.TriggerOnHitAugments(attacker, other.gameObject, actualDamage, damageable);

            if (ownerSkill != null)
            {
                ownerSkill.OnCrescentWaveHit(other.gameObject, damageable, actualDamage);
            }
        }
    }

    private void SpawnTrailSpark(Vector3 pos)
    {
        GameObject spark = new GameObject("GaleSpark");
        spark.transform.position = pos + (Vector3)Random.insideUnitCircle * 0.15f;
        spark.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);

        var sSr = spark.AddComponent<SpriteRenderer>();
        sSr.sprite = GetSparkSprite();
        sSr.color = new Color(0.3f, 1f, 0.9f, 0.75f);
        sSr.sortingOrder = 3400;

        var rb = spark.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = (Vector2)Random.insideUnitCircle * 1.2f - moveDirection * 1.5f;

        StartCoroutine(FadeSparkRoutine(spark, sSr, 0.15f));
    }

    private IEnumerator FadeSparkRoutine(GameObject obj, SpriteRenderer sSr, float duration)
    {
        float elapsed = 0f;
        Color c = (sSr != null) ? sSr.color : Color.white;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (sSr != null) sSr.color = new Color(c.r, c.g, c.b, (1f - t) * c.a);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private void SpawnHitLightningSparks(Vector3 hitPos)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject spk = new GameObject("GaleHitSpike");
            spk.transform.position = hitPos;
            spk.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);

            var sSr = spk.AddComponent<SpriteRenderer>();
            sSr.sprite = GetSparkSprite();
            sSr.color = new Color(1f, 1f, 1f, 0.95f);
            sSr.sortingOrder = 3600;

            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var rb = spk.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(4.5f, 7.5f);

            StartCoroutine(FadeSparkRoutine(spk, sSr, 0.14f));
        }
    }

    private static Sprite GetForwardCrescentSprite()
    {
        if (cachedForwardCrescentSprite != null) return cachedForwardCrescentSprite;

        int w = 36;
        int h = 36;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color core = new Color(1f, 1f, 1f, 1f);
        Color glow = new Color(0.15f, 1f, 0.85f, 0.95f);
        Color edge = new Color(0.05f, 0.7f, 0.95f, 0.45f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Tọa độ chuẩn hóa: +X là phía trước (hướng đâm tới), Y là sải cánh 2 bên (-1 đến 1)
                float nx = (float)x / (w - 1); // 0 (đuôi) -> 1 (mũi đâm phía trước)
                float ny = (float)y / (h - 1) * 2f - 1f; // -1 (cánh dưới) -> 1 (cánh trên)

                // Đường cong mũi kiếm bán nguyệt nhô về phía +X
                float curveApex = 1f - (ny * ny) * 0.78f; // Điểm đỉnh của lưỡi kiếm
                float thickness = 0.26f;

                if (nx <= curveApex && nx >= (curveApex - thickness))
                {
                    float distFromEdge = curveApex - nx;
                    float tipFade = 1f - Mathf.Abs(ny) * 0.45f; // Đậm ở giữa, vuốt nhọn ở 2 đầu

                    if (distFromEdge < 0.08f)
                    {
                        tex.SetPixel(x, y, new Color(core.r, core.g, core.b, core.a * tipFade));
                    }
                    else if (distFromEdge < 0.18f)
                    {
                        tex.SetPixel(x, y, new Color(glow.r, glow.g, glow.b, glow.a * tipFade));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(edge.r, edge.g, edge.b, edge.a * tipFade));
                    }
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        cachedForwardCrescentSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return cachedForwardCrescentSprite;
    }

    private static Sprite GetSparkSprite()
    {
        if (cachedSparkSprite != null) return cachedSparkSprite;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float dy = Mathf.Abs(y - center) / center;
                float d = dx + dy;
                if (d < 1f)
                {
                    float a = 1f - d;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        cachedSparkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return cachedSparkSprite;
    }
}
