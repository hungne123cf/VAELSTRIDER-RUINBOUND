using UnityEngine;

[ExecuteAlways]
public class AutoDepthSort : MonoBehaviour
{
    [Header("Gốc tọa độ chiều sâu")]
    [SerializeField] private int sortingOrderBase = 5000;
    [SerializeField] private int offset = 0;
    [SerializeField] private bool isStatic = false; // Tích chọn nếu là vật tĩnh (đá, nhà) để tối ưu hiệu năng

    private Renderer myRenderer;

    public static int GetSortingOrderForY(float worldY, int offsetOrder = 0, int baseOrder = 5000)
    {
        int effectiveBase = Mathf.Max(baseOrder, 5000);
        // Dùng hệ số 20 đơn vị/mét (độ phân giải 5cm) và kẹp chặt trong dải an toàn [1500 .. 15000]
        // để khi nhân vật/boss đi lên vùng đảo phía Bắc (Y = 40..70) Sorting Order vẫn luôn ~3600..4200,
        // TUYỆT ĐỐI KHÔNG BAO GIỜ bị tụt xuống âm (< 0 hay < -500) dẫn đến bị ẩn dưới mặt đất!
        int computed = Mathf.RoundToInt(effectiveBase - worldY * 20f) + offsetOrder;
        return Mathf.Clamp(computed, 1500, 15000);
    }

    void Awake()
    {
        myRenderer = GetComponent<Renderer>();
        UpdateOrder();
    }

    void OnEnable()
    {
        if (myRenderer == null) myRenderer = GetComponent<Renderer>();
        UpdateOrder();
    }

    void LateUpdate()
    {
        if (!isStatic)
        {
            UpdateOrder();
        }
    }

    public void UpdateOrder()
    {
        if (myRenderer == null) myRenderer = GetComponent<Renderer>();
        if (myRenderer != null)
        {
            // Càng đi xuống dưới (Y thấp hơn), Sorting Order càng lớn -> Đứng đè lên vật phía trên
            myRenderer.sortingOrder = GetSortingOrderForY(transform.position.y, offset, sortingOrderBase);
        }
    }
}
