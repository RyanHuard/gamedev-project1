using UnityEngine;
using HollowTournament.Core;
using HollowTournament.Combat;

namespace HollowTournament.Boss
{
    public enum HorsemanPhase
    {
        Mounted,
        Dismounted,
        HeadThrow,
        Defeated
    }

    /// <summary>
    /// Owner for the Headless Horseman boss FSM:
    /// Mounted (Charge &lt;-&gt; Turn) -&gt; Dismounted (duel FSM + Feint)
    /// -&gt; HeadThrow phase -&gt; Defeated.
    /// Phase advances are one-way and triggered by gameplay events
    /// (successful lance parry, Dismounted health threshold, head catch)
    /// rather than by health alone, so each phase reads as a distinct joust/duel/finisher beat.
    /// </summary>
    [RequireComponent(typeof(GuardComponent), typeof(StaminaSystem), typeof(HealthComponent))]
    public class HorsemanBoss : MonoBehaviour, IDamageable
    {
        [Header("Phase thresholds")]
        [Range(0f, 1f)] public float dismountedHealthFractionToStartHeadThrow = 0.33f;

        [Header("Mounted / Charge")]
        public float chargeSpeed = 12f;
        public float chargeLanceDamage = 30f;
        public float turnDuration = 1.2f;
        public float chargeRunUpDistance = 20f;

        [Header("Dismounted duel")]
        public float windUpDuration = 0.4f;
        public float attackDuration = 0.25f;
        public float recoverDuration = 0.35f;
        public float meleeDamage = 18f;
        public float feintChance = 0.25f;
        public float staggerDuration = 1.0f;

        [Header("Head throw")]
        public GameObject pumpkinHeadProjectilePrefab;
        public Transform headThrowOrigin;
        public float headThrowCooldown = 2.5f;

        [Header("Refs")]
        public Animator animator;
        public WeaponHitbox lanceHitbox;
        public WeaponHitbox meleeHitbox;
        public Transform player;
        public Transform chargeStartPoint;
        public Transform chargeEndPoint;

        public HorsemanPhase Phase { get; private set; } = HorsemanPhase.Mounted;
        public GuardComponent Guard { get; private set; }
        public StaminaSystem Stamina { get; private set; }
        public HealthComponent Health { get; private set; }
        public StateMachine<HorsemanBoss> Machine { get; private set; }

        public System.Action<HorsemanPhase> OnPhaseChanged;

        private void Awake()
        {
            Guard = GetComponent<GuardComponent>();
            Stamina = GetComponent<StaminaSystem>();
            Health = GetComponent<HealthComponent>();

            Health.OnDied += () => Machine.ChangeState<Boss.Horseman.Defeated>();

            Machine = new StateMachine<HorsemanBoss>(this);
            Machine.Register(new Boss.Horseman.Charge());
            Machine.Register(new Boss.Horseman.Turn());
            Machine.Register(new Boss.Horseman.Unhorsed());
            Machine.Register(new Boss.Horseman.DuelWindUp());
            Machine.Register(new Boss.Horseman.DuelAttack());
            Machine.Register(new Boss.Horseman.DuelRecover());
            Machine.Register(new Boss.Horseman.Feint());
            Machine.Register(new Boss.Horseman.DuelBlock());
            Machine.Register(new Boss.Horseman.Staggered());
            Machine.Register(new Boss.Horseman.HeadThrowWindUp());
            Machine.Register(new Boss.Horseman.HeadThrowRecover());
            Machine.Register(new Boss.Horseman.Defeated());
        }

        private void Start() => Machine.ChangeState<Boss.Horseman.Charge>();

        private void Update() => Machine.Tick(Time.deltaTime);

        public void SetPhase(HorsemanPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        /// <summary>Called by the lance's own parry resolution (not TryReceiveDamage) the moment a charge is parried.</summary>
        public void Unhorse()
        {
            Machine.ChangeState<Boss.Horseman.Unhorsed>();
        }

        public bool ShouldEnterHeadThrow()
        {
            return Phase == HorsemanPhase.Dismounted &&
                   (Health.Current / Health.Max) <= dismountedHealthFractionToStartHeadThrow;
        }

        public bool TryReceiveDamage(DamageInfo info)
        {
            if (!Health.IsAlive) return false;
            if (Phase == HorsemanPhase.Mounted) return false; // the charge is only ever resolved by a parry, never raw damage

            GuardSide side = GuardSide.Overhead;
            GuardResult result = Guard.ResolveIncomingHit(info.Direction, side);
            if (result == GuardResult.Parried)
            {
                Machine.ChangeState<Boss.Horseman.Staggered>();
                return true;
            }
            if (result == GuardResult.Blocked) return false;

            Health.ApplyDamage(info);
            return false;
        }

        public bool IsAlive => Health.IsAlive;
    }
}
