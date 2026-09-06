using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System.Linq;

public class MidiReaderManager : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Nome do arquivo MIDI a ser lido (sem extensão .mid)")]
    public string midiFileName;

    // Lista principal que armazena todas as notas
    public List<NoteData> SongNotes { get; private set; } = new List<NoteData>();

    void Start()
    {
        LoadMidiFile();
    }

    void LoadMidiFile()
    {
        // Carrega o arquivo MIDI da pasta Resources
        TextAsset midiAsset = Resources.Load<TextAsset>(midiFileName);

        if (midiAsset == null)
        {
            Debug.LogError($"Arquivo MIDI '{midiFileName}' não encontrado na pasta Resources.");
            return;
        }

        // Lê os bytes
        using (var stream = new MemoryStream(midiAsset.bytes))
        {
            var midiFile = MidiFile.Read(stream);
            var tempoMap = midiFile.GetTempoMap();

            // Pega todas as notas
            var notes = midiFile.GetNotes();
            SongNotes.Clear();

            foreach (var note in notes)
            {
                LaneType? assignedLane = null;

                // Mapeamento com base no nome da nota
                if (note.NoteName == NoteName.C)
                {
                    assignedLane = LaneType.Left;
                }
                else if (note.NoteName == NoteName.D)
                {
                    assignedLane = LaneType.Right;
                }

                // Se a nota for C ou D, é processada e salva
                if (assignedLane.HasValue)
                {
                    // Converte o tempo do MIDI (ticks) para segundos reais usando o BPM da musica
                    double timeInSeconds = note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;

                    NoteData newNote = new NoteData(timeInSeconds, assignedLane.Value, note.NoteNumber);
                    SongNotes.Add(newNote);
                }
            }

            // Ordena as notas pelo tempo em segundos
            SongNotes = SongNotes.OrderBy(n => n.TimeInSeconds).ToList();

            Debug.Log($"Midi carregado com sucesso! Total de notas válidas (C e D): {SongNotes.Count}");
        }
    }
}
