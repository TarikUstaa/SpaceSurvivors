using System.Collections.Generic;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Core;
using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using SpaceSurvivors.Game;
using SpaceSurvivors.Progression;
using UnityEngine;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Single place that turns gameplay events into one-shot sounds. It only *listens* —
    /// weapons, enemies, the run controller etc. raise their normal C# events and never call
    /// audio (AI_Guidelines §1). Owns a small round-robin pool of <see cref="AudioSource"/>s
    /// and reads the SFX volume from <see cref="SettingsService"/>. Lives in the UI layer
    /// because it observes every layer; sits on a scene object in both scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioDirector : MonoBehaviour
    {
        [SerializeField] private SfxBank _bank;
        [SerializeField, Min(1)] private int _voices = 10;

        private AudioSource[] _sources;
        private int _next;
        private readonly Dictionary<SfxId, float> _lastPlayed = new();

        // watched sources (any may be absent, e.g. in the main menu)
        private WeaponController _weapons;
        private SpawnDirector _spawnDirector;
        private LevelSystem _level;
        private ScrapCollector _scrap;
        private ShieldComponent _shield;
        private HealthComponent _playerHealth;
        private RunController _run;

        private void Awake()
        {
            _sources = new AudioSource[Mathf.Max(1, _voices)];
            for (int i = 0; i < _sources.Length; i++)
            {
                var go = new GameObject($"Voice{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _sources[i] = src;
            }

            _weapons = FindFirstObjectByType<WeaponController>();
            _spawnDirector = FindFirstObjectByType<SpawnDirector>();
            _level = FindFirstObjectByType<LevelSystem>();
            _scrap = FindFirstObjectByType<ScrapCollector>();
            _shield = FindFirstObjectByType<ShieldComponent>();
            _playerHealth = _level != null ? _level.GetComponent<HealthComponent>() : null;
            _run = FindFirstObjectByType<RunController>();
        }

        private void OnEnable()
        {
            if (_weapons != null) _weapons.WeaponFired += HandleWeaponFired;
            if (_spawnDirector != null)
            {
                _spawnDirector.EnemyKilled += HandleEnemyKilled;
                _spawnDirector.BossDefeated += HandleBossDefeated;
                _spawnDirector.BossIncoming += HandleBossIncoming;
            }
            if (_level != null) _level.LeveledUp += HandleLeveledUp;
            if (_scrap != null) _scrap.ScrapCollected += HandleScrap;
            if (_shield != null) _shield.Absorbed += HandleShieldAbsorb;
            if (_playerHealth != null) _playerHealth.Damaged += HandlePlayerDamaged;
            if (_run != null) _run.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (_weapons != null) _weapons.WeaponFired -= HandleWeaponFired;
            if (_spawnDirector != null)
            {
                _spawnDirector.EnemyKilled -= HandleEnemyKilled;
                _spawnDirector.BossDefeated -= HandleBossDefeated;
                _spawnDirector.BossIncoming -= HandleBossIncoming;
            }
            if (_level != null) _level.LeveledUp -= HandleLeveledUp;
            if (_scrap != null) _scrap.ScrapCollected -= HandleScrap;
            if (_shield != null) _shield.Absorbed -= HandleShieldAbsorb;
            if (_playerHealth != null) _playerHealth.Damaged -= HandlePlayerDamaged;
            if (_run != null) _run.RunEnded -= HandleRunEnded;
        }

        // ---- public entry (UI buttons call this directly; it's a sibling UI component) ----

        public void Play(SfxId id)
        {
            if (_bank == null || id == SfxId.None) return;

            float interval = _bank.MinInterval(id);
            if (interval > 0f && _lastPlayed.TryGetValue(id, out float last)
                && Time.unscaledTime - last < interval)
                return;
            _lastPlayed[id] = Time.unscaledTime;

            var clip = _bank.Resolve(id, out float vol, out float pitch);
            if (clip == null) return;

            var src = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            src.clip = clip;
            src.volume = Mathf.Clamp01(vol * SettingsService.SfxVolume);
            src.pitch = pitch;
            src.Play();
        }

        // ---- gameplay event handlers ----

        private void HandleWeaponFired(WeaponData data)
        {
            bool missile = data != null && data.displayName != null
                           && data.displayName.ToLowerInvariant().Contains("missile");
            Play(missile ? SfxId.PlayerShootMissile : SfxId.PlayerShootLaser);
        }

        private void HandleEnemyKilled(EnemyBrain brain, Vector2 pos, EnemyData data) => Play(SfxId.EnemyDeath);
        private void HandleBossDefeated() => Play(SfxId.BossDeath);
        private void HandleBossIncoming(string name, float lead) => Play(SfxId.BossWarning);
        private void HandleLeveledUp(int level) => Play(SfxId.LevelUp);
        private void HandleScrap(int amount) => Play(SfxId.PickupCollect);
        private void HandleShieldAbsorb() => Play(SfxId.ShieldAbsorb);
        private void HandlePlayerDamaged(DamageInfo info) => Play(SfxId.PlayerHurt);
        private void HandleRunEnded(bool won, float seconds) => Play(won ? SfxId.RunWon : SfxId.RunLost);
    }
}
