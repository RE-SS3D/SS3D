using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Logging;
using SS3D.Systems.Storage.Items;
using SS3D.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// System used to spawn items.
    /// </summary>
    public sealed class ItemSystem : NetworkSystem
    {
        private readonly Dictionary<ItemIDs, Item> _itemPrefabs = new();

        protected override void OnAwake()
        {
            base.OnAwake();

            LoadItemPrefabs();
        }

        private void LoadItemPrefabs()
        {
            ItemsAssetDatabase itemsAssetDatabase = AssetData.FindDatabase<ItemsAssetDatabase>();
            List<AssetReference> assetReferences = itemsAssetDatabase.Assets;

            for (int index = 0; index < assetReferences.Count; index++)
            {
                ItemIDs id = (ItemIDs)index;
                GameObject itemObject = itemsAssetDatabase.Get(id);
                Item item = itemObject.GetComponent<Item>();

                item.ItemID = id;

                _itemPrefabs.Add(id, item);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnItem(ItemIDs id, Vector3 position, Quaternion rotation)
        {
            SpawnItem(id, position, rotation);
        }

        [Server]
        private Item SpawnItem(ItemIDs id, Vector3 position, Quaternion rotation)
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
            return itemPrefab;
        }
    }
}