using System;
using Mirror;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Inventory.UI;
using UnityEngine;

namespace SS3D.Content.Systems.Player
{
    [RequireComponent(typeof(Engine.Inventory.Inventory))]
    public class InitUI : NetworkBehaviour
    {
        public GameObject prefab;

        private GameObject instance;

        public override void OnStartLocalPlayer()
        {
            instance = Instantiate(prefab);
            var inventoryUi = instance.GetComponent<InventoryUi>();
            inventoryUi.Inventory = GetComponent<Inventory>();
        }

        public void OnDestroy()
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }
    }
}
