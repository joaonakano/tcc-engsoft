using UnityEngine;

/// <summary>
/// Move o ponteiro da barra de progresso no eixo X, do início ao fim da música.
/// Funciona com UI (RectTransform) ou objeto comum: usa a posição LOCAL, então vale para filho de qualquer pai.
/// </summary>
public class SongProgressBar : MonoBehaviour
{
    [SerializeField] private SongManager songManager;
    [Tooltip("GameObject pai do ponteiro (o que vai se mover)")]
    [SerializeField] private Transform pointer;

    [Header("Trajeto (posição X local do ponteiro)")]
    [SerializeField] private float startX = 0f;
    [SerializeField] private float endX = 300f;

    [Header("Pixel Perfect")]
    [Tooltip("Move o ponteiro só em passos de 1 pixel, evitando que os filhos tremam uns em relação aos outros")]
    [SerializeField] private bool snapToPixelGrid = true;
    [Tooltip("Mesmo valor do Assets Pixels Per Unit da Pixel Perfect Camera")]
    [SerializeField] private float pixelsPerUnit = 16f;

    private float _length;

    private void Awake()
    {
        if (songManager == null) songManager = SongManager.Current;
        SetProgress(0f);
    }

    private void Update()
    {
        if (songManager == null || pointer == null || !songManager.IsReady) return;

        if (_length <= 0f)
        {
            var clip = songManager.musicSource.clip;
            if (clip == null) return;
            _length = clip.length;
        }

        // Antes de StartSong() o SongStartTime é 0 e GetAudioTime() devolve o dspTime inteiro (=> 100%)
        if (!songManager.IsPlaying) return;

        // Negativo durante o songDelay e congelado no pause: o clamp do SetProgress cobre os dois casos
        SetProgress((float)(songManager.GetAudioTime() / _length));
    }

    private void SetProgress(float t)
    {
        if (pointer == null) return;

        var p = pointer.localPosition;
        p.x = Mathf.Lerp(startX, endX, Mathf.Clamp01(t));
        pointer.localPosition = p;

        // A Pixel Perfect Camera arredonda cada sprite para a grade de pixels separadamente. Se o pai anda em
        // passos contínuos, filhos com offsets fracionários diferentes cruzam a grade em momentos diferentes (jitter).
        // Ancorando o pai na grade (mundo), a fração de cada filho fica constante e todos pulam juntos.
        if (snapToPixelGrid && pixelsPerUnit > 0f)
        {
            var w = pointer.position;
            w.x = Mathf.Round(w.x * pixelsPerUnit) / pixelsPerUnit;
            pointer.position = w;
        }
    }

    // Atalhos no menu do componente: posicione o ponteiro no início/fim e grave os valores
    [ContextMenu("Gravar posição atual como INÍCIO")]
    private void SaveStart() => startX = pointer.localPosition.x;

    [ContextMenu("Gravar posição atual como FIM")]
    private void SaveEnd() => endX = pointer.localPosition.x;
}
