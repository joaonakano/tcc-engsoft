using UnityEngine;
using UnityEngine.InputSystem;

public class SongManager : MonoBehaviour
{
    private bool isMusicPlaying = true;
    
    [SerializeField]
    private AudioSource audioSrc;

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

    private void Awake()
    {
    }

    void Update()
    {
        //if (audioSrc.isPlaying)
            //Debug.Log($"Current Music Time: {((int)audioSrc.time)}s");

    }
}
