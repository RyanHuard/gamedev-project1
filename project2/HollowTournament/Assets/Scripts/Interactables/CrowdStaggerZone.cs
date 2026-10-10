using System.Collections.Generic;
using UnityEngine;
using HollowTournament.Enemies;

namespace HollowTournament.Interactables
{
    /// <summary>A skull kicked into a packed group of skeletons staggers all of them at once.</summary>
    public class CrowdStaggerZone : MonoBehaviour
    {
        [SerializeField] private List<SkeletonEnemy> skeletonsInZone = new();

        private void OnTriggerEnter(Collider other)
        {
            var skeleton = other.GetComponentInParent<SkeletonEnemy>();
            if (skeleton != null && !skeletonsInZone.Contains(skeleton))
                skeletonsInZone.Add(skeleton);
        }

        private void OnTriggerExit(Collider other)
        {
            var skeleton = other.GetComponentInParent<SkeletonEnemy>();
            if (skeleton != null) skeletonsInZone.Remove(skeleton);
        }

        public void StaggerAll()
        {
            foreach (var skeleton in skeletonsInZone)
            {
                if (skeleton != null && skeleton.IsAlive)
                    skeleton.ReceiveStagger();
            }
        }
    }
}
