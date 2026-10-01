using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HitManager : MonoBehaviour
{
    [Header("Referências")]
    public SongManager songManager;
    public HealthManager healthManager;
    public Animator knifeAnimator;   // lane esquerda
    public Animator hammerAnimator;  // lane direita

    [Header("Input")]
    public InputActionReference leftActionRef;
    public InputActionReference rightActionRef;

    [Header("Arduino")]
    [Tooltip("Marque para usar o protótipo físico")]
    public bool useArduino = false;
    [SerializeField] private ArduinoUSBInput usbInput;

    [Header("Timing (segundos, relativo ao momento em que a fruta chega no Y destino)")]
    [Tooltip("PERFECT: fruta no Y destino +- esse valor")]
    public float perfectWindow = 0.05f;
    [Tooltip("GOOD: ate esse valor antes/depois do destino")]
    public float goodWindow = 0.12f;
    [Tooltip("Golpe mais cedo que goodWindow, mas dentro disso = erro de precipitacao. Mais cedo ainda = ignorado")]
    public float earlyWindow = 0.30f;
    [Tooltip("Compensa latencia do input (+ = considera o golpe mais tarde). Util pro Arduino")]
    public float hitOffset = 0f;

    [Header("SFX")]
    public AudioClip perfectSfx;
    public AudioClip goodSfx;
    public AudioClip missSfx;
    // TODO: adicionar o SFX de erro por precipitacao
    public AudioClip earlyMissSfx;

    [Header("Efeitos Visuais")]
    public GameObject perfectEffectPrefab;
    public GameObject goodEffectPrefab;

    private readonly Queue<NoteController> leftNotes = new();
    private readonly Queue<NoteController> rightNotes = new();
    private AudioSource audioSource;

    // =========================================================
    // SETUP
    // =========================================================

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (usbInput != null)
            usbInput.enabled = useArduino;
    }

    private void OnEnable()
    {
        if (leftActionRef != null)
        {
            leftActionRef.action.Enable();
            leftActionRef.action.performed += OnLeftPerformed;
        }
        if (rightActionRef != null)
        {
            rightActionRef.action.Enable();
            rightActionRef.action.performed += OnRightPerformed;
        }
        if (useArduino && usbInput != null)
            usbInput.OnHit += OnArduinoHit;
    }

    private void OnDisable()
    {
        if (leftActionRef != null)
        {
            leftActionRef.action.performed -= OnLeftPerformed;
            leftActionRef.action.Disable();
        }
        if (rightActionRef != null)
        {
            rightActionRef.action.performed -= OnRightPerformed;
            rightActionRef.action.Disable();
        }
        if (useArduino && usbInput != null)
            usbInput.OnHit -= OnArduinoHit;
    }

    public void RegisterNote(NoteController note, LaneType lane)
    {
        if (note == null) return;
        GetQueue(lane).Enqueue(note);
    }

    private Queue<NoteController> GetQueue(LaneType lane)
    {
        return lane == LaneType.Left ? leftNotes : rightNotes;
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void OnLeftPerformed(InputAction.CallbackContext ctx) => ProcessHit(LaneType.Left);
    private void OnRightPerformed(InputAction.CallbackContext ctx) => ProcessHit(LaneType.Right);

    private void OnArduinoHit(int lane)
    {
        ProcessHit(lane == 0 ? LaneType.Left : LaneType.Right);
    }

    // =========================================================
    // UPDATE: frutas que passaram da janela apodrecem
    // =========================================================

    private void Update()
    {
        if (songManager == null) return;

        double songTime = songManager.GetSongTime();
        ProcessLate(leftNotes, songTime);
        ProcessLate(rightNotes, songTime);
    }

    private void ProcessLate(Queue<NoteController> queue, double songTime)
    {
        while (queue.Count > 0)
        {
            NoteController note = queue.Peek();

            if (note == null)
            {
                queue.Dequeue();
                continue;
            }

            // Ainda dentro da janela, as proximas sao ainda mais tarde
            if (songTime - note.TargetTime <= goodWindow)
                break;

            queue.Dequeue();
            note.Rotten();
            Fail(note, missSfx);
        }
    }

    // =========================================================
    // HIT
    // =========================================================

    private void ProcessHit(LaneType lane)
    {
        if (songManager == null) return;

        PlayAttackAnimation(lane);

        Queue<NoteController> queue = GetQueue(lane);

        // Descarta notas destruidas
        while (queue.Count > 0 && queue.Peek() == null)
            queue.Dequeue();

        if (queue.Count == 0) return;

        NoteController note = queue.Peek();

        // erro > 0 = golpe tarde, erro < 0 = golpe cedo
        double error = (songManager.GetSongTime() + hitOffset) - note.TargetTime;

        // Fruta ainda longe demais: o golpe e ignorado
        if (error < -earlyWindow) return;

        queue.Dequeue();

        // Precipitado
        if (error < -goodWindow)
        {
            Fail(note, earlyMissSfx); // TODO: SFX de erro por precipitacao
            return;
        }

        bool perfect = System.Math.Abs(error) <= perfectWindow;

        note.Cut();
        PlaySfx(perfect ? perfectSfx : goodSfx);
        SpawnEffect(perfect ? perfectEffectPrefab : goodEffectPrefab, note);

        if (ScoreManager.Instance != null)
        {
            int points = perfect
                ? ScoreManager.Instance.GetPerfectPoints()
                : ScoreManager.Instance.GetGoodPoints();
            ScoreManager.Instance.AddScore(points);
        }

        healthManager.RegisterHit(perfect);

        Destroy(note.gameObject);
    }

    // Erro (miss ou precipitado): dano, som e remove a fruta
    private void Fail(NoteController note, AudioClip sfx)
    {
        PlaySfx(sfx);
        healthManager.RegisterMiss();
        Destroy(note.gameObject);
    }

    // =========================================================
    // EXTRAS
    // =========================================================

    private void PlayAttackAnimation(LaneType lane)
    {
        Animator anim = lane == LaneType.Left ? knifeAnimator : hammerAnimator;
        if (anim == null) return;

        anim.ResetTrigger("attack");
        anim.SetTrigger("attack");
    }

    private void SpawnEffect(GameObject prefab, NoteController note)
    {
        if (prefab == null || note == null) return;
        Instantiate(prefab, note.transform.position, Quaternion.identity);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }
}