using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đại diện cho Đối Thủ trong Đấu Trường PvP 1v1 (Hỗ trợ cả Online Photon Fusion 2 & AI Bot Practice).
/// - Triển khai IDamageable & IEnemyHealth (trên Layer "Enemy") để 100% chiêu thức, đạn, sóng thần, thiên thạch
///   của cả 7 tướng đều va chạm và gây sát thương chuẩn xác như đánh Boss.
/// - Nạp 100% Sprite và RuntimeAnimatorController từ Resources/ (Chuẩn Android Zero-Bug Release).
/// </summary>
public class PvPRemoteOpponent : MonoBehaviour, IDamageable, IEnemyHealth
{
    public static PvPRemoteOpponent Instance { get; private set; }

    public string OpponentName { get; private set; } = "ĐỐI THỦ";
    public string OpponentHeroId { get; private set; } = CharacterManager.CHAR_VAELSTRIDER;

    private int _currentHealth = 800;
    private int _maxHealth = 800;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsBoss => true;

    private SpriteRenderer _sr;
    private Animator _animator;
    private Rigidbody2D _rb;
    private CapsuleCollider2D _collider;

    // Network interpolation state
    private Vector2 _targetPos;
    private Vector2 _predictedPos;
    private Vector2 _targetVel;
    private bool _targetFlipX;
    private Vector2 _targetScale = Vector2.one;
    private float _targetMoveX;
    private float _targetMoveY;
    private float _targetSpeed;
    private string _targetSpriteName = "";
    private bool _hasReceivedFirstState = false;
    private float _timeSinceLastNetworkState = 0f;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int SkillTriggerHash = Animator.StringToHash("SkillTrigger");
    private static readonly int SpinTriggerHash = Animator.StringToHash("SpinTrigger");

    // Overhead Health Bar
    private Transform _hpBarRoot;
    private Transform _hpFillTransform;
    private TextMesh _nameplateText;

    // Bot Animation & Combo Tracking (Được điều khiển bởi HeroAIBrain)
    private float _botAnimTimer = 0f;
    private int _botAnimFrame = 0;
    private int _botComboStep = 0;
    private float _botLastAttackTime = 0f;

    // Static Sprite Cache cho cả 7 tướng từ Resources/
    private static Dictionary<string, Sprite> _spriteCache;
    private static bool _isRegistryBuilt = false;

    public static void EnsureSpriteRegistry()
    {
        if (_isRegistryBuilt && _spriteCache != null && _spriteCache.Count > 0) return;
        _spriteCache = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);

        AbyssalMechSkill.EnsureAllSpritesLoaded();
        CelestialMageSkill.LoadAllPlayer6Sprites();
        DuneAssassinSkill.LoadAllPlayer5Sprites();

        var mechFrames = AbyssalMechSkill.GetAllFrames();
        if (mechFrames != null)
        {
            foreach (var s in mechFrames)
            {
                if (s != null && !string.IsNullOrEmpty(s.name))
                    _spriteCache[s.name] = s;
            }
        }

        string[] resourceFolders = new string[]
        {
            "Player",
            "Player2/Sprites",
            "Player3/Sprites",
            "Player4/Sprites",
            "Player5/Sprites",
            "Player6/Sprites",
            "Player6"
        };

        foreach (var folder in resourceFolders)
        {
            var sprites = Resources.LoadAll<Sprite>(folder);
            if (sprites != null)
            {
                foreach (var s in sprites)
                {
                    if (s != null && !string.IsNullOrEmpty(s.name) && !_spriteCache.ContainsKey(s.name))
                    {
                        _spriteCache[s.name] = s;
                    }
                }
            }
        }

