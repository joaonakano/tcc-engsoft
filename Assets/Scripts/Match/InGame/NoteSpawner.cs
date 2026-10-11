using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScrollMode
{
    [Tooltip("A fruta leva sempre fallDuration segundos para cair. Se o BPM sobe, as frutas ficam mais próximas umas das outras.")]
    ConstantTime,
    [Tooltip("A fruta leva fallBeats beats para cair. Se o BPM sobe, a queda acelera e o espaçamento visual entre frutas se mantém.")]
    BeatScaled
}

public class NoteSpawner : MonoBehaviour
{
    [Header("Referências")]
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
    public ScrollMode scrollMode = ScrollMode.ConstantTime;
    [Tooltip("Altura onde a fruta nasce")]
    public float spawnY = 6f;
    [Tooltip("Y DESTINO: onde a fruta esta no momento exato do PERFECT")]
    public float targetY = -3f;
    public float spawnXLeft = -3.76f;
    public float spawnXRight = 1.83f;
    [Tooltip("ConstantTime: tempo (s) de spawnY até targetY")]
    public float fallDuration = 0.4f;
    [Tooltip("BeatScaled: quantos beats a fruta leva de spawnY até targetY")]
    public float fallBeats = 1f;
    [Tooltip("Depois de passar do alvo a fruta segue caindo até ser marcada como erro (em vez de parar no alvo)")]
    public bool continueFallingPastTarget = true;

    [Header("Pré-carga")]
    [Tooltip("Frames em que cada instância fica visível durante o prewarm (sobe sprites/shaders pra GPU). Faça com a tela coberta pelo wipe. 0 = desliga.")]
    public int warmupVisibleFrames = 2;
    [Tooltip("Tempo (s) depois do alvo em que a fruta ainda existe (usado só para dimensionar o pool)")]
    public float lingerTime = 0.3f;

    public bool IsReady { get; private set; }

    private static readonly int SpawnHash = Animator.StringToHash("spawn");

    private BeatmapAsset beatmap;
    private readonly List<GameObject> validPrefabs = new();
    private readonly List<NoteController> active = new(32);

    private double[] spawnTimes;
    private double[] exhaustTimes;
    private int nextSpawnIndex;
    private int nextExhaustIndex;

    // =========================================================
    // PRÉ-CARGA (chamada pelo GameManager antes do wipe abrir)
    // =========================================================

    public IEnumerator PrepareRoutine()
    {
        IsReady = false;

        beatmap = songManager != null ? songManager.Beatmap : null;
        if (beatmap == null || beatmap.NoteCount == 0)
        {
            Debug.LogError("NoteSpawner: beatmap ausente ou sem notas (rode o Bake no .mid e arraste o asset no SongManager).");
            yield break;
        }

        validPrefabs.Clear();
        foreach (GameObject p in notePrefabs)
        {
            if (p == null) continue;
            if (p.GetComponent<NoteController>() == null)
            {
                Debug.LogWarning($"NoteSpawner: prefab '{p.name}' sem NoteController, ignorado.");
                continue;
            }
            validPrefabs.Add(p);
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogError("NoteSpawner: Nenhum prefab de fruta válido configurado.");
            yield break;
        }

        // Tudo que dá pra calcular antes, calcula antes: nada de conta pesada durante a música
        int n = beatmap.NoteCount;
        spawnTimes = new double[n];
        exhaustTimes = new double[n];

        for (int i = 0; i < n; i++)
        {
            spawnTimes[i] = scrollMode == ScrollMode.ConstantTime
                ? beatmap.noteTimes[i] - fallDuration
                : beatmap.BeatsToSeconds(beatmap.noteBeats[i] - fallBeats);

            exhaustTimes[i] = spawnTimes[i] - exhaustLeadTime;
        }

        // Dimensiona o pool pelo pico real de frutas simultâneas deste chart
        int peak = Mathf.Clamp(ComputePeakActive(), 2, 24) + 1;

        Vector3 warmPos = new Vector3(spawnXLeft, spawnY, 0f);
        var slicedPrefabs = new HashSet<GameObject>();

        foreach (GameObject prefab in validPrefabs)
        {
            yield return StartCoroutine(PoolService.Prewarm(prefab, peak, warmPos, warmupVisibleFrames));

            GameObject sliced = prefab.GetComponent<NoteController>().SlicedFruitPrefab;
            if (sliced != null) slicedPrefabs.Add(sliced);
        }

        foreach (GameObject sliced in slicedPrefabs)
            yield return StartCoroutine(PoolService.Prewarm(sliced, Mathf.Min(peak, 12), warmPos, warmupVisibleFrames));

        if (hitManager != null)
            yield return StartCoroutine(hitManager.PrewarmRoutine(warmPos, warmupVisibleFrames));

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ConfigureForSong(n);

        nextSpawnIndex = 0;
        nextExhaustIndex = 0;
        active.Clear();

        Debug.Log($"[NoteSpawner] Pronto: {n} notas, pool de {peak} por prefab, modo {scrollMode}.");
        IsReady = true;
    }

