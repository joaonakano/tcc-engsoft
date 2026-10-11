using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InGameMenuController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Animator _menuAnimator;
    [SerializeField] private Animator _settingsAnimator;
    [SerializeField] private WipeController _wipeController;
    [SerializeField] private string _menuSceneName = "M01";
    [SerializeField] private CursorManager _cursorManager;
    [SerializeField] private AudioSource _backgroundAudioSource;
    [SerializeField] private GrayscaleFilter _filter;

    [SerializeField] private AudioClip firstMenuClip;
    [SerializeField] private AudioClip secondMenuClip;

    public SongManager _songManager;

    private bool _isMenuOpen = false;
    private bool _menuLiberado = false;

    private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");
    private bool _isSettingsOpen = false;

    public void Liberar() => _menuLiberado = true;

    private void Start()
    {
        if (_menuAnimator == null)
            Debug.LogError("Menu Animator não foi configurado no InGameMenuController.");
        if (_songManager == null)
            Debug.LogError("SongManager não foi configurado no InGameMenuController.");
        if (_wipeController == null)
            Debug.LogError("WipeController não foi configurado no InGameMenuController.");
        if (_cursorManager == null)
            Debug.LogError("CursorManager não foi configurado no InGameMenuController.");
    }

    public void AbrirMenu(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_wipeController.IsTransitioning) return;

        if (_isMenuOpen)
            FecharMenu();
        else
            AbrirMenuInterno();
    }

    private void AbrirMenuInterno()
    {
        if (!_menuLiberado) return;
        if (_songManager == null || _menuAnimator == null) return;
        if (!_songManager.CanPause) return;

        _filter?.Show();

        _songManager.PauseSong();
        _cursorManager.HabilitarCursor(true);
        _backgroundAudioSource.Play();

        SFXManager.Instance?.PlaySFX(firstMenuClip);

        _menuAnimator.ResetTrigger("MenuClosing");
        _menuAnimator.SetTrigger("MenuOpening");
        _isMenuOpen = true;
    }

    public void FecharMenu()
    {
        if (!_isMenuOpen || _songManager == null || _menuAnimator == null) return;
        if (_wipeController.IsTransitioning) return;

        _filter?.Hide();

        _cursorManager.HabilitarCursor(false);
        _backgroundAudioSource.Stop();

        SFXManager.Instance?.PlaySFX(firstMenuClip);

        _menuAnimator.ResetTrigger("MenuOpening");
        _menuAnimator.SetTrigger("MenuClosing");
        _isMenuOpen = false;

        SetSettingsOpen(false);

        _songManager.UnpauseSong();
    }

    public void RestartarLevel()
    {
        _wipeController.StartWipeIn(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
        
    }

    public void VoltarMenuInicial()
    {
        _wipeController.StartWipeIn(() =>
        {
            SceneManager.LoadScene(_menuSceneName);
        });
    }

    public void AbrirSettings()
    {
        if (_settingsAnimator == null) return;
        SFXManager.Instance?.PlaySFX(secondMenuClip);
        SetSettingsOpen(!_isSettingsOpen);
    }

    private void SetSettingsOpen(bool open)
    {
        if (_settingsAnimator == null) return;
        if (_isSettingsOpen == open) return;          // nada mudou, não mexe no Animator

        _isSettingsOpen = open;
        _settingsAnimator.SetBool(IsOpenHash, open);
    }
}
