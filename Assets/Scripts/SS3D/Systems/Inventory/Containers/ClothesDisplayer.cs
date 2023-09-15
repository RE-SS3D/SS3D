
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using FishNet.Object.Synchronizing;
using UnityEditor;
using FishNet.Object;


namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Display clothes on player for all clients.
    /// </summary>
    public class ClothesDisplayer : NetworkActor
    {
        /// <summary>
        /// A small structure containing information regarding clothes on player, to help syncing them over the network.
        /// For each bodypart that can have clothing, it also contains information on the item to display, if it should show or not.
        /// </summary>
        private struct ClothDisplayData
        {
            public ClothDisplayData(NetworkObject bodyPart, Item clothToDisplay)
            {
                _bodyPart= bodyPart;
                _clothToDisplay= clothToDisplay;
            }
            public NetworkObject _bodyPart;
            public Item _clothToDisplay;
        }

        /// <summary>
        /// The inventory containing the player's clothing slots.
        /// </summary>
        public HumanInventory _inventory;

        /// <summary>
        /// Synced list of body part associated to the clothes on them.
        /// </summary>
        [SyncObject]
        private readonly SyncList<ClothDisplayData> _clothedBodyParts = new SyncList<ClothDisplayData>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            _inventory.OnContainerContentChanged += HandleContainerContentChanged;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _clothedBodyParts.OnChange += ClothedBodyPartsOnChange;
        }

        /// <summary>
        /// Callback when the syncedList _clothedBodyParts changes. Update the displayed clothes on the player.
        /// </summary>
        private void ClothedBodyPartsOnChange(SyncListOperation op, int index,
            ClothDisplayData oldData, ClothDisplayData newData, bool asServer)
        {

            if(asServer) return;

            switch (op)
            {
                // Show the new cloth on the player
                case SyncListOperation.Add:
                    NetworkObject newBodyPart = newData._bodyPart;
                    Item newItem = newData._clothToDisplay;
                    if (!newBodyPart.TryGetComponent(out SkinnedMeshRenderer renderer))
                    {
                        Log.Warning(this, $"no skinned mesh renderer on game object {newBodyPart}, can't display cloth");
                        return;
                    }
                    newBodyPart.gameObject.SetActive(true);
                    renderer.sharedMesh = newItem.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
                    break;

                // Stop displaying cloth on the player
                case SyncListOperation.RemoveAt:
                    NetworkObject oldBodyPart = oldData._bodyPart;
                    oldBodyPart.gameObject.SetActive(false);
                    break;

            }
        }

		/// <summary>
		/// When the content of a container change, check if it should display or remove display of some clothes.
		/// </summary>
		[Server]
        public void HandleContainerContentChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            // If it's not a cloth type container.
            // It'd be probably better to just create "cloth container" inheriting from container to easily test that.
            if(container.GetComponent<ClothContainer>() == null)
            {
                return;
            }

            switch(type)
            {
                case ContainerChangeType.Add:
					AddCloth(newItem);
                    break;
                case ContainerChangeType.Remove:
					RemoveCloth(oldItem);
                    break;
            }
        }

        /// <summary>
        /// Adds a cloth to the synced list, making a few checks to find where to add it, if possible.
        /// </summary>
        /// <param name="item"> The item to add, it should have a Cloth component on it.</param>
        [Server]
        private void AddCloth(Item item)
        {
            if (item == null || !item.TryGetComponent(out Cloth cloth))
            {
                return;
            }

            ClothType itemClothType = cloth.Type;
            ClothedBodyPart[] clothedBodyParts = GetComponentsInChildren<ClothedBodyPart>(true);
            ClothedBodyPart bodypart = clothedBodyParts.
                Where(x => x.Type == itemClothType).First();

            NetworkObject NetworkedBodyPart = bodypart.gameObject.GetComponent<NetworkObject>();
            if (NetworkedBodyPart != null)
            {
                _clothedBodyParts.Add(new ClothDisplayData(NetworkedBodyPart, item));
            }
        }

        /// <summary>
        /// Remove a cloth from the synced list, check if it's there before removing.
        /// </summary>
        /// <param name="item">The item to add, it should have a Cloth component on it.</param>
        [Server]
        private void RemoveCloth(Item item)
        {
            if (item == null || !item.TryGetComponent(out Cloth cloth))
            {
                return;
            }

            ClothType itemClothType = cloth.Type;
            ClothDisplayData clothdata = _clothedBodyParts.Find(
                x => x._bodyPart.gameObject.GetComponent<ClothedBodyPart>().Type == itemClothType);

            _clothedBodyParts.Remove(clothdata);
        }
    }
}