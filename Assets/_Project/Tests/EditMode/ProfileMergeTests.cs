using System.Collections.Generic;
using NUnit.Framework;
using SpaceSurvivors.Core;

namespace SpaceSurvivors.Tests
{
    /// <summary>
    /// <see cref="ProfileMerge"/> decides, when a save has diverged between two devices, whose
    /// progress survives. It is the one place in this game where a bug costs somebody hours and
    /// nothing on screen says so — no exception, no warning, just a smaller number than they
    /// remember. That is why it is the first thing here to get tests.
    ///
    /// <para>These are the rules the class documents, written back as assertions: earned things
    /// only go up, bought things move together with the wallet that paid for them, and an idle
    /// sync changes nothing.</para>
    /// </summary>
    public class ProfileMergeTests
    {
        /// <summary>A profile with nothing in it but the collections initialised.</summary>
        private static PlayerProfile Empty() => new();

        private static PlayerProfile Profile(long lifetimeScrap, long wallet,
                                             Dictionary<string, int> upgrades = null,
                                             List<string> ships = null)
        {
            var p = Empty();
            p.lifetimeScrap = lifetimeScrap;
            p.wallet = wallet;
            if (upgrades != null) p.metaUpgradeLevels = upgrades;
            if (ships != null) p.ownedShipIds = ships;
            return p;
        }

        // ── the reason this class exists ───────────────────────────────────────────────

        [Test]
        public void A_purchase_made_offline_is_not_refunded_by_syncing()
        {
            // The player spent 800 scrap on an upgrade while offline. The server still holds
            // the save from before that purchase: same lifetime scrap (buying does not earn),
            // but a full wallet and no upgrade.
            var local = Profile(lifetimeScrap: 1000, wallet: 200,
                                upgrades: new Dictionary<string, int> { ["damage"] = 2 });
            var remote = Profile(lifetimeScrap: 1000, wallet: 1000);

            ProfileMerge.MergeInto(local, remote);

            // The naive merge — highest wallet, plus the union of purchases — would leave the
            // player holding both the upgrade and the money that paid for it. Every offline
            // purchase would be free, and the economy would be worth nothing.
            Assert.That(local.wallet, Is.EqualTo(200), "the wallet that paid for the upgrade");
            Assert.That(local.metaUpgradeLevels["damage"], Is.EqualTo(2), "the upgrade bought");
        }

        [Test]
        public void The_side_that_has_played_more_keeps_its_whole_economy()
        {
            // Local played a little, remote played a lot and spent most of it.
            var local = Profile(lifetimeScrap: 500, wallet: 500);
            var remote = Profile(lifetimeScrap: 2000, wallet: 100,
                                 upgrades: new Dictionary<string, int> { ["damage"] = 3 },
                                 ships: new List<string> { "interceptor" });
            remote.selectedShipId = "interceptor";

            ProfileMerge.MergeInto(local, remote);

            // The wallet goes DOWN, and that is correct: it arrives as one block with the
            // purchases it paid for. Keeping the higher balance here would be inventing scrap.
            Assert.That(local.wallet, Is.EqualTo(100));
            Assert.That(local.metaUpgradeLevels["damage"], Is.EqualTo(3));
            Assert.That(local.ownedShipIds, Does.Contain("interceptor"));
            Assert.That(local.selectedShipId, Is.EqualTo("interceptor"));
        }

        [Test]
        public void More_purchases_wins_when_both_sides_have_earned_the_same()
        {
            // Lifetime scrap ties, because buying things does not earn anything. The
            // tie-break is who has bought more: a purchase cannot be undone, while a wallet
            // balance can be earned again.
            var local = Profile(lifetimeScrap: 3000, wallet: 3000);
            var remote = Profile(lifetimeScrap: 3000, wallet: 0,
                                 upgrades: new Dictionary<string, int> { ["damage"] = 5 });

            ProfileMerge.MergeInto(local, remote);

            Assert.That(local.metaUpgradeLevels["damage"], Is.EqualTo(5));
            Assert.That(local.wallet, Is.EqualTo(0));
        }

        // ── earned things only ever go up ──────────────────────────────────────────────

        [Test]
        public void Lifetime_counters_and_personal_bests_take_the_higher_of_the_two()
        {
            var local = Empty();
            local.lifetimeKills = 900;
            local.bestSurvivalSeconds = 400;
            local.runsPlayed = 10;
            local.bestLevel = 12;

            var remote = Empty();
            remote.lifetimeKills = 100;     // lower — must not pull local down
            remote.bestSurvivalSeconds = 950;
            remote.runsPlayed = 3;
            remote.bestKills = 77;
            remote.bossKills = 4;

            ProfileMerge.MergeInto(local, remote);

            Assert.That(local.lifetimeKills, Is.EqualTo(900), "a lower remote must not lower it");
            Assert.That(local.bestSurvivalSeconds, Is.EqualTo(950));
            Assert.That(local.runsPlayed, Is.EqualTo(10));
            Assert.That(local.bestLevel, Is.EqualTo(12));
            Assert.That(local.bestKills, Is.EqualTo(77));
            Assert.That(local.bossKills, Is.EqualTo(4));
        }

