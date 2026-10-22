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
    PATROL, // 순찰: 시작 위치 주변의 두 지점을 오간다.
    Attack, // 공격: 일정 시간마다 플레이어를 때린다.
    Hit, // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death, // 죽음: 쓰러지고 잠시 뒤 사라진다.
}

public class Zombie : MonoBehaviour
{
    [Header("스탯")][SerializeField] private float _detectDistance = 8; // 플레이어를 발견하는 거리
    [SerializeField] private float _combackDistance = 15; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5; // 이동속도

    [Header("순찰")]
    // 나중에 씬에 직접 배치할 두 지점. 비워두면 시작 위치 주변에 자동으로 만든다.
    [SerializeField]
    private Transform _patrolPointA;

    [SerializeField] private Transform _patrolPointB;
    [SerializeField] private float _patrolRadius = 3f; // 자동 생성 시 시작 위치에서 떨어질 거리
    [SerializeField] private float _patrolWaitTime = 1f; // 각 지점에 도착한 뒤 쉬는 시간
    [SerializeField] private float _arriveDistance = 0.4f; // 도착 판정 거리
    [SerializeField] private float _idleDuration = 1.5f; // 순찰을 시작하기 전 대기 시간

    private float _attackTimer = 0f;
    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;

    private Vector3 _autoPatrolA;
    private Vector3 _autoPatrolB;
    private Vector3 _patrolTarget;
    private int _patrolIndex;
    private bool _isPatrolWaiting;
    private float _patrolWaitTimer;
    private float _idleTimer;

    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;

        // 두 순찰 지점을 직접 연결하지 않았다면 시작 위치의 좌우에 자동으로 만든다.
        Vector3 patrolOffset = transform.right * _patrolRadius;
        _autoPatrolA = _startPosition + patrolOffset;
        _autoPatrolB = _startPosition - patrolOffset;
        _patrolIndex = 0;
        _patrolTarget = GetPatrolPoint(_patrolIndex);

        // 게임을 시작하면 플레이어를 찾는다.
        _player = GameObject.FindGameObjectWithTag("Player");
    }


    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;

            case ZombieState.Trace:
                Trace();
                break;

            case ZombieState.Comeback:
                Comeback();
                break;

            case ZombieState.PATROL:
                Patrol();
                break;

            case ZombieState.Attack:
                Attack();
                break;

            case ZombieState.Hit:
                break;

            case ZombieState.Death:
                break;
        }
    }

    // 대기: 잠깐 쉬다가 주변 순찰을 시작한다.
    private void Idle()
    {
        // Todo. Idle 애니메이션 실행

        // 플레이어가 가까워지면 Trace 상태로 전이한다.
        if (CanDetectPlayer())
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }

        _idleTimer += Time.deltaTime;
        if (_idleTimer >= _idleDuration)
        {
            EnterPatrol();
        }
    }

    // 순찰: 시작 위치 주변의 두 지점을 오간다.
    private void Patrol()
    {
        // 순찰 중에도 플레이어를 발견하면 추격한다.
        if (CanDetectPlayer())
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }

        // 지점에 도착한 뒤 잠시 쉰다.
        if (_isPatrolWaiting)
        {
            _controller.SimpleMove(Vector3.zero);
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f)
            {
                _isPatrolWaiting = false;
                _patrolIndex = 1 - _patrolIndex;
                _patrolTarget = GetPatrolPoint(_patrolIndex);
            }

            return;
        }

        Vector3 toTarget = _patrolTarget - transform.position;
        toTarget.y = 0f;

        // 목적지에 도착했으면 잠시 대기한다.
        if (toTarget.sqrMagnitude <= _arriveDistance * _arriveDistance)
        {
            _isPatrolWaiting = true;
            _patrolWaitTimer = _patrolWaitTime;
            _controller.SimpleMove(Vector3.zero);
            return;
        }

        Vector3 direction = toTarget.normalized;
        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 순찰 상태로 들어갈 때 목표 지점을 A로 초기화한다.
    private void EnterPatrol()
    {
        _idleTimer = 0f;
        _isPatrolWaiting = false;
        _patrolWaitTimer = 0f;
        _patrolIndex = 0;
        _patrolTarget = GetPatrolPoint(_patrolIndex);
        _state = ZombieState.PATROL;
        Debug.Log($"{_state} 시작");
    }

    // 연결된 지점이 있으면 그 지점을, 없으면 자동 생성한 지점을 사용한다.
    private Vector3 GetPatrolPoint(int index)
    {
        Transform point = index == 0 ? _patrolPointA : _patrolPointB;
        if (point != null)
        {
            return point.position;
        }

        return index == 0 ? _autoPatrolA : _autoPatrolB;
    }

    private bool CanDetectPlayer()
    {
        if (_player == null)
        {
            return false;
        }

        return Vector3.Distance(transform.position, _player.transform.position) <= _detectDistance;
    }

    // 추격: 플레이어를 쫓아간다.
    private void Trace()
    {
        // Todo. Run 애니메이션 실행

        if (_player == null)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(transform.position, _startPosition) > _combackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }


        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 복귀: 처음 자리로 돌아간다.
    private void Comeback()
    {
        // 처음 자리에 도착하면 Idle
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            Debug.Log($"{_state} -> Idle");
            _idleTimer = 0f;
            _state = ZombieState.Idle;
            return;
        }

        // 처음 자리로 돌아간다
        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 공격: 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        if (_player == null)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // Todo. 공격 애니메이션 실행
            // Todo. 플레이어를 때려!
            Debug.Log("플레이어를 때렸습니다.");
            _attackTimer = 0f;
        }
    }

    // 순찰 지점을 씬 뷰에서 확인할 수 있게 표시한다.
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? _startPosition : transform.position;
        Vector3 offset = transform.right * _patrolRadius;

        Vector3 pointA = _patrolPointA != null
            ? _patrolPointA.position
            : (Application.isPlaying ? _autoPatrolA : origin + offset);

        Vector3 pointB = _patrolPointB != null
            ? _patrolPointB.position
            : (Application.isPlaying ? _autoPatrolB : origin - offset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, 0.2f);
        Gizmos.DrawLine(pointA, pointB);
        Gizmos.DrawWireSphere(pointA, 0.2f);
        Gizmos.DrawWireSphere(pointB, 0.2f);
    }
}
