using System;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// What the server has said about this player's account that the menus need to show — today,
    /// that it is suspended, and why.
    ///
    /// <para>Set by <see cref="BackendSession"/> when the token exchange answers 403 with the code
    /// <c>account_suspended</c>. Lasts for the session: a suspension lifted in the backoffice takes
    /// effect at the next launch, which is also the next time the game asks.</para>
    /// </summary>
    public static class AccountStatus
    {
        /// <summary>Raised when the account's standing changes. On the main thread.</summary>
        public static event Action Changed;

        public static bool IsSuspended { get; private set; }

        /// <summary>The operator's reason, or null if none was given.</summary>
        public static string SuspensionReason { get; private set; }

        internal static void MarkSuspended(string reason)
        {
            IsSuspended = true;
            SuspensionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            Changed?.Invoke();
        }
    }
}
