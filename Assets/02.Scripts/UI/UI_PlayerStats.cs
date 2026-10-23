using UnityEngine;
using UnityEngine.UI;

// 플레이어의 체력/스태미나를 Slider로 표시한다.
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

    private void Update()
    {
        if (_playerStats == null)
        {
            return;
        }

        if (_healthSlider != null)
        {
            _healthSlider.value = _playerStats.Health01;
        }

        if (_staminaSlider != null)
        {
            _staminaSlider.value = _playerStats.Stamina01;
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
