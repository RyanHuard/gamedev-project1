using UnityEngine;

namespace HollowTournament.CameraSystem
{
    /// <summary>
    /// Third-person lock-on targeting. Pairs with a Cinemachine virtual camera:
    /// this script just picks/holds the target and exposes it; wire a
    /// Cinemachine Target Group or a "LookAt override" to CurrentTarget in the
    /// editor rather than fighting Cinemachine's own damping from code.
    /// Directional swings are much easier to read with the camera locked
    /// square-on to the enemy, which is the whole point of offering it.
    /// </summary>
    public class LockOnCamera : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float maxLockOnDistance = 15f;
        [SerializeField] private float lockOnConeDegrees = 60f;
        [SerializeField] private LayerMask targetLayers;

        public Transform CurrentTarget { get; private set; }
        public System.Action<Transform> OnTargetChanged;

        public void ToggleLockOn(UnityEngine.Camera referenceCamera)
        {
            if (CurrentTarget != null)
            {
                SetTarget(null);
                return;
            }

            Transform best = FindBestTarget(referenceCamera);
            SetTarget(best);
        }

        public void ClearIfTargetInvalid()
        {
            if (CurrentTarget == null) return;
            float dist = Vector3.Distance(player.position, CurrentTarget.position);
            if (dist > maxLockOnDistance) SetTarget(null);
        }

        private Transform FindBestTarget(UnityEngine.Camera referenceCamera)
        {
            var candidates = Physics.OverlapSphere(player.position, maxLockOnDistance, targetLayers);
            Transform best = null;
            float bestScore = float.MaxValue;

            foreach (var candidate in candidates)
            {
                Vector3 toCandidate = candidate.transform.position - referenceCamera.transform.position;
                float angle = Vector3.Angle(referenceCamera.transform.forward, toCandidate);
                if (angle > lockOnConeDegrees * 0.5f) continue;

                // Prefer the closest-to-center-of-screen candidate over the nearest one,
                // since that's what the player is visually aiming at.
                if (angle < bestScore)
                {
                    bestScore = angle;
                    best = candidate.transform;
                }
            }
            return best;
        }

        private void SetTarget(Transform target)
        {
            CurrentTarget = target;
            OnTargetChanged?.Invoke(target);
        }

        private void Update() => ClearIfTargetInvalid();
    }
}
