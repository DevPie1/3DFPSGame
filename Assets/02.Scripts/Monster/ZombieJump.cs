using UnityEngine;
using UnityEngine.AI;

// NavMeshLink를 우선 사용하고, 링크가 없는 가까운 발판은 착지 경로와 공간을 확인해 점프한다.
[RequireComponent(typeof(Zombie))]
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieJump : MonoBehaviour
{
    [Header("점프")]
    [SerializeField, Min(0.1f)] private float _jumpHeight = 2.2f;
    [SerializeField, Min(0.1f)] private float _jumpDuration = 0.85f;
    [SerializeField, Min(0f)] private float _jumpCooldown = 1.2f;

    [Header("링크 없는 발판 탐색")]
    [SerializeField] private bool _allowUnlinkedJump = true;
    [SerializeField, Min(0.1f)] private float _searchInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float _minJumpDistance = 1f;
    [SerializeField, Min(1f)] private float _maxJumpDistance = 8f;
    [SerializeField, Min(0f)] private float _minJumpUp = 0.5f;
    [Tooltip("링크 없는 점프에서 허용할 발 위치의 높이 차. 경사로로 접근한 뒤 낮은 단차만 넘는다.")]
    [SerializeField, Min(0.1f)] private float _maxJumpUp = 1.5f;
    [SerializeField, Min(0.1f)] private float _maxJumpDown = 5f;
    [SerializeField, Min(0.1f)] private float _sampleRadius = 1.2f;

    [Header("점프 경로 충돌")]
    [Tooltip("벽, 바닥, 천장 레이어. 현재 맵은 Default 레이어를 사용한다.")]
    [SerializeField] private LayerMask _obstacleMask = 1;
    [SerializeField, Range(8, 40)] private int _arcSegments = 24;

    [Header("디버그")]
    [SerializeField] private bool _debugLog;

    private Zombie _zombie;
    private NavMeshAgent _agent;
    private NavMeshPath _landingPath;
    private bool _isJumping;
    private bool _viaOffMeshLink;
    private Vector3 _jumpStart;
    private Vector3 _jumpEnd;
    private float _jumpElapsed;
    private float _nextJumpTime;
    private float _nextSearchTime;

    public bool IsJumping => _isJumping;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        _zombie = GetComponent<Zombie>();
        _agent = GetComponent<NavMeshAgent>();
        // NavMeshPath 역시 Play 중 도메인 리로드 이후에 복원해야 한다.
        if (_landingPath == null) _landingPath = new NavMeshPath();
        _agent.autoTraverseOffMeshLink = false;
    }

    private void OnDisable()
    {
        CancelJump();
        _nextJumpTime = 0f;
        _nextSearchTime = 0f;
    }

    private void Update()
    {
        if (_zombie.IsDead || !_agent.isActiveAndEnabled)
        {
            CancelJump();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
        if (_isJumping)
        {
            TickJump();
            return;
        }

        if (!_agent.isOnNavMesh || Time.time < _nextJumpTime || Time.time < _nextSearchTime) return;
        if (_zombie.State != ZombieState.Trace && _zombie.State != ZombieState.Patrol
            && _zombie.State != ZombieState.Roam && _zombie.State != ZombieState.Comeback) return;
        _nextSearchTime = Time.time + _searchInterval;

        // 순찰/복귀 중에도 링크를 처리해야 링크 입구에서 멈추지 않는다.
        if (_agent.isOnOffMeshLink)
        {
            OffMeshLinkData link = _agent.currentOffMeshLinkData;
            if (link.valid && link.activated && IsArcClear(_zombie.FeetPosition, link.endPos))
            {
                BeginJump(link.endPos, true);
            }
            return;
        }

        if (!_allowUnlinkedJump || _zombie.State != ZombieState.Trace
            || !_zombie.TryGetPlayerNavMeshPosition(out Vector3 playerGround)) return;

        float heightDifference = playerGround.y - _zombie.FeetPosition.y;
        Vector3 separation = playerGround - _zombie.FeetPosition;
        separation.y = 0f;
        if (heightDifference > _maxJumpUp + 0.5f || heightDifference < -_maxJumpDown - 0.5f
            || separation.magnitude > _maxJumpDistance * 2f + _sampleRadius) return;
        bool hasWalkingPath = NavMesh.CalculatePath(_zombie.FeetPosition, playerGround,
            _zombie.NavigationFilter, _landingPath) && _landingPath.status == NavMeshPathStatus.PathComplete;

        // 같은 높이에서는 걸을 수 없는 틈만 점프한다. 위/아래층은 가까운 발판으로 이동한다.
        if (Mathf.Abs(heightDifference) < _minJumpUp && hasWalkingPath) return;
        if (TryFindJumpTarget(playerGround, out Vector3 target)) BeginJump(target, false);
    }

    private bool TryFindJumpTarget(Vector3 playerGround, out Vector3 target)
    {
        if (TrySample(playerGround, playerGround, out target)) return true;

        // 플레이어 위치가 멀거나 막혀 있으면 같은 발판의 가까운 가장자리도 탐색한다.
        Vector3 towardZombie = _zombie.FeetPosition - playerGround;
        towardZombie.y = 0f;
        if (towardZombie.sqrMagnitude < 0.001f) towardZombie = transform.forward;
        towardZombie.Normalize();

        for (float radius = 0.75f; radius <= _maxJumpDistance; radius += 0.75f)
        {
            for (int turn = 0; turn < 8; turn++)
            {
                Vector3 offset = Quaternion.Euler(0f, turn * 45f, 0f) * towardZombie * radius;
                if (TrySample(playerGround + offset, playerGround, out target)) return true;
            }
        }

        target = default;
        return false;
    }

    private bool TrySample(Vector3 candidate, Vector3 playerGround, out Vector3 target)
    {
        target = default;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _sampleRadius, _zombie.NavigationFilter)) return false;
        // 아래층 바닥을 위층 착지점으로 오인하지 않는다.
        if (Mathf.Abs(hit.position.y - playerGround.y) > 0.5f) return false;

        Vector3 start = _zombie.FeetPosition;
        float height = hit.position.y - start.y;
        if (height > _maxJumpUp || height < -_maxJumpDown) return false;
        Vector3 horizontal = hit.position - start;
        horizontal.y = 0f;
        float distance = horizontal.magnitude;
        if (distance < _minJumpDistance || distance > _maxJumpDistance) return false;

        // 착지 후 플레이어에게 걸어갈 수 있는 발판만 고른다.
        if (!NavMesh.CalculatePath(hit.position, playerGround, _zombie.NavigationFilter, _landingPath)
            || _landingPath.status != NavMeshPathStatus.PathComplete) return false;
        if (!IsArcClear(start, hit.position)) return false;

        target = hit.position;
        return true;
    }

    private Vector3 ArcPosition(Vector3 start, Vector3 end, float t)
    {
        Vector3 position = Vector3.Lerp(start, end, t);
        position.y += _jumpHeight * 4f * t * (1f - t);
        return position;
    }

    private bool IsArcClear(Vector3 startFeet, Vector3 endFeet)
    {
        // NavMesh 샘플링은 벽/천장 충돌을 검사하지 않으므로 몸 크기로 포물선을 훑는다.
        float radius = Mathf.Max(0.05f, _agent.radius - 0.05f);
        Vector3 bottom = Vector3.up * (_agent.radius + 0.03f);
        Vector3 top = Vector3.up * Mathf.Max(_agent.radius + 0.03f, _agent.height - _agent.radius);
        if (Physics.CheckCapsule(endFeet + bottom, endFeet + top, radius,
            _obstacleMask, QueryTriggerInteraction.Ignore)) return false;

        Vector3 previous = startFeet;
        for (int i = 1; i <= _arcSegments; i++)
        {
            Vector3 next = ArcPosition(startFeet, endFeet, (float)i / _arcSegments);
            Vector3 delta = next - previous;
            if (delta.sqrMagnitude > 0.000001f && Physics.CapsuleCast(previous + bottom,
                previous + top, radius, delta.normalized, delta.magnitude,
                _obstacleMask, QueryTriggerInteraction.Ignore)) return false;
            previous = next;
        }
        return true;
    }

    private void BeginJump(Vector3 groundEnd, bool viaOffMeshLink)
    {
        _isJumping = true;
        _viaOffMeshLink = viaOffMeshLink;
        _jumpStart = transform.position;
        // 프리팹의 루트는 발보다 baseOffset(현재 1m)만큼 위에 있다.
        _jumpEnd = groundEnd + Vector3.up * _agent.baseOffset;
        _jumpElapsed = 0f;
        _agent.isStopped = true;
        // 링크 위에서 ResetPath를 호출하면 점프 전에 링크가 즉시 완료된다.
        if (!viaOffMeshLink) _agent.ResetPath();
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        if (_debugLog) Debug.Log($"[ZombieJump] 점프: {_jumpStart} -> {_jumpEnd}", this);
    }

    private void TickJump()
    {
        _jumpElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_jumpElapsed / Mathf.Max(0.1f, _jumpDuration));
        transform.position = ArcPosition(_jumpStart, _jumpEnd, t);

        Vector3 direction = _jumpEnd - _jumpStart;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }

        if (t >= 1f) FinishJump();
    }

    private void FinishJump()
    {
        transform.position = _jumpEnd;
        if (_viaOffMeshLink && _agent.isOnNavMesh && _agent.isOnOffMeshLink)
        {
            _agent.CompleteOffMeshLink();
        }

        // 넓은 반경으로 재샘플링하면 다른 층에 붙을 수 있으므로 선택한 착지점으로만 이동한다.
        if (!_agent.Warp(_jumpEnd))
        {
            _agent.Warp(_jumpStart);
            Debug.LogWarning("[ZombieJump] 착지 NavMesh가 사라져 출발점으로 복귀합니다.", this);
        }
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _isJumping = false;
        _viaOffMeshLink = false;
        _jumpElapsed = 0f;
        _nextJumpTime = Time.time + _jumpCooldown;
        _zombie.RefreshPathAfterJump();
    }

    internal void CancelJump()
    {
        if (!_isJumping) return;
        _isJumping = false;
        _viaOffMeshLink = false;
        _jumpElapsed = 0f;
        if (_agent != null)
        {
            _agent.updatePosition = true;
            _agent.updateRotation = true;
        }
    }
}
