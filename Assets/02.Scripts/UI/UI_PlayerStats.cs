using UnityEngine;
using UnityEngine.UI;

// 체력은 변경 알림을 받아 갱신하고, 스태미나는 매 프레임 표시한다.
public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    private void Awake()
    {
        if (_playerStats == null)
        {
            _playerStats = FindFirstObjectByType<PlayerStats>();
        }

        SetupSlider(_healthSlider);
        SetupSlider(_staminaSlider);
    }

    private void OnEnable()
    {
        if (_playerStats == null)
        {
            return;
        }

        _playerStats.OnHealthChanged += RefreshHealth;
        RefreshHealth(_playerStats.Health, _playerStats.MaxHealth);
    }

    private void OnDisable()
    {
        if (_playerStats != null)
        {
            _playerStats.OnHealthChanged -= RefreshHealth;
        }
    }

    private void Update()
    {
        if (_playerStats == null)
        {
            return;
        }

        if (_staminaSlider != null)
        {
            _staminaSlider.value = _playerStats.Stamina01;
        }
    }

    private void RefreshHealth(float health, float maxHealth)
    {
        if (_healthSlider != null)
        {
            _healthSlider.value = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
        }
    }

    private void SetupSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
    }
}