    private int ComputePeakActive()
    {
        int n = beatmap.NoteCount;
        int peak = 1;
        int oldest = 0;

        for (int i = 0; i < n; i++)
        {
            while (oldest < i && beatmap.noteTimes[oldest] + lingerTime <= spawnTimes[i])
                oldest++;

            peak = Mathf.Max(peak, i - oldest + 1);
        }
        return peak;
    }

    // =========================================================
    // LOOP
    // =========================================================

    private void Update()
    {
        if (!IsReady || songManager == null || !songManager.IsPlaying || songManager.IsPaused)
            return;

        double songTime = songManager.GetSongTime();
        int count = beatmap.NoteCount;

        // Exaustor: dispara antes da fruta nascer
        while (nextExhaustIndex < count && songTime >= exhaustTimes[nextExhaustIndex])
        {
            TriggerExhaust(beatmap.LaneOf(nextExhaustIndex));
            nextExhaustIndex++;
        }

        double songBeat = scrollMode == ScrollMode.BeatScaled ? beatmap.SecondsToBeats(songTime) : 0.0;

        // Spawn
        while (nextSpawnIndex < count && songTime >= spawnTimes[nextSpawnIndex])
        {
            SpawnNote(nextSpawnIndex, songTime, songBeat);
            nextSpawnIndex++;
        }

        // Movimento de todas as frutas ativas, a partir do mesmo relógio
        for (int i = active.Count - 1; i >= 0; i--)
        {
            NoteController note = active[i];
            if (note == null)
            {
                active.RemoveAt(i);
                continue;
            }
            note.SetY(ComputeY(note, songTime, songBeat));
        }
    }

    private float ComputeY(NoteController note, double songTime, double songBeat)
    {
        float t = scrollMode == ScrollMode.ConstantTime
            ? (float)((note.TargetTime - songTime) / fallDuration)
            : (float)((note.TargetBeat - songBeat) / fallBeats);

        // t = 1 -> spawnY, t = 0 -> targetY, t < 0 -> já passou do alvo
        if (!continueFallingPastTarget && t < 0f) t = 0f;

        return targetY + (spawnY - targetY) * t;
    }

    private void TriggerExhaust(LaneType lane)
    {
        Animator anim = lane == LaneType.Left ? leftExaustorAnimator : rightExaustorAnimator;
        if (anim != null)
            anim.SetTrigger(SpawnHash);
    }

    private void SpawnNote(int index, double songTime, double songBeat)
    {
        GameObject prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
        LaneType lane = beatmap.LaneOf(index);
        float x = lane == LaneType.Left ? spawnXLeft : spawnXRight;

        double targetTime = beatmap.noteTimes[index];
        double targetBeat = beatmap.noteBeats[index];

        // Já nasce na posição correta (mesmo se o frame atrasou), sem "pulo" no primeiro frame
        float t = scrollMode == ScrollMode.ConstantTime
            ? (float)((targetTime - songTime) / fallDuration)
            : (float)((targetBeat - songBeat) / fallBeats);
        float y = targetY + (spawnY - targetY) * t;

        GameObject obj = PoolService.Spawn(prefab, new Vector3(x, y, 0f), Quaternion.identity);
        NoteController controller = obj.GetComponent<NoteController>();
        controller.Initialize(targetTime, targetBeat, lane);

        active.Add(controller);

        if (hitManager != null)
            hitManager.RegisterNote(controller, lane);
    }

    /// <summary>Chamado pelo HitManager quando a fruta foi cortada ou apodreceu.</summary>
    public void Despawn(NoteController note)
    {
        if (note == null) return;

        active.Remove(note);
        PoolService.Release(note.gameObject);
    }
}