using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles swipe / drag gestures on hero selection carousel (touch and mouse drag).
/// </summary>
public class HeroCarouselSwipeHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Action onSwipeLeft;   // Swipe left -> Next hero
    public Action onSwipeRight;  // Swipe right -> Previous hero
    
    private Vector2 startPos;
    private bool isDragging;
    private const float SWIPE_THRESHOLD = 35f;

    public void OnPointerDown(PointerEventData eventData)
    {
        startPos = eventData.position;
        isDragging = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = eventData.position;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Keep active for EventSystem tracking
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EvaluateSwipe(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EvaluateSwipe(eventData.position);
    }

    private void EvaluateSwipe(Vector2 endPos)
    {
        if (!isDragging) return;
        isDragging = false;

        float deltaX = endPos.x - startPos.x;
        if (Mathf.Abs(deltaX) >= SWIPE_THRESHOLD)
        {
            if (deltaX < 0)
            {
                onSwipeLeft?.Invoke();
            }
            else
            {
                onSwipeRight?.Invoke();
            }
        }
    }
}
