using UnityEngine;
using UnityEngine.UI;
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
        EnsureRoundedSprites();
    }

    private void EnsureRoundedSprites()
    {
        // 1. Đảm bảo Joystick Background là hình tròn bo viền mượt
        Image bgImg = GetComponent<Image>();
        if (bgImg != null)
        {
            Sprite baseSprite = Resources.Load<Sprite>("UI/joystick_base");
#if UNITY_EDITOR
            if (baseSprite == null) baseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/joystick_base.png");
#endif
            if (baseSprite != null)
            {
                bgImg.sprite = baseSprite;
                bgImg.color = Color.white;
            }
        }

        // 2. Đảm bảo Joystick Handle (núm điều khiển) là hình tròn pha lê 3D thay vì khối vuông trắng
        if (handleRect != null)
        {
            Image handleImg = handleRect.GetComponent<Image>();
            if (handleImg != null)
            {
                Sprite handleSprite = Resources.Load<Sprite>("UI/joystick_handle");
#if UNITY_EDITOR
                if (handleSprite == null) handleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/joystick_handle.png");
#endif
                if (handleSprite != null)
                {
                    handleImg.sprite = handleSprite;
                    handleImg.color = Color.white;
                }
            }
        }
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
