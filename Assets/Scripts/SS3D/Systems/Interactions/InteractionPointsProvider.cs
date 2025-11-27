using System.Linq;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Very simple scripts to define a bunch of points on a target where interactions can occur.
    /// For instance, used on girder near the screw to allow players to have the screw interaction near ground.
    /// </summary>
    public class InteractionPointsProvider : MonoBehaviour
    {
        [SerializeField]
        private Transform[] _interactionPoints;

        public Transform[] InteractionPoints => _interactionPoints;

        /// <summary>
        /// Get the closest point from the source position, in world space coordinates
        /// </summary>
        public Vector3 GetClosestPointFromSource(Vector3 source)
        {
            return _interactionPoints
                .Select(x => x.position)                  
                .OrderBy(p => Vector3.Distance(p, source))
                .First();  
        }

    }
}
