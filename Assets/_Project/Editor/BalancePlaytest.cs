using System.IO;
using System.Linq;
using SpaceSurvivors.Combat;
using SpaceSurvivors.Data;
using SpaceSurvivors.Enemies;
using SpaceSurvivors.Player;
using SpaceSurvivors.Progression;
using SpaceSurvivors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.EditorTools
{
    /// <summary>
    /// M16 balance harness. Opens Game.unity, points the SpawnDirector at a chosen difficulty
    /// config, drives the player with a simple kiting autopilot (via a runtime
    /// <see cref="ExternalMoveInput"/>), auto-answers level-ups, runs the game fast, and every
    /// 10 game-seconds appends a telemetry row to
    /// <c>persistentDataPath/balance_&lt;label&gt;.csv</c>. Exits play at the time cap or player
    /// death. The scene is never saved. The bot is deliberately mediocre — survivability numbers
    /// are a conservative floor.
    /// </summary>
    internal static class BalancePlaytest
    {
        private const string Cfg = "Assets/_Project/ScriptableObjects/Config/";
        private const float SimSpeed = 2.5f;
        private const float SampleEvery = 10f;

        // run state (static — survives the edit→play boundary within a session)
        private static bool _armed;
        private static string _label;
        private static float _maxSeconds;
        private static string _csv;

        private static Transform _player;
        private static ExternalMoveInput _input;
        private static RunStats _stats;
        private static LevelSystem _level;
        private static HealthComponent _health;
        private static SpawnDirector _spawn;

        private static float _nextSample;
        private static Vector2 _heading;
        private static float _headingUntil;
        private static int _enemyMask, _threatMask, _pickupMask;

        [MenuItem("SpaceSurvivors/Balance/M16 Sim — Infinite")]
        private static void SimInfinite() => Start("DifficultyConfig", "infinite", 20f * 60f);

        [MenuItem("SpaceSurvivors/Balance/M16 Sim — Campaign")]
        private static void SimCampaign() => Start("CampaignDifficulty", "campaign", 16f * 60f);

        private static void Start(string configName, string label, float seconds)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[BalancePlaytest] already playing."); return; }

            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Single);

            var config = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(Cfg + configName + ".asset");
            var sd = Object.FindFirstObjectByType<SpawnDirector>();
            var move = Object.FindFirstObjectByType<PlayerMovement>();
            if (config == null || sd == null || move == null)
            {
                Debug.LogError($"[BalancePlaytest] missing: config={config} spawnDirector={sd} player={move}");
                return;
            }

            new SerializedObject(sd).Do(s => s.FindProperty("_config").objectReferenceValue = config);

            var ext = move.GetComponent<ExternalMoveInput>() ?? move.gameObject.AddComponent<ExternalMoveInput>();
            new SerializedObject(move).Do(s => s.FindProperty("_inputSourceBehaviour").objectReferenceValue = ext);

            _armed = true;
            _label = label;
            _maxSeconds = seconds;
            _csv = Path.Combine(Application.persistentDataPath, $"balance_{label}.csv");
            File.WriteAllText(_csv, "t,level,kills,scrap,hp,enemiesAlive,spawnRate\n");
            _player = null; _input = null; _stats = null; _level = null; _health = null; _spawn = null;
            _nextSample = 0f; _heading = Vector2.right; _headingUntil = 0f;
            _enemyMask = 1 << LayerMask.NameToLayer("Enemy");
            _threatMask = _enemyMask | (1 << LayerMask.NameToLayer("EnemyProjectile"));
            _pickupMask = 1 << LayerMask.NameToLayer("Pickup");

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;

            Debug.Log($"[BalancePlaytest] {label}: config={configName}, {seconds / 60f:0.#}min, sim {SimSpeed}x → {_csv}");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange s)
        {
            if (s != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            Time.timeScale = 1f;
            _armed = false;
            if (_csv != null) Debug.Log($"[BalancePlaytest] done → {_csv}");
        }

        private static void Tick()
        {
            if (!_armed || !EditorApplication.isPlaying || EditorApplication.isPaused) return;

            if (_player == null)
            {
                var move = Object.FindFirstObjectByType<PlayerMovement>();
                if (move == null) return;
                _player = move.transform;
                _input = move.GetComponent<ExternalMoveInput>();
                _health = move.GetComponent<HealthComponent>();
                _stats = Object.FindFirstObjectByType<RunStats>();
                _level = Object.FindFirstObjectByType<LevelSystem>();
                _spawn = Object.FindFirstObjectByType<SpawnDirector>();

                Application.runInBackground = true;   // don't throttle when the editor is unfocused
                // A competent player sweeps XP and takes pickup upgrades — model that so the
                // power curve isn't starved by the bot's mediocre pathing.
                var sheet = move.GetComponent<SpaceSurvivors.Stats.StatSheet>();
                if (sheet != null)
                    sheet.AddModifier(new SpaceSurvivors.Stats.StatModifier
                    {
                        stat = SpaceSurvivors.Stats.StatId.PickupRadius,
                        op = SpaceSurvivors.Stats.ModifierOp.PercentAdd,
                        value = 2.2f,
                    });
            }

            bool paused = Time.timeScale == 0f;                 // level-up screen holds timeScale 0
            AutoAnswerLevelUp();
            if (!paused && !Mathf.Approximately(Time.timeScale, SimSpeed)) Time.timeScale = SimSpeed;

            if (_input != null && _player != null) _input.MoveAxis = Steer();

            float t = _stats != null ? _stats.Seconds : 0f;
            if (t >= _nextSample) { _nextSample = t + SampleEvery; Sample(t); }

            bool dead = _health != null && !_health.IsAlive;
            if (t >= _maxSeconds || dead)
            {
                File.AppendAllText(_csv, dead ? $"# player died at t={(int)t}\n" : $"# time cap {(int)t}\n");
                Debug.Log($"[BalancePlaytest] {_label} ended ({(dead ? "died" : "cap")}) at t={(int)t}");
                _armed = false;
                EditorApplication.isPlaying = false;
            }
        }

        private static Vector2 Steer()
        {
            Vector2 self = _player.position;
            var enemies = Physics2D.OverlapCircleAll(self, 14f, _threatMask);

            // Panic: something is about to touch us — flee that thing regardless of the plan.
            Collider2D imminent = null; float best = 2.5f;
            foreach (var c in enemies)
            {
                float d = ((Vector2)c.transform.position - self).magnitude;
                if (d < best) { best = d; imminent = c; }
            }
            if (imminent != null)
            {
                Vector2 away = (self - (Vector2)imminent.transform.position).normalized;
                Vector2 strafe = new(-away.y, away.x);
                _heading = (away * 0.7f + strafe * 0.5f).normalized;
                _headingUntil = Time.unscaledTime + 0.2f;
                return _heading;
            }

            if (Time.unscaledTime < _headingUntil) return _heading;
            _headingUntil = Time.unscaledTime + 0.3f;

            if (enemies.Length == 0)
            {
                var xp0 = Physics2D.OverlapCircleAll(self, 16f, _pickupMask);
                _heading = xp0.Length > 0
                    ? (Nearest(xp0, self) - self).normalized
                    : (_heading == Vector2.zero ? Vector2.right : _heading);
                return _heading;
            }

            // Orbit the horde at a comfortable radius (how VS is actually played), drifting
            // the orbit toward nearby XP.
            Vector2 centroid = Vector2.zero;
            foreach (var c in enemies) centroid += (Vector2)c.transform.position;
            centroid /= enemies.Length;

            Vector2 toC = centroid - self;
            float distC = Mathf.Max(0.01f, toC.magnitude);
            Vector2 radial = -toC / distC;                         // away from the pack
            Vector2 tangent = new(-radial.y, radial.x);            // orbit
            float distErr = Mathf.Clamp((distC - 5.5f) / 3f, -1f, 1f);

            Vector2 move = tangent * 0.85f - radial * distErr * 0.6f;

            var xp = Physics2D.OverlapCircleAll(self, 11f, _pickupMask);
            if (xp.Length > 0) move += ((Nearest(xp, self) - self).normalized) * 0.4f;

            _heading = move.sqrMagnitude > 0.0001f ? move.normalized : tangent;
            return _heading;
        }

        private static Vector2 Nearest(Collider2D[] cs, Vector2 from)
            => (Vector2)cs.OrderBy(c => ((Vector2)c.transform.position - from).sqrMagnitude).First().transform.position;

        private static readonly string[] Priority =
        {
            "evolv", "damage", "fire", "multi", "pierc", "overcl", "haste",
            "orbiter", "plasma", "scatter", "rail", "mine", "static", "missile",
            "shield", "hull", "health", "speed", "pickup",
        };

        private static void AutoAnswerLevelUp()
        {
            var screen = Object.FindFirstObjectByType<LevelUpScreen>();
            if (screen == null) return;
            var buttons = screen.GetComponentsInChildren<Button>(false)
                .Where(b => b.gameObject.activeInHierarchy && b.isActiveAndEnabled).ToList();
            if (buttons.Count == 0) return;

            Button best = null; int bestScore = int.MaxValue;
            foreach (var b in buttons)
            {
                var txt = b.GetComponentInChildren<Text>();
                string s = txt != null ? txt.text.ToLowerInvariant() : "";
                int score = Priority.Length;
                for (int i = 0; i < Priority.Length; i++) if (s.Contains(Priority[i])) { score = i; break; }
                if (score < bestScore) { bestScore = score; best = b; }
            }
            (best ?? buttons[0]).onClick.Invoke();
        }

        private static void Sample(float t)
        {
            if (_player == null) return;
            int alive = Physics2D.OverlapCircleAll(_player.position, 75f, _enemyMask).Length;
            string rate = _spawn != null ? _spawn.CurrentSpawnRate.ToString("0.00") : "0";
            string row = string.Join(",",
                (int)t,
                _level != null ? _level.CurrentLevel : 0,
                _stats != null ? _stats.Kills : 0,
                _stats != null ? _stats.Scrap : 0,
                _health != null ? Mathf.RoundToInt(_health.Current) : 0,
                alive, rate);
            File.AppendAllText(_csv, row + "\n");
        }

        private static void Do(this SerializedObject so, System.Action<SerializedObject> edit)
        {
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
