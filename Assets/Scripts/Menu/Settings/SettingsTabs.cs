using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abas do menu de settings: cada botão (Vídeo, Áudio, Arduino...) mostra o seu painel e esconde os outros.
/// Cada painel é um GameObject com o conteúdo daquela aba (botões, sprites, toggles...).
/// </summary>
public class SettingsTabs : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        public Button button;
        [Tooltip("Raiz do conteúdo da aba. Ligada só quando a aba está selecionada")]
        public GameObject panel;
        [Tooltip("Opcional: sprite/objeto de destaque ligado só na aba selecionada (botão pressionado, seta...)")]
        public GameObject selectedMarker;
    }

    [SerializeField] private Tab[] tabs;
    [SerializeField] private int startIndex = 0;

    public int Current { get; private set; } = -1;

    private void Awake() => Select(startIndex); // já no 1º frame, antes da animação de abrir

    private void OnEnable()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i; // captura por iteração
            if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => Select(index));
        }
    }

    private void OnDisable()
    {
        foreach (var tab in tabs)
            if (tab.button != null) tab.button.onClick.RemoveAllListeners();
    }

    public void Select(int index)
    {
        if (index < 0 || index >= tabs.Length) return;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool on = i == index;
            if (tabs[i].panel != null) tabs[i].panel.SetActive(on);
            if (tabs[i].selectedMarker != null) tabs[i].selectedMarker.SetActive(on);
        }

        Current = index;
    }
}
