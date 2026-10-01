using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Referências")]
    [SerializeField]
    private ScoreDisplayController scoreDisplay;

    [Header("Pontuação")]
    [SerializeField]
    private int perfectPoints = 5;

    [SerializeField]
    private int goodPoints = 2;

    public int CurrentScore { get; private set; }

    public int TotalNotes { get; private set; }

    public int MaxPossibleScore { get; private set; }

    private void Awake()
    {
        // Se já existe um ScoreManager, destrói este.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ResetScore();
    }

    public void ConfigureForSong(int totalNotes)
    {
        TotalNotes = totalNotes;

        MaxPossibleScore =
            TotalNotes * perfectPoints;

        Debug.Log(
            $"Score configurado: " +
            $"{TotalNotes} notas | " +
            $"Máximo: {MaxPossibleScore}"
        );
    }

    public void ResetScore()
    {
        CurrentScore = 0;

        UpdateUI();
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;

        UpdateUI();

        Debug.Log($"Score: {CurrentScore}");
    }

    public float GetScorePercentage()
    {
        if (MaxPossibleScore <= 0)
            return 0f;

        return Mathf.Clamp01(
            (float)CurrentScore / MaxPossibleScore
        );
    }

    public int GetPerfectPoints()
    {
        return perfectPoints;
    }

    public int GetGoodPoints()
    {
        return goodPoints;
    }

    private void UpdateUI()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.UpdateScoreUI(CurrentScore);
        }
    }
}