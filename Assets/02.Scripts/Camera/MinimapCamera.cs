using UnityEngine;

// 미니맵 카메라: 대상의 머리 위에서 아래를 내려다보며 따라간다.
public class MinimapCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;    // 따라갈 대상 (Player)
    [SerializeField] private float _height = 10f;  // 대상보다 얼마나 위에 있을지 (m)

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        // 1. 대상의 머리 위로 옮긴다.
        transform.position = _target.position + Vector3.up * _height;

        // 2. 아래를 보되, 대상이 보는 쪽이 미니맵의 위쪽이 되게 돌린다.
        transform.eulerAngles = new Vector3(90f, _target.eulerAngles.y, 0f);
    }
}
