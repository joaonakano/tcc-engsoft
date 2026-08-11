using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public AudioSource hitSFX;
    //public AudioSource missSFX;

    void Start()
    {
        Instance = this;
    }

    public static void Hit()
    {
        Instance.hitSFX.Play();
    }

    public static void Miss()
    {
        return;
    }
}
