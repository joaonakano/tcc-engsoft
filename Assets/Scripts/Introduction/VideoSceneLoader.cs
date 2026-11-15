using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Ao fim do vídeo de introdução, vai para o menu.</summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoSceneLoader : MonoBehaviour
{
    [SerializeField] private string nextScene = Scenes.Menu;

    [Tooltip("Tempo de tela preta após soltar o botão, antes de carregar a próxima cena.")]
    [SerializeField] private float clearDelay = 0.4f;

    private VideoPlayer videoPlayer;
    private IDisposable anyButtonListener;
    private bool loading;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += OnVideoFinished;

        // Qualquer botão (teclado, mouse, gamepad) pula o vídeo.
        anyButtonListener = InputSystem.onAnyButtonPress.Call(ctrl => Skip(ctrl as ButtonControl));
    }

    private void OnVideoFinished(VideoPlayer vp) => Skip(null);

    private void Skip(ButtonControl pressed)
    {
        if (loading) return;
        loading = true;
        StartCoroutine(LoadWhenReleased(pressed));
    }

    // Tela preta de "meio de campo": cobre o vídeo, espera soltar o botão e deixa
    // passar um tempo de segurança, para o clique não vazar para a UI da próxima cena.
    private IEnumerator LoadWhenReleased(ButtonControl pressed)
    {
        videoPlayer.Pause();
        CreateBlackScreen();

        while (pressed != null && pressed.isPressed)
            yield return null;

        yield return new WaitForSecondsRealtime(clearDelay);

        SceneManager.LoadScene(nextScene);
    }

    private static void CreateBlackScreen()
    {
        var go = new GameObject("BlackScreen");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var image = new GameObject("Black").AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        image.transform.SetParent(go.transform, false);
        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        anyButtonListener?.Dispose();
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
