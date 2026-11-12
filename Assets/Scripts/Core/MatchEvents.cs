using System;

public enum Judgement
{
    Perfect,
    Good,
    Early,   // golpe cedo demais (erro de precipitação)
    Miss     // fruta passou sem ser cortada
}

/// <summary>
/// Canal de eventos da partida. Quem julga (HitManager) apenas anuncia; Score, Vida, Combo etc. assinam.
/// Para uma mecânica nova reagir aos golpes, basta assinar Judged em OnEnable e desassinar em OnDisable,
/// sem mexer no HitManager nem em referências de cena.
/// </summary>
public static class MatchEvents
{
    public static event Action<Judgement> Judged;

    /// <summary>O jogador perdeu (vida zerou). A partida termina, mas não vale estrelas nem recorde.</summary>
    public static event Action Failed;

    public static void RaiseFailed() => Failed?.Invoke();

    public static void RaiseJudged(Judgement judgement) => Judged?.Invoke(judgement);

    public static bool IsHit(this Judgement j) => j == Judgement.Perfect || j == Judgement.Good;
}
