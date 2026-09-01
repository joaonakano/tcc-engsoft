using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Referências")]
    public MidiReaderManager midiReader;
    
    public List<GameObject> notePrefabs = new List<GameObject>();

    public Animator leftExaustorAnimator;
    public Animator rightExaustorAnimator;

    [Header("Posições das Lanes")]
    public Transform leftLaneTransform;
    public Transform rightLaneTransform;

    [Header("Configurações do Movimento")]
    public float targetY = -4f; // Altura da Tábua (Hitline)
    public float spawnY = 6f;   // Altura do Dispenser

    [Tooltip("Tempo em segundos que a fruta leva caindo do dispenser até a tábua")]
    public float fallDuration = 0.4f;

    [Header("Configurações da Partida")]
    [Tooltip("Atraso para começar a música em segundos")]
    public float songDelay = 2f;

    [Header("Configurações do Exaustor")]
    [Tooltip("Quanto tempo antes da fruta nascer o exaustor deve começar a animar (em segundos)")]
    public float exhaustLeadTime = 0.2f;

    private int nextIndex = 0;
    private int nextExhaustIndex = 0;
    public double SongStartTime;
    private bool isPlaying = false;

    void Start()
    {
        Invoke(nameof(StartSong), songDelay);
    }

    void StartSong()
    {
        SongStartTime = AudioSettings.dspTime;
        isPlaying = true;
        GetComponent<AudioSource>().PlayScheduled(SongStartTime);
    }

    void Update()
    {
        if (!isPlaying || midiReader.SongNotes == null) return;

        double currentSongTime = AudioSettings.dspTime - SongStartTime;

        // 1. Controle do Exaustor (dispara antes da fruta nascer)
        while (nextExhaustIndex < midiReader.SongNotes.Count)
        {
            NoteData noteData = midiReader.SongNotes[nextExhaustIndex];

            // A fruta nasce em: targetTime - fallDuration
            double spawnTime = noteData.TimeInSeconds - fallDuration;
            // O exaustor dispara um tempo antes da fruta nascer
            double exhaustTriggerTime = spawnTime - exhaustLeadTime;

            if (currentSongTime >= exhaustTriggerTime)
            {
                TriggerExhaust(noteData.Lane);
                nextExhaustIndex++;
            }
            else
            {
                break;
            }
        }

        // 2. Controle do Spawn da Fruta
        while (nextIndex < midiReader.SongNotes.Count)
        {
            NoteData noteData = midiReader.SongNotes[nextIndex];
            double spawnTime = noteData.TimeInSeconds - fallDuration;

            if (currentSongTime >= spawnTime)
            {
                SpawnNote(noteData);
                nextIndex++;
            }
            else
            {
                break;
            }
        }
    }

    void TriggerExhaust(LaneType lane)
    {
        if (lane == LaneType.Left && leftExaustorAnimator != null)
        {
            leftExaustorAnimator.SetTrigger("spawn");
        }
        else if (lane == LaneType.Right && rightExaustorAnimator != null)
        {
            rightExaustorAnimator.SetTrigger("spawn");
        }
    }

    void SpawnNote(NoteData noteData)
    {
        int randomIndex = Random.Range(0, notePrefabs.Count);
        GameObject prefabToSpawn = notePrefabs[randomIndex];
        Transform targetLaneTransform = (noteData.Lane == LaneType.Left) ? leftLaneTransform : rightLaneTransform;

        if (prefabToSpawn == null || targetLaneTransform == null) return;

        Vector3 spawnPosition = new Vector3(targetLaneTransform.position.x, spawnY, 0f);
        GameObject noteObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Calcula o spawnTime exato para passar ao controller
        double spawnTime = noteData.TimeInSeconds - fallDuration;

        NoteController controller = noteObj.GetComponent<NoteController>();
        if (controller != null)
        {
            controller.Initialize(noteData.TimeInSeconds, spawnTime, fallDuration, spawnY, targetY, SongStartTime);

            HitManager hitManager = FindFirstObjectByType<HitManager>();
            if (hitManager != null)
            {
                hitManager.RegisterNote(controller, noteData.Lane);
            }
        }
    }
}