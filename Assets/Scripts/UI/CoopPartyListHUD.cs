using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - In-Game Co-op Party List HUD (Bảng Tổ Đội / Danh Sách Người Chơi Trong Trận)
/// Hiển thị danh sách người chơi trong trận đấu (Bản thân và các Đồng đội Co-op)
/// Gồm: Tên người chơi, Avatar tướng, Thanh HP mini đổi màu theo % máu, và trạng thái Sống / K.O / Mất kết nối.
/// Tích hợp nút Thu gọn / Mở rộng giúp tối ưu tầm nhìn trên màn hình di động.
/// </summary>
public class CoopPartyListHUD : MonoBehaviour
{
    private static CoopPartyListHUD _instance;
    public static CoopPartyListHUD Instance => _instance;

    [Header("UI Hierarchy")]
    private GameObject _rootPanel;
    private RectTransform _rootRt;
    private Image _bgImage;
    private Outline _bgOutline;

    // Header
    private TextMeshProUGUI _headerTitleText;
    private Button _collapseBtn;
    private TextMeshProUGUI _collapseBtnText;
    private bool _isCollapsed = false;

    // Player Rows Container
    private GameObject _rowsContainer;
    private RectTransform _rowsContainerRt;

    // Cached Player Row Entries
    private class PlayerRowUI
    {
        public GameObject rowObj;
        public Image avatarImg;
        public TextMeshProUGUI nameText;
        public Image hpBarBg;
        public Image hpBarFill;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI statusTagText;
        public Button addFriendBtn;
        public Button kickBtn;

        public float currentFill = 1f;
        public float targetFill = 1f;
    }

    private readonly List<PlayerRowUI> _playerRows = new List<PlayerRowUI>();

    // Sprites cache
    private static Sprite _whitePixelSprite;
    private static Sprite _circleFillSprite;

