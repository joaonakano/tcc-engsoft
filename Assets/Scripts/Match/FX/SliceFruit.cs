using UnityEngine;

/// <summary>
/// Mesmos campos serializados de antes (as referências no prefab continuam valendo).
/// Mudanças: volta ao pool em vez de Destroy.
/// </summary>
public class SlicedFruit : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Rigidbody2D leftHalf;
    [SerializeField] private Rigidbody2D rightHalf;

    [Header("Configuração dos Arcos")]
    [Tooltip("Força mínima/máxima de impulso lateral")]
    [SerializeField] private float minHorizontalForce = 3f;
    [SerializeField] private float maxHorizontalForce = 6f;

    [Tooltip("Força mínima/máxima de pulo vertical")]
    [SerializeField] private float minUpwardForce = 4f;
    [SerializeField] private float maxUpwardForce = 7f;

    [Tooltip("Velocidade de rotação (gira ao voar)")]
    [SerializeField] private float minTorque = 120f;
    [SerializeField] private float maxTorque = 300f;

    [Header("Tempo de Vida")]
    [SerializeField] private float destroyDelay = 3f;

    private SongManager songManager;
    private float lifeTimer;
    private bool initialized;

    public void Initialize()
    {
        if (songManager == null)
            songManager = SongManager.Current;

        // --- Metade Esquerda ---
        float forceXLeft = Random.Range(minHorizontalForce, maxHorizontalForce);
        float forceYLeft = Random.Range(minUpwardForce, maxUpwardForce);
        float torqueLeft = Random.Range(minTorque, maxTorque);

        leftHalf.AddForce(new Vector2(-forceXLeft, forceYLeft), ForceMode2D.Impulse);
        leftHalf.AddTorque(torqueLeft, ForceMode2D.Impulse);

        // --- Metade Direita ---
        float forceXRight = Random.Range(minHorizontalForce, maxHorizontalForce);
        float forceYRight = Random.Range(minUpwardForce, maxUpwardForce);
        float torqueRight = Random.Range(minTorque, maxTorque);

        rightHalf.AddForce(new Vector2(forceXRight, forceYRight), ForceMode2D.Impulse);
        rightHalf.AddTorque(-torqueRight, ForceMode2D.Impulse);

        lifeTimer = 0f;
        initialized = true;
    }

    private void OnDisable()
    {
        initialized = false;
    }

    private void Update()
    {
        if (!initialized) return;

        // Pausado o timer nao avanca
        if (songManager != null && songManager.IsPaused) return;

        lifeTimer += Time.deltaTime;

        if (lifeTimer >= destroyDelay)
        {
            initialized = false;
            PoolService.Release(gameObject); // antes: Destroy(gameObject)
        }
    }
}