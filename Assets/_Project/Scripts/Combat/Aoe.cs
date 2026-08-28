using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Shared area-of-effect helper: damage every live <see cref="IDamageable"/> in a circle,
    /// skipping the owner and one optional target (the direct hit). Non-alloc. Used by the
    /// projectile splash and by mines so the blast logic lives in one place (AI_Guidelines §1).
    /// </summary>
    public static class Aoe
    {
        private static readonly Collider2D[] _buffer = new Collider2D[64];

        public static void Splash(Vector2 center, float radius, float damage,
                                  GameObject owner, IDamageable skip = null)
        {
            if (damage <= 0f || radius <= 0f) return;

            int n = Physics2D.OverlapCircleNonAlloc(center, radius, _buffer);
            for (int i = 0; i < n; i++)
            {
                var col = _buffer[i];
                if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == owner) continue;

                var d = col.GetComponentInParent<IDamageable>();
                if (d == null || ReferenceEquals(d, skip) || !d.IsAlive) continue;

                d.TakeDamage(new DamageInfo(damage, owner, center, Vector2.zero));
            }
        }
    }
}
