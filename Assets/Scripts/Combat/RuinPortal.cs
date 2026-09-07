using UnityEngine;
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

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        baseScale = transform.localScale;

        circleCollider.isTrigger = true;
        circleCollider.radius = 0.85f;

        // Auto load sprite if null
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

        spriteRenderer.sortingOrder = 4900; // Tren san, duoi Player
    }

    void OnEnable()
    {
        WaveSpawner.OnAllWavesCleared += HandleAllWavesCleared;
    }

    void OnDisable()
    {
        WaveSpawner.OnAllWavesCleared -= HandleAllWavesCleared;
    }

    void Start()
    {
        // Neu xuat hien tu dau (chua co wave spawner), kich hoat luon
        var spawner = Object.FindAnyObjectByType<WaveSpawner>();
        if (spawner == null)
        {
            ActivatePortal();
        }
        else
        {
            // An di, cho den khi Wave 3 hoan tat moi bung ra
            gameObject.SetActive(false);
        }
    }

    private void HandleAllWavesCleared()
    {
        gameObject.SetActive(true);
        StartCoroutine(SpawnPortalRoutine());
    }

    private IEnumerator SpawnPortalRoutine()
    {
        // Cho 2s de tinh the va quai rot het
        yield return new WaitForSeconds(2.0f);

        ActivatePortal();
    }

    public void ActivatePortal()
    {
        isActivated = true;
        StartCoroutine(AppearRoutine());
    }

    private IEnumerator AppearRoutine()
    {
        transform.localScale = Vector3.zero;

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.2f, 0.35f);

        DamagePopup.CreateText(transform.position, "PORTAL OPENED!", new Color(0.8f, 0.4f, 1f));

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

    void Update()
    {
        if (!isActivated || isPlayerEntering) return;

        // Hieu ung dap xung nhe nhang (Portal breathing)
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * (1f + pulse);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActivated || isPlayerEntering) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            StartCoroutine(PlayerEnterPortalRoutine(other.transform));
        }
    }

    private IEnumerator PlayerEnterPortalRoutine(Transform playerTransform)
    {
        isPlayerEntering = true;

        // Vo hieu hoa dieu khien cua nguoi choi
        var controller = playerTransform.GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.25f, 0.3f);

        // Nguoi choi bi hut vao tam cong va thu nho bien mat
        Vector3 startPos = playerTransform.position;
        Vector3 portalCenter = transform.position + Vector3.up * 0.4f;
        Vector3 startScale = playerTransform.localScale;

        float t = 0;
        float duration = 0.8f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            playerTransform.position = Vector3.Lerp(startPos, portalCenter, p);
            playerTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            yield return null;
        }

        // Hien thi man hinh chien thang!
        if (VictoryUI.Instance != null)
        {
            VictoryUI.Instance.ShowVictoryScreen();
        }
    }
}
