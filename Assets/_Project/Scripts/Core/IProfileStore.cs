namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Persistence seam for the <see cref="PlayerProfile"/>. M13 ships
    /// <see cref="LocalJsonProfileStore"/>; a backend implementation drops in later behind
    /// this same interface without touching gameplay code (Project_Goals §8). Tests use a
    /// fake in-memory store.
    /// </summary>
    public interface IProfileStore
    {
        /// <summary>Return the stored profile, or a fresh one if nothing is saved / it is unreadable.</summary>
        PlayerProfile Load();

        /// <summary>Persist the given profile.</summary>
        void Save(PlayerProfile profile);
    }
}
