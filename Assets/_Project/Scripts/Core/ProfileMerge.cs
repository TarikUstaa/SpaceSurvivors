using System;
using System.Collections.Generic;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Reconciles two versions of one player's profile — the local save and whatever the server
    /// holds — when they have diverged (the player played offline, or on a second device).
    ///
    /// <para>There is no perfect answer without a full history of every transaction, so the
    /// rules below are chosen to fail in the least damaging direction: <b>never lose progress,
    /// never invent currency, never undo a purchase.</b></para>
    ///
    /// <list type="number">
    /// <item><b>Earned, monotonic values</b> (lifetime counters, personal bests, achievements)
    /// only ever go up, so the higher value — or the union of the two sets — is true on both
    /// sides. Taking the max can't be wrong.</item>
    ///
    /// <item><b>Bought things move together with the wallet.</b> Spending debits the wallet and
    /// grants an upgrade or a ship. If we took the higher wallet <i>and</i> the union of
    /// purchases, every purchase made offline would become free. So the wallet,
    /// <see cref="PlayerProfile.metaUpgradeLevels"/> and <see cref="PlayerProfile.ownedShipIds"/>
    /// are copied as one block from whichever side is further along economically.</item>
    /// </list>
    ///
    /// <para>Achievements are unioned rather than blocked with the purchases because they are
    /// earned, not bought — granting one costs nothing.</para>
    /// </summary>
    public static class ProfileMerge
    {
        /// <summary>
        /// Fold <paramref name="remote"/> into <paramref name="local"/>, mutating
        /// <paramref name="local"/> in place (callers hold that reference — see
        /// <see cref="IRemoteProfileStore.ProfileRefreshed"/>).
        /// </summary>
        /// <returns>True if anything in <paramref name="local"/> actually changed, i.e. the
        /// merged result differs from what the caller had and is worth pushing back.</returns>
        public static bool MergeInto(PlayerProfile local, PlayerProfile remote)
        {
            if (local == null || remote == null) return false;

            Normalise(local);
            Normalise(remote);

            // Decide the economy winner BEFORE touching any field, or the comparison would be
            // reading values we have already merged.
            bool remoteOwnsEconomy = IsFurtherAlong(remote, local);
            bool changed = false;

            // ---- earned, monotonic: max / union is always safe ----
            changed |= Raise(ref local.lifetimeScrap, remote.lifetimeScrap);
            changed |= Raise(ref local.lifetimeKills, remote.lifetimeKills);
            changed |= Raise(ref local.runsPlayed, remote.runsPlayed);
            changed |= Raise(ref local.bestKills, remote.bestKills);
            changed |= Raise(ref local.bestSurvivalSeconds, remote.bestSurvivalSeconds);
            changed |= Raise(ref local.bestLevel, remote.bestLevel);
            changed |= Raise(ref local.bossKills, remote.bossKills);
            changed |= UnionInto(local.unlockedAchievementIds, remote.unlockedAchievementIds);

            // ---- bought: wallet + upgrades + ships come from one side, together ----
            if (remoteOwnsEconomy)
            {
                local.wallet = remote.wallet;
                local.metaUpgradeLevels = new Dictionary<string, int>(remote.metaUpgradeLevels);
                local.ownedShipIds = new List<string>(remote.ownedShipIds);
                local.selectedShipId = remote.selectedShipId;
                changed = true;
            }

            // ---- preference: only adopt the remote pick if we have none ----
            if (string.IsNullOrEmpty(local.selectedMapId) && !string.IsNullOrEmpty(remote.selectedMapId))
            {
                local.selectedMapId = remote.selectedMapId;
                changed = true;
            }

            // A selected ship we no longer own would break the hangar; fall back to the first.
            if (!string.IsNullOrEmpty(local.selectedShipId)
                && !local.ownedShipIds.Contains(local.selectedShipId))
            {
                local.selectedShipId = local.ownedShipIds.Count > 0 ? local.ownedShipIds[0] : "";
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Is <paramref name="candidate"/> economically ahead of <paramref name="incumbent"/>?
        /// Total scrap ever earned is the primary measure of how much has been played. When
        /// that ties — buying does not change it — the side holding more purchases wins,
        /// because a purchase cannot be undone but a wallet balance can be re-earned. A full
        /// tie keeps the incumbent, so an idle sync never churns the save.
        /// </summary>
        private static bool IsFurtherAlong(PlayerProfile candidate, PlayerProfile incumbent)
        {
            if (candidate.lifetimeScrap != incumbent.lifetimeScrap)
                return candidate.lifetimeScrap > incumbent.lifetimeScrap;

            return PurchaseWeight(candidate) > PurchaseWeight(incumbent);
        }

        /// <summary>Rough "how much has been bought" measure — total upgrade levels plus ships.</summary>
        private static int PurchaseWeight(PlayerProfile profile)
        {
            int total = profile.ownedShipIds.Count;
            foreach (var level in profile.metaUpgradeLevels.Values) total += level;
            return total;
        }

        private static bool Raise(ref long field, long candidate)
        {
            if (candidate <= field) return false;
            field = candidate;
            return true;
        }

        private static bool Raise(ref int field, int candidate)
        {
            if (candidate <= field) return false;
            field = candidate;
            return true;
        }

        /// <summary>Add every missing id from <paramref name="source"/> to <paramref name="target"/>.</summary>
        private static bool UnionInto(List<string> target, List<string> source)
        {
            bool changed = false;
            foreach (var id in source)
            {
                if (string.IsNullOrEmpty(id) || target.Contains(id)) continue;
                target.Add(id);
                changed = true;
            }
            return changed;
        }

        /// <summary>Collections arriving from JSON can be null; callers below assume they are not.</summary>
        private static void Normalise(PlayerProfile profile)
        {
            profile.metaUpgradeLevels ??= new Dictionary<string, int>();
            profile.ownedShipIds ??= new List<string>();
            profile.unlockedAchievementIds ??= new List<string>();
            profile.userId ??= "";
            profile.selectedShipId ??= "";
            profile.selectedMapId ??= "";
        }
    }
}
