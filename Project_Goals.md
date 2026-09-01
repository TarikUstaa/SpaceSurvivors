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
| M10 | Juice & UX | ✅ Audio (SFX; music deferred), settings menu, run-stats + high score, "STAGE n/N" HUD, visual pass on level-up / HUD / end screens (CraftPix kit) |
| M11 | Combat content | ✅ New weapons (Plasma Orb AoE, Scatter Shot, Rail Spike, Orbiter, Mine Layer, Static Field), AoE splash, Orbital/Trail/Aura weapon types, 8-branch evolution tree, Pierce + Haste passives |
| M12 | Enemy variety | ✅ Shooter/Charger/Splitter/Brute + enemy projectile system; enemy bonus drops (health capsule, timed power-up + ship aura) |
| M13 | Persistent meta core | ✅ PlayerProfile DTO + IProfileStore/LocalJsonProfileStore (Newtonsoft) + ProfileService seam; run scrap → wallet on run end; wallet on MainMenu + HUD |
| M14a | Permanent upgrade shop | ✅ Damage/Hull/Thrusters/Armour bought with wallet scrap; MetaProgressionService + MetaUpgradeApplier seed the run-start StatSheet; Shop.unity + MainMenu SHOP button; new StatId.DamageResist |
| M14b | Ship shop / hangar | ✅ Scout/Vanguard/Wraith/Ronin — each a hull sprite + run-start StatModifiers via ShipService + ShipApplier; Hangar.unity carousel + MainMenu HANGAR button |
| M14c | Achievements | ✅ (awaiting sign-off) 8 stat-threshold achievements — `AchievementData` (metric + threshold) + `AchievementCatalogue`; `AchievementService` auto-tracks via `ProfileService.Changed` → writes `unlockedAchievementIds`; `Achievements.unity` grid + MainMenu button. Profile schema v2→v3. |
| M15 | Environment & maps | ✅ (awaiting sign-off) `Obstacle` layer + `SpaceSurvivors.Environment` asmdef (`Obstacle` / `HazardZone` / `EnvironmentDirector` chunk streamer — the asteroid/cache/hazard field is the standard arena, the same on every map); `MapData` = backdrop theme + `MapService` (schema v3→v4); 3 backdrops (Milky Way / Crimson Nebula / Supernova) baked by `BackdropTextureBaker`, drawn by `StarfieldParallax.SetBackdrop`; mode → `MapSelect.unity` carousel → PLAY → Game (no MAPS menu); kinematic-ship `Rigidbody2D.Cast` deflection |
| M16 | Balance pass | ✅ (awaiting human playtest) `Editor/BalanceConfig` one-shot tuning applier + `Editor/BalancePlaytest` bot telemetry harness. Curves reshaped to the GDD ramp (gentle 0:00–3:00, no elites, mini-boss @180, hard mid/late escalation). Campaign = 4-boss / 5-stage / ~15-min arc. Damage/FireRate stack ceilings trimmed. |
| QA | Bug-cleanup pass | ✅ (awaiting sign-off) Wallet/scrap made coherent (in-run HUD shows the run haul only — the wallet is a menu concept, banked once by `RunEndScreen`); HUD hidden on run end; `RunEndScreen` `:n0` + WALLET line; overlapping CraftPix header text fixed on Shop/Hangar/Achievements; Settings dim opaque + top sibling; upgrade display names; `Mode_Campaign` copy. Every screen re-checked in play mode. |
| M17 | Weapon visual pass | ✅ (awaiting sign-off) Kenney Particle Pack (CC0) + baked orb sprite + global Bloom volume. `Editor/WeaponVfxBuilder` restyles every projectile (colour identity + trail + glow), builds a distinct `Evo_*` prefab per evolution (base + evolved no longer share one prefab), adds a pooled muzzle flash, makes the Aura ring colour data-driven. Orbiter "+" → glowing teal orb; EventHorizon → black-hole vortex orbs. `Combat/TrailReset` + `Combat/VfxSpinPulse` helpers. Fine visual tuning deferred to human playtest. |
| M18 | Environment & space events | ✅ (awaiting sign-off) SBS "Seamless Space Backgrounds" (CC0) → 6 maps (was 3). New arena props: wreck / debris chunk / crystal / neutral drift-mine / bonus pod. `EventDirector` + `SpaceEventData`/`SpaceEventCatalogue` + `ISpaceEvent`/`SpaceEventBehaviour` running 5 events one at a time (Meteor Shower, Ion Storm, Derelict Convoy, Solar Flare, Wormhole), each with a `UI/EventBanner` announcement; `MapData.signatureEventId` biases a map's flavour event. `Editor/EnvironmentEventsBuilder`. Event balance / nebula brightness / prop density are first-pass — for the human playtest. |
| M19 | Enemy obstacle avoidance + multishot fix | ✅ (awaiting sign-off) `Enemies/ObstacleAvoidance` — new `IVelocityModifier` (forward `CircleCast` vs Obstacle layer; cancels into-surface velocity + slides along it); `Editor/EnemyAvoidanceBuilder` adds it to the 6 non-boss enemy prefabs (probe sized from each collider), bosses excluded. Also: `WeaponController.FireWeapon` fan is now centre-out (shot 0 dead on aim, rest in alternating pairs) — an even multishot used to straddle the target and whiff; odd counts keep the weapon's full designed spread. |
| M20 | Main-menu redesign + meta-screen skin | ✅ (awaiting sign-off) "Plan A" living-diorama menu: parallax starfield + selected-map nebula + idling ship + drifting rocks behind the UI, lit by the Bloom volume; slow camera drift (mouse-lean). Fonts (Google/OFL) Orbitron + Rajdhani + Audiowide; two-line Audiowide logo. `Editor/MainMenuBuilder` (restyle+augment, keeps M10 settings wiring) + `UI/MenuDiorama`/`MenuShowcase`/`MenuStatsReadout`; baked `MenuPanel`/`MenuVignette` sprites; `PilotRecord` card (best time/kills/scrap/achievements). `Editor/MetaScreenSkinner` applies the same skin to Hangar / Achievements / Shop / MapSelect + the settings modal (glass panels, Orbitron titles replacing CraftPix header art, opaque modal dim, Back buttons lifted off the frame). Legacy `Text` throughout — no TMP. Camera drift / logo size / vignette are taste calls for the human playtest. |

