using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _menuMusicSource;

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }

    private void Awake()
    {
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        ApplyVolume();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        SaveToPrefs();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        SaveToPrefs();
    }

    private void ApplyVolume()
    {
        _musicSource.volume = MusicVolume;
        _menuMusicSource.volume = MusicVolume;
        _sfxSource.volume = SFXVolume;
    }

    private void SaveToPrefs()
    {
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.Save();
    }
}
