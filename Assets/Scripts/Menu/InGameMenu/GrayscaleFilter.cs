using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GrayscaleFilter : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    private Coroutine routine;

    private void Awake()
    {
        if (volume != null) volume.weight = 0f;
    }

    public void Show() => FadeTo(1f, fadeInDuration);
    public void Hide() => FadeTo(0f, fadeOutDuration);

    private void FadeTo(float target, float duration)
    {
        if (volume == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Fade(target, duration));
    }

    private IEnumerator Fade(float to, float duration)
    {
        float from = volume.weight;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            volume.weight = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        volume.weight = to;
        routine = null;
    }
}