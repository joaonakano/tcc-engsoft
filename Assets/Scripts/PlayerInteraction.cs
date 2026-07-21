using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private int contadorCliques = 0;
    public TMPro.TextMeshProUGUI attackText;
    public Animator animator_knife;
    public Animator animator_hammer;

    [Header("Referencia ao Placar")]
    public ScoreDisplayController scoreDisplay; // Arraste o objeto PlacarUI aqui no Inspector

    private void Start()
    {
        contadorCliques = 0;

        // Atualiza o placar com 0 logo no início do jogo
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
        Debug.Log("Ataque executado: " + triggerName);
        // Envia o novo valor do contador para o script do placar em pixel art!
        if (scoreDisplay != null)
        {
            scoreDisplay.UpdateScoreUI(contadorCliques);
        }
    }
}
