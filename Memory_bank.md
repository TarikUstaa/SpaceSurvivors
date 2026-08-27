# SpaceSurvivors — Memory Bank

> Working memory log. Update after every major milestone. Newest entry on top.

## Current State
**M1–M6 approved (+ many follow-ups). M7 (Difficulty Director + mini-boss) built + play-tested by Claude, awaiting user sign-off.**

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
| 2026-08-27 | M7 — difficulty director + mini-boss | Built + Claude play-tested (boss schedule, warning, health bar, arrival pop, death). Awaiting user sign-off. |

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

## Next milestone — M8: Main Menu + Game Modes (user-requested, 2026-08-27)
- Main menu scene + mode selection.
- **Campaign mode:** finite run, scripted stages with timed countdowns → mini-boss partway
  → **final boss as stage 3** → kill it to complete the mode (Victory screen). "STAGE 1/3" HUD.
- **Infinite mode:** endless survival, difficulty climbs forever, recurring bosses, score = time.
- Impl sketch: `GameModeData` SO (`DifficultyConfig` + `endless` bool + victory condition +
  stage schedule). Static `SelectedMode` set by menu → load Game scene. `RunController` checks
  victory condition (all scheduled bosses dead) → Victory. Extend `GameOverScreen` → Victory/Defeat
  + "Back to Menu".
- Need a Campaign `DifficultyConfig` (bosses ~1:00 mini + ~2:30 final) and an Infinite one
  (recurring bosses 2:00/4:00/6:00…, curves extended).
- Audio / parallax starfield / settings / persistent high score → pushed to M9.

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
