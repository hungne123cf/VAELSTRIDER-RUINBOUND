using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hệ thống điều hướng thông minh & né vật cản 2D (Context Steering + Unstuck Escape Engine)
/// Giúp tất cả quái và boss tự động luồn lách qua tường, đá, cây cối, chướng ngại vật
/// và tự động thoát kẹt (Unstuck) khi bị ép vào các góc lõm / khe hẹp giữa các prop.
/// </summary>
public static class EnemyNavigationHelper
{
    private static int obstacleLayerMask = -1;
    private static PhysicsMaterial2D sharedFrictionlessMat;
    private static readonly float[] TEST_ANGLES = new float[] { 28f, -28f, 56f, -56f, 85f, -85f };

    // Bộ theo dõi trạng thái di chuyển để phát hiện và gỡ kẹt tự động
    private class StuckTracker
    {
        public Vector2 lastReportedPos;
        public float stuckDuration = 0f;
        public Vector2 escapeDir = Vector2.zero;
        public float escapeTimer = 0f;
        public float lastCheckTime = 0f;
    }

    private static readonly Dictionary<int, StuckTracker> trackers = new Dictionary<int, StuckTracker>();

    /// <summary>
    /// LayerMask chứa tất cả các vật cản rắn (Default, Obstacle, Wall, Environment...)
    /// Ngoại trừ Player và Enemy để quái không tự né mục tiêu tấn công hoặc đồng minh.
    /// </summary>
    public static int ObstacleMask
    {
        get
        {
            if (obstacleLayerMask == -1)
            {
                obstacleLayerMask = 1 << 0; // Layer 0: Default (Tilemap, tường, cây, đá)

                int obs = LayerMask.NameToLayer("Obstacle");
                if (obs != -1) obstacleLayerMask |= (1 << obs);

                int wall = LayerMask.NameToLayer("Wall");
                if (wall != -1) obstacleLayerMask |= (1 << wall);

                int env = LayerMask.NameToLayer("Environment");
                if (env != -1) obstacleLayerMask |= (1 << env);
            }
            return obstacleLayerMask;
        }
    }

