using UnityEngine;

public class ComboDisplayController : MonoBehaviour
{
    [SerializeField] private ComboManager comboManager;

    [Tooltip("Prefab do display de combo")]
    [SerializeField] private ComboDisplayInstance prefab;

    [Tooltip("GameObject pai onde os combos são criados (define a posição na tela)")]
    [SerializeField] private Transform container;

    [Tooltip("Combo mínimo para mostrar o display")]
    [SerializeField] private int minComboRequiredToShow = 10;

    private ComboDisplayInstance current;

    private void OnEnable()
    {
        if (comboManager == null) return;
        comboManager.OnComboChanged += Refresh;
        Refresh(comboManager.Combo);
    }

    private void OnDisable()
    {
        if (comboManager != null)
            comboManager.OnComboChanged -= Refresh;

        if (current != null)
        {
            Destroy(current.gameObject);
            current = null;
        }
    }

    private void Refresh(int combo)
    {
        if (combo <= minComboRequiredToShow)
        {
            // Combo quebrou: a instância atual morre sozinha e deixamos de referenciá-la
            if (current != null)
            {
                current.Die();
                current = null;
            }
            return;
        }

        // Combo novo em folha
        if (current == null)
            current = Instantiate(prefab, container);

        current.SetValue(combo);
    }
}