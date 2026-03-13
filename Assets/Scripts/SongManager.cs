using UnityEngine;
using UnityEngine.InputSystem;

public class SongManager : MonoBehaviour
{
    private bool isMusicPlaying = false;
    
    [SerializeField]
    private AudioSource audioSrc;

    [SerializeField]
    private KeyCode playKey;

    public void Tocar(InputAction.CallbackContext context)
    {
        if (!isMusicPlaying && context.performed)
        {
            audioSrc.Play();
            isMusicPlaying = true;
        } else if (isMusicPlaying && context.performed)
        {
            isMusicPlaying = false;
            audioSrc.Pause();
        }
    }

    void Update()
    {
        if (audioSrc.isPlaying)
            Debug.Log($"Current Music Time: {((int)audioSrc.time)}s");

    }
}
