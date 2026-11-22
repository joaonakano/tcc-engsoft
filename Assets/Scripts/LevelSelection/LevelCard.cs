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

    [Header("Bloqueio")]
    [Tooltip("Objeto com o overlay cinza + cadeado, filho do card. Fica ativo enquanto a fase estiver bloqueada.")]
    [SerializeField] private GameObject lockOverlay;

    [Header("Clique")]
    public UnityEvent onClick;

    [Header("Transição")]
    [SerializeField] private TransitionType transitionType = TransitionType.Loading;

    private bool interactable;
    private bool locked;
    private bool hovering;
    private bool pressing;

    public string LevelId => levelId;
    public bool IsLocked => locked;

    private void OnEnable()
    {
        RefreshLock();
        RefreshStars();
        UpdateVisual();
    }

    // ---------- Bloqueio ----------
    public void RefreshLock()
    {
        LevelData level = Resources.Load<LevelData>("Levels/" + levelId);
        locked = level != null && !level.IsUnlocked;

        if (lockOverlay != null) lockOverlay.SetActive(locked);
        if (locked) pressing = false;
        UpdateVisual();
    }

    // ---------- Estrelas ----------
    public void RefreshStars()
    {
        LevelData level = Resources.Load<LevelData>("Levels/" + levelId);
        int earned = GameSession.BestStars(level != null ? level.Key : levelId);
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
        if (!interactable || locked) return;
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
        if (!interactable || locked) return;
        onClick.Invoke();
        GameSession.StartLevel(levelId, transitionType);
    }

    private void UpdateVisual()
    {
        if (background == null) return;

        if (!interactable || locked) background.sprite = normalSprite;
        else if (pressing)      background.sprite = pressedSprite;
        else if (hovering)      background.sprite = highlightSprite;
        else                    background.sprite = normalSprite;
    }
}