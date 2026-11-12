using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Migração única da refatoração (Tools > Refactor > Clean Scenes).
/// Em TODAS as cenas de Assets/Scenes:
///   1. remove instâncias dos prefabs que viraram serviços globais (WipeEffect, Scene Transition, Cursor);
///   2. remove componentes "Missing Script" (CursorManager, SFXManager, AudioManager, LevelLoader...);
///   3. apaga GameObjects que ficaram vazios por causa disso (ex.: "Cursor Manager", "LevelLoader");
///   4. desmarca "Use Test Score" do StarResultController.
/// Depois apaga os assets do wipe antigo. Faça commit antes de rodar: ele salva as cenas.
/// </summary>
public static class ProjectMigration
{
    private static readonly HashSet<string> ObsoleteInstances = new()
    {
        "Assets/Prefabs/WipeEffect.prefab",
        "Assets/Resources/SceneTransition.prefab",
        "Assets/Resources/Cursor.prefab",
    };

    private static readonly string[] ObsoleteAssets =
    {
        "Assets/Prefabs/WipeEffect.prefab",
        "Assets/Shaders/WipeEffectInGame",
    };

    [MenuItem("Tools/Refactor/Clean Scenes")]
    public static void CleanScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!EditorUtility.DisplayDialog("Clean Scenes",
                "Isto remove objetos obsoletos de TODAS as cenas em Assets/Scenes, salva as cenas e apaga os assets do wipe antigo.\n\n" +
                "Recomendado: faça commit antes.", "Rodar", "Cancelar"))
            return;

        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath).ToArray();

        int instances = 0, scripts = 0, empties = 0;

        foreach (string path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;

            var all = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject).ToList();

            // 1) instâncias de prefabs obsoletos
            foreach (var go in all)
            {
                if (go == null || !PrefabUtility.IsOutermostPrefabInstanceRoot(go)) continue;

                string source = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(go));
                if (!ObsoleteInstances.Contains(source)) continue;

                Object.DestroyImmediate(go);
                instances++;
                changed = true;
            }

            // 2) Missing Scripts (só em objetos da própria cena, nunca dentro de instâncias de prefab)
            var touched = new List<GameObject>();
            foreach (var go in all)
            {
                if (go == null || PrefabUtility.IsPartOfPrefabInstance(go)) continue;

                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed == 0) continue;

                scripts += removed;
                touched.Add(go);
                changed = true;
            }

            // 3) objetos que só existiam para segurar o script removido
            foreach (var go in touched)
            {
                if (go != null && go.transform.childCount == 0 && go.GetComponents<Component>().Length == 1)
                {
                    Object.DestroyImmediate(go);
                    empties++;
                }
            }

            // 4) a tela de resultado deve usar o resultado real
            var stars = Object.FindFirstObjectByType<StarResultController>();
            if (stars != null)
            {
                var so = new SerializedObject(stars);
                var prop = so.FindProperty("useTestScore");
                if (prop != null && prop.boolValue)
                {
                    prop.boolValue = false;
                    so.ApplyModifiedProperties();
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        foreach (string asset in ObsoleteAssets)
            AssetDatabase.DeleteAsset(asset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Refactor] {scenePaths.Length} cenas processadas: {instances} instâncias de prefab, " +
                  $"{scripts} Missing Scripts e {empties} objetos vazios removidos.");
    }
}
