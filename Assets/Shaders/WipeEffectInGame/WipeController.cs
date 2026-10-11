using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WipeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    private Animator _animator;
    private Image _image;
    private EventSystem _eventSystem;

    private readonly int _circleSizeID = Shader.PropertyToID("_Circle_Size");

    [Header("SFX")]
    [SerializeField] private AudioClip wipeStart;
    [SerializeField] private AudioClip wipeEnd;

    public float circleSize = 0;
    public bool IsTransitioning { get; private set; }

    private bool _playOutSfx = true;
    private bool _playInSfx = true;

    // quem chamar isso define o que acontece no final ou comeco da animacao
    private Action _onWipeOutComplete;
    private Action _onWipeInComplete;
    private Action _onWipeOutHalf;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _image = GetComponent<Image>();
    }

    void Update()
    {
        _image.materialForRendering.SetFloat(_circleSizeID, circleSize);
    }

    public void StartWipeOut(Action onComplete = null, Action onHalf = null, bool playSfx = true)
    {
        SetTransitioning(true);
        _onWipeOutComplete = onComplete;
        _onWipeOutHalf = onHalf;
        _playOutSfx = playSfx;
        _animator.SetTrigger("WipeOut");
    }

    public void StartWipeIn(Action onComplete = null, bool playSfx = true)
    {
        SetTransitioning(true);
        _onWipeInComplete = onComplete;
        _playInSfx = playSfx;
        _animator.SetTrigger("WipeIn");
    }

    // Animation Event
    public void WipeOutComplete()
    {
        SetTransitioning(false);
        var callback = _onWipeOutComplete;
        _onWipeOutComplete = null;  // evita duplicar a chamada
        callback?.Invoke();
    }

    // Animation Event
    public void WipeInComplete()
    {
        SetTransitioning(false);
        var callback = _onWipeInComplete;
        _onWipeInComplete = null;
        callback?.Invoke();
    }

    public void WipeOutHalf()
    {
        var callback = _onWipeOutHalf;
        _onWipeOutHalf = null;
        callback?.Invoke();
    }

    private void SetTransitioning(bool value)
    {
        IsTransitioning = value;

        if (value)
        {
            if (EventSystem.current != null)
                _eventSystem = EventSystem.current;
        }

        if (_eventSystem != null)
            _eventSystem.enabled = !value;
    }

    public void PlayWipeOutSfx()
    {
        if (_playOutSfx) PlaySfx(wipeStart);
    }

    public void PlayWipeInSfx()
    {
        if (_playInSfx) PlaySfx(wipeEnd);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void OnDestroy()
    {
        if (_eventSystem != null)
            _eventSystem.enabled = true;
    }
}
