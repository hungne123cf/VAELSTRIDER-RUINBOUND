using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Minimap Radar (Bản đồ thu nhỏ) & La Bàn Dẫn Đường (Objective Compass).
/// Thiết kế chuẩn Dark Fantasy Action RPG: Radar hình tròn chống răng cưa, các điểm chấm (Blip) bo tròn phát sáng,
/// hoàn toàn không bị đè lên các nút góc trên màn hình.
/// </summary>
public class MinimapHUD : MonoBehaviour
{
    public static MinimapHUD Instance { get; private set; }

    [Header("Minimap Settings")]
    [SerializeField] private float radarRadius = 35f; // Bán kính quét thực tế trong game (35m)
    [SerializeField] private float mapUiRadius = 48f;  // Bán kính hiển thị trên Minimap UI (pixel)

    [Header("UI References")]
    private RectTransform radarPanelRt;
    private Image playerIconImg;
    private RectTransform playerIconRt;
    private RectTransform compassNeedleRt;
    private Image compassNeedleImg;
    private TextMeshProUGUI compassDistanceText;
    private RectTransform blipContainer;

    [Header("Target Tracking")]
    private Transform playerTransform;

    // Procedural Sprites
    private static Sprite circleDiscSprite;
    private static Sprite roundDotSprite;
    private static Sprite arrowSprite;
    private static Sprite diamondSprite;

    // Blip pool for performance
    private List<Image> activeBlips = new List<Image>();
    private Queue<Image> blipPool = new Queue<Image>();

    private float updateTimer = 0f;

    void Awake()
    {
        Instance = this;
        EnsureProceduralSprites();
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Cập nhật hướng xoay của icon Player trên Minimap
        float playerFacing = playerTransform.localScale.x > 0 ? 0f : 180f;
        if (playerIconRt != null)
        {
            playerIconRt.localRotation = Quaternion.Euler(0f, 0f, playerFacing == 0f ? -90f : 90f);
        }

        // Cập nhật La Bàn Dẫn Đường (Compass Needle)
        UpdateObjectiveCompass();

        // Cập nhật các chấm Blip trên Radar định kỳ mỗi 0.04s (25 FPS) để siêu mượt & nhẹ máy
        updateTimer += Time.deltaTime;
        if (updateTimer >= 0.04f)
        {
            updateTimer = 0f;
            UpdateRadarBlips();
        }
    }

