using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quan ly cac hieu ung truc quan va co che chien dau cua He Thong Phuc Lanh Dot Bien (Roguelite Blessings / Boons):
/// 1. Chain Lightning (Loi Dinh Lien Hoan): Phong tia set giat lan qua 3 ke dich xung quanh kem hieu ung zigzag dien quang.
/// 2. Hellfire Trail / Magma Patch (Vet Lua Hu Khong / Dung Nham): De lai vet lua thieu dot tren mat dat khi Dash hoac tan cong.
/// 3. Frost Nova (Bang Vuc Tuyet Doi): Soc chan dong bang gia no dien rong lam dong cung quai vat va Boss trong 1.8s.
/// </summary>
public class CombatBlessingFX : MonoBehaviour
{
    private static Material s_lightningMaterial;
    private static Sprite s_fireFlameSprite;
    private static Sprite s_frostRingSprite;

    private static void EnsureResources()
    {
        if (s_lightningMaterial == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (sh != null) s_lightningMaterial = new Material(sh);
        }

        if (s_fireFlameSprite == null)
        {
            // Procedural flame texture (8x8)
            Texture2D tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color cFire = new Color(1f, 0.45f, 0.1f, 1f);
            Color cCore = new Color(1f, 0.95f, 0.3f, 1f);
            Color clear = Color.clear;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 2.5f));
                    if (dist <= 1.5f) tex.SetPixel(x, y, cCore);
                    else if (dist <= 3.2f) tex.SetPixel(x, y, cFire);
                    else tex.SetPixel(x, y, clear);
                }
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            s_fireFlameSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
        }

        if (s_frostRingSprite == null)
        {
            // Procedural frost ring texture (16x16)
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color cIce = new Color(0.4f, 0.85f, 1f, 0.9f);
            Color clear = Color.clear;

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    if (d >= 5.5f && d <= 7.5f) tex.SetPixel(x, y, cIce);
                    else tex.SetPixel(x, y, clear);
                }
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            s_frostRingSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }
    }

    // ─── 1. CHAIN LIGHTNING (LÔI ĐÌNH LIÊN HOÀN) ──────────────────────────────
    public static void TriggerChainLightning(GameObject attacker, GameObject initialTarget, int baseDamage, int maxChains = 3, float chainRadius = 5.0f)
    {
        if (initialTarget == null) return;
        EnsureResources();

        var helper = GetOrCreateHelper();
        helper.StartCoroutine(helper.ChainLightningRoutine(attacker, initialTarget, baseDamage, maxChains, chainRadius));
    }

    private IEnumerator ChainLightningRoutine(GameObject attacker, GameObject initialTarget, int baseDamage, int maxChains, float chainRadius)
    {
        List<GameObject> hitList = new List<GameObject> { initialTarget };
        GameObject current = initialTarget;

        SoundManager.EnsureInstance()?.PlayChainLightning();

        for (int step = 0; step < maxChains; step++)
        {
            if (current == null) break;
            Vector3 currentPos = current.transform.position;

            // Tim ke dich gan nhat trong ban kinh chainRadius ma chua bi giat set
            Collider2D[] colliders = Physics2D.OverlapCircleAll(currentPos, chainRadius);
            GameObject nextTarget = null;
            float nearestDistSqr = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col == null) continue;
                if (!col.CompareTag("Enemy") && !col.CompareTag("Boss")) continue;

                GameObject obj = col.gameObject;
                if (hitList.Contains(obj)) continue;

                float distSqr = (obj.transform.position - currentPos).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nextTarget = obj;
                }
            }

            if (nextTarget == null) break;

            hitList.Add(nextTarget);

            // Ve tia set giat tu current sang nextTarget
            SpawnLightningArc(currentPos, nextTarget.transform.position);

            // Tinh sat thuong giat set (85% base damage)
            int chainDmg = Mathf.Max(15, Mathf.RoundToInt(baseDamage * 0.85f));
            var dmgComp = nextTarget.GetComponent<IDamageable>() ?? nextTarget.GetComponentInParent<IDamageable>();
            if (dmgComp != null)
            {
                dmgComp.TakeDamage(chainDmg);
                DamagePopup.Create(nextTarget.transform.position + Vector3.up * 0.5f, chainDmg, DamageType.Magic, isCritical: false);
            }

            current = nextTarget;
            yield return new WaitForSeconds(0.06f);
        }
    }

    public static void SpawnLightningArc(Vector3 start, Vector3 end)
    {
        EnsureResources();

        GameObject lineObj = new GameObject("FX_LightningArc");
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.material = s_lightningMaterial;
        lr.startColor = new Color(0.4f, 0.95f, 1f, 1f);
        lr.endColor = new Color(1f, 0.9f, 0.3f, 0.8f);
        lr.startWidth = 0.16f;
        lr.endWidth = 0.08f;
        lr.sortingOrder = 5400;

        // Sinh 5 diem zigzag o giua
        int segments = 6;
        Vector3[] points = new Vector3[segments + 1];
        points[0] = start;
        points[segments] = end;

        Vector3 diff = end - start;
        Vector3 perpendicular = Vector3.Cross(diff.normalized, Vector3.forward);

        for (int i = 1; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector3 mid = Vector3.Lerp(start, end, t);
            float offset = Random.Range(-0.25f, 0.25f);
            points[i] = mid + perpendicular * offset;
        }

        lr.positionCount = points.Length;
        lr.SetPositions(points);

        // Them Light2D nhe cho tia set
        var light = lineObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light != null)
        {
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = new Color(0.5f, 0.9f, 1f);
            light.intensity = 1.8f;
            light.pointLightOuterRadius = 1.8f;
            lineObj.transform.position = (start + end) * 0.5f;
        }

        Destroy(lineObj, 0.12f);
    }

    // ─── 2. BLAZING INFERNO (HỎA DIỆM THIÊU ĐỐT BÁM TRÊN CƠ THỂ QUÁI & BOSS) ──
    public static void SpawnHellfirePatch(Vector3 position, float duration = 4.5f, int damagePerTick = 35)
    {
        // Thay thế hoàn toàn vũng dung nham mặt đất bằng ngọn lửa Pixel-Art bám trực tiếp lên cơ thể quái và Boss
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 2.8f);
        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.CompareTag("Enemy") || col.CompareTag("Boss") || col.GetComponentInParent<IDamageable>() != null)
            {
                EnemyBurnEffect.ApplyBurn(col.gameObject, duration: Mathf.Max(3.5f, duration), damagePerTick: damagePerTick, tickInterval: 0.5f);
            }
        }
    }

    // ─── 3. FROST NOVA (BĂNG VỰC TUYỆT ĐỐI) ──────────────────────────────────
    public static void SpawnFrostNova(Vector3 center, float radius = 4.0f, float freezeDuration = 1.8f, int damage = 120)
    {
        EnsureResources();

        GameObject nova = new GameObject("FX_FrostNova");
        nova.transform.position = center;

        var sr = nova.AddComponent<SpriteRenderer>();
        sr.sprite = s_frostRingSprite;
        sr.color = new Color(0.4f, 0.88f, 1f, 0.9f);
        sr.sortingOrder = 5300;

        var light = nova.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light != null)
        {
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = new Color(0.3f, 0.85f, 1f);
            light.intensity = 2.5f;
            light.pointLightOuterRadius = radius;
        }

        var helper = GetOrCreateHelper();
        helper.StartCoroutine(helper.FrostNovaRoutine(nova, sr, light, center, radius, freezeDuration, damage));
    }

    private IEnumerator FrostNovaRoutine(GameObject nova, SpriteRenderer sr, UnityEngine.Rendering.Universal.Light2D light, Vector3 center, float radius, float freezeDuration, int damage)
    {
        SoundManager.EnsureInstance()?.PlayHitImpact(false, true);
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null) cam.Shake(0.15f, 0.25f);

        // Quet sat thuong va dong bang tat ca quai
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.CompareTag("Enemy") || col.CompareTag("Boss"))
            {
                var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (dmg != null)
                {
                    dmg.TakeDamage(damage);
                    DamagePopup.Create(col.transform.position + Vector3.up * 0.65f, damage, DamageType.Magic, isCritical: true);
                }
                EnemyFreezeEffect.ApplyFreeze(col.gameObject, freezeDuration);
            }
        }

        // Hieu ung phong to vong bang roi tan bien
        float animDuration = 0.45f;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float scale = Mathf.Lerp(0.8f, radius * 2.2f, t);
            if (nova != null) nova.transform.localScale = Vector3.one * scale;
            if (sr != null) sr.color = new Color(0.4f, 0.88f, 1f, Mathf.Lerp(0.95f, 0f, t));
            if (light != null) light.intensity = Mathf.Lerp(2.5f, 0f, t);
            yield return null;
        }

        if (nova != null) Destroy(nova);
    }

    // ─── HELPER SINGLETON ──────────────────────────────────────────────────────
    private static CombatBlessingFX s_instance;
    private static CombatBlessingFX GetOrCreateHelper()
    {
        if (s_instance != null) return s_instance;
        GameObject go = new GameObject("CombatBlessingFX_Helper");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<CombatBlessingFX>();
        return s_instance;
    }
}
