using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Drives one <see cref="WeaponKind.Chain"/> weapon. Every <see cref="WeaponData.cooldown"/>
    /// seconds (scaled by FireRate) it strikes the nearest enemy within
    /// <see cref="WeaponData.aimRange"/> of the ship, then leaps to the nearest enemy it has
    /// not already hit within <see cref="WeaponData.chainRange"/>, repeating up to
    /// <see cref="WeaponData.projectilesPerShot"/> targets. Each leap deals
    /// <see cref="WeaponData.chainFalloff"/>× the previous hit.
    ///
    /// <para>MultiShot feeds the target count through the ProjectileCount stat — the same
    /// pipeline every other weapon uses — so the upgrade that widens a shotgun instead
    /// lengthens the chain. Damage runs through the Damage stat (§3). Instant: no projectile,
    /// no pool. Created by <see cref="WeaponController"/>.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ChainLightning : MonoBehaviour
    {
        /// <summary>Hard ceiling so a MultiShot stack cannot turn one cast into a whole-screen sweep.</summary>
        private const int MaxTargets = 12;

        private static readonly Collider2D[] Buffer = new Collider2D[64];

        private WeaponData _data;
        private StatSheet _stats;
        private Transform _center;
        private GameObject _owner;

        private float _cooldown;

        private readonly List<IDamageable> _hit = new();
        private readonly List<Vector3> _nodes = new();
        private ChainArcView _arc;

        public WeaponData Data => _data;

        public void Configure(WeaponData data, StatSheet stats, Transform center, GameObject owner)
        {
            _data = data;
            _stats = stats;
            _center = center;
            _owner = owner;
            _cooldown = 0f;

            if (_arc == null)
            {
                var go = new GameObject("ChainArcView");
                go.transform.SetParent(transform, false);
                _arc = go.AddComponent<ChainArcView>();
            }
            _arc.SetColour(data.chainTint);
        }

        private void Update()
        {
            if (_data == null || _center == null) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            float rate = _stats != null ? Mathf.Max(0.05f, _stats.Modify(StatId.FireRate, 1f)) : 1f;

            IDamageable first = Nearest(_center.position, _data.aimRange, skipHit: false);
            if (first == null) return;   // nothing in range — hold, keep the cooldown ready

            _cooldown = _data.cooldown / rate;

            int targets = _stats != null
                ? Mathf.RoundToInt(_stats.Modify(StatId.ProjectileCount, _data.projectilesPerShot))
                : _data.projectilesPerShot;
            targets = Mathf.Clamp(targets, 1, MaxTargets);

            float damage = _stats != null ? _stats.Modify(StatId.Damage, _data.damage) : _data.damage;

            _hit.Clear();
            _nodes.Clear();
            _nodes.Add(_center.position);

            IDamageable current = first;
            Vector3 fromPos = _center.position;

            for (int i = 0; i < targets && current != null; i++)
            {
                Vector3 pos = Position(current);
                current.TakeDamage(new DamageInfo(damage, _owner, pos, (pos - fromPos).normalized));
                _hit.Add(current);
                _nodes.Add(pos);

                damage *= _data.chainFalloff;
                fromPos = pos;
                current = Nearest(pos, _data.chainRange, skipHit: true);
            }

            if (_nodes.Count >= 2) _arc.Flash(_nodes);
        }

        /// <summary>Closest live enemy to <paramref name="from"/> within <paramref name="radius"/>.</summary>
        private IDamageable Nearest(Vector3 from, float radius, bool skipHit)
        {
            int n = Physics2D.OverlapCircleNonAlloc(from, radius, Buffer);
            IDamageable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < n; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;
                if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == _owner) continue;

                var d = col.GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive) continue;
                if (skipHit && _hit.Contains(d)) continue;

                float sqr = ((Vector3)col.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = d;
                }
            }
            return best;
        }

        private static Vector3 Position(IDamageable d)
            => d is Component c ? c.transform.position : Vector3.zero;
    }
}
