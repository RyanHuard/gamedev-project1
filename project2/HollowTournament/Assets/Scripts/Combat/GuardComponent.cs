using UnityEngine;

namespace HollowTournament.Combat
{
    public enum GuardResult
    {
        NotBlocked,
        Blocked,
        Parried
    }

    /// <summary>
    /// Shared block/parry logic used by the player and every enemy that can guard.
    /// A block becomes a parry only if ResolveIncomingHit is called within
    /// parryWindowSeconds of StartBlocking(), matching the "hold right-click
    /// in the ~0.2s before the hit lands" design.
    /// </summary>
    [RequireComponent(typeof(StaminaSystem))]
    public class GuardComponent : MonoBehaviour
    {
        [SerializeField] private float parryWindowSeconds = 0.2f;
        [SerializeField] private float blockStaminaCost = 20f;
        [SerializeField] private float blockHoldDrainPerSecond = 5f;

        public bool IsBlocking { get; private set; }
        public bool IsInParryWindow => IsBlocking && _timeSinceBlockStarted <= parryWindowSeconds;

        public System.Action OnParrySuccess;
        public System.Action OnBlockSuccess;
        public System.Action OnGuardBroken;

        private StaminaSystem _stamina;
        private float _timeSinceBlockStarted;

        private void Awake() => _stamina = GetComponent<StaminaSystem>();

        private void Update()
        {
            if (IsBlocking)
            {
                _timeSinceBlockStarted += Time.deltaTime;
                _stamina.Drain(blockHoldDrainPerSecond * Time.deltaTime);
                if (_stamina.IsExhausted) BreakGuard();
            }
        }

        public void StartBlocking()
        {
            if (IsBlocking || _stamina.IsExhausted) return;
            IsBlocking = true;
            _timeSinceBlockStarted = 0f;
        }

        public void StopBlocking()
        {
            IsBlocking = false;
        }

        /// <summary>Called by the attacker's hitbox resolution (via IDamageable) to see how the hit is absorbed.</summary>
        public GuardResult ResolveIncomingHit(SwingDirection incomingDirection, GuardSide currentGuardSide)
        {
            if (!IsBlocking) return GuardResult.NotBlocked;

            bool facingRightSide = currentGuardSide == GuardSide.None || DirectionMatchesGuard(incomingDirection, currentGuardSide);

            if (IsInParryWindow && facingRightSide)
            {
                OnParrySuccess?.Invoke();
                return GuardResult.Parried;
            }

            if (!_stamina.TrySpend(blockStaminaCost))
            {
                BreakGuard();
                return GuardResult.NotBlocked;
            }

            OnBlockSuccess?.Invoke();
            return GuardResult.Blocked;
        }

        public void BreakGuard()
        {
            IsBlocking = false;
            OnGuardBroken?.Invoke();
        }

        /// <summary>Kick calls this directly: it ignores block entirely and forces a stagger opening.</summary>
        public void ForceBreak() => BreakGuard();

        private static bool DirectionMatchesGuard(SwingDirection direction, GuardSide guardSide)
        {
            return direction switch
            {
                SwingDirection.Left => guardSide == GuardSide.Left,
                SwingDirection.Right => guardSide == GuardSide.Right,
                SwingDirection.Overhead => guardSide == GuardSide.Overhead,
                _ => false
            };
        }
    }
}
