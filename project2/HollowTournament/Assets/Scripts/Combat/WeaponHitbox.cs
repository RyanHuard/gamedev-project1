using System.Collections.Generic;
using UnityEngine;

namespace HollowTournament.Combat
{
    /// <summary>
    /// Trigger collider on the weapon bone. Only does damage while active.
    /// Activation/deactivation is driven by Animation Events during the
    /// "Release" portion of a swing (see AnimationEventRelay), not by timers,
    /// so the hit window always matches what the player sees on screen.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour
    {
        [SerializeField] private float damage = 15f;
        [SerializeField] private GameObject owner;

        private Collider _collider;
        private SwingDirection _currentDirection = SwingDirection.None;
        private DamageType _currentType = DamageType.Swing;
        private readonly HashSet<IDamageable> _alreadyHitThisSwing = new();

        public System.Action<IDamageable, DamageInfo> OnHitLanded;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            SetActive(false);
            if (owner == null) owner = transform.root.gameObject;
        }

        public void BeginSwing(SwingDirection direction, DamageType type = DamageType.Swing, float overrideDamage = -1f)
        {
            _currentDirection = direction;
            _currentType = type;
            if (overrideDamage >= 0f) damage = overrideDamage;
            _alreadyHitThisSwing.Clear();
            SetActive(true);
        }

        public void EndSwing()
        {
            SetActive(false);
        }

        /// <summary>
        /// Re-opens the collider for a direction/damage already set by BeginSwing,
        /// without clearing the already-hit set. Used by AnimationEventRelay when
        /// the state machine sets up the swing ahead of the clip's actual "active" event.
        /// </summary>
        public void ReactivateCurrentSwing() => SetActive(true);

        private void SetActive(bool active) => _collider.enabled = active;

        private void OnTriggerEnter(Collider other)
        {
            if (!_collider.enabled) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) return;
            if (!_alreadyHitThisSwing.Add(damageable)) return;

            var info = new DamageInfo(damage, _currentType, _currentDirection, owner);
            bool parried = damageable.TryReceiveDamage(info);
            info.WasParried = parried;
            OnHitLanded?.Invoke(damageable, info);
        }
    }
}
