using UnityEngine;

// 실제 오른손에 붙인 총 모델과 총구를 무기 모드 알림으로 교체한다.
[RequireComponent(typeof(PlayerWeapons), typeof(PlayerGunFire))]
public class PlayerWeaponModels : MonoBehaviour
{
    [SerializeField] private PlayerWeapons _weapons;
    [SerializeField] private PlayerGunFire _gun;
    [SerializeField] private EasyFpsWeaponFeedback _feedback;
    [SerializeField] private GameObject _futureRifle;
    [SerializeField] private GameObject _vssRifle;
    [SerializeField] private Transform _futureMuzzle;
    [SerializeField] private Transform _vssMuzzle;
    [SerializeField] private Transform _fallbackMuzzle;

    private void Awake()
    {
        if (_weapons == null) _weapons = GetComponent<PlayerWeapons>();
        if (_gun == null) _gun = GetComponent<PlayerGunFire>();
        if (_feedback == null) _feedback = GetComponent<EasyFpsWeaponFeedback>();
    }

    private void OnEnable()
    {
        if (_weapons == null) _weapons = GetComponent<PlayerWeapons>();
        _weapons.OnModeChanged += Refresh;
        Refresh(_weapons.Mode);
    }

    private void Start() => Refresh(_weapons.Mode);

    private void OnDisable()
    {
        if (_weapons != null) _weapons.OnModeChanged -= Refresh;
    }

    private void Refresh(WeaponMode mode)
    {
        if (_futureRifle != null) _futureRifle.SetActive(mode == WeaponMode.Normal);
        if (_vssRifle != null) _vssRifle.SetActive(mode == WeaponMode.Sniper);
        Transform muzzle = mode == WeaponMode.Normal ? _futureMuzzle
            : mode == WeaponMode.Sniper ? _vssMuzzle : _fallbackMuzzle;
        if (_gun != null) _gun.SetMuzzle(muzzle);
        if (_feedback != null) _feedback.SetMuzzle(muzzle);
    }
}
