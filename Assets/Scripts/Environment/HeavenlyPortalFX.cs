using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class HeavenlyPortalFX : MonoBehaviour
{
    private Light2D holyLight;
    private Light2D innerCoreLight;
    private Transform rotatingRays;
    private Transform outerHalo;
    private ParticleSystem sparkles;

    private static Material _cachedParticleMaterial;

    public static Material GetOrCreateParticleMaterial()
    {
        if (_cachedParticleMaterial != null) return _cachedParticleMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

        _cachedParticleMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));

        // Tạo texture hạt phát sáng tròn mềm (Soft Glowing Circle) để triệt tiêu vỡ pixel / hình vuông
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        _cachedParticleMaterial.mainTexture = tex;
        return _cachedParticleMaterial;
    }

    public static HeavenlyPortalFX Attach(GameObject targetObj)
    {
        var existing = targetObj.GetComponent<HeavenlyPortalFX>();
        if (existing != null) return existing;
        return targetObj.AddComponent<HeavenlyPortalFX>();
    }

    void Awake()
    {
        SetupRadiantVisuals();
    }

    private void SetupRadiantVisuals()
    {
        // 1. Ánh sáng thiêng liêng White-Gold (Holy Light2D)
        GameObject lightObj = new GameObject("Heavenly_HolyLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        holyLight = lightObj.AddComponent<Light2D>();
        holyLight.lightType = Light2D.LightType.Point;
        holyLight.color = new Color(1f, 0.92f, 0.55f, 1f); // Warm Divine Gold
        holyLight.intensity = 3.8f;
        holyLight.pointLightInnerRadius = 1.0f;
        holyLight.pointLightOuterRadius = 6.5f;

        // Inner Core Pure White Light
        GameObject coreLightObj = new GameObject("Heavenly_CoreLight");
        coreLightObj.transform.SetParent(transform, false);
        coreLightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        innerCoreLight = coreLightObj.AddComponent<Light2D>();
        innerCoreLight.lightType = Light2D.LightType.Point;
        innerCoreLight.color = new Color(1f, 1f, 1f, 1f); // Pure Holy White
        innerCoreLight.intensity = 5.0f;
        innerCoreLight.pointLightInnerRadius = 0.4f;
        innerCoreLight.pointLightOuterRadius = 3.2f;

        // 2. Vòng hào quang quay tròn (Rotating Golden Halo)
        GameObject haloObj = new GameObject("Heavenly_Halo");
        haloObj.transform.SetParent(transform, false);
        haloObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        outerHalo = haloObj.transform;

        var haloSr = haloObj.AddComponent<SpriteRenderer>();
        haloSr.sprite = CreateHolyRingsSprite();
        haloSr.color = new Color(1f, 0.95f, 0.7f, 0.9f);
        haloSr.sortingLayerName = "Default";
        haloSr.sortingOrder = 5050;

        // 3. Tia hào quang xoay (Radiant Rays)
        GameObject raysObj = new GameObject("Heavenly_Rays");
        raysObj.transform.SetParent(transform, false);
        raysObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        rotatingRays = raysObj.transform;

        var raysSr = raysObj.AddComponent<SpriteRenderer>();
        raysSr.sprite = CreateHolySunburstSprite();
        raysSr.color = new Color(1f, 0.85f, 0.4f, 0.65f);
        raysSr.sortingLayerName = "Default";
        raysSr.sortingOrder = 5040;

        // 4. Hạt bụi vàng bay lên (Golden Dust Particle System)
        GameObject psObj = new GameObject("Heavenly_Sparkles");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        sparkles = psObj.AddComponent<ParticleSystem>();
        var main = sparkles.main;
        main.loop = true;
        main.startLifetime = 2.2f;
        main.startSpeed = 0.8f;
        main.startSize = 0.16f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.5f, 0.9f), new Color(1f, 1f, 1f, 0.95f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = sparkles.emission;
        emission.rateOverTime = 25f;

        var shape = sparkles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.2f;

        var colorOverLifetime = sparkles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.88f, 0.35f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var psRenderer = psObj.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = GetOrCreateParticleMaterial();
        psRenderer.sortingLayerName = "Default";
        psRenderer.sortingOrder = 5100;
    }

    void Update()
    {
        float pulse = Mathf.Sin(Time.time * 2.8f) * 0.08f;
        if (outerHalo != null)
        {
            outerHalo.Rotate(0f, 0f, 25f * Time.deltaTime);
            outerHalo.localScale = Vector3.one * (1.1f + pulse);
        }

        if (rotatingRays != null)
        {
            rotatingRays.Rotate(0f, 0f, -15f * Time.deltaTime);
            rotatingRays.localScale = Vector3.one * (1.25f - pulse * 0.5f);
        }

        if (holyLight != null)
        {
            holyLight.intensity = 3.6f + Mathf.Sin(Time.time * 4f) * 0.5f;
        }
    }

    private Sprite CreateHolyRingsSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;
        float maxR = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float normD = d / maxR;

                float ring1 = Mathf.Exp(-Mathf.Pow((normD - 0.55f) * 14f, 2));
                float ring2 = Mathf.Exp(-Mathf.Pow((normD - 0.85f) * 16f, 2));
                float coreGlow = Mathf.Clamp01(1f - normD * 1.5f) * 0.7f;

                float alpha = Mathf.Clamp01(ring1 + ring2 + coreGlow);
                tex.SetPixel(x, y, new Color(1f, 0.95f, 0.8f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private Sprite CreateHolySunburstSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;
        float maxR = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                float rays = Mathf.Pow(Mathf.Cos(angle * 4f), 4);
                float falloff = Mathf.Clamp01(1f - d / maxR);
                float alpha = Mathf.Clamp01(rays * falloff * 0.85f);

                tex.SetPixel(x, y, new Color(1f, 0.9f, 0.5f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }
}