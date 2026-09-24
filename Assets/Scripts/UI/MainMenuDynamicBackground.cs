// ========================================================
// VAELSTRIDER: RUINBOUND - Main Menu Dynamic Background
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
    [SerializeField] private Color mysticGoldColor = new Color(1f, 0.85f, 0.35f, 0.85f);
    [SerializeField] private Color mysticCyanColor = new Color(0.35f, 0.9f, 1f, 0.85f);

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
    }

    private List<FloatingEmber> embers = new List<FloatingEmber>();
    private const int EMBER_COUNT = 45;
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
        bgRootObj.transform.SetAsFirstSibling(); // Luôn nằm ở lớp đáy cùng của Canvas

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
        bgArtRt.sizeDelta = new Vector2(100f, 60f); // Slight overflow for smooth parallax & breathing
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

        // 4. Floating Magical Embers / Aethel Sparks Layer
        GameObject embersContainer = new GameObject("EmbersContainer", typeof(RectTransform));
        embersContainer.transform.SetParent(transform, false);
        RectTransform ecRt = embersContainer.GetComponent<RectTransform>();
        ecRt.anchorMin = Vector2.zero;
        ecRt.anchorMax = Vector2.one;
        ecRt.sizeDelta = Vector2.zero;

        embers.Clear();
        for (int i = 0; i < EMBER_COUNT; i++)
        {
            GameObject eObj = new GameObject($"Ember_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            eObj.transform.SetParent(embersContainer.transform, false);

            RectTransform eRt = eObj.GetComponent<RectTransform>();
            float baseSize = Random.Range(3.5f, 9f);
            eRt.sizeDelta = new Vector2(baseSize, baseSize);

            float randX = Random.Range(-960f, 960f);
            float randY = Random.Range(-540f, 540f);
            eRt.anchoredPosition = new Vector2(randX, randY);

            Image eImg = eObj.GetComponent<Image>();
            Color baseCol = (Random.value > 0.45f) ? mysticGoldColor : mysticCyanColor;
            float baseAlpha = Random.Range(0.45f, 0.95f);
            eImg.color = new Color(baseCol.r, baseCol.g, baseCol.b, baseAlpha);
            eImg.raycastTarget = false;

            var eOutline = eObj.AddComponent<Outline>();
            eOutline.effectColor = new Color(baseCol.r, baseCol.g, baseCol.b, 0.6f);
            eOutline.effectDistance = new Vector2(1.2f, -1.2f);

            FloatingEmber ember = new FloatingEmber
            {
                rt = eRt,
                img = eImg,
                startPos = new Vector2(randX, randY),
                floatSpeed = Random.Range(22f, 60f),
                swaySpeed = Random.Range(1.2f, 2.6f),
                swayAmount = Random.Range(15f, 45f),
                phaseOffset = Random.Range(0f, Mathf.PI * 2f),
                baseAlpha = baseAlpha
            };
            embers.Add(ember);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 1. Slow Breathing Zoom / Pan on Background Artwork
        if (bgArtRt != null)
        {
            float breath = Mathf.Sin(timer * 0.45f);
            float scale = 1.02f + breath * 0.025f;
            bgArtRt.localScale = new Vector3(scale, scale, 1f);
        }

        // 2. Update Floating Aethel Sparks & Embers
        for (int i = 0; i < embers.Count; i++)
        {
            var e = embers[i];
            if (e.rt == null) continue;

            Vector2 pos = e.rt.anchoredPosition;
            pos.y += e.floatSpeed * Time.deltaTime;
            pos.x = e.startPos.x + Mathf.Sin(timer * e.swaySpeed + e.phaseOffset) * e.swayAmount;

            // Loop back when reaching top
            if (pos.y > 560f)
            {
                pos.y = -560f;
                e.startPos.x = Random.Range(-960f, 960f);
            }

            e.rt.anchoredPosition = pos;

            // Alpha Twinkle
            if (e.img != null)
            {
                float twinkle = Mathf.Sin(timer * 2.8f + e.phaseOffset);
                float a = e.baseAlpha * (0.65f + 0.35f * twinkle);
                Color ec = e.img.color;
                ec.a = Mathf.Clamp01(a);
                e.img.color = ec;
            }
        }

        // 3. Subtle Mouse / Touch Parallax
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
            Vector2 targetPos = new Vector2(-mouseNorm.x * 15f, -mouseNorm.y * 10f);
            bgArtRt.anchoredPosition = Vector2.Lerp(bgArtRt.anchoredPosition, targetPos, Time.deltaTime * 2.5f);
        }
    }
}
