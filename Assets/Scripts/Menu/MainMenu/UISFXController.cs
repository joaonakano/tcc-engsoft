using UnityEngine;

public class UISFXController : MonoBehaviour
{
    public AudioSource audioSource;

    public void PlaySoundEffect()
    {
        audioSource.Play();
    }
}
