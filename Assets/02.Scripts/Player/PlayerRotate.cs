using UnityEngine;

namespace _02.Scripts.Player
{
    public class PlayerRotate : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 200f;

        private float _accumulationX = 0f;

        private void Update()
        {
            // 커서가 풀려 있으면(UI를 누르는 중이면) 시선을 돌리지 않는다.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X");
            _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;

            transform.eulerAngles = new Vector3(0f, _accumulationX, 0f);
        }
    }
}
