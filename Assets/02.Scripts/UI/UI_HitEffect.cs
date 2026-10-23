using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.06f;
    [SerializeField] private float _fadeTime = 0.3f;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.raycastTarget = false;
        SetAlpha(0f);
        _image.enabled = false;

        // Width/Height가 0이어도 부모 Canvas 전체를 채우게 한다.
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 피격 순간에 호출한다. 잠깐 밝게 보였다가 서서히 사라진다.
    public void Flash(float alpha)
    {
        StopAllCoroutines();

        SetAlpha(alpha);
        _image.enabled = true;
        StartCoroutine(Flash_Coroutine());
    }

    private IEnumerator Flash_Coroutine()
    {
        // 1. 피격 순간의 밝기를 잠깐 유지한다.
        yield return new WaitForSeconds(_flashTime);

        // 2. 현재 알파에서 0까지 서서히 줄인다.
        float elapsedTime = 0f;
        float startAlpha = _image.color.a;

        while (elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / _fadeTime);
            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(0f);
        _image.enabled = false;
    }

    // 페이드 아웃만 필요할 때 사용한다.
    public void Fade(float alpha)
    {
        StopAllCoroutines();

        SetAlpha(alpha);
        _image.enabled = true;
        StartCoroutine(Fade_Coroutine());
    }

    private IEnumerator Fade_Coroutine()
    {
        float elapsedTime = 0f;
        float startAlpha = _image.color.a;

        while (elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / _fadeTime);
            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(0f);
        _image.enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = Mathf.Clamp01(alpha);
        _image.color = color;
    }
}
