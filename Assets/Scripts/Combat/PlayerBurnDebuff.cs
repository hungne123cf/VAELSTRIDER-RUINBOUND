using UnityEngine;
using System.Collections;

/// <summary>
/// Hiệu ứng THIÊU ĐỐT (Burn Debuff) dành cho Player khi trúng Thiên thạch hoặc đạp vào Vũng dung nham:
/// - Gây sát thương theo % máu (mặc định 3.5% máu tối đa mỗi giây) trong thời gian debuff.
/// - Nhấp nháy màu cam rực lửa trên Sprite và phát ra tàn lửa thiêu đốt.
/// - Đi kèm với Vết Thương Sâu (Grievous Wounds) giảm 50% hồi máu.
/// </summary>
public class PlayerBurnDebuff : MonoBehaviour
{
    private float remainingDuration = 0f;
    private float tickTimer = 0f;
    private float percentHpPerSec = 0.035f;
    private PlayerHealth playerHealth;
    private SpriteRenderer playerSr;
    private GameObject fireAuraObj;

    public static PlayerBurnDebuff Apply(GameObject playerObj, float duration = 3.5f, float percentPerSec = 0.035f)
    {
        if (playerObj == null) return null;
        var pHealth = playerObj.GetComponent<PlayerHealth>() ?? playerObj.GetComponentInParent<PlayerHealth>();
        if (pHealth == null || pHealth.IsDead) return null;

        var debuff = pHealth.GetComponent<PlayerBurnDebuff>();
        bool isNew = (debuff == null);
        if (isNew)
        {
            debuff = pHealth.gameObject.AddComponent<PlayerBurnDebuff>();
        }

        debuff.InitOrRefresh(duration, percentPerSec, isNew);
        return debuff;
    }

    private void InitOrRefresh(float duration, float percent, bool isNew)
    {
        remainingDuration = Mathf.Max(remainingDuration, duration);
        percentHpPerSec = Mathf.Max(percentHpPerSec, percent);

        if (isNew)
        {
            playerHealth = GetComponent<PlayerHealth>();
            playerSr = GetComponent<SpriteRenderer>();
            SpawnFireAura();
            DamagePopup.CreateText(transform.position + Vector3.up * 1.6f, LocalizationManager.T("THIÊU ĐỐT!", "BURNING!"), new Color(1.0f, 0.45f, 0.1f));
        }
    }

    private void SpawnFireAura()
    {
        if (fireAuraObj != null) return;
        fireAuraObj = new GameObject("PlayerBurnFX");
        fireAuraObj.transform.SetParent(transform, false);
        fireAuraObj.transform.localPosition = Vector3.zero;

        var sr = fireAuraObj.AddComponent<SpriteRenderer>();
        sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sr.color = new Color(1.0f, 0.4f, 0.05f, 0.45f);
        sr.sortingOrder = (playerSr != null) ? playerSr.sortingOrder - 1 : 4900;
        fireAuraObj.transform.localScale = Vector3.one * 1.4f;
    }

    void Update()
    {
        if (playerHealth == null || playerHealth.IsDead)
        {
            Cleanup();
            return;
        }

        remainingDuration -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (fireAuraObj != null)
        {
            float pulse = 1.2f + Mathf.Sin(Time.time * 16f) * 0.25f;
            fireAuraObj.transform.localScale = Vector3.one * pulse;
        }

        if (tickTimer <= 0f)
        {
            tickTimer = 0.6f;
            int tickDmg = Mathf.Max(1, Mathf.RoundToInt(playerHealth.MaxHealth * percentHpPerSec * 0.6f));
            playerHealth.TakeDamage(tickDmg);
            DamagePopup.CreateText(transform.position + Vector3.up * 0.7f, $"-{tickDmg}", new Color(1.0f, 0.42f, 0.12f));
        }

        if (remainingDuration <= 0f)
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (fireAuraObj != null) Destroy(fireAuraObj);
        Destroy(this);
    }
}
