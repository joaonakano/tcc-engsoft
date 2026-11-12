using UnityEngine;

/// <summary>Pontuação da partida. Reage a MatchEvents.Judged; não é chamado pelo HitManager.</summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>ScoreManager da cena atual.</summary>
    public static ScoreManager Current { get; private set; }

    [Header("Referencias")]
    [SerializeField] private ScoreDisplayController scoreDisplay;

    [Header("Pontuacao")]
    [SerializeField] private int perfectPoints = 5;
    [SerializeField] private int goodPoints = 2;

    [Header("Estrelas")]
    [Tooltip("Fração do score máximo para 1, 2 e 3 estrelas. Esta é a ÚNICA definição: a tela de resultado e a seleção de fase mostram o que for decidido aqui")]
    [SerializeField] private float[] starThresholds = { 0.30f, 0.55f, 0.80f };

    public int CurrentScore { get; private set; }

    /// <summary>Pontuação de uma partida perfeita (todas as notas no PERFECT).</summary>
    public int MaxPossibleScore
    {
        get
        {
            var beatmap = SongManager.Current != null ? SongManager.Current.Beatmap : null;
            return beatmap != null ? beatmap.NoteCount * perfectPoints : 0;
        }
    }

    private void Awake() => Current = this;

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private bool failed;

    private void OnEnable()
    {
        MatchEvents.Judged += OnJudged;
        MatchEvents.Failed += OnFailed;
    }

    private void OnFailed() => failed = true;
    private void OnDisable()
    {
        MatchEvents.Judged -= OnJudged;
        MatchEvents.Failed -= OnFailed;
    }

    private void Start() => RefreshDisplay();

    private void RefreshDisplay()
    {
        if (scoreDisplay != null) scoreDisplay.UpdateScoreUI(CurrentScore);
    }

    private void OnJudged(Judgement judgement)
    {
        int points = judgement switch
        {
            Judgement.Perfect => perfectPoints,
            Judgement.Good => goodPoints,
            _ => 0
        };

        if (points == 0) return;

        CurrentScore += points;
        RefreshDisplay();
    }

    /// <summary>Chamado ao fim da partida: calcula estrelas e entrega o resultado ao GameSession.</summary>
    public void SaveResult()
    {
        int max = MaxPossibleScore;
        float percent = max > 0 ? (float)CurrentScore / max : 0f;

        int stars = 0;
        for (int i = 0; i < starThresholds.Length; i++)
            if (percent >= starThresholds[i]) stars = i + 1;

        // Perdeu = 0 estrelas e nada é salvo, mesmo que a pontuação parcial alcançasse algum limiar
        GameSession.SaveResult(CurrentScore, max, failed ? 0 : stars, failed);
    }
}
