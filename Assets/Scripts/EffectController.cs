using UnityEngine;

public class EffectController : MonoBehaviour
{
    [Header("Configurações de Efeito 2D")]
    public GameObject smokePrefab;
    public Transform hitPoint;

    public void TriggerImpactEffect()
    {
        if (smokePrefab != null && hitPoint != null)
        {
            GameObject smokeEffect = Instantiate(smokePrefab, hitPoint.position, Quaternion.identity);

            Animator anim = smokeEffect.GetComponent<Animator>();
            if (anim != null)
            {
                AnimationClip clip = anim.runtimeAnimatorController.animationClips[0];
                Destroy(smokeEffect, clip.length + 0.1f);
            }
            else
            {
                Destroy(smokeEffect, 0.5f);
            }
        }
    }
}
