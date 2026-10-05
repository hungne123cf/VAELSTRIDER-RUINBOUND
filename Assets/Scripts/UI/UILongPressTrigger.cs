// ========================================================
// VAELSTRIDER: RUINBOUND - Mobile Long-Press Trigger for Item Salvage
// Holding an item for 2.0s reveals the Salvage [X] badge on mobile
// ========================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UILongPressTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler
{
    public float holdDuration = 2.0f;
    public string targetItemId = "";
    public Action<string> onLongPressSuccess;

    private Coroutine holdCoroutine;
    private bool isHolding = false;
    private bool longPressTriggered = false;
    private GameObject activeXBadge = null;

    public void OnPointerDown(PointerEventData eventData)
    {
        longPressTriggered = false;
        isHolding = true;
        holdCoroutine = StartCoroutine(HoldRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CancelHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelHold();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CancelHold();
    }

    private void CancelHold()
    {
        isHolding = false;
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private IEnumerator HoldRoutine()
    {
        float timer = 0f;
        while (timer < holdDuration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (isHolding)
        {
            longPressTriggered = true;
            onLongPressSuccess?.Invoke(targetItemId);
            ShowXBadge();
        }
    }

    public void ShowXBadge()
    {
        if (activeXBadge != null)
        {
            activeXBadge.SetActive(true);
            return;
        }

        var def = InventoryItemDatabase.GetItem(targetItemId);
        if (def == null) return;

        // Calculate salvage shard reward
        int shardReward = (def.category == InventoryItemDatabase.ItemCategory.Equipment) 
            ? InventoryUI.GetDismantleValue(def.rarity) 
            : Mathf.Max(10, def.sellPrice);

        // Create Badge on Top-Right of this Item Slot
        activeXBadge = new GameObject("SalvageXBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        activeXBadge.layer = 5;
        activeXBadge.transform.SetParent(transform, false);

        RectTransform xRt = activeXBadge.GetComponent<RectTransform>();
        xRt.anchorMin = new Vector2(1f, 1f);
        xRt.anchorMax = new Vector2(1f, 1f);
        xRt.pivot = new Vector2(1f, 1f);
        xRt.anchoredPosition = new Vector2(6f, 6f);
        xRt.sizeDelta = new Vector2(36f, 36f);

        Image xImg = activeXBadge.GetComponent<Image>();
        xImg.color = new Color(0.85f, 0.15f, 0.22f, 1f);

        var outline = activeXBadge.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.4f, 1f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button xBtn = activeXBadge.GetComponent<Button>();
        xBtn.targetGraphic = xImg;
        xBtn.onClick.AddListener(() =>
        {
            ExecuteSalvage(def, shardReward);
        });

        // Text 'X' with Shard count
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(activeXBadge.transform, false);
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        string rewardStr = InventoryUI.FormatCompactNumber(shardReward);
        tmp.text = $"<size=15><b>X</b></size>\n<size=8>+{rewardStr}</size>";
        tmp.fontSize = 11;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 7f;
        tmp.fontSizeMax = 11f;
        tmp.lineSpacing = -20f;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
    }

    private void ExecuteSalvage(InventoryItemDatabase.ItemDefinition def, int shardReward)
    {
        if (AccountDatabase.RemoveItemFromActiveUser(def.id, 1))
        {
            AccountDatabase.AddAethelShards(shardReward);

            if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
            if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

            Debug.Log($"<color=green>[SALVAGE 2s]</color> Đã rã vật phẩm {def.nameVi}, nhận được +{shardReward} Linh Hồn!");

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.PlaySoulShardFlyEffect(transform.position, shardReward);
                InventoryUI.Instance.RefreshGrid();
            }
        }
    }
}
