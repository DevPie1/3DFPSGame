using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UI_Gold : MonoBehaviour
{
    [SerializeField] private PlayerGold _player;
    [SerializeField] private GoldPickupText _pickupTextPrefab;
    [SerializeField] private TextMeshProUGUI _pickupFeedback;
    private TextMeshProUGUI _text;
    private float _feedbackTime;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        if (_player == null) _player = FindFirstObjectByType<PlayerGold>();
    }

    private void OnEnable()
    {
        if (_text == null) _text = GetComponent<TextMeshProUGUI>();
        if (_player == null) { Refresh(0); return; }
        _player.OnGoldChanged += Refresh;
        _player.OnGoldCollected += ShowPickup;
        Refresh(_player.Gold);
        if (_pickupFeedback != null) _pickupFeedback.alpha = 0f;
    }

    private void OnDisable()
    {
        if (_player == null) return;
        _player.OnGoldChanged -= Refresh;
        _player.OnGoldCollected -= ShowPickup;
    }

    private void Refresh(int total) => _text.text = $"골드 {total}";

    private void ShowPickup(int amount)
    {
        if (_pickupTextPrefab != null)
        {
            Collider body = _player.GetComponent<Collider>();
            Vector3 position = body != null ? new Vector3(body.bounds.center.x, body.bounds.max.y + 0.2f, body.bounds.center.z)
                : _player.transform.position + Vector3.up;
            GoldPickupText popup = Instantiate(_pickupTextPrefab, position, Quaternion.identity);
            popup.Show(amount);
        }
        // 전신 1인칭에서도 획득 숫자를 볼 수 있도록 골드 HUD 옆에 함께 표시한다.
        if (_pickupFeedback != null)
        {
            _pickupFeedback.text = $"+{amount}";
            _pickupFeedback.alpha = 1f;
            _feedbackTime = 0.8f;
        }
    }

    private void Update()
    {
        if (_pickupFeedback == null || _feedbackTime <= 0f) return;
        _feedbackTime = Mathf.Max(0f, _feedbackTime - Time.deltaTime);
        _pickupFeedback.alpha = _feedbackTime / 0.8f;
    }
}
