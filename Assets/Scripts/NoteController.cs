using UnityEngine;

public class NoteController : MonoBehaviour
{
    private double targetTime; // O tempo em segundos em que a nota deve atingir o alvo
    private float fallSpeed; // A velocidade de queda da nota
    private float spawnY; // A altura onde a nota nasce
    private float targetY; // A altura da linha de hit
    private bool isInitialized = false;

    public void Initialize(double targetTime, float spawnY, float targetY, float fallSpeed)
    {
        this.targetTime = targetTime;
        this.spawnY = spawnY;
        this.targetY = targetY;
        this.fallSpeed = fallSpeed;
        this.isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // Quanto tempo falta para a nota atingir o alvo
        double timeRemaining = targetTime - AudioSettings.dspTime;  // dspTime é o relogio da musica

        // Cálculo da posição Y com base no tempo restante e na velocidade (linear e previsivel)
        float distanceToTravel = (float)(timeRemaining * fallSpeed);

        Vector3 newPosition = transform.position;
        newPosition.y = targetY + distanceToTravel;
        transform.position = newPosition;

        // Se a nota passou do tempo e nao foi destruida, limpa-se a memoria
        if (timeRemaining < -1.0)
        { 
            Destroy(gameObject);
        }
    }
}
