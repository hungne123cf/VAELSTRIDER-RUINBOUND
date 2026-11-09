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
    [Range(0.2f, 0.6f)] public float lowHealthThreshold = 0.50f; // Kích hoạt từ 50% máu trở xuống để người chơi lập tức nhận biết
    [Range(0.05f, 0.3f)] public float criticalHealthThreshold = 0.25f; // Báo động khẩn cấp khi dưới 25% máu

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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        FindPlayerHealth();
        SetupAudio();
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Phím tắt TEST nhanh: Bấm H hoặc K để tự trừ máu xuống 20 HP và xem ngay hiệu ứng đập đỏ!
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.K))
        {
            if (playerHealth != null)
            {
                int targetHp = Mathf.Max(15, (int)(playerHealth.MaxHealth * 0.20f));
                int dmg = playerHealth.CurrentHealth - targetHp;
                if (dmg > 0)
                {
                    playerHealth.TakeDamage(dmg);
                    Debug.Log($"<color=red>[LowHealthVignetteFX TEST]</color> Đã trừ {dmg} HP! Máu hiện tại: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth} ({(float)playerHealth.CurrentHealth / playerHealth.MaxHealth * 100:F0}%). Viền đỏ bắt đầu đập theo nhịp tim!");
                }
            }
        }
#endif

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
            // Tỷ lệ nguy hiểm từ 0.0 (vừa chạm 50%) đến 1.0 (sát tử 0%)
            float dangerFactor = 1f - Mathf.Clamp01(hpPercent / lowHealthThreshold);

            // Tần số nhịp tim: Máu càng thấp tim đập càng nhanh (3.5 Hz -> 8.5 Hz)
            float pulseSpeed = Mathf.Lerp(3.5f, 8.5f, dangerFactor);
            pulseTimer += Time.deltaTime * pulseSpeed;

            // Xung nhịp tim kép (Double beat thump: Lub-Dub)
            float rawSin = Mathf.Sin(pulseTimer);
            float pulseWave = Mathf.Pow(Mathf.Clamp01(rawSin), 2.0f);

            // Độ đậm viền đỏ: dao động nhấp nháy rõ nét theo nhịp đập
            float minAlpha = Mathf.Lerp(0.32f, 0.60f, dangerFactor);
            float maxAlpha = Mathf.Lerp(0.65f, 0.98f, dangerFactor);
            float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulseWave);

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * 6f);

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
            // Máu an toàn (> 50%): Mờ dần êm ái
            currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * 3.0f);
        }

        // Cập nhật màu sắc viền màn hình
        if (vignetteImage != null)
        {
            if (currentAlpha > 0.001f)
            {
                if (!vignetteImage.enabled) vignetteImage.enabled = true;
                // Đỏ thẫm máu Dark Fantasy đậm đà (Crimson Blood Red)
                vignetteImage.color = new Color(0.92f, 0.02f, 0.04f, currentAlpha);
            }
            else
            {
                if (vignetteImage.enabled) vignetteImage.enabled = false;
            }
        }
    }

    private void FadeOutDirect()
    {
        currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * 4.0f);
        if (vignetteImage != null && currentAlpha <= 0.001f && vignetteImage.enabled)
        {
            vignetteImage.enabled = false;
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

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Khoảng cách normalized từ tâm (0 ở tâm, 1 ở mép viền, 1.414 ở 4 góc)
                float dx = (x - center.x) / center.x;
                float dy = (y - center.y) / center.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Tâm màn hình (d < 0.25): trong suốt hoàn toàn để người chơi quan sát nhân vật
                // Từ 0.25 đến 0.95: tăng dần độ đỏ máu lên cực đại 1.0 ở rìa
                if (d < 0.25f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float factor = Mathf.Clamp01((d - 0.25f) / 0.70f);
                    // Đường cong mượt mà Hermite SmoothStep đạt 1.0 tại rìa mép
                    float alpha = Mathf.SmoothStep(0f, 1f, factor);
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
        // Đặt ở lớp hiển thị trên cùng (nhưng raycastTarget = false nên không cản trở nút bấm/joystick)
        vigObj.transform.SetAsLastSibling();

        RectTransform rt = vigObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = vigObj.GetComponent<Image>();
        img.sprite = GetRadialVignetteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0.88f, 0.03f, 0.03f, 0f);
        img.raycastTarget = false; // Tuyệt đối không chặn tương tác chạm
        img.enabled = false; // Tắt renderer khi chưa kích hoạt để 0 draw calls

        vigObj.SetActive(true); // Luôn bật để Update() theo dõi máu nhân vật liên tục!

        var fx = vigObj.AddComponent<LowHealthVignetteFX>();
        fx.vignetteImage = img;
        fx.vignetteRt = rt;

        Instance = fx;
        return fx;
    }
}

