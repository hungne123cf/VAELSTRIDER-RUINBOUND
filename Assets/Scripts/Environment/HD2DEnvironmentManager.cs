using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - HD-2D Environment Manager (Blender 3D Environment in 2D Pixel World)
/// Quan ly he thong moi truong 3D Stylized Dark Fantasy ket hop nhan vat Pixel Art.
/// CO CHE DU TRU AN TOAN (SAFETY ROLLBACK & TOGGLE):
/// - Nguoi choi co the bat/tat bat ky luc nao bang phim F9 hoac trong Settings.
/// - Khi tat (Use3D = false): Game quay tro lai 100% Tilemap/Sprite 2D nguyen ban khong thay doi.
/// - Khi bat (Use3D = true): Kich hoat cay coi 3D, cot da tan tich, vac lua, mang bang, nham thach.
/// </summary>
public class HD2DEnvironmentManager : MonoBehaviour
{
    private static HD2DEnvironmentManager instance;
    public static HD2DEnvironmentManager Instance => instance;

    private const string PREF_KEY = "HD2D_3D_Environment";
    private static bool? s_cachedEnabled = null;

    public static bool Is3DEnvironmentEnabled
    {
        get
        {
            if (!s_cachedEnabled.HasValue)
            {
                s_cachedEnabled = PlayerPrefs.GetInt(PREF_KEY, 1) == 1;
            }
            return s_cachedEnabled.Value;
        }
        set
        {
            s_cachedEnabled = value;
            PlayerPrefs.SetInt(PREF_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyModeToCurrentScene();
        }
    }

    private GameObject root3DProps;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    void Start()
    {
        EnsureEnvironmentForActiveScene();
    }

    void Update()
    {
        // Phim tat F9 de nguoi choi co the so sanh truc tiep truoc va sau khi bat 3D HD-2D
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ToggleMode();
        }
    }

