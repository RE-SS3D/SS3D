using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Inventory))]
public class InitUI : NetworkBehaviour
{
    public GameObject prefab;

    public override void OnStartLocalPlayer()
    {
        var obj = Instantiate(prefab);
        obj.GetComponent<UIInventory>().StartUI(GetComponent<Inventory>());
    }
}
