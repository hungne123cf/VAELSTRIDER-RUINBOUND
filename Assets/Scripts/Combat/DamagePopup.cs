using UnityEngine;

public enum DamageType
{
    Physical = 0,   // Sát thương Vật Lý (Đỏ Cam)
    Magic = 1,      // Sát thương Phép (Tím Tinh Tú)
    TrueDamage = 2  // Sát thương Chuẩn (Trắng Sáng Bạc Tinh Khiết)
}

public class DamagePopup : MonoBehaviour
{
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

    public static DamagePopup Create(Vector3 position, int damageAmount, bool isCritical = false, bool isPlayer = false)
    {
        return Create(position, damageAmount, DamageType.Physical, isCritical, isPlayer);
    }

    public static DamagePopup Create(Vector3 position, int damageAmount, DamageType damageType, bool isCritical = false, bool isPlayer = false)
    {
        GameObject popupObj = new GameObject("DamagePopup");

        // Tản ngẫu nhiên nhẹ quanh tâm để nhiều đòn đánh không xuất hiện đè khít lên cùng 1 tọa độ
        Vector3 spawnOffset = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.6f, 0.85f), 0f);
        popupObj.transform.position = position + spawnOffset;

        TextMesh tm = popupObj.AddComponent<TextMesh>();
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;

        Color mainColor;
        string displayText;

        if (isPlayer)
        {
            // Người chơi bị nhận sát thương: Số đỏ tươi
            displayText = damageAmount.ToString();
            mainColor = new Color(1f, 0.18f, 0.18f);
            tm.fontSize = isCritical ? 38 : 30;
            tm.characterSize = isCritical ? 0.088f : 0.075f;
            tm.fontStyle = FontStyle.Bold;
        }
        else
        {
            // Sát thương gây ra lên Quái / Mục tiêu:
            // Sát thương thường CHỈ HIỆN SỐ (không đính kèm chữ ATK/AP/TD)
            // Sát thương chí mạng hiển thị "CRIT! {số}" với font to và màu rực sáng
            if (isCritical)
            {
                displayText = $"CRIT {damageAmount}";
                tm.fontSize = 34;
                tm.characterSize = 0.080f;
                tm.fontStyle = FontStyle.Bold;

                switch (damageType)
                {
                    case DamageType.TrueDamage:
                        // Chí mạng sát thương chuẩn: Trắng Tinh Điện Quang
                        mainColor = new Color(1f, 1f, 1f);
                        break;
                    case DamageType.Magic:
                        // Chí mạng sát thương phép: Tím Neon siêu sáng
                        mainColor = new Color(0.92f, 0.45f, 1f);
                        break;
                    case DamageType.Physical:
                    default:
                        // Chí mạng sát thương vật lý: Vàng Kim Rực Lửa (Chuẩn Crit Pixel Game)
                        mainColor = new Color(1f, 0.85f, 0.12f);
                        break;
                }
            }
            else
            {
                // Đòn đánh thường: CHỈ HIỂN THỊ CON SỐ THUẦN TÚY (Số nhảy sạch sẽ)
                displayText = damageAmount.ToString();
                tm.fontSize = 30;
                tm.characterSize = 0.075f;
                tm.fontStyle = FontStyle.Bold;

                switch (damageType)
                {
                    case DamageType.TrueDamage:
                        // Sát thương chuẩn: Trắng Bạc
                        mainColor = new Color(0.92f, 0.95f, 1f);
                        break;
                    case DamageType.Magic:
                        // Sát thương phép: Tím AP
                        mainColor = new Color(0.76f, 0.35f, 1f);
                        break;
                    case DamageType.Physical:
                    default:
                        // Sát thương vật lý: Đỏ Cam ATK
                        mainColor = new Color(1f, 0.25f, 0.22f);
                        break;
                }
            }
        }

        tm.text = displayText;
        tm.color = mainColor;

        MeshRenderer mr = popupObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Entities";
            mr.sortingOrder = 999;
        }

        // Tạo bóng đen pixel (Pixel Drop Shadow) phía sau chữ để nổi bật trên mọi nền gạch/đất
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(popupObj.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.04f, -0.04f, 0.01f);

        TextMesh shadowTm = shadowObj.AddComponent<TextMesh>();
        shadowTm.text = displayText;
        shadowTm.alignment = tm.alignment;
        shadowTm.anchor = tm.anchor;
        shadowTm.fontSize = tm.fontSize;
        shadowTm.characterSize = tm.characterSize;
        shadowTm.fontStyle = tm.fontStyle;
        Color sCol = new Color(0f, 0f, 0f, 0.88f);
        shadowTm.color = sCol;

        MeshRenderer smr = shadowObj.GetComponent<MeshRenderer>();
        if (smr != null)
        {
            smr.sortingLayerName = "Entities";
            smr.sortingOrder = 998;
        }

        DamagePopup popup = popupObj.AddComponent<DamagePopup>();
        popup.Setup(tm, shadowTm, mainColor, sCol, isCritical);

        return popup;
    }

    public static DamagePopup CreateText(Vector3 position, string text, Color color)
    {
        GameObject popupObj = new GameObject("TextPopup");
        popupObj.transform.position = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.7f, 0f);

        TextMesh tm = popupObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;
        tm.fontSize = 32;
        tm.characterSize = 0.075f;
        tm.fontStyle = FontStyle.Bold;

        MeshRenderer mr = popupObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Entities";
            mr.sortingOrder = 999;
        }

        // Shadow cho text thông báo
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(popupObj.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.04f, -0.04f, 0.01f);

        TextMesh shadowTm = shadowObj.AddComponent<TextMesh>();
        shadowTm.text = text;
        shadowTm.alignment = tm.alignment;
        shadowTm.anchor = tm.anchor;
        shadowTm.fontSize = tm.fontSize;
        shadowTm.characterSize = tm.characterSize;
        shadowTm.fontStyle = tm.fontStyle;
        Color sCol = new Color(0f, 0f, 0f, 0.88f);
        shadowTm.color = sCol;

        MeshRenderer smr = shadowObj.GetComponent<MeshRenderer>();
        if (smr != null)
        {
            smr.sortingLayerName = "Entities";
            smr.sortingOrder = 998;
        }

        DamagePopup popup = popupObj.AddComponent<DamagePopup>();
        popup.Setup(tm, shadowTm, color, sCol, false);
        return popup;
    }

    public void Setup(TextMesh tm, TextMesh stm, Color color, Color sColor, bool critical)
    {
        textMesh = tm;
        shadowMesh = stm;
        textColor = color;
        shadowColor = sColor;
        isCrit = critical;

        if (isCrit)
        {
            // Chí mạng: Nảy cao bổng lên, bung mạnh sang hai bên
            velocity = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(3.8f, 4.6f), 0f);
            gravity = 7.0f;
            maxLifeTime = 0.85f;
            lifeTimer = maxLifeTime;
            targetScale = Vector3.one * 1.12f;
            transform.localScale = Vector3.one * 1.50f; // Pop nảy vừa vặn, không quá to
        }
        else
        {
            // Thường: Văng nhẹ hình vòng cung tản ra, tránh đè lên nhau
            velocity = new Vector3(Random.Range(-1.1f, 1.1f), Random.Range(2.4f, 3.2f), 0f);
            gravity = 5.5f;
            maxLifeTime = 0.65f;
            lifeTimer = maxLifeTime;
            targetScale = Vector3.one;
            transform.localScale = Vector3.one * 1.35f; // Nảy nhẹ khi xuất hiện
        }
    }

    void Update()
    {
        // 1. Quỹ đạo nảy hình vòng cung (Arc trajectory chống đè số)
        transform.position += velocity * Time.deltaTime;
        velocity.y -= gravity * Time.deltaTime;
        velocity.x *= Mathf.Clamp01(1f - 1.5f * Time.deltaTime); // Ma sát ngang nhẹ

        // 2. Hiệu ứng Scale Punchy Pop (co về targetScale mượt mà)
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
                Destroy(gameObject);
            }
        }
    }
}
