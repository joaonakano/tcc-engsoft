using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NoteSpawnEntry
{
    public GameObject prefab;
    [Tooltip("Peso relativo. 90 e 10 = 90% e 10%. Não precisa somar 100.")]
    [Min(0f)] public float weight = 1f;
}

[Serializable]
public class UnlockRequirement
{
    [Tooltip("Fase que precisa ser concluída antes.")]
    public LevelData level;
    [Tooltip("Estrelas mínimas (melhor resultado salvo) nessa fase.")]
    [Range(1, 3)] public int stars = 3;

    // Itens novos criados pelo "+" da lista nascem com 0 (o Unity não roda o inicializador do campo):
    // 0 contaria como "já cumprido". Exigência mínima é sempre 1 estrela.
    public int RequiredStars => Mathf.Clamp(stars, 1, 3);
}

[CreateAssetMenu(menuName = "Game/Level Data", fileName = "Level_X")]
public class LevelData : ScriptableObject
{
    [Tooltip("Identificador único. Também é a chave das estrelas salvas")]
    public string id;
    public string displayName;
    public BeatmapAsset beatmap;
    [Tooltip("Visual da fase (background, tábuas, dispensers). Vazio = usa o padrão da cena.")]
    public LevelTheme theme;

    [Header("Frutas deste level")]
    [Tooltip("Prefabs e a chance de cada um. Vazio = usa a lista padrão do NoteSpawner.")]
    public List<NoteSpawnEntry> noteEntries = new();

    [Header("Bloqueio")]
    [Tooltip("Fases (e estrelas) exigidas para liberar esta. Vazio = sempre liberada. Todas as exigências precisam ser cumpridas.")]
    public List<UnlockRequirement> unlockRequirements = new();

    public bool IsUnlocked
    {
        get
        {
            foreach (UnlockRequirement r in unlockRequirements)
                if (r != null && r.level != null && GameSession.BestStars(r.level.Key) < r.RequiredStars)
                    return false;
            return true;
        }
    }

    /// <summary>Chave das estrelas salvas: o id (ou o nome do asset, se o id estiver vazio).</summary>
    public string Key => string.IsNullOrEmpty(id) ? name : id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (UnlockRequirement r in unlockRequirements)
        {
            if (r == null) continue;
            r.stars = Mathf.Clamp(r.stars, 1, 3);

            if (r.level == null)
                Debug.LogWarning($"[{name}] Unlock Requirements tem um item sem fase: ele é ignorado.", this);
            else if (r.level == this)
                Debug.LogWarning($"[{name}] Unlock Requirements exige a própria fase: ela nunca será liberada.", this);
        }
    }
#endif

    public static LevelData Find(string id)
    {
        var level = Resources.Load<LevelData>("Levels/" + id);
        if (level == null) Debug.LogError($"LevelData '{id}' não encontrado em Resources/Levels.");
        return level;
    }
}