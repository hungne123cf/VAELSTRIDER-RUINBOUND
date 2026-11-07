using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Bảng chỉ số nhân vật (Character Stat HUD) - 2 cột, nền kính mờ Dark Fantasy.
/// Nút thu gọn/mở rộng ở góc dưới phải. Cập nhật realtime 4 lần/giây (nhẹ cho Android).
/// Tự gắn vào canvas gameplay khi scene có PlayerHealth.
/// </summary>
public class PlayerStatPanelUI : MonoBehaviour
{
    public static PlayerStatPanelUI Instance { get; private set; }

    private const string PREF_COLLAPSED = "StatPanel_Collapsed";
    private const float REFRESH_INTERVAL = 0.25f;

    private RectTransform panelRt;
    private GameObject contentObj;
    private TextMeshProUGUI toggleLabel;
    private bool collapsed;
    private float refreshTimer;

    private TextMeshProUGUI hpTxt, atkTxt, aspdTxt, critTxt, moveTxt;
    private TextMeshProUGUI defTxt, magTxt, cdrTxt, lsTxt;

    private static Sprite circleSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Chỉ tạo trong scene gameplay (có người chơi)
        if (Object.FindAnyObjectByType<PlayerHealth>() == null) return;
        EnsureInstance();
    }

    public static PlayerStatPanelUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = Object.FindAnyObjectByType<PlayerStatPanelUI>(FindObjectsInactive.Include);
        if (existing != null) { existing.gameObject.SetActive(true); Instance = existing; return existing; }

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) return null;

        GameObject root = new GameObject("PlayerStatPanel", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var ui = root.AddComponent<PlayerStatPanelUI>();
        ui.Build();
        Instance = ui;
        return ui;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ======================= BUILD UI =======================
    private void Build()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/VaelstriderFont") ?? TMP_Settings.defaultFontAsset;

        panelRt = GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(24f, -190f); // Dưới cụm Avatar/Thanh máu
        panelRt.sizeDelta = new Vector2(380f, 190f);

        // Nội dung (nền + 2 cột)
        contentObj = new GameObject("Content", typeof(RectTransform), typeof(Image));
        contentObj.transform.SetParent(transform, false);
        var cRt = contentObj.GetComponent<RectTransform>();
        cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
        cRt.offsetMin = cRt.offsetMax = Vector2.zero;
        var bg = contentObj.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.09f, 0.78f);
        bg.raycastTarget = false;
        var outline = contentObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.55f, 0.40f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        float rowH = 34f, top = -14f;
        // Cột trái
        hpTxt   = CreateRow(font, 0, top - rowH * 0, new Color(0.45f, 0.95f, 0.35f), "♥");
        atkTxt  = CreateRow(font, 0, top - rowH * 1, new Color(1f, 0.55f, 0.20f), "⚔");
        aspdTxt = CreateRow(font, 0, top - rowH * 2, new Color(1f, 0.85f, 0.25f), "»");
        critTxt = CreateRow(font, 0, top - rowH * 3, new Color(1f, 0.30f, 0.30f), "◎");
        moveTxt = CreateRow(font, 0, top - rowH * 4, new Color(0.90f, 0.92f, 0.95f), "➤");
        // Cột phải
        defTxt  = CreateRow(font, 1, top - rowH * 0, new Color(0.88f, 0.90f, 0.95f), "■");
        magTxt  = CreateRow(font, 1, top - rowH * 1, new Color(0.35f, 0.75f, 1f), "✦");
        cdrTxt  = CreateRow(font, 1, top - rowH * 2, new Color(0.70f, 0.75f, 0.95f), "⌛");
        lsTxt   = CreateRow(font, 1, top - rowH * 3, new Color(1f, 0.45f, 0.15f), "♦");

        // Nút thu gọn / mở rộng (luôn hiện)
        GameObject btnObj = new GameObject("ToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(transform, false);
        toggleRt = btnObj.GetComponent<RectTransform>();
        toggleRt.sizeDelta = new Vector2(52f, 52f);
        toggleImg = btnObj.GetComponent<Image>();
        toggleImg.sprite = GetStatIconSprite();
        toggleImg.color = Color.white;
        btnObj.GetComponent<Button>().onClick.AddListener(Toggle);

        GameObject lblObj = new GameObject("Label", typeof(RectTransform));
        lblObj.transform.SetParent(btnObj.transform, false);
        var lRt = lblObj.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one; lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        toggleLabel = lblObj.AddComponent<TextMeshProUGUI>();
        if (font != null) toggleLabel.font = font;
        toggleLabel.fontSize = 24; toggleLabel.fontStyle = FontStyles.Bold;
        toggleLabel.alignment = TextAlignmentOptions.Center;
        toggleLabel.color = Color.white;
        toggleLabel.raycastTarget = false;

        collapsed = PlayerPrefs.GetInt(PREF_COLLAPSED, 0) == 1;
        ApplyCollapsed();
        Refresh();
    }

    private TextMeshProUGUI CreateRow(TMP_FontAsset font, int col, float y, Color iconColor, string glyph)
    {
        float x = col == 0 ? 16f : 212f;

        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(contentObj.transform, false);
        var iRt = icon.GetComponent<RectTransform>();
        iRt.anchorMin = iRt.anchorMax = new Vector2(0f, 1f);
        iRt.pivot = new Vector2(0f, 1f);
        iRt.anchoredPosition = new Vector2(x, y);
        iRt.sizeDelta = new Vector2(26f, 26f);
        var img = icon.GetComponent<Image>();
        img.sprite = GetPixelIcon(glyph, iconColor);
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = false;

        GameObject t = new GameObject("Value", typeof(RectTransform));
        t.transform.SetParent(contentObj.transform, false);
        var tRt = t.GetComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = new Vector2(0f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(x + 34f, y + 2f);
        tRt.sizeDelta = new Vector2(160f, 30f);
        var tmp = t.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 21;
        tmp.color = new Color(0.93f, 0.90f, 0.82f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ======================= PIXEL ICONS =======================
    // '#' = màu chính, 'w' = điểm sáng; viền tối được tự động sinh quanh hình.
    private static readonly System.Collections.Generic.Dictionary<string, string[]> IconMaps =
        new System.Collections.Generic.Dictionary<string, string[]>
    {
        { "♥", new[] { // Máu - trái tim
            "..##....##..", ".####..####.", "############", "#ww#########", "#w##########",
            "############", ".##########.", "..########..", "...######...", "....####....", ".....##....." } },
        { "⚔", new[] { // Tấn công - thanh kiếm
            "..........ww", ".........w##", "........w##.", ".......w##..", "......w##...",
            ".....w##....", ".#..w##.....", ".#####......", "..###.......", ".####.......", "##..#.......", "#..........." } },
        { "»", new[] { // Tốc đánh - tia sét
            "......####..", ".....###w...", "....####....", "...####.....", "..########..",
            ".....####...", "....####....", "...###......", "..###.......", ".##.........", ".#.........." } },
        { "◎", new[] { // Chí mạng - tâm ngắm
            "....####....", "..##.##.##..", ".#...##...#.", ".#........#.", "#..........#",
            "####.ww.####", "####.ww.####", "#..........#", ".#........#.", ".#...##...#.", "..##.##.##..", "....####...." } },
        { "➤", new[] { // Di chuyển - chiếc giày
            "...####.....", "...#w##.....", "...#w##.....", "...####.....", "...####.....",
            "...#####....", "..#######...", "..########..", ".##########.", ".###########", ".www########" } },
        { "■", new[] { // Giáp - khiên
            ".##########.", "############", "#ww#########", "#w##########", "############",
            "############", ".##########.", ".##########.", "..########..", "...######...", "....####....", ".....##....." } },
        { "✦", new[] { // Phép - ngôi sao
            ".....##.....", ".....##.....", "....#ww#....", "....####....", "############",
            ".##########.", "..########..", "...######...", "..###..###..", "..##....##..", ".##......##.", ".#........#." } },
        { "⌛", new[] { // Hồi chiêu - đồng hồ cát
            "############", ".#........#.", "..#......#..", "...#wwww#...", "....####....", ".....##.....",
            ".....##.....", "....#..#....", "...#....#...", "..#.wwww.#..", ".##########.", "############" } },
        { "♦", new[] { // Hút máu - giọt máu
            ".....##.....", ".....##.....", "....####....", "....####....", "...######...", "..########..",
            "..#w######..", ".##w#######.", ".##########.", ".##########.", "..########..", "...######..." } },
    };

    private static readonly System.Collections.Generic.Dictionary<string, Sprite> iconCache =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    public static Sprite GetPixelIcon(string key, Color main)
    {
        if (iconCache.TryGetValue(key, out var cached) && cached != null) return cached;
        if (!IconMaps.TryGetValue(key, out var rows)) return GetCircleSprite();

        int h = rows.Length, w = 0;
        foreach (var r in rows) w = Mathf.Max(w, r.Length);
        int size = Mathf.Max(w, h) + 2; // +2 cho viền
        int ox = (size - w) / 2, oy = (size - h) / 2;

        var grid = new char[size, size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) grid[x, y] = '.';
        for (int r = 0; r < h; r++)
            for (int c = 0; c < rows[r].Length; c++)
                grid[ox + c, size - 1 - (oy + r)] = rows[r][c]; // lật trục Y cho texture

        Color outline = new Color(0.04f, 0.03f, 0.05f, 1f);
        Color hi = Color.Lerp(main, Color.white, 0.65f);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                char ch = grid[x, y];
                Color col = new Color(0, 0, 0, 0);
                if (ch == '#') col = main;
                else if (ch == 'w') col = hi;
                else
                {
                    for (int dy = -1; dy <= 1 && col.a == 0; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= size || ny >= size) continue;
                            if (grid[nx, ny] == '#' || grid[nx, ny] == 'w') { col = outline; break; }
                        }
                }
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        iconCache[key] = sp;
        return sp;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int s = 32;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float r = s * 0.5f - 1f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s * 0.5f, s * 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f)));
            }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }

    // ======================= LOGIC =======================
    public void SetTopYOffset(float yPos)
    {
        if (panelRt != null)
        {
            panelRt.anchoredPosition = new Vector2(24f, yPos);
        }
    }

    public void Toggle()
    {
        collapsed = !collapsed;
        PlayerPrefs.SetInt(PREF_COLLAPSED, collapsed ? 1 : 0);
        PlayerPrefs.Save();
        ApplyCollapsed();
        if (!collapsed) Refresh();
    }

    private void ApplyCollapsed()
    {
        if (contentObj != null) contentObj.SetActive(!collapsed);
        if (toggleLabel != null) toggleLabel.text = "";
        if (toggleRt != null)
        {
            if (collapsed)
            {
                // Thu gọn: icon nằm gọn ở góc trên-trái (ngay dưới thanh máu)
                toggleRt.anchorMin = toggleRt.anchorMax = new Vector2(0f, 1f);
                toggleRt.pivot = new Vector2(0f, 1f);
                toggleRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                // Mở rộng: icon nằm ở góc trên-phải của bảng
                toggleRt.anchorMin = toggleRt.anchorMax = new Vector2(1f, 1f);
                toggleRt.pivot = new Vector2(0.5f, 0.5f);
                toggleRt.anchoredPosition = new Vector2(-4f, -4f);
            }
        }
        if (toggleImg != null) toggleImg.color = collapsed ? Color.white : new Color(1f, 1f, 1f, 0.85f);
    }

    private RectTransform toggleRt;
    private Image toggleImg;
    private static Sprite statIconSprite;

    /// <summary>Icon tròn: nền đá đen, viền vàng đồng, 3 cột chỉ số vàng (biểu tượng bảng chỉ số).</summary>
    private static Sprite GetStatIconSprite()
    {
        if (statIconSprite != null) return statIconSprite;
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        Color dark = new Color(0.07f, 0.09f, 0.13f, 0.95f);
        Color gold = new Color(0.95f, 0.78f, 0.38f, 1f);
        Color goldDim = new Color(0.62f, 0.50f, 0.26f, 1f);
        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        float rOut = s * 0.5f - 1f, rRing = rOut - 4f;
        // 3 cột: x-range, chiều cao
        int[] bx = { 20, 29, 38 }; int[] bh = { 12, 22, 16 }; int bw = 6, baseY = 20;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                Color col = new Color(0, 0, 0, 0);
                if (d <= rOut)
                {
                    col = d > rRing ? gold : (d > rRing - 1.5f ? goldDim : dark);
                    for (int i = 0; i < 3; i++)
                        if (x >= bx[i] && x < bx[i] + bw && y >= baseY && y < baseY + bh[i]) col = gold;
                    if (y == baseY - 2 && x >= 17 && x < 47) col = goldDim;
                    col.a *= Mathf.Clamp01(rOut - d + 0.5f);
                }
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        statIconSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        return statIconSprite;
    }

    void Update()
    {
        if (collapsed) return;
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = REFRESH_INTERVAL;
        Refresh();
    }

    private PlayerHealth cHealth;
    private PlayerCombat cCombat;
    private PlayerController cCtrl;

    private void Refresh()
    {
        if (cHealth == null) cHealth = PlayerHealth.Instance ?? Object.FindAnyObjectByType<PlayerHealth>();
        if (cHealth == null) return;
        if (cCombat == null) cCombat = cHealth.GetComponent<PlayerCombat>();
        if (cCtrl == null) cCtrl = cHealth.GetComponent<PlayerController>();

        hpTxt.text = $"{cHealth.CurrentHealth:N0}/{cHealth.MaxHealth:N0}";

        if (cCombat != null)
        {
            // Hiển thị thống nhất với Carousel chọn tướng:
            // - Tướng AP (Pháp Sư, Lam Kiếm Sĩ): Sát thương cơ bản nằm ở cột Phép ✦ (AP). Cột Kiếm ⚔ (Vật lý) là 0.
            // - Tướng Vật Lý (Vael, Ronin, Titan, Dune): Sát thương cơ bản nằm ở cột Kiếm ⚔ (ATK). Cột Phép ✦ là 0.
            atkTxt.text  = cCombat.TotalPhysicalDamage.ToString("N0");
            magTxt.text  = cCombat.TotalMagicDamage.ToString("N0");

            aspdTxt.text = cCombat.AttackSpeedMultiplier.ToString("0.00") + "x";
            critTxt.text = Mathf.RoundToInt(cCombat.TotalCritChance * 100f) + "%";
            lsTxt.text   = FormatPercent(cCombat.PhysLifestealPercent);
            // Hiển thị % Giảm Hồi Chiêu (CDR): Bắt đầu từ 0%, nhặt lõi cộng dồn lên 10%, 20%, 30%...
            cdrTxt.text  = Mathf.RoundToInt(cCombat.TotalCDRPercent) + "%";
        }

        if (cCtrl != null) moveTxt.text = cCtrl.MoveSpeed.ToString("0.0");

        // Giáp hiển thị tổng hợp: Giáp cơ bản của tướng đã chọn + Đồ trang bị + Lõi nâng cấp
        string curCharId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        var cData = CharacterManager.EnsureInstance()?.GetCharacter(curCharId);
        int baseDef = cData != null ? cData.baseDefense : 20;
        ArmorShopUI.GetEquippedArmorBonus(out _, out float armorDef, out _);
        if (armorDef > 0f && armorDef <= 1f) armorDef *= 100f;
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out int sockDef, out _, out _, out _, out _);
        HeroCostumeManager.GetEquippedCostumeBonus(curCharId, out _, out _, out float costumeDef, out _);
        int augDef = AugmentManager.Instance != null ? AugmentManager.Instance.GetAugDef() : 0;
        int totalDisplayDef = Mathf.RoundToInt(baseDef + armorDef + sockDef + costumeDef + augDef);
        defTxt.text = totalDisplayDef.ToString();
    }

    // Hút máu có thể lưu dạng 0.1 hoặc 10 -> chuẩn hóa về %
    private static string FormatPercent(float v)
    {
        float p = v <= 1f ? v * 100f : v;
        return Mathf.RoundToInt(p) + "%";
    }
}

