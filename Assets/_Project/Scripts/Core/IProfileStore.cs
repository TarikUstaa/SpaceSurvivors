using System;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Persistence seam for the <see cref="PlayerProfile"/>. M13 ships
    /// <see cref="LocalJsonProfileStore"/>; a backend implementation drops in later behind
    /// this same interface without touching gameplay code (Project_Goals §8). Tests use a
    /// fake in-memory store.
    ///
    /// Both methods are synchronous by contract: a remote store is expected to be
    /// <b>cache-first</b> — <see cref="Load"/> returns the last known profile immediately and
    /// refreshes in the background; <see cref="Save"/> writes the local cache now and pushes
    /// in the background. Sync health is reported out of band via
    /// <see cref="IRemoteProfileStore"/> rather than by blocking or throwing here.
    /// </summary>
    public interface IProfileStore
    {
        /// <summary>Return the stored profile, or a fresh one if nothing is saved / it is unreadable.</summary>
        PlayerProfile Load();

        /// <summary>Persist the given profile. Never throws — failures surface through sync status.</summary>
        void Save(PlayerProfile profile);
    }

    /// <summary>Health of a remote profile store's link to the backend.</summary>
    public enum ProfileSyncStatus
    {
        /// <summary>Local and remote agree (also the permanent state of a purely local store).</summary>
        Synced,
        /// <summary>A push or pull is in flight.</summary>
        Syncing,
        /// <summary>Working from the local cache; changes are queued for when the link returns.</summary>
        Offline,
        /// <summary>The backend rejected the last sync (auth expired, conflict, server error).</summary>
        Error,
    }

    /// <summary>
    /// Optional capability a networked <see cref="IProfileStore"/> adds so the UI can show an
    /// offline / syncing / error indicator and the player can force a retry. A local store
    /// does not implement this; <see cref="ProfileService"/> treats its absence as always
    /// <see cref="ProfileSyncStatus.Synced"/>.
    /// </summary>
    public interface IRemoteProfileStore : IProfileStore
    {
        ProfileSyncStatus SyncStatus { get; }

        /// <summary>Raised whenever <see cref="SyncStatus"/> changes.</summary>
        event Action<ProfileSyncStatus> SyncStatusChanged;

        /// <summary>Ask the store to reconcile local and remote now (e.g. a "retry" button).</summary>
        void ForceSync();
    }
}
