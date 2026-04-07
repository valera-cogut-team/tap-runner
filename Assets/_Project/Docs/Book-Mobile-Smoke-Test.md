# Mobile / desktop smoke test — Tap Runner sample

Use before tagging a book release build. Devices: at least **one** mid-tier phone (Android or iOS) plus **Editor** or standalone.

## Functional

1. Cold start: Splash visible briefly, then Game screen; no exceptions in Console.
2. **Jump:** Space / tap / Fire1 — character jumps once per press while grounded (no double-jump).
3. **Collision:** Hitting a block → Game Over HUD; forward motion stops.
4. **Restart:** While Game Over, tap / Space → new run; obstacles respawn ahead; score resets to 0 for the run; **best** remains if not beaten.
5. **Persistence:** After new best score, stop Play mode and Play again — best score still shown.

## Feedback (polish)

6. Jump produces a short blip; game over produces noise + optional camera shake (tunable on `TapRunnerTuning` asset).

## Performance (Profiler)

7. **CPU:** Gameplay tick should show **`TapRunnerGameTickService.OnUpdate`** as the main game logic hotspot (not dozens of `Update` scripts).
8. **GC:** During a 60s run, avoid sustained per-frame allocations in the tick path; HUD updates should fire on reactive changes, not every frame with new string garbage (Profiler *GC Alloc* column).

## Safe area

9. On a notched device (or simulator), HUD top text stays inside **safe area** (no clipping under status bar).

## Addressables

10. Delete `Library` (optional clean), open project, run **TapRunner → Project → Bootstrap Content**, then **Play** — content loads or falls back to procedural player/obstacle without blocking.

## Optional

11. **Airplane / background:** Note any `Application.runInBackground` or audio focus behaviour for your publisher QA checklist.
