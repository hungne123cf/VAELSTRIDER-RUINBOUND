using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class RoomBarrier : MonoBehaviour
{
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool isVertical = true;
    [SerializeField] private float size = 3f;

    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Light2D barrierLight;

    void Awake()
    {
        EnsureComponents();
        UpdateColliderSize();
        ApplyStateInstant(isLocked);
    }

    private void EnsureComponents()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (barrierLight == null) barrierLight = GetComponent<Light2D>();
    }

    private void UpdateColliderSize()
    {
        EnsureComponents();
        if (boxCollider != null)
        {
            if (isVertical)
            {
                boxCollider.size = new Vector2(0.8f, size);
                boxCollider.offset = Vector2.zero;
            }
            else
            {
                boxCollider.size = new Vector2(size, 0.8f);
                boxCollider.offset = Vector2.zero;
            }
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = isVertical ? new Vector2(0.7f, size) : new Vector2(size, 0.7f);
        }
    }

    public void Setup(bool vertical, float length)
    {
        EnsureComponents();
        isVertical = vertical;
        size = length;
        UpdateColliderSize();
    }

    public void Lock()
    {
        if (isLocked) return;
        isLocked = true;
        gameObject.SetActive(true);
        boxCollider.enabled = true;

        SoundManager.EnsureInstance()?.PlayHitImpact();
        StartCoroutine(FadeBarrier(0f, 0.85f, 0.3f));
    }

    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;
        boxCollider.enabled = false;

        SoundManager.EnsureInstance()?.PlayShardPickup();
        StartCoroutine(FadeBarrier(0.85f, 0f, 0.5f));
    }

    private void ApplyStateInstant(bool locked)
    {
        EnsureComponents();
        isLocked = locked;
        if (boxCollider != null) boxCollider.enabled = locked;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = locked ? 0.85f : 0f;
            spriteRenderer.color = c;
        }
        if (barrierLight != null)
        {
            barrierLight.enabled = locked;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckAndPassPlayerThrough(collision.collider);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        CheckAndPassPlayerThrough(collision.collider);
    }

    private void CheckAndPassPlayerThrough(Collider2D otherCol)
    {
        if (!isLocked || otherCol == null) return;

        var player = otherCol.GetComponent<PlayerController>() ?? otherCol.GetComponentInParent<PlayerController>();
        if (player == null) return;

        var roomMgr = DungeonRoomManager.Instance;
        if (roomMgr != null && roomMgr.currentRoom != null)
        {
            var curRoom = roomMgr.currentRoom;
            Vector3 pPos = player.transform.position;
            bool isInsideRoom = curRoom.roomBounds.Contains(new Vector2(pPos.x, pPos.y));

            // Nếu người chơi bị kẹt bên ngoài phòng khi cửa đã đóng
            if (!isInsideRoom)
            {
                // Cho phép người chơi bước qua cửa vào sâu trong phòng
                Vector3 toCenter = (curRoom.roomCenter - transform.position).normalized;
                Vector3 insidePos = transform.position + toCenter * 1.5f;
                player.transform.position = new Vector3(insidePos.x, insidePos.y, player.transform.position.z);
                SoundManager.EnsureInstance()?.PlayPortalSpawn();
                DamagePopup.CreateText(player.transform.position + Vector3.up * 1.2f, LocalizationManager.T("BƯỚC VÀO PHÒNG ĐẤU!", "ENTER COMBAT ROOM!"), new Color(0.2f, 0.85f, 1f));
            }
        }
    }

    private IEnumerator FadeBarrier(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = a;
                spriteRenderer.color = c;
            }
            if (barrierLight != null)
            {
                barrierLight.intensity = Mathf.Lerp(0f, 1.6f, toAlpha > 0f ? a : (1f - a));
                barrierLight.enabled = barrierLight.intensity > 0.05f;
            }
            yield return null;
        }

        if (toAlpha <= 0f)
        {
            if (barrierLight != null) barrierLight.enabled = false;
        }
    }

    public static RoomBarrier Create(Vector3 position, bool vertical, float length, string name = "RoomBarrier")
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        // Simple procedural colored texture for the glowing barrier
        Texture2D tex = new Texture2D(32, 32);
        Color runeColor = new Color(0.2f, 0.85f, 1f, 0.9f);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool isBorder = (x == 0 || x == 31 || y == 0 || y == 31);
                tex.SetPixel(x, y, isBorder ? Color.white : runeColor);
            }
        }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        sr.sortingLayerName = "Entities";
        sr.sortingOrder = 4500;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = vertical ? new Vector2(0.7f, length) : new Vector2(length, 0.7f);

        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(0.2f, 0.8f, 1f, 1f);
        light.intensity = 1.6f;
        light.pointLightInnerRadius = 0.5f;
        light.pointLightOuterRadius = length * 1.2f;

        var barrier = go.AddComponent<RoomBarrier>();
        barrier.Setup(vertical, length);
        barrier.ApplyStateInstant(false); // start unlocked

        return barrier;
    }
}
