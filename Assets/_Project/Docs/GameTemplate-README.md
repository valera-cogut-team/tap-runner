# GameTemplate layout (`Assets/_Project/`)

This tree is the **minimal modular stack** used in the book: Zenject `DiContainer`, `IModule` / `ModuleManager`, and feature modules (Core, UI, game).

## Bootstrap

| Path | Role |
|------|------|
| `Source/Bootstrap/AppEntryPoint.cs` | Unity entry: creates `DiContainer`, runs `AppBootstrap.InitializeAsync`. |
| `Source/Bootstrap/AppBootstrap.cs` | Registers Core modules, then **Tap Runner** (`TapRunnerCoreModule` + `TapRunnerRuntimeModule`), then UI / AppFlow. |

## Core (shared)

Under `Source/Modules/Core/`: Logger, LifeCycle (`IUpdateHandler` / `ILateUpdateHandler`), Input, Addressables, Storage, Timer, StateMachine, Pool, Audio, Effects, Shaker, etc.

**Performance note (book Ch. 8 / 14):** Prefer **one** gameplay tick via `ILifeCycleFacade` instead of many `MonoBehaviour.Update` scripts for simulation.

## UI flow

Splash → Game is driven by **AppFlow** and **ScreenRouter**; it does not hard-code TapRunner. The game module spawns its world under `TapRunnerWorld` when enabled.

## Chapter 11 sample

| Path | Role |
|------|------|
| `Source/Modules/Game/TapRunner/` | Tap Runner: Domain / Application / Presentation / Facade / Data + `TapRunnerCoreModule` + `TapRunnerRuntimeModule`. |

## Chapter 12 (optional, not in this repo branch unless added)

Book companion may reference `IdleBuilder` under `Source/Modules/Game/IdleBuilder/` — wire it in `AppBootstrap` like TapRunner if you ship that sample.

## Editor tooling

**TapRunner → Project → Bootstrap Content** — creates Addressables entries, `TapRunnerTuning` asset, gameplay prefabs, validates `AppEntryPoint` in the bootstrap scene.

Batch (CI): `-executeMethod EditorTools.TapRunnerProjectTools.BatchBootstrap`
