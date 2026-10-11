using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WipeController _wipeController;
    [SerializeField] private AudioSource _backgroundAudioSource;
    [SerializeField] private CursorManager _cursorManager;
    [SerializeField] private AnimationManager _animationManager;

    [SerializeField] private string sceneToLoad;

    private void Start()
    {
        if (_wipeController == null)
            Debug.LogError("MenuController: WipeController não foi configurado.");
        if (_cursorManager == null)
            Debug.LogError("MenuController: CursorManager não foi configurado.");
        if (_backgroundAudioSource == null)
            Debug.LogError("MenuController: BackgroundAudioSource não foi configurado.");
        
        Time.timeScale = 1f;
        _cursorManager.HabilitarCursor(true);

        _wipeController.StartWipeOut(
            onHalf: () => {
                _animationManager.PlayStartAnimation();
                _backgroundAudioSource.Play();
            },
            playSfx: false
            );
    }

    private void FadeOutSong()
    {
        StartCoroutine(FadeOutAudio(_backgroundAudioSource, 2f));
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
        FadeOutSong();
        _wipeController.StartWipeIn(() => { ChangeScene(); });
    }
}
