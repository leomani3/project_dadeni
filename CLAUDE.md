# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.3.9f1** (Unity 6), URP, new Input System.
This project is dedicated to building a roguelike deckbuilding game with tactical grid based combat. 


## Build / run / test

Everything goes through the Unity Editor — there is no CLI build script, no test assembly, and no lint config.

- Play mode entry: any scene works (see Bootstrap below). Scenes are `Assets/_GameAssets/Scenes/{InitScene, MenuScene, MainScene}.unity`, in that build order.
- `*.csproj` / `*.sln` at the repo root are Unity-generated (there are four stale `.sln` files from prior template renames). Never edit them; they are rewritten on asset import.
- `com.unity.test-framework` is installed but no test asmdef or test folder exists yet. Adding tests means creating a Tests folder + asmdef first.
- **No asmdefs exist for game code** — everything under `_GameAssets/` and `Team Square/` compiles into `Assembly-CSharp`, and `Assets/Plugins/` into `Assembly-CSharp-firstpass`. Any new script can see any other without wiring references.

## Key third-party plugins (`Assets/Plugins/`)

Odin Inspector (Sirenix), DOTween (Demigiant), Easy Save 3, Lean Pool (CW), MyBox (UPM, git), MPUIKit, ParticleImage, Auto Radial Layout (GIGA — drives the skill tree links), Easy Performant Outline, vHierarchy/vInspector, IngameDebugConsole.

## Architecture

### Bootstrap
`Utils/Bootstraper.cs` uses `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` on an `async Task`: from any scene other than `InitScene` it disables every GameObject, `await`s `LoadSceneAsync("InitScene")` (so the persistent managers exist before anything else loads), then loads `MenuScene` — or `MainScene` when `cheatSettings.noMenu`. Set `cheatSettings.disableBootStrapper` to work in an isolated scene.

### Three ScriptableObject singletons in `Assets/Team Square/Resources/`
`GameConfig`, `GameData`, `GameAssets` — each exposes `static Instance` backed by `Resources.Load<T>("<Name>")`. They are the global service locator for data:

- **GameConfig** — tuning + `cheatSettings` (`preventSave`, `startResetData`, `noFTUE`, `noCurrencyRequired`, `noMenu`, `disableBootStrapper`) and `uiSettings` (shared button hover/click tween values).
- **GameAssets** — Odin `[AssetList]`-populated asset arrays (currencies today).
- **GameData** — *the save file*. Persisted with Easy Save 3 (`ES3.Save("GameData", this)` / `ES3.LoadInto`) into the same ScriptableObject instance. Runtime-only fields and `Action`s must be marked `[ES3NonSerializable]`. Writes set `isDirty`; `SaveManager` flushes every 5s and on `OnApplicationQuit`. Because save data loads *into the asset*, playing in the editor mutates the checked-in `GameData.asset` — use `GameData.ResetGameData()` or `cheatSettings.startResetData` to get a clean state.

### MonoBehaviour singletons
Managers derive from MyBox `Singleton<T>`: `Instance` lazily does `FindObjectOfType` and **never auto-creates**, so every manager (`GameManager`, `UIManager`, `SoundManager`, `StatManager`, `EntityManager`, `TutorialManager`, `FadeManager`, `CameraManager`/`CameraController`, `FloatingTextManager`, `FlyingParticleManager`, `SaveManager`, `LevelManager`) must live in a loaded scene. `InitializeSingleton(persistent)` is the opt-in for `DontDestroyOnLoad` + duplicate destruction.

### UI stack (4 layers, init cascades downward)
`UIManager` (registers every child `CanvasHandler` **by its concrete Type** in `Awake`, `GetCanvas<T>()`) → `CanvasHandler` (owns a `Canvas`, opens/closes its `UIContainer`s, disables the Canvas only after all containers finish closing; `GetContainer<T>()`) → `UIContainer` (DOTween fade/move in-out, `EnableByDefault`, `OnCloseComplete`) → `AUIElement` (`Init`/`Show`/`Hide`, base of `CustomButton`, currency widgets, …).

