
using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.ObserverConditions
{
    [CreateAssetMenu(menuName = "FishNet/SS3D/Observers/ContainerViewed Condition", fileName = "New ContainerViewed Condition")]
    public class ContainerViewedCondition : ObserverCondition
    {
        /// <summary>
        /// ClientId a connection must be to pass the condition.
        /// </summary>
        [Tooltip("ClientId a connection must be to pass the condition.")]
        [SerializeField]
        private int _id = 0;

        /// <summary>
        /// Returns if the object which this condition resides should be visible to connection.
        /// </summary>
        /// <param name="connection">Connection which the condition is being checked for.</param>
        /// <param name="currentlyAdded">True if the connection currently has visibility of this object.</param>
        /// <param name="notProcessed">True if the condition was not processed. This can be used to skip processing for performance. While output as true this condition result assumes the previous ConditionMet value.</param>
        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            var container = NetworkObject.GetComponent<AttachedContainer>();
            notProcessed = false;

            float sqrMaximumDistance = (container.MaxDistance * container.MaxDistance);

            Vector3 thisPosition = NetworkObject.transform.position;
            foreach (NetworkObject nob in connection.Objects)
            {
                //If within distance.
                if (Vector3.SqrMagnitude(nob.transform.position - thisPosition) <= sqrMaximumDistance)
                {
                    // Must be opened for it's content to be visible.
                    if (container.IsOpenable && container.ContainerInteractive.IsOpen())
                        return true;
                    else if (!container.IsOpenable) return true;
                }

            }

            /* If here no client objects are within distance. */
            return false;
        }

        /// <summary>
        /// True if the condition requires regular updates.
        /// </summary>
        /// <returns></returns>
        public override bool Timed()
        {
            return true;
        }

        /// <summary>
        /// Clones referenced ObserverCondition. This must be populated with your conditions settings.
        /// </summary>
        /// <returns></returns>
        public override ObserverCondition Clone()
        {
            return Instantiate(this);
        }

    }
}

