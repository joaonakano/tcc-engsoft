using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool genérico. Elimina Instantiate/Destroy durante a música (principal fonte de GC spike / engasgo no momento do corte).
/// Uso:
///   PoolService.Spawn(prefab, pos, rot, lifetime)   // lifetime > 0 devolve sozinho ao pool
///   PoolService.Release(gameObject)
///   StartCoroutine(PoolService.Prewarm(prefab, n, pos, frames))
/// </summary>
public static class PoolService
{
    private static readonly Dictionary<GameObject, Stack<PooledInstance>> pools = new();
    private static Transform root;
    private static SongManager songManager;
    private static bool searchedSongManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pools.Clear();
        root = null;
        songManager = null;
        searchedSongManager = false;
    }

    /// <summary>True enquanto o jogo está pausado (tempos de vida dos itens do pool não correm).</summary>
    public static bool IsPaused
    {
        get
        {
            if (songManager == null && !searchedSongManager)
            {
                songManager = Object.FindFirstObjectByType<SongManager>();
                searchedSongManager = true;
            }
            return songManager != null && songManager.IsPaused;
        }
    }

    private static Transform GetRoot()
    {
        // Se a cena anterior foi descarregada, o root foi destruído: descarta os pools antigos
        if (root == null)
        {
            pools.Clear();
            songManager = null;
            searchedSongManager = false;
            root = new GameObject("[PoolRoot]").transform;
        }
        return root;
    }

    private static Stack<PooledInstance> GetStack(GameObject prefab)
    {
        GetRoot();
        if (!pools.TryGetValue(prefab, out var stack))
        {
            stack = new Stack<PooledInstance>();
            pools[prefab] = stack;
        }
        return stack;
    }

    private static PooledInstance Create(GameObject prefab, Stack<PooledInstance> home)
    {
        GameObject go = Object.Instantiate(prefab, GetRoot());
        go.name = prefab.name;

        PooledInstance inst = go.GetComponent<PooledInstance>();
        if (inst == null) inst = go.AddComponent<PooledInstance>();

        inst.Home = home;
        inst.Capture();
        return inst;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 0f)
    {
        if (prefab == null) return null;

        Stack<PooledInstance> stack = GetStack(prefab);

        PooledInstance inst = null;
        while (stack.Count > 0 && inst == null)
            inst = stack.Pop(); // pula instâncias que algum script destruiu por conta própria

        if (inst == null)
            inst = Create(prefab, stack);

        inst.transform.SetPositionAndRotation(position, rotation);
        if (!inst.gameObject.activeSelf) inst.gameObject.SetActive(true);

        inst.ResetState();
        inst.Arm(lifetime);
        return inst.gameObject;
    }

    public static void Release(GameObject go)
    {
        if (go == null) return;

        PooledInstance inst = go.GetComponent<PooledInstance>();
        if (inst == null) Object.Destroy(go);
        else inst.Release();
    }

    /// <summary>Cria 'count' instâncias antecipadamente. Com visibleFrames > 0 elas ficam ativas na tela por alguns frames
    /// (compila shaders / sobe sprites pra GPU) - faça isso enquanto o wipe cobre a tela.</summary>
    public static IEnumerator Prewarm(GameObject prefab, int count, Vector3 warmPosition, int visibleFrames)
    {
        if (prefab == null || count <= 0) yield break;

        Stack<PooledInstance> stack = GetStack(prefab);
        var temp = new List<PooledInstance>(count);

        for (int i = 0; i < count; i++)
        {
            PooledInstance inst = Create(prefab, stack);
            inst.transform.position = warmPosition;
            if (!inst.gameObject.activeSelf) inst.gameObject.SetActive(true);
            temp.Add(inst);
        }

        for (int f = 0; f < visibleFrames; f++)
            yield return null;

        for (int i = 0; i < temp.Count; i++)
            if (temp[i] != null) temp[i].Release();
    }
}

