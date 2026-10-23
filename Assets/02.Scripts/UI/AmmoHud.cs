using UnityEngine;
using UnityEngine.UI;


// 총 탄창과 폭탄 개수를 화면에 표시한다. Canvas 아래의 Text에 이 컴포넌트를 붙인다.
public class AmmoHud : MonoBehaviour
{
    [SerializeField] private PlayerGunFire _gun;
    [SerializeField] private PlayerBombFire _bomb;
    [SerializeField] private Text _ammoText;

    private void Awake()
    {
        if (_gun == null)
        {
            _gun = FindFirstObjectByType<PlayerGunFire>();
        }

        if (_bomb == null)
        {
            _bomb = FindFirstObjectByType<PlayerBombFire>();
        }

        SetupText();
    }

    private void SetupText()
    {
        if (_ammoText == null)
        {
            return;
        }

        // 두 줄(총/폭탄)이 잘리거나 줄바꿈되지 않게 한다.
        _ammoText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _ammoText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = _ammoText.rectTransform;
        if (rect.sizeDelta.y < 90f)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 90f);
        }
    }
}
