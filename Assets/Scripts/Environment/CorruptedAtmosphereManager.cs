using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Corrupted Atmosphere & Void Miasma Manager
/// Điều khiển bầu không khí rùng rợn, chết chóc khi người chơi bước vào Vòng Lặp Tha Hóa (Cycle II):
/// - Biến đổi ánh sáng Dungeon sang sắc tím thẫm Hư Không (Abyssal Void Purple)
/// - Đèn lồng của nhân vật bừng cháy ngọn lửa linh hồn tím ma quái
/// - Thêm viền sương mù tử khí (Vignette) đập nhịp xung quanh màn hình
/// - Hiển thị huy hiệu huy hoàng [VÒNG LẶP THA HÓA II: ÁC MỘNG] chuẩn Dash Fantasy
/// </summary>
public class CorruptedAtmosphereManager : MonoBehaviour
{
    private static CorruptedAtmosphereManager instance;
    public static CorruptedAtmosphereManager Instance => instance;

    private GameObject corruptedOverlayRoot;
    private Image vignetteOverlay;
    private TextMeshProUGUI cycleBadgeText;
    private CanvasGroup badgeCanvasGroup;

    private float pulseTimer = 0f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (!CorruptedCycleManager.IsCorruptedActive)
        {
            if (corruptedOverlayRoot != null) Destroy(corruptedOverlayRoot);
            var existingCanvas = GameObject.Find("CorruptedAtmosphereCanvas");
            if (existingCanvas != null) Destroy(existingCanvas);
            Destroy(gameObject);
            return;
        }

