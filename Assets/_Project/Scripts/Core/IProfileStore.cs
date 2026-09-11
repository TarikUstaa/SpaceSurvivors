using System;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Persistence seam for the <see cref="PlayerProfile"/>.
    ///
    /// Two implementations exist: <see cref="LocalJsonProfileStore"/> (a JSON file on this
    /// device) and <see cref="HttpProfileStore"/> (the deployed backend, with that same local
    /// file as its cache). <see cref="BackendBootstrap"/> installs one at startup and no
    /// gameplay code learns which.
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

        /// <summary>
        /// Raised when a background sync folded remote data into the live profile. The store
        /// mutates the instance it handed out from <see cref="IProfileStore.Load"/> <i>in
        /// place</i> — callers hold that reference, so replacing it would leave them on a stale
        /// copy — and then fires this so the UI redraws. <see cref="ProfileService"/> forwards
        /// it as its own <c>Changed</c> event.
        /// </summary>
        event Action ProfileRefreshed;

        /// <summary>Ask the store to reconcile local and remote now (e.g. a "retry" button).</summary>
        void ForceSync();
    }
}
