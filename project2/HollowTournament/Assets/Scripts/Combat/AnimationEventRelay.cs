using UnityEngine;

namespace HollowTournament.Combat
{
    /// <summary>
    /// Sits on the same object as the Animator. Animation Clips call these
    /// methods directly via Animation Events (right-click the clip timeline
    /// in the editor -> Add Event -> pick the method here), so the weapon's
    /// hit window always lines up with what's on screen instead of a timer
    /// guessing at it.
    /// </summary>
    public class AnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private WeaponHitbox hitbox;

        // Called with no direction override; the state that started the swing
        // already told the hitbox which direction/damage to use via BeginSwing.
        public void AE_WeaponActive() => hitbox?.ReactivateCurrentSwing();
        public void AE_WeaponInactive() => hitbox?.EndSwing();

        // Hook for footstep/voice/bone-rattle SFX authored straight on the clip.
        public System.Action<string> OnGenericEvent;
        public void AE_Sfx(string clipName) => OnGenericEvent?.Invoke(clipName);
    }
}
