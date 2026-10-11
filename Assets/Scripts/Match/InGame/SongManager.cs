using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SongManager : MonoBehaviour
{
    [Header("Música")]
    public AudioSource musicSource;

    [Header("Configuração")]
    public float songDelay = 2f;

    public double SongStartTime { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool HasFinished { get; private set; }
    public bool IsPaused { get; private set; }

    public event Action OnPaused;
    public event Action OnResumed;

    private double _pauseStartDspTime;

    // O menu só pode abrir durante a partida (depois do delay inicial e antes do fim)
    public bool CanPause => IsPlaying && !IsPaused && !HasFinished && AudioSettings.dspTime >= SongStartTime;

    private void Start()
    {
        StartSong();
    }

    private void Update()
    {
        if (!IsPlaying || IsPaused) return;

        if (GetSongTime() >= musicSource.clip.length)
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
        IsPaused = false;
        HasFinished = false;
    }

    public void PauseSong()
    {
        if (!CanPause) return;

        _pauseStartDspTime = AudioSettings.dspTime;
        IsPaused = true;

        musicSource.Pause();
        OnPaused?.Invoke();
    }

    public void UnpauseSong()
    {
        if (!IsPaused || HasFinished) return;

        SongStartTime += AudioSettings.dspTime - _pauseStartDspTime;

        IsPaused = false;
        musicSource.UnPause();
        OnResumed?.Invoke();
    }


    public void FinishSong()
    {
        if (HasFinished) return;

        IsPlaying = false;
        IsPaused = false;
        HasFinished = true;

        musicSource.Stop();
    }

    public double GetSongTime()
    {
        if (IsPaused) return _pauseStartDspTime - SongStartTime;

        return AudioSettings.dspTime - SongStartTime;
    }
}
