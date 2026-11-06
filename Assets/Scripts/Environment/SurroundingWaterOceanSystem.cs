using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Surrounding Water Ocean & Shoreline Waves System
/// 
/// ĐẶC TÍNH HÌNH ẢNH:
/// 1. Biến toàn bộ vùng trống / vùng tối màu xanh lục xung quanh và giữa các khu vực bản đồ
///    thành Mặt Nước Pixel-Art sống động (6 khung hình caustics lấp lánh + lớp sóng trôi liên tục).
/// 2. Tự động nhận diện mọi mép bờ (North, South, East, West) nơi sàn đấu / tường đá tiếp giáp với mặt nước
///    để tạo Bờ Đá Ven Hồ + Hiệu ứng Sóng Trắng Vỗ Dạt Bờ (Shoreline Lapping Waves) chân thật.
/// 3. Sử dụng SpriteDrawMode.Tiled & FullRect Mesh chuẩn Zero-Bug Android, siêu nhẹ (khóa cứng 60 FPS).
/// </summary>
public class SurroundingWaterOceanSystem : MonoBehaviour
{
    public enum ShoreDir { North, South, East, West }

    private struct ShoreSegment
    {
        public ShoreDir dir;
        public float fixedCoord; // Y cho North/South, X cho East/West
        public float startCoord; // X_start cho North/South, Y_start cho East/West
        public float endCoord;   // X_end cho North/South, Y_end cho East/West
    }

    private class AnimatedShoreVisual
    {
        public ShoreDir dir;
        public SpriteRenderer bankSr;
        public Transform foamTransform;
        public SpriteRenderer foamSr;
        public Vector3 foamBasePos;
        public Vector3 surgeDir; // Hướng sóng vỗ vào bờ
        public float phaseOffset;
    }

    private static SurroundingWaterOceanSystem instance;
    public static SurroundingWaterOceanSystem Instance => instance;

    private static Sprite[] s_OceanFrames;
    private static Sprite s_WaveCrestsSprite;
    private static Sprite[] s_ShoreNorthFrames;
    private static Sprite[] s_ShoreSouthFrames;
    private static Sprite[] s_ShoreEastFrames;
    private static Sprite[] s_ShoreWestFrames;
    private static Sprite s_FoamNorthSprite;
    private static Sprite s_FoamSouthSprite;
    private static Sprite s_FoamEastSprite;
    private static Sprite s_FoamWestSprite;
    private static Material s_UnlitWaterMaterial;

    private SpriteRenderer oceanBaseSr;
    private SpriteRenderer waveCrestOverlaySr;
    private Transform waveCrestOverlayTransform;
    private Vector3 oceanCenterPos;

    private readonly List<AnimatedShoreVisual> shoreVisuals = new List<AnimatedShoreVisual>();
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private const float FRAME_INTERVAL = 0.15f; // ~6.6 FPS chuẩn pixel art