    private float _updateInterval = 0.05f; // Cập nhật realtime 20Hz mượt mà
    private float _updateTimer = 0f;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        BuildUI();
        RefreshPartyList();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        if (sceneName == "MainMenuScene" || sceneName == "LoginScene")
        {
            if (_rootPanel != null) _rootPanel.SetActive(false);
        }
        else
        {
            if (_rootPanel != null) _rootPanel.SetActive(true);
            RefreshPartyList();
        }
    }

    void Update()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenuScene" || sceneName == "LoginScene")
        {
            if (_rootPanel != null && _rootPanel.activeSelf) _rootPanel.SetActive(false);
            return;
        }

        // Chỉ hiển thị khi đang trong trận đấu Gameplay
        if (_rootPanel != null && !_rootPanel.activeSelf)
        {
            _rootPanel.SetActive(true);
        }

        _updateTimer += Time.unscaledDeltaTime;
        if (_updateTimer >= _updateInterval)
        {
            _updateTimer = 0f;
            RefreshPartyList();
        }

        // Lerp mượt mà thanh fill máu
        for (int i = 0; i < _playerRows.Count; i++)
        {
            var row = _playerRows[i];
            if (row != null && row.hpBarFill != null && row.rowObj.activeSelf)
            {
                row.currentFill = Mathf.MoveTowards(row.currentFill, row.targetFill, Time.unscaledDeltaTime * 4f);
                row.hpBarFill.fillAmount = row.currentFill;
            }
        }
    }

    /// <summary>
    /// Đảm bảo HUD Tổ Đội tồn tại trên Canvas Gameplay
    /// </summary>
    public static CoopPartyListHUD EnsureUIExists(Canvas targetCanvas = null)
    {
        if (_instance != null) return _instance;

        var existing = FindAnyObjectByType<CoopPartyListHUD>();
        if (existing != null)
        {
            _instance = existing;
            return _instance;
        }

        if (targetCanvas == null) targetCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (targetCanvas == null) targetCanvas = FindAnyObjectByType<Canvas>();
        if (targetCanvas == null) return null;

        Transform existingObj = targetCanvas.transform.Find("CoopPartyListHUD");
        if (existingObj != null)
        {
            _instance = existingObj.GetComponent<CoopPartyListHUD>();
            if (_instance == null) _instance = existingObj.gameObject.AddComponent<CoopPartyListHUD>();
            return _instance;
        }

        GameObject hudGo = new GameObject("CoopPartyListHUD");
        hudGo.transform.SetParent(targetCanvas.transform, false);
        _instance = hudGo.AddComponent<CoopPartyListHUD>();
        return _instance;
    }

    private void BuildUI()
    {
        if (_rootPanel != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Canvas c = GameAutoBootstrap.GetMainGameplayCanvas() ?? FindAnyObjectByType<Canvas>();
            if (c != null) transform.SetParent(c.transform, false);
        }

        _rootPanel = gameObject;
        _rootRt = _rootPanel.GetComponent<RectTransform>();
        if (_rootRt == null) _rootRt = _rootPanel.AddComponent<RectTransform>();

        // Căn neo góc trên bên phải (Top-Right), nằm ngay bên trái cụm Gear & Nút Chat
        // Gear & Nút Chat xếp dọc ở mép phải x = -20f đến -112f.
        // Bảng Tổ Đội đặt tại x = -125f, y = -16f (Cách mép nút Gear/Chat 13px, hoàn toàn độc lập, không chồng lấn)
        _rootRt.anchorMin = new Vector2(1f, 1f);
        _rootRt.anchorMax = new Vector2(1f, 1f);
        _rootRt.pivot = new Vector2(1f, 1f);
        _rootRt.anchoredPosition = new Vector2(-125f, -16f);
        _rootRt.sizeDelta = new Vector2(230f, 124f);

        // Khung nền Dark Fantasy mờ tinh tế
        _bgImage = _rootPanel.AddComponent<Image>();
        _bgImage.color = new Color(0.06f, 0.08f, 0.13f, 0.88f);
        _bgImage.sprite = GetWhitePixelSprite();

        _bgOutline = _rootPanel.AddComponent<Outline>();
        _bgOutline.effectColor = new Color(0.85f, 0.72f, 0.38f, 0.55f); // Viền vàng kim Soulslike
        _bgOutline.effectDistance = new Vector2(1.2f, -1.2f);

        // --- 1. HEADER BAR ---
        GameObject headerObj = new GameObject("HeaderBar");
        headerObj.transform.SetParent(_rootPanel.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, 0f);
        headerRt.sizeDelta = new Vector2(0f, 26f);

        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = new Color(0.12f, 0.16f, 0.25f, 0.95f);
        headerBg.sprite = GetWhitePixelSprite();

        // Title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = new Vector2(8f, 0f);
        titleRt.sizeDelta = new Vector2(-40f, 0f);

        _headerTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        _headerTitleText.fontSize = 11.5f;
        _headerTitleText.fontStyle = FontStyles.Bold;
        _headerTitleText.color = new Color(0.96f, 0.84f, 0.45f, 1f);
        _headerTitleText.text = LocalizationManager.T("TỔ ĐỘI CO-OP (1/4)", "CO-OP PARTY (1/4)");
        _headerTitleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Nút thu gọn / mở rộng (Collapse/Expand)
        GameObject colBtnObj = new GameObject("CollapseBtn");
        colBtnObj.transform.SetParent(headerObj.transform, false);
        RectTransform colRt = colBtnObj.AddComponent<RectTransform>();
        colRt.anchorMin = new Vector2(1f, 0.5f);
        colRt.anchorMax = new Vector2(1f, 0.5f);
        colRt.pivot = new Vector2(1f, 0.5f);
        colRt.anchoredPosition = new Vector2(-4f, 0f);
        colRt.sizeDelta = new Vector2(24f, 20f);

        _collapseBtn = colBtnObj.AddComponent<Button>();
        Image colImg = colBtnObj.AddComponent<Image>();
        colImg.color = new Color(0.20f, 0.25f, 0.35f, 0.8f);
        colImg.sprite = GetWhitePixelSprite();

        GameObject colTxtObj = new GameObject("Text");
        colTxtObj.transform.SetParent(colBtnObj.transform, false);
        RectTransform colTxtRt = colTxtObj.AddComponent<RectTransform>();
        colTxtRt.anchorMin = Vector2.zero;
        colTxtRt.anchorMax = Vector2.one;
        colTxtRt.sizeDelta = Vector2.zero;

        _collapseBtnText = colTxtObj.AddComponent<TextMeshProUGUI>();
        _collapseBtnText.fontSize = 11f;
        _collapseBtnText.fontStyle = FontStyles.Bold;
        _collapseBtnText.color = Color.white;
        _collapseBtnText.text = "▲";
        _collapseBtnText.alignment = TextAlignmentOptions.Center;

        _collapseBtn.onClick.AddListener(ToggleCollapse);

        // --- 2. ROWS CONTAINER ---
        _rowsContainer = new GameObject("RowsContainer");
        _rowsContainer.transform.SetParent(_rootPanel.transform, false);
        _rowsContainerRt = _rowsContainer.AddComponent<RectTransform>();
        _rowsContainerRt.anchorMin = new Vector2(0f, 0f);
        _rowsContainerRt.anchorMax = new Vector2(1f, 1f);
        _rowsContainerRt.pivot = new Vector2(0.5f, 1f);
        _rowsContainerRt.anchoredPosition = new Vector2(0f, -26f);
        _rowsContainerRt.sizeDelta = new Vector2(0f, -26f);

        // Khởi tạo sẵn 2 hàng người chơi (Bản thân + Đồng đội 1)
        _playerRows.Clear();
        for (int i = 0; i < 2; i++)
        {
            _playerRows.Add(CreatePlayerRow(i));
        }

        GameFontManager.ApplyFontsGlobally(_rootPanel);
    }

    private PlayerRowUI CreatePlayerRow(int index)
    {
        PlayerRowUI row = new PlayerRowUI();

        float rowHeight = 44f;
        float posY = -(index * (rowHeight + 4f)) - 4f;

        GameObject rowObj = new GameObject($"PlayerRow_{index}");
        rowObj.transform.SetParent(_rowsContainer.transform, false);
        RectTransform rowRt = rowObj.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, posY);
        rowRt.sizeDelta = new Vector2(-8f, rowHeight);

        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.color = new Color(0.10f, 0.13f, 0.18f, 0.75f);
        rowBg.sprite = GetWhitePixelSprite();
        row.rowObj = rowObj;

        // 1. Avatar Icon (Tròn / Bo góc)
        GameObject avatarObj = new GameObject("Avatar");
        avatarObj.transform.SetParent(rowObj.transform, false);
        RectTransform avRt = avatarObj.AddComponent<RectTransform>();
        avRt.anchorMin = new Vector2(0f, 0.5f);
        avRt.anchorMax = new Vector2(0f, 0.5f);
        avRt.pivot = new Vector2(0f, 0.5f);
        avRt.anchoredPosition = new Vector2(4f, 0f);
        avRt.sizeDelta = new Vector2(34f, 34f);

        Image avImg = avatarObj.AddComponent<Image>();
        avImg.color = Color.white;
        avImg.preserveAspect = true;
        avImg.sprite = PlayerAvatarUI.GetHeroFaceSprite(CharacterManager.CHAR_VAELSTRIDER);
        row.avatarImg = avImg;

        // Vành viền avatar
        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(avatarObj.transform, false);
        RectTransform ringRt = ringObj.AddComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.sizeDelta = Vector2.zero;
        Image ringImg = ringObj.AddComponent<Image>();
        ringImg.sprite = PlayerAvatarUI.GetCircleRingSprite();
        ringImg.color = (index == 0) ? new Color(0.35f, 0.85f, 0.55f, 0.8f) : new Color(0.92f, 0.78f, 0.38f, 0.8f);

        // 2. Cột thông tin (Bên phải Avatar)
        float infoLeft = 42f;
        float infoWidth = 176f;

        // Dòng 1: Tên người chơi & Tag
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(rowObj.transform, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(infoLeft, -3f);
        nameRt.sizeDelta = new Vector2(-infoLeft - 82f, 18f);

        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.fontSize = 11.5f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        row.nameText = nameTmp;

        // Nút KÍCH (Kick) - Dành riêng cho Chủ Phòng trong trận
        GameObject kickObj = new GameObject("Btn_Kick");
        kickObj.transform.SetParent(rowObj.transform, false);
        RectTransform kickRt = kickObj.AddComponent<RectTransform>();
        kickRt.anchorMin = new Vector2(1f, 1f);
        kickRt.anchorMax = new Vector2(1f, 1f);
        kickRt.pivot = new Vector2(1f, 1f);
        kickRt.anchoredPosition = new Vector2(-4f, -2f);
        kickRt.sizeDelta = new Vector2(46f, 20f);
        Image kickImg = kickObj.AddComponent<Image>();
        kickImg.color = new Color(0.85f, 0.22f, 0.25f, 0.95f);
        kickImg.sprite = GetWhitePixelSprite();
        Button kickBtn = kickObj.AddComponent<Button>();
        row.kickBtn = kickBtn;

        GameObject kickTxtObj = new GameObject("Text");
        kickTxtObj.transform.SetParent(kickObj.transform, false);
        RectTransform ktRt = kickTxtObj.AddComponent<RectTransform>();
        ktRt.anchorMin = Vector2.zero;
        ktRt.anchorMax = Vector2.one;
        ktRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI ktTmp = kickTxtObj.AddComponent<TextMeshProUGUI>();
        ktTmp.fontSize = 9.5f;
        ktTmp.fontStyle = FontStyles.Bold;
        ktTmp.color = Color.white;
        ktTmp.text = "KÍCH";
        ktTmp.alignment = TextAlignmentOptions.Center;
        kickBtn.onClick.AddListener(() => OnKickTeammateClicked(index));
        kickObj.SetActive(false);

        // Nút KẾT BẠN (+) - Gửi lời mời kết bạn tức thì
        GameObject friendObj = new GameObject("Btn_AddFriend");
        friendObj.transform.SetParent(rowObj.transform, false);
        RectTransform friendRt = friendObj.AddComponent<RectTransform>();
        friendRt.anchorMin = new Vector2(1f, 1f);
        friendRt.anchorMax = new Vector2(1f, 1f);
        friendRt.pivot = new Vector2(1f, 1f);
        friendRt.anchoredPosition = new Vector2(-54f, -2f);
        friendRt.sizeDelta = new Vector2(22f, 20f);
        Image friendImg = friendObj.AddComponent<Image>();
        friendImg.color = new Color(0.18f, 0.55f, 0.95f, 0.95f);
        friendImg.sprite = GetWhitePixelSprite();
        Button friendBtn = friendObj.AddComponent<Button>();
        row.addFriendBtn = friendBtn;

        GameObject ftObj = new GameObject("Text");
        ftObj.transform.SetParent(friendObj.transform, false);
        RectTransform ftRt = ftObj.AddComponent<RectTransform>();
        ftRt.anchorMin = Vector2.zero;
        ftRt.anchorMax = Vector2.one;
        ftRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI ftTmp = ftObj.AddComponent<TextMeshProUGUI>();
        ftTmp.fontSize = 11f;
        ftTmp.fontStyle = FontStyles.Bold;
        ftTmp.color = Color.white;
        ftTmp.text = "+";
        ftTmp.alignment = TextAlignmentOptions.Center;
        friendBtn.onClick.AddListener(() => OnAddFriendClicked(index));
        friendObj.SetActive(false);

        // Dòng 2: Thanh máu mini (HP Bar)
        GameObject hpBgObj = new GameObject("HpBarBg");
        hpBgObj.transform.SetParent(rowObj.transform, false);
        RectTransform hpBgRt = hpBgObj.AddComponent<RectTransform>();
        hpBgRt.anchorMin = new Vector2(0f, 0f);
        hpBgRt.anchorMax = new Vector2(1f, 0f);
        hpBgRt.pivot = new Vector2(0f, 0f);
        hpBgRt.anchoredPosition = new Vector2(infoLeft, 5f);
        hpBgRt.sizeDelta = new Vector2(-infoLeft - 4f, 14f);

        Image hpBgImg = hpBgObj.AddComponent<Image>();
        hpBgImg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);
        hpBgImg.sprite = GetWhitePixelSprite();
        row.hpBarBg = hpBgImg;

        // HP Fill
        GameObject hpFillObj = new GameObject("HpBarFill");
        hpFillObj.transform.SetParent(hpBgObj.transform, false);
        RectTransform hpFillRt = hpFillObj.AddComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.sizeDelta = Vector2.zero;

        Image hpFillImg = hpFillObj.AddComponent<Image>();
        hpFillImg.type = Image.Type.Filled;
        hpFillImg.fillMethod = Image.FillMethod.Horizontal;
        hpFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        hpFillImg.color = new Color(0.18f, 0.80f, 0.44f, 1f);
        hpFillImg.sprite = GetWhitePixelSprite();
        row.hpBarFill = hpFillImg;

        // HP Text overlay (Hiển thị số máu 800/800 hoặc trạng thái)
        GameObject hpTxtObj = new GameObject("HpText");
        hpTxtObj.transform.SetParent(hpBgObj.transform, false);
        RectTransform hpTxtRt = hpTxtObj.AddComponent<RectTransform>();
        hpTxtRt.anchorMin = Vector2.zero;
        hpTxtRt.anchorMax = Vector2.one;
        hpTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI hpTmp = hpTxtObj.AddComponent<TextMeshProUGUI>();
        hpTmp.fontSize = 9.5f;
        hpTmp.fontStyle = FontStyles.Bold;
        hpTmp.color = Color.white;
        hpTmp.alignment = TextAlignmentOptions.Center;
        row.hpText = hpTmp;

        return row;
    }

    /// <summary>
    /// Thu gọn hoặc mở rộng danh sách đồng đội
    /// </summary>
    public void ToggleCollapse()
    {
        _isCollapsed = !_isCollapsed;
        SoundManager.EnsureInstance()?.PlayMenuHover();

        if (_isCollapsed)
        {
            _rowsContainer.SetActive(false);
            _collapseBtnText.text = "▼";
            _rootRt.sizeDelta = new Vector2(230f, 26f);
        }
        else
        {
            _rowsContainer.SetActive(true);
            _collapseBtnText.text = "▲";
            _rootRt.sizeDelta = new Vector2(230f, 124f);
        }
    }

    /// <summary>
    /// Cập nhật dữ liệu thời gian thực của Tổ đội
    /// </summary>
    public void RefreshPartyList()
    {
        if (_playerRows.Count < 2) return;

        bool isMeHost = (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsHost);

        // 1. CẬP NHẬT PLAYER 1 (BẢN THÂN)
        var row0 = _playerRows[0];
        if (row0 != null)
        {
            var acc = AccountDatabase.GetActiveAccount();
            string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName :
                            (acc != null && !string.IsNullOrEmpty(acc.username)) ? acc.username :
                            PlayerPrefs.GetString(AccountDatabase.PREF_CURRENT_USER, "HIỆP SĨ");
            if (string.IsNullOrEmpty(myName)) myName = "HIỆP SĨ";

            string myHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
            row0.avatarImg.sprite = PlayerAvatarUI.GetHeroFaceSprite(myHero);

            int myCurHp = PlayerHealth.Instance != null ? PlayerHealth.Instance.CurrentHealth : 100;
            int myMaxHp = PlayerHealth.Instance != null ? PlayerHealth.Instance.MaxHealth : 100;
            if (myMaxHp <= 0) myMaxHp = 100;

            string myHostTag = isMeHost ? "<color=#FFD700>👑</color> " : "";
            row0.nameText.text = $"{myHostTag}{myName} <color=#2ECC71>{LocalizationManager.T("(BẠN)", "(YOU)")}</color>";
            float pct = Mathf.Clamp01((float)myCurHp / myMaxHp);
            row0.targetFill = pct;

            if (row0.addFriendBtn != null) row0.addFriendBtn.gameObject.SetActive(false);
            if (row0.kickBtn != null) row0.kickBtn.gameObject.SetActive(false);

            if (myCurHp <= 0)
            {
                row0.hpBarFill.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                row0.hpText.text = $"<color=#E74C3C>{LocalizationManager.T("HẠ GỤC (K.O)", "DOWN (K.O)")}</color>";
            }
            else
            {
                row0.hpBarFill.color = GetHpBarColor(pct);
                row0.hpText.text = $"{myCurHp} / {myMaxHp}";
            }
            row0.rowObj.SetActive(true);
        }

        // 2. CẬP NHẬT PLAYER 2 (ĐỒNG ĐỘI / BOT / OPPONENT)
        var row1 = _playerRows[1];
        if (row1 != null)
        {
            bool hasTeammate = (PvPNetworkManager.Instance != null && (PvPNetworkManager.Instance.HasOpponentJoined || PvPNetworkManager.Instance.IsBotPracticeMode || PvPNetworkManager.Instance.GetConnectedPlayerCount() >= 2))
                               || (PvPRemoteOpponent.Instance != null);

            int totalPlayers = hasTeammate ? 2 : 1;
            _headerTitleText.text = LocalizationManager.T($"TỔ ĐỘI CO-OP ({totalPlayers}/4)", $"CO-OP PARTY ({totalPlayers}/4)");

            if (hasTeammate)
            {
                string teammateName = "";
                string teammateHero = CharacterManager.CHAR_VAELSTRIDER;
                int oppCurHp = 100;
                int oppMaxHp = 100;

                if (PvPRemoteOpponent.Instance != null)
                {
                    teammateName = PvPRemoteOpponent.Instance.OpponentName;
                    teammateHero = PvPRemoteOpponent.Instance.OpponentHeroId;
                    oppCurHp = PvPRemoteOpponent.Instance.CurrentHealth;
                    oppMaxHp = PvPRemoteOpponent.Instance.MaxHealth;
                }
                else if (PvPNetworkManager.Instance != null)
                {
                    teammateName = PvPNetworkManager.Instance.OpponentName;
                    teammateHero = PvPNetworkManager.Instance.OpponentHeroId;
                    oppCurHp = PvPNetworkManager.Instance.OpponentCurrentHp;
                    oppMaxHp = PvPNetworkManager.Instance.OpponentMaxHp;
                }

                if (string.IsNullOrEmpty(teammateName)) teammateName = LocalizationManager.T("ĐỒNG ĐỘI 1", "TEAMMATE 1");
                teammateName = teammateName.Replace("[", "(").Replace("]", ")");
                
                // Chuẩn hóa MaxHP trong Co-op về đúng chỉ số gốc của Tướng (VD: Pháp Sư 90/90, Hiệp Sĩ 100/100)
                int heroBaseHp = CharacterManager.GetBaseHealth(teammateHero);
                if (heroBaseHp > 0 && (!PvPMatchManager.IsPvPModeActive || oppMaxHp >= 500))
                {
                    oppMaxHp = heroBaseHp;
                }
                if (oppMaxHp <= 0) oppMaxHp = 100;
                oppCurHp = Mathf.Clamp(oppCurHp, 0, oppMaxHp);

                row1.avatarImg.sprite = PlayerAvatarUI.GetHeroFaceSprite(teammateHero);

                bool isTeammateHost = (PvPNetworkManager.Instance != null && !PvPNetworkManager.Instance.IsHost);
                string teamHostTag = isTeammateHost ? "<color=#FFD700>👑</color> " : "";
                row1.nameText.text = $"{teamHostTag}{teammateName}";

                bool isFriend = FriendManager.EnsureInstance()?.IsFriend(teammateName) ?? false;
                bool isBot = (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsBotPracticeMode);
                if (row1.addFriendBtn != null) row1.addFriendBtn.gameObject.SetActive(!isFriend && !isBot);
                if (row1.kickBtn != null) row1.kickBtn.gameObject.SetActive(isMeHost && !isBot);

                float pct = Mathf.Clamp01((float)oppCurHp / oppMaxHp);
                row1.targetFill = pct;

                if (oppCurHp <= 0)
                {
                    row1.hpBarFill.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                    row1.hpText.text = $"<color=#E74C3C>{LocalizationManager.T("HẠ GỤC (K.O)", "DOWN (K.O)")}</color>";
                }
                else
                {
                    row1.hpBarFill.color = GetHpBarColor(pct);
                    row1.hpText.text = $"{oppCurHp} / {oppMaxHp}";
                }

                row1.rowObj.SetActive(true);
            }
            else
            {
                // Chưa có đồng đội vào phòng: Hiển thị slot chờ (Tuyệt đối không dùng ký tự [ ] vì font pixel map thành chữ z)
                row1.avatarImg.sprite = GetCircleFillSprite();
                row1.avatarImg.color = new Color(0.3f, 0.35f, 0.45f, 0.5f);
                row1.nameText.text = $"<color=#7F8C8D>{LocalizationManager.T("Đang chờ đồng đội...", "Waiting for teammate...")}</color>";
                row1.targetFill = 0f;
                row1.hpBarFill.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                row1.hpText.text = $"<color=#BDC3C7>{LocalizationManager.T("TRỐNG (2/4)", "EMPTY (2/4)")}</color>";
                if (row1.addFriendBtn != null) row1.addFriendBtn.gameObject.SetActive(false);
                if (row1.kickBtn != null) row1.kickBtn.gameObject.SetActive(false);
                row1.rowObj.SetActive(true);
            }
        }
    }

    private void OnKickTeammateClicked(int index)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (PvPNetworkManager.Instance == null || !PvPNetworkManager.Instance.IsHost) return;
        string target = !string.IsNullOrEmpty(PvPNetworkManager.Instance.OpponentName) ? PvPNetworkManager.Instance.OpponentName : "Đồng đội";
        PvPNetworkManager.Instance.KickPlayer(target);
        if (InGameTeamChatUI.Instance != null)
        {
            InGameTeamChatUI.Instance.HandleInGameChatMessage("HỆ THỐNG", LocalizationManager.T($"Đã kích {target} khỏi trận!", $"Kicked {target} from match!"));
        }
    }

    private void OnAddFriendClicked(int index)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string target = "";
        if (PvPRemoteOpponent.Instance != null) target = PvPRemoteOpponent.Instance.OpponentName;
        else if (PvPNetworkManager.Instance != null) target = PvPNetworkManager.Instance.OpponentName;
        if (string.IsNullOrEmpty(target)) return;

        FriendManager.EnsureInstance()?.SendFriendRequest(target);
        if (InGameTeamChatUI.Instance != null)
        {
            InGameTeamChatUI.Instance.HandleInGameChatMessage("BẠN BÈ", LocalizationManager.T($"Đã gửi lời mời kết bạn tới {target}!", $"Sent friend request to {target}!"));
        }
    }

    private Color GetHpBarColor(float pct)
    {
        if (pct > 0.50f)
            return new Color(0.18f, 0.80f, 0.44f, 1f); // Xanh ngọc tươi
        else if (pct > 0.25f)
            return new Color(0.95f, 0.61f, 0.07f, 1f); // Cam / Hổ phách
        else
            return new Color(0.91f, 0.30f, 0.24f, 1f); // Đỏ nguy cấp
    }

    public static Sprite GetWhitePixelSprite()
    {
        if (_whitePixelSprite != null) return _whitePixelSprite;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Color[] cols = new Color[4] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(cols);
        tex.Apply();
        _whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        return _whitePixelSprite;
    }

    public static Sprite GetCircleFillSprite()
    {
        if (_circleFillSprite != null) return _circleFillSprite;
        return PlayerAvatarUI.GetCircleFillSprite();
    }
}
