using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject notePrefab;

    [SerializeField]
    private Transform spawnPoint;

    private float timer = 0f;
    public float interval = 1f;

    void Start()
    {
        
    }

    void Update()
    {
        timer += Time.deltaTime;
        Debug.Log($"Timer: {timer}");

        if (timer >= interval)
        {
            Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
            timer = 0f;
        }
    }
}
