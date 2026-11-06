using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Interactive Altar Shrine (Đài Tế Thánh & Đài Hiến Tế)
/// 
/// ĐẶC TÍNH:
/// 1. Map 1: Sanctuary Blessing Shrine (Đài Tế Thánh Thể)
///    - Cầu nguyện: Hồi 35% Max HP + Ban phước tăng +15% Tốc độ chạy & +20% Tốc độ đánh trong 60s!
/// 2. Map 2: Blood Sacrifice Altar (Đài Hiến Tế Huyết Nguyệt)
///    - Hiến tế 25 HP để đổi lấy 35 Mảnh Aethel + 1 Rơi Trang Bị Cực Phẩm ngẫu nhiên!
/// 3. Hiển thị nhắc nhở tương tác nổi khi người chơi tiến lại gần bán kính 2.2m.
/// </summary>
public class InteractiveAltarShrine : MonoBehaviour
{
    public enum AltarType { SanctuaryBlessing, BloodSacrifice }

    [SerializeField] private AltarType altarType = AltarType.SanctuaryBlessing;
    private bool isUsed = false;
    private bool isPlayerNearby = false;

    private Transform playerTransform;
    private PlayerHealth playerHealth;

    private SpriteRenderer sr;
    private GameObject promptUIObj;
    private TextMeshPro promptText;
    private GameObject runeAuraRing;

    private static Sprite cachedSanctuarySprite;
    private static Sprite cachedBloodAltarSprite;

    void Awake()
    {
        sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1650;

        EnsureSprites();
        UpdateAltarVisuals();
        CreatePromptUI();
        CreateAuraRing();
    }

