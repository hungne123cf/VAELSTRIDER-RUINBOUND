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

    private Camera cam;
    private float defaultOrthoSize = 5f;
    private float targetOrthoSize = 5f;
    private float zoomVelocity = 0f;
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null) defaultOrthoSize = cam.orthographicSize;
        targetOrthoSize = defaultOrthoSize;

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
        // Smooth Zoom hỗ trợ cả Unscaled Time (khi ngưng đọng thời gian Ulti)
        if (cam != null && Mathf.Abs(cam.orthographicSize - targetOrthoSize) > 0.005f)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetOrthoSize, ref zoomVelocity, 0.12f, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    public void SetCinematicZoom(float orthoSize)
    {
        targetOrthoSize = orthoSize;
    }

    public void ResetZoom()
    {
        targetOrthoSize = defaultOrthoSize;
    }

    public void Shake(float duration = 0.15f, float magnitude = 0.2f)
    {
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

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
