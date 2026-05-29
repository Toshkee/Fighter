using UnityEngine;
using SamuraiFighter.Characters;

namespace SamuraiFighter.Combat
{
    public enum MoveDelivery { Hitbox, Projectile }
    public enum HitboxSlot { Light, Heavy }

    [CreateAssetMenu(fileName = "Move", menuName = "Fighter/Move Data")]
    public class MoveData : ScriptableObject
    {
        [Header("Identity")]
        public string moveName;
        public AttackKind attackKind = AttackKind.Light;
        public MoveDelivery delivery = MoveDelivery.Hitbox;
        public HitboxSlot hitboxSlot = HitboxSlot.Light;

        [Header("Timing (FixedUpdate frames)")]
        public int startup = 5;
        public int active = 3;
        public int recovery = 10;

        [Header("Damage")]
        public int damage = 8;
        public float knockback = 7f;
        public int hitstopFrames = 7;

        [Header("Combo cancel (when chained from prior hit)")]
        public float comboDamageBonus = 1f;
        public int comboStartupReduction = 0;
        public int comboMinStartup = 2;

        [Header("Super")]
        public int superCost = 0;
        public float superFlashDuration = 0f;

        [Header("Projectile (when delivery = Projectile)")]
        public GameObject projectilePrefab;
    }
}
