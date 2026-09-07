using System.Collections;
using UnityEngine;
using TMPro;

public class FinalScoreController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField]
    private Transform liquid;

    [SerializeField]
    private TMP_Text mlText;

    [Header("Configurações do Líquido")]
    [SerializeField]
    private float maxY = 3f;

    [SerializeField]
    private int maxScore = 1000;

    [SerializeField]
    private float fillDuration = 2f;

    [Header("Resultado")]
    [SerializeField]
    private int maxMl = 500;

    [Header("Teste")]
    [SerializeField]
    private int scoreVindoDoManager = 1000;

    private float initialY;

    private void Start()
    {
        // Guarda a posição inicial do líquido.
        initialY = liquid.position.y;

        int score = ScoreManager.Instance.CurrentScore;

        StartCoroutine(ShowFinalScore(score));
    }

    private IEnumerator ShowFinalScore(int score)
    {
        mlText.text = "0";

        // Garante que o líquido começa na posição original.
        Vector3 position = liquid.position;
        position.y = initialY;
        liquid.position = position;

        // Converte o score para uma porcentagem de 0 a 1.
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

            mlText.text = $"{currentMl}";

            yield return null;
        }

        // Garante que termina exatamente no ponto calculado.
        Vector3 finalPosition = liquid.position;
        finalPosition.y = targetY;
        liquid.position = finalPosition;

        // Garante o valor final do contador.
        mlText.text = $"{ml}";

        Debug.Log($"Score: {score}");
        Debug.Log($"Resultado: {ml} mL");
    }

    private void ShowMl(int ml)
    {
        Debug.Log($"Você conseguiu {ml} mL!");
    }
}