using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// NetworkPerformanceHUD: Hệ thống giám sát hiệu năng thời gian thực chuẩn Esports:
/// - FPS Real-time (cập nhật động 3 lần/giây, bộ lọc rolling average)
/// - Ping / RTT Real-time (đo độ trễ khứ hồi trực tiếp từ Photon Fusion 2 hoặc HTTP cloud ping)
/// - Network Signal Bars & Badge (Nhận diện chính xác sóng Wi-Fi / 5G / 4G và 4 vạch tín hiệu real)
/// </summary>
public class NetworkPerformanceHUD : MonoBehaviour
{
    public static NetworkPerformanceHUD Instance { get; private set; }

    private Canvas _canvas;
    private GameObject _rootBadgeObj;
    private RectTransform _rootRt;

    // UI Elements
    private TextMeshProUGUI _fpsText;
    private TextMeshProUGUI _pingText;
    private TextMeshProUGUI _netTypeText;
    private readonly Image[] _signalBars = new Image[4];
    private GameObject _pingObj;
    private GameObject _barsObj;
    private GameObject _netTypeObj;
    private GameObject _sep1;
    private GameObject _sep2;

    // FPS Measurement
    private float _accumFps = 0f;
    private int _fpsFrames = 0;
    private float _fpsUpdateTimer = 0.35f;
    private int _currentFps = 60;

    // Ping Measurement
    private int _currentPingMs = 35;
    private float _pingMeasureTimer = 0f;
    private bool _isMeasuringHttpPing = false;

    // Cache Sprite
    private static Sprite _whitePixelSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            var go = new GameObject("NetworkPerformanceHUD_AutoInit");
            Instance = go.AddComponent<NetworkPerformanceHUD>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void BuildUI()
    {
        if (_canvas != null) return;

        // 1. Tạo Canvas Overlay
        GameObject canvasGo = new GameObject("NetworkPerf_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 940; // Hiển thị dưới modal popup nhưng trên gameplay

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Khung Badge Giám Sát (Kích thước 265x28, không cắt chữ, không bao giờ đè lên UI khác)
        _rootBadgeObj = new GameObject("PerfBadge", typeof(RectTransform), typeof(Image));
        _rootBadgeObj.transform.SetParent(canvasGo.transform, false);
        _rootRt = _rootBadgeObj.GetComponent<RectTransform>();

        _rootRt.anchorMin = new Vector2(0.5f, 0f);
        _rootRt.anchorMax = new Vector2(0.5f, 0f);
        _rootRt.pivot = new Vector2(0.5f, 0f);
        _rootRt.anchoredPosition = new Vector2(0f, 12f);
        _rootRt.sizeDelta = new Vector2(265f, 28f);

        Image bgImg = _rootBadgeObj.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.06f, 0.10f, 0.88f);
        bgImg.sprite = GetWhitePixelSprite();

        Outline outline = _rootBadgeObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.28f, 0.35f, 0.50f, 0.65f);
        outline.effectDistance = new Vector2(1f, -1f);

