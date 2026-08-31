using UnityEngine;

namespace SpaceSurvivors.Data
{
    /// <summary>
    /// One playable map (M15). A map is purely a <b>backdrop theme</b> — the sky colour, the
    /// star tint, and a big parallax nebula / galaxy image drawn behind the starfield. The
    /// asteroid field, caches and hazards are the standard arena (owned by the
    /// <c>EnvironmentDirector</c>), the same on every map. Pure data, no logic
    /// (AI_Guidelines §3).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceSurvivors/Config/Map", fileName = "Map")]
    public class MapData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key stored in the profile. Never rename once shipped.")]
        public string id = "milky_way";
        public string displayName = "Milky Way";
        [TextArea] public string description = "";
        [Tooltip("Shown in the map-select screen. Falls back to the backdrop sprite / sky colour.")]
        public Sprite previewSprite;

        [Header("Backdrop")]
        [Tooltip("Solid colour the camera clears to (behind everything).")]
        public Color cameraBackground = new(0.04f, 0.05f, 0.09f);
        [Tooltip("Tint applied to the parallax starfield layers.")]
        public Color starfieldTint = new(0.75f, 0.82f, 1f);
        [Tooltip("Big seamless nebula / galaxy texture drawn as the farthest parallax layer. " +
                 "Null = just the tinted starfield.")]
        public Sprite backdropSprite;
        [Tooltip("Tint for the backdrop layer.")]
        public Color backdropTint = Color.white;
    }
}
