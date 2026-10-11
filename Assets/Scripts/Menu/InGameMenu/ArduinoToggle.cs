using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ArduinoToggle : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private HitManager _hitManager;

    [Header("Configurações")]
    public const string PrefKey = "UseArduino";

    private Toggle toggle;

    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(PrefKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        toggle.SetIsOnWithoutNotify(IsEnabled); // mostra o valor atual armazenado no toggle sem salvar
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDisable()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        Debug.Log($"ArduinoToggle mudou para: {value}");
        IsEnabled = value;
        if (_hitManager != null) _hitManager.SetArduino(value);
    }
}
