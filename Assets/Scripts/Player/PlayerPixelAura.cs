using UnityEngine;

/// <summary>
/// Hiệu ứng hào quang hạt pixel (Floating Pixel Embers / Shadow Particle Aura) 
/// bay lơ lửng, trôi dạt và xoay nhẹ quanh cơ thể chiến binh hắc ám (Player 4 - Titan Berserker).
/// Sử dụng Texture Pixel Point-Filtered để hạt luôn giữ hình vuông sắc nét chuẩn Retro Pixel Art.
/// </summary>
public class PlayerPixelAura : MonoBehaviour
{
    private ParticleSystem particleSys;
    private ParticleSystemRenderer particleRenderer;
    private static Sprite pixelParticleSprite;
    private static Material pixelParticleMaterial;

    private bool isInitialized = false;

    void Awake()
    {
        InitializeAura();
    }

    public void InitializeAura()
    {
        if (isInitialized) return;

        // Tạo hoặc lấy ParticleSystem
        particleSys = GetComponent<ParticleSystem>();
        if (particleSys == null)
        {
            particleSys = gameObject.AddComponent<ParticleSystem>();
        }

        particleRenderer = GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null)
        {
            particleRenderer = gameObject.AddComponent<ParticleSystemRenderer>();
        }

        // Tạo Particle Material & Pixel Texture (Point Filtered)
        SetupPixelMaterial();

        // Cấu hình Main Module
        var main = particleSys.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Hạt trôi tự nhiên theo chuyển động của Player
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
        main.maxParticles = 60;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Màu sắc hạt: Hắc ám Obsidian kết hợp Đốm lửa đỏ Huyết Nguyệt (Crimson Shadow)
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.2f, 0.15f), 0.0f),   // Đỏ rực
                new GradientColorKey(new Color(0.9f, 0.1f, 0.3f), 0.4f),   // Đỏ sẫm
                new GradientColorKey(new Color(0.12f, 0.14f, 0.18f), 0.8f),// Đen than hắc ám
                new GradientColorKey(new Color(0.05f, 0.05f, 0.08f), 1.0f) // Đen tuyền
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.85f, 0.65f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // Cấu hình Emission
        var emission = particleSys.emission;
        emission.enabled = true;
        emission.rateOverTime = 16f; // Số hạt sinh ra mỗi giây

        // Cấu hình Shape (Vùng bao quanh ngực và thân của Minotaur)
        var shape = particleSys.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, 0.65f, 0f);
        shape.scale = new Vector3(0.7f, 0.9f, 0.2f);

        // Vận tốc bay lên và xoáy nhẹ (Velocity over Lifetime)
        var vel = particleSys.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(0.35f, 0.95f); // Bay bốc lên từ từ
        vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f); // Dao động ngang nhẹ
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Hạt xoay nhẹ theo thời gian
        var rot = particleSys.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        // Kích thước thu nhỏ dần khi tan biến (Size over Lifetime)
        var sizeOverLife = particleSys.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.4f);
        sizeCurve.AddKey(0.2f, 1.1f);
        sizeCurve.AddKey(0.8f, 0.9f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer: Thiết lập Render Mode và Sorting Order
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingOrder = 5002; // Hiển thị phía trên sprite người chơi (5000)
        particleRenderer.material = pixelParticleMaterial;

        isInitialized = true;
        particleSys.Play();
    }

    private void SetupPixelMaterial()
    {
        if (pixelParticleMaterial != null) return;

        // Tạo Texture 8x8 Pixel vuông vức sắc cạnh chuẩn Pixel Art
        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // QUAN TRỌNG: Point Filter giữ nét vuông sắc cạnh
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Hình vuông pixel đặc ruột với viền nhẹ
                if (x >= 1 && x <= 6 && y >= 1 && y <= 6)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 0.4f);
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        pixelParticleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);

        // Tạo Shader Sprites/Default để hạt sáng rõ trong môi trường 2D
        Shader s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        if (s == null) s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        pixelParticleMaterial = new Material(s);
        pixelParticleMaterial.mainTexture = tex;
    }

    /// <summary>
    /// Đảm bảo GameObject Người chơi có PlayerPixelAura khi sử dụng Player 4
    /// </summary>
    public static void EnsureAuraForPlayer(GameObject playerObj, bool isEnabled)
    {
        if (playerObj == null) return;

        Transform auraChild = playerObj.transform.Find("PixelShadowAura");
        if (isEnabled)
        {
            if (auraChild == null)
            {
                GameObject newAuraObj = new GameObject("PixelShadowAura");
                newAuraObj.transform.SetParent(playerObj.transform, false);
                newAuraObj.transform.localPosition = Vector3.zero;
                newAuraObj.AddComponent<PlayerPixelAura>();
            }
            else
            {
                var aura = auraChild.GetComponent<PlayerPixelAura>();
                if (aura == null) aura = auraChild.gameObject.AddComponent<PlayerPixelAura>();
                aura.gameObject.SetActive(true);
            }
        }
        else
        {
            if (auraChild != null)
            {
                auraChild.gameObject.SetActive(false);
            }
        }
    }
}
