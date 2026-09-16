using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ArduinoConnectionMode
{
    USB,
    BLE
}

public class HitManager : MonoBehaviour
{
    [Header("Referências")]
    public NoteSpawner noteSpawner;

    public SongManager songManager;

    public Animator hammerAnimator;
    public Animator knifeAnimator;

    [SerializeField]
    private ArduinoConnectionMode connectionMode = ArduinoConnectionMode.USB;

    [SerializeField]
    private ArduinoUSBInput usbInput;

    [SerializeField]
    private ArduinoBLEInput bleInput;

    [Header("SFX")]
    public AudioClip perfectSfx;
    public AudioClip goodSfx;
    public AudioClip missSfx;

    [Header("Efeitos Visuais")]
    public GameObject hitEffectPrefab;
    public GameObject perfectEffectPrefab;
    public GameObject goodEffectPrefab;

    [Header("Input")]
    public InputActionReference leftActionRef;
    public InputActionReference rightActionRef;

    [Header("Timing")]
    [Tooltip("Erro máximo para PERFECT antes ou depois do TargetTime")]
    public float perfectWindow = 0.03f;

    [Tooltip("Erro máximo para GOOD antes ou depois do TargetTime")]
    public float goodWindow = 0.10f;

    [Tooltip("Tempo APÓS o TargetTime até a fruta apodrecer")]
    public float rottenWindow = 0.25f;

    private readonly List<NoteController> leftLaneNotes = new();
    private readonly List<NoteController> rightLaneNotes = new();

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
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

        if (connectionMode == ArduinoConnectionMode.USB && usbInput != null)
            usbInput.OnHit += OnArduinoHit;

        //if (connectionMode == ArduinoConnectionMode.BLE && bleInput != null)
            //bleInput.OnHit += OnArduinoHit;
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

        if (usbInput != null)
            usbInput.OnHit -= OnArduinoHit;

