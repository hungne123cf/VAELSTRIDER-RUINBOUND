using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Player Sky Descent & Heroic Meteor Entrance FX
/// Hiệu ứng Nhân Vật Giáng Thế từ trên trời lao xuống mặt đất ở đầu mỗi Map (Map 1, Map 2, Map 3 & Calamity):
/// - Khi vào trận, Hero từ độ cao lao vút xuống mặt đất theo gia tốc trọng lực mạnh mẽ.
/// - Bóng đổ dưới chân nở to dần theo khoảng cách tiếp đất.
/// - Khoảnh khắc chạm đất: Rung màn hình (Screen Shake), sóng xung kích (Shockwave Ring),
///   bụi đá văng tỏa (Ground Dust VFX), hiệu ứng nén giãn nhân vật (Squash & Stretch).
/// - Khóa điều khiển & Bất tử trong lúc tiếp đất, sau đó mở khóa mượt mà cho phép di chuyển chiến đấu!
/// </summary>
public class PlayerSkyEntranceFX : MonoBehaviour
{
    private static PlayerSkyEntranceFX _instance;
    public static PlayerSkyEntranceFX Instance => _instance;

    private static Sprite _proceduralShadowSprite;
    private static Sprite _proceduralRingSprite;
    private static Sprite _proceduralDustSprite;

    private Coroutine _entranceCoroutine;
    private bool _isPerformingEntrance = false;

    public static bool IsPerformingEntrance => _instance != null && _instance._isPerformingEntrance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Kích hoạt màn tiếp đất từ trên trời cho Người chơi ở đầu mỗi Map
    /// </summary>
    public static void TriggerEntrance(PlayerController player = null)
    {
        if (PvPMatchManager.IsPvPModeActive) return; // Không áp dụng cho Đấu Trường PvP
        if (BossIntroCutsceneUI.IsCutsceneActive) return;

        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenuScene" || scene == "LoginScene") return;

        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<PlayerController>();
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (player == null) return;

        var fx = player.GetComponent<PlayerSkyEntranceFX>();
        if (fx == null) fx = player.gameObject.AddComponent<PlayerSkyEntranceFX>();

        fx.StartDescent(player);
    }

    public void StartDescent(PlayerController player)
    {
        if (_entranceCoroutine != null)
        {
            StopCoroutine(_entranceCoroutine);
        }
        _entranceCoroutine = StartCoroutine(SkyDescentRoutine(player));
    }

    private IEnumerator SkyDescentRoutine(PlayerController player)
    {
        if (player == null) yield break;
        _isPerformingEntrance = true;

        var ph = player.GetComponent<PlayerHealth>();
        var rb = player.GetComponent<Rigidbody2D>();
        var sr = player.GetComponent<SpriteRenderer>();

        // 1. Khóa di chuyển và kích hoạt bất tử tuyệt đối trong lúc lao xuống
        player.SetCanMove(false);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }
        if (ph != null) ph.isInvulnerable = true;

        Vector3 groundPos = player.transform.position;
        float startHeight = 11.5f;
        Vector3 skyStartPos = groundPos + new Vector3(0f, startHeight, 0f);
        player.transform.position = skyStartPos;

        Vector3 originalScale = player.transform.localScale;

        // 2. Tạo Bóng Đổ Dưới Mặt Đất (Ground Shadow) nở to dần
        GameObject shadowObj = new GameObject("PlayerEntrance_Shadow", typeof(SpriteRenderer));
        shadowObj.transform.position = groundPos + new Vector3(0f, -0.35f, 0f);
        var shadowSr = shadowObj.GetComponent<SpriteRenderer>();
        shadowSr.sprite = GetProceduralShadowSprite();
        shadowSr.color = new Color(0f, 0f, 0f, 0.1f);
        shadowSr.sortingOrder = (sr != null) ? sr.sortingOrder - 1 : 0;
        shadowObj.transform.localScale = new Vector3(0.15f, 0.08f, 1f);

