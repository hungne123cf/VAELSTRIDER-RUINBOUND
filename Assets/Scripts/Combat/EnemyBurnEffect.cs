using UnityEngine;
using System.Collections;

/// <summary>
/// Hiệu ứng HỎA DIỆM THIÊU ĐỐT (On-Body Pixel-Art Burning Fire DoT) dành cho Quái vật & Boss:
/// - Ngọn lửa Pixel-Art nhiều tầng bám trực tiếp lên cơ thể quái vật và Boss.
/// - Liên tục gây sát thương thiêu đốt theo nhịp (tick) cho đến khi hết thời hạn (deadline) của hiệu ứng đốt.
/// - Tự động điều chỉnh kích thước ngọn lửa ôm sát cơ thể Quái thường hoặc Đại Trùm (Boss).
/// </summary>
public class EnemyBurnEffect : MonoBehaviour
{
    private static Sprite[] s_pixelFireFrames;

    private float burnTimer = 0f;
    private float totalDuration = 4.5f;
    private int damagePerTick = 32;
    private float tickInterval = 0.5f;
    private float tickTimer = 0f;
    private float animTimer = 0f;
    private float emberTimer = 0f;
    private int currentFrame = 0;
    private bool isBossTarget = false;

    private IDamageable damageable;
    private IEnemyHealth enemyHealth;
    private GameObject flameRootObj;
    private SpriteRenderer mainFlameSr;
    private SpriteRenderer leftFlameSr;
    private SpriteRenderer rightFlameSr;
    private UnityEngine.Rendering.Universal.Light2D fireLight;
    private SpriteRenderer targetMainSr;

    private const float FRAME_DURATION = 0.085f; // ~12 FPS crisp pixel-art flame animation

    /// <summary>
    /// Áp dụng hiệu ứng Lửa Thiêu Đốt bám trực tiếp lên cơ thể Quái vật / Boss cho đến hết deadline.
    /// </summary>
    public static void ApplyBurn(GameObject target, float duration = 4.5f, int damagePerTick = 32, float tickInterval = 0.5f)
    {
        if (target == null) return;

        // Không áp dụng lên Player hoặc vật phẩm nhặt trên đất
        if (target.CompareTag("Player") || target.GetComponent<PlayerController>() != null || target.GetComponent<PlayerHealth>() != null)
            return;
        if (target.GetComponent<WorldItemPickup>() != null || target.GetComponent<HealthPotion>() != null ||
            target.GetComponent<AethelShard>() != null)
            return;

        // Tìm root của quái/Boss chứa IDamageable
        GameObject enemyRoot = target;
        var dmg = target.GetComponent<IDamageable>();
        if (dmg == null)
        {
            dmg = target.GetComponentInParent<IDamageable>();
            if (dmg is MonoBehaviour mb) enemyRoot = mb.gameObject;
        }
        if (dmg == null) return;

        var health = enemyRoot.GetComponent<IEnemyHealth>() ?? enemyRoot.GetComponentInParent<IEnemyHealth>();
        if (health != null && health.CurrentHealth <= 0) return;

        var existing = enemyRoot.GetComponent<EnemyBurnEffect>();
        if (existing != null)
        {
            existing.RefreshBurn(duration, damagePerTick, tickInterval);
            return;
        }

        var burn = enemyRoot.AddComponent<EnemyBurnEffect>();
        burn.InitializeBurn(dmg, health, duration, damagePerTick, tickInterval);
    }

    private void InitializeBurn(IDamageable dmg, IEnemyHealth health, float duration, int dmgPerTick, float interval)
    {
        EnsurePixelFireSprites();

        damageable = dmg;
        enemyHealth = health;
        isBossTarget = (health != null && health.IsBoss) || gameObject.CompareTag("Boss") || gameObject.name.Contains("Boss");

        totalDuration = Mathf.Max(0.5f, duration);
        burnTimer = totalDuration;
        damagePerTick = Mathf.Max(5, dmgPerTick);
        tickInterval = Mathf.Max(0.2f, interval);
        tickTimer = tickInterval * 0.6f; // Nhịp đốt đầu tiên diễn ra sớm

        targetMainSr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        CreateOnBodyPixelFlames();

        DamagePopup.CreateText(
            transform.position + Vector3.up * (isBossTarget ? 1.65f : 1.05f),
            LocalizationManager.T("BỐC CHÁY!", "BURNING!"),
            new Color(1.0f, 0.48f, 0.12f)
        );
    }

