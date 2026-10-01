using UnityEngine;

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

    public void Initialize()
    {
        // --- Metade Esquerda ---
        float forceXLeft = Random.Range(minHorizontalForce, maxHorizontalForce);
        float forceYLeft = Random.Range(minUpwardForce, maxUpwardForce);
        float torqueLeft = Random.Range(minTorque, maxTorque);

        // Impulso para a esquerda + cima
        leftHalf.AddForce(new Vector2(-forceXLeft, forceYLeft), ForceMode2D.Impulse);
        leftHalf.AddTorque(torqueLeft, ForceMode2D.Impulse);

        // --- Metade Direita ---
        float forceXRight = Random.Range(minHorizontalForce, maxHorizontalForce);
        float forceYRight = Random.Range(minUpwardForce, maxUpwardForce);
        float torqueRight = Random.Range(minTorque, maxTorque);

        // Impulso para a direita + cima
        rightHalf.AddForce(new Vector2(forceXRight, forceYRight), ForceMode2D.Impulse);
        rightHalf.AddTorque(-torqueRight, ForceMode2D.Impulse);

        // Limpeza automática após cair para fora da tela
        Destroy(gameObject, destroyDelay);
    }
}