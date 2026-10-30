using TMPro;
using UnityEngine;

// 월드 공간의 획득 숫자는 위로 떠오르며 사라진다.
public class GoldPickupText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField, Min(0.1f)] private float _lifetime = 0.8f;
    [SerializeField, Min(0f)] private float _rise = 0.6f;
    private Vector3 _start;
    private float _elapsed;
    private Camera _camera;

    private void OnEnable()
    {
        _start = transform.position;
        _elapsed = 0f;
        _camera = Camera.main;
    }

    public void Show(int amount)
    {
        if (_text == null) _text = GetComponentInChildren<TMP_Text>();
        _text.text = $"+{amount}";
        _start = transform.position;
        _elapsed = 0f;
        _camera = Camera.main;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _lifetime);
        transform.position = _start + Vector3.up * (_rise * progress);
        if (_text != null) _text.alpha = 1f - progress;
        if (_camera != null) transform.rotation = _camera.transform.rotation;
        if (progress >= 1f) Destroy(gameObject);
    }
}
