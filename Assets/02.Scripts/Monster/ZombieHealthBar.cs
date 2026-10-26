using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Transform _healthBarRoot;
    [SerializeField, Range(0.5f, 5f)] private float _heightOffset = 2.2f;

    private Zombie _zombie;
    private Camera _mainCamera;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
        _mainCamera = Camera.main;

        // HealthBar_Canvas를 직접 연결하지 않아도 Slider의 부모 Canvas를 사용한다.
        if (_healthBarRoot == null && _slider != null)
        {
            _healthBarRoot = _slider.transform.parent;
        }

        EnsureWorldSpaceCanvas();
    }

    // 체력바가 화면 중앙에 고정되는 것을 막는다.
    // Screen Space Canvas는 위치를 월드 좌표로 바꿔도 화면 중앙에 남는다.
    private void EnsureWorldSpaceCanvas()
    {
        if (_healthBarRoot == null)
        {
            return;
        }

        Canvas canvas = _healthBarRoot.GetComponent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            return;
        }

        if (_healthBarRoot.GetComponentInParent<Zombie>() == null)
        {
            Debug.LogError(
                "[ZombieHealthBar] 체력바 Canvas가 좀비의 자식이 아니거나 World Space가 아닙니다.",
                canvas);
            return;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        _healthBarRoot.localScale = Vector3.one * 0.01f;

        Debug.LogWarning(
            "[ZombieHealthBar] 체력바 Canvas를 World Space로 변경했습니다. " +
            "프리팹에서도 Render Mode를 World Space로 맞춰 주세요.",
            canvas);
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

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_healthBarRoot == null || _mainCamera == null)
        {
            return;
        }

        // Canvas의 피벗/스케일 설정과 무관하게 좀비 머리 위에 고정한다.
        _healthBarRoot.position = _zombie.transform.position + Vector3.up * _heightOffset;

        // 체력바 전체(Canvas)가 카메라를 바라보게 한다.
        // Slider만 돌리면 부모 Canvas가 좀비를 따라 돌아 위치가 흔들린다.
        Vector3 direction = _mainCamera.transform.position - _healthBarRoot.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            _healthBarRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
