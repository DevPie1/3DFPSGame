using UnityEngine;

// Animator와 같은 오브젝트에서 이벤트를 받아 플레이어 루트에 전달한다.
public class PlayerMeleeEvent : MonoBehaviour
{
    private PlayerMeleeAttack _melee;

    private void OnEnable() => _melee = GetComponentInParent<PlayerMeleeAttack>();
    public void ActivateCollider() => _melee?.ActivateCollider();
    public void DeactivateCollider() => _melee?.DeactivateCollider();
    public void FanAttack() => _melee?.FanAttack();
}
