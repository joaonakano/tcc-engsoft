using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vida do jogador. Reage a MatchEvents.Judged; quando zera, encerra a música (o GameManager leva ao Score).
///
/// Modelo "momentum" (balanceado por simulação, ver Backup/HealthSim.cs):
///  - ACERTAR cura; a cura cresce com o combo (bônus em ComboManager).
///  - ERRAR tira vida de forma GRADUAL: um erro isolado dói pouco; erros seguidos doem cada vez mais (escalada).
///    O dano NÃO depende de quanta vida o jogador tinha ganho antes (antes, errar após uma boa sequência custava até 60).
///  - ZONA DE PERIGO (vida baixa): o jogo ajuda quem luta para sobreviver.
///      · misericórdia: erros doem menos (até a metade, com a vida em zero);
///      · virada: acertos curam mais, então manter uma sequência faz a vida subir de volta, gradualmente.
/// </summary>
public class HealthManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SongManager songManager;
    [SerializeField] private ReactionManager reactionManager;
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private Image damagePanel;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Cura por acerto")]
    [SerializeField] private float healPerfect = 3.2f;
    [SerializeField] private float healGood = 2.4f;

    [Header("Dano por erro")]
    [Tooltip("Dano de um erro isolado")]
    [SerializeField] private float damageMiss = 6f;
    [Tooltip("Cada erro seguido (sem acertar no meio) aumenta o dano em esta fração (0.25 = +25% por erro)")]
    [SerializeField] private float damageEscalation = 0.25f;
    [Tooltip("Máximo de erros seguidos que contam para a escalada")]
    [SerializeField] private int escalationCap = 4;

    [Header("Zona de perigo")]
    [Tooltip("Abaixo desta fração da vida (0.5 = 50%) a ajuda de misericórdia/virada começa")]
    [Range(0.05f, 1f)] [SerializeField] private float dangerThreshold = 0.5f;
    [Tooltip("Multiplicador do dano com a vida em zero (sobe até 1 no limiar). 0.5 = erros doem a metade")]
    [Range(0.1f, 1f)] [SerializeField] private float mercyDamageMultiplier = 0.5f;
    [Tooltip("Multiplicador da cura com a vida em zero (cai até 1 no limiar). 1.8 = acertos curam 80% a mais")]
    [Range(1f, 3f)] [SerializeField] private float comebackHealMultiplier = 1.8f;

    // Cada faixa de vida (em %, do pior ao melhor) define a reação do personagem e a intensidade do painel de dano
    private static readonly (float maxPercent, Reactions reaction, float panelAlpha)[] Tiers =
    {
        (20f, Reactions.Bad, 0.3f),
        (40f, Reactions.Sad, 0.2f),
        (60f, Reactions.Neutral, 0.1f),
        (float.MaxValue, Reactions.Good, 0f),
    };

    public float CurrentHealth { get; private set; }
    public float HealthPercent => CurrentHealth / maxHealth;

    private int missStreak;
    private bool isDead;

    // O alpha do painel de dano é animado direto na cor da imagem (CrossFadeAlpha multiplica com o alpha da cor e ficava sempre invisível)
    private float damagePanelTarget;
    private const float PanelFadeSpeed = 2f; // alpha por segundo

    private void OnEnable() => MatchEvents.Judged += OnJudged;
    private void OnDisable() => MatchEvents.Judged -= OnJudged;

    private void Start()
    {
        if (damagePanel != null)
        {
            Color c = damagePanel.color;
            c.a = 0f;
            damagePanel.color = c;
        }

        CurrentHealth = maxHealth;
        comboManager.ResetCombo();
        UpdateReaction();
    }

    private void Update()
    {
        if (damagePanel == null) return;

        Color c = damagePanel.color;
        if (Mathf.Approximately(c.a, damagePanelTarget)) return;

        c.a = Mathf.MoveTowards(c.a, damagePanelTarget, PanelFadeSpeed * Time.unscaledDeltaTime);
        damagePanel.color = c;
    }

    private void OnJudged(Judgement judgement)
    {
        if (isDead) return;

        if (judgement.IsHit()) Heal(judgement == Judgement.Perfect);
        else TakeDamage();
    }

    // 0 = vida em zero, 1 = fora da zona de perigo
    private float DangerFactor => Mathf.Clamp01(HealthPercent / dangerThreshold);

    private void Heal(bool perfect)
    {
        float comeback = Mathf.Lerp(comebackHealMultiplier, 1f, DangerFactor);
        float heal = (perfect ? healPerfect : healGood) * comboManager.GetHealMultiplier() * comeback;

        CurrentHealth = Mathf.Min(CurrentHealth + heal, maxHealth);
        missStreak = 0;

        comboManager.Increment();
        UpdateReaction();
    }

    private void TakeDamage()
    {
        float escalation = 1f + damageEscalation * Mathf.Min(missStreak, escalationCap);
        float mercy = Mathf.Lerp(mercyDamageMultiplier, 1f, DangerFactor);

        CurrentHealth = Mathf.Max(CurrentHealth - damageMiss * escalation * mercy, 0f);
        missStreak++;
        comboManager.Break();

        if (CurrentHealth <= 0f)
        {
            isDead = true;
            MatchEvents.RaiseFailed();
            reactionManager.ChangeCurrentStateTo(Reactions.Dead);
            songManager.FinishSong();
            return;
        }

        UpdateReaction();
    }

    private void UpdateReaction()
    {
        float percent = HealthPercent * 100f;

        foreach (var tier in Tiers)
        {
            if (percent > tier.maxPercent) continue;

            reactionManager.ChangeCurrentStateTo(tier.reaction);
            damagePanelTarget = tier.panelAlpha;
            return;
        }
    }
}
