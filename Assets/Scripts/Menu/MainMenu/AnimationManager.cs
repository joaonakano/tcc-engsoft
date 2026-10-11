using System.Collections;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator[] _animators;
    [SerializeField] private float _animationDelay = 0.5f;

    private void Start()
    {
        if (_animators == null || _animators.Length == 0)
            Debug.LogError("AnimationManager: Animators não foram configurados.");
    }

    private IEnumerator PlayAnimationCoroutine()
    {
        yield return new WaitForSeconds(_animationDelay);
        PlayStartAnimation();
    }

    public void PlayStartAnimation()
    {
        foreach (var animator in _animators)
        {
            if (animator != null)
            {
                animator.SetTrigger("Start");
            }
        }
    }
}
