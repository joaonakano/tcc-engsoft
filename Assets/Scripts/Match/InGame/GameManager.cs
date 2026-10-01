using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private WipeController wipeController;

    [SerializeField]
    private SongManager songManager;

    [Header("Settings")]
    [SerializeField]
    private float startDelay = 2f;

    private bool matchEnded = false;

    private void Start()
    {
        StartCoroutine(StartMatchSequence());
    }

    private void Update()
    {
        if (matchEnded) return;

        if (songManager != null && songManager.HasFinished)
        {
            EndMatch();
        }
    }

    private void EndMatch()
    {
        matchEnded = true;
        wipeController.StartWipeIn();
    }

    private IEnumerator StartMatchSequence()
    {
        yield return new WaitForSeconds(startDelay);
        wipeController.StartWipeOut();
    }

    public void OnMatchStarted()
    {
        Debug.Log("MATCH STARTED");
    }

    public void OnMatchEnded()
    {
        SceneManager.LoadScene("S01");
    }
}
