using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Final per-shot numbers, after the <see cref="StatSheet"/> has done its work.
    /// Passed to <see cref="Projectile.Launch"/> so the projectile never touches stats.
    /// </summary>
    public readonly struct ShotParams
    {
        public readonly float Damage;
        public readonly float Speed;
        public readonly float Lifetime;
        public readonly int Pierce;

        public ShotParams(float damage, float speed, float lifetime, int pierce)
        {
            Damage = damage;
            Speed = speed;
            Lifetime = lifetime;
            Pierce = pierce;
        }
    }

    /// <summary>
    /// Owns the ship's weapons and drives their cooldowns. One controller, many
    /// <see cref="WeaponData"/> slots — granting a weapon later (from an upgrade) is just
    /// <see cref="AddWeapon"/>, evolving one is <see cref="EvolveWeapon"/> (AI_Guidelines §1).
    ///
    /// Fires through the <see cref="PoolManager"/> (never Instantiate, §4), aims through an
    /// <see cref="IAimStrategy"/> sibling, and pulls every number from the weapon asset run
    /// through the player's <see cref="StatSheet"/> — zero balance constants here (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponController : MonoBehaviour
    {
        private sealed class Slot
        {
            public WeaponData Data;
            public float CooldownLeft;
        }

        [Header("Dependencies")]
        [SerializeField] private PoolManager _pool;
        [Tooltip("Optional. Empty = auto-resolve an IAimStrategy on this GameObject.")]
        [SerializeField] private MonoBehaviour _aimStrategyBehaviour;
        [Tooltip("Optional. Empty = auto-resolve a StatSheet on this GameObject.")]
        [SerializeField] private StatSheet _stats;

        [Header("Loadout")]
        [SerializeField] private List<WeaponData> _startingWeapons = new();

        [Tooltip("Hard cap on how many weapons the ship can hold at once. Evolutions swap in " +
                 "place and never count against this. Keeps late-run builds about choosing a " +
                 "loadout instead of collecting every gun.")]
        [SerializeField, Min(1)] private int _maxWeapons = 6;

        [Tooltip("Where projectiles spawn. Empty = this transform.")]
        [SerializeField] private Transform _muzzle;

        [Tooltip("Faint ring sprite for Aura weapons (optional).")]
        [SerializeField] private Sprite _auraRingSprite;

        [Tooltip("Optional additive material for the Aura ring glow.")]
        [SerializeField] private Material _auraRingMaterial;

        [Tooltip("Optional pooled one-shot flash spawned at the muzzle each time a projectile " +
                 "weapon fires. Oriented to the shot direction.")]
        [SerializeField] private GameObject _muzzleFlashPrefab;

        private readonly List<Slot> _slots = new();
        private IAimStrategy _aim;
        private readonly List<GameObject> _specialRigs = new();

        /// <summary>Raised each time a weapon actually fires (for audio / VFX). Carries the weapon.</summary>
        public event System.Action<WeaponData> WeaponFired;

        /// <summary>Weapons currently equipped (read-only view for the evolution UI).</summary>
        public IReadOnlyList<WeaponData> Weapons
        {
            get
            {
                _weaponView.Clear();
                foreach (var s in _slots) _weaponView.Add(s.Data);
                return _weaponView;
            }
        }
        private readonly List<WeaponData> _weaponView = new();

        private void Awake()
        {
            _aim = _aimStrategyBehaviour as IAimStrategy ?? GetComponent<IAimStrategy>();
            if (_stats == null) _stats = GetComponent<StatSheet>();
            if (_muzzle == null) _muzzle = transform;

            if (_pool == null)
                Debug.LogError($"{nameof(WeaponController)} on '{name}' has no PoolManager assigned.", this);
            if (_aim == null)
                Debug.LogError($"{nameof(WeaponController)} on '{name}' found no IAimStrategy.", this);

            foreach (var w in _startingWeapons)
                AddWeapon(w);
        }

        /// <summary>True once the ship is carrying its maximum number of weapons.</summary>
        public bool IsFull => _slots.Count >= _maxWeapons;

        /// <summary>Grant a weapon at runtime. Ignores nulls / duplicates / a full loadout.</summary>
        public void AddWeapon(WeaponData data)
        {
            if (data == null || IsFull || _slots.Exists(s => s.Data == data)) return;
            _slots.Add(new Slot { Data = data, CooldownLeft = 0f });
            SyncSpecialWeapons();
        }

        public bool HasWeapon(WeaponData data) => _slots.Exists(s => s.Data == data);

        /// <summary>Swap an equipped weapon for its evolved form, keeping the cooldown state.</summary>
        public void EvolveWeapon(WeaponData from, WeaponData to)
        {
            if (from == null || to == null) return;
            var slot = _slots.Find(s => s.Data == from);
            if (slot != null) slot.Data = to;
            SyncSpecialWeapons();
        }

        /// <summary>
        /// Rebuild the child rigs for every non-projectile weapon (Orbital / Trail / Aura).
        /// Cheap — a handful of rigs — so a full teardown+rebuild on any loadout change keeps
        /// the evolution path simple.
        /// </summary>
        private void SyncSpecialWeapons()
        {
            if (_pool == null) return;

            foreach (var rig in _specialRigs)
            {
                if (rig == null) continue;
                if (rig.TryGetComponent(out OrbitalWeapon ow)) ow.Clear();
                Destroy(rig);
            }
            _specialRigs.Clear();

            foreach (var slot in _slots)
            {
                var d = slot.Data;
                if (d == null || d.kind == WeaponKind.Projectile) continue;

                var go = new GameObject($"{d.kind}_{d.displayName}");
                go.transform.SetParent(transform, false);

                switch (d.kind)
                {
                    case WeaponKind.Orbital:
                        go.AddComponent<OrbitalWeapon>().Configure(d, _pool, _stats, transform, gameObject);
                        break;
                    case WeaponKind.Trail:
                        go.AddComponent<MineLayer>().Configure(d, _pool, _stats, transform, gameObject);
                        break;
                    case WeaponKind.Aura:
                        go.AddComponent<AuraWeapon>().Configure(d, _stats, gameObject, _auraRingSprite, _auraRingMaterial);
                        break;
                }
                _specialRigs.Add(go);
            }
        }

        private void Update()
        {
            if (_pool == null || _aim == null) return;

            float dt = Time.deltaTime;
            Vector2 origin = _muzzle.position;
            float fireRate = _stats != null ? Mathf.Max(0.05f, _stats.Modify(StatId.FireRate, 1f)) : 1f;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Data.kind != WeaponKind.Projectile) continue; // driven by its own rig

                slot.CooldownLeft -= dt;
                if (slot.CooldownLeft > 0f) continue;

                if (!_aim.TryGetAimDirection(origin, slot.Data.aimRange, out Vector2 dir))
                    continue; // No target and no fallback — hold fire, keep cooldown ready.

                FireWeapon(slot.Data, origin, dir);
                WeaponFired?.Invoke(slot.Data);
                slot.CooldownLeft = slot.Data.cooldown / fireRate;
            }
        }

        private void FireWeapon(WeaponData data, Vector2 origin, Vector2 baseDir)
        {
            ShotParams shot = ResolveShot(data);

            int extra = _stats != null
                ? Mathf.RoundToInt(_stats.Modify(StatId.ProjectileCount, data.projectilesPerShot)) - data.projectilesPerShot
                : 0;
            int count = Mathf.Max(1, data.projectilesPerShot + Mathf.Max(0, extra));

            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            if (data.multishotShape == MultishotShape.Stream)
            {
                if (_muzzleFlashPrefab != null)
                    _pool.Spawn(_muzzleFlashPrefab, origin, Quaternion.Euler(0f, 0f, baseAngle));

                // All dead straight, each spawned a little further along the aim line so the
                // volley reads as a tight back-to-back burst instead of one fat blob.
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = origin + baseDir * (i * data.streamGap);
                    GameObject g = _pool.Spawn(data.projectilePrefab, pos, Quaternion.identity);
                    if (g != null && g.TryGetComponent(out Projectile p))
                        p.Launch(baseDir, data, shot, _pool, gameObject);
                }
                return;
            }

            // Fan from the centre outward: shot 0 goes dead on the aim line, the rest
            // peel off in alternating pairs around it (i -> tier 0, +1, -1, +2, -2, …).
            // A symmetric fan left the middle empty on an even count, so an even
            // multishot straddled the target and whiffed — this keeps one shot on
            // target for any projectile count. Odd counts still cover the weapon's
            // full designed spread; even counts pack a little tighter (no centre
            // slot to anchor the designed width).
            float divisor = count <= 1 ? 1f : (count & 1) == 1 ? count - 1 : count;
            float spacing = count > 1 ? data.spreadAngle / divisor : 0f;

            if (_muzzleFlashPrefab != null)
                _pool.Spawn(_muzzleFlashPrefab, origin, Quaternion.Euler(0f, 0f, baseAngle));

            for (int i = 0; i < count; i++)
            {
                int tier = (i + 1) / 2;
                float sign = (i & 1) == 1 ? 1f : -1f;
                float angle = baseAngle + sign * tier * spacing;
                Vector2 dir = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                GameObject go = _pool.Spawn(data.projectilePrefab, origin, Quaternion.identity);
                if (go != null && go.TryGetComponent(out Projectile projectile))
                    projectile.Launch(dir, data, shot, _pool, gameObject);
            }
        }

        private ShotParams ResolveShot(WeaponData data)
        {
            if (_stats == null)
                return new ShotParams(data.damage, data.projectileSpeed, data.projectileLifetime, data.pierce);

            return new ShotParams(
                _stats.Modify(StatId.Damage, data.damage),
                _stats.Modify(StatId.ProjectileSpeed, data.projectileSpeed),
                _stats.Modify(StatId.ProjectileLifetime, data.projectileLifetime),
                data.pierce + Mathf.RoundToInt(_stats.Modify(StatId.ProjectilePierce, 0f)));
        }
    }
}
