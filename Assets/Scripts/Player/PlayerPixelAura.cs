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
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
        main.maxParticles = 75;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Màu sắc hạt: Tinh thể Băng Giá Cực Bắc & Hơi sương lạnh giá (Glacial Diamond Frost)
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.95f, 1.0f, 1.0f), 0.0f),   // Trắng tuyết lấp lánh
                new GradientColorKey(new Color(0.40f, 0.88f, 1.0f), 0.35f),  // Lam băng tinh khiết
                new GradientColorKey(new Color(0.15f, 0.65f, 0.98f), 0.70f), // Xanh băng buốt giá
                new GradientColorKey(new Color(0.65f, 0.92f, 1.0f), 1.0f)   // Hơi sương băng mờ ảo
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
        emission.rateOverTime = 20f; // Số hạt sinh ra mỗi giây

        // Cấu hình Shape (Vùng bao quanh ngực và thân của Minotaur)
        var shape = particleSys.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, 0.55f, 0f);
        shape.scale = new Vector3(0.75f, 0.85f, 0.2f);

        // Vận tốc bay lên và xoáy nhẹ như sương lạnh (Velocity over Lifetime)
        var vel = particleSys.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(0.28f, 0.80f); // Hơi lạnh bốc lên từ từ
        vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f); // Gió thổi đung đưa nhẹ
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Hạt xoay nhẹ theo thời gian mô phỏng bông tuyết / mảnh băng trôi
        var rot = particleSys.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);

        // Kích thước thu nhỏ dần khi tan biến (Size over Lifetime)
        var sizeOverLife = particleSys.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.25f, 1.15f);
        sizeCurve.AddKey(0.75f, 0.9f);
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

        // Tạo Texture 8x8 Tinh thể Băng hình thoi sắc nét (Diamond Frost Crystal)
        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // Point Filter giữ nét hạt pixel art sắc cạnh
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int distManhattan = Mathf.Abs(x - 3) + Mathf.Abs(y - 3); // Hình thoi tinh thể băng
                if (distManhattan <= 1)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f); // Lõi tinh thể sáng trắng
                }
                else if (distManhattan <= 2)
                {
                    pixels[y * size + x] = new Color(0.75f, 0.95f, 1f, 0.85f); // Viền lam băng
                }
                else if (distManhattan <= 3)
                {
                    pixels[y * size + x] = new Color(0.40f, 0.85f, 1f, 0.35f); // Hào quang sương lạnh
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        pixelParticleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);

        // Tạo Shader Sprites/Default để hạt phát sáng lạnh rõ nét trong môi trường 2D
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
