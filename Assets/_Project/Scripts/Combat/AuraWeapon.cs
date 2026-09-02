using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Drives one <see cref="WeaponKind.Aura"/> weapon: a trigger ring of radius
    /// <see cref="WeaponData.orbitRadius"/> around the ship that, every
    /// <see cref="WeaponData.cooldown"/> seconds, damages every enemy currently inside it.
    /// No projectiles, no aiming. Damage runs through the Damage stat (§3). Created by
    /// <see cref="WeaponController"/>; it manages its own child collider + faint ring sprite.
    /// </summary>
    [DisallowMultipleComponent]
    public class AuraWeapon : MonoBehaviour
    {
        private WeaponData _data;
        private StatSheet _stats;
        private GameObject _owner;

        private CircleCollider2D _ring;
        private Transform _view;
        private SpriteRenderer _viewRenderer;
        private float _viewSpriteWorld = 1f;
        private float _pulsePhase;
        private readonly HashSet<Collider2D> _inside = new();
        private readonly List<Collider2D> _tick = new();
        private float _cooldown;

        public WeaponData Data => _data;

        public void Configure(WeaponData data, StatSheet stats, GameObject owner, Sprite ringSprite,
            Material ringMaterial = null)
        {
            _data = data;
            _stats = stats;
            _owner = owner;
            _cooldown = 0f;

            if (_ring == null)
            {
                _ring = gameObject.AddComponent<CircleCollider2D>();
                _ring.isTrigger = true;
            }
            _ring.radius = data.orbitRadius;

            if (ringSprite != null && _view == null)
            {
                var go = new GameObject("AuraView");
                go.transform.SetParent(transform, false);
                _viewRenderer = go.AddComponent<SpriteRenderer>();
                _viewRenderer.sprite = ringSprite;
                _viewRenderer.sortingOrder = -1;
                if (ringMaterial != null) _viewRenderer.sharedMaterial = ringMaterial;
                _view = go.transform;
                _viewSpriteWorld = Mathf.Max(0.01f, ringSprite.rect.width / ringSprite.pixelsPerUnit);
            }
            if (_viewRenderer != null) _viewRenderer.color = data.auraTint;
        }

        private void OnTriggerEnter2D(Collider2D other) { if (IsEnemy(other)) _inside.Add(other); }
        private void OnTriggerExit2D(Collider2D other) => _inside.Remove(other);

        private bool IsEnemy(Collider2D c)
        {
            if (c.attachedRigidbody != null && c.attachedRigidbody.gameObject == _owner) return false;
            return c.GetComponentInParent<IDamageable>() != null;
        }

        private void Update()
        {
            if (_data == null) return;

            float radius = _data.orbitRadius;
            if (_ring != null) _ring.radius = radius;
            if (_view != null)
            {
                // 1.13× so the sprite's bright rim (~0.87 of its own radius) lands on the
                // actual damage boundary rather than inside it.
                _view.localScale = Vector3.one * (radius * 2f / _viewSpriteWorld * 1.13f);
                _view.Rotate(0f, 0f, 24f * Time.deltaTime);
            }
            if (_viewRenderer != null)
            {
                _pulsePhase += Time.deltaTime * 3f;
                var c = _data.auraTint;
                c.a *= 0.85f + 0.15f * Mathf.Sin(_pulsePhase); // gentle breathe, never fades out
                _viewRenderer.color = c;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;
            _cooldown = _data.cooldown;

            float damage = _stats != null ? _stats.Modify(StatId.Damage, _data.damage) : _data.damage;

            // Snapshot first: TakeDamage can kill an enemy, whose despawn fires
            // OnTriggerExit2D and mutates _inside mid-iteration.
            _inside.RemoveWhere(c => c == null);
            _tick.Clear();
            _tick.AddRange(_inside);
            foreach (var c in _tick)
            {
                if (c == null) continue;
                var d = c.GetComponentInParent<IDamageable>();
                if (d != null && d.IsAlive)
                    d.TakeDamage(new DamageInfo(damage, _owner, transform.position, Vector2.zero));
            }
        }
    }
}
