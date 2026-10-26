using UnityEngine;

public class CursorManager : MonoBehaviour
{
    private void Awake()
    {
        LockCursor();
    }

    private void Update()
    {
        // GameManager가 있으면 게임 중일 때만 커서를 조작한다.
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 왼쪽 Alt를 누르고 있는 동안만 커서를 보여 준다.
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            UnlockCursor();
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            LockCursor();
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 화면 가운데에 고정
        Cursor.visible = false; // 커서 숨기기
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