        ApplyCorruptedLighting();
        CreateCorruptedHUDOverlay();
        ShowCorruptedAwakeningNotice();
    }

    void Update()
    {
        if (!CorruptedCycleManager.IsCorruptedActive) return;

        // Hiệu ứng thở nhẹ nhàng của viền tử khí (Void Pulse)
        pulseTimer += Time.deltaTime * 1.5f;
        if (vignetteOverlay != null)
        {
            float alpha = Mathf.Lerp(0.25f, 0.42f, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
            vignetteOverlay.color = new Color(0.25f, 0.04f, 0.35f, alpha);
        }
    }

    public static void EnsureAtmosphere()
    {
        if (!CorruptedCycleManager.IsCorruptedActive)
        {
            var existing = Object.FindAnyObjectByType<CorruptedAtmosphereManager>();
            if (existing != null) Destroy(existing.gameObject);
            var existingCanvas = GameObject.Find("CorruptedAtmosphereCanvas");
            if (existingCanvas != null) Destroy(existingCanvas);
            return;
        }

        var existingMgr = Object.FindAnyObjectByType<CorruptedAtmosphereManager>();
        if (existingMgr == null)
        {
            GameObject obj = new GameObject("CorruptedAtmosphereManager");
            obj.AddComponent<CorruptedAtmosphereManager>();
        }
    }

    private void ApplyCorruptedLighting()
    {
        var realm = CorruptedCycleManager.CurrentRealm;
        Color ambientColor;
        Color lanternColor;
        switch (realm)
        {
            case CalamityRealm.VoidNightmare:
                ambientColor = new Color(0.16f, 0.06f, 0.28f, 1f);
                lanternColor = new Color(0.85f, 0.45f, 1.0f, 1f);
                break;
            case CalamityRealm.MagmaCalamity:
                ambientColor = new Color(0.30f, 0.12f, 0.06f, 1f);
                lanternColor = new Color(1.0f, 0.55f, 0.20f, 1f);
                break;
            case CalamityRealm.AcidDownpour:
                ambientColor = new Color(0.10f, 0.24f, 0.08f, 1f);
                lanternColor = new Color(0.35f, 0.95f, 0.35f, 1f);
                break;
            case CalamityRealm.FrostfallBlizzard:
                ambientColor = new Color(0.12f, 0.20f, 0.32f, 1f);
                lanternColor = new Color(0.45f, 0.85f, 1.0f, 1f);
                break;
            default:
                ambientColor = new Color(0.16f, 0.06f, 0.28f, 1f);
                lanternColor = new Color(0.85f, 0.45f, 1.0f, 1f);
                break;
        }

        // 1. Chỉnh Global Light theo cõi thảm họa
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l != null && l.lightType == Light2D.LightType.Global)
            {
                l.color = ambientColor;
                l.intensity = 0.84f;
            }
        }

        // 2. Chỉnh đèn lồng nhân vật theo màu sắc cõi
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Transform lantern = player.transform.Find("PlayerLanternLight2D");
            if (lantern != null)
            {
                var lightComp = lantern.GetComponent<Light2D>();
                if (lightComp != null)
                {
                    lightComp.color = lanternColor;
                    lightComp.pointLightOuterRadius = 6.2f;
                }
            }
        }
    }

    private void CreateCorruptedHUDOverlay()
    {
        var oldBadge = GameObject.Find("CorruptedBadge");
        if (oldBadge != null) Destroy(oldBadge);

        GameObject canvasObj = GameObject.Find("CorruptedAtmosphereCanvas");
        Canvas canvas = null;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("CorruptedAtmosphereCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400; // Dưới UI gameplay để không che thanh Thời Gian
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 400;
        }

        corruptedOverlayRoot = canvasObj;

        Transform existingVig = canvasObj.transform.Find("VignetteOverlay");
        if (existingVig == null)
        {
            GameObject vigObj = new GameObject("VignetteOverlay");
            vigObj.transform.SetParent(canvasObj.transform, false);
            var vigRt = vigObj.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero;
            vigRt.offsetMax = Vector2.zero;

            vignetteOverlay = vigObj.AddComponent<Image>();
            vignetteOverlay.raycastTarget = false;
            vignetteOverlay.sprite = CreateVignetteTexture();
            vignetteOverlay.type = Image.Type.Simple;
            vignetteOverlay.color = new Color(0.25f, 0.04f, 0.35f, 0.35f);
        }
        else
        {
            vignetteOverlay = existingVig.GetComponent<Image>();
        }
    }

    private void ShowCorruptedAwakeningNotice()
    {
        var waveUI = Object.FindAnyObjectByType<WaveUI>();
        if (waveUI != null)
        {
            var realm = CorruptedCycleManager.CurrentRealm;
            string t = $"[CHU KỲ {(int)realm}] {CorruptedCycleManager.GetRealmName(realm)}";
            string sub = "";
            switch (realm)
            {
                case CalamityRealm.VoidNightmare:
                    sub = LocalizationManager.T("Quái vật phát nổ hư không & Trùm sở hữu tuyệt kỹ tàn khốc!", "Void implosions awake & Bosses wield deadly nightmare arts!");
                    break;
                case CalamityRealm.MagmaCalamity:
                    sub = LocalizationManager.T("Mưa thiên thạch & Vũng dung nham thiêu đốt! Hãy vừa đánh vừa né!", "Molten meteors strike & Magma pools burn! Dash and evade!");
                    break;
                case CalamityRealm.AcidDownpour:
                    sub = LocalizationManager.T("Mưa axit ăn mòn! Tấn công quái liên tục để kích hoạt Khiên Trung Hòa!", "Acid rain erodes! Attack relentlessly to gain Neutralization Shields!");
                    break;
                case CalamityRealm.FrostfallBlizzard:
                    sub = LocalizationManager.T("Bão tuyết băng giá! Cẩn trọng hạt tuyết làm chậm & đóng băng khóa chiêu!", "Glacial blizzard howling! Beware chilling & freezing frostflakes!");
                    break;
                default:
                    sub = LocalizationManager.T("Cõi giới thức tỉnh!", "Realm awakened!");
                    break;
            }
            waveUI.ShowBanner(t, sub, 3.8f);
        }
    }

    private Sprite CreateVignetteTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = dist / maxDist;
                // Gradient mờ ở giữa, đậm ở rìa ngoài
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Pow(norm, 1.8f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
