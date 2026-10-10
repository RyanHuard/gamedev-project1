using UnityEngine;
using HollowTournament.Interactables;

namespace HollowTournament.Enemies
{
    /// <summary>Spawns a KickableSkull where a skeleton dies, feeding the Chapel's bell/crowd beat.</summary>
    [RequireComponent(typeof(SkeletonEnemy))]
    public class SkullDropper : MonoBehaviour
    {
        [SerializeField] private GameObject skullPrefab;

        private void Awake()
        {
            GetComponent<SkeletonEnemy>().OnSkullPopped += DropSkull;
        }

        private void DropSkull()
        {
            if (skullPrefab == null) return;
            Instantiate(skullPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }
}