    public static void EnsureWaterSystem()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "LoginScene" || sceneName == "MainMenuScene") return;

        var existing = FindAnyObjectByType<SurroundingWaterOceanSystem>();
        if (existing != null) return;

        GameObject go = new GameObject("SurroundingWaterOceanSystem");
        go.AddComponent<SurroundingWaterOceanSystem>();
    }

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
        EnsureSpritesLoaded();
        BuildWaterAndShorelinesForCurrentScene();
    }

    void Update()
    {
        float t = Time.time;

        // 1. Chuyển khung hình mặt nước & sóng vỗ bờ (6 frames)
        frameTimer += Time.deltaTime;
        if (frameTimer >= FRAME_INTERVAL)
        {
            frameTimer -= FRAME_INTERVAL;
            currentFrame = (currentFrame + 1) % 6;

            if (oceanBaseSr != null && s_OceanFrames != null && s_OceanFrames[currentFrame] != null)
            {
                oceanBaseSr.sprite = s_OceanFrames[currentFrame];
            }

            for (int i = 0; i < shoreVisuals.Count; i++)
            {
                var sv = shoreVisuals[i];
                if (sv.bankSr == null) continue;
                int fIdx = (currentFrame + i) % 6;
                switch (sv.dir)
                {
                    case ShoreDir.North:
                        if (s_ShoreNorthFrames[fIdx] != null) sv.bankSr.sprite = s_ShoreNorthFrames[fIdx];
                        break;
                    case ShoreDir.South:
                        if (s_ShoreSouthFrames[fIdx] != null) sv.bankSr.sprite = s_ShoreSouthFrames[fIdx];
                        break;
                    case ShoreDir.East:
                        if (s_ShoreEastFrames[fIdx] != null) sv.bankSr.sprite = s_ShoreEastFrames[fIdx];
                        break;
                    case ShoreDir.West:
                        if (s_ShoreWestFrames[fIdx] != null) sv.bankSr.sprite = s_ShoreWestFrames[fIdx];
                        break;
                }
            }
        }

        // 2. Chuyển động trôi mượt của lớp sóng lăn tăn trên mặt nước (Seamless modulo 6m x 3m)
        if (waveCrestOverlayTransform != null)
        {
            float driftX = Mathf.Repeat(t * 0.48f, 6.0f) - 3.0f;
            float driftY = Mathf.Repeat(t * 0.24f, 3.0f) - 1.5f;
            waveCrestOverlayTransform.position = oceanCenterPos + new Vector3(driftX, driftY, 0f);

            if (waveCrestOverlaySr != null)
            {
                float alpha = Mathf.Lerp(0.55f, 0.88f, (Mathf.Sin(t * 1.8f) + 1f) * 0.5f);
                waveCrestOverlaySr.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        // 3. Hiệu ứng sóng trắng dạt vào bờ và rút ra nhịp nhàng
        for (int i = 0; i < shoreVisuals.Count; i++)
        {
            var sv = shoreVisuals[i];
            if (sv.foamTransform == null || sv.foamSr == null) continue;

            // Chu kỳ sóng vỗ vào bờ (0 -> 1)
            float waveCycle = (Mathf.Sin(t * 2.3f + sv.phaseOffset) + 1f) * 0.5f;
            // Sóng đẩy sát vào bờ đá khoảng 0.24m rồi rút nhẹ ra
            float surgeDist = Mathf.Lerp(-0.10f, 0.16f, waveCycle);
            sv.foamTransform.position = sv.foamBasePos + sv.surgeDir * surgeDist;

            // Khi sóng vỗ sát bờ (waveCycle cao), bọt sóng trắng sáng rực lên rồi tan dần khi rút ra
            float foamAlpha = Mathf.Lerp(0.38f, 0.92f, Mathf.Sin(waveCycle * Mathf.PI));
            sv.foamSr.color = new Color(1f, 1f, 1f, foamAlpha);
        }
    }

    private static void EnsureSpritesLoaded()
    {
        if (s_OceanFrames != null && s_OceanFrames[0] != null) return;

        s_OceanFrames = new Sprite[6];
        s_ShoreNorthFrames = new Sprite[6];
        s_ShoreSouthFrames = new Sprite[6];
        s_ShoreEastFrames = new Sprite[6];
        s_ShoreWestFrames = new Sprite[6];

        for (int i = 0; i < 6; i++)
        {
            s_OceanFrames[i] = LoadFullRectSprite($"water_ocean_{i}");
            s_ShoreNorthFrames[i] = LoadFullRectSprite($"shore_north_{i}");
            s_ShoreSouthFrames[i] = LoadFullRectSprite($"shore_south_{i}");
            s_ShoreEastFrames[i] = LoadFullRectSprite($"shore_east_{i}");
            s_ShoreWestFrames[i] = LoadFullRectSprite($"shore_west_{i}");
        }

        s_WaveCrestsSprite = LoadFullRectSprite("water_wave_crests");
        s_FoamNorthSprite = LoadFullRectSprite("wave_foam_north");
        s_FoamSouthSprite = LoadFullRectSprite("wave_foam_south");
        s_FoamEastSprite = LoadFullRectSprite("wave_foam_east");
        s_FoamWestSprite = LoadFullRectSprite("wave_foam_west");

        if (s_UnlitWaterMaterial == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh != null) s_UnlitWaterMaterial = new Material(sh);
        }
    }

    private static Sprite LoadFullRectSprite(string name)
    {
        Texture2D tex = Resources.Load<Texture2D>("Water/" + name);
        if (tex == null)
        {
            var spr = Resources.Load<Sprite>("Water/" + name);
            if (spr != null) tex = spr.texture;
        }
#if UNITY_EDITOR
        if (tex == null)
        {
            tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Water/{name}.png");
        }
#endif
        if (tex == null) return null;

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            32f,
            0,
            SpriteMeshType.FullRect
        );
    }

    private void BuildWaterAndShorelinesForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // 1. Đổi màu nền Camera sang màu xanh nước biển sâu để triệt tiêu hoàn toàn màu xanh lục tối cũ
        Camera cam = Camera.main ?? FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.26f, 0.52f, 0.58f, 1f);
        }

        // 2. Ẩn các tấm đệm sàn tĩnh bên ngoài (nếu có) để toàn bộ vùng ngoài hiển thị mặt nước sống động
        var pad2 = GameObject.Find("Map2_OuterPaddingFloor");
        if (pad2 != null) pad2.SetActive(false);

        var pad3 = GameObject.Find("ArenaFloor_OuterPadding");
        if (pad3 != null) pad3.SetActive(false);

        // 3. Khởi tạo mặt nước đại dương bao phủ toàn bộ bản đồ
        if (sceneName.Contains("Map3") || sceneName.Contains("Forgotten"))
        {
            oceanCenterPos = new Vector3(0f, 42f, 0f);
            CreateOceanLayers(oceanCenterPos, new Vector2(340f, 260f), baseSortingOrder: -800);
            SetupMap3Shorelines();
        }
        else if (sceneName.Contains("Map2") || sceneName.Contains("Valley"))
        {
            oceanCenterPos = new Vector3(0f, -45f, 0f);
            CreateOceanLayers(oceanCenterPos, new Vector2(340f, 260f), baseSortingOrder: -500);
            SetupTilemapShorelines(shoreBankSortingOrder: 3, foamSortingOrder: -20);
            ExpandCameraBounds(new Vector2(-135f, -130f), new Vector2(135f, 22f));
        }
        else
        {
            // Map 1 (Map1_Thestart / SampleScene): Trải dài từ x=-17..72, y=-9..50
            oceanCenterPos = new Vector3(26f, 20f, 0f);
            CreateOceanLayers(oceanCenterPos, new Vector2(220f, 160f), baseSortingOrder: -100);
            SetupTilemapShorelines(shoreBankSortingOrder: 3, foamSortingOrder: -20);
            ExpandCameraBounds(new Vector2(-22f, -14f), new Vector2(76f, 54f));
        }
    }

    private void CreateOceanLayers(Vector3 center, Vector2 size, int baseSortingOrder)
    {
        // Lớp 1: Mặt nước nền động 6 khung hình
        GameObject oceanObj = new GameObject("WaterOcean_AnimatedSurface");
        oceanObj.transform.SetParent(transform, false);
        oceanObj.transform.position = center;

        oceanBaseSr = oceanObj.AddComponent<SpriteRenderer>();
        if (s_UnlitWaterMaterial != null) oceanBaseSr.sharedMaterial = s_UnlitWaterMaterial;
        oceanBaseSr.sprite = s_OceanFrames[0];
        oceanBaseSr.drawMode = SpriteDrawMode.Tiled;
        oceanBaseSr.size = size;
        oceanBaseSr.sortingOrder = baseSortingOrder;
        oceanBaseSr.color = Color.white;

        // Lớp 2: Sóng nước lăn tăn trôi trên bề mặt
        GameObject crestObj = new GameObject("WaterOcean_DriftingWaveCrests");
        crestObj.transform.SetParent(transform, false);
        crestObj.transform.position = center;
        waveCrestOverlayTransform = crestObj.transform;

        waveCrestOverlaySr = crestObj.AddComponent<SpriteRenderer>();
        if (s_UnlitWaterMaterial != null) waveCrestOverlaySr.sharedMaterial = s_UnlitWaterMaterial;
        waveCrestOverlaySr.sprite = s_WaveCrestsSprite;
        waveCrestOverlaySr.drawMode = SpriteDrawMode.Tiled;
        waveCrestOverlaySr.size = size + new Vector2(12f, 12f);
        waveCrestOverlaySr.sortingOrder = baseSortingOrder + 2;
        waveCrestOverlaySr.color = new Color(1f, 1f, 1f, 0.75f);
    }

    private void SetupMap3Shorelines()
    {
        // Đồng bộ kích thước sàn đấu trung tâm Map 3 (32m x 22m: -16..16, -11..11)
        var floorObj = GameObject.Find("ArenaFloor_Mossy");
        if (floorObj != null)
        {
            var sr = floorObj.GetComponent<SpriteRenderer>();
            if (sr != null && sr.drawMode == SpriteDrawMode.Tiled)
            {
                sr.size = new Vector2(32f, 22f);
            }
        }

        // Nếu Map 3 đã được mở rộng thành Quần Đảo Đa Nhánh (MultiPathMapExpansionBuilder), tạo sóng vỗ bao quanh toàn bộ đảo và cầu đá!
        if (MultiPathMapExpansionBuilder.Instance != null && MultiPathMapExpansionBuilder.Instance.WalkableRects.Count > 0)
        {
            HashSet<Vector2Int> landCells = new HashSet<Vector2Int>();
            foreach (var r in MultiPathMapExpansionBuilder.Instance.WalkableRects)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    for (int y = r.yMin; y < r.yMax; y++)
                    {
                        landCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            List<ShoreSegment> multiSegments = BuildShoreSegmentsFromLandCells(landCells);
            SpawnShoreVisuals(multiSegments, bankSortingOrder: -480, foamSortingOrder: -600, coordsAreTileIndices: true);
            ExpandCameraBounds(new Vector2(-126f, -22f), new Vector2(126f, 115f));
            return;
        }

        // Tạo 4 bờ đá + sóng vỗ bao quanh đảo cổ Map 3 (-16..16, -11..11)
        List<ShoreSegment> segments = new List<ShoreSegment>()
        {
            new ShoreSegment { dir = ShoreDir.North, fixedCoord = 11f,  startCoord = -16f, endCoord = 16f },
            new ShoreSegment { dir = ShoreDir.South, fixedCoord = -11f, startCoord = -16f, endCoord = 16f },
            new ShoreSegment { dir = ShoreDir.East,  fixedCoord = 16f,  startCoord = -11f, endCoord = 11f },
            new ShoreSegment { dir = ShoreDir.West,  fixedCoord = -16f, startCoord = -11f, endCoord = 11f }
        };

        SpawnShoreVisuals(segments, bankSortingOrder: -480, foamSortingOrder: -600, coordsAreTileIndices: false);
        ExpandCameraBounds(new Vector2(-21f, -15f), new Vector2(21f, 15f));
    }

    private void SetupTilemapShorelines(int shoreBankSortingOrder, int foamSortingOrder)
    {
        HashSet<Vector2Int> landCells = new HashSet<Vector2Int>();
        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            string n = tm.gameObject.name.ToLower();
            if (!n.Contains("ground") && !n.Contains("floor") && !n.Contains("wall")) continue;

            BoundsInt bounds = tm.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    if (tm.HasTile(cellPos))
                    {
                        landCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        if (landCells.Count == 0) return;

        List<ShoreSegment> segments = BuildShoreSegmentsFromLandCells(landCells);
        SpawnShoreVisuals(segments, shoreBankSortingOrder, foamSortingOrder, coordsAreTileIndices: true);
    }

    private static List<ShoreSegment> BuildShoreSegmentsFromLandCells(HashSet<Vector2Int> landCells)
    {
        // Tìm tất cả cạnh biên nơi ô đất/tường giáp với ô nước trống
        Dictionary<int, List<int>> northEdgesByY = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> southEdgesByY = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> eastEdgesByX  = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> westEdgesByX  = new Dictionary<int, List<int>>();

        foreach (var c in landCells)
        {
            // North: trên (x, y) là nước (x, y + 1)
            if (!landCells.Contains(new Vector2Int(c.x, c.y + 1)))
            {
                if (!northEdgesByY.TryGetValue(c.y, out var list)) northEdgesByY[c.y] = list = new List<int>();
                list.Add(c.x);
            }
            // South: dưới (x, y) là nước (x, y - 1)
            if (!landCells.Contains(new Vector2Int(c.x, c.y - 1)))
            {
                if (!southEdgesByY.TryGetValue(c.y, out var list)) southEdgesByY[c.y] = list = new List<int>();
                list.Add(c.x);
            }
            // East: phải (x, y) là nước (x + 1, y)
            if (!landCells.Contains(new Vector2Int(c.x + 1, c.y)))
            {
                if (!eastEdgesByX.TryGetValue(c.x, out var list)) eastEdgesByX[c.x] = list = new List<int>();
                list.Add(c.y);
            }
            // West: trái (x, y) là nước (x - 1, y)
            if (!landCells.Contains(new Vector2Int(c.x - 1, c.y)))
            {
                if (!westEdgesByX.TryGetValue(c.x, out var list)) westEdgesByX[c.x] = list = new List<int>();
                list.Add(c.y);
            }
        }

        List<ShoreSegment> segments = new List<ShoreSegment>();
        BuildMergedSegments(northEdgesByY, ShoreDir.North, segments);
        BuildMergedSegments(southEdgesByY, ShoreDir.South, segments);
        BuildMergedSegments(eastEdgesByX,  ShoreDir.East,  segments);
        BuildMergedSegments(westEdgesByX,  ShoreDir.West,  segments);
        return segments;
    }

    private static void BuildMergedSegments(Dictionary<int, List<int>> dict, ShoreDir dir, List<ShoreSegment> outSegments)
    {
        foreach (var kvp in dict)
        {
            int fixedIdx = kvp.Key;
            List<int> coords = kvp.Value;
            coords.Sort();

            int runStart = coords[0];
            int prev = coords[0];

            for (int i = 1; i < coords.Count; i++)
            {
                if (coords[i] == prev + 1)
                {
                    prev = coords[i];
                }
                else
                {
                    outSegments.Add(new ShoreSegment
                    {
                        dir = dir,
                        fixedCoord = fixedIdx,
                        startCoord = runStart,
                        endCoord = prev + 1
                    });
                    runStart = coords[i];
                    prev = coords[i];
                }
            }

            outSegments.Add(new ShoreSegment
            {
                dir = dir,
                fixedCoord = fixedIdx,
                startCoord = runStart,
                endCoord = prev + 1
            });
        }
    }

    private void SpawnShoreVisuals(List<ShoreSegment> segments, int bankSortingOrder, int foamSortingOrder, bool coordsAreTileIndices)
    {
        Transform shoresRoot = new GameObject("ShorelineWaveSegments").transform;
        shoresRoot.SetParent(transform, false);

        for (int i = 0; i < segments.Count; i++)
        {
            ShoreSegment seg = segments[i];
            float length = seg.endCoord - seg.startCoord;
            if (length <= 0.1f) continue;

            float mid = (seg.startCoord + seg.endCoord) * 0.5f;

            GameObject bankObj = new GameObject($"ShoreBank_{seg.dir}_{i}");
            bankObj.transform.SetParent(shoresRoot, false);
            SpriteRenderer bankSr = bankObj.AddComponent<SpriteRenderer>();
            if (s_UnlitWaterMaterial != null) bankSr.sharedMaterial = s_UnlitWaterMaterial;
            bankSr.drawMode = SpriteDrawMode.Tiled;
            bankSr.sortingOrder = bankSortingOrder;

            GameObject foamObj = new GameObject($"ShoreFoamWave_{seg.dir}_{i}");
            foamObj.transform.SetParent(shoresRoot, false);
            SpriteRenderer foamSr = foamObj.AddComponent<SpriteRenderer>();
            if (s_UnlitWaterMaterial != null) foamSr.sharedMaterial = s_UnlitWaterMaterial;
            foamSr.drawMode = SpriteDrawMode.Tiled;
            foamSr.sortingOrder = foamSortingOrder;

            Vector3 foamBasePos = Vector3.zero;
            Vector3 surgeDir = Vector3.zero;

            if (seg.dir == ShoreDir.North)
            {
                float boundaryY = coordsAreTileIndices ? (seg.fixedCoord + 1f) : seg.fixedCoord;
                bankSr.sprite = s_ShoreNorthFrames[0];
                bankSr.size = new Vector2(length, 1.5f);
                bankObj.transform.position = new Vector3(mid, boundaryY + 0.32f, 0f);

                foamSr.sprite = s_FoamNorthSprite;
                foamSr.size = new Vector2(length, 1.0f);
                foamBasePos = new Vector3(mid, boundaryY + 0.56f, 0f);
                surgeDir = Vector3.down; // Sóng vỗ từ trên xuống bờ
            }
            else if (seg.dir == ShoreDir.South)
            {
                float boundaryY = seg.fixedCoord;
                bankSr.sprite = s_ShoreSouthFrames[0];
                bankSr.size = new Vector2(length, 2.0f);
                bankObj.transform.position = new Vector3(mid, boundaryY - 0.38f, 0f);

                foamSr.sprite = s_FoamSouthSprite;
                foamSr.size = new Vector2(length, 1.0f);
                foamBasePos = new Vector3(mid, boundaryY - 0.68f, 0f);
                surgeDir = Vector3.up; // Sóng vỗ từ dưới lên bờ
            }
            else if (seg.dir == ShoreDir.East)
            {
                float boundaryX = coordsAreTileIndices ? (seg.fixedCoord + 1f) : seg.fixedCoord;
                bankSr.sprite = s_ShoreEastFrames[0];
                bankSr.size = new Vector2(1.0f, length);
                bankObj.transform.position = new Vector3(boundaryX + 0.14f, mid, 0f);

                foamSr.sprite = s_FoamEastSprite;
                foamSr.size = new Vector2(1.0f, length);
                foamBasePos = new Vector3(boundaryX + 0.48f, mid, 0f);
                surgeDir = Vector3.left; // Sóng vỗ từ phải sang bờ trái
            }
            else if (seg.dir == ShoreDir.West)
            {
                float boundaryX = seg.fixedCoord;
                bankSr.sprite = s_ShoreWestFrames[0];
                bankSr.size = new Vector2(1.0f, length);
                bankObj.transform.position = new Vector3(boundaryX - 0.14f, mid, 0f);

                foamSr.sprite = s_FoamWestSprite;
                foamSr.size = new Vector2(1.0f, length);
                foamBasePos = new Vector3(boundaryX - 0.48f, mid, 0f);
                surgeDir = Vector3.right; // Sóng vỗ từ trái sang bờ phải
            }

            foamObj.transform.position = foamBasePos;

            shoreVisuals.Add(new AnimatedShoreVisual
            {
                dir = seg.dir,
                bankSr = bankSr,
                foamTransform = foamObj.transform,
                foamSr = foamSr,
                foamBasePos = foamBasePos,
                surgeDir = surgeDir,
                phaseOffset = (i * 0.85f) % (Mathf.PI * 2f)
            });
        }
    }

    private static void ExpandCameraBounds(Vector2 min, Vector2 max)
    {
        var mb = FindAnyObjectByType<MapBounds>();
        if (mb != null)
        {
            mb.SetBounds(min, max);
        }

        Camera cam = Camera.main ?? FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            var cf = cam.GetComponent<CameraFollow>();
            if (cf != null)
            {
                cf.SetMapBounds(min, max);
            }
        }
    }
}
