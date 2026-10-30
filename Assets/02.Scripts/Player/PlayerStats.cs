using System;
using UnityEngine;

// 플레이어의 체력과 스태미나를 관리한다.
// 체력은 좀비 공격 등으로 줄어들고, 스태미나는 달리기 중에 소모된다.
public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("체력")][SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _health;

    [Header("스태미나")][SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _stamina;
    [SerializeField] private float _sprintDrainPerSecond = 25f; // 달리는 동안 초당 소모량
    [SerializeField] private float _staminaRegenPerSecond = 20f; // 초당 회복량
    [SerializeField] private float _staminaRegenDelay = 1f; // 달리기를 멈춘 뒤 회복 시작까지의 시간

    private float _regenDelayTimer;
    private bool _sprintLocked;

    public float Health => _health;
    public float MaxHealth => _maxHealth;
    public float Stamina => _stamina;
    public float MaxStamina => _maxStamina;

    // UI Slider에 바로 넣을 수 있는 0~1 비율 값
    public float Health01 => _maxHealth > 0f ? Mathf.Clamp01(_health / _maxHealth) : 0f;
    public float Stamina01 => _maxStamina > 0f ? Mathf.Clamp01(_stamina / _maxStamina) : 0f;

    public bool IsDead => _health <= 0f;

    // (현재 체력, 최대 체력)을 전달한다. UI가 누구인지는 알 필요가 없다.
    public event Action<float, float> OnDamaged;
    public event Action<float, float> OnHealthChanged;

    private void Awake()
    {
        _health = _maxHealth;
        _stamina = _maxStamina;
        _sprintLocked = false;

        OnHealthChanged?.Invoke(_health, _maxHealth);
    }

    private void OnValidate()
    {
        _maxHealth = Mathf.Max(1f, _maxHealth);
        _maxStamina = Mathf.Max(1f, _maxStamina);

        // 에디터에서 값을 조정할 때는 현재 수치도 가득 찬 상태로 보여준다.
        if (Application.isPlaying == false)
        {
            _health = _maxHealth;
            _stamina = _maxStamina;
        }
    }

    private void Update()
    {
        // 달리기를 멈춘 직후에는 잠시 회복을 미룬다.
        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= Time.deltaTime;
            return;
        }

        if (_stamina < _maxStamina)
        {
            _stamina = Mathf.Min(_maxStamina, _stamina + _staminaRegenPerSecond * Time.deltaTime);
        }
    }

    // 달리기 한 프레임 동안 스태미나를 소모한다.
    // 스태미나가 없으면 false를 반환해서 걷기 속도로 전환하게 한다.
    public bool TrySprint(float deltaTime)
    {
        // 0이 된 뒤 바로 다시 달리기를 반복하지 않도록 락을 건다.
        if (_sprintLocked)
        {
            return false;
        }

        if (_stamina <= 0f)
        {
            _sprintLocked = true;
            return false;
        }

        _stamina = Mathf.Max(0f, _stamina - _sprintDrainPerSecond * deltaTime);
        _regenDelayTimer = _staminaRegenDelay;

        if (_stamina <= 0f)
        {
            _sprintLocked = true;
        }

        return true;
    }

    // 달리기 키를 뗐을 때 호출해서 다시 달릴 수 있게 한다.
    public void ReleaseSprintLock()
    {
        _sprintLocked = false;
    }

    public void TakeDamage(DamageInfo damageInfo) => TakeDamage(damageInfo.Value);

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead)
        {
            return;
        }

        _health = Mathf.Max(0f, _health - damage);

        OnHealthChanged?.Invoke(_health, _maxHealth);
        OnDamaged?.Invoke(_health, _maxHealth);

        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌습니다.");
            GameManager.Instance?.GameOver();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        float health = Mathf.Min(_maxHealth, _health + amount);
        if (health == _health)
        {
            return;
        }

        _health = health;
        OnHealthChanged?.Invoke(_health, _maxHealth);
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _stamina = Mathf.Min(_maxStamina, _stamina + amount);
    }
}
