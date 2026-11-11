using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hệ thống hiệu ứng Thăng Cấp (Level Up Visual FX) chuẩn các siêu phẩm ARPG / Roguelike (Diablo, Vampire Survivors, Hades)
/// - Cột sáng vàng kim thiên giới (Divine Ascension Pillar) bao bọc thân nhân vật
/// - Vòng sóng xung kích vàng (Expanding Shockwave Ring) tỏa rộng dưới chân
/// - Banner huy hoàng "LÊN CẤP! / LEVEL UP!" hiệu ứng nảy mượt mà (Punch Pop-up) phát sáng hào quang
/// - Đạt chuẩn Zero-Bug Android: Hoàn toàn procedural, không phụ thuộc asset ngoài, không tốn drawcall
/// </summary>
public class PlayerLevelUpFX : MonoBehaviour
{
    private static PlayerLevelUpFX _instance;

    public static PlayerLevelUpFX EnsureExists()
    {
        if (_instance != null) return _instance;
        var existing = FindAnyObjectByType<PlayerLevelUpFX>();
        if (existing != null) { _instance = existing; return _instance; }

        GameObject go = new GameObject("PlayerLevelUpFX_Manager");
        _instance = go.AddComponent<PlayerLevelUpFX>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        PlayerLevelSystem.OnLevelUp += TriggerLevelUpFX;
    }

    void OnDisable()
    {
        PlayerLevelSystem.OnLevelUp -= TriggerLevelUpFX;
    }

    public void TriggerLevelUpFX(int newLevel)
    {
        // Chỉ kích hoạt hiệu ứng khi lên cấp thật (cấp >= 2, không kích hoạt lúc reset map về level 1)
        if (newLevel <= 1) return;

        StartCoroutine(PlayLevelUpSequence(newLevel));
    }

    private IEnumerator PlayLevelUpSequence(int newLevel)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = playerObj != null ? playerObj.transform.position : Vector3.zero;

        // 1. Âm thanh thăng cấp vang dội
        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

        // 2. Tạo Vòng Sóng Xung Kích Vàng Kim (Golden Shockwave Ring) tỏa ra dưới chân
        StartCoroutine(SpawnShockwaveRing(playerPos));

        // 3. Tạo Cột Ánh Sáng Thiên Giới (Ascension Light Pillar)
        StartCoroutine(SpawnAscensionPillar(playerPos));

        // 4. Tạo Banner LÊN CẤP / LEVEL UP bay bổng phát sáng
        StartCoroutine(SpawnLevelUpBanner(playerPos, newLevel));

        yield return null;
    }

    private IEnumerator SpawnShockwaveRing(Vector3 center)
    {
        GameObject ringObj = new GameObject("LevelUp_ShockwaveRing");
        ringObj.transform.position = center + new Vector3(0f, -0.4f, 0f);

        var lr = ringObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 48;
        lr.startWidth = 0.18f;
        lr.endWidth = 0.18f;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0.90f, 0.35f, 1f);
        lr.material = mat;
        lr.sortingOrder = 500;

        float elapsed = 0f;
        float duration = 0.65f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float radius = Mathf.Lerp(0.3f, 4.2f, Mathf.Sin(t * Mathf.PI * 0.5f));
            float alpha = Mathf.Clamp01((1f - t) * 1.5f);

            Color c = new Color(1f, 0.88f, 0.35f, alpha);
            lr.startColor = c;
            lr.endColor = c;
            lr.startWidth = Mathf.Lerp(0.22f, 0.04f, t);
            lr.endWidth = lr.startWidth;

            for (int i = 0; i < 48; i++)
            {
                float angle = (i / 48f) * Mathf.PI * 2f;
                // Nhìn nghiêng theo góc camera 2.5D / isometric (y thu hẹp 0.65x)
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.65f, 0f));
            }

            yield return null;
        }

        Destroy(ringObj);
    }

    private IEnumerator SpawnAscensionPillar(Vector3 center)
    {
        GameObject pillarObj = new GameObject("LevelUp_Pillar");
        pillarObj.transform.position = center;

        var sr = pillarObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetPillarGlowSprite();
        sr.sortingOrder = 510;
        sr.color = new Color(1f, 0.92f, 0.45f, 0.85f);

        float elapsed = 0f;
        float duration = 0.60f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float scaleX = Mathf.Lerp(2.2f, 0.2f, t);
            float scaleY = Mathf.Lerp(1.0f, 5.5f, Mathf.Sqrt(t));
            pillarObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            float alpha = Mathf.Sin(t * Mathf.PI) * 0.90f;
            sr.color = new Color(1f, 0.92f, 0.50f, alpha);

            yield return null;
        }

        Destroy(pillarObj);
    }

    private IEnumerator SpawnLevelUpBanner(Vector3 playerPos, int level)
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) yield break;

        GameObject bannerObj = new GameObject("LevelUp_Banner", typeof(RectTransform), typeof(CanvasGroup));
        bannerObj.transform.SetParent(canvas.transform, false);

        var rt = bannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Chuyển tọa độ nhân vật sang màn hình Canvas
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector2 screenPos = cam.WorldToScreenPoint(playerPos + new Vector3(0f, 1.6f, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(), screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out Vector2 localPoint);
            rt.anchoredPosition = localPoint;
        }
        else
        {
            rt.anchoredPosition = new Vector2(0f, 120f);
        }
        rt.sizeDelta = new Vector2(480f, 90f);

        var cg = bannerObj.GetComponent<CanvasGroup>();

        // Text LÊN CẤP / LEVEL UP
        var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(bannerObj.transform, false);
        var tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = tRt.offsetMax = Vector2.zero;

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.font = GameFontManager.HeaderFont;
        bool isVi = LocalizationManager.IsVietnamese;
        tmp.text = isVi ? $"<size=44><b><color=#FFE066>LÊN CẤP {level}!</color></b></size>" : $"<size=44><b><color=#FFE066>LEVEL UP {level}!</color></b></size>";
        tmp.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.45f, Color.black);

        // Hiệu ứng nảy mượt mà Punch Pop-up (Scale 0.5 -> 1.3 -> 1.0) và trôi nhẹ lên trên
        float elapsed = 0f;
        float duration = 0.85f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Punch scale
            float scale;
            if (t < 0.25f)
            {
                scale = Mathf.Lerp(0.4f, 1.35f, t / 0.25f);
            }
            else if (t < 0.45f)
            {
                scale = Mathf.Lerp(1.35f, 1.0f, (t - 0.25f) / 0.20f);
            }
            else
            {
                scale = 1.0f;
            }

            rt.localScale = new Vector3(scale, scale, 1f);
            rt.anchoredPosition = startPos + new Vector2(0f, Mathf.Lerp(0f, 65f, t));

            if (t > 0.65f)
            {
                cg.alpha = (1f - t) / 0.35f;
            }
            else
            {
                cg.alpha = 1f;
            }

            yield return null;
        }

        Destroy(bannerObj);
    }

    private static Sprite _pillarSprite;
    private static Sprite GetPillarGlowSprite()
    {
        if (_pillarSprite != null) return _pillarSprite;
        const int w = 32;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / h;
            float alphaY = Mathf.Sin(ny * Mathf.PI);
            for (int x = 0; x < w; x++)
            {
                float nx = (float)(x - w * 0.5f) / (w * 0.5f);
                float alphaX = Mathf.Clamp01(1f - Mathf.Abs(nx));
                float a = alphaY * alphaX;
                Color col = Color.Lerp(new Color(1f, 0.85f, 0.25f, a), Color.white, a * 0.7f);
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        _pillarSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 100f);
        return _pillarSprite;
    }
}
