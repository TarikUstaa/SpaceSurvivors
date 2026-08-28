using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Drives one <see cref="WeaponKind.Orbital"/> weapon: keeps N pooled <see cref="OrbHit"/>
    /// orbs evenly spaced on a circle around the ship and spins them. Orb count follows the
    /// <see cref="StatId.ProjectileCount"/> stat, damage follows <see cref="StatId.Damage"/> —
    /// same pipeline as the projectile weapons (AI_Guidelines §3). Created and configured by
    /// <see cref="WeaponController"/>; it never fires, so it lives outside the cooldown loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class OrbitalWeapon : MonoBehaviour
    {
        private WeaponData _data;
        private PoolManager _pool;
        private StatSheet _stats;
        private Transform _center;
        private GameObject _owner;

        private readonly List<PoolHandle> _orbs = new();
        private float _angle;

        public WeaponData Data => _data;

        public void Configure(WeaponData data, PoolManager pool, StatSheet stats, Transform center, GameObject owner)
        {
            _data = data;
            _pool = pool;
            _stats = stats;
            _center = center;
            _owner = owner;
        }

        private void OnDisable() => Clear();

        public void Clear()
        {
            foreach (var h in _orbs)
                if (h != null) h.Despawn();
            _orbs.Clear();
        }

        private void Update()
        {
            if (_data == null || _pool == null || _center == null) return;

            int want = _stats != null
                ? Mathf.RoundToInt(_stats.Modify(StatId.ProjectileCount, _data.projectilesPerShot))
                : _data.projectilesPerShot;
            want = Mathf.Clamp(want, 1, 12);

            SyncCount(want);

            float damage = _stats != null ? _stats.Modify(StatId.Damage, _data.damage) : _data.damage;
            _angle += _data.orbitDegreesPerSecond * Time.deltaTime;

            for (int i = 0; i < _orbs.Count; i++)
            {
                var h = _orbs[i];
                if (h == null) continue;
                float a = (_angle + i * 360f / _orbs.Count) * Mathf.Deg2Rad;
                Vector3 offset = new(Mathf.Cos(a) * _data.orbitRadius, Mathf.Sin(a) * _data.orbitRadius, 0f);
                h.transform.position = _center.position + offset;

                if (h.TryGetComponent(out OrbHit orb))
                    orb.Configure(damage, _data.orbitHitInterval, _owner);
            }
        }

        private void SyncCount(int want)
        {
            while (_orbs.Count > want)
            {
                var h = _orbs[^1];
                _orbs.RemoveAt(_orbs.Count - 1);
                if (h != null) h.Despawn();
            }
            while (_orbs.Count < want)
            {
                var go = _pool.Spawn(_data.projectilePrefab, _center.position, Quaternion.identity);
                if (go == null) break;
                _orbs.Add(go.GetComponent<PoolHandle>());
            }
        }
    }
}
