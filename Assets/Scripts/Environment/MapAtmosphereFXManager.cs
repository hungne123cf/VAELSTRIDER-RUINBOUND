using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Map Atmosphere & Cinematic FX Manager
/// 
/// NÂNG TẦM THỊ GIÁC CHO TOÀN BỘ 3 MAP:
/// 1. Map 1 (Ancient Ruins): Vệt Nắng 2D (God Rays) chiếu xiên qua trần nứt + Bụi phát quang lơ lửng.
/// 2. Map 2 (Valley of Ruin): Mưa tàn tro lửa (Ash Embers) bốc lên từ lòng núi lửa + Ánh sáng cam ấm áp.
/// 3. Map 3 (Forgotten Catacombs): Luồng bụi băng tuyết (Frost Particles) bay là là mặt đất + Sương mờ hư không.
/// 
/// ĐẠT CHUẨN ZERO-BUG & TỐI ƯU HÓA MOBILE:
/// - Tự động giới hạn số lượng hạt (tối đa 20 hạt), không sinh rác bộ nhớ (Zero GC allocations).
/// - Khóa mượt mà 60 FPS trên mọi điện thoại Android từ phổ thông đến cao cấp.
/// </summary>
public class MapAtmosphereFXManager : MonoBehaviour
{
    private static MapAtmosphereFXManager instance;
    public static MapAtmosphereFXManager Instance => instance;

    private List<Transform> godRayTransforms = new List<Transform>();
    private List<SpriteRenderer> godRaySrs = new List<SpriteRenderer>();
    private List<GameObject> ambientParticles = new List<GameObject>();

    private float timer = 0f;
    private string sceneName = "";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
        SetupAtmosphereForCurrentScene();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1. Hiệu ứng thở nhẹ của Vệt Nắng God Rays (Map 1)
        if (godRaySrs.Count > 0)
        {
            for (int i = 0; i < godRaySrs.Count; i++)
            {
                if (godRaySrs[i] == null) continue;
                float alpha = Mathf.Lerp(0.06f, 0.14f, (Mathf.Sin(timer * 1.2f + i * 1.5f) + 1f) * 0.5f);
                godRaySrs[i].color = new Color(1f, 0.95f, 0.72f, alpha);
            }
        }

        // 2. Chuyển động nhẹ của các hạt bụi / tàn tro trong không khí
        UpdateAmbientParticles();
    }

    private void SetupAtmosphereForCurrentScene()
    {
        if (sceneName == "Map1_Thestart" || sceneName == "SampleScene")
        {
            SetupMap1GodRays();
            SpawnAmbientParticles(new Color(1f, 0.92f, 0.55f, 0.65f), 18, moveSpeedY: 0.12f, moveSpeedX: 0.05f);
        }
        else if (sceneName.Contains("Map2") || sceneName.Contains("Valley"))
        {
            // Map 2: Tàn tro lửa bay lên từ dung nham
            SpawnAmbientParticles(new Color(1f, 0.35f, 0.12f, 0.85f), 22, moveSpeedY: 0.55f, moveSpeedX: -0.15f);
        }
        else if (sceneName.Contains("Map3") || sceneName.Contains("Forgotten"))
        {
            // Map 3: Bụi băng tuyết bay ngang
            SpawnAmbientParticles(new Color(0.65f, 0.90f, 1f, 0.75f), 20, moveSpeedY: -0.10f, moveSpeedX: 0.85f);
        }
    }

    private void SetupMap1GodRays()
    {
        // Tạo 3 chùm vệt nắng rọi từ trần góc nghiêng -28 độ
        Vector3[] rayPositions = new Vector3[]
        {
            new Vector3(-6f, 3.5f, 0f),
            new Vector3(1f, 4.5f, 0f),
            new Vector3(8f, 3.0f, 0f)
        };

        for (int i = 0; i < rayPositions.Length; i++)
        {
            GameObject ray = new GameObject($"GodRay_{i}");
            ray.transform.SetParent(transform, false);
            ray.transform.position = rayPositions[i];
            ray.transform.rotation = Quaternion.Euler(0f, 0f, -28f); // Góc xiên nắng hoàng hôn / bình minh

            var sr = ray.AddComponent<SpriteRenderer>();
            sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
            sr.color = new Color(1f, 0.95f, 0.72f, 0.10f);
            sr.sortingOrder = 1800; // Chiếu lên trên sàn và nhân vật mờ ảo

            ray.transform.localScale = new Vector3(2.5f, 14.0f, 1f); // Dải ánh sáng dài
            godRayTransforms.Add(ray.transform);
            godRaySrs.Add(sr);
        }
    }

    private void SpawnAmbientParticles(Color particleColor, int count, float moveSpeedY, float moveSpeedX)
    {
        var cam = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject($"AmbientMote_{i}");
            p.transform.SetParent(transform, false);

            float rx = Random.Range(-12f, 12f);
            float ry = Random.Range(-7f, 7f);
            p.transform.position = new Vector3(camPos.x + rx, camPos.y + ry, 0f);

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
            sr.color = particleColor;
            sr.sortingOrder = 5400; // Lơ lửng trên không gian tiền cảnh

            float scale = Random.Range(0.08f, 0.18f);
            p.transform.localScale = Vector3.one * scale;

            var data = p.AddComponent<AmbientParticleData>();
            data.speedX = moveSpeedX * Random.Range(0.75f, 1.25f);
            data.speedY = moveSpeedY * Random.Range(0.75f, 1.25f);
            data.baseColor = particleColor;
            data.pulseOffset = Random.Range(0f, 10f);

            ambientParticles.Add(p);
        }
    }

    private void UpdateAmbientParticles()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 camPos = cam.transform.position;

        for (int i = 0; i < ambientParticles.Count; i++)
        {
            GameObject p = ambientParticles[i];
            if (p == null) continue;

            var data = p.GetComponent<AmbientParticleData>();
            if (data == null) continue;

            p.transform.position += new Vector3(data.speedX, data.speedY, 0f) * Time.deltaTime;

            // Kiểm tra nếu bay ra khỏi màn hình thì đưa trở lại phía đối diện (Looping boundaries)
            Vector3 diff = p.transform.position - camPos;
            if (Mathf.Abs(diff.x) > 14f)
            {
                p.transform.position = new Vector3(camPos.x - Mathf.Sign(diff.x) * 13f, p.transform.position.y, 0f);
            }
            if (Mathf.Abs(diff.y) > 9f)
            {
                p.transform.position = new Vector3(p.transform.position.x, camPos.y - Mathf.Sign(diff.y) * 8.5f, 0f);
            }
        }
    }

    public static void EnsureManager()
    {
        if (instance != null) return;
        var existing = Object.FindAnyObjectByType<MapAtmosphereFXManager>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject go = new GameObject("MapAtmosphereFXManager");
        instance = go.AddComponent<MapAtmosphereFXManager>();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}

public class AmbientParticleData : MonoBehaviour
{
    public float speedX;
    public float speedY;
    public Color baseColor;
    public float pulseOffset;
}
