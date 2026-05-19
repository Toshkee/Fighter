using UnityEngine;
using SamuraiFighter.Characters;

namespace SamuraiFighter.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private Fighter _owner;
        [SerializeField] private Health _health;

        public Fighter Owner => _owner;
        public Health Health => _health;

        private void Reset()
        {
            _owner = GetComponentInParent<Fighter>();
            _health = GetComponentInParent<Health>();
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }
    }
}
