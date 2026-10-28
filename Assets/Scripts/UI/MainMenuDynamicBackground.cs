// ========================================================
// VAELSTRIDER: RUINBOUND - Main Menu Dynamic Background
// Apocalyptic Ruined City Background (Assets/ART/Backgrounds/Night/WCP_Example.png)
// ========================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuDynamicBackground : MonoBehaviour
{
    public static MainMenuDynamicBackground Instance { get; private set; }

    [Header("Color Palette")]
    [SerializeField] private Color deepBaseColor = new Color(0.12f, 0.13f, 0.11f, 1f);

    private RectTransform rootRt;
    private RectTransform cityArtRt;
    private Image cityArtImg;

    private struct FloatingAsh
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
        public Color ashColor;
    }

    private List<FloatingAsh> ashMotes = new List<FloatingAsh>();
    private const int ASH_COUNT = 25;
    private float timer = 0f;

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
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (cityArtImg == null || cityArtImg.sprite == null)
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

        // 1. Solid Fallback Base
        GameObject baseObj = new GameObject("BaseColor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        baseObj.transform.SetParent(transform, false);
        RectTransform baseRt = baseObj.GetComponent<RectTransform>();
        baseRt.anchorMin = Vector2.zero;
        baseRt.anchorMax = Vector2.one;
        baseRt.sizeDelta = Vector2.zero;
        Image baseImg = baseObj.GetComponent<Image>();
        baseImg.color = deepBaseColor;
        baseImg.raycastTarget = false;

        // 2. Ruined City Master Artwork (WCP_Example.png - Exact Ruined City Skyline)
        Sprite citySprite = LoadSpriteFromAssetPath("Assets/ART/Backgrounds/Night/WCP_Example.png");

        GameObject artObj = new GameObject("RuinedCityArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        artObj.transform.SetParent(transform, false);

        cityArtRt = artObj.GetComponent<RectTransform>();
        cityArtRt.anchorMin = Vector2.zero;
        cityArtRt.anchorMax = Vector2.one;
        // Overflow margin so smooth mouse parallax movement never reveals canvas edges
        cityArtRt.sizeDelta = new Vector2(80f, 50f);
        cityArtRt.anchoredPosition = Vector2.zero;

        cityArtImg = artObj.GetComponent<Image>();
        if (citySprite != null)
        {
            cityArtImg.sprite = citySprite;
            cityArtImg.color = Color.white;
        }
        else
        {
            cityArtImg.color = new Color(0.35f, 0.38f, 0.35f, 1f);
        }
        cityArtImg.preserveAspect = false;
        cityArtImg.raycastTarget = false;

        // 3. Ambient Wasteland Dust / Ash Motes
        GameObject ashContainer = new GameObject("WastelandAshContainer", typeof(RectTransform));
        ashContainer.transform.SetParent(transform, false);
        RectTransform acRt = ashContainer.GetComponent<RectTransform>();
        acRt.anchorMin = Vector2.zero;
        acRt.anchorMax = Vector2.one;
        acRt.sizeDelta = Vector2.zero;

        // Simple soft dot sprite for ash particles
        Sprite dotSpr = GetProceduralSoftDot();

        Color[] ashTints = new Color[]
        {
            new Color(0.85f, 0.88f, 0.84f, 0.40f), // Pale Wasteland Dust
            new Color(0.70f, 0.74f, 0.68f, 0.35f), // Muted Gray-Green Ash
            new Color(0.92f, 0.90f, 0.82f, 0.30f), // Soft Sand/Ruin Particle
            new Color(0.55f, 0.58f, 0.52f, 0.35f)  // Dark Shadow Smudge
        };

        ashMotes.Clear();
        for (int i = 0; i < ASH_COUNT; i++)
        {
            GameObject aObj = new GameObject($"AshMote_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            aObj.transform.SetParent(ashContainer.transform, false);

            RectTransform aRt = aObj.GetComponent<RectTransform>();
            float baseSize = Random.Range(6f, 16f);
            aRt.sizeDelta = new Vector2(baseSize, baseSize);

            float randX = Random.Range(-960f, 960f);
            float randY = Random.Range(-540f, 540f);
            aRt.anchoredPosition = new Vector2(randX, randY);

            Image aImg = aObj.GetComponent<Image>();
            if (dotSpr != null) aImg.sprite = dotSpr;
            aImg.preserveAspect = true;

            Color chosenTint = ashTints[Random.Range(0, ashTints.Length)];
            float baseAlpha = Random.Range(0.20f, 0.45f);
            aImg.color = new Color(chosenTint.r, chosenTint.g, chosenTint.b, baseAlpha);
            aImg.raycastTarget = false;

            FloatingAsh mote = new FloatingAsh
            {
                rt = aRt,
                img = aImg,
                startPos = new Vector2(randX, randY),
                floatSpeed = Random.Range(8f, 18f),
                swaySpeed = Random.Range(0.3f, 0.8f),
                swayAmount = Random.Range(10f, 25f),
                phaseOffset = Random.Range(0f, Mathf.PI * 2f),
                baseAlpha = baseAlpha,
                baseScale = Random.Range(0.85f, 1.25f),
                ashColor = chosenTint
            };
            ashMotes.Add(mote);
        }
    }

    private static Sprite proceduralDot = null;
    private static Sprite GetProceduralSoftDot()
    {
        if (proceduralDot != null) return proceduralDot;

        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color transparent = new Color(1f, 1f, 1f, 0f);
        float center = 15.5f;
        float radius = 15f;

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float a = Mathf.Clamp01(1f - (dist / radius));
                    a = a * a; // Smooth falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        proceduralDot = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
        return proceduralDot;
    }

    private static Sprite LoadSpriteFromAssetPath(string fullAssetPath)
    {
        Sprite spr = null;
#if UNITY_EDITOR
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fullAssetPath);
        if (spr == null)
        {
            var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fullAssetPath);
            if (subAssets != null)
            {
                for (int i = 0; i < subAssets.Length; i++)
                {
                    if (subAssets[i] is Sprite s)
                    {
                        spr = s;
                        break;
                    }
                }
            }
        }
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

        // 1. Unified Mouse Parallax (Moves the entire artwork together so roads/buildings NEVER tear apart)
        Vector2 mouseNorm = Vector2.zero;
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            mouseNorm = new Vector2(
                (mousePos.x / Screen.width - 0.5f) * 2f,
                (mousePos.y / Screen.height - 0.5f) * 2f
            );
        }

        if (cityArtRt != null)
        {
            float breath = Mathf.Sin(timer * 0.3f);
            float scale = 1.02f + breath * 0.01f;
            cityArtRt.localScale = new Vector3(scale, scale, 1f);

            Vector2 targetPos = new Vector2(-mouseNorm.x * 20f, -mouseNorm.y * 14f);
            cityArtRt.anchoredPosition = Vector2.Lerp(cityArtRt.anchoredPosition, targetPos, Time.deltaTime * 2.5f);
        }

        // 2. Update Ambient Wasteland Dust Motes
        for (int i = 0; i < ashMotes.Count; i++)
        {
            var m = ashMotes[i];
            if (m.rt == null) continue;

            Vector2 pos = m.rt.anchoredPosition;
            pos.y += m.floatSpeed * Time.deltaTime;
            pos.x = m.startPos.x + Mathf.Sin(timer * m.swaySpeed + m.phaseOffset) * m.swayAmount;

            if (pos.y > 560f)
            {
                pos.y = -560f;
                m.startPos.x = Random.Range(-960f, 960f);
            }

            m.rt.anchoredPosition = pos;

            if (m.img != null)
            {
                float verticalDist = Mathf.Abs(pos.y);
                float edgeFade = 1f;
                if (verticalDist > 440f)
                {
                    edgeFade = Mathf.Clamp01((560f - verticalDist) / 120f);
                }

                float finalAlpha = m.baseAlpha * edgeFade;
                Color c = m.ashColor;
                c.a = Mathf.Clamp01(finalAlpha);
                m.img.color = c;
            }

            ashMotes[i] = m;
        }
    }
}
