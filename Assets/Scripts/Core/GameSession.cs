using UnityEngine;

/// <summary>
/// Estado que atravessa as cenas: qual fase está sendo jogada e o resultado da última partida.
/// É o ÚNICO ponto de entrada para iniciar/repetir fase e para ler/gravar estrelas.
///
///   GameSession.StartLevel("Level_1");        // seleção de fase
///   GameSession.Retry();                      // repetir a fase atual
///   GameSession.Score / MaxScore / Stars      // lidos pela tela de resultado
/// </summary>
public static class GameSession
{
    public static LevelData Level { get; private set; }

    // Resultado da última partida
    public static int Score { get; private set; }
    public static int MaxScore { get; private set; }
    public static int Stars { get; private set; }
    public static bool Failed { get; private set; }  // perdeu a partida (vida zerou)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Level = null;
        Score = MaxScore = Stars = 0;
        Failed = false;
    }

    public static void StartLevel(string levelId, TransitionType transition = TransitionType.Loading)
    {
        if (SceneTransition.IsBusy) return;

        var level = LevelData.Find(levelId);
        if (level == null) return;
        if (!level.IsUnlocked) return; // fase bloqueada

        Level = level;
        SceneTransition.Load(Scenes.Level, transition);
    }

    public static void Retry(TransitionType transition = TransitionType.Loading)
    {
        if (Level != null) StartLevel(Level.name, transition);
    }

    /// <summary>Chamado ao fim da partida. Só sobrescreve as estrelas salvas se forem melhores e se o jogador não perdeu.</summary>
    public static void SaveResult(int score, int maxScore, int stars, bool failed = false)
    {
        Score = score;
        MaxScore = maxScore;
        Stars = stars;
        Failed = failed;

        stars = Mathf.Clamp(stars, 0, 3);

        if (!failed && Level != null && stars > BestStars(Level.Key))
        {
            PlayerPrefs.SetInt(StarsKey(Level.Key), stars);
            PlayerPrefs.Save();
        }
    }

    public static int BestStars(string levelId) => PlayerPrefs.GetInt(StarsKey(levelId), 0);

    private static string StarsKey(string levelId) => levelId + "_stars";
}
