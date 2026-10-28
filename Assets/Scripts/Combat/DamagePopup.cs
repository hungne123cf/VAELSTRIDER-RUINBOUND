using UnityEngine;
using System.Collections.Generic;

public enum DamageType
{
    Physical = 0,   // Sát thương Vật Lý (Đỏ Cam / Vàng Kim)
    Magic = 1,      // Sát thương Phép (Tím Tinh Tú)
    TrueDamage = 2  // Sát thương Chuẩn (Trắng Sáng Bạc Tinh Khiết)
}

/// <summary>
/// VAELSTRIDER: RUINBOUND - High-Performance Damage Popup Pool
/// Hệ thống số nhảy sát thương sử dụng Object Pooling 100%:
/// - Triệt tiêu hoàn toàn rác bộ nhớ (Zero GC Allocations) trong các pha combat đông quái
/// - Giới hạn tối đa 35 popup cùng lúc, tự động tái chế popup cũ nhất khi đánh dồn dập
/// - Chống lag giật tuyệt đối khi dùng Tuyệt Kỹ xoáy nhiều quái
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private static readonly Queue<DamagePopup> _pool = new Queue<DamagePopup>();
    private static readonly List<DamagePopup> _activePopups = new List<DamagePopup>();
    private const int MAX_ACTIVE_POPUPS = 35;

    private TextMesh textMesh;
    private TextMesh shadowMesh;
    private Color textColor;
    private Color shadowColor;
    private Vector3 velocity;
    private float gravity = 7.5f;
    private float lifeTimer = 0.75f;
    private float maxLifeTime = 0.75f;
    private bool isCrit = false;
    private Vector3 targetScale = Vector3.one;

    private static Transform _poolRoot;
    private static Transform PoolRoot
    {
        get
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("[DamagePopup_Pool]");
                Object.DontDestroyOnLoad(go);
                _poolRoot = go.transform;
            }
            return _poolRoot;
        }
    }

    public static DamagePopup Create(Vector3 position, int damageAmount, bool isCritical = false, bool isPlayer = false)
    {
        return Create(position, damageAmount, DamageType.Physical, isCritical, isPlayer);
    }

    public static DamagePopup Create(Vector3 position, int damageAmount, DamageType damageType, bool isCritical = false, bool isPlayer = false)
    {
        DamagePopup popup = GetFromPool();

        Vector3 spawnOffset = new Vector3(Random.Range(-0.30f, 0.30f), Random.Range(0.6f, 0.85f), 0f);
        popup.transform.position = position + spawnOffset;

        string displayText;
        Color mainColor;

        if (isPlayer)
        {
            displayText = damageAmount.ToString();
            mainColor = new Color(1f, 0.18f, 0.18f);
            popup.textMesh.fontSize = isCritical ? 36 : 28;
            popup.textMesh.characterSize = isCritical ? 0.085f : 0.072f;
        }
        else
        {
            if (isCritical)
            {
                displayText = damageAmount.ToString();
                popup.textMesh.fontSize = 32;
                popup.textMesh.characterSize = 0.080f;

                switch (damageType)
                {
                    case DamageType.TrueDamage:
                        mainColor = new Color(1f, 1f, 1f);
                        break;
                    case DamageType.Magic:
                        mainColor = new Color(0.92f, 0.45f, 1f);
                        break;
                    case DamageType.Physical:
                    default:
                        mainColor = new Color(1f, 0.85f, 0.12f);
                        break;
                }
            }
            else
            {
                displayText = damageAmount.ToString();
                popup.textMesh.fontSize = 28;
                popup.textMesh.characterSize = 0.072f;

                switch (damageType)
                {
                    case DamageType.TrueDamage:
                        mainColor = new Color(0.92f, 0.95f, 1f);
                        break;
                    case DamageType.Magic:
                        mainColor = new Color(0.76f, 0.35f, 1f);
                        break;
                    case DamageType.Physical:
                    default:
                        mainColor = new Color(1f, 0.60f, 0.15f);
                        break;
                }
            }
        }

        popup.textMesh.text = displayText;
        popup.shadowMesh.text = displayText;
        popup.shadowMesh.fontSize = popup.textMesh.fontSize;
        popup.shadowMesh.characterSize = popup.textMesh.characterSize;

        Color sCol = new Color(0f, 0f, 0f, 0.88f);
        popup.Setup(mainColor, sCol, isCritical);

        return popup;
    }

    public static DamagePopup CreateText(Vector3 position, string text, Color color)
    {
        DamagePopup popup = GetFromPool();

        popup.transform.position = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.7f, 0f);

        popup.textMesh.text = text;
        popup.textMesh.fontSize = 30;
        popup.textMesh.characterSize = 0.075f;

        popup.shadowMesh.text = text;
        popup.shadowMesh.fontSize = 30;
        popup.shadowMesh.characterSize = 0.075f;

        Color sCol = new Color(0f, 0f, 0f, 0.88f);
        popup.Setup(color, sCol, false);

        return popup;
    }

    private static DamagePopup GetFromPool()
    {
        DamagePopup popup = null;

        // 1. Lấy từ Pool nếu có sẵn
        while (_pool.Count > 0 && popup == null)
        {
            popup = _pool.Dequeue();
        }

        // 2. Nếu pool trống nhưng số lượng đang active vượt ngưỡng tối đa: tái chế con cũ nhất
        if (popup == null && _activePopups.Count >= MAX_ACTIVE_POPUPS)
        {
            popup = _activePopups[0];
            _activePopups.RemoveAt(0);
        }

        // 3. Nếu vẫn chưa có thì sinh mới 1 instance
        if (popup == null)
        {
            popup = InstantiatePopupInstance();
        }

        popup.gameObject.SetActive(true);
        _activePopups.Add(popup);
        return popup;
    }

    private static DamagePopup InstantiatePopupInstance()
    {
        GameObject popupObj = new GameObject("DamagePopup_Pooled");
        popupObj.transform.SetParent(PoolRoot, false);

        TextMesh tm = popupObj.AddComponent<TextMesh>();
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontStyle = FontStyle.Bold;

        MeshRenderer mr = popupObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Entities";
            mr.sortingOrder = 999;
        }

        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(popupObj.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.04f, -0.04f, 0.01f);

        TextMesh shadowTm = shadowObj.AddComponent<TextMesh>();
        shadowTm.alignment = TextAlignment.Center;
        shadowTm.anchor = TextAnchor.MiddleCenter;
        shadowTm.fontStyle = FontStyle.Bold;

        MeshRenderer smr = shadowObj.GetComponent<MeshRenderer>();
        if (smr != null)
        {
            smr.sortingLayerName = "Entities";
            smr.sortingOrder = 998;
        }

        DamagePopup comp = popupObj.AddComponent<DamagePopup>();
        comp.textMesh = tm;
        comp.shadowMesh = shadowTm;

        return comp;
    }

    private void Setup(Color color, Color sColor, bool critical)
    {
        textColor = color;
        shadowColor = sColor;
        isCrit = critical;

        if (textMesh != null) textMesh.color = textColor;
        if (shadowMesh != null) shadowMesh.color = shadowColor;

        if (isCrit)
        {
            velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(3.6f, 4.4f), 0f);
            gravity = 7.0f;
            maxLifeTime = 0.80f;
            lifeTimer = maxLifeTime;
            targetScale = Vector3.one * 1.10f;
            transform.localScale = Vector3.one * 1.45f;
        }
        else
        {
            velocity = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(2.2f, 3.0f), 0f);
            gravity = 5.5f;
            maxLifeTime = 0.60f;
            lifeTimer = maxLifeTime;
            targetScale = Vector3.one;
            transform.localScale = Vector3.one * 1.30f;
        }
    }

    void Update()
    {
        // 1. Quỹ đạo nảy hình vòng cung
        transform.position += velocity * Time.deltaTime;
        velocity.y -= gravity * Time.deltaTime;
        velocity.x *= Mathf.Clamp01(1f - 1.5f * Time.deltaTime);

        // 2. Co về targetScale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 18f);

        // 3. Đếm thời gian và Fade Out
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= maxLifeTime * 0.45f)
        {
            float fadeProgress = lifeTimer / (maxLifeTime * 0.45f);
            textColor.a = Mathf.Clamp01(fadeProgress);
            shadowColor.a = Mathf.Clamp01(fadeProgress * 0.88f);

            if (textMesh != null) textMesh.color = textColor;
            if (shadowMesh != null) shadowMesh.color = shadowColor;

            if (lifeTimer <= 0f)
            {
                RecycleThis();
            }
        }
    }

    private void RecycleThis()
    {
        _activePopups.Remove(this);
        gameObject.SetActive(false);
        _pool.Enqueue(this);
    }

    void OnDestroy()
    {
        _activePopups.Remove(this);
    }
}
