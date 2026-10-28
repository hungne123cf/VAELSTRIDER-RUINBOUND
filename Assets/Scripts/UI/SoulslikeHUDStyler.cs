using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Soulslike HUD Styler
/// Hệ thống định dạng mỹ thuật chuẩn Dark Fantasy / Soulslike (Elden Ring / Dark Souls):
/// - Tỉ lệ thanh mảnh, dài thanh thoát, không đè lấp tầm nhìn chiến đấu
/// - Rãnh đá Obsidian rèn chỉ đồng cổ hoàng gia chạm rãnh kép
/// - Ngọc Ruby (Máu), Lam Ngọc (Mana), Kim Đồng (Stamina), Thạch Anh Tím (EXP)
/// - Typography có chân ánh kim cổ điển với số liệu tinh gọn
/// - Khay đá Runic Cartouche đựng Linh Hồn Aethel Shard
/// </summary>
public static class SoulslikeHUDStyler
{
    private const string SOULSLIKE_FOLDER = "Assets/Sprites/UI/Soulslike/";

    private static Sprite _rubyFill;
    private static Sprite _sapphireFill;
    private static Sprite _amberFill;
    private static Sprite _amethystFill;
    private static Sprite _obsidianTrough;
    private static Sprite _bronzeBezel;
    private static Sprite _runicPlaque;
    private static Sprite _darkFantasyPanelBg;
    private static Sprite _stoneButton;
    private static Sprite _actionButtonRing;
    private static Sprite _actionButtonCore;
    private static Sprite _bossBarFrame;
    private static Sprite _gothicDivider;
    private static Sprite _potionSlotFrame;
    private static Sprite _framedPanelBorder;

    public static Sprite GetRubyFillSprite() => LoadSprite(ref _rubyFill, "Bar_Ruby_Fill.png");
    public static Sprite GetSapphireFillSprite() => LoadSprite(ref _sapphireFill, "Bar_Sapphire_Fill.png");
    public static Sprite GetAmberFillSprite() => LoadSprite(ref _amberFill, "Bar_Amber_Fill.png");
    public static Sprite GetAmethystFillSprite() => LoadSprite(ref _amethystFill, "Bar_Amethyst_Fill.png");
    public static Sprite GetObsidianTroughSprite() => LoadSprite(ref _obsidianTrough, "Bar_Obsidian_Trough.png");
    public static Sprite GetBronzeBezelSprite() => LoadSprite(ref _bronzeBezel, "Avatar_Bronze_Bezel.png");
    public static Sprite GetRunicPlaqueSprite() => LoadSprite(ref _runicPlaque, "Runic_Plaque_Bg.png");
    public static Sprite GetDarkFantasyPanelBg() => LoadSprite(ref _darkFantasyPanelBg, "Dark_Fantasy_Panel_Bg.png");
    public static Sprite GetStoneButtonSprite() => LoadSprite(ref _stoneButton, "Soulslike_Button_Stone.png");
    public static Sprite GetActionButtonRingSprite() => LoadSprite(ref _actionButtonRing, "Action_Button_Runic_Ring.png");
    public static Sprite GetActionButtonCoreSprite() => LoadSprite(ref _actionButtonCore, "Action_Button_Obsidian_Core.png");
    public static Sprite GetBossBarFrameSprite() => LoadSprite(ref _bossBarFrame, "Boss_Bar_Frame.png");
    public static Sprite GetGothicDividerSprite() => LoadSprite(ref _gothicDivider, "Gothic_Filigree_Divider.png");
    public static Sprite GetPotionSlotFrameSprite() => LoadSprite(ref _potionSlotFrame, "Potion_Slot_Runic_Frame.png");

