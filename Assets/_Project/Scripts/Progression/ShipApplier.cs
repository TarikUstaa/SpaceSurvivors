using SpaceSurvivors.Stats;
using UnityEngine;

namespace SpaceSurvivors.Progression
{
    /// <summary>
    /// Applies the ship the player bought in the hangar (M14b) at the start of a run: swaps
    /// the hull sprite and layers the ship's <see cref="ShipData.runStartModifiers"/> onto
    /// the <see cref="StatSheet"/>. Runs early, alongside <see cref="MetaUpgradeApplier"/>
    /// (both just add to the sheet, so order doesn't matter).
    /// </summary>
    [RequireComponent(typeof(StatSheet))]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class ShipApplier : MonoBehaviour
    {
        [SerializeField] private StatSheet _stats;
        [Tooltip("The hull renderer to re-skin. Empty = a SpriteRenderer on this GameObject.")]
        [SerializeField] private SpriteRenderer _hull;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<StatSheet>();
            if (_hull == null) _hull = GetComponent<SpriteRenderer>();

            var sprite = ShipService.SelectedSprite;
            if (sprite != null && _hull != null) _hull.sprite = sprite;

            _stats.AddModifiers(ShipService.BuildStartingModifiers());
        }
    }
}
