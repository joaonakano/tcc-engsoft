using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Referências")]
    public ScoreDisplayController scoreDisplay;

    private int currentScore = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentScore = 0;
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateUI();
        Debug.Log($"Score: {currentScore}");
    }

    private void UpdateUI()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.UpdateScoreUI(currentScore);
        }
    }
}
