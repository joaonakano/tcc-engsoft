using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private WipeController _wipeController;
    [SerializeField] private InGameMenuController _menuController;
    [SerializeField] private CursorManager _cursorManager;
    [SerializeField] private string nextSceneName = "G01";
    [SerializeField] private SongManager songManager;

    [Header("Configurações")]
    [SerializeField]
    private float startDelay = 2f;

    private bool matchEnded = false;

    private void Start()
    {
        if (songManager == null) Debug.LogError("GameManager: SongManager não foi configurado.");
        if (_wipeController == null) Debug.LogError("GameManager: WipeController não foi configurado.");
        if (_menuController == null) Debug.LogError("GameManager: InGameMenuController não foi configurado.");
        if (_cursorManager == null) Debug.LogError("GameManager: CursorManager não foi configurado.");

        _cursorManager.HabilitarCursor(false);

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

        ScoreManager.Instance.SaveResult();
        _wipeController.StartWipeIn(OnMatchEnded);
    }

    private IEnumerator StartMatchSequence()
    {
        yield return new WaitForSeconds(startDelay);
        _wipeController.StartWipeOut(() => _menuController.Liberar());
    }

    public void OnMatchStarted()
    {
        Debug.Log("MATCH STARTED");
    }

    public void OnMatchEnded()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
 