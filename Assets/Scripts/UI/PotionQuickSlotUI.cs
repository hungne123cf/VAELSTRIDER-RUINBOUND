using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class PotionQuickSlotUI : MonoBehaviour
{
    public static PotionQuickSlotUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Button potionButton;
    [SerializeField] private Image potionIcon;
    [SerializeField] private Text legacyCountText;
    [SerializeField] private TextMeshProUGUI tmpCountText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Key Shortcut for PC (Nút 1, Q, H, E)")]
    [SerializeField] private KeyCode useKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode secondaryKey = KeyCode.Q;

    private PlayerInventory playerInventory;
    private Canvas parentCanvas;

    void Awake()
    {
        Instance = this;

        if (potionButton == null) potionButton = GetComponent<Button>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (legacyCountText == null) legacyCountText = GetComponentInChildren<Text>();
        if (tmpCountText == null) tmpCountText = GetComponentInChildren<TextMeshProUGUI>();

        parentCanvas = GetComponentInParent<Canvas>();

                // Bo tròn khung nút bình máu
        Image bgImg = GetComponent<Image>();
        if (bgImg != null)
        {
            Sprite slotSprite = Resources.Load<Sprite>("UI/potion_slot_circle");
#if UNITY_EDITOR
            if (slotSprite == null) slotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/potion_slot_circle.png");
#endif
            if (slotSprite != null)
            {
                bgImg.sprite = slotSprite;
                bgImg.color = Color.white;
            }
        }

        if (potionButton != null)
        {
            potionButton.onClick.RemoveAllListeners();
            potionButton.onClick.AddListener(OnPotionButtonClicked);
        }
    }

    void Start()
    {
        BindInventory();
    }

    void OnEnable()
    {
        Instance = this;
        BindInventory();
    }

    void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnPotionsChanged -= UpdateUI;
        }
    }

    private void BindInventory()
    {
        if (playerInventory == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerInventory = player.GetComponent<PlayerInventory>();
            }
        }

        if (playerInventory != null)
        {
            playerInventory.OnPotionsChanged -= UpdateUI;
            playerInventory.OnPotionsChanged += UpdateUI;
            UpdateUI(playerInventory.GetPotions());
        }
        else
        {
            UpdateUI(0);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(useKey) || Input.GetKeyDown(secondaryKey) || Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.E))
        {
            OnPotionButtonClicked();
        }
    }

    public void OnPotionButtonClicked()
    {
        if (playerInventory == null)
        {
            BindInventory();
        }

        var health = (playerInventory != null) 
            ? playerInventory.GetComponent<PlayerHealth>() 
            : FindAnyObjectByType<PlayerHealth>();

        if (health != null && (health.IsDead || health.CurrentHealth <= 0))
        {
            return;
        }

        if (playerInventory != null)
        {
            bool used = playerInventory.UsePotion();
            if (used)
            {
                StopAllCoroutines();
                StartCoroutine(PunchScaleRoutine());
            }
        }
    }

    private void UpdateUI(int count)
    {
        string text;
        if (count > 99) text = "99+";
        else if (count > 0) text = $"x{count}";
        else text = "0";

        if (legacyCountText != null)
        {
            legacyCountText.text = text;
            legacyCountText.horizontalOverflow = HorizontalWrapMode.Overflow;
            legacyCountText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (tmpCountText != null)
        {
            tmpCountText.text = text;
            tmpCountText.enableWordWrapping = false;
            tmpCountText.overflowMode = TextOverflowModes.Overflow;
        }

        if (canvasGroup != null)
        {
            if (count > 0)
            {
                canvasGroup.alpha = 1f;
                        // Bo tròn khung nút bình máu
        Image bgImg = GetComponent<Image>();
        if (bgImg != null)
        {
            Sprite slotSprite = Resources.Load<Sprite>("UI/potion_slot_circle");
#if UNITY_EDITOR
            if (slotSprite == null) slotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/potion_slot_circle.png");
#endif
            if (slotSprite != null)
            {
                bgImg.sprite = slotSprite;
                bgImg.color = Color.white;
            }
        }

        if (potionButton != null) potionButton.interactable = true;
            }
            else
            {
                canvasGroup.alpha = 0.5f;
                        // Bo tròn khung nút bình máu
        Image bgImg = GetComponent<Image>();
        if (bgImg != null)
        {
            Sprite slotSprite = Resources.Load<Sprite>("UI/potion_slot_circle");
#if UNITY_EDITOR
            if (slotSprite == null) slotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/potion_slot_circle.png");
#endif
            if (slotSprite != null)
            {
                bgImg.sprite = slotSprite;
                bgImg.color = Color.white;
            }
        }

        if (potionButton != null) potionButton.interactable = true; // van de click de bao no potions
            }
        }
    }

    public void PlayFlyAnimation(Vector3 worldStartPos, Action onComplete = null)
    {
        if (!gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(FlyToSlotRoutine(worldStartPos, onComplete));
    }

    private IEnumerator FlyToSlotRoutine(Vector3 worldStartPos, Action onComplete)
    {
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

        Camera cam = Camera.main;
        Vector3 startScreenPos = Vector3.zero;
        if (cam != null)
        {
            startScreenPos = cam.WorldToScreenPoint(worldStartPos);
        }
        else
        {
            startScreenPos = transform.position;
        }

        GameObject flyObj = new GameObject("FlyingPotionFX");
        flyObj.transform.SetParent(parentCanvas.transform, false);

        RectTransform rect = flyObj.AddComponent<RectTransform>();
        rect.position = startScreenPos;
        rect.sizeDelta = new Vector2(48, 48);

        Image img = flyObj.AddComponent<Image>();
        Sprite potSprite = Resources.Load<Sprite>("Items/health_potion");
#if UNITY_EDITOR
        if (potSprite == null)
        {
            potSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
        }
#endif
        if (potSprite != null) img.sprite = potSprite;
        img.raycastTarget = false;

        Vector3 targetScreenPos = transform.position;
        Vector3 midPoint = (startScreenPos + targetScreenPos) * 0.5f + new Vector3(UnityEngine.Random.Range(-50f, 50f), 100f, 0);

        float duration = 0.42f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 pos = (1 - easeT) * (1 - easeT) * startScreenPos +
                          2 * (1 - easeT) * easeT * midPoint +
                          easeT * easeT * targetScreenPos;

            rect.position = pos;

            float scale = Mathf.Lerp(1.35f, 0.65f, easeT);
            rect.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(flyObj);

        StartCoroutine(PunchScaleRoutine());
        onComplete?.Invoke();
    }

    private IEnumerator PunchScaleRoutine()
    {
        Vector3 orig = Vector3.one;
        transform.localScale = orig * 1.35f;
        yield return new WaitForSeconds(0.08f);
        transform.localScale = orig * 0.9f;
        yield return new WaitForSeconds(0.06f);
        transform.localScale = orig;
    }

    public static PotionQuickSlotUI EnsureUIExists()
    {
        if (Instance != null) return Instance;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("Btn_Potion");
        if (existing != null)
        {
            Instance = existing.GetComponent<PotionQuickSlotUI>();
            if (Instance == null) Instance = existing.gameObject.AddComponent<PotionQuickSlotUI>();
            return Instance;
        }

        // Tu dong tao nut tren Canvas
        GameObject btnObj = new GameObject("Btn_Potion");
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-220, 360);
        rect.sizeDelta = new Vector2(75, 75);

        Image bgImage = btnObj.AddComponent<Image>();
        Sprite potSlotSprite = Resources.Load<Sprite>("UI/potion_slot_circle");
#if UNITY_EDITOR
        if (potSlotSprite == null) potSlotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/potion_slot_circle.png");
#endif
        if (potSlotSprite != null) bgImage.sprite = potSlotSprite;
        bgImage.color = Color.white;

        Button button = btnObj.AddComponent<Button>();

        GameObject iconObj = new GameObject("PotionIcon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(50, 50);
        iconRect.anchoredPosition = new Vector2(0, 3);
        Image iconImage = iconObj.AddComponent<Image>();
        Sprite potSprite = Resources.Load<Sprite>("Items/health_potion");
#if UNITY_EDITOR
        if (potSprite == null) potSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
#endif
        if (potSprite != null) iconImage.sprite = potSprite;
        iconImage.raycastTarget = false;

        GameObject badgeObj = new GameObject("AmountText");
        badgeObj.transform.SetParent(btnObj.transform, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1, 0);
        badgeRect.anchorMax = new Vector2(1, 0);
        badgeRect.pivot = new Vector2(1, 0);
        badgeRect.anchoredPosition = new Vector2(-4, 4);
        badgeRect.sizeDelta = new Vector2(65, 24);

        var tmp = badgeObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "x0";
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        var outline = badgeObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Instance = btnObj.AddComponent<PotionQuickSlotUI>();
        return Instance;
    }
}
