using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Hộp Thoại Lời Mời Vào Phòng Co-op Toàn Cục (Single Unified Invite Popup).
/// Đảm bảo:
/// 1. Chỉ duy nhất 1 popup hiển thị trên toàn game (không bao giờ bị trùng 2 popup).
/// 2. Xuất hiện ngay lập tức tại Màn Hình Sảnh Chính (MainMenuScene) mà không cần phải bấm vào phòng trước.
/// 3. Bấm "VÀO PHÒNG NGAY" tự động mở Sảnh Co-op và kết nối thẳng vào phòng.
/// </summary>
public class RoomInviteGlobalPopup : MonoBehaviour
{
    private static RoomInviteGlobalPopup _instance;
    public static RoomInviteGlobalPopup Instance => _instance;

    private GameObject _dialogObj;
    private TextMeshProUGUI _promptText;
    private string _pendingRoomPin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (_instance == null)
        {
            var go = new GameObject("RoomInviteGlobalPopup_Singleton");
            _instance = go.AddComponent<RoomInviteGlobalPopup>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Luôn nổi lên trên cùng (kể cả Sảnh Chính hay Sảnh Chờ)

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        _dialogObj = new GameObject("RoomInviteDialog", typeof(RectTransform));
        _dialogObj.transform.SetParent(transform, false);
        var dRt = _dialogObj.GetComponent<RectTransform>();
        dRt.anchorMin = Vector2.zero;
        dRt.anchorMax = Vector2.one;
        dRt.sizeDelta = Vector2.zero;

        // Backdrop làm mờ phía sau
        var bgObj = new GameObject("DimBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        bgObj.transform.SetParent(_dialogObj.transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        var bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);

        // Khung Modal chính giữa màn hình (Soulslike Dark Fantasy)
        var panel = new GameObject("PanelBox", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_dialogObj.transform, false);
        var pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.sizeDelta = new Vector2(620f, 330f);
        var pImg = panel.GetComponent<Image>();
        pImg.color = new Color(0.10f, 0.11f, 0.18f, 0.98f);
        SoulslikeHUDStyler.StyleModalWindow(panel.transform);

        // Tiêu đề
        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panel.transform, false);
        var tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 105f);
        tRt.sizeDelta = new Vector2(560f, 40f);
        var tText = titleObj.GetComponent<TextMeshProUGUI>();
        tText.text = LocalizationManager.T("LỜI MỜI THAM CHIẾN CO-OP", "CO-OP INVITATION");
        tText.fontSize = 24f;
        tText.fontStyle = FontStyles.Bold;
        tText.alignment = TextAlignmentOptions.Center;
        tText.color = new Color(1f, 0.85f, 0.35f, 1f);

        // Nội dung chi tiết
        var promptObj = new GameObject("Prompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        promptObj.transform.SetParent(panel.transform, false);
        var prRt = promptObj.GetComponent<RectTransform>();
        prRt.anchoredPosition = new Vector2(0f, 25f);
        prRt.sizeDelta = new Vector2(540f, 95f);
        _promptText = promptObj.GetComponent<TextMeshProUGUI>();
        _promptText.fontSize = 18f;
        _promptText.alignment = TextAlignmentOptions.Center;
        _promptText.color = new Color(0.92f, 0.95f, 1f, 1f);

        // Nút Vào Phòng Ngay (Xanh lá)
        CreateButton(panel.transform, "Btn_Accept",
            LocalizationManager.T("VÀO PHÒNG NGAY", "JOIN NOW"),
            new Vector2(-120f, -80f), new Vector2(210f, 52f),
            new Color(0.18f, 0.65f, 0.32f, 1f), OnAcceptClicked);

        // Nút Từ Chối (Đỏ sẫm)
        CreateButton(panel.transform, "Btn_Decline",
            LocalizationManager.T("TỪ CHỐI", "DECLINE"),
            new Vector2(120f, -80f), new Vector2(160f, 52f),
            new Color(0.55f, 0.22f, 0.25f, 1f), OnDeclineClicked);

        _dialogObj.SetActive(false);
    }

    private GameObject CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        var btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        var t = textObj.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = 16f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;

        SoulslikeHUDStyler.StyleButton(btn, text, Color.white);
        return btnObj;
    }

    public static void Show(string senderName, string roomPin)
    {
        if (string.IsNullOrEmpty(roomPin)) return;
        if (_instance == null) AutoInit();
        if (_instance != null)
        {
            _instance.OpenPopup(senderName, roomPin);
        }
    }

    public void OpenPopup(string senderName, string roomPin)
    {
        _pendingRoomPin = roomPin;
        if (_dialogObj == null) BuildUI();

        if (_promptText != null)
        {
            _promptText.text = LocalizationManager.T(
                $"<color=#FFD54F><b>{senderName}</b></color> đang mời bạn cùng xuất trận Co-op tại Phòng <color=#00E5FF><b>#{roomPin}</b></color>!\nBạn có muốn tham gia ngay không?",
                $"<color=#FFD54F><b>{senderName}</b></color> invited you to Co-op Room <color=#00E5FF><b>#{roomPin}</b></color>!\nDo you want to join now?");
        }

        if (_dialogObj != null)
        {
            _dialogObj.SetActive(true);
            _dialogObj.transform.SetAsLastSibling();
            SoundManager.EnsureInstance()?.PlayMenuHover();
        }
    }

    private void OnAcceptClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_dialogObj != null) _dialogObj.SetActive(false);
        if (!string.IsNullOrEmpty(_pendingRoomPin))
        {
            PvPLobbyUI.AcceptRoomInviteAndJoin(_pendingRoomPin);
        }
    }

    private void OnDeclineClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_dialogObj != null) _dialogObj.SetActive(false);
        if (!string.IsNullOrEmpty(_pendingRoomPin))
        {
            FriendManager.EnsureInstance()?.RemoveRoomInvite(_pendingRoomPin);
        }
    }
}
