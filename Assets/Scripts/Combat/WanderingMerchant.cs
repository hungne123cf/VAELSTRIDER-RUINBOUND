using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// Thực thể Thương Nhân Tàn Tích (Wandering Merchant).
/// Xuất hiện dọc đường sau khi đánh bại Boss, bên cạnh Cổng Không Gian.
/// Có Animation Idle thở / cử động tự nhiên và Biểu tượng Dấu Chấm Than [!] phát sáng chuẩn RPG hiện đại.
/// </summary>
public class WanderingMerchant : MonoBehaviour
{
    private static WanderingMerchant instance;
    public static WanderingMerchant Instance => instance;

    [Header("Settings")]
    [SerializeField] private float interactRadius = 3.4f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.62f, 0f);

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Transform playerTransform;

    // Sprite Animation
    private Sprite[] idleSprites;
    private Sprite[] interactSprites;
    private float animTimer = 0f;
    private int currentFrame = 0;
    private float frameRate = 0.12f; // ~8 fps

    // Modern RPG Floating Exclamation Mark [!] Indicator (No circular frame)
    private GameObject exclamationObj;
    private TextMeshPro exclamationText;
    private Light2D exclamationGlow;
    private bool isPlayerInRange = false;

    void Awake()
    {
        instance = this;
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    void Start()
    {
        FindPlayer();
        SetupVisuals();
        SetupExclamationIndicator();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRadius;
        isPlayerInRange = inRange;

        bool shopOpen = WanderingShopUI.Instance != null && WanderingShopUI.Instance.IsOpen();
        bool shouldShowIndicator = inRange && !shopOpen;

        // Cập nhật Exclamation Mark [!]
        if (exclamationObj != null)
        {
            if (exclamationObj.activeSelf != shouldShowIndicator)
            {
                exclamationObj.SetActive(shouldShowIndicator);
            }

            if (shouldShowIndicator)
            {
                // Nhấp nhô bồng bềnh và đập nhịp ánh kim chuẩn RPG hiện đại
                float bob = Mathf.Sin(Time.time * 4.2f) * 0.05f;
                float pulse = 1f + Mathf.Sin(Time.time * 5.5f) * 0.08f;
                exclamationObj.transform.localPosition = promptOffset + new Vector3(0f, bob, 0f);
                exclamationObj.transform.localScale = Vector3.one * pulse;

                if (exclamationText != null)
                {
                    float colorPulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                    exclamationText.color = Color.Lerp(new Color(1f, 0.88f, 0.25f, 1f), new Color(1f, 0.98f, 0.7f, 1f), colorPulse);
                }
            }
        }

        // Cập nhật Animation Frame của Thương Nhân
        UpdateMerchantAnimation(shopOpen || inRange);

        // Không mở shop nếu Popup Cổng đang mở hoặc vừa mới đóng
        if (PortalConfirmationPopupUI.Instance != null && (PortalConfirmationPopupUI.Instance.IsOpen || Time.unscaledTime - PortalConfirmationPopupUI.LastClosedTime < 0.4f))
            return;

        // Tương tác phím E trên PC
        if (inRange && !shopOpen && Input.GetKeyDown(KeyCode.E))
        {
            OpenShop();
        }

        // Bấm / chạm trực tiếp vào Thương Nhân (chuẩn xác cho cả PC và Mobile)
        if (Input.GetMouseButtonDown(0) && !shopOpen)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
                if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            }

            if (Camera.main != null)
            {
                Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float clickDist = Vector2.Distance(mouseWorld, transform.position);
                if (clickDist <= 1.5f)
                {
                    if (isPlayerInRange)
                    {
                        OpenShop();
                    }
                    else
                    {
                        DamagePopup.CreateText(transform.position + Vector3.up * 1.2f, LocalizationManager.T("Hãy lại gần hơn!", "Move closer!"), Color.yellow);
                    }
                }
            }
        }
    }

    private void UpdateMerchantAnimation(bool isInteracting)
    {
        Sprite[] activeSprites = (isInteracting && interactSprites != null && interactSprites.Length > 0)
            ? interactSprites
            : idleSprites;

        if (activeSprites == null || activeSprites.Length == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= frameRate)
        {
            animTimer = 0f;
            currentFrame = (currentFrame + 1) % activeSprites.Length;
            if (spriteRenderer != null && activeSprites[currentFrame] != null)
            {
                spriteRenderer.sprite = activeSprites[currentFrame];
            }
        }
    }

    void OnMouseDown()
    {
        // Chặn click xuyên qua UI hoặc khi Popup xác nhận vừa đóng
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (PortalConfirmationPopupUI.Instance != null && (PortalConfirmationPopupUI.Instance.IsOpen || Time.unscaledTime - PortalConfirmationPopupUI.LastClosedTime < 0.4f))
            return;

        // Chạm/Click trực tiếp vào Thương Nhân trên Mobile hoặc PC
        if (isPlayerInRange)
        {
            OpenShop();
        }
        else
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.2f, LocalizationManager.T("Hãy lại gần hơn!", "Move closer!"), Color.yellow);
        }
    }

    public void OpenShop()
    {
        var shopUI = WanderingShopUI.EnsureUIExists();
        if (shopUI != null)
        {
            shopUI.Open();
        }
    }

    private void FindPlayer()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    private void SetupVisuals()
    {
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Tải bộ Texture Animation Idle (8 frames) và Interact (9 frames)
        LoadSpriteAnimations();

        spriteRenderer.sortingOrder = 4920;
        // Tăng scale lên 2.4f để kích thước nhân vật tương đương Player (bù trừ vùng trong suốt 110x110)
        transform.localScale = new Vector3(2.4f, 2.4f, 1f);

        // Đèn lửa trại / đèn lồng thương nhân ấm áp
        var lightObj = new GameObject("MerchantLanternLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0.2f, -0.1f, 0f);

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.76f, 0.38f, 1f); // Warm campfire amber
        light.pointLightInnerRadius = 0.5f;
        light.pointLightOuterRadius = 3.2f;
        light.intensity = 1.6f;

        lightObj.AddComponent<TorchFlicker>();

        if (circleCollider == null) circleCollider = gameObject.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.85f;
    }

    private void LoadSpriteAnimations()
    {
        // 1. Tải Idle Sprites
        Texture2D idleTex = Resources.Load<Texture2D>("NPC/merchant_idle");
#if UNITY_EDITOR
        if (idleTex == null)
        {
            idleTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ART/EPIC RPG World Pack - [FREE Demo]Ancient Ruins - Copia/Characters/NPC Merchant-idle.png");
        }
#endif
        if (idleTex != null)
        {
            int frameCount = Mathf.Min(8, idleTex.width / 110);
            idleSprites = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                idleSprites[i] = Sprite.Create(idleTex, new Rect(i * 110f, 0f, 110f, 110f), new Vector2(0.5f, 0.5f), 100f);
            }
            if (idleSprites.Length > 0) spriteRenderer.sprite = idleSprites[0];
        }

        // 2. Tải Interact Sprites
        Texture2D interactTex = Resources.Load<Texture2D>("NPC/merchant_interact");