Consequences: one `CanvasHandler` subclass per canvas (a second instance of the same type is ignored with a warning), and `AUIElement.Init()` is driven by the container cascade, not only by `Start()`.

### Stat system (`Team Square/Stat System/`, namespace `Stats`)
`StatManager` owns every `Stat`, across two independent axes:
- **Entity axis** — keyed by `EntityType` (a `[Flags]` enum), seeded at `Awake` from `EntityStatDefinition` assets.
- **Source axis** — keyed by any `ScriptableObject` (card, ability, item), each holding a `Stat[MAX_STEPS, MAX_APPLICATIONS]` grid addressed by `(step, application)`. This is the generalized form of the source project's ability stats; nothing in the framework names a concrete source type.

Each axis has a **definition** tier (persistent, what meta-progression modifies) and an **instance** tier (per spawned GameObject). Instance stats subscribe to their definition `Stat.OnValueChanged`, so a definition upgrade propagates live to everything already spawned. `EntityStatModule` registers/unregisters the entity automatically; `UnregisterInstance` unsubscribes both axes — skipping it leaks into the definition's event.

`StatModifier` carries either an `entityType` or a `statSource` + `step`/`application`; the single-argument `AddDefinitionModifier(mod)` / `RemoveDefinitionModifier(mod)` route to the right axis. An entity-axis modifier applies to *every flag* in the mask via `EntityTypeExtensions.GetFlags()`.

`Stat` caches lazily and computes `flat * (1 + addedPercent / 100) * multiplier` — **`ModifierType.Percentage` values are whole percents (25 = +25%)**, not fractions. Modifiers with a non-null `id` are *replaced* rather than stacked; `AddModifier` stores a `Copy()`.

`StatType` uses explicit numbering with gaps, because its values are serialized inside `EntityStatDefinition` assets and modifiers — add entries, never renumber.

### Entity system (`Team Square/Entity System/`)
`Entity` is the composition root: it holds the `EntityType`, discovers its `EntityModule` components and drives their lifecycle — `Initialize`/`OnInitialize` per module → `OnAllModuleInitialized` once all exist → `Cleanup` on despawn. Modules find each other through `Entity.TryGetModule<T>()` rather than direct references, and the module dictionary is keyed by every type in the inheritance chain, so `TryGetModule<EntityHealthModule>()` also finds a subclass.

`Entity` implements LeanPool's `IPoolable`: `OnSpawn` initializes modules and registers with `EntityManager`; `OnDespawn` cleans them up. `EntityManager` is the runtime registry (all entities, enemies, player, entity-by-collider) — nothing needs a scene search.

Shipped modules: `EntityStatModule` (instance stats), `EntityHealthModule` (health/damage/death, MaxHealth read from the stats), `EntityTeamModule` (`Team.Player`/`Team.Enemy`, drives EntityManager bucketing), `EntitySpawnModule` (spawn animation + VFX window), `EntitySheenModule` (white emission flash on hit). Game-specific reactions belong in listeners of `EntityHealthModule`'s events or in a subclass overriding `PlayDamageFeedback` / `Die`.

### Camera
`CameraManager` is the persistent camera owner. Scenes ship their own `Camera` carrying `CameraRegister`, which calls `RegisterMainCamera` on load: the manager copies that camera's position/rotation onto its `CinemachineCamera`, destroys the previous main camera and reparents the new one. `CameraController` (on the `CinemachineCamera`) does pan/zoom/rotation and is driven entirely by the new Input System — the project is `activeInputHandler: 1` (new input only), so legacy `Input.*` calls throw at runtime and must never be reintroduced.

