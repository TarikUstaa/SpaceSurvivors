using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Enemies
{
    /// <summary>
    /// Makes an enemy shoot. Self-contained like <see cref="SpaceSurvivors.Combat.ContactDamage"/>
    /// and <see cref="SeparationSteering"/> — the <see cref="EnemyBrain"/> owns "am I alive /
    /// where is the target", this component owns "fire a shot when in range" (AI_Guidelines §1).
    ///
    /// Fires <see cref="EnemyProjectile"/>s through the shared <see cref="PoolManager"/>
    /// (never Instantiate, §4). All numbers are serialized here so they can be tuned per
    /// prefab (§3) — difficulty scaling deliberately does NOT touch enemy shot damage.
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    [DisallowMultipleComponent]
    public class RangedAttack : MonoBehaviour
    {
        [Header("Projectile")]
        [Tooltip("Prefab must have EnemyProjectile + Collider2D + Rigidbody2D + PoolHandle.")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField, Min(0f)] private float _projectileSpeed = 7f;
        [SerializeField, Min(0f)] private float _projectileDamage = 6f;
        [SerializeField, Min(0.1f)] private float _projectileLifetime = 3f;

        [Header("Firing")]
        [Tooltip("Won't fire unless the target is within this distance.")]
        [SerializeField, Min(0.5f)] private float _fireRange = 8f;
        [Tooltip("Seconds between shots (or bursts).")]
        [SerializeField, Min(0.1f)] private float _fireInterval = 2f;
        [Tooltip("Shots per burst, spread across the fan below.")]
        [SerializeField, Min(1)] private int _shotsPerBurst = 1;
        [Tooltip("Total fan angle (degrees) a multi-shot burst spreads across.")]
        [SerializeField, Min(0f)] private float _spreadAngle = 0f;
        [Tooltip("Random aim error in degrees applied to the whole burst.")]
        [SerializeField, Min(0f)] private float _aimJitter = 3f;
        [Tooltip("Seconds after spawning before the first shot can go off.")]
        [SerializeField, Min(0f)] private float _firstShotDelay = 1f;

        private EnemyBrain _brain;
        private PoolManager _pool;
        private float _cooldown;

        private void Awake()
        {
            _brain = GetComponent<EnemyBrain>();
            _pool = FindAnyObjectByType<PoolManager>();
        }

        private void OnEnable() => _cooldown = _firstShotDelay;

        private void Update()
        {
            if (_pool == null || _projectilePrefab == null) return;
            if (!_brain.IsActive || _brain.Target == null) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            Vector2 origin = transform.position;
            Vector2 toTarget = (Vector2)_brain.Target.position - origin;
            if (toTarget.sqrMagnitude > _fireRange * _fireRange) return; // out of range — keep the shot ready

            _cooldown = _fireInterval;
            FireBurst(origin, toTarget);
        }

        private void FireBurst(Vector2 origin, Vector2 toTarget)
        {
            float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            if (_aimJitter > 0f) baseAngle += Random.Range(-_aimJitter, _aimJitter);

            float step = _shotsPerBurst > 1 ? _spreadAngle / (_shotsPerBurst - 1) : 0f;
            float start = _shotsPerBurst > 1 ? -_spreadAngle * 0.5f : 0f;

            for (int i = 0; i < _shotsPerBurst; i++)
            {
                float a = (baseAngle + start + step * i) * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));

                var go = _pool.Spawn(_projectilePrefab, origin, Quaternion.identity);
                if (go != null && go.TryGetComponent(out EnemyProjectile shot))
                    shot.Launch(dir, _projectileSpeed, _projectileDamage, _projectileLifetime, gameObject, _pool);
            }
        }
    }
}
