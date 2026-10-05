using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Vùng Tung Chiêu / Tia Nhắm Toàn Năng (Universal Skill Aim Indicator) cho Player 1, 2, 3.
/// Hỗ trợ hiển thị trực quan khi nhấn giữ nút:
/// - Đòn đánh thường cận chiến: Vùng quạt 120° / 180° theo hướng chỉ định.
/// - Lướt (Dash): Tia mũi tên định hướng lướt né đòn.
/// - Tuyệt kỹ Vaelstrider: Vòng xoáy bão kiếm 360° diện rộng.
/// - Tuyệt kỹ Gale Ronin: Vòng tầm quét 10.5m & Hồng tâm tự động khóa (Lock-On) mục tiêu gần nhất / Boss.
/// - Tuyệt kỹ Azure Blade: Nan quạt 60° tỏa 5 tia sóng kiếm.
/// </summary>
public class SkillAimIndicator : MonoBehaviour
{
    private static SkillAimIndicator instance;
    public static SkillAimIndicator Instance => instance;

    [Header("Line Renderers")]
    [SerializeField] private LineRenderer arcLine;
    [SerializeField] private LineRenderer directLine;
    [SerializeField] private LineRenderer circleLine;
    [SerializeField] private LineRenderer[] fanLines;

    [Header("Lock-on Reticle")]
    [SerializeField] private GameObject lockOnReticle;
    [SerializeField] private SpriteRenderer reticleSr;
    [SerializeField] private LineRenderer lockBeamLine;

    private bool isVisible = false;
    public bool IsVisible => isVisible;

    public static SkillAimIndicator EnsureExists(GameObject playerObj)
    {
        if (playerObj == null) return null;
        var ind = playerObj.GetComponent<SkillAimIndicator>();
        if (ind == null) ind = playerObj.AddComponent<SkillAimIndicator>();
        instance = ind;
        return ind;
    }

    void Awake()
    {
        instance = this;
        InitializeComponents();
        Hide();
    }

    private void InitializeComponents()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material baseMat = new Material(shader);

        // 1. Arc / Fan Line
        if (arcLine == null)
        {
            GameObject go = new GameObject("Aim_ArcLine");
            go.transform.SetParent(transform, false);
            arcLine = go.AddComponent<LineRenderer>();
            SetupLine(arcLine, baseMat, 0.08f, 5300);
        }

        // 2. Direct Arrow Line
        if (directLine == null)
        {
            GameObject go = new GameObject("Aim_DirectLine");
            go.transform.SetParent(transform, false);
            directLine = go.AddComponent<LineRenderer>();
            SetupLine(directLine, baseMat, 0.12f, 5310);
        }

        // 3. Circle Area Line
        if (circleLine == null)
        {
            GameObject go = new GameObject("Aim_CircleLine");
            go.transform.SetParent(transform, false);
            circleLine = go.AddComponent<LineRenderer>();
            SetupLine(circleLine, baseMat, 0.06f, 5290);
        }

        // 4. Fan Spread Lines (5 lines)
        if (fanLines == null || fanLines.Length != 5)
        {
            fanLines = new LineRenderer[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject go = new GameObject($"Aim_FanLine_{i}");
                go.transform.SetParent(transform, false);
                fanLines[i] = go.AddComponent<LineRenderer>();
                SetupLine(fanLines[i], baseMat, 0.08f, 5300);
            }
        }

        // 5. Lock-on Reticle & Beam
        if (lockOnReticle == null)
        {
            Transform existing = transform.Find("Aim_LockOnReticle");
            if (existing != null)
            {
                lockOnReticle = existing.gameObject;
                reticleSr = lockOnReticle.GetComponent<SpriteRenderer>();
            }
            else
            {
                lockOnReticle = new GameObject("Aim_LockOnReticle");
                lockOnReticle.transform.SetParent(transform, false);
                reticleSr = lockOnReticle.AddComponent<SpriteRenderer>();
                reticleSr.sortingLayerName = "Default";
                reticleSr.sortingOrder = 5400;
                reticleSr.sprite = GenerateLockOnSprite();
                lockOnReticle.transform.localScale = Vector3.one * 0.9f;
            }
        }