    /// <summary>
    /// Gán PhysicsMaterial2D không ma sát (Friction = 0) cho Collider của quái
    /// để khi cọ xát vào tường/đá, quái sẽ trượt mượt mà thay vì bị dính chặt.
    /// </summary>
    public static void EnsureFrictionless(Collider2D col)
    {
        if (col == null) return;

        if (sharedFrictionlessMat == null)
        {
            sharedFrictionlessMat = new PhysicsMaterial2D("EnemyFrictionlessMat")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        col.sharedMaterial = sharedFrictionlessMat;
        CorruptedEnemyBuff.AttachIfCorrupted(col.gameObject);
    }

    /// <summary>
    /// Tính toán hướng di chuyển đã né vật cản & tự động gỡ kẹt
    /// </summary>
    public static Vector2 GetSteeredDirection(
        Vector2 currentPos,
        Vector2 desiredDir,
        Collider2D selfCollider = null,
        float bodyRadius = 0.25f,
        float lookAheadDist = 1.0f)
    {
        if (desiredDir.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        Vector2 normDir = desiredDir.normalized;
        Vector2 origin = (selfCollider != null) ? (Vector2)selfCollider.bounds.center : currentPos;
        float safeRadius = Mathf.Clamp(bodyRadius, 0.15f, 0.28f);

        // 1. KIỂM TRA & XỬ LÝ GỠ KẸT TỰ ĐỘNG (UNSTUCK ENGINE)
        int id = (selfCollider != null) ? selfCollider.gameObject.GetInstanceID() : 0;
        if (id != 0)
        {
            if (!trackers.TryGetValue(id, out var tr))
            {
                tr = new StuckTracker { lastReportedPos = origin, lastCheckTime = Time.time };
                trackers[id] = tr;
            }

            // Nếu đang trong thời gian thoát kẹt khẩn cấp (Escape Phase)
            if (tr.escapeTimer > 0f)
            {
                tr.escapeTimer -= Time.deltaTime;
                // Kết hợp hướng thoát khẩn cấp với hướng tới player
                Vector2 blended = (tr.escapeDir * 0.75f + normDir * 0.25f).normalized;
                return blended;
            }

            // Kiểm tra quãng đường di chuyển mỗi 0.2s
            float deltaT = Time.time - tr.lastCheckTime;
            if (deltaT >= 0.20f)
            {
                float distMoved = Vector2.Distance(origin, tr.lastReportedPos);
                tr.lastReportedPos = origin;
                tr.lastCheckTime = Time.time;

                if (distMoved < 0.04f)
                {
                    tr.stuckDuration += deltaT;
                    // Bị kẹt đứng yên quá 0.25 giây trong khi đang muốn chạy
                    if (tr.stuckDuration >= 0.25f)
                    {
                        // Kích hoạt né vuông góc (90 độ) dạt ra khỏi khe hẹp
                        Vector2 perp = Vector2.Perpendicular(normDir);
                        // Chọn hướng bên nào thoáng hơn
                        RaycastHit2D hitLeft = Physics2D.Raycast(origin, perp, lookAheadDist, ObstacleMask);
                        RaycastHit2D hitRight = Physics2D.Raycast(origin, -perp, lookAheadDist, ObstacleMask);

                        float leftDist = hitLeft.collider == null ? 99f : hitLeft.distance;
                        float rightDist = hitRight.collider == null ? 99f : hitRight.distance;

                        tr.escapeDir = (leftDist >= rightDist) ? perp : -perp;
                        tr.escapeTimer = 0.35f; // Thoát ngang trong 0.35s
                        tr.stuckDuration = 0f;

                        return tr.escapeDir;
                    }
                }
                else
                {
                    tr.stuckDuration = 0f;
                }
            }
        }

        // 2. CONTEXT STEERING TỐI ƯU HÓA CAO (EARLY EXIT + STATIC ANGLES)
        // Kiểm tra trước hướng thẳng tới Player (chiếm 85-90% trường hợp khi không có vật cản chắn trước mặt)
        Vector2 forwardStart = origin + normDir * 0.08f;
        RaycastHit2D forwardHit = Physics2D.CircleCast(forwardStart, safeRadius, normDir, lookAheadDist - 0.08f, ObstacleMask);
        if (forwardHit.collider == null || (selfCollider != null && forwardHit.collider == selfCollider))
        {
            // Phía trước hoàn toàn thông thoáng, đi thẳng ngay lập tức mà không cần tính 6 hướng phụ!
            return normDir;
        }

        // Nếu phía trước có vật cản, đánh giá 6 góc né lân cận (Static array, zero GC allocation)
        Vector2 bestDir = normDir;
        float forwardDanger = 1.0f - Mathf.Clamp01(forwardHit.distance / (lookAheadDist - 0.08f));
        float bestScore = 1.0f - forwardDanger * 2.5f;

        for (int i = 0; i < TEST_ANGLES.Length; i++)
        {
            Vector2 testDir = RotateVector(normDir, TEST_ANGLES[i]);
            Vector2 castStart = origin + testDir * 0.08f;
            RaycastHit2D hit = Physics2D.CircleCast(castStart, safeRadius, testDir, lookAheadDist - 0.08f, ObstacleMask);

            float danger = 0f;
            if (hit.collider != null && (selfCollider == null || hit.collider != selfCollider))
            {
                danger = 1.0f - Mathf.Clamp01(hit.distance / (lookAheadDist - 0.08f));
            }

            float interest = Mathf.Max(-0.2f, Vector2.Dot(testDir, normDir));
            float score = interest - danger * 2.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = testDir;
            }
        }

        return bestDir;
    }

    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
