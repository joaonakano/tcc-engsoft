using UnityEngine;

/// <summary>
/// Visual de uma fase: background, tábuas e dispensers. Referenciado pelo LevelData.
/// </summary>
[CreateAssetMenu(menuName = "Game/Level Theme", fileName = "Theme_")]
public class LevelTheme : ScriptableObject
{
    [Header("Estáticos")]
    public Sprite background;
    public Sprite boardLeft;
    public Sprite boardRight;

    [Header("Barra de progresso")]
    public Sprite pointerSprite;
    [Tooltip("Material (shader) da barra de duração da música")]
    public Material progressBarMaterial;

    [Header("Dispensers")]
    [Tooltip("Override do DispenserController com os clips idle/spawn desse theme. Vazio = mantém o padrão da cena.")]
    public AnimatorOverrideController dispenserAnimator;
}