        if (lockBeamLine == null)
        {
            GameObject go = new GameObject("Aim_LockBeamLine");
            go.transform.SetParent(transform, false);
            lockBeamLine = go.AddComponent<LineRenderer>();
            SetupLine(lockBeamLine, baseMat, 0.06f, 5320);
        }
    }

    private void SetupLine(LineRenderer lr, Material mat, float width, int sortOrder)
    {
        lr.useWorldSpace = true;
        lr.sortingLayerName = "Default";
        lr.sortingOrder = sortOrder;
        lr.material = mat;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.enabled = false;
    }

    private Sprite GenerateLockOnSprite()
    {
        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color redGlow = new Color(1f, 0.25f, 0.25f, 0.95f);
        Color crosshair = new Color(1f, 0.95f, 0.35f, 1f);

        float center = (size - 1) / 2f;
        float outerR = size * 0.44f;
        float innerR = size * 0.34f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                bool isCorner = (Mathf.Abs(x - center) > outerR * 0.5f && Mathf.Abs(y - center) > outerR * 0.5f);
                if (d >= innerR && d <= outerR && isCorner)
                {
                    tex.SetPixel(x, y, redGlow);
                }
                else if (d <= 3f || ((Mathf.Abs(x - center) <= 1f || Mathf.Abs(y - center) <= 1f) && d <= outerR * 0.8f && d >= outerR * 0.45f))
                {
                    tex.SetPixel(x, y, crosshair);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    /// <summary>
    /// Hiển thị vùng quét quạt (Melee Arc) ví dụ 120° hoặc 180°
    /// </summary>
    public void ShowAimArc(Vector2 origin, Vector2 direction, float radius, float angleDeg, Color color)
    {
        Hide();
        isVisible = true;
        if (arcLine == null) return;

        arcLine.enabled = true;
        arcLine.startColor = color;
        arcLine.endColor = new Color(color.r, color.g, color.b, 0.25f);
        arcLine.startWidth = 0.09f;
        arcLine.endWidth = 0.09f;

        direction = direction.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfAngle = angleDeg / 2f;
        int segments = 24;
        arcLine.positionCount = segments + 2;

        Vector3 centerPos = (Vector3)origin + Vector3.up * 0.1f;
        arcLine.SetPosition(0, centerPos);

        for (int i = 0; i <= segments; i++)
        {
            float curA = (baseAngle - halfAngle) + (angleDeg * (float)i / segments);
            float rad = curA * Mathf.Deg2Rad;
            Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            arcLine.SetPosition(i + 1, pt);
        }
    }

    /// <summary>
    /// Hiển thị tia định hướng lướt (Dash Arrow)
    /// </summary>
    public void ShowAimLine(Vector2 origin, Vector2 direction, float length, Color color)
    {
        Hide();
        isVisible = true;
        if (directLine == null) return;

        directLine.enabled = true;
        directLine.startColor = color;
        directLine.endColor = new Color(color.r, color.g, color.b, 0.2f);
        directLine.startWidth = 0.16f;
        directLine.endWidth = 0.06f;

        direction = direction.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;

        Vector3 startPt = (Vector3)origin + Vector3.up * 0.1f;
        Vector3 endPt = startPt + (Vector3)(direction * length);

        directLine.positionCount = 2;
        directLine.SetPosition(0, startPt);
        directLine.SetPosition(1, endPt);
    }

    /// <summary>
    /// Hiển thị vòng tròn diện rộng (Circle Area) ví dụ Bão lốc 360° của Vaelstrider
    /// </summary>
    public void ShowAimCircle(Vector2 origin, float radius, Color color)
    {
        Hide();
        isVisible = true;
        if (circleLine == null) return;

        circleLine.enabled = true;
        circleLine.startColor = color;
        circleLine.endColor = color;
        circleLine.startWidth = 0.08f;
        circleLine.endWidth = 0.08f;

        int segments = 36;
        circleLine.positionCount = segments + 1;
        Vector3 centerPos = (Vector3)origin + Vector3.up * 0.1f;

        for (int i = 0; i <= segments; i++)
        {
            float rad = (i * 360f / segments) * Mathf.Deg2Rad;
            Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            circleLine.SetPosition(i, pt);
        }
    }

    /// <summary>
    /// Hiển thị vòng quét mục tiêu 10.5m & Khóa hồng tâm trên đầu kẻ địch (Lock-On) cho Gale Ronin
    /// </summary>
    public void ShowTargetLockOn(Vector2 origin, float searchRadius, Transform targetTransform, Color circleColor, Color targetColor)
    {
        Hide();
        isVisible = true;

        // 1. Vẽ vòng quét bán kính 10.5m
        if (circleLine != null)
        {
            circleLine.enabled = true;
            circleLine.startColor = circleColor;
            circleLine.endColor = circleColor;
            circleLine.startWidth = 0.06f;
            circleLine.endWidth = 0.06f;

            int segments = 40;
            circleLine.positionCount = segments + 1;
            Vector3 centerPos = (Vector3)origin + Vector3.up * 0.1f;

            for (int i = 0; i <= segments; i++)
            {
                float rad = (i * 360f / segments) * Mathf.Deg2Rad;
                Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * searchRadius, Mathf.Sin(rad) * searchRadius, 0f);
                circleLine.SetPosition(i, pt);
            }
        }

        // 2. Nếu có mục tiêu trong tầm: Đặt hồng tâm khóa & tia nối
        if (targetTransform != null)
        {
            if (lockOnReticle != null)
            {
                lockOnReticle.SetActive(true);
                lockOnReticle.transform.position = targetTransform.position + Vector3.up * 0.35f;
                lockOnReticle.transform.Rotate(0f, 0f, 120f * Time.deltaTime);
                float pingPong = 0.85f + Mathf.PingPong(Time.time * 2f, 0.25f);
                lockOnReticle.transform.localScale = Vector3.one * pingPong;
                if (reticleSr != null) reticleSr.color = targetColor;
            }

            if (lockBeamLine != null)
            {
                lockBeamLine.enabled = true;
                lockBeamLine.startColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.4f);
                lockBeamLine.endColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.85f);
                lockBeamLine.startWidth = 0.04f;
                lockBeamLine.endWidth = 0.09f;

                Vector3 startPt = (Vector3)origin + Vector3.up * 0.1f;
                Vector3 endPt = targetTransform.position + Vector3.up * 0.35f;

                lockBeamLine.positionCount = 2;
                lockBeamLine.SetPosition(0, startPt);
                lockBeamLine.SetPosition(1, endPt);
            }
        }
    }

    /// <summary>
    /// Hiển thị 5 tia sóng kiếm tỏa quạt 60° (Fan Spread) cho Azure Tempest
    /// </summary>
    public void ShowAimFan(Vector2 origin, Vector2 direction, float spreadAngle, int count, float length, Color color)
    {
        Hide();
        isVisible = true;

        direction = direction.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - (spreadAngle / 2f);
        float angleStep = spreadAngle / (count - 1);
        Vector3 centerPos = (Vector3)origin + Vector3.up * 0.1f;

        for (int i = 0; i < count && i < fanLines.Length; i++)
        {
            float curAngle = startAngle + (i * angleStep);
            float rad = curAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            var lr = fanLines[i];
            if (lr != null)
            {
                lr.enabled = true;
                lr.startColor = color;
                lr.endColor = new Color(color.r, color.g, color.b, 0.2f);
                lr.startWidth = 0.10f;
                lr.endWidth = 0.04f;

                lr.positionCount = 2;
                lr.SetPosition(0, centerPos);
                lr.SetPosition(1, centerPos + dir * length);
            }
        }
    }

    /// <summary>
    /// Ẩn toàn bộ chỉ dẫn nhắm
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        if (arcLine != null) arcLine.enabled = false;
        if (directLine != null) directLine.enabled = false;
        if (circleLine != null) circleLine.enabled = false;
        if (lockBeamLine != null) lockBeamLine.enabled = false;
        if (lockOnReticle != null) lockOnReticle.SetActive(false);

        if (fanLines != null)
        {
            foreach (var l in fanLines)
            {
                if (l != null) l.enabled = false;
            }
        }
    }
}
