using UnityEngine;
using HollowTournament.Core;
using HollowTournament.Combat;

namespace HollowTournament.Player
{
    /// <summary>
    /// Owner object for the player's FSM (see PlayerStates.cs). Holds the
    /// MonoBehaviour-facing bits (Animator, Input, CharacterController) and
    /// exposes just enough surface for the states to drive the character.
    ///
    /// FSM shape (per design doc):
    /// Locomotion -> WindUp -> Release (hitbox active) -> Recover -> Locomotion
    /// Locomotion -> Block (parry window at block-start) -> Riposte on success -> Locomotion
    /// Locomotion -> Kick -> Locomotion
    /// Staggered / Dead reachable from any state.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(GuardComponent), typeof(StaminaSystem), typeof(HealthComponent))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Combat tuning")]
        public float windUpDuration = 0.35f;
        public float releaseDuration = 0.25f;
        public float recoverDuration = 0.3f;
        public float attackStaminaCost = 15f;
        public float kickStaminaCost = 25f;
        public float kickDamage = 0f; // kick is a guard-break, not direct damage
        public float riposteDamage = 35f;
        public float staggerDuration = 1.2f;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float turnSpeedDegrees = 540f;

        [Header("Refs")]
        public Animator animator;
        public WeaponHitbox weaponHitbox;
        public SwingDirectionReader swingReader;
        public Transform lockOnTarget;

        public GuardComponent Guard { get; private set; }
        public StaminaSystem Stamina { get; private set; }
        public HealthComponent Health { get; private set; }
        public CharacterController Controller { get; private set; }
        public StateMachine<PlayerController> Machine { get; private set; }

        public GuardSide CurrentGuardSide = GuardSide.None; // set while blocking, used by enemy hit resolution against the player
        public SwingDirection PendingSwingDirection { get; set; }
        public bool RiposteAvailable { get; set; }

        public System.Action<SwingDirection> OnSwingReleased; // combo/sfx hooks
        public System.Action OnKickPerformed; // Chapel level wires this to "kick a skull" logic

        private Vector2 _moveInput;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            Guard = GetComponent<GuardComponent>();
            Stamina = GetComponent<StaminaSystem>();
            Health = GetComponent<HealthComponent>();

            Health.OnDied += () => Machine.ChangeState<Player.Dead>();
            Guard.OnParrySuccess += () => RiposteAvailable = true;

            Machine = new StateMachine<PlayerController>(this);
            Machine.Register(new Player.Locomotion());
            Machine.Register(new Player.WindUp());
            Machine.Register(new Player.Release());
            Machine.Register(new Player.Recover());
            Machine.Register(new Player.Block());
            Machine.Register(new Player.Riposte());
            Machine.Register(new Player.Kick());
            Machine.Register(new Player.Staggered());
            Machine.Register(new Player.Dead());
        }

        private void Start()
        {
            Machine.ChangeState<Player.Locomotion>();
        }

        private void Update()
        {
            PollInput();
            Machine.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Legacy Input Manager polling (no Input System package dependency).
        /// Axis names below ("Horizontal"/"Vertical") are Unity's default ones;
        /// no changes needed to Project Settings > Input Manager.
        /// </summary>
        private void PollInput()
        {
            _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (Input.GetMouseButtonDown(0)) Machine.ChangeState<Player.WindUp>();
            else if (Input.GetMouseButtonUp(0) && Machine.Current is Player.WindUp) Machine.ChangeState<Player.Release>();

            if (Input.GetMouseButtonDown(1)) Machine.ChangeState<Player.Block>();
            else if (Input.GetMouseButtonUp(1) && Machine.Current is Player.Block) Machine.ChangeState<Player.Locomotion>();

            if (Input.GetKeyDown(KeyCode.E)) Machine.ChangeState<Player.Kick>();
        }

        public void ReadMoveInput(Vector2 input) => _moveInput = input;
        public Vector2 MoveInput => _moveInput;

        public void ApplyMoveAndFace(Vector2 input, float deltaTime)
        {
            Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            Vector3 moveDir = camForward * input.y + camRight * input.x;
            Controller.SimpleMove(moveDir * moveSpeed);

            Vector3 faceDir = lockOnTarget != null
                ? (lockOnTarget.position - transform.position)
                : moveDir;
            faceDir.y = 0;
            if (faceDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(faceDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedDegrees * deltaTime);
            }
        }

        public void ReceiveStagger()
        {
            Machine.ChangeState<Player.Staggered>();
        }

        public bool TryReceiveDamage(DamageInfo info)
        {
            if (!Health.IsAlive) return false;

            GuardResult result = Guard.ResolveIncomingHit(info.Direction, CurrentGuardSide);
            if (result == GuardResult.Parried)
            {
                return true; // caller (enemy) opens its own stagger/riposte handling
            }
            if (result == GuardResult.Blocked)
            {
                return false; // chip handled by stamina drain only, no HP loss
            }

            Health.ApplyDamage(info);
            if (Health.IsAlive) Machine.ChangeState<Player.Staggered>();
            return false;
        }

        public bool IsAlive => Health.IsAlive;
    }
}
