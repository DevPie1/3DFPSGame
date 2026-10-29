using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using UnityEngine.AI;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"


// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기: 제자리에서 플레이어를 기다린다.
    Trace, // 추격: 플레이어를 쫓아간다.
    Comeback, // 복귀: 처음 자리로 돌아간다.
    Attack, // 공격: 일정 시간마다 플레이어를 때린다.
    Hit, // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death, // 죽음: 쓰러지고 잠시 뒤 사라진다.
    Patrol, // 지정 지점 순찰
    Roam, // 생성 위치 주변 배회
}

public enum ZombieIdleMode
{
    Idle,
    Patrol,
    Roam
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public class Zombie : MonoBehaviour, IDamageable, IPoolable
{
    [Header("스탯")][SerializeField] private float _detectDistance = 8; // 플레이어를 발견하는 거리
    [SerializeField] private float _combackDistance = 40; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private bool _keepChasingPlayer = true;
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _attackDamage = 10f; // 공격 한 번의 대미지
    [SerializeField] private float _moveSpeed = 5; // 이동속도
    [SerializeField] private float _maxHealth = 30;
    [SerializeField] private float _health;


    [Header("순찰 / 배회")][SerializeField] private ZombieIdleMode _idleMode = ZombieIdleMode.Roam;
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField, Min(0.1f)] private float _arrivalDistance = 0.7f;
    [SerializeField, Min(0.1f)] private float _roamRadius = 5f;
    [SerializeField, Min(0f)] private float _waypointWaitTime = 1f;
    [SerializeField, Min(0.1f)] private float _roamTravelTime = 5f;
    [SerializeField, Min(0f)] private float _hitDuration = 0.3f;
    [SerializeField, Min(0f)] private float _deathDelay = 0.5f;

    private Vector3 _startPosition;
    private Transform _player;
    private IDamageable _playerDamageable;
    private Collider _playerCollider;
    private Vector3 _lastDestination;
    private float _nextPathTime;
    private bool _hasDestination;
    private readonly Dictionary<ZombieState, ZombieStateBase> _states = new Dictionary<ZombieState, ZombieStateBase>();
    private ZombieStateBase _currentState;
    private bool _isSpawned;
    private Transform[] _activePatrolPoints;
    private ZombieIdleMode _activeIdleMode;
    [SerializeField, HideInInspector] private ZombieSpawner _spawner;

    internal ZombieSpawner Spawner { get => _spawner; set => _spawner = value; }

    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public bool IsDead => _state == ZombieState.Death;

    public event Action<float, float> OnHealthChanged;
    public event Action<Zombie> OnDied;
    public event Action<Zombie> OnDespawned;
    public event Action<ZombieState> OnStateChanged;
    public event Action OnAttackPerformed;

    public ZombieState State => _state;
    public float AgentVelocity => _agent != null ? _agent.velocity.magnitude : 0f;
    internal bool IsJumping => _jump != null && _jump.IsJumping;

    internal float AttackInterval => _attackInterval;
    internal float WaypointWaitTime => _waypointWaitTime;
    internal float RoamTravelTime => _roamTravelTime;
    internal float HitDuration => _hitDuration;
    internal float DeathDelay => _deathDelay;
    internal Vector3 HomePosition => _startPosition;
    internal Transform[] PatrolPoints => _activePatrolPoints;
    internal Vector3 FeetPosition => transform.position - Vector3.up * _agent.baseOffset;
    internal NavMeshQueryFilter NavigationFilter => new NavMeshQueryFilter
    {
        agentTypeID = _agent.agentTypeID,
        areaMask = _agent.areaMask
    };