    public static void ToggleMode()
    {
        Is3DEnvironmentEnabled = !Is3DEnvironmentEnabled;
        string msg = Is3DEnvironmentEnabled
            ? (LocalizationManager.IsVietnamese ? "ĐÃ BẬT MÔI TRƯỜNG 3D HD-2D [BLENDER]" : "3D HD-2D ENVIRONMENT: ENABLED")
            : (LocalizationManager.IsVietnamese ? "ĐÃ TRẢ VỀ PHIÊN BẢN 2D NGUYÊN BẢN" : "ORIGINAL 2D ENVIRONMENT: RESTORED");

        Debug.Log($"<color=cyan>[HD-2D]</color> {msg}");
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            DamagePopup.CreateText(pc.transform.position + Vector3.up * 1.5f, msg, Is3DEnvironmentEnabled ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.8f, 0.2f));
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEnvironmentForActiveScene();
    }

    public static void ApplyModeToCurrentScene()
    {
        var mgr = EnsureInstance();
        mgr.EnsureEnvironmentForActiveScene();
    }

    public static HD2DEnvironmentManager EnsureInstance()
    {
        if (instance != null) return instance;
        var existing = Object.FindAnyObjectByType<HD2DEnvironmentManager>();
        if (existing != null) { instance = existing; return existing; }

        var go = new GameObject("HD2DEnvironmentManager");
        instance = go.AddComponent<HD2DEnvironmentManager>();
        return instance;
    }

    public void EnsureEnvironmentForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Login") || sceneName.Contains("MainMenu"))
        {
            if (root3DProps != null) root3DProps.SetActive(false);
            return;
        }

        if (root3DProps != null)
        {
            root3DProps.SetActive(Is3DEnvironmentEnabled);
            return;
        }

        // Tao Container chua toan bo object 3D
        root3DProps = new GameObject("HD2D_Environment_Root");
        root3DProps.transform.position = Vector3.zero;

        if (sceneName.Contains("Map3"))
        {
            BuildMap3Environment(root3DProps.transform);
        }
        else if (sceneName.Contains("Map2"))
        {
            BuildMap2Environment(root3DProps.transform);
        }
        else
        {
            BuildMap1Environment(root3DProps.transform);
        }

        root3DProps.SetActive(Is3DEnvironmentEnabled);
    }

    // ─── 1. MAP 1: HẦM NGỤC KHỞI ĐẦU (The Start) ──────────────────────────────
    private void BuildMap1Environment(Transform parent)
    {
        // Cot tru da tan tich 3D bo tri o cac goc phong va hanh lang
        SpawnProp("Environment/HD2D_Map1/ancient_ruin_pillar", new Vector3(-8f, 4.5f, 0), parent, 1.0f);
        SpawnProp("Environment/HD2D_Map1/ancient_ruin_pillar", new Vector3(-8f, -4.5f, 0), parent, 1.0f);
        SpawnProp("Environment/HD2D_Map1/ancient_ruin_pillar", new Vector3(14f, 6.5f, 0), parent, 1.0f);
        SpawnProp("Environment/HD2D_Map1/ancient_ruin_pillar", new Vector3(22f, 6.5f, 0), parent, 1.0f);
        SpawnProp("Environment/HD2D_Map1/ancient_ruin_pillar", new Vector3(14f, -5.5f, 0), parent, 1.0f);
        SpawnProp("Environment/HD2D_Map1/ancient_ruin_pillar", new Vector3(22f, -5.5f, 0), parent, 1.0f);

        // Vac lua dong 3D ruc chay kem anh sang am
        SpawnBrazier("Environment/HD2D_Map1/dungeon_brazier", new Vector3(-1f, 2.5f, 0), parent);
        SpawnBrazier("Environment/HD2D_Map1/dungeon_brazier", new Vector3(6f, -2.5f, 0), parent);
        SpawnBrazier("Environment/HD2D_Map1/dungeon_brazier", new Vector3(18f, 3.5f, 0), parent);
    }

    // ─── 2. MAP 2: THUNG LŨNG TÀN TÍCH (Valley Ruin) ──────────────────────────
    private void BuildMap2Environment(Transform parent)
    {
        // Cay co thu 3D van xoan co tan la che bong
        SpawnProp("Environment/HD2D_Map2/ancient_valley_tree", new Vector3(-6f, 3.5f, 0), parent, 1.15f);
        SpawnProp("Environment/HD2D_Map2/ancient_valley_tree", new Vector3(7f, 4.0f, 0), parent, 1.15f);
        SpawnProp("Environment/HD2D_Map2/ancient_valley_tree", new Vector3(8f, -4.0f, 0), parent, 1.10f);

        // Tang da reu phong khong lo 3D
        SpawnProp("Environment/HD2D_Map2/mossy_ruin_boulder", new Vector3(-3f, -3.5f, 0), parent, 1.0f);
        SpawnProp("Environment/HD2D_Map2/mossy_ruin_boulder", new Vector3(4f, 2.0f, 0), parent, 0.9f);
        SpawnProp("Environment/HD2D_Map2/mossy_ruin_boulder", new Vector3(-5f, 0.5f, 0), parent, 0.85f);
    }

    // ─── 3. MAP 3: VỰC BĂNG KÝ ỨC (Forgotten Memories & Calamities) ───────────
    private void BuildMap3Environment(Transform parent)
    {
        bool isVoid = CorruptedCycleManager.IsCorruptedActive;

        if (isVoid)
        {
            // Tru da Hu Khong Ac Mong tim ma mi
            SpawnProp("Environment/HD2D_Map3/void_nightmare_monolith", new Vector3(-6f, 3f, 0), parent, 1.0f, addVoidLight: true);
            SpawnProp("Environment/HD2D_Map3/void_nightmare_monolith", new Vector3(6f, 3f, 0), parent, 1.0f, addVoidLight: true);
            SpawnProp("Environment/HD2D_Map3/void_nightmare_monolith", new Vector3(0f, -4f, 0), parent, 1.0f, addVoidLight: true);
        }
        else
        {
            // Mang da bang tuyet 3D phat quang
            SpawnProp("Environment/HD2D_Map3/frost_icicle_monolith", new Vector3(-7f, 3.5f, 0), parent, 1.0f, addFrostLight: true);
            SpawnProp("Environment/HD2D_Map3/frost_icicle_monolith", new Vector3(7f, 3.5f, 0), parent, 1.0f, addFrostLight: true);
            SpawnProp("Environment/HD2D_Map3/frost_icicle_monolith", new Vector3(15f, -3.5f, 0), parent, 1.0f, addFrostLight: true);

            // Mieng phun dung nham ruc lua
            SpawnProp("Environment/HD2D_Map3/magma_vent_fissure", new Vector3(0f, -3.0f, 0), parent, 1.0f, addLavaLight: true);
            SpawnProp("Environment/HD2D_Map3/magma_vent_fissure", new Vector3(18f, 2.0f, 0), parent, 1.0f, addLavaLight: true);
        }
    }

    private GameObject SpawnProp(string resourcePath, Vector3 pos, Transform parent, float scale = 1.0f, bool addFrostLight = false, bool addLavaLight = false, bool addVoidLight = false)
    {
        Sprite sp = Resources.Load<Sprite>(resourcePath);
        if (sp == null) return null;

        GameObject go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(resourcePath));
        go.transform.position = pos;
        go.transform.SetParent(parent);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        // Depth-sort theo y-coordinate
        sr.sortingOrder = 5000 + Mathf.RoundToInt(-pos.y * 100);

        // Add nhe collider chan duong di hop ly
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.45f);
        col.offset = new Vector2(0, -0.3f);

        // Dynamic 2D Light
        if (addFrostLight)
        {
            var lt = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            lt.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            lt.color = new Color(0.35f, 0.85f, 1f);
            lt.intensity = 1.8f;
            lt.pointLightOuterRadius = 3.5f;
        }
        else if (addLavaLight)
        {
            var lt = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            lt.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            lt.color = new Color(1f, 0.4f, 0.1f);
            lt.intensity = 2.4f;
            lt.pointLightOuterRadius = 4.0f;
        }
        else if (addVoidLight)
        {
            var lt = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            lt.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            lt.color = new Color(0.7f, 0.2f, 1f);
            lt.intensity = 2.2f;
            lt.pointLightOuterRadius = 3.8f;
        }

        return go;
    }

    private GameObject SpawnBrazier(string resourcePath, Vector3 pos, Transform parent)
    {
        var go = SpawnProp(resourcePath, pos, parent, 0.9f);
        if (go != null)
        {
            var lt = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            lt.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            lt.color = new Color(1.0f, 0.65f, 0.2f);
            lt.intensity = 2.5f;
            lt.pointLightOuterRadius = 4.5f;
            go.AddComponent<TorchFlicker>();
        }
        return go;
    }
}