    public void RefreshBurn(float duration, int dmgPerTick, float interval)
    {
        totalDuration = Mathf.Max(totalDuration, duration);
        burnTimer = Mathf.Max(burnTimer, duration);
        damagePerTick = Mathf.Max(damagePerTick, dmgPerTick);
        tickInterval = Mathf.Min(tickInterval, Mathf.Max(0.2f, interval));

        if (flameRootObj == null)
        {
            CreateOnBodyPixelFlames();
        }
    }

    private Collider2D targetMainCol;
    private float cachedFootWorldOffsetY = 0f;

    private void CreateOnBodyPixelFlames()
    {
        if (flameRootObj != null) Destroy(flameRootObj);

        EnsurePixelFireSprites();

        targetMainCol = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();

        flameRootObj = new GameObject("BurnFlame_PixelVFX");
        flameRootObj.transform.SetParent(transform, false);

        // Đo kích thước thực tế (World Space) của Quái/Boss để chỉnh lửa nhỏ gọn đúng phần chân
        float worldWidth = 1.0f;
        float worldHeight = 1.2f;
        float bottomWorldY = transform.position.y - 0.35f;

        if (targetMainCol != null && targetMainCol.bounds.size.y > 0.1f)
        {
            Bounds cb = targetMainCol.bounds;
            worldWidth = Mathf.Clamp(cb.size.x, 0.55f, 3.6f);
            worldHeight = Mathf.Clamp(cb.size.y, 0.65f, 4.0f);
            bottomWorldY = cb.min.y;
        }
        else if (targetMainSr != null && targetMainSr.bounds.size.y > 0.1f)
        {
            Bounds sb = targetMainSr.bounds;
            worldWidth = Mathf.Clamp(sb.size.x * 0.65f, 0.55f, 3.6f);
            worldHeight = Mathf.Clamp(sb.size.y * 0.75f, 0.65f, 4.0f);
            bottomWorldY = sb.min.y;
        }

        // Đặt gốc ngọn lửa ngay tại phần chân (dưới đáy) của quái/Boss
        cachedFootWorldOffsetY = (bottomWorldY - transform.position.y) + (worldHeight * 0.04f);

        // Chiều cao ngọn lửa ngoài thế giới chỉ bằng ~24% chiều cao cơ thể (chỉ cháy dưới chân, không che mặt/thân)
        float desiredFlameWorldHeight = Mathf.Clamp(worldHeight * 0.25f, 0.24f, 0.72f);
        float desiredFlameWorldWidth  = Mathf.Clamp(worldWidth * 0.32f, 0.22f, 0.68f);

        // Khử hoàn toàn hệ số phóng đại (lossyScale) của đối tượng cha (ví dụ Boss scale 1.8x - 2.5x)
        Vector3 lossy = transform.lossyScale;
        float parentScaleX = Mathf.Max(0.25f, Mathf.Abs(lossy.x));
        float parentScaleY = Mathf.Max(0.25f, Mathf.Abs(lossy.y));

        flameRootObj.transform.localPosition = new Vector3(0f, cachedFootWorldOffsetY / parentScaleY, -0.04f);
        flameRootObj.transform.localScale = new Vector3(
            desiredFlameWorldWidth / parentScaleX,
            desiredFlameWorldHeight / parentScaleY,
            1f
        );

        float footSpreadX = 0.42f;
        int baseSortOrder = targetMainSr != null ? targetMainSr.sortingOrder + 2 : 5100;
        int sortLayerId = targetMainSr != null ? targetMainSr.sortingLayerID : 0;

        // 1. Ngọn lửa trung tâm ở ngay dưới chân
        GameObject mainObj = new GameObject("PixelFlame_Main");
        mainObj.transform.SetParent(flameRootObj.transform, false);
        mainObj.transform.localPosition = new Vector3(0f, 0f, 0f);
        mainObj.transform.localScale = Vector3.one * 0.92f;
        mainFlameSr = mainObj.AddComponent<SpriteRenderer>();
        mainFlameSr.sprite = s_pixelFireFrames[0];
        mainFlameSr.sortingLayerID = sortLayerId;
        mainFlameSr.sortingOrder = baseSortOrder + 1;
        mainFlameSr.color = new Color(1f, 1f, 1f, 0.85f);

        // 2. Ngọn lửa nhỏ bên chân trái
        GameObject leftObj = new GameObject("PixelFlame_Left");
        leftObj.transform.SetParent(flameRootObj.transform, false);
        leftObj.transform.localPosition = new Vector3(-footSpreadX, 0.03f, 0f);
        leftObj.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        leftObj.transform.localScale = Vector3.one * 0.68f;
        leftFlameSr = leftObj.AddComponent<SpriteRenderer>();
        leftFlameSr.sprite = s_pixelFireFrames[2 % s_pixelFireFrames.Length];
        leftFlameSr.sortingLayerID = sortLayerId;
        leftFlameSr.sortingOrder = baseSortOrder;
        leftFlameSr.color = new Color(1f, 0.92f, 0.82f, 0.78f);

        // 3. Ngọn lửa nhỏ bên chân phải
        GameObject rightObj = new GameObject("PixelFlame_Right");
        rightObj.transform.SetParent(flameRootObj.transform, false);
        rightObj.transform.localPosition = new Vector3(footSpreadX, 0.02f, 0f);
        rightObj.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        rightObj.transform.localScale = Vector3.one * 0.68f;
        rightFlameSr = rightObj.AddComponent<SpriteRenderer>();
        rightFlameSr.sprite = s_pixelFireFrames[4 % s_pixelFireFrames.Length];
        rightFlameSr.flipX = true;
        rightFlameSr.sortingLayerID = sortLayerId;
        rightFlameSr.sortingOrder = baseSortOrder;
        rightFlameSr.color = new Color(1f, 0.92f, 0.82f, 0.78f);

        // 4. Ánh sáng lửa dịu nhẹ ở dưới chân (không làm lóa hình ảnh quái)
        fireLight = flameRootObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (fireLight != null)
        {
            fireLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            fireLight.color = new Color(1.0f, 0.48f, 0.12f);
            fireLight.intensity = isBossTarget ? 0.65f : 0.45f;
            fireLight.pointLightInnerRadius = 0.15f;
            fireLight.pointLightOuterRadius = isBossTarget ? 1.35f : 0.85f;
        }
    }

