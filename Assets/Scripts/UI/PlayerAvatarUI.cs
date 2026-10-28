using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Player Avatar HUD (Circular Frame)
/// Hiển thị khung Avatar TRÒN (Circular Mask & Golden Ring Bezel) cận cảnh khuôn mặt
/// của nhân vật người chơi đang chọn, xếp cạnh thanh Máu, Năng Lượng và EXP.
/// Tích hợp Huy hiệu Cấp độ (Level Badge) bo tròn, phản hồi trạng thái chiến đấu (Low HP, Shield).
/// </summary>
public class PlayerAvatarUI : MonoBehaviour
{
    private static PlayerAvatarUI _instance;
    public static PlayerAvatarUI Instance => _instance;

    [Header("UI References")]
    [SerializeField] private Image rootBgImage;
    [SerializeField] private Image maskImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image frameBorderImage;
    [SerializeField] private GameObject levelBadgeObj;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button avatarButton;

    [Header("Visual Styling")]
    [SerializeField] private Color normalBorderColor = new Color(0.92f, 0.78f, 0.38f, 1f); // Vàng kim cổ hoàng gia
    [SerializeField] private Color criticalHpBorderColor = new Color(1f, 0.20f, 0.20f, 1f); // Đỏ cảnh báo nguy cấp
    [SerializeField] private Color shieldBorderColor = new Color(1f, 0.70f, 0.15f, 1f); // Cam hổ phách khi có giáp

    private PlayerHealth _playerHealth;
    private int _cachedLevel = 1;
    private string _cachedHeroId = "";
    private float _pulseTimer = 0f;

    // Cache sprites
    private static Sprite _circleFillSprite;
    private static Sprite _circleRingSprite;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeEvents();
        SetupCircularStyle();
        RefreshAvatar();
        UpdateLevel(PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeEvents();
    }

    void Start()
    {
        SetupCircularStyle();
        RefreshAvatar();
        AlignHealthAndStatusBars();
        UpdateLevel(PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1);
    }

    void Update()
    {
        // Kiểm tra đổi nhân vật runtime
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        if (!string.Equals(curHero, _cachedHeroId, System.StringComparison.OrdinalIgnoreCase))
        {
            RefreshAvatar(curHero);
        }

        // Cập nhật hiệu ứng viền cảnh báo khi Máu thấp hoặc có Khiên ảo
        UpdateBorderStatusEffects();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AlignHealthAndStatusBars();
        SetupCircularStyle();
        RefreshAvatar();
        UpdateLevel(PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1);
    }

    private void SubscribeEvents()
    {
        PlayerLevelSystem.OnLevelUp += OnLevelUp;
    }

    private void UnsubscribeEvents()
    {
        PlayerLevelSystem.OnLevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateLevel(newLevel);
        if (levelBadgeObj != null)
        {
            levelBadgeObj.transform.localScale = Vector3.one * 1.35f;
        }
    }

    public void UpdateLevel(int level)
    {
        _cachedLevel = level;
        if (levelText != null)
        {
            levelText.text = $"LV.{level}";
        }
    }

