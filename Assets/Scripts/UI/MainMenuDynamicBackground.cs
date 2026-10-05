// ========================================================
// VAELSTRIDER: RUINBOUND - Main Menu Dynamic Background
// Ambient Sparkling Holy Spirit Particles (Holy VFX 01 Impact 1-7)
// ========================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuDynamicBackground : MonoBehaviour
{
    public static MainMenuDynamicBackground Instance { get; private set; }

    [Header("Background Artwork")]
    [SerializeField] private Sprite epicBackgroundSprite;

    [Header("Color Palette")]
    [SerializeField] private Color deepSpaceBase = new Color(0.04f, 0.05f, 0.08f, 1f);

    private RectTransform rootRt;
    private RectTransform bgArtRt;
    private Image bgArtImg;

    private struct FloatingEmber
    {
        public RectTransform rt;
        public Image img;
        public Vector2 startPos;
        public float floatSpeed;
        public float swaySpeed;
        public float swayAmount;
        public float phaseOffset;
        public float baseAlpha;
        public float baseScale;
        public Color goldColor;
        public int currentFrame;
        public float frameTimer;
        public float frameDuration;
    }

    private List<FloatingEmber> embers = new List<FloatingEmber>();
    private const int EMBER_COUNT = 35;
    private float timer = 0f;

    // The exact 7 Holy VFX 01 Impact frames
    private static Sprite[] impactFrames = null;

    public static MainMenuDynamicBackground EnsureBackground(Canvas parentCanvas)
    {
        if (parentCanvas == null)
        {
            parentCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (parentCanvas == null) parentCanvas = Object.FindAnyObjectByType<Canvas>();
        }

        if (parentCanvas == null) return null;

        Transform existing = parentCanvas.transform.Find("DynamicBackgroundRoot");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        GameObject bgRootObj = new GameObject("DynamicBackgroundRoot", typeof(RectTransform), typeof(CanvasRenderer));
        bgRootObj.layer = 5;
        bgRootObj.transform.SetParent(parentCanvas.transform, false);
        bgRootObj.transform.SetAsFirstSibling();

        Instance = bgRootObj.AddComponent<MainMenuDynamicBackground>();
        Instance.BuildBackgroundElements();
        return Instance;
    }

    private void Awake()
    {
        Instance = this;
        if (rootRt == null)
        {
            BuildBackgroundElements();
        }
    }

    public void BuildBackgroundElements()
    {
        rootRt = GetComponent<RectTransform>();
        if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        transform.SetAsFirstSibling();

        // 1. Base Dark Fallback
        GameObject baseObj = new GameObject("BaseColor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        baseObj.transform.SetParent(transform, false);
        RectTransform baseRt = baseObj.GetComponent<RectTransform>();
        baseRt.anchorMin = Vector2.zero;
        baseRt.anchorMax = Vector2.one;
        baseRt.sizeDelta = Vector2.zero;
        Image baseImg = baseObj.GetComponent<Image>();
        baseImg.color = deepSpaceBase;
        baseImg.raycastTarget = false;

        // 2. Load Epic Ruin Artwork Sprite
        if (epicBackgroundSprite == null)
        {
#if UNITY_EDITOR
            epicBackgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Backgrounds/mainmenu_bg_epic.jpg");
#endif
            if (epicBackgroundSprite == null)
            {
                string path = System.IO.Path.Combine(Application.dataPath, "ART/Backgrounds/mainmenu_bg_epic.jpg");
                if (System.IO.File.Exists(path))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(fileData))
                    {
                        epicBackgroundSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
            }
        }

        // 3. Fullscreen Epic Background Image Layer
        GameObject artObj = new GameObject("EpicBgArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        artObj.transform.SetParent(transform, false);
        bgArtRt = artObj.GetComponent<RectTransform>();
        bgArtRt.anchorMin = Vector2.zero;
        bgArtRt.anchorMax = Vector2.one;
        bgArtRt.sizeDelta = new Vector2(100f, 60f);
        bgArtRt.anchoredPosition = Vector2.zero;

        bgArtImg = artObj.GetComponent<Image>();
        if (epicBackgroundSprite != null)
        {
            bgArtImg.sprite = epicBackgroundSprite;
            bgArtImg.color = Color.white;
        }
        else
        {
            bgArtImg.color = new Color(0.08f, 0.12f, 0.18f, 1f);
        }
        bgArtImg.preserveAspect = false;
        bgArtImg.raycastTarget = false;

        // 4. Floating Holy VFX 01 Impact Spirit Layer
        GameObject embersContainer = new GameObject("SpiritSparksContainer", typeof(RectTransform));
        embersContainer.transform.SetParent(transform, false);
        RectTransform ecRt = embersContainer.GetComponent<RectTransform>();
        ecRt.anchorMin = Vector2.zero;
        ecRt.anchorMax = Vector2.one;
        ecRt.sizeDelta = Vector2.zero;

        LoadImpactFrames();

        Color[] goldTints = new Color[]
        {
            new Color(1.0f, 0.95f, 0.70f, 0.95f),  // Bright Radiant Gold
            new Color(1.0f, 0.88f, 0.38f, 0.92f),  // Warm Golden Light
            new Color(1.0f, 0.80f, 0.25f, 0.88f),  // Amber Gold
            new Color(1.0f, 1.00f, 0.90f, 0.98f)   // Pure Holy White-Gold
        };

        embers.Clear();
        for (int i = 0; i < EMBER_COUNT; i++)
        {
            GameObject eObj = new GameObject($"HolySpark_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            eObj.transform.SetParent(embersContainer.transform, false);

            RectTransform eRt = eObj.GetComponent<RectTransform>();
            float baseSize = Random.Range(24f, 40f);
            eRt.sizeDelta = new Vector2(baseSize, baseSize);

            float randX = Random.Range(-920f, 920f);
            float randY = Random.Range(-520f, 520f);
            eRt.anchoredPosition = new Vector2(randX, randY);

            int startFrame = (impactFrames != null && impactFrames.Length > 0) ? Random.Range(0, impactFrames.Length) : 0;

            Image eImg = eObj.GetComponent<Image>();
            if (impactFrames != null && impactFrames.Length > startFrame)
            {
                eImg.sprite = impactFrames[startFrame];
            }
            eImg.preserveAspect = true;

            Color chosenGold = goldTints[Random.Range(0, goldTints.Length)];
            float baseAlpha = Random.Range(0.75f, 0.95f);
            eImg.color = new Color(chosenGold.r, chosenGold.g, chosenGold.b, baseAlpha);
            eImg.raycastTarget = false;

            var eOutline = eObj.AddComponent<Outline>();
            eOutline.effectColor = new Color(1f, 0.8f, 0.25f, 0.4f);
            eOutline.effectDistance = new Vector2(1f, -1f);

            FloatingEmber ember = new FloatingEmber
            {
                rt = eRt,
                img = eImg,
                startPos = new Vector2(randX, randY),
                floatSpeed = Random.Range(12f, 24f), // Slow peaceful upward drift
                swaySpeed = Random.Range(0.5f, 1.2f), // Gentle sway
                swayAmount = Random.Range(10f, 24f),
                phaseOffset = Random.Range(0f, Mathf.PI * 2f),
                baseAlpha = baseAlpha,
                baseScale = Random.Range(0.85f, 1.2f),
                goldColor = chosenGold,
                currentFrame = startFrame,
                frameTimer = Random.Range(0f, 0.16f),
                frameDuration = Random.Range(0.16f, 0.22f) // Gentle, calm frame pacing (~5-6 FPS)
            };
            embers.Add(ember);
        }
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

    private void Update()
    {
        timer += Time.deltaTime;

        // 1. Slow Peaceful Breathing on Background Artwork
        if (bgArtRt != null)
        {
            float breath = Mathf.Sin(timer * 0.35f);
            float scale = 1.02f + breath * 0.02f;
            bgArtRt.localScale = new Vector3(scale, scale, 1f);
        }

        // 2. Update Holy VFX 01 Impact Spirits
        for (int i = 0; i < embers.Count; i++)
        {
            var e = embers[i];
            if (e.rt == null) continue;

            // Gentle Frame Animation across Impact 1-7
            if (impactFrames != null && impactFrames.Length > 0)
            {
                e.frameTimer += Time.deltaTime;
                if (e.frameTimer >= e.frameDuration)
                {
                    e.frameTimer = 0f;
                    e.currentFrame = (e.currentFrame + 1) % impactFrames.Length;
                    if (e.img != null && impactFrames[e.currentFrame] != null)
                    {
                        e.img.sprite = impactFrames[e.currentFrame];
                    }
                }
            }

            Vector2 pos = e.rt.anchoredPosition;
            pos.y += e.floatSpeed * Time.deltaTime;
            pos.x = e.startPos.x + Mathf.Sin(timer * e.swaySpeed + e.phaseOffset) * e.swayAmount;

            // Smooth Loop back when reaching top
            if (pos.y > 560f)
            {
                pos.y = -560f;
                e.startPos.x = Random.Range(-920f, 920f);
            }

            e.rt.anchoredPosition = pos;

            // Gentle Breathing Scale (Very soft)
            float gentlePulse = Mathf.Sin(timer * 1.0f + e.phaseOffset);
            float scale = e.baseScale * (0.95f + 0.07f * gentlePulse);
            e.rt.localScale = new Vector3(scale, scale, 1f);

            // Smooth Edge Fade (Fade in at bottom, fade out at top)
            if (e.img != null)
            {
                float verticalDist = Mathf.Abs(pos.y);
                float edgeFade = 1f;
                if (verticalDist > 440f)
                {
                    edgeFade = Mathf.Clamp01((560f - verticalDist) / 120f);
                }

                float finalAlpha = e.baseAlpha * (0.88f + 0.12f * gentlePulse) * edgeFade;
                Color ec = e.goldColor;
                ec.a = Mathf.Clamp01(finalAlpha);
                e.img.color = ec;
            }

            embers[i] = e;
        }

        // 3. Subtle Parallax
        Vector2 mouseNorm = Vector2.zero;
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            mouseNorm = new Vector2(
                (mousePos.x / Screen.width - 0.5f) * 2f,
                (mousePos.y / Screen.height - 0.5f) * 2f
            );
        }

        if (bgArtRt != null)
        {
            Vector2 targetPos = new Vector2(-mouseNorm.x * 12f, -mouseNorm.y * 8f);
            bgArtRt.anchoredPosition = Vector2.Lerp(bgArtRt.anchoredPosition, targetPos, Time.deltaTime * 2.0f);
        }
    }
}
