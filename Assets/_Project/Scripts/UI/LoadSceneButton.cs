using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceSurvivors.UI
{
    /// <summary>
    /// Wires a <see cref="Button"/> to load a scene by name. A tiny reusable seam for menu
    /// navigation (Shop, and later Hangar / Achievements) so screens don't each grow a
    /// bespoke handler (AI_Guidelines §1).
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class LoadSceneButton : MonoBehaviour
    {
        [SerializeField] private string _sceneName = "MainMenu";

        private void Awake() => GetComponent<Button>().onClick.AddListener(Load);

        private void Load()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_sceneName);
        }
    }
}
