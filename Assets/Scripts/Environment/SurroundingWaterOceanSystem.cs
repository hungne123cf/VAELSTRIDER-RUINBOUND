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

    private struct ShorelineAudioEdge
    {
        public Vector2 a;
        public Vector2 b;
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
    private static AudioClip s_ShorelineWavesClip;

    private SpriteRenderer oceanBaseSr;
    private SpriteRenderer waveCrestOverlaySr;
    private Transform waveCrestOverlayTransform;
    private Vector3 oceanCenterPos;

    private readonly List<AnimatedShoreVisual> shoreVisuals = new List<AnimatedShoreVisual>();
    private readonly List<ShorelineAudioEdge> shoreAudioEdges = new List<ShorelineAudioEdge>();
    private readonly HashSet<Vector2Int> validLandCells = new HashSet<Vector2Int>();
    private AudioSource shoreWaveAudioSource;
    private Transform cachedPlayerTransform;
    private float nextPlayerSearchTime = 0f;
    private const float SHORE_AUDIO_MAX_DIST = 9.5f;
    private const float SHORE_AUDIO_FULL_DIST = 1.6f;

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

    private Camera cachedMainCam;

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
        SetupShorelineProximityAudio();
        cachedMainCam = Camera.main ?? FindAnyObjectByType<Camera>();
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            cachedPlayerTransform = pc.transform;
        }
    }

    void Update()
    {
        float t = Time.time;

        // 1. Chuyển động trôi mượt của lớp sóng lăn tăn trên mặt nước (Zero-Rebuild Transform Translation)
        // Không gán lại .sprite hoặc .color trên SpriteDrawMode.Tiled trong Update để tránh CPU phải rebuild Tiled Mesh mỗi frame!
        if (waveCrestOverlayTransform != null)
        {
            float driftX = Mathf.Repeat(t * 0.48f, 6.0f) - 3.0f;
            float driftY = Mathf.Repeat(t * 0.24f, 3.0f) - 1.5f;
            waveCrestOverlayTransform.position = oceanCenterPos + new Vector3(driftX, driftY, 0f);
        }

        if (cachedMainCam == null) cachedMainCam = Camera.main;
        Vector3 camPos = cachedMainCam != null ? cachedMainCam.transform.position : Vector3.zero;
        bool hasCam = cachedMainCam != null;

        // 2. Hiệu ứng sóng trắng dạt vào bờ và rút ra nhịp nhàng (Chỉ cập nhật các đoạn bờ gần Camera trong bán kính 34m)
        int count = shoreVisuals.Count;
        for (int i = 0; i < count; i++)
        {
            var sv = shoreVisuals[i];
            if (sv.foamTransform == null) continue;

            if (hasCam)
            {
                float dx = sv.foamBasePos.x - camPos.x;
                float dy = sv.foamBasePos.y - camPos.y;
                if (dx * dx + dy * dy > 1156f) continue; // Ngoài tầm nhìn Camera (>34m) -> bỏ qua
            }

            float waveCycle = (Mathf.Sin(t * 2.3f + sv.phaseOffset) + 1f) * 0.5f;
            float surgeDist = Mathf.Lerp(-0.10f, 0.16f, waveCycle);
            sv.foamTransform.position = sv.foamBasePos + sv.surgeDir * surgeDist;
        }

        // 3. Công nghệ Âm thanh Sóng vỗ Dạt bờ theo Khoảng cách Cạnh Map & Định hướng Tai nghe Trái/Phải (Stereo Proximity Shoreline Audio)
        UpdateShorelineProximityAudio(t, camPos);
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

        // 3. Khởi tạo mặt nước đại dương bao phủ toàn bộ bản đồ (kích thước vừa vặn chuẩn 60 FPS)
        if (sceneName.Contains("Map3") || sceneName.Contains("Forgotten"))
        {
            oceanCenterPos = new Vector3(24f, 26f, 0f);
            CreateOceanLayers(oceanCenterPos, new Vector2(150f, 130f), baseSortingOrder: -800);
            SetupMap3Shorelines();
        }
        else if (sceneName.Contains("Map2") || sceneName.Contains("Valley"))
        {
            oceanCenterPos = new Vector3(26f, -26f, 0f);
            CreateOceanLayers(oceanCenterPos, new Vector2(160f, 130f), baseSortingOrder: -500);
            SetupTilemapShorelines(shoreBankSortingOrder: 3, foamSortingOrder: -20);
            ExpandCameraBounds(new Vector2(-24f, -70f), new Vector2(80f, 18f));
        }
        else
        {
            // Map 1 (Map1_Thestart / SampleScene): Trải dài từ x=-17..72, y=-9..50
            oceanCenterPos = new Vector3(26f, 20f, 0f);
            CreateOceanLayers(oceanCenterPos, new Vector2(150f, 120f), baseSortingOrder: -100);
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
        validLandCells.Clear();

        // Đồng bộ kích thước sàn đấu trung tâm Map 3 (32m x 22m: -16..16, -11..11)
        var floorObj = GameObject.Find("ArenaFloor_Mossy");
        if (floorObj != null)
        {
            var sr = floorObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (sr.drawMode == SpriteDrawMode.Tiled)
                {
                    sr.size = new Vector2(32f, 22f);
                }
                if (sr.sortingOrder > -500)
                {
                    sr.sortingOrder = -1500;
                }
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
                        Vector2Int cell = new Vector2Int(x, y);
                        landCells.Add(cell);
                        validLandCells.Add(cell);
                    }
                }
            }

            List<ShoreSegment> multiSegments = BuildShoreSegmentsFromLandCells(landCells);
            SpawnShoreVisuals(multiSegments, bankSortingOrder: -480, foamSortingOrder: -600, coordsAreTileIndices: true);
            ExpandCameraBounds(new Vector2(-22f, -16f), new Vector2(76f, 68f));
            return;
        }

        for (int x = -16; x < 16; x++)
        {
            for (int y = -11; y < 11; y++)
            {
                validLandCells.Add(new Vector2Int(x, y));
            }
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
        validLandCells.Clear();
        HashSet<Vector2Int> landCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> wallCells = new HashSet<Vector2Int>();
        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            string n = tm.gameObject.name.ToLower();
            bool isGround = n.Contains("ground") || n.Contains("floor");
            bool isWall = n.Contains("wall");
            if (!isGround && !isWall) continue;

            BoundsInt bounds = tm.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    if (tm.HasTile(cellPos))
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        landCells.Add(cell);
                        if (isWall) wallCells.Add(cell);
                    }
                }
            }
        }

        if (MultiPathMapExpansionBuilder.Instance != null && MultiPathMapExpansionBuilder.Instance.WalkableRects.Count > 0)
        {
            foreach (var r in MultiPathMapExpansionBuilder.Instance.WalkableRects)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    for (int y = r.yMin; y < r.yMax; y++)
                    {
                        validLandCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        else
        {
            // Bao gồm toàn bộ ô đất và tường hợp lệ của Dungeon để người chơi có thể tiếp xúc sát chân tường tự nhiên
            foreach (var c in landCells)
            {
                validLandCells.Add(c);
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
            float phase = (i * 0.85f) % (Mathf.PI * 2f);
            Vector2 edgeStart = Vector2.zero;
            Vector2 edgeEnd = Vector2.zero;

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
                edgeStart = new Vector2(seg.startCoord, boundaryY);
                edgeEnd = new Vector2(seg.endCoord, boundaryY);
                // Bờ Bắc: Nước ở phía trên boundaryY. Đặt collider nằm hoàn toàn ngoài vùng nước (từ boundaryY trở lên) để người chơi chạm sát chân tường tự nhiên
                SpawnShorelineWallCollider(shoresRoot, new Vector2(mid, boundaryY + 1.0f), new Vector2(length, 2.0f));
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
                edgeStart = new Vector2(seg.startCoord, boundaryY);
                edgeEnd = new Vector2(seg.endCoord, boundaryY);
                // Bờ Nam: Nước ở phía dưới boundaryY. Collider nằm hoàn toàn dưới mép sàn đá (từ boundaryY trở xuống), giúp người chơi đi sát tới mép tường mà không bị chặn từ xa
                SpawnShorelineWallCollider(shoresRoot, new Vector2(mid, boundaryY - 1.0f), new Vector2(length, 2.0f));
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
                edgeStart = new Vector2(boundaryX, seg.startCoord);
                edgeEnd = new Vector2(boundaryX, seg.endCoord);
                // Bờ Đông: Nước ở phía bên phải boundaryX. Collider nằm hoàn toàn ngoài vùng nước (từ boundaryX trở sang phải)
                SpawnShorelineWallCollider(shoresRoot, new Vector2(boundaryX + 1.0f, mid), new Vector2(2.0f, length));
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
                edgeStart = new Vector2(boundaryX, seg.startCoord);
                edgeEnd = new Vector2(boundaryX, seg.endCoord);
                // Bờ Tây: Nước ở phía bên trái boundaryX. Collider nằm hoàn toàn ngoài vùng nước (từ boundaryX trở sang trái)
                SpawnShorelineWallCollider(shoresRoot, new Vector2(boundaryX - 1.0f, mid), new Vector2(2.0f, length));
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
                phaseOffset = phase
            });

            shoreAudioEdges.Add(new ShorelineAudioEdge
            {
                a = edgeStart,
                b = edgeEnd,
                phaseOffset = phase
            });
        }
    }

    private static void SpawnShorelineWallCollider(Transform parent, Vector2 center, Vector2 size)
    {
        GameObject wallObj = new GameObject("ShorelineWallCollider");
        wallObj.transform.SetParent(parent, false);
        wallObj.transform.position = new Vector3(center.x, center.y, 0f);
        var rb = wallObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var box = wallObj.AddComponent<BoxCollider2D>();
        box.size = size;
    }

    /// <summary>
    /// Kẹp chặt vị trí thực thể trên đất liền / đảo hợp lệ, ngăn tuyệt đối việc lọt ra vùng nước.
    /// Cho phép tiếp cận sát mép tường và chướng ngại vật tự nhiên, để Unity Physics 2D xử lý.
    /// </summary>
    public static Vector3 ClampToWalkableLand(Vector3 pos, float padding = 0.55f)
    {
        var builder = MultiPathMapExpansionBuilder.Instance;
        bool hasWalkableRects = builder != null && builder.WalkableRects != null && builder.WalkableRects.Count > 0;

        if (instance != null && instance.validLandCells.Count > 0)
        {
            var cells = instance.validLandCells;
            float x = pos.x;
            float y = pos.y;
            int cx = Mathf.FloorToInt(x);
            int cy = Mathf.FloorToInt(y);

            // Nếu đã lọt ra ngoài ô đất hợp lệ (vào vùng nước): kéo ngay về điểm gần nhất trên WalkableRect hoặc ô đất gần nhất
            if (!cells.Contains(new Vector2Int(cx, cy)))
            {
                if (hasWalkableRects)
                {
                    float bestDistSq = float.MaxValue;
                    Vector2 bestPt = new Vector2(x, y);
                    foreach (var r in builder.WalkableRects)
                    {
                        float rx = Mathf.Clamp(x, r.xMin + padding, r.xMax - padding);
                        float ry = Mathf.Clamp(y, r.yMin + padding, r.yMax - padding);
                        float dSq = (x - rx) * (x - rx) + (y - ry) * (y - ry);
                        if (dSq < bestDistSq)
                        {
                            bestDistSq = dSq;
                            bestPt = new Vector2(rx, ry);
                        }
                    }
                    return new Vector3(bestPt.x, bestPt.y, pos.z);
                }
                else
                {
                    float bestDistSq = float.MaxValue;
                    Vector2Int bestCell = new Vector2Int(cx, cy);
                    foreach (var c in cells)
                    {
                        float cellCenterX = c.x + 0.5f;
                        float cellCenterY = c.y + 0.5f;
                        float dSq = (x - cellCenterX) * (x - cellCenterX) + (y - cellCenterY) * (y - cellCenterY);
                        if (dSq < bestDistSq)
                        {
                            bestDistSq = dSq;
                            bestCell = c;
                        }
                    }
                    cx = bestCell.x;
                    cy = bestCell.y;
                    x = Mathf.Clamp(x, (float)cx + 0.25f, (float)cx + 0.75f);
                    y = Mathf.Clamp(y, (float)cy + 0.25f, (float)cy + 0.75f);
                    return new Vector3(x, y, pos.z);
                }
            }

            // Nếu người chơi đang đứng trên ô đất hợp lệ: Để nguyên cho hệ thống Collider vật lý của Unity (Tilemap/Walls)
            // tự nhiên ngăn chặn va chạm, tuyệt đối KHÔNG ép thêm khoảng đệm padding nhân tạo gây cảm giác "tường vô hình cản lại"
            return pos;
        }

        if (hasWalkableRects)
        {
            // Nếu đã nằm trong ít nhất một WalkableRect hợp lệ: Cho phép di chuyển tự do sát tường
            foreach (var r in builder.WalkableRects)
            {
                if (pos.x >= r.xMin && pos.x <= r.xMax && pos.y >= r.yMin && pos.y <= r.yMax)
                {
                    return pos;
                }
            }

            // Chỉ cứu hộ kéo về mép khi thực sự lọt ra ngoài vùng đi bộ
            float bestDistSq = float.MaxValue;
            Vector2 bestPt = new Vector2(pos.x, pos.y);
            foreach (var r in builder.WalkableRects)
            {
                float rx = Mathf.Clamp(pos.x, r.xMin, r.xMax);
                float ry = Mathf.Clamp(pos.y, r.yMin, r.yMax);
                float dSq = (pos.x - rx) * (pos.x - rx) + (pos.y - ry) * (pos.y - ry);
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestPt = new Vector2(rx, ry);
                }
            }
            return new Vector3(bestPt.x, bestPt.y, pos.z);
        }

        return pos;
    }

    // =========================================================================
    // CÔNG NGHỆ ÂM THANH SÓNG VỖ DẠT BỜ THEO KHOẢNG CÁCH & ĐỊNH HƯỚNG TAI NGHE
    // =========================================================================
    private void SetupShorelineProximityAudio()
    {
        if (s_ShorelineWavesClip == null)
        {
            s_ShorelineWavesClip = Resources.Load<AudioClip>("Audio/SFX/shoreline_waves_loop");
#if UNITY_EDITOR
            if (s_ShorelineWavesClip == null)
            {
                s_ShorelineWavesClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/shoreline_waves_loop.wav");
            }
#endif
            if (s_ShorelineWavesClip == null)
            {
                s_ShorelineWavesClip = BuildProceduralShorelineWaveClip();
            }
        }

        GameObject audioObj = new GameObject("ShorelineProximityWaveAudio");
        audioObj.transform.SetParent(transform, false);

        shoreWaveAudioSource = audioObj.AddComponent<AudioSource>();
        shoreWaveAudioSource.clip = s_ShorelineWavesClip;
        shoreWaveAudioSource.loop = true;
        shoreWaveAudioSource.playOnAwake = false;
        shoreWaveAudioSource.spatialBlend = 0f; // Điều khiển suy hao khoảng cách 2D + Stereo Pan trực tiếp siêu mượt
        shoreWaveAudioSource.volume = 0f;
        shoreWaveAudioSource.pitch = 1.0f;
        shoreWaveAudioSource.Play();
    }

    private void UpdateShorelineProximityAudio(float t, Vector3 fallbackCamPos)
    {
        if (shoreWaveAudioSource == null || shoreAudioEdges.Count == 0) return;

        // 1. Lấy vị trí của Player (ưu tiên PlayerController, dự phòng Camera nếu chưa tìm thấy)
        if (cachedPlayerTransform == null && t >= nextPlayerSearchTime)
        {
            nextPlayerSearchTime = t + 0.5f;
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) cachedPlayerTransform = pc.transform;
        }

        Vector2 listenerPos = cachedPlayerTransform != null
            ? (Vector2)cachedPlayerTransform.position
            : (Vector2)fallbackCamPos;

        // 2. Tìm điểm mép bờ nước gần vị trí người chơi nhất trên toàn bộ bản đồ (Zero-Alloc)
        float minDistSq = float.MaxValue;
        Vector2 nearestShorePt = listenerPos;
        float nearestPhase = 0f;

        int edgeCount = shoreAudioEdges.Count;
        for (int i = 0; i < edgeCount; i++)
        {
            ShorelineAudioEdge edge = shoreAudioEdges[i];
            Vector2 ab = edge.b - edge.a;
            float lenSq = ab.sqrMagnitude;
            float proj = lenSq > 0.0001f ? Mathf.Clamp01(Vector2.Dot(listenerPos - edge.a, ab) / lenSq) : 0f;
            Vector2 pt = edge.a + ab * proj;
            float dSq = (listenerPos - pt).sqrMagnitude;

            if (dSq < minDistSq)
            {
                minDistSq = dSq;
                nearestShorePt = pt;
                nearestPhase = edge.phaseOffset;
            }
        }

        float maxDistSq = SHORE_AUDIO_MAX_DIST * SHORE_AUDIO_MAX_DIST;
        if (minDistSq >= maxDistSq)
        {
            // Người chơi đứng ở khu vực giữa bản đồ -> tắt dần tiếng sóng về 0
            if (shoreWaveAudioSource.volume > 0.001f)
            {
                shoreWaveAudioSource.volume = Mathf.MoveTowards(shoreWaveAudioSource.volume, 0f, Time.deltaTime * 1.5f);
            }
            else
            {
                shoreWaveAudioSource.volume = 0f;
            }
            return;
        }

        // 3. Tính độ lớn âm thanh theo khoảng cách thực tế tới mép nước (càng sát mép bờ tiếng sóng càng rõ)
        float dist = Mathf.Sqrt(minDistSq);
        float proximity = Mathf.Clamp01(1f - (dist - SHORE_AUDIO_FULL_DIST) / (SHORE_AUDIO_MAX_DIST - SHORE_AUDIO_FULL_DIST));
        float distAttenuation = proximity * (0.35f + 0.65f * proximity);

        // 4. Đồng bộ nhịp sóng vỗ âm thanh khớp với nhịp sóng bọt trắng dạt vào bờ (Visual-Audio Sync)
        float waveCycle = (Mathf.Sin(t * 2.3f + nearestPhase) + 1f) * 0.5f;
        float waveSurgeEnvelope = Mathf.Lerp(0.65f, 1.15f, waveCycle);

        float sfxVol = SoundManager.Instance != null ? SoundManager.Instance.SFXVolume : 0.85f;
        float targetVolume = distAttenuation * waveSurgeEnvelope * sfxVol * 0.62f;

        shoreWaveAudioSource.volume = Mathf.MoveTowards(shoreWaveAudioSource.volume, targetVolume, Time.deltaTime * 2.0f);
        shoreWaveAudioSource.pitch = Mathf.Lerp(0.95f, 1.04f, waveCycle);

        // 5. Định hướng tai nghe Trái / Phải (Stereo Panning) dựa theo vị trí bờ nước so với nhân vật
        float dx = nearestShorePt.x - listenerPos.x;
        float targetPan = Mathf.Clamp(dx / 5.5f, -0.65f, 0.65f);
        shoreWaveAudioSource.panStereo = Mathf.MoveTowards(shoreWaveAudioSource.panStereo, targetPan, Time.deltaTime * 2.5f);

        if (!shoreWaveAudioSource.isPlaying)
        {
            shoreWaveAudioSource.Play();
        }
    }

    /// <summary>
    /// Tổng hợp âm thanh Sóng Vỗ Dạt Bờ Đá đa tầng (Deep Surf Wash + Foam Crest + Lapping Water Trickle)
    /// tạo vòng lặp 8.0 giây liền mạch (Seamless Loop) không có điểm ngắt.
    /// </summary>
    private static AudioClip BuildProceduralShorelineWaveClip()
    {
        const int sampleRate = 22050;
        const float duration = 8.0f; // 3 chu kỳ sóng vỗ + tiếng nước róc rách liền mạch
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        uint rngState = 0x7A9E3B15u;
        float lowPass1 = 0f;
        float lowPass2 = 0f;
        float deepSurfLP = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float normT = t / duration; // 0..1

            // Chu kỳ sóng chính (3 đợt sóng dạt bờ trong 8 giây để khớp hoàn hảo ở điểm đầu-cuối vòng lặp)
            float wave1 = (Mathf.Sin(normT * Mathf.PI * 2f * 3f - 0.6f) + 1f) * 0.5f;
            float wave2 = (Mathf.Sin(normT * Mathf.PI * 2f * 2f + 1.1f) + 1f) * 0.5f;
            float crestEnv = Mathf.Pow(wave1, 1.8f) * 0.72f + Mathf.Pow(wave2, 2.2f) * 0.28f;

            // Nhịp nước rút róc rách (trễ pha 90 độ sau khi sóng đập vào bờ)
            float recedeWave = (Mathf.Sin(normT * Mathf.PI * 2f * 3f - 2.1f) + 1f) * 0.5f;
            float recedeEnv = Mathf.Pow(recedeWave, 2.4f);

            // Tạo White Noise chuẩn xác bằng Xorshift32
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;
            float white = ((rngState & 0xFFFFu) / 32767.5f) - 1f;

            // Tầng 1: Sóng ngầm trầm ấm (Deep Rolling Surf 90Hz - 320Hz)
            deepSurfLP += 0.045f * (white - deepSurfLP);
            float deepLayer = deepSurfLP * (0.28f + 0.55f * crestEnv);

            // Tầng 2: Bọt sóng trắng vỗ vào bờ đá (Dynamic Cutoff Low-Pass Filter mở rộng khi sóng vỗ lên đỉnh)
            float cutoffAlpha = Mathf.Lerp(0.06f, 0.28f, crestEnv);
            lowPass1 += cutoffAlpha * (white - lowPass1);
            lowPass2 += cutoffAlpha * (lowPass1 - lowPass2);
            float bandFoam = (lowPass1 - lowPass2 * 0.65f) * (0.14f + 0.86f * crestEnv);

            // Tầng 3: Tiếng nước vỗ róc rách / bọt nước tan vào kẽ đá khi sóng rút (Water Lapping Resonant Trickle)
            float dropletSlot = (t * 19.0f);
            int slotIdx = Mathf.FloorToInt(dropletSlot);
            float slotFrac = dropletSlot - slotIdx;
            uint slotHash = (uint)(slotIdx * 747796405u + 2891336453u);
            slotHash ^= slotHash >> 16;
            float dropletAmp = ((slotHash & 0xFFu) / 255f);
            float trickleSample = 0f;
            if (dropletAmp > 0.52f)
            {
                float baseFreq = 520f + ((slotHash >> 8) & 0x3FFu) * 0.85f;
                float instFreq = baseFreq * (1f + slotFrac * 0.65f);
                float env = Mathf.Sin(slotFrac * Mathf.PI) * Mathf.Exp(-slotFrac * 4.2f);
                trickleSample = Mathf.Sin(2f * Mathf.PI * instFreq * (slotFrac / 19.0f)) * env * 0.075f * (0.35f + 0.65f * recedeEnv);
            }

            float combined = (deepLayer * 0.55f + bandFoam * 0.65f + trickleSample) * 0.82f;
            samples[i] = Mathf.Clamp(combined, -0.95f, 0.95f);
        }

        // Làm mượt biên nối đầu-cuối (Crossfade 0.15s) để lặp 100% không có tiếng click
        int fadeSamples = Mathf.RoundToInt(sampleRate * 0.15f);
        for (int i = 0; i < fadeSamples; i++)
        {
            float a = (float)i / fadeSamples;
            int tailIdx = sampleCount - fadeSamples + i;
            float blended = Mathf.Lerp(samples[tailIdx], samples[i], a);
            samples[i] = blended;
            samples[tailIdx] = Mathf.Lerp(samples[tailIdx], samples[0], a * 0.5f);
        }

        AudioClip clip = AudioClip.Create("ShorelineLappingWavesLoop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
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
