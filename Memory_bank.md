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
| 2026-08-27 | M7 — difficulty director + mini-boss | ✅ APPROVED 2026-08-28 (with M8). |
| 2026-08-28 | M8 — main menu + 2 game modes | ✅ APPROVED 2026-08-28. All 3 paths play-tested. |
| 2026-08-28 | M9 — open arena + camera follow | Built + Claude play-tested (follow, ring-spawn, far-cull, parallax). Awaiting user sign-off. |

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
Wave 3 (audio) deferred by the user until an asset pack is sourced.

### Wave 3 — audio (pending, needs asset pack)
Blocked on a CC0 audio pack (like the UI kit was). Kenney Sci-Fi Sounds / Space Kit suggested.
Then build `AudioDirector` + `SfxEvent` hooks (shoot, hit, enemy death, level-up, boss warning,
pickup, player hurt, win/lose) reading `SettingsService` volumes.

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
