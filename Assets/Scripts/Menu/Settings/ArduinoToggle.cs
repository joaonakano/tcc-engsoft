using UnityEngine;
using UnityEngine.UI;

/// <summary>Toggle "Usar Arduino" do menu. Só grava em Settings; o HitManager reage sozinho.</summary>
[RequireComponent(typeof(Toggle))]
public class ArduinoToggle : MonoBehaviour
{
    private Toggle toggle;

    private void Awake() => toggle = GetComponent<Toggle>();

    private void OnEnable()
    {
        toggle.SetIsOnWithoutNotify(Settings.UseArduino);
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDisable() => toggle.onValueChanged.RemoveListener(OnToggleChanged);

    private void OnToggleChanged(bool value) => Settings.UseArduino = value;
}
