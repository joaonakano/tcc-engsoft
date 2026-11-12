using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Menu de pausa da partida.</summary>
public class InGameMenuController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator _menuAnimator;
    [SerializeField] private Animator _settingsAnimator;
    [SerializeField] private AudioSource _backgroundAudioSource;
    [SerializeField] private GrayscaleFilter _filter;
    public SongManager _songManager;

    [Header("SFX")]
    [SerializeField] private AudioClip firstMenuClip;
    [SerializeField] private AudioClip secondMenuClip;

    [Header("Navegação")]
    [SerializeField] private TransitionType transitionType = TransitionType.Loading;

    private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

    private bool _isMenuOpen;
    private bool _isSettingsOpen;
    private bool _menuLiberado;

    private void Start() => Audio.RegisterMusic(_backgroundAudioSource);

    /// <summary>Chamado pelo GameManager quando a partida começa (antes disso o menu não abre).</summary>
    public void Liberar() => _menuLiberado = true;

    // Ligado ao input de pausa
    public void AbrirMenu(InputAction.CallbackContext context)
    {
        if (!context.performed || SceneTransition.IsBusy) return;

        if (_isMenuOpen) FecharMenu();
        else AbrirMenuInterno();
    }

    private void AbrirMenuInterno()
    {
        if (!_menuLiberado || !_songManager.CanPause) return;

        if (_filter != null) _filter.Show();
        _songManager.PauseSong();
        GameCursor.Show(true);
        _backgroundAudioSource.Play();
        Audio.PlaySfx(firstMenuClip);

        _menuAnimator.ResetTrigger("MenuClosing");
        _menuAnimator.SetTrigger("MenuOpening");
        _isMenuOpen = true;
    }

    public void FecharMenu()
    {
        if (!_isMenuOpen || SceneTransition.IsBusy) return;

        if (_filter != null) _filter.Hide();
        GameCursor.Show(false);
        _backgroundAudioSource.Stop();
        Audio.PlaySfx(firstMenuClip);

        _menuAnimator.ResetTrigger("MenuOpening");
        _menuAnimator.SetTrigger("MenuClosing");
        _isMenuOpen = false;

        SetSettingsOpen(false);
        _songManager.UnpauseSong();
    }

    public void RestartarLevel() => GameSession.Retry(transitionType);

    public void VoltarMenuInicial() => SceneTransition.Load(Scenes.Menu, transitionType);

    public void AbrirSettings()
    {
        if (_settingsAnimator == null) return;
        Audio.PlaySfx(secondMenuClip);
        SetSettingsOpen(!_isSettingsOpen);
    }

    private void SetSettingsOpen(bool open)
    {
        if (_settingsAnimator == null || _isSettingsOpen == open) return;

        _isSettingsOpen = open;
        _settingsAnimator.SetBool(IsOpenHash, open);
    }
}
