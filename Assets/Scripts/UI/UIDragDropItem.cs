// ========================================================
// VAELSTRIDER: RUINBOUND - Mobile Drag & Drop System for Items & Gems
// Enables dragging mineral ores/gems directly into or out of socket slots
// ========================================================
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIDragDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public string itemId = "";
    public Sprite itemIcon = null;
    public Color itemColor = Color.white;
    public Action onClick;
    public ScrollRect parentScrollRect;

    // Support for Drag-To-Unsocket (Kéo ra để tháo ngọc) & Swap Sockets
    public bool isSocketedGem = false;
    public int sourceSocketIndex = -1;
    public int sourceSubSocketIndex = -1;
    public Action<int> onDragOutOfSocket;
    public Action<int, int> onMoveSocket;
    public Action<int, int, int, int> onMoveSubSocket;

    private static GameObject dragAvatarObj = null;
    private static RectTransform dragAvatarRt = null;
    private Canvas rootCanvas = null;
    private bool isDragging = false;

    void Awake()
    {
        if (parentScrollRect == null)
        {
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }
    }

    private Canvas GetRootCanvas()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                rootCanvas = rootCanvas.rootCanvas;
            }
        }
        return rootCanvas;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Visual press effect on mobile
        transform.localScale = new Vector3(0.96f, 0.96f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging)
        {
            onClick?.Invoke();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Neu vuot doc manh trong scroll view thi uu tien scroll
        float dx = Mathf.Abs(eventData.delta.x);
        float dy = Mathf.Abs(eventData.delta.y);
        if (parentScrollRect != null && dy > dx * 1.5f)
        {
            parentScrollRect.OnBeginDrag(eventData);
            return;
        }

        isDragging = true;
        Canvas canvas = GetRootCanvas();
        if (canvas == null) return;

        // Tao Ghost Icon bay theo ngon tay tren Root Canvas
        if (dragAvatarObj != null) Destroy(dragAvatarObj);

        dragAvatarObj = new GameObject("DragAvatar_Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        dragAvatarObj.layer = 5;
        dragAvatarObj.transform.SetParent(canvas.transform, false);
        dragAvatarObj.transform.SetAsLastSibling();

        dragAvatarRt = dragAvatarObj.GetComponent<RectTransform>();
        dragAvatarRt.sizeDelta = new Vector2(70f, 70f);

        Image img = dragAvatarObj.GetComponent<Image>();
        img.sprite = itemIcon;
        img.color = itemColor;
        img.preserveAspect = true;

        var outline = dragAvatarObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Khong chan raycast de drop target ben duoi nhan duoc OnDrop
        CanvasGroup cg = dragAvatarObj.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.88f;

        UpdateAvatarPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            if (parentScrollRect != null) parentScrollRect.OnDrag(eventData);
            return;
        }

        UpdateAvatarPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
            return;
        }

        isDragging = false;
        transform.localScale = Vector3.one;

        // KÉO RA ĐỂ THÁO NGỌC (DRAG-OUT TO UNSOCKET) HOẶC ĐỔI CHỖ LỖ KHẢM (MOVE/SWAP)
        if (isSocketedGem)
        {
            UIDropSocketTarget targetSlot = null;
            if (eventData.pointerEnter != null)
            {
                targetSlot = eventData.pointerEnter.GetComponent<UIDropSocketTarget>() ?? eventData.pointerEnter.GetComponentInParent<UIDropSocketTarget>();
            }

            if (targetSlot == null)
            {
                // Kéo ra ngoài lỗ khảm -> Tháo ngọc hoàn trả về túi đồ!
                onDragOutOfSocket?.Invoke(sourceSocketIndex);
            }
            else
            {
                if (onMoveSubSocket != null && sourceSubSocketIndex >= 0)
                {
                    int toSub = (targetSlot.subSlotIndex >= 0) ? targetSlot.subSlotIndex : 0;
                    if (targetSlot.socketSlotIndex != sourceSocketIndex || toSub != sourceSubSocketIndex)
                    {
                        onMoveSubSocket.Invoke(sourceSocketIndex, sourceSubSocketIndex, targetSlot.socketSlotIndex, toSub);
                    }
                }
                else if (targetSlot.socketSlotIndex != sourceSocketIndex)
                {
                    onMoveSocket?.Invoke(sourceSocketIndex, targetSlot.socketSlotIndex);
                }
            }
        }

        if (dragAvatarObj != null)
        {
            Destroy(dragAvatarObj);
            dragAvatarObj = null;
            dragAvatarRt = null;
        }
    }

    private void UpdateAvatarPosition(PointerEventData eventData)
    {
        if (dragAvatarRt == null) return;
        Canvas canvas = GetRootCanvas();
        if (canvas == null) return;

        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            dragAvatarRt.localPosition = localPoint + new Vector2(0f, 25f); // Dich nhe len tren de ngon tay khong che khuat icon
        }
    }

    void OnDestroy()
    {
        if (dragAvatarObj != null)
        {
            Destroy(dragAvatarObj);
            dragAvatarObj = null;
        }
    }
}

public class UIDropSocketTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int socketSlotIndex = 0;
    public int subSlotIndex = -1;
    public Action<string, int, int> onGemDroppedDetailed;
    public Action<string, int> onGemDropped;

    private Outline outline;
    private Color originalOutlineColor;
    private Vector3 originalScale = Vector3.one;

    void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null) originalOutlineColor = outline.effectColor;
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            var dragItem = eventData.pointerDrag.GetComponent<UIDragDropItem>();
            if (dragItem != null && !string.IsNullOrEmpty(dragItem.itemId) && EquipmentSocketManager.IsGem(dragItem.itemId))
            {
                if (dragItem.isSocketedGem && dragItem.sourceSocketIndex == socketSlotIndex && (subSlotIndex == -1 || dragItem.sourceSubSocketIndex == subSlotIndex))
                {
                    return; // Đang kéo chính nó trên ô của nó
                }

                // Highlight slot with glowing cyan border and scale pop
                if (outline != null) outline.effectColor = new Color(0f, 1f, 0.85f, 1f);
                transform.localScale = originalScale * 1.08f;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetVisual();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ResetVisual();
        if (eventData.pointerDrag != null)
        {
            var dragItem = eventData.pointerDrag.GetComponent<UIDragDropItem>();
            if (dragItem != null && !string.IsNullOrEmpty(dragItem.itemId))
            {
                if (dragItem.isSocketedGem)
                {
                    // Được xử lý tự động trong UIDragDropItem.OnEndDrag
                    return;
                }

                if (onGemDroppedDetailed != null)
                {
                    onGemDroppedDetailed.Invoke(dragItem.itemId, socketSlotIndex, subSlotIndex);
                }
                else
                {
                    onGemDropped?.Invoke(dragItem.itemId, socketSlotIndex);
                }
            }
        }
    }

    private void ResetVisual()
    {
        if (outline != null) outline.effectColor = originalOutlineColor;
        transform.localScale = originalScale;
    }
}
