using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;

/// <summary>
/// Mostra o estado da busca do hardware no menu de configurações: Detectando / Nada detectado / Conectado.
/// Busca quando o toggle "Usar Arduino" liga e repete enquanto nada for encontrado.
/// O texto (TMP) é provisório: quando os sprites ficarem prontos, arraste-os para os 3 campos de visual.
/// </summary>
public class ArduinoStatusDisplay : MonoBehaviour
{
    [Header("Provisório")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private string detectingText = "Detectando...";
    [SerializeField] private string notFoundText = "Nada detectado";
    [SerializeField] private string connectedText = "Conectado";

    [Header("Sprites (opcional)")]
    [SerializeField] private GameObject detectingVisual;
    [SerializeField] private GameObject notFoundVisual;
    [SerializeField] private GameObject connectedVisual;

    [Header("Busca")]
    [SerializeField] private int baudRate = 115200;
    [Tooltip("Segundos entre tentativas enquanto nada for encontrado")]
    [SerializeField] private float retryDelay = 3f;

    private enum State { Off, Detecting, NotFound, Connected }

    private Coroutine routine;
    private CancellationTokenSource cts;

    private void OnEnable()
    {
        Settings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Settings.Changed -= Refresh;
        Stop();
    }

    // Settings.Changed dispara também para volume etc.: só reinicia a busca se o estado do toggle mudou
    private void Refresh()
    {
        bool want = Settings.UseArduino;
        if (want && routine == null) routine = StartCoroutine(SearchRoutine());
        else if (!want) { Stop(); Show(State.Off); }
    }

    private void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        cts?.Cancel();
    }

    private IEnumerator SearchRoutine()
    {
        while (true)
        {
            Show(State.Detecting);

            cts = new CancellationTokenSource();
            var search = ArduinoPortFinder.FindAsync(ArduinoPortFinder.GetCandidates(Settings.ArduinoPort), baudRate, cts.Token);
            while (!search.IsCompleted) yield return null;

            string found = search.IsFaulted ? null : search.Result;
            if (found != null)
            {
                Settings.ArduinoPort = found;
                Show(State.Connected); // routine fica != null: mudanças de volume etc. não refazem a busca
                yield break;
            }

            Show(State.NotFound);
            yield return new WaitForSecondsRealtime(retryDelay);
        }
    }

    private void Show(State state)
    {
        if (label != null)
        {
            label.text = state switch
            {
                State.Detecting => detectingText,
                State.NotFound => notFoundText,
                State.Connected => connectedText,
                _ => ""
            };
        }

        if (detectingVisual != null) detectingVisual.SetActive(state == State.Detecting);
        if (notFoundVisual != null) notFoundVisual.SetActive(state == State.NotFound);
        if (connectedVisual != null) connectedVisual.SetActive(state == State.Connected);
    }
}
