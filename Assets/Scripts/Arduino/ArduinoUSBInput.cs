using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// A leitura da serial agora roda numa thread própria, bloqueando em ReadByte (acorda no instante em que o byte chega).
/// Cada golpe recebe um timestamp na hora da chegada; no Update o evento sai com a "idade" dele (OnHitTimed),
/// e o HitManager desconta essa idade do julgamento.
/// </summary>
public class ArduinoUSBInput : MonoBehaviour
{
    [SerializeField] private string portName = "/dev/cu.usbserial-5B070071601";
    [SerializeField] private int baudRate = 115200;
    [Tooltip("Segundos entre tentativas de busca enquanto o hardware não for encontrado")]
    [SerializeField] private float retryDelay = 3f;

    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool running;
    private volatile bool connectionLost; // setado pela thread de leitura (cabo removido, erro de I/O)
    private readonly ConcurrentQueue<(int lane, long stamp)> pending = new();

    public event Action<int, double> OnHitTimed; // (lane, idade do golpe em segundos)

    public bool IsConnected => serialPort != null && serialPort.IsOpen;

    private CancellationTokenSource searchCts;

    private void OnEnable()
    {
        StartCoroutine(ConnectRoutine());
    }

    private void OnDisable()
    {
        searchCts?.Cancel(); // a coroutine para sozinha ao desabilitar; isto interrompe o scan em thread
        ClosePort();
    }

    // Procura o hardware por handshake (porta salva primeiro) e repete até achar, para aceitar plugar o cabo depois.
    private IEnumerator ConnectRoutine()
    {
        while (!IsConnected)
        {
            string preferred = string.IsNullOrEmpty(Settings.ArduinoPort) ? portName : Settings.ArduinoPort;
            searchCts = new CancellationTokenSource();
            var search = ArduinoPortFinder.FindAsync(ArduinoPortFinder.GetCandidates(preferred), baudRate, searchCts.Token);

            while (!search.IsCompleted) yield return null;

            string found = search.IsFaulted ? null : search.Result;
            if (found != null)
            {
                portName = found;
                Settings.ArduinoPort = found;
                OpenPort();
                if (IsConnected) yield break;
            }

            yield return new WaitForSecondsRealtime(retryDelay);
        }
    }

    private void OnDestroy()
    {
        ClosePort();
    }

    private void OpenPort()
    {
        if (serialPort != null && serialPort.IsOpen) return;

        // Sem DTR/RTS a placa não reinicia ao abrir (mesmo critério do handshake)
        serialPort = new SerialPort(portName, baudRate) { DtrEnable = false, RtsEnable = false };
        serialPort.ReadTimeout = 50; // só para a thread poder encerrar; o ReadByte acorda assim que chega dado

        try
        {
            serialPort.Open();
            serialPort.DiscardInBuffer();
            Debug.Log($"Arduino USB conectado em {portName}");
        }
        catch (Exception e)
        {
            Debug.Log($"Não foi possível conectar ao Arduino em {portName}: {e.Message}");
            return;
        }

        running = true;
        readThread = new Thread(ReadLoop) { IsBackground = true, Name = "ArduinoReader" };
        readThread.Start();
    }

    // lost = true: a porta já morreu (cabo removido). Close() nesse caso pode travar a main thread no Windows,
    // então o fechamento vai para uma thread do pool.
    private void ClosePort(bool lost = false)
    {
        running = false;
        connectionLost = false;

        if (readThread != null)
        {
            readThread.Join(200);
            readThread = null;
        }

        if (serialPort != null)
        {
            SerialPort dead = serialPort;
            serialPort = null;

            if (lost)
            {
                ThreadPool.QueueUserWorkItem(_ => CloseQuietly(dead));
            }
            else
            {
                CloseQuietly(dead);
            }
        }

        while (pending.TryDequeue(out _)) { }
    }

    private static void CloseQuietly(SerialPort port)
    {
        try
        {
            if (port.IsOpen) port.Close();
            port.Dispose();
        }
        catch (Exception) { }
    }

    private void ReadLoop()
    {
        SerialPort port = serialPort;

        while (running)
        {
            try
            {
                int data = port.ReadByte();
                long stamp = Stopwatch.GetTimestamp();

                if (data == 0x01) pending.Enqueue((0, stamp));
                else if (data == 0x02) pending.Enqueue((1, stamp));
            }
            catch (TimeoutException)
            {
                // normal: nenhum byte no período
            }
            catch (Exception e)
            {
                if (running)
                {
                    Debug.LogWarning($"Arduino: conexão perdida ({e.Message}). Tentando reconectar...");
                    connectionLost = true;
                }
                break;
            }
        }
    }

    private void Update()
    {
        if (connectionLost)
        {
            ClosePort(lost: true);
            StartCoroutine(ConnectRoutine()); // volta a procurar até o cabo ser plugado de novo
            return;
        }

        if (pending.IsEmpty) return;

        long now = Stopwatch.GetTimestamp();

        while (pending.TryDequeue(out var hit))
        {
            double age = (now - hit.stamp) / (double)Stopwatch.Frequency;
            OnHitTimed?.Invoke(hit.lane, age);
        }
    }
}