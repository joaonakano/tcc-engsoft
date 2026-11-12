using UnityEngine;

/// <summary>
/// Cursor customizado (pixel art) global. O prefab fica em Resources/Cursor.prefab e é criado
/// na primeira chamada, persistindo entre cenas: nada de "Cursor Manager" em cada cena.
///
///   GameCursor.Show(true);   // menus: cursor do jogo visível
///   GameCursor.Show(false);  // partida: cursor travado e escondido
/// </summary>
public static class GameCursor
{
    private static GameObject cursorObject; // o objeto do CursorUI (filho do Canvas), que vem desativado no prefab

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cursorObject = null;

    public static void Show(bool visible)
    {
        if (cursorObject == null && !Create()) return;

        cursorObject.SetActive(visible);
        Cursor.visible = !visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private static bool Create()
    {
        var prefab = Resources.Load<GameObject>("Cursor");
        if (prefab == null)
        {
            Debug.LogError("GameCursor: prefab 'Cursor' não encontrado em Resources.");
            return false;
        }

        var root = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(root);

        if (root.TryGetComponent<Canvas>(out var canvas))
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // não depende da câmera de cada cena

        var ui = root.GetComponentInChildren<CursorUI>(true);
        if (ui == null)
        {
            Debug.LogError("GameCursor: o prefab 'Cursor' não tem CursorUI.");
            Object.Destroy(root);
            return false;
        }

        cursorObject = ui.gameObject;
        return true;
    }
}
