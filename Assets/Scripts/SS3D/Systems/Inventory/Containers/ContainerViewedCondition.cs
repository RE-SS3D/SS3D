using FishNet.Connection;
using FishNet.Observing;
using SS3D.Core;
using SS3D.Systems.Entities;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// The example below does not have many practical uses
    /// but it shows the bare minimum needed to create a custom condition.
    /// This condition makes an object only visible if the connections
    /// ClientId matches the serialized value, _id.
    /// Make a new class which inherits from ObserverCondition.
    /// ObserverCondition is a scriptable object, so also create an asset
    /// menu to create a new scriptable object of your condition.
    /// </summary>
    [CreateAssetMenu(menuName = "FishNet/SS3D/Observers/ContainerViewed Condition", fileName = "New ContainerViewed Condition")]
    public sealed class ContainerViewedCondition : ObserverCondition
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
            AttachedContainer container = NetworkObject.GetComponent<AttachedContainer>();
            notProcessed = false;

            bool foundEntity = Subsystems.Get<EntitySubsystem>().TryGetSpawnedEntity(connection, out Entity entity);

            return foundEntity && container.Container.ObservingPlayers.Contains(entity);
        }

        /// <summary>
        /// True if the condition requires regular updates.
        /// </summary>
        /// <returns></returns>
        public override bool Timed()
        {
            // A condition should return true for timed
            // if you need the requirements checked regularly.
            // For example, the distance condition is timed
            // because players distances could update regularly.

            // Since a clientId does not change, this does not need
            // to be a timed condition.
            return true;
        }

        /// <summary>
        /// Clones referenced ObserverCondition. This must be populated with your conditions settings.
        /// </summary>
        /// <returns></returns>
        public override ObserverCondition Clone() => this;
    }
}