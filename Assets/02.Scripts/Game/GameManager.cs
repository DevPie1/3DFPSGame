using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;


    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private IEnumerator StartSequence_Coroutine()
    {
        if (_stateText == null)
        {
            Debug.LogError("[GameManager] State Text가 연결되지 않았습니다.");
            _state = GameState.Playing;
            yield break;
        }

        _stateText.gameObject.SetActive(true);
        _stateText.color = Color.white;
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);

        _state = GameState.Playing;
    }

    public void GameOver()
    {
        if (_state == GameState.GameOver)
        {
            return;
        }

        _state = GameState.GameOver;

        if (_stateText == null)
        {
            Debug.LogError("[GameManager] State Text가 연결되지 않아 게임오버 문구를 표시할 수 없습니다.");
            return;
        }

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임오버";
        _stateText.color = Color.red;
    }
}
