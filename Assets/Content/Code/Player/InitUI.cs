﻿using Engine.Inventory.UI;
using Mirror;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Engine.Inventory.Inventory))]
    public class InitUI : NetworkBehaviour
    {
        public GameObject prefab;

        public override void OnStartLocalPlayer()
        {
            var obj = Instantiate(prefab);
            obj.GetComponent<UIInventory>().StartUI(GetComponent<Engine.Inventory.Inventory>());
        }
    }
}
