using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    public static CameraShake Instance;

    private void Awake() 
    {
        Instance = this;
        // Salva os valores iniciais logo no início do jogo
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    private void OnShake(float duration, float strength)
    {
        // 1. Interrompe qualquer shake anterior para evitar acúmulo de bugs
        transform.DOKill();

        // 2. Reseta instantaneamente para o padrão antes de começar o novo shake
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;

        // 3. Executa o Shake de Posição e força o retorno ao original ao terminar
        transform.DOShakePosition(duration, strength)
            .OnComplete(() => transform.localPosition = originalPosition);

        // 4. Executa o Shake de Rotação e força o retorno ao original ao terminar
        transform.DOShakeRotation(duration, strength)
            .OnComplete(() => transform.localRotation = originalRotation);
    }
    public static void Shake(float duration, float strength) => Instance.OnShake(duration, strength);
}