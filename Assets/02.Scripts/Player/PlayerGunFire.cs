using UnityEngine;


// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
// 탄창이 비면 발사할 수 없고, R 키로 재장전한다. (예비 탄약은 무한)
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _range = 100f; // 사거리
    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; // 발사 속도
    private float _fireTimer = 0f;

    [Header("탄창")]
    [SerializeField] private int _magazineSize = 50;
    [SerializeField] private float _reloadTime = 1.5f;

    private int _currentAmmo;
    private float _reloadTimer;
    private bool _isReloading;

    public int CurrentAmmo => _currentAmmo;
    public int MagazineSize => _magazineSize;
    public bool IsReloading => _isReloading;

    private void Awake()
    {
        _currentAmmo = _magazineSize;
    }

    private void Update()
    {
        UpdateReload();

        // R 키를 누르면 재장전한다.
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        // 재장전 중이거나 탄약이 없으면 발사하지 않는다.
        if (_isReloading || _currentAmmo <= 0)
        {
            return;
        }

        _currentAmmo--;
        _fireTimer = _fireInterval;
        Shoot();
    }

    private void TryReload()
    {
        // 이미 재장전 중이거나 탄창이 가득 차 있으면 무시한다.
        if (_isReloading || _currentAmmo >= _magazineSize)
        {
            return;
        }

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

        _currentAmmo = _magazineSize;
        _isReloading = false;
    }

    private void Shoot()
    {
        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체

        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 대미지: {_damage}");

            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();
        }
    }
}
