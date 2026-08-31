using SpaceSurvivors.Core;
using SpaceSurvivors.Enemies;
using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>Everything a running space event needs from the scene, passed in once at start.</summary>
    public readonly struct SpaceEventContext
    {
        public readonly Transform Player;
        public readonly PoolManager Pool;
        public readonly SpawnDirector Spawns;
        public readonly ScrapCollector Collector;
        public readonly Camera Camera;
        public readonly float Duration;

        public SpaceEventContext(Transform player, PoolManager pool, SpawnDirector spawns,
            ScrapCollector collector, Camera camera, float duration)
        {
            Player = player;
            Pool = pool;
            Spawns = spawns;
            Collector = collector;
            Camera = camera;
            Duration = duration;
        }
    }

    /// <summary>
    /// A single arena event (M18). The <c>EventDirector</c> spawns the prefab, calls
    /// <see cref="Begin"/> once, and forgets about it — the event runs for
    /// <see cref="SpaceEventContext.Duration"/> seconds, cleans up whatever it spawned, and
    /// returns itself to the pool. One job per event component (AI_Guidelines §1).
    /// </summary>
    public interface ISpaceEvent
    {
        void Begin(in SpaceEventContext context);
    }
}
