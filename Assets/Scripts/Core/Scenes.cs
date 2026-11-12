/// <summary>
/// Nomes das cenas (devem bater com Build Settings). Fluxo:
///   Introduction > Menu > LevelSelection > Level > Score
/// "Level" é UMA cena só: a fase escolhida vem de GameSession.Level (LevelData).
/// </summary>
public static class Scenes
{
    public const string Menu = "Menu";
    public const string MenuSettings = "MenuSettings";
    public const string LevelSelection = "LevelSelection";
    public const string Level = "Level";
    public const string Score = "Score";
}