        _isRegistryBuilt = true;
    }

    public static Sprite LookupSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        EnsureSpriteRegistry();
        if (_spriteCache != null && _spriteCache.TryGetValue(spriteName, out var s))
            return s;
        return null;
    }

    /// <summary>
    /// Lấy Sprite Avatar chân dung cận cảnh khuôn mặt (Headshot Portrait) của bất kỳ tướng nào trong 7 tướng từ Resources/UI/Avatars/
    /// để hiển thị đồng nhất trên toàn bộ UI: Sảnh PvP, Lưới Chọn Nhân Vật, Khung Chi Tiết, Màn Hình VS, Phòng Chờ và HUD Máu.
    /// Tuyệt đối không dùng sprite toàn thân ở các vị trí này.
    /// </summary>
    public static Sprite GetHeroPortraitSprite(string heroId)
    {
        Sprite face = PlayerAvatarUI.GetHeroFaceSprite(heroId);
        if (face != null) return face;

        string norm = CharacterManager.NormalizeHeroId(heroId);
        Sprite s = Resources.Load<Sprite>($"UI/Avatars/Avatar_{norm}");
        if (s != null) return s;

        string alias = "vael";
        if (norm == CharacterManager.CHAR_TITAN_BERSERKER) alias = "titan";
        else if (norm == CharacterManager.CHAR_AZURE_BLADE) alias = "azure";
        else if (norm == CharacterManager.CHAR_GALE_RONIN) alias = "ronin";
        else if (norm == CharacterManager.CHAR_DUNE_ASSASSIN) alias = "dune";
        else if (norm == CharacterManager.CHAR_CELESTIAL_MAGE) alias = "mage";
        else if (norm == CharacterManager.CHAR_ABYSSAL_MECH) alias = "mech";

        s = Resources.Load<Sprite>($"UI/Avatars/avatar_{alias}");
        if (s != null) return s;

        return MagmaCalamityHazardManager.GetSoftCircleSprite();
    }

    public static PvPRemoteOpponent SpawnOpponent(Vector3 spawnPos, string opponentName, string heroId, int maxHp)
    {
        if (Instance != null && Instance.gameObject != null)
        {
            Destroy(Instance.gameObject);
        }

        EnsureSpriteRegistry();

        GameObject go = new GameObject("PvP_RemoteOpponent");
        go.transform.position = spawnPos;

        bool isCoop = PvPMatchManager.IsCoopModeActive;
        int targetLayer = isCoop ? LayerMask.NameToLayer("Player") : LayerMask.NameToLayer("Enemy");
        if (targetLayer >= 0) go.layer = targetLayer;
        try { go.tag = isCoop ? "Player" : "Enemy"; } catch { }

        var opp = go.AddComponent<PvPRemoteOpponent>();
        opp.InitializeComponents(spawnPos, opponentName, heroId, maxHp);
        return opp;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void InitializeComponents(Vector3 spawnPos, string opponentName, string heroId, int maxHp)
    {
        _targetPos = spawnPos;
        _sr = gameObject.AddComponent<SpriteRenderer>();
        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        if (sh != null) _sr.material = new Material(sh);
        _sr.sortingOrder = 5000;

        _animator = gameObject.AddComponent<Animator>();

        _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _collider = gameObject.AddComponent<CapsuleCollider2D>();
        _collider.size = new Vector2(0.68f, 1.05f);
        _collider.offset = new Vector2(0f, 0.25f);

        if (GetComponent<AutoDepthSort>() == null)
        {
            gameObject.AddComponent<AutoDepthSort>();
        }

        // Bỏ va chạm đẩy nhau giữa Local Player và Remote Opponent
        var localPlayer = FindAnyObjectByType<PlayerController>();
        if (localPlayer != null)
        {
            var localCol = localPlayer.GetComponent<Collider2D>();
            if (localCol != null && _collider != null)
            {
                Physics2D.IgnoreCollision(localCol, _collider, true);
            }
        }

        // Hiển thị Nameplate Tên Đồng Đội trên đầu trong chế độ Co-op
        if (_nameplateText == null && PvPMatchManager.IsCoopModeActive)
        {
            GameObject npObj = new GameObject("TeammateNameplate");
            npObj.transform.SetParent(transform, false);
            npObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            _nameplateText = npObj.AddComponent<TextMesh>();
            _nameplateText.fontSize = 28;
            _nameplateText.characterSize = 0.055f;
            _nameplateText.alignment = TextAlignment.Center;
            _nameplateText.anchor = TextAnchor.MiddleCenter;
            _nameplateText.color = new Color(0.40f, 0.95f, 0.65f, 1f);
            var mr = npObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 6000;
        }

        ApplyOpponentIdentity(opponentName, heroId, maxHp, maxHp);
    }

    public void ApplyOpponentIdentity(string oppName, string heroId, int maxHp, int curHp)
    {
        OpponentName = string.IsNullOrEmpty(oppName) ? "ĐỐI THỦ" : oppName;
        string normHero = CharacterManager.NormalizeHeroId(heroId);
        bool heroChanged = (OpponentHeroId != normHero) || (_sr.sprite == null);
        OpponentHeroId = normHero;

        _maxHealth = Mathf.Max(1, maxHp);
        _currentHealth = Mathf.Clamp(curHp, 0, _maxHealth);

        if (_nameplateText != null)
        {
            bool isRemoteHost = (PvPNetworkManager.Instance != null && !PvPNetworkManager.Instance.IsHost);
            string prefix = isRemoteHost ? "👑 " : "";
            string cleanName = OpponentName.Replace("[", "(").Replace("]", ")");
            _nameplateText.text = $"{prefix}{cleanName}";
            _nameplateText.color = isRemoteHost ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.40f, 0.95f, 0.65f, 1f);
        }

        if (heroChanged)
        {
            ApplyHeroVisualFromResources(OpponentHeroId);
        }

        if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsBotPracticeMode)
        {
            var brain = GetComponent<HeroAIBrain>();
            if (brain == null) brain = gameObject.AddComponent<HeroAIBrain>();
            brain.ConfigureHeroProfile();
        }
    }

    private void ApplyHeroVisualFromResources(string normHero)
    {
        EnsureSpriteRegistry();

        if (normHero == CharacterManager.CHAR_TITAN_BERSERKER)
        {
            transform.localScale = Vector3.one;
            _targetScale = Vector2.one;
            _collider.offset = new Vector2(0f, 0.65f);
            _collider.size = new Vector2(0.90f, 1.30f);

            var idleObjs = Resources.LoadAll<Sprite>("Player4/Sprites/Hammer_Idle");
            if (idleObjs != null && idleObjs.Length > 0) _sr.sprite = idleObjs[0];

            var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/Player4");
            if (ctrl != null)
            {
                _animator.enabled = true;
                _animator.runtimeAnimatorController = ctrl;
            }
            PlayerPixelAura.EnsureAuraForPlayer(gameObject, true);
        }
        else if (normHero == CharacterManager.CHAR_AZURE_BLADE)
        {
            transform.localScale = Vector3.one;
            _targetScale = Vector2.one;
            _collider.offset = Vector2.zero;
            _collider.size = new Vector2(0.55f, 0.85f);

            var idleObjs = Resources.LoadAll<Sprite>("Player3/Sprites/Sword_Idle");
            if (idleObjs != null && idleObjs.Length > 0) _sr.sprite = idleObjs[0];

            var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/Player3");
            if (ctrl != null)
            {
                _animator.enabled = true;
                _animator.runtimeAnimatorController = ctrl;
            }
            PlayerPixelAura.EnsureAuraForPlayer(gameObject, false);
        }
        else if (normHero == CharacterManager.CHAR_GALE_RONIN)
        {
            transform.localScale = Vector3.one;
            _targetScale = Vector2.one;
            _collider.offset = new Vector2(0f, -0.05f);
            _collider.size = new Vector2(0.55f, 0.80f);

            var idleObjs = Resources.LoadAll<Sprite>("Player2/Sprites/_down idle");
            if (idleObjs != null && idleObjs.Length > 0) _sr.sprite = idleObjs[0];

            var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/Player2");
            if (ctrl != null)
            {
                _animator.enabled = true;
                _animator.runtimeAnimatorController = ctrl;
            }
            PlayerPixelAura.EnsureAuraForPlayer(gameObject, false);
        }
        else if (normHero == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            transform.localScale = Vector3.one;
            _targetScale = Vector2.one;
            _collider.offset = Vector2.zero;
            _collider.size = new Vector2(0.55f, 0.80f);
            _animator.enabled = false;

            var idles = Resources.LoadAll<Sprite>("Player5/Sprites/idle");
            if (idles != null && idles.Length > 0) _sr.sprite = idles[0];
            PlayerPixelAura.EnsureAuraForPlayer(gameObject, false);
        }
        else if (normHero == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            _targetScale = new Vector2(0.62f, 0.62f);
            _collider.offset = Vector2.zero;
            _collider.size = new Vector2(0.55f, 0.80f);
            _animator.enabled = false;

            CelestialMageSkill.LoadAllPlayer6Sprites();
            var idles = Resources.LoadAll<Sprite>("Player6/Sprites/idle/sprite sheets/idle");
            if (idles == null || idles.Length == 0) idles = Resources.LoadAll<Sprite>("Player6/Sprites/idle");
            if (idles != null && idles.Length > 0) _sr.sprite = idles[0];
            PlayerPixelAura.EnsureAuraForPlayer(gameObject, false);
        }
        else if (normHero == CharacterManager.CHAR_ABYSSAL_MECH)
        {
            transform.localScale = Vector3.one;
            _targetScale = Vector2.one;
            _collider.offset = new Vector2(0f, 0.08f);
            _collider.size = new Vector2(0.72f, 1.10f);
            _animator.enabled = false;

            AbyssalMechSkill.EnsureAllSpritesLoaded();
            var frames = AbyssalMechSkill.GetIdleFrames();
            if (frames != null && frames.Length > 0) _sr.sprite = frames[0];
            PlayerPixelAura.EnsureAuraForPlayer(gameObject, false);
        }
        else
        {
            transform.localScale = Vector3.one;
            _targetScale = Vector2.one;
            _collider.offset = new Vector2(0f, 1.25f);
            _collider.size = new Vector2(0.55f, 0.95f);

            var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/Player1");
            if (ctrl != null)
            {
                _animator.enabled = true;
                _animator.runtimeAnimatorController = ctrl;
            }
            PlayerPixelAura.EnsureAuraForPlayer(gameObject, false);
        }
    }

    private void CreateOverheadHealthBar()
    {
        // Vô hiệu hóa hoàn toàn Overhead Health Bar và Nameplate theo yêu cầu thiết kế đối kháng
    }

    public void OnReceiveNetworkState(
        Vector2 pos, Vector2 vel, bool flipX, Vector2 scale,
        float moveX, float moveY, float speed,
        int curHp, int maxHp, string spriteName, string heroId)
    {
        if (OpponentHeroId != heroId)
        {
            ApplyOpponentIdentity(OpponentName, heroId, maxHp, curHp);
        }

        _targetPos = pos;
        _targetVel = vel;
        _targetFlipX = flipX;
        if (Mathf.Abs(scale.x) > 0.1f && Mathf.Abs(scale.y) > 0.1f)
        {
            _targetScale = scale;
        }
        if (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            _targetScale = new Vector2(0.62f, 0.62f);
        }
        _targetMoveX = moveX;
        _targetMoveY = moveY;
        _targetSpeed = speed;
        _maxHealth = Mathf.Max(1, maxHp);
        _currentHealth = Mathf.Clamp(curHp, 0, _maxHealth);
        _targetSpriteName = spriteName;
        _timeSinceLastNetworkState = 0f;

        // Bù độ trễ mạng thực tế (RTT) có giới hạn ngưỡng chống nhiễu
        float rawRtt = (PvPNetworkManager.Instance != null) ? PvPNetworkManager.Instance.GetEstimatedLatencySeconds() : 0.04f;
        float rtt = Mathf.Clamp(rawRtt, 0.015f, 0.080f);
        Vector2 targetProjected = ClampPositionIfPvP(pos + vel * rtt);

        if (!_hasReceivedFirstState)
        {
            _hasReceivedFirstState = true;
            _predictedPos = targetProjected;
            transform.position = new Vector3(_predictedPos.x, _predictedPos.y, 0f);
        }
        else
        {
            float err = Vector2.Distance(_predictedPos, targetProjected);
            if (err > 2.5f)
            {
                // Lệch quá xa (Tốc biến / Hồi sinh / Lag spike lớn) -> Snap ngay lập tức
                _predictedPos = targetProjected;
                transform.position = new Vector3(_predictedPos.x, _predictedPos.y, 0f);
            }
            else
            {
                // Triệt tiêu micro-jerk lùi lại: nội suy mượt vị trí đích tới điểm dự đoán mới
                _predictedPos = Vector2.Lerp(_predictedPos, targetProjected, 0.70f);
            }
        }
    }

    private static Vector2 ClampPositionIfPvP(Vector2 pos)
    {
        if (PvPMatchManager.IsPvPModeActive)
        {
            return PvPMatchManager.ClampInsideColosseum(pos);
        }
        return pos;
    }

    private void Update()
    {
        if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsBotPracticeMode)
        {
            // HeroAIBrain điều khiển toàn bộ AI Bot
            return;
        }

        _timeSinceLastNetworkState += Time.deltaTime;
        if (_timeSinceLastNetworkState > 0.18f)
        {
            // Quá 180ms không có gói tin (rớt gói hoặc đứng yên) -> Giảm dần vận tốc về 0 mượt mà (Damping), chống trượt xuyên tường
            _targetVel = Vector2.MoveTowards(_targetVel, Vector2.zero, Time.deltaTime * 24f);
        }

        // THUẬT TOÁN DEAD RECKONING 60 FPS:
        // Tích phân vận tốc liên tục từng khung hình để nhân vật di chuyển mượt mà,
        // triệt tiêu 100% cảm giác giật cục, trễ nhịp hoặc đứng hình chờ gói tin!
        if (_targetVel.sqrMagnitude > 0.01f)
        {
            _predictedPos += _targetVel * Time.deltaTime;
            _predictedPos = ClampPositionIfPvP(_predictedPos);
        }

        float dist = Vector2.Distance(transform.position, _predictedPos);
        if (dist > 2.5f)
        {
            // Lệch vị trí quá xa (Tốc biến / Lag spike) -> Snap ngay lập tức
            transform.position = new Vector3(_predictedPos.x, _predictedPos.y, 0f);
        }
        else
        {
            float lerpSpeed = (_targetVel.sqrMagnitude > 0.01f) ? 32f : 45f;
            if (dist > 0.75f) lerpSpeed *= (1f + (dist - 0.75f) * 1.5f);
            Vector2 smoothPos = Vector2.Lerp(transform.position, _predictedPos, Time.deltaTime * lerpSpeed);
            transform.position = new Vector3(smoothPos.x, smoothPos.y, 0f);
        }

        if (_botAttackLockTimer > 0f)
        {
            _botAttackLockTimer -= Time.deltaTime;
        }

        transform.localScale = new Vector3(_targetScale.x, _targetScale.y, 1f);

        if (_sr != null)
        {
            _sr.flipX = _targetFlipX;
        }

        if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
        {
            _animator.SetFloat(MoveXHash, _targetMoveX);
            _animator.SetFloat(MoveYHash, _targetMoveY);
            _animator.SetFloat(SpeedHash, (_botAttackLockTimer > 0f) ? 0f : _targetSpeed);
        }
    }

    private void LateUpdate()
    {
        // Cập nhật vị trí Nameplate Tên Đồng Đội luôn nằm chuẩn xác trên đầu nhân vật (theo bounds sprite)
        if (_nameplateText != null)
        {
            _nameplateText.transform.rotation = Quaternion.identity;
            _nameplateText.transform.localScale = Vector3.one;

            float headY = transform.position.y + 1.25f;
            if (_sr != null && _sr.sprite != null)
            {
                headY = _sr.bounds.max.y + 0.28f;
            }
            _nameplateText.transform.position = new Vector3(transform.position.x, headY, 0f);

            bool isRemoteHost = (PvPNetworkManager.Instance != null && !PvPNetworkManager.Instance.IsHost);
            string prefix = isRemoteHost ? "👑 " : "";
            string cleanName = OpponentName.Replace("[", "(").Replace("]", ")");
            _nameplateText.text = $"{prefix}{cleanName}";
            _nameplateText.color = isRemoteHost ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.40f, 0.95f, 0.65f, 1f);
        }

        // Khi đối thủ đang trong thời gian tung chiêu hoặc vung đòn (Animation / VFX Lock):
        // Tuyệt đối KHÔNG ĐƯỢC ghi đè sprite tĩnh lên để hoạt ảnh Animation và Sprite chiêu thức hiển thị trọn vẹn 100%!
        if (_botAttackLockTimer > 0f || _botActionAnimRoutine != null) return;

        // Đồng bộ chính xác Sprite của đối thủ (Đặc biệt cho Player 5, 6, 7 và các tư thế chiêu thức)
        if (!string.IsNullOrEmpty(_targetSpriteName) && _sr != null)
        {
            Sprite syncedSpr = LookupSprite(_targetSpriteName);
            if (syncedSpr != null)
            {
                _sr.sprite = syncedSpr;
            }
        }
    }

    private void UpdateOverheadBar()
    {
        if (_hpFillTransform != null && _maxHealth > 0)
        {
            float ratio = Mathf.Clamp01((float)_currentHealth / _maxHealth);
            _hpFillTransform.localScale = new Vector3(ratio, 1f, 1f);
        }
    }

    // =========================================================================
    // NHẬN SÁT THƯƠNG KHI LOCAL PLAYER ĐÁNH TRÚNG (IDamageable & IEnemyHealth)
    // =========================================================================
    public void MultiplyHealth(float mult)
    {
        // Trong PvP không cho phép lõi nhân máu quái làm lệch máu thực của đối thủ
    }

    public void ResetForNewRound(Vector3 spawnPos)
    {
        _currentHealth = _maxHealth;
        transform.position = spawnPos;
        _targetPos = spawnPos;
        _predictedPos = spawnPos;
        _targetVel = Vector2.zero;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        UpdateOverheadBar();
    }

    public void TakeDamage(int damage)
    {
        if (PvPMatchManager.IsCoopModeActive)
        {
            // Trong Co-op, đòn đánh của người chơi không gây sát thương lên đồng đội (No Friendly Fire)
            return;
        }
        if (damage <= 0 || _currentHealth <= 0) return;
        if (PvPMatchManager.Instance != null && !PvPMatchManager.Instance.IsCombatActive) return;

        // Giới hạn sát thương 1 đòn tối đa 28% MaxHP để chống sốc sát thương 1-hit trong PvP
        int balancedDmg = Mathf.Clamp(damage, 1, Mathf.RoundToInt(_maxHealth * 0.28f));

        StartCoroutine(HitFlashRoutine());

        if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsBotPracticeMode)
        {
            _currentHealth = Mathf.Max(0, _currentHealth - balancedDmg);
            if (_currentHealth <= 0 && PvPMatchManager.Instance != null)
            {
                PvPMatchManager.Instance.OnRoundFinished(localPlayerWon: true, "K.O");
            }
        }
        else if (PvPNetworkManager.Instance != null)
        {
            // Dự đoán trừ máu tức thì trên màn hình và gửi gói tin trừ máu sang máy đối thủ
            _currentHealth = Mathf.Max(1, _currentHealth - balancedDmg);
            Vector2 knockDir = Vector2.zero;
            var localP = FindAnyObjectByType<PlayerController>();
            if (localP != null)
            {
                knockDir = ((Vector2)transform.position - (Vector2)localP.transform.position).normalized * 2.5f;
            }
            PvPNetworkManager.Instance.SendDamageToOpponentPacket(balancedDmg, false, knockDir, 0f);
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (_sr == null) yield break;
        _sr.color = new Color(1f, 0.3f, 0.3f, 1f);
        yield return new WaitForSeconds(0.10f);
        if (_sr != null) _sr.color = Color.white;
    }

    // =========================================================================
    // HIỂN THỊ HIỆU ỨNG KỸ NĂNG CỦA ĐỐI THỦ (REMOTE ACTION VFX)
    // =========================================================================
    public static float GetHeroChestOffsetY(string heroId)
    {
        string norm = CharacterManager.NormalizeHeroId(heroId);
        if (norm == CharacterManager.CHAR_ABYSSAL_MECH) return 0.55f;
        if (norm == CharacterManager.CHAR_TITAN_BERSERKER) return 0.65f;
        if (norm == CharacterManager.CHAR_CELESTIAL_MAGE) return 0.35f;
        if (norm == CharacterManager.CHAR_DUNE_ASSASSIN) return 0.45f;
        if (norm == CharacterManager.CHAR_AZURE_BLADE) return 0.50f;
        if (norm == CharacterManager.CHAR_GALE_RONIN) return 0.50f;
        return 0.55f;
    }

    private Coroutine _botActionAnimRoutine = null;
    public bool IsActionAnimationPlaying => _botActionAnimRoutine != null;
    private float _botAttackLockTimer = 0f;

    private void PlayCustomSpriteAttack(bool isUlti, int comboStep = 0)
    {
        if (_botActionAnimRoutine != null) StopCoroutine(_botActionAnimRoutine);
        _botActionAnimRoutine = StartCoroutine(CustomSpriteAttackRoutine(isUlti, comboStep));
    }

    private IEnumerator CustomSpriteAttackRoutine(bool isUlti, int comboStep = 0)
    {
        Sprite[] frames = null;
        float frameTime = 0.052f;

        if (OpponentHeroId == CharacterManager.CHAR_ABYSSAL_MECH)
        {
            if (isUlti) frames = AbyssalMechSkill.GetUltiFrames();
            else if (comboStep == 1) frames = AbyssalMechSkill.GetAtk2Frames();
            else if (comboStep == 2) frames = AbyssalMechSkill.GetAtk3Frames();
            else frames = AbyssalMechSkill.GetAtk1Frames();
            frameTime = 0.055f;
        }
        else if (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            frames = isUlti ? CelestialMageSkill.GetUltiFrames() : CelestialMageSkill.GetAttackFrames();
            frameTime = 0.048f;
        }
        else if (OpponentHeroId == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            if (isUlti) frames = DuneAssassinSkill.GetSpAtkFrames();
            else if (comboStep == 1) frames = DuneAssassinSkill.GetAtk2Frames();
            else if (comboStep == 2) frames = DuneAssassinSkill.GetAtk3Frames();
            else frames = DuneAssassinSkill.GetAtk1Frames();
            frameTime = 0.046f;
        }

        if (frames != null && frames.Length > 0 && _sr != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) _sr.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
        }
        _botActionAnimRoutine = null;
    }

    public IEnumerator PerformMeleeHitRoutine(float windupDelay, float hitRange, int damage, bool isUlti, Vector2 attackDir)
    {
        yield return new WaitForSeconds(windupDelay);

        if (PvPMatchManager.IsCoopModeActive)
        {
            // Trong Co-op: Đòn cận chiến của đồng đội quét trúng quái vật hầm ngục
            Vector2 origin = (Vector2)transform.position + attackDir * 0.45f;
            Collider2D[] enemyHits = Physics2D.OverlapCircleAll(origin, hitRange, 1 << LayerMask.NameToLayer("Enemy"));
            foreach (var h in enemyHits)
            {
                if (h != null && h.gameObject != gameObject)
                {
                    var dmg = h.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        dmg.TakeDamage(damage);
                        DamagePopup.Create(h.transform.position, damage, isUlti ? DamageType.Magic : DamageType.Physical, isCritical: isUlti);
                    }
                }
            }
            yield break;
        }

        var localPlayer = FindAnyObjectByType<PlayerController>();
        if (localPlayer == null) yield break;
        var localHealth = localPlayer.GetComponent<PlayerHealth>();
        if (localHealth == null || localHealth.IsDead) yield break;

        Vector2 myPos = transform.position;
        Vector2 targetPos = localPlayer.transform.position;
        Vector2 toTarget = targetPos - myPos;
        float dist = toTarget.magnitude;

        // Ulti quét 360 độ xung quanh, đòn đánh thường kiểm tra góc chém phía trước mặt
        bool inDirection = isUlti || (Vector2.Dot(toTarget.normalized, attackDir) >= -0.25f);

        if (dist <= hitRange && inDirection)
        {
            SoundManager.EnsureInstance()?.PlayHitImpact();
            localHealth.TakeDamage(damage);
            Vector2 chestPos = (Vector2)localPlayer.transform.position + Vector2.up * 0.55f;
            DamagePopup.Create(chestPos, damage, isUlti ? DamageType.Magic : DamageType.Physical, isCritical: isUlti);
        }
    }

    public void ExecuteBotAction(string actionType, Vector2 dir, Vector2 originPos, int comboStep = 0)
    {
        _botComboStep = comboStep;
        if (actionType == "ATK") _botAttackLockTimer = 0.45f;
        else if (actionType == "ULTI") _botAttackLockTimer = 0.70f;
        PlayRemoteActionVFX(actionType, dir, originPos);
    }

    public void PlayRemoteActionVFX(string actionType, Vector2 dir, Vector2 originPos)
    {
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.left;
        dir.Normalize();

        if (_sr != null)
        {
            _sr.flipX = (dir.x < 0f);
        }

        if (actionType == "ATK")
        {
            _botAttackLockTimer = 0.45f;
            if (Time.time - _botLastAttackTime < 1.35f)
            {
                _botComboStep = (_botComboStep + 1) % 3;
            }
            else
            {
                _botComboStep = 0;
            }
            _botLastAttackTime = Time.time;

            SoundManager.EnsureInstance()?.PlaySwordSlash();

            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                _animator.SetFloat(MoveXHash, dir.x);
                _animator.SetFloat(MoveYHash, dir.y);
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetInteger("ComboStep", _botComboStep + 1);
                _animator.SetTrigger(AttackTriggerHash);

                string stateName = "Attack";
                if (OpponentHeroId == CharacterManager.CHAR_VAELSTRIDER)
                {
                    stateName = (_botComboStep >= 1) ? "Attack2" : "Attack1";
                }
                else if (OpponentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
                {
                    stateName = (_botComboStep == 2) ? "Attack2" : "Attack";
                }
                else if (OpponentHeroId == CharacterManager.CHAR_AZURE_BLADE || OpponentHeroId == CharacterManager.CHAR_GALE_RONIN)
                {
                    stateName = (_botComboStep >= 1) ? "Attack2" : "Attack";
                }

                if (_animator.HasState(0, Animator.StringToHash(stateName)))
                {
                    _animator.Play(stateName, 0, 0f);
                }
                else if (_animator.HasState(0, Animator.StringToHash("Attack")))
                {
                    _animator.Play("Attack", 0, 0f);
                }
                else if (_animator.HasState(0, Animator.StringToHash("Attack1")))
                {
                    _animator.Play("Attack1", 0, 0f);
                }
            }
            else
            {
                PlayCustomSpriteAttack(isUlti: false, comboStep: _botComboStep);
            }

            if (OpponentHeroId == CharacterManager.CHAR_ABYSSAL_MECH)
            {
                float bSpeed = 10.5f + _botComboStep * 1.5f;
                float bScale = 1.15f + _botComboStep * 0.25f;
                int bDmg = Random.Range(28 + _botComboStep * 6, 40 + _botComboStep * 8);
                StartCoroutine(SpawnRemoteWaterBallRoutine(dir, originPos, bSpeed, bScale, bDmg));
            }
            else if (OpponentHeroId == CharacterManager.CHAR_AZURE_BLADE)
            {
                if (_botComboStep >= 1)
                {
                    StartCoroutine(SpawnRemoteSwordWaveRoutine(dir, originPos, 12.5f, 1.25f, Random.Range(32, 48), new Color(0.4f, 0.85f, 1f, 1f)));
                }
                else
                {
                    StartCoroutine(PerformMeleeHitRoutine(0.12f, 1.8f, Random.Range(25, 35), false, dir));
                }
            }
            else if (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE)
            {
                StartCoroutine(SpawnRemoteCelestialOrbRoutine(dir, originPos, 11f, 1.15f, Random.Range(32, 48)));
                if (_botComboStep == 2)
                {
                    Vector2 altDir = Quaternion.Euler(0, 0, 15f) * dir;
                    StartCoroutine(SpawnRemoteCelestialOrbRoutine(altDir, originPos, 11f, 1.0f, Random.Range(24, 36)));
                }
            }
            else if (OpponentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
            {
                if (_botComboStep == 2)
                {
                    IceSlamVFX.Spawn(originPos + dir * 1.2f, dir, 1.6f, 1.2f);
                    StartCoroutine(PerformMeleeHitRoutine(0.15f, 2.2f, Random.Range(40, 55), false, dir));
                }
                else
                {
                    StartCoroutine(PerformMeleeHitRoutine(0.15f, 2.0f, Random.Range(28, 38), false, dir));
                }
            }
            else if (OpponentHeroId == CharacterManager.CHAR_GALE_RONIN)
            {
                if (_botComboStep >= 1)
                {
                    StartCoroutine(SpawnRemoteSwordWaveRoutine(dir, originPos, 13f, 1.1f, Random.Range(30, 45), new Color(0.2f, 1f, 0.65f, 1f)));
                }
                else
                {
                    StartCoroutine(PerformMeleeHitRoutine(0.12f, 1.8f, Random.Range(25, 35), false, dir));
                }
            }
            else
            {
                StartCoroutine(PerformMeleeHitRoutine(0.12f, 1.8f, Random.Range(25, 35), false, dir));
            }
        }
        else if (actionType == "ULTI")
        {
            _botAttackLockTimer = 0.85f; // Khóa giữ hoạt ảnh và cấm đè sprite đứng im trong lúc tung chiêu!
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();

            // 1. Kích hoạt hoạt ảnh (Animation / Sprite Frames)
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                if (OpponentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
                {
                    _animator.SetTrigger(SpinTriggerHash);
                    _animator.Play("Spin", 0, 0f);
                }
                else if (OpponentHeroId == CharacterManager.CHAR_AZURE_BLADE || OpponentHeroId == CharacterManager.CHAR_GALE_RONIN)
                {
                    _animator.Play("Attack2", 0, 0f);
                }
                else
                {
                    if (_animator.HasState(0, Animator.StringToHash("Player_Ulti")))
                    {
                        _animator.Play("Player_Ulti", 0, 0f);
                    }
                    else if (_animator.HasState(0, Animator.StringToHash("Attack2")))
                    {
                        _animator.Play("Attack2", 0, 0f);
                    }
                    else
                    {
                        _animator.Play("Attack", 0, 0f);
                    }
                }
            }
            else
            {
                PlayCustomSpriteAttack(isUlti: true);
            }

            // 2. KHỞI TẠO HIỆU ỨNG VFX CHIÊU THỨC HOÀNG KIM CHO MỌI TƯỚNG (Không bao giờ bị sót!)
            if (OpponentHeroId == CharacterManager.CHAR_ABYSSAL_MECH)
            {
                StartCoroutine(SpawnRemoteSharkTsunamiRoutine(dir, originPos));
            }
            else if (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE)
            {
                StartCoroutine(SpawnRemoteCelestialOrbRoutine(dir, originPos, 14f, 1.8f, 75));
                StartCoroutine(SpawnVisualRingPulseRoutine(originPos, 2.8f, new Color(0.6f, 0.2f, 1f, 0.85f), 0.35f));
            }
            else if (OpponentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
            {
                IceSlamVFX.Spawn(originPos, dir, 2.5f, 1.5f);
                StartCoroutine(PerformMeleeHitRoutine(0.25f, 2.8f, 85, true, dir));
            }
            else if (OpponentHeroId == CharacterManager.CHAR_AZURE_BLADE)
            {
                StartCoroutine(SpawnRemoteSwordWaveRoutine(dir, originPos, 14f, 1.85f, 80, new Color(0.4f, 0.85f, 1f, 1f)));
            }
            else if (OpponentHeroId == CharacterManager.CHAR_GALE_RONIN)
            {
                GaleCrescentWave.Spawn(originPos, dir, 75, 16f, 1.2f, null);
                StartCoroutine(SpawnRemoteSwordWaveRoutine(dir, originPos, 16f, 1.35f, 75, new Color(0.2f, 1f, 0.65f, 1f)));
            }
            else if (OpponentHeroId == CharacterManager.CHAR_DUNE_ASSASSIN)
            {
                StartCoroutine(SpawnVisualRingPulseRoutine(originPos, 2.2f, new Color(1f, 0.6f, 0.1f, 0.85f), 0.30f));
                StartCoroutine(SpawnRemoteSwordWaveRoutine(dir, originPos, 15f, 1.35f, 70, new Color(1f, 0.65f, 0.15f, 1f)));
            }
            else
            {
                StartCoroutine(SpawnRemoteSwordWaveRoutine(dir, originPos, 13f, 1.5f, 65, new Color(1f, 0.85f, 0.2f, 1f)));
                StartCoroutine(SpawnVisualRingPulseRoutine(originPos, 2.2f, new Color(1f, 0.85f, 0.2f, 0.75f), 0.3f));
            }
        }
        else if (actionType == "DASH")
        {
            _botAttackLockTimer = 0.25f;
            SoundManager.EnsureInstance()?.PlayHitImpact();
            if (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE)
            {
                transform.position = ClampPositionIfPvP(transform.position + (Vector3)(dir * 2.4f));
                StartCoroutine(SpawnVisualRingPulseRoutine(originPos, 1.5f, new Color(0.85f, 0.45f, 1f, 0.85f), 0.22f));
            }
            else
            {
                transform.position = ClampPositionIfPvP(transform.position + (Vector3)(dir * 1.8f));
                StartCoroutine(SpawnVisualRingPulseRoutine(originPos, 1.2f, new Color(0.6f, 0.9f, 1f, 0.65f), 0.18f));
            }
        }
    }

    private IEnumerator SpawnRemoteSharkTsunamiRoutine(Vector2 dir, Vector2 startPos)
    {
        var tsunamiSprites = AbyssalMechSkill.GetSharkTsunamiFrames();
        GameObject waveObj = new GameObject("Remote_SharkTsunami");
        waveObj.transform.position = startPos;
        waveObj.transform.localScale = new Vector3(dir.x < 0 ? -1.45f : 1.45f, 1.45f, 1f);
        var sr = waveObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5600;
        if (tsunamiSprites != null && tsunamiSprites.Length > 0)
            sr.sprite = tsunamiSprites[0];
        else
            sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();

        float elapsed = 0f;
        float duration = 1.1f;
        bool hitLocalPlayer = false;
        var p = GameObject.FindWithTag("Player");
        PlayerHealth pHealth = (p != null) ? p.GetComponent<PlayerHealth>() : null;

        while (elapsed < duration && waveObj != null)
        {
            elapsed += Time.deltaTime;
            waveObj.transform.position += (Vector3)(dir * 9.5f * Time.deltaTime);
            if (tsunamiSprites != null && tsunamiSprites.Length > 0)
            {
                int idx = Mathf.FloorToInt((elapsed / duration) * tsunamiSprites.Length) % tsunamiSprites.Length;
                sr.sprite = tsunamiSprites[idx];
            }

            if (PvPMatchManager.IsCoopModeActive)
            {
                Collider2D[] enemyHits = Physics2D.OverlapCircleAll(waveObj.transform.position, 1.45f, 1 << LayerMask.NameToLayer("Enemy"));
                foreach (var h in enemyHits)
                {
                    if (h != null)
                    {
                        var d = h.GetComponent<IDamageable>();
                        if (d != null)
                        {
                            d.TakeDamage(80);
                            DamagePopup.Create(h.transform.position, 80, DamageType.Magic, isCritical: true);
                            SoundManager.EnsureInstance()?.PlayHitImpact();
                        }
                    }
                }
            }
            else if (!hitLocalPlayer && p != null)
            {
                Vector2 targetChest = (Vector2)p.transform.position + Vector2.up * 0.55f;
                if (Vector2.Distance(waveObj.transform.position, targetChest) <= 1.45f)
                {
                    hitLocalPlayer = true;
                    if (pHealth != null && !pHealth.IsDead)
                    {
                        pHealth.TakeDamage(80);
                        DamagePopup.Create(targetChest, 80, DamageType.Magic, isCritical: true);
                        SoundManager.EnsureInstance()?.PlayHitImpact();
                    }
                }
            }

            yield return null;
        }
        if (waveObj != null) Destroy(waveObj);
    }

    private IEnumerator SpawnRemoteWaterBallRoutine(Vector2 dir, Vector2 startPos, float speed, float scale, int damage = 35)
    {
        var startFrames = AbyssalMechSkill.GetWaterBallStartupFrames();
        var impactFrames = AbyssalMechSkill.GetWaterBallImpactFrames();

        GameObject ball = new GameObject("Remote_AbyssalWaterBall");
        ball.transform.position = startPos + dir * 0.75f;
        float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        ball.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        ball.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = ball.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5500;
        sr.flipX = false;
        sr.flipY = false;

        bool hasFrames = startFrames != null && startFrames.Length > 0;
        if (hasFrames) sr.sprite = startFrames[0];
        else sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();

        float maxRange = 9.5f;
        float traveled = 0f;
        float animTick = 0f;
        int fIdx = 0;
        const float frameInterval = 1f / 24f;
        bool hitPlayer = false;

        Transform playerTrans = null;
        PlayerHealth playerHealth = null;
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            playerTrans = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        while (traveled < maxRange && ball != null && !hitPlayer)
        {
            float dt = Time.deltaTime;
            float step = speed * dt;
            ball.transform.position += (Vector3)(dir * step);
            traveled += step;

            animTick += dt;
            if (hasFrames && animTick >= frameInterval)
            {
                animTick = 0f;
                fIdx++;
                int idx = fIdx < 5 ? fIdx : 5 + ((fIdx - 5) % Mathf.Max(1, startFrames.Length - 5));
                sr.sprite = startFrames[Mathf.Clamp(idx, 0, startFrames.Length - 1)];
            }

            if (PvPMatchManager.IsCoopModeActive)
            {
                Collider2D[] enemyHits = Physics2D.OverlapCircleAll(ball.transform.position, 0.85f * scale, 1 << LayerMask.NameToLayer("Enemy"));
                if (enemyHits != null && enemyHits.Length > 0)
                {
                    hitPlayer = true;
                    foreach (var h in enemyHits)
                    {
                        if (h != null)
                        {
                            var d = h.GetComponent<IDamageable>();
                            if (d != null)
                            {
                                d.TakeDamage(damage);
                                DamagePopup.Create(h.transform.position, damage, DamageType.Magic, isCritical: false);
                            }
                        }
                    }
                }
            }
            else if (playerTrans != null)
            {
                Vector2 targetChest = (Vector2)playerTrans.position + Vector2.up * 0.55f;
                if (Vector2.Distance(ball.transform.position, targetChest) <= 0.85f * scale)
                {
                    hitPlayer = true;
                    if (playerHealth != null && !playerHealth.IsDead)
                    {
                        playerHealth.TakeDamage(damage);
                        DamagePopup.Create(targetChest, damage, DamageType.Magic, isCritical: false);
                    }
                }
            }

            yield return null;
        }

        if (ball != null)
        {
            if (impactFrames != null && impactFrames.Length > 0)
            {
                SoundManager.EnsureInstance()?.PlayHitImpact();
                ball.transform.rotation = Quaternion.identity;
                ball.transform.localScale = new Vector3(scale * 1.35f, scale * 1.35f, 1f);
                for (int i = 0; i < impactFrames.Length && ball != null; i++)
                {
                    sr.sprite = impactFrames[i];
                    yield return new WaitForSeconds(1f / 30f);
                }
            }
            if (ball != null) Destroy(ball);
        }
    }

    private IEnumerator SpawnRemoteSwordWaveRoutine(Vector2 dir, Vector2 startPos, float speed, float scale, int damage = 35, Color waveColor = default)
    {
        GameObject wave = new GameObject("Remote_SwordWave");
        wave.transform.position = startPos + dir * 0.75f;
        float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        wave.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        wave.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = wave.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5500;
        sr.color = (waveColor == default) ? new Color(0.4f, 0.85f, 1f, 1f) : waveColor;
        sr.flipX = false;
        sr.flipY = false;

        var frames = Resources.LoadAll<Sprite>("Player3/Sprites/Twin_sword/twin1");
        if (frames == null || frames.Length == 0) frames = Resources.LoadAll<Sprite>("Player3/Sprites/Twin_sword");
        bool hasFrames = frames != null && frames.Length > 0;
        if (hasFrames) sr.sprite = frames[0];
        else sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();

        float maxRange = 9.5f;
        float traveled = 0f;
        float animTick = 0f;
        int fIdx = 0;
        bool hitPlayer = false;

        Transform playerTrans = null;
        PlayerHealth playerHealth = null;
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            playerTrans = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        while (traveled < maxRange && wave != null && !hitPlayer)
        {
            float dt = Time.deltaTime;
            float step = speed * dt;
            wave.transform.position += (Vector3)(dir * step);
            traveled += step;

            animTick += dt;
            if (hasFrames && animTick >= 0.05f)
            {
                animTick = 0f;
                fIdx = (fIdx + 1) % frames.Length;
                sr.sprite = frames[fIdx];
            }

            if (PvPMatchManager.IsCoopModeActive)
            {
                Collider2D[] enemyHits = Physics2D.OverlapCircleAll(wave.transform.position, 0.85f * scale, 1 << LayerMask.NameToLayer("Enemy"));
                if (enemyHits != null && enemyHits.Length > 0)
                {
                    hitPlayer = true;
                    foreach (var h in enemyHits)
                    {
                        if (h != null)
                        {
                            var d = h.GetComponent<IDamageable>();
                            if (d != null)
                            {
                                d.TakeDamage(damage);
                                DamagePopup.Create(h.transform.position, damage, DamageType.Physical, isCritical: false);
                            }
                        }
                    }
                }
            }
            else if (playerTrans != null)
            {
                Vector2 targetChest = (Vector2)playerTrans.position + Vector2.up * 0.55f;
                if (Vector2.Distance(wave.transform.position, targetChest) <= 0.85f * scale)
                {
                    hitPlayer = true;
                    if (playerHealth != null && !playerHealth.IsDead)
                    {
                        playerHealth.TakeDamage(damage);
                        DamagePopup.Create(targetChest, damage, DamageType.Physical, isCritical: false);
                    }
                }
            }

            yield return null;
        }

        if (wave != null) Destroy(wave);
    }

    private IEnumerator SpawnRemoteCelestialOrbRoutine(Vector2 dir, Vector2 startPos, float speed, float scale, int damage = 35)
    {
        GameObject orb = new GameObject("Remote_CelestialOrb");
        orb.transform.position = startPos + dir * 0.75f;
        float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        orb.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        orb.transform.localScale = new Vector3(scale * 1.5f, scale * 1.5f, 1f);

        var sr = orb.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5500;
        sr.flipX = false;
        sr.flipY = false;

        var frames = CelestialMageSkill.GetNovaBurstFrames();
        var hitFrames = CelestialMageSkill.GetAstralAegisFrames();
        bool hasFrames = frames != null && frames.Length > 0;
        if (hasFrames) sr.sprite = frames[0];
        else
        {
            sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
            sr.color = new Color(0.85f, 0.45f, 1f, 0.95f);
        }

        float maxRange = 9.5f;
        float traveled = 0f;
        float animTick = 0f;
        int fIdx = 0;
        bool hitPlayer = false;

        Transform playerTrans = null;
        PlayerHealth playerHealth = null;
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            playerTrans = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        while (traveled < maxRange && orb != null && !hitPlayer)
        {
            float dt = Time.deltaTime;
            float step = speed * dt;
            orb.transform.position += (Vector3)(dir * step);
            traveled += step;

            animTick += dt;
            if (hasFrames && animTick >= 0.04f)
            {
                animTick = 0f;
                fIdx = (fIdx + 1) % frames.Length;
                sr.sprite = frames[fIdx];
            }

            if (PvPMatchManager.IsCoopModeActive)
            {
                Collider2D[] enemyHits = Physics2D.OverlapCircleAll(orb.transform.position, 0.8f * scale, 1 << LayerMask.NameToLayer("Enemy"));
                if (enemyHits != null && enemyHits.Length > 0)
                {
                    hitPlayer = true;
                    foreach (var h in enemyHits)
                    {
                        if (h != null)
                        {
                            var d = h.GetComponent<IDamageable>();
                            if (d != null)
                            {
                                d.TakeDamage(damage);
                                DamagePopup.Create(h.transform.position, damage, DamageType.Magic, isCritical: false);
                            }
                        }
                    }
                }
            }
            else if (playerTrans != null)
            {
                Vector2 targetChest = (Vector2)playerTrans.position + Vector2.up * 0.55f;
                if (Vector2.Distance(orb.transform.position, targetChest) <= 0.8f * scale)
                {
                    hitPlayer = true;
                    if (playerHealth != null && !playerHealth.IsDead)
                    {
                        playerHealth.TakeDamage(damage);
                        DamagePopup.Create(targetChest, damage, DamageType.Magic, isCritical: false);
                    }
                }
            }

            yield return null;
        }

        if (orb != null)
        {
            if (hitFrames != null && hitFrames.Length > 0)
            {
                SoundManager.EnsureInstance()?.PlayHitImpact();
                orb.transform.rotation = Quaternion.identity;
                orb.transform.localScale = new Vector3(scale * 1.8f, scale * 1.8f, 1f);
                for (int i = 0; i < Mathf.Min(12, hitFrames.Length) && orb != null; i++)
                {
                    sr.sprite = hitFrames[i];
                    yield return new WaitForSeconds(1f / 30f);
                }
            }
            if (orb != null) Destroy(orb);
        }
    }

    private IEnumerator SpawnVisualRingPulseRoutine(Vector2 center, float maxRadius, Color col, float duration)
    {
        GameObject ring = new GameObject("Remote_PvP_Pulse");
        ring.transform.position = center;
        var sr = ring.AddComponent<SpriteRenderer>();
        sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sr.color = col;
        sr.sortingOrder = 4900;

        float t = 0f;
        while (t < duration && ring != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float sc = Mathf.Lerp(0.4f, maxRadius, p);
            ring.transform.localScale = new Vector3(sc, sc, 1f);
            Color c = col;
            c.a = col.a * (1f - p);
            sr.color = c;
            yield return null;
        }
        if (ring != null) Destroy(ring);
    }

    // =========================================================================
    // VIRTUAL INPUT & EXECUTION FOR HERO AI BRAIN
    // =========================================================================
    public void SetBotMovement(Vector2 moveVel, Vector2 faceDir)
    {
        if (_sr != null && faceDir.sqrMagnitude > 0.001f)
        {
            _sr.flipX = (faceDir.x < 0f);
        }

        // Nếu đang trong thời gian tung chiêu/vung đòn: Giữ vững vị trí cắm trụ và cấm chuyển sang hoạt ảnh Run/Move!
        if (_botAttackLockTimer > 0f)
        {
            _botAttackLockTimer -= Time.deltaTime;
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                Vector2 aimDir = (faceDir.sqrMagnitude > 0.001f) ? faceDir.normalized : Vector2.left;
                _animator.SetFloat(MoveXHash, aimDir.x);
                _animator.SetFloat(MoveYHash, aimDir.y);
                _animator.SetFloat(SpeedHash, 0f);
            }
            return;
        }

        Vector2 myPos = transform.position;
        Vector2 nextPos = ClampPositionIfPvP(myPos + moveVel * Time.deltaTime);
        transform.position = new Vector3(nextPos.x, nextPos.y, 0f);

        // Cập nhật hoạt ảnh cho Bot
        if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
        {
            Vector2 animDir = (faceDir.sqrMagnitude > 0.001f) ? faceDir.normalized : Vector2.left;
            _animator.SetFloat(MoveXHash, animDir.x);
            _animator.SetFloat(MoveYHash, animDir.y);
            _animator.SetFloat(SpeedHash, moveVel.magnitude);
        }
        else if (_botActionAnimRoutine == null)
        {
            bool isMoving = moveVel.sqrMagnitude > 0.05f;
            Sprite[] animFrames = null;
            float frameInterval = 0.10f;

            if (OpponentHeroId == CharacterManager.CHAR_ABYSSAL_MECH)
            {
                animFrames = isMoving ? AbyssalMechSkill.GetWalkFrames() : AbyssalMechSkill.GetIdleFrames();
                frameInterval = isMoving ? 0.10f : 0.12f;
            }
            else if (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE)
            {
                animFrames = isMoving ? CelestialMageSkill.GetWalkFrames() : CelestialMageSkill.GetIdleFrames();
                frameInterval = isMoving ? 0.09f : 0.12f;
            }
            else if (OpponentHeroId == CharacterManager.CHAR_DUNE_ASSASSIN)
            {
                animFrames = isMoving ? DuneAssassinSkill.GetRunFrames() : DuneAssassinSkill.GetIdleFrames();
                frameInterval = isMoving ? 0.08f : 0.12f;
            }

            if (animFrames != null && animFrames.Length > 0 && _sr != null)
            {
                _botAnimTimer += Time.deltaTime;
                if (_botAnimTimer >= frameInterval)
                {
                    _botAnimTimer = 0f;
                    _botAnimFrame = (_botAnimFrame + 1) % animFrames.Length;
                    _sr.sprite = animFrames[_botAnimFrame];
                }
            }
        }
    }
}

