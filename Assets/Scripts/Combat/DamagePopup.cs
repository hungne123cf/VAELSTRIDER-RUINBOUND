using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private TextMesh textMesh;
    private float moveYSpeed = 2.2f;
    private float disappearTimer = 0.65f;
    private Color textColor;
    private Vector3 moveVector;

    public static DamagePopup Create(Vector3 position, int damageAmount, bool isCritical = false, bool isPlayer = false)
    {
        // Tao GameObject tu dong voi TextMesh de khong can keo tha Prefab
        GameObject popupObj = new GameObject("DamagePopup");
        popupObj.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.7f, 0);

        TextMesh tm = popupObj.AddComponent<TextMesh>();
        tm.text = damageAmount.ToString();
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;

        // Font size & Mau sac
        if (isPlayer)
        {
            tm.color = new Color(1f, 0.2f, 0.2f); // Mau do tuoi khi Player bi danh
            tm.fontSize = 32;
            tm.characterSize = 0.07f;
        }
        else if (isCritical)
        {
            tm.color = new Color(1f, 0.85f, 0.1f); // Mau vang cam chi mang
            tm.fontSize = 38;
            tm.characterSize = 0.085f;
            tm.text = damageAmount + "!";
        }
        else
        {
            tm.color = Color.white; // Mau trang sat thuong thuong
            tm.fontSize = 28;
            tm.characterSize = 0.065f;
        }

        // Sap xep lop hien thi de chu khong bi che boi Map
        MeshRenderer mr = popupObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Entities";
            mr.sortingOrder = 999;
        }

        DamagePopup popup = popupObj.AddComponent<DamagePopup>();
        popup.Setup(tm.color);

        return popup;
    }

    public void Setup(Color color)
    {
        textMesh = GetComponent<TextMesh>();
        textColor = color;
        moveVector = new Vector3(Random.Range(-0.6f, 0.6f), 1f, 0) * moveYSpeed;
    }

    void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 4f * Time.deltaTime;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // Mo dan chu roi bien mat
            float fadeSpeed = 3f;
            textColor.a -= fadeSpeed * Time.deltaTime;
            if (textMesh != null) textMesh.color = textColor;

            if (textColor.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
