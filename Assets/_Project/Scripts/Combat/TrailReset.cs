using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Clears the <see cref="TrailRenderer"/> the moment a pooled object is (re)spawned, so a
    /// recycled projectile never draws a streak from its previous position to its new one
    /// (AI_Guidelines §4 — pooled objects reset fully on spawn). Cosmetic-only sibling.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    [DisallowMultipleComponent]
    public class TrailReset : MonoBehaviour, IPoolable
    {
        private TrailRenderer _trail;

        private void Awake() => _trail = GetComponent<TrailRenderer>();

        public void OnSpawned()
        {
            if (_trail == null) _trail = GetComponent<TrailRenderer>();
            // Wait one frame's worth: Clear() now, then the trail rebuilds from the fresh
            // position on the next LateUpdate.
            _trail.Clear();
        }

        public void OnDespawned() { }
    }
}