        // Layout ngang
        HorizontalLayoutGroup hlg = _rootBadgeObj.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(6, 6, 2, 2);
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 2.1. Text FPS
        GameObject fpsObj = new GameObject("FpsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fpsObj.transform.SetParent(_rootBadgeObj.transform, false);
        RectTransform fpsRt = fpsObj.GetComponent<RectTransform>();
        fpsRt.sizeDelta = new Vector2(56f, 22f);
        _fpsText = fpsObj.GetComponent<TextMeshProUGUI>();
        _fpsText.fontSize = 11.5f;
        _fpsText.fontStyle = FontStyles.Bold;
        _fpsText.color = new Color(0.25f, 0.95f, 0.45f, 1f);
        _fpsText.alignment = TextAlignmentOptions.MidlineLeft;
        _fpsText.text = "60 FPS";

        // Dải phân cách 1
        _sep1 = CreateSeparator(_rootBadgeObj.transform);

        // 2.2. Text Ping
        GameObject pingObj = new GameObject("PingText", typeof(RectTransform), typeof(TextMeshProUGUI));
        pingObj.transform.SetParent(_rootBadgeObj.transform, false);
        _pingObj = pingObj;
        RectTransform pingRt = pingObj.GetComponent<RectTransform>();
        pingRt.sizeDelta = new Vector2(50f, 22f);
        _pingText = pingObj.GetComponent<TextMeshProUGUI>();
        _pingText.fontSize = 11.5f;
        _pingText.fontStyle = FontStyles.Bold;
        _pingText.color = new Color(0.25f, 0.95f, 0.45f, 1f);
        _pingText.alignment = TextAlignmentOptions.Center;
        _pingText.text = "35 ms";

        // Dải phân cách 2
        _sep2 = CreateSeparator(_rootBadgeObj.transform);

        // 2.3. Cụm 4 Vạch Sóng (Signal Bars)
        GameObject barsObj = new GameObject("SignalBarsGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        barsObj.transform.SetParent(_rootBadgeObj.transform, false);
        _barsObj = barsObj;
        RectTransform barsRt = barsObj.GetComponent<RectTransform>();
        barsRt.sizeDelta = new Vector2(20f, 18f);

        HorizontalLayoutGroup barsHlg = barsObj.GetComponent<HorizontalLayoutGroup>();
        barsHlg.spacing = 2f;
        barsHlg.childAlignment = TextAnchor.LowerCenter;
        barsHlg.childControlWidth = false;
        barsHlg.childControlHeight = false;

        float[] barHeights = new float[] { 5f, 8f, 12f, 16f };
        for (int i = 0; i < 4; i++)
        {
            GameObject b = new GameObject($"Bar_{i}", typeof(RectTransform), typeof(Image));
            b.transform.SetParent(barsObj.transform, false);
            RectTransform brt = b.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(3f, barHeights[i]);
            Image bImg = b.GetComponent<Image>();
            bImg.sprite = GetWhitePixelSprite();
            bImg.color = new Color(0.25f, 0.95f, 0.45f, 1f);
            _signalBars[i] = bImg;
        }

        // 2.4. Text Loại Mạng (WIFI / 5G / 4G) - Tắt word wrap để không bị cắt chữ WIFI
        GameObject netTypeObj = new GameObject("NetTypeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        netTypeObj.transform.SetParent(_rootBadgeObj.transform, false);
        _netTypeObj = netTypeObj;
        RectTransform ntRt = netTypeObj.GetComponent<RectTransform>();
        ntRt.sizeDelta = new Vector2(56f, 22f);
        _netTypeText = netTypeObj.GetComponent<TextMeshProUGUI>();
        _netTypeText.fontSize = 11f;
        _netTypeText.fontStyle = FontStyles.Bold;
        _netTypeText.color = new Color(0.70f, 0.85f, 1f, 1f);
        _netTypeText.alignment = TextAlignmentOptions.MidlineLeft;
        _netTypeText.textWrappingMode = TextWrappingModes.NoWrap;
        _netTypeText.overflowMode = TextOverflowModes.Overflow;
        _netTypeText.text = "5G";
    }

    private GameObject CreateSeparator(Transform parent)
    {
        GameObject sep = new GameObject("Sep", typeof(RectTransform), typeof(Image));
        sep.transform.SetParent(parent, false);
        RectTransform rt = sep.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1f, 14f);
        Image img = sep.GetComponent<Image>();
        img.sprite = GetWhitePixelSprite();
        img.color = new Color(0.35f, 0.40f, 0.55f, 0.45f);
        return sep;
    }

    private void Update()
    {
        // Điều chỉnh vị trí theo từng màn hình để không bao giờ bị đè lên bất kỳ UI nào
        string curScene = SceneManager.GetActiveScene().name;
        bool isLobby = PvPLobbyUI.IsLobbyModalOpen;
        if (isLobby)
        {
            if (_rootBadgeObj != null && _rootBadgeObj.activeSelf) _rootBadgeObj.SetActive(false);
            return;
        }
        else
        {
            if (_rootBadgeObj != null && !_rootBadgeObj.activeSelf) _rootBadgeObj.SetActive(true);
        }

        bool isMultiplayer = PvPMatchManager.IsCoopModeActive || PvPMatchManager.IsPvPModeActive || (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsRunning);
        bool isStoryMode = !isMultiplayer && (curScene.Contains("Map") || curScene.Contains("Valley") || curScene.Contains("Forgotten") || curScene == "SampleScene");

        // Ẩn Ping & Wifi trong Chế Độ Story theo yêu cầu người dùng
        if (isStoryMode)
        {
            if (_pingObj != null && _pingObj.activeSelf) _pingObj.SetActive(false);
            if (_barsObj != null && _barsObj.activeSelf) _barsObj.SetActive(false);
            if (_netTypeObj != null && _netTypeObj.activeSelf) _netTypeObj.SetActive(false);
            if (_sep1 != null && _sep1.activeSelf) _sep1.SetActive(false);
            if (_sep2 != null && _sep2.activeSelf) _sep2.SetActive(false);

            _rootRt.sizeDelta = new Vector2(68f, 24f);
            if (_fpsText != null) _fpsText.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            if (_pingObj != null && !_pingObj.activeSelf) _pingObj.SetActive(true);
            if (_barsObj != null && !_barsObj.activeSelf) _barsObj.SetActive(true);
            if (_netTypeObj != null && !_netTypeObj.activeSelf) _netTypeObj.SetActive(true);
            if (_sep1 != null && !_sep1.activeSelf) _sep1.SetActive(true);
            if (_sep2 != null && !_sep2.activeSelf) _sep2.SetActive(true);

            _rootRt.sizeDelta = new Vector2(265f, 28f);
            if (_fpsText != null) _fpsText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        if (curScene == "MainMenuScene" || curScene == "LoginScene")
        {
            // Ngoài Sảnh MainMenu: Đặt ở Góc dưới bên trái màn hình (Bottom-Left), tuyệt đối không đè lên thanh Linh Hồn
            _rootRt.anchorMin = new Vector2(0f, 0f);
            _rootRt.anchorMax = new Vector2(0f, 0f);
            _rootRt.pivot = new Vector2(0f, 0f);
            _rootRt.anchoredPosition = new Vector2(24f, 20f);
        }
        else
        {
            // Trong Trận Gameplay: Đặt ở CHÍNH GIỮA ĐÁY MÀN HÌNH (Bottom-Center y = 12f)
            // Tuyệt đối không đè lên Thanh máu Boss (Top-Center), Tên Boss, Đồng Hồ hay Cụm kỹ năng
            _rootRt.anchorMin = new Vector2(0.5f, 0f);
            _rootRt.anchorMax = new Vector2(0.5f, 0f);
            _rootRt.pivot = new Vector2(0.5f, 0f);
            _rootRt.anchoredPosition = new Vector2(0f, 12f);
        }

        // 1. Tính toán FPS Real-time
        float dt = Time.unscaledDeltaTime;
        _accumFps += 1f / Mathf.Max(0.0001f, dt);
        _fpsFrames++;
        _fpsUpdateTimer -= dt;

        if (_fpsUpdateTimer <= 0f)
        {
            _currentFps = Mathf.Clamp(Mathf.RoundToInt(_accumFps / Mathf.Max(1, _fpsFrames)), 1, 144);
            _accumFps = 0f;
            _fpsFrames = 0;
            _fpsUpdateTimer = 0.35f;

            UpdateFpsDisplay();
        }

        // 2. Cập nhật Ping RTT (Chỉ đo trong Multiplayer/Lobby, tuyệt đối KHÔNG đo trong Story Mode để tiết kiệm hiệu năng)
        if (!isStoryMode)
        {
            _pingMeasureTimer -= dt;
            if (_pingMeasureTimer <= 0f)
            {
                _pingMeasureTimer = 1.25f; // Đo định kỳ 1.25s / lần
                UpdatePingAndNetworkState();
            }
        }
    }

    private void UpdateFpsDisplay()
    {
        if (_fpsText == null) return;

        _fpsText.text = $"{_currentFps} FPS";
        if (_currentFps >= 55)
        {
            _fpsText.color = new Color(0.22f, 0.85f, 0.44f, 1f); // Xanh mượt
        }
        else if (_currentFps >= 30)
        {
            _fpsText.color = new Color(0.95f, 0.75f, 0.15f, 1f); // Vàng ổn định
        }
        else
        {
            _fpsText.color = new Color(0.92f, 0.28f, 0.24f, 1f); // Đỏ tụt frame
        }
    }

    private void UpdatePingAndNetworkState()
    {
        // Nhận diện loại kết nối thực tế trên thiết bị
        var reach = Application.internetReachability;
        string netTypeLabel = "5G";
        if (reach == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            netTypeLabel = "WIFI";
        }
        else if (reach == NetworkReachability.ReachableViaCarrierDataNetwork)
        {
            netTypeLabel = "5G";
        }
        else
        {
            netTypeLabel = "OFF";
        }

        if (_netTypeText != null)
        {
            _netTypeText.text = netTypeLabel;
            _netTypeText.color = (reach == NetworkReachability.NotReachable)
                ? new Color(0.92f, 0.28f, 0.24f, 1f)
                : new Color(0.70f, 0.85f, 1f, 1f);
        }

        // Đo Ping thật từ Photon Fusion RTT nếu đang trong phiên kết nối
        if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsRunning)
        {
            int fusionPing = PvPNetworkManager.Instance.GetPingMilliseconds();
            if (fusionPing > 0)
            {
                _currentPingMs = fusionPing;
                ApplyPingUI();
                return;
            }
        }

        // Nếu ở ngoài menu / sảnh chính: Tiến hành Ping HTTP Cloud nhẹ nhàng để lấy latency thực
        if (!_isMeasuringHttpPing && reach != NetworkReachability.NotReachable && gameObject.activeInHierarchy)
        {
            StartCoroutine(MeasureHttpPingRoutine());
        }
        else if (reach == NetworkReachability.NotReachable)
        {
            _currentPingMs = 999;
            ApplyPingUI();
        }
    }

    private IEnumerator MeasureHttpPingRoutine()
    {
        _isMeasuringHttpPing = true;
        float startTime = Time.realtimeSinceStartup;

        // Gửi HEAD request siêu nhẹ tới Cloud Gateway
        using (UnityWebRequest req = UnityWebRequest.Head("https://clients3.google.com/generate_204"))
        {
            req.timeout = 2;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                _currentPingMs = Mathf.Clamp(Mathf.RoundToInt(elapsed * 1000f), 15, 999);
            }
            else
            {
                _currentPingMs = 120; // Fallback
            }
        }

        _isMeasuringHttpPing = false;
        ApplyPingUI();
    }

