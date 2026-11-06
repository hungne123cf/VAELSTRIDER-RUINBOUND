using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Floor Spike Trap (Bẫy Gai Sàn Cổ Đại)
/// 
/// ĐẶC TÍNH CHIẾN THUẬT:
/// 1. Gây sát thương 2 chiều: Cả Người chơi LẪN Quái vật!
///    - Player trúng: mất ~12 HP.
///    - Quái vật trúng: mất 45 sát thương + bị khống chế choáng (Stun) 0.45s!
/// 2. Chu kỳ hoạt động rõ ràng:
///    - Retracted (Thu gai dưới sàn 2.2s) -> Warning (Rung lắc + nhô nhẹ 0.6s) -> Active (Bật gai 0.9s) -> Cooldown.
/// 3. Người chơi có thể dụ bầy Goblin, Slime, Beast bước vào bẫy để tiêu diệt hàng loạt!
/// </summary>
public class FloorSpikeTrap : MonoBehaviour
{
    public enum TrapState { Retracted, Warning, Active, Cooldown }

    [Header("Timers")]
    [SerializeField] private float retractedDuration = 2.4f;
    [SerializeField] private float warningDuration = 0.6f;
    [SerializeField] private float activeDuration = 0.9f;

    [Header("Damage Settings")]
    [SerializeField] private int playerDamage = 12;
    [SerializeField] private int enemyDamage = 45;

    private TrapState currentState = TrapState.Retracted;
    private SpriteRenderer sr;
    private BoxCollider2D triggerCol;

    private static Sprite cachedRetractedSprite;
    private static Sprite cachedWarningSprite;
    private static Sprite cachedActiveSprite;

    private readonly HashSet<Collider2D> damagedTargetsThisCycle = new HashSet<Collider2D>();
    private float stateTimer = 0f;
    private Vector3 initialPosition;

    void Awake()
    {
        initialPosition = transform.position;
        sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sortingOrder = 1450; // Nằm trên sàn gạch nhưng dưới chân quái & player
        gameObject.layer = 0; // Default layer

        triggerCol = gameObject.GetComponent<BoxCollider2D>();
        if (triggerCol == null) triggerCol = gameObject.AddComponent<BoxCollider2D>();
        triggerCol.size = new Vector2(1.2f, 1.2f);
        triggerCol.isTrigger = true;

        EnsureSprites();
        ApplyVisualState(TrapState.Retracted);
    }

    void Start()
    {
        // Thêm độ lệch ngẫu nhiên để các bẫy không bật cùng một giây
        stateTimer = Random.Range(0f, retractedDuration * 0.8f);
    }

    void Update()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case TrapState.Retracted:
                if (stateTimer >= retractedDuration)
                {
                    ChangeState(TrapState.Warning);
                }
                break;

            case TrapState.Warning:
                // Rung nhẹ cảnh báo
                float shake = Mathf.Sin(Time.time * 45f) * 0.035f;
                transform.position = initialPosition + new Vector3(shake, 0f, 0f);

                if (stateTimer >= warningDuration)
                {
                    transform.position = initialPosition;
                    ChangeState(TrapState.Active);
                }
                break;

            case TrapState.Active:
                if (stateTimer >= activeDuration)
                {
                    ChangeState(TrapState.Cooldown);
                }
                break;

