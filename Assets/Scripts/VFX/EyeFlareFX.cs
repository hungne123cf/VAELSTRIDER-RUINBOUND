using UnityEngine;
using System.Collections;

/// <summary>
/// Hiệu ứng Ánh Mắt Rực Lửa (Eye Flare / Eye Glint VFX) khi nhân vật kích hoạt Chiêu Cuối (Phím F).
/// Tạo tia sáng lóe sắc nét từ 2 mắt nhân vật, vệt lửa kéo dài bùng cháy dữ dội và âm thanh chớp mắt anime.
/// </summary>
public class EyeFlareFX : MonoBehaviour
{
    private static Sprite cachedStarSprite;
    private static Sprite cachedFlameSprite;

    private GameObject flareRoot;
    private SpriteRenderer leftFlareSR;
    private SpriteRenderer rightFlareSR;
    private SpriteRenderer flameTrailSR;
    private Coroutine activeRoutine;

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        LoadSpritesIfNeeded();
    }

    private void LoadSpritesIfNeeded()
    {
        if (cachedStarSprite == null)
        {
#if UNITY_EDITOR
            cachedStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/VFX/eye_flare_star.png");
#endif
            if (cachedStarSprite == null)
            {
                Texture2D tex = CreateProceduralStarTexture();
                cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            }
        }

        if (cachedFlameSprite == null)
        {
#if UNITY_EDITOR
            cachedFlameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/VFX/eye_flame_trail.png");
#endif
            if (cachedFlameSprite == null)
            {
                Texture2D tex = CreateProceduralFlameTexture();
                cachedFlameSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.1f, 0.5f), 100);
            }
        }
    }

    public void TriggerEyeFlare(float duration = 1.0f, bool isMaid = false)
    {
        LoadSpritesIfNeeded();

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(EyeFlareRoutine(duration, isMaid));
    }

    private IEnumerator EyeFlareRoutine(float duration, bool isMaid)
    {
        EnsureFlareObjects();

        // Âm thanh lóe mắt anime SHIIING
        SoundManager.EnsureInstance()?.PlayEyeFlareGlint();

        Color mainFlareColor = isMaid 
            ? new Color(1f, 0.15f, 0.55f, 1f)  // Ruby pink cho Maid
            : new Color(1f, 0.35f, 0.05f, 1f); // Rực đỏ cam lửa cho Vaelstrider

        Color hotCoreColor = new Color(1f, 0.95f, 0.8f, 1f);

        Vector2 facing = playerController != null ? playerController.GetFacingDirection() : Vector2.down;
        if (facing == Vector2.zero) facing = Vector2.down;

        // Tọa độ 2 mắt dựa trên chiều hướng nhân vật
        float eyeY = 0.38f;
        float eyeSpread = 0.09f;
        float xOffset = facing.x * 0.04f;

        Vector3 leftEyeLocal = new Vector3(-eyeSpread + xOffset, eyeY, -0.05f);
        Vector3 rightEyeLocal = new Vector3(eyeSpread + xOffset, eyeY, -0.05f);

        if (leftFlareSR != null)
        {
            leftFlareSR.transform.localPosition = leftEyeLocal;
            leftFlareSR.gameObject.SetActive(true);
            leftFlareSR.color = hotCoreColor;
        }

        if (rightFlareSR != null)
        {
            rightFlareSR.transform.localPosition = rightEyeLocal;
            rightFlareSR.gameObject.SetActive(true);
            rightFlareSR.color = hotCoreColor;
        }

        if (flameTrailSR != null)
        {
            // Vệt lửa bùng cháy từ mắt chiếm ưu thế theo hướng nhìn
            Vector3 dominantEye = (facing.x >= 0) ? rightEyeLocal : leftEyeLocal;
            flameTrailSR.transform.localPosition = dominantEye + new Vector3(0f, 0.02f, -0.06f);
            flameTrailSR.gameObject.SetActive(true);
            flameTrailSR.color = mainFlareColor;

            float trailAngle = (facing.x >= 0) ? 15f : 165f;
            flameTrailSR.transform.localRotation = Quaternion.Euler(0f, 0f, trailAngle);
        }

        flareRoot.SetActive(true);

        // ==========================================
        // GIAI ĐOẠN 1: BÙNG LÓE TỨC THÌ (Flash Bang) trong 0.1s
        // ==========================================
        float flashTime = 0.09f;
        float elapsed = 0f;

        while (elapsed < flashTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashTime);

            // Phóng to cực đại tia chớp mắt
            float scale = Mathf.Lerp(0.1f, 1.8f, t);
            Vector3 s = new Vector3(scale * 1.5f, scale * 1.1f, 1f);

            if (leftFlareSR != null) leftFlareSR.transform.localScale = s;
            if (rightFlareSR != null) rightFlareSR.transform.localScale = s;

            if (flameTrailSR != null)
            {
                flameTrailSR.transform.localScale = new Vector3(scale * 1.4f, scale * 0.9f, 1f);
            }

            yield return null;
        }

        // ==========================================
        // GIAI ĐOẠN 2: LỬA MẮT RỰC CHÁY (Burning Flare)
        // ==========================================
        float burnDuration = Mathf.Max(0.2f, duration - flashTime - 0.18f);
        elapsed = 0f;

        while (elapsed < burnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // Dao động ngọn lửa bập bùng (flicker)
            float flicker = 1.0f + 0.25f * Mathf.Sin(Time.unscaledTime * 35f);
            float pulseScale = Mathf.Lerp(1.3f, 1.0f, elapsed / burnDuration) * flicker;

            Vector3 s = new Vector3(pulseScale * 1.25f, pulseScale * 0.95f, 1f);
            if (leftFlareSR != null)
            {
                leftFlareSR.transform.localScale = s;
                leftFlareSR.color = Color.Lerp(hotCoreColor, mainFlareColor, 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * 20f));
            }

            if (rightFlareSR != null)
            {
                rightFlareSR.transform.localScale = s;
                rightFlareSR.color = Color.Lerp(hotCoreColor, mainFlareColor, 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * 20f));
            }

            if (flameTrailSR != null)
            {
                float trailLen = (1.5f + 0.4f * Mathf.Sin(Time.unscaledTime * 28f)) * flicker;
                flameTrailSR.transform.localScale = new Vector3(trailLen, 0.8f * flicker, 1f);
                flameTrailSR.color = Color.Lerp(mainFlareColor, Color.yellow, 0.25f * Mathf.Sin(Time.unscaledTime * 30f));
            }

            yield return null;
        }

        // ==========================================
        // GIAI ĐOẠN 3: TẮT DẦN ÊM ÁI (Fade Out)
        // ==========================================
        float fadeOutTime = 0.18f;
        elapsed = 0f;

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutTime);
            float alpha = 1f - t;

            if (leftFlareSR != null)
            {
                Color c = leftFlareSR.color;
                c.a = alpha;
                leftFlareSR.color = c;
            }

            if (rightFlareSR != null)
            {
                Color c = rightFlareSR.color;
                c.a = alpha;
                rightFlareSR.color = c;
            }

            if (flameTrailSR != null)
            {
                Color c = flameTrailSR.color;
                c.a = alpha;
                flameTrailSR.color = c;
            }

            yield return null;
        }

        if (flareRoot != null) flareRoot.SetActive(false);
        activeRoutine = null;
    }

    private void EnsureFlareObjects()
    {
        if (flareRoot == null)
        {
            flareRoot = new GameObject("EyeFlare_VFX_Root");
            flareRoot.transform.SetParent(transform, false);
            flareRoot.transform.localPosition = Vector3.zero;

            // Mắt trái
            GameObject leftObj = new GameObject("LeftEyeFlare");
            leftObj.transform.SetParent(flareRoot.transform, false);
            leftFlareSR = leftObj.AddComponent<SpriteRenderer>();
            leftFlareSR.sprite = cachedStarSprite;
            leftFlareSR.sortingLayerName = "UI";
            leftFlareSR.sortingOrder = 950;

            // Mắt phải
            GameObject rightObj = new GameObject("RightEyeFlare");
            rightObj.transform.SetParent(flareRoot.transform, false);
            rightFlareSR = rightObj.AddComponent<SpriteRenderer>();
            rightFlareSR.sprite = cachedStarSprite;
            rightFlareSR.sortingLayerName = "UI";
            rightFlareSR.sortingOrder = 950;

            // Vệt lửa đuôi mắt (Flame trail)
            GameObject trailObj = new GameObject("FlameTrail");
            trailObj.transform.SetParent(flareRoot.transform, false);
            flameTrailSR = trailObj.AddComponent<SpriteRenderer>();
            flameTrailSR.sprite = cachedFlameSprite;
            flameTrailSR.sortingLayerName = "UI";
            flameTrailSR.sortingOrder = 940;
        }
    }

    private Texture2D CreateProceduralStarTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float radial = Mathf.Pow(Mathf.Max(0f, 1f - dist), 2f);
                float hSpike = Mathf.Exp(-Mathf.Abs(dy) * 14f) * Mathf.Max(0f, 1f - Mathf.Abs(dx));
                float vSpike = Mathf.Exp(-Mathf.Abs(dx) * 14f) * Mathf.Max(0f, 1f - Mathf.Abs(dy));

                float val = Mathf.Clamp01(radial * 0.5f + hSpike * 0.9f + vSpike * 0.7f);
                Color c = Color.Lerp(new Color(1f, 0.2f, 0.05f, val), Color.white, Mathf.Pow(val, 3f));
                c.a = val;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }

    private Texture2D CreateProceduralFlameTexture()
    {
        int w = 128, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w;
                float v = (y - h * 0.5f) / (h * 0.5f);
                float curve = 0.15f * Mathf.Sin(u * Mathf.PI * 2.5f);
                float dy = Mathf.Abs(v - curve);
                float thickness = (1f - u) * 0.5f + 0.1f;
                float val = Mathf.Clamp01(1f - (dy / thickness)) * (1f - u * 0.7f);

                Color c = Color.Lerp(new Color(1f, 0.15f, 0.05f, val), Color.yellow, 1f - u);
                c.a = val;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }
}
