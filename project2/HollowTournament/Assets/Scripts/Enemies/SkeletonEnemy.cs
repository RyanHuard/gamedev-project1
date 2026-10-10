using UnityEngine;
using UnityEngine.AI;
using HollowTournament.Core;
using HollowTournament.Combat;

namespace HollowTournament.Enemies
{
    public enum SkeletonVariant
    {
        /// <summary>Gatehouse: plain, dies to any clean hit. Teaches Block.</summary>
        Basic,
        /// <summary>Armory: only takes damage from a Riposte (post-parry counter). Teaches Parry.</summary>
        Armored,
        /// <summary>Chapel: guard must be broken with Kick before swings connect. Teaches Kick.</summary>
        Shield
    }

    /// <summary>
    /// Owner for the skeleton FSM: Idle -> Approach -> Circle -> (WindUp -> Attack -> Recover | Block)
    /// -> Staggered (on parry or kick) -> Dead. The Armored/Shield variants don't add new
    /// states; they change which hits TryReceiveDamage actually lets through.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(GuardComponent), typeof(StaminaSystem))]
    [RequireComponent(typeof(HealthComponent))]
    public class SkeletonEnemy : MonoBehaviour, IDamageable
    {
        [Header("Variant")]
        public SkeletonVariant variant = SkeletonVariant.Basic;

        [Header("Tuning")]
        public float approachStopDistance = 2.2f;
        public float circleRadius = 2.5f;
        public float circleSpeed = 60f; // degrees/sec around the player
        public float decisionIntervalMin = 0.8f;
        public float decisionIntervalMax = 2.0f;
        public float windUpDuration = 0.5f;
        public float attackDuration = 0.3f;
        public float recoverDuration = 0.5f;
        public float attackDamage = 12f;
        public float staggerDuration = 1.0f;
        [Range(0f, 1f)] public float blockChance = 0.35f;

        [Header("Refs")]
        public Animator animator;
        public WeaponHitbox weaponHitbox;
        public Transform target; // the player
        public GuardSide guardSide = GuardSide.Overhead;

        /// <summary>Shield variant only: cleared by a Kick, re-armed on Enter Circle. Gate for damage, not a state.</summary>
        public bool ShieldBroken { get; private set; }

        public NavMeshAgent Agent { get; private set; }
        public GuardComponent Guard { get; private set; }
        public StaminaSystem Stamina { get; private set; }
        public HealthComponent Health { get; private set; }
        public StateMachine<SkeletonEnemy> Machine { get; private set; }

        public System.Action OnSkullPopped; // Chapel beat: a kicked/killed skeleton drops a kickable skull

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Guard = GetComponent<GuardComponent>();
            Stamina = GetComponent<StaminaSystem>();
            Health = GetComponent<HealthComponent>();

            Health.OnDied += HandleDeath;

            Machine = new StateMachine<SkeletonEnemy>(this);
            Machine.Register(new Enemies.Skeleton.Idle());
            Machine.Register(new Enemies.Skeleton.Approach());
            Machine.Register(new Enemies.Skeleton.Circle());
            Machine.Register(new Enemies.Skeleton.WindUp());
            Machine.Register(new Enemies.Skeleton.Attack());
            Machine.Register(new Enemies.Skeleton.Recover());
            Machine.Register(new Enemies.Skeleton.Block());
            Machine.Register(new Enemies.Skeleton.Staggered());
            Machine.Register(new Enemies.Skeleton.Dead());
        }

        private void Start() => Machine.ChangeState<Enemies.Skeleton.Idle>();

        private void Update() => Machine.Tick(Time.deltaTime);

        private void HandleDeath()
        {
            Machine.ChangeState<Enemies.Skeleton.Dead>();
            OnSkullPopped?.Invoke();
        }

        public void BreakShield()
        {
            ShieldBroken = true;
            Guard.ForceBreak();
        }

        public void RearmShield() => ShieldBroken = false;

        public void ReceiveStagger(float duration = -1f)
        {
            Machine.ChangeState<Enemies.Skeleton.Staggered>();
        }

        public bool TryReceiveDamage(DamageInfo info)
        {
            if (!Health.IsAlive) return false;

            // Kick always staggers/breaks regardless of variant gating.
            if (info.Type == DamageType.Kick)
            {
                if (variant == SkeletonVariant.Shield) BreakShield();
                Guard.ForceBreak();
                Machine.ChangeState<Enemies.Skeleton.Staggered>();
                return false;
            }

            if (variant == SkeletonVariant.Armored && info.Type != DamageType.Riposte)
            {
                // Armor shrugs off anything that isn't a post-parry riposte.
                return false;
            }

            if (variant == SkeletonVariant.Shield && !ShieldBroken && info.Type != DamageType.Riposte)
            {
                GuardResult blocked = Guard.ResolveIncomingHit(info.Direction, guardSide);
                return blocked == GuardResult.Parried;
            }

            GuardResult result = Guard.ResolveIncomingHit(info.Direction, guardSide);
            if (result == GuardResult.Parried)
            {
                Machine.ChangeState<Enemies.Skeleton.Staggered>();
                return true;
            }
            if (result == GuardResult.Blocked) return false;

            Health.ApplyDamage(info);
            return false;
        }

        public bool IsAlive => Health.IsAlive;
    }
}
