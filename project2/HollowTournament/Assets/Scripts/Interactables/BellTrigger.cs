using UnityEngine;

namespace HollowTournament.Interactables
{
    /// <summary>A skull bowled into this bell opens the Chapel gate. One-shot.</summary>
    public class BellTrigger : MonoBehaviour
    {
        [SerializeField] private Animator gateAnimator;
        [SerializeField] private string gateOpenTrigger = "Open";
        [SerializeField] private AudioSource bellRingSfx;

        private bool _rung;

        public void Ring()
        {
            if (_rung) return;
            _rung = true;
            bellRingSfx?.Play();
            gateAnimator?.SetTrigger(gateOpenTrigger);
        }
    }
}
