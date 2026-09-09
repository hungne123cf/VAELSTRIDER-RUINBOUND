using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraElevationTrigger : MonoBehaviour
{
    [Header("Góc nhìn khi bước lên đài cao")]
    [Tooltip("Độ rộng góc nhìn (mặc định game là 5.0, trên cao nên từ 6.0 - 6.5 để mở rộng toàn cảnh)")]
    [SerializeField] private float elevatedZoomSize = 6.2f;

    [Tooltip("Đẩy góc nhìn lên phía trên một chút để thấy không gian phía trước (X: ngang, Y: dọc)")]
    [SerializeField] private Vector2 cameraOffset = new Vector2(0f, 0.6f);

    [Tooltip("Thời gian chuyển cảnh mượt mà (giây)")]
    [SerializeField] private float transitionDuration = 0.55f;

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            var cam = CameraFollow.Instance ?? Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null)
            {
                cam.SetElevationView(elevatedZoomSize, cameraOffset, transitionDuration);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            var cam = CameraFollow.Instance ?? Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null)
            {
                cam.ResetElevationView(transitionDuration);
            }
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null;
    }

    private void OnDrawGizmos()
    {
        // Vẽ khung màu xanh ngọc hiển thị vùng kích hoạt góc nhìn trên cao
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
        var c = GetComponent<BoxCollider2D>();
        if (c != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(c.offset, c.size);
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireCube(c.offset, c.size);
        }
    }
}
