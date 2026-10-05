using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MapBoundsApplier
{
    private const string TRIGGER_FILE = "Temp/apply_camera_bounds.trigger";

    static MapBoundsApplier()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch {}
                ApplyCameraBoundsToAllMaps();
            }
        };
    }

    [MenuItem("Vaelstrider/Camera/Apply Camera Bounds To Map 1, 2, 3", false, 10)]
    public static void ApplyCameraBoundsToAllMaps()
    {
        Debug.Log("<color=cyan>[CAMERA BOUNDS]</color> Bắt đầu áp dụng Camera Bounds Clamping cho Map 1, 2, 3...");

        ApplyToMap1();
        ApplyToMap2();
        ApplyToMap3();

        Debug.Log("<color=green>[CAMERA BOUNDS SUCCESS]</color> Hoàn tất cài đặt giới hạn Camera Bounds cho cả 3 Map!");
    }

    private static void SetupCameraInScene(Vector2 min, Vector2 max, Color bgCol)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindAnyObjectByType<Camera>();
        }

        if (cam != null)
        {
            cam.backgroundColor = bgCol;
            var cf = cam.GetComponent<CameraFollow>();
            if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();

            cf.SetMapBounds(min, max);
            cf.EnableBoundsClamp = true;
            EditorUtility.SetDirty(cf);
            EditorUtility.SetDirty(cam);
        }

        // Tạo hoặc cập nhật GameObject MapBounds
        var mb = Object.FindAnyObjectByType<MapBounds>();
        if (mb == null)
        {
            GameObject mbObj = new GameObject("MapBounds_Config");
            mb = mbObj.AddComponent<MapBounds>();
        }
        mb.SetBounds(min, max);
        EditorUtility.SetDirty(mb);
    }

    private static void ApplyToMap1()
    {
        string path = "Assets/Scenes/Map1_Thestart.unity";
        if (!File.Exists(path)) return;

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("<color=yellow>[MAP 1]</color> Đang cấu hình Map1_Thestart...");

        // Map 1 Dungeon: x từ -17 đến 72, y từ -9 đến 50
        Vector2 min = new Vector2(-17f, -9f);
        Vector2 max = new Vector2(72f, 50f);
        SetupCameraInScene(min, max, new Color(0.08f, 0.09f, 0.11f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ApplyToMap2()
    {
        string path = "Assets/Scenes/Map2_ValleyRuin.unity";
        if (!File.Exists(path)) return;

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("<color=yellow>[MAP 2]</color> Đang cấu hình Map2_ValleyRuin...");

        // Map 2 Valley Ruin: x từ -20 đến 20, y từ -14 đến 14
        Vector2 min = new Vector2(-20f, -14f);
        Vector2 max = new Vector2(20f, 14f);
        SetupCameraInScene(min, max, new Color(0.12f, 0.16f, 0.11f));

        // Thêm đệm sàn ngoài cho Map 2 nếu chưa có
        GameObject outerPad = GameObject.Find("Map2_OuterPaddingFloor");
        if (outerPad == null)
        {
            outerPad = new GameObject("Map2_OuterPaddingFloor");
            var sr = outerPad.AddComponent<SpriteRenderer>();
            var grassSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/Valley Ruin Asset Pack V0.3/Tiles Maps/Valley_Floor_Grass.png");
            if (grassSprite != null)
            {
                sr.sprite = grassSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(58f, 44f);
                sr.sortingOrder = -999;
                sr.color = new Color(0.55f, 0.62f, 0.52f, 1f);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ApplyToMap3()
    {
        string path = "Assets/Scenes/Map3_ForgottenMemories.unity";
        if (!File.Exists(path)) return;

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("<color=yellow>[MAP 3]</color> Đang cấu hình Map3_ForgottenMemories...");

        // Map 3 Forgotten Memories: x từ -16 đến 16, y từ -11 đến 11
        Vector2 min = new Vector2(-16f, -11f);
        Vector2 max = new Vector2(16f, 11f);
        SetupCameraInScene(min, max, new Color(0.12f, 0.16f, 0.11f));

        // Thêm thảm cỏ đệm mở rộng ngoại vi (Extended Outer Padding)
        GameObject outerPad = GameObject.Find("ArenaFloor_OuterPadding");
        if (outerPad == null)
        {
            outerPad = new GameObject("ArenaFloor_OuterPadding");
            var sr = outerPad.AddComponent<SpriteRenderer>();
            var mossSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/ForgottenMemories/Extracted/fm_floor_moss.png");
            if (mossSprite != null)
            {
                sr.sprite = mossSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(56f, 40f);
                sr.sortingOrder = -999;
                sr.color = new Color(0.65f, 0.72f, 0.62f, 1f);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
