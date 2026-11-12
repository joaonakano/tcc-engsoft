using UnityEngine;

/// <summary>
/// Aplica o LevelTheme da fase atual (GameSession.Level.theme) na cena.
/// Se a fase não tiver theme, usa o defaultTheme (ou deixa a cena como está).
/// </summary>
public class ThemeApplier : MonoBehaviour
{
    [SerializeField] private LevelTheme defaultTheme;

    [Header("Alvos na cena")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer boardLeft;
    [SerializeField] private SpriteRenderer boardRight;
    [SerializeField] private Animator[] dispensers;

    [Header("Barra de progresso")]
    [Tooltip("SpriteRenderer do ícone do ponteiro (o filho com o sprite, não o pai que se move)")]
    [SerializeField] private SpriteRenderer pointerRenderer;
    [Tooltip("SpriteRenderer da barra, que recebe o material/shader")]
    [SerializeField] private SpriteRenderer progressBarRenderer;

    private void Awake()
    {
        var level = GameSession.Level;
        Apply(level != null && level.theme != null ? level.theme : defaultTheme);
    }

    public void Apply(LevelTheme t)
    {
        if (t == null) return;

        if (t.background && background) background.sprite = t.background;
        if (t.boardLeft && boardLeft) boardLeft.sprite = t.boardLeft;
        if (t.boardRight && boardRight) boardRight.sprite = t.boardRight;

        if (t.pointerSprite && pointerRenderer) pointerRenderer.sprite = t.pointerSprite;
        if (t.progressBarMaterial && progressBarRenderer) progressBarRenderer.sharedMaterial = t.progressBarMaterial;

        if (t.dispenserAnimator)
            foreach (var d in dispensers)
                if (d) d.runtimeAnimatorController = t.dispenserAnimator;
    }
}
