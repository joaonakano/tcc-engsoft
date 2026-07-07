using UnityEngine;

public class AnimatorBrain : MonoBehaviour
{
    private readonly static int[] animations =
    {
        Animator.StringToHash("Idle"),
        Animator.StringToHash("Hammer Attack"),
        Animator.StringToHash("Knife Attack"),
    };

    private Animator animator;
    private Animations[] currentAnimation;


}

public enum Animations
{
    IDLE,
    HAMMER_ATTACK,
    KNIFE_ATTACK,
    NONE,
}