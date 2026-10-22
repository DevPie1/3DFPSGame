using System;
using System.Collections;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
    // 매 초마다 "1초"-> "2초"를 센 다음에 나로호 발사 로그 출력

    private void Start()
    {
        Debug.Log("시작");
        StartCoroutine(Countdown_Corutine());
    }

    private IEnumerator Countdown_Corutine()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("1초");
        yield return new WaitForSeconds(1f);
        Debug.Log("2초");
        yield return new WaitForSeconds(1f);
        Debug.Log("3초");
    }
}
