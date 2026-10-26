using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

[Serializable]
public class ZombieSpawnPoint
{
    public Transform Position;
    public ZombieIdleMode IdleMode = ZombieIdleMode.Roam;
    public Transform[] PatrolPoints;
}

// 최대 수는 한 웨이브의 총 생성 수다. 남은 수에는 아직 생성하지 않은 좀비도 포함한다.
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private ZombieSpawnPoint[] _spawnPoints;
    [SerializeField, Min(1)] private int _maxZombieCount = 5;
    [SerializeField, Min(1)] private int _maxAliveZombies = 2;
    [SerializeField, Min(0.1f)] private float _spawnInterval = 1f;

    private readonly HashSet<Zombie> _livingZombies = new HashSet<Zombie>();
    private readonly HashSet<Zombie> _spawnedZombies = new HashSet<Zombie>();
    private int _spawnedCount;
    private int _killedCount;
    private int _nextPoint;
    private float _spawnTimer;

    public int MaxCount => _maxZombieCount;
    public int RemainingCount => Mathf.Max(0, _maxZombieCount - _killedCount);
    public int AliveCount => _livingZombies.Count;
    public event Action<int, int> OnCountChanged;

    private void OnEnable()
    {
        RestartWave();
    }

    private void OnDisable()
    {
        DespawnAll();
    }

    private void OnValidate()
    {
        _maxZombieCount = Mathf.Max(1, _maxZombieCount);
        _maxAliveZombies = Mathf.Clamp(_maxAliveZombies, 1, _maxZombieCount);
        _spawnInterval = Mathf.Max(0.1f, _spawnInterval);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f || _spawnedCount >= _maxZombieCount || AliveCount >= _maxAliveZombies)
        {
            return;
        }

        _spawnTimer = _spawnInterval;
        SpawnNext();
    }

    [ContextMenu("Restart Wave")]
    public void RestartWave()
    {
        DespawnAll();
        _spawnedCount = 0;
        _killedCount = 0;
        _nextPoint = 0;
        _spawnTimer = 0f;
        OnCountChanged?.Invoke(RemainingCount, MaxCount);
    }

    private void SpawnNext()
    {
        if (_zombiePrefab == null || _spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("[ZombieSpawner] 좀비 프리팹과 생성 위치를 연결해 주세요.", this);
            enabled = false;
            return;
        }

        // 비어 있는 위치는 건너뛰고 다음 유효한 지점을 사용한다.
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            ZombieSpawnPoint point = _spawnPoints[_nextPoint];
            _nextPoint = (_nextPoint + 1) % _spawnPoints.Length;
            if (point == null || point.Position == null) continue;

            Zombie zombie = LeanPool.Spawn(_zombiePrefab, point.Position.position, point.Position.rotation);
            zombie.Configure(point.IdleMode, point.PatrolPoints);
            zombie.OnDied += OnZombieDied;
            zombie.OnDespawned += OnZombieDespawned;
            _livingZombies.Add(zombie);
            _spawnedZombies.Add(zombie);
            _spawnedCount++;
            return;
        }

        Debug.LogWarning("[ZombieSpawner] 유효한 생성 위치가 없습니다.", this);
        enabled = false;
    }

    private void OnZombieDied(Zombie zombie)
    {
        if (_livingZombies.Remove(zombie))
        {
            _killedCount++;
            OnCountChanged?.Invoke(RemainingCount, MaxCount);
        }
    }

    private void OnZombieDespawned(Zombie zombie)
    {
        zombie.OnDied -= OnZombieDied;
        zombie.OnDespawned -= OnZombieDespawned;
        _spawnedZombies.Remove(zombie);
        if (_livingZombies.Remove(zombie))
        {
            // 사망하지 않고 회수한 좀비는 다시 생성할 수 있게 한다.
            _spawnedCount--;
        }
    }

    private void DespawnAll()
    {
        foreach (Zombie zombie in _spawnedZombies)
        {
            if (zombie == null) continue;
            zombie.OnDied -= OnZombieDied;
            zombie.OnDespawned -= OnZombieDespawned;
            LeanPool.Despawn(zombie);
        }

        _spawnedZombies.Clear();
        _livingZombies.Clear();
    }
}
