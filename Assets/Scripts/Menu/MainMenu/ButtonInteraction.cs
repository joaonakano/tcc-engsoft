using UnityEngine;

public class ButtonInteraction : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MenuController _menuController;

    public void onExit()
    {
        Application.Quit();
    }

    public void onPlay()
    {
        _menuController.OnPlayPressed();
    }
}
