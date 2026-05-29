# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project layout

This repo holds a Unity project nested in `Fighterr/`. Open **`Fighterr/`** as the Unity project root (Unity `6000.4.7f1`), not the repo root. All scripts live under `Fighterr/Assets/Scripts/` and use the `SamuraiFighter.*` namespace family (`Characters`, `Combat`, `Input`, `Match`, `Managers`, `UI`, `Utils`, `Stages`, `EditorTools`).

[`DESIGN.md`](./DESIGN.md) at the repo root is the original design doc (roster, stages, intended systems, combat-feel rules). Treat it as **product intent**, not a description of current code — several systems it lists (`CombatSystem`, `Weapon` SO, `StageManager`, `InputBuffer`) do not exist yet. Consult it when making design/scope decisions; don't assume what's in it has been built.

## Audio & VFX (procedural, no asset files)

All sound and most hit VFX are **generated in C# at runtime** — the project ships no `.wav` files or particle textures, so combat always has feedback without external assets.

- `Utils/ProceduralSfx` synthesizes every SFX clip (`SfxId` enum) and a looping taiko music bed from raw samples.
- `Managers/AudioManager` is a self-bootstrapping singleton (same lazy `EnsureInstance` pattern as `HitstopController`/`CameraShake`). Call `AudioManager.Play(SfxId)`, `AudioManager.PlayHit(heavy, comboStep)` (pitch rises with combo), `AudioManager.PlayMusic()`. It pre-warms all clips on first call (done in `MatchController.Start` via `PlayMusic`).
- `Combat/Vfx` spawns asset-free directional spark/dust bursts (`Vfx.Hit/Block/Parry/Dust`) on unscaled time so they animate through hitstop. It supersedes the older `Combat/HitFeedback` (now unused).
- `UI/LowHealthVignette` is a self-building red screen-edge overlay bound to a `Health`.

