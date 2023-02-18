using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Logging;
using SS3D.Systems.Storage.Items;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// System used to spawn items.
    /// </summary>
    public sealed class ItemSystem : NetworkSystem
    {
        /// <summary>
        /// A dictionary of all the preloaded prefabs using the ItemIDs as key.
        /// </summary>
        private readonly Dictionary<ItemIDs, Item> _itemPrefabs = new();

        protected override void OnStart()
        {
            base.OnStart();

            LoadItemPrefabs();
        }

        /// <summary>
        /// Loads the item prefabs into memory with the item id and setting up the item ids variable in the items.
        /// </summary>
        private async void LoadItemPrefabs()
        {
            ItemsAssetDatabase items = Assets.GetDatabase<ItemsAssetDatabase>();

            for (int index = 0; index < items.Assets.Count; index++)
            {
                ItemIDs id = (ItemIDs)index;

                GameObject itemObject = Assets.Get(id);
                Item item = itemObject.GetComponent<Item>();

                item.ItemID = id;

                _itemPrefabs.Add(id, item);
            }
        }

        /// <summary>
        /// Requests to spawn an item.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItem(ItemIDs id, Vector3 position, Quaternion rotation)
        {
            SpawnItem(id, position, rotation);
        }

        /// <summary>
        /// Spawns an Item at a position and rotation.
        ///
        /// TODO: Create a ItemSpawnOptions struct.
        /// </summary>
        /// <param name="id">The item ID to spawn.</param>
        /// <param name="position">The desired position to spawn.</param>
        /// <param name="rotation">The desired rotation to apply.</param>
        /// <returns></returns>
        [Server]
        public Item SpawnItem(ItemIDs id, Vector3 position, Quaternion rotation)
        {
            bool hasValue = _itemPrefabs.TryGetValue(id, out Item itemPrefab);

            if (!hasValue)
            {
                Punpun.Panic(this, $"No item with ID {id.ToString()} was found", Logs.ServerOnly);
                return null;
            }

            Item itemInstance = Instantiate(itemPrefab, position, rotation);
            ServerManager.Spawn(itemInstance.GameObject);

            Punpun.Say(this, $"Item {itemInstance.name} spawned at {position}", Logs.ServerOnly);
            return itemInstance;
        }
    }
}