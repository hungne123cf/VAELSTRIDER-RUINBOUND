using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BỘ NÃO CHIẾN ĐẤU AI CHUYÊN SÂU (HeroAIBrain) - Huấn luyện lại toàn diện AI Đấu Tập 1v1 cho cả 7 Tướng
/// Tuân thủ nghiêm ngặt chuẩn đối kháng:
/// - Chủ động tìm và truy đuổi Người Chơi (Active Player Hunting & Gap-closing).
/// - Khóa triệt để hoạt ảnh Run khi đang ra đòn (Lock-out run animation during attack).
/// - Sensory Layer: Nhận thức cự ly, trạng thái ra đòn/lướt/thủ của Người Chơi, biên giới võ đài.
/// - Decision Layer: 4 Profile chiến thuật đặc trưng (Zoner, Rushdown, Footsies, AllRounder).
/// - Virtual Input Layer: Điều khiển bước chạy, nhấp nhả (footwork), né đòn (fair evasion), combo 3 thức.
/// - Minh bạch sát thương 100%: Tuyệt đối không gây sát thương ảo khi đứng im! Mọi sát thương đều từ
///   hoạt ảnh vung đòn thực tế (wind-up delay 0.18s-0.26s) hoặc va chạm đạn bay thực tế.
/// </summary>
public class HeroAIBrain : MonoBehaviour
{
    public enum HeroArchetype
    {
        AllRounder, // Vaelstrider (Player 1): Cân bằng, giữ nhịp đối kháng chuẩn mực
        Footsies,   // Gale Ronin (Player 2), Azure Blade (Player 3): Nhấp nhả 2.2m, bắt đòn hụt (whiff punish)
        Rushdown,   // Titan Berserker (Player 4), Dune Assassin (Player 5): Áp sát 1.4m, ép góc, combo bão táp
        Zoner       // Celestial Mage (Player 6), Abyssal Mech (Player 7): Thả diều 3.5m - 4.5m, xả đạn tầm xa
    }

    public enum TacticalState
    {
        Approach,      // Chủ động truy đuổi và áp sát Người Chơi (Trạng thái mặc định)
        Attacking,     // Đang tung đòn tấn công (chân cắm trụ vững, cấm hoạt ảnh Run)
        Retreat,       // Lùi bước giữ khoảng cách ngắn (Kite/Spacing)
        NeutralStance  // Đứng thế thủ ngắn giữa các nhịp combo
    }

    [Header("--- AI IDENTITY & PROFILE ---")]
    public HeroArchetype Archetype = HeroArchetype.AllRounder;
    public TacticalState CurrentState = TacticalState.Approach;

    private PvPRemoteOpponent _opponent;
    private PlayerController _playerController;
    private PlayerHealth _playerHealth;
    private PlayerCombat _playerCombat;

    // Cooldown timers
    private float _attackCooldownTimer = 0.2f;
    private float _skillCooldownTimer = 4.0f;
    private float _dashCooldownTimer = 2.5f;
    private float _stateTimer = 0.3f;

    // Combo quản lý
    private int _comboStep = 0;
    private float _lastAttackTime = -10f;

    // Chỉ số động thích ứng theo chất tướng
    private float _preferredMinRange = 1.6f;
    private float _preferredMaxRange = 2.2f;
    private float _moveSpeed = 4.3f;
    private float _baseAttackCooldown = 0.75f;
    private float _dashCooldown = 3.2f;

    private void Awake()
    {
        _opponent = GetComponent<PvPRemoteOpponent>();
    }

    private void Start()
    {
        ConfigureHeroProfile();
    }

