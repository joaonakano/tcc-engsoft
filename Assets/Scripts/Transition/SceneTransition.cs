using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum TransitionType
{
    Simple,   // tinta cobre -> troca cena -> tinta sai
    Loading   // tinta cobre -> UI de loading -> carrega -> UI some -> tinta sai
}

/// <summary>
/// ÚNICO caminho para trocar de cena. Ninguém mais chama SceneManager.LoadScene.
///
///   SceneTransition.Load(Scenes.Menu);                           // rápida
///   SceneTransition.Load("Level_1", TransitionType.Loading);     // com barra de loading
///   SceneTransition.Reload(TransitionType.Loading);              // recarrega a cena atual
///
/// Dentro da cena nova (Start/coroutine):
///   yield return SceneTransition.Track(PrepareStuff());  // a tela só abre depois que isso terminar
///   yield return SceneTransition.WaitForReveal();        // espera a tinta começar a sair
///   yield return SceneTransition.WaitForIdle();          // espera a tela estar 100% visível
///
/// O prefab fica em Resources/SceneTransition.prefab: é criado sozinho na primeira chamada e persiste entre cenas.
/// Não precisa (nem deve) ficar colocado dentro das cenas.
///
/// Linha do tempo (Loading):
///   clique ─ tinta cobre ─ UI aparece ───────────────┐ ativa cena ─ Awake/Start ─ Track() ─ UI some ─ tinta sai
///             └──── LoadSceneAsync roda em paralelo ─┘     (barra: 0–70% cena, 70–100% preparo da fase)
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }
    // Verdadeiro enquanto a UI está bloqueada. Some um pouco antes de a tinta terminar de sair (inputUnlockAt)
    public static bool IsBusy => Instance != null && Instance.busy && Instance.inputLocked;

    [Header("Referências")]
    [Tooltip("GameObject raiz da transição, ativado só durante o processo")]
    [SerializeField] GameObject root;
    [Tooltip("SpriteRenderer em tela cheia com o material do shader da tinta")]
    [SerializeField] SpriteRenderer inkSpriteRenderer;
    [Tooltip("Pai dos elementos da tela de loading (fundo, barra, etc)")]
    [SerializeField] GameObject loadingGroupObject;
    [SerializeField] BlockLoadingBar loadingBar;
    [SerializeField] AudioClip startSFX;
    [SerializeField] AudioClip endSFX;

    [Header("Shader (campo Reference do Blackboard)")]
    [SerializeField] string progressProperty = "_Progress";
    [SerializeField] string directionProperty = "_Direction";
    [SerializeField] Vector2 direction = new Vector2(1f, 1f);
    [Tooltip("Na saída, inverte a direção para a tinta continuar varrendo no mesmo sentido")]
    [SerializeField] bool exitContinuesDirection = true;
    [SerializeField] AnimationCurve inkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Ponto da animação de saída (0–1) em que os cliques da UI voltam. Menor = libera mais cedo")]
    [Range(0f, 1f)] [SerializeField] float inputUnlockAt = 0.45f;

    [Header("Transição simples")]
    [SerializeField] float simpleInDuration = 0.4f;
    [SerializeField] float simpleOutDuration = 0.4f;

    [Header("Transição com loading")]
    [SerializeField] float loadingInDuration = 0.6f;
    [SerializeField] float loadingOutDuration = 0.8f;
    [SerializeField] float uiFadeDuration = 0.3f;
    [Tooltip("Tempo mínimo mostrando a barra, para o jogador ver os blocos mesmo se tudo carregar rápido")]
    [SerializeField] float minLoadingTime = 0.6f;
    [SerializeField] float holdFullBarTime = 0.2f;
    [Tooltip("Velocidade máxima da barra (fração por segundo). Suaviza saltos de progresso")]
    [SerializeField] float barSpeed = 1.5f;

    // A barra mostra a carga da cena até aqui; o resto é o preparo da fase (Track)
    const float SceneShare = 0.7f;
    const float TrackingCap = 0.95f;

    static int pendingTasks;

    Material inkMat;
    SpriteRenderer[] loadingSpriteRenderers;
    int progressId;
    int directionId;
    bool busy;
    bool inputLocked;
    float ink;
    bool revealing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Instância do material para não alterar o asset original
        inkMat = Instantiate(inkSpriteRenderer.material);
        inkSpriteRenderer.material = inkMat;
        progressId = Shader.PropertyToID(progressProperty);
        directionId = Shader.PropertyToID(directionProperty);

        if (loadingGroupObject != null)
        {
            loadingSpriteRenderers = loadingGroupObject.GetComponentsInChildren<SpriteRenderer>(true);
            loadingGroupObject.SetActive(false);
        }

        root.SetActive(false);
    }

    // ---------- API: trocar de cena ----------

    public static void Load(string sceneName, TransitionType type = TransitionType.Simple)
        => Begin(() => SceneManager.LoadSceneAsync(sceneName), type);

    public static void Load(int buildIndex, TransitionType type = TransitionType.Simple)
        => Begin(() => SceneManager.LoadSceneAsync(buildIndex), type);

    public static void Reload(TransitionType type = TransitionType.Simple)
        => Begin(() => SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex), type);

    // ---------- API: usada pela cena que acabou de abrir ----------

    /// <summary>
    /// Registra um trabalho de preparo da cena nova (pré-carga de áudio, pools...).
    /// A tinta só sai (e a barra só chega a 100%) quando ele terminar.
    /// Uso: yield return SceneTransition.Track(MinhaPreparacao());
    /// </summary>
    public static IEnumerator Track(IEnumerator routine)
    {
        pendingTasks++;
        try
        {
            while (routine.MoveNext())
                yield return routine.Current;
        }
        finally
        {
            pendingTasks--;
        }
    }

    /// <summary>Termina quando a tinta COMEÇA a sair (ou na hora, se não há transição).</summary>
    public static IEnumerator WaitForReveal()
    {
        while (Instance != null && Instance.busy && !Instance.revealing)
            yield return null;
    }

    /// <summary>Termina quando a transição acabou por completo (tela 100% visível e input liberado).</summary>
    public static IEnumerator WaitForIdle()
    {
        while (IsBusy)
            yield return null;
    }

    // ---------- Fluxo ----------

    static void Begin(Func<AsyncOperation> startLoad, TransitionType type)
    {
        var t = Get();
        if (t == null || (t.busy && t.inputLocked)) return; // ignora clique duplo durante a transição
        // Clique na janela em que a UI já foi liberada (tinta ainda saindo): reinicia a transição a partir do ponto atual
        if (t.busy) t.StopAllCoroutines();
        t.StartCoroutine(t.Routine(startLoad, type));
    }

    static SceneTransition Get()
    {
        if (Instance == null)
        {
            var prefab = Resources.Load<SceneTransition>("SceneTransition");
            if (prefab == null)
            {
                Debug.LogError("Prefab 'SceneTransition' não encontrado em Resources.");
                return null;
            }
            Instantiate(prefab); // O Awake registra o Instance
        }
        return Instance;
    }

    IEnumerator Routine(Func<AsyncOperation> startLoad, TransitionType type)
    {
        bool resume = busy; // interrompeu uma saída em andamento: a tinta continua de onde está
        busy = true;
        SetInputBlocked(true);
        revealing = false;
        pendingTasks = 0;

        bool withUi = type == TransitionType.Loading && loadingGroupObject != null;

        root.SetActive(true);
        if (!resume)
        {
            SetDirection(direction);
            SetInk(0f);
        }
        SetLoadingAlpha(0f);
        if (loadingGroupObject != null) loadingGroupObject.SetActive(withUi);
        if (loadingBar != null) loadingBar.ResetBar();

        // 1) Tinta cobre a tela (e a UI aparece) ENQUANTO a cena já carrega em paralelo
        bool covered = false;
        Audio.FadeMusic(0f, withUi ? loadingInDuration : simpleInDuration);
        StartCoroutine(CoverRoutine(withUi, () => covered = true));
        yield return null; // deixa o 1º frame da tinta ser desenhado antes de a carga ocupar a thread

        AsyncOperation op = startLoad();
        if (op == null) { End(); yield break; }
        op.allowSceneActivation = false; // segura em 90% até a tela estar coberta

        // 2) Barra segue o progresso REAL da carga (0 → SceneShare)
        float shown = 0f;
        float elapsed = 0f;
        while (!covered || op.progress < 0.9f || elapsed < (withUi ? minLoadingTime : 0f) || shown < SceneShare)
        {
            elapsed += Time.unscaledDeltaTime;
            float real = Mathf.Clamp01(op.progress / 0.9f) * SceneShare;
            shown = Mathf.MoveTowards(shown, real, barSpeed * Time.unscaledDeltaTime);
            SetBar(shown);
            yield return null;
        }

        // 3) Ativa a cena (tela segue coberta) e espera Awake/Start rodarem
        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            SetInputBlocked(true); // o EventSystem da cena nova nasce ligado: desliga assim que existir
            yield return null;
        }
        SetInputBlocked(true);
        yield return null;
        yield return null;

        // 4) Preparo da fase (Track) preenche o resto da barra. 100% = realmente pronto
        if (!withUi) shown = 1f; // sem barra visível, não gasta tempo animando
        while (pendingTasks > 0 || shown < 1f)
        {
            float target = pendingTasks > 0 ? TrackingCap : 1f;
            shown = Mathf.MoveTowards(shown, target, barSpeed * Time.unscaledDeltaTime);
            SetBar(shown);
            yield return null;
        }

        // 5) Revela a nova cena
        if (withUi)
        {
            yield return new WaitForSecondsRealtime(holdFullBarTime);
            yield return FadeGroup(1f, 0f, uiFadeDuration);
        }

        revealing = true;
        Audio.FadeMusic(1f, withUi ? loadingOutDuration : simpleOutDuration);
        SetDirection(exitContinuesDirection ? -direction : direction);
        // Libera os cliques da UI antes do fim da animação, para a resposta parecer imediata
        yield return AnimateInk(1f, 0f, withUi ? loadingOutDuration : simpleOutDuration,
            inputUnlockAt, () => SetInputBlocked(false));

        End();
    }

    IEnumerator CoverRoutine(bool withUi, Action onCovered)
    {
        yield return AnimateInk(ink, 1f, (withUi ? loadingInDuration : simpleInDuration) * (1f - ink));
        if (withUi) yield return FadeGroup(0f, 1f, uiFadeDuration);
        onCovered();
    }

    void End()
    {
        root.SetActive(false);
        busy = false;
        SetInputBlocked(false);
        revealing = false;
    }

    // ---------- Helpers ----------

    // Bloqueia cliques, toques e navegação por teclado/controle em TODA a UI enquanto a transição roda.
    // (Input de gameplay não passa pelo EventSystem; quem usa input direto checa SceneTransition.IsBusy.)
    // ATENÇÃO: um EventSystem desabilitado sai de EventSystem.current (vira null). Por isso eles são
    // buscados na cena, e não via .current, senão nunca seriam religados.
    static void SetInputBlocked(bool blocked)
    {
        if (Instance != null) Instance.inputLocked = blocked;
        foreach (var eventSystem in FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            eventSystem.enabled = !blocked;
    }

    void SetBar(float progress)
    {
        if (loadingBar != null) loadingBar.SetProgress(progress);
    }

    IEnumerator AnimateInk(float from, float to, float duration, float callbackAt = 1f, Action callback = null)
    {
        if (from <= to) Audio.PlaySfx(startSFX);
        else Audio.PlaySfx(endSFX);
        
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            if (callback != null && t >= callbackAt) { callback(); callback = null; }
            SetInk(Mathf.Lerp(from, to, inkCurve.Evaluate(t)));
            yield return null;
        }
        callback?.Invoke();
        SetInk(to);
    }

    IEnumerator FadeGroup(float from, float to, float duration)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            SetLoadingAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetLoadingAlpha(to);
    }

    void SetLoadingAlpha(float alpha)
    {
        if (loadingSpriteRenderers == null) return;

        foreach (var sr in loadingSpriteRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    void SetInk(float v) { ink = v; inkMat.SetFloat(progressId, v); }
    void SetDirection(Vector2 d) => inkMat.SetVector(directionId, d);
}
