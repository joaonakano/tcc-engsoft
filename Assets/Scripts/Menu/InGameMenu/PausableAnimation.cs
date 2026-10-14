using UnityEngine;

/// <summary>
/// Mesma função de antes, agora compatível com pool:
/// - não procura o SongManager a cada reativação;
/// - se o objeto for desativado (devolvido ao pool) enquanto congelado, o estado é restaurado,
///   senão o Animator ficava com speed 0 e a próxima explosão nascia travada.
/// </summary>
public class PausableAnimation : MonoBehaviour
{
    private SongManager songManager;

    private Animator[] animators;
    private ParticleSystem[] particles;
    private Rigidbody2D[] bodies2D;

    private float[] originalSpeeds;
    private Vector2[] velocities2D;
    private float[] angularVelocities2D;
    private bool[] wasSimulated2D;

    private bool isFrozen;

    private void Awake()
    {
        animators = GetComponentsInChildren<Animator>(true);
        particles = GetComponentsInChildren<ParticleSystem>(true);
        bodies2D = GetComponentsInChildren<Rigidbody2D>(true);

        originalSpeeds = new float[animators.Length];
        for (int i = 0; i < animators.Length; i++)
            originalSpeeds[i] = animators[i].speed;

        velocities2D = new Vector2[bodies2D.Length];
        angularVelocities2D = new float[bodies2D.Length];
        wasSimulated2D = new bool[bodies2D.Length];
    }

    private void OnEnable()
    {
        if (songManager == null)
            songManager = FindFirstObjectByType<SongManager>();
        if (songManager == null) return;

        songManager.OnPaused += Freeze;
        songManager.OnResumed += Unfreeze;

        if (songManager.IsPaused) Freeze();
    }

    private void OnDisable()
    {
        if (songManager != null)
        {
            songManager.OnPaused -= Freeze;
            songManager.OnResumed -= Unfreeze;
        }

        // Saiu (ex.: voltou ao pool) ainda congelado: devolve velocidade/física ao normal
        if (isFrozen)
            RestoreState(false);
    }

    private void Freeze()
    {
        if (isFrozen) return;
        isFrozen = true;

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null) continue;
            originalSpeeds[i] = animators[i].speed;
            animators[i].speed = 0f;
        }

        foreach (var ps in particles)
        {
            if (ps != null) ps.Pause(true);
        }

        for (int i = 0; i < bodies2D.Length; i++)
        {
            Rigidbody2D rb = bodies2D[i];
            if (rb == null) continue;

            wasSimulated2D[i] = rb.simulated;

            if (rb.simulated)
            {
                velocities2D[i] = rb.linearVelocity;
                angularVelocities2D[i] = rb.angularVelocity;
                rb.simulated = false; // congela no lugar
            }
        }
    }

    private void Unfreeze()
    {
        if (!isFrozen) return;
        RestoreState(true);
    }

    private void RestoreState(bool resumeParticles)
    {
        isFrozen = false;

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null) continue;
            animators[i].speed = originalSpeeds[i];
        }

        if (resumeParticles)
        {
            foreach (var ps in particles)
            {
                if (ps != null) ps.Play(true);
            }
        }

        for (int i = 0; i < bodies2D.Length; i++)
        {
            Rigidbody2D rb = bodies2D[i];
            if (rb == null) continue;

            if (wasSimulated2D[i])
            {
                rb.simulated = true;
                if (resumeParticles) // ao devolver ao pool a velocidade é zerada pelo reset do pool
                {
                    rb.linearVelocity = velocities2D[i];
                    rb.angularVelocity = angularVelocities2D[i];
                }
            }
        }
    }
}