        // 3. Vệt Sáng Khi Lao Xuống (Sky Streak Trail)
        GameObject streakObj = new GameObject("PlayerEntrance_Streak", typeof(LineRenderer));
        streakObj.transform.position = skyStartPos;
        var lr = streakObj.GetComponent<LineRenderer>();
        lr.startWidth = 0.45f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.92f, 0.55f, 0.85f);
        lr.endColor = new Color(1f, 0.65f, 0.2f, 0f);
        lr.positionCount = 2;
        lr.sortingOrder = (sr != null) ? sr.sortingOrder + 1 : 2;

        float fallDuration = 0.65f;
        float elapsed = 0f;

        // Âm thanh vút gió lao từ trên không
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDash();
        }

        while (elapsed < fallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);

            // Đường cong gia tốc rơi tự do mạnh mẽ (t^2)
            float fallProgress = t * t;
            Vector3 currentPos = Vector3.Lerp(skyStartPos, groundPos, fallProgress);
            player.transform.position = currentPos;

            // Bóng nở to dần và đậm màu khi lại gần đất
            float shadowScale = Mathf.Lerp(0.15f, 1.25f, fallProgress);
            shadowObj.transform.localScale = new Vector3(shadowScale, shadowScale * 0.45f, 1f);
            shadowSr.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.1f, 0.75f, fallProgress));

            // Vệt sáng kéo dài phía trên nhân vật
            lr.SetPosition(0, currentPos);
            lr.SetPosition(1, currentPos + new Vector3(0f, Mathf.Min(4.5f, 6.0f * (1f - fallProgress)), 0f));

            yield return null;
        }

        // 4. TIẾP ĐẤT (IMPACT MOMENT)
        player.transform.position = groundPos;
        Destroy(streakObj);

        // Rung màn hình uy lực chuẩn Superhero Landing
        var camFollow = Object.FindAnyObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.Shake(0.32f, 0.38f);
        }

        // Âm thanh va chạm nổ đất
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTitanSlam();
            SoundManager.Instance.PlayHitImpact(isCrit: false, isHeavy: true);
        }

        // Spawn Sóng Xung Kích Hoàng Kim (Golden Shockwave Ring)
        SpawnShockwaveRing(groundPos);

        // Spawn Bụi Đá Văng Tỏa (Ground Dust Puffs)
        SpawnGroundDustPuffs(groundPos);

        // 5. Hiệu Ứng Nén Giãn Cơ Thể (Squash & Stretch Kneel Pose)
        // Nén xuống khi chạm đất
        player.transform.localScale = new Vector3(originalScale.x * 1.30f, originalScale.y * 0.70f, originalScale.z);
        yield return new WaitForSeconds(0.12f);

        // Nảy nhẹ lên
        player.transform.localScale = new Vector3(originalScale.x * 0.95f, originalScale.y * 1.08f, originalScale.z);
        yield return new WaitForSeconds(0.08f);

        // Đứng thẳng hiên ngang
        player.transform.localScale = originalScale;
        yield return new WaitForSeconds(0.10f);

        // 6. Dọn dẹp bóng đổ và khôi phục điều khiển
        float fadeOut = 0.25f;
        float fadeTimer = 0f;
        while (fadeTimer < fadeOut)
        {
            fadeTimer += Time.deltaTime;
            float a = Mathf.Lerp(0.75f, 0f, fadeTimer / fadeOut);
            if (shadowSr != null) shadowSr.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        Destroy(shadowObj);

        if (rb != null)
        {
            rb.isKinematic = false;
        }
        player.SetCanMove(true);

        // Giữ miễn nhiễm sát thương thêm 0.35s bảo hiểm an toàn
        yield return new WaitForSeconds(0.35f);
        if (ph != null) ph.isInvulnerable = false;

        _isPerformingEntrance = false;
        _entranceCoroutine = null;
    }

    private void SpawnShockwaveRing(Vector3 center)
    {
        GameObject ringObj = new GameObject("Entrance_Shockwave", typeof(SpriteRenderer));
        ringObj.transform.position = center + new Vector3(0f, -0.2f, 0f);
        var sr = ringObj.GetComponent<SpriteRenderer>();
        sr.sprite = GetProceduralRingSprite();
        sr.color = new Color(1f, 0.92f, 0.55f, 0.95f);
        sr.sortingOrder = 5;

        StartCoroutine(ShockwaveExpandRoutine(ringObj, sr));
    }

    private IEnumerator ShockwaveExpandRoutine(GameObject ringObj, SpriteRenderer sr)
    {
        float dur = 0.38f;
        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.3f, 0.15f, 1f);
        Vector3 endScale = new Vector3(4.2f, 2.1f, 1f);

        while (elapsed < dur)
        {
            if (ringObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            ringObj.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            sr.color = new Color(1f, 0.92f, 0.55f, Mathf.Lerp(0.95f, 0f, t));
            yield return null;
        }
        if (ringObj != null) Destroy(ringObj);
    }

    private void SpawnGroundDustPuffs(Vector3 center)
    {
        int particleCount = 10;
        for (int i = 0; i < particleCount; i++)
        {
            float angle = (i / (float)particleCount) * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.45f);

            GameObject dust = new GameObject("Entrance_Dust", typeof(SpriteRenderer));
            dust.transform.position = center + new Vector3(dir.x * 0.2f, dir.y * 0.2f - 0.25f, 0f);
            var sr = dust.GetComponent<SpriteRenderer>();
            sr.sprite = GetProceduralDustSprite();
            sr.color = new Color(0.75f, 0.70f, 0.60f, 0.85f);
            sr.sortingOrder = 4;
            dust.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);

            StartCoroutine(DustPuffRoutine(dust, sr, dir));
        }
    }

    private IEnumerator DustPuffRoutine(GameObject dust, SpriteRenderer sr, Vector2 dir)
    {
        float dur = Random.Range(0.35f, 0.55f);
        float elapsed = 0f;
        Vector3 startPos = dust.transform.position;
        Vector3 targetPos = startPos + (Vector3)(dir * Random.Range(1.2f, 2.2f));
        Vector3 startScale = dust.transform.localScale;

        while (elapsed < dur)
        {
            if (dust == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            dust.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
            dust.transform.localScale = startScale * Mathf.Lerp(1f, 1.8f, t);
            sr.color = new Color(0.75f, 0.70f, 0.60f, Mathf.Lerp(0.85f, 0f, t));
            yield return null;
        }
        if (dust != null) Destroy(dust);
    }

    // ─── Procedural High-Perf Sprites ──────────────────────────────────────────
    private static Sprite GetProceduralShadowSprite()
    {
        if (_proceduralShadowSprite != null) return _proceduralShadowSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        tex.Apply();
        _proceduralShadowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _proceduralShadowSprite;
    }

    private static Sprite GetProceduralRingSprite()
    {
        if (_proceduralRingSprite != null) return _proceduralRingSprite;
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.48f;
        float innerR = size * 0.36f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist >= innerR && dist <= outerR)
                {
                    float ringAlpha = Mathf.Sin(((dist - innerR) / (outerR - innerR)) * Mathf.PI);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, ringAlpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        _proceduralRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _proceduralRingSprite;
    }

    private static Sprite GetProceduralDustSprite()
    {
        if (_proceduralDustSprite != null) return _proceduralDustSprite;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01(1f - (dist / radius));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        _proceduralDustSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _proceduralDustSprite;
    }
}
