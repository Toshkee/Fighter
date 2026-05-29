# Samurai Fighting Game — Claude Code Context

## Project overview

A 2D samurai fighting game built in C# using Unity. Multiple fighters with distinct weapons, special moves, and playstyles battle across a variety of themed stages. The focus is on tight controls, satisfying hit feedback, deep combat feel, and strong visual presentation.

## Tech stack

- **Language:** C#
- **Engine:** Unity (2D mode)
- **IDE:** VS Code with Unity extension
- **Target:** Desktop (Windows/Mac/Linux), potential Steam release

## Architecture

### Core systems

- `Fighter` — state machine per character (Idle, Walk, Jump, Crouch, Attack, AirAttack, Hit, BlockStun, KnockDown, Dead)
- `Weapon` — ScriptableObject with stats (reach, damage, speed, weaponType) and list of `Move` objects
- `InputBuffer` — stores last ~20 frames of input, detects sequences for special moves, supports keyboard and gamepad
- `CombatSystem` — hitbox/hurtbox overlap detection, damage calc, knockback, hitstop, combo counter
- `AnimationController` — Unity Animator integration, frame data tied to states via animation events
- `StageManager` — loads stage, manages background layers, stage hazards, boundaries
- `AudioManager` — hit sounds, music, screen shake, pitch variation on repeated hits
- `GameManager` — round logic, timer, score, match flow (best of 3, character select → fight → result)
- `VFXManager` — spark prefabs, blood/dust particles, sword trail, screen flash on super moves

### Folder structure

```
/Assets
  /Art
    /Characters      — sprite sheets per character
    /Stages          — background layers per stage
    /UI              — HUD, menus, fonts
    /VFX             — particle prefabs, sword trails, hit sparks
  /Audio
    /Music           — stage themes, menu music
    /SFX             — hit sounds, weapon sounds, voice clips
  /Scripts
    /Characters      — Fighter.cs, CharacterData.cs, one file per character
    /Combat          — CombatSystem.cs, Hitbox.cs, Hurtbox.cs, Move.cs, ComboSystem.cs
    /Input           — InputBuffer.cs, InputAction.cs, PlayerInputHandler.cs
    /Weapons         — Weapon.cs, WeaponData.cs (ScriptableObject)
    /Stages          — StageManager.cs, StagePlatform.cs, StageHazard.cs
    /UI              — HealthBar.cs, RoundTimer.cs, HUD.cs, ComboCounter.cs, CharacterSelect.cs
    /Managers        — GameManager.cs, AudioManager.cs, VFXManager.cs, SceneLoader.cs
    /Utils           — Extensions.cs, FrameTimer.cs, ObjectPool.cs
  /Prefabs
    /Characters      — one prefab per fighter
    /Stages          — one prefab per stage
    /VFX             — reusable effect prefabs
  /ScriptableObjects
    /Characters      — CharacterData assets
    /Weapons         — WeaponData assets
    /Moves           — MoveData assets
  /Scenes
    MainMenu.unity
    CharacterSelect.unity
    Fight.unity
    ResultScreen.unity
```

## Characters (roster)

### Kenshin — Katana
- **Style:** Fast rushdown, short range, low damage per hit, high combo potential
- **Specials:** Dash slash, rising cut (anti-air), counter stance (parry into punish)
- **Super:** Phantom blade — teleports behind opponent, multi-hit slash

### Yori — Naginata
- **Style:** Zoner, long range, slow attacks, high damage, controls space
- **Specials:** Spinning sweep (low), charged thrust (breaks guard), wall of steel (area denial)
- **Super:** Storm of the polearm — full-screen spinning attack

### Riku — Twin Sai
- **Style:** Tricky mixup fighter, close range, fast overhead/low mix
- **Specials:** Shadow step (teleport dash), sai throw (projectile), rising fang (reversal)
- **Super:** Phantom rush — rapid 12-hit combo

### Tama — War Fan
- **Style:** Defensive and technical, strong normals, command grabs
- **Specials:** Fan gust (pushback projectile), deflect (reflect projectile), iron fan smash (grab)
- **Super:** Tempest fan — massive wind hitbox, fullscreen

### Gorou — Kanabō (iron club)
- **Style:** Grappler/powerhouse, very slow, extremely high damage, armour on moves
- **Specials:** Ground slam (shockwave), shoulder charge (breaks guard), oni grab (command throw)
- **Super:** Earthbreaker — leaps off screen, crashes down with a shockwave

