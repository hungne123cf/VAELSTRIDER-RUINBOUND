using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class GameSetupTools
{
    [MenuItem("Vaelstrider/Spawn Treasure Chest in Room")]
    [MenuItem("GameObject/Vaelstrider/Treasure Chest", false, 10)]
    public static void SpawnTreasureChest()
    {
        GameObject chest = new GameObject("TreasureChest");

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            chest.transform.position = player.transform.position + new Vector3(2f, 1f, 0);
        }
        else
        {
            chest.transform.position = Vector3.zero;
        }

        var sr = chest.AddComponent<SpriteRenderer>();
        var closedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_closed.png");
        sr.sprite = closedSprite;
        sr.sortingOrder = 5000;

        var col = chest.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.7f);
        col.offset = new Vector2(0f, 0.35f);
        col.isTrigger = true;

        var chestComp = chest.AddComponent<TreasureChest>();

        Selection.activeGameObject = chest;
        Undo.RegisterCreatedObjectUndo(chest, "Spawn Treasure Chest");

        Debug.Log("<color=green>[VAELSTRIDER]</color> Đã tạo Rương Kho Báu tại vị trí: " + chest.transform.position);
    }

    [MenuItem("Vaelstrider/Setup Potion Quick Slot on UI")]
    [MenuItem("GameObject/Vaelstrider/Potion Quick Slot UI", false, 11)]
    public static void SetupPotionQuickSlot()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas trong Scene!");
            return;
        }

        // Kiem tra xem da co Btn_Potion chua
        Transform existing = canvas.transform.Find("Btn_Potion");
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("<color=yellow>[VAELSTRIDER]</color> Nút Btn_Potion đã có sẵn trên Canvas!");
            return;
        }

        // 1. Tao Root Button GameObject
        GameObject btnObj = new GameObject("Btn_Potion");
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0); // Bottom-Right
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-220, 360); // Phia tren nut Attack
        rect.sizeDelta = new Vector2(75, 75);

        // Background tron / bo goc toi mau
        Image bgImage = btnObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.18f, 0.88f);

        Button button = btnObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;

        // 2. Tao Child Icon (Binh mau)
        GameObject iconObj = new GameObject("PotionIcon");
        iconObj.transform.SetParent(btnObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 3);
        iconRect.sizeDelta = new Vector2(50, 50);

        Image iconImage = iconObj.AddComponent<Image>();
        Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
        if (potionSprite != null) iconImage.sprite = potionSprite;
        iconImage.raycastTarget = false;

        // 3. Tao Child Badge Text (So luong binh)
        GameObject badgeObj = new GameObject("AmountText");
        badgeObj.transform.SetParent(btnObj.transform, false);

        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1, 0);
        badgeRect.anchorMax = new Vector2(1, 0);
        badgeRect.pivot = new Vector2(1, 0);
        badgeRect.anchoredPosition = new Vector2(-4, 4);
        badgeRect.sizeDelta = new Vector2(40, 24);

        TextMeshProUGUI tmp = badgeObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "x0";
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.raycastTarget = false;

        // 4. Gan Script PotionQuickSlotUI
        var slotUI = btnObj.AddComponent<PotionQuickSlotUI>();

        Selection.activeGameObject = btnObj;
        Undo.RegisterCreatedObjectUndo(btnObj, "Create Potion Quick Slot");

        Debug.Log("<color=green>[VAELSTRIDER]</color> Đã tạo thành công Nút Bấm Bình Máu (Btn_Potion) trên Canvas!");
    }
}
