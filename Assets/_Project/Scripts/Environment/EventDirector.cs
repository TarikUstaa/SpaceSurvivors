using System;
using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// Runs one arena-wide <see cref="SpaceEventData"/> at a time (M18): waits a cooldown,
    /// rolls a weighted event that is old enough to fire, spawns its prefab and hands it a
    /// <see cref="SpaceEventContext"/>. The event object owns its own lifetime — the director
    /// just spaces them out and announces them (<see cref="EventStarted"/> for the UI banner).
    ///
    /// The catalogue comes from <c>Resources/SpaceEventCatalogue</c>; the selected
    /// <see cref="MapData.signatureEventId"/> gets a weight bump so maps feel different.
    /// Reads <see cref="RunClock"/>; owns nothing else (AI_Guidelines §1).
    /// </summary>
    [DisallowMultipleComponent]
    public class EventDirector : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private RunClock _clock;
        [SerializeField] private Transform _player;
        [SerializeField] private PoolManager _pool;
        [SerializeField] private SpawnDirector _spawns;
        [SerializeField] private ScrapCollector _collector;
        [SerializeField] private Camera _camera;

        [Header("Catalogue")]
        [Tooltip("Empty = load Resources/SpaceEventCatalogue.")]
        [SerializeField] private SpaceEventCatalogue _catalogue;

        [Header("Pacing")]
        [Tooltip("No events before this many seconds survived.")]
        [SerializeField, Min(0f)] private float _firstEventAt = 60f;
        [Tooltip("Quiet gap after one event ends before the next can roll.")]
        [SerializeField] private Vector2 _cooldownRange = new(35f, 70f);
        [Tooltip("Extra weight multiplier for the current map's signature event.")]
        [SerializeField, Min(1f)] private float _signatureBias = 3f;

        /// <summary>Fired when an event starts — carries the data so the UI can announce it.</summary>
        public event Action<SpaceEventData> EventStarted;

        private readonly List<SpaceEventData> _pickable = new();
        private float _nextRollTime;
        private GameObject _active;

        private void Awake()
        {
            if (_clock == null) _clock = FindFirstObjectByType<RunClock>();
            if (_pool == null) _pool = FindFirstObjectByType<PoolManager>();
            if (_spawns == null) _spawns = FindFirstObjectByType<SpawnDirector>();
            if (_collector == null) _collector = FindFirstObjectByType<ScrapCollector>();
            if (_camera == null) _camera = Camera.main;
            if (_catalogue == null) _catalogue = Resources.Load<SpaceEventCatalogue>("SpaceEventCatalogue");
        }

        private void Start() => _nextRollTime = Mathf.Max(_firstEventAt, Elapsed + _cooldownRange.x);

        private float Elapsed => _clock != null ? _clock.Elapsed : Time.timeSinceLevelLoad;

        private void Update()
        {
            if (_catalogue == null || _pool == null || _player == null) return;
            if (_active != null && _active.activeInHierarchy) return;   // one at a time
            if (Elapsed < _nextRollTime) return;

            var data = Roll();
            if (data == null) { _nextRollTime = Elapsed + 10f; return; } // nothing eligible yet — retry soon

            Fire(data);
            _nextRollTime = Elapsed + data.duration + UnityEngine.Random.Range(_cooldownRange.x, _cooldownRange.y);
        }

        private SpaceEventData Roll()
        {
            string signature = MapService.Selected != null ? MapService.Selected.signatureEventId : "";

            _pickable.Clear();
            float total = 0f;
            foreach (var e in _catalogue.events)
            {
                if (e == null || e.eventPrefab == null || e.weight <= 0f) continue;
                if (Elapsed < e.earliestTime) continue;
                _pickable.Add(e);
                total += Weight(e, signature);
            }
            if (_pickable.Count == 0) return null;

            float r = UnityEngine.Random.value * total;
            foreach (var e in _pickable)
            {
                r -= Weight(e, signature);
                if (r <= 0f) return e;
            }
            return _pickable[^1];
        }

        private float Weight(SpaceEventData e, string signatureId)
            => e.weight * (!string.IsNullOrEmpty(signatureId) && e.id == signatureId ? _signatureBias : 1f);

        private void Fire(SpaceEventData data)
        {
            var go = _pool.Spawn(data.eventPrefab, _player.position, Quaternion.identity);
            if (go == null) return;
            _active = go;

            if (go.TryGetComponent(out ISpaceEvent ev))
                ev.Begin(new SpaceEventContext(_player, _pool, _spawns, _collector, _camera, data.duration));

            EventStarted?.Invoke(data);
        }
    }
}