    private ZombieState _state = ZombieState.Idle;
    private NavMeshAgent _agent;
    private ZombieJump _jump;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        // Play 중 스크립트를 다시 컴파일하면 Dictionary는 초기화되지만 Awake는 다시 호출되지 않는다.
        if (_states.Count == 0)
        {
            _states.Add(ZombieState.Idle, new ZombieIdleState(this));
            _states.Add(ZombieState.Patrol, new ZombiePatrolState(this));
            _states.Add(ZombieState.Roam, new ZombieRoamState(this));
            _states.Add(ZombieState.Trace, new ZombieTraceState(this));
            _states.Add(ZombieState.Comeback, new ZombieComebackState(this));
            _states.Add(ZombieState.Attack, new ZombieAttackState(this));
            _states.Add(ZombieState.Hit, new ZombieHitState(this));
            _states.Add(ZombieState.Death, new ZombieDeathState(this));
        }

        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError("[Zombie] NavMeshAgent가 없습니다.", this);
        }

        // 프리팹에 점프 컴포넌트가 없어도 동작하도록 보장한다.
        _jump = GetComponent<ZombieJump>();
        if (_jump == null)
        {
            _jump = gameObject.AddComponent<ZombieJump>();
        }
    }

    private void OnEnable()
    {
        InitializeLife();
    }

    private void Update()
    {
        // 사망 후 풀 반환 타이머는 게임오버 상태에서도 진행한다.
        if (!_isSpawned || (!IsDead && GameManager.Instance != null && GameManager.Instance.State != GameState.Playing))
        {
            StandStill();
            return;
        }

        if (IsJumping && !IsDead) return;
        _currentState?.Tick(Time.deltaTime);
    }

    public void OnSpawn()
    {
        // Lean Pool은 활성화 후 OnSpawn을 호출하므로 중복 초기화를 막는다.
        InitializeLife();
    }

    private void InitializeLife()
    {
        EnsureInitialized();
        if (_isSpawned)
        {
            return;
        }

        _isSpawned = true;
        _health = _maxHealth;
        _activePatrolPoints = _patrolPoints;
        _activeIdleMode = _idleMode;

        // 생성 위치가 NavMesh 밖이면 가장 가까운 NavMesh 위로 올린다.
        if (NavMesh.SamplePosition(FeetPosition, out NavMeshHit hit, 5f, NavigationFilter))
        {
            _startPosition = hit.position;
            transform.position = hit.position + Vector3.up * _agent.baseOffset;
        }
        else
        {
            _startPosition = FeetPosition;
            Debug.LogWarning("[Zombie] 생성 위치 근처에 NavMesh가 없습니다.", this);
        }

        EnableAgent();
        if (_agent.isOnNavMesh) _agent.Warp(transform.position);
        _hasDestination = false;
        _nextPathTime = 0f;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        _player = player != null ? player.transform : null;
        _playerDamageable = player != null ? player.GetComponent<IDamageable>() : null;
        _playerCollider = player != null ? player.GetComponent<Collider>() : null;
        GoToDefaultState();
        OnHealthChanged?.Invoke(_health, _maxHealth);
    }

    // 생성 지점마다 다른 순찰 경로와 행동을 지정한다.
    public void Configure(ZombieIdleMode idleMode, Transform[] patrolPoints)
    {
        _activeIdleMode = idleMode;
        _activePatrolPoints = patrolPoints;
        GoToDefaultState();
    }

    public void OnDespawn()
    {
        if (!_isSpawned)
        {
            return;
        }

        _isSpawned = false;
        _currentState?.Exit();
        _currentState = null;
        DisableAgent();
        _player = null;
        _playerDamageable = null;
        _playerCollider = null;
        OnDespawned?.Invoke(this);
    }

    private void OnDisable()
    {
        OnDespawn();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || !_isSpawned || IsDead)
        {
            return;
        }

        _health = Mathf.Max(0f, _health - damage);
        ChangeState(_health > 0f ? ZombieState.Hit : ZombieState.Death);
        OnHealthChanged?.Invoke(_health, _maxHealth);
        if (IsDead)
        {
            OnDied?.Invoke(this);
        }
    }

    internal void ChangeState(ZombieState state)
    {
        _currentState?.Exit();
        _state = state;
        _currentState = _states[state];
        _currentState.Enter();
        OnStateChanged?.Invoke(state);
    }

    internal void GoToDefaultState()
    {
        switch (_activeIdleMode)
        {
            case ZombieIdleMode.Patrol:
                ChangeState(_activePatrolPoints != null && _activePatrolPoints.Length > 0
                    ? ZombieState.Patrol
                    : ZombieState.Roam);
                break;
            case ZombieIdleMode.Roam:
                ChangeState(ZombieState.Roam);
                break;
            default:
                ChangeState(ZombieState.Idle);
                break;
        }
    }

    internal bool TryDetectPlayer()
    {
        if (_player != null && Vector3.Distance(transform.position, _player.position) <= _detectDistance)
        {
            ChangeState(ZombieState.Trace);
            return true;
        }

        return false;
    }

    internal bool HasPlayer => _player != null;
    internal Vector3 PlayerPosition => _player != null ? _player.position : _startPosition;
    internal Vector3 PlayerFeetPosition => _playerCollider != null
        ? new Vector3(_player.position.x, _playerCollider.bounds.min.y, _player.position.z)
        : PlayerPosition;

    internal bool TryGetPlayerNavMeshPosition(out Vector3 position)
    {
        // 몸 중심으로 샘플링하면 위/아래층이 뒤바뀔 수 있으므로 발 높이를 사용한다.
        Vector3 feet = PlayerFeetPosition;
        if (HasPlayer && NavMesh.SamplePosition(feet, out NavMeshHit hit, 1.2f, NavigationFilter)
            && Mathf.Abs(hit.position.y - feet.y) <= 0.5f)
        {
            position = hit.position;
            return true;
        }

        // 플레이어가 공중에 있는 동안에는 마지막 지상 목적지를 유지한다.
        position = default;
        return false;
    }

    internal bool IsPlayerInAttackRange =>
        _player != null
        && !IsJumping && _agent.isActiveAndEnabled && _agent.isOnNavMesh && !_agent.isOnOffMeshLink
        && Mathf.Abs(PlayerFeetPosition.y - FeetPosition.y) < 0.6f
        && Vector3.Distance(transform.position, _player.position) <= _attackRange;

    internal bool IsOutsideHomeRange => !_keepChasingPlayer
        && PlanarDistance(transform.position, _startPosition) > _combackDistance;

    internal bool MoveTowards(Vector3 destination)
    {
        if (IsJumping || _agent == null || _agent.enabled == false)
        {
            return false;
        }

        if (!_agent.isOnNavMesh || _agent.isOnOffMeshLink) return false;
        if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, 1.2f, NavigationFilter)) return false;
        destination = destinationHit.position;

        _agent.isStopped = false;
        // 매 프레임 새 경로를 요청해 pathPending 상태에 머물지 않게 한다.
        if (Time.time >= _nextPathTime && (!_hasDestination || !_agent.hasPath
            || (destination - _lastDestination).sqrMagnitude > 0.0625f))
        {
            _nextPathTime = Time.time + 0.2f;
            if (!_agent.SetDestination(destination)) return false;
            _lastDestination = destination;
            _hasDestination = true;
            return false;
        }

        if (_agent.pathPending)
        {
            return false;
        }

        float remainingDistance = _agent.remainingDistance;
        if (float.IsInfinity(remainingDistance))
        {
            return false;
        }

        return _agent.pathStatus == NavMeshPathStatus.PathComplete
            && Vector3.Distance(FeetPosition, destination) <= _arrivalDistance + 0.1f
            && remainingDistance <= _arrivalDistance + 0.05f;
    }

    internal void StandStill()
    {
        if (IsJumping || _agent == null || _agent.enabled == false || _agent.isOnNavMesh == false)
        {
            return;
        }

        if (_agent.isStopped == false)
        {
            _agent.isStopped = true;
            // ResetPath는 링크 위에서 즉시 반대편으로 이동시키므로 여기서는 정지만 한다.
        }
    }

    internal void RefreshPathAfterJump()
    {
        _hasDestination = false;
        _nextPathTime = 0f;
    }

    internal Vector3 ChooseRoamDestination()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _roamRadius;
        Vector3 candidate = _startPosition + new Vector3(offset.x, 0f, offset.y);

        // 배회 목적지도 실제 NavMesh 위의 점으로 보정한다.
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _roamRadius, NavigationFilter))
        {
            return hit.position;
        }

        return _startPosition;
    }

    internal void AttackPlayer()
    {
        if (IsPlayerInAttackRange)
        {
            _playerDamageable?.TakeDamage(_attackDamage);
            OnAttackPerformed?.Invoke();
        }
    }

    private void EnableAgent()
    {
        if (_agent == null)
        {
            return;
        }

        _agent.enabled = true;
        _agent.speed = _moveSpeed;
        _agent.stoppingDistance = _arrivalDistance;
        _agent.acceleration = Mathf.Max(8f, _moveSpeed * 2f);
        _agent.angularSpeed = 360f;
        _agent.autoBraking = true;
        _agent.updateRotation = true;
        _agent.updatePosition = true;
        _agent.autoTraverseOffMeshLink = false;
        if (_agent.isOnNavMesh) _agent.isStopped = false;
    }

    internal void DisableAgent()
    {
        if (_agent == null)
        {
            return;
        }

        _agent.enabled = false;
        _jump?.CancelJump();
        _hasDestination = false;
    }

    internal void ReturnToPool() => LeanPool.Despawn(gameObject);

    private static float PlanarDistance(Vector3 from, Vector3 to)
    {
        from.y = to.y;
        return Vector3.Distance(from, to);
    }
}
