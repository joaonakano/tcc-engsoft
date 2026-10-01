using System;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    [Header("Multiplicador de cura")]
    [Tooltip("Multiplicador da cura com combo 0")]
    [SerializeField] private float minHealMultiplier = 0.3f;
    [Tooltip("Multiplicador da cura no combo maximo")]
    [SerializeField] private float maxHealMultiplier = 1.5f;
    [Tooltip("Combo em que o multiplicador chega no maximo")]
    [SerializeField] private int comboForMaxMultiplier = 20;

    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }

    // Para a UI de combo no futuro
    public event Action<int> OnComboChanged;

    public void Increment()
    {
        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;
        OnComboChanged?.Invoke(Combo);
    }

    public void Break()
    {
        if (Combo == 0) return;
        Combo = 0;
        OnComboChanged?.Invoke(Combo);
    }

    public void ResetCombo()
    {
        Combo = 0;
        MaxCombo = 0;
        OnComboChanged?.Invoke(Combo);
    }

    public float GetHealMultiplier()
    {
        float t = Mathf.Clamp01((float)Combo / comboForMaxMultiplier);
        return Mathf.Lerp(minHealMultiplier, maxHealMultiplier, t);
    }
}