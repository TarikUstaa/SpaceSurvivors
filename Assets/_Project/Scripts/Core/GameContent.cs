using System;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Content the backoffice publishes for the menus — today, the one announcement.
    ///
    /// <para><b>Only when cloud sync is on.</b> <see cref="BackendConfig"/>'s rule is that nothing
    /// reaches the network until the player opts in, and a request for an announcement is still a
    /// request: it tells the server this machine's address and that the game was opened. With sync
    /// off the callback simply gets nothing.</para>
    ///
    /// <para>No token: the endpoint is public on purpose (an announcement is most needed when
    /// signing in is broken), so this uses <see cref="BackendRequest.SendRaw"/> directly and never
    /// waits on <see cref="BackendSession"/>. The long timeout is for the same cold start the token
    /// request allows for — this may well be the first call of the day.</para>
    /// </summary>
    public static class GameContent
    {
        public sealed class Announcement
        {
            public string message;
            public string level;

            public bool IsWarning => level == "warning";
        }

        /// <summary>Calls back with the current announcement, or null for "nothing to show".</summary>
        public static void FetchAnnouncement(Action<Announcement> onDone)
        {
            if (!BackendConfig.Enabled)
            {
                onDone(null);
                return;
            }

            BackendRequest.SendRaw(BackendConfig.AnnouncementUrl, "GET", null, null, (code, body) =>
            {
                // 204 is "nothing published"; anything but 200 is treated the same way. A menu
                // banner is not worth an error message.
                var announcement = code == 200 ? BackendRequest.Parse<Announcement>(body) : null;
                onDone(announcement != null && !string.IsNullOrWhiteSpace(announcement.message)
                    ? announcement
                    : null);
            }, BackendRequest.WakeTimeoutSeconds);
        }
    }
}
