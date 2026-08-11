using Melanchall.DryWetMidi.Interaction;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Lane : MonoBehaviour
{
    public GameObject notePrefab;

    List<Note> notes = new List<Note>();
    public List<double> timeStamps = new List<double>();

    [SerializeField]
    private PlayerAttack playerAttack;

    int spawnIndex = 0;

    [SerializeField]
    private float missPositionX = -17f;

    private void OnEnable()
    {
        if (playerAttack != null)
        {
            playerAttack.OnAttack += OnPlayerAttack;
        }
    }

    private void OnDisable()
    {
        if (playerAttack != null)
        {
            playerAttack.OnAttack -= OnPlayerAttack;
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
            CheckMissedNotes();
        }
    }

    private void OnPlayerAttack(string attackType)
    {
        Debug.Log($"ATAQUE RECEBIDO: {attackType}");

        if (notes.Count == 0)
        {
            Debug.Log("MISS: Lane não possui notas.");
            Miss();
            return;
        }

        var note = notes[0];

        double audioTime = SongManager.GetAudioSourceTime();
        double noteTime = note.assignedTime;
        double difference = audioTime - noteTime;

        double margin = SongManager.Instance.marginOfError;

        Debug.Log(
            $"Audio: {audioTime:F3} | " +
            $"Note: {noteTime:F3} | " +
            $"Diferença: {difference:F3} | " +
            $"Janela: {margin:F3}"
        );

        if (Mathf.Abs((float)difference) <= margin)
        {
            Debug.Log(">>> HIT <<<");

            Hit();

            Destroy(note.gameObject);
            notes.RemoveAt(0);
        }
        else
        {
            Debug.Log(">>> MISS <<<");
        }

    }

    private void CheckMissedNotes()
    {
        if (notes.Count == 0)
            return;

        Note note = notes[0];

        if (note.transform.position.x < missPositionX)
        {
            Debug.Log($"NOTA PERDIDA: {missPositionX}");

            Miss();
            notes.RemoveAt(0);
        }
    }

    public void SetTimeStamps(Melanchall.DryWetMidi.Interaction.Note[] array)
    {
        foreach (var note in array)
        {
            var metricTimestamp = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, SongManager.midiFile.GetTempoMap());
            timeStamps.Add((double)metricTimestamp.Minutes * 60f + metricTimestamp.Seconds + (double)metricTimestamp.Milliseconds / 1000f);
        }
    }

    private void Hit()
    {
        ScoreManager.Hit();
    }

    private void Miss()
    {
        ScoreManager.Miss();
    }
}
