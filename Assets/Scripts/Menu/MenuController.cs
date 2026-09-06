using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Animator wipeAnimator;

    [SerializeField]
    private WipeController wipeController;

    [SerializeField]
    private AudioSource backgroundAudioSource;

    [SerializeField]
    private string sceneToLoad;

    private void Start()
    {
        wipeAnimator.Play("IdleOut", 0, 1f);
    }

    public void StartWipeIn()
    {
        wipeController.StartWipeIn();
        FadeOutSong();
    }

    private void FadeOutSong()
    {
        StartCoroutine(FadeOutAudio(backgroundAudioSource, 1f));
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        if (source == null || !source.isPlaying) yield break;

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        // Garante que o volume zerou, para o som e restaura o volume original
        source.volume = 0f;
        source.Stop();
        source.volume = startVolume;
    }

    private void ChangeScene()
    {
        // Chama a troca de cena assim que o fade e o áudio terminam
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    public void OnPlayPressed()
    {
        ChangeScene();
    }
}
