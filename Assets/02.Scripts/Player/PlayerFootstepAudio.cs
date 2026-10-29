using UnityEngine;

[RequireComponent(typeof(PlayerMove))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [SerializeField] private PlayerMove _movement;
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _walkClip;
    [SerializeField] private AudioClip _runClip;
    [SerializeField, Min(0.01f)] private float _minimumSpeed = 0.15f;

    private bool _hasFocus = true;

    private void Awake()
    {
        if (_movement == null) _movement = GetComponent<PlayerMove>();
    }

    private void LateUpdate()
    {
        bool moving = _hasFocus && Time.timeScale > 0f && _movement != null
            && _movement.isActiveAndEnabled
            && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing)
            && _movement.IsGrounded && _movement.HorizontalSpeed > _minimumSpeed;
        UpdatePlayback(moving, _movement != null && _movement.IsSprinting);
    }

    private void UpdatePlayback(bool moving, bool sprinting)
    {
        if (_source == null) return;
        AudioClip next = sprinting ? _runClip : _walkClip;
        if (!moving || next == null)
        {
            _source.Stop();
            return;
        }
        if (_source.clip != next)
        {
            _source.Stop();
            _source.clip = next;
        }
        if (!_source.isPlaying) _source.Play();
    }

    private void OnDisable()
    {
        if (_source != null) _source.Stop();
    }

    private void OnApplicationFocus(bool focused)
    {
        _hasFocus = focused;
        if (!focused && _source != null) _source.Stop();
    }
}