### Sayo — Kusarigama (chain sickle)
- **Style:** Unique range — sickle for close, chain for mid/far, unpredictable hitboxes
- **Specials:** Chain whip (mid range), chain snare (pull opponent in), sickle dive (air)
- **Super:** Death spiral — chains around both characters, multi-hit pull-in finisher

## Stages (roster)

### Crimson Dojo
- Indoor dojo, sunset light through paper screens, cherry blossoms drifting in
- Music: tense koto and taiko

### Burning Bridge
- Night battle on a wooden bridge over a river, fire spreading in the background
- Hazard: bridge collapses in sudden death — smaller platform
- Music: fast taiko drums

### Mountain Shrine
- High altitude shrine at dawn, fog below, torii gate in background
- Music: calm shakuhachi flute that builds during the round

### Demon's Gate
- Dark castle gate, lightning, possessed armor soldiers in the background
- Music: dark orchestral with heavy percussion

### Bamboo Forest
- Dense bamboo at dusk, wind moving the stalks, fireflies
- Music: ambient and eerie, builds into intense taiko

### Harbor at War
- Burning ships in the bay, chaos in background, waves affecting foreground
- Hazard: cannonball occasionally hits the stage edge
- Music: chaotic war drums and shamisen

## Combat design rules

- **Hitstop:** Freeze both characters for 4–8 frames on hit (scale with move strength). Non-negotiable — this is what makes hits feel real.
- **Hitbox/hurtbox:** Hitboxes only active on specific frames. Hurtboxes always active except during invincible frames (super flash, some reversals).
- **Input buffer:** Accept inputs up to 8 frames early. Never drop a move input on fast execution.
- **Special moves:** Defined as MoveData ScriptableObjects — input sequence, startup frames, active frames, recovery frames, hitbox data, animation clip reference, VFX/SFX references.
- **Super moves:** Require full super meter. Super flash (brief freeze + flash) before the move activates.
- **Combo system:** Track hit count and damage scaling. Damage scales down after 3+ hits to prevent infinite combos.
- **Knockback:** Applied as velocity, decays with friction. Heavier weapons = more knockback. Wall splat if opponent hits stage edge during knockdown.
- **Guard:** Hold back to block. Blocking costs chip damage. Some moves are unblockable or guardbreak.

## Code conventions

- PascalCase for classes and public members
- camelCase for private fields with underscore prefix (`_health`, `_currentState`)
- Enums for all state, action, and input types — no magic strings or ints
- All move/character/weapon data as ScriptableObjects — never hardcode stats in scripts
- Unity Animation Events to trigger hitbox activation/deactivation — frame-accurate
- Object pooling for VFX and projectiles — never Instantiate/Destroy in combat loop
- Keep `GameManager` as a singleton for global state only — no combat logic there

## What to prioritise

1. **Game feel first** — hitstop, screenshake, sound variation, and hit sparks before new features
2. **Two characters playable** before building the full roster
3. **One stage complete** before building the rest
4. **Data-driven everything** — moves, characters, stages all defined as ScriptableObjects so tuning never requires code changes
5. **Polish the loop** — character select → fight → result screen → rematch should feel complete early

## What to avoid

- Don't put combat logic inside `Fighter.cs` — keep it in `CombatSystem`
- Don't use `Update()` for frame counting — use a dedicated `FrameTimer` utility
- Don't add a third character until the first two feel complete and balanced
- Don't skip hitstop — it's the single biggest factor in combat feel
- Don't use `Instantiate`/`Destroy` for hit sparks or projectiles — use object pooling
- Don't hardcode frame data as magic numbers — everything goes in ScriptableObjects

## Unity-specific notes

- Use **Physics2D.OverlapBox** for hitbox detection (not OnTriggerEnter — too imprecise for frame data)
- Use **Unity Input System** package (not legacy Input) for gamepad + keyboard support
- Use **Cinemachine** for camera (shake, zoom on supers, follow)
- Use **Universal Render Pipeline (URP)** for post-processing (bloom on super flashes, vignette on low health)
- Animate everything in **Unity Animator** with sub-state machines per character state group
- Use **Animation Events** in clips to fire hitbox on/off, sound cues, and VFX spawns

## Useful references

- Unity 2D docs: https://docs.unity3d.com/Manual/Unity2D.html
- Unity Input System: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- Cinemachine: https://docs.unity3d.com/Packages/com.unity.cinemachine@latest
- ScriptableObjects guide: https://docs.unity3d.com/Manual/class-ScriptableObject.html
- Fighting game frame data primer: search "fighting game frame data explained"
- Hitbox/hurtbox in Unity: search "Unity 2D fighting game hitbox hurtbox tutorial"