    void Update()
    {
        // Kiểm tra nếu mục tiêu đã chết -> tắt lửa ngay
        if (enemyHealth != null && enemyHealth.CurrentHealth <= 0)
        {
            ExtinguishAndDestroy();
            return;
        }

        float dt = Time.deltaTime;
        burnTimer -= dt;
        tickTimer += dt;
        animTimer += dt;
        emberTimer += dt;

        // 1. Chạy hoạt ảnh 6 frame lửa Pixel-Art mượt mà
        if (animTimer >= FRAME_DURATION && s_pixelFireFrames != null && s_pixelFireFrames.Length > 0)
        {
            animTimer -= FRAME_DURATION;
            currentFrame = (currentFrame + 1) % s_pixelFireFrames.Length;

            if (mainFlameSr != null)
                mainFlameSr.sprite = s_pixelFireFrames[currentFrame];
            if (leftFlameSr != null)
                leftFlameSr.sprite = s_pixelFireFrames[(currentFrame + 2) % s_pixelFireFrames.Length];
            if (rightFlameSr != null)
                rightFlameSr.sprite = s_pixelFireFrames[(currentFrame + 4) % s_pixelFireFrames.Length];
        }

        // 2. Đồng bộ sortingOrder để ngọn lửa hiển thị gọn ở chân quái/Boss
        if (targetMainSr != null && mainFlameSr != null)
        {
            int sortOrder = targetMainSr.sortingOrder + 2;
            mainFlameSr.sortingOrder = sortOrder + 1;
            if (leftFlameSr != null) leftFlameSr.sortingOrder = sortOrder;
            if (rightFlameSr != null) rightFlameSr.sortingOrder = sortOrder;
        }

        // 3. Nhấp nháy ánh sáng lửa tự nhiên & giảm dần khi sắp hết deadline
        float fadeAlpha = burnTimer < 0.45f ? Mathf.Clamp01(burnTimer / 0.45f) : 1.0f;
        if (fireLight != null)
        {
            float flicker = 1.0f + Mathf.Sin(Time.time * 18f) * 0.12f;
            fireLight.intensity = (isBossTarget ? 0.65f : 0.45f) * flicker * fadeAlpha;
        }
        if (mainFlameSr != null)
        {
            mainFlameSr.color = new Color(1f, 1f, 1f, 0.85f * fadeAlpha);
        }
        if (leftFlameSr != null)
        {
            leftFlameSr.color = new Color(1f, 0.92f, 0.82f, 0.78f * fadeAlpha);
        }
        if (rightFlameSr != null)
        {
            rightFlameSr.color = new Color(1f, 0.92f, 0.82f, 0.78f * fadeAlpha);
        }

        // 4. Sinh tàn lửa Pixel nhỏ bay nhẹ từ dưới chân
        if (emberTimer >= 0.25f && burnTimer > 0.25f)
        {
            emberTimer = 0f;
            SpawnRisingPixelEmber();
        }

        // 5. Gây sát thương Thiêu Đốt theo chu kỳ cho đến hết deadline
        if (tickTimer >= tickInterval && burnTimer > 0f)
        {
            tickTimer -= tickInterval;
            DealBurnTickDamage();
        }

        // 6. Hết deadline hiệu ứng đốt -> dập lửa hoàn toàn
        if (burnTimer <= 0f)
        {
            ExtinguishAndDestroy();
        }
    }

