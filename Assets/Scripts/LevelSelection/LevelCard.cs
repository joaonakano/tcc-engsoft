using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class LevelCard : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Fase")]
    [SerializeField] private string levelId = "Level_1";

    [Header("Visual do botão")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite highlightSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Estrelas")]
    [SerializeField] private SpriteRenderer[] stars;
    [SerializeField] private Sprite starFull;
    [SerializeField] private Sprite starEmpty;

    [Header("Clique")]
    public UnityEvent onClick;

    [Header("Transição")]
    [SerializeField] private TransitionType transitionType = TransitionType.Loading;

    private bool interactable;
    private bool hovering;
    private bool pressing;

    public string LevelId => levelId;

    private void OnEnable()
    {
        RefreshStars();
        UpdateVisual();
    }

    // ---------- Estrelas ----------
    public void RefreshStars()
    {
        int earned = GameSession.BestStars(levelId);
        SetStars(earned);
    }

    public void SetStars(int earned)
    {
        for (int i = 0; i < stars.Length; i++)
            stars[i].sprite = i < earned ? starFull : starEmpty;
    }

    // ---------- Interação ----------
    // Chamado pelo carrossel: só o card do centro é clicável
    public void SetInteractable(bool value)
    {
        interactable = value;

        // Não zera o hovering aqui: o mouse pode continuar sobre o card.
        // Só limpa o pressing, para não ficar preso no sprite de pressed.
        if (!value) pressing = false;

        UpdateVisual();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hovering = true;      // registra sempre, mesmo durante a animação
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData e)
    {
        hovering = false;
        pressing = false;
        UpdateVisual();
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (!interactable) return;
        pressing = true;
        UpdateVisual();
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressing = false;
        // No toque não existe hover, então não deixa preso no highlight
        if (e.pointerId >= 0) hovering = false;
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!interactable) return;
        onClick.Invoke();
        GameSession.StartLevel(levelId, transitionType);
    }

    private void UpdateVisual()
    {
        if (background == null) return;

        if (!interactable)      background.sprite = normalSprite;
        else if (pressing)      background.sprite = pressedSprite;
        else if (hovering)      background.sprite = highlightSprite;
        else                    background.sprite = normalSprite;
    }
}