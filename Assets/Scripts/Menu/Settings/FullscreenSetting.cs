using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Par de toggles Sim/Não do fullscreen (Panel_Video). Funciona como um grupo exclusivo: sempre um dos dois ligado.
/// A escolha é salva em PlayerPrefs e reaplicada na abertura do jogo (mesmo sem passar pela cena de configurações).
/// </summary>
public class FullscreenSetting : MonoBehaviour
{
    const string PrefKey = "settings.fullscreen";

    [SerializeField] private Toggle yesToggle;
    [SerializeField] private Toggle noToggle;
    [Header("SFX")]
    [Tooltip("Tocado sempre que o toggle muda")]
    [SerializeField] private AudioClip toggleSfx;

    // Aplica a preferência salva assim que o jogo abre. Sem preferência, mantém o que o Unity já está usando
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySaved()
    {
        if (PlayerPrefs.HasKey(PrefKey))
            Apply(PlayerPrefs.GetInt(PrefKey) == 1);
    }

    private static void Apply(bool fullscreen)
    {
        if (fullscreen)
        {
            // Resolução nativa do monitor, sem borda
            Resolution r = Screen.currentResolution;
            Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            // Janela menor que o monitor (2/3, mantendo a proporção), senão a barra de título estoura a tela
            Resolution r = Screen.currentResolution;
            Screen.SetResolution(r.width * 2 / 3, r.height * 2 / 3, FullScreenMode.Windowed);
        }
    }

    private void OnEnable()
    {
        bool isFullscreen = Screen.fullScreen;
        yesToggle.SetIsOnWithoutNotify(isFullscreen);
        noToggle.SetIsOnWithoutNotify(!isFullscreen);

        yesToggle.onValueChanged.AddListener(OnYesChanged);
        noToggle.onValueChanged.AddListener(OnNoChanged);
    }

    private void OnDisable()
    {
        yesToggle.onValueChanged.RemoveListener(OnYesChanged);
        noToggle.onValueChanged.RemoveListener(OnNoChanged);
    }

    private void OnYesChanged(bool on)
    {
        if (on) Set(true);
        else if (!noToggle.isOn) yesToggle.SetIsOnWithoutNotify(true); // clicar no que já está ligado não desliga
    }

    private void OnNoChanged(bool on)
    {
        if (on) Set(false);
        else if (!yesToggle.isOn) noToggle.SetIsOnWithoutNotify(true);
    }

    private void Set(bool fullscreen)
    {
        Audio.PlaySfx(toggleSfx);

        yesToggle.SetIsOnWithoutNotify(fullscreen);
        noToggle.SetIsOnWithoutNotify(!fullscreen);

        Apply(fullscreen);
        PlayerPrefs.SetInt(PrefKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
