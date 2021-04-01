using System.Collections;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

// This handles the cell charger, responsable for exclusivery recharging batteries

// THIS IS NOT NETWORKED
public class CellCharger : NetworkBehaviour
{
    // Visual container for the cell to be put in
    public AttachedContainer AttachedContainer;
    
    // Actual container
    private StorageContainer storageContainer;

    // Current cell being recharged
    private Item powerCell;
    public MeshRenderer renderer;

    private void Start()
    {
        Assert.IsNotNull(AttachedContainer);

        AttachedContainer.Container.ContentsChanged += (_, items, type) =>
        {
            renderer.enabled = !AttachedContainer.Container.Empty;
        };

        if (isServer)
        {
            StartCoroutine(StartCharge());
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

    // Recharges the current cell that is placed on it
    private void Recharge()
    {
        foreach (Item item in AttachedContainer.Container.Items)
        {
            IChargeable chargeable = item.GetComponent<IChargeable>();
            if (chargeable != null)
            {
		// Effectively add charge
                chargeable.AddCharge(chargeable.GetChargeRate());
            }
            
        }
    }
}