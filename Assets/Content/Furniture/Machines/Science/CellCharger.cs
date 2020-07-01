using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Content.Furniture.Storage;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

[RequireComponent(typeof(Container))]
public class CellCharger : NetworkBehaviour
{
    private StorageContainer storageContainer;
    private Container container;
    private Item powerCell;
    public MeshRenderer renderer;

    private void Start()
    {
        container = GetComponent<Container>();
        Assert.IsNotNull(container);
        
        container.onChange += (op, index, oldItem, newItem) =>
        {
            if (newItem == null) {
                powerCell = null;
                renderer.enabled = false;
            } else {
                powerCell = newItem.GetComponent<Item>();
                renderer.enabled = true;
            }
        };
        
        if (isServer)
        {
            StartCoroutine("StartCharge");
        }
    }

    IEnumerator StartCharge()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            Recharge();
        }
    }

    private void Recharge()
    {
        if (powerCell != null)
        {
            RechargePowerCell(powerCell);
        }
    }

    [Server]
    private void RechargePowerCell(Item item)
    {
        IChargeable chargeable = item.GetComponent<IChargeable>();
        chargeable.AddCharge(chargeable.GetChargeRate());
    }
}