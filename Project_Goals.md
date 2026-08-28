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
| M9 | Open arena + camera | ✅ VS-style scrolling arena: camera follows player, no screen clamp, ring spawning, far-cull, parallax starfield |
| M10 | Juice & UX | Audio (SFX + music), settings menu, run-stats + high score, "STAGE 1/3" HUD, visual pass on level-up / HUD / end screens (CraftPix kit) |
| M11 | Combat content | New weapons incl. an area-of-effect weapon; weapon evolution tree (extends M6 evolutions) |
| M12 | Enemy variety | New enemy archetypes; ranged enemies (enemy projectile system) |
| M13 | Persistent meta core | Save system + currency wallet; scrap carries run → profile. Foundation for M14. |
| M14 | Meta screens | Permanent upgrade shop (buy stat upgrades with scrap), ship shop + buyable ships, achievements. All on top of M13; may split. |
| M15 | Environment & maps | Asteroids / obstacles, interactive/destructible objects, map variants |
| M16 | Balance pass | Tuning against the targets below, playtest telemetry |

*(Order is a proposal — see §8 backlog. Milestones after M10 are not locked; earlier ones stay open to revisits — the assembly split makes that safe.)*

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

## 8. Feature backlog (user's idea dump, 2026-08-28)

Raw list from the user, with where each lands:

| Idea | Milestone | Notes / dependencies |
|------|-----------|----------------------|
| Weapon evolution tree | M11 | Extends the M6 evolution system (Laser→Prism already exists). Self-contained in Combat/Progression. |
| Ranged enemies + new enemy types | M12 | Ranged needs a new enemy-projectile system (new but contained). Plain new archetypes are cheap (new EnemyData + prefab). |
| AoE weapon + more weapons | M11 | Pure content on the existing weapon system. AoE = new hit shape (overlap circle). |
| Currency-farming mechanic | **M13** | Scrap already exists per-run (`ScrapCollector.TotalScrap`). Needs a **persistent wallet** that survives the run. This is the linchpin for the 3 items below. |
| Ship shop + buyable ships | M14 | Needs M13. CraftPix kit has Ship_Shop / Hangar / Ship_Parts art. New ships = PlayerConfig variants + sprite. |
| Achievements | M14 | Needs M13 (persistent store) + an event bus to track conditions. |
| Settings screen | M10 | Standalone. Audio volume, maybe graphics/vsync. |
| Permanent upgrades (buy stat upgrades with scrap) | M14 | Needs M13. Feeds the player `StatSheet` at run start via a profile of owned upgrades. CraftPix Upgrade art. |
| New maps + environment art (asteroids etc.) | M15 | Builds on M9's open arena. Asteroids = obstacle colliders / destructibles. |
| Visual pass on level-up + other screens | M10 | Convert the code-built bootstrap UI (RunHud, BossHud, LevelUpScreen, DamageVignette) to prefabs with CraftPix art. |
| Interactive environment objects | M15 | Destructible crates, hazard fields, pickup shrines. Builds on M15 environment work. |

**The key structural note:** currency-farming / ship shop / permanent upgrades / achievements
(4 of the 11) all sit on one prerequisite — a **persistent save + profile system** (M13).
Build that once and they all become tractable; attempt them piecemeal and each reinvents saving.
So the recommended path is: M10 polish (no new systems, fast win) → M11–M12 content (fun, low-risk,
self-contained) → M13 save core → M14 meta screens → M15 environment.

## 6. Non-Goals (for now)
- Multiplayer, controller remapping UI, Steam integration, localization, mobile build.
- 3D art, procedural narrative, save-scumming mid-run.

## 7. Tech Baseline
- Unity **6000.5.9f1**, 2D Universal (URP).
- Input System package (not legacy `Input.GetAxis` long-term — see AI_Guidelines).
- Art: Kenney.nl CC0 space packs.
