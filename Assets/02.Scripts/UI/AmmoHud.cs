using UnityEngine;
using UnityEngine.UI;


// Update에서 수치를 읽지 않고 PlayerWeapons의 변경 알림을 구독한다.
public class AmmoHud : MonoBehaviour
{
    [SerializeField] private PlayerWeapons _weapons;
    [SerializeField] private Text _ammoText;
    [SerializeField] private Text _bombText;
    [SerializeField] private Image _weaponImage;
    [SerializeField] private Sprite _rifleSprite;
    [SerializeField] private Sprite _sniperSprite;
    [SerializeField] private Sprite _meleeSprite;
    [SerializeField] private UI_ScopeOverlay _scopeOverlay;
    [SerializeField] private Graphic _normalCrosshair;

    private void Awake()
    {
        if (_weapons == null) _weapons = FindFirstObjectByType<PlayerWeapons>();
    }

    private void OnEnable()
    {
        if (_weapons == null) return;
        _weapons.OnModeChanged += RefreshMode;
        _weapons.OnScopeChanged += RefreshScope;
        _weapons.OnAmmoChanged += RefreshAmmo;
        _weapons.OnBombCountChanged += RefreshBombs;
        RefreshMode(_weapons.Mode);
        RefreshScope(_weapons.IsScoped);
        RefreshAmmo(_weapons.CurrentAmmo, _weapons.MaxAmmo);
        RefreshBombs(_weapons.RemainingBombs);
    }

    private void OnDisable()
    {
        if (_weapons != null)
        {
            _weapons.OnModeChanged -= RefreshMode;
            _weapons.OnScopeChanged -= RefreshScope;
            _weapons.OnAmmoChanged -= RefreshAmmo;
            _weapons.OnBombCountChanged -= RefreshBombs;
        }
        if (_scopeOverlay != null) _scopeOverlay.enabled = false;
        if (_normalCrosshair != null) _normalCrosshair.enabled = true;
    }

    private void RefreshMode(WeaponMode mode)
    {
        if (_weaponImage == null) return;
        switch (mode)
        {
            case WeaponMode.Melee: _weaponImage.sprite = _meleeSprite; break;
            case WeaponMode.Sniper: _weaponImage.sprite = _sniperSprite; break;
            default: _weaponImage.sprite = _rifleSprite; break;
        }
    }

    private void RefreshScope(bool scoped)
    {
        if (_scopeOverlay != null) _scopeOverlay.enabled = scoped;
        if (_normalCrosshair != null) _normalCrosshair.enabled = !scoped;
    }

    private void RefreshAmmo(int current, int maximum)
    {
        if (_ammoText != null) _ammoText.text = $"{current} / {maximum}";
    }

    private void RefreshBombs(int count)
    {
        if (_bombText != null) _bombText.text = $"폭탄 {count}";
    }
}
