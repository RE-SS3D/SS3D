using System;
using Mirror;
using UnityEngine;

namespace SS3D.Engine.Inventory.Extensions
{
    /**
     * A container for the items that a creature can display on their body
     */
    public class WearableContainer : Container
    {
        public GameObject[] displays;
        private Quaternion[] originalRotations;

        public void Start()
        {
            if (displays != null)
            {
                originalRotations = new Quaternion[displays.Length];
            }
        }

        // Override add item so any changes refresh the interaction system's tool
        public override void AddItem(int index, GameObject item)
        {
            base.AddItem(index, item);

            // Place item in display position
            PlaceItem(index, item);
        }
        public override GameObject RemoveItem(int slot)
        {
            Item item = GetItem(slot);
            if (!item)
            {
                return null;
            }
            UnplaceItem(slot, item.gameObject);

            return base.RemoveItem(slot);
        }

        public void OnValidate()
        {
            Array.Resize(ref displays, slots.Length);
        }
        public override void OnStartClient()
        {
            // Load what is in this slot and place appropriately
            for (int i = 0; i < Length(); ++i)
            {
                var item = GetItem(i);

                // A rare case where we specifically call the method client-side.
                // This allows the client to 'catch up' with the server
                if (item)
                    PlaceItem(i, item.gameObject);
            }
        }

        /**
     * Places an item into the display position.
     * Should be called by server, which then calls on all clients.
     */
        private void PlaceItem(int index, GameObject item)
        {
            item.SetActive(true);

            // Determine physics status
            item.GetComponent<Rigidbody>().isKinematic = true;
            item.GetComponent<Collider>().enabled = false;
            if (item.GetComponent<NetworkTransform>())
                item.GetComponent<NetworkTransform>().enabled = false;

            // Back up old rotation
            if (originalRotations != null)
            {
                originalRotations[index] = item.transform.rotation;
            }
            
            
            item.transform.SetParent(displays[index].transform, false);
            // Check if a custom attachment point should be used
            Item component = item.GetComponent<Item>();
            Transform attachmentPoint = component.attachmentPoint;
            if (component != null && attachmentPoint != null)
            {
                // Create new (temporary) point
                // HACK: Required because rotation pivot can be different
                GameObject temporaryPoint = new GameObject();
                temporaryPoint.transform.parent = displays[index].transform;
                temporaryPoint.transform.localPosition = Vector3.zero;
                temporaryPoint.transform.rotation = attachmentPoint.root.rotation *  attachmentPoint.localRotation;
                
                // Assign parent
                item.transform.parent = temporaryPoint.transform;
                // Assign the relative position between the attachment point and the object
                item.transform.localPosition = -attachmentPoint.localPosition;
                item.transform.localRotation = Quaternion.identity;
            }
            else
            {
                item.transform.localPosition = new Vector3();
                item.transform.localRotation = new Quaternion();
            }
            

            if (isServer)
                RpcPlaceItem(index, item);
        }
        [ClientRpc]
        private void RpcPlaceItem(int index, GameObject item)
        {
            if (!isServer)
                PlaceItem(index, item);
        }

        /**
     * Removes an item from the display
     * Should be called by server, which then calls on all clients
     */
        private void UnplaceItem(int index, GameObject item)
        {
            item.SetActive(false);

            // Determine physics status
            item.GetComponent<Rigidbody>().isKinematic = false;
            item.GetComponent<Collider>().enabled = true;
            if (item.GetComponent<NetworkTransform>())
                item.GetComponent<NetworkTransform>().enabled = true;

            if (item.transform.parent != displays[index].transform)
            {
                // Destroy temporary attachment point
                Destroy(item.transform.parent.gameObject);
            }
            
            item.transform.SetParent(null);
            
            // Restore old rotation
            if (originalRotations != null)
            {
                item.transform.rotation = originalRotations[index];
            }

            if (isServer)
                RpcUnplaceItem(index, item);
        }
        [ClientRpc]
        private void RpcUnplaceItem(int index, GameObject item)
        {
            if (!isServer)
                UnplaceItem(index, item);
        }

    }
}
