using System.Collections.Generic;
using UnityEngine;

public enum MeleeHitMode
{
    Collider,
    Fan
}

// 모드 알림으로 검을 표시하고, Slash 이벤트가 허용한 구간에만 대미지를 준다.
[RequireComponent(typeof(PlayerWeapons))]
public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private GameObject _katana;
    [SerializeField] private Collider _katanaCollider;
    [SerializeField] private Animator _animator;
    [SerializeField] private KatanaSlashTrail _slashTrail;
    [SerializeField, Min(0f)] private float _damage = 25f;
    [SerializeField] private MeleeHitMode _hitMode = MeleeHitMode.Fan;
    [SerializeField, Min(0.1f)] private float _range = 2f;
    [SerializeField, Range(0f, 360f)] private float _angle = 90f;
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private LayerMask _obstacleMask = 1;

    private static readonly int _slashHash = Animator.StringToHash("Slash");
    private readonly HashSet<IDamageable> _fanTargets = new HashSet<IDamageable>();
    private PlayerWeapons _weapons;
    private int _meleeLayer = -1;
    private int _meleeIdleLayer = -1;
    private bool _attacking;
    private bool _waitingToStart;
    private bool _windowOpen;
    private bool _hitConsumed;
    private bool _fanExecuted;

    public bool IsAttacking => _attacking;
    public MeleeHitMode HitMode => _hitMode;

    private void Awake()
    {
        _weapons = GetComponent<PlayerWeapons>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        _meleeLayer = _animator != null ? _animator.GetLayerIndex("Melee Layer") : -1;
        _meleeIdleLayer = _animator != null ? _animator.GetLayerIndex("Melee Idle") : -1;
        DeactivateCollider();
        if (_katana == null || _katanaCollider == null || _meleeLayer < 0)
        {
            Debug.LogError("[PlayerMeleeAttack] Katana, Blade Collider, Melee Layer 연결을 확인하세요.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (_weapons == null) _weapons = GetComponent<PlayerWeapons>();
        _weapons.OnModeChanged += RefreshKatana;
        RefreshKatana(_weapons.Mode);
    }

    private void OnDisable()
    {
        if (_weapons != null) _weapons.OnModeChanged -= RefreshKatana;
        CancelAttack();
        SetIdleWeight(0f);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing))
        {
            if (_attacking) CancelAttack();
            return;
        }
        if (_attacking)
        {
            if (_waitingToStart)
            {
                if (!IsResting()) _waitingToStart = false;
            }
            else if (IsResting())
            {
                _attacking = false;
                DeactivateCollider();
            }
        }
        if (_weapons.CanUseInput && Input.GetButtonDown("Fire2")) TryAttack();
    }

    public bool TryAttack()
    {
        if (!isActiveAndEnabled || _weapons.Mode != WeaponMode.Melee || _attacking || !IsResting()
            || Time.timeScale <= 0f || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)) return false;
        DeactivateCollider();
        _hitConsumed = false;
        _fanExecuted = false;
        _fanTargets.Clear();
        _attacking = true;
        _waitingToStart = true;
        _animator.SetTrigger(_slashHash);
        return true;
    }

    private bool IsResting() => _animator != null && _meleeLayer >= 0
        && _animator.GetCurrentAnimatorStateInfo(_meleeLayer).IsName("Empty")
        && !_animator.IsInTransition(_meleeLayer);

    private void RefreshKatana(WeaponMode mode)
    {
        CancelAttack();
        SetIdleWeight(mode == WeaponMode.Melee ? 1f : 0f);
        if (_katana != null) _katana.SetActive(mode == WeaponMode.Melee);
    }

    private void SetIdleWeight(float weight)
    {
        if (_animator != null && _meleeIdleLayer >= 0) _animator.SetLayerWeight(_meleeIdleLayer, weight);
    }

    private void CancelAttack()
    {
        DeactivateCollider();
        if (_slashTrail != null) _slashTrail.ClearTrail();
        _attacking = false;
        _waitingToStart = false;
        if (_animator != null && _meleeLayer >= 0)
        {
            _animator.ResetTrigger(_slashHash);
            _animator.Play("Empty", _meleeLayer, 0f);
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && _attacking) CancelAttack();
    }

    public void ActivateCollider()
    {
        if (!_attacking || _weapons.Mode != WeaponMode.Melee || _hitConsumed) return;
        _windowOpen = true;
        if (_slashTrail != null) _slashTrail.BeginTrail();
        if (_katanaCollider != null) _katanaCollider.enabled = _hitMode == MeleeHitMode.Collider;
    }

    public void DeactivateCollider()
    {
        CloseHitbox();
        if (_slashTrail != null) _slashTrail.EndTrail();
    }

    private void CloseHitbox()
    {
        _windowOpen = false;
        if (_katanaCollider != null) _katanaCollider.enabled = false;
    }

    public void Hit(Collider other)
    {
        if (_hitMode != MeleeHitMode.Collider || !_attacking || !_windowOpen || _hitConsumed
            || other == null || other.transform.IsChildOf(transform)) return;
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;
        _hitConsumed = true;
        CloseHitbox();
        GiveDamage(target, other.ClosestPoint(transform.position));
    }

    public void FanAttack()
    {
        if (_hitMode != MeleeHitMode.Fan || !_attacking || _fanExecuted || _weapons.Mode != WeaponMode.Melee) return;
        _fanExecuted = true;
        Vector3 origin = transform.position;
        foreach (Collider collider in Physics.OverlapSphere(origin, _range, _targetMask, QueryTriggerInteraction.Ignore))
        {
            if (collider.transform.IsChildOf(transform)) continue;
            Vector3 point = collider.ClosestPoint(origin);
            Vector3 direction = point - origin;
            direction.y = 0f;
            if (Vector3.Angle(transform.forward, direction) > _angle * 0.5f) continue;
            if (Physics.Linecast(origin, point, _obstacleMask, QueryTriggerInteraction.Ignore)) continue;
            IDamageable target = collider.GetComponentInParent<IDamageable>();
            if (target != null && _fanTargets.Add(target)) GiveDamage(target, point);
        }
    }

    private void GiveDamage(IDamageable target, Vector3 point)
    {
        target.TakeDamage(new DamageInfo
        {
            Value = _damage,
            HitPoint = point,
            HitNormal = (transform.position - point).normalized,
            Attacker = gameObject
        });
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Vector3 previous = origin + Quaternion.Euler(0f, -_angle * 0.5f, 0f) * transform.forward * _range;
        Gizmos.DrawLine(origin, previous);
        int count = Mathf.Max(1, Mathf.CeilToInt(_angle / 10f));
        for (int i = 1; i <= count; i++)
        {
            Vector3 next = origin + Quaternion.Euler(0f, -_angle * 0.5f + _angle * i / count, 0f) * transform.forward * _range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
        Gizmos.DrawLine(origin, previous);
    }
}
