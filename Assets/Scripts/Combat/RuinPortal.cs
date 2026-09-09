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
        isFinalRealm = sceneName.Contains("Map2") || sceneName.Contains("Valley") || sceneName.Contains("Map3") || sceneName.Contains("Ancient");

        if (isFinalRealm)
        {
            spriteRenderer.color = new Color(1f, 0.96f, 0.75f, 1f);
            HeavenlyPortalFX.Attach(gameObject);
        }
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
        yield return new WaitForSeconds(1.8f);
        ActivatePortal();
    }

    public void ActivatePortal()
    {
        isActivated = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (circleCollider != null) circleCollider.enabled = true;

        if (isFinalRealm)
        {
            spriteRenderer.color = new Color(1f, 0.96f, 0.75f, 1f);
            HeavenlyPortalFX.Attach(gameObject);
        }

        StartCoroutine(AppearRoutine());
    }

    private IEnumerator AppearRoutine()
    {
        transform.localScale = Vector3.zero;

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.25f, 0.4f);

        if (isFinalRealm)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, "HEAVENLY GATE OPENED!", new Color(1f, 0.95f, 0.4f));
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
        }
        else
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.2f, "PORTAL OPENED!", new Color(0.85f, 0.45f, 1f));
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

    void Update()
    {
        if (!isActivated || isPlayerEntering) return;

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

        var controller = playerTransform.GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.4f);

        Vector3 startPos = playerTransform.position;
        Vector3 portalCenter = transform.position + Vector3.up * 0.4f;
        Vector3 startScale = playerTransform.localScale;

        float t = 0;
        float duration = 0.9f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            playerTransform.position = Vector3.Lerp(startPos, portalCenter, p);
            playerTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            yield return null;
        }

        if (isFinalRealm)
        {
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
            EndingCreditsUI.ShowEnding();
        }
        else
        {
            if (VictoryUI.Instance != null)
            {
                VictoryUI.Instance.ShowVictoryScreen();
            }
            else
            {
                VictoryUI.CreateRuntimeVictoryUI();
            }
        }
    }

    public static RuinPortal SpawnPortal(Vector3 pos)
    {
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

        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene.Contains("Map2") || activeScene.Contains("Valley"))
        {
            sr.color = new Color(1f, 0.96f, 0.75f, 1f);
            HeavenlyPortalFX.Attach(portalObj);
        }

        var col = portalObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.85f;

        var portal = portalObj.AddComponent<RuinPortal>();
        portal.ActivatePortal();
        return portal;
    }
}
