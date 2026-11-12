using System.Collections;
using UnityEngine;

/// <summary>Cena de resultado: espera a tela abrir e toca a animação de estrelas.</summary>
public class ResultSceneController : MonoBehaviour
{
    [SerializeField] private StarResultController starResult;

    [Tooltip("Espera depois da tela abrir, antes da animação começar")]
    [SerializeField] private float startDelay = 0.3f;

    private IEnumerator Start()
    {
        GameCursor.Show(true);

        yield return SceneTransition.WaitForIdle();
        yield return new WaitForSeconds(startDelay);

        starResult.Play();
    }
}
