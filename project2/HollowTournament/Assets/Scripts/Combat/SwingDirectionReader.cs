using UnityEngine;

namespace HollowTournament.Combat
{
    /// <summary>
    /// Mordhau-style directional input: accumulates mouse delta while a swing's
    /// WindUp is active and classifies the net movement into Left/Right/Overhead
    /// by angle. Call BeginSample() on WindUp enter, ReadDirection() on Release enter.
    /// </summary>
    public class SwingDirectionReader : MonoBehaviour
    {
        [SerializeField] private float overheadConeDegrees = 50f;

        private Vector2 _accumulated;
        private bool _sampling;

        public void BeginSample()
        {
            _accumulated = Vector2.zero;
            _sampling = true;
        }

        private void Update()
        {
            if (!_sampling) return;
            _accumulated += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        public SwingDirection ReadDirection()
        {
            _sampling = false;

            if (_accumulated.sqrMagnitude < 0.01f)
                return SwingDirection.Right; // no discernible motion: default to a right swing

            float angle = Mathf.Abs(Mathf.Atan2(_accumulated.x, -_accumulated.y) * Mathf.Rad2Deg);
            // angle measured from straight-up (overhead). Within the cone -> overhead,
            // otherwise whichever side the horizontal component points to.
            if (angle <= overheadConeDegrees * 0.5f)
                return SwingDirection.Overhead;

            return _accumulated.x >= 0f ? SwingDirection.Right : SwingDirection.Left;
        }
    }
}
