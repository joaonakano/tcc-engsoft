using System.Collections;
using UnityEngine;

/// <summary>
/// Orquestra a partida: prepara tudo com a tela coberta, começa a música quando a tela abre
/// e leva para o Score quando a música acaba.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private InGameMenuController _menuController;
    [SerializeField] private SongManager songManager;
    [SerializeField] private NoteSpawner noteSpawner;

    [Header("Configurações")]
    [SerializeField] private TransitionType transitionType = TransitionType.Loading;
    [Tooltip("Espera extra depois da tela abrir, antes de a música começar")]
    [SerializeField] private float startDelay = 0.5f;

    private bool matchEnded;

    private void Start()
    {
        GameCursor.Show(false);
        StartCoroutine(StartMatchSequence());
    }

    private void Update()
    {
        if (!matchEnded && songManager.HasFinished)
            EndMatch();
    }

    private IEnumerator StartMatchSequence()
    {
        // 1) A pré-carga conta como "loading": a tinta só sai quando ela termina
        yield return SceneTransition.Track(PrepareMatch());

        if (!songManager.IsReady || !noteSpawner.IsReady)
        {
            Debug.LogError("GameManager: pré-carga falhou, partida não iniciada.");
            yield break;
        }

        // 2) Espera a tela abrir por completo
        yield return SceneTransition.WaitForIdle();
        yield return new WaitForSeconds(startDelay);

        // 3) A música só começa agora (songDelay do SongManager vira o "ready, set, go")
        _menuController.Liberar();
        songManager.StartSong();
    }

    private IEnumerator PrepareMatch()
    {
        // clip na memória, notas pré-calculadas, pools aquecidos
        yield return songManager.PrepareRoutine();
        yield return noteSpawner.PrepareRoutine();

        // Limpa o lixo da pré-carga agora, para o GC não rodar no meio da música
        yield return null;
        System.GC.Collect();
        yield return null;
    }

    private void EndMatch()
    {
        matchEnded = true;
        ScoreManager.Current.SaveResult();
        SceneTransition.Load(Scenes.Score, transitionType);
    }
}
