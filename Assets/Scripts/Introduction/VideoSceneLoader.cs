using UnityEngine;
using UnityEngine.Video;

/// <summary>Ao fim do vídeo de introdução, vai para o menu.</summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoSceneLoader : MonoBehaviour
{
    [SerializeField] private string nextScene = Scenes.Menu;

    private VideoPlayer videoPlayer;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp) => SceneTransition.Load(nextScene);

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
