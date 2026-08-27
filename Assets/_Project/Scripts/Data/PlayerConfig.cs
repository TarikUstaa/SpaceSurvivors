using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// Immutable tuning data for the player ship. Lives as a ScriptableObject asset so
    /// designers can tweak feel without touching code (AI_Guidelines §3 — no magic numbers).
    /// Runtime state (velocity, cooldowns) belongs on the components, never on this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Target top speed in world units / second.")]
        [Min(0f)] public float moveSpeed = 6f;

        [Tooltip("How quickly the ship reaches top speed. Higher = snappier.")]
        [Min(0.01f)] public float acceleration = 40f;

        [Tooltip("How quickly the ship stops when there is no input. Higher = less drift.")]
        [Min(0.01f)] public float deceleration = 25f;

        [Header("Facing (used by ShipRotator, optional component)")]
        [Tooltip("Degrees/second the hull turns to face its travel direction. 0 = snap instantly.")]
        [Min(0f)] public float turnSpeed = 540f;

        [Tooltip("Sprite's forward axis at 0° rotation. Kenney ships point UP, so this is 90.")]
        public float spriteForwardOffsetDegrees = 90f;

        [Tooltip("Below this speed the ship keeps its last heading instead of spinning at a stop.")]
        [Min(0f)] public float minSpeedToTurn = 0.05f;

        [Header("Bounds")]
        [Tooltip("If true, the ship is clamped to the camera view (minus this margin).")]
        public bool clampToScreen = true;

        [Tooltip("World-unit padding kept between the ship and the screen edge.")]
        [Min(0f)] public float screenEdgePadding = 0.5f;
    }
}