*(Order is a proposal — see §8 backlog. Milestones after M10 are not locked; earlier ones stay open to revisits — the assembly split makes that safe.)*

### Post-M20 goals (user, 2026-09-01 — after first real playthrough)

Order not locked; grouped by theme. Details / decisions to be worked out per milestone.

| # | Goal | Notes |
|---|------|-------|
| G1 | **Swarm mechanic** | ✅ (2026-09-01, awaiting sign-off) `Environment/SwarmEvent` — a `SpaceEventBehaviour` that pours ~34 enemies in from 1–2 screen edges over 16s. Runs on `EventDirector`'s **own fixed 60s track** (`_swarmEvent` / `_swarmInterval`), independent of and able to overlap the random event rotation; removed from the random catalogue. Real enemies (loot/XP/alive-count, not auto-released). Uses the existing `EventBanner`. Roster: weighted Grunt/Swarmer + Charger/Shooter. |
| G2 | **Player ↔ enemy readability** | ⏸ **Deferred (2026-09-01).** Tried: player glow halo + high sort order + enemy threat-tint / dark rim / red rim. Player-glow part worked; the enemy recolour/rim looked worse than the natural Kenney sprites (user rejected). Reverted all of it — nothing committed. Revisit **with an enemy art pass** (distinct silhouettes per archetype) rather than tinting the current sprites. |
| G3 | **Skill / upgrade balance pass** | In-run upgrade choices (`UpgradeService` catalogue, evolutions, passives) rebalanced — some are over/underpowered in practice. Pairs with the M16 balance tooling. |
| G4 | **More enemies on screen** | ✅ (2026-09-01, awaiting sign-off) `Editor/BalanceConfig` iter-5 curves — the old curves peaked ~8/s and the screen felt thin all run. Now: ~3/s by 0:45, ~7/s by 2:30, 14–26/s mid-run; `maxAliveEnemies` 400 (Infinite) / 420 (Campaign); every archetype in the roster by ~2:40. Infinite ramps a touch gentler than Campaign so infinite runs last longer. Verified ~360 enemies on screen at 3:20 @ 465 fps (2.2 ms/frame — pooling headroom fine). Multishot train-stagger was tried here and reverted (user preferred the M19 fan). Note: at this density the deferred G2 readability problem resurfaces. |
| G5 | **Replace the Mine weapon** | Drop **Mine Layer** (and its `Trail` `WeaponKind` if nothing else uses it — currently only Deep Mine evolution) and add new weapon(s) in its place. New content on the existing weapon system. |

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

