using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WipeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AudioSource audioSource;

    private Animator _animator;
    private Image _image;

    private readonly int _circleSizeID = Shader.PropertyToID("_Circle_Size");

    [Header("Events")]
    [SerializeField]
    private UnityEvent onWipeOutComplete;

    [SerializeField]
    private UnityEvent onWipeInComplete;

    [Header("SFX")]
    [SerializeField]
    private AudioClip wipeStart;
    [SerializeField]
    private AudioClip wipeEnd;

    public float circleSize = 0;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _image = GetComponent<Image>();
    }

    void Update()
    {
        _image.materialForRendering.SetFloat(_circleSizeID, circleSize);
    }

    public void StartWipeOut()
    {
        _animator.SetTrigger("WipeOut");

        if (audioSource != null && wipeStart != null)
        {
            audioSource.clip = wipeStart;
            audioSource.Play();
        }
    }

    public void StartWipeIn()
    {
        _animator.SetTrigger("WipeIn");

        if (audioSource != null && wipeEnd != null)
        {
            audioSource.clip = wipeEnd;
            audioSource.Play();
        }
    }

    // Animation Event
    public void WipeOutComplete()
    {
        onWipeOutComplete?.Invoke();
    }

    // Animation Event
    public void WipeInComplete()
    {
        onWipeInComplete?.Invoke();
    }
}
