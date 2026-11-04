using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