### Backend / database (planned, post-M13 — user, 2026-08-28)
The user intends to add a backend + database later (cloud save, likely online leaderboards for
Infinite mode, possibly accounts). **This does not need to be built now, but it dictates how M13
is designed:**
- M13 defines the profile as a **plain serializable DTO** (`PlayerProfile` — wallet, owned
  upgrades, owned ships, achievement flags, best scores). No `MonoBehaviour`, no Unity refs.
- All game code talks to an **`IProfileStore` interface** (`Load()`, `Save(profile)`), never to
  files directly. M13 ships a `LocalJsonProfileStore` (JSON in `Application.persistentDataPath`).
- The backend later is a **new `IProfileStore` implementation** (HTTP) + a thin auth layer —
  a drop-in, not a rewrite. Same DTO goes to disk or over the wire.
- Keep score/currency mutations funnelled through one service so a future server-authoritative
  check has a single seam.
- Leaderboard submission is an `IProfileStore`-adjacent service with the same local-noop-now,
  remote-later shape.
- Not in scope for the game client: the server itself, its DB schema, hosting — separate track.

**Planned stack (user, 2026-08-28):**
- Backend: **Java 25** (IntelliJ IDEA) — presumably Spring Boot-style REST API.
- DB: **PostgreSQL** (DBeaver as the client tool).
- Auth + cloud save entry point: **Firebase** (Firebase Auth for identity is the likely role).
- Hosting: **Azure**.
- Flow: Unity client signs in via Firebase Auth → gets an ID token → sends it as a Bearer to the
  Java API → API verifies with Firebase Admin SDK → reads/writes Postgres. The Unity
  `HttpProfileStore` needs only a token provider + the base URL.

**Client-side implications to honour when M13 lands:**
- Use **Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json`), not `JsonUtility`, for the profile
  DTO — `JsonUtility` can't do dictionaries / nullable / ISO dates, and the payload must
  round-trip cleanly with a Jackson backend.
- Put an `int schemaVersion` on `PlayerProfile` from day one; keep field names stable and
  snake_case or camelCase consistently (agree with the backend once).
- The DTO is the contract shared between Unity and the Java API — one source of truth for its shape.

## 6. Non-Goals (for now)
- Multiplayer, controller remapping UI, Steam integration, localization, mobile build.
- 3D art, procedural narrative, save-scumming mid-run.
- Backend/server & database — planned but post-M13; M13's save layer is built interface-first
  so adding it is a new implementation, not a rewrite (see §8).

## 7. Tech Baseline
- Unity **6000.5.9f1**, 2D Universal (URP).
- Input System package (not legacy `Input.GetAxis` long-term — see AI_Guidelines).
- Art: Kenney.nl CC0 space packs.
