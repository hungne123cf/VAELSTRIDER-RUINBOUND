using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class ValleyRuinSetup
{
    private const string ASSET_ROOT = "Assets/ART/Map/Valley Ruin Asset Pack V0.3";
    private const string TILES_FOLDER = ASSET_ROOT + "/Tiles Maps";
    private const string OBJECTS_FOLDER = ASSET_ROOT + "/Objects";
    private const string GENERATED_TILES_FOLDER = ASSET_ROOT + "/GeneratedTiles";
    private const string PALETTE_PATH = ASSET_ROOT + "/ValleyRuin_Palette.prefab";
    private const string MAP1_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    private const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";

    static ValleyRuinSetup()
    {
        EditorApplication.delayCall += () =>
        {
            // Auto slice if palette not created yet
            if (!File.Exists(PALETTE_PATH) && Directory.Exists(TILES_FOLDER))
            {
                Debug.Log("<color=cyan>[VALLEY RUIN]</color> Khoi tao Tile Palette cho Map 2...");
                AutoSliceAndCreatePalette(false);
            }
        };
    }

    #region MAP 1 RESTORATION
    [MenuItem("Vaelstrider/Map 1/Restore Full 4-Room Dungeon", false, 1)]
    public static void RestoreMap1()
    {
        if (SceneManager.GetActiveScene().path != MAP1_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(MAP1_SCENE_PATH, OpenSceneMode.Single);
        }

        DungeonMapBuilder.BuildDungeon();

        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("<color=green>[MAP 1]</color> Da khoi phuc tron ven 4 phong cua Map 1!");
        EditorUtility.DisplayDialog("Map 1 Restored", 
            "Da khoi phuc tron ven Map 1 voi day du 4 phong:\n" +
            "1. Room 1: Sanctuary of Awakening (Den tho nang cap & Ruong khoi dau)\n" +
            "2. Room 2: The Sunken Hall (Ve binh tuan tra)\n" +
            "3. Room 3: The Cursed Crypt (Ham mo bi nguyen rua)\n" +
            "4. Room 4: Sanctum of the Titan (Boss: Titan Wheel)\n\n" +
            "Sau khi danh bai Boss o Room 4, Cong Dich Chuyen (Portal) se mo ra de sang Map 2!", "Tuyet voi!");
    }
    #endregion

    #region MAP 2 CREATION & SETUP
    [MenuItem("Vaelstrider/Map 2 (Valley Ruin)/1. Auto-Slice and Create Palette", false, 1)]
    public static void MenuSliceAndCreatePalette()
    {
        AutoSliceAndCreatePalette(true);
    }

    public static void AutoSliceAndCreatePalette(bool showDialog = true)
    {
        AssetDatabase.Refresh();

        if (!Directory.Exists(GENERATED_TILES_FOLDER))
        {
            Directory.CreateDirectory(GENERATED_TILES_FOLDER);
            AssetDatabase.Refresh();
        }

        // 1. Slice Tile Sheets (16x16, PPU 16, Point Filter, Uncompressed)
        SliceSheet(TILES_FOLDER + "/GrassTile.png", 16, 16, "GrassTile");
        SliceSheet(TILES_FOLDER + "/GrassWall.png", 16, 16, "GrassWall");
        SliceSheet(TILES_FOLDER + "/RockWall.png", 16, 16, "RockWall");
        SliceSheet(TILES_FOLDER + "/RockCliff.png", 16, 16, "RockCliff");
        SliceSheet(TILES_FOLDER + "/SandTile.png", 16, 16, "SandTile");
        SliceSheet(TILES_FOLDER + "/Sand plus.png", 16, 16, "SandPlus");
        SliceSheet(TILES_FOLDER + "/TopDown tile base.png", 16, 16, "TopDownBase");
        SliceSheet(TILES_FOLDER + "/fence.png", 16, 16, "Fence");
        SliceSheet(ASSET_ROOT + "/FlowerAndGrass.png", 16, 16, "FlowerGrass");

        ConfigureSingleSprite(TILES_FOLDER + "/DirtTile.png", 16);
        ConfigureSingleSprite(OBJECTS_FOLDER + "/Giant_Tree.png", 16);
        ConfigureSingleSprite(OBJECTS_FOLDER + "/stone and bush.png", 16);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        List<Tile> generatedTiles = new List<Tile>();
        generatedTiles.AddRange(GenerateTilesForTexture(TILES_FOLDER + "/GrassTile.png", "GrassTile"));
        generatedTiles.AddRange(GenerateTilesForTexture(TILES_FOLDER + "/GrassWall.png", "GrassWall"));
        generatedTiles.AddRange(GenerateTilesForTexture(TILES_FOLDER + "/RockWall.png", "RockWall"));
        generatedTiles.AddRange(GenerateTilesForTexture(TILES_FOLDER + "/RockCliff.png", "RockCliff"));
        generatedTiles.AddRange(GenerateTilesForTexture(TILES_FOLDER + "/SandTile.png", "SandTile"));
        generatedTiles.AddRange(GenerateTilesForTexture(TILES_FOLDER + "/DirtTile.png", "DirtTile"));
        generatedTiles.AddRange(GenerateTilesForTexture(TILES_FOLDER + "/fence.png", "Fence"));
        generatedTiles.AddRange(GenerateTilesForTexture(ASSET_ROOT + "/FlowerAndGrass.png", "FlowerGrass"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CreateTilePalette(generatedTiles);

        Debug.Log($"<color=green>[VALLEY RUIN]</color> Da tao thanh cong {generatedTiles.Count} Tiles & Palette: {PALETTE_PATH}");
        if (showDialog)
        {
            EditorUtility.DisplayDialog("Map 2 Setup", $"Hoan tat!\n- Da cat toan bo sheet 16x16 voi PPU = 16\n- Da tao {generatedTiles.Count} Tile assets\n- Da tao Palette prefab: ValleyRuin_Palette.prefab\n\nMo Window > 2D > Tile Palette va chon ValleyRuin_Palette de ve!", "OK");
        }
    }

    [MenuItem("Vaelstrider/Map 2 (Valley Ruin)/2. Create Blank Map 2 Scene (Map2_ValleyRuin)", false, 2)]
    public static void CreateBlankMap2Scene()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 1. Copy SampleScene as base to inherit all HUD, Player, Camera, Sound references
        if (!File.Exists(MAP2_SCENE_PATH))
        {
            AssetDatabase.CopyAsset(MAP1_SCENE_PATH, MAP2_SCENE_PATH);
            AssetDatabase.Refresh();
        }

        // 2. Open Map 2
        Scene map2Scene = EditorSceneManager.OpenScene(MAP2_SCENE_PATH, OpenSceneMode.Single);

        // 3. Clear all tiles from Grid to make a 100% blank canvas
        Grid grid = Object.FindAnyObjectByType<Grid>();
        if (grid != null)
        {
            Tilemap[] tilemaps = grid.GetComponentsInChildren<Tilemap>(true);
            foreach (var tm in tilemaps)
            {
                tm.ClearAllTiles();
                tm.RefreshAllTiles();
            }
        }

        // 4. Remove Map 1 specific dungeon objects
        string[] removeNames = new string[] {
            "DungeonBarriers", "DungeonTorches", "DungeonRooms",
            "WaveBanner", "TreasureChest_Crypt", "Map2_Giant_Tree"
        };
        foreach (var n in removeNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }

        // 5. Reset Player position to (0, 0, 0)
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = Vector3.zero;
        }

        // 6. Reposition Ancient Shrine near spawn so player can upgrade in Map 2
        var shrine = Object.FindAnyObjectByType<AncientShrine>();
        if (shrine != null)
        {
            shrine.transform.position = new Vector3(-4f, 0f, 0f);
        }

        // 7. Outdoor valley lighting
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == Light2D.LightType.Global)
            {
                l.color = new Color(0.92f, 0.94f, 1f, 1f); // Fresh valley outdoor daylight
                l.intensity = 0.95f;
            }
        }

        EditorSceneManager.MarkSceneDirty(map2Scene);
        EditorSceneManager.SaveScene(map2Scene);

        // 8. Register both scenes in Build Settings
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(MAP1_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP2_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = scenes;
        AssetDatabase.SaveAssets();

        Debug.Log("<color=green>[MAP 2]</color> Da tao thanh cong Scene Map 2 (Valley Ruin) hoan toan trong trang de ban tu tay ve!");
        EditorUtility.DisplayDialog("Map 2 Scene Created", 
            "Da tao xong Scene: Assets/Scenes/Map2_ValleyRuin.unity!\n\n" +
            "- Trang giay trang 100%: Nen Ground & Walls da duoc don sach sanh sanh.\n" +
            "- Day du he thong: Nhan vat, Camera, Nut dieu khien, Mau, Aethel Shard, Binh mau & Den tho.\n" +
            "- Da them vao Build Settings: Khi pha dao Map 1 se tu dong chuyen sang Map 2!\n\n" +
            "Bay gio ban hay mo Window > 2D > Tile Palette, chon ValleyRuin_Palette va thoa suc ve Map 2 theo y muon!", "Tuyet voi!");
    }

    [MenuItem("Vaelstrider/Scenes/Open Map 1 (SampleScene)")]
    public static void OpenMap1Scene()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(MAP1_SCENE_PATH, OpenSceneMode.Single);
    }

    [MenuItem("Vaelstrider/Scenes/Open Map 2 (Map2_ValleyRuin)")]
    public static void OpenMap2Scene()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (File.Exists(MAP2_SCENE_PATH))
        {
            EditorSceneManager.OpenScene(MAP2_SCENE_PATH, OpenSceneMode.Single);
        }
        else
        {
            CreateBlankMap2Scene();
        }
    }
    #endregion

    #region UTILITIES
    private static void SliceSheet(string assetPath, int tileWidth, int tileHeight, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            importer.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        if (tex != null)
        {
            int cols = tex.width / tileWidth;
            int rows = tex.height / tileHeight;
            List<SpriteMetaData> metaList = new List<SpriteMetaData>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    SpriteMetaData smd = new SpriteMetaData();
                    smd.name = $"{prefix}_{c}_{r}";
                    int yPos = (rows - 1 - r) * tileHeight;
                    int xPos = c * tileWidth;
                    smd.rect = new Rect(xPos, yPos, tileWidth, tileHeight);
                    smd.alignment = (int)SpriteAlignment.Center;
                    smd.pivot = new Vector2(0.5f, 0.5f);
                    metaList.Add(smd);
                }
            }
            importer.spritesheet = metaList.ToArray();
        }

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static void ConfigureSingleSprite(string assetPath, int ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static List<Tile> GenerateTilesForTexture(string assetPath, string groupName)
    {
        List<Tile> result = new List<Tile>();
        Object[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (objects == null || objects.Length == 0) return result;

        foreach (Object obj in objects)
        {
            if (obj is Sprite sprite)
            {
                string tileAssetPath = $"{GENERATED_TILES_FOLDER}/Tile_{sprite.name}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    AssetDatabase.CreateAsset(tile, tileAssetPath);
                }
                else
                {
                    tile.sprite = sprite;
                    EditorUtility.SetDirty(tile);
                }
                result.Add(tile);
            }
        }
        return result;
    }

    private static void CreateTilePalette(List<Tile> tiles)
    {
        if (tiles == null || tiles.Count == 0) return;

        GameObject paletteObj = new GameObject("ValleyRuin_Palette");
        Grid grid = paletteObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        GameObject layerObj = new GameObject("Layer1");
        layerObj.transform.SetParent(paletteObj.transform);
        Tilemap tilemap = layerObj.AddComponent<Tilemap>();
        layerObj.AddComponent<TilemapRenderer>();

        int cols = 10;
        for (int i = 0; i < tiles.Count; i++)
        {
            int x = i % cols;
            int y = -(i / cols);
            tilemap.SetTile(new Vector3Int(x, y, 0), tiles[i]);
        }

        PrefabUtility.SaveAsPrefabAsset(paletteObj, PALETTE_PATH);
        Object.DestroyImmediate(paletteObj);
        AssetDatabase.SaveAssets();
    }
    #endregion
}
