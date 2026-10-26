using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 미니맵 UI: 지역 이름을 보여 주고, 버튼으로 미니맵 카메라의 Size를 바꿔 확대·축소한다.
public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private Camera _minimapCamera;         // 미니맵 카메라
    [SerializeField] private TextMeshProUGUI _mapNameText;  // 지역 이름 글자
    [SerializeField] private float _zoomStep = 2f;          // 버튼 한 번에 바뀌는 Size
    [SerializeField] private float _minSize = 4f;           // 가장 크게 확대했을 때의 Size
    [SerializeField] private float _maxSize = 16f;          // 가장 작게 축소했을 때의 Size

    private void Awake()
    {
        if (_minimapCamera == null)
        {
            MinimapCamera minimap = FindFirstObjectByType<MinimapCamera>();
            if (minimap != null)
            {
                _minimapCamera = minimap.GetComponent<Camera>();
            }
        }

        if (_mapNameText != null)
        {
            // 지금 씬의 이름을 지역 이름으로 보여 준다.
            _mapNameText.text = SceneManager.GetActiveScene().name;
        }
    }

    // 확대: Size를 줄이면 좁은 곳이 미니맵에 꽉 차서 크게 보인다.
    public void ZoomIn()
    {
        if (_minimapCamera == null)
        {
            return;
        }

        float size = _minimapCamera.orthographicSize - _zoomStep;
        _minimapCamera.orthographicSize = Mathf.Max(size, _minSize);
    }

    // 축소: Size를 늘리면 넓은 곳이 미니맵에 들어와 작게 보인다.
    public void ZoomOut()
    {
        if (_minimapCamera == null)
        {
            return;
        }

        float size = _minimapCamera.orthographicSize + _zoomStep;
        _minimapCamera.orthographicSize = Mathf.Min(size, _maxSize);
    }
}
