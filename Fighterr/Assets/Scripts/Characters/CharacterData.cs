using UnityEngine;
using SamuraiFighter.Combat;

namespace SamuraiFighter.Characters
{
    [CreateAssetMenu(fileName = "Character", menuName = "Fighter/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;
        public Color tint = Color.white;

        [Header("Movement")]
        public float walkSpeed = 5f;
        public float jumpForce = 8f;

        [Header("Hitbox sizes (reach x, height y)")]
        public Vector2 lightHitboxSize = new Vector2(1.5f, 1f);
        public Vector2 heavyHitboxSize = new Vector2(1.9f, 1.1f);

        [Header("Moves")]
        public MoveData light;
        public MoveData heavy;
        public MoveData super;
        public MoveData fireball;
    }
}
