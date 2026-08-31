using SpaceSurvivors.Data;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// M18 event — a derelict convoy drifts in off one edge: a big wreck ringed by
    /// scrap caches and guarded by a small pack of enemies. Crack the caches before the
    /// convoy drifts back out. The wreck + caches are pooled children (released on
    /// end / event timeout); the guards are ordinary enemies spawned through the
    /// <see cref="SpaceSurvivors.Enemies.SpawnDirector"/>.
    /// </summary>
    public class DerelictConvoyEvent : SpaceEventBehaviour
    {
        [SerializeField] private GameObject _wreckPrefab;
        [SerializeField] private GameObject _cachePrefab;
        [SerializeField] private EnemyData _guard;
        [SerializeField, Min(1)] private int _caches = 4;
        [SerializeField, Min(0)] private int _guards = 3;
        [Tooltip("How far off the player the convoy warps in.")]
        [SerializeField, Min(3f)] private float _distance = 7f;

        protected override void OnBegin()
        {
            float a = Random.value * Mathf.PI * 2f;
            Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));
            Vector3 anchor = PlayerPos + (Vector3)(dir * _distance);
            Vector2 drift = -dir * 0.5f; // eases back past the player

            var w = SpawnChild(_wreckPrefab, anchor, Quaternion.identity);
            if (w != null && w.TryGetComponent(out Obstacle wreck)) wreck.SetDrift(drift);

            for (int i = 0; i < _caches; i++)
            {
                float t = i / (float)_caches * Mathf.PI * 2f;
                Vector3 p = anchor + new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * 2.4f;
                var c = SpawnChild(_cachePrefab, p, Quaternion.identity);
                if (c != null && c.TryGetComponent(out Obstacle cache)) cache.SetDrift(drift);
            }

            if (_guard != null && Ctx.Spawns != null)
                for (int i = 0; i < _guards; i++)
                    Ctx.Spawns.SpawnEnemyAt(_guard, anchor + (Vector3)(Random.insideUnitCircle * 3f));
        }
    }
}
