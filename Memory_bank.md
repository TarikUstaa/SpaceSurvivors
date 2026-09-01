# SpaceSurvivors — Memory Bank

> Working memory log. Update after every major milestone. Newest entry on top.

## Current State
**M1–M14c approved. M15 committed `18de62c` (environment & maps, revised per user). M16 BUILT 2026-08-31, awaiting human playtest sign-off = balance pass — `Editor/BalanceConfig` (one-shot tuning applier for all curves/roster/bosses/XP/ceilings) + `Editor/BalancePlaytest` telemetry harness (orbit-the-horde bot via `Player/ExternalMoveInput`, CSV output). Iter-3: first 3:00 gentle (no elites, mini-boss @180), then a hard mid/late ramp; Campaign now a 4-boss/5-stage/~15-min arc; Damage/FireRate stack ceilings 5→4 / 8→6. Bot is noisy on survivability — needs the human feel-check. Next: M17 (nothing formally scoped yet) or address the M16 caveats.**

Deferred: gameplay music (needs a CC0 pack); in-run achievement toast (M14c follow-on); enemy obstacle-avoidance `IVelocityModifier`; map preview art; the M16 caveats (bot noise, on-screen density, MaxHealth stacking, hazard-zone count). Full detail for each milestone is in its section below.

### M7 — Mini-boss / boss schedule (built, play-tested OK)
- `DifficultyConfig` +`List<BossEntry> bossSchedule` (`{triggerTime, bossData, count, warningLead}`). Default entry at 180s. Wired: [{180s, MiniBoss, ×1, lead 4s}, {360s, MiniBoss, ×2, lead 4s}].
- `SpawnDirector.CheckBossSchedule(now)` — each entry fires ONCE when `now >= triggerTime`: raises `BossIncoming(name, lead)`, then `StartCoroutine(SpawnBossAfter)` waits `warningLead` then spawns from offscreen. Bosses IGNORE the wave budget + `maxAliveEnemies` cap; HP = `baseHealth * Lerp(1, healthMult, 0.5)`. `OnBossReleased` doesn't touch `_aliveCount`. Events: `BossIncoming`, `BossSpawned`.
- `Scripts/Enemies/BossMarker.cs` — IPoolable, `static BossMarker Active` (reset via RuntimeInitializeOnLoad), `DisplayName`, `Health`. Scale-in "arrival pop" (0.2→1 over 0.5s) in OnSpawned.
- `Scripts/UI/BossHud.cs` — on `BossIncoming`: flashing red "!! MINI-BOSS APPROACHING !!" banner for lead+1s. While `BossMarker.Active` alive: bottom-center name + red health bar (anchor-driven fill). Own canvas (order 300).
- Assets: `Prefabs/Enemies/MiniBoss.prefab` (enemyBlack5, red tint, scale 3, solid CircleCollider r0.48, full enemy component set + HitFlash + BossMarker; SeparationSteering radius 1.2/str 0.6). `ScriptableObjects/Enemies/MiniBoss.asset` (650 HP, 1.7 spd, 20 contact/0.6s, scrap 60, earliestSpawnTime 99999 so never in normal roster). `Prefabs/Enemies/BossDeath.prefab` (scale 3.5 orange PooledSpriteAnimator).
- Scene: UI +BossHud (wired _spawnDirector). PoolManager prewarm +MiniBoss(3) +BossDeath(3).
- Claude play-test (temp triggerTime 6s, reverted to 180): boss spawned after 3s warning, scale-in pop to 3.0, BossHud bar showed, HP ~591/651; killed → BossMarker.Active cleared, BossDeath VFX spawned, 60-scrap pickup dropped. Continuous spawn/HP scaling was already M4.

### M6 — Stat modifier pipeline + new weapons + evolutions (built, play-tested OK)
- `Scripts/Stats/StatId.cs` — enum: MoveSpeed, MaxHealth, Damage, FireRate, ProjectileCount, ProjectileSpeed, ProjectilePierce, ProjectileLifetime, PickupRadius, XpGain. + `ModifierOp` {Flat, PercentAdd, Multiplier}.
- `Scripts/Stats/StatModifier.cs` — serializable {stat, op, value}.
- `Scripts/Stats/StatSheet.cs` — player component. `Modify(id, base) = (base+Σflat)·(1+Σpercent)·∏(1+mult)`. `AddModifier(s)`, `AddModifiers(list)`, `Changed` event.
- Migrated OFF the M5 interim fields — deleted `WeaponController.DamageMultiplier/…`, `PlayerMovement.SpeedMultiplier`, `ScrapCollector.RadiusMultiplier`. All now read `StatSheet.Modify(...)`. `_stats` serialized on WeaponController/PlayerMovement/ScrapCollector/HealthComponent (all optional, null-safe → enemies unaffected).
- `Combat/ShotParams` struct (damage/speed/lifetime/pierce) — WeaponController resolves it via StatSheet, passes to `Projectile.Launch` (signature changed).
- `HealthComponent._stats` (player only): max HP runs through StatId.MaxHealth; listens to `StatSheet.Changed`, +MaxHealth also heals by the added amount.
- `UpgradeData` REWORKED: `StatModifier[] modifiers` + `SpecialEffect {None, GrantWeapon}` + `weaponToGrant`. (old `UpgradeKind`/`value` gone.)
- `WeaponData` +`evolvesInto` +`evolutionCatalyst` (UpgradeData that must be maxed).
- `UpgradeService` REWRITTEN: `Roll(count)` returns `UpgradeOffer[]` (normal OR evolution); a ready weapon evolution takes a guaranteed first slot. `Apply(offer)` → pours modifiers into StatSheet / grants weapon / evolves weapon. Tracks `_timesTaken` + `_evolved`.
- `WeaponController` +`Weapons` (read-only view), `HasWeapon`, `EvolveWeapon(from,to)`.
- `LevelUpScreen` uses `UpgradeOffer`; evolution card tinted gold.
- **DELETED** `Scripts/Dev/DebugDamageDealer.cs` + folder. Removed the missing-script component from Player in the scene (K/L/J test keys are gone — kill yourself by letting enemies hit you).
- Assets: `Weapons/Missile.asset` (30dmg, 1.5s cd, speed 11, pierce 1) + `Prefabs/Projectiles/Missile.prefab` (spaceMissiles_001). `Weapons/PrismLaser.asset` (16dmg, 0.38cd, 5 proj, 26° spread, pierce 2) — LaserBlaster.evolvesInto, catalyst = Damage upgrade (maxStacks 5). `Upgrades/GetMissiles.asset` (GrantWeapon, weight 0.8, maxStacks 1). 6 stat upgrades re-authored to modifier form.
- Scene: Player +StatSheet (wired into 4 components). Systems/UpgradeService catalogue = 7. PoolManager prewarm +Missile(24).
- Claude play-test: MoveSpeed 6→6.72 (+12%), Damage 10→12.5 (+25%) via StatSheet; Missile granted → [Laser Blaster, Missile] both firing; Damage×5 → "EVOLVE: Prism Laser" offered as guaranteed card → applied → [Prism Laser(5 proj, pierce 2), Missile]; evolution doesn't re-offer. Console clean.

### M6 follow-up (user: missile too small; ProjectileCount not visibly affecting missile; wants missile explosion)
- Missile.prefab: localScale 0.5→1.0, sprite spaceMissiles_001→spaceMissiles_016 (cleaner capsule), collider 0.35×0.75.
- ProjectileCount DID work on missile (1 base + Flat mods) but `Missile.spreadAngle` was 0 → the extra missiles stacked invisibly. Set `Missile.spreadAngle = 14`. Verified: MultiShot×2 → 3 missiles/volley fanned (z-angles spread).
- `Prefabs/Projectiles/MissileImpact.prefab` — bigger (scale 1.7) orange PooledSpriteAnimator, frames [laserBlue08/09/10 + spaceEffects_012/014 smoke] @16fps. `Missile.asset.impactVfxPrefab` → it. PoolManager prewarm +MissileImpact(20). (Note: Kenney packs have no real explosion sheet; this is flash+smoke composite.)

### M6 follow-up 2 (user: missiles too slow; wants a "took damage" effect)
- `Missile.asset` projectileSpeed 11→22 (lifetime 3, range ~66).
- `Scripts/Combat/HitFlash.cs` — tints a SpriteRenderer toward a flash colour on `HealthComponent.Damaged` for ~0.09s, lerps back. Reusable (enemies later). On Player.
- `Scripts/UI/DamageVignette.cs` — full-screen red edge overlay; own canvas (order 200). `Damaged` → alpha spikes to 0.6 then fades (`_fadeSpeed`); a constant sine pulse below `_lowHpThreshold` (0.3). raycastTarget off. On the `UI` object.
- Generated `Assets/_Project/Resources/DamageVignette.png` — 256² procedural radial gradient (transparent centre → white edges, SmoothStep 0.45→1.05), imported as Sprite uncompressed clamp. Loaded via `Resources.Load<Sprite>("DamageVignette")` (also serialized on the component).
- Verified in play: after a hit, vignette color.a ≈ 0.56, player sprite RGBA(1, 0.39, 0.39).

