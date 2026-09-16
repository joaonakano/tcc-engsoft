using UnityEngine;
using System;
using System.IO.Ports;
public class ArduinoUSBInput : MonoBehaviour
{
    [SerializeField] private string portName = "COM8";
    [SerializeField] private int baudRate = 115200;

    private SerialPort serialPort;

    public event Action<int> OnHit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 1;

        try
        {
            serialPort.Open();
            Debug.Log($"Arduino USB conectado em {portName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Não foi possível conectar ao Arduino USB: {e.Message}");
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
                    Debug.Log("ARDUINO: SENSOR 1");
                    OnHit?.Invoke(0);
                    break;

                case 0x02:
                    Debug.Log("ARDUINO: SENSOR 2");
                    OnHit?.Invoke(1);
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            serialPort.Dispose();
        }
    }
}
