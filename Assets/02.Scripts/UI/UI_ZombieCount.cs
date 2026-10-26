using TMPro;
using UnityEngine;

// 스포너의 수량 변경 알림을 구독한다. 매 프레임 좀비를 검색하지 않는다.
[RequireComponent(typeof(TextMeshProUGUI))]
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private ZombieSpawner _spawner;
    private TextMeshProUGUI _countText;

    private void Awake()
    {
        _countText = GetComponent<TextMeshProUGUI>();
        if (_spawner == null)
        {
            _spawner = FindFirstObjectByType<ZombieSpawner>();
        }
    }

    private void OnEnable()
    {
        if (_spawner == null)
        {
            _countText.text = "남은 좀비: 0 / 0";
            return;
        }

        _spawner.OnCountChanged += RefreshCount;
        RefreshCount(_spawner.RemainingCount, _spawner.MaxCount);
    }

    private void OnDisable()
    {
        if (_spawner != null)
        {
            _spawner.OnCountChanged -= RefreshCount;
        }
    }

    private void RefreshCount(int remaining, int maximum)
    {
        _countText.text = $"남은 좀비: {remaining} / {maximum}";
    }
}
