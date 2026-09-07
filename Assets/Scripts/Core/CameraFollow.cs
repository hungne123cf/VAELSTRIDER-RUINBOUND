using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Mục tiêu bám theo")]
    [SerializeField] private Transform target;

    [Header("Độ mượt mà của Camera (Càng nhỏ càng bám nhanh)")]
    [Range(0.01f, 1f)]
    [SerializeField] private float smoothSpeed = 0.2f;

    [Header("Khoảng cách nhìn")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        // Tự động tìm Player nếu chưa kéo thả thủ công
        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
            else
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) target = pc.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Điểm đích mà Camera cần đến
        Vector3 desiredPosition = target.position + offset;

        // Di chuyển mượt mà tới mục tiêu bằng thuật toán SmoothDamp (chuẩn 60 FPS mobile)
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
    }

    // Hàm gọi rung màn hình khi chém trúng quái (Screen Shake)
    public void Shake(float duration = 0.15f, float magnitude = 0.2f)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private System.Collections.IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(transform.position.x + x, transform.position.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
