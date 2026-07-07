using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private int contadorCliques = 0;
    public TMPro.TextMeshProUGUI attackText;
    public Animator animator_knife;
    public Animator animator_hammer;

    private void Start()
    {
        contadorCliques = 0;
        attackText.text = "Cliques: " + contadorCliques;
    }

    public void Update()
    {
        attackText.text = "Cliques: " + contadorCliques;
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
    }
}
