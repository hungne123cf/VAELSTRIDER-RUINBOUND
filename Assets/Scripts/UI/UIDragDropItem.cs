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

    [NonSerialized] public bool hasHandledDrop = false;
    public static bool IsAnyDragging { get; private set; } = false;
    public static UIDragDropItem CurrentDraggingItem { get; private set; } = null;
    private static GameObject dragAvatarObj = null;
    private static RectTransform dragAvatarRt = null;
    private Canvas cachedDragCanvas = null;
    private bool isDragging = false;
    private bool isScrolling = false;
    private bool wasDragged = false;

    void Awake()
    {
        if (parentScrollRect == null)
        {
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }
    }

    private Canvas GetDragCanvas()
    {
        if (cachedDragCanvas != null && cachedDragCanvas.gameObject.activeInHierarchy)
        {
            return cachedDragCanvas;
        }

        Canvas[] parentCanvases = GetComponentsInParent<Canvas>();
        if (parentCanvases != null && parentCanvases.Length > 0)
        {
            Canvas topCanvas = parentCanvases[0];
            foreach (var c in parentCanvases)
            {
                if (c.sortingOrder > topCanvas.sortingOrder)
                {
                    topCanvas = c;
                }
            }
            cachedDragCanvas = topCanvas;
            return cachedDragCanvas;
        }

        cachedDragCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        return cachedDragCanvas;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        wasDragged = false;
        transform.localScale = new Vector3(0.96f, 0.96f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging || isDragging || isScrolling)
        {
            return;
        }

        if (Vector2.Distance(eventData.position, eventData.pressPosition) > 12f)
        {
            return;
        }

        onClick?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        float distFromPress = Vector2.Distance(eventData.position, eventData.pressPosition);
        wasDragged = true;
        hasHandledDrop = false;

        float dx = Mathf.Abs(eventData.delta.x);
        float dy = Mathf.Abs(eventData.delta.y);
        float totalDy = eventData.position.y - eventData.pressPosition.y;
        float totalDx = eventData.position.x - eventData.pressPosition.x;

        // Smart gesture resolution:
        // When inside a Bag scrollrect and dragging upward or leftward towards hero sockets:
        if (parentScrollRect != null && !isSocketedGem)
        {
            bool isDraggingUpward = eventData.delta.y > 1f || totalDy > 3f || (dy > dx * 0.6f && totalDy > 0f);
            bool isDraggingLeftward = eventData.delta.x < -1f || totalDx < -3f;
            bool isDraggingTowardsSockets = isDraggingUpward || isDraggingLeftward;

            if (parentScrollRect.horizontal && !parentScrollRect.vertical)
            {
                // Scroll only if gesture is predominantly horizontal and not dragging towards sockets
                if (!isDraggingTowardsSockets && dx > dy * 1.3f && distFromPress > 10f)
                {
                    isScrolling = true;
                    isDragging = false;
                    parentScrollRect.OnBeginDrag(eventData);
                    return;
                }
            }
            else if (parentScrollRect.vertical && !parentScrollRect.horizontal)
            {
                // In vertical bag, dragging upward (totalDy > 0) is towards hero sockets above bag.
                // Scroll only if gesture is downward (totalDy < 0) and not dragging towards sockets.
                if (!isDraggingTowardsSockets && dy > dx * 1.3f && distFromPress > 10f && totalDy < -5f)
                {
                    isScrolling = true;
                    isDragging = false;
                    parentScrollRect.OnBeginDrag(eventData);
                    return;
                }
            }
        }

        isScrolling = false;
        isDragging = true;
        IsAnyDragging = true;
        CurrentDraggingItem = this;
        hasHandledDrop = false;

        Canvas canvas = GetDragCanvas();
        if (canvas == null) return;

        // Clean up any stale avatar
        if (dragAvatarObj != null) Destroy(dragAvatarObj);

        // Root Avatar Container
        dragAvatarObj = new GameObject("DragAvatar_Item", typeof(RectTransform), typeof(CanvasRenderer));
        dragAvatarObj.layer = 5;
        dragAvatarObj.transform.SetParent(canvas.transform, false);
        dragAvatarObj.transform.SetAsLastSibling();

        // High-order canvas overlay (32700) so avatar is ALWAYS on top of modals (modal is ~28000)
        Canvas avatarCanvas = dragAvatarObj.AddComponent<Canvas>();
        avatarCanvas.overrideSorting = true;
        avatarCanvas.sortingOrder = 32700;

        CanvasGroup cg = dragAvatarObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.alpha = 0.95f;

        dragAvatarRt = dragAvatarObj.GetComponent<RectTransform>();
        dragAvatarRt.sizeDelta = new Vector2(76f, 76f);
        dragAvatarRt.localScale = new Vector3(1.15f, 1.15f, 1f);

        // Background Dark Frame Plate
        GameObject bgCard = new GameObject("BgCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgCard.layer = 5;
        bgCard.transform.SetParent(dragAvatarObj.transform, false);
        RectTransform bgRt = bgCard.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgCard.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.14f, 0.95f);
        bgImg.raycastTarget = false;

        var bgOutline = bgCard.AddComponent<Outline>();
        Color glowCol = (itemColor != Color.white && itemColor.a > 0.1f) ? itemColor : new Color(1f, 0.85f, 0.35f, 1f);
        bgOutline.effectColor = glowCol;
        bgOutline.effectDistance = new Vector2(2f, -2f);

        var bgShadow = bgCard.AddComponent<Shadow>();
        bgShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        bgShadow.effectDistance = new Vector2(4f, -4f);

        // Item Icon Image
        GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icObj.layer = 5;
        icObj.transform.SetParent(dragAvatarObj.transform, false);
        RectTransform icRt = icObj.GetComponent<RectTransform>();
        icRt.anchorMin = Vector2.zero;
        icRt.anchorMax = Vector2.one;
        icRt.sizeDelta = new Vector2(-10f, -10f);
        Image img = icObj.GetComponent<Image>();
        img.sprite = itemIcon;
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = false;

        UpdateAvatarPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isScrolling)
        {
            if (parentScrollRect != null) parentScrollRect.OnDrag(eventData);
            return;
        }

        if (isDragging)
        {
            UpdateAvatarPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isScrolling)
        {
            if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
            isScrolling = false;
            transform.localScale = Vector3.one;
            return;
        }

        if (!isDragging)
        {
            if (Vector2.Distance(eventData.position, eventData.pressPosition) < 12f)
            {
                onClick?.Invoke();
            }
            return;
        }

        isDragging = false;
        IsAnyDragging = false;
        CurrentDraggingItem = null;
        transform.localScale = Vector3.one;

        if (dragAvatarObj != null)
        {
            Destroy(dragAvatarObj);
            dragAvatarObj = null;
            dragAvatarRt = null;
        }

        // KÉO RA ĐỂ THÁO NGỌC (DRAG-OUT TO UNSOCKET) HOẶC ĐỔI CHỖ LỖ KHẢM (MOVE/SWAP)
        if (isSocketedGem)
        {
            UIDropSocketTarget targetSlot = null;
            if (eventData.pointerEnter != null)
            {
                targetSlot = eventData.pointerEnter.GetComponent<UIDropSocketTarget>() ?? eventData.pointerEnter.GetComponentInParent<UIDropSocketTarget>();
            }

            if (targetSlot == null && eventData.pointerCurrentRaycast.gameObject != null)
            {
                targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<UIDropSocketTarget>() ?? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<UIDropSocketTarget>();
            }

            if (targetSlot == null && EventSystem.current != null)
            {
                var raycastResults = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, raycastResults);
                foreach (var r in raycastResults)
                {
                    targetSlot = r.gameObject.GetComponent<UIDropSocketTarget>() ?? r.gameObject.GetComponentInParent<UIDropSocketTarget>();
                    if (targetSlot != null) break;
                }
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
        else
        {
            // Kéo từ túi đồ thả vào lỗ khảm (Fallback nếu OnDrop chưa bắt được qua raycast bubbling)
            if (!hasHandledDrop)
            {
                UIDropSocketTarget targetSlot = null;
                if (eventData.pointerEnter != null)
                {
                    targetSlot = eventData.pointerEnter.GetComponent<UIDropSocketTarget>() ?? eventData.pointerEnter.GetComponentInParent<UIDropSocketTarget>();
                }
                if (targetSlot == null && eventData.pointerCurrentRaycast.gameObject != null)
                {
                    targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<UIDropSocketTarget>() ?? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<UIDropSocketTarget>();
                }
                if (targetSlot == null && EventSystem.current != null)
                {
                    var raycastResults = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, raycastResults);
                    foreach (var r in raycastResults)
                    {
                        targetSlot = r.gameObject.GetComponent<UIDropSocketTarget>() ?? r.gameObject.GetComponentInParent<UIDropSocketTarget>();
                        if (targetSlot != null) break;
                    }
                }

                // Distance fallback: Thả gần bất kỳ ô nào trong 6 ô quanh anh hùng (bán kính 85px)
                if (targetSlot == null && InGameInventoryUI.Instance != null)
                {
                    targetSlot = InGameInventoryUI.Instance.FindClosestSocketTarget(eventData.position, 85f);
                }

                if (targetSlot != null)
                {
                    targetSlot.ReceiveDrop(this);
                }
            }
        }

        // Đảm bảo refresh lại modal sau khi hoàn tất thao tác kéo thả
        InGameInventoryUI.Instance?.RefreshModal(true);
    }

    private void UpdateAvatarPosition(PointerEventData eventData)
    {
        if (dragAvatarRt == null) return;
        Canvas canvas = GetDragCanvas();
        if (canvas == null) return;

        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;
        if (cam == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, eventData.position, cam, out Vector2 localPoint))
        {
            dragAvatarRt.localPosition = localPoint + new Vector2(0f, 28f);
        }
    }

    void OnDestroy()
    {
        CurrentDraggingItem = null;
        isDragging = false;
        IsAnyDragging = false;
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
        var dragItem = (eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<UIDragDropItem>() : null) ?? UIDragDropItem.CurrentDraggingItem;
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

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetVisual();
    }

    public void ReceiveDrop(UIDragDropItem dragItem)
    {
        ResetVisual();
        if (dragItem == null || string.IsNullOrEmpty(dragItem.itemId)) return;
        if (dragItem.hasHandledDrop) return;
        dragItem.hasHandledDrop = true;

        if (dragItem.isSocketedGem)
        {
            // Được xử lý tự động trong UIDragDropItem.OnEndDrag
            return;
        }

        if (onGemDroppedDetailed != null)
        {
            onGemDroppedDetailed.Invoke(dragItem.itemId, socketSlotIndex, subSlotIndex);
        }
        else if (onGemDropped != null)
        {
            onGemDropped.Invoke(dragItem.itemId, socketSlotIndex);
        }
        else
        {
            string curHero = EquipmentSocketManager.GetActiveHeroId();
            if (EquipmentSocketManager.SocketGemToHeroSlot(socketSlotIndex, dragItem.itemId, out _, out _, curHero))
            {
                SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                InGameInventoryUI.Instance?.NotifyPlayerCombatStatsChanged();
                InGameInventoryUI.Instance?.RefreshModal();
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragItem = (eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<UIDragDropItem>() : null) ?? UIDragDropItem.CurrentDraggingItem;
        ReceiveDrop(dragItem);
    }

    private void ResetVisual()
    {
        if (outline != null) outline.effectColor = originalOutlineColor;
        transform.localScale = originalScale;
    }
}
