# SpaceSurvivors — AI / Coding Guidelines

These are **hard rules**. If a request would violate one of them, stop and propose a
compliant alternative before writing code.

## 1. Full Modularity (Component-Based Design)
- **No monolithic classes.** There is no `Player.cs` that does movement + health +
  shooting + input. Each responsibility is its own `MonoBehaviour` component:
  - `PlayerMovement` — reads input, moves the body.
  - `HealthComponent` — HP, damage, death (reused by player *and* enemies).
  - `WeaponController` — owns weapons, drives cooldowns.
  - `ScrapCollector`, `LevelSystem`, etc. — one job each.
- **Loose coupling.** Components talk through:
  - C# `event` / `UnityEvent` / a lightweight event bus — **not** direct `FindObjectOfType`
    chains or static singletons everywhere.
  - Interfaces (see §2), never concrete cross-references where an interface fits.
- **Composition over inheritance.** Prefer adding components over deep class hierarchies.
  A shared base is allowed only for genuine "is-a" (e.g. `ProjectileBase`).
- One public responsibility per file; file name == class name.

## 2. Interfaces
Define and use these contracts:
- `IDamageable` — `void TakeDamage(DamageInfo info); bool IsAlive { get; }`
- `ICollectible` — `void Collect(GameObject collector);`
- `IPoolable` — `void OnSpawn(); void OnDespawn();` (pool lifecycle hooks)
- `IMoveStrategy` (enemy AI) — `Vector2 GetMove(Vector2 self, Vector2 target, float dt);`
Damage receivers are found via `GetComponent<IDamageable>()`, never by concrete type.

## 3. Data Management — ScriptableObject as Database
- **Zero magic numbers in code.** Every tunable lives in a `ScriptableObject` asset:
  - `WeaponData` (damage, cooldown, projectile speed, count, pierce, scale curve…)
  - `EnemyData` (HP, speed, contact damage, scrap value, move strategy…)
  - `UpgradeData` (type, magnitude, rarity, prerequisites, icon…)
  - `DifficultyConfig` (spawn-rate curve, HP-multiplier curve, boss schedule…)
  - `PlayerConfig` (base move speed, accel, drag, pickup radius…)
- Code holds *references* to SO assets and reads values at runtime.
- Runtime-mutable state (current HP, current cooldown timer) lives on the component,
  seeded from the SO — never mutate the asset at runtime.
- Use `[CreateAssetMenu]` on every data SO, grouped under a `SpaceSurvivors/` menu path.

## 4. Performance
- **Never `Instantiate` / `Destroy` during gameplay** (i.e. not in `Update`, not on spawn,
  not on kill). Everything transient goes through an **Object Pool**:
  enemies, every projectile type, XP/Scrap drops, VFX, damage numbers.
- Central `PoolManager` (or per-type `Pool<T>`); objects implement `IPoolable`.
- Cache component references in `Awake`; no `GetComponent` in `Update`.
- No LINQ / allocations in hot per-frame paths; reuse buffers, use non-alloc physics
  queries (`Physics2D.OverlapCircleNonAlloc`).
- Prefer a single manager `Update` that ticks many entities over hundreds of individual
  `Update` methods when entity counts get large (data-oriented where it matters).
- Physics: 2D colliders + layers configured so enemies don't collide-solve against
  each other unnecessarily. Set up the layer collision matrix early.

## 5. Input
- Use the **Unity Input System** package with an `InputActions` asset.
- Movement components read a cached `Vector2` from an input provider, so input can be
  rebound, replaced with AI, or recorded for tests without touching movement code.

## 6. Assembly boundaries (HARD — the compiler enforces the architecture)
Every script area is its own assembly (`Assets/_Project/Scripts/<Area>/SpaceSurvivors.<Area>.asmdef`).
Dependencies flow **downward only** — this is what stops the codebase becoming spaghetti:

```
Stats        (no deps)
Core         (no deps)
Data         -> Stats
Combat       -> Core, Data, Stats
Player       -> Core, Data, Stats
Enemies      -> Core, Data, Combat
Progression  -> Core, Data, Stats, Combat, Enemies, Player
Game         -> Core, Combat                (run/mode flow: RunController, later GameModeData)
UI           -> everything above            (top of the stack, nothing depends on UI)
```

- A new script goes in the area that owns its responsibility. If it needs a type from a
  **higher** assembly, that's a design smell — invert it with an interface in `Core`/`Data`,
  or raise an event the higher layer subscribes to. Never add an upward reference.
- `Core` and `Stats` must stay dependency-free. Interfaces and plain data structs live here.
- New cross-area contract → interface in `Core` (`IDamageable`, `IPoolable`, `ICollectible`,
  `IDamageInterceptor`, …). Steering/strategy interfaces live with their consumer's area.
- If a `.asmdef` reference is missing you get a compile error, not a silent tangle — good.
  Add the reference **only** if it points downward.

## 7. Style & Conventions
- Namespaces: `SpaceSurvivors.<Area>` — must match the folder / assembly.
- `PascalCase` types/methods, `camelCase` locals, `_camelCase` private fields,
  `[SerializeField] private` over `public` fields.
- XML `///` summaries on every public type and non-obvious method.
- Keep `MonoBehaviour` thin; push logic into plain C# classes that are unit-testable.
- **No new static singletons.** `BossMarker.Active` is the only allowed static and it's a
  weak pointer reset on load. Wire dependencies through the Inspector or resolve on `Awake`.
- UI screens built in C# (`UiBuilder`, `*Screen`, `*Hud`) are **bootstrap only** — replace
  with prefabs during polish. New gameplay-facing UI should already be a prefab.

## 8. Testing & Safety
- Pure logic (damage math, XP curves, difficulty curves) goes in plain classes with
  EditMode tests.
- Every new system gets a short entry in `Memory_bank.md` when it lands.
- Commit after each milestone; never commit `Library/`, `Temp/`, `Logs/`, `obj/`.

## 9. Continuity — surviving a fresh conversation
The three root docs are the contract. Any new session must be able to resume from them:
- `Project_Goals.md` — vision, milestone list, mode designs.
- `AI_Guidelines.md` — this file (the rules).
- `Memory_bank.md` — detailed running log: every script and what it does, assets,
  scene wiring, decisions, the "next milestone" section, and known gaps.
- Update `Memory_bank.md` **when a milestone lands** and **when a direction decision is
  made** (not just code — the "why" and "what next" too).
- Resume phrase for the user: *"Continue SpaceSurvivors — read Memory_bank.md and
  Project_Goals.md first."*

### Working with the Unity MCP `RunCommand` tool
- Game types now live in `SpaceSurvivors.*` assemblies, **not** `Assembly-CSharp`. Resolve
  them with `AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name))` or an
  assembly-qualified name (`"SpaceSurvivors.Core.PoolManager, SpaceSurvivors.Core"`), never
  `", Assembly-CSharp"`.
- `RunCommand` scripts can't use `System.Reflection` / bare `Time` / `UnityEngine.UI`
  (dynamic-assembly limits + namespace clash with its wrapper). Fully-qualify or avoid.
- Scene edits fail during Play mode; console logs returned by the tool can be stale —
  verify compile via `EditorUtility.scriptCompilationFailed` + type resolution.
