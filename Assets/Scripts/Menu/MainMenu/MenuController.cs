using System.Collections;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AudioSource _backgroundAudioSource;
    [SerializeField] private AnimationManager _animationManager;

    [Header("Navegação")]
    [SerializeField] private string sceneToLoad = Scenes.LevelSelection;
    [SerializeField] private TransitionType transitionType = TransitionType.Simple;

    [SerializeField] private TransitionType settingsTransitionType = TransitionType.Simple;

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        GameCursor.Show(true);
        Audio.RegisterMusic(_backgroundAudioSource);

        // Animações e música começam junto com a tinta saindo
        yield return SceneTransition.WaitForReveal();
        _animationManager.PlayAll();
        _backgroundAudioSource.Play();
    }

    public void OnPlayPressed()
    {
        if (SceneTransition.IsBusy) return;

        SceneTransition.Load(sceneToLoad, transitionType);
    }

    public void OnSettingsPressed()
    {
        if (SceneTransition.IsBusy) return;

        SceneTransition.Load(Scenes.MenuSettings, settingsTransitionType);
    }

    public void OnExitPressed() => Application.Quit();
}
