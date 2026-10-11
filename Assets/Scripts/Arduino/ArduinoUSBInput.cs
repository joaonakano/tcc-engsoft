using UnityEngine;
using System;
using System.IO.Ports;
public class ArduinoUSBInput : MonoBehaviour
{
    [SerializeField] private string portName = "/dev/cu.usbserial-5B070071601";
    [SerializeField] private int baudRate = 115200;

    private SerialPort serialPort;

    public event Action<int> OnHit;

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
        serialPort.ReadTimeout = 1;

        try
        {
            serialPort.Open();
            Debug.Log($"Arduino USB conectado em {portName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Não foi possível conectar ao Arduino em {portName}: {e.Message}");
        }
    }

    private void ClosePort()
    {
        if (serialPort != null)
        {
            if (serialPort.IsOpen) serialPort.Close();
            serialPort.Dispose();
            serialPort = null;
        }
    }

    private void Update()
    {
        if (serialPort == null || !serialPort.IsOpen)
            return;

        while (serialPort.BytesToRead > 0)
        {
            int data = serialPort.ReadByte();

            switch (data)
            {
                case 0x01:
                    OnHit?.Invoke(0);
                    break;

                case 0x02:
                    OnHit?.Invoke(1);
                    break;
            }
        }
    }
}
