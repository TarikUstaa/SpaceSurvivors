using System;
using SpaceSurvivors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// The ranked board on the main menu, in the slot the pilot-record card used to hold.
    ///
    /// <para>Two tabs — Infinite and Campaign — because the two modes rank on completely
    /// different runs and a single list would mix them into nonsense. The tab is the whole
    /// interaction; everything else is display.</para>
    ///
    /// <para>Prefab-style like the other screens (AI_Guidelines §7): a fixed set of row
    /// widgets wired in the Inspector by <c>MainMenuBuilder</c>, filled from the fetched
    /// board, no runtime instantiation. If the board has more entries than rows the extras
    /// are dropped; if this player ranks below the visible rows their standing is forced
    /// into the last one so they always see where they sit.</para>
    ///
    /// <para>Reads only — the board is fetched through <see cref="HighScoreService"/>, which
    /// answers from the server when it can and from this device's own best when it cannot.
    /// A backend that is switched off or unreachable is not an error here: the status line
    /// says so and the one local row is shown.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuLeaderboard : MonoBehaviour
    {
        [Serializable]
        public struct Row
        {
            public GameObject root;
            public Text rank;
            public Text name;
            public Text time;
            /// <summary>Tint behind this player's own row. Hidden on every other row.</summary>
            public Image highlight;
        }

        [Header("Mode tabs")]
        [SerializeField] private Button _infiniteTab;
        [SerializeField] private Button _campaignTab;
        [SerializeField] private string _infiniteModeId = "Mode_Infinite";
        [SerializeField] private string _campaignModeId = "Mode_Campaign";

        [Header("Board")]
        [SerializeField] private Row[] _rows = Array.Empty<Row>();
        [SerializeField] private Text _statusLabel;

        [Header("Tab colours")]
        [SerializeField] private Color _tabSelected = new(0.7f, 0.88f, 1f, 0.95f);
        [SerializeField] private Color _tabIdle = new(0.4f, 0.55f, 0.7f, 0.6f);

        private bool _showingCampaign;

        /// <summary>Bumped every fetch so a slow reply for the old tab is ignored when it lands.</summary>
        private int _request;

        private void Awake()
        {
            if (_infiniteTab != null) _infiniteTab.onClick.AddListener(() => Select(campaign: false));
            if (_campaignTab != null) _campaignTab.onClick.AddListener(() => Select(campaign: true));
        }

        private void OnEnable() => Select(_showingCampaign);

        private void Select(bool campaign)
        {
            _showingCampaign = campaign;
            PaintTabs();
            Load();
        }

        private void PaintTabs()
        {
            if (_infiniteTab != null) _infiniteTab.image.color = _showingCampaign ? _tabIdle : _tabSelected;
            if (_campaignTab != null) _campaignTab.image.color = _showingCampaign ? _tabSelected : _tabIdle;
        }

        private void Load()
        {
            int token = ++_request;
            string modeId = _showingCampaign ? _campaignModeId : _infiniteModeId;

            SetStatus("…");
            HighScoreService.FetchBoard(modeId, _rows.Length, board =>
            {
                if (token != _request) return;   // the player switched tabs before this came back
                Populate(board);
            });
        }

        private void Populate(LeaderboardBoard board)
        {
            int shown = 0;
            var entries = board.Entries;

            for (int i = 0; i < _rows.Length; i++)
            {
                // Keep the last row free for this player's standing when they rank below the
                // visible rows and the board is a real ranking.
                bool reserveLastForMe = board.FromServer
                                        && board.Me != null
                                        && !MeVisible(entries, _rows.Length)
                                        && i == _rows.Length - 1;

                if (reserveLastForMe)
                {
                    Fill(_rows[i], board.Me);
                    shown++;
                    continue;
                }

                if (i < entries.Count)
                {
                    Fill(_rows[i], entries[i]);
                    shown++;
                }
                else
                {
                    Clear(_rows[i]);
                }
            }

            if (!board.FromServer)
                SetStatus(shown > 0 ? "offline — your best only" : "offline");
            else if (shown == 0)
                SetStatus("no runs yet — be the first");
            else
                SetStatus("");
        }

        private static bool MeVisible(System.Collections.Generic.IReadOnlyList<LeaderboardRow> entries, int rowCount)
        {
            int limit = Mathf.Min(entries.Count, rowCount);
            for (int i = 0; i < limit; i++)
                if (entries[i].IsMe) return true;
            return false;
        }

        private static void Fill(Row row, LeaderboardRow data)
        {
            if (row.root != null) row.root.SetActive(true);
            if (row.rank != null) row.rank.text = data.Rank > 0 ? data.Rank.ToString() : "–";
            if (row.name != null) row.name.text = data.Name;
            if (row.time != null) row.time.text = Clock(data.Seconds);
            if (row.highlight != null) row.highlight.enabled = data.IsMe;
        }

        private static void Clear(Row row)
        {
            if (row.root != null) row.root.SetActive(false);
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null) _statusLabel.text = text;
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
