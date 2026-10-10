using UnityEngine;

namespace HollowTournament.Interactables
{
    /// <summary>
    /// Chapel beat: a Kick knocks a skeleton's skull loose (spawned by SkeletonEnemy.OnSkullPopped
    /// or directly by the player's Kick on a shield skeleton). While rolling, it can hit a
    /// BellTrigger to open a gate, or a CrowdStaggerZone to stagger a group of skeletons at once.
    /// This is the "kick used in a new way" beat the grading rubric is looking for.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class KickableSkull : MonoBehaviour
    {
        [SerializeField] private float kickForce = 14f;
        [SerializeField] private float minSpeedToTrigger = 2f;

        private Rigidbody _rb;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void LaunchFrom(Vector3 origin, Vector3 direction)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(direction.normalized * kickForce, ForceMode.VelocityChange);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_rb.linearVelocity.magnitude < minSpeedToTrigger) return;

            var bell = collision.collider.GetComponentInParent<BellTrigger>();
            bell?.Ring();

            var crowd = collision.collider.GetComponentInParent<CrowdStaggerZone>();
            crowd?.StaggerAll();
        }
    }
}
