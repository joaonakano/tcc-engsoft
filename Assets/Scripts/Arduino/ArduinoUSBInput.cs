using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// A leitura da serial agora roda numa thread própria, bloqueando em ReadByte (acorda no instante em que o byte chega).
/// Cada golpe recebe um timestamp na hora da chegada; no Update o evento sai com a "idade" dele (OnHitTimed),
/// e o HitManager desconta essa idade do julgamento. OnHit continua existindo para compatibilidade.
/// </summary>
public class ArduinoUSBInput : MonoBehaviour
{
    [SerializeField] private string portName = "/dev/cu.usbserial-5B070071601";
    [SerializeField] private int baudRate = 115200;

    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool running;
    private readonly ConcurrentQueue<(int lane, long stamp)> pending = new();

    public event Action<int> OnHit;
    public event Action<int, double> OnHitTimed; // (lane, idade do golpe em segundos)

    private void OnEnable()
    {
        OpenPort();
    }

    private void OnDisable()
    {
        ClosePort();
    }

    private void OnDestroy()
    {
        ClosePort();
    }

    private void OpenPort()
    {
        if (serialPort != null && serialPort.IsOpen) return;

        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 50; // só para a thread poder encerrar; o ReadByte acorda assim que chega dado

        try
        {
            serialPort.Open();
            serialPort.DiscardInBuffer();
            Debug.Log($"Arduino USB conectado em {portName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Não foi possível conectar ao Arduino em {portName}: {e.Message}");
            return;
        }

        running = true;
        readThread = new Thread(ReadLoop) { IsBackground = true, Name = "ArduinoReader" };
        readThread.Start();
    }

    private void ClosePort()
    {
        running = false;

        if (readThread != null)
        {
            readThread.Join(200);
            readThread = null;
        }

        if (serialPort != null)
        {
            try
            {
                if (serialPort.IsOpen) serialPort.Close();
                serialPort.Dispose();
            }
            catch (Exception) { }
            serialPort = null;
        }

        while (pending.TryDequeue(out _)) { }
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
                if (running) Debug.LogError($"Arduino: leitura serial falhou: {e.Message}");
                break;
            }
        }
    }

    private void Update()
    {
        if (pending.IsEmpty) return;

        long now = Stopwatch.GetTimestamp();

        while (pending.TryDequeue(out var hit))
        {
            double age = (now - hit.stamp) / (double)Stopwatch.Frequency;
            OnHit?.Invoke(hit.lane);
            OnHitTimed?.Invoke(hit.lane, age);
        }
    }
}