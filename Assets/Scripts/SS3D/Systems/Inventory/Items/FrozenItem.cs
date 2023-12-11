using System;
using FishNet.Component.Transforming;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// Stores values to freeze/unfreeze an item without lasting changes
    /// </summary>
    public class FrozenItem : IDisposable
    {
        private bool[] _originalColliderStates;
        private bool _originalRigidBodyState;
        private bool _originalNetworkTransformState;
        
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
            _originalColliderStates = new bool[colliders.Length];
            for (var i = 0; i < colliders.Length; i++)
            {
                _originalColliderStates[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            var rigidbody = Item.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                _originalRigidBodyState = rigidbody.isKinematic;
                rigidbody.isKinematic = true;
            }
            
            var networkTransform = Item.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                _originalNetworkTransformState = networkTransform.enabled;
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
                colliders[i].enabled = _originalColliderStates[i];
            }
            
            var rigidbody = Item.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = _originalRigidBodyState;
            }
            
            var networkTransform = Item.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.enabled = _originalNetworkTransformState;
            }

            IsFrozen = false;
        }

        public void Dispose()
        {
            Unfreeze();
        }
    }
}