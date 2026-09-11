using SpaceSurvivors.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// On Start it wires every <see cref="Button"/> in the scene to play a click sound on
    /// press and a hover sound on pointer-enter, through the scene's
    /// <see cref="AudioDirector"/>. One per scene (usually on the AudioDirector object) —
    /// keeps the per-button boilerplate out of every screen (§1).
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonSfxInstaller : MonoBehaviour
    {
        [SerializeField] private AudioDirector _audio;

        private void Start()
        {
            if (_audio == null) _audio = FindAnyObjectByType<AudioDirector>();
            if (_audio == null) return;

            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                var b = button;
                b.onClick.AddListener(() => _audio.Play(SfxId.UiClick));

                var trigger = b.GetComponent<EventTrigger>() ?? b.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener(_ => { if (b.interactable) _audio.Play(SfxId.UiHover); });
                trigger.triggers.Add(entry);
            }
        }
    }
}
