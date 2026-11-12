using UnityEngine;

/// <summary>
/// Ponte fina entre os botões do Menu e o MenuController.
/// Pode ser apagada: ligue os botões direto em MenuController.OnPlayPressed / OnExitPressed.
/// </summary>
public class ButtonInteraction : MonoBehaviour
{
    [SerializeField] private MenuController _menuController;

    public void onExit() => _menuController.OnExitPressed();

    public void onPlay() => _menuController.OnPlayPressed();
}
