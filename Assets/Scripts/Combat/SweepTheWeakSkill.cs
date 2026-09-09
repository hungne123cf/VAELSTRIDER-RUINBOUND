using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SweepTheWeakSkill : MonoBehaviour
{
    [Header("Skill Parameters")]
    [SerializeField] private float skillRadius = 4.8f;
    [SerializeField] private float coneAngle = 140f;
    [SerializeField] private int bossDamage = 250;
    [SerializeField] private float bossStunDuration = 2.5f;
    [SerializeField] private LayerMask enemyLayers;

    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private static Sprite cachedSlashSprite;

    private readonly string[] maidQuotesVN = new string[]
    {
        "Dọn dẹp phế tích... là bổn phận của ta!",
        "Tuyệt kỹ Quét Tan Kẻ Yếu! Tuyệt đối không lưu tình!",
        "Trước lưỡi kiếm của ta, các ngươi chỉ là bụi bặm!",
        "Nhát chém này... tiễn ngươi về cõi hư vô!",
        "Rác rưởi ngáng đường... Biến mất mau!"
    };

    private readonly string[] maidQuotesEN = new string[]
    {
        "Cleaning up the ruins... is my sacred duty!",
        "Sweep the Weak! Absolute execution, no mercy!",
        "Before my razor blade, you are mere dust!",
        "This crescent strike... sends you to the void!",
        "Filth in my path... Be swept away!"
    };

    private int quoteIdx = 0;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        if (enemyLayers == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy");
            if (enemyLayers == 0) enemyLayers = ~0;
        }

        if (cachedSlashSprite == null)
        {
            Texture2D tex = CreateSharpCrescentTexture();
            cachedSlashSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.15f), 100);
        }
    }

    public IEnumerator ExecuteSkillRoutine()
    {
        var cam = FindAnyObjectByType<CameraFollow>();
        var anim = GetComponent<Animator>();
        var health = GetComponent<PlayerHealth>();

        try
        {
            // 1. Khoa di chuyen & bat bat tu trong luc tich tu tuyet ky
            if (health != null) health.isInvulnerable = true;
            if (playerController != null) playerController.SetCanMove(false);

            // ====================================================
            // GIAI DOAN 1: HOAT ANH ZOOM-IN DIEN ANH & CAU THOAI NGAU
            // ====================================================
            Time.timeScale = 0.05f; // Slow-motion cinematic zoom

            // Camera zoom can canh vao nhan vat Maid
            if (cam != null) cam.SetCinematicZoom(2.6f);

            // Phat am thanh thuc tinh nang luong & voice cau thoai
            SoundManager.EnsureInstance()?.PlayPortalSpawn();

            // Bat Hieu Ung Anh Mat Ruc Lua (Eye Flare VFX) tren mat nhan vat
            var eyeFlare = GetComponent<EyeFlareFX>();
            if (eyeFlare == null) eyeFlare = gameObject.AddComponent<EyeFlareFX>();
            eyeFlare.TriggerEyeFlare(0.85f, isMaid: true);

            // Hien thi Letterbox den dien anh & Banner cau thoai chat cua Maid
            var cutsceneUI = UltiCinematicUI.EnsureInstance();
            if (cutsceneUI != null)
            {
                bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;
                string speaker = isVi ? "MAID CHIẾN BINH" : "BATTLE MAID";
                string[] quotes = isVi ? maidQuotesVN : maidQuotesEN;
                string quote = quotes[quoteIdx % quotes.Length];
                quoteIdx++;

                cutsceneUI.ShowCutscene(0.85f, speaker, quote, isMaid: true);
            }

            // Cho 0.85s thoi gian thuc de nguoi choi thuong thuc hoat anh dien anh
            yield return new WaitForSecondsRealtime(0.85f);
        }
        finally
        {
            // Luon dam bao thoi gian tro lai 1.0f ngay sau cutscene
            Time.timeScale = 1.0f;
        }

        // ====================================================
        // GIAI DOAN 2: THOI GIAN TRO LAI & VUNG NHAT CHEM HUY DIET
        // ====================================================
        Time.timeScale = 1.0f;
        if (cam != null)
        {
            cam.ResetZoom();
            cam.Shake(0.5f, 0.65f);
        }

        if (anim != null)
        {
            anim.Play("Player_Ulti", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();
        SoundManager.EnsureInstance()?.PlayHitImpact();

        Vector2 facingDir = playerController != null ? playerController.GetFacingDirection() : Vector2.down;
        if (facingDir == Vector2.zero) facingDir = Vector2.down;
        Vector3 origin = transform.position + new Vector3(0f, 0.1f, 0f);

        // Phong duong song kiem khi sac ben HD 512x512 ruc sang ve phia truoc
        SpawnSharpSlashWave(origin, facingDir);

        // Luot nhe ve phia truoc theo huong chem
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = facingDir * 5.5f;
        }

        // Quet va gay sat thuong trong pham vi hinh non
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, skillRadius, enemyLayers);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        int killedCount = 0;

        foreach (var col in colliders)
        {
            if (col == null) continue;
            Vector2 toTarget = ((Vector2)col.transform.position - (Vector2)origin).normalized;
            float angle = Vector2.Angle(facingDir, toTarget);

            if (angle <= coneAngle * 0.5f)
            {
                var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (damageable != null && !damagedTargets.Contains(damageable))
                {
                    damagedTargets.Add(damageable);

                    bool isBoss = false;
                    var botAI = col.GetComponent<BotWheelAI>() ?? col.GetComponentInParent<BotWheelAI>();
                    if (botAI != null && (botAI.IsBoss || botAI.isBoss))
                    {
                        isBoss = true;
                    }
                    else if (col.gameObject.name.ToLower().Contains("boss") || col.gameObject.name.ToLower().Contains("titan") || col.gameObject.name.ToLower().Contains("gormok") || col.GetComponent<BloodMonsterAI>()?.IsBoss == true)
                    {
                        isBoss = true;
                    }

                    if (!isBoss)
                    {
                        damageable.TakeDamage(9999);
                        DamagePopup.Create(col.transform.position, 9999, isCritical: true);
                        killedCount++;
                    }
                    else
                    {
                        damageable.TakeDamage(bossDamage);
                        DamagePopup.Create(col.transform.position, bossDamage, isCritical: true);

                        var rbEnemy = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                        if (rbEnemy != null)
                        {
                            rbEnemy.AddForce(toTarget * 16f, ForceMode2D.Impulse);
                        }
                    }
                }
            }
        }

        // Camera impact feedback manh me thay vi dung timeScale de tranh xung dot
        if (damagedTargets.Count > 0 && cam != null)
        {
            cam.Shake(0.35f, 0.45f);
        }

        // Khoi phuc trang thai nguoi choi
        yield return new WaitForSeconds(0.2f);
        Time.timeScale = 1.0f;
        if (cam != null) cam.ResetZoom();
        if (health != null) health.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);
    }

    private void SpawnSharpSlashWave(Vector3 origin, Vector2 direction)
    {
        if (cachedSlashSprite == null)
        {
            Texture2D tex = CreateSharpCrescentTexture();
            cachedSlashSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.15f), 100);
        }

        // 1. Song kiem chinh (Sac ben, vien hong tim neon, loi trang sang loa)
        GameObject mainWave = new GameObject("SharpSlashWave_Main");
        mainWave.transform.position = origin + (Vector3)(direction * 0.8f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        mainWave.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        var sr1 = mainWave.AddComponent<SpriteRenderer>();
        sr1.sprite = cachedSlashSprite;
        sr1.color = new Color(1f, 0.35f, 0.65f, 1f);
        sr1.sortingOrder = 3000;

        // 2. Song phu loi trang tinh quang chong len tao do choi
        GameObject coreWave = new GameObject("SharpSlashWave_Core");
        coreWave.transform.SetParent(mainWave.transform, false);
        coreWave.transform.localPosition = Vector3.zero;
        coreWave.transform.localRotation = Quaternion.identity;
        coreWave.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var sr2 = coreWave.AddComponent<SpriteRenderer>();
        sr2.sprite = cachedSlashSprite;
        sr2.color = new Color(1f, 1f, 1f, 0.95f);
        sr2.sortingOrder = 3001;

        // Animate song bay phong ve phia truoc
        StartCoroutine(AnimateSlashProjectile(mainWave, sr1, sr2, direction));
    }

    private IEnumerator AnimateSlashProjectile(GameObject waveObj, SpriteRenderer sr1, SpriteRenderer sr2, Vector2 direction)
    {
        float duration = 0.35f;
        float elapsed = 0f;

        Vector3 startPos = waveObj.transform.position;
        Vector3 endPos = startPos + (Vector3)(direction * 3.4f);

        Vector3 startScale = new Vector3(1.8f, 1.2f, 1f);
        Vector3 endScale = new Vector3(5.2f, 3.6f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

            if (waveObj != null)
            {
                waveObj.transform.position = Vector3.Lerp(startPos, endPos, easeT);
                waveObj.transform.localScale = Vector3.Lerp(startScale, endScale, easeT);
            }

            float alpha = Mathf.Clamp01(1f - (t * t));
            if (sr1 != null)
            {
                Color c1 = sr1.color;
                c1.a = alpha;
                sr1.color = c1;
            }
            if (sr2 != null)
            {
                Color c2 = sr2.color;
                c2.a = alpha * 0.95f;
                sr2.color = c2;
            }

            yield return null;
        }

        if (waveObj != null) Destroy(waveObj);
    }

    private Texture2D CreateSharpCrescentTexture()
    {
        int w = 512;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                float nx = (x - w * 0.5f) / (w * 0.5f);
                float dist = Mathf.Sqrt(nx * nx + ny * ny);

                if (dist >= 0.68f && dist <= 0.96f && ny >= 0.12f)
                {
                    float mid = (0.68f + 0.96f) * 0.5f;
                    float halfThick = (0.96f - 0.68f) * 0.5f;
                    float distFromMid = Mathf.Abs(dist - mid) / halfThick;

                    float edgeSharpness = Mathf.Clamp01((1f - distFromMid) * 2.5f);
                    float arcWidth = Mathf.Clamp01(1f - (nx * nx * 1.15f));
                    float tipFade = Mathf.SmoothStep(0f, 1f, arcWidth);

                    float alpha = edgeSharpness * tipFade;
                    float coreBright = Mathf.Clamp01(1f - distFromMid * 1.5f);
                    Color col = Color.Lerp(new Color(1f, 0.4f, 0.7f, alpha), new Color(1f, 1f, 1f, alpha), coreBright);

                    tex.SetPixel(x, y, col);
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
