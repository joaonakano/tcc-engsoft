using UnityEngine;

public class mlDisplayController : MonoBehaviour
{
    [Header("Sprites dos Números (0 ao 9)")]
    public Sprite[] numberSprites; // Array de sprites para os números de 0 a 9

    [Header("Imagens da UI do Placar")]
    public GameObject[] scoreDigitImages; // Componentes Image da UI (Milhar, Centena, Dezena, Unidade)

    // Chame a função UpdateScoreUI sempre que o placar for atualizado
    public void UpdateScoreUI(int currentScore)
    {
        string scoreString = currentScore.ToString().PadLeft(scoreDigitImages.Length, '0'); // Preenche com zeros à esquerda se necessário
        for (int i = 0; i < scoreDigitImages.Length; i++)
        {
            // Pega o caractere atual (ex: '4') e converte para número inteiro (ex: 4)
            int digit = int.Parse(scoreString[i].ToString());

            scoreDigitImages[i].GetComponent<SpriteRenderer>().sprite = numberSprites[digit]; // Atualiza o sprite da imagem correspondente ao dígito
        }
    }
}
