using UnityEngine;

namespace HollowTournament.CameraSystem
{
    /// <summary>
    /// Hand-rolled third-person orbit/follow camera. The design doc calls for
    /// Cinemachine, which is a straightforward drop-in upgrade later (point a
    /// CinemachineCamera's Follow/LookAt at the same target); this script
    /// exists so the prototype has a working camera without requiring a
    /// Package Manager install pass. When LockOnCamera has a target, this rig
    /// snaps to frame the player and target instead of following free-look input.
    /// </summary>
    public class ThirdPersonFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private LockOnCamera lockOn;
        [SerializeField] private float distance = 6f;
        [SerializeField] private float height = 2.2f;
        [SerializeField] private float mouseSensitivity = 3f;
        [SerializeField] private float followDamping = 10f;

        private float _yaw;
        private float _pitch = 15f;

        private void LateUpdate()
        {
            if (target == null) return;

            if (lockOn != null && lockOn.CurrentTarget != null)
            {
                Vector3 midpoint = (target.position + lockOn.CurrentTarget.position) * 0.5f;
                Vector3 toTarget = (lockOn.CurrentTarget.position - target.position);
                Vector3 behindPlayer = target.position - toTarget.normalized * distance;
                behindPlayer.y = target.position.y + height;
                transform.position = Vector3.Lerp(transform.position, behindPlayer, followDamping * Time.deltaTime);
                transform.LookAt(midpoint + Vector3.up * 1.2f);
                return;
            }

            _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -20f, 60f);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPos = target.position + Vector3.up * height - rot * Vector3.forward * distance;

            transform.position = Vector3.Lerp(transform.position, desiredPos, followDamping * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, rot, followDamping * Time.deltaTime);
        }

        public void SetTarget(Transform t) => target = t;
    }
}
