using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Recebe os golpes (teclado/Arduino), julga contra a fruta mais próxima de cada lane e ANUNCIA o resultado
/// em MatchEvents.Judged. Pontuação, vida e combo reagem ao evento; aqui só ficam o julgamento e o feedback
/// imediato (som, partículas, texto).
/// </summary>
public class HitManager : MonoBehaviour
{
    [Header("Referências")]
    public SongManager songManager;
    public Animator knifeAnimator;   // lane esquerda
    public Animator hammerAnimator;  // lane direita

    [Header("Input")]
    public InputActionReference leftActionRef;
    public InputActionReference rightActionRef;
    [SerializeField] private ArduinoUSBInput usbInput;

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
    [Tooltip("Segundos até o efeito voltar ao pool")]
    public float effectLifetime = 1f;

    private const double MaxInputAge = 0.1; // limite de sanidade para a idade do evento de input
    private static readonly int AttackHash = Animator.StringToHash("attack");

    private readonly Queue<NoteController> leftNotes = new();
    private readonly Queue<NoteController> rightNotes = new();

    // =========================================================
    // SETUP
    // =========================================================

    private void OnEnable()
    {
        Bind(leftActionRef, OnLeftPerformed, true);
        Bind(rightActionRef, OnRightPerformed, true);

        Settings.Changed += ApplyInputSource;
        ApplyInputSource();
    }

    private void OnDisable()
    {
        Bind(leftActionRef, OnLeftPerformed, false);
        Bind(rightActionRef, OnRightPerformed, false);

        Settings.Changed -= ApplyInputSource;
        if (usbInput != null) usbInput.OnHitTimed -= OnArduinoHit;
    }

    private static void Bind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler, bool on)
    {
        if (reference == null) return;

        if (on)
        {
            reference.action.Enable();
            reference.action.performed += handler;
        }
        else
        {
            reference.action.performed -= handler;
            reference.action.Disable();
        }
    }

    // Arduino ligado/desligado pela opção do menu (Settings.UseArduino)
    private void ApplyInputSource()
    {
        if (usbInput == null) return;

        usbInput.OnHitTimed -= OnArduinoHit;
        usbInput.enabled = Settings.UseArduino;
        if (Settings.UseArduino) usbInput.OnHitTimed += OnArduinoHit;
    }

    public IEnumerator PrewarmRoutine(Vector3 warmPosition, int visibleFrames)
    {
        foreach (GameObject prefab in new[] { perfectEffectPrefab, goodEffectPrefab, perfectText, goodText, missText })
            yield return PoolService.Prewarm(prefab, 3, warmPosition, visibleFrames);
    }

    /// <summary>Chamado pelo NoteSpawner quando uma fruta nasce.</summary>
    public void RegisterNote(NoteController note, LaneType lane)
    {
        if (note != null) GetQueue(lane).Enqueue(note);
    }

    private Queue<NoteController> GetQueue(LaneType lane) => lane == LaneType.Left ? leftNotes : rightNotes;

    // =========================================================
    // INPUT
    // =========================================================

    private void OnLeftPerformed(InputAction.CallbackContext ctx) => ProcessHit(LaneType.Left, InputAge(ctx));
    private void OnRightPerformed(InputAction.CallbackContext ctx) => ProcessHit(LaneType.Right, InputAge(ctx));

    // O Input System entrega o evento no começo do frame, mas ele aconteceu antes (até ~1 frame).
    // ctx.time é o instante real do evento: a diferença é o quanto o golpe "envelheceu" até aqui.
    private static double InputAge(InputAction.CallbackContext ctx)
        => ClampAge(InputState.currentTime - ctx.time);

    private void OnArduinoHit(int lane, double age)
        => ProcessHit(lane == 0 ? LaneType.Left : LaneType.Right, ClampAge(age));

    private static double ClampAge(double age) => age < 0 ? 0 : (age > MaxInputAge ? MaxInputAge : age);

    // =========================================================
    // JULGAMENTO
    // =========================================================

    private bool Playing => songManager != null && songManager.IsPlaying && !songManager.IsPaused && !songManager.HasFinished;

    // Frutas que passaram da janela apodrecem
    private void Update()
    {
        if (!Playing) return;

        double songTime = songManager.GetSongTime() + hitOffset;
        ProcessLate(leftNotes, songTime);
        ProcessLate(rightNotes, songTime);
    }

    private void ProcessLate(Queue<NoteController> queue, double songTime)
    {
        while (queue.Count > 0)
        {
            NoteController note = queue.Peek();

            if (note == null) { queue.Dequeue(); continue; }

            // Ainda dentro da janela: as próximas são ainda mais tarde
            if (songTime - note.TargetTime <= goodWindow) break;

            queue.Dequeue();
            Resolve(note, Judgement.Miss);
        }
    }

    private void ProcessHit(LaneType lane, double inputAge)
    {
        if (!Playing) return;

        PlayAttackAnimation(lane);

        Queue<NoteController> queue = GetQueue(lane);
        while (queue.Count > 0 && queue.Peek() == null)
            queue.Dequeue();

        if (queue.Count == 0) return;

        NoteController note = queue.Peek();

        // erro > 0 = golpe tarde, erro < 0 = golpe cedo
        double error = songManager.GetSongTime() - inputAge + hitOffset - note.TargetTime;

        if (error < -earlyWindow) return; // fruta longe demais: golpe ignorado

        queue.Dequeue();

        if (error < -goodWindow) Resolve(note, Judgement.Early);
        else if (System.Math.Abs(error) <= perfectWindow) Resolve(note, Judgement.Perfect);
        else Resolve(note, Judgement.Good);
    }

    // Único lugar onde uma fruta termina: feedback + evento + devolve ao pool
    private void Resolve(NoteController note, Judgement judgement)
    {
        Vector3 position = note.transform.position;

        if (judgement.IsHit()) note.Cut();
        else note.Rotten();

        bool perfect = judgement == Judgement.Perfect;
        switch (judgement)
        {
            case Judgement.Perfect:
            case Judgement.Good:
                CameraShake.Shake(0.4f, 0.2f);
                Audio.PlaySfx(perfect ? perfectSfx : goodSfx);
                PoolService.Spawn(perfect ? perfectEffectPrefab : goodEffectPrefab, position, Quaternion.identity, effectLifetime);
                SpawnText(perfect ? perfectText : goodText, position);
                break;
            case Judgement.Early:
                Audio.PlaySfx(earlyMissSfx);
                SpawnText(missText, position);
                break;
            default:
                Audio.PlaySfx(missSfx);
                SpawnText(missText, position);
                break;
        }

        MatchEvents.RaiseJudged(judgement);
        PoolService.Release(note.gameObject);
    }

    // =========================================================
    // FEEDBACK
    // =========================================================

    private void SpawnText(GameObject prefab, Vector3 position)
    {
        GameObject go = PoolService.Spawn(prefab, position, Quaternion.identity);
        if (go != null && go.TryGetComponent<FloatingText>(out var text))
            text.Play();
    }

    private void PlayAttackAnimation(LaneType lane)
    {
        Animator anim = lane == LaneType.Left ? knifeAnimator : hammerAnimator;
        if (anim == null) return;

        anim.ResetTrigger(AttackHash);
        anim.SetTrigger(AttackHash);
    }
}
