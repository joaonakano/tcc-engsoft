using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CursorUI _cursor;

    public void HabilitarCursor(bool habilitar)
    {
        _cursor.gameObject.SetActive(habilitar);
        Cursor.visible = !habilitar;
        Cursor.lockState = habilitar ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
