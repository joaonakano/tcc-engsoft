using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class VolumeSlider : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Visual")]
    [SerializeField] SpriteRenderer[] barras;          // 4 barras, da menor pra maior
    [SerializeField] GameObject outline;
    [SerializeField] GameObject iconeMudo;

    [Header("Controle")]
    [SerializeField] float pixelsParaVolumeTotal = 200f; // arrastar 200px = 0 a 1
    [SerializeField] float volumePadraoAoDesmutar = 0.5f;

    [Header("SFX")]
    [SerializeField] AudioClip interactionPopClip;

    [SerializeField]
    private string ChavePrefs = "MusicVolume";

    float volume = 1f;
    bool mutado;
    bool hover;
    bool arrastando;

    int barrasAnteriores;

    void Start()
    {
        volume = ChavePrefs == "MusicVolume" ? Settings.MusicVolume : Settings.SfxVolume;
        AtualizarVisual();
    }

    // ---------- Hover ----------
    public void OnPointerEnter(PointerEventData e) { hover = true;  AtualizarOutline(); }
    public void OnPointerExit(PointerEventData e)  { hover = false; AtualizarOutline(); }

    void AtualizarOutline() => outline.SetActive(hover || arrastando);

    // ---------- Clique (só dispara se NÃO virou arraste) ----------
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;

        // Soltar o mouse sobre o ícone após um arraste também dispara o clique; ignora
        // (o Unity dispara o clique antes do EndDrag, então e.dragging ainda é true aqui)
        if (e.dragging) return;

        if (mutado || volume <= 0f)
        {
            mutado = false;
            if (volume <= 0f) volume = volumePadraoAoDesmutar;
        }
        else
        {
            mutado = true;
        }

        AplicarVolume();
        Audio.PlaySfx(interactionPopClip);
        AtualizarVisual();
    }

    // ---------- Arraste ----------
    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        arrastando = true;
        mutado = false; // arrastar desmuta
        AtualizarOutline();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!arrastando) return;

        volume = Mathf.Clamp01(volume + e.delta.x / pixelsParaVolumeTotal);
        AplicarVolume();
        AtualizarVisual();
        
        int barrasAtuais = Mathf.CeilToInt(volume * barras.Length);
        if (barrasAtuais != barrasAnteriores)
        {
            barrasAnteriores = barrasAtuais;
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        arrastando = false;
        Audio.PlaySfx(interactionPopClip);
        AtualizarOutline();
    }

    // ---------- Aplicação ----------
    void AplicarVolume()
    {
        float v = mutado ? 0f : volume;
        Debug.Log($"Slider {ChavePrefs} -> {v}");        
        if (ChavePrefs == "MusicVolume") Settings.MusicVolume = v;
        else Settings.SfxVolume = v;
    }

    void AtualizarVisual()
    {
        bool mostrarMudo = mutado || volume <= 0f;
        iconeMudo.SetActive(mostrarMudo);

        // Quantas barras ficam visíveis: 0 se mudo, senão de 1 até barras.Length
        int quantidade = mostrarMudo
            ? 0
            : Mathf.CeilToInt(volume * barras.Length);

        for (int i = 0; i < barras.Length; i++)
        {
            barras[i].enabled = i < quantidade;
        }
    }
}
