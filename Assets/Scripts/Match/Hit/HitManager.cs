using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class HitManager : MonoBehaviour
{
    [Header("Referências")]
    public SongManager songManager;
    public HealthManager healthManager;
    public NoteSpawner noteSpawner;
    public Animator knifeAnimator;   // lane esquerda
    public Animator hammerAnimator;  // lane direita

    [Header("Input")]
    public InputActionReference leftActionRef;
    public InputActionReference rightActionRef;

    [Header("Arduino")]
    [SerializeField] private ArduinoUSBInput usbInput;
    private bool useArduino;

    [Header("Timing (segundos, relativo ao momento em que a fruta chega no Y destino)")]
    [Tooltip("PERFECT: fruta no Y destino +- esse valor")]
    public float perfectWindow = 0.05f;
    [Tooltip("GOOD: ate esse valor antes/depois do destino")]
    public float goodWindow = 0.12f;
    [Tooltip("Golpe mais cedo que goodWindow, mas dentro disso = erro de precipitacao. Mais cedo ainda = ignorado")]
    public float earlyWindow = 0.30f;
    [Tooltip("Calibração de latência de SAÍDA de áudio/tela (+ = considera o golpe mais tarde). A latência do INPUT do teclado já é compensada automaticamente.")]
    public float hitOffset = 0f;

    [Header("SFX")]
    public AudioClip perfectSfx;
    public AudioClip goodSfx;
    public AudioClip missSfx;
    public AudioClip earlyMissSfx;

    [Header("Efeitos Visuais")]
    public GameObject perfectEffectPrefab;
    public GameObject goodEffectPrefab;

    public GameObject perfectText;
    public GameObject goodText;
    public GameObject missText;

    [Tooltip("Segundos até o efeito voltar ao pool. Se o prefab tiver Destroy(gameObject, x), remova.")]
    public float effectLifetime = 1f;

    private const double MaxInputAge = 0.1; // limite de sanidade para a idade do evento de input

    private static readonly int AttackHash = Animator.StringToHash("attack");

    private readonly Queue<NoteController> leftNotes = new();
    private readonly Queue<NoteController> rightNotes = new();

    // =========================================================
    // SETUP
    // =========================================================

    private void Awake()
    {
        useArduino = ArduinoToggle.IsEnabled;

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
            usbInput.OnHitTimed += OnArduinoHit;
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
            usbInput.OnHitTimed -= OnArduinoHit;
    }

    public IEnumerator PrewarmRoutine(Vector3 warmPosition, int visibleFrames)
    {
        yield return StartCoroutine(PoolService.Prewarm(perfectEffectPrefab, 3, warmPosition, visibleFrames));
        yield return StartCoroutine(PoolService.Prewarm(goodEffectPrefab, 3, warmPosition, visibleFrames));

        yield return StartCoroutine(PoolService.Prewarm(perfectText, 3, warmPosition, visibleFrames));
        yield return StartCoroutine(PoolService.Prewarm(goodText, 3, warmPosition, visibleFrames));
        yield return StartCoroutine(PoolService.Prewarm(missText, 3, warmPosition, visibleFrames));
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

    private void OnLeftPerformed(InputAction.CallbackContext ctx) => ProcessHit(LaneType.Left, InputAge(ctx));
    private void OnRightPerformed(InputAction.CallbackContext ctx) => ProcessHit(LaneType.Right, InputAge(ctx));

    // O Input System entrega o evento no começo do frame, mas ele aconteceu antes (até ~1 frame).
    // ctx.time é o instante real do evento: a diferença é o quanto o golpe "envelheceu" até aqui.
    private static double InputAge(InputAction.CallbackContext ctx)
    {
        double age = InputState.currentTime - ctx.time;
        if (age < 0) return 0;
        return age > MaxInputAge ? MaxInputAge : age;
    }

    private void OnArduinoHit(int lane, double age)
    {
        // age = quanto tempo o byte ficou esperando desde que chegou na thread de leitura
        if (age < 0) age = 0;
        if (age > MaxInputAge) age = MaxInputAge;
        ProcessHit(lane == 0 ? LaneType.Left : LaneType.Right, age);
    }

    // =========================================================
    // UPDATE: frutas que passaram da janela apodrecem
    // =========================================================

    private void Update()
    {
        if (songManager == null || songManager.IsPaused || !songManager.IsPlaying) return;

        double songTime = songManager.GetSongTime() + hitOffset;
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
            Fail(note, missSfx, missText);
        }
    }

    // =========================================================
    // HIT
    // =========================================================

    private void ProcessHit(LaneType lane, double inputAge)
    {
        if (songManager == null || songManager.IsPaused || songManager.HasFinished || !songManager.IsPlaying) return;

        PlayAttackAnimation(lane);

        Queue<NoteController> queue = GetQueue(lane);

        // Descarta notas destruidas
        while (queue.Count > 0 && queue.Peek() == null)
            queue.Dequeue();

        if (queue.Count == 0) return;

        NoteController note = queue.Peek();

        // erro > 0 = golpe tarde, erro < 0 = golpe cedo
        double hitTime = songManager.GetSongTime() - inputAge + hitOffset;
        double error = hitTime - note.TargetTime;

        // Fruta ainda longe demais: o golpe e ignorado
        if (error < -earlyWindow) return;

        queue.Dequeue();

        // Precipitado
        if (error < -goodWindow)
        {
            Fail(note, earlyMissSfx, missText); // TODO: SFX de erro por precipitacao
            return;
        }

        bool perfect = System.Math.Abs(error) <= perfectWindow;

        note.Cut();
        PlaySfx(perfect ? perfectSfx : goodSfx);
        SpawnEffect(perfect ? perfectEffectPrefab : goodEffectPrefab, note);
        SpawnText(perfect ? perfectText : goodText, note.transform.position);

        if (ScoreManager.Instance != null)
        {
            int points = perfect
                ? ScoreManager.Instance.GetPerfectPoints()
                : ScoreManager.Instance.GetGoodPoints();
            ScoreManager.Instance.AddScore(points);
        }

        healthManager.RegisterHit(perfect);

        noteSpawner.Despawn(note);
    }

    // Erro (miss ou precipitado): dano, som e remove a fruta
    private void Fail(NoteController note, AudioClip sfx, GameObject text)
    {
        PlaySfx(sfx);
        SpawnText(text, note.transform.position);
        healthManager.RegisterMiss();
        noteSpawner.Despawn(note);
    }

    private void SpawnText(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        GameObject go = PoolService.Spawn(prefab, position, Quaternion.identity);
        if (go != null && go.TryGetComponent<FloatingText>(out var ft))
            ft.Play();
    }

    // =========================================================
    // EXTRAS
    // =========================================================

    private void PlayAttackAnimation(LaneType lane)
    {
        Animator anim = lane == LaneType.Left ? knifeAnimator : hammerAnimator;
        if (anim == null) return;

        anim.ResetTrigger(AttackHash);
        anim.SetTrigger(AttackHash);
    }

    private void SpawnEffect(GameObject prefab, NoteController note)
    {
        if (prefab == null || note == null) return;
        PoolService.Spawn(prefab, note.transform.position, Quaternion.identity, effectLifetime);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        SFXManager.Instance?.PlaySFX(clip);
    }

    // =========================================================
    // ARDUINO
    // =========================================================

    public void SetArduino(bool enabled)
    {
        if (usbInput == null) return;

        if (enabled == useArduino) return;

        if (useArduino) usbInput.OnHitTimed -= OnArduinoHit;

        useArduino = enabled;
        usbInput.enabled = enabled;

        if (useArduino) usbInput.OnHitTimed += OnArduinoHit;
    }
}