using System.Collections;
using UnityEngine;

/// <summary>
/// Painel "Hint" (ajuda) do menu principal.
/// Coloque este script em um objeto SEMPRE ATIVO (ex.: o mesmo do MenuController).
/// Não usa Animation Events: acompanha o estado do Animator (controller Hint, bool "isOpen").
///
/// Open()  (botão Help): ativa o Hint -> toca "opening" -> main-loop -> mostra o botão X.
/// Close() (botão X)   : esconde o X -> toca "closing" -> ao terminar, desativa o Hint.
/// </summary>
public class HintPanel : MonoBehaviour
{
    private static readonly int IsOpenHash = Animator.StringToHash("isOpen");

    [Header("Referências")]
    [Tooltip("GameObject 'Hint' (tem o Animator). Pode começar ativo ou inativo na cena.")]
    [SerializeField] private GameObject _hint;
    [SerializeField] private Animator _animator;
    [Tooltip("Botão X. Pode ser filho do Hint.")]
    [SerializeField] private GameObject _closeButton;

    [Header("Nomes dos estados no Animator")]
    [SerializeField] private string _loopState = "main-loop";
    [SerializeField] private string _closingState = "closing";

    private enum State { Closed, Opening, Open, Closing }
    private State _state = State.Closed;

    private void Awake()
    {
        if (_animator == null && _hint != null)
            _animator = _hint.GetComponent<Animator>();

        _closeButton.SetActive(false);
        _hint.SetActive(false);
    }

    /// <summary>Botão Help.</summary>
    public void Open()
    {
        if (_state != State.Closed) return;
        StartCoroutine(OpenRoutine());
    }

    /// <summary>Botão X.</summary>
    public void Close()
    {
        if (_state != State.Open) return;
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        _state = State.Opening;

        _hint.SetActive(true);                    // o Animator reinicia em "opening"
        _animator.SetBool(IsOpenHash, true);      // libera a transição opening -> main-loop

        // Espera a abertura terminar e o Animator entrar no loop principal
        while (!IsInState(_loopState))
            yield return null;

        _closeButton.SetActive(true);
        _state = State.Open;
    }

    private IEnumerator CloseRoutine()
    {
        _state = State.Closing;

        _closeButton.SetActive(false);
        _animator.SetBool(IsOpenHash, false);     // main-loop -> closing

        // Espera entrar em "closing" e o clip terminar
        while (!IsInState(_closingState) || _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        _hint.SetActive(false);
        _state = State.Closed;
    }

    // Em um estado e não no meio de uma transição
    private bool IsInState(string stateName) =>
        !_animator.IsInTransition(0) && _animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
}
