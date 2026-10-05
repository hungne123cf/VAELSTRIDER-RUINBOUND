// ========================================================
// VAELSTRIDER: RUINBOUND - Map Spirit Sparks Manager
// Ambient Sparkling Holy Spirit Particles (Holy VFX 01 Impact 1-7)
// ========================================================
using UnityEngine;
using System.Collections.Generic;

public class MapSpiritSparksManager : MonoBehaviour
{
    public static MapSpiritSparksManager Instance { get; private set; }

    private static Sprite[] impactFrames = null;

    [Header("Configuration")]
    [SerializeField] private int sparkCount = 40;
    [SerializeField] private float areaWidth = 32f;
    [SerializeField] private float areaHeight = 18f;

    private struct SpiritSpark
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public Transform tf;
        public Vector3 localPos;
        public float floatSpeed;
        public float swaySpeed;
        public float swayAmount;
        public float phaseOffset;
        public float baseScale;
        public float pulseSpeed;
        public Color baseColor;
        public int currentFrame;
        public float frameTimer;
        public float frameDuration;
    }

    private List<SpiritSpark> sparks = new List<SpiritSpark>();
    private Transform cameraTarget;
    private float globalTimer = 0f;

    public static MapSpiritSparksManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject existing = GameObject.Find("MapSpiritSparksManager");
        if (existing != null)
        {
            Instance = existing.GetComponent<MapSpiritSparksManager>();
            if (Instance != null) return Instance;
        }

        GameObject obj = new GameObject("MapSpiritSparksManager");
        Instance = obj.AddComponent<MapSpiritSparksManager>();
        return Instance;
    }

    private static void LoadImpactFrames()
    {
        if (impactFrames != null && impactFrames.Length == 7) return;

        List<Sprite> list = new List<Sprite>();
        for (int i = 1; i <= 7; i++)
        {
            Sprite s = LoadSpriteFromMultiplePaths(
                $"Assets/ART/Item/Holy VFX 01-02/Holy VFX 01/Separated Frames/Holy VFX 01 Impact{i}.png",
                $"Assets/ART/Item/Separated Frames/Holy VFX 01 Impact{i}.png"
            );
            if (s != null) list.Add(s);
        }

        if (list.Count > 0) impactFrames = list.ToArray();
    }

    private static Sprite LoadSpriteFromMultiplePaths(params string[] assetPaths)
    {
        foreach (var path in assetPaths)
        {
            Sprite s = LoadSpriteFromAssetPath(path);
            if (s != null) return s;
        }
        return null;
    }

    private static Sprite LoadSpriteFromAssetPath(string fullAssetPath)
    {
        Sprite spr = null;
#if UNITY_EDITOR
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fullAssetPath);
#endif
        if (spr == null)
        {
            string rel = fullAssetPath.StartsWith("Assets/") ? fullAssetPath.Substring(7) : fullAssetPath;
            string diskPath = System.IO.Path.Combine(Application.dataPath, rel);
            if (System.IO.File.Exists(diskPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(diskPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        return spr;
    }

    private void Awake()
    {
        Instance = this;
        LoadImpactFrames();
        SpawnSparks();
    }

    private void Start()
    {
        FindCameraOrPlayerTarget();
    }

    private void FindCameraOrPlayerTarget()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraTarget = mainCam.transform;
            return;
        }

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            cameraTarget = player.transform;
        }
    }

    private void SpawnSparks()
    {
        LoadImpactFrames();

        Color[] sparkPalettes = new Color[]
        {
            new Color(1.0f, 0.95f, 0.70f, 0.95f),  // Bright Radiant Gold
            new Color(1.0f, 0.88f, 0.38f, 0.92f),  // Warm Golden Light
            new Color(1.0f, 0.80f, 0.25f, 0.88f),  // Amber Gold
            new Color(1.0f, 1.00f, 0.90f, 0.98f)   // Pure Holy White-Gold
        };

        sparks.Clear();

        for (int i = 0; i < sparkCount; i++)
        {
            GameObject sparkObj = new GameObject($"HolySpark_{i}");
            sparkObj.transform.SetParent(transform, false);

            int startFrame = (impactFrames != null && impactFrames.Length > 0) ? Random.Range(0, impactFrames.Length) : 0;

            SpriteRenderer sr = sparkObj.AddComponent<SpriteRenderer>();
            if (impactFrames != null && impactFrames.Length > startFrame)
            {
                sr.sprite = impactFrames[startFrame];
            }
            sr.sortingOrder = 15; // Ambient atmospheric layer

            Color chosenColor = sparkPalettes[Random.Range(0, sparkPalettes.Length)];
            sr.color = chosenColor;

            float baseScale = Random.Range(0.85f, 1.35f);
            sparkObj.transform.localScale = Vector3.one * baseScale;

            Vector3 startPos = new Vector3(
                Random.Range(-areaWidth * 0.5f, areaWidth * 0.5f),
                Random.Range(-areaHeight * 0.5f, areaHeight * 0.5f),
                0f
            );
            sparkObj.transform.position = startPos;

            SpiritSpark spark = new SpiritSpark
            {
                obj = sparkObj,
                sr = sr,
                tf = sparkObj.transform,
                localPos = startPos,
                floatSpeed = Random.Range(0.35f, 0.85f),  // Slow peaceful upward drift
                swaySpeed = Random.Range(0.5f, 1.2f),     // Gentle sway
                swayAmount = Random.Range(0.3f, 0.8f),
                phaseOffset = Random.Range(0f, Mathf.PI * 2f),
                baseScale = baseScale,
                pulseSpeed = Random.Range(0.8f, 1.3f),    // Gentle breathing
                baseColor = chosenColor,
                currentFrame = startFrame,
                frameTimer = Random.Range(0f, 0.16f),
                frameDuration = Random.Range(0.16f, 0.22f) // Pacing ~5-6 FPS for gentle sparkle
            };

            sparks.Add(spark);
        }
    }

    private void Update()
    {
        if (cameraTarget == null)
        {
            FindCameraOrPlayerTarget();
            if (cameraTarget == null) return;
        }

        globalTimer += Time.deltaTime;
        Vector3 centerPos = cameraTarget.position;
        centerPos.z = 0f;

        float halfW = areaWidth * 0.5f;
        float halfH = areaHeight * 0.5f;

        for (int i = 0; i < sparks.Count; i++)
        {
            var s = sparks[i];
            if (s.tf == null || s.sr == null) continue;

            // Animate Impact 1-7 Frames
            if (impactFrames != null && impactFrames.Length > 0)
            {
                s.frameTimer += Time.deltaTime;
                if (s.frameTimer >= s.frameDuration)
                {
                    s.frameTimer = 0f;
                    s.currentFrame = (s.currentFrame + 1) % impactFrames.Length;
                    if (s.sr != null && impactFrames[s.currentFrame] != null)
                    {
                        s.sr.sprite = impactFrames[s.currentFrame];
                    }
                }
            }

            // 1. Slow Peaceful Floating & Sine Sway
            s.localPos.y += s.floatSpeed * Time.deltaTime;
            float swayX = Mathf.Sin(globalTimer * s.swaySpeed + s.phaseOffset) * s.swayAmount;

            // Smooth Loop around bounds
            if (s.localPos.y > halfH)
            {
                s.localPos.y -= areaHeight;
                s.localPos.x = Random.Range(-halfW, halfW);
            }
            else if (s.localPos.y < -halfH)
            {
                s.localPos.y += areaHeight;
                s.localPos.x = Random.Range(-halfW, halfW);
            }

            if (s.localPos.x > halfW)
            {
                s.localPos.x -= areaWidth;
            }
            else if (s.localPos.x < -halfW)
            {
                s.localPos.x += areaWidth;
            }

            s.tf.position = centerPos + s.localPos + new Vector3(swayX, 0f, 0f);

            // 2. Very Gentle Breathing Scale
            float gentlePulse = Mathf.Sin(globalTimer * s.pulseSpeed + s.phaseOffset);
            float scaleMul = 0.95f + 0.07f * gentlePulse;
            s.tf.localScale = Vector3.one * (s.baseScale * scaleMul);

            // 3. Smooth Edge Fade (Prevents abrupt popping)
            float vertDist = Mathf.Abs(s.localPos.y);
            float edgeFade = 1f;
            if (vertDist > (halfH - 2.5f))
            {
                edgeFade = Mathf.Clamp01((halfH - vertDist) / 2.5f);
            }

            Color c = s.baseColor;
            c.a = s.baseColor.a * (0.88f + 0.12f * gentlePulse) * edgeFade;
            s.sr.color = c;

            sparks[i] = s;
        }
    }
}