### M6 follow-up 3 (user: enemies pass through the ship; wants a stackable self-regen shield)
- **Solid enemies:** Grunt prefab collider isTrigger false. Player Rigidbody2D → **Kinematic** (useFullKinematicContacts on). Enemy↔Player physics collide (matrix already ON); Enemy↔Enemy stays OFF (SeparationSteering handles spacing). Result: enemies chasing you stop at the hull (can't overlap); you moving push them along. `ContactDamage` now handles OnCollisionStay/Enter as well as OnTriggerStay (still layer-masked, single-target cd).
- `Stats/StatId.cs` +`ShieldCharges` (appended — enum indices safe).
- `Core/IDamageInterceptor.cs` — `bool Intercept(in DamageInfo)`. `HealthComponent.TakeDamage` consults `GetComponents<IDamageInterceptor>()` before applying HP loss; if one returns true → hit fully absorbed, TakeDamage returns true (attacker still "connected").
- `Combat/ShieldComponent.cs` (player, IDamageInterceptor): MaxCharges = base(0) + `StatId.ShieldCharges` from StatSheet → the Shield upgrade both unlocks & stacks. Absorb: −1 charge, reset regen timer, reflect `info.Amount + _reflectBonus`(12) to `info.Source`. Regen: +1 charge every `_rechargeTime`(6s) after the last loss. Barrier: while up, enemies touching the ship collider take `_barrierDamage`(6) every `_barrierInterval`(0.35s) via OnCollisionStay (Dictionary<Collider2D,float> cd, cleared on exit).
- `Combat/ShieldView.cs` — child "ShieldBubble" SpriteRenderer (shield3.png, cyan a0.35, scale 1.7): visible while charges>0, alpha scales with charge fraction, pop-scale flash on absorb.
- `UI/RunHud.cs` +shield pip line ("SHIELD ●●○", hidden when MaxCharges 0).
- Asset `Upgrades/Shield.asset` — {ShieldCharges, Flat, 1}, weight 0.9, maxStacks 4. Catalogue now 8.
- Missile speed 22 (from prev follow-up), collider capsule.
- Claude play-test: 2 Shield upgrades → 2/2; hit from grunt → player HP unchanged, shield 2→1, grunt 20→0 (reflect); ~8s later shield back to 2/2; HUD "SHIELD ●●"; bubble shows/hides; kinematic player shoves solid grunt (dist stays ~0.87, no overlap).

### M6 follow-up 4 (user: shield sprite should step 1→2→3 as upgraded, and grow)
- `ShieldView` now takes `Sprite[] _tierSprites` — picks `_tierSprites[Clamp(MaxCharges-1, 0, len-1)]` on ShieldChanged, and scales `_designScale * (1 + (MaxCharges-1)*_scalePerTier)` (0.18/tier). A max-charge increase also fires the pop-flash ("power up").
- Scene ShieldBubble: `_tierSprites` = [shield1, shield2, shield3] (Kenney Effects: shield1=small front arc → shield2=¾ bubble → shield3=full bubble). Default SpriteRenderer sprite = shield1.
- Verified clean: max 0→shield1(default), 1→shield1, 2→shield2, 3→shield3; scale grows per tier.
- M6 follow-up 6 (user: feels like taking damage as shield breaks): `HealthComponent.GrantInvulnerability(seconds)` (extends `_iFrameTimer`, never shortens). `ShieldComponent.Intercept` calls it with `_graceAfterAbsorb`(1s) after every absorb → i-frame check in TakeDamage sits before the interceptor loop, so the grace window also stops rapid contact ticks from draining remaining charges. Not re-tested per user.

### M6 follow-up 5 (user: enemies need a death animation)
- `EnemyData` +`deathVfxPrefab`.
- `Scripts/Enemies/DeathVfxSpawner.cs` — on Systems, subscribes `SpawnDirector.EnemyKilled` → pools `data.deathVfxPrefab` (or `_fallbackVfx`) at the death position. Enemy prefab stays VFX-agnostic (§1, §4).
- `Prefabs/Enemies/EnemyDeath.prefab` — PooledSpriteAnimator: [laserBlue08, laserBlue10, spaceEffects_009/012/014] warm-white @20fps, rot/scale jitter. Prewarm 40.
- `HitFlash` added to Grunt prefab (white, 0.08s) → the killing blow flashes the enemy white just before the puff.
- Grunt + Swarmer `deathVfxPrefab` = EnemyDeath.
- Verified in real gameplay: Pool_EnemyDeath created, cycles (40 total, 39 inactive / 1 active mid-anim), never expands. NOTE: enemies spawned directly via PoolManager.Spawn (bypassing SpawnDirector.SpawnOne) don't get the death VFX — SpawnDirector subscribes brain.Killed there; only matters for test scripts.

### M5 — Scrap / XP / Level-Up (built, wired, play-tested OK)
- `Scripts/Core/ICollectible.cs` — `void Collect(GameObject collector)`.
- `Scripts/Data/ProgressionConfig.cs` — SO, `CostForLevel(L)= (base+perLevel*(L-1)) * softGrowth^(L-1)`, clamped.
- `Scripts/Data/UpgradeData.cs` — SO, `UpgradeKind` enum {WeaponDamage, FireRate, MoveSpeed, MaxHealth, PickupRadius, ExtraProjectile}, value/weight/maxStacks.
- `Scripts/Progression/LevelSystem.cs` — player component. `AddXp()`, events `XpChanged(into,need)` / `LeveledUp(level)`. Multi-level-up in one gain handled.
- `Scripts/Progression/ScrapCollector.cs` — player. Owns collect/magnet radii + `RadiusMultiplier`. `Absorb(scrap,xp)` → LevelSystem. TotalScrap stat. Gizmos.
- `Scripts/Progression/XpPickup.cs` — pooled (IPoolable) + ICollectible. Idle bob → magnet fly (accel) → absorb on contact. Distance-based, no collider.
- `Scripts/Progression/LootDropper.cs` — on Systems. Subscribes `SpawnDirector.EnemyKilled` → spawns ONE pooled pickup carrying `EnemyData.scrapValue`.
- `Scripts/Progression/UpgradeService.cs` — on Systems. Weighted no-dup `Roll(3)`, `Apply()` switch → per-component multiplier hooks. `TakenCount` tracks stacks.
- `SpawnDirector` now raises `event EnemyKilled(EnemyBrain, Vector2 pos, EnemyData)` (subscribes each brain.Killed, unsub in OnEnemyReleased).
- Runtime stat hooks added (INTERIM — M6 replaces with StatSheet/modifier pipeline; base values still only in SOs): `WeaponController.DamageMultiplier/FireRateMultiplier/BonusProjectiles` (+ `Projectile.Launch` now takes explicit damage), `PlayerMovement.SpeedMultiplier`, `ScrapCollector.RadiusMultiplier`.
- `Scripts/UI/UiBuilder.cs` — static uGUI helpers (bootstrap UI).
- `Scripts/UI/LevelUpScreen.cs` — on `LeveledUp`: timeScale=0, builds own canvas, 3 UpgradeService picks as buttons, apply→drain queue→resume. Skips if `RunController.RunOver`.
- `Scripts/UI/RunHud.cs` — top XP bar (filled Image) + LV label + mm:ss timer + HP readout. Own canvas.
- Assets: `Prefabs/Pickups/ScrapPickup.prefab` (Pickup layer, star1 sprite gold, scale .35, XpPickup+PoolHandle), `Config/ProgressionConfig.asset` (5/4/1.06), 6 `Upgrades/*.asset`.
- Scene: Player +LevelSystem +ScrapCollector. Systems +LootDropper +UpgradeService. UI +LevelUpScreen +RunHud. PoolManager prewarm +ScrapPickup(60).
- Claude play-test: idle player @18s reached LV2, collected 5 scrap, level-up screen paused (timeScale 0) showing 3 choices, picked "Damage" → DamageMultiplier 1→1.25, timeScale→1, panel closed. HUD showed LV2 / HP52/100 / 00:18. ✅
- Still-there: `DebugDamageDealer` on Player (K/L/J test keys) — remove in M6.

### M5 follow-up (user: XP bar frozen; multishot fires same direction)
- `RunHud` XP bar was `Image.Type.Filled` with NO sprite → never rendered fill. Switched to anchor-driven width (`rectTransform.anchorMax.x = fraction`). Verified: fill = 0.185 after partial XP. Rule: don't use Image.Filled without a sprite; anchor-scale instead.
- `LaserBlaster.spreadAngle` 0 → 18. Fan is symmetric around aim: 2 shots → ±9°, 3 shots → -9/0/+9. Verified in play.
- Laser projectile speed later bumped (user: "too slow, go like a bullet"): LaserBlaster speed 24→42 / lifetime 2.5→1.8 (~75u range); PrismLaser 26→46 / lifetime 1.8.
- BUG FOUND + FIXED (user: "sometimes very fast, generally slow"): `Projectile.Update()` was calling `_body.MovePosition()` with `Time.deltaTime` — MovePosition is a FixedUpdate API, so in Update it's framerate-dependent (multiple Updates per physics step overwrite each other → effective speed varies with FPS). Fix: set `_body.linearVelocity = dir*speed` ONCE in `Launch()`, let the 2D solver integrate it; `Update()` now only ticks lifetime. Added `interpolation = Interpolate` + `collisionDetectionMode = Continuous` in `Projectile.Awake` (smooth + no tunnelling for fast shots). Verified: laser holds exactly 42 u/s (moved 625u in 15s), framerate-independent.

### M5 follow-up 2 (user: fan projectiles ALL vanish on one weak enemy; wants hit VFX)
- ROOT CAUSE: multiple projectiles' `OnTriggerEnter2D` all fire same physics step; all saw target alive, all despawned even when 1 kill sufficed.
- FIX: `IDamageable.TakeDamage` now returns **bool** (true = hit landed on a live/vulnerable target). `HealthComponent.TakeDamage` returns false when dead/i-framed/invulnerable/0-dmg. `Projectile` only consumes itself (pierce-- or despawn) when `TakeDamage` returned true — otherwise it flies on. `ContactDamage`/`DebugDamageDealer` ignore the return (statement call, still fine).
  - Unit-tested: hit#1 on 5hp→True; hit#2/#3 on dead→False. ✅
- Impact VFX: `Scripts/Combat/PooledSpriteAnimator.cs` — generic pooled one-shot frame anim (fps, rotation/scale jitter), despawns at end. Reusable for death puffs/muzzle later.
- `WeaponData.impactVfxPrefab` field added. `Projectile.Launch` now also takes `PoolManager` (from WeaponController._pool) to spawn the VFX at the hit point on a landed hit.
- Asset: `Prefabs/Projectiles/LaserImpact.prefab` (laserBlue08/09/10 frames @22fps, cyan, PoolHandle+PooledSpriteAnimator). Wired to `LaserBlaster.impactVfxPrefab`; PoolManager prewarm +LaserImpact(48).

### M4.5 — Run end / game over (user asked: no spawns + game over on player death)
- `Scripts/Core/RunController.cs` — subscribes to player `HealthComponent.Died`. On death: `RunClock.Running=false`, disables `_disableOnEnd[]` behaviours (SpawnDirector), ramps `Time.timeScale` 1→0 over 0.6s unscaled, fires `RunEnded(float seconds)`. `RunOver` flag guards re-entry. Pause plumbing reused by M5 level-up.
- `Scripts/UI/GameOverScreen.cs` — on `RunEnded`, builds its OWN uGUI canvas at runtime (bootstrap UI, replace with prefab in M8): dark panel + "GAME OVER" + "YOU SURVIVED mm:ss" + RESTART button (`SceneManager.LoadScene` current, resets timeScale). Also spawns an EventSystem w/ InputSystemUIInputModule if none.
- Scene: `Systems` +RunController (wired: player health, RunClock, SpawnDirector). New `UI` GameObject +GameOverScreen (wired: RunController).
- Play-tested by Claude: killed player @27s → SpawnDirector.enabled=false immediately, RunOver=true, timeScale→0 after ramp, panel shows "YOU SURVIVED 00:27", 4 enemies frozen, no new spawns. ✅
- NOTE: RunCommand's dynamic-assembly can't reference UnityEngine.UI (uGUI) — build runtime UI inside project scripts (Assembly-CSharp), not in RunCommand. RunCommand also can't use bare `Time`/`CompilationPipeline` (namespace clash w/ its wrapper ns) — fully-qualify.

### M4 — Enemies + SpawnDirector (code + assets + scene wired, play-tested OK)
- `Scripts/Core/RunClock.cs` — survival timer (scaled dt, pauses at timeScale 0).
- `Scripts/Enemies/IMoveStrategy.cs` + `ChasePlayerStrategy.cs` (beeline + optional sine wobble).
- `Scripts/Data/EnemyData.cs` — SO (prefab, baseHealth, moveSpeed, contactDamage, contactInterval, scrapValue, earliestSpawnTime, spawnWeight).
- `Scripts/Data/DifficultyConfig.cs` — SO with 3 AnimationCurves keyed on SECONDS survived (spawnRatePerSecond, healthMultiplier, speedMultiplier) + maxAliveEnemies + roster.
- `Scripts/Combat/ContactDamage.cs` — OnTriggerStay2D, layer-masked, single-target cooldown, `Configure(dmg,interval)` from EnemyBrain. Zero-alloc.
- `Scripts/Enemies/EnemyBrain.cs` — IPoolable; composes HealthComponent + IMoveStrategy + ContactDamage + PoolHandle. `Initialize(target,data,scaledHp,speedMul,onReleased)`. `Killed` event. Despawns to pool on death.
- `Scripts/Enemies/SpawnDirector.cs` — accumulator spawner off DifficultyConfig curves, weighted roster pick by time, offscreen ring placement, alive-count via release callback, maxAlive cap.
- Assets: `Prefabs/Enemies/Grunt.prefab` (Enemy layer, dynamic RB gravity0, trigger CircleCollider r0.42, all 5 components wired), `ScriptableObjects/Config/EnemyBaseHealth.asset` (20), `Enemies/Grunt.asset` (20hp/2.2spd/8dmg/t0), `Enemies/Swarmer.asset` (10hp/3.6spd/5dmg/t25, shares Grunt prefab), `Config/DifficultyConfig.asset`.
- Scene: TestDummy DELETED. Systems now has PoolManager (Laser 40 + Grunt 80 prewarm) + RunClock + SpawnDirector (logEverySpawn=true). Player keeps DebugDamageDealer as a health logger for now.
- KNOWN GAPS (deferred): player death does nothing (no game-over); enemies don't rotate to face player; SpawnDirector keeps spawning after player death.

### M4 follow-up (user feedback: too-fast spawns + enemies overlapping)
- `Scripts/Enemies/IVelocityModifier.cs` — steering-tweak interface layered over IMoveStrategy.
- `Scripts/Enemies/SeparationSteering.cs` — boids separation (new `Physics2D.OverlapCircle` + ContactFilter2D, no enemy-enemy physics). On Grunt prefab: radius 0.8, strength 1.35, mask Enemy.
- `EnemyBrain` now applies all `IVelocityModifier` components after the base move.
- `NearestEnemyAim` migrated off deprecated `OverlapCircleNonAlloc` → `OverlapCircle`+ContactFilter2D.
- Grunt prefab: localScale 0.7 (sprites were oversized).
- DifficultyConfig spawn curve softened for the 0–3:00 buildup: 0.35/s @0s, 0.6 @30s, 1.0 @60s, 1.8 @120s, 3.2 @180s, 5.0 @240s, 7.5 @300s. healthMultiplier: 1→1.8@120s→4.5@300s.
- Re-tested (Claude, worst case = idle invuln lure, weapon off): 47 enemies formed an evenly-spaced crowd/ring around the player, 0 overlapping pairs (<0.3u), avg nearest-neighbour 0.8u. Genre-correct look confirmed via screenshot.
- Unity 6 note: `Object.GetInstanceID()` and `Physics2D.*NonAlloc` are obsolete-as-error / deprecated — use GetEntityId / OverlapCircle(ContactFilter2D). RunCommand console can show STALE compile errors; verify via `Type.GetType` resolution + `EditorApplication.isCompiling`.

### M3 — Weapons + Pooling (code complete, compile clean, assets built)
- `Scripts/Core/IPoolable.cs`, `Pool.cs` (plain C#), `PoolHandle.cs` (self-despawn), `PoolManager.cs` (scene service, prewarm list, `Spawn`/`Despawn`).
- `Scripts/Combat/Projectile.cs` — IPoolable, kinematic RB2D `MovePosition`, trigger hit → `IDamageable.TakeDamage`, pierce/lifetime → `PoolHandle.Despawn()`. Owner-skip so player's own shots don't hit the player.
- `Scripts/Combat/IAimStrategy.cs` + `NearestEnemyAim.cs` (OverlapCircleNonAlloc, LayerMask, forward-fallback).
- `Scripts/Data/WeaponData.cs` — SO (damage, cooldown, speed, lifetime, projectilesPerShot, spreadAngle, pierce, aimRange). Menu: SpaceSurvivors/Combat/Weapon Data.
- `Scripts/Combat/WeaponController.cs` — slot list, ticks cooldowns, `AddWeapon()` for runtime upgrades, fans projectiles across spread, fires via PoolManager.
- **Layers** (TagManager): 6=Enemy, 7=PlayerProjectile, 8=Pickup, 9=Player.
- **Physics2D matrix**: PlayerProjectile only vs Enemy; Enemy-Enemy off; Pickup only vs Player.
- Assets: `Prefabs/Projectiles/Laser.prefab` (layer PlayerProjectile, kinematic RB, trigger capsule, Projectile+PoolHandle), `ScriptableObjects/Weapons/LaserBlaster.asset` (10 dmg, 0.45s cd, speed 14, range 9).
- Pending: add PoolManager to scene, add WeaponController+NearestEnemyAim to Player, put TestDummy on Enemy layer w/ collider, playtest.

### M2 — Health & Damage (code complete, compile clean)
- `Scripts/Core/DamageInfo.cs` — readonly struct (amount, source, hitPoint, hitDirection).
- `Scripts/Core/IDamageable.cs` — `bool IsAlive`, `void TakeDamage(in DamageInfo)`.
- `Scripts/Combat/HealthState.cs` — pure C# HP math (testable, no Unity types).
- `Scripts/Data/HealthData.cs` — SO (maxHealth, invulnerabilityAfterHit). Menu: SpaceSurvivors/Combat/Health Data.
- `Scripts/Combat/HealthComponent.cs` — MonoBehaviour : IDamageable. C# events (HealthChanged/Damaged/Died) + UnityEvents (onDamaged/onHealed/onDied). OnEnable refills (pool-safe). Heal/SetMaxHealth/ResetToFull.
- `Scripts/Dev/DebugDamageDealer.cs` — DEV SCAFFOLD (delete later). Keys: K dmg, L heal, J kill, I toggle invuln. Logs all health events.
- Assets: `Config/PlayerHealth.asset` (100 HP, 0.5s i-frames), `Config/TestDummyHealth.asset` (30 HP).
- Pending: attach HealthComponent to Player, build a Dummy target, playtest with DebugDamageDealer.
- NOT yet done: player death handling (respawn/game-over) — deferred to a later milestone; M2 just proves the health pipeline.


- Unity 6000.5.9f1, 2D Universal (URP) template.
- Reference docs: `Project_Goals.md`, `AI_Guidelines.md`, `Memory_bank.md`.
- Folder tree created under `Assets/_Project/` (Scripts, ScriptableObjects, Prefabs, Art,
  Audio, Scenes, Input, Tests + subfolders).
- Active Input Handling set to **Both** (was New-only). Input System package present.
- Scripts added (compile clean):
  - `Scripts/Data/PlayerConfig.cs` — SO tuning asset (menu: SpaceSurvivors/Config/Player Config)
  - `Scripts/Player/IMoveInput.cs` — input abstraction
  - `Scripts/Player/KeyboardMoveInput.cs` — WASD/arrows via Input System `Keyboard.current`
  - `Scripts/Player/PlayerMovement.cs` — Rigidbody2D velocity movement + screen clamp
  - `Scripts/Player/ShipRotator.cs` — OPTIONAL: turns hull to face travel direction
- Asset created: `ScriptableObjects/Config/PlayerConfig.asset`
- M1 playtest: movement confirmed working by user. Ship did not rotate (by design —
  PlayerMovement only translates). Added optional ShipRotator + turn fields on PlayerConfig.
- Kenney "Space Shooter Redux" imported at `Art/Sprites/Base_Assets/` (ship: playerShip1_blue).
- Next up: build the ship GameObject in SampleScene, wire components, playtest; then
  import Kenney art; then M2 (HealthComponent / IDamageable).

## Milestone Log
| Date | Milestone | Notes |
|------|-----------|-------|
| 2026-08-27 | M0 — scaffolding | Reference files, folder tree, input handling = Both. |
| 2026-08-27 | M1 — movement | ✅ DONE. Approved by user. PlayerConfig + IMoveInput + KeyboardMoveInput + PlayerMovement + optional ShipRotator. |
| 2026-08-27 | M2 — health | ✅ DONE. Approved. Damage/heal/kill/i-frame/invuln all verified via console. |
| 2026-08-27 | M3 — weapons + pooling | ✅ DONE. Approved. Auto-fire + pooled projectiles + IDamageable hit verified via console. LaserBlaster tuned to speed 24 / lifetime 2.5 / aimRange 14 per user feedback. |
| 2026-08-27 | M4 — enemies + spawning | ✅ DONE. Approved (+ M4.5 crowd steering + game-over). |
| 2026-08-27 | M5 — scrap/xp/levelup | ✅ DONE. Approved (+ XP bar fix, multishot fan, no-overkill projectiles, laser impact VFX). |
| 2026-08-27 | M6 — stat pipeline + weapons | ✅ DONE. Approved (+ missile fixes, damage vignette, hit flash, enemy death VFX, solid enemies, shield, laser speed bug). |
| 2026-08-27 | M7 — difficulty director + mini-boss | ✅ APPROVED 2026-08-28 (with M8). |
| 2026-08-28 | M8 — main menu + 2 game modes | ✅ APPROVED 2026-08-28. All 3 paths play-tested. |
| 2026-08-28 | M9 — open arena + camera | ✅ APPROVED 2026-08-28. |
| 2026-08-28 | M10 — juice & UX (pause, settings, HUD retheme, SFX) | ✅ APPROVED 2026-08-28. Music deferred. |
| 2026-08-28 | M11 — combat content | ✅ APPROVED 2026-08-28. New weapons + AoE splash + Orbital/Trail/Aura weapon types + 8-branch evolution tree + Pierce/Haste passives + Mine Layer & Static Field variety weapons. |
| 2026-08-28 | M12 — enemy variety + bonus drops | ✅ APPROVED 2026-08-28. Shooter/Charger/Splitter/Brute + enemy projectile system; health-capsule + timed power-up drops with ship aura. |
| 2026-08-28 | M13 — persistent profile + wallet | ✅ APPROVED 2026-08-28. PlayerProfile DTO + IProfileStore/LocalJsonProfileStore (Newtonsoft) + ProfileService seam; run scrap → wallet on run end; wallet on MainMenu + HUD (metal look + baked ScrapChip icon). |
| 2026-08-28 | M14a — permanent upgrade shop | ✅ APPROVED 2026-08-28. Damage/Hull/Thrusters/Armour lines bought with wallet scrap; `MetaProgressionService` + `MetaUpgradeApplier` seed the run-start `StatSheet`; `Shop.unity` + MainMenu SHOP button. New `StatId.DamageResist`. Profile schema v1→v2 (`metaUpgradeLevels`). |
| 2026-08-28 | Session follow-ons | Magnet bonus-drop (pulls every XP drop on the map); pickup size tuning; smoother ship turning (input-based ShipRotator via MoveRotation, camera look-ahead reduced). |
| 2026-08-28 | M14b — ship shop / hangar | ✅ APPROVED 2026-08-28. Scout (free) / Vanguard / Wraith / Ronin — each = a hull sprite + run-start `StatModifier[]` via `ShipService` + `ShipApplier`. `Hangar.unity` carousel + MainMenu HANGAR button. MainMenu wallet restyled to the HUD metal look. |
| 2026-08-31 | M14c — achievements | ⏳ BUILT, awaiting sign-off. 8 stat-threshold achievements (`AchievementData` = metric enum + threshold; `AchievementCatalogue` in `Resources/`). `AchievementService` static auto-tracks via `ProfileService.Changed` → `Evaluate()` → writes `unlockedAchievementIds` + `Save`. `Achievements.unity` 2×4 grid (`Rating/` CraftPix art) + MainMenu ACHIEVEMENTS button. Profile schema v2→v3 (`lifetimeKills` / `bestSurvivalSeconds` / `bestLevel` / `bossKills`); `ProfileService.RecordRun` extended; `RunEndScreen` calls `Evaluate()`. |
| 2026-08-31 | M16 — balance pass | ⏳ BUILT, awaiting human playtest sign-off. `Editor/BalanceConfig` = one-shot applier for all difficulty curves / roster timing / boss schedules / XP curve / upgrade stack ceilings. `Editor/BalancePlaytest` = telemetry harness (orbit-the-horde autopilot via `Player/ExternalMoveInput` shim, CSV to persistentDataPath). Iter-3: first 3:00 gentle (Grunt/Swarmer/Shooter only, mini-boss @180) then a hard mid/late ramp (spawn 2.4→13/s, HP-mult 1.6→11); Campaign = 4-boss / 5-stage / ~15-min arc; Damage/FireRate maxStacks 5→4 / 8→6. Bot noisy on survivability → the feel needs human hands. |
| 2026-08-31 | M15 — environment & maps | ✅ committed `18de62c`. `Obstacle` layer (11) + Physics2D matrix. New `SpaceSurvivors.Environment` asmdef: `Obstacle` (kinematic solid + `HealthComponent`; destructible raises `Destroyed` → director spawns debris VFX + `ScrapReward` scrap), `HazardZone` (`OverlapCircleNonAlloc` ticker damaging Player + Enemy), `EnvironmentDirector` (chunk streamer — deterministic per-cell RNG, pooled, far-cull; **owns the field config** — same asteroid/cache/hazard field on every map). `Data/MapData` (= backdrop theme: sky/star tint + baked nebula sprite) + `MapCatalogue` (Resources). `Progression/MapService` (static; profile schema **v3→v4** `selectedMapId`). 3 maps = **backdrops**: Milky Way / Crimson Nebula / Supernova (`Editor/BackdropTextureBaker` bakes 3 seamless 512² PNGs; `StarfieldParallax.SetBackdrop()` draws one as the farthest parallax layer). Flow: Campaign/Infinite → `MapSelect.unity` (build 5) carousel → PLAY → Game (no MAPS menu button). `PlayerMovement` gains `Rigidbody2D.Cast` obstacle deflection (`_obstacleMask`). `EnemyProjectile._blockLayers`. `StarfieldParallax.SetTint()` / `SetBackdrop()`. |

## Tweaks (2026-08-28)
- `ScrapPickup.prefab` scale 0.35 → 0.6, colour brighter gold (user: XP drops too small).
- Imported CraftPix "Free Space Shooter GUI" pack → `Art/UI/PNG/` (244 sprites, glossy blue
  sci-fi, cyan glow, hex-fill). All batch-configured: Sprite / FullRect / uncompressed / clamp.
  Subfolders: Main_Menu, You_Win, You_Lose, Buttons (BTNs + BTNs_Active states), Main_UI
  (Boss_HP_Bar 3-state, Boss_Name_Table, Health_Bar_Table, Health_Dot, Armor_Bar→shield,
  Stats_Bar, Clock_Icon, Cristal_Icon→scrap, Pause_BTN), Loading_Bar (→XP bar), Setting,
  Pause, Level_Menu, Shop/Upgrade/Hangar/Ship_* (future meta-progression).
  License: CraftPix free — commercial OK, no attribution, but NO redistributing source PNGs
  separately. If the repo ever goes PUBLIC, gitignore `Art/UI/PNG/` and ship only in builds.
  M8 menu + M9 polish will use this kit (replaces the code-built bootstrap UI over time).

## Post-M8 tweaks (2026-08-28, after M8 build, before sign-off)
- **Enemy speed scaling too aggressive** (user: "enemyler çok hızlı bize göre"). Old
  `speedMultiplier` hit ×1.5 → Swarmer 5.4 u/s vs player 6 (uncatchable). New: Infinite
  ×1.0→1.2 over 300s, Campaign ×1.0→1.15 over 150s; `Swarmer.moveSpeed` 3.6 → 3.1.
  Now every enemy stays clearly below player top speed (VS rule: threat = numbers, not speed).
- **Final Boss = distinct ship.** New `Prefabs/Enemies/FinalBoss.prefab` (cloned from MiniBoss),
  sprite = `Extension_Assets/Sprites/Ships/spaceShips_007` (red twin-wing heavy fighter),
  scale 2.5, collider r 0.62, `BossMarker._displayName = "FINAL BOSS"`. `FinalBoss.asset.prefab`
  repointed. MiniBoss is still `enemyBlack5` tinted — a distinct MiniBoss ship is an open option.
- **Boss special drop + guaranteed level-up.** New `Prefabs/Pickups/BossXpOrb.prefab`
  (cloned from ScrapPickup): `powerupBlue_star` sprite, cyan, scale 1.7, spin 90°/s, bigger bob.
  Pipeline additions (all backward-compatible):
  - `EnemyData.specialLootPrefab` (optional) — `LootDropper` drops it instead of the normal
    scrap pickup; still carries `scrapValue` as currency + `scrapValue*xpPerScrap` as XP.
  - `XpPickup._guaranteedLevelUps` (+ `_spinSpeed`) — on collect, forces N full level-ups
    on top of the XP.
  - `LevelSystem.GrantLevels(int)` — instant level completion, discards leftover XP.
  - `ScrapCollector.Absorb(scrap, xp, guaranteedLevels = 0)` — new optional 3rd arg.
  - `MiniBoss.asset` + `FinalBoss.asset` → `specialLootPrefab = BossXpOrb` (`_guaranteedLevelUps = 1`).
  Tested: kill MiniBoss @ ~60s → orb drops → collect → L1→L6 (60 xp ≈ +4 levels, +1 guaranteed).
  Early-game swing is large but intentional (rare reward moment); one-number tweak if too strong.

## M9 — Open arena + camera follow (VS-style) (built 2026-08-28)
The play area was one fixed screen (camera static, player clamped). Now it's an open,
infinitely scrolling arena — the *map/camera* concern the user raised.

- **`Core/CameraFollow.cs`** — `SmoothDamp` follow of a target Transform + velocity look-ahead
  (clamped). On the Main Camera, target = Player. No world bounds (infinite).
- **`SpawnDirector` unchanged** — it already spawns at the *camera* edges, so once the camera
  follows the player, enemies ring-spawn around the player for free.
- **`PlayerConfig.clampToScreen` → false** (field kept for other scenes).
- **Far-cull:** `EnemyData.cullWhenFarOffscreen` (default true; MiniBoss + FinalBoss = false).
  `EnemyBrain._farCullRadius = 45` — past that from the target, `_handle.Despawn()` (no Killed
  event → no loot). Keeps the pool + spawn budget honest when the player keeps running.
- **`Core/StarfieldParallax.cs`** — builds 3 tiled `SpriteRenderer` layers parented to the
  camera, slides each by `camPos * parallaxFactor` wrapped to tile size → seamless infinite
  parallax. Layers: parallax 0.03/0.08/0.16, brightness 0.45/0.75/1.0, density 0.6/1.0/1.7.
- **`Editor/StarfieldTextureBaker.cs`** (menu `SpaceSurvivors/Build/Starfield Texture`) →
  bakes `Art/Sprites/Generated/StarTile.png` (256², seamless, wrap Repeat, ~360 soft stars,
  white / pale-blue / pale-amber). Committed asset; script just regenerates it.
- Main Camera `backgroundColor` → opaque dark navy `#0a0d16` (was alpha 0).
- Play-tested: camera tracks player to x=120+, enemies spawn around the roamed position,
  tagged stale enemies recycled, 3 parallax layers scroll at different rates, player unclamped.

**M9 follow-up (same day, user feedback):**
- Starfield was too dense / eye-straining → baker star counts 260/90/14 → 70/28/6, fainter
  alphas; layer brightness 0.45/0.75/1.0 → 0.30/0.50/0.72, densities 0.6/1.0/1.7 → 0.55/0.85/1.15.
- XP drops barely visible (tiny faint gold star on a busy field) → `ScrapPickup` now uses the
  CraftPix `Main_UI/Cristal_Icon` (green crystal), mint tint `(0.65,1,0.75)`, scale 0.6 → 1.3,
  sortingOrder 6, slow spin 45°/s. Reads clearly against dark space + red enemies.

## M10 — Juice & UX (in progress, 2026-08-28)
Three waves. **Wave 1 done + Claude-tested; waves 2–3 pending.**

### Wave 1 — pause, settings, run-stats, stage indicator
- `Core/SettingsService.cs` — static PlayerPrefs wrapper (master/music/sfx volume, fullscreen),
  `Apply()` (AudioListener.volume + Screen fullscreen), `Changed` event, `[RuntimeInitializeOnLoadMethod]`.
  Music/Sfx values are stored now; the wave-3 audio players will read them.
- `Core/HighScoreService.cs` — static PlayerPrefs, `BestSeconds(modeId)` / `Submit(modeId, secs)`
  keyed by `GameModeData.name`. The local stand-in the backend will later shadow (§8) — writes
  funnel through `Submit` for a future leaderboard seam.
- `Progression/RunStats.cs` — on Systems; counts `SpawnDirector.EnemyKilled`, exposes
  `Kills / Level / Scrap / Seconds` (reads LevelSystem, ScrapCollector, RunClock).
- `SpawnDirector.ScheduledBossCount` added.
- `UI/PauseScreen.cs` — Esc (`Keyboard.current`) or `TogglePause()` toggles a panel + `Time.timeScale`.
  Guards: no-op while RunOver or while another screen owns the freeze (`!_paused && timeScale==0`).
  Buttons: Resume / Settings (swaps to settings sub-panel) / Main Menu.
- `UI/SettingsPanel.cs` — binds 3 volume `Slider`s + a fullscreen `Toggle` to `SettingsService`
  (+ optional % labels). Reused by pause now, main menu later.
- `UI/StageIndicator.cs` — Campaign-only "STAGE n/N" (N = `ScheduledBossCount + 1`,
  n = `BossesDefeated + 1`). Hidden entirely in endless modes.
- `UI/RunEndScreen.cs` extended — now also shows `KILLS/LEVEL/SCRAP` (from `RunStats`) and
  `BEST / NEW BEST m:ss` (from `HighScoreService`).
- `Editor/M10UiBuilder.cs` (menu `SpaceSurvivors/Build/M10 UI (Game scene)`) — idempotent; builds
  `PauseCanvas` (Dim > MainGroup{Pause window + Resume/Settings/Menu} + SettingsGroup{Setting
  window + slider/toggle rows + Back}) with the CraftPix Pause/Setting art, `StageCanvas`, adds
  `RunStats` to Systems, and appends `StatsValue`/`BestValue` to the RunEnd window. Sets 9-slice
  borders on `Pause/Window.png` + `Setting/Window.png`.
- Tested (Infinite + Campaign): pause freezes/resumes, settings sub-panel swaps, sliders write
  `SettingsService` + move `AudioListener.volume`, % labels update, STAGE shows "1/3"→"2/3" as
  bosses die (hidden in Infinite), victory screen shows stats + writes/reads the best time.
- Visual screenshot of the pause/settings UI not captured (ScreenSpaceOverlay doesn't render in
  scene-view capture) — layout is a first pass, tune in wave 2.

### Wave 2 — visual pass (partial, 2026-08-28)
- `RunHud.cs` + `LevelUpScreen.cs` rewritten prefab-style (serialized widget refs, no
  `UiBuilder`). `RunHud` gained a scrap counter (`ScrapCollector.ScrapCollected`).
- `Editor/HudBuilder.cs` (menu `SpaceSurvivors/Build/M10 HUD + Level-Up (Game scene)`) builds
  both with the CraftPix kit: `HudCanvas` (XP bar = `Loading_Bar/Table` + `Loading_Bar_1_2`
  Filled fill; level pill; `Clock_Icon` + timer; `Health_Bar_Table` + green Filled fill + HP
  text; `Armor_Bar_Table` shield row; `Cristal_Icon` + scrap count) and `LevelUpCanvas`
  (`Level_Menu/Window` sliced + `Shop/Prise_BTN_Table` choice slabs). 9-slice borders set on
  those sprites.
- `DamageVignette` — baked a real radial-gradient sprite `Art/Sprites/Generated/DamageVignette.png`
  (was a full-screen flat flash because `Resources.Load("DamageVignette")` returned null);
  HudBuilder wires it into the `_vignetteSprite` field.
- **Level-up modal looks great** (screenshot-verified). HUD is themed + functional but rough:
  XP bar is low-contrast against dark space, clock icon barely visible — needs a polish round.
- HUD polish round (user feedback): XP + health bars unified to one rounded
  `Loading_Bar/Table` frame + inset rounded fill; clock icon anchored left with a gap before
  the timer (was overlapping); shield shown as a compact "SHIELD" + `Armor_Bar_Dot` pip row
  (`RunHud._shieldPips`) instead of a giant bar with one bullet.
- **`UI/StatsPanel.cs`** (new) — "SHIP STATUS" column shown left of the pause window (under
  `PauseCanvas/Dim/MainGroup/StatsColumn`, so it hides with the settings sub-panel). Two aligned
  Text columns (labels / values), rebuilt on show. Sections: RUN (level/time/kills/scrap),
  SHIP (move speed / max HP / shield), OFFENSE (damage% / fire rate / projectiles / pierce /
  proj speed), UTILITY (pickup range / xp gain), WEAPONS (equipped list). Reads `StatSheet.Modify`,
  `HealthComponent`, `LevelSystem`, `RunStats`, `WeaponController`. Pause window shifted right to
  make room. Built + wired by `M10UiBuilder.BuildPause`.
- **Main-menu Settings** — `UI/PanelToggle.cs` (open button shows a panel, close hides it) +
  `M10UiBuilder.BuildMainMenuSettings` (menu `SpaceSurvivors/Build/M10 Main-Menu Settings`):
  a SETTINGS button top-right of the main menu opens a `Setting/Window` pop-up with the same
  3 sliders + fullscreen toggle (`SettingsPanel`) and a CLOSE button. Tested.
- **BossHud re-themed** — `BossHud.cs` rewritten prefab-style (`_warningRoot`/`_warningText`/
  `_barRoot`/`_bossNameText`/`_hpFill`). `HudBuilder.BuildBossHud` builds it with
  `Main_UI/Boss_HP_Table` frame + `Boss_HP_Bar_1` Filled red fill + `Boss_Name_Table` name
  plate. Verified with a spawned mini-boss.

**Wave 2 done** (except a full BossHud warning-banner art pass — the banner is still plain text).

### Wave 3 — audio (SFX only, 2026-08-28)
User dropped the 4 Kenney SFX packs into `Audio/SFX/{Kenney_SciFi,Kenney_UI,Kenney_Impact,Kenney_Interface}/`
(CC0). Music deferred — no pack yet; the system has no music path.
- `Data/SfxId.cs` — enum of sound events. `Data/SfxBank.cs` — SO mapping each id to clip(s) +
  volume + pitch range + `minInterval` (repeat-suppression). Asset: `Config/SfxBank.asset`.
- **`UI/AudioDirector.cs`** — the only thing that plays sound. Pure listener: subscribes on
  Awake to `WeaponController.WeaponFired` (NEW event), `SpawnDirector.EnemyKilled` /
  `BossDefeated` (NEW) / `BossIncoming`, `LevelSystem.LeveledUp`, `ScrapCollector.ScrapCollected`,
  `ShieldComponent.Absorbed`, player `HealthComponent.Damaged`, `RunController.RunEnded`.
  Round-robin pool of 10 `AudioSource` voices. Volume = bank entry × `SettingsService.SfxVolume`
  (master already on `AudioListener.volume`). Lives in UI because it observes every layer;
  gameplay code never calls audio, only raises events.
- `UI/ButtonSfxInstaller.cs` — on the AudioDirector object, Start-wires every scene `Button`
  to `UiClick` (onClick) + `UiHover` (PointerEnter).
- `Editor/AudioBuilder.cs` (menu `SpaceSurvivors/Build/Audio (SFX bank + directors)`) —
  populates `SfxBank.asset` with clip picks, sets `.ogg` import to DecompressOnLoad + forceToMono,
  drops an `AudioDirector` (+ installer) into Game.unity and MainMenu.unity, wires `_bank`.
  (Gotcha: re-load the bank asset by path inside the per-scene step — the just-created
  reference goes stale across `OpenScene`.)
- Play-tested: laser / enemy-death / player-hurt / pickup sounds all fire; 10-voice round-robin
  works; no errors.
- New events added: `WeaponController.WeaponFired(WeaponData)`, `SpawnDirector.BossDefeated`.

### Wave 3 — audio (pending, needs asset pack)
Blocked on a CC0 audio pack (like the UI kit was). Kenney Sci-Fi Sounds / Space Kit suggested.
Then build `AudioDirector` + `SfxEvent` hooks (shoot, hit, enemy death, level-up, boss warning,
pickup, player hurt, win/lose) reading `SettingsService` volumes.

## M11 — Combat content (built 2026-08-28)
**AoE:** `WeaponData` gained `explosionRadius` + `splashDamageFraction` + `explosionVfxPrefab`.
`Projectile` — on a landed hit, if `explosionRadius > 0` it runs `Physics2D.OverlapCircleNonAlloc`
and deals `damage × splashFraction` to every other live `IDamageable` in range (skips owner +
the direct-hit target), spawns the explosion VFX, then despawns (AoE never pierces). Verified:
one Plasma Orb into a 3-grunt cluster → direct hit dead, other two −14 each.

**Orbital weapon type:** `WeaponData.kind` = {Projectile, Orbital} + orbit fields
(`orbitRadius`, `orbitDegreesPerSecond`, `orbitHitInterval`). `Combat/OrbHit.cs` — one orb,
trigger collider, per-enemy hit cooldown (like ContactDamage, player-owned). `Combat/OrbitalWeapon.cs`
— a child of the player per equipped orbital weapon; keeps N pooled orbs evenly spaced on a
circle and spins them, count follows `ProjectileCount` stat, damage follows `Damage` stat.
`WeaponController` skips Orbital weapons in the fire loop and calls `SyncOrbitals()` on
`AddWeapon` / `EvolveWeapon` (tears down + rebuilds the `Orbital_*` children; **destroys the
GameObject, not just the component**).

**New weapons + evolution tree** (assets in `ScriptableObjects/Weapons/`, prefabs in
`Prefabs/Projectiles/`):
| Weapon | Evolves to | Catalyst (max stacks) |
|--------|-----------|-----------------------|
| Laser Blaster | Prism Laser | Damage *(pre-existing)* |
| Missile | Cluster Missile (AoE, 3-way) | MultiShot |
| Plasma Orb (slow AoE lob) | Nova Core | FireRate |
| Scatter Shot (5-pellet shotgun) | Buckshot Storm | Pierce *(new passive)* |
| Rail Spike (fast, pierce 8) | Void Lance | Haste *(new passive)* |
| Orbiter (2 orbs) | Event Horizon (4 orbs, r 3.2) | PickupRadius |
New passives: **Pierce** (`Upgrades/Pierce.asset`, +1 ProjectilePierce Flat, max 4),
**Haste** (`Upgrades/Haste.asset`, +15% ProjectileSpeed, max 5).
New GrantWeapon upgrades: `GetPlasmaOrb / GetScatterShot / GetRailSpike / GetOrbiter`.
`Missile.asset.evolvesInto/evolutionCatalyst` wired.
UpgradeService catalogue in Game.unity: 8 → 14 entries (added the 4 grants + Pierce + Haste).
New prefabs: `PlasmaOrb`, `ScatterPellet`, `RailShard`, `OrbitOrb` (all cloned from Laser.prefab;
OrbitOrb swaps Projectile→OrbHit, kinematic, trigger circle, star1 sprite), `PlasmaBoom`
(scaled MissileImpact — Kenney has no real explosion sheet).
New events used: `WeaponController.WeaponFired` (added M10 for audio), `SpawnDirector.BossDefeated`.

**Play-tested:** all 4 new weapons fire; Orbiter → 2 orbs at r2.2 opposite sides; Plasma splash
hits a cluster; PickupRadius×5 → "EVOLVE: Event Horizon" offered → applied → 4 orbs at r3.2,
old rig cleaned up. Note: PlasmaBoom VFX is a weak stand-in (blobby); orb sprite is `star1`
tinted cyan — both open to an art pass.

### M11 follow-on — weapon variety (approved 2026-08-28)
User: "silahlar biraz aynı gibi… arkamızda bir şey bıraksa giderken falan" → two mechanically
different weapons added.
- **`WeaponKind`** extended: `{Projectile, Orbital, Trail, Aura}`. `WeaponController` renamed
  `_orbitals` → `_specialRigs` (List<GameObject>) and `SyncOrbitals` → `SyncSpecialWeapons`,
  which builds a child rig for **every** non-Projectile kind (switch on kind). Fire loop now
  skips `kind != Projectile` (was only `== Orbital`).
- **`Combat/Aoe.cs`** — shared `Aoe.Splash(center, radius, damage, owner, skip=null)` static
  helper. `Projectile.Explode` refactored to call it (single blast implementation).
- **Mine Layer** (`kind = Trail`): `Combat/MineLayer.cs` child rig drops
  `projectilesPerShot` mines every `cooldown / FireRate` sec, offset behind the ship
  (`-body.linearVelocity.normalized * 0.7` + jitter). `Combat/Mine.cs` — pooled `IPoolable`,
  arms after 0.3 s, detonates on enemy contact **or** lifetime → `Aoe.Splash` + VFX + despawn.
  `MineLayer.asset` (dmg 26, cd 1.3, r 1.9, life 6) → evolves **Deep Mine** (dmg 44, 2/shot,
  r 2.8); catalyst **MoveSpeed** max.
- **Static Field** (`kind = Aura`): `Combat/AuraWeapon.cs` — a trigger `CircleCollider2D` of
  radius `orbitRadius` on a child; tracks enemies in `_inside` (HashSet) via enter/exit; every
  `cooldown` sec damages all of them (`Damage` stat). **Snapshot to `_tick` list before the
  damage loop** — a kill fires OnTriggerExit and mutated the set mid-iteration (crash, fixed).
  Optional faint ring view (`_auraRingSprite` on WeaponController, wired to baked
  `Art/Sprites/Generated/AuraRing.png`). `StaticField.asset` (dmg 6/tick, cd 0.5, r 2.6) →
  evolves **Ion Storm** (dmg 11, r 3.6); catalyst **MaxHealth** max.
- Grants `GetMineLayer` / `GetStaticField` (weight 0.7, maxStacks 1) → catalogue 14 → 16.
- `Mine.prefab` cloned from `OrbitOrb`, OrbHit→Mine, `meteorGrey_small1` sprite tinted warm +
  a red `star1` "ArmLight" child. Play-tested: 20 kills w/ both equipped, mines detonate
  (VFX + splash), aura melts enemies in range, no console errors.
- **Still placeholder:** aura ring look (teal glow — user may want fainter), mine sprite.

## M12 — Enemy variety + bonus drops (approved 2026-08-28)

### Enemy projectile system
- **New layer 10 `EnemyProjectile`** (TagManager.asset) + Physics2D matrix: collides with
  Player (9) only. `Physics2D.IgnoreLayerCollision` set in edit mode persists to
  `Physics2DSettings.asset`.
- `Combat/EnemyProjectile.cs` — pooled straight-mover, `_targetLayers` mask (Player), no
  pierce, `Launch(dir, speed, damage, lifetime, owner, pool)`. `EnemyLaser.prefab` (red,
  cloned from Laser, Projectile→EnemyProjectile, layer 10).

### New archetypes (EnemyData in `ScriptableObjects/Enemies/`, prefabs cloned from Grunt)
| Enemy | Behaviour script | hp / speed / contact | earliest / weight |
|-------|------------------|----------------------|-------------------|
| Shooter (enemyBlue2) | `KeepDistanceStrategy` (kite at r6, strafe) + `RangedAttack` | 14 / 2.4 / 4 | 45s / 0.7 |
| Charger (enemyRed4) | `ChargeStrategy` (approach→windup→dash x4.5→recover) | 26 / 2.0 / 14 | 70s / 0.6 |
| Splitter (enemyGreen3) | `SplitOnDeath` → 3× SplitterMite | 30 / 1.9 / 7 | 90s / 0.5 |
| SplitterMite (enemyGreen1, 0.42) | plain chase | 6 / 3.4 / 4 | split-only (weight 0) |
| Brute (enemyBlack4, 1.2) | plain chase, tank | 120 / 1.1 / 18 | 120s / 0.35 |
- `RangedAttack.cs` — self-contained (like `ContactDamage`): resolves PoolManager + reads
  `EnemyBrain.IsActive`/`Target`, fires `EnemyProjectile` bursts on an interval when in range.
  Shot damage is NOT difficulty-scaled (deliberate — feels fairer).
- **`EnemyBrain` change:** added `public Transform Target =>` (for abilities). Nothing else.
- **`SpawnDirector` change:** `SpawnOne` refactored → `public EnemyBrain SpawnEnemyAt(data, pos)`
  (wires alive-count + Killed event); `SplitOnDeath` uses it so mites are real spawns
  (loot + kill-count normal). Bypasses spawn budget/cap like bosses.
- Both rosters (Infinite `DifficultyConfig.asset` + `CampaignDifficulty.asset`) get the 4 new
  archetypes (not the mite).

### Bonus drops (health capsule + power-up)
- `LootDropper` rolls per kill, independent of scrap: `_healthDropChance` **0.022**,
  `_powerUpDropChance` **0.014** (one or the other, never both). Wired in `Game.unity`.
- `Progression/FlyToPlayerPickup.cs` — abstract base (magnet/bob/fly loop + `ICollectible`),
  abstract `OnCollected`. `XpPickup` deliberately NOT folded in (bespoke Absorb path).
- `HealthPickup.cs` — heals `_healAmount` 25 (capped), spawns `HealBurst` VFX.
  `HealthCapsule.prefab` (`pill_green`, scale **1.3**).
- `PowerUpPickup.cs` — applies a timed `StatModifier` via `PlayerPowerUps`. Default:
  FireRate +40% PercentAdd, 8s, label "Overdrive". `PowerUp.prefab` (`bolt_gold`, scale 1.3).
  Author stat/amount/duration on the prefab → new power-up types = new prefab, no code.
- `PlayerPowerUps.cs` (on Player) — holds active timed buffs; `Apply(mod, dur, label)` pushes
  to `StatSheet`, `Update` pulls it back on expiry, same label refreshes (no stack). Drives
  the gold `PowerUpAura` child (spin + pulse) while any buff is active. Uses `Time.time` so
  buffs pause during the level-up screen.
- **`StatSheet` change:** added `RemoveModifier(in StatModifier)` + `Accumulator.Remove`
  (inverse of Add) — the one API the timed-buff system needs; permanent upgrades unaffected.
- `Combat/OneShotPulse.cs` — generic pooled scale-out+fade flourish. Prefabs `HealBurst`
  (green) / `PowerUpBurst` (gold), both from the baked `AuraRing.png` sprite.

### Tuning applied (user feedback, 2026-08-28)
- XP crystal (`ScrapPickup.prefab`): scale 0.7 → **0.3**, colour green → **purple**
  `(0.72, 0.38, 1)` (was reading as health).
- Play-tested: all 4 archetypes behave; Shooter fires (60+ shots verified, hits player, not
  other enemies); Charger dashes; Splitter 0→3 mites confirmed; Brute ~217 scaled hp.
  Health 40→90 via capsules; power-up FireRate 1.20→1.60 + aura on, clean revert to 1.20 on
  expiry. LootDropper drops both at test rates. No console errors.

## M13 — Persistent profile + currency wallet (approved 2026-08-28)

The linchpin for M14. Everything is in `Core` (no gameplay deps → backend-portable).

- **Package added:** `com.unity.nuget.newtonsoft-json` 3.2.1 (`Packages/manifest.json`).
  It's `autoReferenced`, so `Core` picks it up without an asmdef change.
- **`Core/PlayerProfile.cs`** — the DTO. `schemaVersion` (const `CurrentSchemaVersion = 1`),
  `long wallet`, `long lifetimeScrap`, `int runsPlayed`, `int bestKills`, plus reserved M14
  fields (`ownedUpgradeIds`, `ownedShipIds`, `selectedShipId`, `unlockedAchievementIds`) so
  M14 doesn't bump the schema. No UnityEngine types — round-trips through any JSON lib.
- **`Core/IProfileStore.cs`** — `Load()` / `Save(profile)`. Backend = a later HTTP impl behind
  this, not a rewrite.
- **`Core/LocalJsonProfileStore.cs`** — `profile.json` in `Application.persistentDataPath`
  (ctor takes a filename so tests can use a scratch file). `FilePath` property (NOT named
  `Path` — that shadows `System.IO.Path`, CS1061). Defensive read: missing/corrupt → fresh
  profile, corrupt file copied to `.corrupt-<timestamp>` first. Write goes via `.tmp` then
  move, so a crash mid-save can't half-write. `Migrate()` hook for future schema bumps.
- **`Core/ProfileService.cs`** — the single seam (static, like `SettingsService`).
  `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` boot-loads. `Current` / `Wallet` /
  `LifetimeScrap`; `AddScrap(long)` (credits wallet + lifetime), `TrySpend(long)` (false +
  no-op if short), `RecordRun(kills)`, `Save()`. `SetStore(IProfileStore)` swaps the backend
  (tests / future HTTP) and reloads. `event Changed`.
- **`RunEndScreen`** — on `RunEnded` (win OR lose, VS rule): `ProfileService.AddScrap(RunStats.Scrap)`
  + `RecordRun(Kills)` + `Save()`. Stats line now `SCRAP +N` / `WALLET n`.
- **`MainMenuScreen`** — `_walletLabel` (gold, top-left of MenuCanvas) shows `SCRAP n`, live
  via `ProfileService.Changed`.
- **HUD (`RunHud` + `HudBuilder`)** — the top-right scrap counter now shows the **live total**
  `ProfileService.Wallet + ScrapCollector.TotalScrap` (`n0` format), restyled metal: steel
  text `(0.80,0.84,0.90)` + bold + dark `Outline`, and a baked hexagonal metal-nut icon
  `Art/Sprites/Generated/ScrapChip.png` (steel gradient + specular streak + rim + bolt hole).
  **Fixed a pre-existing bug:** the ScrapGroup had pivot (0.5,0.5) at a top-right anchor with
  a negative offset → it was rendering half off the right edge (invisible in every prior
  screenshot). Now pivot (1,1), children pivot (1,0.5).
- **RunCommand gotcha:** direct `System.IO` calls (`File.Exists`/`Delete`) from a RunCommand
  script trigger a macOS TCC prompt ("User interactions are not supported") because the
  ad-hoc assembly isn't signed for `~/Library/Application Support`. File I/O *inside* Core
  code is fine — test ProfileService through its API, never touch `File` in the test script.
- **Play-tested:** store round-trip (load→AddScrap→Save→reload new instance = persisted);
  `TrySpend` success + insufficient both correct; full run-end death → wallet = RunStats.Scrap,
  runs+1, bestKills, persisted, RunEnd screen shows it; MainMenu + HUD labels display + live-
  update. No console errors. (Test-polluted the real `profile.json` once via an early run-end
  before switching to scratch stores — wiped it back to 0/0 after. Scratch files
  `profile_m13*.json` / `profile_hudtest.json` linger in persistentDataPath, harmless,
  outside the repo, never loaded by the game.)

## M14a — Permanent upgrade shop (approved 2026-08-28)

- **`StatId.DamageResist`** added (fraction of incoming damage ignored, 0..0.85). Applied in
  `HealthComponent.TakeDamage` *after* interceptors, player-only (`_stats != null`). MUST be
  a `Flat` op modifier — its base is 0, so `PercentAdd` on it is always 0 (learned the hard way).
- **`PlayerProfile` schema v1 → v2:** `Dictionary<string,int> metaUpgradeLevels` added.
  `LocalJsonProfileStore.Migrate` normalises null dicts/lists; `ownedUpgradeIds` (unused
  reserved list) removed.
- **`Data/MetaUpgradeData`** — id, title, icon, `StatModifier perLevel`, `maxLevel`,
  `baseCost` + `costGrowth` (`CostForNext(lvl) = round(baseCost · growth^lvl)`).
  **`Data/MetaUpgradeCatalogue`** — `List<MetaUpgradeData>`, one asset in
  `Assets/_Project/Resources/` so a static service can load it without per-scene wiring.
- Catalogue (`Resources/Meta_*.asset` + `MetaUpgradeCatalogue.asset`):
  | id | stat / op / per-level | max | baseCost / growth |
  |----|----------------------|-----|-------------------|
  | damage | Damage PercentAdd +0.06 | 12 | 50 / 1.45 |
  | health | MaxHealth Flat +12 | 12 | 45 / 1.40 |
  | speed | MoveSpeed PercentAdd +0.04 | 8 | 60 / 1.50 |
  | armor | DamageResist **Flat** +0.035 | 10 | 70 / 1.55 |
- **`Progression/MetaProgressionService`** (static) — `Upgrades`, `LevelOf`, `CostToNext`,
  `CanAfford`, `IsMaxed`, `TryPurchase` (→ `ProfileService.TrySpend` + bump level + `Save`),
  `BuildStartingModifiers` (one copy of `perLevel` per owned level — works for any op),
  `SetCatalogue` (test/editor hook), `Changed` event.
- **`Progression/MetaUpgradeApplier`** — `[DefaultExecutionOrder(-100)]` on the Player;
  `Awake` → `_statSheet.AddModifiers(MetaProgressionService.BuildStartingModifiers())`.
- **`UI/ShopScreen`** — prefab-style, `UpgradeRow[]` (upgradeId + icon/title/desc/level/cost/
  buyButton) wired by the builder; BUY → `TryPurchase`; refresh on `Changed` / `ProfileService.Changed`.
- **`UI/LoadSceneButton`** — reusable: `Button.onClick → SceneManager.LoadScene(_sceneName)`.
- **`Editor/ShopBuilder`** — MenuItems `M14a Shop scene` (builds `Shop.unity` from
  `Upgrade/` CraftPix art + catalogue, registers it in Build Settings) and
  `M14a Main-Menu Shop button` (SHOP button under SETTINGS). 9-slice borders set on
  `Upgrade/Window.png`, `Upgrade/Price_BTN_Table.png`, `Shop/Prise_BTN_Table.png`,
  `Main_UI/Stats_Bar.png`. Header uses the plate's own "UPGRADE" text (no label).
- Build scenes now: MainMenu (0), Game (1), **Shop (2)**.
- **Play-tested end-to-end:** buy in shop → wallet down, level up, persisted; start run →
  `StatSheet` gets the mods (Damage ×1.18 from lvl 3, Armour → 100 dmg hit took 93 at lvl 2);
  MainMenu SHOP → Shop → BACK. No console errors. Scratch stores used throughout; real
  `profile.json` reset to 0.

## Session follow-ons (2026-08-28, after M14a)

### Magnet bonus-drop (M12 pickups + 1)
- `Progression/MagnetPickup : FlyToPlayerPickup` — on collect, `foreach XpPickup → Attract()`
  + cyan `MagnetBurst` VFX. `XpPickup.Attract()` = `_flying = true` (ignores magnet range).
- `LootDropper` gets `_magnetPickupPrefab` + `_magnetDropChance` 0.02 (3rd bonus roll after
  health / power-up). Wired in `Game.unity`.
- `Magnet.prefab` (from PowerUp clone, PowerUpPickup→MagnetPickup) + baked
  `Art/Sprites/Generated/MagnetIcon.png` (red horseshoe + silver poles).
- **Sizing note:** generated pickup icons render huge because they're 96px @ 96 PPU = 1 unit,
  while Kenney pills are ~22px @ 100 PPU = 0.2 unit. Fix = raise the sprite's Pixels Per Unit
  (MagnetIcon → 430) OR lower the prefab Transform scale. All pickup sizes live on the
  prefab's Transform > Scale (`ScrapPickup` 0.3, `HealthCapsule`/`PowerUp` 1.3, `Magnet` 1.3).

### Smoother ship turning
- **`ShipRotator` rewritten:** steers toward the raw `IMoveInput` heading (full-rate, no
  zero-crossing on reversal) instead of the physics-stepped velocity, which used to freeze
  below `minSpeedToTurn` on a reversal then snap 180°. Rotates via `Rigidbody2D.MoveRotation`
  in `FixedUpdate` so rotation interpolates in lock-step with position (body has Interpolate).
  Falls back to velocity heading only while coasting.
- `PlayerMovement.Awake` now sets `_body.constraints = None` (was `freezeRotation = true`) —
  needed for `MoveRotation`; kinematic body can't be spun by collisions. `Game.unity` RB
  `m_Constraints` 4 → 0.
- `CameraFollow` look-ahead reduced (was sweeping ~5 units on a direction change, read as the
  world lurching): `_smoothTime` 0.18→0.12, `_lookAhead` 0.15→0.09, `_maxLookAhead` 2.5→1.4.

## M14b — Ship shop / hangar (approved 2026-08-28)

- **`Data/ShipData`** — id, displayName, description, `Sprite sprite`, `int cost`,
  `StatModifier[] runStartModifiers`. A ship = "a permanent build you paid for once".
  **`Data/ShipCatalogue`** — `List<ShipData>` in `Resources/`; the first entry is the free
  starter. No profile schema bump — `ownedShipIds` / `selectedShipId` were reserved in M13.
- Catalogue (`Resources/Ship_*.asset` + `ShipCatalogue.asset`), all use Kenney
  `playerShip*` sprites (all 75px tall → consistent in-game size, no scaling needed):
  | id | sprite | cost | run-start modifiers |
  |----|--------|------|---------------------|
  | starter (Scout) | playerShip1_blue | 0 | — (matches the current player sprite) |
  | vanguard | playerShip3_orange | 700 | +35 MaxHealth Flat, -10% MoveSpeed |
  | wraith | playerShip1_green | 700 | +22% MoveSpeed, -15 MaxHealth Flat |
  | ronin | playerShip2_red | 1400 | +20% Damage, +10% FireRate, -25 MaxHealth Flat |
- **`Progression/ShipService`** (static) — `Ships`, `IsOwned` (starter always true),
  `SelectedId` (falls back to starter when blank / not owned / missing), `Selected`,
  `SelectedSprite`, `CanAfford`, `TryBuy` (→ `ProfileService.TrySpend` + add to
  `ownedShipIds` + auto-select + `Save`), `Select` (owned only), `BuildStartingModifiers`,
  `SetCatalogue`, `Changed`.
- **`Progression/ShipApplier`** — `[DefaultExecutionOrder(-100)]` on the Player. `Awake`:
  `_hull.sprite = ShipService.SelectedSprite` + `_stats.AddModifiers(ShipService.BuildStartingModifiers())`.
  Coexists with `MetaUpgradeApplier` (both just add to the sheet). `_hull` = the player's
  root SpriteRenderer.
- **`UI/HangarScreen`** — one-ship carousel: prev/next cycle, big sprite, name/desc, a
  formatted stat block ("+35 Hull\n-10% Speed"), and a BUY / EQUIP / EQUIPPED action button
  (+ chip on BUY). Wired by the builder.
- **`Editor/HangarBuilder`** — MenuItems `M14b Hangar scene` (builds `Hangar.unity` from
  `Ship_Shop/` CraftPix art + `ShipCatalogue`, registers it in Build Settings) and
  `M14b Main-Menu Hangar button` (HANGAR button under SHOP).
- Build scenes: MainMenu (0), Game (1), Shop (2), **Hangar (3)**.
- **MainMenu wallet restyled** (user: "match the game") — `WalletLabel` now steel
  `(0.80,0.84,0.90)` + bold + dark `Outline`, with a `WalletChip` `ScrapChip.png` icon to
  its left. Same look as the HUD scrap counter.
- **Play-tested:** hangar carousel Next → Vanguard, stats shown, BUY (1000→300 scrap, owned +
  auto-equipped, → EQUIPPED); start run → hull sprite = `playerShip3_orange`, MaxHealth
  100→135, MoveSpeed 6→5.40. MainMenu HANGAR button works. No console errors. Real profile
  reset to 0.

## M14c — Achievements (built 2026-08-31, awaiting sign-off)

- **`Data/AchievementData`** — `id`, `title`, `description`, `Sprite icon`, `AchievementMetric metric`,
  `long threshold`. No unlock state here — that lives in `PlayerProfile.unlockedAchievementIds`.
  **`AchievementMetric`** enum: LifetimeKills, BestKillsInRun, BestSurvivalSeconds, RunsPlayed,
  LifetimeScrap, BossKills, BestLevel, ShipsOwned, MetaUpgradeLevels. Every metric resolves to
  one non-decreasing number → an achievement is just "value ≥ target", no per-achievement code.
  **`Data/AchievementCatalogue`** — `List<AchievementData>` in `Resources/AchievementCatalogue.asset`.
- **Profile schema v2 → v3** — added lifetime counters `lifetimeKills` (long), `bestSurvivalSeconds`
  (int), `bestLevel` (int), `bossKills` (int). Additive numeric fields — migration just bumps the
  version, missing keys deserialise to 0. `ProfileService.RecordRun(kills)` → `RecordRun(kills, level,
  survivedSeconds, bossesDefeated)`, folds them all in. `RunStats.BossesDefeated` getter added
  (reads `SpawnDirector.BossesDefeated`).
- **`Progression/AchievementService`** (static, same shape as `ShipService` / `MetaProgressionService`)
  — `All`, `Find(id)`, `Value(a)` (metric → profile/meta-service number), `Target(a)` (`threshold`,
  or the full ship count when metric = ShipsOwned and threshold ≤ 0), `Progress01(a)`, `IsUnlocked(a)`
  (recorded OR condition met now), `Evaluate()` (records newly-earned, `Save` + `Changed` once,
  returns the new ones), `SetCatalogue`, `Changed`. `[RuntimeInitializeOnLoadMethod] Boot()` wires
  `ProfileService.Changed += () => Evaluate()` — so any scrap-bank / run-record / ship-or-upgrade
  purchase auto-checks achievements (ProfileService.Changed only fires at run end + on shop buys,
  never per-frame; `Save()` doesn't raise it, so no recursion).
- **`RunEndScreen`** — after banking scrap + `RecordRun(...)`, calls `AchievementService.Evaluate()`.
- **`UI/AchievementsScreen`** — one tile per catalogue entry: icon, title, description, and either a
  green "✓ UNLOCKED" stamp or a "value / target" progress line (survival metrics render m:ss).
  Locked tiles dim icon + text. Summary "N / M UNLOCKED". `Start()` also calls `Evaluate()` (catch
  up on unlocks earned elsewhere). Prefab-style, wired by the builder.
- **`Editor/AchievementsBuilder`** — MenuItems `M14c Seed achievement catalogue` (creates/updates
  `Resources/Ach_*.asset` + `AchievementCatalogue.asset` from a hardcoded `Seed[]`), `M14c
  Achievements scene` (builds `Achievements.unity` — `Rating/` CraftPix art, 2×4 grid, registers in
  Build Settings), `M14c Main-Menu Achievements button` (ACHIEVEMENTS button under HANGAR).
- Build scenes: MainMenu (0), Game (1), Shop (2), Hangar (3), **Achievements (4)**.
- **The 8 achievements:**
  | id | title | metric ≥ threshold | icon |
  |----|-------|--------------------|------|
  | first_blood | First Blood | LifetimeKills ≥ 1 | star_bronze |
  | swarm_culler | Swarm Culler | LifetimeKills ≥ 500 | star_silver |
  | exterminator | Exterminator | LifetimeKills ≥ 5000 | star_gold |
  | survivor | Survivor | BestSurvivalSeconds ≥ 300 | Clock_Icon |
  | unbreakable | Unbreakable | BestSurvivalSeconds ≥ 600 | shield_gold |
  | giant_slayer | Giant Slayer | BossKills ≥ 1 | bolt_gold |
  | scrap_baron | Scrap Baron | LifetimeScrap ≥ 5000 | ScrapChip |
  | full_hangar | Full Hangar | ShipsOwned ≥ all (threshold 0) | playerShip3_orange |
- **Play-tested (Claude, scratch store `ach_m14ctest.json`):** clean profile → 0/8, all locked, correct
  progress lines. Injected lifetimeKills 600 / survival 330s / scrap 5200 / bossKills 2 → `Evaluate()`
  returned 5, screen showed 5/8 with First Blood / Swarm Culler / Survivor / Giant Slayer / Scrap Baron
  green-stamped, the rest still counting ("600 / 5,000", "5:30 / 10:00", "1 / 4"). MainMenu
  ACHIEVEMENTS button loads the scene. No console errors. Real `profile.json` untouched.
- **Deferred:** in-run achievement toast / unlock animation (`Evaluate()` already returns the freshly
  unlocked list for a future notifier to consume).

## M16 — Balance pass (committed `10b2e08` 2026-08-31; difficulty feel pending human playtest)

**User's calls:** Campaign win ≈ 15 min (4 bosses / 5 stages); GDD-faithful — no elites in the first 3:00.

- **`Editor/BalanceConfig`** (`SpaceSurvivors/Balance/M16 Apply balance`) — the single source of truth
  for every tuning knob, applied programmatically (AnimationCurve keyframes in YAML are unmaintainable).
  Re-run after any tweak. Sets: both difficulty configs' spawn/HP/speed curves, enemy roster timing,
  boss schedules, the XP curve, and upgrade stack ceilings.
- **`Editor/BalancePlaytest`** (`M16 Sim — Infinite` / `M16 Sim — Campaign`) — telemetry harness.
  Opens Game.unity, points the SpawnDirector at the chosen config, drives the player with a simple
  "orbit-the-horde" autopilot (via the runtime **`Player/ExternalMoveInput`** shim — a settable
  `IMoveInput`, ships, inert unless driven), auto-answers level-ups by a keyword priority list, gives
  the sim player +220% pickup radius (models a competent XP-sweeper), runs at 2.5× with
  `Application.runInBackground = true`, and appends a row every 10 game-seconds to
  `persistentDataPath/balance_<mode>.csv` (t, level, kills, scrap, hp, enemiesAlive, spawnRate). Exits
  play at the time cap or player death. Scene never saved.
- **Tuning applied (iteration 3):**
  | knob | before | after |
  |------|--------|-------|
  | roster: Shooter earliest | 45 | 50 |
  | roster: Charger / Splitter / Brute earliest | 70 / 90 / 120 | **185 / 210 / 300** (no elites &lt; 3:00) |
  | Infinite spawn/s @ 3:00 / 8:00 / 16:00 | 3.2 / 5.3 / 9.5 | **2.4 / 6 / 13** |
  | Infinite HP-mult @ 3:00 / 8:00 / 16:00 | 1.6 / ~3.5 / ~6 | **1.6 / 4.2 / 11** |
  | Campaign boss schedule | 60 (mini), 150 (final) — a 2.5-min run | **180, 420, 660 (mini xN), 900 (final)** — 5 stages, ~15 min |
  | Damage upgrade maxStacks | 5 | 4 |
  | FireRate upgrade maxStacks | 8 | 6 |
  | ProgressionConfig | base 5 / per 4 / growth 1.06 | base 5 / per 5 / growth 1.06 |
  - Iteration 1 kept the first 3:00 soft (from the original) but the mid/late game was trivial for a
    focused build (bot at HP 100 at 12 min). Iteration 2 pushed the *early* ramp + XP too hard → bot
    died at 0:90. Iteration 3 = iter-1's first 3:00 exactly + a much harder mid/late ramp + trimmed
    DPS ceilings.
- **Telemetry (Infinite, iter-3 bot run):** 0:00–3:00 — HP holds 60–90, ≤10 enemies, mini-boss at
  exactly 180s. Power — level ~11–14 by 3:00 (≈1 level/16–20s early, stretching to ~1/30s). Mid/late —
  field grows 8 → 38 enemies, spawn 2.4 → 13/s, HP-mult → 11. A near-optimal focused build + perfect
  kiting stays comfortable the whole 16 min (and tanks to 150 HP late by stacking MaxHealth once the
  offensive upgrades cap).
- **KNOWN CAVEATS / open for the human playtest:**
  - The bot is **noisy** — single runs vary a lot on survivability (one Campaign run bled out at 3:00
    from positioning bad-luck; an Infinite run cruised 16 min). The curve *shapes* are GDD-aligned; the
    exact difficulty *feel* needs real hands.
  - **On-screen density** — at 6:00 the camera shows only ~5–8 enemies (an open arena + a mobile player
    strings the horde into a trailing line; far-cull at 45u despawns stragglers). For a denser "bullet
    heaven" look, bump late spawn rates harder and/or spawn enemies in clumps.
  - **MaxHealth stacking** — after offensive upgrades cap (~level 20) every level only buys defence;
    consider MaxHealth maxStacks 6 → 4.
  - **Hazard-zone count (M15 follow-on)** — the environment field renders a lot of orange hazard rings;
    consider the `EnvironmentDirector` hazard weight 0.7 → ~0.3.

## M15 — Environment & Maps (revised per user, committed `18de62c` 2026-08-31)

**User's steer (mid-M15):** asteroids/caches/hazards should be the STANDARD ARENA (they expected them
in the base game regardless of map). Maps = pure **backdrop themes** ("samanyolu galaksisi / kırmızı
sis perdesi / supernova"). NO standalone MAPS menu section — the map picker opens after choosing a mode.

- **`Obstacle` layer = 11** (`TagManager.asset`). Physics2D matrix (`Physics2D.IgnoreLayerCollision` from
  `EnvironmentBuilder`, **persists** to `Physics2DSettings.asset`): Obstacle collides with Enemy(6),
  PlayerProjectile(7), Player(9), EnemyProjectile(10) only — NOT Pickup(8), not itself.
- **New asmdef `SpaceSurvivors.Environment`** → refs Core, Data, Stats, Combat, **Progression** (for
  `XpPickup` / `ScrapCollector` / `MapService`). Nothing references Environment back (Game asmdef
  unchanged — the scene just hosts the components), so no cycle and the Game↛Progression boundary holds.
- **`Environment/Obstacle`** — `[RequireComponent] Rigidbody2D (Kinematic) + Collider2D + HealthComponent
  + PoolHandle`. `_driftSpeed` (0 = static), `_spinSpeed`, `_scrapReward` (>0 = a cache). Every obstacle
  has a `HealthComponent`: **non-destructible** = `Obstacle_AsteroidHard` HealthData (99999 HP — shots
  just vanish into it via the projectile's own IDamageable path, **no Projectile.cs change**);
  **destructible** = modest HP, raises `event Destroyed(obstacle, pos, DamageInfo)` on death then despawns.
- **`Environment/HazardZone`** — no RB/layer. `Update` runs `Physics2D.OverlapCircleNonAlloc` on
  `_tickInterval` (0.6s), `TakeDamage(_damagePerTick=5)` on every `IDamageable` in `_radius` (2.4) whose
  layer is in `_targetLayers` (Player + Enemy). Child `_pulse` sprite scales sine for readability.
- **`Environment/EnvironmentDirector`** — **owns the field config** now (`_props` PropEntry[] +
  `_propsPerCell`/`_cellSize`/`_spawnClearRadius`/`_ringRadius`), NOT the map. Same asteroid/cache/hazard
  field on every map (asteroids = core gameplay). On `Start` calls `ApplyLook(MapService.Selected ??
  _fallbackMap)` → camera bg + `StarfieldParallax.SetTint` + `SetBackdrop`. Keeps a
  `Dictionary<Vector2Int, List<GameObject>>` of cells within `_ringRadius` (3) of the player's cell; each
  cell's props are `new System.Random(HashCell(cell))` deterministic (fixed seed → backtracking stable);
  cells past ring+1 are depopulated (pooled). On `Obstacle.Destroyed`: unsubscribe, drop from cell list
  (so a later Depopulate can't double-release — `Pool.Release` also guards `!activeSelf`), spawn
  `_debrisVfxPrefab`, and for a cache spawn `Clamp(scrap/5,1,5)` `ScrapPickup`s vs the `ScrapCollector`.
  Field wired by `WireGameScene`: large w2 / small w4.5 / cache w1 / hazard w0.7, propsPerCell 5, cell 14.
- **`Data/MapData`** = a **backdrop theme**: `id/displayName/description/previewSprite`,
  `cameraBackground`, `starfieldTint`, `backdropSprite` (big seamless nebula tex), `backdropTint`.
  No gameplay fields. **`Data/MapCatalogue`** — `List<MapData>` in `Resources/MapCatalogue.asset`,
  first entry = default.
- **`Progression/MapService`** (static, same shape as `ShipService`) — `Maps`, `Find`, `SelectedId`
  (from `PlayerProfile.selectedMapId`, falls back to catalogue[0]), `Selected`, `Select` (→ `Save` +
  `Changed`), `SetCatalogue`. **Profile schema v3 → v4**: added `string selectedMapId` (additive —
  migration just bumps the version). All maps free.
- **`Core/StarfieldParallax`** — `SetTint(Color)` recolours the star layers; **`SetBackdrop(Sprite,
  Color)`** builds (once) / shows / hides one extra farthest tiled layer (`_backdropParallax` 0.012,
  `_backdropDensity` 0.30) for the per-map nebula. Passing a null sprite hides it.
- **`Editor/BackdropTextureBaker`** (`M15 Backdrop Textures`) — bakes 3 seamless (toroidal-wrapped),
  512² PNGs at PPU 18 into `Art/Sprites/Generated/`: **Backdrop_MilkyWay** (diagonal blue band of soft
  blobs + warm core hilites + dust lanes), **Backdrop_Nebula** (scattered red/magenta cloud blobs +
  voids + hot cores), **Backdrop_Supernova** (hot haze + bright orange bursts with faint shock rings).
  Modest opacity, dimmed further in-game via `backdropTint` alpha 0.6.
- **3 maps** (`Resources/Map_*.asset`) = backdrops only: **milky_way** (Milky Way, cool blue),
  **crimson_nebula** (Crimson Nebula, deep red "fog curtain"), **supernova** (Supernova, hot amber).
- **`UI/MapSelectScreen`** — one-map carousel: preview Image shows the `backdropSprite` (or a sky-colour
  swatch), name/desc, **PLAY** (`MapService.Select(current)` + `LoadScene("Game")`), prev/next, BACK
  (→ MainMenu). Built by `Editor/MapSelectBuilder` (`MapSelect.unity`, `Ship_Shop/` CraftPix art; header
  is a plain "SELECT MAP" label).
- **Flow:** `MapSelectBuilder`'s `M15 Route Main-Menu through Map-select` sets `MainMenuScreen._gameSceneName
  = "MapSelect"` and deletes any stray `MapsButton`. So Campaign/Infinite → MapSelect → PLAY → Game.
- **`Player/PlayerMovement`** — the ship's RB is **Kinematic**, so the solver won't stop it at a rock.
  New `_obstacleMask` + `Deflect(velocity, dt)`: `Rigidbody2D.Cast` along the intended move, cancel the
  velocity component pointing into any hit surface → the ship slides along rocks. `_obstacleMask` wired
  to `1<<11` by `EnvironmentBuilder`.
- **`Combat/EnemyProjectile`** — new `_blockLayers` mask: an enemy shot hitting terrain spawns its impact
  VFX and despawns (no damage). (Player `Projectile` needs no change — see Obstacle above.)
- **Prefabs** (`Prefabs/Environment/`, all pooled, prewarmed on `PoolManager`): `Env_AsteroidLarge`
  (`meteorGrey_big4`, scale 1.55, indestructible), `Env_AsteroidSmall` (`meteorBrown_big1`, ~46 HP,
  HitFlash), `Env_ScrapCache` (`things_silver` gold-tinted, 30 HP, scrapReward 18), `Env_HazardZone`
  (`AuraRing` orange pulse), `Env_Debris` (`meteorBrown_med3` OneShotPulse).
- **Build scenes:** MainMenu(0) Game(1) Shop(2) Hangar(3) Achievements(4) **MapSelect(5)**.
- **Editor:** `EnvironmentBuilder` (`M15 Setup layers + physics` / `M15 Build environment assets` (also
  bakes backdrops + builds maps) / `M15 Wire Game scene`), `MapSelectBuilder` (`M15 Map-select scene` /
  `M15 Route Main-Menu through Map-select`), `BackdropTextureBaker` (`M15 Backdrop Textures`).
- **Play-tested (Claude, scratch profile `m15test.json`):** MainMenu → Campaign → MapSelect (3 backdrop
  previews look distinct) → PLAY → Game loads with `Backdrop_<map>` on the farthest layer + matching sky
  tint; `selectedMapId` persists. Field streams the same on every map (400+ obstacles); physics matrix
  correct (Player/Enemy/PlayerProjectile↔Obstacle yes, Pickup no); `Rigidbody2D.Cast` from the player
  detects the rock; small asteroid → debris VFX; cache → 3 scrap `XpPickup`s; hazard tick 100→95. No
  MAPS menu button. No console errors. Real `profile.json` untouched.
- **Deferred:** enemy obstacle-avoidance (they physics-bump rocks, VS-style — add an `IVelocityModifier`
  if clumping is bad in the human playtest); real painted backdrop art; drifting-asteroid variant.

## QA / bug-cleanup pass (2026-08-31, after M16 — awaiting sign-off)
User: "projeyi genel bi inceleyip, bugları temizle. wallet/scrap bağlantıları sıkıntılı. iç içe
giren yazılar varsa düzelt, bütün ekranları incele." Reviewed every screen in play mode + fixed:

- **Wallet vs run-scrap made coherent (the reported bug).** In-run HUD (`RunHud`) showed
  `ProfileService.Wallet + ScrapCollector.TotalScrap` — a double-count, because `RunEndScreen`
  *also* banks the run haul into the wallet on `RunEnded`. Now the HUD shows **this run's haul
  only** (`TotalScrap`). Wallet is purely a menu concept; the run-end screen shows `SCRAP +haul`
  then `WALLET <new total>`. Single banking seam stays `ProfileService.AddScrap` in `RunEndScreen`.
- **HUD stayed visible behind the score panel on run end.** `RunHud` now subscribes to
  `RunController.RunEnded` and disables its canvas. `RunHud` sits on a manager object (not under
  the HUD canvas) so `GetComponentInParent<Canvas>()` is null — resolve the canvas late via
  `Graphic.canvas` of a widget (`_xpFill`/`_healthFill`/`_levelLabel`). `StageIndicator` also
  hides its `_root` on `RunEnded`.
- **`RunEndScreen` stats** — thousands separators (`:n0`) on SCRAP / WALLET; added a `WALLET` line.
- **Overlapping text on the CraftPix header sprites** (baked-in words "UPGRADE" / "SHIP SHOP" /
  "RATING"). Shop + Hangar: header sprite narrowed & centred, the `SCRAP <n>` wallet chip moved to
  its **own centred row** below it (was overlapping the header). Achievements: dropped the `RATING`
  sprite entirely for a plain "ACHIEVEMENTS" text label; tile text columns moved right (x 300→340)
  clear of the icon + given a `Shadow` for legibility over the tile swoosh art.
- **Settings pop-up** — dim backdrop was alpha 0.94 (menu title bled through) and later-added
  menu buttons (SHOP/HANGAR/ACHIEVEMENTS) drew over it. Fix: opaque dim (alpha 1.0) + `PanelToggle`
  calls `transform.SetAsLastSibling()` on open.
- **Upgrade display names** — `FireRate`→"Fire Rate", `MoveSpeed`→"Thrusters", `MaxHealth`→"Hull
  Plating", `PickupRadius`→"Pickup Range", `MultiShot`→"Multi-Shot" (LevelUp screen was showing
  raw ids).
- **`Mode_Campaign` description** → "Five stages. Defeat the final boss to win." (was stale).
- Scene `.unity` diffs are large because the Shop/Hangar/Achievements/MainMenu UI was rebuilt by
  the editor builders — Unity re-serialised the hierarchies. Verified visually, no behaviour change.
- **Verified in play mode:** MainMenu, Settings, Shop, Hangar, Achievements, MapSelect, in-run HUD
  (`0` scrap at start), LevelUp (renamed titles), RunEnd (HUD hidden, `1,234` / `3,247` formatting).
  No console errors. Real `profile.json` untouched (used editor-injected scrap for the RunEnd test).

## M17 — Weapon visual pass (2026-08-31, awaiting sign-off)
User: "silahları daha iyi bir hale getir. orbiter'daki + ları plazma toplarına çevir. evolve
olunca farklı görünsünler." Downloaded **Kenney Particle Pack** (CC0) → `Art/Particles/PNG
(Transparent)/` (80 sprites).

- **`Editor/WeaponVfxBuilder.cs`** (`SpaceSurvivors/Build/M17 Weapon VFX`) — idempotent one-shot:
  1. `FixParticleImporters` — the 80 Kenney PNGs → Sprite / Single / PPU 512.
  2. `BakeOrb` — Kenney's circles are all rings/bubbles, so bake `Art/Sprites/Generated/
     WeaponOrb.png` (solid disc, full alpha to 55% r then soft edge) + `WeaponOrbGlow.png`.
  3. `CreateAdditiveMaterial` → `Art/Particles/WeaponGlow.mat` on **`Sprites/Default`**
     (NOT additive — a custom premultiplied-additive shader + ACES tonemapping both washed the
     coloured cores to white; alpha-blend keeps the tint, Bloom does the glow).
  4. `CreateVolumeProfile` → `Assets/Settings/PostProcess/GameVolume.asset` with **Bloom**
     (threshold 0.55, intensity 0.7, scatter 0.5), no Tonemapping. `WireGameScene` adds a
     `Global Volume` GO + sets `Main Camera` `renderPostProcessing = true`.
  5. `StyleBaseProjectiles` — per weapon: core sprite (`WeaponOrb` for orbs, `trace_04/06`
     for bolts) + colour identity (Laser cyan / Missile orange / Plasma violet / Scatter amber
     / Rail blue-white / Orbiter teal / Mine red) + `TrailRenderer` (colour-gradient, width =
     rootScale*0.5) + `Combat/TrailReset` (clears the trail on pool respawn — else a recycled
     shot streaks across the screen) + `Combat/VfxSpinPulse` (pulse on orbs, spin on Orbiter).
     Root scale is retuned and the `CircleCollider2D` radius compensated so the hitbox is
     unchanged.
  6. `BuildEvolutionPrefabs` — **the base + evolved weapon shared ONE projectile prefab**; now
     `CopyAsset`s each base to `Prefabs/Projectiles/Evo_*` (fileIDs preserved so the ref
     resolves), restyles it distinctly, and repoints the evolved `WeaponData.projectilePrefab`.
     Prism = white tri-bolt, Cluster = red + `flame_03` glow, Nova = `magic_04` spinning star,
     Buckshot = bigger amber, VoidLance = deep-violet long trail, **EventHorizon = dark-violet
     orbs with a spinning `twirl_02` vortex glow**, DeepMine = brighter red.
  7. `BuildMuzzleFlash` — pooled `MuzzleFlash.prefab` (`muzzle_03`, `OneShotPulse` 0.08s),
     spawned in `WeaponController.FireWeapon` oriented to the shot.
  8. `StyleAuras` — new `WeaponData.auraTint` (data-driven ring colour); `AuraWeapon` reworked
     to use it + a slow spin + sine alpha pulse. `WeaponController._muzzleFlashPrefab` +
     `_auraRingMaterial` fields added.
- **Play-tested (Claude):** orbiter orbs are now glowing teal balls (no more "+"), scatter =
  orange comets, EventHorizon evo = purple black-hole vortex orbs (visibly different from base
  Orbiter). Bloom on. LevelUp / RunEnd / HUD-hide still fine. No console errors.
- **Heavy iteration** to land the look: additive→alpha-blend, dropped a custom shader and ACES
  tonemapping (both whitewashed colours), baked a real solid-disc orb sprite (Kenney has none).
  Fine-tuning (exact sizes / missile orange saturation / muzzle-flash visibility) is a taste
  call — deferred to the human playtest.
- **⚠ Real `profile.json` polluted during testing** (RunEndScreen auto-banks scrap on run end;
  I injected scrap + let runs end). User chose FULL RESET: profile.json wiped to a clean slate (wallet 0, all lifetime/run
  stats 0, no achievements, no map) and `score.best.*` PlayerPrefs deleted (volume prefs kept).
  **Lesson: always `ProfileService.SetStore` a scratch store before play-mode weapon tests.**

## M18 — Environment & Space Events (2026-08-31, awaiting sign-off)
User: "haritayı güzelleştir, etrafa başka şeyler koy, uzay eventleri olsun, map ekle."
Downloaded **SBS "Seamless Space Backgrounds"** (CC0, itch.io) → `Art/Sprites/Backgrounds/`
(32 seamless 1024² PNGs: blue/green/purple nebulas + starfields). The `.rar` was extracted
with `bsdtar` (macOS can't open RAR natively).

- **New arena props** (Obstacle prefabs, added to `EnvironmentDirector._props`, weighted, prewarmed):
  `Env_Wreck` (big `spaceStation_018`, indestructible cover), `Env_DebrisChunk` (`spaceParts_016`,
  ~22 HP, drifts, 6 scrap), `Env_Crystal` (`meteorGrey_med1` cyan-tinted, ~44 HP, 26 scrap),
  `Env_DriftMine` (neutral — arms then `Aoe.Splash` on any contact, player OR enemy),
  `Env_BonusPod` (rare, heal + scrap burst on player touch). `Obstacle.SetDrift(Vector2)` added.
  Drift mine visibility (user request): dark-red spiked body + wide red `WeaponOrbGlow` halo +
  bright pulsing `star_05` light with a hard on/off blink that speeds up + slow tumble.
- **Space events** — `Data/SpaceEventData` (SO: id/announce/weight/earliestTime/duration/prefab) +
  `Data/SpaceEventCatalogue` (`Resources/`). `Environment/EventDirector` on Systems: one event at a
  time, weighted roll of events past their `earliestTime`, cooldown 35–70s, first at 60s;
  `MapData.signatureEventId` gets a ×3 weight bias so maps feel distinct; `event Action<SpaceEventData>
  EventStarted`. `Environment/ISpaceEvent` + `SpaceEventContext` (player/pool/spawns/collector/camera/
  duration) + `Environment/SpaceEventBehaviour` base (lifetime + auto-release of `SpawnChild`ren +
  `OffscreenPoint` helper). Five events:
  - **MeteorShowerEvent** — directed stream of fast `Env_AsteroidSmall` from one edge for the duration.
  - **IonStormEvent** — `FX_IonOverlay` crackle follows the camera; every 1.8 s an EMP wave damages
    every enemy (layer 6) within 11 u of the player. Player-favourable.
  - **DerelictConvoyEvent** — a wreck + ring of scrap caches warps in near the player, guarded by 3
    Grunts, drifts back out. Caches/wreck are released on timeout.
  - **SolarFlareEvent** — 2.5 s warning strip, then a bright band (`FX_FlareBand`, OverlapBox tick)
    sweeps the whole arena burning player + enemies. One sweep.
  - **WormholeEvent** — `FX_Wormhole` (spinning `twirl_02`) opens; fly in → blink 16 u + scrap burst,
    2 s cooldown.
  - **`UI/EventBanner`** on `HudCanvas` — fades in the `announce` text for ~2.4 s. UI asmdef now refs
    `SpaceSurvivors.Environment`.
- **6 maps** (was 3) on the SBS nebulas: Milky Way (Blue_05) / Crimson Nebula (Purple_02, sig
  solar_flare) / Supernova (Purple_07, sig solar_flare) / **Ion Nebula** (Blue_02, sig ion_storm) /
  **Derelict Graveyard** (Green_05, sig derelict_convoy) / **Deep Void** (Starfield_03, sig wormhole).
  Backgrounds imported at PPU 9 / wrap Repeat / FullRect mesh / mipmap; `MapData.backdropTint` alpha
  ~0.2 so the nebula is mood, not midground noise. `StarfieldParallax._backdropParallax` bumped to
  0.022, `_backdropDensity` 0.55.
- **`Editor/EnvironmentEventsBuilder.cs`** (`SpaceSurvivors/Build/M18 Environment + Events`,
  idempotent) does all of the above + wires the scene.
- **asmdef:** `SpaceSurvivors.Environment` now refs `SpaceSurvivors.Enemies` (for `SpawnDirector`);
  `SpaceSurvivors.UI` refs `SpaceSurvivors.Environment` (for `EventBanner`). No cycles.
- **Play-tested (Claude):** builder runs clean (0 errors, "5 events"); MapSelect shows the real
  nebulas; meteor shower streams; derelict convoy warps in with banner "◈ A derelict convoy drifts
  near" + caches + guards; new props stream in the field. Deep visual tuning (nebula brightness /
  prop density / event pacing & balance) is a taste call — deferred to the human playtest.
- Known: `StarfieldParallax` still warns "Sprite Tiling … not Full Rect" for the generated star tile
  (pre-existing, cosmetic). IonStorm has no enemy-slow (enemies have no StatSheet) — it's pure
  damage. Event balance numbers are first-pass.

## M18 follow-up — maps made visually distinct (committed `83a310a` 2026-08-31)
User: "maplerde sanki çok değişiklik olmamış gibi." Nebula alpha 0.2 was too faint — all 6 maps
read the same. Fixes in `EnvironmentEventsBuilder.BuildMaps`: `backdropTint` alpha 0.2 → ~0.55
(0.32 for Deep Void), strongly-hued dark `cameraBackground` per map, `starfieldTint` dimmed +
hue-matched to each nebula, background PPU 9 → 7, some nebula picks swapped. 6 maps now clearly
distinct (Crimson = red, Ion = teal, etc.).

## M19 — Enemy obstacle avoidance + multishot fan fix (2026-09-01, committed — awaiting sign-off)
Two changes, one commit.

**Enemy obstacle avoidance.** Chasers used to grind into asteroids/wrecks on a straight line to
the player. New `Enemies/ObstacleAvoidance.cs` — an `IVelocityModifier` (same pattern as
`SeparationSteering`, auto-collected by `EnemyBrain`):
- one forward `Physics2D.CircleCast` (ContactFilter2D → Obstacle layer 11) probes the path
- on a hit: cancels the velocity component driving into the surface, adds a slide-along-tangent +
  small push-off force, scaled by proximity (`1 - dist/lookAhead`)
- head-on tie-break: a stable per-instance random `_sideBias` picks which way to round it
- one cast per enemy per FixedUpdate — no worse than separation's existing OverlapCircle
- `Editor/EnemyAvoidanceBuilder.cs` (`SpaceSurvivors/Build/M19 Enemy obstacle avoidance`,
  idempotent) adds + tunes it on the 6 non-boss prefabs (Grunt/Shooter/Charger/Splitter/
  SplitterMite/Brute); `_probeRadius` computed from each prefab's own `CircleCollider2D` × scale
  + 0.14 clearance; per-archetype `_lookAhead` (1.6–2.6) and `_strength` (1.1–1.5). MiniBoss /
  FinalBoss deliberately excluded — they plough through.
- Play-tested (Claude): obstacle 2 u dead ahead → enemy velocity deviates 76.7°, inward-dot
  1.00 → 0.23 (rounds the rock); no obstacle → 0° deviation, full speed (no false positives).
  `strength` feel is a taste call for the human playtest.

**Multishot fan fix.** User: "multishot 2 iken lazer ortadan ikiye ayrılıp düşmanın yanından
geçiyor." `WeaponController.FireWeapon` built a symmetric fan (`start = -spread/2`,
`step = spread/(count-1)`) — an even count left the middle empty, so 2 shots straddled the
target and whiffed. Now **centre-out**: shot 0 goes dead on the aim line, the rest peel off in
alternating pairs (`i → tier 0, +1, -1, +2, -2 …`). `divisor = (count odd) ? count-1 : count` →
odd counts keep the weapon's full designed `spreadAngle` (Prism 5 / Scatter 5 / Buckshot 7 /
Cluster 3 unchanged); even counts pack a little tighter with a guaranteed on-target shot.
Laser (spread 18°): count 2 → offsets `[0°, +9°]`. Verified in play mode.

**Nebula backdrop fix (same M19 commit).** User: "arkaplanlar sadece belli bölgelerde çalışıyor
ve çok piksel piksel." Two bugs in `StarfieldParallax.SetBackdrop`: (1) `size` was assigned
*before* `sprite` — in Tiled draw mode a fresh sprite resets `size` to the sprite's native
dimensions, so the quad was one tile; (2) the quad was `viewW*coverage` (~54u) but the parallax
wrap slid it by up to one full tile (~266u at the old PPU), pushing it clean off-screen → bare
camera background across most of the map. Now: sprite assigned first, quad sized
`(view*coverage + 2*tileWorld)` so it overhangs by a tile on every side. Pixelation: PPU 7 → 32
(the camera saw ~12% of the 1024² tile blown up 8×), `textureCompression` Normal → CompressedHQ
(gradient blocking), Bilinear + mipmaps + maxTextureSize 2048; `_backdropDensity` 0.55 → 1.
`EnvironmentEventsBuilder.ImportBackgrounds` carries the import settings. Verified in play at a
far-from-origin position: nebula fills the frame, soft not blocky.

## Feature backlog captured (2026-08-28)
User dumped 11 ideas before starting M10. Full list + milestone mapping + rationale is in
`Project_Goals.md §8`. Milestone table there re-planned: M10 juice/UX, M11 combat content,
M12 enemy variety, **M13 persistent save/wallet core**, M14 meta screens (shop/ships/upgrades/
achievements — all depend on M13), M15 environment/maps, M16 balance. Order not locked; awaiting
user's call on sequencing.

**Backend + DB planned (post-M13).** User intends cloud save / likely online leaderboards later.
Design rule for M13: profile = plain serializable DTO (`PlayerProfile` + `int schemaVersion`),
all access via an `IProfileStore` interface, M13 ships `LocalJsonProfileStore`, backend is later
a drop-in HTTP implementation — not a rewrite. Score/currency mutations funnelled through one
service for a future server-authoritative seam.
Planned stack: **Java 25** backend (IntelliJ), **PostgreSQL** (DBeaver), **Firebase** for
auth/cloud-save entry, **Azure** hosting. Client-side rule: use **Newtonsoft JSON**
(`com.unity.nuget.newtonsoft-json`), NOT `JsonUtility`, for the profile DTO so it round-trips
with a Jackson backend. Server is a separate track. Detail in `Project_Goals.md §8`.

## Architecture pass — Assembly Definitions (2026-08-27, before M8)
Split the ~45 scripts into 9 compiler-enforced assemblies. Dependency direction is now
enforced by the compiler → cannot become spaghetti. Layout + deps in `AI_Guidelines.md §6`:
`Stats`/`Core` (no deps) < `Data` < `Combat`/`Player` < `Enemies` < `Progression` < `Game` < `UI`.
- `RunController.cs` moved `Core/` → `Game/` (namespace `SpaceSurvivors.Core` → `SpaceSurvivors.Game`);
  updated `using` in GameOverScreen + LevelUpScreen. `.meta` moved with it → scene refs intact.
- `Unity.InputSystem` added to `SpaceSurvivors.Player` + `SpaceSurvivors.UI` asmdef references
  (not auto-referenced under an explicit asmdef).
- Verified: all 9 assemblies compile, scene has 0 missing scripts, game runs (clock 26s, enemies spawning).
- CONSEQUENCE for RunCommand: game types are no longer in `Assembly-CSharp` — use
  `AppDomain...GetType(name)` or `"...,  SpaceSurvivors.<Area>"`.
- AI_Guidelines.md gained §6 (assembly boundaries), §7 (no new statics, bootstrap-UI rule),
  §9 (continuity / resume procedure / RunCommand gotchas).

## M8 — Main Menu + 2 Game Modes (built 2026-08-28)
**Two scenes:** `MainMenu.unity` (build 0) + `Game.unity` (build 1, was `Assets/Scenes/SampleScene`).
`SceneManager.LoadScene` between them; `GameSession.SelectedMode` (static, reset on play) carries the choice.

**Data**
- `Data/GameModeData.cs` — SO: `displayName`, `description`, `DifficultyConfig difficulty`, `bool endless`.
- `Data/GameSession.cs` — static `SelectedMode`; `IsEndless => SelectedMode == null || endless`.
- `Config/Mode_Campaign.asset` (endless=false, → `CampaignDifficulty.asset`),
  `Config/Mode_Infinite.asset` (endless=true, → `DifficultyConfig.asset`).
- `CampaignDifficulty.asset` — bossSchedule = MiniBoss @ 60s (lead 4), FinalBoss @ 150s (lead 5);
  curves peak ~160s, healthMult 1→2.4, speedMult 1→1.3, maxAlive 200.
- `DifficultyConfig.asset` (= Infinite) — bossSchedule 180/360/540/720s (last = FinalBoss).
- `Enemies/FinalBoss.asset` — EnemyData, MiniBoss prefab + BossDeath VFX, 1800 HP, weight 0 /
  earliestSpawnTime 99999 (schedule-only, never random-spawns).

**Victory / defeat**
- `Enemies/SpawnDirector.cs` — added `BossesDefeated`, `AllScheduledBossesDefeated`
  (`_bossEntriesSpawned >= schedule.Count && _bossesAlive == 0`). `Awake` pulls `_config` from
  `GameSession.SelectedMode.difficulty` when set.
- `Game/RunController.cs` — `Update()` fires `EndRun(won:true)` when `AllScheduledBossesDefeated`
  (only if `!GameSession.IsEndless`). Player death → `EndRun(won:false)`. `EndRun` stops the clock,
  disables `_disableOnEnd` behaviours, ramps `Time.timeScale`→0 over 0.6s, fires `RunEnded(won, secs)`.
- `UI/RunEndScreen.cs` — real prefab-style component (no self-build). On `RunEnded`: swaps header
  sprite (win/lose), shows `m:ss`, toggles `_root`. Replay = reload scene; Menu = load `MainMenu`.
  Replaces the deleted `GameOverScreen.cs`.
- `UI/MainMenuScreen.cs` — `ModeButton[] {Button, GameModeData}`; click → `GameSession.SelectedMode = mode`
  → load `Game`. Hover → description label. Quit button.

**Scene UI built by `Editor/M8UiBuilder.cs`** (menu: `SpaceSurvivors/Build/…`). Editor-only, uses the
CraftPix kit (`You_Win/Window|Header|Score|Replay_BTN`, `Buttons/BTNs/Menu_BTN`, `Main_Menu/BG|Exit_BTN`,
`Shop/Prise_BTN_Table` slab for mode buttons). 9-slice borders set on `Prise_BTN_Table` + `Window`.
Lives in `Assembly-CSharp-Editor` because the RunCommand dynamic assembly can't reference `UnityEngine.UI`.

**Play-test (Claude, all 3 paths):**
- Infinite → defeat: menu→Infinite→GameSession set→play→death→RunEndScreen (lose header, "0:20"), timeScale 0, Menu returns.
- Campaign → defeat: same, non-endless config loads.
- Campaign → victory: killed both scheduled bosses → `RunOver=True Won=True`, RunEndCanvas+Dim active,
  win header sprite set, ScoreValue "0:24", timeScale 0. (Tested with temp 5s/12s schedule — REVERTED to 60/150.)
- Layout checked numerically (no overlaps in either scene). Visual screenshot NOT done —
  Scene View capture doesn't render ScreenSpaceOverlay UI; deferred to M9 polish pass.

**Deferred to M9:** "STAGE 1/3" HUD indicator, audio, parallax starfield, settings, persistent high score,
run-stats screen, TMP conversion, converting the rest of the bootstrap-code UI to prefabs.

## Open Decisions / TODO
- [ ] Confirm Input System package is installed (Package Manager) before M1 wiring.
- [ ] Decide pool implementation: custom `PoolManager` vs Unity `ObjectPool<T>`.
- [ ] Set up Layer collision matrix (Player / Enemy / PlayerProjectile / Pickup).
- [ ] Add `.gitignore` (Unity template) and init Git repo.

## Key Facts (so future sessions don't re-derive them)
- Balance: first mini-boss at exactly 180s. Player power ~doubles every 2 levels.
  Enemy spawn rate + HP scale continuously with time survived.
- Architecture: component-based, SO-driven data, object pooling mandatory, interfaces
  `IDamageable` / `ICollectible` / `IPoolable` / `IMoveStrategy`.
