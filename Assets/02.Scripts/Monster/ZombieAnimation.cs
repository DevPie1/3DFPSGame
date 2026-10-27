using UnityEngine;

// 좀비의 FSM 상태와 이동 속도를 Animator 파라미터로 전달한다.
[RequireComponent(typeof(Zombie))]
public class ZombieAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    private Zombie _zombie;

    private static readonly int _speedHash = Animator.StringToHash("Speed");
    private static readonly int _attackHash = Animator.StringToHash("Attack");
    private static readonly int _hitHash = Animator.StringToHash("Hit");
    private static readonly int _deadHash = Animator.StringToHash("Dead");

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        if (_zombie == null)
        {
            return;
        }

        _zombie.OnStateChanged += HandleStateChanged;
        _zombie.OnAttackPerformed += HandleAttackPerformed;
        ResetAnimation();
    }

    private void OnDisable()
    {
        if (_zombie == null)
        {
            return;
        }

        _zombie.OnStateChanged -= HandleStateChanged;
        _zombie.OnAttackPerformed -= HandleAttackPerformed;
    }

    private void Update()
    {
        if (_animator == null || _zombie == null)
        {
            return;
        }

        _animator.SetFloat(_speedHash, GetAnimationSpeed());
    }

    private float GetAnimationSpeed()
    {
        switch (_zombie.State)
        {
            case ZombieState.Trace:
                return _zombie.AgentVelocity;

            case ZombieState.Patrol:
            case ZombieState.Roam:
            case ZombieState.Comeback:
                return _zombie.AgentVelocity > 0.05f ? 2f : 0f;

            default:
                return 0f;
        }
    }

    private void HandleStateChanged(ZombieState state)
    {
        if (_animator == null)
        {
            return;
        }

        if (state == ZombieState.Hit)
        {
            _animator.SetTrigger(_hitHash);
        }
        else if (state == ZombieState.Death)
        {
            _animator.SetBool(_deadHash, true);
        }
    }

    private void HandleAttackPerformed()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(_attackHash);
        }
    }

    private void ResetAnimation()
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetBool(_deadHash, false);
        _animator.SetFloat(_speedHash, 0f);
        _animator.Play("Idle", 0, 0f);
    }
}
