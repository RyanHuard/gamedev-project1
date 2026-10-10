using UnityEngine;
using HollowTournament.Combat;
using HollowTournament.Boss;

namespace HollowTournament.Interactables
{
    /// <summary>
    /// Boss phase 3: the Horseman's own flaming head, thrown at the player.
    /// Same parry-the-projectile skill as PumpkinBomb, but a successful parry
    /// here sends it straight back into the Horseman and ends the fight --
    /// this is the finishing blow, so it bypasses normal armor/guard gating.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PumpkinHeadProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 11f;
        [SerializeField] private float directHitDamage = 20f;
        [SerializeField] private float parriedReturnDamage = 999f; // lethal: finishing move, not a normal hit

        private Rigidbody _rb;
        private Transform _player;
        private HorsemanBoss _boss;
        private bool _returning;
        private bool _resolved;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void Launch(Transform player, HorsemanBoss boss)
        {
            _player = player;
            _boss = boss;
            _rb.linearVelocity = (player.position - transform.position).normalized * speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_resolved) return;

            if (!_returning)
            {
                var guard = other.GetComponentInParent<GuardComponent>();
                if (guard != null && guard.IsInParryWindow)
                {
                    _returning = true;
                    _rb.linearVelocity = (_boss.transform.position - transform.position).normalized * speed * 1.3f;
                    return;
                }

                var player = other.GetComponentInParent<HollowTournament.Player.PlayerController>();
                if (player != null)
                {
                    player.TryReceiveDamage(new DamageInfo(directHitDamage, DamageType.Thrown, SwingDirection.None, _boss.gameObject));
                    Resolve();
                }
            }
            else
            {
                var boss = other.GetComponentInParent<HorsemanBoss>();
                if (boss != null)
                {
                    boss.TryReceiveDamage(new DamageInfo(parriedReturnDamage, DamageType.Riposte, SwingDirection.None, _player.gameObject));
                    Resolve();
                }
            }
        }

        private void Resolve()
        {
            _resolved = true;
            Destroy(gameObject, 0.1f);
        }
    }
}
