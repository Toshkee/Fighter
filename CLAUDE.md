# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project layout

This repo holds a Unity project nested in `Fighterr/`. Open **`Fighterr/`** as the Unity project root (Unity `6000.4.7f1`), not the repo root. All scripts live under `Fighterr/Assets/Scripts/` and use the `SamuraiFighter.*` namespace family (`Characters`, `Combat`, `Input`, `Match`, `Managers`, `UI`, `Utils`, `EditorTools`).

[`DESIGN.md`](./DESIGN.md) at the repo root is the original design doc (roster, stages, intended systems, combat-feel rules). Treat it as **product intent**, not a description of current code — several systems it lists (`CombatSystem`, `Weapon` SO, `AudioManager`, `VFXManager`, `StageManager`, `InputBuffer`) do not exist yet. Consult it when making design/scope decisions; don't assume what's in it has been built.

## Building / running

There is no CLI build or test command — everything goes through the Unity Editor.

- **Rebuild the Fight scene from scratch:** Unity menu `Fighter ▶ Build Fight Scene` (defined in `Assets/Editor/FightSceneBuilder.cs`). This script is the source of truth for scene wiring — fighters, hitboxes, HUD, camera, walls, ground layers, and the two characters' `MoveData`/`CharacterData` ScriptableObjects are all created/refreshed by it. Prefer editing this builder over hand-wiring `Fight.unity` in the Inspector.
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

## Data-driven combat

All move tuning lives in `MoveData` ScriptableObjects under `Assets/ScriptableObjects/Moves/` and is assigned to a `Fighter` either via Inspector slots (`_lightMove`, `_heavyMove`, `_superMove`, `_fireballMove`) or via `Fighter.AssignMoves(...)` (used by `FightSceneBuilder` and `CharacterData`). When adding a new attack: create/extend a `MoveData` asset — do **not** hardcode frame counts or damage in `Fighter.cs`.

`CharacterData` bundles a character's four-move set and is what `FightSceneBuilder` reads when populating each fighter.

## Conventions worth knowing

- Private fields use `_camelCase` with `[SerializeField]` for Inspector exposure; public surface is PascalCase properties (`IsAttacking`, `FacingRight`, …).
- Enums for all state/action kinds — `FighterState`, `AttackKind`, `HitboxSlot`. Don't introduce string/int tags.
- Hitbox detection uses `Physics2D.OverlapBox` against the `Hurtbox` layer (set up by `FightSceneBuilder.EnsureLayer`). The `Ground` and `Hurtbox` layers are created on demand by the builder if missing.
- Fighter facing is encoded as `transform.localScale.x = ±1`; `Flip()` is the only place that should mutate it during gameplay.
