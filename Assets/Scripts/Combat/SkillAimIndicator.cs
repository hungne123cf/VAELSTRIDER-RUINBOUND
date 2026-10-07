using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Vùng Tung Chiêu / Tia Nhắm Toàn Năng (Universal Skill Aim Indicator) cho Player 1, 2, 3.
/// Thiết kế chuẩn ARPG / MOBA cao cấp:
/// - Toàn bộ vùng đều có khoảng cách (offset 0.40m - 0.45m) không chạm đè lên thân nhân vật.
/// - Đồng bộ chuẩn tâm thân nhân vật (Body Center Offset) cho cả Player 1, 2, 3.
/// - Đòn đánh thường (P1 120°, P2 180°): Vùng quạt tam giác lấp đầy (Filled Sector Mesh) phát sáng + viền sắc nét.
/// - Lướt (Dash): Đường thẳng định hướng phát sáng thanh thoát, không có đầu mũi tên to.
/// - Tuyệt kỹ Azure Blade: Nan quạt lấp đầy 60° tỏa 5 luồng đại kiếm khí (6.5m) có đường dẫn và viền quạt.
/// - Tuyệt kỹ Vaelstrider: Vành đai xoáy kiếm 360° lấp đầy (2.4m) phát sáng hoàng kim.
/// - Tuyệt kỹ Gale Ronin: Vòng radar quét 10.5m & Khóa hồng tâm chính xác vào thân quái / Boss.
/// </summary>
public class SkillAimIndicator : MonoBehaviour
{
    private static SkillAimIndicator instance;
    public static SkillAimIndicator Instance => instance;

    [Header("Mesh Renderers for Filled Areas")]
    [SerializeField] private MeshFilter sectorMf;
    [SerializeField] private MeshRenderer sectorMr;
    private Mesh sectorMesh;

    [Header("Line Renderers for Sharp Borders & Effects")]
    [SerializeField] private LineRenderer perimeterLine;
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

        // 1. Procedural Sector Mesh (Filled Fan / Cone / Ring)
        if (sectorMf == null)
        {
            Transform existing = transform.Find("Aim_SectorMesh");
            if (existing != null)
            {
                sectorMf = existing.GetComponent<MeshFilter>();
                sectorMr = existing.GetComponent<MeshRenderer>();
            }
            else
            {
                GameObject meshGo = new GameObject("Aim_SectorMesh");
                meshGo.transform.SetParent(transform, false);
                sectorMf = meshGo.AddComponent<MeshFilter>();
                sectorMr = meshGo.AddComponent<MeshRenderer>();
                sectorMr.sortingLayerName = "Default";
                sectorMr.sortingOrder = 5280;
                sectorMr.material = baseMat;
            }

            sectorMesh = new Mesh();
            sectorMesh.name = "SectorAimMesh";
            sectorMf.mesh = sectorMesh;
        }

        // 2. Perimeter Outline Line (Cho vùng quạt / sector)
        if (perimeterLine == null)
        {
            GameObject go = new GameObject("Aim_PerimeterLine");
            go.transform.SetParent(transform, false);
            perimeterLine = go.AddComponent<LineRenderer>();
            SetupLine(perimeterLine, baseMat, 0.08f, 5300);
        }

        // 3. Direct Line (Thanh định hướng lướt)
        if (directLine == null)
        {
            GameObject go = new GameObject("Aim_DirectLine");
            go.transform.SetParent(transform, false);
            directLine = go.AddComponent<LineRenderer>();
            SetupLine(directLine, baseMat, 0.16f, 5310);
        }

        // 4. Circle Area Line
        if (circleLine == null)
        {
            GameObject go = new GameObject("Aim_CircleLine");
            go.transform.SetParent(transform, false);
            circleLine = go.AddComponent<LineRenderer>();
            SetupLine(circleLine, baseMat, 0.06f, 5290);
        }

        // 5. Fan Spread Lines (5 lines)
        if (fanLines == null || fanLines.Length != 5)
        {
            fanLines = new LineRenderer[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject go = new GameObject($"Aim_FanLine_{i}");
                go.transform.SetParent(transform, false);
                fanLines[i] = go.AddComponent<LineRenderer>();
                SetupLine(fanLines[i], baseMat, 0.06f, 5305);
            }
        }

        // 6. Lock-on Reticle & Beam
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
            SetupLine(lockBeamLine, baseMat, 0.05f, 5320);
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

