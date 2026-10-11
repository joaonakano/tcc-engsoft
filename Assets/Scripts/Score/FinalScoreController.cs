using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class FinalScoreController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform liquid;
    [SerializeField] private Animator scoreHudAnimator;
    [SerializeField] private WipeController wipeController;

    public mlDisplayController mlDisplay;

    [Header("Liquid Settings")]
    [SerializeField] private float maxY = 3f;
    [SerializeField] private float fillDuration = 2f;

    [Header("Settings")]
    [SerializeField] private float startDelay = 2f;

    [Header("Results")]
    [SerializeField] private int maxMl = 500;

    public int scoreTeste = 1000;

    private float initialY;

    private void Start()
    {
        // Guarda a posição inicial do líquido.
        initialY = liquid.position.y;

        StartCoroutine(StartScoreSequence());
    }

    private IEnumerator StartScoreSequence()
    {
        yield return new WaitForSeconds(startDelay);
        wipeController.StartWipeOut(() => { StartFinalScore(); });
    }

    public void StartFinalScore()
    {
        int score = GameResult.Score;
        int maxScore = GameResult.MaxScore;

        if (maxScore <= 0)
        {
            Debug.LogError("MaxPossibleScore é zero ou negativo.");
            return;
        }

        scoreHudAnimator.SetTrigger("startAnimation");

        StartCoroutine(ShowFinalScore(score, maxScore));
    }

    private IEnumerator ShowFinalScore(int score, int maxScore)
    {
        mlDisplay.UpdateScoreUI(0);

        // Reseta o líquido.
        Vector3 position = liquid.position;
        position.y = initialY;
        liquid.position = position;

        // =========================
        // PORCENTAGEM DO SCORE
        // =========================

        float normalizedScore = Mathf.Clamp01(
            (float)score / maxScore
        );

        // Converte a porcentagem em posição Y.
        float targetY = Mathf.Lerp(
            initialY,
            maxY,
            normalizedScore
        );

        // Calcula o resultado final em mL.
        int ml = Mathf.RoundToInt(
            normalizedScore * maxMl
        );

        float elapsed = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fillDuration;

            // Suaviza a animação.
            t = Mathf.SmoothStep(0f, 1f, t);

            float currentY = Mathf.Lerp(
                initialY,
                targetY,
                t
            );

            Vector3 currentPosition = liquid.position;
            currentPosition.y = currentY;
            liquid.position = currentPosition;

            // =========================
            // CONTADOR DE mL
            // =========================

            int currentMl = Mathf.RoundToInt(
                Mathf.Lerp(0, ml, t)
            );

            mlDisplay.UpdateScoreUI(currentMl);

            yield return null;
        }

        // Garante que termina exatamente no ponto calculado.
        Vector3 finalPosition = liquid.position;
        finalPosition.y = targetY;
        liquid.position = finalPosition;

        // Garante o valor final do contador.
        mlDisplay.UpdateScoreUI(ml);

        Debug.Log(
            $"Score: {score}/{maxScore}"
        );

        Debug.Log(
            $"Desempenho: {normalizedScore * 100f:F1}%"
        );

        Debug.Log($"Resultado: {ml} mL");
    }

    
}