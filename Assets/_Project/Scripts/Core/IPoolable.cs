namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Lifecycle hooks for objects that live in an <see cref="Pool"/>. Implement this on
    /// projectiles, enemies, XP drops, VFX — anything that is recycled instead of
    /// Instantiated/Destroyed at runtime (AI_Guidelines §4).
    ///
    /// <see cref="OnSpawned"/> runs after the object is activated and positioned — use it
    /// to reset state (velocity, timers, trail renderers). <see cref="OnDespawned"/> runs
    /// just before it is deactivated — use it to stop coroutines, clear references, emit a
    /// death VFX request, etc.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