    private Vector3 GetPlayerCenter(Vector2 origin)
    {
        var pc = GetComponent<PlayerCombat>();
        if (pc != null)
        {
            return pc.GetBodyCenterWorld();
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            return col.bounds.center;
        }

        return (Vector3)origin + new Vector3(0f, 0.05f, 0f);
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
    /// Hiển thị VÙNG QUẠT TAM GIÁC LẤP ĐẦY (Filled Fan Sector) có khoảng cách cách xa thân nhân vật
    /// </summary>
    public void ShowAimArc(Vector2 origin, Vector2 direction, float radius, float angleDeg, Color color)
    {
        Hide();
        isVisible = true;

        direction = direction.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;

        Vector3 centerPos = GetPlayerCenter(origin);
        float innerRadius = 0.22f; // Khoảng cách cách xa thân nhân vật vừa vặn
        float outerRadius = radius;

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfAngle = angleDeg / 2f;
        int segments = 28;

        // 1. Tạo Mesh lấp đầy bề mặt hình quạt
        BuildSectorMesh(centerPos, baseAngle, halfAngle, angleDeg, innerRadius, outerRadius, segments, color);

        // 2. Tạo đường viền khép kín sắc nét
        if (perimeterLine != null)
        {
            perimeterLine.enabled = true;
            Color borderColor = new Color(color.r, color.g, color.b, 0.90f);
            perimeterLine.startColor = borderColor;
            perimeterLine.endColor = borderColor;
            perimeterLine.startWidth = 0.06f;
            perimeterLine.endWidth = 0.06f;

            int totalPts = (segments + 1) * 2 + 1;
            perimeterLine.positionCount = totalPts;

            int idx = 0;
            // Cung ngoài từ trái sang phải
            for (int i = 0; i <= segments; i++)
            {
                float curA = (baseAngle - halfAngle) + (angleDeg * (float)i / segments);
                float rad = curA * Mathf.Deg2Rad;
                Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * outerRadius, Mathf.Sin(rad) * outerRadius, 0f);
                perimeterLine.SetPosition(idx++, pt);
            }

            // Cung trong từ phải về trái
            for (int i = segments; i >= 0; i--)
            {
                float curA = (baseAngle - halfAngle) + (angleDeg * (float)i / segments);
                float rad = curA * Mathf.Deg2Rad;
                Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * innerRadius, Mathf.Sin(rad) * innerRadius, 0f);
                perimeterLine.SetPosition(idx++, pt);
            }

            // Điểm khép kín
            float startA = (baseAngle - halfAngle) * Mathf.Deg2Rad;
            Vector3 startOuter = centerPos + new Vector3(Mathf.Cos(startA) * outerRadius, Mathf.Sin(startA) * outerRadius, 0f);
            perimeterLine.SetPosition(idx, startOuter);
        }
    }

    private void BuildSectorMesh(Vector3 centerPos, float baseAngle, float halfAngle, float angleDeg, float innerR, float outerR, int segments, Color color)
    {
        if (sectorMf == null || sectorMesh == null) return;

        Transform meshT = sectorMf.transform;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[segments * 6];

        Color innerCol = new Color(color.r, color.g, color.b, 0.16f);
        Color outerCol = new Color(color.r, color.g, color.b, 0.42f);

        for (int i = 0; i <= segments; i++)
        {
            float curA = (baseAngle - halfAngle) + (angleDeg * (float)i / segments);
            float rad = curA * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            int vInner = i * 2;
            int vOuter = i * 2 + 1;

            Vector3 worldInner = centerPos + dir * innerR;
            Vector3 worldOuter = centerPos + dir * outerR;

            vertices[vInner] = meshT.InverseTransformPoint(worldInner);
            vertices[vOuter] = meshT.InverseTransformPoint(worldOuter);

            colors[vInner] = innerCol;
            colors[vOuter] = outerCol;

            if (i < segments)
            {
                int t = i * 6;
                triangles[t + 0] = vInner;
                triangles[t + 1] = vOuter;
                triangles[t + 2] = vInner + 2;

                triangles[t + 3] = vInner + 2;
                triangles[t + 4] = vOuter;
                triangles[t + 5] = vOuter + 2;
            }
        }

        sectorMesh.Clear();
        sectorMesh.vertices = vertices;
        sectorMesh.colors = colors;
        sectorMesh.triangles = triangles;
        sectorMesh.RecalculateBounds();

        if (sectorMr != null) sectorMr.enabled = true;
    }

    /// <summary>
    /// Hiển thị ĐƯỜNG THẲNG ĐỊNH HƯỚNG LƯỚT (Dash Line) thanh thoát, không có đầu mũi tên
    /// </summary>
    public void ShowAimLine(Vector2 origin, Vector2 direction, float length, Color color)
    {
        Hide();
        isVisible = true;

        direction = direction.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;

        Vector3 centerPos = GetPlayerCenter(origin);
        float offsetDist = 0.40f; // Khoảng cách cách xa thân nhân vật
        Vector3 startPt = centerPos + (Vector3)(direction * offsetDist);
        Vector3 endPt = startPt + (Vector3)(direction * length);

        if (directLine != null)
        {
            directLine.enabled = true;
            directLine.startColor = new Color(color.r, color.g, color.b, 0.90f);
            directLine.endColor = new Color(color.r, color.g, color.b, 0.30f);
            directLine.startWidth = 0.16f;
            directLine.endWidth = 0.16f;

            directLine.positionCount = 2;
            directLine.SetPosition(0, startPt);
            directLine.SetPosition(1, endPt);
        }
    }

    /// <summary>
    /// Hiển thị VÀNH ĐAI BÃO XOÁY 360° LẤP ĐẦY (Filled Ring Area) cho Vaelstrider
    /// </summary>
    public void ShowAimCircle(Vector2 origin, float radius, Color color)
    {
        Hide();
        isVisible = true;

        Vector3 centerPos = GetPlayerCenter(origin);
        float innerRadius = 0.35f; // Có khoảng cách với thân người chơi
        float outerRadius = radius;
        int segments = 36;

        // 1. Mesh vành đai 360 độ
        BuildSectorMesh(centerPos, 0f, 180f, 360f, innerRadius, outerRadius, segments, color);

        // 2. Viền ngoài phát sáng
        if (circleLine != null)
        {
            circleLine.enabled = true;
            Color borderColor = new Color(color.r, color.g, color.b, 0.90f);
            circleLine.startColor = borderColor;
            circleLine.endColor = borderColor;
            circleLine.startWidth = 0.08f;
            circleLine.endWidth = 0.08f;

            circleLine.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float rad = (i * 360f / segments) * Mathf.Deg2Rad;
                Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * outerRadius, Mathf.Sin(rad) * outerRadius, 0f);
                circleLine.SetPosition(i, pt);
            }
        }
    }

    /// <summary>
    /// Hiển thị vòng quét mục tiêu 10.5m & Khóa hồng tâm chính xác vào thân kẻ địch (Lock-On) cho Gale Ronin
    /// </summary>
    public void ShowTargetLockOn(Vector2 origin, float searchRadius, Transform targetTransform, Color circleColor, Color targetColor)
    {
        Hide();
        isVisible = true;

        Vector3 centerPos = GetPlayerCenter(origin);
        float innerRadius = 0.40f;
        int segments = 40;

        // 1. Quầng radar mờ quét 10.5m
        Color radarCol = new Color(circleColor.r, circleColor.g, circleColor.b, 0.08f);
        BuildSectorMesh(centerPos, 0f, 180f, 360f, innerRadius, searchRadius, segments, radarCol);

        // 2. Vòng tròn biên độ 10.5m
        if (circleLine != null)
        {
            circleLine.enabled = true;
            circleLine.startColor = circleColor;
            circleLine.endColor = circleColor;
            circleLine.startWidth = 0.06f;
            circleLine.endWidth = 0.06f;

            circleLine.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float rad = (i * 360f / segments) * Mathf.Deg2Rad;
                Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * searchRadius, Mathf.Sin(rad) * searchRadius, 0f);
                circleLine.SetPosition(i, pt);
            }
        }

        // 3. Khóa hồng tâm quay tròn & Tia dẫn chính xác vào trọng tâm mục tiêu
        if (targetTransform != null)
        {
            Vector3 targetCenter = targetTransform.position + Vector3.up * 0.20f;

            if (lockOnReticle != null)
            {
                lockOnReticle.SetActive(true);
                lockOnReticle.transform.position = targetCenter;
                lockOnReticle.transform.Rotate(0f, 0f, 140f * Time.deltaTime);
                float pingPong = 0.85f + Mathf.PingPong(Time.time * 2.5f, 0.25f);
                lockOnReticle.transform.localScale = Vector3.one * pingPong;
                if (reticleSr != null) reticleSr.color = targetColor;
            }

            if (lockBeamLine != null)
            {
                lockBeamLine.enabled = true;
                lockBeamLine.startColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.45f);
                lockBeamLine.endColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.95f);
                lockBeamLine.startWidth = 0.05f;
                lockBeamLine.endWidth = 0.08f;

                Vector3 startPt = centerPos + (targetCenter - centerPos).normalized * 0.40f;
                lockBeamLine.positionCount = 2;
                lockBeamLine.SetPosition(0, startPt);
                lockBeamLine.SetPosition(1, targetCenter);
            }
        }
    }

    /// <summary>
    /// Hiển thị NAN QUẠT 60° LẤP ĐẦY TỎA 5 TIA SÓNG KIẾM (Fan Spread) cho Azure Tempest (Player 3)
    /// </summary>
    public void ShowAimFan(Vector2 origin, Vector2 direction, float spreadAngle, int count, float length, Color color)
    {
        Hide();
        isVisible = true;

        direction = direction.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;

        Vector3 centerPos = GetPlayerCenter(origin);
        float innerRadius = 0.45f;
        float outerRadius = length;

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfAngle = spreadAngle / 2f;
        int segments = 24;

        // 1. Mesh lấp đầy toàn bộ nan quạt 60 độ
        BuildSectorMesh(centerPos, baseAngle, halfAngle, spreadAngle, innerRadius, outerRadius, segments, color);

        // 2. Viền ngoài nan quạt
        if (perimeterLine != null)
        {
            perimeterLine.enabled = true;
            Color borderColor = new Color(color.r, color.g, color.b, 0.85f);
            perimeterLine.startColor = borderColor;
            perimeterLine.endColor = borderColor;
            perimeterLine.startWidth = 0.06f;
            perimeterLine.endWidth = 0.06f;

            perimeterLine.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float curA = (baseAngle - halfAngle) + (spreadAngle * (float)i / segments);
                float rad = curA * Mathf.Deg2Rad;
                Vector3 pt = centerPos + new Vector3(Mathf.Cos(rad) * outerRadius, Mathf.Sin(rad) * outerRadius, 0f);
                perimeterLine.SetPosition(i, pt);
            }
        }

        // 3. 5 tia dẫn hướng sóng kiếm xuất phát từ innerRadius
        float angleStep = spreadAngle / (count - 1);
        for (int i = 0; i < count && i < fanLines.Length; i++)
        {
            float curAngle = (baseAngle - halfAngle) + (i * angleStep);
            float rad = curAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            var lr = fanLines[i];
            if (lr != null)
            {
                lr.enabled = true;
                lr.startColor = new Color(color.r, color.g, color.b, 0.75f);
                lr.endColor = new Color(color.r, color.g, color.b, 0.35f);
                lr.startWidth = 0.08f;
                lr.endWidth = 0.04f;

                lr.positionCount = 2;
                lr.SetPosition(0, centerPos + dir * innerRadius);
                lr.SetPosition(1, centerPos + dir * outerRadius);
            }
        }
    }

    /// <summary>
    /// Chuyển đổi trạng thái cảnh báo hủy chiêu (Đổi sang sắc đỏ cảnh báo hoặc mờ đi)
    /// </summary>
    public void SetCancelHighlight(bool isCancelling)
    {
        if (!isVisible) return;

        Color cancelBorder = isCancelling ? new Color(1f, 0.2f, 0.2f, 0.95f) : Color.white;
        Color cancelInner = isCancelling ? new Color(1f, 0.1f, 0.1f, 0.35f) : Color.white;

        if (perimeterLine != null && perimeterLine.enabled)
        {
            if (isCancelling)
            {
                perimeterLine.startColor = cancelBorder;
                perimeterLine.endColor = cancelBorder;
            }
        }

        if (directLine != null && directLine.enabled)
        {
            if (isCancelling)
            {
                directLine.startColor = new Color(1f, 0.2f, 0.2f, 0.95f);
                directLine.endColor = new Color(1f, 0.2f, 0.2f, 0.35f);
            }
        }

        if (circleLine != null && circleLine.enabled)
        {
            if (isCancelling)
            {
                circleLine.startColor = cancelBorder;
                circleLine.endColor = cancelBorder;
            }
        }

        if (sectorMesh != null && sectorMesh.vertexCount > 0 && isCancelling)
        {
            Color[] cols = new Color[sectorMesh.vertexCount];
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i] = (i % 2 == 0) ? new Color(1f, 0.1f, 0.1f, 0.25f) : new Color(1f, 0.15f, 0.15f, 0.55f);
            }
            sectorMesh.colors = cols;
        }
    }

    /// <summary>
    /// Ẩn toàn bộ chỉ dẫn nhắm
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        if (sectorMr != null) sectorMr.enabled = false;
        if (sectorMesh != null) sectorMesh.Clear();
        if (perimeterLine != null) perimeterLine.enabled = false;
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
