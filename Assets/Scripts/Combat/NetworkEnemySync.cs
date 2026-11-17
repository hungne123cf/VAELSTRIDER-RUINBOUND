using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Quản lý đồng bộ thực thể Quái vật trong chế độ Co-op theo mô hình Host-Authoritative.
/// - Host: Chịu trách nhiệm 100% tính toán di chuyển, sát thương thực tế, AI và quyết định quái chết.
/// - Client: Đóng vai trò Puppet, nhận vị trí và máu từ Host để nội suy hiển thị mượt mà.
///   Khi Client tấn công trúng quái, hiển thị hiệu ứng/popup ngay tức thì và gửi gói tin EN_HIT lên Host.
/// </summary>
public class NetworkEnemySync : MonoBehaviour
{
    private static readonly Dictionary<int, NetworkEnemySync> _registry = new Dictionary<int, NetworkEnemySync>();
    private static readonly StringBuilder _syncSb = new StringBuilder(512);

    public static NetworkEnemySync Get(int id)
    {
        _registry.TryGetValue(id, out var sync);
        return sync;
    }

    public static IEnumerable<NetworkEnemySync> GetAll() => _registry.Values;
    public static void ClearRegistry() => _registry.Clear();

    public int NetworkId { get; private set; }
    public bool IsPuppet { get; private set; }
    public int EnemyType { get; private set; }

    private Vector3 _targetPosition;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private BotWheelAI _botWheel;
    private BloodMonsterAI _bloodMonster;
    private IDamageable _damageable;

    public void Setup(int netId, bool isPuppet, int enemyType = 0)
    {
        NetworkId = netId;
        IsPuppet = isPuppet;
        EnemyType = enemyType;
        _registry[netId] = this;

        _targetPosition = transform.position;
        _sr = GetComponentInChildren<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _botWheel = GetComponent<BotWheelAI>();
        _bloodMonster = GetComponent<BloodMonsterAI>();
        _damageable = GetComponent<IDamageable>();

        if (isPuppet)
        {
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void OnDestroy()
    {
        _registry.Remove(NetworkId);
    }

    private void Update()
    {
        if (IsPuppet)
        {
            // Nội suy vị trí từ Host mượt mà ở 60 FPS, triệt tiêu 100% giật cục và dịch chuyển tức thời
            float dist = Vector3.Distance(transform.position, _targetPosition);
            if (dist > 5.0f)
            {
                // Chỉ snap khi khoảng cách quá xa (chuyển phòng / spawn)
                transform.position = _targetPosition;
            }
            else if (dist > 0.005f)
            {
                float lerpSpeed = dist > 1.5f ? 30f : 18f;
                transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * lerpSpeed);
            }

            // Đồng bộ hoạt ảnh di chuyển mượt mà
            bool isMoving = dist > 0.08f;
            if (_botWheel != null) _botWheel.SetMoving(isMoving);
            else if (_bloodMonster != null) _bloodMonster.SetMoving(isMoving);
        }
    }

    public void UpdateState(float x, float y, bool flipX, int curHp)
    {
        _targetPosition = new Vector3(x, y, transform.position.z);
        if (_sr != null) _sr.flipX = flipX;

        if (_botWheel != null)
        {
            _botWheel.currentHealth = curHp;
            if (!_botWheel.isBoss) EnemyFloatingHealthBar.ReportHealth(gameObject, curHp, _botWheel.maxHealth);
        }
        else if (_bloodMonster != null)
        {
            _bloodMonster.currentHealth = curHp;
            if (!_bloodMonster.isBoss) EnemyFloatingHealthBar.ReportHealth(gameObject, curHp, _bloodMonster.maxHealth);
        }
    }

    public int GetCurrentHealth()
    {
        if (_botWheel != null) return _botWheel.currentHealth;
        if (_bloodMonster != null) return _bloodMonster.currentHealth;
        return EnemyHealthHelper.GetCurrentHealth(gameObject);
    }

    public void ReportLocalHit(int damage)
    {
        if (!IsPuppet) return;
        // Gửi gói tin sát thương lên Host tính toán thẩm quyền
        if (PvPNetworkManager.Instance != null && !PvPNetworkManager.Instance.IsHost)
        {
            PvPNetworkManager.Instance.SendEnemyHitPacket(NetworkId, damage);
        }
    }

    public void ApplyNetworkDamage(int damage)
    {
        if (IsPuppet) return; // Chỉ Host mới thực hiện sát thương Authoritative
        if (_damageable != null)
        {
            _damageable.TakeDamage(damage);
        }
        else if (_botWheel != null)
        {
            _botWheel.TakeDamage(damage);
        }
        else if (_bloodMonster != null)
        {
            _bloodMonster.TakeDamage(damage);
        }
    }

    public void ForceNetworkKill()
    {
        if (_botWheel != null && !_botWheel.IsDead)
        {
            _botWheel.TakeDamage(999999);
        }
        else if (_bloodMonster != null && !_bloodMonster.IsDead)
        {
            _bloodMonster.TakeDamage(999999);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void BroadcastHostEnemyStates()
    {
        if (_registry.Count == 0 || PvPNetworkManager.Instance == null || !PvPNetworkManager.Instance.IsHost) return;

        _syncSb.Clear();
        _syncSb.Append("ES|");
        bool first = true;
        foreach (var kvp in _registry)
        {
            var sync = kvp.Value;
            if (sync == null || sync.IsPuppet) continue;

            if (!first) _syncSb.Append(';');
            first = false;

            Vector3 pos = sync.transform.position;
            bool flip = sync._sr != null && sync._sr.flipX;
            int hp = sync.GetCurrentHealth();

            _syncSb.Append(sync.NetworkId).Append(',')
                   .Append(pos.x.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                   .Append(pos.y.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                   .Append(flip ? '1' : '0').Append(',')
                   .Append(hp);
        }

        if (!first)
        {
            PvPNetworkManager.Instance.SendRawPacket(_syncSb.ToString(), 2);
        }
    }
}
