using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Referências")]
    public MidiReaderManager midiReader;
    public SongManager songManager;
    public HitManager hitManager;

    [Header("Frutas")]
    public List<GameObject> notePrefabs = new();

    [Header("Exaustores")]
    public Animator leftExaustorAnimator;
    public Animator rightExaustorAnimator;
    [Tooltip("Quanto tempo antes da fruta nascer o exaustor dispara")]
    public float exhaustLeadTime = 0.2f;

    [Header("Movimento")]
    [Tooltip("Altura onde a fruta nasce")]
    public float spawnY = 6f;
    [Tooltip("Y DESTINO: onde a fruta esta no momento exato do PERFECT")]
    public float targetY = -3f;
    public float spawnXLeft = -3.76f;
    public float spawnXRight = 1.83f;
    [Tooltip("Tempo que a fruta leva de spawnY ate targetY")]
    public float fallDuration = 0.4f;

    private int nextSpawnIndex;
    private int nextExhaustIndex;

    private void Update()
    {
        if (songManager == null || midiReader == null || !songManager.IsPlaying)
            return;

        List<NoteData> notes = midiReader.SongNotes;
        if (notes == null || notes.Count == 0)
            return;

        double songTime = songManager.GetSongTime();

        // Exaustor: dispara antes da fruta nascer
        while (nextExhaustIndex < notes.Count)
        {
            NoteData data = notes[nextExhaustIndex];
            double exhaustTime = data.TimeInSeconds - fallDuration - exhaustLeadTime;

            if (songTime < exhaustTime) break;

            TriggerExhaust(data.Lane);
            nextExhaustIndex++;
        }

        // Spawn: a fruta nasce fallDuration antes de chegar no destino
        while (nextSpawnIndex < notes.Count)
        {
            NoteData data = notes[nextSpawnIndex];
            double spawnTime = data.TimeInSeconds - fallDuration;

            if (songTime < spawnTime) break;

            SpawnNote(data, spawnTime);
            nextSpawnIndex++;
        }
    }

    private void TriggerExhaust(LaneType lane)
    {
        Animator anim = lane == LaneType.Left ? leftExaustorAnimator : rightExaustorAnimator;
        if (anim != null)
            anim.SetTrigger("spawn");
    }

    private void SpawnNote(NoteData data, double spawnTime)
    {
        if (notePrefabs.Count == 0)
        {
            Debug.LogWarning("NoteSpawner: Nenhum prefab de fruta configurado.");
            return;
        }

        GameObject prefab = notePrefabs[Random.Range(0, notePrefabs.Count)];
        if (prefab == null) return;

        float x = data.Lane == LaneType.Left ? spawnXLeft : spawnXRight;
        GameObject obj = Instantiate(prefab, new Vector3(x, spawnY, 0f), Quaternion.identity);

        NoteController controller = obj.GetComponent<NoteController>();
        if (controller == null)
        {
            Debug.LogWarning("NoteSpawner: Prefab sem NoteController.");
            return;
        }

        controller.Initialize(
            data.TimeInSeconds,   // TargetTime: fruta chega no targetY
            spawnTime,
            fallDuration,
            spawnY,
            targetY,
            songManager.SongStartTime
        );

        if (hitManager != null)
            hitManager.RegisterNote(controller, data.Lane);
    }
}