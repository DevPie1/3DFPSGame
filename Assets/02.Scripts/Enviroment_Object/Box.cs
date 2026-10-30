using UnityEngine;

// 체력이 다하면 상자를 제거한다.
public class Box : MonoBehaviour, IDamageable
{
    [SerializeField] private float _health = 30f;

    public void TakeDamage(DamageInfo damageInfo) => TakeDamage(damageInfo.Value);

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || _health <= 0f) return;
        _health = Mathf.Max(0f, _health - damage);
        if (_health <= 0f) Destroy(gameObject);
    }
}
