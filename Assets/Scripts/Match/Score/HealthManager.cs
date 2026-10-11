using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SongManager songManager;
    [SerializeField] private ReactionManager reactionManager;
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private Image damagePanel;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float missDamage = 10f;
    [SerializeField] private float goodHeal = 3f;
    [SerializeField] private float perfectHeal = 5f;

    [Header("Penalidade proporcional")]
    [Tooltip("Ao errar, perde esta fracao da vida regenerada na sequencia atual (0.5 = 50%)")]
    [Range(0f, 1f)]
    [SerializeField] private float lossRatio = 0.5f;

    public float CurrentHealth { get; private set; }

    private float healthGainedInStreak;
    private bool isDead;

    private void Start()
    {
        if (reactionManager == null) Debug.LogError("HealthManager: sem referencia ao ReactionManager");
        if (comboManager == null) Debug.LogError("HealthManager: sem referencia ao ComboManager");

        ResetHealth();
    }

    // =========================================================
    // API usada pelo HitManager
    // =========================================================

    public void RegisterHit(bool perfect)
    {
        if (isDead) return;

        float heal = (perfect ? perfectHeal : goodHeal) * comboManager.GetHealMultiplier();

        float before = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + heal, maxHealth);

        // So conta o que realmente foi curado (vida cheia nao acumula)
        healthGainedInStreak += CurrentHealth - before;

        comboManager.Increment();
        OnHealthChanged();
    }

    public void RegisterMiss()
    {
        if (isDead) return;

        float damage = missDamage + healthGainedInStreak * lossRatio;
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0f);

        healthGainedInStreak = 0f;
        comboManager.Break();

        if (CurrentHealth <= 0f)
        {
            Die();
            return;
        }

        OnHealthChanged();
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        healthGainedInStreak = 0f;
        isDead = false;

        if (comboManager != null) comboManager.ResetCombo();
        OnHealthChanged();
    }

    // =========================================================
    // INTERNO
    // =========================================================

    private void OnHealthChanged()
    {
        UpdateReaction();
    }

    private void Die()
    {
        isDead = true;
        reactionManager.ChangeCurrentStateTo(Reactions.Dead);

        songManager.FinishSong();
    }

    private void UpdateReaction()
    {
        if (isDead || reactionManager == null) return;

        float percent = CurrentHealth / maxHealth * 100f;

        if (percent <= 20f)
        {
            reactionManager.ChangeCurrentStateTo(Reactions.Bad);
            damagePanel.CrossFadeAlpha(1f, 0.5f, false); // Fade in the damage panel when health is critical
        }
        else if (percent <= 40f)
        {
            reactionManager.ChangeCurrentStateTo(Reactions.Sad);
            damagePanel.CrossFadeAlpha(0.5f, 0.5f, false); // Fade in the damage panel when health is low
        }
        else if (percent <= 60f)
        {
            reactionManager.ChangeCurrentStateTo(Reactions.Neutral);
            damagePanel.CrossFadeAlpha(0.3f, 0.5f, false); // Fade out the damage panel when health is good
        }
        else
        {
            reactionManager.ChangeCurrentStateTo(Reactions.Good);
            damagePanel.CrossFadeAlpha(0f, 0.5f, false); // Fade out the damage panel when health is high
        }
    }
}