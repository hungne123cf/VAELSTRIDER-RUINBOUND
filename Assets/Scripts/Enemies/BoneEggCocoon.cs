using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class BoneEggCocoon : MonoBehaviour, IDamageable
{
    public static event System.Action<Vector3> OnEggHatched;

    [Header("Egg Stats")]
    [SerializeField] private int maxHealth = 120;
    [SerializeField] private int currentHealth;

    private SpriteRenderer sr;
    private CircleCollider2D col;
    private bool isDestroyed = false;

    private Sprite[] idleSprites;
    private Sprite[] breakSprites;
    private Color originalColor;
    private Coroutine breatheRoutine;

    // Chi dan va UI
    private GameObject guidanceObj;
    private TextMeshPro guidanceTMP;
    private GameObject healthBarObj;
    private SpriteRenderer hpFillSr;
    private Light2D holyPillarLight;

    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        originalColor = sr.color;

        // Collider can giua tam trung, ban kinh 2.0m rong rai
        col.offset = new Vector2(0f, 0.35f);
        col.radius = 0.85f;
        col.isTrigger = false;

        // Layer Enemy (Layer 6) va Tag Enemy
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        gameObject.layer = (enemyLayer != -1) ? enemyLayer : 6;
        gameObject.tag = "Enemy";

        sr.sortingOrder = 1500;
        LoadEggSprites();
        SetupGuidanceVisuals();
    }

    private void LoadEggSprites()
    {
        idleSprites = new Sprite[4];
        breakSprites = new Sprite[4];

        for (int i = 0; i < 4; i++)
        {
#if UNITY_EDITOR
            idleSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Bosses/Skeleton King/SkeletonKing_Obstacle_{i}.png");
            breakSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Bosses/Skeleton King/SkeletonKing_Ostacle_Death_{i}.png");
#endif
        }

        if (idleSprites[0] != null) sr.sprite = idleSprites[0];
    }

    private void SetupGuidanceVisuals()
    {
        // 1. Mui ten chi dan & Dong chu nhap nhay tren dau (font vua phai, khong bi loi telex)
        guidanceObj = new GameObject("GuidancePointer");
        guidanceObj.transform.SetParent(transform, false);
        guidanceObj.transform.localPosition = new Vector3(0f, 2.6f, 0f);

        guidanceTMP = guidanceObj.AddComponent<TextMeshPro>();
        guidanceTMP.text = "<color=#FCD34D><b>[!] PHA HUY KEN PHONG AN [!]</b></color>\n<size=68%><color=#67E8F9>[Tan cong Ken de danh thuc Boss]</color></size>";
        guidanceTMP.fontSize = 2.4f;
        guidanceTMP.alignment = TextAlignmentOptions.Center;
        guidanceTMP.sortingOrder = 4000;

        // 2. Thanh mau mini tren dau qua trung
        healthBarObj = new GameObject("EggHealthBar");
        healthBarObj.transform.SetParent(transform, false);
        healthBarObj.transform.localPosition = new Vector3(0f, 1.9f, 0f);

        // Khung nen den (pivot center)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(healthBarObj.transform, false);
        var bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreateBoxSprite(new Vector2(0.5f, 0.5f));
        bgSr.color = new Color(0f, 0f, 0f, 0.85f);
        bgSr.sortingOrder = 3990;
        bgObj.transform.localScale = new Vector3(2.2f, 0.35f, 1f);

        // Thanh mau do (pivot trai de khi tru mau se rut tu phai sang trai)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarObj.transform, false);
        fillObj.transform.localPosition = new Vector3(-1.05f, 0f, 0f);

        hpFillSr = fillObj.AddComponent<SpriteRenderer>();
        hpFillSr.sprite = CreateBoxSprite(new Vector2(0f, 0.5f));
        hpFillSr.color = new Color(1f, 0.25f, 0.25f, 0.95f);
        hpFillSr.sortingOrder = 3995;
        fillObj.transform.localScale = new Vector3(2.1f, 0.25f, 1f);

        // 3. Cot sang huyen ao roi tu troi xuong qua trung
        GameObject lightObj = new GameObject("Egg_HolyLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = Vector3.zero;

        holyPillarLight = lightObj.AddComponent<Light2D>();
        holyPillarLight.lightType = Light2D.LightType.Point;
        holyPillarLight.color = new Color(0.35f, 0.85f, 1f, 1f);
        holyPillarLight.intensity = 2.8f;
        holyPillarLight.pointLightInnerRadius = 0.8f;
        holyPillarLight.pointLightOuterRadius = 4.5f;
    }

    private Sprite CreateBoxSprite(Vector2 pivot)
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), pivot, 4f);
    }

    void Start()
    {
        breatheRoutine = StartCoroutine(EggBreatheRoutine());
        DamagePopup.CreateText(transform.position + Vector3.up * 2f, "MUC TIEU: PHA HUY KEN PHONG AN!", Color.yellow);
    }

    void Update()
    {
        if (isDestroyed) return;

        // Mui ten chi dan dap nhe len xuong de thu hut chu y
        if (guidanceObj != null)
        {
            float bob = Mathf.Sin(Time.time * 5f) * 0.12f;
            guidanceObj.transform.localPosition = new Vector3(0f, 2.6f + bob, 0f);
        }

        // Anh sang nhip tho
        if (holyPillarLight != null)
        {
            holyPillarLight.intensity = 2.5f + Mathf.Sin(Time.time * 4f) * 0.8f;
        }
    }

    private IEnumerator EggBreatheRoutine()
    {
        int frame = 0;
        while (!isDestroyed)
        {
            if (idleSprites != null && idleSprites.Length > 0 && idleSprites[frame] != null)
            {
                sr.sprite = idleSprites[frame];
            }
            frame = (frame + 1) % 4;

            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
            transform.localScale = new Vector3(1.3f * pulse, 1.3f * pulse, 1f);

            yield return new WaitForSeconds(0.18f);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;
        StartCoroutine(DamageFlashRoutine());

        // Cap nhat thanh mau mini (rut tu phai sang trai nho pivot mep trai)
        if (hpFillSr != null)
        {
            float pct = Mathf.Clamp01((float)currentHealth / maxHealth);
            hpFillSr.transform.localScale = new Vector3(2.1f * pct, 0.25f, 1f);
            hpFillSr.color = Color.Lerp(Color.red, Color.green, pct);
        }

        DamagePopup.Create(transform.position + Vector3.up * 0.8f, damage, false);
        SoundManager.EnsureInstance()?.PlayHitImpact();

        if (currentHealth <= 0)
        {
            StartCoroutine(BreakEggRoutine());
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (sr != null)
        {
            sr.color = new Color(1f, 0.4f, 0.4f, 0.9f);
            yield return new WaitForSeconds(0.08f);
            sr.color = originalColor;
        }
    }

    private IEnumerator BreakEggRoutine()
    {
        isDestroyed = true;
        if (breatheRoutine != null) StopCoroutine(breatheRoutine);
        if (col != null) col.enabled = false;
        if (guidanceObj != null) Destroy(guidanceObj);
        if (healthBarObj != null) Destroy(healthBarObj);

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.6f, 0.9f);

        DamagePopup.CreateText(transform.position + Vector3.up * 2.2f, "KEN PHONG AN VO VUN!", Color.yellow);
        SoundManager.EnsureInstance()?.PlayHitImpact();

        for (int i = 0; i < 4; i++)
        {
            if (breakSprites != null && breakSprites[i] != null)
            {
                sr.sprite = breakSprites[i];
            }
            yield return new WaitForSeconds(0.12f);
        }

        yield return new WaitForSeconds(0.1f);

        Vector3 spawnPos = transform.position;
        OnEggHatched?.Invoke(spawnPos);

        Destroy(gameObject);
    }
}