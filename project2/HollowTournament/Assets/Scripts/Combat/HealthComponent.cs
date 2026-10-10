using UnityEngine;

namespace HollowTournament.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsAlive => Current > 0f;

        public System.Action<DamageInfo> OnDamaged;
        public System.Action OnDied;

        private void Awake() => Current = maxHealth;

        public void ApplyDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            Current = Mathf.Max(0f, Current - info.Amount);
            OnDamaged?.Invoke(info);
            if (Current <= 0f) OnDied?.Invoke();
        }

        public void ResetHealth()
        {
            Current = maxHealth;
        }
    }
}
