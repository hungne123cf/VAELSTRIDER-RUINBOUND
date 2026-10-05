using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class Map2Builder
{
    private const string ASSET_ROOT = "Assets/ART/Map/Valley Ruin Asset Pack V0.3";
    private const string TILES_FOLDER = ASSET_ROOT + "/Tiles Maps";
    private const string OBJECTS_FOLDER = ASSET_ROOT + "/Objects";
    private const string EXTRACTED_FOLDER = OBJECTS_FOLDER + "/Extracted";
    private const string GENERATED_TILES_FOLDER = ASSET_ROOT + "/GeneratedTiles";
    private const string MAP1_SCENE_PATH = "Assets/Scenes/Map1_Thestart.unity";
    private const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";
    private const string TRIGGER_FILE = "Temp/build_map2.trigger";

    static Map2Builder()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch {}
                BuildExactMap2();
            }
        };
    }

    [MenuItem("Vaelstrider/Map 2 (Valley Ruin)/BUILD EXACT MAP 2 FROM IMAGE", false, 0)]
    public static void BuildExactMap2()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 1. Ensure scene exists
        if (!File.Exists(MAP2_SCENE_PATH))
        {
            AssetDatabase.CopyAsset(MAP1_SCENE_PATH, MAP2_SCENE_PATH);
            AssetDatabase.Refresh();
        }

        Scene scene = EditorSceneManager.OpenScene(MAP2_SCENE_PATH, OpenSceneMode.Single);

        // 2. Build Settings
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(MAP1_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP2_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = buildScenes;

        // 3. Remove obsolete objects
        string[] cleanupNames = new string[] {
            "DungeonBarriers", "DungeonTorches", "DungeonRooms",
            "WaveBanner", "TreasureChest_Crypt", "Map2_Giant_Tree",
            "Map2_Environment", "RuinPortal", "Map2_Visual_Background",
            "Grid"
        };
        foreach (var n in cleanupNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }

        // 4. Create Grid with Ground & Walls Tilemaps
        GameObject gridObj = new GameObject("Grid");
        Grid grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // 4A. Ground Tilemap (Walkable floor: Grass & Dirt Arena, NO collider)
        GameObject groundObj = new GameObject("Ground");
        groundObj.transform.SetParent(gridObj.transform, false);
        Tilemap groundTm = groundObj.AddComponent<Tilemap>();
        TilemapRenderer groundTr = groundObj.AddComponent<TilemapRenderer>();
        groundTr.sortingLayerName = "Default";
        groundTr.sortingOrder = 0;

        // 4B. Walls Tilemap (Impassable rocks, cliffs, border walls with solid TilemapCollider2D)
        GameObject wallsObj = new GameObject("Walls");
        wallsObj.transform.SetParent(gridObj.transform, false);
        Tilemap wallsTm = wallsObj.AddComponent<Tilemap>();
        TilemapRenderer wallsTr = wallsObj.AddComponent<TilemapRenderer>();
        wallsTr.sortingLayerName = "Default";
        wallsTr.sortingOrder = 2;

        var wallsCol = wallsObj.AddComponent<TilemapCollider2D>();
        var wallsRb = wallsObj.AddComponent<Rigidbody2D>();
        wallsRb.bodyType = RigidbodyType2D.Static;

        // 5. Load Tile Assets
        Tile grassTile = AssetDatabase.LoadAssetAtPath<Tile>($"{GENERATED_TILES_FOLDER}/Tile_GrassTile_1_1.asset");
        if (grassTile == null) grassTile = AssetDatabase.LoadAssetAtPath<Tile>($"{GENERATED_TILES_FOLDER}/Tile_GrassTile_0_0.asset");

        Tile dirtTile = AssetDatabase.LoadAssetAtPath<Tile>($"{GENERATED_TILES_FOLDER}/Tile_DirtTile.asset");
        Tile sandTile = AssetDatabase.LoadAssetAtPath<Tile>($"{GENERATED_TILES_FOLDER}/Tile_SandTile_1_1.asset");
        Tile clearingTile = dirtTile != null ? dirtTile : sandTile;

        Tile rockWallTile = AssetDatabase.LoadAssetAtPath<Tile>($"{GENERATED_TILES_FOLDER}/Tile_RockWall_1_1.asset");
        if (rockWallTile == null) rockWallTile = AssetDatabase.LoadAssetAtPath<Tile>($"{GENERATED_TILES_FOLDER}/Tile_RockWall_0_0.asset");
        if (rockWallTile != null) rockWallTile.colliderType = Tile.ColliderType.Grid;

        Tile grassWallTile = AssetDatabase.LoadAssetAtPath<Tile>($"{GENERATED_TILES_FOLDER}/Tile_GrassWall_1_1.asset");
        if (grassWallTile != null) grassWallTile.colliderType = Tile.ColliderType.Grid;

        // 6. Paint Ground Terrain (Grass base & Central Dirt Clearing)
        int minX = -20, maxX = 20;
        int minY = -14, maxY = 14;

        // 6A. Base Grass
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                groundTm.SetTile(new Vector3Int(x, y, 0), grassTile);
            }
        }

        // 6B. Central Dirt Arena Clearing
        for (int x = -7; x <= 7; x++)
        {
            for (int y = -8; y <= 3; y++)
            {
                float dx = (float)x / 6.8f;
                float dy = (float)(y + 2.2f) / 5.2f;
                float dist = dx * dx + dy * dy;
                float noise = Mathf.Sin(x * 2.5f) * Mathf.Cos(y * 2.1f) * 0.18f;
                if (dist + noise <= 1.0f)
                {
                    groundTm.SetTile(new Vector3Int(x, y, 0), clearingTile);
                }
            }
        }

        // 6C. Paint Solid Perimeter Walls
        Tile boundaryTile = rockWallTile != null ? rockWallTile : grassWallTile;
        if (boundaryTile != null)
        {
            for (int x = minX; x <= maxX; x++)
            {
                wallsTm.SetTile(new Vector3Int(x, maxY, 0), boundaryTile);
                wallsTm.SetTile(new Vector3Int(x, minY, 0), boundaryTile);
            }
            for (int y = minY; y <= maxY; y++)
            {
                wallsTm.SetTile(new Vector3Int(minX, y, 0), boundaryTile);
                wallsTm.SetTile(new Vector3Int(maxX, y, 0), boundaryTile);
            }
        }

        groundTm.RefreshAllTiles();
        wallsTm.RefreshAllTiles();

        // 7. Create Environment Props
        GameObject envRoot = new GameObject("Map2_Environment");

        // 7A. Giant Ancient Tree with EXACT EdgeCollider2D contour bo sat theo goc re cay!
        Sprite giantTreeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OBJECTS_FOLDER + "/Giant_Tree.png");
        if (giantTreeSprite != null)
        {
            GameObject treeObj = new GameObject("Giant_Ancient_Tree");
            treeObj.transform.SetParent(envRoot.transform);
            treeObj.transform.position = new Vector3(0f, 6.5f, 0f);
            var sr = treeObj.AddComponent<SpriteRenderer>();
            sr.sprite = giantTreeSprite;
            sr.sortingOrder = 5;

            // EdgeCollider2D bo khit theo duong cong re cay ben trai
            var edgeLeft = treeObj.AddComponent<EdgeCollider2D>();
            Vector2[] leftPoints = new Vector2[]
            {
                new Vector2(-18.31f, -10.59f), new Vector2(-17.56f, -10.41f), new Vector2(-16.81f, -9.47f),
                new Vector2(-16.06f, -9.28f), new Vector2(-15.31f, -9.03f), new Vector2(-14.56f, -8.59f),
                new Vector2(-13.81f, -7.47f), new Vector2(-13.06f, -8.03f), new Vector2(-12.31f, -8.22f),
                new Vector2(-11.56f, -8.53f), new Vector2(-10.81f, -9.09f), new Vector2(-10.06f, -9.72f),
                new Vector2(-9.31f, -10.97f), new Vector2(-8.56f, -11.66f), new Vector2(-7.81f, -11.97f),
                new Vector2(-7.06f, -12.28f), new Vector2(-6.31f, -10.03f), new Vector2(-5.56f, -8.34f),
                new Vector2(-4.81f, -8.53f), new Vector2(-4.06f, -0.41f), new Vector2(-3.31f, -0.16f),
                new Vector2(-2.56f, 0.03f)
            };
            edgeLeft.points = leftPoints;

            // EdgeCollider2D bo khit theo duong cong re cay ben phai
            var edgeRight = treeObj.AddComponent<EdgeCollider2D>();
            Vector2[] rightPoints = new Vector2[]
            {
                new Vector2(2.0f, -7.41f), new Vector2(2.75f, -8.16f), new Vector2(3.5f, -8.59f),
                new Vector2(4.25f, -10.28f), new Vector2(5.0f, -11.16f), new Vector2(5.75f, -11.22f),
                new Vector2(6.5f, -11.53f), new Vector2(7.25f, -11.28f), new Vector2(8.0f, -11.53f),
                new Vector2(8.75f, -11.59f), new Vector2(9.5f, -11.72f), new Vector2(10.25f, -11.59f),
                new Vector2(11.0f, -11.47f), new Vector2(11.75f, -11.16f), new Vector2(12.5f, -11.16f),
                new Vector2(13.25f, -11.59f), new Vector2(14.0f, -11.53f), new Vector2(14.75f, -11.34f),
                new Vector2(15.5f, -11.47f), new Vector2(16.25f, -10.59f), new Vector2(17.0f, -11.09f),
                new Vector2(17.75f, -10.97f), new Vector2(18.5f, -10.91f)
            };
            edgeRight.points = rightPoints;

            // EdgeCollider2D da bo tu nhien theo re cay, khong can BoxCollider2D tho cung

            // Majestic Canopy Wind Sway
            var sway = treeObj.AddComponent<TreeWindSway>();
            sway.SetupParameters(0.8f, 0.9f, 0.01f);
        }

        // 7B. Glowing Sanctum Doorway
        GameObject doorObj = new GameObject("SanctumDoor_Glow");
        doorObj.transform.SetParent(envRoot.transform);
        doorObj.transform.position = new Vector3(0f, 6.2f, 0f);
        var doorLight = doorObj.AddComponent<Light2D>();
        doorLight.lightType = Light2D.LightType.Point;
        doorLight.color = new Color(0.75f, 0.95f, 1f, 1f);
        doorLight.intensity = 2.8f;
        doorLight.pointLightInnerRadius = 0.6f;
        doorLight.pointLightOuterRadius = 4.5f;

        // 7C. Interactive Golden Treasure Chest
        var chest = Object.FindAnyObjectByType<TreasureChest>();
        if (chest == null) chest = TreasureChest.SpawnChest(new Vector3(0f, 4.0f, 0f));
        else chest.transform.position = new Vector3(0f, 4.0f, 0f);
        chest.gameObject.name = "Map2_GoldenTreasureChest";

        // 7C2. Ancient Shrine Pillar (Be Da Nang Cap voi Tru Da Phat Sang va nut OPEN)
        var shrine = AncientShrine.SpawnShrine(new Vector3(-5.5f, 2.8f, 0f));
        if (shrine != null) shrine.transform.SetParent(envRoot.transform);

        // 7D. Slanted Mossy Boulder
        SpawnProp(envRoot.transform, "obj_47_44x77.png", "Rock_Slanted_Right", new Vector3(5.5f, 6.8f, 0f), 6, new Vector2(2.4f, 4.2f), true);

        // 7E. Mossy Boulder
        SpawnProp(envRoot.transform, "obj_21_49x71.png", "Rock_Mossy_Left", new Vector3(-5.5f, 6.8f, 0f), 6, new Vector2(2.6f, 3.8f), true);

        // 7F. The 4 Standalone Trees around the clearing WITH WIND SWAY ANIMATION
        SpawnTreeWithWind(envRoot.transform, "obj_07_79x89.png", "Tree_MidLeft", new Vector3(-6.8f, -1.8f, 0f), 4, 0.9f, 1.6f, 2.2f);
        SpawnTreeWithWind(envRoot.transform, "obj_08_72x90.png", "Tree_BottomLeft", new Vector3(-4.5f, -6.5f, 0f), 4, 0.9f, 1.4f, 2.0f);
        SpawnTreeWithWind(envRoot.transform, "obj_10_67x85.png", "Tree_MidRight", new Vector3(9.5f, -1.2f, 0f), 4, 0.9f, 1.7f, 2.4f);
        SpawnTreeWithWind(envRoot.transform, "obj_07_79x89.png", "Tree_BottomRight", new Vector3(13.2f, -4.8f, 0f), 4, 0.9f, 1.5f, 2.1f);

        // 7G. Boulders in the glade
        SpawnProp(envRoot.transform, "obj_32_44x27.png", "Boulder_MidLeft", new Vector3(-10.8f, -1.5f, 0f), 3, new Vector2(1.6f, 0.8f), false);
        SpawnProp(envRoot.transform, "obj_33_44x27.png", "Boulder_BottomRight", new Vector3(7.5f, -9.2f, 0f), 3, new Vector2(1.8f, 0.9f), false);
        SpawnProp(envRoot.transform, "obj_31_32x27.png", "Boulder_FarRight", new Vector3(14.0f, -1.8f, 0f), 3, new Vector2(1.8f, 1.3f), false);
        SpawnProp(envRoot.transform, "obj_35_48x47.png", "Boulder_BottomLeft_Mossy", new Vector3(-6.5f, -7.2f, 0f), 3, new Vector2(1.8f, 1.1f), false);

        // 7H. ANCIENT STONE PILLARS WITH GLOWING CRACKS!
        // Pillar 1: South Center
        SpawnGlowingPillar(envRoot.transform, "obj_52_14x38.png", "obj_52_crack_glow.png", "Glowing_Rune_Pillar_South", new Vector3(-2.2f, -11.5f, 0f), new Color(0.45f, 0.95f, 1f, 1f));
        // Pillar 2: West Flank
        SpawnGlowingPillar(envRoot.transform, "obj_53_14x38.png", "obj_53_crack_glow.png", "Glowing_Rune_Pillar_West", new Vector3(-11.5f, -5.5f, 0f), new Color(0.35f, 0.85f, 1f, 1f));
        // Pillar 3: East Flank
        SpawnGlowingPillar(envRoot.transform, "obj_54_14x38.png", "obj_54_crack_glow.png", "Glowing_Rune_Pillar_East", new Vector3(11.8f, -7.5f, 0f), new Color(0.55f, 1f, 0.95f, 1f));

        // 7I. Dense Forest Border Trees WITH WIND SWAY
        List<Vector3> borderTreePositions = new List<Vector3>()
        {
            new Vector3(-16f, 10f, 0), new Vector3(-13f, 11f, 0), new Vector3(-17f, 5f, 0),
            new Vector3(-14f, 3f, 0), new Vector3(-16f, -3f, 0), new Vector3(-13f, -5f, 0),
            new Vector3(-16f, -10f, 0), new Vector3(-12f, -11f, 0),

            new Vector3(16f, 10f, 0), new Vector3(13f, 11f, 0), new Vector3(17f, 5f, 0),
            new Vector3(14f, 3f, 0), new Vector3(16f, -3f, 0), new Vector3(13f, -5f, 0),
            new Vector3(16f, -10f, 0), new Vector3(12f, -11f, 0)
        };

        foreach (var pos in borderTreePositions)
        {
            SpawnTreeWithWind(envRoot.transform, "obj_07_79x89.png", "Forest_Border_Tree", pos, 4, 0.6f, 1.3f, 1.6f);
        }

        // 8. Ancient Upgrade Shrine reposition if needed
        if (shrine != null) shrine.transform.position = new Vector3(-5.5f, 2.8f, 0f);

        // 9. Outdoor Ambient Daylight & Sunbeams
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == Light2D.LightType.Global)
            {
                l.color = new Color(0.92f, 0.98f, 0.94f, 1f);
                l.intensity = 1.05f;
            }
        }

        GameObject sunbeamObj = new GameObject("Sunbeam_Ray");
        sunbeamObj.transform.SetParent(envRoot.transform);
        sunbeamObj.transform.position = new Vector3(-5.5f, 5.5f, 0f);
        var sunLight = sunbeamObj.AddComponent<Light2D>();
        sunLight.lightType = Light2D.LightType.Point;
        sunLight.color = new Color(1f, 0.98f, 0.82f, 0.85f);
        sunLight.intensity = 1.6f;
        sunLight.pointLightInnerRadius = 1.5f;
        sunLight.pointLightOuterRadius = 8.5f;

        // 10. Player Spawn Point
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = new Vector3(0f, -3.0f, 0f);
        }

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null && player != null)
        {
            cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("<color=green>[MAP 2]</color> Kien tao hoan tat Map 2: Cay gio lay tu nhien & Tru da phat sang tren khe nut!");
    }

    private static void SpawnProp(Transform parent, string fileName, string objName, Vector3 pos, int sortingOrder, Vector2 colliderSize, bool isVerticalCapsule)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_FOLDER}/{fileName}");
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;

        if (sprite != null)
        {
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

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

    private static void SpawnTreeWithWind(Transform parent, string fileName, string objName, Vector3 pos, int sortingOrder, float trunkRadius, float windSpeed, float maxAngle)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_FOLDER}/{fileName}");
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;

        if (sprite != null)
        {
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        var circle = obj.AddComponent<CircleCollider2D>();
        circle.radius = trunkRadius;
        circle.offset = new Vector2(0f, -1.8f);

        // Wind Sway Animation
        var sway = obj.AddComponent<TreeWindSway>();
          // TreeOcclusionFader: tu dong gan cho tat ca cay co tan la
          if (obj.GetComponent<TreeOcclusionFader>() == null)
              obj.AddComponent<TreeOcclusionFader>();
        sway.SetupParameters(windSpeed, maxAngle, 0.02f);
    }

    private static void SpawnGlowingPillar(Transform parent, string baseFileName, string glowFileName, string objName, Vector3 pos, Color glowColor)
    {
        Sprite baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_FOLDER}/{baseFileName}");
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{EXTRACTED_FOLDER}/{glowFileName}");

        GameObject pillarObj = new GameObject(objName);
        pillarObj.transform.SetParent(parent);
        pillarObj.transform.position = pos;

        if (baseSprite != null)
        {
            var sr = pillarObj.AddComponent<SpriteRenderer>();
            sr.sprite = baseSprite;
            sr.sortingOrder = 4;
        }

        // Solid collider
        var box = pillarObj.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.7f);
        box.offset = new Vector2(0f, -0.75f);

        // Add Glowing Rune script
        GlowingRunePillar.Create(pillarObj, glowSprite, glowColor);
    }
}
