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
                // Đặt Linh Hồn ở y = -135f để nằm dưới thanh level (đỉnh Linh Hồn ở -135 + 28 = -107f, đáy level ở -96f)
                scRt.anchoredPosition = new Vector2(scRt.anchoredPosition.x, -135f);
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
                startY = hbRt.anchoredPosition.y - (hbRt.sizeDelta.y * (1f - hbRt.pivot.y)) - 4f;
                barWidth = hbRt.sizeDelta.x;
            }
        }

        // Đặt ngay dưới 2 thanh năng lượng (tổng chiều cao năng lượng là 34px)
        float expOffsetY = startY - 34f - 3f; // ~ -80f
        float expBarHeight = 13f;

        _root = new GameObject("PlayerExpBarHUD");
        _root.transform.SetParent(mainCanvas.transform, false);

        var rootRt = _root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);
        rootRt.anchoredPosition = new Vector2(startX, expOffsetY);
        rootRt.sizeDelta = new Vector2(barWidth, expBarHeight);

        // Nền tối giống hệt EnergyBar
        var bgImg = _root.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        var outline = _root.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.6f);
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
        _expFill.color = new Color(0.22f, 0.60f, 1f, 1f); // Xanh Sapphire tươi sáng
        _expSlider.fillRect = fillRt;

        // Label Text hiển thị sắc nét giữa thanh: "LV 1   0 / 8 EXP"
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(_root.transform, false);
        var textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        _expLabelText = textObj.AddComponent<TextMeshProUGUI>();
        _expLabelText.fontSize = 11.5f;
        _expLabelText.fontStyle = FontStyles.Bold;
        _expLabelText.color = Color.white;
        _expLabelText.alignment = TextAlignmentOptions.Center;
        _expLabelText.raycastTarget = false;
        _expLabelText.outlineWidth = 0.35f;
        _expLabelText.outlineColor = Color.black;

        // Initialize state
        _cachedLevel = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentLevel : 1;
        _cachedExp = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.CurrentExp : 0;
        _cachedReq = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.ExpRequired : 4;
        UpdateBar(_cachedExp, _cachedReq);
        UpdateLevelText(_cachedLevel);
    }

    private void RefreshLabel()
    {
        if (_expLabelText != null)
        {
            _expLabelText.text = $"<color=#FFD700>LV {_cachedLevel}</color>   <size=10><color=#E0F2FE>{_cachedExp}/{_cachedReq} EXP</color></size>";
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
        Color orig = _expFill.color;
        _expFill.color = new Color(1f, 0.95f, 0.35f, 1f);
        yield return new WaitForSeconds(0.25f);
        if (_expFill != null)
            _expFill.color = orig;
    }
}
