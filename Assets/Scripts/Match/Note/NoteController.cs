using UnityEngine;

/// <summary>
/// Fruta. Não tem mais Update próprio: quem move todas as frutas é o NoteSpawner (1 cálculo de tempo por frame).
/// LaneType e NoteData NÃO ficam mais aqui (LaneType agora está em BeatmapAsset.cs; NoteData foi removido).
/// </summary>
public class NoteController : MonoBehaviour
{
    [Header("Visual do Corte")]
    [SerializeField] private GameObject slicedFruitPrefab;

    public GameObject SlicedFruitPrefab => slicedFruitPrefab;

    public double TargetTime { get; private set; }
    public double TargetBeat { get; private set; }
    public LaneType Lane { get; private set; }

    private bool resolved;

    public void Initialize(double targetTime, double targetBeat, LaneType lane)
    {
        TargetTime = targetTime;
        TargetBeat = targetBeat;
        Lane = lane;
        resolved = false;
    }

    public void SetY(float y)
    {
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;
    }

    public void Cut()
    {
        if (resolved) return;
        resolved = true;

        if (slicedFruitPrefab == null) return;

        GameObject sliced = PoolService.Spawn(slicedFruitPrefab, transform.position, Quaternion.identity); // o SlicedFruit se devolve ao pool sozinho
        if (sliced != null && sliced.TryGetComponent<SlicedFruit>(out var slicedScript))
            slicedScript.Initialize();
    }

    public void Rotten()
    {
        resolved = true;
    }

    public bool IsResolved()
    {
        return resolved;
    }
}