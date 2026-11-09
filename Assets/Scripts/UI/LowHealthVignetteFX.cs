using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Low Health Blood Pulse Vignette System
/// Hiệu ứng màn hình viền đỏ nhấp nháy theo nhịp tim khi nhân vật thấp máu:
/// - Kích hoạt mượt mà khi HP < 30%, cảnh báo cấp bách khi HP < 15%
/// - Tạo xung nhịp tim đập (Heartbeat Pulse) với tần số tăng dần khi máu càng thấp
/// - Tạo Procedural Radial Soft Texture 100% bằng code, tối ưu Zero-Lag cho Android
/// - Tự động ẩn hoàn toàn khi hồi phục máu hoặc khi tử trận
/// </summary>
public class LowHealthVignetteFX : MonoBehaviour
{
    public static LowHealthVignetteFX Instance { get; private set; }

    [Header("Health Thresholds")]
    [Range(0.1f, 0.5f)] public float lowHealthThreshold = 0.30f;
    [Range(0.05f, 0.2f)] public float criticalHealthThreshold = 0.15f;

    [Header("Visual Components")]
    private Image vignetteImage;
    private RectTransform vignetteRt;
    private PlayerHealth playerHealth;

    // Procedural Soft Radial Vignette Sprite
    private static Sprite s_VignetteSprite;

    // Animation state
    private float currentAlpha = 0f;
    private float pulseTimer = 0f;

