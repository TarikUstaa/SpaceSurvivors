using System.Text;
using SpaceSurvivors.Core;
using SpaceSurvivors.Progression;
using SpaceSurvivors.Stats;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// The ship hangar (M14b): a one-at-a-time carousel over the ship catalogue. Prev/Next
    /// cycle ships; the action button is BUY / EQUIP / EQUIPPED depending on ownership and
    /// selection. All logic lives in <see cref="ShipService"/>; this only reads it and pushes
    /// text, prefab-style (AI_Guidelines §7). Built by <c>HangarBuilder</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class HangarScreen : MonoBehaviour
    {
        [SerializeField] private Text _walletLabel;
        [SerializeField] private Image _shipImage;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _descLabel;
        [SerializeField] private Text _statsLabel;

        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _actionButton;
        [SerializeField] private Text _actionLabel;
        [Tooltip("Scrap chip icon shown only when the action is a purchase.")]
        [SerializeField] private GameObject _actionChip;

        [SerializeField] private Button _backButton;
        [SerializeField] private string _menuSceneName = "MainMenu";

        private int _index;

        private void Awake()
        {
            EnsureEventSystem();

            if (_prevButton != null) _prevButton.onClick.AddListener(() => Step(-1));
            if (_nextButton != null) _nextButton.onClick.AddListener(() => Step(+1));
            if (_actionButton != null) _actionButton.onClick.AddListener(DoAction);
            if (_backButton != null) _backButton.onClick.AddListener(() => SceneManager.LoadScene(_menuSceneName));

            ShipService.Changed += Refresh;
            ProfileService.Changed += Refresh;
        }

        private void OnDestroy()
        {
            ShipService.Changed -= Refresh;
            ProfileService.Changed -= Refresh;
        }

        private void Start()
        {
            _index = Mathf.Max(0, IndexOf(ShipService.SelectedId));
            Refresh();
        }

        private static int IndexOf(string id)
        {
            var ships = ShipService.Ships;
            for (int i = 0; i < ships.Count; i++)
                if (ships[i] != null && ships[i].id == id) return i;
            return 0;
        }

        private void Step(int dir)
        {
            int n = ShipService.Ships.Count;
            if (n == 0) return;
            _index = ((_index + dir) % n + n) % n;
            Refresh();
        }

        private void DoAction()
        {
            var ship = Current();
            if (ship == null) return;
            if (!ShipService.IsOwned(ship.id)) ShipService.TryBuy(ship);
            else ShipService.Select(ship);
        }

        private SpaceSurvivors.Data.ShipData Current()
        {
            var ships = ShipService.Ships;
            return ships.Count > 0 ? ships[Mathf.Clamp(_index, 0, ships.Count - 1)] : null;
        }

        private void Refresh()
        {
            if (_walletLabel != null) _walletLabel.text = $"SCRAP  {ProfileService.Wallet:n0}";

            var ship = Current();
            if (ship == null) return;

            if (_shipImage != null)
            {
                _shipImage.sprite = ship.sprite;
                _shipImage.preserveAspect = true;
                _shipImage.enabled = ship.sprite != null;
            }
            if (_nameLabel != null) _nameLabel.text = ship.displayName;
            if (_descLabel != null) _descLabel.text = ship.description;
            if (_statsLabel != null) _statsLabel.text = FormatModifiers(ship.runStartModifiers);

            bool owned = ShipService.IsOwned(ship.id);
            bool selected = ShipService.SelectedId == ship.id;

            if (_actionChip != null) _actionChip.SetActive(!owned);
            if (_actionLabel != null)
                _actionLabel.text = !owned ? $"{ship.cost:n0}" : selected ? "EQUIPPED" : "EQUIP";
            if (_actionButton != null)
                _actionButton.interactable = !owned ? ShipService.CanAfford(ship) : !selected;

            bool many = ShipService.Ships.Count > 1;
            if (_prevButton != null) _prevButton.interactable = many;
            if (_nextButton != null) _nextButton.interactable = many;
        }

        private static string FormatModifiers(StatModifier[] mods)
        {
            if (mods == null || mods.Length == 0) return "Balanced — no bonuses or penalties.";

            var sb = new StringBuilder();
            foreach (var m in mods)
            {
                if (sb.Length > 0) sb.Append('\n');
                string name = Label(m.stat);
                if (m.op == ModifierOp.Flat)
                    sb.Append($"{(m.value >= 0 ? "+" : "")}{m.value:0.#} {name}");
                else
                    sb.Append($"{(m.value >= 0 ? "+" : "")}{m.value * 100f:0.#}% {name}");
            }
            return sb.ToString();
        }

        private static string Label(StatId stat) => stat switch
        {
            StatId.MoveSpeed => "Speed",
            StatId.MaxHealth => "Hull",
            StatId.Damage => "Damage",
            StatId.FireRate => "Fire Rate",
            StatId.ProjectileCount => "Projectiles",
            StatId.ProjectileSpeed => "Shot Speed",
            StatId.ProjectilePierce => "Pierce",
            StatId.PickupRadius => "Pickup Range",
            StatId.XpGain => "XP Gain",
            StatId.DamageResist => "Armour",
            StatId.ShieldCharges => "Shield",
            _ => stat.ToString(),
        };

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