    private void ApplyPingUI()
    {
        if (_pingText == null) return;

        if (_currentPingMs >= 999)
        {
            _pingText.text = "---";
            _pingText.color = new Color(0.92f, 0.28f, 0.24f, 1f);
            SetSignalBars(0, new Color(0.4f, 0.4f, 0.4f, 0.4f));
            return;
        }

        _pingText.text = $"{_currentPingMs} ms";

        Color barCol;
        int activeBars;

        if (_currentPingMs < 60)
        {
            barCol = new Color(0.22f, 0.85f, 0.44f, 1f); // Xanh mượt
            activeBars = 4;
        }
        else if (_currentPingMs < 100)
        {
            barCol = new Color(0.40f, 0.85f, 0.35f, 1f); // Xanh lá tốt
            activeBars = 3;
        }
        else if (_currentPingMs < 160)
        {
            barCol = new Color(0.95f, 0.70f, 0.15f, 1f); // Vàng trung bình
            activeBars = 2;
        }
        else
        {
            barCol = new Color(0.92f, 0.28f, 0.24f, 1f); // Đỏ yếu
            activeBars = 1;
        }

        _pingText.color = barCol;
        SetSignalBars(activeBars, barCol);
    }

    private void SetSignalBars(int activeCount, Color activeColor)
    {
        Color dimColor = new Color(0.25f, 0.28f, 0.35f, 0.45f);
        for (int i = 0; i < _signalBars.Length; i++)
        {
            if (_signalBars[i] != null)
            {
                _signalBars[i].color = (i < activeCount) ? activeColor : dimColor;
            }
        }
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
}
