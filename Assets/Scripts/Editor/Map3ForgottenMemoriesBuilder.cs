#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Map3ForgottenMemoriesBuilder
{
    private const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";
    private const string MAP3_SCENE_PATH = "Assets/Scenes/Map3_ForgottenMemories.unity";
    private const string EXTRACTED_PATH = "Assets/ART/ForgottenMemories/Extracted";
    private const string TRIGGER_FILE = "Temp/build_map3.trigger";

    static Map3ForgottenMemoriesBuilder()
    {
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        if (File.Exists(TRIGGER_FILE))
        {
            try { File.Delete(TRIGGER_FILE); } catch {}
            BuildMap3Scene();
        }
    }

    [MenuItem("Vaelstrider/Map 3/2. Build Map 3 Scene (Forgotten Memories)", false, 2)]
    public static void BuildMap3Scene()
    {
        Debug.Log("<color=cyan>[MAP 3 BUILDER]</color> Đang khởi tạo và xây dựng Map 3: Forgotten Memories...");

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 1. Tạo hoặc load Scene Map 3
        if (!File.Exists(MAP3_SCENE_PATH))
        {
            if (File.Exists(MAP2_SCENE_PATH))
            {
                AssetDatabase.CopyAsset(MAP2_SCENE_PATH, MAP3_SCENE_PATH);
            }
            else
            {
                AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", MAP3_SCENE_PATH);
            }
            AssetDatabase.Refresh();
        }

        Scene scene = EditorSceneManager.OpenScene(MAP3_SCENE_PATH, OpenSceneMode.Single);

                // 1.5 Setup toan bo Animations & Controllers cho Quai & Boss
        Map3AnimationSetup.SetupAllAnimations();

        // 2. Cấu hình Importer cho các Sprite đã trích xuất
        ConfigureExtractedSprites();
        AssetDatabase.Refresh();

        // 3. Đăng ký Scene vào Build Settings
        RegisterBuildScenes();

        // 4. Dọn sạch toàn bộ đối tượng rác và quái/boss cũ của Map 2
        CleanAllOldObjects(scene);

        // 5. Xây dựng Sàn đấu trường rêu phong cực đẹp
        BuildArenaEnvironment();

        // 6. Cấu hình Ánh sáng URP 2D huyền ảo
        SetupAtmosphericLighting();

        // 7. Cấu hình Player & Map3WaveSpawner
        SetupCombatSystems();

        // 8. Lưu Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[MAP 3 BUILDER]</color> Hoàn tất xây dựng Đấu Trường Map 3 (Forgotten Memories) ĐẸP LỘNG LẪY & CHUẨN XÁC!");
    }

    private static void ConfigureExtractedSprites()
    {
        string[] files = Directory.GetFiles(EXTRACTED_PATH, "*.png", SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            string unityPath = file.Replace("\\", "/");
            var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (unityPath.Contains("floor"))
            {
                importer.wrapMode = TextureWrapMode.Repeat;
            }
            else
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                tis.spriteAlignment = (int)SpriteAlignment.Custom;
                tis.spritePivot = new Vector2(0.5f, 0.0f); // Pivot chân
                importer.SetTextureSettings(tis);
            }

            importer.SaveAndReimport();
        }
    }

    private static void RegisterBuildScenes()
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/LoginScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainMenuScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Map1_Thestart.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Map2_ValleyRuin.unity", true),
            new EditorBuildSettingsScene(MAP3_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    private static void CleanAllOldObjects(Scene scene)
    {
        // Xoa triet de moi RuinPortal hoac HeavenlyPortal cu de khong co bat ky cong nao xuat hien tu dau!
        var portals = Object.FindObjectsByType<RuinPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in portals) { if (p != null) Object.DestroyImmediate(p.gameObject); }
        var heavenlyFXs = Object.FindObjectsByType<HeavenlyPortalFX>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var h in heavenlyFXs) { if (h != null) Object.DestroyImmediate(h.gameObject); }
        var dummies = Object.FindObjectsByType<DummyEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var d in dummies) { if (d != null) Object.DestroyImmediate(d.gameObject); }
        var cocoons = Object.FindObjectsByType<BoneEggCocoon>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var coc in cocoons) { if (coc != null) Object.DestroyImmediate(coc.gameObject); }
        var obelisks = Object.FindObjectsByType<BoneEggObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var o in obelisks) { if (o != null) Object.DestroyImmediate(o.gameObject); }

        string[] destroyKeywords = new string[] {
            "Map2", "Valley", "Tree", "Pillar", "Altar", "Fountain", "Grid", "BloodMonster",
            "GorMok", "BotWheel", "RuinPortal", "WaveSpawner", "Brazier", "Prop", "Environment",
            "AncientRuins", "StonePillar", "Map3_Environment", "ArenaFloor", "ArenaBoundaries",
            "Map3ArenaManager", "Decorations", "Light_FlamePoint"
        };

        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            string n = go.name;
            bool shouldDestroy = false;
            foreach (var kw in destroyKeywords)
            {
                if (n.ToLower().Contains(kw.ToLower()))
                {
                    shouldDestroy = true;
                    break;
                }
            }

            // Tuyệt đối giữ lại Player, Camera, Canvas, Mobile UI, EventSystem, Audio, Bootstrap
            if (n.Contains("Player") || n.Contains("Camera") || n.Contains("Canvas") ||
                n.Contains("EventSystem") || n.Contains("Audio") || n.Contains("Sound") ||
                n.Contains("Bootstrap") || n.Contains("Manager") && !n.Contains("Map3ArenaManager"))
            {
                shouldDestroy = false;
            }

            if (shouldDestroy)
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    private static void BuildArenaEnvironment()
    {
        GameObject envRoot = new GameObject("Map3_ForgottenMemories_Environment");

        // 1. Sàn đấu trường rêu phong liền mạch (32m x 22m)
        Sprite mossFloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_floor_moss.png");
        GameObject outerPad = new GameObject("ArenaFloor_OuterPadding");
        outerPad.transform.SetParent(envRoot.transform, false);
        var srPad = outerPad.AddComponent<SpriteRenderer>();
        srPad.sprite = mossFloorSprite;
        srPad.drawMode = SpriteDrawMode.Tiled;
        srPad.size = new Vector2(56f, 40f);
        srPad.sortingOrder = -999;
        srPad.color = new Color(0.65f, 0.72f, 0.62f, 1f);

        // 1. Sàn đấu trường rêu phong liền mạch (32m x 22m)
        GameObject floorObj = new GameObject("ArenaFloor_Mossy");
        floorObj.transform.SetParent(envRoot.transform, false);
        var srFloor = floorObj.AddComponent<SpriteRenderer>();
        srFloor.sprite = mossFloorSprite;
        srFloor.drawMode = SpriteDrawMode.Tiled;
        srFloor.size = new Vector2(32f, 22f);
        srFloor.sortingOrder = -500;
        srFloor.color = Color.white;

        // 2. Viền tường bảo vệ xung quanh đấu trường (Tạo ranh giới vững chắc)
        BuildArenaWalls(envRoot.transform, 32f, 22f);

        // 3. Trang trí tự nhiên: Cây đại thụ, cây phong, tảng đá rêu, bụi cây
        BuildNatureProps(envRoot.transform);

        // 4. Cấu hình Camera MapBounds
        GameObject mbObj = new GameObject("MapBounds_Config");
        mbObj.transform.SetParent(envRoot.transform, false);
        var mb = mbObj.AddComponent<MapBounds>();
        mb.SetBounds(new Vector2(-16f, -11f), new Vector2(16f, 11f));
        Camera cam = Camera.main;
        if (cam != null) {
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.11f, 1f);
            var cf = cam.GetComponent<CameraFollow>();
            if (cf != null) {
                cf.SetMapBounds(new Vector2(-16f, -11f), new Vector2(16f, 11f));
                cf.EnableBoundsClamp = true;
            }
        }
    }

    private static void BuildArenaWalls(Transform parent, float width, float height)
    {
        GameObject wallGroup = new GameObject("ArenaWalls");
        wallGroup.transform.SetParent(parent, false);

        float halfW = width / 2f;
        float halfH = height / 2f;
        float thickness = 2f;

        CreateWallCollider(wallGroup.transform, "Wall_Top", new Vector2(0f, halfH + thickness / 2f), new Vector2(width + 4f, thickness));
        CreateWallCollider(wallGroup.transform, "Wall_Bottom", new Vector2(0f, -halfH - thickness / 2f), new Vector2(width + 4f, thickness));
        CreateWallCollider(wallGroup.transform, "Wall_Left", new Vector2(-halfW - thickness / 2f, 0f), new Vector2(thickness, height + 4f));
        CreateWallCollider(wallGroup.transform, "Wall_Right", new Vector2(halfW + thickness / 2f, 0f), new Vector2(thickness, height + 4f));
    }

    private static void CreateWallCollider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = pos;
        var col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        wall.layer = LayerMask.NameToLayer("Default");
    }

    private static void BuildNatureProps(Transform parent)
    {
        GameObject decoGroup = new GameObject("NatureDecorations");
        decoGroup.transform.SetParent(parent, false);

        Sprite willowSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_tree_willow.png");
        Sprite redTreeSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_tree_red.png");
        Sprite tealTreeSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_tree_teal.png");
        Sprite pineTreeSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_tree_pine.png");
        Sprite rockLargeSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_rock_large.png");
        Sprite rockSmallSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_rock_small.png");
        Sprite stumpSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_stump.png");
        Sprite bushGreenSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_bush_green.png");
        Sprite bushGoldSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_PATH}/fm_bush_gold.png");

        // 1. Cây Đại Thụ Liễu Cổ đặt ở 2 góc trên uy nghi
        if (willowSp != null)
        {
            SpawnProp(decoGroup.transform, willowSp, "Willow_NW", new Vector3(-11f, 6.5f, 0f), 1.25f, 0.55f, 0.45f);
            SpawnProp(decoGroup.transform, willowSp, "Willow_NE", new Vector3(11f, 6.5f, 0f), 1.25f, 0.55f, 0.45f);
        }

        // 2. Cây Phong Đỏ & Cây Tùng Bích ở 2 cánh đấu trường
        if (redTreeSp != null)
        {
            SpawnProp(decoGroup.transform, redTreeSp, "RedTree_West", new Vector3(-13.5f, 0.5f, 0f), 1.2f, 0.45f, 0.40f);
            SpawnProp(decoGroup.transform, redTreeSp, "RedTree_SE", new Vector3(12.5f, -6.5f, 0f), 1.2f, 0.45f, 0.40f);
        }
        if (tealTreeSp != null)
        {
            SpawnProp(decoGroup.transform, tealTreeSp, "TealTree_East", new Vector3(13.5f, 0.5f, 0f), 1.2f, 0.45f, 0.40f);
            SpawnProp(decoGroup.transform, tealTreeSp, "TealTree_SW", new Vector3(-12.5f, -6.5f, 0f), 1.2f, 0.45f, 0.40f);
        }

        // 3. Cây Thông phía Bắc
        if (pineTreeSp != null)
        {
            SpawnProp(decoGroup.transform, pineTreeSp, "PineTree_NorthL", new Vector3(-4.5f, 8.5f, 0f), 1.1f, 0.35f, 0.35f);
            SpawnProp(decoGroup.transform, pineTreeSp, "PineTree_NorthR", new Vector3(4.5f, 8.5f, 0f), 1.1f, 0.35f, 0.35f);
        }

        // 4. Tảng đá rêu phế tích lớn nhỏ & gốc cây mục
        if (rockLargeSp != null)
        {
            SpawnProp(decoGroup.transform, rockLargeSp, "RockLarge_W", new Vector3(-8.5f, -4f, 0f), 1.0f, 0.45f, 0.40f);
            SpawnProp(decoGroup.transform, rockLargeSp, "RockLarge_E", new Vector3(8.5f, -4f, 0f), 1.0f, 0.45f, 0.40f);
        }
        if (rockSmallSp != null)
        {
            SpawnProp(decoGroup.transform, rockSmallSp, "RockSmall_NW", new Vector3(-6.5f, 4f, 0f), 1.0f, 0.22f, 0.25f);
            SpawnProp(decoGroup.transform, rockSmallSp, "RockSmall_NE", new Vector3(6.5f, 4f, 0f), 1.0f, 0.22f, 0.25f);
        }
        if (stumpSp != null)
        {
            SpawnProp(decoGroup.transform, stumpSp, "Stump_CenterL", new Vector3(-3.5f, -2f, 0f), 1.0f, 0.55f, 0.55f);
            SpawnProp(decoGroup.transform, stumpSp, "Stump_CenterR", new Vector3(3.5f, -2f, 0f), 1.0f, 0.55f, 0.55f);
        }

        // 5. Bụi hoa rêu trang trí (không cản bước đi, Sorting Layer Default)
        if (bushGreenSp != null)
        {
            SpawnBush(decoGroup.transform, bushGreenSp, new Vector3(-7f, -7f, 0f));
            SpawnBush(decoGroup.transform, bushGreenSp, new Vector3(7f, -7f, 0f));
        }
        if (bushGoldSp != null)
        {
            SpawnBush(decoGroup.transform, bushGoldSp, new Vector3(-10f, 3f, 0f));
            SpawnBush(decoGroup.transform, bushGoldSp, new Vector3(10f, 3f, 0f));
        }
    }

    private static void SpawnProp(Transform parent, Sprite sp, string name, Vector3 pos, float scale, float colRadius, float colOffsetY = 0f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = 50;

        go.AddComponent<AutoDepthSort>();
        // Tu dong gan TreeOcclusionFader cho cay tan la cao (willow, pine, teal, etc)
        string _n = name.ToLower(); string _spName = (sp != null) ? sp.name.ToLower() : "";
        bool _isTreeFade = (_n.Contains("tree") || _n.Contains("willow") || _n.Contains("pine") || _n.Contains("teal") || _n.Contains("redtree")) && !_n.Contains("stump") && !_n.Contains("rock") && !_n.Contains("bush");
        bool _isSpriteFade = _spName.Contains("fm_tree_") && !_spName.Contains("stump");
        if (_isTreeFade || _isSpriteFade) { go.AddComponent<TreeOcclusionFader>(); }


        if (colRadius > 0f)
        {
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = colRadius;
            col.offset = new Vector2(0f, colOffsetY);
        }
    }

    private static void SpawnBush(Transform parent, Sprite sp, Vector3 pos)
    {
        GameObject go = new GameObject("Deco_Bush");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = 10;
        go.AddComponent<AutoDepthSort>();
    }

    private static void SetupAtmosphericLighting()
    {
        // Global Light đêm cổ tích dịu mát
        Light2D globalLight = Object.FindAnyObjectByType<Light2D>();
        if (globalLight != null && globalLight.lightType == Light2D.LightType.Global)
        {
            globalLight.color = new Color(0.72f, 0.76f, 0.90f, 1f);
            globalLight.intensity = 0.88f;
        }

        // Các đốm lửa huyền ảo (Point Lights)
        CreatePointLight("Torch_NW", new Vector3(-8f, 5f, 0f), new Color(0.65f, 0.35f, 1f, 1f), 1.3f, 6f);
        CreatePointLight("Torch_NE", new Vector3(8f, 5f, 0f), new Color(0.65f, 0.35f, 1f, 1f), 1.3f, 6f);
        CreatePointLight("Torch_SW", new Vector3(-8f, -4f, 0f), new Color(1f, 0.65f, 0.3f, 1f), 1.2f, 5.5f);
        CreatePointLight("Torch_SE", new Vector3(8f, -4f, 0f), new Color(1f, 0.65f, 0.3f, 1f), 1.2f, 5.5f);
        CreatePointLight("Torch_Center", new Vector3(0f, 1f, 0f), new Color(0.45f, 0.8f, 1f, 1f), 0.9f, 7f);
    }

    private static void CreatePointLight(string name, Vector3 pos, Color col, float intensity, float radius)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.position = pos;
        var l2d = lightObj.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Point;
        l2d.color = col;
        l2d.intensity = intensity;
        l2d.pointLightOuterRadius = radius;
        l2d.pointLightInnerRadius = radius * 0.25f;
    }

    private static void SetupCombatSystems()
    {
        // 1. Đặt vị trí xuất phát cho Player tại (0, -6)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        if (player != null)
        {
            player.transform.position = new Vector3(0f, -6f, 0f);
            player.tag = "Player";
        }

        // 2. Hệ thống Spawner 3 Waves quái và Boss Skeleton King
        GameObject spawnerObj = new GameObject("WaveSpawner");
        spawnerObj.AddComponent<Map3WaveSpawner>();

    }
}
#endif
