using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HitManager : MonoBehaviour
{
    [Header("Referências")]
    public NoteSpawner noteSpawner;
    public Animator hammerAnimator;
    public Animator knifeAnimator;

    [Header("Input Actions")]
    [Tooltip("Arraste ou configure a Action 'Left' aqui (F + Clique Esquerdo)")]
    public InputActionReference leftActionRef;
    [Tooltip("Arraste ou configure a Action 'Right' aqui (J + Clique Direito)")]
    public InputActionReference rightActionRef;

    [Header("Janelas de Acerto (em segundos)")]
    public float perfectWindow = 0.08f;     // até 80ms
    public float goodWindow = 0.15f;        // até 150ms
    public float missWindow = 0.25f;        // até 250ms para atraso
    
    private Queue<NoteController> leftLaneNotes = new Queue<NoteController>();
    private Queue<NoteController> rightLaneNotes = new Queue<NoteController>();

    [Header("Valores de Pontuação")]
    public int perfectPoints = 5;
    public int goodPoints = 2;
    public int badPoints = 1;

    private void OnEnable()
    {
        // Ativa e escuta a Action 'Left' do seu asset
        if (leftActionRef != null && leftActionRef.action != null)
        {
            leftActionRef.action.Enable();
            leftActionRef.action.performed += OnLeftPerformed;
        }

        // Ativa e escuta a Action 'Right' do seu asset
        if (rightActionRef != null && rightActionRef.action != null)
        {
            rightActionRef.action.Enable();
            rightActionRef.action.performed += OnRightPerformed;
        }
    }

    private void OnDisable()
    {
        // Desinscreve e desativa para limpar a memória corretamente
        if (leftActionRef != null && leftActionRef.action != null)
        {
            leftActionRef.action.performed -= OnLeftPerformed;
            leftActionRef.action.Disable();
        }

        if (rightActionRef != null && rightActionRef.action != null)
        {
            rightActionRef.action.performed -= OnRightPerformed;
            rightActionRef.action.Disable();
        }
    }

    // SpawnNote chama esse metodo quando a nota é criada para separar as listas
    public void RegisterNote(NoteController noteController, LaneType lane)
    {
        if (lane == LaneType.Left)
        {
            leftLaneNotes.Enqueue(noteController);
        }
        else
        {
            rightLaneNotes.Enqueue(noteController);
        }
    }

    private void Update()
    {
        if (noteSpawner == null) return;

        // Limpa notas que ja foram processadas
        CleanInvalidNotes(leftLaneNotes);
        CleanInvalidNotes(rightLaneNotes);
    }

    private void OnLeftPerformed(InputAction.CallbackContext context)
    {
        if (noteSpawner == null) return;

        if (knifeAnimator != null)
        {
            knifeAnimator.ResetTrigger("attack");
            knifeAnimator.SetTrigger("attack");
        }

        double currentSongTime = AudioSettings.dspTime - noteSpawner.SongStartTime;
        ProcessHit(leftLaneNotes, currentSongTime, "Esquerda");
    }

    // Acionado automaticamente quando a Action 'Right' é disparada (J ou Clique Direito)
    private void OnRightPerformed(InputAction.CallbackContext context)
    {
        if (noteSpawner == null) return;

        if (hammerAnimator != null)
        {
            hammerAnimator.ResetTrigger("attack"); 
            hammerAnimator.SetTrigger("attack");
        }

        double currentSongTime = AudioSettings.dspTime - noteSpawner.SongStartTime;
        ProcessHit(rightLaneNotes, currentSongTime, "Direita");
    }

    void ProcessHit(Queue<NoteController> laneQueue, double currentSongTime, string laneName)
    {
        if (laneQueue.Count == 0) return;

        // Nota no topo da fila
        NoteController targetNote = laneQueue.Peek();
        if (targetNote == null)
        {
            laneQueue.Dequeue();
            return;
        }

        // Calculando diferença de tempo (>0 = atrasado, <0 = adiantado)
        float timeDiff = (float)(currentSongTime - targetNote.TargetTime);
        float absDiff = Mathf.Abs(timeDiff);

        // Verifica se está na janela aceitável
        if (absDiff <= missWindow)
        {
            laneQueue.Dequeue(); // remove da fila pois esta sendo processada
            int pointsEarned = 0;

            if (absDiff <= perfectWindow)
            {
                Debug.Log($"[{laneName}] **PERFECT! ({perfectPoints})** (Diferença: {timeDiff:F3}s)");
                // TODO: adicionar pontuacao alta, efeito visual
                pointsEarned = perfectPoints;
            }
            else if (absDiff <= goodWindow)
            {
                Debug.Log($"[{laneName}] *GOOD! ({goodPoints})* (Diferença: {timeDiff:F3}s)");
                // TODO: adicionar pontuacao media
                pointsEarned = goodPoints;
            } else
            {
                Debug.Log($"[{laneName}] Bad / Late ({badPoints}) (Diferença: {timeDiff:F3}s)");
                // TODO: pontuacao baixa
                pointsEarned = badPoints;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(pointsEarned);
            }

            Destroy(targetNote.gameObject);
        }
        else
        {
            Debug.Log($"[{laneName}] Apertou cedo demais!");
        }
    }

    void CleanInvalidNotes(Queue<NoteController> laneQueue)
    {
        // Remove as notas que foram destruidas por passarem do tempo limite da fila
        while (laneQueue.Count > 0 && laneQueue.Peek() == null)
        {
            laneQueue.Dequeue();
        }
    }
}
