using UnityEngine;

namespace SpaceSurvivors.Environment
{
    /// <summary>
    /// Builds the <see cref="ContactFilter2D"/> that area-damage queries need.
    ///
    /// <para>It exists for one reason, and it is the kind of thing that is silently wrong
    /// otherwise. Unity 6 deprecated <c>Physics2D.OverlapCircleNonAlloc</c> and friends in
    /// favour of overloads that take a filter instead of a bare layer mask — and the two do not
    /// default the same way. The old call obeyed the project-wide <b>Queries Hit Triggers</b>
    /// setting, which is on in this project; a freshly constructed <see cref="ContactFilter2D"/>
    /// has <see cref="ContactFilter2D.useTriggers"/> <b>false</b>.</para>
    ///
    /// <para>So the obvious port — swap the method name, pass <c>new ContactFilter2D()</c> —
    /// quietly stops hitting every trigger collider in the game. Hazards would keep drawing
    /// their circle, keep ticking, and damage a subset of what they used to, with no error and
    /// nothing in the console. Reading <see cref="Physics2D.queriesHitTriggers"/> here keeps the
    /// two in step, including if that project setting is ever changed.</para>
    ///
    /// <para>One helper rather than the same three lines in three files, because the next area
    /// effect will need it too and this is not a subtlety worth rediscovering.</para>
    /// </summary>
    internal static class OverlapFilter
    {
        /// <summary>A filter matching <paramref name="layers"/>, otherwise behaving exactly as
        /// the layer-mask overloads it replaces.</summary>
        internal static ContactFilter2D For(LayerMask layers)
        {
            var filter = new ContactFilter2D
            {
                // Matches what the deprecated *NonAlloc calls did: obey the project setting.
                useTriggers = Physics2D.queriesHitTriggers,
            };
            // Sets the mask and turns mask filtering on; assigning layerMask alone would not.
            filter.SetLayerMask(layers);
            return filter;
        }
    }
}
