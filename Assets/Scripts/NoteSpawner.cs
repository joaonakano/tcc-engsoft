using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Referências")]
    public MidiReaderManager midiReader;
    public GameObject leftNotePrefab;
    public GameObject rightNotePrefab;
    public Animator leftExaustorAnimator;
    public Animator rightExaustorAnimator;

    [Header("Posições das Lanes")]
    public Transform leftLaneTransform;
    public Transform rightLaneTransform;

    [Header("Configurações do Movimento")]
    public float targetY = -4f; // Altura de Hitline
    public float spawnY = 6f;   // Altura onde a nota nasce
    public float fallSpeed = 5f;    // Unidades por segundo que a nota desce

    [Header("Configurações da Partida")]
    [Tooltip("Atraso para começar a música em segundos")]
    public float songDelay = 2f;

    [Header("Configurações do Exaustor")]
    [Tooltip("Quanto tempo antes da fruta nascer o exaustor deve começar a animar (em segundos)")]
    public float exhaustLeadTime = 0.2f;

    private int nextIndex = 0;
    private int nextExhaustIndex = 0;
    private double songStartTime;
    private bool isPlaying = false;
    private float preSpawnTime; // Quanto tempo antes a nota deve descer

    void Start()
    {
        // Calcula o tempo a nota leva para ir do spawn ao alvo
        float distance = Mathf.Abs(spawnY - targetY);
        preSpawnTime = distance / fallSpeed;

        // Tocar musica apos 2 segundos de preparacao
        Invoke(nameof(StartSong), songDelay);
    }

    void StartSong()
    {
        // dspTime para ter um relógio de precisão para o audio/tempo
        songStartTime = AudioSettings.dspTime;
        isPlaying = true;

        GetComponent<AudioSource>().PlayScheduled(songStartTime);
    }

    void Update()
    {
        if (!isPlaying || midiReader.SongNotes == null || nextIndex >= midiReader.SongNotes.Count) return;

        // Tempo atual da musica em segundos
        double currentSongTime = AudioSettings.dspTime - songStartTime;

        while (nextExhaustIndex < midiReader.SongNotes.Count)
        {
            NoteData noteData = midiReader.SongNotes[nextExhaustIndex];
            double spawnTime = noteData.TimeInSeconds - preSpawnTime;
            double exhaustTriggerTime = spawnTime - exhaustLeadTime; // Dispara antes do spawn

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

        // Verifica se esta na hora de spawnar proximas notas
        while (nextIndex < midiReader.SongNotes.Count)
        {
            NoteData noteData = midiReader.SongNotes[nextIndex];

            // Hora do spawn = tempo da nota - tempo que ela leva para viajar ate a Hitline
            double spawnTime = noteData.TimeInSeconds - preSpawnTime;

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
        GameObject prefabToSpawn = (noteData.Lane == LaneType.Left) ? leftNotePrefab : rightNotePrefab;
        Transform targetLaneTransform = (noteData.Lane == LaneType.Left) ? leftLaneTransform : rightLaneTransform;

        if (prefabToSpawn == null || targetLaneTransform == null) return;

        // Instancia a nota na posicao X da lane e Y no topo
        Vector3 spawnPosition = new Vector3(targetLaneTransform.position.x, spawnY, 0f);
        GameObject noteObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Inicializa o controlador da nota
        NoteController controller = noteObj.GetComponent<NoteController>();
        if (controller != null)
        {
            controller.Initialize(songStartTime + noteData.TimeInSeconds, spawnY, targetY, fallSpeed);
        }
    }
}
