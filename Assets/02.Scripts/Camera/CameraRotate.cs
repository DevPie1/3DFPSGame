using System;
using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;
    [SerializeField] private float _accumulationX;
    [SerializeField] private float _accumulationY;

    private void Update()
    {
        // 커서가 풀려 있으면(UI를 누르는 중이면) 시선을 돌리지 않는다.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        // 1. 마우스 입력을 받는다
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 2. 회전 할 방향을 구한다.
        //Vector3 dir = new Vector3(-mouseY, mouseX, 0);
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;
        _accumulationY += -mouseY * _rotationSpeed * Time.deltaTime;

        //3. 누적하기전에 내 변수에서 먼저 -90 ~90도로 제한

        _accumulationY = Mathf.Clamp(_accumulationY, -90, 90);

        //3. 그 방향으로 회전 시킨다.
        // 새로운 각도 = 현재 각도 + 회전 속도 * 시간 ;
        transform.eulerAngles = new Vector3(_accumulationY, _accumulationX, 0);


        /*Vector3 rotation = transform.eulerAngles;
        rotation.x = Math.Clamp(rotation.x, -90, 90);
        transform.eulerAngles = rotation;*/
    }
}