        //if (bleInput != null)
            //bleInput.OnHit -= OnArduinoHit;
    }

    private void OnArduinoHit(int lane)
    {
        if (lane == 0)
            ProcessHit(leftLaneNotes, LaneType.Left);
        else if (lane == 1)
            ProcessHit(rightLaneNotes, LaneType.Right);
    }

    public void RegisterNote(NoteController note, LaneType lane)
    {
        if (note == null) return;

        if (lane == LaneType.Left)
        {
            leftLaneNotes.Add(note);
        }
        else
        {
            rightLaneNotes.Add(note);
        }
    }

    private void Update()
    {
        if (noteSpawner == null) return;

        double currentSongTime = songManager.GetSongTime();

        ProcessMisses(leftLaneNotes, currentSongTime);
        ProcessMisses(rightLaneNotes, currentSongTime);

        RemoveInvalidNotes(leftLaneNotes);
        RemoveInvalidNotes(rightLaneNotes);
    }

    // =========================================================
    // MISS / APODRECIMENTO
    // =========================================================

    private void ProcessMisses(List<NoteController> notes, double currentSongTime)
    {
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            NoteController note = notes[i];

            if (note == null)
            {
                notes.RemoveAt(i);
                continue;
            }

            if (note.IsResolved())
            {
                notes.RemoveAt(i);
                continue;
            }

            double lateTime = currentSongTime - note.TargetTime;

            if (lateTime > rottenWindow)
            {
                note.Rotten();

                PlaySfx(missSfx);
                
                Debug.Log($"MISS! Fruta apodreceu. " + $"Atraso: {lateTime:F3}s");

                Destroy(note.gameObject);

                notes.RemoveAt(i);
            }            
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void OnLeftPerformed(
        InputAction.CallbackContext context)
    {
        ProcessHit(
            leftLaneNotes,
            LaneType.Left
        );
    }

    private void OnRightPerformed(
        InputAction.CallbackContext context)
    {
        ProcessHit(
            rightLaneNotes,
            LaneType.Right
        );
    }

    // =========================================================
    // HIT
    // =========================================================

    void ProcessHit(List<NoteController> notes, LaneType lane)
    {
        if (noteSpawner == null) return;

        double currentSongTime = songManager.GetSongTime();

        NoteController bestNote = null;

        double smallestError = double.MaxValue;

        foreach (NoteController note in notes)
        {
            if (note == null)
                continue;

            if (note.IsResolved())
                continue;

            double timingError =
                currentSongTime - note.TargetTime;

            double absoluteError =
                System.Math.Abs(timingError);

            // Muito cedo.
            if (timingError < -goodWindow)
                continue;

            // Muito tarde.
            if (timingError > goodWindow)
                continue;

            // Escolhe a fruta mais próxima
            // do momento ideal.
            if (absoluteError < smallestError)
            {
                smallestError = absoluteError;
                bestNote = note;
            }
        }

        // Nenhuma fruta estava dentro da janela.
        if (bestNote == null)
        {
            Debug.Log(
                $"[{lane}] Input sem fruta na janela."
            );

            return;
        }

        HitResult result = EvaluateHit(currentSongTime, bestNote);

        ApplyHit(bestNote, notes, result, lane);
    }

    // =========================================================
    // AVALIAÇÃO
    // =========================================================

    private HitResult EvaluateHit(double currentSongTime, NoteController note)
    {
        double timingError = currentSongTime - note.TargetTime;

        double absoluteError = System.Math.Abs(timingError);

        if (absoluteError <= perfectWindow)
            return HitResult.Perfect;

        if (absoluteError <= goodWindow)
            return HitResult.Good;

        return HitResult.Miss;
    }

    // =========================================================
    // APLICA HIT
    // =========================================================

    private void ApplyHit(
        NoteController note,
        List<NoteController> notes,
        HitResult result,
        LaneType lane)
    {
        double currentSongTime = songManager.GetSongTime();

        double timingError = currentSongTime - note.TargetTime;

        note.Cut();

        notes.Remove(note);

        switch (result)
        {
            case HitResult.Perfect:

                Debug.Log(
                    $"[{lane}] PERFECT! " +
                    $"Erro: {timingError:F4}s"
                );

                PlaySfx(perfectSfx);

                PlayAttackAnimation(lane);

                SpawnEffect(
                    perfectEffectPrefab,
                    note
                );

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(
                        ScoreManager.Instance.GetPerfectPoints()
                    );
                }

                break;

            case HitResult.Good:

                Debug.Log(
                    $"[{lane}] GOOD! " +
                    $"Erro: {timingError:F4}s"
                );

                PlaySfx(goodSfx);

                PlayAttackAnimation(lane);

                SpawnEffect(
                    goodEffectPrefab,
                    note
                );

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(
                        ScoreManager.Instance.GetGoodPoints()
                    );
                }

                break;
        }

        Destroy(note.gameObject);
    }

    private void PlayAttackAnimation(LaneType lane)
    {
        if (lane == LaneType.Left)
        {
            if (knifeAnimator == null)
                return;

            knifeAnimator.ResetTrigger("attack");
            knifeAnimator.SetTrigger("attack");
        }
        else
        {
            if (hammerAnimator == null)
                return;

            hammerAnimator.ResetTrigger("attack");
            hammerAnimator.SetTrigger("attack");
        }
    }

    // =========================================================
    // EFEITOS
    // =========================================================

    private void SpawnEffect(
        GameObject prefab,
        NoteController note)
    {
        if (prefab == null || note == null)
            return;

        Instantiate(
            prefab,
            note.transform.position,
            Quaternion.identity
        );
    }

    // =========================================================
    // ÁUDIO
    // =========================================================

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    // =========================================================
    // LIMPEZA
    // =========================================================

    private void RemoveInvalidNotes(
        List<NoteController> notes)
    {
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            if (notes[i] == null)
            {
                notes.RemoveAt(i);
            }
        }
    }

    public enum HitResult
    {
        Perfect,
        Good,
        Miss
    }
}