**Cinematics / "alive" layer** (added in the game-feel pass — these are what make it read as a game, not a tech demo):
- `Combat/BattleCamera` (on the Fight scene's Main Camera, configured by the builder with both fighter transforms + stage bounds) owns the camera transform: follows the fighters' midpoint, zooms by their separation, clamps to stage bounds, shakes, and zoom-**punches** on heavy hits (`AddPunch`). All smoothing is unscaled-time so it animates through hitstop/slow-mo. `CameraShake.Shake` now **forwards to BattleCamera when present** (legacy self-shake is the fallback) — so don't add CameraShake to the camera when BattleCamera is there.
- `Combat/KoSequence.Play()` runs the KO drama (freeze-frame → slow-mo → ramp back) and is the sole `Time.timeScale` owner during it — it calls `HitstopController.Cancel()` first so the two never fight. Triggered from `Fighter.OnDied`, alongside `BattleCamera.FocusKo(loser)`.
- `Characters/MotionTrail` (auto-wires to the Fighter+SpriteRenderer; builder just AddComponents it) spawns fading afterimage ghosts during dashes and heavy/super attacks.
- `UI/AnnouncerText` (on the banner) punch-scales the banner text on change; `MatchController` runs a "P1 VS P2" intro card → "ROUND n" → "FIGHT!" sequence (names from `GameSession`), and `Vfx.Celebrate` showers the round winner.
- `Combat/FloatingText` = world-space `TextMesh` popups (damage numbers, "GUARD"); spawned from `Hitbox` per hit.
- `Combat/Vfx` also has `ImpactRing` (expanding ring on heavy/super), `CoreFlash` (bright contact pop), `Celebrate` (gold win shower), all via the `ScaleFadeFx` grow-and-fade helper.
- NOTE: combat VFX/popups still use `new GameObject` per spawn (not pooled) — fine at current scale, but the design doc wants pooling here if projectile/VFX volume grows.
- Sound/VFX hooks live in `Hitbox` (impact/block/parry — heavy vs light chosen by the `_heavyImpact` flag the builder sets on the heavy hitbox), `Fighter` (swing/jump/dash/land/KO), and `MatchController` (round start + music). When adding a new sound, add an `SfxId` + a generator in `ProceduralSfx`; don't import audio files.

## Building / running

There is no CLI build or test command — everything goes through the Unity Editor.

- **Rebuild the whole game:** Unity menu `Fighter ▶ Build Everything` (in `Assets/Editor/MenuScenesBuilder.cs`) — builds the Fight scene then the three front-end scenes, sets build-settings order (MainMenu → CharacterSelect → Fight → Result), and leaves `MainMenu.unity` open to Play. `Fighter ▶ Build Menu Scenes` rebuilds just the front-end.
- **Rebuild the Fight scene from scratch:** Unity menu `Fighter ▶ Build Fight Scene` (defined in `Assets/Editor/FightSceneBuilder.cs`). This script is the source of truth for scene wiring — fighters, hitboxes, HUD, camera, walls, ground layers, the `FightBootstrap`, and the two characters' `MoveData`/`CharacterData` ScriptableObjects are all created/refreshed by it. Prefer editing this builder over hand-wiring `Fight.unity` in the Inspector.
- **Play:** open `Assets/Scenes/Fight.unity` and press Play.
- **Sprite import:** `Assets/Editor/SamuraiSpriteImporter.cs` provides another menu item for slicing the Samurai sprite sheets.

When asked to change scene composition, edit `FightSceneBuilder.cs` and re-run the menu item rather than walking through Inspector clicks.

## Architecture — how a hit actually happens

Combat is **frame-based and runs in `FixedUpdate`**. Every duration in `MoveData` and `Fighter` (startup, active, recovery, hitstun, dash frames, parry window, hitstop) is a count of physics frames, not seconds.

The hit pipeline crosses several files — worth knowing end-to-end:

1. `PlayerInputHandler` (Input System callbacks) → calls `Fighter.TryLightAttack` / `TryHeavyAttack` / `TrySuper` / `TryFireball` / `TryDash`.
2. `Fighter.StartAttack` enters `FighterState.Attack`, stashes the chosen `Hitbox` and the move's frame data, and ticks `_attackFrame` each `FixedUpdate`.
3. On the startup frame, `Hitbox.Activate(activeFrames, damage, knockback, hitstop)` turns the hitbox on for N frames.
4. `Hitbox` detects overlap with a `Hurtbox` on the opposing fighter and calls into `Health` (damage), `HitstopController` (freeze both fighters for `hitstopFrames`), `HitFlash` (sprite flash), `CameraShake`, and notifies the attacker via `Fighter.NotifyHitLanded` (which feeds `SuperMeter`).
5. `Health.OnDamaged` event → `Fighter.OnDamaged` → transitions to `FighterState.Hit` for `_hitstunFrames`. Blocking and i-frames (dash, parry) short-circuit this.
6. `MatchController` watches `Health.OnDied` to run round/match flow; `GameManager` is a singleton for global state only.

**Combo chaining** lives in `Fighter.TickAttack` + `TryHitboxMove`: after a move's active frames end, `_comboWindow` opens; if the next attack lands inside that window, `MoveData.comboDamageBonus` and `comboStartupReduction` apply (clamped by `comboMinStartup`).

**Supers** consume `SuperMeter` via `MoveData.superCost`, trigger `SuperFlash` for `superFlashDuration` frames before the hitbox activates.

**Projectiles** (fireball) are still `Instantiate`d in `Fighter.SpawnFireball` — the design doc calls for object pooling here; if you add more projectile-heavy moves, introduce a pool rather than scaling `Instantiate`.

## Stages & roster

- **Stages** are built at runtime by `Stages/StageRenderer` (added to the Fight scene by the builder). It picks between two themes per match: **Mountain Shrine** (fully procedural — gradient sky, sun, mountain silhouettes, torii, floor, petals) and **Crimson Dojo** (the painted `Art/Stages/Dojo/background.png` + floating dust + lantern glow). All procedural sprites are generated from `Texture2D` — no art needed for the shrine. To pin a stage, turn off `_randomEachMatch` and set `_kind`. `Stages/DriftingPetals` is the reusable ambient-particle component (petals or dust). Background layers use negative `sortingOrder`; fighters are at 1.
- **Roster** (6): Kenshin + Yori are baked into the Fight scene as defaults; Riku, Tama, Gorou, Sayo are added by `FightSceneBuilder.EnsureExtraRoster` (one `EnsureRosterEntry` call each — frame data per archetype). CharacterSelect auto-lists **every** `CharacterData` asset, so adding a fighter = adding an `EnsureRosterEntry` call. Characters are still visually distinguished only by `tint` (no per-character sprite sheets yet).

## Scene flow / front-end

The game is four scenes: **MainMenu → CharacterSelect → Fight → Result**, built by `MenuScenesBuilder` (front-end) + `FightSceneBuilder` (Fight). Flow state and transitions live in `Managers/`:

- `GameSession` — persistent (`DontDestroyOnLoad`) singleton holding the chosen `CharacterData` for P1/P2, `RoundsToWin`, and `LastWinner`. Created on demand via `GameSession.GetOrCreate()`.
- `SceneFlow` — scene-name constants + `Load(name)` (resets timescale, plays a UI blip).
- UI controllers `MainMenuController` / `CharacterSelectController` / `ResultController` wire their own button listeners at runtime (the builder only assigns the `Button`/`Text` references) and also handle keyboard nav. Menu scenes get an `EventSystem` + `InputSystemUIInputModule` for mouse clicks.
- `Match/FightBootstrap` runs in the Fight scene's `Awake`: if a `GameSession` exists it applies the selected characters to the in-scene fighters (`Fighter.AssignMoves/SetMovement/SetHitboxSizes` + sprite tint) and the round count. **With no session (Fight played directly) the baked Kenshin/Yori defaults stand — keep this fallback intact so the Fight scene is testable alone.** Likewise `MatchController.BeginMatchEnd` only routes to the Result scene when a `GameSession` exists, else falls back to the in-place banner + R-restart.

## Data-driven combat

All move tuning lives in `MoveData` ScriptableObjects under `Assets/ScriptableObjects/Moves/` and is assigned to a `Fighter` either via Inspector slots (`_lightMove`, `_heavyMove`, `_superMove`, `_fireballMove`) or via `Fighter.AssignMoves(...)` (used by `FightSceneBuilder` and `CharacterData`). When adding a new attack: create/extend a `MoveData` asset — do **not** hardcode frame counts or damage in `Fighter.cs`.

`CharacterData` bundles a character's four-move set and is what `FightSceneBuilder` reads when populating each fighter.

## Conventions worth knowing

- Private fields use `_camelCase` with `[SerializeField]` for Inspector exposure; public surface is PascalCase properties (`IsAttacking`, `FacingRight`, …).
- Enums for all state/action kinds — `FighterState`, `AttackKind`, `HitboxSlot`. Don't introduce string/int tags.
- Hitbox detection uses `Physics2D.OverlapBox` against the `Hurtbox` layer (set up by `FightSceneBuilder.EnsureLayer`). The `Ground` and `Hurtbox` layers are created on demand by the builder if missing.
- Fighter facing is encoded as `transform.localScale.x = ±1`; `Flip()` is the only place that should mutate it during gameplay.