    void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null)
        {
            playerTransform = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (isUsed) return;

        if (playerTransform == null)
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null)
            {
                playerTransform = p.transform;
                playerHealth = p.GetComponent<PlayerHealth>();
            }
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool nowNearby = dist <= 2.2f;

        if (nowNearby != isPlayerNearby)
        {
            isPlayerNearby = nowNearby;
            if (promptUIObj != null) promptUIObj.SetActive(isPlayerNearby);
        }

        // Kiểm tra bấm phím hoặc chạm màn hình để tương tác
        if (isPlayerNearby)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                Interact();
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Vector3 touchWorld = Camera.main != null ? Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position) : Vector3.zero;
                if (Vector2.Distance(touchWorld, transform.position) <= 2.0f)
                {
                    Interact();
                }
            }
        }

        // Xoay nhẹ vòng ma pháp rune
        if (runeAuraRing != null)
        {
            runeAuraRing.transform.Rotate(0f, 0f, (altarType == AltarType.SanctuaryBlessing ? 45f : -60f) * Time.deltaTime);
        }
    }

    public void Setup(AltarType type)
    {
        altarType = type;
        UpdateAltarVisuals();
        UpdatePromptText();
    }

    private void Interact()
    {
        if (isUsed) return;
        isUsed = true;

        if (promptUIObj != null) promptUIObj.SetActive(false);
        if (runeAuraRing != null) Destroy(runeAuraRing);

        if (altarType == AltarType.SanctuaryBlessing)
        {
            ExecuteSanctuaryBlessing();
        }
        else
        {
            ExecuteBloodSacrifice();
        }

        // Làm mờ đài tế sau khi dùng
        if (sr != null) sr.color = new Color(0.6f, 0.6f, 0.65f, 0.75f);
    }

    private void ExecuteSanctuaryBlessing()
    {
        SoundManager.EnsureInstance()?.PlayParrySuccess();

        // 1. Hồi máu 35% Max HP
        if (playerHealth != null)
        {
            int healAmt = Mathf.RoundToInt(playerHealth.MaxHealth * 0.35f);
            playerHealth.Heal(healAmt);
            DamagePopup.CreateText(playerTransform.position + Vector3.up * 0.8f, $"+{healAmt} HP", new Color(0.25f, 1f, 0.4f));
        }

        // 2. Ban Phước: Tăng tốc chạy và tốc đánh
        var pc = playerTransform != null ? playerTransform.GetComponent<PlayerController>() : null;
        if (pc != null)
        {
            pc.ApplySpeedBuff(1.20f, 60f);
        }

        // 3. Hiệu ứng cột sáng vàng kim
        SpawnBlessingPillarFX(new Color(1f, 0.92f, 0.35f, 0.9f));
        DamagePopup.CreateText(transform.position + Vector3.up * 1.4f, LocalizationManager.T("BAN PHƯỚC THÁNH THỂ!", "SANCTUARY BLESSING!"), Color.yellow);
    }

    private void ExecuteBloodSacrifice()
    {
        SoundManager.EnsureInstance()?.PlayHitImpact();

        // 1. Hiến tế 25 HP
        if (playerHealth != null)
        {
            int sacrifice = Mathf.Min(25, playerHealth.CurrentHealth - 1);
            if (sacrifice > 0)
            {
                playerHealth.TakeDamage(sacrifice);
                DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, sacrifice, isPlayer: true);
            }
        }

        // 2. Rơi phần thưởng cực phẩm
        AethelShard.SpawnShards(transform.position + Vector3.down * 0.6f, 35);
        WorldItemPickup.RollDrop(transform.position + Vector3.right * 0.8f, true);

        // 3. Hiệu ứng bộc phá huyết nguyệt
        SpawnBlessingPillarFX(new Color(0.9f, 0.12f, 0.2f, 0.9f));
        DamagePopup.CreateText(transform.position + Vector3.up * 1.4f, LocalizationManager.T("KHẾ ƯỚC HUYẾT NGUYỆT!", "BLOOD PACT!"), new Color(1f, 0.3f, 0.3f));
    }

    private void SpawnBlessingPillarFX(Color col)
    {
        GameObject fx = new GameObject("BlessingPillarFX");
        fx.transform.position = transform.position;
        var fSr = fx.AddComponent<SpriteRenderer>();
        fSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        fSr.color = col;
        fSr.sortingOrder = 5500;

        StartCoroutine(PillarRiseRoutine(fx, fSr, col));
    }

    private IEnumerator PillarRiseRoutine(GameObject fx, SpriteRenderer fSr, Color baseCol)
    {
        float dur = 0.5f;
        float elapsed = 0f;
        Vector3 orig = fx.transform.position;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (fx != null)
            {
                fx.transform.localScale = new Vector3(Mathf.Lerp(1.5f, 0.2f, t), Mathf.Lerp(1.5f, 8.5f, t), 1f);
                fx.transform.position = orig + Vector3.up * (t * 2.5f);
                if (fSr != null)
                {
                    Color c = baseCol;
                    c.a = Mathf.Lerp(baseCol.a, 0f, t);
                    fSr.color = c;
                }
            }
            yield return null;
        }

        if (fx != null) Destroy(fx);
    }

    private void CreateAuraRing()
    {
        runeAuraRing = new GameObject("AltarAuraRing");
        runeAuraRing.transform.SetParent(transform, false);
        runeAuraRing.transform.localPosition = new Vector3(0f, -0.4f, 0f);

        var aSr = runeAuraRing.AddComponent<SpriteRenderer>();
        aSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        aSr.sortingOrder = 1600;
        runeAuraRing.transform.localScale = new Vector3(2.4f, 1.2f, 1f);

        if (altarType == AltarType.SanctuaryBlessing)
        {
            aSr.color = new Color(1f, 0.88f, 0.3f, 0.35f);
        }
        else
        {
            aSr.color = new Color(0.85f, 0.15f, 0.25f, 0.40f);
        }
    }

    private void CreatePromptUI()
    {
        promptUIObj = new GameObject("AltarPromptUI");
        promptUIObj.transform.SetParent(transform, false);
        promptUIObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        promptText = promptUIObj.AddComponent<TextMeshPro>();
        promptText.fontSize = 3.6f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.sortingOrder = 6000;

        UpdatePromptText();
        promptUIObj.SetActive(false);
    }

    private void UpdatePromptText()
    {
        if (promptText == null) return;
        if (altarType == AltarType.SanctuaryBlessing)
        {
            promptText.color = new Color(1f, 0.95f, 0.4f);
            promptText.text = LocalizationManager.T("CẦU NGUYỆN\n[CHẠM / SPACE]", "PRAY AT SHRINE\n[TAP / SPACE]");
        }
        else
        {
            promptText.color = new Color(1f, 0.45f, 0.45f);
            promptText.text = LocalizationManager.T("HIẾN TẾ 25 HP\n[CHẠM / SPACE]", "SACRIFICE 25 HP\n[TAP / SPACE]");
        }
    }

    private void UpdateAltarVisuals()
    {
        if (sr == null) return;
        if (altarType == AltarType.SanctuaryBlessing)
        {
            sr.sprite = cachedSanctuarySprite;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = cachedBloodAltarSprite;
            sr.color = new Color(1f, 0.85f, 0.85f);
        }
    }

    private static void EnsureSprites()
    {
        if (cachedSanctuarySprite == null) cachedSanctuarySprite = GenerateAltarSprite(AltarType.SanctuaryBlessing);
        if (cachedBloodAltarSprite == null) cachedBloodAltarSprite = GenerateAltarSprite(AltarType.BloodSacrifice);
    }

    private static Sprite GenerateAltarSprite(AltarType type)
    {
        int w = 28;
        int h = 34;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color stoneBase = (type == AltarType.SanctuaryBlessing) ? new Color(0.38f, 0.42f, 0.50f, 1f) : new Color(0.25f, 0.12f, 0.15f, 1f);
        Color stoneDark = (type == AltarType.SanctuaryBlessing) ? new Color(0.22f, 0.25f, 0.32f, 1f) : new Color(0.14f, 0.06f, 0.08f, 1f);
        Color gemGlow = (type == AltarType.SanctuaryBlessing) ? new Color(1f, 0.92f, 0.35f, 1f) : new Color(0.95f, 0.15f, 0.25f, 1f);
        Color goldTrim = (type == AltarType.SanctuaryBlessing) ? new Color(0.95f, 0.78f, 0.25f, 1f) : new Color(0.75f, 0.35f, 0.15f, 1f);
        Color outline = new Color(0.08f, 0.08f, 0.10f, 1f);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            // Cấu trúc đài tế: Đế to (y=0..8), Thân thon (y=9..24), Đỉnh đài đỡ ngọc (y=25..33)
            int halfW = (y <= 7) ? 12 : (y <= 24 ? 7 : 11);

            for (int x = 0; x < w; x++)
            {
                int dist = Mathf.Abs(x - w / 2);
                if (dist > halfW)
                {
                    tex.SetPixel(x, y, clear);
                }
                else if (dist == halfW || y == 0 || y == h - 1)
                {
                    tex.SetPixel(x, y, outline);
                }
                else
                {
                    if (y == 7 || y == 24)
                    {
                        tex.SetPixel(x, y, goldTrim);
                    }
                    else
                    {
                        tex.SetPixel(x, y, (x < w / 2) ? stoneBase : stoneDark);
                    }
                }
            }
        }

        // Viên ngọc phát quang lơ lửng trên đỉnh đài tế (x=12..15, y=27..30)
        for (int gy = 27; gy <= 30; gy++)
        {
            for (int gx = 12; gx <= 15; gx++)
            {
                tex.SetPixel(gx, gy, gemGlow);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.35f), 24f);
    }

    public static InteractiveAltarShrine Create(Vector3 worldPos, AltarType type, Transform parent = null)
    {
        GameObject go = new GameObject($"AltarShrine_{type}");
        go.transform.position = worldPos;
        if (parent != null) go.transform.SetParent(parent, true);
        var altar = go.AddComponent<InteractiveAltarShrine>();
        altar.Setup(type);
        return altar;
    }
}

