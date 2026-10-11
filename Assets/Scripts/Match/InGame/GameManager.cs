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
    [SerializeField] private NoteSpawner noteSpawner;

    [Header("Configurações")]
    [Tooltip("Espera extra depois da pré-carga, antes do wipe abrir")]
    [SerializeField]
    private float startDelay = 0.5f;

    private bool matchEnded = false;

    private void Start()
    {
        if (songManager == null) Debug.LogError("GameManager: SongManager não foi configurado.");
        if (noteSpawner == null) Debug.LogError("GameManager: NoteSpawner não foi configurado.");
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
        // 1) PRÉ-CARGA: clip na memória, notas pré-calculadas, pools aquecidos.
        //    Tudo isso acontece com a tela ainda coberta, ANTES da música existir.
        yield return StartCoroutine(songManager.PrepareRoutine());
        yield return StartCoroutine(noteSpawner.PrepareRoutine());

        // 2) Limpa o lixo da pré-carga agora, para o GC não rodar no meio da música
        yield return null;
        System.GC.Collect();
        yield return null;

        if (!songManager.IsReady || !noteSpawner.IsReady)
        {
            Debug.LogError("GameManager: pré-carga falhou, partida não iniciada.");
            yield break;
        }

        yield return new WaitForSeconds(startDelay);

        // 3) A música só começa quando a tela abre (songDelay do SongManager vira o "ready, set, go")
        _wipeController.StartWipeOut(() =>
        {
            _menuController.Liberar();
            songManager.StartSong();
            OnMatchStarted();
        });
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