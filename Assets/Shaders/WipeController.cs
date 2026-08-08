using UnityEngine;
using UnityEngine.UI;

public class WipeController : MonoBehaviour
{
    private Animator _animator;
    private Image _image;
    private readonly int _circleSizeID = Shader.PropertyToID("_Circle_Size");

    public float circleSize = 0;

    void Start()
    {
        _animator = gameObject.GetComponent<Animator>();
        _image = gameObject.GetComponent<Image>();
    }

    void Update()
    {
        _image.materialForRendering.SetFloat(_circleSizeID, circleSize);
    }

    private void StartLevel()
    {
        SongManager.Instance.StartSong();
    }
}