    /// <summary>
    /// Đảm bảo toàn bộ thanh Máu, Năng Lượng và EXP dính liền vào khung Avatar tròn (Seamless Unit Frame).
    /// Các thanh xuất phát chính xác từ trục tâm x = 58f của khung Avatar tròn.
    /// Khung Avatar tròn (với nền tròn Obsidian 100% đục và vành Bezel mạ vàng) luôn nằm ở Sibling trên cùng (SetAsLastSibling),
    /// tự nhiên che phủ hoàn hảo phần đầu các thanh từ x = 58f đến mép vòng tròn,
    /// tạo nên vết cắt khuyết tròn toán học ôm khít 100% mà không bao giờ bị lem hay mất hiển thị!
    /// </summary>
    public static void AlignHealthAndStatusBars()
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform hb = canvas.transform.Find("PlayerHealthBar");
        if (hb == null) hb = canvas.transform.Find("HealthBar");
        if (hb != null)
        {
            RectTransform hbRt = hb.GetComponent<RectTransform>();
            if (hbRt != null)
            {
                // Mobile-optimized Soulslike HUD: Thanh máu dài 360px, dày 26px.
                // Mép trái xuất phát chính xác từ x = 64f (trục tâm của hình tròn Avatar đường kính 92px).
                // Mép phải kết thúc tại x = 424f -> Chiều dài = 360f.
                // Với pivot = (0.5, 0.5) -> center X = 64 + 180 = 244f. Center Y = -30f.
                if (Mathf.Abs(hbRt.anchoredPosition.x - 244f) > 0.5f || 
                    Mathf.Abs(hbRt.anchoredPosition.y - (-30f)) > 0.5f ||
                    Mathf.Abs(hbRt.sizeDelta.x - 360f) > 0.5f ||
                    Mathf.Abs(hbRt.sizeDelta.y - 26f) > 0.5f)
                {
                    hbRt.anchorMin = new Vector2(0f, 1f);
                    hbRt.anchorMax = new Vector2(0f, 1f);
                    hbRt.pivot = new Vector2(0.5f, 0.5f);
                    hbRt.anchoredPosition = new Vector2(244f, -30f);
                    hbRt.sizeDelta = new Vector2(360f, 26f);
                }
            }
        }

        // Căn chỉnh EnergyBar và ExpBar nếu đã tồn tại
        if (PlayerEnergyUI.Instance != null)
        {
            PlayerEnergyUI.Instance.AlignWithHealthBar();
        }
        if (PlayerExpBarUI.Instance != null)
        {
            PlayerExpBarUI.Instance.AlignWithHealthBar();
        }

        // Đảm bảo Khung Avatar tròn luôn nằm trên cùng (đè lên mép trái của 3 thanh)
        Transform avatarHud = canvas.transform.Find("PlayerAvatarHUD");
        if (avatarHud != null)
        {
            avatarHud.SetAsLastSibling();
        }

