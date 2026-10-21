using UnityEngine;
using Lean.Pool;

// 마우스 오른쪽 버튼을 누르고 있는 동안 힘을 모았다가, 떼면 시선 방향으로 수류탄을 던진다.
// 폭탄은 최대 10개까지 들 수 있고, G 키로 재장전한다. (예비 폭탄은 무한)
public class PlayerBombFire : MonoBehaviour
{
    [Header("던지기")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _minThrowPower = 5f;
    [SerializeField] private float _maxThrowPower = 20f;
    [SerializeField] private float _maxChargeTime = 1.5f; // 최대 힘까지 걸리는 시간

    [Header("폭탄")]
    [SerializeField] private int _maxBombs = 10;
    [SerializeField] private float _reloadTime = 2f;
    [SerializeField] private KeyCode _reloadKey = KeyCode.G;

    private int _currentBombs;
    private float _reloadTimer;
    private bool _isReloading;
    private float _chargeTime;
    private bool _isCharging;

    public int CurrentBombs => _currentBombs;
    public int MaxBombs => _maxBombs;
    public bool IsReloading => _isReloading;
    public bool IsCharging => _isCharging;

    // 0 ~ 1 사이의 충전 정도
    public float ChargeRatio => _isCharging ? Mathf.Clamp01(_chargeTime / _maxChargeTime) : 0f;

    private void Awake()
    {
        _currentBombs = _maxBombs;
    }

    private void Update()
    {
        UpdateReload();

        // G 키를 누르면 재장전한다.
        if (Input.GetKeyDown(_reloadKey))
        {
            TryReload();
        }

        HandleCharge();
    }

    private void HandleCharge()
    {
        // 폭탄이 없거나 재장전 중이면 충전을 시작하지 않는다.
        if (Input.GetButtonDown("Fire2") && !_isReloading && _currentBombs > 0)
        {
            _isCharging = true;
            _chargeTime = 0f;
        }

        if (_isCharging)
        {
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, _maxChargeTime);
        }

        // 버튼을 떼는 순간 모은 힘으로 던진다.
        if (Input.GetButtonUp("Fire2") && _isCharging)
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

        _currentBombs--;

        float chargeRatio = Mathf.Clamp01(_chargeTime / _maxChargeTime);
        float throwPower = Mathf.Lerp(_minThrowPower, _maxThrowPower, chargeRatio);
        _chargeTime = 0f;

        // 발사 위치에서 수류탄을 생성하고 시선 방향으로 던진다.
        Bomb bomb = LeanPool.Spawn(_bombPrefab, _firePoint.position, Quaternion.identity);
        Rigidbody bombRigidbody = bomb.GetComponent<Rigidbody>();
        bombRigidbody.AddForce(Camera.main.transform.forward * throwPower, ForceMode.Impulse);
    }

    private void TryReload()
    {
        // 이미 재장전 중이거나 폭탄이 가득 차 있으면 무시한다.
        if (_isReloading || _currentBombs >= _maxBombs)
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
    }
}
