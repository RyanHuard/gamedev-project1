using UnityEngine;
using HollowTournament.Core;
using HollowTournament.Combat;

namespace HollowTournament.Enemies.Skeleton
{
    public class Idle : IState<SkeletonEnemy>
    {
        private float _timer;

        public void Enter(SkeletonEnemy s)
        {
            _timer = 0f;
            if (s.AgentReady) s.Agent.isStopped = true;
            s.animator?.SetBool("Moving", false);
        }

        public void Tick(SkeletonEnemy s, float dt)
        {
            if (s.target == null) return;
            _timer += dt;
            float distance = Vector3.Distance(s.transform.position, s.target.position);
            if (distance <= s.circleRadius * 2.5f && _timer > 0.3f)
                s.Machine.ChangeState<Approach>();
        }

        public void Exit(SkeletonEnemy s) { }
    }

    public class Approach : IState<SkeletonEnemy>
    {
        public void Enter(SkeletonEnemy s)
        {
            if (s.AgentReady) s.Agent.isStopped = false;
            s.animator?.SetBool("Moving", true);
        }

        public void Tick(SkeletonEnemy s, float dt)
        {
            if (s.target == null) return;
            if (s.AgentReady) s.Agent.SetDestination(s.target.position);
            float distance = Vector3.Distance(s.transform.position, s.target.position);
            if (distance <= s.approachStopDistance)
                s.Machine.ChangeState<Circle>();
        }

        public void Exit(SkeletonEnemy s) { }
    }

    /// <summary>Strafes around the player while a random timer decides the next committed action.</summary>
    public class Circle : IState<SkeletonEnemy>
    {
        private float _decisionTimer;
        private float _angle;
        private float _direction;

        public void Enter(SkeletonEnemy s)
        {
            s.RearmShield();
            if (s.AgentReady) s.Agent.isStopped = false;
            _decisionTimer = Random.Range(s.decisionIntervalMin, s.decisionIntervalMax);
            _direction = Random.value > 0.5f ? 1f : -1f;
            Vector3 offset = s.transform.position - s.target.position;
            _angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        }

        public void Tick(SkeletonEnemy s, float dt)
        {
            if (s.target == null) return;

            float distance = Vector3.Distance(s.transform.position, s.target.position);
            if (distance > s.approachStopDistance * 1.6f)
            {
                s.Machine.ChangeState<Approach>();
                return;
            }

            _angle += s.circleSpeed * dt * _direction;
            Vector3 orbit = new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), 0f, Mathf.Sin(_angle * Mathf.Deg2Rad)) * s.circleRadius;
            Vector3 dest = s.target.position + orbit;
            if (s.AgentReady) s.Agent.SetDestination(dest);

            Vector3 faceDir = s.target.position - s.transform.position;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.001f)
                s.transform.rotation = Quaternion.Slerp(s.transform.rotation, Quaternion.LookRotation(faceDir), 5f * dt);

            _decisionTimer -= dt;
            if (_decisionTimer <= 0f)
            {
                if (Random.value < s.blockChance)
                    s.Machine.ChangeState<Block>();
                else
                    s.Machine.ChangeState<WindUp>();
            }
        }

        public void Exit(SkeletonEnemy s) { }
    }

    public class WindUp : IState<SkeletonEnemy>
    {
        private float _timer;

        public void Enter(SkeletonEnemy s)
        {
            _timer = 0f;
            if (s.AgentReady) s.Agent.isStopped = true;
            s.animator?.SetTrigger("WindUp");
        }

        public void Tick(SkeletonEnemy s, float dt)
        {
            _timer += dt;
            if (_timer >= s.windUpDuration)
                s.Machine.ChangeState<Attack>();
        }

        public void Exit(SkeletonEnemy s) { }
    }

    public class Attack : IState<SkeletonEnemy>
    {
        private float _timer;

        public void Enter(SkeletonEnemy s)
        {
            _timer = 0f;
            var dir = s.guardSide switch
            {
                GuardSide.Left => SwingDirection.Left,
                GuardSide.Right => SwingDirection.Right,
                _ => SwingDirection.Overhead
            };
            s.animator?.SetTrigger("Attack");
            s.weaponHitbox?.BeginSwing(dir, DamageType.Swing, s.attackDamage);
        }

        public void Tick(SkeletonEnemy s, float dt)
        {
            _timer += dt;
            if (_timer >= s.attackDuration)
                s.Machine.ChangeState<Recover>();
        }

        public void Exit(SkeletonEnemy s) => s.weaponHitbox?.EndSwing();
    }

    public class Recover : IState<SkeletonEnemy>
    {
        private float _timer;

        public void Enter(SkeletonEnemy s) => _timer = 0f;

        public void Tick(SkeletonEnemy s, float dt)
        {
            _timer += dt;
            if (_timer >= s.recoverDuration)
                s.Machine.ChangeState<Circle>();
        }

        public void Exit(SkeletonEnemy s) { }
    }

    public class Block : IState<SkeletonEnemy>
    {
        private float _timer;
        private const float MaxHold = 1.2f;

        public void Enter(SkeletonEnemy s)
        {
            _timer = 0f;
            s.Guard.StartBlocking();
            s.animator?.SetBool("Blocking", true);
        }

        public void Tick(SkeletonEnemy s, float dt)
        {
            _timer += dt;
            if (!s.Guard.IsBlocking || _timer >= MaxHold)
                s.Machine.ChangeState<Circle>();
        }

        public void Exit(SkeletonEnemy s)
        {
            s.Guard.StopBlocking();
            s.animator?.SetBool("Blocking", false);
        }
    }

    public class Staggered : IState<SkeletonEnemy>
    {
        private float _timer;

        public void Enter(SkeletonEnemy s)
        {
            _timer = 0f;
            if (s.AgentReady) s.Agent.isStopped = true;
            s.animator?.SetTrigger("Staggered");
        }

        public void Tick(SkeletonEnemy s, float dt)
        {
            _timer += dt;
            if (_timer >= s.staggerDuration)
                s.Machine.ChangeState<Circle>();
        }

        public void Exit(SkeletonEnemy s) { }
    }

    public class Dead : IState<SkeletonEnemy>
    {
        public void Enter(SkeletonEnemy s)
        {
            if (s.AgentReady) s.Agent.isStopped = true;
            s.Agent.enabled = false;
            s.animator?.SetTrigger("Dead");
            // skeleton "rattles apart": goofy ragdoll/skull-pop handled by a listener on OnSkullPopped
        }

        public void Tick(SkeletonEnemy s, float dt) { }
        public void Exit(SkeletonEnemy s) { }
    }
}
