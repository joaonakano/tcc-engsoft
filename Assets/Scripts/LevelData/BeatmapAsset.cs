using System;
using UnityEngine;

public enum LaneType
{
    Left = 0,
    Right = 1
}

/// <summary>
/// Trecho de andamento constante. Uma música com BPM crescente vira vários segmentos.
/// </summary>
[Serializable]
public struct TempoSegment
{
    public double startBeat;       // beat (semínima) em que o trecho começa
    public double startTime;       // segundo em que o trecho começa
    public double secondsPerBeat;  // duração de 1 beat neste trecho

    public double Bpm => 60.0 / secondsPerBeat;
}

/// <summary>
/// Beatmap já "assado" a partir do .mid (ver BeatmapBaker no Editor).
/// Em runtime NÃO existe parse de MIDI: é só ler arrays prontos, que carregam junto com a cena.
/// </summary>
public class BeatmapAsset : ScriptableObject
{
    [Header("Música")]
    public string songTitle;
    public AudioClip clip;

    [Tooltip("Segundos. + = as notas acontecem mais tarde no áudio (use se o áudio tiver silêncio inicial / padding de MP3).")]
    public float songOffset = 0f;

    [Header("Notas (ordenadas por tempo) - NAO EDITAR A MAO")]
    public double[] noteTimes = Array.Empty<double>();  // segundos, já com tempo de BPM variável aplicado
    public double[] noteBeats = Array.Empty<double>();  // posição em beats (útil para scroll proporcional ao BPM)
    public byte[] noteLanes = Array.Empty<byte>();      // LaneType

    [Header("Mapa de tempo")]
    public TempoSegment[] tempo = { new TempoSegment { startBeat = 0, startTime = 0, secondsPerBeat = 0.5 } };

    public int NoteCount => noteTimes != null ? noteTimes.Length : 0;

    public LaneType LaneOf(int index) => (LaneType)noteLanes[index];

    // -------------------------------------------------------------
    // Conversões segundos <-> beats (respeitam mudanças de BPM)
    // Valores antes do início extrapolam com o primeiro BPM.
    // -------------------------------------------------------------

    public double SecondsToBeats(double seconds)
    {
        TempoSegment s = tempo[SegmentByTime(seconds)];
        return s.startBeat + (seconds - s.startTime) / s.secondsPerBeat;
    }

    public double BeatsToSeconds(double beats)
    {
        TempoSegment s = tempo[SegmentByBeat(beats)];
        return s.startTime + (beats - s.startBeat) * s.secondsPerBeat;
    }

    public double BpmAtSeconds(double seconds) => tempo[SegmentByTime(seconds)].Bpm;

    private int SegmentByTime(double seconds)
    {
        int lo = 0, hi = tempo.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (seconds >= tempo[mid].startTime) lo = mid; else hi = mid - 1;
        }
        return lo;
    }

    private int SegmentByBeat(double beats)
    {
        int lo = 0, hi = tempo.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (beats >= tempo[mid].startBeat) lo = mid; else hi = mid - 1;
        }
        return lo;
    }
}