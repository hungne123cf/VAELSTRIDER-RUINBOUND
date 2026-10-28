using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CircleCollider2D))]
public class SpiderWebProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed = 8.5f;
    private int damage = 6;
    private float stunDuration = 2.0f;
    private bool hasHit = false;

    private SpriteRenderer sr;
    private Sprite[] bulletSprites;
    private int currentFrame = 0;
    private float animTimer = 0f;
    private float trailTimer = 0f;

    public static void Launch(Vector3 startPos, Vector2 dir)
    {
        GameObject go = new GameObject("SpiderWeb_Projectile");
        go.transform.position = startPos;
        var proj = go.AddComponent<SpiderWebProjectile>();
        proj.Init(dir);
    }

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        damage = MapScalingManager.ScaleDamage(damage);

        sr = gameObject.AddComponent<SpriteRenderer>();
        bulletSprites = SpiderWebSpriteFactory.GetBulletSprites();
        if (bulletSprites != null && bulletSprites.Length > 0 && bulletSprites[0] != null)
        {
            sr.sprite = bulletSprites[0];
        }

        Color realmColor;
        Color lightCol;
        switch (CorruptedCycleManager.CurrentRealm)
        {
            case CalamityRealm.VoidNightmare:
                realmColor = new Color(0.20f, 0.05f, 0.35f, 1f);
                lightCol = new Color(0.6f, 0.1f, 0.9f, 1f);
                break;
            case CalamityRealm.MagmaCalamity:
                realmColor = new Color(1.0f, 0.40f, 0.05f, 1f);
                lightCol = new Color(1.0f, 0.6f, 0.1f, 1f);
                break;
            case CalamityRealm.AcidDownpour:
                realmColor = new Color(0.25f, 1.0f, 0.35f, 1f);
                lightCol = new Color(0.3f, 1.0f, 0.4f, 1f);
                break;
            case CalamityRealm.FrostfallBlizzard:
                realmColor = new Color(0.85f, 0.95f, 1.0f, 1f);
                lightCol = new Color(0.5f, 0.85f, 1.0f, 1f);
                break;
            default:
                realmColor = Color.white;
                lightCol = new Color(0.9f, 0.95f, 1f, 1f);
                break;
        }

        sr.color = realmColor;
        sr.sortingOrder = 2500;
        transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        // Ánh sáng dạ quang theo hệ nguyên tố
        GameObject lightObj = new GameObject("Web_Light");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = Vector3.zero;
        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = lightCol;
        light.intensity = 1.6f;
        light.pointLightInnerRadius = 0.2f;
        light.pointLightOuterRadius = 1.6f;

        var col = GetComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = true;

        Destroy(gameObject, 3.5f);
    }

    void Update()
    {
        if (hasHit) return;

        // Di chuyen bay ve phia truoc
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Xoay tron vien dan to nhen
        transform.Rotate(0f, 0f, 480f * Time.deltaTime);

        // Animation cuon to nhen trang (4 frame)
        animTimer += Time.deltaTime;
        if (animTimer >= 0.08f)
        {
            animTimer = 0f;
            currentFrame = (currentFrame + 1) % 4;
            if (bulletSprites != null && bulletSprites.Length > currentFrame && bulletSprites[currentFrame] != null)
            {
                sr.sprite = bulletSprites[currentFrame];
            }
        }

        // De lai cac vet to nhen nho li ti bay sau duoi dan (Trail)
        trailTimer += Time.deltaTime;
        if (trailTimer >= 0.06f)
        {
            trailTimer = 0f;
            SpawnWebTrailDust();
        }
    }

    private void SpawnWebTrailDust()
    {
        GameObject trail = new GameObject("Web_TrailDust");
        trail.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 0.15f;
        var trailSr = trail.AddComponent<SpriteRenderer>();
        if (bulletSprites != null && bulletSprites[0] != null) trailSr.sprite = bulletSprites[0];
        trailSr.color = new Color(1f, 1f, 1f, 0.6f);
        trailSr.sortingOrder = 2490;
        trail.transform.localScale = Vector3.one * 0.35f;

        StartCoroutine(FadeAndDestroyRoutine(trail, trailSr));
    }

    private IEnumerator FadeAndDestroyRoutine(GameObject obj, SpriteRenderer tsr)
    {
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            if (tsr != null) tsr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.6f, 0f, t / 0.2f));
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 1. Va cham voi Player: Gay Stun 2s & Sat thuong
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            hasHit = true;

            var pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.ApplyStun(stunDuration);
            }

            var dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
            }

            SpiderWebSpriteFactory.SpawnWebBreakVFX(transform.position);
            SoundManager.EnsureInstance()?.PlayHitImpact();
            Destroy(gameObject);
        }
        // 2. Va cham voi Cay coi / Goc cay / Tang da: Cay lam khien chan dan to nhen!
        else if (other.GetComponent<TreeOcclusionFader>() != null ||
                 other.GetComponent<TreeWindSway>() != null ||
                 other.name.ToLower().Contains("tree") ||
                 other.name.ToLower().Contains("stump") ||
                 other.name.ToLower().Contains("boulder") ||
                 other.name.ToLower().Contains("rock") ||
                 other.name.ToLower().Contains("pillar"))
        {
            hasHit = true;
            SpiderWebSpriteFactory.SpawnWebBreakVFX(transform.position);
            SoundManager.EnsureInstance()?.PlayHitImpact();
            Destroy(gameObject);
        }
    }
}