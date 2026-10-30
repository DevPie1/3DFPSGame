using UnityEngine;

// Animator가 있는 모델에서 루트의 내려찍기 판정으로 이벤트를 전달한다.
public class ZombieAnimationEvents : MonoBehaviour
{
    private Zombie _zombie;

    private void OnEnable() => _zombie = GetComponentInParent<Zombie>();

    public void OnAttackHit() => _zombie?.OnAttackHit();
}
