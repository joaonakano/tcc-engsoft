using Melanchall.DryWetMidi.Interaction;
using UnityEngine;
using System.Collections.Generic;

public class Lane : MonoBehaviour
{
    public GameObject notePrefab;
    List<Note> notes = new List<Note>();
    public List<double> timeStamps = new List<double>();

    int spawnIndex = 0;

    public void SetTimeStamps(Melanchall.DryWetMidi.Interaction.Note[] array)
    {
        foreach (var note in array)
        {
            var metricTimestamp = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, SongManager.midiFile.GetTempoMap());
            timeStamps.Add((double)metricTimestamp.Minutes * 60f + metricTimestamp.Seconds + (double)metricTimestamp.Milliseconds / 1000f);
        }
    }

    void Update()
    {
        if (spawnIndex < timeStamps.Count)
        {
            if (SongManager.GetAudioSourceTime() >= timeStamps[spawnIndex] - SongManager.Instance.noteTime)
            {
                var note = Instantiate(notePrefab, transform);
                notes.Add(note.GetComponent<Note>());
                note.GetComponent<Note>().assignedTime = (float)timeStamps[spawnIndex];
                spawnIndex++;
            }
        }
    }
}
