using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// HUD góc màn hình hiển thị Level và thanh EXP trong trận.
/// Thiết kế thanh mảnh, đồng bộ pixel-perfect với thanh Máu và Năng Lượng phía trên.
/// Vị trí nằm ngay dưới thanh Năng Lượng và TRÊN biểu tượng Linh Hồn.
/// </summary>
public class PlayerExpBarUI : MonoBehaviour
{
    private static PlayerExpBarUI _instance;
    public static PlayerExpBarUI Instance => _instance;

    public static PlayerExpBarUI EnsureUIExists()
    {
        if (_instance != null) return _instance;
        var existing = FindAnyObjectByType<PlayerExpBarUI>();
        if (existing != null) { _instance = existing; return existing; }
        var go = new GameObject("PlayerExpBarUI");
        _instance = go.AddComponent<PlayerExpBarUI>();
        return _instance;
    }

    // UI refs
    private GameObject _root;
    private Slider _expSlider;
    private Image _expFill;
    private TextMeshProUGUI _expLabelText;

    private int _cachedLevel = 1;
    private int _cachedExp = 0;
    private int _cachedReq = 8;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        BuildUI();
        SubscribeEvents();
        RepositionShardsCounter();

        int lv = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1;
        int exp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentExp : 0;
        int req = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.ExpRequired : 4;
        UpdateBar(exp, req);
        UpdateLevelText(lv);
    }

    void Update()
    {
        AlignWithHealthBar();
    }

    public void AlignWithHealthBar()
    {
        if (_root == null) return;
        Canvas mainCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (mainCanvas == null) mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas == null) return;

        Transform healthBar = mainCanvas.transform.Find("PlayerHealthBar");
        if (healthBar == null) healthBar = mainCanvas.transform.Find("HealthBar");
        if (healthBar == null) return;

        RectTransform hbRt = healthBar.GetComponent<RectTransform>();
        RectTransform rootRt = _root.GetComponent<RectTransform>();
        if (hbRt != null && rootRt != null)
        {
            float startX = hbRt.anchoredPosition.x - (hbRt.sizeDelta.x * hbRt.pivot.x);
            float startY = hbRt.anchoredPosition.y - (hbRt.sizeDelta.y * (1f - hbRt.pivot.y)) - 3f;
            float expOffsetY = startY - 38f;
            float barWidth = Mathf.Max(260f, hbRt.sizeDelta.x - 30f);
            if (Mathf.Abs(rootRt.anchoredPosition.x - startX) > 0.5f || 
                Mathf.Abs(rootRt.anchoredPosition.y - expOffsetY) > 0.5f ||
                Mathf.Abs(rootRt.sizeDelta.x - barWidth) > 0.5f ||
                Mathf.Abs(rootRt.sizeDelta.y - 14f) > 0.5f)
            {
                rootRt.anchoredPosition = new Vector2(startX, expOffsetY);
                rootRt.sizeDelta = new Vector2(barWidth, 14f);
            }
        }
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        UnsubscribeEvents();
    }

    public void SetVisible(bool visible)
    {
        if (_root != null && _root.activeSelf != visible)
        {
            _root.SetActive(visible);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildUI();
        RepositionShardsCounter();
        int lv = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1;
        int exp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentExp : 0;
        int req = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.ExpRequired : 4;
        UpdateBar(exp, req);
        UpdateLevelText(lv);
    }

    private void SubscribeEvents()
    {
        PlayerLevelSystem.OnExpChanged += UpdateBar;
        PlayerLevelSystem.OnLevelUp += UpdateLevelText;
    }

    private void UnsubscribeEvents()
    {
        PlayerLevelSystem.OnExpChanged -= UpdateBar;
        PlayerLevelSystem.OnLevelUp -= UpdateLevelText;
    }

    public static void RepositionShardsCounter()
    {
        var shardsUI = FindAnyObjectByType<ShardsUI>();
        if (shardsUI != null)
        {
            RectTransform scRt = shardsUI.GetComponent<RectTransform>();
            if (scRt != null)
            {
                scRt.anchoredPosition = new Vector2(175f, -118f);
                scRt.sizeDelta = new Vector2(120f, 28f);
            }
        }
    }

    private void BuildUI()
    {
        if (_root != null) Destroy(_root);

        Canvas mainCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (mainCanvas == null) mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas == null) return;

        // Tính toán tọa độ căn chỉnh chính xác theo thanh Máu & Năng Lượng
        Transform healthBar = mainCanvas.transform.Find("PlayerHealthBar");
        if (healthBar == null) healthBar = mainCanvas.transform.Find("HealthBar");

        float startX = 20f;
        float startY = -43f;
        float barWidth = 220f;

        if (healthBar != null)
        {
            RectTransform hbRt = healthBar.GetComponent<RectTransform>();
            if (hbRt != null)
            {
                startX = hbRt.anchoredPosition.x - (hbRt.sizeDelta.x * hbRt.pivot.x);
                startY = hbRt.anchoredPosition.y - (hbRt.sizeDelta.y * (1f - hbRt.pivot.y)) - 3f;
                barWidth = Mathf.Max(260f, hbRt.sizeDelta.x - 30f);
            }
        }

        // Đặt ngay dưới 2 thanh năng lượng Soulslike (EnergyBar 18px + InnerEnergyBar 14px + đệm = 38px)
        float expOffsetY = startY - 38f;
        float expBarHeight = 14f;

        _root = new GameObject("PlayerExpBarHUD");
        _root.transform.SetParent(mainCanvas.transform, false);

        var rootRt = _root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);
        rootRt.anchoredPosition = new Vector2(startX, expOffsetY);
        rootRt.sizeDelta = new Vector2(barWidth, expBarHeight);

        // Nền rãnh đá Obsidian rèn chỉ đồng cổ tím sâu thẳm rõ ràng
        var bgImg = _root.AddComponent<Image>();
        bgImg.sprite = SoulslikeHUDStyler.GetObsidianTroughSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.35f, 0.25f, 0.45f, 1f);

        // Viền thạch anh tím rõ rệt giúp thanh EXP luôn nổi bật trên nền đá
        var outline = _root.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.40f, 0.75f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);

        // Slider component
        _expSlider = _root.AddComponent<Slider>();
        _expSlider.interactable = false;
        _expSlider.transition = Selectable.Transition.None;

        // Fill Area
        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(_root.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        // Fill
        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        _expFill = fill.AddComponent<Image>();
        _expFill.sprite = SoulslikeHUDStyler.GetAmethystFillSprite();
        _expFill.type = Image.Type.Sliced;
        _expFill.color = new Color(0.92f, 0.65f, 1.0f, 1f); // Tím pha lê rực rỡ
        _expSlider.fillRect = fillRt;

        // Label Text hiển thị góc phải: "0 / 4 XP"
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(_root.transform, false);
        var textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 0f);
        textRt.offsetMax = new Vector2(-10f, 0f);

        _expLabelText = textObj.AddComponent<TextMeshProUGUI>();
        _expLabelText.fontSize = 13f;
        _expLabelText.fontStyle = FontStyles.Bold;
        _expLabelText.color = new Color(0.98f, 0.94f, 1f, 1f);
        _expLabelText.alignment = TextAlignmentOptions.MidlineRight;
        _expLabelText.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(_expLabelText, 0.40f, Color.black);

        // Initialize state
        _cachedLevel = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1;
        _cachedExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentExp : 0;
        _cachedReq = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.ExpRequired : 4;
        UpdateBar(_cachedExp, _cachedReq);
        UpdateLevelText(_cachedLevel);

        // Giữ Avatar HUD ở trên cùng
        if (PlayerAvatarUI.Instance != null)
        {
            PlayerAvatarUI.Instance.transform.SetAsLastSibling();
        }
    }

    private void RefreshLabel()
    {
        if (_expLabelText != null)
        {
            _expLabelText.text = $"<color=#ffffff>{_cachedExp}</color> / <color=#eeddff>{_cachedReq}</color> <color=#ffdd88>XP</color>";
        }
    }

    private void UpdateBar(int current, int required)
    {
        _cachedExp = current;
        _cachedReq = required;

        if (_expSlider != null)
        {
            _expSlider.maxValue = required > 0 ? required : 1;
            _expSlider.value = current;
        }

        RefreshLabel();
    }

    private void UpdateLevelText(int level)
    {
        _cachedLevel = level;
        RefreshLabel();

        if (level > 1 && _expFill != null && gameObject.activeInHierarchy)
            StartCoroutine(FlashColor());
    }

    private System.Collections.IEnumerator FlashColor()
    {
        if (_expFill == null) yield break;
        _expFill.color = new Color(1f, 0.95f, 0.65f, 1f);
        yield return new WaitForSeconds(0.25f);
        if (_expFill != null)
            _expFill.color = Color.white;
    }
}
