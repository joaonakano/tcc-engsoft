using System;
using UnityEngine;

/// <summary>
/// Barra de loading em blocos baseada em GameObjects com SpriteRenderer.
/// Cada filho direto deste objeto é um bloco; a ordem na hierarquia é a ordem em que eles acendem.
/// Posicione os filhos em cima dos "buracos" do seu frame em pixel art.
/// </summary>
public class BlockLoadingBar : MonoBehaviour
{
    [Tooltip("Deixe vazio para coletar automaticamente os filhos diretos")]
    [SerializeField] SpriteRenderer[] blocks;

    /// <summary>Disparado quando um bloco acende (índice do bloco). Útil para som.</summary>
    public event Action<int> BlockFilled;

    int lastCount = -1;

    void Awake()
    {
        if (blocks == null || blocks.Length == 0)
        {
            blocks = new SpriteRenderer[transform.childCount];
            for (int i = 0; i < blocks.Length; i++)
            {
                // Pega o SpriteRenderer direto do filho, ou procura nos filhos se necessário
                blocks[i] = transform.GetChild(i).GetComponent<SpriteRenderer>();
            }
        }
    }

    public void ResetBar()
    {
        lastCount = -1;
        SetProgress(0f);
    }

    /// <summary>progress de 0 a 1</summary>
    public void SetProgress(float progress)
    {
        int count = Mathf.Clamp(Mathf.FloorToInt(progress * blocks.Length + 0.0001f), 0, blocks.Length);
        if (count == lastCount) return;

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] != null)
            {
                // Opção 1: Desliga o componente SpriteRenderer (mantém o GameObject ativo)
                blocks[i].enabled = i < count;
                
                // (Opcional) Se preferir desligar o GameObject inteiro, comente a linha acima e descomente a abaixo:
                // blocks[i].gameObject.SetActive(i < count);
            }
        }

        // Dispara o evento para cada bloco novo
        for (int i = Mathf.Max(lastCount, 0); i < count; i++)
        {
            BlockFilled?.Invoke(i);
        }

        lastCount = count;
    }
}