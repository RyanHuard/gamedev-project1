namespace HollowTournament.Combat
{
    public enum SwingDirection
    {
        None,
        Left,
        Right,
        Overhead
    }

    public enum GuardSide
    {
        None,
        Left,
        Right,
        Overhead
    }

    public enum DamageType
    {
        Swing,
        Kick,
        Riposte,
        Thrown
    }

    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public SwingDirection Direction;
        public UnityEngine.GameObject Source;
        public bool WasParried; // set true by the receiver's resolver, read back by the attacker for riposte windows

        public DamageInfo(float amount, DamageType type, SwingDirection direction, UnityEngine.GameObject source)
        {
            Amount = amount;
            Type = type;
            Direction = direction;
            Source = source;
            WasParried = false;
        }
    }

    /// <summary>
    /// Anything that can take a hit: player, skeletons, the Horseman, even the pumpkin bomb.
    /// Returns true if the hit was absorbed by a parry (caller should open a riposte window).
    /// </summary>
    public interface IDamageable
    {
        bool TryReceiveDamage(DamageInfo info);
        bool IsAlive { get; }
    }
}
