using UnityEngine;
using UnityEngine.InputSystem;

public class Lane : MonoBehaviour
{
    public Transform spawnPoint;
    public Transform hitLine;

    public void SpawnNote(GameObject notePrefab)
    {
        GameObject note = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
        Note noteScript = note.GetComponent<Note>();
        noteScript.SetHitline(hitLine);
    }
}