    // Procedural Heartbeat SFX
    private AudioSource heartbeatAudio;
    private static AudioClip s_HeartbeatClip;
    private float nextHeartbeatTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        FindPlayerHealth();
        SetupAudio();
    }

    void Update()
    {
        if (playerHealth == null)
        {
            FindPlayerHealth();
            if (playerHealth == null)
            {
                FadeOutDirect();
                return;
            }
        }

        int curHp = playerHealth.CurrentHealth;
        int maxHp = playerHealth.MaxHealth;

        // Nếu đã chết hoặc máu đầy/an toàn
        if (curHp <= 0 || maxHp <= 0)
        {
            FadeOutDirect();
            return;
        }

        float hpPercent = (float)curHp / maxHp;

        if (hpPercent < lowHealthThreshold)
        {
            // Tỷ lệ nguy hiểm từ 0.0 (sát tử) đến 1.0 (vừa chạm 30%)
            float dangerFactor = 1f - Mathf.Clamp01(hpPercent / lowHealthThreshold);

            // Tần số nhịp tim: Máu càng thấp tim đập càng nhanh (3.5 Hz -> 8.5 Hz)
            float pulseSpeed = Mathf.Lerp(3.5f, 8.5f, dangerFactor);
            pulseTimer += Time.deltaTime * pulseSpeed;

            // Xung nhịp tim kép (Double beat thump: Lub-Dub)
            float rawSin = Mathf.Sin(pulseTimer);
            float pulseWave = Mathf.Pow(Mathf.Clamp01(rawSin), 2.5f);

            // Độ đậm viền đỏ: dao động nhấp nháy theo nhịp
            float minAlpha = Mathf.Lerp(0.12f, 0.40f, dangerFactor);
            float maxAlpha = Mathf.Lerp(0.38f, 0.85f, dangerFactor);
            float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulseWave);

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * 5f);

            // Âm thanh nhịp tim thình thịch
            if (hpPercent < criticalHealthThreshold && Time.time >= nextHeartbeatTime)
            {
                PlayHeartbeatSFX(dangerFactor);
                float interval = Mathf.Lerp(0.95f, 0.45f, dangerFactor);
                nextHeartbeatTime = Time.time + interval;
            }
        }
        else
        {
            // Máu an toàn (> 30%): Mờ dần êm ái
            currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * 2.5f);
        }

        // Cập nhật màu sắc viền màn hình
        if (vignetteImage != null)
        {
            if (currentAlpha > 0.001f)
            {
                if (!vignetteImage.gameObject.activeSelf) vignetteImage.gameObject.SetActive(true);
                // Đỏ thẫm máu Dark Fantasy (Crimson Red)
                vignetteImage.color = new Color(0.85f, 0.04f, 0.04f, currentAlpha);
            }
            else
            {
                if (vignetteImage.gameObject.activeSelf) vignetteImage.gameObject.SetActive(false);
            }
        }
    }

    private void FadeOutDirect()
    {
        currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * 3.5f);
        if (vignetteImage != null && currentAlpha <= 0.001f && vignetteImage.gameObject.activeSelf)
        {
            vignetteImage.gameObject.SetActive(false);
        }
    }

    private void FindPlayerHealth()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            playerHealth = pc.GetComponent<PlayerHealth>();
        }
        if (playerHealth == null)
        {
            playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        }
    }

    private void SetupAudio()
    {
        if (s_HeartbeatClip == null)
        {
            s_HeartbeatClip = BuildProceduralHeartbeatClip();
        }

        if (heartbeatAudio == null)
        {
            heartbeatAudio = gameObject.AddComponent<AudioSource>();
            heartbeatAudio.playOnAwake = false;
            heartbeatAudio.spatialBlend = 0f; // 2D Stereo
            heartbeatAudio.clip = s_HeartbeatClip;
        }
    }

    private void PlayHeartbeatSFX(float dangerFactor)
    {
        if (heartbeatAudio != null && s_HeartbeatClip != null)
        {
            heartbeatAudio.volume = Mathf.Lerp(0.35f, 0.85f, dangerFactor);
            heartbeatAudio.pitch = Mathf.Lerp(0.90f, 1.25f, dangerFactor);
            heartbeatAudio.PlayOneShot(s_HeartbeatClip);
        }
    }

    private static AudioClip BuildProceduralHeartbeatClip()
    {
        int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * 0.35f);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Hai nhịp đập trầm (Lub: 55Hz tại t=0..0.12s, Dub: 65Hz tại t=0.15..0.28s)
            float env1 = Mathf.Exp(-t * 22f);
            float env2 = t > 0.14f ? Mathf.Exp(-(t - 0.14f) * 26f) : 0f;

            float wave1 = Mathf.Sin(2f * Mathf.PI * 55f * t) * env1;
            float wave2 = Mathf.Sin(2f * Mathf.PI * 68f * (t - 0.14f)) * env2 * 0.75f;

            samples[i] = Mathf.Clamp(wave1 + wave2, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralHeartbeatLowHP", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static Sprite GetRadialVignetteSprite()
    {
        if (s_VignetteSprite != null) return s_VignetteSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDist = center.x * 1.4142f; // Tính tới 4 góc

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Khoảng cách normalized từ tâm (0 ở tâm, 1 ở 4 góc)
                float dx = (x - center.x) / center.x;
                float dy = (y - center.y) / center.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Trong tâm (d < 0.45) trong suốt hoàn toàn, viền ngoài chuyển mịn sang đậm
                if (d < 0.45f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float factor = Mathf.Clamp01((d - 0.45f) / 0.85f);
                    // Đường cong mượt mà quadratic
                    float alpha = factor * factor;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
        }
        tex.Apply();

        s_VignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return s_VignetteSprite;
    }

    /// <summary>
    /// Khởi tạo hoặc tìm kiếm lớp hiển thị viền đỏ trên Canvas Gameplay chính.
    /// </summary>
    public static LowHealthVignetteFX EnsureUIExists()
    {
        if (Instance != null) return Instance;

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = Object.FindAnyObjectByType<Canvas>();
        if (rootCanvas == null) return null;

        Transform existing = rootCanvas.transform.Find("LowHealthVignette_Overlay");
        if (existing != null)
        {
            var comp = existing.GetComponent<LowHealthVignetteFX>();
            if (comp != null)
            {
                Instance = comp;
                return comp;
            }
        }

        GameObject vigObj = new GameObject("LowHealthVignette_Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vigObj.layer = 5;
        vigObj.transform.SetParent(rootCanvas.transform, false);
        // Đặt ở lớp thấp trong Canvas để không đè lên các nút tương tác
        vigObj.transform.SetSiblingIndex(0);

        RectTransform rt = vigObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = vigObj.GetComponent<Image>();
        img.sprite = GetRadialVignetteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0.85f, 0.04f, 0.04f, 0f);
        img.raycastTarget = false; // Tuyệt đối không chặn tương tác chạm
        vigObj.SetActive(false);

        var fx = vigObj.AddComponent<LowHealthVignetteFX>();
        fx.vignetteImage = img;
        fx.vignetteRt = rt;

        Instance = fx;
        return fx;
    }
}