/// <summary>Adicionado automaticamente às instâncias do pool. Guarda o estado inicial para reset ao reutilizar.</summary>
public class PooledInstance : MonoBehaviour
{
    public Stack<PooledInstance> Home;

    private bool inPool;
    private float remaining;
    private float[] animatorSpeeds;
    private bool[] simulated2D;

    private Vector3 rootScale;
    private Transform[] children;
    private Vector3[] childPos;
    private Quaternion[] childRot;
    private Vector3[] childScale;
    private Rigidbody2D[] bodies2D;
    private Rigidbody[] bodies3D;
    private ParticleSystem[] particles;
    private TrailRenderer[] trails;
    private Animator[] animators;
    private SpriteRenderer[] sprites;
    private Color[] spriteColors;

    public void Capture()
    {
        rootScale = transform.localScale;

        var all = GetComponentsInChildren<Transform>(true);
        int n = all.Length - 1; // [0] é o próprio root
        children = new Transform[n];
        childPos = new Vector3[n];
        childRot = new Quaternion[n];
        childScale = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Transform t = all[i + 1];
            children[i] = t;
            childPos[i] = t.localPosition;
            childRot[i] = t.localRotation;
            childScale[i] = t.localScale;
        }

        bodies2D = GetComponentsInChildren<Rigidbody2D>(true);
        bodies3D = GetComponentsInChildren<Rigidbody>(true);
        particles = GetComponentsInChildren<ParticleSystem>(true);
        trails = GetComponentsInChildren<TrailRenderer>(true);
        animators = GetComponentsInChildren<Animator>(true);
        animatorSpeeds = new float[animators.Length];
        for (int i = 0; i < animators.Length; i++) animatorSpeeds[i] = animators[i].speed;
        simulated2D = new bool[bodies2D.Length];
        for (int i = 0; i < bodies2D.Length; i++) simulated2D[i] = bodies2D[i].simulated;
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;
    }

    public void ResetState()
    {
        transform.localScale = rootScale;

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null) continue;
            children[i].localPosition = childPos[i];
            children[i].localRotation = childRot[i];
            children[i].localScale = childScale[i];
        }

        for (int i = 0; i < bodies2D.Length; i++)
        {
            if (bodies2D[i] == null) continue;
            bodies2D[i].simulated = simulated2D[i]; // desfaz congelamento de pausa que ficou preso
#if UNITY_6000_0_OR_NEWER
            bodies2D[i].linearVelocity = Vector2.zero;
#else
            bodies2D[i].velocity = Vector2.zero;
#endif
            bodies2D[i].angularVelocity = 0f;
        }

        for (int i = 0; i < bodies3D.Length; i++)
        {
            if (bodies3D[i] == null) continue;
#if UNITY_6000_0_OR_NEWER
            bodies3D[i].linearVelocity = Vector3.zero;
#else
            bodies3D[i].velocity = Vector3.zero;
#endif
            bodies3D[i].angularVelocity = Vector3.zero;
        }

        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = spriteColors[i];

        for (int i = 0; i < trails.Length; i++)
            if (trails[i] != null) trails[i].Clear();

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;
            particles[i].Clear(true);
            particles[i].Play(true);
        }

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null || !animators[i].isActiveAndEnabled) continue;
            animators[i].speed = animatorSpeeds[i]; // desfaz congelamento de pausa que ficou preso
            animators[i].Rebind();
            animators[i].Update(0f);
        }
    }

    public void Arm(float lifetime)
    {
        inPool = false;
        remaining = lifetime;
        enabled = lifetime > 0f;
    }

    private void Update()
    {
        if (PoolService.IsPaused) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
            Release();
    }

    public void Release()
    {
        if (inPool) return;

        inPool = true;
        remaining = 0f;
        enabled = false;
        gameObject.SetActive(false);

        if (Home != null) Home.Push(this);
    }
}