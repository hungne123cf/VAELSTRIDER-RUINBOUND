using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Enemy & Boss CC Cleanse System (Hệ Thống Tự Giải Khống Chế Tự Thân)
/// 
/// NGUYÊN TẮC HOẠT ĐỘNG:
/// 1. Áp dụng cho TẤT CẢ Quái thường và Boss ở mọi Map trong game.
/// 2. Cơ chế Tự Thân Mỗi 3s (3.0s Auto-Cleanse):
///    - Nếu quái/Boss bị dính bất kỳ loại hiệu ứng khống chế nào (Choáng / Đóng Băng / Ngưng Đọng Thời Gian):
///    - Cứ mỗi 3.0s khống chế tích tụ, quái/Boss sẽ TỰ THÂN BỘC PHÁ HÀO QUANG THANH TẨY:
///      + Lập tức hóa giải Choáng (Stun).
///      + Lập tức giải phóng Đóng Băng (Freeze).
///      + Lập tức giải phóng Chậm / Ngưng Đọng (Chrono Slow).
///      + Tự động khôi phục toàn bộ các script AI di chuyển và tấn công nếu bị tắt lỡ dở.
///      + Khôi phục tốc độ Animator bình thường.
/// 3. HIỆU ỨNG HÌNH ẢNH & POPUP:
///    - Bùng nổ sóng hào quang ánh vàng kim rực rỡ (Cleanse Shockwave).
///    - Hiển thị Text Popup: "THANH TẨY (3S)!" / "CLEANSE (3S)!".
/// 4. KHUNG MIỄN NHIỄM HẬU THANH TẨY (CC IMMUNITY GRACE PERIOD):
///    - Sau khi tự giải khống chế, quái/Boss nhận 1.5 giây MIỄN NHIỄM KHỐNG CHẾ TUYỆT ĐỐI.
///    - Ngăn chặn hoàn toàn tình trạng bị người chơi đánh choáng chồng choáng (perma-stunlock) đứng đơ người,
///      đảm bảo quái luôn có cơ hội phản đòn, giữ độ kịch tính đỉnh cao cho trận đấu!
/// </summary>
public class EnemyCCCleanseSystem : MonoBehaviour
{
    public const float CLEANSE_CYCLE_DURATION = 3.0f; // Cứ mỗi 3.0s dính khống chế sẽ tự động giải
    public const float POST_CLEANSE_IMMUNITY_DURATION = 1.5f; // Miễn nhiễm 1.5s sau khi thanh tẩy

    private static readonly Dictionary<int, float> s_CCImmunityEndTimes = new Dictionary<int, float>();

