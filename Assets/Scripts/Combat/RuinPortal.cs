using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class RuinPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.08f;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Vector3 baseScale;
    private bool isActivated = false;
    private bool isPlayerEntering = false;
    private bool isFinalRealm = false;
    public bool IsFinalRealm => isFinalRealm;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        baseScale = transform.localScale;
        if (baseScale == Vector3.zero) baseScale = Vector3.one;

        circleCollider.isTrigger = true;
        circleCollider.radius = 0.85f;

        if (spriteRenderer.sprite == null)
        {
            var sp = Resources.Load<Sprite>("Environment/ruin_portal");
#if UNITY_EDITOR
            if (sp == null)
            {
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/ruin_portal.png");
            }
#endif
            spriteRenderer.sprite = sp;
        }

        spriteRenderer.sortingOrder = 4900;

        string sceneName = SceneManager.GetActiveScene().name;
        isFinalRealm = sceneName.Contains("Map3") || sceneName.Contains("Forgotten");

        // Tuyet doi khong attach HeavenlyPortalFX o Awake! Chi attach khi thuc su ActivatePortal()
        if (!isActivated)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (circleCollider != null) circleCollider.enabled = false;
            transform.localScale = Vector3.zero;
        }
    }

    void OnEnable()
    {
        WaveSpawner.OnAllWavesCleared += HandleAllWavesCleared;
        Map3WaveSpawner.OnAllWavesCleared += HandleAllWavesCleared;
    }

    void OnDisable()
    {
        WaveSpawner.OnAllWavesCleared -= HandleAllWavesCleared;
        Map3WaveSpawner.OnAllWavesCleared -= HandleAllWavesCleared;
    }

    void Start()
    {
        var spawner = Object.FindAnyObjectByType<WaveSpawner>();
        if (spawner != null && !isActivated)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (circleCollider != null) circleCollider.enabled = false;
            transform.localScale = Vector3.zero;
        }
    }

    private void HandleAllWavesCleared()
    {
        StartCoroutine(SpawnPortalRoutine());
    }

    private IEnumerator SpawnPortalRoutine()
    {
        while (ShadowDoppelgangerBoss.ActiveBoss != null && !ShadowDoppelgangerBoss.ActiveBoss.IsDead)
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1.8f);
        ActivatePortal();
    }

    public void ActivatePortal()
    {
        isActivated = true;
        gameObject.SetActive(true);

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();

        if (baseScale == Vector3.zero || baseScale.magnitude < 0.2f)
        {
            baseScale = new Vector3(1.5f, 1.5f, 1f);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = 5000;
            if (spriteRenderer.sprite == null)
            {
                var sp = Resources.Load<Sprite>("Environment/ruin_portal");
#if UNITY_EDITOR
                if (sp == null)
                {
                    sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/ruin_portal.png");
                }
#endif
                spriteRenderer.sprite = sp;
            }
        }

        if (circleCollider != null)
        {
            circleCollider.enabled = true;
            circleCollider.isTrigger = true;
            if (circleCollider.radius < 0.5f) circleCollider.radius = 0.85f;
        }

        var light = GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light == null)
        {
            light = gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.pointLightInnerRadius = 0.8f;
            light.pointLightOuterRadius = 4.5f;
            light.intensity = 1.8f;
            light.color = isFinalRealm ? new Color(1f, 0.95f, 0.5f) : new Color(0.78f, 0.38f, 1f);
        }
        else
        {
            light.enabled = true;
        }

        if (isFinalRealm)
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.96f, 0.75f, 1f);
            HeavenlyPortalFX.Attach(gameObject);
        }

        StopAllCoroutines();
        StartCoroutine(AppearRoutine());
    }

    private IEnumerator AppearRoutine()
    {
        transform.localScale = Vector3.zero;

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.25f, 0.4f);

        if (isFinalRealm)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, LocalizationManager.T("CỔNG THIÊN ĐƯỜNG ĐÃ MỞ!", "HEAVENLY GATE OPENED!"), new Color(1f, 0.95f, 0.4f));
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
        }
        else
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.2f, LocalizationManager.T("CỔNG KHÔNG GIAN ĐÃ MỞ!", "PORTAL OPENED!"), new Color(0.85f, 0.45f, 1f));
            SoundManager.EnsureInstance()?.PlayPortalSpawn();
        }

        float t = 0;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float progress = t / 0.6f;
            transform.localScale = Vector3.Lerp(Vector3.zero, baseScale, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }
        transform.localScale = baseScale;
    }

    [Header("Interaction Settings")]
    [SerializeField] private float interactRadius = 3.8f;

    private bool isPlayerInRange = false;
    private Transform playerTransform;

    void Update()
    {
        if (!isActivated || isPlayerEntering) return;

        // Pulse hiệu ứng cổng
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * (1f + pulse);

        // Tìm người chơi nếu chưa có
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Tính khoảng cách tới cổng
        Vector2 portalCenter = (Vector2)transform.position + new Vector2(0f, 0.35f);
        float dist = Vector2.Distance(portalCenter, playerTransform.position);
        bool inRange = dist <= interactRadius;
        isPlayerInRange = inRange;

        // Kiểm tra xem Popup xác nhận hoặc Shop có đang mở không
        bool popupOpen = PortalConfirmationPopupUI.Instance != null && PortalConfirmationPopupUI.Instance.IsOpen;
        bool shopOpen = WanderingShopUI.Instance != null && WanderingShopUI.Instance.IsOpen();

        if (popupOpen || shopOpen) return;
        if (Time.unscaledTime - PortalConfirmationPopupUI.LastClosedTime < 0.35f) return;

        // Phím tắt bàn phím PC: [E], [Space], [Enter] khi đứng gần cổng
        if (inRange && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            // Nếu có Thương Nhân ở gần và người chơi đang đứng gần Thương Nhân hơn thì nhường phím cho Thương Nhân
            var merchant = WanderingMerchant.Instance;
            if (merchant != null && merchant.gameObject.activeInHierarchy && playerTransform != null)
            {
                float distToMerchant = Vector2.Distance(merchant.transform.position, playerTransform.position);
                if (distToMerchant < dist && distToMerchant <= 3.5f)
                {
                    return;
                }
            }

            OpenConfirmationPopup();
        }

        // Click chuột hoặc chạm màn hình (chỉ kích hoạt khi click CHÍNH XÁC lên cổng)
        if (Input.GetMouseButtonDown(0))
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

                // CHỈ mở khi con trỏ click trúng vào bán kính cổng (<= 1.4f) và nhân vật trong cự ly tương tác (<= 4.5f)
                if (clickDist <= 1.4f && dist <= 4.5f)
                {
                    // Nếu click gần Thương Nhân hơn thì không mở cổng
                    var merchant = WanderingMerchant.Instance;
                    if (merchant != null && merchant.gameObject.activeInHierarchy)
                    {
                        float distClickToMerchant = Vector2.Distance(mouseWorld, merchant.transform.position);
                        if (distClickToMerchant < clickDist) return;
                    }

                    OpenConfirmationPopup();
                }
            }
        }
    }

    private void FindPlayer()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActivated || isPlayerEntering) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            isPlayerInRange = true;
            playerTransform = other.transform;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            isPlayerInRange = false;
        }
    }

    void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;
        if (PortalConfirmationPopupUI.Instance != null && (PortalConfirmationPopupUI.Instance.IsOpen || Time.unscaledTime - PortalConfirmationPopupUI.LastClosedTime < 0.35f))
            return;

        if (!isActivated || isPlayerEntering) return;
        if (playerTransform == null) FindPlayer();
        float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 99f;
        if (dist <= interactRadius + 1.2f)
        {
            OpenConfirmationPopup();
        }
    }

    public void OpenConfirmationPopup()
    {
        if (!isActivated || isPlayerEntering) return;
        if (playerTransform == null) FindPlayer();

        var popup = PortalConfirmationPopupUI.EnsureUIExists();
        if (popup != null)
        {
            popup.Open(this, playerTransform);
        }
    }

    public void OnConfirmationClosed()
    {
        // Khi đóng Popup (chọn Ở lại), cổng vẫn giữ nguyên chờ người chơi kích hoạt sau
    }

    public void ConfirmProceed(Transform player)
    {
        if (player != null) playerTransform = player;
        if (playerTransform == null) FindPlayer();
        if (playerTransform != null)
        {
            StartCoroutine(PlayerEnterPortalRoutine(playerTransform));
        }
    }

    private IEnumerator PlayerEnterPortalRoutine(Transform pTransform)
    {
        isPlayerEntering = true;

        var ph = pTransform != null ? pTransform.GetComponent<PlayerHealth>() : Object.FindAnyObjectByType<PlayerHealth>();
        if (ph != null && !ph.IsDead && ph.CurrentHealth > 0)
        {
            ph.isInvulnerable = true;
            PlayerHealth.RunPreservedCurrentHealth = ph.CurrentHealth;
            Debug.Log($"<color=yellow>[PORTAL ENTER]</color> Đã khóa bảo lưu {ph.CurrentHealth}/{ph.MaxHealth} HP khi bước vào cổng!");
        }

        var controller = pTransform.GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var rb = pTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.4f);

        Vector3 startPos = pTransform.position;
        Vector3 portalCenter = transform.position + Vector3.up * 0.4f;
        Vector3 startScale = pTransform.localScale;

        float t = 0;
        float duration = 0.9f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            pTransform.position = Vector3.Lerp(startPos, portalCenter, p);
            pTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            yield return null;
        }

        if (isFinalRealm)
        {
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
            EndingCreditsUI.ShowEnding();
        }
        else
        {
            // Duy trì liên tục đồng hồ bấm giờ trận đấu sang Map tiếp theo
            MatchTimerUI.ContinueRunTimer();

            // Bảo lưu chính xác lượng máu hiện tại khi bước qua Cổng sang Map tiếp theo
            ph = Object.FindAnyObjectByType<PlayerHealth>();
            if (ph != null && !ph.IsDead && ph.CurrentHealth > 0)
            {
                PlayerHealth.RunPreservedCurrentHealth = ph.CurrentHealth;
            }

            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == "SampleScene" || currentScene.Contains("Map1"))
            {
                if (Application.CanStreamedLevelBeLoaded("Map2_ValleyRuin"))
                {
                    SceneManager.LoadScene("Map2_ValleyRuin");
                    yield break;
                }
            }
            else if (currentScene.Contains("Map2") || currentScene.Contains("Valley"))
            {
                if (Application.CanStreamedLevelBeLoaded("Map3_ForgottenMemories"))
                {
                    SceneManager.LoadScene("Map3_ForgottenMemories");
                    yield break;
                }
            }
            else if (currentScene.Contains("Map3") || currentScene.Contains("Forgotten"))
            {
                EndingCreditsUI.ShowEnding();
                yield break;
            }

            // Fallback nếu có VictoryUI
            if (VictoryUI.Instance != null)
            {
                VictoryUI.Instance.GoToNextRealm();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    public static RuinPortal SpawnPortal(Vector3 pos)
    {
        // Kiem tra neu trong scene da co RuinPortal nao thi tai su dung, khong sinh ra cong thu hai!
        var existingPortal = Object.FindAnyObjectByType<RuinPortal>(FindObjectsInactive.Include);
        if (existingPortal != null)
        {
            existingPortal.gameObject.SetActive(true);
            existingPortal.transform.position = pos;
            existingPortal.ActivatePortal();
            return existingPortal;
        }

        GameObject portalObj = new GameObject("RuinPortal");
        portalObj.transform.position = pos;

        var sr = portalObj.AddComponent<SpriteRenderer>();
        var portalSprite = Resources.Load<Sprite>("Environment/ruin_portal");
#if UNITY_EDITOR
        if (portalSprite == null)
        {
            portalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/ruin_portal.png");
        }
#endif
        sr.sprite = portalSprite;
        sr.sortingOrder = 4900;

        var col = portalObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.85f;

        var portal = portalObj.AddComponent<RuinPortal>();
        portal.ActivatePortal();
        return portal;
    }
}

