using System;
using UnityEngine;

/// <summary>Sequência de acertos. A vida usa o bônus de combo para curar mais quando o jogador está "pegando o ritmo".</summary>
public class ComboManager : MonoBehaviour
{
    [Header("Bônus de cura por combo")]
    [Tooltip("Multiplicador da cura sem combo. 1 = cura normal (errar não deve deixar a recuperação 'travada')")]
    [SerializeField] private float healBonusAtZero = 1f;

    [Tooltip("Multiplicador da cura no combo máximo")]
    [SerializeField] private float healBonusAtMax = 1.5f;

    [Tooltip("Combo em que o bônus chega no máximo")]
    [SerializeField] private int comboForMaxBonus = 10;

    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }

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

    /// <summary>Multiplicador da cura para o combo atual (healBonusAtZero → healBonusAtMax).</summary>
    public float GetHealMultiplier()
    {
        float t = Mathf.Clamp01((float)Combo / comboForMaxBonus);
        return Mathf.Lerp(healBonusAtZero, healBonusAtMax, t);
    }
}
