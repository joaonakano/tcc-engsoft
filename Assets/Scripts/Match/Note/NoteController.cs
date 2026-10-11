using UnityEngine;

public class NoteController : MonoBehaviour
{
    public double TargetTime { get; private set; }

    [Header("Visual do Corte")]
    [SerializeField] private GameObject slicedFruitPrefab;

    private double spawnTime;
    private float fallDuration;

    private float spawnY;
    private float targetY;

    private SongManager songManager;

    private bool initialized;
    private bool resolved;

    public void Initialize(double targetTime, double spawnTime, float fallDuration, float spawnY, float targetY, SongManager songManager)
    {
        TargetTime = targetTime;

        this.spawnTime = spawnTime;
        this.fallDuration = fallDuration;
        this.spawnY = spawnY;
        this.targetY = targetY;
        this.songManager = songManager;

        initialized = true;
        resolved = false;
    }

    private void Update()
    {
        if (!initialized || resolved || songManager == null)
            return;

        UpdatePosition(songManager.GetSongTime());
    }

    private void UpdatePosition(double currentSongTime)
    {
        double elapsedSinceSpawn = currentSongTime - spawnTime;

        float progress = (float)(elapsedSinceSpawn / fallDuration);
        progress = Mathf.Clamp01(progress);

        float currentY = Mathf.Lerp(spawnY, targetY, progress);

        transform.position = new Vector3(
            transform.position.x,
            currentY,
            transform.position.z
        );
    }

    public void Cut()
    {
        if (resolved)
            return;

        if (slicedFruitPrefab != null)
        {
            GameObject sliced = Instantiate(slicedFruitPrefab, transform.position, Quaternion.identity);

            if (sliced.TryGetComponent<SlicedFruit>(out var slicedScript))
            {
                slicedScript.Initialize();
            }
        }

        resolved = true;
    }

    public void Rotten()
    {
        if (resolved)
            return;

        resolved = true;
    }

    public bool IsResolved()
    {
        return resolved;
    }
}