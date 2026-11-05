using UnityEngine;

/// <summary>
/// Giao diện chuẩn cho toàn bộ Quái vật, Lính và Trùm (Enemies & Bosses).
/// Cho phép các Lõi Nâng Cấp (Augments) truy xuất và điều chỉnh chỉ số Máu thực tế trong Runtime:
/// - Lõi Diệt Tuyệt Huyết Khí (tấn công gây thêm 50% HP kẻ địch)
/// - Lõi Huyết Thệ Cuồng Lực (+100 ATK nhưng quái/boss tăng +50% HP)
/// - Lõi Đầm Lầy Thời Gian (quái bị đánh và di chuyển chậm lại 35%)
/// </summary>
public interface IEnemyHealth
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsBoss { get; }
    void MultiplyHealth(float mult);
}
