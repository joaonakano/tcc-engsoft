using UnityEngine;
using UnityEngine.InputSystem;

public class CursorUI : MonoBehaviour
{
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private Animator cursorAnimator;

    private RectTransform _cursorTransform;
    private RectTransform _canvasRectTransform;
    private Camera _canvasCamera;

    private void Awake()
    {
        _cursorTransform = GetComponent<RectTransform>();

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            _canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            _canvasCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : parentCanvas.worldCamera;
        }
    }

    private void OnEnable()
    {
        pointerPositionAction.action.Enable();

        if (clickAction != null && clickAction.action != null)
        {
            clickAction.action.Enable();
            clickAction.action.performed += OnClick;
        }

        UpdateCursorPosition();
    }

    private void OnDisable()
    {
        if (clickAction != null && clickAction.action != null)
            clickAction.action.performed -= OnClick;
    }

    private void LateUpdate()
    {
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        if (_cursorTransform == null || _canvasRectTransform == null) return;

        Vector2 mousePosition = pointerPositionAction.action.ReadValue<Vector2>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform, mousePosition, _canvasCamera, out var localPoint))
        {
            _cursorTransform.anchoredPosition = localPoint;
        }
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        if (!ctx.ReadValueAsButton()) return;

        if (cursorAnimator != null)
            cursorAnimator.SetTrigger("onClick");
    }
}