    private void DealBurnTickDamage()
    {
        if (damageable == null)
        {
            damageable = GetComponent<IDamageable>() ?? GetComponentInParent<IDamageable>();
        }
        if (damageable == null) return;
        if (enemyHealth != null && enemyHealth.CurrentHealth <= 0) return;

        damageable.TakeDamage(damagePerTick);
        Vector3 popupPos = transform.position + new Vector3(Random.Range(-0.25f, 0.25f), isBossTarget ? 0.95f : 0.55f, 0f);
        DamagePopup.Create(popupPos, damagePerTick, DamageType.Magic, isCritical: false);
    }

    private void SpawnRisingPixelEmber()
    {
        if (flameRootObj == null) return;
        GameObject ember = new GameObject("PixelFireEmber");
        float spread = isBossTarget ? 0.32f : 0.16f;
        ember.transform.position = flameRootObj.transform.position + new Vector3(Random.Range(-spread, spread), Random.Range(0.02f, 0.16f), -0.06f);
        ember.transform.localScale = Vector3.one * Random.Range(0.06f, 0.11f);

        var sr = ember.AddComponent<SpriteRenderer>();
        sr.sprite = GetPixelSquareSprite();
        sr.color = Random.value > 0.45f
            ? new Color(1.0f, 0.88f, 0.25f, 0.90f)  // Searing gold spark
            : new Color(1.0f, 0.38f, 0.08f, 0.90f); // Crimson-orange ember
        sr.sortingOrder = (mainFlameSr != null) ? mainFlameSr.sortingOrder + 2 : 5200;

        ember.AddComponent<RisingPixelEmberMover>();
    }

    private void ExtinguishAndDestroy()
    {
        if (flameRootObj != null)
        {
            Destroy(flameRootObj);
            flameRootObj = null;
        }
        Destroy(this);
    }

    void OnDestroy()
    {
        if (flameRootObj != null)
        {
            Destroy(flameRootObj);
            flameRootObj = null;
        }
    }

    // ─── PIXEL-ART SPRITE LOADER & PROCEDURAL PIXEL FALLBACK ──────────────────
    private static Sprite s_pixelSquare;
    private static Sprite GetPixelSquareSprite()
    {
        if (s_pixelSquare != null) return s_pixelSquare;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        s_pixelSquare = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
        return s_pixelSquare;
    }

