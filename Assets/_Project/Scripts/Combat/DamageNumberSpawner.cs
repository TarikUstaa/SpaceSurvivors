using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Combat
{
    /// <summary>
    /// Scene service: turns every landed hit, anywhere, into a floating number at the point
    /// of impact. Subscribes once to <see cref="HealthComponent.AnyDamaged"/> — a static
    /// event every <see cref="HealthComponent"/> instance raises — so it never has to be
    /// wired onto the player or any of the nine enemy prefabs individually.
    ///
    /// <para>Not a global singleton: put ONE on a scene object and wire the pool + prefab via
    /// the Inspector, same as <see cref="PoolManager"/> itself (AI_Guidelines §1).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private PoolManager _pool;
        [SerializeField] private GameObject _damageNumberPrefab;

        private void OnEnable() => HealthComponent.AnyDamaged += HandleDamaged;
        private void OnDisable() => HealthComponent.AnyDamaged -= HandleDamaged;

        private void HandleDamaged(HealthComponent target, DamageInfo info)
        {
            if (_pool == null || _damageNumberPrefab == null) return;

            // DamageInfo's no-hit-point constructor defaults to Vector2.zero (its own doc
            // comment: "for simple cases with no positional data") — fall back to the
            // target's own position rather than plant a number at the world origin.
            Vector3 pos = info.HitPoint != Vector2.zero
                ? new Vector3(info.HitPoint.x, info.HitPoint.y, 0f)
                : target.transform.position;

            GameObject go = _pool.Spawn(_damageNumberPrefab, pos, Quaternion.identity);
            if (go != null && go.TryGetComponent(out DamageNumber number))
                number.Show(info.Amount);
        }
    }
}
