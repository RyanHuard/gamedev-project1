using UnityEngine;
using HollowTournament.Core;
using HollowTournament.Combat;

namespace HollowTournament.Player
{
    public class Locomotion : IState<PlayerController>
    {
        public void Enter(PlayerController p)
        {
            p.animator?.SetBool("Blocking", false);
            p.animator?.SetBool("Moving", false);
        }

        public void Tick(PlayerController p, float dt)
        {
            p.ApplyMoveAndFace(p.MoveInput, dt);
            p.animator?.SetBool("Moving", p.MoveInput.sqrMagnitude > 0.01f);
        }

        public void Exit(PlayerController p) { }
    }

    /// <summary>Mouse movement is sampled for the whole wind-up; releasing the attack button reads it off.</summary>
    public class WindUp : IState<PlayerController>
    {
        private float _timer;

        public void Enter(PlayerController p)
        {
            _timer = 0f;
            p.swingReader?.BeginSample();
            p.animator?.SetTrigger("WindUp");
        }

        public void Tick(PlayerController p, float dt)
        {
            _timer += dt;
            // Players can hold the wind-up as long as they like (Mordhau feint window);
            // we only auto-release on a hard cap to avoid a stuck state if input is lost.
            if (_timer >= p.windUpDuration * 3f)
                p.Machine.ChangeState<Release>();
        }

        public void Exit(PlayerController p) { }
    }

    public class Release : IState<PlayerController>
    {
        private float _timer;

        public void Enter(PlayerController p)
        {
            _timer = 0f;
            SwingDirection dir = p.swingReader != null ? p.swingReader.ReadDirection() : SwingDirection.Right;
            p.PendingSwingDirection = dir;
            p.animator?.SetInteger("SwingDirection", (int)dir);
            p.animator?.SetTrigger("Release");
            p.weaponHitbox?.BeginSwing(dir);
            p.OnSwingReleased?.Invoke(dir);
        }

        public void Tick(PlayerController p, float dt)
        {
            _timer += dt;
            if (_timer >= p.releaseDuration)
                p.Machine.ChangeState<Recover>();
        }

        public void Exit(PlayerController p)
        {
            p.weaponHitbox?.EndSwing();
        }
    }

    public class Recover : IState<PlayerController>
    {
        private float _timer;

        public void Enter(PlayerController p) => _timer = 0f;

        public void Tick(PlayerController p, float dt)
        {
            _timer += dt;
            if (_timer >= p.recoverDuration)
                p.Machine.ChangeState<Locomotion>();
        }

        public void Exit(PlayerController p) { }
    }

    /// <summary>First ~0.2s of this state is the parry window, enforced by GuardComponent itself.</summary>
    public class Block : IState<PlayerController>
    {
        public void Enter(PlayerController p)
        {
            p.Guard.StartBlocking();
            p.CurrentGuardSide = GuardSide.Overhead; // simple default; directional guard reading is lock-on cone based, see GuardComponent
            p.animator?.SetBool("Blocking", true);
        }

        public void Tick(PlayerController p, float dt)
        {
            if (p.RiposteAvailable)
            {
                p.RiposteAvailable = false;
                p.Machine.ChangeState<Riposte>();
            }
            if (!p.Guard.IsBlocking)
                p.Machine.ChangeState<Locomotion>();
        }

        public void Exit(PlayerController p)
        {
            p.Guard.StopBlocking();
            p.CurrentGuardSide = GuardSide.None;
            p.animator?.SetBool("Blocking", false);
        }
    }

    /// <summary>Free counter-hit opened by a successful parry. Short, guaranteed, high damage.</summary>
    public class Riposte : IState<PlayerController>
    {
        private float _timer;
        private const float Duration = 0.4f;

        public void Enter(PlayerController p)
        {
            _timer = 0f;
            p.animator?.SetTrigger("Riposte");
            p.weaponHitbox?.BeginSwing(SwingDirection.Overhead, DamageType.Riposte, p.riposteDamage);
        }

        public void Tick(PlayerController p, float dt)
        {
            _timer += dt;
            if (_timer >= Duration)
                p.Machine.ChangeState<Locomotion>();
        }

        public void Exit(PlayerController p) => p.weaponHitbox?.EndSwing();
    }

    /// <summary>Breaks guard and, per the Chapel beat, pops a skull loose for later reuse (handled by a KickTarget listener).</summary>
    public class Kick : IState<PlayerController>
    {
        private float _timer;
        private const float Duration = 0.5f;

        public void Enter(PlayerController p)
        {
            _timer = 0f;
            if (!p.Stamina.TrySpend(p.kickStaminaCost))
            {
                p.Machine.ChangeState<Locomotion>();
                return;
            }
            p.animator?.SetTrigger("Kick");
            p.weaponHitbox?.BeginSwing(SwingDirection.None, DamageType.Kick, p.kickDamage);
            p.OnKickPerformed?.Invoke();
        }

        public void Tick(PlayerController p, float dt)
        {
            _timer += dt;
            if (_timer >= Duration)
                p.Machine.ChangeState<Locomotion>();
        }

        public void Exit(PlayerController p) => p.weaponHitbox?.EndSwing();
    }

    public class Staggered : IState<PlayerController>
    {
        private float _timer;

        public void Enter(PlayerController p)
        {
            _timer = 0f;
            p.animator?.SetTrigger("Staggered");
        }

        public void Tick(PlayerController p, float dt)
        {
            _timer += dt;
            if (_timer >= p.staggerDuration)
                p.Machine.ChangeState<Locomotion>();
        }

        public void Exit(PlayerController p) { }
    }

    public class Dead : IState<PlayerController>
    {
        public void Enter(PlayerController p)
        {
            p.animator?.SetTrigger("Dead");
            p.Controller.enabled = false;
        }

        public void Tick(PlayerController p, float dt) { }
        public void Exit(PlayerController p) { }
    }
}
