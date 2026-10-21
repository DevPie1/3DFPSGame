using System;
using Lean.Pool;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    // 다른 콜라이더 와 충돌 순간 실행됨
    private void OnCollisionEnter(Collision collision)
    {
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        //Todo. 오브젝트 풀링 적용
        LeanPool.Despawn(gameObject);
    }
}
