using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Acha a porta serial do hardware do jogo por HANDSHAKE (igual no Windows e no Mac):
/// manda PingByte e espera PongByte. O firmware responde isso no loop() (ver comentário no fim do arquivo).
/// O scan roda em thread de fundo para não travar o menu.
/// </summary>
public static class ArduinoPortFinder
{
    public const byte PingByte = 0xFF;
    public const byte PongByte = 0xA5;

    private const int AttemptsPerPort = 3;
    private const int WaitPerAttemptMs = 300;

    /// <summary>
    /// Portas que vale testar, com a preferida (última que funcionou) na frente.
    /// Chamar na main thread (usa Application.platform).
    /// </summary>
    public static List<string> GetCandidates(string preferred)
    {
        string[] all;
        try { all = SerialPort.GetPortNames(); }
        catch (Exception) { return new List<string>(); }

        bool windows = Application.platform == RuntimePlatform.WindowsEditor
                    || Application.platform == RuntimePlatform.WindowsPlayer;

        // Windows: só existe COMx, então testa todas. Mac/Linux: só portas USB (ignora Bluetooth, debug-console...)
        IEnumerable<string> list = windows
            ? all
            : all.Where(p => p.StartsWith("/dev/cu.", StringComparison.Ordinal) || p.Contains("ttyUSB") || p.Contains("ttyACM"))
                 .Where(p => p.IndexOf("usb", StringComparison.OrdinalIgnoreCase) >= 0 || p.Contains("ttyACM"));

        return list.Distinct()
                   .OrderByDescending(p => p == preferred)
                   .ToList();
    }

    /// <summary>Devolve o nome da porta que respondeu ao handshake, ou null. Nunca lança exceção.</summary>
    public static Task<string> FindAsync(List<string> candidates, int baudRate, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            foreach (string name in candidates)
            {
                if (ct.IsCancellationRequested) return null;
                if (Probe(name, baudRate, ct)) return name;
            }
            return null;
        });
    }

    private static bool Probe(string name, int baudRate, CancellationToken ct)
    {
        SerialPort port = null;
        try
        {
            port = new SerialPort(name, baudRate)
            {
                ReadTimeout = 50,
                WriteTimeout = 200,
                // Sem DTR/RTS a placa (ESP32/Arduino com auto-reset) não reinicia ao abrir a porta
                DtrEnable = false,
                RtsEnable = false
            };
            port.Open();
            port.DiscardInBuffer();

            byte[] ping = { PingByte };
            for (int attempt = 0; attempt < AttemptsPerPort; attempt++)
            {
                if (ct.IsCancellationRequested) return false;

                port.Write(ping, 0, 1);

                var clock = Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < WaitPerAttemptMs)
                {
                    try
                    {
                        // Ignora qualquer outro byte (golpes 0x01/0x02, lixo de boot...) até chegar o Pong
                        if (port.ReadByte() == PongByte) return true;
                    }
                    catch (TimeoutException) { }
                }
            }
            return false;
        }
        catch (Exception)
        {
            return false; // porta ocupada, sem permissão, desconectada...
        }
        finally
        {
            try { port?.Close(); port?.Dispose(); } catch (Exception) { }
        }
    }
}

/* ---------- FIRMWARE: acrescentar no loop() do sketch ----------

  while (Serial.available() > 0) {
    if (Serial.read() == 0xFF) {   // ping do jogo
      Serial.write(0xA5);          // pong: "sou o hardware do jogo"
    }
  }

------------------------------------------------------------------ */
