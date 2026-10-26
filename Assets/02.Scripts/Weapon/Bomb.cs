using Lean.Pool;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Bomb : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField, Min(0.1f)] private float _maxLifetime = 5f;

    private Rigidbody _rigidbody;
    private Collider _collider;
    private Collider _throwerCollider;
    private float _remainingLifetime;
    private bool _isSpawned;
    private bool _hasExploded;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    private void OnEnable() => OnSpawn();

    public void OnSpawn()
    {
        if (_isSpawned) return;
        _isSpawned = true;
        _hasExploded = false;
        _remainingLifetime = _maxLifetime;
        ResetVelocity();
    }

    public void Launch(Vector3 impulse, Collider throwerCollider)
    {
        _throwerCollider = throwerCollider;
        if (_throwerCollider != null)
        {
            Physics.IgnoreCollision(_collider, _throwerCollider, true);
        }

        _rigidbody.AddForce(impulse, ForceMode.Impulse);
    }

    private void Update()
    {
        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f) Explode();
    }

    // 다른 콜라이더 와 충돌 순간 실행됨
    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void Explode()
    {
        if (!_isSpawned || _hasExploded) return;
        _hasExploded = true;
        if (_explosionEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            effect.Play();
        }

        LeanPool.Despawn(gameObject);
    }

    public void OnDespawn()
    {
        if (_throwerCollider != null)
        {
            Physics.IgnoreCollision(_collider, _throwerCollider, false);
            _throwerCollider = null;
        }

        ResetVelocity();
        _isSpawned = false;
    }

    private void OnDisable() => OnDespawn();

    private void ResetVelocity()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }
}
