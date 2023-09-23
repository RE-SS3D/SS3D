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

        public FrozenItem(Item item)
        {
            Item = item;
        }

        public int FreezeCount { get; private set; }

        public bool IsFrozen { get; private set; }

        public Item Item { get; }

        public void Freeze()
        {
            FreezeCount++;

            if (IsFrozen)
            {
                return;
            }

            Collider[] colliders = Item.GetComponents<Collider>();
            _originalColliderStates = new bool[colliders.Length];

            for (int i = 0; i < colliders.Length; i++)
            {
                _originalColliderStates[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            if (Item.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                _originalRigidBodyState = rigidbody.isKinematic;
                rigidbody.isKinematic = true;
            }

            if (Item.TryGetComponent<NetworkTransform>(out NetworkTransform networkTransform))
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

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = _originalColliderStates[i];
            }

            if (Item.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.isKinematic = _originalRigidBodyState;
            }

            if (Item.TryGetComponent<NetworkTransform>(out NetworkTransform networkTransform))
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