### Run lifecycle
`GameManager.StartRun()` / `ResetRun()`: fade → `LeanPool.DespawnAll()` + despawn tutorials → deplete `gameSettings.resetedCurrency` → `GameData.ResetRun()`. `OnRunStart` / `OnRunEnd` are the hooks for game code. `GameManager` is `partial`, so run logic specific to the deckbuilder belongs in a second file under `_GameAssets/Scripts/`.

### Currency
`CurrencyAsset` (SO) wraps the `Currency` enum plus icon / display name / TMP sprite tag. `GameData.currencies` is keyed by the **enum**, so renaming or reordering `Currency` members invalidates existing saves. Currency gains can auto-feed tracked values via `CurrencyAsset.trackedValuesWithCurrencyGained`.

### Tracked values
`TrackedValueType` (enum) → `GameData.trackedValues`, with `GameData.OnTrackedValueChanged` as the broadcast hook. New metrics = new `TrackedValueType` entries (they are auto-seeded in `ResetGameData`).

### Pooling
Direct Lean Pool: `LeanPool.Spawn(prefab, ...)` / `LeanPool.Despawn(this)`, with prefabs assigned on the spawning manager. There is no PoolRef/pool-object indirection — pools are created on demand by LeanPool and cleared by `LeanPool.DespawnAll()` at run reset. UI spawners (`FloatingTextManager`, `FlyingParticleManager`) expose a parent Transform field because UI clones must land under a Canvas to render.

### Audio
`SoundManager` (pooled `AudioPlayer`s, dedicated music/ambient players, per-sound throttling) plays entries from a `SoundLibrary` by the `SoundKeys` enum. **`Team Square/SoundLibrary/Scripts/SoundKeys.cs` is generated** — an Odin `[Button]` on `SoundLibrary` rewrites that exact file from the library's entries. Never hand-edit it; edit the library and regenerate.

## Conventions

- Serialized private fields use `_camelCase`; method parameters are often `_camelCase`; public SO/data fields are plain `camelCase`.
- Odin attributes are the inspector language: `[TitleGroup("Dependencies"/"Settings"/"Variables")]`, `[Required]`, `[Button]` for editor-invocable actions, `[ShowIf]`, `[AssetList]`, `[InlineEditor]`. MyBox `[ReadOnly]` for debug views.
- Logging goes through the `DrowsyLogger` extension methods on `UnityEngine.Object`: `this.Log(msg)`, `this.LogWarning(msg)`, `this.LogError(msg)` — colored and prefixed with the object name. Prefer these over raw `Debug.Log` in new MonoBehaviours.
- Inspector-visible dictionaries/sets use `Utils.SerializableDictionary` / `SerializableHashSet`.
- Tweening is DOTween; store tweens and `Kill()` them before restarting (see `UIContainer`, `CustomButton`).
- Large numbers are displayed via `NumberFormatter.FormatValue` / `value.ToSmartString()`. Currency amounts are `double` (or `ulong` in `Cost`).
- Functions and variable names should make what they're about obvious
- Never write any comment
- Remove every Odin attributes

## Known hazards

- `GameData.SpendCurrency` indexes `currencies[currency]` after an `||` with `noCurrencyRequired`, so enabling that cheat for a currency that was never added throws. `ResetCurrencies()` seeds every key from `GameAssets`.
- Every runtime file that touches `UnityEditor` is wrapped in `#if UNITY_EDITOR` (verified). Keep it that way — an unguarded `using UnityEditor;` compiles fine in the editor and only fails at player-build time.
- To verify compilation without the editor: Assembly-CSharp can be compiled headlessly with Unity's Roslyn (`dotnet "<UnityRoot>/Editor/Data/DotNetSdkRoslyn/csc.dll" @rsp`), feeding it the `HintPath` references and `DefineConstants` from the generated `Assembly-CSharp.csproj` plus `Library/ScriptAssemblies/*.dll` (excluding `Assembly-CSharp*.dll`). Useful when the editor holds the project lock.
