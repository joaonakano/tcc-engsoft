using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;

/// <summary>
/// Pasta: Assets/Editor/
/// Clique direito no .mid (ou .bytes) no Project > Beatmap > Bake MIDI -> BeatmapAsset.
/// Gera "<nome>_Beatmap.asset" ao lado do arquivo. Rode de novo sempre que mexer no chart no FL Studio.
/// Ao re-assar, o clip e o songOffset que voce ja configurou no asset sao preservados.
/// </summary>
public static class BeatmapBaker
{
    // Mapeamento de lanes (qualquer oitava)
    private const NoteName LeftNote = NoteName.C;
    private const NoteName RightNote = NoteName.D;

    [MenuItem("Assets/Beatmap/Bake MIDI -> BeatmapAsset")]
    private static void BakeSelected()
    {
        foreach (string guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) continue;
            Bake(path);
        }
    }

    [MenuItem("Assets/Beatmap/Bake MIDI -> BeatmapAsset", true)]
    private static bool ValidateBake() => Selection.assetGUIDs.Length > 0;

    private static void Bake(string path)
    {
        MidiFile midi;
        try
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(path)))
                midi = MidiFile.Read(ms);
        }
        catch (Exception e)
        {
            Debug.LogError($"[BeatmapBaker] '{path}' não é um MIDI válido: {e.Message}");
            return;
        }

        var division = midi.TimeDivision as TicksPerQuarterNoteTimeDivision;
        if (division == null)
        {
            Debug.LogError("[BeatmapBaker] MIDI com divisão SMPTE não é suportado. Exporte com PPQ (padrão do FL Studio).");
            return;
        }
        double ppq = division.TicksPerQuarterNote;

        // ---------- mapa de tempo (lido direto dos eventos Set Tempo) ----------
        var tempoEvents = midi.GetTimedEvents()
            .Where(t => t.Event is SetTempoEvent)
            .OrderBy(t => t.Time)
            .Select(t => (tick: t.Time, spb: ((SetTempoEvent)t.Event).MicrosecondsPerQuarterNote / 1000000.0))
            .ToList();

        // Sem evento no tick 0 => MIDI assume 120 BPM
        if (tempoEvents.Count == 0 || tempoEvents[0].tick > 0)
            tempoEvents.Insert(0, (0L, 0.5));

        var segments = new List<TempoSegment>();
        double time = 0, prevBeat = 0, currentSpb = 0.5;
        foreach (var te in tempoEvents)
        {
            double beat = te.tick / ppq;
            time += (beat - prevBeat) * currentSpb;
            prevBeat = beat;
            currentSpb = te.spb;

            var seg = new TempoSegment { startBeat = beat, startTime = time, secondsPerBeat = currentSpb };
            if (segments.Count > 0 && Math.Abs(segments[segments.Count - 1].startBeat - beat) < 1e-9)
                segments[segments.Count - 1] = seg; // dois tempos no mesmo tick: vale o último
            else
                segments.Add(seg);
        }

        // ---------- asset de destino ----------
        string dir = Path.GetDirectoryName(path).Replace('\\', '/');
        string baseName = Path.GetFileName(path);
        while (Path.HasExtension(baseName)) baseName = Path.GetFileNameWithoutExtension(baseName);
        string outPath = $"{dir}/{baseName}_Beatmap.asset";

        var asset = AssetDatabase.LoadAssetAtPath<BeatmapAsset>(outPath);
        bool created = asset == null;
        if (created)
        {
            asset = ScriptableObject.CreateInstance<BeatmapAsset>();
            asset.songTitle = baseName;
        }

        asset.tempo = segments.ToArray();

        // ---------- notas ----------
        var raw = new List<(long tick, byte lane)>();
        foreach (var n in midi.GetNotes())
        {
            if (n.NoteName == LeftNote) raw.Add((n.Time, (byte)LaneType.Left));
            else if (n.NoteName == RightNote) raw.Add((n.Time, (byte)LaneType.Right));
        }

        raw.Sort((a, b) => a.tick != b.tick ? a.tick.CompareTo(b.tick) : a.lane.CompareTo(b.lane));

        // Remove duplicatas (ex.: C3 e C4 no mesmo tick na mesma lane = 2 frutas empilhadas)
        var times = new List<double>();
        var beats = new List<double>();
        var lanes = new List<byte>();
        long lastTickL = -1, lastTickR = -1;
        foreach (var r in raw)
        {
            ref long last = ref (r.lane == (byte)LaneType.Left ? ref lastTickL : ref lastTickR);
            if (r.tick == last) continue;
            last = r.tick;

            double beat = r.tick / ppq;
            times.Add(asset.BeatsToSeconds(beat));
            beats.Add(beat);
            lanes.Add(r.lane);
        }

        asset.noteTimes = times.ToArray();
        asset.noteBeats = beats.ToArray();
        asset.noteLanes = lanes.ToArray();

        // Tenta achar o AudioClip com o mesmo nome
        if (asset.clip == null)
        {
            string[] clips = AssetDatabase.FindAssets(baseName + " t:AudioClip");
            if (clips.Length > 0)
                asset.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(clips[0]));
        }

        if (created) AssetDatabase.CreateAsset(asset, outPath);
        else EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        // ---------- relatório ----------
        int left = lanes.Count(l => l == (byte)LaneType.Left);
        int right = lanes.Count - left;
        double minBpm = segments.Min(s => s.Bpm), maxBpm = segments.Max(s => s.Bpm);
        string bpmInfo = segments.Count == 1
            ? $"BPM fixo: {minBpm:0.##}"
            : $"{segments.Count} mudanças de tempo, BPM {minBpm:0.##} -> {maxBpm:0.##}";
        string span = times.Count > 0 ? $"{times[0]:0.000}s .. {times[times.Count - 1]:0.000}s" : "-";

        Debug.Log($"[BeatmapBaker] OK -> {outPath}\n" +
                  $"Notas: {times.Count} (esq {left} / dir {right}) | {bpmInfo} | PPQ {ppq} | {span}\n" +
                  (asset.clip != null ? $"Clip: {asset.clip.name}" : "ATENÇÃO: arraste o AudioClip para o campo 'clip' do asset."),
                  asset);

        if (segments.Count == 1)
            Debug.Log("[BeatmapBaker] Só 1 BPM encontrado. Se sua música acelera, o FL não exportou a automação de tempo no MIDI.");
    }
}