    /// <summary>
    /// Cấu hình bộ chỉ số và phong cách chiến đấu dựa trên HeroId của đối thủ
    /// </summary>
    public void ConfigureHeroProfile()
    {
        if (_opponent == null) _opponent = GetComponent<PvPRemoteOpponent>();
        string heroId = _opponent != null ? _opponent.OpponentHeroId : CharacterManager.CHAR_VAELSTRIDER;
        string normHero = CharacterManager.NormalizeHeroId(heroId);

        if (normHero == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            Archetype = HeroArchetype.Zoner;
            _preferredMinRange = 3.4f;
            _preferredMaxRange = 4.8f;
            _moveSpeed = 4.0f;
            _baseAttackCooldown = 0.90f;
            _dashCooldown = 3.8f;
        }
        else if (normHero == CharacterManager.CHAR_ABYSSAL_MECH)
        {
            Archetype = HeroArchetype.Zoner;
            _preferredMinRange = 3.2f;
            _preferredMaxRange = 4.5f;
            _moveSpeed = 3.9f;
            _baseAttackCooldown = 0.95f;
            _dashCooldown = 4.0f;
        }
        else if (normHero == CharacterManager.CHAR_TITAN_BERSERKER)
        {
            Archetype = HeroArchetype.Rushdown;
            _preferredMinRange = 1.5f;
            _preferredMaxRange = 2.25f;
            _moveSpeed = 4.0f;
            _baseAttackCooldown = 0.65f;
            _dashCooldown = 3.5f;
        }
        else if (normHero == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            Archetype = HeroArchetype.Rushdown;
            _preferredMinRange = 1.3f;
            _preferredMaxRange = 1.85f;
            _moveSpeed = 4.9f;
            _baseAttackCooldown = 0.55f;
            _dashCooldown = 2.2f;
        }
        else if (normHero == CharacterManager.CHAR_AZURE_BLADE)
        {
            Archetype = HeroArchetype.Footsies;
            _preferredMinRange = 1.9f;
            _preferredMaxRange = 2.6f;
            _moveSpeed = 4.4f;
            _baseAttackCooldown = 0.70f;
            _dashCooldown = 3.2f;
        }
        else if (normHero == CharacterManager.CHAR_GALE_RONIN)
        {
            Archetype = HeroArchetype.Footsies;
            _preferredMinRange = 1.8f;
            _preferredMaxRange = 2.35f;
            _moveSpeed = 4.5f;
            _baseAttackCooldown = 0.65f;
            _dashCooldown = 3.0f;
        }
        else // Vaelstrider
        {
            Archetype = HeroArchetype.AllRounder;
            _preferredMinRange = 1.7f;
            _preferredMaxRange = 2.2f;
            _moveSpeed = 4.3f;
            _baseAttackCooldown = 0.75f;
            _dashCooldown = 3.2f;
        }
    }

    private void Update()
    {
        if (PvPMatchManager.Instance == null || _opponent == null || _opponent.CurrentHealth <= 0)
            return;

        // 1. Trong lúc đếm ngược 3 - 2 - 1 giữ tư thế thế thủ trên bệ xuất chiến
        if (!PvPMatchManager.Instance.IsCombatActive)
        {
            UpdateCountdownIdle();
            return;
        }

        // 2. Thu thập dữ liệu cảm biến (Sensory Layer)
        if (!RefreshSensoryData())
            return;

        // Giảm hồi chiêu
        float dt = Time.deltaTime;
        _attackCooldownTimer -= dt;
        _skillCooldownTimer -= dt;
        _dashCooldownTimer -= dt;
        _stateTimer -= dt;

        Vector2 myPos = transform.position;
        Vector2 targetPos = _playerController.transform.position;
        Vector2 toTarget = targetPos - myPos;
        float dist = toTarget.magnitude;
        Vector2 dirToTarget = dist > 0.01f ? toTarget / dist : Vector2.left;

        // 3. Phản xạ né tránh (Fair Evasion / Kite Dash / Gap-closer)
        CheckDefensiveReactions(dist, dirToTarget);

        // 4. Thực thi Máy Trạng Thái Quyết Định (Decision Layer)
        ExecuteTacticalState(dist, dirToTarget);
    }

