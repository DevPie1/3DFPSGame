using UnityEngine;
using UnityEngine.UI;

// 탄창 상태를 화면에 표시한다. Canvas 아래의 Text에 이 컴포넌트를 붙인다.
public class AmmoHud : MonoBehaviour
{
    [SerializeField] private PlayerGunFire _gun;
    [SerializeField] private Text _ammoText;

    private void Awake()
    {
        if (_gun == null)
        {
            _gun = FindFirstObjectByType<PlayerGunFire>();
        }
    }

    private void Update()
    {
        if (_gun == null || _ammoText == null)
        {
            return;
        }

        // 기본 폰트에 한글이 없어 영문으로 표시한다.
        _ammoText.text = _gun.IsReloading
            ? "RELOADING..."
            : $"AMMO {_gun.CurrentAmmo} / INF";
    }
}
