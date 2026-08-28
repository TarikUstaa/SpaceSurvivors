# SpaceSurvivors — Project Goals

## 1. Vision
A 2D space-themed survival / auto-shooter (Bullet Heaven) inspired by *Vampire Survivors*.
The player pilots a lone spaceship against escalating swarms of enemies, automatically
firing weapons while dodging, collecting Scrap (XP), and building a unique run through
roguelite upgrade choices. Runs are short (target 15–25 min), highly replayable, and
driven by emergent build synergies.

## 2. Pillars
- **Motion is the only defense.** No manual attacking — positioning and kiting are the skill.
- **Every run is a build.** Upgrade choices compound into wildly different playstyles.
- **Readable chaos.** Hundreds of entities on screen, but the threat is always parseable.
- **Fast feedback loops.** Kill → loot → level → choose → feel stronger, in seconds.

## 3. Core Gameplay Loop
1. Move (WASD) to survive and to steer through Scrap pickups.
2. Weapons auto-fire on individual cooldowns.
3. Enemies die → drop Scrap/XP → player collects.
4. XP bar fills → game pauses → choose 1 of 3 random upgrades.
5. Difficulty scales with time survived; repeat until death or victory.

## 4. Development Milestones
| # | Milestone | Definition of Done |
|---|-----------|--------------------|
| M0 | Project scaffolding | Folders, reference docs, URP 2D configured, Git-ignore in place |
| M1 | Player movement | Ship moves with WASD, screen-clamped, tuned via ScriptableObject |
| M2 | Health & damage | `IDamageable`, `HealthComponent`, death events, player + dummy target |
| M3 | Weapons + pooling | `WeaponController`, projectile pool, first weapon (laser) auto-fires |
| M4 | Enemies + spawning | Enemy pool, chase AI, time-based `SpawnDirector`, contact damage |
| M5 | Scrap / XP / level-up | `ICollectible`, XP pool, level-up pause, 1-of-3 upgrade UI |
| M6 | Upgrade system | Upgrade ScriptableObjects, stat modifier pipeline, weapon evolutions |
| M7 | Difficulty director | ✅ Continuous spawn/HP scaling, timed boss schedule, mini-boss + boss HUD |
| M8 | Main menu + game modes | ✅ Menu scene, mode selection, Campaign + Infinite modes, Victory/Defeat screens |
| M9 | Meta & polish | Parallax starfield background, audio (SFX + music), settings, run-stats screen, persistent high score |
| M10 | Balance pass | Tuning against the targets below, playtest telemetry |

### Game modes (M8)
- **Campaign** — a finite run in scripted stages. Timed countdowns escalate the fight; a
  **mini-boss** partway through, then a **final boss** as stage 3. Defeat the final boss to
  complete the mode (Victory screen). ~fixed length.
- **Infinite / Survival** — no win condition. Survive as long as possible; difficulty
  climbs forever (spawn rate, enemy HP/speed, recurring bosses). Score = time survived.
- Both modes are the same core loop driven by a different `GameModeData` asset
  (a `DifficultyConfig` + win condition + stage schedule). Menu picks the mode → loads the
  game scene.

## 5. Difficulty & Balance Targets (GDD)
- **Feel:** hectic but survivable — the player should almost always have an out.
- **Buildup phase:** first **0:00–3:00** is a ramp; enemy density low→moderate, no elites.
- **First mini-boss:** spawns at **exactly 3:00** (180s survived).
- **Player power scaling:** effective combat power roughly **doubles every 2 levels**
  (≈ +41% per level, compounding), via damage / fire-rate / multi-projectile upgrades.
- **Enemy scaling:** spawn rate and enemy HP scale **continuously as a function of time
  survived** (not in discrete waves) — smooth curves, tunable via ScriptableObject.
- **Level cadence:** ~1 level every 20–30s early, stretching as XP requirements grow.
- **Session length:** design target 20 min to reach the "win" condition / endless flip.

## 6. Non-Goals (for now)
- Multiplayer, controller remapping UI, Steam integration, localization, mobile build.
- 3D art, procedural narrative, save-scumming mid-run.

## 7. Tech Baseline
- Unity **6000.5.9f1**, 2D Universal (URP).
- Input System package (not legacy `Input.GetAxis` long-term — see AI_Guidelines).
- Art: Kenney.nl CC0 space packs.
