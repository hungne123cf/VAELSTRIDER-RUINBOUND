using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tao hieu ung phat sang huyen bi nhap nhay tren cac khe nut cua tru da co dai
/// </summary>
public class GlowingRunePillar : MonoBehaviour
{
    [Header("Phat sang tren khe nut")]
    [SerializeField] private SpriteRenderer glowCrackRenderer;
    [SerializeField] private Light2D crackLight;

    [Header("Thong so nhip tho phat quang")]
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 1.0f;
    [SerializeField] private Color runeColor = new Color(0.45f, 0.95f, 1f, 1f); // Arcane Cyan
    [SerializeField] private float minLightIntensity = 0.7f;
    [SerializeField] private float maxLightIntensity = 2.2f;

    private float randomPhase;

    void Start()
    {
        randomPhase = transform.position.x * 2.1f + transform.position.y * 3.4f;

        if (glowCrackRenderer == null)
        {
            Transform glowChild = transform.Find("CrackGlow");
            if (glowChild != null) glowCrackRenderer = glowChild.GetComponent<SpriteRenderer>();
        }

        if (crackLight == null)
        {
            crackLight = GetComponentInChildren<Light2D>();
        }
    }

    void Update()
    {
        // Tinh toan nhip tho dao dong mem mai tu 0 den 1
        float pulse = (Mathf.Sin(Time.time * pulseSpeed + randomPhase) + 1f) * 0.5f;

        // 1. Nhap nhay mau va do trong suot tren khe nut
        if (glowCrackRenderer != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            glowCrackRenderer.color = new Color(runeColor.r, runeColor.g, runeColor.b, alpha);
        }

        // 2. Nhap nhay cuong do Light2D lan toa tren mat dat
        if (crackLight != null)
        {
            crackLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, pulse);
        }
    }

    public static GlowingRunePillar Create(GameObject pillarObj, Sprite crackSprite, Color glowColor)
    {
        var comp = pillarObj.GetComponent<GlowingRunePillar>();
        if (comp == null) comp = pillarObj.AddComponent<GlowingRunePillar>();
        comp.runeColor = glowColor;

        // Tao sprite phat sang de len tren khe nut
        Transform existingGlow = pillarObj.transform.Find("CrackGlow");
        if (existingGlow != null) Object.DestroyImmediate(existingGlow.gameObject);

        GameObject glowObj = new GameObject("CrackGlow");
        glowObj.transform.SetParent(pillarObj.transform, false);
        glowObj.transform.localPosition = Vector3.zero;

        var sr = glowObj.AddComponent<SpriteRenderer>();
        sr.sprite = crackSprite;
        var parentSr = pillarObj.GetComponent<SpriteRenderer>();
        if (parentSr != null)
        {
            sr.sortingLayerID = parentSr.sortingLayerID;
            sr.sortingOrder = parentSr.sortingOrder + 1; // Nam ngay phia tren mat tru da
        }
        sr.color = glowColor;
        comp.glowCrackRenderer = sr;

        // Tao Light2D toa sang tu khe nut
        Transform existingLight = pillarObj.transform.Find("CrackLight");
        if (existingLight != null) Object.DestroyImmediate(existingLight.gameObject);

        GameObject lightObj = new GameObject("CrackLight");
        lightObj.transform.SetParent(pillarObj.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = glowColor;
        light.intensity = 1.5f;
        light.pointLightInnerRadius = 0.3f;
        light.pointLightOuterRadius = 3.2f;
        comp.crackLight = light;

        // Am thanh khong gian 3D ngan vang co ngu (Spatial Rune Hum)
        AudioSource existingAudio = pillarObj.GetComponent<AudioSource>();
        if (existingAudio == null) existingAudio = pillarObj.AddComponent<AudioSource>();
#if UNITY_EDITOR
        AudioClip humClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/rune_pillar_hum.wav");
        if (humClip != null)
        {
            existingAudio.clip = humClip;
            existingAudio.loop = true;
            existingAudio.playOnAwake = true;
            existingAudio.spatialBlend = 0.85f;
            existingAudio.minDistance = 1.2f;
            existingAudio.maxDistance = 6.0f;
            existingAudio.rolloffMode = AudioRolloffMode.Linear;
            existingAudio.volume = 0.32f;
            existingAudio.Play();
        }
#endif

        return comp;
    }
}
