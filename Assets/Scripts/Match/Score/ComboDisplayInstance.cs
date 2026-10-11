using System.Collections.Generic;
using UnityEngine;

public class ComboDisplayInstance : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Filho que contém o X e os dígitos. É ele que balança e cai")]
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer multiplierSign;
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Header("Layout")]
    [SerializeField] private int overlapPixels = 1;

    [Header("Balanço")]
    [Tooltip("Ponto de giro relativo ao Visual (unidades do mundo)")]
    [SerializeField] private Vector2 pivot = Vector2.zero;
    [SerializeField] private float angle = 5f;
    [SerializeField] private float speed = 0.8f;
    [SerializeField] private float phaseOffset = 0f;

    [Header("Fade ao morrer")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float fallDistancePixels = 6f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Animação de pulinho ao nascer")]
    [Tooltip("Altura da subida, em pixels")]
    [SerializeField] private float spawnHopPixels = 4f;

    [Tooltip("Duração total (sobe e volta), em segundos")]
    [SerializeField] private float spawnHopDuration = 0.25f;

    [Tooltip("Curva do pulinho: 0 = posição original, 1 = altura máxima")]
    [SerializeField] private AnimationCurve spawnHopCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f));

    private float spawnElapsed;

    private readonly List<SpriteRenderer> digits = new List<SpriteRenderer>();
    private SongManager songManager;

    private float swayTime;
    private bool dying;
    private float dyingElapsed;

    private void Awake()
    {
        songManager = FindFirstObjectByType<SongManager>();
        swayTime = phaseOffset;
    }

    private void Update()
    {
        if (songManager != null && songManager.IsPaused) return;

        swayTime += Time.deltaTime;

        // Balanço em volta do pivô
        float z = Mathf.Sin(swayTime * speed * Mathf.PI * 2f) * angle;
        Quaternion rot = Quaternion.Euler(0f, 0f, z);
        Vector3 p = pivot;
        Vector3 pos = p - rot * p;

        if (spawnElapsed < spawnHopDuration)
        {
            spawnElapsed += Time.deltaTime;
            float h = spawnHopCurve.Evaluate(Mathf.Clamp01(spawnElapsed / spawnHopDuration));
            pos += Vector3.up * (h * spawnHopPixels / multiplierSign.sprite.pixelsPerUnit);
        }

        // Queda + fade
        if (dying)
        {
            dyingElapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(Mathf.Clamp01(dyingElapsed / fadeDuration));

            pos += Vector3.down * (t * fallDistancePixels / multiplierSign.sprite.pixelsPerUnit);
            SetAlpha(1f - t);

            if (dyingElapsed >= fadeDuration)
            {
                Destroy(gameObject);
                return;
            }
        }

        visual.localRotation = rot;
        visual.localPosition = pos;
    }

    // Chamado pelo controller sempre que o combo muda
    public void SetValue(int combo)
    {
        if (dying) return;

        Pop();

        string text = combo.ToString();
        EnsureDigitCount(text.Length);

        Sprite xSprite = multiplierSign.sprite;
        float overlap = overlapPixels / xSprite.pixelsPerUnit;
        float cursor = multiplierSign.transform.localPosition.x + xSprite.bounds.max.x - overlap;

        for (int i = 0; i < digits.Count; i++)
        {
            var sr = digits[i];

            if (i >= text.Length)
            {
                sr.gameObject.SetActive(false);
                continue;
            }

            sr.gameObject.SetActive(true);
            sr.sprite = digitSprites[text[i] - '0'];

            float posX = cursor - sr.sprite.bounds.min.x;
            sr.transform.localPosition = new Vector3(posX, 0f, 0f);

            cursor += sr.sprite.bounds.size.x - overlap;
        }
    }

    public void Pop() => spawnElapsed = 0f;

    // Começa o fade; ao terminar, o objeto se destrói
    public void Die()
    {
        dying = true;
    }

    private void SetAlpha(float a)
    {
        SetAlpha(multiplierSign, a);
        foreach (var d in digits) SetAlpha(d, a);
    }

    private static void SetAlpha(SpriteRenderer sr, float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    private void EnsureDigitCount(int count)
    {
        while (digits.Count < count)
        {
            var go = new GameObject($"Digit_{digits.Count}");
            go.transform.SetParent(visual, false);
            go.layer = gameObject.layer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = multiplierSign.sharedMaterial;
            sr.sortingLayerID = multiplierSign.sortingLayerID;
            sr.sortingOrder = multiplierSign.sortingOrder;

            digits.Add(sr);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (visual == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(visual.TransformPoint(pivot), 0.05f);
    }
}