        // Áp dụng chủ đề mỹ thuật Dark Fantasy / Soulslike
        SoulslikeHUDStyler.ApplySoulslikeThemeToAll();
    }

    public static void ResetBarMaterials(Canvas canvas = null)
    {
        if (canvas == null) canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform hb = canvas.transform.Find("PlayerHealthBar");
        if (hb == null) hb = canvas.transform.Find("HealthBar");
        if (hb != null) ResetImagesMaterial(hb);

        Transform energyHud = canvas.transform.Find("Player_Energy_Bars_HUD");
        if (energyHud != null) ResetImagesMaterial(energyHud);

        Transform expHud = canvas.transform.Find("PlayerExpBarHUD");
        if (expHud != null) ResetImagesMaterial(expHud);
    }

    private static void ResetImagesMaterial(Transform root)
    {
        if (root == null) return;
        Image[] images = root.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.GetComponentInParent<PlayerAvatarUI>() != null) continue;
            if (img.material != null && img.material != img.defaultMaterial)
            {
                img.material = null;
            }
        }
    }

    void LateUpdate()
    {
        // Giữ PlayerAvatarHUD luôn ở trên cùng để vòng bezel vàng và LevelBadge đè lên mép trái của 3 thanh
        // tạo thành vết cắt khuyết tròn hoàn mỹ, ôm khít 100% không bị lem hay đè lên badge
        if (transform.parent != null && transform.GetSiblingIndex() < transform.parent.childCount - 1)
        {
            transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Chuyển đổi và thiết lập giao diện Avatar Tròn (Circular Frame & Mask)
    /// </summary>
    public void SetupCircularStyle()
    {
        // 1. Dọn dẹp component hình chữ nhật cũ nếu có
        Transform oldGothic = transform.Find("GothicFrameBorder");
        if (oldGothic != null) Destroy(oldGothic.gameObject);

        var oldOutline = GetComponent<Outline>();
        if (oldOutline != null) Destroy(oldOutline);

        Sprite circleSprite = GetCircleFillSprite();
        Sprite ringSprite = GetCircleRingSprite();

        // 2. Nền gốc hình tròn
        if (rootBgImage == null) rootBgImage = GetComponent<Image>();
        if (rootBgImage != null)
        {
            rootBgImage.sprite = circleSprite;
            rootBgImage.color = new Color(0.06f, 0.08f, 0.12f, 1f); // Nền tối Obsidian 100% che phủ mép thanh UI
        }

        // 3. Mask hình tròn
        Transform maskTrans = transform.Find("AvatarMask");
        if (maskTrans != null)
        {
            // Xóa RectMask2D cũ nếu có
            var oldRectMask = maskTrans.GetComponent<RectMask2D>();
            if (oldRectMask != null) Destroy(oldRectMask);

            maskImage = maskTrans.GetComponent<Image>();
            if (maskImage == null) maskImage = maskTrans.gameObject.AddComponent<Image>();
            maskImage.sprite = circleSprite;
            maskImage.color = Color.white;

            var maskComp = maskTrans.GetComponent<Mask>();
            if (maskComp == null) maskComp = maskTrans.gameObject.AddComponent<Mask>();
            maskComp.showMaskGraphic = false;

            RectTransform maskRt = maskTrans.GetComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = new Vector2(4f, 4f);
            maskRt.offsetMax = new Vector2(-4f, -4f);

            Transform imgTrans = maskTrans.Find("AvatarImage");
            if (imgTrans != null)
            {
                avatarImage = imgTrans.GetComponent<Image>();
                if (avatarImage != null)
                {
                    avatarImage.preserveAspect = true;
                    avatarImage.raycastTarget = false;
                }
            }
        }

        // 4. Viền tròn kim loại mạ vàng (Circular Ring Bezel)
        Transform ringTrans = transform.Find("CircularRingBorder");
        GameObject ringObj;
        if (ringTrans != null)
        {
            ringObj = ringTrans.gameObject;
        }
        else
        {
            ringObj = new GameObject("CircularRingBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.SetSiblingIndex(maskTrans != null ? maskTrans.GetSiblingIndex() + 1 : 1);
        }

        RectTransform ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.offsetMin = Vector2.zero;
        ringRt.offsetMax = Vector2.zero;

        frameBorderImage = ringObj.GetComponent<Image>();
        frameBorderImage.sprite = SoulslikeHUDStyler.GetBronzeBezelSprite() ?? ringSprite;
        frameBorderImage.color = Color.white;
        frameBorderImage.raycastTarget = false;

        // 5. Căn chỉnh Level Badge dưới đáy hình tròn (Khay đá Runic mạ đồng)
        Transform badgeTrans = transform.Find("LevelBadge");
        if (badgeTrans != null)
        {
            levelBadgeObj = badgeTrans.gameObject;
            badgeTrans.SetAsLastSibling(); // Luôn hiển thị trên viền tròn

            RectTransform bRt = badgeTrans.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0f, 2f);
            bRt.sizeDelta = new Vector2(46f, 20f);

            Image bImg = badgeTrans.GetComponent<Image>();
            if (bImg != null)
            {
                bImg.sprite = SoulslikeHUDStyler.GetRunicPlaqueSprite() ?? circleSprite;
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
            }

            var bOutline = badgeTrans.GetComponent<Outline>();
            if (bOutline != null)
            {
                Destroy(bOutline); // Runic plaque đã có viền đồng cổ tích hợp
            }

            levelText = badgeTrans.GetComponentInChildren<TextMeshProUGUI>();
            if (levelText != null)
            {
                levelText.fontSize = 12f;
                levelText.fontStyle = FontStyles.Bold;
                levelText.color = new Color(0.96f, 0.86f, 0.55f, 1f); // Vàng kim cổ điển Soulslike
                levelText.alignment = TextAlignmentOptions.Center;
                SoulslikeHUDStyler.SetSafeOutline(levelText, 0.35f, Color.black);
            }
        }

        // 6. Nút bấm tương tác
        if (avatarButton == null)
        {
            avatarButton = GetComponent<Button>();
            if (avatarButton == null) avatarButton = gameObject.AddComponent<Button>();
            avatarButton.transition = Selectable.Transition.ColorTint;
            avatarButton.targetGraphic = rootBgImage;
            avatarButton.onClick.RemoveAllListeners();
            avatarButton.onClick.AddListener(OnAvatarClicked);
        }
    }

    /// <summary>
    /// Nạp và hiển thị khuôn mặt nhân vật đang chọn
    /// </summary>
    public void RefreshAvatar(string heroId = null)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            heroId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
        }
        _cachedHeroId = heroId;

        Sprite faceSprite = GetHeroFaceSprite(heroId);
        if (avatarImage != null && faceSprite != null)
        {
            avatarImage.sprite = faceSprite;
            avatarImage.preserveAspect = true;

            GetAvatarVisualConfig(heroId, out Vector3 scale, out Vector2 offset);
            avatarImage.transform.localScale = scale;
            avatarImage.rectTransform.anchoredPosition = offset;
        }
    }

    private void UpdateBorderStatusEffects()
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (_playerHealth == null) return;
        }

        if (frameBorderImage == null) return;

        bool hasShield = _playerHealth.IsVirtualHealthActive();
        int curHp = _playerHealth.GetCurrentHealth();
        int maxHp = _playerHealth.GetMaxHealth();
        bool isLowHp = maxHp > 0 && ((float)curHp / maxHp) <= 0.25f;

        if (isLowHp)
        {
            _pulseTimer += Time.unscaledDeltaTime * 6f;
            float t = Mathf.PingPong(_pulseTimer, 1f);
            frameBorderImage.color = Color.Lerp(normalBorderColor, criticalHpBorderColor, t);
        }
        else if (hasShield)
        {
            _pulseTimer += Time.unscaledDeltaTime * 3f;
            float t = Mathf.PingPong(_pulseTimer, 1f);
            frameBorderImage.color = Color.Lerp(normalBorderColor, shieldBorderColor, t);
        }
        else
        {
            frameBorderImage.color = normalBorderColor;
        }

        // Khôi phục scale level badge nếu đang nảy
        if (levelBadgeObj != null && levelBadgeObj.transform.localScale.x > 1f)
        {
            levelBadgeObj.transform.localScale = Vector3.Lerp(levelBadgeObj.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 8f);
        }
    }

    private void OnAvatarClicked()
    {
        SoundManager.EnsureInstance()?.PlayMenuHover();
        if (InGameInventoryUI.Instance != null)
        {
            if (InGameInventoryUI.Instance.IsOpen())
                InGameInventoryUI.Instance.Close();
            else
                InGameInventoryUI.Instance.Open();
        }
        else
        {
            var modal = InGameInventoryUI.EnsureUIExists();
            modal?.Open();
        }
    }

    /// <summary>
    /// Sprite hình tròn đặc (Filled Circle) có khử răng cưa
    /// </summary>
    public static Sprite GetCircleFillSprite()
    {
        if (_circleFillSprite != null) return _circleFillSprite;

#if UNITY_EDITOR
        var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
        if (s != null) { _circleFillSprite = s; return s; }
#endif
        var resS = Resources.Load<Sprite>("UI/circle_fill");
        if (resS != null) { _circleFillSprite = resS; return resS; }

        // Procedural smooth anti-aliased circle (128x128)
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        Color[] cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        _circleFillSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _circleFillSprite;
    }

    /// <summary>
    /// Sprite vành khăn tròn (Circular Ring Border) có khử răng cưa tinh xảo
    /// </summary>
    public static Sprite GetCircleRingSprite()
    {
        if (_circleRingSprite != null) return _circleRingSprite;

#if UNITY_EDITOR
        var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/potion_slot_circle.png");
        if (s != null) { _circleRingSprite = s; return s; }
#endif
        var resS = Resources.Load<Sprite>("UI/potion_slot_circle");
        if (resS != null) { _circleRingSprite = resS; return resS; }

        // Procedural smooth anti-aliased ring (128x128)
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR - 9f; // Độ dày viền ~9px
        Color[] cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float outerAlpha = Mathf.Clamp01((outerR - dist) + 0.5f);
                float innerAlpha = Mathf.Clamp01((dist - innerR) + 0.5f);
                float alpha = Mathf.Min(outerAlpha, innerAlpha);
                cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        _circleRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _circleRingSprite;
    }

    /// <summary>
    /// Lấy Sprite của nhân vật được chọn
    /// </summary>
    public static Sprite GetHeroFaceSprite(string heroId)
    {
        string norm = HeroCostumeManager.NormalizeHeroId(heroId);

        // 1. Tải avatar chân dung cận cảnh khuôn mặt chuẩn từ Resources/UI/Avatars
        Sprite avatar = Resources.Load<Sprite>($"UI/Avatars/Avatar_{norm}");
        if (avatar != null) return avatar;

        string alias = "vael";
        if (norm == CharacterManager.CHAR_TITAN_BERSERKER) alias = "titan";
        else if (norm == CharacterManager.CHAR_AZURE_BLADE) alias = "azure";
        else if (norm == CharacterManager.CHAR_GALE_RONIN) alias = "ronin";
        else if (norm == CharacterManager.CHAR_DUNE_ASSASSIN) alias = "dune";
        else if (norm == CharacterManager.CHAR_CELESTIAL_MAGE) alias = "mage";

        avatar = Resources.Load<Sprite>($"UI/Avatars/avatar_{alias}");
        if (avatar != null) return avatar;

#if UNITY_EDITOR
        avatar = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Avatars/Avatar_{norm}.png");
        if (avatar != null) return avatar;
        avatar = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Avatars/avatar_{alias}.png");
        if (avatar != null) return avatar;
#endif

        if (norm == CharacterManager.CHAR_TITAN_BERSERKER)
        {
#if UNITY_EDITOR
            var p4 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Player4/UI/Hero_Player4_Portrait.png");
            if (p4 != null) return p4;
#endif
            var p4Res = Resources.Load<Sprite>("UI/Hero_Player4_Portrait");
            if (p4Res != null) return p4Res;
        }

        if (MainMenuManager.Instance != null)
        {
            Sprite s = MainMenuManager.Instance.GetHeroAvatarSprite(norm);
            if (s != null) return s;
        }

        return null;
    }

    /// <summary>
    /// Tỉ lệ zoom và toạ độ dịch chuyển tối ưu khi đặt trong Khung Tròn
    /// </summary>
    public static void GetAvatarVisualConfig(string heroId, out Vector3 scale, out Vector2 offset)
    {
        // Toàn bộ ảnh Avatar trong UI/Avatars đã được crop cận cảnh chính xác khuôn mặt,
        // căn giữa hoàn hảo tâm hình tròn.
        scale = Vector3.one;
        offset = Vector2.zero;
    }

    /// <summary>
    /// Khởi tạo hoặc tìm kiếm PlayerAvatarHUD trên Canvas
    /// </summary>
    public static PlayerAvatarUI EnsureUIExists(Canvas targetCanvas = null)
    {
        if (_instance != null)
        {
            _instance.SetupCircularStyle();
            return _instance;
        }

        var existing = FindAnyObjectByType<PlayerAvatarUI>();
        if (existing != null)
        {
            _instance = existing;
            _instance.SetupCircularStyle();
            return _instance;
        }

        if (targetCanvas == null) targetCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (targetCanvas == null) targetCanvas = FindAnyObjectByType<Canvas>();
        if (targetCanvas == null) return null;

        Transform existingObj = targetCanvas.transform.Find("PlayerAvatarHUD");
        if (existingObj != null)
        {
            _instance = existingObj.GetComponent<PlayerAvatarUI>();
            if (_instance == null) _instance = existingObj.gameObject.AddComponent<PlayerAvatarUI>();
            _instance.SetupCircularStyle();
            return _instance;
        }

        Sprite circleSprite = GetCircleFillSprite();
        Sprite ringSprite = GetCircleRingSprite();

        // 1. Tạo GameObject gốc PlayerAvatarHUD
        GameObject rootObj = new GameObject("PlayerAvatarHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootObj.transform.SetParent(targetCanvas.transform, false);

        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);
        rootRt.anchoredPosition = new Vector2(18f, -16f);
        rootRt.sizeDelta = new Vector2(92f, 92f);

        Image rootBg = rootObj.GetComponent<Image>();
        rootBg.sprite = circleSprite;
        rootBg.color = new Color(0.06f, 0.08f, 0.12f, 1f); // Nền tối Obsidian 100% che phủ mép thanh UI

        // 2. Mặt nạ Mask hình tròn
        GameObject maskObj = new GameObject("AvatarMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        maskObj.transform.SetParent(rootObj.transform, false);
        RectTransform maskRt = maskObj.GetComponent<RectTransform>();
        maskRt.anchorMin = Vector2.zero;
        maskRt.anchorMax = Vector2.one;
        maskRt.offsetMin = new Vector2(4f, 4f);
        maskRt.offsetMax = new Vector2(-4f, -4f);

        Image maskImg = maskObj.GetComponent<Image>();
        maskImg.sprite = circleSprite;
        maskImg.color = Color.white;

        Mask maskComp = maskObj.GetComponent<Mask>();
        maskComp.showMaskGraphic = false;

        // 3. Ảnh Avatar cận cảnh khuôn mặt
        GameObject imgObj = new GameObject("AvatarImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgObj.transform.SetParent(maskObj.transform, false);
        RectTransform imgRt = imgObj.GetComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.sizeDelta = Vector2.zero;
        Image imgComp = imgObj.GetComponent<Image>();
        imgComp.preserveAspect = true;
        imgComp.raycastTarget = false;

        // 4. Viền vành tròn mạ vàng (Circular Ring Bezel)
        GameObject ringObj = new GameObject("CircularRingBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ringObj.transform.SetParent(rootObj.transform, false);
        RectTransform ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.offsetMin = Vector2.zero;
        ringRt.offsetMax = Vector2.zero;

        Image ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = ringSprite;
        ringImg.color = new Color(0.92f, 0.78f, 0.38f, 1f);
        ringImg.raycastTarget = false;

        // 5. Huy hiệu Cấp Độ (Level Badge) bo tròn dưới đáy
        GameObject badgeObj = new GameObject("LevelBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObj.transform.SetParent(rootObj.transform, false);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 0f);
        badgeRt.anchorMax = new Vector2(0.5f, 0f);
        badgeRt.pivot = new Vector2(0.5f, 0.5f);
        badgeRt.anchoredPosition = new Vector2(0f, 2f);
        badgeRt.sizeDelta = new Vector2(46f, 20f);

        Image badgeBg = badgeObj.GetComponent<Image>();
        badgeBg.sprite = circleSprite;
        badgeBg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);

        var badgeOutline = badgeObj.AddComponent<Outline>();
        badgeOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.90f);
        badgeOutline.effectDistance = new Vector2(1f, -1f);

        // Text Level
        GameObject textObj = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(badgeObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "LV.1";
        tmp.fontSize = 12f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.88f, 0.35f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.35f, Color.black);
        tmp.raycastTarget = false;

        // Gán component PlayerAvatarUI
        _instance = rootObj.AddComponent<PlayerAvatarUI>();
        _instance.rootBgImage = rootBg;
        _instance.maskImage = maskImg;
        _instance.avatarImage = imgComp;
        _instance.frameBorderImage = ringImg;
        _instance.levelBadgeObj = badgeObj;
        _instance.levelText = tmp;

        // Căn chỉnh thanh máu bên cạnh
        AlignHealthAndStatusBars();
        _instance.RefreshAvatar();

        return _instance;
    }
}
