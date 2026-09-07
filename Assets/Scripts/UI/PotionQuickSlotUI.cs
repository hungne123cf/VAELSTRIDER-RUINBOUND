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

    [Header("Key Shortcut for PC (Nút 1 hoặc Q)")]
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

        if (potionButton != null)
        {
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
        if (Input.GetKeyDown(useKey) || Input.GetKeyDown(secondaryKey))
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
        string text = count > 0 ? $"x{count}" : "0";
        if (legacyCountText != null) legacyCountText.text = text;
        if (tmpCountText != null) tmpCountText.text = text;

        if (canvasGroup != null)
        {
            if (count > 0)
            {
                canvasGroup.alpha = 1f;
                if (potionButton != null) potionButton.interactable = true;
            }
            else
            {
                canvasGroup.alpha = 0.45f;
                if (potionButton != null) potionButton.interactable = false;
            }
        }
    }

    public void PlayFlyAnimation(Vector3 worldStartPos, Action onComplete)
    {
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

        // Tao vat the bay tren Canvas
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
            // Smooth ease in-out
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            // Quadratic Bezier Arc bay cong len tren roi ha vao slot
            Vector3 pos = (1 - easeT) * (1 - easeT) * startScreenPos +
                          2 * (1 - easeT) * easeT * midPoint +
                          easeT * easeT * targetScreenPos;

            rect.position = pos;

            // Scale phong to nhe luc bat dau roi thu nho khi lao vao slot
            float scale = Mathf.Lerp(1.35f, 0.65f, easeT);
            rect.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(flyObj);

        // Hieu ung dap vao slot
        StartCoroutine(PunchScaleRoutine());

        // Cap nhat du lieu
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
}
