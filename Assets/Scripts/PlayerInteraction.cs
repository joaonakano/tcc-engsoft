using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    
    public Animator animator_knife;
    public Animator animator_hammer;

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
        Debug.Log("Ataque executado: " + triggerName);
    }
}