    private void UpdateCountdownIdle()
    {
        _opponent.SetBotMovement(Vector2.zero, Vector2.left);
    }

    private bool RefreshSensoryData()
    {
        if (_playerController == null)
            _playerController = FindAnyObjectByType<PlayerController>();
        if (_playerController == null) return false;

        if (_playerHealth == null)
            _playerHealth = _playerController.GetComponent<PlayerHealth>();
        if (_playerHealth != null && _playerHealth.IsDead) return false;

        if (_playerCombat == null)
            _playerCombat = _playerController.GetComponent<PlayerCombat>();

        return true;
    }

    /// <summary>
    /// Phản xạ phòng thủ có trí tuệ: Tướng đánh xa lướt lùi khi bị ép sát, Tướng cận chiến lướt áp sát nhanh
    /// </summary>
    private void CheckDefensiveReactions(float dist, Vector2 dirToTarget)
    {
        if (_dashCooldownTimer > 0f || CurrentState == TacticalState.Attacking)
            return;

        // TH 1: Zoner bị áp sát quá gần (< 1.9m) -> Lướt giật lùi (Kite Dash)
        if (Archetype == HeroArchetype.Zoner && dist < 1.9f)
        {
            _dashCooldownTimer = _dashCooldown + Random.Range(0.2f, 0.6f);
            ExecuteDash(-dirToTarget);
            CurrentState = TacticalState.Approach;
            _stateTimer = 0.25f;
            return;
        }

        // TH 2: Rushdown ở xa (> 3.2m) -> Lướt áp sát Người Chơi bạo liệt
        if (Archetype == HeroArchetype.Rushdown && dist > 3.2f && dist < 7.5f)
        {
            _dashCooldownTimer = _dashCooldown + Random.Range(0.2f, 0.6f);
            ExecuteDash(dirToTarget);
            CurrentState = TacticalState.Approach;
            _stateTimer = 0.20f;
            return;
        }

        // TH 3: Người chơi đang lướt Dash tới hoặc đang gồng chiêu cận kề -> Tướng Footsies né nhẹ sang bên
        bool playerIsAggro = (_playerController != null && _playerController.IsDashing()) ||
                             (_playerCombat != null && _playerCombat.IsAttackHolding && dist < 2.0f);

        if (playerIsAggro && dist < 2.2f && Random.value < 0.60f)
        {
            _dashCooldownTimer = _dashCooldown + Random.Range(0.3f, 0.8f);
            Vector2 flankDir = new Vector2(-dirToTarget.y, dirToTarget.x) * (Random.value < 0.5f ? 1f : -1f);
            Vector2 evadeDir = (flankDir - dirToTarget * 0.3f).normalized;
            ExecuteDash(evadeDir);
            CurrentState = TacticalState.Approach;
            _stateTimer = 0.20f;
            return;
        }
    }

    private void ExecuteDash(Vector2 dashDir)
    {
        Vector2 myPos = transform.position;
        _opponent.ExecuteBotAction("DASH", dashDir, myPos);
    }

