using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomWaypointUI : MonoBehaviour
{
    private static RoomWaypointUI instance;

    [Header("UI Elements")]
    private RectTransform arrowRect;
    private TextMeshProUGUI distanceText;
    private Image arrowImage;

    private Transform playerTransform;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        if (DungeonRoomManager.Instance == null) return;

        // Khi danh Boss, an hoan toan Waypoint de khong che khuat thanh mau Boss va khong gian chien dau
        if (BossHealthBarUI.Instance != null && BossHealthBarUI.Instance.IsVisible)
        {
            SetVisible(false);
            return;
        }

        Vector3 target = DungeonRoomManager.Instance.GetNextObjectiveTarget();
        if (target == Vector3.zero)
        {
            SetVisible(false);
            return;
        }

        float distance = Vector2.Distance(playerTransform.position, target);

        // If very close or inside the room, hide compass
        if (distance < 5.5f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        // Calculate direction on screen
        Vector2 dir = ((Vector2)target - (Vector2)playerTransform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (arrowRect != null)
        {
            arrowRect.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        if (distanceText != null)
        {
            distanceText.text = "";
            distanceText.enabled = false;
        }
    }

    private void FindPlayer()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    private void SetVisible(bool visible)
    {
        if (arrowImage != null) arrowImage.enabled = false;
        if (distanceText != null)
        {
            distanceText.text = "";
            distanceText.enabled = false;
        }
    }

    public static RoomWaypointUI EnsureUIExists()
    {
        if (instance != null) return instance;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("RoomWaypointHUD");
        if (existing != null)
        {
            instance = existing.GetComponent<RoomWaypointUI>();
            if (instance != null) return instance;
        }

        GameObject hudObj = new GameObject("RoomWaypointHUD");
        hudObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = hudObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -90);
        rt.sizeDelta = new Vector2(120, 50);

        instance = hudObj.AddComponent<RoomWaypointUI>();

        // Arrow Container
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(hudObj.transform, false);
        instance.arrowRect = arrowObj.AddComponent<RectTransform>();
        instance.arrowRect.sizeDelta = new Vector2(28, 28);
        instance.arrowRect.anchoredPosition = new Vector2(-25, 0);

        instance.arrowImage = arrowObj.AddComponent<Image>();
        instance.arrowImage.color = new Color(0.3f, 0.95f, 1f, 0.9f);
        instance.arrowImage.raycastTarget = false;
        instance.arrowImage.enabled = false;

        // Distance text (Đã bỏ hiển thị theo yêu cầu)
        GameObject distObj = new GameObject("DistText");
        distObj.transform.SetParent(hudObj.transform, false);
        RectTransform dtRt = distObj.AddComponent<RectTransform>();
        dtRt.sizeDelta = new Vector2(70, 30);
        dtRt.anchoredPosition = new Vector2(20, 0);

        instance.distanceText = distObj.AddComponent<TextMeshProUGUI>();
        instance.distanceText.fontSize = 15;
        instance.distanceText.fontStyle = FontStyles.Bold;
        instance.distanceText.color = Color.white;
        instance.distanceText.alignment = TextAlignmentOptions.Left;
        instance.distanceText.raycastTarget = false;
        instance.distanceText.text = "";
        instance.distanceText.enabled = false;
        distObj.SetActive(false);

        return instance;
    }
}
