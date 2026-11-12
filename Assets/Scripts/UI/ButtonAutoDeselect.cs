using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonAutoDeselect : MonoBehaviour, IPointerUpHandler
{
    public void OnPointerUp(PointerEventData eventData)
    {
        // Tira a seleção do botão, voltando ao estado normal
        EventSystem.current.SetSelectedGameObject(null);
    }
}