    /// <summary>
    /// Xử lý di chuyển chủ động tìm người, cắm trụ ra đòn và tung chuỗi combo
    /// </summary>
    private void ExecuteTacticalState(float dist, Vector2 dirToTarget)
    {
        Vector2 moveVel = Vector2.zero;

        // Quy tắc cốt lõi: Khi ở ngoài tầm đánh, bot LUÔN CHỦ ĐỘNG TIẾN TỚI TÌM NGƯỜI CHƠI
        if (CurrentState != TacticalState.Attacking && dist > _preferredMaxRange)
        {
            CurrentState = TacticalState.Approach;
        }

        switch (CurrentState)
        {
            case TacticalState.Attacking:
                // Trong khi đánh: Cấm di chuyển, cắm trụ tuyệt đối để hoạt ảnh vung vũ khí thể hiện trọn vẹn
                moveVel = Vector2.zero;
                _opponent.SetBotMovement(Vector2.zero, dirToTarget);

                if (_stateTimer <= 0f && !_opponent.IsActionAnimationPlaying)
                {
                    // Sau khi ra đòn xong:
                    if (Archetype == HeroArchetype.Rushdown)
                    {
                        // Rushdown tiếp tục ép sát không lùi bước
                        if (dist <= _preferredMaxRange + 0.35f && _attackCooldownTimer <= 0f)
                        {
                            PerformAttack(dist, dirToTarget);
                            return;
                        }
                        CurrentState = TacticalState.Approach;
                        _stateTimer = 0.15f;
                    }
                    else if (Archetype == HeroArchetype.Zoner)
                    {
                        if (dist < 1.8f)
                        {
                            CurrentState = TacticalState.Retreat;
                            _stateTimer = Random.Range(0.30f, 0.55f);
                        }
                        else
                        {
                            CurrentState = TacticalState.Approach;
                            _stateTimer = 0.20f;
                        }
                    }
                    else // Footsies & AllRounder
                    {
                        if (Random.value < 0.30f)
                        {
                            CurrentState = TacticalState.Retreat;
                            _stateTimer = Random.Range(0.15f, 0.25f);
                        }
                        else
                        {
                            CurrentState = TacticalState.Approach;
                            _stateTimer = 0.15f;
                        }
                    }
                }
                return;

            case TacticalState.Approach:
                // CHỦ ĐỘNG TÌM VÀ ÁP SÁT NGƯỜI CHƠI
                moveVel = dirToTarget * _moveSpeed;

                // Khi đã vào tầm đánh:
                if (dist <= _preferredMaxRange)
                {
                    // Ưu tiên 1: Kích hoạt Tuyệt Kỹ (Ulti) khi sẵn sàng
                    if (_skillCooldownTimer <= 0f && dist <= 6.5f)
                    {
                        PerformUlti(dirToTarget);
                        return;
                    }

                    // Ưu tiên 2: Tung đòn tấn công thường / xả đạn ngay lập tức!
                    if (_attackCooldownTimer <= 0f)
                    {
                        PerformAttack(dist, dirToTarget);
                        return;
                    }

                    // Nếu đang chờ hồi chiêu 0.1s - 0.3s: ép sát và dừng bước
                    if (dist <= _preferredMinRange)
                    {
                        moveVel = Vector2.zero;
                    }
                }
                break;

            case TacticalState.Retreat:
                moveVel = -dirToTarget * (_moveSpeed * 1.05f);
                if (dist >= _preferredMinRange + 0.35f || _stateTimer <= 0f)
                {
                    CurrentState = TacticalState.Approach;
                    moveVel = Vector2.zero;
                }
                break;

            case TacticalState.NeutralStance:
                if (dist > _preferredMaxRange)
                {
                    CurrentState = TacticalState.Approach;
                    moveVel = dirToTarget * _moveSpeed;
                }
                else
                {
                    if (_attackCooldownTimer <= 0f)
                    {
                        PerformAttack(dist, dirToTarget);
                        return;
                    }
                    moveVel = Vector2.zero;
                }
                break;
        }

        // Cập nhật vị trí và hoạt ảnh bước chạy cho Bot
        _opponent.SetBotMovement(moveVel, dirToTarget);
    }