    private static void EnsurePixelFireSprites()
    {
        if (s_pixelFireFrames != null && s_pixelFireFrames.Length == 6 && s_pixelFireFrames[0] != null)
            return;

        s_pixelFireFrames = new Sprite[6];
        bool allLoaded = true;
        for (int i = 0; i < 6; i++)
        {
            Texture2D loadedTex = Resources.Load<Texture2D>($"Combat/FireBurn/fire_burn_{i}");
            if (loadedTex == null)
            {
                Sprite sp = Resources.Load<Sprite>($"Combat/FireBurn/fire_burn_{i}");
                if (sp != null) loadedTex = sp.texture;
            }
            if (loadedTex != null)
            {
                loadedTex.filterMode = FilterMode.Point;
                s_pixelFireFrames[i] = Sprite.Create(
                    loadedTex,
                    new Rect(0, 0, loadedTex.width, loadedTex.height),
                    new Vector2(0.5f, 0.08f),
                    40f,
                    0,
                    SpriteMeshType.FullRect
                );
            }
            else
            {
                allLoaded = false;
            }
        }

        if (allLoaded) return;

        // Procedural Pixel-Art Fallback (32x40 crisp pixel clusters, FilterMode.Point)
        int W = 32, H = 40;
        Color cClear   = Color.clear;
        Color cRim     = new Color32(98, 12, 22, 220);
        Color cCrimson = new Color32(205, 30, 18, 250);
        Color cOrange  = new Color32(248, 88, 18, 255);
        Color cAmber   = new Color32(255, 165, 24, 255);
        Color cYellow  = new Color32(255, 232, 75, 255);
        Color cWhite   = new Color32(255, 253, 235, 255);

        for (int frame = 0; frame < 6; frame++)
        {
            if (s_pixelFireFrames[frame] != null) continue;

            float phase = (frame / 6.0f) * Mathf.PI * 2.0f;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    tex.SetPixel(x, y, cClear);

            for (int y = 3; y < H - 2; y++)
            {
                float ty = (y - 3.0f) / 31.0f;
                if (ty < 0f || ty > 1.05f) continue;

                float swayMain  = Mathf.Sin(phase - ty * 3.8f) * 2.1f * Mathf.Pow(ty, 1.1f);
                float swayLeft  = Mathf.Sin(phase + 2.1f - ty * 4.2f) * 1.6f * ty;
                float swayRight = Mathf.Sin(phase + 4.2f - ty * 4.2f) * 1.6f * ty;

                float env = (ty < 0.22f)
                    ? Mathf.Sqrt(Mathf.Max(0f, ty / 0.22f))
                    : Mathf.Max(0f, 1.0f - Mathf.Pow((ty - 0.22f) / 0.78f, 1.15f));

                for (int x = 2; x < W - 2; x++)
                {
                    float cx0 = 15.5f + swayMain;
                    float hw0 = Mathf.Max(0.8f, 10.5f * env);
                    float d0  = Mathf.Max(0f, 1.0f - Mathf.Abs(x - cx0) / hw0);

                    float envL = env * Mathf.Max(0f, 1.0f - ty * 0.28f);
                    float cx1  = 10.2f - ty * 1.4f + swayLeft;
                    float hw1  = Mathf.Max(0.6f, 6.2f * envL);
                    float d1   = Mathf.Max(0f, 1.0f - Mathf.Abs(x - cx1) / hw1) * 0.86f;

                    float envR = env * Mathf.Max(0f, 1.0f - ty * 0.25f);
                    float cx2  = 20.8f + ty * 1.4f + swayRight;
                    float hw2  = Mathf.Max(0.6f, 6.2f * envR);
                    float d2   = Mathf.Max(0f, 1.0f - Mathf.Abs(x - cx2) / hw2) * 0.86f;

                    float rawHeat = Mathf.Max(d0, Mathf.Max(d1, d2)) + 0.22f * Mathf.Min(d0, Mathf.Max(d1, d2));
                    float heat = rawHeat * (1.0f - ty * 0.46f);
                    heat += Mathf.Sin(phase * 2.0f + y * 0.65f + x * 0.45f) * 0.06f * ty;

                    if (heat >= 0.72f)      tex.SetPixel(x, y, cWhite);
                    else if (heat >= 0.56f) tex.SetPixel(x, y, cYellow);
                    else if (heat >= 0.41f) tex.SetPixel(x, y, cAmber);
                    else if (heat >= 0.27f) tex.SetPixel(x, y, cOrange);
                    else if (heat >= 0.14f) tex.SetPixel(x, y, cCrimson);
                    else if (heat >= 0.06f) tex.SetPixel(x, y, cRim);
                }
            }

            tex.Apply();
            s_pixelFireFrames[frame] = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.08f), 40f, 0, SpriteMeshType.FullRect);
        }
    }

    private class RisingPixelEmberMover : MonoBehaviour
    {
        private float elapsed = 0f;
        private float lifetime = 0.28f;
        private Vector3 velocity;
        private SpriteRenderer sr;
        private Color baseColor;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) baseColor = sr.color;
            velocity = new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(0.65f, 1.15f), 0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            transform.position += velocity * dt;
            if (sr != null)
            {
                float alpha = Mathf.Clamp01(1f - (elapsed / lifetime));
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
            }
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}

