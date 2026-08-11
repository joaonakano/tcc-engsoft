using UnityEngine;

public class Note : MonoBehaviour
{
    public float assignedTime;

    private void Update()
    {
        // Momento exato em que esta nota deveria nascer (spawn)
        float spawnTime = assignedTime - SongManager.Instance.noteTime;

        // Momento exato em que ela some (despawn)
        float despawnTime = assignedTime + SongManager.Instance.noteTime;

        // Tempo atual da música
        double audioTime = SongManager.GetAudioSourceTime();

        // Progresso normalizado de 0 (no spawn) a 1 (no despawn)
        float totalDuration = despawnTime - spawnTime;
        float timeSinceSpawn = (float)(audioTime - spawnTime);
        float t = timeSinceSpawn / totalDuration;

        if (audioTime >= despawnTime)
        {
            Destroy(gameObject);
        }
        else
        {
            // O Mathf.Clamp01 impede que a nota ultrapasse os limites se nascer com frações de atraso,
            // garantindo que ela comece exatamente no noteSpawnX.
            t = Mathf.Clamp01(t);

            transform.localPosition = Vector3.Lerp(
                Vector3.right * SongManager.Instance.noteSpawnX,
                Vector3.right * SongManager.Instance.noteDespawnX,
                t
            );
        }
    }
}