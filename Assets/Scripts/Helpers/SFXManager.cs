using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    public static SFXManager Instance;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (_audioSource == null)
        {
            Debug.LogError("AudioSource não foi configurado no SFXManager.");
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (_audioSource == null || clip == null) return;
        _audioSource.PlayOneShot(clip);
    }
}
