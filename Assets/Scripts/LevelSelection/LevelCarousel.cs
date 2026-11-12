using System.Collections;
using UnityEngine;

public class LevelCarousel : MonoBehaviour
{

    [Header("Selection Theme")]
    [SerializeField] private AudioSource levelSelectionSource;
    
    [Header("Cards (na ordem das fases)")]
    [SerializeField] private Transform[] cards;

    [Header("Slots (GameObjects vazios na cena)")]
    [SerializeField] private Transform slotCenter;
    [SerializeField] private Transform slotLeft;
    [SerializeField] private Transform slotRight;
    [SerializeField] private Transform slotHiddenLeft;
    [SerializeField] private Transform slotHiddenRight;

    [Header("Animação")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private LevelCard[] levelCards;

    private int currentIndex = 0;
    private bool isMoving = false;

    private void Start()
    {

        levelCards = new LevelCard[cards.Length];
        Audio.RegisterMusic(levelSelectionSource);

        for (int i = 0; i < cards.Length; i++)
            levelCards[i] = cards[i].GetComponent<LevelCard>();

        // Posiciona tudo sem animação
        for (int i = 0; i < cards.Length; i++)
        {
            Transform slot = GetSlot(GetOffset(i, currentIndex));
            ApplySlot(cards[i], slot);
        }
        
        GameCursor.Show(true);

        RefreshInteractable();
    }

    // ---- Ligue estes dois métodos no OnClick dos botões ----
    public void Next()     { Move(+1); }
    public void Previous() { Move(-1); }

    public void BackToMenu() { SceneTransition.Load(Scenes.Menu, TransitionType.Simple); }

    private void Move(int direction)
    {
        if (isMoving || cards.Length < 2) return;

        int oldIndex = currentIndex;
        currentIndex = (currentIndex + direction + cards.Length) % cards.Length;

        StartCoroutine(MoveRoutine(oldIndex, currentIndex, direction));
    }

    private void RefreshInteractable()
    {
        for (int i = 0; i < levelCards.Length; i++)
            if (levelCards[i] != null)
                levelCards[i].SetInteractable(!isMoving && i == currentIndex);
    }

    private IEnumerator MoveRoutine(int oldIndex, int newIndex, int direction)
{
    isMoving = true;

    RefreshInteractable();

    int n = cards.Length;
    var startPos = new Vector3[n];
    var startRot = new Quaternion[n];
    var startScale = new Vector3[n];
    var animTarget = new Transform[n];
    var finalSlot = new Transform[n];

    // Slot escondido por onde os cards ENTRAM e por onde SAEM
    Transform enterSlot = direction > 0 ? slotHiddenRight : slotHiddenLeft;
    Transform exitSlot  = direction > 0 ? slotHiddenLeft  : slotHiddenRight;

    for (int i = 0; i < n; i++)
    {
        int oldOffset = GetOffset(i, oldIndex);
        int newOffset = GetOffset(i, newIndex);

        finalSlot[i] = GetSlot(newOffset);
        animTarget[i] = finalSlot[i];

        bool wrapped = Mathf.Abs(newOffset - oldOffset) > 1;
        bool wasVisible = Mathf.Abs(oldOffset) <= 1;

        if (wrapped)
        {
            if (wasVisible)
            {
                // Estava na tela e deu a volta: sai pelo lado do movimento
                animTarget[i] = exitSlot;
            }
            else
            {
                // Estava escondido e vai entrar: teleporta para o lado
                // de entrada (fora da tela) antes de animar
                ApplySlot(cards[i], enterSlot);
            }
        }

        startPos[i] = cards[i].position;
        startRot[i] = cards[i].rotation;
        startScale[i] = cards[i].localScale;
    }

    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / moveDuration;
        float k = ease.Evaluate(Mathf.Clamp01(t));

        for (int i = 0; i < n; i++)
        {
            cards[i].position = Vector3.LerpUnclamped(startPos[i], animTarget[i].position, k);
            cards[i].rotation = Quaternion.SlerpUnclamped(startRot[i], animTarget[i].rotation, k);
            cards[i].localScale = Vector3.LerpUnclamped(startScale[i], animTarget[i].localScale, k);
        }
        yield return null;
    }

    for (int i = 0; i < n; i++)
        ApplySlot(cards[i], finalSlot[i]);

    isMoving = false;

    RefreshInteractable();
}

    // Distância circular do card até o card atual (-n/2 ... +n/2)
    private int GetOffset(int cardIndex, int centerIndex)
    {
        int n = cards.Length;
        int offset = ((cardIndex - centerIndex) % n + n) % n;
        if (offset > n / 2) offset -= n;
        return offset;
    }

    private Transform GetSlot(int offset)
    {
        if (offset == 0)  return slotCenter;
        if (offset == -1) return slotLeft;
        if (offset == 1)  return slotRight;
        return offset < 0 ? slotHiddenLeft : slotHiddenRight;
    }

    private void ApplySlot(Transform card, Transform slot)
    {
        card.position = slot.position;
        card.rotation = slot.rotation;
        card.localScale = slot.localScale;
    }
}