using UnityEngine;

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

        velocities2D = new Vector2[bodies2D.Length];
        angularVelocities2D = new float[bodies2D.Length];
        wasSimulated2D = new bool[bodies2D.Length];
    }

    private void OnEnable()
    {
        songManager = FindFirstObjectByType<SongManager>();
        if (songManager == null) return;

        songManager.OnPaused += Freeze;
        songManager.OnResumed += Unfreeze;

        if (songManager.IsPaused) Freeze();
    }

    private void OnDisable()
    {
        if (songManager == null) return;

        songManager.OnPaused -= Freeze;
        songManager.OnResumed -= Unfreeze;
    }

    private void Freeze()
    {
        if (isFrozen) return;
        isFrozen = true;

        // Animators
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null) continue;
            originalSpeeds[i] = animators[i].speed;
            animators[i].speed = 0f;
        }

        // Particulas
        foreach (var ps in particles)
        {
            if (ps != null) ps.Pause(true);
        }

        // RGBody2D
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
        isFrozen = false;

        // Animators
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null) continue;
            animators[i].speed = originalSpeeds[i];
        }

        // Particulas
        foreach (var ps in particles)
        {
            if (ps != null) ps.Play(true);
        }

        // RGBody2D
        for (int i = 0; i < bodies2D.Length; i++)
        {
            Rigidbody2D rb = bodies2D[i];
            if (rb == null) continue;

            if (wasSimulated2D[i])
            {
                rb.simulated = true;
                rb.linearVelocity = velocities2D[i];
                rb.angularVelocity = angularVelocities2D[i];
            }
        }
    }
}
