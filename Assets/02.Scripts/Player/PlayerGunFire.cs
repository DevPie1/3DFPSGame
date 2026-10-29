using System.Collections;
using System;
using UnityEngine;
using UnityEngine.EventSystems;


// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("스탯")][SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;
    [SerializeField] private float _range = 100f; // 사거리
    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; // 발사 속도

    [Header("발사 레이저")][SerializeField] private Material _laserMaterial;
    [SerializeField] private Transform _muzzle;
    [SerializeField, Min(0.01f)] private float _laserDuration = 0.06f;
    [SerializeField, Min(0.001f)] private float _laserWidth = 0.025f;

    private LineRenderer _laser;
    private Camera _mainCamera;
    private Animator _animator;
    private float _laserTimer;

    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadCoroutine = null;

    public int CurrentBulletCount => _bulletCount;
    public int MaxBulletCount => _maxBulletCount;
    public event Action<int, int> OnAmmoChanged;
    public event Action OnShot;


    private void Awake()
    {
        _bulletCount = _maxBulletCount;
        _mainCamera = Camera.main;
        _animator = GetComponentInChildren<Animator>();

        if (_laserMaterial == null)
        {
            Debug.LogWarning("[PlayerGunFire] 레이저 머티리얼을 연결해 주세요.", this);
            return;
        }

        GameObject laserObject = new GameObject("GunLaser");
        laserObject.layer = gameObject.layer;
        laserObject.transform.SetParent(transform, false);
        _laser = laserObject.AddComponent<LineRenderer>();
        _laser.sharedMaterial = _laserMaterial;
        _laser.useWorldSpace = true;
        _laser.positionCount = 2;
        _laser.startWidth = _laserWidth;
        _laser.endWidth = _laserWidth;
        _laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _laser.receiveShadows = false;
        _laser.enabled = false;
    }

    private void Start()
    {
        OnAmmoChanged?.Invoke(_bulletCount, _maxBulletCount);
    }


    private void Update()
    {
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;
        _laserTimer -= Time.deltaTime;
        if (_laser != null && _laserTimer <= 0f)
        {
            _laser.enabled = false;
        }

        // 1. 마우스 왼쪽 버튼 입력 받기
        // 커서가 잠겨 있고, 마우스가 UI 위에 있지 않을 때만 쏜다.
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool canShoot = Cursor.lockState == CursorLockMode.Locked && !isPointerOverUI;

        if (Input.GetButton("Fire1") && canShoot && _fireTimer <= 0f && _bulletCount > 0)
        {
            _bulletCount--;
            OnAmmoChanged?.Invoke(_bulletCount, _maxBulletCount);
            _fireTimer = _fireInterval;
            Shoot();

            // 서 있을 때만 총 쏘는 동작을 재생한다.
            if (_animator != null && _animator.GetFloat("MoveMotion") < 0.1f)
            {
                _animator.SetTrigger("Attack");
            }

            // 이 방식은: 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소
            // StopAllCoroutines();

            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                Debug.Log("재장전 취소!");
                _reloadCoroutine = null;
            }
        }

        // 2. R키 누르면 총알 재장전 시작
        if (Input.GetKeyDown(KeyCode.R) && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작!");

        yield return new WaitForSeconds(_reloadingTime);

        _bulletCount = _maxBulletCount;
        OnAmmoChanged?.Invoke(_bulletCount, _maxBulletCount);

        Debug.Log("재장전 완료!");
        _reloadCoroutine = null;
    }


    private void OnDisable()
    {
        StopAllCoroutines();
        _reloadCoroutine = null;
        if (_laser != null) _laser.enabled = false;
    }

    private void OnDestroy()
    {
        if (_laser != null) Destroy(_laser.gameObject);
    }

    private void Shoot()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        Transform cameraTransform = _mainCamera.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        bool isHit = Physics.Raycast(ray, out RaycastHit hitInfo, _range);
        Vector3 end = isHit ? hitInfo.point : ray.GetPoint(_range);
        ShowLaser(cameraTransform, end);
        OnShot?.Invoke();

        if (!isHit) return;
        if (_hitEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            effect.Play();
        }

        hitInfo.collider.GetComponentInParent<IDamageable>()?.TakeDamage(_damage);
    }

    private void ShowLaser(Transform cameraTransform, Vector3 end)
    {
        if (_laser == null) return;
        // 총구가 없으면 화면 오른쪽 아래에서 시작해 선이 시선과 겹치지 않게 한다.
        Vector3 start = _muzzle != null ? _muzzle.position
            : cameraTransform.position + cameraTransform.forward * 0.5f
              + cameraTransform.right * 0.15f - cameraTransform.up * 0.12f;
        _laser.SetPosition(0, start);
        _laser.SetPosition(1, end);
        _laser.enabled = true;
        _laserTimer = _laserDuration;
    }
}
