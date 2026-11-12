using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Relógio central do jogo.
/// - AudioSettings.dspTime só avança em "degraus" do tamanho do buffer de áudio (~10-23 ms).
///   Usar ele cru faz a fruta andar em soquinhos e quantiza o julgamento do golpe.
///   Aqui o dspTime é interpolado com o relógio real => tempo contínuo, mas ancorado no áudio.
/// - O jogo só começa quando o clip está carregado (PrepareRoutine) e StartSong() é chamado.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SongManager : MonoBehaviour
{
    [Header("Música")]
    public AudioSource musicSource;
    [SerializeField] private BeatmapAsset beatmap;

    [Header("Configuração")]
    [Tooltip("Tempo entre StartSong() e o primeiro instante da música. Deve ser maior que o tempo de queda das frutas.")]
    public float songDelay = 2f;

    public BeatmapAsset Beatmap => beatmap;
    /// <summary>SongManager da cena atual (evita FindObjectOfType em quem só precisa saber se está pausado).</summary>
    public static SongManager Current { get; private set; }

    public bool IsReady { get; private set; }
    public double SongStartTime { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool HasFinished { get; private set; }
    public bool IsPaused { get; private set; }

    public event Action OnPaused;
    public event Action OnResumed;

    private const double ClockDriftPerSecond = 0.0002; // tolerância de deriva entre relógio de áudio e do sistema

    private double _clockOffset;
    private double _lastReal;
    private bool _clockInit;
    private double _pausedAudioTime;
    private double _chartOffset;

    public bool CanPause => IsPlaying && !IsPaused && !HasFinished && NowDsp() >= SongStartTime;

    private void Awake()
    {
        Current = this;

        // A fase escolhida na seleção define a música; sem seleção (Play direto na cena) vale a beatmap da cena
        if (GameSession.Level != null && GameSession.Level.beatmap != null)
            beatmap = GameSession.Level.beatmap;

        Audio.RegisterMusic(musicSource);
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    // =========================================================
    // RELÓGIO
    // =========================================================

    /// <summary>
    /// dspTime "contínuo". Pode ser chamado a qualquer momento (inclusive dentro de callbacks de input).
    /// Como cada amostra de dspTime é <= ao valor verdadeiro (quantização), usamos o MAIOR (dsp - real) visto.
    /// </summary>
    public double NowDsp()
    {
        double real = Time.realtimeSinceStartupAsDouble;
        double sample = AudioSettings.dspTime - real;

        if (!_clockInit)
        {
            _clockOffset = sample;
            _clockInit = true;
        }
        else if (sample > _clockOffset)
        {
            _clockOffset = sample;                       // amostra mais "fresca" do relógio de áudio
        }
        else if (sample < _clockOffset - 0.1)
        {
            _clockOffset = sample;                       // áudio ficou parado (perda de foco etc.): ressincroniza
        }
        else
        {
            _clockOffset -= (real - _lastReal) * ClockDriftPerSecond; // acompanha deriva lenta
        }

        _lastReal = real;
        return real + _clockOffset;
    }

    /// <summary>Tempo do ÁUDIO em segundos (0 = início do clip; negativo durante o songDelay).</summary>
    public double GetAudioTime()
    {
        return IsPaused ? _pausedAudioTime : NowDsp() - SongStartTime;
    }

    /// <summary>Tempo do CHART em segundos (já com o songOffset do beatmap). Use este para notas e julgamento.</summary>
    public double GetSongTime() => GetAudioTime() - _chartOffset;

    public double GetSongBeat() => beatmap != null ? beatmap.SecondsToBeats(GetSongTime()) : 0.0;
    public double GetCurrentBpm() => beatmap != null ? beatmap.BpmAtSeconds(GetSongTime()) : 0.0;

    // =========================================================
    // PRÉ-CARGA
    // =========================================================

    public IEnumerator PrepareRoutine()
    {
        IsReady = false;

        if (musicSource == null) { Debug.LogError("SongManager: Music Source não foi configurado."); yield break; }
        if (beatmap == null) { Debug.LogError("SongManager: Beatmap não foi configurado."); yield break; }

        AudioClip clip = beatmap.clip != null ? beatmap.clip : musicSource.clip;
        if (clip == null) { Debug.LogError("SongManager: nenhum AudioClip (beatmap.clip ou AudioSource.clip)."); yield break; }

        musicSource.playOnAwake = false;
        musicSource.clip = clip;
        _chartOffset = beatmap.songOffset;

        if (clip.loadType != AudioClipLoadType.DecompressOnLoad)
            Debug.LogWarning($"SongManager: '{clip.name}' não está em Decompress On Load. Decodificar durante o jogo causa engasgos e desync.");

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        while (clip.loadState == AudioDataLoadState.Loading)
            yield return null;

        if (clip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogError("SongManager: falha ao carregar os dados do AudioClip.");
            yield break;
        }

        AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
        Debug.Log($"[SongManager] Pronto. DSP buffer {bufferLength} x {numBuffers} @ {AudioSettings.outputSampleRate} Hz " +
                  $"(~{bufferLength * 1000f / AudioSettings.outputSampleRate:0.#} ms por buffer).");

        NowDsp(); // inicia o filtro do relógio
        IsReady = true;
    }

    // =========================================================
    // CONTROLE
    // =========================================================

    private void Update()
    {
        NowDsp(); // amostra o relógio todo frame, mantém o filtro afiado

        if (!IsPlaying || IsPaused) return;

        if (GetAudioTime() >= musicSource.clip.length)
            FinishSong();
    }

    public void StartSong()
    {
        if (IsPlaying || HasFinished) return;
        if (!IsReady) { Debug.LogError("SongManager: StartSong() chamado antes do PrepareRoutine() terminar."); return; }

        SongStartTime = NowDsp() + songDelay;
        musicSource.PlayScheduled(SongStartTime);

        IsPlaying = true;
        IsPaused = false;
        HasFinished = false;
    }

    public void PauseSong()
    {
        if (!CanPause) return;

        _pausedAudioTime = NowDsp() - SongStartTime;
        IsPaused = true;

        musicSource.Pause();
        OnPaused?.Invoke();
    }

    public void UnpauseSong()
    {
        if (!IsPaused || HasFinished) return;

        // Posição exata onde o áudio parou => reancora o relógio sem acumular erro a cada pause
        double position = musicSource.timeSamples / (double)musicSource.clip.frequency;
        musicSource.UnPause();
        SongStartTime = NowDsp() - position;

        IsPaused = false;
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
}