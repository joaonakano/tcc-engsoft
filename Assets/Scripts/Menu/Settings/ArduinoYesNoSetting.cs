using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Par de toggles Sim/Não do "Usar Arduino" (Panel_Controles). Grupo exclusivo: sempre um dos dois ligado.
/// Só grava em Settings.UseArduino; o HitManager e o ArduinoStatusDisplay reagem sozinhos.
/// </summary>
public class ArduinoYesNoSetting : MonoBehaviour
{
    [SerializeField] private Toggle yesToggle;
    [SerializeField] private Toggle noToggle;
    [Tooltip("Tocado sempre que o toggle muda (dart throw)")]
    [SerializeField] private AudioClip toggleSfx;

    private void OnEnable()
    {
        bool on = Settings.UseArduino;
        yesToggle.SetIsOnWithoutNotify(on);
        noToggle.SetIsOnWithoutNotify(!on);

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

    private void Set(bool value)
    {
        Audio.PlaySfx(toggleSfx);

        yesToggle.SetIsOnWithoutNotify(value);
        noToggle.SetIsOnWithoutNotify(!value);
        Settings.UseArduino = value;
    }
}
