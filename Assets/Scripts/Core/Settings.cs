using System;
using UnityEngine;

/// <summary>
/// Preferências do jogador (PlayerPrefs) num lugar só. Nenhum outro script toca nas chaves.
/// Quem precisa reagir a mudanças assina Changed (e desassina em OnDisable).
/// </summary>
public static class Settings
{
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SFXVolume";
    private const string ArduinoKey = "UseArduino";
    private const string ArduinoPortKey = "ArduinoPort";

    public static event Action Changed;

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.7f);
        set => Set(MusicKey, Mathf.Clamp01(value));
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set => Set(SfxKey, Mathf.Clamp01(value));
    }

    public static bool UseArduino
    {
        get => PlayerPrefs.GetInt(ArduinoKey, 0) == 1;
        set => Set(ArduinoKey, value ? 1 : 0);
    }

    /// <summary>Última porta serial que respondeu ao handshake (testada primeiro na próxima busca). Não dispara Changed.</summary>
    public static string ArduinoPort
    {
        get => PlayerPrefs.GetString(ArduinoPortKey, "");
        set
        {
            PlayerPrefs.SetString(ArduinoPortKey, value ?? "");
            PlayerPrefs.Save();
        }
    }

    private static void Set(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        Commit();
    }

    private static void Set(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        Commit();
    }

    private static void Commit()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
