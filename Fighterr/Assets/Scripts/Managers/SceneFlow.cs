using UnityEngine;
using UnityEngine.SceneManagement;
using SamuraiFighter.Utils;

namespace SamuraiFighter.Managers
{
    /// <summary>Central place for scene names and transitions. Resets timescale and plays a UI blip.</summary>
    public static class SceneFlow
    {
        public const string MainMenu = "MainMenu";
        public const string CharacterSelect = "CharacterSelect";
        public const string Fight = "Fight";
        public const string Result = "Result";

        public static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            AudioManager.Play(SfxId.UiConfirm);
            SceneManager.LoadScene(sceneName);
        }
    }
}
