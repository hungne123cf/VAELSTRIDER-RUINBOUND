using UnityEngine;

[ExecuteAlways]
public class AutoDepthSort : MonoBehaviour
{
    [Header("Gốc tọa độ chiều sâu")]
    [SerializeField] private int sortingOrderBase = 5000;
    [SerializeField] private int offset = 0;
    [SerializeField] private bool isStatic = false; // Tích chọn nếu là vật tĩnh (đá, nhà) để tối ưu hiệu năng

    private Renderer myRenderer;

    void Awake()
    {
        myRenderer = GetComponent<Renderer>();
        UpdateOrder();
    }

    void LateUpdate()
    {
        if (!isStatic)
        {
            UpdateOrder();
        }
    }

    private void UpdateOrder()
    {
        if (myRenderer != null)
        {
            // Càng đi xuống dưới (Y thấp hơn), Sorting Order càng lớn -> Đứng đè lên vật phía trên
            myRenderer.sortingOrder = (int)(sortingOrderBase - transform.position.y * 100) + offset;
        }
    }
}
