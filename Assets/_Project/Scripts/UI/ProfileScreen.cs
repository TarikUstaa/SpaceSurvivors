using System;
using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// The player's own page: who they are at the top, what they have done underneath,
    /// one scroll.
    ///
    /// <para>The two halves come from different places and that is the reason they are on
    /// one screen rather than two. The account — name, member since — belongs to the
    /// <em>server</em> and is only ever asked to change (<see cref="PlayerIdentity"/>); the
    /// stats are the local save, written by the game and merely stored by the server
    /// (<see cref="ProfileService"/>). A player thinks of both as "me", so they are shown
    /// together; the code keeps them apart because they fail differently.</para>
    ///
    /// <para>Renaming is the only interaction. It is validated locally first so an
    /// obviously bad name costs no round trip, and the three ways it can fail are reported
    /// separately because the player's next move differs for each.</para>
    ///
    /// <para>Prefab-style (§7): every widget is an Inspector reference wired by
    /// <c>ProfileBuilder</c>. This class formats and assigns, and owns no state.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ProfileScreen : MonoBehaviour
    {
        [Header("Account")]
        [SerializeField] private InputField _nameInput;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Text _nameStatus;
        [SerializeField] private Text _memberSinceValue;
        [SerializeField] private Text _countryValue;

        [Header("Stats — runs")]
        [SerializeField] private Text _runsValue;
        [SerializeField] private Text _bestTimeValue;
        [SerializeField] private Text _bestLevelValue;
        [SerializeField] private Text _bestKillsValue;

        [Header("Stats — lifetime")]
        [SerializeField] private Text _lifetimeKillsValue;
        [SerializeField] private Text _bossKillsValue;
        [SerializeField] private Text _lifetimeScrapValue;

        [Header("Stats — now")]
        [SerializeField] private Text _walletValue;
        [SerializeField] private Text _achievementsValue;

        [Header("Status colours")]
        [SerializeField] private Color _statusNeutral = new(0.62f, 0.76f, 0.9f);
        [SerializeField] private Color _statusGood = new(0.55f, 0.95f, 0.65f);
        [SerializeField] private Color _statusBad = new(1f, 0.62f, 0.55f);

        [Header("Navigation")]
        [SerializeField] private Button _backButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        /// <summary>The name the server last confirmed — what Save is compared against.</summary>
        private string _confirmedName = "";

        private bool _saving;

        /// <summary>
        /// The name the status line is talking about, or empty when it is complaining about
        /// whatever is typed right now.
        ///
        /// <para>This is what lets the message be cleared at the right moment and only
        /// then. Leaving "3-16 characters" up after the player fixes it contradicts the
        /// Save button that just went live; leaving "that name is taken" up while they type
        /// a different name says something untrue about the new one. Both go when the text
        /// stops being the text the message was about — and a result stays put while they
        /// are still looking at the name that earned it.</para>
        /// </summary>
        private string _statusAbout = "";

        private bool _hasStatus;

        private void Awake()
        {
            EnsureEventSystem();

            if (_backButton != null)
                _backButton.onClick.AddListener(() => SceneManager.LoadScene(_menuSceneName));

            if (_saveButton != null)
                _saveButton.onClick.AddListener(Save);

            if (_nameInput != null)
            {
                _nameInput.characterLimit = PlayerIdentity.MaxNameLength;
                _nameInput.onValueChanged.AddListener(_ => RefreshSaveState());
            }
        }

        private void OnEnable()
        {
            RefreshStats();
            ProfileService.Changed += RefreshStats;
            AchievementService.Changed += RefreshStats;
            LoadAccount();
        }

        private void OnDisable()
        {
            ProfileService.Changed -= RefreshStats;
            AchievementService.Changed -= RefreshStats;
        }

        // ── account ────────────────────────────────────────────────────────────────────

        private void LoadAccount()
        {
            // Show the cached name immediately so the field is never blank while the
            // request is out; the fetch corrects it if the server disagrees.
            _confirmedName = PlayerIdentity.CachedName;
            if (_nameInput != null) _nameInput.text = _confirmedName;
            SetStatus(PlayerIdentity.Available ? "" : "cloud sync is off", _statusNeutral);
            RefreshSaveState();

            PlayerIdentity.Fetch(account =>
            {
                if (this == null) return;   // screen closed while the request was out

                _confirmedName = account.DisplayName;
                if (_nameInput != null && !_nameInput.isFocused) _nameInput.text = account.DisplayName;

                Set(_memberSinceValue, account.FirstLogin.HasValue
                    ? account.FirstLogin.Value.ToString("d MMM yyyy")
                    : "–");
                // The column has no source yet — the server leaves it null until a host or
                // CDN supplies one, so a dash is the honest answer rather than a guess.
                Set(_countryValue, string.IsNullOrEmpty(account.Country) ? "–" : account.Country);

                if (!account.FromServer)
                    SetStatus(PlayerIdentity.Available ? "offline — cannot change your name now"
                                                       : "cloud sync is off", _statusNeutral);
                RefreshSaveState();
            });
        }

        private void Save()
        {
            if (_saving || _nameInput == null) return;

            string wanted = _nameInput.text.Trim();
            _saving = true;
            RefreshSaveState();
            SetStatus("saving…", _statusNeutral, wanted);

            PlayerIdentity.Rename(wanted, (outcome, message) =>
            {
                if (this == null) return;
                _saving = false;

                switch (outcome)
                {
                    case RenameOutcome.Ok:
                        _confirmedName = message;
                        if (_nameInput != null) _nameInput.text = message;
                        SetStatus("saved", _statusGood, message);
                        break;

                    case RenameOutcome.Taken:
                    case RenameOutcome.Invalid:
                        SetStatus(message, _statusBad, wanted);
                        break;

                    default:
                        SetStatus(message, _statusNeutral, wanted);
                        break;
                }
                RefreshSaveState();
            });
        }

        /// <summary>
        /// Save is available only when the typed name is legal, different from the one the
        /// server already has, and there is a server to tell.
        /// </summary>
        private void RefreshSaveState()
        {
            if (_saveButton == null) return;

            string typed = _nameInput != null ? _nameInput.text.Trim() : "";
            bool legal = PlayerIdentity.IsValidName(typed, out string reason);
            bool changed = !string.Equals(typed, _confirmedName, StringComparison.Ordinal);

            _saveButton.interactable = !_saving && legal && changed && PlayerIdentity.Available;

            if (_saving) return;

            if (typed.Length > 0 && !legal)
            {
                // Only nag once they have typed enough to mean it.
                SetStatus(reason, _statusBad);
            }
            else if (_hasStatus && !string.Equals(typed, _statusAbout, StringComparison.Ordinal))
            {
                // The message was about a different name than the one in the box now. With
                // no server the notice stays, because otherwise Save would sit there dead
                // with nothing on screen saying why.
                SetStatus(PlayerIdentity.Available ? "" : "cloud sync is off", _statusNeutral);
            }
        }

        /// <summary>
        /// Show a message about <paramref name="about"/>, or about the live text when that
        /// is null. See <see cref="_statusAbout"/> for why the distinction matters.
        /// </summary>
        private void SetStatus(string text, Color colour, string about = null)
        {
            _hasStatus = !string.IsNullOrEmpty(text);
            _statusAbout = about ?? (_nameInput != null ? _nameInput.text.Trim() : "");
            if (_nameStatus == null) return;
            _nameStatus.text = text;
            _nameStatus.color = colour;
        }

        // ── stats ──────────────────────────────────────────────────────────────────────

        private void RefreshStats()
        {
            var p = ProfileService.Current;

            Set(_runsValue, p.runsPlayed.ToString("n0"));
            Set(_bestTimeValue, Clock(p.bestSurvivalSeconds));
            Set(_bestLevelValue, p.bestLevel > 0 ? p.bestLevel.ToString() : "–");
            Set(_bestKillsValue, p.bestKills.ToString("n0"));

            Set(_lifetimeKillsValue, p.lifetimeKills.ToString("n0"));
            Set(_bossKillsValue, p.bossKills.ToString("n0"));
            Set(_lifetimeScrapValue, p.lifetimeScrap.ToString("n0"));

            Set(_walletValue, p.wallet.ToString("n0"));
            Set(_achievementsValue, Achievements());
        }

        private static string Achievements()
        {
            int done = 0, total = 0;
            foreach (var a in AchievementService.All)
            {
                total++;
                if (AchievementService.IsUnlocked(a)) done++;
            }
            return total > 0 ? $"{done} / {total}" : "–";
        }

        private static void Set(Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private static string Clock(int seconds)
        {
            int s = Mathf.Max(0, seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
