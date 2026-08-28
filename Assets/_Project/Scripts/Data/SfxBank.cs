using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Maps each <see cref="SfxId"/> to its clip(s) plus a volume and a pitch-randomisation
    /// range. Designer-authored asset, zero audio constants in code (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/SFX Bank", fileName = "SfxBank")]
    public class SfxBank : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public SfxId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Random pitch range applied per play. (1,1) = no variation.")]
            public Vector2 pitch = new Vector2(1f, 1f);
            [Tooltip("Ignore repeat plays of this id within this many seconds (0 = never).")]
            [Min(0f)] public float minInterval = 0f;
        }

        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<SfxId, Entry> _lookup;

        public Entry Get(SfxId id)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<SfxId, Entry>(_entries.Count);
                foreach (var e in _entries)
                    if (e != null && e.id != SfxId.None) _lookup[e.id] = e;
            }
            return _lookup.TryGetValue(id, out var entry) ? entry : null;
        }

        /// <summary>A random clip for this id, or null if unmapped / empty.</summary>
        public AudioClip Resolve(SfxId id, out float volume, out float pitch)
        {
            volume = 1f;
            pitch = 1f;
            var e = Get(id);
            if (e == null || e.clips == null || e.clips.Length == 0) return null;

            volume = e.volume;
            pitch = Random.Range(Mathf.Min(e.pitch.x, e.pitch.y), Mathf.Max(e.pitch.x, e.pitch.y));
            var clip = e.clips[Random.Range(0, e.clips.Length)];
            return clip;
        }

        public float MinInterval(SfxId id) => Get(id)?.minInterval ?? 0f;
    }
}
