using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Online leaderboard <see cref="ILeaderboardStore"/>, posting to the backend's
    /// <c>/v1/leaderboard</c> endpoint.
    ///
    /// <para>Same shape as <see cref="HttpProfileStore"/>: a local store answers every question
    /// immediately and the network is a background extra. <see cref="Submit"/> in particular
    /// <b>must</b> return synchronously — the end screen decides whether to print "NEW BEST" off
    /// its return value — so the local record check happens first and the POST goes out after.
    /// </para>
    ///
    /// <para>The two sides agree without coordinating because the rule is the same on both:
    /// a run is only stored if it beats the previous best. The server re-checks rather than
    /// trusting us, which is what keeps a tampered client from writing a fake record.</para>
    ///
    /// <para>Reading other players' scores is not here yet — nothing displays a board. That
    /// endpoint (<c>GET /v1/leaderboard</c>) gets a client when a leaderboard screen needs it.</para>
    /// </summary>
    public sealed class HttpLeaderboardStore : ILeaderboardStore
    {
        private readonly ILeaderboardStore _local;
        private readonly string _url;

        public HttpLeaderboardStore(string url, ILeaderboardStore local = null)
        {
            _url = url;
            _local = local ?? new LocalPrefsLeaderboardStore();
        }

        /// <summary>Straight from the local cache — this is read while drawing the end screen.</summary>
        public float BestSeconds(string modeId) => _local.BestSeconds(modeId);

        public bool Submit(string modeId, RunResult run)
        {
            // Local first, and synchronously: the caller needs the answer now, and the score must
            // survive even if the network never comes back.
            bool isLocalRecord = _local.Submit(modeId, run);

            Post(modeId, run);
            return isLocalRecord;
        }

        /// <summary>
        /// Pull the ranked board. Falls back to the local store's single "your best" row when
        /// the mode is not one the server ranks, or when the request does not come back — the
        /// screen distinguishes the two by <see cref="LeaderboardBoard.FromServer"/>.
        /// </summary>
        public void FetchBoard(string modeId, int limit, Action<LeaderboardBoard> onDone)
        {
            string mode = ToWireMode(modeId);
            if (mode == null)
            {
                _local.FetchBoard(modeId, limit, onDone);
                return;
            }

            int clamped = Mathf.Clamp(limit, 1, 100);
            string url = $"{_url}?mode={mode}&limit={clamped}";

            BackendRequest.Send(url, "GET", null, (code, body) =>
            {
                if (code != 200)
                {
                    // Offline, or the server refused: show what this device knows, marked as
                    // not a real ranking. A 401 will already have been retried once upstream.
                    if (code != BackendRequest.NoConnection)
                        Debug.LogWarning($"[Backend] leaderboard fetch failed ({code}): {body}");
                    _local.FetchBoard(modeId, clamped, onDone);
                    return;
                }

                var parsed = BackendRequest.Parse<BoardResponse>(body);
                if (parsed?.entries == null)
                {
                    _local.FetchBoard(modeId, clamped, onDone);
                    return;
                }

                onDone(ToBoard(modeId, parsed));
            });
        }

        private static LeaderboardBoard ToBoard(string modeId, BoardResponse wire)
        {
            int myRank = wire.me?.rank ?? -1;

            var rows = new List<LeaderboardRow>(wire.entries.Length);
            foreach (var e in wire.entries)
            {
                rows.Add(new LeaderboardRow
                {
                    Rank = e.rank,
                    Name = string.IsNullOrEmpty(e.displayName) ? "—" : e.displayName,
                    Seconds = (float)e.survivedSeconds,
                    Kills = e.kills,
                    Level = e.reachedLevel,
                    // The server does not name whose row is whose; the rank in `me` is how we
                    // find it. Falls through harmlessly when the player is outside the top N.
                    IsMe = e.rank == myRank,
                });
            }

            LeaderboardRow me = null;
            if (wire.me != null)
            {
                me = rows.Find(r => r.IsMe) ?? new LeaderboardRow
                {
                    Rank = wire.me.rank,
                    Name = "YOU",
                    Seconds = (float)wire.me.survivedSeconds,
                    IsMe = true,
                };
            }

            return new LeaderboardBoard
            {
                ModeId = modeId,
                FromServer = true,
                Entries = rows,
                Me = me,
            };
        }

        /// <summary>
        /// Push the run for ranking. Sent on every run, not only local records: this device's
        /// best can lag the server's (a run on another device), so the server needs the chance
        /// to make its own comparison.
        /// </summary>
        private void Post(string modeId, RunResult run)
        {
            string mode = ToWireMode(modeId);
            if (mode == null)
            {
                // Usually means the run started straight from the Game scene, so GameSession
                // never got a mode and the id fell back to "default". The local best is still
                // recorded; say so rather than dropping the score without a trace.
                Debug.LogWarning($"[Backend] '{modeId}' is not a ranked mode — score kept locally only. "
                                 + "Start from the main menu so a mode is selected.");
                return;
            }

            var body = BackendRequest.Serialize(new SubmitRequest
            {
                mode = mode,
                survivedSeconds = run.SurvivedSeconds,
                kills = run.Kills,
                reachedLevel = run.ReachedLevel,
                bossesDefeated = run.BossesDefeated,
            });
            if (body == null) return;

            BackendRequest.Send(_url, "POST", body, OnPosted);
        }

        private static void OnPosted(long code, string body)
        {
            if (code == 200)
            {
                var result = BackendRequest.Parse<SubmitResponse>(body);
                if (result != null && result.isNewRecord)
                    Debug.Log($"[Backend] new server record: {result.personalBest:0}s, rank {result.rank}");
                return;
            }

            if (code == BackendRequest.NoConnection) return;   // offline is normal, stay quiet

            // 422 means the server judged the run implausible — worth seeing during development,
            // but never something the player is told about.
            Debug.LogWarning($"[Backend] score submit rejected ({code}): {body}");
        }

        /// <summary>
        /// Translate the game's mode id into the vocabulary the API defines.
        /// The assets are named <c>Mode_Infinite</c> / <c>Mode_Campaign</c>; the server ranks
        /// <c>infinite</c> / <c>campaign</c>. Mapping here rather than renaming the assets keeps
        /// the existing local <c>score.best.*</c> keys — and their saved bests — intact.
        /// </summary>
        /// <returns>The wire mode, or null if the server does not rank this mode.</returns>
        private static string ToWireMode(string modeId)
        {
            string id = (modeId ?? "").Trim().ToLowerInvariant();
            if (id.StartsWith("mode_")) id = id[5..];

            return id is "infinite" or "campaign" ? id : null;
        }

        // ── wire shapes (mirror the backend's api contract) ────────────────────────────

        private sealed class SubmitRequest
        {
            public string mode;
            public float survivedSeconds;
            public int kills;
            public int reachedLevel;
            public int bossesDefeated;
        }

        private sealed class SubmitResponse
        {
            public float personalBest;
            public bool isNewRecord;
            public int? rank;
        }

        private sealed class BoardResponse
        {
            public string mode;
            public BoardEntry[] entries;
            public MeStanding me;
        }

        private sealed class BoardEntry
        {
            public int rank;
            public string displayName;
            public double survivedSeconds;
            public int kills;
            public int reachedLevel;
        }

        private sealed class MeStanding
        {
            public int rank;
            public double survivedSeconds;
        }
    }
}
