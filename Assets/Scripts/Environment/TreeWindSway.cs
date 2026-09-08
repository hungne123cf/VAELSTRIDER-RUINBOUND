using UnityEngine;

/// <summary>
/// Tao hieu ung gio lay tu nhien cho cay coi trong 2D Pixel Art
/// </summary>
public class TreeWindSway : MonoBehaviour
{
    [Header("Thong so gio lay")]
    [Tooltip("Toc do dao dong cua gio")]
    [SerializeField] private float swaySpeed = 1.6f;

    [Tooltip("Goc nghieng toi da khi gio thoi (do)")]
    [SerializeField] private float maxSwayAngle = 2.2f;

    [Tooltip("Do co dan ti le theo nhip tho")]
    [SerializeField] private float stretchAmount = 0.02f;

    [Tooltip("Suc manh cua con gio thoang qua")]
    [SerializeField] private float gustMultiplier = 1.3f;

    [Tooltip("Lay dong manh hon khi nguoi choi di sat qua")]
    [SerializeField] private bool reactToPlayer = true;

    private Quaternion baseRotation;
    private Vector3 baseScale;
    private float phaseOffset;
    private float playerRustleBonus = 0f;
    private Transform playerTransform;

    void Start()
    {
        baseRotation = transform.localRotation;
        baseScale = transform.localScale;

        // Tao do lech pha dua tren toa do the gioi de cac cay khong dong loat lay giong het nhau
        phaseOffset = transform.position.x * 1.85f + transform.position.y * 2.37f;

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        float time = Time.time;

        // 1. Con gio chinh (Sine wave) ket hop dot gio ngau nhien (Perlin Noise)
        float wave = Mathf.Sin(time * swaySpeed + phaseOffset);
        float gust = Mathf.PerlinNoise(time * 0.5f, phaseOffset * 0.2f) * gustMultiplier;

        // 2. Phan ung khi Nguoi choi chay ngang qua cay
        if (reactToPlayer && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < 2.0f)
            {
                playerRustleBonus = Mathf.Lerp(playerRustleBonus, 1.8f, Time.deltaTime * 6f);
            }
            else
            {
                playerRustleBonus = Mathf.Lerp(playerRustleBonus, 0f, Time.deltaTime * 2.5f);
            }
        }

        // 3. Tinh toan goc nghieng xoay quanh goc cay
        float totalAngle = wave * (maxSwayAngle + gust + playerRustleBonus);
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, totalAngle);

        // 4. Hieu ung dan hoi nhe (Breathing / Micro-stretch)
        float scaleX = baseScale.x * (1f + wave * stretchAmount);
        float scaleY = baseScale.y * (1f - wave * stretchAmount * 0.6f);
        transform.localScale = new Vector3(scaleX, scaleY, baseScale.z);
    }

    public void SetupParameters(float speed, float maxAngle, float stretch)
    {
        swaySpeed = speed;
        maxSwayAngle = maxAngle;
        stretchAmount = stretch;
    }
}
