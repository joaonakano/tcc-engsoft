using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Referências")]
    public ScoreDisplayController scoreDisplay;

    public int CurrentScore { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ResetScore();
        
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

    private void UpdateUI()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.UpdateScoreUI(CurrentScore);
        }
    }
}
