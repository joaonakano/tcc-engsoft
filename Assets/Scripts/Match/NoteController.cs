using UnityEngine;

public class NoteController : MonoBehaviour
{
    public double TargetTime { get; private set; }

    private double spawnTime;
    private float fallDuration;

    private float spawnY;
    private float targetY;

    private double songStartTime;

    private bool initialized;
    private bool resolved;

    public void Initialize(
        double targetTime,
        double spawnTime,
        float fallDuration,
        float spawnY,
        float targetY,
        double songStartTime)
    {
        TargetTime = targetTime;

        this.spawnTime = spawnTime;
        this.fallDuration = fallDuration;
        this.spawnY = spawnY;
        this.targetY = targetY;
        this.songStartTime = songStartTime;

        initialized = true;
        resolved = false;
    }

    private void Update()
    {
        if (!initialized || resolved)
            return;

        double currentSongTime = AudioSettings.dspTime - songStartTime;

        UpdatePosition(currentSongTime);
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