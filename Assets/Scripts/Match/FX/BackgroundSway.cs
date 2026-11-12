
using UnityEngine;
using DG.Tweening;

public class BackgroundSway : MonoBehaviour
{
    [Header("Limites (em unidades Unity / graus)")]
    [SerializeField] private Vector2 positionAmplitude = new Vector2(0.25f, 0.15f);
    [SerializeField] private float rotationAmplitude = 0.6f;
 
    [Header("Ritmo")]
    [Tooltip("Quanto menor, mais lento e suave. 0.1 a 0.3 costuma ser bom.")]
    [SerializeField] private float speed = 0.15f;
 
    [Tooltip("0 = oito quase perfeito. 1 = bem irregular/aleatório.")]
    [Range(0f, 1f)]
    [SerializeField] private float randomness = 0.35f;
 
    [Header("Opcional")]
    [Tooltip("Aumenta a escala para cobrir as bordas durante o balanço.")]
    [SerializeField] private bool autoScaleToCoverEdges = false;
    [Tooltip("Tamanho do sprite em unidades (largura, altura). Só usado no auto scale.")]
    [SerializeField] private Vector2 spriteSize = new Vector2(20f, 12f);
 
    private Vector3 startPos;
    private float startRot;
    private Vector3 startScale;
    private Tween tween;
    private float t;
 
    // fases e frequências aleatórias (sorteadas uma vez)
    private float phX, phY, phR, phX2, phY2, phR2;
    private float fX, fY, fR;
 
    private void OnEnable()
    {
        startPos = transform.localPosition;
        startRot = transform.localEulerAngles.z;
        startScale = transform.localScale;
 
        Randomize();
 
        if (autoScaleToCoverEdges)
        {
            // folga necessária: deslocamento + efeito da rotação nas bordas
            float rotRad = rotationAmplitude * Mathf.Deg2Rad;
            float extraX = positionAmplitude.x + spriteSize.y * 0.5f * rotRad;
            float extraY = positionAmplitude.y + spriteSize.x * 0.5f * rotRad;
            float scaleNeeded = 1f + Mathf.Max(extraX * 2f / spriteSize.x, extraY * 2f / spriteSize.y);
            transform.localScale = startScale * scaleNeeded;
        }
 
        // "relógio" linear e praticamente infinito; todo o movimento sai de Apply()
        t = 0f;
        const float total = 100000f;
        tween = DOTween.To(() => t, v => { t = v; Apply(); }, total, total)
            .SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Normal, false)
            .SetLink(gameObject);
    }
 
    private void OnDisable()
    {
        tween?.Kill();
        transform.localPosition = startPos;
        transform.localRotation = Quaternion.Euler(0f, 0f, startRot);
        transform.localScale = startScale;
    }
 
    private void Randomize()
    {
        // X em f, Y em ~2f => formato de oito (lemniscata). randomness deforma a razão.
        float jitter = randomness * 0.4f;
        fX = 1f * Random.Range(1f - jitter, 1f + jitter);
        fY = 2f * Random.Range(1f - jitter, 1f + jitter);
        fR = 0.7f * Random.Range(1f - jitter, 1f + jitter);
 
        phX = Random.value * Mathf.PI * 2f;
        phY = Random.value * Mathf.PI * 2f;
        phR = Random.value * Mathf.PI * 2f;
        phX2 = Random.value * Mathf.PI * 2f;
        phY2 = Random.value * Mathf.PI * 2f;
        phR2 = Random.value * Mathf.PI * 2f;
    }
 
    // soma de duas senoides (peso 1 - r e r) => resultado sempre entre -1 e 1
    private float Wave(float time, float f, float ph, float ph2)
    {
        float r = randomness * 0.5f;
        float main = Mathf.Sin(time * f + ph);
        float second = Mathf.Sin(time * f * 1.618f + ph2); // razão áurea: nunca repete
        return main * (1f - r) + second * r;
    }
 
    private void Apply()
    {
        float time = t * speed;
 
        float x = Wave(time, fX, phX, phX2) * positionAmplitude.x;
        float y = Wave(time, fY, phY, phY2) * positionAmplitude.y;
        float rot = Wave(time, fR, phR, phR2) * rotationAmplitude;
 
        transform.localPosition = startPos + new Vector3(x, y, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, startRot + rot);
    }

}