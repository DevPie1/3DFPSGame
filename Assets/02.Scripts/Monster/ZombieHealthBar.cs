using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
    }

    private void LateUpdate()
    {
        if (_zombie == null || _slider == null)
        {
            return;
        }

        _slider.value = _zombie.MaxHealth > 0f
            ? Mathf.Clamp01(_zombie.Health / _zombie.MaxHealth)
            : 0f;

        transform.forward = Camera.main.transform.forward;
    }
}
