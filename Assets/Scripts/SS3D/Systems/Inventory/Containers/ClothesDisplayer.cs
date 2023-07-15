
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using FishNet.Object.Synchronizing;
using UnityEditor;
using FishNet.Object;

// nouvel item
// add item
// item has a cloth type
// find network object with same clothtype
// add new clothdisplay data using this clothtype and item to display

// remove item
// item has cloth type
// find network object with same clothtype
// remove cloth display data using this cloth type and item to display.

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Display cloth on player for all clients.
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

        public HumanInventory _inventory;

        // The root game objects for clothes
        public Transform ClothesRoot;


		[SyncObject]
		private readonly SyncList<ClothDisplayData> _clothedBodyParts = new SyncList<ClothDisplayData>();

		[SerializeField]
		private List<NetworkObject> startingClothedBodyPart;

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


		private void ClothedBodyPartsOnChange(SyncListOperation op, int index,
			ClothDisplayData oldData, ClothDisplayData newData, bool asServer)
		{

			if(asServer) return;

			switch (op)
			{
				case SyncListOperation.Add:
					NetworkObject newBodyPart = newData._bodyPart;
					Item newItem = newData._clothToDisplay;
					if (!newBodyPart.TryGetComponent(out SkinnedMeshRenderer renderer))
					{
						Punpun.Warning(this, $"no skinned mesh renderer on game object {newBodyPart}, can't display cloth");
						return;
					}
					newBodyPart.gameObject.SetActive(true);
					renderer.sharedMesh = newItem.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
					break;

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
        public void HandleContainerContentChanged(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            // If it's not a cloth type container.
            // It'd be probably better to just create "cloth container" inheriting from container to easily test that.
            if(container.ContainerType < ContainerType.Bag)
            {
                return;
            }

            switch(type)
            {
                case ContainerChangeType.Add:
					//ShowCloth(container, item, true);
					Item newItem = newItems.FirstOrDefault();
					AddCloth(newItem);
                    break;
                case ContainerChangeType.Remove:
					//ShowCloth(container, item, false);
					Item oldItem = oldItems.FirstOrDefault();
					RemoveCloth(oldItem);
                    break;
            }
        }

		[Server]
		private void AddCloth(Item item)
		{
			if (!item.TryGetComponent(out Cloth cloth))
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

		[Server]
		private void RemoveCloth(Item item)
		{
			if (!item.TryGetComponent(out Cloth cloth))
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