        [Test]
        public void Achievements_are_unioned_even_when_the_other_side_owns_the_economy()
        {
            // Achievements are earned, not bought, so granting one costs nothing and they are
            // never part of the economy block. A player who unlocked something on each device
            // must end up holding both.
            var local = Profile(lifetimeScrap: 10, wallet: 0);
            local.unlockedAchievementIds = new List<string> { "first_blood" };

            var remote = Profile(lifetimeScrap: 9000, wallet: 0);   // remote owns the economy
            remote.unlockedAchievementIds = new List<string> { "survivor_10min" };

            ProfileMerge.MergeInto(local, remote);

            Assert.That(local.unlockedAchievementIds,
                Is.EquivalentTo(new[] { "first_blood", "survivor_10min" }));
        }

        // ── the quiet properties ───────────────────────────────────────────────────────

        [Test]
        public void Two_identical_profiles_merge_to_no_change_at_all()
        {
            // An idle sync runs this constantly. Reporting a change would make the client
            // push, which makes the server answer, which merges again — churn with no cause.
            var local = Profile(lifetimeScrap: 1234, wallet: 56);
            var remote = Profile(lifetimeScrap: 1234, wallet: 56);

            Assert.That(ProfileMerge.MergeInto(local, remote), Is.False);
        }

        [Test]
        public void The_caller_s_own_instance_is_the_one_that_changes()
        {
            // HttpProfileStore hands this same object out of Load() and ProfileService holds
            // it for the whole session. Replacing it instead of mutating it would strand every
            // holder on a stale copy — the bug would look like "my scrap went back down".
            var local = Empty();
            var remote = Empty();
            remote.lifetimeScrap = 500;

            ProfileMerge.MergeInto(local, remote);

            Assert.That(local.lifetimeScrap, Is.EqualTo(500));
        }

        [Test]
        public void A_selected_ship_the_player_does_not_own_falls_back_to_one_they_do()
        {
            // Reachable when the economy block arrives from a side that never bought the hull
            // this device had selected. Left dangling it breaks the hangar.
            var local = Profile(lifetimeScrap: 1, wallet: 0, ships: new List<string> { "ghost" });
            local.selectedShipId = "ghost";

            var remote = Profile(lifetimeScrap: 9999, wallet: 0,
                                 ships: new List<string> { "starter" });
            remote.selectedShipId = "";

            ProfileMerge.MergeInto(local, remote);

            Assert.That(local.ownedShipIds, Is.EquivalentTo(new[] { "starter" }));
            Assert.That(local.selectedShipId, Is.EqualTo("starter"),
                "a selection that is not owned must be repaired, not left dangling");
        }

        [Test]
        public void The_remote_map_choice_is_adopted_only_when_this_device_has_none()
        {
            var fresh = Empty();
            var remote = Empty();
            remote.selectedMapId = "nebula";
            ProfileMerge.MergeInto(fresh, remote);
            Assert.That(fresh.selectedMapId, Is.EqualTo("nebula"));

            // A map is a preference, not progress: the choice made on this device wins.
            var chosen = Empty();
            chosen.selectedMapId = "milky_way";
            ProfileMerge.MergeInto(chosen, remote);
            Assert.That(chosen.selectedMapId, Is.EqualTo("milky_way"));
        }

        [Test]
        public void Collections_arriving_as_null_from_json_do_not_throw()
        {
            // Newtonsoft leaves a missing array null rather than empty, and an older save or a
            // hand-edited one can be missing any of these. A crash here happens at startup,
            // before the player can do anything about it.
            var local = Empty();
            local.metaUpgradeLevels = null;
            local.ownedShipIds = null;
            local.unlockedAchievementIds = null;

            var remote = Empty();
            remote.metaUpgradeLevels = null;
            remote.ownedShipIds = null;
            remote.unlockedAchievementIds = null;
            remote.selectedShipId = null;
            remote.selectedMapId = null;

            Assert.DoesNotThrow(() => ProfileMerge.MergeInto(local, remote));
            Assert.That(local.ownedShipIds, Is.Not.Null);
        }

        [Test]
        public void A_missing_profile_on_either_side_is_not_a_merge()
        {
            Assert.That(ProfileMerge.MergeInto(null, Empty()), Is.False);
            Assert.That(ProfileMerge.MergeInto(Empty(), null), Is.False);
        }
    }
}