            case TrapState.Cooldown:
                if (stateTimer >= 0.5f)
                {
                    ChangeState(TrapState.Retracted);
                }
                break;
        }
    }

    private void ChangeState(TrapState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        ApplyVisualState(newState);

        if (newState == TrapState.Active)
        {
            damagedTargetsThisCycle.Clear();
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            SpawnSpikeDustFX();
            CheckOverlappingEntities();
        }
    }

    private void ApplyVisualState(TrapState state)
    {
        if (sr == null) return;

        switch (state)
        {
            case TrapState.Retracted:
            case TrapState.Cooldown:
                sr.sprite = cachedRetractedSprite;
                sr.color = new Color(0.75f, 0.75f, 0.8f, 0.85f);
                break;

            case TrapState.Warning:
                sr.sprite = cachedWarningSprite;
                sr.color = new Color(1f, 0.85f, 0.4f, 1f);
                break;

            case TrapState.Active:
                sr.sprite = cachedActiveSprite;
                sr.color = new Color(1f, 0.45f, 0.4f, 1f);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState == TrapState.Active)
        {
            HandleHitTarget(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (currentState == TrapState.Active)
        {
            HandleHitTarget(other);
        }
    }

    private void CheckOverlappingEntities()
    {
        if (triggerCol == null) return;
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, triggerCol.size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            HandleHitTarget(hits[i]);
        }
    }

    private void HandleHitTarget(Collider2D col)
    {
        if (col == null || damagedTargetsThisCycle.Contains(col)) return;

        // 1. Gây sát thương Player
        if (col.CompareTag("Player") || col.GetComponent<PlayerHealth>() != null)
        {
            var ph = col.GetComponent<PlayerHealth>() ?? col.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                damagedTargetsThisCycle.Add(col);
                ph.TakeDamage(playerDamage);
                DamagePopup.Create(col.bounds.center + Vector3.up * 0.6f, playerDamage, isPlayer: true);
                SoundManager.EnsureInstance()?.PlayHitImpact();
                var cam = Object.FindAnyObjectByType<CameraFollow>();
                if (cam != null) cam.Shake(0.2f, 0.35f);
                return;
            }
        }

        // 2. Gây sát thương Quái vật (Tactical 2-way damage)
        var dmgable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
        if (dmgable != null && (dmgable as Component) != null)
        {
            var comp = dmgable as Component;
            if (comp.CompareTag("Player")) return;

            damagedTargetsThisCycle.Add(col);
            dmgable.TakeDamage(enemyDamage);
            DamagePopup.Create(comp.transform.position + Vector3.up * 0.7f, enemyDamage, isPlayer: false);
            SoundManager.EnsureInstance()?.PlayHitImpact();

            // Áp dụng Stun nhẹ 0.45s cho quái
            EnemyStunEffect.ApplyStun(comp.gameObject, 0.45f);
        }
    }

    private void SpawnSpikeDustFX()
    {
        GameObject fx = new GameObject("SpikeDustFX");
        fx.transform.position = transform.position;
        var fSr = fx.AddComponent<SpriteRenderer>();
        fSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        fSr.color = new Color(0.85f, 0.85f, 0.9f, 0.6f);
        fSr.sortingOrder = 1500;
        fx.transform.localScale = Vector3.one * 0.6f;

        StartCoroutine(DustFadeRoutine(fx, fSr));
    }

    private IEnumerator DustFadeRoutine(GameObject fx, SpriteRenderer fSr)
    {
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float progress = t / 0.25f;
            if (fx != null)
            {
                fx.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.6f, progress);
                if (fSr != null) fSr.color = new Color(0.85f, 0.85f, 0.9f, Mathf.Lerp(0.6f, 0f, progress));
            }
            yield return null;
        }
        if (fx != null) Destroy(fx);
    }

    private static void EnsureSprites()
    {
        if (cachedRetractedSprite == null) cachedRetractedSprite = GenerateProceduralSpikeSprite(TrapState.Retracted);
        if (cachedWarningSprite == null) cachedWarningSprite = GenerateProceduralSpikeSprite(TrapState.Warning);
        if (cachedActiveSprite == null) cachedActiveSprite = GenerateProceduralSpikeSprite(TrapState.Active);
    }

    private static Sprite GenerateProceduralSpikeSprite(TrapState state)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // Crisp retro pixel art

        Color stoneDark = new Color(0.18f, 0.20f, 0.24f, 1f);
        Color stonePlate = new Color(0.32f, 0.35f, 0.42f, 1f);
        Color holeDark = new Color(0.08f, 0.09f, 0.12f, 1f);
        Color steelSpike = new Color(0.85f, 0.88f, 0.95f, 1f);
        Color steelShadow = new Color(0.45f, 0.48f, 0.55f, 1f);
        Color rustRed = new Color(0.65f, 0.25f, 0.18f, 1f);

        // Vẽ phiến đá nền (Stone Plate)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = (x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2);
                tex.SetPixel(x, y, isBorder ? stoneDark : stonePlate);
            }
        }

        // 4 cụm lỗ gai trên phiến đá
        int[] holeXs = { 7, 21, 7, 21 };
        int[] holeYs = { 7, 7, 21, 21 };

        for (int i = 0; i < 4; i++)
        {
            int hx = holeXs[i];
            int hy = holeYs[i];

            // Đục lỗ tròn 5x5
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    tex.SetPixel(hx + dx, hy + dy, holeDark);
                }
            }

            // Vẽ mũi gai tùy theo trạng thái
            if (state == TrapState.Warning)
            {
                // Mũi nhọn nhú nhẹ 2px
                tex.SetPixel(hx, hy + 1, steelSpike);
                tex.SetPixel(hx - 1, hy, steelShadow);
                tex.SetPixel(hx + 1, hy, steelShadow);
            }
            else if (state == TrapState.Active)
            {
                // Mũi gai thép nhọn hoắt nhô cao 6px
                for (int h = 0; h <= 6; h++)
                {
                    int width = (6 - h) / 2;
                    for (int w = -width; w <= width; w++)
                    {
                        Color c = (h >= 5) ? steelSpike : (w == -width ? steelShadow : steelSpike);
                        if (h == 2) c = rustRed; // Vệt rỉ sét/máu tàn tích
                        tex.SetPixel(hx + w, hy + h - 1, c);
                    }
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
    }

    public static FloorSpikeTrap Create(Vector3 worldPos, Transform parent = null)
    {
        GameObject go = new GameObject("FloorSpikeTrap");
        go.transform.position = worldPos;
        if (parent != null) go.transform.SetParent(parent, true);
        return go.AddComponent<FloorSpikeTrap>();
    }
}
