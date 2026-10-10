using UnityEngine;

namespace HollowTournament.Combat
{
    public class StaminaSystem : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float regenPerSecond = 15f;
        [SerializeField] private float regenDelayAfterSpend = 0.6f;

        public float Current { get; private set; }
        public float Max => maxStamina;

        private float _regenDelayTimer;

        private void Awake() => Current = maxStamina;

        private void Update()
        {
            if (_regenDelayTimer > 0f)
            {
                _regenDelayTimer -= Time.deltaTime;
                return;
            }
            Current = Mathf.Min(maxStamina, Current + regenPerSecond * Time.deltaTime);
        }

        public bool TrySpend(float amount)
        {
            if (Current < amount) return false;
            Current -= amount;
            _regenDelayTimer = regenDelayAfterSpend;
            return true;
        }

        /// <summary>Block chip-drain doesn't gate on affordability the way an action cost does; it just can't go below zero.</summary>
        public void Drain(float amount)
        {
            Current = Mathf.Max(0f, Current - amount);
            _regenDelayTimer = regenDelayAfterSpend;
        }

        public bool IsExhausted => Current <= 0f;
    }
}
