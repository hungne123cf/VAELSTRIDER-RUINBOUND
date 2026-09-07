using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform containerRect;
    [SerializeField] private RectTransform handleRect;
    [SerializeField] private float handleRange = 80f;

    [Header("Player Target")]
    [SerializeField] private PlayerController playerController;

    private Vector2 inputVector = Vector2.zero;

    void Awake()
    {
        if (containerRect == null) containerRect = GetComponent<RectTransform>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            containerRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out position
        );

        position = Vector2.ClampMagnitude(position, handleRange);
        if (handleRect != null)
        {
            handleRect.anchoredPosition = position;
        }

        inputVector = position / handleRange;
        if (playerController != null)
        {
            playerController.SetMobileInput(inputVector);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handleRect != null)
        {
            handleRect.anchoredPosition = Vector2.zero;
        }

        if (playerController != null)
        {
            playerController.SetMobileInput(Vector2.zero);
        }
    }
}
