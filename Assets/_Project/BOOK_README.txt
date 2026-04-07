Tap Runner — book companion sample (Unity HyperCasual / Mid-Core Architecture)

Unity version
  See ProjectSettings/ProjectVersion.txt (expected: 6000.x LTS / project pinned version).

Open this project
  1. Clone or unzip the source bundle.
  2. Open the project folder in Unity Hub (same major/minor as ProjectVersion.txt).
  3. Open scene: Assets/_Project/Scenes/BootstrapScene.unity
  4. Ensure bootstrap runs AppEntryPoint (menu: TapRunner → Project → Bootstrap Content if Addressables / prefabs are missing).

First run checklist
  - TapRunner → Project → Bootstrap Content (creates tuning asset, player/obstacle prefabs, registers Addressables).
  - Press Play: Splash → Game; Space or tap = jump; hit block = game over; tap again = restart.
  - Best score and games played persist via Storage module keys TapRunner.* in PlayerPrefs (default storage).

Controls
  - Keyboard: Space = same as tap (Fire1).
  - Mouse: left button = Fire1.
  - Touch: first finger down = jump / retry.

Layout
  - Gameplay: Assets/_Project/Source/Modules/Game/TapRunner/
  - Bootstrap registers Tap Runner as TapRunnerCoreModule (data/facade) + TapRunnerRuntimeModule (tick + scene), see AppBootstrap.cs.

Further reading (repo)
  - Assets/_Project/Docs/GameTemplate-README.md
  - Assets/_Project/Docs/Book-Mobile-Smoke-Test.md
