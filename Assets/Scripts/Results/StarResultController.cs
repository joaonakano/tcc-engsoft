using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class StarResultController : MonoBehaviour
{
    [Serializable]
    private class StarSlot
    {
        [Tooltip("Estrela vazia (o molde/delineado). Fica sempre visível até a estrela ser conquistada")]
        public Transform emptyStar;

        [Tooltip("Estrela cheia (começa desativada)")]
        public Transform filledStar;

        [Tooltip("Porcentagem do score máximo necessária (0 a 1)")]
        [Range(0f, 1f)] public float requiredPercent = 0.5f;
    }

    [Header("Referências")]
    [SerializeField] private Transform liquid;
    [SerializeField] private StarSlot[] stars = new StarSlot[3];

    [Header("SFX")]
    [SerializeField] private AudioSource scoreThemeSource;

    [Header("Teste (opcional)")]
    [Tooltip("Se marcado, ignora o GameSession e usa os valores abaixo")]
    [SerializeField] private bool useTestScore = false;
    [SerializeField] private int testScore = 800;
    [SerializeField] private int testMaxScore = 1000;

    [Header("Líquido")]
    [Tooltip("Posição Y local do líquido com o copo vazio")]
    [SerializeField] private float emptyY = 0f;
    [Tooltip("Posição Y local do líquido com o copo cheio")]
    [SerializeField] private float fullY = 3f;
    [SerializeField] private float liquidRiseDuration = 0.8f;
    [SerializeField] private AnimationCurve liquidCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.8f;
    [SerializeField] private float delayBetweenStars = 0.9f;

    [Header("1) Estrela vazia diminui")]
    [Tooltip("Escala final do molde antes de sumir (relativa ao tamanho original)")]
    [SerializeField] private float shrinkScale = 0.6f;
    [SerializeField] private float shrinkDuration = 0.15f;

    [Header("2) Estrela cheia cresce (pop up)")]
    [Tooltip("Escala com que a estrela cheia aparece (normalmente igual ao Shrink Scale)")]
    [SerializeField] private float filledStartScale = 0.6f;
    [Tooltip("Escala máxima do pop, passando do tamanho normal")]
    [SerializeField] private float popOvershootScale = 1.3f;
    [SerializeField] private float growDuration = 0.18f;

    [Header("3) Volta ao tamanho normal")]
    [SerializeField] private float settleDuration = 0.15f;

    [Header("Eventos opcionais")]
    public UnityEvent<int> onStarEarned;      // passa 1, 2 ou 3
    public UnityEvent onSequenceFinished;

    public int StarsEarned { get; private set; }

    private Vector3[] emptyBaseScales;
    private Vector3[] filledBaseScales;

    private void Awake()
    {
        if (liquid == null)
            Debug.LogError("StarResultController: o campo 'Liquid' está vazio!", this);

        Audio.RegisterMusic(scoreThemeSource);

        emptyBaseScales = new Vector3[stars.Length];
        filledBaseScales = new Vector3[stars.Length];

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i].emptyStar == null || stars[i].filledStar == null)
            {
                Debug.LogError($"StarResultController: estrela {i} está sem referência (vazia ou cheia)!", this);
                continue;
            }

            emptyBaseScales[i] = stars[i].emptyStar.localScale;
            filledBaseScales[i] = stars[i].filledStar.localScale;
        }

        ResetVisuals();
    }

    // Chame quando a tela de resultado estiver visível (chamado pelo ResultSceneController)
    [ContextMenu("Play")]
    public void Play()
    {
        int score, maxScore;

        if (useTestScore)
        {
            score = testScore;
            maxScore = testMaxScore;
        }
        else
        {
            score = GameSession.Score;
            maxScore = GameSession.MaxScore;
        }

        PlayWithValues(score, maxScore);
    }

    public void PlayWithValues(int score, int maxScore)
    {
        StopAllCoroutines();
        StartCoroutine(PlaySequence(score, maxScore));
    }

    private IEnumerator PlaySequence(int score, int maxScore)
    {
        ResetVisuals();

        if (maxScore <= 0)
        {
            Debug.LogWarning($"StarResultController: maxScore é {maxScore}. " +
                             "Preencha o Test Score ou verifique se o GameSession foi salvo.", this);
        }

        float percent = maxScore > 0 ? Mathf.Clamp01((float)score / maxScore) : 0f;

        // Partida real: as estrelas já foram decididas (e salvas) pelo ScoreManager; aqui só se mostra.
        // Só o modo de teste calcula pelos limiares deste componente.
        StarsEarned = useTestScore ? CountStars(percent) : Mathf.Clamp(GameSession.Stars, 0, stars.Length);

        Debug.Log($"Score: {score}/{maxScore} ({percent * 100f:F1}%) → {StarsEarned} estrela(s)");

        yield return new WaitForSeconds(initialDelay);

        for (int i = 0; i < StarsEarned; i++)
        {
            // 1) O molde diminui
            yield return StartCoroutine(ShrinkEmpty(i));

            // 2) e 3) A cheia aparece pequena, cresce e volta ao normal.
            // O líquido sobe ao mesmo tempo.
            float targetFill = (i + 1) / (float)stars.Length;
            Coroutine pop = StartCoroutine(PopFilled(i));
            Coroutine rise = StartCoroutine(RiseLiquid(targetFill));

            yield return pop;
            yield return rise;

            onStarEarned?.Invoke(i + 1);

            if (i < StarsEarned - 1)
                yield return new WaitForSeconds(delayBetweenStars);
        }

        onSequenceFinished?.Invoke();
    }

    private int CountStars(float percent)
    {
        int count = 0;
        for (int i = 0; i < stars.Length; i++)
        {
            if (percent >= stars[i].requiredPercent) count = i + 1;
        }
        return count;
    }

    // 1) Estrela vazia diminui e some
    private IEnumerator ShrinkEmpty(int index)
    {
        Transform empty = stars[index].emptyStar;
        Vector3 baseScale = emptyBaseScales[index];

        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);

            // Ease in: começa devagar e acelera, dando sensação de "puxar" antes do pop
            float eased = t * t;

            empty.localScale = baseScale * Mathf.Lerp(1f, shrinkScale, eased);
            yield return null;
        }

        // Some o molde e devolve a escala original (para o Reset funcionar)
        empty.gameObject.SetActive(false);
        empty.localScale = baseScale;
    }

    // 2) e 3) Estrela cheia aparece pequena, cresce além do normal e assenta
    private IEnumerator PopFilled(int index)
    {
        Transform filled = stars[index].filledStar;
        Vector3 baseScale = filledBaseScales[index];

        filled.localScale = baseScale * filledStartScale;
        filled.gameObject.SetActive(true);

        // Crescimento: pequena → overshoot (ease out)
        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            filled.localScale = baseScale * Mathf.Lerp(filledStartScale, popOvershootScale, eased);
            yield return null;
        }

        // Assenta: overshoot → tamanho normal (ease in out)
        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            filled.localScale = baseScale * Mathf.Lerp(popOvershootScale, 1f, eased);
            yield return null;
        }

        filled.localScale = baseScale;
    }

    private IEnumerator RiseLiquid(float targetFill)
    {
        float startY = liquid.localPosition.y;
        float targetY = Mathf.Lerp(emptyY, fullY, targetFill);

        float elapsed = 0f;
        while (elapsed < liquidRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = liquidCurve.Evaluate(Mathf.Clamp01(elapsed / liquidRiseDuration));
            SetLiquidY(Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        SetLiquidY(targetY);
    }

    private void SetLiquidFill(float fill01)
    {
        SetLiquidY(Mathf.Lerp(emptyY, fullY, fill01));
    }

    private void SetLiquidY(float y)
    {
        if (liquid == null) return;

        Vector3 p = liquid.localPosition;
        p.y = y;
        liquid.localPosition = p;
    }

    private void ResetVisuals()
    {
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i].emptyStar != null)
            {
                stars[i].emptyStar.localScale = emptyBaseScales[i];
                stars[i].emptyStar.gameObject.SetActive(true);
            }

            if (stars[i].filledStar != null)
            {
                stars[i].filledStar.localScale = filledBaseScales[i];
                stars[i].filledStar.gameObject.SetActive(false);
            }
        }

        SetLiquidFill(0f);
    }
}