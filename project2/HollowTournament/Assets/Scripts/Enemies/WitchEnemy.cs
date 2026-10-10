using UnityEngine;
using HollowTournament.Combat;
using HollowTournament.Interactables;

namespace HollowTournament.Enemies
{
    /// <summary>
    /// Armory beat support cast: no FSM of its own (only player/skeleton/Horseman
    /// are specced as FSMs), just a timed pumpkin-bomb lobber that gives the
    /// player something to parry while they're learning to riposte the armored skeleton.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class WitchEnemy : MonoBehaviour, IDamageable
    {
        [SerializeField] private GameObject pumpkinBombPrefab;
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private Transform player;
        [SerializeField] private float throwIntervalSeconds = 3f;

        private HealthComponent _health;
        private float _timer;

        private void Awake() => _health = GetComponent<HealthComponent>();

        private void Update()
        {
            if (!_health.IsAlive || player == null) return;
            _timer += Time.deltaTime;
            if (_timer >= throwIntervalSeconds)
            {
                _timer = 0f;
                ThrowBomb();
            }
        }

        private void ThrowBomb()
        {
            if (pumpkinBombPrefab == null || throwOrigin == null) return;
            GameObject bombGO = Instantiate(pumpkinBombPrefab, throwOrigin.position, throwOrigin.rotation);
            bombGO.GetComponent<PumpkinBomb>()?.Launch(player, gameObject);
        }

        public bool TryReceiveDamage(DamageInfo info)
        {
            if (!_health.IsAlive) return false;
            _health.ApplyDamage(info);
            return false;
        }

        public bool IsAlive => _health.IsAlive;
    }
}
