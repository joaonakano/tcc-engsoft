using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private int contadorCliques = 0;
    public TMPro.TextMeshProUGUI attackText;
    public Animator animator_knife;
    public Animator animator_hammer;

    [Header("Referencia ao Placar")]
    public ScoreDisplayController scoreDisplay;

    // Evento de ataque do jogador
    public event Action<string> OnAttack;

    private void Start()
    {
        contadorCliques = 0;

        if (scoreDisplay != null)
        {
            scoreDisplay.UpdateScoreUI(contadorCliques);
        }
    }

    public void OnHammerAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformAttack("hammer", animator_hammer);
        }
    }

    public void OnKnifeAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformAttack("slash", animator_knife);
        }

    }

    private void PerformAttack(string triggerName, Animator targetAnimator)
    {
        targetAnimator.ResetTrigger(triggerName);
        targetAnimator.SetTrigger(triggerName);

        contadorCliques++;

        if (scoreDisplay != null)
        {
            scoreDisplay.UpdateScoreUI(contadorCliques);
        }

        OnAttack?.Invoke(triggerName);
    }
}