    public static Sprite GetFramedPanelBorderSprite()
    {
        if (_framedPanelBorder != null) return _framedPanelBorder;
        _framedPanelBorder = Resources.Load<Sprite>("UI/frame_gothic_border");
        if (_framedPanelBorder != null) return _framedPanelBorder;
#if UNITY_EDITOR
        _framedPanelBorder = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/frame_gothic_border.png");
        if (_framedPanelBorder != null) return _framedPanelBorder;
#endif
        string[] candidates = new string[]
        {
            System.IO.Path.Combine(Application.dataPath, "Sprites/UI/frame_gothic_border.png"),
            System.IO.Path.Combine(Application.dataPath, "Resources/UI/frame_gothic_border.png"),
            System.IO.Path.Combine(Application.dataPath, "ART/UI/Default/Border/panel-border-004.png")
        };
        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _framedPanelBorder = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
                    break;
                }
            }
        }
        return _framedPanelBorder;
    }

    private static Sprite LoadSprite(ref Sprite cache, string filename)
    {
        if (cache != null) return cache;
        string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filename);
        cache = Resources.Load<Sprite>("UI/Soulslike/" + nameWithoutExt);
        if (cache != null) return cache;
#if UNITY_EDITOR
        cache = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SOULSLIKE_FOLDER + filename);
        if (cache != null) return cache;
