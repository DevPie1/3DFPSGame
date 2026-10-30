using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Gold : MonoBehaviour
{
    [SerializeField, Min(1)] private int _amount = 10;
    [SerializeField, Min(0f)] private float _popPower = 3f;
    [SerializeField, Min(0f)] private float _pickupDelay = 1f;
    [SerializeField, Min(0f)] private float _magnetDistance = 3f;
    [SerializeField, Min(0.01f)] private float _flyTime = 0.4f;
    [SerializeField, Min(0f)] private float _arcHeight = 2f;

    private Rigidbody _rigidbody;
    private Collider _collider;
    private PlayerGold _player;
    [SerializeField, HideInInspector] private float _timer;
    private bool _isFlying;
    [SerializeField, HideInInspector] private bool _collected;

    public int Amount => _amount;

    private void OnEnable()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _player = player.GetComponent<PlayerGold>();
        _isFlying = false;
        if (_collected) Destroy(gameObject);
        else if (Application.isPlaying && _rigidbody.isKinematic) StartCoroutine(FlyToPlayer_Coroutine());
    }

    private void Start()
    {
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), 1.5f, Random.Range(-1f, 1f));
        _rigidbody.AddForce(direction * _popPower, ForceMode.Impulse);
        // 동전이 발밑에서 캐릭터의 이동을 막지 않게 한다. 바닥과는 계속 충돌한다.
        if (_player != null && _player.TryGetComponent(out Collider playerCollider))
            Physics.IgnoreCollision(_collider, playerCollider);
    }

    private void Update()
    {
        if (_isFlying || _player == null || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)) return;
        _timer += Time.deltaTime;
        if (_timer >= _pickupDelay && Vector3.Distance(transform.position, _player.transform.position) <= _magnetDistance)
            StartCoroutine(FlyToPlayer_Coroutine());
    }

    private IEnumerator FlyToPlayer_Coroutine()
    {
        _isFlying = true;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        _collider.enabled = false;
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < _flyTime && _player != null)
        {
            Vector3 end = _player.transform.position;
            Vector3 control = (start + end) * 0.5f + Vector3.up * _arcHeight;
            transform.position = QuadraticBezier(start, control, end, Mathf.Clamp01(elapsed / _flyTime));
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (_player != null && !_collected)
        {
            _collected = true;
            transform.position = _player.transform.position;
            _player.AddGold(_amount);
        }
        Destroy(gameObject);
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        return Vector3.Lerp(Vector3.Lerp(start, control, t), Vector3.Lerp(control, end, t), t);
    }
}
