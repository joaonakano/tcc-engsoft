# Arquitetura

## Fluxo de cenas

```
Introduction → Menu → LevelSelection → Level → Score
                 ▲          ▲            │       │
                 └──────────┴────────────┴───────┘   (voltar / repetir)
```

`Level` é **uma cena só**. A fase vem de `GameSession.Level` (um asset `LevelData`).
Toda troca de cena passa por `SceneTransition.Load(...)`; ninguém chama `SceneManager.LoadScene`.

## Serviços globais (`Scripts/Core`, `Scripts/Transition`)

Criados sozinhos, persistem entre cenas, **não precisam de objeto na cena**. Acesso estático.

| Serviço | Uso |
|---|---|
| `SceneTransition` | `Load(Scenes.X, type)`, `Reload()`, `Track(rotina)`, `WaitForReveal()`, `WaitForIdle()` |
| `Audio` | `Audio.PlaySfx(clip)`, `Audio.RegisterMusic(audioSource)` |
| `GameCursor` | `GameCursor.Show(true/false)` |
| `Settings` | `Settings.MusicVolume / SfxVolume / UseArduino` + evento `Changed` (único dono do PlayerPrefs) |
| `GameSession` | `StartLevel(id)`, `Retry()`, `Score/MaxScore/Stars`, `BestStars(id)` |
| `PoolService` | `Spawn / Release / Prewarm` |

Prefabs dos serviços ficam em `Resources/` (`SceneTransition`, `Cursor`).

## Partida (`Scripts/Match`)

```
Input / Arduino ─► HitManager ──MatchEvents.Judged──►  ScoreManager
                      │ (som, partículas, texto)   ├─►  HealthManager ─► ReactionManager / ComboManager
                      ▼                            └─►  (qualquer mecânica nova)
                  PoolService.Release
NoteSpawner ──RegisterNote──► HitManager      SongManager = relógio da música (SongManager.Current)
GameManager = sequência: preparar (Track) → tela abre → música começa → fim → Score
```

**Mecânica nova que reage aos golpes**: assine `MatchEvents.Judged` em `OnEnable`, desassine em `OnDisable`.
Não precisa mexer no `HitManager` nem ligar referências na cena.

## Recetas

- **Nova fase**: Create > Game > Level Data em `Resources/Levels` (id único + beatmap) e um `LevelCard` com o mesmo id.
- **Nova cena**: adicione a constante em `Scenes`, no Build Settings, e navegue com `SceneTransition.Load`.
- **Preparo pesado ao abrir uma cena**: `yield return SceneTransition.Track(MinhaRotina());` — a barra de loading só chega a 100% e a tinta só sai quando termina.
- **Novo som/volume**: `Audio.PlaySfx(clip)`; para música, `Audio.RegisterMusic(source)`.

## Pastas

```
Scripts/
  Core/          serviços globais (Audio, Settings, GameCursor, GameSession, Scenes, MatchEvents, Pooling/)
  Transition/    SceneTransition + barra de loading
  LevelData/     LevelData (ScriptableObject) e BeatmapAsset
  LevelSelection/ Menu/ Results/ Introduction/   telas
  Match/         InGame, Hit, Note, Score, FX
  Arduino/  UI/  _Legacy/ (código só usado pela cena S01, pode apagar)
```
