using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class DungeonLightingManager : MonoBehaviour
{
    public static DungeonLightingManager Instance { get; private set; }

    [Header("Lighting Palette")]
    [SerializeField] private Color dungeonAmbientColor = new Color(0.20f, 0.24f, 0.35f, 1.0f); // Dark fantasy twilight slate
    [SerializeField] private Color playerLanternColor = new Color(1.0f, 0.85f, 0.58f, 1.0f); // Warm amber candle glow
    [SerializeField] private bool autoApplyLitMaterials = true;

    private Light2D globalLight;
    private Light2D playerLight;
    private static Material sharedLitMaterial;
    private float nextMaterialCheck = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        WaveSpawner.OnWaveStarted += HandleWaveStarted;
    }

    void OnDisable()
    {
        WaveSpawner.OnWaveStarted -= HandleWaveStarted;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        SetupGlobalLighting();
        SetupPlayerLantern();
        if (autoApplyLitMaterials)
        {
            ApplyLitMaterialsToWorld();
        }
    }

    void Update()
    {
        // Tu dong gan lai den cho Player neu Player vua duoc sinh hoac respawn
        if (playerLight == null)
        {
            SetupPlayerLantern();
        }
        else if (playerLight.transform.parent != null)
        {
            var pc = playerLight.transform.parent.GetComponent<PlayerCombat>();
            if (pc != null)
            {
                playerLight.transform.localPosition = pc.GetBodyCenterWorld() - playerLight.transform.parent.position;
            }
        }

        // Dinh ky kiem tra vat lieu Lit cho quai moi sinh
        if (autoApplyLitMaterials && Time.time >= nextMaterialCheck)
        {
            nextMaterialCheck = Time.time + 3.0f;
            ApplyLitMaterialsToWorld();
        }
    }

    private void HandleWaveStarted(int current, int total, string title)
    {
        Debug.Log($"<color=cyan>[LIGHTING]</color> Wave {current} bắt đầu! Làm mới ánh sáng và đèn lồng...");
        SetupGlobalLighting();
        SetupPlayerLantern();
        if (autoApplyLitMaterials)
        {
            ApplyLitMaterialsToWorld();
        }
    }

    public void SetupGlobalLighting()
    {
        if (globalLight == null)
        {
            var lights = FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l != null && l.lightType == Light2D.LightType.Global)
                {
                    globalLight = l;
                    break;
                }
            }
        }

        if (globalLight == null)
        {
            GameObject glObj = new GameObject("DungeonGlobalLight2D");
            globalLight = glObj.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
        }

        globalLight.color = dungeonAmbientColor;
        globalLight.intensity = 0.95f;
    }

    public void SetupPlayerLantern()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        Vector3 localCenter = new Vector3(0f, 0.65f, 0f);
        var pc = player.GetComponent<PlayerCombat>();
        if (pc != null)
        {
            localCenter = pc.GetBodyCenterWorld() - player.transform.position;
        }
        else
        {
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                localCenter = sr.bounds.center - player.transform.position;
            }
        }

        Transform existing = player.transform.Find("PlayerLanternLight2D");
        if (existing != null)
        {
            existing.localPosition = localCenter;
            playerLight = existing.GetComponent<Light2D>();
            if (playerLight != null)
            {
                playerLight.pointLightInnerRadius = 1.2f;
                playerLight.pointLightOuterRadius = 5.5f;
                return;
            }
        }

        GameObject lightObj = new GameObject("PlayerLanternLight2D");
        lightObj.transform.SetParent(player.transform, false);
        lightObj.transform.localPosition = localCenter;

        playerLight = lightObj.AddComponent<Light2D>();
        playerLight.lightType = Light2D.LightType.Point;
        playerLight.color = playerLanternColor;
        playerLight.intensity = 1.4f;
        playerLight.pointLightInnerRadius = 1.2f;
        playerLight.pointLightOuterRadius = 5.5f;

        // Subtle gentle breathing
        lightObj.AddComponent<TorchFlicker>();
        Debug.Log("<color=cyan>[LIGHTING]</color> Đã gắn Đèn Lồng 2D đồng tâm chuẩn xác cho người chơi!");
    }

    public void ApplyLitMaterialsToWorld()
    {
        if (sharedLitMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader != null)
            {
                sharedLitMaterial = new Material(shader);
            }
        }

        if (sharedLitMaterial == null) return;

        // Apply to SpriteRenderers
        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            string name = sr.gameObject.name;
            if (name.Contains("Popup") || name.Contains("Damage") || name.Contains("Prompt") || name.Contains("Ghost")) continue;

            if (sr.sharedMaterial == null || 
                sr.sharedMaterial.shader.name.Contains("Sprites/Default") || 
                sr.sharedMaterial.shader.name.Contains("Sprite-Unlit"))
            {
                sr.sharedMaterial = sharedLitMaterial;
            }
        }

        // Apply to TilemapRenderers
        var tilemaps = FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var tr in tilemaps)
        {
            if (tr == null) continue;
            if (tr.sharedMaterial == null || 
                tr.sharedMaterial.shader.name.Contains("Sprites/Default") || 
                tr.sharedMaterial.shader.name.Contains("Sprite-Unlit"))
            {
                tr.sharedMaterial = sharedLitMaterial;
            }
        }
    }

    public static DungeonLightingManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<DungeonLightingManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject go = new GameObject("DungeonLightingManager");
        Instance = go.AddComponent<DungeonLightingManager>();
        return Instance;
    }
}