    /// <summary>
    /// Kích hoạt chuỗi đòn đánh thường (Combo 1-2-3) với hoạt ảnh và nhịp độ đối kháng thực
    /// </summary>
    private void PerformAttack(float dist, Vector2 dirToTarget)
    {
        CurrentState = TacticalState.Attacking;
        _stateTimer = 0.45f;

        // Dừng bước chạy ngay tức thì
        _opponent.SetBotMovement(Vector2.zero, dirToTarget);

        // Quản lý Combo Step 0 -> 1 -> 2
        if (Time.time - _lastAttackTime < 1.45f)
        {
            _comboStep = (_comboStep + 1) % 3;
        }
        else
        {
            _comboStep = 0;
        }
        _lastAttackTime = Time.time;
        _attackCooldownTimer = _baseAttackCooldown + Random.Range(-0.05f, 0.08f);

        Vector2 myPos = transform.position;
        Vector2 shootOrigin = myPos + Vector2.up * PvPRemoteOpponent.GetHeroChestOffsetY(_opponent.OpponentHeroId);
        Vector2 targetChest = (Vector2)_playerController.transform.position + Vector2.up * 0.55f;
        Vector2 aimDir = (targetChest - shootOrigin).normalized;
        if (aimDir.sqrMagnitude < 0.01f) aimDir = dirToTarget;

        // Gửi lệnh đánh sang PvPRemoteOpponent thực thi hoạt ảnh, VFX, và tính sát thương
        _opponent.ExecuteBotAction("ATK", aimDir, shootOrigin, _comboStep);

        // Với tướng cận chiến: Bắt đầu Coroutine kiểm tra trúng đích sau Windup Delay (0.18s - 0.25s)
        bool isRangedBot = (Archetype == HeroArchetype.Zoner) ||
                            (_opponent.OpponentHeroId == CharacterManager.CHAR_AZURE_BLADE && _comboStep >= 1);

        if (!isRangedBot)
        {
            float windup = 0.20f;
            float hitRange = 1.95f;
            int baseDmg = Random.Range(28, 42) + _comboStep * 6;

            if (_opponent.OpponentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
            {
                windup = 0.24f;
                hitRange = 2.25f;
                baseDmg = Random.Range(34, 48) + _comboStep * 8;
            }
            else if (_opponent.OpponentHeroId == CharacterManager.CHAR_DUNE_ASSASSIN)
            {
                windup = 0.15f;
                hitRange = 1.55f;
                baseDmg = Random.Range(24, 36) + _comboStep * 5;
            }
            else if (_opponent.OpponentHeroId == CharacterManager.CHAR_GALE_RONIN)
            {
                windup = 0.17f;
                hitRange = 2.05f;
                baseDmg = Random.Range(28, 40) + _comboStep * 6;
            }

            StartCoroutine(_opponent.PerformMeleeHitRoutine(windup, hitRange, baseDmg, isUlti: false, aimDir));
        }
    }

    /// <summary>
    /// Kích hoạt Tuyệt Kỹ (Ultimate) với hoạt ảnh hoành tráng và âm thanh uy lực
    /// </summary>
    private void PerformUlti(Vector2 dirToTarget)
    {
        CurrentState = TacticalState.Attacking;
        _stateTimer = 0.65f;
        _skillCooldownTimer = Random.Range(7.5f, 10.5f);

        _opponent.SetBotMovement(Vector2.zero, dirToTarget);

        Vector2 myPos = transform.position;
        Vector2 shootOrigin = myPos + Vector2.up * PvPRemoteOpponent.GetHeroChestOffsetY(_opponent.OpponentHeroId);
        Vector2 targetChest = (Vector2)_playerController.transform.position + Vector2.up * 0.55f;
        Vector2 aimDir = (targetChest - shootOrigin).normalized;
        if (aimDir.sqrMagnitude < 0.01f) aimDir = dirToTarget;

        _opponent.ExecuteBotAction("ULTI", aimDir, shootOrigin, 0);

        bool isRangedBot = (Archetype == HeroArchetype.Zoner) ||
                            (_opponent.OpponentHeroId == CharacterManager.CHAR_AZURE_BLADE);

        if (!isRangedBot)
        {
            int ultDmg = Random.Range(60, 92);
            float windup = 0.26f;
            float hitRange = 2.45f;
            if (_opponent.OpponentHeroId == CharacterManager.CHAR_TITAN_BERSERKER)
            {
                hitRange = 2.75f;
            }
            StartCoroutine(_opponent.PerformMeleeHitRoutine(windup, hitRange, ultDmg, isUlti: true, aimDir));
        }
    }
}
