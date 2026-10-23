using UnityEngine;

// 키보드 방향키에 따라 그 방향으로 이동하고, 점프 키로 점프한다.
public class PlayerMove : MonoBehaviour
{
    private const float GroundedVelocity = -2f;

    [Header("이동")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _sprintSpeed = 12f;

    [Header("스태미나")]
    [SerializeField] private PlayerStats _stats;

    [Header("점프")]
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField] private int _maxJumps = 2;
    [SerializeField] private float _riseGravity = -20f;
    [SerializeField] private float _fallGravity = -36f;
    [SerializeField] private float _maxFallSpeed = -25f;
    [SerializeField] private float _jumpCutMultiplier = 0.5f;
    [SerializeField] private float _coyoteTime = 0.12f;
    [SerializeField] private float _jumpBufferTime = 0.12f;

    [SerializeField] private CharacterController _characterController;

    private float _yVelocity;
    private int _jumpCount;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        if (_stats == null)
        {
            _stats = GetComponent<PlayerStats>();
        }
    }

    private void Update()
    {
        bool isGrounded = _characterController.isGrounded;

        UpdateJumpTimers(isGrounded);
        HandleJumpInput();
        ApplyGravity(isGrounded);
        Move();
    }

    private void UpdateJumpTimers(bool isGrounded)
    {
        if (isGrounded && _yVelocity <= 0f)
        {
            // 지면에 닿아 있으면 코요테 시간을 채우고 점프 횟수를 초기화한다.
            _coyoteTimer = _coyoteTime;
            _jumpCount = 0;
        }
        else
        {
            if (_coyoteTimer > 0f)
            {
                _coyoteTimer -= Time.deltaTime;
            }

            // 코요테 시간이 끝나면 지면 점프를 이미 사용한 것으로 처리한다.
            // (절벽에서 떨어진 뒤에도 공중 점프 1회는 남는다)
            if (_coyoteTimer <= 0f && _jumpCount == 0)
            {
                _jumpCount = 1;
            }
        }

        // 착지 직전에 누른 점프를 기억해 두었다가 착지 순간 실행한다.
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferTimer = _jumpBufferTime;
        }
        else if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void HandleJumpInput()
    {
        if (_jumpBufferTimer > 0f && _jumpCount < _maxJumps)
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
        }

        // 점프 키를 떼면 상승 속도를 줄여 점프 높이를 조절한다.
        if (Input.GetButtonUp("Jump") && _yVelocity > 0f)
        {
            _yVelocity *= _jumpCutMultiplier;
        }
    }

    private void ApplyGravity(bool isGrounded)
    {
        // 상승할 때와 하강할 때 중력을 다르게 적용해 체공 시간을 줄인다.
        float gravity = _yVelocity > 0f ? _riseGravity : _fallGravity;
        _yVelocity += gravity * Time.deltaTime;

        if (_yVelocity < _maxFallSpeed)
        {
            _yVelocity = _maxFallSpeed;
        }

        // 지면에 붙어 있을 때는 살짝 아래로 눌러 isGrounded가 안정적으로 유지되게 한다.
        if (isGrounded && _yVelocity < 0f)
        {
            _yVelocity = GroundedVelocity;
        }
    }

    private void Move()
    {
        // 1. 키보드 입력을 받는다.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 2. 키보드 입력에 따라 방향을 구한다.
        Vector3 direction = new Vector3(h, 0f, v);
        direction.Normalize(); // 대각선이어도 방향의 길이 1로 바꾸기 위해
        // 캐릭터의 회전 방향을 기준으로 로컬 방향 벡터를 월드 방향 벡터로 변환한다.
        direction = transform.TransformDirection(direction);

        // 3. 속도를 구한다.
        // 스태미나가 있으면 달리기, 없으면 걷기 속도로 전환한다.
        bool wantsSprint = Input.GetButton("Sprint") && direction.sqrMagnitude > 0.01f;

        if (!wantsSprint && _stats != null)
        {
            _stats.ReleaseSprintLock();
        }

        bool isSprinting = wantsSprint && (_stats == null || _stats.TrySprint(Time.deltaTime));
        float currentSpeed = isSprinting ? _sprintSpeed : _moveSpeed;
        Vector3 velocity = direction * currentSpeed;
        velocity.y = _yVelocity;

        // 4. 속도(방향과 속력)에 따라 이동한다.
        _characterController.Move(velocity * Time.deltaTime);
    }
}
