using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Corrupted Void Portal (Cổng Tha Hóa / Vết Nứt Ác Mộng)
/// Cánh cổng màu tím hư không xuất hiện sau khi người chơi đánh bại Trùm Cuối hoặc trên màn hình kết thúc:
/// - Kết nối trực tiếp vào Chu Kỳ II (New Game+ Nightmare Mode) phong cách Shape of Dream / Dash Fantasy.
/// - Cho phép người chơi mang toàn bộ Build endgame sang thử lửa với bản đồ tha hóa.
/// </summary>
public class CorruptedVoidPortal : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Vector3 baseScale = new Vector3(1.6f, 1.6f, 1f);

    private bool isPlayerNearby = false;
    private bool isEntering = false;
    private float interactRadius = 3.8f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null) circleCollider = gameObject.AddComponent<CircleCollider2D>();

        circleCollider.isTrigger = true;
        circleCollider.radius = 1.8f;

        // Tải sprite cổng và phủ màu tím đen tử linh hư không
        var sp = Resources.Load<Sprite>("Environment/ruin_portal");
#if UNITY_EDITOR
        if (sp == null)
        {
            sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/ruin_portal.png");
        }
#endif
        if (sp != null) spriteRenderer.sprite = sp;

        CalamityRealm nextRealm = CorruptedCycleManager.GetNextRealm();
        Color realmColor = CorruptedCycleManager.GetRealmColor(nextRealm);
        spriteRenderer.color = new Color(realmColor.r, realmColor.g, realmColor.b, 0.95f);
        spriteRenderer.sortingOrder = 5010;
        transform.localScale = baseScale;

        // Bổ sung Universal Light2D phát sáng theo năng lượng của Cõi Kế Tiếp
        var light = GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light == null)
        {
            light = gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.pointLightInnerRadius = 0.8f;
            light.pointLightOuterRadius = 4.5f;
            light.intensity = 2.4f;
        }
        light.color = realmColor;
    }

    void Update()
    {
        if (isEntering) return;

        // Hiệu ứng thở xoay nhẹ của cổng hư không
        float pulse = 1.0f + Mathf.Sin(Time.time * 3.5f) * 0.08f;
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, 1f);

        // Kiểm tra khoảng cách người chơi
        var player = Object.FindAnyObjectByType<PlayerController>();
        float distToPlayer = player != null ? Vector2.Distance(transform.position, player.transform.position) : 99f;
        isPlayerNearby = (distToPlayer <= interactRadius);

        // Bấm phím E / Space / Enter trên PC khi đứng gần
        if (isPlayerNearby && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            bool popupOpen = PortalConfirmationPopupUI.Instance != null && PortalConfirmationPopupUI.Instance.IsOpen;
            bool shopOpen = WanderingShopUI.Instance != null && WanderingShopUI.Instance.IsOpen();
            if (!popupOpen && !shopOpen && (Time.unscaledTime - PortalConfirmationPopupUI.LastClosedTime >= 0.35f))
            {
                var merchant = WanderingMerchant.Instance;
                if (merchant != null && merchant.gameObject.activeInHierarchy && player != null)
                {
                    float distToMerchant = Vector2.Distance(merchant.transform.position, player.transform.position);
                    if (distToMerchant < distToPlayer && distToMerchant <= 3.5f)
                    {
                        return;
                    }
                }

                TriggerPortalConfirmation();
            }
        }

        // BẤM / CHẠM VÀO CỔNG TÍM (Click chuột hoặc chạm màn hình cảm ứng)
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseOrTouchClick();
        }
    }

    private void HandleMouseOrTouchClick()
    {
        if (isEntering) return;
        if (PortalConfirmationPopupUI.Instance != null && PortalConfirmationPopupUI.Instance.IsOpen) return;
        if (WanderingShopUI.Instance != null && WanderingShopUI.Instance.IsOpen()) return;
        if (Time.unscaledTime - PortalConfirmationPopupUI.LastClosedTime < 0.35f) return;

        // Tránh kích hoạt khi người chơi đang bấm vào các nút UI khác (Pause, Joystick, HUD...)
        if (IsPointerOverUI()) return;

        if (Camera.main == null) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clickDistance = Vector2.Distance(mouseWorld, transform.position);

        var player = Object.FindAnyObjectByType<PlayerController>();
        float distToPlayer = player != null ? Vector2.Distance(transform.position, player.transform.position) : 99f;

        // CHỈ mở khi người chơi click / tap CHÍNH XÁC lên hình ảnh cổng tím (bán kính <= 1.4f)
        // và nhân vật trong cự ly tương tác hợp lý (<= 4.5f)
        if (clickDistance <= 1.4f && distToPlayer <= 4.5f)
        {
            var merchant = WanderingMerchant.Instance;
            if (merchant != null && merchant.gameObject.activeInHierarchy)
            {
                float distClickToMerchant = Vector2.Distance(mouseWorld, merchant.transform.position);
                if (distClickToMerchant < clickDistance) return;
            }

            TriggerPortalConfirmation();
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return true;
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isEntering) return;
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            isPlayerNearby = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            isPlayerNearby = false;
        }
    }

    void OnMouseDown()
    {
        if (isEntering) return;
        if (IsPointerOverUI()) return;
        if (Time.unscaledTime - PortalConfirmationPopupUI.LastClosedTime < 0.35f) return;

        TriggerPortalConfirmation();
    }

    public void TriggerPortalConfirmation()
    {
        if (isEntering) return;

        var popup = PortalConfirmationPopupUI.EnsureUIExists();
        if (popup != null)
        {
            popup.OpenForCorruptedPortal(this);
        }
    }

    public void EnterCorruptedCycle()
    {
        if (isEntering) return;
        StartCoroutine(EnterPortalRoutine());
    }

    private IEnumerator EnterPortalRoutine()
    {
        isEntering = true;

        var player = Object.FindAnyObjectByType<PlayerController>();
        var ph = Object.FindAnyObjectByType<PlayerHealth>();
        if (ph != null && !ph.IsDead && ph.CurrentHealth > 0)
        {
            ph.isInvulnerable = true;
            PlayerHealth.RunPreservedCurrentHealth = ph.CurrentHealth;
            Debug.Log($"<color=magenta>[CORRUPTED PORTAL]</color> Đã khóa bảo lưu {ph.CurrentHealth}/{ph.MaxHealth} HP!");
        }

        if (player != null)
        {
            player.SetCanMove(false);
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();

            // Hiệu ứng hút người chơi vào giữa tâm cổng
            Vector3 startPos = player.transform.position;
            Vector3 centerPos = transform.position;
            Vector3 startScale = player.transform.localScale;

            float t = 0f;
            while (t < 0.85f)
            {
                t += Time.deltaTime;
                float p = t / 0.85f;
                player.transform.position = Vector3.Lerp(startPos, centerPos, p);
                player.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                player.transform.Rotate(0, 0, 720f * Time.deltaTime);
                yield return null;
            }
        }

        ph = Object.FindAnyObjectByType<PlayerHealth>();
        if (ph != null && !ph.IsDead && ph.CurrentHealth > 0)
        {
            PlayerHealth.RunPreservedCurrentHealth = ph.CurrentHealth;
        }

        yield return new WaitForSeconds(0.3f);
        CorruptedCycleManager.StartCorruptedCycle();
    }

    public static CorruptedVoidPortal Spawn(Vector3 pos)
    {
        GameObject obj = new GameObject("CorruptedVoidPortal");
        obj.transform.position = pos;
        return obj.AddComponent<CorruptedVoidPortal>();
    }
}
