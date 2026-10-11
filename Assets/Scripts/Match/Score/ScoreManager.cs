using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private ScoreDisplayController scoreDisplay;

    [Header("Pontuação")]
    [SerializeField] private int perfectPoints = 5;
    [SerializeField] private int goodPoints = 2;

    public int CurrentScore { get; private set; }
    public int MaxPossibleScore { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CurrentScore = 0;
        UpdateUI();
    }

    public void ConfigureForSong(int totalNotes)
    {
        MaxPossibleScore = totalNotes * perfectPoints;
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        UpdateUI();
    }

    // e chamado quando termina a partida
    public void SaveResult()
    {
        GameResult.Score = CurrentScore;
        GameResult.MaxScore = MaxPossibleScore;
    }

    public int GetPerfectPoints() => perfectPoints;
    public int GetGoodPoints() => goodPoints;


    private void UpdateUI()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.UpdateScoreUI(CurrentScore);
        }
    }
}