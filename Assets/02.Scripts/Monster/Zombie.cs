using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

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

[RequireComponent(typeof(CharacterController))]
public class Zombie : MonoBehaviour, IDamageable, IPoolable
{
    [Header("스탯")][SerializeField] private float _detectDistance = 8; // 플레이어를 발견하는 거리
    [SerializeField] private float _combackDistance = 15; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
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

    private CharacterController _controller;
    private Vector3 _startPosition;
    private Transform _player;
    private IDamageable _playerDamageable;
    private readonly Dictionary<ZombieState, ZombieStateBase> _states = new Dictionary<ZombieState, ZombieStateBase>();
    private ZombieStateBase _currentState;
    private bool _isSpawned;
    private Transform[] _activePatrolPoints;
    private ZombieIdleMode _activeIdleMode;

    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public bool IsDead => _state == ZombieState.Death;

    public event Action<float, float> OnHealthChanged;
    public event Action<Zombie> OnDied;
    public event Action<Zombie> OnDespawned;

    internal float AttackInterval => _attackInterval;
    internal float WaypointWaitTime => _waypointWaitTime;
    internal float RoamTravelTime => _roamTravelTime;
    internal float HitDuration => _hitDuration;
    internal float DeathDelay => _deathDelay;
    internal Vector3 HomePosition => _startPosition;
    internal Transform[] PatrolPoints => _activePatrolPoints;

    private ZombieState _state = ZombieState.Idle;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _states.Add(ZombieState.Idle, new ZombieIdleState(this));
        _states.Add(ZombieState.Patrol, new ZombiePatrolState(this));
        _states.Add(ZombieState.Roam, new ZombieRoamState(this));
        _states.Add(ZombieState.Trace, new ZombieTraceState(this));
        _states.Add(ZombieState.Comeback, new ZombieComebackState(this));
        _states.Add(ZombieState.Attack, new ZombieAttackState(this));
        _states.Add(ZombieState.Hit, new ZombieHitState(this));
        _states.Add(ZombieState.Death, new ZombieDeathState(this));
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
            return;
        }

        _currentState?.Tick(Time.deltaTime);
    }

    public void OnSpawn()
    {
        // Lean Pool은 활성화 후 OnSpawn을 호출하므로 중복 초기화를 막는다.
        InitializeLife();
    }

    private void InitializeLife()
    {
        if (_isSpawned)
        {
            return;
        }

        _isSpawned = true;
        _health = _maxHealth;
        _startPosition = transform.position;
        _activePatrolPoints = _patrolPoints;
        _activeIdleMode = _idleMode;
        _controller.enabled = true;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        _player = player != null ? player.transform : null;
        _playerDamageable = player != null ? player.GetComponent<IDamageable>() : null;
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
        _controller.enabled = false;
        _player = null;
        _playerDamageable = null;
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
    }

    internal void GoToDefaultState()
    {
        switch (_activeIdleMode)
        {
            case ZombieIdleMode.Patrol:
                ChangeState(_activePatrolPoints != null && _activePatrolPoints.Length > 0
                    ? ZombieState.Patrol : ZombieState.Roam);
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
    internal bool IsPlayerInAttackRange => _player != null && Vector3.Distance(transform.position, _player.position) <= _attackRange;
    internal bool IsOutsideHomeRange => PlanarDistance(transform.position, _startPosition) > _combackDistance;

    internal bool MoveTowards(Vector3 destination)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= _arrivalDistance * _arrivalDistance)
        {
            StandStill();
            return true;
        }

        direction.Normalize();
        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
        return false;
    }

    internal void StandStill()
    {
        if (_controller.enabled)
        {
            _controller.SimpleMove(Vector3.zero);
        }
    }

    internal Vector3 ChooseRoamDestination()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _roamRadius;
        return _startPosition + new Vector3(offset.x, 0f, offset.y);
    }

    internal void AttackPlayer()
    {
        if (IsPlayerInAttackRange)
        {
            _playerDamageable?.TakeDamage(_attackDamage);
        }
    }

    internal void DisableController() => _controller.enabled = false;

    internal void ReturnToPool() => LeanPool.Despawn(gameObject);

    private static float PlanarDistance(Vector3 from, Vector3 to)
    {
        from.y = to.y;
        return Vector3.Distance(from, to);
    }
}
