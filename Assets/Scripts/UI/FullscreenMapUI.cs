using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Bản Đồ Toàn Màn Hình Hiện Đại (Modern Fullscreen Map & Tactical Overview).
/// Thiết kế chuẩn Action RPG Dark Fantasy (Hades, Diablo, Dead Cells):
/// - Sơ đồ phòng & Sương mù khám phá (Fog of War)
/// - Radar thời gian thực: Quái thường & Boss (Chấm Đỏ), Player (Mũi tên Xanh Ngọc), Đài Thần Bí (Vàng Kim), Rương (Cam Vàng), Cổng (Lam Ngọc)
/// - Hỗ trợ Kéo lia bản đồ (Drag/Pan), Thu phóng (Zoom), Căn giữa (Focus Player)
/// - Bảng Thống Kê Tiến Độ Khám Phá & Gợi Ý Nâng Cấp Chiến Thuật
/// </summary>
public class FullscreenMapUI : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    public static FullscreenMapUI Instance { get; private set; }

    [Header("State")]
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    [Header("UI Hierarchy")]
    private CanvasGroup canvasGroup;
    private RectTransform rootRt;
    private RectTransform mapViewportRt;
    private RectTransform mapContentRt;
    private RectTransform roomsContainerRt;
    private RectTransform markersContainerRt;

    // Panels
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI hintText;
    private Button btnOpenShrine;
    private TextMeshProUGUI zoomLevelText;

    // Player Marker
    private RectTransform playerMarkerRt;
    private Image playerMarkerImg;

    // Zoom & Pan
    private float currentZoom = 1.0f;
    private const float MIN_ZOOM = 0.6f;
    private const float MAX_ZOOM = 2.4f;
    private const float MAP_SCALE = 12f; // 1 mét trong game = 12 pixel trên Map Content

    // Entity Markers Pool
    private List<GameObject> activeMarkers = new List<GameObject>();
    private Queue<GameObject> markerPool = new Queue<GameObject>();

    // Room Visuals Cache
    private List<GameObject> roomVisuals = new List<GameObject>();

    // Tracking
    private Transform playerTransform;
    private float updateTimer = 0f;

    // Procedural Sprites
    private static Sprite circleFillSprite;
    private static Sprite diamondFillSprite;
    private static Sprite arrowFillSprite;
    private static Sprite skullSprite;
    private static Sprite chestSprite;
    private static Sprite shrineSprite;
    private static Sprite portalSprite;
    private static Sprite squareBorderSprite;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureProceduralSprites();
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        // Phím tắt M, Tab hoặc ESC để đóng/mở bản đồ
        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMap();
        }
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMap();
        }

        if (!isOpen) return;

        // Xử lý cuộn chuột để Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            AdjustZoom(scroll * 1.5f);
        }

        // Cập nhật vị trí các chấm thực thể thời gian thực
        updateTimer += Time.unscaledDeltaTime;
        if (updateTimer >= 0.04f)
        {
            updateTimer = 0f;
            UpdateRealtimeEntities();
            UpdateStatsAndHints();
        }
    }

    private void FindPlayer()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    public void ToggleMap()
    {
        if (isOpen) CloseMap();
        else OpenMap();
    }

    public void OpenMap()
    {
        isOpen = true;
        FindPlayer();
        EnsureUIConstructed();

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Tạm dừng/Làm chậm nhẹ để người chơi quan sát an toàn
        Time.timeScale = 0.05f;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (titleText != null)
        {
            if (sceneName.Contains("Map2") || sceneName.Contains("Valley"))
                titleText.text = $"<b>{LocalizationManager.T("TÀN TÍCH THUNG LŨNG", "VALLEY OF THE ANCIENTS")}</b>";
            else if (sceneName.Contains("Map3") || sceneName.Contains("Forgotten"))
                titleText.text = $"<b>{LocalizationManager.T("KÝ ỨC BỊ LÃNG QUÊN", "FORGOTTEN MEMORIES")}</b>";
            else
                titleText.text = $"<b>{LocalizationManager.T("PHẾ TÍCH NGẬP NƯỚC", "SUNKEN SEPULCHER")}</b>";
        }
        if (subtitleText != null)
        {
            subtitleText.text = LocalizationManager.T("BẢN ĐỒ TÁC CHIẾN TOÀN CẢNH | TÀN TÍCH CỔ ĐẠI", "TACTICAL OVERVIEW MAP | ANCIENT RUINS");
        }

        // Xây dựng lại layout phòng
        BuildRoomLayout();

        // Căn giữa vào vị trí nhân vật
        FocusOnPlayer();

        // Cập nhật thực thể ngay lập tức
        UpdateRealtimeEntities();
        UpdateStatsAndHints();

        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void CloseMap()
    {
        isOpen = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);

        // Khôi phục tốc độ game
        Time.timeScale = 1.0f;

        SoundManager.EnsureInstance()?.PlayButtonClick();
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (mapContentRt == null) return;
        mapContentRt.anchoredPosition += eventData.delta;
    }

    public void AdjustZoom(float delta)
    {
        currentZoom = Mathf.Clamp(currentZoom + delta, MIN_ZOOM, MAX_ZOOM);
        if (mapContentRt != null)
        {
            mapContentRt.localScale = Vector3.one * currentZoom;
        }
        if (zoomLevelText != null)
        {
            zoomLevelText.text = $"{(int)(currentZoom * 100)}%";
        }
    }

    public void FocusOnPlayer()
    {
        if (playerTransform == null) FindPlayer();
        if (mapContentRt == null) return;

        if (playerTransform != null)
        {
            Vector2 pPos = new Vector2(playerTransform.position.x, playerTransform.position.y);
            mapContentRt.anchoredPosition = -pPos * MAP_SCALE * currentZoom;
        }
        else
        {
            mapContentRt.anchoredPosition = Vector2.zero;
        }
    }

    private void BuildRoomLayout()
    {
        foreach (var r in roomVisuals)
        {
            if (r != null) Destroy(r);
        }
        roomVisuals.Clear();

        if (roomsContainerRt == null) return;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 1. Kiểm tra nếu có DungeonRoomManager (Map 1)
        if (DungeonRoomManager.Instance != null && DungeonRoomManager.Instance.rooms.Count > 0)
        {
            var rooms = DungeonRoomManager.Instance.rooms;
            var currentRoom = DungeonRoomManager.Instance.currentRoom;

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                if (r == null) continue;

                GameObject rObj = new GameObject($"RoomVisual_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rObj.layer = 5;
                rObj.transform.SetParent(roomsContainerRt, false);

                RectTransform rRt = rObj.GetComponent<RectTransform>();
                rRt.anchoredPosition = new Vector2(r.roomCenter.x, r.roomCenter.y) * MAP_SCALE;
                Vector2 roomDimensions = r.roomBounds.size.sqrMagnitude > 1f ? r.roomBounds.size : new Vector2(18f, 14f);
                rRt.sizeDelta = roomDimensions * MAP_SCALE;

                Image rImg = rObj.GetComponent<Image>();
                rImg.sprite = squareBorderSprite;
                rImg.type = Image.Type.Sliced;

                bool isCurrent = (r == currentRoom);
                bool isCleared = r.isCleared;

                if (isCurrent)
                {
                    rImg.color = new Color(0.15f, 0.65f, 1f, 0.45f); // Phòng hiện tại: Lam ngọc sáng
                }
                else if (isCleared)
                {
                    rImg.color = new Color(0.12f, 0.5f, 0.25f, 0.35f); // Đã dọn sạch: Xanh lá mờ
                }
                else
                {
                    rImg.color = new Color(0.18f, 0.12f, 0.14f, 0.40f); // Chưa vào / Chưa dọn: Tối
                }

                // Nhãn tên phòng
                GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
                labelObj.layer = 5;
                labelObj.transform.SetParent(rObj.transform, false);
                RectTransform lblRt = labelObj.GetComponent<RectTransform>();
                lblRt.anchorMin = new Vector2(0.5f, 1f);
                lblRt.anchorMax = new Vector2(0.5f, 1f);
                lblRt.pivot = new Vector2(0.5f, 1f);
                lblRt.anchoredPosition = new Vector2(0f, -8f);
                lblRt.sizeDelta = new Vector2(180f, 30f);

                var tmp = labelObj.AddComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{r.GetLocalizedTitle()}</b>";
                tmp.fontSize = 11f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = isCurrent ? new Color(1f, 0.9f, 0.3f) : new Color(0.85f, 0.85f, 0.85f, 0.8f);

                roomVisuals.Add(rObj);
            }
        }
        else
        {
            // 2. Với Map 2 (Thung Lũng) hoặc Map 3 (Ký Ức) - Tạo khung đại sảnh
            GameObject arenaObj = new GameObject("ArenaVisual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            arenaObj.layer = 5;
            arenaObj.transform.SetParent(roomsContainerRt, false);
            RectTransform aRt = arenaObj.GetComponent<RectTransform>();
            aRt.anchoredPosition = Vector2.zero;
            aRt.sizeDelta = new Vector2(60f, 45f) * MAP_SCALE;

            Image aImg = arenaObj.GetComponent<Image>();
            aImg.sprite = squareBorderSprite;
            aImg.type = Image.Type.Sliced;
            aImg.color = new Color(0.15f, 0.3f, 0.45f, 0.25f);

            roomVisuals.Add(arenaObj);
        }
    }

    private void UpdateRealtimeEntities()
    {
        // Thu hồi toàn bộ Marker cũ về Pool
        for (int i = 0; i < activeMarkers.Count; i++)
        {
            activeMarkers[i].SetActive(false);
            markerPool.Enqueue(activeMarkers[i]);
        }
        activeMarkers.Clear();

        if (playerTransform == null) FindPlayer();

        // 1. Cập nhật vị trí và hướng nhìn Player
        if (playerTransform != null && playerMarkerRt != null)
        {
            playerMarkerRt.gameObject.SetActive(true);
            playerMarkerRt.anchoredPosition = new Vector2(playerTransform.position.x, playerTransform.position.y) * MAP_SCALE;
            float facing = playerTransform.localScale.x > 0 ? 0f : 180f;
            playerMarkerRt.localRotation = Quaternion.Euler(0f, 0f, facing == 0f ? -90f : 90f);
        }

        // 2. Quét Kẻ Thù Thường & Boss (Chấm ĐỎ và Đầu Lâu Đỏ)
        var enemies = MinimapHUD.GetAllEnemies();
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;

            bool isBoss = enemy.name.Contains("Boss") || enemy.name.Contains("King") || enemy.name.Contains("Golem") || enemy.name.Contains("Cocoon") || enemy.name.Contains("Titan");
            Vector2 worldPos = new Vector2(enemy.transform.position.x, enemy.transform.position.y);

            if (isBoss)
            {
                // Boss: Chấm Đỏ Lớn Rực Lửa (24x24)
                SpawnEntityMarker(worldPos, new Color(1f, 0.15f, 0.25f, 1f), new Vector2(24f, 24f), skullSprite, "BOSS");
            }
            else
            {
                // Quái thường: Chấm Đỏ Tròn (12x12)
                SpawnEntityMarker(worldPos, new Color(1f, 0.30f, 0.30f, 0.95f), new Vector2(12f, 12f), circleFillSprite, "");
            }
        }

        // 2b. Đánh dấu vị trí quái dự kiến trong các phòng chưa dọn sạch (Dungeon Map 1)
        if (DungeonRoomManager.Instance != null && DungeonRoomManager.Instance.rooms != null)
        {
            foreach (var r in DungeonRoomManager.Instance.rooms)
            {
                if (r == null || r.isCleared || r.enemySpawns == null) continue;
                if (r != DungeonRoomManager.Instance.currentRoom)
                {
                    bool isBossRoom = (r.roomType == DungeonRoom.RoomType.TitanSanctum);
                    for (int s = 0; s < r.enemySpawns.Count; s++)
                    {
                        Vector2 spPos = new Vector2(r.enemySpawns[s].x, r.enemySpawns[s].y);
                        if (isBossRoom)
                        {
                            SpawnEntityMarker(spPos, new Color(1f, 0.2f, 0.2f, 0.85f), new Vector2(22f, 22f), skullSprite, "BOSS");
                        }
                        else
                        {
                            SpawnEntityMarker(spPos, new Color(1f, 0.35f, 0.35f, 0.75f), new Vector2(10f, 10f), circleFillSprite, "");
                        }
                    }
                }
            }
        }

        // 3. Quét Bàn Thờ Cổ (Ancient Shrine - Vàng Kim)
        var shrines = Object.FindObjectsByType<AncientShrine>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in shrines)
        {
            if (s == null) continue;
            Vector2 pos = new Vector2(s.transform.position.x, s.transform.position.y);
            SpawnEntityMarker(pos, new Color(1f, 0.85f, 0.15f, 1f), new Vector2(20f, 20f), shrineSprite, LocalizationManager.T("ĐÀI THẦN BÍ", "SHRINE"));
        }

        // 4. Quét Rương Kho Báu (Treasure Chest - Cam Vàng)
        var chests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var c in chests)
        {
            if (c == null) continue;
            Vector2 pos = new Vector2(c.transform.position.x, c.transform.position.y);
            SpawnEntityMarker(pos, new Color(1f, 0.65f, 0.1f, 1f), new Vector2(18f, 18f), chestSprite, LocalizationManager.T("RƯƠNG", "CHEST"));
        }

        // 5. Quét Cổng Dịch Chuyển (Ruin Portal - Lam Ngọc)
        var portals = Object.FindObjectsByType<RuinPortal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in portals)
        {
            if (p == null) continue;
            Vector2 pos = new Vector2(p.transform.position.x, p.transform.position.y);
            SpawnEntityMarker(pos, new Color(0.15f, 0.9f, 1f, 1f), new Vector2(22f, 22f), portalSprite, LocalizationManager.T("CỔNG THOÁT", "PORTAL"));
        }

        // 6. Quét Bình Máu Rơi (Health Potions - Xanh Lá)
        var potions = Object.FindObjectsByType<HealthPotion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var hp in potions)
        {
            if (hp == null) continue;
            Vector2 pos = new Vector2(hp.transform.position.x, hp.transform.position.y);
            SpawnEntityMarker(pos, new Color(0.2f, 1f, 0.4f, 0.9f), new Vector2(10f, 10f), circleFillSprite, "");
        }
    }

    private void SpawnEntityMarker(Vector2 worldPos, Color color, Vector2 size, Sprite spr, string label)
    {
        if (markersContainerRt == null) return;

        GameObject mObj;
        if (markerPool.Count > 0)
        {
            mObj = markerPool.Dequeue();
            mObj.SetActive(true);
        }
        else
        {
            mObj = new GameObject("EntityMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            mObj.layer = 5;
            mObj.transform.SetParent(markersContainerRt, false);

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            txtObj.layer = 5;
            txtObj.transform.SetParent(mObj.transform, false);
            RectTransform tRt = txtObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0f);
            tRt.anchorMax = new Vector2(0.5f, 0f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -2f);
            tRt.sizeDelta = new Vector2(120f, 16f);

            var tTmp = txtObj.AddComponent<TextMeshProUGUI>();
            tTmp.fontSize = 9.5f;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.fontStyle = FontStyles.Bold;
        }

        RectTransform rt = mObj.GetComponent<RectTransform>();
        rt.anchoredPosition = worldPos * MAP_SCALE;
        rt.sizeDelta = size;

        Image img = mObj.GetComponent<Image>();
        img.sprite = spr;
        img.color = color;

        var lbl = mObj.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null)
        {
            lbl.text = label;
            lbl.color = color;
            lbl.gameObject.SetActive(!string.IsNullOrEmpty(label));
        }

        activeMarkers.Add(mObj);
    }

    private void UpdateStatsAndHints()
    {
        // 1. Cập nhật Tiêu đề bản đồ theo Scene
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (titleText != null)
        {
            if (sceneName.Contains("Map2"))
            {
                titleText.text = LocalizationManager.T("THUNG LŨNG TÀN TÍCH", "VALLEY RUIN");
                subtitleText.text = LocalizationManager.T("VÙNG NÚI CỔ ĐẠI | SƯƠNG MÙ CHIẾN TRANH", "ANCIENT VALE | FOG OF WAR");
            }
            else if (sceneName.Contains("Map3"))
            {
                titleText.text = LocalizationManager.T("KÝ ỨC LÃNG QUÊN", "FORGOTTEN MEMORIES");
                subtitleText.text = LocalizationManager.T("RỪNG PHẾ TÍCH | ĐẠI CHIẾN CHÚA TỂ NHỆN", "RUINED WOODS | SPIDER LORD'S LAIR");
            }
            else
            {
                titleText.text = LocalizationManager.T("PHẾ TÍCH NGẬP NƯỚC", "SUNKEN SEPULCHER");
                subtitleText.text = LocalizationManager.T("TẦNG 1 - TÀN TÍCH CỔ | KHÁM PHÁ TOÀN CẢNH", "FLOOR 1 - ANCIENT RUINS | DUNGEON OVERVIEW");
            }
        }

        // 2. Cập nhật Thống kê Tiến độ
        var aliveEnemies = MinimapHUD.GetAllEnemies();
        int enemyCount = aliveEnemies.Count;

        int shards = PlayerPrefs.GetInt("AethelShards", 0);
        int clearedRooms = 0;
        int totalRooms = 1;
        if (DungeonRoomManager.Instance != null && DungeonRoomManager.Instance.rooms.Count > 0)
        {
            totalRooms = DungeonRoomManager.Instance.rooms.Count;
            foreach (var r in DungeonRoomManager.Instance.rooms)
            {
                if (r != null && r.isCleared) clearedRooms++;
            }
        }

        if (statsText != null)
        {
            statsText.text = $"{LocalizationManager.T("🏰 PHÒNG ĐÃ DỌN", "🏰 CLEARED")}: <color=#2ED573><b>{clearedRooms}/{totalRooms}</b></color>\n" +
                             $"{LocalizationManager.T("⚔️ KẺ THÙ", "⚔️ ENEMIES")}: <color=#FF4757><b>{enemyCount} {LocalizationManager.T("Quái", "Foes")}</b></color>\n" +
                             $"{LocalizationManager.T("💎 LINH HỒN", "💎 SHARDS")}: <color=#00D2D3><b>{shards}</b></color>";
        }

        // 3. Gợi ý Nâng Cấp Thông Minh (Smart Tactical Hints)
        if (hintText != null)
        {
            var pHealth = Object.FindAnyObjectByType<PlayerHealth>();
            float hpPercent = pHealth != null && pHealth.MaxHealth > 0 ? (float)pHealth.CurrentHealth / pHealth.MaxHealth : 1f;

            if (hpPercent < 0.4f)
            {
                hintText.text = $"<color=#FF4757><b>⚠️ {LocalizationManager.T("CẢNH BÁO NGUY CẤP:", "CRITICAL HEALTH:")}</b></color>\n" +
                                $"{LocalizationManager.T("Máu dưới 40%! Tìm Bình Máu (chấm xanh) hoặc ghé Đài Thần Bí để hồi phục.", "Health critical! Gather Potions (green dots) or visit a Shrine.")}";
            }
            else if (shards >= 150)
            {
                hintText.text = $"<color=#FFD700><b>💡 {LocalizationManager.T("GỢI Ý CƯỜNG HÓA:", "UPGRADE AVAILABLE:")}</b></color>\n" +
                                $"{LocalizationManager.T("Đang có dồi dào Linh Hồn! Hãy đến Đài Thần Bí để nâng cấp Sát Thương và Thể Lực.", "You hold abundant Shards! Visit Ancient Shrine to boost ATK & HP.")}";
            }
            else if (enemyCount > 0)
            {
                hintText.text = $"<color=#70A1FF><b>⚔️ {LocalizationManager.T("CHIẾN THUẬT:", "TACTIC:")}</b></color>\n" +
                                $"{LocalizationManager.T("Dùng Lướt (Dash) để né đòn quét và tập trung hạ gục các chấm đỏ.", "Use Dash to dodge incoming swings and focus down red blips.")}";
            }
            else
            {
                hintText.text = $"<color=#2ED573><b>✓ {LocalizationManager.T("KHU VỰC AN TOÀN:", "AREA SECURE:")}</b></color>\n" +
                                $"{LocalizationManager.T("Mọi phòng đã dọn sạch! Hãy nhặt rương báu và tiến vào Cổng Dịch Chuyển.", "Area cleared! Collect remaining chests and enter the Ruin Portal.")}";
            }
        }
    }

    public static FullscreenMapUI EnsureUIExists()
    {
        EnsureProceduralSprites();

        if (Instance != null) return Instance;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("FullscreenMapModal");
        if (existing != null)
        {
            Instance = existing.GetComponent<FullscreenMapUI>();
            if (Instance != null) return Instance;
        }

        GameObject rootObj = new GameObject("FullscreenMapModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
        rootObj.layer = 5;
        rootObj.transform.SetParent(canvas.transform, false);

        var comp = rootObj.AddComponent<FullscreenMapUI>();
        comp.EnsureUIConstructed();

        Instance = comp;
        comp.CloseMap(); // Mặc định đóng khi vừa khởi tạo

        return comp;
    }

    private void EnsureUIConstructed()
    {
        if (mapViewportRt != null) return;

        EnsureProceduralSprites();

        canvasGroup = GetComponent<CanvasGroup>();
        rootRt = GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        // 1. Dark Backdrop Overlay (Mờ sương tối)
        GameObject backdrop = new GameObject("DarkBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.layer = 5;
        backdrop.transform.SetParent(transform, false);
        RectTransform bdRt = backdrop.GetComponent<RectTransform>();
        bdRt.anchorMin = Vector2.zero;
        bdRt.anchorMax = Vector2.one;
        bdRt.sizeDelta = Vector2.zero;
        Image bdImg = backdrop.GetComponent<Image>();
        bdImg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

        // 2. Khung viền chính của Bản Đồ (Map Frame)
        GameObject frameObj = new GameObject("MapWindowFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObj.layer = 5;
        frameObj.transform.SetParent(transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.04f, 0.04f);
        frameRt.anchorMax = new Vector2(0.96f, 0.96f);
        frameRt.sizeDelta = Vector2.zero;
        Image fImg = frameObj.GetComponent<Image>();
        fImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);

        var fOutline = frameObj.AddComponent<Outline>();
        fOutline.effectColor = new Color(0.25f, 0.55f, 0.9f, 0.8f);
        fOutline.effectDistance = new Vector2(2f, -2f);

        // 3. Thanh Header trên cùng (Title & Close Button)
        GameObject headerObj = new GameObject("HeaderBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerObj.layer = 5;
        headerObj.transform.SetParent(frameObj.transform, false);
        RectTransform hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(0f, 50f);
        Image hImg = headerObj.GetComponent<Image>();
        hImg.color = new Color(0.08f, 0.12f, 0.20f, 0.98f);

        // Text Tiêu Đề
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.layer = 5;
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = new Vector2(25f, 4f);
        tRt.offsetMax = new Vector2(-80f, -4f);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "<b>PHẾ TÍCH NGẬP NƯỚC</b>";
        titleText.fontSize = 18f;
        titleText.color = new Color(1f, 0.85f, 0.3f);
        titleText.alignment = TextAlignmentOptions.Left;

        // Text Subtitle
        GameObject subObj = new GameObject("SubText", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(headerObj.transform, false);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0.4f);
        sRt.offsetMin = new Vector2(25f, 2f);
        sRt.offsetMax = new Vector2(-80f, 0f);
        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "TẦNG 1 - TÀN TÍCH CỔ ĐẠI | KHÁM PHÁ TOÀN CẢNH";
        subtitleText.fontSize = 10.5f;
        subtitleText.color = new Color(0.7f, 0.85f, 1f, 0.75f);
        subtitleText.alignment = TextAlignmentOptions.Left;

        // Nút Đóng [X]
        GameObject closeObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.layer = 5;
        closeObj.transform.SetParent(headerObj.transform, false);
        RectTransform cRt = closeObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 0.5f);
        cRt.anchorMax = new Vector2(1f, 0.5f);
        cRt.pivot = new Vector2(1f, 0.5f);
        cRt.anchoredPosition = new Vector2(-12f, 0f);
        cRt.sizeDelta = new Vector2(36f, 36f);
        Image cImg = closeObj.GetComponent<Image>();
        cImg.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
        Button cBtn = closeObj.GetComponent<Button>();
        cBtn.onClick.AddListener(CloseMap);

        GameObject cTxtObj = new GameObject("XText", typeof(RectTransform), typeof(CanvasRenderer));
        cTxtObj.layer = 5;
        cTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform cxRt = cTxtObj.GetComponent<RectTransform>();
        cxRt.anchorMin = Vector2.zero;
        cxRt.anchorMax = Vector2.one;
        cxRt.sizeDelta = Vector2.zero;
        var cxTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        cxTmp.text = "<b>✕</b>";
        cxTmp.fontSize = 18f;
        cxTmp.alignment = TextAlignmentOptions.Center;
        cxTmp.color = Color.white;

        // 4. Khu Vực Viewport Bản Đồ (Có Mask cắt góc)
        GameObject viewportObj = new GameObject("MapViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportObj.layer = 5;
        viewportObj.transform.SetParent(frameObj.transform, false);
        mapViewportRt = viewportObj.GetComponent<RectTransform>();
        mapViewportRt.anchorMin = Vector2.zero;
        mapViewportRt.anchorMax = Vector2.one;
        mapViewportRt.offsetMin = new Vector2(15f, 15f);
        mapViewportRt.offsetMax = new Vector2(-15f, -55f);

        Image vpImg = viewportObj.GetComponent<Image>();
        vpImg.color = new Color(0.02f, 0.04f, 0.08f, 1f);
        Mask vpMask = viewportObj.GetComponent<Mask>();
        vpMask.showMaskGraphic = true;

        // 5. Map Content (Nơi chứa các phòng và chấm thực thể, di chuyển khi kéo)
        GameObject contentObj = new GameObject("MapContent", typeof(RectTransform));
        contentObj.layer = 5;
        contentObj.transform.SetParent(viewportObj.transform, false);
        mapContentRt = contentObj.GetComponent<RectTransform>();
        mapContentRt.anchorMin = new Vector2(0.5f, 0.5f);
        mapContentRt.anchorMax = new Vector2(0.5f, 0.5f);
        mapContentRt.pivot = new Vector2(0.5f, 0.5f);
        mapContentRt.anchoredPosition = Vector2.zero;
        mapContentRt.sizeDelta = new Vector2(2000f, 2000f);

        // Grid lines nền bản đồ
        GameObject gridObj = new GameObject("GridBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gridObj.layer = 5;
        gridObj.transform.SetParent(contentObj.transform, false);
        RectTransform gRt = gridObj.GetComponent<RectTransform>();
        gRt.anchorMin = Vector2.zero;
        gRt.anchorMax = Vector2.one;
        gRt.sizeDelta = Vector2.zero;
        Image gImg = gridObj.GetComponent<Image>();
        gImg.color = new Color(0.08f, 0.12f, 0.18f, 0.45f);

        // Container các Phòng
        GameObject roomsObj = new GameObject("RoomsContainer", typeof(RectTransform));
        roomsObj.layer = 5;
        roomsObj.transform.SetParent(contentObj.transform, false);
        roomsContainerRt = roomsObj.GetComponent<RectTransform>();
        roomsContainerRt.anchorMin = Vector2.zero;
        roomsContainerRt.anchorMax = Vector2.one;
        roomsContainerRt.sizeDelta = Vector2.zero;

        // Container các Chấm Marker Thực Thể
        GameObject markersObj = new GameObject("MarkersContainer", typeof(RectTransform));
        markersObj.layer = 5;
        markersObj.transform.SetParent(contentObj.transform, false);
        markersContainerRt = markersObj.GetComponent<RectTransform>();
        markersContainerRt.anchorMin = Vector2.zero;
        markersContainerRt.anchorMax = Vector2.one;
        markersContainerRt.sizeDelta = Vector2.zero;

        // Player Marker (Icon Mũi Tên Xanh Ngọc Bích)
        GameObject pMarkerObj = new GameObject("PlayerMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pMarkerObj.layer = 5;
        pMarkerObj.transform.SetParent(markersContainerRt, false);
        playerMarkerRt = pMarkerObj.GetComponent<RectTransform>();
        playerMarkerRt.sizeDelta = new Vector2(22f, 22f);
        playerMarkerImg = pMarkerObj.GetComponent<Image>();
        playerMarkerImg.sprite = arrowFillSprite;
        playerMarkerImg.color = new Color(0.18f, 1f, 0.45f, 1f);

        // 6. Bảng Thống Kê Dưới Góc Trái (Dungeon Stats Panel)
        GameObject statsPanel = new GameObject("StatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statsPanel.layer = 5;
        statsPanel.transform.SetParent(frameObj.transform, false);
        RectTransform stRt = statsPanel.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(0f, 0f);
        stRt.pivot = new Vector2(0f, 0f);
        stRt.anchoredPosition = new Vector2(25f, 25f);
        stRt.sizeDelta = new Vector2(240f, 75f);
        Image stImg = statsPanel.GetComponent<Image>();
        stImg.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);
        var stOutline = statsPanel.AddComponent<Outline>();
        stOutline.effectColor = new Color(0.2f, 0.45f, 0.7f, 0.7f);

        GameObject stTxtObj = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer));
        stTxtObj.layer = 5;
        stTxtObj.transform.SetParent(statsPanel.transform, false);
        RectTransform stTxtRt = stTxtObj.GetComponent<RectTransform>();
        stTxtRt.anchorMin = Vector2.zero;
        stTxtRt.anchorMax = Vector2.one;
        stTxtRt.offsetMin = new Vector2(12f, 6f);
        stTxtRt.offsetMax = new Vector2(-12f, -6f);
        statsText = stTxtObj.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 11.5f;
        statsText.color = Color.white;
        statsText.lineSpacing = 4f;

        // 7. Bảng Gợi Ý Chiến Thuật Dưới Góc Phải (Upgrade & Tactical Hints)
        GameObject hintPanel = new GameObject("HintPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hintPanel.layer = 5;
        hintPanel.transform.SetParent(frameObj.transform, false);
        RectTransform hntRt = hintPanel.GetComponent<RectTransform>();
        hntRt.anchorMin = new Vector2(1f, 0f);
        hntRt.anchorMax = new Vector2(1f, 0f);
        hntRt.pivot = new Vector2(1f, 0f);
        hntRt.anchoredPosition = new Vector2(-25f, 25f);
        hntRt.sizeDelta = new Vector2(300f, 75f);
        Image hntImg = hintPanel.GetComponent<Image>();
        hntImg.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);
        var hntOutline = hintPanel.AddComponent<Outline>();
        hntOutline.effectColor = new Color(0.2f, 0.45f, 0.7f, 0.7f);

        GameObject hntTxtObj = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer));
        hntTxtObj.layer = 5;
        hntTxtObj.transform.SetParent(hintPanel.transform, false);
        RectTransform hntTxtRt = hntTxtObj.GetComponent<RectTransform>();
        hntTxtRt.anchorMin = Vector2.zero;
        hntTxtRt.anchorMax = Vector2.one;
        hntTxtRt.offsetMin = new Vector2(12f, 6f);
        hntTxtRt.offsetMax = new Vector2(-12f, -6f);
        hintText = hntTxtObj.AddComponent<TextMeshProUGUI>();
        hintText.fontSize = 11f;
        hintText.color = Color.white;

        // 8. Cụm Nút Điều Khiển Bản Đồ (Zoom In, Zoom Out, Focus Player)
        GameObject ctrlPanel = new GameObject("MapControls", typeof(RectTransform));
        ctrlPanel.layer = 5;
        ctrlPanel.transform.SetParent(frameObj.transform, false);
        RectTransform cpRt = ctrlPanel.GetComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(1f, 0.5f);
        cpRt.anchorMax = new Vector2(1f, 0.5f);
        cpRt.pivot = new Vector2(1f, 0.5f);
        cpRt.anchoredPosition = new Vector2(-25f, 20f);
        cpRt.sizeDelta = new Vector2(45f, 150f);

        CreateControlButton(ctrlPanel.transform, "+", new Vector2(0f, 45f), () => AdjustZoom(0.25f));
        CreateControlButton(ctrlPanel.transform, "-", new Vector2(0f, 0f), () => AdjustZoom(-0.25f));
        CreateControlButton(ctrlPanel.transform, "🎯", new Vector2(0f, -45f), FocusOnPlayer);
    }

    private void CreateControlButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.layer = 5;
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(36f, 36f);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.1f, 0.16f, 0.26f, 0.95f);
        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.6f, 0.9f, 0.7f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"<b>{label}</b>";
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private static void EnsureProceduralSprites()
    {
        if (circleFillSprite != null) return;

        // 1. Circle Fill (32x32)
        Texture2D tCirc = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Vector2 cCirc = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), cCirc);
                if (d <= 14f)
                {
                    float alpha = Mathf.Clamp01((14f - d) * 2.5f);
                    tCirc.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else tCirc.SetPixel(x, y, Color.clear);
            }
        }
        tCirc.Apply();
        circleFillSprite = Sprite.Create(tCirc, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 2. Diamond Fill (32x32)
        Texture2D tDia = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Abs(x - 15.5f) / 14f;
                float dy = Mathf.Abs(y - 15.5f) / 14f;
                if (dx + dy <= 1f) tDia.SetPixel(x, y, Color.white);
                else tDia.SetPixel(x, y, Color.clear);
            }
        }
        tDia.Apply();
        diamondFillSprite = Sprite.Create(tDia, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 3. Arrow Fill (32x32)
        Texture2D tArr = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float nx = (x - 15.5f) / 15.5f;
                float ny = y / 31f;
                if (ny >= Mathf.Abs(nx) * 0.85f && y >= 4) tArr.SetPixel(x, y, Color.white);
                else tArr.SetPixel(x, y, Color.clear);
            }
        }
        tArr.Apply();
        arrowFillSprite = Sprite.Create(tArr, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 4. Skull Sprite (32x32)
        Texture2D tSkull = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 17f));
                if (d <= 12f)
                {
                    // Eye sockets
                    bool leftEye = (Vector2.Distance(new Vector2(x, y), new Vector2(11f, 17f)) < 3f);
                    bool rightEye = (Vector2.Distance(new Vector2(x, y), new Vector2(20f, 17f)) < 3f);
                    if (leftEye || rightEye) tSkull.SetPixel(x, y, Color.clear);
                    else tSkull.SetPixel(x, y, Color.white);
                }
                else if (y <= 8 && Mathf.Abs(x - 15.5f) <= 7f)
                {
                    tSkull.SetPixel(x, y, Color.white);
                }
                else tSkull.SetPixel(x, y, Color.clear);
            }
        }
        tSkull.Apply();
        skullSprite = Sprite.Create(tSkull, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 5. Chest Sprite (32x32)
        Texture2D tChest = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (x >= 4 && x <= 27 && y >= 6 && y <= 25)
                {
                    if (y == 16 || (x >= 14 && x <= 17 && y >= 13 && y <= 19))
                        tChest.SetPixel(x, y, new Color(1f, 0.9f, 0.4f, 1f));
                    else
                        tChest.SetPixel(x, y, Color.white);
                }
                else tChest.SetPixel(x, y, Color.clear);
            }
        }
        tChest.Apply();
        chestSprite = Sprite.Create(tChest, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 6. Shrine Sprite (32x32)
        Texture2D tShrine = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Abs(x - 15.5f) / 14f;
                float dy = Mathf.Abs(y - 15.5f) / 14f;
                if (dx + dy <= 1f && (dx + dy >= 0.5f || (dx < 0.2f && dy < 0.2f)))
                    tShrine.SetPixel(x, y, Color.white);
                else tShrine.SetPixel(x, y, Color.clear);
            }
        }
        tShrine.Apply();
        shrineSprite = Sprite.Create(tShrine, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 7. Portal Sprite (32x32)
        Texture2D tPortal = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                if (d <= 14f && (d >= 10f || d <= 5f))
                    tPortal.SetPixel(x, y, Color.white);
                else tPortal.SetPixel(x, y, Color.clear);
            }
        }
        tPortal.Apply();
        portalSprite = Sprite.Create(tPortal, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 8. Square Sliced Border Sprite (32x32 with 4px border)
        Texture2D tBorder = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (x < 3 || x >= 29 || y < 3 || y >= 29)
                    tBorder.SetPixel(x, y, Color.white);
                else
                    tBorder.SetPixel(x, y, new Color(1f, 1f, 1f, 0.25f));
            }
        }
        tBorder.Apply();
        squareBorderSprite = Sprite.Create(tBorder, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
    }
}
