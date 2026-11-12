using System;
using System.Collections;
using UnityEngine;

/// <summary>Dispara triggers de vários Animators, cada um com seu delay (ex.: bordas e botões do menu).</summary>
public class AnimationManager : MonoBehaviour
{
    [Serializable]
    public class AnimationEntry
    {
        public string id;                      // ex: "Borders", "MenuButtons"
        public Animator animator;
        public string triggerName = "Start";
        public float delay = 0f;               // segundos até disparar
    }

    [SerializeField] private AnimationEntry[] _animations;

    /// <summary>Dispara todas respeitando cada delay.</summary>
    public void PlayAll()
    {
        foreach (var entry in _animations)
            StartCoroutine(PlayAfterDelay(entry));
    }

    /// <summary>Dispara uma pelo id, com o delay configurado.</summary>
    public void Play(string id)
    {
        var entry = Find(id);
        if (entry != null) StartCoroutine(PlayAfterDelay(entry));
    }

    private IEnumerator PlayAfterDelay(AnimationEntry entry)
    {
        if (entry.delay > 0f)
            yield return new WaitForSeconds(entry.delay);

        if (entry.animator == null)
            Debug.LogWarning($"AnimationManager: animator nulo em '{entry.id}'.", this);
        else
            entry.animator.SetTrigger(entry.triggerName);
    }

    private AnimationEntry Find(string id)
    {
        foreach (var entry in _animations)
            if (entry.id == id) return entry;

        Debug.LogWarning($"AnimationManager: id '{id}' não encontrado.", this);
        return null;
    }
}
