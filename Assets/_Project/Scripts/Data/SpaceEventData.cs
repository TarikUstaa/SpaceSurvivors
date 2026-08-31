using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// One arena-wide "space event" (M18) — a meteor shower, an ion storm, a derelict convoy…
    /// Pure data: the <c>EventDirector</c> reads these to decide what fires and when, then
    /// spawns <see cref="eventPrefab"/> (which carries the one component that runs the event).
    /// No logic here (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Space Event", fileName = "SpaceEvent")]
    public class SpaceEventData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key. Also matched against MapData.signatureEventId for the per-map bias.")]
        public string id = "meteor_shower";
        public string displayName = "Meteor Shower";
        [Tooltip("Shown in the announcement banner when the event starts.")]
        public string announce = "Meteor shower incoming";

        [Header("Scheduling")]
        [Tooltip("Relative likelihood vs the other events in the catalogue.")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Earliest run time (seconds survived) this event may fire.")]
        [Min(0f)] public float earliestTime = 45f;
        [Tooltip("How long the event runs before it cleans itself up.")]
        [Min(1f)] public float duration = 20f;

        [Header("Prefab")]
        [Tooltip("Spawned once when the event starts. Must have a component implementing ISpaceEvent.")]
        public GameObject eventPrefab;
    }
}
