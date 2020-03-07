using System;
using Mirror;
using UnityEngine;

namespace Inventory.Custom
{
    /**
     * A container for the items that a creature can display on their body
     */
    public class WearableContainer : Container
    {
        public GameObject[] displays;

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

            item.transform.SetParent(displays[index].transform, false);
            item.transform.localPosition = new Vector3();
            item.transform.localRotation = new Quaternion();

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

            item.transform.SetParent(null);

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
