using UnityEngine;

public class NoteController : MonoBehaviour
{
    public double TargetTime { get; private set; }      // O tempo em segundos em que a fruta deve atingir o alvo
    private double spawnTime;       // Momento exato que o dispenser soltou a fruta
    private float fallDuration;     // Tempo de duração da queda
    private float spawnY;           // Altura onde a fruta nasce
    private float targetY;          // Altura da tábua
    private double songStartTime;   // Relógio da música
    private bool isInitialized = false;

    [Header("Configuração de Erro")]
    [Tooltip("Tempo limite em segundos que a fruta fica na tábua após o targetTime antes de 'morrer'")]
    private float missThreshold = 0.6f;

    public void Initialize(double targetTime, double spawnTime, float fallDuration, float spawnY, float targetY, double songStartTime)
    {
        this.TargetTime = targetTime;
        this.spawnTime = spawnTime; // Agora o spawnTime é recebido corretamente!
        this.fallDuration = fallDuration;
        this.spawnY = spawnY;
        this.targetY = targetY;
        this.songStartTime = songStartTime;
        this.isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        double currentSongTime = AudioSettings.dspTime - songStartTime;

        // FASE 1: A fruta está caindo de forma suave até a tábua
        if (currentSongTime < TargetTime)
        {
            double timeElapsedSinceSpawn = currentSongTime - spawnTime;
            float progress = (float)(timeElapsedSinceSpawn / fallDuration);
            progress = Mathf.Clamp01(progress);

            float currentY = Mathf.Lerp(spawnY, targetY, progress);
            transform.position = new Vector3(transform.position.x, currentY, transform.position.z);
        }
        // FASE 2: A fruta já aterrissou na tábua e aguarda o corte
        else
        {
            // Trava estaticamente na tábua
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

            // FASE 3: Passou o limite aceitável sem ser cortada -> "Morre" (Miss)
            if (currentSongTime > TargetTime + missThreshold)
            {
                // TODO: Chamar lógica de erro/miss aqui futuramente
                Destroy(gameObject);
            }
        }
    }
}