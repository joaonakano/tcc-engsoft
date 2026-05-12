using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> notePrefabs = new();

    [SerializeField]
    private List<Lane> lanes = new();

    private float timer = 0f;
    public float interval = 1f;

    void Start()
    {

    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            var lane = lanes[Random.Range(0, lanes.Count)];
            var notePrefab = notePrefabs[Random.Range(0, notePrefabs.Count)];
            lane.SpawnNote(notePrefab);
            timer = 0f;
        }
    }
}
