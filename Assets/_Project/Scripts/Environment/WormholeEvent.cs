using SpaceSurvivors.Core;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// M18 event — a wormhole tears open near the player. Fly into it and you are blinked
    /// across the arena, with a scrap burst waiting where you come out. It stays open for
    /// the event duration; a short cooldown stops it from chain-teleporting you.
    /// </summary>
    public class WormholeEvent : SpaceEventBehaviour
    {
        [SerializeField] private GameObject _portalPrefab;
        [SerializeField] private GameObject _exitVfxPrefab;
        [SerializeField] private GameObject _scrapPickupPrefab;
        [SerializeField, Min(0.3f)] private float _triggerRadius = 1.1f;
        [SerializeField, Min(2f)] private float _blinkDistance = 16f;
        [SerializeField, Min(0.5f)] private float _cooldown = 2f;
        [SerializeField, Min(0)] private int _scrapOnExit = 6;
        [SerializeField, Min(6f)] private float _spawnDistance = 6f;

        private Transform _portal;
        private float _cd;

        protected override void OnBegin()
        {
            Vector3 at = PlayerPos + (Vector3)(Random.insideUnitCircle.normalized * _spawnDistance);
            var go = SpawnChild(_portalPrefab, at, Quaternion.identity);
            _portal = go != null ? go.transform : null;
            _cd = 1f;
        }

        protected override void OnTick(float dt)
        {
            if (_portal == null || Ctx.Player == null) return;
            _cd -= dt;
            if (_cd > 0f) return;

            if (Vector2.Distance(_portal.position, Ctx.Player.position) > _triggerRadius) return;
            _cd = _cooldown;

            Vector3 dest = Ctx.Player.position + (Vector3)(Random.insideUnitCircle.normalized * _blinkDistance);
            if (Ctx.Player.TryGetComponent(out Rigidbody2D body))
            {
                body.position = dest;
                body.linearVelocity = Vector2.zero;
            }
            Ctx.Player.position = dest;

            if (_exitVfxPrefab != null && Ctx.Pool != null)
                Ctx.Pool.Spawn(_exitVfxPrefab, dest, Quaternion.identity);

            for (int i = 0; i < _scrapOnExit; i++)
                DropScrap(_scrapPickupPrefab, dest + (Vector3)(Random.insideUnitCircle * 1.2f), 1, 1);
        }
    }
}