    private void FindPlayer()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    private void UpdateObjectiveCompass()
    {
        Transform priorityTarget = null;
        string targetLabel = "";

        // Ưu tiên 1: Boss đang còn sống
        var bossKing = Object.FindAnyObjectByType<SkeletonKingBossAI>();
        if (bossKing != null && bossKing.gameObject.activeInHierarchy)
        {
            priorityTarget = bossKing.transform;
            targetLabel = LocalizationManager.T("CHÚA TỂ NHỆN", "SPIDER LORD");
        }
        if (priorityTarget == null)
        {
            var bossGolem = Object.FindAnyObjectByType<ValleyGolemBossAI>();
            if (bossGolem != null && bossGolem.gameObject.activeInHierarchy)
            {
                priorityTarget = bossGolem.transform;
                targetLabel = LocalizationManager.T("CỰ THẠCH HỘ VỆ", "VALLEY GOLEM");
            }
        }
        if (priorityTarget == null)
        {
            var bossBlood = Object.FindAnyObjectByType<BloodMonsterAI>();
            if (bossBlood != null && bossBlood.gameObject.activeInHierarchy && bossBlood.isBoss)
            {
                priorityTarget = bossBlood.transform;
                targetLabel = LocalizationManager.T("HUYẾT QUÁI CỔ", "BLOOD FIEND");
            }
        }

        // Ưu tiên 2: Cổng Dịch Chuyển (Portal)
        if (priorityTarget == null)
        {
            var portal = Object.FindAnyObjectByType<RuinPortal>();
            if (portal != null && portal.gameObject.activeInHierarchy)
            {
                priorityTarget = portal.transform;
                targetLabel = LocalizationManager.T("CỔNG TÀN TÍCH", "RUIN PORTAL");
            }
        }

        // Ưu tiên 3: Trụ Nâng Cấp (Ancient Shrine)
        if (priorityTarget == null)
        {
            var shrine = Object.FindAnyObjectByType<AncientShrine>();
            if (shrine != null && shrine.gameObject.activeInHierarchy)
            {
                priorityTarget = shrine.transform;
                targetLabel = LocalizationManager.T("ĐÀI THẦN BÍ", "SHRINE");
            }
        }

        // Ưu tiên 4: Kẻ thù gần nhất
        if (priorityTarget == null)
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float nearestDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || !e.activeInHierarchy) continue;
                float d = Vector2.Distance(playerTransform.position, e.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    priorityTarget = e.transform;
                    targetLabel = LocalizationManager.T("KẺ THÙ", "ENEMY");
                }
            }
        }

        // Cập nhật hướng mũi kim La Bàn
        if (priorityTarget != null)
        {
            Vector2 dir = (priorityTarget.position - playerTransform.position);
            float dist = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (compassNeedleRt != null)
            {
                compassNeedleRt.gameObject.SetActive(true);
                compassNeedleRt.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                compassNeedleRt.anchoredPosition = dir.normalized * (mapUiRadius + 2f);
            }

            if (compassDistanceText != null)
            {
                compassDistanceText.gameObject.SetActive(true);
                compassDistanceText.text = $"<color=#FFD700>{targetLabel}</color> <b>{(int)dist}m</b>";
            }
        }
        else
        {
            if (compassNeedleRt != null) compassNeedleRt.gameObject.SetActive(false);
            if (compassDistanceText != null)
            {
                compassDistanceText.text = $"<color=#2ED573>{LocalizationManager.T("AN TOÀN", "SECURE")}</color>";
            }
        }
    }

    private void UpdateRadarBlips()
    {
        for (int i = 0; i < activeBlips.Count; i++)
        {
            activeBlips[i].gameObject.SetActive(false);
            blipPool.Enqueue(activeBlips[i]);
        }
        activeBlips.Clear();

        Vector3 playerPos = playerTransform.position;

        // 1. Quét Kẻ Thù (Red Circle Blips)
        var enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemyObjects)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;
            Vector2 offset = (enemy.transform.position - playerPos);
            float dist = offset.magnitude;
            if (dist > radarRadius) continue;

            bool isBoss = enemy.name.Contains("Boss") || enemy.name.Contains("King") || enemy.name.Contains("Golem") || enemy.name.Contains("Cocoon");
            Color blipColor = isBoss ? new Color(1f, 0.15f, 0.25f, 1f) : new Color(1f, 0.35f, 0.35f, 0.9f);
            Vector2 blipSize = isBoss ? new Vector2(14f, 14f) : new Vector2(9f, 9f);
            Sprite blipSprite = isBoss ? diamondSprite : roundDotSprite;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, blipColor, blipSize, blipSprite);
        }

        // 2. Quét Cổng Dịch Chuyển (Cyan Diamond Blip)
        var portals = Object.FindObjectsByType<RuinPortal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in portals)
        {
            if (p == null) continue;
            Vector2 offset = (p.transform.position - playerPos);
            if (offset.magnitude > radarRadius) continue;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, new Color(0.1f, 0.9f, 1f, 1f), new Vector2(12f, 12f), diamondSprite);
        }

        // 3. Quét Trụ Nâng Cấp (Golden Star/Diamond Blip)
        var shrines = Object.FindObjectsByType<AncientShrine>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in shrines)
        {
            if (s == null) continue;
            Vector2 offset = (s.transform.position - playerPos);
            if (offset.magnitude > radarRadius) continue;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, new Color(1f, 0.85f, 0.2f, 1f), new Vector2(12f, 12f), diamondSprite);
        }

        // 4. Quét Hòm Báu (Yellow Rounded Dot Blip)
        var chests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var ch in chests)
        {
            if (ch == null) continue;
            Vector2 offset = (ch.transform.position - playerPos);
            if (offset.magnitude > radarRadius) continue;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, new Color(1f, 0.7f, 0.1f, 1f), new Vector2(10f, 10f), roundDotSprite);
        }
    }

    private void SpawnBlip(Vector2 localPos, Color color, Vector2 size, Sprite spr)
    {
        // Giới hạn các blip nằm trong hình tròn radar
        if (localPos.magnitude > mapUiRadius)
        {
            localPos = localPos.normalized * mapUiRadius;
        }

        Image blipImg;
        if (blipPool.Count > 0)
        {
            blipImg = blipPool.Dequeue();
            blipImg.gameObject.SetActive(true);
        }
        else
        {
            GameObject bObj = new GameObject("Blip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bObj.layer = 5;
            bObj.transform.SetParent(blipContainer, false);
            blipImg = bObj.GetComponent<Image>();
        }

        RectTransform rt = blipImg.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;
        rt.sizeDelta = size;
        blipImg.sprite = spr;
        blipImg.color = color;

        activeBlips.Add(blipImg);
    }

    private static void EnsureProceduralSprites()
    {
        if (circleDiscSprite != null) return;

        // 1. Radar Disc (64x64) - Dark translucent circle with smooth anti-aliased border
        Texture2D texDisc = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(31.5f, 31.5f);
        float rMax = 31f;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > rMax)
                {
                    texDisc.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float alpha = Mathf.Clamp01((rMax - dist) * 2.5f);
                    // Glowing border ring on edge
                    if (dist > rMax - 2.5f)
                    {
                        texDisc.SetPixel(x, y, new Color(0.35f, 0.8f, 1f, alpha * 0.95f));
                    }
                    else if (Mathf.Abs(dist - 16f) < 0.8f) // Inner concentric grid ring
                    {
                        texDisc.SetPixel(x, y, new Color(0.2f, 0.45f, 0.75f, alpha * 0.25f));
                    }
                    else
                    {
                        texDisc.SetPixel(x, y, new Color(0.04f, 0.07f, 0.12f, alpha * 0.90f));
                    }
                }
            }
        }
        texDisc.Apply();
        circleDiscSprite = Sprite.Create(texDisc, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        // 2. Round Dot Sprite (32x32)
        Texture2D texDot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Vector2 cDot = new Vector2(15.5f, 15.5f);
        float rDot = 14f;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), cDot);
                if (d > rDot)
                {
                    texDot.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float a = Mathf.Clamp01((rDot - d) * 2.5f);
                    texDot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
        }
        texDot.Apply();
        roundDotSprite = Sprite.Create(texDot, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 3. Arrow Sprite (32x32)
        Texture2D texArrow = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Triangle pointing UP
                float nx = (x - 15.5f) / 15.5f;
                float ny = y / 31f;
                if (ny >= Mathf.Abs(nx) * 0.85f && y >= 4)
                {
                    texArrow.SetPixel(x, y, Color.white);
                }
                else
                {
                    texArrow.SetPixel(x, y, Color.clear);
                }
            }
        }
        texArrow.Apply();
        arrowSprite = Sprite.Create(texArrow, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 4. Diamond Sprite (32x32)
        Texture2D texDia = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Abs(x - 15.5f) / 14f;
                float dy = Mathf.Abs(y - 15.5f) / 14f;
                if (dx + dy <= 1f)
                {
                    texDia.SetPixel(x, y, Color.white);
                }
                else
                {
                    texDia.SetPixel(x, y, Color.clear);
                }
            }
        }
        texDia.Apply();
        diamondSprite = Sprite.Create(texDia, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    public static MinimapHUD EnsureUIExists()
    {
        EnsureProceduralSprites();

        if (Instance != null)
        {
            Instance.gameObject.SetActive(true);
            return Instance;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("Minimap_Compass_HUD");
        if (existing != null)
        {
            Instance = existing.GetComponent<MinimapHUD>();
            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                return Instance;
            }
        }

        // 1. Root Container (Nằm ngay dưới hàng 3 nút [BAG] [INFO] [SET] tại góc trên bên phải)
        GameObject rootObj = new GameObject("Minimap_Compass_HUD", typeof(RectTransform), typeof(CanvasRenderer));
        rootObj.layer = 5;
        rootObj.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(1f, 1f);
        rootRt.anchoredPosition = new Vector2(-20f, -80f); // Không bao giờ đè lên 3 nút trên cùng!
        rootRt.sizeDelta = new Vector2(125f, 155f);

        // 2. Radar Background Disc (115x115 px tròn chống răng cưa)
        GameObject radarBg = new GameObject("RadarDisc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        radarBg.layer = 5;
        radarBg.transform.SetParent(rootObj.transform, false);

        RectTransform rRt = radarBg.GetComponent<RectTransform>();
        rRt.anchoredPosition = new Vector2(0f, -8f);
        rRt.sizeDelta = new Vector2(115f, 115f);

        Image rImg = radarBg.GetComponent<Image>();
        rImg.sprite = circleDiscSprite;
        rImg.color = Color.white;

        // Container các chấm Blip
        GameObject bContainer = new GameObject("BlipContainer", typeof(RectTransform));
        bContainer.layer = 5;
        bContainer.transform.SetParent(radarBg.transform, false);
        RectTransform bRt = bContainer.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.sizeDelta = Vector2.zero;

        // Player Center Icon (Glowing Green Arrow)
        GameObject pIconObj = new GameObject("PlayerIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pIconObj.layer = 5;
        pIconObj.transform.SetParent(radarBg.transform, false);
        RectTransform pRt = pIconObj.GetComponent<RectTransform>();
        pRt.sizeDelta = new Vector2(16f, 16f);
        pRt.anchoredPosition = Vector2.zero;

        var pImg = pIconObj.GetComponent<Image>();
        pImg.sprite = arrowSprite;
        pImg.color = new Color(0.18f, 1f, 0.45f);

        // La Bàn: Needle Pointer chạy quanh viền hình tròn
        GameObject needleObj = new GameObject("CompassNeedle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        needleObj.layer = 5;
        needleObj.transform.SetParent(radarBg.transform, false);
        RectTransform nRt = needleObj.GetComponent<RectTransform>();
        nRt.sizeDelta = new Vector2(18f, 18f);
        nRt.anchoredPosition = Vector2.zero;

        var nImg = needleObj.GetComponent<Image>();
        nImg.sprite = arrowSprite;
        nImg.color = new Color(1f, 0.85f, 0.25f);

        // Label Khoảng Cách Mục Tiêu Dưới Radar
        GameObject distObj = new GameObject("DistanceBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        distObj.layer = 5;
        distObj.transform.SetParent(rootObj.transform, false);
        RectTransform dRt = distObj.GetComponent<RectTransform>();
        dRt.anchoredPosition = new Vector2(0f, -72f);
        dRt.sizeDelta = new Vector2(125f, 22f);

        Image dImg = distObj.GetComponent<Image>();
        dImg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);

        var dOutline = distObj.AddComponent<Outline>();
        dOutline.effectColor = new Color(0.35f, 0.65f, 0.95f, 0.6f);
        dOutline.effectDistance = new Vector2(1f, -1f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(distObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var dTmp = txtObj.AddComponent<TextMeshProUGUI>();
        dTmp.text = "<b>--</b>";
        dTmp.fontSize = 10.5f;
        dTmp.color = Color.white;
        dTmp.alignment = TextAlignmentOptions.Center;

        // Khởi tạo Component
        var comp = rootObj.AddComponent<MinimapHUD>();
        comp.radarPanelRt = rRt;
        comp.playerIconRt = pRt;
        comp.playerIconImg = pImg;
        comp.compassNeedleRt = nRt;
        comp.compassNeedleImg = nImg;
        comp.compassDistanceText = dTmp;
        comp.blipContainer = bRt;

        Instance = comp;
        return comp;
    }
}
