using UnityEngine;
using HollowTournament.Combat;

namespace HollowTournament.Enemies
{
    /// <summary>
    /// Courtyard beat: a stationary scarecrow dummy with armor on one side.
    /// Doesn't move or attack -- it exists purely to teach "read the open
    /// side, swing there" before any threat is involved. Rotates its guarded
    /// side on a timer so the player can't just memorize one direction.
    /// </summary>
    [RequireComponent(typeof(GuardComponent), typeof(StaminaSystem), typeof(HealthComponent))]
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float rotateIntervalSeconds = 4f;
        [SerializeField] private GuardSide guardSide = GuardSide.Left;

        private HealthComponent _health;
        private float _timer;

        private static readonly GuardSide[] Sides = { GuardSide.Left, GuardSide.Right, GuardSide.Overhead };

        private void Awake() => _health = GetComponent<HealthComponent>();

        private void Update()
        {
            if (!_health.IsAlive) return;
            _timer += Time.deltaTime;
            if (_timer >= rotateIntervalSeconds)
            {
                _timer = 0f;
                guardSide = Sides[(System.Array.IndexOf(Sides, guardSide) + 1) % Sides.Length];
            }
        }

        public bool TryReceiveDamage(DamageInfo info)
        {
            if (!_health.IsAlive) return false;

            bool hitOpenSide = guardSide switch
            {
                GuardSide.Left => info.Direction != SwingDirection.Left,
                GuardSide.Right => info.Direction != SwingDirection.Right,
                GuardSide.Overhead => info.Direction != SwingDirection.Overhead,
                _ => true
            };

            if (!hitOpenSide) return false; // clangs off the armored side, no damage

            _health.ApplyDamage(info);
            return false;
        }

        public bool IsAlive => _health.IsAlive;
    }
}
