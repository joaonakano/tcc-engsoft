using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class VideoSceneLoader : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    [SerializeField]
    private string menuSceneName = "MenuScene";

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            // Inscreve o método no evento de fim de vídeo
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        else
        {
            Debug.LogError("Nenhum componente VideoPlayer encontrado neste GameObject!");
        }
    }

    // Método chamado automaticamente quando o vídeo acaba
    private void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(menuSceneName);
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
