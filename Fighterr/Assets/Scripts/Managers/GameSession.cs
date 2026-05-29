using UnityEngine;
using SamuraiFighter.Characters;

namespace SamuraiFighter.Managers
{
    /// <summary>
    /// Persistent match configuration that survives scene loads. The character-select
    /// screen writes the chosen fighters here; the Fight scene's <c>FightBootstrap</c>
    /// reads them and reconfigures the in-scene fighters at runtime. If no session
    /// exists (e.g. playing the Fight scene directly), the scene keeps its baked
    /// defaults — handy for quick testing.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public CharacterData P1Character;
        public CharacterData P2Character;
        public int RoundsToWin = 2;

        /// <summary>Set by the match at the end: 0 = draw, 1 = P1, 2 = P2.</summary>
        public int LastWinner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static GameSession GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("GameSession");
            return go.AddComponent<GameSession>();
        }
    }
}
