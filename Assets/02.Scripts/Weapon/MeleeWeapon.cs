using UnityEngine;

// 트리거의 Rigidbody는 중력 없이 손 뼈를 따라간다.
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class MeleeWeapon : MonoBehaviour
{
    private PlayerMeleeAttack _melee;

    private void Awake() => _melee = GetComponentInParent<PlayerMeleeAttack>();
    private void OnTriggerEnter(Collider other) => _melee?.Hit(other);
    private void OnTriggerStay(Collider other) => _melee?.Hit(other);
}