    private float accumulatedCCTime = 0f;
    private float periodicCheckTimer = 0f;
    private Rigidbody2D rb;
    private Animator animator;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Tăng ma sát giảm chấn để quái/Boss không bao giờ bị trôi dạt vô tận trên mặt đất
            rb.linearDamping = Mathf.Max(rb.linearDamping, 4.0f);
        }
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    void Update()
    {
        bool isHardCC = CheckIsUnderHardCC();
        bool isAnyCC = isHardCC || CheckIsUnderAnyCCOrDisabled();

        if (isHardCC)
        {
            // Khi đang bị Choáng hoặc Đóng Băng: khóa cứng vận tốc = 0 để không bao giờ bị trôi dạt
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            // WATCHDOG MỖI FRAME: Khi KHÔNG bị Choáng / Đóng Băng, bắt buộc mọi script AI/Boss phải được bật lại ngay lập tức!
            RestoreAllAIScripts();
            if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (animator != null && animator.speed < 0.4f)
            {
                animator.speed = EnemyChronoSlowEffect.IsChronoSlowActive ? EnemyChronoSlowEffect.SLOW_FACTOR : 1.0f;
            }
        }

        // Cơ chế tự giải mọi khống chế mỗi 3.0s
        if (isAnyCC)
        {
            accumulatedCCTime += Time.deltaTime;
            if (accumulatedCCTime >= CLEANSE_CYCLE_DURATION)
            {
                TriggerAutoCleanse();
            }
        }
        else
        {
            if (accumulatedCCTime > 0f)
            {
                accumulatedCCTime = Mathf.Max(0f, accumulatedCCTime - (Time.deltaTime * 0.5f));
            }
        }

        // Chu kỳ tự kiểm tra bảo hiểm mỗi 3.0s
        periodicCheckTimer += Time.deltaTime;
        if (periodicCheckTimer >= CLEANSE_CYCLE_DURATION)
        {
            periodicCheckTimer = 0f;
            if (CheckIsUnderHardCC())
            {
                TriggerAutoCleanse();
            }
        }
    }

    public bool CheckIsUnderHardCC()
    {
        var stun = GetComponent<EnemyStunEffect>();
        if (stun != null && stun.IsStunned) return true;

        var freeze = GetComponent<EnemyFreezeEffect>();
        if (freeze != null && freeze.IsFrozen) return true;

        return false;
    }

    /// <summary>
    /// Kiểm tra quái có đang bị khống chế bởi Stun, Freeze, ChronoSlow hoặc bị vô hiệu hóa script AI hay không
    /// </summary>
    public bool CheckIsUnderCC()
    {
        return CheckIsUnderAnyCCOrDisabled();
    }

    private bool CheckIsUnderAnyCCOrDisabled()
    {
        if (CheckIsUnderHardCC()) return true;

        var slow = GetComponent<EnemyChronoSlowEffect>();
        if (slow != null) return true;

        var allBehaviours = GetComponents<MonoBehaviour>();
        foreach (var mb in allBehaviours)
        {
            if (mb == null || mb == this) continue;
            string typeName = mb.GetType().Name;
            if (typeName.EndsWith("AI") || typeName.EndsWith("Enemy") || typeName.Contains("Boss"))
            {
                if (!mb.enabled) return true;
            }
        }

        if (animator != null && animator.speed < 0.4f) return true;

        return false;
    }

    /// <summary>
    /// Kích hoạt cơ chế Tự Thân Thanh Tẩy (Cleanse Breakout)
    /// </summary>
    public void TriggerAutoCleanse()
    {
        accumulatedCCTime = 0f;

        // 1. Giải phóng Stun
        var stun = GetComponent<EnemyStunEffect>();
        if (stun != null)
        {
            stun.RemoveStun();
        }

        // 2. Giải phóng Freeze
        var freeze = GetComponent<EnemyFreezeEffect>();
        if (freeze != null)
        {
            freeze.RemoveFreeze();
        }

        // 3. Giải phóng Chrono Slow
        var slow = GetComponent<EnemyChronoSlowEffect>();
        if (slow != null)
        {
            slow.RestoreAndDestroy();
        }

        // 4. Khôi phục tất cả script AI nếu bị tắt
        RestoreAllAIScripts();

        // 5. Khôi phục Animator
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (animator != null && animator.speed < 0.5f)
        {
            animator.speed = 1.0f;
        }

        // 6. Gán thời gian miễn nhiễm khống chế 1.5s
        int targetId = gameObject.GetInstanceID();
        s_CCImmunityEndTimes[targetId] = Time.time + POST_CLEANSE_IMMUNITY_DURATION;

        // 7. Hiệu ứng âm thanh & Hào quang thanh tẩy bùng nổ (Cleanse Shockwave)
        SpawnCleanseShockwaveFX();
        SoundManager.EnsureInstance()?.PlayParrySuccess();

        // 8. Hiển thị Popup chữ rực rỡ "THANH TẨY (3S)!"
        Vector3 headPos = EnemyStunEffect.GetHeadTopWorldPosition(gameObject);
        DamagePopup.CreateText(headPos, LocalizationManager.T("THANH TẨY (3S)!", "CLEANSE (3S)!"), new Color(1f, 0.92f, 0.25f));
    }

    private void RestoreAllAIScripts()
    {
        var allBehaviours = GetComponents<MonoBehaviour>();
        foreach (var mb in allBehaviours)
        {
            if (mb == null || mb == this) continue;
            string typeName = mb.GetType().Name;
            if (typeName.EndsWith("AI") || typeName.EndsWith("Enemy") || typeName.Contains("Boss") || typeName.Contains("Spawner"))
            {
                if (!mb.enabled) mb.enabled = true;
            }
        }
    }

    private void SpawnCleanseShockwaveFX()
    {
        GameObject fx = new GameObject("CleanseShockwaveFX");
        fx.transform.position = transform.position;
        var fSr = fx.AddComponent<SpriteRenderer>();
        fSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        fSr.color = new Color(1f, 0.95f, 0.45f, 0.85f);
        fSr.sortingOrder = 5300;

        StartCoroutine(CleanseBurstRoutine(fx, fSr));
    }

    private IEnumerator CleanseBurstRoutine(GameObject fx, SpriteRenderer sr)
    {
        float duration = 0.32f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.4f;
        Vector3 endScale = Vector3.one * 3.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (fx != null)
            {
                fx.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                if (sr != null)
                {
                    sr.color = new Color(1f, 0.95f, 0.45f, Mathf.Lerp(0.85f, 0f, t));
                }
            }
            yield return null;
        }

        if (fx != null) Destroy(fx);
    }

    /// <summary>
    /// Gắn hệ thống Cleanse vào GameObject mục tiêu nếu chưa có
    /// </summary>
    public static EnemyCCCleanseSystem EnsureAttached(GameObject target)
    {
        if (target == null) return null;
        var sys = target.GetComponent<EnemyCCCleanseSystem>();
        if (sys == null)
        {
            sys = target.AddComponent<EnemyCCCleanseSystem>();
        }
        return sys;
    }

    /// <summary>
    /// Kiểm tra xem quái vật/Boss có đang trong thời gian Miễn Nhiễm Khống Chế hậu thanh tẩy hay không
    /// </summary>
    public static bool IsCCImmune(GameObject target)
    {
        if (target == null) return false;
        int id = target.GetInstanceID();
        if (s_CCImmunityEndTimes.TryGetValue(id, out float endTime))
        {
            if (Time.time < endTime) return true;
        }
        return false;
    }

    void OnDestroy()
    {
        int id = gameObject.GetInstanceID();
        s_CCImmunityEndTimes.Remove(id);
    }
}

