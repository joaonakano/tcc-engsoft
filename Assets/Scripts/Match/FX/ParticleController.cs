using UnityEngine;

public class ParticleController : MonoBehaviour
{
    // Continua sendo chamado pelo Animation Event no fim da animação (mesmo nome de antes)
    void DestroyParticle()
    {
        PoolService.Release(gameObject); // antes: Destroy(gameObject)
    }
}