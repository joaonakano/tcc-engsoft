using UnityEngine;
using UnityEngine.InputSystem;

public class MoveCamera : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float moveSpeed = 5f; // Velocidade de movimento da câmera

    private Vector2 moveInput;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        float moveX = moveInput.x * moveSpeed * Time.deltaTime;

        transform.Translate(new Vector3(moveX, 0f, 0f)); // Move a câmera horizontalmente
    }
}
