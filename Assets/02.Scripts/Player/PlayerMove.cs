using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 7f;

    private void Update()
    {
        // 1. 키보드 입력을 받는다. (-1, 0, 1 중 하나)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 2. 입력으로 방향을 만든다. y는 0 (위아래로는 움직이지 않는다)
        Vector3 direction = new Vector3(h, 0f, v);
        direction.Normalize();
        direction = transform.TransformDirection(direction);

        // 3. 새 위치 = 현재 위치 + 방향 × 속도 × 시간
        transform.position += direction * _moveSpeed * Time.deltaTime;
    }
}
