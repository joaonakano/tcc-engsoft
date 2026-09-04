using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WipeControllerMenu : MonoBehaviour
{
    private Animator _animator;
    private Image _image;
    private readonly int _circleSizeID = Shader.PropertyToID("_Circle_Size");

    [SerializeField]
    private string sceneToLoad;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioSource backgroundAudioSource;

    [SerializeField]
    private AudioClip roundStartClip;

    [SerializeField]
    private AudioClip roundEndClip;

    public float circleSize = 0;

    void Start()
    {
        _animator = gameObject.GetComponent<Animator>();
        _image = gameObject.GetComponent<Image>();
    }

    void Update()
    {
        _image.materialForRendering.SetFloat(_circleSizeID, circleSize);
    }

    public void StartWipe()
    {
        _animator.SetTrigger("WipeIn");
    }

    private void PlayRoundEnd()
    {
        audioSource.clip = roundEndClip;
        audioSource.Play();
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
}
