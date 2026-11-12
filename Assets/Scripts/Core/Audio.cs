using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço de áudio global. Criado sozinho antes da 1ª cena e persiste: não precisa de objeto em cena.
///
///   Audio.PlaySfx(clip);                 // efeito sonoro (respeita o volume de SFX)
///   Audio.RegisterMusic(audioSource);    // qualquer AudioSource de música passa a seguir o volume de música
///
/// Os volumes vêm de Settings; o VolumeSelector só altera Settings e tudo se ajusta sozinho.
/// </summary>
public class Audio : MonoBehaviour
{
    private static Audio instance;

    private AudioSource sfx;
    private readonly List<AudioSource> music = new();

    private float fade = 1f; // multiplicador de fade das transições de cena
    private Coroutine fadeRoutine;
    private float MusicVolume => Settings.MusicVolume * fade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("[Audio]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Audio>();
    }

    private void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        ApplyVolumes();
    }

    private void OnEnable() => Settings.Changed += ApplyVolumes;
    private void OnDisable() => Settings.Changed -= ApplyVolumes;

    public static void PlaySfx(AudioClip clip)
    {
        if (clip != null && instance != null)
            instance.sfx.PlayOneShot(clip);
    }

    public static void RegisterMusic(AudioSource source)
    {
        if (source == null || instance == null || instance.music.Contains(source)) return;

        instance.music.Add(source);
        source.volume = instance.MusicVolume; // nasce já respeitando o fade atual
    }

    /// <summary>Leva o volume de todas as músicas a target (0–1) do volume configurado.</summary>
    public static void FadeMusic(float target, float duration)
    {
        if (instance == null) return;
        if (instance.fadeRoutine != null) instance.StopCoroutine(instance.fadeRoutine);
        instance.fadeRoutine = instance.StartCoroutine(instance.FadeRoutine(target, duration));
    }

    private IEnumerator FadeRoutine(float target, float duration)
    {
        float from = fade;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            fade = Mathf.Lerp(from, target, t);
            ApplyVolumes();
            yield return null;
        }
        fade = target;
        ApplyVolumes();
        fadeRoutine = null;
    }

    private void ApplyVolumes()
    {
        sfx.volume = Settings.SfxVolume;

        music.RemoveAll(s => s == null); // fontes de cenas já descarregadas
        foreach (var source in music)
            source.volume = MusicVolume;
    }
}
