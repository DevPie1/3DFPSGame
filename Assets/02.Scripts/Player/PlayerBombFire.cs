using System.Collections;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Lean.Pool;

// G 키로 힘을 모아 놓으면 투척한다. B 키는 폭탄 재장전이다.
public class PlayerBombFire : MonoBehaviour
{
    [Header("던지기")][SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _minThrowPower = 5f;
    [SerializeField] private float _maxThrowPower = 20f;
    [SerializeField] private float _maxChargeTime = 1.5f; // 최대 힘까지 걸리는 시간
    [SerializeField, Min(0f)] private float _leftThrowOffset = 0.35f;
    [SerializeField, Min(0f)] private float _releaseDelay = 0.22f;
    [SerializeField, Min(0f)] private float _throwDuration = 0.78f;

    private Animator _animator;
    private Coroutine _throwCoroutine;
    private static readonly int _throwHash = Animator.StringToHash("ThrowBomb");

    [Header("폭탄")][SerializeField] private int _maxBombs = 10;
    [SerializeField] private float _reloadTime = 2f;
    [SerializeField] private KeyCode _throwKey = KeyCode.G;
    [SerializeField] private KeyCode _reloadKey = KeyCode.B;

    private int _currentBombs;
    private float _reloadTimer;
    private bool _isReloading;
    private float _chargeTime;
    private bool _isCharging;

    public int CurrentBombs => _currentBombs;
    public int MaxBombs => _maxBombs;
    public bool IsReloading => _isReloading;
    public bool IsCharging => _isCharging;
    public event Action<int> OnBombCountChanged;

    // 0 ~ 1 사이의 충전 정도
    public float ChargeRatio => _isCharging ? Mathf.Clamp01(_chargeTime / Mathf.Max(0.01f, _maxChargeTime)) : 0f;

    private void Awake()
    {
        _currentBombs = _maxBombs;
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        OnBombCountChanged?.Invoke(_currentBombs);
    }

    private void Update()
    {
        if ((GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            || Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked)
        {
            CancelCharge();
            return;
        }

        UpdateReload();

        // B 키를 누르면 폭탄을 재장전한다.
        if (Input.GetKeyDown(_reloadKey))
        {
            TryReload();
        }

        HandleCharge();
    }

    private void HandleCharge()
    {
        // 폭탄이 없거나 재장전 중이면 충전을 시작하지 않는다.
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetKeyDown(_throwKey) && !overUI && !_isReloading && _throwCoroutine == null && _currentBombs > 0)
        {
            _isCharging = true;
            _chargeTime = 0f;
        }

        if (_isCharging)
        {
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, Mathf.Max(0.01f, _maxChargeTime));
        }

        // 버튼을 떼는 순간 모은 힘으로 던진다.
        if (Input.GetKeyUp(_throwKey) && _isCharging)
        {
            _isCharging = false;
            Throw();
        }
    }

    private void Throw()
    {
        if (_isReloading || _currentBombs <= 0)
        {
            return;
        }

        if (_firePoint == null || _bombPrefab == null)
        {
            Debug.LogWarning("[PlayerBombFire] _firePoint 또는 _bombPrefab이 연결되지 않았습니다.");
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        float chargeRatio = Mathf.Clamp01(_chargeTime / Mathf.Max(0.01f, _maxChargeTime));
        float throwPower = Mathf.Lerp(_minThrowPower, _maxThrowPower, chargeRatio);
        _chargeTime = 0f;

        if (_animator != null)
        {
            _animator.SetTrigger(_throwHash);
        }

        _throwCoroutine = StartCoroutine(ThrowCoroutine(mainCamera.transform.forward * throwPower));
    }

    private IEnumerator ThrowCoroutine(Vector3 impulse)
    {
        // G 키를 뗀 순간의 힘과 방향을 유지하고, 팔을 뻗는 시점에 발사한다.
        yield return new WaitForSeconds(_releaseDelay);
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            _throwCoroutine = null;
            yield break;
        }

        Vector3 spawnPosition = _firePoint.position - transform.right * _leftThrowOffset;
        Bomb bomb = LeanPool.Spawn(_bombPrefab, spawnPosition, Quaternion.identity);
        bomb.Launch(impulse, GetComponent<Collider>());
        _currentBombs--;
        OnBombCountChanged?.Invoke(_currentBombs);
        yield return new WaitForSeconds(Mathf.Max(0f, _throwDuration - _releaseDelay));
        _throwCoroutine = null;
    }

    private void TryReload()
    {
        // 이미 재장전 중이거나 폭탄이 가득 차 있으면 무시한다.
        if (_isReloading || _throwCoroutine != null || _currentBombs >= _maxBombs)
        {
            return;
        }

        // 충전 중이었다면 취소한다.
        _isCharging = false;
        _chargeTime = 0f;
        _isReloading = true;
        _reloadTimer = _reloadTime;
    }

    private void UpdateReload()
    {
        if (!_isReloading)
        {
            return;
        }

        _reloadTimer -= Time.deltaTime;
        if (_reloadTimer > 0f)
        {
            return;
        }

        _currentBombs = _maxBombs;
        _isReloading = false;
        OnBombCountChanged?.Invoke(_currentBombs);
    }

    private void CancelCharge()
    {
        _isCharging = false;
        _chargeTime = 0f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) CancelCharge();
    }

    private void OnDisable()
    {
        CancelCharge();
        StopAllCoroutines();
        _throwCoroutine = null;
        _isReloading = false;
        if (_animator != null)
        {
            _animator.ResetTrigger(_throwHash);
            int layer = _animator.GetLayerIndex("Bomb Left Arm");
            if (layer >= 0) _animator.Play("Empty", layer, 0f);
        }
    }

    private void OnValidate()
    {
        _minThrowPower = Mathf.Max(0f, _minThrowPower);
        _maxThrowPower = Mathf.Max(_minThrowPower, _maxThrowPower);
        _maxChargeTime = Mathf.Max(0.01f, _maxChargeTime);
        _throwDuration = Mathf.Max(_releaseDelay, _throwDuration);
    }
}
