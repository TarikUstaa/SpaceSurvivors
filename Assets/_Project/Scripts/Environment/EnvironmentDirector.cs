using System;
using System.Collections.Generic;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// Streams the arena environment for the open world (M15). The asteroid / cache / hazard
    /// field is the <b>same on every map</b> — configured here, not on the map — because the
    /// obstacles are core gameplay, not a backdrop feature. The selected <see cref="MapData"/>
    /// only supplies the look (sky colour, star tint, nebula backdrop), applied on
    /// <see cref="Start"/>.
    ///
    /// Cells around the player are kept populated with pooled props; cell contents are
    /// deterministic per cell so backtracking finds the same field; far cells are recycled.
    /// It also turns a cracked <see cref="Obstacle"/> into debris VFX + scrap drops
    /// (mirroring <c>SpawnDirector</c> / <c>LootDropper</c> — the prefab stays loot-agnostic).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnvironmentDirector : MonoBehaviour
    {
        [Serializable]
        public struct PropEntry
        {
            public GameObject prefab;
            [Min(0f)] public float weight;
        }

        [Header("Dependencies")]
        [SerializeField] private PoolManager _pool;
        [SerializeField] private Transform _player;
        [SerializeField] private Camera _camera;
        [SerializeField] private ScrapCollector _collector;
        [SerializeField] private StarfieldParallax _starfield;

        [Header("Field (the same on every map)")]
        [SerializeField] private PropEntry[] _props = Array.Empty<PropEntry>();
        [SerializeField, Min(0)] private int _propsPerCell = 5;
        [SerializeField, Min(4f)] private float _cellSize = 14f;
        [Tooltip("Keep-clear radius around the world origin so the run never starts inside a rock.")]
        [SerializeField, Min(0f)] private float _spawnClearRadius = 5f;
        [Tooltip("Cells kept populated on each side of the player's cell (3 → a 7×7 block).")]
        [SerializeField, Min(1)] private int _ringRadius = 3;

        [Header("Loot / VFX")]
        [SerializeField] private GameObject _debrisVfxPrefab;
        [Tooltip("Scrap pickup prefab — the same XpPickup the LootDropper uses.")]
        [SerializeField] private GameObject _scrapPickupPrefab;
        [SerializeField, Min(1)] private int _xpPerScrap = 1;

        [Header("Look fallback")]
        [Tooltip("Used only when Play is pressed with no map picked.")]
        [SerializeField] private MapData _fallbackMap;

        private float _totalWeight;
        private Vector2Int _center = new(int.MinValue, int.MinValue);

        private readonly Dictionary<Vector2Int, List<GameObject>> _cells = new();
        private readonly Dictionary<GameObject, Vector2Int> _cellOf = new();
        private readonly List<Vector2Int> _scratch = new();

        private void Start()
        {
            if (_camera == null) _camera = Camera.main;
            if (_collector == null) _collector = FindFirstObjectByType<ScrapCollector>();

            ApplyLook(MapService.Selected != null ? MapService.Selected : _fallbackMap);

            if (_pool == null || _player == null) { enabled = false; return; }

            foreach (var p in _props)
                if (p.prefab != null) _totalWeight += Mathf.Max(0f, p.weight);

            _cellSize = Mathf.Max(4f, _cellSize);
            if (_totalWeight > 0f && _propsPerCell > 0) Rebuild();
        }

        private void ApplyLook(MapData map)
        {
            if (map == null) return;
            if (_camera != null) _camera.backgroundColor = map.cameraBackground;
            if (_starfield != null)
            {
                _starfield.SetTint(map.starfieldTint);
                _starfield.SetBackdrop(map.backdropSprite, map.backdropTint);
            }
        }

        private void Update()
        {
            if (_totalWeight <= 0f) return;
            if (CellOf(_player.position) != _center) Rebuild();
        }

        private void Rebuild()
        {
            _center = CellOf(_player.position);

            for (int dx = -_ringRadius; dx <= _ringRadius; dx++)
            for (int dy = -_ringRadius; dy <= _ringRadius; dy++)
            {
                var cell = new Vector2Int(_center.x + dx, _center.y + dy);
                if (!_cells.ContainsKey(cell)) Populate(cell);
            }

            _scratch.Clear();
            int keep = _ringRadius + 1;
            foreach (var cell in _cells.Keys)
                if (Mathf.Abs(cell.x - _center.x) > keep || Mathf.Abs(cell.y - _center.y) > keep)
                    _scratch.Add(cell);
            foreach (var cell in _scratch) Depopulate(cell);
        }

        private void Populate(Vector2Int cell)
        {
            var list = new List<GameObject>();
            _cells[cell] = list;

            var rng = new System.Random(HashCell(cell));
            float clearSqr = _spawnClearRadius * _spawnClearRadius;

            for (int i = 0; i < _propsPerCell; i++)
            {
                var pos = new Vector2(
                    (cell.x + (float)rng.NextDouble()) * _cellSize,
                    (cell.y + (float)rng.NextDouble()) * _cellSize);
                if (pos.sqrMagnitude < clearSqr) continue;

                var prefab = PickProp(rng);
                if (prefab == null) continue;

                var go = _pool.Spawn(prefab, pos, Quaternion.identity);
                if (go == null) continue;

                if (go.TryGetComponent(out Obstacle ob)) ob.Destroyed += HandleObstacleDestroyed;
                list.Add(go);
                _cellOf[go] = cell;
            }
        }

        private void Depopulate(Vector2Int cell)
        {
            if (!_cells.TryGetValue(cell, out var list)) return;
            foreach (var go in list)
            {
                if (go == null) continue;
                if (go.TryGetComponent(out Obstacle ob)) ob.Destroyed -= HandleObstacleDestroyed;
                _cellOf.Remove(go);
                _pool.Despawn(go);
            }
            _cells.Remove(cell);
        }

        private GameObject PickProp(System.Random rng)
        {
            float r = (float)rng.NextDouble() * _totalWeight;
            foreach (var p in _props)
            {
                if (p.prefab == null) continue;
                r -= Mathf.Max(0f, p.weight);
                if (r <= 0f) return p.prefab;
            }
            return null;
        }

        private void HandleObstacleDestroyed(Obstacle ob, Vector2 pos, DamageInfo info)
        {
            ob.Destroyed -= HandleObstacleDestroyed;

            if (_cellOf.TryGetValue(ob.gameObject, out var cell))
            {
                _cellOf.Remove(ob.gameObject);
                if (_cells.TryGetValue(cell, out var list)) list.Remove(ob.gameObject);
            }

            if (_debrisVfxPrefab != null) _pool.Spawn(_debrisVfxPrefab, pos, Quaternion.identity);

            int scrap = ob.ScrapReward;
            if (scrap <= 0 || _scrapPickupPrefab == null || _collector == null) return;

            int drops = Mathf.Clamp(scrap / 5, 1, 5);
            int each = Mathf.Max(1, Mathf.RoundToInt(scrap / (float)drops));
            for (int i = 0; i < drops; i++)
            {
                Vector2 p = pos + UnityEngine.Random.insideUnitCircle * 0.6f;
                var go = _pool.Spawn(_scrapPickupPrefab, p, Quaternion.identity);
                if (go != null && go.TryGetComponent(out XpPickup pk))
                    pk.Configure(_collector, each, each * _xpPerScrap);
            }
        }

        private int HashCell(Vector2Int c)
            => unchecked((c.x * 73856093) ^ (c.y * 19349663) ^ ((c.x + c.y) * 83492791) ^ 0x5f356495);

        private Vector2Int CellOf(Vector3 p)
            => new(Mathf.FloorToInt(p.x / _cellSize), Mathf.FloorToInt(p.y / _cellSize));
    }
}
