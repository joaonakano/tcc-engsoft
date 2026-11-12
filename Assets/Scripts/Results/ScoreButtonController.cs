using UnityEngine;

/// <summary>Botões da tela de resultado. Ligue nos OnClick: VoltarMenu / EscolherFase / ResetarLevel.</summary>
public class ScoreButtonController : MonoBehaviour
{
    [SerializeField] private TransitionType transitionType = TransitionType.Loading;

    public void VoltarMenu() => SceneTransition.Load(Scenes.Menu, transitionType);

    public void EscolherFase() => SceneTransition.Load(Scenes.LevelSelection, transitionType);

    public void ResetarLevel() => GameSession.Retry(transitionType);
}
