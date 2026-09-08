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
    private const string MAP1_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
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

        // 3. Remove any flat background or obsolete objects
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

        // 4. Create the Grid and Real Tilemaps: Ground & Walls
        GameObject gridObj = new GameObject("Grid");
        Grid grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // 4A. Ground Tilemap (Floor only: Grass & Dirt Arena, NO collider)
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

        // Solid Physics: Rigidbody2D Static + TilemapCollider2D
        var wallsCol = wallsObj.AddComponent<TilemapCollider2D>();
        var wallsRb = wallsObj.AddComponent<Rigidbody2D>();
        wallsRb.bodyType = RigidbodyType2D.Static;

        // 5. Load Real Tile Assets
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

        // 6. Paint Real Ground Terrain (Grass base & Central Dirt Clearing)
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

        // 6B. Central Dirt Arena Clearing (Natural circular shape in center matching image)
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

        // 6C. Paint Solid Perimeter Walls on "Walls" Tilemap
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

        // 7. Create Real Props with Physical 2D Colliders
        GameObject envRoot = new GameObject("Map2_Environment");

        // 7A. Giant Ancient Tree (Top Center)
        Sprite giantTreeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OBJECTS_FOLDER + "/Giant_Tree.png");
        if (giantTreeSprite != null)
        {
            GameObject treeObj = new GameObject("Giant_Ancient_Tree");
            treeObj.transform.SetParent(envRoot.transform);
            treeObj.transform.position = new Vector3(0f, 6.5f, 0f);
            var sr = treeObj.AddComponent<SpriteRenderer>();
            sr.sprite = giantTreeSprite;
            sr.sortingOrder = 5;

            // SOLID Colliders on left and right roots (cannot walk through!)
            var colLeft = treeObj.AddComponent<BoxCollider2D>();
            colLeft.size = new Vector2(5.5f, 3.2f);
            colLeft.offset = new Vector2(-4.8f, -1.2f);

            var colRight = treeObj.AddComponent<BoxCollider2D>();
            colRight.size = new Vector2(5.5f, 3.2f);
            colRight.offset = new Vector2(4.8f, -1.2f);

            // Back wall of canopy
            var colBack = treeObj.AddComponent<BoxCollider2D>();
            colBack.size = new Vector2(18f, 2.5f);
            colBack.offset = new Vector2(0f, 3.0f);
        }

        // 7B. Glowing Sanctum Doorway (in the tree hollow)
        GameObject doorObj = new GameObject("SanctumDoor_Glow");
        doorObj.transform.SetParent(envRoot.transform);
        doorObj.transform.position = new Vector3(0f, 6.2f, 0f);
        var doorLight = doorObj.AddComponent<Light2D>();
        doorLight.lightType = Light2D.LightType.Point;
        doorLight.color = new Color(0.75f, 0.95f, 1f, 1f);
        doorLight.intensity = 2.8f;
        doorLight.pointLightInnerRadius = 0.6f;
        doorLight.pointLightOuterRadius = 4.5f;

        // 7C. Interactive Golden Treasure Chest (in front of tree)
        var chest = Object.FindAnyObjectByType<TreasureChest>();
        if (chest == null) chest = TreasureChest.SpawnChest(new Vector3(0f, 4.0f, 0f));
        else chest.transform.position = new Vector3(0f, 4.0f, 0f);
        chest.gameObject.name = "Map2_GoldenTreasureChest";

        // 7D. Slanted Mossy Boulder (right of tree)
        SpawnProp(envRoot.transform, "obj_47_44x77.png", "Rock_Slanted_Right", new Vector3(5.5f, 6.8f, 0f), 6, new Vector2(2.4f, 4.2f), true);

        // 7E. Mossy Boulder (left of tree)
        SpawnProp(envRoot.transform, "obj_21_49x71.png", "Rock_Mossy_Left", new Vector3(-5.5f, 6.8f, 0f), 6, new Vector2(2.6f, 3.8f), true);

        // 7F. The 4 Standalone Trees around the clearing (SOLID tree trunk colliders)
        SpawnTreeProp(envRoot.transform, "obj_07_79x89.png", "Tree_MidLeft", new Vector3(-6.8f, -1.8f, 0f), 4, 0.9f);
        SpawnTreeProp(envRoot.transform, "obj_08_72x90.png", "Tree_BottomLeft", new Vector3(-4.5f, -6.5f, 0f), 4, 0.9f);
        SpawnTreeProp(envRoot.transform, "obj_10_67x85.png", "Tree_MidRight", new Vector3(9.5f, -1.2f, 0f), 4, 0.9f);
        SpawnTreeProp(envRoot.transform, "obj_07_79x89.png", "Tree_BottomRight", new Vector3(13.2f, -4.8f, 0f), 4, 0.9f);

        // 7G. Boulders in the glade (SOLID rock colliders)
        SpawnProp(envRoot.transform, "obj_32_44x27.png", "Boulder_MidLeft", new Vector3(-10.8f, -1.5f, 0f), 3, new Vector2(2.2f, 1.4f), false);
        SpawnProp(envRoot.transform, "obj_33_44x27.png", "Boulder_BottomRight", new Vector3(7.5f, -9.2f, 0f), 3, new Vector2(2.4f, 1.4f), false);
        SpawnProp(envRoot.transform, "obj_31_32x27.png", "Boulder_FarRight", new Vector3(14.0f, -1.8f, 0f), 3, new Vector2(1.8f, 1.3f), false);
        SpawnProp(envRoot.transform, "obj_35_48x47.png", "Boulder_BottomLeft_Mossy", new Vector3(-6.5f, -7.2f, 0f), 3, new Vector2(2.4f, 2.2f), false);

        // 7H. Ruin Column at Bottom
        SpawnProp(envRoot.transform, "obj_52_14x38.png", "Ruin_Column", new Vector3(-2.2f, -11.5f, 0f), 4, new Vector2(0.9f, 2.2f), false);

        // 7I. Dense Forest Border Trees (Left and Right edges)
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
            SpawnTreeProp(envRoot.transform, "obj_07_79x89.png", "Forest_Border_Tree", pos, 4, 1.0f);
        }

        // 8. Ancient Upgrade Shrine
        var shrine = Object.FindAnyObjectByType<AncientShrine>();
        if (shrine != null) shrine.transform.position = new Vector3(-9.5f, 3.5f, 0f);

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

        // 10. Player Spawn Point (inside dirt arena)
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

        Debug.Log("<color=green>[MAP 2]</color> Kien tao hoan tat Map 2: Ground va Walls chuan 100% co Collider vat ly khong the di xuyen!");
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

    private static void SpawnTreeProp(Transform parent, string fileName, string objName, Vector3 pos, int sortingOrder, float trunkRadius)
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

        // SOLID CircleCollider2D at tree trunk base so player cannot walk through tree!
        var circle = obj.AddComponent<CircleCollider2D>();
        circle.radius = trunkRadius;
        circle.offset = new Vector2(0f, -1.8f);
    }
}
