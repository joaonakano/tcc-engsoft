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

    public static LevelData Find(string id)
    {
        var level = Resources.Load<LevelData>("Levels/" + id);
        if (level == null) Debug.LogError($"LevelData '{id}' não encontrado em Resources/Levels.");
        return level;
    }
}