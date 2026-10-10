using UnityEngine;
using HollowTournament.Core;
using HollowTournament.Combat;

namespace HollowTournament.Boss.Horseman
{
    // ---------- Phase 1: Mounted ----------

    /// <summary>Rides straight at the player with the lance hitbox active. A parry here calls boss.Unhorse() directly.</summary>
    public class Charge : IState<HorsemanBoss>
    {
        public void Enter(HorsemanBoss b)
        {
            b.SetPhase(HorsemanPhase.Mounted);
            b.transform.position = b.chargeStartPoint != null ? b.chargeStartPoint.position : b.transform.position;
            b.animator?.SetTrigger("Charge");
            b.lanceHitbox?.BeginSwing(SwingDirection.Overhead, DamageType.Swing, b.chargeLanceDamage);
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            Vector3 target = b.chargeEndPoint != null ? b.chargeEndPoint.position : b.player.position;
            b.transform.position = Vector3.MoveTowards(b.transform.position, target, b.chargeSpeed * dt);
            b.transform.LookAt(new Vector3(target.x, b.transform.position.y, target.z));

            if (Vector3.Distance(b.transform.position, target) < 0.5f)
                b.Machine.ChangeState<Turn>();
        }

        public void Exit(HorsemanBoss b) => b.lanceHitbox?.EndSwing();
    }

    /// <summary>Wheels the horse around at the end of the lane before the next charge.</summary>
    public class Turn : IState<HorsemanBoss>
    {
        private float _timer;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            b.animator?.SetTrigger("Turn");
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer >= b.turnDuration)
            {
                (b.chargeStartPoint, b.chargeEndPoint) = (b.chargeEndPoint, b.chargeStartPoint);
                b.Machine.ChangeState<Charge>();
            }
        }

        public void Exit(HorsemanBoss b) { }
    }

    /// <summary>Lance was parried: the Horseman is thrown from the horse and phase 2 begins.</summary>
    public class Unhorsed : IState<HorsemanBoss>
    {
        private float _timer;
        private const float Duration = 1.0f;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            b.animator?.SetTrigger("Unhorsed");
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer >= Duration)
            {
                b.SetPhase(HorsemanPhase.Dismounted);
                b.Machine.ChangeState<DuelWindUp>();
            }
        }

        public void Exit(HorsemanBoss b) { }
    }

    // ---------- Phase 2: Dismounted duel ----------

    public class DuelWindUp : IState<HorsemanBoss>
    {
        private float _timer;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            b.SetPhase(HorsemanPhase.Dismounted);
            b.animator?.SetTrigger("WindUp");
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer < b.windUpDuration) return;

            if (Random.value < b.feintChance)
                b.Machine.ChangeState<Feint>();
            else
                b.Machine.ChangeState<DuelAttack>();
        }

        public void Exit(HorsemanBoss b) { }
    }

    /// <summary>Cancels the swing after the tell to punish a player who parries on reflex instead of on read.</summary>
    public class Feint : IState<HorsemanBoss>
    {
        private float _timer;
        private const float Duration = 0.3f;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            b.animator?.SetTrigger("Feint");
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer >= Duration)
                b.Machine.ChangeState<DuelAttack>();
        }

        public void Exit(HorsemanBoss b) { }
    }

    public class DuelAttack : IState<HorsemanBoss>
    {
        private float _timer;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            SwingDirection dir = (SwingDirection)Random.Range(1, 4);
            b.animator?.SetInteger("SwingDirection", (int)dir);
            b.animator?.SetTrigger("Attack");
            b.meleeHitbox?.BeginSwing(dir, DamageType.Swing, b.meleeDamage);
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer >= b.attackDuration)
                b.Machine.ChangeState<DuelRecover>();
        }

        public void Exit(HorsemanBoss b) => b.meleeHitbox?.EndSwing();
    }

    public class DuelRecover : IState<HorsemanBoss>
    {
        private float _timer;

        public void Enter(HorsemanBoss b) => _timer = 0f;

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer < b.recoverDuration) return;

            if (b.ShouldEnterHeadThrow())
                b.Machine.ChangeState<HeadThrowWindUp>();
            else if (Random.value < 0.2f)
                b.Machine.ChangeState<DuelBlock>();
            else
                b.Machine.ChangeState<DuelWindUp>();
        }

        public void Exit(HorsemanBoss b) { }
    }

    public class DuelBlock : IState<HorsemanBoss>
    {
        private float _timer;
        private const float MaxHold = 1.0f;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            b.Guard.StartBlocking();
            b.animator?.SetBool("Blocking", true);
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (!b.Guard.IsBlocking || _timer >= MaxHold)
                b.Machine.ChangeState<DuelWindUp>();
        }

        public void Exit(HorsemanBoss b)
        {
            b.Guard.StopBlocking();
            b.animator?.SetBool("Blocking", false);
        }
    }

    public class Staggered : IState<HorsemanBoss>
    {
        private float _timer;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            b.animator?.SetTrigger("Staggered");
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer >= b.staggerDuration)
                b.Machine.ChangeState<DuelWindUp>();
        }

        public void Exit(HorsemanBoss b) { }
    }

    // ---------- Phase 3: Flaming pumpkin head ----------

    public class HeadThrowWindUp : IState<HorsemanBoss>
    {
        private float _timer;
        private const float Duration = 0.6f;

        public void Enter(HorsemanBoss b)
        {
            _timer = 0f;
            b.SetPhase(HorsemanPhase.HeadThrow);
            b.animator?.SetTrigger("HeadThrowWindUp");
        }

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer >= Duration)
            {
                ThrowHead(b);
                b.Machine.ChangeState<HeadThrowRecover>();
            }
        }

        public void Exit(HorsemanBoss b) { }

        private static void ThrowHead(HorsemanBoss b)
        {
            if (b.pumpkinHeadProjectilePrefab == null || b.headThrowOrigin == null) return;
            GameObject headGO = Object.Instantiate(b.pumpkinHeadProjectilePrefab, b.headThrowOrigin.position, b.headThrowOrigin.rotation);
            var projectile = headGO.GetComponent<HollowTournament.Interactables.PumpkinHeadProjectile>();
            projectile?.Launch(b.player, b);
        }
    }

    /// <summary>
    /// Waits for the thrown head to resolve. PumpkinHeadProjectile calls back into
    /// boss.Health directly on a successful parry-back (catching the Horseman with
    /// his own flaming head), which can trigger Defeated from HealthComponent.OnDied.
    /// If the throw whiffs or lands on the boss's own miss, loop back for another throw.
    /// </summary>
    public class HeadThrowRecover : IState<HorsemanBoss>
    {
        private float _timer;

        public void Enter(HorsemanBoss b) => _timer = 0f;

        public void Tick(HorsemanBoss b, float dt)
        {
            _timer += dt;
            if (_timer >= b.headThrowCooldown && b.IsAlive)
                b.Machine.ChangeState<HeadThrowWindUp>();
        }

        public void Exit(HorsemanBoss b) { }
    }

    public class Defeated : IState<HorsemanBoss>
    {
        public void Enter(HorsemanBoss b)
        {
            b.SetPhase(HorsemanPhase.Defeated);
            b.animator?.SetTrigger("Defeated");
        }

        public void Tick(HorsemanBoss b, float dt) { }
        public void Exit(HorsemanBoss b) { }
    }
}
