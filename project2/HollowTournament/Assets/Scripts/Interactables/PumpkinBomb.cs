using UnityEngine;
using HollowTournament.Combat;

namespace HollowTournament.Interactables
{
    /// <summary>
    /// Armory beat: the witch lobs this at the player. If the player starts a
    /// block within the parry window right before impact, the bomb reverses
    /// course and flies back at the witch instead of exploding on the player
    /// -- the same parry skill taught on the armored skeleton, reused on a
    /// projectile instead of a melee swing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PumpkinBomb : MonoBehaviour
    {
        [SerializeField] private float speed = 9f;
        [SerializeField] private float explosionDamage = 25f;
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private GameObject explosionVfxPrefab;
        [SerializeField] private LayerMask damageableLayers = ~0;

        private Rigidbody _rb;
        private Transform _currentTarget;
        private GameObject _thrower;
        private bool _hasExploded;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void Launch(Transform target, GameObject thrower)
        {
            _currentTarget = target;
            _thrower = thrower;
            AimAt(target.position);
        }

        private void AimAt(Vector3 point)
        {
            Vector3 dir = (point - transform.position).normalized;
            _rb.linearVelocity = dir * speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasExploded) return;

            var guard = other.GetComponentInParent<GuardComponent>();
            if (guard != null && guard.IsInParryWindow && other.gameObject != _thrower)
            {
                // Redirect back at whoever threw it.
                if (_thrower != null)
                {
                    AimAt(_thrower.transform.position);
                    _thrower = other.gameObject; // the player is now the "thrower" for a second bounce
                }
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Explode();
            }
        }

        private void Explode()
        {
            _hasExploded = true;
            if (explosionVfxPrefab != null)
                Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);

            var hits = Physics.OverlapSphere(transform.position, explosionRadius, damageableLayers);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;
                var info = new DamageInfo(explosionDamage, DamageType.Thrown, SwingDirection.None, _thrower);
                damageable.TryReceiveDamage(info);
            }
            Destroy(gameObject);
        }
    }
}
