using TMPro;
using UnityEngine;

/// <summary>
/// Texto de feedback (PERFECT / GOOD / EARLY / MISS). Vai no objeto raiz do prefab do texto.
/// A cada spawn escolhe uma direção aleatória (sempre para cima, dentro de um cone), sobe e some aos poucos.
/// Funciona com TextMeshPro e/ou SpriteRenderer (imagem do texto). Volta ao pool sozinho no final.
/// </summary>
public class FloatingText : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Segundos que o texto fica na tela")]
    [SerializeField] private float lifetime = 0.8f;
    [Tooltip("Distância total percorrida (sorteada entre mín e máx, em unidades do mundo)")]
    [SerializeField] private float minDistance = 1.0f;
    [SerializeField] private float maxDistance = 1.8f;
    [Tooltip("Ângulo máximo para cada lado a partir da vertical. 0 = sempre reto pra cima, 40 = leque, 180 = qualquer direção")]
    [SerializeField, Range(0f, 180f)] private float maxAngle = 40f;

    [Header("Fade")]
    [Tooltip("Fração da vida em que o texto ainda está 100% opaco antes de começar a sumir (0 = começa a sumir já)")]
    [SerializeField, Range(0f, 0.95f)] private float fadeDelay = 0f;

    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private TMP_Text[] texts;
    private Color[] textColors;

    private SongManager songManager;
    private Vector3 startPos;
    private Vector2 direction;
    private float distance;
    private float timer;
    private bool playing;

    private void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;

        texts = GetComponentsInChildren<TMP_Text>(true);
        textColors = new Color[texts.Length];
        for (int i = 0; i < texts.Length; i++) textColors[i] = texts[i].color;
    }

    /// <summary>Chame logo depois de PoolService.Spawn.</summary>
    public void Play()
    {
        if (songManager == null)
            songManager = SongManager.Current;

        startPos = transform.position;

        float angle = Random.Range(-maxAngle, maxAngle) * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        distance = Random.Range(minDistance, maxDistance);

        timer = 0f;
        playing = true;
        SetAlpha(1f);
    }

    private void OnDisable()
    {
        playing = false;
    }

    private void Update()
    {
        if (!playing) return;
        if (songManager != null && songManager.IsPaused) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        // Sobe rápido no começo e desacelera (ease-out)
        float eased = 1f - (1f - t) * (1f - t);
        transform.position = startPos + new Vector3(direction.x, direction.y, 0f) * (distance * eased);

        float alpha = 1f - Mathf.Clamp01((t - fadeDelay) / (1f - fadeDelay));
        SetAlpha(alpha);

        if (t >= 1f)
        {
            playing = false;
            PoolService.Release(gameObject);
        }
    }

    private void SetAlpha(float a)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            Color c = spriteColors[i];
            c.a = spriteColors[i].a * a;
            sprites[i].color = c;
        }

        for (int i = 0; i < texts.Length; i++)
        {
            Color c = textColors[i];
            c.a = textColors[i].a * a;
            texts[i].color = c;
        }
    }
}