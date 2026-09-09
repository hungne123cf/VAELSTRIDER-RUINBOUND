using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class Map3Builder
{
    private const string EXTRACTED_FOLDER = "Assets/ART/Map3_Extracted";
    private const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";
    private const string MAP3_SCENE_PATH = "Assets/Scenes/Map3_AncientRuins.unity";
    private const string TRIGGER_FILE = "Temp/build_map3.trigger";

    static Map3Builder()
    {
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        if (File.Exists(TRIGGER_FILE))
        {
            try { File.Delete(TRIGGER_FILE); } catch {}
            BloodMonsterAnimationSetup.CheckAndSetup();
            BuildMap3Scene();
        }
    }

    [MenuItem("Vaelstrider/Map 3 (Ancient Ruins)/BUILD MAP 3 (ANCIENT RUINS & BLOOD MONSTER)", false, 0)]
    public static void BuildMap3Scene()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 1. Ensure Map 3 Scene exists (copy from Map 2 to preserve Canvas, Player, Camera, Mobile UI)
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

        // 2. Register all 5 scenes in EditorBuildSettings
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/LoginScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainMenuScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Map2_ValleyRuin.unity", true),
            new EditorBuildSettingsScene(MAP3_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = buildScenes.ToArray();

        // 3. Ensure all extracted textures are imported as 2D Sprites
        EnsureSpriteImports();

        // 4. Thorough clean up of ALL clutter and leftover objects
        string[] cleanupNames = new string[] {
            "Map2_Environment", "Forest_Border_Tree", "Glowing_Rune_Pillar_South",
            "Glowing_Rune_Pillar_West", "Glowing_Rune_Pillar_East", "Sunbeam_Ray",
            "Boulder_MidLeft", "Boulder_BottomRight", "Boulder_FarRight", "Boulder_BottomLeft_Mossy",
            "Tree_MidLeft", "Tree_BottomLeft", "Tree_MidRight", "Tree_BottomRight",
            "Ancient_Upgrade_Shrine", "Map3_Environment", "Grid", "Map2_Visual_Background",
            "Merchant_NPC", "Golden_Chalice_Spirit", "Luck_Creature_Companion",
            "Boss_GorMok_BloodMonster", "BloodMonster_Projectile", "RuinPortal",
            "Sacred_Ancient_Altar", "Sacred_Fountain", "Ancient_Temple_Gate",
            "Light_Altar_Crimson", "Light_Fountain_Azure", "Light_Chalice_Gold",
            "Light_Pillar_NW", "Light_Pillar_NE"
        };
        foreach (var n in cleanupNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }

        // Clean up any other lingering environment objects
        var rootGameObjects = scene.GetRootGameObjects();
        foreach (var rGo in rootGameObjects)
        {
            string lowerName = rGo.name.ToLower();
            if (lowerName.StartsWith("tree") || lowerName.StartsWith("boulder") || lowerName.StartsWith("rock") ||
                lowerName.StartsWith("merchant") || lowerName.StartsWith("chalice") || lowerName.StartsWith("luck") ||
                lowerName.StartsWith("light_") || lowerName.StartsWith("pillar") || lowerName.StartsWith("sacred"))
            {
                Object.DestroyImmediate(rGo);
            }
        }

        // 5. Generate Tiles for Tilemap (Seamless, solid, without transparent gaps)
        Tile cleanFloorTile = CreateOrGetTile("Tile_AncientStoneClean", $"{EXTRACTED_FOLDER}/tile_ancient_stone_clean.png");
        Tile cleanWallTile = CreateOrGetTile("Tile_AncientWallClean", $"{EXTRACTED_FOLDER}/tile_ancient_wall_clean.png", true);

        // 6. Create Grid & Tilemaps
        GameObject gridObj = new GameObject("Grid");
        Grid grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // 6A. Ground Tilemap (Walkable floor)
        GameObject groundObj = new GameObject("Ground");
        groundObj.transform.SetParent(gridObj.transform, false);
        Tilemap groundTm = groundObj.AddComponent<Tilemap>();
        TilemapRenderer groundTr = groundObj.AddComponent<TilemapRenderer>();
        groundTr.sortingLayerName = "Default";
        groundTr.sortingOrder = 0;

        // 6B. Walls Tilemap (Solid boundaries with TilemapCollider2D)
        GameObject wallsObj = new GameObject("Walls");
        wallsObj.transform.SetParent(gridObj.transform, false);
        Tilemap wallsTm = wallsObj.AddComponent<Tilemap>();
        TilemapRenderer wallsTr = wallsObj.AddComponent<TilemapRenderer>();
        wallsTr.sortingLayerName = "Default";
        wallsTr.sortingOrder = 2;

        var wallsCol = wallsObj.AddComponent<TilemapCollider2D>();
        var wallsRb = wallsObj.AddComponent<Rigidbody2D>();
        wallsRb.bodyType = RigidbodyType2D.Static;

        // 7. Paint Arena Floor (Seamless, solid stone floor covering entire arena)
        int minX = -18, maxX = 18;
        int minY = -12, maxY = 12;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                groundTm.SetTile(new Vector3Int(x, y, 0), cleanFloorTile);
            }
        }

        // Perimeter Solid Wall Tiles
        for (int x = minX; x <= maxX; x++)
        {
            wallsTm.SetTile(new Vector3Int(x, maxY, 0), cleanWallTile);
            wallsTm.SetTile(new Vector3Int(x, minY, 0), cleanWallTile);
        }
        for (int y = minY; y <= maxY; y++)
        {
            wallsTm.SetTile(new Vector3Int(minX, y, 0), cleanWallTile);
            wallsTm.SetTile(new Vector3Int(maxX, y, 0), cleanWallTile);
        }

        groundTm.RefreshAllTiles();
        wallsTm.RefreshAllTiles();

        // 8. Construct Clean, Majestic Map 3 Environment
        GameObject envRoot = new GameObject("Map3_Environment");

        // 8A. The Grand Sacred Altar (Center North)
        SpawnProp(envRoot.transform, "altar_224x288.png", "Sacred_Ancient_Altar", new Vector3(0f, 6.5f, 0f), 3, new Vector2(5.5f, 4.0f), false);

        // 8B. Symmetrical Ancient Monoliths Flanking the Arena
        SpawnProp(envRoot.transform, "prop_monolith_64x96.png", "Monolith_West", new Vector3(-10.0f, 2.5f, 0f), 4, new Vector2(1.2f, 2.0f), true);
        SpawnProp(envRoot.transform, "prop_monolith_64x96.png", "Monolith_East", new Vector3(10.0f, 2.5f, 0f), 4, new Vector2(1.2f, 2.0f), true);

        // 8C. Symmetrical Ancient Carved Statues (South Flanks)
        SpawnProp(envRoot.transform, "prop_statue_pillar_64x96.png", "Statue_SouthWest", new Vector3(-10.0f, -4.5f, 0f), 4, new Vector2(1.2f, 2.0f), true);
        SpawnProp(envRoot.transform, "prop_statue_pillar_64x96.png", "Statue_SouthEast", new Vector3(10.0f, -4.5f, 0f), 4, new Vector2(1.2f, 2.0f), true);

        // 9. Atmospheric 2D Lighting (Ancient Twilight & Crimson Arcane Glow)
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == Light2D.LightType.Global)
            {
                l.color = new Color(0.72f, 0.68f, 0.82f, 1f); // Moonlight twilight
                l.intensity = 0.95f;
            }
        }

        // Altar Crimson Arcane Glow
        CreatePointLight(envRoot.transform, "Light_Altar_Crimson", new Vector3(0f, 6.5f, 0f), new Color(1.0f, 0.25f, 0.35f, 0.9f), 2.2f, 2.0f, 9.0f);

        // Torchlights on South Statues
        CreatePointLight(envRoot.transform, "Light_Statue_SW", new Vector3(-10.0f, -3.8f, 0f), new Color(1.0f, 0.60f, 0.25f, 0.8f), 1.3f, 1.0f, 5.5f);
        CreatePointLight(envRoot.transform, "Light_Statue_SE", new Vector3(10.0f, -3.8f, 0f), new Color(1.0f, 0.60f, 0.25f, 0.8f), 1.3f, 1.0f, 5.5f);

        // 10. Setup Player Spawn Position & Camera
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = new Vector3(0f, -6.0f, 0f);
        }

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null && player != null)
        {
            cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        }

        // 11. Setup Blood Monster Boss in Map 3 with Enemy Layer & Tag
        SetupMap3EnemiesAndBoss(envRoot.transform);

        // 12. Setup Ruin Portal at Altar
        var portal = Object.FindAnyObjectByType<RuinPortal>();
        if (portal != null)
        {
            portal.transform.position = new Vector3(0f, 2.5f, 0f);
        }
        else
        {
            RuinPortal.SpawnPortal(new Vector3(0f, 2.5f, 0f));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("<color=green>[MAP 3 BUILDER]</color> Đã dọn dẹp và xây dựng lại Map 3 sạch sẽ, liền mạch với Huyết Thú Gor'Mok!");
    }

    private static void SetupMap3EnemiesAndBoss(Transform parent)
    {
        // Clean up old BotWheel enemies in scene
        var oldEnemies = Object.FindObjectsByType<BotWheelAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var e in oldEnemies)
        {
            if (e != null && e.gameObject != null) Object.DestroyImmediate(e.gameObject);
        }

        // Clean up old BloodMonsters if any
        var oldBloodMonsters = Object.FindObjectsByType<BloodMonsterAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bm in oldBloodMonsters)
        {
            if (bm != null && bm.gameObject != null) Object.DestroyImmediate(bm.gameObject);
        }

        // Load Blood Monster Spritesheet
        string spritesheetPath = "Assets/ART/Blood Monster_A with shadows/Blood Monster_A.png";
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(spritesheetPath);
        Sprite firstSprite = null;
        foreach (var asset in allAssets)
        {
            if (asset is Sprite s)
            {
                firstSprite = s;
                break;
            }
        }

        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/BloodMonster/BloodMonster.controller");

        // 1. Create Boss: ✦ GOR'MOK • HUYẾT THÚ CỔ TỰ ✦
        GameObject bossObj = new GameObject("Boss_GorMok_BloodMonster");
        bossObj.transform.SetParent(parent);
        bossObj.transform.position = new Vector3(0f, 2.2f, 0f);
        bossObj.transform.localScale = Vector3.one * 1.85f;

        // Đảm bảo Boss được gắn đúng Layer "Enemy" (Layer 6) và Tag "Enemy" để người chơi đánh trúng 100%!
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) bossObj.layer = enemyLayer;
        bossObj.tag = "Enemy";

        var sr = bossObj.AddComponent<SpriteRenderer>();
        sr.sprite = firstSprite;
        sr.sortingOrder = 3000;

        var anim = bossObj.AddComponent<Animator>();
        anim.runtimeAnimatorController = animController;

        var cap = bossObj.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.5f, 1.4f);
        cap.direction = CapsuleDirection2D.Vertical;
        cap.isTrigger = false;

        var rb = bossObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var bossAI = bossObj.AddComponent<BloodMonsterAI>();
        bossAI.SetupBoss(1.85f, 2);

        // 2. Map 3 is a direct Boss Arena like Map 2 - disable WaveSpawner so it doesn't interfere
        var spawner = Object.FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.gameObject.SetActive(false);
        }
    }

    private static void EnsureSpriteImports()
    {
        string[] files = Directory.GetFiles(EXTRACTED_FOLDER, "*.png");
        foreach (var file in files)
        {
            string unityPath = file.Replace("\\", "/");
            var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spritePixelsPerUnit != 32) { importer.spritePixelsPerUnit = 32; dirty = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                if (dirty) importer.SaveAndReimport();
            }
        }
    }

    private static Tile CreateOrGetTile(string tileName, string spritePath, bool solidCollider = false)
    {
        string tileDir = "Assets/ART/Map3_GeneratedTiles";
        if (!Directory.Exists(tileDir)) Directory.CreateDirectory(tileDir);

        string tileAssetPath = $"{tileDir}/{tileName}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            tile.sprite = sp;
            tile.colliderType = solidCollider ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, tileAssetPath);
        }
        else
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (tile.sprite == null) tile.sprite = sp;
            tile.colliderType = solidCollider ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
        }
        return tile;
    }

    private static void SpawnProp(Transform parent, string fileName, string objName, Vector3 pos, int sortingOrder, Vector2 colliderSize, bool isVerticalCapsule)
    {
        string spritePath = $"{EXTRACTED_FOLDER}/{fileName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;

        if (sprite != null)
        {
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        if (colliderSize != Vector2.zero)
        {
            if (isVerticalCapsule)
            {
                var cap = obj.AddComponent<CapsuleCollider2D>();
                cap.size = colliderSize;
                cap.direction = CapsuleDirection2D.Vertical;
            }
            else
            {
                var box = obj.AddComponent<BoxCollider2D>();
                box.size = colliderSize;
            }
        }
    }

    private static void CreatePointLight(Transform parent, string lightName, Vector3 pos, Color color, float intensity, float innerR, float outerR)
    {
        GameObject lightObj = new GameObject(lightName);
        lightObj.transform.SetParent(parent);
        lightObj.transform.position = pos;

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = innerR;
        light.pointLightOuterRadius = outerR;
    }
}
