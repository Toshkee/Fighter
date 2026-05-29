using UnityEngine;
using SamuraiFighter.Characters;
using SamuraiFighter.Managers;

namespace SamuraiFighter.Match
{
    /// <summary>
    /// At scene load, applies the characters chosen on the select screen (carried in
    /// <see cref="GameSession"/>) to the two in-scene fighters. With no session present
    /// the scene keeps whatever the builder baked in, so the Fight scene stays
    /// playable on its own for quick testing.
    /// </summary>
    public class FightBootstrap : MonoBehaviour
    {
        [SerializeField] private Fighter _p1;
        [SerializeField] private Fighter _p2;
        [SerializeField] private SpriteRenderer _p1Renderer;
        [SerializeField] private SpriteRenderer _p2Renderer;
        [SerializeField] private MatchController _match;

        private void Awake()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            Apply(_p1, _p1Renderer, session.P1Character);
            Apply(_p2, _p2Renderer, session.P2Character);
            if (_match != null) _match.SetRoundsToWin(session.RoundsToWin);
        }

        private static void Apply(Fighter fighter, SpriteRenderer renderer, CharacterData c)
        {
            if (fighter == null || c == null) return;
            fighter.AssignMoves(c.light, c.heavy, c.super, c.fireball);
            fighter.SetMovement(c.walkSpeed, c.jumpForce);
            fighter.SetHitboxSizes(c.lightHitboxSize, c.heavyHitboxSize);
            if (renderer != null) renderer.color = c.tint;
        }

        public void Configure(Fighter p1, Fighter p2, SpriteRenderer p1Renderer, SpriteRenderer p2Renderer, MatchController match)
        {
            _p1 = p1; _p2 = p2;
            _p1Renderer = p1Renderer; _p2Renderer = p2Renderer;
            _match = match;
        }
    }
}
