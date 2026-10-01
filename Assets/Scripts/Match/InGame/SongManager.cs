using UnityEngine;

public class SongManager : MonoBehaviour
{
    [Header("Música")]
    public AudioSource musicSource;

    [Header("Configuração")]
    public float songDelay = 2f;

    public double SongStartTime { get; private set; }

    public bool IsPlaying { get; private set; }

    public bool HasFinished { get; private set; }

    private void Start()
    {
        StartSong();
    }

    private void Update()
    {
        if (!IsPlaying) return;

        double currentSongTime = GetSongTime();

        if (currentSongTime >= musicSource.clip.length)
        {
            FinishSong();
        }
    }

    private void StartSong()
    {
        if (musicSource == null)
        {
            Debug.LogError("SongManager: Music Source não foi configurado.");
            return;
        }

        SongStartTime = AudioSettings.dspTime + songDelay;

        musicSource.PlayScheduled(SongStartTime);

        IsPlaying = true;
        HasFinished = false;
    }

    public void FinishSong()
    {
        IsPlaying = false;
        HasFinished = true;

        musicSource.Stop();
    }

    public double GetSongTime()
    {
        return AudioSettings.dspTime - SongStartTime;
    }
}
