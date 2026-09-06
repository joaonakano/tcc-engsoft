using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Referências")]
    public MidiReaderManager midiReader;
    public SongManager songManager;

    [Header("Frutas")]
    public List<GameObject> notePrefabs = new List<GameObject>();

    [Header("Exaustores")]
    public Animator leftExaustorAnimator;
    public Animator rightExaustorAnimator;

    [Header("Posições das Lanes")]
    public Transform leftLaneTransform;
    public Transform rightLaneTransform;

    [Header("Configurações do Movimento")]
    [Tooltip("Altura da tábua / hitline")]
    public float targetY = -4f;

    [Tooltip("Altura onde a fruta nasce")]
    public float spawnY = 6f;

    [Tooltip("Tempo que a fruta leva do dispenser até a tábua")]
    public float fallDuration = 0.4f;

    [Header("Configurações do Exaustor")]
    [Tooltip("Quanto tempo antes da fruta nascer o exaustor começa")]
    public float exhaustLeadTime = 0.2f;

    private int nextIndex = 0;
    private int nextExhaustIndex = 0;

    void Update()
    {
        if (songManager == null || midiReader == null)
            return;

        if (!songManager.IsPlaying)
            return;

        if (midiReader.SongNotes == null || midiReader.SongNotes.Count == 0)
            return;

        double currentSongTime = songManager.GetSongTime();

        ProcessExhaustors(currentSongTime);
        ProcessSpawns(currentSongTime);
    }

    private void ProcessExhaustors(double currentSongTime)
    {
        // 1. Controle do Exaustor (dispara antes da fruta nascer)
        while (nextExhaustIndex < midiReader.SongNotes.Count)
        {
            NoteData noteData = midiReader.SongNotes[nextExhaustIndex];
            
            // Momento que a fruta deve nascer
            double spawnTime = noteData.TimeInSeconds - fallDuration;

            // Momento em que o exaustor deve começar
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
    }

    private void ProcessSpawns(double currentSongTime)
    {
        // 2. Controle do Spawn da Fruta
        while (nextIndex < midiReader.SongNotes.Count)
        {
            NoteData noteData = midiReader.SongNotes[nextIndex];

            // A fruta deve nascer antes do TargetTime
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

    private void SpawnNote(NoteData noteData)
    {
        if (notePrefabs == null || notePrefabs.Count == 0)
        {
            Debug.LogWarning("NoteSpawner: Nenhum prefab de fruta foi configurado.");
            return;
        }

        Transform targetLaneTransform =
            noteData.Lane == LaneType.Left
                ? leftLaneTransform
                : rightLaneTransform;

        if (targetLaneTransform == null)
        {
            Debug.LogWarning($"NoteSpawner: Lane {noteData.Lane} não possui Transform configurado.");

            return;
        }

        int randomIndex = Random.Range(0, notePrefabs.Count);

        GameObject prefabToSpawn = notePrefabs[randomIndex];

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("NoteSpawner: Prefab selecionado é nulo.");
            return;
        }

        Vector3 spawnPosition = new Vector3(
            targetLaneTransform.position.x,
            spawnY,
            0f
        );

        GameObject noteObj = Instantiate(
            prefabToSpawn,
            spawnPosition,
            Quaternion.identity
        );

        // O TargetTime vem diretamente do MIDI
        double targetTime = noteData.TimeInSeconds;

        // A fruta precisa nascer exatamente fallDuration antes do momento em que deve ser atingida
        double spawnTime = targetTime - fallDuration;

        NoteController controller = noteObj.GetComponent<NoteController>();

        if (controller != null)
        {
            controller.Initialize(
                targetTime,
                spawnTime,
                fallDuration,
                spawnY,
                targetY,
                songManager.SongStartTime
            );

            HitManager hitManager = FindFirstObjectByType<HitManager>();

            if (hitManager != null)
            {
                hitManager.RegisterNote(
                    controller,
                    noteData.Lane
                );
            }
        }
    }
}