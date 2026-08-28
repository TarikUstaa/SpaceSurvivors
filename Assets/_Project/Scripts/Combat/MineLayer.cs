using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Drives one <see cref="WeaponKind.Trail"/> weapon: every <see cref="WeaponData.cooldown"/>
    /// seconds (scaled by the FireRate stat) it drops <see cref="WeaponData.projectilesPerShot"/>
    /// mines just behind the ship. Damage / count / rate all run through the stat pipeline —
    /// same as the other weapons (§3). Created by <see cref="WeaponController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MineLayer : MonoBehaviour
    {
        private WeaponData _data;
        private PoolManager _pool;
        private StatSheet _stats;
        private Transform _center;
        private GameObject _owner;
        private Rigidbody2D _body;

        private float _cooldown;

        public WeaponData Data => _data;

        public void Configure(WeaponData data, PoolManager pool, StatSheet stats, Transform center, GameObject owner)
        {
            _data = data;
            _pool = pool;
            _stats = stats;
            _center = center;
            _owner = owner;
            _body = center != null ? center.GetComponent<Rigidbody2D>() : null;
            _cooldown = 0f;
        }

        private void Update()
        {
            if (_data == null || _pool == null || _center == null) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            float rate = _stats != null ? Mathf.Max(0.05f, _stats.Modify(StatId.FireRate, 1f)) : 1f;
            _cooldown = _data.cooldown / rate;

            int count = _stats != null
                ? Mathf.Max(1, Mathf.RoundToInt(_stats.Modify(StatId.ProjectileCount, _data.projectilesPerShot)))
                : _data.projectilesPerShot;
            float damage = _stats != null ? _stats.Modify(StatId.Damage, _data.damage) : _data.damage;

            Vector2 back = _body != null && _body.linearVelocity.sqrMagnitude > 0.1f
                ? -_body.linearVelocity.normalized
                : Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = _center.position + (Vector3)(back * 0.7f) + (Vector3)(Random.insideUnitCircle * 0.4f);
                var go = _pool.Spawn(_data.projectilePrefab, pos, Quaternion.identity);
                if (go != null && go.TryGetComponent(out Mine mine))
                    mine.Configure(damage, _data.explosionRadius, _data.projectileLifetime, _owner,
                                   _data.explosionVfxPrefab, _pool);
            }
        }
    }
}
