using System.Collections.Generic;
using UnityEngine;

public enum Reactions
{
    Dead,
    Bad,
    Sad,
    Neutral,
    Good,
}

public class ReactionManager : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] SpriteRenderer reactionSpriteRenderer;
    [SerializeField] private Animator reactionAnimator;

    [Tooltip("Insira, de menor para maior ordem, os sprites para MORTO, PESSIMO, RUIM, MEDIO, BOM")]
    [SerializeField] List<Sprite> reactionSprites = new List<Sprite>();

    private bool hasReaction;

    public Reactions CurrentReaction { get; private set; }

    private void Start()
    {
        if (reactionSprites == null || reactionSprites.Count == 0) Debug.LogError("Os sprites necessarios nao foram informados para as reacoes.");

        if (reactionSpriteRenderer == null) Debug.LogError("Nenhum SpriteRenderer configurado no ReactionManager");

        ApplyState(Reactions.Good, playAnimation: false);
    }

    public void ChangeCurrentStateTo(Reactions value)
    {
        if (hasReaction && value == CurrentReaction) return;

        ApplyState(value, playAnimation: true);
    }

    private void ApplyState(Reactions value, bool playAnimation)
    {
        Reactions previous = CurrentReaction;
        bool hadReaction = hasReaction;

        CurrentReaction = value;
        hasReaction = true;

        SetSprite((int)value);

        if (playAnimation && hadReaction)
            PlayTransitionAnimation(value > previous);
    }

    private void SetSprite(int index)
    {
        if (reactionSpriteRenderer == null || reactionSprites == null) return;
        if (index < 0 || index >= reactionSprites.Count) return;

        reactionSpriteRenderer.sprite = reactionSprites[index];
    }

    private void PlayTransitionAnimation(bool improved)
    {
        if (reactionAnimator == null) return;

        reactionAnimator.ResetTrigger("ChangeReaction");
        reactionAnimator.SetTrigger("ChangeReaction");
    }
}
