using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum WeaponMode
{
    Rifle,
    Sniper
}

// 무기 상태를 모아 알린다. UI는 입력이나 개별 무기 구현을 알 필요가 없다.
[RequireComponent(typeof(PlayerGunFire), typeof(PlayerBombFire))]
public class PlayerWeapons : MonoBehaviour
{
    [SerializeField] private PlayerGunFire _gun;
    [SerializeField] private PlayerBombFire _bomb;
    [SerializeField] private Camera _aimCamera;
    [SerializeField, Range(5f, 60f)] private float _sniperFieldOfView = 25f;
    [SerializeField, Min(1f)] private float _zoomSpeed = 180f;

    private float _normalFieldOfView;

    public WeaponMode Mode { get; private set; }
    public int CurrentAmmo => _gun != null ? _gun.CurrentBulletCount : 0;
    public int MaxAmmo => _gun != null ? _gun.MaxBulletCount : 0;
    public int RemainingBombs => _bomb != null ? _bomb.CurrentBombs : 0;

    public event Action<WeaponMode> OnModeChanged;
    public event Action<int, int> OnAmmoChanged;
    public event Action<int> OnBombCountChanged;

    private void Awake()
    {
        if (_gun == null) _gun = GetComponent<PlayerGunFire>();
        if (_bomb == null) _bomb = GetComponent<PlayerBombFire>();
        if (_aimCamera == null) _aimCamera = Camera.main;
        if (_aimCamera != null) _normalFieldOfView = _aimCamera.fieldOfView;
    }

    private void OnEnable()
    {
        _gun.OnAmmoChanged += HandleAmmoChanged;
        _bomb.OnBombCountChanged += HandleBombCountChanged;
    }

    private void Start()
    {
        // 검 시연처럼 발사 컴포넌트가 꺼져 있어도 모든 Awake 이후 초기 상태를 알린다.
        OnModeChanged?.Invoke(Mode);
        OnAmmoChanged?.Invoke(CurrentAmmo, MaxAmmo);
        OnBombCountChanged?.Invoke(RemainingBombs);
    }

    private void OnDisable()
    {
        _gun.OnAmmoChanged -= HandleAmmoChanged;
        _bomb.OnBombCountChanged -= HandleBombCountChanged;
        ResetScope();
    }

    private void Update()
    {
        bool canAim = Application.isFocused && Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked
            && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing)
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        SetScoped(canAim && Input.GetMouseButton(1));

        if (_aimCamera != null)
        {
            float target = Mode == WeaponMode.Sniper ? _sniperFieldOfView : _normalFieldOfView;
            _aimCamera.fieldOfView = Mathf.MoveTowards(_aimCamera.fieldOfView, target, _zoomSpeed * Time.unscaledDeltaTime);
        }
    }

    public void SetScoped(bool scoped)
    {
        WeaponMode next = scoped && _aimCamera != null ? WeaponMode.Sniper : WeaponMode.Rifle;
        if (Mode == next) return;
        Mode = next;
        OnModeChanged?.Invoke(Mode);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ResetScope();
    }

    private void ResetScope()
    {
        SetScoped(false);
        if (_aimCamera != null) _aimCamera.fieldOfView = _normalFieldOfView;
    }

    private void HandleAmmoChanged(int current, int maximum) => OnAmmoChanged?.Invoke(current, maximum);
    private void HandleBombCountChanged(int count) => OnBombCountChanged?.Invoke(count);
}
