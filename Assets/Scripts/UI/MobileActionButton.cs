using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Attack, Dash }
    [Header("Loại nút")]
    [SerializeField] private ButtonType buttonType = ButtonType.Attack;

    [Header("Gán trực tiếp Player vào đây (hoặc để tự tìm)")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerController playerController;

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        EnsureTargets();
    }

    void Start()
    {
        // Kiểm tra xem nút có thành phần nhận cảm ứng (Graphic) không
        Graphic graphic = GetComponent<Graphic>();
        if (graphic == null)
        {
            Debug.LogError($"[LỖI UI] Nút '{gameObject.name}' KHÔNG CÓ component Image! Unity không thể nhận cảm ứng nếu thiếu Image.");
        }
        else if (!graphic.raycastTarget)
        {
            Debug.LogWarning($"[CẢNH BÁO UI] Nút '{gameObject.name}' bị tắt Raycast Target! Tự động bật lại.");
            graphic.raycastTarget = true;
        }

        Debug.Log($"<color=white>[NÚT SẴN SÀNG]</color> {gameObject.name} cấu hình là: <b>{buttonType}</b>");
    }

    private void EnsureTargets()
    {
        if (playerCombat == null) playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

    // Sự kiện khi chạm / bấm chuột xuống
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"<color=yellow>[BẤM NÚT THÀNH CÔNG]</color> Đã nhấn vào nút: <b>{gameObject.name}</b> (Chức năng: {buttonType})");

        // Hiệu ứng nhún nút
        transform.localScale = originalScale * 0.85f;

        ExecuteAction();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    private void ExecuteAction()
    {
        EnsureTargets();

        if (buttonType == ButtonType.Attack)
        {
            if (playerCombat != null)
            {
                Debug.Log("<color=cyan>[COMBAT]</color> Kích hoạt đòn chém từ nút UI!");
                playerCombat.PerformAttack();
            }
            else
            {
                Debug.LogError("[LỖI] Nút Attack không tìm thấy PlayerCombat trên nhân vật Player! Hãy kéo Player vào ô Player Combat.");
            }
        }
        else if (buttonType == ButtonType.Dash)
        {
            if (playerController != null)
            {
                Debug.Log("<color=cyan>[DASH]</color> Kích hoạt lướt né từ nút UI!");
                playerController.PerformDash();
            }
            else
            {
                Debug.LogError("[LỖI] Nút Dash không tìm thấy PlayerController trên nhân vật Player!");
            }
        }
    }
}
