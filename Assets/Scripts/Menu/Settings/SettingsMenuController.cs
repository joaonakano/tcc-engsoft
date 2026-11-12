using System.Collections;
using UnityEngine;

/// <summary>
/// Cena MenuSettings: abre o menu (Animator, bool isOpen) assim que a transição de loading termina,
/// e volta para o Menu fechando a animação antes de chamar a transição.
/// </summary>
public class SettingsMenuController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openParameter = "isOpen";
    
    [Header("Audio")]
    [SerializeField] private AudioClip menuOpenCloseSfx;

    [Header("Navegação")]
    [SerializeField] private string returnScene = Scenes.Menu;
    [SerializeField] private TransitionType transitionType = TransitionType.Simple;
    [Tooltip("Estado do Animator em que o menu termina de fechar. A transição só começa quando ele é alcançado")]
    [SerializeField] private string closedStateName = "idle-closed";
    [Tooltip("Segurança: se o estado acima nunca for alcançado (nome errado), segue depois deste tempo")]
    [SerializeField] private float closeTimeout = 2f;

    [SerializeField]
    private AudioSource settingsMenuSource;

    private bool closing;

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        GameCursor.Show(true);
        Audio.RegisterMusic(settingsMenuSource);

        // O menu só abre quando a tela está 100% visível (tinta fora, input liberado)
        yield return SceneTransition.WaitForIdle();
        Audio.PlaySfx(menuOpenCloseSfx);
        animator.SetBool(openParameter, true);
    }

    /// <summary>Ligue ao botão Voltar.</summary>
    public void OnBackPressed()
    {
        if (closing || SceneTransition.IsBusy) return;
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        closing = true;
        animator.SetBool(openParameter, false);
        Audio.PlaySfx(menuOpenCloseSfx);

        // Espera a animação de fechar acabar de verdade (e não um tempo fixo): se a transição começasse antes,
        // o LoadSceneAsync travaria o Animator no meio do movimento.
        float elapsed = 0f;
        yield return null; // o Animator só reage ao bool no próximo frame
        while (elapsed < closeTimeout)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(closedStateName) && !animator.IsInTransition(0)) break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (elapsed >= closeTimeout)
            Debug.LogWarning($"SettingsMenuController: estado '{closedStateName}' não foi alcançado em {closeTimeout}s. Confira o nome no Animator.");

        yield return null; // deixa o último frame da animação ser desenhado antes da carga ocupar a thread
        SceneTransition.Load(returnScene, transitionType);
    }
}
