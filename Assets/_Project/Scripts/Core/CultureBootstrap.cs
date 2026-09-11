using System.Globalization;
using System.Threading;
using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Pins number and date formatting to the invariant culture for the whole game.
    ///
    /// <para><b>The problem.</b> Every <c>ToString("n0")</c>, every <c>$"{x:0.00}"</c> and every
    /// <c>float.Parse</c> in .NET uses the machine's culture unless told otherwise. On a Turkish
    /// system that means the decimal separator is a comma and the thousands separator is a dot,
    /// so <c>x1.50</c> renders as <c>x1,50</c> and <c>1,234</c> as <c>1.234</c> — while the
    /// labels beside them stay English, because this game's UI is English throughout. Worse, the
    /// same code then produces different output on the developer's machine and on a player's,
    /// which is the hardest kind of bug to be shown a screenshot of.</para>
    ///
    /// <para><b>Why one line here instead of 26 call sites.</b> There are about two dozen places
    /// that format a number for the screen, and threading <c>CultureInfo.InvariantCulture</c>
    /// through each one fixes those and nothing else: the twenty-seventh, written next month,
    /// starts the problem again. Setting the culture once removes the whole class, and it costs
    /// a single assignment at startup.</para>
    ///
    /// <para><b>What this is not.</b> It is not a decision that the game will never be
    /// translated. It is the decision that <em>formatting follows the UI language</em>, which is
    /// English today. When there are real translations, this is the one place that changes —
    /// the culture becomes the player's chosen language rather than invariant, and every screen
    /// follows automatically, exactly as it does now.</para>
    ///
    /// <para>Both properties are set on purpose: <see cref="CultureInfo.CurrentCulture"/> covers
    /// the thread already running, and <see cref="CultureInfo.DefaultThreadCurrentCulture"/>
    /// covers any thread created afterwards — a background <c>UnityWebRequest</c> callback
    /// would otherwise be free to disagree with the main thread.</para>
    /// </summary>
    public static class CultureBootstrap
    {
        /// <summary>
        /// Runs at <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/> — the earliest
        /// hook there is — so nothing else that boots at startup can format a number before the
        /// culture is settled. Re-running it is harmless, which matters because this project
        /// plays with Domain Reload disabled and every static boot hook fires again on each
        /// Play press.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Apply()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}
