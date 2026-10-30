using System;
using UnityEngine;

// 골드의 소유자는 플레이어다. HUD와 획득 효과는 변경 알림만 구독한다.
public class PlayerGold : MonoBehaviour
{
    [SerializeField, HideInInspector] private int _gold;

    public int Gold => _gold;
    public event Action<int> OnGoldChanged;
    public event Action<int> OnGoldCollected;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
        OnGoldCollected?.Invoke(amount);
    }
}
