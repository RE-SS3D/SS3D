using Inventory.UI;
using Mirror;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Inventory.Inventory))]
    public class InitUI : NetworkBehaviour
    {
        public GameObject prefab;

        public override void OnStartLocalPlayer()
        {
            var obj = Instantiate(prefab);
            obj.GetComponent<UIInventory>().StartUI(GetComponent<Inventory.Inventory>());
        }
    }
}