#if UNITY_EDITOR
        if (interactTex == null)
        {
            interactTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ART/EPIC RPG World Pack - [FREE Demo]Ancient Ruins - Copia/Characters/NPC Merchant-interacting-loop.png");
        }
#endif
        if (interactTex != null)
        {
            int frameCount = Mathf.Min(9, interactTex.width / 110);
            interactSprites = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                interactSprites[i] = Sprite.Create(interactTex, new Rect(i * 110f, 0f, 110f, 110f), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        // Fallback nếu chưa có sprite
        if (spriteRenderer.sprite == null)
        {
#if UNITY_EDITOR
            spriteRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map3_Extracted/merchant_npc_110x110.png");
#endif
        }
    }

    /// <summary>
    /// Tạo Biểu Tượng Dấu Chấm Than [!] phát sáng lơ lửng trên đầu chuẩn phong cách Modern RPG / Dash Fantasy (Không vòng khung)
    /// </summary>
    private void SetupExclamationIndicator()
    {
        if (exclamationObj != null) return;

        exclamationObj = new GameObject("MerchantExclamationIndicator");
        exclamationObj.transform.SetParent(transform, false);
        exclamationObj.transform.localPosition = promptOffset;

        // Dấu Chấm Than [!] Hoàng Kim Sáng Rực Rỡ (Không có vòng khung bezel tròn)
        GameObject textObj = new GameObject("ExclamationMark");
        textObj.transform.SetParent(exclamationObj.transform, false);
        textObj.transform.localPosition = Vector3.zero;

        exclamationText = textObj.AddComponent<TextMeshPro>();
        exclamationText.fontSize = 5.2f;
        exclamationText.fontStyle = FontStyles.Bold;
        exclamationText.alignment = TextAlignmentOptions.Center;
        exclamationText.text = "!";
        exclamationText.color = new Color(1f, 0.88f, 0.25f, 1f);
        exclamationText.sortingOrder = 5510;

        // Nguồn sáng dịu Light2D vàng kim toả nhẹ quanh dấu chấm than
        var glowObj = new GameObject("IndicatorGlow");
        glowObj.transform.SetParent(exclamationObj.transform, false);
        exclamationGlow = glowObj.AddComponent<Light2D>();
        exclamationGlow.lightType = Light2D.LightType.Point;
        exclamationGlow.color = new Color(1f, 0.85f, 0.3f, 1f);
        exclamationGlow.pointLightInnerRadius = 0.15f;
        exclamationGlow.pointLightOuterRadius = 1.1f;
        exclamationGlow.intensity = 1.35f;

        exclamationObj.SetActive(false);
    }

    /// <summary>
    /// Sinh thực thể Thương Nhân tại vị trí chỉ định
    /// </summary>
    public static WanderingMerchant SpawnMerchant(Vector3 pos)
    {
        var existing = FindAnyObjectByType<WanderingMerchant>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.transform.position = pos;
            return existing;
        }

        GameObject obj = new GameObject("WanderingMerchant");
        obj.transform.position = pos;

        var merchant = obj.AddComponent<WanderingMerchant>();
        return merchant;
    }
}
