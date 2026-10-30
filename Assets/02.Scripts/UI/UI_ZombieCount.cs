using TMPro;
using System.Collections.Generic;
using UnityEngine;

// 스포너의 수량 변경 알림을 구독한다. 매 프레임 좀비를 검색하지 않는다.
[RequireComponent(typeof(TextMeshProUGUI))]
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private ZombieSpawner _spawner;
    [SerializeField] private ZombieSpawner[] _additionalSpawners;
    private readonly HashSet<ZombieSpawner> _sources = new HashSet<ZombieSpawner>();
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
        if (_countText == null) _countText = GetComponent<TextMeshProUGUI>();
        _sources.Clear();
        if (_spawner != null) _sources.Add(_spawner);
        if (_additionalSpawners != null)
            foreach (ZombieSpawner spawner in _additionalSpawners)
                if (spawner != null) _sources.Add(spawner);
        foreach (ZombieSpawner spawner in _sources) spawner.OnCountChanged += RefreshCount;
        RefreshCount(0, 0);
    }

    private void OnDisable()
    {
        foreach (ZombieSpawner spawner in _sources)
            if (spawner != null) spawner.OnCountChanged -= RefreshCount;
        _sources.Clear();
    }

    private void RefreshCount(int remaining, int maximum)
    {
        remaining = 0;
        maximum = 0;
        foreach (ZombieSpawner spawner in _sources)
        {
            if (spawner == null) continue;
            remaining += spawner.RemainingCount;
            maximum += spawner.MaxCount;
        }
        _countText.text = $"남은 좀비: {remaining} / {maximum}";
    }
}
