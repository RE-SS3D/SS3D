using System;
using Mirror;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// Stores values to freeze/unfreeze an item without lasting changes
    /// </summary>
    public class FrozenItem : IDisposable
    {
        private bool[] originalColliderStates;
        private bool originalRigidBodyState;
        private bool originalNetworkTransformState;
        
        public int FreezeCount { get; private set; }
        public bool IsFrozen { get; private set; }
        public Item Item { get; }

        public FrozenItem(Item item)
        {
            Item = item;
        }

        public void Freeze()
        {
            FreezeCount++;
            
            if (IsFrozen)
            {
                return;
            }

            Collider[] colliders = Item.GetComponents<Collider>();
            originalColliderStates = new bool[colliders.Length];
            for (var i = 0; i < colliders.Length; i++)
            {
                originalColliderStates[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            var rigidbody = Item.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                originalRigidBodyState = rigidbody.isKinematic;
                rigidbody.isKinematic = true;
            }
            
            var networkTransform = Item.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                originalNetworkTransformState = networkTransform.enabled;
                networkTransform.enabled = false;
            }

            IsFrozen = true;
        }

        public void Unfreeze()
        {
            if (!IsFrozen)
            {
                return;
            }

            FreezeCount--;
            if (FreezeCount > 0)
            {
                return;
            }
            
            Collider[] colliders = Item.GetComponents<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = originalColliderStates[i];
            }
            
            var rigidbody = Item.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = originalRigidBodyState;
            }
            
            var networkTransform = Item.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.enabled = originalNetworkTransformState;
            }

            IsFrozen = false;
        }

        public void Dispose()
        {
            Unfreeze();
        }
    }
}