#endif
        return cache;
    }

    /// <summary>
    /// Áp dụng toàn diện phong cách Soulslike lên toàn bộ HUD chiến đấu
    /// </summary>
    public static void ApplySoulslikeThemeToAll()
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        StyleAvatarHUD(canvas);
        StyleHealthBar(canvas);
        StyleEnergyHUD(canvas);
        StyleExpHUD(canvas);
        StyleShardsCounter(canvas);
        StyleBossHealthBar(canvas);
        StyleActionButtons(canvas);
        StylePotionSlot(canvas);
        StyleWaveAndTimer(canvas);
    }

    public static void StyleAvatarHUD(Canvas canvas)
    {
        Transform avatarHud = canvas.transform.Find("PlayerAvatarHUD");
        if (avatarHud == null) return;

        var avatarUI = avatarHud.GetComponent<PlayerAvatarUI>();
        if (avatarUI != null)
        {
            avatarUI.SetupCircularStyle();
        }
    }

    public static void StyleHealthBar(Canvas canvas)
    {
        Transform hb = canvas.transform.Find("PlayerHealthBar");
        if (hb == null) hb = canvas.transform.Find("HealthBar");
        if (hb == null) return;

        RectTransform hbRt = hb.GetComponent<RectTransform>();
        if (hbRt != null)
        {
            hbRt.anchorMin = new Vector2(0f, 1f);
            hbRt.anchorMax = new Vector2(0f, 1f);
            hbRt.pivot = new Vector2(0.5f, 0.5f);
            hbRt.anchoredPosition = new Vector2(244f, -30f);
            hbRt.sizeDelta = new Vector2(360f, 26f);
        }

        // Background
        Transform bg = hb.Find("Background");
        if (bg != null)
        {
            Image bgImg = bg.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = GetObsidianTroughSprite();
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
        }

        // Fill Sliders
        Transform fillArea = hb.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fill = fillArea.Find("Fill");
            if (fill != null)
            {
                Image fillImg = fill.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.sprite = GetRubyFillSprite();
                    fillImg.type = Image.Type.Sliced;
                    fillImg.color = Color.white;
                }
            }
        }

        // Number Text: Rõ nét, dày dặn, dễ nhìn trên mobile
        var tmp = hb.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            RectTransform txtRt = tmp.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-10f, 0f);

            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.98f, 0.94f, 0.85f, 1f); // Trắng ngà hoàng gia
            SetSafeOutline(tmp, 0.35f, new Color(0.05f, 0.05f, 0.05f, 0.95f));
        }
    }

    public static void StyleEnergyHUD(Canvas canvas)
    {
        Transform hud = canvas.transform.Find("Player_Energy_Bars_HUD");
        if (hud == null) return;

        // EnergyBar (Mana)
        Transform eb = hud.Find("EnergyBar");
        if (eb != null)
        {
            RectTransform ebRt = eb.GetComponent<RectTransform>();
            if (ebRt != null)
            {
                ebRt.anchoredPosition = new Vector2(0f, 0f);
                ebRt.sizeDelta = new Vector2(350f, 18f);
            }

            Image bg = eb.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = GetObsidianTroughSprite();
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            Image fill = eb.Find("FillArea/Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.sprite = GetSapphireFillSprite();
                fill.type = Image.Type.Sliced;
                fill.color = Color.white;
            }

            var txt = eb.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (txt != null)
            {
                RectTransform tRt = txt.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = new Vector2(10f, 0f);
                tRt.offsetMax = new Vector2(-10f, 0f);

                txt.alignment = TextAlignmentOptions.MidlineRight;
                txt.fontSize = 13f;
                txt.fontStyle = FontStyles.Bold;
                txt.color = new Color(0.80f, 0.93f, 1f, 1f); // Lam ngọc ánh sáng
                SetSafeOutline(txt, 0.35f, Color.black);
            }
        }

        // InnerEnergyBar (Stamina)
        Transform ieb = hud.Find("InnerEnergyBar");
        if (ieb != null)
        {
            RectTransform iebRt = ieb.GetComponent<RectTransform>();
            if (iebRt != null)
            {
                iebRt.anchoredPosition = new Vector2(0f, -21f);
                iebRt.sizeDelta = new Vector2(340f, 14f);
            }

            Image bg = ieb.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = GetObsidianTroughSprite();
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            Image fill = ieb.Find("FillArea/Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.sprite = GetAmberFillSprite();
                fill.type = Image.Type.Sliced;
                fill.color = Color.white;
            }

            var txt = ieb.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (txt != null) txt.gameObject.SetActive(false); // Ẩn số nội năng để giữ giao diện thanh thoát
        }
    }

    public static void StyleExpHUD(Canvas canvas)
    {
        Transform expHud = canvas.transform.Find("PlayerExpBarHUD");
        if (expHud == null) return;

        RectTransform expRt = expHud.GetComponent<RectTransform>();
        if (expRt != null)
        {
            expRt.sizeDelta = new Vector2(330f, 14f);
        }

        Image bg = expHud.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = GetObsidianTroughSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.35f, 0.25f, 0.45f, 1f); // Nền tím rãnh đá sâu thẳm rõ ràng
        }

        var outline = expHud.GetComponent<Outline>();
        if (outline == null) outline = expHud.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.40f, 0.75f, 0.85f); // Viền thạch anh tím rõ rệt
        outline.effectDistance = new Vector2(1f, -1f);

        Image fill = expHud.Find("FillArea/Fill")?.GetComponent<Image>();
        if (fill != null)
        {
            fill.sprite = GetAmethystFillSprite();
            fill.type = Image.Type.Sliced;
            fill.color = new Color(0.92f, 0.65f, 1.0f, 1f); // Tím pha lê rực rỡ
        }

        var txt = expHud.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            RectTransform tRt = txt.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(10f, 0f);
            tRt.offsetMax = new Vector2(-10f, 0f);

            txt.alignment = TextAlignmentOptions.MidlineRight;
            txt.fontSize = 13f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.98f, 0.94f, 1f, 1f);
            SetSafeOutline(txt, 0.40f, Color.black);
        }
    }

    public static void StyleShardsCounter(Canvas canvas)
    {
        Transform sc = canvas.transform.Find("ShardsCounter");
        if (sc == null) return;

        RectTransform scRt = sc.GetComponent<RectTransform>();
        if (scRt != null)
        {
            scRt.anchorMin = new Vector2(0f, 1f);
            scRt.anchorMax = new Vector2(0f, 1f);
            scRt.pivot = new Vector2(0.5f, 0.5f);
            // Căn lùi sang phải (X = 175f) ngay dưới thanh EXP, tránh hoàn toàn avatar và không bám rìa trái
            scRt.anchoredPosition = new Vector2(175f, -118f);
            scRt.sizeDelta = new Vector2(120f, 28f);
            scRt.localScale = Vector3.one;
        }

        // Gán nền khay đá Runic
        Image plaqueImg = sc.GetComponent<Image>();
        if (plaqueImg == null) plaqueImg = sc.gameObject.AddComponent<Image>();
        plaqueImg.sprite = GetRunicPlaqueSprite();
        plaqueImg.type = Image.Type.Sliced;
        plaqueImg.color = Color.white;

        // Căn chỉnh viên ngọc Shard bên trái khay
        Transform icon = sc.Find("Icon");
        if (icon == null && sc.childCount > 0)
        {
            for (int i = 0; i < sc.childCount; i++)
            {
                if (sc.GetChild(i).GetComponent<TextMeshProUGUI>() == null && sc.GetChild(i).GetComponent<Text>() == null)
                {
                    icon = sc.GetChild(i);
                    break;
                }
            }
        }
        if (icon != null)
        {
            RectTransform iRt = icon.GetComponent<RectTransform>();
            if (iRt != null)
            {
                iRt.anchorMin = new Vector2(0f, 0.5f);
                iRt.anchorMax = new Vector2(0f, 0.5f);
                iRt.pivot = new Vector2(0.5f, 0.5f);
                iRt.anchoredPosition = new Vector2(18f, 0f);
                iRt.sizeDelta = new Vector2(22f, 22f);
            }
        }

        // Chữ số Shard hiển thị bên phải viên ngọc màu vàng kim đồng
        var tmp = sc.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            RectTransform tRt = tmp.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(34f, 0f);
            tRt.offsetMax = new Vector2(-8f, 0f);

            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.98f, 0.88f, 0.55f, 1f); // Vàng kim cổ điển Soulslike
            SetSafeOutline(tmp, 0.40f, Color.black);
        }

        var leg = sc.GetComponentInChildren<Text>();
        if (leg != null)
        {
            leg.fontSize = 15;
            leg.fontStyle = FontStyle.Bold;
            leg.color = new Color(0.98f, 0.88f, 0.55f, 1f);
        }
    }

    public static void StyleBossHealthBar(Canvas canvas)
    {
        Transform bossPanel = canvas.transform.Find("BossHealthBarPanel");
        if (bossPanel == null) return;

        RectTransform bpRt = bossPanel.GetComponent<RectTransform>();
        if (bpRt != null) bpRt.anchoredPosition = new Vector2(0f, -44f);

        // Frame
        Transform frame = bossPanel.Find("FrameBg");
        if (frame != null)
        {
            RectTransform fRt = frame.GetComponent<RectTransform>();
            if (fRt != null)
            {
                fRt.sizeDelta = new Vector2(860f, 26f);
            }

            Image frameImg = frame.GetComponent<Image>();
            if (frameImg != null)
            {
                frameImg.sprite = GetBossBarFrameSprite();
                frameImg.type = Image.Type.Sliced;
                frameImg.color = Color.white;
            }

            // Ghost Fill
            Image gfImg = frame.Find("GhostSlider/Fill")?.GetComponent<Image>();
            if (gfImg != null)
            {
                gfImg.sprite = GetAmberFillSprite();
                gfImg.type = Image.Type.Sliced;
                gfImg.color = new Color(1f, 0.85f, 0.35f, 0.85f);
            }

            // Health Fill
            Image hpfImg = frame.Find("HealthSlider/Fill")?.GetComponent<Image>();
            if (hpfImg != null)
            {
                hpfImg.sprite = GetRubyFillSprite();
                hpfImg.type = Image.Type.Sliced;
                hpfImg.color = Color.white;
            }

            // HP Percent Text
            var hpTmp = frame.Find("HpPercent")?.GetComponent<TextMeshProUGUI>();
            if (hpTmp != null)
            {
                RectTransform tRt = hpTmp.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = new Vector2(10f, 0f);
                tRt.offsetMax = new Vector2(-18f, 0f);

                hpTmp.alignment = TextAlignmentOptions.MidlineRight;
                hpTmp.fontSize = 11.5f;
                hpTmp.fontStyle = FontStyles.Bold;
                hpTmp.color = new Color(0.96f, 0.92f, 0.82f, 0.95f);
                SetSafeOutline(hpTmp, 0.3f, Color.black);
            }
        }

        // Boss Name Text
        var nameTmp = bossPanel.Find("BossNameText")?.GetComponent<TextMeshProUGUI>();
        if (nameTmp != null)
        {
            nameTmp.fontSize = 20;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = new Color(0.96f, 0.88f, 0.58f); // Antique gold
            SetSafeOutline(nameTmp, 0.35f, Color.black);
        }

        // Ensure Gothic Filigree Divider
        Transform div = bossPanel.Find("BossDivider");
        if (div == null)
        {
            GameObject divObj = new GameObject("BossDivider");
            divObj.transform.SetParent(bossPanel, false);
            divObj.transform.SetSiblingIndex(1);

            RectTransform divRt = divObj.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.5f, 1f);
            divRt.anchorMax = new Vector2(0.5f, 1f);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.anchoredPosition = new Vector2(0f, -28f);
            divRt.sizeDelta = new Vector2(380f, 10f);

            Image divImg = divObj.AddComponent<Image>();
            divImg.sprite = GetGothicDividerSprite();
            divImg.color = new Color(1f, 0.9f, 0.65f, 0.85f);
            divImg.raycastTarget = false;
        }
    }

    public static void StyleActionButtons(Canvas canvas)
    {
        string[] btnNames = new string[] { "Btn_Attack", "Btn_Dash", "Btn_Special", "Btn_Skill" };
        foreach (var bName in btnNames)
        {
            Transform btnT = canvas.transform.Find(bName);
            if (btnT == null)
            {
                var mob = Object.FindObjectsByType<MobileActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var m in mob)
                {
                    if (m != null && m.gameObject.name == bName) { btnT = m.transform; break; }
                }
            }
            if (btnT == null) continue;

            // Xóa ObsidianCoreBg cũ nếu có để tránh che khuất icon
            Transform oldCore = btnT.Find("ObsidianCoreBg");
            if (oldCore != null) Object.Destroy(oldCore.gameObject);

            var mobComp = btnT.GetComponent<MobileActionButton>();
            if (mobComp != null)
            {
                mobComp.SendMessage("EnsureRoundedSprites", UnityEngine.SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public static void StylePotionSlot(Canvas canvas)
    {
        Transform slot = canvas.transform.Find("PotionQuickSlot");
        if (slot == null)
        {
            var pSlot = Object.FindAnyObjectByType<PotionQuickSlotUI>();
            if (pSlot != null) slot = pSlot.transform;
        }
        if (slot == null) return;

        Image bg = slot.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = GetPotionSlotFrameSprite();
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }

        var tmp = slot.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.96f, 0.88f, 0.55f, 1f);
            SetSafeOutline(tmp, 0.35f, Color.black);
        }
    }

    public static void StyleWaveAndTimer(Canvas canvas)
    {
        // Wave Banner (Tắt theo yêu cầu người chơi trên tất cả các map)
        Transform wave = canvas.transform.Find("WaveBanner");
        if (wave != null)
        {
            wave.gameObject.SetActive(false);
            RectTransform wRt = wave.GetComponent<RectTransform>();
            if (wRt != null)
            {
                bool isBossBarActive = (BossHealthBarUI.Instance != null && BossHealthBarUI.Instance.IsVisible);
                wRt.anchoredPosition = new Vector2(0f, isBossBarActive ? -152f : -95f);
            }

            Image bg = wave.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = GetRunicPlaqueSprite();
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            var tmp = wave.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.96f, 0.88f, 0.55f, 1f);
                SetSafeOutline(tmp, 0.35f, Color.black);
            }
        }

        // Match Timer
        Transform timer = canvas.transform.Find("MatchTimerPanel");
        if (timer != null)
        {
            Image bg = timer.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = GetRunicPlaqueSprite();
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            var tmp = timer.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.96f, 0.92f, 0.82f, 1f);
                SetSafeOutline(tmp, 0.35f, Color.black);
            }
        }
    }

    public static void StyleButton(Button btn, string buttonText = null, Color? textColor = null)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = GetStoneButtonSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        var outline = btn.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            if (!string.IsNullOrEmpty(buttonText)) tmp.text = buttonText;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor ?? new Color(0.96f, 0.88f, 0.65f, 1f); // Vàng kim cổ
            SetSafeOutline(tmp, 0.35f, Color.black);
        }
    }

    public static void StyleCloseButton(Button btn)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = GetStoneButtonSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.85f, 0.3f, 0.35f, 1f); // Ruby crimson tone on carved stone
        }

        var outline = btn.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        WhiteIconLoader.ReplaceTextWithIcon(btn, "Cross", Color.white, new Vector2(22f, 22f));
    }

    public static void SetSafeOutline(TextMeshProUGUI tmp, float width, Color color)
    {
        if (tmp == null) return;
        try
        {
            if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            if (tmp.font != null && tmp.fontSharedMaterial == null)
            {
                tmp.fontSharedMaterial = tmp.font.material;
            }

            if (tmp.fontSharedMaterial != null)
            {
                tmp.outlineWidth = width;
                tmp.outlineColor = color;
            }
        }
        catch (System.Exception)
        {
            // Suppress exception when font material instance cannot be cloned yet
        }
    }

    public static void StyleHeader(TextMeshProUGUI title, TextMeshProUGUI subtitle = null)
    {
        if (title != null)
        {
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.98f, 0.88f, 0.55f, 1f); // Hoàng kim Soulslike
            SetSafeOutline(title, 0.40f, new Color(0.05f, 0.05f, 0.05f, 0.95f));
        }

        if (subtitle != null)
        {
            subtitle.color = new Color(0.80f, 0.82f, 0.88f, 0.85f); // Bạc cổ
            SetSafeOutline(subtitle, 0.25f, new Color(0.02f, 0.02f, 0.04f, 0.90f));
        }
    }

    public static void StyleModalWindow(Transform window)
    {
        if (window == null) return;
        Image bg = window.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = GetDarkFantasyPanelBg();
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }

        var outline = window.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        Transform border = window.Find("FrameBorder");
        if (border == null) border = window.Find("GothicFrameBorder");
        if (border != null)
        {
            Image bImg = border.GetComponent<Image>();
            if (bImg != null)
            {
                bImg.sprite = GetDarkFantasyPanelBg();
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
            }
        }
    }

    public static void StyleTabButton(Button btn, bool isSelected, string text = null)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = GetStoneButtonSprite();
            img.type = Image.Type.Sliced;
            img.color = isSelected ? new Color(1f, 0.95f, 0.8f, 1f) : new Color(0.55f, 0.55f, 0.60f, 0.85f);
        }

        var outline = btn.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            if (!string.IsNullOrEmpty(text)) tmp.text = text;
            tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = isSelected ? new Color(1f, 0.92f, 0.55f, 1f) : new Color(0.75f, 0.78f, 0.85f, 0.9f);
            SetSafeOutline(tmp, 0.35f, Color.black);
        }
    }

    public static void StyleInputField(TMP_InputField input)
    {
        if (input == null) return;
        Image img = input.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = GetRunicPlaqueSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        var outline = input.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    public static GameObject AddFiligreeDivider(Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (parent == null) return null;
        GameObject divObj = new GameObject("SoulslikeDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divObj.layer = 5;
        divObj.transform.SetParent(parent, false);

        RectTransform rt = divObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        Image img = divObj.GetComponent<Image>();
        img.sprite = GetGothicDividerSprite();
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = new Color(0.95f, 0.85f, 0.55f, 0.90f);
        img.raycastTarget = false;

        return divObj;
    }
}
